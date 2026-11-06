# ASP.NET Core Integration

`FunnySharp.AspNetCore` is the optional HTTP-boundary package for Minimal APIs. It maps
`Option<T>`, `Result<TValue, TError>`, `UnitResult<TError>`, and `Validation<TValue, TError>` to
`IResult` or caller-selected typed results without introducing ASP.NET Core types into the
`FunnySharp` core package.

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

<!-- documentation-sample: DocumentationSamples.AspNetCore.MappingOutcomes -->
```csharp
IResult optionResult = option.ToHttpResult(NotFound);
IResult resultResult = result.ToHttpResult(Conflict);
IResult validationResult = validation.ToHttpResult(MapValidationFailure);
```

Equivalent `ToHttpResultAsync` overloads accept `Task<...>` and `ValueTask<...>` carriers.
They await the supplied operation normally: faults and cancellation are transparent and are not
converted into HTTP responses.

<!-- documentation-sample: DocumentationSamples.AspNetCore.MapAsyncOutcomes -->
```csharp
app.MapGet("/inventory/{sku}", (string sku, CancellationToken cancellationToken) =>
    GetInventoryAsync(sku, cancellationToken).ToHttpResultAsync(NotFound));

app.MapGet("/deliveries/{sku}", (string sku, CancellationToken cancellationToken) =>
    GetDeliveryAsync(sku, cancellationToken).ToHttpResultAsync(
        Conflict,
        delivery => Results.Accepted(value: delivery)));
```

`ProblemDetails` mappers control both HTTP status and payload. For example, an absent option can
produce 404 while a typed domain failure produces 409; neither convention is selected by the
library.

<!-- documentation-sample: DocumentationSamples.AspNetCore.MapProblemDetails -->
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

A [partial-update schema](partial-updates.md) is not a new HTTP surface. Bind the request body as
`JsonElement`, call `PatchSchema<T>.Decode` followed by `PatchSchema<T>.Validate` against the
current value, and map either `DecodeError` list with the existing `Validation<...>.ToHttpResult`
boundary. The application still chooses persistence, concurrency checks, status codes, and problem
details. The compiled ASP.NET Core example includes a `PATCH` endpoint using this path.

For `UnitResult<TError>`, the failure mapper returns a `ProblemDetails` with a status exactly like
`Result`. Because there is no successful value, the default success result is `204 No Content`;
supply a success mapper to select a different status, headers, or body.

<!-- documentation-sample: DocumentationSamples.AspNetCore.UnitResultOutcome -->
```csharp
app.MapDelete("/carts/{id:int}", (int id, CancellationToken cancellationToken) =>
    DeleteCartAsync(id, cancellationToken).ToHttpResultAsync(OrderConflict));

app.MapPost("/orders/{id:int}/confirm", (int id, CancellationToken cancellationToken) =>
    ConfirmOrderAsync(id, cancellationToken).ToHttpResultAsync(
        OrderConflict,
        () => Results.Accepted()));
```

## Typed Results

`ToHttpResults` and `ToHttpResultsAsync` map an outcome to a `Results<...>` union whose type
arguments are the typed results the caller selected. The union stays visible in the endpoint's
static return shape, so ASP.NET Core can publish response metadata and OpenAPI declarations from
the actual result types instead of reading an erased `IResult`.

<!-- documentation-sample: DocumentationSamples.AspNetCore.TypedOutcomes -->
```csharp
app.MapGet("/products/{id:int}", (int id) =>
    FindProduct(id).ToHttpResults(() => TypedResults.NotFound(NotFound())));

app.MapPost("/orders", (CreateOrderRequest request) =>
    ValidateOrder(request).ToHttpResults(
        errors => TypedResults.ValidationProblem(GroupValidation(errors)),
        order => TypedResults.Created($"/orders/{order.Id}", order)));
```

The default success result is `Ok<T>` for `Option`, `Result`, and `Validation`, and `NoContent`
for `UnitResult`. Supply a success or valid mapper to select a different typed result. Only the
selected outcome mapper runs; required arguments and mapper results are checked eagerly, and
mapper exceptions propagate unchanged.

The failure mapper receives the complete typed failure (`TError`, or the ordered
`IReadOnlyList<TError>` for `Validation`) and returns whatever typed result carries the
application's status and body policy. For example, `TypedResults.ValidationProblem` produces 400
with `HttpValidationProblemDetails`; `TypedResults.UnprocessableEntity(httpValidationProblem)`
produces a caller-selected 422. Error grouping and disclosure remain application policy.

### Metadata And Dynamic Problem Status

Status-specific typed results publish their real response declarations:
`NotFound<ProblemDetails>` publishes 404 with `ProblemDetails`; `ValidationProblem` publishes 400
with `HttpValidationProblemDetails`; `NoContent` publishes 204 with no body. The content type is
the one written by that typed result. In particular, `TypedResults.NotFound(problem)` writes
`application/json`, while `TypedResults.Problem(problem)` writes `application/problem+json`.

`ProblemHttpResult` carries a runtime `ProblemDetails.Status`. ASP.NET Core cannot know that value
when it builds endpoint metadata, so it publishes no response declaration for that result. The
library does not invent every possible status from the runtime value. Declare the statuses the
endpoint can produce, or select a status-specific typed result:

<!-- documentation-sample: DocumentationSamples.AspNetCore.TypedFailurePolicy -->
```csharp
app.MapGet("/orders/{id}/typed", (string id) =>
    FindOrder(id).ToHttpResults(MapOrderFailure));

app.MapGet("/orders/{id}/dynamic", (string id) =>
        FindOrder(id).ToHttpResults(error => TypedResults.Problem(Conflict(error))))
    .ProducesProblem(StatusCodes.Status409Conflict);
```

`ProducesProblem` (or another `.Produces*` call) is endpoint configuration; it is not automatic
inference from domain errors. A typed result union is not a promise that every branch executes,
only that the endpoint may return those shapes.

### Async Outcomes

`ToHttpResultsAsync` accepts `Task<...>` and `ValueTask<...>` carriers and returns
`Task<Results<...>>` or `ValueTask<Results<...>>` with the same type arguments. It awaits the input
once, without sync-over-async. Cancellation and faults propagate unchanged; they are not converted
into HTTP responses.

<!-- documentation-sample: DocumentationSamples.AspNetCore.TypedAsyncOutcome -->
```csharp
app.MapGet("/inventory/{sku}", (string sku, CancellationToken cancellationToken) =>
    GetStockAsync(sku, cancellationToken).ToHttpResultsAsync(
        error => TypedResults.NotFound(NotFound(error))));
```

Effects have no separate typed overload. Await `RunAsync` with exactly `context.RequestAborted`
and map the resulting carrier; the token remains the caller's and is never replaced or swallowed.

<!-- documentation-sample: DocumentationSamples.AspNetCore.TypedEffectOutcome -->
```csharp
app.MapGet("/catalog/{id:int}/refresh", async (HttpContext context, int id) =>
{
    var product = await LoadProductEffect(id).RunAsync(catalog, context.RequestAborted);
    return product.ToHttpResults(() => TypedResults.NotFound(NotFound()));
});
```

### Comparison

The compiled comparison in
[tests/FunnySharp.AspNetCore.Tests/TypedHttpResultComparisonTests.cs](../tests/FunnySharp.AspNetCore.Tests/TypedHttpResultComparisonTests.cs)
maps the same success, 404, and 409 outcomes through three complete consumers: an ordinary
if-plus-`TypedResults` handler, an explicit `Match<Results<...>>` handler, and `ToHttpResults`.
They produce identical HTTP responses and endpoint metadata. The ordinary handler declares the
union return type and branches manually; the `Match` handler still declares the union return type
and repeats it as the generic argument; `ToHttpResults` infers both from the selected mappers, so
the endpoint contains only the domain call and the mapping policy. A failure-union mapper remains
an ordinary method with an explicit union return type, which names the status policy once instead
of at every endpoint.

The helper does not reduce the mapping logic: the caller still chooses status, body, and error
disclosure. It removes repeated union construction and infers the union from the selected mappers'
static return types. The union argument is the mapper's static result type: a mapper declared as
`IResult` (for example `Results.NotFound(...)` or a legacy `static IResult` helper) makes its branch
run without contributing a response declaration. Select `TypedResults.*` or a status-specific typed
result when the endpoint should publish that response.

## Effects And Cancellation

Effects have overloads for values that produce an `Option`, `Result`, `UnitResult`, or
`Validation`. The `HttpContext` argument is intentional: the integration calls `RunAsync` with
exactly `context.RequestAborted`; it does not create, link, replace, or swallow the request
cancellation token.

<!-- documentation-sample: DocumentationSamples.AspNetCore.MapEffect -->
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

<!-- documentation-sample: DocumentationSamples.AspNetCore.CompareBeforeAndAfter -->
```csharp
// Before
var product = await LoadProductEffect(id).RunAsync(catalog, context.RequestAborted);
_ = product.Match(
    value => Results.Ok(value),
    () => Results.Problem(statusCode: StatusCodes.Status404NotFound));

// After
_ = await LoadProductEffect(id).ToHttpResultAsync(
    catalog,
    context,
    () => new ProblemDetails { Status = StatusCodes.Status404NotFound });
```

Run the compiling Minimal API sample; `--verify` starts an ephemeral loopback server to check actual
HTTP responses, endpoint metadata, and the generated OpenAPI document:

```shell
dotnet run --project examples/FunnySharp.AspNetCore.Examples/FunnySharp.AspNetCore.Examples.csproj --configuration Release -- --verify
```

## Performance Boundary

Request pipeline, serialization, dependency injection, transport, and application policy dominate
realistic endpoint measurements and remain caller-owned. The mapping helpers are measured in
isolation: the performance manifest assigns a blocking allocation budget to the carrier-to-`IResult`
mapping of each outcome shape, paired against the equivalent hand-written Minimal API code.

## Measured mapping cost

Each row pairs the hand-written direct baseline with the FunnySharp mapping helper for the same
carrier outcome. Only the mapping call is measured: no result is executed and no host is started.

<!-- performance-table:start aspnet-core -->
| Scenario | Baseline mean | Candidate mean | Ratio | Baseline allocation | Candidate allocation |
| --- | ---: | ---: | ---: | ---: | ---: |
| HTTP mapping overhead - absence | 23.200 ns | 26.420 ns | 1.14x | 168 B | 168 B |
| HTTP mapping overhead - no value | 1.011 ns | 1.442 ns | 1.43x | 0 B | 0 B |
| HTTP mapping overhead - success | 4.440 ns | 5.045 ns | 1.14x | 24 B | 24 B |
| HTTP mapping overhead - typed failure | 22.942 ns | 27.182 ns | 1.18x | 168 B | 168 B |
<!-- performance-table:end aspnet-core -->
## Deliberate Boundaries

- The integration package adds no serializer. The example and tests exercise ASP.NET Core's
  default `System.Text.Json` reflection path with ordinary records. Source-generated serializer
  contexts, other JSON serializers, and custom converters were not tested.
- Typed result classes, endpoint metadata, and OpenAPI generation are framework behavior. The
  example verifies generated OpenAPI with `Microsoft.AspNetCore.OpenApi` 10.0.12. Trimming and
  Native AOT were not exercised by this goal; do not infer their behavior from this narrow
  consumer. The integration assembly is marked trimmable, but result metadata and serialization
  remain the framework's and application's responsibility.
- `ProblemHttpResult` contributes no automatic response metadata. The library never synthesizes
  response declarations from `ProblemDetails.Status`; callers declare possible statuses with
  `.Produces*` or select status-specific typed results.
- The package provides no middleware, DI registration, service location, global exception
  handler, exception-to-response policy, or web framework. It does not convert unexpected
  exceptions into HTTP responses.
- The performance table measures the existing `IResult` mapping only. Typed mapping adds union
  construction but no I/O; it is not benchmarked here.
