using FunnySharp;
using FunnySharp.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

// The Minimal API surface over the shop. Every endpoint keeps its failure, absence, and
// accumulation semantics visible: which rule failed, which status it maps to, and why.

public static class Api
{
    public static void MapApi(WebApplication app)
    {
        // POST /orders — the four independent checks all run and their codes accumulate in
        // rule order; pricing (every sku must resolve) runs only on a valid request.
        app.MapPost("/orders", (OrderRequest request) =>
            ShopRules.CheckOrder(request).ToHttpResult(
                InvalidOrderProblem, // 400: every code, in rule order, under errors["order"]
                PlaceOrder));

        // GET /products/{id} — a known product is 200; an absent one is 404.
        app.MapGet("/products/{id}", (string id) =>
            ShopData.FindProduct(id)
                .ToOption() // absence: the id matches no catalog product
                .ToHttpResult(() => ProductProblem(id)));

        // DELETE /orders/{id} — an unknown order is 409; a paid order stays stored (409);
        // any other order is removed (204).
        app.MapDelete("/orders/{id:int}", (int id) =>
            ShopRules.DeleteOrder(id).ToHttpResult(DeleteProblem));

        // POST /orders/{id}/pay — an unknown order is 404; a declined charge is 402 and
        // leaves the order unpaid; a successful charge marks the order paid (200).
        app.MapPost("/orders/{id:int}/pay", (int id, PaymentRequest payment) =>
            ShopRules.PayOrder(id, payment.CardToken).ToHttpResult(
                PayProblem,
                accepted => Results.Ok(accepted)));
    }

    // Pricing depends on a valid request: every sku must resolve in the catalog — unknown
    // skus accumulate one code per missing line, in line order — then the order is stored.
    private static IResult PlaceOrder(OrderRequest valid) =>
        ShopRules.PriceOrder(valid).ToHttpResult(
            InvalidOrderProblem, // 400: unknown-sku:<sku> codes under errors["order"]
            total => StoreOrder(valid.CustomerEmail, total));

    // Store the priced order and report its id with 201.
    private static IResult StoreOrder(string customerEmail, decimal total)
    {
        var orderId = ShopData.AddOrder(customerEmail, total);
        return Results.Created($"/orders/{orderId}", new OrderCreated(orderId));
    }

    // 400 with every reported code, in order, under the single key "order".
    private static HttpValidationProblemDetails InvalidOrderProblem(IReadOnlyList<string> codes) =>
        new(OrderErrors(codes))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid order",
        };

    private static Dictionary<string, string[]> OrderErrors(IReadOnlyList<string> codes) =>
        new() { ["order"] = [.. codes] };

    // GET: an unknown product is 404.
    private static ProblemDetails ProductProblem(string id) => new()
    {
        Status = StatusCodes.Status404NotFound,
        Title = "Product not found",
        Detail = $"product-not-found:{id}",
    };

    // DELETE: both failures are 409 conflicts.
    private static ProblemDetails DeleteProblem(DeleteOrderError error) => error switch
    {
        DeleteOrderError.NotFound missing => new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Order conflict",
            Detail = $"order-not-found:{missing.OrderId}",
        },

        DeleteOrderError.Paid => new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Order conflict",
            Detail = "order-paid",
        },

        // The private constructor closes the hierarchy; no other case exists.
        _ => throw new System.Diagnostics.UnreachableException(),
    };

    // PAY: an unknown order is 404, a declined charge is 402.
    private static ProblemDetails PayProblem(PayError error) => error switch
    {
        PayError.NotFound missing => new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Order not found",
            Detail = $"order-not-found:{missing.OrderId}",
        },

        PayError.Declined => new ProblemDetails
        {
            Status = StatusCodes.Status402PaymentRequired,
            Title = "Payment declined",
            Detail = "payment-declined",
        },

        // The private constructor closes the hierarchy; no other case exists.
        _ => throw new System.Diagnostics.UnreachableException(),
    };
}
