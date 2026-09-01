using System.Runtime.CompilerServices;

namespace FunnySharp;

/// <summary>
/// Provides immediate, caller-buffered data-pipeline operations for spans and memory.
/// </summary>
public static class SpanPipelineExtensions
{
    /// <summary>
    /// Projects every source item into the destination and returns the written destination prefix.
    /// </summary>
    /// <remarks>The source and destination must not overlap.</remarks>
    public static Span<TResult> SelectTo<TSource, TResult>(
        this scoped ReadOnlySpan<TSource> source,
        Span<TResult> destination,
        Func<TSource, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        EnsureDestinationCapacity(source.Length, destination.Length);

        for (var index = 0; index < source.Length; index++)
        {
            destination[index] = selector(source[index]);
        }

        return destination[..source.Length];
    }

    /// <summary>
    /// Projects every source item into the destination and returns the written destination prefix.
    /// </summary>
    /// <remarks>The source and destination must not overlap.</remarks>
    public static Span<TResult> SelectTo<TSource, TResult>(
        this scoped Span<TSource> source,
        Span<TResult> destination,
        Func<TSource, TResult> selector) =>
        ((ReadOnlySpan<TSource>)source).SelectTo(destination, selector);

    /// <summary>
    /// Copies matching source items into the destination in source order and returns the written prefix.
    /// </summary>
    /// <remarks>The source and destination must not overlap. Use <see cref="WhereInPlace{T}(Span{T}, Func{T, bool})"/> to compact one span.</remarks>
    public static Span<T> WhereTo<T>(
        this scoped ReadOnlySpan<T> source,
        Span<T> destination,
        Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        EnsureDestinationCapacity(source.Length, destination.Length);

        var written = 0;
        for (var index = 0; index < source.Length; index++)
        {
            var value = source[index];
            if (predicate(value))
            {
                destination[written++] = value;
            }
        }

        return destination[..written];
    }

    /// <summary>
    /// Copies matching source items into the destination in source order and returns the written prefix.
    /// </summary>
    /// <remarks>The source and destination must not overlap. Use <see cref="WhereInPlace{T}(Span{T}, Func{T, bool})"/> to compact one span.</remarks>
    public static Span<T> WhereTo<T>(
        this scoped Span<T> source,
        Span<T> destination,
        Func<T, bool> predicate) =>
        ((ReadOnlySpan<T>)source).WhereTo(destination, predicate);

    /// <summary>
    /// Applies a chooser once per source item, writes every present result, and returns the written prefix.
    /// </summary>
    /// <remarks>The source and destination must not overlap.</remarks>
    public static Span<TResult> ChooseTo<TSource, TResult>(
        this scoped ReadOnlySpan<TSource> source,
        Span<TResult> destination,
        Func<TSource, Option<TResult>> chooser)
    {
        ArgumentNullException.ThrowIfNull(chooser);
        EnsureDestinationCapacity(source.Length, destination.Length);

        var written = 0;
        for (var index = 0; index < source.Length; index++)
        {
            if (chooser(source[index]).TryGetValue(out var value))
            {
                destination[written++] = value!;
            }
        }

        return destination[..written];
    }

    /// <summary>
    /// Applies a chooser once per source item, writes every present result, and returns the written prefix.
    /// </summary>
    /// <remarks>The source and destination must not overlap.</remarks>
    public static Span<TResult> ChooseTo<TSource, TResult>(
        this scoped Span<TSource> source,
        Span<TResult> destination,
        Func<TSource, Option<TResult>> chooser) =>
        ((ReadOnlySpan<TSource>)source).ChooseTo(destination, chooser);

    /// <summary>
    /// Projects every item in place and returns the same span.
    /// </summary>
    public static Span<T> SelectInPlace<T>(this Span<T> source, Func<T, T> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        for (var index = 0; index < source.Length; index++)
        {
            source[index] = selector(source[index]);
        }

        return source;
    }

    /// <summary>
    /// Stably compacts matching items in place and returns the valid source prefix.
    /// </summary>
    /// <remarks>Items beyond the returned prefix have unspecified values.</remarks>
    public static Span<T> WhereInPlace<T>(this Span<T> source, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var written = 0;
        for (var index = 0; index < source.Length; index++)
        {
            var value = source[index];
            if (predicate(value))
            {
                if (written != index)
                {
                    source[written] = value;
                }

                written++;
            }
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            source[written..].Clear();
        }

        return source[..written];
    }

    /// <summary>
    /// Projects every source item into the destination and returns the written destination prefix.
    /// </summary>
    /// <remarks>The source and destination must not overlap.</remarks>
    public static Memory<TResult> SelectTo<TSource, TResult>(
        this ReadOnlyMemory<TSource> source,
        Memory<TResult> destination,
        Func<TSource, TResult> selector)
    {
        var written = source.Span.SelectTo(destination.Span, selector).Length;
        return destination[..written];
    }

    /// <summary>
    /// Projects every source item into the destination and returns the written destination prefix.
    /// </summary>
    /// <remarks>The source and destination must not overlap.</remarks>
    public static Memory<TResult> SelectTo<TSource, TResult>(
        this Memory<TSource> source,
        Memory<TResult> destination,
        Func<TSource, TResult> selector) =>
        ((ReadOnlyMemory<TSource>)source).SelectTo(destination, selector);

    /// <summary>
    /// Copies matching source items into the destination in source order and returns the written prefix.
    /// </summary>
    /// <remarks>The source and destination must not overlap. Use <see cref="WhereInPlace{T}(Memory{T}, Func{T, bool})"/> to compact one memory region.</remarks>
    public static Memory<T> WhereTo<T>(
        this ReadOnlyMemory<T> source,
        Memory<T> destination,
        Func<T, bool> predicate)
    {
        var written = source.Span.WhereTo(destination.Span, predicate).Length;
        return destination[..written];
    }

    /// <summary>
    /// Copies matching source items into the destination in source order and returns the written prefix.
    /// </summary>
    /// <remarks>The source and destination must not overlap. Use <see cref="WhereInPlace{T}(Memory{T}, Func{T, bool})"/> to compact one memory region.</remarks>
    public static Memory<T> WhereTo<T>(
        this Memory<T> source,
        Memory<T> destination,
        Func<T, bool> predicate) =>
        ((ReadOnlyMemory<T>)source).WhereTo(destination, predicate);

    /// <summary>
    /// Applies a chooser once per source item, writes every present result, and returns the written prefix.
    /// </summary>
    /// <remarks>The source and destination must not overlap.</remarks>
    public static Memory<TResult> ChooseTo<TSource, TResult>(
        this ReadOnlyMemory<TSource> source,
        Memory<TResult> destination,
        Func<TSource, Option<TResult>> chooser)
    {
        var written = source.Span.ChooseTo(destination.Span, chooser).Length;
        return destination[..written];
    }

    /// <summary>
    /// Applies a chooser once per source item, writes every present result, and returns the written prefix.
    /// </summary>
    /// <remarks>The source and destination must not overlap.</remarks>
    public static Memory<TResult> ChooseTo<TSource, TResult>(
        this Memory<TSource> source,
        Memory<TResult> destination,
        Func<TSource, Option<TResult>> chooser) =>
        ((ReadOnlyMemory<TSource>)source).ChooseTo(destination, chooser);

    /// <summary>
    /// Projects every item in place and returns the same memory region.
    /// </summary>
    public static Memory<T> SelectInPlace<T>(this Memory<T> source, Func<T, T> selector)
    {
        source.Span.SelectInPlace(selector);
        return source;
    }

    /// <summary>
    /// Stably compacts matching items in place and returns the valid memory prefix.
    /// </summary>
    /// <remarks>Items beyond the returned prefix have unspecified values.</remarks>
    public static Memory<T> WhereInPlace<T>(this Memory<T> source, Func<T, bool> predicate)
    {
        var written = source.Span.WhereInPlace(predicate).Length;
        return source[..written];
    }

    private static void EnsureDestinationCapacity(int sourceLength, int destinationLength)
    {
        if (destinationLength < sourceLength)
        {
            throw new ArgumentException(
                "The destination must have space for every source item.",
                "destination");
        }
    }
}
