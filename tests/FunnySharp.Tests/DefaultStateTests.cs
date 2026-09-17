namespace FunnySharp.Tests;

public sealed class DefaultStateTests
{
    private static readonly Result<int, string> DefaultResultField = default;
    private static readonly Validation<int, string> DefaultValidationField = default;

    [Fact]
    public void DefaultResultThrowsForStateAccessAndReportsUninitializedText()
    {
        var result = UninitializedResult();

        Assert.Equal("Uninitialized", result.ToString());
        AssertUninitializedResult(() => result.IsSuccess);
        AssertUninitializedResult(() => result.IsFailure);
        AssertUninitializedResult(() => result.TryGetValue(out _));
        AssertUninitializedResult(() => result.TryGetError(out _));
        AssertUninitializedResult(() => result.GetHashCode());
    }

    [Fact]
    public void DefaultResultThrowsForEveryCombinator()
    {
        var result = UninitializedResult();

        AssertUninitializedResult(() => result.Match(value => value + 1, error => error.Length));
        AssertUninitializedResult(() => result.Match(_ => { }, _ => { }));
        AssertUninitializedResult(() => result.Map(value => value + 1));
        AssertUninitializedResult(() => result.Bind(value => Result<int, string>.Success(value + 1)));
        AssertUninitializedResult(() => result.MapError(error => error.Length));
        AssertUninitializedResult(() => result.Ensure(value => value > 0, "invalid"));
        AssertUninitializedResult(() => result.Ensure(value => value > 0, value => $"invalid:{value}"));
        AssertUninitializedResult(() => result.Recover(error => error.Length));
        AssertUninitializedResult(() => result.RecoverWith(
            error => Result<int, string>.Success(error.Length)));
        AssertUninitializedResult(() => result.Zip(Result<int, string>.Success(2)));
        AssertUninitializedResult(() => result.Zip(
            Result<int, string>.Success(2),
            (first, second) => first + second));
        AssertUninitializedResult(() => result.Zip(
            Result<int, string>.Success(2),
            Result<int, string>.Success(3),
            (first, second, third) => first + second + third));
        AssertUninitializedResult(() => result.ZipWith(() => Result<int, string>.Success(2)));
        AssertUninitializedResult(() => result.Select(value => value + 1));
        AssertUninitializedResult(() => result.SelectMany(
            value => Result<int, string>.Success(value + 1),
            (left, right) => left + right));
    }

    [Fact]
    public void DefaultResultThrowsForEqualityAndOperators()
    {
        var result = UninitializedResult();
        var other = UninitializedResult();
        var initialized = Result<int, string>.Success(1);

        AssertUninitializedResult(() => result.Equals(initialized));
        AssertUninitializedResult(() => result.Equals((object)initialized));
        AssertUninitializedResult(() => initialized.Equals(result));
        AssertUninitializedResult(() => initialized.Equals(default(Result<int, string>)));
        AssertUninitializedResult(() => default(Result<int, string>).Equals(initialized));
        AssertUninitializedResult(() => initialized.Equals((object)result));
        AssertUninitializedResult(() => result == initialized);
        AssertUninitializedResult(() => initialized == result);
        AssertUninitializedResult(() => result != initialized);
        AssertUninitializedResult(() => initialized != result);
        AssertUninitializedResult(() => result == other);
        AssertUninitializedResult(() => result != other);

        Assert.False(initialized.Equals((object?)null));
    }

    [Fact]
    public void DefaultOutcomesInsideArraysAndFieldsThrowOnAccess()
    {
        var results = new Result<int, string>[1];
        var validations = new Validation<int, string>[1];

        AssertUninitializedResult(() => results[0].IsSuccess);
        AssertUninitializedResult(() => results[0].GetHashCode());
        AssertUninitializedValidation(() => validations[0].IsValid);
        AssertUninitializedValidation(() => validations[0].GetHashCode());

        AssertUninitializedResult(() => DefaultResultField.IsSuccess);
        AssertUninitializedValidation(() => DefaultValidationField.IsValid);
    }

    [Fact]
    public void DefaultValidationThrowsForStateAccessAndReportsUninitializedText()
    {
        var validation = UninitializedValidation();

        Assert.Equal("Uninitialized", validation.ToString());
        AssertUninitializedValidation(() => validation.IsValid);
        AssertUninitializedValidation(() => validation.IsInvalid);
        AssertUninitializedValidation(() => validation.TryGetValue(out _));
        AssertUninitializedValidation(() => validation.TryGetErrors(out _));
        AssertUninitializedValidation(() => validation.GetHashCode());
    }

    [Fact]
    public void DefaultValidationThrowsForEveryCombinator()
    {
        var validation = UninitializedValidation();

        AssertUninitializedValidation(() =>
            validation.Match(value => value + 1, errors => errors.Count));
        AssertUninitializedValidation(() => validation.Match(_ => { }, _ => { }));
        AssertUninitializedValidation(() => validation.Map(value => value + 1));
        AssertUninitializedValidation(() => validation.MapErrors(error => error.Length));
        AssertUninitializedValidation(() => validation.Zip(Validation<int, string>.Valid(2)));
        AssertUninitializedValidation(() => validation.Zip(
            Validation<int, string>.Valid(2),
            (first, second) => first + second));
        AssertUninitializedValidation(() => validation.Zip(
            Validation<int, string>.Valid(2),
            Validation<int, string>.Valid(3),
            (first, second, third) => first + second + third));
        AssertUninitializedValidation(() =>
            default(Validation<Func<int, int>, string>).Apply(Validation<int, string>.Valid(1)));
        AssertUninitializedValidation(() =>
            Validation<Func<int, int>, string>.Valid(value => value + 1)
                .Apply(default(Validation<int, string>)));
    }

    [Fact]
    public void DefaultValidationThrowsForEqualityAndOperators()
    {
        var validation = UninitializedValidation();
        var other = UninitializedValidation();
        var initialized = Validation<int, string>.Valid(1);

        AssertUninitializedValidation(() => validation.Equals(initialized));
        AssertUninitializedValidation(() => validation.Equals((object)initialized));
        AssertUninitializedValidation(() => initialized.Equals(validation));
        AssertUninitializedValidation(() => initialized.Equals(default(Validation<int, string>)));
        AssertUninitializedValidation(() => default(Validation<int, string>).Equals(initialized));
        AssertUninitializedValidation(() => initialized.Equals((object)validation));
        AssertUninitializedValidation(() => validation == initialized);
        AssertUninitializedValidation(() => initialized == validation);
        AssertUninitializedValidation(() => validation != initialized);
        AssertUninitializedValidation(() => initialized != validation);
        AssertUninitializedValidation(() => validation == other);
        AssertUninitializedValidation(() => validation != other);

        Assert.False(initialized.Equals((object?)null));
    }

    [Fact]
    public void DefaultOptionIsALegitimateNone()
    {
        var option = default(Option<int>);

        Assert.True(option.IsNone);
        Assert.False(option.IsSome);
        Assert.False(option.TryGetValue(out var value));
        Assert.Equal(0, value);
        Assert.Equal("none", option.Match(_ => "some", () => "none"));

        var someCalls = 0;
        var noneCalls = 0;
        option.Match(_ => { someCalls++; }, () => { noneCalls++; });

        Assert.Equal(0, someCalls);
        Assert.Equal(1, noneCalls);
        Assert.Equal(Option<int>.None, option);
        Assert.True(option.Map(item => item + 1).IsNone);
        Assert.True(option.Bind(item => Option.Some(item + 1)).IsNone);
        Assert.True(option.Filter(item => item > 0).IsNone);
        Assert.True(option.Zip(Option.Some(2)).IsNone);
        Assert.True(option.Zip(Option.Some(2), (first, second) => first + second).IsNone);
        Assert.True(option.Zip(
            Option.Some(2),
            Option.Some(3),
            (first, second, third) => first + second + third).IsNone);
        Assert.Equal(0, option.GetValueOrDefault());
        Assert.Equal(42, option.GetValueOr(42));
        Assert.Equal(Option.Some(42), option.OrElse(Option.Some(42)));
        Assert.Equal(Option<int>.None.GetHashCode(), option.GetHashCode());
        Assert.Equal("None", option.ToString());
        Assert.Null(option.ToNullable());
        Assert.True(new[] { Option.Some(1), default }.Sequence().IsNone);
    }

    [Fact]
    public void UninitializedResultAsyncExtensionsThrowSynchronously()
    {
        var result = UninitializedResult();

        AssertUninitializedResult(() => result.MapAsync(_ => Task.FromResult(1)));
        AssertUninitializedResult(() => result.MapAsync(
            (_, _) => Task.FromResult(1),
            CancellationToken.None));
        AssertUninitializedResult(() => result.BindAsync(
            _ => Task.FromResult(Result<int, string>.Success(1))));
        AssertUninitializedResult(() => result.BindAsync(
            (_, _) => Task.FromResult(Result<int, string>.Success(1)),
            CancellationToken.None));
        AssertUninitializedResult(() => result.MapValueAsync(_ => ValueTask.FromResult(1)));
        AssertUninitializedResult(() => result.MapValueAsync(
            (_, _) => ValueTask.FromResult(1),
            CancellationToken.None));
        AssertUninitializedResult(() => result.BindValueAsync(
            _ => ValueTask.FromResult(Result<int, string>.Success(1))));
        AssertUninitializedResult(() => result.BindValueAsync(
            (_, _) => ValueTask.FromResult(Result<int, string>.Success(1)),
            CancellationToken.None));

        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = ResultExtensions.MapAsync<int, string, int>(
                default(Result<int, string>),
                (Func<int, Task<int>>)null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = ResultExtensions.MapValueAsync<int, string, int>(
                default(Result<int, string>),
                (Func<int, ValueTask<int>>)null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = ResultExtensions.BindAsync<int, string, int>(
                default(Result<int, string>),
                (Func<int, Task<Result<int, string>>>)null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = ResultExtensions.BindValueAsync<int, string, int>(
                default(Result<int, string>),
                (Func<int, ValueTask<Result<int, string>>>)null!);
        });
    }

    [Fact]
    public void UninitializedValidationAsyncExtensionsThrowSynchronously()
    {
        var validation = UninitializedValidation();

        AssertUninitializedValidation(() => validation.MapAsync(_ => Task.FromResult(1)));
        AssertUninitializedValidation(() => validation.MapAsync(
            (_, _) => Task.FromResult(1),
            CancellationToken.None));
        AssertUninitializedValidation(() => validation.MapValueAsync(_ => ValueTask.FromResult(1)));
        AssertUninitializedValidation(() => validation.MapValueAsync(
            (_, _) => ValueTask.FromResult(1),
            CancellationToken.None));

        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = ValidationExtensions.MapAsync<int, int, string>(
                default(Validation<int, string>),
                (Func<int, Task<int>>)null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = ValidationExtensions.MapValueAsync<int, int, string>(
                default(Validation<int, string>),
                (Func<int, ValueTask<int>>)null!);
        });
    }

    [Fact]
    public void UninitializedTraversalFailsFast()
    {
        AssertUninitializedResult(() =>
            new[] { Result<int, string>.Success(1), default }.Sequence());
        AssertUninitializedResult(() =>
            new[] { 1, 2 }.Traverse(item => item == 1
                ? Result<int, string>.Success(item)
                : default(Result<int, string>)));

        AssertUninitializedValidation(() =>
            new[] { Validation<int, string>.Valid(1), default }.Sequence());
        AssertUninitializedValidation(() =>
            new[] { 1, 2 }.Traverse(item => item == 1
                ? Validation<int, string>.Valid(item)
                : default(Validation<int, string>)));
    }

    [Fact]
    public async Task UninitializedAsyncTraversalFailsFastWhenAwaited()
    {
        await AssertUninitializedResultAsync(async () =>
            await AsyncValues(Result<int, string>.Success(1), default).SequenceAsync());
        await AssertUninitializedResultAsync(async () =>
            await AsyncValues(1, 2).TraverseAsync(item => item == 1
                ? Result<int, string>.Success(item)
                : default(Result<int, string>)));
        await AssertUninitializedResultAsync(async () =>
            await AsyncValues(1, 2).TraverseValueAsync(item => ValueTask.FromResult(
                item == 1
                    ? Result<int, string>.Success(item)
                    : default(Result<int, string>))));

        await AssertUninitializedValidationAsync(async () =>
            await AsyncValues(Validation<int, string>.Valid(1), default).SequenceAsync());
        await AssertUninitializedValidationAsync(async () =>
            await AsyncValues(1, 2).TraverseAsync(item => item == 1
                ? Validation<int, string>.Valid(item)
                : default(Validation<int, string>)));
        await AssertUninitializedValidationAsync(async () =>
            await AsyncValues(1, 2).TraverseValueAsync(item => ValueTask.FromResult(
                item == 1
                    ? Validation<int, string>.Valid(item)
                    : default(Validation<int, string>))));
    }

    private static Result<int, string> UninitializedResult() => default;

    private static Validation<int, string> UninitializedValidation() => default;

    private static void AssertUninitializedResult(Action action)
    {
        var exception = Assert.Throws<InvalidOperationException>(action);
        Assert.Equal("The result has not been initialized.", exception.Message);
    }

    private static void AssertUninitializedResult(Func<object> action)
    {
        var exception = Assert.Throws<InvalidOperationException>(action);
        Assert.Equal("The result has not been initialized.", exception.Message);
    }

    private static void AssertUninitializedValidation(Action action)
    {
        var exception = Assert.Throws<InvalidOperationException>(action);
        Assert.Equal("The validation has not been initialized.", exception.Message);
    }

    private static void AssertUninitializedValidation(Func<object> action)
    {
        var exception = Assert.Throws<InvalidOperationException>(action);
        Assert.Equal("The validation has not been initialized.", exception.Message);
    }

    private static async Task AssertUninitializedResultAsync(Func<Task> action)
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(action);
        Assert.Equal("The result has not been initialized.", exception.Message);
    }

    private static async Task AssertUninitializedValidationAsync(Func<Task> action)
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(action);
        Assert.Equal("The validation has not been initialized.", exception.Message);
    }

    private static async IAsyncEnumerable<T> AsyncValues<T>(params T[] values)
    {
        foreach (var value in values)
        {
            yield return value;
            await Task.Yield();
        }
    }
}