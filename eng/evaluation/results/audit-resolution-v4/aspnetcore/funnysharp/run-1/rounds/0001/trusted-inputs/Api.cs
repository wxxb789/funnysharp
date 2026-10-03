using FunnySharp;
using FunnySharp.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        // Pricing is dependent on a valid request: do not look up SKUs until
        // every independent request check has completed successfully.
        Validation<decimal, string> priced = ValidateOrder(request).Match(
            valid => PriceOrder(valid),
            errors => Validation<decimal, string>.InvalidMany(errors));

        return priced
            .Map(total => new OrderCreated(ShopData.AddOrder(request.CustomerEmail, total)))
            .ToHttpResult(
                OrderProblem,
                created => Results.Created($"/orders/{created.OrderId}", created));
    }

    private static Validation<OrderRequest, string> ValidateOrder(OrderRequest request)
    {
        var errors = new List<string>();
        var lines = request.Lines ?? Array.Empty<OrderLine>();

        // These checks are independent. Append all failures in rule order.
        if (!HasValidEmail(request.CustomerEmail))
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

        return errors.Count == 0
            ? Validation<OrderRequest, string>.Valid(request with { Lines = lines })
            : Validation<OrderRequest, string>.InvalidMany(errors);
    }

    private static bool HasValidEmail(string? email)
    {
        if (email is null)
        {
            return false;
        }

        int at = email.IndexOf('@');
        return at >= 0 && email.IndexOf('.', at + 1) >= 0;
    }

    private static Validation<decimal, string> PriceOrder(OrderRequest request)
    {
        // Validation traversal visits every line and retains all missing SKUs
        // in line order, rather than stopping at the first catalog miss.
        return request.Lines.Traverse(PriceLine).Map(prices =>
        {
            decimal goods = prices.Sum();
            decimal discountedGoods = request.PromoCode == "SAVE10" ? goods * 0.90m : goods;
            decimal shipping = request.PromoCode == "FREESHIP" ? 0m : 5m;
            return decimal.Round(discountedGoods + shipping, 2);
        });
    }

    private static Validation<decimal, string> PriceLine(OrderLine line) =>
        Option.FromNullable(ShopData.FindProduct(line.Sku)).Match(
            product => Validation<decimal, string>.Valid(product.Price * line.Quantity),
            () => Validation<decimal, string>.Invalid($"unknown-sku:{line.Sku}"));

    private static HttpValidationProblemDetails OrderProblem(IReadOnlyList<string> errors) =>
        new(new Dictionary<string, string[]>
        {
            ["order"] = errors.ToArray(),
        })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Order validation failed",
        };

    private static IResult GetProduct(string id) =>
        Option.FromNullable(ShopData.FindProduct(id)).ToHttpResult(
            () => Problem(new ApiFailure(
                StatusCodes.Status404NotFound,
                $"product-not-found:{id}")));

    private static IResult DeleteOrder(int id)
    {
        var notFound = new ApiFailure(StatusCodes.Status409Conflict, $"order-not-found:{id}");
        var paid = new ApiFailure(StatusCodes.Status409Conflict, "order-paid");

        // A delete is valueless work. Both absence and a paid-order guard
        // produce explicit failures; removal runs only after the guard passes.
        UnitResult<ApiFailure> deleted = Option.FromNullable(ShopData.FindOrder(id)).Match(
            order => UnitResult<ApiFailure>.Success()
                .Ensure(() => !order.Paid, paid)
                .Bind(() => ShopData.RemoveOrder(id)
                    ? UnitResult<ApiFailure>.Success()
                    : UnitResult<ApiFailure>.Failure(notFound)),
            () => UnitResult<ApiFailure>.Failure(notFound));

        return deleted.ToHttpResult(Problem);
    }

    private static IResult PayOrder(int id, PaymentRequest request) =>
        Option.FromNullable(ShopData.FindOrder(id))
            .ToResult(new ApiFailure(StatusCodes.Status404NotFound, $"order-not-found:{id}"))
            .Ensure(
                order => ShopData.Charge(request.CardToken, order.Total),
                new ApiFailure(StatusCodes.Status402PaymentRequired, "payment-declined"))
            .Map(order =>
            {
                // Only a successful charge reaches this stage.
                ShopData.MarkOrderPaid(order.Id);
                return new PaymentAccepted(true);
            })
            .ToHttpResult(Problem);

    private static ProblemDetails Problem(ApiFailure failure) => new()
    {
        Status = failure.Status,
        Detail = failure.Detail,
    };

    private sealed record ApiFailure(int Status, string Detail);
}
