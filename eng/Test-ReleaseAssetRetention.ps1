$ErrorActionPreference = "Stop"

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$scriptPath = Join-Path $repositoryRoot "eng\Invoke-ReleaseAssetRetention.ps1"
$fixturePath = Join-Path (
    [IO.Path]::GetTempPath()) "release-retention-$([Guid]::NewGuid().ToString('N')).json"

function Assert-Plan
{
    param(
        [Parameter(Mandatory)]
        [object[]] $Plan,

        [Parameter(Mandatory)]
        [string[]] $ExpectedVersions
    )

    $actualVersions = @($Plan.Version | Sort-Object)
    $expected = @($ExpectedVersions | Sort-Object)
    if (($actualVersions -join ",") -ne ($expected -join ","))
    {
        throw "Expected versions '$($expected -join ",")' but found '$($actualVersions -join ",")'."
    }
}

$releases = @(
    @{
        id = 110
        tag_name = "v1.0.10"
        draft = $false
        prerelease = $false
        assets = @(
            @{
                id = 1010
                name = "M365-Trace-Analyzer-v1.0.10-win-x64.zip"
            },
            @{
                id = 1011
                name = "M365-Trace-Analyzer-v1.0.10.spdx.json"
            }
        )
    },
    @{
        id = 108
        tag_name = "v1.0.8"
        draft = $false
        prerelease = $false
        assets = @(
            @{
                id = 1008
                name = "M365-Trace-Analyzer-v1.0.8-win-x64.zip"
            },
            @{
                id = 1009
                name = "unrelated.zip"
            }
        )
    },
    @{
        id = 109
        tag_name = "v1.0.9"
        draft = $false
        prerelease = $false
        assets = @(
            @{
                id = 1009
                name = "M365-Trace-Analyzer-v1.0.9-win-x64.zip"
            }
        )
    },
    @{
        id = 111
        tag_name = "v1.0.11"
        draft = $true
        prerelease = $false
        assets = @(
            @{
                id = 1012
                name = "M365-Trace-Analyzer-v1.0.11-win-x64.zip"
            }
        )
    },
    @{
        id = 200
        tag_name = "v2.0.0-beta.1"
        draft = $false
        prerelease = $true
        assets = @(
            @{
                id = 2000
                name = "M365-Trace-Analyzer-v2.0.0-win-x64.zip"
            }
        )
    }
)

try
{
    $releases |
        ConvertTo-Json -Depth 10 |
        Set-Content -LiteralPath $fixturePath

    $defaultPlan = @(
        & $scriptPath `
            -ReleaseDataPath $fixturePath `
            -KeepStableZipCount 2 `
            -DryRun `
            -PassThru
    )
    Assert-Plan -Plan $defaultPlan -ExpectedVersions @("1.0.8")

    $withdrawalPlan = @(
        & $scriptPath `
            -ReleaseDataPath $fixturePath `
            -KeepStableZipCount 2 `
            -WithdrawVersion "v1.0.9" `
            -DryRun `
            -PassThru
    )
    Assert-Plan `
        -Plan $withdrawalPlan `
        -ExpectedVersions @("1.0.8", "1.0.9")
    $withdrawnRelease = $withdrawalPlan |
        Where-Object Version -eq "1.0.9"
    if ($withdrawnRelease.Reason -ne "Explicit security withdrawal")
    {
        throw "Explicit withdrawals must override age-based retention."
    }

    $singleReleasePlan = @(
        & $scriptPath `
            -ReleaseDataPath $fixturePath `
            -KeepStableZipCount 1 `
            -DryRun `
            -PassThru
    )
    Assert-Plan `
        -Plan $singleReleasePlan `
        -ExpectedVersions @("1.0.8", "1.0.9")

    Write-Host "Release asset retention behavior is verified."
}
finally
{
    Remove-Item -LiteralPath $fixturePath -Force -ErrorAction SilentlyContinue
}
