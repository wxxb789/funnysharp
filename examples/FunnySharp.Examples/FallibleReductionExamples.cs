using FunnySharp;

internal static class FallibleReductionExamples
{
    internal static void Verify()
    {
        var seed = new ExampleTotal(0, 0m);

        var empty = Array.Empty<ExampleLine>().AggregateResult(seed, AddLine);
        ExampleAssertions.True(
            empty.TryGetValue(out var emptyTotal) && emptyTotal == seed,
            "An empty source must return the supplied seed.");

        var lines = new[]
        {
            new ExampleLine("SKU-BOOK", 2, 12.5m),
            new ExampleLine("SKU-PEN", 1, 4m),
        };
        var total = lines.AggregateResult(seed, AddLine);

        ExampleAssertions.True(total.TryGetValue(out var parsed), "Every valid line must be accumulated.");
        ExampleAssertions.Equal(new ExampleTotal(2, 29m), parsed);

        var invalid = new[]
        {
            new ExampleLine("SKU-BOOK", 2, 12.5m),
            new ExampleLine("SKU-BAD", 0, 9m),
            new ExampleLine("SKU-NOTE", 3, 7.5m),
        };
        var visited = new List<string>();
        var failed = invalid.AggregateResult(
            seed,
            (current, line) =>
            {
                visited.Add(line.Sku);
                return AddLine(current, line);
            });

        ExampleAssertions.True(failed.TryGetError(out var error), "The invalid quantity must fail the fold.");
        ExampleAssertions.Equal(new ExampleError("quantity", "SKU-BAD|0"), error!);
        ExampleAssertions.SequenceEqual(["SKU-BOOK", "SKU-BAD"], visited);

        var mutable = new ExampleMutableTotal();
        var mutableFailure = invalid.AggregateResult(
            mutable,
            (current, line) =>
            {
                current.Add(line);
                return line.Quantity > 0
                    ? Result<ExampleMutableTotal, ExampleError>.Success(current)
                    : Result<ExampleMutableTotal, ExampleError>.Failure(
                        new ExampleError("quantity", $"{line.Sku}|{line.Quantity}"));
            });

        ExampleAssertions.True(
            mutableFailure.TryGetError(out _),
            "The mutable accumulator fold must fail on the invalid line.");
        ExampleAssertions.Equal(2, mutable.Lines.Count);
        ExampleAssertions.Equal(2, mutable.Lines[0].Quantity);
        ExampleAssertions.Equal(0, mutable.Lines[1].Quantity);
    }

    internal static async Task VerifyAsync()
    {
        var seed = new ExampleTotal(0, 0m);
        var lines = new[]
        {
            new ExampleLine("SKU-BOOK", 2, 12.5m),
            new ExampleLine("SKU-PEN", 1, 4m),
        };

        using var cancellationSource = new CancellationTokenSource();
        var synchronousStep = await ToAsync(lines).AggregateResultAsync(
            seed,
            AddLine,
            cancellationSource.Token);
        var valueTaskStep = await ToAsync(lines).AggregateResultValueAsync(
            seed,
            AddLineValueAsync,
            cancellationSource.Token);

        ExampleAssertions.True(
            synchronousStep.TryGetValue(out var syncTotal),
            "The asynchronous source fold must succeed.");
        ExampleAssertions.Equal(new ExampleTotal(2, 29m), syncTotal);
        ExampleAssertions.True(
            valueTaskStep.TryGetValue(out var valueTotal),
            "The value-task asynchronous fold must succeed.");
        ExampleAssertions.Equal(new ExampleTotal(2, 29m), valueTotal);

        var invalid = new[]
        {
            new ExampleLine("SKU-BOOK", 2, 12.5m),
            new ExampleLine("SKU-BAD", 0, 9m),
            new ExampleLine("SKU-NOTE", 3, 7.5m),
        };
        var failed = await ToAsync(invalid).AggregateResultAsync(
            seed,
            AddLine,
            cancellationSource.Token);

        ExampleAssertions.True(
            failed.TryGetError(out var error),
            "The asynchronous fold must return the first domain failure.");
        ExampleAssertions.Equal(new ExampleError("quantity", "SKU-BAD|0"), error!);
    }

    private static Result<ExampleTotal, ExampleError> AddLine(
        ExampleTotal total,
        ExampleLine line)
    {
        if (line.Quantity <= 0)
        {
            return Result<ExampleTotal, ExampleError>.Failure(
                new ExampleError("quantity", $"{line.Sku}|{line.Quantity}"));
        }

        return Result<ExampleTotal, ExampleError>.Success(total.Add(line));
    }

    private static ValueTask<Result<ExampleTotal, ExampleError>> AddLineValueAsync(
        ExampleTotal total,
        ExampleLine line) =>
        ValueTask.FromResult(AddLine(total, line));

    private static async IAsyncEnumerable<ExampleLine> ToAsync(
        IEnumerable<ExampleLine> lines)
    {
        foreach (var line in lines)
        {
            await Task.Yield();
            yield return line;
        }
    }

    private readonly record struct ExampleLine(string Sku, int Quantity, decimal UnitPrice)
    {
        internal decimal Amount => Quantity * UnitPrice;
    }

    private readonly record struct ExampleTotal(int Count, decimal Amount)
    {
        internal ExampleTotal Add(ExampleLine line) =>
            new(Count + 1, Amount + line.Amount);
    }

    private sealed record ExampleError(string Code, string Detail);

    private sealed class ExampleMutableTotal
    {
        internal List<ExampleLine> Lines { get; } = [];

        internal void Add(ExampleLine line) => Lines.Add(line);
    }
}
