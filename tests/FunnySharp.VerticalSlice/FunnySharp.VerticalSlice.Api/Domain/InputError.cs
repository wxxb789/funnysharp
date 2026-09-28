namespace FunnySharp.VerticalSlice.Domain;

/// <summary>
/// An external-input validation error: the offending field, a machine-readable code, and a
/// human-readable message. The HTTP boundary groups these into <c>HttpValidationProblemDetails</c>
/// in the order the validator produced them.
/// </summary>
public readonly record struct InputError(string Field, string Code, string Message);
