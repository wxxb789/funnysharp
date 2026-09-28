namespace FunnySharp.TestSupport;

/// <summary>
/// Locates the repository root from a test assembly's output directory, for the tests that read
/// repository files.
/// </summary>
internal static class TestRepositoryRoot
{
    /// <summary>
    /// Walks up from the test assembly's output directory to the first directory that holds the
    /// FunnySharp.slnx marker file, or returns null when no ancestor directory holds it.
    /// </summary>
    internal static string? Find()
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

        return null;
    }
}
