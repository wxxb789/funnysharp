# Validation And Traversal

`FunnySharp` provides `Validation<TValue, TError>` for independent domain checks that should
report every discovered error. The compiling examples are in
[examples/FunnySharp.Examples/Program.cs](../examples/FunnySharp.Examples/Program.cs).

## API Shape

Create values with `Validation<TValue, TError>.Valid(value)`, `Invalid(error)`, or
`InvalidMany(errors)`. `Validation<TValue, TError>` is a `readonly struct` with no public constructor.
Its default value is `Invalid([default(TError)])`. That preserves a total two-case value type, but
it is not a domain-error shortcut: construct invalid values explicitly with a meaningful error.

Use `IsValid`, `IsInvalid`, `TryGetValue`, `TryGetErrors`, or `Match` to inspect a validation.
There is intentionally no throwing value or error accessor. Valid and invalid payloads preserve
their supplied values, including a runtime-null valid value or a null error.

The synchronous composition surface is:

- `Map` transforms a valid value and preserves errors.
- `MapErrors` transforms every error and preserves a valid value.
- `Zip` combines already-created validations into a named `(First, Second)` pair.
- `Apply` applies a validated function to a validated argument.

`Zip` and `Apply` are applicative: when both operands are invalid, they accumulate errors from the
left operand before errors from the right operand. This makes independent field validation
deterministic. `Map` and `MapErrors` validate their selector at entry, invoke it at most once for
the active case, and let selector exceptions escape unchanged.

There is deliberately no `Bind`, `SelectMany`, or `ZipWith`. A later dependent check cannot be
evaluated independently of an earlier valid value, so it cannot honestly participate in
independent error accumulation. Use ordinary C# control flow, or use fail-fast `Result` for that
workflow.

`InvalidMany(IEnumerable<TError>)` rejects a null or empty sequence, enumerates the source exactly
once, and takes an immutable snapshot of its contents. Later mutations of the input collection do
not alter the validation. `Invalid(TError)` represents exactly one error, including a null error.

Equality and hashing are structural and include the active case. Invalid errors are compared in
order. `ToString()` is diagnostic text such as `Valid(value)` or `Invalid([error])`, not a
serialization contract.

## Sequence And Traverse

The shared collection operations work over `IEnumerable<T>`:

- `Sequence` collects a sequence of `Option<T>`, `Result<TValue, TError>`, or
  `Validation<TValue, TError>`.
- `Traverse` applies an Option-, Result-, or Validation-producing selector while collecting its
  successful values.

Successful results contain a materialized `IReadOnlyList<T>` in source order. Empty inputs produce
an empty successful collection. A source is enumerated once, selectors are invoked once per
reached item in source order, and normal `foreach` disposal applies. The implementations are
iterative, so large sequences do not consume stack depth recursively.

`Option` and `Result` are fail-fast: traversal stops on the first `None` or failure, respectively.
`Validation` fully scans the reached source and accumulates every error in source order, retaining
each validation's own error order. This is the key behavioral distinction when selecting an
abstraction for batch work.

Traversal does not catch source or selector exceptions. They propagate with normal C# behavior,
and an enumerator is still disposed when one has been acquired. Traversal is eager and materializes
the returned collection, so it allocates for successful values; invalid Validation traversal also
allocates to collect errors. Capacity may be pre-sized when a synchronous source reports a count,
but this is not a streaming or zero-allocation API.

## Asynchronous Sequence And Traverse

`IAsyncEnumerable<T>` has matching eager operations:

- `SequenceAsync` collects asynchronous sequences of Option, Result, or Validation values.
- `TraverseAsync` accepts a synchronous Option-, Result-, or Validation-producing selector.
- `TraverseValueAsync` accepts a `ValueTask`-producing selector, with overloads that receive the
  caller's `CancellationToken`.

All asynchronous operations return `ValueTask<...>`, preserve source order, materialize an
`IReadOnlyList<T>` for successful results, and have the same empty-input, fail-fast, and Validation
accumulation behavior as their synchronous counterparts. They process one item at a time: there is
no parallelism, `Task.WhenAll`, scheduler, or custom collection hierarchy.

The supplied cancellation token is forwarded unchanged to `GetAsyncEnumerator` and, for a
token-aware selector, to every selector invocation. The library does not inspect it to cancel
eagerly. It awaits each source and selector `ValueTask` once; callers must also observe the normal
single-consumption rule for the returned `ValueTask`.

Non-cancellation source and selector faults propagate unchanged, and `await using` ensures
`DisposeAsync` runs after success, failure, cancellation, or a selector fault. The natural async `OperationCanceledException`
contract applies: an `OperationCanceledException` escaping a source or selector, including one from
a faulted awaitable, completes the returned operation as canceled with that exception's token.

There are intentionally no Task-selector overloads. An `async` lambda can target the `ValueTask`
selector overload directly. A named Task-returning method can be wrapped, for example:

```csharp
await source.TraverseValueAsync(item => new ValueTask<Option<string>>(LookupAsync(item)));
```

The wrapper makes the conversion explicit and preserves the method's normal fault and cancellation
behavior.

## Deliberate Boundaries

Validation does not add a general discriminated-union framework, an async wrapper type, exception
conversion, retry behavior, serialization support, analyzers, source generators, or AOT- and
trimming-specific guarantees. Its accumulation model is intentionally limited to independent
already-created values and sequence traversal.

## Performance Evidence

The sequence benchmark compares successful Option buffering, Result failure at the first item,
and Validation error accumulation with equivalent direct `foreach` implementations. Inputs are
created during benchmark setup: Option inputs are all present, Result inputs fail at index zero,
and every fourth Validation input is invalid with two errors. The Validation cases therefore
accumulate 8 errors for 16 inputs and 512 errors for 1,024 inputs.

Run it with:

```shell
dotnet run --project benchmarks/FunnySharp.Benchmarks/FunnySharp.Benchmarks.csproj --configuration Release -- --filter '*SequenceBenchmarks*'
```

The following `ShortRun` was recorded on August 31, 2026 with BenchmarkDotNet 0.15.8,
.NET SDK 10.0.400, .NET 10.0.11, and an AMD EPYC 7763 2.44 GHz Hyper-V virtual machine.
The job used one launch, three warmups, and three measured iterations. A dash denotes 0 B
allocated; `N/A` marks a ratio whose direct baseline was reported as `ZeroMeasurement`.

| Scenario | Count | Direct mean | FunnySharp mean | Ratio | Direct allocation | FunnySharp allocation |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Option successful buffering | 16 | 36.959 ns | 46.216 ns | 1.25x | 144 B | 144 B |
| Option successful buffering | 1,024 | 1.504 us | 2.052 us | 1.37x | 4,176 B | 4,176 B |
| Result failure at index zero | 16 | 0.403 ns | 3.362 ns | N/A | - | - |
| Result failure at index zero | 1,024 | 0.417 ns | 3.144 ns | N/A | - | - |
| Validation full accumulation | 16 | 159.528 ns | 176.521 ns | 1.11x | 392 B | 392 B |
| Validation full accumulation | 1,024 | 5.236 us | 6.479 us | 1.24x | 12,632 B | 12,632 B |

Successful Option traversal allocated exactly the same output buffer and read-only wrapper as the
direct implementation in this run; the additional cost was execution time rather than hidden
storage. Result failure at the first item allocated nothing and remained approximately constant as
the unused tail grew from 15 to 1,023 items, demonstrating fail-fast enumeration. Validation read
the complete input and retained every error with the same measured allocation as the direct loop;
its additional measured time was about 11-24% in these cases.

The Result direct baseline was below timer resolution, so its sub-nanosecond values and ratios are
not suitable for performance claims. All measurements are directional: `ShortRun` uses only three
measured iterations, and the Hyper-V environment can affect timing. Rerun on representative
hardware before making latency or capacity decisions.
