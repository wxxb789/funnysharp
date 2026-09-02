# FunnySharp Product Contract

This document is the authoritative contract for the FunnySharp product baseline. Later goals
may extend it deliberately, but implementation convenience alone does not override it.

## Product Direction

- FunnySharp is a pragmatic functional-programming library for idiomatic C# on .NET 10 and later.
- APIs are BCL-first and prefer standard delegates, collections, `Task`, `ValueTask`, and
  `CancellationToken` over a parallel runtime or type universe.
- Synchronous and asynchronous APIs stay consistent where both forms are meaningful. Async APIs
  must preserve cancellation and exception behavior and must never use sync-over-async.
- Performance claims require measurements against equivalent direct C# or BCL code. Hot paths
  should avoid hidden allocation, repeated enumeration, reflection, and unnecessary buffering.
- Immutable data is opt-in. The core package does not impose immutable collections or copying on
  consumers that do not ask for them.
- Immutable updates use a deliberately small `Lens<TSource, TFocus>` and
  `Optional<TSource, TFocus>` surface over caller-provided delegates. The core provides no optics
  hierarchy, traversal API, reflection, property-path API, persistent-collection ecosystem, or
  hidden source copying. Lens laws and purity remain obligations of the caller-provided delegates.
- `System.Collections.Immutable` remains the update mechanism for immutable collections. Callers
  choose its operations and builders inside their own setters or updaters; `FrozenDictionary` and
  `FrozenSet` remain read-optimized snapshots that callers query directly or replace explicitly as
  whole values.
- `IReadOnlyCollection<T>` and `IReadOnlyDictionary<TKey, TValue>` are read-only views, not
  evidence of immutable backing storage. The core does not provide borrowed-view adapters, clone
  mutable leaves, or make shallow record copies pure; aliasing, copying, and ownership stay with
  the caller.
- Data pipelines remain on BCL sequence, span, and memory carriers. Streaming operations are
  deferred and single-pass per enumeration; span and memory operations are immediate, respect view
  lifetimes, and use caller-owned storage for zero-copy or fused paths where practical.
- Effects are thin `readonly struct` wrappers over standard delegates. They defer execution until
  `RunAsync`, return `ValueTask`, and make an optional caller-owned environment, cancellation, and
  resource lifetime explicit without changing normal .NET exception or cancellation semantics.
  `Result<TValue, TError>` remains an explicit value rather than an implicit effect failure.
- Concurrency remains BCL-first and explicit. Bounded parallel mapping streams ordered
  `IAsyncEnumerable<T>` results with `Channel` backpressure and linked operation cancellation;
  parallel traversal materializes ordered values and distinguishes Option/Result fail-fast behavior
  from Validation accumulation. First-success coordination accepts only cold
  `Effect<Result<TValue, TError>>` values, drains all started work, uses typed failures only for
  explicit `Result` failures, and supports cooperative `TimeProvider` timeouts. The core provides
  no naked started-Task racing API, scheduler, fiber runtime, or alternative concurrency carrier.

## Package And Dependency Boundary

- `FunnySharp` is the only shipping package in this baseline and targets `net10.0`; later .NET
  runtimes can consume that asset through normal .NET compatibility rules.
- `src/FunnySharp/FunnySharp.csproj` has no `PackageReference`. Its runtime surface is limited to
  platform assemblies shipped with .NET.
- Test dependencies remain in the non-packable `FunnySharp.Tests` project and never flow into the
  public package.
- ASP.NET Core integration, if introduced by a later goal, belongs in a separate integration
  package. The core must not depend on ASP.NET Core.
- A custom scheduler, fiber/runtime layer, DI container, large functional abstraction stack,
  higher-kinded-type emulation, monad-transformer stack, and large IO universe are outside the
  core boundary. `Microsoft.Extensions` is not a core dependency; `TimeProvider` and ordinary DI
  services are caller-provided environment values.

## Deliberate Deferrals

- The foundation exposes no speculative feature API. Public APIs require a later goal with usage,
  behavior, and verification evidence.
- General discriminated unions and first-party analyzers are out of scope. Reconsidering either
  requires a later goal after the official C# 15/.NET 11 union design has stabilized.

## Baseline Verification

A clean checkout is acceptable only when all five commands in the README succeed:

```shell
dotnet restore FunnySharp.slnx
dotnet build FunnySharp.slnx --configuration Release --no-restore
dotnet test FunnySharp.slnx --configuration Release --no-build
dotnet run --project examples/FunnySharp.Examples/FunnySharp.Examples.csproj --configuration Release --no-build
dotnet pack FunnySharp.slnx --configuration Release --no-build --output artifacts/packages
```

The evidence must also show that:

- the xUnit v3 test is discovered and passes through Microsoft.Testing.Platform;
- the executable example compiles in the release build and runs successfully as usage evidence;
- packing produces `FunnySharp.0.1.0.nupkg` and `FunnySharp.0.1.0.snupkg`;
- the package contains a `lib/net10.0` assembly and the repository README;
- the package dependency group for `net10.0` is empty; and
- only the `FunnySharp` project is packable.
