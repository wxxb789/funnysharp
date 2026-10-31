using System.Diagnostics.CodeAnalysis;

#pragma warning disable FS0017

namespace FunnySharp;

using DiagnosticLocation = Location;

/// <summary>
/// A deterministic diagnostic produced while decoding untrusted input.
/// </summary>
/// <remarks>
/// A decoder never throws a <see cref="DecodeError"/>; it carries errors inside
/// <see cref="Validation{TValue, TError}"/>. <see cref="Path"/> is the stable location contract,
/// while <see cref="Location"/> is the structured experimental view. Errors are created relative
/// to the decoder's current input and gain their field and element prefixes from the enclosing
/// <c>Field</c> and <c>List</c> combinators, so the caller never assembles paths.
/// </remarks>
public sealed class DecodeError : IEquatable<DecodeError>
{
    private readonly DiagnosticLocation location;

    private DecodeError(
        DiagnosticLocation location,
        DecodeErrorKind kind,
        string? expected,
        string? detail)
    {
        this.location = location;
        Kind = kind;
        Expected = expected;
        Detail = detail;
    }

    /// <summary>
    /// Gets the rendered location path, such as <c>customer.addresses[1].postalCode</c>.
    /// </summary>
    /// <remarks>
    /// The root location renders as an empty string. The rendered text is cached by the
    /// underlying <see cref="FunnySharp.Location"/> after the first access. This property is the stable
    /// location contract; use <see cref="Location"/> only when structured segments are needed.
    /// </remarks>
    public string Path => location.ToString();

    /// <summary>
    /// Gets the structured location of this error.
    /// </summary>
    /// <remarks>
    /// This member is marked <c>[Experimental("FS0017")]</c> because the <see cref="FunnySharp.Location"/>
    /// type carries no compatibility promise and may change or be removed. Use <see cref="Path"/>
    /// for the stable diagnostic contract.
    /// </remarks>
    [Experimental("FS0017")]
    public DiagnosticLocation Location => location;

    /// <summary>Gets the structural category of this error.</summary>
    public DecodeErrorKind Kind { get; }

    /// <summary>
    /// Gets the value or JSON kind the decoder expected, when the kind supplies one.
    /// </summary>
    public string? Expected { get; }

    /// <summary>
    /// Gets optional diagnostic context, such as the actual JSON kind or a caller-supplied
    /// domain message.
    /// </summary>
    public string? Detail { get; }

    /// <summary>
    /// Creates an error relative to the decoder input that produces it.
    /// </summary>
    /// <param name="kind">The structural category.</param>
    /// <param name="expected">The expected value or JSON kind, or <see langword="null"/>.</param>
    /// <param name="detail">Optional diagnostic context, or <see langword="null"/>.</param>
    /// <returns>An error whose location is the root of the current decoder input.</returns>
    /// <remarks>
    /// This factory is intended for custom decoders built with <c>Decoder.From</c> and for
    /// domain validation. The structural combinators prefix the returned error's location when
    /// the decoder is nested, so callers do not build paths. Reading <see cref="Path"/> on the
    /// returned value before it is nested renders an empty string.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a defined <see cref="DecodeErrorKind"/>.</exception>
    public static DecodeError Create(
        DecodeErrorKind kind,
        string? expected = null,
        string? detail = null)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        return new DecodeError(DiagnosticLocation.Root, kind, expected, detail);
    }

    /// <summary>
    /// Returns a value indicating whether this error and <paramref name="other"/> have the same
    /// location, kind, expected text, and detail.
    /// </summary>
    /// <param name="other">The error to compare, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the errors are structurally equal; otherwise, <see langword="false"/>.</returns>
    public bool Equals(DecodeError? other) =>
        other is not null
        && location == other.location
        && Kind == other.Kind
        && string.Equals(Expected, other.Expected, StringComparison.Ordinal)
        && string.Equals(Detail, other.Detail, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as DecodeError);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(location, Kind, Expected, Detail);

    /// <summary>
    /// Returns a value indicating whether two errors have the same location, kind, expected text,
    /// and detail.
    /// </summary>
    /// <param name="left">The first error, or <see langword="null"/>.</param>
    /// <param name="right">The second error, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the errors are structurally equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(DecodeError? left, DecodeError? right)
    {
        if (right is null)
        {
            return left is null;
        }

        return right.Equals(left);
    }

    /// <summary>
    /// Returns a value indicating whether two errors differ in location, kind, expected text, or detail.
    /// </summary>
    /// <param name="left">The first error, or <see langword="null"/>.</param>
    /// <param name="right">The second error, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the errors are unequal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(DecodeError? left, DecodeError? right) => !(left == right);

    /// <summary>
    /// Returns deterministic diagnostic text with the error's path, such as
    /// <c>customer.addresses[1].postalCode: value must not be null</c>.
    /// </summary>
    /// <returns>The rendered diagnostic text.</returns>
    /// <remarks>
    /// The root path is displayed as <c>input</c>. The text is diagnostic, not a serialization or
    /// stable machine-readable contract; use <see cref="Path"/>, <see cref="Kind"/>,
    /// <see cref="Expected"/>, and <see cref="Detail"/> for programmatic handling.
    /// </remarks>
    public override string ToString()
    {
        var subject = location == DiagnosticLocation.Root ? "input" : Path;
        var message = Kind switch
        {
            DecodeErrorKind.Missing => "required value is missing",
            DecodeErrorKind.Null => Expected is null
                ? "value must not be null"
                : $"value must not be null; expected {Expected}",
            DecodeErrorKind.WrongKind => Expected is null
                ? "wrong JSON kind"
                : $"expected {Expected}",
            DecodeErrorKind.Malformed => Expected is null
                ? "malformed value"
                : $"expected {Expected}",
            DecodeErrorKind.Empty => Expected is null
                ? "value must not be empty"
                : $"expected {Expected}",
            DecodeErrorKind.InvalidValue => Detail ?? "invalid value",
            DecodeErrorKind.UnsupportedField => Detail ?? "field is not part of the allowed change",
            DecodeErrorKind.DuplicateField => Detail ?? "field is supplied more than once",
            _ => "invalid value",
        };

        if (Detail is not null
            && Kind is not DecodeErrorKind.InvalidValue
            && Kind is not DecodeErrorKind.UnsupportedField
            && Kind is not DecodeErrorKind.DuplicateField)
        {
            message += $" ({Detail})";
        }

        return $"{subject}: {message}";
    }

    internal static DecodeError At(
        DiagnosticLocation location,
        DecodeErrorKind kind,
        string? expected = null,
        string? detail = null) =>
        new(location, kind, expected, detail);

    internal DecodeError WithLocation(DiagnosticLocation prefix)
    {
        if (prefix == DiagnosticLocation.Root)
        {
            return this;
        }

        return new DecodeError(prefix.Nest(location), Kind, Expected, Detail);
    }
}
