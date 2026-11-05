using System.Text.Json;
using FunnySharp.TestSupport.Domains;

namespace FunnySharp.Tests;

public sealed class DomainConsumerComparisonTests
{
    [Fact]
    public void FunnySharpAndOrdinaryWorkflowsAgreeOnEveryFixture() =>
        DomainComparison.Verify();

    [Fact]
    public void ModuleFailuresKeepTheirConcretePayloadWhenMappedToTheWorkflowHierarchy()
    {
        using var document = JsonDocument.Parse(DomainFixtures.UnknownSku.Json);
        var decoded = FunnySharpOrderWorkflow.DraftDecoder.Decode(document.RootElement);
        Assert.True(decoded.TryGetValue(out var draft));

        var submitted = draft!.Submit();
        Assert.True(submitted.TryGetError(out var failure));
        var pricing = Assert.IsType<PricingRejected>(failure);
        Assert.Equal("pricing", pricing.Source);
        Assert.Equal("unknown-sku", pricing.Code);
        Assert.Equal("SKU-UNKNOWN", pricing.Failure.Sku);
        Assert.Equal(2, pricing.Failure.Quantity);
    }

    [Fact]
    public void OrdinaryStateGuardsFailAtRuntimeForTheOperationsDomainStateTypesOmit()
    {
        var draft = new OrdinaryOrder(
            "ORD-100",
            "CUS-100",
            "SKU-BOOK",
            2,
            OrdinaryOrderState.Draft,
            0m,
            null,
            null,
            null,
            null);

        var shipped = draft.Ship("TRK-ORD-100");
        Assert.True(shipped.TryGetError(out var shipError));
        var stateError = Assert.IsType<OrdinaryStateRejected>(shipError);
        Assert.Equal("invalid-state", stateError.Code);

        var placed = draft with { State = OrdinaryOrderState.Placed };
        var invalidTracking = placed.Ship("not-a-tracking-code");
        Assert.True(invalidTracking.TryGetError(out var trackingError));
        Assert.Equal(
            "invalid-tracking",
            Assert.IsType<OrdinaryStateRejected>(trackingError).Code);

        var shippedOrder = placed with { State = OrdinaryOrderState.Shipped };
        var cancelled = shippedOrder.Cancel("customer-request");
        Assert.True(cancelled.TryGetError(out var cancelError));
        Assert.Equal("invalid-state", Assert.IsType<OrdinaryStateRejected>(cancelError).Code);
    }
}
