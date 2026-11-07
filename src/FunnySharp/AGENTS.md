# FunnySharp Core Library

**Why this file:** score 31 — the repo's shipping surface; every other project exists to build, test, pack, or consume this one.

## OVERVIEW
Functional carriers (`Option`/`Result`/`UnitResult`/`Validation`/`Effect`), input decoding over BCL `JsonElement`, plus traversal/pipeline extensions over BCL types. C# 13, net10.0, zero runtime dependencies.

## WHERE TO LOOK
| Task | Location |
|------|----------|
| Carrier semantics | `Option.cs`, `Result.cs`, `UnitResult.cs`, `Validation.cs` |
| Reusable checks | `CheckExtensions.cs` (value-preserving `Check`, ordered independent `CheckAll`; Task/ValueTask forms) |
| Fallible reduction | `FallibleReductionExtensions.cs`, `AsyncFallibleReductionExtensions.cs` (seeded `Result` fold; first failure stops source enumeration; sync/async source forms) |
| Input decoding | `Decoder.cs`, `DecodeError.cs`, `DecodeErrorKind.cs` (JsonElement -> Validation; stable `Path`, experimental `Location`) |
| Inspectable contracts | `Contract.cs` plus `Contract*.cs` (one description -> validation, decoder derivation, explanation, schema, generation, shrinking; explicit limitations and caller extensions) |
| Typed partial updates | `PatchChange.cs`, `PatchField.cs`, `Patch.cs`, `PatchSchema.cs` (three-state changes, allowed-field decode, candidate validation, conflict detection, immutable application) |
| Batched reads | `BatchSource.cs`, `BatchRequirement.cs`, `BatchScope.cs` (explicit source batch delegates, deferred typed requirements, bounded dispatch, scope-local cache/invalidation) |
| Tagged domain values | `DomainValue.cs`, `Domain.cs` (phantom marker, validated boundary, derived contract/decoder, uninitialized default detection; ordinary state/error records remain consumer code) |
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
- Decoding accepts a parsed BCL `JsonElement` only; no serializer, schema introspection, reflection binder, or async input model. Location prefixes are materialized only after a decode failure. Patch decoding adds explicit unknown/repeated-field errors and validates the complete candidate, not only supplied fields.
- Batched reads never infer a batch-capable source: the caller supplies the source delegate, equality policy, and partition. Missing, per-key, whole-batch, and unexpected-response outcomes stay distinct from exceptions and cancellation; scope caches are bounded and explicit.
- Contracts never invent a predicate inverse, generator, purity proof, or complete schema. Missing derivations are explicit `ContractLimitation` values; generation never silently rejection-samples a cross-field rule, and every generated or shrunk value is checked against the contract.
- No second absence carrier (e.g. `Maybe<T>`) and no `Where` member on carrier types — frozen by `AdvancedPatternCurationTests`.
- Never add `Bind`/`SelectMany` to `Validation` to share check syntax; `CheckAll` only applies independent rules to one input. Dependent work stays on fail-fast `Check`, `Bind`, or ordinary control flow.
- Fallible reduction never materializes intermediate results, continues source enumeration after a typed failure, clones or rolls back caller accumulators, or adds automatic parallel reduction. Exceptions and cancellation are never converted to `Result` failures.
- Domain values never become a second outcome channel: no union/`Either` system, no implicit conversions, no public constructor, no serializer, and no storage/authorization/exactly-once claim. `Domain.Contract` must derive through `Contract.Widen`, not a parallel rule engine.
