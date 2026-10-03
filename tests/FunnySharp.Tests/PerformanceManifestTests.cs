using System.Text.Json;
using FunnySharp.TestSupport;

namespace FunnySharp.Tests;

public sealed class PerformanceManifestTests
{
    [Fact]
    public void BenchmarkInputsCoverShippingSourcesProjectsLocksAndImports()
    {
        var root = TestRepositoryRoot.Find()
            ?? throw new DirectoryNotFoundException("Could not locate the FunnySharp repository root.");
        var required = Directory.EnumerateFiles(Path.Combine(root, "src"), "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .Where(path => !path.Split('/').Any(part => part is "bin" or "obj"))
            .Where(path => path.EndsWith(".cs", StringComparison.Ordinal)
                || path.EndsWith(".csproj", StringComparison.Ordinal)
                || path.EndsWith(".props", StringComparison.Ordinal)
                || path.EndsWith(".targets", StringComparison.Ordinal)
                || path.EndsWith("/packages.lock.json", StringComparison.Ordinal)
                || Path.GetFileName(path).StartsWith("AnalyzerReleases.", StringComparison.Ordinal))
            .Concat(new[] { "Directory.Build.props", "README.md", "global.json" })
            .ToArray();

        foreach (var name in new[] { "baseline.json", "competitor-baseline.json" })
        {
            using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "eng", "performance", name)));
            var files = manifest.RootElement.GetProperty("benchmarkInput").GetProperty("files")
                .EnumerateArray().Select(file => file.GetString()!).ToHashSet(StringComparer.Ordinal);
            Assert.All(required, file => Assert.Contains(file, files));
        }
    }

    [Fact]
    public void ManifestFilesArraysStayOrdinalSortedAndForwardSlashed()
    {
        var repositoryRoot = TestRepositoryRoot.Find()
            ?? throw new DirectoryNotFoundException("Could not locate the FunnySharp repository root.");
        var manifestPaths = Directory.GetFiles(Path.Combine(repositoryRoot, "eng", "performance"), "*.json")
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(manifestPaths);

        foreach (var manifestPath in manifestPaths)
        {
            using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
            foreach (var section in new[] { "benchmarkInput", "protocol" })
            {
                var files = manifest.RootElement.GetProperty(section).GetProperty("files").EnumerateArray()
                    .Select(file => file.GetString())
                    .ToList();

                Assert.All(files, file => Assert.NotNull(file));
                Assert.All(files, file => Assert.False(
                    file!.Contains('\\'),
                    $"{Path.GetFileName(manifestPath)} {section}.files entry '{file}' must be forward-slashed."));

                for (var index = 1; index < files.Count; index++)
                {
                    Assert.True(
                        string.CompareOrdinal(files[index - 1], files[index]) < 0,
                        $"{Path.GetFileName(manifestPath)} {section}.files must be ordinal-sorted without duplicates: "
                        + $"'{files[index - 1]}' must precede '{files[index]}'.");
                }
            }
        }
    }
}
