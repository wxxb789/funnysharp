using FunnySharp.TestSupport.NestedDataComparison;

namespace FunnySharp.Tests;

public sealed class NestedDataComparisonTests
{
    [Fact]
    public void SharedConsumersAgreeOnRowsRejectionsAndLocations()
    {
        NestedComparisonVerifier.Verify();
    }
}
