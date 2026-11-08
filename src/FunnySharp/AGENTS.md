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
| One-pass summaries | `Summary.cs`, `SummaryPlan.cs`, `ExtremumPolicy.cs` (reusable count/sum/extrema/domain summaries; `Zip`/`MapResult`; one traversal; checked built-ins; local per-component state; total folds) |
| Cardinality-checked keyed joins | `LookupIndex.cs`, `UniqueLookupIndex.cs`, `LookupExtensions.cs`, `LookupError.cs`, `LookupErrorKind.cs` (explicit comparer; source-ordered occurrences; complete-input vs matched-key uniqueness; structured positions; buffered and streaming shapes) |
| Nested data reshaping | `NestedDataExtensions.cs`, `RowOrigin.cs`, `CartesianRowOrigin.cs`, `NestedRowError.cs`, `NestedRowErrorKind.cs` (single/aligned/explicit-Cartesian expansion; all-or-nothing buffered `Validation`; by-value provenance; sync/async parent sources) |
| Domain programs and simulation | `DomainProgram.cs`, `ProgramSimulation.cs`, `EmittedCommand.cs`, `SimulationDelivery.cs`, `CommandExecutionFailure.cs` (pure business-program description over `StateMachine`; parent-aware `Lift`; explicit-time single-threaded simulation with ordered commands, correlation, and execution failures; no runtime) |
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
- Summaries are total folds over BCL sequences: every component observes every item, no early-exit/partial-result protocol exists, and `EvaluateAsync` changes only input acquisition. No public typeclass/summary-interface hierarchy, automatic parallelism, LINQ replacement, or compile-time fusion; seed values are never cloned, and exceptions/cancellation stay exceptions/cancellation.
- Strict joins never expand duplicate lookup keys into multiple output rows, never infer a dictionary's comparer, and never claim complete-input uniqueness from the streaming shape. Complete-input validation belongs to `RequireUniqueKeys`, `ToUniqueLookupIndex`, and the buffered `JoinExactlyOne`; `GetMany` is the explicit one-to-many expansion primitive. Null keys are structural failures, and the index retains caller values by reference under the documented lifetime contract.
- Nested reshaping never treats a null child list as empty, truncates unequal aligned lengths, infers a Cartesian product from data, or returns partial rows on failure. `ExpandAligned` pairs by index; `ExpandCartesian` is the explicit product. Child selectors and combine callbacks are synchronous; async forms change only parent acquisition and forward the token to the enumerator. Rejection stays caller policy, located by the by-value origin.
- Domain programs never execute commands, read ambient time/randomness, persist state, schedule work, or become a runtime. `Update` invokes the pure step once; `Lift` uses the caller's setter as the only parent write and maps one child command to one parent command; `Replay` keeps the existing stop-at-first-non-applied meaning; the simulation is single-threaded and its transcript is not a production event store.
- Domain values never become a second outcome channel: no union/`Either` system, no implicit conversions, no public constructor, no serializer, and no storage/authorization/exactly-once claim. `Domain.Contract` must derive through `Contract.Widen`, not a parallel rule engine.
