using FunnySharp.TestSupport.OperationComparison;

using Microsoft.Data.Sqlite;

namespace FunnySharp.OperationPlans.Tests;

public sealed class SqliteTransactionBoundaryTests
{
    [Fact]
    public async Task RollbackHidesWritesAndCommitRevealsThem()
    {
        var path = Path.Combine(Path.GetTempPath(), $"funnysharp-operation-{Guid.NewGuid():N}.db");

        try
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = path,
                DefaultTimeout = 1,
            }.ToString();

            await using (var setup = new SqliteConnection(connectionString))
            {
                await setup.OpenAsync(TestContext.Current.CancellationToken);
                await ShopSchema.CreateAsync(setup, TestContext.Current.CancellationToken);
                await ShopFixtures.SeedAsync(setup, TestContext.Current.CancellationToken);
            }

            await using var writer = new SqliteConnection(connectionString);
            await writer.OpenAsync(TestContext.Current.CancellationToken);
            await SqliteTestSupport.ExecuteAsync(writer, "PRAGMA foreign_keys=ON;");
            await using var writerTransaction = writer.BeginTransaction(deferred: false);

            await SqliteTestSupport.ExecuteAsync(
                writer,
                "INSERT INTO orders(customer_id, total_cents, status) VALUES('C-100', 100, 'pending');",
                writerTransaction);

            await using var reader = new SqliteConnection(connectionString);
            await reader.OpenAsync(TestContext.Current.CancellationToken);

            Assert.Equal(0L, await SqliteTestSupport.CountAsync(reader, "orders"));

            var busy = await Assert.ThrowsAsync<SqliteException>(
                () => SqliteTestSupport.ExecuteAsync(reader, "INSERT INTO orders(customer_id, total_cents, status) VALUES('C-100', 100, 'pending');"));

            Assert.Equal(5, busy.SqliteErrorCode);

            writerTransaction.Commit();

            Assert.Equal(1L, await SqliteTestSupport.CountAsync(reader, "orders"));

            await using var rollbackWriter = new SqliteConnection(connectionString);
            await rollbackWriter.OpenAsync(TestContext.Current.CancellationToken);
            await using var rollbackTransaction = rollbackWriter.BeginTransaction(deferred: false);

            await SqliteTestSupport.ExecuteAsync(
                rollbackWriter,
                "INSERT INTO orders(customer_id, total_cents, status) VALUES('C-100', 200, 'pending');",
                rollbackTransaction);

            rollbackTransaction.Rollback();

            Assert.Equal(1L, await SqliteTestSupport.CountAsync(reader, "orders"));
        }
        finally
        {
            SqliteTestSupport.DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task DeferredForeignKeyCommitFailureRollsBack()
    {
        await using var sandbox = await SqliteSandbox.CreateAsync(includeAuditLinks: true, TestContext.Current.CancellationToken);

        await using var transaction = sandbox.Connection.BeginTransaction(deferred: false);

        await SqliteTestSupport.ExecuteAsync(
            sandbox.Connection,
            "INSERT INTO audit_links(id, order_id) VALUES(1, 999);",
            transaction);

        var exception = Assert.Throws<SqliteException>(() => transaction.Commit());

        Assert.Equal(19, exception.SqliteErrorCode);

        transaction.Rollback();

        Assert.Equal(0L, await SqliteTestSupport.CountAsync(sandbox.Connection, "audit_links"));
    }
}
