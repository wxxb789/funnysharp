#pragma warning disable FS0017

namespace FunnySharp.TestSupport.TreeComparison;

/// <summary>Classifies a configuration apply or validation failure.</summary>
internal enum ConfigErrorKind : byte
{
    PathNotFound = 0,
    IndexOutOfRange = 1,
    NotAValue = 2,
    InvalidValue = 3,
    DuplicatePath = 4,
    InvalidBaseValue = 5,
}

/// <summary>A configuration failure with a compositional location.</summary>
/// <param name="Kind">The failure kind.</param>
/// <param name="Location">The failing location.</param>
/// <param name="Message">A fixed explanation shared by every consumer.</param>
internal sealed record ConfigError(ConfigErrorKind Kind, Location Location, string Message)
{
    internal string Path => Location.ToString();
}
