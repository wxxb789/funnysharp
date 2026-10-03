namespace FunnySharp.Tests;

public sealed class StableDiagnosticOutputContractTests
{
    [Fact]
    public void StateAndTransitionDiagnosticsReturnNonNullTextForNullableAndUndefinedValues()
    {
        var change = StateChange<string?, string?>.To(null, new string?[] { null });
        Assert.NotNull(change.ToString());
        Assert.NotNull(TransitionResult<string?, string?, string?>.Applied(change).ToString());
        Assert.NotNull(TransitionResult<string?, string?, string?>.Rejected(null).ToString());
        Assert.NotNull(TransitionResult<string?, string?, string?>.Failed(null).ToString());
        Assert.NotNull(TransitionResult<string?, string?, string?>.Undefined().ToString());
    }

    [Fact]
    public void ActivePayloadFormattingExceptionsPropagateByIdentityFromBothDiagnostics()
    {
        var expected = new InvalidOperationException("payload formatting");
        var payload = new FormattingPayload(expected);
        var state = StateChange<FormattingPayload, string>.To(payload);
        var output = StateChange<int, FormattingPayload>.To(1, payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => state.ToString()));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => output.ToString()));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() =>
            TransitionResult<FormattingPayload, string, string>.Applied(state).ToString()));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() =>
            TransitionResult<int, string, FormattingPayload>.Rejected(payload).ToString()));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() =>
            TransitionResult<int, string, FormattingPayload>.Failed(payload).ToString()));
    }

    private sealed class FormattingPayload(InvalidOperationException exception)
    {
        public override string ToString() => throw exception;
    }
}
