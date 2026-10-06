## Business brief: warehouse availability coordination

You are implementing the availability-coordination workflow of a small fulfillment service as a
library consumed by a fixed xUnit test suite. The test suite and the neutral domain contract are
already provided in the build directory; you implement the workflow behind the seam below and
the tests must pass.

Seam (you must define this exactly):

public static class AvailabilityCoordinator
{
    public static Task<IReadOnlyList<ItemAvailability>> CheckAvailabilityAsync(
        IReadOnlyList<StockItem> items,
        WarehouseGateway gateway,
        int maxConcurrency,
        CancellationToken cancellationToken)

    public static Task<ReservationOutcome> ReserveFirstAsync(
        IReadOnlyList<string> suppliers,
        SupplierGateway gateway,
        CancellationToken cancellationToken)
}

Declare your types in the global namespace (no namespace declaration) in the files you write.
The provided contract defines StockItem, ItemAvailability, WarehouseStock, WarehouseReply,
WarehouseGateway, SupplierOffer, SupplierReply, SupplierGateway, and ReservationOutcome; do not
redefine them. The provided fakes are deterministic:

- WarehouseGateway.CheckAsync(sku, token) records the start and waits for the test suite to
  release that input, honoring the token throughout the wait. It then answers with the sku's
  OnHand, or OnHand 0 for an unknown sku. The gateway counts StartedChecks and tracks
  MaxInFlightChecks, the largest number of checks observed in flight at once. The tests use
  CheckEntered/CheckCompleted and ReleaseCheck to observe and control completion order without
  elapsed-time assumptions; the workflow only calls CheckAsync.
- SupplierGateway.ProbeAsync(supplierId, token) records the start and waits for the test suite
  to release that input, honoring the token. It then answers with that supplier's Accepts flag
  and ReservationId. The gateway counts StartedProbes and tracks MaxInFlightProbes. The tests
  use ProbeEntered/ProbeCompleted and ReleaseProbe to control replies; the workflow only calls
  ProbeAsync. Delay fields and timer-based scheduling are not part of either fake.

Business rules:

1. Availability checks - check every item through the warehouse gateway with bounded
   parallelism:
   - at most maxConcurrency checks are in flight at any time (maxConcurrency is at least 1; with
     1 the checks run one at a time);
   - when there are more items than the bound, the checks must actually overlap: the gateway
     observes more than one check in flight;
   - the results come back in source order: the first item's answer is the first result even
     when the first item's check finishes last;
   - every result carries the item's Sku, the gateway's OnHand for that sku, and
     Sufficient = OnHand >= the item's Quantity;
   - an empty item list returns an empty result list and starts no checks.
2. First-success reservation - reserve from the first supplier that accepts:
   - start every supplier's probe: with several suppliers the probes overlap, and a declining
     probe never stops the search (every supplier is probed even when an early one declines
     quickly);
   - the first accepting reply observed wins: Reserved=true with that supplier's SupplierId and
     ReservationId, and FailedSuppliers is empty;
   - when every supplier declines: Reserved=false, SupplierId and ReservationId are null, and
     FailedSuppliers lists every supplier in input order (not completion order);
   - an empty supplier list returns Reserved=false with an empty FailedSuppliers list and starts
     no probes.
3. Cancellation - both methods honor the token and pass it into every gateway call: a token
   cancelled while checks or probes are in flight surfaces OperationCanceledException
   (TaskCanceledException counts as one).

Requirements:

- Correctness under the provided tests is the acceptance bar; the tests are visible to you.
- Keep the bounded-parallelism, source-order, first-success, and cancellation semantics explicit
  and readable: a maintainer should see at a glance how the concurrency is bounded and how the
  reservation winner is chosen.
- Do not read or copy the FunnySharp repository source code.

## Style: idiomatic C# (BCL only)

Use .NET 10 base-class-library C# only: no third-party libraries or packages (the project file
references none). Express the workflow with idiomatic C#: nullable annotations, ordinary
collections and LINQ, exceptions or early returns where natural, and records. There is no
requirement to avoid exceptions or try patterns; choose what a careful .NET maintainer would
write today.

## Delivery

Write every source file you need into the solution/ directory next to this prompt. Only files
under solution/ are copied into the build; the tests and contract are fixed. Compile with:

    dotnet fsi build.fsx -- -p eval-verify --task concurrency --style `STYLE` --run-dir <run-directory>

where <run-directory> contains your solution/ folder. A verifier reply with compiler errors,
test failures, or analyzer diagnostics is feedback: fix your code and verify again.
