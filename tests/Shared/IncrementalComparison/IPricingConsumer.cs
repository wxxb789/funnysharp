using FunnySharp;
using FunnySharp.Incremental;

namespace FunnySharp.TestSupport.IncrementalComparison;

internal interface IPricingConsumer
{
    string Name { get; }

    PricingWork Work { get; }

    bool SupportsMembership { get; }

    IncrementalGraphStatistics? GraphStatistics { get; }

    IReadOnlyList<Result<Quote, PricingError>> Evaluate();

    void SetProductCost(int productIndex, decimal unitCost);

    void SetOrderQuantity(int orderIndex, int lineIndex, int quantity);

    void Batch(Action action);

    void AddOrder(Order order);

    void RemoveLastOrder();

    void ResetObservability();
}