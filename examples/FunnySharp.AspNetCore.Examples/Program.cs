using System.Text.Json;
using FunnySharp;
using FunnySharp.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var catalog = new ProductCatalog();
var schedules = new ScheduleStore();

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

app.MapDelete("/orders/{id}", (string id) =>
    CancelOrder(id).ToHttpResult(MapOrderFailure));

app.MapGet("/catalog/{id:int}/refresh", (HttpContext context, int id) =>
    LoadProductEffect(id).ToHttpResultAsync(
        catalog,
        context,
        () => NotFound($"Product {id} was not found.")));

app.MapMethods("/schedules/{id}", ["PATCH"], (string id, JsonElement body) =>
{
    if (!schedules.Find(id).TryGetValue(out var schedule))
    {
        return Results.NotFound();
    }

    var decoded = SchedulePatch.Schema.Decode(body);
    if (!decoded.TryGetValue(out var patch))
    {
        _ = decoded.TryGetErrors(out var decodeErrors);
        return ToValidationProblem("Schedule change is invalid.", decodeErrors!);
    }

    var validated = SchedulePatch.Schema.Validate(patch, schedule);
    if (!validated.TryGetValue(out var application))
    {
        _ = validated.TryGetErrors(out var validationErrors);
        return ToValidationProblem("Schedule candidate is invalid.", validationErrors!);
    }

    schedules.Save(id, application.Candidate);
    return Results.Ok(application.Candidate);
});

if (args is ["--verify"])
{
    _ = SchedulePatch.Schema.Explain();
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

static UnitResult<OrderFailure> CancelOrder(string id) =>
    id == "ORD-42"
        ? UnitResult<OrderFailure>.Success()
        : UnitResult<OrderFailure>.Failure(new OrderFailure("order-not-found", id));

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

static IResult ToValidationProblem(string title, IReadOnlyList<DecodeError> errors)
{
    var grouped = GroupMessages(
        errors,
        static error => error.Path.Length == 0 ? "request" : error.Path,
        static error => error.ToString());
    return Results.ValidationProblem(
        grouped,
        statusCode: StatusCodes.Status400BadRequest,
        title: title);
}

static HttpValidationProblemDetails MapValidationErrors(IReadOnlyList<InputError> errors) =>
    new(GroupMessages(
        errors,
        static error => error.Field,
        static error => error.Message))
    {
        Status = StatusCodes.Status400BadRequest,
        Title = "Order validation failed",
        Type = "https://example.invalid/problems/invalid-order",
    };

static Dictionary<string, string[]> GroupMessages<T>(
    IEnumerable<T> items,
    Func<T, string> field,
    Func<T, string> message)
{
    var grouped = new Dictionary<string, List<string>>(StringComparer.Ordinal);
    foreach (var item in items)
    {
        var key = field(item);
        if (!grouped.TryGetValue(key, out var messages))
        {
            messages = [];
            grouped.Add(key, messages);
        }

        messages.Add(message(item));
    }

    return grouped.ToDictionary(
        static pair => pair.Key,
        static pair => pair.Value.ToArray(),
        StringComparer.Ordinal);
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

internal sealed class ScheduleStore
{
    private readonly Dictionary<string, DeliverySchedule> schedules =
        new(StringComparer.Ordinal)
        {
            ["daily"] = new DeliverySchedule(
                "Europe/Paris",
                9 * 60,
                17 * 60,
                Option.Some("front desk")),
        };

    internal Option<DeliverySchedule> Find(string id) =>
        schedules.TryGetValue(id, out var schedule)
            ? Option.Some(schedule)
            : Option.None<DeliverySchedule>();

    internal void Save(string id, DeliverySchedule schedule) => schedules[id] = schedule;
}

internal sealed record DeliverySchedule(
    string TimeZone,
    int StartsAt,
    int EndsAt,
    Option<string> Note);

internal static class SchedulePatch
{
    private static readonly Contract<DeliverySchedule> ScheduleContract = BuildContract();

    private static readonly Lens<DeliverySchedule, int> StartsAtLens =
        Lens.Create<DeliverySchedule, int>(
            static value => value.StartsAt,
            static (value, next) => value with { StartsAt = next });

    private static readonly Lens<DeliverySchedule, int> EndsAtLens =
        Lens.Create<DeliverySchedule, int>(
            static value => value.EndsAt,
            static (value, next) => value with { EndsAt = next });

    private static readonly Lens<DeliverySchedule, Option<string>> NoteLens =
        Lens.Create<DeliverySchedule, Option<string>>(
            static value => value.Note,
            static (value, next) => value with { Note = next });

    internal static PatchSchema<DeliverySchedule> Schema { get; } = CreateSchema();

    private static PatchSchema<DeliverySchedule> CreateSchema()
    {
        var created = PatchSchema<DeliverySchedule>.Create(
            ScheduleContract,
            Patch.Field("startsAt", Contract.Int32Range(0, 1440), StartsAtLens),
            Patch.Field("endsAt", Contract.Int32Range(0, 1440), EndsAtLens),
            Patch.ClearableField(
                "note",
                Contract.Optional(Contract.NonEmptyString),
                NoteLens,
                static value => value with { Note = Option<string>.None }));

        if (created.TryGetError(out var limitation))
        {
            throw new InvalidOperationException(limitation.ToString());
        }

        _ = created.TryGetValue(out var schema);
        return schema!;
    }

    private static Contract<DeliverySchedule> BuildContract()
    {
        var fields = Contract.For<DeliverySchedule>();
        return Contract.Object(
                fields.Field("timeZone", Contract.NonEmptyString, static value => value.TimeZone),
                fields.Field("startsAt", Contract.Int32Range(0, 1440), static value => value.StartsAt),
                fields.Field("endsAt", Contract.Int32Range(0, 1440), static value => value.EndsAt),
                fields.OptionalField("note", Contract.NonEmptyString, static value => value.Note),
                static (timeZone, startsAt, endsAt, note) =>
                    new DeliverySchedule(timeZone, startsAt, endsAt, note))
            .Ensure(
                static value => value.StartsAt < value.EndsAt,
                "startsAt must be before endsAt");
    }
}
