using System.Globalization;

using FunnySharp;

using Microsoft.Data.Sqlite;

namespace FunnySharp.TestSupport.OperationComparison;

internal static class ShopSql
{
    internal static async Task<Option<Customer>> LoadCustomerAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string customerId,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            "SELECT name, credit_limit_cents, active FROM customers WHERE id = $id;");
        command.Parameters.AddWithValue("$id", customerId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return Option<Customer>.None;
        }

        return Option.Some(
            new Customer(
                customerId,
                reader.GetString(0),
                reader.GetInt32(1),
                reader.GetBoolean(2)));
    }

    internal static async Task<Option<CatalogItem>> LoadCatalogItemAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sku,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            "SELECT unit_price_cents, active FROM catalog WHERE sku = $sku;");
        command.Parameters.AddWithValue("$sku", sku);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return Option<CatalogItem>.None;
        }

        return Option.Some(new CatalogItem(sku, reader.GetInt32(0), reader.GetBoolean(1)));
    }

    internal static async Task<long> InsertOrderAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string customerId,
        int totalCents,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            INSERT INTO orders(customer_id, total_cents, status)
            VALUES ($customer, $total, 'pending')
            RETURNING id;
            """);
        command.Parameters.AddWithValue("$customer", customerId);
        command.Parameters.AddWithValue("$total", totalCents);

        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        return Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    internal static async Task InsertOrderLineAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long orderId,
        PricedLine line,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            INSERT INTO order_lines(order_id, sku, quantity, unit_price_cents)
            VALUES ($order, $sku, $quantity, $price);
            """);
        command.Parameters.AddWithValue("$order", orderId);
        command.Parameters.AddWithValue("$sku", line.Sku);
        command.Parameters.AddWithValue("$quantity", line.Quantity);
        command.Parameters.AddWithValue("$price", line.UnitPriceCents);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<int> TryReserveInventoryAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        PricedLine line,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            UPDATE inventory
            SET available = available - $quantity,
                reserved = reserved + $quantity
            WHERE sku = $sku AND available >= $quantity;
            """);
        command.Parameters.AddWithValue("$quantity", line.Quantity);
        command.Parameters.AddWithValue("$sku", line.Sku);

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<int> GetAvailableAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sku,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            "SELECT available FROM inventory WHERE sku = $sku;");
        command.Parameters.AddWithValue("$sku", sku);

        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        return value is null or DBNull
            ? 0
            : Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    internal static async Task InsertPaymentAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long orderId,
        int amountCents,
        string reference,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            INSERT INTO payments(order_id, amount_cents, reference)
            VALUES ($order, $amount, $reference);
            """);
        command.Parameters.AddWithValue("$order", orderId);
        command.Parameters.AddWithValue("$amount", amountCents);
        command.Parameters.AddWithValue("$reference", reference);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static async Task InsertOutboxAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long orderId,
        string payload,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            INSERT INTO outbox(order_id, topic, payload)
            VALUES ($order, $topic, $payload);
            """);
        command.Parameters.AddWithValue("$order", orderId);
        command.Parameters.AddWithValue("$topic", ShopFixtures.OutboxTopic);
        command.Parameters.AddWithValue("$payload", payload);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static async Task InsertReviewAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long orderId,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            INSERT INTO reviews(order_id, reason)
            VALUES ($order, $reason);
            """);
        command.Parameters.AddWithValue("$order", orderId);
        command.Parameters.AddWithValue("$reason", ShopFixtures.ReviewReason);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static async Task InsertAuditLinkAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long orderId,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            "INSERT INTO audit_links(id, order_id) VALUES (1, $order);");
        command.Parameters.AddWithValue("$order", orderId);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<(int Available, int Reserved)> GetInventoryAsync(
        SqliteConnection connection,
        string sku,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT available, reserved FROM inventory WHERE sku = $sku;";
        command.Parameters.AddWithValue("$sku", sku);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return (0, 0);
        }

        return (reader.GetInt32(0), reader.GetInt32(1));
    }

    internal static async Task<long> ScalarLongAsync(
        SqliteConnection connection,
        string commandText,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;

        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        return value is null or DBNull
            ? 0
            : Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    internal static async Task<string> ScalarStringAsync(
        SqliteConnection connection,
        string commandText,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;

        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        return value is null or DBNull
            ? string.Empty
            : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static SqliteCommand CreateCommand(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string commandText)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        return command;
    }
}
