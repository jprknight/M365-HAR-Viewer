namespace M365Trace.Web.Services.Telemetry;

public interface IUsageTelemetrySink
{
    bool IsAvailable { get; }

    void TrackApplicationStarted(UsageTelemetryContext context);

    void TrackTraceImportCompleted(
        UsageTelemetryContext context,
        TraceImportUsageEvent import);
}
