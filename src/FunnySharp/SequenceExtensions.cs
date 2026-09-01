namespace FunnySharp;

/// <summary>
/// Provides eager traversal operations for sequences of functional values.
/// </summary>
public static class SequenceExtensions
{
    /// <summary>
    /// Collects the values from a sequence of options when every option is present.
    /// </summary>
    /// <typeparam name="TValue">The option value type.</typeparam>
    /// <param name="source">The sequence of options to collect.</param>
    /// <returns>
    /// An option containing the values in source order, or <c>None</c> when any source option is absent.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static Option<IReadOnlyList<TValue>> Sequence<TValue>(
        this IEnumerable<Option<TValue>> source) =>
        source.Traverse(static value => value);

    /// <summary>
    /// Applies an option-producing selector to each source item and collects the values when every result
    /// is present.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The selected option value type.</typeparam>
    /// <param name="source">The sequence to traverse.</param>
    /// <param name="selector">The option-producing selector.</param>
    /// <returns>
    /// An option containing the selected values in source order, or <c>None</c> when a selector result is
    /// absent.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    public static Option<IReadOnlyList<TResult>> Traverse<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, Option<TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        List<TResult>? values = null;
        foreach (var item in source)
        {
            if (!selector(item).TryGetValue(out var value))
            {
                return Option<IReadOnlyList<TResult>>.None;
            }

            (values ??= new List<TResult>(GetInitialCapacity(source))).Add(value!);
        }

        return Option<IReadOnlyList<TResult>>.Some(ToReadOnlyList(values));
    }

    /// <summary>
    /// Collects the values from a sequence of results when every result is successful.
    /// </summary>
    /// <typeparam name="TValue">The result value type.</typeparam>
    /// <typeparam name="TError">The result error type.</typeparam>
    /// <param name="source">The sequence of results to collect.</param>
    /// <returns>
    /// A successful result containing the values in source order, or the first failed result's error.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static Result<IReadOnlyList<TValue>, TError> Sequence<TValue, TError>(
        this IEnumerable<Result<TValue, TError>> source) =>
        source.Traverse(static value => value);

    /// <summary>
    /// Applies a result-producing selector to each source item and collects the values when every result is
    /// successful.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The selected result value type.</typeparam>
    /// <typeparam name="TError">The result error type.</typeparam>
    /// <param name="source">The sequence to traverse.</param>
    /// <param name="selector">The result-producing selector.</param>
    /// <returns>
    /// A successful result containing the selected values in source order, or the first selector failure.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    public static Result<IReadOnlyList<TResult>, TError> Traverse<TSource, TResult, TError>(
        this IEnumerable<TSource> source,
        Func<TSource, Result<TResult, TError>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        List<TResult>? values = null;
        foreach (var item in source)
        {
            var result = selector(item);
            if (!result.TryGetValue(out var value))
            {
                result.TryGetError(out var error);
                return Result<IReadOnlyList<TResult>, TError>.Failure(error!);
            }

            (values ??= new List<TResult>(GetInitialCapacity(source))).Add(value!);
        }

        return Result<IReadOnlyList<TResult>, TError>.Success(ToReadOnlyList(values));
    }

    /// <summary>
    /// Collects the values from a sequence of validations, accumulating all errors in source order.
    /// </summary>
    /// <typeparam name="TValue">The validation value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="source">The sequence of validations to collect.</param>
    /// <returns>
    /// A valid validation containing the values in source order, or an invalid validation containing all
    /// errors in source and per-validation order.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static Validation<IReadOnlyList<TValue>, TError> Sequence<TValue, TError>(
        this IEnumerable<Validation<TValue, TError>> source) =>
        source.Traverse(static value => value);

    /// <summary>
    /// Applies a validation-producing selector to each source item and collects values or accumulates all
    /// validation errors.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The selected validation value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="source">The sequence to traverse.</param>
    /// <param name="selector">The validation-producing selector.</param>
    /// <returns>
    /// A valid validation containing the selected values in source order, or an invalid validation
    /// containing all selector errors in source and per-validation order.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    public static Validation<IReadOnlyList<TResult>, TError> Traverse<TSource, TResult, TError>(
        this IEnumerable<TSource> source,
        Func<TSource, Validation<TResult, TError>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        List<TResult>? values = null;
        List<TError>? errors = null;

        foreach (var item in source)
        {
            var validation = selector(item);
            if (validation.TryGetValue(out var value))
            {
                if (errors is null)
                {
                    (values ??= new List<TResult>(GetInitialCapacity(source))).Add(value!);
                }

                continue;
            }

            values = null;
            validation.TryGetErrors(out var validationErrors);
            var currentErrors = validationErrors!;
            errors ??= new List<TError>(currentErrors.Count);
            for (var index = 0; index < currentErrors.Count; index++)
            {
                errors.Add(currentErrors[index]);
            }
        }

        return errors is null
            ? Validation<IReadOnlyList<TResult>, TError>.Valid(ToReadOnlyList(values))
            : Validation<IReadOnlyList<TResult>, TError>.InvalidFromOwnedErrors(errors);
    }

    private static int GetInitialCapacity<T>(IEnumerable<T> source) =>
        Enumerable.TryGetNonEnumeratedCount(source, out var count) ? count : 0;

    internal static IReadOnlyList<T> ToReadOnlyList<T>(List<T>? values) =>
        values is null ? Array.Empty<T>() : values.AsReadOnly();
}
