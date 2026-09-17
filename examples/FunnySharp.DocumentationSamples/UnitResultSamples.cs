using FunnySharp;
using FunnySharp.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FunnySharp.DocumentationSamples;

internal static class UnitResultSamples
{
    private static UnitResult<OrderError> DeleteOrNotify(OrderId id, IOrderStore store)
    {
        // <snippet DocumentationSamples.UnitResult.DeleteOrNotify>
        return UnitResult<OrderError>.Success()
            .Ensure(() => store.Exists(id), new OrderError("not-found", "The order was not found."))
            .Bind(() => store.Delete(id))
            .RecoverWith(error => NotifyAndAcknowledge(id, error));
        // </snippet>
    }

    private static UnitResult<ReservationError> ReserveIfAvailable(
        Sku sku,
        int requested,
        IInventory inventory)
    {
        // <snippet DocumentationSamples.UnitResult.ConditionalPresence>
        return Option
            .FromBoolean(inventory.HasStock(sku, requested), () => new Reservation(sku, requested))
            .ToUnitResult(() => new ReservationError(sku, "The requested quantity is not available."));
        // </snippet>
    }

    private static async Task<UnitResult<CheckoutError>> PlaceOrderAsync(
        Cart cart,
        ICheckoutService service)
    {
        // <snippet DocumentationSamples.UnitResult.AsyncCommandChain>
        UnitResult<CheckoutError> reservation = await UnitResult
            .TryAsync(() => service.ReserveAsync(cart), MapCheckoutFailure);

        UnitResult<CheckoutError> confirmed = await reservation
            .BindAsync(() => service.ConfirmAsync(cart));

        return await confirmed.BindAsync(() => service.SendReceiptAsync(cart));
        // </snippet>
    }

    private static UnitResult<OrderError> SubmitValidated(OrderDraft draft, IOrderService service)
    {
        // <snippet DocumentationSamples.UnitResult.ValidationUnaffected>
        Validation<OrderDraft, OrderError> validation = draft.Validate();
        if (!validation.TryGetValue(out var order))
        {
            _ = validation.TryGetErrors(out var errors);
            return UnitResult<OrderError>.Failure(
                new OrderError("validation", $"{errors!.Count} validation errors were found."));
        }

        return service.Submit(order!);
        // </snippet>
    }

    private static void MapUnitResultOutcome(WebApplication app)
    {
        // <snippet DocumentationSamples.AspNetCore.UnitResultOutcome>
        app.MapDelete("/carts/{id:int}", (int id, CancellationToken cancellationToken) =>
            DeleteCartAsync(id, cancellationToken).ToHttpResultAsync(OrderConflict));

        app.MapPost("/orders/{id:int}/confirm", (int id, CancellationToken cancellationToken) =>
            ConfirmOrderAsync(id, cancellationToken).ToHttpResultAsync(
                OrderConflict,
                () => Results.Accepted()));
        // </snippet>
    }

    private static UnitResult<OrderError> NotifyAndAcknowledge(OrderId id, OrderError error) =>
        UnitResult<OrderError>.Success();

    private static CheckoutError MapCheckoutFailure(Exception exception) =>
        new("checkout-failed", exception.Message);

    private static ProblemDetails OrderConflict(OrderError error) => new()
    {
        Status = StatusCodes.Status409Conflict,
        Title = "Order conflict",
        Detail = error.Code,
    };

    private static Task<UnitResult<OrderError>> DeleteCartAsync(
        int id,
        CancellationToken cancellationToken) =>
        Task.FromResult(UnitResult<OrderError>.Success());

    private static Task<UnitResult<OrderError>> ConfirmOrderAsync(
        int id,
        CancellationToken cancellationToken) =>
        Task.FromResult(UnitResult<OrderError>.Success());

    private interface IOrderStore
    {
        bool Exists(OrderId id);

        UnitResult<OrderError> Delete(OrderId id);
    }

    private interface IInventory
    {
        bool HasStock(Sku sku, int quantity);
    }

    private interface ICheckoutService
    {
        Task ReserveAsync(Cart cart);

        Task<UnitResult<CheckoutError>> ConfirmAsync(Cart cart);

        Task<UnitResult<CheckoutError>> SendReceiptAsync(Cart cart);
    }

    private interface IOrderService
    {
        UnitResult<OrderError> Submit(OrderDraft order);
    }

    private sealed record OrderId(int Value);

    private sealed record OrderError(string Code, string Message);

    private sealed record Sku(string Value);

    private sealed record Reservation(Sku Sku, int Quantity);

    private sealed record ReservationError(Sku Sku, string Message);

    private sealed record Cart(string Id);

    private sealed record CheckoutError(string Code, string Message);

    private sealed record OrderDraft(string Id)
    {
        public Validation<OrderDraft, OrderError> Validate() =>
            Validation<OrderDraft, OrderError>.Valid(this);
    }
}
