using System.Text.Json;
using System.Text.Json.Serialization;

namespace M365Trace.Web.Services.Telemetry;

public interface ITelemetrySettingsStore
{
    Task<TelemetrySettings> LoadAsync(
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        TelemetrySettings settings,
        CancellationToken cancellationToken = default);
}

public sealed record TelemetrySettings(
    TelemetryConsent Consent,
    string? InstallationId)
{
    public static TelemetrySettings Default { get; } =
        new(TelemetryConsent.Unknown, null);
}

public interface IApplicationDataPathProvider
{
    string GetTelemetrySettingsPath();
}

public sealed class ApplicationDataPathProvider(
    UsageTelemetryOptions options) :
    IApplicationDataPathProvider
{
    public string GetTelemetrySettingsPath()
    {
        if (!string.IsNullOrWhiteSpace(options.SettingsPath))
        {
            return Path.GetFullPath(options.SettingsPath);
        }

        var root = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(
            root,
            "M365 Trace Analyzer",
            "settings.json");
    }
}

public sealed class FileTelemetrySettingsStore(
    IApplicationDataPathProvider pathProvider,
    ILogger<FileTelemetrySettingsStore> logger) :
    ITelemetrySettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    public async Task<TelemetrySettings> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        var path = pathProvider.GetTelemetrySettingsPath();
        if (!File.Exists(path))
        {
            return TelemetrySettings.Default;
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var settings =
                await JsonSerializer.DeserializeAsync<TelemetrySettings>(
                    stream,
                    JsonOptions,
                    cancellationToken);
            if (settings is null
                || !Enum.IsDefined(settings.Consent))
            {
                logger.LogWarning(
                    "Usage telemetry settings contain an invalid consent "
                    + "value. Telemetry remains disabled.");
                return TelemetrySettings.Default;
            }

            return settings;
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or JsonException)
        {
            logger.LogWarning(
                exception,
                "Unable to read usage telemetry settings. "
                + "Telemetry remains disabled until consent is saved.");
            return TelemetrySettings.Default;
        }
    }

    public async Task SaveAsync(
        TelemetrySettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var path = pathProvider.GetTelemetrySettingsPath();
        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException(
                "The telemetry settings path has no parent directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.Asynchronous))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    settings,
                    JsonOptions,
                    cancellationToken);
            }

            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }
}
