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
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/orders", (OrderRequest request) => CreateOrder(request));

        app.MapGet("/products/{id}", (string id) =>
            Option.FromNullable(ShopData.FindProduct(id)).ToHttpResult(
                () => Problem(StatusCodes.Status404NotFound, $"product-not-found:{id}")));

        app.MapDelete("/orders/{id:int}", (int id) => DeleteOrder(id));

        app.MapPost("/orders/{id:int}/pay", (int id, PaymentRequest request) =>
            PayOrder(id, request));
    }

    private static IResult CreateOrder(OrderRequest request) =>
        ValidateOrder(request).ToHttpResult(
            ValidationProblem,
            validRequest => PriceOrder(validRequest)
                .Map(total => new OrderCreated(
                    ShopData.AddOrder(validRequest.CustomerEmail, total)))
                .ToHttpResult(
                    ValidationProblem,
                    created => Results.Created($"/orders/{created.OrderId}", created)));

    private static Validation<OrderRequest, string> ValidateOrder(OrderRequest request)
    {
        // These checks are independent: construct every outcome before combining them.
        var email = IsValidEmail(request.CustomerEmail)
            ? Validation<string, string>.Valid(request.CustomerEmail)
            : Validation<string, string>.Invalid("invalid-email");

        var lines = request.Lines.Count > 0
            ? Validation<IReadOnlyList<OrderLine>, string>.Valid(request.Lines)
            : Validation<IReadOnlyList<OrderLine>, string>.Invalid("empty-order");

        var quantities = request.Lines.Traverse(line =>
            line.Quantity is >= 1 and <= 99
                ? Validation<OrderLine, string>.Valid(line)
                : Validation<OrderLine, string>.Invalid($"invalid-quantity:{line.Sku}"));

        var promo = request.PromoCode is null or "" or "SAVE10" or "FREESHIP"
            ? Validation<string?, string>.Valid(request.PromoCode)
            : Validation<string?, string>.Invalid("unknown-promo");

        // Zip accumulates errors in operand order; Traverse retains line order.
        return email.Zip(lines).Zip(quantities).Zip(promo).Map(_ => request);
    }

    private static bool IsValidEmail(string email)
    {
        if (email is null)
        {
            return false;
        }

        var at = email.IndexOf('@');
        return at >= 0 && email.IndexOf('.', at + 1) >= 0;
    }

    private static Validation<decimal, string> PriceOrder(OrderRequest request) =>
        request.Lines.Traverse(line =>
            Option.FromNullable(ShopData.FindProduct(line.Sku)).Match(
                product => Validation<decimal, string>.Valid(product.Price * line.Quantity),
                () => Validation<decimal, string>.Invalid($"unknown-sku:{line.Sku}")))
            .Map(lineTotals =>
            {
                var subtotal = lineTotals.Sum();
                var goodsTotal = request.PromoCode == "SAVE10"
                    ? subtotal * 0.90m
                    : subtotal;
                var shipping = request.PromoCode == "FREESHIP" ? 0m : 5m;

                return Math.Round(goodsTotal + shipping, 2);
            });

    private static IResult DeleteOrder(int id) =>
        Option.FromNullable(ShopData.FindOrder(id))
            .ToResult($"order-not-found:{id}")
            .Ensure(order => !order.Paid, "order-paid")
            .ToUnitResult()
            .Bind(() => ShopData.RemoveOrder(id)
                ? UnitResult<string>.Success()
                : UnitResult<string>.Failure($"order-not-found:{id}"))
            .ToHttpResult(error => Problem(StatusCodes.Status409Conflict, error));

    private static IResult PayOrder(int id, PaymentRequest request) =>
        Option.FromNullable(ShopData.FindOrder(id))
            .ToResult(new PaymentFailure(
                StatusCodes.Status404NotFound, $"order-not-found:{id}"))
            .Bind(order => ChargeAndMarkPaid(order, request))
            .ToHttpResult(error => Problem(error.Status, error.Detail));

    private static Result<PaymentAccepted, PaymentFailure> ChargeAndMarkPaid(
        StoredOrder order,
        PaymentRequest request)
    {
        if (!ShopData.Charge(request.CardToken, order.Total))
        {
            return Result<PaymentAccepted, PaymentFailure>.Failure(
                new PaymentFailure(StatusCodes.Status402PaymentRequired, "payment-declined"));
        }

        ShopData.MarkOrderPaid(order.Id);
        return Result<PaymentAccepted, PaymentFailure>.Success(new PaymentAccepted(true));
    }

    private static HttpValidationProblemDetails ValidationProblem(IReadOnlyList<string> errors) =>
        new(new Dictionary<string, string[]>
        {
            ["order"] = errors.ToArray(),
        })
        {
            Status = StatusCodes.Status400BadRequest,
        };

    private static ProblemDetails Problem(int status, string detail) =>
        new()
        {
            Status = status,
            Detail = detail,
        };

    private sealed record PaymentFailure(int Status, string Detail);
}
