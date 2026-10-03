using FunnySharp.AspNetCore;
using FunnySharp.VerticalSlice.Application;
using FunnySharp.VerticalSlice.Domain;
using FunnySharp.VerticalSlice.Http;

namespace FunnySharp.VerticalSlice.Endpoints;

/// <summary>
/// The order endpoints. Each one validates at the boundary, delegates domain work, and states its
/// possible outcomes as typed-result metadata so the OpenAPI document matches the real behavior.
/// </summary>
public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this WebApplication app)
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

        orders.MapGet("/{id}/reconcile", ReconcileOrderAsync)
            .WithName("ReconcileOrder")
            .Produces<ReconciliationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> PlaceOrderAsync(
        PlaceOrderRequest request,
        OrderService service,
        ProblemMappings problems,
        VerticalSliceOptions options,
        CancellationToken cancellationToken)
    {
        var validated = OrderRequestValidation.ValidatePlacement(request, options);
        if (!validated.TryGetValue(out var command))
        {
            return validated.ToHttpResult(problems.FromValidation);
        }

        var placed = await service.PlaceAsync(command, cancellationToken);
        return placed.ToHttpResult(
            problems.FromOrderError,
            order => Results.Created($"/orders/{order.Id.Value}", OrderReceiptResponse.From(order)));
    }

    private static async Task<IResult> FindOrderAsync(
        string id,
        OrderService service,
        ProblemMappings problems,
        CancellationToken cancellationToken)
    {
        if (!OrderId.Create(id).TryGetValue(out var orderId))
        {
            return Results.Problem(problems.OrderNotFound(id));
        }

        var found = await service.FindAsync(orderId, cancellationToken);
        return found.ToHttpResult(
            () => problems.OrderNotFound(orderId),
            record => Results.Ok(OrderResponse.From(record.Order)));
    }

    private static async Task<IResult> PayOrderAsync(
        string id,
        PayOrderRequest request,
        OrderService service,
        ProblemMappings problems,
        CancellationToken cancellationToken)
    {
        if (!OrderId.Create(id).TryGetValue(out var orderId))
        {
            return Results.Problem(problems.OrderNotFound(id));
        }

        var validated = OrderRequestValidation.ValidatePayment(orderId, request);
        if (!validated.TryGetValue(out var command))
        {
            return validated.ToHttpResult(problems.FromValidation);
        }

        var paid = await service.PayAsync(command, cancellationToken);
        return paid.ToHttpResult(
            problems.FromOrderError,
            order => Results.Ok(PaymentReceiptResponse.From(order)));
    }

    private static async Task<IResult> ShipOrderAsync(
        string id,
        ShipOrderRequest request,
        OrderService service,
        ProblemMappings problems,
        CancellationToken cancellationToken)
    {
        if (!OrderId.Create(id).TryGetValue(out var orderId))
        {
            return Results.Problem(problems.OrderNotFound(id));
        }

        var validated = OrderRequestValidation.ValidateShipment(orderId, request);
        if (!validated.TryGetValue(out var command))
        {
            return validated.ToHttpResult(problems.FromValidation);
        }

        var shipped = await service.ShipAsync(command, cancellationToken);
        return shipped.ToHttpResult(
            problems.FromOrderError,
            order => Results.Ok(OrderReceiptResponse.From(order)));
    }

    private static async Task<IResult> CancelOrderAsync(
        string id,
        CancelOrderRequest request,
        OrderService service,
        ProblemMappings problems,
        CancellationToken cancellationToken)
    {
        if (!OrderId.Create(id).TryGetValue(out var orderId))
        {
            return Results.Problem(problems.OrderNotFound(id));
        }

        var validated = OrderRequestValidation.ValidateCancellation(orderId, request);
        if (!validated.TryGetValue(out var command))
        {
            return validated.ToHttpResult(problems.FromValidation);
        }

        var cancelled = await service.CancelAsync(command, cancellationToken);
        return cancelled.ToHttpResult(problems.FromOrderError);
    }

    private static async Task<IResult> FindTimelineAsync(
        string id,
        OrderService service,
        ProblemMappings problems,
        CancellationToken cancellationToken)
    {
        if (!OrderId.Create(id).TryGetValue(out var orderId))
        {
            return Results.Problem(problems.OrderNotFound(id));
        }

        var timeline = await service.TimelineAsync(orderId, cancellationToken);
        return timeline.ToHttpResult(problems.FromOrderError, ToTimelineResponse);
    }

    private static ValueTask<IResult> ReconcileOrderAsync(
        string id,
        HttpContext context,
        OrderService service,
        ProblemMappings problems)
    {
        if (!OrderId.Create(id).TryGetValue(out var orderId))
        {
            return ValueTask.FromResult<IResult>(Results.Problem(problems.OrderNotFound(id)));
        }

        return service.Reconciliation(orderId).ToHttpResultAsync(
            service.Environment,
            context,
            problems.FromOrderError,
            report => Results.Ok(ReconciliationResponse.From(report)));
    }

    private static IResult ToTimelineResponse(TimelineProjection projection)
    {
        var entries = projection.Rows
            .Choose(static row => row.Commands.Any(IsCustomerVisible)
                ? Option.Some(new TimelineEntryResponse(
                    row.Event.ToString(),
                    row.Status.ToString(),
                    row.OccurredAt.ToString("O"),
                    [.. row.Commands.Select(Describe)]))
                : Option.None<TimelineEntryResponse>())
            .ToArray();

        return Results.Ok(new TimelineResponse(
            projection.Recomputed.Status.ToString(),
            projection.Recomputed.Revision,
            projection.MatchesStored,
            entries));
    }

    private static bool IsCustomerVisible(OrderCommand command) =>
        command is OrderCommand.NotifyCustomer or OrderCommand.PublishShipment;

    private static string Describe(OrderCommand command) => command switch
    {
        OrderCommand.ReserveInventory => "reserve-inventory",
        OrderCommand.ReleaseInventory => "release-inventory",
        OrderCommand.Persist => "persist",
        OrderCommand.NotifyCustomer notify => $"notify-customer:{notify.Kind}",
        OrderCommand.PublishShipment => "publish-shipment",
        _ => "unknown",
    };
}
