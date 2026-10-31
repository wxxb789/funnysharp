using FunnySharp.TestSupport.Contracts;

namespace FunnySharp.Tests;

public sealed class ContractConsumerComparisonTests
{
    [Fact]
    public void ContractAndOrdinaryConsumersAgreeOnDecodingValidationGenerationShrinkingAndSchema() =>
        OrderComparison.Verify();
}
