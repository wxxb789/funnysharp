using System.Text.Json;
using FunnySharp;

namespace FunnySharp.TestSupport.NestedDataComparison;

internal static class FunnySharpNestedConsumer
{
    private static readonly Decoder<IReadOnlyList<ImportOrder>> ImportDecoder = Decoder.List(
        Decoder.Object(
            Decoder.Field("orderId", Decoder.NonEmptyString),
            Decoder.Field("customer", Decoder.NonEmptyString),
            Decoder.Field(
                "lines",
                Decoder.List(
                    Decoder.Object(
                        Decoder.Field("sku", Decoder.NonEmptyString),
                        Decoder.Field("quantity", Decoder.Int32),
                        static (sku, quantity) => new WireLine(sku, quantity)))),
            static (orderId, customer, lines) => new ImportOrder(orderId, customer, lines)));

    private static readonly Decoder<IReadOnlyList<MeterReadings>> AlignedDecoder = Decoder.List(
        Decoder.Object(
            Decoder.Field("meterId", Decoder.NonEmptyString),
            Decoder.Field("timestamps", Decoder.List(Decoder.NonEmptyString)),
            Decoder.Field("values", Decoder.List(Decoder.Double)),
            static (meterId, timestamps, values) => new MeterReadings(meterId, timestamps, values)));

    private static readonly Decoder<IReadOnlyList<VariantRecord>> VariantDecoder = Decoder.List(
        Decoder.Object(
            Decoder.Field("productId", Decoder.NonEmptyString),
            Decoder.Field("sizes", Decoder.List(Decoder.NonEmptyString)),
            Decoder.Field("colors", Decoder.List(Decoder.NonEmptyString)),
            static (productId, sizes, colors) => new VariantRecord(productId, sizes, colors)));

    internal static ImportReport Import(string json)
    {
        using var document = JsonDocument.Parse(json);
        var decoded = ImportDecoder.Decode(document.RootElement);
        if (decoded.TryGetErrors(out var decodeErrors))
        {
            return new ImportReport(
                [],
                [],
                decodeErrors!
                    .Select(static error => new DecodeFailure(error.Kind, error.Path))
                    .ToArray(),
                []);
        }

        decoded.TryGetValue(out var orders);
        var expanded = orders!.Expand(
            static order => order.Lines,
            static (order, line, origin) => new LocatedLine(order, line, origin));

        if (expanded.TryGetErrors(out var expansionErrors))
        {
            return new ImportReport(
                [],
                [],
                [],
                expansionErrors!.Select(MapExpansion).ToArray());
        }

        expanded.TryGetValue(out var locatedLines);
        var rows = new List<ImportRow>();
        var rejections = new List<ImportRejection>();

        foreach (var located in locatedLines!)
        {
            if (located.Line.Quantity <= 0)
            {
                rejections.Add(new ImportRejection(
                    located.Origin.RecordIndex,
                    located.Origin.ElementIndex,
                    located.Order.OrderId,
                    "non-positive quantity"));
            }
            else
            {
                rows.Add(new ImportRow(
                    located.Order.OrderId,
                    located.Order.Customer,
                    located.Line.Sku,
                    located.Line.Quantity));
            }
        }

        return new ImportReport(rows, rejections, [], []);
    }

    internal static AlignedReport ImportAligned(string json)
    {
        using var document = JsonDocument.Parse(json);
        var decoded = AlignedDecoder.Decode(document.RootElement);
        var meters = decoded.Require();
        var expanded = meters.ExpandAligned(
            static meter => meter.Timestamps,
            static meter => meter.Values,
            static (meter, timestamp, value, _) => new ReadingRow(meter.MeterId, timestamp, value));

        return expanded.Match(
            static rows => new AlignedReport(rows, []),
            static errors => new AlignedReport([], errors.Select(MapExpansion).ToArray()));
    }

    internal static CartesianReport ImportCartesian(string json)
    {
        using var document = JsonDocument.Parse(json);
        var decoded = VariantDecoder.Decode(document.RootElement);
        var variants = decoded.Require();
        var expanded = variants.ExpandCartesian(
            static variant => variant.Sizes,
            static variant => variant.Colors,
            static (variant, size, color, _) => new CartesianRow(variant.ProductId, size, color));

        return expanded.Match(
            static rows => new CartesianReport(rows, []),
            static errors => new CartesianReport([], errors.Select(MapExpansion).ToArray()));
    }

    private static ExpansionFailure MapExpansion(NestedRowError error) =>
        new(error.Kind, error.RecordIndex, error.FirstElementCount, error.SecondElementCount);

    private sealed record LocatedLine(ImportOrder Order, WireLine Line, RowOrigin Origin);
}
