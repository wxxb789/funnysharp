namespace FunnySharp;

/// <summary>
/// Identifies the structural reason a keyed lookup or join failed.
/// </summary>
public enum LookupErrorKind
{
    /// <summary>
    /// A null key was found where the declared non-null key domain was required.
    /// </summary>
    NullKey = 0,

    /// <summary>
    /// A key occurs more than once in the lookup input while at most one match was required.
    /// </summary>
    DuplicateKey = 1,

    /// <summary>
    /// A source row's key has no matching entry in the lookup input while exactly one was
    /// required.
    /// </summary>
    MissingKey = 2,
}
