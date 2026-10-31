#pragma warning disable FS0017

namespace FunnySharp;

using DiagnosticLocation = Location;

internal sealed class ProductNode1<TParent, T1> : ContractNode<TParent>
{
    private readonly ContractField<TParent, T1> first;
    private readonly Func<T1, TParent> combine;

    internal ProductNode1(ContractField<TParent, T1> first, Func<T1, TParent> combine)
    {
        this.first = first;
        this.combine = combine;
    }

    internal override Validation<TParent, DecodeError> Validate(TParent value)
    {
        if (value is null)
        {
            return ContractSupport.Invalid<TParent>(
                DecodeError.At(DiagnosticLocation.Root, DecodeErrorKind.Null));
        }

        var firstValidation = first.Contract.Validate(first.Get(value));
        if (firstValidation.IsValid)
        {
            return ContractSupport.Valid(value);
        }

        var errors = new List<DecodeError>();
        ContractSupport.AppendPrefixed(
            errors,
            firstValidation,
            DiagnosticLocation.Root.Property(first.Name));
        return Validation<TParent, DecodeError>.InvalidFromOwnedErrors(errors);
    }

    internal override ContractDescription Describe() =>
        ContractDescription.Object([first.Describe()]);

    internal override Result<Decoder<TParent>, ContractLimitation> CreateDecoder()
    {
        var fieldDecoder = first.CreateDecoder();
        if (fieldDecoder.TryGetError(out var limitation))
        {
            return Result<Decoder<TParent>, ContractLimitation>.Failure(
                limitation.WithPathPrefix(first.Name));
        }

        fieldDecoder.TryGetValue(out var decoder);
        return Result<Decoder<TParent>, ContractLimitation>.Success(
            ContractSupport.ValidatedDecoder(decoder!.Map(combine), Validate));
    }

    internal override Result<TParent, ContractLimitation> Generate(
        GenerationContext context,
        int depth,
        string path)
    {
        var generated = first.Contract.GenerateCore(context, depth, string.Empty);
        if (generated.TryGetError(out var limitation))
        {
            return Result<TParent, ContractLimitation>.Failure(
                limitation.WithPathPrefix(first.Name));
        }

        generated.TryGetValue(out var value);
        var result = combine(value!);
        return result is null
            ? ContractSupport.GenerationFailure<TParent>(
                path,
                "the object combine function returned null.")
            : Result<TParent, ContractLimitation>.Success(result);
    }

    internal override Result<IReadOnlyList<TParent>, ContractLimitation> Shrink(
        TParent value,
        ShrinkContext context,
        string path)
    {
        var shrunk = first.Contract.ShrinkCore(first.Get(value), context, string.Empty);
        if (shrunk.TryGetError(out var limitation))
        {
            return Result<IReadOnlyList<TParent>, ContractLimitation>.Failure(
                limitation.WithPathPrefix(first.Name));
        }

        shrunk.TryGetValue(out var candidates);
        var results = new List<TParent>(candidates!.Count);
        foreach (var candidate in candidates)
        {
            results.Add(combine(candidate));
        }

        return Result<IReadOnlyList<TParent>, ContractLimitation>.Success(results);
    }

    internal override void CollectLimitations(
        List<ContractLimitation> sink,
        string path,
        ContractDerivation derivation,
        HashSet<object> visited) =>
        first.Contract.CollectLimitations(
            sink,
            ContractSupport.ChildPath(path, first.Name),
            derivation,
            visited);
}

internal sealed class ProductNode2<TParent, T1, T2> : ContractNode<TParent>
{
    private readonly ContractField<TParent, T1> first;
    private readonly ContractField<TParent, T2> second;
    private readonly Func<T1, T2, TParent> combine;

    internal ProductNode2(
        ContractField<TParent, T1> first,
        ContractField<TParent, T2> second,
        Func<T1, T2, TParent> combine)
    {
        this.first = first;
        this.second = second;
        this.combine = combine;
    }

    internal override Validation<TParent, DecodeError> Validate(TParent value)
    {
        if (value is null)
        {
            return ContractSupport.Invalid<TParent>(
                DecodeError.At(DiagnosticLocation.Root, DecodeErrorKind.Null));
        }

        var firstValidation = first.Contract.Validate(first.Get(value));
        var secondValidation = second.Contract.Validate(second.Get(value));
        if (firstValidation.IsValid && secondValidation.IsValid)
        {
            return ContractSupport.Valid(value);
        }

        var errors = new List<DecodeError>();
        ContractSupport.AppendPrefixed(
            errors,
            firstValidation,
            DiagnosticLocation.Root.Property(first.Name));
        ContractSupport.AppendPrefixed(
            errors,
            secondValidation,
            DiagnosticLocation.Root.Property(second.Name));
        return Validation<TParent, DecodeError>.InvalidFromOwnedErrors(errors);
    }

    internal override ContractDescription Describe() =>
        ContractDescription.Object([first.Describe(), second.Describe()]);

    internal override Result<Decoder<TParent>, ContractLimitation> CreateDecoder()
    {
        var firstDecoder = first.CreateDecoder();
        if (firstDecoder.TryGetError(out var firstLimitation))
        {
            return Result<Decoder<TParent>, ContractLimitation>.Failure(
                firstLimitation.WithPathPrefix(first.Name));
        }

        var secondDecoder = second.CreateDecoder();
        if (secondDecoder.TryGetError(out var secondLimitation))
        {
            return Result<Decoder<TParent>, ContractLimitation>.Failure(
                secondLimitation.WithPathPrefix(second.Name));
        }

        firstDecoder.TryGetValue(out var firstValue);
        secondDecoder.TryGetValue(out var secondValue);
        return Result<Decoder<TParent>, ContractLimitation>.Success(
            ContractSupport.ValidatedDecoder(
                Decoder.Object(firstValue!, secondValue!, combine),
                Validate));
    }

    internal override Result<TParent, ContractLimitation> Generate(
        GenerationContext context,
        int depth,
        string path)
    {
        var firstGenerated = first.Contract.GenerateCore(context, depth, string.Empty);
        if (firstGenerated.TryGetError(out var firstLimitation))
        {
            return Result<TParent, ContractLimitation>.Failure(
                firstLimitation.WithPathPrefix(first.Name));
        }

        var secondGenerated = second.Contract.GenerateCore(context, depth, string.Empty);
        if (secondGenerated.TryGetError(out var secondLimitation))
        {
            return Result<TParent, ContractLimitation>.Failure(
                secondLimitation.WithPathPrefix(second.Name));
        }

        firstGenerated.TryGetValue(out var firstValue);
        secondGenerated.TryGetValue(out var secondValue);
        var result = combine(firstValue!, secondValue!);
        return result is null
            ? ContractSupport.GenerationFailure<TParent>(
                path,
                "the object combine function returned null.")
            : Result<TParent, ContractLimitation>.Success(result);
    }

    internal override Result<IReadOnlyList<TParent>, ContractLimitation> Shrink(
        TParent value,
        ShrinkContext context,
        string path)
    {
        var currentFirst = first.Get(value);
        var currentSecond = second.Get(value);
        var firstShrunk = first.Contract.ShrinkCore(currentFirst, context, string.Empty);
        if (firstShrunk.TryGetError(out var firstLimitation))
        {
            return Result<IReadOnlyList<TParent>, ContractLimitation>.Failure(
                firstLimitation.WithPathPrefix(first.Name));
        }

        var secondShrunk = second.Contract.ShrinkCore(currentSecond, context, string.Empty);
        if (secondShrunk.TryGetError(out var secondLimitation))
        {
            return Result<IReadOnlyList<TParent>, ContractLimitation>.Failure(
                secondLimitation.WithPathPrefix(second.Name));
        }

        var results = new List<TParent>();
        firstShrunk.TryGetValue(out var firstCandidates);
        foreach (var candidate in firstCandidates!)
        {
            results.Add(combine(candidate, currentSecond));
        }

        secondShrunk.TryGetValue(out var secondCandidates);
        foreach (var candidate in secondCandidates!)
        {
            results.Add(combine(currentFirst, candidate));
        }

        return Result<IReadOnlyList<TParent>, ContractLimitation>.Success(results);
    }

    internal override void CollectLimitations(
        List<ContractLimitation> sink,
        string path,
        ContractDerivation derivation,
        HashSet<object> visited)
    {
        first.Contract.CollectLimitations(
            sink,
            ContractSupport.ChildPath(path, first.Name),
            derivation,
            visited);
        second.Contract.CollectLimitations(
            sink,
            ContractSupport.ChildPath(path, second.Name),
            derivation,
            visited);
    }
}

internal sealed class ProductNode3<TParent, T1, T2, T3> : ContractNode<TParent>
{
    private readonly ContractField<TParent, T1> first;
    private readonly ContractField<TParent, T2> second;
    private readonly ContractField<TParent, T3> third;
    private readonly Func<T1, T2, T3, TParent> combine;

    internal ProductNode3(
        ContractField<TParent, T1> first,
        ContractField<TParent, T2> second,
        ContractField<TParent, T3> third,
        Func<T1, T2, T3, TParent> combine)
    {
        this.first = first;
        this.second = second;
        this.third = third;
        this.combine = combine;
    }

    internal override Validation<TParent, DecodeError> Validate(TParent value)
    {
        if (value is null)
        {
            return ContractSupport.Invalid<TParent>(
                DecodeError.At(DiagnosticLocation.Root, DecodeErrorKind.Null));
        }

        var firstValidation = first.Contract.Validate(first.Get(value));
        var secondValidation = second.Contract.Validate(second.Get(value));
        var thirdValidation = third.Contract.Validate(third.Get(value));
        if (firstValidation.IsValid
            && secondValidation.IsValid
            && thirdValidation.IsValid)
        {
            return ContractSupport.Valid(value);
        }

        var errors = new List<DecodeError>();
        ContractSupport.AppendPrefixed(
            errors,
            firstValidation,
            DiagnosticLocation.Root.Property(first.Name));
        ContractSupport.AppendPrefixed(
            errors,
            secondValidation,
            DiagnosticLocation.Root.Property(second.Name));
        ContractSupport.AppendPrefixed(
            errors,
            thirdValidation,
            DiagnosticLocation.Root.Property(third.Name));
        return Validation<TParent, DecodeError>.InvalidFromOwnedErrors(errors);
    }

    internal override ContractDescription Describe() =>
        ContractDescription.Object([first.Describe(), second.Describe(), third.Describe()]);

    internal override Result<Decoder<TParent>, ContractLimitation> CreateDecoder()
    {
        var firstDecoder = first.CreateDecoder();
        if (firstDecoder.TryGetError(out var firstLimitation))
        {
            return Result<Decoder<TParent>, ContractLimitation>.Failure(
                firstLimitation.WithPathPrefix(first.Name));
        }

        var secondDecoder = second.CreateDecoder();
        if (secondDecoder.TryGetError(out var secondLimitation))
        {
            return Result<Decoder<TParent>, ContractLimitation>.Failure(
                secondLimitation.WithPathPrefix(second.Name));
        }

        var thirdDecoder = third.CreateDecoder();
        if (thirdDecoder.TryGetError(out var thirdLimitation))
        {
            return Result<Decoder<TParent>, ContractLimitation>.Failure(
                thirdLimitation.WithPathPrefix(third.Name));
        }

        firstDecoder.TryGetValue(out var firstValue);
        secondDecoder.TryGetValue(out var secondValue);
        thirdDecoder.TryGetValue(out var thirdValue);
        return Result<Decoder<TParent>, ContractLimitation>.Success(
            ContractSupport.ValidatedDecoder(
                Decoder.Object(firstValue!, secondValue!, thirdValue!, combine),
                Validate));
    }

    internal override Result<TParent, ContractLimitation> Generate(
        GenerationContext context,
        int depth,
        string path)
    {
        var firstGenerated = first.Contract.GenerateCore(context, depth, string.Empty);
        if (firstGenerated.TryGetError(out var firstLimitation))
        {
            return Result<TParent, ContractLimitation>.Failure(
                firstLimitation.WithPathPrefix(first.Name));
        }

        var secondGenerated = second.Contract.GenerateCore(context, depth, string.Empty);
        if (secondGenerated.TryGetError(out var secondLimitation))
        {
            return Result<TParent, ContractLimitation>.Failure(
                secondLimitation.WithPathPrefix(second.Name));
        }

        var thirdGenerated = third.Contract.GenerateCore(context, depth, string.Empty);
        if (thirdGenerated.TryGetError(out var thirdLimitation))
        {
            return Result<TParent, ContractLimitation>.Failure(
                thirdLimitation.WithPathPrefix(third.Name));
        }

        firstGenerated.TryGetValue(out var firstValue);
        secondGenerated.TryGetValue(out var secondValue);
        thirdGenerated.TryGetValue(out var thirdValue);
        var result = combine(firstValue!, secondValue!, thirdValue!);
        return result is null
            ? ContractSupport.GenerationFailure<TParent>(
                path,
                "the object combine function returned null.")
            : Result<TParent, ContractLimitation>.Success(result);
    }

    internal override Result<IReadOnlyList<TParent>, ContractLimitation> Shrink(
        TParent value,
        ShrinkContext context,
        string path)
    {
        var currentFirst = first.Get(value);
        var currentSecond = second.Get(value);
        var currentThird = third.Get(value);
        var firstShrunk = first.Contract.ShrinkCore(currentFirst, context, string.Empty);
        if (firstShrunk.TryGetError(out var firstLimitation))
        {
            return Result<IReadOnlyList<TParent>, ContractLimitation>.Failure(
                firstLimitation.WithPathPrefix(first.Name));
        }

        var secondShrunk = second.Contract.ShrinkCore(currentSecond, context, string.Empty);
        if (secondShrunk.TryGetError(out var secondLimitation))
        {
            return Result<IReadOnlyList<TParent>, ContractLimitation>.Failure(
                secondLimitation.WithPathPrefix(second.Name));
        }

        var thirdShrunk = third.Contract.ShrinkCore(currentThird, context, string.Empty);
        if (thirdShrunk.TryGetError(out var thirdLimitation))
        {
            return Result<IReadOnlyList<TParent>, ContractLimitation>.Failure(
                thirdLimitation.WithPathPrefix(third.Name));
        }

        var results = new List<TParent>();
        firstShrunk.TryGetValue(out var firstCandidates);
        foreach (var candidate in firstCandidates!)
        {
            results.Add(combine(candidate, currentSecond, currentThird));
        }

        secondShrunk.TryGetValue(out var secondCandidates);
        foreach (var candidate in secondCandidates!)
        {
            results.Add(combine(currentFirst, candidate, currentThird));
        }

        thirdShrunk.TryGetValue(out var thirdCandidates);
        foreach (var candidate in thirdCandidates!)
        {
            results.Add(combine(currentFirst, currentSecond, candidate));
        }

        return Result<IReadOnlyList<TParent>, ContractLimitation>.Success(results);
    }

    internal override void CollectLimitations(
        List<ContractLimitation> sink,
        string path,
        ContractDerivation derivation,
        HashSet<object> visited)
    {
        first.Contract.CollectLimitations(
            sink,
            ContractSupport.ChildPath(path, first.Name),
            derivation,
            visited);
        second.Contract.CollectLimitations(
            sink,
            ContractSupport.ChildPath(path, second.Name),
            derivation,
            visited);
        third.Contract.CollectLimitations(
            sink,
            ContractSupport.ChildPath(path, third.Name),
            derivation,
            visited);
    }
}

internal sealed class ProductNode4<TParent, T1, T2, T3, T4> : ContractNode<TParent>
{
    private readonly ContractField<TParent, T1> first;
    private readonly ContractField<TParent, T2> second;
    private readonly ContractField<TParent, T3> third;
    private readonly ContractField<TParent, T4> fourth;
    private readonly Func<T1, T2, T3, T4, TParent> combine;

    internal ProductNode4(
        ContractField<TParent, T1> first,
        ContractField<TParent, T2> second,
        ContractField<TParent, T3> third,
        ContractField<TParent, T4> fourth,
        Func<T1, T2, T3, T4, TParent> combine)
    {
        this.first = first;
        this.second = second;
        this.third = third;
        this.fourth = fourth;
        this.combine = combine;
    }

    internal override Validation<TParent, DecodeError> Validate(TParent value)
    {
        if (value is null)
        {
            return ContractSupport.Invalid<TParent>(
                DecodeError.At(DiagnosticLocation.Root, DecodeErrorKind.Null));
        }

        var firstValidation = first.Contract.Validate(first.Get(value));
        var secondValidation = second.Contract.Validate(second.Get(value));
        var thirdValidation = third.Contract.Validate(third.Get(value));
        var fourthValidation = fourth.Contract.Validate(fourth.Get(value));
        if (firstValidation.IsValid
            && secondValidation.IsValid
            && thirdValidation.IsValid
            && fourthValidation.IsValid)
        {
            return ContractSupport.Valid(value);
        }

        var errors = new List<DecodeError>();
        ContractSupport.AppendPrefixed(
            errors,
            firstValidation,
            DiagnosticLocation.Root.Property(first.Name));
        ContractSupport.AppendPrefixed(
            errors,
            secondValidation,
            DiagnosticLocation.Root.Property(second.Name));
        ContractSupport.AppendPrefixed(
            errors,
            thirdValidation,
            DiagnosticLocation.Root.Property(third.Name));
        ContractSupport.AppendPrefixed(
            errors,
            fourthValidation,
            DiagnosticLocation.Root.Property(fourth.Name));
        return Validation<TParent, DecodeError>.InvalidFromOwnedErrors(errors);
    }

    internal override ContractDescription Describe() =>
        ContractDescription.Object(
            [first.Describe(), second.Describe(), third.Describe(), fourth.Describe()]);

    internal override Result<Decoder<TParent>, ContractLimitation> CreateDecoder()
    {
        var firstDecoder = first.CreateDecoder();
        if (firstDecoder.TryGetError(out var firstLimitation))
        {
            return Result<Decoder<TParent>, ContractLimitation>.Failure(
                firstLimitation.WithPathPrefix(first.Name));
        }

        var secondDecoder = second.CreateDecoder();
        if (secondDecoder.TryGetError(out var secondLimitation))
        {
            return Result<Decoder<TParent>, ContractLimitation>.Failure(
                secondLimitation.WithPathPrefix(second.Name));
        }

        var thirdDecoder = third.CreateDecoder();
        if (thirdDecoder.TryGetError(out var thirdLimitation))
        {
            return Result<Decoder<TParent>, ContractLimitation>.Failure(
                thirdLimitation.WithPathPrefix(third.Name));
        }

        var fourthDecoder = fourth.CreateDecoder();
        if (fourthDecoder.TryGetError(out var fourthLimitation))
        {
            return Result<Decoder<TParent>, ContractLimitation>.Failure(
                fourthLimitation.WithPathPrefix(fourth.Name));
        }

        firstDecoder.TryGetValue(out var firstValue);
        secondDecoder.TryGetValue(out var secondValue);
        thirdDecoder.TryGetValue(out var thirdValue);
        fourthDecoder.TryGetValue(out var fourthValue);
        return Result<Decoder<TParent>, ContractLimitation>.Success(
            ContractSupport.ValidatedDecoder(
                Decoder.Object(firstValue!, secondValue!, thirdValue!, fourthValue!, combine),
                Validate));
    }

    internal override Result<TParent, ContractLimitation> Generate(
        GenerationContext context,
        int depth,
        string path)
    {
        var firstGenerated = first.Contract.GenerateCore(context, depth, string.Empty);
        if (firstGenerated.TryGetError(out var firstLimitation))
        {
            return Result<TParent, ContractLimitation>.Failure(
                firstLimitation.WithPathPrefix(first.Name));
        }

        var secondGenerated = second.Contract.GenerateCore(context, depth, string.Empty);
        if (secondGenerated.TryGetError(out var secondLimitation))
        {
            return Result<TParent, ContractLimitation>.Failure(
                secondLimitation.WithPathPrefix(second.Name));
        }

        var thirdGenerated = third.Contract.GenerateCore(context, depth, string.Empty);
        if (thirdGenerated.TryGetError(out var thirdLimitation))
        {
            return Result<TParent, ContractLimitation>.Failure(
                thirdLimitation.WithPathPrefix(third.Name));
        }

        var fourthGenerated = fourth.Contract.GenerateCore(context, depth, string.Empty);
        if (fourthGenerated.TryGetError(out var fourthLimitation))
        {
            return Result<TParent, ContractLimitation>.Failure(
                fourthLimitation.WithPathPrefix(fourth.Name));
        }

        firstGenerated.TryGetValue(out var firstValue);
        secondGenerated.TryGetValue(out var secondValue);
        thirdGenerated.TryGetValue(out var thirdValue);
        fourthGenerated.TryGetValue(out var fourthValue);
        var result = combine(firstValue!, secondValue!, thirdValue!, fourthValue!);
        return result is null
            ? ContractSupport.GenerationFailure<TParent>(
                path,
                "the object combine function returned null.")
            : Result<TParent, ContractLimitation>.Success(result);
    }

    internal override Result<IReadOnlyList<TParent>, ContractLimitation> Shrink(
        TParent value,
        ShrinkContext context,
        string path)
    {
        var currentFirst = first.Get(value);
        var currentSecond = second.Get(value);
        var currentThird = third.Get(value);
        var currentFourth = fourth.Get(value);
        var firstShrunk = first.Contract.ShrinkCore(currentFirst, context, string.Empty);
        if (firstShrunk.TryGetError(out var firstLimitation))
        {
            return Result<IReadOnlyList<TParent>, ContractLimitation>.Failure(
                firstLimitation.WithPathPrefix(first.Name));
        }

        var secondShrunk = second.Contract.ShrinkCore(currentSecond, context, string.Empty);
        if (secondShrunk.TryGetError(out var secondLimitation))
        {
            return Result<IReadOnlyList<TParent>, ContractLimitation>.Failure(
                secondLimitation.WithPathPrefix(second.Name));
        }

        var thirdShrunk = third.Contract.ShrinkCore(currentThird, context, string.Empty);
        if (thirdShrunk.TryGetError(out var thirdLimitation))
        {
            return Result<IReadOnlyList<TParent>, ContractLimitation>.Failure(
                thirdLimitation.WithPathPrefix(third.Name));
        }

        var fourthShrunk = fourth.Contract.ShrinkCore(currentFourth, context, string.Empty);
        if (fourthShrunk.TryGetError(out var fourthLimitation))
        {
            return Result<IReadOnlyList<TParent>, ContractLimitation>.Failure(
                fourthLimitation.WithPathPrefix(fourth.Name));
        }

        var results = new List<TParent>();
        firstShrunk.TryGetValue(out var firstCandidates);
        foreach (var candidate in firstCandidates!)
        {
            results.Add(combine(candidate, currentSecond, currentThird, currentFourth));
        }

        secondShrunk.TryGetValue(out var secondCandidates);
        foreach (var candidate in secondCandidates!)
        {
            results.Add(combine(currentFirst, candidate, currentThird, currentFourth));
        }

        thirdShrunk.TryGetValue(out var thirdCandidates);
        foreach (var candidate in thirdCandidates!)
        {
            results.Add(combine(currentFirst, currentSecond, candidate, currentFourth));
        }

        fourthShrunk.TryGetValue(out var fourthCandidates);
        foreach (var candidate in fourthCandidates!)
        {
            results.Add(combine(currentFirst, currentSecond, currentThird, candidate));
        }

        return Result<IReadOnlyList<TParent>, ContractLimitation>.Success(results);
    }

    internal override void CollectLimitations(
        List<ContractLimitation> sink,
        string path,
        ContractDerivation derivation,
        HashSet<object> visited)
    {
        first.Contract.CollectLimitations(
            sink,
            ContractSupport.ChildPath(path, first.Name),
            derivation,
            visited);
        second.Contract.CollectLimitations(
            sink,
            ContractSupport.ChildPath(path, second.Name),
            derivation,
            visited);
        third.Contract.CollectLimitations(
            sink,
            ContractSupport.ChildPath(path, third.Name),
            derivation,
            visited);
        fourth.Contract.CollectLimitations(
            sink,
            ContractSupport.ChildPath(path, fourth.Name),
            derivation,
            visited);
    }
}
