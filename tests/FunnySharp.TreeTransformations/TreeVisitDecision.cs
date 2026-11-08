namespace FunnySharp.TreeTransformations;

/// <summary>Controls whether a traversal continues after the current visitor call.</summary>
public enum TreeVisitDecision : byte
{
    /// <summary>Continue with the next node.</summary>
    Continue = 0,

    /// <summary>Stop after the current visitor call; no later node is examined.</summary>
    Stop = 1,
}
