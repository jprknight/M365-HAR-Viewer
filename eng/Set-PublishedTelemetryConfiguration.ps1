param(
    [Parameter(Mandatory)]
    [string] $PublishDirectory,

    [string] $ConnectionString
)

$ErrorActionPreference = "Stop"

if (-not [string]::IsNullOrWhiteSpace($ConnectionString) -and
    $ConnectionString -notmatch "^InstrumentationKey=[^;]+;IngestionEndpoint=https://") {
    throw "The Application Insights connection string is not valid."
}

$publishPath = (Resolve-Path -LiteralPath $PublishDirectory).Path
$settingsPath = Join-Path $publishPath "appsettings.json"
if (-not (Test-Path -LiteralPath $settingsPath)) {
    throw "Published settings were not found at '$settingsPath'."
}

$settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
if ($null -eq $settings.Telemetry) {
    $settings | Add-Member -MemberType NoteProperty -Name Telemetry -Value ([pscustomobject] @{})
}

if ($null -eq $settings.Telemetry.ApplicationInsightsConnectionString) {
    $settings.Telemetry | Add-Member `
        -MemberType NoteProperty `
        -Name ApplicationInsightsConnectionString `
        -Value ([string] $ConnectionString)
}
else {
    $settings.Telemetry.ApplicationInsightsConnectionString =
        [string] $ConnectionString
}

$settings |
    ConvertTo-Json -Depth 20 |
    Set-Content -LiteralPath $settingsPath -Encoding utf8

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    Write-Host "Usage telemetry is disabled in the published application."
}
else {
    Write-Host "Configured anonymous usage telemetry in the published application."
}
