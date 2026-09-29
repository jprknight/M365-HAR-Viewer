$ErrorActionPreference = "Stop"

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$workflowPath = Join-Path $repositoryRoot ".github\workflows\release.yml"
$sbomScriptPath = Join-Path $repositoryRoot "eng\New-ReleaseSbom.ps1"
$licensePath = Join-Path $repositoryRoot "LICENSE"
$codeOwnersPath = Join-Path $repositoryRoot ".github\CODEOWNERS"

foreach ($path in @(
    $workflowPath,
    $sbomScriptPath,
    $licensePath,
    $codeOwnersPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required release security asset was not found at '$path'."
    }
}

$workflow = Get-Content -LiteralPath $workflowPath -Raw
$sbomScript = Get-Content -LiteralPath $sbomScriptPath -Raw

$requiredWorkflowText = @(
    "New-ReleaseSbom.ps1",
    "M365-Trace-Analyzer-v`$version.spdx.json",
    "sbom-path:",
    "actions/attest@v4",
    "steps.sbom.outputs.sbom-path"
)
foreach ($requiredText in $requiredWorkflowText) {
    if (-not $workflow.Contains($requiredText, [StringComparison]::Ordinal)) {
        throw "Release workflow is missing required SBOM text '$requiredText'."
    }
}

$requiredScriptText = @(
    '$sbomToolVersion = "4.1.5"',
    "625767b371b7fdd58f40f618b8a86da0247a33c89e419039c86b4edba1dad4b5",
    "Get-FileHash",
    "manifest.spdx",
    "PackageSupplier"
)
foreach ($requiredText in $requiredScriptText) {
    if (-not $sbomScript.Contains($requiredText, [StringComparison]::OrdinalIgnoreCase)) {
        throw "SBOM generation script is missing required text '$requiredText'."
    }
}

$license = Get-Content -LiteralPath $licensePath -Raw
$containsLicenseName = $license.Contains(
    "Apache License",
    [StringComparison]::Ordinal)
$containsLicenseVersion = $license.Contains(
    "Version 2.0, January 2004",
    [StringComparison]::Ordinal)
if (-not $containsLicenseName -or -not $containsLicenseVersion) {
    throw "LICENSE does not contain the Apache License 2.0 text."
}

Write-Host "Release security assets are configured."
