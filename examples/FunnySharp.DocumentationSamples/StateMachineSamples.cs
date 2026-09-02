using FunnySharp;

namespace FunnySharp.DocumentationSamples;

internal static class StateMachineSamples
{
    private static void CreateTransition()
    {
        // <snippet DocumentationSamples.StateMachines.CreateTransition>
        StateTransition<Account, AuditCommand> submit = account =>
            StateChange<Account, AuditCommand>.To(
                account with { Status = AccountStatus.Submitted },
                new StoreAccount(account.Id),
                new NotifyReviewer(account.Id));
        // </snippet>
    }

    private static async Task ExecuteOutputsAsync(CancellationToken cancellationToken)
    {
        var result = TransitionResult<Account, AuditCommand, WorkflowError>.Applied(
            StateChange<Account, AuditCommand>.To(
                new Account(Guid.NewGuid(), AccountStatus.Submitted),
                new StoreAccount(Guid.NewGuid())));

        // <snippet DocumentationSamples.StateMachines.ExecuteOutputs>
        var commands = result.Match(
            change => change.Outputs,
            rejection => throw new InvalidOperationException(rejection.Code),
            failure => throw new InvalidOperationException(failure.Code),
            () => throw new InvalidOperationException("No handler matched."));

        foreach (var command in commands)
        {
            await ExecuteCommandAsync(command, cancellationToken);
        }
        // </snippet>
    }

    private static Task ExecuteCommandAsync(AuditCommand command, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    private sealed record Account(Guid Id, AccountStatus Status);

    private enum AccountStatus
    {
        Draft,
        Submitted,
    }

    private abstract record AuditCommand(Guid AccountId);

    private sealed record StoreAccount(Guid Id) : AuditCommand(Id);

    private sealed record NotifyReviewer(Guid Id) : AuditCommand(Id);

    private sealed record WorkflowError(string Code);
}
