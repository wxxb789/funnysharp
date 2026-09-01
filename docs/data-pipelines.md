# Data Pipelines

FunnySharp keeps data pipelines on BCL carriers instead of introducing a collection hierarchy.
Use the standard .NET 10 `Select`, `Where`, `SelectMany`, `Take`, `Skip`, ordering, and explicit
materialization operations. `Pipe` connects whole-value stages. FunnySharp adds only the fused
filter-map operation `Choose` and caller-buffered span/memory operations that LINQ cannot express
without changing lifetime or allocation behavior.

## Streaming Sequences

`IEnumerable<T>.Choose` applies a function returning `Option<TResult>`. `Some` values flow through
and `None` values are skipped. The operator is deferred, preserves source order, and enumerates the
source once per consumer enumeration. It does not cache results or materialize an intermediate
collection.

`.NET 10` includes `System.Linq.AsyncEnumerable`, so asynchronous pipelines use the same BCL
`Select`, `Where`, and `SelectMany` names. FunnySharp adds:

- `IAsyncEnumerable<T>.Choose` for a synchronous chooser.
- `IAsyncEnumerable<T>.ChooseValueAsync` for `ValueTask<Option<TResult>>` choosers, including a
  cancellation-aware overload.

These operators are pull-based and deferred. They request and process one source item at a time,
await each `ValueTask` once, preserve source order, and never introduce parallel execution or
prefetching. Enumeration cancellation comes from the consumer, such as `WithCancellation(token)`
or `ToListAsync(token)`. The exact token is forwarded to the source and cancellation-aware chooser;
the operator does not inspect it eagerly. Source, chooser, cancellation, and disposal failures flow
through normal `await foreach` behavior without wrapping.

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
that cost. `Choose`, `WhereTo`, `ChooseTo`, and `WhereInPlace` retain the relative order of emitted
items.

## Performance Evidence

`DataPipelineBenchmarks` compares:

- `IEnumerable<T>.Choose` with idiomatic LINQ `Where` plus `Select`.
- `ReadOnlySpan<T>.ChooseTo` with an equivalent indexed loop writing to caller storage.
- `IAsyncEnumerable<T>.Choose` with both a direct `await foreach` loop and .NET 10 async LINQ.

Run the focused benchmark with:

```shell
dotnet run --project benchmarks/FunnySharp.Benchmarks/FunnySharp.Benchmarks.csproj --configuration Release -- --filter '*DataPipelineBenchmarks*'
```

ShortRun results are directional and should be rerun on deployment hardware before capacity
decisions.

The following ShortRun was recorded on September 1, 2026 with BenchmarkDotNet 0.15.8,
.NET 10.0.11, and an AMD EPYC 7763 Hyper-V virtual machine:

| Carrier and count | Direct/BCL baseline | FunnySharp | Ratio | Baseline allocation | FunnySharp allocation |
| --- | ---: | ---: | ---: | ---: | ---: |
| `IEnumerable`, 16 | 145.83 ns | 122.58 ns | 0.84x | 160 B | 112 B |
| `IEnumerable`, 1024 | 5.95 us | 4.64 us | 0.78x | 160 B | 112 B |
| `Span`, 16 | 13.59 ns | 50.61 ns | 3.72x | 0 B | 0 B |
| `Span`, 1024 | 892.17 ns | 3.18 us | 3.57x | 0 B | 0 B |
| Async stream, 16 | 303.37 ns direct / 762.03 ns async LINQ | 728.20 ns | 2.40x direct | 0 B direct / 400 B async LINQ | 312 B |
| Async stream, 1024 | 14.71 us direct / 39.41 us async LINQ | 44.17 us | 3.00x direct | 0 B direct / 400 B async LINQ | 312 B |

`IEnumerable<T>.Choose` removes one iterator layer compared with `Where` plus `Select` in this
scenario. The span helper stays allocation-free and uses caller-owned storage, but standard
delegate calls and `Option<T>` inspection are measurably slower than an inlined handwritten loop.
The async helper uses 88 B less than the equivalent two-operator async LINQ pipeline; its timing was
slightly better for 16 items and slightly worse for 1024 items in this short virtualized run. Use a
direct loop when that measured overhead matters more than the reusable pipeline shape.
