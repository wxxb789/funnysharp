namespace FunnySharp.TestSupport.TreeComparison;

/// <summary>
/// The application's ordinary configuration node: a section has a null value and ordered children;
/// a leaf has a value and no children. <see cref="Indexed"/> selects whether a section's children
/// are addressed by position or by name, which keeps the path semantics explicit.
/// </summary>
internal sealed record ConfigNode(
    string Name,
    string? Value,
    IReadOnlyList<ConfigNode> Children,
    bool Indexed = false)
{
    internal bool IsSection => Value is null;

    internal static ConfigNode Section(string name, params ConfigNode[] children) =>
        new(name, null, children, Indexed: false);

    internal static ConfigNode IndexedSection(string name, params ConfigNode[] children) =>
        new(name, null, children, Indexed: true);

    internal static ConfigNode Leaf(string name, string value) =>
        new(name, value, [], Indexed: false);
}
