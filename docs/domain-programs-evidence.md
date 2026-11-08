# Domain Programs And Deterministic Simulation Consumer Evidence

This record supports the maintainer's judgment for
[Goal 0037](goals/archive/0037-goal.md): whether the existing pure state-machine surface can become
reusable business programs whose substate updates, message routing, and output commands compose
without duplicated routing glue or obscured failure; whether a child program can be lifted into a
parent model; whether emitted commands can be inspected and simulated deterministically while real
I/O stays ordinary .NET; and whether the simulation makes late responses, duplicate messages, stale
request versions, rejected transitions, and command failures visible and reproducible.

Source identity is the Goal 0037 working tree on top of commit `3b63d1b`; the landing commit
becomes the durable identity. Measurements were taken on this host:

- Ubuntu 24.04.5 LTS, Linux 7.0.0-1017-azure, x64.
- .NET SDK 10.0.401, runtime 10.0.12, x64; Release build.
- Date: 2026-11-08 local / 2026-11-08 UTC.

Allocation is the blocking comparison; timing is directional on this shared host.

## Compared Consumers

The consumers run the same order workflow rules over the same shared models, messages, commands,
errors, initial models, scenario script, and expected reports. Those shared definitions and the
comparison harness are not charged to either side.

| Version | File | Raw lines | Shape |
| --- | --- | ---: | --- |
| Direct switch workflow | [HandWrittenOrderWorkflow.cs](../tests/Shared/ProgramComparison/HandWrittenOrderWorkflow.cs) | 265 | One parent switch routes to per-area inventory, payment, and cancellation helpers; each applied update copies the parent once and constructs parent commands directly. |
| Direct switch simulation adapter | [HandWrittenOrderSimulation.cs](../tests/Shared/ProgramComparison/HandWrittenOrderSimulation.cs) | 203 | Hand-written transcript, command sequence, pending FIFO, reply correlation, execution-failure recording, and replay over the same messages. |
| Direct switch total | — | **468** | Complete consumer implementation of the compared workflow and its adapter. |
| FunnySharp child and parent programs | [FunnySharpOrderPrograms.cs](../tests/Shared/ProgramComparison/FunnySharpOrderPrograms.cs) | 344 | Inventory and payment child programs with their own message/command types, lifted into `OrderModel`; a parent-level cancellation program; `OrElse` composition. |
| FunnySharp simulation adapter | [FunnySharpOrderSimulation.cs](../tests/Shared/ProgramComparison/FunnySharpOrderSimulation.cs) | 133 | Wraps `ProgramSimulation`, exposes pending peek/correlation, and maps the core transcript to the shared report records. |
| FunnySharp total | — | **477** | Complete consumer implementation of the same workflow and report. |

Shared definitions are [OrderModel.cs](../tests/Shared/ProgramComparison/OrderModel.cs) (52
lines), [OrderMessage.cs](../tests/Shared/ProgramComparison/OrderMessage.cs) (47),
[OrderCommand.cs](../tests/Shared/ProgramComparison/OrderCommand.cs) (27),
[OrderError.cs](../tests/Shared/ProgramComparison/OrderError.cs) (22),
[OrderSimulationContract.cs](../tests/Shared/ProgramComparison/OrderSimulationContract.cs) (163),
[OrderScenarios.cs](../tests/Shared/ProgramComparison/OrderScenarios.cs) (796), and
[ProgramComparison.cs](../tests/Shared/ProgramComparison/ProgramComparison.cs) (168): 1,275 lines
not charged to either side. The shipped implementation is
[DomainProgram.cs](../src/FunnySharp/DomainProgram.cs) (274),
[EmittedCommand.cs](../src/FunnySharp/EmittedCommand.cs) (71),
[SimulationDelivery.cs](../src/FunnySharp/SimulationDelivery.cs) (87),
[CommandExecutionFailure.cs](../src/FunnySharp/CommandExecutionFailure.cs) (76), and
[ProgramSimulation.cs](../src/FunnySharp/ProgramSimulation.cs) (433): 941 lines, not consumer code.
The compiling example is [DomainProgramExamples.cs](../examples/FunnySharp.Examples/DomainProgramExamples.cs)
(712 lines); the byte-exact documentation samples are
[DomainProgramSamples.cs](../examples/FunnySharp.DocumentationSamples/DomainProgramSamples.cs)
(957 lines). Raw lines include braces, comments, and blank lines; this is not a LOC target.

The FunnySharp consumer is **9 lines larger** than the direct switch baseline (477 versus 468,
1.9%). The direct switch uses parent message/command types directly; the FunnySharp side declares
separate child message/command types and their lift mappings. The library adapter is smaller than
the hand-written simulation adapter (133 versus 203), but the child-program declarations are larger
than the direct switch's per-area helpers (344 versus 265). The comparison does not claim a code
size win.

## Equivalence

`ProgramComparisonVerifier.Verify` runs seven scenarios through the fixed expected report, the
direct switch implementation, and the FunnySharp composition:

`successful cancellation`, `late duplicate and stale responses`, `rejected transitions`, `failed
transition and command failure`, `inventory unavailable and payment timeout`, `random gateway
captured` (seed 1), and `random gateway declined` (seed 2).

Each scenario has a fixed `OrderScenarioExpectation` containing the final model, final explicit
time, the full ordered `EmittedOrderCommand` list (sequence, command value, emission time), the
delivery outcomes (correlation sequence, time, status, error), and the command execution failures.
The comparison asserts:

- both implementations match the fixed expectation, not only each other;
- both implementations produce identical reports, including the delivered message values,
  command values, times, statuses, errors, correlations, and failures;
- each implementation produces the same report when the same scenario runs twice, including the
  same-seed random scenarios;
- the two random seeds produce the fixed captured and declined outcomes;
- state ownership: an inventory update preserves the payment and cancellation references, and a
  payment update preserves the inventory and cancellation references;
- command ordering: a cancellation emits release, refund, and notify in that order; sequences are
  monotonic; the pending count is part of the normalized report, so hand/funny parity and repeated
  runs assert it. The reply-append FIFO rule is pinned by the focused simulation test;
- message correlation: replies and execution failures carry the emitted command sequence; the
  domain request version inside the message drives stale/duplicate decisions;
- replay parity, replay of an applied transcript reproducing the final model and command list,
  replay stopping at the first non-applied delivery, and replay not changing either simulation's
  emitted-command, delivery, command-failure, or pending counts.

Focused tests in [DomainProgramTests.cs](../tests/FunnySharp.Tests/DomainProgramTests.cs),
[DomainProgramLiftTests.cs](../tests/FunnySharp.Tests/DomainProgramLiftTests.cs), and
[ProgramSimulationTests.cs](../tests/FunnySharp.Tests/ProgramSimulationTests.cs) pin the boundaries
the shared scenarios do not: default-program failures, null delegates, callback ledgers and order,
route `None` behavior, zero/many command outputs, the lifter's pre-update parent and the setter's
original parent reference, child reuse in two parents, exceptions, time monotonicity and
clock restoration on failed `...At` calls, unknown/completed command sequences, live pending-list
behavior, pending preservation under non-applied deliveries and selective failure removal, the
optional random source, transcript value equality, replay purity, mutation of emitted command
payloads and returned state snapshots, and the caller-owned immutability boundary for recorded
messages and the initial model.

## Semantic Behavior Ledger

| Scenario | Observed behavior |
| --- | --- |
| successful cancellation | Reserve and payment replies apply; cancellation emits release, refund, notify in order; the refund command failure is recorded separately; the refund-failure domain message moves cancellation to `RefundFailed`. |
| late response | A reply to the older command is delivered with the older sequence; the program sees the newer pending version and returns `StaleResponse`. |
| duplicate message | A second reply to the current command is delivered; the program returns `DuplicateResponse`; a duplicate cancellation returns `DuplicateResponse`. |
| stale request version | A reply with an older domain version returns `StaleResponse` with received and expected versions. |
| rejected transition | Early cancellation and second reserve are `Rejected`; the model and pending commands are unchanged. |
| failed transition | A non-positive charge is `Failed` with `InvalidTransition`; it is not a command execution failure. |
| command execution failure | `FailCommand` records `CommandExecutionFailure`, removes the command from pending, and leaves the model unchanged; a later domain message can apply. |
| inventory unavailable | The unavailable reply is an applied state change to `Unavailable` plus a notify command; a duplicate reply is `Rejected`; a second reserve is `Rejected`. |
| payment timeout | The early timeout is `Rejected`; the late timeout applies `TimedOut` and emits a notify; a duplicate timeout is `Rejected`. |
| random captured | Seed 1 chooses capture; the payment applies `Captured` with no notify command. |
| random declined | Seed 2 chooses decline; the payment applies `Declined` and emits the decline notify command. |
| replay | Applies the recorded deliveries from the initial model, reproduces the final model and commands for an applied transcript, stops at the first non-applied delivery, and never re-emits, re-delivers, or re-records. |
| command payload mutation | Mutating an emitted command's mutable payload does not change the replayed state or command shape; commands are outputs, not replay inputs. |
| state snapshot mutation | Mutating a returned final-state list does not change a replay from the initial model; the update creates a new list. |
| explicit time | `AdvanceTo`/`AdvanceBy` move forward only; the timeout decision changes with the message's `At`; the transcript records the explicit time. |
| explicit randomness | The simulation exposes the caller's seeded `Random` and never consumes it; the scenario script consumes it. |

## Correlation And Ordering Ledger

The successful cancellation scenario fixes the following correlation and ordering evidence:

| Emitted sequence | Command | Emitted at | Reply/failure correlation |
| ---: | --- | --- | --- |
| 1 | `ReserveInventoryCommand("ORD-1", "SKU-1", 2, 100)` | `T+0` | reply delivery sequence 1 |
| 2 | `ChargePaymentCommand("ORD-1", 25, 200)` | `T+1m` | reply delivery sequence 2 |
| 3 | `ReleaseInventoryCommand("ORD-1", "SKU-1", 2)` | `T+3m` | pending |
| 4 | `RefundPaymentCommand("ORD-1", 25)` | `T+3m` | command execution failure sequence 4 |
| 5 | `NotifyCustomerCommand("ORD-1", "Cancellation accepted.")` | `T+3m` | pending |
| 6 | `NotifyCustomerCommand("ORD-1", "Refund failed: gateway.")` | `T+6m` | pending |

The late/duplicate scenario fixes two charge commands (versions 200 and 201), a stale reply to
sequence 1, an applied reply to sequence 2, a duplicate reply to sequence 2, and a late reply to
sequence 1. The domain versions are 200/201 while the simulation sequences are 1/2; the two
identities are deliberately separate and both visible.

## Measurements

Command:

```bash
dotnet run --project tests/FunnySharp.ProgramMeasurements/FunnySharp.ProgramMeasurements.csproj -c Release
```

The harness first runs the complete comparison, then measures prebuilt fixtures with 50 iterations
after 5 warmup iterations, reporting the minimum of three post-warmup rounds. The measurement
project disables tiered compilation (`System.Runtime.TieredCompilation=false`), so every row is
full-JIT. Every row uses `GC.GetAllocatedBytesForCurrentThread`; the operations are synchronous, so
one counter observes the whole operation. Allocation is the stable comparison; timing is
directional.

The dispatch fixture applies a seven-message order sequence: reserve, inventory reply, charge,
payment reply, cancel, duplicate cancel, and an unrouted message. The state-copy fixture applies
one reserve message. The full scenario is the successful cancellation scenario. The replay fixture
replays the successful cancellation transcript.

| Operation | Allocated B/op | Time ns/op |
| --- | ---: | ---: |
| Composition - direct switch delegate | 0.8 | 8.0 |
| Composition - `DomainProgram` `Lift` + `OrElse` | 432.8 | 90.0 |
| Dispatch - direct switch, 7 messages | 1,488.8 | 718.0 |
| Dispatch - `DomainProgram`, 7 messages | 2,000.8 | 1,106.0 |
| State copy - direct switch, one inventory update | 520.8 | 182.0 |
| State copy - `DomainProgram`, one inventory update | 688.8 | 306.0 |
| Full scenario - direct switch simulation | 4,248.8 | 2,422.0 |
| Full scenario - `ProgramSimulation` | 6,984.8 | 3,636.0 |
| Replay - direct switch | 1,960.8 | 1,544.0 |
| Replay - `ProgramSimulation` | 2,528.8 | 1,992.0 |

Two final consecutive runs reproduced every allocation row in this table byte-for-byte; only the
directional timings varied. The full-scenario library row measured 3,636.0 and 3,770.0 ns/op
(direct switch 2,422.0 and 2,504.0); an earlier pre-optimization run measured 7,168.0 ns/op and is
not used. The table shows the first of the two final runs. No universal speedup is claimed.

## Interpretation

- **The library is larger in every measured row.** Composition allocates 432.8 B versus 0.8 B for
  the static method-group delegate. The composition creates two `Lift` closures and two `OrElse`
  closures, with their delegates, once per composition; the cancellation decision is a direct
  program with no lift or fallback closure. A consumer can compose once and reuse the program, but
  the comparison charges a fresh composition per measured full scenario.
- **Dispatch costs 512.0 B more (34.4%).** The seven-message sequence applies five child or parent
  updates. Each applied lifted child update creates the child's `StateChange` and output snapshot
  in addition to the parent `StateChange` and mapped output array; the direct switch creates only
  the parent result. The composed route chain invokes up to two route delegates for messages owned
  by the later programs; the cancellation decision is a direct program with no route delegate. The
  rejected duplicate and undefined messages add no applied-state snapshots.
  The extra 512.0 B is the child snapshot plus route scaffolding, not a hidden second parent copy.
- **One state copy costs 168.0 B more (32.3%).** Both sides copy the parent once through `with`.
  The library additionally creates the child `StateChange` and child output snapshot before the
  parent setter; the direct switch constructs the parent result directly. The parent copy count is
  the same; the child program abstraction is the extra cost.
- **The full scenario costs 2,736.0 B more (64.4%).** The composed update pays the child snapshot
  cost for every applied child message, and the adapter maps the core `EmittedCommand`,
  `SimulationDelivery`, and `CommandExecutionFailure` values into the shared report records. The
  baseline writes those shared records directly. The mapping is the normalized-output cost of the
  comparison, not a hidden execution path. The core simulation's transcript types are part of the
  shipped driver; a production consumer that uses them directly avoids the shared-report mapping.
- **Replay costs 568.0 B more (29.0%).** Both sides use the existing `StateMachine.Replay` over the
  recorded messages. The difference is the composed update's route chain and child snapshots, not
  a second replay implementation.
- **Consumer code is not smaller.** The direct switch consumer is 468 lines; the FunnySharp
  consumer is 477 lines. The adapter is smaller (133 versus 203), but the child program types and
  lift mappings are larger than the direct switch helpers (344 versus 265). The shared definitions
  are not charged. The value is not a code-size or allocation win.

## Maintainer Judgment

The program composition and deterministic driver make the complete workflow easier to reason about
in a specific way: inventory and payment are independently testable child programs with their own
message and command types; the parent route is declared next to each lift; the parent setter is the
only parent write; the same inventory program is reused in a shipment parent; and the simulation
exposes late, duplicate, stale, rejected, failed, and execution-failure outcomes in one transcript
with explicit time, seeded randomness, command ordering, and correlation. The example shows the
ordinary `while`/`await` execution boundary without a library scheduler.

The judgment is conditional and sceptical about cost. For a single small workflow, the direct
switch is slightly smaller, materially leaner in allocation, and has no composition closures. The
FunnySharp consumer pays for the child-program abstraction and the normalized transcript. The
surface earns its place when the child programs are genuinely reused across parents, when the
deterministic simulation and transcript are the product requirement, or when the declarative route
and state-ownership contract prevent duplicated routing glue. It does not earn a claim of lower
allocation, shorter consumer code, or faster dispatch against a competent direct switch. The
maintainer should accept the surface on the composition and simulation contract, not on a
performance story.

## Residual Risks And Limits

- `Replay` stops at the first non-applied delivery, exactly like the existing state-machine replay.
  The simulation transcript is the record of later steps; a second transcript-replay policy is not
  added.
- **Mutation clause (maintainer-accepted interpretation).** Goal 0037 requires that state or
  payload mutation must not invalidate claimed reproducibility. The maintainer reads that clause as
  covering the values the library produces and returns: mutating an emitted command payload or a
  returned state snapshot cannot change a replay. The initial model and recorded messages are
  caller-owned inputs retained by reference; the library does not deep-copy arbitrary caller
  records, which would require cloning every generic model and message and would contradict the
  stated no-deep-payload-copying boundary. Reproducibility therefore requires value-immutable
  inputs: mutating a recorded message or the initial model can change a later replay. Tests pin
  both the guarantee and this boundary.
- The simulation is single-threaded and not thread-safe. Its transcript retains every delivery and
  emitted command; it is not a bounded production event store.
- No library execution adapter ships. The example and guide use an ordinary `while`/`await` loop
  with `Task`/`ValueTask`, `CancellationToken`, and caller-owned dependencies.
- `Lift` shares `TError` between child and parent, maps one child command to one parent command,
  and routes one parent message to at most one child. Error mapping, command fan-out, and broadcast
  remain caller policy.
- The direct switch baseline is one competent implementation. A different hand-written design could
  be leaner or clearer for a different workflow; the comparison fixes the same models, policies,
  transcript, and scenario script so the two sides do the same work.
- The full-scenario timing is directional and noisy; allocation is the stable comparison.
