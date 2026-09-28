using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FunnySharp.VerticalSlice.Baseline;

/// <summary>An order id that does not exist.</summary>
public sealed class OrderNotFoundException(string orderId) : Exception($"Order '{orderId}' was not found.")
{
    public string OrderId { get; } = orderId;
}

/// <summary>An event the current state does not accept.</summary>
public sealed class TransitionRejectedException(string code, OrderStatus status, string @event)
    : Exception($"A {@event} event was rejected for an order in state {status}.")
{
    public string Code { get; } = code;

    public OrderStatus Status { get; } = status;

    public string Event { get; } = @event;
}

/// <summary>The payment gateway declined the authorization.</summary>
public sealed class PaymentDeclinedException(string reason, decimal amount, string currency) : Exception(reason)
{
    public decimal Amount { get; } = amount;

    public string Currency { get; } = currency;
}

/// <summary>Stock cannot cover the requested quantity.</summary>
public sealed class InventoryShortfallException(string sku, int requested, int available)
    : Exception($"Only {available} unit(s) of '{sku}' are available.")
{
    public string Sku { get; } = sku;

    public int Requested { get; } = requested;

    public int Available { get; } = available;
}

/// <summary>A lost update: the order changed since this request read it.</summary>
public sealed class ConcurrencyConflictException(string orderId, int expectedRevision, int actualRevision)
    : Exception("The request was computed from a stale revision and was not applied.")
{
    public string OrderId { get; } = orderId;

    public int ExpectedRevision { get; } = expectedRevision;

    public int ActualRevision { get; } = actualRevision;
}

/// <summary>No supplier could quote the SKU.</summary>
public sealed class NoSupplierQuoteException(string sku, IReadOnlyList<string> suppliers)
    : Exception($"Every configured supplier failed to quote '{sku}'.")
{
    public string Sku { get; } = sku;

    public IReadOnlyList<string> Suppliers { get; } = suppliers;
}

/// <summary>A dependency did not answer.</summary>
public sealed class DependencyUnavailableException(string dependency, string operation)
    : Exception($"The '{dependency}' dependency failed while performing '{operation}'.")
{
    public string Dependency { get; } = dependency;

    public string Operation { get; } = operation;
}

/// <summary>A dispatch miss: no branch owns this state and event.</summary>
public sealed class UndefinedTransitionException(OrderStatus status, string @event)
    : Exception($"No handler owns a {@event} event for an order in state {status}.")
{
    public OrderStatus Status { get; } = status;

    public string Event { get; } = @event;
}

/// <summary>Stored history no longer replays to the stored projection.</summary>
public sealed class HistoryDivergedException(string orderId, string @event)
    : Exception($"Replaying the {@event} event diverged from the stored projection of '{orderId}'.")
{
    public string OrderId { get; } = orderId;

    public string Event { get; } = @event;
}

/// <summary>
/// The single exception-to-HTTP boundary. Unlike the carrier version, every failure arrives here as
/// a thrown exception, so this switch is the only place that knows a failure exists.
/// </summary>
public sealed class BaselineExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<BaselineExceptionHandler> logger) : IExceptionHandler
{
    private const string Base = "https://funnysharp.example/problems/";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (problem, status) = Translate(exception);
        logger.LogError(
            exception,
            "Unhandled exception while serving {Method} {Path}; mapped to {Status}.",
            httpContext.Request.Method,
            httpContext.Request.Path,
            status);
        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
        });
    }

    private static (ProblemDetails Problem, int Status) Translate(Exception exception) => exception switch
    {
        OrderNotFoundException missing => (new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "The order was not found",
            Type = Base + "order-not-found",
            Detail = $"Order '{missing.OrderId}' does not exist.",
        }, StatusCodes.Status404NotFound),
        TransitionRejectedException rejected => (new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "The order does not accept this event",
            Type = Base + "order-transition-rejected",
            Detail = rejected.Message,
            Extensions =
            {
                ["code"] = rejected.Code,
                ["orderStatus"] = rejected.Status.ToString(),
                ["event"] = rejected.Event,
            },
        }, StatusCodes.Status409Conflict),
        PaymentDeclinedException declined => (new ProblemDetails
        {
            Status = StatusCodes.Status402PaymentRequired,
            Title = "The payment was declined",
            Type = Base + "payment-declined",
            Detail = declined.Message,
            Extensions = { ["amount"] = declined.Amount, ["currency"] = declined.Currency },
        }, StatusCodes.Status402PaymentRequired),
        InventoryShortfallException shortfall => (new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "The order cannot be sourced",
            Type = Base + "inventory-shortfall",
            Detail = shortfall.Message,
            Extensions = { ["sku"] = shortfall.Sku, ["requested"] = shortfall.Requested, ["available"] = shortfall.Available },
        }, StatusCodes.Status409Conflict),
        ConcurrencyConflictException conflict => (new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "The order changed while this request was in flight",
            Type = Base + "concurrency-conflict",
            Detail = conflict.Message,
            Extensions = { ["expectedRevision"] = conflict.ExpectedRevision, ["actualRevision"] = conflict.ActualRevision },
        }, StatusCodes.Status409Conflict),
        NoSupplierQuoteException none => (new ProblemDetails
        {
            Status = StatusCodes.Status503ServiceUnavailable,
            Title = "No supplier could quote this SKU",
            Type = Base + "no-supplier-quote",
            Detail = none.Message,
            Extensions = { ["sku"] = none.Sku, ["suppliers"] = none.Suppliers.ToArray() },
        }, StatusCodes.Status503ServiceUnavailable),
        DependencyUnavailableException unavailable => (new ProblemDetails
        {
            Status = StatusCodes.Status503ServiceUnavailable,
            Title = "A dependency did not answer",
            Type = Base + "dependency-unavailable",
            Detail = unavailable.Message,
            Extensions = { ["dependency"] = unavailable.Dependency, ["operation"] = unavailable.Operation },
        }, StatusCodes.Status503ServiceUnavailable),
        UndefinedTransitionException undefined => (new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "The lifecycle has no handler for this event",
            Type = Base + "undefined-transition",
            Detail = undefined.Message,
        }, StatusCodes.Status500InternalServerError),
        HistoryDivergedException diverged => (new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Stored order history no longer replays",
            Type = Base + "history-diverged",
            Detail = diverged.Message,
        }, StatusCodes.Status500InternalServerError),
        _ => (new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred",
            Type = Base + "unexpected-error",
            Detail = "The request failed before it could produce a domain outcome; the failure was logged.",
        }, StatusCodes.Status500InternalServerError),
    };
}
