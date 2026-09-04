# FunnySharp Product Contract

This document is the authoritative contract for the FunnySharp product baseline. Later goals
may extend it deliberately, but implementation convenience alone does not override it.

## Product Direction

- FunnySharp is a pragmatic functional-programming library for idiomatic C# targeting .NET 10.
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

- `FunnySharp` and `FunnySharp.AspNetCore` are the two shipping packages and target `net10.0`.
- The required release matrix treats Windows x64, Linux x64, and macOS arm64 as full supported
  hosts. Intel macOS is a required bounded package-consumer smoke. Every required host consumes the
  exact canonical package hashes produced by the Windows full job.
- Release evidence records the exact SDK, runtime, operating system, architecture, RID, workflow
  revision, package hashes, and loaded assembly hashes. Passing one host does not imply another.
- `src/FunnySharp/FunnySharp.csproj` has no `PackageReference`. Its runtime surface is limited to
  platform assemblies shipped with .NET.
- `src/FunnySharp.AspNetCore/FunnySharp.AspNetCore.csproj` is the separate optional HTTP
  integration package. It depends only on `FunnySharp` and the `Microsoft.AspNetCore.App`
  framework reference; it does not add a package dependency to the core.
- Test dependencies remain in the non-packable `FunnySharp.Tests` project and never flow into the
  public packages. ASP.NET Core integration test dependencies remain isolated in the non-packable
  `FunnySharp.AspNetCore.Tests` project.
- Both packages use the repository's MIT license and include explicit `MIT` NuGet license metadata.
- The core remains BCL-only and must not depend on ASP.NET Core.
- A custom scheduler, fiber/runtime layer, DI container, large functional abstraction stack,
  higher-kinded-type emulation, monad-transformer stack, and large IO universe are outside the
  core boundary. `Microsoft.Extensions` is not a core dependency; `TimeProvider` and ordinary DI
  services are caller-provided environment values.

## Deliberate Deferrals

- The foundation exposes no speculative feature API. Public APIs require a later goal with usage,
  behavior, and verification evidence.
- First-party analyzers remain out of scope. Reconsidering them requires a later goal with concrete
  diagnostics, false-positive policy, versioning rules, and measured maintenance cost.
- General discriminated unions remain out of scope. FunnySharp stays on `net10.0` until .NET 11 is
  generally available and its language/runtime contracts are stable; preview targeting does not
  count as supported release evidence.
- A later goal may reconsider native C# unions only after the .NET 11 SDK and runtime are generally
  available, the language and metadata contracts are stable, and representative FunnySharp usage
  can be compared with the focused `Option`, `Result`, `Validation`, and transition types.
- A `net10.0` union compatibility layer is considered only when real consumers must remain on the
  .NET 10 LTS line while also needing the same general-union source model. Such a proposal must
  include a fixed public API and semantics, source and binary migration to native unions, a removal
  plan, trimming and Native AOT results, allocation and throughput comparisons, and evidence that
  the focused existing types are insufficient. Until every condition is met, no compatibility
  layer or general union API is added.

## Trimming And Native AOT Policy

- Compatibility is evaluated from self-contained consumer applications built from the produced
  packages. The trim and Native AOT analyzers run without suppressions. Trimming roots both shipping
  assemblies so their complete implementations are analyzed; Native AOT compiles and executes
  representative closed generic usages from both packages.
- A successful publish is not sufficient: each produced executable must run representative core
  or ASP.NET Core mappings successfully on the recorded RID.
- `IsTrimmable` and `IsAotCompatible` are package claims, not warning switches. Both shipping
  projects set `IsTrimmable` because full-root trim analysis and representative execution pass.
  They do not set `IsAotCompatible` for the limitation below.
- Full-assembly Native AOT rooting is not a supported claim for this release. With the .NET 10.0.11
  compiler, rooting every member of the open generic `Option`, `Result`, and `Validation` surfaces
  causes artificial, recursively nested `ValueTuple` instantiations that the compiler cannot
  materialize. Representative closed generic consumers are still published and executed, but the
  packages do not set `IsAotCompatible` until a future toolchain can analyze the complete surface or
  the API changes under a separately accepted goal.
- Compatibility evidence is specific to the recorded SDK, runtime patch, RID, and package hashes.
  The required Windows, Linux, and macOS jobs are independent gates; unexercised architectures,
  later runtimes, and unexercised ASP.NET Core features are not implied by a passing result.

Platform references used for this policy:

- [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
- [.NET 11 preview downloads](https://dotnet.microsoft.com/en-us/download/dotnet/11.0)
- [C# union proposal](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/unions.md)
- [C# union language-design tracking issue](https://github.com/dotnet/csharplang/issues/9662)
- [Prepare .NET libraries for trimming](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming)

## Baseline Verification

A release candidate is acceptable only when the complete README verification runner succeeds:

```powershell
pwsh -NoProfile -File eng/Run-Release.ps1 `
  -AttemptId local-full-1 `
  -CompatibilityRuntimeIdentifier win-x64 `
  -CompatibilityPackageFeed https://packagefeedproxy.microsoft.io/nuget/v3/index.json `
  -DistributionFeed https://packagefeedproxy.microsoft.io/nuget/v3/index.json
```

The runner records and verifies each protocol step, requires a clean unchanged source fingerprint,
uses an immutable attempt directory and isolated NuGet cache, and binds Release assemblies, XML
documentation, packages, compatibility consumers, performance receipts, and generated documentation
to that candidate. GitHub exposes four required contexts: `release / win-x64`,
`release / linux-x64`, `release / osx-arm64`, and `release / osx-x64-consumer`.

The evidence must also show that:

- the xUnit v3 test is discovered and passes through Microsoft.Testing.Platform;
- the executable examples compile in the release build and run successfully as usage evidence;
- packing produces `FunnySharp.0.1.0.nupkg`, `FunnySharp.0.1.0.snupkg`,
  `FunnySharp.AspNetCore.0.1.0.nupkg`, and `FunnySharp.AspNetCore.0.1.0.snupkg`;
- each package contains a `lib/net10.0` assembly and the repository README;
- the `FunnySharp` package dependency group for `net10.0` is empty;
- the `FunnySharp.AspNetCore` package has only a `FunnySharp` dependency plus the
  `Microsoft.AspNetCore.App` framework reference; and
- exactly `FunnySharp` and `FunnySharp.AspNetCore` projects are packable.
