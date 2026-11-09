using FunnySharp.TestSupport.TreeComparison;

namespace FunnySharp.TreeTransformationsMeasurements;

internal static class RecursiveDepthProbe
{
    internal static int Run(int depth)
    {
        var tree = ConfigFixtures.CreateDeep(depth);
        var item = ConfigFixtures.CreateDeepOverride(depth);
        var result = new RecursiveConfigEditor(tree).Apply([item]);

        if (!result.TryGetValue(out var actual))
        {
            Console.WriteLine($"invalid {depth}");
            return 2;
        }

        Console.WriteLine($"ok {depth} {ConfigKernel.Digest(actual!)}");
        return 0;
    }
}
