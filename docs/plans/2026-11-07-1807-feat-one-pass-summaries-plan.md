---
title: Composable one-pass sequence summaries
type: feat
date: 2026-11-07
artifact_contract: ce-unified-plan/v1
product_contract_source: docs/goals/archive/0034-goal.md, docs/product-contract.md, docs/grammar.md
execution: code
---

# One-Pass Summaries Plan

## Goal Capsule

- **Objective:** Let a consumer define count, sum, extrema, and a domain-specific summary
  independently, combine them into one typed result, and evaluate the combination over a BCL
  sequence with one traversal and isolated per-component state. A summary definition remains
  reusable when the same calculation is later fed by an asynchronous source.
- **Means:** A sealed `Summary<TSource, TState, TResult>` description with `Evaluate` and
  `EvaluateAsync`; a non-generic `Summary` factory for `Create` and flat `Zip` arities 2-4; a
  static `Summary<TSource>` factory for `Count`, selector-based `Sum`, and selector/comparer
  `Min`/`Max`; `MapResult` to project the combined tuple into a consumer record. All state is a
  value local to one evaluation; state initialization comes from a seed value or a caller-supplied
  seed factory.
- **Authority:** [Goal 0034](../goals/archive/0034-goal.md) is the completion contract.
  [Product contract](../product-contract.md) and [grammar](../grammar.md) are the current product
  boundaries. Goal 0035 (cardinality and constrained joins) and Goal 0039 (compile-time fusion)
  remain out of scope.
- **Execution profile:** Implement in `src/FunnySharp/Summary.cs`, pin behavior in
  `tests/FunnySharp.Tests`, share the consumer comparison in `tests/Shared/SummaryComparison`,
  exercise through `examples/FunnySharp.Examples/SummaryExamples.cs`, measure through
  `tests/FunnySharp.SummaryMeasurements`, and record evidence in
  `docs/one-pass-summaries-evidence.md`.
- **Tail ownership:** The maintainer judges whether reusable composition improves complete
  consumer code without an unexplained performance tax. A negative judgment returns to the
  composition surface or the built-in factory set; it does not narrow the goal contract.
- **Stop conditions:** Stop for a new decision if the maintainer rejects `Summary`/`Evaluate` as
  the vocabulary, if a required empty-extrema or numeric-overflow semantic would contradict the
  shipped `*OrNone`/BCL behavior, if the comparison cannot demonstrate a real one-traversal
  difference, or if preserving the goal would force a public typeclass hierarchy, automatic
  parallelism, a LINQ replacement, or compile-time fusion.

## Product Contract

**Included**

- `Summary.Create<TSource, TState>(seed, accumulate)` and
  `Summary.Create<TSource, TState, TResult>(seed, accumulate, result)` for independent summaries
  over a value seed; seed-factory overloads of both for per-evaluation state isolation.
- `Summary<TSource>.Count()` with an `int` result, checked for overflow.
- `Summary<TSource>.Sum<TNumber>(selector)` where `TNumber : INumber<TNumber>`; checked sequential
  addition in source order, zero for empty input, `OverflowException` on machine overflow.
- `Summary<TSource>.Min<TKey>` / `Max<TKey>` with selector and optional explicit `IComparer<TKey>`
  forms; `Option<TKey>` result; empty or all-null-key input is `None`; null keys are skipped;
  ties retain the first encountered key.
- `Summary.Zip` for 2, 3, and 4 summaries producing flat tuple states and flat tuple results, plus
  `MapResult` to project the result once after traversal.
- `Summary<TSource, TState, TResult>.Evaluate(IEnumerable<TSource>)`: eager, one enumeration,
  source order, no materialization, no cache.
- `Summary<TSource, TState, TResult>.EvaluateAsync(IAsyncEnumerable<TSource>, CancellationToken)`:
  reuses the same synchronous summary calculation; the token governs source acquisition and is
  forwarded unchanged; no sync-over-async, no async accumulate callback.
- Empty input returns each component's result computed from its seed without invoking any
  accumulate delegate. Every component observes every source item; there is no early-exit channel.
- A source, accumulate, seed-factory, result-selector, comparer, or selector exception propagates
  unchanged. An acquired enumerator is disposed exactly once; normal C#/async disposal precedence
  applies.
- A comparison consumer proves outcome parity with one hand-written loop, proves the combined form
  enumerates the source once where separate summary evaluations enumerate it once per component,
  and measures state size, delegate overhead, allocation, and large-value copying.
- An explicit MapAccum assessment compares the proposed state-plus-output transformation with
  `Scan` plus `Select`, `Aggregate` over a tuple, and a hand loop; no public operation is added
  unless evidence shows state-isolation or expression gains.

**Deferred to Follow-Up Work**

- Automatic parallel summary evaluation remains absent; arbitrary callbacks do not establish
  associativity, commutativity, purity, or safe partitioning.
- A partial-result or early-completion protocol remains absent; single fallible short-circuiting
  stays on `AggregateResult` ([fallible reduction](../fallible-reduction.md)).
- A public typeclass, HKT, or summary-interface hierarchy remains absent.
- Compile-time fusion, expression-tree analysis, and source generation remain the separate
  research outcome of Goal 0039.
- A LINQ replacement, query syntax, `Where`, `SelectMany`, or a second sequence carrier is not
  added. `System.Linq` remains the ordinary query surface.

**Outside this product's identity**

- A new outcome or absence carrier; automatic exception-to-domain-error conversion; a custom
  scheduler or parallel runtime; pervasive immutability; a second collection universe; a custom
  retry/backoff layer.

## Planning Contract

### Requirements

- **R1:** Add the public `Summary` factory, `Summary<TSource>` built-in factory, and sealed
  `Summary<TSource, TState, TResult>` description with complete XML documentation.
- **R2:** `Create` accepts value seeds and seed factories; null delegates are rejected eagerly; a
  null seed value is a valid caller-owned value.
- **R3:** `Count` returns the exact item count, checked for `int` overflow, and `0` for empty input.
- **R4:** `Sum` invokes its selector once per reached item, adds in source order with checked
  arithmetic, returns zero for empty input, and does not reassociate or parallelize.
- **R5:** `Min`/`Max` return `Option<TKey>`, skip null keys before invoking the comparer, return
  `None` for empty/all-null input, and retain the first key on a comparer tie.
- **R6:** `Zip` composes 2-4 summaries into a flat tuple state and result; components initialize in
  operand order, run in operand order per item, and finalize in operand order after traversal.
  `MapResult` runs once after the composed result.
- **R7:** `Evaluate` is eager, enumerates the source exactly once, requests items in source order,
  invokes each reached component accumulate once per item, and does not materialize, buffer, cache,
  or defer an intermediate sequence.
- **R8:** Empty input returns each component result computed from its initialized seed without
  invoking any accumulate delegate.
- **R9:** Execution state is local to one evaluation. A value seed initializes that local state
  from the exact seed value; a seed factory is invoked once per component per evaluation. Mutable
  seeds are not cloned, snapshotted, isolated, or rolled back. A factory is the documented
  isolation form, including when the same summary definition appears twice in one `Zip`.
- **R10:** Source, seed-factory, accumulate, result, selector, comparer, and map exceptions
  propagate unchanged. No exception is converted to a result value. The acquired enumerator is
  disposed exactly once after success or failure; disposal exceptions keep normal precedence.
- **R11:** No component can stop or skip source enumeration. A component whose result is already
  determined still observes every later item, so composing it with another required component
  preserves the second component's input.
- **R12:** `EvaluateAsync` reuses the same summary definition, forwards the exact token to
  `GetAsyncEnumerator`, awaits each source item sequentially, never prefetches, never blocks, and
  propagates cancellation and faults with normal .NET semantics.
- **R13:** Null source, null summary operand, and null selector/delegate/comparer arguments throw
  eagerly before enumeration.
- **R14:** Provide an instrumented sync/async source, separate-summary and hand-written-loop
  reference consumers, a complete comparison, and a MapAccum assessment artifact. Add a dedicated
  measurement executable reporting state size, per-item delegate counts/overhead, allocation, and
  large-value copying.
- **R15:** Ship XML documentation, a compiling example, a guide closing with
  `## Deliberate Boundaries`, an evidence record, API-baseline and performance-manifest updates,
  and reconciled product/grammar/stability/index/goal documentation.
- **R16:** Add no carrier, sequence hierarchy, public typeclass, automatic parallelism, early-exit
  protocol, LINQ replacement, compile-time fusion, or new runtime dependency.

### Key Technical Decisions

- **KTD1 (governs R1, R16):** The reusable description is `Summary<TSource, TState, TResult>` in
  `Summary.cs`, beside non-generic `Summary` and static `Summary<TSource>` factories. `Summary` is
  a description, not a carrier: it owns no failure or absence channel and stays outside the
  canonical carrier vocabulary.
- **KTD2 (governs R2, R9):** `Create` exposes both a value seed and a seed factory. The value seed
  is assigned to the evaluation-local state and is never cloned, so a mutable reference seed is
  visibly shared; the factory is the isolation form and runs once per component per evaluation.
  A summary definition itself holds only delegates and is reusable across evaluations and sources.
- **KTD3 (governs R3-R5):** Built-ins encode the edge semantics rather than leaving them to every
  caller: `Count` and `Sum` use `checked` arithmetic (matching BCL `Count`/`Sum` overflow),
  `Sum` adds strictly in source order, and extrema return `Option<TKey>`, skip null keys, and keep
  the first tied key. `Min`/`Max` constrain `TKey : IComparable<TKey>` and offer an explicit
  comparer constructor, mirroring BCL `Min`/`Max` and the shipped `MinOrNone`/`MaxOrNone`.
- **KTD4 (governs R6, R7):** `Zip` is a static 2/3/4-arity combinator returning flat tuple state
  and result types. Flat arity avoids nested tuple drilling at the canonical four-summary call
  site; the bounded 2-4 range mirrors carrier `Zip`, and more components remain expressible by
  nesting or a domain-specific summary. `MapResult` projects the final tuple exactly once.
- **KTD5 (governs R7, R8, R9):** The evaluator is a direct `foreach`/`await foreach` over a local
  state variable. There is no iterator, materialized sequence, cache, or shared mutable
  accumulator. Empty input therefore returns seed-derived results with zero accumulate calls.
- **KTD6 (governs R10):** No `try`/`catch` translates exceptions. `foreach`/`await foreach` owns
  disposal on every exit path. A component exception stops the remaining components for that item
  and the final result selectors; it does not roll back caller-owned mutable state.
- **KTD7 (governs R11):** Summaries are total folds. There is no completion predicate, partial
  result, or skip protocol. This makes the "do not stop while another component needs input"
  invariant structural: the evaluator has no stop branch. Consumers that need to stop use
  `FirstOrNone`, `AggregateResult`, or ordinary control flow.
- **KTD8 (governs R12):** `EvaluateAsync` accepts an `IAsyncEnumerable<TSource>` only; the
  calculation stays synchronous. This is the goal's "synchronous calculation remains reusable
  when only input acquisition is asynchronous" clause. There is no `ValueTask` accumulate overload
  and no async summary variant.
- **KTD9 (governs R13):** Public non-async members validate arguments before any work.
  `EvaluateAsync` validates in a non-async wrapper and then calls a private `async ValueTask` core,
  so null arguments throw directly while source/accumulate faults surface through the operation.
- **KTD10 (governs R14, R15):** The comparison is Compile-included shared test support used by both
  the xUnit suite and a dedicated measurement executable. It runs before printing measurements.
  The MapAccum assessment is a shared local helper and consumer comparison, not public API.
- **KTD11 (governs R16):** Composition is runtime delegate composition. No expression trees, no
  fusion, no parallel scheduler, no `Summary` interface hierarchy, and no changes to the core
  package dependency graph.

### Open Decisions And Recommendations

1. **Type/verb spelling.** Options: `Summary` + `Evaluate`; `Summary` + `Summarize`; a source
   extension `AggregateSummary`; a `Fold`-named family. Recommendation: `Summary` + `Evaluate`.
   A summary is a reusable description, and `Evaluate` names execution without introducing a
   sequence verb that suggests LINQ replacement. `Fold` is already rejected AD-9 vocabulary.
2. **Composition shape.** Options: static flat `Zip` arities 2-4; instance pair `Zip`; a builder
   DSL. Recommendation: static flat arities plus `MapResult`. The canonical count/sum/min/max
   combination then returns one flat tuple, with no nested-tuple drilling, while the surface stays
   bounded and mechanical.
3. **Seed shape.** Options: value seed only; factory only; both. Recommendation: both. Value seeds
   keep ordinary immutable and value-type summaries terse; factories make per-evaluation isolation
   explicit for mutable storage. Both semantics are tested, and the guide recommends the factory
   for mutable state.
4. **Built-in extent.** Options: `Create` only; count/sum/min/max families; a larger statistics
   library. Recommendation: the four named families only. They carry the non-obvious overflow,
   empty-extrema, null-key, and ordering semantics the goal requires; averages, percentiles, and
   other statistics stay consumer summaries.
5. **Extrema emptiness.** Options: `Option<TKey>`; throw; return a default sentinel. Recommendation:
   `Option<TKey>`. Absence is not an error, `None` has exactly one meaning, and `MinOrNone` already
   establishes this behavior.
6. **Sum overflow.** Options: `checked`; `unchecked`; delegate policy. Recommendation: `checked`.
   BCL `Enumerable.Sum` throws for integral overflow, and silent wraparound would violate the
   goal's "actual machine arithmetic" requirement. Floating-point and `decimal` behavior follows
   their own checked/rounding semantics, and sequential order is never reassociated.
7. **Early completion.** Options: total folds only; per-component completion plus partial results;
   stop when all components complete. Recommendation: total folds only. A completion protocol would
   need partial-result types, finalizer timing, failure interaction, and a new observable stop
   contract, while no evidenced consumer needs it. Total folds structurally satisfy the goal's
   "do not stop a source while another component needs it" clause.
8. **MapAccum.** Recommendation: do not add a public operation. `Scan` plus `Select` already
   expresses output-only state threading in one deferred pass; `Aggregate` over `(state, outputs)`
   is the complete both-outputs-and-final-state form and is a short hand loop. A public `MapAccum`
   would duplicate that shape without adding state isolation (state is threaded sequentially in
   both) or a demonstrated expression gain. The assessment is recorded with code and line counts.
9. **Async callback shape.** Options: async accumulation; synchronous calculation with asynchronous
   acquisition; both. Recommendation: synchronous calculation with asynchronous acquisition. It
   preserves one calculation definition across sync and async inputs, matches the goal sentence,
   and avoids a second `ValueTask` overload tower.
10. **Mutable result ownership.** Options: clone the final state; return it directly. Recommendation:
    return it directly. The library never clones caller state; a consumer that returns its own
    mutable state owns that aliasing and the guide states it.

### Acceptance Examples

- **AE1:** Count, sum, min, max, and a domain-specific category summary are defined independently,
  zipped into a flat result, projected to an order report, and evaluated over a `List<OrderLine>`
  with one enumerator and one `MoveNext` sequence.
- **AE2:** Empty input returns count `0`, sum `0`, min/max `None`, and the custom summary's
  seed-derived result, with zero accumulate calls and zero selector calls.
- **AE3:** Six separate summary evaluations over a probe enumerate the source six times; the
  composed evaluation enumerates once and returns the same complete result as one hand-written
  loop.
- **AE4:** Empty/all-null extrema return `None`; the comparer never observes a null key; a custom
  comparer tie retains the first key reference.
- **AE5:** Adding `int.MaxValue + 1` and adding a negative value to `long.MinValue` throw
  `OverflowException`, dispose the enumerator, and return no partial result.
- **AE6:** Reusing a mutable value seed accumulates across evaluations, while a seed factory
  produces isolated state; the same factory-seeded summary composed twice in one `Zip` produces
  two independent accumulations.
- **AE7:** A throwing source, seed factory, accumulate, result selector, selector, comparer, or
  `MapResult` selector propagates its original exception; the enumerator is disposed once; a
  disposal exception replaces a pending success.
- **AE8:** A component whose result is determined by the first item still observes every later
  source item; a second component's count/sum remains correct and the source reaches its end.
- **AE9:** The same summary instance evaluates a synchronous source and an asynchronous source with
  equal results; the async form forwards the exact token, enumerates once, disposes once, and keeps
  cancellation/faults as cancellation/faults.
- **AE10:** The MapAccum assessment runs the same pricing fixture through `Scan` + `Select`,
  `Aggregate` over a tuple, and a hand-written helper; outcomes agree, and the evidence records why
  no public operation is added.
- **AE11:** The measurement executable reports real state sizes, allocation, delegate counts,
  timing, and large-value copying for combined, separate, and hand-written consumers at 0, 16, and
  1024 items, including async rows.

## Implementation Units

### U1. Add the summary description and factories

- **Goal:** Add `Summary`, `Summary<TSource>`, and `Summary<TSource, TState, TResult>` with
  complete XML documentation.
- **Requirements:** R1, R2, R6, R9, R13, R16.
- **Dependencies:** None.
- **Files:** `src/FunnySharp/Summary.cs`.
- **Approach:** A sealed class holding seed/seed-factory, accumulate, and result delegates. Public
  `Evaluate` and `EvaluateAsync`; `MapResult`; `Create` value/factory overloads with and without a
  final result selector; static `Zip` arities 2-4. Validate every delegate/source eagerly. The
  evaluator is a direct loop over a local state.
- **Execution note:** No iterator methods; no `try`/`catch`; no mutable shared fields. Tuple states
  and results are `ValueTuple`, not library-owned classes.
- **Patterns to follow:** `BatchRequirement` description shape; `Effect` deferred/callable boundary
  docs; `FallibleReductionExtensions` wording for single enumeration and disposal.
- **Test scenarios:** argument validation, flat tuple composition, map projection, empty input,
  repeated evaluation, factory isolation. `(happy path)`, `(edge)`, `(error path)`.
- **Verification:** Build the core project; run the focused `Summary*` tests.

### U2. Add the built-in count, sum, and extrema summaries

- **Goal:** Add `Summary<TSource>.Count`, `Sum`, `Min`, and `Max`.
- **Requirements:** R3-R5, R9, R15.
- **Dependencies:** U1.
- **Files:** `src/FunnySharp/Summary.cs`.
- **Approach:** `Count` is a checked `int` increment. `Sum` combines `TNumber.Zero` with
  `checked(accumulator + selected)` in source order. `Min`/`Max` hold `Option<TKey>`, skip null
  keys before invoking the comparer, use `Comparer<TKey>.Default` or the supplied comparer, and
  replace only on a strict comparison. Document selector invocation, null skipping, ties, and
  checked overflow.
- **Execution note:** Do not constrain against `INumber` beyond the generic arithmetic operators
  needed by `checked` addition; do not special-case `double` or reassociate. `decimal`/floating
  behavior remains the machine behavior of sequential addition.
- **Patterns to follow:** `CardinalityExtensions.MinOrNone`/`MaxOrNone` null semantics; BCL
  `Enumerable.Sum` checked behavior; `Option<T>` construction rules.
- **Test scenarios:** zero/empty, normal totals, overflow, decimal and double sequences, null keys,
  all-null keys, ties, custom comparer, selector faults. `(happy path)`, `(edge)`, `(error path)`.
- **Verification:** Focused tests plus the combined comparison.

### U3. Add asynchronous-source evaluation

- **Goal:** Add `EvaluateAsync` over `IAsyncEnumerable<TSource>` with exact-token forwarding.
- **Requirements:** R12, R13.
- **Dependencies:** U1.
- **Files:** `src/FunnySharp/Summary.cs`.
- **Approach:** A non-async public wrapper validates the source and calls a private
  `async ValueTask<TResult>` core using
  `await foreach (source.WithCancellation(cancellationToken).ConfigureAwait(false))`. The same
  seed/accumulate/result delegates run synchronously per item. No token inspection, blocking, or
  prefetching.
- **Execution note:** No `Task` overload, no `ValueTask` accumulate, no cancellation-aware
  accumulate. `await foreach` owns disposal.
- **Patterns to follow:** `AsyncFallibleReductionExtensions` argument validation and token
  forwarding; `AsyncSequenceExtensions` disposal/fault wording.
- **Test scenarios:** success, empty, sync/async reuse of one definition, exact token, already
  canceled token ignored by a cooperative source, canceled source, source/accumulate faults,
  disposal exception, pending source without blocking. `(happy path)`, `(edge)`, `(error path)`.
- **Verification:** Focused async tests; comparison async pass.

### U4. Build the shared comparison, probes, and MapAccum assessment

- **Goal:** Prove one traversal, outcome parity, edge semantics, and the MapAccum non-adoption.
- **Requirements:** R7, R8, R10, R11, R14.
- **Dependencies:** U1-U3.
- **Files:** `tests/Shared/SummaryComparison/SummaryModels.cs`,
  `SummaryRules.cs`, `SummaryFixtures.cs`, `../FallibleReduction/ReductionProbes.cs`,
  `OrdinarySummaryConsumer.cs`, `SeparateSummaryConsumer.cs`,
  `FunnySharpSummaryConsumer.cs`, `MapAccumAssessment.cs`, `SummaryComparison.cs`.
- **Approach:** An instrumented sync/async source records enumerator creation, `MoveNext`,
  `Current`, `Dispose`, token, and optional faults. Six summaries (count, sum, min, max, category
  counts, and first SKU) are composed. The ordinary consumer is one loop; the separate consumer evaluates each
  summary independently; the FunnySharp consumer uses `Summary.Zip(...).MapResult(...).Evaluate`.
  `Verify`/`VerifyAsync` assert structural outcome equality, exact consumption/disposal counts,
  component invocation order, token identity, and the early-determined-component invariant. The
  MapAccum assessment compares a local `MapAccum` helper with `Scan` + `Select` and `Aggregate`
  over a tuple.
- **Execution note:** Do not weaken the probe to count only accumulate calls; source request counts
  are the acceptance-relevant comparison. The MapAccum helper is internal test support only and is
  never added to `src`.
- **Patterns to follow:** `tests/Shared/FallibleReduction/ReductionComparison.cs` probe/ledger
  assertions; `CheckComparison` shared consumer structure.
- **Test scenarios:** empty, single, ties, ordinary, fault-after-failure, large-value state,
  async cancellation/fault, MapAccum parity. `(happy path)`, `(edge)`, `(error path)`.
- **Verification:** The comparison runs in the xUnit suite and measurement executable.

### U5. Pin semantics with the xUnit suite

- **Goal:** Add focused tests for every contract edge.
- **Requirements:** R1-R13.
- **Dependencies:** U1-U4.
- **Files:** `tests/FunnySharp.Tests/SummaryTests.cs`,
  `SummaryAsyncTests.cs`, `SummaryComparisonTests.cs`,
  `tests/FunnySharp.Tests/FunnySharp.Tests.csproj`.
- **Approach:** Attribute-only `[Fact]`/`[Theory]`; classic asserts; no skips. Use the shared probes
  for consumption/disposal. Deliberate nullability and uninitialized-value warnings, if any, are
  suppressed locally with the repository's existing patterns; never skip a test.
- **Execution note:** No sleeps and no timing asserts. Pending async cases use a
  `TaskCompletionSource`/value-task source completed explicitly.
- **Patterns to follow:** `FallibleReductionTests`/`FallibleReductionAsyncTests` structure;
  `ProbeEnumerable` counting conventions.
- **Test scenarios:** AE1-AE9 plus null arguments, map faults, disposal precedence, repeated
  evaluation, mutable seed alias/isolation, selector/comparer faults, and tie identity.
  `(happy path)`, `(edge)`, `(error path)`, `(integration)`.
- **Verification:** Focused run reports zero failed and zero skipped; full suite stays green.

### U6. Add the compiling example

- **Goal:** Show independent definitions, one-pass composition, a typed report, empty extrema,
  async reuse, and mutable-state isolation.
- **Requirements:** R15.
- **Dependencies:** U1-U3.
- **Files:** `examples/FunnySharp.Examples/SummaryExamples.cs`,
  `examples/FunnySharp.Examples/Program.cs`.
- **Approach:** Build an order-line report from count, sum, min, max, and a category-count summary;
  assert the empty result; reuse the same summary instance for an async source; demonstrate a
  factory-seeded mutable summary appearing twice with isolated state. Assert every result through
  `ExampleAssertions`.
- **Execution note:** The example project must build with zero `FS####` diagnostics. Do not discard
  carrier outcomes.
- **Patterns to follow:** `FallibleReductionExamples` and `CheckExamples` registration/assertion
  shape.
- **Test scenarios:** the program prints the exact `FunnySharp examples passed.` marker.
  `(integration)`.
- **Verification:** `dotnet run --project examples/FunnySharp.Examples -c Release`.

### U7. Add the measurement harness and record evidence

- **Goal:** Measure state size, delegate overhead, allocations, and large-value copying against
  separate summaries and a hand-written loop.
- **Requirements:** R14.
- **Dependencies:** U4-U6.
- **Files:** `tests/FunnySharp.SummaryMeasurements/SummaryMeasurements.cs`,
  `Program.cs`, `FunnySharp.SummaryMeasurements.csproj`, `packages.lock.json`,
  `FunnySharp.slnx`, `docs/one-pass-summaries-evidence.md`.
- **Approach:** Run the comparison first. Report `Unsafe.SizeOf` for individual and composed state
  types; measure combined/separate/ordinary consumers at 0, 16, and 1024 items for `int`,
  `decimal`, a 64-byte value state, and a mutable category dictionary; measure async
  `EvaluateAsync` against `await foreach` and separate async evaluations. Report
  `GetAllocatedBytesForCurrentThread` for sync and `GetTotalAllocatedBytes(precise: true)` for async.
- **Execution note:** Measurements are explicit-request evidence, not a CI gate. No BenchmarkDotNet
  scenario and no numeric release budget without a separate goal.
- **Patterns to follow:** `tests/FunnySharp.FallibleReductionMeasurements` project shape and
  evidence table; `Shared/MeasurementHarness.cs`.
- **Test scenarios:** AE11; comparison passes before measurement output. `(integration)`.
- **Verification:** Release run; copy real values into the evidence record.

### U8. Reconcile docs, baseline, manifests, and goal state

- **Goal:** Ship coherent documentation and committed surface records.
- **Requirements:** R15, R16.
- **Dependencies:** U1-U7.
- **Files:** `docs/one-pass-summaries.md`, `docs/one-pass-summaries-evidence.md`,
  `docs/grammar.md`, `docs/product-contract.md`, `docs/stability-inventory.md`,
  `docs/data-pipelines.md`, `docs/AGENTS.md`, `docs/plans/AGENTS.md`,
  `docs/goals/README.md`, `docs/goals/AGENTS.md`, `docs/goals/archive/0034-goal.md`,
  `AGENTS.md`, `README.md`, `README_zh-cn.md`, `src/FunnySharp/AGENTS.md`,
  `tests/FunnySharp.Tests/AGENTS.md`, `examples/FunnySharp.Examples/AGENTS.md`,
  `eng/api-baseline/FunnySharp.public-api.txt`, `eng/performance/baseline.json`,
  `eng/performance/competitor-baseline.json`.
- **Approach:** Guide starts with `## API Shape`, covers definition/composition/edge semantics/costs,
  and closes with `## Deliberate Boundaries`. Grammar adds a `Summary` row and links the guide.
  Product contract gains one stable-surface paragraph. Stability inventory adds the family row.
  Baseline is regenerated with `verify-api-baseline --write`. Performance manifests add the new
  source path in ordinal order. The completed goal moves unchanged to the archive and the index is
  updated.
- **Execution note:** Do not hand-edit generated inventory or archived goal contents; archive is a
  rename of the unchanged contract. Regenerate the API baseline only after the public surface is
  final.
- **Patterns to follow:** Goal 0033 landing shape (guide/evidence/example/baseline/manifests/
  archive/index).
- **Test scenarios:** API baseline, docs snippets, format, manifest shape, example run, full test
  gate. `(integration)`.
- **Verification:** `verify-api-baseline`, `verify-docs-snippets`, `format`, full `test`.

## Verification Contract

| Gate | Command | Applies to | Completion evidence |
| --- | --- | --- | --- |
| Build | `dotnet fsi build.fsx -- -p build` | U1-U8 | exit 0, zero warnings/errors |
| Test | `dotnet fsi build.fsx -- -p test` | U1-U8 | all tests passed, zero failed, zero skipped |
| Format | `dotnet fsi build.fsx -- -p format` | U1-U8 | exit 0 |
| Docs snippets | `dotnet fsi build.fsx -- -p verify-docs-snippets` | U8 | exit 0 |
| API baseline | `dotnet fsi build.fsx -- -p verify-api-baseline` (after `--write`) | U8 | exit 0 with only the new members |
| Example | `dotnet run --project examples/FunnySharp.Examples/FunnySharp.Examples.csproj -c Release` | U6 | exact `FunnySharp examples passed.` |
| Comparison | `dotnet run --project tests/FunnySharp.SummaryMeasurements/FunnySharp.SummaryMeasurements.csproj -c Release` | U4, U7 | comparison passes; consumption, state-size, allocation, and timing tables captured |
| Manifest shape | full test suite (`PerformanceManifestTests`) | U8 | source lists sorted and complete |

## Definition of Done

### Per-Unit Completion

| Unit | Done when |
| --- | --- |
| U1 | Description, factories, composition, evaluation, map, and argument validation tests pass; no public state is shared across evaluations. |
| U2 | Count/sum/min/max tests cover empty, overflow, ordering, null keys, all-null input, ties, comparers, and selector faults. |
| U3 | Async evaluation matches sync results over an async source; token, cancellation, fault, disposal, and pending behavior are pinned. |
| U4 | The comparison proves equal outcomes, one enumeration for the composed form, six for separate summaries, exact component order, full consumption for an early-determined component, and MapAccum parity. |
| U5 | Focused tests pass with zero failed and zero skipped; the full suite remains green. |
| U6 | The example proves typed composition, empty extrema, async reuse, and mutable-state isolation; the program prints the exact success marker. |
| U7 | The measurement executable runs the comparison and prints real state-size/allocation/timing tables copied into the evidence record. |
| U8 | The guide closes with `## Deliberate Boundaries`; product contract, grammar, stability inventory, baseline, manifests, READMEs, and goal archive agree. |

### Global Completion

- R1-R16 each map to an implementation artifact and an executed verification result.
- AE1-AE11 pass in the test suite or the evidence run.
- The comparison shows one traversal for the composed form, N traversals for N separate summaries,
  and parity with the hand-written loop.
- The evidence discloses state size, delegate overhead, allocation, and large-value copying, and
  does not claim a universal speedup.
- The MapAccum assessment records a non-adoption finding with evidence; no public operation is added.
- No carrier, typeclass hierarchy, automatic parallelism, LINQ replacement, early-exit protocol,
  compile-time fusion, or new runtime dependency is introduced.
- The maintainer has the comparison, measurement table, and residual-risk list for judgment.
