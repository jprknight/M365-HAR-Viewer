using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

namespace M365Trace.Web.Services.Telemetry;

public sealed class AzureMonitorUsageTelemetrySink :
    IUsageTelemetrySink,
    IDisposable
{
    private const string ServiceName = "M365TraceAnalyzer.UsageTelemetry";

    private readonly ILoggerFactory loggerFactory;
    private readonly ILogger logger;

    public AzureMonitorUsageTelemetrySink(
        UsageTelemetryOptions options,
        string applicationVersion)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(
            options.ApplicationInsightsConnectionString);

        loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddOpenTelemetry(openTelemetry =>
            {
                openTelemetry.SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        .AddService(
                            ServiceName,
                            serviceVersion: applicationVersion));
                openTelemetry.AddAzureMonitorLogExporter(exporter =>
                {
                    exporter.ConnectionString =
                        options.ApplicationInsightsConnectionString;
                    exporter.DisableOfflineStorage = true;
                });
            });
        });
        logger = loggerFactory.CreateLogger(ServiceName);
    }

    public bool IsAvailable => true;

    public void TrackApplicationStarted(UsageTelemetryContext context)
    {
        logger.LogInformation(
            "Usage event {microsoft.custom_event.name} "
            + "installation {installation_id} "
            + "application session {application_session_id} "
            + "version {app_version} "
            + "operating system {os_family} "
            + "architecture {architecture} "
            + "schema {telemetry_schema_version}",
            "ApplicationStarted",
            context.InstallationId,
            context.ApplicationSessionId,
            context.ApplicationVersion,
            context.OperatingSystem,
            context.Architecture,
            context.SchemaVersion);
    }

    public void TrackTraceImportCompleted(
        UsageTelemetryContext context,
        TraceImportUsageEvent import)
    {
        logger.LogInformation(
            "Usage event {microsoft.custom_event.name} "
            + "installation {installation_id} "
            + "application session {application_session_id} "
            + "version {app_version} "
            + "format {format} "
            + "encrypted {encrypted} "
            + "outcome {outcome} "
            + "session count {session_count_bucket} "
            + "duration {duration_bucket} "
            + "error {error_code} "
            + "schema {telemetry_schema_version}",
            "TraceImportCompleted",
            context.InstallationId,
            context.ApplicationSessionId,
            context.ApplicationVersion,
            GetFormat(import.Format),
            import.Encrypted,
            GetOutcome(import.Outcome),
            import.SessionCountBucket,
            import.DurationBucket,
            GetErrorCode(import.Error),
            context.SchemaVersion);
    }

    public void Dispose() => loggerFactory.Dispose();

    private static string GetFormat(UsageTraceFormat format) =>
        format switch
        {
            UsageTraceFormat.Har => "har",
            UsageTraceFormat.Saz => "saz",
            _ => "unknown"
        };

    private static string GetOutcome(UsageTraceImportOutcome outcome) =>
        outcome switch
        {
            UsageTraceImportOutcome.Complete => "complete",
            UsageTraceImportOutcome.Partial => "partial",
            UsageTraceImportOutcome.Failed => "failed",
            UsageTraceImportOutcome.Cancelled => "cancelled",
            _ => "failed"
        };

    private static string GetErrorCode(UsageTraceImportError error) =>
        error switch
        {
            UsageTraceImportError.None => "none",
            UsageTraceImportError.PasswordIncorrect => "invalid_password",
            UsageTraceImportError.ImportRejected => "import_rejected",
            UsageTraceImportError.IoError => "io_error",
            UsageTraceImportError.InvalidOperation => "invalid_operation",
            _ => "invalid_operation"
        };
}
