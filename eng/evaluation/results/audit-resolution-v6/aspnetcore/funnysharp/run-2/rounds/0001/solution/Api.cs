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
            PayOrder(id, request).ToHttpResult(error => error switch
            {
                PaymentError.OrderNotFound => Problem(
                    StatusCodes.Status404NotFound,
                    $"order-not-found:{id}"),
                PaymentError.Declined => Problem(
                    StatusCodes.Status402PaymentRequired,
                    "payment-declined"),
                _ => throw new InvalidOperationException("Unknown payment error."),
            }));
    }

    private static IResult CreateOrder(OrderRequest request)
    {
        var validated = RequestChecks(request).Sequence().Map(_ => request);

        // Pricing depends on a valid request. Validation intentionally has no Bind:
        // independent checks accumulate, then ordinary branching starts the next stage.
        Validation<OrderCreated, string> outcome = validated.Match(
            validRequest => PriceOrder(validRequest).Map(total =>
                new OrderCreated(ShopData.AddOrder(validRequest.CustomerEmail, total))),
            errors => Validation<OrderCreated, string>.InvalidMany(errors));

        return outcome.ToHttpResult(
            OrderProblem,
            created => Results.Created($"/orders/{created.OrderId}", created));
    }

    private static IEnumerable<Validation<bool, string>> RequestChecks(OrderRequest request)
    {
        // Sequence visits every independent check and retains errors in this order.
        yield return Check(IsValidEmail(request.CustomerEmail), "invalid-email");
        yield return Check(request.Lines is { Count: > 0 }, "empty-order");

        if (request.Lines is not null)
        {
            foreach (var line in request.Lines)
            {
                yield return Check(
                    line.Quantity is >= 1 and <= 99,
                    $"invalid-quantity:{line.Sku}");
            }
        }

        yield return Check(
            request.PromoCode is null or "" or "SAVE10" or "FREESHIP",
            "unknown-promo");
    }

    private static Validation<bool, string> Check(bool accepted, string error) =>
        accepted
            ? Validation<bool, string>.Valid(true)
            : Validation<bool, string>.Invalid(error);

    private static bool IsValidEmail(string? email)
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
                var goods = lineTotals.Sum();
                var discountedGoods = request.PromoCode == "SAVE10" ? goods * 0.90m : goods;
                var shipping = request.PromoCode == "FREESHIP" ? 0m : 5m;
                return decimal.Round(discountedGoods + shipping, 2);
            });

    private static UnitResult<string> DeleteOrder(int id) =>
        Option.FromNullable(ShopData.FindOrder(id))
            .ToResult($"order-not-found:{id}")
            .Ensure(order => !order.Paid, "order-paid")
            .ToUnitResult()
            .Bind(() => ShopData.RemoveOrder(id)
                ? UnitResult<string>.Success()
                : UnitResult<string>.Failure($"order-not-found:{id}"));

    private static Result<PaymentAccepted, PaymentError> PayOrder(int id, PaymentRequest request) =>
        Option.FromNullable(ShopData.FindOrder(id))
            .ToResult(PaymentError.OrderNotFound)
            // Charge runs only for a found order; marking paid runs only after acceptance.
            .Ensure(order => ShopData.Charge(request.CardToken, order.Total), PaymentError.Declined)
            .Map(order =>
            {
                ShopData.MarkOrderPaid(order.Id);
                return new PaymentAccepted(true);
            });

    private static HttpValidationProblemDetails OrderProblem(IReadOnlyList<string> errors) =>
        new(new Dictionary<string, string[]>
        {
            ["order"] = errors.ToArray(),
        })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Order validation failed",
        };

    private static ProblemDetails Problem(int status, string detail) =>
        new()
        {
            Status = status,
            Detail = detail,
        };

    private enum PaymentError
    {
        OrderNotFound,
        Declined,
    }
}
