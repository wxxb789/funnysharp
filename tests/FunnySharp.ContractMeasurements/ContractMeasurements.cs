using System.Runtime.InteropServices;
using System.Text.Json;
using FunnySharp;
using FunnySharp.TestSupport;
using FunnySharp.TestSupport.Contracts;
using static FunnySharp.TestSupport.MeasurementHarness;

internal static class ContractMeasurements
{
    private const int WarmupIterations = 200;
    private const int DecodeIterations = 10_000;
    private const int ValidationIterations = 20_000;
    private const int GenerationIterations = 5_000;
    private const int ShrinkIterations = 5_000;
    private const int SchemaIterations = 20_000;

    internal static void Run()
    {
        Console.WriteLine($"runtime: {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"os: {RuntimeInformation.OSDescription}");
        Console.WriteLine($"cores: {Environment.ProcessorCount}");
        Console.WriteLine();

        using var validDocument = JsonDocument.Parse(OrderFixtures.Valid.Json);
        using var invalidDocument = JsonDocument.Parse(OrderFixtures.InvalidValues.Json);
        var validRoot = validDocument.RootElement;
        var invalidRoot = invalidDocument.RootElement;

        var contractDecoder = ContractOrderConsumer.CreateOrderDecoder();
        var validOrder = ContractOrderConsumer.Decode(validRoot).Value!;
        var invalidOrder = OrderFixtures.InvalidOrder();

        Console.WriteLine(
            "| Operation | Implementation | Allocated B/op | Time ns/op |");
        Console.WriteLine("| --- | --- | ---: | ---: |");

        Row(
            "decode valid",
            "contract",
            Measure(() => Checksum(contractDecoder.Decode(validRoot).IsValid), DecodeIterations));
        Row(
            "decode valid",
            "ordinary",
            Measure(() => Checksum(OrdinaryOrderConsumer.Decode(validRoot).IsSuccess), DecodeIterations));
        Row(
            "decode invalid",
            "contract",
            Measure(() => Checksum(contractDecoder.Decode(invalidRoot).IsValid), DecodeIterations));
        Row(
            "decode invalid",
            "ordinary",
            Measure(() => Checksum(OrdinaryOrderConsumer.Decode(invalidRoot).IsSuccess), DecodeIterations));

        Row(
            "validate valid",
            "contract",
            Measure(() => Checksum(ContractOrderConsumer.Validate(validOrder).Count), ValidationIterations));
        Row(
            "validate valid",
            "ordinary",
            Measure(() => Checksum(OrdinaryOrderConsumer.Validate(validOrder).Count), ValidationIterations));
        Row(
            "validate invalid",
            "contract",
            Measure(() => Checksum(ContractOrderConsumer.Validate(invalidOrder).Count), ValidationIterations));
        Row(
            "validate invalid",
            "ordinary",
            Measure(() => Checksum(OrdinaryOrderConsumer.Validate(invalidOrder).Count), ValidationIterations));

        var generationOptions = new ContractGenerationOptions(seed: 20261031);
        Row(
            "generate one",
            "contract",
            Measure(
                () => Checksum(ContractOrderConsumer.OrderContract.Generate(generationOptions).IsSuccess),
                GenerationIterations));
        Row(
            "generate one",
            "ordinary",
            Measure(
                () => Checksum(OrdinaryOrderConsumer.Generate(20261031) is not null),
                GenerationIterations));
        Row(
            "generate ten",
            "contract",
            Measure(
                () => Checksum(ContractOrderConsumer.OrderContract.GenerateSamples(10, generationOptions).IsSuccess),
                GenerationIterations / 5));
        Row(
            "generate ten",
            "ordinary",
            Measure(
                () =>
                {
                    var count = 0;
                    for (var index = 0; index < 10; index++)
                    {
                        count += OrdinaryOrderConsumer.Generate(index + 20261031) is not null ? 1 : 0;
                    }

                    return count;
                },
                GenerationIterations / 5));

        Row(
            "shrink one",
            "contract",
            Measure(
                () => Checksum(ContractOrderConsumer.OrderContract.Shrink(validOrder).IsSuccess),
                ShrinkIterations));
        Row(
            "shrink one",
            "ordinary",
            Measure(
                () => Checksum(OrdinaryOrderConsumer.Shrink(validOrder).Count),
                ShrinkIterations));

        Row(
            "schema facts",
            "contract",
            Measure(() => Checksum(ContractOrderConsumer.DescribeFacts().Count), SchemaIterations));
        Row(
            "schema facts",
            "ordinary",
            Measure(() => Checksum(OrdinaryOrderConsumer.DescribeFacts().Count), SchemaIterations));
        Row(
            "explain",
            "contract",
            Measure(() => Checksum(ContractOrderConsumer.Explain().Length), SchemaIterations));

        Console.WriteLine();
        Console.WriteLine($"contract shrink candidates: {ContractOrderConsumer.Shrink(validOrder).Count}");
        Console.WriteLine($"ordinary shrink candidates: {OrdinaryOrderConsumer.Shrink(validOrder).Count}");
        Console.WriteLine(
            "Allocations are per complete operation. Timing is directional on this host; "
            + "the allocation columns are the decisive evidence.");
    }

    private static void Row(string operation, string implementation, Measurement measurement) =>
        Console.WriteLine(
            $"| {operation} | {implementation} | {Format(measurement.BytesPerOperation)} | "
            + $"{Format(measurement.NanosecondsPerOperation)} |");

    private static Measurement Measure(Func<int> operation, int iterations) =>
        MeasurementHarness.Measure(operation, iterations, WarmupIterations);

    private static int Checksum(bool value) => value ? 1 : 0;

    private static int Checksum(int value) => value;
}
