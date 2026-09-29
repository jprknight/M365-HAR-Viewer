# Support-engineer operating guide

## Intended use

M365 Trace Analyzer is a single-user local diagnostic application for authorized
analysis of HAR and SAZ traces. It assists investigation; it does not replace
engineering judgment or provide an authoritative legal, privacy, compliance, or
security decision.

## Before use

1. Download only the latest supported Windows release from the official
   repository.
2. Confirm that the release is not marked superseded or unsupported.
3. Verify the downloaded ZIP's SHA-256 value against the release notes.
4. When required by your organization, verify the GitHub artifact and SBOM
   attestations.
5. Extract the complete ZIP to an approved local location.
6. Do not run the application from an email attachment, public file-sharing
   location, or an untrusted download source.
7. Do not run the application with elevated privileges unless a separately
   reviewed operational requirement exists.

## Start the analyzer

Run:

```powershell
./M365Trace.Web.exe
```

The application opens `http://localhost:8080` and listens only on the local
computer's IPv4 and IPv6 loopback interfaces.

To use another local port:

```powershell
./M365Trace.Web.exe --port 9090
```

Network-interface, wildcard, shared-server, and remote-hosting modes are not
supported.

## Analyze a trace

1. Confirm that you are authorized to access the trace and its contents.
2. Open the trace from an approved local location.
3. For an encrypted SAZ, enter the password only in the analyzer password
   prompt.
4. Review import-quality warnings before relying on findings.
5. Treat findings as diagnostic leads and validate them against the underlying
   request and response evidence.
6. Review copied findings and screenshots for sensitive values before sharing.

## Telemetry choice

Telemetry controls appear only in builds configured with a telemetry endpoint.
Telemetry is disabled until the user explicitly opts in.

The telemetry schema is intended to exclude trace content, file names, URLs,
hosts, headers, bodies, findings, identities, passwords, and exception
messages. See [Anonymous usage telemetry](USAGE-TELEMETRY.md) for the reviewed
schema.

Telemetry is not an audit log and must not be used as evidence that a particular
trace was or was not analyzed.

## Complete the investigation

1. Close the browser tab.
2. Stop the application with `Ctrl+C`.
3. Delete source traces and copied findings according to applicable retention
   requirements.
4. If the process or computer terminated abnormally during SAZ import, inspect
   the operating system's temporary directory according to your organization's
   cleanup process.
5. Do not retain archive passwords in notes, scripts, command histories, or
   issue reports.

## Prohibited and unsupported use

- Do not upload traces to public GitHub issues or pull requests.
- Do not bind or proxy the analyzer to a network interface.
- Do not deploy it as a shared, multi-user, hosted, or Windows service.
- Do not add unsupported plug-ins, scripts, or telemetry collectors.
- Do not treat generated findings as definitive without reviewing the source
  evidence.
- Do not use an unsupported release when a security replacement is available.

## Unexpected behavior

Stop using the application and report through the approved security process if:

- It listens on a non-loopback address.
- It sends trace-derived data to an unexpected destination.
- Trace content appears in telemetry or logs.
- Temporary trace data is retained unexpectedly.
- An archive escapes configured paths or resource limits.
- The downloaded package, checksum, SBOM, or attestation cannot be verified.

Public vulnerability-reporting instructions are in
[SECURITY.md](../SECURITY.md). Do not include real traces, credentials, or
customer data in the report.
