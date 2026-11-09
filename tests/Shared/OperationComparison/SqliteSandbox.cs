using Microsoft.Data.Sqlite;

namespace FunnySharp.TestSupport.OperationComparison;

internal sealed class SqliteSandbox : IAsyncDisposable
{
    private SqliteSandbox(SqliteConnection connection)
    {
        Connection = connection;
    }

    internal SqliteConnection Connection { get; }

    internal static async Task<SqliteSandbox> CreateAsync(
        bool includeAuditLinks = false,
        CancellationToken cancellationToken = default)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = $"file:operation-{Guid.NewGuid():N}?mode=memory&cache=shared",
            DefaultTimeout = 5,
        };

        var connection = new SqliteConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await ShopSchema.CreateAsync(connection, cancellationToken).ConfigureAwait(false);

        if (includeAuditLinks)
        {
            await ShopSchema.CreateAuditLinksAsync(connection, cancellationToken).ConfigureAwait(false);
        }

        await ShopFixtures.SeedAsync(connection, cancellationToken).ConfigureAwait(false);

        return new SqliteSandbox(connection);
    }

    public ValueTask DisposeAsync() => Connection.DisposeAsync();
}
