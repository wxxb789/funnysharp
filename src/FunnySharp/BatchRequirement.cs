namespace FunnySharp;

/// <summary>
/// Creates and composes typed, deferred batch read requirements.
/// </summary>
/// <remarks>
/// A requirement is a description, not a running query. Build one from values, source reads, and
/// composition, then hand it to <see cref="BatchScope.ExecuteAsync{TValue, TError}"/>. Independent
/// reads are grouped by source and key when the execution can reach them; dependent reads made by
/// <see cref="BatchRequirement{TValue, TError}.Bind{TResult}"/> wait for their inputs. The scope
/// owns all caching and bounds; the description owns none.
/// </remarks>
public static class BatchRequirement
{
    /// <summary>Creates a requirement that succeeds with the supplied value.</summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The source's typed failure value.</typeparam>
    public static BatchRequirement<TValue, TError> FromValue<TValue, TError>(TValue value) =>
        new(new BatchValueNode<TValue, TError>(value));

    /// <summary>Creates a requirement that reads one key from a batch-capable source.</summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The source's typed failure value.</typeparam>
    /// <param name="source">The explicit batch source.</param>
    /// <param name="key">The key to read.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or
    /// <paramref name="key"/> is null.</exception>
    public static BatchRequirement<TValue, TError> Read<TKey, TValue, TError>(
        BatchSource<TKey, TValue, TError> source,
        TKey key)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(key);
        return new(new BatchReadNode<TKey, TValue, TError>(source, key));
    }

    /// <summary>Creates a requirement that fails without reading a source.</summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The source's typed failure value.</typeparam>
    /// <param name="error">The typed failure.</param>
    public static BatchRequirement<TValue, TError> Failure<TValue, TError>(BatchReadError<TError> error) =>
        new(new BatchFailureNode<TValue, TError>(error));

    /// <summary>
    /// Starts every supplied requirement and preserves each outcome in input order.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The source's typed failure value.</typeparam>
    /// <param name="requirements">The requirements to evaluate independently.</param>
    /// <returns>A requirement whose value is one result per input, in input order. The outer
    /// result succeeds even when an inner requirement fails; this operation does not short-circuit
    /// independent branches.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="requirements"/> is null or
    /// contains null.</exception>
    /// <remarks>The input sequence is enumerated once when this method is called. Its outcomes are
    /// not retained by this description beyond the snapshot.</remarks>
    public static BatchRequirement<IReadOnlyList<Result<TValue, BatchReadError<TError>>>, TError> All<TValue, TError>(
        IEnumerable<BatchRequirement<TValue, TError>> requirements)
    {
        ArgumentNullException.ThrowIfNull(requirements);

        var snapshot = requirements.ToArray();
        for (var index = 0; index < snapshot.Length; index++)
        {
            if (snapshot[index] is null)
            {
                throw new ArgumentNullException(
                    nameof(requirements),
                    "The requirement sequence contains null.");
            }
        }

        return new(new BatchAllNode<TValue, TError>(snapshot));
    }

    /// <summary>
    /// Starts two requirements independently and combines their successful values.
    /// </summary>
    /// <typeparam name="TFirst">The first successful value type.</typeparam>
    /// <typeparam name="TSecond">The second successful value type.</typeparam>
    /// <typeparam name="TError">The source's typed failure value.</typeparam>
    /// <param name="first">The first requirement.</param>
    /// <param name="second">The second requirement.</param>
    /// <returns>A requirement that succeeds with both values, or fails with the first failure in
    /// operand order. Both requirements start before either failure is observed, so an independent
    /// read is never cancelled merely because its sibling failed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="first"/> or
    /// <paramref name="second"/> is null.</exception>
    public static BatchRequirement<(TFirst First, TSecond Second), TError> Zip<TFirst, TSecond, TError>(
        BatchRequirement<TFirst, TError> first,
        BatchRequirement<TSecond, TError> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        return new(new BatchZipNode<TFirst, TSecond, TError>(first, second));
    }
}

/// <summary>
/// A typed, deferred batch read requirement.
/// </summary>
/// <typeparam name="TValue">The successful value type.</typeparam>
/// <typeparam name="TError">The source's typed failure value.</typeparam>
/// <remarks>
/// The requirement has no execution state and can be reused across scopes.
/// </remarks>
public sealed class BatchRequirement<TValue, TError>
{
    private readonly BatchRequirementNode<TValue, TError> _node;

    internal BatchRequirement(BatchRequirementNode<TValue, TError> node)
    {
        _node = node;
    }

    /// <summary>Transforms a successful value while preserving the requirement's failure.</summary>
    /// <typeparam name="TResult">The transformed value type.</typeparam>
    /// <param name="selector">The transformation applied only to a successful value.</param>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
    public BatchRequirement<TResult, TError> Map<TResult>(Func<TValue, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return new(new BatchMapNode<TValue, TResult, TError>(_node, selector));
    }

    /// <summary>Starts another requirement independently and combines both successful values.</summary>
    /// <typeparam name="TResult">The other requirement's successful value type.</typeparam>
    /// <param name="other">The other requirement.</param>
    /// <returns>A requirement that starts both operands and reports the first failure in operand
    /// order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is null.</exception>
    public BatchRequirement<(TValue First, TResult Second), TError> Zip<TResult>(
        BatchRequirement<TResult, TError> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return BatchRequirement.Zip(this, other);
    }

    /// <summary>Sequences a dependent requirement after a successful value.</summary>
    /// <typeparam name="TResult">The dependent requirement's value type.</typeparam>
    /// <param name="binder">The dependent requirement factory, invoked only after success.</param>
    /// <exception cref="ArgumentNullException"><paramref name="binder"/> is null.</exception>
    /// <remarks>Reads made by the returned requirement are not dispatched until the preceding
    /// value resolves. A failure skips the binder and the dependent reads.</remarks>
    public BatchRequirement<TResult, TError> Bind<TResult>(
        Func<TValue, BatchRequirement<TResult, TError>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        return new(new BatchBindNode<TValue, TResult, TError>(_node, binder));
    }

    internal ValueTask<Result<TValue, BatchReadError<TError>>> EvaluateAsync(
        BatchExecution execution,
        CancellationToken cancellationToken) =>
        _node.EvaluateAsync(execution, cancellationToken);
}
