using FunnySharp;
using FunnySharp.Incremental;
using FunnySharp.TestSupport.IncrementalComparison;

namespace FunnySharp.Incremental.Tests;

public sealed class IncrementalWorkloadTests
{
    [Fact]
    public void SharedPricingComparisonVerifiesAllConsumers()
    {
        var checks = IncrementalPricingComparison.Verify();

        Assert.True(checks > 20_000, $"Expected the complete verifier to run many quote checks; got {checks}.");
    }

    [Fact]
    public void PerOrderGraphRejectsTopologyChangesExplicitly()
    {
        var products = PricingFixtures.CreateProducts();
        var orders = PricingFixtures.CreateOrders();
        var graph = new IncrementalPricingConsumer(products, orders);
        var additional = PricingFixtures.CreateAdditionalOrder(10_000);

        Assert.False(graph.SupportsMembership);
        Assert.Throws<NotSupportedException>(() => graph.AddOrder(additional));
        Assert.Throws<NotSupportedException>(() => graph.RemoveLastOrder());
    }

    [Fact]
    public void CollectionNodeGraphRecomputesEveryQuoteForOneOrderUpdate()
    {
        var products = PricingFixtures.CreateProducts();
        var orders = PricingFixtures.CreateOrders();
        var collection = new CollectionNodePricingConsumer(products, orders);
        var perOrder = new IncrementalPricingConsumer(products, orders);

        collection.ResetObservability();
        perOrder.ResetObservability();
        collection.SetOrderQuantity(5, 3, 7);
        perOrder.SetOrderQuantity(5, 3, 7);

        Assert.Equal(1024, collection.Work.QuoteEvaluations);
        Assert.Equal(8192, collection.Work.LineEvaluations);
        Assert.Equal(1, perOrder.Work.QuoteEvaluations);
        Assert.Equal(8, perOrder.Work.LineEvaluations);
        Assert.Equal(1L, collection.GraphStatistics!.Value.NodeEvaluations);
        Assert.Equal(1L, perOrder.GraphStatistics!.Value.NodeEvaluations);
    }

    [Fact]
    public void DomainFailureRemainsAResultValueAndValidationAccumulatesErrors()
    {
        var products = PricingFixtures.CreateProducts();
        var orders = PricingFixtures.CreateOrders();
        var graph = new IncrementalPricingConsumer(products, orders);

        graph.SetOrderQuantity(7, 0, 0);

        Assert.True(graph.Evaluate()[7].TryGetError(out var error));
        Assert.Equal("quantity", error!.Code);

        var validation = PricingKernel.ValidateQuantities(orders[7], [1, 2, 3, 4, 0, -1, 7, 8]);

        Assert.True(validation.TryGetErrors(out var errors));
        Assert.Equal(2, errors!.Count);
    }

    [Fact]
    public void NoOpProductCostUpdateDoesNotCommitOrEvaluate()
    {
        var products = PricingFixtures.CreateProducts();
        var orders = PricingFixtures.CreateOrders();
        var graph = new IncrementalPricingConsumer(products, orders);

        graph.ResetObservability();
        graph.SetProductCost(0, products[0].UnitCost);

        Assert.Equal(0, graph.Work.ProductEvaluations);
        Assert.Equal(0, graph.Work.QuoteEvaluations);
        Assert.Equal(0L, graph.GraphStatistics!.Value.Commits);
    }
}