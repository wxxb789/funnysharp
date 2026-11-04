using FunnySharp;

namespace FunnySharp.TestSupport.Domains;

/// <summary>
/// A hand-written tagged identifier used only as the comparison baseline for the generic
/// <see cref="DomainValue{TValue, TTag}"/> wrapper. It declares its own constructor, validation,
/// contract derivation, decoder, equality (through the record struct), and diagnostic text.
/// </summary>
internal readonly record struct HandWrittenOrderId
{
    private HandWrittenOrderId(string value) => Value = value;

    internal string Value { get; }

    private static readonly Contract<string> Underlying =
        global::FunnySharp.Contract.NonEmptyString.Ensure(
            static value => value.StartsWith("ORD-", StringComparison.Ordinal),
            "order identifier must start with ORD-");

    internal static Contract<HandWrittenOrderId> Contract { get; } = BuildContract();

    internal static Decoder<HandWrittenOrderId> Decoder { get; } = BuildDecoder();

    internal static Validation<HandWrittenOrderId, DecodeError> Create(string value)
    {
        if (value is null)
        {
            return Validation<HandWrittenOrderId, DecodeError>.Invalid(
                DecodeError.Create(DecodeErrorKind.Null));
        }

        return Underlying.Validate(value).Map(static validated => new HandWrittenOrderId(validated));
    }

    private static Contract<HandWrittenOrderId> BuildContract() =>
        Underlying.Widen(
                static value => new HandWrittenOrderId(value),
                static domain => domain.Value is { } value
                    ? Option.Some(value)
                    : Option.None<string>(),
                "not a hand-written order identifier");

    private static Decoder<HandWrittenOrderId> BuildDecoder() =>
        Contract.CreateDecoder().Match(
            static decoder => decoder,
            static limitation => throw new InvalidOperationException(limitation.ToString()));
}
