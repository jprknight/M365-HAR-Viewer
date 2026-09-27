# Performance benchmarks

The benchmark harness generates representative traces and exercises the real
HAR and SAZ importers, ruleset, trace summary, session-table renderer, filtering,
and sorting. Generated fixtures and result files are written under
`artifacts/benchmarks` and are not committed.

Run the complete Release benchmark:

```powershell
dotnet run --project .\benchmarks\M365Trace.Benchmarks\M365Trace.Benchmarks.csproj `
  --configuration Release -- `
  --enforce-budgets
```

Run a smaller development scenario:

```powershell
dotnet run --project .\benchmarks\M365Trace.Benchmarks\M365Trace.Benchmarks.csproj `
  --configuration Release -- `
  --counts 1000,10000 `
  --formats har,saz
```

Available formats are `har`, `saz`, and `saz-encrypted`. Encrypted SAZ is an
explicit opt-in workload because encrypting hundreds of thousands of ZIP
entries is disproportionately expensive:

```powershell
dotnet run --project .\benchmarks\M365Trace.Benchmarks\M365Trace.Benchmarks.csproj `
  --configuration Release -- `
  --counts 1000 `
  --formats saz-encrypted `
  --enforce-budgets
```

Each scenario records:

- fixture size
- import time
- ruleset analysis time
- trace-summary construction time
- initial server-side `SessionTable` render time
- combined content and structured-filter latency
- sort latency
- allocated bytes and sampled peak managed memory for each operation

Fixture generation is excluded from measured operation times. Import includes
opening and parsing the generated file. Initial render uses ASP.NET Core's
server-side `HtmlRenderer` and does not serialize the completed render tree to
a string.

## Baseline

The initial September 27, 2026 baseline was captured with .NET 10.0.12 on
Windows using 16 logical processors. Times are milliseconds and peak is the
highest sampled managed-memory value across the measured operations.

| Format | Sessions | Fixture | Import | Analysis | Summary | Render | Filter | Sort | Peak |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| HAR | 1,000 | 0.6 MiB | 86 | 46 | 13 | 92 | 7 | 1 | 15 MiB |
| SAZ | 1,000 | 0.4 MiB | 236 | 30 | 1 | 6 | 1 | 1 | 31 MiB |
| Encrypted SAZ | 1,000 | 0.4 MiB | 3,286 | 42 | 13 | 93 | 7 | 1 | 20 MiB |
| HAR | 10,000 | 6.1 MiB | 514 | 364 | 23 | 75 | 5 | 40 | 130 MiB |
| SAZ | 10,000 | 4.0 MiB | 1,709 | 227 | 9 | 67 | 6 | 17 | 173 MiB |
| HAR | 100,000 | 61.4 MiB | 1,512 | 1,069 | 93 | 602 | 19 | 105 | 1,243 MiB |
| SAZ | 100,000 | 41.0 MiB | 6,951 | 997 | 71 | 573 | 20 | 88 | 1,565 MiB |

The baseline does not justify paging or virtualization on latency alone.
However, rendering 100,000 sessions retains more than 1 GiB of managed memory,
and the SAZ import allocates substantially more transient memory than HAR.
Those measurements should guide the next scale work rather than adding
virtualization speculatively.

The versioned budgets are in `benchmarks/performance-budgets.json`. They are
initial regression ceilings rather than optimization targets. Change a budget
only with a recorded benchmark result and an explanation of the intended
behavior change.

Use `--skip-render` only for importer-focused diagnostics. Use
`--keep-fixtures` when a generated HAR or SAZ is needed for separate profiling.
