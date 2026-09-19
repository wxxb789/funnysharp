# Result

`FunnySharp` provides `Result<TValue, TError>` for a value that is either successful or a typed
failure. It supports pragmatic Railway Oriented Programming with standard C# values, delegates,
`Task`, and `ValueTask`; it does not introduce an effect runtime or make exceptions the normal
control-flow mechanism. Compiling examples are in
[examples/FunnySharp.Examples/Program.cs](../examples/FunnySharp.Examples/Program.cs).

## API Shape

Create values with `Result<TValue, TError>.Success(value)` and
`Result<TValue, TError>.Failure(error)`. `Result<TValue, TError>` is a `readonly struct` with no
public constructor. `default(Result<TValue, TError>)` is uninitialized, not a failure: every member
that reads the case or payload throws `InvalidOperationException` with the message
`"The result has not been initialized."`, and only `ToString()` returns the diagnostic text
`"Uninitialized"` without throwing. Construct a value explicitly before inspecting or composing it.

Use `IsSuccess`, `IsFailure`, `TryGetValue`, `TryGetError`, or `Match` to inspect a result. There is
intentionally no throwing value or error accessor. The active case is independent of its payload:
successful and failed results preserve runtime-null payloads instead of silently changing cases.

The synchronous composition surface is:

- `Map` transforms success while preserving failure.
- `Bind` sequences result-returning functions and stops at the first failure.
- `MapError` transforms failure while preserving success.
- `Ensure` keeps a successful value only when a predicate accepts it.
- `Recover` and `RecoverWith` turn a failure into a value or another result.
- `Zip` combines already-created results: two into a `(First, Second)` pair, or 2–4 through a
  combine function; either form returns the first failure in left-to-right order.
- `ZipWith` accepts a factory so the second operation is not invoked after the first failure.

`Select` and `SelectMany` are secondary LINQ aliases of `Map` and `Bind` for fail-fast query-syntax
composition. There is no `Where` alias because a Boolean predicate cannot create a `TError`; use
`Ensure` to make that failure explicit. Documentation presents the member-centric vocabulary first.
The generic `Pipe` extension already accepts Result-returning standard delegates, so no
Result-specific pipeline or delegate hierarchy is required.

## Option Interop

`Option<T>.ToResult(error)` and `Option<T>.ToResult(errorFactory)` convert presence to success and
absence to an explicit failure. The factory is lazy and runs only for `None`.

`Result<TValue, TError>.ToOption()` keeps success and discards failure details. Because `Option<T>`
does not contain runtime null, converting `Success(null)` produces `None`; this is an explicit,
potentially lossy boundary.

`Result<TValue, TError>.ToUnitResult()` drops the successful value and preserves the failure object
when a value-producing step continues into no-value work; `ToUnitResultAsync` does the same for
`Task<Result<...>>` and `ValueTask<Result<...>>` completions. See
[Unit result semantics](unit-result.md).

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

Results do not flatten. `Result<Result<int, string>, string>` is a success whose value is another
result; comparing two such values compares the inner carriers with their own structural equality, and
a nested failure is not promoted to the outer failure channel.

## Asynchronous Composition

`MapAsync` and `BindAsync` use Task-returning callbacks. `MapValueAsync` and `BindValueAsync` use
ValueTask-returning callbacks. Each also has a cancellation-aware overload that passes the exact
supplied `CancellationToken` to the callback without eagerly cancelling.

Failure returns an already-completed failed Result and does not invoke a callback or inspect the
token. Success invokes the callback once and observes the returned awaitable once. Faults and
cancellation remain ordinary asynchronous failures, including the original exception object,
cancellation status, established stack trace, and token. A faulted source containing an
`OperationCanceledException` stays faulted; only a canceled source or callback cancellation produces
a canceled result operation. Incomplete Task-backed and ValueTask-backed paths transfer that exact
completed state rather than reconstructing cancellation from a token. `ValueTask` follows its normal
single-consumption rule.

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

`TryAsync` and `TryValueAsync` deliberately accept tokenless delegates. They are narrow exception
boundaries, not cancellation owners: a caller that needs cancellation captures its
`CancellationToken` and passes it inside the delegate to the underlying API. The helpers do not
inject, replace, or map that token, and an `OperationCanceledException` is never converted to a
Result failure or passed to an error mapper. A `ValueTask` returned by the delegate is observed
once, so its normal single-consumption rule still applies.

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
`Validation<TValue, TError>`. General data transforms remain on BCL carriers rather than a custom
collection hierarchy. There is no retry policy, exception taxonomy, serialization converter,
analyzer, or source generator in this surface.

## Performance Evidence

The benchmark project compares Result construction, fail-fast pipelines, exception boundaries,
and completed Task and ValueTask mapping with equivalent direct branches or try/catch code.
Delegates are cached outside steady-state benchmark methods, and each ValueTask is created per
invocation.

Run it with:

```shell
dotnet run --project benchmarks/FunnySharp.Benchmarks/FunnySharp.Benchmarks.csproj --configuration Release -- --filter '*ResultBenchmarks*'
```

The exact table below is generated from the approved observation in
`eng/performance/baseline.json`. It includes genuinely pending Task and ValueTask completion paths.
Hosted timing is directional; allocation ceilings are the blocking contract. `N/A` means timing was
below resolution or unavailable.

<!-- performance-table:start result -->
| Scenario | Baseline mean | FunnySharp mean | Ratio | Baseline allocation | FunnySharp allocation |
| --- | ---: | ---: | ---: | ---: | ---: |
| Completed Task mapping | 27.478 ns | 50.798 ns | 1.85x | 144 B | 256 B |
| Completed ValueTask mapping | 11.724 ns | 38.824 ns | 3.31x | 0 B | 0 B |
| Construction and inspection - failure | 1.929 ns | N/A | N/A | 0 B | 0 B |
| Construction and inspection - success | N/A | N/A | N/A | 0 B | 0 B |
| Exception boundary - failure | 2.896 us | 4.142 us | 1.43x | 512 B | 680 B |
| Exception boundary - success | 11.511 ns | 14.299 ns | 1.24x | 0 B | 0 B |
| Fail-fast pipeline - failure | 0.160 ns | 2.700 ns | 16.91x | 0 B | 0 B |
| Fail-fast pipeline - success | N/A | 10.295 ns | N/A | 0 B | 0 B |
| Pending Task mapping | 2.515 us | 3.586 us | 1.43x | 296 B | 744 B |
| Pending ValueTask mapping | 2.413 us | 3.450 us | 1.43x | 304 B | 840 B |
<!-- performance-table:end result -->

The generated table exposes measured costs without interpreting below-resolution ratios. Rerun
timing on representative deployment hardware before making latency or capacity decisions.
