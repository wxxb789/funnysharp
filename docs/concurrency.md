# Concurrency

`FunnySharp` adds a small set of explicit concurrency coordinators over standard .NET
carriers. The compiling workflow is in
[examples/FunnySharp.Examples/Program.cs](../examples/FunnySharp.Examples/Program.cs).
These APIs use `IAsyncEnumerable<T>`, `ValueTask`, `CancellationToken`, `Channel`,
`TimeProvider`, and cold `Effect<T>` values; they do not introduce a scheduler, runtime, or
alternative `Task` type.

## Bounded Parallel Mapping

`SelectParallelValueAsync` maps an `IAsyncEnumerable<TSource>` with a
`ValueTask<TResult>` selector and returns a deferred `IAsyncEnumerable<TResult>`:

<!-- documentation-sample: DocumentationSamples.Concurrency.SelectParallel -->
```csharp
var quotedOrders = orders.SelectParallelValueAsync(
    maxConcurrency: 4,
    (order, cancellationToken) =>
        new ValueTask<ShippingQuote>(GetShippingQuoteAsync(order, cancellationToken)));

await foreach (var quote in quotedOrders.WithCancellation(cancellationToken))
{
    Process(quote);
}
```

Construction does not enumerate the source or invoke the selector. Each enumeration creates
one linked operation token, which is supplied both to the source enumerator and to the
token-aware selector. Results are yielded in source order even when later selectors complete
first.

`maxConcurrency` is the maximum number of started but not yet delivered selectors. Internally,
a bounded `Channel` and an equally bounded admission window apply backpressure: the producer
does not read and start an unbounded source prefix, and it cannot open the next slot until the
consumer has observed a result. This is a streaming operation, not an eager materialization.

Dispose an enumeration when stopping early. `await foreach` does this automatically for
`break`, exceptions, and cancellation. Disposal cancels the linked operation, waits for the
producer and all started selectors to finish, observes their faults, and disposes the source
enumerator before it completes. Source and selector faults remain ordinary .NET exceptions;
the first observed fault stops admission and becomes the primary failure even when an earlier
source item is still pending. Cleanup faults are retained in an `AggregateException` after that
primary failure rather than silently abandoned. Consumer cancellation is rethrown with the
consumer's token after clean cleanup; cleanup faults are aggregated with it. A selector
`ValueTask` is converted once and never consumed twice.

Cancellation and disposal are cooperative. They wait for source and selector operations that
ignore the linked token, because returning while those operations still run would orphan work.

The API intentionally accepts `ValueTask` selectors only. An `async` lambda can target the
delegate directly. A named `Task`-returning method must be wrapped explicitly, as in the
example above, so the carrier conversion remains visible.

## Completion-Order Parallel Mapping

`SelectParallelCompletionOrderValueAsync` maps an `IAsyncEnumerable<TSource>` with a
`ValueTask<TResult>` selector and returns a deferred `IAsyncEnumerable<TResult>` that yields
each result as its selector completes:

<!-- documentation-sample: DocumentationSamples.Concurrency.SelectParallelCompletionOrder -->
```csharp
var quotedOrders = orders.SelectParallelCompletionOrderValueAsync(
    maxConcurrency: 4,
    (order, cancellationToken) =>
        new ValueTask<ShippingQuote>(GetShippingQuoteAsync(order, cancellationToken)));

await foreach (var quote in quotedOrders.WithCancellation(cancellationToken))
{
    Process(quote);
}
```

Construction does not enumerate the source or invoke the selector. Each enumeration creates
one linked operation token, which is supplied both to the source enumerator and to the
token-aware selector. Results are delivered in completion order: whichever selector completes
first is delivered first, so a slow early source item never withholds a completed later item.
Selectors that complete concurrently are delivered in whichever completion is observed first;
that relative order is not specified. Choose `SelectParallelValueAsync` when the output order
must match the source order.

`maxConcurrency` is the maximum number of started but not yet delivered selectors. The same
bounded `Channel` and admission window as the ordered map apply backpressure: the producer does
not read and start an unbounded source prefix, and it cannot open the next slot until the
consumer has observed a result. The channel is sized so a completed result never blocks behind
the consumer. This is a streaming operation, not an eager materialization.

Disposal, failures, and cancellation follow the ordered map's contract. Disposal cancels the
linked operation, waits for the producer and all started selectors to finish, observes their
faults, and disposes the source enumerator before it completes. The first observed source or
selector failure stops admission and becomes the primary failure; faults raised during cleanup
are retained in an `AggregateException` after that primary failure. Consumer cancellation is
rethrown with the consumer's token after clean cleanup. A selector `ValueTask` is converted
once and never consumed twice, and a named `Task`-returning selector must be wrapped
explicitly with `new ValueTask<T>(task)`.

The ordering choice is part of the method name, never a boolean or enum parameter: the
ordered and completion-order maps are two operations with one contract each, so the call site
always reveals the delivery order.

## Parallel Traverse

`TraverseParallelValueAsync` eagerly materializes a bounded parallel traversal of an
`IAsyncEnumerable<TSource>`. It has overloads for `Option<T>`, `Result<TValue, TError>`, and
`Validation<TValue, TError>`, with token-aware selector overloads for each:

<!-- documentation-sample: DocumentationSamples.Concurrency.TraverseParallel -->
```csharp
var checkedOrders = await orders.TraverseParallelValueAsync(
    maxConcurrency: 4,
    (order, cancellationToken) => ValidateOrderAsync(order, cancellationToken),
    cancellationToken);
```

All selectors receive one linked operation token, while the outer operation preserves caller
cancellation. At most `maxConcurrency` selectors are active. Successful values are always
materialized in source order.

- `Option` and `Result` are fail-fast. A `None` or failure stops further admission, cancels
  started siblings, drains them, disposes the source, and then returns the source-earliest
  normally completed terminal result. A sibling that honors internal cancellation contributes no
  terminal value.
- `Validation` starts and observes every source item when enumeration completes normally, then
  accumulates every typed error in source order. Errors within each invalid validation retain
  their own order.

Normal source, selector, and disposal exceptions do not become `Option`, `Result`, or
`Validation` values. They are observed after started work has drained and propagate with normal
.NET exception behavior. Caller cancellation is preserved with the caller token after clean
cleanup. If cleanup also faults, the caller cancellation and cleanup failures are retained in an
`AggregateException`. As with `SelectParallelValueAsync`, a named Task-returning selector must be
explicitly adapted with `new ValueTask<T>(task)`.

The pre-existing `SequenceAsync`, `TraverseAsync`, and `TraverseValueAsync` operations remain
sequential. Choose a parallel method only when concurrent source work is intentional and its
bounded, cancellation, and ordering behavior is appropriate for the workflow.

## First Successful Effect

`FirstSuccessAsync` coordinates a non-empty `IEnumerable<Effect<Result<TValue, TError>>>`.
Each effect must be cold: it starts only when `RunAsync` is called and creates fresh work for
that run. The coordinator snapshots the input once, starts every effect, and returns the first
successful `Result` observed in completion order. When successes are already observable in the
same observation turn, input order breaks the tie.

<!-- documentation-sample: DocumentationSamples.Concurrency.FirstSuccess -->
```csharp
var providers = new[]
{
    Effect.FromTask<Result<ShippingQuote, QuoteError>>(
        cancellationToken => GetQuoteFromCarrierAsync("north", cancellationToken)),
    Effect.FromTask<Result<ShippingQuote, QuoteError>>(
        cancellationToken => GetQuoteFromCarrierAsync("south", cancellationToken)),
};

var firstQuote = await providers.FirstSuccessAsync(
    TimeSpan.FromSeconds(2),
    TimeProvider.System,
    cancellationToken);
```

A successful result cancels and drains its remaining started effects before returning. If every
effect returns a typed `Result` failure, the method returns an invalid
`Validation<TValue, TError>` whose errors are in input order. Ordinary exceptions are not
converted into typed failures: when no success wins, a single fault is rethrown by identity and
multiple faults use `AggregateException` in input order. When there are faults and canceled source
operations, faults take precedence; when every non-typed outcome is cancellation, the first source
cancellation is rethrown with its token. A winning success still drains and observes losing faults
so that no started work is left unobserved. A failure raised by cancellation callbacks is a cleanup
failure and propagates instead of being discarded.

The timeout overload takes `TimeProvider` so callers can test time deterministically. Timeout is
cooperative: it cancels the internal operation token and waits for started work to drain before
throwing `TimeoutException`; it cannot forcibly stop non-cooperating work. Caller cancellation
has precedence over both timeout and a concurrently observed success. The coordinator freezes the
winner/timeout/failure candidate, drains started work, then checks caller cancellation once at the
shared publication point. Cancellation that occurs during cleanup therefore throws
`OperationCanceledException` with the caller token; cleanup failures remain secondary through the
documented aggregate contract.

`FirstSuccessAsync` accepts cold `Effect<Result<...>>` values rather than started `Task`s.
Wrap a Task-returning method with `Effect.FromTask`, or a ValueTask-returning method with
`Effect.FromValueTask`. FunnySharp deliberately provides no naked started-Task race API, custom
runtime, or scheduler.

## Performance Evidence

The concurrency benchmark compares:

- Ordered bounded mapping with `Parallel.ForEachAsync` writing to a known-length array.
- Completion-order bounded mapping with `Parallel.ForEachAsync` placing results into
  known-length completion-order slots. The BCL has no bounded completion-order streaming
  primitive: `Task.WhenEach` enumerates already-started tasks and cannot bound fan-out over a
  lazy asynchronous source.
- Parallel `Option` and `Validation` traversal with `Parallel.ForEachAsync` followed by explicit
  source-ordered sequencing.
- First-success coordination with a direct `Task.WhenAny` loop that cancels and drains the same
  cold operation delegates.

Mapping and traversal use `maxConcurrency: 4`. Every selector and first-success candidate crosses
one real asynchronous continuation through `Task.Yield`; the benchmark therefore measures
coordination around asynchronous work rather than only completed-`ValueTask` dispatch. The mapping
baseline has the advantage of knowing the output length, while the FunnySharp path remains a
general streaming operator followed by `ToArrayAsync`.

Run the focused benchmark with:

```shell
dotnet run --project benchmarks/FunnySharp.Benchmarks/FunnySharp.Benchmarks.csproj --configuration Release -- --filter '*ConcurrencyBenchmarks*'
```

The exact table below is generated from the approved observation in
`eng/performance/baseline.json`. Hosted timing is directional; allocation ceilings are the blocking
contract.

<!-- performance-table:start concurrency -->
| Scenario | Baseline mean | FunnySharp mean | Ratio | Baseline allocation | FunnySharp allocation |
| --- | ---: | ---: | ---: | ---: | ---: |
| Completion-order bounded asynchronous map ([Count=1024]) | 506.493 us | 1,062.209 us | 2.10x | 312747 B | 471291 B |
| Completion-order bounded asynchronous map ([Count=16]) | 14.103 us | 26.218 us | 1.86x | 5345 B | 10066 B |
| Ordered bounded asynchronous map ([Count=1024]) | 479.380 us | 941.191 us | 1.96x | 303959 B | 525730 B |
| Ordered bounded asynchronous map ([Count=16]) | 13.614 us | 26.220 us | 1.93x | 5350 B | 11473 B |
| First successful cold Result operation ([CandidateCount=16]) | 7.559 us | 6.026 us | 0.80x | 5655 B | 4044 B |
| First successful cold Result operation ([CandidateCount=4]) | 5.414 us | 6.860 us | 1.27x | 1738 B | 2738 B |
| Parallel Option traversal ([Count=1024]) | 489.797 us | 669.685 us | 1.37x | 321553 B | 266853 B |
| Parallel Option traversal ([Count=16]) | 14.279 us | 18.778 us | 1.32x | 5961 B | 7617 B |
| Parallel Validation accumulation ([Count=1024]) | 523.963 us | 713.088 us | 1.36x | 359790 B | 308321 B |
| Parallel Validation accumulation ([Count=16]) | 16.780 us | 19.719 us | 1.18x | 6532 B | 7190 B |

Excluded measurements:
- Result parallel traversal: The prior supplemental comparison used different input carriers and is not reproducible from tracked sources.
- Unmeasured failure paths: Failure-path concurrency is covered by deterministic tests rather than timing claims.
<!-- performance-table:end concurrency -->

The two streaming maps pay for their reusable enumerator, channel backpressure, bounded
admission, and cleanup tracking; in the recorded observation both are slower and allocate more
than the known-length BCL array paths at both sizes. The completion-order map allocates less
than the ordered map at both sizes — a completed result is delivered through its observer without
suspending the consumer on a pending work task — while its recorded mean is slower than the
ordered map at both sizes in this observation. The traversal and first-success timing directions
are scheduler-sensitive and have split by input across observations: in this one the Validation
and Option coordinators are slower at both sizes, and first-success is faster with sixteen
candidates and slower with four while allocating less with sixteen and more with four. Treat
these directions as unstable at this precision; the generated table above owns the exact ratios
and allocation figures for the recorded environment.

These measurements are directional. `Task.Yield` models scheduler handoff, not production I/O, and
three measured iterations on a virtualized host produce wide confidence intervals for the smallest
cases. Rerun on representative hardware and workloads before making latency or capacity decisions.
