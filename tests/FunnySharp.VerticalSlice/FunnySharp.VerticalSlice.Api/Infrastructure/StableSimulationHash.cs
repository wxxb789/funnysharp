namespace FunnySharp.VerticalSlice.Infrastructure;

/// <summary>
/// The stable string hash the simulated stock and price formulas share. It cannot be
/// <see cref="string.GetHashCode()"/>, which is randomized per process: the comparison harness asserts
/// that the slice and the idiomatic comparison quote the same prices.
/// </summary>
internal static class StableSimulationHash
{
    internal static int Of(string value)
    {
        var hash = 17;
        foreach (var character in value)
        {
            hash = ((hash * 31) + character) & 0x7fffffff;
        }

        return hash;
    }
}
