# Anonymous usage telemetry

M365 Trace Analyzer supports optional, explicit opt-in product-usage telemetry. Telemetry is disabled by default and the consent UI is shown only when an Application Insights connection string is configured.

Trace processing remains local. Telemetry never includes trace filenames, paths, URLs, hosts, headers, bodies, findings, usernames, machine names, hardware identifiers, tenant IDs, mailbox identifiers, exception messages, or passwords.

## Configuration

Configure a dedicated Application Insights resource using either:

```text
Telemetry__ApplicationInsightsConnectionString
```

or the `Telemetry:ApplicationInsightsConnectionString` application setting. Do not use the analyzer's telemetry resource for server or dependency monitoring. The application creates an isolated OpenTelemetry logger that exports only the allowlisted events below. Exporter offline storage is disabled so events are not retained for later transmission.

When no connection string is configured, the exporter and consent controls are unavailable and no usage events are created.

The release workflow reads the encrypted GitHub repository secret
`APPLICATIONINSIGHTS_CONNECTION_STRING` and writes it into the published
package's `appsettings.json`. The connection string is never committed to
source. If the secret is absent, release packages keep telemetry disabled.

## Consent and identifiers

- Consent starts as `Unknown`, which behaves as disabled and displays a blocking full-screen choice before trace controls can be used.
- Selecting **Yes, share anonymous usage** persists `Enabled`.
- Selecting **No, do not share** or **Turn off** persists `Disabled`.
- A random anonymous installation GUID is created only when telemetry is enabled.
- A new random application-session GUID is created for every process launch.
- The installation GUID can be reset from the telemetry settings control.
- Settings are stored in `%LOCALAPPDATA%\M365 Trace Analyzer\settings.json` using an atomic file replacement.
- Unreadable or invalid settings fail closed to `Unknown`.

The installation GUID represents one analyzer installation, not a person. Application Insights may derive a coarse country or region from the public egress IP. That value can reflect a corporate proxy or VPN rather than the user's physical location.

## Event schema

Schema version `1` includes two custom events:

### `ApplicationStarted`

- `installation_id`
- `application_session_id`
- `app_version`
- `os_family`
- `architecture`
- `telemetry_schema_version`

### `TraceImportCompleted`

- All `ApplicationStarted` dimensions except operating system and architecture
- `format`: `har`, `saz`, or `unknown`
- `encrypted`: `true` or `false`
- `outcome`: `complete`, `partial`, `failed`, or `cancelled`
- `session_count_bucket`: `0`, `1-99`, `100-999`, `1k-9k`, `10k-99k`, `100k+`, or `not_available`
- `duration_bucket`: `under_1s`, `1-5s`, `5-30s`, `30-120s`, or `120s+`
- `error_code`: `none`, `invalid_password`, `import_rejected`, `io_error`, or `invalid_operation`
- `telemetry_schema_version`

One event is emitted per completed trace-import attempt, never per HTTP session in the trace. A password-required prompt is an intermediate state and does not emit a failure event.

## Operational safeguards

- Use a dedicated Application Insights resource with a daily cap and cost alert.
- Keep IP masking enabled.
- Restrict resource access to the minimum reporting audience.
- Monitor unexpected volume because a desktop connection string is not a secret and can be spoofed.
- Do not add automatic ASP.NET Core request, dependency, exception, page-view, performance-counter, or framework-log collection.
- Review every schema change for high-cardinality or trace-derived values before release.
