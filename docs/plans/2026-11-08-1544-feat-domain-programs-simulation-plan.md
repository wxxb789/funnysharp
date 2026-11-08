---
title: Composable domain programs and deterministic simulation
type: feat
date: 2026-11-08
artifact_contract: ce-unified-plan/v1
product_contract_source: docs/goals/archive/0037-goal.md, docs/product-contract.md, docs/grammar.md
execution: code
---

# Domain Programs And Deterministic Simulation Plan

## Goal Capsule

- **Objective:** Advance the existing pure state-machine surface into reusable business programs
  whose substate updates, message routing, and output commands compose without duplicated routing
  glue. A realistic order workflow must combine independently understandable inventory, payment,
  and cancellation decisions; a child program must be liftable into a parent model; emitted
  commands must be inspectable and simulatable while real I/O stays ordinary .NET code. A
  deterministic simulation must cover late responses, duplicate messages, stale request versions,
  rejected transitions, and command failures, with explicit time, randomness, and external
  replies. Replay must never silently re-execute historical side effects, and mutation of emitted
  commands or returned state snapshots must not invalidate the reproducibility the library claims.
- **Means:** A small `DomainProgram<TModel, TMessage, TCommand, TError>` description over the
  existing `StateMachine` delegate. It exposes the pure `Update` step, `OrElse`, `Replay`, and a
  `Lift<TParentModel, TParentMessage, TParentCommand>` instance method that embeds a child program
  into a parent model through a substate getter, a parent setter, an `Option`-based message route,
  and a parent-aware child-to-parent command lifter. A `ProgramSimulation<TModel, TMessage, TCommand, TError>`
  drives the program with explicit `DateTimeOffset` time, an optional caller-supplied `Random`, and
  explicit external replies. It records ordered emitted commands, deliveries, and command execution
  failures; it never executes commands and never reads ambient time or randomness. The direct
  switch-based implementation and its hand-written simulation adapter are the comparison baseline.
- **Authority:** [Goal 0037](../goals/archive/0037-goal.md) is the completion contract.
  [Product contract](../product-contract.md), [grammar](../grammar.md), and
  [core design](../core-design.md) are the current boundaries. Goal 0030's domain values remain
  optional boundary types; Goal 0036's reshaping and earlier sequence families are not re-delivered.
  Goal 0038 concurrency is out of scope.
- **Execution profile:** Implement in `src/FunnySharp/DomainProgram.cs`,
  `EmittedCommand.cs`, `SimulationDelivery.cs`, `CommandExecutionFailure.cs`, and
  `ProgramSimulation.cs`; pin behavior in `tests/FunnySharp.Tests`; share the order workflow,
  direct switch baseline, and comparison in `tests/Shared/ProgramComparison`; measure through
  `tests/FunnySharp.ProgramMeasurements`; exercise through
  `examples/FunnySharp.Examples/DomainProgramExamples.cs` and the documentation samples; record
  evidence in `docs/domain-programs-evidence.md`.
- **Tail ownership:** The maintainer judges whether the program composition and deterministic
  driver materially reduce the complete order workflow and make failure, correlation, ordering,
  and state ownership clearer. The evidence records a judgment even if it is sceptical, for example
  when a direct switch plus a hand-written simulation loop is shorter or leaner for a single small
  workflow.
- **Stop conditions:** Stop for a new decision if the design would need a second outcome carrier,
  a scheduler or actor runtime, automatic command execution, durable event storage, a second
  replay semantics, deep payload copying or serialization, ambient time/randomness, a universal
  application program representation, or Goal 0038 concurrency. If the abstraction is materially
  heavier than the competent baseline, report that result rather than changing semantics or adding
  hidden copies to win a measurement.

## Product Contract

**Included**

- A non-generic `DomainProgram` factory with `Create<TModel, TMessage, TCommand, TError>(update)`
  and `DomainProgram<TModel, TMessage, TCommand, TError>`, a readonly struct wrapping one
  `StateMachine<TModel, TMessage, TCommand, TError>` delegate. `Create` validates and wraps;
  `Update(model, message)` invokes the pure step; `AsStateMachine()` exposes the underlying
  delegate; the default value is uninitialized and every operation fails explicitly.
- `OrElse(fallback)`: delegates to the existing `StateMachineExtensions.OrElse`; the fallback runs
  only when the first program returns `Undefined`, and `Applied`, `Rejected`, and `Failed` pass
  through unchanged.
- `Replay(initialModel, messages)`: delegates to the existing `StateMachineExtensions.Replay`;
  enumerates the message history once, stops at the first non-applied result, preserves that
  status and error, and never executes commands.
- `Lift<TParentModel, TParentMessage, TParentCommand>(getChild, setChild, route, liftCommand)`:
  embeds a child program into a parent model. `route` returns `Option<TChildMessage>`; `None`
  yields `Undefined` without reading the child state. A routed message invokes the child update
  once on `getChild(parent)`. An applied child change invokes `liftCommand(parent, output)` once
  per child output in order, then `setChild` once; the lifter receives the parent model before the
  child state is applied so it can add aggregate identity or other parent context. Rejected,
  failed, and undefined child results pass through with their status and error. The child and
  parent share `TError`; no error mapper is added.
- `EmittedCommand<TCommand>`: a readonly value carrying a monotonic simulation `Sequence`, the
  command value, and the explicit `EmittedAt` time. It is an execution/diagnostic value, not a
  carrier. Command order is sequence order; multiple commands from one change keep their
  `StateChange.Outputs` order.
- `SimulationDelivery<TModel, TMessage, TCommand, TError>`: a readonly transcript entry carrying
  the delivered message, explicit `At`, the correlated command sequence (`null` for a direct
  message), and the `TransitionResult`. It is a diagnostic value, not a carrier.
- `CommandExecutionFailure<TCommand, TError>`: a readonly record of an external command execution
  failure carrying the emitted command, the typed error, and the explicit `At`. It is distinct
  from `TransitionStatus.Rejected` and `TransitionStatus.Failed`.
- `ProgramSimulation<TModel, TMessage, TCommand, TError>`: a single-threaded deterministic driver.
  - Construction takes the program, the initial model, an explicit `DateTimeOffset startedAt`, and
    optionally a caller-supplied `Random`. The simulation never reads ambient time or randomness
    and never consumes the `Random`; a scenario uses it to choose external replies.
  - `AdvanceTo(at)` and `AdvanceBy(by)` move the explicit clock forward; backwards or negative time
    is rejected. `Deliver(message)`, `Reply(commandSequence, message)`, and
    `FailCommand(commandSequence, error)` use the current time; `...At` overloads advance first.
  - `Deliver` invokes the program once, records a delivery, and on `Applied` updates the model and
    appends emitted commands in order. Non-applied results leave the model unchanged.
  - `Reply` validates that the sequence was emitted, invokes the program once, records a delivery
    with the correlation, and on `Applied` updates the model and appends new commands. A reply to a
    completed or failed command is allowed and is the duplicate/late-response path; the program
    decides whether it is stale or unexpected. An unknown sequence is a driver error.
  - `FailCommand` validates that the sequence was emitted and still pending, removes it from the
    pending list, records a `CommandExecutionFailure`, and leaves the model unchanged. Failing an
    already completed or failed command is a driver error. The caller may deliver a domain message
    afterward; the simulation never invents a failure reply.
  - `EmittedCommands` is append-only in emission order; `PendingCommands` is the FIFO list of
    commands not yet replied to or failed; `Deliveries` and `CommandFailures` are the ordered
    transcripts. New commands emitted by a reply append after existing pending commands; no
    scheduler, priority, retry, or execution policy is added.
  - `Replay()` delegates to `StateMachine.Replay` over the recorded delivery messages from the
    initial model. It stops at the first non-applied delivery, never executes or re-records
    commands or command failures, and is a pure audit of the recorded applied prefix.
- Focused tests cover the program step, `OrElse`, `Replay`, lifting, routing, command lifting,
  state ownership, simulation delivery/reply/failure, time, randomness, correlation, late and
  duplicate messages, stale versions, rejected transitions, command failures, replay purity, and
  mutation boundaries.
- A shared comparison runs the same order workflow through the program composition and through a
  competent direct switch-based implementation with a hand-written simulation adapter. It asserts
  fixed expected reports for success, late, duplicate, stale, rejected, failed-transition, and
  command-failure scenarios, plus parity on command order, correlation, state ownership, time, and
  randomness.
- A measurement executable reports composition, dispatch, state-copying, and full-scenario
  allocation and directional timing against the direct switch baseline. Allocation reproduces
  across two runs with one counter and one JIT tier.
- A compiling example, a guide closing with `## Deliberate Boundaries`, byte-exact documentation
  samples, an evidence record, API-baseline and performance-manifest updates, and reconciled
  product/grammar/stability/index/goal documents.

**Deferred to Follow-Up Work**

- A library in-process execution adapter. The example shows the ordinary `while`/`await` loop with
  `Task`/`ValueTask`, `CancellationToken`, and caller-owned dependencies. A library adapter would
  have to choose a failure-to-message mapping and a stop/continue policy, duplicating the consumer
  loop and risking a scheduler-shaped API; it returns only with a demonstrated repeated burden and
  a policy the goal can state.
- A transcript replay that continues after expected rejections. The existing `Replay` semantics
  stop at the first non-applied result; the simulation transcript remains the record of later
  steps. A second replay shape returns only if a concrete consumer needs full transcript
  reproduction and the grammar can state the distinct stop policy honestly.
- Asynchronous or concurrent simulation delivery. The simulation is a synchronous, single-threaded
  deterministic driver; concurrent delivery and per-key ordering belong to Goal 0038.
- Error mapping in `Lift`. Child and parent programs share `TError`, matching `OrElse`; a consumer
  with distinct child error types maps them into the shared hierarchy or adapts before lifting.
- Command fan-out, filtering, or many-to-many command lifting. `Lift` maps each child command to
  exactly one parent command in order; a child that should not emit a command omits it.
- Broadcast or multiple child messages from one parent message. `route` returns one
  `Option<TChildMessage>`; a message is owned by at most one lifted child. Multi-dispatch is not
  inferred.
- Durable event logs, snapshots, serialization, persistence, retries, compensation, transactions,
  distributed recovery, exactly-once effects, an actor runtime, a mailbox, or a custom scheduler.
- Deep copying of models, messages, commands, or errors. The simulation and replay retain caller
  values by reference under the repository's existing caller-owned-payload rule.
- A universal application program representation. `DomainProgram` is an opt-in description for one
  workflow; ordinary C# and the existing state-machine surface remain valid without it.

**Outside this product's identity**

- A second outcome carrier, a replacement type system, a workflow runtime, an actor framework, a
  scheduler, a persistence layer, an event-sourcing framework, or a distributed orchestrator.

## Planning Contract

### Requirements

- **R1:** Add the non-generic `DomainProgram` factory with `Create<TModel, TMessage, TCommand,
  TError>` and the public `DomainProgram<TModel, TMessage, TCommand, TError>` readonly struct with
  `Update`, `AsStateMachine`, `OrElse`, `Replay`, and `Lift`, with complete XML documentation. The
  default value is uninitialized; every operation fails explicitly.
- **R2:** `Create` validates its delegate eagerly and wraps it without copying or executing it.
  `Update` invokes the delegate exactly once and returns its `TransitionResult` unchanged.
- **R3:** `OrElse` preserves the existing state-machine meaning: the fallback runs only after
  `Undefined`; `Applied`, `Rejected`, and `Failed` pass through unchanged with their payloads.
- **R4:** `Replay` preserves the existing state-machine meaning: one enumeration of the history,
  source order, final state and ordered outputs on success, and the first non-applied result with
  its status and error on failure. It never executes an output.
- **R5:** `Lift` validates all four delegates eagerly. `route` is invoked once per parent message;
  `None` returns `Undefined` without invoking the getter, the child update, the command lifter, or
  the setter.
- **R6:** A routed message invokes `getChild` once and the child update once. An applied child
  change invokes `liftCommand(parent, output)` once per child output in order, then `setChild` once
  with the child's next state. The lifter receives the parent model before the child state is
  applied. Rejected, failed, and undefined child results pass through without invoking the command
  lifter or the setter.
- **R7:** Lifting preserves command order and count one-to-one. The parent result's state change
  owns a fresh exact-size parent-command array; the child output snapshot is read, not mutated.
  Zero child outputs reuse `Array.Empty<T>`.
- **R8:** Lifting does not add a parent state copy. The parent setter is the only parent update and
  is invoked exactly once per applied child change; sibling substate references are unchanged
  unless the caller's setter changes them. Rejected, failed, and undefined results do not copy or
  change the parent.
- **R9:** The same child program can be lifted into different parent models with different
  getters, setters, routes, and command lifters; the child update is unchanged.
- **R10:** `EmittedCommand`, `SimulationDelivery`, and `CommandExecutionFailure` are readonly
  values with deterministic properties, value equality where the payload supports it, and
  deterministic text. None is an outcome carrier.
- **R11:** `ProgramSimulation` construction validates the program, requires an explicit
  `startedAt`, and accepts an optional non-null caller-supplied `Random`. It never reads
  `DateTimeOffset.UtcNow`, `DateTime.UtcNow`, `Random.Shared`, or an ambient clock.
- **R12:** `AdvanceTo` and `AdvanceBy` move time forward only; `AdvanceBy` rejects a negative
  interval. `Deliver`, `Reply`, and `FailCommand` with an `...At` overload advance first. Every
  transcript entry records the explicit time used.
- **R13:** `Deliver` invokes the program once and records a delivery. `Applied` updates the model
  and appends commands in order with monotonic sequences; `Rejected`, `Failed`, and `Undefined`
  leave the model and pending commands unchanged.
- **R14:** `Reply` validates the sequence was emitted, invokes the program once, records a delivery
  with the correlation, and on `Applied` updates the model and appends commands. A reply to a
  completed or failed command is allowed; the program decides. An unknown sequence is an
  `ArgumentOutOfRangeException`.
- **R15:** `FailCommand` validates the sequence was emitted and pending, removes it from pending,
  records a `CommandExecutionFailure`, and leaves the model unchanged. An unknown sequence is an
  `ArgumentOutOfRangeException`; an already completed or failed command is an
  `InvalidOperationException`. A later reply to a failed command is allowed and recorded.
- **R16:** `EmittedCommands` is append-only and ordered; `PendingCommands` is FIFO in emission
  order and removes the first completion or failure by sequence; new commands from a reply append
  after existing pending commands. `Deliveries` and `CommandFailures` preserve their operation
  order. No execution, retry, priority, or scheduling policy is added.
- **R17:** `ProgramSimulation.Replay()` delegates to `StateMachine.Replay` over the recorded
  delivery messages from the initial model, stops at the first non-applied result, never executes
  or re-records commands or failures, and does not read or mutate emitted commands. Mutating an
  emitted command after the simulation cannot change the replayed state or command shape.
- **R18:** The shared comparison and measurement cover the complete consumer workflow, the direct
  switch baseline, state-copying and dispatch costs, and the fixed scenarios. The evidence reports
  the honest direction, including any library cost, and the maintainer judgment. The example,
  guide, snippets, evidence, API baseline, performance manifests, solution, and goal state are
  reconciled. No durable event sourcing, distributed recovery, exactly-once effects, automatic
  transactions, actor runtime, custom scheduler, ambient time/randomness, or Goal 0038 behavior is
  added.

### Key Technical Decisions

- **KTD1 (governs R1, R10, R18):** `DomainProgram` is a thin readonly description over the
  existing `StateMachine` delegate. `EmittedCommand`, `SimulationDelivery`, and
  `CommandExecutionFailure` are execution/diagnostic values. The canonical four carriers and the
  existing `TransitionResult` status vocabulary are unchanged.
- **KTD2 (governs R3, R4):** `OrElse` and `Replay` delegate to the existing `StateMachine`
  extensions. The program adds composition and naming, not a second fallback or replay meaning.
- **KTD3 (governs R5-R8):** Lifting is a routing/ownership adapter. The route declares which parent
  messages the child owns; `Option.None` is the only "not mine" signal; the child update sees only
  its substate; the parent setter is the only parent write. This keeps sibling state independent
  and makes the parent copy count explicit.
- **KTD4 (governs R6, R7):** The `Lift` instance method is generic only over the parent types, so
  the call site writes `child.Lift<TParentModel, TParentMessage, TParentCommand>(...)` and the
  child model/message/command/error types come from the receiver. This is the reason the program is
  a struct wrapper rather than a bare delegate.
- **KTD5 (governs R11-R13):** Time is a `DateTimeOffset` value on the simulation and a message/model
  input to the program. The simulation never supplies time to the update implicitly; a decision
  that depends on time carries it in the message or state. Randomness is the caller's explicit
  seeded `Random` used to choose external replies; the program never reads it.
- **KTD6 (governs R14-R16):** The simulation's monotonic command sequence is execution
  correlation; the domain request version in the command/reply payload is stale-response
  correlation. They are deliberately separate and both visible. The simulation never deduplicates
  messages or replies; the program decides stale, duplicate, unexpected, rejected, or failed.
- **KTD7 (governs R15, R16):** An external execution failure is not a domain transition. `FailCommand`
  records it, removes the pending command, and leaves the model unchanged; the caller may deliver a
  domain message. `TransitionStatus.Failed` remains a defined-transition domain failure.
- **KTD8 (governs R17):** Replay is the existing pure state-machine replay over recorded messages.
  Commands are outputs, not replay inputs, so replay cannot re-execute them. The simulation
  transcript is the record of later steps after the first non-applied result.
- **KTD9 (governs R18):** The direct switch baseline is competent: per-area helper methods, pattern
  matching, the same immutable records, one parent copy per applied update, pre-sized output, and
  a hand-written simulation adapter with the same correlation/transcript contract. The shared
  fixtures, scenario script, models, messages, commands, errors, and report contract are not
  charged to either side. The comparison asserts fixed expectations and parity, not only parity.
- **KTD10 (governs R18):** The measurement uses one allocation counter
  (`GC.GetAllocatedBytesForCurrentThread`) and one JIT tier (`TieredCompilation=false`), runs twice,
  and republishes only allocation-stable rows. Timing is directional. The interpretation names the
  heavier side and why.

### Open Decisions And Recommendations

1. **Program representation: wrapper struct, bare delegate, or extension-only?** Options: a
   `DomainProgram` readonly struct over `StateMachine`; a new delegate type; extension methods on
   `StateMachine` alone. Recommendation: the readonly struct. It gives a named business-program
   type, a default-uninitialized contract, and an instance `Lift` whose generic parameters are only
   the parent types. A bare delegate cannot offer that call-site shape; extension-only would leave
   the program unnamed and make `Lift` generic over all seven types.
2. **Composition: `Lift` + existing `OrElse` or a new router type?** Options: declarative
   `Option` routes with `OrElse`; a single parent switch; a routing table keyed by message type.
   Recommendation: `Lift` + `OrElse`. It reuses the starting capability, keeps each child's route
   next to its program, and preserves the existing `Undefined`/`Rejected`/`Failed` distinction. A
   router type would be a second dispatch model and would hide the O(preceding children) route
   cost.
3. **Replay semantics for a simulation with expected rejections?** Options: reuse
   `StateMachine.Replay` (stop at first non-applied); add a transcript replay that continues; add
   both. Recommendation: reuse the existing `Replay` for `DomainProgram` and `ProgramSimulation`.
   The goal says `Replay` remains a starting capability; a second stop policy would be a second
   verb meaning. The simulation transcript records later steps, and the evidence states the
   applied-prefix scope explicitly.
4. **Command failure: automatic failure message or explicit `FailCommand`?** Options: the
   simulation invents a failure reply; `FailCommand` records the failure and the caller delivers a
   domain message; `FailCommand` only. Recommendation: explicit `FailCommand` plus caller-delivered
   message. The library cannot know the domain's failure message type or policy, and automatic
   delivery would blur expected rejection with execution failure.
5. **In-process execution adapter: ship one?** Options: ship a generic sequential executor; ship
   nothing and document the ordinary loop; ship an optional adapter only for `ValueTask` handlers.
   Recommendation: ship nothing. The example's `while`/`await` loop is the adapter and keeps
   `Task`/`ValueTask`, `CancellationToken`, and caller-owned dependencies at the boundary. A
   library adapter would need a failure-to-message mapping and a stop/continue policy, which is a
   scheduler-shaped decision the goal excludes. Revisit only if the comparison shows a material
   repeated burden.
6. **Time and randomness surface.** Options: `TimeProvider` plus ambient `Random`; explicit
   `DateTimeOffset` plus optional seeded `Random`; messages only. Recommendation: explicit
   `DateTimeOffset` and optional caller-supplied `Random`, with decisions taking time/randomness
   from messages or state. `TimeProvider` remains a real-boundary concern; ambient sources would
   make replay and the measurement non-deterministic.
7. **Correlation identity: one id or two?** Options: the simulation sequence is the domain request
   version; a domain version is the simulation sequence; both, separate. Recommendation: both,
   separate. The sequence is the transcript/execution correlation; the domain version is the
   stale-response decision input. The evidence shows both in the same scenario.
8. **Error type in `Lift`: shared or mapped?** Options: shared `TError`; an error mapper; a
   separate child error type with wrapping. Recommendation: shared `TError`, matching `OrElse`.
   The order workflow uses one `OrderError` hierarchy; a consumer with distinct child errors maps
   them into that hierarchy or adapts before lifting.
9. **Naming.** Options considered: `Program`, `BusinessProgram`, `DomainProgram`, `Workflow`.
   Recommendation: `DomainProgram`. It names the domain role, avoids the entry-point `Program`
   collision, and does not imply a runtime. `Lift` names the child-into-parent embedding;
   `Deliver`, `Reply`, and `FailCommand` name the simulation operations; `FailCommand` keeps
   execution failure distinct from `TransitionResult.Failed` and `Result.Failure`.
10. **Domain values from Goal 0030.** Options: require them in the program surface; use them in
    the example/guide only; omit them. Recommendation: keep the program generic and optional; use
    one validated identifier in the example/guide, and use plain records on both sides of the
    shared comparison so the measurement isolates the program abstraction. The guide states that
    `DomainValue` composes at the boundary without becoming a mandatory replacement type system.
11. **Transcript entry shape.** Options: a unified step union; separate delivery/failure ledgers;
    a single class with nullable fields. Recommendation: separate readonly `SimulationDelivery`
    and `CommandExecutionFailure` ledgers plus ordered `EmittedCommands`. It keeps each value
    meaningful, avoids nullable field soup, and still exposes correlation, ordering, and the
    rejection-versus-execution-failure distinction.
12. **Command lifter context.** Options: `Func<TChildCommand, TParentCommand>` only; pass the
    parent model as `Func<TParentModel, TChildCommand, TParentCommand>`; pass the child's next
    state too. Recommendation: pass the parent model before the child state is applied. It lets a
    reusable child command omit aggregate identity and lets the parent lifter add the order id,
    tenant, or other parent context without duplicating those fields in every child state. Passing
    the next child state would duplicate what the child command should already describe and would
    make the lifter order more surprising.

### Acceptance Examples

- **AE1:** An order workflow composes inventory and payment child programs lifted into
  `OrderModel` and a parent-level cancellation program through `OrElse`. A reserve message updates
  only `Inventory`, emits one ordered `ReserveInventoryCommand`, and leaves `Payment` and
  `Cancellation` references unchanged.
- **AE2:** A cancellation decision that sees reserved inventory and captured payment emits
  `ReleaseInventoryCommand`, `RefundPaymentCommand`, and `NotifyCustomerCommand` in that order;
  simulation sequences are monotonic and the pending FIFO preserves that order.
- **AE3:** `route` returns `None` for an unowned message; the lifted child returns `Undefined`
  without invoking the getter, child update, command lifter, or setter; `OrElse` tries the next
  program. A routed message that the child rejects stops the fallback and preserves the rejection.
- **AE4:** An applied child update invokes the child update once, the command lifter once per
  output in order, and the setter once. The parent is copied once; sibling references are
  unchanged. A rejected, failed, or undefined child result invokes neither the lifter nor the
  setter and leaves the parent unchanged.
- **AE5:** The same inventory child program is lifted into a second parent model with a different
  getter, setter, route, and command lifter; its update behavior is unchanged and both parents
  receive the lifted commands.
- **AE6:** `DomainProgram.Replay` on an applied message history reproduces the final model and
  ordered commands; a command executor counter is zero; commands are data and are never invoked.
- **AE7:** `Replay` stops at the first `Rejected`, `Failed`, or `Undefined` result and preserves
  its status and error; the simulation transcript still records later deliveries.
- **AE8:** `Deliver` records a delivery with the explicit time; `Applied` updates `Model` and
  appends commands; `Rejected`, `Failed`, and `Undefined` leave the model and pending commands
  unchanged.
- **AE9:** A reply correlated to command sequence 1 is recorded with that sequence; a late reply
  to an older command is delivered and the program rejects it with a stale-version error; a
  duplicate reply is delivered and the program rejects it as a duplicate or unexpected response.
  An unknown sequence is an `ArgumentOutOfRangeException`.
- **AE10:** `FailCommand` records a `CommandExecutionFailure` with the emitted command and typed
  error, removes the command from pending, and leaves the model unchanged. A `TransitionResult`
  with `Rejected` or `Failed` is a delivery, not a command failure; the three outcomes are
  distinguishable in the transcript. A later reply to the failed command is allowed; failing a
  completed command throws.
- **AE11:** `AdvanceTo`/`AdvanceBy` move the explicit clock forward; backwards or negative time
  throws. A timeout message carrying `At` changes the program decision at different simulation
  times; no ambient clock is read.
- **AE12:** Two runs with the same seed produce the same reply script, emitted commands, and
  transcript; a different seed produces the different fixed outcome. The simulation never reads
  `Random.Shared`.
- **AE13:** After a successful simulation, mutating an emitted command's mutable payload does not
  change the replayed state or command shape; mutating a returned final-state snapshot does not
  change a replay from the initial model. The evidence states the value-immutable message/model
  limit.
- **AE14:** The shared comparison runs success, late, duplicate, stale, rejected,
  failed-transition, command-failure, ordering, correlation, state-ownership, time, and randomness
  scenarios through both the direct switch implementation and the program composition. Both match
  fixed expectations and each other on models, command types/order, statuses, errors, and
  correlations.
- **AE15:** The measurement reports composition, dispatch, state-copying, and full-scenario rows;
  two runs reproduce every allocation row with one counter and one tier; the evidence names the
  heavier side and why.

## Implementation Units

### U1. Add the program description and its composition operations

- **Goal:** Add the non-generic `DomainProgram` factory with `Create<TModel, TMessage, TCommand,
  TError>` and the `DomainProgram<TModel, TMessage, TCommand, TError>` struct with `Update`,
  `AsStateMachine`, `OrElse`, `Replay`, and `Lift`.
- **Requirements:** R1-R9.
- **Dependencies:** None.
- **Files:** `src/FunnySharp/DomainProgram.cs`.
- **Approach:** Follow `Effect`'s readonly-struct wrapper and uninitialized-value discipline.
  Store one nullable `StateMachine` delegate; the factory validates; `Update`/`AsStateMachine` throw
  `InvalidOperationException("The domain program has not been initialized.")` for the default.
  `OrElse` and `Replay` call the existing `StateMachineExtensions` members. `Lift` captures the
  child delegate and delegates, routes with `Option.TryGetValue`, maps child outputs into a fresh
  parent array in order with the parent model, calls the setter once, and builds the parent
  `StateChange` with the internal owned-output factory to avoid a second copy. Use
  `Array.Empty<T>()` for zero outputs.
- **Execution note:** Do not add an implicit conversion from `StateMachine` or `DomainProgram`
  (the product contract forbids implicit conversions on normal paths). Do not mutate the child
  change or its output array. Do not call the setter for non-applied results. Document the callback
  order and the shared `TError` constraint.
- **Patterns to follow:** `Effect<T>` struct/default behavior, `StateMachineExtensions.OrElse`/
  `Replay`, `StateChange.FromOwnedOutputs`, `StateTransitionExtensions.Then` output ownership.
- **Test scenarios:** `Create(null)`; default `Update`/`AsStateMachine`/`OrElse`/`Replay`/`Lift`
  throw; `Update` invokes once; `OrElse` applied/rejected/failed/undefined; `Replay` empty,
  applied history, first non-applied, one enumeration, exceptions; `Lift` route Some/None,
  getter/setter/lifter ledgers, applied/rejected/failed/undefined, null delegates, exceptions,
  child reuse in two parents, zero/many commands. `(happy path)`, `(edge)`, `(error path)`.
- **Verification:** Focused `DomainProgramTests` and `DomainProgramLiftTests`; core build.

### U2. Add the execution and transcript values

- **Goal:** Add `EmittedCommand<TCommand>`, `SimulationDelivery<...>`, and
  `CommandExecutionFailure<TCommand, TError>`.
- **Requirements:** R10.
- **Dependencies:** None.
- **Files:** `src/FunnySharp/EmittedCommand.cs`, `SimulationDelivery.cs`,
  `CommandExecutionFailure.cs`.
- **Approach:** Follow `RowOrigin`/`NestedRowError`: readonly structs, internal constructors,
  public properties, `IEquatable` without operators, deterministic `ToString`, no `Location` or
  string allocation unless rendered. `EmittedCommand` carries `Sequence`, `Command`, `EmittedAt`;
  `SimulationDelivery` carries `Message`, `At`, nullable `CommandSequence`, and `Result`;
  `CommandExecutionFailure` carries `Command`, `Error`, and `At`. XML docs state they are
  diagnostics, not carriers.
- **Execution note:** Do not add equality operators (reserved for carriers). Do not expose a
  public constructor that lets a caller fabricate a sequence inconsistent with a simulation; the
  simulation is the producer. A default value is not a valid transcript entry; document that.
- **Patterns to follow:** `RowOrigin`, `NestedRowError`, `LookupError<TKey>`.
- **Test scenarios:** property values, equality/hash, `ToString`, default values, payload
  nullability, transcript correlation fields. `(happy path)`, `(edge)`.
- **Verification:** Focused `ProgramSimulationTests`; core build.

### U3. Add the deterministic simulation driver

- **Goal:** Add `ProgramSimulation<TModel, TMessage, TCommand, TError>`.
- **Requirements:** R11-R17.
- **Dependencies:** U1, U2.
- **Files:** `src/FunnySharp/ProgramSimulation.cs`.
- **Approach:** A sealed class holding the resolved `StateMachine`, the initial model, the current
  model, explicit `DateTimeOffset` now, optional `Random`, and append-only lists for emitted,
  pending, deliveries, and failures. `DeliverCore` invokes once, records, and on `Applied` updates
  and emits. `ReplyCore` validates the sequence, invokes once, records with correlation, updates
  and emits, then removes the pending sequence. `FailCommandCore` validates and removes a pending
  sequence, records the failure, and leaves the model. Sequence lookup is O(1) through the emitted
  list (contiguous sequences from 1); pending removal is a linear scan. `Replay` uses the recorded
  delivery messages and the existing `StateMachine.Replay`.
- **Execution note:** Do not read ambient time/randomness. Do not execute commands. Do not
  auto-deliver a failure message. Do not deduplicate messages/replies. Do not add async,
  concurrency, retries, priority, or a scheduler. Document that the lists are live, single-threaded,
  and must not be mutated during enumeration; the example reads `PendingCommands[0]` in a loop.
- **Patterns to follow:** `BatchScope` explicit bounds/cancellation discipline without its async
  execution; `StateMachineExtensions.Replay`; `StateChange` output ownership.
- **Test scenarios:** construction validation, time monotonicity, deliver applied/rejected/failed/
  undefined, sequence/order/time, pending FIFO and removal, reply correlation/unknown/duplicate/
  late/failed, fail-command pending/unknown/completed, model unchanged on failure, replay purity,
  random optionality, live-list behavior. `(happy path)`, `(edge)`, `(error path)`.
- **Verification:** Focused `ProgramSimulationTests`; full test suite.

### U4. Pin the program and simulation semantics

- **Goal:** Add focused xUnit tests for every contract boundary and the shared comparison.
- **Requirements:** R1-R17.
- **Dependencies:** U1-U3.
- **Files:** `tests/FunnySharp.Tests/DomainProgramTests.cs`,
  `DomainProgramLiftTests.cs`, `ProgramSimulationTests.cs`, `ProgramComparisonTests.cs`,
  `FunnySharp.Tests.csproj`.
- **Approach:** Attribute-only `[Fact]`/`[Theory]`, classic asserts, no skips. Use local records and
  ledgers; assert callback counts and order; assert sibling reference identity; assert exact
  statuses/errors; assert no command execution during replay. The comparison test calls the shared
  verifier once.
- **Execution note:** Deliberate null violations use `null!`. No sleeps or timing asserts. No
  analyzer suppressions outside deliberate-misuse diagnostics. Consume every `TransitionResult`.
- **Patterns to follow:** `StateMachineTests`, `NestedDataExpansionTests`, `NestedDataErrorTests`.
- **Test scenarios:** AE1-AE13 plus argument nullness, default values, callback exceptions,
  repeated calls, source re-enumeration, duplicate/late/unknown sequences, and pending-list
  behavior. `(happy path)`, `(edge)`, `(error path)`, `(integration)`.
- **Verification:** Focused run zero failed and zero skipped; full suite green.

### U5. Build the direct switch baseline, shared comparison, and measurement harness

- **Goal:** Prove fixed scenario behavior, parity, state ownership, and honest costs.
- **Requirements:** R18.
- **Dependencies:** U1-U4.
- **Files:** `tests/Shared/ProgramComparison/OrderModel.cs`, `OrderMessage.cs`,
  `OrderCommand.cs`, `OrderError.cs`, `OrderSimulationContract.cs`,
  `HandWrittenOrderWorkflow.cs`, `HandWrittenOrderSimulation.cs`,
  `FunnySharpOrderPrograms.cs`, `FunnySharpOrderSimulation.cs`, `OrderScenarios.cs`,
  `ProgramComparison.cs`; `tests/FunnySharp.ProgramMeasurements/FunnySharp.ProgramMeasurements.csproj`,
  `Program.cs`, `ProgramMeasurements.cs`, `packages.lock.json`, `FunnySharp.slnx`.
- **Approach:** Share the immutable models, messages, commands, errors, initial models, scenario
  script, expected reports, and `IOrderSimulation`/report contract. The direct switch baseline
  routes parent messages in one switch to per-area update helpers and uses a hand-written
  simulation adapter with the same transcript/correlation contract. The FunnySharp consumer defines
  inventory and payment child programs, lifts them, composes with a parent-level cancellation
  program, and wraps `ProgramSimulation` in an adapter. The comparison runs the fixed scenarios
  through both and asserts fixed expected reports and parity. The measurement executable runs the
  comparison first, then measures composition, dispatch (including the worst-case third-child
  route), one state copy, and the full scenario at N=1,024 steps; it prints one allocation counter
  and directional timing.
- **Execution note:** The baseline must be competent, not a strawman: per-area helpers, pattern
  matching, the same records and command arrays, pre-sized lists, no success-path path strings, no
  LINQ materialization in the hot path. Normalize both sides to the same report records; charge
  the mapping/adapter code to the library consumer. Do not share mutable reports. Disable tiered
  compilation; use `Measure` (thread counter) for every row; run twice and compare allocation
  rows.
- **Patterns to follow:** `tests/Shared/NestedDataComparison`, `JoinMeasurements.cs`,
  `MeasurementHarness`.
- **Test scenarios:** success, late response, duplicate message, stale version, rejected
  transition, failed transition, command failure, command ordering, correlation, state ownership,
  time, randomness, replay purity, mutation; composition/dispatch/state-copy/full-scenario
  measurements. `(integration)`.
- **Verification:** Comparison passes in xUnit and measurement; Release run prints the table twice
  with identical allocation rows.

### U6. Add the compiling example and documentation samples

- **Goal:** Show the complete workflow and provide byte-exact guide snippets.
- **Requirements:** R18.
- **Dependencies:** U1-U3.
- **Files:** `examples/FunnySharp.Examples/DomainProgramExamples.cs`,
  `examples/FunnySharp.Examples/Program.cs`,
  `examples/FunnySharp.DocumentationSamples/DomainProgramSamples.cs`.
- **Approach:** The example defines a compact order workflow, composes child programs, runs a
  successful simulation, asserts command order/correlation, exercises late/duplicate/stale/
  rejected/command-failure paths, shows explicit time/randomness, proves replay does not call a
  fake gateway, and shows the same child lifted into a second parent. The async form shows the
  ordinary `while`/`await` execution loop with `Task`/`ValueTask`, `CancellationToken`, and a
  caller-owned gateway. The documentation samples provide the guide snippets: define/lift, compose/
  update, simulate/reply, explicit time/randomness, failure distinction, replay, and the async
  boundary.
- **Execution note:** The example must build with zero `FS####` diagnostics; consume every
  `TransitionResult`; no suppressions. Keep snippet source and guide byte-identical. The example
  prints the exact `FunnySharp examples passed.` marker.
- **Patterns to follow:** `NestedDataExamples`, `DomainExamples`, `NestedDataSamples`,
  `StateMachineSamples`.
- **Test scenarios:** the example's assertions pass; the docs verifier compares every new snippet.
  `(integration)`.
- **Verification:** `dotnet run --project examples/FunnySharp.Examples -c Release`;
  `verify-docs-snippets`.

### U7. Reconcile docs, baseline, manifests, and goal state

- **Goal:** Ship coherent documentation and committed surface records.
- **Requirements:** R18.
- **Dependencies:** U1-U6.
- **Files:** `docs/domain-programs.md`, `docs/domain-programs-evidence.md`, `docs/grammar.md`,
  `docs/product-contract.md`, `docs/stability-inventory.md`, `docs/performance.md`,
  `docs/state-machines.md`, `docs/AGENTS.md`, `docs/plans/AGENTS.md`, `docs/goals/README.md`,
  `docs/goals/AGENTS.md`, `docs/goals/archive/0037-goal.md`, `AGENTS.md`, `README.md`,
  `README_zh-cn.md`, `src/FunnySharp/AGENTS.md`, `tests/FunnySharp.Tests/AGENTS.md`,
  `examples/FunnySharp.Examples/AGENTS.md`,
  `examples/FunnySharp.DocumentationSamples/AGENTS.md`,
  `eng/api-baseline/FunnySharp.public-api.txt`, `eng/performance/baseline.json`,
  `eng/performance/competitor-baseline.json`, `eng/harness/DocsSnippets.fs`,
  `tests/FunnySharp.Harness.Tests/DocsSnippetsTests.fs`.
- **Approach:** The guide starts with `## API Shape`, covers the pure program, lifting, `OrElse`
  composition, deterministic simulation, late/duplicate/stale/rejected/failure visibility, replay
  and mutation, the ordinary async execution boundary, costs, and domain-value compatibility, and
  closes with `## Deliberate Boundaries`. Grammar adds the program family as descriptions and the
  new verbs; the product contract adds the program paragraph and clarifies the existing
  "no workflow engine" sentence without weakening it. Stability inventory adds the family row.
  Performance adds characteristic rows and the evidence link. The guide joins the primary-guide
  and snippet corpus; hardcoded counts in harness tests and AGENTS files are updated. The API
  baseline is regenerated with `--write` after the surface is final. Both performance manifests add
  the new source paths in ordinal order. Goal 0037 moves unchanged to the archive and the index
  removes it from Next-Stage.
- **Execution note:** Do not hand-edit generated inventory, archived goal contents, or historical
  release evidence. Archive is a rename of the unchanged file. Do not modify Goal 0038. Grep the
  constitutional prose for blanket workflow/actor/scheduler/event-sourcing claims and reconcile
  only the ones the pure composition and deterministic driver falsify; do not weaken the runtime
  exclusions.
- **Patterns to follow:** Goal 0036 landing shape (guide/evidence/example/baseline/manifests/
  archive/index).
- **Test scenarios:** API baseline, docs snippets, format, manifest shape, example run, full test
  gate. `(integration)`.
- **Verification:** `verify-api-baseline`, `verify-docs-snippets`, `format`, full `test`.

## Verification Contract

| Gate | Command | Applies to | Completion evidence |
| --- | --- | --- | --- |
| Build | `dotnet fsi build.fsx -- -p build` | U1-U7 | exit 0, zero warnings/errors |
| Test | `dotnet fsi build.fsx -- -p test` | U1-U7 | all tests passed, zero failed, zero skipped |
| Format | `dotnet fsi build.fsx -- -p format` | U1-U7 | exit 0 |
| Docs snippets | `dotnet fsi build.fsx -- -p verify-docs-snippets` | U6, U7 | exit 0 with the new guide and snippets in the corpus |
| API baseline | `dotnet fsi build.fsx -- -p verify-api-baseline` (after `--write`) | U7 | exit 0 with only the new members |
| Example | `dotnet run --project examples/FunnySharp.Examples/FunnySharp.Examples.csproj -c Release` | U6 | exact `FunnySharp examples passed.` |
| Comparison and measurement | `dotnet run --project tests/FunnySharp.ProgramMeasurements/FunnySharp.ProgramMeasurements.csproj -c Release` | U5, U6 | comparison passes; allocation and timing tables captured twice with identical allocation |
| Manifest shape | full test suite (`PerformanceManifestTests`) | U7 | source lists sorted and complete |

## Definition of Done

### Per-Unit Completion

| Unit | Done when |
| --- | --- |
| U1 | The program step, `OrElse`, `Replay`, and `Lift` produce the documented statuses/errors, preserve command order and parent ownership, and reuse the existing state-machine operations; default values fail explicitly. |
| U2 | The execution/diagnostic values expose deterministic fields, equality, and text; construction allocates only the value; no carrier or operator is added. |
| U3 | The simulation records deliveries, commands, and failures with explicit time and correlation; pending FIFO and late/duplicate/failure paths match the contract; replay is pure; no ambient source or execution exists. |
| U4 | Focused tests pass with zero failed and zero skipped; the full suite remains green. |
| U5 | The direct switch baseline and program composition agree on fixed reports for every scenario; state ownership, ordering, correlation, time, randomness, and replay are asserted; the measurement prints real costs twice with identical allocation. |
| U6 | The example prints the exact success marker; guide snippets byte-match the documentation sample regions; analyzer diagnostics are clean. |
| U7 | The guide closes with `## Deliberate Boundaries`; product contract, grammar, inventory, performance, baseline, manifests, READMEs, and goal archive agree; Goal 0038 is untouched. |

### Global Completion

- R1-R18 each map to an implementation artifact and an executed verification result.
- AE1-AE15 pass in the test suite, the comparison, or the evidence run.
- The evidence records late responses, duplicate messages, stale versions, rejected transitions,
  failed transitions, command failures, command ordering, message correlation, state ownership,
  explicit time, explicit randomness, replay purity, and mutation boundaries.
- The evidence records complete consumer-code line counts, composition/dispatch/state-copy/
  full-scenario allocation, and directional timing against a competent direct switch baseline.
- The evidence records the maintainer judgment on whether the program composition and deterministic
  driver reduce the complete workflow and clarify failure, including a sceptical boundary for a
  single small workflow where the direct switch is simpler or leaner.
- No new carrier, runtime dependency, scheduler, actor runtime, persistence layer, event-sourcing
  framework, distributed recovery, exactly-once claim, automatic transaction, automatic command
  execution, ambient time/randomness, deep copy, universal program representation, or Goal 0038
  behavior is introduced.
