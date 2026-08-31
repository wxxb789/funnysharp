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
