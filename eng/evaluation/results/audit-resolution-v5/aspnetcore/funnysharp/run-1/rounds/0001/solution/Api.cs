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

        app.MapDelete("/orders/{id:int}", (int id) => DeleteOrder(id));

        app.MapPost("/orders/{id:int}/pay", (int id, PaymentRequest request) =>
            PayOrder(id, request));
    }

    private static IResult CreateOrder(OrderRequest request)
    {
        // Independent checks accumulate before dependent catalog lookup begins.
        return ValidateOrder(request).Match(
            validated => PriceOrder(validated)
                .Map(total => new OrderCreated(
                    ShopData.AddOrder(validated.CustomerEmail, total)))
                .ToHttpResult(
                    OrderProblem,
                    created => Results.Created($"/orders/{created.OrderId}", created)),
            errors => Results.Problem(OrderProblem(errors)));
    }

    private static Validation<OrderRequest, string> ValidateOrder(OrderRequest request)
    {
        var lines = request.Lines ?? Array.Empty<OrderLine>();
        var at = request.CustomerEmail?.IndexOf('@') ?? -1;
        var emailIsValid = at >= 0 && request.CustomerEmail!.IndexOf('.', at + 1) >= 0;

        var email = emailIsValid
            ? Validation<string, string>.Valid(request.CustomerEmail!)
            : Validation<string, string>.Invalid("invalid-email");

        var nonEmpty = lines.Count > 0
            ? Validation<IReadOnlyList<OrderLine>, string>.Valid(lines)
            : Validation<IReadOnlyList<OrderLine>, string>.Invalid("empty-order");

        var quantities = lines.Traverse(line =>
            line.Quantity is >= 1 and <= 99
                ? Validation<OrderLine, string>.Valid(line)
                : Validation<OrderLine, string>.Invalid($"invalid-quantity:{line.Sku}"));

        var promo = request.PromoCode is null or "" or "SAVE10" or "FREESHIP"
            ? Validation<string?, string>.Valid(request.PromoCode)
            : Validation<string?, string>.Invalid("unknown-promo");

        // Zip retains left-to-right rule order, including each line's quantity error.
        return email.Zip(nonEmpty)
            .Zip(quantities)
            .Zip(promo)
            .Map(_ => request with { Lines = lines });
    }

    private static Validation<decimal, string> PriceOrder(OrderRequest request)
    {
        // Unlike fail-fast Result traversal, Validation reports every missing SKU.
        return request.Lines.Traverse(line =>
            Option.FromNullable(ShopData.FindProduct(line.Sku)).Match(
                product => Validation<decimal, string>.Valid(product.Price * line.Quantity),
                () => Validation<decimal, string>.Invalid($"unknown-sku:{line.Sku}")))
            .Map(lineTotals =>
            {
                var subtotal = lineTotals.Sum();
                var goods = request.PromoCode == "SAVE10" ? subtotal * 0.90m : subtotal;
                var shipping = request.PromoCode == "FREESHIP" ? 0m : 5m;
                return decimal.Round(goods + shipping, 2);
            });
    }

    private static IResult DeleteOrder(int id)
    {
        var missing = new HttpFailure(
            StatusCodes.Status409Conflict, $"order-not-found:{id}");

        // Absence and a paid-order conflict both prevent removal; success has no payload.
        return Option.FromNullable(ShopData.FindOrder(id))
            .ToResult(missing)
            .Ensure(order => !order.Paid,
                new HttpFailure(StatusCodes.Status409Conflict, "order-paid"))
            .ToUnitResult()
            .Bind(() => ShopData.RemoveOrder(id)
                ? UnitResult<HttpFailure>.Success()
                : UnitResult<HttpFailure>.Failure(missing))
            .ToHttpResult(ToProblem);
    }

    private static IResult PayOrder(int id, PaymentRequest request)
    {
        // Lookup failure skips charging, and charge failure skips marking the order paid.
        return Option.FromNullable(ShopData.FindOrder(id))
            .ToResult(new HttpFailure(
                StatusCodes.Status404NotFound, $"order-not-found:{id}"))
            .Bind(order => ShopData.Charge(request.CardToken, order.Total)
                ? Result<StoredOrder, HttpFailure>.Success(order)
                : Result<StoredOrder, HttpFailure>.Failure(
                    new HttpFailure(StatusCodes.Status402PaymentRequired, "payment-declined")))
            .Map(order =>
            {
                ShopData.MarkOrderPaid(order.Id);
                return new PaymentAccepted(true);
            })
            .ToHttpResult(ToProblem);
    }

    private static HttpValidationProblemDetails OrderProblem(IReadOnlyList<string> errors) =>
        new(new Dictionary<string, string[]>
        {
            ["order"] = errors.ToArray(),
        })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Order validation failed",
        };

    private static ProblemDetails ToProblem(HttpFailure failure) =>
        Problem(failure.Status, failure.Detail);

    private static ProblemDetails Problem(int status, string detail) => new()
    {
        Status = status,
        Detail = detail,
    };

    private sealed record HttpFailure(int Status, string Detail);
}
