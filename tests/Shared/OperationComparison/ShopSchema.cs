using Microsoft.Data.Sqlite;

namespace FunnySharp.TestSupport.OperationComparison;

internal static class ShopSchema
{
    private const string Tables = """
        PRAGMA foreign_keys = ON;

        CREATE TABLE customers (
            id TEXT PRIMARY KEY,
            name TEXT NOT NULL,
            credit_limit_cents INTEGER NOT NULL,
            active INTEGER NOT NULL
        );

        CREATE TABLE catalog (
            sku TEXT PRIMARY KEY,
            unit_price_cents INTEGER NOT NULL,
            active INTEGER NOT NULL
        );

        CREATE TABLE inventory (
            sku TEXT PRIMARY KEY,
            available INTEGER NOT NULL,
            reserved INTEGER NOT NULL,
            CHECK (available >= 0)
        );

        CREATE TABLE orders (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            customer_id TEXT NOT NULL REFERENCES customers(id),
            total_cents INTEGER NOT NULL,
            status TEXT NOT NULL
        );

        CREATE TABLE order_lines (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            order_id INTEGER NOT NULL REFERENCES orders(id),
            sku TEXT NOT NULL,
            quantity INTEGER NOT NULL,
            unit_price_cents INTEGER NOT NULL
        );

        CREATE TABLE payments (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            order_id INTEGER NOT NULL REFERENCES orders(id),
            amount_cents INTEGER NOT NULL,
            reference TEXT NOT NULL UNIQUE
        );

        CREATE TABLE outbox (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            order_id INTEGER NOT NULL REFERENCES orders(id),
            topic TEXT NOT NULL,
            payload TEXT NOT NULL
        );

        CREATE TABLE reviews (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            order_id INTEGER NOT NULL REFERENCES orders(id),
            reason TEXT NOT NULL
        );
        """;

    internal static async Task CreateAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = Tables;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static async Task CreateAuditLinksAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE audit_links (
                id INTEGER PRIMARY KEY,
                order_id INTEGER NOT NULL REFERENCES orders(id) DEFERRABLE INITIALLY DEFERRED
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
