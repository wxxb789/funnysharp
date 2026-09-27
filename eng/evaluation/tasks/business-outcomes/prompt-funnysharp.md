## Business brief: order placement

You are implementing the order-placement workflow of a small shop as a library consumed by a
fixed xUnit test suite. The test suite and the neutral domain contract are already provided in
the build directory; you implement the workflow behind the seam below and the tests must pass.

Seam (you must define this exactly):

public static class OrderWorkflow
{
    public static OrderOutcome PlaceOrder(
        OrderRequest request,
        ProductCatalog catalog,
        PaymentGateway gateway,
        OrderRepository repository,
        string cardToken)
}

Declare your types in the global namespace (no namespace declaration) in the files you write.
The provided contract defines OrderRequest, OrderLine, OrderOutcome, ProductCatalog,
PaymentGateway, and OrderRepository; do not redefine them. The provided fakes are deterministic:

- ProductCatalog.TryGetPrice returns false for a missing sku.
- PaymentGateway.Charge returns false (and charges nothing) when cardToken equals its
  DeclineCard; otherwise it counts the charge and returns true.
- OrderRepository.Save returns false for the FailOnCustomer email; otherwise it assigns the
  next order id (starting at 1001) and records it.

Business rules, in evaluation order:

1. Validation - run every independent check and report every failure, in this order:
   - invalid-email: CustomerEmail does not contain '@', or contains no '.' after the '@'.
   - empty-order: Lines is empty.
   - invalid-quantity:<sku>: a line's Quantity is outside 1..99 (one error per offending line,
     in line order).
   - unknown-promo: PromoCode is neither null, empty, SAVE10, nor FREESHIP.
   If any validation error exists: Placed=false, OrderId=0, Total=0, Errors lists them in the
   order above, and nothing else runs (no pricing, no charge, no save).
2. Pricing - every line's sku must have a catalog price, otherwise unknown-sku:<sku> (one per
   missing line, in line order) with Placed=false and nothing else runs. Total starts as the sum
   of price x quantity over all lines, plus 5.00 shipping. Promo SAVE10 removes 10% of the goods
   subtotal (shipping unaffected). Promo FREESHIP removes the shipping. Round the final total to
   two decimals.
3. Payment - gateway.Charge(cardToken, total) returning false means payment-declined,
   Placed=false, and the order is not saved.
4. Persistence - repository.Save(request.CustomerEmail, total, out var orderId) returning false
   means save-failed and Placed=false. On success Placed=true with the assigned OrderId, the
   final Total, and an empty Errors list.

Requirements:

- Correctness under the provided tests is the acceptance bar; the tests are visible to you.
- Keep the failure, absence, and accumulation semantics explicit and readable: a maintainer
  should see at a glance which step failed and why.
- Do not read or copy the FunnySharp repository source code.

## Style: FunnySharp

Use the FunnySharp package (already referenced) for absence, failure, and validation outcomes
where it fits, and ordinary C# everywhere else:

- Option<T> for absence (a missing catalog price, a missing promo effect),
- Validation<TValue, TError> for the accumulated validation checks,
- Result<TValue, TError> / UnitResult<TError> for fail-fast value-producing and command steps,
- ordinary exceptions stay exceptions.

You may read the FunnySharp package documentation in this repository: README.md and every file
under docs/ (option.md, result.md, unit-result.md, validation.md, grammar.md, analyzers.md,
function-composition.md, and so on). The package ships compiler analyzers; compiler diagnostics
with FS ids report API misuse. Do not read or copy the FunnySharp source code under src/.

## Delivery

Write every source file you need into the solution/ directory next to this prompt. Only files
under solution/ are copied into the build; the tests and contract are fixed. Compile with:

    python3 eng/evaluation/runner.py verify business-outcomes `STYLE` <run-directory>

where <run-directory> contains your solution/ folder. A verifier reply with compiler errors,
test failures, or analyzer diagnostics is feedback: fix your code and verify again.