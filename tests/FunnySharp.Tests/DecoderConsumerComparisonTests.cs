using FunnySharp.TestSupport.Decoding;

namespace FunnySharp.Tests;

public sealed class DecoderConsumerComparisonTests
{
    [Fact]
    public void DecoderOrdinaryAndValidationConsumersAgreeOnEveryFixture() =>
        OrderComparison.Verify();
}
