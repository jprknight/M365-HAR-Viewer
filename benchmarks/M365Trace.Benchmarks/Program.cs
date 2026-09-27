using System.Text.Json;
using M365Trace.Benchmarks;

var options = BenchmarkOptions.Parse(args);
var runner = new BenchmarkRunner(options);
var report = await runner.RunAsync();

Directory.CreateDirectory(Path.GetDirectoryName(options.OutputPath)!);
await using (var output = File.Create(options.OutputPath))
{
    await JsonSerializer.SerializeAsync(
        output,
        report,
        BenchmarkJson.Options);
}

BenchmarkConsole.WriteReport(report, options.OutputPath);
return report.BudgetFailures.Count == 0 || !options.EnforceBudgets ? 0 : 2;
