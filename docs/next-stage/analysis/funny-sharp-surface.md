# FunnySharp current public surface — Goal 14 analysis memo

Scope: F1–F11 assessment of the stable FunnySharp surface at commit
`4dbebd94b7b58648632112b7ca47c39cc517f153` (main), version `0.1.0`, plus the single
`FunnySharp.AspNetCore` extensions type. Product/scope analysis only; no implementation, no
library edits.

Evidence base (all local paths relative to the repository root):

- `docs/next-stage/inventory/generated/inv-funny-sharp-core.md` / `.json` — 33 types, 254 members
  (SHA256 md `8660a1f…`). This memo cites that dump by md line number; the shared generated
  directory was refreshed by a parallel Goal 14 workstream during this analysis, and all citations
  here were re-verified against the current copies.
- `docs/next-stage/inventory/generated/inv-funny-sharp-aspnetcore.md` / `.json` — 1 type,
  15 members (SHA256 md `a33cde5…`).
- Repository docs `docs/*.md`, `README.md`, `docs/goals/0015..0023-goal.md`, `TODO.md`.
- Tests under `tests/FunnySharp.Tests` and `tests/FunnySharp.AspNetCore.Tests`; examples under
  `examples/`; performance evidence `eng/performance/baseline.json` and the generated tables in
  `docs/*.md`.
- Local verification performed 2026-09-17 on Linux x64 with SDK 10.0.400 / runtime 10.0.11:
  `FunnySharp.Tests` 305 passed, `FunnySharp.AspNetCore.Tests` 18 passed, 0 failed/skipped.
  Compiled XML docs: 275 core members (33 types + 224 methods + 14 properties + 4 fields), 16
  ASP.NET Core members; every entry has `summary` or `inheritdoc`.
- Two compiler probes run outside the repository (`/tmp/opencode/optprobe`, `/tmp/opencode/expprobe`)
  against `src/FunnySharp/bin/Release/net10.0/FunnySharp.dll` and the net10 reference pack. Probe
  results are quoted with compiler diagnostic codes; the probe sources are not committed.

Decision vocabulary: keep / redesign / experimental / remove for FunnySharp APIs; adopt / adapt /
defer / reject for capabilities that are not currently APIs. Criteria are referenced as
C1 ordinary C# usefulness, C2 consumer-side semantic LOC, C3 clarity from signatures,
C4 compile-time verifiability, C5 AI predictability, C6 BCL interoperability, C7 async and
cancellation semantics, C8 credible performance feasibility.

Unverifiable judgments are marked **UNVERIFIED: reason** inline.

---

## 1. Surface summary

| Assembly | Types | Members (dump) | XML-documented members | Notes |
| --- | ---: | ---: | ---: | --- |
| `FunnySharp` 0.1.0.0 | 33 | 254 (222 methods, 98 of them extension methods; 14 properties; 8 operators; 3 ctors; 3 delegate Invoke; 4 enum values) | 275 | One namespace `FunnySharp`; no FunnySharp-declared interface or event types, 3 delegate types, 1 enum, 8 readonly structs |
| `FunnySharp.AspNetCore` 0.1.0.0 | 1 | 15 (all extension methods) | 16 | Depends on core + `Microsoft.AspNetCore.App` only (`docs/product-contract.md §"Package And Dependency Boundary"`) |

Everything is decided per type in §5. Family membership is inventoried in
`docs/next-stage/inventory/funny-sharp.md` (F1–F11), with the full member lists in the two generated
dumps.

Structural observations:

- All core carriers are `readonly struct` value types with private constructors plus static
  factories (`Option<T>` line 74, `Result<TValue,TError>` line 358, `Validation<TValue,TError>`
  line 10, `TransitionResult<…>` line 37 of `src/FunnySharp/…`); `StateChange` is the only sealed
  class; optics are readonly structs wrapping delegates. No public mutable state, no inheritance
  hierarchy, no FunnySharp-declared interface types. This matches the BCL-first contract
  (`docs/product-contract.md §"Product Direction"`).
- The whole stable surface is expression-oriented except the `Match(Action…)` overloads, which
  return `void`; every public method that accepts a delegate validates it eagerly
  (e.g. `src/FunnySharp/Result.cs:425-426`, `src/FunnySharp/Option.cs:130-131`,
  `src/FunnySharp/FunctionExtensions.cs:18`).
- 98 of 222 core methods are extension methods; the design relies on extension resolution rather
  than instance APIs for bridges and traversal. This is ordinary C#-friendly (C1) and keeps
  `readonly struct` carriers small (C6, C8), but extension-based discovery is weaker for AI and
  IntelliSense than instance members (C5) — flagged, not blocking.

## 2. Cross-cutting investigations (F11)

### 2.1 Default and uninitialized states

| Carrier | `default` state | Documented | Tested | Goal 15 position (`docs/goals/0015-goal.md:2`) |
| --- | --- | --- | --- | --- |
| `Option<T>` | `None` | `docs/option.md:8` | `tests/FunnySharp.Tests/OptionTests.cs:64-73` | Explicitly allowed ("Option's default remains a valid None") |
| `Result<TValue,TError>` | failure containing `default(TError)` | `docs/result.md:11-13` | `tests/FunnySharp.Tests/ResultTests.cs:8-19` | "must not masquerade as legitimate domain outcomes" — currently indistinguishable from `Failure(default(TError))` |
| `Validation<TValue,TError>` | `Invalid([default(TError)])` | `docs/validation.md:9-12` | `tests/FunnySharp.Tests/ValidationTests.cs:8-19` | Same requirement; currently indistinguishable from `Invalid(default(TError))` |
| `TransitionResult<TState,TOutput,TError>` | `Undefined` | `docs/state-machines.md:50-52` | `tests/FunnySharp.Tests/StateMachineTests.cs:37-42` | Consistent: `Undefined` is a meaningful machine status, and the state-machine docs already treat "no handler" as a real outcome |
| `Effect<T>` / `Effect<TEnvironment,T>` | uninitialized; `RunAsync` returns a faulted `ValueTask` with `InvalidOperationException` | `src/FunnySharp/Effect.cs:262-266`, `:388-392` | `tests/FunnySharp.Tests/EffectTests.cs:99-104`, `:471-478` | Runtime failure only; no type-level detection (C4) |
| `Lens<TSource,TFocus>` / `Optional<TSource,TFocus>` | uninitialized delegates; every operation including `Compose` throws `InvalidOperationException` | `docs/immutable-updates.md:155-164` | `tests/FunnySharp.Tests/OpticsTests.cs:198-213` | Runtime failure only; acceptable for an opt-in surface but C4-weak |

Finding: `Result` and `Validation` defaults are exactly the "masquerade" pattern Goal 15
prohibits, while `Option`, `TransitionResult`, `Effect`, and optics have coherent default stories.
`default(Result<int,string>)` is `Failure(null)` and `default(Validation<int,string>)` is
`Invalid([null])`; a consumer cannot distinguish "never initialized" from a real domain outcome
without additional signaling. This is the single most consequential semantic decision in the
matrix (§5, §6).

### 2.2 Naming consistency across carriers

Observed naming rules, per family:

| Verb family | Rule observed | Evidence |
| --- | --- | --- |
| `MapAsync`/`BindAsync` | callback returns `Task` | `src/FunnySharp/OptionExtensions.cs:56,77,98,119`; `src/FunnySharp/ResultExtensions.cs:66,92,118,144` |
| `MapValueAsync`/`BindValueAsync` | callback returns `ValueTask` | `src/FunnySharp/OptionExtensions.cs:140,161,182,203`; `src/FunnySharp/ResultExtensions.cs:170,196,225,251` |
| `TapAsync`/`TapValueAsync` | observer returns `Task` / `ValueTask` | `src/FunnySharp/FunctionExtensions.cs:220,234,250,264` |
| `ComposeAsync` | one name for **both** Task and ValueTask shapes | `src/FunnySharp/FunctionExtensions.cs:130,153,176,199` |
| `Choose` / `ChooseValueAsync` | sync chooser / `ValueTask` chooser (no Task chooser) | `src/FunnySharp/AsyncEnumerablePipelineExtensions.cs:21,44,66` |
| `SequenceAsync` / `TraverseAsync` / `TraverseValueAsync` | `TraverseAsync` takes a **synchronous** selector over `IAsyncEnumerable<T>`; `TraverseValueAsync` takes a `ValueTask` selector | `src/FunnySharp/AsyncSequenceExtensions.cs:19-29,47-59,77-116` |
| `FirstSuccessAsync`, `SelectParallelValueAsync`, `TraverseParallelValueAsync` | `ValueAsync` = ValueTask selector | `src/FunnySharp/ConcurrentEffectExtensions.cs:26`; `ParallelAsyncEnumerableExtensions.cs`; `ParallelAsyncSequenceExtensions.cs` |
| `GetValueOr`/`GetValueOrElse` + `OrElse`/`OrElseWith` | eager vs factory fallback | `src/FunnySharp/Option.cs:212-263` |
| `GetValueOrDefault` | third fallback form that may return `default(T)` including runtime-null | `src/FunnySharp/Option.cs:244-245`; `docs/option.md:37` |

Findings:

1. The `Async`/`ValueAsync` distinction does **not** carry one meaning. In
   `MapAsync`/`BindAsync`/`TapAsync` it means "the supplied delegate returns `Task`"; in
   `TraverseAsync`/`SequenceAsync` it means "the source is `IAsyncEnumerable<T>`" and the selector
   is synchronous. A reader applying the `MapAsync` rule to `TraverseAsync` would expect a
   Task-returning selector and be wrong (C3, C5). The docs partially compensate
   (`docs/validation.md:72-75,92-101`) but the API grammar itself is ambiguous.
2. `ComposeAsync` collapses both async kinds into one name while the other verb families split
   them, so the grammar is not local either (C3, C5).
3. `GetValueOrElse` (lazy value fallback) and `OrElseWith` (lazy option fallback) use different
   factory suffixes (`Else` vs `With`), while the eager forms are `GetValueOr` and `OrElse`
   (C5). The split itself is defensible (value vs option), but the suffix vocabulary is not
   uniform; `GetValueOrElse` is also a longer synonym for `GetValueOr` applied to a factory
   invocation.
4. Candidate canonical rule if the lead wants one, proposed for maintainer ratification only:
   `Async` = the operation crosses an asynchronous boundary (Task/ValueTask/IAsyncEnumerable);
   `ValueAsync` = the *callback* returns `ValueTask`. Under that rule `MapAsync` should accept
   either Task or ValueTask (as `ComposeAsync` already does) and `MapValueAsync` becomes the
   explicit ValueTask-callback form; `TraverseAsync` is acceptable. **UNVERIFIED: canonical
   vocabulary acceptance is the maintainer's call per the Goal 14 text; this memo only records the
   inconsistency and a candidate rule.**

### 2.3 `FirstSuccessAsync` returns `Validation<TValue,TError>`

Evidence: `src/FunnySharp/ConcurrentEffectExtensions.cs:26,53,81,287`;
`docs/concurrency.md:116-118`; test `tests/FunnySharp.Tests/FirstSuccessTests.cs:42-63`.

The API is semantically a winner-take-all race with a fallback accumulation of typed failures.
Returning `Validation` means: (a) a success is wrapped as `Valid`; (b) an all-failure race is
`Invalid(errors-in-input-order)`. Problems against the criteria:

- C3/C5: a reader sees `Validation<TValue,TError>` from a method named `FirstSuccessAsync` and
  cannot tell whether failure is fail-fast or accumulating; the shape collides with F3 semantics
  where `Validation` means "independent checks that all run" (`docs/validation.md:3-5`), whereas
  here all failures did run but are alternatives, not independent field errors.
- C2: callers who only want the winner must still handle the multi-error carrier; the common path
  (`Valid`) is not distinguishable in type from a validation API.
- C6: `Result<TValue, IReadOnlyList<TError>>` would be the BCL-shaped equivalent and composes with
  existing `Result`/HTTP mapping; `Validation` composes with `Apply`/`Zip` accumulation that does
  not apply here.

Counter-argument: the existing shape cannot lose errors (all typed failures retained) and tests
pin the behavior. Recommendation in §5: redesign the return type to
`Result<TValue, IReadOnlyList<TError>>` (or, second choice, keep and rename to
`TryFirstSuccessAsync`); this is a breaking change that is acceptable at `0.1.0` preview.
**UNVERIFIED: the maintainer may prefer to keep the accumulation shape; the qualitative preference
is not derivable from evidence.**

### 2.4 `StateTransition.Then` left-associated composition cost

Evidence: `src/FunnySharp/StateTransition.cs:151-163` allocates a new combined `TOutput[]` on every
invocation of every composite; `StateChange.To` also clones outputs (`src/FunnySharp/StateTransition.cs:36-40`).
Generated table `docs/state-machines.md:110-115`: Count=8 85.43x / 1,792 B vs 56 B; Count=64
210.92x / 22,176 B vs 280 B; Count=256 457.67x / 187,296 B vs 1,048 B. Allocation budgets in
`eng/performance/baseline.json` still admit the quadratic curve (234,152 B at Count=256;
27,752 B at Count=64; 2,272 B at Count=8; rationale text "Exploratory characterization plus 25% and
32 B fixed headroom; acceptance uses a later measurement"). Goal 20 states the long-chain
`StateTransition` issue "must therefore be resolved or excluded from the stable contract"
(`docs/goals/0020-goal.md:2`).

Assessment: this is a real algorithmic cliff in a stable API (C8), while the semantics are
otherwise good (C1-C4). The 0.1.0 surface cannot retain it unchanged. Resolve options: (a) make
`Then` accumulate outputs into one list per run (single materialization) and/or expose a
`StateTransition.Build`/chain type that composes without per-level copying; (b) declare long
left-associated chains out of the stable contract and keep only bounded chains, enforced by docs
and a benchmark-based acceptance threshold (weaker, still needs an explicit bound). The decision
row in §5 selects redesign; Goal 20/19 decides the implementation shape later.

### 2.5 Effect benchmark overhead

Evidence: generated table `docs/effects.md:122-138`; benchmark source
`benchmarks/FunnySharp.Benchmarks/EffectBenchmarks.cs`; `eng/performance/baseline.json` has
21 `EffectBenchmarks` policy rows (20 included; 5 carry allocation budgets).

Measured ratios versus direct .NET code: Map composition 17.46x; wrapper construction 12.14x
(88 B vs 0 B); Environment `Provide` 10.16x; `UsingAsync` 7.93x; `Using` 7.33x; completed
synchronous `RunAsync` 5.88x; Bind ValueTask composition 4.44x (208 B vs 24 B); completed ValueTask
map 3.33x; completed Task composition 2.73x; completed value `RunAsync` 1.86x. The docs explicitly
present these as "the boundary's cost rather than claiming that it is free"
(`docs/effects.md:140-141`).

Assessment: no stable path may impose an undisclosed material regression without additional
visible semantics (C8, Goal 20). `Effect` adds deferred execution, explicit cancellation, and
resource lifetime (`docs/effects.md:11-25,75-92`), so the overhead is disclosed and semantically
motivated; absolute costs are small (tens of nanoseconds, ≤208 B). Keep, but the next release must
(a) document complexity/allocation per factory and operator, and (b) keep the "not a runtime"
boundary (`docs/effects.md:94-102`). `Effect.Map` at 17x should get a targeted optimization or an
explicit complexity note before any "performance trust" claim.
**UNVERIFIED: production-scale throughput impact; only ShortRun hosted measurements exist
(`docs/release-evidence/goal-12.md:107-112`).**

### 2.6 Partial / Flip / Curry arity limits

Evidence: only binary shapes exist — `Curry` `src/FunnySharp/FunctionExtensions.cs:49-55`,
`Uncurry` `:65-71`, `Partial` `:82-89`, `Flip` `:99-105`; deliberate boundary documented in
[function-composition.md — Deliberate Boundaries](../../function-composition.md#deliberate-boundaries) ("Arbitrary arities would require a large overload family with
limited additional discoverability"). `Partial` binds only the first argument
(`src/FunnySharp/FunctionExtensions.cs:82-89`).

Assessment: the limit is documented and a lambda remains the ordinary fallback (C1, C2, C5). Keep.

### 2.7 `Option.FromNullable` overload selection and `Option.Some(null)` policy

Evidence: two constrained overloads `src/FunnySharp/Option.cs:40-52`; `Some` rejects null at runtime
`src/FunnySharp/Option.cs:105-108,296-307`; documented `docs/option.md:27-31`; tests
`tests/FunnySharp.Tests/OptionTests.cs:82-87` and
`tests/FunnySharp.Tests/OptionInteropTests.cs:45-48`.

Compiler probe results (net10, `Nullable enable`, 0 warnings for the good cases):

| Case | Result |
| --- | --- |
| `Option.FromNullable((int?)0)`, `Option.FromNullable<string>(null)`, `FromNullable(string?)`, `FromNullable(int?)`, `FromNullable<T>(T?) where T : class`, `FromNullable<T>(Nullable<T>) where T : struct` | compile, no warnings |
| `Option.FromNullable(value)` where `T` is unconstrained | **CS0452** — `T` must be a reference type for the class overload; the struct overload also does not apply |
| `Option.FromNullable(value)` where `value` is `T?` and `T` unconstrained | **CS0452** |
| `Option.Some(null)` with an untyped null literal | **CS0411** — type argument cannot be inferred |
| `Option.Some<string?>(null)` | warning **CS8625** (null literal to non-nullable reference) |
| `Result<string,int>.Success(null)` | warning **CS8625**; `Success(null!)` or `Result<string?,int>` compiles |

Findings: (a) dedicated `Nullable<T>` conversion unwraps to `Option<T>` while generic transforms
such as `Map<int?>` preserve `Option<int?>` — documented and tested (`docs/option.md:27-28`).
(b) The no-null invariant is enforced by runtime throw plus `[DisallowNull]` and compiler
warnings, not by the type system: `Option<string?>` is a legal type argument yet
`Option<string?>.Some(null)` still throws (test `OptionInteropTests.cs:45-46`). (c) Unconstrained
generic code cannot use `Option.FromNullable` at all (CS0452) and `Option<T>.FromNullable` is
`internal` (`src/FunnySharp/Option.cs:292-293`), so generic absence conversion requires the
Try-pattern or an explicit null check — an inference-friction finding for C4/C5. Recommendation:
keep the runtime policy; document the generic-code guidance (`Option.FromTry` or explicit branch)
in the Option guide; consider a public `Option<T>.FromNullable`-equivalent only if a real consumer
case appears (**UNVERIFIED: no consumer evidence today**).

### 2.8 `Result.Try` exception taxonomy

Evidence: `src/FunnySharp/Result.cs:18-44` (sync), `:61-141` (async), `:283-350` (cores),
`:303-307` (null task); docs `docs/result.md:77-105`; tests
`tests/FunnySharp.Tests/ResultBoundaryTests.cs:9-22,46-58,60-74,139-156,158-191,335-355,357+`.

Taxonomy as shipped:

1. `OperationCanceledException` is never converted to a failure: sync cancellation is rethrown;
   async cancellation stays a canceled `Task`/`ValueTask`; a faulted awaitable containing an
   `OperationCanceledException` stays faulted (`Result.cs:40,293-301,326-334`).
2. Other exceptions map through the caller mapper; the default mapper returns the exact exception
   object preserving identity and stack (`Result.cs:143`; `docs/result.md:79-83`).
3. Mapper exceptions are not swallowed: synchronous mapper failures propagate (test
   `ResultBoundaryTests.cs:60-74`); async mapper failures fault the returned task
   (`Result.cs:338-350`).
4. A null task returned by the delegate faults with `InvalidOperationException` and is not sent to
   the mapper (`Result.cs:303-307`; docs `docs/result.md:99-101`).
5. Async boundaries are deliberately tokenless; callers pass their token inside the delegate
   (`Result.cs:53-60`; docs `docs/result.md:92-97`).

This is a strong C3/C4/C7 story. The one open question is whether a tokenless exception boundary
should be the canonical shape for ordinary application code (Goal 18 mentions `CancellationToken`
participation). Keep, with documentation.

### 2.9 Optics law obligations and default-throwing behavior

Evidence: laws are caller obligations `docs/immutable-updates.md:54-64`; default-throwing
`docs/immutable-updates.md:155-164`, `src/FunnySharp/Optics.cs:140-146,309-315`, tests
`tests/FunnySharp.Tests/OpticsTests.cs:198-213`; absent-focus identity
`docs/immutable-updates.md:85-89`, `src/FunnySharp/Optics.cs:210-247`.

Findings: (a) law enforcement is impossible at runtime without breaking the thin-delegate design;
documented obligations plus tests are the honest maximum, so C4 is structurally weak for optics
but the contract is explicit. (b) Default optic values throw on every operation, including
`Compose` — a coherent uninitialized policy, unlike `Result`/`Validation`. (c) No prism/traversal/
iso hierarchy exists, matching the product contract (`docs/product-contract.md §"Product Direction"`). Keep.

### 2.10 Result/Option LINQ alias policy

Evidence: `Result<TValue,TError>` has `Select`/`SelectMany` delegating to `Map`/`Bind`
(`src/FunnySharp/Result.cs:600,611-631`) and deliberately no `Where`
(`docs/result.md:29-32`); both `Effect` shapes have `Select`/`SelectMany`
(`src/FunnySharp/Effect.cs:313,326,441,454`); `Option<T>` and
`Validation<TValue,TError>` have none (`generated/inv-funny-sharp-core.md` type sections;
`docs/option.md:58-59`, `docs/validation.md:30-33`).

Finding: LINQ support is carrier-specific. Option query syntax is unavailable even though
`Select`/`SelectMany` would be mechanical aliases; a consumer who learned `from x in result ...`
from Result cannot apply it to Option. Goal 16 expects "any LINQ syntax bridge remains secondary to
the canonical vocabulary" (`docs/goals/0016-goal.md:2`), which allows either policy but not
silence. Decision in §5.4: keep the Result/Effect aliases; add Option aliases or record a documented
exception; Validation must not grow `SelectMany` (no `Bind`).

### 2.11 XML documentation coverage

Evidence: `src/FunnySharp/bin/Release/net10.0/FunnySharp.xml` analyzed locally: 275 documented
members (33 types, 224 methods, 14 properties, 4 fields); 259 with `summary`, 16 with
`inheritdoc`, 0 missing. ASP.NET Core: 16 members, all documented. `GenerateDocumentationFile` and
`TreatWarningsAsErrors` are enabled (`src/FunnySharp/FunnySharp.csproj:16`,
`Directory.Build.props:6`), so CS1591 would fail the build. Historical release evidence matches
(`docs/release-evidence/goal-12.md:50-53`).

Finding: coverage is complete by the project's definition. Residual quality questions (per-parameter
null/cancellation contracts, evaluation-order statements) are sampled in this memo and are strong
on the central semantics (`docs/result.md:43-75`, `docs/option.md:29-47`), thinner on
`Effect.FromValue` null policy (§2.12). No missing-documentation action needed; add a doc-style
requirement for null policy and complexity to the next-stage contract (F11).

### 2.12 `Effect.FromValue(T?)` nullability

Evidence: source `src/FunnySharp/Effect.cs:16-17` — `public static Effect<T> FromValue<T>(T value)`
with no null check and no null-policy remark; generated dump line 47 renders
`Effect<T> FromValue<T>(T? value)`; no test or example exercises a null value
(`grep -rn "FromValue.*null" tests examples` → no matches); `FromResult` flows through `FromValue`
(`src/FunnySharp/Effect.cs:26-28`).

Finding: `Effect.FromValue<string?>(null)` is legal, produces an effect that yields null on every
run, and is indistinguishable in docs from a non-null value effect. This conflicts with the
Option-style no-null policy and with Goal 23's "nullability … contracts" requirement
(`docs/goals/0023-goal.md:2`). The dump's `T?` is a NullabilityInfoContext rendering of the
unconstrained parameter, not a source annotation (contrast `Option.Some<T>([DisallowNull] T)` which
the dump also shows as `T?`). Recommendation: redesign the documentation/contract only — state
either "null is a value" (consistent with `Result.Success(null)`, `ResultTests.cs:21-31`) or add an
explicit rejection; do not leave it unstated. If a runtime rejection is chosen, note that it would
differ from `Result.Success(null)` semantics. **UNVERIFIED: the desired null policy is a product
judgment; the current absence of any policy is verified.**

### 2.13 Nullability annotations in the public surface

Evidence: dump rendering rules (`/tmp/opencode/tools/api-inventory/Program.cs:349-369`): only
parameters whose `NullabilityInfoContext` state is `Nullable` (and not `Nullable<T>`) receive `?`;
properties print `(nullability: X)` only when not `NotNull`. The JSON dumps contain no nullability
data at all (verified: no `nullability` key in `generated/inv-funny-sharp-core.json`).

Observed surface:

- Exactly one member is flagged: `StateChange<TState,TOutput>.State` renders
  `(nullability: Nullable)` (`generated/inv-funny-sharp-core.md:284` — the property is declared
  `TState State` with an unconstrained `TState`, so it advertises "may be null" and enforces
  nothing; `StateChange.To` accepts any `TState` including null, `src/FunnySharp/StateTransition.cs:36-40`).
- ASP.NET Core optional success mappers render genuinely nullable: `Func<T,IResult>? some`
  (`generated/inv-funny-sharp-aspnetcore.md:11-25`), matching source default `= null`
  (`src/FunnySharp.AspNetCore/HttpResultExtensions.cs:23,41,59,…`).
- Unconstrained type parameters render as `T?` in the markdown (e.g. dump lines 47, 77-78, 125, 234,
  244, 324, 327), which is a dump artifact, not source intent. Source-level annotations are
  actually present and correct in the important places: `[DisallowNull]` + `[return: NotNull]` on
  Option value/fallback members (`src/FunnySharp/Option.cs:105,211-212,226-227`),
  `[NotNullWhen(true)]` on `TryGetValue` (`Option.cs:115`), `[MaybeNull]` on `out` results
  (`Result.cs:399,410`; `Validation.cs:75`; `StateMachine.cs:136`), `[NotNullWhen(true)]` on
  `TryGetErrors`/`TryGetChange` (`Validation.cs:86`; `StateMachine.cs:119`).

Finding: real nullability annotations exist where they matter; the only public-surface weakness is
`StateChange.State`, which cannot be non-null-annotated while `TState` is unconstrained. Flag for
the contract: do not treat dump `?` on unconstrained `T` as a source annotation. No redesign
required.

### 2.14 Stability boundary and experimental marking

Evidence: no `ExperimentalAttribute`, no `PublicAPI`/`ApiCompat` baseline, no analyzer project
anywhere in `src/`, `tests/`, or `eng/` (`grep -rn "Experimental|PublicAPI|ApiCompat"` — no
matches outside build caches). The product contract defers first-party analyzers
(`docs/product-contract.md §"Analyzers And Compiler Feedback"`) and general unions (§"Deliberate Deferrals"), and the current release occupies
one stability tier. Local probe confirms `System.Diagnostics.CodeAnalysis.ExperimentalAttribute`
works on net10: `[Experimental("FSX0001")]` on a type makes a consumer use fail compilation with
diagnostic `FSX0001` (suppressible).

Finding: Goal 23 requires experimental APIs to be unmistakable
(`docs/goals/0023-goal.md:2`), and Goal 19 requires "stable-versus-experimental API inventory"
(`docs/goals/0019-goal.md:2`). Today there is no mechanism and no inventory. Recommendation:
adopt `ExperimentalAttribute` + a `docs/…-stability.md` inventory as part of the next-stage
constitution; decide which of the current 33 types are stable 1.0. My recommendation: stabilize
Option / Result / Validation / sequence / traversal / HTTP mapping after the §5 redesigns;
keep Effect, optics, and state machines stable as well (they are heavily documented and tested)
but mark any *new* Goal 15–23 capability experimental until it has its own evidence.
**UNVERIFIED: the stability tiering is a maintainer acceptance item, not an evidence question.**

### 2.15 Compile-time verifiability summary (C4)

| Hazard | Rejected by compiler? | Rejected at runtime? | Guidance exists? |
| --- | --- | --- | --- |
| `Option.Some(null)` | Warning CS8625 / CS0411; legal via `null!` or `Option<string?>` | `ArgumentNullException` | `docs/option.md:31` |
| Unconstrained generic → Option conversion | CS0452 | n/a | Not documented (finding §2.7) |
| default(Result)/default(Validation) | No | No — indistinguishable from real outcomes | `docs/result.md:13`, `docs/validation.md:11` |
| Uninitialized Effect / optics | No | `InvalidOperationException` | Yes (`docs/effects.md`, `docs/immutable-updates.md:155-164`) |
| Discarded Result/Validation | No | No | No analyzer (Goal 21 gap) |
| Cancellation → domain failure | n/a | Prevented in `Result.Try`, traversal, HTTP mapping | Yes |
| Non-exhaustive case handling | `Match` requires all branches at compile time; no union type, so impossible states are not type-rejected | n/a | Yes |

Observation for Goal 21: the only compiler-enforced hazard today is `Match` exhaustiveness and
delegate nullability; the rest is documentation + tests. This motivates the analyzer gap in §6.

### 2.16 Package, dependency, and test integrity (F11)

- Core references only platform assemblies; enforced by test
  `tests/FunnySharp.Tests/PackageBoundaryTests.cs:8-19` and by the package contract
  `docs/product-contract.md §"Package And Dependency Boundary"`.
- Exactly two packable projects (`docs/product-contract.md §"Baseline Verification"`); ASP.NET Core is a separate
  optional package with a framework reference only.
- Trim/AOT: `IsTrimmable` is set; `IsAotCompatible` is deliberately not claimed because the
  .NET 10.0.11 compiler cannot materialize full-assembly open-generic roots
  (`docs/product-contract.md §"Trimming And Native AOT Policy"`). Historical evidence only (`docs/release-evidence/goal-12.md:76-104`,
  marked historical at its line 5). **UNVERIFIED: trim/AOT at this exact commit was not re-run by
  this agent.**
- Tests: 305 core + 18 ASP.NET Core pass locally at the pinned build; the release evidence of
  record is fail-closed (`docs/release-readiness.md:16-24,84-87`).

## 3. Family assessments (F1–F10)

Each family lists purpose, current semantics with citations, representative call sites, and the
eight criteria. Decisions are consolidated in §5.

### F1 Absence — `Option`, `Option<T>`, `OptionExtensions`, `TryOperation<T>`

Purpose and semantics: `docs/option.md:3-4,6-27`; two-case value with no flattening
(`docs/option.md:33`), no throwing accessor or implicit conversion (`docs/option.md:10`), and
fault/cancellation never becoming absence (`docs/option.md:45`). Call sites:
`examples/FunnySharp.Examples/Program.cs:90` (`Option.Some(0)` proves non-null default is
present), `:108` (`FromTry`), `:114-123` (`Map`/`Bind`/`OrElseWith` short-circuit),
`:364-379` (`MapAsync`/`MapValueAsync`, short-circuit without invoking the callback),
`examples/FunnySharp.AspNetCore.Examples/Program.cs:42-45` (`Option` at the HTTP boundary).

Criteria: C1 high (absence is a daily C# problem; no dialect); C2 good (one expression replaces
null checks); C3 good (`IsSome`/`TryGetValue`/`Match` are self-explanatory; async naming caveat
§2.2); C4 partial (`Some` null rejection is partly compiler-visible; generic conversion friction
§2.7); C5 good but weakened by LINQ asymmetry (§2.10); C6 strong (dictionary/Task/ValueTask
bridges, `docs/option.md:19-27`); C7 strong (faults and cancellation propagate; ValueTask observed
once, `docs/option.md:43-47`); C8 strong (0 B in every synchronous and ValueTask row; the
completed-Task row allocates 216 B vs 144 B and runs 1.62x; mapping runs 1.62x-3.11x on completed
tasks with small absolute values, `docs/option.md:77-98`).

### F2 Fail-fast value outcome — `Result`, `Result<TValue,TError>`, `ResultExtensions`

Purpose and semantics: `docs/result.md:3-7,9-17`; canonical fail-fast carrier with explicit
exception boundaries (`docs/result.md:77-105`). Call sites:
`examples/FunnySharp.Examples/Program.cs:135-166` (`Try` + `Match` mapping a checkout failure),
`:395-412` (`BindAsync`/`MapValueAsync`/`TryAsync` with exception identity).

Criteria: C1 high (result-shaped error handling is the core business pattern); C2 high;
C3 strong (fail-fast and left-to-right are documented and testable); C4 eroded only by the default
state (§2.1); C5 strong except the default trap; C6 strong (`Exception` boundary, Option interop,
Task/ValueTask); C7 strong (the most carefully specified cancellation story in the surface,
§2.8); C8 acceptable (1.18x-1.20x exception boundary; completed Task 1.87x with 264 B vs 144 B;
documented at `docs/result.md:136-149`).

### F3 Accumulation — `Validation<TValue,TError>`, `ValidationExtensions`

Purpose and semantics: `docs/validation.md:3-5,7-41`; applicative error accumulation with no
`Bind` contract (`docs/validation.md:30-33`); snapshot semantics for `InvalidMany`
(`Validation.cs:57-68`); traversal accumulation (`docs/validation.md:43-66,68-101`). Call sites:
`examples/FunnySharp.Examples/Program.cs:180-183` (`Traverse(ValidateAccount)` collecting two
errors), `:417-427` (`TraverseValueAsync`), `:498-517` (parallel accumulation),
`examples/FunnySharp.AspNetCore.Examples/Program.cs:52-73,137-158` (field errors to
`HttpValidationProblemDetails`).

Criteria: C1 high (batch/field validation); C2 high; C3 strong (the "all errors" contract is in
the name and docs; deliberately no Bind); C4 eroded by default state (§2.1); C5 good;
C6 strong (`IReadOnlyList<TError>` is BCL-idiomatic; HTTP mapping); C7 good (sequential and
parallel traversals forward tokens; parallel accumulation has bounded concurrency); C8 good
(1.10x-1.28x accumulation, equal allocations 392 B/12,632 B at `docs/validation.md:130-142`;
parallel Validation 0.77x-1.22x at `docs/concurrency.md:173-176`).

### F4 Function grammar — `FunctionExtensions`

Purpose and semantics: [API Shape](../../function-composition.md#api-shape) and
[Evaluation And Failure Semantics](../../function-composition.md#evaluation-and-failure-semantics);
eager delegate validation, no exception wrapping, `ConfigureAwait(false)`, exact token forwarding. Call sites:
`examples/FunnySharp.Examples/Program.cs:25-40` (`Pipe`, `Compose`, `Curry`, `Partial`, `Flip`,
`Tap`), `:280-328` (async compose/tap incl. cancellation).

Criteria: C1 medium (helpers are conveniences; lambdas are the fallback); C2 medium (saves a local
variable or lambda per stage); C3 strong for sync, weakened by `ComposeAsync` naming (§2.2);
C4 good (delegate nullability and arity are compiler-checked); C5 medium (multiple equivalent ways
to read a pipeline — `Pipe` vs `Compose` vs nested calls — but only one async carrier per name);
C6 strong (standard delegates only); C7 good when delegates are well-behaved; a delegate that
ignores a canceled token still runs, documented in
[function-composition.md — Evaluation And Failure Semantics](../../function-composition.md#evaluation-and-failure-semantics);
C8 mixed: sync invocation 5.77x, delegate construction 1.94x with 96 B vs 64 B
([function-composition.md — Performance Evidence](../../function-composition.md#performance-evidence)) — acceptable because the docs disclose it and BCL
`Enumerable`-style composition has no cheaper equivalent for function values; `Pipe`/`Tap` are
identities. Keep with a complexity note.

### F5 Collections and traversal — sequence, async sequence, span/memory

Purpose and semantics: `docs/data-pipelines.md:3-7,9-28,30-53`, `docs/validation.md:43-101`.
Call sites: `examples/FunnySharp.Examples/Program.cs:60-82` (`Choose`, `ChooseTo`, `WhereTo`,
`WhereInPlace`), `:180-183` (`Traverse`), `:342-358` (`ChooseValueAsync` with enumeration token),
`:417-427` (`TraverseValueAsync`).

Criteria: C1 medium-high (`Choose` is a real fused operator; span families are niche);
C2 medium (traversal removes unpack loops; span helpers replace loops one-to-one);
C3 good (deferred vs immediate, ordering, materialization are documented per operator);
C4 medium (allocation/lifetime mistakes in span usage are not type-prevented; documented
contracts at `docs/data-pipelines.md:43-53`); C5 good; C6 strong (BCL carriers only; no
replacement collection universe, `docs/product-contract.md §"Product Direction"`);
C7 good (async traversal is sequential by design; parallel variants are separate types);
C8 mixed and disclosed: `Choose` beats LINQ on `IEnumerable` (0.72x-0.81x) but async `Choose` is
2.35x-3.02x slower than a direct loop, with 312 B per enumeration versus 400 B for the async-LINQ
comparator; span filter-map is
4.17x-4.57x a loop with 0 B allocations (`docs/data-pipelines.md:84-101`). No blanket claim is
made (`docs/data-pipelines.md:100-101`). Traversal has no location context — a goal-derived gap
(§6), not a defect of the current contract.

### F6 Async, streaming, concurrency

Purpose and semantics: `docs/concurrency.md:3-8,10-53,55-90,92-138`. Call sites:
`examples/FunnySharp.Examples/Program.cs:342-358` (async chooser), `:487-497` (ordered bounded
map), `:498-517` (parallel validation), `:519-528` (first success with timeout).

Criteria: C1 high (bounded parallel calls and races are ordinary server problems);
C2 high (coordination LOC is genuinely removed vs manual `Channel`/`Task.WhenAll` plumbing);
C3 mostly strong (ordered vs completion-order, fail-fast vs accumulate, drain behavior documented);
weak point: `FirstSuccessAsync`'s `Validation` return (§2.3); C4 medium (behavior is runtime,
not type-level; policies such as `maxConcurrency` are ints with no positive-value type);
C5 good except the race return shape; C6 strong (`Channel`, `TimeProvider`, `IAsyncEnumerable`,
`ValueTask`; `TimeProvider` only in the timeout overload, verified by grep);
C7 strong (linked tokens, drain-before-return, cleanup aggregation, cancellation precedence;
tested in `tests/FunnySharp.Tests/FirstSuccessTests.cs:117-243,296-440` and
`tests/FunnySharp.Tests/ParallelAsyncEnumerableTests.cs`, `ParallelAsyncSequenceTests.cs`);
C8 acceptable and disclosed: ordered mapping 1.59x-2.12x and more allocation than a known-length
BCL array path, rationalized by streaming + ordering + cleanup; traversal is competitive
(0.76x-1.23x); first success 1.15x-1.60x (`docs/concurrency.md:166-188`).

### F7 Effects, resources, environment

Purpose and semantics: `docs/effects.md:3-7,9-59,61-73,75-92`. Call sites:
`examples/FunnySharp.Examples/Program.cs:432-480` (deferred trace, environment `Provide`,
`TimeProvider`, `Using`/`UsingAsync`), `examples/FunnySharp.AspNetCore.Examples/Program.cs:28-32,97-99`
(effect + request cancellation).

Criteria: C1 high (deferred I/O with explicit dependency is a real pattern);
C2 high (removes try/finally, environment plumbing); C3 good (deferred execution, token
forwarding, resource precedence documented); C4 weak (uninitialized/default path is runtime-only;
no environment-type checking beyond generics); C5 medium (Effect vs plain `Func<ValueTask<T>>`
is a judgment call; docs frame it as a thin boundary, `docs/effects.md:94-102`);
C6 strong (plain delegates; `TimeProvider` as an ordinary value; no `Microsoft.Extensions`
dependency, `docs/product-contract.md §"Package And Dependency Boundary"`); C7 strong (exceptions captured into the returned
`ValueTask`, exact token forwarding, release-on-cancel tested in
`tests/FunnySharp.Tests/EffectResourceTests.cs`); C8 weak-by-measurement but disclosed (§2.5).

### F8 State transitions and machines

Purpose and semantics: `docs/state-machines.md:3-6,8-34,36-57,59-77,79-101`. Call sites:
`examples/FunnySharp.Examples/Program.cs:538-631` (`OrElse`, `Replay`, `Then`, commands as data),
`examples/FunnySharp.DocumentationSamples/StateMachineSamples.cs`.

Criteria: C1 medium (pure decision + emitted commands is valuable where workflows exist);
C2 high for multi-step transitions; C3 strong (four explicit statuses, exhaustive `Match`);
C4 good (statuses are typed; `Undefined` default is deliberate); C5 good except `Undefined` vs
`Rejected` remains a modeling judgment (`docs/state-machines.md:54-57`); C6 strong (plain
delegates/collections, no runtime); C7 not applicable by design and clearly stated
(`docs/state-machines.md:79-101`); C8 failing for left-associated `Then` (§2.4), otherwise fine
(`StateChange.To` clones a `params` array on each call, `src/FunnySharp/StateTransition.cs:36-40`).

### F9 Optics and immutability

Purpose and semantics: `docs/immutable-updates.md:3-13,17-89,91-153`. Call sites:
`examples/FunnySharp.Examples/Program.cs:215-265` (nested record lens, optional dictionary focus,
frozen snapshot replacement).

Criteria: C1 medium (ordinary C# records plus `with` already cover most cases); C2 medium-high for
deep chains (`examples/FunnySharp.Examples/Program.cs:239-253` vs the nested `with` shown at
`docs/immutable-updates.md:36-49`); C3 good (total vs optional focus is in the type; laws are
documented obligations); C4 weak (laws unenforceable, uninitialized optics runtime-only);
C5 medium (optics vs plain `with` is a maintainer judgment; no reflection/property paths keeps it
predictable); C6 strong (`System.Collections.Immutable` and `Frozen*` guidance,
`docs/immutable-updates.md:91-153`); C7 not applicable (sync delegates); C8 good
(1.03x-1.65x, equal allocations; `docs/immutable-updates.md:186-197`).

### F10 HTTP integration

Purpose and semantics: `docs/aspnet-core.md:3-13,15-69,71-88`. Call sites:
`examples/FunnySharp.AspNetCore.Examples/Program.cs:11-32`.

Criteria: C1 high (Minimal API outcome mapping); C2 high (endpoint body becomes one expression);
C3 strong (explicit problem mapper, documented success default, exact `RequestAborted` semantics);
C4 medium (mapper return shapes are runtime-validated: null/absent `Status` throws,
`src/FunnySharp.AspNetCore/HttpResultExtensions.cs:403-409`); C5 good (one canonical mapping verb
per carrier; success mapper optional); C6 strong (`IResult`, `ProblemDetails`; no HTTP types in
domain, `docs/aspnet-core.md:7-13`); C7 strong (exact token; faults not converted; tested in
`tests/FunnySharp.AspNetCore.Tests/HttpResultExtensionsTests.cs:275-359` and
`KestrelCancellationTests.cs`); C8 no numeric claim, documented as application-owned
(`docs/aspnet-core.md:116-121`).

## 4. Specific investigation findings (index)

| Investigation | Where |
| --- | --- |
| Default/uninitialized states | §2.1 |
| Naming consistency (`MapAsync`/`MapValueAsync`/`ChooseValueAsync`/`TraverseValueAsync`, `Compose`/`ComposeAsync`, `GetValueOr`/`GetValueOrElse`) | §2.2 |
| `FirstSuccessAsync` → `Validation` | §2.3 |
| `StateTransition.Then` left-associated cost (85x–457x, Goal 20 resolve-or-exclude) | §2.4 |
| Effect benchmark overhead | §2.5 |
| Partial/Flip/Curry arity limits | §2.6 |
| `Option.FromNullable` overload selection, `Option.Some(null)` policy | §2.7 |
| `Result.Try` exception taxonomy | §2.8 |
| Lens/Optional law obligations and default-throwing | §2.9 |
| Result/Option LINQ alias policy | §2.10 |
| XML documentation coverage (275 core / 16 ASP.NET Core, no gaps) | §2.11 |
| `Effect.FromValue(T?)` nullability | §2.12 |
| Nullability annotations from the dump | §2.13 |
| Stability boundary and experimental marking | §2.14 |
| Compile-time verifiability | §2.15 |

## 5. Candidate decision table — every public type

Legend: decisions are `keep`, `redesign`, `experimental`, `remove`. "Family" names group members
with identical treatment inside a type; the dump remains the exhaustive member list. Rationales
name criteria. Compound entries list one decision per family; no type is omitted. No current type is
recommended as `experimental`; that tier is reserved for new Goal 15–23 capabilities until they
carry their own evidence (§6 G10).

### 5.1 Core (`FunnySharp`, 33 types)

| # | Type | Member families | Decision | Rationale (one line) |
| --- | --- | --- | --- | --- |
| 1 | `AsyncEnumerablePipelineExtensions` | Choose; ChooseValueAsync (± token) | keep | Fused Option filter-map on async streams with no BCL equivalent; disclosed 2.35x-3.02x vs loop, fewer allocations (C1, C6, C8; `docs/data-pipelines.md:16-28,84-98`). |
| 2 | `AsyncSequenceExtensions` | SequenceAsync; TraverseAsync; TraverseValueAsync (± token) | keep | Semantics verified and tested; `TraverseAsync`'s sync selector conflicts with the `MapAsync` Task rule, so the F11 vocabulary decision must either clarify or rename it (C3, C5; §2.2). |
| 3 | `ConcurrentEffectExtensions` | FirstSuccessAsync ×3 overloads | redesign (return type) | Replace `Validation<TValue,TError>` with `Result<TValue,IReadOnlyList<TError>>` (or rename); the accumulation carrier misleads for a race (C3, C5, C6; §2.3). Drain/cleanup semantics stay. |
| 4 | `Effect` (static) | FromValue/FromResult; FromSync; FromTask; FromValueTask; env variants | keep | Thin BCL-first factories; the `FromValue` null policy must be stated in docs without changing the shape (C4, C5; §2.12). |
| 5 | `EffectResourceExtensions` | Using; UsingAsync; env variants | keep | Safety across success/failure/exception/cancellation is tested and documented (C1, C7; `docs/effects.md:75-92`). |
| 6 | `Effect<T>` | RunAsync; Map/Select; Bind; SelectMany; WithEnvironment | keep | Deferred execution, exact token, explicit uninitialized failure; overhead disclosed (C3, C7, C8; §2.5). |
| 7 | `Effect<TEnvironment,T>` | RunAsync; Provide; Map/Select; Bind; SelectMany | keep | Caller-owned environment instead of DI/service location (C1, C6; `docs/effects.md:40-45`). |
| 8 | `EnumerablePipelineExtensions` | Choose | keep | `IEnumerable.Choose` beats LINQ Where+Select in the measured workload with fewer allocations (C1, C8; `docs/data-pipelines.md:91-92`). |
| 9 | `FunctionExtensions` | Pipe; Compose; ComposeAsync (4); Curry; Uncurry; Partial; Flip; Tap; TapAsync; TapValueAsync | keep | Binary-only arity is a documented boundary; sync/async delegation is ordinary C# (C1, C3; §2.6). |
| 10 | `Lens` (static) | Create; Identity | keep | BCL-native, no reflection/hierarchy; factory validates delegates (C6; `docs/product-contract.md §"Product Direction"`). |
| 11 | `Lens<TSource,TFocus>` | Get; Set; Update; Compose(Lens); Compose(Optional) | keep | Laws remain caller obligations by design; uninitialized throws are documented and tested (C3, C4; §2.9). |
| 12 | `Option` (static) | Some; None; FromNullable ×2; FromTry | keep | Inference-friendly factories; constraint split is deliberate (C1, C4; `docs/option.md:8,19-28`). |
| 13 | `OptionExtensions` | MapAsync/BindAsync (± token); MapValueAsync/BindValueAsync (± token); GetOption; ToOption ×2; ToOptionAsync ×4 | keep | Bridges to the three ordinary absence conventions; Task/ValueTask naming is internally consistent (C6; §2.2). |
| 14 | `Option<T>` | Case/factory; TryGetValue; Match ×2; Map; Bind; Filter; Zip; GetValueOr/GetValueOrElse/GetValueOrDefault; OrElse/OrElseWith; equality; ToString | keep | Goal 15 explicitly keeps default `None`; no throwing accessor; no `Value` property (C1-C4; `docs/goals/0015-goal.md:2`, `docs/option.md:8-10`). |
| 15 | `Optional` (static) | Create | keep | Total/Optional split is the minimal optic vocabulary (C1, C3; `docs/immutable-updates.md:66-89`). |
| 16 | `Optional<TSource,TFocus>` | GetOption; Set; Update; Compose(Lens); Compose(Optional) | keep | Absent focus preserves source identity and skips the setter — a real bug-prevention semantics (C1, C4; `docs/immutable-updates.md:85-89`). |
| 17 | `ParallelAsyncEnumerableExtensions` | SelectParallelValueAsync (± token) | keep | Ordered bounded streaming with backpressure and drain-on-dispose; no BCL equivalent (C1, C7; `docs/concurrency.md:10-53`). |
| 18 | `ParallelAsyncSequenceExtensions` | TraverseParallelValueAsync (Option/Result/Validation × ± token) | keep | Bounded eager traversal with explicit fail-fast vs accumulate split (C1, C3, C7; `docs/concurrency.md:55-90`). |
| 19 | `Result` (static) | Try; Try(mapper); TryAsync; TryValueAsync (+ mappers) | keep | The most carefully specified cancellation/exception boundary in the surface (C3, C7; §2.8). |
| 20 | `ResultExtensions` | MapAsync/BindAsync (± token); MapValueAsync/BindValueAsync (± token); ToOption; ToResult ×2 | keep | Consistent async grammar within the family; explicit lossy Option boundary documented (C3, C6; `docs/result.md:34-41`). |
| 21 | `Result<TValue,TError>` | Case/factory; inspection; Map/Bind/MapError/Ensure/Recover/RecoverWith; Zip/ZipWith; Select/SelectMany; equality | **redesign** (default-state signalling) | `default` is indistinguishable from `Failure(default(TError))`, violating Goal 15's no-masquerade requirement; minimal fix is an explicit uninitialized signal (third state, `IsInitialized`, or throwing default accessors) plus analyzer coverage (C4, C5; §2.1, `docs/goals/0015-goal.md:2`). |
| 22 | `SequenceExtensions` | Sequence (Option/Result/Validation); Traverse (Option/Result/Validation) | keep | Current fail-fast/accumulate split is correct; index/key/path context is a missing Goal 17 capability recorded as a separate adopt item (C1, C3; §6 G6). |
| 23 | `SpanPipelineExtensions` | SelectTo; WhereTo; ChooseTo; SelectInPlace; WhereInPlace (span/memory) | keep | Honest lifetimes and mutation semantics; measured slower than a loop and disclosed as directional (C4, C8; `docs/data-pipelines.md:30-53,93-94`). |
| 24 | `StateChange<TState,TOutput>` | To; State/Outputs; equality; ToString | keep | Data-only snapshot with shallow-copy contract stated; no command execution (C1, C3; `docs/state-machines.md:10-14`). |
| 25 | `StateMachineExtensions` | OrElse; Replay | keep | Undefined-only fallback and single-pass replay are deterministic and tested (C1, C3; `tests/FunnySharp.Tests/StateMachineTests.cs:128`, `:268`). |
| 26 | `StateMachine<TState,TEvent,TOutput,TError>` | delegate Invoke (+ plumbing) | keep | Four-status pure transition delegate with exhaustive `Match` (C1, C4; `docs/state-machines.md:36-57`). |
| 27 | `StateTransitionExtensions` | Then | **redesign** | O(n²) output copying makes left-associated chains 85x-457x slower and allocation-quadratic; Goal 20 requires resolve-or-exclude (C8; §2.4, `docs/goals/0020-goal.md:2`). |
| 28 | `StateTransition<TState,TOutput>` | delegate Invoke (+ plumbing) | keep | The transition shape itself is sound; only composition needs redesign (C1, C3). |
| 29 | `TransitionResult<TState,TOutput,TError>` | factories; status; inspection/Match; equality | keep | Four explicit statuses with no payload leakage; `default` = `Undefined` is coherent (C3, C4; §2.1). |
| 30 | `TransitionStatus` | 4 enum values | keep | Stable ordinal contract with `Undefined = 0` matching the default (§2.1). |
| 31 | `TryOperation<T>` | delegate Invoke | keep | Minimal Try-pattern adapter; `[MaybeNull]` annotated (C1, C6; `src/FunnySharp/Option.cs:11`). |
| 32 | `ValidationExtensions` | Apply | keep | The applicative function application is the one combinator `Zip` cannot express (C1, C3; `docs/validation.md:23-28`). |
| 33 | `Validation<TValue,TError>` | case/factory; inspection; Map/MapErrors; Zip; equality | **redesign** (default-state signalling) | Same masquerade problem as Result: `default` equals `Invalid([default(TError)])`; keep the no-`Bind` boundary and accumulation semantics (C3, C4; §2.1). |

Terminology decided here for the record: `StateChange`/`StateTransition`/`StateMachine` remain
the canonical F8 names; the generic parameter order difference between
`StateMachine<TState,TEvent,TOutput,TError>` and `TransitionResult<TState,TOutput,TError>` is
intentional (the event is only an input to the machine) and should be documented, not unified.

### 5.2 FunnySharp.AspNetCore (1 type)

| # | Type | Member families | Decision | Rationale |
| --- | --- | --- | --- | --- |
| 34 | `HttpResultExtensions` | ToHttpResult ×3; ToHttpResultAsync (Task ×3, ValueTask ×3, Effect ×3, env Effect ×3) | keep | Exact `RequestAborted` forwarding, explicit problem mappers, no DI/global policy; a typed-results/OpenAPI API is deferred as a separate item rather than added here (C1, C3, C6, C7; `docs/aspnet-core.md:15-31,71-88,116-121`; §6 G12). |

### 5.3 Cross-type vocabulary decisions (F11)

These member-level policies cut across types and are decisions Goal 14 must settle; they are not
per-type rows.

| # | Policy | Decision | Rationale |
| --- | --- | --- | --- |
| V1 | `Async` / `ValueAsync` suffix meaning | redesign (vocabulary) | Pick one rule and apply it consistently; today `MapAsync`/`BindAsync`/`TapAsync` mean Task callbacks while `TraverseAsync`/`SequenceAsync` mean async sources, and `ComposeAsync` covers both kinds (C3, C5; §2.2). |
| V2 | LINQ alias policy | redesign (consistency) | Keep `Select`/`SelectMany` on `Result` and `Effect` (idiomatic, compiler-checked), add the mechanical aliases on `Option<T>`, and record the deliberate absence on `Validation` (no `Bind`); do not add `Where` anywhere without an error-producing predicate (C5; §2.10). |
| V3 | Lazy-fallback suffix | redesign (naming) | Choose one factory suffix (`…OrElse` vs `…OrElseWith`) across value and option fallbacks; `GetValueOrElse` and `OrElseWith` currently disagree (C5; §2.2). |
| V4 | Generic parameter order and type-name suffix | keep | `StateMachine<TState,TEvent,TOutput,TError>` vs `TransitionResult<TState,TOutput,TError>` differ intentionally; document rather than unify (C3; §5.1 note). |

### 5.4 Remove decisions

No current type or member family is recommended for outright removal. Removal is reserved for
shapes superseded by the redesigns above (the old `FirstSuccessAsync` return shape, the old `Then`
algorithm) and for any capability that the maintainer rejects in §6. Recording this explicitly
satisfies the "no silent omission" requirement; a decision matrix with justified keeps is
intentional, not an oversight.

## 6. Goal-derived gap list (Goals 15–23)

These are requirements stated by later goals, **not present APIs**. Each item is marked with the
source goal and the recommended decision only where Goal 14 must decide product scope. All
recommendations still require maintainer acceptance.

| # | Goal-derived requirement | Missing today | Evidence | Recommended decision |
| --- | --- | --- | --- | --- |
| G1 | `UnitResult<TError>` as the canonical success-or-failure-without-value carrier (`docs/goals/0015-goal.md:2`) | Entire type; also UnitResult traversal and HTTP mapping | No `UnitResult` anywhere in `src/`, `tests/`, `examples/` (grep, no matches); `Result<TValue,TError>` forces a dummy `TValue` today | **adopt** (small struct mirroring `Result` case/factory/inspection but no `Map` value; add F3/F5/F10 integration only after the core shape is accepted) |
| G2 | Default/uninitialized `Result`/`Validation` must not masquerade as domain outcomes (`docs/goals/0015-goal.md:2`) | Distinguishable default state | §2.1; `tests/FunnySharp.Tests/ResultTests.cs:8-19`, `ValidationTests.cs:8-19` | **redesign** (one decision point: explicit initialized flag/status, or default accessors throw, or keep + mandatory analyzer diagnostic; §5 rows 21 and 33) |
| G3 | No second absence carrier (`Maybe<T>`) and no `Validation.Bind` (`docs/goals/0015-goal.md:2`) | Nothing — already satisfied | Dump type list; `docs/validation.md:30-33` | **reject** (record in the constitution as an explicit non-goal) |
| G4 | Fallible-function composition and folding/scan in one grammar (`docs/goals/0016-goal.md:2`) | No Result/Option-aware `Compose`; no `Fold`/`Scan` | `FunctionExtensions` has no fallible composition; no fold/scan in any dump section; BCL `Aggregate` covers fold, not scan | **adopt** a narrow `Scan` (running aggregate) only; **adapt** fallible composition by documenting `Bind` as the canonical form instead of adding a pipeline hierarchy |
| G5 | Cardinality-sensitive and partition operations, non-empty guarantees (`docs/goals/0017-goal.md:2`) | No `Partition`, `Single`/`ExactlyOne`, `NonEmpty`, exact-vs-truncating zip | `SequenceExtensions` and pipeline types only; no such members in dumps | **adopt** `Partition`; **defer** a non-empty carrier and exact-vs-truncating zip until a concrete consumer case (avoid a collection universe) |
| G6 | Traversal location/path context (`customers[17].addresses[2].postalCode`, `docs/goals/0017-goal.md:2`) | No index/key/path on traversal failures | `docs/validation.md:43-66`; `SequenceExtensions.cs`, `AsyncSequenceExtensions.cs` produce only carrier payloads | **adopt** a minimal error-context shape for `Traverse`/`TraverseValueAsync` (index/key plus optional path segments), designed together with `UnitResult` so the API is not multiplied later |
| G7 | Completion-order concurrency results and no unbounded fan-out ambiguity (`docs/goals/0018-goal.md:2`) | `SelectParallelValueAsync` is ordered-only; `FirstSuccessAsync` starts every candidate | `docs/concurrency.md:10-53,92-138` | **defer** a completion-order operator (BCL `Task.WhenEach` exists for raw tasks); keep `FirstSuccessAsync` fan-out but document it and consider a bounded overload only if a consumer needs it |
| G8 | Retry/timeout policy in the effects family (F7 taxonomy; not explicitly required by Goals 15–23) | Only the `FirstSuccessAsync` timeout exists | `docs/effects.md:94-102` excludes retries; `grep TimeProvider` → only `ConcurrentEffectExtensions` | **reject** a general retry/policy layer (would recreate the runtime/DI universe the contract forbids); keep timeout only where a coordinator owns it |
| G9 | Analyzers, code fixes, and diagnostic docs (`docs/goals/0021-goal.md:2`) | No analyzer project, package, or diagnostics | No `Experimental`/analyzer/PublicAPI artifacts in the repo; `docs/product-contract.md §"Analyzers And Compiler Feedback"` defers analyzers | **adopt** a separate analyzer package (no core runtime dependency) targeting at least: discarded `Result`/`Validation`, default/uninitialized carriers (G2), `Option.Some(null)`-adjacent patterns, and misplaced cancellation |
| G10 | Unmistakable experimental tier + stable/experimental inventory (`docs/goals/0019-goal.md:2`, `docs/goals/0023-goal.md:2`) | No marker mechanism, no inventory | §2.14; local probe: `[Experimental("FSX0001")]` produces a suppressible compile error on net10 | **adopt** `ExperimentalAttribute` plus a tracked stability inventory; apply to new G1–G6 capabilities until they have evidence; keep the current 33 types stable after the §5 redesigns |
| G11 | Package-consuming ASP.NET Core vertical slice (`docs/goals/0022-goal.md:2`) | Examples use `ProjectReference`; compatibility consumers are smokes | `examples/FunnySharp.AspNetCore.Examples/FunnySharp.AspNetCore.Examples.csproj` (ProjectReference); `tests/FunnySharp.Compatibility/*/Program.cs` | **adopt** a new package-only vertical-slice sample with persistence/I/O, request cancellation, bounded parallel work, streaming, state decisions, and idiomatic-C# comparison; reuse the compatibility harness for package consumption |
| G12 | OpenAPI/typed-results fidelity (`docs/goals/0022-goal.md:2`) | Mapping returns `IResult`; no OpenAPI metadata or typed results | No OpenAPI references in tests or examples (grep) | **defer** a first-party typed-results API; **adapt** by documenting `.Produces`/`.ProducesProblem` usage in the G11 slice and testing OpenAPI output there |
| G13 | Fixed-hardware performance runner and competitor comparisons (`docs/goals/0020-goal.md:2`) | Hosted timing is directional; no Funcky/CFE/language-ext benchmark rows | `TODO.md:3-9`; `benchmarks/FunnySharp.Benchmarks/*` benchmark BCL/direct code only; `docs/release-evidence/goal-12.md:107-112` | **adopt** the fixed runner (infrastructure, not API); **defer** competitor timing rows — Goal 20 requires them but the hard independence constraint forbids competitor dependencies, so a benchmark-only reference needs an explicit maintainer exception that the lead must resolve; allocation and complexity budgets can proceed now |
| G14 | API-compatibility baseline, versioning, release notes, AOT claim (`docs/goals/0023-goal.md:2`) | No committed API baseline or CHANGELOG; `IsAotCompatible` intentionally unclaimed | `grep` for `PublicAPI`/`CHANGELOG` → none; `docs/product-contract.md §"Trimming And Native AOT Policy"` | **adopt** a committed public-API baseline + release-notes file once the §5 redesigns land; **defer** the AOT claim until the toolchain can analyze the full open-generic surface |
| G15 | Trim/AOT/package re-verification at the next candidate | Historical evidence only | `docs/release-evidence/goal-12.md:5,76-104`; §2.16 | **adopt** as release-gate work (not an API decision) |
| G16 | `net11.0` targeting (`TODO.md:13-15`) | Not targeted | `TODO.md:13-15`; `Directory.Build.props:4` | **defer** until .NET 11 GA; consistent with `docs/product-contract.md §"Deliberate Deferrals"` |
| G17 | Uniform null policy for value-accepting factories (`docs/goals/0023-goal.md:2`) | `Effect.FromValue` accepts null with no stated contract; `Result.Success(null)` preserves the case while `Option.Some(null)` throws | §2.12; `src/FunnySharp/Effect.cs:16-17`; `tests/FunnySharp.Tests/ResultTests.cs:21-31`; `tests/FunnySharp.Tests/OptionInteropTests.cs:45-48` | **adapt** (document the per-carrier policy explicitly; no shape change unless the maintainer prefers a runtime rejection in `FromValue`) |

## 7. Not examined

- External baselines (BCL/FSharp.Core/Funcky/CSharpFunctionalExtensions/language-ext) and the
  side-by-side `docs/next-stage/call-sites.md` comparisons — separate Goal 14 workstreams. This
  memo makes no competitor API or naming claims except where a pinned BCL ref-pack file was read
  locally (`System.Linq.AsyncEnumerable.xml`, ref pack 10.0.11, used only to confirm that no BCL
  bounded-parallel select exists and that `Select` already covers Task/ValueTask selectors).
- Full `eng/performance/baseline.json` review (3,994 lines); only the rows relevant to the
  investigations were read.
- No benchmark execution, no trim/Native AOT publish, no package-consumer execution, no Windows or
  macOS run, no .NET 11 preview, no OpenAPI runtime behavior, no Kestrel end-to-end run
  (the ASP.NET Core tests were executed only as the prebuilt test binary).
- The generated dumps are produced by a separate Goal 14 workstream and were refreshed during this
  analysis; the current committed copies are byte-identical to the current
  `/tmp/opencode/evidence/inv-funny-sharp-*.{md,json}` files (verified by `cmp`), and every dump
  line citation in this memo was re-checked against the refreshed copies.
- Nullability attribute metadata was not inspected at the IL level; conclusions use
  NullabilityInfoContext output (the dumps), source annotations, and two compiler probes.
- No usage telemetry or maintainer interviews; qualitative judgments (LINQ policy, first-success
  return shape, how much Effect overhead is justified, whether optics earn their place) are flagged
  **UNVERIFIED** where they appear.
- `docs/goals/0024-goal.md` (the later audit goal) is acknowledged but out of scope for this
  workstream.

## 8. Provenance pins

| Item | Pin |
| --- | --- |
| Source | commit `4dbebd94b7b58648632112b7ca47c39cc517f153`, version `0.1.0` |
| SDK / runtime | 10.0.400 / 10.0.11 (Linux x64, local verification 2026-09-17) |
| Core dump | `docs/next-stage/inventory/generated/inv-funny-sharp-core.{md,json}` SHA256 `8660a1f…` / `f5324bc…` (33 types, 254 members) |
| AspNetCore dump | `docs/next-stage/inventory/generated/inv-funny-sharp-aspnetcore.{md,json}` SHA256 `a33cde5…` / `600adb6…` |
| XML docs | `src/FunnySharp/bin/Release/net10.0/FunnySharp.xml` (275 members), `src/FunnySharp.AspNetCore/bin/Release/net10.0/FunnySharp.AspNetCore.xml` (16 members) |
| Test run | `FunnySharp.Tests` 305 passed; `FunnySharp.AspNetCore.Tests` 18 passed |
| Compiler probes | `/tmp/opencode/optprobe` (CS0452/CS0411/CS8625), `/tmp/opencode/expprobe` (EXP diagnostic `FSX0001`) |
