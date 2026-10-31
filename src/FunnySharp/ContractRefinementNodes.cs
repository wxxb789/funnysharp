#pragma warning disable FS0017

namespace FunnySharp;

using DiagnosticLocation = Location;

internal sealed class RefinedNode<T> : ContractNode<T>
{
    private const string GenerationReason =
        "the rule contains an arbitrary predicate or transformation, so no generator can be "
        + "derived; supply one with WithGenerator.";

    private const string SchemaReason =
        "the rule contains an arbitrary predicate or transformation, so no complete "
        + "schema can be derived; supply metadata with WithSchema.";

    private readonly Contract<T> source;
    private readonly Func<T, Validation<T, string>> validate;
    private readonly string detail;

    internal RefinedNode(
        Contract<T> source,
        Func<T, Validation<T, string>> validate,
        string detail)
    {
        this.source = source;
        this.validate = validate;
        this.detail = detail;
    }

    internal override Validation<T, DecodeError> Validate(T value)
    {
        var sourceValidation = source.Validate(value);
        if (!sourceValidation.IsValid)
        {
            return sourceValidation;
        }

        var refined = validate(value);
        if (refined.IsValid)
        {
            return sourceValidation;
        }

        refined.TryGetErrors(out var messages);
        return ContractSupport.InvalidFromMessages<T>(messages!);
    }

    internal override ContractDescription Describe() =>
        ContractDescription.Refined(source.Description, detail);

    internal override Result<Decoder<T>, ContractLimitation> CreateDecoder()
    {
        var created = source.CreateDecoder();
        if (created.TryGetError(out var limitation))
        {
            return Result<Decoder<T>, ContractLimitation>.Failure(limitation);
        }

        created.TryGetValue(out var decoder);
        return Result<Decoder<T>, ContractLimitation>.Success(
            ContractSupport.ValidatedDecoder(decoder!, ValidateRefinement));
    }

    private Validation<T, DecodeError> ValidateRefinement(T value)
    {
        var refined = validate(value);
        if (refined.IsValid)
        {
            return ContractSupport.Valid(value);
        }

        refined.TryGetErrors(out var messages);
        return ContractSupport.InvalidFromMessages<T>(messages!);
    }

    internal override Result<T, ContractLimitation> Generate(
        GenerationContext context,
        int depth,
        string path) =>
        ContractSupport.GenerationFailure<T>(path, GenerationReason);

    internal override Result<IReadOnlyList<T>, ContractLimitation> Shrink(
        T value,
        ShrinkContext context,
        string path) =>
        source.ShrinkCore(value, context, path);

    internal override void CollectLimitations(
        List<ContractLimitation> sink,
        string path,
        ContractDerivation derivation,
        HashSet<object> visited)
    {
        switch (derivation)
        {
            case ContractDerivation.Generation:
                sink.Add(new ContractLimitation(ContractDerivation.Generation, path, GenerationReason));
                break;
            case ContractDerivation.Schema:
                sink.Add(new ContractLimitation(ContractDerivation.Schema, path, SchemaReason));
                break;
            default:
                source.CollectLimitations(sink, path, derivation, visited);
                break;
        }
    }
}

internal sealed class OpaqueNode<T> : ContractNode<T>
{
    private const string DecoderReason =
        "the rule is an arbitrary predicate or transformation, so no decoder can be "
        + "derived; supply one with WithDecoder.";

    private const string GeneratorReason =
        "the rule is an arbitrary predicate or transformation, so no generator can be derived; "
        + "supply one with WithGenerator.";

    private const string ShrinkerReason =
        "the rule is an arbitrary predicate or transformation, so no shrinker can be "
        + "derived; supply one with WithShrinker.";

    private const string SchemaReason =
        "the rule is an arbitrary predicate or transformation, so no complete schema can be "
        + "derived; supply metadata with WithSchema.";

    private readonly string detail;
    private readonly Func<T, Validation<T, string>> validate;

    internal OpaqueNode(string detail, Func<T, Validation<T, string>> validate)
    {
        this.detail = detail;
        this.validate = validate;
    }

    internal override Validation<T, DecodeError> Validate(T value)
    {
        var validation = validate(value);
        if (validation.IsValid)
        {
            return ContractSupport.Valid(value);
        }

        validation.TryGetErrors(out var messages);
        return ContractSupport.InvalidFromMessages<T>(messages!);
    }

    internal override ContractDescription Describe() => ContractDescription.Opaque(detail);

    internal override Result<Decoder<T>, ContractLimitation> CreateDecoder() =>
        Result<Decoder<T>, ContractLimitation>.Failure(
            new ContractLimitation(ContractDerivation.Decoding, string.Empty, DecoderReason));

    internal override Result<T, ContractLimitation> Generate(
        GenerationContext context,
        int depth,
        string path) =>
        ContractSupport.GenerationFailure<T>(path, GeneratorReason);

    internal override Result<IReadOnlyList<T>, ContractLimitation> Shrink(
        T value,
        ShrinkContext context,
        string path) =>
        Result<IReadOnlyList<T>, ContractLimitation>.Failure(
            new ContractLimitation(ContractDerivation.Shrinking, path, ShrinkerReason));

    internal override void CollectLimitations(
        List<ContractLimitation> sink,
        string path,
        ContractDerivation derivation,
        HashSet<object> visited)
    {
        var reason = derivation switch
        {
            ContractDerivation.Decoding => DecoderReason,
            ContractDerivation.Generation => GeneratorReason,
            ContractDerivation.Shrinking => ShrinkerReason,
            ContractDerivation.Schema => SchemaReason,
            _ => "the rule cannot derive this interpretation.",
        };

        sink.Add(new ContractLimitation(derivation, path, reason));
    }
}

internal sealed class DeferredNode<T> : ContractNode<T>
{
    private readonly string name;
    private readonly Lazy<Contract<T>> contract;

    internal DeferredNode(string name, Func<Contract<T>> factory)
    {
        this.name = name;
        contract = new Lazy<Contract<T>>(
            () => factory() ?? throw new InvalidOperationException(
                "The deferred contract factory returned null."),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    internal override Validation<T, DecodeError> Validate(T value) =>
        contract.Value.Validate(value);

    internal override ContractDescription Describe() =>
        ContractDescription.Recursive(name);

    internal override Result<Decoder<T>, ContractLimitation> CreateDecoder() =>
        contract.Value.CreateDecoder();

    internal override Result<T, ContractLimitation> Generate(
        GenerationContext context,
        int depth,
        string path)
    {
        if (depth >= context.Options.MaxDepth)
        {
            return ContractSupport.GenerationFailure<T>(
                path,
                "recursive contract '"
                + name
                + "' reached the generation depth budget of "
                + context.Options.MaxDepth
                + "; increase MaxDepth or use a terminal structure such as a list with minimum "
                + "zero or an optional field.");
        }

        if (!context.TryTakeNode())
        {
            return ContractSupport.GenerationBudgetFailure<T>(context, path);
        }

        return contract.Value.GenerateCore(context, depth + 1, path);
    }

    internal override Result<IReadOnlyList<T>, ContractLimitation> Shrink(
        T value,
        ShrinkContext context,
        string path) =>
        contract.Value.ShrinkCore(value, context, path);

    internal override void CollectLimitations(
        List<ContractLimitation> sink,
        string path,
        ContractDerivation derivation,
        HashSet<object> visited)
    {
        if (derivation == ContractDerivation.Schema)
        {
            sink.Add(
                new ContractLimitation(
                    ContractDerivation.Schema,
                    path,
                    "recursive contract '" + name + "' is represented by name; its body is not "
                    + "expanded in schema metadata."));
            return;
        }

        contract.Value.CollectLimitations(sink, path, derivation, visited);
    }
}
