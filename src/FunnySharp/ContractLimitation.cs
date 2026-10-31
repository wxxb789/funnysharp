namespace FunnySharp;

/// <summary>
/// Explains why one interpretation cannot be derived from a contract.
/// </summary>
/// <remarks>
/// A limitation is descriptive, deterministic, and carries no exception. It names the derivation,
/// the contract-relative path at which the derivation stops, and the reason. The root path is the
/// empty string, matching <see cref="DecodeError.Path"/>; generated paths use field names,
/// <c>[]</c> for collection elements, and alternative labels. Callers supply a missing derivation
/// explicitly with <c>WithDecoder</c>, <c>WithGenerator</c>, <c>WithShrinker</c>, or
/// <c>WithSchema</c>.
/// </remarks>
public sealed class ContractLimitation : IEquatable<ContractLimitation>
{
    internal ContractLimitation(ContractDerivation derivation, string path, string reason)
    {
        Derivation = derivation;
        Path = path;
        Reason = reason;
    }

    /// <summary>Gets the interpretation that is unavailable.</summary>
    public ContractDerivation Derivation { get; }

    /// <summary>Gets the contract-relative path at which the derivation stops; the root is empty.</summary>
    public string Path { get; }

    /// <summary>Gets the deterministic explanation of the missing derivation.</summary>
    public string Reason { get; }

    /// <summary>
    /// Returns a value indicating whether this limitation and <paramref name="other"/> have the
    /// same derivation, path, and reason.
    /// </summary>
    /// <param name="other">The limitation to compare, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when both limitations are structurally equal.</returns>
    public bool Equals(ContractLimitation? other) =>
        other is not null
        && Derivation == other.Derivation
        && string.Equals(Path, other.Path, StringComparison.Ordinal)
        && string.Equals(Reason, other.Reason, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ContractLimitation);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Derivation, Path, Reason);

    /// <summary>Returns deterministic diagnostic text for this limitation.</summary>
    /// <returns>The derivation, the path, and the reason.</returns>
    public override string ToString() =>
        $"{Derivation} unavailable at {(Path.Length == 0 ? "the contract root" : Path)}: {Reason}";

    internal ContractLimitation WithPathPrefix(string prefix) =>
        new(Derivation, JoinPath(prefix, Path), Reason);

    private static string JoinPath(string prefix, string path)
    {
        if (prefix.Length == 0)
        {
            return path;
        }

        if (path.Length == 0)
        {
            return prefix;
        }

        return path[0] == '[' ? prefix + path : prefix + "." + path;
    }
}
