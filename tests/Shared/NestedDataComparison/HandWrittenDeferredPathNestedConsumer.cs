using System.Text.Json;
using FunnySharp;

namespace FunnySharp.TestSupport.NestedDataComparison;

/// <summary>
/// The hand-written import with diagnostic paths built only in failure branches. It produces the
/// same report as <see cref="HandWrittenNestedConsumer.Import"/> without allocating a path string
/// for every successful field, so allocation comparisons do not charge the eager baseline's
/// success-path diagnostics.
/// </summary>
internal static class HandWrittenDeferredPathNestedConsumer
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
        if (!TryGetFirstProperty(record, propertyName, out var value))
        {
            failures.Add(new DecodeFailure(
                DecodeErrorKind.Missing,
                $"[{recordIndex}].{propertyName}"));
            return null;
        }

        if (value.ValueKind == JsonValueKind.Null)
        {
            failures.Add(new DecodeFailure(
                DecodeErrorKind.Null,
                $"[{recordIndex}].{propertyName}"));
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            failures.Add(new DecodeFailure(
                DecodeErrorKind.WrongKind,
                $"[{recordIndex}].{propertyName}"));
            return null;
        }

        var text = value.GetString()!;
        if (text.Length == 0)
        {
            failures.Add(new DecodeFailure(
                DecodeErrorKind.Empty,
                $"[{recordIndex}].{propertyName}"));
            return null;
        }

        return text;
    }

    private static IReadOnlyList<WireLine>? DecodeLines(
        JsonElement record,
        int recordIndex,
        List<DecodeFailure> failures)
    {
        if (!TryGetFirstProperty(record, "lines", out var value))
        {
            failures.Add(new DecodeFailure(DecodeErrorKind.Missing, $"[{recordIndex}].lines"));
            return null;
        }

        if (value.ValueKind == JsonValueKind.Null)
        {
            failures.Add(new DecodeFailure(DecodeErrorKind.Null, $"[{recordIndex}].lines"));
            return null;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            failures.Add(new DecodeFailure(DecodeErrorKind.WrongKind, $"[{recordIndex}].lines"));
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
        if (line.ValueKind != JsonValueKind.Object)
        {
            failures.Add(new DecodeFailure(
                DecodeErrorKind.WrongKind,
                $"[{recordIndex}].lines[{lineIndex}]"));
            return null;
        }

        var before = failures.Count;
        var sku = DecodeString(line, recordIndex, lineIndex, failures);
        var quantity = DecodeQuantity(line, recordIndex, lineIndex, failures);

        return failures.Count == before ? new WireLine(sku!, quantity) : null;
    }

    private static string? DecodeString(
        JsonElement line,
        int recordIndex,
        int lineIndex,
        List<DecodeFailure> failures)
    {
        if (!TryGetFirstProperty(line, "sku", out var value))
        {
            failures.Add(new DecodeFailure(
                DecodeErrorKind.Missing,
                $"[{recordIndex}].lines[{lineIndex}].sku"));
            return null;
        }

        if (value.ValueKind == JsonValueKind.Null)
        {
            failures.Add(new DecodeFailure(
                DecodeErrorKind.Null,
                $"[{recordIndex}].lines[{lineIndex}].sku"));
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            failures.Add(new DecodeFailure(
                DecodeErrorKind.WrongKind,
                $"[{recordIndex}].lines[{lineIndex}].sku"));
            return null;
        }

        var text = value.GetString()!;
        if (text.Length == 0)
        {
            failures.Add(new DecodeFailure(
                DecodeErrorKind.Empty,
                $"[{recordIndex}].lines[{lineIndex}].sku"));
            return null;
        }

        return text;
    }

    private static int DecodeQuantity(
        JsonElement line,
        int recordIndex,
        int lineIndex,
        List<DecodeFailure> failures)
    {
        if (!TryGetFirstProperty(line, "quantity", out var value))
        {
            failures.Add(new DecodeFailure(
                DecodeErrorKind.Missing,
                $"[{recordIndex}].lines[{lineIndex}].quantity"));
            return 0;
        }

        if (value.ValueKind == JsonValueKind.Null)
        {
            failures.Add(new DecodeFailure(
                DecodeErrorKind.Null,
                $"[{recordIndex}].lines[{lineIndex}].quantity"));
            return 0;
        }

        if (value.ValueKind != JsonValueKind.Number)
        {
            failures.Add(new DecodeFailure(
                DecodeErrorKind.WrongKind,
                $"[{recordIndex}].lines[{lineIndex}].quantity"));
            return 0;
        }

        if (!value.TryGetInt32(out var quantity))
        {
            failures.Add(new DecodeFailure(
                DecodeErrorKind.Malformed,
                $"[{recordIndex}].lines[{lineIndex}].quantity"));
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
