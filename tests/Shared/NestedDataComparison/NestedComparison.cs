using System.Text.Json;
using FunnySharp;
using FunnySharp.TestSupport.FallibleReduction;

namespace FunnySharp.TestSupport.NestedDataComparison;

internal static class NestedComparisonVerifier
{
    internal static void Verify()
    {
        VerifyImportFixtures();
        VerifyAlignedFixtures();
        VerifyCartesianFixtures();
        VerifyTypedNullLists();
        VerifyConsumptionLedger();
        VerifyCombineLedger();
    }

    private static void VerifyImportFixtures()
    {
        foreach (var fixture in NestedFixtures.Imports)
        {
            var hand = HandWrittenNestedConsumer.Import(fixture.Json);
            var deferred = HandWrittenDeferredPathNestedConsumer.Import(fixture.Json);
            var funny = FunnySharpNestedConsumer.Import(fixture.Json);

            Equal(fixture.Expected.Rows, hand.Rows, $"{fixture.Name}: hand rows");
            Equal(fixture.Expected.Rows, deferred.Rows, $"{fixture.Name}: deferred rows");
            Equal(fixture.Expected.Rows, funny.Rows, $"{fixture.Name}: funny rows");
            Equal(fixture.Expected.Rejections, hand.Rejections, $"{fixture.Name}: hand rejections");
            Equal(
                fixture.Expected.Rejections,
                deferred.Rejections,
                $"{fixture.Name}: deferred rejections");
            Equal(fixture.Expected.Rejections, funny.Rejections, $"{fixture.Name}: funny rejections");
            Equal(
                fixture.Expected.DecodeFailures,
                hand.DecodeFailures,
                $"{fixture.Name}: hand decode failures");
            Equal(
                fixture.Expected.DecodeFailures,
                deferred.DecodeFailures,
                $"{fixture.Name}: deferred decode failures");
            Equal(
                fixture.Expected.DecodeFailures,
                funny.DecodeFailures,
                $"{fixture.Name}: funny decode failures");
            Equal(
                fixture.Expected.ExpansionFailures,
                hand.ExpansionFailures,
                $"{fixture.Name}: hand expansion failures");
            Equal(
                fixture.Expected.ExpansionFailures,
                deferred.ExpansionFailures,
                $"{fixture.Name}: deferred expansion failures");
            Equal(
                fixture.Expected.ExpansionFailures,
                funny.ExpansionFailures,
                $"{fixture.Name}: funny expansion failures");

            if (fixture.Ordinary is { } ordinaryBehavior)
            {
                VerifyOrdinaryImport(fixture, ordinaryBehavior);
            }
        }
    }

    private static void VerifyOrdinaryImport(
        ImportFixture fixture,
        OrdinaryImportBehavior behavior)
    {
        if (behavior is OrdinaryImportBehavior.ThrowsJson or OrdinaryImportBehavior.ThrowsArgumentNull)
        {
            var exception = Capture(() => OrdinaryNestedConsumer.Import(fixture.Json));
            var expectedType = behavior == OrdinaryImportBehavior.ThrowsJson
                ? typeof(JsonException)
                : typeof(ArgumentNullException);

            True(
                exception.GetType() == expectedType,
                $"{fixture.Name}: ordinary form threw {exception.GetType().Name} instead of "
                + $"{expectedType.Name}");
            return;
        }

        var ordinaryRows = OrdinaryNestedConsumer.Import(fixture.Json);
        if (behavior == OrdinaryImportBehavior.RowsMatch)
        {
            Equal(fixture.Expected.Rows, ordinaryRows, $"{fixture.Name}: ordinary rows");
        }
        else
        {
            True(
                !fixture.Expected.Rows.SequenceEqual(ordinaryRows),
                $"{fixture.Name}: ordinary rows must differ without a rejection or field policy");
        }
    }

    private static void VerifyAlignedFixtures()
    {
        foreach (var fixture in NestedFixtures.Aligned)
        {
            var hand = HandWrittenNestedConsumer.ImportAligned(fixture.Json);
            var funny = FunnySharpNestedConsumer.ImportAligned(fixture.Json);

            Equal(fixture.Expected.Rows, hand.Rows, $"{fixture.Name}: hand rows");
            Equal(fixture.Expected.Rows, funny.Rows, $"{fixture.Name}: funny rows");
            Equal(fixture.Expected.Failures, hand.Failures, $"{fixture.Name}: hand failures");
            Equal(fixture.Expected.Failures, funny.Failures, $"{fixture.Name}: funny failures");

            var ordinaryRows = OrdinaryNestedConsumer.ImportAligned(fixture.Json);
            Equal(fixture.OrdinaryRows, ordinaryRows, $"{fixture.Name}: ordinary rows");
            if (fixture.Expected.Failures.Count > 0)
            {
                True(
                    hand.Rows.Count == 0 && funny.Rows.Count == 0,
                    $"{fixture.Name}: aligned failure must suppress all observed rows");
            }
        }
    }

    private static void VerifyCartesianFixtures()
    {
        foreach (var fixture in NestedFixtures.Cartesian)
        {
            var hand = HandWrittenNestedConsumer.ImportCartesian(fixture.Json);
            var funny = FunnySharpNestedConsumer.ImportCartesian(fixture.Json);
            var ordinaryRows = OrdinaryNestedConsumer.ImportCartesian(fixture.Json);

            Equal(fixture.Expected.Rows, hand.Rows, $"{fixture.Name}: hand rows");
            Equal(fixture.Expected.Rows, funny.Rows, $"{fixture.Name}: funny rows");
            Equal(fixture.Expected.Failures, hand.Failures, $"{fixture.Name}: hand failures");
            Equal(fixture.Expected.Failures, funny.Failures, $"{fixture.Name}: funny failures");
            Equal(fixture.Expected.Rows, ordinaryRows, $"{fixture.Name}: ordinary rows");
        }
    }

    private static void VerifyTypedNullLists()
    {
        var records = new[]
        {
            new ImportOrder("O-1", "Ada", null!),
        };
        var result = records.Expand(
            static record => record.Lines,
            static (record, line, _) => $"{record.OrderId}:{line.Sku}");

        True(result.TryGetErrors(out var errors), "typed null child list must fail");
        Equal(
            [new ExpansionFailure(NestedRowErrorKind.NullChildSequence, 0, null, null)],
            errors!.Select(MapExpansion).ToArray(),
            "typed null child list failure");
        True(
            errors![0].ToString() == "NullChildSequence at record[0]",
            "typed null child list must render its record position");
    }

    private static void VerifyConsumptionLedger()
    {
        var records = new ReductionProbe<ImportOrder>(
        [
            new ImportOrder("O-1", "Ada", [new WireLine("A", 1), new WireLine("B", 2)]),
            new ImportOrder("O-2", "Grace", [new WireLine("C", 3)]),
        ]);
        var selectorCalls = 0;
        var combineCalls = 0;

        var result = records.Expand(
            record =>
            {
                selectorCalls++;
                return record.Lines;
            },
            (record, line, _) =>
            {
                combineCalls++;
                return $"{record.OrderId}:{line.Sku}";
            });

        True(result.TryGetValue(out var rows), "ledger expansion must succeed");
        Equal(3, rows!.Count, "ledger expansion row count");
        Equal(2, selectorCalls, "ledger child selector calls");
        Equal(3, combineCalls, "ledger combine calls");
        Equal(1, records.GetEnumeratorCalls, "ledger source GetEnumerator");
        Equal(3, records.MoveNextCalls, "ledger source MoveNext");
        Equal(2, records.CurrentReads, "ledger source Current");
        Equal(1, records.DisposeCalls, "ledger source Dispose");
    }

    private static void VerifyCombineLedger()
    {
        var records = new[]
        {
            new ImportOrder("O-1", "Ada", [new WireLine("A", 1)]),
            new ImportOrder("O-2", "Grace", null!),
            new ImportOrder("O-3", "Linus", [new WireLine("C", 3)]),
        };
        var selectorCalls = 0;
        var combineCalls = 0;

        var result = records.Expand(
            record =>
            {
                selectorCalls++;
                return record.Lines;
            },
            (record, line, _) =>
            {
                combineCalls++;
                return $"{record.OrderId}:{line.Sku}";
            });

        True(result.TryGetErrors(out var errors), "ledger failure must be reported");
        Equal(1, errors!.Count, "ledger failure count");
        Equal(3, selectorCalls, "later records must still be checked for null children");
        Equal(1, combineCalls, "combine must stop after the first recorded error");

        var alignedFirst = new[]
        {
            new ImportOrder("O-1", "Ada", [new WireLine("A", 1), new WireLine("B", 2)]),
            new ImportOrder("O-2", "Grace", [new WireLine("C", 3)]),
        };
        var firstSelectorCalls = 0;
        var alignedCombines = 0;
        var aligned = alignedFirst.ExpandAligned(
            record =>
            {
                firstSelectorCalls++;
                return record.Lines;
            },
            static _ => new[] { 10, 20 },
            (record, line, value, _) =>
            {
                alignedCombines++;
                return $"{record.OrderId}:{line.Sku}:{value}";
            });

        True(aligned.TryGetErrors(out var alignedErrors), "aligned ledger must fail");
        Equal(1, alignedErrors!.Count, "aligned ledger failure count");
        Equal(2, firstSelectorCalls, "aligned later record selectors must still run");
        Equal(2, alignedCombines, "aligned combine must stop after the first error");
    }

    private static ExpansionFailure MapExpansion(NestedRowError error) =>
        new(error.Kind, error.RecordIndex, error.FirstElementCount, error.SecondElementCount);

    private static Exception Capture(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            return exception;
        }

        throw new InvalidOperationException("The operation was expected to throw.");
    }

    private static void Equal<T>(
        IReadOnlyList<T> expected,
        IReadOnlyList<T> actual,
        string description)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException(
                $"{description}: expected [{string.Join(", ", expected)}] but observed "
                + $"[{string.Join(", ", actual)}].");
        }
    }

    private static void Equal(int expected, int actual, string description)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException(
                $"{description}: expected {expected} but observed {actual}.");
        }
    }

    private static void True(bool condition, string description)
    {
        if (!condition)
        {
            throw new InvalidOperationException(description);
        }
    }
}
