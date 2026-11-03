namespace FunnySharp;

/// <summary>
/// Bounds and cache policy for one <see cref="BatchScope"/>.
/// </summary>
/// <remarks>
/// Every option is validated when the scope is created. The defaults bound one request-sized
/// execution: at most 128 keys per source call, one batch operation at a time, 1024 unresolved
/// requests, and 1024 cached key results per source.
/// </remarks>
public sealed class BatchScopeOptions
{
    /// <summary>Gets the maximum number of keys passed to one source batch call.</summary>
    /// <value>A positive integer. The default is 128.</value>
    public int MaxBatchSize { get; init; } = 128;

    /// <summary>Gets the maximum number of source batch calls that may run concurrently.</summary>
    /// <value>A positive integer. The default is 1.</value>
    public int MaxConcurrentBatches { get; init; } = 1;

    /// <summary>Gets the maximum number of distinct unresolved keys retained by one execution.</summary>
    /// <value>A positive integer. The default is 1024.</value>
    public int MaxPendingRequests { get; init; } = 1024;

    /// <summary>Gets whether resolved key results are retained for the lifetime of the scope.</summary>
    /// <value><see langword="true"/> by default. Disable to deduplicate only unresolved requests.</value>
    public bool CacheResults { get; init; } = true;

    /// <summary>Gets the maximum number of cached key results retained per source by the scope.</summary>
    /// <value>A positive integer when <see cref="CacheResults"/> is enabled. The default is 1024.
    /// Each distinct source instance owns its own cache table, so a scope touching several
    /// sources retains up to this bound for each of them. Entries are evicted in insertion order
    /// when the bound is reached.</value>
    public int MaxCachedEntries { get; init; } = 1024;

    internal void Validate()
    {
        if (MaxBatchSize < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaxBatchSize),
                MaxBatchSize,
                "The maximum batch size must be at least one.");
        }

        if (MaxConcurrentBatches < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaxConcurrentBatches),
                MaxConcurrentBatches,
                "The maximum concurrent batch count must be at least one.");
        }

        if (MaxPendingRequests < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaxPendingRequests),
                MaxPendingRequests,
                "The maximum pending request count must be at least one.");
        }

        if (MaxCachedEntries < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaxCachedEntries),
                MaxCachedEntries,
                "The maximum cached entry count cannot be negative.");
        }

        if (CacheResults && MaxCachedEntries < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaxCachedEntries),
                MaxCachedEntries,
                "The maximum cached entry count must be at least one when caching is enabled.");
        }
    }
}
