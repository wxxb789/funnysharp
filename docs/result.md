# Result

`FunnySharp` provides `Result<TValue, TError>` for a value that is either successful or a typed
failure. It supports pragmatic Railway Oriented Programming with standard C# values, delegates,
`Task`, and `ValueTask`; it does not introduce an effect runtime or make exceptions the normal
control-flow mechanism. Compiling examples are in
[examples/FunnySharp.Examples/Program.cs](../examples/FunnySharp.Examples/Program.cs).

## API Shape

Create values with `Result<TValue, TError>.Success(value)` and
`Result<TValue, TError>.Failure(error)`. `Result<TValue, TError>` is a `readonly struct` with no
public constructor. `default(Result<TValue, TError>)` is a failure containing `default(TError)`.

Use `IsSuccess`, `IsFailure`, `TryGetValue`, `TryGetError`, or `Match` to inspect a result. There is
intentionally no throwing value or error accessor. The active case is independent of its payload:
successful and failed results preserve runtime-null payloads instead of silently changing cases.

The synchronous composition surface is:

- `Map` transforms success while preserving failure.
- `Bind` sequences result-returning functions and stops at the first failure.
- `MapError` transforms failure while preserving success.
- `Ensure` keeps a successful value only when a predicate accepts it.
- `Recover` and `RecoverWith` turn a failure into a value or another result.
- `Zip` combines two already-created results and returns the first failure in left-to-right order.
- `ZipWith` accepts a factory so the second operation is not invoked after the first failure.

`Select` and `SelectMany` provide standard LINQ query syntax for fail-fast result composition.
There is no `Where` alias because a Boolean predicate cannot create a `TError`; use `Ensure` to make
that failure explicit. The generic `Pipe` extension already accepts Result-returning standard
delegates, so no Result-specific pipeline or delegate hierarchy is required.

## Option Interop

`Option<T>.ToResult(error)` and `Option<T>.ToResult(errorFactory)` convert presence to success and
absence to an explicit failure. The factory is lazy and runs only for `None`.

`Result<TValue, TError>.ToOption()` keeps success and discards failure details. Because `Option<T>`
does not contain runtime null, converting `Success(null)` produces `None`; this is an explicit,
potentially lossy boundary.

## Evaluation And Failure Semantics

All callback-taking methods validate their delegates at entry, even when the active case will
short-circuit. A selected callback is invoked at most once. Callback exceptions are not caught,
wrapped, or replaced by ordinary composition methods.

`Map`, `Bind`, `Ensure`, and LINQ query composition do not invoke success callbacks after failure.
`MapError`, `Recover`, and `RecoverWith` do not invoke failure callbacks after success. `Zip`
chooses the left failure before the right failure; it does not undo computation already performed
to create its argument. Use `ZipWith` or `Bind` when the later operation itself must be skipped.

Equality, `==`, and `!=` compare the active case and its payload with the appropriate
`EqualityComparer<T>.Default`. Equal results have equal hashes, and success never equals failure
solely because their payloads compare equally. `ToString()` returns diagnostic `Success(payload)`
or `Failure(error)` text and is not a serialization contract.

## Asynchronous Composition

`MapAsync` and `BindAsync` use Task-returning callbacks. `MapValueAsync` and `BindValueAsync` use
ValueTask-returning callbacks. Each also has a cancellation-aware overload that passes the exact
supplied `CancellationToken` to the callback without eagerly cancelling.

Failure returns an already-completed failed Result and does not invoke a callback or inspect the
token. Success invokes the callback once and observes the returned awaitable once. Faults and
cancellation remain ordinary asynchronous failures, including the original exception object,
cancellation status, and token. A faulted source containing an `OperationCanceledException` stays
faulted; only a cancelled source produces a cancelled result operation. `ValueTask` follows its
normal single-consumption rule.

FunnySharp does not add an async Result wrapper. Await a Result-producing operation at the normal
C# boundary, then continue with the same synchronous or asynchronous Result methods.

## Exception Boundaries

`Result.Try`, `Result.TryAsync`, and `Result.TryValueAsync` are explicit adapters for APIs that
throw. Their default overloads return `Result<TValue, Exception>` and store the exact exception
object, preserving its identity and established stack trace. Mapper overloads convert an exception
to a domain-specific `TError`; retaining the original exception in that error is an explicit caller
choice rather than a silent library transformation.

The boundary helpers never convert `OperationCanceledException` or its subclasses into a Result
failure. Synchronous cancellation is thrown to the caller. Asynchronous cancellation remains a
cancelled Task or ValueTask with its cancellation token. A faulted awaitable containing an
`OperationCanceledException` remains faulted rather than being reclassified as cancellation.
Delegate validation is synchronous; exceptions raised by an async operation are represented by
the returned awaitable.

A `TryAsync` operation that returns a null Task violates the delegate contract. The returned Task
faults with `InvalidOperationException`, and that programming error is not sent through the domain
error mapper.

These helpers are intended for narrow trust boundaries around exception-throwing APIs. Ordinary
Result composition does not catch exceptions, and exceptions are not the default Result creation
path.

## Deliberate Boundaries

The core package has no custom scheduler, effect runtime, async Result type, implicit conversions,
throwing accessors, or general discriminated-union machinery. Result is fail-fast, including the
shared `IEnumerable<T>` and `IAsyncEnumerable<T>` `Sequence` and `Traverse` operations; their
common ordering, materialization, fault, cancellation, and disposal rules are documented in
[Validation and traversal semantics](validation.md). Error accumulation belongs to
`Validation<TValue, TError>`. There is no broad collection API, retry policy, exception taxonomy,
serialization converter, analyzer, or source generator in this surface.

## Performance Evidence

The benchmark project compares Result construction, fail-fast pipelines, exception boundaries,
and completed Task and ValueTask mapping with equivalent direct branches or try/catch code.
Delegates are cached outside steady-state benchmark methods, and each ValueTask is created per
invocation.

Run it with:

```shell
dotnet run --project benchmarks/FunnySharp.Benchmarks/FunnySharp.Benchmarks.csproj --configuration Release -- --filter '*ResultBenchmarks*'
```

The following `ShortRun` was recorded on August 31, 2026 with BenchmarkDotNet 0.15.8,
.NET SDK 10.0.400, .NET 10.0.11, and an AMD EPYC 7763 2.44 GHz Hyper-V virtual machine.
The job used one launch, three warmups, and three measured iterations. A dash in an allocation
column denotes 0 B; `N/A` denotes that the baseline was at or below timer resolution, so a ratio
would not be meaningful.

| Scenario | Direct mean | FunnySharp mean | Ratio | Direct allocation | FunnySharp allocation |
| --- | ---: | ---: | ---: | ---: | ---: |
| Completed `Task` mapping | 20.398 ns | 52.580 ns | 2.58x | 144 B | 264 B |
| Completed `ValueTask` mapping | 10.355 ns | 39.809 ns | 3.85x | - | - |
| Construction and inspection, failure | 0.009 ns | 0.214 ns | N/A | - | - |
| Construction and inspection, success | 0.020 ns | 0.046 ns | N/A | - | - |
| Exception boundary, failure | 2.642 us | 3.230 us | 1.22x | 512 B | 680 B |
| Exception boundary, success | 11.407 ns | 14.593 ns | 1.28x | - | - |
| Fail-fast pipeline, failure | 0.014 ns | 1.524 ns | N/A | - | - |
| Fail-fast pipeline, success | 0.000 ns | 8.753 ns | N/A | - | - |

The completed Task path added 120 B per operation in this run, while the completed ValueTask path
remained allocation-free. The explicit exception boundary added about 28% on success and 22% on
failure; most failure-path cost still came from throwing and constructing the exception itself.
The synchronous Result construction and pipeline cases allocated nothing. Their direct baselines,
and several Result measurements, were reported as `ZeroMeasurement`, so the sub-nanosecond values
and ratios are not suitable for performance claims. These results are directional because the run
used a virtualized host and only three measured iterations; rerun on representative deployment
hardware before making latency or capacity decisions.
