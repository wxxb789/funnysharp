#pragma warning disable FS0017

namespace FunnySharp;

using DiagnosticLocation = Location;

internal sealed class LeafNode<T> : ContractNode<T>
{
    private readonly Func<T, Validation<T, DecodeError>> validate;
    private readonly Decoder<T> shapeDecoder;
    private readonly Func<GenerationContext, Result<T, string>> generate;
    private readonly Func<T, IEnumerable<T>> shrink;
    private readonly ContractDescription description;

    internal LeafNode(
        Func<T, Validation<T, DecodeError>> validate,
        Decoder<T> shapeDecoder,
        Func<GenerationContext, Result<T, string>> generate,
        Func<T, IEnumerable<T>> shrink,
        ContractDescription description)
    {
        this.validate = validate;
        this.shapeDecoder = shapeDecoder;
        this.generate = generate;
        this.shrink = shrink;
        this.description = description;
    }

    internal override Validation<T, DecodeError> Validate(T value) => validate(value);

    internal override ContractDescription Describe() => description;

    internal override Result<Decoder<T>, ContractLimitation> CreateDecoder() =>
        Result<Decoder<T>, ContractLimitation>.Success(
            ContractSupport.ValidatedDecoder(shapeDecoder, validate));

    internal override Result<T, ContractLimitation> Generate(
        GenerationContext context,
        int depth,
        string path)
    {
        if (!context.TryTakeNode())
        {
            return ContractSupport.GenerationBudgetFailure<T>(context, path);
        }

        var generated = generate(context);
        if (generated.TryGetError(out var reason))
        {
            return ContractSupport.GenerationFailure<T>(path, reason);
        }

        generated.TryGetValue(out var value);
        return Result<T, ContractLimitation>.Success(value!);
    }

    internal override Result<IReadOnlyList<T>, ContractLimitation> Shrink(
        T value,
        ShrinkContext context,
        string path)
    {
        if (!context.TryTakeNode())
        {
            return ContractSupport.ShrinkBudgetFailure<IReadOnlyList<T>>(path);
        }

        return ContractSupport.TakeShrinkCandidates(shrink(value));
    }

    internal override void CollectLimitations(
        List<ContractLimitation> sink,
        string path,
        ContractDerivation derivation,
        HashSet<object> visited)
    {
    }

    internal static Contract<T> Unconstrained(
        string name,
        Decoder<T> shapeDecoder,
        Func<GenerationContext, Result<T, string>> generate,
        Func<T, IEnumerable<T>> shrink) =>
        new(
            new LeafNode<T>(
                static value => ContractSupport.Valid(value),
                shapeDecoder,
                generate,
                shrink,
                ContractDescription.Scalar(name)));

    internal static Contract<T> Constrained(
        string name,
        Decoder<T> shapeDecoder,
        Func<T, Validation<T, DecodeError>> validate,
        Func<GenerationContext, Result<T, string>> generate,
        Func<T, IEnumerable<T>> shrink,
        ContractDescription? description = null) =>
        new(
            new LeafNode<T>(
                validate,
                shapeDecoder,
                generate,
                shrink,
                description ?? ContractDescription.Scalar(name)));

    internal static DecodeError InvalidValue(string detail) =>
        DecodeError.At(DiagnosticLocation.Root, DecodeErrorKind.InvalidValue, detail: detail);
}
