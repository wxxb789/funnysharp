using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace CallSites.Idiomatic;

// Workflows implemented with plain C# and the BCL only.
public static class Workflows
{
    // W1: dictionary lookup with fallback.
    public static int TimeoutSeconds(IReadOnlyDictionary<string, string> config)
    {
        if (config.TryGetValue("request.timeoutSeconds", out var raw) &&
            int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
        {
            return seconds;
        }

        return 30;
    }

    // W2: parse-then-validate fail-fast pipeline with typed errors.
    public static async Task<InvoiceOutcome> CreateInvoiceAsync(
        OrderRequest request,
        ICustomerRepository repository,
        CancellationToken cancellationToken)
    {
        if (request.CustomerId.Length == 0)
        {
            return InvoiceOutcome.Failed(new InvalidPayload("CustomerId is required."));
        }

        if (request.Lines.IsEmpty)
        {
            return InvoiceOutcome.Failed(new NoLines(request.OrderId));
        }

        foreach (var line in request.Lines)
        {
            if (line.Quantity <= 0)
            {
                return InvoiceOutcome.Failed(new InvalidQuantity(line.Sku, line.Quantity));
            }
        }

        var customer = await repository.FindCustomerAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return InvoiceOutcome.Failed(new CustomerNotFound(request.CustomerId));
        }

        return InvoiceOutcome.Ok(Domain.BuildInvoice(customer, request));
    }

    // W3: independent form-field validation accumulating all errors.
    public static IReadOnlyList<string> ValidateSignup(SignupForm form)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(form.Email) || !form.Email.Contains('@'))
        {
            errors.Add("email must contain '@'");
        }

        if (form.Password.Length < 12)
        {
            errors.Add("password must have at least 12 characters");
        }

        if (form.Age is < 18 or > 130)
        {
            errors.Add("age must be between 18 and 130");
        }

        return errors;
    }

    // W4: delete-or-notify unit-result workflow.
    public static async Task<OrderError?> DeleteOrderAsync(
        string orderId,
        IOrderStore store,
        INotifier notifier,
        CancellationToken cancellationToken)
    {
        var existing = await store.FindAsync(orderId, cancellationToken);
        if (existing is null)
        {
            return new CustomerNotFound(orderId);
        }

        await store.DeleteAsync(orderId, cancellationToken);
        await notifier.NotifyAsync($"order {orderId} deleted", cancellationToken);
        return null;
    }

    // W5: composed data transforms with observation/tap.
    public static decimal TotalWithAudit(OrderLine line, Action<string> audit)
    {
        var total = line.Quantity * line.UnitPrice;
        audit($"line={line.Sku} total={total}");
        var discounted = total * Discount(line.Sku);
        audit($"discounted={discounted}");
        return discounted;
    }

    // W6: traverse a sequence with index/path context.
    public static Parsed<IReadOnlyList<OrderLine>> ParseAll(IReadOnlyList<string> rows)
    {
        var lines = new List<OrderLine>(rows.Count);
        for (var index = 0; index < rows.Count; index++)
        {
            var parsed = ParseLine(rows[index]);
            if (!parsed.IsSuccess)
            {
                parsed.TryGetError(out var error);
                return Parsed<IReadOnlyList<OrderLine>>.Fail(
                    new LocatedError($"rows[{index}]", error!));
            }

            lines.Add(parsed.Value!);
        }

        return Parsed<IReadOnlyList<OrderLine>>.Ok(lines);
    }

    // W7: bounded parallel ordered fetch over IAsyncEnumerable with cancellation.
    public static async IAsyncEnumerable<TResult> SelectParallelAsync<TResult>(
        IAsyncEnumerable<string> source,
        int maxConcurrency,
        Func<string, CancellationToken, Task<TResult>> selector,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var concurrency = new SemaphoreSlim(maxConcurrency);
        var channel = Channel.CreateBounded<Task<TResult>>(maxConcurrency);
        var producer = ProduceAsync();
        try
        {
            await foreach (var pending in channel.Reader.ReadAllAsync(operation.Token))
            {
                try
                {
                    yield return await pending.ConfigureAwait(false);
                }
                finally
                {
                    concurrency.Release();
                }
            }
        }
        finally
        {
            operation.Cancel();
            try
            {
                await producer.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        async Task ProduceAsync()
        {
            try
            {
                await foreach (var item in source.WithCancellation(operation.Token).ConfigureAwait(false))
                {
                    await concurrency.WaitAsync(operation.Token).ConfigureAwait(false);
                    var pending = selector(item, operation.Token);
                    await channel.Writer.WriteAsync(pending, operation.Token).ConfigureAwait(false);
                }

                channel.Writer.Complete();
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                channel.Writer.TryComplete(exception);
            }
        }
    }

    // W7 consumer: the call site an application writes on top of the operator above.
    public static async Task<int> SumFetchedAsync(
        IAsyncEnumerable<string> skus,
        Func<string, CancellationToken, Task<int>> fetch,
        CancellationToken cancellationToken)
    {
        var total = 0;
        await foreach (var price in SelectParallelAsync(skus, 4, fetch, cancellationToken))
        {
            total += price;
        }

        return total;
    }

    // W8: first-success selection across cold operations with timeout.
    public static async Task<Invoice> FirstInvoiceAsync(
        IReadOnlyList<Func<CancellationToken, Task<Invoice>>> providers,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutSource = new CancellationTokenSource(timeout, TimeProvider.System);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);
        var pending = providers.Select(provider => provider(linked.Token)).ToList();
        while (pending.Count > 0)
        {
            var completed = await Task.WhenAny(pending).ConfigureAwait(false);
            pending.Remove(completed);
            try
            {
                return await completed.ConfigureAwait(false);
            }
            catch (Exception) when (!linked.IsCancellationRequested)
            {
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        throw new TimeoutException($"No invoice provider succeeded within {timeout}.");
    }

    // W9: environment + resource scoped workflow with cancellation.
    public static async Task<decimal> LoadOrderTotalAsync(
        PricingEnvironment environment,
        string orderId,
        CancellationToken cancellationToken)
    {
        await using var session = await environment.Connections
            .OpenSessionAsync(cancellationToken)
            .ConfigureAwait(false);
        var lines = await session.LoadOrderLinesAsync(orderId, cancellationToken).ConfigureAwait(false);
        return lines.Sum(line => line.Quantity * line.UnitPrice);
    }

    // W10: nested record update with immutable collections.
    public static Order ReviewPostalCode(Order order, string postalCode) =>
        order with
        {
            Customer = order.Customer with
            {
                PrimaryAddress = order.Customer.PrimaryAddress with { PostalCode = postalCode },
                Tags = order.Customer.Tags.Add("address-reviewed"),
            },
        };

    // Shared helpers for the workflows above.
    public static decimal Discount(string sku) => sku.StartsWith("SAVE", StringComparison.Ordinal) ? 0.9m : 1m;

    public static Parsed<OrderLine> ParseLine(string row)
    {
        var parts = row.Split(':');
        if (parts.Length != 3 ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity) ||
            !decimal.TryParse(parts[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
        {
            return Parsed<OrderLine>.Fail(new InvalidPayload($"cannot parse row '{row}'"));
        }

        return Parsed<OrderLine>.Ok(new OrderLine(parts[0], quantity, price));
    }
}

public readonly record struct Parsed<T>(T? Value, OrderError? Error)
{
    public bool IsSuccess => Error is null;

    public static Parsed<T> Ok(T value) => new(value, null);

    public static Parsed<T> Fail(OrderError error) => new(default, error);

    public bool TryGetError(out OrderError? error) => (error = Error) is not null;
}

public sealed record LocatedError(string Path, OrderError Inner) : OrderError;

public sealed record SignupForm(string Email, string Password, int Age);

public interface IOrderStore
{
    Task<Order?> FindAsync(string orderId, CancellationToken cancellationToken);

    Task DeleteAsync(string orderId, CancellationToken cancellationToken);
}

public interface INotifier
{
    Task NotifyAsync(string message, CancellationToken cancellationToken);
}

public sealed record PricingEnvironment(ISessionFactory Connections);

public interface ISessionFactory
{
    Task<DbSession> OpenSessionAsync(CancellationToken cancellationToken);
}

public sealed class DbSession : IAsyncDisposable
{
    public Task<IReadOnlyList<OrderLine>> LoadOrderLinesAsync(
        string orderId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OrderLine>>([]);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
