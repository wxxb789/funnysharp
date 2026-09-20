namespace FunnySharp;

/// <summary>
/// Provides option bridges for parsing common types.
/// </summary>
/// <remarks>
/// The named bridges follow the C# keyword and type names of the common parseable types; the
/// generic <see cref="IParsable{T}"/> form <see cref="ParseOrNone{T}(string)"/> is the canonical
/// bridge for any parseable type. Parse failures are translated to <c>None</c> rather than
/// exceptions.
/// </remarks>
public static class ParseExtensions
{
    /// <summary>
    /// Parses a string to an <see cref="IParsable{T}"/> type and converts the result to an option.
    /// </summary>
    /// <typeparam name="T">The type to parse to.</typeparam>
    /// <param name="source">The string to parse.</param>
    /// <returns>
    /// An option containing the parsed value, or <c>None</c> when <paramref name="source"/> is not
    /// a valid value of <typeparamref name="T"/> under the current culture.
    /// </returns>
    /// <remarks>
    /// A null source is a programming error and throws eagerly; parse failures are never
    /// exceptions. The parse uses the current culture (provider: <see langword="null"/>) exactly
    /// like <c>T.Parse(source)</c>, and exceptions thrown by custom <see cref="IParsable{T}"/>
    /// implementations propagate unchanged. The bridge is iterative rather than recursive, so
    /// large inputs add no stack use.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static Option<T> ParseOrNone<T>(this string source)
        where T : IParsable<T>
    {
        ArgumentNullException.ThrowIfNull(source);

        return T.TryParse(source, null, out var value) ? Option<T>.Some(value) : Option<T>.None;
    }

    /// <summary>
    /// Parses a string to an <see cref="IParsable{T}"/> type with a format provider and converts
    /// the result to an option.
    /// </summary>
    /// <typeparam name="T">The type to parse to.</typeparam>
    /// <param name="source">The string to parse.</param>
    /// <param name="provider">The format provider forwarded unchanged to the underlying parse.</param>
    /// <returns>
    /// An option containing the parsed value, or <c>None</c> when <paramref name="source"/> is not
    /// a valid value of <typeparamref name="T"/> under <paramref name="provider"/>.
    /// </returns>
    /// <remarks>
    /// A null source or provider is a programming error and throws eagerly; parse failures are
    /// never exceptions. Exceptions thrown by custom <see cref="IParsable{T}"/> implementations
    /// propagate unchanged.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="provider"/> is <see langword="null"/>.
    /// </exception>
    public static Option<T> ParseOrNone<T>(this string source, IFormatProvider provider)
        where T : IParsable<T>
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(provider);

        return T.TryParse(source, provider, out var value) ? Option<T>.Some(value) : Option<T>.None;
    }

    /// <summary>
    /// Parses a string to a <see cref="bool"/> and converts the result to an option.
    /// </summary>
    /// <param name="source">The string to parse.</param>
    /// <returns>
    /// An option containing the parsed value, or <c>None</c> when <paramref name="source"/> is not
    /// a valid <see cref="bool"/> under the current culture.
    /// </returns>
    /// <remarks>
    /// <c>None</c> can only mean that the input is not a valid value of the type under the current
    /// culture. Use <see cref="ParseOrNone{T}(string, IFormatProvider)"/> for culture-explicit
    /// parsing.
    /// </remarks>
    public static Option<bool> ParseBoolOrNone(this string source) => source.ParseOrNone<bool>();

    /// <summary>
    /// Parses a string to a <see cref="byte"/> and converts the result to an option.
    /// </summary>
    /// <param name="source">The string to parse.</param>
    /// <returns>
    /// An option containing the parsed value, or <c>None</c> when <paramref name="source"/> is not
    /// a valid <see cref="byte"/> under the current culture.
    /// </returns>
    /// <remarks>
    /// <c>None</c> can only mean that the input is not a valid value of the type under the current
    /// culture. Use <see cref="ParseOrNone{T}(string, IFormatProvider)"/> for culture-explicit
    /// parsing.
    /// </remarks>
    public static Option<byte> ParseByteOrNone(this string source) => source.ParseOrNone<byte>();

    /// <summary>
    /// Parses a string to a <see cref="sbyte"/> and converts the result to an option.
    /// </summary>
    /// <param name="source">The string to parse.</param>
    /// <returns>
    /// An option containing the parsed value, or <c>None</c> when <paramref name="source"/> is not
    /// a valid <see cref="sbyte"/> under the current culture.
    /// </returns>
    /// <remarks>
    /// <c>None</c> can only mean that the input is not a valid value of the type under the current
    /// culture. Use <see cref="ParseOrNone{T}(string, IFormatProvider)"/> for culture-explicit
    /// parsing.
    /// </remarks>
    public static Option<sbyte> ParseSByteOrNone(this string source) => source.ParseOrNone<sbyte>();

    /// <summary>
    /// Parses a string to a <see cref="short"/> and converts the result to an option.
    /// </summary>
    /// <param name="source">The string to parse.</param>
    /// <returns>
    /// An option containing the parsed value, or <c>None</c> when <paramref name="source"/> is not
    /// a valid <see cref="short"/> under the current culture.
    /// </returns>
    /// <remarks>
    /// <c>None</c> can only mean that the input is not a valid value of the type under the current
    /// culture. Use <see cref="ParseOrNone{T}(string, IFormatProvider)"/> for culture-explicit
    /// parsing.
    /// </remarks>
    public static Option<short> ParseShortOrNone(this string source) => source.ParseOrNone<short>();

    /// <summary>
    /// Parses a string to a <see cref="ushort"/> and converts the result to an option.
    /// </summary>
    /// <param name="source">The string to parse.</param>
    /// <returns>
    /// An option containing the parsed value, or <c>None</c> when <paramref name="source"/> is not
    /// a valid <see cref="ushort"/> under the current culture.
    /// </returns>
    /// <remarks>
    /// <c>None</c> can only mean that the input is not a valid value of the type under the current
    /// culture. Use <see cref="ParseOrNone{T}(string, IFormatProvider)"/> for culture-explicit
    /// parsing.
    /// </remarks>
    public static Option<ushort> ParseUShortOrNone(this string source) => source.ParseOrNone<ushort>();

    /// <summary>
    /// Parses a string to a <see cref="int"/> and converts the result to an option.
    /// </summary>
    /// <param name="source">The string to parse.</param>
    /// <returns>
    /// An option containing the parsed value, or <c>None</c> when <paramref name="source"/> is not
    /// a valid <see cref="int"/> under the current culture.
    /// </returns>
    /// <remarks>
    /// <c>None</c> can only mean that the input is not a valid value of the type under the current
    /// culture. Use <see cref="ParseOrNone{T}(string, IFormatProvider)"/> for culture-explicit
    /// parsing.
    /// </remarks>
    public static Option<int> ParseIntOrNone(this string source) => source.ParseOrNone<int>();

    /// <summary>
    /// Parses a string to a <see cref="uint"/> and converts the result to an option.
    /// </summary>
    /// <param name="source">The string to parse.</param>
    /// <returns>
    /// An option containing the parsed value, or <c>None</c> when <paramref name="source"/> is not
    /// a valid <see cref="uint"/> under the current culture.
    /// </returns>
    /// <remarks>
    /// <c>None</c> can only mean that the input is not a valid value of the type under the current
    /// culture. Use <see cref="ParseOrNone{T}(string, IFormatProvider)"/> for culture-explicit
    /// parsing.
    /// </remarks>
    public static Option<uint> ParseUIntOrNone(this string source) => source.ParseOrNone<uint>();

    /// <summary>
    /// Parses a string to a <see cref="long"/> and converts the result to an option.
    /// </summary>
    /// <param name="source">The string to parse.</param>
    /// <returns>
    /// An option containing the parsed value, or <c>None</c> when <paramref name="source"/> is not
    /// a valid <see cref="long"/> under the current culture.
    /// </returns>
    /// <remarks>
    /// <c>None</c> can only mean that the input is not a valid value of the type under the current
    /// culture. Use <see cref="ParseOrNone{T}(string, IFormatProvider)"/> for culture-explicit
    /// parsing.
    /// </remarks>
    public static Option<long> ParseLongOrNone(this string source) => source.ParseOrNone<long>();

    /// <summary>
    /// Parses a string to a <see cref="ulong"/> and converts the result to an option.
    /// </summary>
    /// <param name="source">The string to parse.</param>
    /// <returns>
    /// An option containing the parsed value, or <c>None</c> when <paramref name="source"/> is not
    /// a valid <see cref="ulong"/> under the current culture.
    /// </returns>
    /// <remarks>
    /// <c>None</c> can only mean that the input is not a valid value of the type under the current
    /// culture. Use <see cref="ParseOrNone{T}(string, IFormatProvider)"/> for culture-explicit
    /// parsing.
    /// </remarks>
    public static Option<ulong> ParseULongOrNone(this string source) => source.ParseOrNone<ulong>();

    /// <summary>
    /// Parses a string to a <see cref="float"/> and converts the result to an option.
    /// </summary>
    /// <param name="source">The string to parse.</param>
    /// <returns>
    /// An option containing the parsed value, or <c>None</c> when <paramref name="source"/> is not
    /// a valid <see cref="float"/> under the current culture.
    /// </returns>
    /// <remarks>
    /// <c>None</c> can only mean that the input is not a valid value of the type under the current
    /// culture. Use <see cref="ParseOrNone{T}(string, IFormatProvider)"/> for culture-explicit
    /// parsing.
    /// </remarks>
    public static Option<float> ParseFloatOrNone(this string source) => source.ParseOrNone<float>();

    /// <summary>
    /// Parses a string to a <see cref="double"/> and converts the result to an option.
    /// </summary>
    /// <param name="source">The string to parse.</param>
    /// <returns>
    /// An option containing the parsed value, or <c>None</c> when <paramref name="source"/> is not
    /// a valid <see cref="double"/> under the current culture.
    /// </returns>
    /// <remarks>
    /// <c>None</c> can only mean that the input is not a valid value of the type under the current
    /// culture. Use <see cref="ParseOrNone{T}(string, IFormatProvider)"/> for culture-explicit
    /// parsing.
    /// </remarks>
    public static Option<double> ParseDoubleOrNone(this string source) => source.ParseOrNone<double>();

    /// <summary>
    /// Parses a string to a <see cref="decimal"/> and converts the result to an option.
    /// </summary>
    /// <param name="source">The string to parse.</param>
    /// <returns>
    /// An option containing the parsed value, or <c>None</c> when <paramref name="source"/> is not
    /// a valid <see cref="decimal"/> under the current culture.
    /// </returns>
    /// <remarks>
    /// <c>None</c> can only mean that the input is not a valid value of the type under the current
    /// culture. Use <see cref="ParseOrNone{T}(string, IFormatProvider)"/> for culture-explicit
    /// parsing.
    /// </remarks>
    public static Option<decimal> ParseDecimalOrNone(this string source) => source.ParseOrNone<decimal>();

    /// <summary>
    /// Parses a string to a <see cref="DateTime"/> and converts the result to an option.
    /// </summary>
    /// <param name="source">The string to parse.</param>
    /// <returns>
    /// An option containing the parsed value, or <c>None</c> when <paramref name="source"/> is not
    /// a valid <see cref="DateTime"/> under the current culture.
    /// </returns>
    /// <remarks>
    /// <c>None</c> can only mean that the input is not a valid value of the type under the current
    /// culture. Use <see cref="ParseOrNone{T}(string, IFormatProvider)"/> for culture-explicit
    /// parsing.
    /// </remarks>
    public static Option<DateTime> ParseDateTimeOrNone(this string source) => source.ParseOrNone<DateTime>();

    /// <summary>
    /// Parses a string to a <see cref="DateTimeOffset"/> and converts the result to an option.
    /// </summary>
    /// <param name="source">The string to parse.</param>
    /// <returns>
    /// An option containing the parsed value, or <c>None</c> when <paramref name="source"/> is not
    /// a valid <see cref="DateTimeOffset"/> under the current culture.
    /// </returns>
    /// <remarks>
    /// <c>None</c> can only mean that the input is not a valid value of the type under the current
    /// culture. Use <see cref="ParseOrNone{T}(string, IFormatProvider)"/> for culture-explicit
    /// parsing.
    /// </remarks>
    public static Option<DateTimeOffset> ParseDateTimeOffsetOrNone(this string source) => source.ParseOrNone<DateTimeOffset>();

    /// <summary>
    /// Parses a string to a <see cref="TimeSpan"/> and converts the result to an option.
    /// </summary>
    /// <param name="source">The string to parse.</param>
    /// <returns>
    /// An option containing the parsed value, or <c>None</c> when <paramref name="source"/> is not
    /// a valid <see cref="TimeSpan"/> under the current culture.
    /// </returns>
    /// <remarks>
    /// <c>None</c> can only mean that the input is not a valid value of the type under the current
    /// culture. Use <see cref="ParseOrNone{T}(string, IFormatProvider)"/> for culture-explicit
    /// parsing.
    /// </remarks>
    public static Option<TimeSpan> ParseTimeSpanOrNone(this string source) => source.ParseOrNone<TimeSpan>();

    /// <summary>
    /// Parses a string to a <see cref="Guid"/> and converts the result to an option.
    /// </summary>
    /// <param name="source">The string to parse.</param>
    /// <returns>
    /// An option containing the parsed value, or <c>None</c> when <paramref name="source"/> is not
    /// a valid <see cref="Guid"/> under the current culture.
    /// </returns>
    /// <remarks>
    /// <c>None</c> can only mean that the input is not a valid value of the type under the current
    /// culture. Use <see cref="ParseOrNone{T}(string, IFormatProvider)"/> for culture-explicit
    /// parsing.
    /// </remarks>
    public static Option<Guid> ParseGuidOrNone(this string source) => source.ParseOrNone<Guid>();
}
