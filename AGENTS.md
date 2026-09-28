# PROJECT KNOWLEDGE BASE

**Generated:** 2026-09-28T12:52:41Z
**Commit:** c11db45
**Branch:** main

## OVERVIEW

FunnySharp: pragmatic, BCL-first functional-programming library written entirely in C# 13 / .NET 10 (`net10.0`) - despite the name, no F# anywhere. Feature APIs land only when a goal defines behavior plus verification evidence.

## STRUCTURE

```
funnysharp/
├── src/                  # 4 projects: core nupkg (zero runtime deps), Roslyn analyzers, code fixes, AspNetCore package
├── tests/                # xUnit v3 suites + Compatibility (outside FunnySharp.slnx, PowerShell-run)
├── eng/                  # release/performance protocol: Run-Release.ps1, release-protocol.json, Verify-*.ps1; tools/ (Python uv), evaluation/ harness
├── docs/                 # product contract, grammar, per-carrier guides; next-stage/ = Goal 14 evidence-set workflow; goals/ frozen contracts
├── benchmarks/           # BenchmarkDotNet suites; JSON receipts bound to eng/performance manifests
├── examples/             # executable examples + DocumentationSamples (byte-exact docs snippet contract)
├── .github/workflows/    # release.yml (the only release gate) + tooling.yml (informational 3-OS matrix)
└── FunnySharp.slnx       # 11 projects; excludes tests/FunnySharp.Compatibility
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Carrier APIs (Option/Result/UnitResult/Validation/Effect) | `src/FunnySharp/` | one public type-family per file |
| Analyzers + code fixes (FS1001-FS1005) | `src/FunnySharp.Analyzers/`, `src/FunnySharp.Analyzers.CodeFixes/` | ship inside core nupkg `analyzers/dotnet/cs` |
| ASP.NET Core mapping | `src/FunnySharp.AspNetCore/` | separate nupkg, ~21 `HttpResultExtensions` overloads |
| Release gate | `eng/Run-Release.ps1`, `eng/release-protocol.json` | `benchmarkSkipped` mode is what CI runs |
| Performance verification | `eng/Verify-Performance.ps1`, `eng/performance/` | allocation budgets block; timing directional |
| Goal evaluation | `eng/evaluation/runner.py` | 5 task areas, style-neutral Contract.cs seam |
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
- Python tooling: PEP 723, stdlib only, run from repo root via `uv run --no-project`.

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
# build + test (solution)
dotnet test FunnySharp.slnx
# pack all nupkgs
dotnet pack FunnySharp.slnx -c Release
# format gate
dotnet format FunnySharp.slnx --verify-no-changes --no-restore
# release protocol (CI runs this with -SkipBenchmarks)
pwsh -NoProfile -File eng/Run-Release.ps1
# tooling verification (informational CI)
uv run --no-project eng/tools/verify_local.py
uv run --no-project python -m unittest discover -s eng/tools/tests
# docs snippets byte-compare
pwsh -NoProfile -File examples/FunnySharp.DocumentationSamples/VerifyDocumentationSnippets.ps1
# benchmark + performance verify
dotnet run --project benchmarks/FunnySharp.Benchmarks -c Release -- --filter '*' --artifacts <dir>
pwsh -NoProfile -File eng/Verify-Performance.ps1 -ReceiptDirectory <dir>
# evaluation harness
python3 eng/evaluation/runner.py prep-feed
python3 eng/evaluation/runner.py verify <area> <style> <run-dir>
```

## NOTES

- Child knowledge bases: `src/FunnySharp/AGENTS.md`, `src/FunnySharp.Analyzers/AGENTS.md`, `src/FunnySharp.AspNetCore/AGENTS.md`, `tests/FunnySharp.Tests/AGENTS.md`, `tests/FunnySharp.Analyzers.Tests/AGENTS.md`, `tests/FunnySharp.AspNetCore.Tests/AGENTS.md`, `tests/FunnySharp.Compatibility/AGENTS.md`, `eng/AGENTS.md`, `eng/tools/AGENTS.md`, `eng/evaluation/AGENTS.md`, `benchmarks/AGENTS.md`, `examples/AGENTS.md`, `examples/FunnySharp.Examples/AGENTS.md`, `examples/FunnySharp.DocumentationSamples/AGENTS.md`, `docs/AGENTS.md`, `docs/goals/AGENTS.md`, `docs/plans/AGENTS.md`, `docs/next-stage/AGENTS.md` (+ analysis/, inventory/, inventory/generated/, call-sites-code/), `.github/workflows/AGENTS.md`.
- `docs/next-stage/decision-record.md` is a pinned Goal 14 baseline - later goals append, never modify.
- Timing is informational until a fixed-hardware runner exists; allocation budgets are the blocking gate.
- No .NET 11 preview support; `net11.0` targeting waits for GA (TODO.md).
