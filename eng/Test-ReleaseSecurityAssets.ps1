$ErrorActionPreference = "Stop"

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$workflowPath = Join-Path $repositoryRoot ".github\workflows\release.yml"
$retentionWorkflowPath = Join-Path `
    $repositoryRoot ".github\workflows\release-asset-retention.yml"
$sbomScriptPath = Join-Path $repositoryRoot "eng\New-ReleaseSbom.ps1"
$retentionScriptPath = Join-Path `
    $repositoryRoot "eng\Invoke-ReleaseAssetRetention.ps1"
$licensePath = Join-Path $repositoryRoot "LICENSE"
$codeOwnersPath = Join-Path $repositoryRoot ".github\CODEOWNERS"

foreach ($path in @(
    $workflowPath,
    $retentionWorkflowPath,
    $sbomScriptPath,
    $retentionScriptPath,
    $licensePath,
    $codeOwnersPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required release security asset was not found at '$path'."
    }
}

$workflow = Get-Content -LiteralPath $workflowPath -Raw
$retentionWorkflow = Get-Content -LiteralPath $retentionWorkflowPath -Raw
$sbomScript = Get-Content -LiteralPath $sbomScriptPath -Raw
$retentionScript = Get-Content -LiteralPath $retentionScriptPath -Raw

$requiredWorkflowText = @(
    "New-ReleaseSbom.ps1",
    "M365-Trace-Analyzer-v`$version.spdx.json",
    "sbom-path:",
    "actions/attest@v4",
    "steps.sbom.outputs.sbom-path",
    "Invoke-ReleaseAssetRetention.ps1",
    "KeepStableZipCount 2"
)
foreach ($requiredText in $requiredWorkflowText) {
    if (-not $workflow.Contains($requiredText, [StringComparison]::Ordinal)) {
        throw "Release workflow is missing required SBOM text '$requiredText'."
    }
}

$requiredRetentionText = @(
    "workflow_dispatch:",
    "schedule:",
    "dry_run:",
    "withdraw_versions:",
    "`$isDryRun =",
    "Invoke-ReleaseAssetRetention.ps1"
)
foreach ($requiredText in $requiredRetentionText) {
    if (-not $retentionWorkflow.Contains(
            $requiredText,
            [StringComparison]::Ordinal)) {
        throw "Retention workflow is missing required text '$requiredText'."
    }
}

$requiredRetentionScriptText = @(
    "KeepStableZipCount",
    "WithdrawVersion",
    "ReleaseDataPath",
    "DryRun",
    "releases/assets",
    "M365-Trace-Analyzer-v"
)
foreach ($requiredText in $requiredRetentionScriptText) {
    if (-not $retentionScript.Contains(
            $requiredText,
            [StringComparison]::Ordinal)) {
        throw "Retention script is missing required text '$requiredText'."
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
