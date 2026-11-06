using FunnySharp;
using FunnySharp.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FunnySharp.DocumentationSamples;

internal static class AspNetCoreSamples
{
    private static void MapOutcomes()
    {
        var option = Option.Some(new Product(1));
        var result = Result<Product, ReservationError>.Failure(new ReservationError(
            "conflict",
            "reservation",
            "Reservation already exists."));
        var validation = Validation<Product, ReservationError>.InvalidMany([
            new ReservationError("required", "email", "Email is required."),
            new ReservationError("format", "email", "Email must contain '@'."),
            new ReservationError("range", "partySize", "Party size must be at least 1."),
        ]);

        // <snippet DocumentationSamples.AspNetCore.MappingOutcomes>
        IResult optionResult = option.ToHttpResult(NotFound);
        IResult resultResult = result.ToHttpResult(Conflict);
        IResult validationResult = validation.ToHttpResult(MapValidationFailure);
        // </snippet>
    }

    private static void MapTypedOutcomes(WebApplication app)
    {
        // <snippet DocumentationSamples.AspNetCore.TypedOutcomes>
        app.MapGet("/products/{id:int}", (int id) =>
            FindProduct(id).ToHttpResults(() => TypedResults.NotFound(NotFound())));

        app.MapPost("/orders", (CreateOrderRequest request) =>
            ValidateOrder(request).ToHttpResults(
                errors => TypedResults.ValidationProblem(GroupValidation(errors)),
                order => TypedResults.Created($"/orders/{order.Id}", order)));
        // </snippet>
    }

    private static void MapTypedFailurePolicy(WebApplication app)
    {
        // <snippet DocumentationSamples.AspNetCore.TypedFailurePolicy>
        app.MapGet("/orders/{id}/typed", (string id) =>
            FindOrder(id).ToHttpResults(MapOrderFailure));

        app.MapGet("/orders/{id}/dynamic", (string id) =>
                FindOrder(id).ToHttpResults(error => TypedResults.Problem(Conflict(error))))
            .ProducesProblem(StatusCodes.Status409Conflict);
        // </snippet>
    }

    private static void MapTypedEffectOutcome(WebApplication app, Catalog catalog)
    {
        // <snippet DocumentationSamples.AspNetCore.TypedEffectOutcome>
        app.MapGet("/catalog/{id:int}/refresh", async (HttpContext context, int id) =>
        {
            var product = await LoadProductEffect(id).RunAsync(catalog, context.RequestAborted);
            return product.ToHttpResults(() => TypedResults.NotFound(NotFound()));
        });
        // </snippet>
    }

    private static void MapTypedAsyncOutcome(WebApplication app)
    {
        // <snippet DocumentationSamples.AspNetCore.TypedAsyncOutcome>
        app.MapGet("/inventory/{sku}", (string sku, CancellationToken cancellationToken) =>
            GetStockAsync(sku, cancellationToken).ToHttpResultsAsync(
                error => TypedResults.NotFound(NotFound(error))));
        // </snippet>
    }

    private static void MapAsyncOutcomes(WebApplication app)
    {
        // <snippet DocumentationSamples.AspNetCore.MapAsyncOutcomes>
        app.MapGet("/inventory/{sku}", (string sku, CancellationToken cancellationToken) =>
            GetInventoryAsync(sku, cancellationToken).ToHttpResultAsync(NotFound));

        app.MapGet("/deliveries/{sku}", (string sku, CancellationToken cancellationToken) =>
            GetDeliveryAsync(sku, cancellationToken).ToHttpResultAsync(
                Conflict,
                delivery => Results.Accepted(value: delivery)));
        // </snippet>
    }

    private static void MapProblemDetails()
    {
        // <snippet DocumentationSamples.AspNetCore.MapProblemDetails>
        static ProblemDetails NotFound() => new()
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Not found",
        };

        static ProblemDetails Conflict(ReservationError error) => new()
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Reservation conflict",
            Detail = error.Code,
        };
        // </snippet>

        _ = NotFound();
        _ = Conflict(new ReservationError("conflict", "reservation", "Reservation already exists."));
    }

    private static void MapEffect(WebApplication app, Catalog catalog)
    {
        // <snippet DocumentationSamples.AspNetCore.MapEffect>
        app.MapGet("/catalog/{id:int}/refresh", (HttpContext context, int id) =>
            LoadProductEffect(id).ToHttpResultAsync(
                catalog,
                context,
                () => NotFound()));
        // </snippet>
    }

    private static async Task<IResult> CompareBeforeAndAfterAsync(
        int id,
        Catalog catalog,
        HttpContext context)
    {
        // <snippet DocumentationSamples.AspNetCore.CompareBeforeAndAfter>
        // Before
        var product = await LoadProductEffect(id).RunAsync(catalog, context.RequestAborted);
        _ = product.Match(
            value => Results.Ok(value),
            () => Results.Problem(statusCode: StatusCodes.Status404NotFound));

        // After
        _ = await LoadProductEffect(id).ToHttpResultAsync(
            catalog,
            context,
            () => new ProblemDetails { Status = StatusCodes.Status404NotFound });
        // </snippet>

        return Results.NoContent();
    }

    private static Task<Option<Product>> GetInventoryAsync(string sku, CancellationToken cancellationToken) =>
        Task.FromResult(Option.Some(new Product(1)));

    private static Task<Result<Delivery, ReservationError>> GetDeliveryAsync(
        string sku,
        CancellationToken cancellationToken) =>
        Task.FromResult(Result<Delivery, ReservationError>.Success(new Delivery(sku)));

    private static Effect<Catalog, Option<Product>> LoadProductEffect(int id) =>
        Effect.FromSync((Catalog catalog, CancellationToken cancellationToken) => Option.Some(new Product(id)));

    private static ProblemDetails NotFound() => new()
    {
        Status = StatusCodes.Status404NotFound,
        Title = "Not found",
    };

    private static ProblemDetails Conflict(ReservationError error) => new()
    {
        Status = StatusCodes.Status409Conflict,
        Title = "Reservation conflict",
        Detail = error.Code,
    };

    private static Option<Product> FindProduct(int id) =>
        id > 0 ? Option.Some(new Product(id)) : Option.None<Product>();

    private static Validation<Order, ReservationError> ValidateOrder(CreateOrderRequest request) =>
        request.Quantity > 0
            ? Validation<Order, ReservationError>.Valid(new Order("ORD-1"))
            : Validation<Order, ReservationError>.Invalid(
                new ReservationError("range", "quantity", "Quantity must be positive."));

    private static Result<Order, ReservationError> FindOrder(string id) =>
        id == "ORD-1"
            ? Result<Order, ReservationError>.Success(new Order(id))
            : Result<Order, ReservationError>.Failure(
                new ReservationError("not-found", "order", "Order was not found."));

    private static Results<NotFound<ProblemDetails>, Conflict<ProblemDetails>> MapOrderFailure(
        ReservationError error) =>
        error.Code == "conflict"
            ? TypedResults.Conflict(Conflict(error))
            : TypedResults.NotFound(NotFound(error));

    private static Task<Result<Product, ReservationError>> GetStockAsync(
        string sku,
        CancellationToken cancellationToken) =>
        Task.FromResult(Result<Product, ReservationError>.Failure(
            new ReservationError("not-found", "sku", "SKU was not found.")));

    private static Dictionary<string, string[]> GroupValidation(IReadOnlyList<ReservationError> errors) =>
        errors
            .GroupBy(static error => error.Field, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.Select(static error => error.Message).ToArray(),
                StringComparer.Ordinal);

    private static ProblemDetails NotFound(ReservationError error) => new()
    {
        Status = StatusCodes.Status404NotFound,
        Title = "Not found",
        Detail = error.Code,
    };

    private static HttpValidationProblemDetails MapValidationFailure(
        IReadOnlyList<ReservationError> errors) =>
        new(GroupValidation(errors))
        {
            Status = StatusCodes.Status400BadRequest,
        };

    private sealed record CreateOrderRequest(int Quantity);

    private sealed record Order(string Id);

    private sealed record Product(int Id);

    private sealed record Delivery(string Sku);

    private sealed record ReservationError(string Code, string Field, string Message);

    private sealed class Catalog;
}
