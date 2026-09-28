# FunnySharp.Analyzers

**Why this file:** score 22 — Roslyn analyzer package shipped inside the core nupkg; distinct diagnostic domain (FS100x).

## OVERVIEW
Five operation-based Roslyn analyzers (netstandard2.0, Roslyn 4.14+) guarding FunnySharp carrier misuse.

## WHERE TO LOOK
| Task | Location |
|------|----------|
| Diagnostic IDs / severities | `DiagnosticIds.cs`, `Descriptors.cs` |
| Shared type resolution | `FunnySharpWellKnownTypes.cs` (`TryCreate` per compilation) |
| FS1001 uninitialized carrier | `UninitializedCarrierAnalyzer.cs` (Error) |
| FS1002 discarded outcome | `DiscardedOutcomeAnalyzer.cs` (Warning) |
| FS1003 ignored TryGet result | `IgnoredTryGetResultAnalyzer.cs` (Warning) |
| FS1004 blocked ValueTask | `BlockedValueTaskAnalyzer.cs` (Warning) |
| FS1005 sync dispose of IAsyncDisposable | `SyncDisposeAnalyzer.cs` (Warning) |

## CONVENTIONS
- Operation-based analysis: `RegisterCompilationStartAction` -> `RegisterOperationAction`; never syntax-only.
- Well-known types resolved once per compilation via `FunnySharpWellKnownTypes.TryCreate`.
- Release tracking mandatory (RS2008): rule changes go in `AnalyzerReleases.Unshipped.md`; `Shipped.md` stays empty until first shipping release.
- Severity rule: Error only for guaranteed failure (FS1001); silent hazards are Warning.
- Help links point at `docs/analyzers.md` anchors.
- `IsPackable=false`, `EnforceExtendedAnalyzerRules=true`, `ImplicitUsings` disabled, `LangVersion` 12 — ships inside the FunnySharp package under `analyzers/dotnet/cs`, not as its own NuGet.
- Sibling `src/FunnySharp.Analyzers.CodeFixes` (score 5, no own file) shares this namespace and `DiagnosticIds`; it provides `DiscardedOutcomeCodeFixProvider` (FS1002) and `SyncDisposeCodeFixProvider` (FS1005), and ships beside this assembly in the package.

## ANTI-PATTERNS
- No `NotConfigurable` diagnostics — everything suppressible.
- Don't add syntax-based analyzers when an operation action covers it.
