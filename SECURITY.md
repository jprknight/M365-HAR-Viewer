# Security policy

## Supported versions

Security fixes are applied to the latest published release and the current
development branch. Older releases may no longer receive fixes.

| Version | Supported |
| --- | --- |
| Latest published release | Yes |
| Current development branch | Yes |
| Older releases | No |

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
implemented mitigations, and residual risks.
