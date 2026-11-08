# Incremental Domain Computation Evidence

This record supports the maintainer's judgment for
[Goal 0040](goals/archive/0040-goal.md): whether explicit incremental computation is a useful and
competitive optional FunnySharp capability for expensive, shared domain calculations that change
locally over time. It records the executable workload, the static graph prototype, the comparison
with full recomputation, a competent hand-maintained incremental engine, a collection-as-one-node
graph, and a `Microsoft.Extensions.Caching.Memory` product cache. It reports actual avoided work,
graph maintenance, setup cost, retained memory, update allocation, and latency distribution.

**Decision: ADAPT (static node-level subset), not shipped in this goal.** The prototype removes
98.4% of quote and line work on a shared pricing update and is about 19x faster than full recomputation,
while remaining within about 20% of the competent hand-maintained engine and using about 60% fewer
consumer-specific incremental lines. It is not a blanket incremental capability: dynamic
dependencies, collection-level deltas, topology changes, async/I/O, and ambient caching are not
supported. The hand-maintained engine remains faster, smaller, and lower-allocation for this
specific pricing workload, so plain functions and the hand engine remain preferable where the
dependency bookkeeping is simple. No public capability, package, grammar, product-contract,
stability-inventory, or API-baseline change occurred in this research outcome.

Source identity is this Goal 0040 working tree on top of commit `e635d29`; the landing commit
becomes the durable identity. Measurements were taken on this host:

- Ubuntu 24.04.5 LTS, Linux 7.0.0-1017-azure x86_64, 4 logical cores.
- .NET SDK 10.0.401, runtime 10.0.12 x64; Release build; `TieredCompilation=false`.
- Dates: 2026-11-08 UTC. The two final runs started at 15:09:09 and 15:09:24.

Allocation is the blocking comparison; timing is directional on this shared host. Setup and
retained-memory figures are reported separately from per-update rows.

## Chosen Workload And Why It Is Representative

The workload is a long-lived pricing service over a fixed catalog and order book:

- 512 products, each with a unit cost, tax rate, and category.
- 1,024 orders, each with 8 lines. Product references are distributed round-robin, so every product
  is shared by 16 distinct orders; the model contains 8,192 line references.
- A per-product `ProductEconomics` node (rounded unit price plus tax/category) is shared by the
  orders that reference the product.
- A per-order `Result<Quote, PricingError>` node combines the order input with the economics of its
  eight products. The quote carries subtotal, region-dependent discount, tax, and total.
- The complete consumer result is the post-update quote list for all 1,024 orders.

The workload has genuine fan-out and fan-in rather than a trivial arithmetic chain. A product-cost
update invalidates one product and the 16 orders that use it; a quantity update invalidates one
order. The full model remains large enough that avoiding 1,008 of 1,024 quote evaluations is
observable: full recomputation is about 456-458 &micro;s and 184 KB per complete update.

The topology is deliberately fixed after construction. Product membership of an order and order
membership of the quote collection are not graph edges; adding or removing an order is a
collection-level change. This keeps node-level recomputation and collection-level delta processing
separate.

## Prototype Model

The prototype is the non-packable research project
[FunnySharp.Incremental](../tests/FunnySharp.Incremental/). It is not referenced by `src/` and is
not part of the shipping surface. Its shape is:

```csharp
var graph = new IncrementalGraph();
var product = graph.Input("product", initialProduct);
var economics = graph.Map(
    "economics",
    product,
    Pure<Product, ProductEconomics>.Of(p => PricingKernel.ComputeProduct(p, work)));
var orderProducts = graph.Collect("order-products", productNodesInLineOrder);
var quote = graph.Combine(
    "quote",
    order,
    orderProducts,
    Pure<(Order Order, IReadOnlyList<ProductEconomics> Products), Result<Quote, PricingError>>.Of(...));
using var observer = graph.ObserveChanges(quote, reportChangedQuote);
graph.Batch(() =>
{
    product.Set(nextProduct);
    order.SetChanged(nextOrder);
});
```

### Consistency boundary and input ownership

- `graph.Batch(Action)` stages input publications. No input value, computed node, or observer is
  published until the outermost action returns successfully. Observers then run in deterministic
  node-creation order and registration order and see only final values.
- A throwing batch action discards every staged input value and runs no observer. This is
  publication rollback of staged inputs only, not a domain transaction or external-effect rollback.
- A single `Set` outside a batch uses the same staging path and publishes immediately. Nested
  batches join the outer boundary.
- Inputs retain caller values by reference and never clone them. `Set(value)` applies the input's
  comparer; `SetChanged(value)` publishes unconditionally for caller-mutated storage such as a
  `List<T>` mutated in place. The mutable-list test demonstrates that plain `Set` after an in-place
  mutation is a deliberate cutoff, not an implicit mutation detector.
- Observers and node creation are rejected during a commit. Observer exceptions propagate; values
  are already consistent, but later observers in that commit do not run and no rollback is claimed.
- The graph is single-threaded. There is no ambient cache, task scheduler, background work,
  cross-graph cache, persistence, or automatic I/O re-execution.

### Node level versus collection level

- **Node level:** one product node, one `Collect` fan-in node per order, and one quote node per
  order. A product-cost update evaluates one product node, the 16 dependent collect nodes, and the
  16 dependent quote nodes; the other 4,063 nodes are skipped.
- **Collection level:** the collection-as-one-node graph keeps a product-collection node and one
  order-collection node; a single quote-map node recomputes all 1,024 quotes. It supports add/remove
  only by republishing the whole order list. A list inside a node is not treated as an incremental
  collection operation.
- The per-order graph has fixed topology. `AddOrder` and `RemoveLastOrder` throw
  `NotSupportedException` with an explicit message. The hand baseline and the collection graph
  support append/remove-last, and their measured costs are reported.

### Static dependencies, cycles, and unsupported change shapes

- All dependencies are static. `Map`, `Combine`, and `Collect` receive already-created nodes; there
  is no `Bind`, rebinding API, or mutable edge list. A node can only depend on earlier-created
  nodes, so a cycle cannot be represented. This is a structural rejection, not runtime cycle
  detection.
- Dynamic dependency topology, order-line product membership changes, async/I/O nodes,
  cancellation, and multi-threaded publication are unsupported and not claimed. A workload that
  needs them must use another mechanism.
- A same-reference `Set` after in-place mutation is a cutoff; `SetChanged` is the explicit change
  contract. An unsupported topology change is a thrown `NotSupportedException`, never a silent
  rebuild that appears incremental.

### Purity visibility

- Every user computation must be wrapped in `Pure<TInput, TResult>` via
  `Pure<TInput, TResult>.Of(...)`; there is no implicit conversion from `Func`. The call site
  states that the callback is deterministic and side-effect free.
- The graph may skip invocation after cutoff, reuse retained values across commits, and invoke in
  dependency order. It does not detect impurity or guarantee callback counts for impure callbacks.
- Observers are explicitly side-effecting `Action<T>` callbacks and are documented separately. The
  library-supplied `Collect` node makes no user purity assumption.

### Equality and cutoff

- `Input.Set` uses the configured input comparer. The default is `EqualityComparer<T>.Default`.
- Every computed node has an output comparer. A recomputed output equal to the retained output
  stops dependent scheduling. Tests pin both input no-op behavior and output cutoff, including a
  custom ordinal-ignore-case comparer and a batch that round-trips to the published value.
- Input and output comparers are explicit constructor options where the default is not the caller's
  policy. The prototype does not infer equality from a collection interface or deep-copy values.

### Error propagation

- Domain failure is a value: the workload's quote node is
  `Result<Quote, PricingError>`. A non-positive quantity produces `Failure`, observers receive it,
  and `Result` equality participates in cutoff. A dedicated test also uses an accumulating
  `Validation<int, string>` as a node value.
- A callback exception is not converted to a domain failure. It propagates out of the publishing
  call, observers are not run, and the graph makes no rollback or recovery promise. Expected
  failures should be `Result`/`Validation` values.
- A missing product index or invalid quantity in the workload is a `PricingError`, not an exception.

### Observers, disposal, and reuse isolation

- `Observe` invokes immediately; `ObserveChanges` does not. Both return `IDisposable`.
- Disposal is idempotent, stops future notifications, removes the registration, and does not disturb
  other observers. Disposal during notification skips a not-yet-invoked registration.
- Two graphs built from the same static `Pure` definitions keep independent nodes, values, and
  observers. Repeated updates do not grow node, edge, or observer counts.

## Compared Implementations And Fairness

| Implementation | Shape | Fairness treatment |
| --- | --- | --- |
| Full recomputation | Recomputes all product economics and all quotes from the model on every complete operation. | Direct, competent C#; charged the same pricing kernel, work ledger, quote digest, and complete retrieval. |
| Hand-maintained incremental | Caches economics and quotes, maintains a product-to-order reverse index, batches changes, deduplicates dirty orders, and applies value equality. | The competent domain-specific baseline; 259 lines including batching, reverse indexing, and membership support. No re-enumeration of unaffected orders. |
| Per-order incremental graph | Product nodes, per-order `Collect` fan-in, per-order quote nodes. | Pays all node, edge, collector, scheduler, and retained-value costs. Consumer-specific dependency code is 101 lines. |
| Collection-as-one-node graph | Product collection node and a single quote-map node over the whole order list. | The explicit demonstration that a list in a node is not incremental; supports membership by whole-list replacement. |
| `Microsoft.Extensions.Caching.Memory` | Caches product economics with precomputed keys and recomputes all quotes from cached values. | The closest general .NET memoization facility. It avoids product work, not dependent quote work; cache keys are precomputed so key construction is not charged to the cache. |

Shared models, fixtures, work ledger, verifier, and digest are not charged to any consumer. All
consumers use the same `PricingKernel` product and quote calculations and the same fixtures. The
hand engine is charged its own reverse-index construction, dirty-set batching, and membership
handling. The graph is charged its initial full evaluation, node and edge allocation, and retained
values. Full recomputation is charged its complete result materialization.

Assessed and not measured as fair competitors:

- `System.Reactive` supplies notification composition, not an automatic dependency graph,
  glitch-free batch publication, or output cutoff. A fair version would be a hand-wired
  per-order subscription graph with manual disposal and `CombineLatest`; that adds subscription
  overhead to the hand baseline rather than testing a different incremental algorithm.
- `DynamicData` is a collection change-set library, appropriate for collection-level deltas rather
  than node-level shared value dependencies.
- `QuikGraph` and `ComputeSharp` address static graph algorithms and GPU execution, not incremental
  value maintenance across local updates.

## Work Actually Avoided

Work is deterministic across both final runs. The table reports one complete update plus retrieval
of all 1,024 quotes. Product/quote/line counts are domain-kernel invocations; node/skipped/changed
counts are graph statistics.

| Scenario | Consumer | Product evals | Quote evals | Line evals | Node evals | Skipped nodes | Changed nodes | Input changes |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Product cost update | Full recomputation | 512 | 1,024 | 8,192 | - | - | - | - |
| Product cost update | Hand-maintained incremental | 1 | 16 | 128 | - | - | - | - |
| Product cost update | Per-order incremental graph | 1 | 16 | 128 | 33 | 4,063 | 34 | 1 |
| Product cost update | Collection-as-one-node graph | 1 | 1,024 | 8,192 | 3 | 1,024 | 4 | 1 |
| Product cost update | `MemoryCache` product cache | 1 | 1,024 | 8,192 | - | - | - | - |
| Order quantity update | Full recomputation | 512 | 1,024 | 8,192 | - | - | - | - |
| Order quantity update | Hand-maintained incremental | 0 | 1 | 8 | - | - | - | - |
| Order quantity update | Per-order incremental graph | 0 | 1 | 8 | 1 | 4,095 | 2 | 1 |
| Order quantity update | Collection-as-one-node graph | 0 | 1,024 | 8,192 | 1 | 1,026 | 2 | 1 |
| Order quantity update | `MemoryCache` product cache | 0 | 1,024 | 8,192 | - | - | - | - |
| Mixed batch | Full recomputation | 512 | 1,024 | 8,192 | - | - | - | - |
| Mixed batch | Hand-maintained incremental | 2 | 17 | 136 | - | - | - | - |
| Mixed batch | Per-order incremental graph | 2 | 17 | 136 | 35 | 4,061 | 38 | 3 |
| Mixed batch | Collection-as-one-node graph | 2 | 1,024 | 8,192 | 4 | 1,023 | 7 | 3 |
| Mixed batch | `MemoryCache` product cache | 2 | 1,024 | 8,192 | - | - | - | - |
| No-op product cost | Hand-maintained incremental | 0 | 0 | 0 | - | - | - | - |
| No-op product cost | Per-order incremental graph | 0 | 0 | 0 | 0 | 4,096 | 0 | 0 |
| Add one order | Full recomputation | 512 | 1,025 | 8,200 | - | - | - | - |
| Add one order | Hand-maintained incremental | 0 | 1 | 8 | - | - | - | - |
| Add one order | Collection-as-one-node graph | 0 | 1,025 | 8,200 | 1 | 1,026 | 2 | 1 |
| Add one order | `MemoryCache` product cache | 0 | 1,025 | 8,200 | - | - | - | - |
| Add one order | Per-order incremental graph | - | - | - | - | - | - | - (fixed topology) |
| Remove last order | Full recomputation | 512 | 1,023 | 8,184 | - | - | - | - |
| Remove last order | Hand-maintained incremental | 0 | 0 | 0 | - | - | - | - |
| Remove last order | Collection-as-one-node graph | 0 | 1,023 | 8,184 | 1 | 1,026 | 2 | 1 |
| Remove last order | `MemoryCache` product cache | 0 | 1,023 | 8,184 | - | - | - | - |
| Remove last order | Per-order incremental graph | - | - | - | - | - | - | - (fixed topology) |

The per-order graph avoids 1,008 of 1,024 quote evaluations (98.4%) and 8,064 of 8,192 line
evaluations (98.4%) for a product-cost update. It evaluates 33 of 4,096 nodes (0.8%). A quantity
update avoids 1,023 of 1,024 quote evaluations. The mixed batch recomputes the union of affected
orders once: 17 quotes, not 33, demonstrating batch deduplication.

The collection-as-one-node graph evaluates only 3 nodes for a product update but still performs all
1,024 quote evaluations. The cache evaluates one product and all 1,024 quotes. Low node counts do
not imply avoided domain work.

## Graph Maintenance, Setup, And Retention

Graph shape:

| Graph consumer | Nodes | Edges |
| --- | ---: | ---: |
| Per-order incremental graph | 4,096 | 10,752 |
| Collection-as-one-node graph | 1,027 | 1,026 |

Setup is construction plus the initial complete evaluation, best of three measured runs. Both final
runs are shown because setup timing varied on the shared host; allocation was stable.

| Consumer | Setup ms run 1 | Setup ms run 2 | Allocated B/setup | Product/quote/line evals |
| --- | ---: | ---: | ---: | ---: |
| Full recomputation | 0.774 | 0.487 | 336,136 | 512 / 1,024 / 8,192 |
| Hand-maintained incremental | 0.987 | 0.605 | 488,688 | 512 / 1,024 / 8,192 |
| Per-order incremental graph | 1.576 | 1.603 | 1,841,728 | 512 / 1,024 / 8,192 |
| Collection-as-one-node graph | 0.787 | 0.755 | 656,160 | 512 / 1,024 / 8,192 |
| `MemoryCache` product cache | 1.134 | 0.701 | 485,040 | 512 / 1,024 / 8,192 |

Retained memory is forced-GC retained bytes after setup and after 200 product-cost updates. Both
runs are shown; small deltas are GC/capacity noise.

| Consumer | Retained B run 1 | After 200 updates run 1 | Retained B run 2 | After 200 updates run 2 |
| --- | ---: | ---: | ---: | ---: |
| Full recomputation | 143,512 | 143,576 | 143,512 | 143,576 |
| Hand-maintained incremental | 365,080 | 373,616 | 365,080 | 365,392 |
| Per-order incremental graph | 1,632,536 | 1,625,248 | 1,632,536 | 1,633,472 |
| Collection-as-one-node graph | 602,664 | 602,928 | 602,664 | 602,928 |
| `MemoryCache` product cache | 301,616 | 293,456 | 293,392 | 293,456 |

The per-order graph retains about 1.63 MB for 4,096 nodes and 10,752 edges (roughly 400 B per
node). It retains 4.5x the hand engine and 11.4x the full model's inputs, but the retained size is
stable across 200 updates. The collection graph retains about 0.60 MB; the cache about 0.29 MB.
The hand engine's +8.5 KB in run 1 is capacity churn, not sustained growth (run 2 is +0.3 KB).

## Update Allocation And Directional Timing

Complete update plus retrieval of all quotes. Allocation is from
`GC.GetAllocatedBytesForCurrentThread`; time is the best of three internal rounds. These are two
complete process runs.

Run 1:

| Scenario | Consumer | Allocated B/op | Time ns/op |
| --- | --- | ---: | ---: |
| Product cost update | Full recomputation | 184,432.4 | 458,327.0 |
| Product cost update | Hand-maintained incremental | 2,160.0 | 19,926.1 |
| Product cost update | Per-order incremental graph | 3,328.0 | 23,948.4 |
| Product cost update | Collection-as-one-node graph | 151,752.2 | 473,272.0 |
| Product cost update | `MemoryCache` product cache | 151,984.2 | 486,065.5 |
| Order quantity update | Full recomputation | 184,496.4 | 459,310.0 |
| Order quantity update | Hand-maintained incremental | 352.0 | 13,642.6 |
| Order quantity update | Per-order incremental graph | 216.0 | 13,842.2 |
| Order quantity update | Collection-as-one-node graph | 286,896.2 | 515,664.5 |
| Order quantity update | `MemoryCache` product cache | 151,832.2 | 485,417.5 |
| Mixed batch | Full recomputation | 184,720.4 | 460,656.0 |
| Mixed batch | Hand-maintained incremental | 2,848.0 | 20,745.8 |
| Mixed batch | Per-order incremental graph | 3,768.0 | 25,058.4 |
| Mixed batch | Collection-as-one-node graph | 291,392.2 | 523,724.0 |
| Mixed batch | `MemoryCache` product cache | 152,488.2 | 506,442.0 |
| No-op product cost | Hand-maintained incremental | 112.0 | 13,015.9 |
| No-op product cost | Per-order incremental graph | 64.0 | 13,100.7 |

Run 2:

| Scenario | Consumer | Allocated B/op | Time ns/op |
| --- | --- | ---: | ---: |
| Product cost update | Full recomputation | 184,432.4 | 456,476.0 |
| Product cost update | Hand-maintained incremental | 2,160.0 | 22,375.1 |
| Product cost update | Per-order incremental graph | 3,328.0 | 24,311.4 |
| Product cost update | Collection-as-one-node graph | 151,752.2 | 481,273.5 |
| Product cost update | `MemoryCache` product cache | 151,984.2 | 487,813.0 |
| Order quantity update | Full recomputation | 184,496.4 | 458,482.0 |
| Order quantity update | Hand-maintained incremental | 352.0 | 13,661.2 |
| Order quantity update | Per-order incremental graph | 216.0 | 13,872.3 |
| Order quantity update | Collection-as-one-node graph | 286,896.2 | 519,466.0 |
| Order quantity update | `MemoryCache` product cache | 151,832.2 | 482,920.0 |
| Mixed batch | Full recomputation | 184,720.4 | 461,523.0 |
| Mixed batch | Hand-maintained incremental | 2,848.0 | 20,810.0 |
| Mixed batch | Per-order incremental graph | 3,768.0 | 27,454.8 |
| Mixed batch | Collection-as-one-node graph | 291,392.2 | 535,436.0 |
| Mixed batch | `MemoryCache` product cache | 152,488.2 | 554,878.5 |
| No-op product cost | Hand-maintained incremental | 112.0 | 13,794.4 |
| No-op product cost | Per-order incremental graph | 64.0 | 13,609.0 |

Honest direction:

- **Product cost update:** the graph is 18.8-19.1x faster than full recomputation and allocates
  55x less per operation. It is 1.09-1.20x slower than the hand engine and allocates 1.54x more.
- **Order quantity update:** the graph and hand engine are effectively tied (within about 1.5%);
  both avoid all product work and 1,023 of 1,024 quote evaluations. The graph allocates 216 B/op
  versus 352 B/op for the hand engine; both are far below full recomputation.
- **Mixed batch:** the graph is 1.21-1.32x slower than the hand engine and allocates 3,768 B/op
  versus 2,848 B/op. The hand batch rows are stable at about 20.8 &micro;s; the graph rows are
  25.1-27.5 &micro;s.
- **No-op product cost:** both hand and graph report zero product, quote, line, and node
  evaluations. The remaining 13.0-13.8 &micro;s is the complete retrieval/digest of all 1,024
  quotes, not incremental work. The 64 B/op graph and 112 B/op hand allocations are the consumer
  update wrapper (a replacement record and, for the hand engine, a batch closure), not graph
  recomputation.
- **Collection graph:** 1,024 quote and 8,192 line evaluations on every update; it is slightly
  slower than full recomputation because it pays graph and whole-list replacement costs on top of
  the same complete quote work. The mixed batch allocates 291 KB/op because it clones the whole
  order list.
- **Cache:** the product cache avoids 511 of 512 product evaluations but still performs all 1,024
  quote and 8,192 line evaluations. It is slower than full recomputation on this workload because
  cache lookup and materialization cost more than the product work it saves. Caching alone is not
  incrementality.
- **Timing caveat:** the host is shared. Allocation is stable across runs; timings are directional.
  Setup timing varied more than allocation and is shown per run.

Break-even against full recomputation, using setup plus complete update times:

- Run 1: `(1.576 ms - 0.774 ms) / (458.327 us - 23.948 us) = 1.85` updates.
- Run 2: `(1.603 ms - 0.487 ms) / (456.476 us - 24.311 us) = 2.58` updates.

The graph pays back its setup after roughly two to three updates on this workload. It never catches
the hand engine on per-update time, allocation, or retained memory; its advantage there is the
101-line consumer dependency declaration versus the 259-line domain-specific hand engine.

## Update Latency Distribution

Microseconds, timestamp recorder, warmup discarded. Two complete runs.

Run 1:

| Scenario | Consumer | p50 | p95 | p99 | max | Samples |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Product cost update | Full recomputation | 466.08 | 720.68 | 736.67 | 1,418.71 | 200 |
| Product cost update | Hand-maintained incremental | 19.40 | 29.79 | 42.21 | 53.34 | 1,000 |
| Product cost update | Per-order incremental graph | 23.53 | 44.06 | 47.69 | 69.50 | 1,000 |
| Product cost update | Collection-as-one-node graph | 470.90 | 621.29 | 787.16 | 1,179.64 | 1,000 |
| Product cost update | `MemoryCache` product cache | 490.30 | 1,277.05 | 1,930.69 | 2,770.99 | 1,000 |

Run 2:

| Scenario | Consumer | p50 | p95 | p99 | max | Samples |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Product cost update | Full recomputation | 460.20 | 492.28 | 559.15 | 1,421.16 | 200 |
| Product cost update | Hand-maintained incremental | 19.40 | 32.19 | 42.19 | 52.24 | 1,000 |
| Product cost update | Per-order incremental graph | 23.59 | 43.67 | 52.96 | 72.31 | 1,000 |
| Product cost update | Collection-as-one-node graph | 474.22 | 786.02 | 810.10 | 1,791.27 | 1,000 |
| Product cost update | `MemoryCache` product cache | 477.18 | 541.87 | 765.25 | 1,133.94 | 1,000 |

The graph's p50 is within about 4.1-4.2 &micro;s of the hand engine and about 21% above it. Its p95
is 43.7-44.1 &micro;s versus 29.8-32.2 &micro;s for the hand engine, and p99 is 47.7-53.0 &micro;s
versus 42.2 &micro;s. The distribution is not bimodal; no occasional full-graph replay appeared in
the recorded samples. The `MemoryCache` p95/p99 in run 1 is a host outlier (its p50 stays close to
the other full-work consumers).

## Consumer Expression And Maintenance

| Artifact | Lines | Notes |
| --- | ---: | --- |
| Per-order graph consumer | 101 | Declares product nodes, per-order collect nodes, quote nodes; no reverse index, dirty set, or invalidation bookkeeping |
| Hand-maintained incremental consumer | 259 | Reverse index, dirty stamps, batching, equality checks, membership handling |
| Collection-as-one-node consumer | 125 | Whole-list replacement and quote map |
| Full recomputation consumer | 77 | Direct full evaluation |
| Graph prototype | 771 | Nodes, graph, batch, equality, observers, statistics; research-only. `IncrementalGraph.cs` alone is 441 lines. |
| Tests | 725 | Semantics, batching, cutoff, observers, workload comparison |

The graph consumer code is about 61% smaller than the competent hand engine, but the library core
is additional code that a public capability would have to own, document, test, and maintain. The
hand engine is specific to one domain and would be duplicated or diverged in each consuming
application; the graph trades that duplication for about 20% per-update overhead, 4.5x retained
memory, and a 1.6-2.7x setup-time increase in these runs.

## Workload Guidance

**Where the static graph pays:**

- Long-lived fixed-topology DAGs with expensive shared nodes, high fan-in/fan-out, and many value
  updates. The measured product update avoids 98.4% of quote/line work and pays setup back after
  roughly two to three complete updates versus full recomputation.
- Consumers that need one consistent post-batch view, input/output cutoff, per-node observers,
  observer disposal, and reuse isolation without writing reverse-index and dirty-set code.
- No-op and localized updates: zero node/domain evaluation for equal inputs and one quote node for
  a quantity change.
- `Result`/`Validation` domain failures and mutable caller storage expressed through ordinary
  values plus `SetChanged`.

**Where ordinary functions or the hand engine remain preferable:**

- One-shot or short-lived calculations. Full recomputation has no node/edge setup, 336 KB setup
  allocation, and 143 KB retained; the graph's 1.84 MB setup and 1.63 MB retention are not
  amortized.
- Simple dependency bookkeeping, especially when a domain-specific reverse index is easy to write.
  The hand engine wins per-update time, allocation, and retained memory on this workload.
- Collection membership changes and collection aggregates. The per-order graph cannot add or remove
  orders without rebuilding; the collection graph recomputes all quotes and is slower than full.
  A list in a node is not a collection delta.
- Pure memoization problems. The `MemoryCache` product cache did not avoid dependent quote work and
  was slower than full recomputation; a cache without dependency awareness is not incremental.
- Cheap nodes or update-heavy traces where node/scheduler overhead dominates the domain work.
- Dynamic dependency topology. The prototype has no `Bind`; a consumer whose dependencies change
  with data is outside the supported subset.

## Reconsideration Of Earlier Caching Exclusions

The existing capability guides exclude caches and memoization from one-shot operations: `Summary`
has no materialization/cache, `AggregateResult` does not cache, checks do not cache, strict joins do
not memoize, `BatchScope` owns only a bounded scope-local cache and no ambient cache, and effects do
not cache. Those exclusions were about hiding repeated work inside an operation that promises one
enumeration or one execution. They remain valid and unchanged.

This research tested the strongest adjacent idea: an explicitly constructed, caller-owned graph
that retains values across calls. It is not an ambient cache. It is created per consumer, has no
static storage, owns its own nodes and observers, and becomes collectible with the consumer. The
measurements show both why a generic cache exclusion was wise and where an explicit graph differs:
the `MemoryCache` product cache saved product work but did not localize dependent quote work, while
the graph did; the collection node did not. A future product goal that adopts the static subset must
add a separate opt-in family and state that it does not retrofit caching into `Summary`,
`AggregateResult`, checks, joins, effects, or `BatchScope`. No such product change was made here.

## Relationship To Goals 0028 And 0034

- **Changesets (0028):** a `Patch<Order>` could be used by a consumer to describe a localized order
  replacement before publishing a graph input, but it is not required. The graph's honest change
  contract is `Set`/`SetChanged`; the prototype therefore works with BCL records and mutable storage
  without taking a dependency on `Patch<T>`. `Patch` behavior is unchanged.
- **Reusable summaries (0034):** a `Summary` can be used as a node's compute over a collection, but
  the node still recomputes the complete summary when the collection changes. The collection-as-one-
  node measurement is the explicit evidence: 1,024 quote evaluations for one order update. No
  incremental summary protocol was added, and `Summary` remains a total fold.

Neither earlier capability needed a different representation for this investigation.

## Maintainer Judgment

**ADAPT (static node-level subset), not shipped.** A later product goal may adopt an explicit,
caller-owned static dependency graph with:

- value inputs and static `Map`/`Combine`/fan-in dependencies;
- the visible `Pure.Of` purity assertion;
- a staged `Batch(Action)` publication boundary;
- input equality and output cutoff, with `SetChanged` for caller mutation;
- per-node observers with deterministic notification and disposal;
- `Result`/`Validation` as ordinary node values;
- explicit rejection of unsupported topology changes.

The recommendation is ADAPT rather than ADOPT because no shipping API was designed, no consumer
integration was attempted, and the public shape must be justified by a real application with a
complex shared DAG. It is ADAPT rather than a blanket DEFER because the static subset removes 98.4%
of quote/line work, is about 19x faster than full recomputation, and is within about 20% of the
competent hand engine while cutting consumer-specific incremental code by about 60%. A blanket DEFER
is defensible for the measured simple pricing workload, where the hand engine wins every runtime,
allocation, and memory axis; the static subset earns a later product investigation, not a product
commitment now.

Non-adoptions:

- No public incremental API, package, analyzer, grammar, product-contract, stability-inventory, or
  API-baseline change. `src/` is untouched.
- No dynamic dependency rebinding (`Bind`), runtime cycle detection, or mutable graph topology.
- No collection-level delta processing, change-set protocol, automatic mutation detection, or
  incremental summary/fold.
- No ambient, static, cross-graph, or cross-request cache. The `MemoryCache` consumer is comparison
  code in the measurement project only.
- No task scheduler, threads, async nodes, cancellation, distributed reactivity, persistence, or
  automatic I/O re-execution.
- No purity inference from `Func`; `Pure.Of` is required.
- No rollback or transaction promise for compute or observer faults; only staged input publication
  is discarded when a batch action throws.
- No dependency on `System.Reactive`, `DynamicData`, `QuikGraph`, or `ComputeSharp`.
- No change to Goals 0028/0034 behavior or to any existing caching exclusion.

Conditions for a later product goal:

1. Identify a real consumer whose shared dependency graph is complex enough that hand-maintained
   reverse indexes, invalidation, and batch consistency are a material maintenance burden.
2. Freeze the static subset and its unsupported shapes; add diagnostics for topology changes and
   reject dynamic dependencies rather than silently rebuilding.
3. Decide the public `Pure`/observer/batch surface and whether a specialized heterogeneous fan-in
   node avoids the prototype's `Collect` node and its retained memory.
4. Measure package-level trimming, Native AOT, multi-OS behavior, and a much larger graph where the
   roughly 400 B/node retention is acceptable.
5. Revisit dynamic dependencies and collection deltas only with a separately scoped goal; neither is
   solved here.

## Residual Risks And Open Questions

- The measured workload is synthetic though representative; production pricing or risk graphs may
  be dominated by I/O, concurrency, or node calculations with very different granularity.
- The prototype is single-threaded and eager: every dependent of a changed input recomputes, even
  when no observer needs it. A demand-driven "needed" set was not implemented; it could reduce work
  further or add bookkeeping cost.
- The per-order graph uses a `Collect` fan-in node per order. It materializes an 8-element array and
  a read-only wrapper for every affected order. A specialized heterogeneous fan-in could lower
  allocation, setup, and retained memory; the recorded graph is an honest prototype, not an
  optimized upper bound on the approach's performance.
- Retained memory scales with node and edge count (about 400 B/node in this sample). Large graphs
  need a budget; 1.63 MB for 4,096 nodes is not a universal cost.
- Setup timings varied across runs (graph 1.58-1.60 ms in the two final runs; an earlier invocation
  on the same host was 2.99 ms). Allocations were stable. Timing remains directional and
  host-specific.
- The graph makes no retry, rollback, or recovery promise after a compute exception. Expected
  failures must be `Result`/`Validation` values.
- Mutation after publication is the caller's responsibility; `Set` applies equality cutoff and can
  hide an in-place change. `SetChanged` is the explicit contract, and the evidence/tests disclose
  it.
- Purity is an assertion, not a proof. Skipped callbacks and retained values are observable to
  impure code.
- The `MemoryCache` comparison caches product economics only. A quote cache would require the same
  reverse index and invalidation work as the hand engine plus cache lookup overhead; it is a
  cache-assisted hand variant, not a distinct algorithm, and was not measured.
- No trim, Native AOT, package-consumer, or multi-OS evidence is claimed because no shipping package
  was produced.

## Gate Evidence

Final gate results for this working tree:

- `dotnet fsi build.fsx -- -p build`: exit 0, 0 warnings, 0 errors.
- `dotnet fsi build.fsx -- -p test`: exit 0; 2,915 passed, 0 failed, 0 skipped across the solution
  (includes 34 new incremental tests). The test gate ran after the final code change and before the
  two final measurement runs.
- `dotnet fsi build.fsx -- -p format`: exit 0; only the F# harness projects are outside the
  C#-only formatter.
- `dotnet fsi build.fsx -- -p verify-docs-snippets`: exit 0; 81 snippets across 15 primary guides,
  unchanged.
- `dotnet fsi build.fsx -- -p verify-api-baseline`: exit 0; 115 types and 989 members across 2
  assemblies, 0 delta.
- `dotnet run --project examples/FunnySharp.Examples/FunnySharp.Examples.csproj -c Release`:
  exit 0; `FunnySharp examples passed.`
- Measurement harness, run 1 at 2026-11-08 15:09:09 UTC: exit 0; 22,565 parity checks; tables above.
- Measurement harness, run 2 at 2026-11-08 15:09:24 UTC: exit 0; 22,565 parity checks; tables above.

Run the harness with:

```shell
dotnet run --project tests/FunnySharp.IncrementalMeasurements/FunnySharp.IncrementalMeasurements.csproj -c Release
```

No commit, push, or history rewrite was performed. The working tree is left for review. No Goal 0041
work started.