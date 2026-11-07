using FunnySharp.TestSupport.FallibleReduction;

namespace FunnySharp.Tests;

public sealed class FallibleReductionComparisonTests
{
    [Fact]
    public void ConsumerComparisonProvesParityAndTheUpstreamSourceConsumptionDifference()
    {
        ReductionComparison.Verify();
    }

    [Fact]
    public async Task AsyncConsumerComparisonProvesParityAndTheUpstreamSourceConsumptionDifference()
    {
        await ReductionComparison.VerifyAsync();
    }
}
