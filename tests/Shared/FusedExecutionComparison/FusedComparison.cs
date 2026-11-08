using System.Text.Json;
using FunnySharp;

namespace FunnySharp.TestSupport.FusedExecution;

public static class FusedComparison
{
    public static int VerifyAll()
    {
        var checks = 0;
        checks += VerifySummaryParity();
        checks += VerifyDecoderParity();
        return checks;
    }

    private static int VerifySummaryParity()
    {
        var checks = 0;
        var empty = Array.Empty<OrderLine>();
        var single = new[] { new OrderLine("A", "note", 1, 10m) };
        var mixed = FusedFixtures.CreateLines(32);
        var ties = new[]
        {
            new OrderLine("A", "first", 1, 10m),
            new OrderLine("B", "second", 2, 10m),
            new OrderLine("C", "third", 3, 10m),
        };
        var nullNotes = new[]
        {
            new OrderLine("A", null, 1, 1m),
            new OrderLine("B", "b", 1, 2m),
            new OrderLine("C", null, 1, 3m),
        };

        var fixtures = new (string Name, IReadOnlyList<OrderLine> Lines)[]
        {
            ("empty", empty),
            ("single", single),
            ("mixed", mixed),
            ("ties", ties),
            ("null-notes", nullNotes),
        };

        foreach (var (name, lines) in fixtures)
        {
            var delegated = FusedSummaryDefinitions.Statistics.Evaluate(lines);
            var fused = FusedSummaryDefinitions.EvaluateFused(lines);
            AssertEqual(delegated, fused, "summary " + name);
            checks++;

            var countingDelegated = new CountingSequence<OrderLine>(lines);
            _ = FusedSummaryDefinitions.Statistics.Evaluate(countingDelegated);
            AssertEqual(1, countingDelegated.Enumerations, "delegated enumerations " + name);
            AssertEqual(lines.Count + 1, countingDelegated.MoveNextCalls, "delegated MoveNext " + name);
            AssertEqual(lines.Count, countingDelegated.CurrentCalls, "delegated Current " + name);
            AssertEqual(1, countingDelegated.DisposeCalls, "delegated Dispose " + name);

            var countingFused = new CountingSequence<OrderLine>(lines);
            _ = FusedSummaryDefinitions.EvaluateFused(countingFused);
            AssertEqual(1, countingFused.Enumerations, "fused enumerations " + name);
            AssertEqual(lines.Count + 1, countingFused.MoveNextCalls, "fused MoveNext " + name);
            AssertEqual(lines.Count, countingFused.CurrentCalls, "fused Current " + name);
            AssertEqual(1, countingFused.DisposeCalls, "fused Dispose " + name);
            checks += 8;
        }

        FusedSelectorLedger.Clear();
        _ = FusedSummaryDefinitions.Ledger.Evaluate(ties);
        var delegatedLedger = FusedSelectorLedger.Events.ToArray();
        FusedSelectorLedger.Clear();
        _ = FusedSummaryDefinitions.EvaluateLedgerFused(ties);
        var fusedLedger = FusedSelectorLedger.Events.ToArray();
        AssertSequenceEqual(delegatedLedger, fusedLedger, "selector ledger");
        AssertSequenceEqual(
            new[] { "quantity", "price", "quantity", "price", "quantity", "price" },
            fusedLedger,
            "selector ledger content");
        checks += 2;

        var delegatedNotes = FusedSummaryDefinitions.Notes.Evaluate(nullNotes);
        var fusedNotes = FusedSummaryDefinitions.EvaluateNotesFused(nullNotes);
        AssertEqual(delegatedNotes, fusedNotes, "null-note extrema");
        checks++;

        var overflowLines = new[]
        {
            new OrderLine("A", null, 1, decimal.MaxValue),
            new OrderLine("B", null, 1, decimal.MaxValue),
        };
        AssertThrows<OverflowException>(
            () => FusedSummaryDefinitions.Overflow.Evaluate(overflowLines),
            "delegated overflow");
        AssertThrows<OverflowException>(
            () => FusedSummaryDefinitions.EvaluateOverflowFused(overflowLines),
            "fused overflow");
        checks += 2;

        var nullSourceException = AssertThrows<ArgumentNullException>(
            () => FusedSummaryDefinitions.Statistics.Evaluate(null!),
            "delegated null source");
        AssertEqual("source", nullSourceException.ParamName, "delegated null parameter");
        var fusedNullSourceException = AssertThrows<ArgumentNullException>(
            () => FusedSummaryDefinitions.EvaluateFused(null!),
            "fused null source");
        AssertEqual("source", fusedNullSourceException.ParamName, "fused null parameter");
        checks += 2;

        var firstKey = new string('a', 4);
        var secondKey = new string(new[] { 'a', 'a', 'a', 'a' });
        AssertEqual(false, ReferenceEquals(firstKey, secondKey), "tie keys are reference-distinct");
        var tieLines = new[]
        {
            new OrderLine("A", firstKey, 1, 1m),
            new OrderLine("B", secondKey, 2, 2m),
        };
        var delegatedTie = FusedSummaryDefinitions.TieOrder.Evaluate(tieLines);
        var fusedTie = FusedSummaryDefinitions.EvaluateTieOrderFused(tieLines);
        AssertEqual(delegatedTie, fusedTie, "tie order");
        AssertEqual(true, delegatedTie.First.TryGetValue(out var delegatedFirst) && ReferenceEquals(delegatedFirst, firstKey), "delegated first tie");
        AssertEqual(true, delegatedTie.Last.TryGetValue(out var delegatedLast) && ReferenceEquals(delegatedLast, firstKey), "delegated last tie");
        AssertEqual(true, fusedTie.First.TryGetValue(out var fusedFirst) && ReferenceEquals(fusedFirst, firstKey), "fused first tie");
        AssertEqual(true, fusedTie.Last.TryGetValue(out var fusedLast) && ReferenceEquals(fusedLast, firstKey), "fused last tie");
        checks += 6;

        FusedSelectorLedger.Clear();
        _ = FusedSummaryDefinitions.ExtremaLedger.Evaluate(ties);
        var delegatedExtrema = FusedSelectorLedger.Events.ToArray();
        FusedSelectorLedger.Clear();
        _ = FusedSummaryDefinitions.EvaluateExtremaLedgerFused(ties);
        var fusedExtrema = FusedSelectorLedger.Events.ToArray();
        AssertSequenceEqual(delegatedExtrema, fusedExtrema, "extrema selector ledger");
        AssertSequenceEqual(
            new[] { "price", "price", "price", "price", "price", "price" },
            fusedExtrema,
            "extrema selector ledger content");
        checks += 2;

        var selectorOverflowLines = new[] { new OverflowLine(3_000_000, long.MaxValue / 2) };
        var delegatedIntOverflow = FusedSummaryDefinitions.IntSelectorOverflow.Evaluate(selectorOverflowLines);
        var fusedIntOverflow = FusedSummaryDefinitions.EvaluateIntSelectorOverflowFused(selectorOverflowLines);
        AssertEqual(delegatedIntOverflow, fusedIntOverflow, "int selector overflow parity");
        AssertEqual(unchecked(3_000_000 * 1000), fusedIntOverflow.Second, "int selector wrapped value");
        var delegatedLongOverflow = FusedSummaryDefinitions.LongSelectorOverflow.Evaluate(selectorOverflowLines);
        var fusedLongOverflow = FusedSummaryDefinitions.EvaluateLongSelectorOverflowFused(selectorOverflowLines);
        AssertEqual(delegatedLongOverflow, fusedLongOverflow, "long selector overflow parity");
        AssertEqual(unchecked((long.MaxValue / 2) * 3), fusedLongOverflow.Second, "long selector wrapped value");
        checks += 4;

        foreach (var fault in new[] { SourceFault.MoveNext, SourceFault.Current, SourceFault.Dispose })
        {
            var delegatedSequence = new FaultingSequence<OrderLine>(mixed, fault);
            var delegatedException = AssertThrows<InvalidOperationException>(
                () => FusedSummaryDefinitions.Statistics.Evaluate(delegatedSequence),
                "delegated source fault " + fault);
            var fusedSequence = new FaultingSequence<OrderLine>(mixed, fault);
            var fusedException = AssertThrows<InvalidOperationException>(
                () => FusedSummaryDefinitions.EvaluateFused(fusedSequence),
                "fused source fault " + fault);
            AssertEqual(delegatedException.Message, fusedException.Message, "source fault message " + fault);
            AssertEqual(1, delegatedSequence.DisposeCalls, "delegated source fault disposal " + fault);
            AssertEqual(1, fusedSequence.DisposeCalls, "fused source fault disposal " + fault);
            checks += 3;
        }

        return checks;
    }

    private static int VerifyDecoderParity()
    {
        var checks = 0;
        foreach (var (name, json) in FusedFixtures.DecoderFixtures())
        {
            using var document = JsonDocument.Parse(json);
            var input = document.RootElement;
            var delegated = FusedFixtures.Summarize(FusedDecoderDefinitions.OrderDecoder.Decode(input));
            var fused = FusedFixtures.Summarize(FusedDecoderDefinitions.DecodeFused(input));
            AssertEqual(delegated, fused, "decoder " + name);
            checks++;

            if (!delegated.StartsWith("valid:", StringComparison.Ordinal))
            {
                continue;
            }

            var handWritten = FusedConsumers.DecoderHandWritten(input);
            var fusedDigest = FusedConsumers.DecoderFused(input);
            AssertEqual(fusedDigest, handWritten, "hand-written decoder " + name);
            checks++;
        }

        foreach (var (name, json) in FusedFixtures.OptionalFixtures())
        {
            using var document = JsonDocument.Parse(json);
            var input = document.RootElement;
            var delegated = FusedFixtures.Summarize(FusedDecoderDefinitions.OptionalDecoder.Decode(input));
            var fused = FusedFixtures.Summarize(FusedDecoderDefinitions.DecodeOptionalFused(input));
            AssertEqual(delegated, fused, "optional decoder " + name);
            checks++;
        }

        using var duplicate = JsonDocument.Parse(FusedFixtures.DuplicateFirstValidJson);
        AssertEqual(
            true,
            FusedDecoderDefinitions.DecodeFused(duplicate.RootElement).IsValid,
            "duplicate first-valid accepted");
        using var duplicateInvalid = JsonDocument.Parse(FusedFixtures.DuplicateFirstInvalidJson);
        AssertEqual(
            false,
            FusedDecoderDefinitions.DecodeFused(duplicateInvalid.RootElement).IsValid,
            "duplicate first-invalid rejected");
        checks += 2;

        foreach (var (name, json) in FusedFixtures.ScalarFixtures())
        {
            using var document = JsonDocument.Parse(json);
            var delegated = FusedFixtures.Summarize(FusedDecoderDefinitions.ScalarDecoder.Decode(document.RootElement));
            var fused = FusedFixtures.Summarize(FusedDecoderDefinitions.DecodeScalarFused(document.RootElement));
            AssertEqual(delegated, fused, "scalar leaf decoder " + name);
            checks++;
        }

        foreach (var (name, json) in FusedFixtures.TemporalFixtures())
        {
            using var document = JsonDocument.Parse(json);
            var delegated = FusedFixtures.Summarize(FusedDecoderDefinitions.TemporalDecoder.Decode(document.RootElement));
            var fused = FusedFixtures.Summarize(FusedDecoderDefinitions.DecodeTemporalFused(document.RootElement));
            AssertEqual(delegated, fused, "temporal leaf decoder " + name);
            checks++;
        }

        foreach (var (name, json) in FusedFixtures.PlainListFixtures())
        {
            using var document = JsonDocument.Parse(json);
            var delegated = FusedFixtures.Summarize(FusedDecoderDefinitions.PlainListDecoder.Decode(document.RootElement));
            var fused = FusedFixtures.Summarize(FusedDecoderDefinitions.DecodePlainListFused(document.RootElement));
            AssertEqual(delegated, fused, "plain list decoder " + name);
            checks++;
        }

        using (var listShapeDocument = JsonDocument.Parse("{\"label\":\"x\",\"values\":[1,2,3]}"))
        {
            var validation = FusedDecoderDefinitions.DecodePlainListFused(listShapeDocument.RootElement);
            AssertEqual(true, validation.TryGetValue(out var listValue), "plain list valid");
            AssertEqual(false, listValue!.Values is List<int>, "plain list is not List<T>");
            var immutableValues = (ICollection<int>)listValue!.Values;
            AssertEqual(true, immutableValues.IsReadOnly, "plain list is read-only");
            AssertThrows<NotSupportedException>(() => immutableValues.Add(4), "plain list rejects mutation");
            checks += 4;
        }

        using (var emptyListDocument = JsonDocument.Parse("{\"label\":\"x\",\"values\":[]}"))
        {
            var validation = FusedDecoderDefinitions.DecodePlainListFused(emptyListDocument.RootElement);
            AssertEqual(true, validation.TryGetValue(out var emptyValue), "empty plain list valid");
            AssertEqual(true, ReferenceEquals(emptyValue!.Values, Array.Empty<int>()), "empty plain list is Array.Empty");
            checks += 2;
        }

        using (var overloadDocument = JsonDocument.Parse("{\"label\":\"x\",\"values\":[\"a\",\"b\"]}"))
        {
            var delegated = FusedFixtures.Summarize(FusedDecoderDefinitions.ListOverloadDecoder.Decode(overloadDocument.RootElement));
            var fused = FusedFixtures.Summarize(FusedDecoderDefinitions.DecodeListOverloadFused(overloadDocument.RootElement));
            AssertEqual(delegated, fused, "list overload parity");
            AssertEqual("valid:readonly", fused, "list overload picks the IReadOnlyList constructor");
            checks += 2;
        }

        using (var probeDocument = JsonDocument.Parse("{\"id\":7,\"name\":\"probe\"}"))
        {
            ConstructionProbe.ResetConstructions();
            var delegated = FusedDecoderDefinitions.ProbeDecoder.Decode(probeDocument.RootElement);
            AssertEqual(1, ConstructionProbe.Constructions, "delegated construction count");
            ConstructionProbe.ResetConstructions();
            var fused = FusedDecoderDefinitions.DecodeProbeFused(probeDocument.RootElement);
            AssertEqual(1, ConstructionProbe.Constructions, "fused construction count");
            AssertEqual(
                FusedFixtures.Summarize(delegated, static value => value.Id + ":" + value.Name),
                FusedFixtures.Summarize(fused, static value => value.Id + ":" + value.Name),
                "construction probe content");
            checks += 3;

            ConstructionProbe.ResetConstructions();
            using var invalidProbe = JsonDocument.Parse("{\"id\":7}");
            _ = FusedDecoderDefinitions.DecodeProbeFused(invalidProbe.RootElement);
            AssertEqual(0, ConstructionProbe.Constructions, "invalid probe constructs nothing");
            checks++;

            ExplosiveProbe.ResetConstructions();
            var delegatedFault = AssertThrows<InvalidOperationException>(
                () => FusedDecoderDefinitions.ExplosiveDecoder.Decode(probeDocument.RootElement),
                "delegated construction fault");
            AssertEqual(1, ExplosiveProbe.Constructions, "delegated construction fault count");
            ExplosiveProbe.ResetConstructions();
            var fusedFault = AssertThrows<InvalidOperationException>(
                () => FusedDecoderDefinitions.DecodeExplosiveFused(probeDocument.RootElement),
                "fused construction fault");
            AssertEqual(1, ExplosiveProbe.Constructions, "fused construction fault count");
            AssertEqual(delegatedFault.Message, fusedFault.Message, "construction fault message");
            checks += 4;
        }

        return checks;
    }

    private static void AssertEqual<T>(T expected, T actual, string context)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                context + ": expected " + expected + ", actual " + actual + ".");
        }
    }

    private static void AssertSequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual, string context)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException(
                context + ": expected [" + string.Join(", ", expected) + "], actual ["
                + string.Join(", ", actual) + "].");
        }
    }

    private static TException AssertThrows<TException>(Action action, string context)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException exception)
        {
            return exception;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                context + ": expected " + typeof(TException).Name + ", caught "
                + exception.GetType().Name + ".",
                exception);
        }

        throw new InvalidOperationException(
            context + ": expected " + typeof(TException).Name + ", no exception was thrown.");
    }
}
