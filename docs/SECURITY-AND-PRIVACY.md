# Security and privacy

M365 Trace Analyzer is a local browser-based application for inspecting HAR and
SAZ diagnostic traces. Diagnostic traces can contain sensitive or personal
information. Use the application only with data you are authorized to access
and according to your organization's data-handling and retention requirements.

This document describes the current implementation. It is not a certification,
compliance guarantee, or authorization to process a particular category of
data.

## Processing model

- The application runs as a local ASP.NET Core process and listens on
  `http://localhost:8080` by default.
- The listener is programmatically restricted to the local computer's IPv4 and
  IPv6 loopback interfaces. It cannot be changed to a network-interface or
  wildcard binding through URL configuration.
- The browser connects to that local process to select, analyze, and display a
  trace.
- Trace parsing, rule evaluation, filtering, and display occur in the local
  process. Trace contents are not uploaded to a hosted analysis service.
- Imported sessions and findings remain in process memory until another trace
  replaces them or the application exits.
- The port can be changed with `--port`, but the loopback-only restriction
  remains in effect.

## Input files and temporary storage

### HAR

HAR content is read from the browser-selected file stream and processed by the
local application. The application does not intentionally create a persistent
copy of the HAR file.

### SAZ

SAZ content is copied to a uniquely named file in the operating system's
temporary directory so the ZIP implementation can inspect the archive. The
temporary file is deleted in a `finally` block after the import attempt.

An abrupt process or operating-system termination can prevent normal cleanup.
Users handling highly sensitive data should review and clean the operating
system's temporary directory according to their organization's requirements.

Password-protected SAZ files support ZipCrypto, AES-128, and AES-256. A password
is used only for the active import attempt and is not written to application
settings or included in usage telemetry.

## Input safety controls

HAR imports enforce limits on file size, entry count, retained text length, and
metadata item count.

SAZ imports enforce limits on:

- Compressed file size.
- Archive entry count.
- Total expanded size.
- Individual archive entry size.
- Retained text length.
- Archive paths.

These controls reduce, but do not eliminate, the risk of excessive resource
consumption from malformed or intentionally hostile files.

HTML response previews run in a sandboxed iframe with a restrictive content
security policy and no-referrer policy. Scripts and remote resources are
blocked. XML parsing prohibits DTD processing and external entity resolution.

## Network communication

The application has two documented outbound network behaviors.

### Release update check

At startup, the application makes one anonymous HTTPS request to the public
GitHub Releases API for the latest M365 Trace Analyzer release. The result is
cached for the lifetime of the process. Trace analysis continues when the
request fails or the computer is offline.

The request does not include trace data. As with ordinary HTTPS traffic, the
destination service can observe connection metadata such as the public egress
IP address, request time, and application user agent.

### Optional usage telemetry

Published builds can be configured with a dedicated Application Insights
connection string. When configured, telemetry remains disabled until the user
explicitly opts in. Consent can be withdrawn and the anonymous installation ID
can be reset from the application.

Allowlisted events contain:

- Application version.
- Operating-system family and architecture.
- Random installation and application-session identifiers.
- Coarse trace format and encrypted status.
- Import outcome.
- Session-count and duration buckets.
- Normalized error codes.

Telemetry does not intentionally include trace filenames, paths, URLs, hosts,
headers, bodies, findings, usernames, machine names, hardware identifiers,
tenant or mailbox identifiers, passwords, or exception messages. Exporter
offline storage is disabled.

The telemetry preference and random installation identifier are stored in:

```text
%LOCALAPPDATA%\M365 Trace Analyzer\settings.json
```

For the complete schema and configuration behavior, see
[Anonymous usage telemetry](USAGE-TELEMETRY.md).

## User responsibilities

Users must:

- Analyze only data they are authorized to access.
- Protect trace files and analyzer output according to the sensitivity of the
  source data.
- Use synthetic or appropriately sanitized data for demonstrations, tests, and
  public issue reports.
- Review displayed findings before copying or sharing them.
- Close the application when analysis is complete.
- Delete source files and residual temporary data according to applicable
  retention requirements.

Do not attach trace files, screenshots containing diagnostic content, generated
findings, credentials, tokens, tenant information, or customer data to public
GitHub issues or pull requests.

## Development and review

Security and privacy requirements are considered during design, implementation,
verification, release, and maintenance. Material changes to data handling,
network communication, authentication, storage, telemetry, dependencies,
deployment architecture, or diagnostic previews require renewed review by the
appropriate security teams.

To report a suspected vulnerability, follow [SECURITY.md](../SECURITY.md).
