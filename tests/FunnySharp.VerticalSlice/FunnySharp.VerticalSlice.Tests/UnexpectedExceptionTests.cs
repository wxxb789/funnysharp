using System.Net;
using FunnySharp.VerticalSlice.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace FunnySharp.VerticalSlice.Tests;

/// <summary>
/// An unexpected exception is observed at the boundary and never becomes a domain outcome.
/// </summary>
public sealed class UnexpectedExceptionTests
{
    [Fact]
    public async Task AThrowingDependencyProducesAGenericFiveHundredAndIsLogged()
    {
        GateOrderStore? store = null;
        var logProvider = new CapturingLoggerProvider();
        await using var host = await SliceHost.StartAsync(
            services =>
            {
                services.AddSingleton<IOrderStore>(store!);
                services.AddSingleton<Microsoft.Extensions.Logging.ILoggerProvider>(logProvider);
            },
            options => store = new GateOrderStore(options));
        store!.OnFind = static (_, _) => throw new InvalidOperationException("ledger corruption");

        using var response = await host.Client.GetAsync("/orders/ORD-1");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var problem = await response.ReadJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("https://funnysharp.example/problems/unexpected-error", problem.Type);
        Assert.DoesNotContain(
            "funnysharp.example/problems/order-not-found",
            problem.Type,
            StringComparison.Ordinal);
        var logged = Assert.Single(logProvider.Exceptions);
        Assert.IsType<InvalidOperationException>(logged);
        Assert.Equal("ledger corruption", logged.Message);
    }

    [Fact]
    public async Task ADomainFailureNeverBecomesAnUnexpectedError()
    {
        await using var host = await SliceHost.StartAsync();

        using var response = await host.Client.GetAsync("/orders/ORD-123456");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.ReadJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.NotEqual("https://funnysharp.example/problems/unexpected-error", problem.Type);
    }
}
