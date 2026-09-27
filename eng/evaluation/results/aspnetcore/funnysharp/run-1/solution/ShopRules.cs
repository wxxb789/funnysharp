using FunnySharp;

// The shop rules behind the endpoints, on FunnySharp carriers: Validation for the
// independent checks that accumulate every code, Option for absence, and Result or
// UnitResult for the fail-fast command steps. No HTTP appears here.

public static class ShopRules
{
    private const decimal Shipping = 5.00m;

    // POST /orders — the four independent request checks. Every check runs and its code
    // accumulates in rule order:
    //   1. invalid-email — the email contains no '@', or no '.' after the '@',
    //   2. empty-order — the order has no lines,
    //   3. invalid-quantity:<sku> — a line's quantity is outside 1..99, one code per
    //      offending line, in line order,
    //   4. unknown-promo — the promo code is neither null, empty, SAVE10, nor FREESHIP.
    public static Validation<OrderRequest, string> CheckOrder(OrderRequest request)
    {
        Validation<OrderRequest, string> email =
            Require(EmailIsPlausible(request.CustomerEmail), request, "invalid-email");
        Validation<OrderRequest, string> lines =
            Require(request.Lines is { Count: > 0 }, request, "empty-order");
        Validation<IReadOnlyList<OrderLine>, string> quantities =
            CheckQuantities(request.Lines ?? []);
        Validation<OrderRequest, string> promo =
            Require(PromoIsKnown(request.PromoCode), request, "unknown-promo");

        // Zip accumulates the codes of all four checks in rule order.
        return email.Zip(lines, quantities, promo, (_, _, _, _) => request);
    }

    // Rule 3: one code per offending line, in line order.
    private static Validation<IReadOnlyList<OrderLine>, string> CheckQuantities(IReadOnlyList<OrderLine> lines) =>
        lines.Traverse(line => Require(line.Quantity is >= 1 and <= 99, line, $"invalid-quantity:{line.Sku}"));

    // POST /orders — pricing, which depends on a request that passed every check. Every
    // line's sku must resolve in the catalog: unknown-sku:<sku> accumulates one code per
    // missing line, in line order. The valid value is the rounded total.
    public static Validation<decimal, string> PriceOrder(OrderRequest request) =>
        request.Lines
            .Traverse(PriceLine)
            .Map(amounts => Total(amounts.Sum(), request.PromoCode));

    // One line's amount: the catalog price times the quantity, or the code for a sku that
    // is absent from the catalog.
    private static Validation<decimal, string> PriceLine(OrderLine line) =>
        ShopData.FindProduct(line.Sku)
            .ToOption() // absence: the sku matches no catalog product
            .Match(
                product => Validation<decimal, string>.Valid(product.Price * line.Quantity),
                () => Validation<decimal, string>.Invalid($"unknown-sku:{line.Sku}"));

    // Goods plus 5.00 shipping. SAVE10 removes 10% of the goods subtotal (shipping
    // unaffected); FREESHIP removes the shipping; the total is rounded to two decimals.
    private static decimal Total(decimal goods, string? promoCode) => promoCode switch
    {
        "SAVE10" => Math.Round(goods * 0.9m + Shipping, 2),
        "FREESHIP" => Math.Round(goods, 2),
        _ => Math.Round(goods + Shipping, 2),
    };

    // DELETE /orders/{id} — remove an unpaid order. A missing order fails fast; a paid
    // order fails fast and stays stored; any other order is removed.
    public static UnitResult<DeleteOrderError> DeleteOrder(int id) =>
        ShopData.FindOrder(id)
            .ToOption() // absence: the id matches no stored order
            .ToResult<StoredOrder, DeleteOrderError>(() => new DeleteOrderError.NotFound(id))
            .Ensure(order => !order.Paid, new DeleteOrderError.Paid())
            .ToUnitResult() // the removal is a command, not a value
            .Bind(() => RemoveOrder(id));

    // The removal itself; a false return means the order was never there.
    private static UnitResult<DeleteOrderError> RemoveOrder(int id) =>
        ShopData.RemoveOrder(id)
            ? UnitResult<DeleteOrderError>.Success()
            : UnitResult<DeleteOrderError>.Failure(new DeleteOrderError.NotFound(id));

    // POST /orders/{id}/pay — charge the order's total. A missing order fails fast; a
    // declined charge fails fast and leaves the order unpaid; a successful charge marks
    // the order paid.
    public static Result<PaymentAccepted, PayError> PayOrder(int id, string cardToken) =>
        ShopData.FindOrder(id)
            .ToOption() // absence: the id matches no stored order
            .ToResult<StoredOrder, PayError>(() => new PayError.NotFound(id))
            .Bind(order => Charge(order, cardToken));

    // Charge the card; only a successful charge marks the order paid.
    private static Result<PaymentAccepted, PayError> Charge(StoredOrder order, string cardToken) =>
        ShopData.Charge(cardToken, order.Total)
            ? Result<PaymentAccepted, PayError>.Success(MarkPaid(order))
            : Result<PaymentAccepted, PayError>.Failure(new PayError.Declined());

    private static PaymentAccepted MarkPaid(StoredOrder order)
    {
        ShopData.MarkOrderPaid(order.Id);
        return new PaymentAccepted(Paid: true);
    }

    // An independent check: keep the value when it passes, report the code when it fails.
    private static Validation<T, string> Require<T>(bool passes, T value, string code) =>
        passes ? Validation<T, string>.Valid(value) : Validation<T, string>.Invalid(code);

    // Rule 1: the email must contain '@' and a '.' after it.
    private static bool EmailIsPlausible(string? email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return false;
        }

        var at = email.IndexOf('@');
        return at >= 0 && email.IndexOf('.', at) > at;
    }

    // Rule 4: null, empty, SAVE10, and FREESHIP are the known promo codes.
    private static bool PromoIsKnown(string? promoCode) =>
        promoCode is null or "" or "SAVE10" or "FREESHIP";
}

// The DELETE /orders/{id} failures: which rule failed. The private constructor closes
// the hierarchy, so the HTTP mapping can switch over every case exhaustively.
public abstract class DeleteOrderError
{
    private DeleteOrderError() { }

    // No stored order matches the id.
    public sealed class NotFound(int orderId) : DeleteOrderError
    {
        public int OrderId { get; } = orderId;
    }

    // The stored order is already paid and stays stored.
    public sealed class Paid : DeleteOrderError
    {
    }
}

// The POST /orders/{id}/pay failures: which rule failed. Closed the same way.
public abstract class PayError
{
    private PayError() { }

    // No stored order matches the id.
    public sealed class NotFound(int orderId) : PayError
    {
        public int OrderId { get; } = orderId;
    }

    // The card charge was declined; the order is not marked paid.
    public sealed class Declined : PayError
    {
    }
}
