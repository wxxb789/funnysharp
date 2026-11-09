namespace FunnySharp.TreeTransformations;

/// <summary>Reports a zipper root reconstruction's root and rebuild work.</summary>
/// <typeparam name="TNode">The caller's reference-type node.</typeparam>
/// <param name="Root">The reconstructed root.</param>
/// <param name="RebuiltNodes">The number of parent nodes rebuilt by <c>ToRoot</c>.</param>
/// <param name="SharedNodes">The number of sibling references copied without rebuilding.</param>
/// <param name="Changed">Whether the reconstructed root differs from the original root by reference.</param>
public readonly record struct ZipperRootResult<TNode>(
    TNode Root,
    int RebuiltNodes,
    int SharedNodes,
    bool Changed)
    where TNode : class;
