# Function Composition

`FunnySharp` provides a small set of extension methods over standard C# delegates and values.
The executable examples are in [examples/FunnySharp.Examples/Program.cs](../examples/FunnySharp.Examples/Program.cs).
The authoritative verb table and naming rules are in [Functional API Grammar](grammar.md).

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
- `Func<T, ValueTask<TIntermediate>>.ComposeValueAsync(Func<TIntermediate, ValueTask<TResult>>)`
- Cancellation-aware variants of both shapes, taking and returning delegates whose second parameter is `CancellationToken`.
- `value.TapAsync(...)` observes through a `Task`-returning delegate and returns `Task<T>`.
- `value.TapValueAsync(...)` observes through a `ValueTask`-returning delegate and returns `ValueTask<T>`.

## Evaluation And Failure Semantics

`Compose`, `ComposeAsync`, and `ComposeValueAsync` evaluate left to right: the first delegate receives the input, and the second delegate receives the first result. A second stage is not invoked when the first stage throws, faults, or is canceled. The helpers do not catch, wrap, or replace those failures, so the original exception instance flows through normal C# invocation or `await` semantics.

Every public helper validates its delegate argument with `ArgumentNullException`. Composition validates both delegates when the composed delegate is created. `Pipe` and the `Tap` helpers validate their delegate before attempting the operation.

The cancellation-aware `ComposeAsync`, `ComposeValueAsync`, and tap overloads pass the exact supplied `CancellationToken` to each user delegate. They do not inspect a token or cancel eagerly; cancellation behavior remains the delegates' responsibility. Thus a canceled token can still produce a value when both delegates elect not to observe it.

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

There are also no mixed `Task`/`ValueTask` compose overloads. Keeping each composition in one async return kind avoids a broader overload set, unclear conversion choices, and accidental changes to `ValueTask` consumption behavior. Convert explicitly at a call site when a mixed pipeline is necessary.

## Fallible Composition

Fallible-function composition stays `Bind` on the carrier: there is deliberately no Kleisli-compose
operator over `Func<T, Result<...>>` and no fallible pipeline hierarchy. The stages compose through
`Result.Map`/`Result.Bind` themselves, so a `Func<string, Result<int, TError>>` stage chains
directly into the next stage, and a failure stops the chain with the first failure's error. The full
verb-by-carrier contract is in [Functional API Grammar](grammar.md).

<!-- documentation-sample: DocumentationSamples.FunctionComposition.FallibleCompose -->
```csharp
Func<string, Result<int, ParseError>> parseQuantity = ParseQuantity;
Func<int, Result<decimal, ParseError>> lookupUnitPrice = LookupUnitPrice;

Result<decimal, ParseError> lineTotal = parseQuantity(request.QuantityText)
    .Bind(lookupUnitPrice)
    .Map(unitPrice => unitPrice * request.Units);
```

`Option.Bind` chains compose absence-producing functions the same way. The chain replaces the nested
`if`/`switch` plumbing each fallible stage would otherwise need, and the generic `Pipe` extension
already accepts Result-returning standard delegates, so no Result-specific pipeline or delegate
hierarchy is required.

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
| Completed Task invocation | 34.784 ns | 39.945 ns | 1.15x | 216 B | 216 B |
| Completed ValueTask invocation | 14.292 ns | 19.488 ns | 1.36x | 0 B | 0 B |
| Delegate construction | 11.283 ns | 19.032 ns | 1.69x | 64 B | 96 B |
| Synchronous invocation | 1.377 ns | 5.855 ns | 4.25x | 0 B | 0 B |

Excluded measurements:
- Unmeasured helpers: Pipe, Tap, Curry, Uncurry, Partial, and Flip have no numeric release claim.
<!-- performance-table:end function-composition -->

These measurements expose the trade-off rather than claiming the wrapper is free. Rerun timing on
the target deployment hardware before using it for capacity decisions.
