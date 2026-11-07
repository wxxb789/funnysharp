# Fallible Reduction Consumer Evidence

This record supports the maintainer's judgment for
[Goal 0033](goals/archive/0033-goal.md): whether a consumer can reduce an ordinary or asynchronous
sequence through a fallible accumulator, return the final accumulator or the first typed failure,
and stop consuming the source at the failure, without a new stream hierarchy or an async wrapper.

Source identity is the Goal 0033 working tree on top of commit `819f294`; the landing commit
becomes the durable identity. Measurements were taken on this host:

- Ubuntu 24.04.5 LTS, Linux 7.0.0-1017-azure, 4 logical cores.
- .NET SDK 10.0.401, runtime 10.0.12, x64; Release build.
- Date: 2026-11-07 (UTC).

Allocation is the blocking comparison; timing is directional on this shared host.

## Compared Consumers

The consumers run the same line-accumulation rules over the same fixtures. The rules, fixtures,
probes, and comparison harness are shared and are not charged to any consumer.

| Version | File | Raw lines | Shape |
| --- | --- | ---: | --- |
| Ordinary reference | [OrdinaryReductionConsumer.cs](../tests/Shared/FallibleReduction/OrdinaryReductionConsumer.cs) | 93 | Generic `foreach` and `await foreach` loops plus typed entry points; explicit break on the first failed step. |
| Aggregate over Result | [AggregateOverResultConsumer.cs](../tests/Shared/FallibleReduction/AggregateOverResultConsumer.cs) | 39 | Generic `source.Aggregate(Result.Success(seed), (accumulator, item) => accumulator.Bind(...))` and async-source equivalent plus typed entry points; the step is skipped after failure, but the source is consumed to the end. |
| FunnySharp fold | [FunnySharpReductionConsumer.cs](../tests/Shared/FallibleReduction/FunnySharpReductionConsumer.cs) | 29 | `AggregateResult` / `AggregateResultAsync` / `AggregateResultValueAsync`; the loop returns on the failed step. |

The ordinary and aggregate files now carry generic overloads so the measurement harness reuses the same reference loops for `long`, the 64-byte accumulator, and the mutable list accumulator; their raw counts include that shared measurement support. The typed comparison entry points remain short forwarders, and the FunnySharp consumer remains 29 lines. These counts do not charge the shared rules, fixtures, probes, comparison harness, or the library implementation.

Shared definitions are [ReductionModels.cs](../tests/Shared/FallibleReduction/ReductionModels.cs)
(31 lines), [ReductionRules.cs](../tests/Shared/FallibleReduction/ReductionRules.cs) (68 lines),
[ReductionFixtures.cs](../tests/Shared/FallibleReduction/ReductionFixtures.cs) (30 lines),
[ReductionProbes.cs](../tests/Shared/FallibleReduction/ReductionProbes.cs) (169 lines), and
[ReductionComparison.cs](../tests/Shared/FallibleReduction/ReductionComparison.cs) (364 lines).
The source implementations are
[FallibleReductionExtensions.cs](../src/FunnySharp/FallibleReductionExtensions.cs) (88 lines) and
[AsyncFallibleReductionExtensions.cs](../src/FunnySharp/AsyncFallibleReductionExtensions.cs)
(197 lines). Raw lines include braces and comments; this is not a LOC target.

## Equivalence

`ReductionComparison.Verify` and `VerifyAsync` run five fixtures through the ordinary reference,
the `Aggregate`-over-`Result` baseline, and every FunnySharp form:

`valid`, `invalid-middle`, `invalid-first`, `over-limit`, and `empty`.

The comparison asserts:

- structural equality of the complete `Result<ReductionTotal, ReductionError>` outcome against
  the ordinary reference;
- the exact number of source `MoveNext`/`MoveNextAsync` calls, `Current` reads, step invocations,
  enumerator creations, and synchronous/asynchronous disposals;
- the exact caller token on the enumerator and on every token-aware step invocation;
- that the source-fault-after-failure fixture returns the domain failure from the ordinary and
  FunnySharp consumers while the `Aggregate` baseline throws the later source fault.

The comparison is called by
[FallibleReductionComparisonTests.cs](../tests/FunnySharp.Tests/FallibleReductionComparisonTests.cs)
and by the measurement harness; a disagreement fails both the test run and the measurement run.

The important distinction is the **source** request count, not the step count. Both
`Aggregate` over `Result` and `AggregateResult` skip the step after a typed failure; only
`AggregateResult` stops asking the source for another item.

| Consumer | Fixture | MoveNext | Current | Step | Dispose |
| --- | --- | ---: | ---: | ---: | ---: |
| `AggregateResult` | success N=1024 | 1025 | 1024 | 1024 | 1 |
| `foreach` reference | success N=1024 | 1025 | 1024 | 1024 | 1 |
| `Aggregate` over `Result` | success N=1024 | 1025 | 1024 | 1024 | 1 |
| `AggregateResult` | failure at 8, N=1024 | 9 | 9 | 9 | 1 |
| `foreach` reference | failure at 8, N=1024 | 9 | 9 | 9 | 1 |
| `Aggregate` over `Result` | failure at 8, N=1024 | 1025 | 1024 | 9 | 1 |

The asynchronous table is identical with `MoveNextAsync`/`DisposeAsync`; the probe counts are
reported in the measurement output and asserted by the comparison.

The source-fault-after-failure fixture is the observable consequence: the source throws when asked
for item 4 after the domain failure at item 3. The ordinary and FunnySharp folds return the domain
failure; the `Aggregate`-over-`Result` baseline throws the source exception because it kept
consuming. That is the upstream short-circuit difference the goal asks to demonstrate, not a
timing artifact.

## Semantic Behavior Ledger

The following behavior is directly asserted in
[FallibleReductionTests.cs](../tests/FunnySharp.Tests/FallibleReductionTests.cs),
[FallibleReductionAsyncTests.cs](../tests/FunnySharp.Tests/FallibleReductionAsyncTests.cs), and the
shared comparison:

| Scenario | Observed behavior |
| --- | --- |
| empty source | exact seed value/reference returned; zero step calls; one enumerator, one false `MoveNext`, one dispose |
| successful fold | step once per item in source order; final accumulator returned; one enumeration |
| first typed failure | failed result returned unchanged; no later `MoveNext` or step; one dispose |
| source fault | original exception propagates; acquired enumerator disposed |
| step fault | original exception propagates; later items are not requested; enumerator disposed |
| uninitialized step result | `InvalidOperationException`; not converted to a typed failure; enumerator disposed |
| null seed/success/error | preserved as null; no default coercion |
| mutable accumulator | mutation before failure remains visible; no clone, snapshot, isolation, or rollback |
| repeated calls | each call enumerates again; no cache or memoization |
| null source/step | eager `ArgumentNullException`; no enumeration |
| disposal exception | replaces a pending failure/success under normal C# precedence |
| async token | exact token reaches `GetAsyncEnumerator` and the token-aware step |
| already-canceled token | not inspected eagerly; a source/step that ignores it can succeed |
| canceled source/step | returned operation is canceled with the original token; never a domain failure |
| faulted `OperationCanceledException` step | returned operation is canceled with that exception's token, matching the package's async traversal contract |
| async pending step | returned operation stays pending; no blocking; one disposal after completion |
| `ValueTask` step | observed exactly once |
| async disposal exception | replaces a pending failure |

## Measurements

Command:

```bash
dotnet run --project tests/FunnySharp.FallibleReductionMeasurements/FunnySharp.FallibleReductionMeasurements.csproj -c Release
```

The harness first runs the complete sync and async comparison, then measures precreated step
delegates. Synchronous allocation uses `GC.GetAllocatedBytesForCurrentThread`; asynchronous
allocation uses `GC.GetTotalAllocatedBytes(precise: true)`. Timing is the minimum of three rounds.

### Synchronous allocation and timing

| Operation | Allocated B/op | Time ns/op |
| --- | ---: | ---: |
| `AggregateResult`, `long`, success N=1024 | 0.0 | 2941.6 |
| `foreach` reference, `long`, success N=1024 | 0.0 | 2944.1 |
| `AggregateResult`, `long`, `List<int>` source, success N=1024 | 40.0 | 10198.7 |
| `foreach` reference, `long`, `List<int>` source, success N=1024 | 40.0 | 8829.0 |
| `Aggregate` over `Result` (natural `Bind`), `long`, success N=1024 | 98275.6 | 22602.8 |
| `Aggregate` over `Result` (helper, no per-item closure), `long`, success N=1024 | 24.0 | 12213.0 |
| `AggregateResult`, `long`, failure at 8, N=1024 | 0.0 | 46.8 |
| `foreach` reference, `long`, failure at 8, N=1024 | 0.0 | 47.0 |
| `Aggregate` over `Result` (natural `Bind`), `long`, failure at 8, N=1024 | 98328.0 | 22211.0 |
| `Aggregate` over `Result` (helper), `long`, failure at 8, N=1024 | 24.0 | 11952.4 |
| `AggregateResult`, `long`, empty | 0.0 | 16.2 |
| `AggregateResult`, `long`, success N=16 | 0.0 | 55.5 |
| `AggregateResult`, 64-byte value accumulator, success N=1024 | 0.0 | 18802.8 |
| `foreach` reference, 64-byte value accumulator, success N=1024 | 0.0 | 18808.0 |
| `AggregateResult`, mutable list accumulator, success N=1024 | 8456.0 | 5888.2 |
| `foreach` reference, mutable list accumulator, success N=1024 | 8456.0 | 5884.7 |
| `AggregateResult`, mutable list accumulator, failure at 8, N=1024 | 160.0 | 102.5 |

### Asynchronous allocation and timing

| Operation | Allocated B/op | Time ns/op |
| --- | ---: | ---: |
| `AggregateResultAsync`, sync step, success N=1024 | 192.0 | 21236.3 |
| `await foreach` reference, sync step, success N=1024 | 104.0 | 6091.8 |
| `AggregateAsync` over `Result`, success N=1024 | 98496.0 | 30329.5 |
| `AggregateResultAsync`, sync step, failure at 8, N=1024 | 120.0 | 310.1 |
| `await foreach` reference, sync step, failure at 8, N=1024 | 32.0 | 145.5 |
| `AggregateAsync` over `Result`, failure at 8, N=1024 | 98424.0 | 27452.2 |
| `AggregateResultAsync`, sync step, empty | 120.0 | 112.4 |
| `await foreach` reference, sync step, empty | 32.0 | 104.0 |
| `AggregateResultValueAsync`, tokenless `ValueTask` step, success N=1024 | 192.0 | 13345.6 |
| `AggregateResultValueAsync`, token-aware `ValueTask` step, success N=1024 | 104.0 | 12865.5 |
| `await foreach` reference, `ValueTask` step, success N=1024 | 104.0 | 12386.5 |
| `AggregateResultValueAsync`, tokenless `ValueTask` step, failure at 8, N=1024 | 120.0 | 230.8 |
| `AggregateResultValueAsync`, token-aware `ValueTask` step, failure at 8, N=1024 | 32.0 | 222.2 |
| `await foreach` reference, `ValueTask` step, failure at 8, N=1024 | 32.0 | 205.4 |

### Interpretation

- **No buffering.** The empty and 16-item `long` rows allocate 0 B, and the 1024-item success row
  allocates 0 B after the harness discards one warm-up measurement before the reported table. With
  a `List<int>` source, both the fold and the reference loop allocate the same 40 B for the boxed
  list enumerator. The failure rows stop at item 9 and allocate 0 B.
- **Copying.** Value-type accumulators are copied into and out of each step by the delegate
  signature. The 64-byte accumulator costs about 18.7-19.0 us/op for both the fold and the
  reference loop at 1024 items, against about 3.0 us/op for an 8-byte `long`; the difference is
  copy cost, not heap allocation.
- **Mutable accumulator.** The list accumulator allocates 8,456 B/op for caller-owned capacity
  growth, identical to the reference loop; the library adds no clone or snapshot. Failure at item 9
  allocates 160 B, which is the partially grown list and the error payload, not a buffered result
  sequence.
- **Async adapter cost.** Both adapter-based asynchronous overloads (synchronous step and the
  tokenless `ValueTask` step) cost one closure per call: 192 B/op on success and 120 B/op on the
  failure/empty path. The token-aware `ValueTask` overload forwards the step directly and measured
  104 B and 32 B, matching the hand-written `await foreach` reference; the empty synchronous-step
  reference measures 32 B/op.
- **Aggregate-over-Result baseline.** The natural `Bind` form allocates a per-item closure because
  the lambda captures the current item; that is a property of the idiomatic baseline, not of
  `Aggregate`. A helper-based baseline avoids the closure (24 B/op for its one per-call closure)
  and still consumes all 1025 source requests after the failure. The source-consumption table is
  the acceptance-relevant comparison; the allocation rows disclose both baselines rather than
  claiming an allocation win over an artificially weakened one.
- **Timing.** Timing is directional. The success rows carry the full 1024-step loop; the failure
  rows stop at item 9 and are correspondingly cheaper. No universal speedup is claimed.

## Semantic Independence

The existing operations have different shapes and guarantees:

- BCL `Aggregate` over `Result` composes the accumulator with `Bind`. It stops invoking the step
  after failure but keeps calling `MoveNext` to the end, so a later source fault or side effect is
  still observed. `AggregateResult` stops the source.
- `Sequence`/`Traverse` materialize successful values or errors; they are collection boundaries,
  not a constant-space fold.
- `Scan` is deferred and yields every intermediate accumulator; it does not stop on a fallible
  step and does not return one final result.
- `Bind` composes a carrier-returning function but cannot stop an already-running `foreach`.
- `NonEmpty<T>.Aggregate` is a non-fallible seedless fold over a proven non-empty value.
- `Check`/`CheckAll` validate one value or an ordered independent rule set; they do not consume a
  sequence.
- `Validation` traversal accumulates independent item errors; a dependent fold must not pretend
  those rules are independent.

The public operation is therefore not an alias: no existing member combines a caller-supplied
seeded fallible step with source-level short-circuiting and the existing `Result` carrier.

## Guarantees Kept Separate

| Fact | Where it is established | What it does not establish |
| --- | --- | --- |
| The final accumulator or first typed failure | `AggregateResult` outcome and comparison | That external work or mutation was rolled back |
| No later source request after a typed failure | Probe consumption ledger | That a source's own side effects before the failure did not happen |
| The exact failure payload is preserved | Failure identity/equality tests | That errors are deduplicated or merged |
| The enumerator is disposed once | Probe disposal counts | Forced termination of uncooperative source work |
| Cancellation and faults stay cancellation/faults | Async tests | That a started external operation was canceled |
| The accumulator is not cloned or snapshotted | Mutable accumulator tests | Isolation, immutability, or thread safety |
| Async work completes without blocking | Pending-step test | Parallel execution or scheduler policy |

## Deliberate Non-Guarantees

The evidence does not claim:

- parallel or out-of-order reduction, because arbitrary callbacks do not establish associativity,
  commutativity, purity, or safe partitioning;
- rollback, compensation, transaction, or exactly-once behavior for external work;
- cloning, isolation, immutability, or thread safety for mutable accumulators;
- retry, backoff, timeout, or scheduling policy;
- exception-to-domain-error conversion;
- memoization or caching across calls;
- a new outcome carrier, async wrapper, stream hierarchy, or generic early-exit protocol;
- a universal speedup or a release timing claim from the measurement harness.

## Verification

```bash
dotnet fsi build.fsx -- -p test
dotnet fsi build.fsx -- -p format
dotnet fsi build.fsx -- -p verify-docs-snippets
dotnet fsi build.fsx -- -p verify-api-baseline
dotnet run --project examples/FunnySharp.Examples/FunnySharp.Examples.csproj -c Release
dotnet run --project tests/FunnySharp.FallibleReductionMeasurements/FunnySharp.FallibleReductionMeasurements.csproj -c Release
```

The focused run reports 37 passing tests, 0 failed, 0 skipped across `FallibleReductionTests`,
`FallibleReductionAsyncTests`, and `FallibleReductionComparisonTests`. The complete
`dotnet fsi build.fsx -- -p test` run reports 2,616 passing tests, 0 failed, 0 skipped across
`FunnySharp.Tests`, `FunnySharp.AspNetCore.Tests`, `FunnySharp.Analyzers.Tests`, and
`FunnySharp.Harness.Tests`. `dotnet fsi build.fsx -- -p verify-docs-snippets` verifies the 60
existing primary-guide snippets. `verify-api-baseline` reports 95 types and 858 members across the
two shipping assemblies, including the four new methods. The example program prints
`FunnySharp examples passed.` The measurement harness first runs both comparison passes and then
prints the tables above. The table is one representative run; timing remains directional.
