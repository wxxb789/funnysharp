using FunnySharp.TestSupport.OperationComparison;

using Microsoft.Data.Sqlite;

namespace FunnySharp.OperationPlans.Tests;

internal static class SqliteTestSupport
{
    internal static async Task ExecuteAsync(
        SqliteConnection connection,
        string commandText,
        SqliteTransaction? transaction = null)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    internal static Task<long> CountAsync(SqliteConnection connection, string table) =>
        ShopSql.ScalarLongAsync(connection, $"SELECT COUNT(*) FROM {table};", CancellationToken.None);

    internal static void DeleteDatabase(string path)
    {
        foreach (var suffix in new[] { string.Empty, "-journal", "-shm", "-wal" })
        {
            var file = path + suffix;

            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }
}
