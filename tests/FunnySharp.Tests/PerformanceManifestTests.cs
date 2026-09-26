using System.Text.Json;

namespace FunnySharp.Tests;

public sealed class PerformanceManifestTests
{
    [Fact]
    public void ManifestFilesArraysStayOrdinalSortedAndForwardSlashed()
    {
        var repositoryRoot = FindRepositoryRoot();
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

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FunnySharp.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the FunnySharp repository root.");
    }
}
