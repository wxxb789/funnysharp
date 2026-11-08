namespace FunnySharp;

/// <summary>
/// Expands nested child sequences into ordinary typed rows while retaining the parent record and
/// source positions.
/// </summary>
/// <remarks>
/// <para>
/// The family has three row shapes. <see cref="Expand{TParent, TChild, TResult}"/> yields one row
/// per child. <see cref="ExpandAligned{TParent, TFirst, TSecond, TResult}"/> pairs two child
/// sequences by position and rejects unequal lengths. <see cref="ExpandCartesian{TParent, TFirst,
/// TSecond, TResult}"/> is the separately named explicit product. Aligned expansion never infers a
/// Cartesian relation.
/// </para>
/// <para>
/// Every operation is eager and all-or-nothing: it enumerates the record source once, materializes
/// the complete row list, and returns either exactly one row per expanded element in record order
/// or no rows plus every structural failure. A null child list is a
/// <see cref="NestedRowError"/>; an empty child list is valid and produces zero rows. Once an error
/// has been recorded, later records are still inspected for null and length failures, but the
/// combine callback is not invoked again; earlier invocations are not rolled back.
/// </para>
/// <para>
/// The combine callback receives a <see cref="RowOrigin"/> or
/// <see cref="CartesianRowOrigin"/> carrying the record and element positions. A structural failure
/// carries the record position and, for a mismatch, both counts; it does not carry an element
/// position. A caller that only needs the record position of a structural failure may ignore the
/// origin; a caller that records a rejection or needs an element position reads the origin. A
/// caller that needs a per-row trace stores it. No path string or <see cref="Location"/> object is
/// created per row.
/// </para>
/// <para>
/// The operations are total with respect to the caller's values: source, selector, combine,
/// enumerator, and disposal exceptions propagate unchanged and are never converted into
/// <see cref="NestedRowError"/> values. Argument nullness is rejected eagerly.
/// </para>
/// </remarks>
public static class NestedDataExtensions
{
    /// <summary>
    /// Expands each record's child list into one row per child, in record order then child order.
    /// </summary>
    /// <typeparam name="TParent">The parent record type.</typeparam>
    /// <typeparam name="TChild">The nested child type.</typeparam>
    /// <typeparam name="TResult">The output row type.</typeparam>
    /// <param name="records">The parent records to expand.</param>
    /// <param name="children">
    /// The function that selects one record's child list. It is invoked once per reached record.
    /// A runtime-null result is a <see cref="NestedRowErrorKind.NullChildSequence"/> failure; an
    /// empty list is valid.
    /// </param>
    /// <param name="combine">
    /// The function that produces one row from the parent, the child, and the row origin. It is
    /// invoked once per child, in record order then child order, and is not invoked for any child
    /// after a structural failure has been recorded.
    /// </param>
    /// <returns>
    /// A valid exact-size list with one row per child of every successfully expanded record, or no
    /// rows plus every reached structural failure in record order.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The source is enumerated exactly once. The child list is read during the call and is not
    /// cloned, snapshotted, or cached; the callback owns whatever value it returns. The output
    /// count on success is the sum of the child counts. A record with an empty child list
    /// contributes no rows and no error. A null child element, including a null parent record, is
    /// passed to <paramref name="combine"/> unchanged; the library does not filter or reject it.
    /// </para>
    /// <para>
    /// If a null child list is found, the failure carries the record position. Once any failure is
    /// recorded, later records still have their child selector invoked so further null failures are
    /// reported, but <paramref name="combine"/> is not invoked for later children. Rows already
    /// produced are discarded when the overall result is a failure.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="records"/>, <paramref name="children"/>, or <paramref name="combine"/> is
    /// <see langword="null"/>.
    /// </exception>
    public static Validation<IReadOnlyList<TResult>, NestedRowError> Expand<TParent, TChild, TResult>(
        this IEnumerable<TParent> records,
        Func<TParent, IReadOnlyList<TChild>> children,
        Func<TParent, TChild, RowOrigin, TResult> combine)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(children);
        ArgumentNullException.ThrowIfNull(combine);

        var rows = new List<TResult>(SequenceExtensions.GetInitialCapacity(records));
        List<NestedRowError>? errors = null;
        var recordIndex = 0;

        foreach (var record in records)
        {
            var childList = children(record);
            if (childList is null)
            {
                (errors ??= []).Add(NestedRowError.NullChildSequence(recordIndex));
            }
            else if (errors is null)
            {
                for (var elementIndex = 0; elementIndex < childList.Count; elementIndex++)
                {
                    rows.Add(combine(record, childList[elementIndex], new RowOrigin(recordIndex, elementIndex)));
                }
            }

            recordIndex++;
        }

        return errors is null
            ? Validation<IReadOnlyList<TResult>, NestedRowError>.Valid(rows.ToArray())
            : Validation<IReadOnlyList<TResult>, NestedRowError>.InvalidFromOwnedErrors(errors);
    }

    /// <summary>
    /// Expands each record's child list asynchronously into one row per child, using the same
    /// semantics as <see cref="Expand{TParent, TChild, TResult}"/>.
    /// </summary>
    /// <typeparam name="TParent">The parent record type.</typeparam>
    /// <typeparam name="TChild">The nested child type.</typeparam>
    /// <typeparam name="TResult">The output row type.</typeparam>
    /// <param name="records">The asynchronous parent record source.</param>
    /// <param name="children">The function that selects one record's child list.</param>
    /// <param name="combine">
    /// The function that produces one row from the parent, child, and origin. It is not invoked
    /// for any child after a structural failure has been recorded.
    /// </param>
    /// <param name="cancellationToken">
    /// The token forwarded unchanged to the asynchronous enumerator; it is not passed to the
    /// synchronous selector or combine callback.
    /// </param>
    /// <returns>
    /// A value task that produces a valid exact-size row list or no rows plus every reached
    /// structural failure, matching the synchronous form.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The source is enumerated exactly once and each awaitable is observed once. Nothing blocks.
    /// The operation begins enumerating synchronously up to the first incomplete await, like an
    /// ordinary <c>async</c> method, so source side effects can occur before the caller awaits the
    /// returned value task. The returned value task is consumed once.
    /// </para>
    /// <para>
    /// Cancellation, source faults, selector exceptions, combine exceptions, uninitialized values,
    /// and disposal exceptions keep normal asynchronous behavior; they are never converted into
    /// <see cref="NestedRowError"/> values. Local rows and errors are discarded when an exception or
    /// cancellation replaces the outcome.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="records"/>, <paramref name="children"/>, or <paramref name="combine"/> is
    /// <see langword="null"/>.
    /// </exception>
    public static ValueTask<Validation<IReadOnlyList<TResult>, NestedRowError>> ExpandAsync<TParent, TChild, TResult>(
        this IAsyncEnumerable<TParent> records,
        Func<TParent, IReadOnlyList<TChild>> children,
        Func<TParent, TChild, RowOrigin, TResult> combine,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(children);
        ArgumentNullException.ThrowIfNull(combine);

        return ExpandAsyncCore(records, children, combine, cancellationToken);
    }

    /// <summary>
    /// Pairs two child sequences position by position for every record and expands the pairs into
    /// rows.
    /// </summary>
    /// <typeparam name="TParent">The parent record type.</typeparam>
    /// <typeparam name="TFirst">The first child sequence's element type.</typeparam>
    /// <typeparam name="TSecond">The second child sequence's element type.</typeparam>
    /// <typeparam name="TResult">The output row type.</typeparam>
    /// <param name="records">The parent records to expand.</param>
    /// <param name="firstChildren">The first child-sequence selector, invoked once per record.</param>
    /// <param name="secondChildren">The second child-sequence selector, invoked once per record.</param>
    /// <param name="combine">
    /// The function that produces one row from the parent, the element at the current index in each
    /// child sequence, and the shared index origin. It is invoked once per aligned index in record
    /// order, and is not invoked after a structural failure has been recorded.
    /// </param>
    /// <returns>
    /// A valid exact-size list with one row per index of every successfully aligned record, or no
    /// rows plus every reached structural failure in record order.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is the exact aligned shape. Both selectors are invoked once per reached record. When
    /// both lists are non-null their lengths must be equal; an unequal pair is one
    /// <see cref="NestedRowErrorKind.ChildCountMismatch"/> carrying the record position and both
    /// counts, and contributes no rows. Two empty lists are equal and contribute no rows. The
    /// output order is record order then increasing child index. A two-element pair set produces
    /// two rows, never four. A null element in either non-null list, including a null parent
    /// record, is passed to <paramref name="combine"/> unchanged; the library does not filter or
    /// reject it.
    /// </para>
    /// <para>
    /// A null first list is <see cref="NestedRowErrorKind.NullFirstChildSequence"/>; a null second
    /// list is <see cref="NestedRowErrorKind.NullSecondChildSequence"/>. Both selectors are still
    /// evaluated so both failures can be reported for the same record. Once any failure is
    /// recorded, later records are still checked for null and length failures, but
    /// <paramref name="combine"/> is not invoked again. Earlier rows are discarded on failure.
    /// </para>
    /// <para>
    /// This operation never infers a Cartesian product. Use
    /// <see cref="ExpandCartesian{TParent, TFirst, TSecond, TResult}"/> when the product is the
    /// intended relation, and BCL <c>Zip</c> only when silent truncation is acceptable.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="records"/>, <paramref name="firstChildren"/>,
    /// <paramref name="secondChildren"/>, or <paramref name="combine"/> is <see langword="null"/>.
    /// </exception>
    public static Validation<IReadOnlyList<TResult>, NestedRowError> ExpandAligned<TParent, TFirst, TSecond, TResult>(
        this IEnumerable<TParent> records,
        Func<TParent, IReadOnlyList<TFirst>> firstChildren,
        Func<TParent, IReadOnlyList<TSecond>> secondChildren,
        Func<TParent, TFirst, TSecond, RowOrigin, TResult> combine)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(firstChildren);
        ArgumentNullException.ThrowIfNull(secondChildren);
        ArgumentNullException.ThrowIfNull(combine);

        var rows = new List<TResult>(SequenceExtensions.GetInitialCapacity(records));
        List<NestedRowError>? errors = null;
        var recordIndex = 0;

        foreach (var record in records)
        {
            var first = firstChildren(record);
            var second = secondChildren(record);
            var failed = false;

            if (first is null)
            {
                (errors ??= []).Add(NestedRowError.NullFirstChildSequence(recordIndex));
                failed = true;
            }

            if (second is null)
            {
                (errors ??= []).Add(NestedRowError.NullSecondChildSequence(recordIndex));
                failed = true;
            }

            if (!failed)
            {
                if (first!.Count != second!.Count)
                {
                    (errors ??= []).Add(
                        NestedRowError.ChildCountMismatch(recordIndex, first.Count, second.Count));
                }
                else if (errors is null)
                {
                    for (var elementIndex = 0; elementIndex < first.Count; elementIndex++)
                    {
                        rows.Add(combine(
                            record,
                            first[elementIndex],
                            second[elementIndex],
                            new RowOrigin(recordIndex, elementIndex)));
                    }
                }
            }

            recordIndex++;
        }

        return errors is null
            ? Validation<IReadOnlyList<TResult>, NestedRowError>.Valid(rows.ToArray())
            : Validation<IReadOnlyList<TResult>, NestedRowError>.InvalidFromOwnedErrors(errors);
    }

    /// <summary>
    /// Pairs two child sequences position by position asynchronously, using the same semantics as
    /// <see cref="ExpandAligned{TParent, TFirst, TSecond, TResult}"/>.
    /// </summary>
    /// <typeparam name="TParent">The parent record type.</typeparam>
    /// <typeparam name="TFirst">The first child sequence's element type.</typeparam>
    /// <typeparam name="TSecond">The second child sequence's element type.</typeparam>
    /// <typeparam name="TResult">The output row type.</typeparam>
    /// <param name="records">The asynchronous parent record source.</param>
    /// <param name="firstChildren">The first child-sequence selector.</param>
    /// <param name="secondChildren">The second child-sequence selector.</param>
    /// <param name="combine">
    /// The function that produces one aligned row. It is not invoked after a structural failure has
    /// been recorded.
    /// </param>
    /// <param name="cancellationToken">
    /// The token forwarded unchanged to the asynchronous enumerator; it is not passed to the
    /// synchronous selectors or combine callback.
    /// </param>
    /// <returns>
    /// A value task that produces the same rows and failures as the synchronous form.
    /// </returns>
    /// <remarks>
    /// The source is enumerated once, no await is blocked on, and the returned value task is
    /// consumed once. Cancellation, faults, selector exceptions, combine exceptions, and disposal
    /// keep normal asynchronous behavior and are never converted into
    /// <see cref="NestedRowError"/> values.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="records"/>, <paramref name="firstChildren"/>,
    /// <paramref name="secondChildren"/>, or <paramref name="combine"/> is <see langword="null"/>.
    /// </exception>
    public static ValueTask<Validation<IReadOnlyList<TResult>, NestedRowError>> ExpandAlignedAsync<TParent, TFirst, TSecond, TResult>(
        this IAsyncEnumerable<TParent> records,
        Func<TParent, IReadOnlyList<TFirst>> firstChildren,
        Func<TParent, IReadOnlyList<TSecond>> secondChildren,
        Func<TParent, TFirst, TSecond, RowOrigin, TResult> combine,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(firstChildren);
        ArgumentNullException.ThrowIfNull(secondChildren);
        ArgumentNullException.ThrowIfNull(combine);

        return ExpandAlignedAsyncCore(
            records,
            firstChildren,
            secondChildren,
            combine,
            cancellationToken);
    }

    /// <summary>
    /// Expands the explicit product of two child sequences for every record into rows.
    /// </summary>
    /// <typeparam name="TParent">The parent record type.</typeparam>
    /// <typeparam name="TFirst">The first child sequence's element type.</typeparam>
    /// <typeparam name="TSecond">The second child sequence's element type.</typeparam>
    /// <typeparam name="TResult">The output row type.</typeparam>
    /// <param name="records">The parent records to expand.</param>
    /// <param name="firstChildren">The first child-sequence selector, invoked once per record.</param>
    /// <param name="secondChildren">The second child-sequence selector, invoked once per record.</param>
    /// <param name="combine">
    /// The function that produces one row from the parent, one first-sequence element, one
    /// second-sequence element, and the two-index origin. It is invoked once per product row in
    /// first-index then second-index order, and is not invoked after a structural failure has been
    /// recorded.
    /// </param>
    /// <returns>
    /// A valid exact-size list containing every combination for every successfully reached record,
    /// or no rows plus every reached structural failure in record order.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is the explicitly named Cartesian relation. For each record, the rows are ordered by
    /// first index ascending and then second index ascending; the output count is the sum of
    /// <c>first.Count * second.Count</c> over successfully reached records. Unequal lengths are
    /// valid and never produce a <see cref="NestedRowErrorKind.ChildCountMismatch"/>.
    /// </para>
    /// <para>
    /// A null first list is <see cref="NestedRowErrorKind.NullFirstChildSequence"/>; a null second
    /// list is <see cref="NestedRowErrorKind.NullSecondChildSequence"/>. Both selectors are still
    /// evaluated for the record. Once any failure is recorded, later records are still checked for
    /// null lists, but <paramref name="combine"/> is not invoked again. An empty list on either
    /// side contributes no rows and no error. A null element in either non-null list, including a
    /// null parent record, is passed to <paramref name="combine"/> unchanged; the library does not
    /// filter or reject it. Aligned expansion remains the operation that rejects
    /// unequal lengths; this operation never changes into aligned behavior.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="records"/>, <paramref name="firstChildren"/>,
    /// <paramref name="secondChildren"/>, or <paramref name="combine"/> is <see langword="null"/>.
    /// </exception>
    public static Validation<IReadOnlyList<TResult>, NestedRowError> ExpandCartesian<TParent, TFirst, TSecond, TResult>(
        this IEnumerable<TParent> records,
        Func<TParent, IReadOnlyList<TFirst>> firstChildren,
        Func<TParent, IReadOnlyList<TSecond>> secondChildren,
        Func<TParent, TFirst, TSecond, CartesianRowOrigin, TResult> combine)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(firstChildren);
        ArgumentNullException.ThrowIfNull(secondChildren);
        ArgumentNullException.ThrowIfNull(combine);

        var rows = new List<TResult>(SequenceExtensions.GetInitialCapacity(records));
        List<NestedRowError>? errors = null;
        var recordIndex = 0;

        foreach (var record in records)
        {
            var first = firstChildren(record);
            var second = secondChildren(record);
            var failed = false;

            if (first is null)
            {
                (errors ??= []).Add(NestedRowError.NullFirstChildSequence(recordIndex));
                failed = true;
            }

            if (second is null)
            {
                (errors ??= []).Add(NestedRowError.NullSecondChildSequence(recordIndex));
                failed = true;
            }

            if (!failed && errors is null)
            {
                for (var firstIndex = 0; firstIndex < first!.Count; firstIndex++)
                {
                    for (var secondIndex = 0; secondIndex < second!.Count; secondIndex++)
                    {
                        rows.Add(combine(
                            record,
                            first[firstIndex],
                            second[secondIndex],
                            new CartesianRowOrigin(recordIndex, firstIndex, secondIndex)));
                    }
                }
            }

            recordIndex++;
        }

        return errors is null
            ? Validation<IReadOnlyList<TResult>, NestedRowError>.Valid(rows.ToArray())
            : Validation<IReadOnlyList<TResult>, NestedRowError>.InvalidFromOwnedErrors(errors);
    }

    /// <summary>
    /// Expands the explicit product of two child sequences asynchronously, using the same semantics
    /// as <see cref="ExpandCartesian{TParent, TFirst, TSecond, TResult}"/>.
    /// </summary>
    /// <typeparam name="TParent">The parent record type.</typeparam>
    /// <typeparam name="TFirst">The first child sequence's element type.</typeparam>
    /// <typeparam name="TSecond">The second child sequence's element type.</typeparam>
    /// <typeparam name="TResult">The output row type.</typeparam>
    /// <param name="records">The asynchronous parent record source.</param>
    /// <param name="firstChildren">The first child-sequence selector.</param>
    /// <param name="secondChildren">The second child-sequence selector.</param>
    /// <param name="combine">
    /// The function that produces one Cartesian row. It is not invoked after a structural failure
    /// has been recorded.
    /// </param>
    /// <param name="cancellationToken">
    /// The token forwarded unchanged to the asynchronous enumerator; it is not passed to the
    /// synchronous selectors or combine callback.
    /// </param>
    /// <returns>
    /// A value task that produces the same rows and failures as the synchronous form.
    /// </returns>
    /// <remarks>
    /// The source is enumerated once, no await is blocked on, and the returned value task is
    /// consumed once. Cancellation, faults, selector exceptions, combine exceptions, and disposal
    /// keep normal asynchronous behavior and are never converted into
    /// <see cref="NestedRowError"/> values.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="records"/>, <paramref name="firstChildren"/>,
    /// <paramref name="secondChildren"/>, or <paramref name="combine"/> is <see langword="null"/>.
    /// </exception>
    public static ValueTask<Validation<IReadOnlyList<TResult>, NestedRowError>> ExpandCartesianAsync<TParent, TFirst, TSecond, TResult>(
        this IAsyncEnumerable<TParent> records,
        Func<TParent, IReadOnlyList<TFirst>> firstChildren,
        Func<TParent, IReadOnlyList<TSecond>> secondChildren,
        Func<TParent, TFirst, TSecond, CartesianRowOrigin, TResult> combine,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(firstChildren);
        ArgumentNullException.ThrowIfNull(secondChildren);
        ArgumentNullException.ThrowIfNull(combine);

        return ExpandCartesianAsyncCore(
            records,
            firstChildren,
            secondChildren,
            combine,
            cancellationToken);
    }

    private static async ValueTask<Validation<IReadOnlyList<TResult>, NestedRowError>> ExpandAsyncCore<TParent, TChild, TResult>(
        IAsyncEnumerable<TParent> records,
        Func<TParent, IReadOnlyList<TChild>> children,
        Func<TParent, TChild, RowOrigin, TResult> combine,
        CancellationToken cancellationToken)
    {
        var rows = new List<TResult>();
        List<NestedRowError>? errors = null;
        var recordIndex = 0;

        await foreach (var record in records.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var childList = children(record);
            if (childList is null)
            {
                (errors ??= []).Add(NestedRowError.NullChildSequence(recordIndex));
            }
            else if (errors is null)
            {
                for (var elementIndex = 0; elementIndex < childList.Count; elementIndex++)
                {
                    rows.Add(combine(record, childList[elementIndex], new RowOrigin(recordIndex, elementIndex)));
                }
            }

            recordIndex++;
        }

        return errors is null
            ? Validation<IReadOnlyList<TResult>, NestedRowError>.Valid(rows.ToArray())
            : Validation<IReadOnlyList<TResult>, NestedRowError>.InvalidFromOwnedErrors(errors);
    }

    private static async ValueTask<Validation<IReadOnlyList<TResult>, NestedRowError>> ExpandAlignedAsyncCore<TParent, TFirst, TSecond, TResult>(
        IAsyncEnumerable<TParent> records,
        Func<TParent, IReadOnlyList<TFirst>> firstChildren,
        Func<TParent, IReadOnlyList<TSecond>> secondChildren,
        Func<TParent, TFirst, TSecond, RowOrigin, TResult> combine,
        CancellationToken cancellationToken)
    {
        var rows = new List<TResult>();
        List<NestedRowError>? errors = null;
        var recordIndex = 0;

        await foreach (var record in records.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var first = firstChildren(record);
            var second = secondChildren(record);
            var failed = false;

            if (first is null)
            {
                (errors ??= []).Add(NestedRowError.NullFirstChildSequence(recordIndex));
                failed = true;
            }

            if (second is null)
            {
                (errors ??= []).Add(NestedRowError.NullSecondChildSequence(recordIndex));
                failed = true;
            }

            if (!failed)
            {
                if (first!.Count != second!.Count)
                {
                    (errors ??= []).Add(
                        NestedRowError.ChildCountMismatch(recordIndex, first.Count, second.Count));
                }
                else if (errors is null)
                {
                    for (var elementIndex = 0; elementIndex < first.Count; elementIndex++)
                    {
                        rows.Add(combine(
                            record,
                            first[elementIndex],
                            second[elementIndex],
                            new RowOrigin(recordIndex, elementIndex)));
                    }
                }
            }

            recordIndex++;
        }

        return errors is null
            ? Validation<IReadOnlyList<TResult>, NestedRowError>.Valid(rows.ToArray())
            : Validation<IReadOnlyList<TResult>, NestedRowError>.InvalidFromOwnedErrors(errors);
    }

    private static async ValueTask<Validation<IReadOnlyList<TResult>, NestedRowError>> ExpandCartesianAsyncCore<TParent, TFirst, TSecond, TResult>(
        IAsyncEnumerable<TParent> records,
        Func<TParent, IReadOnlyList<TFirst>> firstChildren,
        Func<TParent, IReadOnlyList<TSecond>> secondChildren,
        Func<TParent, TFirst, TSecond, CartesianRowOrigin, TResult> combine,
        CancellationToken cancellationToken)
    {
        var rows = new List<TResult>();
        List<NestedRowError>? errors = null;
        var recordIndex = 0;

        await foreach (var record in records.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var first = firstChildren(record);
            var second = secondChildren(record);
            var failed = false;

            if (first is null)
            {
                (errors ??= []).Add(NestedRowError.NullFirstChildSequence(recordIndex));
                failed = true;
            }

            if (second is null)
            {
                (errors ??= []).Add(NestedRowError.NullSecondChildSequence(recordIndex));
                failed = true;
            }

            if (!failed && errors is null)
            {
                for (var firstIndex = 0; firstIndex < first!.Count; firstIndex++)
                {
                    for (var secondIndex = 0; secondIndex < second!.Count; secondIndex++)
                    {
                        rows.Add(combine(
                            record,
                            first[firstIndex],
                            second[secondIndex],
                            new CartesianRowOrigin(recordIndex, firstIndex, secondIndex)));
                    }
                }
            }

            recordIndex++;
        }

        return errors is null
            ? Validation<IReadOnlyList<TResult>, NestedRowError>.Valid(rows.ToArray())
            : Validation<IReadOnlyList<TResult>, NestedRowError>.InvalidFromOwnedErrors(errors);
    }
}
