using FunnySharp;
using FunnySharp.Incremental;

namespace FunnySharp.TestSupport.IncrementalComparison;

internal sealed class IncrementalPricingConsumer : IPricingConsumer
{
    private readonly IncrementalGraph _graph = new();
    private readonly Input<Product>[] _productInputs;
    private readonly IncrementalNode<ProductEconomics>[] _productNodes;
    private readonly Input<Order>[] _orderInputs;
    private readonly IncrementalNode<Result<Quote, PricingError>>[] _quoteNodes;
    private readonly NodeListView<Result<Quote, PricingError>> _quotesView;

    internal IncrementalPricingConsumer(
        IReadOnlyList<Product> products,
        IReadOnlyList<Order> orders)
    {
        var clonedProducts = PricingFixtures.CloneProducts(products);
        var clonedOrders = PricingFixtures.CloneOrders(orders);
        _productInputs = new Input<Product>[clonedProducts.Length];
        _productNodes = new IncrementalNode<ProductEconomics>[clonedProducts.Length];
        _orderInputs = new Input<Order>[clonedOrders.Length];
        _quoteNodes = new IncrementalNode<Result<Quote, PricingError>>[clonedOrders.Length];

        var work = Work;
        var productCompute = Pure<Product, ProductEconomics>.Of(
            product => PricingKernel.ComputeProduct(product, work));
        var quoteCompute =
            Pure<(Order Order, IReadOnlyList<ProductEconomics> Products), Result<Quote, PricingError>>.Of(
                pair => PricingKernel.ComputeQuoteFromLineProducts(pair.Order, pair.Products, work));

        for (var index = 0; index < clonedProducts.Length; index++)
        {
            _productInputs[index] = _graph.Input($"product[{index}]", clonedProducts[index]);
            _productNodes[index] = _graph.Map(
                $"product-economics[{index}]",
                _productInputs[index],
                productCompute);
        }

        for (var orderIndex = 0; orderIndex < clonedOrders.Length; orderIndex++)
        {
            var order = clonedOrders[orderIndex];
            var sources = new IncrementalNode<ProductEconomics>[order.Lines.Count];

            for (var lineIndex = 0; lineIndex < sources.Length; lineIndex++)
            {
                sources[lineIndex] = _productNodes[order.Lines[lineIndex].ProductIndex];
            }

            _orderInputs[orderIndex] = _graph.Input($"order[{orderIndex}]", order);
            var collected = _graph.Collect($"order-products[{orderIndex}]", sources);
            _quoteNodes[orderIndex] = _graph.Combine(
                $"quote[{orderIndex}]",
                _orderInputs[orderIndex],
                collected,
                quoteCompute);
        }

        _quotesView = new NodeListView<Result<Quote, PricingError>>(_quoteNodes);
    }

    public string Name => "Per-order incremental graph";

    public PricingWork Work { get; } = new();

    public bool SupportsMembership => false;

    public IncrementalGraphStatistics? GraphStatistics => _graph.Statistics;

    public IReadOnlyList<Result<Quote, PricingError>> Evaluate() => _quotesView;

    public void SetProductCost(int productIndex, decimal unitCost)
    {
        var input = _productInputs[productIndex];
        input.Set(input.PendingOrPublishedValue with { UnitCost = unitCost });
    }

    public void SetOrderQuantity(int orderIndex, int lineIndex, int quantity)
    {
        var input = _orderInputs[orderIndex];
        input.Set(PricingFixtures.ReplaceQuantity(input.PendingOrPublishedValue, lineIndex, quantity));
    }

    public void Batch(Action action) => _graph.Batch(action);

    public void AddOrder(Order order) =>
        throw new NotSupportedException(
            "The per-order graph has a fixed topology. Build a new graph to add or remove an order.");

    public void RemoveLastOrder() =>
        throw new NotSupportedException(
            "The per-order graph has a fixed topology. Build a new graph to add or remove an order.");

    public void ResetObservability()
    {
        Work.Reset();
        _graph.ResetStatistics();
    }
}