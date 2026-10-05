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
        app.MapPost("/orders", (OrderRequest request) =>
            CreateOrder(request).ToHttpResult(
                OrderProblem,
                created => Results.Created($"/orders/{created.OrderId}", created)));

        app.MapGet("/products/{id}", (string id) =>
            Option.FromNullable(ShopData.FindProduct(id)).ToHttpResult(
                () => Problem(StatusCodes.Status404NotFound, $"product-not-found:{id}")));

        app.MapDelete("/orders/{id:int}", (int id) =>
            DeleteOrder(id).ToHttpResult(
                error => Problem(StatusCodes.Status409Conflict, error)));

        app.MapPost("/orders/{id:int}/pay", (int id, PaymentRequest request) =>
            PayOrder(id, request).ToHttpResult(
                error => Problem(error.Status, error.Detail)));
    }

    private static Validation<OrderCreated, string> CreateOrder(OrderRequest request)
    {
        // Pricing depends on a valid request; it is not an independent validation check.
        return ValidateOrder(request).Match(
            valid => PriceOrder(valid).Map(total =>
                new OrderCreated(ShopData.AddOrder(valid.CustomerEmail, total))),
            errors => Validation<OrderCreated, string>.InvalidMany(errors));
    }

    private static Validation<OrderRequest, string> ValidateOrder(OrderRequest request)
    {
        var email = request.CustomerEmail;
        var at = email?.IndexOf('@') ?? -1;
        var lines = request.Lines ?? Array.Empty<OrderLine>();

        // Evaluate every independent check, then accumulate in business-rule order.
        var emailCheck = Check(
            at >= 0 && email!.IndexOf('.', at + 1) >= 0,
            "invalid-email");
        var nonEmptyCheck = Check(lines.Count > 0, "empty-order");
        var quantityChecks = lines.Traverse(line => Check(
            line.Quantity is >= 1 and <= 99,
            $"invalid-quantity:{line.Sku}"));
        var promoCheck = Check(
            request.PromoCode is null or "" or "SAVE10" or "FREESHIP",
            "unknown-promo");

        return emailCheck
            .Zip(nonEmptyCheck)
            .Zip(quantityChecks)
            .Zip(promoCheck)
            .Map(_ => request with { Lines = lines });
    }

    private static Validation<bool, string> Check(bool passes, string error) =>
        passes
            ? Validation<bool, string>.Valid(true)
            : Validation<bool, string>.Invalid(error);

    private static Validation<decimal, string> PriceOrder(OrderRequest request)
    {
        // Validation traversal reports every missing SKU in line order.
        var pricedLines = request.Lines.Traverse(line =>
            Option.FromNullable(ShopData.FindProduct(line.Sku)).Match(
                product => Validation<decimal, string>.Valid(product.Price * line.Quantity),
                () => Validation<decimal, string>.Invalid($"unknown-sku:{line.Sku}")));

        return pricedLines.Map(amounts =>
        {
            var goods = amounts.Sum();
            var discountedGoods = request.PromoCode == "SAVE10" ? goods * 0.90m : goods;
            var shipping = request.PromoCode == "FREESHIP" ? 0m : 5m;
            return decimal.Round(discountedGoods + shipping, 2);
        });
    }

    private static UnitResult<string> DeleteOrder(int id) =>
        Option.FromNullable(ShopData.FindOrder(id))
            .ToResult($"order-not-found:{id}")
            .Ensure(order => !order.Paid, "order-paid")
            .ToUnitResult()
            .Bind(() => ShopData.RemoveOrder(id)
                ? UnitResult<string>.Success()
                : UnitResult<string>.Failure($"order-not-found:{id}"));

    private static Result<PaymentAccepted, PaymentFailure> PayOrder(int id, PaymentRequest request) =>
        Option.FromNullable(ShopData.FindOrder(id))
            .ToResult(new PaymentFailure(
                StatusCodes.Status404NotFound, $"order-not-found:{id}"))
            .Bind(order =>
            {
                if (!ShopData.Charge(request.CardToken, order.Total))
                {
                    return Result<PaymentAccepted, PaymentFailure>.Failure(
                        new PaymentFailure(StatusCodes.Status402PaymentRequired, "payment-declined"));
                }

                ShopData.MarkOrderPaid(order.Id);
                return Result<PaymentAccepted, PaymentFailure>.Success(new PaymentAccepted(true));
            });

    private static HttpValidationProblemDetails OrderProblem(IReadOnlyList<string> errors) =>
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
