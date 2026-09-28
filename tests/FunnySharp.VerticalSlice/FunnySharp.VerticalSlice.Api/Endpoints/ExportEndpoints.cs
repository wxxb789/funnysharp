using FunnySharp.AspNetCore;
using FunnySharp.VerticalSlice.Application;
using FunnySharp.VerticalSlice.Http;

namespace FunnySharp.VerticalSlice.Endpoints;

/// <summary>
/// The export endpoint. It writes NDJSON with a running aggregate as the port yields orders, so the
/// pipeline and the response writer never buffer the result; how incrementally an adapter yields is the
/// adapter's own contract (the shipped in-memory store snapshots before its first row).
/// </summary>
public static class ExportEndpoints
{
    public static void MapExportEndpoints(this WebApplication app)
    {
        app.MapGet("/orders/export", ExportAsync)
            .WithName("ExportOrders")
            .Produces<ExportRow>(StatusCodes.Status200OK, "application/x-ndjson");
    }

    private static IResult ExportAsync(IOrderStore store, HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Stream(
            async stream =>
            {
                var cancellationToken = context.RequestAborted;
                await foreach (var row in OrderExport.RowsAsync(store, cancellationToken))
                {
                    await JsonLines.WriteAsync(stream, row, cancellationToken);
                }
            },
            "application/x-ndjson");
    }
}
