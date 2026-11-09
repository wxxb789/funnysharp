using FunnySharp;
using FunnySharp.OperationPlans;

using static FunnySharp.OperationPlans.Tests.PlanTestFactory;

namespace FunnySharp.OperationPlans.Tests;

public sealed class PlanConstructionTests
{
    [Fact]
    public void DuplicateOperationNameIsRejected()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreatePlan(
                Step("same", Succeed),
                Step("same", Succeed)));

        Assert.Contains("duplicated", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EmptyAndWhitespaceNamesAreRejected()
    {
        Assert.Throws<ArgumentException>(() => Info(string.Empty));
        Assert.Throws<ArgumentException>(() => Info("   "));
        Assert.Throws<ArgumentException>(() => Info("a", string.Empty));
        Assert.Throws<ArgumentException>(() => Info("a", "   "));
    }

    [Fact]
    public void EmptyPlanIsRejected()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => OperationPlan<object?, int, string>.Create(
                Array.Empty<OperationStep<object?, int, string>>()));

        Assert.Contains("at least one", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnknownForwardAndSelfDependenciesAreRejected()
    {
        Assert.Throws<ArgumentException>(
            () => CreatePlan(
                Step("a", Succeed, dependsOn: "missing"),
                Step("b", Succeed)));

        Assert.Throws<ArgumentException>(
            () => CreatePlan(
                Step("a", Succeed, dependsOn: "b"),
                Step("b", Succeed)));

        Assert.Throws<ArgumentException>(
            () => CreatePlan(Step("a", Succeed, dependsOn: "a")));
    }

    [Fact]
    public void DuplicateDependencyIsRejected()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new OperationInfo(
                "a",
                OperationKind.Read,
                new[] { "b", "b" },
                requiresPriorResult: false,
                dynamicWork: false,
                conditional: false,
                "test"));

        Assert.Contains("more than once", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConditionalFlagMustMatchTheConditionDelegate()
    {
        Assert.Throws<ArgumentException>(
            () => CreatePlan(
                new OperationStep<object?, int, string>(
                    new OperationInfo(
                        "a",
                        OperationKind.Read,
                        Array.Empty<string>(),
                        requiresPriorResult: false,
                        dynamicWork: false,
                        conditional: true,
                        "test"),
                    Succeed)));

        Assert.Throws<ArgumentException>(
            () => CreatePlan(
                new OperationStep<object?, int, string>(
                    Info("a"),
                    Succeed,
                    static _ => true)));
    }

    [Fact]
    public void NullInputsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(
            () => OperationPlan<object?, int, string>.Create(null!));

        Assert.Throws<ArgumentNullException>(
            () => new OperationStep<object?, int, string>(null!, Succeed));

        Assert.Throws<ArgumentNullException>(
            () => new OperationStep<object?, int, string>(Info("a"), null!));

        Assert.Throws<ArgumentException>(
            () => OperationPlan<object?, int, string>.Create(
                new OperationStep<object?, int, string>[] { null! }));

        Assert.Throws<ArgumentNullException>(
            () => new OperationInfo(
                "a",
                OperationKind.Read,
                null!,
                requiresPriorResult: false,
                dynamicWork: false,
                conditional: false,
                "test"));

        Assert.Throws<ArgumentNullException>(
            () => new OperationInfo(
                "a",
                OperationKind.Read,
                Array.Empty<string>(),
                requiresPriorResult: false,
                dynamicWork: false,
                conditional: false,
                null!));
    }

    [Fact]
    public void DescribeIsReadOnly()
    {
        var plan = CreatePlan(Step("a", Succeed));

        Assert.Throws<NotSupportedException>(
            () => ((IList<OperationInfo>)plan.Describe).Add(Info("b")));
    }

    [Fact]
    public void PlanCopiesTheStepArray()
    {
        var steps = new[] { Step("a", Succeed) };
        var plan = OperationPlan<object?, int, string>.Create(steps);

        steps[0] = Step("b", Succeed);

        Assert.Equal("a", plan.Describe[0].Name);
    }
}
