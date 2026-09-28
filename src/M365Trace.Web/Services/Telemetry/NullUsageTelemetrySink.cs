namespace M365Trace.Web.Services.Telemetry;

public sealed class NullUsageTelemetrySink : IUsageTelemetrySink
{
    public bool IsAvailable => false;

    public void TrackApplicationStarted(UsageTelemetryContext context)
    {
    }

    public void TrackTraceImportCompleted(
        UsageTelemetryContext context,
        TraceImportUsageEvent import)
    {
    }
}
