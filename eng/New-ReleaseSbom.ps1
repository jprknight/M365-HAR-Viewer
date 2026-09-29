param(
    [Parameter(Mandatory)]
    [string] $BuildDropPath,

    [Parameter(Mandatory)]
    [string] $PackageVersion,

    [Parameter(Mandatory)]
    [string] $OutputPath,

    [string] $PackageSupplier = "Person: jprknight",

    [string] $SourceRevision = "unknown"
)

$ErrorActionPreference = "Stop"

$sbomToolVersion = "4.1.5"
$sbomToolSha256 =
    "625767b371b7fdd58f40f618b8a86da0247a33c89e419039c86b4edba1dad4b5"
$sbomToolUri =
    "https://github.com/microsoft/sbom-tool/releases/download/" +
    "v$sbomToolVersion/sbom-tool-win-x64.exe"

$buildDrop = (Resolve-Path -LiteralPath $BuildDropPath).Path
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$componentRoot = Join-Path $repositoryRoot "src"
$resolvedOutputPath = [IO.Path]::GetFullPath($OutputPath)
$outputDirectory = Split-Path -Parent $resolvedOutputPath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$toolRoot = if ([string]::IsNullOrWhiteSpace($env:RUNNER_TEMP)) {
    Join-Path ([IO.Path]::GetTempPath()) "m365-trace-sbom-tool"
}
else {
    Join-Path $env:RUNNER_TEMP "m365-trace-sbom-tool"
}
New-Item -ItemType Directory -Path $toolRoot -Force | Out-Null
$toolPath = Join-Path $toolRoot "sbom-tool-v$sbomToolVersion-win-x64.exe"

$downloadTool = $true
if (Test-Path -LiteralPath $toolPath) {
    $existingHash = (Get-FileHash -LiteralPath $toolPath -Algorithm SHA256).Hash
    $downloadTool = -not $existingHash.Equals(
        $sbomToolSha256,
        [StringComparison]::OrdinalIgnoreCase)
}

if ($downloadTool) {
    Invoke-WebRequest -Uri $sbomToolUri -OutFile $toolPath
}

$toolHash = (Get-FileHash -LiteralPath $toolPath -Algorithm SHA256).Hash
if (-not $toolHash.Equals(
        $sbomToolSha256,
        [StringComparison]::OrdinalIgnoreCase)) {
    throw "The downloaded Microsoft SBOM tool did not match the expected SHA-256."
}

$manifestRoot = Join-Path (
    [IO.Path]::GetTempPath()) "m365-trace-sbom-$([Guid]::NewGuid().ToString('N'))"
$telemetryPath = Join-Path $manifestRoot "sbom-tool-telemetry.json"
$validationPath = Join-Path $manifestRoot "sbom-tool-validation.json"

try {
    New-Item -ItemType Directory -Path $manifestRoot -Force | Out-Null
    $namespacePart = "M365-Trace-Analyzer-$PackageVersion-$SourceRevision" `
        -replace "[^A-Za-z0-9._-]", "-"

    & $toolPath generate `
        -b $buildDrop `
        -bc $componentRoot `
        -m $manifestRoot `
        -pn "M365 Trace Analyzer" `
        -pv $PackageVersion `
        -ps $PackageSupplier `
        -nsb "https://github.com/jprknight/M365-Trace-Analyzer" `
        -nsu $namespacePart `
        -pm true `
        -D true `
        -t $telemetryPath `
        -V Warning
    if ($LASTEXITCODE -ne 0) {
        throw "Microsoft SBOM tool failed with exit code $LASTEXITCODE."
    }

    $manifestPath = Join-Path `
        $manifestRoot "_manifest\spdx_2.2\manifest.spdx.json"
    if (-not (Test-Path -LiteralPath $manifestPath)) {
        throw "The Microsoft SBOM tool did not create an SPDX JSON manifest."
    }

    & $toolPath validate `
        -b $buildDrop `
        -m (Join-Path $manifestRoot "_manifest") `
        -o $validationPath `
        -mi "SPDX:2.2" `
        -t $telemetryPath `
        -V Warning
    if ($LASTEXITCODE -ne 0) {
        throw "Microsoft SBOM tool validation failed with exit code $LASTEXITCODE."
    }

    Copy-Item `
        -LiteralPath $manifestPath `
        -Destination $resolvedOutputPath `
        -Force

    Write-Host "Generated release SBOM at '$resolvedOutputPath'."
}
finally {
    Remove-Item -LiteralPath $manifestRoot -Recurse -Force -ErrorAction SilentlyContinue
}
