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
are `readonly struct` carriers. Construction, dispatch (`Map`, `Bind`, `Ensure`, `Filter`,
`MapError`, `Recover`, `OrElse`), and extraction (`Match`, `TryGetValue`,
`GetValueOr*`) are O(1) branch-and-call operations over struct fields: they allocate nothing on
the heap and never box a contained value type. The zero-allocation observations for the value,
completed-`ValueTask`, and span rows in each topic table are direct measurements of this
foundation. Reference-type payloads are stored as references, not copied; the carriers add no
hidden per-operation cost beyond the selector or predicate the caller supplies.

Fail-fast carriers short-circuit on the first failure: a failed `Result`/`UnitResult` never
invokes the next selector, and `Option` preserves absence without invoking any callback.
`Validation` is the deliberate exception: independent checks accumulate every error, so its
traversal cost is always O(n) selector invocations plus one list per accumulated error group.

## Per-operation characteristics

| Family | Operation | Complexity | Enumeration | Allocation / boxing | Materialization / buffering | Async scheduling |
| --- | --- | --- | --- | --- | --- | --- |
| Option | `Some`, `None`, `FromNullable`, `FromBoolean`, `FromTry` | O(1) | — | Zero on construction (struct carrier) | None | — |
| Option | `Map`, `Bind`, `Filter`, `Select`, `SelectMany` | O(1) + selector | — | Zero beyond the selector's own cost | None; a null selector result becomes `None` | — |
| Option | `Match`, `TryGetValue`, `GetValueOr`, `GetValueOrElse`, `GetValueOrDefault`, `OrElse`, `OrElseWith` | O(1) | — | Zero | None | — |
| Option | `Zip`, `ZipWith` | O(1) | — | Zero (the tuple is a struct field) | None | — |
| Option | `MapAsync`/`BindAsync` (`Task`) | O(1) + awaitable | — | Completed path: the async machinery plus the returned `Task` (measured 216-256 B per call); pending: per continuation | None | Continues on the awaited task's scheduler; no sync-over-async |
| Option | `MapValueAsync`/`BindValueAsync` (`ValueTask`) | O(1) + awaitable | — | Completed path: zero; pending: per continuation | None | Preserves the underlying `ValueTask`; never blocks |
| Option bridges | `ToOption`, `ToNullable`, `GetOption`, `FromTry` parse/dictionary bridges | O(1) (hash lookup for `GetOption`) | — | Zero | None | — |
| Container bridges | `PopOrNone`, `PeekOrNone`, `DequeueOrNone` (stack, queue, priority queue) | O(1); `DequeueOrNone` peeks, validates, then removes | — | Zero | None; the container is mutated only when the returned option is present, which is why `DequeueOrNone` acquires the head twice (peek, then dequeue) | — |
| Result / UnitResult | `Success`, `Failure`, `Map`, `Bind`, `Ensure`, `MapError`, `Recover`, `OrElse`, `Match`, `TryGetValue` | O(1) + selector/predicate | — | Zero beyond caller callbacks | None | — |
| Result | `Result.Try` | O(1) + operation | — | Zero on success; the caught exception is the operation's own allocation | None | — |
| Result / UnitResult | `MapAsync`/`BindAsync` (`Task`) and `*ValueAsync` variants | O(1) + awaitable | — | Completed `Task` path: async machinery plus the returned `Task`; completed `ValueTask` path: zero (measured); pending: per continuation | None | Token forwarded unchanged; no sync-over-async |
| Validation | `Valid`, `Invalid`, `Map`, `MapErrors`, `Recover` | O(1) + callback | — | Zero beyond callbacks | Error groups are shared `IReadOnlyList` values, not copied | — |
| Validation | accumulation (`Apply`, `Combine`, traversal) | O(errors) | — | One wrapper per combined error group | Errors accumulate eagerly in order | — |
| Sequences | `Sequence`/`Traverse` (Option, Result, UnitResult: fail-fast) | O(n) selectors, single pass | Source enumerated exactly once | Values list (capacity-hinted when the source is a collection) | Materializes one `IReadOnlyList` result; fails fast, no partial list | — |
| Sequences | `Traverse` (Validation) | O(n) selectors, single pass | Source enumerated exactly once | Values list plus one error list | Materializes values or the full ordered error list | — |
| Sequences | located `Traverse` overloads (experimental, FS0017) | O(n) selectors, single pass | Once | Values list plus `Location` chain per selector call | Same as the non-located form | — |
| Sequences | `SequenceAsync`/`TraverseAsync`/`*ValueAsync` | O(n) awaits, single pass | Source enumerated exactly once | Same as sync plus per-await state machines on pending paths | Same as sync | Sequential awaiting; token forwarded to the enumerator |
| Pipelines | `Choose`, `WhereNotNull`, `Scan` (sync and async) | O(n) per enumeration | Deferred; re-enumerates the source per enumeration | Iterator state machine once per enumeration | No materialization; `Scan` state is per-enumeration | Async forms await sequentially, token forwarded |
| Pipelines | span/memory `Choose`/`Scan` variants | O(n) immediate | Single immediate pass | Zero beyond caller-supplied buffers | Writes into caller-owned storage; respects view lifetimes | — |
| Cardinality | `FirstOrNone`, `SingleOrNone`, `LastOrNone`, `MinOrNone`, `MaxOrNone`, `ElementAtOrNone`, aggregates | O(n) single pass | Once | Zero | No materialization; `SingleOrNone` keeps scanning to reject duplicates | — |
| Cardinality | `NonEmpty`, `NonEmptyAggregate` | O(n) single pass | Once | Zero when empty; one list when non-empty | Materializes only the non-empty proof | — |
| Cardinality | `ZipExact`, `ZipExactOrNone` | O(min(n, m)) + one count check | Each source once | Zero | No materialization; unequal lengths fail without buffering | — |
| Partition | `Partition`, `OptionPartition`, `ResultPartition`, `UnitResultPartition` | O(n) single pass | Once | Two lists, allocated lazily on first element | Materializes both result lists | — |
| Function grammar | `Pipe`, `Tap` | O(1) | — | Zero | None | — |
| Function grammar | `Compose`, `Curry`, `Uncurry`, `Partial`, `Flip` | O(1) per call | — | One composed delegate at construction | None | — |
| Function grammar | `ComposeAsync`/`ComposeValueAsync` | O(1) per call | — | State machine per invocation on pending paths | None | Sequential await, token forwarded |
| Effects | `Effect.From*`, `Map`, `Bind` construction | O(1) | — | Struct wrapper; closures only where the caller captures | Deferred until `RunAsync` | `RunAsync` returns `ValueTask`; synchronous completion is zero-allocation |
| Effects | `EffectResourceExtensions` scopes | O(scope) | — | The `using`-shaped scope; released on success, failure, exception, and cancellation | None | Token forwarded; no fire-and-forget |
| Concurrency | `SelectParallelValueAsync`, `SelectParallelCompletionOrderValueAsync` | O(n/parallelism) | Source enumerated once | Bounded `Channel` buffering; per-item task coordination | Streams results with backpressure; no unbounded queue | Caller's scheduler; linked cancellation; delivery order fixed by the method name |
| Concurrency | parallel traversal | O(n/parallelism) | Once | Results array sized to the source | Materializes ordered values (fail-fast vs. accumulation per carrier) | Bounded fan-out; linked cancellation |
| Concurrency | first-success family | O(started work) | Cold effects only | One coordination scope | Drains all started work before returning `Validation` | `TimeProvider` timeouts are cooperative, coordinator-owned |
| State machines | `StateChange.To` | O(outputs) snapshot | — | Params array plus snapshot copy plus read-only view (visible per-step semantics) | Snapshot per change | — |
| State machines | `Then` composition | O(1) per composition; O(steps) evaluation, iterative | — | One composition node per `Then`; pooled scratch; one exact-size output array per evaluation | Single-materialization: outputs concatenate once | — |
| State machines | `OrElse` | O(1) | — | Zero | None | — |
| State machines | `Replay` | O(events) | History enumerated once | One output list | One final outputs array | — |
| Optics | `Lens`, `Optional` `Get`/`Set` | O(1) + caller delegate | — | Zero beyond the caller's delegates | None | — |
| HTTP | `FunnySharp.AspNetCore` mapping | O(1) mapping | — | Measured only through a representative application pipeline | None | Runs on the ASP.NET Core request pipeline |

The zero and nonzero statements above are enforced where measured by the committed observation
rows and their budgets; families marked as excluded (HTTP mapping, unmeasured variants such as
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

Read the generated table below by row, not by column header: the baseline columns carry each
scenario's `Direct` reference method, and the columns headed `FunnySharp` carry the method named in
the row label - for competitor rows that is the competitor library's method, not FunnySharp's.
Attribute every number to the row's named method, never to the column header.

<!-- performance-table:start competitor-comparison -->
| Scenario | Baseline mean | FunnySharp mean | Ratio | Baseline allocation | FunnySharp allocation |
| --- | ---: | ---: | ---: | ---: | ---: |
| Map - absent - FSharpCoreMapAbsent | N/A | 0.230 ns | N/A | 0 B | 0 B |
| Map - absent - FunckyMapAbsent | N/A | 6.517 ns | N/A | 0 B | 24 B |
| Map - absent - FunnySharpMapAbsent | N/A | 0.834 ns | N/A | 0 B | 0 B |
| Map - absent - LanguageExtMapAbsent | N/A | 0.601 ns | N/A | 0 B | 0 B |
| Map - present - FSharpCoreMapPresent | N/A | 2.645 ns | N/A | 0 B | 0 B |
| Map - present - FunckyMapPresent | N/A | 8.870 ns | N/A | 0 B | 24 B |
| Map - present - FunnySharpMapPresent | N/A | 2.325 ns | N/A | 0 B | 0 B |
| Map - present - LanguageExtMapPresent | N/A | 2.326 ns | N/A | 0 B | 0 B |
| Value-or-fallback - absent - FSharpCoreValueOrFallbackAbsent | N/A | N/A | N/A | 0 B | 0 B |
| Value-or-fallback - absent - FunckyValueOrFallbackAbsent | N/A | N/A | N/A | 0 B | 0 B |
| Value-or-fallback - absent - FunnySharpValueOrFallbackAbsent | N/A | N/A | N/A | 0 B | 0 B |
| Value-or-fallback - absent - LanguageExtValueOrFallbackAbsent | N/A | 0.319 ns | N/A | 0 B | 0 B |
| Value-or-fallback - present - FSharpCoreValueOrFallbackPresent | 0.257 ns | N/A | N/A | 0 B | 0 B |
| Value-or-fallback - present - FunckyValueOrFallbackPresent | 0.257 ns | 0.339 ns | 1.32x | 0 B | 0 B |
| Value-or-fallback - present - FunnySharpValueOrFallbackPresent | 0.257 ns | 0.398 ns | 1.55x | 0 B | 0 B |
| Value-or-fallback - present - LanguageExtValueOrFallbackPresent | 0.257 ns | 0.141 ns | 0.55x | 0 B | 0 B |
| Construction and inspection - failure - CSharpFunctionalExtensionsConstructionInspectionFailure | N/A | 0.576 ns | N/A | 0 B | 0 B |
| Construction and inspection - failure - FSharpCoreConstructionInspectionFailure | N/A | N/A | N/A | 0 B | 0 B |
| Construction and inspection - failure - FunnySharpConstructionInspectionFailure | N/A | N/A | N/A | 0 B | 0 B |
| Construction and inspection - failure - LanguageExtConstructionInspectionFailure | N/A | 5.258 ns | N/A | 0 B | 24 B |
| Construction and inspection - success - CSharpFunctionalExtensionsConstructionInspectionSuccess | N/A | 0.831 ns | N/A | 0 B | 0 B |
| Construction and inspection - success - FSharpCoreConstructionInspectionSuccess | N/A | N/A | N/A | 0 B | 0 B |
| Construction and inspection - success - FunnySharpConstructionInspectionSuccess | N/A | N/A | N/A | 0 B | 0 B |
| Construction and inspection - success - LanguageExtConstructionInspectionSuccess | N/A | 8.099 ns | N/A | 0 B | 24 B |
| Fail-fast pipeline - failure - CSharpFunctionalExtensionsFailFastPipelineFailure | N/A | 6.590 ns | N/A | 0 B | 0 B |
| Fail-fast pipeline - failure - FSharpCoreFailFastPipelineFailure | N/A | 15.660 ns | N/A | 0 B | 0 B |
| Fail-fast pipeline - failure - FunnySharpFailFastPipelineFailure | N/A | 1.870 ns | N/A | 0 B | 0 B |
| Fail-fast pipeline - failure - LanguageExtFailFastPipelineFailure | N/A | 50.399 ns | N/A | 0 B | 48 B |
| Fail-fast pipeline - success - CSharpFunctionalExtensionsFailFastPipelineSuccess | 0.355 ns | 12.996 ns | 36.58x | 0 B | 0 B |
| Fail-fast pipeline - success - FSharpCoreFailFastPipelineSuccess | 0.355 ns | 11.904 ns | 33.51x | 0 B | 0 B |
| Fail-fast pipeline - success - FunnySharpFailFastPipelineSuccess | 0.355 ns | 10.505 ns | 29.57x | 0 B | 0 B |
| Fail-fast pipeline - success - LanguageExtFailFastPipelineSuccess | 0.355 ns | 41.975 ns | 118.15x | 0 B | 48 B |
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

## Reproduction

The committed observation in `eng/performance/baseline.json` (main suite) and
`eng/performance/competitor-baseline.json` (competitor suite) records the environment,
candidate commit, and per-row results. To reproduce a measurement run:

```powershell
# Main suite (receipts land in the results directory; verify against the manifest)
dotnet run --project benchmarks/FunnySharp.Benchmarks/FunnySharp.Benchmarks.csproj -c Release -- --filter '*' --artifacts <results-path>
pwsh -NoProfile -File eng/Verify-Performance.ps1 -ReceiptDirectory <results-path>

# Competitor suite (isolated project, pinned competitor packages)
dotnet run --project benchmarks/FunnySharp.CompetitorBenchmarks/FunnySharp.CompetitorBenchmarks.csproj -c Release -- --filter '*' --artifacts <competitor-results-path>
pwsh -NoProfile -File eng/Verify-Performance.ps1 -ManifestPath eng/performance/competitor-baseline.json -ReceiptDirectory <competitor-results-path>
```

Both verifiers check every row against the current policy fingerprint, the recorded environment,
and the committed budgets before an observation can be approved.
