# FunnySharp Product Contract

This document is the authoritative contract for the FunnySharp product baseline. Later goals
may extend it deliberately, but implementation convenience alone does not override it. The
next-stage capability decisions that produced this revision are recorded in
[`next-stage/decision-record.md`](next-stage/decision-record.md) and
[`next-stage/api-decisions.md`](next-stage/api-decisions.md); baseline pins, inventories,
analysis, and compile-verified call sites are in [`next-stage/`](next-stage/).

## Canonical Vocabulary

- FunnySharp has exactly four canonical semantic carriers:
  - `Option<T>` — absence of a value; never an error.
  - `Result<TValue, TError>` — fail-fast value-producing work with an explicit typed error.
  - `UnitResult<TError>` — fail-fast work with no meaningful value (delivered by Goal 15, including
  `Sequence`/`Traverse` participation and ASP.NET Core mapping integration).
  - `Validation<TValue, TError>` — independent checks whose errors accumulate deterministically.
- `Effect<T>` and `Effect<TEnvironment, T>` are the deferred-work boundary, not a fifth outcome
  carrier. `StateChange`/`StateTransition`/`StateMachine`/`TransitionResult` are the state
  surface, and `Lens`/`Optional` are the optics surface.
- No second carrier for any of these meanings may be added. `Maybe`, `Either`, `Fin`, `Try`,
  `IO`, `Reader`, `State`, `OptionAsync`, and async outcome wrappers remain out of scope.
- One verb has one meaning. `Map`, `Bind`, `MapError`/`MapErrors`, `Filter`, `Ensure`, `Recover`/
  `RecoverWith`, `OrElse`/`OrElseWith`, `Zip`, `ZipWith`, `Apply`, `Match`, `TryGet*`, and
  `GetValueOr*` keep exactly the meanings recorded in the decision record. `Where` is never added
  to a carrier: a bare predicate cannot produce absence or an error. The verb-by-carrier table in
  [`grammar.md`](grammar.md) is the authoritative verb reference.
- Async naming: `...ValueAsync` marks a callback returning `ValueTask`; `...Async` marks an
  awaitable operation that takes no callback or a `Task`-returning callback. The renamed
  `ComposeValueAsync` removes the only exception. A `CancellationToken` is always an explicit
  parameter and is forwarded unchanged.
- LINQ `Select`/`SelectMany` are a secondary bridge that exists only where `Bind` exists;
  `Validation` never gets them. Documentation presents the member-centric vocabulary first.
- Cardinality access uses the `*OrNone` suffix; keyed lookups use `GetOption`; value conversion
  uses `ToOption`. Eager fallbacks are parameters; lazy fallbacks are factories (`...With`/
  `...Else`); `GetValueOrDefault` is the only member that may return `default(T)`.
- No naming concession, alias, or carrier conversion for any competitor is allowed.
- **Scope rejections that remain in force** (product-scope rejections, not deferrals): no custom
  runtime or scheduler, no replacement collection universe, no pervasive immutability, no
  higher-kinded-type, typeclass, or monad-transformer hierarchy, and no premature general
  discriminated-union system.

## Stability Boundary

- **Stable** members are public, XML-documented, covered by semantic tests, included in a
  committed public-API baseline, and benchmark-characterized (or explicitly excluded with
  rationale) where performance-relevant. Stable members change only through a later accepted
  goal.
- **Experimental** capabilities carry `System.Diagnostics.CodeAnalysis.ExperimentalAttribute`
  with a documented diagnostic ID, an entry in a tracked stability inventory, no compatibility
  promise, and removal without a breaking-change process. The next stage begins with no
  experimental members in the 0.1.0 surface; new uncertain capabilities (for example Goal 17's
  traversal-location API) enter as experimental unless their goal produces full stable evidence.
- **Removal** requires an accepted decision row and a migration note in the same goal. Nothing
  from 0.1.0 is removed by the next-stage decisions; removal is reserved for shapes superseded
  by the recorded redesigns.
- `EnablePackageValidation` plus a committed API baseline configures the boundary; the next
  release also carries release notes and versioning rules. A release candidate that changes the
  baseline without an accepted goal is invalid.

## Product Direction

- FunnySharp is a pragmatic functional-programming library for idiomatic C# targeting .NET 10.
- APIs are BCL-first and prefer standard delegates, collections, `Task`, `ValueTask`, and
  `CancellationToken` over a parallel runtime or type universe.
- Synchronous and asynchronous APIs stay consistent where both forms are meaningful. Async APIs
  must preserve cancellation and exception behavior and must never use sync-over-async. There is
  no task-carrier operator universe: mixed sync/async chains use ordinary `await` plus the
  synchronous vocabulary.
- Performance claims require measurements against equivalent direct C# or BCL code. Hot paths
  should avoid hidden allocation, repeated enumeration, reflection, and unnecessary buffering.
  Every stable operation has documented complexity, allocation, enumeration, materialization,
  buffering, and scheduling characteristics; allocation budgets remain blocking while hosted
  timing stays directional until a fixed-hardware runner exists.
- `StateTransition.Then` composition must be resolved to single-materialization composition with
  acceptable measured cost, or excluded from the stable contract; the recorded 0.1.0
  left-associated chain behavior (85x–457x, up to 187 KB at Count=256) cannot remain stable
  unchanged (Goal 20).
- Immutable data is opt-in. The core package does not impose immutable collections or copying on
  consumers that do not ask for them.
- Immutable updates use a deliberately small `Lens<TSource, TFocus>` and
  `Optional<TSource, TFocus>` surface over caller-provided delegates. The core provides no optics
  hierarchy, traversal API, reflection, property-path API, persistent-collection ecosystem, or
  hidden source copying. Lens laws and purity remain obligations of the caller-provided
  delegates. Lenses are for paths updated in more than one place; a single nested `with`
  expression is the honest baseline.
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
  `Choose`, `Sequence`, `Traverse`, the bounded parallel operators, the `*OrNone` cardinality
  family, `Partition`, `Scan`, and container/`IParsable` option bridges are the adopted additions;
  they stay on BCL carriers.
- Collection traversal failures must be able to retain compositional location context (index,
  key, item name, property path) for failures such as `customers[17].addresses[2].postalCode`.
  The location carrier is small, value-based, and must not become a collection or error hierarchy.
- Default and uninitialized values are explicit contracts:
  - `Option<T>` default is a valid `None`.
  - `Result<TValue, TError>`, `UnitResult<TError>`, and `Validation<TValue, TError>` defaults
    must not masquerade as legitimate domain outcomes; uninitialized access is a programming
    error with an explicit diagnostic, and Goal 15 owns the mechanism.
  - `TransitionResult<...>` default is `Undefined`, a meaningful machine status.
  - Uninitialized `Effect` and optics values fail explicitly when used.
- Effects are thin `readonly struct` wrappers over standard delegates. They defer execution until
  `RunAsync`, return `ValueTask`, and make an optional caller-owned environment, cancellation, and
  resource lifetime explicit without changing normal .NET exception or cancellation semantics.
  `Result<TValue, TError>` remains an explicit value rather than an implicit effect failure.
  `Effect` is used when deferred execution, environment, cancellation, or resource lifetime pay
  for the wrapper; a single synchronous call stays direct C#.
- A general retry, backoff, or scheduling policy layer is not part of the stable surface for this
  stage. Coordinator-owned timeouts (as in the first-success family) remain. Reconsideration
  requires a goal with `TimeProvider`, explicit cancellation, cap/jitter rules, and measured
  comparison against a hand-written loop.
- Concurrency remains BCL-first and explicit. Bounded parallel mapping streams
  `IAsyncEnumerable<T>` results with `Channel` backpressure and linked operation cancellation in
  two named delivery orders: `SelectParallelValueAsync` yields source order and
  `SelectParallelCompletionOrderValueAsync` yields completion order; the delivery order is part
  of the method name, never a boolean or enum parameter. Parallel traversal materializes ordered
  values and distinguishes Option/Result fail-fast behavior from Validation accumulation.
  First-success coordination accepts only cold `Effect<Result<TValue, TError>>` values, drains
  all started work, uses typed failures only for explicit `Result` failures, and supports
  cooperative `TimeProvider` timeouts. Its return shape stays `Validation<TValue, TError>`: a
  winner is `Valid`, and an all-typed-failure race is `Invalid` with the failures in input
  order; that race contract is documented explicitly at the method and in the concurrency guide.
  The core provides no naked started-Task racing API, unbounded fan-out, scheduler, fiber runtime,
  or alternative concurrency carrier. Completion-order coordination is limited to
  `IAsyncEnumerable<T>` and cold effects. Goal 20 comparisons against pinned competitor packages
  run only in an isolated, non-shipping benchmark project; those results are performance evidence
  and never API-compatibility or Goal 22 acceptance evidence.
- State remains pure. `StateChange`, `StateTransition`, and `StateMachine` model next state,
  emitted commands, rejection, failure, and undefined handling without executing effects; callers
  own persistence, event storage, and command execution. No workflow engine, actor system, STM,
  or distributed orchestrator is added.
- HTTP integration stays a separate package with explicit caller-selected mapping. There is no
  DI registration, middleware, global error policy, or exception handler, and domain code stays
  HTTP-free. First-party typed-results/OpenAPI APIs are deferred; the vertical slice documents and
  tests `.Produces*` usage instead.
- Serialization of carriers is caller policy today. No `System.Text.Json` converter ships until a
  goal fixes the wire contract and verifies trimming and Native AOT behavior; callers use DTOs,
  and `JsonSerializerOptions.Strict` is the recommended executable form of a DTO-only policy.

## Analyzers And Compiler Feedback

- Analyzers ship inside the `FunnySharp` package under `analyzers/dotnet/cs` (the Funcky model),
  as build assets with no runtime dependency and no separate package to install. The original
  deferral is deliberately superseded.
- The reopening conditions from the original contract remain deliverable requirements: concrete
  diagnostics, a false-positive policy, versioning rules, and measured maintenance cost.
- Diagnostics must be suppressible by default except for impossible states, use stable ASCII IDs,
  and document severity behavior. Safe code fixes are provided only where a transformation is
  unambiguous.
- Analyzer coverage must at least address uninitialized/default semantic carriers, silently
  discarded outcomes, `TryGetValue`-style exhaustion where it hides the canonical path, and
  async/cancellation/enumeration/resource hazards that can be diagnosed with low false-positive
  risk. Analyzer tooling adds no runtime dependency to shipping packages.

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
- FunnySharp remains independent of competing functional libraries: no competitor dependency,
  carrier conversion, compatibility package, naming concession, or migration promise. Comparison
  tooling may reference pinned competitor packages only in an isolated, non-packable,
  non-shipping evidence project that is never part of `FunnySharp.slnx` or any release artifact.

## Deliberate Deferrals

- Public APIs require a later goal with usage, behavior, and verification evidence; no
  speculative feature API is added.
- General discriminated unions remain out of scope. FunnySharp stays on `net10.0` until .NET 11
  is generally available and its language/runtime contracts are stable; preview targeting does
  not count as supported release evidence.
- A later goal may reconsider native C# unions only after the .NET 11 SDK and runtime are generally
  available, the language and metadata contracts are stable, and representative FunnySharp usage
  can be compared with the focused `Option`, `Result`, `UnitResult`, and `Validation` types.
- A `net10.0` union compatibility layer is considered only when real consumers must remain on the
  .NET 10 LTS line while also needing the same general-union source model. Such a proposal must
  include a fixed public API and semantics, source and binary migration to native unions, a removal
  plan, trimming and Native AOT results, allocation and throughput comparisons, and evidence that
  the focused existing types are insufficient. Until every condition is met, no compatibility
  layer or general union API is added.
- Deferred capabilities stay absent from the stable surface: a general retry/backoff layer,
  `Memoize`/async-buffer carriers, k-way async merge, completion-order operators beyond the
  recorded constrained form, typed-results/OpenAPI APIs, System.Text.Json converters,
  `Either` as a non-error carrier, Option comparers/ordering, awaitable
  `Option<Task<T>>` sugar, and `net11.0` targeting. The non-empty guarantee
  (`NonEmpty<T>`) and exact-versus-truncating zip (`ZipExact`/`ZipExactOrNone`) left this
  list when Goal 17 adopted them as stable after proving their safety value (decision E72's
  adoption condition); the current tier assignment of every capability is the
  [stability inventory](stability-inventory.md). A public `Unit` type is rejected, not
  deferred: no-value outcomes use `UnitResult<TError>`, and no-value work uses ordinary
  `void`/`Task`/`ValueTask` shapes.
- Each deferred item has recorded triggers in the decision record; adopting one requires a goal
  that satisfies the trigger and the stability boundary above.

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
