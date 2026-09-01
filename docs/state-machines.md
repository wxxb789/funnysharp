# State Machines

`FunnySharp` provides small, pure building blocks for state transformations and finite-state
workflows. They make a transition's next state, emitted output commands, invalid events, explicit
failures, and missing handlers visible in ordinary C# types. The compiling approval-workflow
example is in [examples/FunnySharp.Examples/Program.cs](../examples/FunnySharp.Examples/Program.cs).

## State Changes And Transitions

`StateChange<TState, TOutput>` is a sealed data object that pairs a next `State` with its emitted
`Outputs`. Create it with `StateChange<TState, TOutput>.To(...)`. Output collections supplied to
the factory are snapshotted, so later mutations do not alter a completed change. A change carries
data only: constructing or composing it does not execute an output command. The snapshot is shallow;
mutable output objects remain owned by the caller.

`StateTransition<TState, TOutput>` is the standard delegate for a total pure state transformation:

```csharp
StateTransition<Account, AuditCommand> submit = account =>
    StateChange<Account, AuditCommand>.To(
        account with { Status = AccountStatus.Submitted },
        new StoreAccount(account.Id),
        new NotifyReviewer(account.Id));
```

Use `Then` to compose transitions. The first transition runs once, its resulting state becomes the
second transition's input, and their outputs are concatenated in execution order. `Then` does not
catch exceptions or turn them into a transition result; ordinary exceptions retain their identity
and the second transition is not run after a first-transition exception.

Purity is the delegate's contract rather than something the runtime can enforce. A transition that
reads the clock, randomness, I/O, or mutable global state can produce different results during replay;
put those inputs in the state or event, or emit a command that the caller executes later.

## State-Machine Results

`StateMachine<TState, TEvent, TOutput, TError>` is a delegate from a current state and event to a
`TransitionResult<TState, TOutput, TError>`. A result always has exactly one
`TransitionStatus`:

- `Applied` contains a `StateChange<TState, TOutput>`.
- `Rejected` contains a typed error for a known event that is not valid in the current state.
- `Failed` contains a typed error for a defined transition that cannot be completed.
- `Undefined` contains no change or error and means this machine has no handler for the state/event
  pair.

Create results with `Applied`, `Rejected`, `Failed`, and `Undefined`. Inspect them with `Status`,
the corresponding `IsApplied`, `IsRejected`, `IsFailed`, or `IsUndefined` property,
`TryGetChange`, `TryGetError`, or exhaustive `Match`. A rejected or failed result contains only an
error; an undefined result contains neither payload. `default(TransitionResult<...>)` is
`Undefined`.

The distinction between rejection and undefined handling is deliberate. A lifecycle handler can
reject an approval attempted before submission, while another independently owned handler may be
allowed to define a later event such as access revocation. Use `Undefined` for the latter kind of
dispatch miss, not as a silent success.

## Composition And Replay

`OrElse` combines two state machines by trying the fallback only when the first returns
`Undefined`. An `Applied`, `Rejected`, or `Failed` result passes through unchanged. This permits
small handlers to own their state/event pairs without replacing clear tuple and pattern switches
with deeply nested conditionals.

`Replay(initialState, events)` applies a state machine to an `IEnumerable<TEvent>` in source order.
It enumerates the history once, feeds each applied change's next state into the next event, and
collects emitted outputs in order. A successful replay returns an applied
`TransitionResult<TState, TOutput, TError>` whose `StateChange` contains the final state and the
materialized output history. Empty histories succeed with the initial state and an empty output
list.

Replay stops at the first `Rejected`, `Failed`, or `Undefined` result and preserves that status and
error; state changes and outputs staged by earlier events are not returned from a failed replay. It
does not swallow exceptions from the event history or machine. For a pure machine, retaining the
event values is enough to reproduce its workflow state without adding persistence, event sourcing,
concurrency control, or a workflow runtime to the library.

## Async Boundary

State machines intentionally have no async executor. Model an output as a command value, let the
pure transition produce it, and execute it only at a visible application boundary. That boundary
owns I/O, ordering, retries, error handling, and cancellation:

```csharp
var commands = result.Match(
    change => change.Outputs,
    rejection => throw new InvalidOperationException(rejection.Code),
    failure => throw new InvalidOperationException(failure.Code),
    () => throw new InvalidOperationException("No handler matched."));

foreach (var command in commands)
{
    await ExecuteCommandAsync(command, cancellationToken);
}
```

The caller supplies and observes its `CancellationToken`; the pure machine never executes a
command or inspects cancellation. This keeps replay and transition tests deterministic while
making the actual asynchronous effects easy to locate and review.

## Deliberate Boundaries

This surface is not a workflow engine, actor system, scheduler, persistence layer, distributed
orchestrator, or general discriminated-union framework. It does not add retries, serialization,
event storage, compensation, concurrency rules, an asynchronous state-machine type, or implicit
effect execution. Use ordinary .NET services at the explicit output-execution boundary when those
capabilities are needed.
