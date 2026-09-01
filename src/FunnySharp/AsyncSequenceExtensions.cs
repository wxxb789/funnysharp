namespace FunnySharp;

/// <summary>
/// Provides asynchronous traversal operations for asynchronous sequences of functional values.
/// </summary>
public static class AsyncSequenceExtensions
{
    /// <summary>
    /// Asynchronously collects the values from a sequence of options when every option is present.
    /// </summary>
    /// <typeparam name="TValue">The option value type.</typeparam>
    /// <param name="source">The asynchronous sequence of options to collect.</param>
    /// <param name="cancellationToken">The token passed to the asynchronous enumerator.</param>
    /// <returns>
    /// An asynchronous operation that produces an option containing the values in source order, or
    /// <c>None</c> when any source option is absent.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static ValueTask<Option<IReadOnlyList<TValue>>> SequenceAsync<TValue>(
        this IAsyncEnumerable<Option<TValue>> source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        return TraverseOptionValueAsyncCore(
            source,
            static (value, _) => ValueTask.FromResult(value),
            cancellationToken);
    }

    /// <summary>
    /// Asynchronously applies an option-producing selector to each source item and collects the values
    /// when every result is present.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The selected option value type.</typeparam>
    /// <param name="source">The asynchronous sequence to traverse.</param>
    /// <param name="selector">The option-producing selector.</param>
    /// <param name="cancellationToken">The token passed to the asynchronous enumerator.</param>
    /// <returns>
    /// An asynchronous operation that produces an option containing the selected values in source order,
    /// or <c>None</c> when a selector result is absent.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    public static ValueTask<Option<IReadOnlyList<TResult>>> TraverseAsync<TSource, TResult>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, Option<TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        return TraverseOptionValueAsyncCore(
            source,
            (value, _) => ValueTask.FromResult(selector(value)),
            cancellationToken);
    }

    /// <summary>
    /// Asynchronously applies a ValueTask-based option-producing selector to each source item and collects
    /// the values when every result is present.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The selected option value type.</typeparam>
    /// <param name="source">The asynchronous sequence to traverse.</param>
    /// <param name="selector">The ValueTask-based option-producing selector.</param>
    /// <param name="cancellationToken">The token passed to the asynchronous enumerator.</param>
    /// <returns>
    /// An asynchronous operation that produces an option containing the selected values in source order,
    /// or <c>None</c> when a selector result is absent.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    public static ValueTask<Option<IReadOnlyList<TResult>>> TraverseValueAsync<TSource, TResult>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, ValueTask<Option<TResult>>> selector,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        return TraverseOptionValueAsyncCore(
            source,
            (value, _) => selector(value),
            cancellationToken);
    }

    /// <summary>
    /// Asynchronously applies a cancellation-aware ValueTask-based option-producing selector to each source
    /// item and collects the values when every result is present.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The selected option value type.</typeparam>
    /// <param name="source">The asynchronous sequence to traverse.</param>
    /// <param name="selector">The cancellation-aware ValueTask-based option-producing selector.</param>
    /// <param name="cancellationToken">The token passed to the asynchronous enumerator and selector.</param>
    /// <returns>
    /// An asynchronous operation that produces an option containing the selected values in source order,
    /// or <c>None</c> when a selector result is absent.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    public static ValueTask<Option<IReadOnlyList<TResult>>> TraverseValueAsync<TSource, TResult>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, CancellationToken, ValueTask<Option<TResult>>> selector,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        return TraverseOptionValueAsyncCore(source, selector, cancellationToken);
    }

    /// <summary>
    /// Asynchronously collects the values from a sequence of results when every result is successful.
    /// </summary>
    /// <typeparam name="TValue">The result value type.</typeparam>
    /// <typeparam name="TError">The result error type.</typeparam>
    /// <param name="source">The asynchronous sequence of results to collect.</param>
    /// <param name="cancellationToken">The token passed to the asynchronous enumerator.</param>
    /// <returns>
    /// An asynchronous operation that produces a successful result containing the values in source order, or
    /// the first failed result's error.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static ValueTask<Result<IReadOnlyList<TValue>, TError>> SequenceAsync<TValue, TError>(
        this IAsyncEnumerable<Result<TValue, TError>> source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        return TraverseResultValueAsyncCore(
            source,
            static (value, _) => ValueTask.FromResult(value),
            cancellationToken);
    }

    /// <summary>
    /// Asynchronously applies a result-producing selector to each source item and collects the values when
    /// every result is successful.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The selected result value type.</typeparam>
    /// <typeparam name="TError">The result error type.</typeparam>
    /// <param name="source">The asynchronous sequence to traverse.</param>
    /// <param name="selector">The result-producing selector.</param>
    /// <param name="cancellationToken">The token passed to the asynchronous enumerator.</param>
    /// <returns>
    /// An asynchronous operation that produces a successful result containing the selected values in source
    /// order, or the first selector failure.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    public static ValueTask<Result<IReadOnlyList<TResult>, TError>> TraverseAsync<TSource, TResult, TError>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, Result<TResult, TError>> selector,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        return TraverseResultValueAsyncCore(
            source,
            (value, _) => ValueTask.FromResult(selector(value)),
            cancellationToken);
    }

    /// <summary>
    /// Asynchronously applies a ValueTask-based result-producing selector to each source item and collects the
    /// values when every result is successful.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The selected result value type.</typeparam>
    /// <typeparam name="TError">The result error type.</typeparam>
    /// <param name="source">The asynchronous sequence to traverse.</param>
    /// <param name="selector">The ValueTask-based result-producing selector.</param>
    /// <param name="cancellationToken">The token passed to the asynchronous enumerator.</param>
    /// <returns>
    /// An asynchronous operation that produces a successful result containing the selected values in source
    /// order, or the first selector failure.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    public static ValueTask<Result<IReadOnlyList<TResult>, TError>> TraverseValueAsync<TSource, TResult, TError>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, ValueTask<Result<TResult, TError>>> selector,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        return TraverseResultValueAsyncCore(
            source,
            (value, _) => selector(value),
            cancellationToken);
    }

    /// <summary>
    /// Asynchronously applies a cancellation-aware ValueTask-based result-producing selector to each source
    /// item and collects the values when every result is successful.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The selected result value type.</typeparam>
    /// <typeparam name="TError">The result error type.</typeparam>
    /// <param name="source">The asynchronous sequence to traverse.</param>
    /// <param name="selector">The cancellation-aware ValueTask-based result-producing selector.</param>
    /// <param name="cancellationToken">The token passed to the asynchronous enumerator and selector.</param>
    /// <returns>
    /// An asynchronous operation that produces a successful result containing the selected values in source
    /// order, or the first selector failure.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    public static ValueTask<Result<IReadOnlyList<TResult>, TError>> TraverseValueAsync<TSource, TResult, TError>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, CancellationToken, ValueTask<Result<TResult, TError>>> selector,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        return TraverseResultValueAsyncCore(source, selector, cancellationToken);
    }

    /// <summary>
    /// Asynchronously collects the values from a sequence of validations, accumulating all errors in source
    /// order.
    /// </summary>
    /// <typeparam name="TValue">The validation value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="source">The asynchronous sequence of validations to collect.</param>
    /// <param name="cancellationToken">The token passed to the asynchronous enumerator.</param>
    /// <returns>
    /// An asynchronous operation that produces a valid validation containing the values in source order, or an
    /// invalid validation containing all errors in source and per-validation order.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static ValueTask<Validation<IReadOnlyList<TValue>, TError>> SequenceAsync<TValue, TError>(
        this IAsyncEnumerable<Validation<TValue, TError>> source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        return TraverseValidationValueAsyncCore(
            source,
            static (value, _) => ValueTask.FromResult(value),
            cancellationToken);
    }

    /// <summary>
    /// Asynchronously applies a validation-producing selector to each source item and collects values or
    /// accumulates all validation errors.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The selected validation value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="source">The asynchronous sequence to traverse.</param>
    /// <param name="selector">The validation-producing selector.</param>
    /// <param name="cancellationToken">The token passed to the asynchronous enumerator.</param>
    /// <returns>
    /// An asynchronous operation that produces a valid validation containing the selected values in source order,
    /// or an invalid validation containing all selector errors in source and per-validation order.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    public static ValueTask<Validation<IReadOnlyList<TResult>, TError>> TraverseAsync<TSource, TResult, TError>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, Validation<TResult, TError>> selector,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        return TraverseValidationValueAsyncCore(
            source,
            (value, _) => ValueTask.FromResult(selector(value)),
            cancellationToken);
    }

    /// <summary>
    /// Asynchronously applies a ValueTask-based validation-producing selector to each source item and collects
    /// values or accumulates all validation errors.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The selected validation value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="source">The asynchronous sequence to traverse.</param>
    /// <param name="selector">The ValueTask-based validation-producing selector.</param>
    /// <param name="cancellationToken">The token passed to the asynchronous enumerator.</param>
    /// <returns>
    /// An asynchronous operation that produces a valid validation containing the selected values in source order,
    /// or an invalid validation containing all selector errors in source and per-validation order.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    public static ValueTask<Validation<IReadOnlyList<TResult>, TError>> TraverseValueAsync<TSource, TResult, TError>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, ValueTask<Validation<TResult, TError>>> selector,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        return TraverseValidationValueAsyncCore(
            source,
            (value, _) => selector(value),
            cancellationToken);
    }

    /// <summary>
    /// Asynchronously applies a cancellation-aware ValueTask-based validation-producing selector to each source
    /// item and collects values or accumulates all validation errors.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The selected validation value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="source">The asynchronous sequence to traverse.</param>
    /// <param name="selector">The cancellation-aware ValueTask-based validation-producing selector.</param>
    /// <param name="cancellationToken">The token passed to the asynchronous enumerator and selector.</param>
    /// <returns>
    /// An asynchronous operation that produces a valid validation containing the selected values in source order,
    /// or an invalid validation containing all selector errors in source and per-validation order.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    public static ValueTask<Validation<IReadOnlyList<TResult>, TError>> TraverseValueAsync<TSource, TResult, TError>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, CancellationToken, ValueTask<Validation<TResult, TError>>> selector,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        return TraverseValidationValueAsyncCore(source, selector, cancellationToken);
    }

    private static async ValueTask<Option<IReadOnlyList<TResult>>> TraverseOptionValueAsyncCore<TSource, TResult>(
        IAsyncEnumerable<TSource> source,
        Func<TSource, CancellationToken, ValueTask<Option<TResult>>> selector,
        CancellationToken cancellationToken)
    {
        List<TResult>? values = null;
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var option = await selector(item, cancellationToken).ConfigureAwait(false);
            if (!option.TryGetValue(out var value))
            {
                return Option<IReadOnlyList<TResult>>.None;
            }

            (values ??= new List<TResult>()).Add(value!);
        }

        return Option<IReadOnlyList<TResult>>.Some(SequenceExtensions.ToReadOnlyList(values));
    }

    private static async ValueTask<Result<IReadOnlyList<TResult>, TError>> TraverseResultValueAsyncCore<TSource, TResult, TError>(
        IAsyncEnumerable<TSource> source,
        Func<TSource, CancellationToken, ValueTask<Result<TResult, TError>>> selector,
        CancellationToken cancellationToken)
    {
        List<TResult>? values = null;
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var result = await selector(item, cancellationToken).ConfigureAwait(false);
            if (!result.TryGetValue(out var value))
            {
                result.TryGetError(out var error);
                return Result<IReadOnlyList<TResult>, TError>.Failure(error!);
            }

            (values ??= new List<TResult>()).Add(value!);
        }

        return Result<IReadOnlyList<TResult>, TError>.Success(
            SequenceExtensions.ToReadOnlyList(values));
    }

    private static async ValueTask<Validation<IReadOnlyList<TResult>, TError>> TraverseValidationValueAsyncCore<TSource, TResult, TError>(
        IAsyncEnumerable<TSource> source,
        Func<TSource, CancellationToken, ValueTask<Validation<TResult, TError>>> selector,
        CancellationToken cancellationToken)
    {
        List<TResult>? values = null;
        List<TError>? errors = null;
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var validation = await selector(item, cancellationToken).ConfigureAwait(false);
            if (validation.TryGetValue(out var value))
            {
                if (errors is null)
                {
                    (values ??= new List<TResult>()).Add(value!);
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
            ? Validation<IReadOnlyList<TResult>, TError>.Valid(
                SequenceExtensions.ToReadOnlyList(values))
            : Validation<IReadOnlyList<TResult>, TError>.InvalidFromOwnedErrors(errors);
    }
}
