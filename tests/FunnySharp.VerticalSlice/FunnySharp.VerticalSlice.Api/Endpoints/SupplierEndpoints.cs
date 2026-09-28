using FunnySharp.AspNetCore;
using FunnySharp.VerticalSlice.Application;
using FunnySharp.VerticalSlice.Domain;
using FunnySharp.VerticalSlice.Http;

namespace FunnySharp.VerticalSlice.Endpoints;

/// <summary>One row of the streaming quote endpoint; a failed supplier is an in-band outcome.</summary>
public sealed record QuoteStreamRow(
    string Supplier,
    string Sku,
    string Outcome,
    decimal? UnitPrice,
    string? Currency,
    int? LeadTimeDays,
    string? Reason);

/// <summary>
/// The supplier endpoints. The decision endpoint waits for every supplier under a concurrency cap;
/// the streaming endpoint delivers each answer as it arrives.
/// </summary>
public static class SupplierEndpoints
{
    public static void MapSupplierEndpoints(this WebApplication app)
    {
        app.MapGet("/suppliers/{sku}/quotes", QuoteAsync)
            .WithName("QuoteSuppliers")
            .Produces<QuoteResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/suppliers/{sku}/quotes/stream", StreamQuotesAsync)
            .WithName("StreamSupplierQuotes")
            .Produces<QuoteStreamRow>(StatusCodes.Status200OK, "application/x-ndjson")
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> QuoteAsync(
        string sku,
        int? quantity,
        SupplierQuoting quoting,
        ProblemMappings problems,
        CancellationToken cancellationToken)
    {
        var validated = OrderRequestValidation.ValidateQuote(sku, quantity);
        if (!validated.TryGetValue(out var query))
        {
            return validated.ToHttpResult(problems.FromValidation);
        }

        var board = await quoting.BestAsync(query.Sku, query.Quantity, cancellationToken);
        return board.ToHttpResult(problems.FromOrderError, board => Results.Ok(QuoteResponse.From(board)));
    }

    private static IResult StreamQuotesAsync(
        string sku,
        int? quantity,
        SupplierQuoting quoting,
        ProblemMappings problems,
        HttpContext context)
    {
        var validated = OrderRequestValidation.ValidateQuote(sku, quantity);
        if (!validated.TryGetValue(out var query))
        {
            return validated.ToHttpResult(problems.FromValidation);
        }

        context.Response.Headers.CacheControl = "no-store";
        return Results.Stream(
            async stream =>
            {
                var cancellationToken = context.RequestAborted;
                await foreach (var attempt in quoting.StreamAsync(query.Sku, query.Quantity, cancellationToken))
                {
                    await JsonLines.WriteAsync(stream, ToRow(query.Sku, attempt), cancellationToken);
                }
            },
            "application/x-ndjson");
    }

    private static QuoteStreamRow ToRow(Sku sku, Result<SupplierQuote, OrderError> attempt) =>
        attempt.Match(
            quote => new QuoteStreamRow(
                quote.Supplier,
                sku.Value,
                "quoted",
                quote.UnitPrice.Amount,
                quote.UnitPrice.Currency,
                quote.LeadTime.Days,
                null),
            error => new QuoteStreamRow(
                error is OrderError.SupplierUnavailable unavailable ? unavailable.Supplier : "unknown",
                sku.Value,
                "unavailable",
                null,
                null,
                null,
                error is OrderError.SupplierUnavailable unavailableError ? unavailableError.Reason : "unknown"));
}
