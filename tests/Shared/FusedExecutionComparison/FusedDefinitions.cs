using System.Text.Json;
using FunnySharp;

namespace FunnySharp.TestSupport.FusedExecution;

public static class FusedSelectorLedger
{
    private static readonly List<string> events = [];

    public static IReadOnlyList<string> Events => events;

    public static void Clear() => events.Clear();

    public static int Quantity(OrderLine line)
    {
        events.Add("quantity");
        return line.Quantity;
    }

    public static decimal Price(OrderLine line)
    {
        events.Add("price");
        return line.UnitPrice;
    }

    public static int ThrowingQuantity(OrderLine line)
    {
        events.Add("quantity");
        if (line.Sku == "BOOM")
        {
            throw new InvalidOperationException("quantity failed");
        }

        return line.Quantity;
    }

    public static decimal ThrowingPrice(OrderLine line)
    {
        events.Add("price");
        if (line.Sku == "BOOM")
        {
            throw new InvalidOperationException("price failed");
        }

        return line.UnitPrice;
    }
}

public static partial class FusedSummaryDefinitions
{
    public static readonly Summary<
        OrderLine,
        (int First, decimal Second, Option<decimal> Third, Option<decimal> Fourth),
        (int First, decimal Second, Option<decimal> Third, Option<decimal> Fourth)> Statistics =
        Summary.Zip(
            Summary<OrderLine>.Count(),
            Summary<OrderLine>.Sum(static line => line.Quantity * line.UnitPrice),
            Summary<OrderLine>.Min(static line => line.UnitPrice),
            Summary<OrderLine>.Max(static line => line.UnitPrice));

    public static readonly Summary<
        OrderLine,
        (int First, int Second, decimal Third),
        (int First, int Second, decimal Third)> Ledger =
        Summary.Zip(
            Summary<OrderLine>.Count(),
            Summary<OrderLine>.Sum(static line => FusedSelectorLedger.Quantity(line)),
            Summary<OrderLine>.Sum(static line => FusedSelectorLedger.Price(line)));

    public static readonly Summary<
        OrderLine,
        (int First, Option<string> Second, Option<string> Third),
        (int First, Option<string> Second, Option<string> Third)> Notes =
        Summary.Zip(
            Summary<OrderLine>.Count(),
            Summary<OrderLine>.Min(static line => line.Note!),
            Summary<OrderLine>.Max(static line => line.Note!));

    public static readonly Summary<OrderLine, (int First, decimal Second), (int First, decimal Second)> Overflow =
        Summary.Zip(
            Summary<OrderLine>.Count(),
            Summary<OrderLine>.Sum(static line => line.UnitPrice));

    public static readonly Summary<
        OrderLine,
        (int First, int Second, decimal Third),
        (int First, int Second, decimal Third)> Throwing =
        Summary.Zip(
            Summary<OrderLine>.Count(),
            Summary<OrderLine>.Sum(static line => FusedSelectorLedger.ThrowingQuantity(line)),
            Summary<OrderLine>.Sum(static line => FusedSelectorLedger.ThrowingPrice(line)));

    public static readonly Summary<
        OrderLine,
        (Option<string> First, Option<string> Last),
        (Option<string> First, Option<string> Last)> TieOrder =
        Summary.Zip(
            Summary<OrderLine>.Min(static line => line.Note!),
            Summary<OrderLine>.Max(static line => line.Note!));

    public static readonly Summary<
        OrderLine,
        (Option<decimal> First, Option<decimal> Last),
        (Option<decimal> First, Option<decimal> Last)> ExtremaLedger =
        Summary.Zip(
            Summary<OrderLine>.Min(static line => FusedSelectorLedger.Price(line)),
            Summary<OrderLine>.Max(static line => FusedSelectorLedger.Price(line)));

    public static readonly Summary<OverflowLine, (int First, int Second), (int First, int Second)> IntSelectorOverflow =
        Summary.Zip(
            Summary<OverflowLine>.Count(),
            Summary<OverflowLine>.Sum(static line => line.Quantity * 1000));

    public static readonly Summary<OverflowLine, (int First, long Second), (int First, long Second)> LongSelectorOverflow =
        Summary.Zip(
            Summary<OverflowLine>.Count(),
            Summary<OverflowLine>.Sum(static line => line.Big * 3));

    [FusedSummary(nameof(Statistics))]
    public static partial (int First, decimal Second, Option<decimal> Third, Option<decimal> Fourth) EvaluateFused(
        IEnumerable<OrderLine> source);

    [FusedSummary(nameof(Ledger))]
    public static partial (int First, int Second, decimal Third) EvaluateLedgerFused(
        IEnumerable<OrderLine> source);

    [FusedSummary(nameof(Notes))]
    public static partial (int First, Option<string> Second, Option<string> Third) EvaluateNotesFused(
        IEnumerable<OrderLine> source);

    [FusedSummary(nameof(Overflow))]
    public static partial (int First, decimal Second) EvaluateOverflowFused(
        IEnumerable<OrderLine> source);

    [FusedSummary(nameof(Throwing))]
    public static partial (int First, int Second, decimal Third) EvaluateThrowingFused(
        IEnumerable<OrderLine> source);

    [FusedSummary(nameof(TieOrder))]
    public static partial (Option<string> First, Option<string> Last) EvaluateTieOrderFused(
        IEnumerable<OrderLine> source);

    [FusedSummary(nameof(ExtremaLedger))]
    public static partial (Option<decimal> First, Option<decimal> Last) EvaluateExtremaLedgerFused(
        IEnumerable<OrderLine> source);

    [FusedSummary(nameof(IntSelectorOverflow))]
    public static partial (int First, int Second) EvaluateIntSelectorOverflowFused(
        IEnumerable<OverflowLine> source);

    [FusedSummary(nameof(LongSelectorOverflow))]
    public static partial (int First, long Second) EvaluateLongSelectorOverflowFused(
        IEnumerable<OverflowLine> source);
}

public static class OneShotSummaryDefinition
{
    // An explicit static constructor makes initialization precise for the one-time allocation
    // measurement instead of leaving it to beforefieldinit scheduling.
    static OneShotSummaryDefinition()
    {
    }

    public static readonly Summary<
        OrderLine,
        (int First, decimal Second, Option<decimal> Third, Option<decimal> Fourth),
        (int First, decimal Second, Option<decimal> Third, Option<decimal> Fourth)> Statistics =
        Summary.Zip(
            Summary<OrderLine>.Count(),
            Summary<OrderLine>.Sum(static line => line.Quantity * line.UnitPrice),
            Summary<OrderLine>.Min(static line => line.UnitPrice),
            Summary<OrderLine>.Max(static line => line.UnitPrice));
}

public static partial class FusedDecoderDefinitions
{
    public static readonly Decoder<OrderInput> OrderDecoder =
        Decoder.Object(
            Decoder.Field("id", Decoder.Int32),
            Decoder.Field("customer", Decoder.NonEmptyString),
            Decoder.Field("total", Decoder.Decimal),
            Decoder.Field("tags", Decoder.NonEmptyList(Decoder.NonEmptyString)),
            static (id, customer, total, tags) => new OrderInput(id, customer, total, tags));

    public static readonly Decoder<OrderOptional> OptionalDecoder =
        Decoder.Object(
            Decoder.Field("id", Decoder.Int32),
            Decoder.OptionalField("customer", Decoder.NonEmptyString),
            Decoder.NullableField("note", Decoder.NonEmptyString),
            static (id, customer, note) => new OrderOptional(id, customer, note));

    [FusedDecoding(nameof(OrderDecoder))]
    public static partial Validation<OrderInput, DecodeError> DecodeFused(JsonElement input);

    public static readonly Decoder<ScalarGroup> ScalarDecoder =
        Decoder.Object(
            Decoder.Field("big", Decoder.Int64),
            Decoder.Field("ratio", Decoder.Double),
            Decoder.Field("flag", Decoder.Boolean),
            Decoder.Field("text", Decoder.String),
            static (big, ratio, flag, text) => new ScalarGroup(big, ratio, flag, text));

    public static readonly Decoder<TemporalGroup> TemporalDecoder =
        Decoder.Object(
            Decoder.Field("id", Decoder.Guid),
            Decoder.Field("when", Decoder.DateTime),
            Decoder.Field("at", Decoder.DateTimeOffset),
            Decoder.Field("payload", Decoder.Element),
            static (id, when, at, payload) => new TemporalGroup(id, when, at, payload));

    public static readonly Decoder<PlainListGroup> PlainListDecoder =
        Decoder.Object(
            Decoder.Field("label", Decoder.NonEmptyString),
            Decoder.Field("values", Decoder.List(Decoder.Int32)),
            static (label, values) => new PlainListGroup(label, values));

    public static readonly Decoder<ListOverloadProbe> ListOverloadDecoder =
        Decoder.Object(
            Decoder.Field("label", Decoder.NonEmptyString),
            Decoder.Field("values", Decoder.List(Decoder.String)),
            static (label, values) => new ListOverloadProbe(values));

    public static readonly Decoder<ConstructionProbe> ProbeDecoder =
        Decoder.Object(
            Decoder.Field("id", Decoder.Int32),
            Decoder.Field("name", Decoder.String),
            static (id, name) => new ConstructionProbe(id, name));

    public static readonly Decoder<ExplosiveProbe> ExplosiveDecoder =
        Decoder.Object(
            Decoder.Field("id", Decoder.Int32),
            Decoder.Field("name", Decoder.String),
            static (id, name) => new ExplosiveProbe(id, name));

    [FusedDecoding(nameof(OptionalDecoder))]
    public static partial Validation<OrderOptional, DecodeError> DecodeOptionalFused(JsonElement input);

    [FusedDecoding(nameof(ScalarDecoder))]
    public static partial Validation<ScalarGroup, DecodeError> DecodeScalarFused(JsonElement input);

    [FusedDecoding(nameof(TemporalDecoder))]
    public static partial Validation<TemporalGroup, DecodeError> DecodeTemporalFused(JsonElement input);

    [FusedDecoding(nameof(PlainListDecoder))]
    public static partial Validation<PlainListGroup, DecodeError> DecodePlainListFused(JsonElement input);

    [FusedDecoding(nameof(ListOverloadDecoder))]
    public static partial Validation<ListOverloadProbe, DecodeError> DecodeListOverloadFused(JsonElement input);

    [FusedDecoding(nameof(ProbeDecoder))]
    public static partial Validation<ConstructionProbe, DecodeError> DecodeProbeFused(JsonElement input);

    [FusedDecoding(nameof(ExplosiveDecoder))]
    public static partial Validation<ExplosiveProbe, DecodeError> DecodeExplosiveFused(JsonElement input);
}
