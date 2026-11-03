# Batched Reads

`FunnySharp.BatchSource<TKey, TValue, TError>`, `BatchRequirement<TValue, TError>`, and
`BatchScope` let business code declare typed reads, group independent keys, and dispatch each
source's batch operation once per execution. A requirement is a description; the scope owns
admission, dispatch, caching, cancellation, and disposal. The compiling example is
[examples/FunnySharp.Examples/BatchExamples.cs](../examples/FunnySharp.Examples/BatchExamples.cs);
the deterministic consumer comparison and measured costs are in
[batch-evidence.md](batch-evidence.md).

This is not a GraphQL feature and does not require a DI container, serializer, or framework. It
also does not make an ordinary HTTP or database call batch-capable: the source construction is the
explicit claim that the supplied delegate accepts a bounded list of keys. The request-buffer and
dispatch model is adapted from the DataLoader family; it is not a wrapper around GreenDonut, and
the comparison evidence records the differences.

## API Shape

```csharp
using FunnySharp;

var customers = new BatchSource<string, Customer, string>(
    "customers",
    "tenant-a/user-7",
    StringComparer.Ordinal,
    (keys, cancellationToken) => FetchCustomersAsync(keys, cancellationToken));

var preferences = new BatchSource<string, Preference, string>(
    "preferences",
    "tenant-a/user-7",
    StringComparer.Ordinal,
    FetchPreferencesAsync);

var regions = new BatchSource<string, Region, string>(
    "regions",
    "tenant-a/user-7",
    StringComparer.Ordinal,
    FetchRegionsAsync);

var plan = BatchRequirement.All(orders.Select(order =>
    BatchRequirement
        .Read(customers, order.CustomerKey)
        .Zip(BatchRequirement.Read(preferences, order.CustomerKey))
        .Bind(pair => BatchRequirement
            .Read(regions, pair.First.RegionKey)
            .Map(region => new EnrichedOrder(order, pair.First, pair.Second, region)))));

await using var scope = BatchScope.Create(new BatchScopeOptions
{
    MaxBatchSize = 128,
    MaxConcurrentBatches = 1,
    MaxPendingRequests = 1024,
    MaxCachedEntries = 1024,
});

var execution = await scope.ExecuteAsync(plan, cancellationToken);
if (!execution.TryGetValue(out var outcomes))
{
    // A top-level requirement can fail; All itself preserves inner outcomes.
    throw new InvalidOperationException(execution.ToString());
}

foreach (var outcome in outcomes!)
{
    if (outcome.TryGetValue(out var enriched))
    {
        Console.WriteLine(enriched!.Customer.Name);
    }
    else
    {
        outcome.TryGetError(out var error);
        Console.WriteLine(error.Kind);
    }
}
```

`FetchCustomersAsync` and the other delegates return
`ValueTask<Result<IReadOnlyList<BatchSourceEntry<TKey, TValue, TError>>, TError>>`. They receive
keys in first-request order, each key at most once per batch, and never more than
`MaxBatchSize`. The returned entries are keyed and may be in any order; they are not associated by
position. See [Source Contract](#source-contract).

## Requirements

| Member | Meaning |
| --- | --- |
| `BatchRequirement.FromValue(value)` | A successful value with no source read. |
| `BatchRequirement.Read(source, key)` | One keyed read from an explicit source. |
| `BatchRequirement.Failure(error)` | A typed failure with no source read. |
| `BatchRequirement.All(requirements)` | Starts every requirement and returns one `Result` per input in input order. Independent failures do not short-circuit siblings. |
| `BatchRequirement.Zip(first, second)` / `first.Zip(second)` | Starts both operands and combines successful values; returns the first domain failure in operand order. A source exception is never hidden by a sibling's domain failure. |
| `requirement.Map(selector)` | Transforms a successful value; skipped after failure. |
| `requirement.Bind(binder)` | Starts dependent reads only after the preceding value succeeds. A failure skips the binder and its reads. |

`All` snapshots its input sequence once. Empty input succeeds with an empty outcome list. The
outer result of `All` succeeds even when individual outcomes fail; this is intentional and
preserves the association between an order and its own failure. A plan can be reused across
executions and scopes.

A read is a typed requirement, not a started `Task`. That distinction is what allows independent
reads reached before the first dispatch to share a batch and allows the scope to bound retained
work. `Bind`, `Map`, and `Zip` are ordinary synchronous delegates; the only asynchronous boundary
is the source batch delegate and `BatchScope.ExecuteAsync`.

## Source Contract

`BatchSource<TKey, TValue, TError>` is constructed with:

- `name` — a diagnostic source name;
- `partition` — the explicit tenant, authorization, or parameter partition the source serves;
- `keyComparer` — the equality policy for grouping, response matching, and cache entries;
- `fetchAsync` — the source-supplied batch operation.

Creating the source is the only place the API assumes batching. There is no automatic HTTP,
database, or GraphQL loader discovery.

The delegate's response is a `Result`:

| Response | Outcome |
| --- | --- |
| Success with one `Found(key, value)` per requested key | Each read succeeds. |
| Success with a requested key omitted | `BatchReadErrorKind.Missing` for that key. Omission is not an error and is not unexpected. |
| Success with `Failed(key, error)` | `BatchReadErrorKind.KeyFailure` for that key; other keys are unaffected. |
| Failure with a typed error | `BatchReadErrorKind.BatchFailure` for every key in that batch; other batches are unaffected. |
| Success with a null collection, more entries than requested, an uninitialized entry, a null key, an unknown key, or a repeated key | `BatchReadErrorKind.UnexpectedResponse` for every key in that batch. The response is rejected as a whole rather than partially trusted. |

`BatchSourceEntry.Found` rejects a runtime-null value and `Found`/`Failed` reject a null key. Use
`Option<T>` or a nullable domain model when absence is a value. The source instance is the cache
identity; two sources with the same name and partition but different instances remain distinct.
Create each source within the tenant, authorization, and parameter context it serves. Do not share
one source instance across those boundaries.

`MapError` adapts a source's per-key and whole-batch typed errors to another error type. It never
runs for missing or unexpected responses, and the adapted source has its own cache identity.

## Grouping, Ordering, And Association

Within one execution, the scope groups unresolved reads by source and key using the source's
comparer. Each distinct key is passed to the source once, in the order the plan first reached it.
`All` retains the input order of its outcomes, `Zip` preserves operand order, and `Map`/`Bind`
preserve their branch. The source's response order has no effect on association.

For an order list with repeated customer keys, the scope makes one customer call, one preference
call, and one dependent region call (when those stages are reachable) rather than one call per
order. If a customer or preference is missing or failed, `Bind` skips the dependent region read for
that order and the corresponding outcome remains attached to that order.

## Failures, Exceptions, And Cancellation

`BatchReadError<TError>` separates four data outcomes:

| Kind | Source error | Meaning |
| --- | --- | --- |
| `Missing` | none | The source omitted the requested key. |
| `KeyFailure` | `TError` | The source returned a typed failure for this key. |
| `BatchFailure` | `TError` | The source returned a typed failure for the whole batch; every key in that batch receives it. |
| `UnexpectedResponse` | none | The response violated the source contract; `Detail` explains the rejection. |

Exceptions are not converted into `BatchReadError<TError>`. A source delegate exception, a
`Map`/`Bind` selector exception, or a comparer exception faults the execution. If independent
branches were started, the execution awaits and observes them before propagating; `All` reports
the first exception in branch order, and `Zip` reports an exception from either operand before a
sibling domain failure. A later exception from a branch that already faulted is observed but is
not surfaced separately. A faulted execution stops dispatching, cancels queued reads that were
never dispatched, and drains started source batches instead of cancelling them.

Cancellation is also not a data failure:

- An already-canceled caller token prevents source invocation.
- The scope links the caller token with its disposal token and forwards the linked token to every
  started source batch.
- Cancellation stops new dispatch, cancels queued reads, waits for started source batches, and
  then reports `OperationCanceledException`. The caller token is retained when the caller caused
  cancellation; scope disposal uses the link's token.
- A source delegate that ignores its token is not forcibly terminated. The execution remains
  incomplete until that delegate returns or throws, even after cancellation. This is the honest
  limit of arbitrary `Task` and `ValueTask` work.

`BatchScope.DisposeAsync` is idempotent. It stops new work, cancels queued reads, waits for started
work, and clears the cache. It does not claim to terminate arbitrary Tasks.

## Bounds And Admission

`BatchScopeOptions` bounds one execution:

| Option | Default | Effect |
| --- | ---: | --- |
| `MaxBatchSize` | 128 | Maximum keys in one source call. Larger request queues are split in first-request order. |
| `MaxConcurrentBatches` | 1 | Maximum source batch calls running at once across all sources. |
| `MaxPendingRequests` | 1024 | Maximum distinct queued or in-flight keys retained by one execution. Admission beyond this faults the execution with `InvalidOperationException`; excess work is never dispatched, and already-started source batches are drained rather than cancelled. |
| `CacheResults` | true | Retains resolved key results for the scope lifetime, including missing and failed outcomes. |
| `MaxCachedEntries` | 1024 | Per-source FIFO bound on retained cache entries. The oldest entry for that source is evicted first. |

These limits apply to execution coordination. A plan description itself is caller-owned and is
not bounded by the scope; `All` snapshots its input once at construction. Because each distinct
source instance owns its own cache table, a scope retains up to `MaxCachedEntries` per source,
not one shared total.

## Cache Lifetime And Invalidation

The cache is owned by `BatchScope`; there is no ambient, static, or cross-request cache. A scope
should represent one request or unit of work. Separate scopes never share requests, results, or
source batches, even for the same source instance.

Cached entries include successes, `Missing`, `KeyFailure`, `BatchFailure`, and
`UnexpectedResponse`. Exceptions and cancellation are not cached. Results are evicted in insertion
order per source when `MaxCachedEntries` is reached.

Mutation is not detected automatically. After changing data that a scope has already read, call:

- `Invalidate(source, key)` to remove one cached key;
- `Invalidate(source)` to remove every cached key for one source;
- `ClearCache()` to remove every cached result in the scope.

Invalidation is rejected while an execution is active, because the execution's in-flight requests
already belong to a consistent snapshot. Dispose the scope or finish the execution first. A new
scope is the normal isolation boundary between requests or tenants.

## Performance

The evidence record compares the executable FunnySharp consumer, an equivalent hand-batched BCL
implementation, and GreenDonut. On a 200-order/64-customer workload the FunnySharp consumer made
three source calls instead of 600 per-item calls. The plan and coordinator allocate more than the
hand-batched code and more than GreenDonut's loader-only path; the complete measured trade-off and
the consumer line counts are in [batch-evidence.md](batch-evidence.md). Allocation is reported
there because it is deterministic; timing on the shared host is directional.

The scope is deliberately serial by default (`MaxConcurrentBatches = 1`). Raising the bound allows
independent source batches to overlap but also changes source concurrency; it does not guarantee a
latency improvement on a shared or rate-limited data source.

## Deliberate Boundaries

- No DI registration, GraphQL dependency, serializer, reflection binder, or automatic source
  discovery.
- No write coalescing, mutation batching, transaction, retry, backoff, or implicit re-execution.
  An invalidation makes a later read eligible; it does not start one.
- No ambient cache, cache expiration policy, or cross-scope sharing. Cache lifetime is the scope,
  and mutation invalidation is explicit.
- No forced termination of arbitrary `Task`/`ValueTask` work. Cancellation and disposal are
  cooperative and drain started source calls.
- No automatic per-key cancellation, response ordering, source inference, or batch-capability
  assumption. The source is the explicit batching contract.
- No universal query engine or second scheduler runtime. The coordinator is limited to the request
  buffer, bounded batch dispatch, and the typed scope cache documented here.
