namespace FunnySharp.Tests;

public sealed class DomainProgramLiftTests
{
    private sealed record UnknownChildMessage : CounterMessage;

    private sealed record ChildUnknown : ParentMessage;

    [Fact]
    public void LiftRejectsNullDelegates()
    {
        var child = ProgramTestSupport.CreateProgram();

        Assert.Throws<ArgumentNullException>(
            () => child.Lift<ParentModel, ParentMessage, ParentCommand>(
                null!,
                static (parent, childState) => parent with { Child = childState },
                Route,
                LiftCommandWithParent));
        Assert.Throws<ArgumentNullException>(
            () => child.Lift<ParentModel, ParentMessage, ParentCommand>(
                static parent => parent.Child,
                null!,
                Route,
                LiftCommandWithParent));
        Assert.Throws<ArgumentNullException>(
            () => child.Lift<ParentModel, ParentMessage, ParentCommand>(
                static parent => parent.Child,
                static (parent, childState) => parent with { Child = childState },
                null!,
                LiftCommandWithParent));
        Assert.Throws<ArgumentNullException>(
            () => child.Lift<ParentModel, ParentMessage, ParentCommand>(
                static parent => parent.Child,
                static (parent, childState) => parent with { Child = childState },
                Route,
                null!));
    }

    [Fact]
    public void LiftRoutesNoneWithoutTouchingTheChild()
    {
        var ledger = new List<string>();
        var lifted = InstrumentedChild(ledger).Lift<ParentModel, ParentMessage, ParentCommand>(
            parent =>
            {
                ledger.Add("get");
                return parent.Child;
            },
            (parent, childState) =>
            {
                ledger.Add("set");
                return parent with { Child = childState };
            },
            message =>
            {
                ledger.Add("route");
                return Route(message);
            },
            (_, command) =>
            {
                ledger.Add("lift");
                return LiftCommand(command);
            });

        var result = lifted.Update(NewParent(), new Unrouted());

        Assert.True(result.IsUndefined);
        Assert.Equal(["route"], ledger);
    }

    [Fact]
    public void LiftAppliesTheChildAndLiftsCommandsInOutputOrder()
    {
        var ledger = new List<string>();
        var lifterParents = new List<ParentModel>();
        ParentModel? setterParent = null;
        var lifted = InstrumentedChild(ledger).Lift<ParentModel, ParentMessage, ParentCommand>(
            parent =>
            {
                ledger.Add("get");
                return parent.Child;
            },
            (parent, childState) =>
            {
                ledger.Add("set");
                setterParent = parent;
                return parent with { Child = childState };
            },
            message =>
            {
                ledger.Add("route");
                return Route(message);
            },
            (parent, command) =>
            {
                ledger.Add("lift");
                lifterParents.Add(parent);
                return LiftCommand(command);
            });

        var parent = NewParent();
        var result = lifted.Update(parent, new ChildSplit(1, 2, 7));

        Assert.True(result.TryGetChange(out var change));
        Assert.Equal(["route", "get", "child", "lift", "lift", "set"], ledger);
        Assert.Equal(new CounterModel(3, 7), change!.State.Child);
        Assert.Equal(
            new ParentCommand[] { new ParentEmit(1, 7), new ParentEmit(2, 7) },
            change.Outputs);
        Assert.Equal(2, lifterParents.Count);
        Assert.All(
            lifterParents,
            seen =>
            {
                Assert.Equal(new CounterModel(0, 0), seen.Child);
                Assert.Same(parent.Sibling, seen.Sibling);
            });
        Assert.Same(parent, setterParent);
        Assert.Equal(0, parent.Child.Value);
        Assert.Equal(0, parent.Child.Version);
    }

    [Fact]
    public void LiftPreservesRejectedAndFailedChildResultsWithoutLiftingOrSetting()
    {
        var ledger = new List<string>();
        var lifted = InstrumentedChild(ledger).Lift<ParentModel, ParentMessage, ParentCommand>(
            parent =>
            {
                ledger.Add("get");
                return parent.Child;
            },
            (parent, childState) =>
            {
                ledger.Add("set");
                return parent with { Child = childState };
            },
            message =>
            {
                ledger.Add("route");
                return Route(message);
            },
            (_, command) =>
            {
                ledger.Add("lift");
                return LiftCommand(command);
            });

        var rejected = lifted.Update(NewParent(), new ChildReject("no"));
        Assert.True(rejected.IsRejected);
        Assert.True(rejected.TryGetError(out var rejection));
        Assert.Equal(new RejectedError("no"), rejection);
        Assert.Equal(["route", "get", "child"], ledger);

        ledger.Clear();
        var failed = lifted.Update(NewParent(), new ChildFail("broken"));
        Assert.True(failed.IsFailed);
        Assert.True(failed.TryGetError(out var failure));
        Assert.Equal(new FailedError("broken"), failure);
        Assert.Equal(["route", "get", "child"], ledger);
    }

    [Fact]
    public void LiftPreservesAChildUndefinedResult()
    {
        var ledger = new List<string>();
        var lifted = InstrumentedChild(ledger).Lift<ParentModel, ParentMessage, ParentCommand>(
            parent =>
            {
                ledger.Add("get");
                return parent.Child;
            },
            (parent, childState) =>
            {
                ledger.Add("set");
                return parent with { Child = childState };
            },
            message =>
            {
                ledger.Add("route");
                return message is ChildUnknown
                    ? Option.Some<CounterMessage>(new UnknownChildMessage())
                    : Route(message);
            },
            (_, command) =>
            {
                ledger.Add("lift");
                return LiftCommand(command);
            });

        var result = lifted.Update(NewParent(), new ChildUnknown());

        Assert.True(result.IsUndefined);
        Assert.Equal(["route", "get", "child"], ledger);
    }

    [Fact]
    public void LiftAppliesAZeroOutputChildWithoutInvokingTheCommandLifter()
    {
        var ledger = new List<string>();
        var lifted = InstrumentedChild(ledger).Lift<ParentModel, ParentMessage, ParentCommand>(
            parent =>
            {
                ledger.Add("get");
                return parent.Child;
            },
            (parent, childState) =>
            {
                ledger.Add("set");
                return parent with { Child = childState };
            },
            message =>
            {
                ledger.Add("route");
                return Route(message);
            },
            (_, command) =>
            {
                ledger.Add("lift");
                return LiftCommand(command);
            });

        var result = lifted.Update(NewParent(), new ChildNoOp());

        Assert.True(result.TryGetChange(out var change));
        Assert.Equal(["route", "get", "child", "set"], ledger);
        Assert.Empty(change!.Outputs);
    }

    [Fact]
    public void LiftInvokesTheSetterOnceAndPreservesSiblingReferences()
    {
        var setterCalls = 0;
        ParentModel? setterParent = null;
        var lifted = ProgramTestSupport.CreateProgram().Lift<ParentModel, ParentMessage, ParentCommand>(
            static parent => parent.Child,
            (parent, childState) =>
            {
                setterCalls++;
                setterParent = parent;
                return parent with { Child = childState };
            },
            Route,
            LiftCommandWithParent);
        var sibling = new string('s', 1);
        var parent = new ParentModel(new CounterModel(0, 0), sibling);

        var result = lifted.Update(parent, new ChildAdd(4, 3));

        Assert.True(result.TryGetChange(out var change));
        Assert.Equal(1, setterCalls);
        Assert.Same(parent, setterParent);
        Assert.Same(sibling, change!.State.Sibling);
        Assert.Equal(new CounterModel(4, 3), change.State.Child);
        Assert.Same(sibling, parent.Sibling);
        Assert.Equal(new CounterModel(0, 0), parent.Child);
    }

    [Fact]
    public void LiftPropagatesRouteGetterChildLifterAndSetterExceptions()
    {
        var routeException = new InvalidOperationException("route");
        var routeLifted = ProgramTestSupport.CreateProgram().Lift<ParentModel, ParentMessage, ParentCommand>(
            static parent => parent.Child,
            static (parent, childState) => parent with { Child = childState },
            _ => throw routeException,
            LiftCommandWithParent);
        var routeActual = Assert.Throws<InvalidOperationException>(
            () => routeLifted.Update(NewParent(), new ChildAdd(1, 1)));
        Assert.Same(routeException, routeActual);

        var getterException = new InvalidOperationException("get");
        var getterLifted = ProgramTestSupport.CreateProgram().Lift<ParentModel, ParentMessage, ParentCommand>(
            _ => throw getterException,
            static (parent, childState) => parent with { Child = childState },
            Route,
            LiftCommandWithParent);
        var getterActual = Assert.Throws<InvalidOperationException>(
            () => getterLifted.Update(NewParent(), new ChildAdd(1, 1)));
        Assert.Same(getterException, getterActual);

        var childException = new InvalidOperationException("child");
        var throwingChild = DomainProgram.Create<CounterModel, CounterMessage, CounterCommand, CounterError>(
            (model, message) => throw childException);
        var childLifted = throwingChild.Lift<ParentModel, ParentMessage, ParentCommand>(
            static parent => parent.Child,
            static (parent, childState) => parent with { Child = childState },
            Route,
            LiftCommandWithParent);
        var childActual = Assert.Throws<InvalidOperationException>(
            () => childLifted.Update(NewParent(), new ChildAdd(1, 1)));
        Assert.Same(childException, childActual);

        var liftException = new InvalidOperationException("lift");
        var setterCalls = 0;
        var liftLifted = ProgramTestSupport.CreateProgram().Lift<ParentModel, ParentMessage, ParentCommand>(
            static parent => parent.Child,
            (parent, childState) =>
            {
                setterCalls++;
                return parent with { Child = childState };
            },
            Route,
            (_, _) => throw liftException);
        var liftActual = Assert.Throws<InvalidOperationException>(
            () => liftLifted.Update(NewParent(), new ChildAdd(1, 1)));
        Assert.Same(liftException, liftActual);
        Assert.Equal(0, setterCalls);

        var setterException = new InvalidOperationException("set");
        var setterLifted = ProgramTestSupport.CreateProgram().Lift<ParentModel, ParentMessage, ParentCommand>(
            static parent => parent.Child,
            (parent, childState) => throw setterException,
            Route,
            LiftCommandWithParent);
        var setterActual = Assert.Throws<InvalidOperationException>(
            () => setterLifted.Update(NewParent(), new ChildAdd(1, 1)));
        Assert.Same(setterException, setterActual);
    }

    [Fact]
    public void LiftReusesTheSameChildProgramInDifferentParents()
    {
        var child = ProgramTestSupport.CreateProgram();
        var first = child.Lift<ParentModel, ParentMessage, ParentCommand>(
            static parent => parent.Child,
            static (parent, childState) => parent with { Child = childState },
            Route,
            LiftCommandWithParent);
        var second = child.Lift<ParentModel, ParentMessage, ParentCommand>(
            static parent => parent.Child,
            static (parent, childState) => parent with { Child = childState, Sibling = "second" },
            message => message is ChildAdd add
                ? Option.Some<CounterMessage>(new Add(add.Amount, add.Version))
                : Option.None<CounterMessage>(),
            (_, command) => command switch
            {
                Emit emit => new ParentEmit(emit.Amount + 100, emit.Version),
                _ => throw new InvalidOperationException("Unexpected command."),
            });

        var firstResult = first.Update(NewParent(), new ChildAdd(4, 3));
        var secondResult = second.Update(NewParent(), new ChildAdd(4, 3));
        var direct = child.Update(new CounterModel(0, 0), new Add(4, 3));

        Assert.True(firstResult.TryGetChange(out var firstChange));
        Assert.True(secondResult.TryGetChange(out var secondChange));
        Assert.True(direct.TryGetChange(out var directChange));
        Assert.Equal(firstChange!.State.Child, directChange!.State);
        Assert.Equal(secondChange!.State.Child, directChange.State);
        Assert.Equal(new ParentEmit(4, 3), firstChange.Outputs[0]);
        Assert.Equal(new ParentEmit(104, 3), secondChange.Outputs[0]);
        Assert.Equal("second", secondChange.State.Sibling);
    }

    [Fact]
    public void LiftedProgramComposesWithOrElse()
    {
        var child = ProgramTestSupport.CreateProgram();
        var lifted = child.Lift<ParentModel, ParentMessage, ParentCommand>(
            static parent => parent.Child,
            static (parent, childState) => parent with { Child = childState },
            message => message is ChildUnknown
                ? Option.Some<CounterMessage>(new UnknownChildMessage())
                : Option.None<CounterMessage>(),
            LiftCommandWithParent);
        var fallback = DomainProgram.Create<ParentModel, ParentMessage, ParentCommand, CounterError>(
            (parent, message) => message is ChildUnknown
                ? TransitionResult<ParentModel, ParentCommand, CounterError>.Applied(
                    StateChange<ParentModel, ParentCommand>.To(
                        parent with { Sibling = "fallback" }))
                : TransitionResult<ParentModel, ParentCommand, CounterError>.Undefined());

        var result = lifted.OrElse(fallback).Update(NewParent(), new ChildUnknown());

        Assert.True(result.TryGetChange(out var change));
        Assert.Equal("fallback", change!.State.Sibling);
    }

    private static DomainProgram<CounterModel, CounterMessage, CounterCommand, CounterError>
        InstrumentedChild(List<string> ledger) =>
        DomainProgram.Create<CounterModel, CounterMessage, CounterCommand, CounterError>(
            (model, message) =>
            {
                ledger.Add("child");
                return ProgramTestSupport.Update(model, message);
            });

    private static ParentModel NewParent() =>
        new(new CounterModel(0, 0), "sibling");

    private static Option<CounterMessage> Route(ParentMessage message) =>
        message switch
        {
            ChildAdd add => Option.Some<CounterMessage>(new Add(add.Amount, add.Version)),
            ChildAddMany many => Option.Some<CounterMessage>(
                new AddMany(many.First, many.Second, many.Version)),
            ChildSplit split => Option.Some<CounterMessage>(
                new Split(split.First, split.Second, split.Version)),
            ChildNoOp => Option.Some<CounterMessage>(new NoOp()),
            ChildReject reject => Option.Some<CounterMessage>(new Reject(reject.Reason)),
            ChildFail fail => Option.Some<CounterMessage>(new Fail(fail.Reason)),
            _ => Option.None<CounterMessage>(),
        };

    private static ParentCommand LiftCommandWithParent(ParentModel _, CounterCommand command) =>
        LiftCommand(command);

    private static ParentCommand LiftCommand(CounterCommand command) =>
        command switch
        {
            Emit emit => new ParentEmit(emit.Amount, emit.Version),
            EmitMany many => new ParentEmitMany(many.First, many.Second, many.Version),
            _ => throw new InvalidOperationException("Unexpected command."),
        };
}
