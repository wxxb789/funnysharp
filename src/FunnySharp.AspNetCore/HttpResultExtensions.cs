using FunnySharp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FunnySharp.AspNetCore;

/// <summary>
/// Maps FunnySharp outcome types to ASP.NET Core HTTP results.
/// </summary>
public static class HttpResultExtensions
{
    /// <summary>
    /// Maps an option to a successful result or a mapped problem result.
    /// </summary>
    /// <typeparam name="T">The option value type.</typeparam>
    /// <param name="option">The option to map.</param>
    /// <param name="none">Maps absence to a problem with a status.</param>
    /// <param name="some">Optionally maps a present value; the default produces <see cref="Results.Ok(object?)"/>.</param>
    /// <returns>The mapped HTTP result.</returns>
    public static IResult ToHttpResult<T>(
        this Option<T> option,
        Func<ProblemDetails> none,
        Func<T, IResult>? some = null)
    {
        ArgumentNullException.ThrowIfNull(none);
        return ToHttpResultCore(option, none, some);
    }

    /// <summary>
    /// Maps a result to a successful result or a mapped problem result.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="result">The result to map.</param>
    /// <param name="failure">Maps failure to a problem with a status.</param>
    /// <param name="success">Optionally maps a successful value; the default produces <see cref="Results.Ok(object?)"/>.</param>
    /// <returns>The mapped HTTP result.</returns>
    public static IResult ToHttpResult<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TError, ProblemDetails> failure,
        Func<TValue, IResult>? success = null)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return ToHttpResultCore(result, failure, success);
    }

    /// <summary>
    /// Maps a validation to a successful result or a mapped validation problem result.
    /// </summary>
    /// <typeparam name="TValue">The valid value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="validation">The validation to map.</param>
    /// <param name="invalid">Maps validation errors to a validation problem with a status.</param>
    /// <param name="valid">Optionally maps a valid value; the default produces <see cref="Results.Ok(object?)"/>.</param>
    /// <returns>The mapped HTTP result.</returns>
    public static IResult ToHttpResult<TValue, TError>(
        this Validation<TValue, TError> validation,
        Func<IReadOnlyList<TError>, HttpValidationProblemDetails> invalid,
        Func<TValue, IResult>? valid = null)
    {
        ArgumentNullException.ThrowIfNull(invalid);
        return ToHttpResultCore(validation, invalid, valid);
    }

    /// <summary>
    /// Asynchronously maps an option task to an HTTP result.
    /// </summary>
    /// <typeparam name="T">The option value type.</typeparam>
    /// <param name="option">The option task to map.</param>
    /// <param name="none">Maps absence to a problem with a status.</param>
    /// <param name="some">Optionally maps a present value.</param>
    /// <returns>A task that produces the mapped HTTP result.</returns>
    public static Task<IResult> ToHttpResultAsync<T>(
        this Task<Option<T>> option,
        Func<ProblemDetails> none,
        Func<T, IResult>? some = null)
    {
        ArgumentNullException.ThrowIfNull(option);
        ArgumentNullException.ThrowIfNull(none);
        return ToHttpResultAsyncCore(option, none, some);
    }

    /// <summary>
    /// Asynchronously maps a result task to an HTTP result.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="result">The result task to map.</param>
    /// <param name="failure">Maps failure to a problem with a status.</param>
    /// <param name="success">Optionally maps a successful value.</param>
    /// <returns>A task that produces the mapped HTTP result.</returns>
    public static Task<IResult> ToHttpResultAsync<TValue, TError>(
        this Task<Result<TValue, TError>> result,
        Func<TError, ProblemDetails> failure,
        Func<TValue, IResult>? success = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(failure);
        return ToHttpResultAsyncCore(result, failure, success);
    }

    /// <summary>
    /// Asynchronously maps a validation task to an HTTP result.
    /// </summary>
    /// <typeparam name="TValue">The valid value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="validation">The validation task to map.</param>
    /// <param name="invalid">Maps validation errors to a validation problem with a status.</param>
    /// <param name="valid">Optionally maps a valid value.</param>
    /// <returns>A task that produces the mapped HTTP result.</returns>
    public static Task<IResult> ToHttpResultAsync<TValue, TError>(
        this Task<Validation<TValue, TError>> validation,
        Func<IReadOnlyList<TError>, HttpValidationProblemDetails> invalid,
        Func<TValue, IResult>? valid = null)
    {
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(invalid);
        return ToHttpResultAsyncCore(validation, invalid, valid);
    }

    /// <summary>
    /// Asynchronously maps an option value task to an HTTP result.
    /// </summary>
    /// <typeparam name="T">The option value type.</typeparam>
    /// <param name="option">The option value task to map.</param>
    /// <param name="none">Maps absence to a problem with a status.</param>
    /// <param name="some">Optionally maps a present value.</param>
    /// <returns>A value task that produces the mapped HTTP result.</returns>
    public static ValueTask<IResult> ToHttpResultAsync<T>(
        this ValueTask<Option<T>> option,
        Func<ProblemDetails> none,
        Func<T, IResult>? some = null)
    {
        ArgumentNullException.ThrowIfNull(none);
        return ToHttpResultAsyncCore(option, none, some);
    }

    /// <summary>
    /// Asynchronously maps a result value task to an HTTP result.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="result">The result value task to map.</param>
    /// <param name="failure">Maps failure to a problem with a status.</param>
    /// <param name="success">Optionally maps a successful value.</param>
    /// <returns>A value task that produces the mapped HTTP result.</returns>
    public static ValueTask<IResult> ToHttpResultAsync<TValue, TError>(
        this ValueTask<Result<TValue, TError>> result,
        Func<TError, ProblemDetails> failure,
        Func<TValue, IResult>? success = null)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return ToHttpResultAsyncCore(result, failure, success);
    }

    /// <summary>
    /// Asynchronously maps a validation value task to an HTTP result.
    /// </summary>
    /// <typeparam name="TValue">The valid value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="validation">The validation value task to map.</param>
    /// <param name="invalid">Maps validation errors to a validation problem with a status.</param>
    /// <param name="valid">Optionally maps a valid value.</param>
    /// <returns>A value task that produces the mapped HTTP result.</returns>
    public static ValueTask<IResult> ToHttpResultAsync<TValue, TError>(
        this ValueTask<Validation<TValue, TError>> validation,
        Func<IReadOnlyList<TError>, HttpValidationProblemDetails> invalid,
        Func<TValue, IResult>? valid = null)
    {
        ArgumentNullException.ThrowIfNull(invalid);
        return ToHttpResultAsyncCore(validation, invalid, valid);
    }

    /// <summary>
    /// Runs an option-producing effect with the request cancellation token and maps its result.
    /// </summary>
    /// <typeparam name="T">The option value type.</typeparam>
    /// <param name="effect">The effect to run.</param>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="none">Maps absence to a problem with a status.</param>
    /// <param name="some">Optionally maps a present value.</param>
    /// <returns>A value task that produces the mapped HTTP result.</returns>
    public static ValueTask<IResult> ToHttpResultAsync<T>(
        this Effect<Option<T>> effect,
        HttpContext context,
        Func<ProblemDetails> none,
        Func<T, IResult>? some = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(none);
        return ToHttpResultAsyncCore(effect.RunAsync(context.RequestAborted), none, some);
    }

    /// <summary>
    /// Runs a result-producing effect with the request cancellation token and maps its result.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="effect">The effect to run.</param>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="failure">Maps failure to a problem with a status.</param>
    /// <param name="success">Optionally maps a successful value.</param>
    /// <returns>A value task that produces the mapped HTTP result.</returns>
    public static ValueTask<IResult> ToHttpResultAsync<TValue, TError>(
        this Effect<Result<TValue, TError>> effect,
        HttpContext context,
        Func<TError, ProblemDetails> failure,
        Func<TValue, IResult>? success = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(failure);
        return ToHttpResultAsyncCore(effect.RunAsync(context.RequestAborted), failure, success);
    }

    /// <summary>
    /// Runs a validation-producing effect with the request cancellation token and maps its result.
    /// </summary>
    /// <typeparam name="TValue">The valid value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="effect">The effect to run.</param>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="invalid">Maps validation errors to a validation problem with a status.</param>
    /// <param name="valid">Optionally maps a valid value.</param>
    /// <returns>A value task that produces the mapped HTTP result.</returns>
    public static ValueTask<IResult> ToHttpResultAsync<TValue, TError>(
        this Effect<Validation<TValue, TError>> effect,
        HttpContext context,
        Func<IReadOnlyList<TError>, HttpValidationProblemDetails> invalid,
        Func<TValue, IResult>? valid = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(invalid);
        return ToHttpResultAsyncCore(effect.RunAsync(context.RequestAborted), invalid, valid);
    }

    /// <summary>
    /// Runs an environment-dependent option effect with the request cancellation token and maps its result.
    /// </summary>
    /// <typeparam name="TEnvironment">The effect environment type.</typeparam>
    /// <typeparam name="T">The option value type.</typeparam>
    /// <param name="effect">The effect to run.</param>
    /// <param name="environment">The explicit environment supplied to the effect.</param>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="none">Maps absence to a problem with a status.</param>
    /// <param name="some">Optionally maps a present value.</param>
    /// <returns>A value task that produces the mapped HTTP result.</returns>
    public static ValueTask<IResult> ToHttpResultAsync<TEnvironment, T>(
        this Effect<TEnvironment, Option<T>> effect,
        TEnvironment environment,
        HttpContext context,
        Func<ProblemDetails> none,
        Func<T, IResult>? some = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(none);
        return ToHttpResultAsyncCore(effect.RunAsync(environment, context.RequestAborted), none, some);
    }

    /// <summary>
    /// Runs an environment-dependent result effect with the request cancellation token and maps its result.
    /// </summary>
    /// <typeparam name="TEnvironment">The effect environment type.</typeparam>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="effect">The effect to run.</param>
    /// <param name="environment">The explicit environment supplied to the effect.</param>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="failure">Maps failure to a problem with a status.</param>
    /// <param name="success">Optionally maps a successful value.</param>
    /// <returns>A value task that produces the mapped HTTP result.</returns>
    public static ValueTask<IResult> ToHttpResultAsync<TEnvironment, TValue, TError>(
        this Effect<TEnvironment, Result<TValue, TError>> effect,
        TEnvironment environment,
        HttpContext context,
        Func<TError, ProblemDetails> failure,
        Func<TValue, IResult>? success = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(failure);
        return ToHttpResultAsyncCore(effect.RunAsync(environment, context.RequestAborted), failure, success);
    }

    /// <summary>
    /// Runs an environment-dependent validation effect with the request cancellation token and maps its result.
    /// </summary>
    /// <typeparam name="TEnvironment">The effect environment type.</typeparam>
    /// <typeparam name="TValue">The valid value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="effect">The effect to run.</param>
    /// <param name="environment">The explicit environment supplied to the effect.</param>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="invalid">Maps validation errors to a validation problem with a status.</param>
    /// <param name="valid">Optionally maps a valid value.</param>
    /// <returns>A value task that produces the mapped HTTP result.</returns>
    public static ValueTask<IResult> ToHttpResultAsync<TEnvironment, TValue, TError>(
        this Effect<TEnvironment, Validation<TValue, TError>> effect,
        TEnvironment environment,
        HttpContext context,
        Func<IReadOnlyList<TError>, HttpValidationProblemDetails> invalid,
        Func<TValue, IResult>? valid = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(invalid);
        return ToHttpResultAsyncCore(effect.RunAsync(environment, context.RequestAborted), invalid, valid);
    }

    private static IResult ToHttpResultCore<T>(
        Option<T> option,
        Func<ProblemDetails> none,
        Func<T, IResult>? some)
    {
        return option.TryGetValue(out var value)
            ? ToSuccessResult(value, some)
            : ToProblemResult(none());
    }

    private static IResult ToHttpResultCore<TValue, TError>(
        Result<TValue, TError> result,
        Func<TError, ProblemDetails> failure,
        Func<TValue, IResult>? success)
    {
        if (result.TryGetValue(out var value))
        {
            return ToSuccessResult<TValue>(value!, success);
        }

        _ = result.TryGetError(out var error);
        return ToProblemResult(failure(error!));
    }

    private static IResult ToHttpResultCore<TValue, TError>(
        Validation<TValue, TError> validation,
        Func<IReadOnlyList<TError>, HttpValidationProblemDetails> invalid,
        Func<TValue, IResult>? valid)
    {
        if (validation.TryGetValue(out var value))
        {
            return ToSuccessResult<TValue>(value!, valid);
        }

        _ = validation.TryGetErrors(out var errors);
        return ToValidationProblemResult(invalid(errors!));
    }

    private static async Task<IResult> ToHttpResultAsyncCore<T>(
        Task<Option<T>> option,
        Func<ProblemDetails> none,
        Func<T, IResult>? some) =>
        ToHttpResultCore(await option.ConfigureAwait(false), none, some);

    private static async Task<IResult> ToHttpResultAsyncCore<TValue, TError>(
        Task<Result<TValue, TError>> result,
        Func<TError, ProblemDetails> failure,
        Func<TValue, IResult>? success) =>
        ToHttpResultCore(await result.ConfigureAwait(false), failure, success);

    private static async Task<IResult> ToHttpResultAsyncCore<TValue, TError>(
        Task<Validation<TValue, TError>> validation,
        Func<IReadOnlyList<TError>, HttpValidationProblemDetails> invalid,
        Func<TValue, IResult>? valid) =>
        ToHttpResultCore(await validation.ConfigureAwait(false), invalid, valid);

    private static async ValueTask<IResult> ToHttpResultAsyncCore<T>(
        ValueTask<Option<T>> option,
        Func<ProblemDetails> none,
        Func<T, IResult>? some) =>
        ToHttpResultCore(await option.ConfigureAwait(false), none, some);

    private static async ValueTask<IResult> ToHttpResultAsyncCore<TValue, TError>(
        ValueTask<Result<TValue, TError>> result,
        Func<TError, ProblemDetails> failure,
        Func<TValue, IResult>? success) =>
        ToHttpResultCore(await result.ConfigureAwait(false), failure, success);

    private static async ValueTask<IResult> ToHttpResultAsyncCore<TValue, TError>(
        ValueTask<Validation<TValue, TError>> validation,
        Func<IReadOnlyList<TError>, HttpValidationProblemDetails> invalid,
        Func<TValue, IResult>? valid) =>
        ToHttpResultCore(await validation.ConfigureAwait(false), invalid, valid);

    private static IResult ToSuccessResult<T>(T value, Func<T, IResult>? mapper) =>
        mapper is null ? Results.Ok(value) : mapper(value) ?? throw new InvalidOperationException("The success mapper returned null.");

    private static IResult ToProblemResult(ProblemDetails problem)
    {
        ValidateProblem(problem);
        return Results.Problem(problem);
    }

    private static IResult ToValidationProblemResult(HttpValidationProblemDetails problem)
    {
        ValidateProblem(problem);
        return Results.ValidationProblem(
            problem.Errors,
            problem.Detail,
            problem.Instance,
            problem.Status,
            problem.Title,
            problem.Type,
            problem.Extensions);
    }

    private static void ValidateProblem(ProblemDetails? problem)
    {
        if (problem is null || problem.Status is null)
        {
            throw new InvalidOperationException("Problem mappers must return a problem with a status.");
        }
    }
}
