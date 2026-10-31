using System.Diagnostics;
using System.Globalization;

namespace FunnySharp.TestSupport;

internal readonly record struct Measurement(
    double BytesPerOperation,
    double NanosecondsPerOperation);

internal static class MeasurementHarness
{
    private const int Rounds = 3;

    internal static Measurement Measure(Func<int> operation, int iterations, int warmupIterations)
    {
        for (var index = 0; index < warmupIterations; index++)
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

            bytesPerOperation = Math.Min(bytesPerOperation, allocated / (double)iterations);
            nanosecondsPerOperation = Math.Min(
                nanosecondsPerOperation,
                stopwatch.Elapsed.TotalMilliseconds * 1_000_000d / iterations);
        }

        return new Measurement(bytesPerOperation, nanosecondsPerOperation);
    }

    internal static string Format(double value) =>
        value.ToString("F1", CultureInfo.InvariantCulture);
}
