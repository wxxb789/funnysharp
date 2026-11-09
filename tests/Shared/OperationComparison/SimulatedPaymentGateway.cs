using System.Globalization;

using FunnySharp;

namespace FunnySharp.TestSupport.OperationComparison;

internal sealed class SimulatedPaymentGateway
{
    private readonly List<PaymentAttempt> _attempts = [];

    internal bool DeclineNext { get; set; }

    internal IReadOnlyList<PaymentAttempt> Attempts => _attempts;

    internal ValueTask<Result<string, OrderError>> AuthorizeAsync(
        long orderId,
        int amountCents,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<Result<string, OrderError>>(cancellationToken);
        }

        if (DeclineNext)
        {
            _attempts.Add(new PaymentAttempt(orderId, amountCents, null, false));

            return ValueTask.FromResult(
                Result<string, OrderError>.Failure(
                    new OrderError.PaymentDeclined(ShopFixtures.PaymentDeclinedReason)));
        }

        var reference = $"AUTH-{orderId.ToString(CultureInfo.InvariantCulture)}-{amountCents.ToString(CultureInfo.InvariantCulture)}";
        _attempts.Add(new PaymentAttempt(orderId, amountCents, reference, true));

        return ValueTask.FromResult(Result<string, OrderError>.Success(reference));
    }
}

internal sealed class FaultInjector
{
    internal bool FailOutbox { get; set; }

    internal string? CancelAtStep { get; set; }

    internal string? CancelInsideStep { get; set; }

    internal CancellationTokenSource? Cancellation { get; set; }

    internal Exception? TransactionOpenException { get; set; }

    internal Exception? CommitException { get; set; }

    internal Exception? RollbackException { get; set; }

    internal void OnTransactionOpen()
    {
        if (TransactionOpenException is not null)
        {
            throw TransactionOpenException;
        }
    }

    internal void OnCommit()
    {
        if (CommitException is not null)
        {
            throw CommitException;
        }
    }

    internal void OnRollback()
    {
        if (RollbackException is not null)
        {
            throw RollbackException;
        }
    }

    internal void OnStepEnter(string name)
    {
        if (Cancellation is not null
            && string.Equals(CancelInsideStep, name, StringComparison.Ordinal))
        {
            Cancellation.Cancel();
            throw new OperationCanceledException(Cancellation.Token);
        }
    }

    internal void OnStepExit(string name)
    {
        if (Cancellation is not null
            && string.Equals(CancelAtStep, name, StringComparison.Ordinal))
        {
            Cancellation.Cancel();
        }
    }

    internal void OnOutbox()
    {
        if (FailOutbox)
        {
            throw new InvalidOperationException("outbox unavailable");
        }
    }
}
