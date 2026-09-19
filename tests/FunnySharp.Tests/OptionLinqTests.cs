namespace FunnySharp.Tests;

public sealed class OptionLinqTests
{
    [Fact]
    public void SelectMatchesMapForSomeAndNone()
    {
        foreach (var option in new[] { Option.Some(2), Option.None<int>(), default(Option<int>) })
        {
            Assert.Equal(
                option.Map(static value => value * 2),
                option.Select(static value => value * 2));
            Assert.Equal(
                option.Map(static value => $"value:{value}"),
                option.Select(static value => $"value:{value}"));
            Assert.Equal(
                option.Map<string?>(_ => null),
                option.Select<string?>(_ => null));
        }
    }

    [Fact]
    public void LinqQuerySyntaxUsesFailFastBinding()
    {
        var binderCalls = 0;
        var projectorCalls = 0;
        Func<int, Option<int>> next = value =>
        {
            binderCalls++;
            return Option.Some(value + 1);
        };
        Func<int, int, int> project = (left, right) =>
        {
            projectorCalls++;
            return left * right;
        };

        var single =
            from value in Option.Some(3)
            select value * 2;
        var success =
            from left in Option.Some(2)
            from right in next(left)
            select project(left, right);
        var failure =
            from left in Option.None<int>()
            from right in next(left)
            select project(left, right);
        var intermediateFailure =
            from left in Option.Some(3)
            from right in Option<int>.None
            select project(left, right);

        Assert.Equal(Option.Some(6), single);
        Assert.Equal(Option.Some(6), success);
        Assert.True(failure.IsNone);
        Assert.True(intermediateFailure.IsNone);
        Assert.Equal(1, binderCalls);
        Assert.Equal(1, projectorCalls);
    }

    [Fact]
    public void SelectManyRunsTheProjectorOnlyWhenBothOptionsArePresent()
    {
        var binderCalls = 0;
        var projectorCalls = 0;
        Func<int, Option<string>> binder = value =>
        {
            binderCalls++;
            return value > 0 ? Option.Some($"value:{value}") : Option.None<string>();
        };
        Func<int, string, string> projector = (first, intermediate) =>
        {
            projectorCalls++;
            return $"{first}+{intermediate}";
        };

        var present = Option.Some(2).SelectMany(binder, projector);
        var absent = Option.None<int>().SelectMany(binder, projector);
        var intermediateAbsent = Option.Some(-1).SelectMany(binder, projector);

        Assert.True(present.TryGetValue(out var combined));
        Assert.Equal("2+value:2", combined);
        Assert.True(absent.IsNone);
        Assert.True(intermediateAbsent.IsNone);
        Assert.Equal(2, binderCalls);
        Assert.Equal(1, projectorCalls);
    }

    [Fact]
    public void SelectAndSelectManyNormalizeNullProjectionResultsToNone()
    {
        Assert.True(Option.Some(1).Select<string?>(_ => null).IsNone);
        Assert.True(Option.None<int>().Select<string?>(_ => "value").IsNone);
        Assert.True(Option.Some(1)
            .SelectMany(_ => Option.Some("value"), (_, _) => (string?)null)
            .IsNone);
        Assert.True(Option.None<int>()
            .SelectMany(_ => Option.Some("value"), (_, _) => "value")
            .IsNone);
    }

    [Fact]
    public void SelectAndSelectManyPreserveCallbackExceptionIdentity()
    {
        var exception = new InvalidOperationException("callback failed");
        Func<int, int> selector = _ => throw exception;
        Func<int, Option<int>> binder = _ => throw exception;
        Func<int, int, int> projector = (_, _) => throw exception;

        Assert.Same(exception, Assert.Throws<InvalidOperationException>(() =>
            Option.Some(1).Select(selector)));
        Assert.Same(exception, Assert.Throws<InvalidOperationException>(() =>
            Option.Some(1).SelectMany(binder, static (left, right) => left + right)));
        Assert.Same(exception, Assert.Throws<InvalidOperationException>(() =>
            Option.Some(1).SelectMany(static value => Option.Some(value), projector)));
        Assert.Same(exception, Assert.Throws<InvalidOperationException>(() =>
        {
            var query = from left in Option.Some(1)
                        from right in Option.Some(2)
                        select projector(left, right);
        }));

        Assert.True(default(Option<int>).Select(selector).IsNone);
        Assert.True(default(Option<int>).SelectMany(binder, projector).IsNone);
        Assert.True(Option.Some(1).SelectMany(_ => default(Option<int>), projector).IsNone);
    }

    [Fact]
    public void SelectAndSelectManyRejectNullArgumentsEagerly()
    {
        Assert.Throws<ArgumentNullException>(() => Option.Some(1).Select<int>(null!));
        Assert.Throws<ArgumentNullException>(() => Option.None<int>().Select<int>(null!));
        Assert.Throws<ArgumentNullException>(() =>
            Option.Some(1).SelectMany<string, int>(
                null!,
                static (first, intermediate) => first + intermediate.Length));
        Assert.Throws<ArgumentNullException>(() =>
            Option.None<int>().SelectMany<string, int>(
                null!,
                static (first, intermediate) => first + intermediate.Length));
        Assert.Throws<ArgumentNullException>(() =>
            Option.Some(1).SelectMany(
                static value => Option.Some(value.ToString()),
                (Func<int, string, int>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            Option.None<int>().SelectMany(
                static value => Option.Some(value.ToString()),
                (Func<int, string, int>)null!));
    }
}
