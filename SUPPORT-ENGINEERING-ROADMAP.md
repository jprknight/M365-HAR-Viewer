# M365 Trace Analyzer support-engineering roadmap

## Problem and approach

M365 Trace Analyzer already provides a strong local foundation: bounded HAR and SAZ import, encrypted SAZ support, 151 migrated classifications, session filtering and sorting, request/response inspection, secure body previews, update checking, and a self-contained local web experience.

The remaining gaps are less about basic file viewing and more about helping a support engineer move efficiently from a large trace to a defensible diagnosis. The roadmap should therefore prioritize trace-wide triage, richer source fidelity, repeatable investigation workflows, and performance before adding optional automation surfaces.

## Current-state assessment

### Strong existing capabilities

- Local-only HAR and SAZ analysis with no trace upload.
- Bounded parsing and archive safety limits.
- ZipCrypto, AES-128, and AES-256 password-protected SAZ support.
- Deterministic per-session analysis with 151 classifications.
- Search across URLs, statuses, classifications, findings, evidence, and recommendations.
- Sortable session grid with request, response, or combined detail views.
- Raw, JSON, HTML, image, and XML body inspectors.
- Responsive viewport layout and accessible controls.
- Unified application versioning and release checking.

### Original highest-value gaps and current status

1. **Trace-wide triage is delivered.** The collapsible summary covers severe findings, failing hosts, status distribution, slowest sessions, authentication patterns, and high-impact rules, with drill-through into matching sessions.
2. **Composable filtering and content search are delivered.** Debounced global search covers session metadata, findings, request and response headers, and retained decoded body text. It combines with structured filters for severity, status family or exact code, method, host, duration, session type, authentication, finding rule, and finding presence.
3. **Source metadata fidelity is delivered.** The normalized model and HAR/SAZ importers retain protocol, size, endpoint, process, connection, TLS, redirect, cache, page, detailed timing, and completeness metadata when the source provides it.
4. **Full-trace timing visualization is out of scope.** A timeline/waterfall was evaluated and removed because it did not improve the session-list and request/response troubleshooting workflow enough to justify its UI and complexity.
5. **Large-trace behavior is measured and controllable.** Benchmarks and budgets cover 1,000-100,000-session HAR and SAZ traces. Imports expose transient phase status, support cancellation through analysis and aggregation, reject stale results, and virtualize the session grid without adding paging controls.
6. **Import diagnostics are delivered.** Recoverable sessions remain available and an import-quality panel reports skipped, truncated, partial, unsupported, invalid, and missing-response conditions.
7. **Broad automatic correlation is out of scope.** A proof of concept grouped identifiers, redirects, authentication sequences, retries, and repeated failures, but normal Outlook traffic triggered the panel frequently and created noise rather than actionable troubleshooting value. Diagnostic headers remain available for focused manual investigation.
8. **Investigation-state controls are out of scope.** A bookmarks, notes, and marked-session prototype was removed because it bloated the session-first workspace without enough troubleshooting value.
9. **Current UI behavior has component and browser coverage.** Component tests cover import success and recovery, import-quality warnings, encrypted-SAZ password handling, global search and structured filtering, active chips, summary drill-through, selection recovery, keyboard accessibility semantics, diagnostic headers, every sortable column, finding rendering, request/response view modes, update states, and body inspectors. A packaged Chromium smoke test covers HAR, unencrypted SAZ, and AES-256 encrypted SAZ imports; password retry; content search; filter clearing; keyboard navigation; focus movement; diagnostic headers; detail selection; and warning-panel layout.

## Product decisions

- Deliver this as a **prioritized roadmap**, not one monolithic implementation.
- Data and report export are out of scope; the analyzer remains an interactive local investigation tool.
- Keep core models and analysis services suitable for a future MCP/API adapter, but **do not implement MCP or an external API in this roadmap**.
- Define and benchmark both ordinary traces and large traces because typical field size is not yet known.
- Do not add request replay or transmission features; the analyzer remains a passive, local diagnostic tool.

## Roadmap

### Implementation status — September 27, 2026

Status markers in this document apply only where the complete listed outcome has been delivered:

- `[x]` Completed and merged into `master`.
- `[ ]` Planned or only partially implemented. Partial coverage is described inline.

Phases 1 and 2 are complete. Phase 3 concepts were evaluated and removed as out of scope. Phase 4 is underway:

- Release `v1.0.2` delivered the encrypted SAZ, UI, port, classification, and unified-versioning work that formed the roadmap baseline.
- CI now validates formatting, warning-free builds, 209 solution tests, a 40% aggregate coverage floor, vulnerable NuGet packages, and a self-contained Windows package.
- A packaged Chromium test validates application startup; HAR, unencrypted SAZ, and AES-256 encrypted SAZ uploads; encrypted-archive password retry; filtering; filter clearing; diagnostic headers; request/response detail selection; and import-warning layout.
- CodeQL, Dependabot, tag-driven release packaging, checksums, and artifact provenance are configured.
- Importer boundary, malformed-input, cancellation, archive safety, component, secure body-preview, and packaged-browser tests have been expanded.
- XML display now handles diagnostic responses containing prohibited numeric character references without altering Raw content.
- JSON display now handles a leading UTF-8 BOM without altering Raw content.
- Phase 1 foundation now separates immutable session query state, filtering, sorting, and selection from `Home.razor` into unit-tested services.
- The Fiddler-style workspace presentation is split into focused `SessionTable` and `SessionDetailPanel` components while `Home.razor` remains the import and composition root.
- A collapsible trace summary now reports trace timing, severity and HTTP status distributions, findings, slow sessions, failing hosts, high-impact rule IDs, slowest sessions, and authentication classifications.
- Structured filters now combine with free text using explicit AND/OR semantics, display removable chips and visible counts, reconcile selection when results change, and support summary-driven drill-through.
- The detail workspace elevates recognized diagnostic headers while retaining the complete request and response header lists.
- HAR and SAZ importers retain richer normalized metadata and recover usable sessions with explicit import-quality reporting.
- A repeatable benchmark harness now measures 1,000, 10,000, and 100,000-session HAR and SAZ workflows against versioned performance and managed-memory budgets.
- Long-running imports now expose transient phase status, support cancellation through import, analysis, and summary aggregation, and prevent stale operations from replacing newer selections.
- The session grid now virtualizes large result sets while preserving table semantics, filtering, sorting, selection, and keyboard navigation across off-screen rows. Focused 100,000-session validation reduced peak managed memory from approximately 1.2-1.5 GiB to 596-810 MiB.

### Phase 1 — Support-engineer triage essentials

## Phase 1 detailed design

Phase 1 is intended to make the existing analyzer substantially more useful without changing how HAR or SAZ files are parsed. It operates on the normalized sessions and analysis results already produced today. This keeps the first phase lower risk: importer fidelity and timeline work remain in later phases.

### Expected support-engineer workflow

1. The engineer opens a HAR or SAZ exactly as they do today.
2. The application presents a compact trace summary above or alongside the session workspace.
3. The engineer immediately sees whether the trace contains severe findings, HTTP failures, authentication issues, repeated classifications, or unusually slow calls.
4. Selecting a summary item applies a visible structured filter to the existing session grid.
5. The engineer combines summary-driven filters with global session-content search and sorting.
6. The engineer selects relevant sessions and inspects their findings, headers, and bodies in the detail workspace.

### Proposed screen changes

#### Trace summary panel

Add a collapsible summary region between the application header and the current two-panel workspace. It should avoid permanently reducing the session grid height when the engineer does not need it.

The first version should contain:

- Total sessions and currently visible sessions.
- Trace start, trace end, and elapsed trace window.
- Counts for Severe, Concerning, Warning, Normal, and other analysis states.
- HTTP status-family counts: no response/0, 2xx, 3xx, 4xx, and 5xx.
- Number of sessions with findings.
- Number of slow sessions using the existing rule thresholds.
- Top failing hosts.
- Most frequent concerning/severe rule IDs.
- Top slowest sessions.
- Authentication classification counts.

Each count or row should be a button or link that applies the corresponding filter. For example, selecting `5xx · 42` filters the grid to 5xx sessions; selecting a host filters to that exact host.

The summary should not claim a root cause. It summarizes observed evidence and existing rule results.

#### Filter bar

Keep the global session search box and clear `×` control. Search covers URLs, methods, statuses, analysis findings, request and response headers, and retained decoded body text. Add a structured filter bar with:

- Severity multi-select.
- Status family and optional exact status code.
- Method.
- Host.
- Duration preset/range.
- Session type.
- Authentication classification.
- Has findings / No findings.

Applied filters appear as individual chips, for example:

```text
[Severity: Concerning + Severe ×] [Status: 5xx ×] [Host: outlook.office365.com ×]
```

The filter bar also shows:

```text
42 of 8,316 sessions
```

A `Clear all` action removes structured filters and free text. Clicking a summary metric replaces or augments the relevant filter category rather than building hidden state.

#### Diagnostic headers

Add a focused diagnostic-header section above the full header lists when any recognized values are present. Initial recognized headers should include:

- `request-id`
- `client-request-id`
- `x-ms-request-id`
- `x-ms-correlation-id`
- `x-feserver`
- `x-beserver`
- `x-calculatedbetarget`
- `x-diaginfo`
- `x-ms-diagnostics`
- `retry-after`
- `location`
- `www-authenticate`

Header names remain case-insensitive. This section is only a convenience view; the complete headers remain available below it.

### Engineering changes

#### Separate page state from presentation

`Home.razor` currently handles update checks, file selection, password retry, import, analysis, filtering, sorting, session selection, and most rendering. Phase 1 should avoid adding all new logic directly to this component.

Introduce:

- `TraceWorkspaceState` — loaded file identity, analyzed sessions, selected session, sort state, and active filters.
- `SessionFilter` or `SessionQuery` — immutable structured filter values plus free text.
- `SessionQueryService` — filtering, sorting, and visible counts.
- `TraceSummaryService` — aggregate counts and ranked summary groups.
- `TraceSummary` — immutable result model consumed by the UI.

The exact namespaces may follow the existing project conventions, but filtering and aggregation must be unit-testable without rendering Blazor components.

#### Suggested UI component split

Refactor the main page into focused components:

- `TraceSummaryPanel.razor`
- `TraceFilterBar.razor`
- `SessionTable.razor`
- `SessionDetailPanel.razor`
- `DiagnosticHeaders.razor`

`Home.razor` remains the composition root for file opening and top-level workspace state. This split is not cosmetic; it prevents Phase 1 from making an already large page difficult to test and maintain.

#### Summary calculations

Summary calculations should be deterministic and computed from the analyzed session collection:

- Severity and finding counts come from `TraceAnalysisResult`.
- Status family comes from `StatusCode`.
- Host, method, authentication, session type, and response server come from existing fields.
- Slowest sessions use `Duration`.
- Trace window uses minimum `StartedAt` and maximum `StartedAt + Duration`.
- Repeated finding groups use stable `RuleId`, not localized titles.

The summary is recomputed after a trace is loaded. Lightweight visible-count information may update after filtering, but the original trace-wide totals must remain distinguishable from filtered totals.

#### Structured filtering behavior

Filter categories combine with logical AND. Multiple values inside one category combine with logical OR.

Example:

```text
(Severity is Severe OR Concerning)
AND (Status is 500 OR 503)
AND (Host is outlook.office365.com)
AND free text contains "authentication"
```

Filtering remains case-insensitive. Exact host and rule selections from the summary should not use substring matching. Sorting remains independent of filtering.

If the selected session is removed from the visible result:

- Select the next visible session by original trace order.
- If none follows, select the previous visible session.
- If no sessions remain, show a clear empty-filter result rather than the initial open-file state.

### Phase 1 implementation sequence

1. [x] Extract and test the session query/filter/sort behavior from `Home.razor`.
2. [x] Introduce workspace state and split the large page into focused components without intentionally changing behavior.
3. [x] Add the trace summary service and summary panel.
4. [x] Add structured filters, active chips, counts, and summary drill-through.
5. [x] Add diagnostic-header emphasis.
6. [x] Add keyboard navigation, accessibility coverage, and component/browser-level tests for the complete Phase 1 workflow.

### Phase 1 acceptance criteria

- Opening existing HAR and SAZ fixtures produces the same session count and per-session findings as before.
- A support engineer can identify all Severe and Concerning sessions without writing free-text queries.
- Every trace summary metric that represents sessions can drill into the exact matching session set.
- Filter chips accurately describe all active structured filters.
- Global session search and structured filters combine predictably.
- Existing importer safety limits and body-preview protections remain unchanged.
- Targeted unit, component, and browser smoke tests pass.

### Phase 1 explicitly does not include

- New HAR or SAZ metadata parsing.
- Waterfall/timeline visualization.
- Cross-session root-cause or retry-chain analysis.
- Persistent notes or sidecar project files.
- Data export or report generation.
- MCP server, HTTP API, or cloud service.
- Request replay, live capture, or outbound diagnostic actions.
- Performance optimizations chosen without benchmark evidence.

#### 1. Add a trace-wide analysis summary

- Introduce a trace summary service over analyzed sessions rather than placing aggregation logic in the Razor page.
- Show counts by severity, status family, host, method, authentication type, response server, and session type.
- Highlight top severe/concerning findings, repeated rule IDs, failing hosts, and slowest sessions.
- Make every summary metric actionable by applying the corresponding session filter.
- Preserve per-session analysis as the source of truth; aggregate findings must link back to matching sessions.

#### 2. Replace text-only filtering with composable filters

- Retain global session search and its clear control.
- Add multi-select severity filtering and common quick filters such as Errors, Warnings+, Slow, Authentication, and Has findings.
- Add structured filters for status family/code, method, host, duration range, session type, and authentication classification.
- Display active filters as removable chips with a single Clear all action.
- Show visible-session count versus total-session count.
- Keep filtering and sorting in a dedicated testable query model/service.

#### 3. Add investigation ergonomics

- Elevate commonly useful diagnostic headers such as request IDs, correlation IDs, diagnostic headers, retry headers, authentication challenges, and redirect locations.
- Add keyboard navigation for the session grid and detail panel.
- Preserve the selected session where possible when filters or sorting change.

### Phase 2 — Import fidelity and explainability

#### 5. [x] Expand the normalized trace model

- Add optional fields for HTTP protocol/version, request and response sizes, client/server endpoint, process information, connection ID, TLS protocol/cipher/certificate metadata, redirect target, cache state, and source-specific metadata.
- Add a structured timing model covering blocked/queued, DNS, connect, TLS, send, wait, receive, and total duration.
- Keep all new fields optional so HAR and SAZ can expose only what each source actually contains.
- Add source provenance to each field or metadata group where ambiguity would otherwise lead engineers to assume inferred values.
- Avoid storing arbitrary unbounded metadata dictionaries in the UI-facing model; explicitly map supported diagnostic fields.

#### 6. [x] Improve HAR fidelity

- Parse HAR HTTP version, headers/body sizes, cookies, query-string entries, cache information, redirect URL, server IP, connection ID, and detailed timings.
- Parse page references and page timing where present so sessions can be grouped by navigation.
- Preserve unknown/unsupported HAR fields only when required for future compatibility, without weakening bounds.
- Record whether body content was missing from the HAR, Base64-decoded, truncated, or unavailable.

#### 7. [x] Improve SAZ fidelity

- Map useful `SessionTimers` values instead of retaining only start and total duration.
- Map supported session flags for client/server IP, process name and ID, HTTPS/TLS, socket/connection identifiers, protocol, and relevant Fiddler diagnostic metadata.
- Decode chunked transfer bodies before content decoding.
- Distinguish unsupported content encoding from corrupt encoded content rather than silently treating it as ordinary text.
- Preserve request-only and response-missing sessions with explicit completeness state.
- Add fixtures representing real-world HTTP/1.1, CONNECT, compressed, chunked, encrypted, request-only, and partially malformed archives.

#### 8. [x] Add import quality reporting

- Return an import result containing sessions plus warnings, truncations, skipped entries, unsupported features, and completeness counts.
- Present a post-import quality banner with an expandable details view.
- Continue importing recoverable sessions when one archive entry is malformed, while retaining strict failure for unsafe paths, archive bombs, invalid top-level formats, and unusable archives.
- Make partial-import behavior explicit and covered by tests.

### Phase 3 — Investigation workflows

#### 9. Timeline/waterfall — out of scope

- A full-trace timeline/waterfall was implemented and evaluated with the support-engineering workflow.
- It was removed because it did not provide enough troubleshooting value beyond the sortable session list, trace summary, and request/response details.
- Normalized timing metadata remains available for focused rules, summaries, and future evidence-driven diagnostics.
- Do not reintroduce a general timeline/waterfall without a concrete support scenario that cannot be served effectively by the existing session-first workflow.

#### 10. Broad automatic correlation — out of scope

- A proof of concept detected identifier families, redirect chains, authentication challenge/follow-up sequences, retries, and repeated failures.
- Normal working Outlook traces triggered multiple correlation cards, making expected protocol behavior look suspicious and adding noise to the session-detail workflow.
- The proof of concept was removed after support-engineering review.
- Do not reintroduce broad automatic correlation without an anomaly-specific, actionable scenario that distinguishes unhealthy behavior from normal protocol sequences.
- Focused actions such as finding other sessions with the same diagnostic identifier may be reconsidered separately if a concrete workflow requires them.

#### 11. Bookmarks and investigation notes — out of scope

- A proof of concept added session bookmarks, per-session local notes, and a Marked sessions filter.
- The additional grid controls, filter state, and detail-panel editor made the investigation workspace feel bloated relative to their troubleshooting value.
- The proof of concept was removed after support-engineering review.
- Do not reintroduce general bookmarks or notes unless a concrete investigation workflow demonstrates that the state cannot be managed effectively outside the trace analyzer.
- Never modify a source HAR or SAZ to store investigation state.

### Phase 4 — Scale, resilience, and release readiness

#### 12. Define performance budgets and benchmarks

- [x] Add generated benchmark fixtures for representative 1,000, 10,000, and 100,000-session traces within configured size limits.
- [x] Measure import time, analysis time, peak managed memory, initial render time, filter latency, and sort latency.
- [x] Establish versioned pass/fail budgets before selecting optimization techniques.
- [x] Run benchmarks for HAR and compressed SAZ, with an explicit encrypted-SAZ workload for practical targeted runs.

#### 13. Make long operations controllable

- [x] Add transient import and analysis progress states without adding a persistent panel.
- [x] Add cancellation that propagates through browser stream reading, importers, analysis, aggregation, and UI state.
- [x] Move analysis and summary aggregation off the Blazor UI interaction path.
- [x] Add table virtualization over in-memory data based on benchmark evidence without introducing paging controls.
- [x] Prevent stale imports from replacing newer user selections.

#### 14. Add UI and integration regression coverage

- [x] Add component tests for filters, clear actions, sorting, session selection, request/response view modes, password flow, and import warnings.
  - Current component coverage includes import/password states, import warnings, free-text and structured filtering, chips, summary drill-through, selection reconciliation, diagnostic headers, sorting, finding details, request/response modes, version states, virtualized large-trace rendering, JSON/XML formatting, HTML sandboxing, images, binary fallbacks, and truncation messaging.
- [x] Add browser-level smoke tests for opening representative HAR, unencrypted SAZ, and encrypted SAZ files.
  - The packaged Chromium test covers HAR and SAZ session rendering, filtering, filter clearing, diagnostic headers, request/response detail selection, AES-256 password prompting, incorrect-password recovery, and decrypted response inspection.
- [x] Test accessibility semantics and keyboard workflows for the main investigation path.
- [x] Retain importer safety, ruleset determinism, and secure HTML/XML preview tests.

#### 15. Complete release engineering

- [x] Commit and review the substantial post-`v0.1.0` work before beginning roadmap implementation.
- [x] Publish a release containing the already-completed encrypted SAZ, UI, port, classification, and versioning improvements before advertising later roadmap features.
- [x] Add repeatable publish/package verification for the Windows self-contained artifact.
- [x] Keep the local-only binding guidance and verify packaged static assets in release smoke tests.
- [ ] Update README capabilities and planned-work sections at each shipped phase.
  - Partial: the README documents current trace support and links to this roadmap; future shipped phases must continue updating it.

## Architecture notes

- Extract trace state, filtering, aggregation, and trace-level analysis from `Home.razor`; it is already carrying import, analysis, filtering, sorting, selection, password, update, and rendering responsibilities.
- Keep `M365Trace.Core` free of Blazor dependencies and suitable for later CLI, MCP, or API adapters.
- Model per-session analysis and trace-wide analysis separately:
  - `TraceAnalysisEngine` continues deterministic per-session classification.
  - A new trace analysis service consumes the complete analyzed session collection.
- Introduce a richer import return type without breaking safety boundaries:
  - normalized sessions
  - import warnings
  - skipped/partial counts
  - source capabilities and completeness
- Keep source-specific parsing in importer projects and expose only normalized optional fields to rules and UI.

## Deferred items

- Full-trace timeline or waterfall visualization.
- Broad automatic trace-correlation panels.
- Session bookmarks and investigation notes.
- MCP server or external web API implementation.
- Cloud-hosted trace processing or trace upload.
- Live traffic capture, proxying, or request replay.
- Collaborative cloud storage.
- Automatic remediation actions.
- Broad protocol support beyond the HTTP semantics represented in HAR and SAZ.

## Validation strategy

- Unit tests for every new normalized field and malformed/partial input path.
- Trace-level analysis fixtures proving deterministic grouping and evidence.
- Component tests for triage filters and session-first investigation workflows.
- Browser smoke tests for primary support-engineer workflows.
- Performance benchmarks at 1,000, 10,000, and 100,000 sessions.
- Security tests verifying bounded parsing, path safety, decompression limits, HTML sandboxing, XML restrictions, and password non-retention.

## Recommended delivery order

1. [x] Commit and release the completed current-state work.
2. [x] Complete Phase 1 triage, structured filters, ergonomics, accessibility, and browser coverage.
3. [x] Expand the core model and importer fidelity with import-quality reporting.
4. [ ] Harden scale, cancellation, UI regression coverage, and packaging.
   - Partial: scale benchmarks, cancellation, stale-operation protection, session-grid virtualization, HAR/SAZ packaged-browser coverage, and Windows packaging automation are in place. The remaining work is release/version completion rather than another investigation workflow.
5. [ ] Reassess whether an MCP adapter has a concrete support workflow after the standalone investigation experience is proven.
