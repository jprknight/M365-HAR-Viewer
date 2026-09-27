namespace M365Trace.Benchmarks;

internal static class BenchmarkConsole
{
    public static void WriteReport(
        BenchmarkReport report,
        string outputPath)
    {
        Console.WriteLine();
        Console.WriteLine(
            "Format         Sessions    Import  Analysis  Summary  Render"
            + "  Filter   Sort   Peak");

        foreach (var scenario in report.Scenarios)
        {
            var peak = new[]
            {
                scenario.Import.PeakManagedBytes,
                scenario.Analysis.PeakManagedBytes,
                scenario.Summary.PeakManagedBytes,
                scenario.InitialRender?.PeakManagedBytes ?? 0,
                scenario.Filter.PeakManagedBytes,
                scenario.Sort.PeakManagedBytes
            }.Max();
            Console.WriteLine(
                $"{scenario.Format,-14}"
                + $"{scenario.SessionCount,8:N0}"
                + $"{scenario.Import.ElapsedMilliseconds,10:N0}"
                + $"{scenario.Analysis.ElapsedMilliseconds,10:N0}"
                + $"{scenario.Summary.ElapsedMilliseconds,9:N0}"
                + $"{FormatOptional(scenario.InitialRender),8}"
                + $"{scenario.Filter.ElapsedMilliseconds,8:N0}"
                + $"{scenario.Sort.ElapsedMilliseconds,7:N0}"
                + $"{peak / 1024d / 1024d,7:N0} MiB");
        }

        Console.WriteLine();
        Console.WriteLine($"Results: {outputPath}");
        if (report.BudgetFailures.Count == 0)
        {
            Console.WriteLine("Budgets: PASS");
            return;
        }

        Console.WriteLine("Budgets: FAIL");
        foreach (var failure in report.BudgetFailures)
        {
            Console.WriteLine($"  - {failure}");
        }
    }

    private static string FormatOptional(BenchmarkMeasurement? measurement) =>
        measurement is null
            ? "-"
            : measurement.ElapsedMilliseconds.ToString("N0");
}
