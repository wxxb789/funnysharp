#pragma warning disable xUnit1051

using FunnySharp.TestSupport.Batching;

namespace FunnySharp.Tests;

public sealed class BatchConsumerComparisonTests
{
    [Fact]
    public async Task FunnySharpAndHandBatchedOrderConsumersAgreeAcrossBatchingAndFailureFixtures()
    {
        await BatchComparison.VerifyAsync();
    }
}
