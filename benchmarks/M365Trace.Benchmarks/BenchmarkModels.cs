using System.Text.Json;

namespace M365Trace.Benchmarks;

internal sealed record BenchmarkReport
{
    public int SchemaVersion { get; init; } = 1;

    public required DateTimeOffset CreatedAt { get; init; }

    public required BenchmarkEnvironment Environment { get; init; }

    public required IReadOnlyList<BenchmarkScenarioResult> Scenarios
    {
        get;
        init;
    }

    public required IReadOnlyList<string> BudgetFailures { get; init; }
}

internal sealed record BenchmarkEnvironment(
    string OperatingSystem,
    string Framework,
    int ProcessorCount,
    long AvailableMemoryBytes);

internal sealed record BenchmarkScenarioResult
{
    public required string Format { get; init; }

    public required int SessionCount { get; init; }

    public required long FixtureBytes { get; init; }

    public required BenchmarkMeasurement Import { get; init; }

    public required BenchmarkMeasurement Analysis { get; init; }

    public required BenchmarkMeasurement Summary { get; init; }

    public BenchmarkMeasurement? InitialRender { get; init; }

    public required BenchmarkMeasurement Filter { get; init; }

    public required BenchmarkMeasurement Sort { get; init; }

    public required int FilteredSessionCount { get; init; }

    public required int SummarySessionCount { get; init; }
}

internal sealed record BenchmarkMeasurement(
    double ElapsedMilliseconds,
    long AllocatedBytes,
    long PeakManagedBytes);

internal sealed record PerformanceBudgetFile
{
    public int SchemaVersion { get; init; }

    public IReadOnlyList<PerformanceBudget> Budgets { get; init; } = [];
}

internal sealed record PerformanceBudget
{
    public required string Format { get; init; }

    public required int SessionCount { get; init; }

    public required double MaximumImportMilliseconds { get; init; }

    public required double MaximumAnalysisMilliseconds { get; init; }

    public required double MaximumSummaryMilliseconds { get; init; }

    public required double MaximumInitialRenderMilliseconds { get; init; }

    public required double MaximumFilterMilliseconds { get; init; }

    public required double MaximumSortMilliseconds { get; init; }

    public required long MaximumPeakManagedBytes { get; init; }
}

internal static class BenchmarkJson
{
    public static JsonSerializerOptions Options { get; } = new(
        JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
}
