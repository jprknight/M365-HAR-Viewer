using System.Reflection;
using System.Runtime.InteropServices;

namespace M365Trace.Web.Services.Telemetry;

public sealed class UsageTelemetryService(
    ITelemetrySettingsStore settingsStore,
    IUsageTelemetrySink sink,
    ILogger<UsageTelemetryService> logger) :
    IUsageTelemetryService
{
    private const int TelemetrySchemaVersion = 1;

    private readonly SemaphoreSlim settingsLock = new(1, 1);
    private TelemetrySettings settings = TelemetrySettings.Default;
    private bool initialized;
    private bool applicationStartedTracked;

    public bool IsAvailable => sink.IsAvailable;

    public TelemetryConsent Consent => settings.Consent;

    public string ApplicationSessionId { get; } =
        Guid.NewGuid().ToString("N");

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        await settingsLock.WaitAsync(cancellationToken);
        try
        {
            if (initialized)
            {
                return;
            }

            settings = await settingsStore.LoadAsync(cancellationToken);
            if (settings.Consent == TelemetryConsent.Enabled
                && !IsValidInstallationId(settings.InstallationId))
            {
                settings = settings with
                {
                    InstallationId = CreateInstallationId()
                };
                await settingsStore.SaveAsync(settings, cancellationToken);
            }

            initialized = true;
            TrackApplicationStartedIfEnabled();
        }
        finally
        {
            settingsLock.Release();
        }
    }

    public async Task SetConsentAsync(
        TelemetryConsent consent,
        CancellationToken cancellationToken = default)
    {
        if (consent == TelemetryConsent.Unknown)
        {
            throw new ArgumentOutOfRangeException(
                nameof(consent),
                "Consent must be explicitly enabled or disabled.");
        }

        await settingsLock.WaitAsync(cancellationToken);
        try
        {
            EnsureInitialized();

            settings = settings with
            {
                Consent = consent,
                InstallationId = consent == TelemetryConsent.Enabled
                    ? GetOrCreateInstallationId()
                    : settings.InstallationId
            };
            await settingsStore.SaveAsync(settings, cancellationToken);
            TrackApplicationStartedIfEnabled();
        }
        finally
        {
            settingsLock.Release();
        }
    }

    public async Task ResetInstallationIdAsync(
        CancellationToken cancellationToken = default)
    {
        await settingsLock.WaitAsync(cancellationToken);
        try
        {
            EnsureInitialized();

            settings = settings with
            {
                InstallationId = CreateInstallationId()
            };
            await settingsStore.SaveAsync(settings, cancellationToken);
            applicationStartedTracked = false;
            TrackApplicationStartedIfEnabled();
        }
        finally
        {
            settingsLock.Release();
        }
    }

    public void TrackTraceImportCompleted(
        UsageTraceFormat format,
        bool encrypted,
        UsageTraceImportOutcome outcome,
        int? sessionCount,
        TimeSpan duration,
        UsageTraceImportError error = UsageTraceImportError.None)
    {
        if (!CanTrack())
        {
            return;
        }

        try
        {
            sink.TrackTraceImportCompleted(
                CreateContext(),
                new TraceImportUsageEvent(
                    format,
                    encrypted,
                    outcome,
                    GetSessionCountBucket(sessionCount),
                    GetDurationBucket(duration),
                    error));
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Unable to emit anonymous usage telemetry.");
        }
    }

    private void TrackApplicationStartedIfEnabled()
    {
        if (!CanTrack() || applicationStartedTracked)
        {
            return;
        }

        try
        {
            sink.TrackApplicationStarted(CreateContext());
            applicationStartedTracked = true;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Unable to emit anonymous application-start telemetry.");
        }
    }

    private bool CanTrack() =>
        initialized
        && IsAvailable
        && settings.Consent == TelemetryConsent.Enabled
        && IsValidInstallationId(settings.InstallationId);

    private UsageTelemetryContext CreateContext() =>
        new(
            settings.InstallationId!,
            ApplicationSessionId,
            GetApplicationVersion(),
            GetOperatingSystemFamily(),
            RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant(),
            TelemetrySchemaVersion);

    private string GetOrCreateInstallationId() =>
        IsValidInstallationId(settings.InstallationId)
            ? settings.InstallationId!
            : CreateInstallationId();

    private void EnsureInitialized()
    {
        if (!initialized)
        {
            throw new InvalidOperationException(
                "Usage telemetry must be initialized before changing settings.");
        }
    }

    private static bool IsValidInstallationId(string? value) =>
        Guid.TryParseExact(value, "N", out _);

    private static string CreateInstallationId() =>
        Guid.NewGuid().ToString("N");

    private static string GetApplicationVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        return version is null
            ? "unknown"
            : $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private static string GetOperatingSystemFamily() =>
        OperatingSystem.IsWindows()
            ? "windows"
            : OperatingSystem.IsMacOS()
                ? "macos"
                : OperatingSystem.IsLinux()
                    ? "linux"
                    : "other";

    private static string GetSessionCountBucket(int? sessionCount) =>
        sessionCount switch
        {
            null => "not_available",
            < 1 => "0",
            < 100 => "1-99",
            < 1_000 => "100-999",
            < 10_000 => "1k-9k",
            < 100_000 => "10k-99k",
            _ => "100k+"
        };

    private static string GetDurationBucket(TimeSpan duration) =>
        duration.TotalSeconds switch
        {
            < 1 => "under_1s",
            < 5 => "1-5s",
            < 30 => "5-30s",
            < 120 => "30-120s",
            _ => "120s+"
        };
}
