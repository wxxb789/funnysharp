# BCL baseline analysis for Goal 14 (net10.0)

Purpose: judge, per FunnySharp API family F1–F11, whether the pinned .NET 10 BCL is
already sufficient, whether FunnySharp reduces consumer-side semantic LOC or adds
compile-time verifiability, and what BCL operation is the honest performance
reference. This memo records **BCL coverage verdicts** only. It does not record
keep/redesign/remove decisions for FunnySharp APIs and does not record
adopt/adapt/defer/reject decisions for external capabilities — those belong to the
lead in `docs/next-stage/decision-record.md` and `docs/next-stage/api-decisions.md`.

## Method

- Baseline: SDK 10.0.400, runtime 10.0.11, `Microsoft.NETCore.App.Ref/10.0.11/ref/net10.0`;
  repo target `net10.0` (`Directory.Build.props:3`). Capability facts and exact names
  are in [`../inventory/bcl.md`](../inventory/bcl.md) and the dumps it indexes.
- FunnySharp surface: `inventory/generated/inv-funny-sharp-core.*` (commit
  `4dbebd9`, 0.1.0, 33 types) and `inv-funny-sharp-aspnetcore.*` (1 type, 15 members).
- Criteria labels (1–8) are the numbered dimensions from the brief: ordinary C#
  usefulness, consumer-side semantic LOC, clarity from signatures, compile-time
  verifiability, AI predictability, BCL interoperability, async/cancellation
  semantics, credible performance feasibility.
- Verdict vocabulary:
  - **duplicate-risk** — the BCL already covers the capability in idiomatic C#; a
    FunnySharp API for it must justify itself against criteria 1, 2, 5, 8 or be
    redesigned/removed.
  - **additive-value** — no BCL equivalent for the capability itself; FunnySharp adds
    semantics the BCL lacks.
  - **necessary-bridge** — the value is in binding BCL/framework carriers together
    (cancellation, carriers, HTTP, serialization), not in a new capability.
- Every unverifiable judgment is flagged **UNVERIFIED**; nothing here is a decision.

## Summary

| Family | BCL already sufficient? | FunnySharp value | Honest BCL performance reference | BCL coverage verdict |
| --- | --- | --- | --- | --- |
| F1 absence | Yes for ordinary absence | One more carrier + bridges | `Nullable<T>`, `TryGetValue`, `FirstOrDefault`, `GetValueOrDefault` | **duplicate-risk** |
| F2 fail-fast outcome | No result type; exceptions/Try only | Typed error parameter, chain, `Match` exhaustiveness | `bool TryX(out E)`, `throw`, `ExceptionDispatchInfo` | **additive-value** |
| F3 accumulation | No accumulation algebra | Typed error accumulation across independent failures | `List<TError>` collection, `Validator.TryValidateObject` | **additive-value** |
| F4 function grammar | Yes (delegates + lambdas) | Little; composition sugar | direct lambda, method group, extension chaining | **duplicate-risk** |
| F5 collections/traversal | LINQ + spans cover general case | Option/Result/Validation traversal; span pipelines | `Enumerable.*`, `MemoryExtensions`, `Span<T>` loops | **additive-value** (generic LINQ surface is duplicate-risk) |
| F6 async/streaming | Plumbing complete | Async traversal, bounded streaming map, first-success | `Task.WhenAll`, `Parallel.ForEachAsync`, `Channel` | **additive-value** (plumbing must stay BCL) |
| F7 effects/resources/env | `using`/`await using` cover lifetime | Deferred composition, environment, timeouts | direct calls, `using`, `IDisposable` patterns | **additive-value** (resource wrappers duplicate-risk) |
| F8 state machines | Nothing in BCL | Vocabulary, typed transition outcome, replay | `switch` on state, readonly struct result | **additive-value** |
| F9 optics/immutability | Immutable/Frozen/Concurrent complete | 2-type Lens/Optional only | `record with`, `ImmutableX.SetItem`, `FrozenX` lookup | **additive-value** (collection wrappers duplicate-risk) |
| F10 HTTP | Framework provides results/ProblemDetails/OpenAPI/SSE | Carrier → `IResult` mapping | `TypedResults`, `Results<T1..Tn>`, `IProblemDetailsService` | **necessary-bridge** |
| F11 cross-cutting | Measurement, trim/AOT, JSON all present | Vocabulary only; must measure against BCL | `GC.GetAllocatedBytesForCurrentThread`, `Stopwatch.GetTimestamp` | **necessary-bridge** |

---

## F1 — Absence: `Option`/nullable/Try/dictionary bridges

**BCL baseline.** NRT + `Nullable<T>` + Try-pattern + `...OrDefault` +
`CollectionExtensions.GetValueOrDefault` cover ordinary absence, with
`[NotNullWhen]`/`[MaybeNullWhen]` flow contracts (inventory F1; dump
`inv-bcl-language-errors.md`, `inv-bcl-supplement.md`).

**Sufficiency.** For a normal business problem, `T?` + `TryGetValue` + null checks
is the idiomatic answer, and it interoperates with every BCL and framework API.
FunnySharp's `Option<T>` is a second absence carrier; its bridge surface
(`Option.FromNullable`, `OptionExtensions.ToOption`, `GetOption`) exists precisely
because the carriers do not unify (`inv-funny-sharp-core.md`, `Option`,
`OptionExtensions`). A second carrier must pay for itself; the honest reading is
that plain absence is a duplicate.

**Compile-time verifiability.** NRT is a warning-level analysis that `!` and
defaults can defeat, so `Option<T>` genuinely forces handling where NRT only warns.
But `Option<T>.Some(T? value)` accepts a nullable argument per the dump — whether
`Some(default)` normalizes to `None` is **UNVERIFIED** from the inventory; if it
does not, `Option<T>` admits the same null hole it is meant to close.

**Semantic LOC.** Single lookups are a wash (`TryGetValue` + `if` vs `Match`).
Chained lookups (`a.GetOption("k").Bind(...)` + one `Match`) reduce nesting, but
`?.`/`??` chains and early returns are competitive.

**Performance reference.** `Nullable<T>` and `Dictionary.TryGetValue` for absence;
`Enumerable.FirstOrDefault`/`ElementAtOrDefault` for sequence absence;
`CollectionExtensions.GetValueOrDefault` for defaulted lookup. Any `Option` default
path must be within a small constant factor of these, and delegate-based `Match`
with non-cached lambdas will allocate.

**Verdict: duplicate-risk** for carrying absence; additive-value only for the
dictionary/Try bridges and for Option-aware traversal (F5).

## F2 — Fail-fast value outcomes: `Result`, `UnitResult`, Try boundaries

**BCL baseline.** Exceptions with a deep hierarchy; `bool`+`out` Try conventions;
`ExceptionDispatchInfo`; `AggregateException`; cancellation as
`OperationCanceledException` (inventory F2).

**Sufficiency.** The BCL has no `Result<TValue,TError>` (verified absence).
Exceptions remain the default for contract violations and for framework
interoperability; a result type cannot replace `try/catch` at library boundaries.
So this is additive, but only if the boundary policy is explicit: `Result.Try*`
at exception edges, ordinary exceptions inside, and cancellation never converted.

**Compile-time verifiability.** `Result<TValue,TError>` parameterizes the error
type, and `Match`/`TryGetError(out)` force the caller to consider both branches.
This is real, but note `TryGetError(out TError?)` allows ignoring the error, and
the type does not prevent using exceptions alongside it.

**Semantic LOC.** For N sequential fallible steps: nested `if (!Try...)` blocks with
error plumbing vs one `Bind` chain + `Match`. Reduction is real for N ≥ 2; for a
single step, a `bool TryX(out E)` local function is shorter and clearer.

**Async semantics (criterion 7).** Cancellation must stay cancellation.
FunnySharp's `Result.Try*` deliberately excludes `OperationCanceledException` from
its catch filter and rethrows (`src/FunnySharp/Result.cs:40`, contract documented
at `:57,80,105,129`) — a correct use of the BCL convention. This is the kind of
behavior a naive `catch (Exception)` implementation gets wrong, so it is a
positive differentiator if the decision record keeps it.

**Performance reference.** The no-throw path of `bool TryX(out TError)` plus a
`switch` on the error; `throw` costs are only relevant on failure paths. The
struct carrier must not box (it is a `readonly struct`, `inv-funny-sharp-core.md`)
and its `Match` delegates must be static-lambda cacheable. Existing receipts:
`benchmarks/FunnySharp.Benchmarks/ResultBenchmarks.cs`
(`eng/performance/baseline.json:16`).

**Verdict: additive-value** (no BCL equivalent), with the exception-boundary policy
as the condition that keeps it from becoming a second error universe.

## F3 — Accumulation: `Validation`

**BCL baseline.** No generic accumulation type. The only collector is
`System.ComponentModel.DataAnnotations.Validator.TryValidateObject(..., ICollection<ValidationResult>)`
plus `IValidatableObject`/`ValidationResult.MemberNames`
(`inv-bcl-supplement-f2f3f11.md`). It is reflection/attribute-driven, produces
untyped messages + member names, has no error type parameter and no applicative
composition. `AggregateException` accumulates task faults only.

**Sufficiency.** The BCL is not sufficient for "validate N independent things,
report all failures of type `TError`". `Validation<TValue,TError>` with
`Apply`/`Zip`/`InvalidMany` has no BCL counterpart. DataAnnotations is a different
tool (model-level validation with reflection); ASP.NET Core's Minimal API validation
already uses it and renders `HttpValidationProblemDetails`
(`.../ValidationSupportMinAPI.md`, `.../validation-with-problem.md`).

**Compile-time verifiability.** `Match(invalid: IReadOnlyList<TError>)` forces
handling multiple errors, which `Result` cannot express. However
`InvalidMany(IEnumerable<TError>)` can construct an invalid state with an empty
list (**UNVERIFIED**: whether the implementation normalizes/throws on empty), and
the error list is not type-level non-empty.

**Semantic LOC.** Accumulating K properties each with independent validation:
manual `List<TError>.Add` + boolean flags vs `Zip`/`Apply` merging — meaningful
reduction once K ≥ 2.

**Performance reference.** `List<TError>` accumulation with early-exit disabled,
and `Validator.TryValidateObject` for the reflection comparison. The honest claim
is "no worse than a hand-written error list", not "faster than DataAnnotations"
(which is a different workload).

**Verdict: additive-value.** This is the strongest no-BCL-equivalent family.

## F4 — Function grammar: `Pipe`, `Compose`, `Curry`, `Partial`, `Flip`, `Tap`

**BCL baseline.** `Func`/`Action` arities 0–16, `Predicate<T>`, `Comparison<T>`,
`Converter<TIn,TOut>`, `Lazy<T>`, `Lazy<T,TMetadata>`, `LazyInitializer`; plus
language method groups, lambdas, static lambdas, local functions (inventory F4).

**Sufficiency.** Ordinary C# composes with lambdas and chained extension methods:
`Func<T,TResult>` composition is a one-line lambda. `Curry`/`Partial`/`Flip` are
not ordinary C# workflows and generally hurt criteria 3 and 5 (a curried
`Func<T1,Func<T2,TResult>>` is harder for a reader and an agent to predict than a
two-argument method). `Pipe` is one extension call whose only value is argument
reordering; the F# pipe idiom is attractive but it is a dialect choice (criterion 1).

**Compile-time verifiability.** None: all of these are delegate plumbing; misuse is
a type error only where the delegates differ, and `Partial` with `null` first
arguments as shown in the dump (`Partial<TFirst,TSecond,TResult>(Func<...>, TFirst?)`)
can silently bind `null`.

**Semantic LOC.** `x.Pipe(f)` vs `f(x)` is LOC-neutral; `Compose(f, g)` vs
`x => g(f(x))` is LOC-neutral for one composition, positive for long chains reused
in many places.

**Performance reference.** A direct lambda call chain: `Compose` builds one extra
delegate; `Tap` adds one delegate invocation per element. Any composed path must
match the allocation count of a hand-written lambda. Existing receipts:
`benchmarks/FunnySharp.Benchmarks/FunctionCompositionBenchmarks.cs`
(`eng/performance/baseline.json:10`).

**Verdict: duplicate-risk.** Keep only if representative call sites (Goal 14
evidence requirement) show a measurable LOC or readability win over lambdas; the
async `Compose`/`TapAsync` forms are the plausible survivors because sequencing
`await` calls is genuinely repetitive.

## F5 — Collections and traversal

**BCL baseline.** `Enumerable` (228 members) + `AsyncEnumerable` (188; the
assembly ships in the 10.0.11 ref pack) + `MemoryExtensions` (264) +
buffers/pools + `CollectionExtensions` (inventory F5). Async LINQ in-box means
`IAsyncEnumerable` consumers get the full sync-like operator set with
`CancellationToken`-aware `ValueTask` predicate/selector overloads; whether
in-box delivery began in .NET 10 or earlier is **UNVERIFIED** (see the inventory's
Not examined section).

**Sufficiency.** General traversal is BCL-covered; no FunnySharp API should shadow
`Where`/`Select`/`Aggregate`/`GroupBy`/`Order`/set operators, sync or async. What
the BCL does not have:

1. `Sequence`/`Traverse` over `Option`/`Result`/`Validation` — early-exit or
   accumulate semantics over a carrier the BCL does not know about. Additive.
2. `Choose` — `Where` + `Select` on `Option`; the BCL idiom is a nullable
   `Where` + `Select`, which is two operators and an implicit absence convention.
   Additive but small.
3. Span pipelines (`SelectTo`, `ChooseTo`, `WhereTo`, `SelectInPlace`,
   `WhereInPlace`) — the BCL has no span LINQ; `MemoryExtensions` has
   `Split`/`SequenceEqual`/`IndexOf` but no projection/filter into caller-owned
   storage. Additive if the destination-buffer contract (length, truncation
   behavior, capacity exceptions) is clear from signatures (criterion 3).
4. Error-location context for traversal failures — **the BCL has none and the
   current FunnySharp surface also appears to have none**: `Traverse`/`Sequence`
   return the carrier with the first/aggregated error but no element index/key
   (`inv-funny-sharp-core.md`, `SequenceExtensions`). If Goal 14 values error
   context, no existing API provides it in either baseline.

**Compile-time verifiability.** Carrier-aware traversal is type-checked (you cannot
mix `Result` with `Option` selectors), and `Validation.Sequence` accumulates while
`Result.Sequence` short-circuits — a semantic distinction encoded in types, which
the BCL cannot express.

**Semantic LOC.** `source.Traverse(Parse)` vs a `foreach` + `if (result.IsFailure) return ...`
loop: positive for `Result`/`Validation`, marginal for `Option` (LINQ + `Where`).

**Performance reference.** Sync: `Enumerable.Select(...).ToArray()` plus a manual
short-circuit loop; span: a hand-written `for` loop over `ReadOnlySpan<T>` writing
into `Span<T>`; async: `await foreach` with `WithCancellation` + a `List<T>`.
Existing receipts: `benchmarks/FunnySharp.Benchmarks/SequenceBenchmarks.cs`,
`DataPipelineBenchmarks.cs` (`eng/performance/baseline.json:8,17`). The
`IReadOnlyList<T>` result materializes, so the honest comparison includes
allocation of the final list.

**Verdict: additive-value** for carrier-aware traversal and span pipelines;
**duplicate-risk** for any future generic collection/LINQ-like surface.

## F6 — Async, streaming, concurrency

**BCL baseline.** `Task`/`ValueTask`/TCS, `WhenAll`/`WhenAny`/`WhenEach`,
`WaitAsync(timeout, TimeProvider)`, `Parallel.ForEachAsync` (including
`IAsyncEnumerable` sources and `ParallelOptions.MaxDegreeOfParallelism`),
`IAsyncEnumerable` + `[EnumeratorCancellation]` + `WithCancellation` +
`ConfigureAwait`, `Channel` with `BoundedChannelFullMode` backpressure,
`CancellationToken(Source)` + linked tokens, `TimeProvider`/`ITimer`/`PeriodicTimer`
(inventory F6). Plumbing is complete and must not be re-implemented.

**Sufficiency and FunnySharp value.** The carrier-aware async extensions
(`ChooseValueAsync`, `SequenceAsync`, `TraverseAsync`, `TraverseValueAsync`) are
additive because .NET 10 async LINQ still has no `Traverse`/`Sequence`/`Choose`.
The concurrency extensions are additive for the same reason:

- `SelectParallelValueAsync(source, maxConcurrency, selector)` — an ordered
  streaming map with a concurrency bound and `Channel` backpressure. The BCL has
  the pieces (`Channel`, `Parallel.ForEachAsync`) but no combinator; note that
  `Parallel.ForEachAsync` **consumes** and produces one `Task`, while this API
  produces `IAsyncEnumerable<TResult>` (with ordering to verify).
- `TraverseParallelValueAsync` — bounded fan-out that materializes an ordered list
  and distinguishes fail-fast from accumulation.
- `FirstSuccessAsync` — run N `Effect<Result<...>>`, return first success, drain
  all started work, optional `TimeProvider` timeout. `Task.WhenAny` returns the
  first *completed* task and leaves the rest unobserved; the drain/observe
  semantics are not a BCL feature.

**Async/cancellation semantics (criterion 7) — the main risk surface.** The
implementation already uses `[EnumeratorCancellation]` and
`WithCancellation(...).ConfigureAwait(false)` in async iterators
(`src/FunnySharp/AsyncEnumerablePipelineExtensions.cs:79-81`,
`src/FunnySharp/AsyncSequenceExtensions.cs:353-396`) and treats caller
cancellation as an `OperationCanceledException` that stays faulted rather than a
domain failure (`src/FunnySharp/Result.cs:57,80,105,129`). The BCL offers no
compiler protection here; the review burden is on the analysis, and the remaining
UNVERIFIED points are: ordering guarantee of `SelectParallelValueAsync`, whether a
failed consumer disposes upstream enumerators, and whether `FirstSuccessAsync`
distinguishes "all failed" from "caller cancelled" in every path.

**Performance reference.** Sequential `await` vs `Task.WhenAll` for independent
work; `Parallel.ForEachAsync` with `MaxDegreeOfParallelism` for bounded
concurrency; `Channel.CreateBounded(capacity).Reader.ReadAllAsync()` for streaming
backpressure; `TaskCompletionSource` for first-completion. Existing receipts:
`benchmarks/FunnySharp.Benchmarks/ConcurrencyBenchmarks.cs`,
`FirstSuccessConcurrencyBenchmarks`, `ParallelTraverseConcurrencyBenchmarks`
(`eng/performance/baseline.json:7`). ValueTask-returning APIs must beat or match
Task-based equivalents on the synchronous-completion path; `ValueTask` must never
be awaited twice (the carrier is a struct; misuse is not compiler-checked).

**Verdict: additive-value** for carrier-aware async traversal, bounded parallel
streaming, and first-success; **duplicate-risk** for anything that recreates BCL
scheduling/cancellation primitives or wraps `Parallel.ForEachAsync` without adding
the streaming/ordered result contract.

## F7 — Effects, resources, environment

**BCL baseline.** `IDisposable`/`IAsyncDisposable` + `using`/`await using` +
`await foreach` disposal; `AsyncLocal<T>`; `IServiceProvider.GetService` in the
base BCL, while DI scopes/registration are `Microsoft.Extensions.*` (inventory F7).

**Sufficiency.** Resource lifetime is fully covered by the language forms;
`Effect.Using`/`UsingAsync` (`inv-funny-sharp-core.md`, `EffectResourceExtensions`)
re-package `using`/`await using` into the effect algebra and are **duplicate-risk**
unless the decision record can show that the effect ordering/resource lifetime
guarantee is otherwise hard to express (it is not, for a single resource).

**Additive value.** The deferred effect (`Effect<T>.RunAsync`, `Bind`, `Map`,
`WithEnvironment`, `Provide`) plus `TimeProvider`-based timeouts is not a BCL
feature: there is no deferred-computation type, no typed environment, and no
retry/timeout policy in the base BCL. `Provide` returning `Effect<T>` after
injecting `TEnvironment?` is ordinary DI-by-parameter, which is the contract
(`docs/product-contract.md §"Package And Dependency Boundary"`). Compile-time verification is limited: the
environment type is a type parameter, but effect reuse/caching semantics are
documented behavior, not types.

**Semantic LOC.** N composed effect steps with a shared environment and one
`RunAsync` are shorter than N `await` calls threaded with a parameter; the value
disappears for one-off calls.

**Performance reference.** A direct method call / `await` for the same work; an
`Effect` adds one struct + delegate invocation per `RunAsync`. Existing receipts:
`benchmarks/FunnySharp.Benchmarks/EffectBenchmarks.cs`
(`eng/performance/baseline.json:9`).

**Verdict: additive-value** for deferred composition/environment/timeout;
**duplicate-risk** for resource-scoping wrappers around `using`/`await using`.

## F8 — State transitions and machines

**BCL baseline.** Nothing. `IAsyncStateMachine` is compiler plumbing (inventory F8).

**Sufficiency.** Nothing in the BCL competes, so no duplicate risk. Ordinary C#
alternatives are a `switch` expression over an enum state returning a record
(`StateChange<TState,TOutput>`) or a `(state, evt)` tuple. FunnySharp's
contribution is vocabulary (`StateTransition<TState,TOutput>`,
`StateMachine<TState,TEvent,TOutput,TError>`) plus `TransitionResult` with four
statuses (`Applied`, `Rejected`, `Failed`, `Undefined`) and a `Match` that forces
all four. Compile-time verifiability is partial: the transition function is a
delegate, so illegal transitions are runtime behavior of the user's function, not
type errors. `Replay` is a straightforward fold.

**Semantic LOC.** A switch-based transition returning a typed result is close in
size; the win is naming (`Rejected` vs `Failed` vs `Undefined`) and replay
composition, and it shrinks if the carrier requires conversion to/from domain
state before use.

**Performance reference.** `(state, evt) => new StateChange<...>(...)` delegate or
a `switch` expression; `Replay` is O(events) with one delegate call per event.
Existing receipts: `benchmarks/FunnySharp.Benchmarks/StateMachineBenchmarks.cs`
(`eng/performance/baseline.json:18`).

**Verdict: additive-value** (nothing to duplicate), with the general-union
deferral (`docs/product-contract.md §"Deliberate Deferrals"`) as the main future pressure.

## F9 — Optics and immutability

**BCL baseline.** `System.Collections.Immutable` (immutable array/list/dictionary/
set/sorted variants, builders, `ImmutableInterlocked`), `FrozenDictionary`/
`FrozenSet` for read-optimized snapshots, concurrent collections
(inventory F9). The product contract already delegates all immutable update
plumbing to these (`docs/product-contract.md §"Product Direction"`).

**Sufficiency.** Any FunnySharp wrapper around immutable/frozen collections would
duplicate a complete BCL universe — **duplicate-risk**. The `Lens<TSource,TFocus>`
+ `Optional<TSource,TFocus>` pair (7 public members total, `inv-funny-sharp-core.md`)
has no BCL counterpart, so it is additive; the contract deliberately keeps it
delegate-based with caller-owned laws (`docs/product-contract.md §"Product Direction"`).

**Compile-time verifiability.** None beyond `Lens<T,T>` identity typing: laws are
documented obligations. `Get`/`Set`/`Update` cannot verify that the focus actually
exists for `Lens` (it can throw or produce `default`).

**Semantic LOC.** `lens.Update(entity, f)` vs `entity with { Prop = f(entity.Prop) }`
is LOC-neutral for a direct record property, positive for deeply nested focus
paths or when a lens is passed around as a value. `with` remains the idiomatic
baseline, and for `ImmutableDictionary` the honest baseline is
`dict.SetItem(key, value)`.

**Performance reference.** `record` `with` expression (shallow copy) for records;
`ImmutableDictionary.SetItem`/`SetItems`/`ToBuilder` for immutable collections;
`FrozenDictionary.TryGetValue` for lookups; `ImmutableArray<T>.AsSpan()` for
zero-copy reads. `Lens.Get`/`Set` are delegate invocations, so the lens path must
not add more than one delegate call per focus hop. Existing receipts:
`benchmarks/FunnySharp.Benchmarks/ImmutableUpdateBenchmarks.cs`
(`eng/performance/baseline.json:12`).

**Verdict: additive-value** for the two optics types; **duplicate-risk** for any
collection wrapper or "persistent collection universe".

## F10 — HTTP integration

**BCL/framework baseline.** `IResult` (single `ExecuteAsync(HttpContext)`),
`Results`/`TypedResults` (68 factories each), typed-results unions
`HttpResults.Results<T1..T6>`, `ProblemDetails`/`HttpValidationProblemDetails`,
`IProblemDetailsService`, Minimal API validation (`AddValidation`,
`DisableValidation`), `TypedResults.ServerSentEvents(IAsyncEnumerable<SseItem<T>>)`
with `SseItem<T>`/`SseParser<T>` in the base BCL, and OpenAPI 3.1/YAML/XML
comments (inventory F10). The framework reference enters from
`src/FunnySharp.AspNetCore/FunnySharp.AspNetCore.csproj:23`.

**Sufficiency.** Everything HTTP-shaped except the carrier mapping already exists
in the framework. FunnySharp.AspNetCore's 15 members map `Option`/`Result`/
`Validation`/`Effect` to `IResult` with caller-supplied `ProblemDetails`
factories — that is a **necessary-bridge**, and it must stay thin, return
`IResult`, and not define its own problem-details/result hierarchy
(**duplicate-risk** if it does).

**Compile-time verifiability.** The adapter's `Func<...>` factories force the
caller to define the HTTP mapping; the return type `IResult` erases the endpoint's
response shape, so it does not contribute to OpenAPI inference the way
`Results<T1,T2>`/`TypedResults` do. If OpenAPI accuracy matters, a typed-results
variant would be the additive step (a decision for the lead, not taken here).

**Async/cancellation semantics.** The `Effect`-based overloads pass
`context.RequestAborted` into `RunAsync`
(`src/FunnySharp.AspNetCore/HttpResultExtensions.cs:191,212,233,256,280,304`),
which is the correct BCL/framework integration. The `Task<T>`/`ValueTask<T>`
overloads rely on the caller's token (none in the signature). Whether the adapter
observes `RequestAborted` when the caller passes `CancellationToken.None` cannot
be enforced by types; it is a documented-behavior risk.

**Performance reference.** `TypedResults.Ok(...)`/`Results.Problem(...)` directly;
`IProblemDetailsService` for consistent error rendering. The mapping adds one
delegate call + `ProblemDetails` allocation only on the failing branch; the
success path should be allocation-neutral relative to `Results.Ok(value)`.

**Verdict: necessary-bridge.**

## F11 — Cross-cutting: vocabulary, stability, performance, serialization, packaging

**BCL baseline.** `Stopwatch` + `GC.GetTotalAllocatedBytes`/
`GetAllocatedBytesForCurrentThread` for measurement; trimming/AOT annotations
(`RequiresUnreferencedCode`, `DynamicallyAccessedMembers`, `RequiresDynamicCode`,
`UnconditionalSuppressMessage`) and `RuntimeFeature.IsDynamicCodeSupported`;
`System.Text.Json` with the .NET 10 `Strict` preset and
`AllowDuplicateProperties`; `RuntimeHelpers`/`Unsafe` for boxing-avoidance;
`StringSyntax`/`ConstantExpected`/`Experimental` for compiler-visible hints
(inventory F11).

**Sufficiency.** The BCL covers every mechanism; FunnySharp's cross-cutting
contribution is vocabulary (one canonical carrier per family) and policy
(stability boundary, JSON policy for carriers, allocation budget). That makes it a
**necessary-bridge** rather than a new capability. Two concrete obligations follow
from the baseline:

1. Performance claims must be measured against the BCL references named per family
   above, using `GC.GetAllocatedBytesForCurrentThread` deltas and
   `Stopwatch.GetTimestamp`, with the receipt format already in
   `eng/performance/baseline.json` (`benchmarks/FunnySharp.Benchmarks/*.cs`).
2. Serialization policy: the BCL has no converters for `Option`/`Result`/
   `Validation`/`Effect`, and `System.Text.Json` will serialize their public
   properties (e.g. `Result.IsSuccess`) rather than a domain shape unless a
   converter or DTO policy exists. `JsonSerializerOptions.Strict` (disallow
   unmapped members and duplicate properties) is the .NET 10 tool that makes a
   DTO policy enforceable; a "serialize the DTO, not the carrier" rule needs no
   new package.

**Compile-time verifiability.** `IsTrimmable` is a package claim, not a compiler
switch (`docs/product-contract.md §"Trimming And Native AOT Policy"`); trim/AOT behavior is verified by
analytic and execution evidence, not by types. No BCL analyzer enforces allocation
or async rules; first-party analyzers are deferred
(`docs/product-contract.md §"Analyzers And Compiler Feedback"`), and Funcky's analyzer package is the
comparable external capability (out of scope for this memo).

**Verdict: necessary-bridge.**

---

## Cross-family observations

1. **Two error channels, one cancellation convention.** The BCL's only universal
   failure channel is the exception; `Result`/`Validation` add value only if the
   boundary between them and exceptions is canonical (criterion 5, AI
   predictability). The code already keeps `OperationCanceledException` out of
   the value channel (`src/FunnySharp/Result.cs:40,57,80,105,129`), and the
   ASP.NET adapter forwards `RequestAborted`
   (`HttpResultExtensions.cs:191-304`); the remaining decision is documentation
   and consistency, not new BCL capability.
2. **.NET 10 async LINQ changed the F5/F6 baseline.** `System.Linq.AsyncEnumerable`
   ships in the 10.0.11 ref pack with 188 members and `CancellationToken`-aware
   `ValueTask` overloads, so any FunnySharp async helper that only renames a
   standard operator is now clearly duplicate-risk. The additive helpers are the
   ones whose semantics the BCL does not have: traversal over carriers, bounded
   ordered streaming, and first-success with drain.
3. **`IAsyncEnumerable` is now the mainstream streaming carrier.**
   `TypedResults.ServerSentEvents` consumes it, async LINQ operates on it, and
   `Parallel.ForEachAsync` accepts it. Any FunnySharp streaming API should be an
   extension over `IAsyncEnumerable<T>` with `[EnumeratorCancellation]` and
   `ConfigureAwait(false)` (as the current code is), not a new stream type.
4. **The framework boundary is already correct.** Core has no package references
   (`src/FunnySharp/FunnySharp.csproj`, empty dependency group per
   `docs/product-contract.md §"Package And Dependency Boundary"`); HTTP types live only in the ASP.NET Core
   package via `FrameworkReference` (`src/FunnySharp.AspNetCore/FunnySharp.AspNetCore.csproj:23`).
   `IServiceProvider` is the only DI type in the base BCL; scopes are not, so
   "environment as caller-provided value" is the only BCL-first DI model.
5. **Everything additive is small and carrier-specific.** The total additive
   surface implied by this survey is: carrier-aware sync/async traversal
   (F2/F3/F5/F6), bounded ordered parallel streaming and first-success (F6),
   deferred effects with environment/timeout (F7), state-transition vocabulary
   (F8), a 2-type optics surface (F9), and one HTTP mapping (F10). Everything else
   in F1/F4/F5/F9/F11 has a competent BCL or language baseline.

## Open questions and UNVERIFIED judgments

- **UNVERIFIED**: whether `Option<T>.Some(default)` normalizes to `None`; the dump
  shows `Some(T? value)`, which would otherwise admit null. Affects F1
  compile-time soundness.
- **UNVERIFIED**: whether `Validation.InvalidMany(empty)` can construct a
  non-empty-invalid-state hole; the signatures allow an empty
  `IEnumerable<TError>`.
- **UNVERIFIED**: ordering guarantee of `SelectParallelValueAsync` and
  disposal-of-upstream behavior when the consumer stops early; the product
  contract claims ordered results and Channel backpressure
  (`docs/product-contract.md §"Product Direction"`) but this survey did not execute the code.
- **UNVERIFIED**: whether `TraverseParallelValueAsync` continues draining after a
  fail-fast failure or cancels siblings first; the distinction matters for
  criteria 7 (no orphaned work) and is not visible from signatures.
- **UNVERIFIED**: whether `FirstSuccessAsync` reports "all attempts failed" and
  "caller cancelled" through distinguishable outcomes in every path; the code has
  substantial cancellation bookkeeping (`ConcurrentEffectExtensions.cs:95-392`).
- **UNVERIFIED**: exact `Result.Try*` behavior for non-`Exception`-derived fault
  mechanisms (none exist in .NET) and for `AggregateException` unwrapping; not
  executed.
- **UNVERIFIED**: which .NET 10 preview moved `System.Linq.AsyncEnumerable` in-box
  and which operators are new versus carried from the former package; the 10.0.11
  ref-pack presence is verified, history is not.
- **UNVERIFIED**: whether ASP.NET Core Minimal API OpenAPI inference improves when
  an endpoint returns `Results<T1,T2>` versus `IResult` from the adapter; the
  Learn pages document OpenAPI 3.1/XML comments but the adapter's `IResult` return
  type suggests no inference. Not tested.
- **UNVERIFIED (decision-adjacent)**: no representative FunnySharp call sites were
  compared side-by-side in this memo; semantic-LOC claims are estimates pending
  `docs/next-stage/call-sites.md`.

## Not examined

- FSharp.Core, Funcky, CSharpFunctionalExtensions, language-ext (other
  workstreams); no comparison to their baselines is made here.
- Runtime execution or benchmarking of any BCL API; all performance references are
  the operations to measure against, not measured numbers.
- Non-base BCL assemblies (HTTP, regex, numerics, crypto, diagnostics/metrics) and
  the full ASP.NET Core surface; inventory F10 is doc-based except for the 11
  dumped HTTP types.
- C# 14/15 language roadmap beyond the union deferral already in the product
  contract; a future union feature could alter F1/F2/F3/F8 decisions and is
  deliberately out of this baseline.
- The FunnySharp test suite and generated documentation (`docs/*.md` other than the
  product contract); behavior claims were read from source/dumps only where cited.
