# Architecture

M365 Trace Analyzer is a local ASP.NET Core and Blazor application. The browser
provides the user interface while parsing, analysis, query, and summary services
run in the local application process.

## Operating model

1. The executable starts Kestrel on the selected port.
2. Kestrel is programmatically restricted to the local computer's IPv4 and IPv6
   loopback interfaces.
3. The application opens its local URL in the default browser on Windows.
4. The browser streams a selected HAR or SAZ file to the local process.
5. An importer validates and normalizes the file into `TraceSession` records.
6. The analysis engine evaluates each normalized session with the ordered rule
   catalog.
7. Query and summary services prepare searchable sessions, findings, metrics,
   and drill-through results.
8. Blazor renders the investigation workspace in the locally connected browser.

Trace contents are not sent to a hosted analysis service. See
[Security and privacy](SECURITY-AND-PRIVACY.md) for storage and network details
and the [public threat model](THREAT-MODEL.md) for trust boundaries.

## Solution projects

| Project | Responsibility |
| --- | --- |
| `M365Trace.Core` | Normalized trace models, metadata, content, and import-result contracts |
| `M365Trace.Import.Har` | Bounded HAR parsing, validation, metadata normalization, and quality reporting |
| `M365Trace.Import.Saz` | Bounded SAZ/ZIP handling, raw HTTP decoding, encryption support, metadata normalization, and quality reporting |
| `M365Trace.Rules` | Rule contracts, catalog validation, analysis engine, classifications, findings, and recommendations |
| `M365Trace.Web` | Local server, Blazor interface, query and summary services, update checking, telemetry consent, and application lifecycle |
| `M365Trace.Benchmarks` | Generated large-trace performance and memory benchmarks |
| `M365Trace.Core.Tests` | Core model and compatibility tests |
| `M365Trace.Import.Har.Tests` | HAR importer regression, fidelity, limit, and malformed-input tests |
| `M365Trace.Import.Saz.Tests` | SAZ importer, archive safety, compression, decoding, and encryption tests |
| `M365Trace.Rules.Tests` | Deterministic rule and catalog regression tests |
| `M365Trace.Web.Tests` | Service, component, query, telemetry, port, and browser-launch tests |

The packaged Chromium workflow under `tests\M365Trace.Web.E2E.Tests` builds
separately and exercises the published Windows executable.

## Import pipeline

Both importers implement `ITraceImporter` and produce a `TraceImportResult`
containing normalized sessions and an import-quality report.

### HAR

HAR imports retain structured protocol, size, endpoint, connection, redirect,
cache, query, cookie, page, timing, and content-completeness metadata when
present.

The importer enforces limits on:

- Input file size.
- Entry count.
- Retained body text.
- Metadata item count.

### SAZ

SAZ imports retain protocol, endpoint, process, connection, TLS, timing, and
content-completeness metadata. Raw HTTP bodies are decoded in wire order,
including chunked and compressed content.

Encrypted archives support ZipCrypto, AES-128, and AES-256. The importer uses a
uniquely named temporary file and enforces limits on:

- Compressed file size.
- Archive entry count.
- Total expanded size.
- Individual entry size.
- Retained body text.
- Archive paths.

## Rules architecture

Concrete `ITraceRule` implementations are discovered at startup and loaded into
a validated immutable `RuleCatalog`. Duplicate or malformed rule identifiers
prevent startup rather than failing during an investigation.

Each session receives a cached `SessionFacts` view containing normalized URL,
host, path, headers, content type, and searchable request and response text.
Rules use this shared representation instead of repeatedly rebuilding values.

Rules execute in explicit phases and deterministic order. Specialized rules
take precedence over generic fallbacks, and confidence values prevent weaker
classifications from replacing stronger results.

The pipeline includes:

- Standard HTTP response and unknown-status classification.
- Specialized HTTP 0, 200, 302, 307, 400, 401, 403, 404, 456, 500, 502, 503,
  and 504 analysis.
- Errors returned inside successful HTTP 200 responses.
- Exchange, Autodiscover, MAPI, EWS, OWA, RPC, NSPI, REST People, suggestions,
  attachment, and Free/Busy conditions.
- Bearer, basic, SAML, and missing-authentication classification.
- Response-server, Apache Autodiscover, loopback, and NetLog checks.
- Duration-based warning and severe findings.

See [Classification coverage](../CLASSIFICATION-COVERAGE.md) for the full
classification and implementation inventory.

## Presentation safety

- HTML bodies render in a sandboxed iframe with a restrictive content security
  policy and no-referrer policy.
- HTML scripts and remote resources are blocked.
- XML parsing prohibits DTD processing and external entity resolution.
- Supported raster images render from local data URLs; SVG previews are
  excluded.
- Body and metadata retention limits prevent unbounded display content.

## External communication

The application has two documented outbound behaviors:

- One process-cached request to the public GitHub Releases API for update
  availability.
- Explicitly opted-in, allowlisted anonymous usage telemetry in configured
  builds.

Neither channel intentionally includes trace content. See
[Anonymous usage telemetry](USAGE-TELEMETRY.md) for the event schema.
