[CmdletBinding()]
param(
    [string] $ServerInstance = "localhost\SQLEXPRESS",
    [string[]] $DatabaseNames = @("InventoryDb", "ShipmentDb"),
    [string] $OutputDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Import-SqlServerSmo {
    try {
        Import-Module SqlServer -ErrorAction Stop
        return
    }
    catch {
        throw @"
The PowerShell SqlServer module is required to export schema with SMO, but it is not available.

Install it from an elevated or appropriately permitted PowerShell session:
  Install-Module SqlServer -Scope CurrentUser

No database changes were made.
"@
    }
}

function New-SchemaScripter {
    param(
        [Microsoft.SqlServer.Management.Smo.Server] $Server
    )

    $scripter = [Microsoft.SqlServer.Management.Smo.Scripter]::new($Server)
    $scripter.Options.ScriptData = $false
    $scripter.Options.ScriptSchema = $true
    $scripter.Options.WithDependencies = $false
    $scripter.Options.SchemaQualify = $true
    $scripter.Options.IncludeDatabaseContext = $false
    $scripter.Options.IncludeHeaders = $false
    $scripter.Options.NoCollation = $false
    $scripter.Options.NoFileGroup = $true
    $scripter.Options.NoIdentities = $false
    $scripter.Options.DriDefaults = $true
    $scripter.Options.DriPrimaryKey = $true
    $scripter.Options.DriForeignKeys = $true
    $scripter.Options.DriUniqueKeys = $true
    $scripter.Options.DriChecks = $true
    $scripter.Options.Indexes = $true
    $scripter.Options.ClusteredIndexes = $true
    $scripter.Options.NonClusteredIndexes = $true
    $scripter.Options.Triggers = $false
    $scripter.Options.Permissions = $false
    $scripter.Options.ExtendedProperties = $false
    $scripter.Options.ToFileOnly = $false

    return $scripter
}

function Add-ScriptBlock {
    param(
        [System.Collections.Generic.List[string]] $Lines,
        [object] $ScriptResult
    )

    foreach ($line in $ScriptResult) {
        if (-not [string]::IsNullOrWhiteSpace($line)) {
            $Lines.Add($line)
            $Lines.Add("GO")
            $Lines.Add("")
        }
    }
}

Import-SqlServerSmo

$scriptRoot = if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) {
    $PSScriptRoot
}
else {
    (Get-Location).Path
}

$outputRoot = if (-not [string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory
}
else {
    $scriptRoot
}

$resolvedOutputDirectory = Resolve-Path -LiteralPath $outputRoot
$server = [Microsoft.SqlServer.Management.Smo.Server]::new($ServerInstance)
$server.ConnectionContext.LoginSecure = $true
$server.ConnectionContext.Connect()

try {
    foreach ($databaseName in $DatabaseNames) {
        $database = $server.Databases[$databaseName]
        if ($null -eq $database) {
            throw "Database '$databaseName' was not found on server '$ServerInstance'."
        }

        $outputPath = Join-Path -Path $resolvedOutputDirectory -ChildPath "$databaseName.schema.sql"
        $scripter = New-SchemaScripter -Server $server
        $lines = [System.Collections.Generic.List[string]]::new()

        $lines.Add("-- LogisticsHub schema export")
        $lines.Add("-- Server: $ServerInstance")
        $lines.Add("-- Database: $databaseName")
        $lines.Add("-- Generated: $([DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"))")
        $lines.Add("-- Schema only. No table data is included.")
        $lines.Add("")

        $userSchemas = @($database.Schemas |
            Where-Object {
                -not $_.IsSystemObject -and
                $_.Name -notin @("dbo", "guest", "INFORMATION_SCHEMA", "sys")
            } |
            Sort-Object Name)

        foreach ($schema in $userSchemas) {
            Add-ScriptBlock -Lines $lines -ScriptResult $schema.Script()
        }

        $tables = @($database.Tables |
            Where-Object { -not $_.IsSystemObject } |
            Sort-Object Schema, Name)

        foreach ($table in $tables) {
            Add-ScriptBlock -Lines $lines -ScriptResult $scripter.Script($table)
        }

        Set-Content -LiteralPath $outputPath -Value $lines -Encoding UTF8
        Write-Host "Exported schema for $databaseName to $outputPath"
    }
}
finally {
    if ($server.ConnectionContext.IsOpen) {
        $server.ConnectionContext.Disconnect()
    }
}
