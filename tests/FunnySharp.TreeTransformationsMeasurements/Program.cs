using System.Globalization;

namespace FunnySharp.TreeTransformationsMeasurements;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 2
            && args[0] == "--recursive-depth"
            && int.TryParse(args[1], NumberStyles.None, CultureInfo.InvariantCulture, out var depth))
        {
            return RecursiveDepthProbe.Run(depth);
        }

        TreeMeasurements.Run();
        return 0;
    }
}
