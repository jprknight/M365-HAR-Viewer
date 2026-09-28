$ErrorActionPreference = "Stop"

$configurationScript = Join-Path `
    $PSScriptRoot `
    "Set-PublishedTelemetryConfiguration.ps1"
$temporaryDirectory = Join-Path `
    ([System.IO.Path]::GetTempPath()) `
    "m365-trace-telemetry-config-$([Guid]::NewGuid().ToString('N'))"
$settingsPath = Join-Path $temporaryDirectory "appsettings.json"
$validConnectionString =
    "InstrumentationKey=00000000-0000-0000-0000-000000000000;" +
    "IngestionEndpoint=https://example.test/"

try {
    New-Item -ItemType Directory -Path $temporaryDirectory | Out-Null
    @{
        Logging = @{
            LogLevel = @{
                Default = "Information"
            }
        }
    } |
        ConvertTo-Json -Depth 10 |
        Set-Content -LiteralPath $settingsPath -Encoding utf8

    & $configurationScript `
        -PublishDirectory $temporaryDirectory `
        -ConnectionString $validConnectionString
    $settings = Get-Content -LiteralPath $settingsPath -Raw |
        ConvertFrom-Json
    if ($settings.Telemetry.ApplicationInsightsConnectionString -ne
        $validConnectionString) {
        throw "The valid connection string was not written."
    }

    & $configurationScript `
        -PublishDirectory $temporaryDirectory `
        -ConnectionString ""
    $settings = Get-Content -LiteralPath $settingsPath -Raw |
        ConvertFrom-Json
    if (-not [string]::IsNullOrEmpty(
        $settings.Telemetry.ApplicationInsightsConnectionString)) {
        throw "An absent connection string did not disable telemetry."
    }

    $invalidConnectionStringRejected = $false
    try {
        & $configurationScript `
            -PublishDirectory $temporaryDirectory `
            -ConnectionString "invalid"
    }
    catch {
        $invalidConnectionStringRejected = $true
    }

    if (-not $invalidConnectionStringRejected) {
        throw "An invalid connection string was accepted."
    }

    Remove-Item -LiteralPath $settingsPath -Force
    $missingSettingsRejected = $false
    try {
        & $configurationScript `
            -PublishDirectory $temporaryDirectory `
            -ConnectionString $validConnectionString
    }
    catch {
        $missingSettingsRejected = $true
    }

    if (-not $missingSettingsRejected) {
        throw "A missing published settings file was accepted."
    }

    Write-Host "Published telemetry configuration tests passed."
}
finally {
    if (Test-Path -LiteralPath $temporaryDirectory) {
        Remove-Item -LiteralPath $temporaryDirectory -Recurse -Force
    }
}
