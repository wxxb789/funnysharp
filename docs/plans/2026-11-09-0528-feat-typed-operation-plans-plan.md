---
title: Typed inspectable operation plans feasibility
type: feat
date: 2026-11-09
artifact_contract: ce-unified-plan/v1
product_contract_source: docs/goals/archive/0042-goal.md, docs/product-contract.md, docs/grammar.md, docs/core-design.md
execution: code
---

# Typed Inspectable Operation Plans Plan

## Goal Capsule

- **Objective:** Decide whether a typed, inspectable sequence or dependency structure of named
  business operations gives FunnySharp consumers a material advantage over explicit `async`/`await`
  code and an existing transaction facility for a real multi-operation workflow. Completion is a
  maintainer-ready ADOPT / ADAPT / DEFER / REJECT decision backed by an executable consumer, an
  equivalent reference behavior, typed end-to-end results, named failures that retain earlier
  context, explicit duplicate-name / dynamic-dependency / cancellation / callback-exception /
  partial-execution semantics, and a blunt boundary between one storage adapter's real transaction
  and arbitrary external effects. A supported negative finding completes the research; it does not
  claim a shipped capability.
- **Means:** One non-shipping prototype in `tests/FunnySharp.OperationPlans` containing a typed
  operation plan over a caller-owned state record (`OperationPlan<TEnvironment, TState, TError>`), a minimal
  `IPlanTransaction` seam, descriptor validation, and an executor. A shared SQLite-backed
  order-placement workload in `tests/Shared/OperationComparison` drives three complete consumers:
  the typed plan, a direct `Result`/`await` consumer with a static descriptor array and a
  named-step helper, and a `Microsoft.EntityFrameworkCore.Sqlite` consumer as the existing
  transaction abstraction. A shared verifier checks exact rows, outcomes, error identities,
  inspection metadata, rollback, cancellation, callback faults, commit failure, isolation, and the
  external-effect boundary. Two isolated Release measurement runs report construction/inspection
  cost, complete-operation allocation, directional timing, diagnostics, and consumer line counts.
- **Authority:** [Goal 0042](../goals/archive/0042-goal.md) is the completion contract.
  [Product contract](../product-contract.md) excludes automatic transactions, workflow engines,
  durable execution, DI containers, and custom schedulers; [grammar](../grammar.md) and
  [core design](../core-design.md) remain the current boundaries. Goals 0039, 0040, and 0041 are
  the shape references for a research outcome. This plan starts no other goal.
- **Execution profile:** Prototype and comparison code live under `tests/` with
  `IsPackable=false`; `src/` is untouched. `Microsoft.EntityFrameworkCore.Sqlite` 10.0.12 is a
  test-only package reference with committed lock files; it resolves `Microsoft.Data.Sqlite`
  10.0.12 transitively. Tests use xUnit v3 and the shared verifier; the measurement executable is
  a plain Release console program. No shipping project, package, API baseline, grammar,
  product-contract, or stability-inventory entry changes unless a public shape is actually adopted.
- **Tail ownership:** The maintainer judges whether named, inspectable operation metadata and a
  reusable executor repay their core, per-consumer metadata, adapter, and semantic surface. The
  evidence must identify where plain `await`, a static descriptor array plus a small named-step
  helper, an existing persistence abstraction, or ordinary exceptions remain preferable. A
  negative decision is complete and does not claim a shipped capability.
- **Stop for a new decision if:** the only defensible prototype requires a durable workflow engine,
  a custom scheduler, an ambient transaction, a DI container, a universal plan carrier in
  `src/FunnySharp`, or a dynamic type system to keep results typed; the representative workload
  shrinks to a toy chain that cannot show rollback, external effects, cancellation, callback
  faults, commit failure, or dynamic dependencies; or the SQLite packages cannot be restored with
  locked dependencies and the honest fallback would not provide real transactional semantics.

## Product Contract

### Chosen workload and why it is representative

The workload is a single "place an order" business operation over a small but real SQLite schema:

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
9. `queue-review` conditionally inserts a review row when the total reaches a threshold. Whether
   it runs is not knowable until `create-order` produces the total.

This is a real task because it mixes dependent reads, generated identifiers, a multi-row write
whose count and success depend on prior results and stored stock, a business rule over an earlier
result, an external effect, a transactional integration event, and a conditional step. It also has
natural failure points before and after writes and before and after the external effect. The
schema is ordinary SQL, not a FunnySharp abstraction, and the business outcome is observable as
rows plus a typed outcome. A single-insert or arithmetic chain would not exercise the goal's
boundaries; a large order-management system would add unrelated domain rules.

The database is SQLite through `Microsoft.Data.Sqlite` 10.0.12 in a non-shipping test project.
SQLite is chosen because it supplies real `BEGIN` / `COMMIT` / `ROLLBACK` semantics on one storage
adapter without a server, and because its limits are easy to state honestly. The measurement uses
a fresh in-memory shared-cache database per operation so every row starts from the same state; the
in-memory database has no durability. The isolation, rollback, and commit-failure tests use a file
database with the default rollback journal and `PRAGMA foreign_keys=ON`; those tests observe real
cross-connection isolation and on-disk atomicity. SQLite provides no distributed transaction, no
ambient `TransactionScope` enlistment, no exactly-once external effect, and no compensation.

### Storage adapter and its actual guarantees

`SqliteTransaction` is the only transaction facility used by the plan and direct consumers. Its
actual guarantees on this workload are:

- **Atomicity:** `COMMIT` makes all writes visible; `ROLLBACK` makes none visible. A failed commit
  is reported as a distinct outcome; the plan attempts a rollback and does not claim success.
- **Isolation:** a second connection reading during an open write transaction sees the pre-commit
  state; a concurrent writer receives `SQLITE_BUSY` / `database is locked` under the configured
  busy timeout. The tests use a short timeout so the failure is observed quickly.
- **Durability:** a file database with the default rollback journal and `synchronous=FULL` is
  durable at commit on this host; the in-memory measurement database is not durable. The evidence
  states this difference rather than generalizing.
- **No distributed transaction:** one SQLite connection/file. The external gateway call is not
  enlisted and cannot be rolled back.
- **No ambient transaction:** a probe records that `TransactionScope` does not roll back commands
  executed by this provider without an explicit `SqliteTransaction`; the consumer must not assume
  ambient enlistment. This is an existing-facility finding, not a plan capability.
- **No exactly-once execution:** a commit can fail or its client can lose the acknowledgement after
  the database committed. The plan reports the outcome it observed; retries, idempotency keys, and
  reconciliation remain application responsibilities.

`IPlanTransaction` is deliberately minimal: `CommitAsync` and `RollbackAsync`, with the caller
opening, owning, and disposing the real transaction. The plan does not open connections, create
databases, retry, compensate, or interpret storage errors. The application-side
`SqlitePlanTransaction` adapter is charged to the plan consumer; the direct consumer uses
`SqliteTransaction` directly.

### Reference behavior

The reference is explicit, not merely "whatever the direct consumer returns":

- Happy path: one order, three order lines, one payment, one outbox row, one review row for a total
  at or above the threshold, decremented `available` and incremented `reserved` inventory, one
  gateway authorization.
- Small order: the same rows without a review row, and the review step reported as skipped.
- Customer missing or inactive: failure at `load-customer`; no orders, lines, payments, outbox,
  reviews, or inventory changes; no gateway call.
- Credit limit exceeded: failure at `check-credit`; the already-inserted order and lines are rolled
  back; no inventory or gateway effect.
- Insufficient stock: failure at `reserve-inventory` at the responsible SKU; earlier inventory
  updates and the order/lines are rolled back; no gateway call.
- Payment declined: failure at `authorize-payment`; database writes are rolled back; the external
  gateway records the declined attempt, which is not compensated.
- Outbox fault after authorization: the step throws; the database is rolled back, the gateway
  authorization remains, and the outcome preserves the completed operation names, the order id,
  the total, and the payment reference.
- Cancellation before the first step, between steps, and inside a step: no commit; rollback;
  cancellation is distinguishable from a domain failure and from an unexpected exception.
- Duplicate names, empty names, forward dependencies, duplicate dependencies, and empty plans are
  rejected at construction with an explicit exception.
- Commit failure via a deferred foreign-key violation: `CommitFailed` is distinct; the database is
  rolled back for this adapter; the state after all steps is retained for diagnosis. A rollback
  failure is reported separately and never hides the original failure.

The shared verifier checks these rows, counts, identities, and outcomes for all three consumers.
The direct consumer is one of the compared implementations, but the expected values are written
independently in the verifier so a bug in the direct consumer cannot define the reference.

### Smallest useful shape

The candidate is the smallest typed plan that can express the workload. Ecto.Multi is the reference
for named outcomes and inspectability; the plan borrows the named sequence and named failure, not
its repository, changeset, or database semantics. The transaction remains the caller's storage
adapter; the plan does not open, retry, compensate, or interpret it. The shape is:

```csharp
public sealed record OperationInfo(
    string Name,
    OperationKind Kind,
    IReadOnlyList<string> DependsOn,
    bool RequiresPriorResult,
    bool DynamicWork,
    bool Conditional,
    string Note);

public sealed class OperationStep<TEnvironment, TState, TError>
{
    internal OperationInfo Info { get; }
    internal Func<TEnvironment, TState, CancellationToken, Task<Result<TState, TError>>> Run { get; }
    internal Func<TState, bool>? Condition { get; }
}

public interface IPlanTransaction
{
    ValueTask CommitAsync();
    ValueTask RollbackAsync();
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

`OperationInfo` is the shared inspection vocabulary (name, kind, declared dependencies, and the
knowledge flags). `OperationStep` couples the metadata to the delegate so the two cannot drift.
`OperationPlan.Create` validates the metadata and copies the steps; `Describe` is side-effect free.
`RunAsync` is the only executor: it iterates in declared order, evaluates conditions, checks
cancellation at step boundaries and before commit, catches cancellation and exceptions, rolls
back on failure, and commits on success. `IPlanTransaction` is the one-storage-adapter seam; it is
not an ambient transaction, a scheduler, or a retry policy. `OperationSuccess` carries the final state,
executed names, and skipped names; `RunFailure` is the shared diagnostic value described below.

A narrower documented pattern is also a candidate and is the comparison's baseline: a static
`OperationInfo[]` descriptor array, a hand-written `NamedStep.RunAsync` helper that catches domain
failure, cancellation, and exceptions while tracking completed names, and a direct
`SqliteTransaction` block that commits or rolls back. This pattern needs no generic plan core and
no `IPlanTransaction`; it is charged to the direct consumer. The evidence decides whether the plan
type repays its core over this pattern; if it does not, the documented pattern is the smallest
useful shape.

### Names, dependencies, and knowledge flags

- **Names** are unique ordinal, non-empty, non-whitespace strings. A duplicate, empty, or
  whitespace-only name is a construction failure (`ArgumentException`) with the offending name.
  A plan with no steps is rejected; an empty transaction is not a meaningful use of the plan.
  Names are case-sensitive.
- **Declared dependencies** are names of earlier steps. A forward reference, an unknown name, a
  self-reference, or a duplicate dependency is a construction failure. Dependencies are
  inspection and validation metadata, not a scheduler: execution is always the declared sequence
  order, never a topological or parallel order. A step may declare several prior dependencies.
  The plan cannot prove that the delegate reads only the declared fields; the declaration is an
  explicit contract, and the verifier checks it against the workload.
- **`RequiresPriorResult`** means the step's arguments or decision are computed from a preceding
  step's result (for example, the order id, the computed total, the payment reference, or the
  line list). The descriptor makes this visible before execution; the actual value is not.
- **`DynamicWork`** means the step emits a number of sub-operations or statements that the static
  descriptor does not enumerate (for example, one line insert per requested SKU, or one inventory
  update per line, with database-dependent success). The descriptor names the steps; it does not
  list the sub-writes.
- **`Conditional`** means the step may be skipped based on a preceding result. The descriptor
  lists the step and its dependencies; whether it executes is not knowable until the condition
  sees the state. The run outcome records the step as skipped rather than completed.
- **`Kind`** distinguishes `Read`, `Write`, and `External`. `External` is the honest boundary: the
  plan can sequence and report the call but cannot commit or roll it back with SQLite. A
  transactional outbox is a write step; the actual external delivery is not performed by the plan.

### Cancellation, callback exceptions, partial execution, and commit

- **Cancellation:** the token is forwarded to every step. The executor checks it before each step,
  after each successful step (therefore before the commit boundary), and before opening the first
  step. A cancellation observed at a boundary returns `Canceled` with the next step's name as the
  unstarted operation, the completed names, and the last good state. An
  `OperationCanceledException` thrown by a step or condition returns `Canceled` with that step
  named as the in-flight operation. Rollback is not cancellable, so cleanup is attempted even
  after the caller cancels. Once `CommitAsync` starts it is not canceled, so a commit is not
  abandoned halfway. The documented consequence is that a cancellation racing the commit may
  arrive after a successful commit; callers that need stronger guarantees own idempotency and
  reconciliation.
- **Callback exceptions:** a non-cancellation exception from a condition or step, including a
  null task returned by a step, is caught and reported as `OperationFaulted` with the step name,
  the exception, the completed names, and the last good state. The plan does not rethrow; the
  caller can inspect or rethrow. A domain `Result` failure is reported as `OperationFailed` with
  the typed error, never flattened to an exception or a string.
- **Partial execution:** after any failure the executor attempts `RollbackAsync`. The database
  writes of completed steps and the failing step are not committed; `Completed` records which
  steps had returned success before the failure, and `State` records the last good typed state.
  This is diagnostic context, not a claim that those writes survive. An external effect that
  already happened is not undone.
- **Commit failure:** `CommitAsync` failure is `CommitFailed` with the exception, all executed
  names, and the final state. The executor attempts rollback; if rollback also fails, the outcome
  is `RollbackFailed` with both the original failure and the rollback exception. The plan never
  reports success when the commit did not complete. A lost acknowledgement after a successful
  commit remains an application reconciliation problem.
- **Rollback failure:** `RollbackFailed` sets `OriginalKind` to the pre-rollback kind and
  preserves the operation, typed error or exception, completed names, and state alongside the
  rollback exception. The database state is unknown; the caller must not assume either the writes
  or the rollback.
- **Multiple runs:** a plan is immutable and holds no run state. The same plan can run against
  different transactions and initial states. The workload verifier runs each plan twice on fresh
  databases to prove isolation.

### Compared implementations and fairness

| Implementation | Shape | Charged to it |
| --- | --- | --- |
| Typed plan | `OperationPlan<OrderState, OrderError>` with nine `OperationStep`s, `OperationInfo` metadata, dependency validation, and the executor; a `SqlitePlanTransaction` adapter. | The plan core (`OperationPlan`, `OperationStep`, `IPlanTransaction`, validation and executor), the SQLite adapter, and the nine step definitions with inline metadata. The shared `OperationInfo` record is the comparison vocabulary and is reported separately. |
| Direct `Result`/`await` | A hand-written `PlaceOrderAsync`/`RunAsync` pair in `DirectOrderConsumer` with a `SqliteTransaction`, a local `NamedStep.RunAsync` helper that maps domain failure / cancellation / exception and tracks completed names, and a static `OperationInfo[]` descriptor array with a validator. | The direct method, the named-step helper, the static descriptor array and validator, and the transaction open / commit / rollback code. No generic plan core and no plan adapter. |
| EF Core | A `ShopDbContext` with entities mapped to the same tables, a `SaveChangesAsync` per write step inside an explicit `IDbContextTransaction`, the same gateway, and the same static descriptor array and named-step helper. | The context, entity model, mapping configuration, consumer, descriptor array and validator, and transaction handling. EF Core's mapping, change tracking, SQL generation, and provider packages are part of this alternative; they are not a like-for-like plan overhead. |
| Ambient `TransactionScope` | A probe that opens a scope, executes a SQLite insert, completes or omits `Complete`, and checks whether a rollback occurs; explicit enlistment is also probed. | Not a measured consumer: the probe shows whether this existing facility provides the required guarantee. It is reported as a negative capability finding, not as an equivalent implementation. |

Shared and not charged to any consumer: the schema, seed fixtures, SQL statement helpers, typed
`OrderState` / `OrderError` values, `RunFailure` diagnostic value, `OperationInfo` inspection
record, the simulated gateway, the verifier, and the measurement harness. The direct and EF
consumers share the alternative `NamedStep.RunAsync` helper and descriptor validator as source, but
their line counts include those lines because the alternative would need them; the plan's core
counts its own executor and validation. The EF Core consumer uses EF-generated SQL, not the raw
SQL helpers, because it is the existing persistence/transaction abstraction; that difference is
disclosed rather than pretending the implementations are byte-identical.

The plan does not get the direct consumer's static descriptor array for free, and the direct
consumer does not get the plan's generic executor for free. Both carry complete code for their
chosen approach.

### Measurement protocol

- **Environment disclosed:** host OS, kernel, architecture, logical cores, .NET SDK/runtime,
  Release build, `TieredCompilation=false`, UTC date and start times. Allocation is the blocking
  comparison; timing is directional on this shared host.
- **Database setup:** every measured operation gets a fresh in-memory shared-cache SQLite database
  with the schema and seed rows. Creation and seeding happen before the allocation/time snapshot
  and are not charged to any consumer. The measured operation is the complete business operation
  from transaction open to commit or rollback; database provisioning is reported separately.
- **Measured scenarios:** (a) plan construction plus `Describe` with no database and no step
  execution, compared with the direct static descriptor access; (b) happy path with review; (c)
  small order with review skipped; (d) insufficient stock failure; (e) payment decline; (f) outbox
  fault after external authorization; (g) cancellation before the first step; (h) cancellation
  between steps; (i) a credit-limit failure. Every scenario is the same business operation for
  every consumer; the failure scenarios use the same request, gateway behavior, and fault
  injection hook.
- **Metrics:** allocated bytes per operation from `GC.GetTotalAllocatedBytes(precise: true)` with
  the fresh database outside the snapshot, directional nanoseconds per operation, executed and
  skipped step names, outcome kind and operation name, database row counts, gateway authorization
  count, and consumer raw source lines.
- **Measurement harness:** a custom per-iteration loop rather than the shared
  `MeasurementHarness.MeasureAsync`, because the fresh database must be outside the measured
  window and the shared harness has no setup/teardown seam. The loop performs warmup, three
  internal rounds, and takes the minimum bytes and time per round; the sandbox allocation is
  excluded. The harness verifies parity with the shared verifier before measuring. The plan
  construction/inspection row uses a synchronous loop with
  `GC.GetAllocatedBytesForCurrentThread`.
- **Two runs:** the complete measurement executable is run twice in isolation. Both process runs
  are recorded. The harness is never run concurrently with the test suite; one heavy process at a
  time. It is run after the test gate, not during it.
- **Diagnostics:** the evidence table reports the failure kind, responsible operation, typed error
  or exception identity, completed names, retained state fields, database rows, and gateway ledger
  for each consumer. Allocation and code-volume rows describe the same operation; any helper is
  charged to the side that uses it.
- **Identity:** no custom fingerprints or receipts. Source identity is the Goal 0042 working tree;
  the landing commit becomes the durable identity.

### Scope markers

**Included:** non-shipping plan core, SQLite schema/workload, direct consumer, EF Core consumer,
simulated external gateway, shared verifier, xUnit semantics/comparison tests, transaction-boundary
probes, allocation/diagnostic/line-count measurements, evidence record, and goal close.

**Deferred to follow-up work (only if a later goal reopens):** a public plan type or adapter in
`src/FunnySharp`; a storage-agnostic transaction abstraction; a dependency-graph scheduler; parallel
step execution; retry/backoff; durable execution; compensation APIs; idempotency keys; an outbox
dispatcher; and any DI integration.

**Outside this product's identity:** a universal plan carrier, durable workflow engine, actor
system, custom scheduler, ambient transaction, distributed transaction, exactly-once effect
promise, automatic rollback of external effects, ORM replacement, or migration tool.

## Planning Contract

### Requirements

- **R1:** Add a non-packable `tests/FunnySharp.OperationPlans` project containing the typed plan
  core (`OperationInfo`, `OperationKind`, `OperationStep`, `OperationPlan`, `OperationSuccess`,
  `IPlanTransaction`) and no shipping reference from `src/`.
- **R2:** Keep caller results typed end to end through a caller-owned `TState` and the existing
  `Result<TState, TError>` carrier; introduce no parallel error universe, no `object`/dictionary
  state, and no exception-to-domain-failure flattening.
- **R3:** Validate non-empty/whitespace/duplicate names, non-empty plans, earlier-only dependencies,
  duplicate dependencies, null steps/info/delegates/conditions, and immutable copies at
  construction. Expose side-effect-free `Describe`.
- **R4:** Implement the executor with declared-order iteration, condition evaluation, cancellation
  at step boundaries and before commit, domain-failure / cancellation / exception separation,
  rollback on every failure, distinct commit failure, rollback failure preservation, and no commit
  on a failed step.
- **R5:** Add the shared SQLite workload: schema, seed fixtures, SQL helpers, request/state/error
  types, deterministic gateway, fault injection, and the nine-step reference behavior.
- **R6:** Implement the direct `Result`/`await` consumer with a `SqliteTransaction`, a named-step
  helper, a static `OperationInfo[]`, a descriptor validator, and complete commit/rollback handling.
- **R7:** Implement the EF Core consumer as the existing transaction abstraction over the same
  business operation and tables, with an explicit `IDbContextTransaction`, per-write
  `SaveChangesAsync`, the same gateway, and the same descriptor array/validator.
- **R8:** Implement a shared verifier that checks exact rows, counts, outcomes, error identities,
  completed/skipped names, state retention, inspection metadata, duplicate/forward-dependency
  rejection, plan immutability, multiple runs, cancellation, callback faults, partial execution,
  commit failure, rollback failure, external-effect persistence, ambient-scope behavior, and
  cross-connection isolation.
- **R9:** Add xUnit v3 tests for every plan and consumer promise, including guards and boundary
  cases; no skipped tests; the shared verifier is called once from the suite and from the
  measurement harness.
- **R10:** Add the Release measurement executable: environment and workload description, raw line
  counts, construction/inspection row, happy/small/failure/outbox/cancel rows, diagnostics table,
  parity check count, and two isolated complete runs.
- **R11:** Exercise the storage boundary directly: commit/rollback visibility with two connections,
  deferred-foreign-key commit failure, ambient `TransactionScope` behavior, and the external
  gateway ledger after a later database failure. No compensation or exactly-once claim.
- **R12:** Write `docs/typed-operation-plans-evidence.md` with the decision, workload rationale,
  model and semantics, fairness, both measurement runs, diagnostics, consumer expression,
  workload guidance, non-adoptions, residual risks, and an explicit statement that no public
  capability changed unless one was actually adopted.
- **R13:** Move `docs/goals/0042-goal.md` to `docs/goals/archive/0042-goal.md` unchanged; update the
  goals index and navigation docs; record the maintainer judgment and evidence links.
- **R14:** Run every required gate and the measurement harness twice, one heavy process at a time;
  leave the working tree uncommitted and do not rewrite history.

### Non-goals

- No shipping plan type, transaction abstraction, adapter, package, analyzer, grammar,
  product-contract, stability-inventory, or API-baseline change.
- No durable workflow engine, scheduler, actor runtime, DI container, ambient transaction,
  distributed transaction, or universal application program representation.
- No parallel step execution, topological scheduling, retry/backoff, compensation, idempotency
  key, outbox dispatch, or exactly-once promise.
- No replacement ORM, query translator, migration tool, or database-level integrity guarantee.
- No claim that a SQLite transaction covers HTTP, messaging, filesystem, or payment-gateway
  effects; the external ledger is evidence of the opposite.
- No change to Goals 0039, 0040, or 0041 behavior; their shapes are references only.

## Implementation Units

### U1. Typed plan core

- **Goal:** A small, inspectable, typed plan with explicit semantics.
- **Requirements:** R1, R2, R3, R4.
- **Dependencies:** none.
- **Files:** `tests/FunnySharp.OperationPlans/` (`OperationInfo.cs`, `OperationKind.cs`,
  `OperationStep.cs`, `OperationPlan.cs`, `OperationSuccess.cs`, `IPlanTransaction.cs`,
  `RunFailure.cs`).
- **Approach:** `OperationPlan<TEnvironment, TState, TError>` validates and copies an `OperationStep` array;
  `Describe` returns the immutable `OperationInfo` list. The executor is an `async` loop with a
  completed-name list, skipped-name list, condition guard, per-step try/catch, rollback helper,
  and commit guard. `RunFailure` is the plan core's shared diagnostic value; the plan assembly does not
  need a storage package. Use `Result` success/failure and `TryGetValue`/`TryGetError` rather than
  `Match` allocations where practical.
- **Execution note:** no recursion, no scheduler, no ambient state; rollback and commit are
  delegates on `IPlanTransaction`. Guard every public entry point. The executor checks cancellation
  before each step, after a successful step (which covers the pre-commit boundary), and before
  starting; rollback and commit are not cancellable.
- **Patterns to follow:** existing `readonly record struct`/`sealed record` carriers, `Result` as
  the outcome vocabulary, `ArgumentException` for construction mistakes, `ConfigureAwait(false)`
  in library code.
- **Test scenarios:** duplicate/empty/whitespace names, empty plan, forward/unknown/self/duplicate
  dependencies, null step/info/run/condition, immutable copy, describe without execution,
  conditional skip/execute, domain failure, cancellation before/between/inside, null task,
  condition exception, step exception, commit failure, rollback failure, multiple runs.
- **Verification:** prototype build and focused xUnit tests.

### U2. Shared SQLite workload and three consumers

- **Goal:** The same real business operation implemented by the plan, direct code, and EF Core.
- **Requirements:** R5, R6, R7.
- **Dependencies:** U1.
- **Files:** `tests/Shared/OperationComparison/` (`ShopSchema.cs`, `ShopFixtures.cs`,
  `OrderModels.cs`, `OrderPricing.cs`, `ShopSql.cs`, `SimulatedPaymentGateway.cs`, `NamedStep.cs`,
  `OrderEnvironment.cs`, `SqlitePlanTransaction.cs`, `DirectOrderConsumer.cs`,
  `PlanOrderConsumer.cs`, `EfShopDbContext.cs`, `EfOrderConsumer.cs`, `ComparisonDescriptors.cs`,
  `IOrderConsumer.cs`, `SqliteSandbox.cs`, `OperationComparison.cs`).
- **Approach:** `ShopSchema.CreateAsync(connection)` creates the tables and `PRAGMA foreign_keys=ON`;
  `ShopFixtures.SeedAsync` inserts the standard customer/catalog/inventory;
  `ShopFixtures.StandardRequest()` returns the standard request. `ShopSql` holds shared command
  text/parameter helpers. Direct uses `SqliteTransaction` and `NamedStep.RunAsync`; plan uses
  `OperationStep` metadata plus the SQLite transaction adapter; EF uses entities mapped to the same
  table/column names and a context over the open connection. The gateway records
  attempts/authorizations in a caller-owned ledger and can decline or record. The fault hook makes
  the outbox step throw before writing for the callback exception scenario, and injects
  transaction-open, commit, and rollback faults for the cancellation and rollback-failure
  scenarios.
- **Execution note:** every consumer returns the same `Result<OrderState, RunFailure<OrderState>>`
  for success/failure, and the same row/ledger state. The direct consumer is the reference
  implementation but expected values are explicit in the verifier. The EF Core model uses
  `ToTable`/`HasColumnName` so the verifier queries the same rows. The plan's adapter wraps
  `SqliteTransaction`; the caller opens/disposes it. No consumer retries.
- **Patterns to follow:** existing shared comparison consumers (0040/0041), deterministic fixtures,
  internal shared types, no test-framework dependency in shared code, no ambient static state.
- **Test scenarios:** happy path, small order, customer missing/inactive, credit limit, insufficient
  stock, payment decline, outbox fault, cancellation before/between/inside, duplicate names,
  forward dependencies, inspection, two runs, external ledger after failure, isolation, commit
  failure, rollback failure.
- **Verification:** shared verifier plus xUnit comparison tests.

### U3. xUnit semantics and comparison tests

- **Goal:** Executable coverage of every promise and every consumer.
- **Requirements:** R8, R9.
- **Dependencies:** U1, U2.
- **Files:** `tests/FunnySharp.OperationPlans.Tests/` (`PlanConstructionTests.cs`,
  `PlanExecutionTests.cs` including the cancellation and rollback-failure cases,
  `OperationComparisonTests.cs`, `SqliteTransactionBoundaryTests.cs`, `AmbientTransactionTests.cs`,
  `PlanTestFactory.cs`, `SqliteTestSupport.cs`).
- **Approach:** call the shared verifier once; add focused boundary tests for construction guards,
  cancellation, callback faults, commit failure, rollback failure, external ledger, isolation, and
  ambient scope. Use a fresh database per test.
- **Execution note:** one public sealed class per file; classic asserts; no `Skip`; no measurement
  in the test suite; keep the TransactionScope test synchronous and disposed so it cannot leak an
  ambient transaction.
- **Test scenarios:** the U2 list plus null/whitespace/immutability/duplicate/forward-dependency
  details and the two-connection isolation probe.
- **Verification:** `dotnet test` on the new project and the solution gate.

### U4. Measurement harness

- **Goal:** Honest construction, inspection, allocation, timing, and diagnostic evidence.
- **Requirements:** R10, R11.
- **Dependencies:** U2.
- **Files:** `tests/FunnySharp.OperationPlansMeasurements/`.
- **Approach:** verify parity first; print environment, workload shape, and raw line counts; run a
  synchronous construction/describe row; run the complete operation rows with a fresh in-memory
  database outside the measured window; report bytes/op, ns/op, outcome, rows, and gateway count;
  print the diagnostics table; print two complete process runs when invoked twice. Use a custom
  per-iteration loop for the database rows and the shared harness for the construction row.
- **Execution note:** never run concurrently with the test suite; one heavy process at a time. The
  in-memory database is created/seeded before each measurement snapshot and disposed after. The
  same request, gateway, and fault injection are used on every row.
- **Test scenarios:** inspection only, happy path, small order, insufficient stock, payment
  decline, outbox fault, cancellation before start, cancellation between steps, credit-limit
  failure, and the diagnostics table.
- **Verification:** two isolated harness runs with verified parity.

### U5. Evidence, decision, and close

- **Goal:** Maintainer-ready evidence and goal closure.
- **Requirements:** R12, R13.
- **Dependencies:** U4.
- **Files:** `docs/typed-operation-plans-evidence.md`, `docs/goals/README.md`,
  `docs/goals/archive/0042-goal.md`, `docs/goals/AGENTS.md`, `docs/AGENTS.md`,
  `docs/plans/AGENTS.md`.
- **Approach:** record the model, semantics, fairness assessment, both measurement runs,
  diagnostics, consumer expression, workload guidance, non-adoptions, residual risks, and the
  ADOPT/ADAPT/DEFER/REJECT judgment. Move the goal unchanged and verify with `cmp`. State
  explicitly that no public capability changed for a negative or research-only decision; reconcile
  product contract/grammar/stability only if a public shape was actually adopted.
- **Execution note:** preserve the goal file byte-for-byte; update the index and navigation after
  the move. Do not start another goal.
- **Test scenarios:** none beyond gates.
- **Verification:** final gate transcript and working-tree review.

## Verification Contract

| Gate | Command | Applies to | Completion evidence |
| --- | --- | --- | --- |
| Build | `dotnet fsi build.fsx -- -p build` | all projects including the new research projects | 0 warnings, 0 errors |
| Test | `dotnet fsi build.fsx -- -p test` | all suites including the new tests | 0 failed, 0 skipped |
| Format | `dotnet fsi build.fsx -- -p format` | C# | clean |
| Docs snippets | `dotnet fsi build.fsx -- -p verify-docs-snippets` | primary guides | unchanged 15 guides / 81 snippets |
| API baseline | `dotnet fsi build.fsx -- -p verify-api-baseline` | shipping assemblies | 0 delta; no public capability changed |
| Examples | `dotnet run --project examples/FunnySharp.Examples/FunnySharp.Examples.csproj -c Release` | examples | passed |
| Measurement | `dotnet run --project tests/FunnySharp.OperationPlansMeasurements/FunnySharp.OperationPlansMeasurements.csproj -c Release` | comparison | two complete isolated runs, parity verified |

## Definition of Done

- **U1-U3:** the plan core, shared workload, three consumers, verifier, and tests build and run;
  construction guards, inspection, typed results, named failures, cancellation, callback faults,
  partial execution, commit/rollback failures, external-effect boundary, and isolation promises
  hold; the direct and plan consumer outcomes match the explicit reference.
- **U4:** the harness reports construction/inspection cost, complete-operation allocation,
  directional timing, diagnostics, line counts, and two isolated runs for every compared
  implementation; the measured operation and setup exclusion are the same on every row.
- **U5:** the evidence states the decision with reasoning and non-adoptions, the goal is archived
  unchanged, navigation is updated, and no public capability is claimed if the decision is not an
  adopt/adapt with a public shape.
- **Global:** every required gate has a real recorded exit code; the working tree is left
  uncommitted; no other goal starts.
