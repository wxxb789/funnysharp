using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

public static class Api
{
    public static void MapApi(WebApplication app)
    {
        app.MapPost("/orders", (OrderRequest request) => CreateOrder(request));
        app.MapGet("/products/{id}", (string id) => GetProduct(id));
        app.MapDelete("/orders/{id:int}", (int id) => DeleteOrder(id));
        app.MapPost("/orders/{id:int}/pay", (int id, PaymentRequest request) => PayOrder(id, request));
    }

    private static IResult CreateOrder(OrderRequest request)
    {
        var errors = new List<string>();
        var lines = request.Lines ?? Array.Empty<OrderLine>();

        // Independent checks accumulate errors in business-rule order.
        if (!IsValidEmail(request.CustomerEmail))
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

        // Pricing runs only after validation; every missing SKU is reported.
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

        decimal goodsTotal = request.PromoCode == "SAVE10" ? subtotal * 0.90m : subtotal;
        decimal shipping = request.PromoCode == "FREESHIP" ? 0m : 5m;
        decimal total = decimal.Round(goodsTotal + shipping, 2);

        int orderId = ShopData.AddOrder(request.CustomerEmail, total);
        return Results.Created($"/orders/{orderId}", new OrderCreated(orderId));
    }

    private static bool IsValidEmail(string? email)
    {
        if (email is null)
        {
            return false;
        }

        int atIndex = email.IndexOf('@');
        return atIndex >= 0 && email.IndexOf('.', atIndex + 1) >= 0;
    }

    private static IResult OrderValidationProblem(List<string> errors) =>
        Results.ValidationProblem(
            new Dictionary<string, string[]> { ["order"] = errors.ToArray() },
            statusCode: StatusCodes.Status400BadRequest);

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
}
