namespace FunnySharp.Analyzers.Tests;

using Microsoft.CodeAnalysis;

public sealed class DomainMisuseContractTests
{
    [Fact]
    public void ConfusableDomainValuesDoNotAssignToEachOther()
    {
        var diagnostic = SingleDiagnostic("""
            using FunnySharp;
            public readonly struct OrderIdTag;
            public readonly struct CustomerIdTag;
            public static class Probe
            {
                public static void Assign(DomainValue<string, OrderIdTag> orderId)
                {
                    DomainValue<string, CustomerIdTag> customerId = orderId;
                }
            }
            """);

        Assert.Equal("CS0029", diagnostic.Id);
        Assert.Contains("CustomerIdTag", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("OrderIdTag", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void RawUnderlyingValuesCannotCrossTheDomainBoundaryImplicitly()
    {
        var diagnostic = SingleDiagnostic("""
            using FunnySharp;
            public readonly struct OrderIdTag;
            public static class Probe
            {
                public static void Take(DomainValue<string, OrderIdTag> orderId)
                {
                }

                public static void Call() => Take("ORD-1");
            }
            """);

        Assert.Equal("CS1503", diagnostic.Id);
        Assert.Contains("string", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("OrderIdTag", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void TheValidatedConstructorIsNotPubliclyCallable()
    {
        var diagnostic = SingleDiagnostic("""
            using FunnySharp;
            public readonly struct OrderIdTag;
            public static class Probe
            {
                public static DomainValue<string, OrderIdTag> Forge() =>
                    new DomainValue<string, OrderIdTag>("ORD-1");
            }
            """);

        Assert.Equal("CS1729", diagnostic.Id);
        Assert.Contains("DomainValue", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    private static Diagnostic SingleDiagnostic(string source)
    {
        var compilation = AnalyzerHarness.CreateCompilation(source);
        var diagnostics = compilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        return Assert.Single(diagnostics);
    }
}
