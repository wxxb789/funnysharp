namespace FunnySharp;

/// <summary>
/// States how a contract field treats a missing object property and an explicit JSON null.
/// </summary>
public enum ContractFieldPresence
{
    /// <summary>The property is required and an explicit null reaches the member contract.</summary>
    Required,

    /// <summary>
    /// A missing property is <see cref="Option{T}.None"/>; an explicit null reaches the member
    /// contract and normally becomes a null error.
    /// </summary>
    MissingIsAbsence,

    /// <summary>
    /// A missing property and an explicit null are both <see cref="Option{T}.None"/>.
    /// </summary>
    MissingOrNullIsAbsence,
}
