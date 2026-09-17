namespace CallSites.FunnySharpAspNet;

public sealed record Customer(string Id, string Name);

public sealed record Order(string Id, decimal Total);

public sealed record OrderRequest(string CustomerId, decimal Total);

public sealed record SignupForm(string Email, string Password);

public abstract record OrderError
{
    public abstract int Status { get; }

    public abstract string Title { get; }

    public abstract string Detail { get; }
}

public sealed record CustomerNotFound(string CustomerId) : OrderError
{
    public override int Status => 404;

    public override string Title => "customer-not-found";

    public override string Detail => $"No customer '{CustomerId}' exists.";
}

public sealed record AmountOutOfRange(decimal Total) : OrderError
{
    public override int Status => 400;

    public override string Title => "amount-out-of-range";

    public override string Detail => $"Total {Total} must be between 0 and 100000.";
}

public interface IOrderService
{
    Task<Order> CreateAsync(OrderRequest request, CancellationToken cancellationToken);
}
