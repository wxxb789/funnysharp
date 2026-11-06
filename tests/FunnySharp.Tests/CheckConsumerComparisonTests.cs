using FunnySharp.TestSupport.Checks;

namespace FunnySharp.Tests;

public sealed class CheckConsumerComparisonTests
{
    [Fact]
    public void CurrentIdiomsAndCheckOperationsProduceTheSameSynchronousOutcomes()
    {
        CheckComparison.Verify();
    }

    [Fact]
    public async Task CurrentIdiomsAndCheckOperationsProduceTheSameAsynchronousOutcomes()
    {
        await CheckComparison.VerifyAsync();
    }
}
