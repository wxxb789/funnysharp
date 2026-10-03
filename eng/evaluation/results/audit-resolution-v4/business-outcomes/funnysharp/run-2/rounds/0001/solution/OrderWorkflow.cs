using System;
using System.Collections.Generic;
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
        // Validation and pricing accumulate errors; later steps are fail-fast.
        return Validate(request).Match(
            validRequest => Price(validRequest, catalog).Match(
                total => ChargeAndSave(validRequest, total, gateway, repository, cardToken),
                errors => Rejected(errors)),
            errors => Rejected(errors));
    }

    private static Validation<OrderRequest, string> Validate(OrderRequest request)
    {
        var errors = new List<string>();

        string email = request.CustomerEmail;
        int at = email?.IndexOf('@') ?? -1;
        if (at < 0 || email!.IndexOf('.', at + 1) < 0)
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

    private static Validation<decimal, string> Price(
        OrderRequest request,
        ProductCatalog catalog)
    {
        // Validation traversal visits every line, retaining all missing SKUs in order.
        var pricedLines = request.Lines.Traverse(line =>
            Option.FromTry<decimal>(
                (out decimal price) => catalog.TryGetPrice(line.Sku, out price))
            .Match(
                price => Validation<decimal, string>.Valid(price * line.Quantity),
                () => Validation<decimal, string>.Invalid($"unknown-sku:{line.Sku}")));

        return pricedLines.Map(lineTotals =>
        {
            decimal goodsSubtotal = 0m;
            foreach (decimal lineTotal in lineTotals)
            {
                goodsSubtotal += lineTotal;
            }

            decimal discountedGoods = request.PromoCode == "SAVE10"
                ? goodsSubtotal - goodsSubtotal * 0.10m
                : goodsSubtotal;
            decimal shipping = request.PromoCode == "FREESHIP" ? 0m : 5m;

            return Math.Round(discountedGoods + shipping, 2);
        });
    }

    private static OrderOutcome ChargeAndSave(
        OrderRequest request,
        decimal total,
        PaymentGateway gateway,
        OrderRepository repository,
        string cardToken)
    {
        var payment = gateway.Charge(cardToken, total)
            ? UnitResult<string>.Success()
            : UnitResult<string>.Failure("payment-declined");

        // Bind prevents even a persistence attempt after a declined payment.
        return payment
            .ToResult(() => total)
            .Bind(paidTotal => repository.Save(request.CustomerEmail, paidTotal, out var orderId)
                ? Result<int, string>.Success(orderId)
                : Result<int, string>.Failure("save-failed"))
            .Match(
                orderId => new OrderOutcome(true, orderId, total, Array.Empty<string>()),
                error => new OrderOutcome(false, 0, total, new[] { error }));
    }

    private static OrderOutcome Rejected(IReadOnlyList<string> errors) =>
        new(false, 0, 0m, errors);
}
