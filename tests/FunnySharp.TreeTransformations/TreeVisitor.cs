namespace FunnySharp.TreeTransformations;

/// <summary>Receives a node, its depth, and a callback-scoped path view.</summary>
/// <typeparam name="TNode">The caller's reference-type node.</typeparam>
/// <param name="node">The current node.</param>
/// <param name="depth">The root-relative depth; the root is zero.</param>
/// <param name="path">The path view, valid only during this callback.</param>
/// <returns>Whether the traversal continues.</returns>
public delegate TreeVisitDecision TreeVisitor<TNode>(TNode node, int depth, TreePathView path);
