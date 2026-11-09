namespace FunnySharp.OperationPlans;

/// <summary>
/// Classifies an operation by the effect it can have outside the caller's typed state.
/// </summary>
public enum OperationKind
{
    /// <summary>The operation reads storage or derives a value without writing.</summary>
    Read,

    /// <summary>The operation writes through the caller-owned transaction.</summary>
    Write,

    /// <summary>The operation calls a system outside the transaction; it cannot be rolled back here.</summary>
    External,
}
