---
title: Bounded concurrency with per-key ordering
type: feat
date: 2026-11-08
artifact_contract: ce-unified-plan/v1
product_contract_source: docs/goals/archive/0038-goal.md, docs/product-contract.md, docs/grammar.md
execution: code
---

# Bounded Concurrency With Per-Key Ordering Plan

## Goal Capsule

- **Objective:** Let an asynchronous consumer process independent business entities concurrently
  while guaranteeing that a later operation for one key never starts before its predecessor for
  that key has actually completed. The consumer observes every operation's typed `Result` outcome
  and the key and source item that produced it; a typed failure is data, not a lost background
  failure. Admitted work, pending per-key buffering, and retained key state are explicitly bounded.
  Cancellation, source failure, callback failure, early disposal, and a non-cooperative callback
  have honest, documented cleanup semantics.
- **Means:** One new deferred extension over `IAsyncEnumerable<TSource>`, implemented by a
  coordinator that keeps at most `maxConcurrency` started operations, at most
  `maxBufferedItems` source items waiting to start, one active operation per key, and FIFO
  fairness among keys whose head item is ready. Keys are compared by an explicit
  `IEqualityComparer<TKey>` or `EqualityComparer<TKey>.Default`; null keys are programming errors.
  Results are delivered as `(TKey Key, TSource Source, Result<TResult, TError> Outcome)` in
  operation-completion order. The callback is a standard `ValueTask<Result<...>>` delegate; no new
  outcome carrier, scheduler, actor runtime, retry, or delivery guarantee is added.
- **Authority:** [Goal 0038](../goals/archive/0038-goal.md) is the completion contract.
  [Product contract](../product-contract.md), [grammar](../grammar.md), and
  [core design](../core-design.md) are the current boundaries. Goal 0037's domain programs and
  simulation are complete and unrelated; no Goal 0039 work starts here.
- **Execution profile:** Implement in `src/FunnySharp/ParallelAsyncEnumerableExtensions.cs` (or a
  sibling file with the same public type, decided by size); pin behavior in
  `tests/FunnySharp.Tests`; share a complete account-event consumer comparison in
  `tests/Shared/KeyedConcurrencyComparison`; measure through
  `tests/FunnySharp.KeyedConcurrencyMeasurements`; exercise through
  `examples/FunnySharp.Examples`; document in `docs/concurrency.md`; record evidence in
  `docs/keyed-concurrency-evidence.md`; update API baseline, performance manifests, solution,
  grammar, product contract, stability inventory, and goal index/archive.
- **Tail ownership:** The maintainer judges whether the stronger per-key ordering guarantee and
  the reduced consumer code justify the coordinator's scheduling and allocation overhead against a
  competent `Channel`/`Task`-chaining BCL implementation. If the library is slower or allocates
  more, the evidence says so and does not redefine the baseline to manufacture a win.

## Product Contract

### Public API

```csharp
public static IAsyncEnumerable<(TKey Key, TSource Source, Result<TResult, TError> Outcome)>
    SelectParallelByKeyCompletionOrderValueAsync<TSource, TKey, TResult, TError>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        int maxConcurrency,
        int maxBufferedItems,
        Func<TSource, ValueTask<Result<TResult, TError>>> selector,
        IEqualityComparer<TKey>? comparer = null);

public static IAsyncEnumerable<(TKey Key, TSource Source, Result<TResult, TError> Outcome)>
    SelectParallelByKeyCompletionOrderValueAsync<TSource, TKey, TResult, TError>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        int maxConcurrency,
        int maxBufferedItems,
        Func<TSource, CancellationToken, ValueTask<Result<TResult, TError>>> selector,
        IEqualityComparer<TKey>? comparer = null);
```

The delivery order is part of the method name, matching the existing grammar rule. The method
yields completion order, not global source order. The element carries enough context for the
consumer to correlate a result with its key and original item without retaining a side table.

### Semantics

- **Deferred construction.** Calling the method validates arguments and captures delegates but does
  not enumerate the source, invoke `keySelector`, or invoke `selector`. Each enumeration creates a
  fresh linked operation token and a fresh coordinator.
- **Key equality.** `comparer` is used for every key lookup; when it is `null`,
  `EqualityComparer<TKey>.Default` is used. The comparer is never inferred from a source or
  callback object. The comparer is invoked for admission and for state removal only.
- **Null keys.** A `null` returned by `keySelector` is a programming error, not a typed outcome.
  It stops admission, cancels and drains started operations, disposes the source enumerator, and
  propagates `ArgumentNullException` naming `keySelector`. No `Some(null)`/`Option` conversion is
  introduced.
- **One active operation per key.** The coordinator starts at most one selector call for a key at a
  time. After that operation completes, the key's next pending item may start. "Completes" means
  the returned `ValueTask` has completed and its `Result` has been inspected; it does not mean the
  consumer has read the outcome. A typed failure is a completed outcome: the next item for that
  key is still eligible to start after the failure is delivered.
- **Result order.** Within one key, outcomes are yielded in source order. Across keys, an outcome is
  yielded when its operation completes; relative order between concurrently completing operations
  is the order the coordinator observes, which is not specified. Global source order is never
  promised. A source-order variant is not added.
- **Typed results and failures.** The callback returns `Result<TResult, TError>`. A `Success` or
  `Failure` is yielded unchanged in `Outcome`. A typed failure does not stop admission, cancel
  sibling keys, or become an exception. The caller decides whether a failed account should
  receive later events. An uninitialized `Result` (the `default` value) is a programming fault:
  observing it is a terminal `InvalidOperationException`, because it is not a typed domain outcome.
- **Unexpected callback exceptions.** A thrown exception from the callback (including a faulted
  returned task) is a terminal operation failure. Admission stops, the linked operation token is
  canceled, started operations are drained, the source enumerator is disposed, and the fault is
  propagated with normal .NET exception identity. Multiple independent faults are aggregated;
  cleanup failures follow the primary failure. A fault is never converted into a typed `Result`
  failure.
- **Bounds.**
  - `maxConcurrency` is the maximum number of started but not-yet-delivered operations. A
    completed operation continues to occupy its slot until the consumer observes its outcome.
    This includes operations whose outcome is buffered in the delivery channel.
  - `maxBufferedItems` is the maximum number of source items read but not yet started. It must be
    at least one; at least one lookahead slot exists because a source item's key is known only
    after the item is read.
  - Therefore, at any time the coordinator retains at most
    `maxConcurrency + maxBufferedItems` source items: started/undelivered operations, pending
    per-key items, and completed outcomes waiting for the consumer. Dictionary entries and
    per-key queues exist only for keys that own one of those retained items; an inactive key's
    state is removed when its last pending item starts and its active operation is delivered.
  - Source reading blocks when the pending buffer is full. It cannot run ahead of the declared
    bound even when the active slots are free. This is deliberate backpressure, not an unbounded
    source prefix.
- **Hot-key behavior.** A hot key has at most one active operation. Its later items accumulate in
  its per-key FIFO until the buffer bound is reached. When a key becomes ready, it joins the tail
  of the ready-key FIFO; a hot key does not stay at the head forever, so other ready keys get
  turns. With `maxConcurrency: 1`, a hot key nevertheless occupies the only active slot for as
  long as its queue remains and other keys are behind it in the source; this is disclosed, not
  hidden.
- **Fairness.** Ready keys start in FIFO order of becoming ready, with the completing key
  re-enqueued at the tail. No strict round-robin, priority, or latency guarantee is claimed.
  Fairness is bounded by the source's order and by which keys are visible within the buffer.
- **Partition-induced blocking.** A hot key can fill the `maxBufferedItems` pending slots with its
  own later items. The producer then stops reading, so a different key later in the source cannot
  start even if an active slot is free. Likewise, a slow consumer keeps completed outcomes in
  active slots, so no new key is admitted. These are consequences of the declared bounds and the
  input order; they are documented explicitly rather than described as global parallelism.
- **Source backpressure.** The producer acquires one pending-buffer permit before each source
  `MoveNextAsync`. An item that starts immediately releases the permit; an item queued for a busy
  key holds it until it starts. When the buffer is full, the producer awaits capacity; it does not
  read ahead. This is the exact mechanism that prevents unbounded key cardinality from retaining
  unbounded queues.
- **Cancellation.** The enumeration token is linked into one operation token. The linked token is
  passed to the source enumerator and every selector call. Consumer cancellation is observed by
  pending delivery waits and by the producer; the coordinator cancels the linked operation,
  stops admission, drains started work, observes its faults, disposes the source, and rethrows an
  `OperationCanceledException` carrying the caller's token. Cancellation is never converted into a
  typed `Result` failure.
- **Source failure.** A source `MoveNextAsync` or source disposal exception is terminal. Already
  delivered outcomes remain observed; outcomes already buffered in the channel may still be
  yielded before the terminal exception; pending unstarted items are dropped without callbacks.
  Started work is canceled and drained, then the fault propagates.
- **Callback failure.** A thrown or faulted callback is terminal as described above. Already
  buffered outcomes may be yielded before the exception. Pending items that never started are
  dropped. A callback that ignores the operation token is awaited during cleanup; disposal may
  therefore wait indefinitely. No BEAM-style forced termination, thread abort, or signal kill is
  promised.
- **Early consumer disposal.** `DisposeAsync` stops admission, cancels the linked token, drops
  pending unstarted items, awaits all started callbacks and their observers, observes faults,
  disposes the source enumerator exactly once, and only then completes. A callback that ignores
  cancellation makes disposal wait for it. A `MoveNextAsync` after disposal throws
  `ObjectDisposedException`; concurrent `MoveNextAsync` calls are rejected with
  `InvalidOperationException`.
- **Key-state release.** A key entry is removed as soon as it has no active operation and no
  pending item. No cache is retained between enumerations and no idle key state survives the
  operation that created it. Keys are not evicted by a capacity policy because the source-item
  bound already bounds every live key entry; a key with no retained item has no entry.
- **No added semantics.** There is no implicit retry, backoff, circuit breaker, process supervision,
  distributed ownership, exactly-once delivery, durable queue, priority, or second execution
  runtime. The coordinator only schedules caller-supplied delegates.

### API spelling decisions (options + recommendation)

| Decision | Options | Recommendation and reason |
| --- | --- | --- |
| Public shape | (a) Extension method over `IAsyncEnumerable<T>`; (b) a consumer class with explicit lifecycle. | **(a)** extension. It matches the existing streaming concurrency surface, composes with `await foreach`, and does not invent a second lifetime model. |
| Delivery order | (a) Completion order; (b) global source order with reorder buffering; (c) both as separate named methods. | **(a)** completion order. It preserves the useful per-key guarantee without a global reorder buffer or a slow-key head-of-line block. (b) would need to hold completed outcomes for arbitrary earlier keys and would make the name/contract larger; (c) doubles the surface without evidence that source order is needed. |
| Failure policy | (a) Typed failure continues the key and the stream; (b) typed failure stops the key; (c) typed failure stops the whole stream. | **(a)**. The goal says the consumer observes failures rather than losing them. Whether an account continues is caller policy; the library does not infer poisoning. Unexpected exceptions remain terminal, matching ordinary .NET. |
| Buffer bound | (a) Exact pending-item bound, one permit per unstarted item; (b) total-retained bound with pending only implicit; (c) per-key queue depth. | **(a)**. It is the strongest simple contract: source reads stop at exactly `maxBufferedItems` unstarted items, and the total retained bound follows. (b) makes the word "buffering" misleading; (c) multiplies a bound by unbounded key cardinality. |
| Null keys | (a) Terminal `ArgumentNullException`; (b) a typed structural failure item; (c) allow null under a caller comparer. | **(a)**. A null key is a key-selector bug, not an account outcome. (c) conflicts with `Dictionary<TKey,...>` and with the comparer policy; (b) would need a second output shape for a condition the caller can prevent. |
| Comparer | (a) Optional `IEqualityComparer<TKey>?` parameter; (b) two overloads; (c) infer from an interface. | **(a)**. It matches the existing keyed-join surface, keeps one method per callback shape, and `null` means the documented default. Inference is rejected because no dictionary interface proves its comparer. |
| Output element | (a) `(Key, Source, Result)` tuple; (b) a new public record; (c) `Result<(Key, Source, Value), TError>`. | **(a)**. It uses BCL shapes, keeps key and source correlation explicit, and avoids a new public type or carrier. A typed failure remains a `Result` value inside the tuple. |

## Planning Contract

### Requirements

- **R1:** Add the two async extension overloads with complete XML documentation, argument
  validation at construction, and deferred source/key/callback execution. `maxConcurrency` and
  `maxBufferedItems` must each be at least one. `comparer` null means
  `EqualityComparer<TKey>.Default`.
- **R2:** The coordinator starts at most `maxConcurrency` operations and retains at most
  `maxBufferedItems` unstarted source items. A source item is read only after a buffer permit is
  acquired. A started item releases its permit; a queued item holds it until started.
- **R3:** Per-key serialization is exact: no selector for a key starts while a previous selector
  for the same key is incomplete, including when the predecessor returns a typed failure. A later
  same-key item starts only after the predecessor's returned `ValueTask` has completed and its
  `Result` has been inspected.
- **R4:** Outcomes are delivered in completion order across keys and in source order within a key.
  The tuple carries the exact key value observed for the source item, the exact source item, and
  the exact `Result` returned by the selector.
- **R5:** Typed `Success` and `Failure` outcomes are yielded, never converted to exceptions and
  never discarded. An uninitialized `Result` is a terminal programming fault.
- **R6:** A callback exception is terminal: stop admission, cancel the linked token, drain started
  work, dispose the source once, preserve the first fault and aggregate independent cleanup
  faults. A callback that ignores the token is awaited, not abandoned.
- **R7:** A source exception is terminal with the same cleanup. Pending unstarted items are
  dropped without selector calls; already delivered and already buffered outcomes remain
  observable according to channel order.
- **R8:** Consumer cancellation preserves the caller's token, stops admission, cancels and drains
  started work, disposes the source once, and never reports caller cancellation as a domain
  failure.
- **R9:** Early disposal performs the same cleanup; pending unstarted items are dropped; every
  started callback is awaited; a non-cooperative callback blocks disposal. No forced termination
  is claimed.
- **R10:** Key state is removed when its queue and active operation are both gone. Dictionary
  state is bounded by retained items; a large or unbounded source key cardinality cannot create
  unbounded retained queues or state. A hot key can occupy the whole pending buffer and delay
  source reading for other keys; this is exposed in docs and evidence.
- **R11:** Ready-key fairness is FIFO with the completing key re-enqueued at the tail. With one
  active slot a hot key can still dominate until its queue drains; this is documented.
- **R12:** The enumerator rejects concurrent `MoveNextAsync` calls and rejects `MoveNextAsync`
  after disposal. `DisposeAsync` is idempotent. Each enumeration owns a separate linked token,
  source enumerator, coordinator state, and delivery channel.
- **R13:** Add deterministic xUnit v3 tests using `TaskCompletionSource` gates and controlled
  source enumerators. No wall-clock sleep is correctness evidence. Cover per-key overlap,
  start/completion order, typed failure continuation, uninitialized results, callback/source
  faults, null keys, comparer behavior, bounds from a skewed source, backpressure, fairness,
  cancellation, disposal, non-cooperative callbacks, concurrent `MoveNextAsync`, and state
  release.
- **R14:** Add a compiling example and a byte-exact `documentation-sample` region in the existing
  `docs/concurrency.md`; the guide closes with `## Deliberate Boundaries` covering every
  non-promise in this plan.
- **R15:** Add a shareable account-event comparison: the full FunnySharp consumer, a competent
  BCL `Task`/`Channel` implementation with the same per-key guarantee and bound, and an existing
  .NET-facility baseline (`Parallel.ForEachAsync` with per-key gating, explicitly marked as a
  different guarantee). Assert fixed expected results, order, typed failures, in-flight bounds,
  and cleanup on a deterministic scenario. Report honest consumer line counts and measured
  allocation/timing.
- **R16:** Add a measurement executable that runs the same work through both complete consumers
  twice, with one allocation counter and one JIT tier, and records per-key overlap, start and
  completion order, maximum in-flight work, and retained source items under a skewed input.
- **R17:** Update `eng/api-baseline/FunnySharp.public-api.txt`, the `benchmarkInput.files` lists in
  `eng/performance/baseline.json` and `eng/performance/competitor-baseline.json`,
  `FunnySharp.slnx`, `AGENTS.md`-adjacent knowledge where counts change, grammar, product
  contract, stability inventory, goals index, and archive goal 0038 unchanged.
- **R18:** Run the required gates and record actual counts: build 0/0, test 0 failed/0 skipped,
  format, docs snippets, API baseline, examples run, and the comparison/measurement harness twice.

### Non-goals

- No source-order delivery variant, no `maxBufferedItems` per key, no priority or weighted
  fairness, no dynamic reconfiguration during enumeration.
- No implicit retry, backoff, circuit breaker, timeout policy, or cancellation of non-cooperative
  work. No thread abort, `Task` termination, or BEAM-style supervision.
- No distributed coordination, durable queue, exactly-once delivery, persistence, actor runtime,
  mailbox, scheduler, or second general execution runtime.
- No inference of key equality from a dictionary or comparable, no null-key special case, no
  `Maybe`/`Either`/async outcome wrapper.
- No claim that different keys always make progress concurrently: source order, buffer capacity,
  the active bound, and hot-key behavior can delay them.
- No change to Goal 0037's domain programs or to Goal 0039's research direction.

## Execution Phases

1. **API and coordinator.** Add the extension methods and internal enumerator in
   `src/FunnySharp/ParallelAsyncEnumerableExtensions.cs` (a sibling file is acceptable if the
   file becomes unwieldy). Implement the buffer permits, per-key FIFO, ready-key FIFO, delivery
   channel, completion observation, terminal-fault aggregation, and cleanup exactly as above.
2. **Deterministic tests.** Add `KeyedConcurrencyTests.cs` with controlled sources and gates.
   Use `TaskCompletionSource` with `RunContinuationsAsynchronously`; wait with bounded test
   timeouts; never sleep for correctness. Re-run the suite repeatedly to catch flakes.
3. **Example and guide.** Add an account-event example that demonstrates per-account serialization
   and cross-account overlap; add the documentation sample region; extend `docs/concurrency.md`
   with the operation, bounds, ordering, fairness, cleanup, and a closing
   `## Deliberate Boundaries`.
4. **Comparison and measurement.** Add `tests/Shared/KeyedConcurrencyComparison` with the shared
   account model, account script, FunnySharp consumer, competent BCL implementation, existing
   facility baseline, and verifier. Add `tests/FunnySharp.KeyedConcurrencyMeasurements` and its
   solution entry. Run the verifier in xUnit and the measurement executable twice.
5. **Reconcile and close.** Update the API baseline with `--write`, performance manifest file
   lists, grammar/product contract/stability inventory/goals index, move the goal unchanged to
   `docs/goals/archive/0038-goal.md`, and add the evidence record.
6. **Gate and report.** Run build, test, format, docs snippets, API baseline, examples, and the
   measurement harness; collect real counts and honest numbers.

## Evidence Plan

- **Correctness evidence:** deterministic tests listed in R13, including a skewed 10k-key source
  that proves source reading stops at the declared retained bound, a hot-key FIFO fairness case,
  and a non-cooperative callback disposal case. The comparison verifier asserts both consumers
  produce the same typed outcomes and per-key order for a fixed account script.
- **Consumer-code evidence:** raw line counts for the complete FunnySharp consumer, the competent
  BCL implementation, and the existing-facility implementation, with shared models/scripts not
  charged to either side. No LOC target is claimed.
- **Scheduling evidence:** per-key overlap (maximum and per-key start sequences), start and
  completion order, maximum in-flight operations, and maximum retained source items under a
  skewed input. These are measured by instrumented callbacks/sources, not inferred.
- **Performance evidence:** one allocation counter (`GC.GetTotalAllocatedBytes(precise: true)`),
  one JIT tier (`TieredCompilation=false`), two complete runs of the comparison harness. Report
  the honest direction. State that scheduling overhead is the coordinator's admission/delivery
  work and that algorithmic work is the same callback work charged to both sides. Do not claim a
  speedup without measured evidence.
- **Baseline discipline:** the competent BCL baseline uses the same per-key guarantee, the same
  typed `Result` carrier, the same source script, the same selector work, the same bound, and the
  same full consumer loop. A `Parallel.ForEachAsync` baseline is reported separately as an
  existing facility with a different (weaker) per-key-order guarantee, not as an equivalent.
- **Residuals:** if the library allocates more, attribute it to the coordinator, per-item tuples,
  channel, observer, and cleanup state; if it allocates less, do not hide baseline buffering.
  Hosted timing remains directional; allocation is the blocking comparison.

## Verification

- `dotnet fsi build.fsx -- -p build` → 0 warnings, 0 errors.
- `dotnet fsi build.fsx -- -p test` → 0 failed, 0 skipped; repeat the keyed tests to check
  determinism.
- `dotnet fsi build.fsx -- -p format`.
- `dotnet fsi build.fsx -- -p verify-docs-snippets`.
- `dotnet fsi build.fsx -- -p verify-api-baseline` (write the baseline deliberately first).
- `dotnet run --project examples/FunnySharp.Examples/FunnySharp.Examples.csproj -c Release`.
- `dotnet run --project tests/FunnySharp.KeyedConcurrencyMeasurements/FunnySharp.KeyedConcurrencyMeasurements.csproj -c Release`
  twice; compare allocation determinism and record timing as directional.
- `git diff --stat` and `git status` for the final landed-file report; no commit or push.

## Risks And Open Questions

- **Hot-key starvation under `maxConcurrency: 1`.** Disclosed, not fixed by a hidden larger bound.
  A future goal can add a weighted policy only if a consumer needs it and the grammar can express
  it without a mode parameter.
- **Non-cooperative callbacks.** Disposal can wait forever. This is the honest .NET behavior; the
  alternative would be forced termination, which the goal forbids.
- **Completion-order ambiguity.** Concurrent completions have no specified relative order. Tests
  release one gate at a time; the docs state the limit.
- **Buffering semantics.** `maxBufferedItems` counts unstarted items, not completed outcomes
  awaiting delivery. The total retained bound is `maxConcurrency + maxBufferedItems`; the evidence
  measures source items held, not just callbacks started.
- **Baseline equivalence.** A BCL baseline that uses `Task` chaining may allocate differently from
  a queue coordinator. The comparison charges the same complete consumer loop to both sides and
  reports the residual; it does not claim algorithm identity.
- **API spelling.** The method name is long but encodes both the by-key guarantee and the delivery
  order, following the existing "order in the name" rule. A shorter name would hide either
  ordering or concurrency from the call site.
