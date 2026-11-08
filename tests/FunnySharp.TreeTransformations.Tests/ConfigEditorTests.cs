using FunnySharp.TestSupport.TreeComparison;
using FunnySharp.TreeTransformations;

namespace FunnySharp.TreeTransformations.Tests;

public sealed class ConfigEditorTests
{
    [Fact]
    public void ModerateDepthMatchesTheRecursiveReference()
    {
        const int depth = 1_000;
        var baseTree = ConfigFixtures.CreateDeep(depth);
        var item = ConfigFixtures.CreateDeepOverride(depth);
        var reference = new RecursiveConfigEditor(baseTree).Apply(new[] { item });

        Assert.True(reference.TryGetValue(out var expectedTree));

        foreach (var editor in CreateTypedEditors(baseTree))
        {
            var actual = editor.Apply(new[] { item });

            Assert.True(actual.TryGetValue(out var actualTree), editor.Name);
            Assert.Equal(ConfigKernel.ToCanonicalJson(expectedTree!), ConfigKernel.ToCanonicalJson(actualTree!));
            Assert.Equal("2", DeepLeafValue(actualTree!, item));
        }
    }

    [Fact]
    public void DeepPathsDoNotExhaustTheStack()
    {
        const int depth = 10_000;
        var baseTree = ConfigFixtures.CreateDeep(depth);
        var item = ConfigFixtures.CreateDeepOverride(depth);

        foreach (var editor in new IConfigEditor[]
        {
            new IterativeConfigEditor(baseTree),
            new GenericPathConfigEditor(baseTree),
            new ZipperConfigEditor(baseTree),
        })
        {
            editor.ResetStats();
            var result = editor.Apply(new[] { item });

            Assert.True(result.TryGetValue(out var actual), editor.Name);
            Assert.Equal("2", DeepLeafValue(actual!, item));
        }
    }

    [Fact]
    public void FailFastStopsAtTheFirstError()
    {
        var baseTree = ConfigFixtures.CreateBase();
        var overrides = ConfigFixtures.CreateMixedOverrides();

        foreach (var editor in CreateTypedEditors(baseTree).Append<IConfigEditor>(new JsonNodeConfigEditor(baseTree)))
        {
            editor.ResetStats();
            var result = editor.ApplyFirst(overrides);

            Assert.True(result.TryGetError(out var error), editor.Name);
            Assert.Equal(ConfigErrorKind.IndexOutOfRange, error!.Kind);
            Assert.Equal("services[64]", error.Path);
            Assert.Equal(1, editor.Stats.Errors);
        }
    }

    [Fact]
    public void ApplyFirstMatchesApplyOnValidOverrides()
    {
        var baseTree = ConfigFixtures.CreateBase();
        var overrides = ConfigFixtures.CreateValidOverrides();
        var reference = new IterativeConfigEditor(baseTree).Apply(overrides);

        Assert.True(reference.TryGetValue(out var expectedTree));

        foreach (var editor in CreateTypedEditors(baseTree).Append<IConfigEditor>(new JsonNodeConfigEditor(baseTree)))
        {
            var result = editor.ApplyFirst(overrides);

            Assert.True(result.TryGetValue(out var actualTree), editor.Name);
            Assert.Equal(
                ConfigKernel.ToCanonicalJson(expectedTree!),
                ConfigKernel.ToCanonicalJson(actualTree!));
        }
    }

    [Fact]
    public void JsonNodeRoundTripsEscapedValues()
    {
        var baseTree = ConfigNode.Section("root", ConfigNode.Leaf("note", "a\"b\\c"));
        var editor = new JsonNodeConfigEditor(baseTree);
        var item = new ConfigOverride("note", TreePath.Parse("note"), "next");
        var result = editor.Apply(new[] { item });

        Assert.True(result.TryGetValue(out var actualTree));
        Assert.Equal("next", actualTree!.Children[0].Value);
        Assert.True(editor.Validate().TryGetValue(out _));
    }

    [Fact]
    public void ApplyFirstOnAnInvalidBaseReturnsTheFirstBaseError()
    {
        var baseTree = ConfigFixtures.CreateInvalidBase();

        foreach (var editor in CreateTypedEditors(baseTree).Append<IConfigEditor>(new JsonNodeConfigEditor(baseTree)))
        {
            var result = editor.ApplyFirst([]);

            Assert.True(result.TryGetError(out var error), editor.Name);
            Assert.Equal(ConfigErrorKind.InvalidBaseValue, error!.Kind);
            Assert.Equal("services[0].endpoints[0].port", error.Path);
        }
    }

    private static IEnumerable<IConfigEditor> CreateTypedEditors(ConfigNode baseTree) =>
    [
        new RecursiveConfigEditor(baseTree),
        new IterativeConfigEditor(baseTree),
        new GenericFullConfigEditor(baseTree),
        new GenericPathConfigEditor(baseTree),
        new ZipperConfigEditor(baseTree),
    ];

    private static string DeepLeafValue(ConfigNode tree, ConfigOverride item)
    {
        var resolved = TreeRewrite.Resolve(tree, ConfigTreeAdapter.Instance, item.Path);

        Assert.True(resolved.TryGetValue(out var node));

        return node.Node.Value!;
    }
}
