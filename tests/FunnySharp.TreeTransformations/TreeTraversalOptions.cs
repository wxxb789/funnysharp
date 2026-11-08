namespace FunnySharp.TreeTransformations;

/// <summary>Selects when a traversal calls its visitor relative to a node's children.</summary>
public enum TreeVisitOrder : byte
{
    /// <summary>Visit a node before its children, in child order.</summary>
    PreOrder = 0,

    /// <summary>Visit children in child order before their parent.</summary>
    PostOrder = 1,
}

/// <summary>Selects whether repeated references are distinct tree occurrences or one DAG node.</summary>
public enum TreeShape : byte
{
    /// <summary>Every reference is a distinct occurrence; a shared reference is visited again.</summary>
    Tree = 0,

    /// <summary>Each distinct node is visited once; later occurrences are skipped and counted.</summary>
    Dag = 1,
}

/// <summary>Selects what a traversal does when it encounters an ancestor repeat.</summary>
public enum TreeCyclePolicy : byte
{
    /// <summary>Throw <see cref="TreeCycleException"/> at the repeated node.</summary>
    Throw = 0,

    /// <summary>Stop the traversal and report <see cref="TreeTraversalResult.CycleDetected"/>.</summary>
    Stop = 1,
}

/// <summary>Controls traversal order, shape, cycle policy, and the node-count guard.</summary>
/// <typeparam name="TNode">The caller's reference-type node.</typeparam>
public sealed class TreeTraversalOptions<TNode>
    where TNode : class
{
    /// <summary>Gets the visit order; the default is pre-order.</summary>
    public TreeVisitOrder Order { get; init; } = TreeVisitOrder.PreOrder;

    /// <summary>Gets the tree or DAG shape; the default is a tree.</summary>
    public TreeShape Shape { get; init; } = TreeShape.Tree;

    /// <summary>Gets the cycle policy; the default throws.</summary>
    public TreeCyclePolicy CyclePolicy { get; init; } = TreeCyclePolicy.Throw;

    /// <summary>Gets the maximum number of entered nodes; the default is unlimited.</summary>
    public int MaxNodes { get; init; } = int.MaxValue;

    /// <summary>Gets the optional identity comparer; the adapter's identity is the default.</summary>
    public IEqualityComparer<TNode>? Identity { get; init; }

    internal IEqualityComparer<TNode> EffectiveIdentity(TreeNodeAdapter<TNode> adapter) =>
        Identity ?? adapter.Identity;

    internal void Validate()
    {
        if (MaxNodes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxNodes), "The node limit must be positive.");
        }

        if (!Enum.IsDefined(Order))
        {
            throw new ArgumentOutOfRangeException(nameof(Order));
        }

        if (!Enum.IsDefined(Shape))
        {
            throw new ArgumentOutOfRangeException(nameof(Shape));
        }

        if (!Enum.IsDefined(CyclePolicy))
        {
            throw new ArgumentOutOfRangeException(nameof(CyclePolicy));
        }
    }
}
