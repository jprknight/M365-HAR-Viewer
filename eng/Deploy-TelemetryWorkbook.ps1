param(
    [Parameter(Mandatory)]
    [string] $SubscriptionId,

    [Parameter(Mandatory)]
    [string] $ResourceGroup,

    [Parameter(Mandatory)]
    [string] $ApplicationInsightsName,

    [string] $WorkbookId = "0112be0f-8b0a-4d73-ad7e-6d70bc2a0a2d",

    [string] $WorkbookPath = (
        Join-Path $PSScriptRoot `
            "telemetry\M365-Trace-Analyzer-Telemetry.workbook.json"
    )
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI is required to deploy the telemetry workbook."
}

if (-not (Test-Path -LiteralPath $WorkbookPath -PathType Leaf)) {
    throw "Workbook definition not found: $WorkbookPath"
}

& az account set --subscription $SubscriptionId
if ($LASTEXITCODE -ne 0) {
    throw "Unable to select Azure subscription '$SubscriptionId'."
}

$componentJson = & az monitor app-insights component show `
    --resource-group $ResourceGroup `
    --app $ApplicationInsightsName `
    --output json
if ($LASTEXITCODE -ne 0) {
    throw "Unable to find Application Insights component '$ApplicationInsightsName'."
}

$component = $componentJson | ConvertFrom-Json
$workbook = Get-Content -LiteralPath $WorkbookPath -Raw | ConvertFrom-Json
$workbook.fallbackResourceIds = @($component.id)
$serializedData = $workbook | ConvertTo-Json -Depth 100 -Compress

$request = @{
    location = $component.location
    kind = "shared"
    tags = @{
        Project = "M365 Trace Analyzer"
        Purpose = "Privacy-first usage telemetry reporting"
    }
    properties = @{
        category = "workbook"
        description = "Privacy-first product usage telemetry for M365 Trace Analyzer."
        displayName = "M365 Trace Analyzer - Usage Telemetry"
        serializedData = $serializedData
        sourceId = $component.id
        version = "Notebook/1.0"
    }
}

$requestPath = Join-Path `
    ([System.IO.Path]::GetTempPath()) `
    "m365-trace-analyzer-workbook-$([Guid]::NewGuid()).json"

try {
    $request |
        ConvertTo-Json -Depth 100 |
        Set-Content -LiteralPath $requestPath -Encoding utf8

    $resourceUrl =
        "https://management.azure.com/subscriptions/$SubscriptionId" `
        + "/resourceGroups/$ResourceGroup/providers/Microsoft.Insights" `
        + "/workbooks/${WorkbookId}?api-version=2022-04-01"

    $responseJson = & az rest `
        --method put `
        --url $resourceUrl `
        --body "@$requestPath" `
        --output json
    if ($LASTEXITCODE -ne 0) {
        throw "Azure Monitor workbook deployment failed."
    }

    $response = $responseJson | ConvertFrom-Json
    Write-Host "Workbook deployed: $($response.id)"
}
finally {
    Remove-Item -LiteralPath $requestPath -Force -ErrorAction SilentlyContinue
}
