using FunnySharp.TestSupport.SummaryComparison;

namespace FunnySharp.Tests;

public sealed class SummaryComparisonTests
{
    [Fact]
    public void ComposedSummariesMatchTheOrdinaryAndSeparateConsumersInOneTraversal()
    {
        SummaryComparisonVerifier.Verify();
    }

    [Fact]
    public async Task AsyncComposedSummariesMatchTheOrdinaryAndSeparateConsumersInOneTraversal()
    {
        await SummaryComparisonVerifier.VerifyAsync();
    }
}
