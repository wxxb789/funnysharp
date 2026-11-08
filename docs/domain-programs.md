# Composable Domain Programs And Deterministic Simulation

`FunnySharp` advances the existing pure state-machine surface into reusable business programs.
A `DomainProgram<TModel, TMessage, TCommand, TError>` names the model, message, command, and error
roles and keeps the update pure: one message produces the next model and descriptions of work.
`Lift` embeds a child program into a parent model, `OrElse` composes independently owned decisions,
and `ProgramSimulation` drives the program through an explicit, single-threaded scenario without
executing a command or reading an ambient clock. The compiling workflow is in
[DomainProgramExamples.cs](../examples/FunnySharp.Examples/DomainProgramExamples.cs); the direct
switch baseline, fixed scenarios, consumer counts, and measured costs are in
[domain-programs-evidence.md](domain-programs-evidence.md).

## API Shape

`DomainProgram` is a description, not a runtime. `ProgramSimulation` is a deterministic test and
inspection driver, not a scheduler. `EmittedCommand`, `SimulationDelivery`, and
`CommandExecutionFailure` are execution and diagnostic values. None is an outcome carrier; the
canonical outcomes remain `TransitionResult`, `Option`, `Result`, `UnitResult`, and `Validation`.

| Member | Receiver / source | Result |
| --- | --- | --- |
| `Create(update)` | non-generic `DomainProgram` factory | `DomainProgram<TModel, TMessage, TCommand, TError>` |
| `Update(model, message)` | `DomainProgram` | `TransitionResult<TModel, TCommand, TError>` |
| `AsStateMachine()` | `DomainProgram` | The underlying `StateMachine<TModel, TMessage, TCommand, TError>` |
| `OrElse(fallback)` | `DomainProgram` | A program that runs the fallback only after `Undefined` |
| `Replay(initialModel, messages)` | `DomainProgram` | The existing state-machine replay result |
| `Lift(getChild, setChild, route, liftCommand)` | `DomainProgram` | A parent program for one child substate |
| `Deliver(message)` / `Deliver(message, at)` | `ProgramSimulation` | The transition result; the delivery is recorded |
| `Reply(commandSequence, message)` / `Reply(..., at)` | `ProgramSimulation` | The transition result; the delivery is correlated |
| `FailCommand(commandSequence, error)` / `FailCommand(..., at)` | `ProgramSimulation` | A recorded `CommandExecutionFailure` |
| `AdvanceTo(at)` / `AdvanceBy(by)` | `ProgramSimulation` | Moves the explicit clock forward only |
| `Replay()` | `ProgramSimulation` | The existing replay over the recorded delivery messages |
| `Model`, `Now`, `Random` | `ProgramSimulation` | Current model, explicit time, optional caller-supplied random source |
| `EmittedCommands`, `PendingCommands` | `ProgramSimulation` | Ordered emitted commands; FIFO commands not yet replied to or failed |
| `Deliveries`, `CommandFailures` | `ProgramSimulation` | Ordered delivery and execution-failure transcripts |

The default `DomainProgram` value is uninitialized. Every operation on it throws
`InvalidOperationException("The domain program has not been initialized.")`; use the factory.

## The Program Is A Pure Update

`Update` invokes the supplied state-machine update exactly once. It does not mutate the model,
execute a command, read a clock, read a random source, or catch an exception. The update returns
the existing `TransitionResult` vocabulary:

- `Applied` contains the next model and ordered command values.
- `Rejected` means the program recognized the message and refused it for the current model.
- `Failed` means a defined transition could not complete.
- `Undefined` means this program has no handler for the model/message pair.

Commands are values. Constructing, inspecting, ordering, or replaying them never executes work.
The caller owns persistence, time, randomness, external I/O, and command execution. A program is
opt-in for one workflow; it does not move an application into a universal program representation.

## Lifting A Child Program

`Lift` embeds a child program into a parent model through four caller-supplied delegates:

- `getChild` reads the child substate from the parent.
- `setChild` replaces that substate in the parent. It is the only parent write.
- `route` maps a parent message to the child message it owns, or returns `Option.None`.
- `liftCommand(parent, childCommand)` maps one child command to one parent command. It receives the
  parent model before the child's next state is applied, so it can add aggregate identity or other
  parent context without duplicating those fields in every child state.

`None` returns `Undefined` without invoking the getter, child update, command lifter, or setter.
A routed message invokes the getter and child update once. An applied child change invokes the
command lifter once per child output in order, then the setter once with the child's next state.
A rejected, failed, or undefined child result passes through unchanged and invokes neither the
lifter nor the setter. The child and parent share `TError`; a consumer with distinct child error
types maps them into the shared hierarchy before lifting.

The parent is copied only by the caller's setter. The child update receives the substate reference
and must not mutate it. Sibling substate references are preserved unless the setter changes them.
The same child program can be lifted into different parents with different getters, setters,
routes, and command lifters.

<!-- documentation-sample: DocumentationSamples.DomainPrograms.DefineAndLift -->
```csharp
var inventory = DomainProgram.Create<ExampleInventory, ExampleInventoryMessage,
    ExampleInventoryCommand, ExampleError>(UpdateInventory);
var order = inventory.Lift<ExampleOrder, ExampleMessage, ExampleCommand>(
    static model => model.Inventory,
    static (model, inventory) => model with { Inventory = inventory },
    static message => message switch
    {
        ExampleReserve reserve => Option.Some<ExampleInventoryMessage>(
            new ExampleInventoryRequested(reserve.Quantity, reserve.Version)),
        ExampleReserved reserved => Option.Some<ExampleInventoryMessage>(
            new ExampleInventoryAnswered(reserved.Version, reserved.Accepted)),
        _ => Option.None<ExampleInventoryMessage>(),
    },
    static (parent, command) => command switch
    {
        ExampleInventoryReserveCommand reserve => new ExampleReserveCommand(
            parent.OrderId,
            reserve.Sku,
            reserve.Quantity,
            reserve.Version),
        _ => throw new InvalidOperationException("Unexpected inventory command."),
    });

var applied = order.Update(
    InitialOrder(),
    new ExampleReserve("ORD-1", "SKU-1", 2, 100));
var commandCount = applied.Match(
    static change => change.Outputs.Count,
    static _ => -1,
    static _ => -2,
    static () => -3);
Console.WriteLine($"{commandCount} command(s) from the lifted child.");
```

## Composing Decisions

`OrElse` delegates to the existing state-machine operation. It invokes the fallback only when the
first program returns `Undefined`; `Applied`, `Rejected`, and `Failed` are defined answers and pass
through unchanged. A route should claim only the messages its child owns. A routed message that the
child rejects does not fall through to another program.

Independently understandable inventory and payment programs can be lifted into an order model while
a cancellation decision is written directly over the parent model because it needs several
substates. The composition is one `OrElse` chain; no router type, message dictionary, or duplicated
parent switch is added.

<!-- documentation-sample: DocumentationSamples.DomainPrograms.ComposeAndUpdate -->
```csharp
var program = CreateProgram();
var result = program.Update(
    InitialOrder(),
    new ExampleReserve("ORD-1", "SKU-1", 2, 100));
var summary = result.Match(
    static change => $"Applied with {change.Outputs.Count} command(s).",
    static rejection => "Rejected: " + rejection,
    static failure => "Failed: " + failure,
    static () => "Undefined.");
Console.WriteLine(summary);
```

## Deterministic Simulation

`ProgramSimulation` owns the current model, the explicit `DateTimeOffset` clock, the optional
caller-supplied `Random`, and the transcripts. It never reads `DateTimeOffset.UtcNow`,
`DateTime.UtcNow`, `Random.Shared`, or another ambient source. `AdvanceTo` and `AdvanceBy` move the
clock forward only; a backwards instant or negative interval is rejected. The `...At` overloads
validate the time and, for replies and failures, the command sequence before advancing;
`FailCommand` additionally validates that the command is still pending. If the update throws, the
clock is restored and the model and transcripts are unchanged.

- `Deliver` invokes the program once and records a delivery. `Applied` updates the model and
  appends commands with monotonic one-based sequences; `Rejected`, `Failed`, and `Undefined` leave
  the model and pending commands unchanged.
- `Reply` validates that the sequence was emitted, invokes the program once, and records the
  delivery with that correlation. A reply to a command that was already replied to or failed is
  allowed: duplicate and late responses are delivered and the program decides whether they are
  stale, duplicate, unexpected, rejected, or applied. An unknown sequence is a driver error.
- `FailCommand` validates that the sequence was emitted and still pending, removes it from pending,
  records a `CommandExecutionFailure`, and leaves the model unchanged. An already completed or
  failed command is a driver error. The caller may then deliver a domain message; the simulation
  never invents a failure reply.
- `EmittedCommands` is append-only in emission order. `PendingCommands` is FIFO in emission order;
  new commands emitted by a reply append after the commands already pending. No scheduler,
  priority, retry, or execution policy is added.

The simulation's command `Sequence` is execution correlation for the transcript. A domain request
version carried inside a command and reply is stale-response correlation. They are deliberately
separate: the simulation never interprets the domain version, and the program never sees the
simulation sequence.

<!-- documentation-sample: DocumentationSamples.DomainPrograms.SimulateAndReply -->
```csharp
var simulation = new ProgramSimulation<ExampleOrder, ExampleMessage, ExampleCommand,
    ExampleError>(CreateProgram(), InitialOrder(), StartedAt);
if (!simulation.Deliver(new ExampleReserve("ORD-1", "SKU-1", 2, 100))
    .TryGetChange(out _))
{
    throw new InvalidOperationException("The reserve message must apply.");
}

var emitted = simulation.PendingCommands[0];
if (!simulation.Reply(
        emitted.Sequence,
        new ExampleReserved("ORD-1", 100, true))
    .TryGetChange(out _))
{
    throw new InvalidOperationException("The inventory reply must apply.");
}

Console.WriteLine(
    $"{simulation.EmittedCommands.Count} command(s), "
    + $"{simulation.Deliveries.Count} delivery/deliveries.");
```

## Late, Duplicate, Stale, Rejected, And Failed

The deterministic scenarios cover the outcomes the contract names. The shared comparison fixes
expected command order, correlation, model state, and errors for each one.

| Scenario | Visible outcome |
| --- | --- |
| late response | A reply to an older command is delivered with its old sequence; the program rejects the stale domain version. |
| duplicate message | The message is delivered again; the program rejects a duplicate or unexpected response instead of silently applying it twice. |
| stale request version | The reply carries an older version than the model's pending version; the program returns a `StaleResponse` rejection. |
| rejected transition | The program recognizes the message and refuses it for the current model; the model and pending commands are unchanged. |
| failed transition | A defined transition cannot complete; the result is `Failed`, not a command execution failure. |
| command execution failure | `FailCommand` records a `CommandExecutionFailure`, removes the command from pending, and leaves the model unchanged. |

An execution failure is not a domain transition. The program accepted a transition and emitted a
command; the caller's boundary failed to perform it. The caller may deliver a domain message after
the failure if the workflow has one.

<!-- documentation-sample: DocumentationSamples.DomainPrograms.FailureDistinction -->
```csharp
var simulation = new ProgramSimulation<ExampleOrder, ExampleMessage, ExampleCommand,
    ExampleError>(CreateProgram(), InitialOrder(), StartedAt);
if (!simulation.Deliver(new ExampleCharge("ORD-1", 25m, 200, simulation.Now))
    .TryGetChange(out _))
{
    throw new InvalidOperationException("The charge message must apply.");
}

var charge = simulation.PendingCommands[0];
var failure = simulation.FailCommand(
    charge.Sequence,
    new ExampleExecutionUnavailable("gateway"));
var rejected = simulation.Deliver(new ExampleCancel("ORD-1", 300));
var kind = rejected.Match(
    static _ => "applied",
    static _ => "rejected",
    static _ => "failed",
    static () => "undefined");
Console.WriteLine(
    $"Execution failure #{failure.Command.Sequence}; domain outcome: {kind}.");
```

## Explicit Time And Randomness

Time and randomness are inputs, not ambient services. A decision that depends on time carries the
time in its message or model; the simulation clock records when the step happened. A scenario that
needs randomness supplies a seeded `Random`; the simulation exposes it but never consumes it. The
same seed produces the same external replies and transcript; a different seed can produce a
different fixed outcome. The program itself never reads either source.

<!-- documentation-sample: DocumentationSamples.DomainPrograms.ExplicitTimeAndRandomness -->
```csharp
var random = new Random(2);
var simulation = new ProgramSimulation<ExampleOrder, ExampleMessage, ExampleCommand,
    ExampleError>(CreateProgram(), InitialOrder(), StartedAt, random);
if (!simulation.Deliver(new ExampleCharge("ORD-1", 25m, 200, simulation.Now))
    .TryGetChange(out _))
{
    throw new InvalidOperationException("The charge message must apply.");
}

var charge = simulation.PendingCommands[0];
var captured = simulation.Random!.Next(2) == 0;
if (!simulation.Reply(
        charge.Sequence,
        new ExampleCharged("ORD-1", 200, captured, captured ? null : "declined"))
    .TryGetChange(out _))
{
    throw new InvalidOperationException("The payment reply must apply.");
}

simulation.AdvanceBy(TimeSpan.FromMinutes(6));
var timeout = simulation.Deliver(new ExampleTimeout("ORD-1", simulation.Now));
Console.WriteLine(
    $"Captured: {captured}; timeout outcome: {timeout.Status}; now: {simulation.Now:O}.");
```

## Replay And Side Effects

`Replay` uses the existing state-machine replay over the recorded delivery messages from the
initial model. It enumerates the messages once, stops at the first `Rejected`, `Failed`, or
`Undefined` result, and preserves that status and error. An empty transcript succeeds with the
initial model and no outputs. The simulation transcript remains the record of any later steps after
the first non-applied result.

Replay invokes only the pure update. It never executes, retries, or re-records commands or
execution failures. Emitted commands are outputs, not replay inputs, so mutating an emitted command
after the simulation cannot change the replayed state or command shape. The recorded messages and
the initial model are caller-owned inputs retained by reference; mutating them can change a later
replay. Replay is therefore not immune to caller mutation of its inputs: callers rely on
value-immutable messages and models for reproducibility, and the library does not deep-copy caller
payloads.

<!-- documentation-sample: DocumentationSamples.DomainPrograms.Replay -->
```csharp
var simulation = new ProgramSimulation<ExampleOrder, ExampleMessage, ExampleCommand,
    ExampleError>(CreateProgram(), InitialOrder(), StartedAt);
if (!simulation.Deliver(new ExampleReserve("ORD-1", "SKU-1", 2, 100))
    .TryGetChange(out _))
{
    throw new InvalidOperationException("The reserve message must apply.");
}

var emittedCount = simulation.EmittedCommands.Count;
var replayed = simulation.Replay();
if (!replayed.TryGetChange(out var change))
{
    throw new InvalidOperationException("The replay must apply.");
}

Console.WriteLine(
    $"Replayed {change.Outputs.Count} command(s); "
    + $"recorded commands unchanged: {simulation.EmittedCommands.Count == emittedCount}.");
```

## Executing Commands With Ordinary .NET

The library ships no in-process execution adapter. The ordinary `while`/`await` loop is the
adapter: it reads pending commands in order, executes them with the caller's own dependency and
cancellation token, delivers the reply, and records an execution failure explicitly. `Task` and
`ValueTask`, `CancellationToken`, exception identity, and dependency ownership stay at the
boundary. A library adapter would need a failure-to-message mapping and a stop/continue policy,
which is a scheduler-shaped decision this surface does not own.

<!-- documentation-sample: DocumentationSamples.DomainPrograms.AsyncBoundary -->
```csharp
while (simulation.PendingCommands.Count > 0)
{
    var emitted = simulation.PendingCommands[0];
    try
    {
        var reply = await gateway.ExecuteAsync(
            emitted.Command,
            cancellationToken);
        if (!simulation.Reply(emitted.Sequence, reply).TryGetChange(out _))
        {
            throw new InvalidOperationException("The reply must apply.");
        }
    }
    catch (ExampleGatewayException exception)
    {
        var failure = simulation.FailCommand(
            emitted.Sequence,
            new ExampleExecutionUnavailable(exception.Message));
        Console.WriteLine(
            $"Command #{failure.Command.Sequence} failed: {failure.Error}.");
    }
}
```

## Domain Values And Existing Capabilities

The program surface is generic over its model, message, command, and error types, so Goal 0030's
validated domain values compose at the boundary without becoming a mandatory replacement type
system. A validated identifier can be carried in a command or message exactly like any other value.
`OrElse` and `Replay` remain the existing state-machine operations; `DomainProgram` names their
business-program role and adds lifting and the deterministic driver, not a second fallback or
replay meaning.

<!-- documentation-sample: DocumentationSamples.DomainPrograms.DomainValueCompatibility -->
```csharp
var validated = OrderIds.Validate("ORD-1");
if (!validated.TryGetValue(out var orderId))
{
    throw new InvalidOperationException("The order id must validate.");
}

var command = new ExampleReserveCommand(orderId.Value, "SKU-1", 2, 100);
Console.WriteLine($"{command.OrderId}: {command.Quantity} unit(s).");
```

## Costs

The complete consumer workflow is compared with a competent direct switch-based implementation and
its hand-written simulation adapter. The shared fixtures, models, messages, commands, errors,
scenario script, and report contract are not charged to either side. The measurement uses one
allocation counter, one JIT tier, and two runs; the evidence table records the exact numbers.

The library is **larger** than the direct switch baseline in every measured row:

- composition allocates the `Lift`/`OrElse` closures once;
- dispatch pays the route chain and the child program's extra `StateChange` and output snapshot;
- a single applied child update copies the child record and the parent once, exactly as the direct
  switch's nested `with` does; the library additionally creates the child `StateChange` and output
  snapshot before the parent setter;
- the full scenario adds the normalized transcript mapping from the core simulation types to the
  shared report types;
- replay pays the same composed-update cost as dispatch.

The case for the surface is the reusable child program, declarative routing, explicit state
ownership, and the shipped deterministic driver that makes late, duplicate, stale, rejected, and
failed scenarios reproducible. It is not an allocation win over a well-written direct switch. The
full comparison and the honest direction are in
[domain-programs-evidence.md](domain-programs-evidence.md).

## Deliberate Boundaries

This surface is not a workflow engine, actor runtime, mailbox, scheduler, persistence layer,
event-sourcing framework, distributed orchestrator, or exactly-once effect system. It does not add
durable event storage, snapshots, serialization, distributed recovery, automatic transactions,
retries, compensation, automatic command execution, ambient time or randomness, deep payload
copying, a universal application program representation, or Goal 0038 concurrency. The simulation is
single-threaded and not thread-safe; its transcript retains every delivery and emitted command and
is not a bounded production event store. `Replay` stops at the first non-applied result, exactly
like the existing state-machine replay; the simulation transcript is the record of later steps. A
consumer that needs a second replay policy or a failure-to-message execution adapter must supply it
at its own boundary. `Lift` shares `TError` between child and parent, maps each child command to one
parent command, and routes one parent message to at most one child; error mapping, command fan-out,
and broadcast are caller policy.
