# FunnySharp

FunnySharp is a pragmatic, BCL-first functional-programming library for .NET 10 and later.
Feature APIs are added only when a goal defines their behavior and verification evidence.

The authoritative design and dependency boundaries are recorded in the
[product contract](https://github.com/wxxb789/funnysharp/blob/main/docs/product-contract.md).

## Function Composition

FunnySharp provides a small standard-delegate surface for piping, left-to-right composition,
currying, partial application, argument flipping, and side-effect observation. Matching `Task`
and `ValueTask` composition preserves asynchronous execution without sync-over-async.

- [Semantics and performance evidence](https://github.com/wxxb789/funnysharp/blob/main/docs/function-composition.md)
- [Compiling examples](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## Option

`Option<T>` represents explicit presence (`Some`) or absence (`None`) with safe inspection,
synchronous composition, and focused nullable, Try-pattern, dictionary, `Task`, and `ValueTask`
bridges. Faults and cancellation remain normal asynchronous failures rather than becoming absence.

- [Semantics](https://github.com/wxxb789/funnysharp/blob/main/docs/option.md)
- [Compiling examples](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## Result

`Result<TValue, TError>` represents explicit success or typed failure with fail-fast mapping,
binding, validation, recovery, combination, LINQ query syntax, Option interop, and matching
`Task`/`ValueTask` composition. Explicit `Try` boundaries preserve cancellation and retain the
original exception unless the caller deliberately maps it to a domain error.

- [Semantics](https://github.com/wxxb789/funnysharp/blob/main/docs/result.md)
- [Compiling examples](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## Effects

`Effect<T>` and `Effect<TEnvironment, T>` provide a thin, deferred boundary for standard .NET
work. They compose through `ValueTask`, make dependencies and resource lifetime explicit, and
preserve normal exception and cancellation behavior without adding an effect runtime or DI
container.

- [Semantics and performance evidence](https://github.com/wxxb789/funnysharp/blob/main/docs/effects.md)
- [Compiling examples](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## Concurrency

FunnySharp coordinates explicit bounded parallel mapping and traversal over `IAsyncEnumerable<T>`,
plus first-success selection over cold `Effect<Result<TValue, TError>>` values. These APIs retain
standard .NET cancellation, exception, `ValueTask`, `Channel`, and `TimeProvider` behavior without
adding a concurrency runtime or scheduler.

- [Semantics and performance evidence](https://github.com/wxxb789/funnysharp/blob/main/docs/concurrency.md)
- [Compiling examples](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## Validation

`Validation<TValue, TError>` represents a valid value or one or more domain errors. It is for
independent checks that should all run and report their errors in deterministic order; use
`Option<T>` or `Result<TValue, TError>` when fail-fast behavior is the intended contract.

- [Semantics and shared traversal behavior](https://github.com/wxxb789/funnysharp/blob/main/docs/validation.md)
- [Compiling examples](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## Data Pipelines

FunnySharp keeps pipelines on standard .NET carriers. Use LINQ and .NET 10 async LINQ for ordinary
projection, filtering, flattening, ordering, and explicit materialization. `Choose` adds a fused
Option-aware filter-map for synchronous and asynchronous streams, while span and memory helpers
write to caller-owned storage or transform it in place.

- [Semantics, lifetime rules, and performance evidence](https://github.com/wxxb789/funnysharp/blob/main/docs/data-pipelines.md)
- [Compiling data-cleaning examples](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## State Machines

FunnySharp models pure state changes and finite-state workflows with explicit state, emitted output
commands, invalid events, transition failures, undefined transitions, composition, and replay. The
transition core remains synchronous and can stay deterministic without executing effects; callers
choose where and how emitted commands perform asynchronous work.

- [Semantics, replay rules, and async boundary](https://github.com/wxxb789/funnysharp/blob/main/docs/state-machines.md)
- [Compiling approval-workflow example](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## Verify

```shell
dotnet restore FunnySharp.slnx
dotnet build FunnySharp.slnx --configuration Release --no-restore
dotnet test FunnySharp.slnx --configuration Release --no-build
dotnet run --project examples/FunnySharp.Examples/FunnySharp.Examples.csproj --configuration Release --no-build
dotnet pack FunnySharp.slnx --configuration Release --no-build --output artifacts/packages
```

## Benchmark

```shell
dotnet run --project benchmarks/FunnySharp.Benchmarks/FunnySharp.Benchmarks.csproj --configuration Release -- --filter '*'
```
