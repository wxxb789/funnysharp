using System.Globalization;
using System.Runtime.InteropServices;
using FunnySharp;
using static FunnySharp.TestSupport.MeasurementHarness;

namespace FunnySharp.TestSupport.Checks;

internal static class CheckMeasurements
{
    private const int WarmupIterations = 2_000;
    private const int SyncIterations = 200_000;
    private const int AsyncIterations = 50_000;

    internal static async Task RunAsync()
    {
        Console.WriteLine($"runtime: {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"os: {RuntimeInformation.OSDescription}");
        Console.WriteLine($"cores: {Environment.ProcessorCount}");
        Console.WriteLine();

        CheckComparison.Verify();
        await CheckComparison.VerifyAsync();

        var rules = new CheckRuleSet();
        var syncRules = rules.SyncRules();
        var taskRules = rules.TaskRules();
        var valueRules = rules.ValueRules();
        var validInput = Result<CheckOrder, CheckError>.Success(CheckFixtures.ValidOrder);
        var failingInput = Result<CheckOrder, CheckError>.Success(CheckFixtures.MultipleFailures.Order);
        var failedInput = Result<CheckOrder, CheckError>.Failure(
            new CheckError("existing", "already failed"));
        var emptyRules = Array.Empty<Func<CheckOrder, UnitResult<CheckError>>>();
        var emptyTaskRules = Array.Empty<Func<CheckOrder, Task<UnitResult<CheckError>>>>();
        var emptyValueRules = Array.Empty<Func<CheckOrder, ValueTask<UnitResult<CheckError>>>>();
        var stock = syncRules[0];
        var permission = syncRules[1];
        var fraud = syncRules[2];
        var stockAsync = taskRules[0];
        var permissionAsync = taskRules[1];
        var fraudAsync = taskRules[2];

        Console.WriteLine("| Operation | Allocated B/op | Time ns/op |");
        Console.WriteLine("| --- | ---: | ---: |");
        Write(
            "Fail-fast, current Bind + UnitResult.ToResult, all pass",
            Measure(
                () => Digest(OrdinaryCheckConsumer.FailFast(
                    validInput,
                    stock,
                    permission,
                    fraud)),
                SyncIterations));
        Write(
            "Fail-fast, Check, all pass",
            Measure(
                () => Digest(FunnySharpCheckConsumer.FailFast(
                    validInput,
                    stock,
                    permission,
                    fraud)),
                SyncIterations));
        Write(
            "Fail-fast, current Bind + UnitResult.ToResult, first fails",
            Measure(
                () => Digest(OrdinaryCheckConsumer.FailFast(
                    failingInput,
                    stock,
                    permission,
                    fraud)),
                SyncIterations));
        Write(
            "Fail-fast, Check, first fails",
            Measure(
                () => Digest(FunnySharpCheckConsumer.FailFast(
                    failingInput,
                    stock,
                    permission,
                    fraud)),
                SyncIterations));
        Write(
            "Accumulate, current Validation.Zip, all pass",
            Measure(
                () => Digest(OrdinaryCheckConsumer.Accumulate(validInput, syncRules)),
                SyncIterations));
        Write(
            "Accumulate, CheckAll, all pass",
            Measure(
                () => Digest(FunnySharpCheckConsumer.Accumulate(validInput, syncRules)),
                SyncIterations));
        Write(
            "Accumulate, current Validation.Zip, all fail",
            Measure(
                () => Digest(OrdinaryCheckConsumer.Accumulate(failingInput, syncRules)),
                SyncIterations));
        Write(
            "Accumulate, CheckAll, all fail",
            Measure(
                () => Digest(FunnySharpCheckConsumer.Accumulate(failingInput, syncRules)),
                SyncIterations));
        Write(
            "Accumulate, current Validation.Zip, empty rules",
            Measure(
                () => Digest(OrdinaryCheckConsumer.Accumulate(validInput, emptyRules)),
                SyncIterations));
        Write(
            "Accumulate, CheckAll, empty rules",
            Measure(
                () => Digest(FunnySharpCheckConsumer.Accumulate(validInput, emptyRules)),
                SyncIterations));
        Write(
            "Accumulate, current Validation.Zip, already failed",
            Measure(
                () => Digest(OrdinaryCheckConsumer.Accumulate(failedInput, syncRules)),
                SyncIterations));
        Write(
            "Accumulate, CheckAll, already failed",
            Measure(
                () => Digest(FunnySharpCheckConsumer.Accumulate(failedInput, syncRules)),
                SyncIterations));
        Write(
            "Rule array with three reused delegates",
            Measure(
                () =>
                {
                    var snapshot = new[] { stock, permission, fraud };
                    return snapshot.Length;
                },
                SyncIterations));
        Write(
            "Three-rule delegate snapshot",
            Measure(() => rules.SyncRules().Length, SyncIterations));

        Console.WriteLine();
        Console.WriteLine("| Async operation | Allocated B/op | Time ns/op |");
        Console.WriteLine("| --- | ---: | ---: |");
        Write(
            "Fail-fast, current BindAsync + ToResultAsync, all pass",
            await MeasureAsync(
                async () => Digest(await OrdinaryCheckConsumer.FailFastAsync(
                    validInput,
                    stockAsync,
                    permissionAsync,
                    fraudAsync)),
                AsyncIterations));
        Write(
            "Fail-fast, CheckAsync, all pass",
            await MeasureAsync(
                async () => Digest(await FunnySharpCheckConsumer.FailFastAsync(
                    validInput,
                    stockAsync,
                    permissionAsync,
                    fraudAsync)),
                AsyncIterations));
        Write(
            "Accumulate, current await + Validation.Zip, all pass",
            await MeasureAsync(
                async () => Digest(await OrdinaryCheckConsumer.AccumulateAsync(
                    validInput,
                    taskRules)),
                AsyncIterations));
        Write(
            "Accumulate, CheckAllAsync, all pass",
            await MeasureAsync(
                async () => Digest(await FunnySharpCheckConsumer.AccumulateAsync(
                    validInput,
                    taskRules)),
                AsyncIterations));
        Write(
            "Accumulate, value-task current await + Validation.Zip, all pass",
            await MeasureAsync(
                async () => Digest(await OrdinaryCheckConsumer.AccumulateValueAsync(
                    validInput,
                    valueRules)),
                AsyncIterations));
        Write(
            "Accumulate, CheckAllValueAsync, all pass",
            await MeasureAsync(
                async () => Digest(await FunnySharpCheckConsumer.AccumulateValueAsync(
                    validInput,
                    valueRules)),
                AsyncIterations));
        Write(
            "Accumulate, current await + Validation.Zip, empty rules",
            await MeasureAsync(
                async () => Digest(await OrdinaryCheckConsumer.AccumulateAsync(
                    validInput,
                    emptyTaskRules)),
                AsyncIterations));
        Write(
            "Accumulate, CheckAllAsync, empty rules",
            await MeasureAsync(
                async () => Digest(await FunnySharpCheckConsumer.AccumulateAsync(
                    validInput,
                    emptyTaskRules)),
                AsyncIterations));
        Write(
            "Accumulate, CheckAllValueAsync, empty rules",
            await MeasureAsync(
                async () => Digest(await FunnySharpCheckConsumer.AccumulateValueAsync(
                    validInput,
                    emptyValueRules)),
                AsyncIterations));
        Write(
            "Accumulate, current await + Validation.Zip, already failed",
            await MeasureAsync(
                async () => Digest(await OrdinaryCheckConsumer.AccumulateAsync(
                    failedInput,
                    taskRules)),
                AsyncIterations));
        Write(
            "Accumulate, CheckAllAsync, already failed",
            await MeasureAsync(
                async () => Digest(await FunnySharpCheckConsumer.AccumulateAsync(
                    failedInput,
                    taskRules)),
                AsyncIterations));

        Console.WriteLine();
        Console.WriteLine(
            "Allocations are per complete operation, including the rule snapshot and any error storage. "
            + "Timing is directional on this host.");
    }

    private static int Digest(Result<CheckOrder, CheckError> result) =>
        result.Match(
            static order => order.Quantity,
            static error => -error.Code.Length);

    private static int Digest(Validation<CheckOrder, CheckError> validation) =>
        validation.Match(
            static order => order.Quantity,
            static errors => -errors.Count);

    private static Measurement Measure(Func<int> operation, int iterations) =>
        MeasurementHarness.Measure(operation, iterations, WarmupIterations);

    private static Task<Measurement> MeasureAsync(
        Func<Task<int>> operation,
        int iterations) =>
        MeasurementHarness.MeasureAsync(operation, iterations, WarmupIterations);

    private static void Write(string name, Measurement measurement) =>
        Console.WriteLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $"| {name} | {Format(measurement.BytesPerOperation)} | "
                + $"{Format(measurement.NanosecondsPerOperation)} |"));
}
