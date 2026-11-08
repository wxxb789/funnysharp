using FunnySharp;
using FunnySharp.Incremental;

namespace FunnySharp.TestSupport.IncrementalComparison;

internal static class IncrementalPricingComparison
{
    private static int s_checks;

    internal static int Verify()
    {
        s_checks = 0;
        var products = PricingFixtures.CreateProducts();
        var orders = PricingFixtures.CreateOrders();
        var full = new FullRecomputePricingConsumer(products, orders);
        var hand = new HandIncrementalPricingConsumer(products, orders);
        var graph = new IncrementalPricingConsumer(products, orders);
        var collection = new CollectionNodePricingConsumer(products, orders);

        var initial = full.Evaluate();
        AssertSame(initial, hand, "initial hand");
        AssertSame(initial, graph, "initial graph");
        AssertSame(initial, collection, "initial collection graph");
        AssertGraphShape(graph);
        AssertGraphShape(collection);

        ResetAll(full, hand, graph, collection);
        full.SetProductCost(0, 120m);
        hand.SetProductCost(0, 120m);
        graph.SetProductCost(0, 120m);
        collection.SetProductCost(0, 120m);
        var productQuotes = full.Evaluate();
        AssertSame(productQuotes, hand, "product update hand");
        AssertSame(productQuotes, graph, "product update graph");
        AssertSame(productQuotes, collection, "product update collection");
        AssertWork(full, 512, 1024, 8192, "full product update");
        AssertWork(hand, 1, 16, 128, "hand product update");
        AssertWork(graph, 1, 16, 128, "graph product update");
        AssertWork(collection, 1, 1024, 8192, "collection product update");
        AssertStatistics(
            graph.GraphStatistics,
            commits: 1,
            inputChanges: 1,
            nodeEvaluations: 33,
            cutoffStops: 0,
            changedNodes: 34,
            observerNotifications: 0,
            nodeCount: 4096,
            edgeCount: 10752,
            observerCount: 0,
            "graph product statistics");
        AssertStatistics(
            collection.GraphStatistics,
            commits: 1,
            inputChanges: 1,
            nodeEvaluations: 3,
            cutoffStops: 0,
            changedNodes: 4,
            observerNotifications: 0,
            nodeCount: 1027,
            edgeCount: 1026,
            observerCount: 0,
            "collection product statistics");

        ResetAll(full, hand, graph, collection);
        full.SetOrderQuantity(5, 3, 7);
        hand.SetOrderQuantity(5, 3, 7);
        graph.SetOrderQuantity(5, 3, 7);
        collection.SetOrderQuantity(5, 3, 7);
        var quantityQuotes = full.Evaluate();
        AssertSame(quantityQuotes, hand, "quantity update hand");
        AssertSame(quantityQuotes, graph, "quantity update graph");
        AssertSame(quantityQuotes, collection, "quantity update collection");
        AssertWork(full, 512, 1024, 8192, "full quantity update");
        AssertWork(hand, 0, 1, 8, "hand quantity update");
        AssertWork(graph, 0, 1, 8, "graph quantity update");
        AssertWork(collection, 0, 1024, 8192, "collection quantity update");
        AssertStatistics(
            graph.GraphStatistics,
            commits: 1,
            inputChanges: 1,
            nodeEvaluations: 1,
            cutoffStops: 0,
            changedNodes: 2,
            observerNotifications: 0,
            nodeCount: 4096,
            edgeCount: 10752,
            observerCount: 0,
            "graph quantity statistics");
        AssertStatistics(
            collection.GraphStatistics,
            commits: 1,
            inputChanges: 1,
            nodeEvaluations: 1,
            cutoffStops: 0,
            changedNodes: 2,
            observerNotifications: 0,
            nodeCount: 1027,
            edgeCount: 1026,
            observerCount: 0,
            "collection quantity statistics");

        ResetAll(full, hand, graph, collection);
        full.Batch(() =>
        {
            full.SetProductCost(1, 130m);
            full.SetProductCost(2, 140m);
            full.SetOrderQuantity(10, 2, 4);
        });
        hand.Batch(() =>
        {
            hand.SetProductCost(1, 130m);
            hand.SetProductCost(2, 140m);
            hand.SetOrderQuantity(10, 2, 4);
        });
        graph.Batch(() =>
        {
            graph.SetProductCost(1, 130m);
            graph.SetProductCost(2, 140m);
            graph.SetOrderQuantity(10, 2, 4);
        });
        collection.Batch(() =>
        {
            collection.SetProductCost(1, 130m);
            collection.SetProductCost(2, 140m);
            collection.SetOrderQuantity(10, 2, 4);
        });
        var batchQuotes = full.Evaluate();
        AssertSame(batchQuotes, hand, "batch hand");
        AssertSame(batchQuotes, graph, "batch graph");
        AssertSame(batchQuotes, collection, "batch collection");
        AssertWork(full, 512, 1024, 8192, "full batch");
        AssertWork(hand, 2, 17, 136, "hand batch");
        AssertWork(graph, 2, 17, 136, "graph batch");
        AssertWork(collection, 2, 1024, 8192, "collection batch");
        AssertStatistics(
            graph.GraphStatistics,
            commits: 1,
            inputChanges: 3,
            nodeEvaluations: 35,
            cutoffStops: 0,
            changedNodes: 38,
            observerNotifications: 0,
            nodeCount: 4096,
            edgeCount: 10752,
            observerCount: 0,
            "graph batch statistics");

        ResetAll(full, hand, graph, collection);
        full.SetOrderQuantity(7, 0, 0);
        hand.SetOrderQuantity(7, 0, 0);
        graph.SetOrderQuantity(7, 0, 0);
        collection.SetOrderQuantity(7, 0, 0);
        var failureQuotes = full.Evaluate();
        AssertSame(failureQuotes, hand, "failure hand");
        AssertSame(failureQuotes, graph, "failure graph");
        AssertSame(failureQuotes, collection, "failure collection");
        AssertFailure(full, 7, "quantity");
        AssertFailure(hand, 7, "quantity");
        AssertFailure(graph, 7, "quantity");
        AssertFailure(collection, 7, "quantity");

        ResetAll(full, hand, graph, collection);
        full.SetOrderQuantity(7, 0, 1);
        hand.SetOrderQuantity(7, 0, 1);
        graph.SetOrderQuantity(7, 0, 1);
        collection.SetOrderQuantity(7, 0, 1);
        var recoveryQuotes = full.Evaluate();
        AssertSame(recoveryQuotes, hand, "failure recovery hand");
        AssertSame(recoveryQuotes, graph, "failure recovery graph");
        AssertSame(recoveryQuotes, collection, "failure recovery collection");

        ResetAll(hand, graph);
        hand.SetProductCost(3, products[3].UnitCost);
        graph.SetProductCost(3, products[3].UnitCost);
        AssertWork(hand, 0, 0, 0, "hand no-op");
        AssertWork(graph, 0, 0, 0, "graph no-op");
        AssertStatistics(
            graph.GraphStatistics,
            commits: 0,
            inputChanges: 0,
            nodeEvaluations: 0,
            cutoffStops: 0,
            changedNodes: 0,
            observerNotifications: 0,
            nodeCount: 4096,
            edgeCount: 10752,
            observerCount: 0,
            "graph no-op statistics");

        ResetAll(full, hand, collection);
        var additional = PricingFixtures.CreateAdditionalOrder(10_000);
        full.AddOrder(additional);
        hand.AddOrder(additional);
        collection.AddOrder(additional);
        var addQuotes = full.Evaluate();
        AssertSame(addQuotes, hand, "add order hand");
        AssertSame(addQuotes, collection, "add order collection");
        AssertWork(full, 512, 1025, 8200, "full add order");
        AssertWork(hand, 0, 1, 8, "hand add order");
        AssertWork(collection, 0, 1025, 8200, "collection add order");
        AssertThrowsNotSupported(() => graph.AddOrder(additional), "graph add order");

        ResetAll(full, hand, collection);
        full.RemoveLastOrder();
        hand.RemoveLastOrder();
        collection.RemoveLastOrder();
        var removeQuotes = full.Evaluate();
        AssertSame(removeQuotes, hand, "remove order hand");
        AssertSame(removeQuotes, collection, "remove order collection");
        AssertWork(full, 512, 1024, 8192, "full remove order");
        AssertWork(hand, 0, 0, 0, "hand remove order");
        AssertWork(collection, 0, 1024, 8192, "collection remove order");
        AssertThrowsNotSupported(() => graph.RemoveLastOrder(), "graph remove order");

        VerifyValidation(orders[0]);

        return s_checks;
    }

    private static void AssertGraphShape(IPricingConsumer consumer)
    {
        if (consumer.GraphStatistics is not { } statistics)
        {
            throw new InvalidOperationException($"{consumer.Name} did not expose graph statistics.");
        }

        if (statistics.NodeCount <= 0 || statistics.EdgeCount <= 0)
        {
            throw new InvalidOperationException($"{consumer.Name} has an empty graph.");
        }

        Check(true, $"{consumer.Name} graph shape");
    }

    private static void ResetAll(params IPricingConsumer[] consumers)
    {
        foreach (var consumer in consumers)
        {
            consumer.ResetObservability();
        }
    }

    private static void AssertSame(
        IReadOnlyList<Result<Quote, PricingError>> expectedQuotes,
        IPricingConsumer actual,
        string description)
    {
        var actualQuotes = actual.Evaluate();

        if (expectedQuotes.Count != actualQuotes.Count)
        {
            throw new InvalidOperationException(
                $"{description}: expected {expectedQuotes.Count} quotes, actual {actualQuotes.Count}.");
        }

        for (var index = 0; index < expectedQuotes.Count; index++)
        {
            if (!expectedQuotes[index].Equals(actualQuotes[index]))
            {
                throw new InvalidOperationException(
                    $"{description}: quote {index} differs. Expected {expectedQuotes[index]}, actual {actualQuotes[index]}.");
            }

            Check(true, $"{description} quote {index}");
        }
    }

    private static void AssertWork(
        IPricingConsumer consumer,
        int productEvaluations,
        int quoteEvaluations,
        int lineEvaluations,
        string description)
    {
        var work = consumer.Work;

        if (work.ProductEvaluations != productEvaluations
            || work.QuoteEvaluations != quoteEvaluations
            || work.LineEvaluations != lineEvaluations)
        {
            throw new InvalidOperationException(
                $"{description}: expected work ({productEvaluations}, {quoteEvaluations}, {lineEvaluations}), "
                + $"actual ({work.ProductEvaluations}, {work.QuoteEvaluations}, {work.LineEvaluations}).");
        }

        Check(true, description);
    }

    private static void AssertFailure(
        IPricingConsumer consumer,
        int orderIndex,
        string expectedCode)
    {
        if (!consumer.Evaluate()[orderIndex].TryGetError(out var error) || error is null)
        {
            throw new InvalidOperationException(
                $"Order {orderIndex} was expected to fail.");
        }

        if (error.Code != expectedCode)
        {
            throw new InvalidOperationException(
                $"Order {orderIndex} failed with '{error.Code}', expected '{expectedCode}'.");
        }

        Check(true, $"failure at order {orderIndex}");
    }

    private static void AssertStatistics(
        IncrementalGraphStatistics? actual,
        long commits,
        long inputChanges,
        long nodeEvaluations,
        long cutoffStops,
        long changedNodes,
        long observerNotifications,
        int nodeCount,
        int edgeCount,
        int observerCount,
        string description)
    {
        if (actual is not { } statistics)
        {
            throw new InvalidOperationException($"{description}: no statistics.");
        }

        if (statistics.Commits != commits
            || statistics.InputChanges != inputChanges
            || statistics.NodeEvaluations != nodeEvaluations
            || statistics.CutoffStops != cutoffStops
            || statistics.ChangedNodes != changedNodes
            || statistics.ObserverNotifications != observerNotifications
            || statistics.NodeCount != nodeCount
            || statistics.EdgeCount != edgeCount
            || statistics.ObserverCount != observerCount)
        {
            throw new InvalidOperationException(
                $"{description}: actual statistics {statistics}.");
        }

        Check(true, description);
    }

    private static void AssertThrowsNotSupported(Action action, string description)
    {
        try
        {
            action();
        }
        catch (NotSupportedException)
        {
            Check(true, description);
            return;
        }

        throw new InvalidOperationException($"{description}: expected NotSupportedException.");
    }

    private static void VerifyValidation(Order order)
    {
        var validation = PricingKernel.ValidateQuantities(order, [1, 2, 3, 0, 5, -1, 7, 8]);

        if (!validation.TryGetErrors(out var errors)
            || errors is not { Count: 2 }
            || errors[0].Code != "quantity"
            || errors[1].Code != "quantity")
        {
            throw new InvalidOperationException("Accumulating quantity validation did not preserve both errors.");
        }

        Check(true, "validation accumulation");
    }

    private static void Check(bool condition, string description)
    {
        if (!condition)
        {
            throw new InvalidOperationException(description);
        }

        s_checks++;
    }
}