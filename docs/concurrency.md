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
cooperative: it cancels the linked operation token and waits for started work to drain before
throwing `TimeoutException`; it cannot forcibly stop non-cooperating work. Caller cancellation
has precedence over both timeout and a concurrently observed success, and throws
`OperationCanceledException` with the caller token after cleanup.

`FirstSuccessAsync` accepts cold `Effect<Result<...>>` values rather than started `Task`s.
Wrap a Task-returning method with `Effect.FromTask`, or a ValueTask-returning method with
`Effect.FromValueTask`. FunnySharp deliberately provides no naked started-Task race API, custom
runtime, or scheduler.

## Performance Evidence

The concurrency benchmark compares:

- Ordered bounded mapping with `Parallel.ForEachAsync` writing to a known-length array.
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

The following `ShortRun` was recorded on September 1, 2026 with BenchmarkDotNet 0.15.8,
.NET SDK 10.0.400, .NET 10.0.11, and an AMD EPYC 7763 Hyper-V virtual machine. The job used one
launch, three warmups, and three measured iterations:

| Scenario | Size | BCL/direct mean | FunnySharp mean | Ratio | BCL/direct allocation | FunnySharp allocation |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Ordered bounded map | 16 | 29.44 us | 58.05 us | 2.07x | 5.86 KB | 8.88 KB |
| Ordered bounded map | 1,024 | 860.53 us | 1.381 ms | 1.61x | 293.35 KB | 335.86 KB |
| Parallel Option traversal | 16 | 35.95 us | 33.10 us | 0.92x | 5.94 KB | 5.16 KB |
| Parallel Option traversal | 1,024 | 926.07 us | 705.33 us | 0.76x | 315.83 KB | 166.04 KB |
| Parallel Validation accumulation | 16 | 36.60 us | 36.94 us | 1.01x | 6.23 KB | 6.27 KB |
| Parallel Validation accumulation | 1,024 | 941.63 us | 750.73 us | 0.80x | 348.31 KB | 207.66 KB |
| First success | 4 candidates | 2.376 us | 3.529 us | 1.49x | 1.42 KB | 1.69 KB |
| First success | 16 candidates | 17.306 us | 19.349 us | 1.12x | 5.31 KB | 4.78 KB |

The ordered streaming map pays for its reusable enumerator, channel backpressure, ordered delivery,
and cleanup tracking; in this workload it was 1.6-2.1x slower and allocated 1.1-1.5x as much as the
known-length BCL array path. The traversal coordinator was competitive for 16 items and used less
time and storage than the direct intermediate-array baseline at 1,024 items. First-success added
12-49% time in these short cases, with allocation slightly higher for four candidates and lower for
sixteen.

These measurements are directional. `Task.Yield` models scheduler handoff, not production I/O, and
three measured iterations on a virtualized host produce wide confidence intervals for the smallest
cases. Rerun on representative hardware and workloads before making latency or capacity decisions.
