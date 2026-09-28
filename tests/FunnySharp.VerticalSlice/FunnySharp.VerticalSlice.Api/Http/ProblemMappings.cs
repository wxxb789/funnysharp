using System.Diagnostics;
using FunnySharp.VerticalSlice.Domain;
using Microsoft.AspNetCore.Mvc;

namespace FunnySharp.VerticalSlice.Http;

/// <summary>
/// The single place where a domain failure becomes HTTP. Each case states its own status and problem
/// type; unexpected exceptions never travel through here. Cases that return a 5xx log, because a
/// server-side fault must be visible in the log rather than only in a response body.
/// </summary>
public sealed class ProblemMappings(ILogger<ProblemMappings> logger)
{
    private const string Base = "https://funnysharp.example/problems/";

    public ProblemDetails OrderNotFound(OrderId orderId) => new()
    {
        Status = StatusCodes.Status404NotFound,
        Title = "The order was not found",
        Type = Base + "order-not-found",
        Detail = $"Order '{orderId.Value}' does not exist.",
    };

    public ProblemDetails OrderNotFound(string rawOrderId) => new()
    {
        Status = StatusCodes.Status404NotFound,
        Title = "The order was not found",
        Type = Base + "order-not-found",
        Detail = $"'{rawOrderId}' is not an order id.",
    };

    public ProblemDetails FromOrderError(OrderError error) => error switch
    {
        OrderError.OrderNotFound missing => OrderNotFound(missing.OrderId),
        OrderError.TransitionRejected rejected => new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "The order does not accept this event",
            Type = Base + "order-transition-rejected",
            Detail = $"A {rejected.Event} event was rejected for an order in state {rejected.Status}.",
            Extensions =
            {
                ["code"] = rejected.Code,
                ["orderStatus"] = rejected.Status.ToString(),
                ["event"] = rejected.Event.ToString(),
            },
        },
        OrderError.PaymentDeclined declined => new ProblemDetails
        {
            Status = StatusCodes.Status402PaymentRequired,
            Title = "The payment was declined",
            Type = Base + "payment-declined",
            Detail = declined.Reason,
            Extensions =
            {
                ["amount"] = declined.Amount.Amount,
                ["currency"] = declined.Amount.Currency,
            },
        },
        OrderError.InventoryShortfall shortfall => new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "The order cannot be sourced",
            Type = Base + "inventory-shortfall",
            Detail = $"Only {shortfall.Available} unit(s) of '{shortfall.Sku.Value}' are available.",
            Extensions =
            {
                ["sku"] = shortfall.Sku.Value,
                ["requested"] = shortfall.Requested.Value,
                ["available"] = shortfall.Available,
            },
        },
        OrderError.ConcurrencyConflict conflict => new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "The order changed while this request was in flight",
            Type = Base + "concurrency-conflict",
            Detail = "The request was computed from a stale revision and was not applied.",
            Extensions =
            {
                ["expectedRevision"] = conflict.ExpectedRevision,
                ["actualRevision"] = conflict.ActualRevision,
            },
        },
        OrderError.SupplierUnavailable unavailable => new ProblemDetails
        {
            Status = StatusCodes.Status503ServiceUnavailable,
            Title = "A supplier could not answer",
            Type = Base + "supplier-unavailable",
            Detail = $"Supplier '{unavailable.Supplier}' did not quote '{unavailable.Sku.Value}': {unavailable.Reason}.",
            Extensions =
            {
                ["supplier"] = unavailable.Supplier,
                ["sku"] = unavailable.Sku.Value,
            },
        },
        OrderError.NoSupplierQuote none => new ProblemDetails
        {
            Status = StatusCodes.Status503ServiceUnavailable,
            Title = "No supplier could quote this SKU",
            Type = Base + "no-supplier-quote",
            Detail = $"Every configured supplier failed to quote '{none.Sku.Value}'.",
            Extensions =
            {
                ["sku"] = none.Sku.Value,
                ["suppliers"] = none.Suppliers.ToArray(),
            },
        },
        OrderError.DependencyUnavailable unavailable => DependencyFault(unavailable, error),
        OrderError.UndefinedTransition undefined => ServerFault(
            StatusCodes.Status500InternalServerError,
            "undefined-transition",
            "The lifecycle has no handler for this event",
            $"No handler owns a {undefined.Event} event for an order in state {undefined.Status}.",
            error),
        OrderError.HistoryDiverged diverged => ServerFault(
            StatusCodes.Status500InternalServerError,
            "history-diverged",
            "Stored order history no longer replays",
            $"Replaying the {diverged.Event} event diverged from the stored projection.",
            error),
        _ => throw new UnreachableException($"No problem mapping exists for {error.GetType().Name}."),
    };

    /// <summary>
    /// Groups validation errors by field, adding a field on first encounter and appending messages in
    /// the order the validator produced them.
    /// </summary>
    public HttpValidationProblemDetails FromValidation(IReadOnlyList<InputError> errors)
    {
        var fields = errors
            .GroupBy(static error => error.Field, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.Select(static error => error.Message).ToArray(),
                StringComparer.Ordinal);

        return new HttpValidationProblemDetails(fields)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "The request could not be validated",
            Type = Base + "request-invalid",
        };
    }

    private ProblemDetails DependencyFault(OrderError.DependencyUnavailable unavailable, OrderError error)
    {
        logger.LogError(
            "Order failure {Failure} produced a server fault {ProblemType} for {Dependency}.",
            error.GetType().Name,
            "dependency-unavailable",
            unavailable.Dependency);
        return new ProblemDetails
        {
            Status = StatusCodes.Status503ServiceUnavailable,
            Title = "A dependency did not answer",
            Type = Base + "dependency-unavailable",
            Detail = $"The '{unavailable.Dependency}' dependency failed while performing '{unavailable.Operation}'.",
            Extensions =
            {
                ["failure"] = error.GetType().Name,
                ["dependency"] = unavailable.Dependency,
                ["operation"] = unavailable.Operation,
            },
        };
    }

    private ProblemDetails ServerFault(int status, string problemType, string title, string detail, OrderError error)
    {
        logger.LogError("Order failure {Failure} produced a server fault {ProblemType}.", error.GetType().Name, problemType);
        return new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = Base + problemType,
            Detail = detail,
            Extensions = { ["failure"] = error.GetType().Name },
        };
    }
}
