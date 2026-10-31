namespace FunnySharp;

/// <summary>
/// Identifies the state of one field inside a partial change.
/// </summary>
/// <remarks>
/// The three states keep omission, an explicit clearing instruction, and a replacement value
/// distinct. A JSON property that is absent, a property whose value is <c>null</c>, and a property
/// carrying a replacement therefore remain observable as different patch states even when the
/// resulting candidate field happens to be empty.
/// </remarks>
public enum PatchChangeKind
{
    /// <summary>The field was not supplied and keeps its current value.</summary>
    Unchanged = 0,

    /// <summary>The field carries a replacement value.</summary>
    Set = 1,

    /// <summary>The field is explicitly cleared through the field's clear callback.</summary>
    Clear = 2,
}
