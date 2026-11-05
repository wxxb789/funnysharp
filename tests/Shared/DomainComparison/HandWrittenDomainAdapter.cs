using FunnySharp;

namespace FunnySharp.TestSupport.Domains;

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

    internal static Validation<HandWrittenOrderId, DecodeError> Create(string value) =>
        Underlying.Validate(value).Map(static validated => new HandWrittenOrderId(validated));

    private static Contract<HandWrittenOrderId> BuildContract() =>
        Underlying.Widen(
                static value => new HandWrittenOrderId(value),
                static domain => domain.Value is { } value
                    ? Option.Some(value)
                    : Option.None<string>(),
                "not a hand-written order identifier");

    private static Decoder<HandWrittenOrderId> BuildDecoder() =>
        Domains.RequireDecoder(
            Underlying.CreateDecoder().Map(
                static decoder => decoder.Map(static value => new HandWrittenOrderId(value))));
}
