# Contributing

Contributions that improve trace compatibility, diagnostic accuracy,
performance, accessibility, security, privacy, tests, or documentation are
welcome.

Review the [architecture](docs/ARCHITECTURE.md) before changing import,
analysis, storage, network, or rendering behavior. Release maintainers should
also follow the [release process](docs/RELEASING.md).

Contributions are accepted under the
[Apache License 2.0](LICENSE). Unless explicitly stated otherwise, an
intentionally submitted contribution is provided under that license.

## Protect diagnostic data

Do not commit or submit:

- Customer or production HAR and SAZ files.
- Real URLs, hostnames, tenant identifiers, mailbox identifiers, user names,
  email addresses, cookies, tokens, credentials, or message content.
- Screenshots or logs containing diagnostic data.
- Passwords used to test encrypted archives.
- Internal service endpoints, security-review records, or non-public policy
  material.

Tests and issue reports must use generated fixtures or appropriately sanitized
examples that cannot be associated with a person, customer, tenant, device, or
production environment.

Suspected vulnerabilities must be reported privately according to
[SECURITY.md](SECURITY.md), not through a public issue or pull request.

## Development requirements

- Preserve bounded parsing for untrusted input.
- Keep trace processing local unless a design change has been explicitly
  reviewed.
- Do not add trace-derived fields to logs or telemetry.
- Do not persist archive passwords.
- Keep HTML previews sandboxed and block remote resource loading.
- Keep XML DTD processing and external entity resolution disabled.
- Fail closed when consent, configuration, or input validity cannot be
  established.
- Add regression tests for new formats, parsing behavior, limits, and error
  handling.
- Prefer explicit errors over silently accepting malformed or unsupported
  content.

## Changes requiring security review

Request review from the appropriate security teams before release when a change:

- Adds or changes data collection, storage, retention, or deletion.
- Adds a network destination or changes transmitted fields.
- Adds authentication, authorization, remote access, or multi-user behavior.
- Adds telemetry or changes its schema or consent behavior.
- Adds a parser, active-content renderer, hosted service, AI integration, or
  cloud storage.
- Changes archive extraction, encryption, file limits, temporary-file handling,
  update behavior, or release integrity controls.
- Introduces a dependency that processes untrusted trace content or communicates
  over the network.

Update [Security and privacy](docs/SECURITY-AND-PRIVACY.md) and the
[public threat model](docs/THREAT-MODEL.md) when observable behavior or trust
boundaries change.

## Build and test

The repository requires the .NET SDK version declared in `global.json`.

```powershell
dotnet restore .\M365-Trace-Analyzer.sln
dotnet format .\M365-Trace-Analyzer.sln --verify-no-changes --no-restore
dotnet build .\M365-Trace-Analyzer.sln --configuration Release --no-restore -warnaserror
dotnet test .\M365-Trace-Analyzer.sln --configuration Release --no-build
```

Run the smallest relevant tests while developing, then run the complete
validation before requesting review.

## Dependencies

- Use the existing package ecosystem and repository tooling.
- Add a dependency only when the capability is necessary and cannot be
  reasonably implemented with an existing dependency or platform API.
- Document why a new dependency is needed.
- Review its maintenance status, license, transitive dependencies, and known
  vulnerabilities.
- Commit dependency manifest and lock-file changes together when applicable.

## Pull requests

Describe:

- The user-visible behavior being changed.
- Security and privacy effects.
- New or changed network, storage, telemetry, or parsing behavior.
- Tests added or run.
- Documentation updated.

Do not state that a change is secure, compliant, certified, or approved unless
the statement has a defined scope and documented authorization.
