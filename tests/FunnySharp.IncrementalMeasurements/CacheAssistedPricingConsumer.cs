using FunnySharp;
using FunnySharp.Incremental;
using FunnySharp.TestSupport.IncrementalComparison;
using Microsoft.Extensions.Caching.Memory;

namespace FunnySharp.IncrementalMeasurements;

internal sealed class CacheAssistedPricingConsumer : IPricingConsumer, IDisposable
{
    private readonly Product[] _products;
    private readonly List<Order> _orders;
    private readonly string[] _keys;
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    internal CacheAssistedPricingConsumer(
        IReadOnlyList<Product> products,
        IReadOnlyList<Order> orders)
    {
        _products = PricingFixtures.CloneProducts(products);
        _orders = PricingFixtures.CloneOrders(orders).ToList();
        _keys = new string[_products.Length];

        for (var index = 0; index < _products.Length; index++)
        {
            _keys[index] = Key(index);
            _cache.Set(_keys[index], PricingKernel.ComputeProduct(_products[index], Work));
        }
    }

    public string Name => "MemoryCache product cache";

    public PricingWork Work { get; } = new();

    public bool SupportsMembership => true;

    public IncrementalGraphStatistics? GraphStatistics => null;

    public IReadOnlyList<Result<Quote, PricingError>> Evaluate()
    {
        var economics = new ProductEconomics[_products.Length];

        for (var index = 0; index < economics.Length; index++)
        {
            economics[index] = _cache.GetOrCreate(
                _keys[index],
                _ => PricingKernel.ComputeProduct(_products[index], Work))!;
        }

        var quotes = new Result<Quote, PricingError>[_orders.Count];

        for (var index = 0; index < quotes.Length; index++)
        {
            quotes[index] = PricingKernel.ComputeQuote(_orders[index], economics, Work);
        }

        return quotes;
    }

    public void SetProductCost(int productIndex, decimal unitCost)
    {
        _products[productIndex] = _products[productIndex] with { UnitCost = unitCost };
        _cache.Remove(_keys[productIndex]);
    }

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

    public void Dispose() => _cache.Dispose();

    private static string Key(int productIndex) => $"product:{productIndex}";
}