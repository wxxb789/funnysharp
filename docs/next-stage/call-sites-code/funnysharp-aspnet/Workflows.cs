using FunnySharp;
using FunnySharp.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CallSites.FunnySharpAspNet;

// W11: minimal API endpoint mapping. Both variants compile against the same domain.
public static class W11Http
{
    // --- Straightforward idiomatic C# ---

    public static IResult GetCustomerIdiomatic(
        IReadOnlyDictionary<string, Customer> customers,
        string customerId)
    {
        if (!customers.TryGetValue(customerId, out var customer))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "customer-not-found",
                detail: $"No customer '{customerId}' exists.");
        }

        return Results.Ok(customer);
    }

    public static async Task<IResult> CreateOrderIdiomaticAsync(
        OrderRequest request,
        IOrderService service,
        HttpContext context)
    {
        if (request.Total is < 0 or > 100_000)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "amount-out-of-range",
                detail: $"Total {request.Total} must be between 0 and 100000.");
        }

        var order = await service.CreateAsync(request, context.RequestAborted);
        return Results.Created($"/orders/{order.Id}", order);
    }

    public static IResult SignUpIdiomatic(SignupForm form)
    {
        var errors = new Dictionary<string, string[]>();
        if (!form.Email.Contains('@'))
        {
            errors["email"] = ["email must contain '@'"];
        }

        if (form.Password.Length < 12)
        {
            errors["password"] = ["password must have at least 12 characters"];
        }

        return errors.Count == 0
            ? Results.Created("/signup", form)
            : Results.ValidationProblem(errors);
    }

    // --- FunnySharp.AspNetCore ---

    public static IResult GetCustomer(
        IReadOnlyDictionary<string, Customer> customers,
        string customerId) =>
        customers.GetOption(customerId).ToHttpResult(
            () => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "customer-not-found",
                Detail = $"No customer '{customerId}' exists.",
            });

    public static IResult CreateOrder(Result<Order, OrderError> result) =>
        result.ToHttpResult(
            error => new ProblemDetails
            {
                Status = error.Status,
                Title = error.Title,
                Detail = error.Detail,
            },
            order => Results.Created($"/orders/{order.Id}", order));

    public static IResult SignUp(Validation<SignupForm, string> validation) =>
        validation.ToHttpResult(
            errors => new HttpValidationProblemDetails(
                new Dictionary<string, string[]> { ["form"] = errors.ToArray() })
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "validation-failed",
            },
            form => Results.Created("/signup", form));

    public static ValueTask<IResult> CreateOrderFromEffectAsync(
        Effect<Result<Order, OrderError>> effect,
        HttpContext context) =>
        effect.ToHttpResultAsync(
            context,
            error => new ProblemDetails
            {
                Status = error.Status,
                Title = error.Title,
                Detail = error.Detail,
            },
            order => Results.Created($"/orders/{order.Id}", order));
}
