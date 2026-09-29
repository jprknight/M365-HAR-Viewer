# Security policy

## Supported versions

Security fixes are applied to the latest published release and the current
development branch. Older releases are unsupported and their binary assets may
be withdrawn when a security replacement is available.

| Version | Supported |
| --- | --- |
| Latest published release | Yes |
| Current development branch | Yes |
| Older releases | No |

Users should upgrade to the latest supported release before reporting a
problem. Historical tags and release notes may remain available for audit and
source-history purposes even when obsolete binary assets are removed.

## Report a vulnerability

Do not report suspected vulnerabilities through a public issue, discussion,
pull request, or trace attachment.

Use the repository's **Security** tab and select **Report a vulnerability** to
send a private report. If private vulnerability reporting is unavailable, open
a public issue containing only a request for a private contact channel. Do not
include vulnerability details, proof-of-concept material, credentials, or
diagnostic data in that issue.

Include the following when it is safe to do so:

- A concise description of the vulnerability and its potential impact.
- The affected version, operating system, and deployment configuration.
- Reproduction steps using synthetic or sanitized data.
- Relevant logs with secrets, personal data, tenant information, URLs, headers,
  and trace contents removed.
- Any mitigations already identified.

Reports are reviewed and prioritized based on reproducibility, impact, and
affected configurations. Resolution details may be withheld until a fix or
mitigation is available.

## Severity and response targets

The project uses the following best-effort targets. They are goals rather than a
service-level agreement and can change based on reproducibility, affected
configurations, dependency ownership, and coordinated-disclosure needs.

| Severity | Example impact | Acknowledgement target | Mitigation or remediation target |
| --- | --- | --- | --- |
| Critical | Likely unauthorized disclosure of trace data or remote code execution in a supported configuration | 2 business days | 14 calendar days |
| High | Significant confidentiality, integrity, or local-code-execution impact requiring user interaction | 3 business days | 30 calendar days |
| Medium | Limited impact, strong prerequisites, or meaningful defense-in-depth issue | 5 business days | 90 calendar days |
| Low | Minimal direct impact or security-hardening improvement | 10 business days | A future planned release |

Severity considers exploitability, affected data, required privileges, user
interaction, deployment assumptions, and the supported local-only operating
model.

## Coordinated disclosure

- Allow time for investigation and mitigation before public disclosure.
- Do not publish proof-of-concept material for an unresolved issue.
- The project may request additional reproduction details using synthetic data.
- Reporter credit is available when requested and when disclosure is
  coordinated.
- A security replacement can include a GitHub Security Advisory, release notes,
  removal of superseded binary assets, and updated operating guidance.

## Security incident handling

When a report is validated, maintainers will:

1. Record the affected versions and supported configurations.
2. Assess whether trace confidentiality, integrity, or local execution is at
   risk.
3. Identify mitigations and affected users.
4. Prepare a fix or operational restriction.
5. Re-run security-relevant tests and release evidence.
6. Publish a replacement release or advisory when appropriate.
7. Withdraw unsupported binary assets when continued distribution creates
   unnecessary risk.
8. Update the threat model, assurance matrix, and operating guidance.

## Sensitive diagnostic data

HAR and SAZ files can contain authentication material, personal data, tenant
identifiers, URLs, headers, request and response bodies, and other sensitive
information. Never submit real diagnostic files or generated findings to the
public repository.

Use generated fixtures or sanitized examples that cannot be associated with a
person, customer, tenant, device, or production environment.

## Security documentation

See [Security and privacy](docs/SECURITY-AND-PRIVACY.md) for the application's
data flow, network behavior, storage behavior, and user responsibilities. See
the [public threat model](docs/THREAT-MODEL.md) for major trust boundaries,
implemented mitigations, and residual risks. The
[security assurance matrix](docs/SECURITY-ASSURANCE.md) maps controls to
implementation and evidence.
