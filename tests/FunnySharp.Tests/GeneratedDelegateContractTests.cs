namespace FunnySharp.Tests;

public sealed class GeneratedDelegateContractTests
{
    [Fact]
    public void ModernRuntimeRejectsGeneratedDelegateAsyncMembersWithoutInvokingTargetsOrCallbacks()
    {
        var targets = 0;
        var callbacks = 0;
        AsyncCallback callback = _ => callbacks++;
        var state = new object();
        TryOperation<int> operation = (out int value) =>
        {
            targets++;
            value = 42;
            throw new InvalidOperationException("Unexpected delegate target invocation.");
        };
        StateTransition<int, int> transition = _ =>
        {
            targets++;
            throw new InvalidOperationException("Unexpected delegate target invocation.");
        };
        StateMachine<int, int, int, string> machine = (_, _) =>
        {
            targets++;
            throw new InvalidOperationException("Unexpected delegate target invocation.");
        };

        Assert.Throws<PlatformNotSupportedException>(() => operation.BeginInvoke(out _, callback, state));
        Assert.Throws<PlatformNotSupportedException>(() => operation.EndInvoke(out _, Task.CompletedTask));
        Assert.Throws<PlatformNotSupportedException>(() => transition.BeginInvoke(1, callback, state));
        Assert.Throws<PlatformNotSupportedException>(() => transition.EndInvoke(Task.CompletedTask));
        Assert.Throws<PlatformNotSupportedException>(() => machine.BeginInvoke(1, 2, callback, state));
        Assert.Throws<PlatformNotSupportedException>(() => machine.EndInvoke(Task.CompletedTask));
        Assert.Equal(0, targets);
        Assert.Equal(0, callbacks);
    }
}
