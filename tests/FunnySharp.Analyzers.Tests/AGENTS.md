# FunnySharp.Analyzers.Tests

**Why this file:** score 22 — analyzer suite with a hand-rolled in-memory Roslyn harness; distinct domain from behavioral tests.

## OVERVIEW
xUnit v3 tests for the 5 shipped analyzers + 2 code fixes, compiling real built FunnySharp assemblies — not `Microsoft.CodeAnalysis.Testing`.

## WHERE TO LOOK
| Task | Location |
|------|----------|
| Compilation harness | `AnalyzerHarness.cs` (`AllAnalyzers`, `CreateCompilation`, `GetDiagnosticsAsync`, `ApplyFirstFixAsync`) |
| Assert helpers | `AnalyzerTestAssert.cs` (`DiagnosticAsync` = exactly one, `QuietAsync` = zero) |
| Per-rule suites | `UninitializedCarrierAnalyzerTests.cs`, `DiscardedOutcomeAnalyzerTests.cs` (393 lines, largest), `IgnoredTryGetResultAnalyzerTests.cs`, `BlockedValueTaskAnalyzerTests.cs`, `SyncDisposeAnalyzerTests.cs` |
| Code fixes | `CodeFixTests.cs` |
| Packaging invariants | `AnalyzerPackagingTests.cs` |
| Dogfood invariant | `ExampleProjectsAnalyzeCleanTests.cs` |

## CONVENTIONS
- Harness builds in-memory `CSharpCompilation`s referencing the REAL built assemblies; asserts sources compile error-free first, filters suppressed diagnostics, prefers Latest language version.
- Example projects must analyze clean: `ExampleProjectsAnalyzeCleanTests` compiles every repo example project against the real analyzers and expects zero unsuppressed diagnostics.
- Shares `../Shared/TestRepositoryRoot.cs` via Compile-include link; `Xunit` global using; `OutputType=Exe` on the Microsoft.Testing.Platform runner (set globally via `global.json`).
- Reflection member-scan misses some members, so curation tests scan them directly — check both paths when changing public surface.
- Run individual rule suites with `--filter FullyQualifiedName~DiscardedOutcomeAnalyzerTests`.
- Run whole suite: `dotnet test tests/FunnySharp.Analyzers.Tests/FunnySharp.Analyzers.Tests.csproj` (or `dotnet test FunnySharp.slnx`).
- Code-fix tests call `ApplyFirstFixAsync` — one fix application, not fix-all.
- New diagnostics must land in `AnalyzerReleases.Unshipped.md` alongside the analyzer change or `AnalyzerPackagingTests` fails.

## ANTI-PATTERNS
- Do not add `Microsoft.CodeAnalysis.Testing` — the hand-rolled harness is the contract.
- Deliberate-misuse snippets in `examples/` are wrapped in `#pragma warning disable FS100x` — removing those pragmas breaks the samples-clean invariant.
