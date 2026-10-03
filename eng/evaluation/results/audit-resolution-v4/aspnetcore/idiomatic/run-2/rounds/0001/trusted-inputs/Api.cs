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
        // Independent request checks accumulate before catalog lookup or storage.
        var errors = ValidateOrder(request);
        if (errors.Count > 0)
        {
            return OrderValidationProblem(errors);
        }

        decimal goodsSubtotal = 0m;
        foreach (var line in request.Lines)
        {
            var product = ShopData.FindProduct(line.Sku);
            if (product is null)
            {
                errors.Add($"unknown-sku:{line.Sku}");
            }
            else
            {
                goodsSubtotal += product.Price * line.Quantity;
            }
        }

        if (errors.Count > 0)
        {
            return OrderValidationProblem(errors);
        }

        decimal shipping = 5m;
        switch (request.PromoCode)
        {
            case "SAVE10":
                goodsSubtotal *= 0.90m;
                break;
            case "FREESHIP":
                shipping = 0m;
                break;
        }

        var total = decimal.Round(goodsSubtotal + shipping, 2);
        var orderId = ShopData.AddOrder(request.CustomerEmail, total);
        return Results.Created($"/orders/{orderId}", new OrderCreated(orderId));
    }

    private static List<string> ValidateOrder(OrderRequest request)
    {
        var errors = new List<string>();
        var email = request.CustomerEmail;
        var atIndex = email?.IndexOf('@') ?? -1;
        if (atIndex < 0 || email!.IndexOf('.', atIndex + 1) < 0)
        {
            errors.Add("invalid-email");
        }

        if (request.Lines is null || request.Lines.Count == 0)
        {
            errors.Add("empty-order");
        }

        if (request.Lines is not null)
        {
            foreach (var line in request.Lines)
            {
                if (line.Quantity is < 1 or > 99)
                {
                    errors.Add($"invalid-quantity:{line.Sku}");
                }
            }
        }

        if (request.PromoCode is not (null or "" or "SAVE10" or "FREESHIP"))
        {
            errors.Add("unknown-promo");
        }

        return errors;
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
