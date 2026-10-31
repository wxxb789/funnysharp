using FunnySharp;

namespace FunnySharp.TestSupport.Decoding;

internal sealed record OrderId(Guid Value);

internal sealed record EmailAddress(string Value);

internal sealed record Address(string Line, string City, string PostalCode);

internal sealed record Customer(string Name, EmailAddress Email, IReadOnlyList<Address> Addresses);

internal sealed record OrderLine(string Sku, int Quantity, decimal UnitPrice);

internal sealed record Order(OrderId Id, Customer Customer, IReadOnlyList<OrderLine> Lines, Option<string> Note);

internal sealed record OrderDecodeResult(Order? Value, IReadOnlyList<OrderError> Errors)
{
    internal bool IsSuccess => Errors.Count == 0;
}
