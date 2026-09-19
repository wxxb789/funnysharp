# Functional API Grammar

FunnySharp has one small, orthogonal, predictable grammar. Every verb has one primary meaning and a
predictable output shape on every carrier where that meaning is valid, so a reader infers evaluation
order, short-circuiting, exception, cancellation, enumeration, and materialization behavior from the
signature and its contract.

This file is the authoritative verb reference. The per-feature guides
([option.md](option.md), [result.md](result.md), [unit-result.md](unit-result.md),
[validation.md](validation.md), [function-composition.md](function-composition.md),
[data-pipelines.md](data-pipelines.md), [effects.md](effects.md),
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
(`Func`, `Action`, `StateTransition`) are the composition surface. No second carrier for any of the
meanings above is added: `Maybe`, `Either`, `Fin`, `Try`, `IO`, `Reader`, `OptionAsync`, and async
outcome wrappers remain out of scope ([product contract](product-contract.md)).

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
- A `CancellationToken` is always an explicit parameter and is forwarded unchanged.
- Eager fallbacks are plain parameters (`OrElse`, `Recover`, `GetValueOr`); lazy fallbacks are
  factories (`OrElseWith`, `RecoverWith`, `GetValueOrElse`); `GetValueOrDefault` is the only member
  that may return `default(T)`.
- Sequence cardinality reserves the `*OrNone` suffix for a future goal (no current members); keyed lookups use `GetOption`; value conversion
  uses `ToOption`.
- LINQ `Select`/`SelectMany` exist only where `Bind` exists (never `Where`) and stay secondary to
  the member-centric vocabulary. `Bind` does not imply a LINQ bridge: `UnitResult` has none.
- No aliases, no competitor naming, no naming concessions.

## The Verb Table

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
| `Map` | Transforms the contained value, keeping the carrier. | `Option`, `Result`, `Validation`, `Effect` | Same carrier | `MapAsync`, `MapValueAsync` | Delegate validated at entry even when short-circuited; invoked at most once for the active case; `Option` normalizes a runtime-null result to `None`. |
| `MapAsync` | `Map` with a `Task`-returning selector. | `Option`, `Result`, `Validation` | `Task<carrier>` (± token) | — | An inactive case returns an already-completed carrier without invoking the selector or inspecting the token. |
| `MapValueAsync` | `Map` with a `ValueTask`-returning selector. | `Option`, `Result`, `Validation` | `ValueTask<carrier>` (± token) | — | Same short-circuit contract; each `ValueTask` is awaited once. |
| `Bind` | Sequences a carrier-returning function and stops at the first absence or failure. | `Option`, `Result`, `UnitResult`, `Effect` | Same carrier | `BindAsync`, `BindValueAsync` | Callback skipped after failure and never invoked for the inactive case; callback exceptions propagate unchanged. |
| `BindAsync` | `Bind` with a `Task`-returning binder. | `Option`, `Result`, `UnitResult` | `Task<carrier>` (± token) | — | Same short-circuit and exact-token-forwarding contract. |
| `BindValueAsync` | `Bind` with a `ValueTask`-returning binder. | `Option`, `Result`, `UnitResult` | `ValueTask<carrier>` (± token) | — | Same short-circuit contract; each `ValueTask` is awaited once. |
| `MapError` | Transforms the failure while preserving success. | `Result`, `UnitResult` | Same carrier | — | Selector skipped after success; invoked at most once. |
| `MapErrors` | Transforms every accumulated error while preserving a valid value. | `Validation` | Same carrier | — | Selector invoked once per error in order; skipped for a valid value. |
| `Filter` | Keeps a present value only when a predicate accepts it. | `Option` | `Option<T>` | — | Predicate skipped for `None`; a `Some` whose predicate fails becomes `None`. |
| `Ensure` | Keeps a success only when a predicate accepts it, otherwise returns the supplied failure. | `Result`, `UnitResult` | Same carrier | — | Predicate skipped after failure; an overload takes a lazy error factory. |

### Fallback And Recovery

| Verb | Meaning | Present on | Output shape | Async forms | Key contract |
| --- | --- | --- | --- | --- | --- |
| `Recover` | Eagerly replaces a failure with a value. | `Result` | `Result<TValue, TError>` (success-shaped) | — | Recovery skipped after success; invoked at most once. |
| `RecoverWith` | Lazily replaces a failure with another value of the same carrier. | `Result`, `UnitResult` | Same carrier | — | Factory skipped after success; `UnitResult` has no plain `Recover` because there is no value to produce eagerly. |
| `OrElse` | Returns the first value when it is present, otherwise an eager fallback. | `Option`, `StateMachine` | `Option<T>`; machine | — | Fallback skipped for `Some`; on machines, the fallback runs only when the first returns `Undefined`. |
| `OrElseWith` | Returns the first value when present, otherwise invokes a fallback factory. | `Option` | `Option<T>` | — | Factory validated at entry and invoked only for `None`. |
| `GetValueOr` | Returns the contained value or an eager non-null fallback. | `Option` | `T` (non-null) | — | Fallback validated before the option is examined. |
| `GetValueOrElse` | Returns the contained value or invokes a fallback factory. | `Option` | `T` (non-null) | — | Factory invoked only for `None`; a null factory result is rejected. |
| `GetValueOrDefault` | Returns the contained value or `default`. | `Option` | `T` (may be `default`) | — | The only member that may return `default(T)`; named to say so. |

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
| `TryGet*` | Non-throwing case access (`TryGetValue`, `TryGetError`, `TryGetErrors`, `TryGetChange`). | `Option`, `Result`, `UnitResult`, `Validation`, `TransitionResult` | `bool` + `out` payload | — | Returns `false` for the inactive case with a `default` payload; never throws for a constructed carrier. |
| `Match` | Executes exactly one branch for the active case. | `Option`, `Result`, `UnitResult`, `Validation`, `TransitionResult` | Caller-chosen result | — | Branches validated at entry; the selected branch may return runtime null because it leaves the abstraction. |

### Traversal And Sequences

| Verb | Meaning | Present on | Output shape | Async forms | Key contract |
| --- | --- | --- | --- | --- | --- |
| `Sequence` | Collects a sequence of carriers into a carrier of the materialized list. | `IEnumerable<Option/Result/Validation/UnitResult>` | `Option<IReadOnlyList<T>>`, `Result<IReadOnlyList<T>, TError>`, `Validation<IReadOnlyList<T>, TError>`, or `UnitResult<TError>` | `SequenceAsync` (± token) | Eager; single enumeration; source order; `Option`/`Result`/`UnitResult` fail fast, `Validation` accumulates every error. |
| `Traverse` | Applies a carrier-producing selector to every item and collects its successes. | `IEnumerable<T>` | Same as `Sequence` | `TraverseAsync` (sync selector), `TraverseValueAsync` (± token) | Selector invoked once per reached item in source order; same fail-fast versus accumulation split; iterative (no recursion depth). |
| `Choose` | Fused Option-aware filter-map. | `IEnumerable<T>`, `IAsyncEnumerable<T>` (bare verb) | `IEnumerable<TResult>` / `IAsyncEnumerable<TResult>` | `ChooseValueAsync` (cancellation-aware chooser receives the enumeration token) | Deferred; single pass per consumer enumeration; source order; no intermediate collection or caching. |
| `Scan` | Running aggregate: yields the accumulator after each element. | `IEnumerable<T>`, `IAsyncEnumerable<T>` (bare verb) | Sequence of accumulators | `ScanValueAsync` (cancellation-aware accumulator receives the enumeration token) | Deferred; the seed is never yielded; an empty source yields nothing; for a non-empty source the last value equals `Aggregate`/`AggregateAsync`; accumulator exceptions propagate unchanged. |

### Async And Concurrency

| Verb | Meaning | Present on | Output shape | Async forms | Key contract |
| --- | --- | --- | --- | --- | --- |
| `SelectParallelValueAsync` | Bounded parallel mapping of an async stream. | `IAsyncEnumerable<T>` | `IAsyncEnumerable<TResult>` | cancellation-aware selector receives a linked operation token | Ordered streaming with `Channel` backpressure; one linked operation token per enumeration; cooperative drain on early disposal. |
| `TraverseParallelValueAsync` | Bounded parallel traversal, materialized. | `IAsyncEnumerable<T>` | `ValueTask<carrier<IReadOnlyList<T>>>` | (± token) | Ordered values; `Option`/`Result` fail fast, `Validation` accumulates; bounded fan-out only. |
| `FirstSuccessAsync` | First-success race over cold effects. | `Effect<Result<TValue, TError>>` | `ValueTask<Validation<TValue, TError>>` | (± `TimeProvider`, token) | Drains all started work; a winner is `Valid`, an all-typed-failure race is `Invalid` in input order; coordinator-owned timeout only. |

### Conversion Bridges

| Verb | Meaning | Present on | Output shape | Async forms | Key contract |
| --- | --- | --- | --- | --- | --- |
| `ToOption` | Converts a nullable value or a `Result` to an `Option`. | `T?`, `Nullable<T>`, `Result<TValue, TError>` | `Option<T>` | `ToOptionAsync` over `Task` and `ValueTask` sources | Runtime null normalizes to `None`; `Result` failure details are dropped (an explicit, potentially lossy boundary). |
| `ToResult` | Converts absence or no-value success into a `Result`. | `Option` (error/factory), `UnitResult` (value factory) | `Result<TValue, TError>` | `ToResultAsync`, `ToResultValueAsync` (± token, `UnitResult`) | The `Option` error factory runs only for `None`; the `UnitResult` value factory runs only for success. |
| `ToUnitResult` | Drops the successful value and preserves the failure. | `Result`, `Option` (error/factory) | `UnitResult<TError>` | `ToUnitResultAsync` over `Task` and `ValueTask` sources | No dummy value is materialized; the `Option` error factory runs only for `None`. |
| `ToNullable` | Unwraps a value-type option. | `Option<T>` where `T : struct` | `Nullable<T>` | — | `None` becomes `null`; never throws for a present option. |
| `ToHttpResult`/`ToHttpResultAsync` | Maps a carrier to `IResult` with an explicit problem mapper. | `Option`, `Result`, `UnitResult`, `Validation`, `Effect` (FunnySharp.AspNetCore) | `IResult`, `Task<IResult>`, or `ValueTask<IResult>` | Task, `ValueTask`, and `Effect` source forms | Faults and cancellation are never converted into HTTP responses; the effect forms call `RunAsync` with exactly `RequestAborted`. |

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
| `Get` | Reads the focus of a total lens. | `Lens` | `TFocus` | — | Invokes the caller getter. |
| `GetOption` | Reads a possibly absent focus (also the keyed-lookup bridge). | `Optional`, keyed containers | `Option<TFocus>` | — | An absent focus is `None`; a missing key is distinct from a present default value. |
| `Set` | Writes the focus, returning an updated source. | `Lens`, `Optional` | `TSource` | — | An absent optional focus returns the exact original source without invoking the setter. |
| `Update` | Applies a function to the focus, returning an updated source. | `Lens`, `Optional` | `TSource` | — | An absent optional focus skips both setter and updater. |
| `Compose` | Left-to-right optic composition. | `Lens`, `Optional` | Same optic kind | — | Written and evaluated left to right. |

### State

| Verb | Meaning | Present on | Output shape | Async forms | Key contract |
| --- | --- | --- | --- | --- | --- |
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
| `TraverseParallelValueAsync` over `UnitResult<TError>` | Deferred to the concurrency goal absent a concrete consumer. |
| Task-carrier operator universe (including `PipeAsync`) | "Mixed sync/async chains use ordinary `await` plus the synchronous vocabulary" (product contract); there is no `MapAsync`-returning-carrier chaining universe. |

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
