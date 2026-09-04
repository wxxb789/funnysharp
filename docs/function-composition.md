# Function Composition

`FunnySharp` provides a small set of extension methods over standard C# delegates and values.
The executable examples are in [examples/FunnySharp.Examples/Program.cs](../examples/FunnySharp.Examples/Program.cs).

## API Shape

The synchronous surface is intentionally small:

- `value.Pipe(function)` applies `Func<T, TResult>` to a value.
- `first.Compose(second)` creates `Func<T, TResult>` from two compatible unary functions.
- `binary.Curry()` and `curried.Uncurry()` convert between `Func<TFirst, TSecond, TResult>` and `Func<TFirst, Func<TSecond, TResult>>`.
- `binary.Partial(first)` binds the first argument of a binary function.
- `binary.Flip()` returns a binary function with its arguments reversed.
- `value.Tap(observer)` invokes `Action<T>` and returns the same value.

Async composition is available for matching return kinds:

- `Func<T, Task<TIntermediate>>.ComposeAsync(Func<TIntermediate, Task<TResult>>)`
- `Func<T, ValueTask<TIntermediate>>.ComposeAsync(Func<TIntermediate, ValueTask<TResult>>)`
- Cancellation-aware variants of both shapes, taking and returning delegates whose second parameter is `CancellationToken`.
- `value.TapAsync(...)` observes through a `Task`-returning delegate and returns `Task<T>`.
- `value.TapValueAsync(...)` observes through a `ValueTask`-returning delegate and returns `ValueTask<T>`.

## Evaluation And Failure Semantics

`Compose` and `ComposeAsync` evaluate left to right: the first delegate receives the input, and the second delegate receives the first result. A second stage is not invoked when the first stage throws, faults, or is canceled. The helpers do not catch, wrap, or replace those failures, so the original exception instance flows through normal C# invocation or `await` semantics.

Every public helper validates its delegate argument with `ArgumentNullException`. Composition validates both delegates when the composed delegate is created. `Pipe` and the `Tap` helpers validate their delegate before attempting the operation.

The cancellation-aware `ComposeAsync` and tap overloads pass the exact supplied `CancellationToken` to each user delegate. They do not inspect a token or cancel eagerly; cancellation behavior remains the delegates' responsibility. Thus a canceled token can still produce a value when both delegates elect not to observe it.

Internally, asynchronous helpers await with `ConfigureAwait(false)`. This avoids imposing a synchronization-context capture on the helper's own continuations, while leaving each supplied delegate responsible for its own async behavior.

After eager delegate-argument validation, exceptions thrown while invoking an asynchronous user delegate are represented by the returned `Task` or `ValueTask`, even when the delegate throws before returning its awaitable. Await the returned operation to observe that original exception instance. A failure in the first stage still prevents the second stage from running.

The `ValueTask` composition and tap helpers await each returned `ValueTask` exactly once. Consumers must still follow the normal `ValueTask` rule: await the `ValueTask` returned by the composed function once, rather than storing and awaiting it repeatedly.

## Deliberate Boundaries

The argument-reordering helpers stop at binary delegates. Arbitrary arities would require a large overload family with limited additional discoverability, and consumers can use an ordinary lambda for uncommon shapes.

There is no `PipeAsync`. `Pipe` applies a delegate without awaiting its result, so an async delegate naturally produces an awaitable that callers can await:

<!-- documentation-sample: DocumentationSamples.FunctionComposition.PipeAsync -->
```csharp
var result = await 4.Pipe(async value =>
{
    await Task.Yield();
    return value * 3;
});
```

There are also no mixed `Task`/`ValueTask` `ComposeAsync` overloads. Keeping each composition in one async return kind avoids a broader overload set, unclear conversion choices, and accidental changes to `ValueTask` consumption behavior. Convert explicitly at a call site when a mixed pipeline is necessary.

## Performance Evidence

The benchmark project compares prebuilt composed delegates with equivalent direct C# delegates that execute the same leaf transforms. Delegate construction is measured separately from repeated invocation.

Run it with:

```shell
dotnet run --project benchmarks/FunnySharp.Benchmarks/FunnySharp.Benchmarks.csproj --configuration Release -- --filter '*'
```

The exact table below is generated from the approved observation in
`eng/performance/baseline.json`. Hosted timing is directional; allocation ceilings are the blocking
contract.

<!-- performance-table:start function-composition -->
| Scenario | Baseline mean | FunnySharp mean | Ratio | Baseline allocation | FunnySharp allocation |
| --- | ---: | ---: | ---: | ---: | ---: |
| Completed Task invocation | 27.609 ns | 30.374 ns | 1.10x | 216 B | 216 B |
| Completed ValueTask invocation | 13.716 ns | 15.642 ns | 1.14x | 0 B | 0 B |
| Delegate construction | 7.633 ns | 14.771 ns | 1.94x | 64 B | 96 B |
| Synchronous invocation | 0.956 ns | 5.520 ns | 5.77x | 0 B | 0 B |

Excluded measurements:
- Unmeasured helpers: Pipe, Tap, Curry, Uncurry, Partial, and Flip have no numeric release claim.
<!-- performance-table:end function-composition -->

These measurements expose the trade-off rather than claiming the wrapper is free. Rerun timing on
the target deployment hardware before using it for capacity decisions.
