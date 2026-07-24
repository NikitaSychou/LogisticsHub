[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string] $GatewayUrl = "https://ca-gateway-logisticshub-dev-free.wittyisland-fa7a06fc.swedencentral.azurecontainerapps.io",
    [string] $StaticWebsiteUrl = "https://stlghubdevfree600544.z1.web.core.windows.net/",
    [string] $StorageAccountName = "stlghubdevfree600544",
    [string] $SpaClientId = "9a11cd54-d5e6-4a09-a236-dfbb02309d3a",
    [string] $Authority = "https://login.microsoftonline.com/942af48b-f19a-49f4-a016-5f3ef85774a9",
    [string] $ApiScope = "api://dcfdc59c-73f1-457d-9dcd-4363640e9bf9/access_as_user",
    [string] $RedirectUri = "https://stlghubdevfree600544.z1.web.core.windows.net/",
    [switch] $SkipBuild,
    [switch] $SkipUpload
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) {
    $PSScriptRoot
}
else {
    (Get-Location).Path
}

$webRoot = Join-Path $scriptRoot "src\Web\LogisticsHub.Web"
$browserOutput = Join-Path $webRoot "dist\LogisticsHub.Web\browser"
$runtimeConfigPath = Join-Path $browserOutput "runtime-config.json"

function Test-HttpUrl {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Value,
        [bool] $RequireTrailingSlash = $false
    )

    if ([string]::IsNullOrWhiteSpace($Value) -or $Value -match "\s") {
        return $false
    }

    $uri = $null
    if (-not [System.Uri]::TryCreate($Value, [System.UriKind]::Absolute, [ref] $uri)) {
        return $false
    }

    if ($uri.Scheme -notin @("http", "https")) {
        return $false
    }

    if ($RequireTrailingSlash -and -not $Value.EndsWith("/", [System.StringComparison]::Ordinal)) {
        return $false
    }

    return $true
}

function Assert-SafeScalar {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name,
        [Parameter(Mandatory = $true)]
        [string] $Value
    )

    if ([string]::IsNullOrWhiteSpace($Value) -or $Value -match "\s") {
        throw "$Name must be a non-empty value without whitespace."
    }
}

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string] $FilePath,
        [Parameter(Mandatory = $true)]
        [string[]] $Arguments,
        [string] $WorkingDirectory = $scriptRoot
    )

    Push-Location $WorkingDirectory
    try {
        & $FilePath @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "$FilePath $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}

function Get-RelativeBlobName {
    param(
        [Parameter(Mandatory = $true)]
        [string] $BasePath,
        [Parameter(Mandatory = $true)]
        [string] $FilePath
    )

    $relative = [System.IO.Path]::GetRelativePath($BasePath, $FilePath)
    return $relative.Replace([System.IO.Path]::DirectorySeparatorChar, "/")
}

function Test-HashedAngularAsset {
    param(
        [Parameter(Mandatory = $true)]
        [string] $BlobName
    )

    $fileName = [System.IO.Path]::GetFileName($BlobName)
    return $fileName -match '^(main|polyfills|runtime|styles|chunk)-[A-Za-z0-9_-]{8,}\.(js|css)$'
}

function Get-CacheControl {
    param(
        [Parameter(Mandatory = $true)]
        [string] $BlobName
    )

    if ($BlobName -eq "index.html" -or $BlobName -eq "runtime-config.json") {
        return "no-store, no-cache, must-revalidate"
    }

    if (Test-HashedAngularAsset -BlobName $BlobName) {
        return "public, max-age=31536000, immutable"
    }

    return "no-cache, must-revalidate"
}

if (-not (Test-HttpUrl -Value $GatewayUrl)) {
    throw "GatewayUrl must be an absolute HTTP or HTTPS URL."
}

if (-not (Test-HttpUrl -Value $StaticWebsiteUrl -RequireTrailingSlash $true)) {
    throw "StaticWebsiteUrl must be an absolute HTTP or HTTPS URL ending with '/'."
}

if (-not (Test-HttpUrl -Value $RedirectUri -RequireTrailingSlash $true)) {
    throw "RedirectUri must be an absolute HTTP or HTTPS URL ending with '/'."
}

Assert-SafeScalar -Name "StorageAccountName" -Value $StorageAccountName
Assert-SafeScalar -Name "SpaClientId" -Value $SpaClientId
Assert-SafeScalar -Name "Authority" -Value $Authority
Assert-SafeScalar -Name "ApiScope" -Value $ApiScope

if ($GatewayUrl.IndexOf("localhost", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
    $RedirectUri.IndexOf("localhost", [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
    throw "Production runtime configuration must not point to localhost."
}

if (-not $SkipBuild) {
    Invoke-CheckedCommand -FilePath "npm" -Arguments @("run", "build") -WorkingDirectory $webRoot
}

if (-not (Test-Path -LiteralPath $browserOutput -PathType Container)) {
    throw "Angular browser output was not found at $browserOutput."
}

$runtimeConfig = [ordered]@{
    api = [ordered]@{
        gatewayBaseUrl = $GatewayUrl.TrimEnd("/")
        scope = $ApiScope
    }
    msal = [ordered]@{
        clientId = $SpaClientId
        authority = $Authority.TrimEnd("/")
        redirectUri = $RedirectUri
    }
}

$runtimeConfig | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $runtimeConfigPath -Encoding utf8 -NoNewline

$loadedRuntimeConfig = Get-Content -Raw -LiteralPath $runtimeConfigPath | ConvertFrom-Json
if (-not (Test-HttpUrl -Value $loadedRuntimeConfig.api.gatewayBaseUrl) -or
    -not (Test-HttpUrl -Value $loadedRuntimeConfig.msal.authority) -or
    -not (Test-HttpUrl -Value $loadedRuntimeConfig.msal.redirectUri -RequireTrailingSlash $true) -or
    [string]::IsNullOrWhiteSpace($loadedRuntimeConfig.api.scope) -or
    [string]::IsNullOrWhiteSpace($loadedRuntimeConfig.msal.clientId)) {
    throw "Generated runtime-config.json failed validation."
}

if ($loadedRuntimeConfig.api.gatewayBaseUrl -match "localhost" -or $loadedRuntimeConfig.msal.redirectUri -match "localhost") {
    throw "Generated runtime-config.json still contains localhost."
}

if ($SkipUpload) {
    Write-Host "Generated runtime-config.json at $runtimeConfigPath. Upload skipped."
    return
}

$localFiles = Get-ChildItem -LiteralPath $browserOutput -File -Recurse
$localBlobNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

foreach ($file in $localFiles) {
    $blobName = Get-RelativeBlobName -BasePath $browserOutput -FilePath $file.FullName
    [void] $localBlobNames.Add($blobName)
    $cacheControl = Get-CacheControl -BlobName $blobName

    if ($PSCmdlet.ShouldProcess("$StorageAccountName/`$web/$blobName", "Upload Angular asset")) {
        Invoke-CheckedCommand -FilePath "az" -Arguments @(
            "storage", "blob", "upload",
            "--account-name", $StorageAccountName,
            "--container-name", "`$web",
            "--name", $blobName,
            "--file", $file.FullName,
            "--auth-mode", "login",
            "--overwrite", "true",
            "--content-cache-control", $cacheControl,
            "--only-show-errors"
        )
    }
}

$remoteBlobNames = @(& az storage blob list --account-name $StorageAccountName --container-name "`$web" --auth-mode login --query "[].name" -o tsv)
if ($LASTEXITCODE -ne 0) {
    throw "az storage blob list failed with exit code $LASTEXITCODE."
}

foreach ($blobName in $remoteBlobNames) {
    if (-not $localBlobNames.Contains($blobName)) {
        if ($PSCmdlet.ShouldProcess("$StorageAccountName/`$web/$blobName", "Delete obsolete Angular asset")) {
            Invoke-CheckedCommand -FilePath "az" -Arguments @(
                "storage", "blob", "delete",
                "--account-name", $StorageAccountName,
                "--container-name", "`$web",
                "--name", $blobName,
                "--auth-mode", "login",
                "--only-show-errors"
            )
        }
    }
}

Write-Host "Angular deployment completed for $StaticWebsiteUrl."