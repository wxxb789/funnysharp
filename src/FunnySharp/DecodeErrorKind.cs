namespace FunnySharp;

/// <summary>
/// Identifies the structural category of a <see cref="DecodeError"/>.
/// </summary>
/// <remarks>
/// The kinds are deliberately distinct so a consumer can react to absence, an explicit null, a
/// wrong JSON shape, an unparseable value, an empty required value, and a rejected domain value
/// without inspecting diagnostic text. <see cref="UnsupportedField"/> and
/// <see cref="DuplicateField"/> describe the allowed-field boundary of a partial change.
/// </remarks>
public enum DecodeErrorKind
{
    /// <summary>A required object property was not present.</summary>
    Missing = 0,

    /// <summary>An explicit JSON null was found where a decoded value was required.</summary>
    Null = 1,

    /// <summary>A JSON value had a kind the decoder does not accept, such as a string where a number is required.</summary>
    WrongKind = 2,

    /// <summary>A JSON value had the expected kind but could not be parsed as the target type.</summary>
    Malformed = 3,

    /// <summary>An empty string or array was found where a non-empty value was required.</summary>
    Empty = 4,

    /// <summary>A caller-supplied domain check rejected the decoded value.</summary>
    InvalidValue = 5,

    /// <summary>An input object contains a property that is not part of the allowed change.</summary>
    UnsupportedField = 6,

    /// <summary>An input object supplies the same property name more than once.</summary>
    DuplicateField = 7,
}
