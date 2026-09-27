using FunnySharp;
using FunnySharp.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

// The HTTP surface. Every handler runs a domain step on a FunnySharp carrier and maps the
// outcome with the FunnySharp.AspNetCore ToHttpResult extensions. Each problem mapper below
// states which rule failed, the status it maps to, and why, so the failure policy is
// readable at the endpoint.

public static class Api
{
    public static void MapApi(WebApplication app)
    {
        // POST /orders — the independent request rules accumulate into one 400 problem that
        // reports every code in rule order under the single key "order"; a valid request is
        // priced, stored, and answered with 201.
        app.MapPost("/orders", (OrderRequest request) =>
            Shop.CreateOrder(request).ToHttpResult(
                codes => OrderValidationProblem(codes),
                created => Results.Created($"/orders/{created.OrderId}", created)));

        // GET /products/{id} — a known product is returned with 200; absence is a 404 problem.
        app.MapGet("/products/{id}", (string id) =>
            Shop.FindProduct(id).ToHttpResult(() => ProductNotFound(id)));

        // DELETE /orders/{id} — an unknown order is rejected with order-not-found:<id> and an
        // already-paid order with order-paid (both 409, the order stays stored); an unpaid
        // order is removed and the command answers 204.
        app.MapDelete("/orders/{id}", (int id) =>
            Shop.DeleteOrder(id).ToHttpResult(
                DeleteProblem,
                () => Results.NoContent()));

        // POST /orders/{id}/pay — an unknown order is a 404 problem and a declined charge a
        // 402 problem that charges nothing and marks nothing paid; a successful charge marks
        // the order paid and answers 200.
        app.MapPost("/orders/{id}/pay", (int id, PaymentRequest payment) =>
            Shop.PayOrder(id, payment.CardToken).ToHttpResult(
                PayProblem,
                accepted => Results.Ok(accepted)));
    }

    // POST /orders failures: every reported rule code, in rule order, under the single
    // key "order".
    private static HttpValidationProblemDetails OrderValidationProblem(IEnumerable<string> codes) => new(
        new Dictionary<string, string[]> { ["order"] = codes.ToArray() })
    {
        Status = StatusCodes.Status400BadRequest,
        Title = "order-validation-failed",
    };

    // GET /products/{id} absence: the unknown product id is the problem detail.
    private static ProblemDetails ProductNotFound(string id) => new()
    {
        Status = StatusCodes.Status404NotFound,
        Title = "product-not-found",
        Detail = $"product-not-found:{id}",
    };

    // DELETE failures: both rules reject the delete with 409 and keep the order stored.
    private static ProblemDetails DeleteProblem(DeleteOrderError error) => error switch
    {
        DeleteOrderError.OrderNotFound notFound => Problem(
            StatusCodes.Status409Conflict, "order-not-found", $"order-not-found:{notFound.OrderId}"),
        DeleteOrderError.OrderAlreadyPaid => Problem(
            StatusCodes.Status409Conflict, "order-paid", "order-paid"),
        _ => throw new InvalidOperationException($"Unhandled delete failure: {error}."),
    };

    // Pay failures: an unknown order is 404, a declined charge is 402 and nothing is charged.
    private static ProblemDetails PayProblem(PayOrderError error) => error switch
    {
        PayOrderError.OrderNotFound notFound => Problem(
            StatusCodes.Status404NotFound, "order-not-found", $"order-not-found:{notFound.OrderId}"),
        PayOrderError.PaymentDeclined => Problem(
            StatusCodes.Status402PaymentRequired, "payment-declined", "payment-declined"),
        _ => throw new InvalidOperationException($"Unhandled pay failure: {error}."),
    };

    private static ProblemDetails Problem(int status, string title, string detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
    };
}
