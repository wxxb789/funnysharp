namespace FunnySharp.OperationPlans;

/// <summary>
/// The caller-owned transaction seam. The caller opens, owns, and disposes the real transaction;
/// the plan commits or rolls it back.
/// </summary>
public interface IPlanTransaction
{
    /// <summary>Commits the caller-owned transaction.</summary>
    ValueTask CommitAsync();

    /// <summary>Rolls back the caller-owned transaction.</summary>
    ValueTask RollbackAsync();
}
