using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using FunnySharp.Benchmarks;

if (args is ["--preflight"])
{
    new OptionCarrierBenchmarks().ValidateEquivalence();
    new ResultCarrierBenchmarks().ValidateEquivalence();
    Console.WriteLine("Competitor benchmark semantic preflight passed.");
    return;
}

var config = ManualConfig.Create(DefaultConfig.Instance)
    .AddExporter(new CompetitorReceiptExporter());
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

internal static class CompetitorPreflight
{
    internal static void ValidateGroup(
        List<string> validated,
        string scenario,
        int expected,
        params Func<int>[] paths)
    {
        foreach (var path in paths)
        {
            var result = path();
            if (result != expected)
            {
                throw new InvalidOperationException(
                    $"Competitor comparison group '{scenario}' produced a divergent result: {path.Method.Name} returned {result}, expected {expected}.");
            }

            validated.Add(path.Method.Name);
        }
    }

    internal static void ValidateCompleteness(Type benchmarkClass, List<string> validated)
    {
        var benchmarkMethods = benchmarkClass
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttribute<BenchmarkAttribute>() is not null)
            .Select(method => method.Name)
            .ToList();
        var unvalidated = benchmarkMethods.Except(validated, StringComparer.Ordinal).ToList();
        var unknown = validated.Except(benchmarkMethods, StringComparer.Ordinal).ToList();
        if (unvalidated.Count > 0 || unknown.Count > 0)
        {
            throw new InvalidOperationException(
                $"Competitor comparison preflight does not cover every benchmark of {benchmarkClass.Name}: unvalidated [{string.Join(", ", unvalidated)}], validated non-benchmarks [{string.Join(", ", unknown)}].");
        }
    }
}
