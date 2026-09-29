# Data handling

## Scope

This document inventories data handled by M365 Trace Analyzer and describes its
local lifecycle. Diagnostic traces can contain sensitive support data,
authentication material, personal data, tenant identifiers, URLs, headers,
request bodies, response bodies, and message content.

Use the application only with data you are authorized to access and according
to applicable organizational retention and handling requirements.

## Data inventory

| Data | Sensitivity | Location | Persistence | Network transfer | Deletion or reset |
| --- | --- | --- | --- | --- | --- |
| HAR source content | Potentially sensitive support data | Browser-selected stream and application memory | Active import and investigation | None | Released when replaced or the process exits |
| SAZ source content | Potentially sensitive support data | Browser-selected stream, unique OS temporary file, and application memory | Temporary file for the import attempt; sessions remain in memory | None | Temporary file deleted in `finally`; memory released when replaced or the process exits |
| Normalized sessions and findings | Inherits source sensitivity | Application memory and local browser connection | Current investigation | Loopback connection only | Replaced by a new trace or process exit |
| SAZ password | Secret | Active component and importer state | Current password attempt only | None | Cleared from component state and not written to settings |
| Telemetry consent | User preference | `%LOCALAPPDATA%\\M365 Trace Analyzer\\settings.json` | Until changed or the file is removed | None | User can change consent; settings file can be deleted |
| Anonymous installation ID | Low-sensitivity pseudonymous identifier | Local telemetry settings | Created only after opt-in; retained until reset | Included in opted-in telemetry | User can reset the ID or disable telemetry |
| Application-session ID | Low-sensitivity pseudonymous identifier | Application memory | One process lifetime | Included in opted-in telemetry | Process exit |
| Allowlisted usage events | Coarse product-usage data | Application Insights when configured and opted in | Controlled by the telemetry resource configuration | HTTPS to the configured telemetry endpoint | Governed by the telemetry deployment's retention and deletion process |
| Release-check request | Connection metadata | GitHub Releases API | GitHub service policy | One process-cached HTTPS request | No application-side persistence |

## Data categories that may appear in traces

Depending on the source system and capture method, traces may contain:

- Authentication headers, cookies, access tokens, or session identifiers.
- User names, email addresses, tenant identifiers, mailbox identifiers, and IP
  addresses.
- URLs and query parameters.
- Message, calendar, directory, or collaboration content.
- Device names, process names, file paths, and diagnostic identifiers.
- Request and response bodies.

The analyzer does not attempt to classify or redact every sensitive value in a
trace. Findings and displayed output inherit the sensitivity of the source
trace.

## Storage behavior

### Memory

Imported sessions, body content retained within configured limits, search
indexes, summaries, and findings remain in the local process memory. They are
not saved as an analyzer workspace.

### Temporary SAZ file

The SAZ importer creates a uniquely named file under the operating system's
temporary directory because the ZIP implementation requires seekable access.
Normal completion, rejection, cancellation, and handled failures delete the file
in a `finally` block.

A hard process termination, device restart, or operating-system failure can
prevent cleanup. Users handling highly sensitive traces must follow their
organization's temporary-file cleanup requirements.

### Local settings

The settings file contains only telemetry consent and, when telemetry is
enabled, a random installation identifier. It must not contain trace content,
passwords, URLs, findings, or exception messages.

## Network behavior

Trace content is not intentionally sent over a network. The documented outbound
behaviors are:

1. A public GitHub Releases API request for update availability.
2. Explicitly opted-in anonymous usage events when a telemetry endpoint is
   configured.

The local browser communicates with the application only through IPv4 and IPv6
loopback.

## Telemetry operational evidence

The source repository defines the telemetry schema and safeguards but does not
control the deployed Application Insights resource. Before telemetry-enabled
distribution is approved, the deployment owner must record:

- Azure tenant, subscription, resource, and region in the confidential audit
  package.
- Configured retention period.
- IP masking configuration.
- Role assignments and reporting audience.
- Access-review cadence.
- Daily cap and cost-alert configuration.
- Incident and deletion procedures.

Do not publish resource identifiers, connection strings, role-assignment
details, or confidential operational evidence in the public repository.

## User handling requirements

- Use only traces you are authorized to access.
- Store source traces only in approved locations.
- Do not attach traces, findings, screenshots, tokens, or customer data to
  public issues or pull requests.
- Use generated or appropriately sanitized fixtures for demonstrations and
  testing.
- Review findings before copying or sharing them.
- Close the application when analysis is complete.
- Delete source files and residual temporary data according to applicable
  retention requirements.
- Treat generated findings as diagnostic assistance, not as an authoritative
  legal, compliance, privacy, or security decision.

See the
[support-engineer operating guide](SUPPORT-ENGINEER-OPERATING-GUIDE.md) for the
recommended end-user procedure.
