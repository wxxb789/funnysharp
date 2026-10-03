using System;
using System.Collections.Generic;
using System.Linq;
using FunnySharp;
using FunnySharp.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public static class Api
{
    public static void MapApi(WebApplication app)
    {
        app.MapPost("/orders", (OrderRequest request) => CreateOrder(request));

        app.MapGet("/products/{id}", (string id) =>
            Option.FromNullable(ShopData.FindProduct(id))
                .ToHttpResult(() => Problem(
                    StatusCodes.Status404NotFound,
                    $"product-not-found:{id}")));

        app.MapDelete("/orders/{id:int}", (int id) =>
            DeleteOrder(id).ToHttpResult(error => Problem(
                StatusCodes.Status409Conflict,
                error)));

        app.MapPost("/orders/{id:int}/pay", (int id, PaymentRequest request) =>
            PayOrder(id, request).ToHttpResult(error => Problem(
                error.Status,
                error.Detail)));
    }

    private static IResult CreateOrder(OrderRequest request)
    {
        var validated = ValidateOrder(request);
        if (!validated.TryGetValue(out var validRequest))
        {
            return validated.ToHttpResult(OrderProblems);
        }

        // Pricing is dependent on successful input validation. Unknown SKUs
        // accumulate here, and storage runs only after every line is priced.
        var order = validRequest!;
        return PriceOrder(order)
            .Map(total => new OrderCreated(ShopData.AddOrder(order.CustomerEmail, total)))
            .ToHttpResult(
                OrderProblems,
                created => Results.Created($"/orders/{created.OrderId}", created));
    }

    private static Validation<OrderRequest, string> ValidateOrder(OrderRequest request)
    {
        // Build every independent check before combining their outcomes.
        // Zip and Traverse retain errors in rule order and then line order.
        var email = Check(HasValidEmail(request.CustomerEmail), "invalid-email");
        var nonEmpty = Check(request.Lines.Count > 0, "empty-order");
        var quantities = request.Lines.Traverse(line => Check(
            line.Quantity is >= 1 and <= 99,
            $"invalid-quantity:{line.Sku}"));
        var promo = Check(
            request.PromoCode is null or "" or "SAVE10" or "FREESHIP",
            "unknown-promo");

        return email
            .Zip(nonEmpty)
            .Zip(quantities)
            .Zip(promo)
            .Map(_ => request);
    }

    private static Validation<bool, string> Check(bool accepted, string error) =>
        accepted
            ? Validation<bool, string>.Valid(true)
            : Validation<bool, string>.Invalid(error);

    private static bool HasValidEmail(string? email)
    {
        var at = email?.IndexOf('@') ?? -1;
        return at >= 0 && email!.IndexOf('.', at + 1) >= 0;
    }

    private static Validation<decimal, string> PriceOrder(OrderRequest request) =>
        request.Lines
            .Traverse(line => Option.FromNullable(ShopData.FindProduct(line.Sku))
                .Match(
                    product => Validation<decimal, string>.Valid(product.Price * line.Quantity),
                    () => Validation<decimal, string>.Invalid($"unknown-sku:{line.Sku}")))
            .Map(lineTotals =>
            {
                var subtotal = lineTotals.Sum();
                var goods = request.PromoCode == "SAVE10" ? subtotal * 0.90m : subtotal;
                var shipping = request.PromoCode == "FREESHIP" ? 0m : 5m;
                return decimal.Round(goods + shipping, 2);
            });

    private static UnitResult<string> DeleteOrder(int id) =>
        Option.FromNullable(ShopData.FindOrder(id))
            .ToResult($"order-not-found:{id}")
            .Ensure(order => !order.Paid, "order-paid")
            .ToUnitResult()
            .Bind(() => ShopData.RemoveOrder(id)
                ? UnitResult<string>.Success()
                : UnitResult<string>.Failure($"order-not-found:{id}"));

    private static Result<PaymentAccepted, PaymentFailure> PayOrder(
        int id,
        PaymentRequest request) =>
        Option.FromNullable(ShopData.FindOrder(id))
            .ToResult(new PaymentFailure(
                StatusCodes.Status404NotFound,
                $"order-not-found:{id}"))
            .Bind(order => ChargeOrder(order, request));

    private static Result<PaymentAccepted, PaymentFailure> ChargeOrder(
        StoredOrder order,
        PaymentRequest request)
    {
        if (!ShopData.Charge(request.CardToken, order.Total))
        {
            return Result<PaymentAccepted, PaymentFailure>.Failure(new PaymentFailure(
                StatusCodes.Status402PaymentRequired,
                "payment-declined"));
        }

        ShopData.MarkOrderPaid(order.Id);
        return Result<PaymentAccepted, PaymentFailure>.Success(new PaymentAccepted(true));
    }

    private static HttpValidationProblemDetails OrderProblems(IReadOnlyList<string> errors) =>
        new(new Dictionary<string, string[]>
        {
            ["order"] = errors.ToArray(),
        })
        {
            Status = StatusCodes.Status400BadRequest,
        };

    private static ProblemDetails Problem(int status, string detail) => new()
    {
        Status = status,
        Detail = detail,
    };

    private sealed record PaymentFailure(int Status, string Detail);
}
