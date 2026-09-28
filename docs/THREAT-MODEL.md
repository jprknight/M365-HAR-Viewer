# Public threat model

## Scope

This threat model covers the standalone M365 Trace Analyzer application, its
HAR and SAZ importers, browser interface, optional usage telemetry, release
update check, build pipeline, and published Windows package.

The primary deployment is a single-user application running on a trusted
Windows computer and listening only on the loopback interface.

## Security objectives

- Keep diagnostic trace contents on the local computer.
- Prevent trace-derived content from executing active code or loading remote
  resources in previews.
- Bound resource use when parsing large, malformed, or hostile files.
- Avoid collecting trace-derived values in usage telemetry.
- Avoid persisting archive passwords.
- Produce reproducible, testable, and attributable release artifacts.

## Assets

- HAR and SAZ source files.
- Request and response URLs, headers, bodies, cookies, and authentication
  material contained in traces.
- Analysis findings and normalized session data.
- Passwords supplied for encrypted SAZ archives.
- Telemetry consent and anonymous installation identifier.
- Source code, dependencies, build workflows, and release artifacts.

## Trust boundaries

1. **Selected trace file to local parser:** HAR and SAZ files are untrusted
   input, even when supplied by an authorized user.
2. **Local process to browser:** The ASP.NET Core process sends trace-derived
   content to the locally connected browser interface.
3. **HTML or image content to preview:** Trace bodies can contain attacker-
   controlled markup or binary data.
4. **Application to GitHub:** The release checker makes an outbound HTTPS
   request to the public GitHub API.
5. **Application to telemetry endpoint:** Allowlisted usage events are sent only
   after explicit opt-in when telemetry is configured.
6. **Source repository to release artifact:** Dependencies and CI workflows
   participate in producing the published executable and archive.
7. **Local application listener:** The selected port is reachable only through
   the local computer's IPv4 and IPv6 loopback interfaces.

## Threats and mitigations

| Threat | Current mitigations |
| --- | --- |
| Malformed or oversized HAR input exhausts memory or CPU | File-size, entry-count, retained-text, and metadata limits; bounded JSON parsing; cancellable imports |
| Archive bomb or oversized SAZ input exhausts disk, memory, or CPU | Compressed-size, expanded-size, entry-count, per-entry, and retained-text limits |
| Archive path traversal writes outside the intended location | Archive paths are validated; entries are read rather than extracted to caller-controlled paths |
| Malicious HTML executes script or loads remote content | HTML is rendered in a sandboxed iframe with a restrictive content security policy and `no-referrer` policy |
| XML external entity processing reads local or remote resources | DTD processing is prohibited and external resolution is disabled |
| Trace data is disclosed through telemetry | Telemetry uses a fixed allowlist of coarse values and excludes trace-derived strings; consent is explicit; offline exporter storage is disabled |
| Archive password is disclosed through storage or telemetry | Passwords are used only for the active import and are not written to settings or telemetry |
| Temporary SAZ copy remains on disk | A unique temporary name is used and deletion runs after each import attempt |
| Application becomes reachable from another device | Kestrel is configured programmatically with a loopback-only endpoint; URL configuration cannot broaden the listener; allowed Host values are restricted to loopback hosts |
| Update checking blocks core functionality | The request has a timeout, is cached once per process, and fails without disabling trace analysis |
| Vulnerable dependency enters the build | CI restores from declared manifests and runs a vulnerable-package check; automated dependency updates are configured |
| Published package is replaced or modified | Release automation publishes a SHA-256 checksum and generates artifact attestation |
| Sensitive data is exposed through public collaboration | Repository policy prohibits real traces and requires synthetic or sanitized fixtures and reports |

## Residual risks

- A hard process or operating-system termination can leave a temporary SAZ copy
  in the operating system's temporary directory.
- Files within allowed limits can still consume substantial memory and CPU.
- The application cannot determine whether the user is authorized to analyze a
  selected trace.
- Displayed findings can contain sensitive values inherited from the source
  trace and must be reviewed before sharing.
- A desktop telemetry connection string is not a secret and telemetry events
  can be spoofed; telemetry must not be treated as an authoritative audit log.
- Dependency scanning reduces supply-chain risk but cannot prove that every
  dependency or build component is free of vulnerabilities.

## Review triggers

The threat model must be reviewed when a change:

- Adds a trace format, parser, body renderer, or active-content preview.
- Adds authentication, authorization, remote access, or multi-user behavior.
- Persists trace data or findings outside process memory.
- Adds or changes a network destination.
- Changes telemetry fields, consent behavior, or settings storage.
- Changes file limits, archive handling, encryption handling, or temporary-file
  behavior.
- Adds a hosted service, AI integration, browser extension, or cloud storage.
- Changes release signing, checksums, attestations, or update behavior.

Material changes require review by the appropriate security teams before
release.
