using FunnySharp;
using static FunnySharp.TestSupport.MeasurementHarness;

namespace FunnySharp.TestSupport.ProgramComparison;

internal static class ProgramMeasurements
{
    private const int Iterations = 50;
    private const int WarmupIterations = 5;

    internal static void Run()
    {
        ProgramComparisonVerifier.Verify();

        var dispatchMessages = BuildDispatchMessages();
        var funnyProgram = FunnySharpOrderPrograms.Create();
        var successScenario = OrderScenarios.All[0];
        var replayHand = OrderScenarios.RunHandWritten(successScenario).Simulation;
        var replayFunny = OrderScenarios.RunFunnySharp(successScenario).Simulation;

        OrderAssert.Equal(
            DispatchBaseline(dispatchMessages),
            DispatchFunny(funnyProgram, dispatchMessages),
            "dispatch outputs");
        OrderAssert.Equal(
            StateCopyBaseline(),
            StateCopyFunny(funnyProgram),
            "state-copy outputs");
        OrderAssert.Equal(
            FullScenarioBaseline(successScenario),
            FullScenarioFunny(successScenario),
            "full-scenario outputs");
        OrderAssert.Equal(
            ReplayBaseline(replayHand),
            ReplayFunny(replayFunny),
            "replay outputs");

        var rows = new (string Name, Measurement Value)[]
        {
            (
                "Composition - direct switch delegate",
                MeasureRow(BaselineComposition)),
            (
                "Composition - DomainProgram Lift + OrElse",
                MeasureRow(FunnyComposition)),
            (
                "Dispatch - direct switch, 7 messages",
                MeasureRow(() => DispatchBaseline(dispatchMessages))),
            (
                "Dispatch - DomainProgram, 7 messages",
                MeasureRow(() => DispatchFunny(funnyProgram, dispatchMessages))),
            (
                "State copy - direct switch, one inventory update",
                MeasureRow(StateCopyBaseline)),
            (
                "State copy - DomainProgram, one inventory update",
                MeasureRow(() => StateCopyFunny(funnyProgram))),
            (
                "Full scenario - direct switch simulation",
                MeasureRow(() => FullScenarioBaseline(successScenario))),
            (
                "Full scenario - ProgramSimulation",
                MeasureRow(() => FullScenarioFunny(successScenario))),
            (
                "Replay - direct switch",
                MeasureRow(() => ReplayBaseline(replayHand))),
            (
                "Replay - ProgramSimulation",
                MeasureRow(() => ReplayFunny(replayFunny))),
        };

        Console.WriteLine("Domain program composition and simulation measurements");
        Console.WriteLine(
            $"Dispatch messages: {dispatchMessages.Count}; iterations: {Iterations}; "
            + $"warmup: {WarmupIterations}; min of 3 rounds.");
        Console.WriteLine(
            "Allocation uses GC.GetAllocatedBytesForCurrentThread for every row; "
            + "tiered compilation is disabled. Timing is directional.");
        Console.WriteLine();
        Console.WriteLine("| Operation | Allocated B/op | Time ns/op |");
        Console.WriteLine("| --- | ---: | ---: |");

        foreach (var row in rows)
        {
            Console.WriteLine(
                $"| {row.Name} | {Format(row.Value.BytesPerOperation)} | "
                + $"{Format(row.Value.NanosecondsPerOperation)} |");
        }
    }

    private static Measurement MeasureRow(Func<int> operation) =>
        Measure(operation, Iterations, WarmupIterations);

    private static IReadOnlyList<OrderMessage> BuildDispatchMessages() =>
    [
        new ReserveInventory("ORD-1", "SKU-1", 2, 100),
        new InventoryReserved("ORD-1", 100, true),
        new ChargePayment("ORD-1", 25m, 200, OrderScenarios.StartedAt),
        new PaymentCharged("ORD-1", 200, true, null),
        new CancelOrder("ORD-1", "customer", 300),
        new CancelOrder("ORD-1", "again", 300),
        new UnroutedOrderMessage("ORD-1"),
    ];

    private static int BaselineComposition()
    {
        StateMachine<OrderModel, OrderMessage, OrderCommand, OrderError> update =
            HandWrittenOrderWorkflow.Update;
        _ = update.Target;
        return 1;
    }

    private static int FunnyComposition()
    {
        var program = FunnySharpOrderPrograms.Create();
        _ = program.AsStateMachine().Target;
        return 1;
    }

    private static int DispatchBaseline(IReadOnlyList<OrderMessage> messages)
    {
        var model = OrderScenarios.InitialModel();
        var commands = 0;
        for (var index = 0; index < messages.Count; index++)
        {
            var result = HandWrittenOrderWorkflow.Update(model, messages[index]);
            if (result.TryGetChange(out var change))
            {
                model = change.State;
                commands += change.Outputs.Count;
            }
        }

        return commands;
    }

    private static int DispatchFunny(
        DomainProgram<OrderModel, OrderMessage, OrderCommand, OrderError> program,
        IReadOnlyList<OrderMessage> messages)
    {
        var model = OrderScenarios.InitialModel();
        var commands = 0;
        for (var index = 0; index < messages.Count; index++)
        {
            var result = program.Update(model, messages[index]);
            if (result.TryGetChange(out var change))
            {
                model = change.State;
                commands += change.Outputs.Count;
            }
        }

        return commands;
    }

    private static int StateCopyBaseline()
    {
        var model = OrderScenarios.InitialModel();
        var result = HandWrittenOrderWorkflow.Update(
            model,
            new ReserveInventory("ORD-1", "SKU-1", 2, 100));
        return result.TryGetChange(out var change) ? change.Outputs.Count : 0;
    }

    private static int StateCopyFunny(
        DomainProgram<OrderModel, OrderMessage, OrderCommand, OrderError> program)
    {
        var model = OrderScenarios.InitialModel();
        var result = program.Update(
            model,
            new ReserveInventory("ORD-1", "SKU-1", 2, 100));
        return result.TryGetChange(out var change) ? change.Outputs.Count : 0;
    }

    private static int FullScenarioBaseline(OrderScenario scenario)
    {
        var run = OrderScenarios.RunHandWritten(scenario);
        return run.Report.EmittedCommands.Count
            + run.Report.Deliveries.Count
            + run.Report.CommandFailures.Count;
    }

    private static int FullScenarioFunny(OrderScenario scenario)
    {
        var run = OrderScenarios.RunFunnySharp(scenario);
        return run.Report.EmittedCommands.Count
            + run.Report.Deliveries.Count
            + run.Report.CommandFailures.Count;
    }

    private static int ReplayBaseline(IOrderSimulation simulation)
    {
        var result = simulation.Replay();
        return result.TryGetChange(out var change) ? change.Outputs.Count : -1;
    }

    private static int ReplayFunny(IOrderSimulation simulation)
    {
        var result = simulation.Replay();
        return result.TryGetChange(out var change) ? change.Outputs.Count : -1;
    }
}
