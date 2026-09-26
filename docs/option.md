# Option

`FunnySharp` provides `Option<T>` for a value that is either present (`Some`) or absent (`None`).
It makes absence explicit while preserving standard C# delegates, dictionaries, `Task`, `ValueTask`, exceptions, and cancellation. The compiling examples are in [examples/FunnySharp.Examples/Program.cs](../examples/FunnySharp.Examples/Program.cs).

## API Shape

Create values with `Option.Some(value)`, `Option.None<T>()`, `Option<T>.Some(value)`, or `Option<T>.None`. `Option<T>` is a `readonly struct` with no public constructor, so `default(Option<T>)` is also `None`.

Use `IsSome`, `IsNone`, `TryGetValue`, or `Match` to inspect an option. There is intentionally no throwing `Value` property and no implicit conversion from `Option<T>` to `T`.

The synchronous composition surface is:

- `Map`, `Bind`, and `Filter` for conditional transformation.
- `Zip` to combine already-created options: two present values into `(First, Second)`, or 2–4
  present values through a combine function.
- `GetValueOr`, `GetValueOrElse`, and `GetValueOrDefault` for explicit value fallbacks.
- `OrElse` and `OrElseWith` for option fallbacks.

Common .NET absence forms have focused bridges:

- `Option.FromNullable(value)` and `value.ToOption()` convert nullable references and `Nullable<T>` values.
- `Option.FromBoolean(condition, value)` and `Option.FromBoolean(condition, valueFactory)` adapt a boolean condition; the factory runs only when the condition is true, and a runtime-null result is `None`.
- `Option.FromTry(operation)` adapts a `bool`/`out` operation. Bind a Try API with input arguments through a lambda.
- `dictionary.GetOption(key)` adapts `IReadOnlyDictionary<TKey, TValue>.TryGetValue`.
- `option.ToNullable()` unwraps a value-type option to `Nullable<T>`; `None` becomes `null`.
- `Task<T?>.ToOptionAsync()` and `ValueTask<T?>.ToOptionAsync()` adapt nullable asynchronous completions.
- `MapAsync` / `BindAsync` use `Task` callbacks; `MapValueAsync` / `BindValueAsync` use `ValueTask` callbacks. Each also has a cancellation-aware overload.

Dedicated nullable conversions unwrap `Nullable<T>`: `Option.FromNullable((int?)0)` returns `Option<int>`. Generic transforms preserve the declared result type instead: `Map<int?>`, `FromTry<int?>`, and a dictionary whose `TValue` is `int?` return `Option<int?>`. In either form, a runtime-null nullable payload is `None`; a contained `0` is `Some`.

`Option<T>.ToNullable()` is the explicit value-type unwrap: `None` becomes `null`, `Some` returns the contained value, and it never throws for a present option. Reference types use `GetValueOrDefault()` (annotated `[MaybeNull]`) or `Match` instead, because C# cannot declare two extension overloads that differ only by a `struct` or `class` constraint. The `default(Option<T>)` value remains a valid `None` in every case; it is not an uninitialized state.

The two `FromBoolean` overloads differ by the second parameter type. A bare `null` literal as that argument is ambiguous between them; pass a typed variable or cast the value, as in `Option.FromBoolean(condition, (string?)null)`, or pass a typed `Func<T>` variable to select the factory overload.

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

The collection surface also includes the shared `IEnumerable<T>` and `IAsyncEnumerable<T>`
`Sequence` and `Traverse` operations. They preserve Option's fail-fast absence behavior; their
common ordering, materialization, fault, cancellation, and disposal rules are documented in
[Validation and traversal semantics](validation.md). `Option<T>.ToResult` and
`Result<TValue, TError>.ToOption` are the shipped explicit conversion boundary documented in
[Result semantics](result.md). `Select` and `SelectMany` are secondary LINQ aliases of `Map` and
`Bind` for query syntax; there is no `Where` because a bare predicate cannot produce absence.
Documentation presents the member-centric vocabulary first. Serialization converters, analyzers,
and source generators remain outside the Option API.
Package-wide trimming and Native AOT evidence and limits are recorded in
the [product contract](product-contract.md) and [release-readiness checklist](release-readiness.md).

## Performance Evidence

The benchmark project compares direct C# or BCL branches with the corresponding `Option<T>` operation. `TryOperation<T>` adapters and async selectors are cached outside the steady-state methods; every `ValueTask` source is created per invocation.

Run it with:

```shell
dotnet run --project benchmarks/FunnySharp.Benchmarks/FunnySharp.Benchmarks.csproj --configuration Release -- --filter '*OptionBenchmarks*'
```

The exact table below is generated from the approved observation in
`eng/performance/baseline.json`. Hosted timing is directional; allocation ceilings are the blocking
contract. `N/A` means timing was below resolution or unavailable.

<!-- performance-table:start option -->
| Scenario | Baseline mean | FunnySharp mean | Ratio | Baseline allocation | FunnySharp allocation |
| --- | ---: | ---: | ---: | ---: | ---: |
| Completed Task mapping | 24.044 ns | 47.388 ns | 1.97x | 144 B | 216 B |
| Completed ValueTask mapping | 9.761 ns | 26.583 ns | 2.72x | 0 B | 0 B |
| Dictionary lookup - hit | 6.034 ns | 10.187 ns | 1.69x | 0 B | 0 B |
| Dictionary lookup - miss | 6.049 ns | 6.972 ns | 1.15x | 0 B | 0 B |
| GetValueOr - None | N/A | 0.600 ns | N/A | 0 B | 0 B |
| GetValueOr - Some | N/A | 0.363 ns | N/A | 0 B | 0 B |
| Map - None | N/A | 1.221 ns | N/A | 0 B | 0 B |
| Map - Some | N/A | 2.532 ns | N/A | 0 B | 0 B |
| Nullable conversion - None | N/A | 1.048 ns | N/A | 0 B | 0 B |
| Nullable conversion - Some | N/A | N/A | N/A | 0 B | 0 B |
| Try pattern - hit | 11.367 ns | 13.549 ns | 1.19x | 0 B | 0 B |
| Try pattern - miss | 7.351 ns | 10.006 ns | 1.36x | 0 B | 0 B |

Excluded measurements:
- Construction and inspection - large readonly struct: Direct and Option paths do not perform equivalent construction work.
- Construction and inspection - None: Direct and Option paths do not perform equivalent construction work.
- Construction and inspection - Some: Direct and Option paths do not perform equivalent construction work.
- Unmeasured Option variants: Bind, Filter, lazy fallback, and combination variants have no numeric release claim.
<!-- performance-table:end option -->

The generated table exposes measured costs without making a package-wide superiority claim. Rerun
timing on representative deployment hardware before making capacity or latency decisions.
