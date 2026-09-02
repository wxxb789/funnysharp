# ASP.NET Core Integration

`FunnySharp.AspNetCore` is the optional HTTP-boundary package for Minimal APIs. It maps
`Option<T>`, `Result<TValue, TError>`, and `Validation<TValue, TError>` to `IResult` without
introducing ASP.NET Core types into the `FunnySharp` core package.

## Package Boundary

Add the integration package alongside the core package in an ASP.NET Core application. The
integration assembly depends on `FunnySharp` and the `Microsoft.AspNetCore.App` framework
reference only. It provides no DI registrations, service location, middleware, global error
policy, or exception handler. Domain code can continue to use the core package without any HTTP
concepts.

## Mapping Outcomes

Every mapping requires an explicit problem mapper. It must return a non-null `ProblemDetails` or
`HttpValidationProblemDetails` with `Status` set. A success mapper is optional: when omitted,
the integration layer returns `Results.Ok(value)`. Supply one to select a different success status,
headers, or body shape.

```csharp
IResult ToHttpResult<T>(
    this Option<T> option,
    Func<ProblemDetails> none,
    Func<T, IResult>? some = null);

IResult ToHttpResult<TValue, TError>(
    this Result<TValue, TError> result,
    Func<TError, ProblemDetails> failure,
    Func<TValue, IResult>? success = null);

IResult ToHttpResult<TValue, TError>(
    this Validation<TValue, TError> validation,
    Func<IReadOnlyList<TError>, HttpValidationProblemDetails> invalid,
    Func<TValue, IResult>? valid = null);
```

Equivalent `ToHttpResultAsync` overloads accept `Task<...>` and `ValueTask<...>` carriers.
They await the supplied operation normally: faults and cancellation are transparent and are not
converted into HTTP responses.

```csharp
app.MapGet("/inventory/{sku}", (string sku, CancellationToken cancellationToken) =>
    GetInventoryAsync(sku, cancellationToken).ToHttpResultAsync(MapInventoryFailure));

app.MapGet("/deliveries/{sku}", (string sku, CancellationToken cancellationToken) =>
    GetDeliveryAsync(sku, cancellationToken).ToHttpResultAsync(
        MapDeliveryFailure,
        delivery => new AcceptedDeliveryResult(delivery)));
```

`ProblemDetails` mappers control both HTTP status and payload. For example, an absent option can
produce 404 while a typed domain failure produces 409; neither convention is selected by the
library.

```csharp
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
```

For `Validation`, the mapper receives the complete ordered error list. Grouping for
`HttpValidationProblemDetails.Errors` is application policy. When grouping, add fields on their
first encounter and append messages in input order so a field's array retains the validation
order. The integration forwards `Errors`, the standard `ProblemDetails` fields, and `Extensions`;
put application-specific data in `Extensions` rather than custom subclass properties. The compiled
example shows this explicitly.

## Effects And Cancellation

Effects have overloads for values that produce an `Option`, `Result`, or `Validation`. The
`HttpContext` argument is intentional: the integration calls `RunAsync` with exactly
`context.RequestAborted`; it does not create, link, replace, or swallow the request cancellation
token.

```csharp
app.MapGet("/catalog/{id:int}/refresh", (HttpContext context, int id) =>
    LoadProductEffect(id).ToHttpResultAsync(
        catalog,
        context,
        () => NotFound()));
```

Environment-dependent effects require the caller to supply their environment explicitly. This
keeps dependency acquisition and HTTP policy visible at the endpoint boundary.

## Before And After

The mapping keeps the endpoint focused on domain work and chosen HTTP policy while retaining both
branches visibly in the mapper functions.

```csharp
// Before
var product = await LoadProductEffect(id).RunAsync(catalog, context.RequestAborted);
return product.Match(
    value => Results.Ok(value),
    () => Results.Problem(statusCode: StatusCodes.Status404NotFound));

// After
return await LoadProductEffect(id).ToHttpResultAsync(
    catalog,
    context,
    () => new ProblemDetails { Status = StatusCodes.Status404NotFound });
```

Run the compiling Minimal API sample without starting a server:

```shell
dotnet run --project examples/FunnySharp.AspNetCore.Examples/FunnySharp.AspNetCore.Examples.csproj --configuration Release -- --verify
```
