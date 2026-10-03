## Warehouse availability and reservation

Define global AvailabilityCoordinator with these seams:

    public static Task<IReadOnlyList<ItemAvailability>> CheckAvailabilityAsync(
        IReadOnlyList<StockItem> items, WarehouseGateway gateway, int maxConcurrency,
        CancellationToken cancellationToken)
    public static Task<ReservationOutcome> ReserveFirstAsync(
        IReadOnlyList<string> suppliers, SupplierGateway gateway, CancellationToken cancellationToken)

Use the supplied neutral Contract.cs; do not redefine its types. Availability checks must
overlap up to maxConcurrency, never exceed it, check each item once and return input-ordered
Sku/OnHand/Sufficient (OnHand >= Quantity). Empty inputs start nothing. Cancellation during
work must propagate and all started calls must exit before the coordinator returns.

Reservation inputs in this study contain at most 32 suppliers. Start the probes concurrently;
the first accepting reply observed wins. Already-completed successes are resolved in input
order. After selection cancel remaining calls, drain every started call, and preserve that
winner even if a canceled loser returns a late success. Independent loser/cleanup faults
propagate instead of being hidden by success. All declines produce Reserved=false, null IDs
and every FailedSuppliers entry in input order. Success has empty FailedSuppliers. Empty
inputs produce no reservation and start nothing. Caller cancellation before publication
must propagate; owned linked tokens are allowed and must propagate caller cancellation.

Fakes expose test-controlled entered/release/canceled/cleanup gates; they do not delay for
time. Returned carries the exact gateway task: awaiting that task observes its terminal
state after cleanup and active-count accounting, unlike CleanupEntered. This is observation
only; coordinators do not use these test signals. Gateway side effects and active counts
are observable. Never replace the
gateway calls with guessed answers, skip calls, double-call, return while work remains, or
swallow independent faults. Both styles have this same cancel-and-drain workload. This
condition is new; it does not describe the historical concurrency study.
