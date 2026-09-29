# Security assurance

## Document control

| Field | Value |
| --- | --- |
| Owner | Repository maintainers, with review by the appropriate security teams |
| Baseline date | 2026-09-29 |
| Review cadence | At least annually and whenever a review trigger occurs |
| Product scope | Standalone Windows x64 M365 Trace Analyzer application |
| Current status | Repository security baseline implemented; independent assessment pending |

This document maps security requirements to implementation and evidence. It is
not a certification, compliance statement, or authorization to process a
particular category of data.

## Status definitions

- **Verified**: implemented and covered by repeatable repository evidence.
- **Implemented**: present in the product or process but requires additional
  operational or independent verification.
- **Operational evidence required**: depends on the deployment environment and
  cannot be proven by the public repository alone.
- **Planned**: recognized gap that has not yet been implemented.
- **Not applicable**: reviewed and determined not to apply to the current
  operating model.

## Control matrix

| ID | Requirement | Implementation | Evidence | Status | Review trigger |
| --- | --- | --- | --- | --- | --- |
| GOV-01 | Changes to `master` must follow controlled review | Repository ruleset requires pull requests, required checks, resolved review threads, linear history, and blocks deletion and force pushes | GitHub repository ruleset and CI/Security workflows | Verified | Repository governance change |
| GOV-02 | Security-sensitive paths must have defined ownership | `.github/CODEOWNERS` assigns repository and security-sensitive path ownership | `.github/CODEOWNERS` | Implemented | Ownership change |
| VULN-01 | Vulnerabilities must have a private reporting path | GitHub private vulnerability reporting and `SECURITY.md` | Repository security settings and `SECURITY.md` | Verified | Reporting-process change |
| VULN-02 | Supported versions and response handling must be public | Latest release and current development branch policy, severity definitions, response targets, and withdrawal process | `SECURITY.md` | Implemented | Support-policy change |
| NET-01 | The application must remain local-only | Kestrel uses `ListenLocalhost`; `--urls` is rejected; Host values are restricted | `Program.cs`, `LocalServerOptions.cs`, packaged listener tests | Verified | Listener or hosting change |
| NET-02 | Outbound destinations must be enumerated | Only the GitHub release check and explicitly opted-in telemetry are documented | `SECURITY-AND-PRIVACY.md`, `DATA-HANDLING.md`, `USAGE-TELEMETRY.md` | Verified | New destination or protocol |
| DATA-01 | Trace contents must remain local | Parsing, analysis, search, and rendering run in the local process | Architecture and data-handling documentation; packaged workflow | Verified | Hosted processing or storage change |
| DATA-02 | Temporary support data must be minimized and deleted | HAR is streamed; SAZ uses a unique temporary file deleted in a `finally` block | `SazTraceImporter.cs`, importer tests, `DATA-HANDLING.md` | Verified | Import implementation change |
| DATA-03 | Passwords must not be persisted or reported | Password is scoped to the active import and excluded from telemetry | Import and component tests; telemetry schema tests | Verified | Encryption or telemetry change |
| DATA-04 | Operational telemetry retention and access must be governed | Dedicated Application Insights resource with restricted access and documented safeguards | Deployment configuration and access-review evidence | Operational evidence required | Telemetry resource or audience change |
| INPUT-01 | Untrusted HAR input must be bounded | File-size, entry-count, text, metadata, JSON-depth, and cancellation controls | HAR importer tests | Verified | Parser or limit change |
| INPUT-02 | Untrusted SAZ input must be bounded | Compressed-size, expanded-size, entry-count, entry-size, path, text, and cancellation controls | SAZ importer tests | Verified | Archive or limit change |
| RENDER-01 | Trace-derived HTML must not execute active content | Sandboxed iframe, restrictive content security policy, no-referrer policy, remote resources blocked | `BodyInspector.razor`, component tests | Verified | Preview behavior change |
| RENDER-02 | XML must not resolve external entities | DTD processing prohibited and external resolution disabled | Importer and component implementation; component tests | Verified | XML parser change |
| TEL-01 | Telemetry must be opt-in and use a fixed schema | Consent defaults disabled; reviewed allowlist; coarse buckets; no exception messages | Telemetry unit, component, configuration, and packaged tests | Verified | Schema or consent change |
| TEL-02 | Telemetry must not be treated as an audit log | Connection string is a routing identifier and events can be spoofed | `USAGE-TELEMETRY.md`, workbook annotation | Implemented | Telemetry architecture change |
| DEP-01 | Dependency changes must be reviewed and monitored | Dependabot version and security updates, dependency review, NuGet vulnerability scan | Dependabot configuration and CI/Security workflows | Verified | Dependency-process change |
| SAST-01 | Source must receive automated security analysis | CodeQL security-extended queries on pull requests, `master`, and schedule | Security workflow | Verified | Workflow or language change |
| REL-01 | Release artifacts must be attributable and integrity protected | SHA-256 checksum and GitHub artifact attestation | Release workflow and GitHub attestation records | Verified | Packaging change |
| REL-02 | Releases must include a machine-readable component inventory | SPDX 2.2 SBOM generated from the final publish directory, attached to the release, and included in an SBOM attestation | Release workflow and SBOM generation script; release assets and attestation required for operational verification | Implemented | SBOM tool or packaging change |
| REL-03 | Obsolete security-sensitive binaries must be withdrawable | Unsupported release assets are removed while release pages and tags remain for history | `SECURITY.md`, `RELEASING.md`, GitHub release history | Implemented | Security release or support-window change |
| REL-04 | Windows executable publisher identity must be verifiable | Authenticode signing | No implementation evidence | Planned | Signing implementation |
| IR-01 | Security incidents must have a defined response process | Private intake, triage, severity, communication, release withdrawal, and disclosure guidance | `SECURITY.md` | Implemented | Response-process change |
| USE-01 | Support engineers must have a safe operating procedure | Download, verification, handling, cleanup, sharing, and incident guidance | `SUPPORT-ENGINEER-OPERATING-GUIDE.md` | Implemented | User workflow change |

## Evidence package

For an assessment, provide:

- The commit SHA and released version being assessed.
- Successful CI and Security workflow URLs for that commit.
- The release ZIP SHA-256 value.
- Build provenance and SBOM attestation records.
- The release SPDX SBOM.
- CodeQL and dependency-review results.
- NuGet vulnerability-scan output.
- Packaged listener and browser-workflow results.
- Telemetry resource configuration, retention, region, role assignments, and
  access-review evidence when telemetry is enabled.
- Any independent penetration-test or application-risk-assessment report.
- Recorded risk decisions for controls marked **Planned** or
  **Operational evidence required**.

Do not commit confidential assessment reports, customer data, internal ticket
identifiers, reviewer identities, or non-public infrastructure details to the
public repository.

## Review triggers

Review this matrix when:

- A threat-model review trigger occurs.
- A control changes implementation or evidence location.
- A new release pipeline, signing mechanism, telemetry destination, parser, or
  storage location is introduced.
- A vulnerability demonstrates that a control is incomplete.
- An assessment changes a control's status or identifies a new gap.
