using FunnySharp.TestSupport.Contracts;

namespace FunnySharp.Tests;

public sealed class ContractGenerationTests
{
    [Fact]
    public void GeneratedValuesRespectEveryDeclaredConstraint()
    {
        var ints = Contract.Int32Range(-2, 2);
        var decimals = Contract.DecimalRange(0.01m, 5m);
        var doubles = Contract.DoubleRange(-1.5d, 1.5d);
        var strings = Contract.NonEmptyString;
        var lists = Contract.List(Contract.Int32Range(3, 3), 2, 4);
        var optional = Contract.Optional(Contract.Int32Range(7, 7));

        var options = new ContractGenerationOptions(
            seed: 1,
            maxDepth: 4,
            maxCollectionLength: 4,
            maxNodes: 64);

        for (var seed = 1; seed <= 25; seed++)
        {
            var seeded = new ContractGenerationOptions(
                seed,
                options.MaxDepth,
                options.MaxCollectionLength,
                options.MaxNodes);

            Assert.True(ints.Generate(seeded).TryGetValue(out var intValue));
            Assert.InRange(intValue, -2, 2);

            Assert.True(decimals.Generate(seeded).TryGetValue(out var decimalValue));
            Assert.InRange(decimalValue, 0.01m, 5m);

            Assert.True(doubles.Generate(seeded).TryGetValue(out var doubleValue));
            Assert.InRange(doubleValue, -1.5d, 1.5d);

            Assert.True(strings.Generate(seeded).TryGetValue(out var stringValue));
            Assert.InRange(stringValue.Length, 1, options.MaxCollectionLength);

            Assert.True(lists.Generate(seeded).TryGetValue(out var listValue));
            Assert.InRange(listValue.Count, 2, 4);
            Assert.All(listValue, static item => Assert.Equal(3, item));

            Assert.True(optional.Generate(seeded).TryGetValue(out var optionalValue));
            if (optionalValue.TryGetValue(out var present))
            {
                Assert.Equal(7, present);
            }
        }
    }

    [Fact]
    public void GenerationIsDeterministicPerSeed()
    {
        var options = new ContractGenerationOptions(seed: 42);
        var first = ContractOrderConsumer.OrderContract.GenerateSamples(5, options);
        var second = ContractOrderConsumer.OrderContract.GenerateSamples(5, options);

        Assert.True(first.TryGetValue(out var firstSamples));
        Assert.True(second.TryGetValue(out var secondSamples));
        Assert.Equal(firstSamples!.Count, secondSamples!.Count);
        for (var index = 0; index < firstSamples.Count; index++)
        {
            Assert.True(OrderComparison.OrderEquals(firstSamples[index], secondSamples[index]));
        }

        Assert.All(firstSamples, static order => Assert.Empty(ContractOrderConsumer.Validate(order)));
    }

    [Fact]
    public void DeclaredMinimumLengthFailsExplicitlyWhenTheWidthBudgetIsTooSmall()
    {
        var contract = Contract.List(Contract.Int32, minimumLength: 3);
        var generated = contract.Generate(
            new ContractGenerationOptions(seed: 1, maxCollectionLength: 2));

        var limitation = Assert.IsType<ContractLimitation>(
            generated.TryGetError(out var error) ? error : null);
        Assert.Equal(ContractDerivation.Generation, limitation.Derivation);
        Assert.Contains("minimum length", limitation.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void NonEmptyStringFailsExplicitlyWhenTheWidthBudgetIsZero()
    {
        var generated = Contract.NonEmptyString.Generate(
            new ContractGenerationOptions(seed: 1, maxCollectionLength: 0));

        var limitation = Assert.IsType<ContractLimitation>(
            generated.TryGetError(out var error) ? error : null);
        Assert.Contains("minimum string length", limitation.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void NodeBudgetIsExplicit()
    {
        var generated = Contract.List(Contract.Int32, 3, 3).Generate(
            new ContractGenerationOptions(seed: 1, maxNodes: 2));

        var limitation = Assert.IsType<ContractLimitation>(
            generated.TryGetError(out var error) ? error : null);
        Assert.Contains("node budget", limitation.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void CrossFieldRulesAreNotSilentlyRejectionSampled()
    {
        var contract = Contract.Int32Range(1, 10).Ensure(
            static value => value >= 5,
            "value must be at least 5");

        Assert.False(contract.Capabilities.CanGenerate);

        var generated = contract.Generate(new ContractGenerationOptions(seed: 1));
        var limitation = Assert.IsType<ContractLimitation>(
            generated.TryGetError(out var error) ? error : null);
        Assert.Equal(ContractDerivation.Generation, limitation.Derivation);
        Assert.Contains("arbitrary predicate", limitation.Reason, StringComparison.Ordinal);

        var validGenerator = contract.WithGenerator(static _ => 7).Generate(
            new ContractGenerationOptions(seed: 1));
        Assert.True(validGenerator.TryGetValue(out var generatedValue));
        Assert.Equal(7, generatedValue);

        var invalidGenerator = contract.WithGenerator(static _ => 2).Generate(
            new ContractGenerationOptions(seed: 1));
        var invalidLimitation = Assert.IsType<ContractLimitation>(
            invalidGenerator.TryGetError(out var invalidError) ? invalidError : null);
        Assert.Contains("does not satisfy", invalidLimitation.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void ShrinkingPreservesRefinedRangesAndCollectionCardinality()
    {
        var contract = Contract.Int32Range(1, 10)
            .Ensure(static value => value >= 5, "value must be at least 5");

        var shrunk = contract.Shrink(10);
        Assert.True(shrunk.TryGetValue(out var candidates));
        Assert.NotEmpty(candidates);
        Assert.All(candidates, static candidate => Assert.InRange(candidate, 5, 10));
        Assert.All(candidates, static candidate => Assert.True(candidate != 10));

        var listContract = Contract.List(Contract.Int32Range(1, 10), 2, 4);
        var listShrunk = listContract.Shrink(new[] { 10, 9, 8 });
        Assert.True(listShrunk.TryGetValue(out var listCandidates));
        Assert.NotEmpty(listCandidates);
        Assert.All(
            listCandidates,
            static candidate => Assert.InRange(candidate.Count, 2, 4));
        Assert.All(
            listCandidates,
            static candidate => Assert.All(candidate, static item => Assert.InRange(item, 1, 10)));
        Assert.DoesNotContain(listCandidates, static candidate => candidate.SequenceEqual(new[] { 10, 9, 8 }));
    }

    [Fact]
    public void CallerSuppliedShrinkerCandidatesAreFilteredToTheContractDomain()
    {
        var contract = Contract.Int32Range(0, 10).WithShrinker(
            static _ => [5, 11, -1, 5]);

        var shrunk = contract.Shrink(10);
        Assert.True(shrunk.TryGetValue(out var candidates));
        Assert.Equal(new[] { 5 }, candidates.ToArray());
    }

    [Fact]
    public void ShrinkingWideNumericRangesUsesOverflowSafeMidpoints()
    {
        var decimals = Contract.DecimalRange(decimal.MinValue, decimal.MaxValue);
        var shrunkDecimals = decimals.Shrink(decimal.MaxValue);
        Assert.True(shrunkDecimals.TryGetValue(out var decimalCandidates));
        Assert.Contains(0m, decimalCandidates!);
        Assert.All(
            decimalCandidates!,
            static candidate => Assert.InRange(
                candidate,
                decimal.MinValue,
                decimal.MaxValue));

        var integers = Contract.Int32Range(int.MinValue, int.MaxValue);
        var shrunkIntegers = integers.Shrink(int.MaxValue);
        Assert.True(shrunkIntegers.TryGetValue(out var integerCandidates));
        Assert.Contains(-1, integerCandidates!);
    }

    [Fact]
    public void CallerGeneratedNullIsAnExplicitFailure()
    {
        var contract = Contract.String.WithGenerator(static _ => null!);
        var generated = contract.Generate(new ContractGenerationOptions(seed: 1));

        var limitation = Assert.IsType<ContractLimitation>(
            generated.TryGetError(out var error) ? error : null);
        Assert.Contains("null", limitation.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void ShrinkingRejectsAnInputThatDoesNotSatisfyTheContract()
    {
        var contract = Contract.Int32Range(1, 10);
        var shrunk = contract.Shrink(0);

        var limitation = Assert.IsType<ContractLimitation>(
            shrunk.TryGetError(out var error) ? error : null);
        Assert.Equal(ContractDerivation.Shrinking, limitation.Derivation);
        Assert.Contains("does not satisfy", limitation.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerationOptionsRejectInvalidBudgets()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContractGenerationOptions(1, maxDepth: -1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ContractGenerationOptions(1, maxCollectionLength: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContractGenerationOptions(1, maxNodes: 0));
    }

    [Fact]
    public void GenerateSamplesReturnsTheRequestedCount()
    {
        var generated = Contract.Int32Range(1, 5).GenerateSamples(
            4,
            new ContractGenerationOptions(seed: 3));

        Assert.True(generated.TryGetValue(out var samples));
        Assert.Equal(4, samples.Count);
        Assert.All(samples, static sample => Assert.InRange(sample, 1, 5));

        var empty = Contract.Int32.GenerateSamples(
            0,
            new ContractGenerationOptions(seed: 3));
        Assert.True(empty.TryGetValue(out var none));
        Assert.Empty(none);
    }
}
