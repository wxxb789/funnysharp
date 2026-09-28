using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace FunnySharp.VerticalSlice.Baseline;

/// <summary>One requested line as it arrives, before refinement.</summary>
public sealed record PlaceOrderLineRequest(string? Sku, int Quantity);

/// <summary>The placement body.</summary>
public sealed record PlaceOrderRequest(string? CustomerId, PlaceOrderLineRequest[]? Lines);

/// <summary>The payment body.</summary>
public sealed record PayOrderRequest(string? PaymentMethod);

/// <summary>The shipment body.</summary>
public sealed record ShipOrderRequest(string? TrackingCode);

/// <summary>The cancellation body.</summary>
public sealed record CancelOrderRequest(string? Reason);

/// <summary>One order line in a response.</summary>
public sealed record OrderLineResponse(string Sku, int Quantity, decimal UnitPrice);

/// <summary>The full order projection.</summary>
public sealed record OrderResponse(
    string OrderId,
    string CustomerId,
    string Status,
    int Revision,
    decimal Total,
    string Currency,
    string CreatedAt,
    string UpdatedAt,
    string? PaymentReference,
    string? TrackingCode,
    string? CancellationReason,
    OrderLineResponse[] Lines)
{
    public static OrderResponse From(Order order) => new(
        order.Id,
        order.CustomerId,
        order.Status.ToString(),
        order.Revision,
        order.Total,
        order.Currency,
        order.CreatedAt.ToString("O"),
        order.UpdatedAt.ToString("O"),
        order.PaymentReference,
        order.TrackingCode,
        order.CancellationReason,
        [.. order.Lines.Select(static line => new OrderLineResponse(line.Sku, line.Quantity, line.UnitPrice))]);
}

/// <summary>A compact receipt for an applied lifecycle event.</summary>
public sealed record OrderReceiptResponse(
    string OrderId,
    string Status,
    int Revision,
    decimal Total,
    string Currency,
    string? PaymentReference,
    string? TrackingCode)
{
    public static OrderReceiptResponse From(Order order) => new(
        order.Id,
        order.Status.ToString(),
        order.Revision,
        order.Total,
        order.Currency,
        order.PaymentReference,
        order.TrackingCode);
}

/// <summary>The payment receipt.</summary>
public sealed record PaymentReceiptResponse(string OrderId, string Status, string? PaymentReference, decimal Amount)
{
    public static PaymentReceiptResponse From(Order order) => new(
        order.Id,
        order.Status.ToString(),
        order.PaymentReference,
        order.Total);
}

/// <summary>One replayed timeline step.</summary>
public sealed record TimelineEntryResponse(string Event, string Status, string OccurredAt, string[] Commands);

/// <summary>The timeline with its replay-integrity flag.</summary>
public sealed record TimelineResponse(string Status, int Revision, bool MatchesStoredProjection, TimelineEntryResponse[] Entries);

/// <summary>The cheapest supplier answer.</summary>
public sealed record QuoteResponse(
    string Sku,
    int Quantity,
    string Supplier,
    decimal UnitPrice,
    string Currency,
    int LeadTimeDays,
    string[] UnavailableSuppliers);

/// <summary>One supplier answer delivered by the streaming endpoint.</summary>
public sealed record QuoteStreamRow(
    string Supplier,
    string Sku,
    string Outcome,
    decimal? UnitPrice,
    string? Currency,
    int? LeadTimeDays,
    string? Reason);

/// <summary>One exported row with its running totals.</summary>
public sealed record ExportRow(
    string OrderId,
    string Status,
    decimal Total,
    string Currency,
    int Sequence,
    decimal RevenueToDate);

/// <summary>One reconciliation line.</summary>
public sealed record ReconciliationLineResponse(string Sku, int Ordered, int Available);

/// <summary>The reconciliation report.</summary>
public sealed record ReconciliationResponse(
    string OrderId,
    string Status,
    int Revision,
    bool HistoryReplaysToStoredProjection,
    ReconciliationLineResponse[] Lines);

/// <summary>The same endpoints as the carrier slice, written in ordinary C#.</summary>
public static class BaselineEndpoints
{
    private const string Base = "https://funnysharp.example/problems/";

    private static readonly string[] PaymentMethods = ["card", "card-declined", "card-unavailable", "invoice"];

    public static void MapBaselineEndpoints(this WebApplication app)
    {
        var orders = app.MapGroup("/orders");

        orders.MapPost(string.Empty, PlaceOrderAsync)
            .WithName("PlaceOrder")
            .Produces<OrderReceiptResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        orders.MapGet("/{id}", FindOrderAsync)
            .WithName("FindOrder")
            .Produces<OrderResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        orders.MapPost("/{id}/payments", PayOrderAsync)
            .WithName("PayOrder")
            .Produces<PaymentReceiptResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status402PaymentRequired)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        orders.MapPost("/{id}/shipments", ShipOrderAsync)
            .WithName("ShipOrder")
            .Produces<OrderReceiptResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        orders.MapPost("/{id}/cancellation", CancelOrderAsync)
            .WithName("CancelOrder")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        orders.MapGet("/{id}/timeline", FindTimelineAsync)
            .WithName("FindOrderTimeline")
            .Produces<TimelineResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        orders.MapGet("/{id}/reconcile", ReconcileAsync)
            .WithName("ReconcileOrder")
            .Produces<ReconciliationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/orders/export", ExportAsync)
            .WithName("ExportOrders")
            .Produces<ExportRow>(StatusCodes.Status200OK, "application/x-ndjson");

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

    private static async Task<IResult> PlaceOrderAsync(
        PlaceOrderRequest request,
        InMemoryOrderStore store,
        BaselineOptions options,
        CancellationToken cancellationToken)
    {
        var errors = ValidatePlacement(request, options);
        if (errors.Count > 0)
        {
            return Results.Problem(ValidationProblem(errors));
        }

        var lines = await PriceLinesAsync(request.Lines!, options, cancellationToken);
        SimulatedDependencies.ReserveAll(lines);

        var now = DateTimeOffset.UtcNow;
        var draft = new Order
        {
            Id = $"ORD-{Interlocked.Increment(ref Sequence)}",
            CustomerId = request.CustomerId!,
            Lines = lines,
            Currency = options.Currency,
            Status = OrderStatus.Draft,
            Revision = 0,
            CreatedAt = now,
            UpdatedAt = now,
        };
        store.Create(draft);

        var placed = Lifecycle.Apply(draft, new OrderEvent("Place"));
        store.Persist(placed, "Place");
        return Results.Created($"/orders/{placed.Id}", OrderReceiptResponse.From(placed));
    }

    private static async Task<IResult> FindOrderAsync(string id, InMemoryOrderStore store, CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var order = store.Find(id) ?? throw new OrderNotFoundException(id);
        return Results.Ok(OrderResponse.From(order));
    }

    private static async Task<IResult> PayOrderAsync(
        string id,
        PayOrderRequest request,
        InMemoryOrderStore store,
        BaselineOptions options,
        CancellationToken cancellationToken)
    {
        if (request.PaymentMethod is not { } method || !PaymentMethods.Contains(method, StringComparer.Ordinal))
        {
            return Results.Problem(ValidationProblem(
                [("paymentMethod", $"A payment method must be one of: {string.Join(", ", PaymentMethods)}.")]));
        }

        await Task.CompletedTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var order = store.Find(id) ?? throw new OrderNotFoundException(id);
        var approved = method != "card-declined";
        if (method == "card-unavailable")
        {
            throw new DependencyUnavailableException("payments", "authorize");
        }

        var paid = Lifecycle.Apply(
            order,
            new OrderEvent(
                "Pay",
                Reference: approved ? $"PAY-{order.Id}-{order.Total:0.00}" : null,
                Amount: order.Total,
                Approved: approved,
                Reason: approved ? null : "the issuer declined the card"));
        store.Persist(paid, "Pay");
        return Results.Ok(PaymentReceiptResponse.From(paid));
    }

    private static async Task<IResult> ShipOrderAsync(
        string id,
        ShipOrderRequest request,
        InMemoryOrderStore store,
        CancellationToken cancellationToken)
    {
        if (request.TrackingCode is not { Length: >= 4 and <= 40 }
            || !request.TrackingCode.All(static character => char.IsAsciiLetterOrDigit(character) || character == '-'))
        {
            return Results.Problem(ValidationProblem(
                [("trackingCode", "A tracking code must be 4 to 40 letters, digits, or dashes.")]));
        }

        await Task.CompletedTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var order = store.Find(id) ?? throw new OrderNotFoundException(id);
        var shipped = Lifecycle.Apply(order, new OrderEvent("Ship", Reference: request.TrackingCode));
        store.Persist(shipped, "Ship");
        return Results.Ok(OrderReceiptResponse.From(shipped));
    }

    private static async Task<IResult> CancelOrderAsync(
        string id,
        CancelOrderRequest request,
        InMemoryOrderStore store,
        CancellationToken cancellationToken)
    {
        if (request.Reason is not { } reason || reason.Length < 5 || reason.Length > 200)
        {
            return Results.Problem(ValidationProblem(
                [("reason", "A cancellation reason must be 5 to 200 characters.")]));
        }

        await Task.CompletedTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var order = store.Find(id) ?? throw new OrderNotFoundException(id);
        var cancelled = Lifecycle.Apply(order, new OrderEvent("Cancel", Reason: reason));
        foreach (var line in cancelled.Lines)
        {
            SimulatedDependencies.Release(line.Sku, line.Quantity);
        }

        store.Persist(cancelled, "Cancel");
        return Results.NoContent();
    }

    private static async Task<IResult> FindTimelineAsync(
        string id,
        InMemoryOrderStore store,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var order = store.Find(id) ?? throw new OrderNotFoundException(id);
        var history = store.History(id);
        var (replayed, matches) = Lifecycle.Replay(order, history);
        var entries = new List<TimelineEntryResponse>(history.Count);
        var state = order with { Status = OrderStatus.Draft, Revision = 0 };
        foreach (var kind in history)
        {
            state = Lifecycle.Apply(state, new OrderEvent(kind));
            entries.Add(new TimelineEntryResponse(kind, state.Status.ToString(), state.UpdatedAt.ToString("O"), []));
        }

        if (state.Status != replayed.Status)
        {
            throw new HistoryDivergedException(id, history[^1]);
        }

        return Results.Ok(new TimelineResponse(replayed.Status.ToString(), replayed.Revision, matches, [.. entries]));
    }

    private static async Task<IResult> ReconcileAsync(
        string id,
        InMemoryOrderStore store,
        BaselineOptions options,
        CancellationToken cancellationToken)
    {
        var order = store.Find(id) ?? throw new OrderNotFoundException(id);
        var available = new int[order.Lines.Count];
        await Parallel.ForEachAsync(
            Enumerable.Range(0, order.Lines.Count),
            new ParallelOptions { MaxDegreeOfParallelism = options.QuoteConcurrency, CancellationToken = cancellationToken },
            (index, _) =>
            {
                available[index] = SimulatedDependencies.Available(order.Lines[index].Sku);
                return ValueTask.CompletedTask;
            });
        var (_, matches) = Lifecycle.Replay(order, store.History(id));
        var lines = order.Lines
            .Select((line, index) => new ReconciliationLineResponse(line.Sku, line.Quantity, available[index]))
            .ToArray();
        return Results.Ok(new ReconciliationResponse(order.Id, order.Status.ToString(), order.Revision, matches, lines));
    }

    private static async Task<IResult> QuoteAsync(
        string sku,
        int? quantity,
        BaselineOptions options,
        CancellationToken cancellationToken)
    {
        var errors = ValidateQuote(sku, quantity);
        if (errors.Count > 0)
        {
            return Results.Problem(ValidationProblem(errors));
        }

        var (best, unavailable) = await BestQuoteAsync(sku, options, cancellationToken);
        return Results.Ok(new QuoteResponse(
            sku,
            quantity ?? 1,
            best!.Value.Supplier,
            best.Value.UnitPrice,
            options.Currency,
            best.Value.LeadTimeDays,
            [.. unavailable]));
    }

    private static IResult StreamQuotesAsync(
        string sku,
        int? quantity,
        BaselineOptions options,
        HttpContext context)
    {
        var errors = ValidateQuote(sku, quantity);
        if (errors.Count > 0)
        {
            return Results.Problem(ValidationProblem(errors));
        }

        context.Response.Headers.CacheControl = "no-store";
        return Results.Stream(
            async stream =>
            {
                var cancellationToken = context.RequestAborted;
                var pending = new List<Task<QuoteStreamRow>>(options.SupplierCount);
                for (var rank = 0; rank < options.SupplierCount; rank++)
                {
                    pending.Add(QuoteAsRowAsync($"supplier-{rank + 1:00}", sku, options, cancellationToken));
                }

                while (pending.Count > 0)
                {
                    var finished = await Task.WhenAny(pending);
                    pending.Remove(finished);
                    await WriteAsync(stream, await finished, cancellationToken);
                }
            },
            "application/x-ndjson");
    }

    private static IResult ExportAsync(InMemoryOrderStore store, HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Stream(
            async stream =>
            {
                var cancellationToken = context.RequestAborted;
                var sequence = 0;
                var revenue = 0m;
                foreach (var order in store.Snapshot())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (order.Status == OrderStatus.Draft)
                    {
                        continue;
                    }

                    sequence++;
                    revenue += order.Total;
                    await WriteAsync(
                        stream,
                        new ExportRow(order.Id, order.Status.ToString(), order.Total, order.Currency, sequence, revenue),
                        cancellationToken);
                    await Task.CompletedTask.ConfigureAwait(false);
                }
            },
            "application/x-ndjson");
    }

    private static async Task<((string Supplier, decimal UnitPrice, int LeadTimeDays)? Best, List<string> Unavailable)>
        BestQuoteAsync(string sku, BaselineOptions options, CancellationToken cancellationToken)
    {
        var quotes = new (string Supplier, decimal UnitPrice, int LeadTimeDays)?[options.SupplierCount];
        var unavailable = new List<string>();
        await Parallel.ForEachAsync(
            Enumerable.Range(0, options.SupplierCount),
            new ParallelOptions { MaxDegreeOfParallelism = options.QuoteConcurrency, CancellationToken = cancellationToken },
            (rank, token) =>
            {
                token.ThrowIfCancellationRequested();
                var supplier = $"supplier-{rank + 1:00}";
                if (sku == "SKU-UNLISTED")
                {
                    lock (unavailable)
                    {
                        unavailable.Add(supplier);
                    }

                    return ValueTask.CompletedTask;
                }

                quotes[rank] = (supplier, SimulatedDependencies.PriceOf(sku, supplier), 1 + (rank % 5));
                return ValueTask.CompletedTask;
            });

        // Parallel.ForEachAsync completes work in whatever order it finishes, so the caller of this
        // method has to impose the ordering the response contract needs.
        unavailable.Sort(StringComparer.Ordinal);
        var answered = quotes.Where(static quote => quote is not null)
            .Select(static quote => quote!.Value)
            .OrderBy(static quote => quote.UnitPrice)
            .ThenBy(static quote => quote.Supplier, StringComparer.Ordinal)
            .ToList();
        if (answered.Count == 0)
        {
            throw new NoSupplierQuoteException(sku, unavailable);
        }

        return (answered[0], unavailable);
    }

    private static async Task<QuoteStreamRow> QuoteAsRowAsync(
        string supplier,
        string sku,
        BaselineOptions options,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (sku == "SKU-UNLISTED")
        {
            return new QuoteStreamRow(supplier, sku, "unavailable", null, null, null, "the SKU is not stocked");
        }

        var rank = int.Parse(supplier.AsSpan("supplier-".Length), System.Globalization.CultureInfo.InvariantCulture);
        return new QuoteStreamRow(
            supplier,
            sku,
            "quoted",
            SimulatedDependencies.PriceOf(sku, supplier),
            options.Currency,
            1 + ((rank - 1) % 5),
            null);
    }

    private static async Task WriteAsync<T>(Stream stream, T row, CancellationToken cancellationToken)
    {
        await JsonSerializer.SerializeAsync(stream, row, JsonSerializerOptions.Web, cancellationToken);
        await stream.WriteAsync("\n"u8.ToArray(), cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private static async Task<List<OrderLine>> PriceLinesAsync(
        IReadOnlyList<PlaceOrderLineRequest> lines,
        BaselineOptions options,
        CancellationToken cancellationToken)
    {
        var priced = new OrderLine[lines.Count];
        await Parallel.ForEachAsync(
            Enumerable.Range(0, lines.Count),
            new ParallelOptions { MaxDegreeOfParallelism = options.QuoteConcurrency, CancellationToken = cancellationToken },
            (index, token) =>
            {
                var line = lines[index];
                if (line.Sku == "SKU-UNLISTED")
                {
                    throw new NoSupplierQuoteException(line.Sku!, []);
                }

                var price = decimal.MaxValue;
                for (var rank = 0; rank < options.SupplierCount; rank++)
                {
                    price = Math.Min(price, SimulatedDependencies.PriceOf(line.Sku!, $"supplier-{rank + 1:00}"));
                }

                priced[index] = new OrderLine(line.Sku!, line.Quantity, price);
                return ValueTask.CompletedTask;
            });
        return [.. priced];
    }

    private static List<(string Field, string Message)> ValidatePlacement(PlaceOrderRequest request, BaselineOptions options)
    {
        var errors = new List<(string Field, string Message)>();
        if (request.CustomerId is not { Length: >= 3 and <= 40 } || request.CustomerId.Any(char.IsControl))
        {
            errors.Add(("customerId", "A customer id must be 3 to 40 printable characters."));
        }

        if (request.Lines is not { Length: > 0 })
        {
            errors.Add(("lines", "At least one order line is required."));
            return errors;
        }

        if (request.Lines.Length > options.MaximumOrderLines)
        {
            errors.Add(("lines", $"An order can contain at most {options.MaximumOrderLines} lines."));
            return errors;
        }

        for (var index = 0; index < request.Lines.Length; index++)
        {
            var line = request.Lines[index];
            var field = $"lines[{index}]";
            if (line.Sku is not { Length: >= 2 and <= 32 }
                || !line.Sku.All(static character => char.IsAsciiLetterUpper(character) || char.IsAsciiDigit(character) || character == '-'))
            {
                errors.Add(($"{field}.sku", "A SKU must be 2 to 32 upper-case letters, digits, or dashes."));
            }

            if (line.Quantity < 1)
            {
                errors.Add(($"{field}.quantity", "A quantity must be at least 1."));
            }
            else if (line.Quantity > 20)
            {
                errors.Add(($"{field}.quantity", "A quantity cannot exceed 20."));
            }
        }

        return errors;
    }

    private static List<(string Field, string Message)> ValidateQuote(string? sku, int? quantity)
    {
        var errors = new List<(string Field, string Message)>();
        if (sku is not { Length: >= 2 and <= 32 }
            || !sku.All(static character => char.IsAsciiLetterUpper(character) || char.IsAsciiDigit(character) || character == '-'))
        {
            errors.Add(("sku", "A SKU must be 2 to 32 upper-case letters, digits, or dashes."));
        }

        var requested = quantity ?? 1;
        if (requested < 1)
        {
            errors.Add(("quantity", "A quantity must be at least 1."));
        }
        else if (requested > 20)
        {
            errors.Add(("quantity", "A quantity cannot exceed 20."));
        }

        return errors;
    }

    private static HttpValidationProblemDetails ValidationProblem(List<(string Field, string Message)> errors)
    {
        var fields = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var order = new List<string>(errors.Count);
        foreach (var error in errors)
        {
            if (!fields.TryGetValue(error.Field, out var messages))
            {
                messages = [];
                fields.Add(error.Field, messages);
                order.Add(error.Field);
            }

            messages.Add(error.Message);
        }

        return new HttpValidationProblemDetails(
            order.ToDictionary(static field => field, field => fields[field].ToArray(), StringComparer.Ordinal))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "The request could not be validated",
            Type = Base + "request-invalid",
        };
    }

    private static int Sequence;
}
