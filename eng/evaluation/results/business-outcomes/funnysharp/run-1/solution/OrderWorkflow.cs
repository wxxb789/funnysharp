using FunnySharp;

// Order placement for the shop: validate the request, price the lines, charge the card, and
// persist the order. The steps run in that order, and each failure stops everything after it.
//
// FunnySharp carries the outcomes: Validation accumulates the independent request checks and
// the per-line catalog lookups, Option models the absent catalog price and the absent promo
// effect, UnitResult is the payment command, and Result carries the saved order id.
public static class OrderWorkflow
{
    private const decimal ShippingFee = 5.00m;

    public static OrderOutcome PlaceOrder(
        OrderRequest request,
        ProductCatalog catalog,
        PaymentGateway gateway,
        OrderRepository repository,
        string cardToken)
    {
        // Step 1 - validation: every independent check runs and every error is reported, in
        // rule order. On any error nothing else runs: no pricing, no charge, no save.
        Validation<ValidatedOrder, string> requestChecks = ValidateRequest(request);
        if (!requestChecks.TryGetValue(out var validated))
        {
            _ = requestChecks.TryGetErrors(out var errors);
            return NotPlaced(errors!);
        }

        // Step 2 - pricing: every line's sku must have a catalog price (each missing sku is
        // reported, in line order). The goods subtotal, the shipping fee, and the promo
        // effect produce the final total, rounded to two decimals.
        Validation<decimal, string> pricing = PriceOrder(validated!, catalog);
        if (!pricing.TryGetValue(out var total))
        {
            _ = pricing.TryGetErrors(out var unknownSkus);
            return NotPlaced(unknownSkus!);
        }

        // Step 3 - payment: the gateway charges the final total; a declined card stops the
        // workflow before anything is saved.
        UnitResult<string> payment = Charge(gateway, cardToken, total);
        if (payment.TryGetError(out var paymentError))
        {
            return NotPlaced([paymentError!]);
        }

        // Step 4 - persistence: the repository assigns the order id; a failed save is
        // reported after the charge.
        return SaveOrder(repository, validated!.CustomerEmail, total)
            .Match(
                orderId => new OrderOutcome(true, orderId, total, []),
                error => NotPlaced([error]));
    }

    // A failure outcome: nothing was placed, no order id was assigned, and no total is reported.
    private static OrderOutcome NotPlaced(IReadOnlyList<string> errors) => new(false, 0, 0m, errors);

    // Step 1: the independent request checks, combined applicatively so every error survives,
    // in rule order: invalid-email, empty-order / invalid-quantity:<sku>, unknown-promo.
    private static Validation<ValidatedOrder, string> ValidateRequest(OrderRequest request) =>
        ValidateEmail(request.CustomerEmail)
            .Zip(
                ValidateLines(request.Lines),
                ValidatePromo(request.PromoCode),
                (email, lines, promoCode) => new ValidatedOrder(email, lines, promoCode));

    // invalid-email: the address must contain '@' and a '.' after it.
    private static Validation<string, string> ValidateEmail(string? customerEmail)
    {
        var at = customerEmail?.IndexOf('@') ?? -1;
        return at >= 0 && customerEmail![(at + 1)..].Contains('.')
            ? Validation<string, string>.Valid(customerEmail!)
            : Validation<string, string>.Invalid("invalid-email");
    }

    // empty-order when no lines were submitted; invalid-quantity:<sku> for each line whose
    // quantity is outside 1..99, one error per offending line in line order.
    private static Validation<IReadOnlyList<OrderLine>, string> ValidateLines(IReadOnlyList<OrderLine> lines) =>
        lines.Count == 0
            ? Validation<IReadOnlyList<OrderLine>, string>.Invalid("empty-order")
            : lines.Traverse(ValidateQuantity);

    private static Validation<OrderLine, string> ValidateQuantity(OrderLine line) =>
        line.Quantity is >= 1 and <= 99
            ? Validation<OrderLine, string>.Valid(line)
            : Validation<OrderLine, string>.Invalid($"invalid-quantity:{line.Sku}");

    // unknown-promo: only SAVE10 and FREESHIP are known, besides no code at all.
    private static Validation<string?, string> ValidatePromo(string? promoCode) =>
        promoCode is null or "" or "SAVE10" or "FREESHIP"
            ? Validation<string?, string>.Valid(promoCode)
            : Validation<string?, string>.Invalid("unknown-promo");

    // Step 2: every line's sku must have a catalog price, then the priced lines produce the
    // final total.
    private static Validation<decimal, string> PriceOrder(ValidatedOrder order, ProductCatalog catalog) =>
        PriceLines(catalog, order.Lines)
            .Map(pricedLines => FinalTotal(pricedLines, PromoEffectFor(order.PromoCode)));

    // unknown-sku:<sku>: one error per line whose sku has no catalog price, in line order.
    private static Validation<IReadOnlyList<PricedLine>, string> PriceLines(
        ProductCatalog catalog,
        IReadOnlyList<OrderLine> lines) =>
        lines.Traverse(line => CatalogPrice(catalog, line.Sku)
            .Match(
                price => Validation<PricedLine, string>.Valid(new PricedLine(line.Sku, line.Quantity, price)),
                () => Validation<PricedLine, string>.Invalid($"unknown-sku:{line.Sku}")));

    // A catalog price lookup: a sku the catalog does not carry is an absence, not an error.
    private static Option<decimal> CatalogPrice(ProductCatalog catalog, string sku) =>
        Option.FromTry<decimal>((out decimal price) => catalog.TryGetPrice(sku, out price));

    // The final total: the goods subtotal plus the shipping fee, adjusted by the promo effect
    // and rounded to two decimals.
    private static decimal FinalTotal(IReadOnlyList<PricedLine> lines, Option<PromoEffect> promoEffect)
    {
        var goodsSubtotal = lines.Sum(line => line.Price * line.Quantity);
        return promoEffect.Match(
            effect => ApplyPromo(goodsSubtotal, effect),
            () => Round(goodsSubtotal + ShippingFee));
    }

    // SAVE10 removes 10% of the goods subtotal (shipping unaffected); FREESHIP removes the
    // shipping fee.
    private static decimal ApplyPromo(decimal goodsSubtotal, PromoEffect effect) =>
        effect switch
        {
            PromoEffect.TenPercentOffGoods => Round(goodsSubtotal * 0.90m + ShippingFee),
            PromoEffect.FreeShipping => Round(goodsSubtotal),
            _ => throw new ArgumentOutOfRangeException(nameof(effect), effect, "Unknown promo effect."),
        };

    // The final total is rounded to two decimals.
    private static decimal Round(decimal value) => decimal.Round(value, 2, MidpointRounding.ToEven);

    // The promo effect for a code: Some for a known code, None when the order carries no code.
    // Unknown codes cannot get here: ValidatePromo rejects them first.
    private static Option<PromoEffect> PromoEffectFor(string? promoCode) =>
        promoCode switch
        {
            null or "" => Option<PromoEffect>.None,
            "SAVE10" => Option.Some(PromoEffect.TenPercentOffGoods),
            "FREESHIP" => Option.Some(PromoEffect.FreeShipping),
            _ => throw new InvalidOperationException($"Unknown promo code '{promoCode}'."),
        };

    // Step 3: the payment command - no value is produced; a declined card is the only failure.
    private static UnitResult<string> Charge(PaymentGateway gateway, string cardToken, decimal total) =>
        gateway.Charge(cardToken, total)
            ? UnitResult<string>.Success()
            : UnitResult<string>.Failure("payment-declined");

    // Step 4: persistence - the save produces the order id the repository assigned.
    private static Result<int, string> SaveOrder(OrderRepository repository, string customerEmail, decimal total) =>
        repository.Save(customerEmail, total, out var orderId)
            ? Result<int, string>.Success(orderId)
            : Result<int, string>.Failure("save-failed");

    // The request pieces that survived validation.
    private sealed record ValidatedOrder(string CustomerEmail, IReadOnlyList<OrderLine> Lines, string? PromoCode);

    // An order line with its catalog price resolved.
    private sealed record PricedLine(string Sku, int Quantity, decimal Price);

    // The two supported promo effects.
    private enum PromoEffect
    {
        TenPercentOffGoods,
        FreeShipping,
    }
}
