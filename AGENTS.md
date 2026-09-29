# PROJECT KNOWLEDGE BASE

**Generated:** 2026-09-28T12:52:41Z
**Commit:** c11db45
**Branch:** main

## OVERVIEW

FunnySharp: pragmatic, BCL-first functional-programming library whose shipping code is C# 13 / .NET 10 (`net10.0`). F# appears only in the development harness (root `build.fsx`, `eng/harness/*.fs`, `tests/FunnySharp.Harness.Tests/`) - it never ships. Feature APIs land only when a goal defines behavior plus verification evidence.

## STRUCTURE

```
funnysharp/
├── src/                  # 4 projects: core nupkg (zero runtime deps), Roslyn analyzers, code fixes, AspNetCore package
├── tests/                # xUnit v3 suites + Compatibility (outside FunnySharp.slnx) + FunnySharp.Harness.Tests (F#)
├── eng/                  # F# harness: root build.fsx + harness/*.fs implement every gate; release-protocol.json; evaluation/ harness
├── docs/                 # product contract, grammar, per-carrier guides; next-stage/ = Goal 14 evidence-set workflow; goals/ frozen contracts
├── benchmarks/           # BenchmarkDotNet suites; JSON receipts bound to eng/performance manifests
├── examples/             # executable examples + DocumentationSamples (byte-exact docs snippet contract)
├── .github/workflows/    # release.yml (the only release gate) + tooling.yml (informational 3-OS matrix)
└── FunnySharp.slnx       # 13 projects; excludes tests/FunnySharp.Compatibility
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Carrier APIs (Option/Result/UnitResult/Validation/Effect) | `src/FunnySharp/` | one public type-family per file |
| Analyzers + code fixes (FS1001-FS1005) | `src/FunnySharp.Analyzers/`, `src/FunnySharp.Analyzers.CodeFixes/` | ship inside core nupkg `analyzers/dotnet/cs` |
| ASP.NET Core mapping | `src/FunnySharp.AspNetCore/` | separate nupkg, ~21 `HttpResultExtensions` overloads |
| Release gate | `build.fsx` (`-p release`), `eng/release-protocol.json` | `benchmarkSkipped` mode is what CI runs |
| Performance verification | `build.fsx` (`-p verify-performance`), `eng/performance/` | allocation budgets block; timing directional |
| Goal evaluation | `build.fsx` (`-p eval-*`), `eng/evaluation/` | 5 task areas, style-neutral Contract.cs seam |
| Capability decisions | `docs/next-stage/` | Goal 14 evidence set; later goals append only |
| Authoritative contracts | `docs/product-contract.md`, `docs/grammar.md` | boundaries + verb vocabulary |
| CI | `.github/workflows/release.yml` | win-x64 canonical, then linux/osx/consumer jobs |

## CODE MAP

| Symbol | Type | Location | Refs | Role |
|--------|------|----------|------|------|
| `Option<T>` | struct | `src/FunnySharp/Option.cs` | core carrier | presence/absence; no `Where`, no `Maybe<T>` |
| `Result<TValue,TError>` | struct | `src/FunnySharp/Result.cs` | core carrier | success/typed failure, fail-fast |
| `UnitResult<TError>` | struct | `src/FunnySharp/UnitResult.cs` | core carrier | value-less command outcome |
| `Validation<TValue,TError>` | struct | `src/FunnySharp/Validation.cs` | core carrier | collects all errors, deterministic order |
| `Effect<T>` / `Effect<TEnvironment,T>` | struct | `src/FunnySharp/Effect.cs` | deferred boundary | composes via ValueTask, no DI runtime |
| `HttpResultExtensions` | static class | `src/FunnySharp.AspNetCore/` | ~21 overloads | carriers -> `IResult`/`ProblemDetails` |
| `Choose` / `Scan` | extensions | `src/FunnySharp/EnumerablePipelineExtensions.cs`, `AsyncEnumerablePipelineExtensions.cs` (+ `SpanPipelineExtensions.cs` for `Choose`) | fused Option filter-map, running aggregate | sync + async streams |
| FS1001-FS1005 | analyzers | `src/FunnySharp.Analyzers/` | dogfooded everywhere | uninitialized carriers, discarded outcomes, ignored TryGet, blocked ValueTask, sync-disposed IAsyncDisposable |

(Ref counts unmeasured - LSP symbol census not run at root; roles from subtree digests.)

## CONVENTIONS

- SDK pinned `10.0.400` (`global.json`, `rollForward: latestPatch`); test runner `Microsoft.Testing.Platform`.
- Root `Directory.Build.props`: `ImplicitUsings`, `Nullable`, `TreatWarningsAsErrors`, `Deterministic` all on - a warning is a build failure.
- Tests: xUnit v3 (`xunit.v3` 4.0.0), attribute-only, self-executing exes; zero skipped tests is an invariant.
- Naming contract: `...Async` suffix on async members; `OrNone` for absence-translating bridges; file name = primary type; file-scoped namespaces.
- Restores are locked: `packages.lock.json` committed everywhere.
- Docs: lowercase-hyphen filenames; guides close with `## Deliberate Boundaries`; 10 primary guides carry byte-exact `documentation-sample:` snippets mirrored in `examples/FunnySharp.DocumentationSamples`.
- Shared test source (`tests/Shared/`) links via Compile-include, never ProjectReference.
- Development gates run through the F# harness: `dotnet fsi build.fsx -- -p <pipeline> [args]` (see [docs/harness.md](docs/harness.md)).

## ANTI-PATTERNS (THIS PROJECT)

- NEVER add `Maybe<T>` (second absence carrier), `Where` on carriers, or AD-6..AD-9 type names - frozen by `AdvancedPatternCurationTests`.
- NEVER make `src/FunnySharp` depend on ASP.NET Core (goal 0001) or add any runtime dependency.
- NEVER sync-block on async work (goal 0009); use the `...Async`/ValueTask variants.
- NEVER hand-edit machine-generated dirs: `docs/next-stage/inventory/generated/`, `docs/goals/archive/`, `docs/release-evidence/`, `docs/next-stage/call-sites-code/`, `docs/next-stage/review/` (append only).
- NEVER treat tooling.yml CI as the release gate - only release.yml contexts gate a release.
- Deliberate-misuse test snippets must wrap in `#pragma warning disable FS100x`; example projects must build with zero FS#### diagnostics.

## UNIQUE STYLES

- Analyzers ship inside the core nupkg - consumers get diagnostics with an empty dependency group, no extra install.
- Evaluation harness is model-agnostic: prompt/template/tests anatomy + style-neutral `Contract.cs` seam, results recorded under `eng/evaluation/results/`.
- `docs/next-stage/` pins provenance via SHA256 pin paragraphs, not YAML front matter.
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
# docs snippets byte-compare (10 primary guides, 44 snippets)
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
- Timing is informational until a fixed-hardware runner exists; allocation budgets are the blocking gate.
- No .NET 11 preview support; `net11.0` targeting waits for GA (TODO.md).
