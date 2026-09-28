using Microsoft.AspNetCore.Diagnostics;

namespace FunnySharp.VerticalSlice.Baseline;

/// <summary>Fan-out and validation settings of the comparison application.</summary>
public sealed class BaselineOptions
{
    public const string SectionName = "Baseline";

    public int SupplierCount { get; set; } = 4;

    public int QuoteConcurrency { get; set; } = 2;

    public int MaximumOrderLines { get; set; } = 10;

    public string Currency { get; set; } = "EUR";
}

/// <summary>The comparison application: the same features with no FunnySharp reference.</summary>
public static class BaselineApp
{
    public const string VerifyMarker = "Baseline (idiomatic C#) vertical slice endpoints mapped.";

    public static bool IsVerifyRun(string[] args) => args is ["--verify"];

    public static WebApplication Build(
        string[] args,
        Action<IServiceCollection>? configureServices = null,
        Action<IWebHostBuilder>? configureHost = null)
    {
        var builder = WebApplication.CreateBuilder(IsVerifyRun(args) ? [] : args);
        configureHost?.Invoke(builder.WebHost);
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<BaselineExceptionHandler>();
        builder.Services.AddOpenApi();
        var options = builder.Configuration.GetSection(BaselineOptions.SectionName).Get<BaselineOptions>()
            ?? new BaselineOptions();
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<InMemoryOrderStore>();
        configureServices?.Invoke(builder.Services);

        var app = builder.Build();
        app.UseExceptionHandler();
        app.MapOpenApi();
        app.MapBaselineEndpoints();
        return app;
    }
}
