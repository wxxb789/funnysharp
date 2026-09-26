# Stability Inventory

FunnySharp ships a stable 0.1.0 surface plus a tracked set of experimental members. Every
experimental member carries `[Experimental("FS####")]` with the diagnostic ID recorded here, an
entry in this inventory, no compatibility promise, and removal without a breaking-change
process ([stability boundary](next-stage/api-decisions.md)). Stable members change only
through a later accepted goal.

Consumers opt into an experimental member by suppressing its diagnostic ID
(`#pragma warning disable FS####` or `NoWarn`); the diagnostic makes the experimental status
visible at every use site.

## Stable surface

Every stable type is public, XML-documented, covered by semantic xUnit v3 tests, and
benchmark-characterized where performance-relevant (product contract: [stability
boundary](product-contract.md#stability-boundary)). The member-level authority for the
Goal 14 pinned surface is the [decision matrix](next-stage/decision-matrix.csv); later-goal
additions are recorded below by family. The frozen advanced-pattern portion of this
inventory (AD-6 through AD-9) is enforced by
`tests/FunnySharp.Tests/AdvancedPatternCurationTests.cs`, and the experimental-tier sync by
its stability-inventory test.

| Family | Types | Stability decision | Delivered by |
| --- | --- | --- | --- |
| Absence | `Option`, `Option<T>`, `OptionExtensions`, `TryOperation<T>` | AD-1 keep | 0.1.0 |
| Fail-fast value outcome | `Result`, `Result<TValue,TError>`, `ResultExtensions` | AD-2 keep; default/uninitialized-state redesign delivered | 0.1.0; Goal 15 |
| Fail-fast no-value outcome | `UnitResult`, `UnitResult<TError>`, `UnitResultExtensions` | G1 adopt | Goal 15 (fourth carrier), Goals 16–17 (grammar, traversal) |
| Accumulating validation | `Validation`, `Validation<TValue,TError>`, `ValidationExtensions` | AD-3 keep; default/uninitialized-state redesign delivered | 0.1.0; Goal 15 |
| Function grammar | `FunctionExtensions` | AD-4 keep; `ComposeValueAsync` rename delivered | 0.1.0; Goal 16 |
| Pipelines | `EnumerablePipelineExtensions`, `AsyncEnumerablePipelineExtensions`, `SpanPipelineExtensions` | AD-5 keep | 0.1.0 |
| Sequences and traversal | `SequenceExtensions`, `AsyncSequenceExtensions` (located overloads are experimental, below) | AD-5 keep; G4 narrow `Scan` | 0.1.0; Goal 17 |
| Cardinality and container safety | `CardinalityExtensions`, `AsyncCardinalityExtensions`, `ContainerExtensions`, `ParseExtensions`, `PartitionExtensions`, `Partition<T>`, `OptionPartition<T>`, `ResultPartition<TValue,TError>`, `UnitResultPartition<TError>` | G5 adopt (`*OrNone`, `Partition`, bridges) | Goal 17 |
| Non-empty guarantee and exact zip | `NonEmpty<T>`, `ExactZipExtensions` | E72 defer resolved by adoption: Goal 17 proved the safety value without a collection universe | Goal 17 |
| Async concurrency | `ParallelAsyncEnumerableExtensions`, `ParallelAsyncSequenceExtensions`, `ConcurrentEffectExtensions` | AD-6 keep; G7 completion-order adopt | 0.1.0; Goal 18 |
| Effects and resources | `Effect`, `Effect<T>`, `Effect<TEnvironment,T>`, `EffectResourceExtensions` | AD-7 keep | 0.1.0 |
| State machines | `TransitionStatus`, `StateChange<TState,TOutput>`, `StateTransition<TState,TOutput>`, `StateTransitionExtensions`, `TransitionResult<TState,TOutput,TError>`, `StateMachine<TState,TEvent,TOutput,TError>`, `StateMachineExtensions` | AD-8 keep; the `Then` single-materialization redesign delivered by Goal 20 | 0.1.0; Goal 20 |
| Optics | `Lens`, `Lens<TSource,TFocus>`, `Optional`, `Optional<TSource,TFocus>` | AD-9 keep | 0.1.0 |
| HTTP integration | `HttpResultExtensions` (`FunnySharp.AspNetCore`) | AD-10 keep; G1 `UnitResult` mapping delivered | 0.1.0; Goal 15 |

Deferred capabilities stay absent from the stable surface: a general retry/backoff layer
(G8), `Memoize`/async-buffer carriers and k-way async merge, completion-order coordination
beyond the recorded constrained form, typed-results/OpenAPI APIs (Goal 22 documents
`.Produces*` instead), System.Text.Json converters, `Either` as a non-error carrier, Option
comparers/ordering, awaitable `Option<Task<T>>` sugar, and `net11.0` targeting. A public
`Unit` type is rejected, not deferred (G17). The absence of these names from the public
surface is enforced by `AdvancedPatternCurationTests`.

## Experimental surface

| Diagnostic ID | Member | Status | Entered | Owner goal | Notes |
| --- | --- | --- | --- | --- | --- |
| FS0017 | `Location` (src/FunnySharp/Location.cs) | Experimental | 2026-09-20 | Goal 17 | Compositional traversal location context (decision G6: adopt experimental first; E44). |
| FS0017 | `SequenceExtensions`.`Traverse` located list overloads (Option/Result/UnitResult/Validation) | Experimental | 2026-09-20 | Goal 17 | Selector receives `root.At(index)`. |
| FS0017 | `SequenceExtensions`.`Traverse` located dictionary overloads | Experimental | 2026-09-20 | Goal 17 | Selector receives `root.Key(key)` plus key and value. |
| FS0017 | `AsyncSequenceExtensions`.`TraverseAsync`/`TraverseValueAsync` located overloads | Experimental | 2026-09-20 | Goal 17 | Async located traversal; token forwarded to the enumerator. |
