#pragma warning disable FS0017

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using FunnySharp;

namespace FunnySharp.TestSupport.Decoding;

internal static class DecoderMeasurements
{
    private const int WarmupIterations = 500;
    private const int ValidIterations = 20_000;
    private const int InvalidIterations = 10_000;
    private const int PathIterations = 20_000;
    private const int Rounds = 3;

    internal static void Run()
    {
        Console.WriteLine($"runtime: {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"os: {RuntimeInformation.OSDescription}");
        Console.WriteLine($"cores: {Environment.ProcessorCount}");
        Console.WriteLine();

        using var validDocument = JsonDocument.Parse(OrderFixtures.Valid.Json);
        using var invalidDocument = JsonDocument.Parse(OrderFixtures.Invalid.Json);
        var validRoot = validDocument.RootElement;
        var invalidRoot = invalidDocument.RootElement;

        Console.WriteLine("| Implementation | Case | Allocated B/op | Time ns/op |");
        Console.WriteLine("| --- | --- | ---: | ---: |");
        foreach (var (name, decode) in OrderComparison.Consumers)
        {
            var valid = Measure(() => decode(validRoot), ValidIterations);
            Console.WriteLine(
                $"| {name} | valid | {Format(valid.BytesPerOperation)} | " +
                $"{Format(valid.NanosecondsPerOperation)} |");

            var invalid = Measure(() => decode(invalidRoot), InvalidIterations);
            Console.WriteLine(
                $"| {name} | invalid | {Format(invalid.BytesPerOperation)} | " +
                $"{Format(invalid.NanosecondsPerOperation)} |");
        }

        Console.WriteLine();
        Console.WriteLine(
            "| Path depth | Construction B/op | Construction ns/op | " +
            "Render-inclusive B/op | Render-inclusive ns/op |");
        Console.WriteLine("| --- | ---: | ---: | ---: | ---: |");
        foreach (var depth in new[] { 1, 2, 4, 8, 16 })
        {
            var construction = MeasurePath(depth, render: false);
            var render = MeasurePath(depth, render: true);
            Console.WriteLine(
                $"| {depth} | {Format(construction.BytesPerOperation)} | " +
                $"{Format(construction.NanosecondsPerOperation)} | " +
                $"{Format(render.BytesPerOperation)} | " +
                $"{Format(render.NanosecondsPerOperation)} |");
        }

        Console.WriteLine();
        Console.WriteLine(
            "Allocations are per complete mapped operation. Timing is directional on this host.");
    }

    private static Measurement Measure(Func<OrderDecodeResult> operation, int iterations) =>
        MeasureOperation(
            () =>
            {
                var result = operation();
                return result.IsSuccess ? 1 : result.Errors.Count;
            },
            iterations);

    private static Measurement MeasurePath(int depth, bool render) =>
        MeasureOperation(
            () =>
            {
                var location = BuildPath(depth);
                return render ? location.ToString().Length : location.GetHashCode();
            },
            PathIterations);

    private static Measurement MeasureOperation(Func<int> operation, int iterations)
    {
        for (var index = 0; index < WarmupIterations; index++)
        {
            GC.KeepAlive(operation());
        }

        var bytesPerOperation = double.MaxValue;
        var nanosecondsPerOperation = double.MaxValue;

        for (var round = 0; round < Rounds; round++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var bytesBefore = GC.GetAllocatedBytesForCurrentThread();
            var checksum = 0;
            var stopwatch = Stopwatch.StartNew();
            for (var index = 0; index < iterations; index++)
            {
                checksum += operation();
            }

            stopwatch.Stop();
            var allocated = GC.GetAllocatedBytesForCurrentThread() - bytesBefore;
            GC.KeepAlive(checksum);

            bytesPerOperation = Math.Min(
                bytesPerOperation,
                allocated / (double)iterations);
            nanosecondsPerOperation = Math.Min(
                nanosecondsPerOperation,
                stopwatch.Elapsed.TotalMilliseconds * 1_000_000d / iterations);
        }

        return new Measurement(bytesPerOperation, nanosecondsPerOperation);
    }

    private static Location BuildPath(int depth)
    {
        var location = Location.Root;
        for (var index = 0; index < depth; index++)
        {
            location = index % 2 == 0
                ? location.Property("p")
                : location.At(index);
        }

        return location;
    }

    private static string Format(double value) =>
        value.ToString("F1", CultureInfo.InvariantCulture);

    private readonly record struct Measurement(
        double BytesPerOperation,
        double NanosecondsPerOperation);
}
