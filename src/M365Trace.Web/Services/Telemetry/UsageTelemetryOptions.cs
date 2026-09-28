namespace M365Trace.Web.Services.Telemetry;

public sealed class UsageTelemetryOptions
{
    public const string SectionName = "Telemetry";

    public string ApplicationInsightsConnectionString { get; set; } =
        string.Empty;

    public string SettingsPath { get; set; } = string.Empty;
}
