# FunnySharp: Pragmatic, BCL-First Functional Programming for C# and .NET 10

[![NuGet FunnySharp](https://img.shields.io/nuget/v/FunnySharp.svg?style=flat-square&label=FunnySharp)](https://www.nuget.org/packages/FunnySharp)
[![NuGet FunnySharp.AspNetCore](https://img.shields.io/nuget/v/FunnySharp.AspNetCore.svg?style=flat-square&label=FunnySharp.AspNetCore)](https://www.nuget.org/packages/FunnySharp.AspNetCore)
[![Target Framework](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)
[![Zero Runtime Dependencies](https://img.shields.io/badge/dependencies-0-brightgreen.svg?style=flat-square)](#design-principles)

[English](README.md) | [简体中文](README_zh-cn.md)

**FunnySharp** is a modern, high-performance, BCL-first functional programming library engineered specifically for C# 13 and .NET 10. It delivers zero-allocation semantic carriers (`Option<T>`, `Result<TValue, TError>`, `UnitResult<TError>`, and `Validation<TValue, TError>`), Railway-Oriented Programming (ROP), built-in Roslyn analyzers, function composition, data streaming pipelines, state machines, and seamless ASP.NET Core Minimal API integration—without forcing a foreign type hierarchy, custom runtime, or third-party dependencies onto your codebase.

---

## Table of Contents

- [Why FunnySharp?](#why-funnysharp)
- [Key Features](#key-features)
- [Packages & Installation](#packages--installation)
- [Quick Start & Code Examples](#quick-start--code-examples)
  - [Option: Safe Nullability & Absence Handling](#1-optiont--safe-nullability--absence-handling)
  - [Result & UnitResult: Railway-Oriented Programming](#2-result-and-unitresult--railway-oriented-programming)
  - [Validation: Applicative Error Accumulation](#3-validation--applicative-error-accumulation)
  - [Built-In Roslyn Analyzers: Compile-Time Safety](#4-built-in-roslyn-analyzers-fs1001fs1005)
  - [Function Composition & Pipelines](#5-function-composition--pipelines)
  - [Data Pipelines: Fused Operations & Streaming](#6-data-pipelines-choose-scan-partition)
  - [ASP.NET Core Minimal API Integration](#7-aspnet-core-minimal-api-integration)
- [Core Semantic Carriers at a Glance](#core-semantic-carriers-at-a-glance)
- [Predictable Grammar & Verbs](#predictable-grammar--verbs)
- [Zero-Dependency & Allocation-Conscious Performance](#zero-dependency--allocation-conscious-performance)
- [Verification & Build](#verification--build)
- [Documentation & Deep Dives](#documentation--deep-dives)
- [License](#license)

---

## Why FunnySharp?

In functional C#, developers often have to choose between two extremes: heavy monad frameworks that invent their own runtime and collection universes (leading to steep learning curves and allocation overhead), or ad-hoc custom `Result` classes scattered across projects.

FunnySharp takes a **BCL-first pragmatic approach**:

1. **Native to .NET 10 & C# 13**: Built directly on BCL primitives (`ValueTask`, `IAsyncEnumerable<T>`, `ReadOnlySpan<T>`, `CancellationToken`, standard delegates). No custom task schedulers, no wrapper-over-wrapper monad towers.
2. **Zero Runtime Dependencies**: The core package references nothing outside the standard .NET runtime.
3. **Guardrails Built-in**: Ships compile-time Roslyn analyzers directly inside the core NuGet package. Misuses like uninitialized default structs, ignored results, or sync-over-async blocking on `ValueTask` are caught right in your IDE.
4. **Allocation-Conscious & Predictable**: Critical operations carry strict zero-allocation budgets and benchmark baselines verified against raw handwritten BCL and idiomatic LINQ.
5. **Orthogonal Grammar**: One verb has exactly one meaning across all carriers (`Map`, `Bind`, `Ensure`, `Match`, `Recover`, `Filter`).

---

## Key Features

- 🛡️ **Four Canonical Carriers**:
  - `Option<T>`: Strict absence handling without null reference exceptions (`NullReferenceException`).
  - `Result<TValue, TError>`: Fail-fast outcome for value-producing domain workflows.
  - `UnitResult<TError>`: Lightweight command/mutation outcome without dummy payloads or void compromises.
  - `Validation<TValue, TError>`: Applicative multi-error accumulation with deterministic ordering for forms, DTOs, and batch checks.
- 🚦 **Railway-Oriented Programming (ROP)**: Fluent method chaining (`Map`, `Bind`, `Ensure`, `Recover`, `OrElse`, `Tap`) with full `Task` and `ValueTask` async support.
- 🔍 **First-Class Roslyn Analyzers (`FS1001`–`FS1005`)**: Enforces correct carrier initialization, prevents discarded outcomes, flags unhandled Try patterns, and warns against blocked `ValueTask`s.
- ⚡ **High-Performance Streaming**: Fused `Choose` (filter-map in a single pass), `Scan` (running accumulators), and `Partition` for `IEnumerable<T>`, `IAsyncEnumerable<T>`, and `ReadOnlySpan<T>`.
- 🌐 **ASP.NET Core Minimal API Adapter**: Effortlessly project domain `Result` and `Validation` outcomes directly into typed `IResult` or RFC 7807 / RFC 9457 `ProblemDetails`.
- ⚙️ **Pure State Machines & Effects**: Deterministic event-driven state transitions with pure output events, separate from side effects (`Effect<T>`, `Effect<TEnvironment, T>`).
- 🧩 **Optics & Immutable Updates**: Composable `Lens<TSource, TFocus>` and `Optional<TSource, TFocus>` working seamlessly with C# record `with` expressions.

---

## Packages & Installation

Install via the .NET CLI or NuGet Package Manager:

### Core Package (Carriers, Grammar, Analyzers, Pipelines)
```shell
dotnet add package FunnySharp
```

### ASP.NET Core Integration (Minimal API Result & ProblemDetails Mapping)
```shell
dotnet add package FunnySharp.AspNetCore
```

*Requirements: .NET 10.0 SDK (`net10.0` target).*

---

## Quick Start & Code Examples

### 1. `Option<T>`: Safe Nullability & Absence Handling

Say goodbye to `null` ambiguity and `NullReferenceException`. `Option<T>` clearly expresses presence (`Some`) or absence (`None`).

```csharp
using FunnySharp;

// Construction from values or standard Try patterns
Option<string> name = Option.Some("Alice");
Option<string> empty = Option.None;
Option<int> parsed = int.TryParse("128", out var val) ? Option.Some(val) : Option.None;

// Transform, filter, and extract safely
string greeting = name
    .Map(n => n.ToUpperInvariant())
    .Filter(n => n.StartsWith("A"))
    .Match(
        some: n => $"Hello, {n}!",
        none: () => "Hello, Guest!"
    );

// Fallback operations
string displayName = empty.GetValueOr("Anonymous");
string lazyName = empty.GetValueOrElse(() => ComputeFallbackName());
```

### 2. `Result` and `UnitResult`: Railway-Oriented Programming

Chain operations safely with automatic early exit on error (fail-fast Railway-Oriented Programming):

```csharp
using FunnySharp;

public record User(int Id, string Email, bool IsActive);
public record UserError(string Code, string Message);

public Result<User, UserError> ProcessUser(string rawEmail)
{
    return ValidateEmail(rawEmail)
        .Ensure(email => !email.EndsWith("@blocked.com"), new UserError("BlockedDomain", "Domain not permitted."))
        .Bind(FindUserByEmail)
        .Tap(user => LogAudit(user.Id));
}

// UnitResult for commands without return values
public UnitResult<UserError> DeactivateAccount(int userId)
{
    return FindUser(userId)
        .ToUnitResult()
        .Ensure(() => CheckPermission(userId), new UserError("Forbidden", "Permission denied."))
        .Bind(() => DeleteSession(userId));
}
```

### 3. `Validation`: Applicative Error Accumulation

Unlike `Result` which short-circuits on the first error, `Validation<TValue, TError>` collects all validation failures across independent rules—ideal for request models, inputs, and form validation:

```csharp
using FunnySharp;

public record RegisterRequest(string Username, string Password, int Age);

public Validation<RegisterRequest, string> ValidateRegistration(RegisterRequest req)
{
    var validUser = req.Username.Length >= 3
        ? Validation<string, string>.Valid(req.Username)
        : Validation<string, string>.Invalid("Username must be at least 3 characters.");

    var validPass = req.Password.Length >= 8
        ? Validation<string, string>.Valid(req.Password)
        : Validation<string, string>.Invalid("Password must be at least 8 characters.");

    var validAge = req.Age >= 18
        ? Validation<int, string>.Valid(req.Age)
        : Validation<int, string>.Invalid("Must be at least 18 years old.");

    // Combine all checks; all failures accumulate in deterministic order
    return validUser.ZipWith(validPass, validAge, (u, p, a) => new RegisterRequest(u, p, a));
}
```

### 4. Built-in Roslyn Analyzers (`FS1001`–`FS1005`)

FunnySharp includes Roslyn analyzers directly in the NuGet package with zero additional setup. Your IDE and CI will catch common functional programming hazards immediately:

| Diagnostic ID | Severity | Purpose | Why It Matters |
|:---:|:---:|---|---|
| **FS1001** | **Error** | Prohibits creating carriers via `default` or `new()` | Semantic carriers require deliberate state (`Some`/`None`, `Success`/`Failure`). |
| **FS1002** | **Warning** | Flags unhandled/discarded outcomes | Prevents silently ignoring errors and failures in Railway pipelines. |
| **FS1003** | **Warning** | Requires checking the Boolean return of `TryGet*` methods | Prevents consuming unverified extracted values. |
| **FS1004** | **Warning** | Forbids synchronous blocking (`.Result`, `.Wait()`) on FunnySharp `ValueTask`s | Prevents thread-pool starvation and sync-over-async deadlocks. |
| **FS1005** | **Warning** | Flags synchronous disposal of `IAsyncDisposable` resources | Guarantees proper asynchronous teardown. |

### 5. Function Composition & Pipelines

Write expressive, readable, and composable functional C# pipelines with native delegate extensions:

```csharp
using FunnySharp;

Func<string, string> trim = s => s.Trim();
Func<string, string> lowercase = s => s.ToLowerInvariant();
Func<string, string> sanitize = trim.Compose(lowercase);

// Pipe value forward
var result = "  Hello FunnySharp  "
    .Pipe(sanitize)
    .Pipe(s => $"[Clean: {s}]");
// Output: "[Clean: hello funnysharp]"

// Currying and partial application
Func<int, int, int> add = (a, b) => a + b;
var addFive = add.Curry()(5);
int fifteen = addFive(10); // 15
```

### 6. Data Pipelines: `Choose`, `Scan`, `Partition`

Extend standard LINQ with fused functional stream operators for `IEnumerable<T>`, `IAsyncEnumerable<T>`, and spans:

```csharp
using FunnySharp;

var inputs = new[] { "10", "abc", "25", "40", "invalid" };

// Fused filter-map in a single pass (zero intermediate allocation)
IEnumerable<int> numbers = inputs.Choose(s => int.TryParse(s, out var n) ? Option.Some(n) : Option.None);
// Results: 10, 25, 40

// Single-pass partition into matches and non-matches
var (adults, minors) = users.Partition(u => u.Age >= 18);

// Running cumulative aggregation
IEnumerable<int> runningTotal = numbers.Scan(0, (acc, n) => acc + n);
// Results: 0, 10, 35, 75
```

### 7. ASP.NET Core Minimal API Integration

Map domain outcomes to HTTP responses seamlessly with `FunnySharp.AspNetCore`. Convert `Result`, `Validation`, and `Effect` into typed `IResult` and RFC-compliant `ProblemDetails` with clean status codes:

```csharp
using FunnySharp;
using FunnySharp.AspNetCore;

var app = WebApplication.Create();

app.MapGet("/users/{id:int}", (int id, IUserService service) =>
{
    return service.FindUser(id)
        .ToHttpResult(
            onSuccess: user => Results.Ok(user),
            onNone: () => Results.NotFound($"User {id} not found")
        );
});

app.MapPost("/orders", (CreateOrderDto dto, IOrderService service) =>
{
    return service.CreateOrder(dto)
        .ToHttpResult(
            onSuccess: order => Results.Created($"/orders/{order.Id}", order),
            onFailure: error => Results.BadRequest(new ProblemDetails
            {
                Title = "Order Creation Failed",
                Detail = error.Message,
                Status = StatusCodes.Status400BadRequest
            })
        );
});
```

---

## Core Semantic Carriers at a Glance

| Carrier Type | Representation | Fail-Fast / Accumulating | Default Value Contract | Primary Use Case |
|---|---|:---:|:---:|---|
| **`Option<T>`** | Presence (`Some`) or absence (`None`) | Fail-fast | Valid `None` | Replace `null`, express missing query results, dictionary lookups, parsing. |
| **`Result<TValue, TError>`** | Typed success or typed failure | Fail-fast | Uninitialized (FS1001 error) | Domain workflows, business logic operations, fallible calculations. |
| **`UnitResult<TError>`** | Typed command success or failure | Fail-fast | Uninitialized (FS1001 error) | Mutations, updates, deletes, notifications with no return value. |
| **`Validation<TValue, TError>`** | Valid value or accumulated errors | Accumulating | Uninitialized (FS1001 error) | Form verification, multi-rule business validation, batch data ingestion. |
| **`Effect<T>` / `Effect<TEnv, T>`** | Deferred computation with environment | Deferred execution | Uninitialized | Cold asynchronous workflows, scoped dependency and resource management. |

---

## Predictable Grammar & Verbs

FunnySharp uses a single orthogonal grammar across all carriers. If you know the verb, you know the behavior:

```
          ┌──────────────────────────────────────────────────────────────┐
          │                      FunnySharp Grammar                      │
          ├───────────────┬──────────────────────────────────────────────┤
          │ Map           │ Transform value within the carrier           │
          │ Bind          │ Chain function returning the same carrier    │
          │ Ensure        │ Validate condition or switch to error        │
          │ Match         │ Exhaustive terminal extraction               │
          │ Recover       │ Fallback from failure to a success value     │
          │ RecoverWith   │ Fallback from failure to another carrier     │
          │ Filter        │ Keep value if predicate holds, else None     │
          │ Tap           │ Observe value or error for side-effects      │
          │ Zip / ZipWith │ Combine multiple independent carriers        │
          └───────────────┴──────────────────────────────────────────────┘
```

---

## Zero-Dependency & Allocation-Conscious Performance

FunnySharp is designed from day one with high-throughput cloud services in mind:

- **Strict Allocation Budgets**: Verified by BenchmarkDotNet in continuous integration. Zero-allocation paths are defended with regression tests.
- **No Sync-Over-Async**: Async pipelines preserve cancellation and exception semantics without blocking or deadlocks.
- **Trimmable & Native AOT Friendly**: Verified against Native AOT trimming requirements (`<IsTrimmable>true</IsTrimmable>`).
- **No Reflection**: All pipeline and carrier transformations rely on statically-typed delegates and inlinable structs.

Read our complete [Performance Policy & Measurement Evidence](docs/performance.md).

---

## Verification & Build

FunnySharp employs a fail-closed development harness. All release gates verify cross-platform execution on Windows, Linux, and macOS:

```bash
# Build solution
dotnet build FunnySharp.slnx

# Run xUnit test suites
dotnet test FunnySharp.slnx

# Run harness release verification pipeline
dotnet fsi build.fsx -- -p release -SkipBenchmarks

# Run full BenchmarkDotNet performance suite
dotnet run --project benchmarks/FunnySharp.Benchmarks -c Release -- --filter '*'
```

---

## Documentation & Deep Dives

Explore our comprehensive guides, contracts, and examples:

- 📖 [Product Contract](docs/product-contract.md) — Authoritative architecture principles & boundaries
- 📖 [Quick Start Guide](docs/quick-start.md) — End-to-end guided walkthrough
- 📖 [Authoritative Grammar](docs/grammar.md) — Comprehensive verb-by-carrier reference table
- 📖 [Option Guide](docs/option.md) — Deep dive into `Option<T>`
- 📖 [Result Guide](docs/result.md) — Deep dive into `Result<TValue, TError>`
- 📖 [UnitResult Guide](docs/unit-result.md) — Deep dive into command outcomes
- 📖 [Validation Guide](docs/validation.md) — Multi-error validation workflows
- 📖 [Analyzers Guide](docs/analyzers.md) — Roslyn diagnostics FS1001–FS1005 details & configuration
- 📖 [ASP.NET Core Guide](docs/aspnet-core.md) — Minimal API & ProblemDetails integration
- 📖 [Collections & Traversal](docs/collections.md) — Sequence extensions, `NonEmpty<T>`, and `Location`
- 📖 [Data Pipelines](docs/data-pipelines.md) — Stream processing, `Choose`, and `Scan`
- 📖 [Concurrency Guide](docs/concurrency.md) — Bounded parallel operations & effect coordination
- 📖 [State Machines](docs/state-machines.md) — Pure state transition workflows
- 📖 [Immutable Updates](docs/immutable-updates.md) — Lens and Optional optics
- 📖 [Performance Guidance](docs/performance.md) — Allocation budgets & benchmark methodology

---

## License

FunnySharp is open-source software licensed under the [MIT License](LICENSE).
