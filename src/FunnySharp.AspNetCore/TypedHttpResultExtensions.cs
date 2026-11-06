using FunnySharp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FunnySharp.AspNetCore;

/// <summary>
/// Maps FunnySharp outcome types to caller-selected ASP.NET Core typed results.
/// </summary>
/// <remarks>
/// The returned <see cref="Results{TResult1, TResult2}"/> union keeps the selected result types in
/// the endpoint's static return shape, so ASP.NET Core can derive response metadata from them.
/// Mappers own status, body, headers, and error disclosure. Dynamic results such as
/// <see cref="ProblemHttpResult"/> contribute no response metadata; declare their statuses with
/// endpoint metadata or select a status-specific typed result.
/// </remarks>
public static class TypedHttpResultExtensions
{
    /// <summary>
    /// Maps an option to a typed <see cref="Ok{TValue}"/> or a caller-selected absence result.
    /// </summary>
    /// <typeparam name="T">The option value type.</typeparam>
    /// <typeparam name="TNone">The typed result selected for absence.</typeparam>
    /// <param name="option">The option to map.</param>
    /// <param name="none">Maps absence to a caller-selected typed result.</param>
    /// <returns>A typed result union containing the presence and absence result types.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="none"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The selected mapper returns null.</exception>
    /// <remarks>Only the selected outcome mapper runs; mapper exceptions propagate unchanged.</remarks>
    public static Results<Ok<T>, TNone> ToHttpResults<T, TNone>(
        this Option<T> option,
        Func<TNone> none)
        where TNone : IResult
    {
        ArgumentNullException.ThrowIfNull(none);
        return ToHttpResultsCore(option, none, static value => TypedResults.Ok(value));
    }

    /// <summary>
    /// Maps an option to a caller-selected presence result or absence result.
    /// </summary>
    /// <typeparam name="T">The option value type.</typeparam>
    /// <typeparam name="TSome">The typed result selected for a present value.</typeparam>
    /// <typeparam name="TNone">The typed result selected for absence.</typeparam>
    /// <param name="option">The option to map.</param>
    /// <param name="none">Maps absence to a caller-selected typed result.</param>
    /// <param name="some">Maps a present value to a caller-selected typed result.</param>
    /// <returns>A typed result union containing the selected result types.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="none"/> or <paramref name="some"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The selected mapper returns null.</exception>
    /// <remarks>Only the selected outcome mapper runs; mapper exceptions propagate unchanged.</remarks>
    public static Results<TSome, TNone> ToHttpResults<T, TSome, TNone>(
        this Option<T> option,
        Func<TNone> none,
        Func<T, TSome> some)
        where TSome : IResult
        where TNone : IResult
    {
        ArgumentNullException.ThrowIfNull(none);
        ArgumentNullException.ThrowIfNull(some);
        return ToHttpResultsCore(option, none, some);
    }

    /// <summary>
    /// Maps a result to a typed <see cref="Ok{TValue}"/> or a caller-selected failure result.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <typeparam name="TFailure">The typed result selected for failure.</typeparam>
    /// <param name="result">The result to map.</param>
    /// <param name="failure">Maps failure to a caller-selected typed result.</param>
    /// <returns>A typed result union containing the success and failure result types.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="failure"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The selected mapper returns null, or the outcome is uninitialized.</exception>
    /// <remarks>Only the selected outcome mapper runs; mapper exceptions propagate unchanged.</remarks>
    public static Results<Ok<TValue>, TFailure> ToHttpResults<TValue, TError, TFailure>(
        this Result<TValue, TError> result,
        Func<TError, TFailure> failure)
        where TFailure : IResult
    {
        ArgumentNullException.ThrowIfNull(failure);
        return ToHttpResultsCore(result, failure, static value => TypedResults.Ok(value));
    }

    /// <summary>
    /// Maps a result to caller-selected success and failure results.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <typeparam name="TSuccess">The typed result selected for success.</typeparam>
    /// <typeparam name="TFailure">The typed result selected for failure.</typeparam>
    /// <param name="result">The result to map.</param>
    /// <param name="failure">Maps failure to a caller-selected typed result.</param>
    /// <param name="success">Maps a successful value to a caller-selected typed result.</param>
    /// <returns>A typed result union containing the selected result types.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="failure"/> or <paramref name="success"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The selected mapper returns null, or the outcome is uninitialized.</exception>
    /// <remarks>Only the selected outcome mapper runs; mapper exceptions propagate unchanged.</remarks>
    public static Results<TSuccess, TFailure> ToHttpResults<TValue, TError, TSuccess, TFailure>(
        this Result<TValue, TError> result,
        Func<TError, TFailure> failure,
        Func<TValue, TSuccess> success)
        where TSuccess : IResult
        where TFailure : IResult
    {
        ArgumentNullException.ThrowIfNull(failure);
        ArgumentNullException.ThrowIfNull(success);
        return ToHttpResultsCore(result, failure, success);
    }

    /// <summary>
    /// Maps a validation to a typed <see cref="Ok{TValue}"/> or a caller-selected invalid result.
    /// </summary>
    /// <typeparam name="TValue">The valid value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <typeparam name="TInvalid">The typed result selected for invalid errors.</typeparam>
    /// <param name="validation">The validation to map.</param>
    /// <param name="invalid">Maps the complete ordered error list to a caller-selected typed result.</param>
    /// <returns>A typed result union containing the valid and invalid result types.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="invalid"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The selected mapper returns null, or the outcome is uninitialized.</exception>
    /// <remarks>Only the selected outcome mapper runs; mapper exceptions propagate unchanged.</remarks>
    public static Results<Ok<TValue>, TInvalid> ToHttpResults<TValue, TError, TInvalid>(
        this Validation<TValue, TError> validation,
        Func<IReadOnlyList<TError>, TInvalid> invalid)
        where TInvalid : IResult
    {
        ArgumentNullException.ThrowIfNull(invalid);
        return ToHttpResultsCore(validation, invalid, static value => TypedResults.Ok(value));
    }

    /// <summary>
    /// Maps a validation to caller-selected valid and invalid results.
    /// </summary>
    /// <typeparam name="TValue">The valid value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <typeparam name="TValid">The typed result selected for a valid value.</typeparam>
    /// <typeparam name="TInvalid">The typed result selected for invalid errors.</typeparam>
    /// <param name="validation">The validation to map.</param>
    /// <param name="invalid">Maps the complete ordered error list to a caller-selected typed result.</param>
    /// <param name="valid">Maps a valid value to a caller-selected typed result.</param>
    /// <returns>A typed result union containing the selected result types.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="invalid"/> or <paramref name="valid"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The selected mapper returns null, or the outcome is uninitialized.</exception>
    /// <remarks>Only the selected outcome mapper runs; mapper exceptions propagate unchanged.</remarks>
    public static Results<TValid, TInvalid> ToHttpResults<TValue, TError, TValid, TInvalid>(
        this Validation<TValue, TError> validation,
        Func<IReadOnlyList<TError>, TInvalid> invalid,
        Func<TValue, TValid> valid)
        where TValid : IResult
        where TInvalid : IResult
    {
        ArgumentNullException.ThrowIfNull(invalid);
        ArgumentNullException.ThrowIfNull(valid);
        return ToHttpResultsCore(validation, invalid, valid);
    }

    /// <summary>
    /// Maps a unit result to a typed <see cref="NoContent"/> or a caller-selected failure result.
    /// </summary>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <typeparam name="TFailure">The typed result selected for failure.</typeparam>
    /// <param name="result">The unit result to map.</param>
    /// <param name="failure">Maps failure to a caller-selected typed result.</param>
    /// <returns>A typed result union containing the success and failure result types.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="failure"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The selected mapper returns null, or the outcome is uninitialized.</exception>
    /// <remarks>Only the selected outcome mapper runs; mapper exceptions propagate unchanged.</remarks>
    public static Results<NoContent, TFailure> ToHttpResults<TError, TFailure>(
        this UnitResult<TError> result,
        Func<TError, TFailure> failure)
        where TFailure : IResult
    {
        ArgumentNullException.ThrowIfNull(failure);
        return ToHttpResultsCore(result, failure, static () => TypedResults.NoContent());
    }

    /// <summary>
    /// Maps a unit result to caller-selected success and failure results.
    /// </summary>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <typeparam name="TSuccess">The typed result selected for success.</typeparam>
    /// <typeparam name="TFailure">The typed result selected for failure.</typeparam>
    /// <param name="result">The unit result to map.</param>
    /// <param name="failure">Maps failure to a caller-selected typed result.</param>
    /// <param name="success">Maps success to a caller-selected typed result.</param>
    /// <returns>A typed result union containing the selected result types.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="failure"/> or <paramref name="success"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The selected mapper returns null, or the outcome is uninitialized.</exception>
    /// <remarks>Only the selected outcome mapper runs; mapper exceptions propagate unchanged.</remarks>
    public static Results<TSuccess, TFailure> ToHttpResults<TError, TSuccess, TFailure>(
        this UnitResult<TError> result,
        Func<TError, TFailure> failure,
        Func<TSuccess> success)
        where TSuccess : IResult
        where TFailure : IResult
    {
        ArgumentNullException.ThrowIfNull(failure);
        ArgumentNullException.ThrowIfNull(success);
        return ToHttpResultsCore(result, failure, success);
    }

    /// <summary>
    /// Asynchronously maps an option task to typed HTTP results.
    /// </summary>
    /// <typeparam name="T">The option value type.</typeparam>
    /// <typeparam name="TNone">The typed result selected for absence.</typeparam>
    /// <param name="option">The option task to map.</param>
    /// <param name="none">Maps absence to a caller-selected typed result.</param>
    /// <returns>A task that produces the typed result union.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="option"/> or <paramref name="none"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The selected mapper returns null.</exception>
    /// <remarks>
    /// The input is awaited once; faults and cancellation propagate unchanged, and only the selected
    /// outcome mapper runs.
    /// </remarks>
    public static Task<Results<Ok<T>, TNone>> ToHttpResultsAsync<T, TNone>(
        this Task<Option<T>> option,
        Func<TNone> none)
        where TNone : IResult
    {
        ArgumentNullException.ThrowIfNull(option);
        ArgumentNullException.ThrowIfNull(none);
        return ToHttpResultsAsyncCore(option, none, static value => TypedResults.Ok(value));
    }

    /// <summary>
    /// Asynchronously maps an option task to caller-selected typed results.
    /// </summary>
    /// <typeparam name="T">The option value type.</typeparam>
    /// <typeparam name="TSome">The typed result selected for a present value.</typeparam>
    /// <typeparam name="TNone">The typed result selected for absence.</typeparam>
    /// <param name="option">The option task to map.</param>
    /// <param name="none">Maps absence to a caller-selected typed result.</param>
    /// <param name="some">Maps a present value to a caller-selected typed result.</param>
    /// <returns>A task that produces the typed result union.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="option"/>, <paramref name="none"/>, or <paramref name="some"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The selected mapper returns null.</exception>
    /// <remarks>
    /// The input is awaited once; faults and cancellation propagate unchanged, and only the selected
    /// outcome mapper runs.
    /// </remarks>
    public static Task<Results<TSome, TNone>> ToHttpResultsAsync<T, TSome, TNone>(
        this Task<Option<T>> option,
        Func<TNone> none,
        Func<T, TSome> some)
        where TSome : IResult
        where TNone : IResult
    {
        ArgumentNullException.ThrowIfNull(option);
        ArgumentNullException.ThrowIfNull(none);
        ArgumentNullException.ThrowIfNull(some);
        return ToHttpResultsAsyncCore(option, none, some);
    }

    /// <summary>
    /// Asynchronously maps a result task to typed HTTP results.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <typeparam name="TFailure">The typed result selected for failure.</typeparam>
    /// <param name="result">The result task to map.</param>
    /// <param name="failure">Maps failure to a caller-selected typed result.</param>
    /// <returns>A task that produces the typed result union.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="failure"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The selected mapper returns null, or the outcome is uninitialized.</exception>
    /// <remarks>
    /// The input is awaited once; faults and cancellation propagate unchanged, and only the selected
    /// outcome mapper runs.
    /// </remarks>
    public static Task<Results<Ok<TValue>, TFailure>> ToHttpResultsAsync<TValue, TError, TFailure>(
        this Task<Result<TValue, TError>> result,
        Func<TError, TFailure> failure)
        where TFailure : IResult
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(failure);
        return ToHttpResultsAsyncCore(result, failure, static value => TypedResults.Ok(value));
    }

    /// <summary>
    /// Asynchronously maps a result task to caller-selected typed results.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <typeparam name="TSuccess">The typed result selected for success.</typeparam>
    /// <typeparam name="TFailure">The typed result selected for failure.</typeparam>
    /// <param name="result">The result task to map.</param>
    /// <param name="failure">Maps failure to a caller-selected typed result.</param>
    /// <param name="success">Maps a successful value to a caller-selected typed result.</param>
    /// <returns>A task that produces the typed result union.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/>, <paramref name="failure"/>, or <paramref name="success"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The selected mapper returns null, or the outcome is uninitialized.</exception>
    /// <remarks>
    /// The input is awaited once; faults and cancellation propagate unchanged, and only the selected
    /// outcome mapper runs.
    /// </remarks>
    public static Task<Results<TSuccess, TFailure>> ToHttpResultsAsync<TValue, TError, TSuccess, TFailure>(
        this Task<Result<TValue, TError>> result,
        Func<TError, TFailure> failure,
        Func<TValue, TSuccess> success)
        where TSuccess : IResult
        where TFailure : IResult
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(failure);
        ArgumentNullException.ThrowIfNull(success);
        return ToHttpResultsAsyncCore(result, failure, success);
    }

    /// <summary>
    /// Asynchronously maps a validation task to typed HTTP results.
    /// </summary>
    /// <typeparam name="TValue">The valid value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <typeparam name="TInvalid">The typed result selected for invalid errors.</typeparam>
    /// <param name="validation">The validation task to map.</param>
    /// <param name="invalid">Maps the complete ordered error list to a caller-selected typed result.</param>
    /// <returns>A task that produces the typed result union.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="validation"/> or <paramref name="invalid"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The selected mapper returns null, or the outcome is uninitialized.</exception>
    /// <remarks>
    /// The input is awaited once; faults and cancellation propagate unchanged, and only the selected
    /// outcome mapper runs.
    /// </remarks>
    public static Task<Results<Ok<TValue>, TInvalid>> ToHttpResultsAsync<TValue, TError, TInvalid>(
        this Task<Validation<TValue, TError>> validation,
        Func<IReadOnlyList<TError>, TInvalid> invalid)
        where TInvalid : IResult
    {
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(invalid);
        return ToHttpResultsAsyncCore(validation, invalid, static value => TypedResults.Ok(value));
    }

    /// <summary>
    /// Asynchronously maps a validation task to caller-selected typed results.
    /// </summary>
    /// <typeparam name="TValue">The valid value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <typeparam name="TValid">The typed result selected for a valid value.</typeparam>
    /// <typeparam name="TInvalid">The typed result selected for invalid errors.</typeparam>
    /// <param name="validation">The validation task to map.</param>
    /// <param name="invalid">Maps the complete ordered error list to a caller-selected typed result.</param>
    /// <param name="valid">Maps a valid value to a caller-selected typed result.</param>
    /// <returns>A task that produces the typed result union.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="validation"/>, <paramref name="invalid"/>, or <paramref name="valid"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The selected mapper returns null, or the outcome is uninitialized.</exception>
    /// <remarks>
    /// The input is awaited once; faults and cancellation propagate unchanged, and only the selected
    /// outcome mapper runs.
    /// </remarks>
    public static Task<Results<TValid, TInvalid>> ToHttpResultsAsync<TValue, TError, TValid, TInvalid>(
        this Task<Validation<TValue, TError>> validation,
        Func<IReadOnlyList<TError>, TInvalid> invalid,
        Func<TValue, TValid> valid)
        where TValid : IResult
        where TInvalid : IResult
    {
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(invalid);
        ArgumentNullException.ThrowIfNull(valid);
        return ToHttpResultsAsyncCore(validation, invalid, valid);
    }

    /// <summary>
    /// Asynchronously maps a unit result task to typed HTTP results.
    /// </summary>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <typeparam name="TFailure">The typed result selected for failure.</typeparam>
    /// <param name="result">The unit result task to map.</param>
    /// <param name="failure">Maps failure to a caller-selected typed result.</param>
    /// <returns>A task that produces the typed result union.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> or <paramref name="failure"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The selected mapper returns null, or the outcome is uninitialized.</exception>
    /// <remarks>
    /// The input is awaited once; faults and cancellation propagate unchanged, and only the selected
    /// outcome mapper runs.
    /// </remarks>
    public static Task<Results<NoContent, TFailure>> ToHttpResultsAsync<TError, TFailure>(
        this Task<UnitResult<TError>> result,
        Func<TError, TFailure> failure)
        where TFailure : IResult
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(failure);
        return ToHttpResultsAsyncCore(result, failure, static () => TypedResults.NoContent());
    }

    /// <summary>
    /// Asynchronously maps a unit result task to caller-selected typed results.
    /// </summary>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <typeparam name="TSuccess">The typed result selected for success.</typeparam>
    /// <typeparam name="TFailure">The typed result selected for failure.</typeparam>
    /// <param name="result">The unit result task to map.</param>
    /// <param name="failure">Maps failure to a caller-selected typed result.</param>
    /// <param name="success">Maps success to a caller-selected typed result.</param>
    /// <returns>A task that produces the typed result union.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/>, <paramref name="failure"/>, or <paramref name="success"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The selected mapper returns null, or the outcome is uninitialized.</exception>
    /// <remarks>
    /// The input is awaited once; faults and cancellation propagate unchanged, and only the selected
    /// outcome mapper runs.
    /// </remarks>
    public static Task<Results<TSuccess, TFailure>> ToHttpResultsAsync<TError, TSuccess, TFailure>(
        this Task<UnitResult<TError>> result,
        Func<TError, TFailure> failure,
        Func<TSuccess> success)
        where TSuccess : IResult
        where TFailure : IResult
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(failure);
        ArgumentNullException.ThrowIfNull(success);
        return ToHttpResultsAsyncCore(result, failure, success);
    }

    /// <summary>
    /// Asynchronously maps an option value task to typed HTTP results.
    /// </summary>
    /// <typeparam name="T">The option value type.</typeparam>
    /// <typeparam name="TNone">The typed result selected for absence.</typeparam>
    /// <param name="option">The option value task to map.</param>
    /// <param name="none">Maps absence to a caller-selected typed result.</param>
    /// <returns>A value task that produces the typed result union.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="none"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The selected mapper returns null.</exception>
    /// <remarks>
    /// The input is awaited once; faults and cancellation propagate unchanged, and only the selected
    /// outcome mapper runs.
    /// </remarks>
    public static ValueTask<Results<Ok<T>, TNone>> ToHttpResultsAsync<T, TNone>(
        this ValueTask<Option<T>> option,
        Func<TNone> none)
        where TNone : IResult
    {
        ArgumentNullException.ThrowIfNull(none);
        return ToHttpResultsAsyncCore(option, none, static value => TypedResults.Ok(value));
    }

    /// <summary>
    /// Asynchronously maps an option value task to caller-selected typed results.
    /// </summary>
    /// <typeparam name="T">The option value type.</typeparam>
    /// <typeparam name="TSome">The typed result selected for a present value.</typeparam>
    /// <typeparam name="TNone">The typed result selected for absence.</typeparam>
    /// <param name="option">The option value task to map.</param>
    /// <param name="none">Maps absence to a caller-selected typed result.</param>
    /// <param name="some">Maps a present value to a caller-selected typed result.</param>
    /// <returns>A value task that produces the typed result union.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="none"/> or <paramref name="some"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The selected mapper returns null.</exception>
    /// <remarks>
    /// The input is awaited once; faults and cancellation propagate unchanged, and only the selected
    /// outcome mapper runs.
    /// </remarks>
    public static ValueTask<Results<TSome, TNone>> ToHttpResultsAsync<T, TSome, TNone>(
        this ValueTask<Option<T>> option,
        Func<TNone> none,
        Func<T, TSome> some)
        where TSome : IResult
        where TNone : IResult
    {
        ArgumentNullException.ThrowIfNull(none);
        ArgumentNullException.ThrowIfNull(some);
        return ToHttpResultsAsyncCore(option, none, some);
    }

    /// <summary>
    /// Asynchronously maps a result value task to typed HTTP results.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <typeparam name="TFailure">The typed result selected for failure.</typeparam>
    /// <param name="result">The result value task to map.</param>
    /// <param name="failure">Maps failure to a caller-selected typed result.</param>
    /// <returns>A value task that produces the typed result union.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="failure"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The selected mapper returns null, or the outcome is uninitialized.</exception>
    /// <remarks>
    /// The input is awaited once; faults and cancellation propagate unchanged, and only the selected
    /// outcome mapper runs.
    /// </remarks>
    public static ValueTask<Results<Ok<TValue>, TFailure>> ToHttpResultsAsync<TValue, TError, TFailure>(
        this ValueTask<Result<TValue, TError>> result,
        Func<TError, TFailure> failure)
        where TFailure : IResult
    {
        ArgumentNullException.ThrowIfNull(failure);
        return ToHttpResultsAsyncCore(result, failure, static value => TypedResults.Ok(value));
    }

    /// <summary>
    /// Asynchronously maps a result value task to caller-selected typed results.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <typeparam name="TSuccess">The typed result selected for success.</typeparam>
    /// <typeparam name="TFailure">The typed result selected for failure.</typeparam>
    /// <param name="result">The result value task to map.</param>
    /// <param name="failure">Maps failure to a caller-selected typed result.</param>
    /// <param name="success">Maps a successful value to a caller-selected typed result.</param>
    /// <returns>A value task that produces the typed result union.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="failure"/> or <paramref name="success"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The selected mapper returns null, or the outcome is uninitialized.</exception>
    /// <remarks>
    /// The input is awaited once; faults and cancellation propagate unchanged, and only the selected
    /// outcome mapper runs.
    /// </remarks>
    public static ValueTask<Results<TSuccess, TFailure>> ToHttpResultsAsync<TValue, TError, TSuccess, TFailure>(
        this ValueTask<Result<TValue, TError>> result,
        Func<TError, TFailure> failure,
        Func<TValue, TSuccess> success)
        where TSuccess : IResult
        where TFailure : IResult
    {
        ArgumentNullException.ThrowIfNull(failure);
        ArgumentNullException.ThrowIfNull(success);
        return ToHttpResultsAsyncCore(result, failure, success);
    }

    /// <summary>
    /// Asynchronously maps a validation value task to typed HTTP results.
    /// </summary>
    /// <typeparam name="TValue">The valid value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <typeparam name="TInvalid">The typed result selected for invalid errors.</typeparam>
    /// <param name="validation">The validation value task to map.</param>
    /// <param name="invalid">Maps the complete ordered error list to a caller-selected typed result.</param>
    /// <returns>A value task that produces the typed result union.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="invalid"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The selected mapper returns null, or the outcome is uninitialized.</exception>
    /// <remarks>
    /// The input is awaited once; faults and cancellation propagate unchanged, and only the selected
    /// outcome mapper runs.
    /// </remarks>
    public static ValueTask<Results<Ok<TValue>, TInvalid>> ToHttpResultsAsync<TValue, TError, TInvalid>(
        this ValueTask<Validation<TValue, TError>> validation,
        Func<IReadOnlyList<TError>, TInvalid> invalid)
        where TInvalid : IResult
    {
        ArgumentNullException.ThrowIfNull(invalid);
        return ToHttpResultsAsyncCore(validation, invalid, static value => TypedResults.Ok(value));
    }

    /// <summary>
    /// Asynchronously maps a validation value task to caller-selected typed results.
    /// </summary>
    /// <typeparam name="TValue">The valid value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <typeparam name="TValid">The typed result selected for a valid value.</typeparam>
    /// <typeparam name="TInvalid">The typed result selected for invalid errors.</typeparam>
    /// <param name="validation">The validation value task to map.</param>
    /// <param name="invalid">Maps the complete ordered error list to a caller-selected typed result.</param>
    /// <param name="valid">Maps a valid value to a caller-selected typed result.</param>
    /// <returns>A value task that produces the typed result union.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="invalid"/> or <paramref name="valid"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The selected mapper returns null, or the outcome is uninitialized.</exception>
    /// <remarks>
    /// The input is awaited once; faults and cancellation propagate unchanged, and only the selected
    /// outcome mapper runs.
    /// </remarks>
    public static ValueTask<Results<TValid, TInvalid>> ToHttpResultsAsync<TValue, TError, TValid, TInvalid>(
        this ValueTask<Validation<TValue, TError>> validation,
        Func<IReadOnlyList<TError>, TInvalid> invalid,
        Func<TValue, TValid> valid)
        where TValid : IResult
        where TInvalid : IResult
    {
        ArgumentNullException.ThrowIfNull(invalid);
        ArgumentNullException.ThrowIfNull(valid);
        return ToHttpResultsAsyncCore(validation, invalid, valid);
    }

    /// <summary>
    /// Asynchronously maps a unit result value task to typed HTTP results.
    /// </summary>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <typeparam name="TFailure">The typed result selected for failure.</typeparam>
    /// <param name="result">The unit result value task to map.</param>
    /// <param name="failure">Maps failure to a caller-selected typed result.</param>
    /// <returns>A value task that produces the typed result union.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="failure"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The selected mapper returns null, or the outcome is uninitialized.</exception>
    /// <remarks>
    /// The input is awaited once; faults and cancellation propagate unchanged, and only the selected
    /// outcome mapper runs.
    /// </remarks>
    public static ValueTask<Results<NoContent, TFailure>> ToHttpResultsAsync<TError, TFailure>(
        this ValueTask<UnitResult<TError>> result,
        Func<TError, TFailure> failure)
        where TFailure : IResult
    {
        ArgumentNullException.ThrowIfNull(failure);
        return ToHttpResultsAsyncCore(result, failure, static () => TypedResults.NoContent());
    }

    /// <summary>
    /// Asynchronously maps a unit result value task to caller-selected typed results.
    /// </summary>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <typeparam name="TSuccess">The typed result selected for success.</typeparam>
    /// <typeparam name="TFailure">The typed result selected for failure.</typeparam>
    /// <param name="result">The unit result value task to map.</param>
    /// <param name="failure">Maps failure to a caller-selected typed result.</param>
    /// <param name="success">Maps success to a caller-selected typed result.</param>
    /// <returns>A value task that produces the typed result union.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="failure"/> or <paramref name="success"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The selected mapper returns null, or the outcome is uninitialized.</exception>
    /// <remarks>
    /// The input is awaited once; faults and cancellation propagate unchanged, and only the selected
    /// outcome mapper runs.
    /// </remarks>
    public static ValueTask<Results<TSuccess, TFailure>> ToHttpResultsAsync<TError, TSuccess, TFailure>(
        this ValueTask<UnitResult<TError>> result,
        Func<TError, TFailure> failure,
        Func<TSuccess> success)
        where TSuccess : IResult
        where TFailure : IResult
    {
        ArgumentNullException.ThrowIfNull(failure);
        ArgumentNullException.ThrowIfNull(success);
        return ToHttpResultsAsyncCore(result, failure, success);
    }

    private static Results<TSome, TNone> ToHttpResultsCore<T, TSome, TNone>(
        Option<T> option,
        Func<TNone> none,
        Func<T, TSome> some)
        where TSome : IResult
        where TNone : IResult
    {
        if (option.TryGetValue(out var value))
        {
            return RequireResult(some(value!));
        }

        return RequireResult(none());
    }

    private static Results<TSuccess, TFailure> ToHttpResultsCore<TValue, TError, TSuccess, TFailure>(
        Result<TValue, TError> result,
        Func<TError, TFailure> failure,
        Func<TValue, TSuccess> success)
        where TSuccess : IResult
        where TFailure : IResult
    {
        if (result.TryGetValue(out var value))
        {
            return RequireResult(success(value!));
        }

        _ = result.TryGetError(out var error);
        return RequireResult(failure(error!));
    }

    private static Results<TValid, TInvalid> ToHttpResultsCore<TValue, TError, TValid, TInvalid>(
        Validation<TValue, TError> validation,
        Func<IReadOnlyList<TError>, TInvalid> invalid,
        Func<TValue, TValid> valid)
        where TValid : IResult
        where TInvalid : IResult
    {
        if (validation.TryGetValue(out var value))
        {
            return RequireResult(valid(value!));
        }

        _ = validation.TryGetErrors(out var errors);
        return RequireResult(invalid(errors!));
    }

    private static Results<TSuccess, TFailure> ToHttpResultsCore<TError, TSuccess, TFailure>(
        UnitResult<TError> result,
        Func<TError, TFailure> failure,
        Func<TSuccess> success)
        where TSuccess : IResult
        where TFailure : IResult
    {
        if (result.TryGetError(out var error))
        {
            return RequireResult(failure(error!));
        }

        return RequireResult(success());
    }

    private static async Task<Results<TSome, TNone>> ToHttpResultsAsyncCore<T, TSome, TNone>(
        Task<Option<T>> option,
        Func<TNone> none,
        Func<T, TSome> some)
        where TSome : IResult
        where TNone : IResult =>
        ToHttpResultsCore(await option.ConfigureAwait(false), none, some);

    private static async Task<Results<TSuccess, TFailure>> ToHttpResultsAsyncCore<TValue, TError, TSuccess, TFailure>(
        Task<Result<TValue, TError>> result,
        Func<TError, TFailure> failure,
        Func<TValue, TSuccess> success)
        where TSuccess : IResult
        where TFailure : IResult =>
        ToHttpResultsCore(await result.ConfigureAwait(false), failure, success);

    private static async Task<Results<TValid, TInvalid>> ToHttpResultsAsyncCore<TValue, TError, TValid, TInvalid>(
        Task<Validation<TValue, TError>> validation,
        Func<IReadOnlyList<TError>, TInvalid> invalid,
        Func<TValue, TValid> valid)
        where TValid : IResult
        where TInvalid : IResult =>
        ToHttpResultsCore(await validation.ConfigureAwait(false), invalid, valid);

    private static async Task<Results<TSuccess, TFailure>> ToHttpResultsAsyncCore<TError, TSuccess, TFailure>(
        Task<UnitResult<TError>> result,
        Func<TError, TFailure> failure,
        Func<TSuccess> success)
        where TSuccess : IResult
        where TFailure : IResult =>
        ToHttpResultsCore(await result.ConfigureAwait(false), failure, success);

    private static async ValueTask<Results<TSome, TNone>> ToHttpResultsAsyncCore<T, TSome, TNone>(
        ValueTask<Option<T>> option,
        Func<TNone> none,
        Func<T, TSome> some)
        where TSome : IResult
        where TNone : IResult =>
        ToHttpResultsCore(await option.ConfigureAwait(false), none, some);

    private static async ValueTask<Results<TSuccess, TFailure>> ToHttpResultsAsyncCore<TValue, TError, TSuccess, TFailure>(
        ValueTask<Result<TValue, TError>> result,
        Func<TError, TFailure> failure,
        Func<TValue, TSuccess> success)
        where TSuccess : IResult
        where TFailure : IResult =>
        ToHttpResultsCore(await result.ConfigureAwait(false), failure, success);

    private static async ValueTask<Results<TValid, TInvalid>> ToHttpResultsAsyncCore<TValue, TError, TValid, TInvalid>(
        ValueTask<Validation<TValue, TError>> validation,
        Func<IReadOnlyList<TError>, TInvalid> invalid,
        Func<TValue, TValid> valid)
        where TValid : IResult
        where TInvalid : IResult =>
        ToHttpResultsCore(await validation.ConfigureAwait(false), invalid, valid);

    private static async ValueTask<Results<TSuccess, TFailure>> ToHttpResultsAsyncCore<TError, TSuccess, TFailure>(
        ValueTask<UnitResult<TError>> result,
        Func<TError, TFailure> failure,
        Func<TSuccess> success)
        where TSuccess : IResult
        where TFailure : IResult =>
        ToHttpResultsCore(await result.ConfigureAwait(false), failure, success);

    private static TResult RequireResult<TResult>(TResult result)
        where TResult : IResult =>
        result is null
            ? throw new InvalidOperationException("The HTTP result mapper returned null.")
            : result;
}
