# Bounded Per-Key Concurrency Consumer Evidence

This record supports the maintainer's judgment for
[Goal 0038](goals/archive/0038-goal.md): whether an asynchronous consumer can process independent
business entities concurrently while executing operations for the same key in the order observed
from the input sequence, with explicit bounds on admitted work and buffering, delivered typed
outcomes, and honest cleanup. It compares the complete FunnySharp consumer with a competent BCL
`Task`/`Channel` implementation and with an existing `Parallel.ForEachAsync` facility baseline.
Source identity is this Goal 0038 working tree on top of commit `a5c2545`; the landing commit
becomes the durable identity.

Measurements were taken on this host:

- Ubuntu 24.04.5 LTS, Linux 7.0.0-1017-azure x86_64.
- .NET SDK 10.0.401, runtime 10.0.12 x64; Release build; `TieredCompilation=false`.
- Date: 2026-11-08 UTC.

Allocation is the blocking comparison; timing is directional on this shared host.

## Compared Consumers

The consumers run the same account-event script, the same per-account ledger rule, the same
business operation, the same `Result<decimal, AccountError>` carrier, the same admission/buffer
bounds, and the same complete consumption loop. Shared models, script, ledger, and assertions are
not charged to either side.

| Version | File | Raw lines | Shape |
| --- | --- | ---: | --- |
| FunnySharp consumer | [FunnySharpKeyedConsumer.cs](../tests/Shared/KeyedConcurrencyComparison/FunnySharpKeyedConsumer.cs) | **41** | Wraps the source in a counting enumerable; calls `SelectParallelByKeyCompletionOrderValueAsync`; collects `(key, event, Result)` outcomes and reports metrics. All admission, per-key FIFO, fairness, delivery, and cleanup behavior is library code. |
| Competent BCL `Task`/`Channel` | [BclKeyedConsumer.cs](../tests/Shared/KeyedConcurrencyComparison/BclKeyedConsumer.cs) | **330** | Explicit linked cancellation, a pending-item semaphore, a started-but-not-delivered active semaphore released by the consumer on delivery, a per-key task-chain dictionary with release, a bounded output channel, per-key delivery chained inside the work task, fault tracking, and cleanup/drain. |
| Existing `Parallel.ForEachAsync` facility | [ParallelFacilityKeyedConsumer.cs](../tests/Shared/KeyedConcurrencyComparison/ParallelFacilityKeyedConsumer.cs) | **65** | `Parallel.ForEachAsync` with a per-key `SemaphoreSlim`. It bounds global worker count and avoids same-key overlap in the measured scripts, but it does not guarantee per-key *start* order and its per-key semaphore dictionary is not bounded. It is not an equivalent implementation of the FunnySharp contract. |

Shared definitions are [KeyedConcurrencyModels.cs](../tests/Shared/KeyedConcurrencyComparison/KeyedConcurrencyModels.cs)
(318 lines: models, processor transitions, ledger, counting source, scripts, expected report) and
[KeyedComparison.cs](../tests/Shared/KeyedConcurrencyComparison/KeyedComparison.cs) (179 lines:
parity and fixed-expectation verifier). They are not charged to either side. The shipped
implementation is
[KeyedParallelAsyncEnumerableExtensions.cs](../src/FunnySharp/KeyedParallelAsyncEnumerableExtensions.cs)
(1,049 lines, including XML documentation and cleanup). The complete FunnySharp consumer is
**289 lines smaller** than the competent BCL consumer (41 versus 330). That is a consumer-code
result, not a claim that the library is small: the library carries the coordination and cleanup
that both consumers otherwise repeat.

## Equivalence

`KeyedComparisonVerifier.VerifyAsync` runs the fixed 18-event, six-account script through the
FunnySharp and competent BCL consumers and asserts:

- the same 18 typed outcomes, in source order within each account, including two
  `insufficient-funds` failures at the fixed account/sequence positions;
- the same final balance for every account (`ACC-1` 75, `ACC-2` 30, `ACC-3` 30, `ACC-4` 25,
  `ACC-5` 1, `ACC-6` 0);
- per-key parity between the two consumers and with a repeat run of each;
- maximum in-flight work at or below `maxConcurrency`;
- zero same-key overlap violations;
- the source-side retained high-water at or below the exact
  `maxConcurrency + maxBufferedItems` for the BCL baseline, and at or below that bound plus the
  one-item FunnySharp delivery handoff explained under "Bounds" below.

The measurement harness repeats equivalent-consumer verification for the fixed, skewed, and
interleaved scripts: FunnySharp and the competent BCL consumer must produce the same per-key
outcomes, final balances, start order, and completion order, and both must respect the in-flight
bound and the retained-source bound (exact for the baseline, with the one-item FunnySharp handoff).
The facility baseline is verified for event coverage, zero same-key overlap, and the global
in-flight bound; it is not asserted to match per-key order because it provides no start-order
protocol.

The deterministic feature tests in
[KeyedConcurrencyTests.cs](../tests/FunnySharp.Tests/KeyedConcurrencyTests.cs) pin the boundaries
the fixed script does not: exact same-key start/completion order, completion order across keys,
typed-failure continuation, uninitialized results, callback/source/key-selector faults, multiple
concurrent callback faults, source-dispose and cancellation-callback faults, buffered-outcome
draining before a terminal fault, null keys, equivalent-key comparers, exact source-read stopping,
key-state release, hot-key pending-buffer blocking, ready-key fairness, cancellation, early
disposal, concurrent disposal idempotence, non-cooperative callbacks, concurrent `MoveNextAsync`,
disposal before first pull, malformed `ValueTask` start failure, and dispose-once source cleanup.

## Semantic Behavior Ledger

| Contract | Evidence |
| --- | --- |
| One active operation per key | `SameKeyNeverOverlapsAndStartsAfterThePredecessorCompletes`, `TypedFailureIsDeliveredAndTheNextSameKeyOperationStillRuns`, fixed script ledger parity, zero overlap violations in both equivalent consumers. |
| Later same-key start waits for actual completion | Gated tests assert the predecessor's completion before the successor's start; the successor starts after the predecessor's `ValueTask` completes and its `Result` is inspected. |
| Completion order across keys | `OutcomesArriveInCompletionOrderAcrossDifferentKeys` releases the later-started key first and observes it first. Concurrent ties are unspecified. |
| Typed failure is data | The fixed script's `ACC-2` sequence 2 and `ACC-3` sequence 0 are `invalid` outcomes; later events for those accounts still run. |
| Explicit equality and null keys | `ExplicitComparerSerializesEquivalentKeysAndPreservesTheObservedKey` uses `OrdinalIgnoreCase`, serializes `A` and `a`, and yields the exact observed key. `NullKeyIsATerminalKeySelectorFault` reports `ArgumentNullException` with `ParamName == "keySelector"` and never invokes the operation. |
| Bounded source retention | `UnboundedKeyCardinalityRetainsOnlyTheDeclaredWindow` uses a 10,000-key source with `maxConcurrency=2`, `maxBufferedItems=2`; while two operations are blocked, the source has yielded exactly four items and no fifth read is attempted. The same test asserts four live key-state entries at the bound and zero after the stream drains, so a leaked dictionary entry fails the test. |
| Hot-key partition blocking | `HotKeyFillsThePendingBufferAndBlocksLaterKeys` pins the mechanism with gates: one active hot key plus two pending hot items stop source reading before a later cold key is observed; starting the active item admits the next pending item and only then reads the next source item. The skewed measurement table records the observed scheduler-dependent in-flight values separately. |
| Fairness among ready keys | `ReadyKeysAreStartedFairlyAfterAnActiveHotKeyCompletes` with one slot shows the hot key's next item re-queues behind the already-ready cold key. |
| Callback/source/key-selector faults | Fault tests assert terminal propagation, exact exception identity, canceled/drained siblings, one source disposal, aggregation of two independent callback faults, source-dispose failure, source-fault plus dispose-fault aggregation, and cancellation-callback failure aggregation. |
| Caller cancellation | `ConsumerCancellationPreservesTheCallerTokenAndWaitsForCleanup` asserts the caller token is preserved after started work drains. |
| Early disposal | `EarlyDisposalDropsBufferedItemsAndWaitsForStartedWork` asserts buffered items never start, started work is awaited, source disposal occurs once, and `MoveNextAsync` after disposal throws. `ConcurrentDisposeCallsShareTheSameCleanup` shows a second concurrent `DisposeAsync` awaits the same cleanup rather than returning early. |
| Buffered outcomes before a terminal fault | `BufferedSuccessIsYieldedBeforeTheTerminalCallbackFault` completes a sibling success before a third callback faults, then observes the buffered success before the terminal exception. |
| Non-cooperative callback | `NonCooperativeCallbackIsAwaitedByCancellation` and the early-disposal test show cancellation/disposal waiting on a callback that ignores the token; no forced termination is claimed. |
| Rejected calls | `RejectsInvalidArgumentsEagerly`, `ConcurrentMoveNextCallsAreRejected`, and `DisposeBeforeFirstPullDoesNotEnumerateTheSource`. |

## Bounds

The coordinator owns two independent limits:

- `maxConcurrency`: started but not yet delivered operations, including completed outcomes still in
  the delivery channel.
- `maxBufferedItems`: source items read but not yet started.

A source item acquires one buffer permit before its `MoveNextAsync`; the permit is released when the
item starts. A hot key can hold the whole pending buffer, so source reading stops even while an
active slot is free. Dictionary and per-key FIFO state exist only while a key owns a started or
buffered item. The retained-source high-water is therefore at most
`maxConcurrency + maxBufferedItems`, and the number of live key entries is at most the number of
retained items.

The deterministic blocked-source test pins the exact quiescent bound: four source reads for
`2 + 2`, with no fifth attempt. The measurement source-side counter is a measurement handoff, not a
second admission limit: the FunnySharp coordinator releases its active slot just before the caller's
delivery counter advances, so a source read can observe one extra retained item during that
handoff. The BCL baseline advances its delivery counter before releasing the active slot, so it
reaches the exact `maxConcurrency + maxBufferedItems` bound. The comparison verifier and the
measurement harness assert the exact bound for the baseline and the one-item handoff allowance for
FunnySharp. The property being measured is "retained source items"; the coordinator's own bound is
`maxConcurrency + maxBufferedItems`. Completed work entries are removed from the coordinator's
in-flight list in the observer's `finally`, so a long stream does not retain one work entry per
item; only started-but-incomplete work remains tracked.

The competent BCL consumer is charged the same two limits, not a third total-retained limit:
`maxBufferedItems` pending permits are released when the chain actually starts its operation, and
`maxConcurrency` active permits are released only when the consumer observes the delivered outcome.
Every retained source item is either pending or active, so
`maxBufferedItems + maxConcurrency` is the complete retained bound. Removing a separate
total-retained semaphore is only sound with that delivery-time release: a variant that released the
active slot when the operation completed instead allowed completed outcomes in the channel and
blocked writers to push source-side retained readings to 16 and 21 on the fixed and interleaved
scheduling runs, above the bound. Those readings were rejected, and the active release remains at
delivery.

## Scheduling Evidence

Measurement runs use one non-measured run of each scenario for scheduling, `maxConcurrency=4`,
`maxBufferedItems=8`. `Fixed` is the 18-event interleaved script; `Skewed` places 120 hot-key events
before 16 cold accounts; `Interleaved` alternates the hot key with cold accounts. The harness records
per-key start and completion sequence in the shared processor and prints both sequences for the
184-event scripts; the table's delivery, start, and completion columns are independent observations,
not derivations from one another.

| Scenario | Consumer | max in-flight (run 1 / run 2) | overlap violations | source-side retained (run 1 / run 2) | delivery order | start order | completion order |
| --- | --- | ---: | ---: | ---: | --- | --- | --- |
| Fixed | FunnySharp | 4 / 4 | 0 / 0 | 12 / 12 | OK / OK | OK / OK | OK / OK |
| Fixed | Competent BCL | 4 / 4 | 0 / 0 | 12 / 12 | OK / OK | OK / OK | OK / OK |
| Fixed | Facility | 4 / 4 | 0 / 0 | 4 / 4 | OK / OK | OK / OK | OK / OK |
| Skewed | FunnySharp | 3 / 2 | 0 / 0 | 13 / 13 | OK / OK | OK / OK | OK / OK |
| Skewed | Competent BCL | 4 / 4 | 0 / 0 | 12 / 12 | OK / OK | OK / OK | OK / OK |
| Skewed | Facility | 3 / 3 | 0 / 0 | 4 / 4 | OK / OK | OK / OK | OK / OK |
| Interleaved | FunnySharp | 4 / 4 | 0 / 0 | 12 / 13 | OK / OK | OK / OK | OK / OK |
| Interleaved | Competent BCL | 4 / 4 | 0 / 0 | 12 / 12 | OK / OK | OK / OK | OK / OK |
| Interleaved | Facility | 3 / 3 | 0 / 0 | 4 / 4 | OK / OK | OK / OK | OK / OK |

The facility happened to record no per-key order violation in these two runs, but a passing
scheduling sample is not an equivalence claim: `Parallel.ForEachAsync` has no start-order protocol,
and its per-key semaphore dictionary is unbounded. An earlier run of the same harness did record a
per-key start, completion, and delivery-order violation on the interleaved script. The equivalent
consumers recorded no violation in either run. That is why equivalence is compared against the
task-chain baseline, not the facility.

The hot-prefix script is the partition-blocking witness in structure: one hot key fills the pending
buffer, so the coordinator cannot read later keys until the hot queue drains. Observed maximum
in-flight work under that prefix varied between 2 and 4 across these runs, depending on scheduler
state; the deterministic `HotKeyFillsThePendingBufferAndBlocksLaterKeys` test pins the mechanism,
and the deterministic blocked-source test pins the source-read bound. The interleaved script shows
distinct keys overlapping when the buffer makes them visible. The maximum in-flight count is
scheduler-sensitive; the gated tests, not this table, are the correctness evidence for overlap and
start order.

## Performance Evidence

The harness executes the complete consumers through one allocation counter
(`GC.GetTotalAllocatedBytes(precise: true)`), one JIT tier, and three internal rounds; the table
reports the best round per row. Each operation crosses one real asynchronous continuation
(`Task.Yield`) in the shared account operation. The same shared report construction (outcomes,
final balances, and start/completion transitions) is charged identically to both equivalent
consumers; it is measurement instrumentation, not consumer-code difference. Two complete runs of
the harness are shown; timing is directional and allocation varies with scheduler state.

Run 1:

| Scenario | FunnySharp | Competent BCL | Facility |
| --- | ---: | ---: | ---: |
| Fixed | 23,424 B/op, 67,625 ns/op | 30,470 B/op, 67,502 ns/op | 18,499 B/op, 34,578 ns/op |
| Skewed | 221,875 B/op, 668,458 ns/op | 259,919 B/op, 657,183 ns/op | 179,315 B/op, 509,592 ns/op |
| Interleaved | 208,068 B/op, 670,417 ns/op | 219,473 B/op, 541,517 ns/op | 162,227 B/op, 461,792 ns/op |

Run 2:

| Scenario | FunnySharp | Competent BCL | Facility |
| --- | ---: | ---: | ---: |
| Fixed | 23,190 B/op, 72,785 ns/op | 30,391 B/op, 68,898 ns/op | 18,119 B/op, 38,670 ns/op |
| Skewed | 218,785 B/op, 707,492 ns/op | 265,255 B/op, 687,742 ns/op | 179,125 B/op, 537,000 ns/op |
| Interleaved | 201,293 B/op, 588,375 ns/op | 218,555 B/op, 504,467 ns/op | 162,313 B/op, 398,575 ns/op |

Honest direction:

- **Consumer code:** FunnySharp is much smaller (41 versus 330 lines). This is the strongest result.
- **Fixed script allocation:** FunnySharp is lower in both runs (23.2–23.4 KB versus 30.4–30.5 KB
  per operation, roughly 23–24%). The coordinator's shorter fixed script does not fill the per-key
  queues, while the task-chain baseline allocates its per-item chain, observer state, and both
  semaphores.
- **Skewed allocation:** FunnySharp is lower in both runs (218.8–221.9 KB versus 259.9–265.3 KB,
  roughly 15–17%). The baseline's active slot stays held until delivery, so its per-key chain tasks
  contend on that semaphore; the comparison charges both sides the same pending-buffer and
  started-but-not-delivered bounds.
- **Interleaved allocation:** FunnySharp is lower in both runs (201.3–208.1 KB versus
  218.6–219.5 KB, roughly 5–8%). The run-to-run spread is wider than in the fixed script; treat this
  row as directional.
- **Corrected baseline:** these readings use the two-semaphore baseline: pending
  (`maxBufferedItems`) plus started-but-not-delivered (`maxConcurrency`). A literal deletion of the
  old third total-retained semaphore while keeping the active release after operation completion
  violated the C+B retained bound (see "Bounds"); the fair baseline holds the active slot until
  delivery. Holding that slot longer costs more than the removed semaphore, so the corrected
  baseline is not cheaper than the earlier three-semaphore variant. No allocation row flipped:
  FunnySharp remains lower on all three rows in both runs.
- **Timing:** the three scripts are not uniform. FunnySharp was at parity or slightly slower on the
  fixed script (67.6–72.8 us versus 67.5–68.9 us), slower on both skewed runs (668.5–707.5 us
  versus 657.2–687.7 us, roughly 2–3%), and slower on both interleaved runs (588.4–670.4 us versus
  504.5–541.5 us, roughly 17–24%). No speedup is claimed; the facility remains faster on these rows
  but does not provide the per-key start-order guarantee.
- **Scheduling overhead versus algorithmic work:** both equivalent consumers run the same 18/184
  account operations with the same `Task.Yield` continuation and the same ledger rule. The measured
  residual is coordination: admission, per-key queue selection, result publication, channel
  handoff, and cleanup. No operation work is moved from one side to the other.

The harness is
[KeyedConcurrencyMeasurements.cs](../tests/FunnySharp.KeyedConcurrencyMeasurements/KeyedConcurrencyMeasurements.cs);
run it twice with:

```shell
dotnet run --project tests/FunnySharp.KeyedConcurrencyMeasurements/FunnySharp.KeyedConcurrencyMeasurements.csproj -c Release
```

## Residual Risks And Open Questions

- **Hot-key starvation.** With `maxConcurrency: 1`, or a hot-key prefix that fills the pending
  buffer, other keys wait. This is disclosed, not hidden. A weighted or round-robin cross-key
  policy would need a new goal and a grammar that does not add a mode parameter.
- **Non-cooperative callbacks.** Cleanup waits for them; a hung callback can hang disposal. Forced
  termination would be a different runtime promise and is rejected.
- **Completion-order ties.** Concurrent completions have no specified relative order. Tests release
  gates one at a time; the docs state the limit.
- **`maxBufferedItems` must be at least one.** The key is unknown until the item is read; a value of
  zero cannot guarantee immediate admission without a transient extra item.
- **Measurement handoff.** The FunnySharp source-side retained counter can lead the delivery
  counter by one item; the deterministic blocked-source test is the exact bound witness. The BCL
  baseline advances its delivery counter before releasing its active slot, so it reaches the exact
  bound and the comparison verifier asserts no extra allowance for it. Timing remains noisy and
  scheduler-sensitive.
- **Facility comparison.** `Parallel.ForEachAsync` plus per-key semaphores has no start-order
  protocol and does not bound its per-key gate dictionary; an earlier harness run observed a
  per-key order violation on the interleaved script. It is not used for equivalence or as the
  primary allocation baseline.

## Gate Evidence

Final gate results for this working tree:

- `build`: 0 warnings, 0 errors.
- `test`: 2,850 passed, 0 failed, 0 skipped on each of three consecutive full runs.
- `format`: clean for all C# projects; the F# harness projects are not format-checked.
- `verify-docs-snippets`: 81 snippets across 15 primary guides.
- `verify-api-baseline`: 115 types and 989 members across 2 assemblies.
- Examples executable: `FunnySharp examples passed.`; ASP.NET Core example verification:
  `FunnySharp ASP.NET Core example endpoints mapped.`
- Measurement harness: two complete Release runs; both verified equivalent-consumer parity,
  per-key start/completion order, the exact BCL retained bound and the one-item FunnySharp handoff,
  and event coverage before printing the tables above.
