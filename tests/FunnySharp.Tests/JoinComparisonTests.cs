using FunnySharp.TestSupport.JoinComparison;

namespace FunnySharp.Tests;

public sealed class JoinComparisonTests
{
    [Fact]
    public void HandIndexedAndFunnySharpConsumersAgreeOnOutputsFailuresAndPositions()
    {
        JoinComparisonVerifier.Verify();
    }

    [Fact]
    public void OrdinaryJoinDistinguishesNullSourceKeyFromDuplicateArgumentException()
    {
        var nullSource = new JoinInput(
            "null source",
            [new("O-1", null)],
            [new("C-1", "Ada")],
            StringComparer.Ordinal);
        var nullException = Assert.Throws<ArgumentNullException>(
            () => OrdinaryJoinConsumer.Join(nullSource));
        Assert.IsType<ArgumentNullException>(nullException);

        var duplicate = new JoinInput(
            "duplicate",
            [new("O-1", "C-1")],
            [new("C-1", "Ada"), new("C-1", "Duplicate")],
            StringComparer.Ordinal);
        var duplicateException = Assert.Throws<ArgumentException>(
            () => OrdinaryJoinConsumer.Join(duplicate));
        Assert.IsType<ArgumentException>(duplicateException);
    }
}
