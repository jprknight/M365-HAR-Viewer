namespace M365Trace.Web.Services.Telemetry;

public interface IUsageTelemetryService
{
    bool IsAvailable { get; }

    TelemetryConsent Consent { get; }

    string ApplicationSessionId { get; }

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task SetConsentAsync(
        TelemetryConsent consent,
        CancellationToken cancellationToken = default);

    Task ResetInstallationIdAsync(
        CancellationToken cancellationToken = default);

    void TrackTraceImportCompleted(
        UsageTraceFormat format,
        bool encrypted,
        UsageTraceImportOutcome outcome,
        int? sessionCount,
        TimeSpan duration,
        UsageTraceImportError error = UsageTraceImportError.None);
}
