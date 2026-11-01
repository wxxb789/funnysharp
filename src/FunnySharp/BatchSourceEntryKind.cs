namespace FunnySharp;

/// <summary>
/// Describes the state of one entry returned by a batch source.
/// </summary>
public enum BatchSourceEntryKind
{
    /// <summary>The entry is an uninitialized default and is not a valid source response.</summary>
    Unset = 0,

    /// <summary>The entry carries a value for its key.</summary>
    Found = 1,

    /// <summary>The entry carries a typed failure for its key.</summary>
    Failed = 2,
}
