# Analyzers

FunnySharp ships its compiler feedback inside the core package: every `FunnySharp` package
reference adds the analyzer assemblies under `analyzers/dotnet/cs`, so a consumer gets the
diagnostics with zero extra installs. The analyzers are compile-time only — the package keeps an
empty dependency group and consumers gain no runtime reference — and every diagnostic is
suppressible through the standard mechanisms. The code fixes reference
`Microsoft.CodeAnalysis.Workspaces`, which the IDE provides, and are loaded only there.

The analyzer assembly targets `netstandard2.0` against Roslyn 4.14 and therefore requires an SDK
that ships Roslyn 4.14 or newer (the .NET 10 SDK). The analyzers are versioned with the package
itself: no separate package, no separate version, and new rules are tracked in
`src/FunnySharp.Analyzers/AnalyzerReleases.Unshipped.md` until the release containing them ships.

## Diagnostic Summary

| ID | Severity | Title | Code fix |
| --- | --- | --- | --- |
| FS1001 | error | Do not create a FunnySharp carrier with default or new() | none (intent-dependent) |
| FS1002 | warning | Do not silently discard a FunnySharp outcome | make the discard explicit with `_ =` |
| FS1003 | warning | Use the Boolean result of a FunnySharp TryGet* member | none (branch intent unknown) |
| FS1004 | warning | Await the ValueTask returned by FunnySharp instead of blocking on it | none (await needs an async context) |
| FS1005 | warning | Use UsingAsync for a resource that implements IAsyncDisposable | rename `Using` to `UsingAsync` |

The diagnostic ids never collide with the experimental-member ids the compiler generates from
`[Experimental("FS####")]` (currently `FS0017`, recorded in the
[stability inventory](stability-inventory.md)).

## FS1001: Uninitialized Semantic Carrier

Severity: **error**. `Result<TValue, TError>`, `UnitResult<TError>`, `Validation<TValue, TError>`,
`NonEmpty<T>`, `Effect<T>`, `Effect<TEnvironment, T>`, `Lens<TSource, TFocus>`, and
`Optional<TSource, TFocus>` are uninitialized in their default state, and every member that reads
them throws `InvalidOperationException` (only `ToString` returns diagnostic text). A default or
parameterless-`new` value of one of these types can never produce a legitimate outcome, so the
analyzer reports the creation site instead of waiting for the run-time throw:

<!-- documentation-sample: DocumentationSamples.Analyzers.UninitializedCarrier -->
```csharp
Result<int, string> result = default;
```

Create the value with the factories instead:

<!-- documentation-sample: DocumentationSamples.Analyzers.UninitializedCarrierFix -->
```csharp
Result<int, string> result = Result<int, string>.Success(42);
```

Reported forms: `default`, `default(T)`, `default!`, parameterless `new()` /
`new Result<...>()`, and default-valued optional parameters. Deliberately excluded, because their
defaults are valid: `Option<T>` (default is `None`) and `TransitionResult` (default is
`Undefined`). Array creation is not reported: array elements are write targets, and a read of an
unwritten element still fails at run time. Unassigned fields are not reported either: constructor
assignment makes a syntactic check unacceptably noisy, and the run-time guard remains fail-closed.

No code fix is offered: the replacement depends on whether the author meant success or failure.
Code that deliberately demonstrates the throwing contract (as the repository's own examples do)
suppresses the diagnostic locally with a documented pragma.

## FS1002: Silently Discarded Outcome

Severity: **warning**. A statement whose value is a FunnySharp semantic outcome drops absence,
failure, accumulated-validation, transition, or deferred-work information without a trace. The
outcome set is the four carriers (`Option`, `Result`, `UnitResult`, `Validation`),
`TransitionResult`, `Effect`, and the composed `StateTransition`/`StateMachine` delegates, from
any method:

<!-- documentation-sample: DocumentationSamples.Analyzers.DiscardedOutcome -->
```csharp
checkout.Save();
```

Consume the outcome, or mark the discard explicit:

<!-- documentation-sample: DocumentationSamples.Analyzers.DiscardedOutcomeFix -->
```csharp
Result<Order, CheckoutError> saved = checkout.Save();
```

A `Task`/``ValueTask`` of an outcome is equally reported, awaited or not, from any method, and an
unawaited `Task`/``ValueTask`` of any payload is reported when the invoked member belongs to
FunnySharp, because for FunnySharp members the work never runs at all without the await:

<!-- documentation-sample: DocumentationSamples.Analyzers.DiscardedAsyncWork -->
```csharp
checkout.SaveAsync();
```

Quiet by design: assignments, `return`s, awaited-and-consumed results, explicit discards
(`_ = ...`), and the observation members `Tap`, `TapAsync`, and `TapValueAsync` — whose
documented purpose is to observe a value and return it — stay callable as statements. Awaiting a
FunnySharp member whose payload is not an outcome stays quiet: the work ran and discarding a
non-outcome value is the author's choice.

The code fix makes the discard explicit; it does not decide what the discarded outcome should
mean. The documented escape hatch for a deliberate fire-and-forget is a scoped suppression:

<!-- documentation-sample: DocumentationSamples.Analyzers.DiscardedOutcomeSuppression -->
```csharp
#pragma warning disable FS1002 // The discard is deliberate; see docs/analyzers.md.
        checkout.Save();
#pragma warning restore FS1002
```

## FS1003: Ignored TryGet Presence Result

Severity: **warning**. `TryGetValue`, `TryGetError`, `TryGetErrors`, and `TryGetChange` follow
the Try pattern: the Boolean result reports presence, success, failure, or a produced change, and
the out value is `default` otherwise. A bare statement call loses that information and hands the
caller a default value:

<!-- documentation-sample: DocumentationSamples.Analyzers.IgnoredTryGetPresence -->
```csharp
option.TryGetValue(out var value);
```

Use the result — a conditional is the most common form:

<!-- documentation-sample: DocumentationSamples.Analyzers.IgnoredTryGetPresenceFix -->
```csharp
return option.TryGetValue(out var value) ? value : 0;
```

Quiet by design: conditions (`if`/``while``), assignments, and returns that use the Boolean result;
explicit discards of the whole call (`_ = option.TryGetValue(out var value);`); and out arguments
declared as discards (`out _`, `out var _`) when only the side effect matters. Try-pattern
members on non-FunnySharp types (the BCL dictionaries included) are out of scope. No code fix is
offered: what to do per branch is intent, not mechanics.

## FS1004: Blocked ValueTask

Severity: **warning**. FunnySharp's `*ValueAsync` members return `ValueTask<TResult>` under the
documented single-consumption rule, and the underlying value may be backed by pooled resources.
Blocking on `Result` or `GetAwaiter().GetResult()` may consume such a ValueTask without a proper
await:

<!-- documentation-sample: DocumentationSamples.Analyzers.BlockedValueTask -->
```csharp
var value = option.MapValueAsync(number => ValueTask.FromResult(number)).Result;
```

Await it once instead:

<!-- documentation-sample: DocumentationSamples.Analyzers.BlockedValueTaskFix -->
```csharp
var value = await option.MapValueAsync(number => ValueTask.FromResult(number));
```

Reported are `ValueTask<T>` values whose payload is a FunnySharp outcome — regardless of where
the value came from, including values stored in intermediate variables — plus any ValueTask
produced directly by a FunnySharp member. Blocking a `Task` (including `vt.AsTask().Result`)
stays quiet: that is the general sync-over-async concern rather than FunnySharp's
single-consumption contract, and a Task can be blocked safely in narrow contexts. No code fix:
converting a blocking access into an await requires an async context the fix cannot invent.

## FS1005: Synchronous Dispose of an Async-Disposable Resource

Severity: **warning**. `EffectResourceExtensions.Using` scopes the resource lifetime with the
synchronous `Dispose`, so any asynchronous cleanup implemented through `IAsyncDisposable` never
runs. When the acquired resource type implements both interfaces — `MemoryStream` and every
other dual-mode BCL resource — `UsingAsync` runs `DisposeAsync` with an otherwise identical
signature:

<!-- documentation-sample: DocumentationSamples.Analyzers.SyncDisposeOfAsyncDisposableResource -->
```csharp
return Effect.FromSync(() => new MemoryStream())
    .Using(stream => Effect.FromValue(stream.Length));
```

<!-- documentation-sample: DocumentationSamples.Analyzers.SyncDisposeOfAsyncDisposableResourceFix -->
```csharp
return Effect.FromSync(() => new MemoryStream())
    .UsingAsync(stream => Effect.FromValue(stream.Length));
```

The code fix applies that rename; the overload shapes match for a resource that implements both
interfaces, so type inference and arguments are unchanged. A resource that implements only
`IDisposable` is correctly served by `Using` and stays quiet.

## Suppression and Severity Behavior

Every diagnostic is enabled by default, configurable, and suppressible — none is
`NotConfigurable`. The standard mechanisms all work:

- `#pragma warning disable FS100x` around a deliberate use, with a comment saying why;
- `NoWarn` in a project that accepts the pattern globally;
- `[SuppressMessage]` at a call site or for a whole scope;
- severity remapping through `.editorconfig` (`dotnet_diagnostic.FS1002.severity = none`).

`FS1001` is an error because every read of an uninitialized carrier throws at run time; the
remaining diagnostics are warnings because they describe silent-behavior hazards rather than
guaranteed failures. In a project with `TreatWarningsAsErrors` all of them fail the build — which
is how this repository consumes its own analyzers in the example projects.

## Evaluated and Rejected

Candidates that could not be diagnosed with acceptably low false-positive risk stay out, recorded
here so the boundary is deliberate:

| Candidate | Why not shipped |
| --- | --- |
| Cancellation misuse (`CancellationToken.None`, missing token in scope) | Passing no token is semantically identical to `None`; forwarding heuristics are not locally provable and eager-cancellation detection needs dataflow. |
| Multiple enumeration of a deferred sequence | Requires effect analysis across arbitrary receivers; single-pass sources cannot be distinguished syntactically. |
| Resource acquisition outside `Using` (leak) | Needs whole-program dataflow to distinguish escape from disposal. |
| Blocking a `Task` (`Result`/``Wait``) | General .NET hazard, not FunnySharp-specific; FS1004 covers the ValueTask single-consumption contract that the library documents. |
| Sync-over-async inside `Result.Try` boundaries | The tokenless delegates are the recorded deliberate design ([product contract](product-contract.md)). |
| Unassigned carrier fields | Constructor assignment defeats a syntactic check; the run-time guard already fails closed. |
| Unchecked `GetValueOrDefault()` | Returning `default(T)` is its documented contract; post-call flow is nullable-analysis territory for reference payloads. |
| Deprecated or ambiguous API forms | The shipped surface contains none to deprecate: one verb has one meaning, and the curation tests pin the surface; a future deprecation travels as `[Obsolete]` plus a recorded decision. |

## Packaging, Maintenance, and Verification

- The analyzer and code-fix assemblies build as `netstandard2.0` (`src/FunnySharp.Analyzers`,
  `src/FunnySharp.Analyzers.CodeFixes`) and are embedded into `FunnySharp.nupkg` under
  `analyzers/dotnet/cs`; the package keeps an empty dependency group.
- Versioning: the rules ride the package version, and a rule is tracked in
  `src/FunnySharp.Analyzers/AnalyzerReleases.Unshipped.md` until the package version containing
  it ships, when it moves to `AnalyzerReleases.Shipped.md` with that version. The Roslyn
  reference floor is 4.14 (the .NET 10 SDK); raising it is a deliberate, recorded decision.
- Measured maintenance surface at delivery: 932 analyzer/code-fix lines of code across 10 files,
  five diagnostics, two code fixes, and 73 tests (1,577 test lines) covering every reported form,
  every quiet form, the suppressions, both fixes, the embedding invariant, and the clean analysis
  of all three example projects. Ordinary rule changes touch one analyzer plus its test file; the
  suite runs with the repository tests in about 15 seconds.
- `tests/FunnySharp.Analyzers.Tests` covers every diagnostic (reported forms, quiet forms,
  suppressions, severities), both code fixes, the package-embedding invariant, and compiles the
  repository's example projects with the analyzers to prove ordinary code stays quiet.
- The example projects consume the analyzers through project references, so the release gate
  fails on any diagnostic in ordinary example code.
- The model-agnostic coding evaluation that measured the compiler-feedback effect is recorded in
  [next-stage/ai-usability-goal-21.md](next-stage/ai-usability-goal-21.md).
