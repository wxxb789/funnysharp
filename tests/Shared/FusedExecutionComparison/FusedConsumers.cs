using System.Text.Json;
using FunnySharp;

namespace FunnySharp.TestSupport.FusedExecution;

public static class FusedConsumers
{
    public static int SummaryFused(IEnumerable<OrderLine> lines) =>
        FusedFixtures.Digest(FusedSummaryDefinitions.EvaluateFused(lines));

    public static int SummaryDelegated(IEnumerable<OrderLine> lines) =>
        FusedFixtures.Digest(FusedSummaryDefinitions.Statistics.Evaluate(lines));

    public static int SummaryHandWritten(IEnumerable<OrderLine> lines)
    {
        var count = 0;
        var total = 0m;
        var min = Option<decimal>.None;
        var max = Option<decimal>.None;

        foreach (var line in lines)
        {
            count = checked(count + 1);
            total = checked(total + (line.Quantity * line.UnitPrice));

            var price = line.UnitPrice;
            if (!min.TryGetValue(out var currentMin) || price < currentMin)
            {
                min = Option<decimal>.Some(price);
            }

            if (!max.TryGetValue(out var currentMax) || price > currentMax)
            {
                max = Option<decimal>.Some(price);
            }
        }

        return FusedFixtures.Digest((count, total, min, max));
    }

    public static int SummaryLinqAggregate(IEnumerable<OrderLine> lines)
    {
        var result = lines.Aggregate(
            (Count: 0, Total: 0m, Min: (decimal?)null, Max: (decimal?)null),
            static (state, line) => (
                checked(state.Count + 1),
                checked(state.Total + (line.Quantity * line.UnitPrice)),
                state.Min is null || line.UnitPrice < state.Min ? line.UnitPrice : state.Min,
                state.Max is null || line.UnitPrice > state.Max ? line.UnitPrice : state.Max));

        return FusedFixtures.Digest((
            result.Count,
            result.Total,
            result.Min is { } min ? Option<decimal>.Some(min) : Option<decimal>.None,
            result.Max is { } max ? Option<decimal>.Some(max) : Option<decimal>.None));
    }

    public static int SummaryLinqSeparate(IEnumerable<OrderLine> lines)
    {
        var count = lines.Count();
        var total = lines.Sum(static line => line.Quantity * line.UnitPrice);
        var min = Option<decimal>.Some(lines.Min(static line => line.UnitPrice));
        var max = Option<decimal>.Some(lines.Max(static line => line.UnitPrice));
        return FusedFixtures.Digest((count, total, min, max));
    }

    public static int DecoderFused(JsonElement input) =>
        Digest(FusedDecoderDefinitions.DecodeFused(input));

    public static int DecoderDelegated(JsonElement input) =>
        Digest(FusedDecoderDefinitions.OrderDecoder.Decode(input));

    public static int DecoderHandWritten(JsonElement input)
    {
        if (input.ValueKind != JsonValueKind.Object
            || !TryGetFirstProperty(input, "id", out var idElement)
            || idElement.ValueKind != JsonValueKind.Number
            || !idElement.TryGetInt32(out var id)
            || !TryGetFirstProperty(input, "customer", out var customerElement)
            || customerElement.ValueKind != JsonValueKind.String)
        {
            return 0;
        }

        var customer = customerElement.GetString()!;
        if (customer.Length == 0
            || !TryGetFirstProperty(input, "total", out var totalElement)
            || totalElement.ValueKind != JsonValueKind.Number
            || !totalElement.TryGetDecimal(out var total)
            || !TryGetFirstProperty(input, "tags", out var tagsElement)
            || tagsElement.ValueKind != JsonValueKind.Array)
        {
            return 0;
        }

        var tags = new List<string>(tagsElement.GetArrayLength());
        foreach (var tagElement in tagsElement.EnumerateArray())
        {
            if (tagElement.ValueKind != JsonValueKind.String)
            {
                return 0;
            }

            var tag = tagElement.GetString()!;
            if (tag.Length == 0)
            {
                return 0;
            }

            tags.Add(tag);
        }

        if (tags.Count == 0)
        {
            return 0;
        }

        return Digest(new OrderInput(id, customer, total, tags));
    }

    private static int Digest(Validation<OrderInput, DecodeError> validation)
    {
        if (validation.TryGetValue(out var value))
        {
            return Digest(value);
        }

        validation.TryGetErrors(out var errors);
        return HashCode.Combine(false, errors!.Count);
    }

    private static int Digest(OrderInput value) =>
        HashCode.Combine(value.Id, value.Customer, value.Total, value.Tags.Count);

    private static bool TryGetFirstProperty(JsonElement input, string name, out JsonElement value)
    {
        foreach (var property in input.EnumerateObject())
        {
            if (property.NameEquals(name))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
