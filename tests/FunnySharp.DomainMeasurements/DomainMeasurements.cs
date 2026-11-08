using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using static FunnySharp.TestSupport.MeasurementHarness;

namespace FunnySharp.TestSupport.Domains;

internal static class DomainMeasurements
{
    private const int WarmupIterations = 500;
    private const int WorkflowIterations = 20_000;
    private const int ValueIterations = 500_000;

    internal static void Run()
    {
        Console.WriteLine($"runtime: {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"os: {RuntimeInformation.OSDescription}");
        Console.WriteLine($"cores: {Environment.ProcessorCount}");
        Console.WriteLine();

        DomainComparison.Verify();

        using var validDocument = JsonDocument.Parse(DomainFixtures.Valid.Json);
        using var invalidDocument = JsonDocument.Parse(DomainFixtures.MultipleInvalid.Json);
        var valid = validDocument.RootElement;
        var invalid = invalidDocument.RootElement;

        Console.WriteLine("| Workflow | Case | Allocated B/op | Time ns/op |");
        Console.WriteLine("| --- | --- | ---: | ---: |");
        WriteWorkflow(
            "FunnySharp domain workflow",
            "valid decode + submit + ship",
            Measure(() => Digest(FunnySharpOrderWorkflow.Run(valid)), WorkflowIterations));
        WriteWorkflow(
            "Ordinary adapter workflow",
            "valid decode + submit + ship",
            Measure(() => Digest(OrdinaryOrderWorkflow.Run(valid)), WorkflowIterations));
        WriteWorkflow(
            "FunnySharp domain workflow",
            "invalid decode",
            Measure(() => Digest(FunnySharpOrderWorkflow.Run(invalid)), WorkflowIterations));
        WriteWorkflow(
            "Ordinary adapter workflow",
            "invalid decode",
            Measure(() => Digest(OrdinaryOrderWorkflow.Run(invalid)), WorkflowIterations));

        Console.WriteLine();
        Console.WriteLine(
            "struct sizes: " +
            $"DomainValue<string, OrderIdTag> {Unsafe.SizeOf<DomainValue<string, OrderIdTag>>()} B, " +
            $"DomainValue<int, QuantityTag> {Unsafe.SizeOf<DomainValue<int, QuantityTag>>()} B, " +
            $"hand-written wrapper {Unsafe.SizeOf<HandWrittenOrderId>()} B, " +
            $"raw reference {Unsafe.SizeOf<string>()} B, raw int {Unsafe.SizeOf<int>()} B.");
        Console.WriteLine();

        Console.WriteLine("| Identifier operation | Allocated B/op | Time ns/op |");
        Console.WriteLine("| --- | ---: | ---: |");
        var orderId = "ORD-100";
        WriteRow(
            "Raw string identity",
            Measure(() => orderId.Length, ValueIterations));
        WriteRow(
            "Hand-written wrapper Create",
            Measure(
                () => HandWrittenOrderId.Create(orderId).IsValid ? 1 : 0,
                ValueIterations));
        WriteRow(
            "Domain.Validate",
            Measure(
                () => Domains.OrderId.Validate(orderId).IsValid ? 1 : 0,
                ValueIterations));
        var domainOrderId = ValidationUnwrap.Require(Domains.OrderId.Validate(orderId));
        var handWrittenOrderId = ValidationUnwrap.Require(HandWrittenOrderId.Create(orderId));
        WriteRow(
            "Domain.Contract.Validate",
            Measure(
                () => Domains.OrderId.Contract.Validate(domainOrderId).IsValid ? 1 : 0,
                ValueIterations));
        WriteRow(
            "Hand-written Contract.Validate",
            Measure(
                () => HandWrittenOrderId.Contract.Validate(handWrittenOrderId).IsValid ? 1 : 0,
                ValueIterations));
        using var orderIdDocument = JsonDocument.Parse("\"ORD-100\"");
        WriteRow(
            "Domain decoder",
            Measure(
                () => Domains.OrderIdDecoder.Decode(orderIdDocument.RootElement).IsValid ? 1 : 0,
                ValueIterations));
        WriteRow(
            "Hand-written wrapper decoder",
            Measure(
                () => HandWrittenOrderId.Decoder.Decode(orderIdDocument.RootElement).IsValid ? 1 : 0,
                ValueIterations));

        Console.WriteLine();
        Console.WriteLine("| State operation | Allocated B/op | Time ns/op |");
        Console.WriteLine("| --- | ---: | ---: |");
        var draft = DecodeDraft(valid);
        var placed = draft.Submit().Match(
            static value => value,
            static error => throw new InvalidOperationException(error.Detail));
        var ordinaryPlaced = new OrdinaryOrder(
            "ORD-100",
            "CUS-100",
            "SKU-BOOK",
            2,
            OrdinaryOrderState.Placed,
            12.50m,
            "RSV-100",
            "AUTH-100",
            null,
            null);
        var tracking = Domains.TrackingCode("ORD-100");
        WriteRow(
            "Domain placed -> shipped",
            Measure(() => placed.Ship(tracking).Quantity.Value, WorkflowIterations));
        WriteRow(
            "Ordinary placed -> shipped",
            Measure(
                () => ordinaryPlaced.Ship("TRK-ORD-100").Match(
                    static value => value.Quantity,
                    static error => error.Code.Length),
                WorkflowIterations));
        WriteRow(
            "Ordinary wrong-state guard",
            Measure(
                () => ordinaryPlaced.Cancel("customer-request").Match(
                    static value => 1,
                    static error => 0),
                WorkflowIterations));

        Console.WriteLine();
        Console.WriteLine(
            "Allocations are per complete mapped operation. Timing is directional on this host.");
    }

    private static DraftOrder DecodeDraft(JsonElement input) =>
        ValidationUnwrap.Require(FunnySharpOrderWorkflow.DraftDecoder.Decode(input));

    private static int Digest(WorkflowOutcome outcome) =>
        outcome.Status.Length
        + (outcome.OrderId?.Length ?? 0)
        + outcome.Quantity
        + outcome.Failures.Count
        + (outcome.TrackingCode?.Length ?? 0);

    private static Measurement Measure(Func<int> operation, int iterations) =>
        MeasurementHarness.Measure(operation, iterations, WarmupIterations);

    private static void WriteWorkflow(string name, string scenario, Measurement measurement) =>
        Console.WriteLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $"| {name} | {scenario} | {Format(measurement.BytesPerOperation)} | " +
                $"{Format(measurement.NanosecondsPerOperation)} |"));

    private static void WriteRow(string name, Measurement measurement) =>
        Console.WriteLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $"| {name} | {Format(measurement.BytesPerOperation)} | " +
                $"{Format(measurement.NanosecondsPerOperation)} |"));
}
