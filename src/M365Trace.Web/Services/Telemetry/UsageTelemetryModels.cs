namespace M365Trace.Web.Services.Telemetry;

public enum UsageTraceFormat
{
    Unknown,
    Har,
    Saz
}

public enum UsageTraceImportOutcome
{
    Complete,
    Partial,
    Failed,
    Cancelled
}

public enum UsageTraceImportError
{
    None,
    PasswordIncorrect,
    ImportRejected,
    IoError,
    InvalidOperation
}

public sealed record UsageTelemetryContext(
    string InstallationId,
    string ApplicationSessionId,
    string ApplicationVersion,
    string OperatingSystem,
    string Architecture,
    int SchemaVersion);

public sealed record TraceImportUsageEvent(
    UsageTraceFormat Format,
    bool Encrypted,
    UsageTraceImportOutcome Outcome,
    string SessionCountBucket,
    string DurationBucket,
    UsageTraceImportError Error);
