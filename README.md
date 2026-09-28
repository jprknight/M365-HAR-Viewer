# M365 Trace Analyzer

M365 Trace Analyzer is a local browser-based application for inspecting and analyzing Microsoft 365 HTTP traces.

The current implementation supports opening HTTP Archive (`.har`) and encrypted or unencrypted Fiddler Session Archive (`.saz`) files, viewing normalized sessions, summarizing trace-wide findings and traffic patterns, combining global session search with structured filters, elevating diagnostic headers, inspecting request and response headers, viewing bodies as raw text, formatted JSON, sandboxed HTML, images, or formatted XML, and running the migrated Office 365 Fiddler Extension analysis rules. HAR imports retain structured protocol, size, endpoint, connection, redirect, cache, query, cookie, page, timing, and content-completeness metadata when present. SAZ imports retain protocol, endpoint, process, connection, TLS, timing, and completeness metadata, decode chunked and compressed bodies in wire order, and distinguish unsupported encodings from corrupt encoded content. An expandable import-quality banner reports complete, partial, skipped, truncated, missing-response, unsupported, and invalid-content conditions while retaining usable sessions from partially malformed traces. Session search covers URLs, methods, statuses, analysis findings, request and response headers, and retained decoded body text. The virtualized session grid supports Up/Down and Home/End navigation across large traces, with Right Arrow moving into session details and Left Arrow returning to the selected session. Summary metrics can drill into the matching sessions, and active structured filters remain visible as removable chips. Long-running imports show transient phase status, can be cancelled, and cannot overwrite a newer file selection. Trace data is processed by the local ASP.NET Core application and is not uploaded to an external service.

## Run the standalone Windows application

- Download the latest Windows release.
- Extract the ZIP file.
- Open a terminal in the extracted folder and run:

```powershell
./M365Trace.Web.exe
```

The application attempts to open `http://localhost:8080` in the default web browser after startup. If the browser does not open, navigate to that address manually. Keep the terminal open while using the analyzer and press `Ctrl+C` to stop it.

To use a different local port:

```powershell
./M365Trace.Web.exe --port 9090
```

The application always listens only on the local computer's IPv4 and IPv6
loopback interfaces. Network-interface and wildcard bindings are not supported.

## Versioning and releases

The application version is defined in `src\M365Trace.Web\M365Trace.Web.csproj` using semantic versioning. The browser header displays the version embedded in the running assembly.

At startup, the application makes one anonymous request to the public GitHub Releases API for `jprknight/M365-Trace-Analyzer`. The result is cached for the lifetime of the local process. The UI reports:

- **Latest available version** when the running version matches or exceeds the latest published release
- **Version x.y.z available** with a link when a newer release exists
- **No published release yet** when the repository does not have a release
- **Update check unavailable** when GitHub cannot be reached

Trace import and analysis continue to work when offline or when the version check fails.

## Privacy and optional usage telemetry

Trace data is always processed locally and is never sent through the optional telemetry channel. Builds configured with a dedicated Application Insights connection string show a full-screen first-run consent prompt for anonymous product-usage telemetry. The user must explicitly choose Yes or No before opening a trace. Consent defaults to off, can be changed at any time, and includes a control to reset the random anonymous installation ID.

The allowlisted telemetry reports application version, operating-system family, architecture, random installation and application-session IDs, coarse trace format, encrypted status, import outcome, session-count and duration buckets, and normalized error codes. It never reports trace names, paths, URLs, hosts, headers, bodies, findings, user or machine identity, tenant or mailbox identifiers, passwords, or exception messages. Exporter offline storage is disabled.

See [Anonymous usage telemetry](docs/USAGE-TELEMETRY.md) for configuration, consent behavior, the complete event schema, and operational safeguards.

Repository-maintained [telemetry reporting queries and workbook deployment instructions](docs/TELEMETRY-REPORTING.md) cover anonymous installations, application sessions, imports, versions, countries or regions, outcomes, and normalized failures.

See [Security and privacy](docs/SECURITY-AND-PRIVACY.md) for the complete public data-flow and storage description. The [public threat model](docs/THREAT-MODEL.md) documents trust boundaries, mitigations, residual risks, and review triggers. Report suspected vulnerabilities privately according to [SECURITY.md](SECURITY.md).

To publish a version:

1. Update `<Version>`, `<AssemblyVersion>`, and `<FileVersion>` in `M365Trace.Web.csproj`.
2. Build and test the release.
3. Create and push a matching tag such as `v0.1.0`.
4. The tag-triggered release workflow validates the version, builds and tests the application, runs the packaged Chromium workflow, creates the Windows ZIP and SHA-256 checksum, attests the artifact, and publishes the GitHub Release.

The latest stable GitHub Release is the update source of truth. Tags without a published release are not offered to users.

## Test

```powershell
dotnet test .\M365-Trace-Analyzer.sln
```

See [CONTRIBUTING.md](CONTRIBUTING.md) for secure development requirements,
test expectations, and rules for synthetic or sanitized fixtures.

## Projects

- `M365Trace.Core` - normalized trace models
- `M365Trace.Core.Tests` - normalized metadata and compatibility tests
- `M365Trace.Import.Har` - bounded HAR parsing and validation
- `M365Trace.Import.Saz` - bounded SAZ/ZIP and raw HTTP session parsing
- `M365Trace.Rules` - host-independent analysis rule contracts
- `M365Trace.Web` - local Blazor browser interface
- `M365Trace.Benchmarks` - generated large-trace performance and memory benchmarks
- `M365Trace.Import.Har.Tests` - importer tests and sanitized fixtures
- `M365Trace.Import.Saz.Tests` - generated SAZ archive and safety tests
- `M365Trace.Rules.Tests` - deterministic ruleset regression tests

## Supported trace files

- HAR 1.x JSON exported from browser developer tools
- Encrypted or unencrypted SAZ archives containing `raw/*_c.txt`, `*_s.txt`, and optional `*_m.xml` files

Password-protected SAZ archives support traditional ZipCrypto plus AES-128 and AES-256 encryption. Passwords are used only for the current import and are not stored. SAZ imports enforce compressed-file, expanded-size, entry-count, per-session-file, and decompressed-body limits.

## Migrated rules

The analyzer embeds the legacy session-classification data, localized ruleset strings, and a ruleset metadata manifest. Concrete `ITraceRule` implementations are discovered automatically at startup and loaded into a validated immutable catalog. Duplicate or malformed rule IDs prevent startup rather than failing during trace analysis.

Each imported session receives a cached `SessionFacts` view containing normalized URL, host, path, headers, content type, and searchable request and response text. Rules use those facts instead of repeatedly rebuilding the same values.

HTTP 200 handling is split into focused ordered rule classes under `Legacy\Http200`, covering hidden failures, Microsoft 365 protocols, content validation, and the final healthy fallback.

The current deterministic rule pipeline includes:

- Standard HTTP response classifications and unknown-status handling
- Specialized HTTP 0, 200, 302, 307, 400, 401, 403, 404, 456, 500, 502, 503, and 504 analysis
- Hidden HTTP 200 failures, including Client Access Rules, MAPI protocol/culture errors, malformed JSON, Autodiscover response validation, Free/Busy failures, and errors returned inside successful bodies
- Exchange Online and Exchange Server MAPI, EWS, Autodiscover, OWA, attachment, RPC, NSPI, REST People, suggestions, and Free/Busy classification
- Authentication classification for bearer, basic, SAML, and sessions without authentication headers
- Response-server header classification
- Apache Autodiscover, loopback, and NetLog broad checks
- Corrected warning and severe thresholds for long-running trace sessions

Rules execute in explicit phases and deterministic order. Specialized rules take precedence over generic fallbacks, and confidence values prevent less-specific classifications from replacing stronger results.

The browser displays the application version. See [Classification coverage](CLASSIFICATION-COVERAGE.md) for the included classification count, internal schema version, and technical implementation inventory.

Several clearly unreachable legacy predicates were implemented according to their apparent intent and covered by regression tests. This includes Exchange Online HTTP 401 Autodiscover host matching, attachment classification before generic OWA handling, specialized HTTP 0 handling, and accepting either known Outlook Microsoft 365 host for general classification.

HAR does not normally contain Fiddler process names, server/client IP addresses, TLS tunnel details, or detailed server timing. Rules that require those fields are not guessed. SAZ metadata coverage can be expanded as those values are added to the normalized trace model.

## Roadmap

See the [support-engineering roadmap](SUPPORT-ENGINEERING-ROADMAP.md) for the prioritized trace-analysis, reporting, metadata, performance, and investigation-workflow plan.
