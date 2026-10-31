using FunnySharp.TestSupport.Patching;

namespace FunnySharp.Tests;

public sealed class PatchConsumerComparisonTests
{
    [Fact]
    public void PatchAndOrdinaryConsumersAgreeOnEveryFixture() =>
        ScheduleComparison.Verify();
}
