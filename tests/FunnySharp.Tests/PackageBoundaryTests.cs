using System.Reflection;

namespace FunnySharp.Tests;

public sealed class PackageBoundaryTests
{
    [Fact]
    public void CoreAssemblyReferencesOnlyPlatformAssemblies()
    {
        var assembly = Assembly.Load(new AssemblyName("FunnySharp"));
        var nonPlatformReferences = assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null && !IsPlatformAssembly(name))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(nonPlatformReferences);
    }

    private static bool IsPlatformAssembly(string name) =>
        name is "mscorlib" or "netstandard" or "System" ||
        name.StartsWith("System.", StringComparison.Ordinal);
}
