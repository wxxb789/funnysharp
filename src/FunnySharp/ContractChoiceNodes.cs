#pragma warning disable FS0017

using System.Text.Json;

namespace FunnySharp;

using DiagnosticLocation = Location;

internal sealed class OneOfNode<T> : ContractNode<T>
{
    private readonly ContractAlternative<T>[] alternatives;
    private readonly Func<T, int> selectValue;
    private readonly Func<JsonElement, Option<int>>? selectInput;
    private readonly string? discriminator;

    internal OneOfNode(
        ContractAlternative<T>[] alternatives,
        Func<T, int> selectValue,
        Func<JsonElement, Option<int>>? selectInput,
        string? discriminator)
    {
        this.alternatives = alternatives;
        this.selectValue = selectValue;
        this.selectInput = selectInput;
        this.discriminator = discriminator;
    }

    internal override Validation<T, DecodeError> Validate(T value)
    {
        var index = selectValue(value);
        if (index < 0 || index >= alternatives.Length)
        {
            return ContractSupport.Invalid<T>(ExpectedOneOf());
        }

        var alternative = alternatives[index];
        var validation = alternative.Contract.Validate(value);
        if (validation.IsValid)
        {
            return validation;
        }

        validation.TryGetErrors(out var errors);
        return Decoder.PrefixErrors<T>(
            errors!,
            DiagnosticLocation.Root.Property(alternative.Label));
    }

    internal override ContractDescription Describe() =>
        ContractDescription.OneOf(
            discriminator,
            alternatives.Select(static alternative => alternative.Describe()).ToArray());

    internal override Result<Decoder<T>, ContractLimitation> CreateDecoder()
    {
        if (selectInput is null)
        {
            return Result<Decoder<T>, ContractLimitation>.Failure(
                new ContractLimitation(
                    ContractDerivation.Decoding,
                    string.Empty,
                    "the one-of contract has no input selector; supply a decoder with WithDecoder "
                    + "or construct the contract with an input selector."));
        }

        var decoders = new Decoder<T>[alternatives.Length];
        for (var index = 0; index < alternatives.Length; index++)
        {
            var created = alternatives[index].Contract.CreateDecoder();
            if (created.TryGetError(out var limitation))
            {
                return Result<Decoder<T>, ContractLimitation>.Failure(
                    limitation.WithPathPrefix(alternatives[index].Label));
            }

            created.TryGetValue(out var decoder);
            decoders[index] = decoder!;
        }

        return Result<Decoder<T>, ContractLimitation>.Success(
            new Decoder<T>(input =>
            {
                var selected = selectInput(input);
                if (!selected.TryGetValue(out var index) || index < 0 || index >= decoders.Length)
                {
                    return ContractSupport.Invalid<T>(ExpectedOneOf());
                }

                var decoded = decoders[index].Decode(input);
                if (!decoded.TryGetErrors(out var errors))
                {
                    return decoded;
                }

                return Decoder.PrefixErrors<T>(
                    errors,
                    DiagnosticLocation.Root.Property(alternatives[index].Label));
            }));
    }

    internal override Result<T, ContractLimitation> Generate(
        GenerationContext context,
        int depth,
        string path)
    {
        if (!context.TryTakeNode())
        {
            return ContractSupport.GenerationBudgetFailure<T>(context, path);
        }

        var index = context.Random.Next(alternatives.Length);
        var generated = alternatives[index].Contract.GenerateCore(
            context,
            depth,
            string.Empty);
        return generated.TryGetError(out var limitation)
            ? Result<T, ContractLimitation>.Failure(
                limitation.WithPathPrefix(alternatives[index].Label))
            : generated;
    }

    internal override Result<IReadOnlyList<T>, ContractLimitation> Shrink(
        T value,
        ShrinkContext context,
        string path)
    {
        var index = selectValue(value);
        if (index < 0 || index >= alternatives.Length)
        {
            return Result<IReadOnlyList<T>, ContractLimitation>.Failure(
                new ContractLimitation(
                    ContractDerivation.Shrinking,
                    path,
                    "the value does not select a declared alternative."));
        }

        var shrunk = alternatives[index].Contract.ShrinkCore(value, context, string.Empty);
        return shrunk.TryGetError(out var limitation)
            ? Result<IReadOnlyList<T>, ContractLimitation>.Failure(
                limitation.WithPathPrefix(alternatives[index].Label))
            : shrunk;
    }

    internal override void CollectLimitations(
        List<ContractLimitation> sink,
        string path,
        ContractDerivation derivation,
        HashSet<object> visited)
    {
        if (derivation == ContractDerivation.Decoding && selectInput is null)
        {
            sink.Add(
                new ContractLimitation(
                    ContractDerivation.Decoding,
                    path,
                    "the one-of contract has no input selector; supply a decoder with WithDecoder "
                    + "or construct the contract with an input selector."));
            return;
        }

        foreach (var alternative in alternatives)
        {
            alternative.Contract.CollectLimitations(
                sink,
                ContractSupport.ChildPath(path, alternative.Label),
                derivation,
                visited);
        }
    }

    private DecodeError ExpectedOneOf() =>
        DecodeError.At(
            DiagnosticLocation.Root,
            DecodeErrorKind.InvalidValue,
            detail: "expected one of: "
            + string.Join(", ", alternatives.Select(static alternative => alternative.Label)));
}

internal sealed class WidenNode<TInner, TBase> : ContractNode<TBase>
{
    private readonly Contract<TInner> source;
    private readonly Func<TInner, TBase> widen;
    private readonly Func<TBase, Option<TInner>> narrow;
    private readonly string mismatchDetail;

    internal WidenNode(
        Contract<TInner> source,
        Func<TInner, TBase> widen,
        Func<TBase, Option<TInner>> narrow,
        string mismatchDetail)
    {
        this.source = source;
        this.widen = widen;
        this.narrow = narrow;
        this.mismatchDetail = mismatchDetail;
    }

    internal override Validation<TBase, DecodeError> Validate(TBase value)
    {
        if (value is null)
        {
            return ContractSupport.Invalid<TBase>(
                DecodeError.At(DiagnosticLocation.Root, DecodeErrorKind.Null));
        }

        var narrowed = narrow(value);
        if (!narrowed.TryGetValue(out var inner))
        {
            return ContractSupport.Invalid<TBase>(
                DecodeError.At(
                    DiagnosticLocation.Root,
                    DecodeErrorKind.InvalidValue,
                    detail: mismatchDetail));
        }

        var validation = source.Validate(inner);
        if (validation.IsValid)
        {
            return ContractSupport.Valid(value);
        }

        validation.TryGetErrors(out var errors);
        return Validation<TBase, DecodeError>.InvalidFromOwnedErrors(errors!);
    }

    internal override ContractDescription Describe() =>
        ContractDescription.Derived(source.Description, mismatchDetail);

    internal override Result<Decoder<TBase>, ContractLimitation> CreateDecoder()
    {
        var created = source.CreateDecoder();
        if (created.TryGetError(out var limitation))
        {
            return Result<Decoder<TBase>, ContractLimitation>.Failure(limitation);
        }

        created.TryGetValue(out var innerDecoder);
        return Result<Decoder<TBase>, ContractLimitation>.Success(
            ContractSupport.ValidatedDecoder(innerDecoder!.Map(widen), Validate));
    }

    internal override Result<TBase, ContractLimitation> Generate(
        GenerationContext context,
        int depth,
        string path)
    {
        var generated = source.GenerateCore(context, depth, path);
        if (generated.TryGetError(out var limitation))
        {
            return Result<TBase, ContractLimitation>.Failure(limitation);
        }

        generated.TryGetValue(out var inner);
        var value = widen(inner!);
        var validated = Validate(value);
        if (validated.IsValid)
        {
            return Result<TBase, ContractLimitation>.Success(value);
        }

        validated.TryGetErrors(out var errors);
        return ContractSupport.GenerationFailure<TBase>(
            path,
            "the derived projection produced a value that the contract rejects: "
            + ContractSupport.FormatErrors(errors!));
    }

    internal override Result<IReadOnlyList<TBase>, ContractLimitation> Shrink(
        TBase value,
        ShrinkContext context,
        string path)
    {
        var narrowed = narrow(value);
        if (!narrowed.TryGetValue(out var inner))
        {
            return Result<IReadOnlyList<TBase>, ContractLimitation>.Failure(
                new ContractLimitation(ContractDerivation.Shrinking, path, mismatchDetail));
        }

        var shrunk = source.ShrinkCore(inner, context, path);
        if (shrunk.TryGetError(out var limitation))
        {
            return Result<IReadOnlyList<TBase>, ContractLimitation>.Failure(limitation);
        }

        shrunk.TryGetValue(out var candidates);
        var results = new List<TBase>(candidates!.Count);
        foreach (var candidate in candidates)
        {
            results.Add(widen(candidate));
        }

        return Result<IReadOnlyList<TBase>, ContractLimitation>.Success(results);
    }

    internal override void CollectLimitations(
        List<ContractLimitation> sink,
        string path,
        ContractDerivation derivation,
        HashSet<object> visited) =>
        source.CollectLimitations(sink, path, derivation, visited);
}
