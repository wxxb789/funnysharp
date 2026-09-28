using FunnySharp.VerticalSlice.Application;
using FunnySharp.VerticalSlice.Domain;
using FunnySharp.VerticalSlice.Endpoints;
using FunnySharp.VerticalSlice.Http;
using FunnySharp.VerticalSlice.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace FunnySharp.VerticalSlice;

/// <summary>
/// Composition root for the vertical slice. The application is built here once for the host and
/// once per test, so a test can replace any port without reaching into a built provider.
/// </summary>
public static class VerticalSliceApp
{
    public const string VerifyMarker = "FunnySharp ASP.NET Core vertical slice endpoints mapped.";

    public static bool IsVerifyRun(string[] args) => args is ["--verify"];

    public static WebApplication Build(
        string[] args,
        Action<IServiceCollection>? configureServices = null,
        Action<IWebHostBuilder>? configureHost = null)
    {
        var builder = WebApplication.CreateBuilder(IsVerifyRun(args) ? [] : args);
        configureHost?.Invoke(builder.WebHost);
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<UnexpectedExceptionHandler>();
        builder.Services.AddOpenApi();
        AddVerticalSlice(builder);
        configureServices?.Invoke(builder.Services);

        var app = builder.Build();
        app.UseExceptionHandler();
        app.MapOpenApi();
        app.MapOrderEndpoints();
        app.MapSupplierEndpoints();
        app.MapExportEndpoints();
        return app;
    }

    /// <summary>
    /// Registers the slice's ports and services. A later registration of the same service type
    /// replaces these defaults, which is how tests and measurements substitute dependencies.
    /// </summary>
    public static void AddVerticalSlice(WebApplicationBuilder builder)
    {
        var options = builder.Configuration.GetSection(VerticalSliceOptions.SectionName).Get<VerticalSliceOptions>()
            ?? new VerticalSliceOptions();
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IOrderStore, InMemoryOrderStore>();
        builder.Services.AddSingleton<IInventoryService, SimulatedInventoryService>();
        builder.Services.AddSingleton<IPaymentGateway, SimulatedPaymentGateway>();
        builder.Services.AddSingleton<IOrderEventPublisher, SimulatedOrderEventPublisher>();
        builder.Services.AddSingleton<ISupplierGateway, SimulatedSupplierGateway>();
        builder.Services.AddSingleton<OrderCommandExecutor>();
        builder.Services.AddSingleton<SupplierQuoting>();
        builder.Services.AddSingleton<ProblemMappings>();
        builder.Services.AddSingleton<OrderService>();
    }
}
