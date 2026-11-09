using FunnySharp.TestSupport.TreeComparison;

namespace FunnySharp.TreeTransformations.Tests;

public sealed class TreeComparisonTests
{
    [Fact]
    public void EveryConfigurationConsumerMatchesTheSharedReferenceBehavior()
    {
        var checks = TreeComparison.Verify();

        Assert.True(checks > 0);
    }
}
