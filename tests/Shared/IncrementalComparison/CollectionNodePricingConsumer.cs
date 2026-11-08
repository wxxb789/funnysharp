using FunnySharp;
using FunnySharp.Incremental;

namespace FunnySharp.TestSupport.IncrementalComparison;

internal sealed class CollectionNodePricingConsumer : IPricingConsumer
{
    private readonly IncrementalGraph _graph = new();
    private readonly Input<Product>[] _productInputs;
    private readonly IncrementalNode<ProductEconomics>[] _productNodes;
    private readonly IncrementalNode<IReadOnlyList<ProductEconomics>> _productValues;
    private readonly Input<IReadOnlyList<Order>> _ordersInput;
    private readonly IncrementalNode<IReadOnlyList<Result<Quote, PricingError>>> _quotesNode;

    internal CollectionNodePricingConsumer(
        IReadOnlyList<Product> products,
        IReadOnlyList<Order> orders)
    {
        var clonedProducts = PricingFixtures.CloneProducts(products);
        var clonedOrders = PricingFixtures.CloneOrders(orders);
        _productInputs = new Input<Product>[clonedProducts.Length];
        _productNodes = new IncrementalNode<ProductEconomics>[clonedProducts.Length];

        var work = Work;
        var productCompute = Pure<Product, ProductEconomics>.Of(
            product => PricingKernel.ComputeProduct(product, work));
        var quoteCompute =
            Pure<(IReadOnlyList<Order> Orders, IReadOnlyList<ProductEconomics> Products),
                IReadOnlyList<Result<Quote, PricingError>>>.Of(
                pair => ComputeQuotes(pair.Orders, pair.Products, work));

        for (var index = 0; index < clonedProducts.Length; index++)
        {
            _productInputs[index] = _graph.Input($"product[{index}]", clonedProducts[index]);
            _productNodes[index] = _graph.Map(
                $"product-economics[{index}]",
                _productInputs[index],
                productCompute);
        }

        _productValues = _graph.Collect("product-values", _productNodes);
        _ordersInput = _graph.Input<IReadOnlyList<Order>>("orders", clonedOrders);
        _quotesNode = _graph.Combine(
            "quotes",
            _ordersInput,
            _productValues,
            quoteCompute);
    }

    public string Name => "Collection-as-one-node graph";

    public PricingWork Work { get; } = new();

    public bool SupportsMembership => true;

    public IncrementalGraphStatistics? GraphStatistics => _graph.Statistics;

    public IReadOnlyList<Result<Quote, PricingError>> Evaluate() => _quotesNode.Value;

    public void SetProductCost(int productIndex, decimal unitCost)
    {
        var current = _productInputs[productIndex].Value;
        _productInputs[productIndex].Set(current with { UnitCost = unitCost });
    }

    public void SetOrderQuantity(int orderIndex, int lineIndex, int quantity)
    {
        var current = _ordersInput.Value;
        var replacement = PricingFixtures.CloneOrders(current);
        replacement[orderIndex] = PricingFixtures.ReplaceQuantity(
            replacement[orderIndex],
            lineIndex,
            quantity);
        _ordersInput.Set(replacement);
    }

    public void Batch(Action action) => _graph.Batch(action);

    public void AddOrder(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        var current = _ordersInput.Value;
        var replacement = PricingFixtures.CloneOrders(current);
        _ordersInput.Set([.. replacement, order]);
    }

    public void RemoveLastOrder()
    {
        var current = _ordersInput.Value;

        if (current.Count == 0)
        {
            throw new InvalidOperationException("The order list is empty.");
        }

        var replacement = new Order[current.Count - 1];

        for (var index = 0; index < replacement.Length; index++)
        {
            replacement[index] = current[index];
        }

        _ordersInput.Set(replacement);
    }

    public void ResetObservability()
    {
        Work.Reset();
        _graph.ResetStatistics();
    }

    private static IReadOnlyList<Result<Quote, PricingError>> ComputeQuotes(
        IReadOnlyList<Order> orders,
        IReadOnlyList<ProductEconomics> products,
        PricingWork work)
    {
        var quotes = new Result<Quote, PricingError>[orders.Count];

        for (var index = 0; index < quotes.Length; index++)
        {
            quotes[index] = PricingKernel.ComputeQuote(orders[index], products, work);
        }

        return quotes;
    }
}