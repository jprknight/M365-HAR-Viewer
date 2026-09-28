param(
    [string] $WorkbookPath = (
        Join-Path $PSScriptRoot `
            "telemetry\M365-Trace-Analyzer-Telemetry.workbook.json"
    )
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $WorkbookPath -PathType Leaf)) {
    throw "Workbook definition not found: $WorkbookPath"
}

$workbook = Get-Content -LiteralPath $WorkbookPath -Raw | ConvertFrom-Json

if ($workbook.version -ne "Notebook/1.0") {
    throw "The workbook must use Notebook/1.0."
}

$queryItems = @($workbook.items | Where-Object { $_.type -eq 3 })
if ($queryItems.Count -lt 7) {
    throw "Expected at least seven telemetry report queries."
}

$queries = $queryItems.content.query -join "`n"
$requiredDimensions = @(
    "installation_id",
    "application_session_id",
    "app_version",
    "ClientCountryOrRegion",
    "outcome",
    "error_code",
    "session_count_bucket",
    "duration_bucket"
)

foreach ($dimension in $requiredDimensions) {
    if (-not $queries.Contains($dimension, [StringComparison]::Ordinal)) {
        throw "Workbook queries do not cover '$dimension'."
    }
}

if (-not $queries.Contains(
        "M365TraceAnalyzer.UsageTelemetry",
        [StringComparison]::Ordinal)) {
    throw "Workbook queries must filter to the usage telemetry category."
}

if (-not $queries.Contains(
        'Properties["telemetry_schema_version"]',
        [StringComparison]::Ordinal)) {
    throw "Workbook queries must filter to a telemetry schema version."
}

Write-Host "Telemetry reporting workbook validation passed."
