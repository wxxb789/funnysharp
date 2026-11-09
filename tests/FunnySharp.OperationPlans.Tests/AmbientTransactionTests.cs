using System.Transactions;

using FunnySharp.TestSupport.OperationComparison;

using Microsoft.Data.Sqlite;

namespace FunnySharp.OperationPlans.Tests;

public sealed class AmbientTransactionTests
{
    [Fact]
    public async Task AmbientScopeWithoutCompleteDoesNotRollBackSqliteCommands()
    {
        await using var sandbox = await SqliteSandbox.CreateAsync(cancellationToken: TestContext.Current.CancellationToken);

        using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            Assert.NotNull(Transaction.Current);

            await SqliteTestSupport.ExecuteAsync(
                sandbox.Connection,
                "INSERT INTO catalog(sku, unit_price_cents, active) VALUES('SKU-SCOPE', 1000, 1);");

            // The scope is disposed without Complete; a real enlistment would roll the insert back.
        }

        Assert.Equal(
            1L,
            await ShopSql.ScalarLongAsync(
                sandbox.Connection,
                "SELECT COUNT(*) FROM catalog WHERE sku = 'SKU-SCOPE';",
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExplicitEnlistmentIsNotSupported()
    {
        await using var sandbox = await SqliteSandbox.CreateAsync(cancellationToken: TestContext.Current.CancellationToken);

        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        Assert.NotNull(Transaction.Current);
        Assert.Throws<NotSupportedException>(
            () => sandbox.Connection.EnlistTransaction(Transaction.Current));
    }
}
