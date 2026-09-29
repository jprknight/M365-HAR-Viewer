# Security testing

## Purpose

This document maps security-relevant behavior to repeatable repository tests and
workflow evidence. It does not replace independent security testing.

## Automated evidence

| Area | Security behavior | Evidence |
| --- | --- | --- |
| HAR size limits | Seekable and non-seekable files beyond the configured limit are rejected | `HarTraceImporterTests.ImportAsync_SeekableFileBeyondLimit_IsRejected`, `ImportAsync_NonSeekableFileBeyondLimit_IsRejected` |
| HAR entry limits | Excessive entry counts are rejected | `HarTraceImporterTests.ImportAsync_EntryCountBeyondLimit_IsRejected` |
| HAR metadata limits | Excessive metadata collections are rejected | `HarTraceImporterTests.ImportAsync_MetadataItemsBeyondLimit_AreRejected` |
| HAR content handling | Oversized retained text is truncated; invalid Base64 is rejected | `HarTraceImporterTests.ImportAsync_TextBeyondLimit_TruncatesBody`, `ImportAsync_InvalidBase64_IsRejected` |
| SAZ path safety | Unsafe archive paths are rejected | `SazTraceImporterTests.ImportAsync_UnsafeEntryPath_IsRejected` |
| SAZ archive limits | Entry count, expanded size, and per-entry size are bounded | `ImportAsync_EntryCountBeyondLimit_IsRejected`, `ImportAsync_ExpandedArchiveBeyondLimit_IsRejected`, `ImportAsync_SessionFileBeyondLimit_IsRejected` |
| SAZ encryption | ZipCrypto, AES-128, and AES-256 imports are covered | `SazTraceImporterTests.ImportAsync_EncryptedSaz_DecryptsSupportedEncryption` |
| SAZ password handling | Missing and incorrect passwords produce explicit flows | `ImportAsync_EncryptedSazWithoutPassword_RequestsPassword`, `ImportAsync_EncryptedSazWithWrongPassword_RejectsPassword` |
| Import cancellation | HAR and SAZ imports honor cancellation | `HarTraceImporterTests.ImportAsync_ObservesCancellation`, `SazTraceImporterTests.ImportAsync_CancelledImport_StopsProcessing` |
| HTML preview | Preview iframe remains sandboxed | `BodyInspectorTests` sandbox assertion |
| XML preview | DTD processing and external resolution remain disabled | `BodyInspectorTests` XML cases and implementation review |
| Telemetry schema | Only reviewed model properties are exposed | `UsageTelemetryServiceTests.TelemetryModels_ExposeOnlyReviewedProperties` |
| Telemetry consent | Unknown consent emits no telemetry; enable and disable decisions persist | `UsageTelemetryServiceTests` and `HomeTests` consent cases |
| Telemetry minimization | Import telemetry uses coarse buckets and normalized errors | `UsageTelemetryServiceTests.ImportTelemetry_UsesCoarseSessionBuckets`, `HomeTests` import telemetry cases |
| Telemetry failure | Export failures do not interrupt trace analysis | `UsageTelemetryServiceTests.SinkFailure_DoesNotEscapeTelemetryService` |
| Local listener | Wildcard environment configuration still produces loopback-only listeners | `StandaloneApplicationTests.StandalonePackage_EnforcesLoopbackOnlyListener` |
| Removed network binding | `--urls` exits without opening a listener | `StandaloneApplicationTests.StandalonePackage_RejectsUrlsOptionWithoutListening` |
| Host validation | Untrusted Host values receive HTTP 400 | `StandaloneApplicationTests.StandalonePackage_SupportsCriticalInvestigationWorkflow` |
| Packaged workflow | HAR, unencrypted SAZ, encrypted SAZ, password retry, previews, telemetry consent, filtering, and keyboard workflows execute against the published package | `StandaloneApplicationTests.StandalonePackage_SupportsCriticalInvestigationWorkflow` |

Test paths:

- `tests\M365Trace.Import.Har.Tests`
- `tests\M365Trace.Import.Saz.Tests`
- `tests\M365Trace.Web.Tests`
- `tests\M365Trace.Web.E2E.Tests`

## Workflow evidence

| Workflow check | Purpose |
| --- | --- |
| **Build, test, and format** | Restore, formatting, warning-free build, unit and component tests, coverage floor, and NuGet vulnerability scan |
| **Publish and smoke test Windows package** | Publish the self-contained package, start it, load static assets, and run packaged Chromium tests |
| **Dependency review** | Reject pull requests that introduce dependencies with moderate-or-higher known vulnerabilities |
| **CodeQL** | Run C# security-extended static analysis |
| **Release** | Re-run build and tests, package the application, generate an SBOM and checksums, create attestations, and publish release assets |

## Manual and independent testing

Before broad support-engineer distribution, the confidential assessment package
should include:

- Independent architecture and threat-model review.
- Static-analysis findings and dispositions.
- Dependency and SBOM review.
- Localhost and browser-origin testing.
- Malformed and adversarial HAR/SAZ testing.
- Temporary-file and process-termination testing.
- Telemetry endpoint and access-control review when telemetry is enabled.
- Verification that the published archive and SBOM attestations are valid.
- Review of residual risks and accepted exceptions.

Do not commit confidential findings, exploit details for unresolved issues,
customer data, or internal infrastructure evidence to the public repository.

## Reproduction

Run the standard validation:

```powershell
dotnet restore .\M365-Trace-Analyzer.sln
dotnet format .\M365-Trace-Analyzer.sln --verify-no-changes --no-restore
dotnet build .\M365-Trace-Analyzer.sln --configuration Release --no-restore -warnaserror
dotnet test .\M365-Trace-Analyzer.sln --configuration Release --no-build
```

The packaged browser workflow requires a self-contained Windows publish
directory identified by `M365_TRACE_PUBLISH_DIR`.
