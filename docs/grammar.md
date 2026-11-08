# Functional API Grammar

FunnySharp has one small, orthogonal, predictable grammar. Every verb has one primary meaning and a
predictable output shape on every carrier where that meaning is valid, so a reader infers evaluation
order, short-circuiting, exception, cancellation, enumeration, and materialization behavior from the
signature and its contract.

This file is the authoritative verb reference. The per-feature guides
([option.md](option.md), [result.md](result.md), [unit-result.md](unit-result.md),
[validation.md](validation.md), [checks.md](checks.md), [decoding.md](decoding.md), [function-composition.md](function-composition.md),
[data-pipelines.md](data-pipelines.md), [fallible-reduction.md](fallible-reduction.md), [one-pass-summaries.md](one-pass-summaries.md), [cardinality-joins.md](cardinality-joins.md), [effects.md](effects.md),
[concurrency.md](concurrency.md), [immutable-updates.md](immutable-updates.md),
[state-machines.md](state-machines.md), and [aspnet-core.md](aspnet-core.md)) carry the detailed
contracts and performance evidence; the rows below stay one line each.

## Carriers

| Carrier | Meaning |
| --- | --- |
| `Option<T>` | Absence of a value; never an error. |
| `Result<TValue, TError>` | Fail-fast value-producing work with a typed error. |
| `UnitResult<TError>` | Fail-fast work with no meaningful value. |
| `Validation<TValue, TError>` | Independent checks whose errors accumulate deterministically. |
| `Effect<T>`, `Effect<TEnvironment, T>` | The deferred-work boundary, not an outcome carrier. |

The BCL supplies every other carrier. Sequences stay on `IEnumerable<T>`, `IAsyncEnumerable<T>`, and
span/memory views; `Task` and `ValueTask` are awaitables, not carriers; ordinary delegates
(`Func`, `Action`, `StateTransition`) are the composition surface. `NonEmpty<T>` is a cardinality
guarantee over BCL lists — not an outcome carrier and not a collection — and `Location` is the
experimental traversal-context carrier (`FS0017`, [stability inventory](stability-inventory.md)).
No second carrier for any of the
meanings above is added: `Maybe`, `Either`, `Fin`, `Try`, `IO`, `Reader`, `OptionAsync`, and async
outcome wrappers remain out of scope ([product contract](product-contract.md)). `Decoder<T>` is not a
semantic outcome carrier: it is a callable input-to-`Validation` boundary, and `DecodeError` is its
diagnostic value. `Contract<T>` is also not an outcome carrier: it is an inspectable constraint
description. `Validate` checks an existing value, `CreateDecoder` derives a decoder, `Generate` and
`Shrink` derive test inputs and candidates, and `Description`/`Explain` expose metadata.
`BatchSource<TKey, TValue, TError>`, `BatchRequirement<TValue, TError>`, and `BatchScope` are an
explicit batched-read description and its execution scope, not outcome carriers; their reads yield
the canonical `Result` shape. `DomainValue<TValue, TTag>` is a validated tagged value and
`Domain<TValue, TTag>` is its validation, decoding, contract, and explanation boundary; neither is
an outcome carrier. `Summary<TSource, TState, TResult>` is a reusable sequence-summary description,
not an outcome carrier; evaluation returns the caller-selected result and adds no failure or
absence channel. `LookupIndex<TKey, TValue>` and `UniqueLookupIndex<TKey, TValue>` are materialized
keyed-association descriptions, not outcome carriers; their reads return `Option`/`Result` values,
their buffered joins return `Validation`, and their streaming join yields `Result` outcomes.
`LookupError<TKey>` is their diagnostic value. Explicit state
variants and typed module-error hierarchies are ordinary C#
record types over the canonical carriers, not new library carriers.

## Naming Rules

- One verb has one meaning, and the verb never changes for a concept across carriers or sync/async
  sources. `Where` is never added to a carrier: a bare predicate cannot produce absence or an error.
- `...Async` marks an awaitable operation — one that takes no callback or a `Task`-returning
  callback, including an async source consumed to completion (`SequenceAsync`, `TraverseAsync`),
  where the selector type is visible in the signature.
- `...ValueAsync` marks a callback returning `ValueTask`.
- Streaming element-wise operators on `IAsyncEnumerable<T>` use the bare verb with synchronous
  callbacks (`Choose`, `Scan`) and `...ValueAsync` with `ValueTask` callbacks (`ChooseValueAsync`,
  `ScanValueAsync`). Both forms return `IAsyncEnumerable<T>`, never an awaitable.
- `Using`/`UsingAsync` marks the `IDisposable` versus `IAsyncDisposable` resource boundary; the
  constraint is visible in the signature.
- Parallel delivery order is part of the name, never a parameter:
  `SelectParallelValueAsync` yields source order and
  `SelectParallelCompletionOrderValueAsync` yields completion order; no boolean or enum ordering
  parameter hides the contract from the call site.
- A `CancellationToken` is always an explicit parameter and is forwarded unchanged.
- Eager absence fallbacks are plain parameters (`OrElse`, `GetValueOr`); lazy absence fallbacks
  are factories (`OrElseWith`, `GetValueOrElse`). Recovery callbacks are lazy in both forms:
  `Recover` produces a successful value, while `RecoverWith` produces another value of the same
  carrier. `GetValueOrDefault` is the only absence getter with an implicit `default(T)` fallback.
- Sequence cardinality and container/parse absence use the `*OrNone` suffix (`FirstOrNone`, `LastOrNone`,
  `SingleOrNone`, `ElementAtOrNone`, `MinOrNone`/`MaxOrNone`, `ToNonEmptyOrNone`, `DequeueOrNone`,
  `PopOrNone`, `PeekOrNone`, `IndexOfOrNone`, `RemoveOrNone`, the generic `ParseOrNone`, and the named
  `Parse*OrNone` bridges); keyed lookups use `GetOption`; value conversion uses `ToOption`.
- Cardinality-constrained keyed lookups put the cardinality in the name (`GetMany`,
  `GetZeroOrOne`, `GetExactlyOne`), and an exactly-one join is named `JoinExactlyOne`. The
  streaming per-row form is `JoinExactlyOneResults`. No boolean or enum cardinality parameter
  hides the contract from the call site.
- LINQ `Select`/`SelectMany` exist only where `Bind` exists (never `Where`) and stay secondary to
  the member-centric vocabulary. `Bind` does not imply a LINQ bridge: `UnitResult` has none.
- No aliases, no competitor naming, no naming concessions.

## The Verb Table

The table covers workflow operations. Constructors, state/payload properties, equality, hashing,
diagnostic formatting, and compiler-generated record/delegate members follow their documented
.NET/C# contracts; they are not alternative pipeline vocabulary. Experimental location operations
remain outside the stable table and are identified in the [stability inventory](stability-inventory.md).
Decoder construction (`Field`, `OptionalField`, `NullableField`, `OptionalValue`, `List`,
`NonEmptyList`, `Object`, `From`, and the scalar factories) is documented in
[decoding.md](decoding.md); those names are constructors, not workflow verbs, and their
missing/null/empty policies are part of the contract. Contract construction (`For`, `Field`,
`OptionalField`, `NullableField`, `Optional`, `List`, `NonEmptyList`, `Object`, `OneOf`,
`DiscriminatedOneOf`, `Defer`, `From`, and the scalar/range factories) and derivation
(`CreateDecoder`, `Generate`, `GenerateSamples`, `Shrink`, `Description`, `Explain`, `Ensure`,
`ValidateWith`, and the `With*` extension methods) are documented in [contracts.md](contracts.md).
Patch construction (`For`, `Field`, `ClearableField`, `PatchSchema.Create`, and the
`PatchChange` factories) is documented in [partial-updates.md](partial-updates.md); its workflow
verbs are listed below.

### Construction And Exception Boundaries

| Verb | Meaning | Present on | Output shape | Async forms | Key contract |
| --- | --- | --- | --- | --- | --- |
| `Some`/`None` | Explicitly constructs presence or absence. | `Option` | `Option<T>` | — | `Some` rejects runtime null; `None` is the valid default option. |
| `Success`/`Failure` | Explicitly constructs a fail-fast outcome. | `Result`, `UnitResult` | Same carrier | — | `UnitResult.Success` has no payload; typed nullable payloads retain their declared meaning. |
| `Valid`/`Invalid`/`InvalidMany` | Explicitly constructs a valid value or ordered validation errors. | `Validation` | `Validation<TValue, TError>` | — | `Invalid` owns one error; `InvalidMany` enumerates once into a nonempty snapshot and rejects null or empty input. |
| `FromNullable` | Converts nullable input to presence or absence. | nullable values | `Option<T>` | — | Runtime null is `None`; a present default value is not absence. |
| `FromTry` | Invokes a Boolean-returning Try-pattern operation. | `TryOperation<T>` | `Option<T>` | — | Invoked once; false or a runtime-null successful output is `None`; exceptions propagate unchanged. |
| `FromBoolean` | Constructs presence when a condition is true. | Boolean plus value or value factory | `Option<T>` | — | The value overload is eager; the factory is validated at entry but invoked only when true; a runtime-null selected value is `None`. |
| `Try` | Executes an explicit non-cancellation exception boundary. | value-producing or valueless work | `Result<TValue, TError>` / `UnitResult<TError>` | `TryAsync`, `TryValueAsync` | Ordinary operation exceptions are preserved or mapped by the supplied mapper; cancellation and mapper exceptions propagate. |
| `TryAsync` | Executes the boundary over a Task-returning operation. | tokenless `Func<Task<T>>` / `Func<Task>` | `Task<Result<...>>` / `Task<UnitResult<...>>` | — | The callback owns any captured token; canceled tasks and faulted OCE tasks retain their states, and a null returned Task faults rather than reaching the mapper. |
| `TryValueAsync` | Executes the boundary over a ValueTask-returning operation. | tokenless `Func<ValueTask<T>>` / `Func<ValueTask>` | `ValueTask<Result<...>>` / `ValueTask<UnitResult<...>>` | — | Each ValueTask is consumed once; cancellation and faulted OCE are not domain failures. |
| `Decode` | Interprets a parsed BCL `JsonElement` as a typed value or allowed change and accumulates structural and domain errors. | `Decoder<T>`, `PatchSchema<T>` | `Validation<T, DecodeError>`, `Validation<Patch<T>, DecodeError>` | `DecodeAsync`, `DecodeValueAsync` (decoder asynchronous validation only) | Single call; input is not retained; missing/null/wrong-kind/malformed/empty are distinct; a patch additionally distinguishes unchanged, explicit clear, replacement, unsupported field, and duplicate field; callback, parse, cancellation, and programming exceptions propagate unchanged. |
| `CreateDecoder` | Derives a decoder from a contract's declared structure or a caller-supplied decoder. | `Contract<T>` | `Result<Decoder<T>, ContractLimitation>` | — | An unavailable structure is a typed limitation; a decoded value is still checked against the contract. |
| `Generate` | Generates one deterministic value within the caller's explicit budget. | `Contract<T>` | `Result<T, ContractLimitation>` | — | Every value is checked; missing derivations, exhausted budgets, unsatisfiable declared constraints, and caller-generator violations fail explicitly; never rejection sampling. |
| `GenerateSamples` | Generates several values from one deterministic sequence. | `Contract<T>` | `Result<IReadOnlyList<T>, ContractLimitation>` | — | The node budget is shared across the call; every value is checked. |
| `Shrink` | Proposes one-step smaller candidates that still satisfy the contract. | `Contract<T>` | `Result<IReadOnlyList<T>, ContractLimitation>` | — | Candidates are checked against the whole contract, including refinements; duplicates and the input are removed; at most 256 candidates. |

### Function Grammar

| Verb | Meaning | Present on | Output shape | Async forms | Key contract |
| --- | --- | --- | --- | --- | --- |
| `Pipe` | Applies a function to a value. | any value | `TResult` | none (an async delegate returns its awaitable) | Delegate validated at entry; exceptions propagate unchanged. |
| `Compose` | Composes two unary functions left to right. | `Func` delegates | `Func<T, TResult>` | `ComposeAsync`, `ComposeValueAsync` | Both delegates validated when the composed delegate is created; the second stage is skipped after the first throws. |
| `ComposeAsync` | Composes two `Task`-returning functions left to right. | `Func<T, Task<TIntermediate>>` | `Func<T, Task<TResult>>` (± token) | — | Delegate exceptions are represented by the returned task; the exact token is forwarded to both stages. |
| `ComposeValueAsync` | Composes two `ValueTask`-returning functions left to right. | `Func<T, ValueTask<TIntermediate>>` | `Func<T, ValueTask<TResult>>` (± token) | — | Each `ValueTask` is awaited exactly once; there are no mixed `Task`/`ValueTask` compose overloads. |
| `Curry` | Converts a binary function into nested unary functions. | `Func<TFirst, TSecond, TResult>` | `Func<TFirst, Func<TSecond, TResult>>` | — | Binary arity only. |
| `Uncurry` | Converts nested unary functions back into a binary function. | `Func<TFirst, Func<TSecond, TResult>>` | `Func<TFirst, TSecond, TResult>` | — | Binary arity only. |
| `Partial` | Binds the first argument of a binary function. | `Func<TFirst, TSecond, TResult>` | `Func<TSecond, TResult>` | — | Binary arity only. |
| `Flip` | Returns a binary function with its arguments reversed. | `Func<TFirst, TSecond, TResult>` | `Func<TSecond, TFirst, TResult>` | — | Binary arity only. |
| `Tap` | Observes a value with an `Action` and returns it. | any value | `T` | `TapAsync`, `TapValueAsync` | Observer validated at entry; the original value is returned; observer exceptions propagate. |
| `TapAsync` | Observes a value through a `Task`-returning observer. | any value | `Task<T>` (± token) | — | Observer faults surface through the returned task; the value is returned after observation. |
| `TapValueAsync` | Observes a value through a `ValueTask`-returning observer. | any value | `ValueTask<T>` (± token) | — | The `ValueTask` is awaited exactly once. |

### Carrier Transforms

| Verb | Meaning | Present on | Output shape | Async forms | Key contract |
| --- | --- | --- | --- | --- | --- |
| `Map` | Transforms the contained value, keeping the carrier. | `Option`, `Result`, `Validation`, `Effect`, `Decoder<T>` | Same carrier; `Decoder<T>` for decoder composition | `MapAsync`, `MapValueAsync` | Delegate validated at entry even when short-circuited; invoked at most once for the active case; `Option` normalizes a runtime-null result to `None`; a `Decoder` selector runs only after a successful decode and its exceptions propagate. |
| `MapAsync` | `Map` with a `Task`-returning selector. | `Option`, `Result`, `Validation` | `Task<carrier>` (± token) | — | An inactive case returns an already-completed carrier without invoking the selector or inspecting the token. |
| `MapValueAsync` | `Map` with a `ValueTask`-returning selector. | `Option`, `Result`, `Validation` | `ValueTask<carrier>` (± token) | — | Same short-circuit contract; each `ValueTask` is awaited once. |
| `Bind` | Sequences a carrier-returning function and stops at the first absence or failure. | `Option`, `Result`, `UnitResult`, `Effect`, `Decoder<T>` | Same carrier; `Decoder<T>` for decoder composition | `BindAsync`, `BindValueAsync` | Callback skipped after failure and never invoked for the inactive case; callback exceptions propagate unchanged; a `Decoder` binder returns the next decoder, which decodes the same input, and a null decoder is a programming error. |
| `BindAsync` | `Bind` with a `Task`-returning binder. | `Option`, `Result`, `UnitResult` | `Task<carrier>` (± token) | — | Same short-circuit and exact-token-forwarding contract. |
| `BindValueAsync` | `Bind` with a `ValueTask`-returning binder. | `Option`, `Result`, `UnitResult` | `ValueTask<carrier>` (± token) | — | Same short-circuit contract; each `ValueTask` is awaited once. |
| `MapError` | Transforms the failure while preserving success. | `Result`, `UnitResult` | Same carrier | — | Selector skipped after success; invoked at most once. |
| `MapErrors` | Transforms every accumulated error while preserving a valid value. | `Validation` | Same carrier | — | Selector invoked once per error in order; skipped for a valid value. |
| `Filter` | Keeps a present value only when a predicate accepts it. | `Option` | `Option<T>` | — | Predicate skipped for `None`; a `Some` whose predicate fails becomes `None`. |
| `Ensure` | Keeps a success only when a predicate accepts it, otherwise returns the supplied failure. | `Result`, `UnitResult`, `Decoder<T>`, `Contract<T>` | Same carrier/contract; `Decoder<T>` | — | Predicate skipped after failure; an overload takes a lazy error factory; a `Decoder` rejection is one `InvalidValue` error at the decoder's location; a `Contract` rejection adds one `InvalidValue` error and makes generation unavailable until a generator is supplied. |
| `Check` | Runs a fallible no-value check against a success value and retains that exact value. | `Result` | Same `Result<TValue, TError>` | `CheckAsync`, `CheckValueAsync` | Delegate guarded at entry; skipped after failure; the check result supplies only the failure; no value factory or reconstruction; distinct from `Ensure` (predicate) and `Bind` (value-changing). |
| `CheckAll` | Runs an ordered set of independent no-value checks against one active value and accumulates every error. | `Result`, `Validation` | `Validation<TValue, TError>` | `CheckAllAsync`, `CheckAllValueAsync` | Rules snapshotted once; every rule runs after a typed failure; inactive input skips enumeration and callbacks; empty rules retain the value; errors in rule order; dependent rules use `Check`/`Bind`, and no `Validation.Bind` is added. |
| `Validate` | Applies an accumulating domain validation to a successfully decoded value, checks an existing value against a contract, or checks a complete patch candidate against a contract. | `Decoder<T>`, `Contract<T>`, `PatchSchema<T>` | `Decoder<T>` for decoder composition; `Validation<T, DecodeError>` for a contract check; `Validation<PatchApplication<T>, DecodeError>` for a patch | — | On a decoder: callback skipped after failure; every returned message becomes an ordered `InvalidValue` error at the decoder's location; callback exceptions propagate. On a contract: a runtime-null reference value is a `Null` error; object fields and collection elements accumulate in declaration/source order; refinement callbacks contribute messages only. A patch schema validates the applied candidate, so unchanged fields and cross-field rules participate. |

### Fallback And Recovery

| Verb | Meaning | Present on | Output shape | Async forms | Key contract |
| --- | --- | --- | --- | --- | --- |
| `Recover` | Lazily replaces a failure with a successful value produced from its error. | `Result` | `Result<TValue, TError>` (success-shaped) | — | Callback validated at entry, skipped after success, and invoked at most once after failure. |
| `RecoverWith` | Lazily replaces a failure with another value of the same carrier. | `Result`, `UnitResult` | Same carrier | — | Callback validated at entry and skipped after success; `UnitResult` has no plain `Recover` because its success has no value payload. |
| `OrElse` | Returns the first value when it is present, otherwise an eager fallback. | `Option`, `StateMachine` | `Option<T>`; machine | — | Fallback skipped for `Some`; on machines, the fallback runs only when the first returns `Undefined`. |
| `OrElseWith` | Returns the first value when present, otherwise invokes a fallback factory. | `Option` | `Option<T>` | — | Factory validated at entry and invoked only for `None`. |
| `GetValueOr` | Returns the contained value or an eager non-null fallback. | `Option` | `T` (non-null) | — | Fallback validated before the option is examined. |
| `GetValueOrElse` | Returns the contained value or invokes a fallback factory. | `Option` | `T` (non-null) | — | Factory invoked only for `None`; a null factory result is rejected. |
| `GetValueOrDefault` | Returns the contained value or `default`. | `Option` | `T` (may be `default`) | — | The only absence getter with an implicit `default(T)` fallback; named to say so. |

### Combination

| Verb | Meaning | Present on | Output shape | Async forms | Key contract |
| --- | --- | --- | --- | --- | --- |
| `Zip` | Combines already-created carriers into a pair tuple or a combined value. | `Option`, `Result`, `UnitResult`, `Validation` | Pair tuple on `Option`/`Result`/`Validation`; combined value (arity 2–4, bounded) on those carriers; `UnitResult<TError>` on `UnitResult` | — | `Option`: `None` when any operand is absent, and a runtime-null combined result is `None`. `Result`/`UnitResult`: the first failure in left-to-right order. `Validation`: every error, in left-to-right operand order. |
| `ZipWith` | Combines with a lazy second operand. | `Result`, `UnitResult` | Same carrier | — | The factory is skipped after a first failure; use it (or `Bind`) when the second operation itself must not run. |
| `Apply` | Applies a validated function to a validated argument. | `Validation` | `Validation<TResult, TError>` | — | Applicative: errors from the function operand accumulate before errors from the argument. |
| `Select`/`SelectMany` | LINQ query-syntax bridge for `Map`/`Bind`. | `Option`, `Result`, `Effect` | Same carrier | — | Secondary to the member-centric vocabulary; exists only where `Bind` exists; never `Where`. |

### Inspection And Elimination

| Verb | Meaning | Present on | Output shape | Async forms | Key contract |
| --- | --- | --- | --- | --- | --- |
| `TryGet*` | Non-throwing case access (`TryGetValue`, `TryGetError`, `TryGetErrors`, `TryGetChange`, `TryGet`). | `Option`, `Result`, `UnitResult`, `Validation`, `TransitionResult`, `Patch<T>` | `bool` + `out` payload | — | Returns `false` for the inactive case with a `default` payload; never throws for a constructed carrier. A patch lookup returns only the change recorded for that exact field binding. |
| `Match` | Executes exactly one branch for the active case. | `Option`, `Result`, `UnitResult`, `Validation`, `TransitionResult` | Caller-chosen result | — | Branches validated at entry; the selected branch may return runtime null because it leaves the abstraction. |

### Traversal And Sequences

| Verb | Meaning | Present on | Output shape | Async forms | Key contract |
| --- | --- | --- | --- | --- | --- |
| `Sequence` | Collects a sequence of carriers into a carrier of the materialized list. | `IEnumerable<Option/Result/Validation/UnitResult>` | `Option<IReadOnlyList<T>>`, `Result<IReadOnlyList<T>, TError>`, `Validation<IReadOnlyList<T>, TError>`, or `UnitResult<TError>` | `SequenceAsync` (± token) | Eager; single enumeration; source order; `Option`/`Result`/`UnitResult` fail fast, `Validation` accumulates every error. |
| `Traverse` | Applies a carrier-producing selector to every item and collects its successes. | `IEnumerable<T>` (plain, indexed, dictionary, located), `IReadOnlyDictionary<TKey, TValue>` (keyed, located), `IAsyncEnumerable<T>` (plain, located) | Same as `Sequence`; dictionary forms produce `carrier<IReadOnlyDictionary<TKey, TResult>>` | `TraverseAsync` (sync selector), `TraverseValueAsync` (± token); located async forms are experimental (`FS0017`) | Selector invoked once per reached item in source order; indexed/keyed/located overloads pass each reached item's index, key, or composed `Location`; dictionary forms preserve the dictionary shape; same fail-fast versus accumulation split; iterative (no recursion depth). |
| `*OrNone` | Cardinality access whose absence is a value with exactly one meaning per member. | `IEnumerable<T>`, `IAsyncEnumerable<T>` (mirrored set) | `Option<T>`; `ToNonEmptyOrNone` produces `Option<NonEmpty<T>>` | `FirstOrNoneAsync`, `LastOrNoneAsync`, `SingleOrNoneAsync`, `ElementAtOrNoneAsync`, `MinOrNoneAsync`, `MaxOrNoneAsync`, `ToNonEmptyOrNoneAsync` (± token) | Eager; single enumeration; `SingleOrNone` collapses empty-or-multiple (documented; `ToNonEmptyOrNone` + `Rest.Count` distinguishes); `Min`/`Max` skip nulls, all-null or empty is `None`. |
| `Partition` | Splits a sequence in one enumeration into meaning-bearing sides. | `IEnumerable<T>` (predicate, `Option`, `Result`, `UnitResult`), `IAsyncEnumerable<T>` (predicate, `Result`) | `Partition<T>`, `OptionPartition<T>`, `ResultPartition<TValue, TError>`, `UnitResultPartition<TError>` | `PartitionAsync` (predicate, `Result`; ± token) | Single enumeration; source order preserved per side; valueless sides (`Nones`, `Succeeded`) are counts; `ResultPartition` keeps errors, not failed values. |
| `ZipExact`/`ZipExactOrNone` | Combines two sequences exactly, rejecting unequal lengths. | `IEnumerable<T>` | `Option`/`Result` of `IReadOnlyList<(TFirst First, TSecond Second)>` | — | Unequal lengths are `None` or `Failure(lengthError(firstCount, secondCount))` with both total counts observable; each source enumerated once; truncation stays with BCL `Zip`. |
| `WhereNotNull` | Filters nulls in one deferred pass. | `IEnumerable<T?>`, `IAsyncEnumerable<T?>` (class and struct overloads) | Same sequence carrier without nulls | — (bare verb; no callback exists) | Deferred; single pass per consumer enumeration; source order; no caching or intermediate collection. |
| `Choose` | Fused Option-aware filter-map. | `IEnumerable<T>`, `IAsyncEnumerable<T>` (bare verb) | `IEnumerable<TResult>` / `IAsyncEnumerable<TResult>` | `ChooseValueAsync` (cancellation-aware chooser receives the enumeration token) | Deferred; single pass per consumer enumeration; source order; no intermediate collection or caching. |
| `Scan` | Running aggregate: yields the accumulator after each element. | `IEnumerable<T>`, `IAsyncEnumerable<T>` (bare verb) | Sequence of accumulators | `ScanValueAsync` (cancellation-aware accumulator receives the enumeration token) | Deferred; the seed is never yielded; an empty source yields nothing; for a non-empty source the last value equals `Aggregate`/`AggregateAsync`; accumulator exceptions propagate unchanged. |
| `AggregateResult` | Folds a sequence through a fallible accumulator, stopping at the first typed failure. | `IEnumerable<T>`, `IAsyncEnumerable<T>` | `Result<TAccumulate,TError>`; async source `ValueTask<Result<TAccumulate,TError>>` | `AggregateResultAsync` (sync step), `AggregateResultValueAsync` (`ValueTask` step; tokenless and cancellation-aware) | Eager; one enumeration; empty input returns the supplied seed; a failed step returns its error and makes no later source request or step call; no materialization; source/step exceptions, cancellation, and disposal keep normal .NET semantics; mutable accumulators are not rolled back. |
| `Aggregate` | Folds a nonempty sequence without a separate seed. | `NonEmpty<T>` | `T` | — | Starts from `First`, visits `Rest` once in order, and propagates callback exceptions; seeded non-fallible folds remain BCL operations. |
| `ToReadOnlyList` | Materializes the nonempty sequence into a fresh read-only list. | `NonEmpty<T>` | `IReadOnlyList<T>` | — | Copies `First` followed by `Rest`; the list and wrapper are allocated and do not alias the nonempty value's state. |

### One-Pass Summaries

`Summary<TSource, TState, TResult>` is a reusable sequence-summary description, not a carrier. A
leaf summary holds a seed or seed factory, an accumulate delegate, and a result delegate; a composed
summary holds component descriptions and no top-level seed or accumulate delegate. Evaluating either
shape creates state local to that call. `Summary<TSource>` supplies the built-in factories and
`Summary.Zip` composes independent descriptions. Summaries are total folds: every component observes
every source item, so there is no early-exit or partial-result protocol.

| Member | Meaning | Output shape | Key contract |
| --- | --- | --- | --- |
| `Create` | Defines an independent summary over a seed or seed factory. | `Summary<TSource, TState, TState>` or `Summary<TSource, TState, TResult>` | A value seed is not cloned; a seed factory runs once per component per evaluation; delegates are validated eagerly. |
| `Count`/`Sum`/`Min`/`Max` | Supplies the built-in count, checked sum, and extrema summaries. | `Summary<TSource, ...>` | `Count`/`Sum` use checked source-order arithmetic; empty `Sum` is zero; extrema are `Option<TKey>`, skip null keys, and retain the first tied key. |
| `Zip` | Combines two to four independent summaries into one evaluation. | Summary of a flat tuple state and result | Components initialize, accumulate, and finalize in operand order; each keeps its own typed state. |
| `MapResult` | Projects the final result once. | Summary with the projected result | Runs after traversal, or immediately for an empty source; a component exception prevents it. |
| `Evaluate`/`EvaluateAsync` | Runs the summary over a BCL sequence. | `TResult` / `ValueTask<TResult>` | Eager, one enumeration, source order, no materialization or cache; empty input makes no accumulate call; the async form forwards the exact token to the enumerator only and never blocks. |

### Strict Keyed Joins

`LookupIndex<TKey, TValue>` retains every occurrence of a key in lookup-source order under an
explicit comparer; `UniqueLookupIndex<TKey, TValue>` proves at most one value per key over the
complete indexed input. The buffered join validates the complete lookup input before success and
returns no partial output on failure. The streaming join checks uniqueness only for matched keys
and yields one outcome per source row. Neither shape claims the other's guarantee.

| Member | Meaning | Present on | Output shape | Key contract |
| --- | --- | --- | --- | --- |
| `ToLookupIndex`/`ToUniqueLookupIndex` | Builds a many-occurrence or complete-input-unique index in one enumeration. | `IEnumerable<TValue>` | `Validation<LookupIndex<...>, LookupError<TKey>>` / `Validation<UniqueLookupIndex<...>, LookupError<TKey>>` | Explicit comparer or `EqualityComparer<TKey>.Default`; null lookup keys are `NullKey` failures with positions; duplicate keys are retained for the many index and reported for the unique index. |
| `GetMany` | Reads every value for a key in lookup-source order. | `LookupIndex` | `IReadOnlyList<TValue>` | Explicit one-to-many read; empty for an absent key; no uniqueness check. |
| `GetZeroOrOne` | Reads a matched key that permits zero or one value. | `LookupIndex` | `Result<Option<TValue>, LookupError<TKey>>` | Zero is `Success(None)`; multiple is `DuplicateKey` with both lookup positions; `Option` rejects a present null value. |
| `GetExactlyOne` | Reads a key that must have exactly one value. | `LookupIndex` | `Result<TValue, LookupError<TKey>>` | Missing is a failure without a source position; multiple is `DuplicateKey`; a null value is a successful payload. |
| `GetOption` | Reads a key on a proven unique index. | `UniqueLookupIndex` | `Option<TValue>` | Absence is `None`; multiple is impossible; a present null value is rejected by `Option`. |
| `RequireUniqueKeys` | Validates at most one value per key over the complete retained index. | `LookupIndex` | `Validation<UniqueLookupIndex<TKey,TValue>, LookupError<TKey>>` | Every occurrence after the first is reported with its position; the wrapper shares the index storage. |
| `JoinExactlyOne` | Enriches every source row with exactly one lookup match. | `IEnumerable<TSource>` plus a raw lookup sequence or `UniqueLookupIndex` | `Validation<IReadOnlyList<TResult>, LookupError<TKey>>` | Buffered; complete-input uniqueness for the raw form; one output per source row in source order on success; no outputs on failure; lookup errors precede source errors. |
| `JoinExactlyOneResults` | Yields one independent outcome per source row. | `IEnumerable<TSource>` plus `LookupIndex` | `IEnumerable<Result<TResult, LookupError<TKey>>>` | Deferred; matched-key uniqueness only; failures are items; earlier successes may precede a later failure; no all-or-nothing claim. |

### Immediate Span And Memory Pipelines

| Verb | Meaning | Present on | Output shape | Async forms | Key contract |
| --- | --- | --- | --- | --- | --- |
| `SelectTo` | Projects every source item into caller-owned destination storage. | span and memory views | Written `Span<TResult>` / `Memory<TResult>` prefix | — | Immediate; source/destination must not overlap; whole-source capacity is validated before callbacks/writes; completed writes remain if the selector throws. |
| `WhereTo` | Copies matching items into caller-owned destination storage. | span and memory views | Written `Span<T>` / `Memory<T>` prefix | — | Immediate and source-ordered; nonoverlapping destination capacity must cover the whole source before callbacks; partial writes remain on a predicate fault. |
| `ChooseTo` | Writes each present chooser result into caller-owned destination storage. | span and memory views | Written `Span<TResult>` / `Memory<TResult>` prefix | — | Immediate, once per source item, and source-ordered; nonoverlapping capacity is validated before callbacks; no intermediate collection, and partial writes remain on a fault. |
| `SelectInPlace` | Replaces every item through a projection in its existing storage. | mutable span and memory views | Same span/memory view | — | Immediate, once per item; projections completed before a callback fault remain. |
| `WhereInPlace` | Stably compacts matching items in existing storage. | mutable span and memory views | Valid source prefix | — | Immediate and source-ordered; reference-containing tail slots are cleared after successful compaction, other tail values are unspecified, and completed moves remain on a predicate fault. |

### Async And Concurrency

| Verb | Meaning | Present on | Output shape | Async forms | Key contract |
| --- | --- | --- | --- | --- | --- |
| `SelectParallelValueAsync` | Bounded parallel mapping of an async stream. | `IAsyncEnumerable<T>` | `IAsyncEnumerable<TResult>` | cancellation-aware selector receives a linked operation token | Ordered streaming with `Channel` backpressure; one linked operation token per enumeration; cooperative drain on early disposal. |
| `SelectParallelCompletionOrderValueAsync` | Bounded parallel mapping of an async stream, delivered as selectors complete. | `IAsyncEnumerable<T>` | `IAsyncEnumerable<TResult>` | cancellation-aware selector receives a linked operation token | Same bounded backpressure, one-token, and drain contract as `SelectParallelValueAsync`; results arrive in completion order (concurrent completions in unspecified relative order); the delivery order is named, never a parameter. |
| `TraverseParallelValueAsync` | Bounded parallel traversal, materialized. | `IAsyncEnumerable<T>` | `ValueTask<carrier<IReadOnlyList<T>>>` | (± token) | Ordered values; `Option`/`Result` fail fast, `Validation` accumulates; bounded fan-out only. |
| `FirstSuccessAsync` | Bounded first-success selection over cold effects. | `Effect<Result<TValue, TError>>` | `ValueTask<Validation<TValue, TError>>` | (± `TimeProvider`, token; explicit-degree overload) | Finite input snapshotted once; legacy degree 32 or explicit positive bound; each observed candidate accounted once; drains all admitted work and propagates independent faults even after a winner; all typed failures produce `Invalid` in input order; coordinator-owned timeout only. |

### Conversion Bridges

| Verb | Meaning | Present on | Output shape | Async forms | Key contract |
| --- | --- | --- | --- | --- | --- |
| `ToOption` | Converts a nullable value or a `Result` to an `Option`. | `T?`, `Nullable<T>`, `Result<TValue, TError>` | `Option<T>` | `ToOptionAsync` over `Task` and `ValueTask` sources | Runtime null normalizes to `None`; `Result` failure details are dropped (an explicit, potentially lossy boundary). |
| `ToResult` | Converts absence or no-value success into a `Result`. | `Option` (error/factory), `UnitResult` (value factory) | `Result<TValue, TError>` | `ToResultAsync`, `ToResultValueAsync` (± token, `UnitResult`) | The `Option` error factory runs only for `None`; the `UnitResult` value factory runs only for success. |
| `ToUnitResult` | Drops the successful value and preserves the failure. | `Result`, `Option` (error/factory) | `UnitResult<TError>` | `ToUnitResultAsync` over `Task` and `ValueTask` sources | No dummy value is materialized; the `Option` error factory runs only for `None`. |
| `ToNullable` | Unwraps a value-type option. | `Option<T>` where `T : struct` | `Nullable<T>` | — | `None` becomes `null`; never throws for a present option. |
| `ParseOrNone` | Parses a string through `IParsable<T>` into an option; named `Parse*OrNone` bridges cover the common types. | `string` (± `IFormatProvider`) | `Option<T>` | — | Never throws for a parse failure; named bridges parse with the current culture; the provider overload forwards it unchanged; a null input string is an eager `ArgumentNullException`. |
| Container `*OrNone` | Translates container Try-pattern or sentinel outcomes into options. | `Queue<T>`, `Stack<T>`, `PriorityQueue<TElement, TPriority>`, `IList<T>`, `Dictionary<TKey, TValue>` | `Option<...>` | — | `None` means empty, not found, or an absent key exactly; mutating members (`DequeueOrNone`, `PopOrNone`, `RemoveOrNone`) mutate only on `Some`. |
| `ToHttpResult`/`ToHttpResultAsync` | Maps a carrier to `IResult` with an explicit problem mapper. | `Option`, `Result`, `UnitResult`, `Validation`, `Effect` (FunnySharp.AspNetCore) | `IResult`, `Task<IResult>`, or `ValueTask<IResult>` | Task, `ValueTask`, and `Effect` source forms | Faults and cancellation are never converted into HTTP responses; the effect forms call `RunAsync` with exactly `RequestAborted`. |
| `ToHttpResults`/`ToHttpResultsAsync` | Maps a carrier to a caller-selected ASP.NET Core typed result union. | `Option`, `Result`, `UnitResult`, `Validation` (FunnySharp.AspNetCore) | `Results<...>`, `Task<Results<...>>`, or `ValueTask<Results<...>>` | Task and `ValueTask` source forms | The union stays in the endpoint's static return shape; only the selected mapper runs, mapper faults propagate, and dynamic `ProblemHttpResult` statuses are not expanded into metadata. |

### Effects

| Verb | Meaning | Present on | Output shape | Async forms | Key contract |
| --- | --- | --- | --- | --- | --- |
| `FromValue`/`FromResult`/`FromSync`/`FromTask`/`FromValueTask` | Adapt a value or ordinary work into a deferred effect. | values, `Result`, sync/`Task`/`ValueTask` work (± token, ± environment) | `Effect<T>` or `Effect<TEnvironment, T>` | — | Creating or composing never executes; execution starts only at `RunAsync`. |
| `RunAsync` | Executes the effect. | `Effect` (± environment) | `ValueTask<T>` | (± token) | Forwards the exact caller token to every applicable delegate; faults and cancellation keep ordinary .NET semantics. |
| `WithEnvironment` | Lifts an environment-independent effect to one that accepts (and ignores) an environment. | `Effect<T>` | `Effect<TEnvironment, T>` | — | Pure delegation to `RunAsync`. |
| `Provide` | Supplies a fixed environment. | `Effect<TEnvironment, T>` | `Effect<T>` | — | The environment is an ordinary caller-owned value. |
| `Using`/`UsingAsync` | Scopes an `IDisposable`/`IAsyncDisposable` resource across an effect. | `Effect` | `Effect<TResult>` | — | Resource released exactly once after success, failure, exception, or cancellation; natural C# `using` precedence when both use and release fail. |

### Optics

| Verb | Meaning | Present on | Output shape | Async forms | Key contract |
| --- | --- | --- | --- | --- | --- |
| `Create` | Builds a total (`Lens`) or possibly absent (`Optional`) focus from caller delegates. | any source type | `Lens<TSource, TFocus>` / `Optional<TSource, TFocus>` | — | Lens laws are caller obligations; uninitialized optics throw on every operation. |
| `Identity` | Creates a total focus on the entire source. | `Lens` | `Lens<T, T>` | — | `Get` returns the source; `Set` returns the supplied replacement focus. |
| `Get` | Reads the focus of a total lens. | `Lens` | `TFocus` | — | Invokes the caller getter. |
| `GetOption` | Reads a possibly absent focus (also the keyed-lookup bridge). | `Optional`, keyed containers | `Option<TFocus>` | — | An absent focus is `None`; a missing key is distinct from a present default value. |
| `Set` | Writes the focus or marks a patch field for replacement. | `Lens`, `Optional`, `PatchChange<T>` | `TSource` for optics; `PatchChange<T>` for a patch state | — | An absent optional focus returns the exact original source without invoking the setter. `PatchChange.Set` rejects a null replacement; the change is applied only when a patch is applied. |
| `Update` | Applies a function to the focus, returning an updated source. | `Lens`, `Optional` | `TSource` | — | An absent optional focus skips both setter and updater. |
| `Compose` | Left-to-right optic composition. | `Lens`, `Optional` | `Lens` for total lens plus total lens; otherwise `Optional` | — | Any partial focus keeps the composed path partial; written and evaluated left to right. |

### Partial Changes

| Verb | Meaning | Present on | Output shape | Async forms | Key contract |
| --- | --- | --- | --- | --- | --- |
| `Clear` | Explicitly constructs a clearing instruction for a clearable patch field. | `PatchChange<T>` | `PatchChange<T>` | — | Distinct from `Unchanged` and from a null replacement; a replacement-only field rejects the instruction during decoding. |
| `With` | Records one field change in a patch. | `Patch<T>` | `Patch<T>` | — | Rejects an `Unchanged` change and a repeated field name; the returned patch is independent of the source patch. |
| `ApplyTo` | Applies recorded changes to a source, invoking each field setter or clear callback once in order. | `Patch<T>` | `PatchApplication<T>` | — | Creates a candidate, never mutates or clones the source itself; an empty patch preserves source identity; callback exceptions propagate with no rollback; no version check. |
| `Combine` | Combines two patches written for the same value type. | `Patch<T>` | `Validation<Patch<T>, PatchConflict>` | — | Any field name present in both operands is a conflict, even for equal values; a conflicted combine returns no partial merged patch and chooses no merge policy; no inverse is derived. |
| `Explain` | Renders allowed patch fields and candidate rules deterministically. | `PatchSchema<T>` | `string` | — | Field names, value descriptions, clearing policy, and the candidate contract explanation; no values or callbacks are rendered. |

### Domain Values

`Domain<TValue, TTag>` groups an underlying `Contract<TValue>` with a compile-time marker.
`DomainValue<TValue, TTag>` stores a value that passed that boundary. Construction is not a
pipeline verb: `Domain.Validate` checks and wraps an existing value, `Domain.CreateDecoder`
derives a decoder, and `Domain.Contract` exposes the tagged contract through
`Contract<T>.Widen`. Read with `Value` (throws when uninitialized) or `TryGetValue` (returns
`false`). State-specific types and typed module-error hierarchies remain ordinary C# records, so
their operations and pattern matches use the normal language grammar rather than new verbs.

| Member | Meaning | Present on | Output shape | Key contract |
| --- | --- | --- | --- | --- |
| `Validate` | Checks an underlying value and wraps it when the boundary accepts it. | `Domain<TValue, TTag>` | `Validation<DomainValue<TValue, TTag>, DecodeError>` | The underlying contract runs once; errors keep their order, kind, and path; a runtime null is a `Null` error. |
| `CreateDecoder` | Derives the tagged decoder from the underlying contract's decoder. | `Domain<TValue, TTag>` | `Result<Decoder<DomainValue<TValue, TTag>>, ContractLimitation>` | The tag is added only after the underlying decode and validation succeed; an unavailable decoder is a typed limitation. |
| `Contract` | Exposes the tagged contract derived through `Contract<T>.Widen`. | `Domain<TValue, TTag>` | `Contract<DomainValue<TValue, TTag>>` | Validation, decoding, generation, shrinking, and description stay aligned with the underlying rule where available; an uninitialized wrapper is rejected. |
| `Value` / `TryGetValue` | Reads the validated underlying value. | `DomainValue<TValue, TTag>` | `TValue` / `bool` + out payload | `Value` throws for an uninitialized wrapper; `TryGetValue` returns `false`; there is no implicit conversion and no public constructor accepting the underlying value. |

### Batched Reads

`BatchSource<TKey, TValue, TError>`, `BatchRequirement<TValue, TError>`, and `BatchScope` are an
explicit description/execution family, not carriers. A requirement's `Map`, `Bind`, and `Zip`
retain their ordinary meanings over a successful value; a failure skips dependent work, and a
source exception remains an exception. `BatchScope` owns all request buffering and result caching,
so no operation below has ambient cross-request behavior.

| Verb | Meaning | Present on | Output shape | Async forms | Key contract |
| --- | --- | --- | --- | --- | --- |
| `Read` | Declares one keyed read from an explicit batch-capable source. | `BatchRequirement` | `BatchRequirement<TValue, TError>` | — | The source is the only batching assumption; keys use its explicit comparer and are forwarded in first-request order. |
| `FromValue`/`Failure` | Lifts a successful value or a typed read failure into a description without reading. | `BatchRequirement` | `BatchRequirement<TValue, TError>` | — | Construction does not execute or cache; the failure remains a `BatchReadError<TError>` outcome. |
| `All` | Starts every independent description and keeps every outcome in input order. | `BatchRequirement` | `BatchRequirement<IReadOnlyList<Result<TValue, BatchReadError<TError>>>, TError>` | — | The input is snapshotted once; one inner failure does not short-circuit siblings and the outer result succeeds. |
| `Zip` | Starts two descriptions independently and combines successful values. | `BatchRequirement` | `BatchRequirement<(TFirst First, TSecond Second), TError>` | — | Both start before either failure is observed; a source exception from either operand is never hidden by a sibling domain failure. |
| `MapError` | Adapts per-key and whole-batch source errors to another typed error. | `BatchSource` | `BatchSource<TKey, TValue, TResultError>` | — | Applies only to source errors; missing and unexpected responses are unaffected, and the adapted source has its own cache identity. |
| `ExecuteAsync` | Executes one requirement under the scope's bounds and cache. | `BatchScope` | `ValueTask<Result<TValue, BatchReadError<TError>>>` | — | One execution at a time; the caller token and scope disposal are linked and forwarded; source exceptions and cancellation are not converted to `BatchReadError`. |
| `Invalidate`/`ClearCache` | Removes cached key results after a mutation. | `BatchScope` | `int` | — | Rejected during an active execution; invalidation is explicit and never triggers a read by itself. |

### State

| Verb | Meaning | Present on | Output shape | Async forms | Key contract |
| --- | --- | --- | --- | --- | --- |
| `To` | Constructs a state change and snapshots its outputs. | `StateChange<TState, TOutput>` | `StateChange<TState, TOutput>` | — | The output array is cloned; a null array is rejected, individual nullable outputs are retained. |
| `Applied`/`Rejected`/`Failed`/`Undefined` | Explicitly constructs one of the four transition statuses. | `TransitionResult` | `TransitionResult<TState, TOutput, TError>` | — | `Applied` requires a non-null state change; rejected/failed errors stay typed; `Undefined` is the meaningful default, not a caught exception. |
| `Then` | Composes two transitions; the first runs once and its state feeds the second. | `StateTransition` | `StateTransition` | — | Outputs are concatenated in execution order; the second transition is not run after a first-transition exception. |
| `OrElse` | Falls back to a second machine only when the first returns `Undefined`. | `StateMachine` | `StateMachine` | — | `Applied`, `Rejected`, and `Failed` pass through unchanged. |
| `Replay` | Applies a machine to an event history in order. | `StateMachine` + `IEnumerable<TEvent>` | `TransitionResult` (final state, output history) | — | Stops at the first non-applied result and preserves its status; an empty history succeeds with the initial state. |

## Fallible Composition Is `Bind`

The canonical way to compose fallible functions is `Bind` on the carrier: `Option.Bind` chains
compose absence-producing functions and `Result.Bind` chains compose failure-producing functions.
FunnySharp deliberately has no Kleisli-compose operator over `Func<T, Result<...>>` and no pipeline
hierarchy for fallible work (decision E74: adapt — document, do not add a wrapper type). The stages
compose through the carrier itself, and the chain replaces nested fallible calls:

```csharp
Func<string, Result<int, ParseError>> parseQuantity = ParseQuantity;
Func<int, Result<decimal, ParseError>> lookupUnitPrice = LookupUnitPrice;

Result<decimal, ParseError> lineTotal = parseQuantity(request.QuantityText)
    .Bind(lookupUnitPrice)
    .Map(unitPrice => unitPrice * request.Units);
```

A failure at any stage stops the chain and carries the first failure's error; the generic `Pipe`
extension already accepts Result-returning standard delegates, so no Result-specific pipeline or
delegate hierarchy is required. See [function composition](function-composition.md).

## Folding And Scanning

BCL `Aggregate`/`AggregateAsync` is the canonical fold; FunnySharp never duplicates it. `Scan` is
the one narrow running-aggregate addition (decision E73: adopt narrow — one operator, no pipeline
hierarchy), with these invariants:

- Deferred and single-pass per consumer enumeration, preserving source order.
- The seed is never yielded; the accumulator is yielded after each element.
- An empty source yields nothing.
- For a non-empty source, the last yielded value equals `Aggregate(seed, accumulate)` (or
  `AggregateAsync` for the asynchronous form).

`NonEmpty<T>.Aggregate(Func<T, T, T>)` is the one seedless fold addition: it cannot encounter
emptiness because the type carries the guarantee (`ToNonEmptyOrNone` is its only constructor
path), where BCL `Enumerable.Aggregate` throws `InvalidOperationException` on an empty sequence.

## Deliberate Absences

| Absence | Reason |
| --- | --- |
| `Where` on any carrier | A bare predicate cannot produce absence or an error; `Filter` (Option) and `Ensure` (Result/UnitResult) carry that meaning. |
| `Validation.Bind`/`SelectMany` | A dependent check cannot be evaluated independently of an earlier valid value, so it cannot honestly participate in error accumulation. |
| `Validation.ZipWith` | A lazy second would skip an independent check and hide its errors from accumulation. |
| `Option.ZipWith` | Valid but no call-site evidence yet; deferred. |
| `UnitResult.Map` | Renamed `ToResult`: `Map` keeps one shape across carriers (same carrier, value transform), and the success-to-value conversion joins the `To*` family. |
| `Apply` beyond `Validation` | Applicative application belongs to the accumulating carrier only. |
| Throwing accessors | Case access is `TryGet*`/`Match`; a throwing accessor hides the inactive case in an exception. |
| Implicit conversions | The created branch stays visible in code. |
| Operator overloads beyond `==`/`!=` | Other operators are tricks that hide control flow or cost. |
| `Curry`/`Partial`/`Flip` beyond binary arity | A larger overload family adds no discoverability; an ordinary lambda covers uncommon shapes. |
| `Zip` combine beyond arity 4 | Unbounded tuple towers are rejected; wider forms nest or use `Traverse`. |
| Decoder schema/introspection | `Decoder<T>` itself is a callable function; inspectable constraints and derivations belong to `Contract<T>` ([contracts](contracts.md)), not to the decoder. |
| Automatic cross-field generation or predicate inversion | A contract predicate is opaque; generation is unavailable until the caller supplies a generator, and generated values are checked rather than rejection-sampled. |
| Universal schema registry or reflection binder | A contract describes one value type through explicit C# accessors and contracts; no application-wide schema or reflection layer is added. |
| Test-framework dependency in the core package | Generation and shrinking return BCL values; FsCheck, Hedgehog, or xUnit integration belongs to the caller. |
| Async input decoder | The in-memory `JsonElement` input is synchronous; async is only an actual validation callback (`DecodeAsync`/`DecodeValueAsync`). |
| `TraverseParallelValueAsync` over `UnitResult<TError>` | Deferred to the concurrency goal absent a concrete consumer. |
| Ordering parameters on `SelectParallel*` | The delivery order is the method name (decision E82); a boolean or enum parameter would make one verb carry two hidden contracts. |
| Task-carrier operator universe (including `PipeAsync`) | "Mixed sync/async chains use ordinary `await` plus the synchronous vocabulary" (product contract); there is no `MapAsync`-returning-carrier chaining universe. |
| `Inspect`/`Pairwise`/`SlidingWindow` | `Inspect` duplicates `Tap`; no Goal 17 capability needs adjacent-pair or window shapes and the product contract forbids speculative feature APIs (decided once in Goal 17, decision E57). |
| `AverageOrNone` towers | Per-numeric-type overload towers rejected (decision E70); compose `ParseOrNone` with `Aggregate`, or check emptiness explicitly before BCL `Average`. |
| Async `ZipExact`, async indexed/keyed traversal mirrors | Decision E88: adopt the capability set on async carriers, not every overload; the exact-combination safety story is the synchronous data-cleaning path. |
| Concurrent container `*OrNone` bridges | `ConcurrentQueue`/`ConcurrentStack`/`ConcurrentDictionary` already expose Try-pattern members; wrapping them adds no safety. |
| A second seeded fold | BCL `Aggregate`/`AggregateAsync` stays canonical; only the seedless `NonEmpty<T>.Aggregate` adds the cannot-be-empty guarantee. |
| `NonEmpty<T>` as a collection | It implements no `IEnumerable` and no indexer; materialize with `ToReadOnlyList()` or use LINQ over `Rest`. |
| A general duplicate-sensitive dictionary builder | BCL `ToLookup`/`GroupBy` remain the ordinary duplicate-tolerant shapes; `LookupIndex` is a cardinality contract with source positions, not a replacement dictionary. It retains duplicates for `GetMany`/`GetZeroOrOne` or rejects them under `RequireUniqueKeys`/`JoinExactlyOne`; it never silently picks one. |
| A many-to-many or one-to-many join | Duplicate lookup keys never multiply output rows; explicit one-to-many expansion uses `GetMany` plus ordinary LINQ, and no Cartesian product is inferred. |
| A zero-or-one buffered join | `UniqueLookupIndex.GetOption` and `LookupIndex.GetZeroOrOne` express zero-or-one lookup; a join has no single natural absent-row output shape (dropping rows would break the output-count promise). |
| Async index or join mirrors | Index construction and joins are in-memory calculations after the caller acquires data; async acquisition remains the caller's boundary. |
| A dictionary-input index factory | `IReadOnlyDictionary<TKey, TValue>` does not reveal its equality policy; callers pass its pairs as a sequence and state the comparer explicitly. |

## Forbidden Mechanisms

- No reflection.
- No dynamic dispatch.
- No hidden scheduling.
- No higher-kinded-type or typeclass simulation.
- No operator tricks; the only operators anywhere are `==`/`!=` on the carriers.
- No magical implicit conversions on normal paths.

## How This Table Changes

This table is part of the stable contract and changes only through a later accepted goal, mirroring
the [product contract](product-contract.md). A verb row changes when its member changes through the
recorded decision process ([decision record](next-stage/decision-record.md),
[API decisions](next-stage/api-decisions.md)); per-feature guides, XML documentation, tests, and the
public API baseline change in the same change.
