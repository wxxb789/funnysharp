namespace FunnySharp;

/// <summary>
/// The explicit budget and seed controlling deterministic contract generation.
/// </summary>
/// <remarks>
/// Generation is deterministic for a given seed, contract, and options. <see cref="MaxDepth"/>
/// bounds recursive descent; a deferred contract reached at the depth limit fails with a
/// <see cref="ContractLimitation"/> instead of looping. Collections use at most
/// <see cref="MaxCollectionLength"/> items, and <see cref="MaxNodes"/> bounds the total generated
/// structure. The declared minimum length of a collection always wins over the collection budget;
/// if the two are inconsistent, generation fails explicitly rather than producing an invalid
/// value. The budget is a termination guarantee, not a quality target.
/// </remarks>
public sealed class ContractGenerationOptions
{
    /// <summary>Creates generation options.</summary>
    /// <param name="seed">The seed for the deterministic random sequence.</param>
    /// <param name="maxDepth">The maximum recursive depth; must not be negative.</param>
    /// <param name="maxCollectionLength">The maximum generated collection or string width; must not be negative.</param>
    /// <param name="maxNodes">The maximum number of generated nodes; must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException">A budget is outside its allowed range.</exception>
    public ContractGenerationOptions(
        int seed,
        int maxDepth = 8,
        int maxCollectionLength = 5,
        int maxNodes = 512)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxDepth);
        ArgumentOutOfRangeException.ThrowIfNegative(maxCollectionLength);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxNodes);

        Seed = seed;
        MaxDepth = maxDepth;
        MaxCollectionLength = maxCollectionLength;
        MaxNodes = maxNodes;
    }

    /// <summary>Gets the deterministic random seed.</summary>
    public int Seed { get; }

    /// <summary>Gets the maximum recursive generation depth.</summary>
    public int MaxDepth { get; }

    /// <summary>Gets the maximum generated collection or string width.</summary>
    public int MaxCollectionLength { get; }

    /// <summary>Gets the maximum number of generated nodes.</summary>
    public int MaxNodes { get; }
}
