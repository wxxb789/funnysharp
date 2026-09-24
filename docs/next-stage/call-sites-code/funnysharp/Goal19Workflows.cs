using FunnySharp;

namespace CallSites.FunnySharp;

// Goal 19 versions of the call sites that exercise the advanced-pattern
// surfaces the Goal 14 record curates: the pure state-transition and
// commands-as-data model (AD-8, WF-19A) and the optics surface on a path
// updated at more than one site (AD-9, WF-19B). The effect and resource
// boundary (AD-7) keeps its pinned Goal 14 comparison W9; the async
// coordination family (AD-6) keeps W7, W8, and the Goal 18 delivery. The
// harness types at the bottom of this file are excluded from every count,
// like the shared Domain.cs model.
public static class Goal19Workflows
{
    // WF-19A: support-ticket escalation. The lifecycle machine is owned by
    // the ticket team; it decides the next state and emits commands as data.
    // Nothing here executes a command, reads a clock, or touches I/O, so a
    // replay of the event history reproduces every decision.
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

    // WF-19A: the retention machine is owned by the compliance team and owns
    // every Reopen outcome. OrElse composes the two machines without either
    // team editing the other's switch, and a defined lifecycle result is
    // never consulted against retention.
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

    // WF-19A decision boundary: one call; the result carries the applied
    // change with its commands, the typed rejection, or an explicit
    // dispatch miss. Invalid transitions are values, not exceptions.
    public static TransitionResult<Ticket, TicketCommand, TicketError> Decide(
        Ticket ticket,
        TicketEvent @event) =>
        Escalation(ticket, @event);

    // WF-19A execution boundary: the only place commands become I/O.
    // Ordering, retries, and cancellation belong to this caller-owned loop.
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

    // WF-19A replay: the pure machine reproduces the final state and the
    // complete command history from the event history alone.
    public static TransitionResult<Ticket, TicketCommand, TicketError> ReplayHistory(
        Ticket initial,
        IEnumerable<TicketEvent> history) =>
        Escalation.Replay(initial, history);

    // WF-19B: two call sites update the same nested mailing-address path.
    // The composed lenses are defined once and named, so both sites reuse
    // one read/write path that cannot drift between them (AD-9: lenses are
    // for paths updated in more than one place).
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

    // WF-19B site 1: the customer moved; the whole address is replaced
    // through the shared path.
    public static CustomerProfile MoveCustomer(
        CustomerProfile profile,
        string city,
        string postalCode) =>
        ProfileAddress.Update(profile, address => address with
        {
            City = city,
            PostalCode = postalCode,
        });

    // WF-19B site 2: the data-quality batch normalizes the city through the
    // same shared path; every other field is untouched.
    public static CustomerProfile NormalizeCity(CustomerProfile profile) =>
        ProfileCity.Update(profile, city => city.Trim().ToUpperInvariant());
}

// Harness types for the Goal 19 workflows (excluded from every count, like
// the shared Domain.cs model; used identically by the idiomatic baselines).
public enum TicketStatus
{
    New,
    Triaged,
    Escalated,
    Resolved,
    Closed,
}

public sealed record Ticket(string Id, TicketStatus Status, string Assignee);

public abstract record TicketEvent;

public sealed record Triage(string Assignee) : TicketEvent;

public sealed record Escalate() : TicketEvent;

public sealed record Resolve() : TicketEvent;

public sealed record Close() : TicketEvent;

public sealed record Reopen(string Assignee) : TicketEvent;

public abstract record TicketCommand;

public sealed record NotifyAssignee(string TicketId, string Assignee) : TicketCommand;

public sealed record PageOnCallEngineer(string TicketId) : TicketCommand;

public sealed record PublishResolution(string TicketId) : TicketCommand;

public sealed record ArchiveTicket(string TicketId) : TicketCommand;

public abstract record TicketError;

public sealed record NotTriageable(string TicketId) : TicketError;

public sealed record StillOpen(string TicketId) : TicketError;

public sealed record NotReopenable(string TicketId) : TicketError;

public interface ITicketCommandBus
{
    Task SendAsync(TicketCommand command, CancellationToken cancellationToken);
}

public sealed record CustomerProfile(CustomerContact Contact);

public sealed record CustomerContact(MailingAddress Address);

public sealed record MailingAddress(string City, string PostalCode);
