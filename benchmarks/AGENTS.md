# benchmarks/ — performance measurement suites

**Earned:** score ~13 — distinct domain (BenchmarkDotNet suites + receipt protocol).
One file covers both suites: they share the manifest/receipt contract; neither project
warrants its own file (uniform one-class-per-API-slice layout).

## OVERVIEW

Two BenchmarkDotNet suites measuring allocation (blocking) and timing (directional);
results are custom JSON receipts bound to the tracked manifests in `eng/performance/`.
`FunnySharp.DataLoaderComparison` is a separate, non-shipping evidence app comparing the batched-read
consumer with GreenDonut; it is not a benchmark gate and is never in a release artifact.

## WHERE TO LOOK

| Need | Location |
| --- | --- |
| Main suite (FunnySharp ops) | `FunnySharp.Benchmarks/` — 11 suites, one class per API slice: Option, Result, Collection, Sequence, Effect, Concurrency, DataPipeline, ImmutableUpdate, FunctionComposition, StateMachine, AspNetCore (HTTP mapping) |
| Shared receipt writer | `FunnySharp.Benchmarks/ReceiptExporterCore.cs` |
| Receipt binding | `FunnySharp.Benchmarks/AllocationReceiptExporter.cs` (registered into `ManualConfig`) |
| Competitor comparison | `FunnySharp.CompetitorBenchmarks/` — `OptionCarrierBenchmarks`, `ResultCarrierBenchmarks`; each `comparisonGroup` pairs a raw/direct baseline row with funcky / FSharp.Core competitor paths |
| Competitor receipts | `FunnySharp.CompetitorBenchmarks/CompetitorReceiptExporter.cs` |
| Batched-read comparison | `FunnySharp.DataLoaderComparison/` — isolated GreenDonut 16.7.3 consumer comparison; not in `FunnySharp.slnx` or any release artifact |
| Budget policy | `eng/performance/baseline.json` (main) / `competitor-baseline.json` (competitor) — see `eng/AGENTS.md` |

## CONVENTIONS

- Output is a custom `schemaVersion = 1` JSON receipt (camelCase, indented, one trailing
  newline) — not BenchmarkDotNet's default markdown/CSV.
- Each `[Benchmark]` carries exactly ONE `[BenchmarkCategory]` (`.Single()`); each category
  has exactly one `Baseline = true` method.
- Receipt rows are ordinal-sorted Class → Category → Method → Parameters. Git identifies
  the source revision; allocation policy and host `environmentKey` describe the comparison.
- `TimingState`: `observed` / `below-resolution` (mean < 0.1 ns) / `unavailable`;
  allocation (`AllocatedBytesPerOperation`) is the decisive signal, timing is directional.
- Both csprojs: `Exe`, `IsPackable=false`, BenchmarkDotNet 0.15.8, locked restore
  (`packages.lock.json`), net10.0, in `FunnySharp.slnx`.
- Benchmarks are explicit human-requested developer-machine commands. Agents and CI
  never start them during normal iterations. Heavy tests/release checks are manual too.

## ANTI-PATTERNS

- Use Git for source identity. Do not calculate or verify custom source, binary,
  policy, log or receipt checksums. Preserve semantic preflight and allocation checks.
- NEVER commit receipts as results without running them through `verify-performance`
  (or `-ObservationProposalPath` for observation-only proposals; policy is read-only).

## COMMANDS

```bash
# Main suite (run from repo root, then verify)
dotnet run --project benchmarks/FunnySharp.Benchmarks/FunnySharp.Benchmarks.csproj -c Release -- --filter '*' --artifacts <results-path>
dotnet fsi build.fsx -- -p verify-performance -ReceiptDirectory <results-path>

# Competitor suite
dotnet run --project benchmarks/FunnySharp.CompetitorBenchmarks/FunnySharp.CompetitorBenchmarks.csproj -c Release -- --filter '*' --artifacts <dir>
dotnet fsi build.fsx -- -p verify-performance -ManifestPath eng/performance/competitor-baseline.json -ReceiptDirectory <dir>

# Semantic preflights (validate benchmark equivalence before measuring)
dotnet run --project benchmarks/FunnySharp.Benchmarks/FunnySharp.Benchmarks.csproj -c Release -- --preflight
dotnet run --project benchmarks/FunnySharp.CompetitorBenchmarks/FunnySharp.CompetitorBenchmarks.csproj -c Release -- --preflight
```
