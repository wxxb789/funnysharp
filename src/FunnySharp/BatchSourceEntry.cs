using System.Diagnostics.CodeAnalysis;

namespace FunnySharp;

/// <summary>
/// One entry in a batch source response: either a value or a typed per-key failure.
/// </summary>
/// <typeparam name="TKey">The requested key type.</typeparam>
/// <typeparam name="TValue">The successful value type.</typeparam>
/// <typeparam name="TError">The source's typed failure value.</typeparam>
/// <remarks>
/// A source omits a key to report <see cref="BatchReadErrorKind.Missing"/>. An entry created by
/// <see cref="Found"/> or <see cref="Failed"/> is valid; a default entry is
/// <see cref="BatchSourceEntryKind.Unset"/> and makes the whole response rejected as unexpected.
/// </remarks>
public readonly struct BatchSourceEntry<TKey, TValue, TError> : IEquatable<BatchSourceEntry<TKey, TValue, TError>>
{
    private readonly TKey? _key;
    private readonly TValue? _value;
    private readonly TError? _error;

    private BatchSourceEntry(TKey key, TValue? value, TError? error, BatchSourceEntryKind kind)
    {
        _key = key;
        _value = value;
        _error = error;
        Kind = kind;
    }

    /// <summary>Gets the entry state.</summary>
    public BatchSourceEntryKind Kind { get; }

    /// <summary>Gets the key this entry answers.</summary>
    /// <value>The key for a <see cref="BatchSourceEntryKind.Found"/> or
    /// <see cref="BatchSourceEntryKind.Failed"/> entry; the default key type value for an unset
    /// default entry.</value>
    public TKey Key => _key!;

    /// <summary>Creates a value entry.</summary>
    /// <param name="key">The requested key.</param>
    /// <param name="value">The value for the key.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> or
    /// <paramref name="value"/> is null.</exception>
    public static BatchSourceEntry<TKey, TValue, TError> Found(TKey key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        return new(key, value, default, BatchSourceEntryKind.Found);
    }

    /// <summary>Creates a typed per-key failure entry.</summary>
    /// <param name="key">The requested key.</param>
    /// <param name="error">The source's typed failure for the key.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
    public static BatchSourceEntry<TKey, TValue, TError> Failed(TKey key, TError error)
    {
        ArgumentNullException.ThrowIfNull(key);
        return new(key, default, error, BatchSourceEntryKind.Failed);
    }

    /// <summary>Reads the value of a found entry.</summary>
    /// <param name="value">The value when <see cref="Kind"/> is
    /// <see cref="BatchSourceEntryKind.Found"/>.</param>
    /// <returns><see langword="true"/> for a found entry; otherwise <see langword="false"/>.</returns>
    public bool TryGetValue([MaybeNullWhen(false)] out TValue value)
    {
        if (Kind == BatchSourceEntryKind.Found)
        {
            value = _value!;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>Reads the error of a failed entry.</summary>
    /// <param name="error">The error when <see cref="Kind"/> is
    /// <see cref="BatchSourceEntryKind.Failed"/>.</param>
    /// <returns><see langword="true"/> for a failed entry; otherwise <see langword="false"/>.</returns>
    public bool TryGetError([MaybeNullWhen(false)] out TError error)
    {
        if (Kind == BatchSourceEntryKind.Failed)
        {
            error = _error!;
            return true;
        }

        error = default;
        return false;
    }

    /// <inheritdoc />
    public bool Equals(BatchSourceEntry<TKey, TValue, TError> other) =>
        Kind == other.Kind
        && EqualityComparer<TKey>.Default.Equals(_key!, other._key!)
        && EqualityComparer<TValue>.Default.Equals(_value!, other._value!)
        && EqualityComparer<TError>.Default.Equals(_error!, other._error!);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is BatchSourceEntry<TKey, TValue, TError> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Kind, _key, _value, _error);

    /// <summary>Compares two entries for value equality.</summary>
    public static bool operator ==(
        BatchSourceEntry<TKey, TValue, TError> left,
        BatchSourceEntry<TKey, TValue, TError> right) => left.Equals(right);

    /// <summary>Compares two entries for value inequality.</summary>
    public static bool operator !=(
        BatchSourceEntry<TKey, TValue, TError> left,
        BatchSourceEntry<TKey, TValue, TError> right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => Kind switch
    {
        BatchSourceEntryKind.Found => $"Found({_key}, {_value})",
        BatchSourceEntryKind.Failed => $"Failed({_key}, {_error})",
        _ => "Unset",
    };
}
