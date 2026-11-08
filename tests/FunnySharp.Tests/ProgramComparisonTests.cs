using FunnySharp.TestSupport.ProgramComparison;

namespace FunnySharp.Tests;

public sealed class ProgramComparisonTests
{
    [Fact]
    public void SharedOrderWorkflowsAgreeOnFixedReportsAndParity()
    {
        ProgramComparisonVerifier.Verify();
    }
}
