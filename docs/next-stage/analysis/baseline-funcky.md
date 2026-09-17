# Baseline analysis — Funcky 3.6.0 (Goal 14 workstream)

Inputs: `docs/next-stage/inventory/funcky.md` (this repo), the generated dumps
`docs/next-stage/inventory/generated/inv-funcky.md` / `inv-funcky-analyzers.md`, the pinned
package `/tmp/opencode/baselines/funcky.3.6.0.nupkg`
(SHA256 `1a7ab6c6595a2f4d3bdfa83768f9054450beaffd322817644f3dea65940cc32f`), and the clone
`~/repos/funcky` HEAD `133ba5bac4cb57861a3b2fbea49a98f0aa9a953a` (`3.6.0-32-g133ba5ba`).

Decision vocabulary: **adopt** (take the capability as-is or near-as-is), **adapt** (take the
capability, change carrier/naming/semantics), **defer** (plausible but not now; record trigger),
**reject** (do not take). Criteria are the numbered Goal 14 dimensions (1 usefulness,
2 semantic LOC, 3 signature clarity, 4 compile-time verifiability, 5 AI predictability,
6 BCL interop, 7 async/cancellation, 8 performance feasibility). Judgments not verifiable from
the pinned evidence are marked **UNVERIFIED**.

## 1. Decision summary

| # | Funcky capability (pin) | Decision | Driving criteria |
| --- | --- | --- | --- |
| F1.1 | `Option.Some/Return/FromNullable` model | keep FunnySharp; **reject** `where TItem : notnull` | 4, 6 |
| F1.2 | `Option.FromBoolean(bool[, item/selector])` | **adapt** (value/factory overloads; bool-only needs `Unit`) | 1, 2, 3, 5 |
| F1.3 | `Option.ToNullable` (marker-class overloads) | **adapt** (plain constrained overloads) | 2, 6 |
| F1.4 | `OptionEqualityComparer`/`OptionComparer`/`IComparable` | **defer** | 1, 2, 5 |
| F1.5 | `OptionJsonConverter` (None⇒null, transparent Some) | **adapt** (manual registration; AOT-clean design required) | 6, 1, 8 |
| F1.6 | `await Option<Task<T>>` / `Option<ValueTask<T>>` | **defer** | 3, 5, 8 |
| F1.7 | `Option<T>` list-pattern support + λ0003 | **defer** to Goal 21; pattern is prior art | 4, 5 |
| F1.8 | λ0001 restriction of `TryGetValue` | **adapt** in Goal 21 | 1, 3, 5 |
| F1.9 | `DownCast`/`UpCast` monad casting | **adapt** (Option/Lazy/Result via existing error factory; reject Either variant) | 4, 5 |
| F2.1 | `Result<T>` with `Exception` channel + stack-trace side effect | **reject** | 3, 4, 6 |
| F2.2 | `Either<TLeft,TRight>` | **defer** (triggers: real two-valid-outcomes consumers) | 2, 5 |
| F2.3 | `EitherOrBoth` / `ZipLongest` | **defer** (use `(TLeft?, TRight?)`/`Option` pair if needed) | 1, 5, 8 |
| F2.4 | `Result.GetOrThrow` | **defer** (needs error→exception policy) | 3, 6 |
| F3.1 | accumulation (`Validation`, applicative) | nothing in Funcky; FunnySharp keeps | — |
| F4.1 | `Identity`, parameterless `NoOperation`, `Not` | **adopt** | 2, 5 |
| F4.2 | `True`/`False` constant predicates, `All`/`Any` folds | **defer** | 1, 5 |
| F4.3 | `Curry`/`Uncurry`/`Flip` beyond arity 2; `Apply` over `Unit` (81 overloads) | **reject** | 1, 5, 8 |
| F4.4 | `Compose` receiver direction | **reject** (keep FunnySharp's) | 5 |
| F4.5 | `Unit`, `Discard.__` | **defer** / **reject** `Discard` | 1, 6 / 5 |
| F4.6 | `Sequence.Return/FromNullable/Successors` (sync+async) | **adapt** (small set) | 2, 3, 5 |
| F4.7 | `Cycle`, `CycleRange`/`RepeatRange` (`IBuffer`), `CycleMaterialized`, `Concat` | **reject** | 1, 5, 8 |
| F5.1 | `FirstOrNone/LastOrNone/SingleOrNone/ElementAtOrNone/None(predicate)` | **adopt** under one naming rule | 2, 3, 4, 6 |
| F5.2 | `MinOrNone/MaxOrNone` | **adopt**; `AverageOrNone` overload towers | 2 / **reject** 5, 8 |
| F5.3 | Container Try→Option bridges (Dictionary/List/ImmutableList/OrderedDictionary/Queue/PriorityQueue/Stream/HttpHeaders/JsonSerializerOptions/Enumerator/Queryable) | **adopt**, trimmed overloads | 2, 3, 4, 6 |
| F5.4 | `Parse*OrNone` bridges (~200) | **adapt** (common set + generic `IParsable`) | 2, 3, 6, 8 |
| F5.5 | `WhereSelect` | **reject** (duplicate of FunnySharp `Choose`) | 5 |
| F5.6 | `WhereNotNull` | **adopt** | 2, 5 |
| F5.7 | `Inspect`, `Pairwise`, `SlidingWindow` | **adopt** / **adopt** / **adapt** | 2, 3, 8 |
| F5.8 | `Split`, `Intersperse`, `Interleave`, `TakeEvery`, `Transpose`, `AdjacentGroupBy`, `InspectEmpty` | **defer** | 1, 2 |
| F5.9 | `PowerSet`, `Shuffle`, `Chunk`, `JoinToString`/`ConcatToString`, `Materialize`, `Range` enumeration | **reject** | 1, 6, 8 |
| F5.10 | `Memoize` + `IBuffer`/`IAsyncBuffer` public carrier | **defer** with hard conditions; carrier **reject** | 7, 8, 3 |
| F5.11 | k-way `Merge` | **defer** | 2, 8 |
| F5.12 | `Partition` (predicate / Result / Either) | **adopt** predicate + typed-result carriers; skip Either | 2, 3, 6 |
| F5.13 | `ValueWithIndex/First/Last/Previous` + `WithX` | **defer** | 1, 5 |
| F5.14 | `SplitLazy`, string `IndexOfOrNone` family | **adapt** / **adopt** | 2, 8 / 2, 6 |
| F5.15 | `Sequence`/`Traverse` fail-fast | keep FunnySharp (already superset with Validation) | — |
| F6.1 | async `...Await.../WithCancellation` grammar | **adopt** the grammar; FunnySharp already uses it | 5, 7 |
| F6.2 | async OrNone/Partition/Materialize mirrors | **adopt** sync set only | 2, 7 |
| F6.3 | `Merge` async, async `Memoize` | **defer** / **reject** (token loss) | 8 / 7 |
| F6.4 | `ShuffleAsync` | **reject** | 1, 8 |
| F7.1 | retry policies (`IRetryPolicy`, Constant/Linear/Exponential) | **adapt** (TimeProvider, one delay-index rule, cap/jitter hooks) | 7, 3, 8 |
| F7.2 | `Functional.Retry` sync (`Thread.Sleep`), unbounded `Retry(producer)` | **reject** | 7, 1 |
| F7.3 | `Reader<TEnvironment,TResult>` delegate | **reject** (overlaps `Effect<TEnvironment,T>`) | 5, 3 |
| F9.1 | `Lazy` static class + `System.Lazy` monad extensions | **defer** (annotation debt) | 1, 8 |
| F11.1 | analyzer packaging in the main package | **adopt** shape, with mitigations | 4, 5, 11 |
| F11.2 | `NotConfigurable` Error diagnostics | **reject** as a default; reserve for impossible states | 1, 5 |
| F11.3 | `PublicAPI.Shipped/Unshipped` + PublicApiAnalyzers | **adopt** as process | 4, 11 |
| F11.4 | multi-TFM support + `System.Text.Json 5.0.2` / `System.Collections.Immutable 1.7.1` pins | **reject** | 6, 8 |
| F11.5 | `FunckyImplicitUsings` build targets | **reject** | 5 |
| F11.6 | internal source generator for repetitive bridge APIs | **adopt** as internal technique | 5, 8 |

## 2. Rationales

### F1 absence

- **F1.1 `notnull` constraint — reject.** Funcky requires `where TItem : notnull` on `Option`,
  `Either`, `Result` and throws `ArgumentNullException` in `Some`/constructors
  (`~/repos/funcky/Funcky/Monads/Option/Option.Core.cs:8,17-22,100-107`). The constraint does not
  make absent-vs-null impossible: `Option<T>` for an unconstrained `T` still exists in generic
  code, and `Option<string?>` is expressible through `default`. FunnySharp's contract is
  null-collapsing (`Map` turns a null selector result into `None`,
  `src/FunnySharp/Option.cs:162-166`), and adding the constraint would be a source-breaking
  annotation change for consumers plus a `notnull` obligation on all Funcky-shaped factory
  APIs (criterion 4 adds nothing measurable; criterion 6 loses nullable-flow interop).
  **Do not inherit** Funcky's constraint/annotation stack.
- **F1.2 `FromBoolean` — adapt.** Removes the `condition ? Option.Some(x) : None` pattern
  (criteria 1, 2), the name states semantics (3), and the selector overload defers the value
  computation until the condition is true (8, no wasted allocation). FunnySharp has no `Unit`,
  so only the `(bool, TItem)` and `(bool, Func<TItem>)` overloads are directly portable; the
  `bool → Option<Unit>` overload should wait for the `Unit` decision (F4.5). Naming: Funcky
  uses `FromBoolean`; FunnySharp's existing grammar is `FromNullable`/`FromTry`, so
  `FromBoolean` is consistent (5).
- **F1.3 `ToNullable` — adapt.** One line vs `option.Match(none: (T?)null, some: v => v)`
  (2). Funcky's implementation needs `RequireClass<T>`/`RequireStruct<T>` marker parameters to
  make the two overloads applicable and disambiguated
  (`inv-funcky.md:1279-1282`, `~/repos/funcky/Funcky/RequireClass.cs`). FunnySharp's `Option<T>`
  is unconstrained, so plain `where T : struct` / `where T : class` extension overloads are
  unambiguous for concrete `T` and fail predictably for unconstrained `T` — adopt the
  capability, reject the marker classes (criterion 5: no leaked internal trick).
- **F1.4 comparers/ordering — defer.** Default structural equality already exists in
  FunnySharp (`Option<T> : IEquatable<Option<T>>`). Custom `OptionEqualityComparer`/
  `OptionComparer`/`IComparable<Option<T>>` add 6 public types and semantics (`None < Some(x)`,
  comparer-based item comparison, `ArgumentException` when items are not comparable) for a use
  case with no observed demand (criteria 1, 2, 8). Trigger for reconsideration: a consumer that
  needs `Option<T>` as a dictionary key under a custom item comparer or as a sorted key.
- **F1.5 JSON — adapt.** `None ⇒ null`, `Some(v) ⇒ v`, read `null ⇒ None` is the natural
  mapping and matches `System.Text.Json` (6, 1). Two Funcky properties must not be inherited:
  (a) the converter must be explicitly registered; if FunnySharp ever ships one, an
  opt-in `FunnySharpJsonSerializerOptions` extension or attribute-free registration helper is
  safer than auto-injection into every `JsonSerializerOptions` (criterion 6: no hidden global
  state); (b) `JsonConverterFactory` + `MakeGenericType` + `Activator.CreateInstance` is
  annotated `[RequiresDynamicCode]` in Funcky (`Option/OptionJsonConverter.cs:9-11`), which is
  incompatible with an unqualified AOT claim (8). If adopted, either accept and document the
  `RequiresDynamicCode` boundary or use a closed-generic converter strategy. **UNVERIFIED:**
  whether a closed-generic converter can be produced without reflection in .NET 10 for
  arbitrary `Option<T>`; no measurement/experiment was run.
- **F1.6 awaitable `Option<Task<T>>` — defer.** It removes `await option.Match(...)`-style
  ceremony (2) and `IsCompleted = true` for `None` is sound (7). But it adds 8 public awaiter
  types plus `ConfigureAwait` overloads (`inv-funcky.md:1234-1250`, `:1295-1317`) and a second
  way to await through a wrapper (5). FunnySharp's `ToOptionAsync` already covers
  `Task<T> → Option<T>`; measure demand first. Trigger: recurring `Option<Task<T>>` consumers.
- **F1.7 list patterns — defer.** `Count` + indexer exist only for `is [var x]` patterns and
  require λ0003 to prevent misuse (`Option/Option.ListPattern.cs:14-33`,
  `SyntaxSupportOnlyAnalyzer.cs:15-21`). The syntax is niche and semantically ambiguous
  (`Option<T>` is not a collection; criterion 5). The *attribute + analyzer* mechanism is
  excellent Goal 21 prior art (see §4).
- **F1.8 `TryGetValue` restriction — adapt in Goal 21.** FunnySharp exposes
  `Option<T>.TryGetValue` with no guard (`src/FunnySharp/Option.cs:115`). Funcky keeps the
  escape hatch for three imperative shapes (loop, iterator `if`, `catch when`) and makes every
  other use an Error (criteria 1: the escape hatch survives; 3: `Match`/`Bind` become the
  obvious path; 5: one canonical extraction story). Adaptation over adoption: FunnySharp's
  XML docs and tests already depend on it, so Goal 21 should ship it as Warning-by-default or
  opt-in severity first, and the analyzer must not fire inside `FromTry`-style library code
  (Funcky excludes library internals by the operation shape, not by assembly).
- **F1.9 casting — adapt.** `UpCast<TResult>` constrains `where TItem : TResult`, giving
  compile-time proof (4) and removing `Select(x => (TResult)x)` noise (2). `DownCast<TResult>`
  for `Option` (`None` on failed cast) is useful. Reject two Funcky shapes: `DownCast.From(Result)`
  fabricating a new `InvalidCastException` (the caller cannot supply the error; 3, 4) and
  `DownCast.From(Either, Func<TLeft> failedCast)` (a thunk parameter on a conversion; 3).
  FunnySharp's `Result<T,TError>` can accept `DownCast.From(Result<TItem>, Func<TResult, TError>)`
  or similar; design in implementation goal.

### F2 fail-fast

- **F2.1 `Result<T>` / stack-trace side effect — reject.** Funcky's error channel is
  `Exception` and `Error()` mutates the exception's stack trace when absent (inconsistent with
  a value type; 3, 4) and does not survive serialization/marshalling contracts (6). FunnySharp's
  typed `Result<TValue,TError>` is strictly more expressive for business failures, and its
  `Try` boundary already preserves the original exception (`src/FunnySharp/Result.cs:18-49`).
  **Do not inherit** either the `Exception` carrier or the stack-trace mutation.
- **F2.2 `Either` — defer.** The capability (two-case value without error semantics) is real,
  but FunnySharp would then carry `Option`, `Result<T,E>`, `Validation<T,E>`, and `Either<TL,TR>`
  as four overlapping two-case carriers (criterion 5: canonical vocabulary; criterion 2: the
  LOC win over `Result<T,E>` is only real when the left case is not an error and does not need
  mapping). Funcky's `Either` is also `[NonDefaultable]` and throws `NotSupportedException`
  from `default` (`Either.Core.cs:8-10,117-137`) — a runtime-detection design (4) that
  FunnySharp should not copy. Trigger: a concrete domain with two valid outcomes and no
  failure semantics (e.g. `Found`/`NotFound` pipelines).
- **F2.3 `EitherOrBoth`/`ZipLongest` — defer.** Three-way `Match` plus a new struct for the
  "zip of different lengths" case; a `(TLeft?, TRight?)`/`Option` pair or the existing
  `Zip` + `Option` covers most of it (1, 5). Trigger: interleaving two ragged streams.
- **F2.4 `GetOrThrow` — defer.** FunnySharp has no throw-from-Result API; adding one needs a
  policy for mapping `TError` to `Exception` (e.g. `Func<TError, Exception>`), otherwise it
  silently throws `Exception` (3, 6). `TransitionResult`/`Result` consumers already call
  `Match`/`Recover`.

### F3 accumulation

Funcky has no accumulation capability; its `Sequence`/`Traverse` are fail-fast
(`EnumerableExtensions/Sequence.cs:40-60`). There is no external capability to adopt here.
This validates FunnySharp's `Validation<TValue,TError>` + `Apply` + accumulating `Traverse` as
a differentiator; keep. One comparison finding for the lead's API matrix: FunnySharp's
`Result<TValue,TError>` and `Validation<TValue,TError>` are *default-representable* invalid
values (`new Result<T,E>()` = failure with `default` error, `src/FunnySharp/Result.cs:358-392`;
`new Validation<T,E>()` = invalid with a synthetic `[default!]` error,
`src/FunnySharp/Validation.cs:12-28,289`). Funcky's answer to the same problem is
`[NonDefaultable]` + analyzer λ1009 in its optional analyzer package — see §4.

### F4 function grammar

- **F4.1 `Identity`/`NoOperation`/`Not` — adopt.** Each replaces a lambda that would otherwise
  be spelled out (`GetOrElse(Identity)`, `Tap(NoOperation)`, `Where(Not(p))`) and the names are
  unambiguous (2, 5). Keep arity 1 only: Funcky's 0–8-arity `NoOperation`/`True`/`False`
  families (`inv-funcky.md:149-188`) are overload-resolution and API-surface noise.
- **F4.2 `All`/`Any` predicate folds — defer.** `p1` AND `p2` is already clearer as
  `x => p1(x) && p2(x)`; the fold hides short-circuiting composition behind a params array
  (5). Trigger: predicate composition appears repeatedly in real consumer code.
- **F4.3 arity-8 combinator families / `Apply` over `Unit` — reject.** 81 `Apply` overloads,
  `Curry`/`Uncurry`/`Flip` to arity 8, and `ω1..ω8` parameter names
  (`inv-funcky.md:83-134,150-153,625-699`) are the clearest over-engineering in the baseline:
  API-surface and overload-resolution cost (5), reader cost (3), and no measured LOC win beyond
  arity 2–3 (2). FunnySharp keeps 2-arity `Curry`/`Uncurry`/`Flip`/`Partial` and stops there.
- **F4.4 `Compose` direction — reject.** Funcky's receiver is the *second* function
  (`f.Compose(g)` applies `g` then `f`, `Extensions/FuncExtensions/Compose.cs`); FunnySharp's
  `first.Compose(second)` reads left-to-right. Having both would create an
  ambiguity that no signature clarifies (3, 5). Keep FunnySharp's.
- **F4.5 `Unit`/`Discard` — defer/reject.** There is no BCL `void` value; Funcky's `Unit`
  exists for `Option<Unit>` from booleans, `ForEach` returns, and `Func<T,Unit>` conversion
  (`Unit.cs:3-40`). FunnySharp's `Action`/`Task` carriers make most uses unnecessary
  (6). If `FromBoolean(bool)` (F1.2) is wanted, `Option<bool>` or `Option<ValueTuple>` avoids a
  new public type; decide in the implementation goal. `Discard.__` is rejected: the name
  collides with the discard token in spirit, is undiscoverable, and `_ => ...` already exists
  (5).
- **F4.6 sequence constructors — adapt.** `Sequence.Return(x)`, `FromNullable`, and
  `Successors(first, next)` remove `new[] { … }`/`yield` boilerplate and are the natural seeds
  of `Sequence`/`Traverse` (2, 3, 5). Adaptations: `Return` should return the same carrier for
  all overloads (Funcky returns `IReadOnlyList<T>` for `Return` while `Cycle` returns
  `IEnumerable<T>`, `inv-funcky.md:244-249` — inconsistent materialization promises, 3);
  provide the async `Successors` with `ValueTask<Option<T>>`.
- **F4.7 infinite/buffered constructors — reject.** `Cycle`, `CycleRange`, `RepeatRange`,
  `CycleMaterialized`, `RepeatMaterialized` (`Sequence/Sequence.CycleRange.cs`) introduce the
  public `IBuffer<T>` carrier, unbounded buffering of any lazy source, deferred
  `InvalidOperationException` for empty input, and disposal obligations (3, 5, 8). `Concat` is
  covered by `Enumerable.Concat`/`Append` (6).

### F5 collections

- **F5.1 optional cardinality — adopt.** `FirstOrNone`, `LastOrNone`, `SingleOrNone`,
  `ElementAtOrNone`, `None(predicate)` replace `FirstOrDefault`-plus-null-checks and preserve
  the "was there an element?" answer that `OrDefault` erases (2, 3, 4, 6). Naming is the one
  open vocabulary decision (see §3). Adopt the predicate overloads; reject the
  Option-returning-selector overload variants and per-element-type `AverageOrNone` towers (5).
- **F5.2 min/max — adopt; average towers — reject.** `MinOrNone`/`MaxOrNone` (and ByKey
  variants) have no BCL equivalent that distinguishes empty from default (2). `AverageOrNone`
  duplicates 10 numeric overloads × {source, selector, Option-selector} for sync and the same
  ×3 for async (`inv-funcky.md:510-529`); the generic `Average` semantics differ per type, so a
  single generic overload cannot replace them; defer the whole group until a real consumer
  needs averaging-with-emptiness (1, 5, 8).
- **F5.3 container bridges — adopt (trimmed).** This family is Funcky's highest-value
  contribution against criterion 2: each `…OrNone` deletes a `Try`-call + `if` + out variable
  and keeps the negative case in the type system. Highest value: `Dictionary.GetValueOrNone`
  (+ `RemoveOrNone`), `Enumerator.MoveNextOrNone`, `Queue/PriorityQueue
  DequeueOrNone/PeekOrNone`, `Stream.ReadByteOrNone` and friends (they correctly translate
  `NotSupportedException`/`-1` into `None`), `ImmutableList.IndexOfOrNone`,
  `HttpHeaders.GetValuesOrNone`, `JsonSerializerOptions.GetTypeInfoOrNone` (STJ metadata lookup
  is exactly a Try pattern), and `IQueryable` variants with `Expression` predicates (EF-shaped;
  6). Adaptations: trim the 5-overload `IndexOfOrNone` shapes to the 1–2 common ones, and keep
  the `OverloadResolutionPriority(1)` trick that fixes the `Dictionary` dual-interface
  ambiguity (`Extensions/DictionaryExtensions.cs:20`; the regression is pinned by
  `Funcky.Test/Extensions/DictionaryExtensionTest.cs:20-24`). **UNVERIFIED:** that FunnySharp's
  `IQueryable` adapters translate correctly under EF Core; this needs an execution test.
- **F5.4 parse bridges — adapt.** `…OrNone` parse bridges are the canonical Try-pattern
  replacement, remove `if (int.TryParse(...))` ceremony (2), and never throw (3). Rejecting the
  ~200-signature zoo in favour of ~15 common types plus the generic
  `IParsable<T>`/`ISpanParsable<T>`/`IUtf8SpanParsable<T>` bridges (net10 BCL; 6) keeps the
  surface predictable for agents (5) and avoids the generator machinery unless the generic
  overloads prove insufficient (8: zero reflection). Funcky's own generic overloads show the
  pattern (`Extensions/ParseExtensions/ParseExtensions.GenericParseable.cs`). Defer the
  `System.Net.Http.Headers.*` parse bridges (20 types; niche; 1).
- **F5.5 `WhereSelect` — reject.** Same capability as FunnySharp's `Choose`; taking
  `WhereSelect` would create two names for one operation (5). Note `WhereSelect(source)`
  (identity over `IEnumerable<Option<T>>`) is `Choose(identity)` and should not be added
  separately.
- **F5.6 `WhereNotNull` — adopt.** One call replaces
  `Choose(Option.FromNullable)`, and the name states the filter (2, 5). Adopt for
  `IEnumerable<T?>`, `IAsyncEnumerable<T?>`, class and struct overloads.
- **F5.7 deferred combinators — adopt `Inspect`/`Pairwise`; adapt `SlidingWindow`.** These
  three have no BCL equivalent and small, well-documented semantics (2, 3). Funcky accumulates
  as a result (`Inspect` = `Select` with side effect, deferred until enumeration,
  `Extensions/EnumerableExtensions/Inspect.cs:9-18`) — keep the deferred contract explicit.
  `Pairwise` disposes the enumerator and yields `Count-1` pairs (`Pairwise.cs:22-30`); adopt
  the result-selector overload. `SlidingWindow(width)` returns full-width
  `IReadOnlyList<T>` windows and yields nothing when the source is shorter
  (`SlidingWindow.cs:20-38`); adapt: document that each yielded window is a fresh copy (8:
  bounded per-window allocation) and keep the strict "full window only" rule.
- **F5.8 remaining combinators — defer.** `Split`, `Intersperse`, `Interleave`, `TakeEvery`,
  `Transpose`, `AdjacentGroupBy`, `InspectEmpty` are all correct, but each adds a public
  surface and a reason to enumerate before consumers need it (1, 2, 5). Trigger: a concrete
  consumer workflow for each.
- **F5.9 duplicates of BCL or niche/costly combinators — reject.** `Chunk` exists as
  `Enumerable.Chunk` and `System.Linq.AsyncEnumerable.Chunk` in .NET 10
  (`/tmp/opencode/evidence/inv-bcl-sequences-linq.md:1471,1668`; the lead owns committing that
  dump into `docs/next-stage/inventory/generated/`);
  `Shuffle` exists as `Random.Shuffle` (`SHUFFLE_EXTENSION` constant is net10.0-only,
  `FrameworkFeatureConstants.props:33`); `JoinToString`/`ConcatToString` are `string.Join`/
  `string.Concat`; `Materialize` is `ToList` with a factory whose doc claim ("if the underlying
  sequence is a collection type we do not actively enumerate them") is not implemented in the
  sync path (`Extensions/EnumerableExtensions/Materialize.cs`) — shipping an API whose
  contract overpromises fails criterion 3; `PowerSet` is 2^n (8); `Range` enumeration overlaps
  `Enumerable.Range` and makes `1..5` semantics depend on a Funcky overload (5).
- **F5.10 `Memoize`/buffers — defer with conditions.** Multiple enumeration with a single
  upstream pass is a genuine gap in the BCL (2). But the Funcky design has four problems that
  must not be inherited: (a) the async buffer ignores the caller's token and creates its source
  enumerator without one (`Extensions/AsyncEnumerableExtensions/Memoize.cs:57-65`) — a
  criterion-7 violation; (b) the buffer is not thread-safe and silently corrupts under
  concurrent enumerators (3, 8); (c) the `IBuffer`/`IAsyncBuffer` public carrier adds disposal
  obligations and `ObjectDisposedException` behavior (3, 5); (d) buffer growth is unbounded for
  infinite sources (8). If adopted later: return a documented single-consumer buffer, forward
  the token, cap or document growth, and test concurrent-access rejection. Trigger: a consumer
  that must avoid double-fetching a stream.
- **F5.11 `Merge` — defer.** k-way ordered merge is not in the BCL (2). But the sync
  implementation allocates an `ImmutableList` of enumerators and rebuilds it per exhausted
  input, and the async version additionally materializes with `ToListAsync` inside the merge
  (`Extensions/AsyncEnumerableExtensions/Merge.cs:62-77`); there is no measured profile
  (8). Defer until a consumer needs it and a bounded-state design is specified.
- **F5.12 `Partition` — adopt predicate + typed-result carriers; skip Either.** A struct with
  named `True`/`False` (or `Passed`/`Failed`) properties and `Deconstruct` is more meaning-bearing
  than two filtered lists (2, 3), and the implementation materializes once instead of
  enumerating twice (8). Adapt the Result carrier to FunnySharp's typed error
  (`IReadOnlyList<TError>`), not Funcky's `IReadOnlyList<Exception>`
  (`inv-funcky.md:1026-1035`). Skip `EitherPartitions` until `Either` (F2.2) exists.
- **F5.13 `ValueWithX` structs — defer.** `WithIndex`/`WithPrevious` are useful, but four new
  public structs duplicate what `(T Value, int Index)` tuples or the existing
  `Input`/`Output`-style carriers express (1, 5). Trigger: an index-aware pipeline where a
  named type carries its weight in signatures.
- **F5.14 string bridges — adopt `IndexOfOrNone` family; adapt `SplitLazy`.** Case- and
  culture-aware `IndexOf` returning `-1` is a classic negative-result bug; the `Option<int>`
  form is unambiguous (2, 6). `SplitLazy` avoids `string.Split`'s array allocation (8) but its
  comparison semantics (Ordinal for string separators, `SplitBy` deferral,
  `Extensions/StringExtensions/SplitLazy.cs:15-70`) must be documented per overload (3). Defer
  `SplitLines` and string `Chunk`/`SlidingWindow`.

### F6 async

- **F6.1 grammar — adopt.** Funcky's `X` / `XAwait` / `XAwaitWithCancellation` naming and
  `[EnumeratorCancellation]` token plumbing (`Extensions/AsyncEnumerableExtensions/Inspect.cs:41-58`)
  match the BCL/`System.Linq.AsyncEnumerable` convention and FunnySharp's existing
  `ValueAsync`/`AwaitWithCancellation`-style split. The shared rule (criterion 5) should be
  finalized once; whichever token suffix FunnySharp keeps, do not add a third convention.
- **F6.2 async mirrors — adopt only what the sync set adopts.** Async twins of F5.1/F5.3/F5.4/
  F5.6/F5.7 with a `CancellationToken` on terminals and `[EnumeratorCancellation]` on
  iterators (7). Skip `AverageOrNoneAsync` towers, `MaterializeAsync` (BCL `ToListAsync`),
  `ShuffleAsync`.
- **F6.3 async `Merge`/`Memoize` — defer/reject as in F5.10/F5.11.** The clone-only `Merge`
  token fix (commit `7ccbe658`) shows the shipped 3.6.0 behavior was worse; nothing here is
  prior art worth copying.
- **F6.4 `ShuffleAsync` — reject** (niche; requires a crypto-or-`Random` policy decision).

### F7 effects/environment/retries

- **F7.1 retry — adapt.** A policy value is the right carrier (3, 4): `MaxRetries` +
  `Delay(attempt)` is inspectable and testable. Adaptations: drive all delays through
  `TimeProvider` (7: deterministic tests, no ambient timers; 3: `TimeProvider` is the BCL
  convention), use one documented attempt-index convention for every overload (Funcky's two
  variants disagree: `Functional/Retry.cs` calls `Delay(0)` first; `RetryWithException.cs:23`
  increments before sleeping), and cap the delay (Funcky's exponential has no cap and
  `firstDelay * 1.5^n` overflows to `OverflowException` via `TimeSpan.Multiply`; 8). Add jitter
  only as an explicit user-supplied delegate (3: no hidden randomness). Async only, or a sync
  overload that takes an explicit sleep delegate; never `Thread.Sleep` inside a library
  (7).
- **F7.2 reject Funcky's retry entry points.** `Retry(Func<Option<T>>)` retries forever with no
  cancellation and no policy (`Functional/Retry.cs:10-13`); the sync paths block the thread
  (`RetryWithException.cs:23`). Both fail criterion 7 outright.
- **F7.3 `Reader` — reject as a new carrier.** It is a bare delegate with no cancellation, no
  async, and no deferral semantics beyond "function of environment"
  (`Reader/Reader.Core.cs:3-6`); FunnySharp's `Effect<TEnvironment,T>` already expresses the
  same thing with `RunAsync` and token forwarding (5, 3).

### F8/F9/F10

No Funcky capability to decide. F10's only shareable capability is the `HttpHeaders`/
`JsonSerializerOptions` bridges already counted in F5.3. FunnySharp's state machine and optics
families stay unchallenged by this baseline.

### F11 cross-cutting

- **F11.1 analyzer packaging — adopt the shape, not the friction.** See §4.
- **F11.2 `NotConfigurable` — reject as a default.** λ0003 cannot be suppressed or downgraded
  (`SyntaxSupportOnlyAnalyzer.cs:21`); a consumer who deliberately wants the annotated member
  has no path except `#pragma` (which the analyzer also ignores for NotConfigurable rules) or
  removing the analyzer package. Reserve Error/NotConfigurable for states the BCL/type system
  cannot express at all; every other diagnostic must be suppressible and severity-configurable.
- **F11.3 `PublicAPI.Shipped/Unshipped` + `Microsoft.CodeAnalysis.PublicApiAnalyzers` — adopt
  as process.** This is the cheapest mechanical enforcement of the stability boundary
  (criterion 4 evidence, criterion 11); Funcky's 1 262-line shipped file and explicit
  unshipped delta show it working in practice (`Funcky/PublicAPI.Shipped.txt`,
  `"Unshipped.txt"`).
- **F11.4 multi-TFM + dependency pins — reject.** FunnySharp is `net10.0`-only and
  `PackageReference`-free (`docs/product-contract.md §"Package And Dependency Boundary"`). Funcky's pins
  (`System.Text.Json 5.0.2`, `System.Collections.Immutable 1.7.1`) are exactly the dependency
  debt the contract rejects; also note that the async surface only exists on net10.0 because of
  the multi-TFM `INTEGRATED_ASYNC` split — a compatibility matrix with two different API
  surfaces per TFM (5).
- **F11.5 implicit usings — reject.** Global `Using` injection changes consumer compilation
  and creates hidden coupling (5).
- **F11.6 internal source generator — adopt as a technique, not a shipped artifact.** If
  FunnySharp adopts a sizable bridge family (e.g. parse bridges), generating it from a small
  declarative input keeps signatures consistent (5) with zero runtime cost (8). Funcky's
  generator is internal-only and the package ships no generator.

## 3. Naming and vocabulary observations (for the lead's canonical vocabulary)

1. **`…OrNone` grammar.** Funcky's strongest naming contribution: `TryX`/`XOrDefault` APIs
   become `XOrNone` (`GetValueOrNone`, `FirstOrNone`, `ParseInt32OrNone`, `DequeueOrNone`,
   `MoveNextOrNone`, `ReadByteOrNone`). It is used uniformly across ~300 members and states the
   negative case in the name (3, 5). FunnySharp currently uses `GetOption` for
   `IReadOnlyDictionary` (`inv-funny-sharp-core.md:133`) and `ToOption`/`FromNullable`; there is
   no single rule. Recommendation: pick one rule and apply it consistently — either all
   Try→Option bridges are `…OrNone`, or all are `…Option`. Funcky's convention is the more
   common external prior art, but renaming `GetOption` is a source-breaking change (allowed:
   FunnySharp is 0.1.0 and compatibility promises are out of scope). This is a maintainer
   decision, not an agent one.
2. **`Inspect` vs `Tap`.** Funcky uses `Inspect` at the sequence level and `AndThen(Action)`/
   `Inspect` on `Option`; FunnySharp uses `Tap` for values. Both names are idiomatic in their
   domain (LINQ `Inspect` is the Funcky/MoreLINQ-style sequence operation); keep `Tap` for
   values and adopt `Inspect` for sequences, documenting the split (5).
3. **`WhereSelect` vs `Choose`.** Keep `Choose` (5); do not import `WhereSelect`.
4. **`Match(none, some)` vs FunnySharp's `Match(some, none)`.** Funcky orders the none branch
   first for the value/thunk overloads; FunnySharp orders success/some first. Both are
   defensible; mixing them across two libraries is a hazard, but Funcky is not a dependency, so
   keep FunnySharp's order (5) and never add a second `Match` shape.
5. **`FromBoolean`/`FromNullable`/`FromTry`** form a consistent factory prefix in both
   libraries; keep.
6. **Funcky naming that should not be adopted:** `Discard.__`, `ω1..ω8` parameters,
   `RequireClass<T>`/`RequireStruct<T>` marker classes, `XOrBoth`, `CycleMaterialized`/
   `RepeatMaterialized` (`Materialized` suffix hides the allocation contract), `IAsyncBuffer`
   (a buffer is not an enumerable; 3).

## 4. Prior art assessment for Goal 21 (analyzers and compiler feedback)

Funcky ships two analyzers inside the main package (λ0001, λ0003) plus one code fix wired to a
compiler error, and a separate optional package (`Funcky.Analyzers` 1.4.x) with eleven further
rules (details and citations in `inventory/funcky.md` §4). Assessment against Goal 21:

**Adopt as prior art**

- **Packaging**: analyzers in `analyzers/dotnet/cs` of the main package give zero-friction
  protection, and a separately versioned package carries opinionated style rules
  (`DevelopmentDependency=true` keeps them out of downstream libraries'
  transitively-visible graph). This two-package split matches FunnySharp's likely needs:
  correctness analyzers in `FunnySharp`, style/pedagogy rules optional.
- **Mechanism design**: the `SyntaxSupportOnlyAttribute` + analyzer pair (λ0003) is the cleanest
  way to expose syntax-sugar-only members (`Option.Count`/indexer) without letting them become
  general-purpose API; the attribute carries the feature name used in the message, and the
  member stays usable by the compiler-mandated pattern. If FunnySharp ever adds list-pattern or
  collection-expression support to a non-collection carrier, copy the mechanism (not the IDs).
- **Escape-hatch restriction**: λ0001 shows the pattern for keeping a dangerous-but-needed API
  (`TryGetValue`) public while restricting its use to the few imperative shapes that cannot be
  expressed functionally. FunnySharp has the same hazard today (`Option.TryGetValue`,
  `Result.TryGetValue`, `Result.TryGetError`, `TransitionResult.TryGetChange/TryGetError`,
  `Validation.TryGetValue/TryGetErrors`), all currently unconstrained.
- **Default-state protection**: λ1009 (`NonDefaultableAnalyzer`, Error) is directly relevant:
  FunnySharp's `Result<TValue,TError>` and `Validation<TValue,TError>` are silently constructible
  via `default` and then present a `default` error (`src/FunnySharp/Result.cs:358-392`,
  `src/FunnySharp/Validation.cs:12-28,289`). Either the types get a compiler-visible guard
  (an analyzer that flags `default(...)`/`new T()` for these types) or their default semantics
  get documented as supported; leaving it implicit is the worst option (4).
  `TransitionResult<TState,TOutput,TError>` is different: its default is the meaningful
  `TransitionStatus.Undefined` state (`src/FunnySharp/StateMachine.cs:8-45,77`), so it must be
  excluded from any such analyzer.
- **Release tracking and tests**: `AnalyzerReleases.Shipped/Unshipped.md` with RS2008 plus
  `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing`/`CodeFix.Testing` is the minimum evidence
  bar (criterion 4).
- **Fixing a compiler error instead of a custom diagnostic**: Funcky deleted λ0002 and shipped
  `OptionNoneInvocationCodeFix` for CS1955 when `Option.None()` became a property; the code
  fix's `FixableDiagnosticIds` is `CS1955` and it registers only for the exact
  `Option.None`-invocation shape (`Funcky.BuiltinAnalyzers.CodeFixes/OptionNoneInvocationCodeFix.cs:17-56`).
  Useful if FunnySharp renames a member: the compiler error becomes the trigger and the fix
  restores ergonomics.

**Adapt / reject**

- **Reject `λ` IDs** (non-ASCII) for FunnySharp; use ASCII IDs.
- **Reject `NotConfigurable`** as a default (see F11.2). If a NotConfigurable rule is ever
  needed, pair it with a build property that disables the analyzer package, and document it.
- **Reject the Error-by-default severity** for style rules. λ1001–λ1008 are all Warning; keep
  that line. Error is reserved for states that are otherwise impossible to detect.
- **Reject analyzer rules that restate the compiler** (`Enumerable.Repeat(x, 1)` → the
  IDE already offers hints; 1) unless paired with a *FunnySharp* replacement that is clearly
  canonical.
- **False-positive surface**: Funcky's λ0001 whitelists AST shapes (loop condition, iterator
  `if` with `yield`, `catch when`) and still needs an escape hatch for Razor components
  (`TryGetValueAnalyzer.cs:44-104`). Any FunnySharp equivalent needs the same shape analysis
  plus tests for iterators, `catch when`, expressions in query syntax, and generated code.

**Suggested Goal 21 starter set (ranked, for the lead's consideration)**

1. `default(Result<TValue,TError>)` / `default(Validation<TValue,TError>)` detection (adapt λ1009).
2. Try-extraction restriction for `Option<T>.TryGetValue` and `Result`/`Validation`/
   `TransitionResult` Try members (adapt λ0001).
3. Rewrite `Match`-shaped `Select`/`Bind`/`Recover` to the direct combinator (adapt
   λ1005–λ1008); this enforces FunnySharp's canonical vocabulary (criterion 5).
4. Argument-name enforcement on `Match` overloads where the branches differ by intent
   (adapt λ1003); optional, Warning.
5. A `SyntaxSupportOnly`-style mechanism if FunnySharp ever adds collection-expression/list-
   pattern support (adapt λ0003 mechanism).

## 5. FunnySharp API findings surfaced by this baseline (for the lead's API matrix)

These are not external-capability decisions; they are contract-relevant observations found
while comparing. Evidence is cited so the lead can decide `keep`/`redesign`/`experimental`/
`remove`.

1. **`Result<TValue,TError>`/`Validation<TValue,TError>` default states** — see §4; either
   protect, document, or redesign (criterion 4).
2. **`Option<T>` null-collapsing in `Map`** (`Map` treats a null selector result as `None`,
   `src/FunnySharp/Option.cs:162-166`) while `Some` rejects null; this is a deliberate
   `FromNullable`-style rule but is not stated in the `Map` XML doc. Funcky avoids the question
   with `notnull`. Clarify the contract (3, 5).
3. **`GetOption` naming** — one bridge name that does not follow either `…OrNone` or
   `…Option` consistently with the rest of the surface (§3.1).
4. **`TryGetValue` exposure without a guard** — the analyzer decision (§4) doubles as an API
   stability decision: mark it `Advanced`/`EditorBrowsable(Never)` or keep it as a normal
   member before Goal 21 can flag its misuse without breaking intended users.
5. **`Result.Try` doc/behavior** — preserve-exception vs mapped-error split already exists and
   is more careful than Funcky's; no change implied, recorded as evidence of the typed-error
   direction being deliberate.

## 6. Funcky choices FunnySharp must not inherit (condensed)

The full list with citations is in `inventory/funcky.md` §6. The load-bearing ones:

1. `Result`/`Either` error channels built on `Exception`, including `Result.Error`'s
   stack-trace mutation.
2. Invalid `default` states detected by a *separate optional* analyzer package rather than by
   the type or the main package's diagnostics.
3. Async `Memoize`/`IAsyncBuffer` that drops the caller's token.
4. `Thread.Sleep`-based sync retry, unbounded policy-less retry, inconsistent delay indexing,
   uncapped exponential growth.
5. Multi-TFM dependency pins (`System.Text.Json 5.0.2`, `System.Collections.Immutable 1.7.1`)
   and an API surface that differs per TFM.
6. `NotConfigurable` Error diagnostics, `λ`-prefixed IDs.
7. Marker-class overload disambiguation (`RequireClass`/`RequireStruct`) and 81 `Unit`-based
   `Apply` overloads with `ω1..ω8` parameter names.
8. `Match` overload pairs that differ only by value-vs-thunk.
9. Implicit global usings injected into consumer builds.
10. `Option.Count`/indexer list-pattern support as public surface.

## 7. Not examined / unverified

- **Not examined**
  - No Funcky code was executed; no Funcky test suite or benchmarks were run in this
    workstream. Per-API behavior claims are source-read.
  - `Funcky.Analyzers` 1.4.1 nupkg, `Funcky.Xunit`, `Funcky.EntityFrameworkCore`,
    `Funcky.DiscriminatedUnion` were not surveyed (out of the pinned scope).
  - Non-net10.0 assemblies and the `Funcky.Async` package surface were not dumped.
  - Funcky's `Documentation/src/case-studies` and most extension docs were not read.
  - No FunnySharp implementation exploration beyond the public API dump, `product-contract.md`,
    and the `Result/Validation/Option` sources cited above; the FunnySharp-API findings in §5
    are limited to what those sources state.
- **UNVERIFIED judgments**
  - **UNVERIFIED: performance** of every Funcky combinator considered for adoption (`Merge`,
    `Memoize`, `SlidingWindow`, `Sequence`/`Traverse`, parse bridges). The criteria-8 verdicts
    above rest on source reading (allocation shapes, immutable-list stepping, unbounded
    buffers), not on `eng/performance/baseline.json`-style measurements.
  - **UNVERIFIED: EF Core translation** for `IQueryable` `…OrNone` bridges; no EF test ran.
  - **UNVERIFIED: AOT-clean STJ converter** design for `Option<T>`; no experiment ran.
  - **UNVERIFIED: consumer demand** for F1.6 awaiters, F2.2 `Either`, F5.10 `Memoize`, F5.13
    `WithX` structs; those deferrals are explicit triggers rather than measured conclusions.
  - **UNVERIFIED: whether `Option<T>`'s list-pattern support is used outside Funcky's own
    tests**; the adoption decision for that mechanism depends on Goal 21 scope.
  - **UNVERIFIED: naming decision authority** — the `…OrNone` vs `…Option` recommendation in
    §3.1 is a maintainer/product decision and is recorded as such.
