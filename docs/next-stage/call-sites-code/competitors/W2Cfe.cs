using CSharpFunctionalExtensions;

namespace CallSites.Competitors;

// W2 competitor: CSharpFunctionalExtensions 3.7.0 typed-error Result pipeline.
public static class W2Cfe
{
    public static async Task<Result<Invoice, OrderError>> CreateInvoiceAsync(
        OrderRequest request,
        ICustomerRepository repository,
        CancellationToken cancellationToken)
    {
        var customer = await repository.FindCustomerAsync(request.CustomerId, cancellationToken)
            .ConfigureAwait(false);
        return Result.Success<OrderRequest, OrderError>(request)
            .Ensure(r => r.CustomerId.Length > 0, new InvalidPayload("CustomerId is required."))
            .Ensure(r => !r.Lines.IsEmpty, new NoLines(request.OrderId))
            .Ensure(r => r.Lines.All(line => line.Quantity > 0), InvalidQuantityFor(request))
            .Bind(_ => customer is null
                ? Result.Failure<Customer, OrderError>(new CustomerNotFound(request.CustomerId))
                : Result.Success<Customer, OrderError>(customer))
            .Map(c => Domain.BuildInvoice(c, request));
    }

    private static OrderError InvalidQuantityFor(OrderRequest request)
    {
        var invalid = request.Lines.First(line => line.Quantity <= 0);
        return new InvalidQuantity(invalid.Sku, invalid.Quantity);
    }
}
