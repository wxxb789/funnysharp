namespace FunnySharp.TestSupport.Batching;

internal sealed record Order(
    string OrderId,
    string CustomerKey,
    int Units,
    decimal UnitPrice);

internal sealed record Customer(
    string Key,
    string Name,
    string RegionKey);

internal sealed record Preference(
    string Key,
    string DisplayName,
    bool GiftWrap);

internal sealed record Region(
    string Key,
    string Name,
    decimal TaxRate);

internal sealed record EnrichedOrder(
    Order Order,
    Customer Customer,
    Preference Preference,
    Region Region,
    decimal Total);

internal sealed record OrderFailure(string Code, string Detail)
{
    internal static OrderFailure MissingRecord { get; } =
        new("missing-record", "a required record is missing");

    internal static OrderFailure UnexpectedSourceResponse(string detail) =>
        new("unexpected-source-response", detail);

    internal static OrderFailure SourceFailure(string source, string key) =>
        new($"{source}-failure", $"the source failed the key '{key}'");

    internal static OrderFailure BatchFailure(string source) =>
        new($"{source}-batch-failure", "the source failed the whole batch");
}
