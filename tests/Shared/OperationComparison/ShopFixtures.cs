using Microsoft.Data.Sqlite;

namespace FunnySharp.TestSupport.OperationComparison;

internal static class ShopFixtures
{
    internal const int ReviewThresholdCents = 40_000;
    internal const string OutboxTopic = "orders.created";
    internal const string ReviewReason = "high-value";
    internal const string PaymentDeclinedReason = "gateway declined";

    internal static readonly PlaceOrderLine[] StandardLines =
    [
        new("SKU-A", 2),
        new("SKU-B", 1),
        new("SKU-C", 1),
    ];

    internal static PlaceOrderRequest StandardRequest() => new("C-100", StandardLines);

    internal static PlaceOrderRequest SmallRequest() =>
        new("C-100", [new PlaceOrderLine("SKU-A", 1)]);

    internal static PlaceOrderRequest StockShortfallRequest() =>
        new("C-100", [new PlaceOrderLine("SKU-A", 1), new PlaceOrderLine("SKU-B", 6)]);

    internal static PlaceOrderRequest CreditLimitRequest() => new("C-LOW", StandardLines);

    internal static PlaceOrderRequest MissingCustomerRequest() =>
        new("C-MISSING", [new PlaceOrderLine("SKU-A", 1)]);

    internal static PlaceOrderRequest InactiveCustomerRequest() =>
        new("C-INACTIVE", [new PlaceOrderLine("SKU-A", 1)]);

    internal static PlaceOrderRequest CatalogInactiveRequest() =>
        new("C-100", [new PlaceOrderLine("SKU-OFF", 1)]);

    internal static PlaceOrderRequest CatalogMissingRequest() =>
        new("C-100", [new PlaceOrderLine("SKU-NONE", 1)]);

    internal static async Task SeedAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO customers(id, name, credit_limit_cents, active) VALUES
                ('C-100', 'Ada', 100000, 1),
                ('C-LOW', 'Low Limit', 10000, 1),
                ('C-INACTIVE', 'Inactive', 100000, 0);

            INSERT INTO catalog(sku, unit_price_cents, active) VALUES
                ('SKU-A', 10000, 1),
                ('SKU-B', 15000, 1),
                ('SKU-C', 5000, 1),
                ('SKU-OFF', 1000, 0);

            INSERT INTO inventory(sku, available, reserved) VALUES
                ('SKU-A', 10, 0),
                ('SKU-B', 5, 0),
                ('SKU-C', 4, 0),
                ('SKU-OFF', 10, 0);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
