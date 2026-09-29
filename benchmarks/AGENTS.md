# benchmarks/ — performance measurement suites

**Earned:** score ~13 — distinct domain (BenchmarkDotNet suites + receipt protocol).
One file covers both suites: they share the manifest/receipt contract; neither project
warrants its own file (uniform one-class-per-API-slice layout).

## OVERVIEW

Two BenchmarkDotNet suites measuring allocation (blocking) and timing (directional);
results are custom JSON receipts bound to the tracked manifests in `eng/performance/`.

## WHERE TO LOOK

| Need | Location |
| --- | --- |
| Main suite (FunnySharp ops) | `FunnySharp.Benchmarks/` — 11 suites, one class per API slice: Option, Result, Collection, Sequence, Effect, Concurrency, DataPipeline, ImmutableUpdate, FunctionComposition, StateMachine, AspNetCore (HTTP mapping) |
| Shared receipt writer | `FunnySharp.Benchmarks/ReceiptExporterCore.cs` |
| Receipt binding | `FunnySharp.Benchmarks/AllocationReceiptExporter.cs` (registered into `ManualConfig`) |
| Competitor comparison | `FunnySharp.CompetitorBenchmarks/` — `OptionCarrierBenchmarks`, `ResultCarrierBenchmarks`; each `comparisonGroup` pairs a raw/direct baseline row with funcky / FSharp.Core competitor paths |
| Competitor receipts | `FunnySharp.CompetitorBenchmarks/CompetitorReceiptExporter.cs` |
| Budget policy | `eng/performance/baseline.json` (main) / `competitor-baseline.json` (competitor) — see `eng/AGENTS.md` |

## CONVENTIONS

- Output is a custom `schemaVersion = 1` JSON receipt (camelCase, indented, one trailing
  newline) — not BenchmarkDotNet's default markdown/CSV.
- Each `[Benchmark]` carries exactly ONE `[BenchmarkCategory]` (`.Single()`); each category
  has exactly one `Baseline = true` method.
- Receipt rows are ordinal-sorted Class → Category → Method → Parameters and bound to the
  manifest via policy / benchmarkInput / protocol SHA-256 fingerprints + host `environmentKey`.
- `TimingState`: `observed` / `below-resolution` (mean < 0.1 ns) / `unavailable`;
  allocation (`AllocatedBytesPerOperation`) is the decisive signal, timing is directional.
- Both csprojs: `Exe`, `IsPackable=false`, BenchmarkDotNet 0.15.8, locked restore
  (`packages.lock.json`), net10.0, in `FunnySharp.slnx`.
- Benchmarks are a developer-machine activity: CI runs the release with `-SkipBenchmarks`
  and verifies docs instead (see `.github/workflows/release.yml`).

## ANTI-PATTERNS

- NEVER let `check_action_pins`-style re-checks touch these suites' pins — n/a here, but
  likewise never bypass the receipt fingerprinting: a receipt without matching fingerprints
  fails the `verify-performance` pipeline.
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
