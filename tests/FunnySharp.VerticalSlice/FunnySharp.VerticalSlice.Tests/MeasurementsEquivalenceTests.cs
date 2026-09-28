using FunnySharp.VerticalSlice.Measurements;

namespace FunnySharp.VerticalSlice.Tests;

/// <summary>
/// The comparison harness's own verdict rule. A body that matches while the status code differs must be
/// reported as a behavioral difference, which is the gate the evidence claim rests on.
/// </summary>
public sealed class MeasurementsEquivalenceTests
{
    [Fact]
    public void TheSameStatusAndPayloadIsEquivalent() =>
        Assert.True(Equivalence.Same(Measurement(200, """{"orderId":"<id>"}"""), Measurement(200, """{"orderId":"<id>"}""")));

    [Fact]
    public void TheSamePayloadUnderDifferentStatusesIsNotEquivalent() =>
        Assert.False(Equivalence.Same(Measurement(200, """{"error":"declined"}"""), Measurement(402, """{"error":"declined"}""")));

    [Fact]
    public void DifferentPayloadsUnderTheSameStatusAreNotEquivalent() =>
        Assert.False(Equivalence.Same(Measurement(200, """{"revision":1}"""), Measurement(200, """{"revision":2}""")));

    private static Measurement Measurement(int statusCode, string canonicalResponse) =>
        new(statusCode, 0L, 0d, 0d, 0d, 0d, canonicalResponse);
}
