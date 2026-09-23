# Goal 19 Call-Site Comparison Evidence

Follow-up evidence for the Goal 19 advanced-pattern curation, in the footsteps of
`docs/next-stage/call-sites-goal-15.md` and `docs/next-stage/call-sites-goal-16.md`. Goal 19
curates the advanced functional patterns against the Goal 14 decision record: the effect and
resource boundary (AD-7), the pure state-transition and commands-as-data model (AD-8), the
optics surface (AD-9), and the async coordination family (AD-6). The pinned Goal 14 record
already carries compiled comparisons for the effect family (W9: environment + resource
scope) and the optics family (W10: single-site first use), and Goal 18 delivered the
completion-order coordination (G7). What was missing is compiled comparative evidence for
the state family and for the optics adoption scenario the decision record names ("lenses
are for paths updated in more than one place"). This document adds two workflows, numbered
**WF-19A** and **WF-19B**, each exercising one of those gaps against a compiled idiomatic
C# baseline.

This is evidence, not a decision. It does not replace the Goal 14 pinned comparisons and it
does not restate any W1–W11 or WF-1..WF-7 count as authoritative: `call-sites.md` remains
the record for W1–W11, `call-sites-goal-15.md` for its three workflows, and
`call-sites-goal-16.md` for its seven; nothing in those files was modified. Both scratch
projects build with 0 warnings and 0 errors (§8); every code block below is byte-identical
to the committed scratch code, and each names its file.

## 1. Provenance

| Item | Pin used here |
| --- | --- |
| Date | 2026-09-23 |
| HEAD commit | `4a85d1b63b60680f8a5fa8de1eff690b52209977` (`fix(deps): pin the implicit ILLink.Tasks reference in the trimmable projects`), branch `main` |
| Goal 19 surface | the working tree of `src/FunnySharp` at evidence time — identical to HEAD for `src/` (Goal 19 adds only tests and documentation in this evidence); the exact compiled surface is pinned by the `FunnySharp.dll` SHA256 below |
| FunnySharp reference | local `ProjectReference` to `/home/azureuser/repos/funnysharp/src/FunnySharp/FunnySharp.csproj` (absolute path intentionally machine-specific in the scratch copy, the Goal 14/15/16 pattern); built `FunnySharp.dll` SHA256 `9b63b3acc15f2f8af5a20ac5189c8c2674d77d5a1800f35d7b079ada10ea5c12` |
| FunnySharp variants | `docs/next-stage/call-sites-code/funnysharp/Goal19Workflows.cs` (namespace `CallSites.FunnySharp`, class `Goal19Workflows`), compiled by `funnysharp/FunnySharpCallSites.csproj`; no Goal 14/15/16 file was modified |
| Idiomatic baselines | additive class `Goal19Workflows` plus harness in `docs/next-stage/call-sites-code/idiomatic/Workflows.cs`, compiled by `idiomatic/Idiomatic.csproj` |
| .NET SDK | 10.0.400 (`dotnet --version`) |

Exact build commands and results are in §8.

## 2. Counting rule used here

Same rule as `call-sites.md` §2 and `call-sites-goal-16.md` §2: **S** is one per statement
(`if`, `else`/`else if`, loop headers, `return`, `throw`, local declaration with
initializer, assignment, `using`, `await foreach`, `yield return`, `catch`, invocation
statements, and the body of an expression-bodied member), **O** is one per operation inside a
statement (method call, `new`, indexer, `with` update, `await`, `is`/relational pattern,
`?:`, `??`, `&&`, `||`, `!`, comparison/arithmetic operator, interpolated string,
collection expression), and property/field access, `ConfigureAwait(false)`, declaration
headers, braces, blank lines, and comments are excluded. Harness types and shared helper
bodies are excluded from every count, exactly as `Domain.cs` is in the Goal 14 record; their
*invocations* count as operations.

Six clarifications specific to the Goal 19 workflows, stated so a reviewer can disagree with
a specific element rather than a total:

1. **A switch expression that is the body of an expression-bodied member counts as that one
   S.** Each arm is an expression, not a statement; only its operations are counted.
2. **A static field declaration with an initializer counts as 1 S** (the "local declaration
   with initializer" category), whether the initializer is a switch-expression lambda (the
   machine values) or a factory call (the lens values). `loc.py` cannot extract field
   declarations, so field raw LOC is hand-counted from the committed file, per the W10
   practice of hand-counted one-time lens definitions.
3. **Each pattern test in a switch arm counts as 1 O** (the `is`-pattern category): a
   positional/type pattern (`Triage(var assignee)`) and a constant pattern (`Escalate`)
   each count 1, and each `or` between patterns counts 1 (the `||` category). A discard
   `_` counts 0. The matched tuple `(ticket.Status, @event)` counts 1 O (the collection
   expression category).
4. **A `throw` expression inside a `Match` arm counts as 1 S** (the `throw` category)
   and its `new` counts 1 O; a lambda arm without a throw (`change => change.Outputs`)
   counts 0 S.
5. **The null-forgiving `!` (`decision.Ticket!`) is a compiler annotation, not an
   operation**, and counts 0 O; the logical `!` is not used in these workflows.
6. **The idiomatic `TicketDecision` envelope is counted, not excluded as harness.** It is
   the idiomatic replacement for the library's `TransitionResult`/`StateChange` values, so
   excluding it would understate the idiomatic cost; it is reported as a one-time definition
   (6 semantic, 19 raw) in the WF-19A table, and a reviewer who instead excludes it entirely
   reads the idiomatic total as 92 semantic / 71 raw — the comparison verdict does not
   change.

Raw LOC is measured per method with `python3 tools/loc.py <file> <method>` (non-blank,
non-comment lines including the declaration line), per the Goal 15/16 practice. Semantic LOC
is directional, hand-derived evidence, not an integer-precise metric; the S+O breakdown is
shown in every table so disagreement localizes to one element.

## 3. Summary

| Workflow | Variant | S | O | Semantic | Raw |
| --- | --- | ---: | ---: | ---: | ---: |
| WF-19A ticket escalation (decision + execution + replay) | idiomatic C# (incl. one-time envelope) | 22 | 76 | **98** | 71 + 19 |
| | FunnySharp (`StateMachine`/`OrElse`/`Replay`) | 11 | 64 | **75** | 69 |
| WF-19B two-site nested update | idiomatic C# (path spelled at each site) | 2 | 8 | **10** | 22 |
| | FunnySharp (entries only, composed lens) | 2 | 5 | **7** | 11 |
| | FunnySharp (entries + one-time lens definitions) | 7 | 12 | **19** | 11 + 16 |

Net: the state family is the one advanced-pattern family where the library owns the whole
protocol around the customer's own decision logic, and the count shows it — the decision
switches themselves are identical in size (55 = 55 semantic), while the envelope, the
delegation, and the replay protocol shrink from 43 semantic to 20. WF-19B honestly shows the
opposite at two sites: the lens pays 9 extra semantic up front and wins per site (3–4 vs 4–6
semantic), so its LOC break-even is ≈7–8 sites; its two-site value is the single named path
that cannot drift between sites, not LOC. Per-workflow honesty is in §4–§5; the net
assessment is §6.

## 4. WF-19A — Ticket escalation: pure decision, commands as data, replay

**Scenario.** A support-ticket escalation workflow owned by two teams. The ticket team owns
the lifecycle: `Triage`, `Escalate`, `Resolve`, `Close`; the compliance team owns
`Reopen` for closed tickets. Each applied transition emits commands (`NotifyAssignee`,
`PageOnCallEngineer`, `PublishResolution`, `ArchiveTicket`) that must execute later at a
caller-owned boundary with its own cancellation, never inside the decision; an invalid
transition is a typed rejection, not an exception; and the complete event history must
reproduce the final state and the full command log deterministically.

| Variant | S | O | Semantic | Raw |
| --- | ---: | ---: | ---: | ---: |
| Idiomatic C# (decision + execution + replay) | 19 | 73 | **92** | 71 |
| Idiomatic C# one-time envelope (`TicketDecision`) | 3 | 3 | **6** | 19 |
| FunnySharp (machines + execution + replay) | 11 | 64 | **75** | 69 |

Idiomatic C# — `docs/next-stage/call-sites-code/idiomatic/Workflows.cs` (class
`Goal19Workflows`):

```csharp
public static TicketDecision DecideLifecycle(Ticket ticket, TicketEvent @event) =>
    (ticket.Status, @event) switch
    {
        (TicketStatus.New, Triage(var assignee)) => TicketDecision.Applied(
            ticket with { Status = TicketStatus.Triaged, Assignee = assignee },
            [new NotifyAssignee(ticket.Id, assignee)]),
        (TicketStatus.Triaged, Escalate) => TicketDecision.Applied(
            ticket with { Status = TicketStatus.Escalated },
            [new PageOnCallEngineer(ticket.Id)]),
        (TicketStatus.Triaged or TicketStatus.Escalated, Resolve) => TicketDecision.Applied(
            ticket with { Status = TicketStatus.Resolved },
            [new PublishResolution(ticket.Id)]),
        (TicketStatus.Resolved, Close) => TicketDecision.Applied(
            ticket with { Status = TicketStatus.Closed },
            [new ArchiveTicket(ticket.Id)]),
        (TicketStatus.New, Escalate or Resolve) =>
            TicketDecision.Rejected(new NotTriageable(ticket.Id)),
        (TicketStatus.Triaged or TicketStatus.Escalated or TicketStatus.Resolved, Close) =>
            TicketDecision.Rejected(new StillOpen(ticket.Id)),
        _ => TicketDecision.Undefined(),
    };

public static TicketDecision DecideRetention(Ticket ticket, TicketEvent @event) =>
    (ticket.Status, @event) switch
    {
        (TicketStatus.Closed, Reopen(var assignee)) => TicketDecision.Applied(
            ticket with { Status = TicketStatus.Triaged, Assignee = assignee },
            [new NotifyAssignee(ticket.Id, assignee)]),
        (_, Reopen) => TicketDecision.Rejected(new NotReopenable(ticket.Id)),
        _ => TicketDecision.Undefined(),
    };

public static TicketDecision Decide(Ticket ticket, TicketEvent @event)
{
    var lifecycle = DecideLifecycle(ticket, @event);
    return lifecycle.Status == TicketDecisionStatus.Undefined
        ? DecideRetention(ticket, @event)
        : lifecycle;
}

public static async Task ExecuteAsync(
    TicketDecision decision,
    ITicketCommandBus bus,
    CancellationToken cancellationToken)
{
    if (decision.Status != TicketDecisionStatus.Applied)
    {
        throw new InvalidOperationException(decision.Error?.ToString() ?? "No handler matched.");
    }

    foreach (var command in decision.Commands)
    {
        await bus.SendAsync(command, cancellationToken).ConfigureAwait(false);
    }
}

public static TicketDecision Replay(Ticket initial, IEnumerable<TicketEvent> history)
{
    var ticket = initial;
    List<TicketCommand>? commands = null;
    foreach (var @event in history)
    {
        var decision = Decide(ticket, @event);
        if (decision.Status != TicketDecisionStatus.Applied)
        {
            return decision;
        }

        ticket = decision.Ticket!;
        if (decision.Commands.Count > 0)
        {
            commands ??= [];
            commands.AddRange(decision.Commands);
        }
    }

    return TicketDecision.Applied(ticket, commands ?? []);
}
```

FunnySharp — `docs/next-stage/call-sites-code/funnysharp/Goal19Workflows.cs`:

```csharp
public static readonly StateMachine<Ticket, TicketEvent, TicketCommand, TicketError> Lifecycle =
    (ticket, @event) => (ticket.Status, @event) switch
    {
        (TicketStatus.New, Triage(var assignee)) =>
            TransitionResult<Ticket, TicketCommand, TicketError>.Applied(
                StateChange<Ticket, TicketCommand>.To(
                    ticket with { Status = TicketStatus.Triaged, Assignee = assignee },
                    new NotifyAssignee(ticket.Id, assignee))),
        (TicketStatus.Triaged, Escalate) =>
            TransitionResult<Ticket, TicketCommand, TicketError>.Applied(
                StateChange<Ticket, TicketCommand>.To(
                    ticket with { Status = TicketStatus.Escalated },
                    new PageOnCallEngineer(ticket.Id))),
        (TicketStatus.Triaged or TicketStatus.Escalated, Resolve) =>
            TransitionResult<Ticket, TicketCommand, TicketError>.Applied(
                StateChange<Ticket, TicketCommand>.To(
                    ticket with { Status = TicketStatus.Resolved },
                    new PublishResolution(ticket.Id))),
        (TicketStatus.Resolved, Close) =>
            TransitionResult<Ticket, TicketCommand, TicketError>.Applied(
                StateChange<Ticket, TicketCommand>.To(
                    ticket with { Status = TicketStatus.Closed },
                    new ArchiveTicket(ticket.Id))),
        (TicketStatus.New, Escalate or Resolve) =>
            TransitionResult<Ticket, TicketCommand, TicketError>.Rejected(
                new NotTriageable(ticket.Id)),
        (TicketStatus.Triaged or TicketStatus.Escalated or TicketStatus.Resolved, Close) =>
            TransitionResult<Ticket, TicketCommand, TicketError>.Rejected(
                new StillOpen(ticket.Id)),
        _ => TransitionResult<Ticket, TicketCommand, TicketError>.Undefined(),
    };

public static readonly StateMachine<Ticket, TicketEvent, TicketCommand, TicketError> Retention =
    (ticket, @event) => (ticket.Status, @event) switch
    {
        (TicketStatus.Closed, Reopen(var assignee)) =>
            TransitionResult<Ticket, TicketCommand, TicketError>.Applied(
                StateChange<Ticket, TicketCommand>.To(
                    ticket with { Status = TicketStatus.Triaged, Assignee = assignee },
                    new NotifyAssignee(ticket.Id, assignee))),
        (_, Reopen) =>
            TransitionResult<Ticket, TicketCommand, TicketError>.Rejected(
                new NotReopenable(ticket.Id)),
        _ => TransitionResult<Ticket, TicketCommand, TicketError>.Undefined(),
    };

public static readonly StateMachine<Ticket, TicketEvent, TicketCommand, TicketError> Escalation =
    Lifecycle.OrElse(Retention);

public static TransitionResult<Ticket, TicketCommand, TicketError> Decide(
    Ticket ticket,
    TicketEvent @event) =>
    Escalation(ticket, @event);

public static async Task ExecuteAsync(
    TransitionResult<Ticket, TicketCommand, TicketError> decision,
    ITicketCommandBus bus,
    CancellationToken cancellationToken)
{
    var commands = decision.Match(
        change => change.Outputs,
        error => throw new InvalidOperationException(error.ToString()),
        error => throw new InvalidOperationException(error.ToString()),
        () => throw new InvalidOperationException("No handler matched."));
    foreach (var command in commands)
    {
        await bus.SendAsync(command, cancellationToken).ConfigureAwait(false);
    }
}

public static TransitionResult<Ticket, TicketCommand, TicketError> ReplayHistory(
    Ticket initial,
    IEnumerable<TicketEvent> history) =>
    Escalation.Replay(initial, history);
```

**Compile evidence.** Both variants compile in their scratch projects (§8).

**Assessment.** S/O breakdown: idiomatic 19 S (two switch bodies, the delegation block's
declaration and return, the execution `if`/`throw`/`foreach`/`await`, and the replay
loop's eleven statements) / 73 O (the two switches carry 53, the delegation 4, the execution
boundary 7, the replay loop 9 — pattern tests and `with`/`new`/call operations per §2);
FunnySharp 11 S (two switch bodies, the composition, the decision, the execution boundary's
declaration, `foreach`, `await`, and three `Match`-arm throws, and the replay) / 64 O
(the machines carry the identical 53, the composition and decision 2, the execution boundary
8, the replay 1).

What FunnySharp adds, by segment:

- **The decision logic is the same size** (the two switches vs the two machines: 55 vs 55
  semantic — 43 + 12 each way). No density claim is made for the switches themselves; the
  rules are the customer's own and cost the same either way.
- **The envelope disappears.** The idiomatic baseline must hand-roll
  `TicketDecision`/`TicketDecisionStatus` (6 semantic, 19 raw, one-time) to carry
  applied/rejected/undefined plus the next state, the commands, and the error; the library
  provides `TransitionResult`/`StateChange` with a four-status model where the fourth
  status (`Failed`) is already there for transitions that recognize an event but cannot
  complete it — a state the envelope does not model and the workflow would eventually need.
- **The delegation disappears.** `Lifecycle.OrElse(Retention)` (2 semantic) replaces the
  hand-rolled undefined-check-and-fall-through (6 semantic), with the pass-through guarantee
  documented and tested (`StateMachineTests.OrElseUsesFallbackOnlyForUndefinedTransitions`):
  an `Applied`/`Rejected`/`Failed` lifecycle result is never re-asked of retention.
- **The replay protocol disappears.** `Escalation.Replay(initial, history)` (2 semantic)
  replaces the hand-threaded loop (20 semantic) — state threading, command accumulation, the
  stop-at-first-non-applied rule, and the drop-staged-commands-on-failure rule are the
  library's tested contract (`StateMachineTests`, `docs/state-machines.md`), not twelve
  hand-maintained lines per project that owns a machine. This is the protocol-shaped win the
  Goal 14 record predicted (W7, W8, W11 have the same shape).
- **Determinism and separation are structural, not conventional.** The machines are pure
  delegate values: nothing in the decision can execute a command, read a clock, or observe
  cancellation (`StateMachineTests.PureTransitionsDoNotExecuteOutputs...`,
  `...OutputCancellationRemainsOutsideThePureTransitionCore`), and replay is deterministic
  (`ReplayStopsAtUndefinedAndProducesDeterministicResults`). The idiomatic version reaches
  the same discipline only by convention and review.

Honest losses: the `TransitionResult<Ticket, TicketCommand, TicketError>` spelling is long
and C# offers no alias for it — six of its appearances dominate the machine definitions' raw
height (the AD-8 note that the generic parameter orders intentionally differ and are not
unified); the machines are indirect static delegate values, so "find the decision logic" is
one hop further than a method; the four-status vocabulary and the `Failed`/`Rejected`
distinction must be learned; and `Match` forces the execution boundary to spell all four
arms where the idiomatic `if` checks one condition. If the two teams were one team, the
idiomatic single-switch version would be simpler than either variant — the OrElse value is
specifically split ownership, and the baseline deliberately mirrors that split (the merged
single switch would have been 6 semantic smaller and one file both teams edit).

## 5. WF-19B — Two-site nested update through one named path

**Scenario.** A customer profile stores a mailing address three levels deep. Two call sites
update the same nested path: the account page replaces the whole address when a customer
moves, and a data-quality batch normalizes the city casing. The AD-9 adoption note is that
lenses are for paths updated in more than one place; this workflow measures exactly that
case. (W10 already measured single-site first use and found nested `with` unbeatable; that
result is not restated here.)

| Variant | S | O | Semantic | Raw |
| --- | ---: | ---: | ---: | ---: |
| Idiomatic C# (both sites) | 2 | 8 | **10** | 22 |
| FunnySharp (entries only) | 2 | 5 | **7** | 11 |
| FunnySharp one-time definitions (three lenses + two compositions) | 5 | 7 | **12** | 16 |

Idiomatic C# — `docs/next-stage/call-sites-code/idiomatic/Workflows.cs` (class
`Goal19Workflows`):

```csharp
public static CustomerProfile MoveCustomer(
    CustomerProfile profile,
    string city,
    string postalCode) =>
    profile with
    {
        Contact = profile.Contact with
        {
            Address = profile.Contact.Address with { City = city, PostalCode = postalCode },
        },
    };

public static CustomerProfile NormalizeCity(CustomerProfile profile) =>
    profile with
    {
        Contact = profile.Contact with
        {
            Address = profile.Contact.Address with
            {
                City = profile.Contact.Address.City.Trim().ToUpperInvariant(),
            },
        },
    };
```

FunnySharp — `docs/next-stage/call-sites-code/funnysharp/Goal19Workflows.cs` (the one-time
definitions are listed after the entries):

```csharp
public static CustomerProfile MoveCustomer(
    CustomerProfile profile,
    string city,
    string postalCode) =>
    ProfileAddress.Update(profile, address => address with
    {
        City = city,
        PostalCode = postalCode,
    });

public static CustomerProfile NormalizeCity(CustomerProfile profile) =>
    ProfileCity.Update(profile, city => city.Trim().ToUpperInvariant());

private static readonly Lens<CustomerProfile, CustomerContact> Contact =
    Lens.Create<CustomerProfile, CustomerContact>(
        profile => profile.Contact,
        (profile, contact) => profile with { Contact = contact });

private static readonly Lens<CustomerContact, MailingAddress> Address =
    Lens.Create<CustomerContact, MailingAddress>(
        contact => contact.Address,
        (contact, address) => contact with { Address = address });

private static readonly Lens<MailingAddress, string> City =
    Lens.Create<MailingAddress, string>(
        address => address.City,
        (address, city) => address with { City = city });

private static readonly Lens<CustomerProfile, MailingAddress> ProfileAddress =
    Contact.Compose(Address);

private static readonly Lens<CustomerProfile, string> ProfileCity =
    ProfileAddress.Compose(City);
```

**Compile evidence.** Both variants compile in their scratch projects (§8).

**Assessment.** S/O breakdown: idiomatic 2 S (two expression bodies) / 8 O (three `with`
per site plus `Trim` and `ToUpperInvariant` at the batch site); FunnySharp entries 2 S /
5 O (`Update` + one `with` at the move site; `Update` + `Trim` +
`ToUpperInvariant` at the batch site), one-time definitions 5 S (five field declarations)
/ 7 O (three `Create` + three setter `with` + two `Compose`).

What FunnySharp adds: **per-site brevity with one spelling of the path.** Every site names
`ProfileAddress` or `ProfileCity` instead of re-spelling `profile.Contact.Address`; the
path exists once, as a value, so a path change (a contact list, an address history) edits
one definition, and both sites provably use the same path because they call the same value —
the idiomatic sites agree only by convention and review, and the W10 finding that the
hand-spelled path silently drifts between sites is what this workflow's two sites would
exercise over time. The composed path is also a testable value: the lens laws can be checked
once (`OpticsTests.LensObeysGetPutPutGetPutPutAndIdentityLaws`) for a path that every site
then inherits, and it can be passed as a parameter — the idiomatic path cannot be passed at
all. The absent-focus guarantee is inherited at every site: an `Optional` stage returns the
exact original source instead of constructing a default (`docs/immutable-updates.md`).

Honest losses, stated as plainly as W10's: **at two sites the lens is not a LOC win** — 19
vs 10 semantic including the one-time definitions. Each additional site saves 1–2 semantic
(4→3 at a move site, 6→4 at a transform site), so the LOC break-even is ≈7–8 sites; a
reviewer planning two or three sites should write nested `with` expressions (W10's
verdict, unchanged). The lens laws remain caller obligations — a bad getter/setter pair
compiles and misbehaves — and `ProfileCity` reads right-to-left from the type parameters'
perspective (`Lens<CustomerProfile, string>` composed left-to-right from
`CustomerProfile` to `string`; the composition order is the reading order, but the
delegate types must be checked to see it). No multi-focus combinator exists, so the move
site updates the address as one value rather than two composed field updates — which is also
the honest shape for "replace the whole address".

## 6. Assessment

- **The state family is the advanced-pattern win the Goal 14 record predicted.** Where
  FunnySharp owns a protocol — here the outcome envelope, undefined-fallback delegation, and
  replay — the consumer surface shrinks: 92 + 6 one-time semantic for the hand-rolled
  baseline versus 75 semantic for the library form, with the decision switches themselves
  identical (55 = 55 semantic). This is the same shape as W7/W8/W11 in the
  Goal 14 record: reduction concentrates where the library owns a protocol. The retained
  AD-8 surface earns its place for split-owner, replayable, command-emitting workflows; a
  single-owner, execute-inline state mutation should stay a switch statement, and the
  `docs/state-machines.md` async boundary section says so.
- **The optics family's honest economics are now quantified at both ends.** Single site
  (W10): nested `with` wins 5 vs 16. Two sites (WF-19B): `with` still wins 10 vs 19; the
  lens wins per site (3–4 vs 4–6) and wins the single-point-of-truth property, with LOC
  break-even at ≈7–8 sites. The AD-9 adoption note ("paths updated in more than one place")
  is thereby refined, not contradicted: the second site is where the path becomes a named
  value that can be reused, tested once, and passed around, and the LOC argument arrives
  several sites later. This is recorded so no "lenses are shorter" claim is made.
- **The effect family stays on its pinned evidence.** W9 (9 vs 10 semantic) remains the
  honest single-scope baseline and is not re-measured here; the AD-7 adoption guidance
  (effects pay when deferred execution, explicit environment, cancellation flow, or resource
  lifetime are composed, not for one synchronous call) is unchanged.
- **API frictions observed while compiling (reported, not fixed).** The
  `TransitionResult<Ticket, TicketCommand, TicketError>` generic spelling has no alias
  mechanism in C# and dominates the machine definitions' height; `Match` requires all four
  arms at the execution boundary, where the idiomatic envelope can check one field; lens
  composition order is the reading order but is only visible in the types.

## 7. Limitations

- Two workflows, one scenario each; no competitor variants were compiled (Goal 19 is a
  curation goal against the maintainer-accepted record, and the Goal 14 competitor
  comparison duties are not re-run; language-ext's `State`/`StateT` and optics
  hierarchies are rejected by AD-8/AD-9 and were not surveyed here).
- Semantic LOC is directional, hand-derived evidence; the §2 rule and its six
  clarifications let a reviewer re-derive or dispute any single element, and the raw LOC
  tool output is reproducible with `tools/loc.py` (field initializers hand-counted per
  clarification 2).
- The break-even estimate in §5 assumes site costs like the two measured sites (a
  whole-value replacement and a unary transform); sites with different shapes move it.
- The idiomatic delegation baseline mirrors the two-team ownership framing; a single-team
  consumer would merge the switches (≈6 semantic smaller), as §4 records.

## 8. Compile evidence

Both scratch projects were cleaned and rebuilt from the committed sources:

```shell
cd docs/next-stage/call-sites-code
rm -rf idiomatic/bin idiomatic/obj funnysharp/bin funnysharp/obj
dotnet build idiomatic/Idiomatic.csproj -c Release
dotnet build funnysharp/FunnySharpCallSites.csproj -c Release
```

Both builds report `Build succeeded. 0 Warning(s) 0 Error(s)` (SDK 10.0.400,
2026-09-23). Every code block in §4–§5 is byte-identical to the committed scratch files.

## 9. Files

- `docs/next-stage/call-sites-code/funnysharp/Goal19Workflows.cs` — FunnySharp WF-19A and
  WF-19B plus the shared harness.
- `docs/next-stage/call-sites-code/idiomatic/Workflows.cs` — additive `Goal19Workflows`
  class (idiomatic WF-19A and WF-19B), the one-time `TicketDecision` envelope, and the
  shared harness; the Goal 14/15/16 content is unmodified.
- `docs/next-stage/call-sites-goal-16.md`, `docs/next-stage/call-sites-goal-15.md`,
  `docs/next-stage/call-sites.md` — unmodified prior evidence.
