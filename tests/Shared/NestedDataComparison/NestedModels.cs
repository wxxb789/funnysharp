using FunnySharp;

namespace FunnySharp.TestSupport.NestedDataComparison;

internal sealed record WireLine(string Sku, int Quantity);

internal sealed record ImportOrder(
    string OrderId,
    string Customer,
    IReadOnlyList<WireLine> Lines);

internal sealed record ImportRow(
    string OrderId,
    string Customer,
    string Sku,
    int Quantity);

internal sealed record ImportRejection(
    int RecordIndex,
    int ElementIndex,
    string OrderId,
    string Reason);

internal sealed record DecodeFailure(DecodeErrorKind Kind, string Path);

internal sealed record ExpansionFailure(
    NestedRowErrorKind Kind,
    int RecordIndex,
    int? FirstCount,
    int? SecondCount);

internal sealed record ImportReport(
    IReadOnlyList<ImportRow> Rows,
    IReadOnlyList<ImportRejection> Rejections,
    IReadOnlyList<DecodeFailure> DecodeFailures,
    IReadOnlyList<ExpansionFailure> ExpansionFailures);

internal sealed record ReadingRow(string MeterId, string Timestamp, double Value);

internal sealed record MeterReadings(
    string MeterId,
    IReadOnlyList<string> Timestamps,
    IReadOnlyList<double> Values);

internal sealed record CartesianRow(string ProductId, string Size, string Color);

internal sealed record VariantRecord(
    string ProductId,
    IReadOnlyList<string> Sizes,
    IReadOnlyList<string> Colors);

internal sealed record AlignedReport(
    IReadOnlyList<ReadingRow> Rows,
    IReadOnlyList<ExpansionFailure> Failures);

internal sealed record CartesianReport(
    IReadOnlyList<CartesianRow> Rows,
    IReadOnlyList<ExpansionFailure> Failures);

internal enum OrdinaryImportBehavior
{
    RowsMatch,
    RowsDiffer,
    ThrowsJson,
    ThrowsArgumentNull,
}

internal sealed record ImportFixture(
    string Name,
    string Json,
    ImportReport Expected,
    OrdinaryImportBehavior? Ordinary);

internal sealed record AlignedFixture(
    string Name,
    string Json,
    AlignedReport Expected,
    IReadOnlyList<ReadingRow> OrdinaryRows);

internal sealed record CartesianFixture(
    string Name,
    string Json,
    CartesianReport Expected);

internal sealed record OrdinaryOrder(string OrderId, string Customer, OrdinaryLine[]? Lines);

internal sealed record OrdinaryLine(string Sku, int Quantity);

internal sealed record OrdinaryAligned(string MeterId, string[]? Timestamps, double[]? Values);

internal sealed record OrdinaryVariant(string ProductId, string[]? Sizes, string[]? Colors);
