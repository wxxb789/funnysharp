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
        // Validation accumulates errors; dependent stages run only after it succeeds.
        return Validate(request).Match(
            validRequest => Price(validRequest, catalog).Match(
                total => ChargeAndSave(validRequest, total, gateway, repository, cardToken),
                errors => Rejected(errors)),
            errors => Rejected(errors));
    }

    private static Validation<OrderRequest, string> Validate(OrderRequest request)
    {
        var errors = new List<string>();

        var email = request.CustomerEmail;
        int at = email is null ? -1 : email.IndexOf('@');
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
            if (line.Quantity < 1 || line.Quantity > 99)
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
        // Missing prices are absence. Traversal converts each absence to an error
        // and accumulates all missing SKUs while looking up every line once.
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

    private static OrderOutcome ChargeAndSave(
        OrderRequest request,
        decimal total,
        PaymentGateway gateway,
        OrderRepository repository,
        string cardToken)
    {
        // Payment is a no-value command. Bind prevents persistence after decline.
        UnitResult<string> payment = gateway.Charge(cardToken, total)
            ? UnitResult<string>.Success()
            : UnitResult<string>.Failure("payment-declined");

        return payment
            .ToResult(() => total)
            .Bind(paidTotal =>
                Option.FromTry<int>(
                    (out int orderId) => repository.Save(
                        request.CustomerEmail, paidTotal, out orderId))
                    .ToResult("save-failed")
                    .Map(orderId => new OrderOutcome(
                        true, orderId, paidTotal, Array.Empty<string>())))
            .Match(
                placed => placed,
                error => Rejected(new[] { error }, total));
    }

    private static OrderOutcome Rejected(IReadOnlyList<string> errors, decimal total = 0m)
        => new(false, 0, total, errors);
}
