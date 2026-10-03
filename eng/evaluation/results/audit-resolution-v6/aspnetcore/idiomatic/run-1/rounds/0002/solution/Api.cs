using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

public static class Api
{
    public static void MapApi(WebApplication app)
    {
        app.MapPost("/orders", CreateOrder);
        app.MapGet("/products/{id}", GetProduct);
        app.MapDelete("/orders/{id:int}", DeleteOrder);
        app.MapPost("/orders/{id:int}/pay", PayOrder);
    }

    private static IResult CreateOrder(OrderRequest request)
    {
        var errors = new List<string>();
        var customerEmail = request.CustomerEmail ?? string.Empty;
        var lines = request.Lines ?? Array.Empty<OrderLine>();

        // Independent checks accumulate in business-rule order.
        var at = customerEmail.IndexOf('@');
        if (at < 0 || customerEmail.IndexOf('.', at + 1) < 0)
        {
            errors.Add("invalid-email");
        }

        if (lines.Count == 0)
        {
            errors.Add("empty-order");
        }

        foreach (var line in lines)
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

        if (errors.Count > 0)
        {
            return OrderValidationProblem(errors);
        }

        // Pricing depends on validation, but reports every missing SKU.
        decimal subtotal = 0m;
        foreach (var line in lines)
        {
            var product = ShopData.FindProduct(line.Sku);
            if (product is null)
            {
                errors.Add($"unknown-sku:{line.Sku}");
            }
            else
            {
                subtotal += product.Price * line.Quantity;
            }
        }

        if (errors.Count > 0)
        {
            return OrderValidationProblem(errors);
        }

        var discountedGoods = request.PromoCode == "SAVE10"
            ? subtotal * 0.90m
            : subtotal;
        var shipping = request.PromoCode == "FREESHIP" ? 0m : 5m;
        var total = Math.Round(discountedGoods + shipping, 2);

        var orderId = ShopData.AddOrder(customerEmail, total);
        return Results.Created($"/orders/{orderId}", new OrderCreated(orderId));
    }

    private static IResult GetProduct(string id)
    {
        var product = ShopData.FindProduct(id);
        if (product is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: $"product-not-found:{id}");
        }

        return Results.Ok(product);
    }

    private static IResult DeleteOrder(int id)
    {
        var order = ShopData.FindOrder(id);
        if (order is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                detail: $"order-not-found:{id}");
        }

        if (order.Paid)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                detail: "order-paid");
        }

        if (!ShopData.RemoveOrder(id))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                detail: $"order-not-found:{id}");
        }

        return Results.NoContent();
    }

    private static IResult PayOrder(int id, PaymentRequest request)
    {
        var order = ShopData.FindOrder(id);
        if (order is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                detail: $"order-not-found:{id}");
        }

        if (!ShopData.Charge(request.CardToken, order.Total))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status402PaymentRequired,
                detail: "payment-declined");
        }

        ShopData.MarkOrderPaid(id);
        return Results.Ok(new PaymentAccepted(true));
    }

    private static IResult OrderValidationProblem(List<string> errors) =>
        Results.ValidationProblem(
            new Dictionary<string, string[]> { ["order"] = errors.ToArray() },
            statusCode: StatusCodes.Status400BadRequest);
}
