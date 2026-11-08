namespace FunnySharp;

/// <summary>
/// Describes the structural reason a nested data expansion could not produce rows.
/// </summary>
/// <remarks>
/// The kinds distinguish a null child collection from an unequal-length pair. Empty collections are
/// not failures; they produce zero rows. A missing or malformed field is a
/// <see cref="DecodeError"/> produced by the decoder before expansion, not one of these values.
/// <see cref="NullChildSequence"/> is zero, so <c>default(NestedRowError)</c> reports that kind at
/// record 0; the expansion operations never produce a default value.
/// </remarks>
public enum NestedRowErrorKind
{
    /// <summary>The single child selector returned a runtime null instead of a list.</summary>
    NullChildSequence = 0,

    /// <summary>The first child selector of an aligned or Cartesian expansion returned null.</summary>
    NullFirstChildSequence = 1,

    /// <summary>The second child selector of an aligned or Cartesian expansion returned null.</summary>
    NullSecondChildSequence = 2,

    /// <summary>The two aligned child sequences have different lengths for one record.</summary>
    ChildCountMismatch = 3,
}
