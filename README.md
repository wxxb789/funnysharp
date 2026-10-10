# FunnySharp: Pragmatic, BCL-First Functional Programming for C# and .NET 10

[![NuGet FunnySharp](https://img.shields.io/nuget/v/FunnySharp.svg?style=flat-square&label=FunnySharp)](https://www.nuget.org/packages/FunnySharp) [![NuGet FunnySharp.AspNetCore](https://img.shields.io/nuget/v/FunnySharp.AspNetCore.svg?style=flat-square&label=FunnySharp.AspNetCore)](https://www.nuget.org/packages/FunnySharp.AspNetCore) [![Target Framework](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)

[English](README.md) | [简体中文](README_zh-cn.md)

![FunnySharp overview: presence via Option, fail-fast via Result and UnitResult, deterministic error accumulation via Validation, and BCL-first design for .NET 10](docs/assets/funnysharp-overview.webp)

FunnySharp is a pragmatic, BCL-first C# functional programming library for C# 13 and .NET 10.
Its Option/Result pattern supports typed error handling and railway-oriented programming on
standard .NET values, delegates, `Task`, `ValueTask`, and `IAsyncEnumerable<T>`. The core package
has zero runtime dependencies and bundles Roslyn analyzers; `FunnySharp.AspNetCore` adds optional
ASP.NET Core Minimal API result mapping.

## Installation

```shell
dotnet add package FunnySharp              # Carriers, grammar, pipelines, analyzers (zero runtime dependencies)
dotnet add package FunnySharp.AspNetCore   # Optional Minimal API HTTP result mapping
```

## Canonical Semantic Carriers

Choose a carrier by the meaning of the outcome. Shared verbs such as `Map`, `Bind`, `Ensure`, and `Match` apply where their semantics are valid; `Validation` deliberately has no `Bind`.

| Carrier | Use it for | Behavior |
|---|---|---|
| `Option<T>` | A value that may be absent | `Some` or `None`; absence is not an error |
| `Result<TValue, TError>` | Typed error handling for value-producing work | Stops at the first failure |
| `UnitResult<TError>` | Commands with no successful payload | Stops at the first failure |
| `Validation<TValue, TError>` | Independent checks and batch input validation | Accumulates errors in deterministic order |

`Option<T>` defaults to a valid `None`. The other three defaults are uninitialized, not domain outcomes: state inspection throws `InvalidOperationException`, while `ToString()` returns diagnostic text. `FS1001` diagnoses common default-construction syntax, not every possible uninitialized value.

`Effect<T>` and `Effect<TEnvironment, T>` serve as the deferred `ValueTask` execution boundary, not a fifth outcome carrier.

## Examples

### 1. Fail-Fast Workflow with Option and Result

```csharp
using System;
using FunnySharp;

Option<string> sku = Option.Some("SKU-42");

Result<int, string> skuLength = sku
    .Map(static text => text.Length)
    .ToResult("The SKU is absent.")
    .Ensure(static length => length > 0, "The SKU is empty.");

string summary = skuLength.Match(
    success: static len => $"Valid SKU length: {len}",
    failure: static err => $"Validation failed: {err}");

Console.WriteLine(summary);
```

### 2. Independent Multi-Error Validation

```csharp
using System;
using FunnySharp;

var nameValidation = Validation<string, string>.Valid("Ada");
var ageValidation = Validation<int, string>.Invalid("Age must be at least 18.");
var codeValidation = Validation<string, string>.Invalid("Postal code is required.");

Validation<(string Name, int Age, string Code), string> registration =
    nameValidation.Zip(
        ageValidation,
        codeValidation,
        static (name, age, code) => (name, age, code));

registration.Match(
    valid: static user => Console.WriteLine($"Registered: {user.Name}"),
    invalid: static errors => Console.WriteLine($"Errors ({errors.Count}): {string.Join(", ", errors)}"));
```

## Core Capabilities

- **Built-in Roslyn Analyzers**: Embedded in core (`FS1001` uninitialized carriers, `FS1002` discarded outcomes, `FS1003` ignored `TryGet*` booleans, `FS1004` blocked `ValueTask`s, `FS1005` sync-disposed `IAsyncDisposable`).
- **Function Composition**: Standard delegate helpers (`Pipe`, `Compose`, `Curry`, `Tap`) plus async forms (`ComposeValueAsync`, `TapValueAsync`).
- **Collections & Traversal**: Safe cardinality access (`FirstOrNone`, `SingleOrNone`), `NonEmpty<T>`, one-pass `Partition`, exact-length `ZipExact`, and carrier-aware `Sequence`/`Traverse`.
- **Streaming & Span Pipelines**: Fused `Choose` and running `Scan` on `IEnumerable<T>` and `IAsyncEnumerable<T>`, plus caller-buffered `ChooseTo`/`WhereTo` on `Span<T>`.
- **Bounded Concurrency**: Source-ordered (`SelectParallelValueAsync`) or completion-ordered (`SelectParallelCompletionOrderValueAsync`) mapping, and `FirstSuccessAsync` over cold effects.
- **Pure State Machines & Optics**: Pure `StateTransition` workflows with typed commands, and composable `Lens`/`Optional` for immutable nested updates.
- **ASP.NET Core Minimal APIs**: Maps carriers and effects to caller-selected `IResult` and RFC 7807/9457 `ProblemDetails` via `FunnySharp.AspNetCore`.

## Documentation & Examples

| Architecture & Specifications | Carrier & Language Guides | Systems & Integration |
|---|---|---|
| [Product Contract](https://github.com/wxxb789/funnysharp/blob/main/docs/product-contract.md) | [Option Guide](https://github.com/wxxb789/funnysharp/blob/main/docs/option.md) | [Data Pipelines](https://github.com/wxxb789/funnysharp/blob/main/docs/data-pipelines.md) |
| [Grammar Reference](https://github.com/wxxb789/funnysharp/blob/main/docs/grammar.md) | [Result Guide](https://github.com/wxxb789/funnysharp/blob/main/docs/result.md) | [Concurrency Guide](https://github.com/wxxb789/funnysharp/blob/main/docs/concurrency.md) |
| [Performance Guidance](https://github.com/wxxb789/funnysharp/blob/main/docs/performance.md) | [UnitResult Guide](https://github.com/wxxb789/funnysharp/blob/main/docs/unit-result.md) | [Effects & Resources](https://github.com/wxxb789/funnysharp/blob/main/docs/effects.md) |
| [Release Readiness](https://github.com/wxxb789/funnysharp/blob/main/docs/release-readiness.md) | [Validation Guide](https://github.com/wxxb789/funnysharp/blob/main/docs/validation.md) | [State Machines](https://github.com/wxxb789/funnysharp/blob/main/docs/state-machines.md) |
| [Versioning Policy](https://github.com/wxxb789/funnysharp/blob/main/docs/versioning.md) | [Function Composition](https://github.com/wxxb789/funnysharp/blob/main/docs/function-composition.md) | [Immutable Updates](https://github.com/wxxb789/funnysharp/blob/main/docs/immutable-updates.md) |
| [Quick Start](https://github.com/wxxb789/funnysharp/blob/main/docs/quick-start.md) | [Roslyn Analyzers](https://github.com/wxxb789/funnysharp/blob/main/docs/analyzers.md) | [ASP.NET Core](https://github.com/wxxb789/funnysharp/blob/main/docs/aspnet-core.md) |
| [Release Notes](https://github.com/wxxb789/funnysharp/blob/main/docs/release-notes.md) | [Collections & Traversal](https://github.com/wxxb789/funnysharp/blob/main/docs/collections.md) | [Contributor Harness](https://github.com/wxxb789/funnysharp/blob/main/docs/harness.md) |

Executable samples: [FunnySharp Core Examples](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.Examples/Program.cs) and [ASP.NET Core Examples](https://github.com/wxxb789/funnysharp/blob/main/examples/FunnySharp.AspNetCore.Examples/Program.cs).

## Performance, Compatibility & Verification

- **Evidence-Based Performance**: Operation bounds and allocation budgets (`eng/performance/baseline.json`) block release regressions; hosted timing is directional.
- **Trimming & AOT**: Both packages set `IsTrimmable=true` with full-rooting verification. Due to open generic `ValueTuple` compiler limits in .NET 10, packages do not set `IsAotCompatible`; Native AOT is validated on representative closed generic usages.
- **Versioning**: `0.x` releases are preview candidates; minor versions may break callers under the documented versioning rules. `1.0.0` begins the major-version compatibility promise.
- **Contributor Tooling**: Requires .NET SDK `10.0.400` (`rollForward: latestPatch`). Verification runs through the F# harness:

```bash
dotnet fsi build.fsx -- -p build                 # Build solution FunnySharp.slnx
dotnet fsi build.fsx -- -p test                  # Execute xUnit v3 test suites
dotnet fsi build.fsx -- -p verify-docs-snippets  # Byte-exact documentation snippet verification
dotnet fsi build.fsx -- -p verify-tooling        # Contributor pre-check (locked restore, build, test, snippets)
```

See [release readiness](https://github.com/wxxb789/funnysharp/blob/main/docs/release-readiness.md) for the authoritative full release verification invocation and multi-platform criteria.

## License

FunnySharp is licensed under the [MIT License](LICENSE).
