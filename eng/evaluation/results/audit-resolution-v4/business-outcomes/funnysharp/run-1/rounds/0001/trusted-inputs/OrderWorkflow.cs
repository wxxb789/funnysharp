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
        // Each accumulating phase must succeed before the next phase runs.
        return Validate(request).Match(
            validRequest => PriceOrder(validRequest, catalog).Match(
                total => CompleteOrder(validRequest, total, gateway, repository, cardToken),
                errors => NotPlaced(0m, errors)),
            errors => NotPlaced(0m, errors));
    }

    private static Validation<OrderRequest, string> Validate(OrderRequest request)
    {
        var errors = new List<string>();

        int at = request.CustomerEmail.IndexOf('@');
        if (at < 0 || request.CustomerEmail.IndexOf('.', at + 1) < 0)
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

    private static Validation<decimal, string> PriceOrder(
        OrderRequest request,
        ProductCatalog catalog)
    {
        // Missing prices are absence at the catalog boundary. Validation then
        // accumulates a domain error for every missing line, in source order.
        Validation<IReadOnlyList<decimal>, string> lineTotals = request.Lines.Traverse(line =>
        {
            Option<decimal> price = Option.FromTry<decimal>(
                (out decimal value) => catalog.TryGetPrice(line.Sku, out value));

            return price.Match(
                value => Validation<decimal, string>.Valid(value * line.Quantity),
                () => Validation<decimal, string>.Invalid($"unknown-sku:{line.Sku}"));
        });

        return lineTotals.Map(amounts =>
        {
            decimal subtotal = amounts.Sum();
            decimal goods = request.PromoCode == "SAVE10" ? subtotal * 0.90m : subtotal;
            decimal shipping = request.PromoCode == "FREESHIP" ? 0m : 5.00m;
            return decimal.Round(goods + shipping, 2);
        });
    }

    private static OrderOutcome CompleteOrder(
        OrderRequest request,
        decimal total,
        PaymentGateway gateway,
        OrderRepository repository,
        string cardToken)
    {
        UnitResult<string> payment = gateway.Charge(cardToken, total)
            ? UnitResult<string>.Success()
            : UnitResult<string>.Failure("payment-declined");

        // Bind is fail-fast: a declined payment never invokes persistence.
        return payment
            .ToResult(() => total)
            .Bind(amount => Save(request.CustomerEmail, amount, repository)
                .Map(orderId => new OrderOutcome(
                    true, orderId, amount, Array.Empty<string>())))
            .Match(
                outcome => outcome,
                error => NotPlaced(total, new[] { error }));
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

    private static OrderOutcome NotPlaced(decimal total, IReadOnlyList<string> errors)
        => new(false, 0, total, errors);
}
