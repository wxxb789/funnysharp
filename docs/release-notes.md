# Release Notes

This page is the release history and the supported-runtime statement for the FunnySharp preview
candidate. The [product contract](product-contract.md) names it from the Stability Boundary
section; the versioning rules and the committed public-API baseline that gate every release are in
[versioning](versioning.md).

## Supported Runtimes

| Item | Statement | Verified by |
| --- | --- | --- |
| Target framework | `net10.0` only; `net11.0` waits for general availability | `Directory.Build.props`, `global.json` |
| SDK | `10.0.400` feature band with same-band patch roll-forward | `global.json` |
| Runtime | `Microsoft.NETCore.App` 10.0.x and `Microsoft.AspNetCore.App` 10.0.x | release-run environment capture |
| Package consumers | the packed packages build, run, and publish in consumer projects that reference the `.nupkg` files rather than the projects | `tests/FunnySharp.Compatibility` scenarios through the `compatibility` pipeline |
| Release-gate platforms | `win-x64`, `linux-x64`, `osx-arm64`, and a bounded `osx-x64` consumer smoke | `.github/workflows/release.yml` contexts |
| Trimming | supported: both packages declare `IsTrimmable=true` and a full-mode trimmed consumer publish runs clean | `CoreTrimmed` and `AspNetCoreTrimmed` scenarios |
| Native AOT | a `PublishAot=true` consumer publish runs clean; the packages make no `IsAotCompatible` claim | `CoreNativeAot` and `AspNetCoreNativeAot` scenarios |
| Dependencies | BCL-first: the core package declares no runtime package dependency and embeds its analyzers as build assets; the ASP.NET Core package depends only on the matching `FunnySharp` package and uses the `Microsoft.AspNetCore.App` shared framework | `.nuspec` inspection of the packed packages |

## 0.1.0 (preview)

The first release-quality preview candidate. Every surface below is stable, XML-documented, covered
by semantic xUnit v3 tests, and benchmark-characterized or explicitly excluded where
performance-relevant.

- **Carriers** - `Option<T>` (presence/absence), `Result<TValue, TError>` (fail-fast typed
  failure), `UnitResult<TError>` (value-less outcome), `Validation<TValue, TError>` (accumulating
  validation). See [option](option.md), [result](result.md), [unit-result](unit-result.md),
  [validation](validation.md).
- **One grammar** - `Map`, `Bind`, `Ensure`, `Filter`, `MapError`, `Recover`, `OrElse`, `Match`,
  `TryGetValue`, and `GetValueOr*` mean the same thing on every carrier where they are valid. See
  [grammar](grammar.md) and [function composition](function-composition.md).
- **Collections and traversal** - `*OrNone` cardinality access, `NonEmpty<T>`, one-pass
  `Partition`, exact zip, container and parse bridges, and traversal that carries indexed, keyed,
  and compositional location context. See [collections](collections.md).
- **Pipelines and streaming** - `Choose`, `WhereNotNull`, and `Scan` for synchronous,
  asynchronous, and span/memory pipelines over standard `IEnumerable` and `IAsyncEnumerable`
  carriers. See [data pipelines](data-pipelines.md).
- **Concurrency** - bounded parallel mapping in source or completion order, parallel
  `IAsyncEnumerable` traversal, and first-success selection over cold
  `Effect<Result<TValue, TError>>` values, on standard cancellation and `TimeProvider`. See
  [concurrency](concurrency.md).
- **Effects and resources** - `Effect<T>` and `Effect<TEnvironment, T>` as a deferred `ValueTask`
  boundary with `Using`/`UsingAsync` resource scoping and an explicit environment, and no effect
  runtime. See [effects](effects.md).
- **State and immutable updates** - pure state transitions and finite-state machines with emitted
  outputs, invalid events, and replay; `Lens`/`Optional` for total and possibly missing nested
  updates over records and BCL immutable collections. See [state machines](state-machines.md) and
  [immutable updates](immutable-updates.md).
- **Compiler feedback** - analyzers ship inside the core package under `analyzers/dotnet/cs` and
  report `FS1001`-`FS1005` for uninitialized carriers, discarded outcomes, ignored `TryGet*`
  results, blocked `ValueTask`s, and synchronous disposal of async-disposable resources. See
  [analyzers](analyzers.md).
- **ASP.NET Core** - `FunnySharp.AspNetCore` maps carriers and effects to caller-selected `IResult`
  and RFC-compatible `ProblemDetails`. See [ASP.NET Core](aspnet-core.md).
- **Measured performance** - every performance-relevant operation has a documented cost and a
  measured comparison against raw BCL or idiomatic C#, with allocation budgets that block a
  release. See [performance](performance.md).

Deliberately absent from `0.1.0`, and unchanged by it: general discriminated unions, a second
absence carrier, a `Where` member on carriers, a retry/backoff policy layer, System.Text.Json
converters, typed-results/OpenAPI APIs, `net11.0` targeting, and any competitor compatibility
surface. See the [product contract](product-contract.md) for the source of each boundary.
