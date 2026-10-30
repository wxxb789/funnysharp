using System.Text.Json;

#pragma warning disable FS0017

namespace FunnySharp;

using BclDateTime = System.DateTime;
using BclDateTimeOffset = System.DateTimeOffset;
using BclGuid = System.Guid;

/// <summary>
/// Provides immutable factories and structural combinators for <see cref="Decoder{T}"/>.
/// </summary>
/// <remarks>
/// A decoder interprets a BCL <see cref="JsonElement"/> and returns
/// <see cref="Validation{TValue, TError}"/> with <see cref="DecodeError"/> values. Factories and
/// combinators are reusable and hold no per-invocation state. The surface is deliberately a
/// callable decoder, not an inspectable schema or serializer; field names and structure are
/// carried only by the delegates the caller composes.
/// </remarks>
public static class Decoder
{
    /// <summary>
    /// Gets a decoder for a JSON string, including the empty string.
    /// </summary>
    public static Decoder<string> String { get; } = new(DecodeString);

    /// <summary>
    /// Gets a decoder for a JSON string that must not be empty.
    /// </summary>
    public static Decoder<string> NonEmptyString { get; } = new(DecodeNonEmptyString);

    /// <summary>
    /// Gets a decoder for a JSON <c>true</c> or <c>false</c> value.
    /// </summary>
    public static Decoder<bool> Boolean { get; } = new(DecodeBoolean);

    /// <summary>
    /// Gets a decoder for a JSON number that fits in a <see cref="int"/>.
    /// </summary>
    public static Decoder<int> Int32 { get; } = new(DecodeInt32);

    /// <summary>
    /// Gets a decoder for a JSON number that fits in a <see cref="long"/>.
    /// </summary>
    public static Decoder<long> Int64 { get; } = new(DecodeInt64);

    /// <summary>
    /// Gets a decoder for a JSON number that fits in a <see cref="decimal"/>.
    /// </summary>
    public static Decoder<decimal> Decimal { get; } = new(DecodeDecimal);

    /// <summary>
    /// Gets a decoder for a JSON number that fits in a <see cref="double"/>.
    /// </summary>
    public static Decoder<double> Double { get; } = new(DecodeDouble);

    /// <summary>
    /// Gets a decoder for a JSON string in one of the GUID formats accepted by <see cref="Guid.TryParse(string, out Guid)"/>.
    /// </summary>
    public static Decoder<BclGuid> Guid { get; } = new(DecodeGuid);

    /// <summary>
    /// Gets a decoder for a JSON string in an ISO 8601 date-time format accepted by
    /// <see cref="JsonElement.TryGetDateTime(out DateTime)"/>.
    /// </summary>
    public static Decoder<BclDateTime> DateTime { get; } = new(DecodeDateTime);

    /// <summary>
    /// Gets a decoder for a JSON string in an ISO 8601 date-time-offset format accepted by
    /// <see cref="JsonElement.TryGetDateTimeOffset(out DateTimeOffset)"/>.
    /// </summary>
    public static Decoder<BclDateTimeOffset> DateTimeOffset { get; } = new(DecodeDateTimeOffset);

    /// <summary>
    /// Gets a decoder that returns the raw JSON value, including an explicit null.
    /// </summary>
    /// <remarks>
    /// The returned <see cref="JsonElement"/> is a view over the caller's document. It must not be
    /// retained after that document is disposed. The default <see cref="JsonElement"/> (an
    /// <see cref="JsonValueKind.Undefined"/> value) is rejected as a wrong kind.
    /// </remarks>
    public static Decoder<JsonElement> Element { get; } = new(DecodeElement);

    /// <summary>
    /// Creates a decoder from a custom decode function.
    /// </summary>
    /// <typeparam name="T">The decoded value type.</typeparam>
    /// <param name="decode">The function invoked once for the decoder's current input.</param>
    /// <returns>A decoder that returns the function's validation.</returns>
    /// <remarks>
    /// The function receives only the current input, never an invocation context or cached state.
    /// Errors built with <see cref="DecodeError.Create"/> are relative to that input; enclosing
    /// <c>Field</c> and <c>List</c> combinators add their location prefixes. A function that throws
    /// propagates the exception unchanged. To report a nested shape, compose the structural
    /// combinators inside the function or use them around this decoder rather than assembling
    /// paths by hand.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="decode"/> is <see langword="null"/>.</exception>
    public static Decoder<T> From<T>(Func<JsonElement, Validation<T, DecodeError>> decode)
    {
        ArgumentNullException.ThrowIfNull(decode);

        return new Decoder<T>(context => decode(context.Input));
    }

    /// <summary>
    /// Creates a decoder for a required object property.
    /// </summary>
    /// <typeparam name="T">The decoded property value type.</typeparam>
    /// <param name="name">The case-sensitive property name, which must not be empty.</param>
    /// <param name="decoder">The decoder applied to the property value.</param>
    /// <returns>A decoder that reports <see cref="DecodeErrorKind.Missing"/> when the property is absent.</returns>
    /// <remarks>
    /// A present property delegates to <paramref name="decoder"/>; an explicit null therefore
    /// reaches that decoder and normally becomes <see cref="DecodeErrorKind.Null"/>. Duplicate
    /// property names resolve to the first matching property in document order. The property
    /// value's errors receive a <c>name</c> prefix only when decoding fails.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="decoder"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public static Decoder<T> Field<T>(string name, Decoder<T> decoder)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(decoder);

        return new Decoder<T>(context =>
        {
            if (!context.ObjectShapeVerified)
            {
                var objectError = ObjectKindError(context.Input);
                if (objectError is not null)
                {
                    return Validation<T, DecodeError>.Invalid(objectError);
                }
            }

            if (!TryGetFirstProperty(context.Input, name, out var value))
            {
                return Validation<T, DecodeError>.Invalid(
                    DecodeError.At(
                        Location.Root.Property(name),
                        DecodeErrorKind.Missing));
            }

            var decoded = decoder.Decode(new DecodeContext(value, objectShapeVerified: false));
            if (!decoded.TryGetErrors(out var errors))
            {
                return decoded;
            }

            return PrefixErrors(decoded, errors!, Location.Root.Property(name));
        });
    }

    /// <summary>
    /// Creates a decoder for an optional object property.
    /// </summary>
    /// <typeparam name="T">The decoded property value type.</typeparam>
    /// <param name="name">The case-sensitive property name, which must not be empty.</param>
    /// <param name="decoder">The decoder applied when the property is present.</param>
    /// <returns>
    /// A decoder that produces <c>None</c> when the property is absent and <c>Some(value)</c> when
    /// it decodes successfully.
    /// </returns>
    /// <remarks>
    /// Missing is absence. A present explicit null is not absence: it reaches
    /// <paramref name="decoder"/> and normally becomes <see cref="DecodeErrorKind.Null"/>. Use
    /// <see cref="NullableField{T}(string, Decoder{T})"/> when both missing and null deliberately
    /// mean absence. A present null is never silently folded into <c>None</c> here.
    /// Duplicate property names resolve to the first matching property in document order.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="decoder"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public static Decoder<Option<T>> OptionalField<T>(string name, Decoder<T> decoder)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(decoder);

        return new Decoder<Option<T>>(context =>
        {
            if (!context.ObjectShapeVerified)
            {
                var objectError = ObjectKindError(context.Input);
                if (objectError is not null)
                {
                    return Validation<Option<T>, DecodeError>.Invalid(objectError);
                }
            }

            if (!TryGetFirstProperty(context.Input, name, out var value))
            {
                return Validation<Option<T>, DecodeError>.Valid(Option<T>.None);
            }

            var decoded = decoder.Decode(new DecodeContext(value, objectShapeVerified: false));
            if (!decoded.TryGetErrors(out var errors))
            {
                return decoded.Map(static item => Option<T>.Some(item!));
            }

            return PrefixErrors(decoded, errors!, Location.Root.Property(name))
                .Map(static item => Option<T>.Some(item!));
        });
    }

    /// <summary>
    /// Creates a decoder for an object property where both absence and explicit null mean absence.
    /// </summary>
    /// <typeparam name="T">The decoded property value type.</typeparam>
    /// <param name="name">The case-sensitive property name, which must not be empty.</param>
    /// <param name="decoder">The decoder applied to a present, non-null property value.</param>
    /// <returns>
    /// A decoder that produces <c>None</c> when the property is absent or explicitly null, and
    /// <c>Some(value)</c> when a present non-null value decodes successfully.
    /// </returns>
    /// <remarks>
    /// This combinator deliberately collapses missing and explicit null into absence; the name
    /// states that policy. Use <see cref="OptionalField{T}(string, Decoder{T})"/> when explicit
    /// null must remain a distinguishable error. The inner decoder is not invoked for a null
    /// property. Duplicate property names resolve to the first matching property in document order.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="decoder"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public static Decoder<Option<T>> NullableField<T>(string name, Decoder<T> decoder)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(decoder);

        return new Decoder<Option<T>>(context =>
        {
            if (!context.ObjectShapeVerified)
            {
                var objectError = ObjectKindError(context.Input);
                if (objectError is not null)
                {
                    return Validation<Option<T>, DecodeError>.Invalid(objectError);
                }
            }

            if (!TryGetFirstProperty(context.Input, name, out var value)
                || value.ValueKind == JsonValueKind.Null)
            {
                return Validation<Option<T>, DecodeError>.Valid(Option<T>.None);
            }

            var decoded = decoder.Decode(new DecodeContext(value, objectShapeVerified: false));
            if (!decoded.TryGetErrors(out var errors))
            {
                return decoded.Map(static item => Option<T>.Some(item!));
            }

            return PrefixErrors(decoded, errors!, Location.Root.Property(name))
                .Map(static item => Option<T>.Some(item!));
        });
    }

    /// <summary>
    /// Creates a decoder that treats an explicit null value as absence.
    /// </summary>
    /// <typeparam name="T">The decoded value type.</typeparam>
    /// <param name="decoder">The decoder applied to a non-null value.</param>
    /// <returns>
    /// A decoder that produces <c>None</c> for an explicit null and <c>Some(value)</c> when a
    /// non-null value decodes successfully.
    /// </returns>
    /// <remarks>
    /// This is the value-level counterpart of <see cref="NullableField{T}(string, Decoder{T})"/>
    /// for array elements or already-selected values. There is no missing state at this level.
    /// The inner decoder is not invoked for null.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="decoder"/> is <see langword="null"/>.</exception>
    public static Decoder<Option<T>> OptionalValue<T>(Decoder<T> decoder)
    {
        ArgumentNullException.ThrowIfNull(decoder);

        return new Decoder<Option<T>>(context =>
            context.Input.ValueKind == JsonValueKind.Null
                ? Validation<Option<T>, DecodeError>.Valid(Option<T>.None)
                : decoder.Decode(context).Map(static item => Option<T>.Some(item!)));
    }

    /// <summary>
    /// Creates a decoder that maps every array element through <paramref name="decoder"/>.
    /// </summary>
    /// <typeparam name="T">The decoded element type.</typeparam>
    /// <param name="decoder">The decoder applied to every element in source order.</param>
    /// <returns>
    /// A decoder that produces an empty read-only list for an empty array, or all decoded elements
    /// in source order. Element errors accumulate in source order with an index prefix.
    /// </returns>
    /// <remarks>
    /// The array is enumerated once. Every reached element is decoded even when earlier elements
    /// are invalid, because <see cref="Validation{TValue, TError}"/> accumulates independent
    /// element errors. An explicit null at the array position is
    /// <see cref="DecodeErrorKind.Null"/>; a non-array is
    /// <see cref="DecodeErrorKind.WrongKind"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="decoder"/> is <see langword="null"/>.</exception>
    public static Decoder<IReadOnlyList<T>> List<T>(Decoder<T> decoder)
    {
        ArgumentNullException.ThrowIfNull(decoder);

        return new Decoder<IReadOnlyList<T>>(context =>
        {
            var input = context.Input;
            if (input.ValueKind == JsonValueKind.Null)
            {
                return Validation<IReadOnlyList<T>, DecodeError>.Invalid(
                    DecodeError.At(Location.Root, DecodeErrorKind.Null, "an array"));
            }

            if (input.ValueKind != JsonValueKind.Array)
            {
                return Validation<IReadOnlyList<T>, DecodeError>.Invalid(
                    DecodeError.At(
                        Location.Root,
                        DecodeErrorKind.WrongKind,
                        "an array",
                        "found " + input.ValueKind));
            }

            return input.EnumerateArray().Traverse((index, element) =>
            {
                var decoded = decoder.Decode(new DecodeContext(element, objectShapeVerified: false));
                if (!decoded.TryGetErrors(out var errors))
                {
                    return decoded;
                }

                return PrefixErrors(decoded, errors!, Location.Root.At(index));
            });
        });
    }

    /// <summary>
    /// Creates a decoder for an array that must contain at least one element.
    /// </summary>
    /// <typeparam name="T">The decoded element type.</typeparam>
    /// <param name="decoder">The decoder applied to every element in source order.</param>
    /// <returns>
    /// A decoder that reports <see cref="DecodeErrorKind.Empty"/> for an empty array, or all
    /// decoded elements in source order.
    /// </returns>
    /// <remarks>
    /// A non-empty array uses the same single enumeration, source ordering, and element error
    /// accumulation as <see cref="List{T}(Decoder{T})"/>. An explicit null is
    /// <see cref="DecodeErrorKind.Null"/> and a non-array is
    /// <see cref="DecodeErrorKind.WrongKind"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="decoder"/> is <see langword="null"/>.</exception>
    public static Decoder<IReadOnlyList<T>> NonEmptyList<T>(Decoder<T> decoder)
    {
        ArgumentNullException.ThrowIfNull(decoder);

        return new Decoder<IReadOnlyList<T>>(context =>
        {
            var input = context.Input;
            if (input.ValueKind == JsonValueKind.Null)
            {
                return Validation<IReadOnlyList<T>, DecodeError>.Invalid(
                    DecodeError.At(Location.Root, DecodeErrorKind.Null, "a non-empty array"));
            }

            if (input.ValueKind != JsonValueKind.Array)
            {
                return Validation<IReadOnlyList<T>, DecodeError>.Invalid(
                    DecodeError.At(
                        Location.Root,
                        DecodeErrorKind.WrongKind,
                        "a non-empty array",
                        "found " + input.ValueKind));
            }

            if (input.GetArrayLength() == 0)
            {
                return Validation<IReadOnlyList<T>, DecodeError>.Invalid(
                    DecodeError.At(
                        Location.Root,
                        DecodeErrorKind.Empty,
                        "a non-empty array"));
            }

            return List(decoder).Decode(context);
        });
    }

    /// <summary>
    /// Creates a decoder for a JSON object whose fields are decoded independently and combined.
    /// </summary>
    /// <typeparam name="TFirst">The first decoded field type.</typeparam>
    /// <typeparam name="TSecond">The second decoded field type.</typeparam>
    /// <typeparam name="TResult">The combined result type.</typeparam>
    /// <param name="first">The first independent field decoder.</param>
    /// <param name="second">The second independent field decoder.</param>
    /// <param name="combine">The function invoked only when both field decoders are valid.</param>
    /// <returns>
    /// A decoder that reports one object-kind error, or every field error in argument order, and
    /// invokes <paramref name="combine"/> only on success.
    /// </returns>
    /// <remarks>
    /// The input object kind is checked once before any field decoder runs. Field decoders are
    /// expected to be built with <see cref="Field{T}(string, Decoder{T})"/> or its optional
    /// variants; an arbitrary decoder receives the same object input. Independent field errors
    /// accumulate in argument order. The combine function is not invoked after a field failure.
    /// </remarks>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static Decoder<TResult> Object<TFirst, TSecond, TResult>(
        Decoder<TFirst> first,
        Decoder<TSecond> second,
        Func<TFirst, TSecond, TResult> combine)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(combine);

        return new Decoder<TResult>(context =>
        {
            var objectError = ObjectKindError(context.Input);
            if (objectError is not null)
            {
                return Validation<TResult, DecodeError>.Invalid(objectError);
            }

            var objectContext = new DecodeContext(context.Input, objectShapeVerified: true);
            return first.Decode(objectContext).Zip(second.Decode(objectContext), combine);
        });
    }

    /// <summary>
    /// Creates a decoder for a JSON object whose fields are decoded independently and combined.
    /// </summary>
    /// <typeparam name="TFirst">The first decoded field type.</typeparam>
    /// <typeparam name="TSecond">The second decoded field type.</typeparam>
    /// <typeparam name="TThird">The third decoded field type.</typeparam>
    /// <typeparam name="TResult">The combined result type.</typeparam>
    /// <param name="first">The first independent field decoder.</param>
    /// <param name="second">The second independent field decoder.</param>
    /// <param name="third">The third independent field decoder.</param>
    /// <param name="combine">The function invoked only when every field decoder is valid.</param>
    /// <returns>
    /// A decoder that reports one object-kind error, or every field error in argument order, and
    /// invokes <paramref name="combine"/> only on success.
    /// </returns>
    /// <remarks>
    /// Field errors accumulate in argument order. The combine function is not invoked after a
    /// field failure. See <see cref="Object{TFirst, TSecond, TResult}(Decoder{TFirst}, Decoder{TSecond}, Func{TFirst, TSecond, TResult})"/>
    /// for the object-kind and field-decoder contract.
    /// </remarks>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static Decoder<TResult> Object<TFirst, TSecond, TThird, TResult>(
        Decoder<TFirst> first,
        Decoder<TSecond> second,
        Decoder<TThird> third,
        Func<TFirst, TSecond, TThird, TResult> combine)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(third);
        ArgumentNullException.ThrowIfNull(combine);

        return new Decoder<TResult>(context =>
        {
            var objectError = ObjectKindError(context.Input);
            if (objectError is not null)
            {
                return Validation<TResult, DecodeError>.Invalid(objectError);
            }

            var objectContext = new DecodeContext(context.Input, objectShapeVerified: true);
            return first.Decode(objectContext)
                .Zip(second.Decode(objectContext), third.Decode(objectContext), combine);
        });
    }

    /// <summary>
    /// Creates a decoder for a JSON object whose fields are decoded independently and combined.
    /// </summary>
    /// <typeparam name="TFirst">The first decoded field type.</typeparam>
    /// <typeparam name="TSecond">The second decoded field type.</typeparam>
    /// <typeparam name="TThird">The third decoded field type.</typeparam>
    /// <typeparam name="TFourth">The fourth decoded field type.</typeparam>
    /// <typeparam name="TResult">The combined result type.</typeparam>
    /// <param name="first">The first independent field decoder.</param>
    /// <param name="second">The second independent field decoder.</param>
    /// <param name="third">The third independent field decoder.</param>
    /// <param name="fourth">The fourth independent field decoder.</param>
    /// <param name="combine">The function invoked only when every field decoder is valid.</param>
    /// <returns>
    /// A decoder that reports one object-kind error, or every field error in argument order, and
    /// invokes <paramref name="combine"/> only on success.
    /// </returns>
    /// <remarks>
    /// Field errors accumulate in argument order. The combine function is not invoked after a
    /// field failure. See <see cref="Object{TFirst, TSecond, TResult}(Decoder{TFirst}, Decoder{TSecond}, Func{TFirst, TSecond, TResult})"/>
    /// for the object-kind and field-decoder contract.
    /// </remarks>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static Decoder<TResult> Object<TFirst, TSecond, TThird, TFourth, TResult>(
        Decoder<TFirst> first,
        Decoder<TSecond> second,
        Decoder<TThird> third,
        Decoder<TFourth> fourth,
        Func<TFirst, TSecond, TThird, TFourth, TResult> combine)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(third);
        ArgumentNullException.ThrowIfNull(fourth);
        ArgumentNullException.ThrowIfNull(combine);

        return new Decoder<TResult>(context =>
        {
            var objectError = ObjectKindError(context.Input);
            if (objectError is not null)
            {
                return Validation<TResult, DecodeError>.Invalid(objectError);
            }

            var objectContext = new DecodeContext(context.Input, objectShapeVerified: true);
            return first.Decode(objectContext)
                .Zip(
                    second.Decode(objectContext),
                    third.Decode(objectContext),
                    fourth.Decode(objectContext),
                    combine);
        });
    }

    private static Validation<string, DecodeError> DecodeString(DecodeContext context)
    {
        var input = context.Input;
        if (input.ValueKind == JsonValueKind.String)
        {
            return Validation<string, DecodeError>.Valid(input.GetString()!);
        }

        return input.ValueKind == JsonValueKind.Null
            ? Validation<string, DecodeError>.Invalid(
                DecodeError.At(Location.Root, DecodeErrorKind.Null, "a string"))
            : Validation<string, DecodeError>.Invalid(WrongKind(input, "a string"));
    }

    private static Validation<string, DecodeError> DecodeNonEmptyString(DecodeContext context)
    {
        var input = context.Input;
        if (input.ValueKind == JsonValueKind.String)
        {
            var value = input.GetString()!;
            return value.Length == 0
                ? Validation<string, DecodeError>.Invalid(
                    DecodeError.At(
                        Location.Root,
                        DecodeErrorKind.Empty,
                        "a non-empty string"))
                : Validation<string, DecodeError>.Valid(value);
        }

        return input.ValueKind == JsonValueKind.Null
            ? Validation<string, DecodeError>.Invalid(
                DecodeError.At(Location.Root, DecodeErrorKind.Null, "a non-empty string"))
            : Validation<string, DecodeError>.Invalid(WrongKind(input, "a non-empty string"));
    }

    private static Validation<bool, DecodeError> DecodeBoolean(DecodeContext context)
    {
        var input = context.Input;
        if (input.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return Validation<bool, DecodeError>.Valid(input.GetBoolean());
        }

        return input.ValueKind == JsonValueKind.Null
            ? Validation<bool, DecodeError>.Invalid(
                DecodeError.At(Location.Root, DecodeErrorKind.Null, "a boolean"))
            : Validation<bool, DecodeError>.Invalid(WrongKind(input, "a boolean"));
    }

    private static Validation<int, DecodeError> DecodeInt32(DecodeContext context)
    {
        var input = context.Input;
        if (input.ValueKind == JsonValueKind.Number)
        {
            return input.TryGetInt32(out var value)
                ? Validation<int, DecodeError>.Valid(value)
                : Validation<int, DecodeError>.Invalid(Malformed(input, "a 32-bit integer"));
        }

        return input.ValueKind == JsonValueKind.Null
            ? Validation<int, DecodeError>.Invalid(
                DecodeError.At(Location.Root, DecodeErrorKind.Null, "a 32-bit integer"))
            : Validation<int, DecodeError>.Invalid(WrongKind(input, "a 32-bit integer"));
    }

    private static Validation<long, DecodeError> DecodeInt64(DecodeContext context)
    {
        var input = context.Input;
        if (input.ValueKind == JsonValueKind.Number)
        {
            return input.TryGetInt64(out var value)
                ? Validation<long, DecodeError>.Valid(value)
                : Validation<long, DecodeError>.Invalid(Malformed(input, "a 64-bit integer"));
        }

        return input.ValueKind == JsonValueKind.Null
            ? Validation<long, DecodeError>.Invalid(
                DecodeError.At(Location.Root, DecodeErrorKind.Null, "a 64-bit integer"))
            : Validation<long, DecodeError>.Invalid(WrongKind(input, "a 64-bit integer"));
    }

    private static Validation<decimal, DecodeError> DecodeDecimal(DecodeContext context)
    {
        var input = context.Input;
        if (input.ValueKind == JsonValueKind.Number)
        {
            return input.TryGetDecimal(out var value)
                ? Validation<decimal, DecodeError>.Valid(value)
                : Validation<decimal, DecodeError>.Invalid(Malformed(input, "a decimal number"));
        }

        return input.ValueKind == JsonValueKind.Null
            ? Validation<decimal, DecodeError>.Invalid(
                DecodeError.At(Location.Root, DecodeErrorKind.Null, "a decimal number"))
            : Validation<decimal, DecodeError>.Invalid(WrongKind(input, "a decimal number"));
    }

    private static Validation<double, DecodeError> DecodeDouble(DecodeContext context)
    {
        var input = context.Input;
        if (input.ValueKind == JsonValueKind.Number)
        {
            return input.TryGetDouble(out var value) && double.IsFinite(value)
                ? Validation<double, DecodeError>.Valid(value)
                : Validation<double, DecodeError>.Invalid(Malformed(input, "a floating-point number"));
        }

        return input.ValueKind == JsonValueKind.Null
            ? Validation<double, DecodeError>.Invalid(
                DecodeError.At(Location.Root, DecodeErrorKind.Null, "a floating-point number"))
            : Validation<double, DecodeError>.Invalid(WrongKind(input, "a floating-point number"));
    }

    private static Validation<BclGuid, DecodeError> DecodeGuid(DecodeContext context)
    {
        var input = context.Input;
        if (input.ValueKind == JsonValueKind.String)
        {
            return input.TryGetGuid(out var value)
                ? Validation<BclGuid, DecodeError>.Valid(value)
                : Validation<BclGuid, DecodeError>.Invalid(Malformed(input, "a GUID string"));
        }

        return input.ValueKind == JsonValueKind.Null
            ? Validation<BclGuid, DecodeError>.Invalid(
                DecodeError.At(Location.Root, DecodeErrorKind.Null, "a GUID string"))
            : Validation<BclGuid, DecodeError>.Invalid(WrongKind(input, "a GUID string"));
    }

    private static Validation<BclDateTime, DecodeError> DecodeDateTime(DecodeContext context)
    {
        var input = context.Input;
        if (input.ValueKind == JsonValueKind.String)
        {
            return input.TryGetDateTime(out var value)
                ? Validation<BclDateTime, DecodeError>.Valid(value)
                : Validation<BclDateTime, DecodeError>.Invalid(
                    Malformed(input, "an ISO 8601 date-time string"));
        }

        return input.ValueKind == JsonValueKind.Null
            ? Validation<BclDateTime, DecodeError>.Invalid(
                DecodeError.At(Location.Root, DecodeErrorKind.Null, "an ISO 8601 date-time string"))
            : Validation<BclDateTime, DecodeError>.Invalid(
                WrongKind(input, "an ISO 8601 date-time string"));
    }

    private static Validation<BclDateTimeOffset, DecodeError> DecodeDateTimeOffset(DecodeContext context)
    {
        var input = context.Input;
        if (input.ValueKind == JsonValueKind.String)
        {
            return input.TryGetDateTimeOffset(out var value)
                ? Validation<BclDateTimeOffset, DecodeError>.Valid(value)
                : Validation<BclDateTimeOffset, DecodeError>.Invalid(
                    Malformed(input, "an ISO 8601 date-time-offset string"));
        }

        return input.ValueKind == JsonValueKind.Null
            ? Validation<BclDateTimeOffset, DecodeError>.Invalid(
                DecodeError.At(
                    Location.Root,
                    DecodeErrorKind.Null,
                    "an ISO 8601 date-time-offset string"))
            : Validation<BclDateTimeOffset, DecodeError>.Invalid(
                WrongKind(input, "an ISO 8601 date-time-offset string"));
    }

    private static Validation<JsonElement, DecodeError> DecodeElement(DecodeContext context) =>
        context.Input.ValueKind == JsonValueKind.Undefined
            ? Validation<JsonElement, DecodeError>.Invalid(
                DecodeError.At(
                    Location.Root,
                    DecodeErrorKind.WrongKind,
                    "a JSON value",
                    "found Undefined"))
            : Validation<JsonElement, DecodeError>.Valid(context.Input);

    private static DecodeError? ObjectKindError(JsonElement input) =>
        input.ValueKind switch
        {
            JsonValueKind.Object => null,
            JsonValueKind.Null => DecodeError.At(
                Location.Root,
                DecodeErrorKind.Null,
                "an object"),
            _ => DecodeError.At(
                Location.Root,
                DecodeErrorKind.WrongKind,
                "an object",
                "found " + input.ValueKind),
        };

    private static DecodeError WrongKind(JsonElement input, string expected) =>
        DecodeError.At(
            Location.Root,
            DecodeErrorKind.WrongKind,
            expected,
            "found " + input.ValueKind);

    private static DecodeError Malformed(JsonElement input, string expected) =>
        DecodeError.At(
            Location.Root,
            DecodeErrorKind.Malformed,
            expected,
            "the value cannot be parsed as " + expected);

    private static bool TryGetFirstProperty(JsonElement input, string name, out JsonElement value)
    {
        foreach (var property in input.EnumerateObject())
        {
            if (property.NameEquals(name))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static Validation<T, DecodeError> PrefixErrors<T>(
        Validation<T, DecodeError> validation,
        IReadOnlyList<DecodeError> errors,
        Location prefix)
    {
        if (prefix == Location.Root)
        {
            return validation;
        }

        var prefixed = new DecodeError[errors.Count];
        for (var index = 0; index < prefixed.Length; index++)
        {
            prefixed[index] = errors[index].WithLocation(prefix);
        }

        return Validation<T, DecodeError>.InvalidFromOwnedErrors(prefixed);
    }
}

/// <summary>
/// Interprets an untrusted BCL <see cref="JsonElement"/> as a typed value.
/// </summary>
/// <typeparam name="T">The decoded value type.</typeparam>
/// <remarks>
/// A decoder is an immutable, reusable function. Each <see cref="Decode(JsonElement)"/> call has
/// its own local state; the decoder holds only its composed delegates. Independent structural
/// errors accumulate inside <see cref="Validation{TValue, TError}"/>. The decoder does not catch
/// exceptions, inspect cancellation tokens, retain the input document, or expose an inspectable
/// schema.
/// </remarks>
public sealed class Decoder<T>
{
    private readonly Func<DecodeContext, Validation<T, DecodeError>> decode;

    internal Decoder(Func<DecodeContext, Validation<T, DecodeError>> decode)
    {
        this.decode = decode;
    }

    /// <summary>
    /// Decodes <paramref name="input"/> with the composed decoder.
    /// </summary>
    /// <param name="input">The JSON value to decode; it may be the default <see cref="JsonElement"/>.</param>
    /// <returns>
    /// A valid validation containing the domain value, or every accumulated
    /// <see cref="DecodeError"/> in deterministic order.
    /// </returns>
    /// <remarks>
    /// The input is read during the call only. A decoder built from <see cref="Decoder.Element"/>
    /// returns a view over the input document, so that view must not outlive the document. All
    /// other built-in decoders return values independent of the document. A disposed document,
    /// a throwing caller callback, or an invalid <see cref="JsonElement"/> state propagates its
    /// exception unchanged; it is never converted into a <see cref="DecodeError"/>.
    /// </remarks>
    public Validation<T, DecodeError> Decode(JsonElement input) =>
        decode(new DecodeContext(input, objectShapeVerified: false));

    /// <summary>
    /// Decodes <paramref name="input"/> and, when decoding succeeds, awaits one asynchronous
    /// domain validation.
    /// </summary>
    /// <param name="input">The JSON value to decode.</param>
    /// <param name="validate">
    /// The asynchronous domain validation. It is invoked only after the decode succeeds and may
    /// return a normalized value or one or more domain messages.
    /// </param>
    /// <param name="cancellationToken">
    /// The token forwarded unchanged to <paramref name="validate"/>. It is not inspected when the
    /// decode fails.
    /// </param>
    /// <returns>A task completing with the decode and validation outcome.</returns>
    /// <remarks>
    /// The decode itself is synchronous because the input is an in-memory <see cref="JsonElement"/>;
    /// the only asynchronous work is <paramref name="validate"/>. It is invoked at most once and
    /// awaited once. Its messages become <see cref="DecodeErrorKind.InvalidValue"/> errors at the
    /// root of this decoder's input. An invalid decode returns the existing errors without invoking
    /// the callback. Cancellation and faults propagate with their original exception and token;
    /// they are never converted into <see cref="DecodeError"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="validate"/> is <see langword="null"/>.</exception>
    public Task<Validation<T, DecodeError>> DecodeAsync(
        JsonElement input,
        Func<T, CancellationToken, Task<Validation<T, string>>> validate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(validate);

        var decoded = Decode(input);
        return decoded.TryGetValue(out var value)
            ? ValidateAsync(value, validate, cancellationToken)
            : Task.FromResult(decoded);
    }

    /// <summary>
    /// Decodes <paramref name="input"/> and, when decoding succeeds, awaits one asynchronous
    /// domain validation returning a <see cref="ValueTask"/>.
    /// </summary>
    /// <param name="input">The JSON value to decode.</param>
    /// <param name="validate">
    /// The asynchronous domain validation. It is invoked only after the decode succeeds and may
    /// return a normalized value or one or more domain messages.
    /// </param>
    /// <param name="cancellationToken">
    /// The token forwarded unchanged to <paramref name="validate"/>. It is not inspected when the
    /// decode fails.
    /// </param>
    /// <returns>A value task completing with the decode and validation outcome.</returns>
    /// <remarks>
    /// This is the <see cref="ValueTask"/>-returning counterpart of
    /// <see cref="DecodeAsync(JsonElement, Func{T, CancellationToken, Task{Validation{T, string}}}, CancellationToken)"/>.
    /// The callback is awaited once and its messages become root
    /// <see cref="DecodeErrorKind.InvalidValue"/> errors. Cancellation and faults propagate
    /// unchanged.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="validate"/> is <see langword="null"/>.</exception>
    public ValueTask<Validation<T, DecodeError>> DecodeValueAsync(
        JsonElement input,
        Func<T, CancellationToken, ValueTask<Validation<T, string>>> validate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(validate);

        var decoded = Decode(input);
        return decoded.TryGetValue(out var value)
            ? ValidateValueAsync(value, validate, cancellationToken)
            : new ValueTask<Validation<T, DecodeError>>(decoded);
    }

    /// <summary>
    /// Transforms a successfully decoded value and preserves accumulated errors.
    /// </summary>
    /// <typeparam name="TResult">The transformed value type.</typeparam>
    /// <param name="selector">The transformation applied at most once to a valid value.</param>
    /// <returns>The transformed decoder.</returns>
    /// <remarks>
    /// The selector is not invoked for an invalid decode. Its exceptions propagate unchanged. A
    /// runtime-null reference result is preserved by <see cref="Validation{TValue, TError}"/> in
    /// the same way as its other <c>Map</c> forms; wrapping that value in <c>Option.Some</c>
    /// subsequently rejects it.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
    public Decoder<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return new Decoder<TResult>(context => decode(context).Map(selector));
    }

    /// <summary>
    /// Sequences a decoder-producing function after a successful decode.
    /// </summary>
    /// <typeparam name="TResult">The second decoder's value type.</typeparam>
    /// <param name="binder">The function that selects the next decoder from the decoded value.</param>
    /// <returns>A decoder that runs the selected decoder on the same input after success.</returns>
    /// <remarks>
    /// This is dependent composition, not independent accumulation: the binder is skipped when
    /// the first decode fails, and a second-decoder failure does not add new errors to a first
    /// success. The binder is invoked at most once and its exceptions propagate unchanged. A null
    /// decoder returned by the binder is a programming error reported as
    /// <see cref="InvalidOperationException"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="binder"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The binder returns <see langword="null"/>.</exception>
    public Decoder<TResult> Bind<TResult>(Func<T, Decoder<TResult>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        return new Decoder<TResult>(context =>
            decode(context).Match(
                value =>
                {
                    var next = binder(value);
                    if (next is null)
                    {
                        throw new InvalidOperationException("The decoder binder returned null.");
                    }

                    return next.Decode(context);
                },
                errors => Validation<TResult, DecodeError>.InvalidFromOwnedErrors(errors)));
    }

    /// <summary>
    /// Keeps a successful decode only when <paramref name="predicate"/> accepts it.
    /// </summary>
    /// <param name="predicate">The dependent domain check applied to a successful value.</param>
    /// <param name="detail">The non-empty domain message used when the predicate rejects the value.</param>
    /// <returns>A decoder that adds one <see cref="DecodeErrorKind.InvalidValue"/> error on rejection.</returns>
    /// <remarks>
    /// The predicate is skipped after an earlier failure. Its exceptions propagate unchanged. The
    /// rejection error is relative to this decoder's input, so enclosing field and list
    /// combinators add the appropriate location. This is one dependent check; use
    /// <see cref="Validate(Func{T, Validation{T, string}})"/> when several independent domain
    /// checks should accumulate.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="detail"/> is empty.</exception>
    public Decoder<T> Ensure(Func<T, bool> predicate, string detail)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentException.ThrowIfNullOrEmpty(detail);

        return new Decoder<T>(context =>
        {
            var decoded = decode(context);
            if (!decoded.TryGetValue(out var value) || predicate(value))
            {
                return decoded;
            }

            return Validation<T, DecodeError>.Invalid(
                DecodeError.At(
                    Location.Root,
                    DecodeErrorKind.InvalidValue,
                    detail: detail));
        });
    }

    /// <summary>
    /// Applies an accumulating domain validation to a successfully decoded value.
    /// </summary>
    /// <param name="validate">
    /// The validation function invoked at most once for a successful decode. It returns the
    /// domain value, possibly normalized, or one or more messages describing independent failures.
    /// </param>
    /// <returns>A decoder whose messages become <see cref="DecodeErrorKind.InvalidValue"/> errors.</returns>
    /// <remarks>
    /// The callback is skipped after an earlier failure. Every returned message becomes an
    /// <see cref="DecodeErrorKind.InvalidValue"/> at this decoder's input root, so enclosing
    /// field and list combinators add the location. The callback's own order is preserved. Its
    /// exceptions propagate unchanged.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="validate"/> is <see langword="null"/>.</exception>
    public Decoder<T> Validate(Func<T, Validation<T, string>> validate)
    {
        ArgumentNullException.ThrowIfNull(validate);

        return new Decoder<T>(context =>
        {
            var decoded = decode(context);
            if (!decoded.TryGetValue(out var value))
            {
                return decoded;
            }

            return validate(value).MapErrors(detail =>
                DecodeError.At(
                    Location.Root,
                    DecodeErrorKind.InvalidValue,
                    detail: detail));
        });
    }

    internal Validation<T, DecodeError> Decode(DecodeContext context) => decode(context);

    private static async Task<Validation<T, DecodeError>> ValidateAsync(
        T value,
        Func<T, CancellationToken, Task<Validation<T, string>>> validate,
        CancellationToken cancellationToken)
    {
        var pending = validate(value, cancellationToken);
        if (pending is null)
        {
            throw new InvalidOperationException("The asynchronous validator returned a null task.");
        }

        var validation = await pending.ConfigureAwait(false);
        return validation.MapErrors(detail =>
            DecodeError.At(
                Location.Root,
                DecodeErrorKind.InvalidValue,
                detail: detail));
    }

    private static async ValueTask<Validation<T, DecodeError>> ValidateValueAsync(
        T value,
        Func<T, CancellationToken, ValueTask<Validation<T, string>>> validate,
        CancellationToken cancellationToken)
    {
        var validation = await validate(value, cancellationToken).ConfigureAwait(false);
        return validation.MapErrors(detail =>
            DecodeError.At(
                Location.Root,
                DecodeErrorKind.InvalidValue,
                detail: detail));
    }
}

internal readonly struct DecodeContext
{
    public DecodeContext(JsonElement input, bool objectShapeVerified)
    {
        Input = input;
        ObjectShapeVerified = objectShapeVerified;
    }

    public JsonElement Input { get; }

    public bool ObjectShapeVerified { get; }
}
