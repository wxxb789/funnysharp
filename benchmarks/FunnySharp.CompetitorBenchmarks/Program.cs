using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using FunnySharp.CompetitorBenchmarks;

if (args is ["--preflight"])
{
    ValidateOptionEquivalence();
    ValidateResultEquivalence();
    Console.WriteLine("Competitor benchmark semantic preflight passed.");
    return;
}

var config = ManualConfig.Create(DefaultConfig.Instance)
    .AddExporter(new CompetitorReceiptExporter());
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

static void ValidateOptionEquivalence()
{
    var results = new OptionCarrierBenchmarks().ValidateEquivalence();

    AssertGroup("Map - present", 43, results[0..5]);
    AssertGroup("Map - absent", -1, results[5..10]);
    AssertGroup("Value-or-fallback - present", 42, results[10..15]);
    AssertGroup("Value-or-fallback - absent", -1, results[15..20]);
}

static void ValidateResultEquivalence()
{
    var results = new ResultCarrierBenchmarks().ValidateEquivalence();

    AssertGroup("Construction and inspection - success", 42, results[0..5]);
    AssertGroup("Construction and inspection - failure", -1, results[5..10]);
    AssertGroup("Fail-fast pipeline - success", 86, results[10..15]);
    AssertGroup("Fail-fast pipeline - failure", -1, results[15..20]);
}

static void AssertGroup(string scenario, int expected, ReadOnlySpan<int> results)
{
    if (results.ToArray().Any(result => result != expected))
    {
        throw new InvalidOperationException(
            $"Competitor comparison group '{scenario}' produced divergent results: [{string.Join(", ", results.ToArray())}], expected {expected}.");
    }
}
