# Nested Data Reshaping Consumer Evidence

This record supports the maintainer's judgment for
[Goal 0036](goals/archive/0036-goal.md): whether a realistic nested import can reach ordinary
typed rows with explicit policies for missing fields, nulls, empty child collections, unequal
parallel lengths, and rejected items; whether decoder and rejection failures identify their
originating record and nested element without application-built path strings while structural
failures identify the record and counts; and whether aligned expansion is distinguishable from
Cartesian expansion in practice.

Source identity is the Goal 0036 working tree on top of commit `ffde9c9`; the landing commit
becomes the durable identity. Measurements were taken on this host:

- Ubuntu 24.04.5 LTS, Linux 7.0.0-1017-azure, x64.
- .NET SDK 10.0.401, runtime 10.0.12, x64; Release build.
- Date: 2026-11-08 local / 2026-11-08 UTC.

Allocation is the blocking comparison; timing is directional on this shared host.

## Compared Consumers

The consumers run the same nested import rules over the same fixtures. The models, fixtures, probe,
and comparison harness are shared and are not charged to any consumer.

| Version | File | Raw lines | Shape |
| --- | --- | ---: | --- |
| Ordinary reference | [OrdinaryNestedConsumer.cs](../tests/Shared/NestedDataComparison/OrdinaryNestedConsumer.cs) | 50 | `JsonSerializer` plus `SelectMany`, materialized with `ToList`; no field policy, no rejection record, no positions. For aligned data it uses BCL `Zip`, which truncates. |
| Hand-written whole import, eager paths | [HandWrittenNestedConsumer.cs](../tests/Shared/NestedDataComparison/HandWrittenNestedConsumer.cs) | 331 | `JsonDocument` plus manual property, kind, and path checks; aligned length comparison; rejection with record/element indices. Builds a path string at the top of each decode helper on the success path. |
| Hand-written whole import, deferred paths | [HandWrittenDeferredPathNestedConsumer.cs](../tests/Shared/NestedDataComparison/HandWrittenDeferredPathNestedConsumer.cs) | 287 | The same import decisions with the path string built only inside a failure branch; the comparison asserts the identical report. |
| FunnySharp consumer | [FunnySharpNestedConsumer.cs](../tests/Shared/NestedDataComparison/FunnySharpNestedConsumer.cs) | 125 | `Decoder.List`/`Field`/`List` plus `Expand`/`ExpandAligned`/`ExpandCartesian`, diagnostic mapping, and rejection through `RowOrigin`. |

Shared definitions are [NestedModels.cs](../tests/Shared/NestedDataComparison/NestedModels.cs)
(91 lines), [NestedFixtures.cs](../tests/Shared/NestedDataComparison/NestedFixtures.cs) (244 lines),
and [NestedComparison.cs](../tests/Shared/NestedDataComparison/NestedComparison.cs) (290 lines). The
source implementations are [NestedDataExtensions.cs](../src/FunnySharp/NestedDataExtensions.cs)
(620 lines), [NestedRowError.cs](../src/FunnySharp/NestedRowError.cs) (155 lines),
[RowOrigin.cs](../src/FunnySharp/RowOrigin.cs) (56 lines), and
[CartesianRowOrigin.cs](../src/FunnySharp/CartesianRowOrigin.cs) (57 lines). Raw lines include
braces and comments; this is not a LOC target. The FunnySharp file contains three decoders and all
three expansion shapes; the hand-written files spend most of their lines reimplementing exactly the
missing/null/kind/path decisions the decoder already owns.

## Equivalence

`NestedComparisonVerifier.Verify` runs eleven import fixtures through the fixed expected report,
the eager-path hand-written consumer, the deferred-path hand-written consumer, and the FunnySharp
consumer:

`valid import`, `rejected item`, `empty lines`, `missing quantity`, `null quantity`,
`wrong quantity kind`, `malformed quantity`, `null lines`, `missing order id`, `empty customer`,
and `multiple field failures`; a separate fixture covers the null typed child list. Five aligned
fixtures cover `aligned success`, `aligned mismatch`, `aligned mismatch after a valid record`,
`aligned empty pair`, and `aligned empty versus nonempty`. Three Cartesian fixtures cover the
`cartesian product`, `cartesian unequal sides`, and `cartesian empty side`.

The comparison asserts:

- fixed expected rows, rejections, decode failures, and expansion failures for every import
  fixture, including deterministic field order and `[record].lines[element]` paths;
- eager-path, deferred-path, and FunnySharp report equality, including the rejection at record 2
  element 0;
- the ordinary form's fixed behavior: rows match on valid input, differ when it has no rejection
  policy, throw `JsonException` on null/wrong-kind/malformed values, and throw
  `ArgumentNullException` on a null `lines` array;
- hand-written and FunnySharp aligned-row/failure equality, and the exact rows BCL `Zip` keeps
  after truncation;
- hand-written and FunnySharp Cartesian-row equality, and ordinary nested `SelectMany` equality;
- exact source enumerator, `MoveNext`, `Current`, `Dispose`, selector, and combine counts for the
  buffered expansion ledger;
- the combine ledger: later records are still checked for null/length failures after a first
  error, but no later combine call occurs;
- the typed null child-list failure and its rendered record position.

The comparison is called by
[NestedDataComparisonTests.cs](../tests/FunnySharp.Tests/NestedDataComparisonTests.cs) and by the
measurement harness; a disagreement fails both the test run and the measurement run. Focused tests
in [NestedDataErrorTests.cs](../tests/FunnySharp.Tests/NestedDataErrorTests.cs),
[NestedDataExpansionTests.cs](../tests/FunnySharp.Tests/NestedDataExpansionTests.cs),
[NestedDataAlignmentTests.cs](../tests/FunnySharp.Tests/NestedDataAlignmentTests.cs), and
[NestedDataAsyncTests.cs](../tests/FunnySharp.Tests/NestedDataAsyncTests.cs) pin the boundaries the
shared fixtures do not: argument nullness, negative factory positions, equal-count mismatch
rejection, source/selector/combine exceptions for the single, aligned, and Cartesian shapes,
repeated calls, source re-enumeration, null child-element and null parent pass-through, child-list
mutation during the call, async token forwarding, cancellation, source faults, and disposal.

## Fixed Failure Locations

The fixed expectations demonstrate that a consumer sees actionable positions rather than a generic
exception or a silently truncated row set:

| Scenario | Expected failure |
| --- | --- |
| missing quantity | `Missing` at `[0].lines[0].quantity` |
| null quantity | `Null` at `[0].lines[0].quantity` |
| wrong quantity kind | `WrongKind` at `[0].lines[0].quantity` |
| malformed quantity `1.5` | `Malformed` at `[0].lines[0].quantity` |
| null lines array | `Null` at `[0].lines` |
| missing order id | `Missing` at `[0].orderId` |
| empty customer | `Empty` at `[0].customer` |
| multiple independent failures | `[0].lines[0].sku` Empty, `[0].lines[0].quantity` Null, `[0].lines[1].quantity` Missing, in that order |
| aligned mismatch | `ChildCountMismatch` at record 0 with counts 1 and 2 |
| later aligned mismatch | `ChildCountMismatch` at record 1 with counts 2 and 1; the earlier valid record contributes no row |
| empty versus nonempty aligned | `ChildCountMismatch` at record 0 with counts 0 and 1 |
| rejected row | record 2, element 0, rendered by the origin as `record[2].element[0]` |

The hand-written form builds these path strings itself; the FunnySharp form receives
`DecodeError.Path` from the decoder, the rejection position from `RowOrigin`, and the structural
record position (with both counts for a mismatch) from `NestedRowError`. No application code
parses or assembles a path on the FunnySharp side.

## Semantic Behavior Ledger

The following behavior is directly asserted in the focused tests and the shared comparison:

| Scenario | Observed behavior |
| --- | --- |
| empty record source | valid empty success; no selector or combine call |
| record with empty child list | zero rows and no error; other records still expand |
| null typed child list | `NullChildSequence` with the record index; never treated as empty |
| null first/second aligned list | `NullFirstChildSequence`/`NullSecondChildSequence`; both selectors run; one or two errors |
| equal aligned lengths | one row per index in increasing index order; two pairs produce two rows, not four |
| unequal aligned lengths | one `ChildCountMismatch` with both counts and no rows |
| several unequal aligned records | one `ChildCountMismatch` per mismatching record, in record order, and no rows |
| BCL `Zip` over the same unequal input | keeps the common prefix and silently drops the extra element |
| Cartesian equal sides | explicit product in record, first-index, second-index order |
| Cartesian unequal sides | product succeeds; no mismatch error |
| Cartesian empty side | zero rows and no error |
| rejected quantity | library returns the row; the consumer records a rejection with the origin; nothing is dropped silently |
| combine ledger | combine runs for a unique/valid record before the first error and not after it; earlier rows are discarded |
| source enumeration | exactly once for every synchronous shape; one `GetEnumerator`, `n + 1` `MoveNext`, `n` `Current`, one `Dispose` |
| async source | `ExpandAsync`/`ExpandAlignedAsync`/`ExpandCartesianAsync` match the synchronous rows and failures; token forwarded unchanged; enumerator disposed |
| async cancellation/fault | `OperationCanceledException` and source faults propagate unchanged; no `NestedRowError` conversion |
| selector/combine exception | original exception identity propagates; no result is returned |
| null child element or null parent record | passed to the combine callback unchanged; not filtered or rejected |
| child-list mutation | reads follow the live list during the call; no snapshot or clone |
| repeated call | source is enumerated again; no library cache |
| origin value | record/element positions and value equality; `ToString` renders `record[i].element[j]` and `record[i].elements[a,b]` |
| mismatch error factory | equal counts and negative positions are rejected eagerly |

## Consumption Ledger

| Operation | Source `GetEnumerator` | Source `MoveNext` | Source `Current` | Source `Dispose` | Selector calls | Combine calls |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `Expand`, 2 records / 3 children | 1 | 3 | 2 | 1 | 2 | 3 |
| `Expand`, valid then null then valid | 1 | 4 | 3 | 1 | 3 | 1 |
| `ExpandAligned`, 2 records / 2 and 1 children, second always 2 | 1 | 3 | 2 | 1 | 2 (first) | 2 before the record-1 mismatch |

The observable consequence is that the source is never re-enumerated for validation or output, the
child selector runs once per reached record, and combine stops after the first structural error
while later records are still inspected for diagnostics.

The async focused test additionally pins one `GetAsyncEnumerator`, `records + 1`
`MoveNextAsync`, `records` `Current`, and one `DisposeAsync` for `ExpandAsync`, plus exact token
forwarding to `GetAsyncEnumerator`.

## Measurements

Command:

```bash
dotnet run --project tests/FunnySharp.NestedDataMeasurements/FunnySharp.NestedDataMeasurements.csproj -c Release
```

The harness first runs the complete comparison, then measures precreated fixtures at N=1,024
records with four lines (or four-by-three variants) per record, 50 iterations after 5 warmup
iterations, reporting the minimum of three post-warmup rounds. The measurement project disables
tiered compilation (`System.Runtime.TieredCompilation=false`), so every row is full-JIT. Every row
uses `GC.GetAllocatedBytesForCurrentThread`; the measured awaits all complete synchronously on the
measuring thread, so one counter observes the whole operation. Allocation is the stable comparison;
timing is directional.

The hand-written import is measured twice: the existing eager-path form and a deferred-path form
that builds a diagnostic path only inside a failure branch. The comparison asserts that both forms
produce the identical report. All ordinary `JsonSerializer` rows materialize with `ToList` so the
LINQ builder/pool path is not a measurement variable.

| Operation | Allocated B/op | Time ns/op |
| --- | ---: | ---: |
| Import success - ordinary `JsonSerializer` + `SelectMany` | 1,492,768.8 | 4,066,194.0 |
| Import success - hand-written `JsonDocument` + eager paths | 1,706,128.8 | 4,305,882.0 |
| Import success - hand-written `JsonDocument` + deferred paths | 771,456.8 | 3,451,548.0 |
| Import success - FunnySharp `Decoder` + `Expand` | 1,148,776.8 | 4,008,492.0 |
| Import failure - hand-written eager paths (wrong quantity kind) | 1,705,952.8 | 4,347,626.0 |
| Import failure - hand-written deferred paths (wrong quantity kind) | 771,352.8 | 3,450,740.0 |
| Import failure - FunnySharp decoder (wrong quantity kind) | 633,736.8 | 3,774,558.0 |
| Aligned success - ordinary `Zip` | 1,082,400.8 | 2,336,254.0 |
| Aligned success - hand-written | 787,024.8 | 2,088,730.0 |
| Aligned success - `ExpandAligned` | 869,136.8 | 2,580,108.0 |
| Aligned mismatch - hand-written | 787,544.8 | 2,080,518.0 |
| Aligned mismatch - `ExpandAligned` | 853,616.8 | 2,658,588.0 |
| Cartesian typed - hand-written index loops | 753,976.8 | 421,436.0 |
| Cartesian typed - `ExpandCartesian` | 843,984.8 | 566,954.0 |
| Expand success - origin ignored | 221,344.8 | 94,112.0 |
| Expand success - origin ignored, uncounted source (sync control) | 229,696.8 | 92,134.0 |
| Expand success - origin stored | 254,112.8 | 91,750.0 |
| Expand success - per-row `Location` object (disclosure) | 1,007,776.8 | 311,312.0 |
| Expand success - per-row location string (disclosure) | 545,824.8 | 315,548.0 |
| Expand failure - one null child list, 1,024 records | 8,456.8 | 7,778.0 |
| Expand failure - one null child list, 1 record | 272.8 | 366.0 |
| Expand failure - many null child lists | 57,608.8 | 17,942.0 |
| Expand failure - render one error (disclosure) | 8,544.8 | 7,860.0 |
| Expand failure - render many errors (disclosure) | 147,776.8 | 74,954.0 |
| Empty - `Expand` | 32.8 | 88.0 |
| Async - `ExpandAsync` success | 229,768.8 | 101,954.0 |

Two consecutive runs reproduced every allocation row in this table byte-for-byte; only the
directional timings varied.

The ordinary Cartesian `JsonSerializer`/LINQ row is excluded from the stable table, not because it
is unavailable but because it does not reproduce: it measured 2,090,017.1 B and 2,090,034.6 B in
two exploratory runs (spread 17.5 B/op) after the `ToList` change, so the remaining variance is
inside `JsonSerializer`'s pooled buffers rather than the LINQ materialization call. The comparison
still asserts its row output on the Cartesian fixtures; the hand-written index-loop and
`ExpandCartesian` rows are byte-identical.

## Interpretation

- **The full import is smaller than the ordinary and eager hand-written forms, but larger than the
  deferred-path hand-written form.** The ordinary `JsonSerializer` + `SelectMany` form allocates
  1,492,768.8 B for the valid fixture but has no field policy, no rejection record, and no location
  for a malformed value. The eager hand-written form allocates 1,706,128.8 B; the deferred-path
  form allocates 771,456.8 B. FunnySharp allocates 1,148,776.8 B: 343,992.0 B less than the
  ordinary form and 557,352.0 B less than the eager hand-written form, but 377,320.0 B *more* than
  the deferred-path form. The ordinary form's 1,492,768.8 B is `JsonSerializer` deserialization
  plus LINQ materialization for a narrower contract, not a like-for-like policy implementation.
  The eager form's 934,672.0 B premium is the 15,360 success-path path
  strings it builds (about 60.8 B each; even at the 40-byte minimum string object that is
  614,400 B). The deferred baseline performs the same field/null/kind/empty decisions and returns
  the same report, so the residual gap is decoder and `Validation` machinery plus `Expand`'s
  buffered list and exact-size array. FunnySharp also materializes one `LocatedLine` per row before
  mapping to report rows, so this record does not claim it avoids intermediate diagnostic
  scaffolding.
- **Decoder failure is cheaper than both hand-written forms on this fixture.** The failing fixture
  makes `"quantity":"bad"` at the last record's first line, a `WrongKind` failure: FunnySharp
  allocates 633,736.8 B versus 771,352.8 B for the deferred-path form (137,616.0 B less) and
  1,705,952.8 B for the eager form (1,072,216.0 B less). The eager form builds 15,360 paths before
  reaching the last record; the deferred form builds only the one failing path. The remaining cost
  is parsing and typed-value materialization, not the decoder's error list.
- **Aligned expansion pays a bounded, disclosed cost.** On a valid aligned import the hand-written
  form allocates 787,024.8 B; `ExpandAligned` allocates 869,136.8 B, 82,112.0 B more, because the
  decoder and the buffered `Validation` machinery are included. The ordinary `Zip` form allocates
  1,082,400.8 B and silently truncates on mismatch. On the mismatch fixture both complete forms
  suppress all rows; the hand-written form allocates 787,544.8 B and `ExpandAligned` 853,616.8 B,
  a 66,072.0 B gap for the same one-error report.
- **Cartesian expansion is more expensive than an equally careful hand-written product.**
  `ExpandCartesian` materializes 12,288 rows in 843,984.8 B versus 753,976.8 B for the manual index
  loops, 90,008.0 B *more*. The earlier hand loop allocated 917,848.8 B because `foreach` over
  `IReadOnlyList<string>` boxed one outer, 1,024 size, and 4,096 color enumerators (5,121 × 32 B
  ≈ 163,872 B). With index loops the baseline no longer pays that tax, and the library's buffered
  `Validation` and exact-size output array make it the more expensive form. Aligned expansion on
  the same input would produce 4 rows per record by index, not 12; the relation is chosen by the
  method name, and the table does not conflate the two shapes.
- **Provenance is a by-value cost, not a hidden trace.** Ignoring the origin allocates 221,344.8 B;
  storing it in every row allocates 254,112.8 B. The difference is exactly 32,768 B for 4,096 rows,
  or 8 B per row—the two `int` fields of `RowOrigin` inside the caller's row record. A per-row
  `Location` object allocates 1,007,776.8 B, 753,664.0 B more than the origin-stored row (the
  786,432 B figure is the difference against the origin-ignored row and is not used here). A
  per-row rendered string allocates 545,824.8 B, 291,712.0 B more than the origin-stored row. The
  minimal form is genuinely minimal: the library neither creates a `Location` nor forms a string
  unless the caller asks.
- **Failure storage is small; the 1,024-record row is mostly the pre-sized output list.** One null
  child list in a 1,024-record source costs 8,456.8 B, of which 8,192 B is the row list pre-sized
  to 1,024 references and 264.8 B is list and error scaffolding. The single-record source costs
  272.8 B, so failure storage itself is a few hundred bytes, not 8 KB. 1,024 null child lists cost
  57,608.8 B, 49,152.0 B more than the one-null row, or 48.0 B per additional error from error-list
  growth. Rendering one error adds 88.0 B; rendering all 1,024 adds 90,168.0 B, 88.05 B per
  rendered string. The structured error is a struct and the text is not part of the stored result.
- **Async changes acquisition, not the row contract.** `ExpandAsync` over the same 1,024×4 typed
  fixture allocates 229,768.8 B, 8,424.0 B more than the synchronous `Expand`. A synchronous control
  over an uncounted wrapper (the async source exposes no count) allocates 229,696.8 B, so the
  async form adds only 72.0 B. The pre-size difference is 8,352.0 B, 99.1% of the reported delta:
  the synchronous core starts the list at the source count (1,024) while the async core starts at
  0, and growth from 0 to 4,096 costs that much more. The residual 72.0 B is the async
  enumerator/state machine. Both rows use the same counter, so this is a like-for-like comparison.
- **Timing is directional.** Two consecutive runs reproduced every allocation row in the table
  byte-for-byte and varied only the directional timings. No universal speedup is claimed. The
  ordinary forms remain shorter and carry less policy machinery when no field policy, alignment,
  rejection, or location is required, but they are not cheaper in allocation on these fixtures:
  ordinary import 1,492,768.8 B versus FunnySharp 1,148,776.8 B, and ordinary aligned 1,082,400.8 B
  versus 869,136.8 B. The ordinary Cartesian row is not allocation-stable and is disclosed above.

## Maintainer Judgment

The expansion family materially simplifies the *complete* nested import. The eager hand-written
reference needs 331 lines to parse the document, distinguish missing/null/kind/malformed/empty,
build paths, compare aligned lengths, and record rejections; the deferred-path variant needs 287
lines and returns the same report. The FunnySharp consumer expresses the same rules in 125 lines:
three decoders, three expansion calls, diagnostic mapping, and the rejection policy. The ordinary
50-line form is shorter only because it does not meet the same contract: it has no field policy, no
rejection record, no failure position, and BCL `Zip` truncates.

The allocation judgment is mixed and does not favor the library in every shape. Against the eager
hand-written import FunnySharp is 557,352.0 B smaller on success, but against the deferred-path
import it is 377,320.0 B larger on success and 137,616.0 B smaller on the failing fixture. Aligned
expansion is 82,112.0 B larger than the hand-written form, and Cartesian expansion is 90,008.0 B
larger than the index-loop baseline. The case for the family is code shape and explicit policy, not
an allocation win over competent hand-written code.

The judgment is conditional. For a plain nested list where the input is already trusted, every
item is accepted, and no location is needed, `SelectMany` remains shorter; the origin parameter and
the `Validation` boundary are not earned there. `ExpandAligned` also adds a structural check that a
caller must handle, and its all-or-nothing output means a single bad record discards earlier rows.
The family should be kept where alignment, an explicit rejection policy, or a record-level
structural failure location is the actual requirement; element positions come from the origin or
the decoder path, not from `NestedRowError`. The evidence does not support replacing ordinary
sequence projection or BCL `Zip` generally.

## Guarantees Kept Separate

| Fact | Where it is established | What it does not establish |
| --- | --- | --- |
| Typed field extraction and JSON paths | `Decoder.List`/`Field`/`List`, fixed paths | That the document remains alive after the call |
| Exact aligned pairing | `ExpandAligned`, count-read fixtures | That BCL `Zip` is safe to substitute |
| Explicit Cartesian relation | `ExpandCartesian`, 4-versus-2 row fixture | That a product is ever inferred automatically |
| Null child list is a failure | `NestedRowError` fixtures | That an empty list is also a failure |
| Null child element and null parent are passed through | Focused pass-through test | That the library filters or rejects either |
| Empty child list is zero rows | Empty fixtures | That an empty record is an error |
| Rejection is explicit | Consumer rejection with `RowOrigin` | That the library filters domain values |
| All-or-nothing output | `Validation` success/failure fixtures | That partial rows are available |
| By-value provenance | Origin equality and allocation rows | That a `Location` trace is free |
| Single source enumeration | Probe ledger | That a caller source is replayable |
| Async token forwarding | Focused async tests | That the selectors or combine are asynchronous |
| Exceptions stay exceptions | Focused exception tests | Any rollback of earlier work |

## Deliberate Non-Guarantees

The evidence does not claim:

- a serializer, reflection binder, schema registry, DataFrame engine, or R-style evaluation;
- automatic lossy coercion or a second absence/outcome carrier;
- streaming partial output or a per-row failure stream;
- span-based or `JsonElement`-retaining child views across an await;
- asynchronous selectors or combine callbacks;
- more than two parallel child sequences;
- an inferred Cartesian product from data;
- cloning, freezing, snapshotting, or rollback of caller values or earlier combine calls;
- conversion of cancellation or programming exceptions into `NestedRowError` values;
- a database, file, or network boundary;
- a universal simplification or speedup over `SelectMany`, `Zip`, or `JsonSerializer`;
- any Goal 0037 behavior.

## Verification

```bash
dotnet fsi build.fsx -- -p build
dotnet fsi build.fsx -- -p test
dotnet fsi build.fsx -- -p format
dotnet fsi build.fsx -- -p verify-docs-snippets
dotnet fsi build.fsx -- -p verify-api-baseline
dotnet run --project examples/FunnySharp.Examples/FunnySharp.Examples.csproj -c Release
dotnet run --project tests/FunnySharp.NestedDataMeasurements/FunnySharp.NestedDataMeasurements.csproj -c Release
```

Recorded results:

- `dotnet fsi build.fsx -- -p build`: exit 0, 0 warnings, 0 errors across the solution.
- `dotnet fsi build.fsx -- -p test`: 2,774 passed, 0 failed, 0 skipped across
  `FunnySharp.Tests`, `FunnySharp.AspNetCore.Tests`, `FunnySharp.Analyzers.Tests`, and
  `FunnySharp.Harness.Tests`. The previous baseline was 2,717 tests; the 57 new tests are
  `NestedDataErrorTests`, `NestedDataExpansionTests`, `NestedDataAlignmentTests`,
  `NestedDataAsyncTests`, and `NestedDataComparisonTests`.
- `dotnet fsi build.fsx -- -p format`: exit 0. The two F# projects are reported as unsupported
  because `dotnet format` checks C# only; every C# project passed.
- `dotnet fsi build.fsx -- -p verify-docs-snippets`: 72 C# documentation snippets across 14
  primary guides, including the four new `NestedData` regions.
- `dotnet fsi build.fsx -- -p verify-api-baseline`: 108 types and 942 members across the two
  shipping assemblies against `eng/api-baseline`. The committed surface adds
  `NestedDataExtensions`, `RowOrigin`, `CartesianRowOrigin`, `NestedRowError`, and
  `NestedRowErrorKind`.
- `dotnet run --project examples/FunnySharp.Examples/FunnySharp.Examples.csproj -c Release`:
  prints `FunnySharp examples passed.`
- `dotnet run --project tests/FunnySharp.NestedDataMeasurements/FunnySharp.NestedDataMeasurements.csproj -c Release`:
  the complete comparison passes, then the measurement table above is printed. The final two runs
  produced identical allocation rows.

The focused new-test run alone reports 57 passing tests, 0 failed, 0 skipped. The full suite count
above includes those tests; `PerformanceManifestTests` also confirms both new manifest source lists
are sorted and complete.
