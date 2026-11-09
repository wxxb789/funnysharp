using FunnySharp.TestSupport.OperationComparison;

namespace FunnySharp.OperationPlans.Tests;

public sealed class OperationComparisonTests
{
    private const int ExpectedChecks = 1612;

    [Fact]
    public async Task SharedVerifierPassesForEveryConsumer()
    {
        var checks = await OperationComparison.VerifyAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ExpectedChecks, checks);
    }
}
