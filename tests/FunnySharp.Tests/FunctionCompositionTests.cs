namespace FunnySharp.Tests;

public sealed class FunctionCompositionTests
{
    [Fact]
    public void PipeAppliesFunctionAndPermitsNullValues()
    {
        string? value = null;

        var result = value.Pipe(text => text ?? "fallback");

        Assert.Equal("fallback", result);
    }

    [Fact]
    public void PipeRejectsNullFunction()
    {
        Assert.Throws<ArgumentNullException>(() => 1.Pipe<int, int>(null!));
    }

    [Fact]
    public void ComposeObeysIdentityAndAssociativity()
    {
        Func<int, int> identity = value => value;
        Func<int, int> increment = value => value + 1;
        Func<int, int> doubleValue = value => value * 2;
        Func<int, int> formatLength = value => $"value:{value}".Length;

        Assert.Equal(increment(4), identity.Compose(increment)(4));
        Assert.Equal(increment(4), increment.Compose(identity)(4));
        Assert.Equal(
            increment.Compose(doubleValue).Compose(formatLength)(4),
            increment.Compose(doubleValue.Compose(formatLength))(4));
    }

    [Fact]
    public void ComposeEvaluatesLeftToRight()
    {
        var calls = new List<string>();
        Func<int, int> first = value =>
        {
            calls.Add("first");
            return value + 1;
        };
        Func<int, int> second = value =>
        {
            calls.Add("second");
            return value * 2;
        };

        var result = first.Compose(second)(3);

        Assert.Equal(8, result);
        Assert.Equal(["first", "second"], calls);
    }

    [Fact]
    public void ComposePreservesExceptionIdentityAndShortCircuits()
    {
        var expected = new InvalidOperationException("first failed");
        var secondCalled = false;
        Func<int, int> first = _ => throw expected;
        Func<int, int> second = value =>
        {
            secondCalled = true;
            return value;
        };

        var actual = Assert.Throws<InvalidOperationException>(() => first.Compose(second)(1));

        Assert.Same(expected, actual);
        Assert.False(secondCalled);
    }

    [Fact]
    public void ComposeRejectsNullDelegatesEagerly()
    {
        Func<int, int> function = value => value;

        Assert.Throws<ArgumentNullException>(() => ((Func<int, int>)null!).Compose(function));
        Assert.Throws<ArgumentNullException>(() => function.Compose<int, int, int>(null!));
    }

    [Fact]
    public void CurryAndUncurryAreEquivalentToTheOriginalBinaryFunction()
    {
        Func<int, int, int> subtract = (left, right) => left - right;

        var curried = subtract.Curry();
        var uncurried = curried.Uncurry();

        Assert.Equal(subtract(9, 4), curried(9)(4));
        Assert.Equal(subtract(9, 4), uncurried(9, 4));
    }

    [Fact]
    public void PartialAndFlipPreserveBinaryArgumentSemantics()
    {
        Func<string, string, string> combine = (left, right) => $"{left}:{right}";

        var partial = combine.Partial("left");
        var flipped = combine.Flip();

        Assert.Equal("left:right", partial("right"));
        Assert.Equal("right:left", flipped("left", "right"));
    }

    [Fact]
    public void BinaryDelegateHelpersRejectNullFunctions()
    {
        Assert.Throws<ArgumentNullException>(() => ((Func<int, int, int>)null!).Curry());
        Assert.Throws<ArgumentNullException>(() => ((Func<int, Func<int, int>>)null!).Uncurry());
        Assert.Throws<ArgumentNullException>(() => ((Func<int, int, int>)null!).Partial(1));
        Assert.Throws<ArgumentNullException>(() => ((Func<int, int, int>)null!).Flip());
    }

    [Fact]
    public void TapReturnsTheOriginalValueAfterObservingIt()
    {
        var value = new object();
        object? observed = null;

        var result = value.Tap(item => observed = item);

        Assert.Same(value, result);
        Assert.Same(value, observed);
    }

    [Fact]
    public void TapPreservesObserverExceptionIdentity()
    {
        var expected = new InvalidOperationException("observer failed");

        var actual = Assert.Throws<InvalidOperationException>(() => 1.Tap(_ => throw expected));

        Assert.Same(expected, actual);
    }

    [Fact]
    public void TapRejectsNullAction()
    {
        Assert.Throws<ArgumentNullException>(() => 1.Tap<int>(null!));
    }
}
