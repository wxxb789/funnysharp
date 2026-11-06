# Typed ASP.NET Core Boundary Evidence

This record supports the maintainer's judgment for
[Goal 0031](goals/archive/0031-goal.md): whether caller-selected typed results preserve endpoint
information while keeping mapping policy and the complete consumer cost visible. It is comparison
and behavioral evidence, not a completion claim or a new gate.

Source identity is the Goal 0031 working tree on top of commit `edfd097`; the landing commit
becomes the durable identity. Verification ran on this host:

- Ubuntu 24.04.5 LTS, Linux 7.0.0-1017-azure, x86_64.
- .NET SDK 10.0.401, runtime 10.0.12.
- Date: 2026-11-05 (UTC).

## Selected API

[TypedHttpResultExtensions.cs](../src/FunnySharp.AspNetCore/TypedHttpResultExtensions.cs) adds
`ToHttpResults` and `ToHttpResultsAsync` for `Option`, `Result`, `UnitResult`, and `Validation`.
Each carrier has a default-success overload and a caller-selected-success overload; `Task` and
`ValueTask` sources mirror those forms. The method returns the concrete ASP.NET Core union, for
example `Results<Ok<Order>, NotFound<ProblemDetails>>`, rather than `IResult`.

The existing `ToHttpResult`/`ToHttpResultAsync` surface remains unchanged. It still accepts
`Func<..., ProblemDetails>` mappers and returns `IResult`; callers choose between the explicit
problem mapper and the typed boundary instead of the package silently changing either contract.

The typed methods add no serializer, middleware, DI registration, exception handler, or global
error policy. They construct the union selected by the caller's mappers. The library does not
inspect `ProblemDetails.Status` or synthesize response metadata from runtime values.

## Complete Consumer Comparison

All three consumers below appear in
[TypedHttpResultComparisonTests.cs](../tests/FunnySharp.AspNetCore.Tests/TypedHttpResultComparisonTests.cs).
They map the same `Result<Order, OrderFailure>` to the same 200, 404, and 409 responses and are
checked for identical HTTP status, body, content type, and endpoint metadata.

Ordinary if-plus-`TypedResults`:

```csharp
app.MapGet("/compare/ordinary/{id}", (string id) => Ordinary(id));

private static Results<Ok<Order>, Results<NotFound<ProblemDetails>, Conflict<ProblemDetails>>> Ordinary(
    string id)
{
    var outcome = Find(id);
    if (outcome.TryGetValue(out var order))
    {
        return TypedResults.Ok(order);
    }

    _ = outcome.TryGetError(out var failure);
    return MapFailure(failure!);
}
```

Explicit `Match`:

```csharp
app.MapGet("/compare/match/{id}", (string id) => Match(id));

private static Results<Ok<Order>, Results<NotFound<ProblemDetails>, Conflict<ProblemDetails>>> Match(
    string id) =>
    Find(id).Match<Results<Ok<Order>, Results<NotFound<ProblemDetails>, Conflict<ProblemDetails>>>>(
        order => TypedResults.Ok(order),
        failure => MapFailure(failure));
```

Typed helper:

```csharp
app.MapGet("/compare/typed/{id}", (string id) =>
    Find(id).ToHttpResults(MapFailure));
```

Shared status policy for all three:

```csharp
private static Results<NotFound<ProblemDetails>, Conflict<ProblemDetails>> MapFailure(
    OrderFailure failure) =>
    failure.Kind == OrderFailureKind.Conflict
        ? TypedResults.Conflict(Problem(failure, StatusCodes.Status409Conflict))
        : TypedResults.NotFound(Problem(failure, StatusCodes.Status404NotFound));
```

Raw source-line accounting, including braces and line wrapping; this is not a LOC target:

| Version | First endpoint, including shared mapper | Additional endpoint | Static union declarations |
| --- | ---: | ---: | --- |
| Ordinary if-plus-`TypedResults` | 18 | 13 | Return type plus manual branch |
| Explicit `Match` | 11 | 6 | Return type plus repeated generic argument |
| `ToHttpResults` | 7 | 2 | Inferred by the compiler from selected mappers |

The helper does not relocate the mapping decision: every version still calls the same
`MapFailure`, which owns status, body, and error disclosure. It removes the repeated union return
type and `Match<Results<...>>` generic argument, and it cannot silently infer `IResult` for the
endpoint. The first-endpoint saving is 11 raw lines over the ordinary form and 4 over the explicit
`Match` form; each additional endpoint saves 11 or 4 respectively. The shared mapper remains
explicit, so a multi-status policy stays named once rather than being hidden in the adapter.

For a single-failure endpoint the default success mapper saves the success branch as well; the
compiled examples in
[TypedHttpResultExtensionsTests.cs](../tests/FunnySharp.AspNetCore.Tests/TypedHttpResultExtensionsTests.cs)
use that form for `Option`, `Result`, and `UnitResult`.

## Actual HTTP Responses

The focused suite runs these through `Microsoft.AspNetCore.TestHost`:

| Endpoint shape | Success | Expected failure | Accumulated errors |
| --- | --- | --- | --- |
| `Option<T>` | 200 `Payload`, `application/json` | 404 `ProblemDetails`, `application/json` | — |
| `Result<TValue, TError>` | 200 `Payload`, `application/json` | 404 or 409 `ProblemDetails`, `application/json` | — |
| `UnitResult<TError>` | 204, empty body | 409 `ProblemDetails`, `application/json` | — |
| `Validation<TValue, TError>` | 200 `Payload`, `application/json` | — | 400 `HttpValidationProblemDetails`, `application/problem+json` |
| Caller-selected 422 validation | — | — | 422 body with `status: 422` and grouped field errors |
| `Task`/`ValueTask` sources | 200 | 404 or 409 | 400 for validation |
| `Effect` composed by the example | 200 | 404 | — |

The executable consumer
[Program.cs](../examples/FunnySharp.AspNetCore.Examples/Program.cs) starts Kestrel on an ephemeral
loopback port under `--verify` and issues real HTTP requests for the success and failure paths.
It checks the returned bodies, the 400 accumulated validation errors, the caller-selected 422
status, the 204 no-content response, and the composed effect's `RequestAborted` path. The command
prints `FunnySharp ASP.NET Core example endpoints mapped.` only after those checks pass.

## Endpoint Metadata

Metadata tests read `IProducesResponseTypeMetadata` from the built endpoints:

| Selected typed results | Published declarations |
| --- | --- |
| `Results<Ok<Payload>, NotFound<ProblemDetails>>` | 200 `Payload`; 404 `ProblemDetails` |
| `Results<Ok<Payload>, Results<NotFound<ProblemDetails>, Conflict<ProblemDetails>>>` | 200 `Payload`; 404 `ProblemDetails`; 409 `ProblemDetails` |
| `Results<NoContent, Conflict<ProblemDetails>>` | 204 (void, no body); 409 `ProblemDetails` |
| `Results<Ok<Payload>, ValidationProblem>` | 200 `Payload`; 400 `HttpValidationProblemDetails` |
| `Results<Ok<Payload>, UnprocessableEntity<HttpValidationProblemDetails>>` | 200 `Payload`; 422 `HttpValidationProblemDetails` |
| `Results<Ok<Payload>, ProblemHttpResult>` | 200 `Payload` only |

The JSON content types reported by metadata match the actual writes: status-specific wrappers
such as `NotFound<ProblemDetails>` write `application/json`; `ValidationProblem` writes
`application/problem+json`; `ProblemHttpResult` writes `application/problem+json`.

### Dynamic ProblemDetails

`TypedResults.Problem(problemDetails)` returns `ProblemHttpResult`. ASP.NET Core builds metadata
from the static result type, not from the runtime `problemDetails.Status`, so the union above
publishes no 404, 409, or other problem declaration. The test
`DynamicProblemResultDoesNotInventResponseDeclarations` pins that behavior and verifies the actual
response is still 409 with `application/problem+json`.

The same endpoint with `.ProducesProblem(StatusCodes.Status409Conflict)` publishes the 409
declaration. This is caller-owned endpoint configuration; the library never expands one runtime
status into every possible status. If an endpoint can return several statuses, it declares each
one or selects status-specific typed results.

## OpenAPI

The executable consumer enables `Microsoft.AspNetCore.OpenApi` 10.0.12 and verifies the generated
document at `/openapi/v1.json` after starting the host. It checks:

| Operation | Responses checked |
| --- | --- |
| `GET /typed/orders/{id}` | 200 `application/json Order`; 404 `application/json ProblemDetails` |
| `POST /typed/orders` | 201 `application/json Order`; 400 `application/problem+json HttpValidationProblemDetails` |

The check reads the actual paths, methods, status keys, media types, and schema references from the
generated document. No hand-written OpenAPI document is compared.

## Asynchronous Behavior

The typed `Task` and `ValueTask` forms await the source once and propagate the same cancellation and
fault behavior as the existing mapping helpers:

- A faulted `OperationCanceledException` remains the same exception instance, preserves its token,
  and leaves the returned task canceled rather than faulted.
- An independent `InvalidOperationException` remains the same faulted exception.
- Mapper exceptions propagate unchanged.
- `ValueTask` sources are consumed exactly once for completed and pending inputs.
- The effect example explicitly passes `context.RequestAborted` to `RunAsync`; the typed mapping
  then runs only after the effect completes. The integration does not replace or swallow the token.

## Limits

- **Serializers:** the tests and example use ASP.NET Core's default `System.Text.Json` reflection
  path with ordinary records. Source-generated serializer contexts, other JSON serializers, and
  custom converters were not exercised. `ProblemDetails` bodies keep their existing framework
  serialization; the integration adds no converter.
- **Content type:** `TypedResults.NotFound(problem)` and other status-specific wrappers with a
  `ProblemDetails` body write `application/json`. A `problem+json` response with a dynamic status
  requires `ProblemHttpResult` plus caller-declared `.Produces*` metadata, or another caller-owned
  typed result.
- **Trimming and Native AOT:** the integration assembly is marked trimmable, but this goal did not
  run trimming or Native AOT. Typed-result metadata and serialization remain framework and
  application responsibilities; the passing narrow consumer does not establish AOT behavior.
- **OpenAPI:** verified with `Microsoft.AspNetCore.OpenApi` 10.0.12 and the default document
  generator. Other OpenAPI packages, transformers, document names, or schema customization were
  not exercised.
- **Framework behavior:** the published declarations and content types above are ASP.NET Core
  behavior attached to the selected typed result classes. The library does not duplicate or
  override that metadata.

## Deliberate Non-Guarantees

The evidence does not claim:

- automatic status selection from a domain error;
- automatic serialization or deserialization of `Option`, `Result`, `UnitResult`, or `Validation`;
- automatic grouping of validation errors or a fixed error field vocabulary;
- exception-to-HTTP conversion, a global exception handler, or a problem-details convention;
- middleware, dependency injection registration, service location, or a replacement web framework;
- trimming, Native AOT, source-generated JSON, or non-`System.Text.Json` support;
- that a generated OpenAPI document proves the business meaning of a response.

## Verification

Commands run for this evidence:

```bash
dotnet build src/FunnySharp.AspNetCore/FunnySharp.AspNetCore.csproj -c Release --no-restore
dotnet build tests/FunnySharp.AspNetCore.Tests/FunnySharp.AspNetCore.Tests.csproj -c Release --no-restore
dotnet test tests/FunnySharp.AspNetCore.Tests/FunnySharp.AspNetCore.Tests.csproj -c Release --no-build --no-restore
dotnet run --project examples/FunnySharp.AspNetCore.Examples/FunnySharp.AspNetCore.Examples.csproj -c Release --no-build --no-restore -- --verify
dotnet fsi build.fsx -- -p verify-docs-snippets
dotnet fsi build.fsx -- -p verify-api-baseline
```

Observed results:

- ASP.NET Core test project: 50 passed, 0 failed, 0 skipped.
- Example verification: `FunnySharp ASP.NET Core example endpoints mapped.`
- Documentation snippets: 60 snippets across 11 primary guides.
- API baseline: the typed surface is recorded in
  [eng/api-baseline/FunnySharp.AspNetCore.public-api.txt](../eng/api-baseline/FunnySharp.AspNetCore.public-api.txt).
