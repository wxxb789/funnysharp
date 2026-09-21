# Data Pipelines

FunnySharp keeps data pipelines on BCL carriers instead of introducing a collection hierarchy.
Use the standard .NET 10 `Select`, `Where`, `SelectMany`, `Take`, `Skip`, ordering, and explicit
materialization operations. `Pipe` connects whole-value stages. FunnySharp adds only the fused
filter-map operation `Choose`, the running-aggregate operator `Scan`, and caller-buffered
span/memory operations that LINQ cannot express without changing lifetime or allocation behavior.
Cardinality access, the non-empty guarantee, partitioning, exact combination, container/parse
bridges, and traversal context live in [collections](collections.md).

## Streaming Sequences

`IEnumerable<T>.Choose` applies a function returning `Option<TResult>`. `Some` values flow through
and `None` values are skipped. The operator is deferred, preserves source order, and enumerates the
source once per consumer enumeration. It does not cache results or materialize an intermediate
collection.

`IEnumerable<T>.Scan(seed, accumulate)` produces the running aggregate. It yields the accumulator
after each element, never yields the seed, and an empty source yields nothing; for a non-empty
source, the last yielded value equals `source.Aggregate(seed, accumulate)`. The operator is
deferred, preserves source order, and enumerates the source once per consumer enumeration without
caching or materializing. An `accumulate` exception propagates unchanged and stops the sequence at
that element.

`.NET 10` includes `System.Linq.AsyncEnumerable`, so asynchronous pipelines use the same BCL
`Select`, `Where`, and `SelectMany` names. FunnySharp adds:

- `IAsyncEnumerable<T>.Choose` for a synchronous chooser.
- `IAsyncEnumerable<T>.ChooseValueAsync` for `ValueTask<Option<TResult>>` choosers, including a
  cancellation-aware overload.
- `IAsyncEnumerable<T>.Scan` for a synchronous accumulator, with the same running-aggregate
  contract as the synchronous form.
- `IAsyncEnumerable<T>.ScanValueAsync` for `ValueTask` accumulators, including a
  cancellation-aware overload.

These operators are pull-based and deferred. They request and process one source item at a time,
await each `ValueTask` once, preserve source order, and never introduce parallel execution or
prefetching. Enumeration cancellation comes from the consumer, such as `WithCancellation(token)`
or `ToListAsync(token)`. The exact token is forwarded to the source and to a cancellation-aware
chooser or accumulator; the operator does not inspect it eagerly. Source, delegate, cancellation,
and disposal failures flow through normal `await foreach` behavior without wrapping.

## Span And Memory

Span and memory operations execute immediately. They do not return deferred sequences, retain
additional internal references, own the underlying storage, or cross an `await` boundary. Returned
span and memory views still keep or borrow their caller-owned backing storage according to normal
BCL lifetime rules.

- `SelectTo` projects every item into caller-provided destination storage.
- `WhereTo` stably copies matching items into caller-provided destination storage.
- `ChooseTo` fuses filtering and projection through `Option<TResult>` in one pass.
- `SelectInPlace` transforms a writable span or memory region without another buffer.
- `WhereInPlace` stably compacts a writable span or memory region and returns its valid prefix.

The `*To` operations require destination capacity at least equal to the source length so validation
happens before any delegate runs or write occurs. They return a view over the written destination prefix;
the unused tail is unchanged. Source and destination must not overlap. Use the explicit `*InPlace`
operations when one writable region should be reused. If a delegate throws, the original exception
is preserved and an already-written prefix is not rolled back.

`ReadOnlySpan<T>` and `Span<T>` views remain subject to normal stack and scope lifetime rules.
`ReadOnlyMemory<T>` and `Memory<T>` overloads forward synchronously to their spans and return memory
slices over the caller-owned backing storage. Filtering cannot generally produce a zero-copy
contiguous view, so it either writes to a destination or compacts in place rather than pretending
that a lazy span exists.

## Materialization And Ordering

Materialization is always explicit through BCL operations such as `ToArray`, `ToList`,
`ToArrayAsync`, or `ToListAsync`. Re-enumerating a deferred pipeline repeats the work. Operations
such as ordering and grouping may buffer by their BCL contract; FunnySharp does not hide or cache
that cost. `Choose`, `Scan`, `WhereTo`, `ChooseTo`, and `WhereInPlace` retain the relative order of
emitted items.

## Performance Evidence

`DataPipelineBenchmarks` compares:

- `IEnumerable<T>.Choose` with idiomatic LINQ `Where` plus `Select`.
- `IEnumerable<T>.Scan` with an equivalent hand-written running-aggregate iterator.
- `ReadOnlySpan<T>.ChooseTo` with an equivalent indexed loop writing to caller storage.
- `IAsyncEnumerable<T>.Choose` with both a direct `await foreach` loop and .NET 10 async LINQ.
- `IAsyncEnumerable<T>.Scan` with an equivalent hand-written asynchronous running-aggregate
  iterator.

Run the focused benchmark with:

```shell
dotnet run --project benchmarks/FunnySharp.Benchmarks/FunnySharp.Benchmarks.csproj --configuration Release -- --filter '*DataPipelineBenchmarks*'
```

ShortRun results are directional and should be rerun on deployment hardware before capacity
decisions.

The exact table below is generated from the approved observation in
`eng/performance/baseline.json`. Hosted timing is directional; allocation ceilings are the blocking
contract.

<!-- performance-table:start data-pipelines -->
| Scenario | Baseline mean | FunnySharp mean | Ratio | Baseline allocation | FunnySharp allocation |
| --- | ---: | ---: | ---: | ---: | ---: |
| Async stream filter-map ([Count=1024]) - BclAsyncWhereSelect | 15.643 us | 45.939 us | 2.94x | 0 B | 400 B |
| Async stream filter-map ([Count=1024]) - FunnySharpAsyncChoose | 15.643 us | 42.180 us | 2.70x | 0 B | 312 B |
| Async stream filter-map ([Count=16]) - BclAsyncWhereSelect | 323.193 ns | 901.055 ns | 2.79x | 0 B | 400 B |
| Async stream filter-map ([Count=16]) - FunnySharpAsyncChoose | 323.193 ns | 764.644 ns | 2.37x | 0 B | 312 B |
| Async stream running aggregate ([Count=1024]) | 40.681 us | 43.086 us | 1.06x | 184 B | 312 B |
| Async stream running aggregate ([Count=16]) | 774.260 ns | 848.335 ns | 1.10x | 184 B | 312 B |
| IEnumerable filter-map ([Count=1024]) | 6.084 us | 4.741 us | 0.78x | 160 B | 112 B |
| IEnumerable filter-map ([Count=16]) | 154.441 ns | 126.759 ns | 0.82x | 160 B | 112 B |
| IEnumerable running aggregate ([Count=1024]) | 4.733 us | 4.575 us | 0.97x | 120 B | 120 B |
| IEnumerable running aggregate ([Count=16]) | 114.130 ns | 118.393 ns | 1.04x | 120 B | 120 B |
| Span filter-map ([Count=1024]) | 800.990 ns | 3.146 us | 3.93x | 0 B | 0 B |
| Span filter-map ([Count=16]) | 15.326 ns | 55.389 ns | 3.61x | 0 B | 0 B |

Excluded measurements:
- Unmeasured pipeline variants: ChooseValueAsync, SelectTo, WhereTo, in-place variants, Memory wrappers, and ScanValueAsync have no numeric release claim.
<!-- performance-table:end data-pipelines -->

The generated comparisons keep carriers, parameters, and allocation visible. Use a direct loop
when measured overhead matters more than the reusable pipeline shape.
