using System.Runtime.InteropServices;
using System.Text.Json;
using FunnySharp;
using static FunnySharp.TestSupport.MeasurementHarness;

namespace FunnySharp.TestSupport.Patching;

internal static class PatchMeasurements
{
    private const int WarmupIterations = 500;
    private const int ConsumerIterations = 20_000;
    private const int ApplyIterations = 50_000;

    private const string ThreeFieldJson =
        """{"timeZone":"Europe/Berlin","closesAt":"18:30","note":"updated"}""";

    private const string OneFieldJson = """{"timeZone":"Europe/Berlin"}""";

    internal static void Run()
    {
        Console.WriteLine($"runtime: {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"os: {RuntimeInformation.OSDescription}");
        Console.WriteLine($"cores: {Environment.ProcessorCount}");
        Console.WriteLine();

        using var threeFieldDocument = JsonDocument.Parse(ThreeFieldJson);
        using var oneFieldDocument = JsonDocument.Parse(OneFieldJson);
        var threeFieldRoot = threeFieldDocument.RootElement;
        var oneFieldRoot = oneFieldDocument.RootElement;
        var current = ScheduleRules.ValidSchedule;

        var threeFieldPatch = DecodeOrThrow(threeFieldRoot);
        var oneFieldPatch = DecodeOrThrow(oneFieldRoot);

        Console.WriteLine("| Operation | Changed fields | Allocated B/op | Time ns/op |");
        Console.WriteLine("| --- | ---: | ---: | ---: |");

        Write(
            "Patch consumer: decode + validate + apply",
            3,
            Measure(() =>
            {
                var result = PatchScheduleConsumer.Update(threeFieldRoot, current);
                return result.IsSuccess ? 1 : result.Errors.Count;
            }, ConsumerIterations));
        Write(
            "Ordinary consumer: parse + validate + apply",
            3,
            Measure(() =>
            {
                var result = OrdinaryScheduleConsumer.Update(threeFieldRoot, current);
                return result.IsSuccess ? 1 : result.Errors.Count;
            }, ConsumerIterations));
        Write(
            "Patch validate + apply (predecoded)",
            3,
            Measure(() =>
            {
                var validated = ScheduleRules.Schema.Validate(threeFieldPatch, current);
                return validated.IsValid ? 1 : 0;
            }, ApplyIterations));
        Write(
            "Patch apply only (predecoded)",
            3,
            Measure(() =>
            {
                var application = threeFieldPatch.ApplyTo(current);
                return application.IsIdentity ? 0 : 1;
            }, ApplyIterations));
        Write(
            "Patch apply + inspect changes",
            3,
            Measure(
                () => threeFieldPatch.ApplyTo(current).Changes.Count,
                ApplyIterations));
        Write(
            "Ordinary direct with expression",
            3,
            Measure(
                () => (current with
                {
                    TimeZone = "Europe/Berlin",
                    ClosesAt = new TimeOnly(18, 30),
                    Note = Option.Some("updated"),
                }).GetHashCode(),
                ApplyIterations));

        Write(
            "Patch apply only (one field)",
            1,
            Measure(() =>
            {
                var application = oneFieldPatch.ApplyTo(current);
                return application.IsIdentity ? 0 : 1;
            }, ApplyIterations));
        Write(
            "Ordinary direct with expression (one field)",
            1,
            Measure(
                () => (current with { TimeZone = "Europe/Berlin" }).GetHashCode(),
                ApplyIterations));

        Write(
            "Patch apply only (empty)",
            0,
            Measure(() =>
            {
                var application = Patch<Schedule>.Empty.ApplyTo(current);
                return application.IsIdentity ? 0 : 1;
            }, ApplyIterations));
        Write(
            "Ordinary identity (empty)",
            0,
            Measure(() => current.GetHashCode(), ApplyIterations));

        Console.WriteLine();
        Console.WriteLine(
            "Allocations are per complete operation on this host. Timing is directional. " +
            "Patch copies equal the number of changed fields because each immutable lens setter " +
            "produces one candidate; overlapping setters are not merged.");
    }

    private static Measurement Measure(Func<int> operation, int iterations) =>
        MeasurementHarness.Measure(operation, iterations, WarmupIterations);

    private static void Write(string operation, int changedFields, Measurement measurement) =>
        Console.WriteLine(
            $"| {operation} | {changedFields} | {Format(measurement.BytesPerOperation)} | " +
            $"{Format(measurement.NanosecondsPerOperation)} |");

    private static Patch<Schedule> DecodeOrThrow(JsonElement root)
    {
        var decoded = ScheduleRules.Schema.Decode(root);
        if (!decoded.TryGetValue(out var patch))
        {
            decoded.TryGetErrors(out var errors);
            throw new InvalidOperationException(
                string.Join("; ", errors!.Select(static error => error.ToString())));
        }

        return patch;
    }
}
