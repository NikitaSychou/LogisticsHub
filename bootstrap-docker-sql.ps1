[CmdletBinding()]
param(
    [string] $ContainerName = "logisticshub-sqlserver",
    [string] $SaPassword = "LogisticsHub_DevPassword123!",
    [string] $SqlCmdPath = "/opt/mssql-tools18/bin/sqlcmd"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) {
    $PSScriptRoot
}
else {
    (Get-Location).Path
}

$databases = @(
    @{
        Name = "InventoryDb"
        SchemaFile = "InventoryDb.schema.sql"
        ExpectedTables = @(
            "inventory_inbox_messages",
            "inventory_outbox_messages",
            "items",
            "stock_balances",
            "stock_reservation_items",
            "stock_reservations"
        )
    },
    @{
        Name = "ShipmentDb"
        SchemaFile = "ShipmentDb.schema.sql"
        PatchFiles = @(
            "ShipmentDb.company-address-columns.sql"
        )
        ExpectedTables = @(
            "shipment_inbox_messages",
            "shipment_items",
            "shipment_outbox_messages",
            "shipment_status_history",
            "shipments"
        )
    },
    @{
        Name = "CompanyDb"
        SchemaFile = "CompanyDb.schema.sql"
        ExpectedTables = @(
            "companies",
            "company_addresses"
        )
    }
)

function Invoke-Docker {
    param(
        [Parameter(Mandatory = $true)]
        [string[]] $Arguments
    )

    $output = @(& docker @Arguments 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw "docker $($Arguments -join ' ') failed with exit code ${LASTEXITCODE}:`n$($output -join "`n")"
    }

    return @($output)
}

function Test-HasAnyItem {
    param(
        [AllowNull()]
        [object[]] $Items
    )

    return ($null -ne $Items -and $Items.Length -gt 0)
}

function Invoke-SqlCmd {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Query,

        [string] $Database = "master"
    )

    return Invoke-Docker -Arguments @(
        "exec",
        $ContainerName,
        $SqlCmdPath,
        "-S",
        "localhost,1433",
        "-U",
        "sa",
        "-P",
        $SaPassword,
        "-C",
        "-b",
        "-d",
        $Database,
        "-Q",
        $Query
    )
}

function Invoke-SqlFile {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Database,

        [Parameter(Mandatory = $true)]
        [string] $ContainerPath
    )

    Invoke-Docker -Arguments @(
        "exec",
        $ContainerName,
        $SqlCmdPath,
        "-S",
        "localhost,1433",
        "-U",
        "sa",
        "-P",
        $SaPassword,
        "-C",
        "-b",
        "-d",
        $Database,
        "-i",
        $ContainerPath
    ) | Out-Null
}

function Invoke-SchemaPatchFiles {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Database,

        [Parameter(Mandatory = $true)]
        [hashtable] $DatabaseConfig,

        [Parameter(Mandatory = $true)]
        [string] $ContainerDirectory
    )

    $patchFileNames = @()
    if ($DatabaseConfig.ContainsKey("PatchFiles") -and $null -ne $DatabaseConfig["PatchFiles"]) {
        $patchFileNames = @($DatabaseConfig["PatchFiles"] |
            Where-Object { -not [string]::IsNullOrWhiteSpace([string] $_) } |
            ForEach-Object { [string] $_ })
    }

    if (-not (Test-HasAnyItem -Items $patchFileNames)) {
        return
    }

    foreach ($patchFileName in $patchFileNames) {
        $patchFilePath = Join-Path -Path $scriptRoot -ChildPath $patchFileName

        if (-not (Test-Path -LiteralPath $patchFilePath)) {
            throw "Patch file '$patchFileName' was not found at '$patchFilePath'. Keep patch files beside this script."
        }

        $containerPatchPath = "$ContainerDirectory/$Database.$([System.IO.Path]::GetFileName($patchFileName))"
        Invoke-Docker -Arguments @("cp", $patchFilePath, "${ContainerName}:$containerPatchPath") | Out-Null
        Invoke-SqlFile -Database $Database -ContainerPath $containerPatchPath

        Write-Host "Applied $patchFileName to $Database."
    }
}

function Get-UserTables {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Database
    )

    $query = @"
SET NOCOUNT ON;
SELECT s.name + '.' + t.name
FROM sys.tables AS t
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name;
"@

    $lines = @(Invoke-Docker -Arguments @(
        "exec",
        $ContainerName,
        $SqlCmdPath,
        "-S",
        "localhost,1433",
        "-U",
        "sa",
        "-P",
        $SaPassword,
        "-C",
        "-b",
        "-d",
        $Database,
        "-h",
        "-1",
        "-W",
        "-Q",
        $query
    ))

    $tables = @($lines |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { $_.Trim() })

    return $tables
}

function Split-SchemaFileForApply {
    param(
        [Parameter(Mandatory = $true)]
        [string] $SchemaFilePath,

        [Parameter(Mandatory = $true)]
        [string] $Database
    )

    $tempDirectory = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "logisticshub-schema-$([Guid]::NewGuid().ToString('N'))"
    New-Item -ItemType Directory -Path $tempDirectory | Out-Null

    $mainPath = Join-Path -Path $tempDirectory -ChildPath "$Database.main.sql"
    $foreignKeyPath = Join-Path -Path $tempDirectory -ChildPath "$Database.foreign-keys.sql"

    $mainLines = [System.Collections.Generic.List[string]]::new()
    $foreignKeyLines = [System.Collections.Generic.List[string]]::new()
    $batchLines = [System.Collections.Generic.List[string]]::new()

    $appendBatch = {
        param(
            [System.Collections.Generic.List[string]] $SourceLines,
            [System.Collections.Generic.List[string]] $MainLines,
            [System.Collections.Generic.List[string]] $ForeignKeyLines
        )

        if ($null -eq $SourceLines -or $SourceLines.ToArray().Length -eq 0) {
            return
        }

        $batchArray = @($SourceLines.ToArray())
        $batchText = [string]::Join("`n", $batchArray)
        $isForeignKeyBatch = $false

        if (-not [string]::IsNullOrWhiteSpace($batchText)) {
            $isForeignKeyBatch =
                [regex]::IsMatch($batchText, "\bFOREIGN\s+KEY\b", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase) -or
                [regex]::IsMatch($batchText, "\bCHECK\s+CONSTRAINT\s+\[FK_", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        }

        if ($isForeignKeyBatch) {
            foreach ($line in $batchArray) {
                $ForeignKeyLines.Add([string] $line)
            }

            $ForeignKeyLines.Add("GO")
            $ForeignKeyLines.Add("")
        }
        else {
            foreach ($line in $batchArray) {
                $MainLines.Add([string] $line)
            }

            $MainLines.Add("GO")
            $MainLines.Add("")
        }

        $SourceLines.Clear()
    }

    $schemaLines = @(Get-Content -LiteralPath $SchemaFilePath)

    foreach ($line in $schemaLines) {
        $currentLine = if ($null -eq $line) { "" } else { [string] $line }

        if ([regex]::IsMatch($currentLine, "^\s*GO\s*$", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
            & $appendBatch $batchLines $mainLines $foreignKeyLines
        }
        else {
            $batchLines.Add($currentLine)
        }
    }

    & $appendBatch $batchLines $mainLines $foreignKeyLines

    Set-Content -LiteralPath $mainPath -Value @($mainLines.ToArray()) -Encoding UTF8
    Set-Content -LiteralPath $foreignKeyPath -Value @($foreignKeyLines.ToArray()) -Encoding UTF8

    return @{
        Directory = $tempDirectory
        MainPath = $mainPath
        ForeignKeyPath = $foreignKeyPath
    }
}

function Assert-ContainerReady {
    $runningContainerId = @(& docker ps --filter "name=^/$ContainerName$" --filter "status=running" --format "{{.ID}}" 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to inspect Docker containers. Ensure Docker is running. Output:`n$($runningContainerId -join "`n")"
    }

    $firstRunningContainerId = @($runningContainerId | Select-Object -First 1)
    if (-not (Test-HasAnyItem -Items $firstRunningContainerId) -or [string]::IsNullOrWhiteSpace([string] $firstRunningContainerId[0])) {
        throw "Container '$ContainerName' is not running. Start the local system with 'docker compose up --build' first."
    }

    Invoke-Docker -Arguments @("exec", $ContainerName, "test", "-x", $SqlCmdPath) | Out-Null
}

function Ensure-Database {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Database
    )

    $query = @"
SET NOCOUNT ON;
IF DB_ID(N'$Database') IS NULL
BEGIN
    CREATE DATABASE [$Database];
    PRINT 'Created database $Database.';
END
ELSE
BEGIN
    PRINT 'Database $Database already exists.';
END
"@

    Invoke-SqlCmd -Query $query | Out-Null
}

Assert-ContainerReady

$containerSchemaDirectory = "/tmp/logisticshub-schema"
Invoke-Docker -Arguments @("exec", $ContainerName, "mkdir", "-p", $containerSchemaDirectory) | Out-Null

foreach ($database in $databases) {
    $databaseName = [string] $database["Name"]
    $schemaFileName = [string] $database["SchemaFile"]
    $schemaFilePath = Join-Path -Path $scriptRoot -ChildPath $schemaFileName

    if (-not (Test-Path -LiteralPath $schemaFilePath)) {
        throw "Schema file '$schemaFileName' was not found at '$schemaFilePath'. Run this script from the repository root or keep the schema snapshots beside this script."
    }

    Ensure-Database -Database $databaseName

    $existingTables = @(Get-UserTables -Database $databaseName)
    $expectedTables = @($database["ExpectedTables"] | ForEach-Object { "dbo.$_" })
    $missingExpectedTables = @($expectedTables | Where-Object { $_ -notin $existingTables })

    if ((Test-HasAnyItem -Items $existingTables) -and -not (Test-HasAnyItem -Items $missingExpectedTables)) {
        Write-Host "$databaseName already contains the expected schema tables."
        Invoke-SchemaPatchFiles -Database $databaseName -DatabaseConfig $database -ContainerDirectory $containerSchemaDirectory
        continue
    }

    if (Test-HasAnyItem -Items $existingTables) {
        throw @"
$databaseName already contains user tables but does not match the expected schema table set.
Existing tables:
$($existingTables -join "`n")

Missing expected tables:
$($missingExpectedTables -join "`n")

Leaving the database unchanged. Use a fresh Docker SQL volume or clean the database manually before rerunning this bootstrap.
"@
    }

    $splitSchema = Split-SchemaFileForApply -SchemaFilePath $schemaFilePath -Database $databaseName

    try {
        $containerMainSchemaPath = "$containerSchemaDirectory/$databaseName.main.sql"
        $containerForeignKeySchemaPath = "$containerSchemaDirectory/$databaseName.foreign-keys.sql"

        Invoke-Docker -Arguments @("cp", ([string] $splitSchema["MainPath"]), "${ContainerName}:$containerMainSchemaPath") | Out-Null
        Invoke-Docker -Arguments @("cp", ([string] $splitSchema["ForeignKeyPath"]), "${ContainerName}:$containerForeignKeySchemaPath") | Out-Null

        Invoke-SqlFile -Database $databaseName -ContainerPath $containerMainSchemaPath
        Invoke-SqlFile -Database $databaseName -ContainerPath $containerForeignKeySchemaPath

        Write-Host "Applied $schemaFileName to $databaseName."
        Invoke-SchemaPatchFiles -Database $databaseName -DatabaseConfig $database -ContainerDirectory $containerSchemaDirectory
    }
    finally {
        if ($null -ne $splitSchema -and $splitSchema.ContainsKey("Directory") -and (Test-Path -LiteralPath ([string] $splitSchema["Directory"]))) {
            Remove-Item -LiteralPath ([string] $splitSchema["Directory"]) -Recurse -Force
        }
    }
}

Write-Host "Docker SQL bootstrap complete."
