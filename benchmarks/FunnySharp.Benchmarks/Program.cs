using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using FunnySharp.Benchmarks;

if (args is ["--preflight"])
{
    var resultBenchmarks = new FunnySharp.Benchmarks.ResultBenchmarks();
    resultBenchmarks.Setup();
    await resultBenchmarks.ValidatePendingTransformSemanticsAsync().ConfigureAwait(false);
    Console.WriteLine("Benchmark semantic preflight passed.");
    return;
}

var config = ManualConfig.Create(DefaultConfig.Instance)
    .AddExporter(new AllocationReceiptExporter());
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
