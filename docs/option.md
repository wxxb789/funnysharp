# Option

`FunnySharp` provides `Option<T>` for a value that is either present (`Some`) or absent (`None`).
It makes absence explicit while preserving standard C# delegates, dictionaries, `Task`, `ValueTask`, exceptions, and cancellation. The compiling examples are in [examples/FunnySharp.Examples/Program.cs](../examples/FunnySharp.Examples/Program.cs).

## API Shape

Create values with `Option.Some(value)`, `Option.None<T>()`, `Option<T>.Some(value)`, or `Option<T>.None`. `Option<T>` is a `readonly struct` with no public constructor, so `default(Option<T>)` is also `None`.

Use `IsSome`, `IsNone`, `TryGetValue`, or `Match` to inspect an option. There is intentionally no throwing `Value` property and no implicit conversion from `Option<T>` to `T`.

The synchronous composition surface is:

- `Map`, `Bind`, and `Filter` for conditional transformation.
- `Zip` to combine two present values into `(First, Second)`.
- `GetValueOr`, `GetValueOrElse`, and `GetValueOrDefault` for explicit value fallbacks.
- `OrElse` and `OrElseWith` for option fallbacks.

Common .NET absence forms have focused bridges:

- `Option.FromNullable(value)` and `value.ToOption()` convert nullable references and `Nullable<T>` values.
- `Option.FromTry(operation)` adapts a `bool`/`out` operation. Bind a Try API with input arguments through a lambda.
- `dictionary.GetOption(key)` adapts `IReadOnlyDictionary<TKey, TValue>.TryGetValue`.
- `Task<T?>.ToOptionAsync()` and `ValueTask<T?>.ToOptionAsync()` adapt nullable asynchronous completions.
- `MapAsync` / `BindAsync` use `Task` callbacks; `MapValueAsync` / `BindValueAsync` use `ValueTask` callbacks. Each also has a cancellation-aware overload.

Dedicated nullable conversions unwrap `Nullable<T>`: `Option.FromNullable((int?)0)` returns `Option<int>`. Generic transforms preserve the declared result type instead: `Map<int?>`, `FromTry<int?>`, and a dictionary whose `TValue` is `int?` return `Option<int?>`. In either form, a runtime-null nullable payload is `None`; a contained `0` is `Some`.

## Evaluation And Failure Semantics

`Some` rejects a runtime-null payload with `ArgumentNullException`. Nullable conversion APIs, dictionary hits, successful Try outputs, and `Map` results normalize runtime-null to `None`. A non-null `default(T)` remains present, so `Option.Some(0)`, `Option.Some(false)`, and `Option.Some(default(DateTime))` are `Some`.

An option is not flattened. `Option.Some(Option.None<int>())` is a present outer option containing an absent inner option, and it is distinct from `Option.None<Option<int>>()`.

`Map`, `Bind`, and `Filter` validate their delegates at entry, invoke a callback at most once for `Some`, and do not invoke it for `None`. `Map` normalizes a runtime-null result; `Bind` returns the callback's option unchanged. `Match` validates both branches and executes exactly one; because it leaves the abstraction, its selected branch may return runtime-null.

`GetValueOr` validates its eager fallback before examining the option and rejects a runtime-null fallback. `GetValueOrElse` validates its factory at entry, invokes it only for `None`, and rejects a runtime-null factory result. `GetValueOrDefault` is explicitly the fallback that can return `default(T)`, including runtime-null. `OrElseWith` validates the factory at entry and invokes it only for `None`.

`FromTry` invokes its operation once. A `false` result is `None` even when the `out` variable was assigned; a `true` result is then normalized for runtime-null. It never catches exceptions. `GetOption` validates only the dictionary receiver, calls `TryGetValue` once, passes the key unchanged, distinguishes a missing key from a present non-null default value, and likewise never catches a dictionary exception.

`None` equals `None`. Two `Some` values use `EqualityComparer<T>.Default`, as do `==` and `!=`. Equal options have equal hashes; the hash includes the presence case, but different options are not guaranteed unique hashes. `ToString()` produces diagnostic `None` or `Some(payload)` text and is not a serialization or display-format contract.

The async methods validate callback arguments synchronously. For `Some`, they invoke the selected callback, await it once with `ConfigureAwait(false)`, and apply the same runtime-null normalization as `Map`; `BindAsync` and `BindValueAsync` return the awaited callback option unchanged. For `None`, they return a completed `None` without invoking a callback or inspecting a supplied `CancellationToken`.

Faults and cancellation are not absence. `ToOptionAsync`, `MapAsync`, `BindAsync`, `MapValueAsync`, and `BindValueAsync` do not catch, wrap, or replace faults or cancellation. Await the returned operation to observe ordinary C# failure or cancellation semantics, including the original exception instance. Cancellation-aware callbacks receive the exact token supplied by the caller, but the bridge does not cancel eagerly.

`ValueTask` follows its normal single-consumption rule. The bridge awaits a source or callback-returned `ValueTask` exactly once; callers must likewise await the returned `ValueTask<Option<T>>` once rather than store and await it repeatedly. A default `ValueTask<T?>` completes with its default nullable result, which therefore normalizes to `None`.

## Deliberate Boundaries

`Option<T>` is a two-case absence value, not a general discriminated union or result type. It does not convert exceptions or cancellation into `None`, provide an async wrapper type, or add an implicit unwrap or throwing absent-value accessor.

The collection surface stops at `IReadOnlyDictionary<TKey, TValue>` lookup. Sequence traversal, accumulation, and async collection combinators are deferred. Result conversion, LINQ query aliases (`Select`, `SelectMany`, and `Where`), serialization converters, analyzers, source generators, and AOT- or trimming-specific guarantees are also outside this API.

## Performance Evidence

The benchmark project compares direct C# or BCL branches with the corresponding `Option<T>` operation. `TryOperation<T>` adapters and async selectors are cached outside the steady-state methods; every `ValueTask` source is created per invocation.

Run it with:

```shell
dotnet run --project benchmarks/FunnySharp.Benchmarks/FunnySharp.Benchmarks.csproj --configuration Release -- --filter '*OptionBenchmarks*'
```

The following `ShortRun` was recorded on August 31, 2026 with BenchmarkDotNet 0.15.8, .NET SDK 10.0.400, .NET 10.0.11, and an AMD EPYC 7763 2.44 GHz Hyper-V virtual machine. The job used one launch, three warmups, and three measured iterations. A dash in an allocation column denotes 0 B; `N/A` denotes that BenchmarkDotNet could not compute a meaningful ratio because a result was at or below timer resolution.

| Scenario | Direct mean | FunnySharp mean | Ratio | Direct allocation | FunnySharp allocation |
| --- | ---: | ---: | ---: | ---: | ---: |
| Completed `Task` mapping | 18.909 ns | 38.195 ns | 2.02x | 144 B | 216 B |
| Completed `ValueTask` mapping | 8.148 ns | 25.103 ns | 3.08x | 0 B | 0 B |
| Construction and inspection, `None` | 0.000 ns | 0.007 ns | N/A | 0 B | 0 B |
| Construction and inspection, `Some` | 0.000 ns | 0.033 ns | N/A | 0 B | 0 B |
| Construction and inspection, large readonly struct | 1.230 ns | 0.818 ns | 0.67x | 0 B | 0 B |
| Dictionary lookup, hit | 5.582 ns | 5.614 ns | 1.01x | 0 B | 0 B |
| Dictionary lookup, miss | 5.548 ns | 6.156 ns | 1.11x | 0 B | 0 B |
| `GetValueOr`, `None` | 0.020 ns | 0.019 ns | N/A | 0 B | 0 B |
| `GetValueOr`, `Some` | 0.018 ns | 0.079 ns | N/A | 0 B | 0 B |
| `Map`, `None` | 0.000 ns | 0.871 ns | N/A | 0 B | 0 B |
| `Map`, `Some` | 0.064 ns | 2.250 ns | N/A | 0 B | 0 B |
| Nullable conversion, `None` | 0.051 ns | 1.218 ns | N/A | 0 B | 0 B |
| Nullable conversion, `Some` | 0.396 ns | 0.000 ns | N/A | 0 B | 0 B |
| `TryParse`, hit | 11.447 ns | 12.877 ns | 1.13x | 0 B | 0 B |
| `TryParse`, miss | 6.605 ns | 8.457 ns | 1.28x | 0 B | 0 B |

The completed async cases expose measurable wrapper work: the `Task` path added 72 B per operation in this run, while the completed `ValueTask` path remained allocation-free. The `TryParse` adapter added roughly 13-28% without allocation, and dictionary lookup stayed close to the direct branch. The remaining synchronous results are largely sub-nanosecond and several were explicitly reported as `ZeroMeasurement`; neither the apparently favorable ratios nor ratios whose baseline is near timer resolution are meaningful enough for a performance claim. These results are directional only: they were collected on a virtualized host with three measured iterations, so rerun them on representative deployment hardware before making capacity or latency decisions.
