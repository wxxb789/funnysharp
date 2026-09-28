using FunnySharp;

// The shop domain behind the endpoints. Outcomes ride FunnySharp carriers: Option<T> for
// absence (a missing product or order), Validation<TValue, TError> for the accumulated order
// rules, and Result<TValue, TError> / UnitResult<TError> for the fail-fast steps.

public static class Shop
{
    private const decimal Shipping = 5.00m;
    private const decimal Save10Rate = 0.10m;

    // POST /orders — the four independent request rules run together and report every
    // failure in rule order; pricing and storage run only when all four passed.
    public static Validation<OrderCreated, string> CreateOrder(OrderRequest request) =>
        ValidateRequest(request).Match(
            valid => PriceOrder(valid),
            codes => Validation<OrderCreated, string>.InvalidMany(codes));

    // GET /products/{id} — a missing product is absence, not an error.
    public static Option<Product> FindProduct(string id) => ShopData.FindProduct(id).ToOption();

    // DELETE /orders/{id} — the order must exist and be unpaid; only then is it removed.
    public static UnitResult<DeleteOrderError> DeleteOrder(int id) =>
        FindOrder(id)
            .ToResult(() => DeleteOrderError.NotFound(id))
            .Ensure(order => !order.Paid, _ => DeleteOrderError.AlreadyPaid())
            .Map(order => ShopData.RemoveOrder(order.Id))
            .ToUnitResult();

    // POST /orders/{id}/pay — the order must exist and the charge must go through; only a
    // successful charge marks the order paid.
    public static Result<PaymentAccepted, PayOrderError> PayOrder(int id, string cardToken) =>
        FindOrder(id)
            .ToResult(() => PayOrderError.NotFound(id))
            .Bind(order => Charge(order, cardToken));

    private static Option<StoredOrder> FindOrder(int id) => ShopData.FindOrder(id).ToOption();

    // The four independent request rules, in report order: the Zip combiner's arguments are
    // the accumulation order, left to right.
    private static Validation<OrderRequest, string> ValidateRequest(OrderRequest request) =>
        ValidateEmail(request.CustomerEmail).Zip(
            ValidateLinesPresent(request.Lines),
            ValidateQuantities(request.Lines),
            ValidatePromo(request.PromoCode),
            (email, lines, quantities, promo) => request);

    // Rule 1: invalid-email — the address must contain '@' and a '.' after it.
    private static Validation<string, string> ValidateEmail(string? customerEmail)
    {
        var atIndex = customerEmail?.IndexOf('@') ?? -1;
        return atIndex >= 0 && customerEmail!.IndexOf('.', atIndex + 1) > atIndex
            ? Validation<string, string>.Valid(customerEmail!)
            : Validation<string, string>.Invalid("invalid-email");
    }

    // Rule 2: empty-order — at least one line is required.
    private static Validation<IReadOnlyList<OrderLine>, string> ValidateLinesPresent(IReadOnlyList<OrderLine>? lines) =>
        lines is { Count: > 0 }
            ? Validation<IReadOnlyList<OrderLine>, string>.Valid(lines)
            : Validation<IReadOnlyList<OrderLine>, string>.Invalid("empty-order");

    // Rule 3: invalid-quantity:<sku> — every line's quantity must be within 1..99; traversal
    // accumulates one error per offending line, in line order.
    private static Validation<IReadOnlyList<OrderLine>, string> ValidateQuantities(IReadOnlyList<OrderLine>? lines) =>
        (lines ?? []).Traverse(ValidateQuantity);

    private static Validation<OrderLine, string> ValidateQuantity(OrderLine line) =>
        line.Quantity is >= 1 and <= 99
            ? Validation<OrderLine, string>.Valid(line)
            : Validation<OrderLine, string>.Invalid($"invalid-quantity:{line.Sku}");

    // Rule 4: unknown-promo — only null, empty, SAVE10, and FREESHIP are known codes.
    private static Validation<string, string> ValidatePromo(string? promoCode) =>
        promoCode is null or "" or "SAVE10" or "FREESHIP"
            ? Validation<string, string>.Valid(promoCode ?? "")
            : Validation<string, string>.Invalid("unknown-promo");

    // Pricing — every line's sku must exist in the catalog; unknown skus accumulate one
    // unknown-sku:<sku> error per missing line, in line order. Only a fully priced order is
    // stored.
    private static Validation<OrderCreated, string> PriceOrder(OrderRequest request) =>
        request.Lines.Traverse(PriceLine)
            .Map(pricedLines => StoreOrder(request.CustomerEmail, Total(pricedLines, request.PromoCode)));

    private static Validation<(Product Product, int Quantity), string> PriceLine(OrderLine line) =>
        FindProduct(line.Sku).Match(
            product => Validation<(Product, int), string>.Valid((product, line.Quantity)),
            () => Validation<(Product, int), string>.Invalid($"unknown-sku:{line.Sku}"));

    // The total is the goods subtotal plus shipping; SAVE10 removes 10% of the goods subtotal
    // (shipping unaffected) and FREESHIP removes the shipping; the final total is rounded to
    // two decimals.
    private static decimal Total(IReadOnlyList<(Product Product, int Quantity)> pricedLines, string? promoCode)
    {
        var goods = pricedLines.Sum(line => line.Product.Price * line.Quantity);
        var total = promoCode switch
        {
            "SAVE10" => goods - (goods * Save10Rate) + Shipping,
            "FREESHIP" => goods,
            _ => goods + Shipping,
        };
        return Math.Round(total, 2);
    }

    private static OrderCreated StoreOrder(string customerEmail, decimal total) =>
        new(ShopData.AddOrder(customerEmail, total));

    // A declined charge records nothing and marks nothing paid; a successful charge records
    // the payment and marks the order paid.
    private static Result<PaymentAccepted, PayOrderError> Charge(StoredOrder order, string cardToken) =>
        ShopData.Charge(cardToken, order.Total)
            ? Result<PaymentAccepted, PayOrderError>.Success(MarkPaid(order))
            : Result<PaymentAccepted, PayOrderError>.Failure(PayOrderError.Declined());

    private static PaymentAccepted MarkPaid(StoredOrder order)
    {
        ShopData.MarkOrderPaid(order.Id);
        return new PaymentAccepted(Paid: true);
    }
}

// The two ways DELETE /orders/{id} is rejected; the endpoint maps each rule to its status.
// The factories return the base type so the fail-fast steps carry either rule.
public abstract record DeleteOrderError
{
    public static DeleteOrderError NotFound(int orderId) => new OrderNotFound(orderId);

    public static DeleteOrderError AlreadyPaid() => new OrderAlreadyPaid();

    public sealed record OrderNotFound(int OrderId) : DeleteOrderError;

    public sealed record OrderAlreadyPaid : DeleteOrderError;
}

// The two ways POST /orders/{id}/pay is rejected; the endpoint maps each rule to its status.
// The factories return the base type so the fail-fast steps carry either rule.
public abstract record PayOrderError
{
    public static PayOrderError NotFound(int orderId) => new OrderNotFound(orderId);

    public static PayOrderError Declined() => new PaymentDeclined();

    public sealed record OrderNotFound(int OrderId) : PayOrderError;

    public sealed record PaymentDeclined : PayOrderError;
}
