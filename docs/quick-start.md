# Quick Start

FunnySharp is a pragmatic, BCL-first functional programming library for `net10.0`. It builds
`Option`, `Result`, `UnitResult`, and `Validation` on ordinary C# values, delegates, `Task`,
`ValueTask`, cancellation, and the standard collection types instead of introducing a runtime, a
dependency-injection container, or a parallel type hierarchy. This page introduces each surface in
the order below and links the guide that owns its full contract. The compiled counterpart of every
C# block on this page is
[examples/FunnySharp.Examples/Program.cs](../examples/FunnySharp.Examples/Program.cs), and the
[API shape](#api-shape) list names each surface and its canonical verbs.

```shell
dotnet add package FunnySharp
dotnet add package FunnySharp.AspNetCore
```

The core package carries the carriers, the shared grammar, the collection and pipeline operators,
and the analyzers. `FunnySharp.AspNetCore` is optional and adds Minimal API result mapping only;
it is not needed outside an ASP.NET Core application.

## API Shape

- **Outcome carriers** — `Option<T>` (absence), `Result<TValue, TError>` (fail-fast value),
  `UnitResult<TError>` (fail-fast command), and `Validation<TValue, TError>` (accumulating
  checks). Canonical verbs: `Map`, `Bind`, `Ensure`, `Match`.
  See [option.md](option.md), [result.md](result.md), [unit-result.md](unit-result.md), and
  [validation.md](validation.md).
- **Shared grammar** — one verb has one meaning and one output shape on every carrier where it is
  valid; `...Async` marks an awaitable form. See [grammar.md](grammar.md).
- **Collections and traversal** — cardinality access (`FirstOrNone`, `SingleOrNone`), the
  `NonEmpty<T>` guarantee, `Partition`, exact combination (`ZipExact`), and traversal. See
  [collections.md](collections.md).
- **Pipelines and streaming** — the fused `Choose`, the running `Scan`, and caller-buffered
  span/memory operations over `IEnumerable<T>` and `IAsyncEnumerable<T>`. See
  [data-pipelines.md](data-pipelines.md).
- **Concurrency** — bounded parallel mapping in source or completion order, and first-success over
  cold effects. See [concurrency.md](concurrency.md).
- **Effects and resources** — the deferred `Effect` boundary and scoped disposal. See
  [effects.md](effects.md).
- **State machines** — total `StateTransition` values and their typed outputs. See
  [state-machines.md](state-machines.md).
- **Immutable updates** — `Lens` and `Optional` for composable nested reads and replacements. See
  [immutable-updates.md](immutable-updates.md).
- **Analyzers** — compiler feedback FS1001 through FS1005 for common carrier misuse. See
  [analyzers.md](analyzers.md).
- **Performance** — per-operation cost policy and allocation budgets. See
  [performance.md](performance.md).
- **ASP.NET Core** — `ToHttpResult` and `ToHttpResultAsync` mapping for Minimal APIs. See
  [aspnet-core.md](aspnet-core.md).

## Carriers

The four carriers cover presence, fail-fast value work, fail-fast command work, and independent
validation. `Option<T>` maps and binds a present value and carries absence without an error.
`Result<TValue, TError>` and `UnitResult<TError>` stop at the first failure. `Validation` collects
every error from independent checks. `Result`, `UnitResult`, and `Validation` are `readonly struct` carriers whose `default` value is
uninitialized rather than a legitimate outcome, so the analyzer reports creating one (see
[Analyzer Feedback](#analyzer-feedback)); `Option<T>`'s default remains a valid `None`, matching
its own contract.

<!-- documentation-sample: DocumentationSamples.QuickStart.OptionResult -->
```csharp
Option<string> sku = Option.Some("SKU-1");
Result<int, string> quantity = sku
    .Map(static text => text.Length)
    .ToResult("The SKU is absent.");

UnitResult<string> stored = UnitResult<string>.Success()
    .Ensure(() => quantity.IsSuccess, "The quantity is missing.");
Validation<int, string> price = Validation<int, string>.Valid(
    quantity.Match(static value => value, static _ => 0));
```

`Option<T>.ToResult(error)` and `Option<T>.ToResult(errorFactory)` convert presence to success and
absence to the supplied failure, which is how the SKU becomes a `Result` above. This guide covers
the carriers only at the level the quick start needs; the per-carrier guides carry the full
evaluation, null, and default-state contracts.

## Grammar

`Map`, `Bind`, `Ensure`, and `Match` keep one meaning across carriers. `Map` transforms the active
value without changing the shape. `Bind` sequences a function that returns the same carrier.
`Ensure` keeps a value only when a predicate accepts it, producing the supplied error otherwise.
`Match` is the terminal extraction and forces both cases to be handled. Composition stays on
standard delegates: `Map`/`Bind` take ordinary `Func` values, so no carrier-specific delegate
hierarchy is needed.

<!-- documentation-sample: DocumentationSamples.QuickStart.SharedVerbs -->
```csharp
Option<int> doubled = Option.Some(4)
    .Map(static value => value * 2)
    .Bind(static value => Option.Some(value + 1));

int total = doubled
    .ToResult("missing")
    .Ensure(static value => value > 0, "not positive")
    .Match(static value => value, static _ => 0);
```

The same verbs apply to `Result`, `UnitResult`, and `Validation` where the meaning is valid;
[grammar.md](grammar.md) is the authoritative verb reference, and the differences that matter —
`Validation` has no `Bind` because dependent checks cannot accumulate independently — are stated
there and in [validation.md](validation.md).

## Collections

FunnySharp keeps the standard .NET collections and sequence types. Its additions make cardinality
and combination explicit: the `*OrNone` family turns "no item" into `None` instead of a default
value, `NonEmpty<T>` proves at least one item exists, `Partition` splits one enumeration into
meaning-bearing sides, and `ZipExact` pairs sequences only when their lengths agree.

<!-- documentation-sample: DocumentationSamples.QuickStart.Cardinality -->
```csharp
var quantities = new[] { 2, 4, 6 };

Option<int> first = quantities.FirstOrNone();
Option<NonEmpty<int>> nonEmpty = quantities.ToNonEmptyOrNone();
var (even, odd) = quantities.Partition(static value => value % 2 == 0);
Option<IReadOnlyList<(int First, int Second)>> pairs =
    quantities.ZipExactOrNone(new[] { 10, 20, 30 });
```

`FirstOrNone` is eager and stops at the first item; `None` means only that the source was empty.
`ToNonEmptyOrNone` returns `Some` exactly when an item exists, and its `Count` distinguishes a
singleton from multiple items. `Partition` enumerates once and preserves source order within each
side. `ZipExact` succeeds identically or fails with the mismatch factory's error, which receives both
total counts. See
[collections.md](collections.md) for the full cardinality, traversal, and bridge surface.

## Pipelines And Streaming

Pipelines stay on BCL sequence types. Two operators are added where LINQ cannot express the shape
without changing lifetime or allocation behavior: `Choose` filters and maps in one deferred pass,
and `Scan` yields the running aggregate after each element. Both are deferred, preserve source
order, and enumerate the source once per consumer enumeration.

<!-- documentation-sample: DocumentationSamples.QuickStart.ChooseScan -->
```csharp
var quantities = new[] { 0, 2, 0, 4 };

IEnumerable<int> present = quantities
    .Choose(static value => value == 0 ? Option.None<int>() : Option.Some(value));
IEnumerable<int> running = present.Scan(0, static (total, value) => total + value);
```

The asynchronous forms use the same names on `IAsyncEnumerable<T>`. They are pull-based: they
request and process one source item at a time, await each `ValueTask` once, and never introduce
parallel execution. Cancellation comes from the consumer through `WithCancellation`.

<!-- documentation-sample: DocumentationSamples.QuickStart.AsyncChooseScan -->
```csharp
IAsyncEnumerable<int> present = source.Choose(
    static value => value % 2 == 0 ? Option.Some(value) : Option.None<int>());
IAsyncEnumerable<int> running = present.Scan(0, static (total, value) => total + value);
```

Span and memory pipelines are separate and caller-buffered: they are immediate, never deferred,
and never falsely unified with asynchronous streams. See
[data-pipelines.md](data-pipelines.md) for the exact contracts.

## Concurrency

Asynchronous sequences support bounded parallel mapping: at most `maxConcurrency` selectors are in
flight, and results are delivered in source order by `SelectParallelValueAsync` or as they complete
by `SelectParallelCompletionOrderValueAsync`. First-success over cold effects picks the first
successful outcome within a timeout.

<!-- documentation-sample: DocumentationSamples.QuickStart.ParallelMap -->
```csharp
return source.SelectParallelValueAsync(
    maxConcurrency: 4,
    static item => new ValueTask<int>(item * 2));
```

The first observed source or selector failure stops admission, cancels the linked operation, and
drains the work already started before it propagates. Cancellation stays an exception and is never
folded into a result. See [concurrency.md](concurrency.md) for completion-order and first-success
variants.

## Effects And Resources

`Effect<T>` is the deferred-work boundary, not an outcome carrier: it describes work that runs when
`RunAsync` is awaited, and it composes with `Map` and `Bind` before anything runs.
`Effect.FromValue` wraps an already-computed value, and `Effect.FromSync` defers a synchronous
delegate; `FromTask` and `FromValueTask` defer asynchronous work.

<!-- documentation-sample: DocumentationSamples.QuickStart.CreateEffect -->
```csharp
Effect<string> greeting = Effect.FromSync(() => "hello")
    .Map(static text => text.ToUpperInvariant());

string value = await greeting.RunAsync();
```

Resource lifetimes scope to an effect with `Using` for `IDisposable` and `UsingAsync` for
`IAsyncDisposable`. Choose `UsingAsync` whenever the resource implements `IAsyncDisposable`; using
the synchronous overload there is reported by FS1005.

<!-- documentation-sample: DocumentationSamples.QuickStart.ScopeResources -->
```csharp
Effect<int> syncHandle = Effect.FromSync(() => new Handle())
    .Using(static handle => Effect.FromValue(handle.Value));
Effect<long> asyncStream = Effect.FromSync(() => new MemoryStream())
    .UsingAsync(static stream => Effect.FromValue(stream.Length));

int syncLength = await syncHandle.RunAsync();
long asyncLength = await asyncStream.RunAsync();
```

`Effect.FromValue` preserves a runtime `null` rather than rejecting it: annotate the type argument
nullable, as in `Effect.FromValue<string?>(null)`, to make the nullability explicit. See
[effects.md](effects.md) for the execution and disposal ordering contracts.

## State And Immutable Updates

A state transition is a total, pure delegate from a state to its next state and outputs. Records
with `with` expressions express the next state, so the transition stays ordinary C#.

<!-- documentation-sample: DocumentationSamples.QuickStart.StateTransition -->
```csharp
StateTransition<Account, AuditCommand> submit = current =>
    StateChange<Account, AuditCommand>.To(
        current with { Status = AccountStatus.Submitted },
        new StoreAccount(current.Id));
```

Nested immutable updates compose `Lens` values for total focuses and `Optional` values for
conditional ones. A lens reads and replaces a focus; composition builds a path into a nested
record, and `Set` writes the new focus through that path.

<!-- documentation-sample: DocumentationSamples.QuickStart.NestedUpdate -->
```csharp
var profile = Lens.Create<Account, Profile>(
    value => value.Profile,
    (value, next) => value with { Profile = next });
var city = Lens.Create<Profile, string>(
    value => value.City,
    (value, next) => value with { City = next });

Account updated = profile.Compose(city).Set(account, "Paris");
```

Use `Optional` instead of `Lens` when the focus may be absent, such as a dictionary key. See
[state-machines.md](state-machines.md) and [immutable-updates.md](immutable-updates.md) for the
full transition and optics surface.

## Analyzer Feedback

The analyzers ship inside the core package and run at compile time. They report five common
mistakes: FS1001 creates an uninitialized carrier with `default` or `new()` (an error, because the
value can never be a legitimate outcome) for `Result<TValue, TError>`, `UnitResult<TError>`,
`Validation<TValue, TError>`, `NonEmpty<T>`, `Effect<T>`, `Effect<TEnvironment, T>`,
`Lens<TSource, TFocus>`, and `Optional<TSource, TFocus>` — `Option<T>` is deliberately excluded
because its default is a legitimate `None` — FS1002 silently discards a carrier-returning outcome, FS1003 ignores the
Boolean result of a `TryGet*` member, FS1004 blocks on a `ValueTask`, and FS1005 disposes an
`IAsyncDisposable` through the synchronous `Using`. Handling a carrier keeps the diagnostic quiet.

<!-- documentation-sample: DocumentationSamples.QuickStart.HandledOutcome -->
```csharp
Option<int> quantity = Option.Some(3);
int present = quantity.TryGetValue(out var found) ? found : 0;

Result<Order, OrderError> saved = SaveOrder(order);
string outcome = saved.Match(static _ => "saved", static error => error.Code);
```

`TryGetValue` is used as a condition here rather than ignored, and the saved result is matched
instead of dropped. Every diagnostic is suppressible through the standard mechanisms when a
deliberate discard is warranted. See [analyzers.md](analyzers.md) for the per-diagnostic contracts.

## Performance

Cost is measured, not assumed. Each performance-relevant stable operation has a per-operation
allocation budget in the tracked manifest, and a release observation that exceeds a budget or
regresses a zero-allocation row to nonzero fails verification. Timing is directional evidence tied
to its recorded environment; allocation is blocking evidence. The carriers allocate nothing on the
heap for their construction and dispatch over struct fields, and no zero-cost or always-faster
claim is made anywhere. See [performance.md](performance.md) for the budgets and how to read them.

## ASP.NET Core

`FunnySharp.AspNetCore` maps carriers to `IResult` for Minimal APIs. Every mapping requires an
explicit problem mapper that returns a non-null `ProblemDetails`; the success mapper is optional
and defaults to `Results.Ok(value)`. The `...Async` overloads accept `Task<...>` and `ValueTask<...>`
carriers and await them normally, so faults and cancellation stay transparent.

<!-- documentation-sample: DocumentationSamples.QuickStart.ToHttpResult -->
```csharp
app.MapGet("/orders/{id:int}", (int id, CancellationToken cancellationToken) =>
    FindOrderAsync(id, cancellationToken).ToHttpResultAsync(NotFound));
```

The package adds no dependency-injection registrations, middleware, or global error policy, so the
core package stays usable without any HTTP concept. See [aspnet-core.md](aspnet-core.md) for the
mapping overloads.

## Deliberate Boundaries

FunnySharp intentionally does not provide:

- A second absence carrier. `Option<T>` is the only one; `Maybe`, `Either`, `Fin`, `Try`, and
  `IO` remain out of scope.
- `Where` on a carrier. A bare predicate cannot produce absence or a typed error, so `Filter`
  (option) and `Ensure` (result and unit result) carry that meaning explicitly.
- An effect runtime or dependency-injection container. `Effect<T>` is a deferred-work boundary
  that composes with standard delegates; it is not an interpreter or a service locator.
- A retry, circuit-breaker, or resilience policy layer. Retries belong to the caller or a
  dedicated library.
- General discriminated unions. The official .NET design and a separate accepted goal are required
  before that surface appears.
- A competitor compatibility surface. Every package dependency stays BCL-first; there is no
  namespace, type, or member added only to match another library.
