using System.Text.Json;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Validators;

namespace FunnySharp.Benchmarks;

internal sealed class RepositoryProjectToolchain : Toolchain
{
    private static readonly IToolchain DefaultToolchain = CsProjCoreToolchain.NetCoreApp10_0;

    private RepositoryProjectToolchain(string projectPath)
        : base(DefaultToolchain.Name, new ProjectGenerator(projectPath), DefaultToolchain.Builder, DefaultToolchain.Executor)
    {
    }

    internal static IToolchain Create()
    {
        var root = ReceiptExporterCore.FindRepositoryRoot();
        var manifestPath = Environment.GetEnvironmentVariable("FUNNYSHARP_PERFORMANCE_MANIFEST")!;
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, manifestPath)));
        var project = manifest.RootElement.GetProperty("runBinding").GetProperty("benchmarkProject").GetString()!;
        return new RepositoryProjectToolchain(Path.Combine(root, project));
    }

    public override IEnumerable<ValidationError> Validate(BenchmarkCase benchmarkCase, IResolver resolver) =>
        DefaultToolchain.Validate(benchmarkCase, resolver);

    // The default solution-wide search also finds projects in nested agent worktrees.
    private sealed class ProjectGenerator(string projectPath) : CsProjGenerator(
        NetCoreAppSettings.NetCoreApp10_0.TargetFrameworkMoniker,
        NetCoreAppSettings.NetCoreApp10_0.CustomDotNetCliPath ?? "dotnet",
        NetCoreAppSettings.NetCoreApp10_0.PackagesPath ?? string.Empty,
        NetCoreAppSettings.NetCoreApp10_0.RuntimeFrameworkVersion ?? string.Empty)
    {
        protected override FileInfo GetProjectFilePath(Type benchmarkTarget, ILogger logger) => new(projectPath);
    }
}
