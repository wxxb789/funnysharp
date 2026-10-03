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
        // Independent checks accumulate; later stages run only after validation succeeds.
        return Validate(request).Match(
            validRequest => Price(validRequest, catalog).Match(
                total => ChargeAndSave(validRequest, total, gateway, repository, cardToken).Match(
                    orderId => new OrderOutcome(true, orderId, total, Array.Empty<string>()),
                    error => Failed(new[] { error }, total)),
                errors => Failed(errors)),
            errors => Failed(errors));
    }

    private static Validation<OrderRequest, string> Validate(OrderRequest request)
    {
        var errors = new List<string>();

        var email = request.CustomerEmail;
        int at = string.IsNullOrEmpty(email) ? -1 : email.IndexOf('@');
        if (at < 0 || email.IndexOf('.', at + 1) < 0)
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
        // Missing prices are absence. Validation traversal collects every missing SKU
        // while looking up each line exactly once, in source order.
        return request.Lines.Traverse(line =>
            Option.FromTry<decimal>(
                (out decimal price) => catalog.TryGetPrice(line.Sku, out price))
                .Match(
                    price => Validation<decimal, string>.Valid(price * line.Quantity),
                    () => Validation<decimal, string>.Invalid($"unknown-sku:{line.Sku}")))
            .Map(lineTotals =>
            {
                decimal goods = lineTotals.Sum();
                decimal discount = request.PromoCode == "SAVE10" ? goods * 0.10m : 0m;
                decimal shipping = request.PromoCode == "FREESHIP" ? 0m : 5.00m;
                return decimal.Round(goods - discount + shipping, 2);
            });
    }

    private static Result<int, string> ChargeAndSave(
        OrderRequest request,
        decimal total,
        PaymentGateway gateway,
        OrderRepository repository,
        string cardToken)
    {
        // Payment is a no-value command. Bind prevents even attempting persistence
        // when payment fails; a save failure does not retry or undo the charge.
        return UnitResult<string>.Success()
            .Ensure(() => gateway.Charge(cardToken, total), "payment-declined")
            .ToResult(() => total)
            .Bind(paidTotal =>
                repository.Save(request.CustomerEmail, paidTotal, out var orderId)
                    ? Result<int, string>.Success(orderId)
                    : Result<int, string>.Failure("save-failed"));
    }

    private static OrderOutcome Failed(IReadOnlyList<string> errors, decimal total = 0m)
        => new(false, 0, total, errors);
}
