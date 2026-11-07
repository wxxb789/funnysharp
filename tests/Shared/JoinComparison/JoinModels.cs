namespace FunnySharp.TestSupport.JoinComparison;

internal sealed record JoinOrder(string Id, string? CustomerId);

internal sealed record JoinCustomer(string Id, string Name);

internal sealed record JoinedOrder(string Id, string CustomerName);

internal sealed record JoinFailure(
    LookupErrorKind Kind,
    string? Key,
    int? SourceIndex,
    int? LookupSourceIndex,
    int? FirstLookupSourceIndex);

internal sealed record JoinReport(
    IReadOnlyList<JoinedOrder> Outputs,
    IReadOnlyList<JoinFailure> Failures);

internal sealed record StreamOutcome(JoinedOrder? Output, JoinFailure? Failure)
{
    internal static StreamOutcome Success(JoinedOrder output) => new(output, null);

    internal static StreamOutcome Failed(JoinFailure failure) => new(null, failure);
}

internal sealed record JoinInput(
    string Name,
    IReadOnlyList<JoinOrder> Orders,
    IReadOnlyList<JoinCustomer> Customers,
    IEqualityComparer<string> Comparer);

internal sealed record JoinFixture(
    JoinInput Input,
    IReadOnlyList<JoinedOrder> ExpectedOutputs,
    IReadOnlyList<JoinFailure> ExpectedFailures,
    IReadOnlyList<StreamOutcome> ExpectedStream)
{
    internal JoinFixture(
        string name,
        IReadOnlyList<JoinOrder> orders,
        IReadOnlyList<JoinCustomer> customers,
        IEqualityComparer<string> comparer,
        IReadOnlyList<JoinedOrder> expectedOutputs,
        IReadOnlyList<JoinFailure> expectedFailures,
        IReadOnlyList<StreamOutcome> expectedStream)
        : this(
            new JoinInput(name, orders, customers, comparer),
            expectedOutputs,
            expectedFailures,
            expectedStream)
    {
    }
}
