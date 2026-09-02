using FunnySharp;
using FunnySharp.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var catalog = new ProductCatalog();

app.MapGet("/products/{id:int}", (int id) =>
    FindProduct(id).ToHttpResult(
        () => NotFound($"Product {id} was not found.")));

app.MapGet("/orders/{id}", (string id) =>
    FindOrder(id).ToHttpResult(MapOrderFailure));

app.MapPost("/orders", (CreateOrderRequest request) =>
    ValidateOrder(request).ToHttpResult(MapValidationErrors, order => Results.Created($"/orders/{order.Id}", order)));

app.MapGet("/inventory/{sku}", (string sku, CancellationToken cancellationToken) =>
    GetInventoryAsync(sku, cancellationToken).ToHttpResultAsync(MapInventoryFailure));

app.MapGet("/deliveries/{sku}", (string sku, CancellationToken cancellationToken) =>
    GetDeliveryAsync(sku, cancellationToken)
        .ToHttpResultAsync(MapDeliveryFailure, delivery => new AcceptedDeliveryResult(delivery)));

app.MapGet("/catalog/{id:int}/refresh", (HttpContext context, int id) =>
    LoadProductEffect(id).ToHttpResultAsync(
        catalog,
        context,
        () => NotFound($"Product {id} was not found.")));

if (args is ["--verify"])
{
    Console.WriteLine("FunnySharp ASP.NET Core example endpoints mapped.");
    return;
}

await app.RunAsync();

static Option<Product> FindProduct(int id) =>
    id == 42
        ? Option.Some(new Product(42, "Functional C#"))
        : Option.None<Product>();

static Result<Order, OrderFailure> FindOrder(string id) =>
    id == "ORD-42"
        ? Result<Order, OrderFailure>.Success(new Order(id, "book", 2))
        : Result<Order, OrderFailure>.Failure(new OrderFailure("order-not-found", id));

static Validation<Order, InputError> ValidateOrder(CreateOrderRequest request)
{
    var errors = new List<InputError>();
    if (string.IsNullOrWhiteSpace(request.Sku))
    {
        errors.Add(new InputError("sku", "required", "A SKU is required."));
    }

    if (request.Quantity <= 0)
    {
        errors.Add(new InputError("quantity", "positive", "Quantity must be positive."));
    }

    if (request.Quantity > 20)
    {
        errors.Add(new InputError("quantity", "maximum", "Quantity cannot exceed 20."));
    }

    return errors.Count == 0
        ? Validation<Order, InputError>.Valid(new Order("ORD-43", request.Sku, request.Quantity))
        : Validation<Order, InputError>.InvalidMany(errors);
}

static Task<Result<Inventory, InventoryFailure>> GetInventoryAsync(
    string sku,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult(
        sku == "book"
            ? Result<Inventory, InventoryFailure>.Success(new Inventory(sku, 8))
            : Result<Inventory, InventoryFailure>.Failure(new InventoryFailure("unknown-sku", sku)));
}

static ValueTask<Result<Delivery, DeliveryFailure>> GetDeliveryAsync(
    string sku,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    return ValueTask.FromResult(
        sku == "book"
            ? Result<Delivery, DeliveryFailure>.Success(new Delivery(sku, "scheduled"))
            : Result<Delivery, DeliveryFailure>.Failure(new DeliveryFailure("not-deliverable", sku)));
}

static Effect<ProductCatalog, Option<Product>> LoadProductEffect(int id) =>
    Effect.FromValueTask<ProductCatalog, Option<Product>>(
        (catalog, cancellationToken) => catalog.LoadAsync(id, cancellationToken));

static ProblemDetails NotFound(string detail) =>
    new()
    {
        Status = StatusCodes.Status404NotFound,
        Title = "Not found",
        Type = "https://example.invalid/problems/product-not-found",
        Detail = detail,
    };

static ProblemDetails MapOrderFailure(OrderFailure failure) =>
    new()
    {
        Status = StatusCodes.Status404NotFound,
        Title = "Order was not found",
        Type = "https://example.invalid/problems/order-not-found",
        Detail = $"Order '{failure.OrderId}' was not found.",
    };

static ProblemDetails MapInventoryFailure(InventoryFailure failure) =>
    new()
    {
        Status = StatusCodes.Status404NotFound,
        Title = "Inventory was not found",
        Type = "https://example.invalid/problems/unknown-sku",
        Detail = $"SKU '{failure.Sku}' is not stocked.",
    };

static ProblemDetails MapDeliveryFailure(DeliveryFailure failure) =>
    new()
    {
        Status = StatusCodes.Status409Conflict,
        Title = "Delivery cannot be scheduled",
        Type = "https://example.invalid/problems/not-deliverable",
        Detail = $"SKU '{failure.Sku}' cannot be delivered.",
    };

static HttpValidationProblemDetails MapValidationErrors(IReadOnlyList<InputError> errors)
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
        Title = "Order validation failed",
        Type = "https://example.invalid/problems/invalid-order",
    };
}

internal sealed class ProductCatalog
{
    internal ValueTask<Option<Product>> LoadAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(
            id == 42
                ? Option.Some(new Product(42, "Functional C#"))
                : Option.None<Product>());
    }
}

internal sealed class AcceptedDeliveryResult(Delivery delivery) : IResult
{
    public Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = StatusCodes.Status202Accepted;
        return httpContext.Response.WriteAsJsonAsync(delivery, cancellationToken: httpContext.RequestAborted);
    }
}

internal sealed record Product(int Id, string Name);

internal sealed record Order(string Id, string Sku, int Quantity);

internal sealed record CreateOrderRequest(string Sku, int Quantity);

internal sealed record InputError(string Field, string Code, string Message);

internal sealed record OrderFailure(string Code, string OrderId);

internal sealed record Inventory(string Sku, int Available);

internal sealed record InventoryFailure(string Code, string Sku);

internal sealed record Delivery(string Sku, string Status);

internal sealed record DeliveryFailure(string Code, string Sku);
