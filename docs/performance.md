# Performance

This page is the consolidated performance guidance for FunnySharp's stable surface: how each
carrier family is built, what each performance-relevant operation costs, which comparisons back
those statements, and which regressions block a release. Each topic guide carries its own
generated measurement table for its benchmark family; this page links them and adds the
cross-cutting characteristics that do not fit a single table row.

## How to read the evidence

- **Allocation is blocking evidence.** Every measured operation has an allocation budget in the
  tracked manifest ([`eng/performance/baseline.json`](../eng/performance/baseline.json)); a
  release observation that exceeds a budget, or regresses a zero-allocation row to nonzero, fails
  verification. Budgets are set from measured bytes plus roughly 25% headroom, rounded up.
- **Timing is directional evidence.** Means come from BenchmarkDotNet `ShortRunJob` runs on the
  recorded environment (operating system, architecture, SDK, runtime, JIT, GC configuration in
  the committed observation). They are reproducible on that environment, not a promise for
  arbitrary hardware. A timing state of `below-resolution` or `unavailable` never produces a
  ratio.
- **Raw handwritten BCL or LINQ is the honest reference.** Every comparison group pairs
  semantically equivalent paths: the direct baseline does the same work with plain C#, and each
  FunnySharp candidate must justify any remaining cost with visible semantics (a typed carrier,
  an immutable snapshot, a safe traversal) rather than winning by picking a weak baseline.
- **Unsupported blanket claims are prohibited.** No zero-cost, always-faster, or faster-IO
  claims appear here or in the API documentation; every numeric statement traces to a committed
  observation row.

## Carrier foundation

`Option<T>`, `Result<TValue, TError>`, `UnitResult<TError>`, and `Validation<TValue, TError>`
are `readonly struct` carriers. Their representation does not itself allocate, but individual
operations can allocate library-owned arrays, wrappers, delegates, iterators, or collections.
Converting a carrier to `object` or an interface can box it. Callback, payload, comparer,
enumerator, and exception costs must be added to the library work below. A measured zero applies
to that exact successful execution path, not every member or construction path in a family.

Fail-fast carriers short-circuit on the first failure: a failed `Result`/`UnitResult` never
invokes the next selector, and `Option` preserves absence without invoking any callback.
`Validation` is the deliberate exception: independent checks accumulate every error. Traversal
visits every input and copies every retained error into its ordered error list. Error factories,
error mapping, and combination have distinct array and read-only-wrapper costs.

## Per-operation characteristics

The bounds describe library work for initialized values and valid arguments, excluding caller
work and waits. Let n be reached source items, e accumulated errors, p the configured concurrency
bound, and k retained parallel outcomes. Receiver-dependent operations and interface enumeration
do not acquire a universal zero-allocation or constant-time guarantee from these extensions.

| Family | Operation | Complexity | Enumeration | Allocation / boxing | Materialization / buffering | Async scheduling |
| --- | --- | --- | --- | --- | --- | --- |
| Option | `Some`, `None`, `FromNullable`, `FromBoolean`, `FromTry` | O(1) | — | Zero on construction (struct carrier) | None | — |
| Option | `Map`, `Bind`, `Filter`, `Select`, `SelectMany` | O(1) + selector | — | Zero beyond the selector's own cost | None; a null selector result becomes `None` | — |
| Option | `Match`, `TryGetValue`, `GetValueOr`, `GetValueOrElse`, `GetValueOrDefault`, `OrElse`, `OrElseWith` | O(1) | — | Zero | None | — |
| Option | `Zip`, `ZipWith` | O(1) | — | Zero (the tuple is a struct field) | None | — |
| Option | `MapAsync`/`BindAsync` (`Task`) | O(1) + awaitable | — | Completed path: the async machinery plus the returned `Task` (measured 216-256 B per call); pending: per continuation | None | Continues on the awaited task's scheduler; no sync-over-async |
| Option | `MapValueAsync`/`BindValueAsync` (`ValueTask`) | O(1) + awaitable | — | Completed path: zero; pending: per continuation | None | Preserves the underlying `ValueTask`; never blocks |
| Option bridges | `ToOption`, `ToNullable`, `FromTry` | O(1) carrier work plus the supplied operation | — | No library-owned result buffer | None | — |
| Container bridges | `GetOption`, `RemoveOrNone`, `IndexOfOrNone` | Receiver-dependent hashing/equality/search; `IList<T>.IndexOf` can scan O(n) | — | No library-owned search buffer | Dictionary removal validates the selected value before mutation | — |
| Container bridges | `PopOrNone`, `PeekOrNone`, `DequeueOrNone` | Stack/Queue head operations O(1); PriorityQueue removal O(log n), plus comparer work | — | No library-owned result buffer | PriorityQueue dequeue peeks and validates before removing the root | — |
| Result | `Success`, `Failure`, `Map`, `Bind`, `Ensure`, `MapError`, `Recover`, `RecoverWith`, `Match`, `TryGetValue` | O(1) + selector/predicate | — | No library-owned payload buffer | None | — |
| UnitResult | `Success`, `Failure`, `Bind`, `Ensure`, `MapError`, `RecoverWith`, `ToResult`, `Match`, `TryGetError` | O(1) + callback | — | No success payload or success-value buffer | `ToResult` explicitly constructs a value-producing result | — |
| Result | `Result.Try` | O(1) + operation | — | Zero on success; the caught exception is the operation's own allocation | None | — |
| Result / UnitResult | `MapAsync`/`BindAsync` (`Task`) and `*ValueAsync` variants | O(1) + awaitable | — | Completed `Task` path: async machinery plus the returned `Task`; completed `ValueTask` path: zero (measured); pending: per continuation | None | Token forwarded unchanged; no sync-over-async |
| Validation | `Valid`, `Map`, valid `MapErrors` | O(1) + callback | — | No new error buffer; invalid `Map` shares the existing errors | None | — |
| Validation | `Invalid`, `InvalidMany` | O(1) for one error; O(e) for a supplied error sequence | `InvalidMany` enumerates once | Owned error array and read-only wrapper | Snapshots all supplied errors | — |
| Validation | Invalid `MapErrors` | O(e) callbacks and writes | Visits every stored error | New error array and read-only wrapper | Materializes all mapped errors | — |
| Validation | `Zip`, `Apply` | O(e) when combining invalid error groups; O(1) + callback when valid | Copies combined errors in operand order | Combined invalid groups create an array and wrapper; a single invalid group can be shared | Eager accumulation; valid operands have no error buffer | — |
| Sequences | `Sequence`/`Traverse` (Option, Result: fail-fast) | O(n) + selectors | One pass, stops on first failure | Lazily created values list, capacity-hinted where available; success wrapper | Successful values materialized; no partial collection returned on failure | — |
| Sequences | `Sequence`/`Traverse` (UnitResult: fail-fast) | O(n) + selectors | One pass, stops on first failure | No success-value list | Returns only success or the first error | — |
| Sequences | `Traverse` (Validation) | O(n + e) + selectors | Source enumerated once; all selector errors visited | Successful prefix list is discarded after the first error; ordered error list and wrapper retained | Materializes values on success or all errors on invalid result | — |
| Sequences | Keyed `Traverse` (Option, Result, Validation) | Receiver/comparer-dependent dictionary insertion plus reached-item/error work | Source enumerated once | Successful dictionary and read-only wrapper; Validation also retains errors | Preserves exposed or explicitly supplied equality; UnitResult creates no dictionary | — |
| Sequences | located `Traverse` overloads (experimental, FS0017) | Carrier-specific traversal work plus location construction | Once | Location chain per reached item plus the corresponding carrier buffers | Same outcome shape as the non-located form | — |
| Sequences | `SequenceAsync`/`TraverseAsync`/`TraverseValueAsync` | Same carrier-specific traversal work, plus awaits | One source pass | Same carrier buffers plus asynchronous machinery where needed | Same carrier-specific materialization | Sequential awaiting; token forwarded to the enumerator |
| Pipelines | `Choose`, `WhereNotNull`, `Scan` (sync and async) | O(n) per enumeration | Deferred; re-enumerates the source per enumeration | Iterator state machine once per enumeration | No materialization; `Scan` state is per-enumeration | Async forms await sequentially, token forwarded |
| Pipelines | span/memory `SelectTo`, `WhereTo`, `ChooseTo`, `SelectInPlace`, `WhereInPlace` | O(n) immediate | Single immediate pass | Caller-owned storage; no library-owned output buffer | Writes or compacts the destination prefix; no span `Scan` member | — |
| Cardinality | `FirstOrNone`, `SingleOrNone`, `LastOrNone`, `MinOrNone`, `MaxOrNone`, `ElementAtOrNone` | Member/receiver-dependent, at most a source scan plus predicate/comparer work | One pass where enumeration is needed | No library-owned result buffer; enumerator costs depend on the source | `SingleOrNone` stops at the second qualifying item; unique/no-match predicate cases can scan all inputs | — |
| NonEmpty | `ToNonEmptyOrNone`, `First`, `Rest`, `Count`, `Aggregate` | Construction and fold O(n); access O(1) | Construction consumes one source pass; fold visits stored rest | Construction stores remaining items in a list | Retains first/rest; seedless fold needs no output buffer | — |
| NonEmpty | `ToReadOnlyList` | O(n) copy | Visits retained items | Fresh list and read-only wrapper on each call | Materializes a new ordered collection | — |
| Cardinality | `ZipExact`, `ZipExactOrNone` | O(n + m) for both full input lengths | Each source once; the longer side is drained | Pair list allocated even on mismatch; success also creates a read-only wrapper | Stores up to min(n, m) common-prefix pairs and reports exact counts | — |
| Partition | Predicate and Result `Partition` | O(n) | Once | Up to two lazy lists and corresponding read-only views, plus a reference-type partition record | Stores each non-empty side in source order | — |
| Partition | Option `Partition` | O(n) | Once | Lazy present-value list/view and partition record | Stores present values; counts absences | — |
| Partition | UnitResult `Partition` | O(n) | Once | Lazy error list/view and partition record | Stores errors; counts successes; no success-value list | — |
| Function grammar | `Pipe`, `Tap` | O(1) | — | Zero | None | — |
| Function grammar | `Compose`, `Curry`, `Uncurry`, `Partial`, `Flip` | O(1) per call | — | One composed delegate at construction | None | — |
| Function grammar | `ComposeAsync`/`ComposeValueAsync` | O(1) per call | — | State machine per invocation on pending paths | None | Sequential await, token forwarded |
| Effects | `Effect.From*`, `Map`, `Bind`, `Provide`, `WithEnvironment` construction | O(1) construction work | — | Library-owned runner closures retain values, source effects, environments, or callbacks where needed; a supplied runner can also be retained directly | Execution deferred until `RunAsync`; construction is a separate cost | A measured completed run of a preconstructed effect does not establish zero-cost construction |
| Effects | `EffectResourceExtensions` scopes | Constant coordination plus acquisition/use/disposal work | — | Construction captures the resource/use effects; pending execution has async machinery | Releases the acquired resource once, including failure/cancellation | Token forwarded; natural C# disposal-fault precedence |
| Concurrency | `SelectParallelValueAsync`, `SelectParallelCompletionOrderValueAsync` | O(n * p) conservative bookkeeping bound plus source/selector work | Source enumerated once | Channel, admission window, and started-work list bounded by p; per-item tasks/observers | Streams with backpressure; indexed list lookup/removal can scan the window | BCL async coordination; linked cancellation; elapsed parallelism is not an algorithmic bound |
| Concurrency | Parallel traversal | O(n * p + k log k + e) coordination/finalization plus source/selector work | Once | Active-work list bounded by p, growing indexed outcome list, final carrier-specific buffers | Sorts retained outcomes by index before materializing values or errors | Bounded admission and cancel-and-drain |
| Concurrency | `FirstSuccessAsync` | O(n) finite snapshot/final ordered pass plus O(started work) indexed completion accounting | Cold inputs snapshotted once | O(n) snapshot/outcome storage and coordination bounded by p (default 32) | Stops admission after selection and drains candidates/observers/callbacks; returns Validation or propagates independent faults | Cooperative TimeProvider timeout is not a hard cleanup deadline; 32 is compatibility policy, not measured optimal parallelism |
| State machines | `StateChange.To` | O(outputs) snapshot | — | Params array plus snapshot copy plus read-only view (visible per-step semantics) | Snapshot per change | — |
| State machines | `Then` composition | O(1) per composition; O(steps) evaluation, iterative | — | One composition node per `Then`; pooled scratch; one exact-size output array per evaluation | Single-materialization: outputs concatenate once | — |
| State machines | `OrElse` | O(1) | — | Zero | None | — |
| State machines | `Replay` | O(events) | History enumerated once | One output list | One final outputs array | — |
| Optics | `Lens`, `Optional` `Get`/`Set` | O(1) + caller delegate | — | Zero beyond the caller's delegates | None | — |
| HTTP | `FunnySharp.AspNetCore` mapping | O(1) mapping | — | Measured per outcome shape (0-168 B per call; see [`aspnet-core.md`](aspnet-core.md)) | None | Runs on the ASP.NET Core request pipeline |

The zero and nonzero statements above are enforced where measured by the committed observation
rows and their budgets; families marked as excluded (unmeasured variants such as
`Pipe`/`Tap`/`Curry`, optics construction, and real resource I/O) carry no numeric claim
until a benchmark exists.

## Regression policy

- **Allocation regressions block independently of timing.** The verifier fails any observation
  row above its budget, any zero-allocation row that regresses to nonzero, and any missing,
  non-numeric, or semantically mismatched row. Allocation evidence is deterministic, so it gates
  every measurement run.
- **Complexity regressions block independently of timing.** The complexity column above is part
  of the stable contract: a stable path whose asymptotic behavior regresses (a new quadratic
  copy, a second enumeration, an unbounded buffer) is a defect even when hosted timing looks
  acceptable. `StateTransition.Then` is the worked example: its left-associated chain was
  redesigned to single-materialization composition because the recorded quadratic behavior
  (85x-457x, up to 187 KB at Count=256) could not remain stable.
- **Throughput regressions become blocking only after the environment is controlled.** Today's
  means are directional on the recorded environment. Making them blocking requires the fixed
  self-hosted runner tracked in [`TODO.md`](../TODO.md): pinned hardware and power settings, a
  pinned runtime, a measured noise floor, and a documented comparison policy. Until then, a
  throughput change is investigated against the directional observation, not automatically
  rejected.
- **Faster-alternative positioning must hold on a controlled environment.** An API positioned as
  a faster alternative must statistically tie or beat the closest high-level baseline on the
  recorded environment, and the raw handwritten reference stays honest rather than weakened to
  manufacture a win.

## Equivalence policy

Every comparison group in the benchmark suite pairs semantically equivalent paths: the same
input carrier values, the same selector/predicate behavior, and the same observable result.
Equivalence is established two ways:

- **Runtime preflight validation.** The benchmark projects validate before measuring that every
  compared path in each scenario returns the same value (pending-`Task`/`ValueTask` transforms
  in the main suite; every scenario in the competitor suite).
- **Recorded justification.** Each benchmark class documents its equivalence contract at the top
  of the file, including where a library lacks a member (for example a separate `Ensure`) and
  how the idiomatic equivalent preserves the paired path's input-to-output semantics.

## Competitor comparisons

These comparisons run only in the isolated, non-packable
[`benchmarks/FunnySharp.CompetitorBenchmarks`](../benchmarks/FunnySharp.CompetitorBenchmarks)
project, which is outside `FunnySharp.slnx` and every release artifact, against pinned
competitor packages. They are performance evidence for how FunnySharp's carriers compare with
comparable functional-programming carriers; they are never API-compatibility evidence or a
compatibility promise. Pinned packages: FSharp.Core 10.1.400, Funcky 3.6.0,
CSharpFunctionalExtensions 3.7.0, language-ext Core 4.4.9.

Read the generated table below by row: baseline columns carry the scenario's `Direct` reference
method, and candidate columns carry the exact method named in the row label. A competitor method
is attributed to that competitor, not to FunnySharp. Neither column is a library-wide average.

<!-- performance-table:start competitor-comparison -->
| Scenario | Baseline mean | Candidate mean | Ratio | Baseline allocation | Candidate allocation |
| --- | ---: | ---: | ---: | ---: | ---: |
| Map - absent - FSharpCoreMapAbsent | N/A | N/A | N/A | 0 B | 0 B |
| Map - absent - FunckyMapAbsent | N/A | 4.169 ns | N/A | 0 B | 24 B |
| Map - absent - FunnySharpMapAbsent | N/A | 0.115 ns | N/A | 0 B | 0 B |
| Map - absent - LanguageExtMapAbsent | N/A | 0.230 ns | N/A | 0 B | 0 B |
| Map - present - FSharpCoreMapPresent | 0.377 ns | 2.857 ns | 7.59x | 0 B | 0 B |
| Map - present - FunckyMapPresent | 0.377 ns | 6.151 ns | 16.33x | 0 B | 24 B |
| Map - present - FunnySharpMapPresent | 0.377 ns | 2.320 ns | 6.16x | 0 B | 0 B |
| Map - present - LanguageExtMapPresent | 0.377 ns | 2.040 ns | 5.42x | 0 B | 0 B |
| Value-or-fallback - absent - FSharpCoreValueOrFallbackAbsent | N/A | N/A | N/A | 0 B | 0 B |
| Value-or-fallback - absent - FunckyValueOrFallbackAbsent | N/A | N/A | N/A | 0 B | 0 B |
| Value-or-fallback - absent - FunnySharpValueOrFallbackAbsent | N/A | N/A | N/A | 0 B | 0 B |
| Value-or-fallback - absent - LanguageExtValueOrFallbackAbsent | N/A | N/A | N/A | 0 B | 0 B |
| Value-or-fallback - present - FSharpCoreValueOrFallbackPresent | N/A | N/A | N/A | 0 B | 0 B |
| Value-or-fallback - present - FunckyValueOrFallbackPresent | N/A | 0.390 ns | N/A | 0 B | 0 B |
| Value-or-fallback - present - FunnySharpValueOrFallbackPresent | N/A | N/A | N/A | 0 B | 0 B |
| Value-or-fallback - present - LanguageExtValueOrFallbackPresent | N/A | N/A | N/A | 0 B | 0 B |
| Construction and inspection - failure - CSharpFunctionalExtensionsConstructionInspectionFailure | 0.307 ns | 1.134 ns | 3.70x | 0 B | 0 B |
| Construction and inspection - failure - FSharpCoreConstructionInspectionFailure | 0.307 ns | 0.211 ns | 0.69x | 0 B | 0 B |
| Construction and inspection - failure - FunnySharpConstructionInspectionFailure | 0.307 ns | N/A | N/A | 0 B | 0 B |
| Construction and inspection - failure - LanguageExtConstructionInspectionFailure | 0.307 ns | 3.128 ns | 10.20x | 0 B | 24 B |
| Construction and inspection - success - CSharpFunctionalExtensionsConstructionInspectionSuccess | N/A | 1.142 ns | N/A | 0 B | 0 B |
| Construction and inspection - success - FSharpCoreConstructionInspectionSuccess | N/A | N/A | N/A | 0 B | 0 B |
| Construction and inspection - success - FunnySharpConstructionInspectionSuccess | N/A | N/A | N/A | 0 B | 0 B |
| Construction and inspection - success - LanguageExtConstructionInspectionSuccess | N/A | 4.817 ns | N/A | 0 B | 24 B |
| Fail-fast pipeline - failure - CSharpFunctionalExtensionsFailFastPipelineFailure | 0.282 ns | 4.919 ns | 17.46x | 0 B | 0 B |
| Fail-fast pipeline - failure - FSharpCoreFailFastPipelineFailure | 0.282 ns | 3.630 ns | 12.88x | 0 B | 0 B |
| Fail-fast pipeline - failure - FunnySharpFailFastPipelineFailure | 0.282 ns | 1.865 ns | 6.62x | 0 B | 0 B |
| Fail-fast pipeline - failure - LanguageExtFailFastPipelineFailure | 0.282 ns | 46.833 ns | 166.23x | 0 B | 48 B |
| Fail-fast pipeline - success - CSharpFunctionalExtensionsFailFastPipelineSuccess | N/A | 13.857 ns | N/A | 0 B | 0 B |
| Fail-fast pipeline - success - FSharpCoreFailFastPipelineSuccess | N/A | 5.865 ns | N/A | 0 B | 0 B |
| Fail-fast pipeline - success - FunnySharpFailFastPipelineSuccess | N/A | 9.082 ns | N/A | 0 B | 0 B |
| Fail-fast pipeline - success - LanguageExtFailFastPipelineSuccess | N/A | 32.496 ns | N/A | 0 B | 48 B |
<!-- performance-table:end competitor-comparison -->

Absence is represented differently across these libraries (struct carriers for FunnySharp,
Funcky, and language-ext; a nullable reference carrier for FSharp.Core), and the compared
operations are each library's idiomatic equivalent. Where a library has no separate ensure
member, the idiomatic equivalent fuses the ensure check into the bind step with the same
predicate, error, and final value; the runtime preflight validates that every path returns the
same result.

Read this table through the allocation columns first: most single-carrier operations and the raw
direct baselines measure below the recorded environment's resolution, so the discriminating
evidence is which carrier allocates (zero for FunnySharp, FSharp.Core, and CSharpFunctionalExtensions
in these scenarios; per-operation bytes for Funcky's and language-ext's carriers) and which
operation stays zero-allocation through a multi-step pipeline.

## Topic measurement tables

Each topic guide carries the generated measurement table for its benchmark family:

- [`option.md`](option.md) — option construction, mapping, bridges, and async transforms.
- [`result.md`](result.md) — result construction, fail-fast pipelines, exception boundaries,
  and async transforms.
- [`validation.md`](validation.md) — traversal and accumulation.
- [`function-composition.md`](function-composition.md) — composed and curried delegates.
- [`data-pipelines.md`](data-pipelines.md) — sequence, span, and async-stream pipelines.
- [`collections.md`](collections.md) — cardinality-safe collection operations.
- [`effects.md`](effects.md) — effect construction, composition, and `RunAsync`.
- [`concurrency.md`](concurrency.md) — bounded parallel mapping, parallel traversal, and
  first-success coordination.
- [`immutable-updates.md`](immutable-updates.md) — lens and optional updates.
- [`state-machines.md`](state-machines.md) — state transitions, composition, and replay.
- [`aspnet-core.md`](aspnet-core.md) — carrier-to-`IResult` mapping for successful, failed,
  absent, and no-value outcomes.

## Reproduction

The committed observation in `eng/performance/baseline.json` (main suite) and
`eng/performance/competitor-baseline.json` (competitor suite) records the environment and the per-row
results, and the candidate commit the measurements came from when the recording run supplies it. The
release path exports `FUNNYSHARP_CANDIDATE_COMMIT` before the benchmark run
(`eng/harness/ReleaseRun.fs`); a local recording that leaves it unset writes `"candidateCommit": null`
and the observation then names no commit. To reproduce a measurement run:

```bash
# Main suite (receipts land in the results directory; verify against the manifest).
# Export FUNNYSHARP_CANDIDATE_COMMIT so the receipts, and the observation applied from them,
# name the commit the measurements came from - the release path exports it the same way.
FUNNYSHARP_CANDIDATE_COMMIT=$(git rev-parse HEAD) dotnet run --project benchmarks/FunnySharp.Benchmarks/FunnySharp.Benchmarks.csproj -c Release -- --filter '*' --artifacts <results-path>
dotnet fsi build.fsx -- -p verify-performance -ReceiptDirectory <results-path>

# Competitor suite (isolated project, pinned competitor packages)
dotnet run --project benchmarks/FunnySharp.CompetitorBenchmarks/FunnySharp.CompetitorBenchmarks.csproj -c Release -- --filter '*' --artifacts <competitor-results-path>
dotnet fsi build.fsx -- -p verify-performance -ManifestPath eng/performance/competitor-baseline.json -ReceiptDirectory <competitor-results-path>
```

Both verifiers check every row against the current policy fingerprint, the recorded environment,
and the committed budgets before an observation can be approved.
