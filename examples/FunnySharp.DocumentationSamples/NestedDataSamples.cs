using System.Text.Json;
using FunnySharp;

namespace FunnySharp.DocumentationSamples;

internal static class NestedDataSamples
{
    private static void DecodeAndExpand(string json)
    {
        // <snippet DocumentationSamples.NestedData.DecodeAndExpand>
        var lineDecoder = Decoder.Object(
            Decoder.Field("sku", Decoder.NonEmptyString),
            Decoder.Field("quantity", Decoder.Int32),
            static (sku, quantity) => new NestedSampleLine(sku, quantity));
        var orderDecoder = Decoder.Object(
            Decoder.Field("orderId", Decoder.NonEmptyString),
            Decoder.Field("customer", Decoder.NonEmptyString),
            Decoder.Field("lines", Decoder.List(lineDecoder)),
            static (orderId, customer, lines) =>
                new NestedSampleOrder(orderId, customer, lines));

        using var document = JsonDocument.Parse(json);
        var decoded = Decoder.List(orderDecoder).Decode(document.RootElement);

        decoded.Match(
            orders =>
            {
                var expanded = orders.Expand(
                    static order => order.Lines,
                    static (order, line, origin) =>
                        new NestedSampleRow(
                            order.OrderId,
                            order.Customer,
                            line.Sku,
                            line.Quantity,
                            origin));

                expanded.Match(
                    static rows => Console.WriteLine($"{rows.Count} rows."),
                    static errors => Console.WriteLine($"{errors.Count} expansion failures."));
            },
            static errors => Console.WriteLine($"{errors.Count} decode failures."));
        // </snippet>
    }

    private static void ExpandAligned(IReadOnlyList<NestedSampleMeter> meters)
    {
        // <snippet DocumentationSamples.NestedData.ExpandAligned>
        var readings = meters.ExpandAligned(
            static meter => meter.Timestamps,
            static meter => meter.Values,
            static (meter, timestamp, value, origin) =>
                new NestedSampleReading(meter.MeterId, timestamp, value, origin));

        readings.Match(
            static rows => Console.WriteLine($"{rows.Count} readings."),
            static errors => Console.WriteLine(errors[0].ToString()));
        // </snippet>
    }

    private static void RejectWithOrigin(IReadOnlyList<NestedSampleOrder> orders)
    {
        // <snippet DocumentationSamples.NestedData.ProvenanceRejection>
        var expanded = orders.Expand(
            static order => order.Lines,
            static (order, line, origin) =>
                new NestedSampleRow(
                    order.OrderId,
                    order.Customer,
                    line.Sku,
                    line.Quantity,
                    origin));

        expanded.Match(
            static rows =>
            {
                foreach (var row in rows)
                {
                    if (row.Quantity <= 0)
                    {
                        Console.WriteLine($"rejected {row.Origin}");
                    }
                }
            },
            static errors => Console.WriteLine(errors[0].ToString()));
        // </snippet>
    }

    private static void DistinguishRelations(IReadOnlyList<NestedSampleVariant> variants)
    {
        // <snippet DocumentationSamples.NestedData.CartesianDistinction>
        var aligned = variants.ExpandAligned(
            static variant => variant.Sizes,
            static variant => variant.Colors,
            static (variant, size, color, _) => $"{variant.ProductId}:{size}:{color}");
        var cartesian = variants.ExpandCartesian(
            static variant => variant.Sizes,
            static variant => variant.Colors,
            static (variant, size, color, _) => $"{variant.ProductId}:{size}:{color}");

        aligned.Match(
            static rows => Console.WriteLine($"{rows.Count} aligned pairs."),
            static errors => Console.WriteLine(errors[0].ToString()));
        cartesian.Match(
            static rows => Console.WriteLine($"{rows.Count} product rows."),
            static errors => Console.WriteLine(errors[0].ToString()));
        // </snippet>
    }

    internal sealed record NestedSampleLine(string Sku, int Quantity);

    internal sealed record NestedSampleOrder(
        string OrderId,
        string Customer,
        IReadOnlyList<NestedSampleLine> Lines);

    internal sealed record NestedSampleRow(
        string OrderId,
        string Customer,
        string Sku,
        int Quantity,
        RowOrigin Origin);

    internal sealed record NestedSampleMeter(
        string MeterId,
        IReadOnlyList<string> Timestamps,
        IReadOnlyList<double> Values);

    internal sealed record NestedSampleReading(
        string MeterId,
        string Timestamp,
        double Value,
        RowOrigin Origin);

    internal sealed record NestedSampleVariant(
        string ProductId,
        IReadOnlyList<string> Sizes,
        IReadOnlyList<string> Colors);
}
