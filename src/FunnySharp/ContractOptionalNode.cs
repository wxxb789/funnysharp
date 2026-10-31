#pragma warning disable FS0017

namespace FunnySharp;

internal sealed class OptionalNode<T> : ContractNode<Option<T>>
{
    private readonly Contract<T> inner;

    internal OptionalNode(Contract<T> inner) => this.inner = inner;

    internal override Validation<Option<T>, DecodeError> Validate(Option<T> value)
    {
        if (!value.TryGetValue(out var present))
        {
            return ContractSupport.Valid(value);
        }

        return inner.Validate(present).Map(_ => value);
    }

    internal override ContractDescription Describe() =>
        ContractDescription.Optional(inner.Description);

    internal override Result<Decoder<Option<T>>, ContractLimitation> CreateDecoder() =>
        inner.CreateDecoder().Map(
            static decoder => (Decoder<Option<T>>)Decoder.OptionalValue(decoder));

    internal override Result<Option<T>, ContractLimitation> Generate(
        GenerationContext context,
        int depth,
        string path)
    {
        if (!context.TryTakeNode())
        {
            return ContractSupport.GenerationBudgetFailure<Option<T>>(context, path);
        }

        if (depth >= context.Options.MaxDepth || context.Random.Next(2) == 0)
        {
            return Result<Option<T>, ContractLimitation>.Success(Option<T>.None);
        }

        return inner.GenerateCore(context, depth, string.Empty)
            .Map(static item => Option<T>.Some(item!));
    }

    internal override Result<IReadOnlyList<Option<T>>, ContractLimitation> Shrink(
        Option<T> value,
        ShrinkContext context,
        string path)
    {
        if (!context.TryTakeNode())
        {
            return ContractSupport.ShrinkBudgetFailure<IReadOnlyList<Option<T>>>(path);
        }

        if (!value.TryGetValue(out var present))
        {
            return Result<IReadOnlyList<Option<T>>, ContractLimitation>.Success([]);
        }

        var shrunk = inner.ShrinkCore(present, context, string.Empty);
        if (shrunk.TryGetError(out var limitation))
        {
            return Result<IReadOnlyList<Option<T>>, ContractLimitation>.Failure(limitation);
        }

        shrunk.TryGetValue(out var candidates);
        var results = new List<Option<T>>(candidates!.Count + 1)
        {
            Option<T>.None,
        };
        foreach (var candidate in candidates)
        {
            results.Add(Option<T>.Some(candidate!));
        }

        return Result<IReadOnlyList<Option<T>>, ContractLimitation>.Success(results);
    }

    internal override void CollectLimitations(
        List<ContractLimitation> sink,
        string path,
        ContractDerivation derivation,
        HashSet<object> visited) =>
        inner.CollectLimitations(sink, path, derivation, visited);
}
