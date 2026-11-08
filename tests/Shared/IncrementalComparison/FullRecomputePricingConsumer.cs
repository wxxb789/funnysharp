using FunnySharp;
using FunnySharp.Incremental;

namespace FunnySharp.TestSupport.IncrementalComparison;

internal sealed class FullRecomputePricingConsumer : IPricingConsumer
{
    private readonly Product[] _products;
    private readonly List<Order> _orders;

    internal FullRecomputePricingConsumer(
        IReadOnlyList<Product> products,
        IReadOnlyList<Order> orders)
    {
        _products = PricingFixtures.CloneProducts(products);
        _orders = PricingFixtures.CloneOrders(orders).ToList();
    }

    public string Name => "Full recomputation";

    public PricingWork Work { get; } = new();

    public bool SupportsMembership => true;

    public IncrementalGraphStatistics? GraphStatistics => null;

    public IReadOnlyList<Result<Quote, PricingError>> Evaluate()
    {
        var economics = new ProductEconomics[_products.Length];

        for (var index = 0; index < economics.Length; index++)
        {
            economics[index] = PricingKernel.ComputeProduct(_products[index], Work);
        }

        var quotes = new Result<Quote, PricingError>[_orders.Count];

        for (var index = 0; index < quotes.Length; index++)
        {
            quotes[index] = PricingKernel.ComputeQuote(_orders[index], economics, Work);
        }

        return quotes;
    }

    public void SetProductCost(int productIndex, decimal unitCost) =>
        _products[productIndex] = _products[productIndex] with { UnitCost = unitCost };

    public void SetOrderQuantity(int orderIndex, int lineIndex, int quantity) =>
        _orders[orderIndex] = PricingFixtures.ReplaceQuantity(
            _orders[orderIndex],
            lineIndex,
            quantity);

    public void Batch(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        action();
    }

    public void AddOrder(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        _orders.Add(order);
    }

    public void RemoveLastOrder()
    {
        if (_orders.Count == 0)
        {
            throw new InvalidOperationException("The order list is empty.");
        }

        _orders.RemoveAt(_orders.Count - 1);
    }

    public void ResetObservability() => Work.Reset();
}