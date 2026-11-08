# PROJECT KNOWLEDGE BASE

**Generated:** 2026-09-28T12:52:41Z
**Commit:** c11db45
**Branch:** main

## OVERVIEW

FunnySharp: pragmatic, BCL-first functional-programming library whose shipping code is C# 13 / .NET 10 (`net10.0`). F# appears only in the development harness (root `build.fsx`, `eng/harness/*.fs`, `tests/FunnySharp.Harness.Tests/`) - it never ships. Feature APIs land only when a goal defines behavior plus verification evidence.

Read [Core Design](docs/core-design.md) when shaping a goal or choosing an abstraction. It supplies the shared design intent and trade-off judgment; the product contract, grammar, and selected goal retain their existing authority over shipped behavior and completion.

## STRUCTURE

```
funnysharp/
├── src/                  # 4 projects: core nupkg (zero runtime deps), Roslyn analyzers, code fixes, AspNetCore package
├── tests/                # xUnit v3 suites + measurement harnesses + Compatibility (outside FunnySharp.slnx) + FunnySharp.Harness.Tests (F#)
├── eng/                  # F# harness: root build.fsx + harness/*.fs implement every gate; release-protocol.json; evaluation/ harness
├── docs/                 # product contract, grammar, per-carrier guides; next-stage/ = Goal 14 evidence-set workflow; goals/ frozen contracts
├── benchmarks/           # BenchmarkDotNet suites; isolated GreenDonut comparison; JSON receipts bound to eng/performance manifests
├── examples/             # executable examples + DocumentationSamples (byte-exact docs snippet contract)
├── .github/workflows/    # release.yml (manual release gate) + tooling.yml (3-OS PR merge gate)
└── FunnySharp.slnx       # 29 projects; excludes tests/FunnySharp.Compatibility
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Carrier APIs (Option/Result/UnitResult/Validation/Effect) | `src/FunnySharp/` | one public type-family per file |
| Reusable checks | `src/FunnySharp/CheckExtensions.cs`, `docs/checks.md` | value-preserving `Check`; ordered independent `CheckAll`; no `Validation.Bind` |
| Fallible reduction | `src/FunnySharp/FallibleReductionExtensions.cs`, `src/FunnySharp/AsyncFallibleReductionExtensions.cs`, `docs/fallible-reduction.md` | seeded `Result` fold; first failure stops source enumeration; sync/async source forms |
| One-pass summaries | `src/FunnySharp/Summary.cs`, `src/FunnySharp/SummaryPlan.cs`, `src/FunnySharp/ExtremumPolicy.cs`, `docs/one-pass-summaries.md` | independent count/sum/extrema/domain summaries; `Zip`/`MapResult`; one traversal, local component state; sync calculation reused for async acquisition |
| Cardinality-checked keyed joins | `src/FunnySharp/LookupError.cs`, `LookupErrorKind.cs`, `LookupIndex.cs`, `UniqueLookupIndex.cs`, `LookupExtensions.cs`, `docs/cardinality-joins.md` | explicit comparer; many and complete-input-unique indexes; structured null/duplicate/missing positions; buffered all-or-nothing and streaming per-row shapes; no Cartesian join |
| Typed nested data reshaping | `src/FunnySharp/NestedDataExtensions.cs`, `RowOrigin.cs`, `CartesianRowOrigin.cs`, `NestedRowError.cs`, `NestedRowErrorKind.cs`, `docs/nested-data-reshaping.md` | single/aligned/explicit-Cartesian expansion; null/empty/unequal/rejection policies; by-value provenance; sync + async parent sources; no serializer or DataFrame layer |
| Composable domain programs and simulation | `src/FunnySharp/DomainProgram.cs`, `ProgramSimulation.cs`, `EmittedCommand.cs`, `SimulationDelivery.cs`, `CommandExecutionFailure.cs`, `docs/domain-programs.md` | pure business-program description over `StateMachine`; `Lift`/`OrElse`/`Replay`; explicit-time single-threaded simulation with ordered commands, correlation, and execution failures; no runtime or scheduler |
| Bounded per-key concurrency | `src/FunnySharp/KeyedParallelAsyncEnumerableExtensions.cs`, `docs/concurrency.md` | one active operation per caller key; explicit `maxConcurrency` and `maxBufferedItems`; completion-order outcomes across keys; lifecycle release and drain; no global source order or scheduler |
| Batched reads (source/requirement/scope) | `src/FunnySharp/Batch*.cs`, `docs/batched-reads.md` | explicit source batch delegates; bounded scope-local cache, no ambient cache |
| Tagged domain values | `src/FunnySharp/Domain*.cs`, `docs/domain-values.md` | phantom marker, validated boundary, derived contract/decoder; no union/serializer/runtime dependency |
| Analyzers + code fixes (FS1001-FS1005) | `src/FunnySharp.Analyzers/`, `src/FunnySharp.Analyzers.CodeFixes/` | ship inside core nupkg `analyzers/dotnet/cs` |
| ASP.NET Core mapping | `src/FunnySharp.AspNetCore/` | separate nupkg, ~21 `HttpResultExtensions` overloads |
| Release gate | `build.fsx` (`-p release`), `eng/release-protocol.json` | `benchmarkSkipped` mode is what CI runs |
| Performance verification | `build.fsx` (`-p verify-performance`), `eng/performance/` | allocation budgets block; timing directional |
| Goal evaluation | `build.fsx` (`-p eval-*`), `eng/evaluation/` | 5 task areas, style-neutral Contract.cs seam |
| Capability decisions | `docs/next-stage/` | Goal 14 evidence set; later goals append only |
| Compile-time fusion research | `docs/compile-time-fused-execution-evidence.md` | Goal 39 ADAPT summary-only; no public capability shipped |
| Design intent and abstraction judgment | `docs/core-design.md` | enduring direction for goal-driven work |
| Authoritative contracts | `docs/product-contract.md`, `docs/grammar.md` | boundaries + verb vocabulary |
| PR merge gate | `.github/workflows/tooling.yml` | required `tooling-gate`; light 3-OS checks |

## CODE MAP

| Symbol | Type | Location | Refs | Role |
|--------|------|----------|------|------|
| `Option<T>` | struct | `src/FunnySharp/Option.cs` | core carrier | presence/absence; no `Where`, no `Maybe<T>` |
| `Result<TValue,TError>` | struct | `src/FunnySharp/Result.cs` | core carrier | success/typed failure, fail-fast |
| `UnitResult<TError>` | struct | `src/FunnySharp/UnitResult.cs` | core carrier | value-less command outcome |
| `Validation<TValue,TError>` | struct | `src/FunnySharp/Validation.cs` | core carrier | collects all errors, deterministic order |
| `CheckExtensions` | static class | `src/FunnySharp/CheckExtensions.cs` | check workflow | fail-fast value retention + independent error accumulation; Task/ValueTask forms |
| `Effect<T>` / `Effect<TEnvironment,T>` | struct | `src/FunnySharp/Effect.cs` | deferred boundary | composes via ValueTask, no DI runtime |
| `BatchRequirement<TValue,TError>` / `BatchScope` | sealed classes | `src/FunnySharp/BatchRequirement.cs`, `src/FunnySharp/BatchScope.cs` | typed reads | deferred composition + bounded scope-local batch execution |
| `HttpResultExtensions` | static class | `src/FunnySharp.AspNetCore/` | ~21 overloads | carriers -> `IResult`/`ProblemDetails` |
| `Choose` / `Scan` | extensions | `src/FunnySharp/EnumerablePipelineExtensions.cs`, `AsyncEnumerablePipelineExtensions.cs` (+ `SpanPipelineExtensions.cs` for `Choose`) | fused Option filter-map, running aggregate; sync + async streams |
| `AggregateResult` | extensions | `src/FunnySharp/FallibleReductionExtensions.cs`, `AsyncFallibleReductionExtensions.cs` | fallible seeded fold; first typed failure stops source consumption |
| `Summary` / `Summary<...>` | factory + description | `src/FunnySharp/Summary.cs`, `src/FunnySharp/SummaryPlan.cs` | reusable independent summaries; `Zip`/`MapResult`; one traversal; checked built-ins; total folds |
| `LookupIndex` / `UniqueLookupIndex` / `LookupError` | keyed association + diagnostic | `src/FunnySharp/LookupIndex.cs`, `UniqueLookupIndex.cs`, `LookupError.cs`, `LookupExtensions.cs` | explicit comparer; source-ordered occurrences; complete-input or matched-key uniqueness; positions; no expansion |
| `Expand` / `ExpandAligned` / `ExpandCartesian` / `RowOrigin` | sequence expansion + provenance | `src/FunnySharp/NestedDataExtensions.cs`, `RowOrigin.cs`, `CartesianRowOrigin.cs`, `NestedRowError.cs` | one row per child, exact positional pairing, explicit product; buffered all-or-nothing `Validation`; record/element positions; null child lists fail, empty lists yield no rows |
| `DomainProgram` / `Lift` / `ProgramSimulation` | pure business program + deterministic driver | `src/FunnySharp/DomainProgram.cs`, `ProgramSimulation.cs`, `EmittedCommand.cs`, `SimulationDelivery.cs`, `CommandExecutionFailure.cs` | pure `Update`; substate getter/setter, `Option` route, parent-aware command lift; explicit time, caller random, replies, ordered commands, correlated deliveries, execution failures; no command execution |
| `SelectParallelByKeyCompletionOrderValueAsync` | extension | `src/FunnySharp/KeyedParallelAsyncEnumerableExtensions.cs` | per-key serialized bounded processing | one started operation per key, `maxConcurrency`/`maxBufferedItems` bounds, explicit comparer, typed result delivery, completion-order cross-key outcomes |
| FS1001-FS1005 | analyzers | `src/FunnySharp.Analyzers/` | dogfooded everywhere | uninitialized carriers, discarded outcomes, ignored TryGet, blocked ValueTask, sync-disposed IAsyncDisposable |

(Ref counts unmeasured - LSP symbol census not run at root; roles from subtree digests.)

## CONVENTIONS

- SDK pinned `10.0.400` (`global.json`, `rollForward: latestPatch`); test runner `Microsoft.Testing.Platform`.
- Root `Directory.Build.props`: `ImplicitUsings`, `Nullable`, `TreatWarningsAsErrors`, `Deterministic` all on - a warning is a build failure.
- Tests: xUnit v3 (`xunit.v3` 4.0.0), attribute-only, self-executing exes; zero skipped tests is an invariant.
- Naming contract: `...Async` suffix on async members; `OrNone` for absence-translating bridges; file name = primary type; file-scoped namespaces.
- Restores are locked: `packages.lock.json` committed everywhere.
- Docs: lowercase-hyphen filenames; guides close with `## Deliberate Boundaries`; 15 primary guides carry byte-exact `documentation-sample:` snippets mirrored in `examples/FunnySharp.DocumentationSamples`.
- Shared test source (`tests/Shared/`) links via Compile-include, never ProjectReference.
- Development gates run through the F# harness: `dotnet fsi build.fsx -- -p <pipeline> [args]` (see [docs/harness.md](docs/harness.md)).

## ANTI-PATTERNS (THIS PROJECT)

- NEVER add `Maybe<T>` (second absence carrier), `Where` on carriers, or AD-6..AD-9 type names - frozen by `AdvancedPatternCurationTests`.
- NEVER make `src/FunnySharp` depend on ASP.NET Core (goal 0001) or add any runtime dependency.
- NEVER sync-block on async work (goal 0009); use the `...Async`/ValueTask variants.
- NEVER add `Bind`/`SelectMany` to `Validation` to share check syntax; dependent rules stay on fail-fast `Check`, `Bind`, or ordinary control flow.
- NEVER make `AggregateResult` continue source enumeration after a typed failure, materialize intermediate results, clone/roll back caller accumulators, or add automatic parallel reduction.
- NEVER add a public typeclass/summary-interface hierarchy, automatic parallelism, LINQ replacement, compile-time fusion, or an early-exit/partial-result protocol to `Summary`; every component observes every source item, and `EvaluateAsync` changes only input acquisition.
- NEVER let strict joins expand duplicate lookup keys into multiple output rows, infer a comparer from a dictionary interface, or claim complete-input uniqueness from the streaming shape; `GetMany` is the explicit expansion primitive.
- NEVER treat a null child list as empty, truncate unequal `ExpandAligned` lengths, infer a Cartesian product from data, or return partial nested rows; `ExpandCartesian` is the explicit product.
- NEVER execute commands during `DomainProgram.Replay` or `ProgramSimulation.Replay`, read ambient time/randomness in the simulation, add a scheduler/actor/persistence layer, or make `Lift` copy the parent outside the caller's setter; commands are values and the simulation is an explicit single-threaded test driver.
- NEVER hand-edit machine-generated dirs: `docs/next-stage/inventory/generated/`, `docs/goals/archive/`, `docs/release-evidence/`, `docs/next-stage/call-sites-code/`, `docs/next-stage/review/` (append only).
- NEVER treat tooling.yml CI as the release gate - only release.yml contexts gate a release.
- Deliberate-misuse test snippets must wrap in `#pragma warning disable FS100x`; example projects must build with zero FS#### diagnostics.

## UNIQUE STYLES

- Analyzers ship inside the core nupkg - consumers get diagnostics with an empty dependency group, no extra install.
- Evaluation harness is model-agnostic: prompt/template/tests anatomy + style-neutral `Contract.cs` seam, results recorded under `eng/evaluation/results/`.
- Use Git for source revision and change identity. Do not compute or verify custom SHA file, package, binary, log, receipt, or snapshot fingerprints. Frozen historical provenance remains read-only.
- Decision vocabulary: adopt/adapt/defer/reject (candidates), keep/redesign/experimental/remove (existing).

## COMMANDS

```bash
# every development gate (see docs/harness.md for all pipelines)
dotnet fsi build.fsx -- -p build              # dotnet build FunnySharp.slnx
dotnet fsi build.fsx -- -p test               # dotnet test FunnySharp.slnx
dotnet pack FunnySharp.slnx -c Release        # pack all nupkgs
dotnet fsi build.fsx -- -p format             # formatter gate (C# only; F# is not format-checked)
dotnet fsi build.fsx -- -p verify-tooling     # local pre-check
# release protocol (CI runs this with -SkipBenchmarks)
dotnet fsi build.fsx -- -p release -SkipBenchmarks
# docs snippets byte-compare (15 primary guides, 81 snippets)
dotnet fsi build.fsx -- -p verify-docs-snippets
# benchmark + performance verify
dotnet run --project benchmarks/FunnySharp.Benchmarks -c Release -- --filter '*' --artifacts <dir>
dotnet fsi build.fsx -- -p verify-performance -ReceiptDirectory <dir>
# evaluation harness
dotnet fsi build.fsx -- -p eval-prep-feed
dotnet fsi build.fsx -- -p eval-verify --task <area> --style <style> --run-dir <run-dir>
```

## NOTES

- Child knowledge bases: `tests/FunnySharp.VerticalSlice/AGENTS.md` (Goal 22 package-consumer vertical slice + its idiomatic-C# comparison + measurement harness),
  `src/FunnySharp/AGENTS.md`, `src/FunnySharp.Analyzers/AGENTS.md`, `src/FunnySharp.AspNetCore/AGENTS.md`, `tests/FunnySharp.Tests/AGENTS.md`, `tests/FunnySharp.Analyzers.Tests/AGENTS.md`, `tests/FunnySharp.AspNetCore.Tests/AGENTS.md`, `tests/FunnySharp.Compatibility/AGENTS.md`, `eng/AGENTS.md`, `eng/tools/AGENTS.md`, `eng/evaluation/AGENTS.md`, `benchmarks/AGENTS.md`, `examples/AGENTS.md`, `examples/FunnySharp.Examples/AGENTS.md`, `examples/FunnySharp.DocumentationSamples/AGENTS.md`, `docs/AGENTS.md`, `docs/goals/AGENTS.md`, `docs/plans/AGENTS.md`, `docs/next-stage/AGENTS.md` (+ analysis/, inventory/, inventory/generated/, call-sites-code/), `.github/workflows/AGENTS.md`.
- `docs/next-stage/decision-record.md` is a pinned Goal 14 baseline - later goals append, never modify.
- Heavy tests, full pre-check/release, package/platform/AOT checks, evaluation cohorts and benchmarks run only on explicit human request. Do not auto-run them in CI or Agent iterations. Daily checks use incremental project builds and small relevant tests.
- Timing is informational until a fixed-hardware runner exists; allocation budgets apply to manually requested measurements.
- No .NET 11 preview support; `net11.0` targeting waits for GA (TODO.md).
