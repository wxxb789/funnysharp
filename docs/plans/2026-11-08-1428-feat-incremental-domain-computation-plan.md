---
title: Incremental domain computation feasibility
type: feat
date: 2026-11-08
artifact_contract: ce-unified-plan/v1
product_contract_source: docs/goals/archive/0040-goal.md, docs/product-contract.md, docs/grammar.md, docs/core-design.md
execution: code
---

# Incremental Domain Computation Plan

## Goal Capsule

- **Objective:** Decide whether explicit incremental computation is a useful and competitive
  optional FunnySharp capability for expensive, shared domain calculations that change locally
  over time. Completion is a maintainer-ready ADOPT / ADAPT / DEFER / REJECT decision backed by an
  executable workload, an honest comparison with full recomputation and a competent hand-maintained
  incremental baseline, and measurements of avoided work, graph maintenance, retained memory,
  update latency, and setup cost. A supported negative finding completes the research.
- **Means:** One non-shipping graph prototype in `tests/` with explicit inputs, statically declared
  `Map`/`Combine` dependencies, an explicit fan-in `Collect` node, value equality cutoffs, a staged
  `Batch` publication boundary, per-node observers, and structured statistics. A shared pricing
  workload with genuinely shared product dependencies and localized updates drives four complete
  consumers: the graph, a full recomputation, a competent hand-maintained incremental engine, and a
  collection-as-one-node graph. A `Microsoft.Extensions.Caching.Memory` product-cache consumer is
  measured as the relevant .NET cache alternative. Two isolated Release harness runs report the
  same tables.
- **Authority:** [Goal 0040](../goals/archive/0040-goal.md) is the completion contract.
  [Product contract](../product-contract.md), [grammar](../grammar.md), and
  [core design](../core-design.md) are current boundaries. Goal 0028 supplies an assessed but
  non-required changeset input; Goal 0034 supplies an assessed but non-required reduction input.
  Goal 0039 is the shape reference for a research outcome. This plan starts no Goal 0041 work.
- **Execution profile:** Graph prototype in `tests/FunnySharp.Incremental/`; shared workload,
  consumers, and verifier in `tests/Shared/IncrementalComparison/`; xUnit v3 semantics and
  comparison tests in `tests/FunnySharp.Incremental.Tests/`; measurements in
  `tests/FunnySharp.IncrementalMeasurements/`. Record the decision in
  `docs/incremental-domain-computation-evidence.md`, move the goal unchanged to archive, and update
  the goals index. No shipping project, package, API baseline, grammar, product contract, or
  stability-inventory entry changes unless a public shape is actually adopted; the prototype is
  non-packable and not referenced by `src/`.
- **Tail ownership:** The maintainer judges whether avoided recomputation pays for graph setup,
  retention, update overhead, and consumer complexity. The evidence must identify workloads where
  ordinary functions or a hand-maintained incremental engine remain preferable. A negative
  decision is complete and does not claim a shipped capability.
- **Stop for a new decision if:** the only defensible prototype needs a public DSL, ambient cache,
  custom task scheduler, distributed reactive system, automatic I/O re-execution, arbitrary
  delegate purity inference, dynamic graph cycles, or a wholesale collection-delta engine; or the
  representative workload shrinks to a trivial arithmetic graph that cannot show shared
  dependencies and localized updates.

## Product Contract

### Chosen workload and why it is representative

The shared workload is a long-lived pricing service for a fixed catalog and order book:

- **Catalog:** 512 products. Each product has a unit cost, tax rate, and category.
- **Orders:** 1,024 orders, each with 8 order lines. A line references a product index and a
  quantity; product references are distributed round-robin, so every product is genuinely shared by
  16 distinct orders and the full model contains 8,192 line references.
- **Derived per-product value:** `ProductEconomics` recomputes the rounded unit price and retains
  tax/category. It is a shared dependency of every order that references the product.
- **Derived per-order value:** a `Result<Quote, PricingError>` containing subtotal, region-dependent
  discount, tax, and total. A quote reads its order input and the economics of every referenced
  product.
- **Consumer-visible result:** the complete post-update quote list for all 1,024 orders.

This is not a trivial arithmetic graph: product nodes have high fan-out (16 orders each), order
nodes have fan-in (8 shared product dependencies), and a localized product-cost update invalidates
only the product and its 16 dependent orders. A quantity update invalidates one order. The domain
work is ordinary decimal pricing, not artificial padding; the scale makes the difference between
8,192 line calculations and 128 line calculations observable. The workload is representative of
catalog pricing, quote refresh, eligibility/risk rollups, and similar shared calculations with
localized value changes.

The graph topology is deliberately **fixed** after construction. Product membership of an order and
order membership of the quote collection are not graph edges: adding or removing an order is a
collection-level change. This keeps node-level recomputation and collection-level delta processing
separate and measurable rather than hiding a collection operation inside a node.

### Compared implementations and fairness

| Implementation | What it does | Why it is fair |
| --- | --- | --- |
| Full recomputation | Recomputes every product economics value and every quote from the model on demand. | Direct, competent C#; no dependency bookkeeping. It is the honest baseline for a consumer that cannot maintain state. |
| Hand-maintained incremental engine | Caches product economics and quotes, maintains a product-to-order reverse index, batches changes, deduplicates dirty orders, applies value equality, and recomputes only affected orders. | It is what a competent engineer writes for this exact domain. It has no generic node scheduler, no reflection, no library, and no strawman re-enumeration. |
| Per-order incremental graph | One node per product and one node per order, with a typed `Collect` fan-in node per order for its product dependencies. Each node retains its value and recomputes only when a dependency publishes a changed value past cutoff. | It pays for generic node and edge maintenance, scheduler bookkeeping, and retained values; the consumer only declares dependencies and pure callbacks. |
| Collection-as-one-node graph | One product-collection node and one order-collection node; a single quote-map node recomputes the complete quote list. | It is the honest demonstration that placing a list in a node does not make collection operations incremental. It supports add/remove only by republishing the whole collection. |
| `Microsoft.Extensions.Caching.Memory` | Caches product economics entries and recomputes every quote by reading the cached product values. | It is the closest general .NET memoization facility for the expensive shared leaf. It shows whether caching alone avoids downstream recomputation; it is not treated as a dependency engine. |

Assessed and not measured as competitors:

- `System.Reactive` supplies push notification and composition, but not an automatic dependency
  graph, glitch-free batch publication, or output cutoff; a fair version would be a hand-wired
  per-order subscription graph with `CombineLatest` and manual disposal, adding subscription
  overhead to the hand-maintained baseline rather than testing a different incremental algorithm.
- `DynamicData` is a collection change-set library. It is the right alternative for collection-level
  deltas, not for node-level shared value dependencies; it is explicitly out of this prototype's
  collection-level scope.
- `QuikGraph` and `ComputeSharp` address static graph algorithms and GPU execution respectively; they
  do not maintain incremental values across updates and are not comparable.

All implementations use the same product/order fixtures and the same `PricingKernel` product and
quote functions. Shared models, fixture construction, verifier code, and result digesting are not
charged to any consumer. Every consumer is charged its own setup, state retention, update path, and
complete post-update quote retrieval.

### Consistency boundary and publishing

- `IncrementalGraph.Batch(Action)` is the explicit publication boundary. Calls to `Input.Set` or
  `Input.SetChanged` inside the action stage pending values; no input value, computed node, or
  observer is published until the outermost batch action returns successfully.
- A successful outermost batch publishes all staged input values, recomputes every affected node,
  and then invokes observers in deterministic node-creation order and observer-registration order.
  Observers never see an intermediate value of a multi-change batch.
- If the batch action throws, every staged input value is discarded and no observer runs. This is
  publication rollback of staged inputs only; it is not a domain transaction and does not roll back
  caller-owned state or external effects.
- A single `Input.Set` outside a batch uses the same staging path and publishes immediately.
- Nested batches join the outer boundary; only the outermost successful action commits.
- Observers and node construction are rejected while a commit is in progress. An observer that
  throws propagates its exception; node values are already consistent, but later observers in that
  commit do not run and no rollback is claimed.
- The graph is single-threaded. It provides no ambient cache, background task, scheduler,
  cross-graph sharing, persistence, or automatic I/O re-execution.

### Input ownership, mutation, equality, and cutoff

- `Input<T>` retains the caller's value by reference; it never clones it. The caller owns mutable
  contents and must not mutate them in place and then call `Set` when the selected equality considers
  the reference unchanged.
- `Set(value)` publishes only when the input's configured `IEqualityComparer<T>` says the pending
  value differs from the published value. `SetChanged(value)` publishes unconditionally. The latter
  is the honest contract for caller-mutated storage; a `List<T>` mutated in place is published with
  `SetChanged`.
- Every computed node has its own output comparer, defaulting to `EqualityComparer<T>.Default`.
  When a recomputed output is equal to the retained output, dependents are not scheduled. This is the
  output cutoff and the reason a changed input can still avoid downstream work.
- Comparers are explicit constructor parameters where the default is not the caller's policy. The
  prototype does not infer equality from a collection interface or attempt deep equality.
- Default values and nulls have ordinary .NET meaning; the graph does not normalize either.

### Error propagation

- Expected domain failure is a value. The workload uses `Result<Quote, PricingError>` nodes; a bad
  quantity yields `Failure`, an observer receives it, and equality cutoff applies to the whole
  `Result` value. `Validation<TValue, TError>` is equally usable as a node value because the graph
  treats node values as opaque; a dedicated test exercises an accumulating `Validation` value.
- A node callback exception is not converted into a domain failure. It propagates out of the
  publishing call, observers are not run for that commit, and the graph makes no rollback or
  recovery promise. Use `Result`/`Validation` for expected failure and treat callback exceptions as
  programming faults.
- A missing product index or invalid quantity is a `PricingError`, not an exception.

### Static versus dynamic dependencies; cycles and unsupported changes

- **All graph dependencies are static.** `Map`, `Combine`, and `Collect` receive already-created
  source nodes and never rebind. There is no `Bind`, dynamic edge API, or mutable dependency list in
  the prototype. A graph is a DAG by construction: a source must exist before a dependent is
  created, and no public operation adds an edge to an existing node. Cycles therefore cannot be
  represented; this is a structural rejection, not runtime cycle detection. Dynamic dependencies
  remain unsupported and are a decisive non-adoption for any future decision.
- **Collection membership is a collection-level change.** The per-order graph has fixed topology; a
  consumer that needs a new order must build a new graph. Its `AddOrder`/`RemoveLastOrder` methods
  throw `NotSupportedException` with an explicit fixed-topology message. The collection-as-one-node
  graph can republish the whole order list and supports membership changes, but recomputes every
  quote; it is never described as a collection delta.
- **Line product membership is static.** The prototype changes quantities and product costs, not
  the set of products referenced by an order. A product-reference change would alter topology and
  reverse indexes; it is unsupported in both the graph and the hand baseline, and is not measured.
- Mutable same-reference publication requires `SetChanged`; plain `Set` deliberately applies the
  selected equality cutoff and is documented.
- Nested batch actions are supported; a failed action discards staged inputs. Mutating the graph
  from an observer or compute callback throws.

### Purity visibility

- Every user computation is wrapped in `Pure<TInput, TResult>`, created only through
  `Pure<TInput, TResult>.Of(...)`. There is no implicit conversion from `Func`. The call site states
  that the callback is deterministic and side-effect free.
- The graph is allowed to skip invocations after equality cutoff, reuse a retained value across
  commits, and invoke callbacks in dependency order. It does not detect impurity, reorder
  non-commutative work, or guarantee invocation counts for impure callbacks.
- Observers are explicitly side-effecting `Action<T>` callbacks and are documented separately from
  pure computations.
- The `Collect` fan-in node is library-supplied and makes no user purity assumption.

### Observers, disposal, and reuse isolation

- `Observe(node, observer)` registers an observer and invokes it immediately with the current
  value; `ObserveChanges(node, observer)` registers without an initial callback.
- Registration returns `IDisposable`. Disposal is idempotent, stops future notifications, removes
  the registration, and does not disturb other observers on the same node. Disposal during
  notification skips any not-yet-invoked callback for that registration.
- Observer registration and `Input.Set` are allowed only outside a commit. Attempting either from a
  running observer or compute callback throws a documented `InvalidOperationException`.
- Every graph owns its nodes, values, observers, queue, and statistics. Two graphs built from the
  same static `Pure` delegates and the same source model have independent state; updating one does
  not affect the other. There is no static cache or ambient reuse.
- Nodes are not disposed individually. The graph and its retained values become collectible when
  the consumer drops the graph and all observers; disposing observers is the explicit lifecycle
  operation.

### Node-level versus collection-level treatment

- **Node-level:** product, per-order `Collect`, and per-order quote nodes. A product update
  recomputes one product, the 16 `Collect` nodes for its dependent orders, and those 16 quote nodes.
  Unaffected order nodes retain their values and are not recomputed.
- **Collection-level:** the collection-as-one-node graph and any add/remove order scenario. The
  whole quote list is recomputed from the whole order list. A list inside a node does not make
  element updates incremental; the evidence reports the complete quote work, not a node count that
  would flatter the mechanism.
- The hand baseline is allowed to maintain a reverse index and a dirty set because that is the
  competent domain-specific implementation; the graph is not credited with reverse-index work it
  does not do.

### Measurement protocol

- **Environment disclosed:** host OS, kernel, architecture, logical cores, .NET SDK/runtime,
  Release build, `TieredCompilation=false`, UTC date. Allocation is the blocking comparison; timing
  is directional on this shared host.
- **Work avoided:** a ledger counts product-economics evaluations, quote evaluations, and line
  evaluations for each consumer and scenario. Graph statistics count recomputed nodes, cutoff stops,
  changed nodes, input changes, observer notifications, node count, and edge count. One product
  update, one order-quantity update, one mixed batch, one no-op update, and collection membership
  changes are recorded. A fresh consumer and ledger are used for each row.
- **Setup cost:** construction plus initial complete evaluation, measured for each implementation,
  with allocated bytes and wall time. Graph setup includes all node/edge/array allocation and the
  initial computation; full recomputation setup includes the initial complete evaluation.
- **Retained memory:** forced-GC retained bytes after setup and after repeated updates, with the
  consumer rooted. The report separates retained input/model values from retained derived values.
- **Update latency:** complete update plus retrieval of all quotes, measured with the shared
  allocation harness for bytes/op and mean ns/op, and a separate timestamp recorder for p50/p95/p99
  and maximum microseconds. Cold/warm boundaries are explicit: setup is reported separately from
  steady-state updates; warmup iterations are discarded.
- **Generation-free identity:** no custom fingerprints or derived receipts. Source identity is the
  Goal 0040 working tree; measurements are reported from the actual harness runs. Two complete
  harness runs are recorded.
- **Serialization:** the measurement harness is not run while the test suite is running. One heavy
  process at a time on the 4-core/8-GB host.

### Scope markers

**Included:** non-shipping static incremental graph prototype; shared pricing workload; full,
hand-maintained, per-order graph, collection-as-node graph, and `MemoryCache` consumers; semantics
tests; work-avoidance, setup, retention, allocation, and latency measurements; evidence record and
goal close.

**Deferred to follow-up work (only if a later goal reopens):** dynamic dependencies (`Bind`),
per-element collection deltas, async/I/O nodes, cancellation, graph serialization, multi-threaded
publication, and any public package or API surface.

**Outside this product's identity:** an ambient cache, a custom task scheduler, a distributed
reactive system, automatic re-execution of I/O, a wholesale replacement for LINQ/collections, a
general DataFrame engine, or automatic purity/mutation inference.

## Planning Contract

### Requirements

- **R1:** Add a non-packable `tests/FunnySharp.Incremental` library with `Input<T>`,
  `IncrementalGraph`, generic value nodes, `Pure<TInput, TResult>`, `Collect`, observers, batching,
  equality cutoff, statistics, and explicit failure guards. It must not be referenced by `src/`.
- **R2:** Implement static dependency creation only. `Map`, `Combine`, and `Collect` require existing
  source nodes; no dynamic rebinding API. Null sources, null callbacks, cross-graph sources, and
  graph mutation during a commit are rejected with documented exceptions.
- **R3:** Implement staged `Batch(Action)` publication: input values are invisible until the
  outermost successful action returns; a throwing action discards staged values and observers do
  not run; nested batches join; observers run after all affected values are recomputed and in a
  deterministic order.
- **R4:** Implement input equality and output cutoff. `Set` applies the input comparer;
  `SetChanged` bypasses it. A computed node's retained value is published only when its output
  comparer finds a difference; dependents are not scheduled after cutoff.
- **R5:** Implement observer registration/disposal and reuse isolation: immediate and changes-only
  forms, idempotent disposal, no callback after disposal, independent observers, independent
  graphs, and rejection of registration/mutation during a commit.
- **R6:** Add the shared pricing workload with 512 products, 1,024 orders, 8 lines per order,
  round-robin product references, decimal pricing, region discounts, `Result<Quote, PricingError>`
  domain failures, a `Validation` usability case, and a work ledger.
- **R7:** Implement four complete consumers against the same workload: full recomputation,
  hand-maintained incremental engine (reverse index, dirty-set batching, cutoff), per-order graph,
  and collection-as-one-node graph. The hand baseline must handle the same scenarios without
  re-enumerating unaffected orders. The per-order graph must explicitly reject topology changes.
- **R8:** Add a `Microsoft.Extensions.Caching.Memory` consumer in the measurement project only. It
  caches product economics and recomputes all quotes; it must not be packaged or referenced by the
  prototype or tests.
- **R9:** Add a shared verifier used by both the xUnit suite and the measurement harness. It checks
  initial parity, product-cost updates, quantity updates, mixed batches, domain failures, no-op
  updates, membership handling, work counts, and graph statistics.
- **R10:** Add deterministic xUnit v3 tests for every promise above: publication, abort, nested
  batches, observers, disposal, cutoff, mutable storage, Result/Validation values, cross-graph
  isolation, work counts, per-order topology rejection, collection-node whole recomputation, null
  guards, and callback exception propagation.
- **R11:** Add a measurement executable that prints work-avoidance, setup, retained-memory,
  allocation/time, and latency-distribution tables, verifies the shared comparison before measuring,
  and can be run twice in isolation.
- **R12:** Write the evidence record with the decision, workload rationale, model, fairness
  assessment, tables from both runs, workload guidance, non-adoptions, residual risks, and an
  explicit statement that no public capability changed. Move the goal unchanged to archive and
  update the goals index/navigation.
- **R13:** Run the required gates and record real exit codes: build, test, format, docs snippets,
  API baseline, examples, and the measurement harness twice.

### Non-goals

- No shipping incremental API, package, analyzer, grammar, product-contract, or API-baseline change.
- No dynamic dependency rebinding (`Bind`), runtime cycle detection, or mutable graph topology.
- No per-element collection delta engine, change-set protocol, or automatic mutation detection.
- No ambient cache, cross-graph cache, custom task scheduler, threads, async nodes, cancellation,
  distributed reactivity, persistence, or automatic I/O re-execution.
- No purity inference from arbitrary delegates; `Pure` is the visible caller assertion.
- No `System.Reactive`, `DynamicData`, `QuikGraph`, or `ComputeSharp` dependency; those are assessed
  for fairness and excluded from the prototype.
- No changes to Goals 0028/0034 behavior; their changesets and summaries are optional inputs only.

## Implementation Units

### U1. Graph prototype skeleton and core semantics

- **Goal:** A buildable, non-shipping graph library implementing R1-R5.
- **Requirements:** R1, R2, R3, R4, R5.
- **Dependencies:** none.
- **Files:** `tests/FunnySharp.Incremental/`.
- **Approach:** `IncrementalNode`/`IncrementalNode<T>` base, `Input<T>`, `Map`, `Combine`,
  `Collect`, `Pure<TInput,TResult>`, a reusable priority-queue scheduler keyed by static node
  height, staged pending inputs, deterministic notification order, and a statistics snapshot.
  Constructors are internal; graph factories register and initialize nodes after construction.
- **Execution note:** initialize computed nodes eagerly at construction so setup includes initial
  evaluation and so no lazy dependency recursion is needed. Reset statistics after setup.
- **Patterns to follow:** ordinary C# records, `ArgumentNullException`/`ArgumentException` guards,
  single-threaded explicit state, no static mutable state, no runtime package dependency.
- **Test scenarios:** initial values; Map/Combine/Collect; null/cross-graph guards; output cutoff;
  input equality; `SetChanged`; staged and nested batches; abort; observer order/disposal;
  mutation during commit; callback exception propagation.
- **Verification:** prototype build and semantics tests.

### U2. Shared workload and four consumers

- **Goal:** Complete pricing workload, full/hand/graph/collection consumers, work ledger, and
  shared verifier.
- **Requirements:** R6, R7, R9.
- **Dependencies:** U1.
- **Files:** `tests/Shared/IncrementalComparison/`.
- **Approach:** `PricingModels`, `PricingKernel`, `PricingFixtures`, `PricingWork`,
  `FullRecomputePricingConsumer`, `HandIncrementalPricingConsumer`,
  `IncrementalPricingConsumer`, `CollectionNodePricingConsumer`, `IncrementalPricingComparison`.
  Use a common `IPricingConsumer` with `SetProductCost`, `SetOrderQuantity`, `Batch`, `Evaluate`,
  `AddOrder`, `RemoveLastOrder`, `ResetObservability`, `Work`, and optional graph statistics.
- **Execution note:** keep fixtures immutable to each consumer by cloning in constructors; do not
  share mutable product/order arrays between consumers. The hand baseline uses a reverse index and
  dirty stamps; the per-order graph uses a `Collect` node per order; the collection graph uses one
  quote-map node.
- **Patterns to follow:** existing shared comparison consumers; internal types; no test framework
  dependency in shared code.
- **Test scenarios:** parity for initial, product, quantity, batch, failure, no-op, membership;
  exact work counts; per-order topology rejection; collection graph full work.
- **Verification:** shared verifier plus xUnit comparison tests.

### U3. Semantics and comparison tests

- **Goal:** Executable xUnit v3 coverage of every prototype promise.
- **Requirements:** R10.
- **Dependencies:** U1, U2.
- **Files:** `tests/FunnySharp.Incremental.Tests/`.
- **Approach:** Split tests by concern: graph/batch/cutoff/observer/workload/reuse. Call the shared
  verifier once and use focused tests for promises and boundary failures.
- **Execution note:** no skipped tests; classic asserts only; one public sealed class per file.
- **Test scenarios:** R10 list; `Result` failure and `Validation` value; `Set` same-reference no-op
  versus `SetChanged`; two independent graphs; observer disposal during notification; add-order
  exception; collection graph recomputes all quotes.
- **Verification:** `dotnet test` on the new project and the solution gate.

### U4. Measurement harness

- **Goal:** Honest work, setup, retention, allocation/time, and latency measurements.
- **Requirements:** R8, R11.
- **Dependencies:** U2.
- **Files:** `tests/FunnySharp.IncrementalMeasurements/`, with
  `CacheAssistedPricingConsumer.cs` local to the measurement project.
- **Approach:** Verify parity first; print environment; measure work ledgers and graph statistics;
  measure construction plus initial evaluation; measure retained bytes after setup and after
  repeated updates; use the shared allocation harness for complete update+retrieve operations; use
  a timestamp recorder for p50/p95/p99/max; report two complete process runs.
- **Execution note:** do not run concurrently with the test suite. Use one heavy process at a time.
  Charge the same digest to every consumer; include the work ledger increments in all timings.
- **Test scenarios:** product update, order quantity update, mixed batch, no-op, add/remove order
  where supported, collection graph full recomputation, cache product hit/miss.
- **Verification:** two isolated harness runs with verified parity.

### U5. Evidence, decision, and close

- **Goal:** Maintainer-ready evidence and goal closure.
- **Requirements:** R12, R13.
- **Dependencies:** U4.
- **Files:** `docs/incremental-domain-computation-evidence.md`, `docs/goals/README.md`,
  `docs/goals/archive/0040-goal.md`, `docs/goals/AGENTS.md`, `docs/AGENTS.md`,
  `docs/plans/AGENTS.md`.
- **Approach:** Record the model, fairness assessment, both measurement runs, workload guidance,
  caching-exclusion reconsideration, non-adoptions, residual risks, and the ADOPT/ADAPT/DEFER/REJECT
  judgment. Move the goal unchanged. State explicitly that no public capability changed for a
  negative or research-only decision; reconcile product contract/grammar/stability only if a public
  shape was actually adopted.
- **Execution note:** preserve the goal file byte-for-byte; verify with `cmp` before and after the
  move.
- **Test scenarios:** none beyond gates.
- **Verification:** final gate transcript and working-tree review.

## Verification Contract

| Gate | Command | Applies to | Completion evidence |
| --- | --- | --- | --- |
| Build | `dotnet fsi build.fsx -- -p build` | all projects | 0 warnings, 0 errors |
| Test | `dotnet fsi build.fsx -- -p test` | all suites including new tests | 0 failed, 0 skipped |
| Format | `dotnet fsi build.fsx -- -p format` | C# | clean |
| Docs snippets | `dotnet fsi build.fsx -- -p verify-docs-snippets` | primary guides | unchanged count unless a guide is added |
| API baseline | `dotnet fsi build.fsx -- -p verify-api-baseline` | shipping assemblies | 0 delta; no public capability changed |
| Examples | `dotnet run --project examples/FunnySharp.Examples/FunnySharp.Examples.csproj -c Release` | examples | passed |
| Measurement | `dotnet run --project tests/FunnySharp.IncrementalMeasurements/FunnySharp.IncrementalMeasurements.csproj -c Release` | comparison | two complete isolated runs, parity verified |

## Definition of Done

- **U1-U3:** graph prototype, shared workload, consumers, verifier, and tests build and run; the
  static, batch, cutoff, error, observer, and isolation promises hold; unsupported topology changes
  are explicit.
- **U4:** the harness reports actual avoided work, graph maintenance, setup, retention, allocation,
  and latency for every compared implementation, twice.
- **U5:** the evidence states the decision with reasoning and non-adoptions, the goal is archived
  unchanged, navigation is updated, and no public capability is claimed if the decision is not an
  adopt/adapt with a public shape.
- **Global:** every required gate has a real recorded result; the working tree is left uncommitted;
  no Goal 0041 work starts.