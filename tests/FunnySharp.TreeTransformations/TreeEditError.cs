#pragma warning disable FS0017

namespace FunnySharp.TreeTransformations;

/// <summary>Classifies a path-directed edit failure.</summary>
public enum TreeEditErrorKind : byte
{
    /// <summary>A named segment selected no child.</summary>
    PathNotFound = 0,

    /// <summary>An indexed segment was outside the child range or used on a named parent.</summary>
    IndexOutOfRange = 1,
}

/// <summary>A path-directed edit failure with the failing segment's location.</summary>
/// <param name="Kind">The failure kind.</param>
/// <param name="Location">The location of the segment that failed.</param>
/// <param name="Message">A human-readable explanation.</param>
public sealed record TreeEditError(TreeEditErrorKind Kind, Location Location, string Message);
