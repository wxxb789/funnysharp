#pragma warning disable FS0017

namespace FunnySharp.TreeTransformations;

/// <summary>
/// A zero-allocation view of the current traversal path. The view is valid only during the
/// traversal callback that received it; the traversal reuses its segment buffer, so storing the view
/// or reading it after the callback observes later segments.
/// </summary>
/// <remarks>
/// Call <see cref="ToPath"/> or <see cref="ToLocation"/> to retain the path. The view exists so a
/// visitor can build a <see cref="Location"/> only when it records a failure instead of allocating
/// one path object per visited node.
/// </remarks>
public readonly struct TreePathView
{
    private readonly TreePathSegment[] _segments;
    private readonly int _count;

    internal TreePathView(TreePathSegment[] segments, int count)
    {
        _segments = segments;
        _count = count;
    }

    /// <summary>Gets the number of segments, which equals the current node's depth.</summary>
    public int Count => _count;

    /// <summary>Gets the segment at <paramref name="index"/>.</summary>
    /// <param name="index">The zero-based segment position.</param>
    /// <returns>The segment.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the view.</exception>
    public TreePathSegment this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            if (index >= _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _segments[index];
        }
    }

    /// <summary>Copies the view into an immutable <see cref="TreePath"/>.</summary>
    /// <returns>A new path that remains valid after the callback.</returns>
    public TreePath ToPath()
    {
        var segments = new TreePathSegment[_count];
        Array.Copy(_segments, segments, _count);

        return TreePath.FromOwnedSegments(segments);
    }

    /// <summary>Builds a <see cref="Location"/> from the view.</summary>
    /// <returns>A new location; this is the allocating diagnostic form.</returns>
    public Location ToLocation()
    {
        var location = Location.Root;

        for (var index = 0; index < _count; index++)
        {
            location = _segments[index].AppendTo(location);
        }

        return location;
    }

    /// <inheritdoc />
    public override string ToString() => ToLocation().ToString();
}
