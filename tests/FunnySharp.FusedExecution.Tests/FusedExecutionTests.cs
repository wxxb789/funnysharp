using System.Text.Json;
using FunnySharp;
using FunnySharp.TestSupport.FusedExecution;

namespace FunnySharp.FusedExecution.Tests;

public sealed class FusedExecutionTests
{
    [Fact]
    public void SharedComparisonPassesForSupportedSummaryAndDecoderShapes()
    {
        var checks = FusedComparison.VerifyAll();
        Assert.Equal(129, checks);
    }

    [Fact]
    public void GeneratedSummaryReturnsTheSameReportAsTheDelegatedSummary()
    {
        var lines = FusedFixtures.CreateLines(24);
        var (expectedCount, expectedTotal, expectedMin, expectedMax) =
            FusedSummaryDefinitions.Statistics.Evaluate(lines);
        var (actualCount, actualTotal, actualMin, actualMax) =
            FusedSummaryDefinitions.EvaluateFused(lines);

        Assert.Equal(expectedCount, actualCount);
        Assert.Equal(expectedTotal, actualTotal);
        Assert.Equal(expectedMin, actualMin);
        Assert.Equal(expectedMax, actualMax);
    }

    [Fact]
    public void GeneratedSummaryPreservesSelectorOrderAndSingleEnumeration()
    {
        var lines = FusedFixtures.CreateLines(5);
        FusedSelectorLedger.Clear();
        _ = FusedSummaryDefinitions.Ledger.Evaluate(lines);
        var delegated = FusedSelectorLedger.Events.ToArray();

        FusedSelectorLedger.Clear();
        var counting = new CountingSequence<OrderLine>(lines);
        _ = FusedSummaryDefinitions.EvaluateLedgerFused(counting);
        var fused = FusedSelectorLedger.Events.ToArray();

        Assert.Equal(delegated, fused);
        Assert.Equal(1, counting.Enumerations);
        Assert.Equal(lines.Length + 1, counting.MoveNextCalls);
        Assert.Equal(lines.Length, counting.CurrentCalls);
        Assert.Equal(1, counting.DisposeCalls);
    }

    [Fact]
    public void GeneratedSummaryThrowsTheDocumentedExceptions()
    {
        var overflowLines = new[]
        {
            new OrderLine("A", null, 1, decimal.MaxValue),
            new OrderLine("B", null, 1, decimal.MaxValue),
        };

        Assert.Throws<OverflowException>(() => FusedSummaryDefinitions.EvaluateOverflowFused(overflowLines));
        var nullSource = Assert.Throws<ArgumentNullException>(
            () => FusedSummaryDefinitions.EvaluateFused(null!));
        Assert.Equal("source", nullSource.ParamName);
    }

    [Fact]
    public void GeneratedSummaryStopsLaterComponentsAfterASelectorFaultAndDisposesOnce()
    {
        var lines = new[]
        {
            new OrderLine("A", null, 1, 10m),
            new OrderLine("BOOM", null, 1, 20m),
            new OrderLine("C", null, 1, 30m),
        };

        FusedSelectorLedger.Clear();
        var delegatedCounting = new CountingSequence<OrderLine>(lines);
        var delegatedException = Assert.Throws<InvalidOperationException>(
            () => FusedSummaryDefinitions.Throwing.Evaluate(delegatedCounting));
        var delegatedEvents = FusedSelectorLedger.Events.ToArray();

        FusedSelectorLedger.Clear();
        var fusedCounting = new CountingSequence<OrderLine>(lines);
        var fusedException = Assert.Throws<InvalidOperationException>(
            () => FusedSummaryDefinitions.EvaluateThrowingFused(fusedCounting));
        var fusedEvents = FusedSelectorLedger.Events.ToArray();

        Assert.Equal("quantity failed", delegatedException.Message);
        Assert.Equal("quantity failed", fusedException.Message);
        Assert.Equal(delegatedEvents, fusedEvents);
        Assert.Equal(new[] { "quantity", "price", "quantity" }, fusedEvents);
        Assert.Equal(1, delegatedCounting.DisposeCalls);
        Assert.Equal(1, fusedCounting.DisposeCalls);
    }

    [Fact]
    public void GeneratedDecoderMatchesRuntimeErrorsForInvalidInput()
    {
        using var document = JsonDocument.Parse(FusedFixtures.EmptyTagElementJson);
        var input = document.RootElement;
        var expected = FusedFixtures.Summarize(FusedDecoderDefinitions.OrderDecoder.Decode(input));
        var actual = FusedFixtures.Summarize(FusedDecoderDefinitions.DecodeFused(input));

        Assert.Equal(expected, actual);
        Assert.StartsWith("invalid:", actual, StringComparison.Ordinal);
        Assert.Contains("tags[1]", actual, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedDecoderUsesTheFirstDuplicateProperty()
    {
        using var firstValid = JsonDocument.Parse(FusedFixtures.DuplicateFirstValidJson);
        Assert.True(FusedDecoderDefinitions.DecodeFused(firstValid.RootElement).IsValid);

        using var firstInvalid = JsonDocument.Parse(FusedFixtures.DuplicateFirstInvalidJson);
        var validation = FusedDecoderDefinitions.DecodeFused(firstInvalid.RootElement);
        Assert.False(validation.IsValid);
        validation.TryGetErrors(out var errors);
        Assert.Contains(errors!, error => error.Path == "id");
    }

    [Fact]
    public void GeneratedOptionalDecoderDistinguishesMissingNullAndPresent()
    {
        using var missing = JsonDocument.Parse(FusedFixtures.OptionalMissingJson);
        var missingValue = FusedDecoderDefinitions.DecodeOptionalFused(missing.RootElement);
        Assert.True(missingValue.TryGetValue(out var missingOrder));
        Assert.True(missingOrder.Customer.IsNone);
        Assert.True(missingOrder.Note.IsNone);

        using var present = JsonDocument.Parse(FusedFixtures.OptionalPresentJson);
        var presentValue = FusedDecoderDefinitions.DecodeOptionalFused(present.RootElement);
        Assert.True(presentValue.TryGetValue(out var presentOrder));
        Assert.True(presentOrder.Customer.TryGetValue(out var customer));
        Assert.Equal("ACME", customer);
        Assert.True(presentOrder.Note.TryGetValue(out var note));
        Assert.Equal("hello", note);

        using var nullNote = JsonDocument.Parse(FusedFixtures.OptionalNullNoteJson);
        var nullNoteValue = FusedDecoderDefinitions.DecodeOptionalFused(nullNote.RootElement);
        Assert.True(nullNoteValue.TryGetValue(out var nullNoteOrder));
        Assert.True(nullNoteOrder.Customer.IsNone);
        Assert.True(nullNoteOrder.Note.IsNone);

        using var nullCustomer = JsonDocument.Parse(FusedFixtures.OptionalNullCustomerJson);
        var nullCustomerValue = FusedDecoderDefinitions.DecodeOptionalFused(nullCustomer.RootElement);
        Assert.False(nullCustomerValue.IsValid);
        nullCustomerValue.TryGetErrors(out var errors);
        Assert.Contains(errors!, error => error.Path == "customer");
    }

    [Fact]
    public void HandWrittenValidDecoderMatchesGeneratedAndDelegatedDecoders()
    {
        using var document = JsonDocument.Parse(FusedFixtures.ValidJson);
        var input = document.RootElement;
        var fused = FusedConsumers.DecoderFused(input);
        var delegated = FusedConsumers.DecoderDelegated(input);
        var handWritten = FusedConsumers.DecoderHandWritten(input);

        Assert.Equal(fused, delegated);
        Assert.Equal(fused, handWritten);
    }
}
