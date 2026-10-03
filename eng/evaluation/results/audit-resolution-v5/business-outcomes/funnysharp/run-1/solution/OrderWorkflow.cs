using System;
using System.Collections.Generic;
using System.Linq;
using FunnySharp;

public static class OrderWorkflow
{
    public static OrderOutcome PlaceOrder(
        OrderRequest request,
        ProductCatalog catalog,
        PaymentGateway gateway,
        OrderRepository repository,
        string cardToken)
    {
        // Each stage starts only after the previous stage succeeds.
        return Validate(request).Match(
            validRequest => Price(validRequest, catalog).Match(
                total => ChargeAndSave(validRequest, total, gateway, repository, cardToken),
                errors => NotPlaced(errors)),
            errors => NotPlaced(errors));
    }

    private static Validation<OrderRequest, string> Validate(OrderRequest request)
    {
        // Independent checks accumulate in the business-defined order.
        var errors = new List<string>();

        if (!HasValidEmail(request.CustomerEmail))
        {
            errors.Add("invalid-email");
        }

        if (request.Lines.Count == 0)
        {
            errors.Add("empty-order");
        }

        foreach (var line in request.Lines)
        {
            if (line.Quantity is < 1 or > 99)
            {
                errors.Add($"invalid-quantity:{line.Sku}");
            }
        }

        if (request.PromoCode is not (null or "" or "SAVE10" or "FREESHIP"))
        {
            errors.Add("unknown-promo");
        }

        return errors.Count == 0
            ? Validation<OrderRequest, string>.Valid(request)
            : Validation<OrderRequest, string>.InvalidMany(errors);
    }

    private static bool HasValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return false;
        }

        int at = email.IndexOf('@');
        return at >= 0 && email.IndexOf('.', at + 1) >= 0;
    }

    private static Validation<decimal, string> Price(
        OrderRequest request,
        ProductCatalog catalog)
    {
        // Validation traversal visits every line, retaining every missing SKU
        // in source order instead of stopping at the first missing price.
        Validation<IReadOnlyList<decimal>, string> lineTotals = request.Lines.Traverse(
            line => PriceLine(line, catalog));

        return lineTotals.Map(totals =>
        {
            decimal goodsSubtotal = totals.Sum();
            decimal discount = request.PromoCode == "SAVE10" ? goodsSubtotal * 0.10m : 0m;
            decimal shipping = request.PromoCode == "FREESHIP" ? 0m : 5.00m;
            return decimal.Round(goodsSubtotal - discount + shipping, 2);
        });
    }

    private static Validation<decimal, string> PriceLine(
        OrderLine line,
        ProductCatalog catalog)
    {
        // Catalog absence is explicit before it becomes a domain validation error.
        Option<decimal> price = Option.FromTry<decimal>(
            (out decimal value) => catalog.TryGetPrice(line.Sku, out value));

        return price.Match(
            value => Validation<decimal, string>.Valid(value * line.Quantity),
            () => Validation<decimal, string>.Invalid($"unknown-sku:{line.Sku}"));
    }

    private static OrderOutcome ChargeAndSave(
        OrderRequest request,
        decimal total,
        PaymentGateway gateway,
        OrderRepository repository,
        string cardToken)
    {
        UnitResult<string> payment = gateway.Charge(cardToken, total)
            ? UnitResult<string>.Success()
            : UnitResult<string>.Failure("payment-declined");

        // Payment is valueless; persistence produces an ID. Bind ensures that
        // a declined payment cannot even attempt persistence.
        return payment
            .ToResult(() => total)
            .Bind(paidTotal => Save(request.CustomerEmail, paidTotal, repository))
            .Match(
                orderId => new OrderOutcome(true, orderId, total, Array.Empty<string>()),
                error => new OrderOutcome(false, 0, total, new[] { error }));
    }

    private static Result<int, string> Save(
        string customerEmail,
        decimal total,
        OrderRepository repository)
    {
        return repository.Save(customerEmail, total, out int orderId)
            ? Result<int, string>.Success(orderId)
            : Result<int, string>.Failure("save-failed");
    }

    private static OrderOutcome NotPlaced(IReadOnlyList<string> errors)
    {
        return new OrderOutcome(false, 0, 0m, errors);
    }
}
