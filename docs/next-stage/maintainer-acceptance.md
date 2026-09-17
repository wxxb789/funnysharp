# Maintainer Acceptance Record (Goal 14)

Goal 14 requires the canonical vocabulary and the stability boundary to be accepted by the
repository maintainer. This file records the exact acceptance items, the options, the lead's
recommendation, and the final outcome. Until every outcome cell says "accepted" or
"accepted with change", the Goal 14 result is evidence-complete but not accepted.

Status: **accepted 2026-09-17** — all eight items decided by the repository maintainer.

How to accept: reply in the session with `A-1..A-8 accepted` (all recommendations), or list only
the items that differ, for example `A-2=B, A-7=C`. The lead writes the outcome into each section
below, applies any accepted change to the constitution, decision record, and CSV, and asks the
independent auditor to re-check the delta.

## A-1 Default state of `Result`, `UnitResult`, `Validation` (Goal 15 mechanism)

- Context: `default(Result<TValue,TError>)` today equals `Failure(default(TError))`, and
  `default(Validation<TValue,TError>)` equals `Invalid([default(TError)])`; Goal 15 requires that
  defaults not masquerade as domain outcomes.
- Option A (recommended): keep two public cases; detect uninitialized values; access or
  composition on an uninitialized value throws `InvalidOperationException`; Goal 21 analyzer adds
  static checks. Precedent: `Effect` and optics already throw on uninitialized use.
- Option B: add an explicit public uninitialized state (`IsInitialized`/status); larger layout and
  `Match`/equality changes.
- Option C: keep runtime behavior and rely on analyzer diagnostics only; weakest against the
  Goal 15 wording.
- Outcome: **accepted** — option A (uninitialized access throws; analyzer adds static checks).

## A-2 `FirstSuccessAsync` return shape

- Context: returning `Validation<TValue,TError>` for a race conflates "independent checks that
  all run" with "alternatives that all failed".
- Option A (recommended): return `Result<TValue, IReadOnlyList<TError>>`; drain/cleanup semantics
  unchanged; Goal 18 finalizes the exact shape.
- Option B: keep `Validation`.
- Option C: keep the shape, rename to `TryFirstSuccessAsync`.
- Outcome: **accepted with change — option B** ("keep it simple; `TError` can be nested"). The
  `Validation` return shape stays; the race contract (winner is `Valid`, all typed failures are
  `Invalid` in input order) must be documented explicitly at the method and in the concurrency
  guide. Callers needing a different aggregation can nest `TError` in their own domain type. The
  `analysis/funny-sharp-surface.md` §2.3 redesign recommendation is superseded.

## A-3 `Option` LINQ aliases

- Context: `Result`/`Effect` expose `Select`/`SelectMany`; `Option` does not (documented as
  deliberate).
- Option A (recommended): add `Select`/`SelectMany` to `Option` (never `Where`) for grammar
  consistency and AI predictability; LINQ remains a secondary bridge.
- Option B: keep the deliberate exception and document it.
- Outcome: **accepted** — option A (add `Select`/`SelectMany` to `Option`; never `Where`).

## A-4 Public `Unit` type

- Context: Funcky and language-ext use `Unit` for value-less generics; FunnySharp has no `Unit`
  and plans `UnitResult<TError>`.
- Option A (recommended): do not introduce `Unit`; no-value outcomes use `UnitResult<TError>`;
  no-value work uses `void`/`Task`/`ValueTask`.
- Option B: introduce a minimal `Unit` struct for generic carriers.
- Outcome: **accepted** — option A (no public `Unit`), recorded with the requested comparison of
  the Funcky and language-ext designs:
  - Funcky's `Unit` is a readonly struct with a single `Value`; it exists so generic carriers can
    hold a payload for value-less work (`Option<Unit>`, `Result<Unit>`) and so `ForEach`/`Apply`
    can be expressed generically — which is why Funcky also carries large `Apply`-over-`Unit`
    overload towers.
  - language-ext's `Unit` is pervasive in `Eff`/`Aff` and fold signatures because every effect is
    generic and everything participates in the HKT/typeclass hierarchy.
  - FSharp.Core's `unit` is a language value; an F# function returning nothing is not part of
    FunnySharp's C# surface, and the F# baseline memo rejects importing it (void/Task/ValueTask
    are the C# forms).
  - FunnySharp's adopted capability set does not need a no-payload generic value: no-value
    outcomes are `UnitResult<TError>`; no-value deferred work is expressible as
    `Effect<UnitResult<TError>>` or a domain payload; no-value branches use `Action`/`Match`
    overloads. Adding `Unit` now would exist mainly to mirror foreign signatures and would
    justify the overload towers this record rejects.
  - Revisit trigger: if, after `UnitResult<TError>` lands, a concrete consumer or a Goal 15–23
    API cannot express no-value generic work without a dummy value, a minimal `Unit` (single
    `Value`, no overload towers) can be reconsidered by that goal with call-site evidence.

## A-5 Stability boundary

- Proposed (recommended): every kept 0.1.0 type/member after the recorded redesigns is stable;
  new uncertain capabilities (for example Goal 17's traversal-location API) ship experimental
  using `[Experimental("FS####")]` plus a tracked stability inventory until their goals produce
  full evidence; nothing from 0.1.0 is removed; a committed API baseline plus
  `EnablePackageValidation` enforces the boundary; release notes and versioning rules accompany
  the next release.
- Option B: mark `Effect`, optics, and state as experimental for one release to gather more
  consumer evidence.
- Option C: no experimental tier; everything documented is stable.
- Outcome: **accepted** — option A (current stability boundary and enforcement plan).

## A-6 Serialization converters

- Context: CFE and Funcky ship opinionated converters; Goal 14 record defers a converter until the
  wire contract is fixed and trim/AOT behavior is proven.
- Option A (recommended): defer with recorded triggers; callers use DTOs and
  `JsonSerializerOptions.Strict` where a DTO-only policy is desired.
- Option B: ship opt-in converters this stage (explicit registration, no global state; accept the
  `RequiresDynamicCode` boundary or use closed generics).
- Outcome: **accepted** — option A (defer converters with recorded triggers).

## A-7 Retry policy layer

- Context: Funcky ships policy-as-value retries; language-ext ships a `Schedule` DSL; the Funcky
  memo recommends a narrowed adapt.
- Option A (recommended): defer; a future goal must specify `TimeProvider`, explicit cancellation,
  cap/jitter policy, and measured comparison against a hand-written loop.
- Option B: reject outright (retries belong to the application or the resilience ecosystem).
- Option C: adapt a minimal stable policy API in the next stage (Goal 19 delivery burden).
- Outcome: **accepted** — option A (defer a retry layer with recorded triggers).

## A-8 Cardinality naming rule

- Context: Goal 17 adopts cardinality-safe operations and container bridges.
- Option A (recommended): `*OrNone` for sequence cardinality (`FirstOrNone`, `SingleOrNone`,
  `ElementAtOrNone`, `MinOrNone`, `MaxOrNone`); keep `GetOption` for keyed lookups and `ToOption`
  for value conversion.
- Option B: one uniform absence suffix, renaming existing members.
- Outcome: **accepted** — option A (`*OrNone` for sequence cardinality; `GetOption` for keyed
  lookups; `ToOption` for value conversion).

## Acceptance consequence

- If all recommendations are accepted, the canonical vocabulary and stability boundary in
  `../product-contract.md` and `api-decisions.md` are the accepted next-stage constitution and
  Goal 14 can close after the round-2 independent audit passes.
- Any accepted change is applied to `../product-contract.md`, `api-decisions.md`,
  `decision-record.md`, and `decision-matrix.csv`, and the independent audit re-checks the delta.
