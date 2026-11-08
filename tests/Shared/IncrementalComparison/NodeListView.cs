using System.Collections;
using FunnySharp.Incremental;

namespace FunnySharp.TestSupport.IncrementalComparison;

internal sealed class NodeListView<T> : IReadOnlyList<T>
{
    private readonly IncrementalNode<T>[] _nodes;

    internal NodeListView(IncrementalNode<T>[] nodes) => _nodes = nodes;

    public T this[int index] => _nodes[index].Value;

    public int Count => _nodes.Length;

    public IEnumerator<T> GetEnumerator()
    {
        for (var index = 0; index < _nodes.Length; index++)
        {
            yield return _nodes[index].Value;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}