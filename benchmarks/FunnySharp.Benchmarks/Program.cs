using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using FunnySharp.Benchmarks;

await BenchmarkPreflight.RunAsync(
    typeof(OptionBenchmarks).Assembly,
    "eng/performance/baseline.json",
    190,
    11).ConfigureAwait(false);
if (args is ["--preflight"])
{
    Console.WriteLine(BenchmarkPreflight.Current.GetRawText());
    return;
}

var config = ManualConfig.Create(DefaultConfig.Instance)
    .WithOptions(ConfigOptions.KeepBenchmarkFiles)
    .AddJob(Job.Default.WithMsBuildArguments("/p:UseArtifactsOutput=false").AsMutator())
    .AddExporter(new AllocationReceiptExporter());
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
