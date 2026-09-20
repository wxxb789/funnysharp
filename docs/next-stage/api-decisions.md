# FunnySharp API Decisions (Goal 14)

This is the complete decision matrix for the FunnySharp 0.1.0 public surface pinned at
commit `4dbebd94b7b58648632112b7ca47c39cc517f153`. Every public type and member receives an
explicit decision: **keep**, **redesign**, **experimental**, or **remove**. The member-level
matrix is [`decision-matrix.csv`](decision-matrix.csv); this document records the type-level
decisions, member exceptions, vocabulary rules, and the stability boundary.

Decision criteria (from the goal; used as C1–C8 below):

| # | Criterion |
| --- | --- |
| C1 | ordinary C# usefulness |
| C2 | reduction of consumer-side semantic LOC |
| C3 | clarity from signatures |
| C4 | compile-time verifiability |
| C5 | AI predictability |
| C6 | BCL interoperability |
| C7 | async and cancellation semantics |
| C8 | credible performance feasibility |

Evidence pins and inventories: [`baselines.md`](baselines.md),
[`inventory/generated/`](inventory/generated/). Analysis: [`analysis/`](analysis/).
Call sites: [`call-sites.md`](call-sites.md).

## Decision summary

| Decision | Types | Members |
| --- | ---: | ---: |
| keep | 31 | 223 |
| redesign | 3 types: `Result<TValue,TError>`, `Validation<TValue,TError>`, `StateTransitionExtensions`; `FunctionExtensions` is type-level keep with two redesigned members (`ComposeValueAsync`) | 46 |
| experimental | 0 current members | 0 |
| remove | 0 | 0 |

CSV convention: when a type is redesigned, every member of that type carries
`member_decision=redesign`, because the type-wide contract change applies to each member;
the narrative below names the members whose signatures or documented semantics change most.

The Goal 14 matrix removes no capability. Goal 16 replaces seven old member signatures
through the accepted `ToResult` and `ComposeValueAsync` renames below. Removal is reserved
for shapes superseded by the redesigns
(the old `Then` algorithm) and for capabilities the
maintainer rejects.

## AD-1 Absence — `Option`, `Option<T>`, `OptionExtensions`, `TryOperation<T>`

**Decision: keep all four types; adapt the grammar with three additive members.**

- Existing semantics are the safest of every surveyed baseline: `readonly struct`,
  `default = None`, no throwing accessor, no implicit conversion, null-normalized `Map`
  (C1, C3, C4). FSharp.Core's `FSharpOption`/`FSharpValueOption`, Funcky's `Option` with
  `notnull`, CFE's `Maybe` implicit conversions, and language-ext's `OptionAsync`/`OptionUnsafe`
  all add failure modes that FunnySharp deliberately avoids
  (`analysis/baseline-fsharp-core.md` §4 #1–#4; `analysis/baseline-funcky.md` F1.1;
  `analysis/baseline-csharpfunctionalextensions.md` 1.1–1.2; `analysis/baseline-language-ext.md` 1.1–1.3).
- `Option.Some(null)` rejects runtime null with `ArgumentNullException`; bridges normalize null
  to `None` (`docs/option.md:31`). Keep.
- `Option.FromNullable` cannot be used from unconstrained generic code (CS0452) and
  `Option<T>.FromNullable` is internal (`analysis/funny-sharp-surface.md` §2.7). Keep the
  constraint split; document the generic-code guidance. **Acceptance: keep.**
- Additive members adopted for Goal 15/16/17 compliance (implemented by those goals):
  `Select`/`SelectMany` LINQ aliases for parity with `Result`/`Effect` (no `Where`), and
  `FromBoolean(bool)` plus its lazy selector overload. `ToNullable` bridges are adopted as a
  Goal 15 item. The `*OrNone` cardinality family is AD-5.
- `TryOperation<T>` stays the only custom delegate: minimal Try-pattern adapter (C1, C6).

## AD-2 Fail-fast value outcome — `Result`, `Result<TValue,TError>`, `ResultExtensions`

**Decision: keep the semantics; redesign the default/uninitialized state contract.**

- `Result` static `Try`/`TryAsync`/`TryValueAsync` is the strongest exception boundary in the
  surface: `OperationCanceledException` is never mapped, mapper exceptions propagate, exception
  identity is preserved, tokenless by design (`analysis/funny-sharp-surface.md` §2.8; C3, C4, C7).
  Keep.
- `Result<TValue,TError>` composition verbs (`Map`, `Bind`, `MapError`, `Ensure`, `Recover`,
  `RecoverWith`, `Zip`, `ZipWith`, `Select`, `SelectMany`) keep one canonical name per intent,
  unlike CFE's alias families and 24-overload `Match` sets (C1, C2, C5;
  `analysis/baseline-csharpfunctionalextensions.md` 2.5).
- **Redesign (member-level)**: case inspection, equality, and text members of
  `Result<TValue,TError>` — `IsSuccess`, `IsFailure`, `TryGetValue`, `TryGetError`, `Match`,
  `Equals`, `GetHashCode`, `ToString`, `==`, `!=`. Today `default(Result<TValue,TError>)` is
  indistinguishable from `Failure(default(TError))`, which Goal 15 prohibits
  (`docs/goals/0015-goal.md:2`; `analysis/funny-sharp-surface.md` §2.1).
  Lead decision: an uninitialized value keeps two public cases but is detected; operating on it
  is a programming error that throws `InvalidOperationException` with an explicit message,
  matching the Effect/optics precedent. Goal 15 owns the final mechanism (flag, accessor
  behavior, analyzer coverage) inside this boundary. The redesign is **type-wide**: every
  member must define its behavior for an uninitialized value (factories produce initialized
  values; transforms either propagate the uninitialized state consistently or throw), which
  is why every `Result<TValue,TError>` CSV row carries the redesign decision.
- No throwing accessor, no implicit conversions: keep rejecting the CFE shapes (C3, C4, C5).

### Goal 16 amendment: `UnitResult` value conversion

**Decision: accept the breaking rename of all five old value-producing signatures.**

The accepted [G16-3 decision](maintainer-acceptance-goal-16.md#g16-3-unitresultterrormap-renamed-toresult-grammar-completion)
renames all five value-producing `UnitResult<TError>` signatures: `Map` to `ToResult`,
`MapAsync` (with and without a token) to `ToResultAsync`, and `MapValueAsync` (with and
without a token) to `ToResultValueAsync`. These factories introduce a value and return
`Result<TValue, TError>`; they do not transform a contained value. The conversion names
preserve the grammar rule that `Map` keeps the carrier shape. This amendment supplements
the Goal 14 inventory pinned above; the Goal 16 acceptance record owns the decision.

## AD-3 Accumulation — `Validation`, `Validation<TValue,TError>`, `ValidationExtensions`

**Decision: keep applicative semantics and the no-`Bind` boundary; redesign the
default/uninitialized state contract the same way as AD-2 (type-wide, including the
factories `Valid`/`Invalid`/`InvalidMany` and the composition verbs: each must have defined
behavior for an uninitialized value).**

- `Valid`/`Invalid`/`InvalidMany`, ordered left-to-right accumulation, `Zip` and `Apply`, and
  the deliberate absence of `Bind`/`SelectMany`/`ZipWith` are the capability language-ext
  expresses through a monoid type argument and CFE only approximates with string joining
  (C2, C3, C5; `analysis/baseline-language-ext.md` 3.1–3.4;
  `analysis/baseline-csharpfunctionalextensions.md` 3.1–3.3). Keep.
- **Redesign (member-level)**: the same inspection/equality/text members as AD-2, because
  `default(Validation<TValue,TError>)` equals `Invalid([default(TError)])`
  (`analysis/funny-sharp-surface.md` §2.1). `InvalidMany` already rejects null/empty, so an
  empty-error invalid state is not constructible through the factory (`docs/validation.md:35`).
- `Validation` has no numeric-LOC advantage at small scale — the honest justification is typed
  reusable validators, guaranteed error retention, and compositional paths (C4, C5;
  `call-sites.md` W3, §16 item 3). Recorded so no "always shorter" claim is made.
- Ergonomics: Goal 16 may add a bounded applicative helper (arity ≤ 4, explicit argument order)
  only if it demonstrably reduces semantic LOC at form-scale call sites (W3: 19→24 today) and
  keeps error order visible. Unbounded tuple towers and `BindZip`-style arity sets stay rejected
  (C2, C3, C5; `call-sites.md` §16 item 3; CFE 5.2).

## AD-4 Function grammar — `FunctionExtensions`

**Decision: keep; one member-level rename for vocabulary consistency.**

- `Pipe`, `Compose`, `Curry`, `Uncurry`, `Partial`, `Flip`, `Tap`/`TapAsync`/`TapValueAsync`
  are a superset of the useful language-ext subset with token-aware async forms (C1, C2, C5;
  `analysis/baseline-language-ext.md` 4.1). Binary-only arity is a documented boundary
  ([function-composition.md — Deliberate Boundaries](../function-composition.md#deliberate-boundaries)). Keep.
- **Redesign (member-level)**: the two `ValueTask` overloads of `ComposeAsync` become
  `ComposeValueAsync`. Rationale: every other verb family distinguishes a `ValueTask`-returning
  callback with the `ValueAsync` suffix; `ComposeAsync` is the only exception
  (`analysis/funny-sharp-surface.md` §2.2; C3, C5). Breaking change accepted at 0.1.0.
- Reject operator/free-function models (`|>`, `>>`, `Prelude`, 2,284-member vocabulary,
  global-namespace extensions): C3, C5 (`analysis/baseline-language-ext.md` 4.2–4.3;
  `analysis/baseline-fsharp-core.md` #10–#12).

## AD-5 Collections, traversal, pipelines

**Types: `EnumerablePipelineExtensions`, `AsyncEnumerablePipelineExtensions`,
`SequenceExtensions`, `AsyncSequenceExtensions`, `SpanPipelineExtensions` — all keep.**

- `Choose`/`ChooseValueAsync` fuse Option filtering without an intermediate collection; the
  sync form beats LINQ `Where`+`Select` with fewer allocations (C1, C8;
  `docs/data-pipelines.md:91-92`). Keep.
- `Sequence`/`Traverse` (sync and async) preserve the fail-fast `Option`/`Result` versus
  accumulating `Validation` split; BCL has no equivalent (C2, C6;
  `analysis/baseline-bcl.md` coverage verdicts).
- `SpanPipelineExtensions` keeps honest lifetimes and caller-owned storage (C4);
  measured slower than a direct loop and disclosed as directional (`docs/data-pipelines.md:93-94`).
- Adopt (Goal 17 implementation): traversal location/path context (`customers[17].addresses[2].postalCode`),
  `Partition` (predicate and typed carriers), the `*OrNone` cardinality family
  (`FirstOrNone`, `LastOrNone`, `SingleOrNone`, `ElementAtOrNone`, `MinOrNone`, `MaxOrNone`),
  container Try→Option bridges (dictionary/list/immutable/queue/stack/priority-queue; keep the
  existing `GetOption` for keyed lookups and `ToOption` for conversion), common `Parse*OrNone`
  bridges over `IParsable`, and a narrow `Scan`. `WhereNotNull` and `Pairwise` are adopted only
  as part of the Goal 17 design if they survive its LOC tests.
- Reject custom sequence carriers (`Sequence`, `IBuffer`, `Seq`, `Lst`, `Map`, `HashMap`),
  `WhereSelect` (duplicate of `Choose`), `Cycle`/`Shuffle`/`Chunk`/`Materialize`/
  `JoinToString` (BCL or trivial), `BindZip` tuples, and per-carrier lens properties
  (C5, C6, C8; `analysis/baseline-funcky.md` F5.5–F5.11;
  `analysis/baseline-language-ext.md` 5.2–5.3; `analysis/baseline-csharpfunctionalextensions.md` 5.2).

## AD-6 Async, streaming, concurrency

**Types: `ParallelAsyncEnumerableExtensions`, `ParallelAsyncSequenceExtensions`, and
`ConcurrentEffectExtensions` keep; the first-success race contract is documented explicitly
(maintainer A-2=B).**

- `SelectParallelValueAsync` and `TraverseParallelValueAsync` are bounded, ordered,
  cancellation-correct, and have no BCL equivalent (C1, C7;
  `analysis/baseline-bcl.md`; `docs/concurrency.md:10-90`). Ordering, backpressure, and drain
  behavior are specified in `docs/concurrency.md:28-46,73-86` and covered by
  `tests/FunnySharp.Tests/ParallelAsyncEnumerableTests.cs`,
  `tests/FunnySharp.Tests/ParallelAsyncSequenceTests.cs`, and
  `tests/FunnySharp.Tests/FirstSuccessTests.cs`; the BCL memo's uncertainty about them was a
  BCL-only limitation, not an unverified FunnySharp contract. Keep the ValueTask-only selector
  policy and the drain-on-dispose contract.
- **Keep (maintainer decision A-2=B)**: all three `FirstSuccessAsync` overloads keep
  `Validation<TValue,TError>`. A winner is `Valid`; an all-typed-failure race is `Invalid` with
  the failures in input order. The race contract is documented explicitly at the method and in
  `docs/concurrency.md` so readers cannot mistake it for independent-check accumulation; callers
  that need a different error aggregation can nest `TError` in their own domain type. The
  `analysis/funny-sharp-surface.md` §2.3 redesign recommendation is superseded. The drain,
  cleanup, and caller-cancellation-precedence contract is unchanged.
- Adopt (Goal 18): completion-order coordination over `IAsyncEnumerable<T>` and cold effects.
  Reject naked started-Task racing APIs, unbounded fan-out, `Fork`, and any async wrapper carrier
  (C4, C7; `analysis/baseline-language-ext.md` 6.3–6.5; `analysis/baseline-fsharp-core.md` #16).
- `Memoize`/`IAsyncBuffer` carriers and k-way merge defer until a concrete consumer exists
  (C1, C8; `analysis/baseline-funcky.md` F5.10–F5.11).
- Mixed sync/async chains cannot be a single expression because `MapAsync`/`BindAsync` return
  task carriers (`call-sites.md` §16 item 4). The canonical form stays ordinary `await` plus
  synchronous composition; no task-carrier operator universe (this is exactly CFE's 664-member
  mistake on the async axis; C3, C5, C8).

## AD-7 Effects, resources, environment

**Types: `Effect`, `Effect<T>`, `Effect<TEnvironment,T>`, `EffectResourceExtensions` — keep.**

- Deferred `ValueTask` execution, exact token forwarding, caller-owned environment, and
  exactly-once resource release are documented, tested, and semantically motivated (C1, C3, C7,
  C8; `docs/effects.md:61-92`). Overhead is disclosed by the generated tables; the requirement is
  to document complexity/allocation per operator and to keep the "not a runtime" boundary
  (`analysis/funny-sharp-surface.md` §2.5).
- `Effect.FromValue` must state its null policy; the lead decision is "null is a value",
  consistent with `Result.Success(null)` and `Validation.Valid(null)` (`analysis/funny-sharp-surface.md`
  §2.12). Documentation-only change; Goal 23 owns it.
- Reject Eff/Aff/IO monads, Reader delegates, runtime DI traits (`Has<RT,Trait>`), retry
  schedules as a DSL, and `Effect<Fin<T>>`-style failure carriers (C4, C5, C6, C7;
  `analysis/baseline-language-ext.md` 7.1–7.5; `analysis/baseline-funcky.md` F7.1–F7.3).
- Adoption guidance for the constitution: use `Effect` when deferred execution, explicit
  environment, cancellation flow, or resource lifetime actually pay for the wrapper; for a single
  synchronous call the direct BCL expression is the honest baseline (W9: 9→10 semantic units;
  `call-sites.md` §16 item 7).
- Retry/timeout policy: a general retry layer stays out of the stable surface for this stage
  (see the decision record, E102). Only coordinator-owned timeouts (as in the first-success
  family) exist today.

## AD-8 State transitions and machines

**Types: `TransitionStatus`, `StateChange<TState,TOutput>`, `StateTransition<TState,TOutput>`,
`TransitionResult<TState,TOutput,TError>`, `StateMachine<TState,TEvent,TOutput,TError>`,
`StateMachineExtensions` keep; `StateTransitionExtensions` redesign (member-level).**

- The four-status model, data-only `StateChange`, undefined-handler distinction, and pure
  replay are deterministic and tested (C1, C3, C4; `docs/state-machines.md:36-77`). Keep.
- **Redesign (member-level)**: `Then` composes by concatenating output arrays on every
  invocation, making left-associated chains O(n²): 85x–457x slower and up to 187 KB allocated at
  Count=256 (`analysis/funny-sharp-surface.md` §2.4; `docs/state-machines.md:110-115`). Goal 20
  requires resolve-or-exclude; the lead decision is **resolve** with single-materialization
  composition. Excluding long chains from the stable contract is the accepted fallback only if
  Goal 20 cannot meet the resolution bar.
- `StateMachine<...>` and `TransitionResult<...>` intentionally differ in generic parameter
  count/order; document, do not unify (`analysis/funny-sharp-surface.md` §5.1 note).
- Reject `State`/`StateT`/`ReaderT`/`WriterT`/`RWS`, actor runtimes (`MailboxProcessor`), STM,
  persistence, and workflow engines (C1, C3, C6, C7; `analysis/baseline-language-ext.md`
  8.1–8.2; `analysis/baseline-fsharp-core.md` #20).

## AD-9 Optics and immutability

**Types: `Lens`, `Lens<TSource,TFocus>`, `Optional`, `Optional<TSource,TFocus>` — keep.**

- Delegate-based total and partial focuses with left-to-right composition, absent-focus source
  identity, and caller-owned laws; language-ext confirms the shape rather than improving it
  (C2, C3, C4, C6; `docs/immutable-updates.md:15-89`;
  `analysis/baseline-language-ext.md` 9.1–9.2). The 3-level composed update is 3 semantic lines
  against ≥4 statements of manual copying (`call-sites.md` W10), but first-use cost is real
  (17 raw definition lines), so lenses are for paths updated in more than one place
  (`call-sites.md` §16 item 8).
- Optics laws remain caller obligations; uninitialized optics throw on every operation. Keep,
  documented (C3, C4).
- Reject prism/traversal/iso/getter/setter/fold hierarchies, per-carrier lenses, reflection
  property paths, and persistent-collection policy (C1, C5, C6;
  `analysis/baseline-language-ext.md` 9.3–9.4).

## AD-10 HTTP integration — `HttpResultExtensions`

**Decision: keep.**

- Explicit problem mappers, optional success mappers, exact `RequestAborted` forwarding, no DI,
  middleware, or global policy (C1, C3, C6, C7; `docs/aspnet-core.md:15-88`). The mapping is a
  pure win at call sites when composed (W11: 34→21 semantic units; `call-sites.md` §16 item 7).
- Adopt (Goal 15/22): `UnitResult<TError>` mapping follows the `Result` overload pattern.
  TypedResults/OpenAPI metadata stays deferred; the vertical slice documents `.Produces*` usage
  and tests OpenAPI output instead of adding a first-party typed-results API (`call-sites.md` W11;
  `analysis/baseline-bcl.md` F10).
- The `IResult` return type does not carry typed-results/OpenAPI metadata; adapter-only
  throughput/latency stays unclaimed, while Goal 22 may publish end-to-end measurements scoped
  to the sample application (standing-constraints C6, resolved by this record).

## Canonical vocabulary (maintainer acceptance item)

1. **One carrier per meaning.** `Option<T>` absence; `Result<TValue,TError>` fail-fast value;
   `UnitResult<TError>` fail-fast no value; `Validation<TValue,TError>` accumulating independent
   errors. No second absence/outcome/async carrier.
2. **One verb per meaning.** `Map`/`Bind`/`MapError`/`Ensure`/`Filter`/`Recover`/`OrElse`/`Zip`/
   `ZipWith`/`Apply`/`Match`/`TryGet*`/`GetValueOr*` keep exactly the meanings recorded in
   `analysis/funny-sharp-surface.md` §2.2 and the analysis memos. `Where` is never added to a
   carrier: a bare predicate cannot produce absence or an error.
3. **Async suffixes.** `...ValueAsync` marks a `ValueTask`-returning callback. `...Async` marks an
   awaitable operation (no callback, or a `Task`-returning callback). `TraverseAsync` over an
   `IAsyncEnumerable<T>` takes a synchronous selector; the selector type is visible in the
   signature and the docs state the rule. `ComposeValueAsync` is the renamed ValueTask form
   (AD-4).
4. **Fallback and extraction names.** Eager fallback is a plain parameter (`OrElse`, `Recover`,
   `GetValueOr`); lazy fallback is a factory in a `...With`/`...Else` form (`OrElseWith`,
   `RecoverWith`, `GetValueOrElse`); `GetValueOrDefault` is the only member allowed to return
   `default(T)` and is named to say so.
5. **Cardinality names.** Sequence access uses the `*OrNone` suffix; keyed lookups use
   `GetOption`; value conversion uses `ToOption`.
6. **LINQ aliases are secondary.** `Select`/`SelectMany` exist only where `Bind` exists;
   `Validation` never gets them. Option gains them for parity (AD-1). Docs present the
   member-centric form first.
7. **No naming concessions.** Names never change to match a competitor's vocabulary, and no
   competitor alias is added.

## Stability boundary (maintainer acceptance item)

- **Stable**: every kept 0.1.0 type and member after the AD-1–AD-10 edits, plus capabilities
  adopted into the next stage only when their own goal lands them with tests, XML docs, and
  performance characterization. Stable members change only through a later accepted goal.
- **Experimental**: new capabilities may ship as `[Experimental("FS####")]` with a documented
  diagnostic, an entry in a tracked stability inventory, no compatibility promise, and removal
  without a breaking-change process. The lead decision is that the next stage starts with no
  experimental members in the 0.1.0 surface; Goal 17's location-context API and Goal 21's
  diagnostics enter as experimental unless their goals produce full evidence.
- **Replaced signatures**: Goal 16 removes the five old `UnitResult.Map` family signatures
  in favor of `ToResult` (AD-2 amendment), and the two `ValueTask` `ComposeAsync` signatures
  in favor of `ComposeValueAsync` (AD-4). Migrate callers by using the new names; behavior
  and parameter shapes are preserved. No capability is removed. Further removal requires an
  accepted decision row here and a migration note in the same goal; no migration promise is
  made to competitors.
- **Enforcement**: committed public-API baseline + `EnablePackageValidation`; XML documentation
  required; release gates unchanged (`docs/release-readiness.md`).

## Goal-derived capability decisions

| ID | Capability | Decision | Owner |
| --- | --- | --- | --- |
| G1 | `UnitResult<TError>` fourth carrier + traversal/HTTP integration | adopt | Goals 15, 17, 22 |
| G2 | Uninitialized `Result`/`UnitResult`/`Validation` signalling | redesign | Goal 15 |
| G3 | No second absence carrier; no `Validation.Bind`; no carrier `Where` | reject (non-goals) | constitution |
| G4 | Fallible composition stays `Bind`; narrow `Scan` | adapt / adopt | Goal 16 |
| G5 | `Partition`, `*OrNone`, common `Parse*OrNone` | adopt | Goal 17 |
| G6 | Traversal location/path context | adopt (experimental first) | Goal 17 |
| G7 | Completion-order concurrency (BCL carriers only) | adopt | Goal 18 |
| G8 | General retry/policy layer | reject for this stage | constitution |
| G9 | Analyzer package + code fixes + diagnostic docs | adopt | Goal 21 |
| G10 | `[Experimental]` marker + stability inventory | adopt | Goals 19, 23 |
| G11 | Package-only ASP.NET Core vertical slice | adopt | Goal 22 |
| G12 | First-party typed-results/OpenAPI API | defer; document `.Produces*` | Goal 22 |
| G13 | Fixed-hardware performance runner | adopt (infrastructure) | Goal 20 |
| G14 | Committed API-compatibility baseline + release notes | adopt | Goal 23 |
| G15 | Trim/AOT/package re-verification at the next candidate | adopt (release gate) | Goal 23 |
| G16 | `net11.0` targeting | defer until GA | constitution |
| G17 | Public `Unit` type | reject | constitution |
| G18 | System.Text.Json converters | defer with documented triggers | constitution |

## Unverified judgments

- Maintainer acceptance was recorded on 2026-09-17 (A-1=A, A-2=B, A-3=A, A-4=A, A-5=A,
  A-6=A, A-7=A, A-8=A).
- Production-scale throughput of `Effect` operators and the StateTransition redesign target;
  only ShortRun hosted data exists today (`analysis/funny-sharp-surface.md` §2.5).
- Whether a closed-generic (AOT-safe) Option/Result JSON converter is feasible in .NET 10
  (`analysis/baseline-funcky.md` F1.5).
- OpenAPI inference impact of returning `IResult` from the adapter; Goal 22 must test it.
- Consumer demand triggers for Option comparers, awaitable Option, `Memoize`, and k-way merge;
  recorded in the analysis memos with explicit triggers.
- language-ext stable-line maintenance risk (E140) is inferred from tag/commit dates; no
  EOL/deprecation statement was read.
- The analysis memos carry the full per-workstream `UNVERIFIED` inventory; this list records the
  decision-relevant subset.
