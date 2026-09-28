using M365Trace.Web.Components;
using M365Trace.Core;
using M365Trace.Import.Har;
using M365Trace.Import.Saz;
using M365Trace.Rules;
using M365Trace.Rules.Legacy;
using M365Trace.Web.Services;
using M365Trace.Web.Services.Telemetry;

LocalServerOptions localServerOptions;
try
{
    localServerOptions = LocalServerOptions.Parse(args);
}
catch (ArgumentException exception)
{
    Console.Error.WriteLine($"Error: {exception.Message}");
    Environment.ExitCode = 2;
    return;
}

var builder = WebApplication.CreateBuilder(
    localServerOptions.RemainingArguments);
builder.WebHost.ConfigureKestrel(options =>
    options.ListenLocalhost(localServerOptions.Port));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHttpClient("GitHubReleases", client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("M365-Trace-Analyzer");
    client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddSingleton<VersionUpdateService>();
builder.Services.AddSingleton<SessionQueryService>();
builder.Services.AddSingleton<TraceSummaryService>();
builder.Services.AddSingleton<DiagnosticHeaderService>();
builder.Services.AddScoped<TraceOperationCoordinator>();
builder.Services.AddSingleton<DefaultBrowserLauncher>();
builder.Services.AddSingleton<IApplicationDataPathProvider, ApplicationDataPathProvider>();
builder.Services.AddSingleton<ITelemetrySettingsStore, FileTelemetrySettingsStore>();
var telemetryOptions = new UsageTelemetryOptions();
builder.Configuration
    .GetSection(UsageTelemetryOptions.SectionName)
    .Bind(telemetryOptions);
builder.Services.AddSingleton(telemetryOptions);
builder.Services.AddSingleton<IUsageTelemetrySink>(serviceProvider =>
    string.IsNullOrWhiteSpace(
        telemetryOptions.ApplicationInsightsConnectionString)
        ? new NullUsageTelemetrySink()
        : new AzureMonitorUsageTelemetrySink(
            telemetryOptions,
            serviceProvider
                .GetRequiredService<VersionUpdateService>()
                .CurrentVersion));
builder.Services.AddSingleton<IUsageTelemetryService, UsageTelemetryService>();
builder.Services.AddSingleton<ITraceImporter, HarTraceImporter>();
builder.Services.AddSingleton<ITraceImporter, SazTraceImporter>();
builder.Services.AddSingleton<LegacyRulesetData>();
builder.Services.AddSingleton<RulesetManifestProvider>();
foreach (var ruleType in typeof(ITraceRule).Assembly
             .GetTypes()
             .Where(type =>
                 !type.IsAbstract
                 && !type.IsInterface
                 && typeof(ITraceRule).IsAssignableFrom(type))
             .OrderBy(type => type.FullName, StringComparer.Ordinal))
{
    builder.Services.AddSingleton(typeof(ITraceRule), ruleType);
}

builder.Services.AddSingleton<RuleCatalog>();
builder.Services.AddSingleton(serviceProvider =>
    new TraceAnalysisEngine(
        serviceProvider.GetRequiredService<RuleCatalog>()));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

var browserLauncher = app.Services.GetRequiredService<DefaultBrowserLauncher>();
app.Lifetime.ApplicationStarted.Register(() =>
    browserLauncher.TryLaunch(localServerOptions.Port));

await app.Services
    .GetRequiredService<IUsageTelemetryService>()
    .InitializeAsync();

await app.RunAsync();
