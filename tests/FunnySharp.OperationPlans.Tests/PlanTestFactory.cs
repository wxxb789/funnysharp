using FunnySharp;
using FunnySharp.OperationPlans;

namespace FunnySharp.OperationPlans.Tests;

internal static class PlanTestFactory
{
    internal static OperationPlan<object?, int, string> CreatePlan(
        params OperationStep<object?, int, string>[] steps) =>
        OperationPlan<object?, int, string>.Create(steps);

    internal static OperationInfo Info(string name, params string[] dependsOn) =>
        new(
            name,
            OperationKind.Read,
            dependsOn,
            requiresPriorResult: false,
            dynamicWork: false,
            conditional: false,
            "test");

    internal static OperationStep<object?, int, string> Step(
        string name,
        Func<object?, int, CancellationToken, Task<Result<int, string>>> run,
        Func<int, bool>? condition = null,
        params string[] dependsOn) =>
        new(
            new OperationInfo(
                name,
                OperationKind.Read,
                dependsOn,
                requiresPriorResult: false,
                dynamicWork: false,
                conditional: condition is not null,
                "test"),
            run,
            condition);

    internal static Task<Result<int, string>> Succeed(
        object? environment,
        int state,
        CancellationToken cancellationToken) =>
        Task.FromResult(Result<int, string>.Success(state + 1));

    internal static Func<object?, int, CancellationToken, Task<Result<int, string>>> Fail(string error) =>
        (_, _, _) => Task.FromResult(Result<int, string>.Failure(error));
}
