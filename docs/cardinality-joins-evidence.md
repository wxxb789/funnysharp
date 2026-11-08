# Strict Cardinality Join Consumer Evidence

This record supports the maintainer's judgment for
[Goal 0035](goals/archive/0035-goal.md): whether an order-to-customer enrichment consumer can
require exactly one matching customer per order, receive structured duplicate-key and missing-key
failures with real positions, preserve source order and the output-count relationship, and whether
the safer relation contract materially simplifies complete application code.

Source identity is the Goal 0035 working tree on top of commit `a36c0b7`; the landing commit
becomes the durable identity. Measurements were taken on this host:

- Ubuntu 24.04.5 LTS, Linux 7.0.0-1017-azure, x64.
- .NET SDK 10.0.401, runtime 10.0.12, x64; Release build.
- Date: 2026-11-08 local / 2026-11-07 UTC.

Allocation is the blocking comparison; timing is directional on this shared host.

## Compared Consumers

The consumers run the same order-to-customer rules over the same fixtures. The models, fixtures,
probe, and comparison harness are shared and are not charged to any consumer.

| Version | File | Raw lines | Shape |
| --- | --- | ---: | --- |
| Ordinary reference | [OrdinaryJoinConsumer.cs](../tests/Shared/JoinComparison/OrdinaryJoinConsumer.cs) | 18 | `ToDictionary` plus one indexed lookup per order; throws on duplicate, missing, or null keys and carries no positions. |
| Hand-indexed whole join | [HandIndexedJoinConsumer.cs](../tests/Shared/JoinComparison/HandIndexedJoinConsumer.cs) | 74 | Manual dictionary, first-position dictionary, ordered error records, all-or-nothing output. |
| Hand-indexed streaming | [HandIndexedStreamingConsumer.cs](../tests/Shared/JoinComparison/HandIndexedStreamingConsumer.cs) | 67 | Manual dictionary plus per-key duplicate positions and one outcome per order. |
| FunnySharp consumer | [FunnySharpJoinConsumer.cs](../tests/Shared/JoinComparison/FunnySharpJoinConsumer.cs) | 47 | `JoinExactlyOne` and `JoinExactlyOneResults` plus diagnostic mapping. |

Shared definitions are [JoinModels.cs](../tests/Shared/JoinComparison/JoinModels.cs) (54 lines),
[JoinFixtures.cs](../tests/Shared/JoinComparison/JoinFixtures.cs) (272 lines),
[JoinComparison.cs](../tests/Shared/JoinComparison/JoinComparison.cs) (273 lines),
[ReductionProbes.cs](../tests/Shared/FallibleReduction/ReductionProbes.cs) (169 lines), and
[ValidationUnwrap.cs](../tests/Shared/ValidationUnwrap.cs) (12 lines). The source implementations
are [LookupError.cs](../src/FunnySharp/LookupError.cs) (229 lines),
[LookupErrorKind.cs](../src/FunnySharp/LookupErrorKind.cs) (23 lines),
[LookupIndex.cs](../src/FunnySharp/LookupIndex.cs) (393 lines),
[UniqueLookupIndex.cs](../src/FunnySharp/UniqueLookupIndex.cs) (66 lines), and
[LookupExtensions.cs](../src/FunnySharp/LookupExtensions.cs) (419 lines). Raw lines include braces
and comments; this is not a LOC target. The 47-line FunnySharp file contains both the buffered and
streaming consumer helpers; the whole-join method itself is fewer lines than the hand-indexed
error bookkeeping.

## Equivalence

`JoinComparisonVerifier.Verify` runs fourteen fixtures through the fixed expected report, the
hand-indexed consumer, and the FunnySharp consumer:

`empty`, `unique success`, `repeated source keys`, `missing middle row`, `empty lookup`,
`duplicate matched`, `duplicate unused`, `case-insensitive duplicate`, `ordinal contrast`,
`three occurrences`, `combined duplicate and missing`, `null source key`, `empty string key`, and
`no orders`. A separate comparison covers a null lookup key beside a valid matching key.

The comparison asserts:

- fixed expected outputs and fixed expected failure kinds, keys, and positions for every fixture;
- hand-indexed and FunnySharp buffered-report equality, including deterministic error order;
- ordinary-consumer output equality on success fixtures, and the ordinary form's BCL exception on
  failure fixtures (it cannot produce the structured report without becoming the hand-indexed
  consumer);
- hand-indexed and FunnySharp streaming-outcome equality for every fixture;
- exact enumerator creation, `MoveNext`, `Current`, and `Dispose` counts for both the lookup and
  source sequences;
- the buffered combine ledger and the streaming per-row combine ledger;
- the unused-duplicate scope difference: the buffered join rejects a complete-input duplicate even
  when no source row uses the key, while the streaming join reports no duplicate for that row.

The comparison is called by
[JoinComparisonTests.cs](../tests/FunnySharp.Tests/JoinComparisonTests.cs) and by the measurement
harness; a disagreement fails both the test run and the measurement run. Focused tests in
[LookupIndexTests.cs](../tests/FunnySharp.Tests/LookupIndexTests.cs),
[JoinTests.cs](../tests/FunnySharp.Tests/JoinTests.cs), and
[JoinStreamingTests.cs](../tests/FunnySharp.Tests/JoinStreamingTests.cs) pin the boundaries that
the shared fixtures do not: null lookup keys, null indexed values, argument nullness, comparer
exposure, mutable-value visibility, key changes after construction, early streaming disposal,
source/selector/combine exceptions, and repeated calls.

## Fixed Failure Locations

The fixed expectations demonstrate that a consumer sees actionable positions rather than a generic
exception:

| Scenario | Expected failure |
| --- | --- |
| missing middle row | `MissingKey 'C-9'` at source 1 |
| empty lookup | `MissingKey 'C-1'` at source 0 |
| duplicate matched | `DuplicateKey 'C-2'` at lookup 2, first at lookup 1 |
| duplicate unused | duplicate at lookup 2 first at lookup 1, plus the missing row at source 1 |
| three occurrences | duplicate at lookup 2 and lookup 3, both first at lookup 1 |
| combined duplicate and missing | duplicate first, then missing (lookup errors precede source errors) |
| case-insensitive duplicate | complete-input failure reports the first spelling `C-1`; the matched streaming row reports the queried spelling `c-1` |
| ordinal contrast | no duplicate under `Ordinal`; `c-1` matches the distinct `c-1` entry |
| null source key | `NullKey` at source 0 with no lookup position |

## Semantic Behavior Ledger

The following behavior is directly asserted in the focused tests and the shared comparison:

| Scenario | Observed behavior |
| --- | --- |
| empty lookup source | valid empty `LookupIndex`; `DistinctCount` and `EntryCount` are 0 |
| empty source with a nonempty lookup | valid empty buffered output; streaming yields nothing |
| empty lookup with source rows | one `MissingKey` per source row; streaming yields one failure per row |
| empty string key | an ordinary key; groups, matches, and duplicate-checks normally |
| repeated source keys | one output per source row in source order |
| repeated lookup key, buffered | complete-input failure with every occurrence after the first; no outputs |
| repeated lookup key, streaming | one `DuplicateKey` outcome for the matching row; no extra output row |
| repeated lookup key, unused by the source | buffered failure; streaming rows not using it are unaffected |
| non-default equality | `OrdinalIgnoreCase` groups case variants and detects case-only duplicates; `Ordinal` keeps them distinct; the index exposes the exact comparer |
| dictionary-pair input | the index uses the explicitly supplied comparer, not an opaque dictionary policy |
| null lookup key | `NullKey` with the lookup position; no null-key entry is retained |
| null source key | `NullKey` with the source-row position; buffered join invalid, streaming row failure |
| present null value | retained by `GetMany` and `GetExactlyOne`; `GetZeroOrOne`/`GetOption` reject it like the container bridges |
| null query key | eager `ArgumentNullException` from every keyed read |
| source lookup single enumeration | the buffered join enumerates the lookup source once and the source once |
| streaming deferral | no source enumeration until the first consumer step |
| streaming re-enumeration | each consumer enumeration reads the source again; the lookup index is not re-enumerated |
| early streaming disposal | later rows are not requested; the source enumerator is disposed |
| buffered combine ledger | combine runs only for a unique match and only before the first recorded error; later rows are still checked for null/missing |
| streaming combine ledger | combine runs for every successful row, including after an earlier failure |
| complete-input duplicate | combine does not run at all because the index is invalid before probing |
| output count | buffered success count equals source count; streaming outcome count equals source count including failures |
| Cartesian expansion | duplicate lookup keys never increase the output count; `GetMany` is the explicit expansion path |
| mutable value | a later value mutation is visible through the index; a later key change is not observed |
| source/selector/combine exception | original exception propagates; acquired enumerators are disposed; no `LookupError` conversion |
| duplicate lookup key and null source key together | error order is lookup-source order followed by source order |

## Consumption Ledger

| Operation | Lookup `GetEnumerator` | Lookup `MoveNext` | Lookup `Current` | Lookup `Dispose` | Source `GetEnumerator` | Source `MoveNext` | Source `Current` | Source `Dispose` |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Buffered join, 2 lookup rows, 3 source rows | 1 | 3 | 2 | 1 | 1 | 4 | 3 | 1 |
| Streaming join, first enumeration | 1 (index build) | 3 | 2 | 1 | 1 | 4 | 3 | 1 |
| Streaming join, second enumeration | 1 (unchanged) | 3 (unchanged) | 2 (unchanged) | 1 (unchanged) | 2 | 8 | 6 | 2 |

The observable consequence is that the buffered join does not re-enumerate either input for index
validation or output construction, and the streaming join reuses its prebuilt index while asking
the source for its items on each consumer enumeration.

## Measurements

Command:

```bash
dotnet run --project tests/FunnySharp.JoinMeasurements/FunnySharp.JoinMeasurements.csproj -c Release
```

The harness first runs the complete comparison, then measures precreated fixtures at N=1,024 with
100 iterations after 10 warmup iterations, reporting the minimum of three post-warmup rounds. The
measurement project disables tiered compilation (`System.Runtime.TieredCompilation=false`) so every
row is full-JIT and allocation is attributed consistently. Synchronous allocation uses
`GC.GetAllocatedBytesForCurrentThread`. Allocation is the stable comparison; timing is directional.

| Operation | Allocated B/op | Time ns/op |
| --- | ---: | ---: |
| Index build - ordinary `ToDictionary` | 31,016.4 | 36,746.0 |
| Index build - hand-indexed with positions | 204,432.4 | 60,560.0 |
| Index build - `LookupIndex` | 147,808.4 | 48,395.0 |
| Index build - `UniqueLookupIndex` | 147,832.4 | 52,167.0 |
| Join success - ordinary | 72,032.4 | 78,110.0 |
| Join success - hand-indexed | 253,896.4 | 119,143.0 |
| Join success - `JoinExactlyOne` raw | 197,128.4 | 130,872.0 |
| Join success - `JoinExactlyOne` prebuilt unique | 49,296.4 | 77,136.0 |
| Join missing - hand-indexed | 253,976.4 | 121,880.0 |
| Join missing - `JoinExactlyOne` raw | 189,088.4 | 132,370.0 |
| Join missing - `JoinExactlyOne` prebuilt unique | 41,256.4 | 76,190.0 |
| Join duplicate - hand-indexed | 204,672.4 | 109,977.0 |
| Join duplicate - `JoinExactlyOne` raw | 148,312.4 | 107,530.0 |
| Join many failures - `JoinExactlyOne` raw | 238,144.4 | 151,513.0 |
| Streaming success - hand-indexed collect | 278,296.4 | 113,289.0 |
| Streaming success - `JoinExactlyOneResults` collect | 238,136.4 | 188,629.0 |
| Streaming matched duplicate - hand-indexed collect | 278,456.4 | 119,346.0 |
| Streaming matched duplicate - `JoinExactlyOneResults` collect | 238,120.4 | 190,391.0 |
| Empty - hand-indexed join | 288.4 | 113.0 |
| Empty - `JoinExactlyOne` raw | 272.4 | 180.0 |

The final two runs reproduced every allocation row exactly. Relative to the pre-change
measurement, presizing and the validation consolidation changed the affected rows by:
`LookupIndex` and `UniqueLookupIndex` each -16,512.0 B; raw join success -24,864.0 B and missing
-24,928.0 B; prebuilt unique success -8,352.0 B and missing -8,416.0 B; duplicate raw -49,472.0 B.
The final row-error centralization accounts for 64.0 B in the missing and duplicate rows.
Ordinary, hand-indexed, and empty rows are unchanged. The streaming comparison charges index
construction to both shapes (see the streaming bullet), so those four rows were re-measured after
that correction; their allocation values are the ones shown above.

## Interpretation

- **Index cost is disclosed, not hidden.** An ordinary single dictionary over 1,024 unique
  customers allocates 31,016.4 B. The complete safe hand-indexed form allocates 204,432.4 B because
  it keeps the customer dictionary plus a first-position dictionary. `LookupIndex` allocates
  147,808.4 B: one key dictionary plus one flat entry per value, with no per-key list object and no
  second position table. `UniqueLookupIndex` adds 24 B for the wrapper over the same storage. The
  presized entry list accounts for the lower index allocation relative to the earlier build.
- **The buffered raw join pays for the index it validates.** The ordinary success path allocates
  72,032.4 B but cannot report a duplicate or missing position. The hand-indexed complete safe
  form allocates 253,896.4 B. `JoinExactlyOne` raw allocates 197,128.4 B, 56,768.0 B less than the
  hand-indexed form while producing the same report. The prebuilt unique join allocates 49,296.4 B,
  which isolates output materialization from index construction; presizing its output list is
  reflected in that row.
- **Error storage is proportional to the failures.** A single missing row costs 41,256.4 B in the
  prebuilt path because the index is already present and only one error and no output are
  materialized; the raw path adds the index cost. The hand-indexed missing row allocates
  253,976.4 B because it builds both dictionaries even when it later fails. The duplicate rows show
  the same shape: 204,672.4 B hand-indexed versus 148,312.4 B raw; the invalid raw path no longer
  materializes an unused output list. The many-failure row is 238,144.4 B for 1,024 missing rows,
  about 49,056.0 B more than the one-missing raw row, or roughly 48 B per additional failure. It
  still allocates no output buffer.
- **Streaming is cheaper than the complete hand-indexed shape when both build their index.**
  Charging index construction inside the measured delegate on both sides, collecting 1,024
  successful outcomes allocates 238,136.4 B through `JoinExactlyOneResults` versus 278,296.4 B in
  the hand-indexed streaming consumer, which keeps two dictionaries plus a first- and
  second-position table and a materialized outcome list. The matched-duplicate row is 238,120.4 B
  versus 278,456.4 B. The roughly 40,160 B gap is the hand-indexed consumer's extra position
  dictionaries and separate outcome list; the streaming join builds one index, allocates outcomes
  lazily, and allocates no output buffer of its own.
- **The ordinary form is the cheap happy path, not a complete relation contract.** It allocates
  125,096.0 B less than the raw buffered join on success, but on a duplicate or missing row it
  throws `ArgumentException` or `KeyNotFoundException` with no key, lookup position, or source
  position. Reaching the complete contract in ordinary C# is the 74-line hand-indexed consumer,
  not the 18-line form.
- **Timing is directional.** A second measurement run reproduced every allocation row exactly and
  varied only the directional timings; the allocation order and the fixed reports are the stable
  comparisons. The raw join is not claimed to beat the ordinary form, and no universal speedup is
  claimed.

## Maintainer Judgment

The safer relation contract materially simplifies the *complete* order-to-customer consumer. The
hand-indexed reference needs 74 lines to build two dictionaries, retain duplicate positions, order
index errors before source errors, apply the no-work-after-first-error combine rule, and clear the
output on failure. `JoinExactlyOne` expresses the same contract in one call and the comparison
consumer file is 47 lines including both buffered and streaming helpers and the diagnostic
mapping. The ordinary 18-line form is shorter only because it does not meet the contract: it
throws BCL exceptions without the key or position information the goal requires.

The judgment is conditional, not universal. For a happy-path lookup where duplicates are
impossible or genuinely exceptional, `ToDictionary` plus an indexed read remains shorter and
allocates less; the index and error machinery are not earned there. The buffered join also adds an
index-validation boundary that a caller must handle, and its combine callback can run for earlier
rows before a later missing row is found. The streaming shape is the better fit when partial output
is wanted and matched-key uniqueness is sufficient; it is cheaper than the hand-indexed
streaming form but does not claim complete-input validation. The maintainer should keep the family
only where the structured, position-bearing failure contract is the actual requirement; the
evidence does not support replacing ordinary dictionary lookups generally.

## Guarantees Kept Separate

| Fact | Where it is established | What it does not establish |
| --- | --- | --- |
| Buffered complete-input uniqueness | `ToUniqueLookupIndex`, `RequireUniqueKeys`, raw `JoinExactlyOne` | That a database or stored source is unique |
| Streaming matched-key uniqueness | `GetZeroOrOne`, `GetExactlyOne`, `JoinExactlyOneResults` | That unused duplicate keys were validated |
| Source order and one-output-per-row | Fixed fixtures and output-count assertions | That source items are unique |
| Duplicate never expands output | Fixed duplicate fixtures and `GetMany` boundary | That a caller cannot explicitly expand with `GetMany` |
| Real positions | Fixed failure tables and comparison | That positions identify items across separate inputs |
| Single enumeration | Probe ledger | That a caller-supplied source is replayable |
| Mutable-value visibility | Index lifetime tests | Thread safety under concurrent mutation |
| Explicit equality policy | Comparer exposure and dictionary-policy test | The policy of an unrelated dictionary instance |
| Exceptions propagate | Focused exception tests | Rollback of external work or earlier combine calls |

## Deliberate Non-Guarantees

The evidence does not claim:

- a SQL provider, ORM query translator, DataFrame universe, or database-level integrity;
- a many-to-many or one-to-many join, or any implicit Cartesian expansion;
- complete-input uniqueness from the streaming shape;
- all-or-nothing output from the streaming shape;
- cloning, freezing, snapshotting, or rollback of caller values;
- retry, caching, memoization, or replay of a source;
- conversion of cancellation or programming exceptions into `LookupError` values;
- an async `IAsyncEnumerable` index or join mirror;
- a universal simplification or speedup over ordinary dictionary lookups;
- any Goal 0036 nested reshaping, rectangling, or provenance behavior.

## Verification

```bash
dotnet fsi build.fsx -- -p build
dotnet fsi build.fsx -- -p test
dotnet fsi build.fsx -- -p format
dotnet fsi build.fsx -- -p verify-docs-snippets
dotnet fsi build.fsx -- -p verify-api-baseline
dotnet run --project examples/FunnySharp.Examples/FunnySharp.Examples.csproj -c Release
dotnet run --project tests/FunnySharp.JoinMeasurements/FunnySharp.JoinMeasurements.csproj -c Release
```

Recorded results:

- `dotnet fsi build.fsx -- -p build`: exit 0, 0 warnings, 0 errors across the solution.
- `dotnet fsi build.fsx -- -p test`: 2,717 passed, 0 failed, 0 skipped across
  `FunnySharp.Tests`, `FunnySharp.AspNetCore.Tests`, `FunnySharp.Analyzers.Tests`, and
  `FunnySharp.Harness.Tests`. The previous baseline was 2,658 tests; the 59 new tests are
  `LookupIndexTests`, `JoinTests`, `JoinStreamingTests`, and `JoinComparisonTests`.
- `dotnet fsi build.fsx -- -p format`: exit 0. The F# projects are not C#-formatted, as documented;
  the harness reports the two F# projects as unsupported and checks the C# projects.
- `dotnet fsi build.fsx -- -p verify-docs-snippets`: 68 C# documentation snippets across 13
  primary guides, including the three new `CardinalityJoins` regions.
- `dotnet fsi build.fsx -- -p verify-api-baseline`: 103 types and 906 members across the two
  shipping assemblies against `eng/api-baseline`. The committed surface adds
  `LookupErrorKind`, `LookupError<TKey>`, `LookupIndex<TKey,TValue>`,
  `UniqueLookupIndex<TKey,TValue>`, and `LookupExtensions`.
- `dotnet run --project examples/FunnySharp.Examples/FunnySharp.Examples.csproj -c Release`:
  prints `FunnySharp examples passed.`
- `dotnet run --project tests/FunnySharp.JoinMeasurements/FunnySharp.JoinMeasurements.csproj -c Release`:
  the complete comparison passes, then the measurement table above is printed.

The focused new-test run alone reports 38 passing tests, 0 failed, 0 skipped. The full suite count
above includes those tests and the existing suite; `PerformanceManifestTests` also confirms the
two new manifest source lists are sorted and complete.
