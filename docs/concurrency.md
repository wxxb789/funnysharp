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

## Bounded Per-Key Processing

`SelectParallelByKeyCompletionOrderValueAsync` runs one asynchronous operation per source item
with two explicit bounds: `maxConcurrency` started operations, and `maxBufferedItems` source items
read but not yet started. Operations for different keys run concurrently; operations for the same
key run one at a time, in source order. The callback returns the canonical
`Result<TResult, TError>`, so each successful value or typed failure is delivered as an item rather
than disappearing inside background work.

<!-- documentation-sample: DocumentationSamples.Concurrency.SelectParallelByKey -->
```csharp
var processed = accountEvents.SelectParallelByKeyCompletionOrderValueAsync(
    accountEvent => accountEvent.AccountId,
    maxConcurrency: 4,
    maxBufferedItems: 8,
    (accountEvent, cancellationToken) =>
        ApplyAccountEventAsync(accountEvent, cancellationToken));

await foreach (var (accountId, accountEvent, outcome) in processed.WithCancellation(cancellationToken))
{
    Observe(accountId, accountEvent.Sequence, outcome);
}
```

Construction validates arguments and captures delegates but does not enumerate the source, invoke
the key selector, or invoke the operation. Each enumeration creates one linked operation token,
which is forwarded to the source enumerator and the token-aware operation overload. The tokenless
overload's callback does not receive that token. The element is a tuple of the key
observed for that exact source item, the source item, and the returned `Result`.

The operation delegate returns `ValueTask<Result<TResult, TError>>`. An `async` lambda can target it
directly; a named `Task`-returning method must be wrapped explicitly with
`new ValueTask<Result<TResult, TError>>(task)` so the carrier conversion stays visible.

`maxConcurrency` counts started but not yet delivered operations. A completed operation keeps its
slot until the enumerator reads its outcome, so a slow consumer applies backpressure instead of
allowing completed results to accumulate without bound. `maxBufferedItems` counts source items
read but not yet started; it must be at least one because an item's key is known only after the
item has been read. The producer acquires one buffer permit before each source move and releases
it when the item starts. The coordinator therefore retains at most
`maxConcurrency + maxBufferedItems` source items. A measurement that counts source items before the
consumer's delivery counter advances can observe one extra item during that handoff; the
coordinator bound itself is exact. Key state exists only while a key owns an active operation or a
buffered item, and is removed when both are gone. A large or unbounded key cardinality cannot
create an unbounded per-key queue or dictionary; a hot key can consume the declared buffer, which
then stops source reading.

The key selector is invoked once per read item. Equal keys are determined by the supplied
`IEqualityComparer<TKey>`, or by `EqualityComparer<TKey>.Default` when it is omitted. A null key is a
key-selector programming error: it stops admission, cancels and drains started work, disposes the
source, and throws `ArgumentNullException` naming the key selector. A null source, key selector, or
operation is rejected at construction.

Results are delivered in operation-completion order. Within one key, results are always in source
order because the next operation for that key is admitted only after its predecessor has completed
and its outcome has been read by the enumerator. The successor can start during `MoveNextAsync`,
before the `await foreach` body runs for the predecessor's outcome; it never starts before the
predecessor's callback completed. Across keys, concurrently completing operations have no specified
relative order; global source order is not promised. A typed `Failure` is a completed
outcome and does not stop the key or the stream; the next item for that key may start after the
failure completes and is read. An uninitialized `Result` is a programming fault, not a domain
failure.

Ready keys start in FIFO order of becoming ready, and a completing key re-joins the tail. This is
fair progress among admitted work, not a strict round-robin or latency guarantee. With
`maxConcurrency: 1`, one hot key can occupy the only slot until its buffered queue is drained. A
hot key can also fill the pending buffer and delay reading a later key in the source even when an
active slot is free. Both are partition-induced blocking created by the declared bounds and the
input order; they are not hidden behind a larger implicit buffer.

Dispose an enumeration when stopping early. Cleanup stops admission, drops buffered items that
never started, cancels the linked token, waits for every started operation and its observer,
observes faults, disposes the source enumerator once, and only then completes. A callback that
ignores the token is awaited; it cannot be forcibly terminated. A source, key-selector, or callback
exception is terminal: outcomes already buffered in the delivery channel are yielded before the
terminal failure, pending items are dropped, and the first fault is propagated after started work
drains. A queue of independent callbacks is not a scheduler, retry policy, process supervisor, or
exactly-once delivery mechanism.

## First Successful Effect

`FirstSuccessAsync` coordinates a non-empty `IEnumerable<Effect<Result<TValue, TError>>>`.
Each effect must be cold: it starts only when `RunAsync` is called and creates fresh work for
that run. The finite input is eagerly snapshotted once, requiring O(N) input storage. Existing
overloads admit at most 32 candidates at a time; a slot stays occupied until that candidate's
completion has been accounted for. The coordinator selects the lowest-input-index success within
the observed ready batch, stops new admission, cancels remaining admitted work, and drains it.
The default 32 is a compatibility policy, not an I/O throughput recommendation. For an explicit
bound, use the required-argument overload
`effects.FirstSuccessAsync(timeout, timeProvider, cancellationToken, maxConcurrency)`; pass
`Timeout.InfiniteTimeSpan` and `TimeProvider.System` when no timeout is needed. These arguments
preserve old signatures and default-literal/method-group call shapes. Opaque dependencies between
candidates must not assume unlimited co-starting. Infinite input enumeration is not supported.

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
converted into typed failures. Independent faults observed before winner selection and during
loser cleanup still propagate even when another candidate succeeded; a single fault is rethrown
by identity and multiple faults are aggregated in input order. All exceptions represented by a
source Task are retained. The first independent source cancellation retains its token and is not
hidden by a winner; cancellation artifacts are suppressed only for canceled tasks carrying the
canceled internal operation token. A faulted OCE is still an independent fault. Cancellation-callback
failures follow candidate failures in the aggregate. A winner is returned as `Valid` only after
all admitted candidates, completion observers, and cancellation callbacks finish without an
independent failure that must propagate.

Coordination processes each admitted candidate once through an indexed completion notification;
there is no repeated whole-input scan, pending-list compaction, or rebuilt `WhenAny` wait set.
Total coordination work and input/outcome storage are O(N), while queued unobserved candidate
notifications are bounded by the chosen degree (plus one cancellation wake-up). A numeric
benchmark exclusion does not exempt admission, cleanup, or these complexity contracts.

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
| Scenario | Baseline mean | Candidate mean | Ratio | Baseline allocation | Candidate allocation |
| --- | ---: | ---: | ---: | ---: | ---: |
| Completion-order bounded asynchronous map ([Count=1024]) | 900.251 us | 1,145.159 us | 1.27x | 281400 B | 372440 B |
| Completion-order bounded asynchronous map ([Count=16]) | 24.837 us | 45.788 us | 1.84x | 5284 B | 10002 B |
| Ordered bounded asynchronous map ([Count=1024]) | 856.182 us | 1,287.783 us | 1.50x | 293953 B | 373364 B |
| Ordered bounded asynchronous map ([Count=16]) | 26.225 us | 53.802 us | 2.05x | 5436 B | 9325 B |
| First successful cold Result operation ([CandidateCount=16]) | 10.748 us | 16.662 us | 1.55x | 5268 B | 6819 B |
| First successful cold Result operation ([CandidateCount=4]) | 3.237 us | 5.718 us | 1.77x | 1466 B | 3105 B |
| Parallel Option traversal ([Count=1024]) | 886.577 us | 691.661 us | 0.78x | 337894 B | 165540 B |
| Parallel Option traversal ([Count=16]) | 24.174 us | 15.459 us | 0.64x | 5971 B | 3515 B |
| Parallel Validation accumulation ([Count=1024]) | 917.989 us | 701.823 us | 0.76x | 356185 B | 205074 B |
| Parallel Validation accumulation ([Count=16]) | 27.844 us | 17.085 us | 0.61x | 6290 B | 4161 B |

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

## Deliberate Boundaries

The concurrency surface is explicit and BCL-first. It does not add a scheduler, fiber runtime,
actor system, process supervisor, distributed coordinator, durable queue, exactly-once delivery
guarantee, implicit retry, backoff, circuit breaker, or second execution runtime. Per-key ordering
is local to one enumeration; it is not a global source-order guarantee across keys, and completion
order between concurrent keys is unspecified. A hot key, a full buffer, a slow consumer, and
source order can delay work for other keys; the declared bounds are the only admission promise.
Cancellation and disposal are cooperative and wait for started operations, including a
non-cooperative callback. A typed `Result.Failure` is a delivered outcome, not a stream failure;
unexpected exceptions and source faults remain terminal. The operation does not persist, deduplicate,
or retry worked items, and it does not provide a replayable event log. Callers that need those
properties own them outside this coordinator.
