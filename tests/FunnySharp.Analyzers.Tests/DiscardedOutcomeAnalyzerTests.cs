namespace FunnySharp.Analyzers.Tests;

public sealed class DiscardedOutcomeAnalyzerTests
{
    [Fact]
    public async Task DiscardedUserCarrierCallIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                Option<int> Find() => Option.Some(1);

                void Use()
                {
                    Find();
                }
            }
            """,
            "FS1002");
    }

    [Fact]
    public async Task DiscardedFactoryCallIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                void Use()
                {
                    Option.Some(1);
                }
            }
            """,
            "FS1002");
    }

    [Fact]
    public async Task DiscardedEffectCreationIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                void Use()
                {
                    Effect.FromValue(1);
                }
            }
            """,
            "FS1002");
    }

    [Fact]
    public async Task UnawaitedValueTaskFromFunnySharpMemberIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                void Use()
                {
                    Effect.FromValue(1).RunAsync();
                }
            }
            """,
            "FS1002");
    }

    [Fact]
    public async Task UnawaitedTaskOfOutcomeFromAnyMethodIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                Task<Result<int, string>> SaveAsync() => Task.FromResult(Result<int, string>.Success(1));

                void Use()
                {
                    SaveAsync();
                }
            }
            """,
            "FS1002");
    }

    [Fact]
    public async Task AwaitedTaskOfOutcomeIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                Task<Result<int, string>> SaveAsync() => Task.FromResult(Result<int, string>.Success(1));

                async Task UseAsync()
                {
                    await SaveAsync();
                }
            }
            """,
            "FS1002");
    }

    [Fact]
    public async Task AwaitedStoredTaskOfOutcomeIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                Task<Result<int, string>> SaveAsync() => Task.FromResult(Result<int, string>.Success(1));

                async Task UseAsync()
                {
                    var pending = SaveAsync();
                    await pending;
                }
            }
            """,
            "FS1002");
    }

    [Fact]
    public async Task AwaitedConfiguredTaskOfOutcomeIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                Task<Result<int, string>> SaveAsync() => Task.FromResult(Result<int, string>.Success(1));

                async Task UseAsync()
                {
                    await SaveAsync().ConfigureAwait(false);
                }
            }
            """,
            "FS1002");
    }

    [Fact]
    public async Task AwaitedTaskOfNonOutcomeFromFunnySharpMemberIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                void Use()
                {
                    4.Pipe(async number => number.ToString());
                }
            }
            """,
            "FS1002");
    }

    [Fact]
    public async Task DiscardedStateMachineInvocationIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                TransitionResult<int, int, string> Step(int state, int @event) =>
                    TransitionResult<int, int, string>.Applied(StateChange<int, int>.To(state, @event));

                void Use()
                {
                    StateMachine<int, int, int, string> machine = Step;
                    machine(0, 0);
                }
            }
            """,
            "FS1002");
    }

    [Fact]
    public async Task DiscardedComposedTransitionIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                StateChange<int, int> First(int state) => StateChange<int, int>.To(state, 1);

                StateChange<int, int> Second(int state) => StateChange<int, int>.To(state, 2);

                void Use()
                {
                    StateTransition<int, int> first = First;
                    StateTransition<int, int> second = Second;
                    first.Then(second);
                }
            }
            """,
            "FS1002");
    }

    [Fact]
    public async Task ConditionalAccessInvocationIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                Option<int> Find() => Option.Some(1);

                void Use(C? subject)
                {
                    subject?.Find();
                }
            }
            """,
            "FS1002");
    }

    [Fact]
    public async Task AssignedAndReturnedCallsAreQuiet()
    {
        await AnalyzerTestAssert.QuietAsync(
            """
            using FunnySharp;
            class C
            {
                Option<int> Find() => Option.Some(1);

                int Use()
                {
                    var found = Find();
                    return found.TryGetValue(out var value) ? value : 0;
                }

                Option<int> Pass() => Find();
            }
            """);
    }

    [Fact]
    public async Task ExplicitDiscardIsQuiet()
    {
        await AnalyzerTestAssert.QuietAsync(
            """
            using FunnySharp;
            class C
            {
                Option<int> Find() => Option.Some(1);

                void Use()
                {
                    _ = Find();
                }
            }
            """);
    }

    [Fact]
    public async Task AwaitedNonOutcomeTaskFromUserMethodIsQuiet()
    {
        await AnalyzerTestAssert.QuietAsync(
            """
            using FunnySharp;
            class C
            {
                Task<int> LoadAsync() => Task.FromResult(1);

                async Task UseAsync()
                {
                    await LoadAsync();
                }
            }
            """);
    }

    [Fact]
    public async Task AwaitedNonOutcomeValueTaskFromFunnySharpMemberIsQuiet()
    {
        await AnalyzerTestAssert.QuietAsync(
            """
            using FunnySharp;
            class C
            {
                async Task UseAsync()
                {
                    await Effect.FromValue(1).RunAsync();
                }
            }
            """);
    }

    [Fact]
    public async Task AwaitedAndConsumedTaskOfOutcomeIsQuiet()
    {
        await AnalyzerTestAssert.QuietAsync(
            """
            using FunnySharp;
            class C
            {
                Task<Result<int, string>> SaveAsync() => Task.FromResult(Result<int, string>.Success(1));

                async Task<Result<int, string>> UseAsync()
                {
                    var consumed = await SaveAsync();
                    return consumed;
                }
            }
            """);
    }

    [Fact]
    public async Task VoidMatchStatementIsQuiet()
    {
        await AnalyzerTestAssert.QuietAsync(
            """
            using FunnySharp;
            class C
            {
                void Use(UnitResult<string> result)
                {
                    result.Match(() => { }, error => { });
                }
            }
            """);
    }

    [Fact]
    public async Task TapStatementsAreQuiet()
    {
        await AnalyzerTestAssert.QuietAsync(
            """
            using FunnySharp;
            class C
            {
                Option<int> Find() => Option.Some(1);

                void Use()
                {
                    Find().Tap(Observe);
                    Find().TapAsync(value => Task.CompletedTask);
                    Find().TapValueAsync(value => ValueTask.CompletedTask);
                }

                void Observe(Option<int> value)
                {
                }
            }
            """);
    }

    [Fact]
    public async Task TaskDelayAndAccessorsAreQuiet()
    {
        await AnalyzerTestAssert.QuietAsync(
            """
            using FunnySharp;
            class C
            {
                async Task UseAsync()
                {
                    await Task.Delay(0);
                }

                void Use()
                {
                    var text = Option.Some(1).ToString();
                    var hash = Option.Some(1).GetHashCode();
                    _ = text;
                    _ = hash;
                }
            }
            """);
    }
}
