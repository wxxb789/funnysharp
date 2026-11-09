# Typed Operation Plans Evidence

This record supports the maintainer's judgment for
[Goal 0042](goals/archive/0042-goal.md): whether a typed, inspectable sequence or dependency
structure of named business operations gives FunnySharp consumers a material advantage over
explicit `async`/`await` code and an existing transaction facility for a real multi-operation
workload. It records the executable SQLite order-placement workload, the non-shipping plan
prototype, the direct and EF Core consumers, the shared verifier, both measurement runs, and the
transaction and external-effect boundaries.

**Decision: REJECT a public typed operation plan; retain the documented named-step pattern.** The
plan is competitive at runtime (0.6-4.4% lower allocation on most complete-operation rows, with no
timing direction) and its per-workflow call site is 323 raw lines smaller than the direct consumer,
but the 584-line plan core (356 lines unique after the shared diagnostic and inspection vocabulary)
does not repay one application. The complete plan solution is 1,309 raw lines versus 1,048 for the
direct `Result`/`await` pattern and 1,014 for EF Core. The plan adds no diagnostic, cancellation,
rollback, or transaction guarantee that the pattern and `SqliteTransaction` do not already provide.
The smallest useful shape is the pattern: a validated static `OperationInfo[]`, a `NamedStep.RunAsync`
helper, a typed caller state and `Result`, and a caller-owned `SqliteTransaction`. The real
transaction belongs to one storage adapter; the plan cannot cover the external gateway, and the
ambient `TransactionScope` does not roll this provider's commands back. No public capability,
package, grammar, product-contract, stability-inventory, or API-baseline change occurred.

Source identity is this Goal 0042 working tree on top of commit `4149d0a`; the landing commit
becomes the durable identity. Measurements were taken on this host:

- Ubuntu 24.04.5 LTS, Linux 7.0.0-1017-azure x86_64, 4 logical cores.
- .NET SDK 10.0.401, runtime 10.0.12 x64; Release build; `TieredCompilation=false`.
- Dates: 2026-11-09 UTC. The two final runs started at 14:27:21 and 14:27:43.
- SQLite provider: `Microsoft.Data.Sqlite` 10.0.12; EF Core provider:
  `Microsoft.EntityFrameworkCore.Sqlite` 10.0.12.

Allocation is the blocking comparison; timing is directional on this shared host.

## Chosen Workload And Why It Is Representative

The workload is a single "place an order" operation over a small but real SQLite schema:

- `customers(id, name, credit_limit_cents, active)`.
- `catalog(sku, unit_price_cents, active)`.
- `inventory(sku, available, reserved)` with `CHECK (available >= 0)`.
- `orders(id, customer_id, total_cents, status)`.
- `order_lines(id, order_id, sku, quantity, unit_price_cents)`.
- `payments(id, order_id, amount_cents, reference UNIQUE)`.
- `outbox(id, order_id, topic, payload)`.
- `reviews(id, order_id, reason)`.

One request names a customer and three line items. The operation has nine named steps:

1. `load-customer` reads the customer and rejects a missing or inactive customer.
2. `load-catalog` reads active catalog prices for the requested SKUs.
3. `create-order` computes the total from catalog prices, inserts the order, and inserts one line
   per requested SKU. The database-generated order id is not known before this step.
4. `check-credit` compares the computed total with the customer's credit limit.
5. `reserve-inventory` decrements each line's available stock and increments reserved stock
   conditionally; a shortfall fails at the responsible SKU.
6. `authorize-payment` calls a simulated external gateway with the computed amount and order id.
   The gateway ledger is outside SQLite and is not rolled back.
7. `record-payment` inserts the payment row with the gateway reference.
8. `record-outbox` inserts the integration event payload containing the order id, total, and
   payment reference.
9. `queue-review` conditionally inserts a review row when the total reaches 40,000 cents. Whether
   it runs is not knowable until `create-order` produces the total.

This is a real task because it mixes dependent reads, a generated identifier, a multi-row write
whose count and success depend on prior results and stored stock, a business rule over an earlier
result, an external effect, a transactional integration event, and a conditional step. It also has
natural failure points before and after writes and before and after the external effect. The schema
is ordinary SQL, not a FunnySharp abstraction, and the business outcome is observable as rows plus a
typed outcome. A single insert or arithmetic chain would not exercise the goal's boundaries; a
larger order-management system would add unrelated domain rules.

The work ledger in both runs is deterministic and identical:

| Scenario | Consumer | Outcome | Operation | Completed | Orders | Payments | Outbox | Reviews | Gateway |
| --- | --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| happy path | Typed plan | success | - | 9 | 1 | 1 | 1 | 1 | 1 |
| happy path | Direct await | success | - | 9 | 1 | 1 | 1 | 1 | 1 |
| happy path | EF Core transaction | success | - | 9 | 1 | 1 | 1 | 1 | 1 |
| small order | Typed plan | success | - | 8 | 1 | 1 | 1 | 0 | 1 |
| small order | Direct await | success | - | 8 | 1 | 1 | 1 | 0 | 1 |
| small order | EF Core transaction | success | - | 8 | 1 | 1 | 1 | 0 | 1 |
| credit limit | Typed plan | OperationFailed | check-credit | 3 | 0 | 0 | 0 | 0 | 0 |
| credit limit | Direct await | OperationFailed | check-credit | 3 | 0 | 0 | 0 | 0 | 0 |
| credit limit | EF Core transaction | OperationFailed | check-credit | 3 | 0 | 0 | 0 | 0 | 0 |
| stock shortfall | Typed plan | OperationFailed | reserve-inventory | 4 | 0 | 0 | 0 | 0 | 0 |
| stock shortfall | Direct await | OperationFailed | reserve-inventory | 4 | 0 | 0 | 0 | 0 | 0 |
| stock shortfall | EF Core transaction | OperationFailed | reserve-inventory | 4 | 0 | 0 | 0 | 0 | 0 |
| payment decline | Typed plan | OperationFailed | authorize-payment | 5 | 0 | 0 | 0 | 0 | 1 |
| payment decline | Direct await | OperationFailed | authorize-payment | 5 | 0 | 0 | 0 | 0 | 1 |
| payment decline | EF Core transaction | OperationFailed | authorize-payment | 5 | 0 | 0 | 0 | 0 | 1 |
| outbox fault | Typed plan | OperationFaulted | record-outbox | 7 | 0 | 0 | 0 | 0 | 1 |
| outbox fault | Direct await | OperationFaulted | record-outbox | 7 | 0 | 0 | 0 | 0 | 1 |
| outbox fault | EF Core transaction | OperationFaulted | record-outbox | 7 | 0 | 0 | 0 | 0 | 1 |
| cancel before start | Typed plan | Canceled | load-customer | 0 | 0 | 0 | 0 | 0 | 0 |
| cancel before start | Direct await | Canceled | load-customer | 0 | 0 | 0 | 0 | 0 | 0 |
| cancel before start | EF Core transaction | Canceled | load-customer | 0 | 0 | 0 | 0 | 0 | 0 |
| cancel between steps | Typed plan | Canceled | queue-review | 8 | 0 | 0 | 0 | 0 | 1 |
| cancel between steps | Direct await | Canceled | queue-review | 8 | 0 | 0 | 0 | 0 | 1 |
| cancel between steps | EF Core transaction | Canceled | queue-review | 8 | 0 | 0 | 0 | 0 | 1 |

The failure rows show that earlier writes were rolled back: orders, payments, outbox, and reviews
are zero after every failure. The inventory rows are separately checked by the shared verifier: the
seed is `SKU-A` 10/0, `SKU-B` 5/0, `SKU-C` 4/0, `SKU-OFF` 10/0; the happy path ends at A 8/2, B 4/1,
C 3/1; the stock-shortfall failure leaves all inventory at the seed values. The payment-decline and
outbox-fault rows leave one external gateway attempt even though the database is empty; the
cancel-between-steps row leaves one successful authorization. That is the external-effect boundary,
not a rollback.

## Storage Adapter And Real Guarantees

`Microsoft.Data.Sqlite` 10.0.12 supplies the actual transaction for the plan and direct consumers.
The guarantees observed on this workload are:

- **Atomicity:** `COMMIT` makes all writes visible; `ROLLBACK` makes none visible. A failed commit is
  reported as a distinct outcome; the plan attempts a rollback and does not claim success.
- **Isolation:** a second connection reading during an open write transaction sees the pre-commit
  state; a concurrent writer receives `SQLITE_BUSY` (error code 5) under the configured busy
  timeout. The boundary test uses a one-second timeout so the failure is observable quickly.
- **Durability:** a file database with the default rollback journal is durable at commit on this
  host; the in-memory measurement database is not durable. The evidence does not generalize the
  in-memory result to a file database's crash behavior.
- **No distributed transaction:** one connection and one database file. The external gateway call is
  not enlisted and cannot be rolled back.
- **No ambient transaction:** a `TransactionScope` without `Complete` does not roll back commands
  executed by this provider; explicit `EnlistTransaction` throws `NotSupportedException`. A caller
  must use the explicit `SqliteTransaction`; assuming ambient enlistment silently loses atomicity.
- **No exactly-once execution:** a commit can fail, or its acknowledgement can be lost after the
  database committed. The plan reports the outcome it observed; retries, idempotency keys, and
  reconciliation remain application responsibilities.

`IPlanTransaction` is deliberately minimal: `CommitAsync()` and `RollbackAsync()`, with the caller
opening, owning, and disposing the real transaction. Neither method takes a token; commit and
rollback are not cancellable. The plan does not open connections, create databases, retry,
compensate, or interpret storage errors. The application-side
`SqlitePlanTransaction` adapter is charged to the plan consumer; the direct consumer uses
`SqliteTransaction` directly; the EF consumer uses `IDbContextTransaction`.

## Reference Behavior

The reference is explicit, not merely "whatever the direct consumer returns":

- Happy path: one order, three order lines, one payment, one outbox row, one review row for a total
  at or above the threshold, decremented `available` and incremented `reserved` inventory, and one
  gateway authorization.
- Small order: the same rows without a review row, and `queue-review` reported as skipped.
- Customer missing or inactive: failure at `load-customer`; no orders, lines, payments, outbox,
  reviews, inventory changes, or gateway calls.
- Catalog missing or inactive: failure at `load-catalog`; only `load-customer` completed; no writes.
- Credit limit exceeded: failure at `check-credit`; the already-inserted order and lines are rolled
  back; the failure state retains order id 1 and total 40,000.
- Insufficient stock: failure at `reserve-inventory` at the responsible SKU; the earlier inventory
  update and the order/lines are rolled back; the failure state retains order id 1 and total 100,000.
- Payment declined: failure at `authorize-payment`; database writes are rolled back; the external
  gateway records the declined attempt; no authorization reference is produced.
- Outbox fault after authorization: the step throws; the database is rolled back, the gateway
  authorization remains, and the outcome preserves the completed operation names, order id, total,
  and payment reference.
- Cancellation before the first step, between steps, inside a step, and at the commit boundary: no
  successful commit; rollback; cancellation is distinguishable from a domain failure and from an
  unexpected exception. A commit-boundary `OperationCanceledException` retains all executed names,
  no responsible operation, and the exception.
- Rollback failure for a domain failure, callback exception, cancellation, or commit failure:
  `RollbackFailed` with `OriginalKind` set to the pre-rollback kind and the original error or
  exception preserved beside the rollback exception.
- Duplicate names, empty/whitespace names, forward/unknown/self dependencies, duplicate
  dependencies, empty plans, and a `Conditional` flag that disagrees with the condition delegate
  are rejected at construction.
- Commit failure via a deferred foreign-key violation: `CommitFailed` is distinct; this adapter
  rolls back; the state after all steps is retained for diagnosis. A rollback failure is reported
  separately and never hides the original failure.

The shared verifier checks these rows, counts, identities, and outcomes for the plan, direct, and EF
consumers. It also exercises commit failure and commit-phase cancellation for all three, EF
transaction-open cancellation, and rollback failure for domain, callback, cancellation,
commit-failure, and commit-cancellation originals, asserting `Kind`, `Operation`, `OriginalKind`,
and the preserved exception or typed error. The direct consumer is one of the compared
implementations, but the expected values are written independently in the verifier, so a bug in
the direct consumer cannot define the reference. `OperationComparison.VerifyAsync`
reports 1,612 checks; both measurement runs reproduced that count.

## Prototype Model

The non-packable research project
[FunnySharp.OperationPlans](../tests/FunnySharp.OperationPlans/) is not referenced by `src/` and is
not part of the shipping surface. Ecto.Multi is the reference for named outcomes and inspectability;
this prototype borrows the named sequence and named failure, not its repository, changeset, or
database semantics. The transaction remains the caller's storage adapter. The existing `Effect`
boundary is unchanged; the plan uses `Result` and ordinary `async` because the caller owns the
transaction and cancellation, and the plan is a named sequence rather than a deferred-work wrapper. Its core is `OperationPlan<TEnvironment, TState, TError>`, an
immutable sequence of `OperationStep<TEnvironment, TState, TError>` values. Each step couples an
`OperationInfo` description to a typed delegate and an optional condition:

```csharp
public sealed class OperationStep<TEnvironment, TState, TError>
{
    internal OperationInfo Info { get; }
    internal Func<TEnvironment, TState, CancellationToken, Task<Result<TState, TError>>> Run { get; }
    internal Func<TState, bool>? Condition { get; }
}

public sealed class OperationPlan<TEnvironment, TState, TError>
{
    public static OperationPlan<TEnvironment, TState, TError> Create(
        IEnumerable<OperationStep<TEnvironment, TState, TError>> steps);
    public IReadOnlyList<OperationInfo> Describe { get; }
    public Task<Result<OperationSuccess<TState>, RunFailure<TState, TError>>> RunAsync(
        TEnvironment environment,
        TState initialState,
        IPlanTransaction transaction,
        CancellationToken cancellationToken = default);
}
```

`OperationInfo` carries the operation name, `Kind` (`Read`, `Write`, `External`), declared
dependencies, `RequiresPriorResult`, `DynamicWork`, `Conditional`, and a short note. `Create`
validates and copies the sequence; `Describe` returns the immutable descriptions without invoking a
delegate. `RunAsync` iterates in declared order, evaluates a condition, checks cancellation at step
boundaries and before commit, catches cancellation and non-cancellation exceptions, rolls back on
failure, commits on success, and returns a typed `OperationSuccess<TState>` or
`RunFailure<TState, TError>`.

The plan core is 584 raw lines, including 212 lines of inspection/diagnostic vocabulary
(`OperationInfo` 81, `OperationSuccess` 39, `RunFailure` 92) and 16 lines of `OperationKind` that
the alternatives also use. The plan-specific executor, step type, and transaction interface are 356
lines. The plan is a small framework, not a scheduler: it has no retry, backoff, topology sort,
parallel step, DI, durable store, or ambient transaction.

### Inspection Metadata

The workload's plan describes itself before execution:

| Operation | Kind | DependsOn | RequiresPriorResult | DynamicWork | Conditional |
| --- | --- | --- | --- | --- | --- |
| load-customer | Read | - | False | False | False |
| load-catalog | Read | - | False | False | False |
| create-order | Write | load-customer, load-catalog | True | True | False |
| check-credit | Read | load-customer, create-order | True | False | False |
| reserve-inventory | Write | create-order, load-catalog | True | True | False |
| authorize-payment | External | create-order | True | False | False |
| record-payment | Write | create-order, authorize-payment | True | False | False |
| record-outbox | Write | create-order, authorize-payment | True | False | False |
| queue-review | Write | create-order | True | False | True |

What is knowable up front: the ordered names, each operation's kind, the declared dependency names,
whether the operation requires a prior result, whether its work is dynamically shaped, and whether
it may be skipped. What is not knowable up front: the generated order id, the computed total, the
priced lines, the payment reference, the stored inventory availability, the line count of database
sub-writes (the plan describes the step, not each insert), and whether `queue-review` will run. The
plan marks those facts; it does not pretend to resolve them. The direct descriptor array carries
exactly the same metadata, and the verifier checks the two descriptions field by field.

### Execution, Errors, And Typed Context

`TState` is the caller-owned typed context. The workload's `OrderState` record carries the request,
the loaded customer, the priced catalog, the order id, the total, the priced lines, and the payment
reference. Every step returns `Result<OrderState, OrderError>`; the executor and the direct/EF
helpers return `OperationSuccess<OrderState>` or `RunFailure<OrderState, OrderError>`. There is no
`object` state, stringly dictionary, or second error hierarchy.

`RunFailure` carries:

- `Kind`: `OperationFailed`, `OperationFaulted`, `Canceled`, `CommitFailed`, or `RollbackFailed`.
- `OriginalKind`: the pre-rollback kind when `Kind` is `RollbackFailed`; otherwise `null`.
- `Operation`: the responsible operation, the unstarted operation at a boundary cancellation, the
  in-flight operation for a callback cancellation, or `null` for the commit boundary.
- `Error`: the caller's typed domain error for `OperationFailed`.
- `Exception`: the callback, cancellation, or commit exception when one applies.
- `RollbackException`: the cleanup exception for `RollbackFailed`.
- `Completed`: the operation names that returned success before the failure.
- `State`: the last good typed state, including any order id, total, and payment reference.

The domain error and exception are never flattened into the other. A `Result` failure from a step
becomes `OperationFailed`; an `OperationCanceledException` becomes `Canceled`; any other exception
becomes `OperationFaulted`; a commit exception becomes `CommitFailed`. A failed step's state is not
used; the failure keeps the state from before the step.

### Transaction Adapter

The plan's only transaction contract is `IPlanTransaction`. The caller opens the real transaction,
constructs the application environment with the same transaction, and passes both to `RunAsync`.
The plan commits or rolls back through the interface; the caller disposes the real transaction. The
workload's `SqlitePlanTransaction` is a 22-line adapter over `SqliteTransaction` (including the
verifier's commit/rollback fault seam). The direct consumer uses `SqliteTransaction` directly; EF
uses `IDbContextTransaction`. The plan core never
references `Microsoft.Data.Sqlite`, which keeps the research core storage-agnostic but also means
the plan cannot enforce that a step's SQL uses the same transaction as the adapter. That is a
caller contract, not a plan guarantee.

## Semantics

### Names, Dependencies, And Construction

- Names are unique ordinal, non-empty, non-whitespace strings; a duplicate, empty, or whitespace
  name is an `ArgumentException` naming the offending value. Names are case-sensitive.
- A plan must contain at least one operation. An empty plan is rejected rather than committing an
  empty transaction.
- Declared dependencies must name earlier operations. A forward reference, unknown name,
  self-reference, or duplicate dependency is rejected. Dependencies are inspection and validation
  metadata, not a scheduler: execution is always declared order, never a topological or parallel
  order. A step may declare several prior dependencies.
- `Conditional` must agree with the presence of a condition delegate. A step marked conditional
  without a condition, or carrying a condition while marked non-conditional, is rejected at
  construction.
- `Create` copies the step array and the descriptions; mutating the caller's array after `Create`
  does not change the plan. `Describe` is read-only and side-effect free. The shared verifier and
  the construction tests check all of these cases.

The plan cannot prove that a delegate reads only its declared dependency fields. The `TState`
record is the actual typed dependency carrier; the declared names are an explicit contract, and a
step can read any field in the state. The direct descriptor array has the same limitation. This is
a material limitation for the "dependencies remain typed" question: the results are typed, but the
edges are named metadata, not typed per-edge values. A graph of typed inputs would require a
different shape (and overloads or a tuple/record of node values); this sequence does not provide it.

### Dynamic Dependencies

- `RequiresPriorResult` marks a step whose arguments or decision come from a prior result. The
  actual values are not available at construction.
- `DynamicWork` marks a step whose sub-work is not enumerated by the description. `create-order`
  emits one line insert per requested SKU; `reserve-inventory` emits one conditional update per
  priced line, and the database decides each success. The description names the step, not the
  individual statements.
- `Conditional` marks a step that may be skipped after a prior result. `queue-review` runs only when
  the total is at least 40,000 cents. The run outcome records it as skipped, not completed, and the
  direct/EF consumers take the same branch.
- The plan does not infer dependencies, schedule around them, or execute sub-work in parallel. The
  conditional step is a static entry with a runtime predicate; a truly dynamic sequence of N
  operations must live inside one step and is not inspectable.

### Cancellation

- The token is forwarded to every step. The executor checks it before each step and before the
  commit boundary; a step that honors the token may also throw `OperationCanceledException`.
- A boundary cancellation returns `Canceled` with the next step's name as the unstarted operation,
  the completed names, and the last good state.
- An `OperationCanceledException` from a step or condition returns `Canceled` with that step named
  as the in-flight operation.
- Rollback is not cancellable, so cleanup is attempted even after the caller cancels. Once
  `CommitAsync` starts it is not canceled; the plan does not abandon a commit halfway. A
  cancellation racing a successful commit may therefore arrive after the commit; callers that
  need stronger guarantees own idempotency and reconciliation.
- A pre-canceled token opens no step. The plan and direct consumers open the real transaction and
  roll it back; the EF consumer checks before opening its context transaction and returns the same
  `Canceled` outcome. The EF consumer also maps an `OperationCanceledException` from
  `BeginTransactionAsync` to that outcome with the exception preserved, closing the
  check-then-open race. The observable database state is the same; the EF pre-cancel row allocates
  less because it has no transaction to roll back.

The shared verifier covers pre-start (including the EF transaction-open boundary), between-step,
inside-step, and commit-boundary cancellation for all three consumers, plus rollback failure for
domain, callback, cancellation, and commit failures. The work ledger shows the between-step
cancellation leaves one gateway authorization (the `record-outbox` step completed before the next
boundary) and no database rows.

### Callback Exceptions And Partial Execution

- A non-cancellation exception from a condition or step, including a null task returned by a step,
  is caught and reported as `OperationFaulted` with the step name, the exception, the completed
  names, and the last good state. The plan does not rethrow; the caller can inspect or rethrow.
- A domain `Result` failure is `OperationFailed` with the typed error; it is never flattened to an
  exception or a string.
- On any failure the executor attempts `RollbackAsync`. Database writes from completed steps and
  the failing step are not committed; `Completed` is diagnostic context, not a claim that those
  writes survive. The state preserves earlier useful values.
- An external effect that already happened is not undone. The outbox-fault row has no database rows
  but one gateway authorization; the plan marks `authorize-payment` as `External` and does not
  imply compensation.
- A rollback failure returns `RollbackFailed` with `OriginalKind` set to the original failure kind
  and the operation, error or exception, completed, and state preserved alongside the rollback
  exception. The database state is unknown; the caller must not assume either the writes or the
  cleanup.

### Commit, Rollback, And Multiple Runs

- Commit happens only after every non-skipped step succeeds and no cancellation is observed at the
  pre-commit boundary. A non-cancellation commit exception is `CommitFailed` with all executed
  names and the final state; the executor attempts rollback and never reports success. An
  `OperationCanceledException` from `CommitAsync` is `Canceled` with no responsible operation and
  the commit exception preserved; a custom adapter can throw it even though the interface takes no
  token. The same rule applies to the direct and EF consumers.
- Commit failure via a deferred foreign-key violation is tested against real SQLite: the commit
  throws `SqliteException` with error code 19, the plan attempts rollback, the rows are gone, and
  the outcome is `CommitFailed` with `Operation = null`, two completed operations, and the state
  after the last step. The boundary test also observes the same `SQLITE_CONSTRAINT` behavior
  directly.
- A plan is immutable and holds no run state; the same static plan runs against fresh databases in
  the verifier and in both measurement runs. Two runs produce identical work ledgers.

## Compared Implementations And Fairness

| Implementation | Shape | Charged to it |
| --- | --- | --- |
| Typed plan | `OperationPlan<OrderEnvironment, OrderState, OrderError>` with nine `OperationStep`s, inline `OperationInfo` metadata, dependency/condition validation, the executor, and a `SqlitePlanTransaction` adapter. | The plan core (`OperationPlan`, `OperationStep`, `IPlanTransaction`, validation and executor), the SQLite adapter, and the nine step definitions with inline metadata. The shared `OperationInfo` / `OperationSuccess` / `RunFailure` vocabulary is reported separately. |
| Direct `Result`/`await` | A hand-written `PlaceOrderAsync`/`RunAsync` pair in `DirectOrderConsumer` with `SqliteTransaction`, a `NamedStep.RunAsync` helper that maps domain failure / cancellation / exception and tracks completed names, and a static `OperationInfo[]` descriptor array with a validator. | The direct method, the named-step helper, the static descriptor array and validator, and the transaction open / commit / rollback code. No generic plan core and no plan adapter. |
| EF Core | A `ShopDbContext` with entities mapped to the same tables, a `SaveChangesAsync` per write step inside an explicit `IDbContextTransaction`, the same gateway, and the same static descriptor array/helper. | The context, entity model, mapping configuration, consumer, descriptor array/helper, and transaction handling. EF Core's mapping, change tracking, SQL generation, and provider packages are part of this alternative; they are not a like-for-like plan overhead. |

Shared and not charged to a specific consumer: the schema, seed fixtures, SQL statement helpers,
typed `OrderState`/`OrderError`, `RunFailure` diagnostic value, `OperationInfo` inspection record,
the shared `OrderEnvironment` record, the simulated gateway, the verifier, and the measurement
harness. The direct and EF consumers share the alternative `NamedStep` helper and descriptor
validator as source, but their rows include those lines because the alternative needs them; the
plan's core counts its own executor and validation, and the plan-only `SqlitePlanTransaction`
adapter is charged to the plan row. The plan and direct rows both include the same raw SQL helpers
(`ShopSql` and `OrderOperations`) because both execute the same SQL workload. The EF consumer uses
EF-generated SQL, not those helpers; its allocation includes mapping and change tracking. The
line-count table below reports the raw numbers and states the double-counting explicitly.

The direct consumer is not a straw man: it uses a reusable `NamedStep.RunAsync` helper, a validated
static descriptor array, explicit commit/rollback with the same cancellation and rollback-failure
treatment, and the same typed `OrderState`/`Result`/`RunFailure` values. The plan's per-workflow
call site is shorter because the executor owns the loop and the failure plumbing; that is the
plan's only structural saving over this baseline.

## Measurements

Command:

```bash
dotnet run --project tests/FunnySharp.OperationPlansMeasurements/FunnySharp.OperationPlansMeasurements.csproj -c Release
```

The harness first runs the complete comparison (1,612 checks), then prints the environment,
workload descriptors, source volume, inspection cost, work ledger, and complete-operation
allocation. The database is created and seeded before each measured operation and disposed after;
its allocation and time are outside the snapshot. The measured operation is the consumer's complete
business operation from transaction open to commit or rollback, including the gateway call and the
failure/cancellation machinery. The harness was run twice in isolation; no test suite or other
heavy process ran concurrently.

### Consumer Source Volume

Raw lines, braces and comments included; this is not a LOC target.

| Artifact | Lines | Notes |
| --- | ---: | --- |
| Prototype core | 584 | `OperationPlan` 301, `OperationInfo` 81, `OperationStep` 41, `RunFailure` 92, `OperationSuccess` 39, `IPlanTransaction` 14, `OperationKind` 16. |
| Typed plan consumer | 725 | `PlanOrderConsumer` 137, `SqlitePlanTransaction` 22, `OrderOperations` 285, `ShopSql` 281. |
| Direct await consumer | 1,048 | `DirectOrderConsumer` 234, `NamedStep` 140, `ComparisonDescriptors` 108, `OrderOperations` 285, `ShopSql` 281. |
| EF Core consumer | 1,014 | `EfShopDbContext` 197, `EfOrderConsumer` 569, `NamedStep` 140, `ComparisonDescriptors` 108. |
| Shared workload and verifier | 1,471 | `OperationComparison` 988, `OrderPricing` 98, `ShopSchema` 88, `ShopFixtures` 66, `SimulatedPaymentGateway` 107, `OrderModels` 56, `SqliteSandbox` 39, `IOrderConsumer` 20, `OrderEnvironment` 9. |
| Tests | 742 | Construction, execution, SQLite boundary, ambient-scope, and comparison tests. |
| Measurement harness | 470 | Environment, line counts, inspection, work ledger, and per-operation loop. |

The plan and direct rows each include the shared raw SQL helpers they use; the direct and EF rows
each include the shared named-step helper and descriptor array. The shared `OrderEnvironment` record
is reported once in the shared row and charged to neither consumer; the plan-only
`SqlitePlanTransaction` adapter is charged to the plan row. The plan core's 584 lines include
212 lines of `OperationInfo`/`OperationSuccess`/`RunFailure` vocabulary that the direct and EF
consumers also use, plus 16 lines of `OperationKind`; the plan-specific executor/step/transaction
core is 356 lines. Treating the shared vocabulary as common to all complete solutions, the plan's
unique solution is 1,081 raw lines (356 core + 725 consumer) versus 1,048 for direct and 1,014 for
EF Core. The raw whole-solution table is 1,309 versus 1,048 versus 1,014; it charges the shared
vocabulary only to the plan.

The direct consumer is 33 raw lines smaller than the plan's unique solution for this one workload.
The plan's per-workflow call site is 323 lines smaller; its 356-line unique core needs roughly 1.1
comparable workflows to repay that call-site saving. The direct's `NamedStep`/descriptor helpers are
already reusable, so a second direct workflow does not pay the same helper cost again. The code
volume case is therefore not a material one-application advantage; it becomes a possible local
adapter argument only when several workflows share the same transaction/state/metadata shape.

### Inspection And Construction Cost

| Operation | Allocated B/op | Time ns/op |
| --- | ---: | ---: |
| Plan build + Describe (9 steps) | 4,200.0 | 3,462.4 / 3,412.7 |
| Cached plan Describe | 0.0 | 5.9 / 6.4 |
| Static direct descriptor access | 0.0 | 3.7 / 3.9 |

Construction is 4,200 bytes and about 3.5 microseconds per build; a static plan pays this once, and
`Describe` then costs zero bytes. The static direct descriptor array costs zero per access. The plan
therefore does not add per-operation traceability cost when cached, but the traceability is not
free: it is a 584-line core plus a one-time construction, and the plan's `Create` validation runs
once rather than per operation. The direct pattern's one-time descriptor array plus validator is
108 lines; its `NamedStep` helper is 140 lines.

### Allocation And Directional Time, Run 1

| Scenario | Consumer | Allocated B/op | Time ns/op |
| --- | --- | ---: | ---: |
| happy path | Typed plan | 22,968.0 | 242,136.9 |
| happy path | Direct await | 23,832.0 | 244,620.2 |
| happy path | EF Core transaction | 253,069.0 | 2,497,196.4 |
| small order | Typed plan | 14,696.0 | 191,168.7 |
| small order | Direct await | 15,376.0 | 157,670.8 |
| small order | EF Core transaction | 160,697.9 | 1,567,131.6 |
| credit limit | Typed plan | 15,728.0 | 177,083.8 |
| credit limit | Direct await | 15,824.0 | 175,182.4 |
| credit limit | EF Core transaction | 150,224.1 | 1,256,625.1 |
| stock shortfall | Typed plan | 16,424.0 | 188,190.9 |
| stock shortfall | Direct await | 16,608.0 | 197,715.1 |
| stock shortfall | EF Core transaction | 146,909.0 | 1,298,057.7 |
| payment decline | Typed plan | 19,248.0 | 211,182.5 |
| payment decline | Direct await | 19,608.0 | 211,402.8 |
| payment decline | EF Core transaction | 202,921.9 | 1,967,850.9 |
| outbox fault | Typed plan | 21,856.0 | 253,568.4 |
| outbox fault | Direct await | 22,392.0 | 254,146.8 |
| outbox fault | EF Core transaction | 221,285.0 | 2,156,799.2 |
| cancel before start | Typed plan | 4,216.0 | 14,817.6 |
| cancel before start | Direct await | 3,992.0 | 14,182.4 |
| cancel before start | EF Core transaction | 512.0 | 3,108.2 |
| cancel between steps | Typed plan | 22,248.0 | 268,542.8 |
| cancel between steps | Direct await | 22,784.0 | 257,455.1 |
| cancel between steps | EF Core transaction | 236,917.0 | 2,301,610.7 |

### Allocation And Directional Time, Run 2

| Scenario | Consumer | Allocated B/op | Time ns/op |
| --- | --- | ---: | ---: |
| happy path | Typed plan | 22,968.0 | 243,935.6 |
| happy path | Direct await | 23,832.0 | 244,212.7 |
| happy path | EF Core transaction | 253,069.0 | 2,520,337.6 |
| small order | Typed plan | 14,696.0 | 190,315.4 |
| small order | Direct await | 15,376.0 | 188,121.5 |
| small order | EF Core transaction | 160,697.9 | 1,510,725.0 |
| credit limit | Typed plan | 15,728.0 | 175,601.4 |
| credit limit | Direct await | 15,824.0 | 173,714.8 |
| credit limit | EF Core transaction | 150,224.1 | 1,257,809.7 |
| stock shortfall | Typed plan | 16,424.0 | 188,193.9 |
| stock shortfall | Direct await | 16,608.0 | 198,648.9 |
| stock shortfall | EF Core transaction | 146,909.0 | 1,303,311.4 |
| payment decline | Typed plan | 19,248.0 | 212,658.3 |
| payment decline | Direct await | 19,608.0 | 214,147.4 |
| payment decline | EF Core transaction | 202,921.9 | 1,905,436.3 |
| outbox fault | Typed plan | 21,856.0 | 252,812.4 |
| outbox fault | Direct await | 22,392.0 | 251,116.6 |
| outbox fault | EF Core transaction | 221,285.0 | 2,157,556.9 |
| cancel before start | Typed plan | 4,216.0 | 15,762.7 |
| cancel before start | Direct await | 3,992.0 | 13,919.2 |
| cancel before start | EF Core transaction | 512.0 | 2,587.4 |
| cancel between steps | Typed plan | 22,248.0 | 271,692.2 |
| cancel between steps | Direct await | 22,784.0 | 259,846.0 |
| cancel between steps | EF Core transaction | 236,917.0 | 2,298,586.8 |

The plan, direct, and EF allocation rows reproduce exactly across the two runs; timing varies by
roughly 1.0-1.2x on the shared host and has no consistent direction; allocation is the blocking
comparison.

### Headline

- **The plan is not materially cheaper at runtime.** The happy path is 22,968 B for the plan versus
  23,832 B for direct (3.6% lower) and 253,069 B for EF Core (11.0x the plan). The small order is
  14,696 B versus 15,376 B (4.4% lower) and 160,698 B for EF Core. The stock-shortfall failure is
  16,424 B versus 16,608 B (1.1% lower); the outbox fault is 21,856 B versus 22,392 B (2.4% lower).
  The pre-cancel row is 4,216 B for the plan versus 3,992 B for direct (5.6% higher) because the
  plan's executor and adapter are still present when no step runs. The differences are small and
  do not establish a runtime advantage.
- **Timing has no direction.** Run 1's happy path is 242 &micro;s for the plan versus 245 &micro;s
  for direct and 2.50 ms for EF Core; run 2 is 244 &micro;s versus 244 &micro;s and 2.52 ms. The
  plan and direct swap order across scenarios and runs. Timing is directional only.
- **The EF Core transaction abstraction is a different layer.** It is 9-11x the plan's allocation
  and 7-10x its time on the database-operation rows; the EF pre-cancel row is lower because it
  opens no transaction. Its code is 1,014 raw lines, smaller than the plan's 1,081
  unique solution and the direct's 1,048, because EF owns mapping, change tracking, and SQL
  generation. It does not expose named operation inspection; the consumer still provides the
  descriptor array and named-step helper. The EF comparison shows what an existing persistence
  transaction abstraction charges; it is not a like-for-like plan substitute.
- **Diagnostics are identical.** Every consumer reports the same outcome kind, responsible
  operation, completed names, typed error, retained state, database row counts, and gateway
  attempts in the work ledger. The shared verifier's 1,612 checks include the failure kinds,
  operations, completed sequences, state fields, external ledger, and rollback-failure
  preservation. The plan's `RunFailure` is used by all three consumers; there is no plan-specific
  diagnostic advantage.
- **The real transaction is the adapter's.** The SQLite commit/rollback, isolation, and commit
  failure are the `SqliteTransaction` guarantees. The plan's `IPlanTransaction` is a 14-line seam
  plus a 22-line adapter; it does not add durability, isolation, distributed coordination, or
  compensation. The ambient `TransactionScope` does not enlist this provider: a scope disposed
  without `Complete` leaves the inserted `SKU-SCOPE` catalog row, and explicit enlistment throws
  `NotSupportedException`. The plan's `External` gateway marker is honest, but the plan cannot
  roll the gateway authorization back.

### Storage Boundary Probes

The test suite records the following real-adapter observations (all 28 tests passed in the full
suite; these are not timing measurements):

- A writer's open transaction leaves a second connection reading the pre-commit state (0 orders);
  a concurrent writer receives `SQLITE_BUSY` (error code 5) under a one-second timeout; commit makes
  the row visible (1 order); a later insert rolled back leaves the count at 1.
- A deferred foreign-key violation fails at `COMMIT` with `SQLITE_CONSTRAINT` (19); the transaction
  can be rolled back; `audit_links` remains empty.
- A `TransactionScope` without `Complete` leaves the inserted catalog row (`SKU-SCOPE` count 1);
  `SqliteConnection.EnlistTransaction` throws `NotSupportedException`. The ambient facility is not
  a transaction for this provider.
- The outbox fault leaves no database rows and one gateway authorization; the payment decline
  leaves no database rows and one declined gateway attempt; the between-step cancellation leaves
  no database rows and one authorization. These are external effects, not partial database
  commits.

## Diagnostics And Consumer Expression

The plan consumer's complete call site is the static `OperationPlan` definition with nine
`OperationStep`s and the inline `OperationInfo` metadata, plus `PlaceOrderAsync` opening the
`SqliteTransaction` and calling `RunAsync`. The direct consumer writes the same nine steps in
sequence, each through `NamedStep.RunAsync`, plus the static descriptor array and the commit/rollback
handling. The EF consumer writes the same nine business steps against `ShopDbContext`, each write
through `SaveChangesAsync`, plus the same descriptor array/helper and explicit transaction.

The plan's expression advantage is the per-workflow call site: the loop, cancellation, exception,
rollback, skip, and commit logic live in the core, so the consumer's nine steps are declarations
rather than repeated failure checks. The direct helper already centralizes a single step's failure
mapping; the consumer still checks each `Result` and owns the transaction. The plan's total code is
larger because the executor/validation vocabulary is larger than the direct helper. The plan's
inline metadata cannot drift from the step list; the direct descriptor array is a second table and
is kept honest by the shared verifier and `ComparisonDescriptors.Validate`. The plan's
`Conditional` flag is tied to the condition delegate; the direct/EF arrays are static and their
branch is in the consumer; the verifier checks the branch outcome. The plan's validation advantage
is real but narrow.

The plan's typed context is the same `OrderState` record used by the direct and EF consumers; the
failure state retains the order id, total, and payment reference after the outbox fault. The direct
consumer's `NamedStep.RunAsync` preserves the same state. The EF consumer's `DbUpdateException` for the
payment unique constraint is mapped to the same `PaymentRecordConflict`; the main scenarios do not
hit it, but the mapping is in the consumer. The `OperationFaulted` exception identity is the same
`InvalidOperationException` from the injected outbox fault for all three consumers; the shared
verifier checks it.

## Workload Guidance

**Where plain `await` plus the documented pattern is preferable** (the recommended default):

- One application with one to a few transaction workflows. A static `OperationInfo[]` for
  inspection, a validated descriptor array, a `NamedStep.RunAsync` helper for domain failure /
  cancellation / exception / completed names, a typed caller `OrderState`/`Result`, and a
  caller-owned `SqliteTransaction` commit/rollback. The pattern is 33 raw lines smaller than the
  plan's unique solution for this workload and has the same runtime allocation (within a few
  percent), diagnostics, cancellation, exception, rollback, skip, and inspection behavior.
- Teams that need ordinary debugging and stack traces. The direct method's steps are visible in the
  call stack; the plan's executor adds a generic loop and delegate frames without changing the
  domain algorithm.
- Any workflow that already has a competent named-step helper. The plan's executor duplicates that
  helper's responsibility; adding a second framework for one or two workflows adds a learning and
  maintenance surface.
- Workflows whose dependencies are genuinely a graph (fan-out, parallel reads, typed per-edge
  values). The plan's linear sequence with named dependencies is not a scheduler; `Task.WhenAll`,
  `Result.Zip`, or a hand-written graph is the better shape. The plan does not infer or enforce
  dependencies.

**Where a local, non-public plan-shaped adapter could pay:**

- Several (two or more) workflows with the same transaction/state/metadata shape, where
  the 323-line per-workflow call-site saving repays the 356-line unique core. The adapter should
  stay application-local, storage-specific, and explicit about the transaction it commits; it
  should not become a public FunnySharp carrier.
- A team that wants the metadata adjacent to the step delegate and is willing to maintain the
  core. The plan's `Create` validation (unique names, earlier-only dependencies, conditional
  consistency) is useful; the direct array/validator provides the name/dependency checks, and the
  shared verifier provides the workload check.

**Where an existing persistence/transaction abstraction is preferable:**

- Applications already using EF Core for mapping and change tracking. EF Core's explicit
  `IDbContextTransaction` and per-step `SaveChangesAsync` provide the real transaction and a
  smaller consumer (1,014 raw lines here), at 9-11x the allocation on the database-operation
  rows. Adding the plan on top would add naming and a second execution model without removing
  EF's cost. Use EF's transaction and a descriptor array/helper if named inspection is needed.
- Any application that needs distributed transactions, durable execution, retries, or
  compensation. The plan deliberately does not provide them; the storage/adapter and the
  application own those guarantees.

**Always true:**

- The transaction is the storage adapter's. For SQLite, use the explicit `SqliteTransaction`;
  do not assume `TransactionScope` enlistment. For a different provider, verify enlistment and
  isolation before relying on it.
- External effects are outside the transaction. Mark them (`External`), record what happened, and
  own compensation/idempotency in the application. Do not read the plan's `Completed` list as a
  promise that external work was undone.
- A commit failure is not a domain failure. Report it distinctly; attempt rollback; do not claim
  success or exactly-once.

## Maintainer Judgment

**REJECT a public typed operation plan; retain the documented named-step pattern.** The evidence
supports a pattern, not a library type:

- The plan's unique complete solution is 1,081 raw lines versus 1,048 for the direct pattern and
  1,014 for EF Core; the raw whole-solution table is 1,309 versus 1,048 versus 1,014. The plan's
  356-line unique core plus 725-line consumer does not repay one application; the per-workflow
  call-site saving is 323 lines, so the core needs roughly 1.1 comparable workflows to break even
  on source volume. The direct pattern's `NamedStep`/descriptor helpers are already reusable.
- The plan's complete-operation allocation is 0.6-4.4% lower than direct on most rows and 5.6%
  higher on pre-cancel; timing has no direction. The plan allocates about 9-11x less than EF Core
  on the database-operation rows, but EF Core is a persistence/change-tracking abstraction, not a
  naming layer. No material runtime advantage is established.
- The plan's diagnostics are identical to the direct/EF consumers: same kinds, operations,
  completed names, typed errors, retained state, database rows, and gateway attempts. The plan's
  `RunFailure` is the shared vocabulary for all three; the direct helper already produces it.
- The plan's inspection metadata is equivalent to the direct descriptor array; the verifier checks
  every field. The plan's inline metadata and conditional validation are a maintainability
  improvement, not a capability the pattern lacks. The plan cannot make the dependency edges
  typed or enforce that a step reads only its declared fields.
- The real transaction is `SqliteTransaction`; the plan's `IPlanTransaction` is a seam, not a
  guarantee. The ambient `TransactionScope` does not roll back this provider's commands, and the
  external gateway is not transactional. The plan's `External` marker is honest but the plan
  cannot compensate it. A public plan could invite the wrong assumption that it is a workflow or
  transaction framework; the product contract already excludes automatic transactions and workflow
  engines.
- The smallest useful shape is the pattern: a validated static `OperationInfo[]`, a
  `NamedStep.RunAsync` helper, a typed state/`Result`/`RunFailure`, and a caller-owned
  `SqliteTransaction`. The plan's executor is a local convenience, not a FunnySharp capability.

Non-adoptions:

- No public plan type, adapter, transaction abstraction, package, analyzer, grammar,
  product-contract, stability-inventory, or API-baseline change. `src/` is untouched.
- No dependency graph, topological/parallel scheduler, DI container, durable workflow engine, actor
  runtime, retry/backoff, compensation API, idempotency key, outbox dispatcher, or exactly-once
  promise.
- No typed per-edge dependency values. Dependencies are named metadata over a typed `TState`; a
  step can read any state field, and the plan cannot prove otherwise.
- No claim that `TransactionScope` is a transaction for `Microsoft.Data.Sqlite`; no distributed or
  ambient transaction claim.
- No claim that the plan's small allocation differences are material or stable beyond this host;
  timing is directional.
- No EF Core replacement or query translation. The EF consumer is an existing persistence/
  transaction abstraction with a different responsibility.
- No change to Goals 0039, 0040, or 0041; their shapes are references only.

Conditions for a later reconsideration:

1. Several (two or more, preferably three) real application workflows share the same transaction,
   state, and metadata shape, and a measured complete-workflow source or runtime gain appears.
2. The shape remains application-local and storage-specific, or a later accepted goal defines a
   storage-agnostic seam without implying transaction, durability, or compensation guarantees.
3. The dependency story is honest: named metadata over a typed state, or a different graph shape
   with typed inputs if typed per-edge dependencies are required.
4. External effects remain explicitly marked and uncompensated; no public plan claims exactly-once
   or automatic rollback.
5. The product contract is reconciled by a later goal if a public capability is proposed; this
   research does not change it.

## Residual Risks And Open Questions

- The EF transaction-open cancellation scenario injects an `OperationCanceledException` at the
  boundary immediately before `BeginTransactionAsync`; it proves the consumer's typed mapping and
  cleanup, not the instant at which a provider observes a racing token. The consumer catches any
  cancellation from `BeginTransactionAsync`, so both the deterministic seam and the provider path
  reach the same `Canceled` outcome.
- The workload is synthetic though representative: nine steps, three order lines, small SQLite
  tables, an in-memory measurement database, and a simulated gateway. Production workflows may have
  more steps, longer transactions, network latency, retries, concurrent writers, larger result
  sets, and different isolation; the allocation and timing rankings may change.
- The in-memory measurement database is not durable. The file-database tests prove transaction
  visibility and commit failure for this provider, but they are not a production durability,
  crash-recovery, or latency benchmark.
- The plan's construction row (4,200 B / ~3.5 µs) is a per-build cost; a static plan pays it once,
  and cached `Describe` is zero bytes. The direct static descriptor array is zero per access. The
  plan's core lines, not its per-operation describe, are the traceability cost.
- The plan's `TState` uses nullable fields for prior results; the failure carries `TError?`. The
  workload uses a reference-type `OrderError` hierarchy. A struct error would need an explicit
  presence flag or an `Option`-style error field; the prototype does not settle that shape.
- The plan's `IPlanTransaction` and the environment's `SqliteTransaction` are separate references.
  The caller must pass the same transaction; the plan cannot enforce it. A step could use another
  connection or transaction and the plan would still report success if the delegate returned
  success. The direct and EF consumers have the same caller responsibility.
- The plan's dependencies are declared names; a step can read any `TState` field. The plan validates
  that names are earlier and unique, not that the delegate uses exactly those fields. Typed
  per-edge dependencies would require a different graph/inputs design and were not prototyped.
- The EF Core consumer's model build and query-plan cache are warmed before measurement; the
  first-run model cost is excluded. EF's allocation includes mapping/change tracking, so it is not
  a pure transaction-facility cost. The 1,014-line EF consumer is smaller than the raw plan/direct
  consumers because EF owns persistence mapping; it is not a naming/inspection plan.
- The ambient `TransactionScope` finding is specific to `Microsoft.Data.Sqlite` 10.0.12 on this
  host. Another provider or version may enlist; the evidence does not generalize the negative
  finding beyond this adapter.
- The SQLite `SQLITE_BUSY` observation depends on the one-second busy timeout and the default
  rollback journal; WAL mode and a different timeout change the concurrency behavior.
- The measurement host is shared (4 cores, 8 GB, external load observed); timing is directional
  and the two runs show roughly 1.0-1.2x timing spread. Allocation is stable across the two runs.
- The plan's shared diagnostic vocabulary (`OperationInfo`, `OperationSuccess`, `RunFailure`) is
  in the non-shipping core; no public API is proposed. The line counts double-count the raw SQL
  and alternative helpers as disclosed; they are not a LOC target.
- The plan's `Describe` exposes metadata but not the delegates; a caller cannot inspect the
  delegate body. The direct descriptor array has the same limit. Reflection or source analysis is
  not added.
- The plan's `Conditional` flag is tied to a condition delegate at construction; the direct/EF
  arrays are static and the branch is in the consumer. The verifier checks the workload; a future
  direct workflow could drift its array from its branch without a construction-time check. The
  plan's validation advantage is real but narrow.
- The plan's `Create` validation does not check that a dependency's result is actually used, nor
  that a step with `RequiresPriorResult=true` actually uses a prior result. The flags are caller
  declarations; the verifier checks this workload's declarations.
- The plan has no retry, timeout, or compensation policy. A production workflow that needs any of
  them must build it outside the plan; the plan's transaction boundary is only as good as the
  adapter and the caller's external-effect handling.
- The goal's comparison is one workload and one comparison set. A different workload with a
  different transaction shape could change the code-volume/runtime trade-off; the conditions above
  define when to reopen it.

## Gate Evidence

Final gate results for this working tree:

- `dotnet fsi build.fsx -- -p build`: exit 0; 0 warnings, 0 errors (8.04 s).
- `dotnet fsi build.fsx -- -p test`: exit 0; 3,000 passed, 0 failed, 0 skipped (32.35 s) on a
  quiet host, including 28/28 in `FunnySharp.OperationPlans.Tests`.
- `dotnet fsi build.fsx -- -p format`: exit 0; C# formatting clean. The formatter reports the known
  F# project skip (`eng/harness` and `tests/FunnySharp.Harness.Tests` are not C#/VB).
- `dotnet fsi build.fsx -- -p verify-docs-snippets`: exit 0; 81 snippets across 15 primary guides.
- `dotnet fsi build.fsx -- -p verify-api-baseline`: exit 0; 115 types and 989 members across 2
  assemblies.
- `dotnet run --project examples/FunnySharp.Examples/FunnySharp.Examples.csproj -c Release`:
  exit 0; examples passed.
- `dotnet restore FunnySharp.slnx --locked-mode`: exit 0; the EF Core provider and its transitive
  SQLite packages restore from the committed lock files.
- Measurement harness, run 1 at 2026-11-09 14:27:21 UTC: exit 0; 1,612 parity checks; tables above.
- Measurement harness, run 2 at 2026-11-09 14:27:43 UTC: exit 0; 1,612 parity checks; tables above.

Run the harness with:

```shell
dotnet run --project tests/FunnySharp.OperationPlansMeasurements/FunnySharp.OperationPlansMeasurements.csproj -c Release
```

Goal 0042 is archived unchanged; the landing commit becomes the durable identity. No public
capability changed. The prototype, tests, and measurement harness remain under `tests/` and are
not referenced by `src/`. The product contract, grammar, stability inventory, and API baseline
retain their existing boundaries; no automatic transaction, workflow engine, DI container,
scheduler, compensation, or exactly-once promise was added.
