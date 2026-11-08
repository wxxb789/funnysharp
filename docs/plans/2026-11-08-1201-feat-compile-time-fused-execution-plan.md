---
title: Compile-time fused execution feasibility
type: feat
date: 2026-11-08
artifact_contract: ce-unified-plan/v1
product_contract_source: docs/goals/archive/0039-goal.md, docs/product-contract.md, docs/grammar.md, docs/core-design.md
execution: code
---

# Compile-Time Fused Execution Plan

## Goal Capsule

- **Objective:** Decide whether a bounded, explicit subset of FunnySharp's composable
  calculations can be translated by a Roslyn source generator into specialized C# execution that
  preserves the calculation's promised semantics and how that changes the consumer-expression
  versus runtime-cost trade-off. Completion is a maintainer-ready ADOPT / ADAPT / DEFER / REJECT
  decision backed by an executable prototype and honest measurements.
- **Means:** One non-shipping Roslyn incremental generator in `tests/` recognizes two existing,
  statically analyzable FunnySharp descriptions: a `Summary<...>` built only from `Summary.Zip`
  and the built-in `Count`/`Sum`/`Min`/`Max` factories with static expression selectors, and a
  `Decoder<T>` built only from `Decoder.Object`, required/optional `Field`, scalar leaves,
  `List`/`NonEmptyList` of scalar leaves, and object-creation combine lambdas. Two opt-in
  attributes on partial methods request generated execution; unsupported shapes produce an
  error diagnostic and no silent fallback. The generated summary evaluates components inline in
  operand order; the generated decoder is a success fast path and calls the declared runtime
  decoder for every invalid input, preserving the exact `DecodeError` result without generating
  error paths.
- **Authority:** [Goal 0039](../goals/archive/0039-goal.md) is the completion contract.
  [Product contract](../product-contract.md), [grammar](../grammar.md), and
  [core design](../core-design.md) are current boundaries. Goal 0034 supplies the summary
  calculation and Goal 0026 the decoder calculation. Goals 0026/0027/0034 are complete; this plan
  starts no Goal 0040 work.
- **Execution profile:** Prototype generator in
  `tests/FunnySharp.FusedExecution.Generator/`; functional and diagnostic tests in
  `tests/FunnySharp.FusedExecution.Tests/`; shared generated/delegated/hand-written consumers in
  `tests/Shared/FusedExecutionComparison/`; measurements in
  `tests/FunnySharp.FusedExecutionMeasurements/`. Document the decision in
  `docs/compile-time-fused-execution-evidence.md`, move the goal unchanged to archive, and update
  the goals index. No shipping project, package, API baseline, grammar, product contract, or
  stability inventory changes if the decision is negative.
- **Tail ownership:** The maintainer judges whether the generated path's measured savings justify
  a new opt-in description and build-time cost. A negative finding is a complete outcome; the
  evidence does not redefine the baseline to manufacture a win.
- **Stop for a new decision if:** the only defensible prototype requires a public DSL, rewriting
  arbitrary call sites, or weakening error/exception/callback semantics; or the supported subset
  shrinks so far that it no longer represents a real 0026/0034 calculation.

## Product Contract

### Chosen representative calculations and why

- **Summary (Goal 0034) is the primary representative.** `Summary.Zip` over `Count`, `Sum`, `Min`,
  and `Max` is a complete combined folding workload with value results and no error-path
  construction. Its promised semantics (one source enumeration, source order, checked arithmetic,
  empty extrema, null-key skipping, first-key tie retention, component order, exception
  propagation, disposal) are fully observable and can be reproduced by a generated loop without
  inventing a second error model.
- **Decoder (Goal 0026) is the secondary representative validation calculation.** A flat order
  decoder over `id`, `customer`, `total`, and a scalar `tags` list exercises required fields,
  non-empty strings, numeric parsing, array cardinality, and independent accumulated errors. Its
  generated success path is a real fused parse; its invalid path delegates to the declared
  decoder, so the exact `DecodeError` locations and ordering remain the runtime decoder's.
- **Why not Contract (0027):** generated `Contract<T>.Validate` errors would require constructing
  and prefixing `DecodeError` locations, which is internal to the core. A fast valid-path plus
  runtime fallback would either call contract getters twice on failure or require a new public
  error-prefixing API. Both are worse research artifacts than the decoder's immutable `JsonElement`
  fallback. This is recorded as a non-adoption, not as evidence that contract fusion is good.

### Prototype public shape (research-only, not shipping)

The generator injects attribute definitions into the consuming compilation and reads two
opt-in declarations:

```csharp
[FusedSummary(nameof(OrderStatistics))]
public static partial (int Count, decimal Total, Option<decimal> Min, Option<decimal> Max)
    Evaluate(IEnumerable<OrderLine> source);

[FusedDecoding(nameof(OrderDecoder))]
public static partial Validation<OrderInput, DecodeError> Decode(JsonElement input);
```

- The attribute argument names an existing `static readonly` field in the same type.
- The summary field must be a single `Summary.Zip` of two to four components, each a built-in
  `Summary<TSource>.Count()`, `Sum(selector)`, `Min(selector)`, or `Max(selector)`. Selectors must
  be `static` expression lambdas with one parameter. The body is bound in a static local function
  with the declared parameter and return types, so target typing, the project's checked/unchecked
  context, and lexical scope are preserved; only the count/sum accumulation addition is `checked`.
  The function is invoked once per reached item in component order. Method groups, statement
  bodies, captured variables, nested `Zip`, `MapResult`, and `Summary.Create` are unsupported.
- The decoder field must be a `Decoder.Object` of two to four field arguments. Each argument is a
  `Decoder.Field`/`OptionalField`/`NullableField` with a constant name over a built-in leaf or a
  `Decoder.List`/`NonEmptyList` of a built-in leaf. The combine lambda's parameter types must
  exactly match each field decoder's result type. Combine lambdas must be `static` expression
  lambdas whose body is an object-creation expression (`new T(...)`) using the lambda parameters. Custom `Decoder.From`, `Map`, `Bind`, `Ensure`, `Validate`, nested `Decoder.Object`
  fields, lists of objects, and non-construction combines are unsupported.
- Every unsupported construct emits an error diagnostic and the build fails; no silent fallback
  with different guarantees occurs. Recognizer rejections (`FS3901`/`FS3902`/`FS3904`) also emit a
  throwing body; method-shape rejections (`FS3903`) emit no implementation, because a second
  method declaration would not compile.

### Semantics preservation

- **Summary:** the generated method throws `ArgumentNullException(nameof(source))` before
  enumeration, uses one `foreach`, accumulates components in operand order per item, initializes
  sum with `default(TNumber)`, uses checked count/sum arithmetic, skips null extrema keys before
  the comparer, retains the first tied key, and returns the same tuple shape. Selector expressions are evaluated exactly once per reached item per component, in
  the same order as the delegated summary. Delegate objects disappear; their observable
  evaluation count and order do not.
- **Decoder:** the generated method returns the declared decoder's exact result for every input
  it cannot prove valid before construction. On the fast path it checks object/array kinds,
  finds the first duplicate property in document order (matching `Decoder.Field`), parses the
  same leaf kinds with the same BCL APIs, applies the optional/nullable absence policies where
  supported, and only then evaluates the object-creation expression once. No user delegate is
  invoked in field decoders, and the combine lambda is inlined as a static local function, so
  fallback cannot duplicate a combine callback. List fields are exposed with the runtime shape
  (`Array.Empty<T>()` for empty arrays, `AsReadOnly()` otherwise) and typed as
  `IReadOnlyList<T>`. Construction exceptions propagate from the single construction site with
  normal .NET semantics. Nested object construction is deliberately diagnosed: a fallback-based fast path
  cannot preserve a nested combine's invocation count when a later sibling fails.
- **Span/async:** `IAsyncEnumerable` and span/memory source forms are unsupported and diagnosed.
  The prototype makes no lifetime claim beyond the synchronous `IEnumerable`/`JsonElement` forms.

### Comparison and packaging

- **Generated vs delegated:** the same summary/decoder field evaluated through the generated
  partial method and through the runtime `Summary.Evaluate`/`Decoder.Decode`.
- **Fair hand-written loop:** a direct `foreach` summary and a direct `JsonElement` valid-input
  decoder, charged the same models, fixtures, and report construction.
- **Current LINQ:** separate `Count`/`Sum`/`Min`/`Max` and a one-pass `Aggregate` over a tuple.
  Hyperlinq is assessed as not a fair combined-summary competitor; it specializes individual LINQ
  operators and has no shared one-pass multi-summary protocol, so no package is added.
- **Costs disclosed:** generated code lines/bytes, generator execution time, warm build-time
  direction, diagnostics, debugging visibility through emitted files, allocation per operation,
  copying, and directional timing on the disclosed host. Generation-time and runtime costs are
  separated. Two complete harness runs are reported.
- **Packaging:** the generator is non-packable, not part of `FunnySharp.slnx` shipping projects,
  not referenced by `FunnySharp.csproj`, and adds no runtime dependency. If the decision is
  negative, no product contract, grammar, stability inventory, API baseline, or package changes.
  If the decision were ADOPT/ADAPT, a later product goal would choose packaging and public shape;
  this plan does not.

### Scope markers

**Included:** bounded summary and decoder source generation, diagnostics, semantic tests, shared
parity verifier, measurement harness, evidence record, goal close.

**Deferred to follow-up work (only if a later goal reopens):** async/span/memory generated forms,
nested object-list decoding, custom summary leaves, public packaging, build-server caching policy.

**Outside this product's identity:** a universal compiler, a second language, rewriting arbitrary
call sites, a runtime scheduler, wholesale LINQ replacement, a general data mapper.

## Planning Contract

### Requirements

- **R1:** Add a netstandard2.0 Roslyn 4.14 incremental generator with injected `FusedSummary` and
  `FusedDecoding` attributes. It must process opt-in partial methods only and never rewrite
  unrelated source.
- **R2:** Resolve the named `static readonly` field and its initializer by symbol and syntax. The
  generator must not execute user code, infer purity from arbitrary delegates, or follow helper
  methods.
- **R3:** Support the summary subset in "Prototype public shape": single `Zip` arity 2–4,
  built-in components, static expression selectors, `IEnumerable<T>` source. Emit a direct loop
  preserving order, counts, checked arithmetic, extrema semantics, exceptions, and disposal.
- **R4:** Support the decoder subset: `Decoder.Object` arity 2–4, flat required/optional fields,
  scalar leaves, scalar lists, object-creation combine. Emit a success fast path and call the
  declared decoder for every non-proven-valid input.
- **R5:** Every unsupported construct produces an error diagnostic with a stable ASCII ID and a
  precise location. Recognizer rejections also emit a throwing body rather than silently changing
  guarantees; method-shape rejections emit no implementation and the diagnostic fails the build.
  Supported shapes produce no diagnostics.
- **R6:** Add deterministic xUnit v3 tests for semantic parity, callback/selector order and count,
  source enumeration count, decoder error equality across valid and invalid fixtures, duplicate
  property first-match behavior, missing/null/wrong-kind/malformed/empty boundaries, checked sum
  and selector overflow, empty/tied/null extrema with reference-distinct tied keys, source
  enumerator faults, decoder construction count/fault, every supported leaf, and unsupported-shape
  diagnostics. Generator tests must compile the generated source, not only inspect its text.
- **R7:** Add a shared comparison verifier and a measurement executable. The verifier asserts
  generated/delegated parity and fixed expected outcomes; the measurement harness runs the same
  complete consumers twice with one allocation counter and `TieredCompilation=false`.
- **R8:** Disclose generated code size, generator execution time, build-time direction,
  allocation, copying, and directional timing. Report slower or worse axes plainly.
- **R9:** Keep the generator non-shipping; wire the projects into `FunnySharp.slnx`; do not change
  the API baseline, product contract, grammar, or stability inventory unless a public shape is
  actually adopted.
- **R10:** Close the research outcome: move `0039-goal.md` unchanged to archive, update the goals
  index and related navigation, record the maintainer decision with reasoning and non-adoptions,
  and state explicitly that no public capability changed for a negative decision.
- **R11:** Run the required gates and record real counts: build, test, format, docs snippets, API
  baseline, examples, and the measurement harness twice.

### Non-goals

- No shipping source generator, package, analyzer diagnostic family, or public attribute API.
- No general expression compiler, IL weaving, runtime code generation, or delegate purity
  analysis.
- No async, span, or memory generated forms in this prototype.
- No nested object-list decoder generation; no replacement for `Decoder`'s complete composition
  surface.
- No `Contract<T>` or arbitrary `Summary.Create` generation.
- No benchmark package dependency on Hyperlinq or any competitor.

## Implementation Units

### U1. Generator skeleton and attribute injection

- **Goal:** A buildable netstandard2.0 incremental generator that injects the two attributes and
  reports unsupported shapes.
- **Requirements:** R1, R2, R5.
- **Dependencies:** none.
- **Files:** `tests/FunnySharp.FusedExecution.Generator/`.
- **Approach:** `IIncrementalGenerator`, two `ForAttributeWithMetadataName` pipelines, a shared
  semantic parser, stable diagnostic descriptors, `RegisterPostInitializationOutput` for the
  attributes. Emit generated sources with `#nullable enable` and fully qualified names.
- **Execution note:** Build and unit-test the skeleton against a trivial supported field before
  adding parser breadth.
- **Patterns to follow:** existing analyzer project uses Roslyn 4.14, netstandard2.0,
  `IsPackable=false`, locked restore.
- **Test scenarios:** attribute injection; supported partial method compiles; unsupported shape
  reports one error and a throwing body.
- **Verification:** generator project build; generator tests.

### U2. Summary parser and emitter

- **Goal:** Generate a direct loop from a supported `Summary.Zip` field.
- **Requirements:** R2, R3, R6.
- **Dependencies:** U1.
- **Files:** `tests/FunnySharp.FusedExecution.Generator/SummaryFusion.cs`,
  `tests/Shared/FusedExecutionComparison/`.
- **Approach:** parse the field initializer expression, flatten one `Zip` call, resolve each
  component symbol, validate selectors, bind each selector in a static local function with its
  declared parameter and return types, emit checked count/sum accumulation, and extrema state.
- **Execution note:** reject nested `Zip`, `MapResult`, custom `Create`, method groups, captured
  lambdas, custom comparers.
- **Test scenarios:** empty; single; reference-distinct ties; null keys; checked addition and
  unchecked selector overflow for `int`/`long`; selector order/count for sum and extrema; source
  `MoveNext`/`Current`/`Dispose` faults; one enumeration.
- **Verification:** parity tests and measurement rows.

### U3. Decoder parser and emitter

- **Goal:** Generate a success fast path from a supported `Decoder.Object` field, with exact
  runtime fallback.
- **Requirements:** R2, R4, R6.
- **Dependencies:** U1.
- **Files:** `tests/FunnySharp.FusedExecution.Generator/DecoderFusion.cs`,
  `tests/Shared/FusedExecutionComparison/`.
- **Approach:** parse `Decoder.Object`, field wrappers, leaf symbols, list wrappers, and
  object-creation combines. Emit first-match property lookup, kind/scalar checks, read-only list
  construction (`Array.Empty<T>()`/`AsReadOnly()`), then a static local combine function; every
  failure branch returns `Field.Decode(input)`.
- **Execution note:** diagnose nested object lists, custom decoders, optional/field policies not
  implemented, and non-construction combines.
- **Test scenarios:** valid fixtures; missing; null; wrong kind; malformed number; empty
  non-empty list; duplicate property first-match; optional/absence; every supported leaf;
  read-only/list-overload shape; combine construction count and fault; error equality.
- **Verification:** parity tests and measurement rows.

### U4. Comparison consumers, verifier, and measurements

- **Goal:** Complete generated/delegated/hand-written/LINQ consumers and an honest harness.
- **Requirements:** R7, R8.
- **Dependencies:** U2, U3.
- **Files:** `tests/Shared/FusedExecutionComparison/`,
  `tests/FunnySharp.FusedExecutionMeasurements/`.
- **Approach:** shared models/fixtures; delegated and hand-written consumers; fixed-expectation
  verifier; measurement harness with three rounds, best round, one allocation counter, and two
  process runs. Add a generator-timing test or console mode.
- **Execution note:** charge shared report construction equally; do not count generation in
  runtime rows.
- **Test scenarios:** verifier parity; fixed expected values/errors; two harness runs.
- **Verification:** test gate and measurement transcript.

### U5. Evidence, decision, and close

- **Goal:** Maintainer-ready evidence and goal closure.
- **Requirements:** R9, R10, R11.
- **Dependencies:** U4.
- **Files:** `docs/compile-time-fused-execution-evidence.md`, `docs/goals/README.md`,
  `docs/goals/archive/0039-goal.md`, navigation files.
- **Approach:** record supported/unsupported table, semantic ledger, comparison tables, costs,
  residual risks, and the ADOPT/ADAPT/DEFER/REJECT judgment. Move the goal unchanged; make no
  speculative API change for a negative decision.
- **Execution note:** reconcile constitutional prose only if a public shape was adopted.
- **Test scenarios:** none beyond gates.
- **Verification:** final gate transcript and working-tree review.

## Verification Contract

| Gate | Command | Applies to | Completion evidence |
| --- | --- | --- | --- |
| Build | `dotnet fsi build.fsx -- -p build` | all projects | 0 warnings, 0 errors |
| Test | `dotnet fsi build.fsx -- -p test` | functional and generator tests | 0 failed, 0 skipped |
| Format | `dotnet fsi build.fsx -- -p format` | C# | clean |
| Docs snippets | `dotnet fsi build.fsx -- -p verify-docs-snippets` | primary guides | unchanged count unless a guide is added |
| API baseline | `dotnet fsi build.fsx -- -p verify-api-baseline` | shipping assemblies | 0 delta (negative decision) |
| Examples | `dotnet run --project examples/FunnySharp.Examples/FunnySharp.Examples.csproj -c Release` | examples | passed |
| Measurement | `dotnet run --project tests/FunnySharp.FusedExecutionMeasurements/FunnySharp.FusedExecutionMeasurements.csproj -c Release` | comparison | two complete runs, parity verified |

## Definition of Done

- **U1–U4:** generator, consumers, verifier, and measurements build and run; supported paths are
  parity-tested; unsupported paths diagnose.
- **U5:** evidence record states the decision, supported table, costs, residual risks, and the
  explicit no-public-change statement for a negative decision; goal archived unchanged; index
  updated.
- **Global:** all gates have real recorded results; the working tree is left uncommitted; no
  Goal 0040 work starts.
