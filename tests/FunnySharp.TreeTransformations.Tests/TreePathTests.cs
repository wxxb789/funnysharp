using FunnySharp.TreeTransformations;

namespace FunnySharp.TreeTransformations.Tests;

public sealed class TreePathTests
{
    [Fact]
    public void ParsesNamedAndIndexedSegments()
    {
        var path = TreePath.Parse("services[3].endpoints[0].port");

        Assert.Equal(5, path.Count);
        Assert.Equal(TreePathSegment.Named("services"), path[0]);
        Assert.Equal(TreePathSegment.At(3), path[1]);
        Assert.Equal("services[3].endpoints[0].port", path.ToString());
    }

    [Fact]
    public void EmptyTextIsTheRootPath()
    {
        var path = TreePath.Parse(string.Empty);

        Assert.Equal(0, path.Count);
        Assert.Equal(TreePath.Root, path);
        Assert.Equal(string.Empty, path.ToString());
    }

    [Fact]
    public void AppendingCopiesThePath()
    {
        var root = TreePath.Parse("services");
        var appended = root.Append(TreePathSegment.At(3));

        Assert.Equal(1, root.Count);
        Assert.Equal(2, appended.Count);
        Assert.Equal("services[3]", appended.ToString());
    }

    [Theory]
    [InlineData("services[")]
    [InlineData("services..port")]
    [InlineData("[0]")]
    [InlineData("services[-1]")]
    [InlineData("services[1")]
    [InlineData("services[1].")]
    [InlineData("services[1].2bad")]
    public void RejectsMalformedPaths(string text) =>
        Assert.Throws<FormatException>(() => TreePath.Parse(text));
}
