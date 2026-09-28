using FunnySharp.VerticalSlice.Domain;

namespace FunnySharp.VerticalSlice.Application;

/// <summary>The explicit environment an environment-dependent effect requires.</summary>
public sealed record ReconciliationEnvironment(
    IOrderStore Store,
    IInventoryService Inventory,
    int MaxConcurrency);

/// <summary>One line of the reconciliation report.</summary>
public sealed record ReconciliationLine(string Sku, int Ordered, int Available);

/// <summary>The reconciliation report recomputed from history and current stock.</summary>
public sealed record ReconciliationReport(
    OrderId OrderId,
    OrderStatus Status,
    int Revision,
    bool HistoryReplaysToStoredProjection,
    IReadOnlyList<ReconciliationLine> Lines);
