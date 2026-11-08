using System.Text.Json;
using FunnySharp;

internal static class NestedDataExamples
{
    internal static void Verify()
    {
        VerifyDecodeAndExpand();
        VerifyAlignmentAndMismatch();
        VerifyCartesianDistinction();
    }

    internal static async Task VerifyAsync()
    {
        var expanded = await AsyncOrders().ExpandAsync(
            static order => order.Lines,
            static (order, line, origin) => new NestedRow(
                order.OrderId,
                order.Customer,
                line.Sku,
                line.Quantity,
                origin));

        ExampleAssertions.True(expanded.TryGetValue(out var rows), "The asynchronous expansion must succeed.");
        ExampleAssertions.Equal(2, rows!.Count);
        ExampleAssertions.Equal("O-1", rows[0].OrderId);
        ExampleAssertions.Equal("BOOK-1", rows[0].Sku);
        ExampleAssertions.Equal("record[0].element[0]", rows[0].Origin.ToString());
        ExampleAssertions.Equal("record[0].element[1]", rows[1].Origin.ToString());
    }

    private static void VerifyDecodeAndExpand()
    {
        const string json = """
            [
              {"orderId":"O-1","customer":"Ada","lines":[{"sku":"BOOK-1","quantity":2},{"sku":"PEN-2","quantity":1}]},
              {"orderId":"O-2","customer":"Grace","lines":[]},
              {"orderId":"O-3","customer":"Linus","lines":[{"sku":"BAD-1","quantity":0}]}
            ]
            """;

        var lineDecoder = Decoder.Object(
            Decoder.Field("sku", Decoder.NonEmptyString),
            Decoder.Field("quantity", Decoder.Int32),
            static (sku, quantity) => new NestedLine(sku, quantity));
        var orderDecoder = Decoder.Object(
            Decoder.Field("orderId", Decoder.NonEmptyString),
            Decoder.Field("customer", Decoder.NonEmptyString),
            Decoder.Field("lines", Decoder.List(lineDecoder)),
            static (orderId, customer, lines) => new NestedOrder(orderId, customer, lines));

        using var document = JsonDocument.Parse(json);
        var decoded = Decoder.List(orderDecoder).Decode(document.RootElement);
        ExampleAssertions.True(decoded.TryGetValue(out var orders), "The nested orders must decode.");

        var expanded = orders!.Expand(
            static order => order.Lines,
            static (order, line, origin) => new NestedRow(
                order.OrderId,
                order.Customer,
                line.Sku,
                line.Quantity,
                origin));
        ExampleAssertions.True(expanded.TryGetValue(out var rows), "The nested lines must expand.");
        ExampleAssertions.Equal(3, rows!.Count);

        var rejections = rows
            .Where(static row => row.Quantity <= 0)
            .Select(static row => row.Origin.ToString())
            .ToArray();
        ExampleAssertions.SequenceEqual(["record[2].element[0]"], rejections);
        ExampleAssertions.Equal("record[2].element[0]", rows[2].Origin.ToString());
    }

    private static void VerifyAlignmentAndMismatch()
    {
        var alignedRecords = new[]
        {
            new ParallelRecord("M-1", ["t0", "t1"], [1.5, 2.5]),
            new ParallelRecord("M-2", ["t2"], [3.5, 4.5]),
        };

        var aligned = alignedRecords.ExpandAligned(
            static record => record.Labels,
            static record => record.Values,
            static (record, label, value, origin) => new ReadingRow(
                record.Id,
                label,
                value,
                origin));

        ExampleAssertions.True(
            aligned.TryGetErrors(out var errors),
            "Unequal parallel child lengths must fail instead of truncating.");
        ExampleAssertions.Equal(1, errors!.Count);
        ExampleAssertions.Equal(NestedRowErrorKind.ChildCountMismatch, errors[0].Kind);
        ExampleAssertions.Equal(1, errors[0].RecordIndex);
        ExampleAssertions.Equal(1, errors[0].FirstElementCount!.Value);
        ExampleAssertions.Equal(2, errors[0].SecondElementCount!.Value);

        var truncated = alignedRecords
            .SelectMany(static record => record.Labels.Zip(record.Values))
            .Count();
        ExampleAssertions.Equal(3, truncated);
    }

    private static void VerifyCartesianDistinction()
    {
        var variants = new[]
        {
            new VariantRecord("P-1", ["S", "M"], ["red", "blue"]),
        };

        var aligned = variants.ExpandAligned(
            static record => record.Sizes,
            static record => record.Colors,
            static (record, size, color, _) => $"{record.Id}:{size}:{color}");
        var cartesian = variants.ExpandCartesian(
            static record => record.Sizes,
            static record => record.Colors,
            static (record, size, color, _) => $"{record.Id}:{size}:{color}");

        ExampleAssertions.True(aligned.TryGetValue(out var alignedRows), "The aligned pairs must succeed.");
        ExampleAssertions.True(cartesian.TryGetValue(out var cartesianRows), "The Cartesian product must succeed.");
        ExampleAssertions.SequenceEqual(
            ["P-1:S:red", "P-1:M:blue"],
            alignedRows!);
        ExampleAssertions.SequenceEqual(
            ["P-1:S:red", "P-1:S:blue", "P-1:M:red", "P-1:M:blue"],
            cartesianRows!);
    }

    private static async IAsyncEnumerable<NestedOrder> AsyncOrders()
    {
        yield return new NestedOrder(
            "O-1",
            "Ada",
            [
                new NestedLine("BOOK-1", 2),
                new NestedLine("PEN-2", 1),
            ]);

        await Task.Yield();
    }

    internal sealed record NestedLine(string Sku, int Quantity);

    internal sealed record NestedOrder(
        string OrderId,
        string Customer,
        IReadOnlyList<NestedLine> Lines);

    internal sealed record NestedRow(
        string OrderId,
        string Customer,
        string Sku,
        int Quantity,
        RowOrigin Origin);

    internal sealed record ParallelRecord(
        string Id,
        IReadOnlyList<string> Labels,
        IReadOnlyList<double> Values);

    internal sealed record ReadingRow(
        string Id,
        string Label,
        double Value,
        RowOrigin Origin);

    internal sealed record VariantRecord(
        string Id,
        IReadOnlyList<string> Sizes,
        IReadOnlyList<string> Colors);
}
