namespace FunnySharp;

/// <summary>
/// Provides exact-length combination operations for sequences, which report a length mismatch
/// instead of truncating to the shorter side like BCL <c>Enumerable.Zip</c>.
/// </summary>
public static class ExactZipExtensions
{
    /// <summary>
    /// Combines two sequences positionally when their lengths are equal.
    /// </summary>
    /// <typeparam name="TFirst">The first sequence's item type.</typeparam>
    /// <typeparam name="TSecond">The second sequence's item type.</typeparam>
    /// <param name="first">The first sequence.</param>
    /// <param name="second">The second sequence.</param>
    /// <returns>
    /// An option containing the pairs in source order when both sequences have the same length;
    /// otherwise, <c>None</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="first"/> or <paramref name="second"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// BCL <c>Enumerable.Zip</c> truncates to the shorter side; this operation is exact, so absence
    /// (<c>None</c>) can only mean the lengths differ. When the distinction matters, use
    /// <see cref="ZipExact{TFirst, TSecond, TError}(IEnumerable{TFirst}, IEnumerable{TSecond}, Func{int, int, TError})"/>,
    /// which reports both counts. Each sequence is enumerated exactly once and to its end, the
    /// pairs are materialized in source order, the lockstep combination is iterative so large
    /// inputs are safe, and source exceptions propagate unchanged.
    /// </remarks>
    public static Option<IReadOnlyList<(TFirst First, TSecond Second)>> ZipExactOrNone<TFirst, TSecond>(
        this IEnumerable<TFirst> first,
        IEnumerable<TSecond> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        var outcome = ZipExactCore(first, second);
        return outcome.FirstCount == outcome.SecondCount
            ? Option<IReadOnlyList<(TFirst First, TSecond Second)>>.Some(outcome.Pairs.AsReadOnly())
            : Option<IReadOnlyList<(TFirst First, TSecond Second)>>.None;
    }

    /// <summary>
    /// Combines two sequences positionally when their lengths are equal, or reports both lengths
    /// when they are not.
    /// </summary>
    /// <typeparam name="TFirst">The first sequence's item type.</typeparam>
    /// <typeparam name="TSecond">The second sequence's item type.</typeparam>
    /// <typeparam name="TError">The length-mismatch error type.</typeparam>
    /// <param name="first">The first sequence.</param>
    /// <param name="second">The second sequence.</param>
    /// <param name="lengthError">
    /// The delegate that produces the failure from the total counts of <paramref name="first"/>
    /// and <paramref name="second"/>, invoked only when the lengths differ.
    /// </param>
    /// <returns>
    /// A successful result containing the pairs in source order when both sequences have the same
    /// length; otherwise, a failure containing the result of <paramref name="lengthError"/> for
    /// the two total counts.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="first"/>, <paramref name="second"/>, or <paramref name="lengthError"/> is
    /// <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// BCL <c>Enumerable.Zip</c> truncates to the shorter side; this operation is exact.
    /// <paramref name="lengthError"/> is validated eagerly but invoked only when the lengths
    /// differ, and the failure it produces receives the total counts of both sequences, not the
    /// number of paired items. Reporting exact totals requires enumerating the longer side to its
    /// end, so the unequal-length path fully enumerates both sequences once; the equal path
    /// enumerates both once. Each sequence is enumerated exactly once, the pairs are materialized
    /// in source order, the lockstep combination is iterative so large inputs are safe, and
    /// <paramref name="lengthError"/> and source exceptions propagate unchanged.
    /// </remarks>
    public static Result<IReadOnlyList<(TFirst First, TSecond Second)>, TError> ZipExact<TFirst, TSecond, TError>(
        this IEnumerable<TFirst> first,
        IEnumerable<TSecond> second,
        Func<int, int, TError> lengthError)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(lengthError);

        var outcome = ZipExactCore(first, second);
        if (outcome.FirstCount != outcome.SecondCount)
        {
            return Result<IReadOnlyList<(TFirst First, TSecond Second)>, TError>.Failure(
                lengthError(outcome.FirstCount, outcome.SecondCount));
        }

        return Result<IReadOnlyList<(TFirst First, TSecond Second)>, TError>.Success(
            outcome.Pairs.AsReadOnly());
    }

    private static ZipOutcome<TFirst, TSecond> ZipExactCore<TFirst, TSecond>(
        IEnumerable<TFirst> first,
        IEnumerable<TSecond> second)
    {
        var pairs = new List<(TFirst First, TSecond Second)>(
            Math.Min(GetInitialCapacity(first), GetInitialCapacity(second)));

        using var firstEnumerator = first.GetEnumerator();
        using var secondEnumerator = second.GetEnumerator();

        var hasFirst = firstEnumerator.MoveNext();
        var hasSecond = secondEnumerator.MoveNext();
        var count = 0;
        while (hasFirst && hasSecond)
        {
            pairs.Add((firstEnumerator.Current, secondEnumerator.Current));
            count++;
            hasFirst = firstEnumerator.MoveNext();
            hasSecond = secondEnumerator.MoveNext();
        }

        var firstCount = count;
        if (hasFirst)
        {
            firstCount++;
            while (firstEnumerator.MoveNext())
            {
                firstCount++;
            }
        }

        var secondCount = count;
        if (hasSecond)
        {
            secondCount++;
            while (secondEnumerator.MoveNext())
            {
                secondCount++;
            }
        }

        return new(pairs, firstCount, secondCount);
    }

    private static int GetInitialCapacity<T>(IEnumerable<T> source) =>
        Enumerable.TryGetNonEnumeratedCount(source, out var count) ? count : 0;

    private readonly record struct ZipOutcome<TFirst, TSecond>(
        List<(TFirst First, TSecond Second)> Pairs,
        int FirstCount,
        int SecondCount);
}
