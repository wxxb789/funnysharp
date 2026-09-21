using System.Diagnostics.CodeAnalysis;

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
    /// Collects a sequence of unit results, stopping at the first failure.
    /// </summary>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="source">The sequence of unit results to collect.</param>
    /// <returns>Success when every source unit result is successful; otherwise, the first failure's error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static UnitResult<TError> Sequence<TError>(
        this IEnumerable<UnitResult<TError>> source) =>
        source.Traverse(static value => value);

    /// <summary>
    /// Applies a unit-result-producing selector to each source item, stopping at the first failure.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="source">The sequence to traverse.</param>
    /// <param name="selector">The unit-result-producing selector.</param>
    /// <returns>
    /// Success when every selector result is successful; otherwise, the first failed selector's error.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    public static UnitResult<TError> Traverse<TSource, TError>(
        this IEnumerable<TSource> source,
        Func<TSource, UnitResult<TError>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        foreach (var item in source)
        {
            var result = selector(item);
            if (!result.IsSuccess)
            {
                result.TryGetError(out var error);
                return UnitResult<TError>.Failure(error!);
            }
        }

        return UnitResult<TError>.Success();
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

    /// <summary>
    /// Applies an indexed option-producing selector to each source item and collects the values when
    /// every result is present.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The selected option value type.</typeparam>
    /// <param name="source">The sequence to traverse.</param>
    /// <param name="selector">The indexed option-producing selector.</param>
    /// <returns>
    /// An option containing the selected values in source order, or <c>None</c> when a selector result
    /// is absent.
    /// </returns>
    /// <remarks>
    /// The selector receives each reached item's zero-based index and is invoked once per item in
    /// source order; the index counts reached items, so a short-circuit stops incrementing because the
    /// loop returns.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    public static Option<IReadOnlyList<TResult>> Traverse<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<int, TSource, Option<TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var index = 0;
        List<TResult>? values = null;
        foreach (var item in source)
        {
            if (!selector(index++, item).TryGetValue(out var value))
            {
                return Option<IReadOnlyList<TResult>>.None;
            }

            (values ??= new List<TResult>(GetInitialCapacity(source))).Add(value!);
        }

        return Option<IReadOnlyList<TResult>>.Some(ToReadOnlyList(values));
    }

    /// <summary>
    /// Applies an indexed result-producing selector to each source item and collects the values when
    /// every result is successful.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The selected result value type.</typeparam>
    /// <typeparam name="TError">The result error type.</typeparam>
    /// <param name="source">The sequence to traverse.</param>
    /// <param name="selector">The indexed result-producing selector.</param>
    /// <returns>
    /// A successful result containing the selected values in source order, or the first selector
    /// failure.
    /// </returns>
    /// <remarks>
    /// The selector receives each reached item's zero-based index and is invoked once per item in
    /// source order; the index counts reached items, so a short-circuit stops incrementing because the
    /// loop returns the first failure.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    public static Result<IReadOnlyList<TResult>, TError> Traverse<TSource, TResult, TError>(
        this IEnumerable<TSource> source,
        Func<int, TSource, Result<TResult, TError>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var index = 0;
        List<TResult>? values = null;
        foreach (var item in source)
        {
            var result = selector(index++, item);
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
    /// Applies an indexed unit-result-producing selector to each source item, stopping at the first
    /// failure.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="source">The sequence to traverse.</param>
    /// <param name="selector">The indexed unit-result-producing selector.</param>
    /// <returns>
    /// Success when every selector result is successful; otherwise, the first failed selector's error.
    /// </returns>
    /// <remarks>
    /// The selector receives each reached item's zero-based index and is invoked once per item in
    /// source order; the index counts reached items, so a short-circuit stops incrementing because the
    /// loop returns the first failure.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    public static UnitResult<TError> Traverse<TSource, TError>(
        this IEnumerable<TSource> source,
        Func<int, TSource, UnitResult<TError>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var index = 0;
        foreach (var item in source)
        {
            var result = selector(index++, item);
            if (!result.IsSuccess)
            {
                result.TryGetError(out var error);
                return UnitResult<TError>.Failure(error!);
            }
        }

        return UnitResult<TError>.Success();
    }

    /// <summary>
    /// Applies an indexed validation-producing selector to each source item and collects values or
    /// accumulates all validation errors.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The selected validation value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="source">The sequence to traverse.</param>
    /// <param name="selector">The indexed validation-producing selector.</param>
    /// <returns>
    /// A valid validation containing the selected values in source order, or an invalid validation
    /// containing all selector errors in source and per-validation order.
    /// </returns>
    /// <remarks>
    /// The selector receives each reached item's zero-based index and is invoked once per item in
    /// source order; validation reads every item, so every index is reached.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    public static Validation<IReadOnlyList<TResult>, TError> Traverse<TSource, TResult, TError>(
        this IEnumerable<TSource> source,
        Func<int, TSource, Validation<TResult, TError>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var index = 0;
        List<TResult>? values = null;
        List<TError>? errors = null;

        foreach (var item in source)
        {
            var validation = selector(index++, item);
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
            for (var errorIndex = 0; errorIndex < currentErrors.Count; errorIndex++)
            {
                errors.Add(currentErrors[errorIndex]);
            }
        }

        return errors is null
            ? Validation<IReadOnlyList<TResult>, TError>.Valid(ToReadOnlyList(values))
            : Validation<IReadOnlyList<TResult>, TError>.InvalidFromOwnedErrors(errors);
    }

    /// <summary>
    /// Applies an option-producing selector to each dictionary value and collects the results into a
    /// new dictionary when every result is present.
    /// </summary>
    /// <typeparam name="TKey">The dictionary key type.</typeparam>
    /// <typeparam name="TValue">The dictionary value type.</typeparam>
    /// <typeparam name="TResult">The selected option value type.</typeparam>
    /// <param name="source">The dictionary whose values to traverse.</param>
    /// <param name="selector">The option-producing selector, invoked once per entry with its key and value.</param>
    /// <returns>
    /// An option containing a new dictionary mapping each source key to its selected value, or
    /// <c>None</c> when a selector result is absent.
    /// </returns>
    /// <remarks>
    /// The dictionary shape is preserved instead of collapsing to a list: success materializes a new
    /// dictionary filled in the source dictionary's enumeration order, with its capacity hinted from
    /// the source dictionary's <c>Count</c>. <c>Dictionary</c> in practice preserves insertion order,
    /// but the BCL does not contract it.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    public static Option<IReadOnlyDictionary<TKey, TResult>> Traverse<TKey, TValue, TResult>(
        this IReadOnlyDictionary<TKey, TValue> source,
        Func<TKey, TValue, Option<TResult>> selector)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        Dictionary<TKey, TResult>? values = null;
        foreach (var pair in source)
        {
            if (!selector(pair.Key, pair.Value).TryGetValue(out var value))
            {
                return Option<IReadOnlyDictionary<TKey, TResult>>.None;
            }

            (values ??= new Dictionary<TKey, TResult>(source.Count)).Add(pair.Key, value!);
        }

        return Option<IReadOnlyDictionary<TKey, TResult>>.Some(
            ToReadOnlyDictionary(values));
    }

    /// <summary>
    /// Applies a result-producing selector to each dictionary value and collects the results into a new
    /// dictionary when every result is successful.
    /// </summary>
    /// <typeparam name="TKey">The dictionary key type.</typeparam>
    /// <typeparam name="TValue">The dictionary value type.</typeparam>
    /// <typeparam name="TResult">The selected result value type.</typeparam>
    /// <typeparam name="TError">The result error type.</typeparam>
    /// <param name="source">The dictionary whose values to traverse.</param>
    /// <param name="selector">The result-producing selector, invoked once per entry with its key and value.</param>
    /// <returns>
    /// A successful result containing a new dictionary mapping each source key to its selected value,
    /// or the first selector failure.
    /// </returns>
    /// <remarks>
    /// The dictionary shape is preserved instead of collapsing to a list: success materializes a new
    /// dictionary filled in the source dictionary's enumeration order, with its capacity hinted from
    /// the source dictionary's <c>Count</c>. <c>Dictionary</c> in practice preserves insertion order,
    /// but the BCL does not contract it.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    public static Result<IReadOnlyDictionary<TKey, TResult>, TError> Traverse<TKey, TValue, TResult, TError>(
        this IReadOnlyDictionary<TKey, TValue> source,
        Func<TKey, TValue, Result<TResult, TError>> selector)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        Dictionary<TKey, TResult>? values = null;
        foreach (var pair in source)
        {
            var result = selector(pair.Key, pair.Value);
            if (!result.TryGetValue(out var value))
            {
                result.TryGetError(out var error);
                return Result<IReadOnlyDictionary<TKey, TResult>, TError>.Failure(error!);
            }

            (values ??= new Dictionary<TKey, TResult>(source.Count)).Add(pair.Key, value!);
        }

        return Result<IReadOnlyDictionary<TKey, TResult>, TError>.Success(
            ToReadOnlyDictionary(values));
    }

    /// <summary>
    /// Applies a unit-result-producing selector to each dictionary value, stopping at the first
    /// failure.
    /// </summary>
    /// <typeparam name="TKey">The dictionary key type.</typeparam>
    /// <typeparam name="TValue">The dictionary value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="source">The dictionary whose values to traverse.</param>
    /// <param name="selector">The unit-result-producing selector, invoked once per entry with its key and value.</param>
    /// <returns>
    /// Success when every selector result is successful; otherwise, the first failed selector's error.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    public static UnitResult<TError> Traverse<TKey, TValue, TError>(
        this IReadOnlyDictionary<TKey, TValue> source,
        Func<TKey, TValue, UnitResult<TError>> selector)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        foreach (var pair in source)
        {
            var result = selector(pair.Key, pair.Value);
            if (!result.IsSuccess)
            {
                result.TryGetError(out var error);
                return UnitResult<TError>.Failure(error!);
            }
        }

        return UnitResult<TError>.Success();
    }

    /// <summary>
    /// Applies a validation-producing selector to each dictionary value and collects the results into a
    /// new dictionary or accumulates all validation errors.
    /// </summary>
    /// <typeparam name="TKey">The dictionary key type.</typeparam>
    /// <typeparam name="TValue">The dictionary value type.</typeparam>
    /// <typeparam name="TResult">The selected validation value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="source">The dictionary whose values to traverse.</param>
    /// <param name="selector">The validation-producing selector, invoked once per entry with its key and value.</param>
    /// <returns>
    /// A valid validation containing a new dictionary mapping each source key to its selected value,
    /// or an invalid validation containing all selector errors in source and per-validation order.
    /// </returns>
    /// <remarks>
    /// The dictionary shape is preserved instead of collapsing to a list: success materializes a new
    /// dictionary filled in the source dictionary's enumeration order, with its capacity hinted from
    /// the source dictionary's <c>Count</c>. <c>Dictionary</c> in practice preserves insertion order,
    /// but the BCL does not contract it. Validation reads every entry and accumulates all errors in
    /// the source dictionary's enumeration order.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    public static Validation<IReadOnlyDictionary<TKey, TResult>, TError> Traverse<TKey, TValue, TResult, TError>(
        this IReadOnlyDictionary<TKey, TValue> source,
        Func<TKey, TValue, Validation<TResult, TError>> selector)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        Dictionary<TKey, TResult>? values = null;
        List<TError>? errors = null;

        foreach (var pair in source)
        {
            var validation = selector(pair.Key, pair.Value);
            if (validation.TryGetValue(out var value))
            {
                if (errors is null)
                {
                    (values ??= new Dictionary<TKey, TResult>(source.Count)).Add(pair.Key, value!);
                }

                continue;
            }

            values = null;
            validation.TryGetErrors(out var validationErrors);
            var currentErrors = validationErrors!;
            errors ??= new List<TError>(currentErrors.Count);
            for (var errorIndex = 0; errorIndex < currentErrors.Count; errorIndex++)
            {
                errors.Add(currentErrors[errorIndex]);
            }
        }

        return errors is null
            ? Validation<IReadOnlyDictionary<TKey, TResult>, TError>.Valid(
                ToReadOnlyDictionary(values))
            : Validation<IReadOnlyDictionary<TKey, TResult>, TError>.InvalidFromOwnedErrors(errors);
    }

    /// <summary>
    /// Applies a location-aware option-producing selector to each source item and collects the values
    /// when every result is present.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The selected option value type.</typeparam>
    /// <param name="source">The sequence to traverse.</param>
    /// <param name="root">The location root that each item's location is composed from.</param>
    /// <param name="selector">
    /// The location-aware option-producing selector, invoked once per reached item with the item's
    /// composed location, <c>root.At(index)</c>, and the item.
    /// </param>
    /// <returns>
    /// An option containing the selected values in source order, or <c>None</c> when a selector result
    /// is absent.
    /// </returns>
    /// <remarks>
    /// This method is marked <c>[Experimental("FS0017")]</c>: it carries no compatibility promise and
    /// may change or be removed until the location-context design is promoted. A new location
    /// is composed per item via <c>root.At(index)</c>. Nesting composes contexts: an inner traversal's
    /// failures carry locations relative to the inner root, and the outer level passes its item's
    /// location, such as <c>customerLocation.Property("addresses")</c>, as the inner root, so a
    /// composed path such as <c>customers[17].addresses[2].postalCode</c> emerges without application
    /// code assembling it.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/>, <paramref name="root"/>, or <paramref name="selector"/> is
    /// <see langword="null"/>.
    /// </exception>
    [Experimental("FS0017")]
    public static Option<IReadOnlyList<TResult>> Traverse<TSource, TResult>(
        this IEnumerable<TSource> source,
        Location root,
        Func<Location, TSource, Option<TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(selector);

        return source.Traverse<TSource, TResult>((index, item) => selector(root.At(index), item));
    }

    /// <summary>
    /// Applies a location-aware result-producing selector to each source item and collects the values
    /// when every result is successful.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The selected result value type.</typeparam>
    /// <typeparam name="TError">The result error type.</typeparam>
    /// <param name="source">The sequence to traverse.</param>
    /// <param name="root">The location root that each item's location is composed from.</param>
    /// <param name="selector">
    /// The location-aware result-producing selector, invoked once per reached item with the item's
    /// composed location, <c>root.At(index)</c>, and the item.
    /// </param>
    /// <returns>
    /// A successful result containing the selected values in source order, or the first selector
    /// failure.
    /// </returns>
    /// <remarks>
    /// This method is marked <c>[Experimental("FS0017")]</c>: it carries no compatibility promise and
    /// may change or be removed until the location-context design is promoted. A new location
    /// is composed per item via <c>root.At(index)</c>. Nesting composes contexts: an inner traversal's
    /// failures carry locations relative to the inner root, and the outer level passes its item's
    /// location, such as <c>customerLocation.Property("addresses")</c>, as the inner root, so a
    /// composed path such as <c>customers[17].addresses[2].postalCode</c> emerges without application
    /// code assembling it.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/>, <paramref name="root"/>, or <paramref name="selector"/> is
    /// <see langword="null"/>.
    /// </exception>
    [Experimental("FS0017")]
    public static Result<IReadOnlyList<TResult>, TError> Traverse<TSource, TResult, TError>(
        this IEnumerable<TSource> source,
        Location root,
        Func<Location, TSource, Result<TResult, TError>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(selector);

        return source.Traverse<TSource, TResult, TError>(
            (index, item) => selector(root.At(index), item));
    }

    /// <summary>
    /// Applies a location-aware unit-result-producing selector to each source item, stopping at the
    /// first failure.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="source">The sequence to traverse.</param>
    /// <param name="root">The location root that each item's location is composed from.</param>
    /// <param name="selector">
    /// The location-aware unit-result-producing selector, invoked once per reached item with the item's
    /// composed location, <c>root.At(index)</c>, and the item.
    /// </param>
    /// <returns>
    /// Success when every selector result is successful; otherwise, the first failed selector's error.
    /// </returns>
    /// <remarks>
    /// This method is marked <c>[Experimental("FS0017")]</c>: it carries no compatibility promise and
    /// may change or be removed until the location-context design is promoted. A new location
    /// is composed per item via <c>root.At(index)</c>. Nesting composes contexts: an inner traversal's
    /// failures carry locations relative to the inner root, and the outer level passes its item's
    /// location, such as <c>customerLocation.Property("addresses")</c>, as the inner root, so a
    /// composed path such as <c>customers[17].addresses[2].postalCode</c> emerges without application
    /// code assembling it.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/>, <paramref name="root"/>, or <paramref name="selector"/> is
    /// <see langword="null"/>.
    /// </exception>
    [Experimental("FS0017")]
    public static UnitResult<TError> Traverse<TSource, TError>(
        this IEnumerable<TSource> source,
        Location root,
        Func<Location, TSource, UnitResult<TError>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(selector);

        return source.Traverse<TSource, TError>(
            (index, item) => selector(root.At(index), item));
    }

    /// <summary>
    /// Applies a location-aware validation-producing selector to each source item and collects values
    /// or accumulates all validation errors.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The selected validation value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="source">The sequence to traverse.</param>
    /// <param name="root">The location root that each item's location is composed from.</param>
    /// <param name="selector">
    /// The location-aware validation-producing selector, invoked once per reached item with the item's
    /// composed location, <c>root.At(index)</c>, and the item.
    /// </param>
    /// <returns>
    /// A valid validation containing the selected values in source order, or an invalid validation
    /// containing all selector errors in source and per-validation order.
    /// </returns>
    /// <remarks>
    /// This method is marked <c>[Experimental("FS0017")]</c>: it carries no compatibility promise and
    /// may change or be removed until the location-context design is promoted. A new location
    /// is composed per item via <c>root.At(index)</c>. Nesting composes contexts: an inner traversal's
    /// failures carry locations relative to the inner root, and the outer level passes its item's
    /// location, such as <c>customerLocation.Property("addresses")</c>, as the inner root, so a
    /// composed path such as <c>customers[17].addresses[2].postalCode</c> emerges without application
    /// code assembling it.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/>, <paramref name="root"/>, or <paramref name="selector"/> is
    /// <see langword="null"/>.
    /// </exception>
    [Experimental("FS0017")]
    public static Validation<IReadOnlyList<TResult>, TError> Traverse<TSource, TResult, TError>(
        this IEnumerable<TSource> source,
        Location root,
        Func<Location, TSource, Validation<TResult, TError>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(selector);

        return source.Traverse<TSource, TResult, TError>(
            (index, item) => selector(root.At(index), item));
    }

    /// <summary>
    /// Applies a location-aware option-producing selector to each dictionary value and collects the
    /// results into a new dictionary when every result is present.
    /// </summary>
    /// <typeparam name="TKey">The dictionary key type.</typeparam>
    /// <typeparam name="TValue">The dictionary value type.</typeparam>
    /// <typeparam name="TResult">The selected option value type.</typeparam>
    /// <param name="source">The dictionary whose values to traverse.</param>
    /// <param name="root">The location root that each entry's location is composed from.</param>
    /// <param name="selector">
    /// The location-aware option-producing selector, invoked once per entry with the entry's composed
    /// location, key, and value.
    /// </param>
    /// <returns>
    /// An option containing a new dictionary mapping each source key to its selected value, or
    /// <c>None</c> when a selector result is absent.
    /// </returns>
    /// <remarks>
    /// This method is marked <c>[Experimental("FS0017")]</c>: it carries no compatibility promise and
    /// may change or be removed until the location-context design is promoted. A new location
    /// is composed per entry: a string key composes <c>root.Key(key)</c>, which quotes the key, and any
    /// other key composes <c>root.Key(key)</c> through its string representation. Success materializes
    /// a new dictionary filled in the source dictionary's enumeration order, with its capacity hinted
    /// from the source dictionary's <c>Count</c>; <c>Dictionary</c> in practice preserves insertion
    /// order, but the BCL does not contract it.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/>, <paramref name="root"/>, or <paramref name="selector"/> is
    /// <see langword="null"/>.
    /// </exception>
    [Experimental("FS0017")]
    public static Option<IReadOnlyDictionary<TKey, TResult>> Traverse<TKey, TValue, TResult>(
        this IReadOnlyDictionary<TKey, TValue> source,
        Location root,
        Func<Location, TKey, TValue, Option<TResult>> selector)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(selector);

        return source.Traverse<TKey, TValue, TResult>(
            (key, value) => selector(KeyedLocation(root, key), key, value));
    }

    /// <summary>
    /// Applies a location-aware result-producing selector to each dictionary value and collects the
    /// results into a new dictionary when every result is successful.
    /// </summary>
    /// <typeparam name="TKey">The dictionary key type.</typeparam>
    /// <typeparam name="TValue">The dictionary value type.</typeparam>
    /// <typeparam name="TResult">The selected result value type.</typeparam>
    /// <typeparam name="TError">The result error type.</typeparam>
    /// <param name="source">The dictionary whose values to traverse.</param>
    /// <param name="root">The location root that each entry's location is composed from.</param>
    /// <param name="selector">
    /// The location-aware result-producing selector, invoked once per entry with the entry's composed
    /// location, key, and value.
    /// </param>
    /// <returns>
    /// A successful result containing a new dictionary mapping each source key to its selected value,
    /// or the first selector failure.
    /// </returns>
    /// <remarks>
    /// This method is marked <c>[Experimental("FS0017")]</c>: it carries no compatibility promise and
    /// may change or be removed until the location-context design is promoted. A new location
    /// is composed per entry: a string key composes <c>root.Key(key)</c>, which quotes the key, and any
    /// other key composes <c>root.Key(key)</c> through its string representation. Success materializes
    /// a new dictionary filled in the source dictionary's enumeration order, with its capacity hinted
    /// from the source dictionary's <c>Count</c>; <c>Dictionary</c> in practice preserves insertion
    /// order, but the BCL does not contract it.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/>, <paramref name="root"/>, or <paramref name="selector"/> is
    /// <see langword="null"/>.
    /// </exception>
    [Experimental("FS0017")]
    public static Result<IReadOnlyDictionary<TKey, TResult>, TError> Traverse<TKey, TValue, TResult, TError>(
        this IReadOnlyDictionary<TKey, TValue> source,
        Location root,
        Func<Location, TKey, TValue, Result<TResult, TError>> selector)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(selector);

        return source.Traverse<TKey, TValue, TResult, TError>(
            (key, value) => selector(KeyedLocation(root, key), key, value));
    }

    /// <summary>
    /// Applies a location-aware unit-result-producing selector to each dictionary value, stopping at
    /// the first failure.
    /// </summary>
    /// <typeparam name="TKey">The dictionary key type.</typeparam>
    /// <typeparam name="TValue">The dictionary value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="source">The dictionary whose values to traverse.</param>
    /// <param name="root">The location root that each entry's location is composed from.</param>
    /// <param name="selector">
    /// The location-aware unit-result-producing selector, invoked once per entry with the entry's
    /// composed location, key, and value.
    /// </param>
    /// <returns>
    /// Success when every selector result is successful; otherwise, the first failed selector's error.
    /// </returns>
    /// <remarks>
    /// This method is marked <c>[Experimental("FS0017")]</c>: it carries no compatibility promise and
    /// may change or be removed until the location-context design is promoted. A new location
    /// is composed per entry: a string key composes <c>root.Key(key)</c>, which quotes the key, and any
    /// other key composes <c>root.Key(key)</c> through its string representation. Success materializes
    /// a new dictionary filled in the source dictionary's enumeration order, with its capacity hinted
    /// from the source dictionary's <c>Count</c>; <c>Dictionary</c> in practice preserves insertion
    /// order, but the BCL does not contract it.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/>, <paramref name="root"/>, or <paramref name="selector"/> is
    /// <see langword="null"/>.
    /// </exception>
    [Experimental("FS0017")]
    public static UnitResult<TError> Traverse<TKey, TValue, TError>(
        this IReadOnlyDictionary<TKey, TValue> source,
        Location root,
        Func<Location, TKey, TValue, UnitResult<TError>> selector)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(selector);

        return source.Traverse<TKey, TValue, TError>(
            (key, value) => selector(KeyedLocation(root, key), key, value));
    }

    /// <summary>
    /// Applies a location-aware validation-producing selector to each dictionary value and collects the
    /// results into a new dictionary or accumulates all validation errors.
    /// </summary>
    /// <typeparam name="TKey">The dictionary key type.</typeparam>
    /// <typeparam name="TValue">The dictionary value type.</typeparam>
    /// <typeparam name="TResult">The selected validation value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="source">The dictionary whose values to traverse.</param>
    /// <param name="root">The location root that each entry's location is composed from.</param>
    /// <param name="selector">
    /// The location-aware validation-producing selector, invoked once per entry with the entry's
    /// composed location, key, and value.
    /// </param>
    /// <returns>
    /// A valid validation containing a new dictionary mapping each source key to its selected value,
    /// or an invalid validation containing all selector errors in source and per-validation order.
    /// </returns>
    /// <remarks>
    /// This method is marked <c>[Experimental("FS0017")]</c>: it carries no compatibility promise and
    /// may change or be removed until the location-context design is promoted. A new location
    /// is composed per entry: a string key composes <c>root.Key(key)</c>, which quotes the key, and any
    /// other key composes <c>root.Key(key)</c> through its string representation. Success materializes
    /// a new dictionary filled in the source dictionary's enumeration order, with its capacity hinted
    /// from the source dictionary's <c>Count</c>; <c>Dictionary</c> in practice preserves insertion
    /// order, but the BCL does not contract it.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/>, <paramref name="root"/>, or <paramref name="selector"/> is
    /// <see langword="null"/>.
    /// </exception>
    [Experimental("FS0017")]
    public static Validation<IReadOnlyDictionary<TKey, TResult>, TError> Traverse<TKey, TValue, TResult, TError>(
        this IReadOnlyDictionary<TKey, TValue> source,
        Location root,
        Func<Location, TKey, TValue, Validation<TResult, TError>> selector)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(selector);

        return source.Traverse<TKey, TValue, TResult, TError>(
            (key, value) => selector(KeyedLocation(root, key), key, value));
    }

    internal static int GetInitialCapacity<T>(IEnumerable<T> source) =>
        Enumerable.TryGetNonEnumeratedCount(source, out var count) ? count : 0;

    internal static IReadOnlyList<T> ToReadOnlyList<T>(List<T>? values) =>
        values is null ? Array.Empty<T>() : values.AsReadOnly();

    private static IReadOnlyDictionary<TKey, TValue> ToReadOnlyDictionary<TKey, TValue>(
        Dictionary<TKey, TValue>? values)
        where TKey : notnull =>
        values ?? new Dictionary<TKey, TValue>();

    [Experimental("FS0017")]
    private static Location KeyedLocation<TKey>(Location root, TKey key)
        where TKey : notnull =>
        key is string text ? root.Key(text) : root.Key((object)key);
}
