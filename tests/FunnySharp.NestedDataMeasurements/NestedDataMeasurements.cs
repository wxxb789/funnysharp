using System.Collections;
using System.Text;
using FunnySharp;
using FunnySharp.TestSupport;
using FunnySharp.TestSupport.NestedDataComparison;
using static FunnySharp.TestSupport.MeasurementHarness;

#pragma warning disable FS0017

namespace FunnySharp.TestSupport.NestedDataComparison;

internal static class NestedDataMeasurements
{
    private const int ItemCount = 1024;
    private const int LinesPerRecord = 4;
    private const int Iterations = 50;
    private const int WarmupIterations = 5;

    internal static void Run()
    {
        NestedComparisonVerifier.Verify();

        var validJson = BuildImportJson(malformed: false);
        var malformedJson = BuildImportJson(malformed: true);
        var orders = BuildOrders();
        var nullChildOrders = BuildNullChildOrders();
        var allNullChildOrders = BuildAllNullChildOrders();
        var singleNullChildOrder = BuildSingleNullChildOrder();
        var uncountedOrders = new UncountedSequence<ImportOrder>(orders);
        var alignedJson = BuildAlignedJson();
        var alignedMismatchJson = BuildAlignedMismatchJson();
        var variants = BuildVariants();
        var emptyOrders = Array.Empty<ImportOrder>();
        var asyncSource = new AsyncArraySequence<ImportOrder>(orders);

        var rows = new (string Name, Measurement Value)[]
        {
            (
                "Import success - ordinary JsonSerializer + SelectMany",
                MeasureRow(() => OrdinaryNestedConsumer.Import(validJson).Count)),
            (
                "Import success - hand-written JsonDocument + eager paths",
                MeasureRow(() => HandWrittenNestedConsumer.Import(validJson).Rows.Count)),
            (
                "Import success - hand-written JsonDocument + deferred paths",
                MeasureRow(() => HandWrittenDeferredPathNestedConsumer.Import(validJson).Rows.Count)),
            (
                "Import success - FunnySharp Decoder + Expand",
                MeasureRow(() => FunnySharpNestedConsumer.Import(validJson).Rows.Count)),
            (
                "Import failure - hand-written eager paths (wrong quantity kind)",
                MeasureRow(() => HandWrittenNestedConsumer.Import(malformedJson).DecodeFailures.Count)),
            (
                "Import failure - hand-written deferred paths (wrong quantity kind)",
                MeasureRow(() => HandWrittenDeferredPathNestedConsumer.Import(malformedJson).DecodeFailures.Count)),
            (
                "Import failure - FunnySharp decoder (wrong quantity kind)",
                MeasureRow(() => FunnySharpNestedConsumer.Import(malformedJson).DecodeFailures.Count)),
            (
                "Aligned success - ordinary Zip",
                MeasureRow(() => OrdinaryNestedConsumer.ImportAligned(alignedJson).Count)),
            (
                "Aligned success - hand-written",
                MeasureRow(() => HandWrittenNestedConsumer.ImportAligned(alignedJson).Rows.Count)),
            (
                "Aligned success - ExpandAligned",
                MeasureRow(() => FunnySharpNestedConsumer.ImportAligned(alignedJson).Rows.Count)),
            (
                "Aligned mismatch - hand-written",
                MeasureRow(() => HandWrittenNestedConsumer.ImportAligned(alignedMismatchJson).Failures.Count)),
            (
                "Aligned mismatch - ExpandAligned",
                MeasureRow(() => FunnySharpNestedConsumer.ImportAligned(alignedMismatchJson).Failures.Count)),
            (
                "Cartesian typed - hand-written index loops",
                MeasureRow(() => CountHandWrittenCartesian(variants))),
            (
                "Cartesian typed - ExpandCartesian",
                MeasureRow(() => CountFunnySharpCartesian(variants))),
            (
                "Expand success - origin ignored",
                MeasureRow(() => CountPlainRows(orders))),
            (
                "Expand success - origin ignored, uncounted source (sync control)",
                MeasureRow(() => CountPlainRows(uncountedOrders))),
            (
                "Expand success - origin stored",
                MeasureRow(() => CountTracedRows(orders))),
            (
                "Expand success - per-row Location object (disclosure)",
                MeasureRow(() => CountLocationRows(orders))),
            (
                "Expand success - per-row location string (disclosure)",
                MeasureRow(() => CountStringRows(orders))),
            (
                "Expand failure - one null child list, 1,024 records",
                MeasureRow(() => CountExpansionErrors(nullChildOrders))),
            (
                "Expand failure - one null child list, 1 record",
                MeasureRow(() => CountExpansionErrors(singleNullChildOrder))),
            (
                "Expand failure - many null child lists",
                MeasureRow(() => CountExpansionErrors(allNullChildOrders))),
            (
                "Expand failure - render one error (disclosure)",
                MeasureRow(() => RenderFirstError(nullChildOrders))),
            (
                "Expand failure - render many errors (disclosure)",
                MeasureRow(() => RenderAllErrors(allNullChildOrders))),
            (
                "Empty - Expand",
                MeasureRow(() => CountPlainRows(emptyOrders))),
            (
                "Async - ExpandAsync success",
                MeasureAsyncRow(() => CountAsyncRows(asyncSource))),
        };

        Console.WriteLine("Nested data reshaping measurements");
        Console.WriteLine(
            $"Records: {ItemCount}; lines per record: {LinesPerRecord}; iterations: {Iterations}; "
            + $"warmup: {WarmupIterations}; min of 3 rounds.");
        Console.WriteLine(
            "Allocation uses GC.GetAllocatedBytesForCurrentThread for every row; every measured "
            + "await completes synchronously on the measuring thread. Timing is directional.");
        Console.WriteLine();
        Console.WriteLine("| Operation | Allocated B/op | Time ns/op |");
        Console.WriteLine("| --- | ---: | ---: |");

        foreach (var row in rows)
        {
            Console.WriteLine(
                $"| {row.Name} | {Format(row.Value.BytesPerOperation)} | {Format(row.Value.NanosecondsPerOperation)} |");
        }
    }

    private static Measurement MeasureRow(Func<int> operation) =>
        Measure(operation, Iterations, WarmupIterations);

    private static Measurement MeasureAsyncRow(Func<Task<int>> operation) =>
        MeasureAsyncThread(operation, Iterations, WarmupIterations).GetAwaiter().GetResult();

    private static int CountPlainRows(IEnumerable<ImportOrder> orders) =>
        orders.Expand(
                static order => order.Lines,
                static (order, line, _) => new PlainRow(order.OrderId, line.Sku))
            .Match(static rows => rows.Count, static _ => 0);

    private static int CountTracedRows(IReadOnlyList<ImportOrder> orders) =>
        orders.Expand(
                static order => order.Lines,
                static (order, line, origin) => new TracedRow(order.OrderId, line.Sku, origin))
            .Match(static rows => rows.Count, static _ => 0);

    private static int CountLocationRows(IReadOnlyList<ImportOrder> orders) =>
        orders.Expand(
                static order => order.Lines,
                static (order, line, origin) => new LocationTracedRow(
                    order.OrderId,
                    line.Sku,
                    Location.Root.At(origin.RecordIndex).At(origin.ElementIndex)))
            .Match(static rows => rows.Count, static _ => 0);

    private static int CountStringRows(IReadOnlyList<ImportOrder> orders) =>
        orders.Expand(
                static order => order.Lines,
                static (order, line, origin) => new StringTracedRow(
                    order.OrderId,
                    line.Sku,
                    $"record[{origin.RecordIndex}].element[{origin.ElementIndex}]"))
            .Match(static rows => rows.Count, static _ => 0);

    private static int CountExpansionErrors(IReadOnlyList<ImportOrder> orders) =>
        orders.Expand(
                static order => order.Lines,
                static (order, line, _) => new PlainRow(order.OrderId, line.Sku))
            .Match(static _ => 0, static errors => errors.Count);

    private static int RenderFirstError(IReadOnlyList<ImportOrder> orders) =>
        orders.Expand(
                static order => order.Lines,
                static (order, line, _) => new PlainRow(order.OrderId, line.Sku))
            .Match(static _ => 0, static errors => errors[0].ToString().Length);

    private static int RenderAllErrors(IReadOnlyList<ImportOrder> orders) =>
        orders.Expand(
                static order => order.Lines,
                static (order, line, _) => new PlainRow(order.OrderId, line.Sku))
            .Match(
                static _ => 0,
                static errors =>
                {
                    var length = 0;
                    foreach (var error in errors)
                    {
                        length += error.ToString().Length;
                    }

                    return length;
                });

    private static async Task<int> CountAsyncRows(IAsyncEnumerable<ImportOrder> orders)
    {
        var result = await orders.ExpandAsync(
            static order => order.Lines,
            static (order, line, _) => new PlainRow(order.OrderId, line.Sku));

        return result.Match(static rows => rows.Count, static _ => 0);
    }

    private static ImportOrder[] BuildOrders() =>
        Enumerable.Range(0, ItemCount)
            .Select(static index => new ImportOrder(
                $"O-{index}",
                $"Customer {index}",
                Enumerable.Range(0, LinesPerRecord)
                    .Select(line => new WireLine($"SKU-{index}-{line}", line + 1))
                    .ToArray()))
            .ToArray();

    private static ImportOrder[] BuildNullChildOrders()
    {
        var orders = BuildOrders();
        orders[0] = new ImportOrder("O-null", "Null customer", null!);
        return orders;
    }

    private static VariantRecord[] BuildVariants() =>
        Enumerable.Range(0, ItemCount)
            .Select(static index => new VariantRecord(
                $"P-{index}",
                ["S0", "S1", "S2", "S3"],
                ["C0", "C1", "C2"]))
            .ToArray();

    private static int CountHandWrittenCartesian(IReadOnlyList<VariantRecord> variants)
    {
        var rows = new List<CartesianRow>();
        for (var variantIndex = 0; variantIndex < variants.Count; variantIndex++)
        {
            var variant = variants[variantIndex];
            for (var sizeIndex = 0; sizeIndex < variant.Sizes.Count; sizeIndex++)
            {
                var size = variant.Sizes[sizeIndex];
                for (var colorIndex = 0; colorIndex < variant.Colors.Count; colorIndex++)
                {
                    rows.Add(new CartesianRow(variant.ProductId, size, variant.Colors[colorIndex]));
                }
            }
        }

        return rows.Count;
    }

    private static int CountFunnySharpCartesian(IReadOnlyList<VariantRecord> variants) =>
        variants.ExpandCartesian(
                static variant => variant.Sizes,
                static variant => variant.Colors,
                static (variant, size, color, _) => new CartesianRow(variant.ProductId, size, color))
            .Match(static rows => rows.Count, static _ => 0);

    private static ImportOrder[] BuildSingleNullChildOrder() =>
    [
        new ImportOrder("O-null", "Null customer", null!),
    ];

    private static ImportOrder[] BuildAllNullChildOrders() =>
        Enumerable.Range(0, ItemCount)
            .Select(static index => new ImportOrder($"O-{index}", $"Customer {index}", null!))
            .ToArray();

    private static string BuildImportJson(bool malformed)
    {
        var builder = new StringBuilder();
        builder.Append('[');

        for (var recordIndex = 0; recordIndex < ItemCount; recordIndex++)
        {
            if (recordIndex > 0)
            {
                builder.Append(',');
            }

            builder.Append("{\"orderId\":\"O-")
                .Append(recordIndex)
                .Append("\",\"customer\":\"Customer ")
                .Append(recordIndex)
                .Append("\",\"lines\":[");

            for (var lineIndex = 0; lineIndex < LinesPerRecord; lineIndex++)
            {
                if (lineIndex > 0)
                {
                    builder.Append(',');
                }

                builder.Append("{\"sku\":\"SKU-")
                    .Append(recordIndex)
                    .Append('-')
                    .Append(lineIndex)
                    .Append("\",\"quantity\":");

                if (malformed && recordIndex == ItemCount - 1 && lineIndex == 0)
                {
                    builder.Append("\"bad\"");
                }
                else
                {
                    builder.Append(lineIndex + 1);
                }

                builder.Append('}');
            }

            builder.Append("]}");
        }

        builder.Append(']');
        return builder.ToString();
    }

    private static string BuildAlignedJson()
    {
        var builder = new StringBuilder();
        builder.Append('[');

        for (var index = 0; index < ItemCount; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append("{\"meterId\":\"M-")
                .Append(index)
                .Append("\",\"timestamps\":[\"t0\",\"t1\",\"t2\",\"t3\"],\"values\":[1.5,2.5,3.5,4.5]}");
        }

        builder.Append(']');
        return builder.ToString();
    }

    private static string BuildAlignedMismatchJson()
    {
        var builder = new StringBuilder(BuildAlignedJson());
        builder.Length -= 1;
        builder.Append(",{\"meterId\":\"M-mismatch\",\"timestamps\":[\"t0\"],\"values\":[1.5,2.5]}]");
        return builder.ToString();
    }

    private sealed class UncountedSequence<T>(IReadOnlyList<T> items) : IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator() => items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed record PlainRow(string OrderId, string Sku);

    private sealed record TracedRow(string OrderId, string Sku, RowOrigin Origin);

    private sealed record LocationTracedRow(string OrderId, string Sku, Location Origin);

    private sealed record StringTracedRow(string OrderId, string Sku, string Origin);
}
