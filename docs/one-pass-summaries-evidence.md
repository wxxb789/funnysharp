# One-Pass Summary Consumer Evidence

This record supports the maintainer's judgment for
[Goal 0034](goals/archive/0034-goal.md): whether independently defined count, sum, extrema, and
domain summaries compose into one typed result with one traversal and isolated per-component state,
and whether the reusable composition improves complete consumer code without an unexplained
performance tax. It also records the explicit MapAccum non-adoption finding.

Source identity is the Goal 0034 working tree on top of commit `ab31c0d`; the landing commit
becomes the durable identity. Measurements were taken on this host:

- Ubuntu 24.04.5 LTS, Linux 7.0.0-1017-azure, 4 logical cores.
- .NET SDK 10.0.401, runtime 10.0.12, x64; Release build.
- Date: 2026-11-07 (UTC).

Allocation is the blocking comparison; timing is directional on this shared host.

## Compared Consumers

The consumers run the same line-report rules over the same fixtures. The rules, fixtures, probes,
and comparison harness are shared and are not charged to any consumer.

| Version | File | Raw lines | Shape |
| --- | --- | ---: | --- |
| Ordinary reference | [OrdinarySummaryConsumer.cs](../tests/Shared/SummaryComparison/OrdinarySummaryConsumer.cs) | 92 | One `foreach`/`await foreach` loop with local count, total, extrema, category dictionary, and first-SKU state. |
| Separate summaries | [SeparateSummaryConsumer.cs](../tests/Shared/SummaryComparison/SeparateSummaryConsumer.cs) | 36 | Six independent summary evaluations and one `OrderReport` projection; the source is enumerated once per summary. |
| FunnySharp composition | [FunnySharpSummaryConsumer.cs](../tests/Shared/SummaryComparison/FunnySharpSummaryConsumer.cs) | 14 | One reusable nested `Summary.Zip`/`MapResult` definition evaluated once. |

Shared definitions are [SummaryModels.cs](../tests/Shared/SummaryComparison/SummaryModels.cs) (21
lines), [SummaryRules.cs](../tests/Shared/SummaryComparison/SummaryRules.cs) (69 lines),
[SummaryFixtures.cs](../tests/Shared/SummaryComparison/SummaryFixtures.cs) (38 lines),
[ReductionProbes.cs](../tests/Shared/FallibleReduction/ReductionProbes.cs) (169 lines, shared with the fallible-reduction evidence),
[SummaryComparison.cs](../tests/Shared/SummaryComparison/SummaryComparison.cs) (269 lines), and
[MapAccumAssessment.cs](../tests/Shared/SummaryComparison/MapAccumAssessment.cs) (125 lines). The
source implementations are [Summary.cs](../src/FunnySharp/Summary.cs) (597 lines),
[SummaryPlan.cs](../src/FunnySharp/SummaryPlan.cs) (420 lines), and
[ExtremumPolicy.cs](../src/FunnySharp/ExtremumPolicy.cs) (20 lines). Raw lines include braces and
comments; this is not a LOC target.

## Equivalence

`SummaryComparisonVerifier.Verify` and `VerifyAsync` run four fixtures through the ordinary
reference, the separate-summary consumer, and the composed consumer:

`empty`, `single`, `tied prices`, and `mixed categories`.

The comparison asserts:

- structural equality of the complete `OrderReport` (totals, extrema options, ordered category
  counts, and first-SKU option) against the ordinary reference and the separate-summary consumer;
- the exact number of source `GetEnumerator`/`GetAsyncEnumerator`, `MoveNext`/`MoveNextAsync`,
  `Current`, and `Dispose`/`DisposeAsync` calls;
- the exact caller token on the asynchronous enumerator;
- component accumulation and finalization order through a callback ledger;
- that a first-component exception prevents later components and finalizers for that item;
- that an early-determined component still observes every later item while a second component's
  full count remains correct;
- `MapAccumAssessment` parity for `Scan` plus `Select`, `Aggregate` over a tuple, and the local
  MapAccum helper, with one enumeration per form.

The comparison is called by
[SummaryComparisonTests.cs](../tests/FunnySharp.Tests/SummaryComparisonTests.cs) and by the
measurement harness; a disagreement fails both the test run and the measurement run.

| Consumer | GetEnumerator | MoveNext | Current | Dispose |
| --- | ---: | ---: | ---: | ---: |
| Composed summary, N=1024 | 1 | 1,025 | 1,024 | 1 |
| Hand-written loop, N=1024 | 1 | 1,025 | 1,024 | 1 |
| Six separate summaries, N=1024 | 6 | 6,150 | 6,144 | 6 |

The asynchronous table is identical with `GetAsyncEnumerator`/`MoveNextAsync`/`DisposeAsync` and
the same exact token. The observable consequence is that one composed evaluation asks the source
for its items once, while six separately evaluated summaries ask six times.

## Semantic Behavior Ledger

The following behavior is directly asserted in
[SummaryTests.cs](../tests/FunnySharp.Tests/SummaryTests.cs),
[SummaryAsyncTests.cs](../tests/FunnySharp.Tests/SummaryAsyncTests.cs), and the shared comparison:

| Scenario | Observed behavior |
| --- | --- |
| empty source | `Count` is 0; `Sum` is the numeric zero; `Min`/`Max` are `None`; a custom summary returns its seed-derived result; no accumulate or selector call |
| successful composition | one enumeration; each component initialized once, invoked once per reached item, finalized once in operand order |
| component result already determined | every later item is still requested and observed; no early stop or skip |
| integral/decimal sum overflow | `OverflowException` propagates; enumerator disposed; no partial result |
| floating-point sum | sequential source-order addition; not reassociated |
| empty or all-null-key extrema | `None`; the comparer is not invoked for a null key |
| comparer tie | the first encountered key reference is retained |
| value seed | exact seed object is assigned to the evaluation-local state; never cloned |
| mutable value seed | mutations remain visible across later items and later evaluations of the same definition |
| seed factory | invoked once per component per evaluation; two evaluations and two composition positions receive separate objects |
| seed-factory/source/accumulate/result/selector/comparer exception | original exception propagates before or during traversal; enumerator disposed once when acquired; later components for that item and finalizers do not run |
| disposal exception | replaces a pending success under normal C#/async precedence |
| asynchronous token | exact token reaches `GetAsyncEnumerator`; accumulate delegates receive none |
| already-canceled token | not inspected eagerly; a source that ignores it can complete |
| canceled source | returned operation is canceled with the original token; never a summary result |
| async fault | returned operation faults; no result value; enumerator disposed |
| pending async source | operation stays pending without blocking; completes after release |
| repeated evaluation | each call creates state and enumerates again; no cache or memoization |
| null source/delegate/comparer | eager `ArgumentNullException` before enumeration |

## MapAccum Assessment

The previously proposed state-plus-output transformation (often called MapAccum) was compared with
the shipped `Scan` plus `Select` composition and with BCL `Aggregate` over a tuple. The complete
pricing consumer is in
[MapAccumAssessment.cs](../tests/Shared/SummaryComparison/MapAccumAssessment.cs).

| Form | Lines | Shape | One pass |
| --- | ---: | --- | --- |
| `Scan` + `Select` | 16 | `source.Scan(seed, step)` threads `(runningTotal, output)`; `Select` projects the outputs; the final total comes from the last output. | Yes |
| `Aggregate` over `(state, outputs)` | 13 | BCL `Aggregate` updates a running total and a caller-owned output list. | Yes |
| Local MapAccum helper | 13 + 19 | A generic helper wraps a `foreach` and a caller-owned output list; the consumer supplies the step. | Yes |

All three forms returned the same `PricingResult` for every fixture and each enumerated the source
once. The consumer comparison asserts that parity and the enumeration count.

**Finding: do not add a public MapAccum operation.** `Scan` plus `Select` already expresses the
output-only form in one deferred pass; when both outputs and the final state are required,
`Aggregate` over a tuple expresses it in 13 lines without a new public type. The local MapAccum
helper is 32 lines including the generic helper, and it adds no state isolation: it threads state by
value exactly as `Scan` and `Aggregate` do, and a mutable state object remains caller-owned in all
three forms. No consumer in the evidence set demonstrates a useful isolation guarantee or a
meaningful expression gain from a distinct public operation. A public `MapAccum` would duplicate
the existing fold shape and expand the surface without a new contract. The identity of a
state-plus-output operation therefore remains an ordinary consumer helper; if a later goal presents
a consumer with a state-isolation or expression requirement that these forms cannot meet, this
finding should be revisited.

## Measurements

Command:

```bash
dotnet run --project tests/FunnySharp.SummaryMeasurements/FunnySharp.SummaryMeasurements.csproj -c Release
```

The harness first runs the complete sync and async comparison, then reports source consumption and
state sizes, then measures precreated summary definitions. The measurement project disables tiered
compilation (`System.Runtime.TieredCompilation=false`) so every row is full-JIT and allocation is
attributed consistently. Synchronous allocation uses `GC.GetAllocatedBytesForCurrentThread`;
asynchronous allocation uses `GC.GetTotalAllocatedBytes(precise: true)`. Both report the minimum of
three post-warmup rounds, as does timing. Allocation is the stable comparison; timing is
directional.

### State size

| State | Bytes |
| --- | ---: |
| `int` | 4 |
| `decimal` | 16 |
| `Option<decimal>` | 24 |
| numeric totals tuple `(int, decimal, Option<decimal>, Option<decimal>)` | 72 |
| full report tuple `(numeric totals, Dictionary reference, Option<string>)` | 96 |
| 64-byte value state | 64 |
| `Dictionary<string,int>` reference | 8 |
| 64-byte value item | 64 |

The declared state type documents the components but is not copied as a whole during evaluation.
The plan evaluator threads each component's typed state separately.

### Synchronous allocation and timing

| Operation | Allocated B/op | Time ns/op |
| --- | ---: | ---: |
| Built-in `Count` summary, N=1024 | 32.0 | 5,176.9 |
| Custom delegate `Count` summary, N=1024 | 32.0 | 5,922.2 |
| Hand-written `Count` loop, N=1024 | 32.0 | 4,234.9 |
| Bare minimum control, N=1024 | 32.0 | 10,687.0 |
| `MinOrNone`, N=1024 | 32.0 | 12,954.5 |
| `MaxOrNone`, N=1024 | 32.0 | 13,410.8 |
| `Summary.Min`, N=1024 | 32.0 | 17,166.9 |
| `Summary.Max`, N=1024 | 32.0 | 17,863.2 |
| Composed numeric summaries, N=1024 | 328.0 | 60,719.0 |
| Hand-written numeric loop, N=1024 | 120.0 | 30,256.4 |
| Four separate numeric summaries, N=1024 | 216.0 | 66,395.3 |
| Composed numeric summaries, N=16 | 328.0 | 1,152.6 |
| Hand-written numeric loop, N=16 | 120.0 | 548.3 |
| Four separate numeric summaries, N=16 | 216.0 | 1,278.7 |
| Composed numeric summaries, empty | 296.0 | 107.3 |
| Hand-written numeric loop, empty | 88.0 | 31.9 |
| Four separate numeric summaries, empty | 88.0 | 77.7 |
| Full report composed summary, N=1024 | 1,344.0 | 103,895.0 |
| Full report hand-written loop, N=1024 | 960.0 | 61,392.4 |
| Full report separate summaries, N=1024 | 1,120.0 | 112,422.6 |
| 64-byte value state leaf summary, N=1024 | 32.0 | 10,820.6 |
| 64-byte value state hand-written loop, N=1024 | 32.0 | 6,530.2 |
| 64-byte value item count, built-in `Count` summary, N=1024 | 32.0 | 3,898.5 |
| 64-byte value item count, hand-written loop, N=1024 | 32.0 | 3,908.5 |

### Asynchronous allocation and timing

| Operation | Allocated B/op | Time ns/op |
| --- | ---: | ---: |
| Composed numeric summaries, async source, N=1024 | 400.0 | 72,089.2 |
| Hand-written numeric loop, async source, N=1024 | 192.1 | 37,557.8 |
| Four separate numeric summaries, async source, N=1024 | 288.1 | 113,190.8 |
| Full report composed summary, async source, N=1024 | 1,416.1 | 118,832.4 |
| Full report hand-written loop, async source, N=1024 | 1,032.1 | 70,995.3 |
| Full report separate summaries, async source, N=1024 | 1,192.1 | 180,521.3 |

### Interpretation

- **One traversal is real.** The composed report uses one enumerator and 1,025 `MoveNext` calls;
  the six separate evaluations use six enumerators and 6,150 calls. The difference is independent
  of timing noise. The same ledger holds for the asynchronous source with the corresponding
  `GetAsyncEnumerator`/`MoveNextAsync`/`DisposeAsync` counters.
- **Built-in plans avoid delegate accumulation but still enumerate through the interface.** Built-in
  `Count` measured 5,176.9 ns with a 32 B source enumerator; the custom delegate leaf measured
  5,922.2 ns with the same 32 B. The hand-written loop measured 4,234.9 ns with the same 32 B. The
  32 B is the source enumerator, not a library-owned object; the built-in path avoids the per-item
  accumulate delegate that the custom leaf pays.
- **Composed numeric is slightly faster than four separate passes and slower than the hand loop.**
  Four built-in plans composed into one pass measured 60,719.0 ns versus 66,395.3 ns for four
  separate passes, about 9% faster, and 30,256.4 ns for the hand-written loop. On a cheap
  in-memory array, the dispatch and evaluation-object cost largely cancels the saved traversals;
  the one-pass form is not claimed to beat a hand-written loop.
- **The one-pass form wins when acquisition is expensive.** The asynchronous composed numeric form
  measured 72,089.2 ns versus 113,190.8 ns for four separate async passes, about 36% faster, because
  each separate pass repeats the async acquisition machinery. The hand-written loop remains the
  lower bound at 37,557.8 ns.
- **Full report disclosure.** The full six-component report includes a mutable category dictionary
  and first-SKU summary. Its composed form measured 103,895.0 ns versus 112,422.6 ns for six
  separate passes and 61,392.4 ns for the hand loop. The extra cost over the hand loop is the
  custom delegate steps and the nested evaluation plan; it is visible here rather than hidden.
  Composition is chosen for a single-pass or expensive source, not for a cheap in-memory loop.
- **Allocation is constant in the source length, with a fixed evaluation-object cost.** The composed
  numeric form reports 328 B/op at N=16 and N=1024, 296 B/op when empty, and the hand loop reports
  120 B/op non-empty and 88 B/op empty. The fixed composed cost is therefore +208 B per evaluation
  (one small evaluation object per leaf and `Zip` node), not a per-item allocation. The hand loop's
  120 B/op is its result tuple plus the array enumerator.
- **Large values copy without heap growth.** A 64-byte value state accumulates with 32 B/op and
  measured 10,820.6 ns versus 6,530.2 ns for the hand loop; the 32 B is the source enumerator. The
  difference is copying the state through the accumulate delegate boundary, not allocation. A
  64-byte value item with the built-in `Count` plan measured 3,898.5 ns versus 3,908.5 ns for the
  hand loop, with 32 B/op in both. Extrema is allocation-neutral: a bare loop, `MinOrNone`,
  `MaxOrNone`, `Summary.Min`, and `Summary.Max` each measured exactly 32 B/op over the same source.
- **Checked-step extraction preserves the hot path.** The count and sum plans initially duplicated
  `checked(count + 1)` and `checked(total + selector(item))` in their loop, async loop, and
  evaluation paths. Extracting those expressions into one checked `Step` helper per plan produced
  allocation rows identical row-for-row before and after the extraction, with the same composed
  consumption ledger (1 enumerator, 1,025 `MoveNext`). The extraction removes duplicated arithmetic,
  not execution work or allocation.
- **Timing is directional.** The hosted runner varies between runs; the consumption ledger and
  allocation order are the stable comparisons. No universal speedup is claimed.

## Guarantees Kept Separate

| Fact | Where it is established | What it does not establish |
| --- | --- | --- |
| One traversal for the composed form | Probe consumption ledger | That a hand-written loop is slower |
| Independent typed component state | Plan architecture and tests | Immutability or thread safety for caller-owned state |
| Empty extrema are `None` | Option results and empty fixtures | That the source contained no non-null key beyond the selected projection |
| Checked sum overflow | Overflow tests | Floating-point mathematical exactness or reassociation |
| First key retained on ties | Comparer-tie test | A stable sort or a distinction between equal keys |
| Seed factory runs per component/evaluation | Isolation tests | Cloning of a factory result or of a value seed |
| Exceptions propagate and disposal runs | Fault/disposal tests | Rollback of external work or earlier mutations |
| Token reaches the async enumerator | Async token tests | Cancellation of synchronous accumulate work |
| Early-determined component keeps consuming | Full-consumption test | An early-exit or partial-result protocol |
| MapAccum forms agree | Assessment comparison | A public MapAccum guarantee |

## Deliberate Non-Guarantees

The evidence does not claim:

- parallel or out-of-order summary evaluation;
- a public typeclass, summary-interface, or monad hierarchy;
- a LINQ replacement, query syntax, `Where`, or `SelectMany`;
- compile-time fusion, expression-tree analysis, or source generation;
- early completion, partial results, skipping, retry, caching, or memoization;
- cloning, snapshotting, isolation, or rollback of a mutable seed value;
- exception-to-result conversion;
- a universal speedup over a hand-written loop or over separately evaluated summaries;
- a new carrier, outcome channel, or runtime dependency.

## Verification

```bash
dotnet fsi build.fsx -- -p build
dotnet fsi build.fsx -- -p test
dotnet fsi build.fsx -- -p format
dotnet fsi build.fsx -- -p verify-docs-snippets
dotnet fsi build.fsx -- -p verify-api-baseline
dotnet run --project examples/FunnySharp.Examples/FunnySharp.Examples.csproj -c Release
dotnet run --project tests/FunnySharp.SummaryMeasurements/FunnySharp.SummaryMeasurements.csproj -c Release
```

`dotnet fsi build.fsx -- -p build` exits 0 with 0 warnings and 0 errors across the solution.
The benchmark receipt exporter aliases `BenchmarkDotNet.Reports.Summary` because the new public
`FunnySharp.Summary` otherwise shadows it inside the `FunnySharp.Benchmarks` namespace; that is a
consumer-integration point, not a product API change. The focused run reports 42 passing tests,
0 failed, 0 skipped across `SummaryTests`, `SummaryAsyncTests`, and `SummaryComparisonTests`.
The complete `dotnet fsi build.fsx -- -p test` run reports 2,658 passing tests, 0 failed, 0 skipped across
`FunnySharp.Tests`, `FunnySharp.AspNetCore.Tests`, `FunnySharp.Analyzers.Tests`, and
`FunnySharp.Harness.Tests`. `dotnet fsi build.fsx -- -p format` exits 0 (the F# projects are not
C#-formatted, as documented). `dotnet fsi build.fsx -- -p verify-docs-snippets` verifies the 65
primary-guide snippets across 12 guides. `verify-api-baseline` reports 98 types and 874 members
across the two shipping assemblies, including the new summary types and members. The example
program prints `FunnySharp examples passed.` The measurement harness first runs both comparison passes and then
prints the tables above. The table is one representative run; timing remains directional.
