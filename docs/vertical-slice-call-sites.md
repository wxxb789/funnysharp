# Vertical Slice: Call-Site Comparison

`docs/goals/archive/0022-goal.md` requires a maintainable idiomatic-C# comparison that says which ceremony
FunnySharp removes and which semantics it adds. This guide compares two applications that expose the
same ten scenarios over the same routes:

| Application | Location | FunnySharp reference |
| --- | --- | --- |
| Carrier slice | `tests/FunnySharp.VerticalSlice/FunnySharp.VerticalSlice.Api` | `FunnySharp` and `FunnySharp.AspNetCore` **packages** only |
| Idiomatic comparison | `tests/FunnySharp.VerticalSlice/FunnySharp.VerticalSlice.Baseline` | none |

Both are hosted side by side by `tests/FunnySharp.VerticalSlice/FunnySharp.VerticalSlice.Measurements`,
which sends every scenario to both applications, requires the same status code and the same payload
(modulo per-run identity, timestamps, and trace ids), and then measures allocations, latency, and
throughput. Reproduce the whole evidence set with:

```shell
dotnet fsi build.fsx -- -p vertical-slice --output artifacts/vertical-slice/consumer-run
```

The comparison is deliberately not a straw man. The baseline validates, cancels, bounds its
parallelism, streams, and maps failures explicitly; it stays a design a reviewer would accept for a
small service. Where the baseline is shorter, this guide says what it gave up; where it is not
shorter, it says what it pays for.

## Scenario Map

| Scenario | Route | What it proves |
| --- | --- | --- |
| `place-order-valid` | `POST /orders` | Pricing through bounded supplier fan-out, inventory reservation, persistence, lifecycle event |
| `place-order-invalid` | `POST /orders` | Every invalid field reported in one response, in input order |
| `get-order-found` | `GET /orders/{id}` | Refined path identifier, projection read |
| `get-order-missing` | `GET /orders/{id}` | Absence mapped explicitly to 404 |
| `pay-order-declined` | `POST /orders/{id}/payments` | A defined transition that failed, mapped to 402 |
| `cancel-order` | `POST /orders/{id}/cancellation` | No-value outcome executed through an interpreter |
| `quote-fanout` | `GET /suppliers/{sku}/quotes` | Bounded parallel work with deterministic source-order result |
| `quote-unavailable` | `GET /suppliers/{sku}/quotes` | Every supplier failed: one typed aggregate failure |
| `reconcile-order` | `GET /orders/{id}/reconcile` | Deferred effect with an explicit environment, replay check |
| `export-stream` | `GET /orders/export` | NDJSON stream with a running aggregate and cancellation |

## 1. External Input Validation

Carrier slice (`Http/OrderRequestValidation.cs`):

```csharp
var errors = new List<InputError>();
var customerId = Collect(CustomerId.Create(request.CustomerId), errors);
var lines = CollectLines(request.Lines, options.MaximumOrderLines, errors);
if (errors.Count > 0 || !customerId.TryGetValue(out var customer) || !lines.TryGetValue(out var orderLines))
{
    return Validation<PlaceOrderCommand, InputError>.InvalidMany(errors);
}

return Validation<PlaceOrderCommand, InputError>.Valid(new PlaceOrderCommand(customer, orderLines));
```

Baseline (`Endpoints.cs`):

```csharp
var errors = ValidatePlacement(request, options);
if (errors.Count > 0)
{
    return Results.Problem(ValidationProblem(errors));
}
```

**Removed ceremony:** the carrier version needs the small `Collect` helper to fold each refined
factory into the error list, and it keeps a `Validation` value through the boundary instead of an
`if (errors.Count > 0)` return. Ten of its lines exist so the *type* of the result carries the two
branches.

**Added semantics:** the validator cannot forget a field: every factory returns a carrier, and the
only way to build the command is to have a valid value for each one. `Validation` also makes
"valid value or the complete ordered error list" a single value the endpoint passes to
`ToHttpResult`, so the 400 branch and the success branch live in one expression. The baseline's
list-and-early-return shape is fine, but nothing ties the returned command to the validated fields
once the method returns.

## 2. Refined Domain Values

Carrier slice (`Domain/OrderId.cs`, `Domain/Quantity.cs`):

```csharp
public readonly record struct OrderId
{
    private readonly string? value;
    private OrderId(string value) => this.value = value;
    public string Value => value ?? throw new InvalidOperationException("The order id is uninitialized.");
    public static Result<OrderId, InputError> Create(string? candidate) => /* ... */;
}
```

Baseline (`Endpoints.cs`):

```csharp
if (line.Sku is not { Length: >= 2 and <= 32 }
    || !line.Sku.All(static character => char.IsAsciiLetterUpper(character) || char.IsAsciiDigit(character) || character == '-'))
{
    errors.Add(($"{field}.sku", "A SKU must be 2 to 32 upper-case letters, digits, or dashes."));
}
```

**Removed ceremony:** nowhere in the carrier slice does a handler re-check a SKU, because
`OrderCommand.Sku` cannot be constructed invalid. The baseline re-states the rule at each entry
point (placement, quotes) and passes plain `string` values inward, so every inner call either
re-validates or trusts its caller.

**Added semantics:** the private constructor makes the refinement a property of the type rather than
of the code path. The cost is real and visible: each refined type is ~25 lines of factory boilerplate
that the baseline does not pay. This slice stayed at four refined values (order id, customer id, SKU,
quantity, plus money and tracking code) precisely because the boilerplate does not amortize below
that.

## 3. Domain Outcomes

Carrier slice:

```csharp
var plan = OrderLifecycle.Plan(state, @event);
if (!plan.TryGetValue(out var planned))
{
    _ = plan.TryGetError(out var planningError);
    return Result<Order, OrderError>.Failure(planningError!);
}

var executed = await executor.ExecuteAsync(planned.Commands, cancellationToken);
return executed.TryGetError(out var executionError)
    ? Result<Order, OrderError>.Failure(executionError)
    : Result<Order, OrderError>.Success(planned.Order);
```

Baseline (`Lifecycle.cs`, `Endpoints.cs`):

```csharp
var paid = Lifecycle.Apply(order, new OrderEvent("Pay", /* ... */));
store.Persist(paid, "Pay");
return Results.Ok(PaymentReceiptResponse.From(paid));
```

**Removed ceremony:** the baseline reads better here. Two statements, no carrier plumbing, and the
failure path is invisible because it is a `throw` inside `Lifecycle.Apply`.

**Added semantics:** three things the baseline does not have.

1. *Exhaustiveness where the failure is created.* `OrderError` is a closed hierarchy with a private
   base constructor, and `ProblemMappings.FromOrderError` switches over every case, ending in
   `UnreachableException` for a case that is not mapped; nothing forces a new `OrderError` into that
   switch, but the first request that produces it fails loudly instead of returning a wrong status. The baseline's `throw new TransitionRejectedException`
   sites are open-ended: a new failure kind compiles everywhere until a request reaches it.
2. *The failure channel is a value.* `Result<Order, OrderError>` is returned, logged, or mapped; it
   cannot be dropped unnoticed, and `FS1002` reports an outright discard. A `throw` is invisible to
   the caller's type signature, so the baseline's endpoint bodies look identical whether the
   operation can fail or not.
3. *Rejection versus failure.* Replaying an event that is invalid for the state is `Rejected`;
   a payment the issuer declined is `Failed`; a dispatch miss is `Undefined`. The baseline collapses
   all three into exceptions of the same shape and recovers the distinction from message fields —
   which is why its 402/409 mapping had to re-derive the taxonomy in `Problems.cs`.

## 4. Lifecycle Decisions And Their Interpreter

Carrier slice (`Domain/OrderLifecycle.cs`): a transition returns the next state *and the commands it
emitted*, and nothing executes while the decision is made.

```csharp
outputs.Add(new OrderCommand.ReserveInventory(line.Sku, line.Quantity));
outputs.Add(new OrderCommand.Persist(placed, placement));
outputs.Add(new OrderCommand.NotifyCustomer(placed.CustomerId, placed.Id, "order-placed"));
return Applied(placed, outputs);
```

`Application/OrderCommandExecutor.cs` is the only place that runs them:

```csharp
var outcome = command switch
{
    OrderCommand.ReserveInventory reserve => await inventory.ReserveAsync(reserve.Sku, reserve.Quantity, cancellationToken),
    OrderCommand.Persist persist => await store.PersistAsync(persist.Order, persist.Change, cancellationToken),
    OrderCommand.NotifyCustomer notify => await publisher.NotifyCustomerAsync(notify.CustomerId, notify.OrderId, notify.Kind, cancellationToken),
    /* ... */
};
```

Baseline: `Lifecycle.Apply` returns the next state, and each endpoint performs the effects inline
(`SimulatedDependencies.Reserve`, `store.Persist`, …).

**Removed ceremony:** the carrier version needs a command hierarchy and an interpreter class; the
baseline needs neither.

**Added semantics:** the decision is testable without I/O and the effect order is one reviewable
policy instead of being spread over five endpoints. `tests/FunnySharp.VerticalSlice` asserts the
emitted command sequence directly (`PlacementAppliesAndEmitsInventoryThenPersistThenNotification`),
which the baseline cannot do: its effects are inseparable from its state change. The interpreter is
also where the slice states its transactional boundary in one sentence: the authoritative persist is
emitted before customer-facing effects, so a failed notification surfaces with the state already
advanced (there is no distributed transaction in either application).

## 5. Bounded Parallel Fan-Out

Carrier slice (`Application/SupplierQuoting.cs`):

```csharp
var attempts = await gateway.FindSuppliersAsync(sku, cancellationToken)
    .SelectParallelValueAsync(
        options.QuoteConcurrency,
        (supplier, token) => gateway.QuoteAsync(supplier, sku, quantity, token))
    .ToListAsync(cancellationToken);
```

Baseline (`Endpoints.cs`):

```csharp
await Parallel.ForEachAsync(
    Enumerable.Range(0, options.SupplierCount),
    new ParallelOptions { MaxDegreeOfParallelism = options.QuoteConcurrency, CancellationToken = cancellationToken },
    (rank, token) => { quotes[rank] = /* ... */; return ValueTask.CompletedTask; });
```

**Removed ceremony:** comparable. The baseline's index-into-an-array trick is the standard way to
recover source order, and it is only slightly longer than the lazy asynchronous source the slice
composes from its port.

**Added semantics:** the ordering guarantee and the failure semantics are *documented* rather than
array-arithmetic. `SelectParallelValueAsync` states that results are delivered in source order even
when later selectors complete first, that `maxConcurrency` counts started-but-undelivered selectors,
and that disposal cancels and drains started work. The baseline re-derives all three: it must know
that `Parallel.ForEachAsync` preserves nothing, that the array write is the only reason order holds,
and — as the comparison harness caught — that a diagnostic list filled inside the parallel body
comes out in **completion order**. The baseline needed an explicit `unavailable.Sort(...)` before its
response matched the slice's deterministic list. That is precisely the class of bug the documented
source-order guarantee removes.

## 6. Completion-Order Streaming

Carrier slice: one named method, with the ordering choice in the name (`SupplierQuoting.cs`):

```csharp
gateway.FindSuppliersAsync(sku, cancellationToken)
    .SelectParallelCompletionOrderValueAsync(
        options.QuoteConcurrency,
        (supplier, token) => gateway.QuoteAsync(supplier, sku, quantity, token));
```

Baseline:

```csharp
var pending = new List<Task<QuoteStreamRow>>(options.SupplierCount);
for (var rank = 0; rank < options.SupplierCount; rank++)
{
    pending.Add(QuoteAsRowAsync($"supplier-{rank + 1:00}", sku, options, cancellationToken));
}

while (pending.Count > 0)
{
    var finished = await Task.WhenAny(pending);
    pending.Remove(finished);
    await WriteRowAsync(stream, await finished, cancellationToken);
}
```

**Removed ceremony:** the baseline is ~10 lines that start every supplier eagerly. It has no bound on
how many suppliers run at once, so a SKU with 60 suppliers would issue 60 concurrent calls.

**Added semantics:** the bound and the "as soon as ready" delivery are one operation. Reproducing the
bound with `Task.WhenAny` requires an admission window the baseline does not have, and the ordering
choice is a boolean away from being silently flipped: in the carrier slice the method *name* carries
it, so a reviewer sees `SelectParallelValueAsync` versus `SelectParallelCompletionOrderValueAsync` at
the call site.

## 7. Streaming With A Running Aggregate

Carrier slice (`Application/OrderExport.cs`):

```csharp
store.StreamAsync(cancellationToken)
    .Scan(ExportCursor.Start, static (cursor, record) => cursor.Advance(record))
    .Choose(static cursor => Option.FromNullable(cursor.Row));
```

Baseline:

```csharp
var sequence = 0;
var revenue = 0m;
foreach (var order in store.Snapshot())
{
    cancellationToken.ThrowIfCancellationRequested();
    if (order.Status == OrderStatus.Draft)
    {
        continue;
    }

    sequence++;
    revenue += order.Total;
    await WriteAsync(stream, new ExportRow(/* ... */), cancellationToken);
}
```

**Removed ceremony:** none worth claiming. The baseline's loop is the clearer code, and the slice's
`Scan`/`Choose` pair does not save a line here.

**Added semantics:** the slice's cursor is a value the test can assert on, and the filter is the same
`Choose` used over synchronous sequences; the *pipeline* is lazy, so a caller can stop early without
buffering the result, and the streaming guarantee belongs to the `IOrderStore` port — an adapter that
yields incrementally makes the whole path incremental.

That guarantee does **not** hold for the in-memory adapter this slice ships:
`InMemoryOrderStore.StreamAsync` snapshots and sorts every order under its lock before the first yield,
exactly as the baseline's `Snapshot()` does, so first-byte latency and peak memory grow with the total
order count in both applications. The honest statement is therefore: the carrier version's *pipeline*
streams and the comparison's loop does not, while both adapters currently buffer — a difference that
would become user-visible only once the port is backed by a paged or streaming store.

## 8. HTTP Mapping

Carrier slice:

```csharp
var placed = await service.PlaceAsync(command, cancellationToken);
return placed.ToHttpResult(
    problems.FromOrderError,
    order => Results.Created($"/orders/{order.Id.Value}", OrderReceiptResponse.From(order)));
```

Baseline: the endpoint returns `Results.Ok(...)` and failures travel as exceptions into one handler
(`BaselineExceptionHandler.Translate`), which is ~90 lines of `switch` over exception types.

**Removed ceremony:** the baseline keeps the endpoints short — no explicit failure mapper at each
call site.

**Added semantics:** the carrier slice declares the possible outcomes at the boundary, and each
mapper states its own status, problem type, and extensions. In the baseline the same information is
concentrated in one handler that must be kept in step with every `throw` site, and nothing forces a
new failure kind to be mapped: the `_ =>` arm silently returns a 500. The slice's `FromOrderError`
throws `UnreachableException` for an unmapped case instead, so a forgotten mapping fails loudly
during the first test that exercises it.

The wire shape is identical for all ten scenarios, including `ProblemDetails`, `402 Payment
Required`, `409 Conflict` with `code`/`orderStatus`/`event` extensions, and the validation problem's
per-field message arrays. The comparison harness asserts that equality.

## 9. Effects And Explicit Environments

Carrier slice:

```csharp
public Effect<ReconciliationEnvironment, Result<ReconciliationReport, OrderError>> Reconciliation(OrderId id) =>
    Effect.FromValueTask<ReconciliationEnvironment, Result<ReconciliationReport, OrderError>>(
        (environment, cancellationToken) => ReconcileAsync(environment, id, cancellationToken));
```

```csharp
return service.Reconciliation(orderId).ToHttpResultAsync(
    service.Environment,
    context,
    problems.FromOrderError,
    report => Results.Ok(ReconciliationResponse.From(report)));
```

Baseline: the handler reads the dependencies from its parameter list and awaits them directly.

**Removed ceremony:** the baseline is simpler. The slice pays for a second type parameter and an
environment value so the deferred work is a value the caller can pass around.

**Added semantics:** the dependency set of the operation is visible in its type, the work does not
start until the boundary runs it, and the integration helper passes exactly `context.RequestAborted`
to `RunAsync` — `docs/aspnet-core.md` documents that it never creates, links, or replaces the request
token. `tests/FunnySharp.VerticalSlice` proves the identity claim for the non-effect path
(`OneRequestTokenReachesEveryDependencyAndTheClientCancelsIt`) and the disconnect claim on real
Kestrel (`AClientDisconnectStopsTheStoreEnumeration`).

## What The Measurements Say

Both applications were measured through the same in-process `TestServer`, 20 warmup and 200 measured
iterations per scenario (3/15 for the stream; a fresh in-memory state per run), on the repository's
development machine. Allocation is `GC.GetTotalAllocatedBytes(precise: true)` divided by iterations;
latency is the median end-to-end request time. Reproduce with
`dotnet fsi build.fsx -- -p vertical-slice` (`eng/harness/VerticalSlice.fs`), which writes the
receipt next to its output.

| Scenario | Slice alloc (B/op) | Baseline alloc (B/op) | Slice p50 (us) | Baseline p50 (us) |
| --- | ---: | ---: | ---: | ---: |
| `place-order-valid` | 23266 | 21203 | 418 | 354 |
| `place-order-invalid` | 22787 | 22180 | 382 | 424 |
| `get-order-found` | 16000 | 15867 | 248 | 210 |
| `get-order-missing` | 15854 | 44058 | 212 | 485 |
| `pay-order-declined` | 18502 | 67100 | 449 | 662 |
| `cancel-order` | 40117 | 36987 | 956 | 840 |
| `quote-fanout` | 25980 | 19019 | 422 | 374 |
| `quote-unavailable` | 24217 | 75936 | 334 | 769 |
| `reconcile-order` | 18177 | 16951 | 280 | 275 |
| `export-stream` | 398348 | 458851 | 4467 | 4564 |

Reading (one receipt; re-run before quoting these anywhere else):

- **Placement and supplier fan-out cost more with carriers.** `place-order-valid` allocates ~10% more
  and its median request takes ~18% longer; `quote-fanout` allocates ~37% more and takes ~13% longer.
  That is the measured price of the bounded channel-based fan-out and the carrier plumbing, and it is
  the honest counterweight to every "shorter code" claim below.
- **Reads and cancellation are close.** `get-order-found` allocates the same and answers ~18% slower,
  `cancel-order` allocates ~8% more and answers ~14% slower, `reconcile-order` allocates ~7% more at the
  same latency, and `export-stream` allocates ~13% less at the same latency.
- **Failure paths are where carriers pay for themselves.** When the payload of the request *is* a
  failure — `get-order-missing`, `pay-order-declined`, `quote-unavailable` — the slice allocates 64% to
  72% less and answers 1.5x to 2.3x faster, because it returns a value and builds one `ProblemDetails`
  while the baseline throws, captures a stack, unwinds through middleware, and then builds the same
  problem.
- **Streaming is dominated by the response body.** ~0.40-0.46 MB per operation in both applications;
  the difference between them is smaller than the payload itself.
- **These are application observations, not a release claim.** `eng/performance/baseline.json` still
  records `excluded|aspnet-mapping` with the rationale that no numeric release claim is made without
  a representative application pipeline; this evidence is scoped to application end-to-end behavior
  through `TestServer`, on one developer machine, and does not revise that policy. Timing is
  directional, as `docs/performance.md` requires.
- **The comparison verdict is status *and* payload.** `Equivalence.Same` in the harness rejects a
  scenario where the bodies match but the status codes differ, and `MeasurementsEquivalenceTests` pins
  that rule.

## Where Shorter Code Would Be A Lie

The goal forbids counting shorter code that hides cancellation, allocation, or failure modes as an
improvement. Concretely:

- Both applications pass `HttpContext.RequestAborted` (or the endpoint's `CancellationToken`) into
  every dependency call; neither wraps a public entry point in `Task.Run`, `Result`, or a
  fire-and-forget discard.
- The slice's `ToHttpResultAsync` overloads call `RunAsync` with exactly the request token; the
  cancellation tests compare the tokens dependencies received within one request and assert the
  disconnected stream stops.
- The slice never converts an exception into a domain outcome: the only catch-all is
  `UnexpectedExceptionHandler`, which logs the exception object and returns a generic 500 problem.
  The baseline's handler does the same, and the tests assert both.
- Neither application hides allocations behind a caching layer: the measured structures are the ones
  the request path actually builds.

## Deliberate Boundaries

- The comparison covers ten scenarios, not the whole surface: no authentication, payment provider
  integration, database, or deployment concerns are modelled, and neither application is a template
  for production configuration.
- No competitor library participates. The baseline is ordinary C# with the BCL only, and no
  compatibility with another functional library is claimed or tested.
- The measurement harness is not a BenchmarkDotNet receipt and never enters
  `eng/performance/baseline.json`; the release performance gate is unchanged.
- The numbers are machine-specific. Re-run the harness rather than quoting this table on other
  hardware.
