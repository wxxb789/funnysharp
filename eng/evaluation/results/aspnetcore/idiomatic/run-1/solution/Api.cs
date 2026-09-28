using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

/// <summary>
/// The shop's minimal API. Every endpoint makes its failure, absence, and success rules
/// explicit: which rule failed, which status that failure maps to, and what stays stored.
/// </summary>
public static class Api
{
    private const string OrderErrorKey = "order";
    private const decimal Shipping = 5.00m;

    public static void MapApi(WebApplication app)
    {
        app.MapPost("/orders", CreateOrder);
        app.MapGet("/products/{id}", GetProduct);
        app.MapDelete("/orders/{id}", DeleteOrder);
        app.MapPost("/orders/{id}/pay", PayOrder);
    }

    // POST /orders: every independent rule first (each failure reported, in rule order),
    // then the unknown-sku pass, and only then pricing and storage.
    private static IResult CreateOrder(OrderRequest request)
    {
        var ruleFailures = CollectRuleFailures(request);
        if (ruleFailures.Count > 0)
        {
            return InvalidOrder(ruleFailures);
        }

        var unknownSkus = request.Lines
            .Where(line => ShopData.FindProduct(line.Sku) is null)
            .Select(line => $"unknown-sku:{line.Sku}")
            .ToList();
        if (unknownSkus.Count > 0)
        {
            return InvalidOrder(unknownSkus);
        }

        var orderId = ShopData.AddOrder(request.CustomerEmail, PriceOrder(request));
        return Results.Created($"/orders/{orderId}", new OrderCreated(orderId));
    }

    // The four independent order rules, each failure collected in rule order.
    private static List<string> CollectRuleFailures(OrderRequest request)
    {
        var failures = new List<string>();

        if (!LooksLikeEmail(request.CustomerEmail))
        {
            failures.Add("invalid-email");
        }

        if (request.Lines.Count == 0)
        {
            failures.Add("empty-order");
        }

        failures.AddRange(request.Lines
            .Where(line => line.Quantity is < 1 or > 99)
            .Select(line => $"invalid-quantity:{line.Sku}"));

        if (request.PromoCode is not (null or "" or "SAVE10" or "FREESHIP"))
        {
            failures.Add("unknown-promo");
        }

        return failures;
    }

    // An address needs an '@' and a '.' somewhere after it.
    private static bool LooksLikeEmail(string email)
    {
        var at = email.IndexOf('@');
        return at >= 0 && email.IndexOf('.', at) >= 0;
    }

    // Goods plus shipping, less the promo's discount, rounded to two decimals.
    // Every sku is known to exist by the time an order is priced.
    private static decimal PriceOrder(OrderRequest request)
    {
        var goods = request.Lines.Sum(line => ShopData.FindProduct(line.Sku)!.Price * line.Quantity);
        return Math.Round(request.PromoCode switch
        {
            "SAVE10" => goods * 0.9m + Shipping, // 10% off the goods; shipping unaffected.
            "FREESHIP" => goods,                 // Shipping removed.
            _ => goods + Shipping,
        }, 2);
    }

    private static IResult InvalidOrder(IReadOnlyList<string> failureCodes) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [OrderErrorKey] = [.. failureCodes] });

    // GET /products/{id}.
    private static IResult GetProduct(string id)
    {
        if (ShopData.FindProduct(id) is not { } product)
        {
            return Results.Problem($"product-not-found:{id}", statusCode: StatusCodes.Status404NotFound);
        }

        return Results.Ok(product);
    }

    // DELETE /orders/{id}.
    private static IResult DeleteOrder(int id)
    {
        var order = ShopData.FindOrder(id);
        if (order is null)
        {
            return Results.Problem($"order-not-found:{id}", statusCode: StatusCodes.Status409Conflict);
        }

        if (order.Paid)
        {
            return Results.Problem("order-paid", statusCode: StatusCodes.Status409Conflict);
        }

        ShopData.RemoveOrder(id);
        return Results.NoContent();
    }

    // POST /orders/{id}/pay.
    private static IResult PayOrder(int id, PaymentRequest payment)
    {
        var order = ShopData.FindOrder(id);
        if (order is null)
        {
            return Results.Problem($"order-not-found:{id}", statusCode: StatusCodes.Status404NotFound);
        }

        if (!ShopData.Charge(payment.CardToken, order.Total))
        {
            return Results.Problem("payment-declined", statusCode: StatusCodes.Status402PaymentRequired);
        }

        ShopData.MarkOrderPaid(id);
        return Results.Ok(new PaymentAccepted(Paid: true));
    }
}
