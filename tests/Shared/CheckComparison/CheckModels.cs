namespace FunnySharp.TestSupport.Checks;

internal sealed record CheckOrder(
    string OrderId,
    string CustomerId,
    string Sku,
    int Quantity,
    string Token);

internal sealed record CheckError(string Code, string Detail);

internal sealed class CheckLog
{
    private readonly List<string> entries = [];

    internal IReadOnlyList<string> Entries => entries;

    internal void Add(string name) => entries.Add(name);

    internal void Clear() => entries.Clear();
}
