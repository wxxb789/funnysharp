# Effects

`FunnySharp` provides `Effect<T>` and `Effect<TEnvironment, T>` as deliberately thin,
deferred boundaries around standard .NET work. They make execution, dependencies,
cancellation, exceptions, and resource lifetime visible without introducing a runtime of their
own. Compiling examples are in
[examples/FunnySharp.Examples/Program.cs](../examples/FunnySharp.Examples/Program.cs).

## API Shape

`Effect<T>` represents work that produces `T` and may use a `CancellationToken`.
`Effect<TEnvironment, T>` represents the same work when an explicit environment is also
required. Both are `readonly struct` wrappers over a delegate and expose `RunAsync`, which
returns `ValueTask<T>`.

Creating or composing an effect never invokes its delegate. Execution starts only when a caller
invokes `RunAsync`:

```csharp
var greeting = Effect.FromSync(() => "hello")
    .Map(text => text.ToUpperInvariant());

var value = await greeting.RunAsync(cancellationToken);
```

The factory methods retain ordinary .NET carriers rather than translating them into a separate
effect language:

- `FromValue` creates a completed effect.
- `FromSync` adapts synchronous work.
- `FromTask` adapts Task-returning work.
- `FromValueTask` adapts ValueTask-returning work.
- `FromResult` places an explicit `Result<TValue, TError>` value in an effect.

`Map` transforms a successful produced value, and `Bind` chooses a later effect from that value.
`Select` and `SelectMany` support LINQ query syntax with the same deferred, left-to-right
composition. A later stage is not invoked when an earlier stage throws, faults, or is cancelled.

The environment-aware factory overloads create `Effect<TEnvironment, T>` values, while the
instance `WithEnvironment<TEnvironment>` method lifts an environment-independent effect to the
same shape. `Provide` supplies a particular environment and returns an environment-independent
`Effect<T>`. The environment is an ordinary caller-owned value, so services, `TimeProvider`,
configuration, and application-specific dependency containers can be supplied directly without
a `Microsoft.Extensions` dependency or a FunnySharp container.

```csharp
var now = Effect
    .FromSync((TimeProvider clock) => clock.GetUtcNow())
    .Provide(TimeProvider.System);

var observed = await now.RunAsync();
```

`Result<TValue, TError>` remains an explicit value inside an effect. `FromResult` does not turn
a Result failure into an exception or a hidden effect failure. Choose `Result` when a domain
failure must be represented and matched; use normal exception behavior for exceptional .NET
failures.

## Execution, Exceptions, And Cancellation

`RunAsync` forwards the exact caller-supplied `CancellationToken` to every applicable delegate.
Effects do not inspect a token, cancel eagerly, replace it, or add a cancellation policy. A
delegate that ignores an already-cancelled token can still return a value; a delegate that throws
or returns a cancelled awaitable retains normal .NET cancellation behavior and its token.

Exceptions are not wrapped or converted to `Result` by ordinary factories or composition. A
synchronous exception thrown while an adapted delegate is invoked is captured in the returned
`ValueTask`, so the `RunAsync` call itself does not throw and `await` observes the original
exception. A faulted or cancelled Task or ValueTask preserves its normal status, exception, and
cancellation token. As with every `ValueTask`, callers must observe the returned operation
according to its normal single-consumption rules.

## Resource Lifetime

`Using` scopes an `IDisposable` resource across an effect, and `UsingAsync` does the same for an
`IAsyncDisposable` resource. Each helper acquires the resource, runs the use effect, and releases
the resource exactly once after success, an explicit Result failure, an exception, or
cancellation.

The acquisition effect runs once per `RunAsync` and must return a resource owned by that run. Use
`FromSync`, `FromTask`, or `FromValueTask` to acquire a fresh resource when the scoped effect will
be reused or run concurrently. `FromValue(existingResource)` returns the same instance on every
run, so combining it with `Using` or `UsingAsync` is an ownership transfer for a single run rather
than a reusable resource factory.

The helpers follow the natural C# `using` and `await using` precedence rules. If both use and
release fail, the release failure is the one observed by the caller; no aggregate, wrapper, or
custom precedence policy is introduced. A resource that was not acquired is not released. A
successful acquisition that produces a null resource fails with `InvalidOperationException`
before invoking the use effect.

## Deliberate Boundaries

Effects are not a scheduler, fiber runtime, dependency-injection container, higher-kinded-type
emulation layer, monad-transformer stack, or large IO universe. An `Effect<T>` itself still does
not add retries, timeouts, supervision, implicit concurrency, parallel execution, or an
alternative Task implementation. Goal 09 adds narrowly scoped extension methods that can
coordinate explicitly supplied cold `Effect<Result<TValue, TError>>` values; see
[Concurrency](concurrency.md). That coordination remains BCL-first, preserves normal exceptions
and cancellation, drains started work, and does not turn `Effect<T>` into a concurrency runtime.

## Performance Evidence

`EffectBenchmarks` compares direct .NET invocation with equivalent `Effect<T>` construction,
execution, mapping, binding, environment provision, and resource-scoping paths. Delegates and
compositions are cached outside steady-state benchmark methods, and every benchmark consumes its
result. Wrapper construction separately compares reusing a cached `Func` with adapting that same
delegate to an effect.

Run the focused benchmark with:

```shell
dotnet run --project benchmarks/FunnySharp.Benchmarks/FunnySharp.Benchmarks.csproj --configuration Release -- --filter '*EffectBenchmarks*'
```

The following ShortRun was recorded on September 1, 2026 with BenchmarkDotNet 0.15.8, .NET
10.0.11, and an AMD EPYC 7763 Hyper-V virtual machine:

| Scenario | Direct mean | FunnySharp mean | Ratio | Direct allocation | FunnySharp allocation |
| --- | ---: | ---: | ---: | ---: | ---: |
| Wrapper construction | 1.014 ns | 10.131 ns | 10.00x | 0 B | 88 B |
| Completed value execution | 14.194 ns | 20.205 ns | 1.42x | 0 B | 0 B |
| Completed synchronous execution | 3.976 ns | 29.458 ns | 7.41x | 0 B | 0 B |
| Synchronous mapping | 4.325 ns | 71.017 ns | 16.42x | 0 B | 0 B |
| Completed `Task` mapping | 25.559 ns | 74.844 ns | 2.93x | 80 B | 80 B |
| Completed `ValueTask` binding | 21.640 ns | 88.503 ns | 4.09x | 24 B | 208 B |
| Environment provision | 3.939 ns | 41.935 ns | 10.65x | 0 B | 0 B |
| Synchronous resource scope | 12.483 ns | 116.816 ns | 9.36x | 0 B | 0 B |
| Asynchronous resource scope | 13.120 ns | 119.344 ns | 9.10x | 0 B | 0 B |

The measurements expose the boundary's cost rather than claiming that it is free. Prebuilt
completed-value, synchronous, mapping, environment, and synchronous-resource effects remained
allocation-free in this run. Completed `Task` mapping matched direct allocation, while effectful
`ValueTask` binding allocated 184 B more than its direct counterpart. Both synchronous and
asynchronous resource scopes remained allocation-free. The workloads are deliberately tiny, so
ratios amplify tens or hundreds of nanoseconds. The run used only three measured iterations on a
virtualized host; treat absolute timings as directional and rerun on target hardware before using
them for capacity decisions.
