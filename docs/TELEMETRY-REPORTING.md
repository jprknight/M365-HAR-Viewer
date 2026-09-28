# Anonymous usage telemetry reporting

The repository includes a deployable Azure Monitor workbook for the allowlisted events documented in [Anonymous usage telemetry](USAGE-TELEMETRY.md).

The workbook reports:

- Distinct anonymous installations
- Distinct application sessions
- Trace import attempts
- Daily adoption and import trends
- Application-version adoption
- Country or region derived by Application Insights
- Import outcomes by trace format
- Normalized failures and cancellations
- Session-count, duration, and encrypted-import profiles

All queries filter to `M365TraceAnalyzer.UsageTelemetry` and telemetry schema version `1`. Treat the desktop connection string as a routing identifier rather than an authentication secret. Unexpected event volume or dimension values can be spoofed and should not be treated as authoritative customer records.

## Deploy or update the workbook

Authenticate Azure CLI to the subscription that contains the Application Insights resource, then run:

```powershell
.\eng\Deploy-TelemetryWorkbook.ps1 `
  -SubscriptionId d0ef73a3-25d8-4521-96c6-d0f5063c169d `
  -ResourceGroup rg-m365-trace-analyzer-telemetry `
  -ApplicationInsightsName appi-m365-trace-analyzer-telemetry
```

The deployment is idempotent. It updates the shared workbook with resource ID `0112be0f-8b0a-4d73-ad7e-6d70bc2a0a2d` and binds the resource-neutral definition in `eng\telemetry\M365-Trace-Analyzer-Telemetry.workbook.json` to the selected Application Insights component.

## Query validation

Validate the checked-in workbook structure without Azure access:

```powershell
.\eng\Test-TelemetryReporting.ps1
```

The workbook uses a rolling 30-day reporting window. Application Insights may derive country or region from public egress IP, so corporate proxies and VPNs can affect the result.
