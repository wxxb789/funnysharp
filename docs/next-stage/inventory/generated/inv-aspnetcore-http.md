# Public API inventory: ASP.NET Core HTTP results (F10, framework reference)

Assemblies: Microsoft.AspNetCore.Http.Abstractions 10.0.0.0, Microsoft.AspNetCore.Http.Results 10.0.0.0, Microsoft.AspNetCore.Http 10.0.0.0

Type count: 11

## Microsoft.AspNetCore.Http

### HttpValidationProblemDetails (class) : Microsoft.AspNetCore.Mvc.ProblemDetails

- `public HttpValidationProblemDetails()`
- `public HttpValidationProblemDetails(System.Collections.Generic.IDictionary<System.String, System.String[]> errors)`
- `public HttpValidationProblemDetails(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String, System.String[]>> errors)`
- `public System.Collections.Generic.IDictionary<System.String, System.String[]> Errors { get; set; }`

### IProblemDetailsService (interface)

- `public System.Threading.Tasks.ValueTask<System.Boolean> TryWriteAsync(Microsoft.AspNetCore.Http.ProblemDetailsContext context)`
- `public System.Threading.Tasks.ValueTask WriteAsync(Microsoft.AspNetCore.Http.ProblemDetailsContext context)`

### IResult (interface)

- `public System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Http.HttpContext httpContext)`

### IResultExtensions (interface)


### IStatusCodeHttpResult (interface)

- `public System.Int32? StatusCode { get; }`

### IValueHttpResult (interface)

- `public System.Object Value { get; }`

### ProblemDetailsContext (class [sealed])

- `public ProblemDetailsContext()`
- `public Microsoft.AspNetCore.Http.EndpointMetadataCollection AdditionalMetadata { get; init; }`
- `public System.Exception Exception { get; init; }`
- `public Microsoft.AspNetCore.Http.HttpContext HttpContext { get; init; }`
- `public Microsoft.AspNetCore.Mvc.ProblemDetails ProblemDetails { get; set; }`

### Results (class [static])

- `public static Microsoft.AspNetCore.Http.IResult Empty { get; }`
- `public static Microsoft.AspNetCore.Http.IResultExtensions Extensions { get; }`
- `public static Microsoft.AspNetCore.Http.IResult Accepted(System.String uri, System.Object value)`
- `public static Microsoft.AspNetCore.Http.IResult Accepted<TValue>(System.String uri, TValue value)`
- `public static Microsoft.AspNetCore.Http.IResult AcceptedAtRoute(System.String routeName, System.Object routeValues, System.Object value)`
- `public static Microsoft.AspNetCore.Http.IResult AcceptedAtRoute(System.String routeName, Microsoft.AspNetCore.Routing.RouteValueDictionary routeValues, System.Object value)`
- `public static Microsoft.AspNetCore.Http.IResult AcceptedAtRoute<TValue>(System.String routeName, System.Object routeValues, TValue value)`
- `public static Microsoft.AspNetCore.Http.IResult AcceptedAtRoute<TValue>(System.String routeName, Microsoft.AspNetCore.Routing.RouteValueDictionary routeValues, TValue value)`
- `public static Microsoft.AspNetCore.Http.IResult BadRequest(System.Object error)`
- `public static Microsoft.AspNetCore.Http.IResult BadRequest<TValue>(TValue error)`
- `public static Microsoft.AspNetCore.Http.IResult Bytes(System.Byte[] contents, System.String contentType, System.String fileDownloadName, System.Boolean enableRangeProcessing, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag)`
- `public static Microsoft.AspNetCore.Http.IResult Bytes(System.ReadOnlyMemory<System.Byte> contents, System.String contentType, System.String fileDownloadName, System.Boolean enableRangeProcessing, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag)`
- `public static Microsoft.AspNetCore.Http.IResult Challenge(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, System.Collections.Generic.IList<System.String> authenticationSchemes)`
- `public static Microsoft.AspNetCore.Http.IResult Conflict(System.Object error)`
- `public static Microsoft.AspNetCore.Http.IResult Conflict<TValue>(TValue error)`
- `public static Microsoft.AspNetCore.Http.IResult Content(System.String content, Microsoft.Net.Http.Headers.MediaTypeHeaderValue contentType)`
- `public static Microsoft.AspNetCore.Http.IResult Content(System.String content, System.String contentType, System.Text.Encoding contentEncoding)`
- `public static Microsoft.AspNetCore.Http.IResult Content(System.String content, System.String contentType, System.Text.Encoding contentEncoding, System.Int32? statusCode)`
- `public static Microsoft.AspNetCore.Http.IResult Created()`
- `public static Microsoft.AspNetCore.Http.IResult Created(System.String uri, System.Object value)`
- `public static Microsoft.AspNetCore.Http.IResult Created<TValue>(System.String uri, TValue value)`
- `public static Microsoft.AspNetCore.Http.IResult Created(System.Uri uri, System.Object value)`
- `public static Microsoft.AspNetCore.Http.IResult Created<TValue>(System.Uri uri, TValue value)`
- `public static Microsoft.AspNetCore.Http.IResult CreatedAtRoute(System.String routeName, System.Object routeValues, System.Object value)`
- `public static Microsoft.AspNetCore.Http.IResult CreatedAtRoute(System.String routeName, Microsoft.AspNetCore.Routing.RouteValueDictionary routeValues, System.Object value)`
- `public static Microsoft.AspNetCore.Http.IResult CreatedAtRoute<TValue>(System.String routeName, System.Object routeValues, TValue value)`
- `public static Microsoft.AspNetCore.Http.IResult CreatedAtRoute<TValue>(System.String routeName, Microsoft.AspNetCore.Routing.RouteValueDictionary routeValues, TValue value)`
- `public static Microsoft.AspNetCore.Http.IResult File(System.Byte[] fileContents, System.String contentType, System.String fileDownloadName, System.Boolean enableRangeProcessing, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag)`
- `public static Microsoft.AspNetCore.Http.IResult File(System.IO.Stream fileStream, System.String contentType, System.String fileDownloadName, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag, System.Boolean enableRangeProcessing)`
- `public static Microsoft.AspNetCore.Http.IResult File(System.String path, System.String contentType, System.String fileDownloadName, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag, System.Boolean enableRangeProcessing)`
- `public static Microsoft.AspNetCore.Http.IResult Forbid(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, System.Collections.Generic.IList<System.String> authenticationSchemes)`
- `public static Microsoft.AspNetCore.Http.IResult InternalServerError()`
- `public static Microsoft.AspNetCore.Http.IResult InternalServerError<TValue>(TValue error)`
- `public static Microsoft.AspNetCore.Http.IResult Json(System.Object data, System.Text.Json.JsonSerializerOptions options, System.String contentType, System.Int32? statusCode)`
- `public static Microsoft.AspNetCore.Http.IResult Json(System.Object data, System.Text.Json.Serialization.Metadata.JsonTypeInfo jsonTypeInfo, System.String contentType, System.Int32? statusCode)`
- `public static Microsoft.AspNetCore.Http.IResult Json<TValue>(TValue data, System.Text.Json.JsonSerializerOptions options, System.String contentType, System.Int32? statusCode)`
- `public static Microsoft.AspNetCore.Http.IResult Json<TValue>(TValue data, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo, System.String contentType, System.Int32? statusCode)`
- `public static Microsoft.AspNetCore.Http.IResult Json<TValue>(TValue data, System.Text.Json.Serialization.JsonSerializerContext context, System.String contentType, System.Int32? statusCode)`
- `public static Microsoft.AspNetCore.Http.IResult Json(System.Object data, System.Type type, System.Text.Json.Serialization.JsonSerializerContext context, System.String contentType, System.Int32? statusCode)`
- `public static Microsoft.AspNetCore.Http.IResult LocalRedirect(System.String localUrl, System.Boolean permanent, System.Boolean preserveMethod)`
- `public static Microsoft.AspNetCore.Http.IResult NoContent()`
- `public static Microsoft.AspNetCore.Http.IResult NotFound(System.Object value)`
- `public static Microsoft.AspNetCore.Http.IResult NotFound<TValue>(TValue value)`
- `public static Microsoft.AspNetCore.Http.IResult Ok(System.Object value)`
- `public static Microsoft.AspNetCore.Http.IResult Ok<TValue>(TValue value)`
- `public static Microsoft.AspNetCore.Http.IResult Problem(Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails)`
- `public static Microsoft.AspNetCore.Http.IResult Problem(System.String detail, System.String instance, System.Int32? statusCode, System.String title, System.String type, System.Collections.Generic.IDictionary<System.String, System.Object> extensions)`
- `public static Microsoft.AspNetCore.Http.IResult Problem(System.String detail, System.String instance, System.Int32? statusCode, System.String title, System.String type, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String, System.Object>> extensions)`
- `public static Microsoft.AspNetCore.Http.IResult Redirect(System.String url, System.Boolean permanent, System.Boolean preserveMethod)`
- `public static Microsoft.AspNetCore.Http.IResult RedirectToRoute(System.String routeName, System.Object routeValues, System.Boolean permanent, System.Boolean preserveMethod, System.String fragment)`
- `public static Microsoft.AspNetCore.Http.IResult RedirectToRoute(System.String routeName, Microsoft.AspNetCore.Routing.RouteValueDictionary routeValues, System.Boolean permanent, System.Boolean preserveMethod, System.String fragment)`
- `public static Microsoft.AspNetCore.Http.IResult ServerSentEvents<T>(System.Collections.Generic.IAsyncEnumerable<System.Net.ServerSentEvents.SseItem<T>> values)`
- `public static Microsoft.AspNetCore.Http.IResult ServerSentEvents(System.Collections.Generic.IAsyncEnumerable<System.String> values, System.String eventType)`
- `public static Microsoft.AspNetCore.Http.IResult ServerSentEvents<T>(System.Collections.Generic.IAsyncEnumerable<T> values, System.String eventType)`
- `public static Microsoft.AspNetCore.Http.IResult SignIn(System.Security.Claims.ClaimsPrincipal principal, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, System.String authenticationScheme)`
- `public static Microsoft.AspNetCore.Http.IResult SignOut(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, System.Collections.Generic.IList<System.String> authenticationSchemes)`
- `public static Microsoft.AspNetCore.Http.IResult StatusCode(System.Int32 statusCode)`
- `public static Microsoft.AspNetCore.Http.IResult Stream(System.Func<System.IO.Stream, System.Threading.Tasks.Task> streamWriterCallback, System.String contentType, System.String fileDownloadName, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag)`
- `public static Microsoft.AspNetCore.Http.IResult Stream(System.IO.Stream stream, System.String contentType, System.String fileDownloadName, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag, System.Boolean enableRangeProcessing)`
- `public static Microsoft.AspNetCore.Http.IResult Stream(System.IO.Pipelines.PipeReader pipeReader, System.String contentType, System.String fileDownloadName, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag, System.Boolean enableRangeProcessing)`
- `public static Microsoft.AspNetCore.Http.IResult Text(System.String content, System.String contentType, System.Text.Encoding contentEncoding)`
- `public static Microsoft.AspNetCore.Http.IResult Text(System.ReadOnlySpan<System.Byte> utf8Content, System.String contentType, System.Int32? statusCode)`
- `public static Microsoft.AspNetCore.Http.IResult Text(System.String content, System.String contentType, System.Text.Encoding contentEncoding, System.Int32? statusCode)`
- `public static Microsoft.AspNetCore.Http.IResult Unauthorized()`
- `public static Microsoft.AspNetCore.Http.IResult UnprocessableEntity(System.Object error)`
- `public static Microsoft.AspNetCore.Http.IResult UnprocessableEntity<TValue>(TValue error)`
- `public static Microsoft.AspNetCore.Http.IResult ValidationProblem(System.Collections.Generic.IDictionary<System.String, System.String[]> errors, System.String detail, System.String instance, System.Int32? statusCode, System.String title, System.String type, System.Collections.Generic.IDictionary<System.String, System.Object> extensions)`
- `public static Microsoft.AspNetCore.Http.IResult ValidationProblem(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String, System.String[]>> errors, System.String detail, System.String instance, System.Int32? statusCode, System.String title, System.String type, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String, System.Object>> extensions)`

### StatusCodes (class [static])

- `public const System.Int32 Status100Continue`
- `public const System.Int32 Status101SwitchingProtocols`
- `public const System.Int32 Status102Processing`
- `public const System.Int32 Status200OK`
- `public const System.Int32 Status201Created`
- `public const System.Int32 Status202Accepted`
- `public const System.Int32 Status203NonAuthoritative`
- `public const System.Int32 Status204NoContent`
- `public const System.Int32 Status205ResetContent`
- `public const System.Int32 Status206PartialContent`
- `public const System.Int32 Status207MultiStatus`
- `public const System.Int32 Status208AlreadyReported`
- `public const System.Int32 Status226IMUsed`
- `public const System.Int32 Status300MultipleChoices`
- `public const System.Int32 Status301MovedPermanently`
- `public const System.Int32 Status302Found`
- `public const System.Int32 Status303SeeOther`
- `public const System.Int32 Status304NotModified`
- `public const System.Int32 Status305UseProxy`
- `public const System.Int32 Status306SwitchProxy`
- `public const System.Int32 Status307TemporaryRedirect`
- `public const System.Int32 Status308PermanentRedirect`
- `public const System.Int32 Status400BadRequest`
- `public const System.Int32 Status401Unauthorized`
- `public const System.Int32 Status402PaymentRequired`
- `public const System.Int32 Status403Forbidden`
- `public const System.Int32 Status404NotFound`
- `public const System.Int32 Status405MethodNotAllowed`
- `public const System.Int32 Status406NotAcceptable`
- `public const System.Int32 Status407ProxyAuthenticationRequired`
- `public const System.Int32 Status408RequestTimeout`
- `public const System.Int32 Status409Conflict`
- `public const System.Int32 Status410Gone`
- `public const System.Int32 Status411LengthRequired`
- `public const System.Int32 Status412PreconditionFailed`
- `public const System.Int32 Status413PayloadTooLarge`
- `public const System.Int32 Status413RequestEntityTooLarge`
- `public const System.Int32 Status414RequestUriTooLong`
- `public const System.Int32 Status414UriTooLong`
- `public const System.Int32 Status415UnsupportedMediaType`
- `public const System.Int32 Status416RangeNotSatisfiable`
- `public const System.Int32 Status416RequestedRangeNotSatisfiable`
- `public const System.Int32 Status417ExpectationFailed`
- `public const System.Int32 Status418ImATeapot`
- `public const System.Int32 Status419AuthenticationTimeout`
- `public const System.Int32 Status421MisdirectedRequest`
- `public const System.Int32 Status422UnprocessableEntity`
- `public const System.Int32 Status423Locked`
- `public const System.Int32 Status424FailedDependency`
- `public const System.Int32 Status426UpgradeRequired`
- `public const System.Int32 Status428PreconditionRequired`
- `public const System.Int32 Status429TooManyRequests`
- `public const System.Int32 Status431RequestHeaderFieldsTooLarge`
- `public const System.Int32 Status451UnavailableForLegalReasons`
- `public const System.Int32 Status499ClientClosedRequest`
- `public const System.Int32 Status500InternalServerError`
- `public const System.Int32 Status501NotImplemented`
- `public const System.Int32 Status502BadGateway`
- `public const System.Int32 Status503ServiceUnavailable`
- `public const System.Int32 Status504GatewayTimeout`
- `public const System.Int32 Status505HttpVersionNotsupported`
- `public const System.Int32 Status506VariantAlsoNegotiates`
- `public const System.Int32 Status507InsufficientStorage`
- `public const System.Int32 Status508LoopDetected`
- `public const System.Int32 Status510NotExtended`
- `public const System.Int32 Status511NetworkAuthenticationRequired`

### TypedResults (class [static])

- `public static Microsoft.AspNetCore.Http.HttpResults.EmptyHttpResult Empty { get; }`
- `public static Microsoft.AspNetCore.Http.IResultExtensions Extensions { get; }`
- `public static Microsoft.AspNetCore.Http.HttpResults.Accepted Accepted(System.String uri)`
- `public static Microsoft.AspNetCore.Http.HttpResults.Accepted Accepted(System.Uri uri)`
- `public static Microsoft.AspNetCore.Http.HttpResults.Accepted<TValue> Accepted<TValue>(System.String uri, TValue value)`
- `public static Microsoft.AspNetCore.Http.HttpResults.Accepted<TValue> Accepted<TValue>(System.Uri uri, TValue value)`
- `public static Microsoft.AspNetCore.Http.HttpResults.AcceptedAtRoute AcceptedAtRoute(System.String routeName, System.Object routeValues)`
- `public static Microsoft.AspNetCore.Http.HttpResults.AcceptedAtRoute AcceptedAtRoute(System.String routeName, Microsoft.AspNetCore.Routing.RouteValueDictionary routeValues)`
- `public static Microsoft.AspNetCore.Http.HttpResults.AcceptedAtRoute<TValue> AcceptedAtRoute<TValue>(TValue value, System.String routeName, System.Object routeValues)`
- `public static Microsoft.AspNetCore.Http.HttpResults.AcceptedAtRoute<TValue> AcceptedAtRoute<TValue>(TValue value, System.String routeName, Microsoft.AspNetCore.Routing.RouteValueDictionary routeValues)`
- `public static Microsoft.AspNetCore.Http.HttpResults.BadRequest BadRequest()`
- `public static Microsoft.AspNetCore.Http.HttpResults.BadRequest<TValue> BadRequest<TValue>(TValue error)`
- `public static Microsoft.AspNetCore.Http.HttpResults.FileContentHttpResult Bytes(System.Byte[] contents, System.String contentType, System.String fileDownloadName, System.Boolean enableRangeProcessing, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag)`
- `public static Microsoft.AspNetCore.Http.HttpResults.FileContentHttpResult Bytes(System.ReadOnlyMemory<System.Byte> contents, System.String contentType, System.String fileDownloadName, System.Boolean enableRangeProcessing, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag)`
- `public static Microsoft.AspNetCore.Http.HttpResults.ChallengeHttpResult Challenge(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, System.Collections.Generic.IList<System.String> authenticationSchemes)`
- `public static Microsoft.AspNetCore.Http.HttpResults.Conflict Conflict()`
- `public static Microsoft.AspNetCore.Http.HttpResults.Conflict<TValue> Conflict<TValue>(TValue error)`
- `public static Microsoft.AspNetCore.Http.HttpResults.ContentHttpResult Content(System.String content, Microsoft.Net.Http.Headers.MediaTypeHeaderValue contentType)`
- `public static Microsoft.AspNetCore.Http.HttpResults.ContentHttpResult Content(System.String content, System.String contentType, System.Text.Encoding contentEncoding)`
- `public static Microsoft.AspNetCore.Http.HttpResults.ContentHttpResult Content(System.String content, System.String contentType, System.Text.Encoding contentEncoding, System.Int32? statusCode)`
- `public static Microsoft.AspNetCore.Http.HttpResults.Created Created()`
- `public static Microsoft.AspNetCore.Http.HttpResults.Created Created(System.String uri)`
- `public static Microsoft.AspNetCore.Http.HttpResults.Created Created(System.Uri uri)`
- `public static Microsoft.AspNetCore.Http.HttpResults.Created<TValue> Created<TValue>(System.String uri, TValue value)`
- `public static Microsoft.AspNetCore.Http.HttpResults.Created<TValue> Created<TValue>(System.Uri uri, TValue value)`
- `public static Microsoft.AspNetCore.Http.HttpResults.CreatedAtRoute CreatedAtRoute(System.String routeName, System.Object routeValues)`
- `public static Microsoft.AspNetCore.Http.HttpResults.CreatedAtRoute CreatedAtRoute(System.String routeName, Microsoft.AspNetCore.Routing.RouteValueDictionary routeValues)`
- `public static Microsoft.AspNetCore.Http.HttpResults.CreatedAtRoute<TValue> CreatedAtRoute<TValue>(TValue value, System.String routeName, System.Object routeValues)`
- `public static Microsoft.AspNetCore.Http.HttpResults.CreatedAtRoute<TValue> CreatedAtRoute<TValue>(TValue value, System.String routeName, Microsoft.AspNetCore.Routing.RouteValueDictionary routeValues)`
- `public static Microsoft.AspNetCore.Http.HttpResults.FileContentHttpResult File(System.Byte[] fileContents, System.String contentType, System.String fileDownloadName, System.Boolean enableRangeProcessing, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag)`
- `public static Microsoft.AspNetCore.Http.HttpResults.FileStreamHttpResult File(System.IO.Stream fileStream, System.String contentType, System.String fileDownloadName, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag, System.Boolean enableRangeProcessing)`
- `public static Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult Forbid(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, System.Collections.Generic.IList<System.String> authenticationSchemes)`
- `public static Microsoft.AspNetCore.Http.HttpResults.InternalServerError InternalServerError()`
- `public static Microsoft.AspNetCore.Http.HttpResults.InternalServerError<TValue> InternalServerError<TValue>(TValue error)`
- `public static Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<TValue> Json<TValue>(TValue data, System.Text.Json.JsonSerializerOptions options, System.String contentType, System.Int32? statusCode)`
- `public static Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<TValue> Json<TValue>(TValue data, System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> jsonTypeInfo, System.String contentType, System.Int32? statusCode)`
- `public static Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<TValue> Json<TValue>(TValue data, System.Text.Json.Serialization.JsonSerializerContext context, System.String contentType, System.Int32? statusCode)`
- `public static Microsoft.AspNetCore.Http.HttpResults.RedirectHttpResult LocalRedirect(System.String localUrl, System.Boolean permanent, System.Boolean preserveMethod)`
- `public static Microsoft.AspNetCore.Http.HttpResults.NoContent NoContent()`
- `public static Microsoft.AspNetCore.Http.HttpResults.NotFound NotFound()`
- `public static Microsoft.AspNetCore.Http.HttpResults.NotFound<TValue> NotFound<TValue>(TValue value)`
- `public static Microsoft.AspNetCore.Http.HttpResults.Ok Ok()`
- `public static Microsoft.AspNetCore.Http.HttpResults.Ok<TValue> Ok<TValue>(TValue value)`
- `public static Microsoft.AspNetCore.Http.HttpResults.PhysicalFileHttpResult PhysicalFile(System.String path, System.String contentType, System.String fileDownloadName, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag, System.Boolean enableRangeProcessing)`
- `public static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult Problem(Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails)`
- `public static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult Problem(System.String detail, System.String instance, System.Int32? statusCode, System.String title, System.String type, System.Collections.Generic.IDictionary<System.String, System.Object> extensions)`
- `public static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult Problem(System.String detail, System.String instance, System.Int32? statusCode, System.String title, System.String type, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String, System.Object>> extensions)`
- `public static Microsoft.AspNetCore.Http.HttpResults.RedirectHttpResult Redirect(System.String url, System.Boolean permanent, System.Boolean preserveMethod)`
- `public static Microsoft.AspNetCore.Http.HttpResults.RedirectToRouteHttpResult RedirectToRoute(System.String routeName, System.Object routeValues, System.Boolean permanent, System.Boolean preserveMethod, System.String fragment)`
- `public static Microsoft.AspNetCore.Http.HttpResults.RedirectToRouteHttpResult RedirectToRoute(System.String routeName, Microsoft.AspNetCore.Routing.RouteValueDictionary routeValues, System.Boolean permanent, System.Boolean preserveMethod, System.String fragment)`
- `public static Microsoft.AspNetCore.Http.HttpResults.ServerSentEventsResult<T> ServerSentEvents<T>(System.Collections.Generic.IAsyncEnumerable<System.Net.ServerSentEvents.SseItem<T>> values)`
- `public static Microsoft.AspNetCore.Http.HttpResults.ServerSentEventsResult<System.String> ServerSentEvents(System.Collections.Generic.IAsyncEnumerable<System.String> values, System.String eventType)`
- `public static Microsoft.AspNetCore.Http.HttpResults.ServerSentEventsResult<T> ServerSentEvents<T>(System.Collections.Generic.IAsyncEnumerable<T> values, System.String eventType)`
- `public static Microsoft.AspNetCore.Http.HttpResults.SignInHttpResult SignIn(System.Security.Claims.ClaimsPrincipal principal, Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, System.String authenticationScheme)`
- `public static Microsoft.AspNetCore.Http.HttpResults.SignOutHttpResult SignOut(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties, System.Collections.Generic.IList<System.String> authenticationSchemes)`
- `public static Microsoft.AspNetCore.Http.HttpResults.StatusCodeHttpResult StatusCode(System.Int32 statusCode)`
- `public static Microsoft.AspNetCore.Http.HttpResults.PushStreamHttpResult Stream(System.Func<System.IO.Stream, System.Threading.Tasks.Task> streamWriterCallback, System.String contentType, System.String fileDownloadName, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag)`
- `public static Microsoft.AspNetCore.Http.HttpResults.FileStreamHttpResult Stream(System.IO.Stream stream, System.String contentType, System.String fileDownloadName, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag, System.Boolean enableRangeProcessing)`
- `public static Microsoft.AspNetCore.Http.HttpResults.FileStreamHttpResult Stream(System.IO.Pipelines.PipeReader pipeReader, System.String contentType, System.String fileDownloadName, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag, System.Boolean enableRangeProcessing)`
- `public static Microsoft.AspNetCore.Http.HttpResults.ContentHttpResult Text(System.String content, System.String contentType, System.Text.Encoding contentEncoding)`
- `public static Microsoft.AspNetCore.Http.HttpResults.Utf8ContentHttpResult Text(System.ReadOnlySpan<System.Byte> utf8Content, System.String contentType, System.Int32? statusCode)`
- `public static Microsoft.AspNetCore.Http.HttpResults.ContentHttpResult Text(System.String content, System.String contentType, System.Text.Encoding contentEncoding, System.Int32? statusCode)`
- `public static Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult Unauthorized()`
- `public static Microsoft.AspNetCore.Http.HttpResults.UnprocessableEntity UnprocessableEntity()`
- `public static Microsoft.AspNetCore.Http.HttpResults.UnprocessableEntity<TValue> UnprocessableEntity<TValue>(TValue error)`
- `public static Microsoft.AspNetCore.Http.HttpResults.ValidationProblem ValidationProblem(System.Collections.Generic.IDictionary<System.String, System.String[]> errors, System.String detail, System.String instance, System.String title, System.String type, System.Collections.Generic.IDictionary<System.String, System.Object> extensions)`
- `public static Microsoft.AspNetCore.Http.HttpResults.ValidationProblem ValidationProblem(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String, System.String[]>> errors, System.String detail, System.String instance, System.String title, System.String type, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String, System.Object>> extensions)`
- `public static Microsoft.AspNetCore.Http.HttpResults.VirtualFileHttpResult VirtualFile(System.String path, System.String contentType, System.String fileDownloadName, System.DateTimeOffset? lastModified, Microsoft.Net.Http.Headers.EntityTagHeaderValue entityTag, System.Boolean enableRangeProcessing)`

## Microsoft.AspNetCore.Mvc

### ProblemDetails (class)

- `public ProblemDetails()`
- `public System.String Detail { get; set; }`
- `public System.Collections.Generic.IDictionary<System.String, System.Object> Extensions { get; set; }`
- `public System.String Instance { get; set; }`
- `public System.Int32? Status { get; set; }`
- `public System.String Title { get; set; }`
- `public System.String Type { get; set; }`

