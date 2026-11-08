using System.Text.Json;

namespace FunnySharp.TestSupport.NestedDataComparison;

internal static class OrdinaryNestedConsumer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    internal static IReadOnlyList<ImportRow> Import(string json)
    {
        var orders = JsonSerializer.Deserialize<OrdinaryOrder[]>(json, Options)!;

        return orders
            .SelectMany(
                static order => order.Lines!.Select(
                    line => new ImportRow(
                        order.OrderId,
                        order.Customer,
                        line.Sku,
                        line.Quantity)))
            .ToList();
    }

    internal static IReadOnlyList<ReadingRow> ImportAligned(string json)
    {
        var meters = JsonSerializer.Deserialize<OrdinaryAligned[]>(json, Options)!;

        return meters
            .SelectMany(
                static meter => meter.Timestamps!.Zip(
                    meter.Values!,
                    (timestamp, value) => new ReadingRow(meter.MeterId, timestamp, value)))
            .ToList();
    }

    internal static IReadOnlyList<CartesianRow> ImportCartesian(string json)
    {
        var variants = JsonSerializer.Deserialize<OrdinaryVariant[]>(json, Options)!;

        return variants
            .SelectMany(
                static variant => variant.Sizes!.SelectMany(
                    size => variant.Colors!.Select(
                        color => new CartesianRow(variant.ProductId, size, color))))
            .ToList();
    }
}
