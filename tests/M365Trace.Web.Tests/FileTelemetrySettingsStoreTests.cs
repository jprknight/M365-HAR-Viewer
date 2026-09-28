using M365Trace.Web.Services.Telemetry;
using Microsoft.Extensions.Logging.Abstractions;

namespace M365Trace.Web.Tests;

public sealed class FileTelemetrySettingsStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        $"M365TraceAnalyzerTests-{Guid.NewGuid():N}");

    [Fact]
    public async Task SaveAndLoad_RoundTripsConsentAndInstallationId()
    {
        var store = CreateStore();
        var expected = new TelemetrySettings(
            TelemetryConsent.Enabled,
            Guid.NewGuid().ToString("N"));

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        Assert.Equal(expected, actual);
        Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
    }

    [Fact]
    public async Task CorruptSettings_FailClosed()
    {
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(
            Path.Combine(directory, "settings.json"),
            "{not valid JSON");
        var store = CreateStore();

        var settings = await store.LoadAsync();

        Assert.Equal(TelemetrySettings.Default, settings);
    }

    [Fact]
    public async Task InvalidConsentValue_FailsClosed()
    {
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(
            Path.Combine(directory, "settings.json"),
            """
            {
              "consent": 999,
              "installationId": "0123456789abcdef0123456789abcdef"
            }
            """);
        var store = CreateStore();

        var settings = await store.LoadAsync();

        Assert.Equal(TelemetrySettings.Default, settings);
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private FileTelemetrySettingsStore CreateStore() =>
        new(
            new TestApplicationDataPathProvider(
                Path.Combine(directory, "settings.json")),
            NullLogger<FileTelemetrySettingsStore>.Instance);

    private sealed class TestApplicationDataPathProvider(string path) :
        IApplicationDataPathProvider
    {
        public string GetTelemetrySettingsPath() => path;
    }
}
