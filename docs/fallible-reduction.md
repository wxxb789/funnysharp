# Fallible Reduction

`FunnySharp` lets a consumer reduce an ordinary or asynchronous sequence through a fallible
accumulator and stop at the first typed failure without materializing a result sequence or
managing an enumerator. Compiling examples are in
[FallibleReductionExamples.cs](../examples/FunnySharp.Examples/FallibleReductionExamples.cs); the
reference comparison, consumption ledger, and measured costs are in
[fallible-reduction-evidence.md](fallible-reduction-evidence.md).

## API Shape

The operation stays on BCL sequence types and the existing `Result<TAccumulate, TError>` carrier.

| Member | Source | Step | Result |
| --- | --- | --- | --- |
| `AggregateResult(seed, accumulate)` | `IEnumerable<TSource>` | `Func<TAccumulate, TSource, Result<TAccumulate, TError>>` | `Result<TAccumulate, TError>` |
| `AggregateResultAsync(seed, accumulate, cancellationToken = default)` | `IAsyncEnumerable<TSource>` | `Func<TAccumulate, TSource, Result<TAccumulate, TError>>` | `ValueTask<Result<TAccumulate, TError>>` |
| `AggregateResultValueAsync(seed, accumulate, cancellationToken = default)` | `IAsyncEnumerable<TSource>` | `Func<TAccumulate, TSource, ValueTask<Result<TAccumulate, TError>>>` | `ValueTask<Result<TAccumulate, TError>>` |
| `AggregateResultValueAsync(seed, accumulate, cancellationToken)` | `IAsyncEnumerable<TSource>` | `Func<TAccumulate, TSource, CancellationToken, ValueTask<Result<TAccumulate, TError>>>` | `ValueTask<Result<TAccumulate, TError>>` |

The synchronous form is eager. The asynchronous forms are eager `ValueTask` operations, not
deferred sequences: they process one item at a time, never prefetch, and never run items in
parallel. `AggregateResult` is not an alias for `Enumerable.Aggregate`; the `Result` suffix names
the fallible output, and the operation stops consuming the source at a typed failure.

## Fold Semantics

- **Empty input.** The operation returns `Result<TAccumulate, TError>.Success(seed)` and invokes
  the step zero times. The exact seed value is returned, including a null reference or a mutable
  object.
- **Success.** The source is enumerated once, in source order. The step runs once per item with
  the accumulator produced by the previous step; the final step's successful accumulator is
  returned.
- **First typed failure.** The failed step result is returned unchanged. The operation does not
  call `MoveNext` or the step again, so later source items are not read. This is the difference
  from `source.Aggregate(Result.Success(seed), (accumulator, item) => accumulator.Bind(...))`:
  `Bind` skips the step after failure, but `Aggregate` keeps requesting items to the end of the
  source.
- **Source faults.** Exceptions from `GetEnumerator`, `MoveNext`, or `Current` propagate unchanged.
  An acquired enumerator is disposed by `foreach`.
- **Step faults.** An exception from the step propagates unchanged, the enumerator is disposed, and
  no later item is requested.
- **Uninitialized step results.** A step that returns `default(Result<TAccumulate, TError>)` is a
  programming error. Inspecting it throws `InvalidOperationException` with
  `"The result has not been initialized."`; it is not converted to a typed failure. The enumerator
  is still disposed.
- **Disposal.** The enumerator is disposed exactly once after success, failure, or an exception.
  Normal C# precedence applies: an exception thrown by `Dispose` (or `DisposeAsync`) can replace a
  pending failure or step/source exception.
- **Null payloads.** A null seed, a null successful accumulator, and a null typed error are valid
  payloads. The operation does not replace them with defaults or failures.
- **Single pass and repeated calls.** The source is not cached. Each call enumerates again, and a
  deferred or single-pass source is evaluated on every call. There is no result-sequence
  materialization, intermediate list, or internal buffer.
- **Mutable accumulators.** The accumulator is passed by value. Reference-type and mutable
  accumulators are not cloned, snapshotted, isolated, or rolled back. Mutations performed before a
  failure remain visible to later steps and to a caller that holds the seed. External work already
  performed is not undone. A caller that needs isolation or rollback must copy or own the state
  itself.

Argument validation is eager: a null source or step delegate throws `ArgumentNullException` before
any enumeration. The seed is deliberately not validated.

## Choosing The Operation

| Need | Operation |
| --- | --- |
| Fold through a typed failure and stop reading the source | `AggregateResult` / async-source forms |
| Fold without a typed failure | BCL `Aggregate`, or `NonEmpty<T>.Aggregate` for a proven non-empty source |
| Collect every successful value, failing fast | `Sequence` / `Traverse` |
| Collect every independent error | `Validation.Sequence` / `Validation.Traverse` |
| Observe a running aggregate after each item | `Scan` / `ScanValueAsync` |
| Reuse several summaries in one pass | [`Summary`](one-pass-summaries.md) composition with `Zip`/`Evaluate`/`EvaluateAsync` |
| Require exactly one keyed match per input row | [`JoinExactlyOne`](cardinality-joins.md) |

`AggregateResult` is fail-fast only. It has no accumulating mode: a fold is sequential and
dependent, and independent error accumulation is already expressed by `Validation` traversal or
`CheckAll`. It also has no `UnitResult` or `Option` step overload; a same-accumulator check can
return `Result.Success(accumulator)` explicitly.

## Asynchronous Path

The asynchronous source forms have the same empty, success, first-failure, disposal, exception, and
mutation semantics as the synchronous form.

- `AggregateResultAsync` takes a synchronous step. `AggregateResultValueAsync` takes a
  `ValueTask` step and observes each returned value task exactly once.
- The tokenless `ValueTask` overload forwards the caller token to the enumerator only; the
  cancellation-aware overload also passes the exact token to every reached step.
- The token is not inspected eagerly. A source or step that ignores it can complete normally even
  when the token is already canceled.
- Cancellation and faults keep ordinary asynchronous behavior. A canceled source or step, including
  an `OperationCanceledException` escaping a faulted step awaitable, leaves the returned operation
  canceled; a non-cancellation fault leaves it faulted. Neither becomes a typed domain failure.
- A step exception thrown synchronously inside the asynchronous operation is delivered through the
  returned operation rather than thrown directly to the caller. The synchronous `AggregateResult`
  throws such an exception directly.
- `await foreach` disposes the enumerator with `DisposeAsync` on every exit. A disposal exception
  can replace a pending outcome.
- The operation begins enumerating up to the first incomplete await when called, like any `async`
  method; it does not wait until the caller awaits to run synchronously completed work.
- There is no `Task`-step overload. An `async` lambda targets the `ValueTask` overload directly.

## Costs

The fold keeps one accumulator and one enumerator; it does not buffer items or accumulator history.
Value-type accumulators are copied into and out of each step by the delegate signature. A mutable
accumulator avoids those copies but owns its storage. The adapter-based asynchronous overloads
(synchronous step and tokenless `ValueTask` step) each add one closure per call; the token-aware
`ValueTask` overload forwards the step directly. Async timing is directional; allocation is the
blocking comparison.

The measured allocation, copying, buffering, and directional timing tables are in
[fallible-reduction-evidence.md](fallible-reduction-evidence.md).

## Deliberate Boundaries

The operation adds no stream, sequence, or enumerator hierarchy and no async wrapper. It adds no
carrier: the result remains the existing `Result<TAccumulate, TError>`, and exceptions are never
converted into domain errors. It performs no automatic parallel reduction, because arbitrary
callbacks do not establish associativity, commutativity, purity, or safe partitioning; the
explicit parallel operators remain the separate choice for bounded concurrency. It also provides
no rollback, transaction, compensation, retry, caching, memoization, buffering, or exactly-once
claim for external work. There is no accumulating `Validation` mode, no `Task`-step overload tower,
and no generic early-exit protocol beyond the typed `Result` failure. Accumulator isolation and
mutation policy remain with the caller, and the core package gains no runtime dependency.
