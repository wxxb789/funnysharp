# FunnySharp public surface inventory (F1–F11)

Evidence pin: commit `4dbebd94b7b58648632112b7ca47c39cc517f153` (main), version `0.1.0`,
assemblies built from `src/FunnySharp` and `src/FunnySharp.AspNetCore`
(`src/*/bin/Release/net10.0/*.dll`).

Machine dumps (verbatim copies in `docs/next-stage/inventory/generated/`):

- `generated/inv-funny-sharp-core.md` / `.json` — FunnySharp 0.1.0.0, 33 types, 254 members
  (222 methods incl. 98 extension methods, 14 properties, 8 operators, 3 constructors, 3 delegate
  Invoke members, 4 enum values). SHA256 (md) `95cd11da03735613c1ad757d67e3ba74b4e338a1265dbac6cbcd69a2690bd998`,
  (json) `e5f50f283e91f5f07a34dfb69238cdebcc806abc2892c9aa2e4fd8039b9bdb7b`.
- `generated/inv-funny-sharp-aspnetcore.md` / `.json` — FunnySharp.AspNetCore 0.1.0.0, 1 type,
  15 extension methods. SHA256 (md) `a33cde57ebbea808bffff6b07b287ab4513fda1d9060ec7c4e94fd13e608cc8e`.

Other evidence used here: XML docs from the same build (`src/FunnySharp/bin/Release/net10.0/FunnySharp.xml`
— 33 types, 224 methods, 14 properties, 4 fields = 275 documented members; and
`src/FunnySharp.AspNetCore/bin/Release/net10.0/FunnySharp.AspNetCore.xml` — 16 documented members);
local test run 2026-09-17 on Linux x64, SDK 10.0.400 / runtime 10.0.11: `FunnySharp.Tests` 305 passed,
`FunnySharp.AspNetCore.Tests` 18 passed, 0 failed/skipped.

This file is a shape inventory, not a decision record. Decisions are in
`docs/next-stage/analysis/funny-sharp-surface.md`. Full member lists are always the generated dumps;
signatures below are representative and, unless noted, copied from the dumps.

## Family summary

| Family | Theme | Types (primary family) |
| --- | --- | --- |
| F1 | Absence: Option and nullable/Try/dictionary bridges | `Option`, `Option<T>`, `OptionExtensions`, `TryOperation<T>` |
| F2 | Fail-fast value outcome | `Result`, `Result<TValue,TError>`, `ResultExtensions` |
| F3 | Accumulation | `Validation<TValue,TError>`, `ValidationExtensions` |
| F4 | Function grammar | `FunctionExtensions` |
| F5 | Collections and traversal | `EnumerablePipelineExtensions`, `AsyncEnumerablePipelineExtensions`, `SequenceExtensions`, `AsyncSequenceExtensions`, `SpanPipelineExtensions` |
| F6 | Async, streaming, concurrency | `ParallelAsyncEnumerableExtensions`, `ParallelAsyncSequenceExtensions`, `ConcurrentEffectExtensions` (also F5 types above) |
| F7 | Effects, resources, environment | `Effect`, `Effect<T>`, `Effect<TEnvironment,T>`, `EffectResourceExtensions` |
| F8 | State transitions and machines | `TransitionStatus`, `StateChange<TState,TOutput>`, `StateTransition<TState,TOutput>`, `StateTransitionExtensions`, `TransitionResult<TState,TOutput,TError>`, `StateMachine<TState,TEvent,TOutput,TError>`, `StateMachineExtensions` |
| F9 | Optics and immutability | `Lens`, `Lens<TSource,TFocus>`, `Optional`, `Optional<TSource,TFocus>` |
| F10 | HTTP integration | `HttpResultExtensions` (FunnySharp.AspNetCore only) |
| F11 | Cross-cutting | Applies to every type: vocabulary, stability, defaults, nullability, analyzers, serialization, performance, packaging |

No `Maybe<T>`, `UnitResult<TError>`, result-collection type, prism/traversal/iso type, or DI type
exists (verified by the type list in `generated/inv-funny-sharp-core.md` and
`grep -rn "UnitResult" src/` → no matches).

---

## F1 — Absence

Purpose: explicit presence/absence without `null` ambiguity; bridges to nullable, Try-pattern,
dictionary, `Task`, and `ValueTask` conventions. Semantics:
`docs/option.md:3-10`; boundaries: `docs/option.md:49-61`.

### `Option` (class, static; 5 members) — dump: `generated/inv-funny-sharp-core.md` "Option (class [static])"

| Operation family | Representative signatures | Notes |
| --- | --- | --- |
| Some / None | `Option<T> Some<T>(T value)`; `Option<T> None<T>()` | `Some` rejects runtime-null (`Option.cs:25`, `Option.cs:105-108`); policy `docs/option.md:31`. |
| Nullable conversion | `Option<T> FromNullable<T>(T? value) where T : class`; `Option<T> FromNullable<T>(T? value) where T : struct` | Two constrained overloads; dedicated conversion unwraps `Nullable<T>` (`docs/option.md:27`; `Option.cs:40-52`). |
| Try-pattern | `Option<T> FromTry<T>(TryOperation<T> operation)` | Invokes once, never catches (`docs/option.md:39`; `Option.cs:62-67`). |

### `Option<T>` (readonly struct : `IEquatable<Option<T>>`; 22 members)

| Operation family | Representative signatures | Notes |
| --- | --- | --- |
| Case / factory | `bool IsSome { get; }`; `bool IsNone { get; }`; `static Option<T> None { get; }`; `static Option<T> Some(T value)` | `default(Option<T>)` is `None` (`Option.cs:87`; test `tests/FunnySharp.Tests/OptionTests.cs:64-73`). |
| Inspection | `bool TryGetValue([NotNullWhen(true)] out T? value)`; `TResult Match<TResult>(Func<T,TResult> some, Func<TResult> none)`; `void Match(Action<T>, Action)` | No throwing accessor, no implicit conversion (`docs/option.md:10`). |
| Transform | `Option<TResult> Map<TResult>(Func<T,TResult>)`; `Option<TResult> Bind<TResult>(Func<T,Option<TResult>>)`; `Option<T> Filter(Func<T,bool>)`; `Option<(T First,TSecond Second)> Zip<TSecond>(Option<TSecond>)` | `Map` normalizes null to `None`; `Bind` returns the callback option unchanged (`Option.cs:162-180`; `docs/option.md:35`). |
| Value fallback | `T GetValueOr(T fallback)`; `T GetValueOrElse(Func<T> fallbackFactory)`; `T? GetValueOrDefault()` (`[return: MaybeNull]`) | Eager fallback validated before case inspection; lazy factory invoked only for `None`; `GetValueOrDefault` may return runtime-null (`Option.cs:212-245`; `docs/option.md:37`). |
| Option fallback | `Option<T> OrElse(Option<T> fallback)`; `Option<T> OrElseWith(Func<Option<T>> fallbackFactory)` | `OrElseWith` validates at entry, invokes only for `None` (`Option.cs:259-263`). |
| Equality / text | `bool Equals(Option<T>)`; `GetHashCode`; `static ==`/`!=`; `ToString()` | `None == None`; `Some` uses `EqualityComparer<T>.Default` (`docs/option.md:41`). |
| LINQ aliases | none | Deliberate; see `docs/option.md:58-59`. |

### `OptionExtensions` (class, static; 15 members)

| Operation family | Representative signatures | Notes |
| --- | --- | --- |
| Async Task transform | `Task<Option<TResult>> MapAsync<T,TResult>(this Option<T>, Func<T,Task<TResult>>)`; `BindAsync`; each with `Func<T,CancellationToken,Task<...>>` + token | `MapAsync`/`BindAsync` = Task callbacks (`OptionExtensions.cs:56-127`). |
| Async ValueTask transform | `ValueTask<Option<TResult>> MapValueAsync<...>(this Option<T>, Func<T,ValueTask<TResult>>)`; `BindValueAsync`; each with token-aware overload | `ValueAsync` = ValueTask callbacks (`OptionExtensions.cs:140-224`). |
| Dictionary bridge | `Option<TValue> GetOption<TKey,TValue>(this IReadOnlyDictionary<TKey,TValue>, TKey key)` | Missing key and present runtime-null both become `None` (`docs/option.md:39`). |
| Nullable bridge | `Option<T> ToOption<T>(this T? value) where T : class`; `... where T : struct` | Same two-overload shape as `FromNullable`. |
| Async nullable bridge | `Task<Option<T>> ToOptionAsync<T>(this Task<T>)` / `(Task<T?>)`; ValueTask variants | Normalizes a null completion to `None`; faults/cancellation propagate (`docs/option.md:43-47`). |

### `TryOperation<T>` (sealed delegate; dump member: `delegate bool Invoke(out T value)`)

Try-pattern adapter shape used only by `Option.FromTry<T>`; annotated `[MaybeNull] out T value`
(`Option.cs:11`).

---

## F2 — Fail-fast value outcome

Purpose: explicit success or typed failure with fail-fast composition; explicit exception
boundaries. Semantics: `docs/result.md:3-7`; boundaries `docs/result.md:107-116`.

### `Result` (class, static; 6 members)

| Operation family | Representative signatures | Notes |
| --- | --- | --- |
| Sync exception boundary | `Result<TValue,Exception> Try<TValue>(Func<TValue>)`; `Result<TValue,TError> Try<TValue,TError>(Func<TValue>, Func<Exception,TError>)` | `OperationCanceledException` excluded from mapping (`Result.cs:40`; `docs/result.md:85-90`). |
| Task boundary | `Task<Result<TValue,Exception>> TryAsync<TValue>(Func<Task<TValue>>)`; mapper overload | Tokenless by design; null task faults with `InvalidOperationException` (`Result.cs:53-60`, `Result.cs:303-307`). |
| ValueTask boundary | `ValueTask<Result<TValue,Exception>> TryValueAsync<TValue>(Func<ValueTask<TValue>>)`; mapper overload | Observed once (`Result.cs:94-141`). |

### `Result<TValue,TError>` (readonly struct : `IEquatable<...>`; 25 members)

| Operation family | Representative signatures | Notes |
| --- | --- | --- |
| Case / factory | `bool IsSuccess { get; }`; `bool IsFailure { get; }`; `static Success(TValue value)`; `static Failure(TError error)` | `default(Result<TValue,TError>)` is a failure containing `default(TError)` (`Result.cs:385-392`; `docs/result.md:13`; test `tests/FunnySharp.Tests/ResultTests.cs:8-19`). Case is independent of payload; `Success(null)`/`Failure(null)` preserved (`tests/FunnySharp.Tests/ResultTests.cs:21-31`). |
| Inspection | `bool TryGetValue([MaybeNull] out TValue)`; `bool TryGetError([MaybeNull] out TError)`; `Match` (func + action) | No throwing accessor (`docs/result.md:15-17`). |
| Fail-fast transform | `Map<TResult>`; `Bind<TResult>`; `MapError<TResultError>`; `Ensure` (eager error + factory); `Recover`; `RecoverWith` | Callbacks validated at entry even when short-circuited; invoked at most once (`Result.cs:451-552`; `docs/result.md:45-52`). |
| Combination | `Zip<TSecond>`; `ZipWith<TSecond>(Func<Result<TSecond,TError>>)` | Left failure wins; `ZipWith` skips the factory after failure (`docs/result.md:26-27`). |
| LINQ aliases | `Select<TResult>`; `SelectMany<TIntermediate,TResult>` | Query syntax supported; no `Where` because a `bool` cannot create `TError` (`docs/result.md:29-32`; `Result.cs:600-631`). |
| Equality / text | `Equals`; `GetHashCode`; `==`/`!=`; `ToString()` | Case + payload structural (`docs/result.md:54-57`). |

### `ResultExtensions` (class, static; 11 members)

| Operation family | Representative signatures | Notes |
| --- | --- | --- |
| Async Task transform | `Task<Result<TResult,TError>> MapAsync/BindAsync<TValue,TError,TResult>(...)` with Task callbacks ± token | `ResultExtensions.cs:66-161`. |
| Async ValueTask transform | `ValueTask<Result<TResult,TError>> MapValueAsync/BindValueAsync(...)` ± token | `ResultExtensions.cs:170-270`. |
| Option interop | `Option<TValue> ToOption<TValue,TError>(this Result<TValue,TError>)`; `Result<TValue,TError> ToResult<TValue,TError>(this Option<TValue>, TError error)`; `ToResult(..., Func<TError> errorFactory)` | `Success(null)` → `None` is an explicit lossy boundary; factory lazy (`docs/result.md:34-41`). |

---

## F3 — Accumulation

Purpose: valid value or one or more independent errors, with deterministic left-to-right
accumulation. Semantics: `docs/validation.md:3-5`; boundaries `docs/validation.md:103-110`.

### `Validation<TValue,TError>` (readonly struct : `IEquatable<...>`; 18 members)

| Operation family | Representative signatures | Notes |
| --- | --- | --- |
| Case / factory | `bool IsValid { get; }`; `bool IsInvalid { get; }`; `static Valid(TValue)`; `static Invalid(TError)`; `static InvalidMany(IEnumerable<TError>)` | Default is `Invalid([default(TError)])` (`Validation.cs:12-13`; `docs/validation.md:11`; test `tests/FunnySharp.Tests/ValidationTests.cs:8-19`). `InvalidMany` snapshots and rejects null/empty (`Validation.cs:57-68`). |
| Inspection | `TryGetValue`; `TryGetErrors([NotNullWhen(true)] out IReadOnlyList<TError>?)`; `Match` (func + action) | No throwing accessor (`docs/validation.md:14`). |
| Transform | `Map<TResult>`; `MapErrors<TResultError>` | No `Bind`/`SelectMany`/`ZipWith` by design (`docs/validation.md:30-33`). |
| Applicative combination | `Zip<TSecond>`; `Apply<TValue,TResult,TError>(this Validation<Func<TValue,TResult>,TError>, Validation<TValue,TError>)` | Errors accumulate left before right (`Validation.cs:177-211`; `ValidationExtensions.cs`; `docs/validation.md:25-28`). |
| Equality / text | `Equals`; `GetHashCode`; `==`/`!=`; `ToString()` | Ordered error comparison (`docs/validation.md:39-41`). |

### `ValidationExtensions` (class, static; 1 member) — `Apply` only; see dump.

---

## F4 — Function grammar

Purpose: small standard-delegate surface over ordinary `Func`/`Action`. Semantics:
[function-composition.md — API Shape](../../function-composition.md#api-shape); boundaries
[function-composition.md — Deliberate Boundaries](../../function-composition.md#deliberate-boundaries).

### `FunctionExtensions` (class, static; 15 members)

| Operation family | Representative signatures | Notes |
| --- | --- | --- |
| Pipe | `TResult Pipe<T,TResult>(this T value, Func<T,TResult> function)` | No `PipeAsync`; an async delegate returns an awaitable naturally ([Deliberate Boundaries](../../function-composition.md#deliberate-boundaries)). |
| Compose | `Func<T,TResult> Compose<T,TIntermediate,TResult>(this Func<T,TIntermediate>, Func<TIntermediate,TResult>)` | Left-to-right (`FunctionExtensions.cs:31-39`). |
| ComposeAsync | `Func<T,Task<TResult>> ComposeAsync(...)`; `Func<T,ValueTask<TResult>> ComposeAsync(...)`; two token-aware variants | One name covers both carrier kinds (`FunctionExtensions.cs:130-211`). |
| Curry / Uncurry | `Func<TFirst,Func<TSecond,TResult>> Curry<TFirst,TSecond,TResult>(this Func<TFirst,TSecond,TResult>)`; `Uncurry` inverse | Binary only ([Deliberate Boundaries](../../function-composition.md#deliberate-boundaries)). |
| Partial / Flip | `Func<TSecond,TResult> Partial<TFirst,TSecond,TResult>(this Func<TFirst,TSecond,TResult>, TFirst first)`; `Flip` reverses two args | Binary only. |
| Observation | `T Tap<T>(this T value, Action<T>)`; `Task<T> TapAsync<T>(...)`; `ValueTask<T> TapValueAsync<T>(...)`; token-aware variants | Task vs ValueTask names follow the `Async`/`ValueAsync` convention (`FunctionExtensions.cs:220-271`). |

---

## F5 — Collections and traversal

Purpose: keep BCL sequence/span/memory carriers; add fused Option-aware filter-map, traversal,
and caller-buffered span helpers. Semantics: `docs/data-pipelines.md:3-7`,
`docs/validation.md:43-66`.

### `EnumerablePipelineExtensions` (class, static; 1 member)

| Operation family | Signature | Notes |
| --- | --- | --- |
| Fused filter-map | `IEnumerable<TResult> Choose<TSource,TResult>(this IEnumerable<TSource>, Func<TSource,Option<TResult>>)` | Deferred, source order, single enumeration per consumer (`docs/data-pipelines.md:11-14`). |

### `AsyncEnumerablePipelineExtensions` (class, static; 3 members) — also F6

| Operation family | Signatures | Notes |
| --- | --- | --- |
| Fused filter-map | `IAsyncEnumerable<TResult> Choose<TSource,TResult>(..., Func<TSource,Option<TResult>>)`; `ChooseValueAsync(..., Func<TSource,ValueTask<Option<TResult>>>)`; `ChooseValueAsync(..., Func<TSource,CancellationToken,ValueTask<Option<TResult>>>)` | Pull-based, deferred, one item at a time; no Task-chooser overload (`AsyncEnumerablePipelineExtensions.cs:21-66`; `docs/data-pipelines.md:19-28`). |

### `SequenceExtensions` (class, static; 6 members)

| Operation family | Signatures | Notes |
| --- | --- | --- |
| Option collect/traverse | `Option<IReadOnlyList<TValue>> Sequence(this IEnumerable<Option<TValue>>)`; `Option<IReadOnlyList<TResult>> Traverse(this IEnumerable<TSource>, Func<TSource,Option<TResult>>)` | Fail-fast; eager materialization; iterative (`SequenceExtensions.cs:17-55`). |
| Result collect/traverse | `Result<IReadOnlyList<TValue>,TError> Sequence(...)`; `Traverse(...)` | Stops at first failure, returns that error (`SequenceExtensions.cs:67-107`). |
| Validation collect/traverse | `Validation<IReadOnlyList<TValue>,TError> Sequence(...)`; `Traverse(...)` | Scans all reached items, accumulates errors in source order (`SequenceExtensions.cs:120-176`). |
| Location context | none | Traversal results carry no index/key/path (goal-17 gap; see analysis memo). |

### `AsyncSequenceExtensions` (class, static; 12 members) — also F6

| Operation family | Signatures | Notes |
| --- | --- | --- |
| Option | `SequenceAsync(IAsyncEnumerable<Option<TValue>>, CancellationToken = default)`; `TraverseAsync(..., Func<TSource,Option<TResult>>, token)`; `TraverseValueAsync(..., Func<TSource,ValueTask<Option<TResult>>>, token)`; `TraverseValueAsync(..., Func<TSource,CancellationToken,ValueTask<Option<TResult>>>, token)` | `TraverseAsync` takes a **synchronous** selector over an async source; `TraverseValueAsync` takes a ValueTask selector (`AsyncSequenceExtensions.cs:19-116`). |
| Result | same 4-shape set for `Result<TValue,TError>` | `AsyncSequenceExtensions.cs:130-...`. |
| Validation | same 4-shape set for `Validation<TValue,TError>` | Accumulates; `await using` disposal (`docs/validation.md:68-101`). |
| Cancellation | one `CancellationToken` per overload, forwarded to enumerator and token-aware selector | No eager cancellation; no Task-selector overloads (`docs/validation.md:82-101`). |

### `SpanPipelineExtensions` (class, static; 16 members)

| Operation family | Signatures | Notes |
| --- | --- | --- |
| Projection to destination | `Span<TResult> SelectTo<TSource,TResult>(this ReadOnlySpan<TSource>, Span<TResult>, Func<TSource,TResult>)`; `Span`/`ReadOnlySpan`/`Memory`/`ReadOnlyMemory` variants | Requires destination capacity ≥ source length before delegates run; returns written prefix (`docs/data-pipelines.md:37-47`). |
| Stable filter to destination | `WhereTo` (same 4 carriers) | Stable order. |
| Fused filter-map to destination | `ChooseTo` (same 4 carriers) | Fused one pass. |
| In-place transform | `Span<T> SelectInPlace<T>(this Span<T>, Func<T,T>)`; `Memory<T>` variant | Mutates caller storage. |
| In-place compaction | `Span<T> WhereInPlace<T>(this Span<T>, Func<T,bool>)`; `Memory<T>` variant | Returns valid prefix. |

---

## F6 — Async, streaming, concurrency

Purpose: explicit BCL-first coordination over `IAsyncEnumerable<T>`, `Channel`, `ValueTask`,
`CancellationToken`, and cold `Effect<Result<...>>`. Semantics: `docs/concurrency.md:3-8`;
boundaries `docs/concurrency.md:135-138`.

### `ParallelAsyncEnumerableExtensions` (class, static; 2 members)

| Operation family | Signatures | Notes |
| --- | --- | --- |
| Ordered bounded mapping | `IAsyncEnumerable<TResult> SelectParallelValueAsync<TSource,TResult>(this IAsyncEnumerable<TSource>, int maxConcurrency, Func<TSource,ValueTask<TResult>>)`; token-aware selector variant | Deferred; linked operation token; Channel backpressure; ordered delivery; drains on disposal (`docs/concurrency.md:10-53`). ValueTask-only selector. |

### `ParallelAsyncSequenceExtensions` (class, static; 6 members)

| Operation family | Signatures | Notes |
| --- | --- | --- |
| Parallel traversal | `ValueTask<Option<IReadOnlyList<TResult>>> TraverseParallelValueAsync<...>(..., int maxConcurrency, Func<TSource,ValueTask<Option<TResult>>>, CancellationToken)`; same for `Result` and `Validation`; token-aware selector variants | Eager; ≤ `maxConcurrency` active; ordered results; Option/Result fail-fast with sibling cancel+drain; Validation scans all (`docs/concurrency.md:55-90`). |

### `ConcurrentEffectExtensions` (class, static; 3 members)

| Operation family | Signatures | Notes |
| --- | --- | --- |
| First success | `ValueTask<Validation<TValue,TError>> FirstSuccessAsync<TValue,TError>(this IEnumerable<Effect<Result<TValue,TError>>>, CancellationToken = default)`; `(..., TimeSpan timeout, token)`; `(..., TimeSpan, TimeProvider, token)` | Starts every effect; input-order errors when all fail; winner cancels+drains losers; TimeProvider used only for the timeout (`ConcurrentEffectExtensions.cs:26-100`; `docs/concurrency.md:92-138`). Return type is `Validation` even for the success path. |

`TimeProvider` appears only in this type across `src/` (grep: `ConcurrentEffectExtensions.cs:57,84`).

---

## F7 — Effects, resources, environment

Purpose: thin deferred wrapper over standard delegates; explicit environment, cancellation, and
resource lifetime; no runtime. Semantics: `docs/effects.md:3-7`; boundaries `docs/effects.md:94-102`.

### `Effect` (class, static; 14 members)

| Operation family | Representative signatures | Notes |
| --- | --- | --- |
| Value / Result | `Effect<T> FromValue<T>(T value)`; `Effect<Result<TValue,TError>> FromResult<TValue,TError>(Result<TValue,TError>)` | Dump renders `FromValue<T>(T? value)`; source is non-annotated `T value` (`Effect.cs:16`). No documented null policy; see analysis memo. |
| Sync | `FromSync<T>(Func<T>)`; `FromSync<T>(Func<CancellationToken,T>)` | Null delegate rejected at factory (`Effect.cs:37-54`). |
| Task | `FromTask<T>(Func<Task<T>>)`; `FromTask<T>(Func<CancellationToken,Task<T>>)` | (`Effect.cs:63-80`). |
| ValueTask | `FromValueTask<T>(Func<ValueTask<T>>)`; token-aware variant | (`Effect.cs:89-106`). |
| Environment variants | `FromSync/FromTask/FromValueTask<TEnvironment,T>(Func<TEnvironment,...>)` | Four carrier shapes × env (`Effect.cs:116-194`). |

### `Effect<T>` (readonly struct; 6 members)

| Operation family | Representative signatures | Notes |
| --- | --- | --- |
| Run | `ValueTask<T> RunAsync(CancellationToken = default)` | Default effect returns a faulted `ValueTask` with `InvalidOperationException` (`Effect.cs:263-266`; test `tests/FunnySharp.Tests/EffectTests.cs:471-478`). |
| Compose | `Effect<TResult> Map<TResult>(Func<T,TResult>)`; `Bind<TResult>(Func<T,Effect<TResult>>)`; `Select`; `SelectMany` | Deferred; later stage skipped after throw/fault/cancel (`docs/effects.md:36-38`). |
| Environment lift | `Effect<TEnvironment,T> WithEnvironment<TEnvironment>()` | Ignores environment, forwards token (`Effect.cs:301-305`). |

### `Effect<TEnvironment,T>` (readonly struct; 6 members)

| Operation family | Representative signatures | Notes |
| --- | --- | --- |
| Run | `ValueTask<T> RunAsync(TEnvironment environment, CancellationToken = default)` | Default effect fails explicitly (`Effect.cs:389-392`). |
| Provide | `Effect<T> Provide(TEnvironment environment)` | Environment is an ordinary caller-owned value (`docs/effects.md:40-45`). |
| Compose | `Map`; `Bind`; `Select`; `SelectMany` | (`Effect.cs:412-467`). |

### `EffectResourceExtensions` (class, static; 4 members)

| Operation family | Signatures | Notes |
| --- | --- | --- |
| Sync disposal | `Effect<TResult> Using<TResource,TResult>(this Effect<TResource>, Func<TResource,Effect<TResult>>) where TResource : IDisposable`; environment variant | Acquire once per run; release exactly once after success/failure/exception/cancellation; null resource fails with `InvalidOperationException` (`docs/effects.md:75-92`). |
| Async disposal | `UsingAsync<...> where TResource : IAsyncDisposable`; environment variant | Same lifecycle; natural `await using` precedence (release failure wins). |

---

## F8 — State transitions and machines

Purpose: pure state changes with emitted commands, explicit status, composition, and replay.
Semantics: `docs/state-machines.md:3-6`; boundaries `docs/state-machines.md:117-123`.

### `TransitionStatus` (enum; 4 values) — `Undefined = 0`, `Applied = 1`, `Rejected = 2`, `Failed = 3` (`StateMachine.cs:8-29`).

### `StateChange<TState,TOutput>` (sealed class : `IEquatable<...>`; 7 members)

| Operation family | Representative signatures | Notes |
| --- | --- | --- |
| Factory | `static StateChange<TState,TOutput> To(TState state, params TOutput[] outputs)` | Clones the outputs array → snapshot (`StateTransition.cs:36-40`). |
| Data | `TState State { get; }`; `IReadOnlyList<TOutput> Outputs { get; }` | No command execution; shallow snapshot (`docs/state-machines.md:10-14`). |
| Equality / text | `Equals`; `GetHashCode`; `ToString()` | Structural, ordered outputs. |

### `StateTransition<TState,TOutput>` (sealed delegate) — `delegate StateChange<TState,TOutput> Invoke(TState state)` (`StateTransition.cs:113`).

### `StateTransitionExtensions` (class, static; 1 member)

| Operation family | Signature | Notes |
| --- | --- | --- |
| Composition | `StateTransition<TState,TOutput> Then<TState,TOutput>(this StateTransition<TState,TOutput>, StateTransition<TState,TOutput>)` | Threads state, concatenates outputs, rejects null changes with `InvalidOperationException` (`StateTransition.cs:130-165`). Each composition allocates a combined output array (`StateTransition.cs:151-163`); left-associated chains are O(n²) (`docs/state-machines.md:103-115`). |

### `TransitionResult<TState,TOutput,TError>` (readonly struct : `IEquatable<...>`; 18 members)

| Operation family | Representative signatures | Notes |
| --- | --- | --- |
| Factory | `Applied(StateChange<TState,TOutput>)`; `Rejected(TError)`; `Failed(TError)`; `Undefined()` | `default` equals `Undefined()` (`StateMachine.cs:112`; test `tests/FunnySharp.Tests/StateMachineTests.cs:6-43`). |
| Status | `TransitionStatus Status { get; }`; `IsApplied`/`IsRejected`/`IsFailed`/`IsUndefined` | Exactly one status (`docs/state-machines.md:38-46`). |
| Inspection | `TryGetChange([NotNullWhen(true)] out StateChange<...>?)`; `TryGetError([MaybeNull] out TError)`; `Match` with 4 branches | Exhaustive; undefined carries no payload (`StateMachine.cs:119-170`). |
| Equality / text | `Equals`; `GetHashCode`; `==`/`!=`; `ToString()` | Status + active payload. |

### `StateMachine<TState,TEvent,TOutput,TError>` (sealed delegate) — `delegate TransitionResult<TState,TOutput,TError> Invoke(TState state, TEvent @event)` (`StateMachine.cs:232-234`).

### `StateMachineExtensions` (class, static; 2 members)

| Operation family | Signatures | Notes |
| --- | --- | --- |
| Fallback | `StateMachine<...> OrElse<...>(this StateMachine<...>, StateMachine<...>)` | Fallback only after `Undefined` (`StateMachine.cs:252-264`). |
| Replay | `TransitionResult<...> Replay<...>(this StateMachine<...>, TState initialState, IEnumerable<TEvent> events)` | One pass; stops at first non-applied; materializes outputs once; empty history succeeds (`StateMachine.cs:278-307`; `docs/state-machines.md:66-77`). |

Reflection-generated delegate members (`BeginInvoke`, `EndInvoke`, constructors) appear in the dump
for all three delegates; they are compiler plumbing, not designed API.

---

## F9 — Optics and immutability

Purpose: opt-in composable updates over caller-provided getters/setters; no optics hierarchy, no
reflection, no copying layer. Semantics: `docs/immutable-updates.md:3-13`; law obligations
`docs/immutable-updates.md:54-64`.

### `Lens` (class, static; 2 members) — `Lens<TSource,TFocus> Create<...>(Func<TSource,TFocus> get, Func<TSource,TFocus,TSource> set)`; `Lens<T,T> Identity<T>()` (`Optics.cs:17-32`).

### `Lens<TSource,TFocus>` (readonly struct; 5 members)

| Operation family | Representative signatures | Notes |
| --- | --- | --- |
| Read | `TFocus Get(TSource source)` | Uninitialized struct throws `InvalidOperationException` (`Optics.cs:57-61`, `Optics.cs:140-146`). |
| Write | `TSource Set(TSource source, TFocus focus)`; `TSource Update(TSource source, Func<TFocus,TFocus> update)` | Caller setter defines copying. |
| Compose | `Lens<TSource,TNext> Compose<TNext>(Lens<TFocus,TNext>)`; `Optional<TSource,TNext> Compose<TNext>(Optional<TFocus,TNext>)` | Left-to-right (`docs/immutable-updates.md:36-49`). |

### `Optional` (class, static; 1 member) — `Optional<TSource,TFocus> Create<...>(Func<TSource,Option<TFocus>> getOption, Func<TSource,TFocus,TSource> set)` (`Optics.cs:163-171`).

### `Optional<TSource,TFocus>` (readonly struct; 5 members)

| Operation family | Representative signatures | Notes |
| --- | --- | --- |
| Read | `Option<TFocus> GetOption(TSource source)` | Absent focus is `None`. |
| Write | `TSource Set(TSource source, TFocus focus)`; `TSource Update(TSource source, Func<TFocus,TFocus>)` | Absent focus returns exact original source and skips setter/updater (`Optics.cs:210-247`). |
| Compose | `Compose(Lens)`; `Compose(Optional)` | Left-to-right; any absent stage preserves source identity. |

Law obligations (get-put, put-get, put-put) are caller-owned and not enforced
(`docs/immutable-updates.md:56-64`). Default-initialized optics throw on every operation
(`docs/immutable-updates.md:155-164`; test `tests/FunnySharp.Tests/OpticsTests.cs:198-213`).

---

## F10 — HTTP integration (FunnySharp.AspNetCore)

Purpose: optional Minimal API mapping from core outcomes/effects to `IResult` and
`ProblemDetails`; no DI, middleware, or global error policy. Semantics:
`docs/aspnet-core.md:3-13`; effects/cancellation `docs/aspnet-core.md:71-88`; no numeric claim
`docs/aspnet-core.md:116-121`.

### `HttpResultExtensions` (class, static; 15 members)

| Operation family | Representative signatures | Notes |
| --- | --- | --- |
| Sync mapping | `IResult ToHttpResult<T>(this Option<T>, Func<ProblemDetails> none, Func<T,IResult>? some = null)`; `Result` overload with `Func<TError,ProblemDetails> failure`; `Validation` overload with `Func<IReadOnlyList<TError>,HttpValidationProblemDetails> invalid` | Required problem mapper validated; success mapper optional → `Results.Ok(value)`; problem without `Status` throws `InvalidOperationException` (`HttpResultExtensions.cs:20-63`, `HttpResultExtensions.cs:381-409`). |
| Task mapping | `Task<IResult> ToHttpResultAsync<T>(this Task<Option<T>>, ...)`; Result and Validation overloads | Faults/cancellation transparent (`docs/aspnet-core.md:29-31`). |
| ValueTask mapping | `ValueTask<IResult> ToHttpResultAsync<T>(this ValueTask<Option<T>>, ...)`; Result and Validation overloads | `docs/aspnet-core.md:29-31`. |
| Effect mapping | `ValueTask<IResult> ToHttpResultAsync<T>(this Effect<Option<T>>, HttpContext, Func<ProblemDetails> none, ...)`; Result and Validation overloads | Runs with exactly `context.RequestAborted` (`HttpResultExtensions.cs:183-234`). |
| Environment effect mapping | same for `Effect<TEnvironment, ...>` with explicit `TEnvironment` | No service location; caller supplies environment (`HttpResultExtensions.cs:247-305`). |
| OpenAPI / typed results | none | Caller owns `IResult` shapes and metadata; goal-22 gap. |

---

## F11 — Cross-cutting facts

- Vocabulary: `Async` / `ValueAsync` families are used as described above; `MapAsync`/`BindAsync`
  take Task callbacks while `TraverseAsync` takes a sync selector on an async source — a naming
  inconsistency (see analysis memo §2.2).
- Default/uninitialized states: `Option<T>` = `None` (valid); `Result<,>` = failure with
  `default(TError)`; `Validation<,>` = `Invalid([default(TError)])`; `TransitionResult<...>` =
  `Undefined`; `Effect<T>` / `Effect<,>` / optics throw `InvalidOperationException` when run/used
  uninitialized.
- Nullability: XML docs and `[DisallowNull]`/`[MaybeNull]`/`[NotNullWhen]`/`[return: MaybeNull]`/
  `[return: NotNull]` annotations exist on Option/Result/Validation/TransitionResult members. The
  runtime-mode dump decodes nullability for parameters and return types: `?` is appended when the
  decoded state is `Nullable` (`NotNull`/`Unknown` add nothing) and not appended when the type is
  already `System.Nullable<T>`, which renders as `T?`. Properties additionally note non-`NotNull`
  states as `(nullability: ...)` (the single property flagged Nullable is
  `StateChange<TState,TOutput>.State`, dump line 284). Unconstrained type parameters decode as
  `Nullable`, so they render as `T?` in parameter and return position (e.g. `Success(TValue? value)`,
  `TSource? Set(TSource? source, TFocus? focus)`), which is a dump decoding artifact, not a
  source-level annotation; a genuine return annotation such as `[return: MaybeNull]` on
  `Option<T>.GetValueOrDefault()` is now visible as `T?` too.
- XML documentation: 275 core members and 16 AspNetCore members documented; no member lacks
  `summary` or `inheritdoc`; `GenerateDocumentationFile` + `TreatWarningsAsErrors` enforce coverage
  (`Directory.Build.props:1-9`, `src/FunnySharp/FunnySharp.csproj:20`).
- Packaging/dependency: core has no `PackageReference`; AspNetCore depends on core +
  `Microsoft.AspNetCore.App` only (`docs/product-contract.md §"Package And Dependency Boundary"`; test
  `tests/FunnySharp.Tests/PackageBoundaryTests.cs:8-19`).
- Analyzers / experimental markers / serialization converters: none exist
  (grep for `Experimental`, `PublicAPI`, converter types in `src/` → no matches).
- Tests: 305 core + 18 AspNetCore passing at the pinned build (local run, 2026-09-17).
