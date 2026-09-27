namespace M365Trace.Benchmarks;

internal sealed record BenchmarkOptions
{
    private static readonly int[] DefaultSessionCounts =
        [1_000, 10_000, 100_000];

    private static readonly BenchmarkFormat[] DefaultFormats =
        [BenchmarkFormat.Har, BenchmarkFormat.Saz];

    public required string RepositoryRoot { get; init; }

    public required string OutputPath { get; init; }

    public required string BudgetPath { get; init; }

    public IReadOnlyList<int> SessionCounts { get; init; } =
        DefaultSessionCounts;

    public IReadOnlyList<BenchmarkFormat> Formats { get; init; } =
        DefaultFormats;

    public bool EnforceBudgets { get; init; }

    public bool KeepFixtures { get; init; }

    public bool SkipRender { get; init; }

    public static BenchmarkOptions Parse(string[] args)
    {
        var repositoryRoot = FindRepositoryRoot();
        var outputPath = Path.Combine(
            repositoryRoot,
            "artifacts",
            "benchmarks",
            $"performance-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
        var budgetPath = Path.Combine(
            repositoryRoot,
            "benchmarks",
            "performance-budgets.json");
        IReadOnlyList<int> counts = DefaultSessionCounts;
        IReadOnlyList<BenchmarkFormat> formats = DefaultFormats;
        var enforce = false;
        var keepFixtures = false;
        var skipRender = false;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--counts":
                    counts = ParseCounts(GetValue(args, ref index));
                    break;
                case "--formats":
                    formats = ParseFormats(GetValue(args, ref index));
                    break;
                case "--output":
                    outputPath = ResolvePath(
                        repositoryRoot,
                        GetValue(args, ref index));
                    break;
                case "--budgets":
                    budgetPath = ResolvePath(
                        repositoryRoot,
                        GetValue(args, ref index));
                    break;
                case "--enforce-budgets":
                    enforce = true;
                    break;
                case "--keep-fixtures":
                    keepFixtures = true;
                    break;
                case "--skip-render":
                    skipRender = true;
                    break;
                case "--help":
                case "-h":
                    PrintHelp();
                    Environment.Exit(0);
                    break;
                default:
                    throw new ArgumentException(
                        $"Unknown benchmark argument '{args[index]}'.");
            }
        }

        return new BenchmarkOptions
        {
            RepositoryRoot = repositoryRoot,
            OutputPath = outputPath,
            BudgetPath = budgetPath,
            SessionCounts = counts,
            Formats = formats,
            EnforceBudgets = enforce,
            KeepFixtures = keepFixtures,
            SkipRender = skipRender
        };
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(
                    directory.FullName,
                    "M365-Trace-Analyzer.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Run the benchmark harness from the repository or one of its subdirectories.");
    }

    private static string GetValue(string[] args, ref int index)
    {
        index++;
        if (index >= args.Length)
        {
            throw new ArgumentException(
                $"Argument '{args[index - 1]}' requires a value.");
        }

        return args[index];
    }

    private static IReadOnlyList<int> ParseCounts(string value)
    {
        var counts = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries
                | StringSplitOptions.TrimEntries)
            .Select(item => int.TryParse(item, out var count) && count > 0
                ? count
                : throw new ArgumentException(
                    $"Invalid session count '{item}'."))
            .Distinct()
            .Order()
            .ToArray();

        return counts.Length > 0
            ? counts
            : throw new ArgumentException(
                "At least one session count is required.");
    }

    private static IReadOnlyList<BenchmarkFormat> ParseFormats(string value)
    {
        var formats = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries
                | StringSplitOptions.TrimEntries)
            .Select(item => item.ToLowerInvariant() switch
            {
                "har" => BenchmarkFormat.Har,
                "saz" => BenchmarkFormat.Saz,
                "saz-encrypted" => BenchmarkFormat.EncryptedSaz,
                _ => throw new ArgumentException(
                    $"Unsupported benchmark format '{item}'.")
            })
            .Distinct()
            .ToArray();

        return formats.Length > 0
            ? formats
            : throw new ArgumentException(
                "At least one benchmark format is required.");
    }

    private static string ResolvePath(string root, string path) =>
        Path.GetFullPath(
            Path.IsPathRooted(path)
                ? path
                : Path.Combine(root, path));

    private static void PrintHelp()
    {
        Console.WriteLine(
            """
            M365 Trace Analyzer performance benchmark

              --counts 1000,10000,100000
              --formats har,saz,saz-encrypted
              --output <json-path>
              --budgets <json-path>
              --enforce-budgets
              --keep-fixtures
              --skip-render
            """);
    }
}

internal enum BenchmarkFormat
{
    Har,
    Saz,
    EncryptedSaz
}
