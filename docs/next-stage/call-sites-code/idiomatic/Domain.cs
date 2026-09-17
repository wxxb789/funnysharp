using System.Collections.Immutable;

namespace CallSites.Idiomatic;

public sealed record Address(string Line1, string City, string PostalCode);

public sealed record Customer(
    string Id,
    string Name,
    string Email,
    Address PrimaryAddress,
    ImmutableArray<string> Tags);

public sealed record OrderLine(string Sku, int Quantity, decimal UnitPrice);

public sealed record Order(string Id, Customer Customer, ImmutableArray<OrderLine> Lines);

public sealed record Invoice(string InvoiceId, decimal Total);

public sealed record OrderRequest(string OrderId, string CustomerId, ImmutableArray<OrderLine> Lines);

public abstract record OrderError;

public sealed record InvalidPayload(string Message) : OrderError;

public sealed record CustomerNotFound(string CustomerId) : OrderError;

public sealed record NoLines(string OrderId) : OrderError;

public sealed record InvalidQuantity(string Sku, int Quantity) : OrderError;

public interface ICustomerRepository
{
    Task<Customer?> FindCustomerAsync(string customerId, CancellationToken cancellationToken);
}

public interface IPricingApi
{
    Task<decimal> GetPriceAsync(string sku, CancellationToken cancellationToken);
}

public interface IInventoryApi
{
    Task<bool> ReserveAsync(string sku, int quantity, CancellationToken cancellationToken);
}

/// <summary>Straightforward C# outcome carrier used by the idiomatic variants.</summary>
public readonly record struct InvoiceOutcome(Invoice? Invoice, OrderError? Error)
{
    public static InvoiceOutcome Ok(Invoice invoice) => new(invoice, null);

    public static InvoiceOutcome Failed(OrderError error) => new(null, error);
}

public static class Domain
{
    public static Invoice BuildInvoice(Customer customer, OrderRequest request)
    {
        var total = 0m;
        foreach (var line in request.Lines)
        {
            total += line.Quantity * line.UnitPrice;
        }

        return new Invoice($"INV-{request.OrderId}", total);
    }
}
