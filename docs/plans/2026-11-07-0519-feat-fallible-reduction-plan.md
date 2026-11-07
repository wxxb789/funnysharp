---
title: Short-circuiting fallible reduction over BCL sequences
type: feat
date: 2026-11-07
artifact_contract: ce-unified-plan/v1
product_contract_source: docs/goals/archive/0033-goal.md, docs/product-contract.md, docs/grammar.md
execution: code
---

# Fallible Reduction Plan

## Goal Capsule

- **Objective:** Let a consumer fold an ordinary enumerable through a `Result`-returning
  accumulator and return the final accumulator or the first typed failure without materializing a
  sequence, without manually managing an enumerator, and without asking the source for another
  item after a typed failure. Provide an asynchronous-source path with the same observable
  contract.
- **Means:** One synchronous extension member, `AggregateResult`, over `IEnumerable<TSource>`; two
  asynchronous-source forms, `AggregateResultAsync` (synchronous step) and
  `AggregateResultValueAsync` (`ValueTask` step, including a cancellation-aware overload), over
  `IAsyncEnumerable<TSource>`. All return the existing `Result<TAccumulate, TError>` carrier. A
  direct `foreach`/`await foreach` loop is the implementation and the reference semantics.
- **Authority:** [Goal 0033](../goals/archive/0033-goal.md) is the completion contract.
  [Product contract](../product-contract.md) and [grammar](../grammar.md) are the current product
  boundaries. Goal 0034 owns composable one-pass summaries and is out of scope.
- **Execution profile:** Implement in `src/FunnySharp`, pin behavior in `tests/FunnySharp.Tests`,
  share the consumer comparison with `tests/Shared/FallibleReduction` and
  `tests/FunnySharp.FallibleReductionMeasurements`, exercise through
  `examples/FunnySharp.Examples`, and record evidence in `docs/fallible-reduction-evidence.md`.
- **Tail ownership:** The maintainer judges consumer clarity, code reduction, and whether the
  measured allocation/copying disclosure is acceptable. A negative judgment returns to the
  affected unit; it does not silently narrow the goal contract.
- **Stop conditions:** Stop for a new decision if the maintainer rejects `AggregateResult` as the
  verb spelling, if a required asynchronous path would force a second carrier or an async wrapper,
  if the comparison cannot show a real upstream source-consumption difference, or if preserving
  the goal requires parallel reduction or rollback semantics.

## Product Contract

**Included**

- `IEnumerable<TSource>.AggregateResult(seed, accumulate)` where `accumulate` is
  `Func<TAccumulate, TSource, Result<TAccumulate, TError>>`; returns
  `Result<TAccumulate, TError>`.
- `IAsyncEnumerable<TSource>.AggregateResultAsync(seed, accumulate, cancellationToken = default)`
  where `accumulate` is the same synchronous delegate; returns
  `ValueTask<Result<TAccumulate, TError>>`.
- `IAsyncEnumerable<TSource>.AggregateResultValueAsync(seed, accumulate, cancellationToken = default)`
  where `accumulate` is `Func<TAccumulate, TSource, ValueTask<Result<TAccumulate, TError>>>`, plus
  a cancellation-aware overload whose delegate receives the exact caller token.
- Empty input returns `Result<TAccumulate, TError>.Success(seed)`, including a null or mutable
  reference seed, without invoking the step.
- Each call enumerates the source once and, on success, invokes the step once per item in source
  order with the accumulator produced by the previous step.
- The first typed failure is returned unchanged; the operation does not call `MoveNext` or the
  step again, and the source enumerator is disposed.
- Source exceptions, step exceptions, and uninitialized step results are never translated into
  typed failures. They propagate with ordinary C#/async exception behavior. An uninitialized step
  result is a programming error (`InvalidOperationException`) raised when the result is inspected.
- Mutable accumulators are caller-owned. The library does not clone, snapshot, isolate, or roll
  back an accumulator. Mutations and external work performed before a failure remain observable.
- The asynchronous forms forward the exact token to the source enumerator and, for the
  cancellation-aware step overload, to the step. They do not inspect the token eagerly, block, or
  convert cancellation into a domain failure.
- A comparison consumer proves parity with a complete `foreach`/`await foreach` reference and
  demonstrates that `Enumerable.Aggregate`/`AsyncEnumerable.AggregateAsync` over `Result` keeps
  consuming the source after the step has already failed.

**Deferred to Follow-Up Work**

- Composable reusable summaries belong to [Goal 0034](../goals/0034-goal.md).
- Accumulating independent item errors remains `Validation` traversal or `CheckAll`, not a
  fallible fold.
- General retry, backoff, scheduling, transaction, compensation, and rollback remain absent.
- A custom async buffer or async wrapper remains absent.

**Outside this product's identity**

- A new stream, sequence, or enumerator hierarchy; a generic early-exit protocol; automatic
  parallel reduction; a second absence/failure carrier; synchronous blocking on asynchronous work;
  exception-to-domain-error conversion.

## Planning Contract

### Requirements

- **R1:** Add the synchronous seeded fallible fold returning the existing `Result` carrier.
- **R2:** Empty input returns the supplied seed and invokes no step.
- **R3:** Success is a single eager enumeration in source order with one step invocation per item
  and no intermediate sequence, list, buffer, or cache.
- **R4:** A step failure returns that failed `Result` and causes no later source `MoveNext` or step
  invocation.
- **R5:** Source exceptions, step exceptions, and uninitialized step results propagate unchanged;
  the acquired enumerator is still disposed.
- **R6:** Describe and test mutable/reference seed behavior: no cloning, no isolation, no rollback,
  and no disposal claim over caller-owned state.
- **R7:** Add an asynchronous-source path with synchronous and `ValueTask` steps, exact token
  forwarding, awaited disposal, and the same short-circuit contract.
- **R8:** Cancellation and faults remain cancellation and faults; no sync-over-async and no
  exception conversion.
- **R9:** Null source or step delegates are rejected eagerly; null seeds, null successful
  accumulators, and null errors remain valid payloads.
- **R10:** Provide a probe source exposing consumption and disposal, complete reference consumers,
  and an `Aggregate`-over-`Result` comparison that shows the upstream difference. Measure
  allocation, copying, and buffering for realistic accumulators.
- **R11:** Ship XML documentation, a compiling example, a guide closing with the repository's
  `## Deliberate Boundaries` section, an evidence record, API-baseline and performance-manifest
  updates, and reconciled product/grammar/index documentation.
- **R12:** Add no stream hierarchy, carrier, parallel reduction, rollback, retry, or generic
  early-exit protocol.

### Key Technical Decisions

- **KTD1 (governs R1, R12):** The verb is `AggregateResult`; `Async` and `ValueAsync` name the
  awaitable source forms. `Aggregate` remains the fold verb; the `Result` suffix names the
  fallible output rather than introducing a second fold spelling. No `Fold`/`Reduce` alias is
  added (the curation test also treats `Fold` as rejected AD-9 vocabulary).
- **KTD2 (governs R2, R3):** The operation is eager and direct: a local accumulator, one
  `foreach`/`await foreach`, and a returned `Result`. It is not an iterator, so it never defers,
  caches, or materializes; the seed object is returned directly on empty input.
- **KTD3 (governs R4):** The step's `Result` is inspected with `TryGetValue`. A failure breaks the
  loop and returns the exact failed result. Because the loop never continues, the enumerator is
  not asked for another item. The step is not invoked after failure for the same reason.
- **KTD4 (governs R5, R6):** No `try`/`catch` converts exceptions. `foreach`/`await foreach`
  supplies normal disposal after success, failure, exception, or cancellation. An uninitialized
  step result throws when `TryGetValue` inspects it, after which normal disposal runs. A throwing
  `Dispose`/`DisposeAsync` keeps normal C#/async precedence and can replace a pending failure; the
  guide states that explicitly. No clone, snapshot, or rollback is introduced for mutable
  accumulators.
- **KTD5 (governs R7, R8):** The asynchronous surface consumes `IAsyncEnumerable<TSource>`. It
  offers a synchronous step and a `ValueTask` step with tokenless and cancellation-aware
  overloads, mirroring `TraverseAsync`/`TraverseValueAsync`. There are no `Task`-step overloads;
  an async lambda can target `ValueTask`. The core is an `async ValueTask` method, so a synchronous
  exception from a step is delivered through the returned operation, while the synchronous API
  throws directly. The token is passed through `WithCancellation` and, where the overload accepts
  it, to the step; it is never inspected eagerly.
- **KTD6 (governs R9):** `source` and the step delegate are validated synchronously before any
  enumeration. The seed is not validated: any `TAccumulate` value, including null, is a legal
  accumulator, matching `Result<TValue, TError>.Success`.
- **KTD7 (governs R10):** The comparison is a shared, Compile-included test-support family used by
  both the xUnit suite and a dedicated measurement executable. The measurement executable runs the
  complete comparison first, then reports consumption counts and allocation/timing rows. No
  BenchmarkDotNet scenario is added; the source paths are added to the existing performance
  manifests so the manifest-coverage test remains truthful.
- **KTD8 (governs R11):** The guide is `docs/fallible-reduction.md`, the evidence record is
  `docs/fallible-reduction-evidence.md`, the example is
  `examples/FunnySharp.Examples/FallibleReductionExamples.cs`, and the goal moves to
  `docs/goals/archive/0033-goal.md` with the goal index updated, following the Goal 0032 landing
  shape.

### Open Decisions And Recommendations

The contract permits several defensible spellings and surface sizes. These are the choices this
plan makes; each is contract-preserving and can be revisited without changing the goal.

1. **Method spelling.** Options: `AggregateResult`, `TryAggregate`, `Reduce`, `FoldResult`.
   Recommendation: `AggregateResult`. `Aggregate` is the repository's fold verb; `TryAggregate`
   would imply the package's exception-boundary `Try*` convention, `Reduce` would introduce a
   synonym, and `FoldResult` collides with rejected AD-9 fold-hierarchy vocabulary in the curation
   test.
2. **Asynchronous shape.** Options: async source only; async step over a synchronous source only;
   both. Recommendation: async source with synchronous and `ValueTask` steps. This mirrors the
   shipped `TraverseAsync`/`TraverseValueAsync` convention, satisfies the goal's
   "asynchronous-source or asynchronous-step" clause, and avoids a second async-step pattern that
   no existing sequence operation uses.
3. **Failure policy.** Options: fail-fast only; add an accumulating `Validation` fold.
   Recommendation: fail-fast only. A fold is sequential and dependent; accumulating independent
   errors is a different operation already owned by `Validation.Traverse`/`CheckAll`.
4. **Step outcome vocabulary.** Options: `Result` only; also add `UnitResult` or `Option` step
   overloads. Recommendation: `Result` only. A same-accumulator check is already expressible by
   returning `Result.Success(accumulator)`; extra step shapes would multiply the surface without a
   distinct contract.
5. **Disposal-exception precedence.** Options: normal C# precedence (disposal exception can replace
   a pending outcome); preserve the domain failure and suppress disposal errors. Recommendation:
   normal C# precedence. Suppressing disposal exceptions would invent a guarantee the BCL does not
   provide and would hide resource failures.
6. **Measurement home.** Options: a BenchmarkDotNet suite entry; a dedicated measurement
   executable like Goal 0032. Recommendation: dedicated executable. It can run the comparison and
   the consumption counters in one place and is not a release timing gate.

### Acceptance Examples

- **AE1:** An empty source returns `Success(seed)` with the exact seed value/reference and zero
  step calls.
- **AE2:** A successful fold invokes the step once per item in source order and returns the final
  accumulator.
- **AE3:** A source `[1, 2, 0, 4]` whose step fails on `0` returns that failure after four
  `MoveNext` calls, with no step for `4`, no fifth `MoveNext`, and one dispose.
- **AE4:** A source that throws after the failing item returns the domain failure through
  `AggregateResult`, while `Aggregate` over `Result` throws the source exception because it kept
  consuming.
- **AE5:** A step that mutates a reference accumulator and then fails leaves the mutation visible;
  no rollback or clone occurs.
- **AE6:** A throwing step, a throwing source, and an uninitialized step result propagate their
  original exception; the enumerator is disposed and later items are not requested.
- **AE7:** The async source forms match the reference `await foreach` consumer on outcomes,
  consumption, and disposal; the token reaches the enumerator and the token-aware step unchanged;
  cancellation remains cancellation.
- **AE8:** Measurement reports show constant-order allocation for a value accumulator across input
  sizes (no buffering), the per-item cost of a large value accumulator, and the mutable
  accumulator's caller-owned allocation. The comparison table shows the `Aggregate`-over-`Result`
  source-consumption difference for both sync and async sources.

## Implementation Units

### U1. Add the synchronous fallible fold

- **Goal:** Add `FallibleReductionExtensions.AggregateResult` with complete XML documentation.
- **Requirements:** R1-R6, R9, R12.
- **Dependencies:** None.
- **Files:** `src/FunnySharp/FallibleReductionExtensions.cs`.
- **Approach:** Validate `source` and `accumulate`; loop with `foreach`; inspect each step with
  `TryGetValue`; return the exact failed result or `Success(accumulator)`. Document empty input,
  single enumeration, no materialization, first-failure short-circuit, disposal, exception
  propagation, uninitialized results, null payloads, and mutable accumulator behavior.
- **Execution note:** No iterator method and no `try`/`catch`; `foreach` owns disposal.
- **Patterns to follow:** `EnumerablePipelineExtensions` argument checks, `Result` XML-doc
  precision, and `SequenceExtensions` enumeration/fault wording.
- **Test scenarios:** Empty seed identity, success ordering, first-failure cut-off, source/step
  exceptions, uninitialized step, null payloads, mutable accumulator, repeated calls, null
  arguments. `(happy path)`, `(edge)`, `(error path)`.
- **Verification:** Build the core project; run the focused `FallibleReduction*` tests.

### U2. Add the asynchronous-source fallible fold

- **Goal:** Add `AsyncFallibleReductionExtensions.AggregateResultAsync` and
  `AggregateResultValueAsync` (including the cancellation-aware step overload).
- **Requirements:** R7-R9, R12.
- **Dependencies:** U1 (shared semantics and naming).
- **Files:** `src/FunnySharp/AsyncFallibleReductionExtensions.cs`.
- **Approach:** Validate arguments in the non-async public methods, then call a private
  `async ValueTask` core using `await foreach (source.WithCancellation(token).ConfigureAwait(false))`.
  The synchronous step wraps through `ValueTask.FromResult`; the `ValueTask` step is awaited once.
  A failure returns before the next `MoveNextAsync`; `await foreach` disposes on every exit.
- **Execution note:** No `Task` overloads, no eager token inspection, no blocking, no catch blocks.
- **Patterns to follow:** `AsyncSequenceExtensions.TraverseAsync`/`TraverseValueAsync` token and
  fault wording; `docs/validation.md` async traversal contract.
- **Test scenarios:** Async success, empty, first-failure cut-off, source/step faults, uninitialized
  step, disposal, token forwarding, already-canceled token not inspected eagerly, canceled source,
  canceled step, single `ValueTask` observation. `(happy path)`, `(edge)`, `(error path)`.
- **Verification:** Build the core project; run the focused async tests.

### U3. Build the shared comparison and probes

- **Goal:** Provide a source that exposes consumption/disposal, the complete `foreach`/`await
  foreach` references, and the `Aggregate`-over-`Result` baseline.
- **Requirements:** R3-R10.
- **Dependencies:** U1, U2.
- **Files:** `tests/Shared/FallibleReduction/*.cs`.
- **Approach:** A sync and async probe record `GetEnumerator`, `MoveNext`, `Current`, dispose, and
  optional source faults. Shared domain fixtures model line parsing and a running total. The
  ordinary consumer is the complete loop; the aggregate baseline is
  `source.Aggregate(Result.Success(seed), (acc, item) => acc.Bind(current => step(current, item)))`
  (and `AggregateAsync` for the async probe). `ReductionComparison.Verify`/`VerifyAsync` assert
  outcome parity, consumption counts, disposal counts, step counts, and the source-fault-after-
  failure difference.
- **Execution note:** The comparison must fail if the new operation keeps calling `MoveNext` after
  a typed failure. Do not weaken the probe to only count step calls.
- **Patterns to follow:** `tests/Shared/CheckComparison` shared-consumer structure; `ProbeEnumerable`
  counting style.
- **Test scenarios:** valid, failing, empty, source fault after failure, disposal exceptions.
  `(happy path)`, `(edge)`, `(error path)`.
- **Verification:** The comparison runs in the test suite and in the measurement executable.

### U4. Pin semantics with the xUnit suite

- **Goal:** Add focused tests for every contract edge.
- **Requirements:** R1-R9.
- **Dependencies:** U1-U3.
- **Files:** `tests/FunnySharp.Tests/FallibleReductionTests.cs`,
  `FallibleReductionAsyncTests.cs`, `FallibleReductionComparisonTests.cs`,
  `tests/FunnySharp.Tests/FunnySharp.Tests.csproj`.
- **Approach:** Attribute-only `[Fact]`/`[Theory]`; classic asserts; no skips. Use the shared probes
  for consumption/disposal and local sources for exception and identity cases. Deliberate
  uninitialized results use `#pragma warning disable FS1001` around the witness.
- **Execution note:** Tests must not sleep or assert timing; pending async steps use a
  `TaskCompletionSource`/`ValueTaskSource` and are completed explicitly.
- **Patterns to follow:** `tests/FunnySharp.Tests/AGENTS.md`, `CheckAsyncTests` cancellation
  patterns, `CountingValueTaskSource` for single consumption.
- **Test scenarios:** AE1-AE7 plus null arguments, disposal precedence, token identity, repeated
  enumeration, and null payloads. `(happy path)`, `(edge)`, `(error path)`, `(integration)`.
- **Verification:** Focused run reports zero failures and zero skipped.

### U5. Add the compiling example

- **Goal:** Show parsing and checked accumulation over an ordinary enumerable and an async source.
- **Requirements:** R11.
- **Dependencies:** U1, U2.
- **Files:** `examples/FunnySharp.Examples/FallibleReductionExamples.cs`,
  `examples/FunnySharp.Examples/Program.cs`.
- **Approach:** Fold order lines into an immutable total, show the first failure, show the empty
  seed, show a mutable accumulator surviving a failure, and show both async-source forms. Assert
  every outcome with the existing example assertion helper.
- **Execution note:** The example project must build with zero `FS####` diagnostics; never discard a
  `Result`.
- **Patterns to follow:** `CheckExamples` structure and `Program.cs` registration.
- **Test scenarios:** Example runs to the exact `FunnySharp examples passed.` marker.
  `(integration)`.
- **Verification:** `dotnet run --project examples/FunnySharp.Examples -c Release`.

### U6. Add the measurement harness and record evidence

- **Goal:** Measure allocation, copying, and buffering for realistic accumulators, and publish the
  consumption comparison.
- **Requirements:** R10, R11.
- **Dependencies:** U3-U5.
- **Files:** `tests/FunnySharp.FallibleReductionMeasurements/Program.cs`,
  `tests/FunnySharp.FallibleReductionMeasurements/FallibleReductionMeasurements.cs`,
  `tests/FunnySharp.FallibleReductionMeasurements/FunnySharp.FallibleReductionMeasurements.csproj`,
  `tests/FunnySharp.FallibleReductionMeasurements/packages.lock.json`,
  `FunnySharp.slnx`, `docs/fallible-reduction-evidence.md`.
- **Approach:** Run both comparison passes first. Measure sync success/failure for `long`,
  a 64-byte value accumulator, and a mutable list accumulator across 0, 16, and 1024 items; report
  `MoveNext`/step/dispose counts; measure async success/failure for both async forms against the
  await-foreach and `AggregateAsync`-over-Result baselines. Use the shared
  `MeasurementHarness` (`GC.GetAllocatedBytesForCurrentThread` for sync,
  `GC.GetTotalAllocatedBytes(precise: true)` for async).
- **Execution note:** Measurements are explicit-request evidence, not a CI gate. Do not add a
  BenchmarkDotNet scenario or numeric release budget without a separate goal.
- **Patterns to follow:** `tests/FunnySharp.CheckMeasurements` project shape and evidence table.
- **Test scenarios:** AE8; the comparison passes before measurement output. `(integration)`.
- **Verification:** Run the measurement executable in Release and copy the real table into the
  evidence record.

### U7. Reconcile docs, baseline, manifests, and goal state

- **Goal:** Ship coherent documentation and committed surface records.
- **Requirements:** R11, R12.
- **Dependencies:** U1-U6.
- **Files:** `docs/fallible-reduction.md`, `docs/grammar.md`, `docs/product-contract.md`,
  `docs/result.md`, `docs/data-pipelines.md`, `docs/stability-inventory.md`, `docs/AGENTS.md`,
  `docs/plans/AGENTS.md`, `docs/goals/README.md`, `docs/goals/AGENTS.md`,
  `docs/goals/archive/0033-goal.md`, `AGENTS.md`, `README.md`, `README_zh-cn.md`,
  `src/FunnySharp/AGENTS.md`, `tests/FunnySharp.Tests/AGENTS.md`,
  `examples/FunnySharp.Examples/AGENTS.md`, `eng/api-baseline/FunnySharp.public-api.txt`,
  `eng/performance/baseline.json`, `eng/performance/competitor-baseline.json`.
- **Approach:** Guide body starts with `## API Shape`, covers semantics/costs, and closes with
  `## Deliberate Boundaries`. Grammar adds one `AggregateResult` row. Product contract gains one
  paragraph. Baseline is regenerated with `verify-api-baseline --write`. Performance manifests add
  the two new source paths in ordinal order. The completed goal moves to the archive index.
- **Execution note:** Do not hand-edit generated inventory or archived goal contents; the archive
  move is a rename of the unchanged contract. Regenerate the API baseline only after the public
  surface is final.
- **Patterns to follow:** Goal 0032 landing shape (`checks.md`, `check-evidence.md`, README/doc
  indexes, baseline, manifests).
- **Test scenarios:** API baseline, docs snippets, format, manifest tests, example run.
  `(integration)`.
- **Verification:** `verify-api-baseline`, `verify-docs-snippets`, format gate, full test gate.

## Verification Contract

| Gate | Command | Applies to | Completion evidence |
| --- | --- | --- | --- |
| Build | `dotnet fsi build.fsx -- -p build` | U1-U7 | exit 0, zero warnings/errors |
| Test | `dotnet fsi build.fsx -- -p test` | U1-U7 | all tests passed, zero failed, zero skipped |
| Format | `dotnet fsi build.fsx -- -p format` | U1-U7 | exit 0 |
| Docs snippets | `dotnet fsi build.fsx -- -p verify-docs-snippets` | U7 | exit 0 |
| API baseline | `dotnet fsi build.fsx -- -p verify-api-baseline` (after `--write`) | U7 | exit 0 with only the new members |
| Example | `dotnet run --project examples/FunnySharp.Examples -c Release` | U5 | exact `FunnySharp examples passed.` |
| Comparison | `dotnet run --project tests/FunnySharp.FallibleReductionMeasurements/... -c Release` | U3, U6 | comparison passes; consumption table and allocation/timing table captured |
| Manifest shape | full test suite (`PerformanceManifestTests`) | U7 | source lists sorted and complete |

## Definition of Done

### Per-Unit Completion

| Unit | Done when |
| --- | --- |
| U1 | Empty, success-order, first-failure cut-off, null payload, mutable accumulator, source/step fault, uninitialized-step, and argument-validation tests pass for `AggregateResult`. |
| U2 | The async forms match the reference consumer on outcome, consumption, disposal, token forwarding, cancellation, fault, and single-`ValueTask` observation. |
| U3 | The comparison proves outcome parity and shows `Aggregate` over `Result` making more `MoveNext` calls after a typed failure, including the source-fault-after-failure observable difference. |
| U4 | Focused tests pass with zero failed and zero skipped, and the full suite remains green. |
| U5 | The example proves sync and async folds, empty seed identity, first failure, and mutable-accumulator no-rollback; the program prints the exact success marker. |
| U6 | The measurement executable runs both comparison passes and prints real allocation/timing/consumption tables copied into the evidence record. |
| U7 | The guide closes with `## Deliberate Boundaries`; product contract, grammar, stability inventory, API baseline, performance manifests, README indexes, and goal archive agree. |

### Global Completion

- R1-R12 each map to an implementation artifact and an executed verification result.
- AE1-AE8 pass in the test suite or the evidence run.
- No new carrier, stream hierarchy, async wrapper, parallel reduction, rollback, retry, or
  exception conversion is introduced.
- The evidence discloses allocation, copying, buffering, and the actual upstream short-circuit
  difference rather than claiming a universal speedup.
- The maintainer has the comparison, measurement table, and residual-risk list for judgment.
