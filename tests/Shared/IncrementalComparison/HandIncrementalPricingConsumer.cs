using FunnySharp;
using FunnySharp.Incremental;

namespace FunnySharp.TestSupport.IncrementalComparison;

internal sealed class HandIncrementalPricingConsumer : IPricingConsumer
{
    private readonly Product[] _products;
    private readonly List<Order> _orders;
    private readonly ProductEconomics[] _economics;
    private readonly List<Result<Quote, PricingError>> _quotes;
    private readonly List<int>[] _productOrders;
    private readonly Dictionary<int, decimal> _pendingProductCosts = [];
    private readonly Dictionary<(int OrderIndex, int LineIndex), int> _pendingQuantities = [];
    private readonly List<int> _dirtyOrders = [];
    private int[] _dirtyStamp;
    private int _commitStamp;
    private int _batchDepth;

    internal HandIncrementalPricingConsumer(
        IReadOnlyList<Product> products,
        IReadOnlyList<Order> orders)
    {
        _products = PricingFixtures.CloneProducts(products);
        _orders = PricingFixtures.CloneOrders(orders).ToList();
        _economics = new ProductEconomics[_products.Length];
        _quotes = new List<Result<Quote, PricingError>>(_orders.Count);
        _productOrders = new List<int>[_products.Length];
        _dirtyStamp = new int[_orders.Count];

        for (var index = 0; index < _productOrders.Length; index++)
        {
            _productOrders[index] = [];
        }

        for (var index = 0; index < _economics.Length; index++)
        {
            _economics[index] = PricingKernel.ComputeProduct(_products[index], Work);
        }

        BuildReverseIndex();

        for (var index = 0; index < _orders.Count; index++)
        {
            _quotes.Add(PricingKernel.ComputeQuote(_orders[index], _economics, Work));
        }
    }

    public string Name => "Hand-maintained incremental";

    public PricingWork Work { get; } = new();

    public bool SupportsMembership => true;

    public IncrementalGraphStatistics? GraphStatistics => null;

    public IReadOnlyList<Result<Quote, PricingError>> Evaluate() => _quotes;

    public void SetProductCost(int productIndex, decimal unitCost) =>
        Batch(() => _pendingProductCosts[productIndex] = unitCost);

    public void SetOrderQuantity(int orderIndex, int lineIndex, int quantity) =>
        Batch(() => _pendingQuantities[(orderIndex, lineIndex)] = quantity);

    public void Batch(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _batchDepth++;

        try
        {
            action();
        }
        catch
        {
            if (--_batchDepth == 0)
            {
                _pendingProductCosts.Clear();
                _pendingQuantities.Clear();
            }

            throw;
        }

        if (--_batchDepth == 0)
        {
            Commit();
        }
    }

    public void AddOrder(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (_batchDepth != 0)
        {
            throw new InvalidOperationException(
                "The research baseline does not batch order membership changes.");
        }

        EnsureProductReferences(order);
        _orders.Add(order);
        _quotes.Add(PricingKernel.ComputeQuote(order, _economics, Work));
        AddToReverseIndex(order, _orders.Count - 1);
        Array.Resize(ref _dirtyStamp, _orders.Count);
    }

    public void RemoveLastOrder()
    {
        if (_batchDepth != 0)
        {
            throw new InvalidOperationException(
                "The research baseline does not batch order membership changes.");
        }

        if (_orders.Count == 0)
        {
            throw new InvalidOperationException("The order list is empty.");
        }

        var orderIndex = _orders.Count - 1;
        var order = _orders[orderIndex];

        foreach (var line in order.Lines)
        {
            _productOrders[line.ProductIndex].Remove(orderIndex);
        }

        _quotes.RemoveAt(orderIndex);
        _orders.RemoveAt(orderIndex);
    }

    public void ResetObservability() => Work.Reset();

    private void Commit()
    {
        if (_pendingProductCosts.Count == 0 && _pendingQuantities.Count == 0)
        {
            return;
        }

        _dirtyOrders.Clear();
        _commitStamp++;

        foreach (var pair in _pendingProductCosts)
        {
            var product = _products[pair.Key];

            if (product.UnitCost == pair.Value)
            {
                continue;
            }

            _products[pair.Key] = product with { UnitCost = pair.Value };
            _economics[pair.Key] = PricingKernel.ComputeProduct(_products[pair.Key], Work);

            foreach (var orderIndex in _productOrders[pair.Key])
            {
                MarkDirty(orderIndex);
            }
        }

        foreach (var pair in _pendingQuantities)
        {
            var order = _orders[pair.Key.OrderIndex];
            var line = order.Lines[pair.Key.LineIndex];

            if (line.Quantity == pair.Value)
            {
                continue;
            }

            _orders[pair.Key.OrderIndex] = PricingFixtures.ReplaceQuantity(
                order,
                pair.Key.LineIndex,
                pair.Value);

            MarkDirty(pair.Key.OrderIndex);
        }

        foreach (var orderIndex in _dirtyOrders)
        {
            var quote = PricingKernel.ComputeQuote(_orders[orderIndex], _economics, Work);

            if (!_quotes[orderIndex].Equals(quote))
            {
                _quotes[orderIndex] = quote;
            }
        }

        _pendingProductCosts.Clear();
        _pendingQuantities.Clear();
    }

    private void MarkDirty(int orderIndex)
    {
        if (_dirtyStamp[orderIndex] == _commitStamp)
        {
            return;
        }

        _dirtyStamp[orderIndex] = _commitStamp;
        _dirtyOrders.Add(orderIndex);
    }

    private void BuildReverseIndex()
    {
        var seen = new bool[_products.Length];
        var touched = new List<int>();

        for (var orderIndex = 0; orderIndex < _orders.Count; orderIndex++)
        {
            foreach (var line in _orders[orderIndex].Lines)
            {
                if (!seen[line.ProductIndex])
                {
                    seen[line.ProductIndex] = true;
                    touched.Add(line.ProductIndex);
                    _productOrders[line.ProductIndex].Add(orderIndex);
                }
            }

            foreach (var productIndex in touched)
            {
                seen[productIndex] = false;
            }

            touched.Clear();
        }
    }

    private void AddToReverseIndex(Order order, int orderIndex)
    {
        var seen = new bool[_products.Length];

        foreach (var line in order.Lines)
        {
            if (seen[line.ProductIndex])
            {
                continue;
            }

            seen[line.ProductIndex] = true;
            _productOrders[line.ProductIndex].Add(orderIndex);
        }
    }

    private void EnsureProductReferences(Order order)
    {
        foreach (var line in order.Lines)
        {
            if ((uint)line.ProductIndex >= (uint)_products.Length)
            {
                throw new ArgumentException(
                    $"The order references product index {line.ProductIndex}.",
                    nameof(order));
            }
        }
    }
}