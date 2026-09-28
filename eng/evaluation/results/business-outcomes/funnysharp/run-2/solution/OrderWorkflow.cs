using System;
using System.Collections.Generic;
using System.Linq;

using FunnySharp;

// The order-placement workflow: validation accumulates every error, pricing reports every
// missing sku, and payment and persistence fail fast, in that order.
public static class OrderWorkflow
{
    private const decimal ShippingFee = 5.00m;

    private const string InvalidEmailError = "invalid-email";
    private const string EmptyOrderError = "empty-order";
    private const string InvalidQuantityPrefix = "invalid-quantity:";
    private const string UnknownPromoError = "unknown-promo";
    private const string UnknownSkuPrefix = "unknown-sku:";
    private const string PaymentDeclinedError = "payment-declined";
    private const string SaveFailedError = "save-failed";

    public static OrderOutcome PlaceOrder(
        OrderRequest request,
        ProductCatalog catalog,
        PaymentGateway gateway,
        OrderRepository repository,
        string cardToken)
    {
        // 1. Validation: every independent check runs and every failure is reported, in order.
        //    Nothing else runs when any check fails.
        Validation<ValidatedOrder, string> validation = ValidateOrder(request);
        if (!validation.TryGetValue(out var validated))
        {
            _ = validation.TryGetErrors(out var validationErrors);
            return NotPlaced(validationErrors!);
        }

        // 2. Pricing: every line needs a catalog price; every missing sku is reported, in line
        //    order, and nothing else runs when any is missing.
        Validation<PricedOrder, string> pricing = PriceOrder(validated!, catalog);
        if (!pricing.TryGetValue(out var priced))
        {
            _ = pricing.TryGetErrors(out var pricingErrors);
            return NotPlaced(pricingErrors!);
        }

        // 3. Payment: a fail-fast command; a declined card stops before anything is saved.
        UnitResult<string> payment = Charge(gateway, cardToken, priced!.Total);
        if (payment.TryGetError(out var paymentError))
        {
            return NotPlaced([paymentError!]);
        }

        // 4. Persistence: the save runs after the charge and assigns the order id.
        return Save(repository, priced!.CustomerEmail, priced!.Total).Match(
            orderId => new OrderOutcome(true, orderId, priced!.Total, []),
            error => NotPlaced([error]));
    }

    // Step 1: the independent checks, combined left to right so their errors accumulate in
    // the order invalid-email, empty-order, invalid-quantity:<sku>, unknown-promo.
    private static Validation<ValidatedOrder, string> ValidateOrder(OrderRequest request) =>
        ValidateEmail(request.CustomerEmail).Zip(
            ValidateLines(request.Lines),
            ValidatePromo(request.PromoCode),
            (email, lines, promoCode) => new ValidatedOrder(email, lines, promoCode));

    // invalid-email: the address needs an '@' and a '.' after it.
    private static Validation<string, string> ValidateEmail(string email)
    {
        int atIndex = email.IndexOf('@');
        bool dotAfterAt = atIndex >= 0 && email.IndexOf('.', atIndex + 1) >= 0;
        return dotAfterAt
            ? Validation<string, string>.Valid(email)
            : Validation<string, string>.Invalid(InvalidEmailError);
    }

    // The lines: the empty-order check and the per-line quantity checks both run; the
    // quantity-checked lines are the validated lines.
    private static Validation<IReadOnlyList<OrderLine>, string> ValidateLines(IReadOnlyList<OrderLine> lines) =>
        ValidateNonEmpty(lines)
            .Zip(lines.Traverse(ValidateQuantity))
            .Map(pair => pair.Second);

    // empty-order: an order needs at least one line.
    private static Validation<IReadOnlyList<OrderLine>, string> ValidateNonEmpty(IReadOnlyList<OrderLine> lines) =>
        lines.Count == 0
            ? Validation<IReadOnlyList<OrderLine>, string>.Invalid(EmptyOrderError)
            : Validation<IReadOnlyList<OrderLine>, string>.Valid(lines);

    // invalid-quantity:<sku>: the quantity must be within 1..99, one error per offending line.
    private static Validation<OrderLine, string> ValidateQuantity(OrderLine line) =>
        line.Quantity is >= 1 and <= 99
            ? Validation<OrderLine, string>.Valid(line)
            : Validation<OrderLine, string>.Invalid(InvalidQuantityPrefix + line.Sku);

    // unknown-promo: only no code, SAVE10, and FREESHIP are recognized.
    private static Validation<string?, string> ValidatePromo(string? promoCode) =>
        promoCode is null or "" or "SAVE10" or "FREESHIP"
            ? Validation<string?, string>.Valid(promoCode)
            : Validation<string?, string>.Invalid(UnknownPromoError);

    // Step 2: price every line; the total applies the promo effect and is rounded to two decimals.
    private static Validation<PricedOrder, string> PriceOrder(ValidatedOrder order, ProductCatalog catalog) =>
        order.Lines
            .Traverse(line => PriceLine(line, catalog))
            .Map(pricedLines => new PricedOrder(order.CustomerEmail, TotalOf(pricedLines, order.PromoCode)));

    // unknown-sku:<sku>: the catalog price is present or absent; each absent price is one error.
    private static Validation<PricedLine, string> PriceLine(OrderLine line, ProductCatalog catalog)
    {
        Option<decimal> price = CatalogPrice(catalog, line.Sku);
        if (price.TryGetValue(out decimal catalogPrice))
        {
            return Validation<PricedLine, string>.Valid(new PricedLine(line.Sku, line.Quantity, catalogPrice));
        }

        return Validation<PricedLine, string>.Invalid(UnknownSkuPrefix + line.Sku);
    }

    // The catalog price of a sku; a sku the catalog does not carry is absent.
    private static Option<decimal> CatalogPrice(ProductCatalog catalog, string sku) =>
        Option.FromTry<decimal>((out decimal price) => catalog.TryGetPrice(sku, out price));

    // The total: the goods subtotal plus shipping, adjusted by the promo effect and rounded to
    // two decimals. A promo code that names no effect pays the plain total.
    private static decimal TotalOf(IReadOnlyList<PricedLine> lines, string? promoCode)
    {
        decimal goodsSubtotal = lines.Sum(line => line.Price * line.Quantity);
        return PromoEffectFor(promoCode)
            .Map(effect => effect.ApplyTo(goodsSubtotal, ShippingFee))
            .GetValueOrElse(() => Math.Round(goodsSubtotal + ShippingFee, 2, MidpointRounding.AwayFromZero));
    }

    // The promo effect a promo code names; a code that names no effect is absent.
    private static Option<PromoEffect> PromoEffectFor(string? promoCode) =>
        promoCode switch
        {
            "SAVE10" => Option.Some(PromoEffect.Save10),
            "FREESHIP" => Option.Some(PromoEffect.FreeShip),
            _ => Option.None<PromoEffect>(),
        };

    // Step 3: the charge command; a declined card is the typed failure.
    private static UnitResult<string> Charge(PaymentGateway gateway, string cardToken, decimal total) =>
        gateway.Charge(cardToken, total)
            ? UnitResult<string>.Success()
            : UnitResult<string>.Failure(PaymentDeclinedError);

    // Step 4: persistence; a successful save assigns the order id, a failed one is reported.
    private static Result<int, string> Save(OrderRepository repository, string customerEmail, decimal total) =>
        repository.Save(customerEmail, total, out var orderId)
            ? Result<int, string>.Success(orderId)
            : Result<int, string>.Failure(SaveFailedError);

    // The outcome when a step failed: nothing ran after that step.
    private static OrderOutcome NotPlaced(IReadOnlyList<string> errors) => new(false, 0, 0m, errors);

    // The order draft after every validation check passed.
    private sealed record ValidatedOrder(string CustomerEmail, IReadOnlyList<OrderLine> Lines, string? PromoCode);

    // The order after pricing: who placed it and what they owe.
    private sealed record PricedOrder(string CustomerEmail, decimal Total);

    // One order line with its catalog price.
    private sealed record PricedLine(string Sku, int Quantity, decimal Price);

    // What a recognized promo code does to the goods subtotal and the shipping fee.
    private sealed record PromoEffect(decimal GoodsMultiplier, decimal ShippingMultiplier)
    {
        // SAVE10 removes 10% of the goods subtotal; the shipping is unaffected.
        public static readonly PromoEffect Save10 = new(GoodsMultiplier: 0.90m, ShippingMultiplier: 1m);

        // FREESHIP removes the shipping.
        public static readonly PromoEffect FreeShip = new(GoodsMultiplier: 1m, ShippingMultiplier: 0m);

        public decimal ApplyTo(decimal goodsSubtotal, decimal shipping) =>
            Math.Round((GoodsMultiplier * goodsSubtotal) + (ShippingMultiplier * shipping), 2, MidpointRounding.AwayFromZero);
    }
}
