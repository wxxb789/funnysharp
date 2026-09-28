# FunnySharp Core Library

**Why this file:** score 31 — the repo's shipping surface; every other project exists to build, test, pack, or consume this one.

## OVERVIEW
Functional carriers (`Option`/`Result`/`UnitResult`/`Validation`/`Effect`) plus traversal/pipeline extensions over BCL types. C# 13, net10.0, zero runtime dependencies.

## WHERE TO LOOK
| Task | Location |
|------|----------|
| Carrier semantics | `Option.cs`, `Result.cs`, `UnitResult.cs`, `Validation.cs` |
| Traversal matrix | `SequenceExtensions.cs` / `AsyncSequenceExtensions.cs` / `ParallelAsyncEnumerableExtensions.cs` |
| Parallel plumbing (Channels, IValueTaskSource) | `ParallelAsyncEnumerableExtensions.cs` (680 lines, highest complexity) |
| Optics | `Optics.cs` (`Lens`, `Optional`) |
| State machines | `StateMachine.cs`, `StateTransition.cs` |
| Span pipelines | `SpanPipelineExtensions.cs` |
| Naming grammar contract | `ContainerExtensions.cs` (OrNone docs) |

## CONVENTIONS
- File-scoped `namespace FunnySharp;`; one type-family per file; file name matches primary type.
- Sync/async/parallel files are deliberately mirrored triplets — not duplication; change them in lockstep.
- `...Async` suffix on async members; `OrNone` suffix on absence-translating bridges. Naming grammar is contract.
- Null arguments throw eagerly BEFORE the deferred value is constructed; parse failures never throw.
- XML docs are spec: behavioral contracts (single enumeration, cancellation) live in `<remarks>`.
- Packaging config lives in the csproj: README packed from repo root, analyzers packed under `analyzers/dotnet/cs`, `IsTrimmable`, `EnablePackageValidation`, locked restore (`packages.lock.json`).

## ANTI-PATTERNS
- Never fold a selected null into `None` — bridges throw `ArgumentNullException` instead.
- Span pipelines: source and destination must not overlap; capacity validated before any write.
- No runtime dependencies — BCL only; no new PackageReference without a goal contract.
- No second absence carrier (e.g. `Maybe<T>`) and no `Where` member on carrier types — frozen by `AdvancedPatternCurationTests`.
