namespace FunnySharp;

/// <summary>
/// Describes why one batched read did not produce a value.
/// </summary>
public enum BatchReadErrorKind
{
    /// <summary>The source did not return an entry for the requested key.</summary>
    Missing = 0,

    /// <summary>The source returned a typed failure for this key.</summary>
    KeyFailure = 1,

    /// <summary>The source failed the whole batch.</summary>
    BatchFailure = 2,

    /// <summary>The source response did not satisfy the batch contract.</summary>
    UnexpectedResponse = 3,
}
