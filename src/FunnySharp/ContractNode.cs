#pragma warning disable FS0017

using System.Globalization;

namespace FunnySharp;

using DiagnosticLocation = Location;

internal abstract class ContractNode<T>
{
    internal abstract Validation<T, DecodeError> Validate(T value);

    internal abstract ContractDescription Describe();

    internal abstract Result<Decoder<T>, ContractLimitation> CreateDecoder();

    internal abstract Result<T, ContractLimitation> Generate(
        GenerationContext context,
        int depth,
        string path);

    internal abstract Result<IReadOnlyList<T>, ContractLimitation> Shrink(
        T value,
        ShrinkContext context,
        string path);

    internal abstract void CollectLimitations(
        List<ContractLimitation> sink,
        string path,
        ContractDerivation derivation,
        HashSet<object> visited);
}

internal sealed class GenerationContext
{
    private int nodes;

    internal GenerationContext(ContractGenerationOptions options)
    {
        Options = options;
        Random = new Random(options.Seed);
    }

    internal ContractGenerationOptions Options { get; }

    internal Random Random { get; }

    internal bool TryTakeNode() => ++nodes <= Options.MaxNodes;
}

internal sealed class ShrinkContext
{
    private const int MaxNodes = 1024;

    private int nodes;

    internal bool TryTakeNode() => ++nodes <= MaxNodes;
}

internal static class ContractSupport
{
    internal const int MaxShrinkCandidates = 256;

    internal static string ChildPath(string path, string segment) =>
        path.Length == 0 ? segment : path + "." + segment;

    internal static string ElementPath(string path) =>
        path.Length == 0 ? "[]" : path + "[]";

    internal static string IndexPath(string path, int index) =>
        path + "[" + index.ToString(CultureInfo.InvariantCulture) + "]";

    internal static Validation<T, DecodeError> Valid<T>(T value) =>
        Validation<T, DecodeError>.Valid(value);

    internal static Decoder<T> ValidatedDecoder<T>(
        Decoder<T> decoder,
        Func<T, Validation<T, DecodeError>> validate) =>
        new(input =>
        {
            var decoded = decoder.Decode(input);
            if (!decoded.TryGetValue(out var value))
            {
                return decoded;
            }

            var validated = validate(value);
            return validated.IsValid ? decoded : validated;
        });

    internal static Validation<T, DecodeError> Invalid<T>(DecodeError error) =>
        Validation<T, DecodeError>.Invalid(error);

    internal static Validation<T, DecodeError> InvalidFromMessages<T>(
        IReadOnlyList<string> messages)
    {
        var errors = new DecodeError[messages.Count];
        for (var index = 0; index < errors.Length; index++)
        {
            errors[index] = LeafNode<T>.InvalidValue(messages[index]);
        }

        return Validation<T, DecodeError>.InvalidFromOwnedErrors(errors);
    }

    internal static void AppendPrefixed<T>(
        List<DecodeError> sink,
        Validation<T, DecodeError> validation,
        DiagnosticLocation prefix)
    {
        if (!validation.TryGetErrors(out var errors))
        {
            return;
        }

        AppendPrefixed(sink, errors, prefix);
    }

    internal static void AppendPrefixed(
        List<DecodeError> sink,
        IReadOnlyList<DecodeError> errors,
        DiagnosticLocation prefix)
    {
        for (var index = 0; index < errors.Count; index++)
        {
            sink.Add(errors[index].WithLocation(prefix));
        }
    }

    internal static DecodeError? CardinalityError(int count, int minimum, int? maximum)
    {
        if (count < minimum)
        {
            if (count == 0)
            {
                return DecodeError.At(
                    DiagnosticLocation.Root,
                    DecodeErrorKind.Empty,
                    minimum == 1
                        ? "a non-empty array"
                        : "at least " + minimum.ToString(CultureInfo.InvariantCulture) + " items");
            }

            return DecodeError.At(
                DiagnosticLocation.Root,
                DecodeErrorKind.InvalidValue,
                detail: "at least " + minimum.ToString(CultureInfo.InvariantCulture) + " items are required");
        }

        if (maximum is { } maximumLength && count > maximumLength)
        {
            return DecodeError.At(
                DiagnosticLocation.Root,
                DecodeErrorKind.InvalidValue,
                detail: "at most " + maximumLength.ToString(CultureInfo.InvariantCulture) + " items are allowed");
        }

        return null;
    }

    internal static string FormatValue<T>(T value) =>
        value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
            : value?.ToString() ?? "null";

    internal static string FormatErrors(IReadOnlyList<DecodeError> errors) =>
        errors.Count == 0 ? "no details" : string.Join("; ", errors);

    internal static Result<IReadOnlyList<T>, ContractLimitation> TakeShrinkCandidates<T>(
        IEnumerable<T> candidates)
    {
        var taken = new List<T>(Math.Min(MaxShrinkCandidates, 16));
        foreach (var candidate in candidates)
        {
            taken.Add(candidate);
            if (taken.Count == MaxShrinkCandidates)
            {
                break;
            }
        }

        return Result<IReadOnlyList<T>, ContractLimitation>.Success(taken);
    }

    internal static Result<T, ContractLimitation> GenerationFailure<T>(
        string path,
        string reason) =>
        Result<T, ContractLimitation>.Failure(
            new ContractLimitation(ContractDerivation.Generation, path, reason));

    internal static Result<T, ContractLimitation> GenerationBudgetFailure<T>(
        GenerationContext context,
        string path) =>
        GenerationFailure<T>(
            path,
            "the generation node budget of "
            + context.Options.MaxNodes
            + " was exhausted.");

    internal static Result<T, ContractLimitation> ShrinkBudgetFailure<T>(string path) =>
        Result<T, ContractLimitation>.Failure(
            new ContractLimitation(
                ContractDerivation.Shrinking,
                path,
                "the shrink node budget was exhausted."));
}
