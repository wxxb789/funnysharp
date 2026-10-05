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
        // Dependent stages run only after the preceding stage succeeds.
        return Validate(request).Match(
            validRequest => Price(validRequest, catalog).Match(
                total => ChargeAndSave(validRequest, total, gateway, repository, cardToken),
                errors => NotPlaced(errors)),
            errors => NotPlaced(errors));
    }

    private static Validation<OrderRequest, string> Validate(OrderRequest request)
    {
        // Independent checks accumulate errors in business-rule order.
        var errors = new List<string>();
        string? email = request.CustomerEmail;
        int at = email?.IndexOf('@') ?? -1;
        if (email is null || at < 0 || email.IndexOf('.', at + 1) < 0)
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
        // Validation traversal visits every line and accumulates missing SKUs.
        Validation<IReadOnlyList<decimal>, string> lineTotals = request.Lines.Traverse(
            line => Option.FromTry<decimal>(
                    (out decimal price) => catalog.TryGetPrice(line.Sku, out price))
                .Match(
                    price => Validation<decimal, string>.Valid(price * line.Quantity),
                    () => Validation<decimal, string>.Invalid($"unknown-sku:{line.Sku}")));

        return lineTotals.Map(amounts =>
        {
            decimal goodsSubtotal = amounts.Sum();
            decimal discount = request.PromoCode == "SAVE10"
                ? goodsSubtotal * 0.10m
                : 0m;
            decimal shipping = request.PromoCode == "FREESHIP" ? 0m : 5.00m;
            return decimal.Round(goodsSubtotal - discount + shipping, 2);
        });
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

        // Bind prevents even a persistence attempt when payment is declined.
        return payment
            .ToResult(() => total)
            .Bind(chargedTotal => Save(request.CustomerEmail, chargedTotal, repository)
                .Map(orderId => new OrderOutcome(
                    true, orderId, chargedTotal, Array.Empty<string>())))
            .Match(
                placed => placed,
                error => NotPlaced(new[] { error }, total));
    }

    private static Result<int, string> Save(
        string customerEmail,
        decimal total,
        OrderRepository repository)
    {
        return repository.Save(customerEmail, total, out var orderId)
            ? Result<int, string>.Success(orderId)
            : Result<int, string>.Failure("save-failed");
    }

    private static OrderOutcome NotPlaced(IReadOnlyList<string> errors, decimal total = 0m)
    {
        return new OrderOutcome(false, 0, total, errors);
    }
}
