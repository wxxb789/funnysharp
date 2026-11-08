using FunnySharp.TestSupport.KeyedConcurrencyComparison;

namespace FunnySharp.Tests;

public sealed class KeyedConcurrencyComparisonTests
{
    [Fact]
    public async Task FunnySharpAndCompetentBclConsumersAgreeOnPerKeyOutcomesAndBounds()
    {
        await KeyedComparisonVerifier.VerifyAsync();
    }
}
