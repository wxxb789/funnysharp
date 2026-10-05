namespace FunnySharp.Tests;

public sealed class StableCardinalityNullContractTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task SelectedNullIsRejectedByTheSelectedOptionFactory(int operation)
    {
        var source = Values((string?)null);
        var result = operation switch
        {
            0 => source.FirstOrNoneAsync(TestContext.Current.CancellationToken),
            1 => source.LastOrNoneAsync(TestContext.Current.CancellationToken),
            2 => source.SingleOrNoneAsync(TestContext.Current.CancellationToken),
            3 => source.ElementAtOrNoneAsync(0, TestContext.Current.CancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => { _ = await result; });
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public async Task MultipleItemsDoNotSelectTheNullFirstItemAsASingleValue()
    {
        Assert.True((await Values(null, "second").SingleOrNoneAsync(TestContext.Current.CancellationToken)).IsNone);
    }

    [Fact]
    public async Task NonEmptyConstructionPreservesNullFirstAndRestItems()
    {
        var option = await Values(null, "middle", null).ToNonEmptyOrNoneAsync(TestContext.Current.CancellationToken);
        Assert.True(option.TryGetValue(out var items));
        Assert.Null(items.First);
        Assert.Equal(3, items.Count);
        Assert.Equal(new string?[] { "middle", null }, items.Rest);
        Assert.Equal(new string?[] { null, "middle", null }, items.ToReadOnlyList());
    }

    private static async IAsyncEnumerable<string?> Values(params string?[] values)
    {
        await Task.CompletedTask;
        foreach (var value in values)
        {
            yield return value;
        }
    }
}
