# FunnySharp

FunnySharp is a pragmatic, BCL-first functional-programming library targeting .NET 10.
Feature APIs are added only when a goal defines their behavior and verification evidence.

The authoritative design and dependency boundaries are recorded in the
[product contract](https://github.com/wxxb789/funnysharp/blob/main/docs/product-contract.md).
The current fail-closed release gate and its explicit evidence checklist are recorded in
[release readiness](https://github.com/wxxb789/funnysharp/blob/main/docs/release-readiness.md).

## Performance

Every performance-relevant stable operation has documented complexity, enumeration, allocation,
boxing, materialization, buffering, and async-scheduling characteristics, backed by measured
comparisons against raw BCL, idiomatic LINQ, FSharp.Core, and pinned functional-library
alternatives. Allocation budgets and complexity regressions block a release; hosted timing stays
directional until a fixed-hardware runner exists.

- [Consolidated performance guidance](https://github.com/wxxb789/funnysharp/blob/main/docs/performance.md)

## Grammar

One small grammar governs every verb: one primary meaning and a predictable output shape on every
carrier where that meaning is valid, so evaluation order, short-circuiting, exception,
cancellation, enumeration, and materialization behavior follow from the signature and contract.

- [Authoritative grammar table](https://github.com/wxxb789/funnysharp/blob/main/docs/grammar.md)

## Function Composition

FunnySharp provides a small standard-delegate surface for piping, left-to-right composition,
currying, partial application, argument flipping, and side-effect observation. Matching `Task`
and `ValueTask` composition and observation (`ComposeAsync`/`ComposeValueAsync`,
`TapAsync`/`TapValueAsync`) preserve asynchronous execution without sync-over-async.

- [Semantics and performance evidence](https://github.com/wxxb789/funnysharp/blob/main/docs/function-composition.md)
- [Compiling examples](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## Option

`Option<T>` represents explicit presence (`Some`) or absence (`None`) with safe inspection,
synchronous composition, and focused nullable, Try-pattern, dictionary, `Task`, and `ValueTask`
bridges. Faults and cancellation remain normal asynchronous failures rather than becoming absence.
LINQ `Select`/`SelectMany` are secondary aliases of `Map`/`Bind` for query syntax; there is no
`Where`.

- [Semantics](https://github.com/wxxb789/funnysharp/blob/main/docs/option.md)
- [Compiling examples](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## Result

`Result<TValue, TError>` represents explicit success or typed failure with fail-fast mapping,
binding, validation, recovery, combination, LINQ query syntax, Option interop, and matching
`Task`/`ValueTask` composition. Explicit `Try` boundaries preserve cancellation and retain the
original exception unless the caller deliberately maps it to a domain error.

- [Semantics](https://github.com/wxxb789/funnysharp/blob/main/docs/result.md)
- [Compiling examples](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## UnitResult

`UnitResult<TError>` represents success or typed failure for commands, deletes, and notifications
that have no meaningful value to carry, without a dummy payload or a public `Unit` type. It offers
matching inspection, mapping to `Result`, binding, validation, recovery, error mapping, combination,
fail-fast sequence traversal, and matching `Task`/`ValueTask` composition.

- [Semantics](https://github.com/wxxb789/funnysharp/blob/main/docs/unit-result.md)
- [Compiling examples](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## Effects

`Effect<T>` and `Effect<TEnvironment, T>` provide a thin, deferred boundary for standard .NET
work. They compose through `ValueTask`, make dependencies and resource lifetime explicit, and
preserve normal exception and cancellation behavior without adding an effect runtime or DI
container.

- [Semantics and performance evidence](https://github.com/wxxb789/funnysharp/blob/main/docs/effects.md)
- [Compiling examples](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## Concurrency

FunnySharp coordinates explicit bounded parallel mapping in source order or completion order and
traversal over `IAsyncEnumerable<T>`, plus first-success selection over cold
`Effect<Result<TValue, TError>>` values. These APIs retain standard .NET cancellation, exception,
`ValueTask`, `Channel`, and `TimeProvider` behavior without adding a concurrency runtime or
scheduler.

- [Semantics and performance evidence](https://github.com/wxxb789/funnysharp/blob/main/docs/concurrency.md)
- [Compiling examples](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## Validation

`Validation<TValue, TError>` represents a valid value or one or more domain errors. It is for
independent checks that should all run and report their errors in deterministic order; use
`Option<T>`, `Result<TValue, TError>`, or `UnitResult<TError>` when fail-fast behavior is the
intended contract.

- [Semantics and shared traversal behavior](https://github.com/wxxb789/funnysharp/blob/main/docs/validation.md)
- [Compiling examples](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## Data Pipelines

FunnySharp keeps pipelines on standard .NET carriers. Use LINQ and .NET 10 async LINQ for ordinary
projection, filtering, flattening, ordering, and explicit materialization. `Choose` adds a fused
Option-aware filter-map and `Scan` adds a running aggregate for synchronous and asynchronous
streams, while span and memory helpers write to caller-owned storage or transform it in place.

- [Semantics, lifetime rules, and performance evidence](https://github.com/wxxb789/funnysharp/blob/main/docs/data-pipelines.md)
- [Compiling data-cleaning examples](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## Collections

FunnySharp keeps standard .NET collections and sequence types as the ecosystem and adds the
operations whose absence the BCL leaves ambiguous: `*OrNone` cardinality access
(`FirstOrNone`, `LastOrNone`, `SingleOrNone`, `ElementAtOrNone`, `MinOrNone`, `MaxOrNone`),
the `NonEmpty<T>` guarantee with a seedless fold that cannot throw, one-pass `Partition` for
predicate and Option/Result/UnitResult sequences, exact versus truncating combination
(`ZipExact`, `ZipExactOrNone`), container and `IParsable` parse bridges, `WhereNotNull`, and
traversal context — indexed, keyed, and compositional `Location` context — so a traversal failure
can carry `customers[17].addresses[2].postalCode` without application code assembling it.

- [Semantics, carrier behavior matrix, and deliberate exclusions](https://github.com/wxxb789/funnysharp/blob/main/docs/collections.md)
- [Compiling data-cleaning and batch-validation examples](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## Immutable Updates

`Lens<TSource, TFocus>` and `Optional<TSource, TFocus>` provide a small, composable surface for
total and possibly missing nested updates. They work with record `with` expressions and caller
chosen BCL immutable collection operations without adding a collection hierarchy or hidden copies.

- [Semantics, BCL collection guidance, and performance evidence](https://github.com/wxxb789/funnysharp/blob/main/docs/immutable-updates.md)
- [Compiling immutable-update examples](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## State Machines

FunnySharp models pure state changes and finite-state workflows with explicit state, emitted output
commands, invalid events, transition failures, undefined transitions, composition, and replay. The
transition core remains synchronous and can stay deterministic without executing effects; callers
choose where and how emitted commands perform asynchronous work.

- [Semantics, replay rules, and async boundary](https://github.com/wxxb789/funnysharp/blob/main/docs/state-machines.md)
- [Compiling approval-workflow example](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs)

## ASP.NET Core

`FunnySharp.AspNetCore` is a separate Minimal API integration package. It maps explicit
`Option`, `Result`, `UnitResult`, `Validation`, `Task`, `ValueTask`, and `Effect` outcomes to
caller-selected `IResult` and RFC-compatible `ProblemDetails` without coupling the BCL-only core
package to ASP.NET Core.

- [Integration semantics and endpoint examples](https://github.com/wxxb789/funnysharp/blob/main/docs/aspnet-core.md)
- [Compiling Minimal API example](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.AspNetCore.Examples/Program.cs)

## Verify

```powershell
pwsh -NoProfile -File eng/Run-Release.ps1 `
  -AttemptId local-full-1 `
  -CompatibilityRuntimeIdentifier win-x64 `
  -CompatibilityPackageFeed https://packagefeedproxy.microsoft.io/nuget/v3/index.json `
  -DistributionFeed https://packagefeedproxy.microsoft.io/nuget/v3/index.json
```

The runner rejects a dirty candidate, re-used attempt identity, published or ambiguous package
version, unsafe generated-output path, or non-isolated restore. It performs locked no-cache restore,
Release build, xUnit tests, both examples, pack, formatting verification, semantic benchmark
preflight, the complete BenchmarkDotNet suite, allocation-policy verification, generated-table
verification, and package-consuming trim/Native AOT smokes. Compatibility results apply only to
their recorded SDK, runtime patch, OS, RID, and canonical package hashes; see the
[product contract](https://github.com/wxxb789/funnysharp/blob/main/docs/product-contract.md) for the
current support and Native AOT limits.

GitHub release validation exposes four stable required contexts: `release / win-x64`,
`release / linux-x64`, `release / osx-arm64`, and `release / osx-x64-consumer`. Repository ruleset
readback is separate operational evidence; without it, Goal 13 product acceptance remains failed.

For a PowerShell-free local pre-check, see [tooling](https://github.com/wxxb789/funnysharp/blob/main/docs/tooling.md).

## Benchmark

```shell
dotnet run --project benchmarks/FunnySharp.Benchmarks/FunnySharp.Benchmarks.csproj --configuration Release -- --filter '*'
```

## License

FunnySharp is licensed under the [MIT License](LICENSE).
