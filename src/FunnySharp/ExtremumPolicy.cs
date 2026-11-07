namespace FunnySharp;

internal static class ExtremumPolicy
{
    internal static bool Replaces<T>(
        bool hasCurrent,
        T candidate,
        T current,
        IComparer<T> comparer,
        bool selectLargest)
    {
        if (!hasCurrent)
        {
            return true;
        }

        var comparison = comparer.Compare(candidate, current);
        return selectLargest ? comparison > 0 : comparison < 0;
    }
}
