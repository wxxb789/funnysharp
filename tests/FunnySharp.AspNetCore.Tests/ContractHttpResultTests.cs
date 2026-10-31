using System.Net;
using System.Text.Json;
using FunnySharp;
using FunnySharp.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;

namespace FunnySharp.AspNetCore.Tests;

public sealed class ContractHttpResultTests
{
    [Fact]
    public async Task ContractValidationErrorsMapThroughTheExistingHttpBoundary()
    {
        var fields = Contract.For<ContractPayload>();
        var contract = Contract.Object(
            fields.Field("name", Contract.NonEmptyString, static payload => payload.Name),
            static name => new ContractPayload(name));

        var validation = contract.Validate(new ContractPayload(string.Empty));

        await using var host = await TestApplication.StartAsync(app =>
            app.MapGet("/contract", () => validation.ToHttpResult(
                errors => new HttpValidationProblemDetails(
                    errors
                        .GroupBy(static error => error.Path, StringComparer.Ordinal)
                        .ToDictionary(
                            static group => group.Key,
                            static group => group
                                .Select(error => error.Detail ?? error.Expected ?? error.Kind.ToString())
                                .ToArray(),
                            StringComparer.Ordinal))
                {
                    Status = StatusCodes.Status400BadRequest,
                })));

        using var response = await host.Client.GetAsync("/contract", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var errors = document.RootElement.GetProperty("errors");
        Assert.Equal("a non-empty string", errors.GetProperty("name")[0].GetString());
    }

    private sealed record ContractPayload(string Name);

    private sealed class TestApplication(WebApplication application, HttpClient client) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;

        public static async Task<TestApplication> StartAsync(Action<WebApplication> configure)
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseTestServer();
            var application = builder.Build();
            configure(application);
            await application.StartAsync(TestContext.Current.CancellationToken);
            return new TestApplication(application, application.GetTestClient());
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await application.StopAsync(TestContext.Current.CancellationToken);
            await application.DisposeAsync();
        }
    }
}
