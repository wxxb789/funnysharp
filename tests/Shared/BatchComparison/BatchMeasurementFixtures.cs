namespace FunnySharp.TestSupport.Batching;

internal static class BatchMeasurementFixtures
{
    internal static IReadOnlyList<Order> CreateOrders(int orderCount, int customerCount)
    {
        var orders = new Order[orderCount];
        for (var index = 0; index < orders.Length; index++)
        {
            orders[index] = new Order(
                $"O{index}",
                $"c{index % customerCount}",
                (index % 5) + 1,
                5m + (index % 7));
        }

        return orders;
    }

    internal static BatchDataset CreateDataset(int customerCount, int regionCount)
    {
        var customers = new Dictionary<string, Customer>(StringComparer.Ordinal);
        var preferences = new Dictionary<string, Preference>(StringComparer.Ordinal);
        var regions = new Dictionary<string, Region>(StringComparer.Ordinal);

        for (var index = 0; index < customerCount; index++)
        {
            customers[$"c{index}"] = new Customer(
                $"c{index}",
                $"Customer {index}",
                $"r{index % regionCount}");
            preferences[$"c{index}"] = new Preference(
                $"c{index}",
                index % 2 == 0 ? "Standard" : "Gift",
                index % 2 != 0);
        }

        for (var index = 0; index < regionCount; index++)
        {
            regions[$"r{index}"] = new Region(
                $"r{index}",
                $"Region {index}",
                0.05m + (index * 0.01m));
        }

        return new BatchDataset(
            customers,
            preferences,
            regions,
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal));
    }
}
