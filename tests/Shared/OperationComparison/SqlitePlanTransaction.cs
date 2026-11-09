using Microsoft.Data.Sqlite;

using FunnySharp.OperationPlans;

namespace FunnySharp.TestSupport.OperationComparison;

internal sealed class SqlitePlanTransaction(SqliteTransaction transaction, FaultInjector faults) : IPlanTransaction
{
    public ValueTask CommitAsync()
    {
        faults.OnCommit();
        transaction.Commit();
        return ValueTask.CompletedTask;
    }

    public ValueTask RollbackAsync()
    {
        faults.OnRollback();
        transaction.Rollback();
        return ValueTask.CompletedTask;
    }
}
