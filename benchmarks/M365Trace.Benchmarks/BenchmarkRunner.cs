using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using M365Trace.Core;
using M365Trace.Import.Har;
using M365Trace.Import.Saz;
using M365Trace.Rules;
using M365Trace.Rules.Legacy;
using M365Trace.Web.Components;
using M365Trace.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace M365Trace.Benchmarks;

internal sealed class BenchmarkRunner(BenchmarkOptions options)
{
    private readonly TraceAnalysisEngine _analysisEngine =
        CreateAnalysisEngine();
    private readonly SessionQueryService _queryService = new();
    private readonly TraceSummaryService _summaryService = new();

    public async Task<BenchmarkReport> RunAsync(
        CancellationToken cancellationToken = default)
    {
        var budgets = await LoadBudgetsAsync(cancellationToken);
        var results = new List<BenchmarkScenarioResult>();
        var fixtureDirectory = Path.Combine(
            options.RepositoryRoot,
            "artifacts",
            "benchmarks",
            "fixtures");

        foreach (var count in options.SessionCounts)
        {
            foreach (var format in options.Formats)
            {
                Console.WriteLine(
                    $"Running {TraceFixtureGenerator.GetFormatName(format)} "
                    + $"with {count:N0} sessions...");
                var result = await RunScenarioAsync(
                    fixtureDirectory,
                    format,
                    count,
                    cancellationToken);
                results.Add(result);
            }
        }

        var failures = EvaluateBudgets(results, budgets);
        return new BenchmarkReport
        {
            CreatedAt = DateTimeOffset.UtcNow,
            Environment = new BenchmarkEnvironment(
                RuntimeInformation.OSDescription,
                RuntimeInformation.FrameworkDescription,
                Environment.ProcessorCount,
                GC.GetGCMemoryInfo().TotalAvailableMemoryBytes),
            Scenarios = results,
            BudgetFailures = failures
        };
    }

    private async Task<BenchmarkScenarioResult> RunScenarioAsync(
        string fixtureDirectory,
        BenchmarkFormat format,
        int sessionCount,
        CancellationToken cancellationToken)
    {
        var fixturePath = await TraceFixtureGenerator.CreateAsync(
            fixtureDirectory,
            format,
            sessionCount,
            cancellationToken);

        try
        {
            var fixtureBytes = new FileInfo(fixturePath).Length;
            IReadOnlyList<TraceSession> importedSessions = [];
            var import = await MeasureAsync(async () =>
            {
                await using var stream = new FileStream(
                    fixturePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    1024 * 1024,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                var importer = CreateImporter(format, fixtureBytes);
                var importOptions = format == BenchmarkFormat.EncryptedSaz
                    ? new TraceImportOptions
                    {
                        Password = TraceFixtureGenerator.GetPassword(format)
                    }
                    : null;
                importedSessions = (await importer.ImportWithReportAsync(
                    stream,
                    importOptions,
                    cancellationToken)).Sessions;
                if (importedSessions.Count != sessionCount)
                {
                    throw new InvalidDataException(
                        $"Expected {sessionCount:N0} imported sessions but "
                        + $"received {importedSessions.Count:N0}.");
                }
            });

            IReadOnlyList<TraceSession> analyzedSessions = [];
            var analysis = await MeasureAsync(() =>
            {
                analyzedSessions = importedSessions
                    .Select(session => session with
                    {
                        Analysis = _analysisEngine.Analyze(session)
                    })
                    .ToArray();
                return Task.CompletedTask;
            });
            TraceSummary? summaryResult = null;
            var summary = await MeasureAsync(() =>
            {
                summaryResult = _summaryService.Create(analyzedSessions);
                return Task.CompletedTask;
            });
            if (summaryResult?.TotalSessions != sessionCount)
            {
                throw new InvalidDataException(
                    $"Expected a {sessionCount:N0}-session summary but "
                    + $"received {summaryResult?.TotalSessions ?? 0:N0}.");
            }

            BenchmarkMeasurement? initialRender = null;
            if (!options.SkipRender)
            {
                initialRender = await MeasureAsync(() =>
                    RenderSessionTableAsync(analyzedSessions));
            }

            IReadOnlyList<TraceSession> filteredSessions = [];
            var filter = await MeasureAsync(() =>
            {
                filteredSessions = _queryService.Apply(
                    analyzedSessions,
                    new SessionQuery
                    {
                        FreeText = "benchmark-marker-0",
                        StatusFamilies = [TraceStatusFamily.ServerError]
                    });
                return Task.CompletedTask;
            });
            var expectedFilteredSessionCount = sessionCount / 100;
            if (filteredSessions.Count != expectedFilteredSessionCount)
            {
                throw new InvalidDataException(
                    $"Expected {expectedFilteredSessionCount:N0} filtered "
                    + $"sessions but received {filteredSessions.Count:N0}.");
            }

            var sort = await MeasureAsync(() =>
            {
                _ = _queryService.Apply(
                    analyzedSessions,
                    new SessionQuery
                    {
                        SortColumn = SessionSortColumn.Duration,
                        SortAscending = false
                    });
                return Task.CompletedTask;
            });

            return new BenchmarkScenarioResult
            {
                Format = TraceFixtureGenerator.GetFormatName(format),
                SessionCount = sessionCount,
                FixtureBytes = fixtureBytes,
                Import = import,
                Analysis = analysis,
                Summary = summary,
                InitialRender = initialRender,
                Filter = filter,
                Sort = sort,
                FilteredSessionCount = filteredSessions.Count,
                SummarySessionCount = summaryResult?.TotalSessions ?? 0
            };
        }
        finally
        {
            if (!options.KeepFixtures)
            {
                File.Delete(fixturePath);
            }
        }
    }

    private static ITraceImporter CreateImporter(
        BenchmarkFormat format,
        long fixtureBytes) =>
        format switch
        {
            BenchmarkFormat.Har => new HarTraceImporter(new HarImportOptions
            {
                MaximumFileSize = Math.Max(
                    HarImportOptions.DefaultMaximumFileSize,
                    fixtureBytes + 1)
            }),
            BenchmarkFormat.Saz or BenchmarkFormat.EncryptedSaz =>
                new SazTraceImporter(new SazImportOptions
                {
                    MaximumFileSize = Math.Max(
                        SazImportOptions.DefaultMaximumFileSize,
                        fixtureBytes + 1)
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

    private static async Task RenderSessionTableAsync(
        IReadOnlyList<TraceSession> sessions)
    {
        using var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IJSRuntime, BenchmarkJsRuntime>()
            .BuildServiceProvider();
        await using var renderer = new HtmlRenderer(
            services,
            services.GetRequiredService<ILoggerFactory>());

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            await renderer.RenderComponentAsync<SessionTable>(
                ParameterView.FromDictionary(
                    new Dictionary<string, object?>
                    {
                        [nameof(SessionTable.FileName)] = "benchmark.har",
                        [nameof(SessionTable.TotalSessionCount)] = sessions.Count,
                        [nameof(SessionTable.AllSessions)] = sessions,
                        [nameof(SessionTable.Sessions)] = sessions,
                        [nameof(SessionTable.SelectedSessionId)] =
                            sessions.FirstOrDefault()?.Id,
                        [nameof(SessionTable.Query)] = new SessionQuery()
                    }));
        });
    }

    private static async Task<BenchmarkMeasurement> MeasureAsync(
        Func<Task> operation)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        using var sampler = new ManagedMemorySampler();
        var stopwatch = Stopwatch.StartNew();
        await operation();
        stopwatch.Stop();
        sampler.Stop();

        return new BenchmarkMeasurement(
            stopwatch.Elapsed.TotalMilliseconds,
            GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore,
            sampler.PeakBytes);
    }

    private sealed class BenchmarkJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            object?[]? args) =>
            ValueTask.FromResult(default(TValue)!);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args) =>
            ValueTask.FromResult(default(TValue)!);
    }

    private async Task<PerformanceBudgetFile> LoadBudgetsAsync(
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(options.BudgetPath);
        return await JsonSerializer.DeserializeAsync<PerformanceBudgetFile>(
                stream,
                BenchmarkJson.Options,
                cancellationToken)
            ?? throw new InvalidDataException(
                "The performance budget file is empty.");
    }

    private static IReadOnlyList<string> EvaluateBudgets(
        IEnumerable<BenchmarkScenarioResult> results,
        PerformanceBudgetFile budgetFile)
    {
        var failures = new List<string>();
        foreach (var result in results)
        {
            var budget = budgetFile.Budgets.SingleOrDefault(candidate =>
                string.Equals(
                    candidate.Format,
                    result.Format,
                    StringComparison.OrdinalIgnoreCase)
                && candidate.SessionCount == result.SessionCount);
            if (budget is null)
            {
                failures.Add(
                    $"No budget exists for {result.Format} "
                    + $"{result.SessionCount:N0} sessions.");
                continue;
            }

            Check(
                failures,
                result,
                "import",
                result.Import.ElapsedMilliseconds,
                budget.MaximumImportMilliseconds);
            Check(
                failures,
                result,
                "analysis",
                result.Analysis.ElapsedMilliseconds,
                budget.MaximumAnalysisMilliseconds);
            Check(
                failures,
                result,
                "summary",
                result.Summary.ElapsedMilliseconds,
                budget.MaximumSummaryMilliseconds);
            if (result.InitialRender is not null)
            {
                Check(
                    failures,
                    result,
                    "initial render",
                    result.InitialRender.ElapsedMilliseconds,
                    budget.MaximumInitialRenderMilliseconds);
            }
            Check(
                failures,
                result,
                "filter",
                result.Filter.ElapsedMilliseconds,
                budget.MaximumFilterMilliseconds);
            Check(
                failures,
                result,
                "sort",
                result.Sort.ElapsedMilliseconds,
                budget.MaximumSortMilliseconds);

            var peakManagedBytes = new[]
            {
                result.Import.PeakManagedBytes,
                result.Analysis.PeakManagedBytes,
                result.Summary.PeakManagedBytes,
                result.InitialRender?.PeakManagedBytes ?? 0,
                result.Filter.PeakManagedBytes,
                result.Sort.PeakManagedBytes
            }.Max();
            if (peakManagedBytes > budget.MaximumPeakManagedBytes)
            {
                failures.Add(
                    $"{result.Format} {result.SessionCount:N0}: peak managed "
                    + $"{FormatBytes(peakManagedBytes)} exceeded "
                    + $"{FormatBytes(budget.MaximumPeakManagedBytes)}.");
            }
        }

        return failures;
    }

    private static void Check(
        ICollection<string> failures,
        BenchmarkScenarioResult result,
        string metric,
        double actual,
        double maximum)
    {
        if (actual > maximum)
        {
            failures.Add(
                $"{result.Format} {result.SessionCount:N0}: {metric} "
                + $"{actual:N0} ms exceeded {maximum:N0} ms.");
        }
    }

    private static string FormatBytes(long bytes) =>
        $"{bytes / 1024d / 1024d:N0} MiB";

    private static TraceAnalysisEngine CreateAnalysisEngine()
    {
        var services = new ServiceCollection();
        services.AddSingleton<LegacyRulesetData>();
        services.AddSingleton<RulesetManifestProvider>();
        using var provider = services.BuildServiceProvider();
        var rules = typeof(ITraceRule).Assembly
            .GetTypes()
            .Where(type =>
                !type.IsAbstract
                && !type.IsInterface
                && typeof(ITraceRule).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .Select(type => (ITraceRule)ActivatorUtilities.CreateInstance(
                provider,
                type))
            .ToArray();

        return new TraceAnalysisEngine(new RuleCatalog(
            rules,
            provider.GetRequiredService<RulesetManifestProvider>()));
    }
}

internal sealed class ManagedMemorySampler : IDisposable
{
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Thread _thread;
    private long _peakBytes = GC.GetTotalMemory(forceFullCollection: false);

    public ManagedMemorySampler()
    {
        _thread = new Thread(Sample)
        {
            IsBackground = true,
            Name = "M365Trace benchmark memory sampler"
        };
        _thread.Start();
    }

    public long PeakBytes => Interlocked.Read(ref _peakBytes);

    public void Stop()
    {
        if (_cancellation.IsCancellationRequested)
        {
            return;
        }

        _cancellation.Cancel();
        _thread.Join();
        ObserveMemory();
    }

    public void Dispose()
    {
        Stop();
        _cancellation.Dispose();
    }

    private void Sample()
    {
        while (!_cancellation.IsCancellationRequested)
        {
            ObserveMemory();
            Thread.Sleep(5);
        }
    }

    private void ObserveMemory()
    {
        var current = GC.GetTotalMemory(forceFullCollection: false);
        var observed = Interlocked.Read(ref _peakBytes);
        while (current > observed)
        {
            var original = Interlocked.CompareExchange(
                ref _peakBytes,
                current,
                observed);
            if (original == observed)
            {
                break;
            }

            observed = original;
        }
    }
}
