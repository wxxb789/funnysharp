using System.Diagnostics.CodeAnalysis;

namespace FunnySharp;

/// <summary>
/// Describes one failed batched read without converting cancellation or programming exceptions
/// into data failures.
/// </summary>
/// <typeparam name="TError">The source's typed failure value.</typeparam>
/// <remarks>
/// <see cref="BatchReadErrorKind.Missing"/> and <see cref="BatchReadErrorKind.UnexpectedResponse"/>
/// carry no source error. <see cref="BatchReadErrorKind.KeyFailure"/> carries the source's failure
/// for one key; <see cref="BatchReadErrorKind.BatchFailure"/> carries the failure the source
/// returned for the whole batch. A source exception remains an exception and is never represented
/// by this value.
/// </remarks>
public readonly struct BatchReadError<TError> : IEquatable<BatchReadError<TError>>
{
    private readonly TError? _error;
    private readonly string? _detail;

    private BatchReadError(BatchReadErrorKind kind, TError? error, string? detail)
    {
        Kind = kind;
        _error = error;
        _detail = detail;
    }

    /// <summary>Gets the failure category.</summary>
    public BatchReadErrorKind Kind { get; }

    /// <summary>Gets a deterministic explanation for an unexpected source response.</summary>
    /// <value>An empty string unless <see cref="Kind"/> is
    /// <see cref="BatchReadErrorKind.UnexpectedResponse"/>.</value>
    public string Detail => _detail ?? string.Empty;

    /// <summary>Creates the result of a source that omitted the key.</summary>
    public static BatchReadError<TError> Missing() =>
        new(BatchReadErrorKind.Missing, default, null);

    /// <summary>Creates the result of a source that failed one key.</summary>
    /// <param name="error">The source's typed failure for the key.</param>
    public static BatchReadError<TError> KeyFailure(TError error) =>
        new(BatchReadErrorKind.KeyFailure, error, null);

    /// <summary>Creates the result of a source that failed the whole batch.</summary>
    /// <param name="error">The source's typed failure for the batch.</param>
    public static BatchReadError<TError> BatchFailure(TError error) =>
        new(BatchReadErrorKind.BatchFailure, error, null);

    /// <summary>Creates the result of a source response that violates the batch contract.</summary>
    /// <param name="detail">A deterministic explanation of the rejected response.</param>
    /// <exception cref="ArgumentException"><paramref name="detail"/> is null, empty, or whitespace.</exception>
    public static BatchReadError<TError> UnexpectedResponse(string detail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(detail);
        return new(BatchReadErrorKind.UnexpectedResponse, default, detail);
    }

    /// <summary>Reads the source error carried by a key or batch failure.</summary>
    /// <param name="error">The source error when this is a key or batch failure.</param>
    /// <returns><see langword="true"/> for <see cref="BatchReadErrorKind.KeyFailure"/> and
    /// <see cref="BatchReadErrorKind.BatchFailure"/>; otherwise <see langword="false"/>.</returns>
    public bool TryGetSourceError([MaybeNullWhen(false)] out TError error)
    {
        if (Kind is BatchReadErrorKind.KeyFailure or BatchReadErrorKind.BatchFailure)
        {
            error = _error!;
            return true;
        }

        error = default;
        return false;
    }

    /// <inheritdoc />
    public bool Equals(BatchReadError<TError> other) =>
        Kind == other.Kind
        && EqualityComparer<TError>.Default.Equals(_error!, other._error!)
        && string.Equals(_detail, other._detail, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is BatchReadError<TError> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Kind, _error, Detail);

    /// <summary>Compares two errors for value equality.</summary>
    public static bool operator ==(BatchReadError<TError> left, BatchReadError<TError> right) =>
        left.Equals(right);

    /// <summary>Compares two errors for value inequality.</summary>
    public static bool operator !=(BatchReadError<TError> left, BatchReadError<TError> right) =>
        !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => Kind switch
    {
        BatchReadErrorKind.Missing => "Missing",
        BatchReadErrorKind.KeyFailure => $"KeyFailure({_error})",
        BatchReadErrorKind.BatchFailure => $"BatchFailure({_error})",
        _ => $"UnexpectedResponse({Detail})",
    };
}
