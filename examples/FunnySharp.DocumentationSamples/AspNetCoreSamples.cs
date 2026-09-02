using FunnySharp;
using FunnySharp.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

    private static HttpValidationProblemDetails MapValidationFailure(
        IReadOnlyList<ReservationError> errors)
    {
        var grouped = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var error in errors)
        {
            if (!grouped.TryGetValue(error.Field, out var messages))
            {
                messages = [];
                grouped.Add(error.Field, messages);
            }

            messages.Add(error.Message);
        }

        return new HttpValidationProblemDetails(
            grouped.ToDictionary(static pair => pair.Key, static pair => pair.Value.ToArray(), StringComparer.Ordinal))
        {
            Status = StatusCodes.Status400BadRequest,
        };
    }

    private sealed record Product(int Id);

    private sealed record Delivery(string Sku);

    private sealed record ReservationError(string Code, string Field, string Message);

    private sealed class Catalog;
}
