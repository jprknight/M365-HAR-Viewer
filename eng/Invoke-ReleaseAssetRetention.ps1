[CmdletBinding()]
param(
    [string] $Repository = $env:GITHUB_REPOSITORY,

    [ValidateRange(1, 100)]
    [int] $KeepStableZipCount = 2,

    [string[]] $WithdrawVersion = @(),

    [switch] $DryRun,

    [string] $ReleaseDataPath,

    [switch] $PassThru
)

$ErrorActionPreference = "Stop"
$zipNamePattern =
    "^M365-Trace-Analyzer-v(?<version>\d+\.\d+\.\d+)-win-x64\.zip$"

function Get-ReleaseData
{
    if (-not [string]::IsNullOrWhiteSpace($ReleaseDataPath))
    {
        return @(Get-Content -LiteralPath $ReleaseDataPath -Raw |
            ConvertFrom-Json)
    }

    if ([string]::IsNullOrWhiteSpace($Repository))
    {
        throw "Repository is required outside a GitHub Actions environment."
    }

    $response = & gh api `
        --paginate `
        "repos/$Repository/releases?per_page=100" `
        --slurp
    if ($LASTEXITCODE -ne 0)
    {
        throw "Unable to list releases for '$Repository'."
    }

    $pages = $response -join [Environment]::NewLine | ConvertFrom-Json
    $releases = @()
    foreach ($page in @($pages))
    {
        $releases += @($page)
    }

    return $releases
}

function Normalize-Version
{
    param([Parameter(Mandatory)][string] $Value)

    $normalized = $Value.Trim().TrimStart("v", "V")
    if ($normalized -notmatch "^\d+\.\d+\.\d+$")
    {
        throw "Version '$Value' must use the X.Y.Z format."
    }

    return ([version] $normalized).ToString()
}

$withdrawnVersions = [Collections.Generic.HashSet[string]]::new(
    [StringComparer]::OrdinalIgnoreCase)
foreach ($version in $WithdrawVersion)
{
    if (-not [string]::IsNullOrWhiteSpace($version))
    {
        $null = $withdrawnVersions.Add((Normalize-Version $version))
    }
}

$stableReleases = @(
    foreach ($release in @(Get-ReleaseData))
    {
        if ($release.draft -or $release.prerelease)
        {
            continue
        }

        $tag = [string] $release.tag_name
        if ($tag -notmatch "^v?(?<version>\d+\.\d+\.\d+)$")
        {
            Write-Warning "Ignoring non-semantic release tag '$tag'."
            continue
        }

        [pscustomobject] @{
            Release = $release
            Tag = $tag
            Version = [version] $Matches.version
        }
    }
) | Sort-Object Version -Descending

$plan = @()
for ($index = 0; $index -lt $stableReleases.Count; $index++)
{
    $stableRelease = $stableReleases[$index]
    $version = $stableRelease.Version.ToString()
    $removeForAge = $index -ge $KeepStableZipCount
    $removeForWithdrawal = $withdrawnVersions.Contains($version)
    if (-not $removeForAge -and -not $removeForWithdrawal)
    {
        continue
    }

    $expectedAssetName =
        "M365-Trace-Analyzer-v$version-win-x64.zip"
    foreach ($asset in @($stableRelease.Release.assets))
    {
        $assetName = [string] $asset.name
        $matchesZipPattern = $assetName -match $zipNamePattern
        $matchesExpectedName = $assetName -ceq $expectedAssetName
        if (-not $matchesZipPattern -or -not $matchesExpectedName)
        {
            continue
        }

        $plan += [pscustomobject] @{
            Version = $version
            ReleaseTag = $stableRelease.Tag
            ReleaseId = [long] $stableRelease.Release.id
            AssetId = [long] $asset.id
            AssetName = $assetName
            Reason = if ($removeForWithdrawal)
            {
                "Explicit security withdrawal"
            }
            else
            {
                "Outside latest $KeepStableZipCount stable releases"
            }
        }
    }
}

if ($plan.Count -eq 0)
{
    Write-Host "No release ZIP assets require removal."
}

foreach ($item in $plan)
{
    $message =
        "$($item.AssetName) from $($item.ReleaseTag): $($item.Reason)"
    if ($DryRun)
    {
        Write-Host "[dry-run] Would remove $message"
        continue
    }

    Write-Host "Removing $message"
    & gh api `
        --method DELETE `
        "repos/$Repository/releases/assets/$($item.AssetId)" |
        Out-Null
    if ($LASTEXITCODE -ne 0)
    {
        throw "Unable to delete release asset '$($item.AssetName)'."
    }
}

if ($PassThru)
{
    $plan
}
