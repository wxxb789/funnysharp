# Maintainer Acceptance Record (Goal 16)

Goal 16 requires, among its completion evidence, that "maintainers judge it [FunnySharp code]
at least as explicit and readable as idiomatic C#" and that the grammar's judgment calls are
decided by the repository maintainer. This file records the Goal 16 acceptance items, the
options, the lead's recommendation, and the final outcome. Until every outcome cell says
"accepted" or "accepted with change", the Goal 16 result is evidence-complete but not accepted.

Status: **awaiting maintainer response**.

How to accept: reply in the session with `G16-1..G16-8 accepted` (all recommendations), or list
only the items that differ, for example `G16-2=B`. The lead writes the outcome into each section
below, reverts any rejected change, and re-runs the verification gates on the delta.

## G16-1 `Scan` semantics (decision E73: adopt narrow)

- Context: E73 adopts a narrow running-aggregate operator ("one operator, no pipeline
  hierarchy"). The exact enumeration semantics were not pinned by the decision.
- Option A (recommended, implemented): Rx-style running aggregate — `Scan(seed, accumulate)`
  yields the accumulator **after** each element; the seed itself is never yielded; an empty
  source yields nothing; for a non-empty source the last yielded value equals
  `source.Aggregate(seed, accumulate)`. Async family mirrors `Choose` exactly: bare `Scan` over
  `IAsyncEnumerable<T>` with a synchronous accumulator, `ScanValueAsync` (± token) with a
  `ValueTask` accumulator. BCL `Aggregate`/`AggregateAsync` remains the canonical fold and is
  never duplicated.
- Option B: F#-style seed-inclusive scan (yields the seed first; empty source yields the seed).
- Option C: do not adopt `Scan` this stage; defer to Goal 17.
- Outcome: **accepted** (Option A, 2026-09-19).

## G16-2 `Zip` combine arity 4 (decision AD-3: bounded applicative helper, arity ≤ 4)

- Context: AD-3 authorizes "a bounded applicative helper (arity ≤ 4, explicit argument order)
  only if it demonstrably reduces semantic LOC at form-scale call sites and keeps error order
  visible". Goal 15 delivered arity 2 and 3; Goal 16 completes the authorized bound with arity 4
  on `Option<T>`, `Result<TValue, TError>`, and `Validation<TValue, TError>` (all three carriers
  keep the identical shape so `Zip` keeps one predictable output shape).
- Option A (recommended, implemented): keep arity 2/3/4. A four-field form (the modal
  registration form) composes in one expression; error order stays the argument order;
  unbounded tuple towers remain rejected.
- Option B: revert to arity 2/3 (remove the arity-4 overloads; wider forms nest or use
  `Traverse`).
- Outcome: **accepted** (Option A, 2026-09-19).

## G16-3 `UnitResult<TError>.Map` renamed `ToResult` (grammar completion)

- Context: `Map` on every value-bearing carrier means "transform the contained value through
  `Func<T, TResult>`" and keeps the carrier shape. `UnitResult<TError>.Map<TValue>(Func<TValue>)`
  changed the carrier type (`UnitResult<TError>` → `Result<TValue, TError>`) and took no input
  value, so a reader could not predict its output shape from `Map`'s meaning — a direct
  violation of the Goal 16 completion criterion.
- Option A (recommended, implemented): rename to the conversion family —
  `UnitResult<TError>.ToResult<TValue>(Func<TValue> valueFactory)`,
  `ToResultAsync` (± token), `ToResultValueAsync` (± token). `To*` verbs are exactly the
  carrier-changing family (`Option.ToResult`, `Option.ToUnitResult`, `Result.ToUnitResult`,
  `Option.ToNullable`, `ToHttpResult`), each supplying what the target carrier needs;
  `Map` now keeps one shape everywhere it exists.
- Option B: keep the name `Map` and document it as the one exception.
- Outcome: **accepted** (Option A, 2026-09-19).

## G16-4 `ComposeValueAsync` rename executed (already accepted)

- Context: AD-4 and the accepted product contract rename the two `ValueTask` `ComposeAsync`
  overloads to `ComposeValueAsync` ("the renamed `ComposeValueAsync` removes the only
  exception"). This is informational: the change implements an already-accepted decision.
  Implemented: the Task overloads keep `ComposeAsync`, the `ValueTask` overloads are
  `ComposeValueAsync`, and every carrier-facing call site, test, benchmark, and doc uses the
  new names.
- Outcome: **acknowledged** (2026-09-19, no objection).

- Context: maintainer decision A-3 (2026-09-17) accepted adding `Select`/`SelectMany` to
  `Option<T>` and never `Where`. This is informational: Goal 16 implements it, and
  `docs/option.md` now documents the aliases as secondary to the canonical `Map`/`Bind`
  vocabulary, matching `Result` and `Effect`.
- Outcome: **acknowledged** (2026-09-19, no objection).

## G16-6 Documented deliberate absences (no surface added)

- Context: the grammar table records where a verb's meaning would be valid but the member is
  deliberately absent, with the reason:
  - `Option<T>.ZipWith` — valid (lazy second), deferred until a call-site evidence trigger;
  - `Validation<TValue, TError>.ZipWith` — rejected: a lazy second would skip an independent
    check and hide errors from accumulation;
  - `TraverseParallelValueAsync` over `UnitResult<TError>` — deferred to the async goal
    (Goal 18) absent a consumer;
  - no `PipeAsync` and no task-carrier operator universe — mixed sync/async chains stay
    ordinary `await` plus the synchronous vocabulary (product contract);
  - arity beyond 2 for `Curry`/`Partial`/`Flip`, beyond 4 for `Zip` combine, `Where` on any
    carrier, `Apply` beyond `Validation`, and LINQ aliases on `Validation` all stay rejected.
- Option A (recommended): record the absences as listed.
- Option B: add any of the deferred members now.
- Outcome: **accepted** (Option A, 2026-09-19).

## G16-7 The authoritative grammar table

- Context: Goal 16 requires "an authoritative grammar table". `docs/grammar.md` is that table:
  every stable verb with one primary meaning, the carriers where it exists, its output shape,
  its async forms, and its evaluation/short-circuit/exception/cancellation/enumeration/
  materialization contract; the accepted naming rules; the "fallible composition is `Bind`"
  contract (E74); the fold/scan boundary (BCL `Aggregate` is the fold; `Scan` is the narrow
  running aggregate); deliberate absences; and the forbidden-mechanisms list (no reflection,
  dynamic dispatch, hidden scheduling, HKT simulation, operator tricks, or implicit
  conversions on normal paths).
- Option A (recommended): accept `docs/grammar.md` as the authoritative verb reference,
  alongside the per-feature guides that carry the detailed contracts.
- Option B: request changes to specific rows (list them).
- Outcome: **accepted** (Option A, 2026-09-19).

## G16-8 Readability judgment (goal requirement)

- Context: the goal text requires the side-by-side evidence to show FunnySharp code is shorter
  **while maintainers judge it at least as explicit and readable as idiomatic C#**. The
  Goal 16 workflow comparisons are in
  [`call-sites-goal-16.md`](call-sites-goal-16.md) (function-grammar workflows: multi-stage
  pipelines with observation, reusable composed transforms, fallible composition via `Bind`,
  running aggregates via `Scan`, async composed stages, four-field form validation via
  arity-4 `Zip`, and the Option LINQ bridge as secondary), each with the semantic S+O counts,
  raw LOC, compile evidence, and honest losses.
- Option A (recommended): judge the FunnySharp variants at least as explicit and readable as
  the idiomatic baselines (where the doc itself reports an idiomatic win, the honest judgment
  is recorded there, not here).
- Option B: judge specific workflows below the bar (list them; the lead records the finding
  and the follow-up).
- Outcome: **accepted** (Option A, 2026-09-19: the FunnySharp variants are judged at least as
  explicit and readable as the idiomatic baselines).

## G16-9 Compile-time ambiguity and inference tests (lead-added, evidence completion)

- Context: the goal lists "compile-time ambiguity and inference tests" among its evidence. The
  behavior suite already exercised the surface implicitly (it compiles against inference-heavy
  shapes), but no file pinned the compile-time contracts by name. The lead added
  `tests/FunnySharp.Tests/GrammarInferenceTests.cs`: eight tests pinning that every call shape
  resolves to exactly one overload and infers every type argument — `Zip` arity 2 (tuple and
  combiner) through arity 4 on `Option`/`Result`/`Validation` with heterogeneous operand types,
  the `Compose` family resolving by callback return kind (including token-aware
  `ComposeAsync`/`ComposeValueAsync`), `Curry`/`Uncurry`/`Partial`/`Flip` argument-position
  inference, the `ToResult` family (including the recorded WF-3 explicit-type-argument friction
  shape), and `Scan`/`ScanValueAsync` accumulator inference (including the token-aware
  overload). All eight pass.
- Option A (recommended, implemented): accept the file as the goal's compile-time evidence.
- Option B: adjust the coverage (list the gaps).
- Outcome: **accepted** (Option A, 2026-09-19).

## G16-10 Benchmark observation environment and budget calibration (lead-added)

- Context: the acceptance consequence requires benchmarks plus approved baseline observations
  and regenerated performance tables. The previous observation was Windows 10.0.12 evidence
  (Goal 15); the Goal 16 policy (v5) added `Scan` rows and the `ComposeValueAsync` rename, so a
  fresh full-suite run was required. The available host is Linux x64 (1 physical core, 2 logical
  cores; runtime 10.0.11). The maintainer approved applying a Linux-based observation
  (allocation budgets are the blocking contract and are environment-checked; hosted timing is
  directional, and the release gate re-runs the complete suite and replaces the observation on
  its own environment).
- The first full-suite run (138 benchmarks) passed 134/138 allocation budgets. Four
  parallelism-degree-dependent rows exceeded budgets derived from Windows candidate evidence
  (BCL baseline rows also drifted ±10-13%, confirming an environment shift, not a code
  regression — none of the four rows' source changed in Goal 16):
  `FunnySharpSelectParallelValueAsync[Count=1024]` 458,366 B vs 436,440 B (+5.0%),
  `FunnySharpFirstSuccessAsync[CandidateCount=4]` 2,521 B vs 1,816 B (+38.8%),
  `FunnySharpParallelOptionTraversal[Count=1024]` 287,032 B vs 218,264 B (+31.5%),
  `FunnySharpParallelValidationTraversal[Count=1024]` 289,573 B vs 267,912 B (+8.1%).
- Option A (recommended, approved): recalibrate those four budgets with the manifest's own
  formula (evidence +25% + 32 B, rounded up to 8 B) from the Linux measurements —
  572,992 / 3,184 / 358,824 / 362,000 B — with the environment difference recorded in each
  row's `budgetRationale`, then re-run the complete suite under the calibrated policy (the
  policy fingerprint changes, so fresh receipts are required).
- Option B: keep the prior budgets (the Linux observation cannot be applied; the goal stays
  blocked pending a Windows environment).
- Outcome: **accepted** (Option A, 2026-09-19): budgets calibrated, full suite re-run, receipts
  verified, observation applied, and performance tables regenerated.

## Acceptance consequence

- All recommendations were accepted (2026-09-19), so no change was reverted. The verification
  gates were run on the final tree: locked restore, Release build, xUnit tests, both examples,
  formatter check, and documentation-snippet parity via `eng/tools/verify_local.py`; benchmark
  semantic preflight; the complete BenchmarkDotNet suite (138 benchmarks) on Linux x64;
  allocation-policy verification with an approved observation applied to
  `eng/performance/baseline.json` (policy revision `2026-09-19-v5`); regenerated performance
  tables; and the regenerated public API inventory (36 core types / 324 members, +9 members and
  7 renames over Goal 15's 315; `FunnySharp.AspNetCore` unchanged at 1 type / 20 members).
- Any accepted change is applied by the lead, the grammar table and affected docs are updated,
  and the verification gates are re-run on the delta before the goal closes.
