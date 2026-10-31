#pragma warning disable FS0017

using System.Text.Json;

namespace FunnySharp;

using DiagnosticLocation = Location;

internal sealed class CollectionNode<T> : ContractNode<IReadOnlyList<T>>
{
    private readonly Contract<T> element;
    private readonly int minimumLength;
    private readonly int? maximumLength;

    internal CollectionNode(Contract<T> element, int minimumLength, int? maximumLength)
    {
        this.element = element;
        this.minimumLength = minimumLength;
        this.maximumLength = maximumLength;
    }

    internal override Validation<IReadOnlyList<T>, DecodeError> Validate(IReadOnlyList<T> value)
    {
        List<DecodeError>? errors = null;
        var cardinality = ContractSupport.CardinalityError(value.Count, minimumLength, maximumLength);
        if (cardinality is not null)
        {
            errors = [cardinality];
        }

        for (var index = 0; index < value.Count; index++)
        {
            if (!element.Validate(value[index]).TryGetErrors(out var elementErrors))
            {
                continue;
            }

            errors ??= [];
            ContractSupport.AppendPrefixed(
                errors,
                elementErrors,
                DiagnosticLocation.Root.At(index));
        }

        return errors is null
            ? ContractSupport.Valid(value)
            : Validation<IReadOnlyList<T>, DecodeError>.InvalidFromOwnedErrors(errors);
    }

    internal override ContractDescription Describe() =>
        ContractDescription.Collection(element.Description, minimumLength, maximumLength);

    internal override Result<Decoder<IReadOnlyList<T>>, ContractLimitation> CreateDecoder()
    {
        var elementDecoder = element.CreateDecoder();
        if (elementDecoder.TryGetError(out var limitation))
        {
            return Result<Decoder<IReadOnlyList<T>>, ContractLimitation>.Failure(
                limitation.WithPathPrefix(ContractSupport.ElementPath(string.Empty)));
        }

        elementDecoder.TryGetValue(out var decoder);
        var listDecoder = Decoder.List(decoder!);

        return Result<Decoder<IReadOnlyList<T>>, ContractLimitation>.Success(
            new Decoder<IReadOnlyList<T>>(input =>
            {
                var decoded = listDecoder.Decode(input);
                if (input.ValueKind != JsonValueKind.Array)
                {
                    return decoded;
                }

                var cardinality = ContractSupport.CardinalityError(
                    input.GetArrayLength(),
                    minimumLength,
                    maximumLength);
                if (cardinality is null)
                {
                    return decoded;
                }

                if (decoded.TryGetErrors(out var errors))
                {
                    var combined = new List<DecodeError>(errors.Count + 1)
                    {
                        cardinality,
                    };
                    combined.AddRange(errors);
                    return Validation<IReadOnlyList<T>, DecodeError>.InvalidFromOwnedErrors(combined);
                }

                return ContractSupport.Invalid<IReadOnlyList<T>>(cardinality);
            }));
    }

    internal override Result<IReadOnlyList<T>, ContractLimitation> Generate(
        GenerationContext context,
        int depth,
        string path)
    {
        if (!context.TryTakeNode())
        {
            return ContractSupport.GenerationBudgetFailure<IReadOnlyList<T>>(context, path);
        }

        var effectiveMaximum = Math.Min(
            maximumLength ?? int.MaxValue,
            context.Options.MaxCollectionLength);
        if (minimumLength > effectiveMaximum)
        {
            return ContractSupport.GenerationFailure<IReadOnlyList<T>>(
                path,
                "the declared minimum length of "
                + minimumLength
                + " exceeds the available collection budget of "
                + effectiveMaximum
                + ".");
        }

        var length = depth >= context.Options.MaxDepth
            ? minimumLength
            : context.Random.Next(
                minimumLength,
                effectiveMaximum == int.MaxValue ? int.MaxValue : effectiveMaximum + 1);

        var values = new T[length];
        for (var index = 0; index < length; index++)
        {
            var generated = element.GenerateCore(context, depth, string.Empty);
            if (generated.TryGetError(out var limitation))
            {
                return Result<IReadOnlyList<T>, ContractLimitation>.Failure(
                    limitation.WithPathPrefix(ContractSupport.IndexPath(string.Empty, index)));
            }

            generated.TryGetValue(out var elementValue);
            values[index] = elementValue!;
        }

        return Result<IReadOnlyList<T>, ContractLimitation>.Success(values);
    }

    internal override Result<IReadOnlyList<IReadOnlyList<T>>, ContractLimitation> Shrink(
        IReadOnlyList<T> value,
        ShrinkContext context,
        string path)
    {
        if (!context.TryTakeNode())
        {
            return ContractSupport.ShrinkBudgetFailure<IReadOnlyList<IReadOnlyList<T>>>(path);
        }

        var candidates = new List<IReadOnlyList<T>>();
        foreach (var length in ShrunkLengths(value.Count))
        {
            var prefix = new T[length];
            for (var index = 0; index < length; index++)
            {
                prefix[index] = value[index];
            }

            candidates.Add(prefix);
        }

        for (var index = 0; index < value.Count; index++)
        {
            var shrunk = element.ShrinkCore(value[index], context, string.Empty);
            if (shrunk.TryGetError(out var limitation))
            {
                return Result<IReadOnlyList<IReadOnlyList<T>>, ContractLimitation>.Failure(
                    limitation.WithPathPrefix(ContractSupport.IndexPath(string.Empty, index)));
            }

            shrunk.TryGetValue(out var elementCandidates);
            foreach (var candidate in elementCandidates!)
            {
                var copy = new T[value.Count];
                for (var copyIndex = 0; copyIndex < copy.Length; copyIndex++)
                {
                    copy[copyIndex] = value[copyIndex];
                }

                copy[index] = candidate;
                candidates.Add(copy);
                if (candidates.Count >= ContractSupport.MaxShrinkCandidates)
                {
                    return Result<IReadOnlyList<IReadOnlyList<T>>, ContractLimitation>.Success(candidates);
                }
            }
        }

        return Result<IReadOnlyList<IReadOnlyList<T>>, ContractLimitation>.Success(candidates);
    }

    internal override void CollectLimitations(
        List<ContractLimitation> sink,
        string path,
        ContractDerivation derivation,
        HashSet<object> visited) =>
        element.CollectLimitations(sink, ContractSupport.ElementPath(path), derivation, visited);

    private IEnumerable<int> ShrunkLengths(int count)
    {
        if (count <= minimumLength)
        {
            yield break;
        }

        if (minimumLength < count)
        {
            yield return minimumLength;
        }

        var decreased = count - 1;
        if (decreased > minimumLength)
        {
            yield return decreased;
        }

        var halved = minimumLength + ((count - minimumLength) / 2);
        if (halved > minimumLength && halved < count - 1)
        {
            yield return halved;
        }
    }
}
