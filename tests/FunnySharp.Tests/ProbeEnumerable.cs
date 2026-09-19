using System.Collections;

namespace FunnySharp.Tests;

internal sealed class ProbeEnumerable<T>(IReadOnlyList<T> values) : IEnumerable<T>
{
    public int EnumeratorCount { get; private set; }

    public int ItemsYielded { get; private set; }

    public int DisposeCount { get; private set; }

    public IEnumerator<T> GetEnumerator()
    {
        EnumeratorCount++;
        return Enumerate().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private IEnumerable<T> Enumerate()
    {
        try
        {
            foreach (var value in values)
            {
                ItemsYielded++;
                yield return value;
            }
        }
        finally
        {
            DisposeCount++;
        }
    }
}
