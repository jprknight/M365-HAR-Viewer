using M365Trace.Web.Services.Telemetry;
using Microsoft.Extensions.Logging.Abstractions;

namespace M365Trace.Web.Tests;

public sealed class UsageTelemetryServiceTests
{
    [Fact]
    public void TelemetryModels_ExposeOnlyReviewedProperties()
    {
        Assert.Equal(
            [
                "ApplicationSessionId",
                "ApplicationVersion",
                "Architecture",
                "InstallationId",
                "OperatingSystem",
                "SchemaVersion"
            ],
            typeof(UsageTelemetryContext)
                .GetProperties()
                .Select(property => property.Name)
                .Order(StringComparer.Ordinal));
        Assert.Equal(
            [
                "DurationBucket",
                "Encrypted",
                "Error",
                "Format",
                "Outcome",
                "SessionCountBucket"
            ],
            typeof(TraceImportUsageEvent)
                .GetProperties()
                .Select(property => property.Name)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task UnknownConsent_EmitsNoTelemetry()
    {
        var sink = new RecordingTelemetrySink();
        var service = CreateService(
            new InMemoryTelemetrySettingsStore(
                TelemetrySettings.Default),
            sink);

        await service.InitializeAsync();
        service.TrackTraceImportCompleted(
            UsageTraceFormat.Har,
            encrypted: false,
            UsageTraceImportOutcome.Complete,
            sessionCount: 12,
            TimeSpan.FromSeconds(2));

        Assert.Empty(sink.ApplicationStarts);
        Assert.Empty(sink.Imports);
    }

    [Fact]
    public async Task EnablingConsent_PersistsRandomIdAndEmitsStartup()
    {
        var store = new InMemoryTelemetrySettingsStore(
            TelemetrySettings.Default);
        var sink = new RecordingTelemetrySink();
        var service = CreateService(store, sink);
        await service.InitializeAsync();

        await service.SetConsentAsync(TelemetryConsent.Enabled);

        Assert.Equal(TelemetryConsent.Enabled, store.Settings.Consent);
        Assert.True(Guid.TryParseExact(
            store.Settings.InstallationId,
            "N",
            out _));
        var startup = Assert.Single(sink.ApplicationStarts);
        Assert.Equal(store.Settings.InstallationId, startup.InstallationId);
        Assert.Equal(
            service.ApplicationSessionId,
            startup.ApplicationSessionId);
    }

    [Fact]
    public async Task ExistingInstallationId_RemainsStableAcrossSessions()
    {
        var installationId = Guid.NewGuid().ToString("N");
        var store = new InMemoryTelemetrySettingsStore(
            new TelemetrySettings(
                TelemetryConsent.Enabled,
                installationId));
        var firstSink = new RecordingTelemetrySink();
        var secondSink = new RecordingTelemetrySink();

        var first = CreateService(store, firstSink);
        var second = CreateService(store, secondSink);
        await first.InitializeAsync();
        await second.InitializeAsync();

        Assert.Equal(
            installationId,
            Assert.Single(firstSink.ApplicationStarts).InstallationId);
        Assert.Equal(
            installationId,
            Assert.Single(secondSink.ApplicationStarts).InstallationId);
        Assert.NotEqual(
            first.ApplicationSessionId,
            second.ApplicationSessionId);
    }

    [Fact]
    public async Task DisablingConsent_StopsFutureEvents()
    {
        var sink = new RecordingTelemetrySink();
        var service = CreateEnabledService(sink);
        await service.InitializeAsync();
        await service.SetConsentAsync(TelemetryConsent.Disabled);

        service.TrackTraceImportCompleted(
            UsageTraceFormat.Har,
            encrypted: false,
            UsageTraceImportOutcome.Complete,
            sessionCount: 10,
            TimeSpan.FromSeconds(1));

        Assert.Empty(sink.Imports);
        Assert.Single(sink.ApplicationStarts);
    }

    [Fact]
    public async Task ResetInstallationId_CreatesNewAnonymousIdentity()
    {
        var originalId = Guid.NewGuid().ToString("N");
        var store = new InMemoryTelemetrySettingsStore(
            new TelemetrySettings(
                TelemetryConsent.Enabled,
                originalId));
        var sink = new RecordingTelemetrySink();
        var service = CreateService(store, sink);
        await service.InitializeAsync();

        await service.ResetInstallationIdAsync();

        Assert.NotEqual(originalId, store.Settings.InstallationId);
        Assert.True(Guid.TryParseExact(
            store.Settings.InstallationId,
            "N",
            out _));
        Assert.Equal(2, sink.ApplicationStarts.Count);
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(99, "1-99")]
    [InlineData(100, "100-999")]
    [InlineData(9999, "1k-9k")]
    [InlineData(10000, "10k-99k")]
    [InlineData(100000, "100k+")]
    public async Task ImportTelemetry_UsesCoarseSessionBuckets(
        int sessionCount,
        string expectedBucket)
    {
        var sink = new RecordingTelemetrySink();
        var service = CreateEnabledService(sink);
        await service.InitializeAsync();

        service.TrackTraceImportCompleted(
            UsageTraceFormat.Saz,
            encrypted: true,
            UsageTraceImportOutcome.Partial,
            sessionCount,
            TimeSpan.FromSeconds(8));

        var import = Assert.Single(sink.Imports);
        Assert.Equal(expectedBucket, import.Import.SessionCountBucket);
        Assert.Equal("5-30s", import.Import.DurationBucket);
        Assert.Equal(UsageTraceFormat.Saz, import.Import.Format);
        Assert.True(import.Import.Encrypted);
    }

    [Fact]
    public async Task SinkFailure_DoesNotEscapeTelemetryService()
    {
        var service = new UsageTelemetryService(
            new InMemoryTelemetrySettingsStore(
                new TelemetrySettings(
                    TelemetryConsent.Enabled,
                    Guid.NewGuid().ToString("N"))),
            new ThrowingTelemetrySink(),
            NullLogger<UsageTelemetryService>.Instance);

        await service.InitializeAsync();
        service.TrackTraceImportCompleted(
            UsageTraceFormat.Har,
            encrypted: false,
            UsageTraceImportOutcome.Failed,
            sessionCount: null,
            TimeSpan.Zero,
            UsageTraceImportError.ImportRejected);
    }

    private static UsageTelemetryService CreateEnabledService(
        RecordingTelemetrySink sink) =>
        CreateService(
            new InMemoryTelemetrySettingsStore(
                new TelemetrySettings(
                    TelemetryConsent.Enabled,
                    Guid.NewGuid().ToString("N"))),
            sink);

    private static UsageTelemetryService CreateService(
        ITelemetrySettingsStore store,
        IUsageTelemetrySink sink) =>
        new(
            store,
            sink,
            NullLogger<UsageTelemetryService>.Instance);

    private sealed class InMemoryTelemetrySettingsStore(
        TelemetrySettings settings) :
        ITelemetrySettingsStore
    {
        public TelemetrySettings Settings { get; private set; } = settings;

        public Task<TelemetrySettings> LoadAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Settings);

        public Task SaveAsync(
            TelemetrySettings settings,
            CancellationToken cancellationToken = default)
        {
            Settings = settings;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingTelemetrySink : IUsageTelemetrySink
    {
        public bool IsAvailable => true;

        public List<UsageTelemetryContext> ApplicationStarts { get; } = [];

        public List<RecordedImport> Imports { get; } = [];

        public void TrackApplicationStarted(UsageTelemetryContext context) =>
            ApplicationStarts.Add(context);

        public void TrackTraceImportCompleted(
            UsageTelemetryContext context,
            TraceImportUsageEvent import) =>
            Imports.Add(new RecordedImport(context, import));
    }

    private sealed class ThrowingTelemetrySink : IUsageTelemetrySink
    {
        public bool IsAvailable => true;

        public void TrackApplicationStarted(UsageTelemetryContext context) =>
            throw new InvalidOperationException("Expected test failure.");

        public void TrackTraceImportCompleted(
            UsageTelemetryContext context,
            TraceImportUsageEvent import) =>
            throw new InvalidOperationException("Expected test failure.");
    }

    private sealed record RecordedImport(
        UsageTelemetryContext Context,
        TraceImportUsageEvent Import);
}
