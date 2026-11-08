using FunnySharp;

namespace FunnySharp.TestSupport.ProgramComparison;

internal static class ProgramComparisonVerifier
{
    internal static void Verify()
    {
        foreach (var scenario in OrderScenarios.All)
        {
            var hand = OrderScenarios.RunHandWritten(scenario);
            var funny = OrderScenarios.RunFunnySharp(scenario);

            AssertExpectation(scenario.Expectation, hand.Report);
            AssertExpectation(scenario.Expectation, funny.Report);
            OrderRunReports.AssertEqual(
                hand.Report,
                funny.Report,
                scenario.Name + ": hand/funny parity");

            var handAgain = OrderScenarios.RunHandWritten(scenario);
            var funnyAgain = OrderScenarios.RunFunnySharp(scenario);
            OrderRunReports.AssertEqual(
                hand.Report,
                handAgain.Report,
                scenario.Name + ": hand reproducibility");
            OrderRunReports.AssertEqual(
                funny.Report,
                funnyAgain.Report,
                scenario.Name + ": funny reproducibility");

            AssertReplay(scenario, hand, funny);
        }
    }

    private static void AssertExpectation(
        OrderScenarioExpectation expectation,
        OrderRunReport report)
    {
        OrderAssert.Equal(
            expectation.Model,
            report.Model,
            expectation.Name + ": final model");
        OrderAssert.Equal(
            expectation.Now,
            report.Now,
            expectation.Name + ": final time");
        OrderAssert.SequenceEqual(
            expectation.EmittedCommands,
            report.EmittedCommands,
            expectation.Name + ": emitted commands");
        OrderAssert.SequenceEqual(
            expectation.EmittedCommands.Select(static command => command.Sequence).ToArray(),
            report.EmittedCommands.Select(static command => command.Sequence).ToArray(),
            expectation.Name + ": command sequence");
        var deliveries = report.Deliveries
            .Select(static delivery => new ExpectedDelivery(
                delivery.CommandSequence,
                delivery.At,
                delivery.Status,
                delivery.Error))
            .ToArray();
        OrderAssert.SequenceEqual(
            expectation.Deliveries,
            deliveries,
            expectation.Name + ": delivery outcomes");
        OrderAssert.SequenceEqual(
            expectation.CommandFailures,
            report.CommandFailures,
            expectation.Name + ": command failures");
    }

    private static void AssertReplay(
        OrderScenario scenario,
        OrderRun hand,
        OrderRun funny)
    {
        var handReplay = hand.Simulation.Replay();
        var funnyReplay = funny.Simulation.Replay();
        OrderAssert.Equal(
            handReplay,
            funnyReplay,
            scenario.Name + ": replay parity");

        var firstNonApplied = hand.Report.Deliveries
            .FirstOrDefault(static delivery => delivery.Status != TransitionStatus.Applied);
        if (firstNonApplied is null)
        {
            OrderAssert.True(
                handReplay.TryGetChange(out var change),
                scenario.Name + ": applied replay");
            OrderAssert.Equal(
                hand.Report.Model,
                change!.State,
                scenario.Name + ": replay final model");
            OrderAssert.SequenceEqual(
                hand.Report.EmittedCommands.Select(static command => command.Command).ToArray(),
                change.Outputs,
                scenario.Name + ": replay commands");
        }
        else
        {
            OrderAssert.Equal(
                firstNonApplied.Status,
                handReplay.Status,
                scenario.Name + ": replay stop status");
            if (firstNonApplied.Error is { } expectedError)
            {
                OrderAssert.True(
                    handReplay.TryGetError(out var replayError),
                    scenario.Name + ": replay stop error");
                OrderAssert.Equal(
                    expectedError,
                    replayError!,
                    scenario.Name + ": replay stop error value");
            }
            else
            {
                OrderAssert.True(
                    handReplay.IsUndefined,
                    scenario.Name + ": replay stops at undefined");
            }
        }

        var handEmittedCount = hand.Simulation.EmittedCommands.Count;
        var handDeliveryCount = hand.Simulation.Deliveries.Count;
        var handFailureCount = hand.Simulation.CommandFailures.Count;
        var handPendingCount = hand.Simulation.PendingCount;
        var funnyEmittedCount = funny.Simulation.EmittedCommands.Count;
        var funnyDeliveryCount = funny.Simulation.Deliveries.Count;
        var funnyFailureCount = funny.Simulation.CommandFailures.Count;
        var funnyPendingCount = funny.Simulation.PendingCount;
        _ = hand.Simulation.Replay();
        _ = funny.Simulation.Replay();
        OrderAssert.Equal(
            handEmittedCount,
            hand.Simulation.EmittedCommands.Count,
            scenario.Name + ": direct replay does not emit commands");
        OrderAssert.Equal(
            handDeliveryCount,
            hand.Simulation.Deliveries.Count,
            scenario.Name + ": direct replay does not deliver messages");
        OrderAssert.Equal(
            handFailureCount,
            hand.Simulation.CommandFailures.Count,
            scenario.Name + ": direct replay does not record failures");
        OrderAssert.Equal(
            handPendingCount,
            hand.Simulation.PendingCount,
            scenario.Name + ": direct replay does not change pending commands");
        OrderAssert.Equal(
            funnyEmittedCount,
            funny.Simulation.EmittedCommands.Count,
            scenario.Name + ": replay does not emit commands");
        OrderAssert.Equal(
            funnyDeliveryCount,
            funny.Simulation.Deliveries.Count,
            scenario.Name + ": replay does not deliver messages");
        OrderAssert.Equal(
            funnyFailureCount,
            funny.Simulation.CommandFailures.Count,
            scenario.Name + ": replay does not record failures");
        OrderAssert.Equal(
            funnyPendingCount,
            funny.Simulation.PendingCount,
            scenario.Name + ": replay does not change pending commands");
    }
}
