# Batched Read Consumer Evidence

This record supports the maintainer's judgment for
[Goal 0029](goals/archive/0029-goal.md): whether typed deferred requirements reduce complete
consumer code while preserving result association, source errors, request isolation, and bounded
execution. It is comparison and measurement evidence, not a completion claim or a new gate.

Source identity is the Goal 0029 working tree; the landing commit becomes the durable identity.
Measurements were taken on this host:

- Ubuntu 24.04.5 LTS, Linux 7.0.0-1017-azure, 2 logical cores.
- .NET SDK 10.0.403, runtime 10.0.14, x64.
- Date: 2026-11-01 (UTC).

Allocation is deterministic enough to reproduce within a few percent; timing varies with host load
and is directional only.

## Compared Consumers

All three consumers enrich the same eight orders. Orders repeat customer keys; every order needs a
customer and preference, and a successful pair needs a dependent region read. The compared files:

| Consumer | File | Lines | Shape |
| --- | --- | ---: | --- |
| FunnySharp requirements | [FunnySharpOrderEnricher.cs](../tests/Shared/BatchComparison/FunnySharpOrderEnricher.cs) | 86 | `BatchRequirement.All` of per-order `Read`/`Zip`/`Bind`/`Map` plans, one `BatchScope`, typed `Result` outcomes. |
| Hand-batched BCL | [HandBatchedOrderEnricher.cs](../tests/Shared/BatchComparison/HandBatchedOrderEnricher.cs) | 190 | Distinct-key lists, direct source calls, manual response validation, dictionaries, dependent region pass, and per-order error assembly. |
| GreenDonut | [GreenDonutOrderEnricher.cs](../benchmarks/FunnySharp.DataLoaderComparison/GreenDonutOrderEnricher.cs) | 232 | `DataLoaderBase` subclasses, exception-based per-key results, explicit exception mapping, and manual independent/dependent task staging. |

The shared deterministic source and dataset are in
[DeterministicOrderSources.cs](../tests/Shared/BatchComparison/DeterministicOrderSources.cs). They
are test infrastructure used by all three consumers, not charged to any one implementation. The
FunnySharp plan consumer's request path is about half the hand-batched path and less than half the
GreenDonut path when each file's source adapters and outcome mapping are included. Library
implementation, XML documentation, and measurement code are one-time costs, not consumer lines.

## Equivalence

`BatchComparison.VerifyAsync` runs ten fixtures through the FunnySharp and hand-batched consumers
and compares:

- the complete ordered list of `Result<EnrichedOrder, OrderFailure>` outcomes;
- the exact ordered source invocation sequence, including each requested key list.

The fixtures are:

| Fixture | FunnySharp calls | Hand-batched calls | GreenDonut calls |
| --- | ---: | ---: | ---: |
| repeated keys, successful enrichment | 3 | 3 | 3-4 |
| missing customer | 3 | 3 | 3-4 |
| per-key customer failure | 3 | 3 | 3-4 |
| per-key preference failure | 3 | 3 | 3-4 |
| missing region | 3 | 3 | 3-4 |
| per-key region failure | 3 | 3 | not run |
| whole customer batch failure | 2 | 2 | 2-3 |
| whole preference batch failure | 2 | 2 | not run |
| duplicate customer response | 2 | 2 | not run |
| null customer response | 2 | 2 | not run |

The eight-order per-item path would make 24 calls (three stages per order). The plan and
hand-batched consumers make three calls in the successful fixture: one customer batch, one
preference batch, one dependent region batch. A failing stage that prevents dependent regions
removes the region call rather than issuing an empty one.

The deterministic source records every invocation and requested key list. For the successful
fixture the customer batch is `["c1","c2","c3"]` in first-request order; the preference batch is
the same; the region batch is `["r1","r2"]`. Duplicate customer keys are never sent twice.

The GreenDonut comparison runs six fixtures, including all three data-failure shapes and the
whole-batch failure, and compares the same ordered outcomes. Its per-key errors are .NET
exceptions (`KeyNotFoundException` for omission and a small `OrderLoaderException` for typed source
errors), so the comparison consumer maps them to the same `OrderFailure` values. GreenDonut's auto
scheduler may split a stage across more batches, so its source-call counts are recorded for context
rather than asserted as an exact equivalence; its loader cache and dispatch model are
request-scoped in this harness.

The comparison test is
[BatchConsumerComparisonTests.cs](../tests/FunnySharp.Tests/BatchConsumerComparisonTests.cs). The
GreenDonut app is
[benchmarks/FunnySharp.DataLoaderComparison/Program.cs](../benchmarks/FunnySharp.DataLoaderComparison/Program.cs);
it is outside `FunnySharp.slnx` and is not a release artifact.

## Measurements

Commands:

```bash
dotnet run --project tests/FunnySharp.BatchMeasurements/FunnySharp.BatchMeasurements.csproj -c Release
dotnet run --project benchmarks/FunnySharp.DataLoaderComparison/FunnySharp.DataLoaderComparison.csproj -c Release
```

Workload: 200 orders, 64 distinct customers, 64 distinct preferences, 16 distinct regions, all
records present. The scope uses the defaults (`MaxBatchSize = 128`, `MaxConcurrentBatches = 1`,
`MaxPendingRequests = 1024`, `MaxCachedEntries = 1024`). Allocations use
`GC.GetTotalAllocatedBytes(precise: true)`; timing is the minimum of three rounds.

| Operation | Source calls | Allocated B/op | Time ns/op |
| --- | ---: | ---: | ---: |
| Plan construction only (200 orders) | 0 | 65,808.2 | 49,917.5 |
| Prebuilt plan, new scope, cold cache | 3 | 365,405.3 | 1,488,778.0 |
| FunnySharp consumer, new scope, cold cache | 3 | 419,238.2 | 709,572.5 |
| Hand-batched BCL consumer | 3 | 75,163.3 | 261,592.0 |
| Prebuilt plan, reused scope, warm cache | 0 | 72,091.3 | 146,899.0 |
| FunnySharp consumer, reused scope, warm cache | 0 | 137,899.3 | 184,303.0 |
| GreenDonut consumer, new loaders, cold cache | 3 | 180,582.0 | 725,285.0 |

The GreenDonut row comes from a separate process run; the allocation relationship is stable, and
timing is not compared as a controlled benchmark. Repeated runs reproduced the allocation rows
within a few percent.

The new shipping source files were added to both performance manifests' `benchmarkInput.files`
because the manifest coverage test requires every shipping source. That changes the committed
benchmark input fingerprint. This goal did not start the heavy benchmark suite; the committed
performance observation remains stale until a full benchmark re-record is explicitly authorized.
The measurement harness above is a separate, non-blocking evidence app and is not a release
benchmark gate.

The cost is visible and expected:

- Plan construction is about 66 KB for 200 orders, roughly 330 B per order of description nodes.
- A cold execution adds request slots, per-key completion sources, response validation, typed cache
  entries, and per-branch async machinery. That is about 365 KB for 64+64+16 distinct keys, versus
  75 KB for the hand-batched dictionaries and lists.
- A warm-cache execution of a prebuilt plan is about 72 KB, essentially the same allocation as the
  hand-batched path, because no source or response work remains.
- Building a new plan on every warm execution raises that to about 138 KB. Reusing a plan is the
  economical shape when the same request shape is repeated against a changed source dataset.
- GreenDonut allocates less than the cold plan execution but more than the hand-batched path. It
  does not expose typed per-key outcomes; its loader cache and dispatch are internal to its
  exception-based `Task` model.

No numeric speedup is claimed. The successful fixture makes 3 source calls instead of the 24-call
per-item path, and the 200-order measurement makes 3 instead of 600 per-item calls. The latency
trade-off is disclosed above rather than hidden behind the call reduction.

## Material Composability

The consumer benefit is not only a loadered cache:

- A `BatchRequirement` is a reusable value. The same plan can execute against another scope,
  another request, or a changed source instance; `All` snapshots its input once and preserves one
  outcome per input.
- Independent branches are composable with `Zip` and `All`; dependent branches use the ordinary
  `Bind` shape. The consumer does not manually stage source calls, maintain dictionaries, or
  reimplement response association.
- Missing, per-key, whole-batch, and unexpected-response outcomes are typed `Result` values. The
  scope cache, invalidation, admission limits, and cancellation/disposal behavior are explicit
  properties of the execution rather than conventions in each consumer.
- The order consumer handles missing customer, missing region, per-key customer/preference/region
  failures, whole-batch failures, and malformed responses with the same composition. The
  hand-batched and GreenDonut consumers need separate assembly and exception-mapping code for the
  same fixtures.

GreenDonut remains a capable existing loader. The core does not depend on it or wrap it: GreenDonut
has a runtime package and GraphQL-oriented scheduling surface, and its per-key failures are
exceptions. The FunnySharp implementation adapts the request-buffer/dispatch shape while keeping
the core's zero-runtime-dependency and typed-`Result` boundary. A maintainer who prefers the
existing loader can use it; this goal does not claim GraphQL or loader parity.

## Deliberate Non-Guarantees

The evidence does not claim:

- a cross-request cache, cache propagation, or automatic invalidation after source mutations;
- a general scheduler, DI container, GraphQL execution engine, write coalescing, retry, backoff,
  or automatic batch-capability discovery;
- forced termination of a source delegate that ignores cancellation;
- per-key cancellation, source response ordering, or a specific source-call order across unrelated
  sources;
- a fixed latency ratio on shared hardware, or a universal performance win over GreenDonut or
  hand-batched code;
- transactional, durability, authorization, or tenant-enforcement guarantees. The source's explicit
  partition and the caller's per-request scope are the isolation boundary.
