namespace FunnySharp.Analyzers.Tests;

public sealed class SyncDisposeAnalyzerTests
{
    [Fact]
    public async Task SyncOnlyDisposableIsQuiet()
    {
        await AnalyzerTestAssert.QuietAsync(
            """
            using FunnySharp;
            class C
            {
                Effect<int> Use()
                {
                    return Effect.FromSync(() => new SyncOnly()).Using(resource => Effect.FromValue(1));
                }
            }

            class SyncOnly : IDisposable
            {
                public void Dispose()
                {
                }
            }
            """);
    }

    [Fact]
    public async Task BothInterfacesIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                Effect<int> Use()
                {
                    return Effect.FromSync(() => new Both()).Using(resource => Effect.FromValue(1));
                }
            }

            class Both : IDisposable, IAsyncDisposable
            {
                public void Dispose()
                {
                }

                public ValueTask DisposeAsync()
                {
                    return ValueTask.CompletedTask;
                }
            }
            """,
            "FS1005",
            "Both");
    }

    [Fact]
    public async Task StructImplementingBothInterfacesIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                Effect<int> Use()
                {
                    return Effect.FromSync(() => new BothStruct()).Using(resource => Effect.FromValue(1));
                }
            }

            struct BothStruct : IDisposable, IAsyncDisposable
            {
                public void Dispose()
                {
                }

                public ValueTask DisposeAsync()
                {
                    return ValueTask.CompletedTask;
                }
            }
            """,
            "FS1005");
    }

    [Fact]
    public async Task EnvironmentOverloadIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                Effect<Env, int> Use()
                {
                    return Effect.FromSync((Env env) => new Both()).Using(resource => Effect.FromSync<Env, int>(env => 1));
                }
            }

            class Env;

            class Both : IDisposable, IAsyncDisposable
            {
                public void Dispose()
                {
                }

                public ValueTask DisposeAsync()
                {
                    return ValueTask.CompletedTask;
                }
            }
            """,
            "FS1005");
    }

    [Fact]
    public async Task UsingAsyncIsQuiet()
    {
        await AnalyzerTestAssert.QuietAsync(
            """
            using FunnySharp;
            class C
            {
                Effect<int> Use()
                {
                    return Effect.FromSync(() => new Both()).UsingAsync(resource => Effect.FromValue(1));
                }
            }

            class Both : IDisposable, IAsyncDisposable
            {
                public void Dispose()
                {
                }

                public ValueTask DisposeAsync()
                {
                    return ValueTask.CompletedTask;
                }
            }
            """);
    }

    [Fact]
    public async Task AsyncOnlyResourceWithUsingAsyncIsQuiet()
    {
        await AnalyzerTestAssert.QuietAsync(
            """
            using FunnySharp;
            class C
            {
                Effect<int> Use()
                {
                    return Effect.FromSync(() => new AsyncOnly()).UsingAsync(resource => Effect.FromValue(1));
                }
            }

            class AsyncOnly : IAsyncDisposable
            {
                public ValueTask DisposeAsync()
                {
                    return ValueTask.CompletedTask;
                }
            }
            """);
    }

    [Fact]
    public async Task UserUsingMethodIsQuiet()
    {
        await AnalyzerTestAssert.QuietAsync(
            """
            using FunnySharp;
            class C
            {
                int Use()
                {
                    return Using(new Both());
                }

                int Using<TResource>(TResource resource) => 1;
            }

            class Both : IDisposable, IAsyncDisposable
            {
                public void Dispose()
                {
                }

                public ValueTask DisposeAsync()
                {
                    return ValueTask.CompletedTask;
                }
            }
            """);
    }
}
