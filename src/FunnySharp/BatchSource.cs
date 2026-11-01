namespace FunnySharp;

/// <summary>
/// An explicit batch-capable data source. It owns the keys' equality policy, the tenant or
/// authorization partition it was created for, and the batch delegate itself.
/// </summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The successful value type.</typeparam>
/// <typeparam name="TError">The source's typed failure value.</typeparam>
/// <remarks>
/// Creating a source is an explicit claim that the supplied delegate supports batched reads. An
/// ordinary HTTP or database call is not batched merely because it is wrapped here. The source
/// instance is the cache identity inside a <see cref="BatchScope"/>; sources with the same
/// <see cref="Name"/> and <see cref="Partition"/> remain distinct cache entries unless the same
/// instance is used. Create a source within the tenant, authorization, and parameter context it
/// serves, and do not share that instance across those boundaries.
/// <para>
/// The delegate receives each key at most once per batch, in first-request order, with a batch
/// length no greater than <see cref="BatchScopeOptions.MaxBatchSize"/>. It may return entries in
/// any order keyed by <typeparamref name="TKey"/>; omitting a requested key reports
/// <see cref="BatchReadErrorKind.Missing"/>. Duplicate, unknown, or uninitialized entries make the
/// whole response <see cref="BatchReadErrorKind.UnexpectedResponse"/>. Returning
/// <see cref="Result{TValue, TError}.Failure"/> reports a whole-batch failure; throwing an
/// exception or returning a canceled task remains an exception or cancellation.
/// </para>
/// </remarks>
public sealed class BatchSource<TKey, TValue, TError>
    where TKey : notnull
{
    private readonly Func<
        IReadOnlyList<TKey>,
        CancellationToken,
        ValueTask<Result<IReadOnlyList<BatchSourceEntry<TKey, TValue, TError>>, TError>>> _fetchAsync;

    /// <summary>Creates a batch source.</summary>
    /// <param name="name">The diagnostic source name.</param>
    /// <param name="partition">The explicit tenant, authorization, or parameter partition.</param>
    /// <param name="keyComparer">The equality policy for keys and cache entries.</param>
    /// <param name="fetchAsync">The source-supplied batch operation.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> or
    /// <paramref name="partition"/> is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="keyComparer"/> or
    /// <paramref name="fetchAsync"/> is null.</exception>
    public BatchSource(
        string name,
        string partition,
        IEqualityComparer<TKey> keyComparer,
        Func<
            IReadOnlyList<TKey>,
            CancellationToken,
            ValueTask<Result<IReadOnlyList<BatchSourceEntry<TKey, TValue, TError>>, TError>>> fetchAsync)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(partition);
        ArgumentNullException.ThrowIfNull(keyComparer);
        ArgumentNullException.ThrowIfNull(fetchAsync);

        Name = name;
        Partition = partition;
        KeyComparer = keyComparer;
        _fetchAsync = fetchAsync;
    }

    /// <summary>Gets the diagnostic source name.</summary>
    public string Name { get; }

    /// <summary>Gets the explicit tenant, authorization, or parameter partition.</summary>
    public string Partition { get; }

    /// <summary>Gets the equality policy used for request grouping and scope caching.</summary>
    public IEqualityComparer<TKey> KeyComparer { get; }

    /// <summary>Adapts per-key and whole-batch failures to another typed error.</summary>
    /// <typeparam name="TResultError">The adapted source failure type.</typeparam>
    /// <param name="mapError">The failure mapping.</param>
    /// <returns>A distinct source with the same name, partition, keys, and successful values.
    /// The adapted source has its own cache identity.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapError"/> is null.</exception>
    /// <remarks>The mapper runs for each source error, never for a missing or unexpected response.
    /// Exceptions thrown by the mapper propagate unchanged.</remarks>
    public BatchSource<TKey, TValue, TResultError> MapError<TResultError>(
        Func<TError, TResultError> mapError)
    {
        ArgumentNullException.ThrowIfNull(mapError);
        return new BatchSource<TKey, TValue, TResultError>(
            Name,
            Partition,
            KeyComparer,
            async (keys, cancellationToken) =>
            {
                var response = await FetchAsync(keys, cancellationToken).ConfigureAwait(false);
                if (response.TryGetValue(out var entries))
                {
                    if (entries is null)
                    {
                        return Result<IReadOnlyList<BatchSourceEntry<TKey, TValue, TResultError>>, TResultError>.Success(
                            null!);
                    }

                    var mapped = new BatchSourceEntry<TKey, TValue, TResultError>[entries.Count];
                    for (var index = 0; index < entries.Count; index++)
                    {
                        var entry = entries[index];
                        if (entry.TryGetValue(out var value))
                        {
                            mapped[index] = BatchSourceEntry<TKey, TValue, TResultError>.Found(
                                entry.Key,
                                value);
                        }
                        else if (entry.TryGetError(out var error))
                        {
                            mapped[index] = BatchSourceEntry<TKey, TValue, TResultError>.Failed(
                                entry.Key,
                                mapError(error));
                        }
                        else
                        {
                            mapped[index] = default;
                        }
                    }

                    return Result<IReadOnlyList<BatchSourceEntry<TKey, TValue, TResultError>>, TResultError>.Success(
                        mapped);
                }

                response.TryGetError(out var batchError);
                return Result<IReadOnlyList<BatchSourceEntry<TKey, TValue, TResultError>>, TResultError>.Failure(
                    mapError(batchError!));
            });
    }

    internal ValueTask<Result<IReadOnlyList<BatchSourceEntry<TKey, TValue, TError>>, TError>>
        FetchAsync(IReadOnlyList<TKey> keys, CancellationToken cancellationToken) =>
        _fetchAsync(keys, cancellationToken);

    /// <inheritdoc />
    public override string ToString() => $"{Name}[{Partition}]";
}
