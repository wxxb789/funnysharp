namespace FunnySharp.TestSupport.FallibleReduction;

internal static class ReductionRules
{
    internal const int MaximumQuantity = 100;

    internal static bool IsValid(ReductionLine line) =>
        line.Quantity is > 0 and <= MaximumQuantity;

    internal static Result<ReductionTotal, ReductionError> Add(
        ReductionTotal total,
        ReductionLine line)
    {
        if (line.Quantity <= 0)
        {
            return Result<ReductionTotal, ReductionError>.Failure(
                new ReductionError("quantity", $"{line.Sku}|{line.Quantity}"));
        }

        if (line.Quantity > MaximumQuantity)
        {
            return Result<ReductionTotal, ReductionError>.Failure(
                new ReductionError("quantity-limit", $"{line.Sku}|{line.Quantity}"));
        }

        return Result<ReductionTotal, ReductionError>.Success(total.Add(line));
    }

    internal static ValueTask<Result<ReductionTotal, ReductionError>> AddValueAsync(
        ReductionTotal total,
        ReductionLine line) =>
        ValueTask.FromResult(Add(total, line));

    internal static Result<MutableReductionTotal, ReductionError> AddMutable(
        MutableReductionTotal total,
        ReductionLine line)
    {
        if (!IsValid(line))
        {
            return Result<MutableReductionTotal, ReductionError>.Failure(
                new ReductionError("quantity", $"{line.Sku}|{line.Quantity}"));
        }

        total.Add(line);
        return Result<MutableReductionTotal, ReductionError>.Success(total);
    }

    internal static int ReachedCount(IReadOnlyList<ReductionLine> lines)
    {
        for (var index = 0; index < lines.Count; index++)
        {
            if (!IsValid(lines[index]))
            {
                return index + 1;
            }
        }

        return lines.Count;
    }

    internal static (int Reached, int StoppingMoveNext, bool Completed) Expectations(
        IReadOnlyList<ReductionLine> lines)
    {
        var reached = ReachedCount(lines);
        var completed = reached == lines.Count;
        return (reached, completed ? lines.Count + 1 : reached, completed);
    }
}
