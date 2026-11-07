using System.Runtime.InteropServices;
using FunnySharp;
using static FunnySharp.TestSupport.MeasurementHarness;

namespace FunnySharp.TestSupport.FallibleReduction;

internal static class FallibleReductionMeasurements
{
    private const int WarmupIterations = 2_000;
    private const int SyncIterations = 100_000;
    private const int AsyncIterations = 20_000;

    private static readonly Func<long, int, Result<long, string>> AddLongDelegate = AddLong;

    private static readonly Func<long, int, ValueTask<Result<long, string>>> AddLongValueDelegate =
        AddLongValueAsync;

    private static readonly Func<long, int, CancellationToken, ValueTask<Result<long, string>>>
        AddLongTokenDelegate = AddLongTokenAsync;

    private static readonly Func<LargeTotal, int, Result<LargeTotal, string>> AddLargeDelegate = AddLarge;

    private static readonly Func<ListAccumulator, int, Result<ListAccumulator, string>> AddListDelegate =
        AddList;

    internal static async Task RunAsync()
    {
        Console.WriteLine($"runtime: {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"os: {RuntimeInformation.OSDescription}");
        Console.WriteLine($"cores: {Environment.ProcessorCount}");
        Console.WriteLine();

        ReductionComparison.Verify();
        await ReductionComparison.VerifyAsync().ConfigureAwait(false);

        var success = CreateValues(1024, failingIndex: null);
        var failure = CreateValues(1024, failingIndex: 8);
        var small = CreateValues(16, failingIndex: null);
        var empty = Array.Empty<int>();
        var successList = success.ToList();
        using var cancellationSource = new CancellationTokenSource();
        var token = cancellationSource.Token;

        await WriteConsumption(success, failure, token).ConfigureAwait(false);

        // Measure one row before the reported table so the first reported row runs at the same tier.
        _ = Measure(
            () => Digest(success.AggregateResult(0L, AddLongDelegate)),
            SyncIterations,
            WarmupIterations);

        Console.WriteLine("## Synchronous allocation and directional timing");
        Console.WriteLine();
        Console.WriteLine("| Operation | Allocated B/op | Time ns/op |");
        Console.WriteLine("| --- | ---: | ---: |");
        WriteRow(
            "AggregateResult, long, success N=1024",
            Measure(() => Digest(success.AggregateResult(0L, AddLongDelegate)), SyncIterations, WarmupIterations));
        WriteRow(
            "foreach reference, long, success N=1024",
            Measure(() => Digest(OrdinaryReductionConsumer.Run(success, 0L, AddLongDelegate)), SyncIterations, WarmupIterations));
        WriteRow(
            "AggregateResult, long, List source, success N=1024",
            Measure(() => Digest(successList.AggregateResult(0L, AddLongDelegate)), SyncIterations, WarmupIterations));
        WriteRow(
            "foreach reference, long, List source, success N=1024",
            Measure(() => Digest(OrdinaryReductionConsumer.Run(successList, 0L, AddLongDelegate)), SyncIterations, WarmupIterations));
        WriteRow(
            "Aggregate over Result (Bind, natural), long, success N=1024",
            Measure(() => Digest(AggregateOverResultConsumer.Run(success, 0L, AddLongDelegate)), SyncIterations, WarmupIterations));
        WriteRow(
            "Aggregate over Result (helper, no per-item closure), long, success N=1024",
            Measure(
                () => Digest(AggregateLongHelper(success, AddLongDelegate)),
                SyncIterations,
                WarmupIterations));
        WriteRow(
            "AggregateResult, long, failure at 8, N=1024",
            Measure(() => Digest(failure.AggregateResult(0L, AddLongDelegate)), SyncIterations, WarmupIterations));
        WriteRow(
            "foreach reference, long, failure at 8, N=1024",
            Measure(() => Digest(OrdinaryReductionConsumer.Run(failure, 0L, AddLongDelegate)), SyncIterations, WarmupIterations));
        WriteRow(
            "Aggregate over Result (Bind, natural), long, failure at 8, N=1024",
            Measure(() => Digest(AggregateOverResultConsumer.Run(failure, 0L, AddLongDelegate)), SyncIterations, WarmupIterations));
        WriteRow(
            "Aggregate over Result (helper, no per-item closure), long, failure at 8, N=1024",
            Measure(
                () => Digest(AggregateLongHelper(failure, AddLongDelegate)),
                SyncIterations,
                WarmupIterations));
        WriteRow(
            "AggregateResult, long, empty",
            Measure(() => Digest(empty.AggregateResult(0L, AddLongDelegate)), SyncIterations, WarmupIterations));
        WriteRow(
            "AggregateResult, long, success N=16",
            Measure(() => Digest(small.AggregateResult(0L, AddLongDelegate)), SyncIterations, WarmupIterations));
        WriteRow(
            "AggregateResult, 64-byte value accumulator, success N=1024",
            Measure(
                () => DigestLarge(success.AggregateResult(new LargeTotal(), AddLargeDelegate)),
                SyncIterations,
                WarmupIterations));
        WriteRow(
            "foreach reference, 64-byte value accumulator, success N=1024",
            Measure(
                () => DigestLarge(OrdinaryReductionConsumer.Run(success, new LargeTotal(), AddLargeDelegate)),
                SyncIterations,
                WarmupIterations));
        WriteRow(
            "AggregateResult, mutable list accumulator, success N=1024",
            Measure(
                () => DigestList(success.AggregateResult(new ListAccumulator(), AddListDelegate)),
                SyncIterations,
                WarmupIterations));
        WriteRow(
            "foreach reference, mutable list accumulator, success N=1024",
            Measure(
                () => DigestList(OrdinaryReductionConsumer.Run(success, new ListAccumulator(), AddListDelegate)),
                SyncIterations,
                WarmupIterations));
        WriteRow(
            "AggregateResult, mutable list accumulator, failure at 8, N=1024",
            Measure(
                () => DigestList(failure.AggregateResult(new ListAccumulator(), AddListDelegate)),
                SyncIterations,
                WarmupIterations));

        var asyncSuccess = new AsyncArraySequence<int>(success);
        var asyncFailure = new AsyncArraySequence<int>(failure);
        var asyncEmpty = new AsyncArraySequence<int>(empty);

        Console.WriteLine();
        Console.WriteLine("## Asynchronous allocation and directional timing");
        Console.WriteLine();
        Console.WriteLine("| Operation | Allocated B/op | Time ns/op |");
        Console.WriteLine("| --- | ---: | ---: |");
        WriteRow(
            "AggregateResultAsync, sync step, success N=1024",
            await MeasureAsync(
                async () => Digest(await asyncSuccess.AggregateResultAsync(0L, AddLongDelegate, token)),
                AsyncIterations,
                WarmupIterations));
        WriteRow(
            "await foreach reference, sync step, success N=1024",
            await MeasureAsync(
                async () => Digest(await OrdinaryReductionConsumer.RunAsync(asyncSuccess, 0L, AddLongDelegate, token)),
                AsyncIterations,
                WarmupIterations));
        WriteRow(
            "AggregateAsync over Result, success N=1024",
            await MeasureAsync(
                async () => Digest(await AggregateOverResultConsumer.RunAsync(asyncSuccess, 0L, AddLongDelegate, token)),
                AsyncIterations,
                WarmupIterations));
        WriteRow(
            "AggregateResultAsync, sync step, failure at 8, N=1024",
            await MeasureAsync(
                async () => Digest(await asyncFailure.AggregateResultAsync(0L, AddLongDelegate, token)),
                AsyncIterations,
                WarmupIterations));
        WriteRow(
            "await foreach reference, sync step, failure at 8, N=1024",
            await MeasureAsync(
                async () => Digest(await OrdinaryReductionConsumer.RunAsync(asyncFailure, 0L, AddLongDelegate, token)),
                AsyncIterations,
                WarmupIterations));
        WriteRow(
            "AggregateAsync over Result, failure at 8, N=1024",
            await MeasureAsync(
                async () => Digest(await AggregateOverResultConsumer.RunAsync(asyncFailure, 0L, AddLongDelegate, token)),
                AsyncIterations,
                WarmupIterations));
        WriteRow(
            "AggregateResultAsync, sync step, empty",
            await MeasureAsync(
                async () => Digest(await asyncEmpty.AggregateResultAsync(0L, AddLongDelegate, token)),
                AsyncIterations,
                WarmupIterations));
        WriteRow(
            "await foreach reference, sync step, empty",
            await MeasureAsync(
                async () => Digest(await OrdinaryReductionConsumer.RunAsync(asyncEmpty, 0L, AddLongDelegate, token)),
                AsyncIterations,
                WarmupIterations));
        WriteRow(
            "AggregateResultValueAsync, tokenless ValueTask step, success N=1024",
            await MeasureAsync(
                async () => Digest(
                    await asyncSuccess.AggregateResultValueAsync(0L, AddLongValueDelegate, token)),
                AsyncIterations,
                WarmupIterations));
        WriteRow(
            "AggregateResultValueAsync, token-aware ValueTask step, success N=1024",
            await MeasureAsync(
                async () => Digest(
                    await asyncSuccess.AggregateResultValueAsync(0L, AddLongTokenDelegate, token)),
                AsyncIterations,
                WarmupIterations));
        WriteRow(
            "await foreach reference, ValueTask step, success N=1024",
            await MeasureAsync(
                async () => Digest(
                    await OrdinaryReductionConsumer.RunValueAsync(
                        asyncSuccess,
                        0L,
                        AddLongValueDelegate,
                        token)),
                AsyncIterations,
                WarmupIterations));
        WriteRow(
            "AggregateResultValueAsync, tokenless ValueTask step, failure at 8, N=1024",
            await MeasureAsync(
                async () => Digest(
                    await asyncFailure.AggregateResultValueAsync(0L, AddLongValueDelegate, token)),
                AsyncIterations,
                WarmupIterations));
        WriteRow(
            "AggregateResultValueAsync, token-aware ValueTask step, failure at 8, N=1024",
            await MeasureAsync(
                async () => Digest(
                    await asyncFailure.AggregateResultValueAsync(0L, AddLongTokenDelegate, token)),
                AsyncIterations,
                WarmupIterations));
        WriteRow(
            "await foreach reference, ValueTask step, failure at 8, N=1024",
            await MeasureAsync(
                async () => Digest(
                    await OrdinaryReductionConsumer.RunValueAsync(
                        asyncFailure,
                        0L,
                        AddLongValueDelegate,
                        token)),
                AsyncIterations,
                WarmupIterations));
    }

    private static async Task WriteConsumption(
        IReadOnlyList<int> success,
        IReadOnlyList<int> failure,
        CancellationToken token)
    {
        Console.WriteLine("## Synchronous consumption");
        Console.WriteLine();
        Console.WriteLine("| Consumer | Fixture | MoveNext | Current | Step | Dispose |");
        Console.WriteLine("| --- | --- | ---: | ---: | ---: | ---: |");
        WriteSyncConsumption(success, "success N=1024");
        WriteSyncConsumption(failure, "failure at 8, N=1024");

        Console.WriteLine();
        Console.WriteLine("## Asynchronous consumption");
        Console.WriteLine();
        Console.WriteLine("| Consumer | Fixture | MoveNextAsync | Current | Step | DisposeAsync |");
        Console.WriteLine("| --- | --- | ---: | ---: | ---: | ---: |");
        await WriteAsyncConsumption(success, "success N=1024", token).ConfigureAwait(false);
        await WriteAsyncConsumption(failure, "failure at 8, N=1024", token).ConfigureAwait(false);
    }

    private static void WriteSyncConsumption(IReadOnlyList<int> values, string fixture)
    {
        var steps = 0;
        Func<long, int, Result<long, string>> step = (total, value) =>
        {
            steps++;
            return AddLong(total, value);
        };

        var probe = new ReductionProbe<int>(values);
        _ = probe.AggregateResult(0L, step);
        WriteSyncConsumptionRow("AggregateResult", fixture, probe, steps);

        steps = 0;
        probe = new ReductionProbe<int>(values);
        _ = OrdinaryReductionConsumer.Run(probe, 0L, step);
        WriteSyncConsumptionRow("foreach reference", fixture, probe, steps);

        steps = 0;
        probe = new ReductionProbe<int>(values);
        _ = AggregateOverResultConsumer.Run(probe, 0L, step);
        WriteSyncConsumptionRow("Aggregate over Result", fixture, probe, steps);
    }

    private static void WriteSyncConsumptionRow(
        string consumer,
        string fixture,
        ReductionProbe<int> probe,
        int steps)
    {
        Console.WriteLine(
            $"| {consumer} | {fixture} | {probe.MoveNextCalls} | {probe.CurrentReads} | {steps} | {probe.DisposeCalls} |");
    }

    private static async Task WriteAsyncConsumption(
        IReadOnlyList<int> values,
        string fixture,
        CancellationToken token)
    {
        var steps = 0;
        Func<long, int, Result<long, string>> step = (total, value) =>
        {
            steps++;
            return AddLong(total, value);
        };

        var probe = new AsyncReductionProbe<int>(values);
        _ = await probe.AggregateResultAsync(0L, step, token).ConfigureAwait(false);
        WriteAsyncConsumptionRow("AggregateResultAsync", fixture, probe, steps);

        steps = 0;
        probe = new AsyncReductionProbe<int>(values);
        _ = await OrdinaryReductionConsumer.RunAsync(probe, 0L, step, token).ConfigureAwait(false);
        WriteAsyncConsumptionRow("await foreach reference", fixture, probe, steps);

        steps = 0;
        probe = new AsyncReductionProbe<int>(values);
        _ = await AggregateOverResultConsumer.RunAsync(probe, 0L, step, token).ConfigureAwait(false);
        WriteAsyncConsumptionRow("AggregateAsync over Result", fixture, probe, steps);
    }

    private static void WriteAsyncConsumptionRow(
        string consumer,
        string fixture,
        AsyncReductionProbe<int> probe,
        int steps)
    {
        Console.WriteLine(
            $"| {consumer} | {fixture} | {probe.MoveNextAsyncCalls} | {probe.CurrentReads} | {steps} | {probe.DisposeAsyncCalls} |");
    }

    private static void WriteRow(string name, Measurement measurement)
    {
        Console.WriteLine(
            $"| {name} | {Format(measurement.BytesPerOperation)} | {Format(measurement.NanosecondsPerOperation)} |");
    }

    private static int[] CreateValues(int count, int? failingIndex)
    {
        var values = new int[count];

        for (var index = 0; index < count; index++)
        {
            values[index] = index + 1;
        }

        if (failingIndex is not null)
        {
            values[failingIndex.Value] = 0;
        }

        return values;
    }

    private static Result<long, string> AddLong(long total, int value) =>
        value == 0
            ? Result<long, string>.Failure("zero")
            : Result<long, string>.Success(total + value);

    private static ValueTask<Result<long, string>> AddLongValueAsync(long total, int value) =>
        ValueTask.FromResult(AddLong(total, value));

    private static ValueTask<Result<long, string>> AddLongTokenAsync(
        long total,
        int value,
        CancellationToken _) =>
        ValueTask.FromResult(AddLong(total, value));

    private static Result<LargeTotal, string> AddLarge(LargeTotal total, int value) =>
        value == 0
            ? Result<LargeTotal, string>.Failure("zero")
            : Result<LargeTotal, string>.Success(total.Add(value));

    private static Result<ListAccumulator, string> AddList(ListAccumulator total, int value)
    {
        if (value == 0)
        {
            return Result<ListAccumulator, string>.Failure("zero");
        }

        total.Add(value);
        return Result<ListAccumulator, string>.Success(total);
    }

    private static Result<long, string> AggregateLongHelper(
        IEnumerable<int> source,
        Func<long, int, Result<long, string>> step) =>
        source.Aggregate(
            Result<long, string>.Success(0L),
            (accumulator, value) => BindLong(accumulator, value, step));

    private static Result<long, string> BindLong(
        Result<long, string> result,
        int value,
        Func<long, int, Result<long, string>> step) =>
        result.TryGetValue(out var total) ? step(total, value) : result;

    private static int Digest(Result<long, string> result) =>
        result.TryGetValue(out var value) ? (int)(value % int.MaxValue) : -1;

    private static int DigestLarge(Result<LargeTotal, string> result) =>
        result.TryGetValue(out var value) ? value.Checksum : -1;

    private static int DigestList(Result<ListAccumulator, string> result) =>
        result.TryGetValue(out var value) ? (int)(value.Sum + value.Values.Count) : -1;

    private readonly record struct LargeTotal(
        long A,
        long B,
        long C,
        long D,
        long E,
        long F,
        long G,
        long H)
    {
        internal LargeTotal Add(long value) =>
            new(A + value, B + value, C + value, D + value, E + value, F + value, G + value, H + value);

        internal int Checksum => (int)((A ^ B ^ C ^ D ^ E ^ F ^ G ^ H) % int.MaxValue);
    }

    private sealed class ListAccumulator
    {
        internal List<int> Values { get; } = [];

        internal long Sum { get; private set; }

        internal void Add(int value)
        {
            Values.Add(value);
            Sum += value;
        }
    }

}
