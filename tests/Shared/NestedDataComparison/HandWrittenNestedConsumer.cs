using System.Text.Json;
using FunnySharp;

namespace FunnySharp.TestSupport.NestedDataComparison;

internal static class HandWrittenNestedConsumer
{
    internal static ImportReport Import(string json)
    {
        using var document = JsonDocument.Parse(json);
        var rows = new List<ImportRow>();
        var rejections = new List<ImportRejection>();
        var failures = new List<DecodeFailure>();

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            failures.Add(new DecodeFailure(DecodeErrorKind.WrongKind, string.Empty));
            return new ImportReport([], [], failures, []);
        }

        var recordIndex = 0;
        foreach (var record in document.RootElement.EnumerateArray())
        {
            var decoded = DecodeRecord(record, recordIndex, failures);
            if (decoded is not null)
            {
                for (var elementIndex = 0; elementIndex < decoded.Lines.Count; elementIndex++)
                {
                    var line = decoded.Lines[elementIndex];
                    if (line.Quantity <= 0)
                    {
                        rejections.Add(new ImportRejection(
                            recordIndex,
                            elementIndex,
                            decoded.OrderId,
                            "non-positive quantity"));
                    }
                    else
                    {
                        rows.Add(new ImportRow(
                            decoded.OrderId,
                            decoded.Customer,
                            line.Sku,
                            line.Quantity));
                    }
                }
            }

            recordIndex++;
        }

        return failures.Count == 0
            ? new ImportReport(rows, rejections, failures, [])
            : new ImportReport([], [], failures, []);
    }

    internal static AlignedReport ImportAligned(string json)
    {
        using var document = JsonDocument.Parse(json);
        var rows = new List<ReadingRow>();
        var failures = new List<ExpansionFailure>();
        var recordIndex = 0;

        foreach (var record in document.RootElement.EnumerateArray())
        {
            var meterId = record.GetProperty("meterId").GetString()!;
            var timestamps = record.GetProperty("timestamps")
                .EnumerateArray()
                .Select(static element => element.GetString()!)
                .ToArray();
            var values = record.GetProperty("values")
                .EnumerateArray()
                .Select(static element => element.GetDouble())
                .ToArray();

            if (timestamps.Length != values.Length)
            {
                failures.Add(new ExpansionFailure(
                    NestedRowErrorKind.ChildCountMismatch,
                    recordIndex,
                    timestamps.Length,
                    values.Length));
            }
            else
            {
                for (var elementIndex = 0; elementIndex < timestamps.Length; elementIndex++)
                {
                    rows.Add(new ReadingRow(meterId, timestamps[elementIndex], values[elementIndex]));
                }
            }

            recordIndex++;
        }

        return failures.Count == 0
            ? new AlignedReport(rows, failures)
            : new AlignedReport([], failures);
    }

    internal static CartesianReport ImportCartesian(string json)
    {
        using var document = JsonDocument.Parse(json);
        var rows = new List<CartesianRow>();

        foreach (var record in document.RootElement.EnumerateArray())
        {
            var productId = record.GetProperty("productId").GetString()!;
            var sizes = record.GetProperty("sizes")
                .EnumerateArray()
                .Select(static element => element.GetString()!)
                .ToArray();
            var colors = record.GetProperty("colors")
                .EnumerateArray()
                .Select(static element => element.GetString()!)
                .ToArray();

            foreach (var size in sizes)
            {
                foreach (var color in colors)
                {
                    rows.Add(new CartesianRow(productId, size, color));
                }
            }
        }

        return new CartesianReport(rows, []);
    }

    private static ImportOrder? DecodeRecord(
        JsonElement record,
        int recordIndex,
        List<DecodeFailure> failures)
    {
        var before = failures.Count;
        if (record.ValueKind != JsonValueKind.Object)
        {
            failures.Add(new DecodeFailure(DecodeErrorKind.WrongKind, $"[{recordIndex}]"));
            return null;
        }

        var orderId = DecodeString(record, recordIndex, "orderId", failures);
        var customer = DecodeString(record, recordIndex, "customer", failures);
        var lines = DecodeLines(record, recordIndex, failures);

        return failures.Count == before
            ? new ImportOrder(orderId!, customer!, lines!)
            : null;
    }

    private static string? DecodeString(
        JsonElement record,
        int recordIndex,
        string propertyName,
        List<DecodeFailure> failures)
    {
        var path = $"[{recordIndex}].{propertyName}";
        if (!TryGetFirstProperty(record, propertyName, out var value))
        {
            failures.Add(new DecodeFailure(DecodeErrorKind.Missing, path));
            return null;
        }

        if (value.ValueKind == JsonValueKind.Null)
        {
            failures.Add(new DecodeFailure(DecodeErrorKind.Null, path));
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            failures.Add(new DecodeFailure(DecodeErrorKind.WrongKind, path));
            return null;
        }

        var text = value.GetString()!;
        if (text.Length == 0)
        {
            failures.Add(new DecodeFailure(DecodeErrorKind.Empty, path));
            return null;
        }

        return text;
    }

    private static IReadOnlyList<WireLine>? DecodeLines(
        JsonElement record,
        int recordIndex,
        List<DecodeFailure> failures)
    {
        var path = $"[{recordIndex}].lines";
        if (!TryGetFirstProperty(record, "lines", out var value))
        {
            failures.Add(new DecodeFailure(DecodeErrorKind.Missing, path));
            return null;
        }

        if (value.ValueKind == JsonValueKind.Null)
        {
            failures.Add(new DecodeFailure(DecodeErrorKind.Null, path));
            return null;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            failures.Add(new DecodeFailure(DecodeErrorKind.WrongKind, path));
            return null;
        }

        var before = failures.Count;
        var lines = new List<WireLine>();
        var lineIndex = 0;
        foreach (var line in value.EnumerateArray())
        {
            var decoded = DecodeLine(line, recordIndex, lineIndex, failures);
            if (decoded is not null)
            {
                lines.Add(decoded);
            }

            lineIndex++;
        }

        return failures.Count == before ? lines : null;
    }

    private static WireLine? DecodeLine(
        JsonElement line,
        int recordIndex,
        int lineIndex,
        List<DecodeFailure> failures)
    {
        var path = $"[{recordIndex}].lines[{lineIndex}]";
        if (line.ValueKind != JsonValueKind.Object)
        {
            failures.Add(new DecodeFailure(DecodeErrorKind.WrongKind, path));
            return null;
        }

        var before = failures.Count;
        var sku = DecodeString(line, recordIndex, lineIndex, failures);
        var quantity = DecodeQuantity(line, path, failures);

        return failures.Count == before ? new WireLine(sku!, quantity) : null;
    }

    private static string? DecodeString(
        JsonElement line,
        int recordIndex,
        int lineIndex,
        List<DecodeFailure> failures)
    {
        var path = $"[{recordIndex}].lines[{lineIndex}].sku";
        if (!TryGetFirstProperty(line, "sku", out var value))
        {
            failures.Add(new DecodeFailure(DecodeErrorKind.Missing, path));
            return null;
        }

        if (value.ValueKind == JsonValueKind.Null)
        {
            failures.Add(new DecodeFailure(DecodeErrorKind.Null, path));
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            failures.Add(new DecodeFailure(DecodeErrorKind.WrongKind, path));
            return null;
        }

        var text = value.GetString()!;
        if (text.Length == 0)
        {
            failures.Add(new DecodeFailure(DecodeErrorKind.Empty, path));
            return null;
        }

        return text;
    }

    private static int DecodeQuantity(
        JsonElement line,
        string linePath,
        List<DecodeFailure> failures)
    {
        var path = $"{linePath}.quantity";
        if (!TryGetFirstProperty(line, "quantity", out var value))
        {
            failures.Add(new DecodeFailure(DecodeErrorKind.Missing, path));
            return 0;
        }

        if (value.ValueKind == JsonValueKind.Null)
        {
            failures.Add(new DecodeFailure(DecodeErrorKind.Null, path));
            return 0;
        }

        if (value.ValueKind != JsonValueKind.Number)
        {
            failures.Add(new DecodeFailure(DecodeErrorKind.WrongKind, path));
            return 0;
        }

        if (!value.TryGetInt32(out var quantity))
        {
            failures.Add(new DecodeFailure(DecodeErrorKind.Malformed, path));
            return 0;
        }

        return quantity;
    }

    private static bool TryGetFirstProperty(
        JsonElement element,
        string propertyName,
        out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.NameEquals(propertyName))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
