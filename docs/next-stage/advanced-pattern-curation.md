# Advanced-Pattern Curation (Goal 19)

Goal 19 curates FunnySharp's advanced functional patterns against the maintainer-accepted
Goal 14 decisions so that every pattern exposed as stable earns its place in ordinary modern
C# code. This record is the alignment evidence: each decision row from
[`api-decisions.md`](api-decisions.md) and the [product
contract](../product-contract.md) that touches an advanced pattern is mapped to its delivered
state, with the tests, documentation, benchmarks, examples, and comparative call-site
evidence that verify it. The curation mechanism itself — the frozen-surface and
no-smuggling tests, the two-tier stability inventory — is described in §3 so the alignment
survives this goal.

The advanced patterns and their decision sections:

- **AD-6** — async, streaming, concurrency: `ParallelAsyncEnumerableExtensions`,
  `ParallelAsyncSequenceExtensions`, `ConcurrentEffectExtensions`.
- **AD-7** — effects, resources, environment: `Effect`, `Effect<T>`,
  `Effect<TEnvironment,T>`, `EffectResourceExtensions`.
- **AD-8** — state transitions and machines: `TransitionStatus`, `StateChange`,
  `StateTransition`, `StateTransitionExtensions`, `TransitionResult`, `StateMachine`,
  `StateMachineExtensions`.
- **AD-9** — optics and immutability: `Lens`, `Lens<TSource,TFocus>`, `Optional`,
  `Optional<TSource,TFocus>`.
- The capability rows that govern them: G6 (traversal location context, experimental), G7
  (completion-order coordination), G8 (retry layer rejected for this stage), G10
  (`[Experimental]` marker + stability inventory, shared with Goal 23), G17 (public
  `Unit` type rejected).

Scope boundary: the carrier, grammar, and collection families (AD-1 through AD-5) were
delivered and verified by Goals 15–17 with their own evidence, and the HTTP integration
family (AD-10) is owned by Goal 22's vertical slice; none is re-curated here. No advanced
capability row is silently omitted — every row that touches an advanced pattern appears in
§1.

## 1. Decision alignment

| Decision | Decision text (abridged) | Delivered state | Evidence |
| --- | --- | --- | --- |
| AD-6 keep | Bounded, ordered, cancellation-correct parallel mapping and traversal; first-success over cold effects; the race contract is `Validation` (maintainer A-2=B) | Delivered and stable: `SelectParallelValueAsync`, `SelectParallelCompletionOrderValueAsync` (delivery order in the name), `TraverseParallelValueAsync`, three `FirstSuccessAsync` overloads with the documented all-typed-failure race contract | `tests/FunnySharp.Tests/ParallelAsyncEnumerableTests.cs`, `ParallelAsyncSequenceTests.cs`, `FirstSuccessTests.cs`; `docs/concurrency.md`; performance table in `docs/concurrency.md`; frozen member sets in `AdvancedPatternCurationTests` |
| AD-6 adopt (G7) | Completion-order coordination over `IAsyncEnumerable<T>` and cold effects | Delivered by Goal 18 (commit `48c6129`): completion-order delivery is named in the method, not a parameter | Goal 18 delivery (archived `docs/goals/archive/0018-goal.md`); `docs/concurrency.md`; `ConcurrencyBenchmarks` |
| AD-6 reject/defer | Reject naked started-Task racing, unbounded fan-out, `Fork`; defer `Memoize`/`IAsyncBuffer` and k-way merge until a concrete consumer exists | Absent from the public surface; enforced by test | `AdvancedPatternCurationTests.RejectedAndDeferredCapabilitiesAreAbsentFromThePublicSurface` (forbidden names incl. `Fork`, `Race`, `WhenAny`, `Memoize*`, `*AsyncBuffer*`) |
| AD-7 keep | Deferred `ValueTask` execution, exact token forwarding, caller-owned environment, exactly-once resource release, "not a runtime" boundary | Delivered and stable, unchanged since the pinned inventory; the surface is frozen member-for-member | `EffectTests` (19 tests: deferral, token forwarding without eager checks, environment threading, exception capture), `EffectResourceTests` (14 tests: exactly-once release across success, domain failure, use fault, acquisition fault, cancellation, dispose failure, dispose cancellation); `docs/effects.md`; `EffectBenchmarks`; frozen member sets in `AdvancedPatternCurationTests` |
| AD-7 keep (adoption guidance) | Use `Effect` when deferred execution, explicit environment, cancellation flow, or resource lifetime pay for the wrapper; a single synchronous call stays direct C# | Documented in the product contract and `docs/effects.md`; quantified by the pinned W9 comparison (9→10 semantic) and not re-measured | `docs/product-contract.md` "Product Direction" effects bullet; `call-sites.md` §12 (W9) |
| AD-7 reject | Eff/Aff/IO monads, Reader delegates, runtime DI traits (`Has<RT,Trait>`), retry schedules as a DSL, `Effect<Fin<T>>` failure carriers | Absent; enforced by test | `AdvancedPatternCurationTests` forbidden type names (`Eff`, `Aff`, `IO`, `Reader`, `Has`, `Trait`) |
| AD-8 keep | Four-status model, data-only `StateChange`, undefined-handler distinction, pure replay | Delivered and stable; replay threads state and collects ordered outputs, stops at the first non-applied result, and is deterministic | `StateMachineTests` (14 tests incl. `ReplayStopsAtUndefinedAndProducesDeterministicResults`, `ReplayPreservesOrdinaryExceptionsByIdentity`), `StateTransitionTests` (9 tests); `docs/state-machines.md`; comparative evidence `call-sites-goal-19.md` §4 (WF-19A: 92+6 → 75 semantic vs the hand-rolled envelope/delegation/replay) |
| AD-8 redesign (member-level) | `Then` left-associated chains are O(n²); Goal 20 requires resolve-or-exclude, lead decision resolve with single-materialization composition | **Deliberately unchanged by Goal 19** (Goal 20 owns the redesign); the current cost stays disclosed, not hidden | `docs/state-machines.md` "Performance Characterization" (81.92x–518.87x, up to 187,296 B at Count=256); `StateTransitionTests.ThenThreadsStateAndConcatenatesOutputsInExecutionOrder`; Goal 20 owns the resolution |
| AD-8 reject | `State`/`StateT`/`ReaderT`/`WriterT`/`RWS`, actor runtimes (`MailboxProcessor`), STM, persistence, workflow engines | Absent; enforced by test | `AdvancedPatternCurationTests` forbidden type names (`State`, `StateT`, `Writer`, `WriterT`, `RWS`, `Mailbox*`, `Actor`, `Workflow`) |
| AD-9 keep | Delegate-based total and partial focuses, left-to-right composition, absent-focus source identity, caller-owned laws, uninitialized optics throw | Delivered and stable | `OpticsTests` (9 tests: lens laws, left-to-right composition, absent-focus identity, BCL/frozen interop, no-copy/no-freeze, default rejection, exception identity); `docs/immutable-updates.md`; `ImmutableUpdateBenchmarks` |
| AD-9 keep (adoption note) | Lenses are for paths updated in more than one place; first-use cost is real | Refined with compiled evidence: at two sites the lens still costs more LOC (10 vs 19 semantic incl. one-time definitions) but wins per site (3–4 vs 4–6) and makes the path a single named value that cannot drift; LOC break-even ≈7–8 sites | `call-sites-goal-19.md` §5 (WF-19B) |
| AD-9 reject | Prism/traversal/iso/getter/setter/fold hierarchies, per-carrier lenses, reflection property paths, persistent-collection policy | Absent; enforced by test | `AdvancedPatternCurationTests` forbidden names (`Prism`, `Iso`, `Getter`, `Setter`, `Traversal`, `Fold` as types or member fragments) |
| G6 adopt experimental | Traversal location/path context enters as experimental unless its goal produces full evidence | Delivered by Goal 17 as the only experimental family: `Location` + 16 located `Traverse` overloads, all `[Experimental("FS0017")]` | `docs/stability-inventory.md` (experimental tier); `AdvancedPatternCurationTests.EveryExperimentalMemberCarriesTheTrackedStabilityDiagnostic` |
| G8 reject for this stage | A general retry, backoff, or scheduling policy layer is not part of the stable surface | Absent; enforced by test; coordinator-owned timeouts (first-success family) remain the only timeouts | `AdvancedPatternCurationTests` forbidden names (`Retry`, `Backoff`, `Schedule`); `docs/product-contract.md` retry bullet |
| G10 adopt | `[Experimental("FS####")]` marker + a tracked stability inventory; the next stage starts with no experimental members in the 0.1.0 surface, and Goal 21's diagnostics enter as experimental unless their goal produces full evidence | Delivered: every experimental member carries the marker with a documented diagnostic, and `docs/stability-inventory.md` now inventories **both tiers** (stable families with decision references + the experimental member table); the experimental-tier sync is test-enforced; the only experimental family today is Goal 17's FS0017 location context | `docs/stability-inventory.md`; `AdvancedPatternCurationTests.EveryExperimentalMemberCarriesTheTrackedStabilityDiagnostic` and `.StabilityInventoryTracksExactlyTheExperimentalSurface`; Goal 23 owns the committed API-compatibility baseline |
| G17 reject | No public `Unit` type | Absent; no-value outcomes use `UnitResult<TError>` | `AdvancedPatternCurationTests` forbidden type name `Unit` |

No remove decision exists in the record for these families, and none was needed: the AD-8
`Then` redesign target is a performance redesign, not a removal, and is owned by Goal 20.

## 2. Goal-constraint verification

Each completion constraint from `docs/goals/0019-goal.md` maps to verifiable evidence:

| Constraint | Evidence |
| --- | --- |
| Retained abstractions demonstrably reduce consumer ceremony or prevent a meaningful bug | State family: `call-sites-goal-19.md` §4 — the envelope, delegation, and replay protocol shrink 43 → 20 semantic with the decision logic identical; the replay determinism and command separation are structural. Optics: `call-sites-goal-19.md` §5 — per-site ceremony 4–6 → 3–4 semantic and the path becomes a single named, testable value. Effects: pinned W9 honest single-scope cost (9→10) with the composed-resource-scope win; resource scopes prevent the dispose-on-every-path bug class (`EffectResourceTests`). Concurrency: pinned W7/W8 (46→7, 25→14 semantic) |
| Retained abstractions compose with the semantic core and async model | `EffectTests.SelectAndSelectManySupportStandardQueryComposition`, `EffectTests.EnvironmentEffectsComposeWithTheSameEnvironmentAndExactToken`, `Effect.FromResult` keeps `Result` an explicit value; `StateMachineTests.PureTransitionsDoNotExecuteOutputsAndAsyncExecutionReceivesTheCallerToken`; the WF-19A execution boundary composes the state decision with ordinary `await` and `CancellationToken` |
| Clear evaluation and resource-lifetime semantics | Effects evaluate only on `RunAsync` (`EffectTests.EffectsAreDeferred...`); resources are acquired per run and released exactly once (`EffectResourceTests`); optics delegates are the only evaluation and absent focuses skip setters (`OpticsTests`); state changes snapshot outputs at construction (`StateTransitionTests.ToPreservesStateAndSnapshotsOutputs`) |
| Acceptable measured cost | §4 below: the four advanced-pattern benchmark tables are generated, approved, and reproduced on this machine; allocation ceilings are the blocking contract and the tables disclose the wrapper cost rather than claiming it is free |
| State decisions deterministic and separable from command execution | `StateMachineTests.ReplayStopsAtUndefinedAndProducesDeterministicResults`, `PureTransitionsDoNotExecuteOutputs...`, `OutputCancellationRemainsOutsideThePureTransitionCore`; `docs/state-machines.md` "Async Boundary" |
| Resource scopes safe across success, failure, exception, and cancellation | `EffectResourceTests`: exactly-once disposal after success (`UsingAndUsingAsyncAreDeferred...`), explicit Result failure (`...ReleaseResourcesWhenUseReturnsADomainFailure`), use fault and cancellation (`...DisposeExactlyOnceAfterUseFaultAndCancellation`), acquisition fault/cancellation (`...DoNotUseOrDisposeAfterAcquisitionFaultOrCancellation`), dispose failure precedence, and dispose cancellation precedence |
| Immutable programming remains opt-in | The core package has zero package references (`PackageBoundaryTests.CoreAssemblyReferencesOnlyPlatformAssemblies`); optics never copy, freeze, or prevent mutable-leaf aliasing (`OpticsTests.LensDoesNotCopyFreezeOrPreventMutableLeafAliasing`); no persistent-collection universe exists (`AdvancedPatternCurationTests`) |
| Environment-dependent effects must not become a DI container or execution universe | The environment is an ordinary caller-owned value (`EffectTests.ProvideBindsOneEnvironment...`, `docs/effects.md` "API Shape"); no `Microsoft.Extensions` dependency, no runtime DI traits, no scheduler (`AdvancedPatternCurationTests`, `PackageBoundaryTests`) |
| Rejected or deferred capabilities not smuggled back through aliases or convenience APIs | `AdvancedPatternCurationTests.RejectedAndDeferredCapabilitiesAreAbsentFromThePublicSurface` scans every public type and member of both the exact-name list and the fragment list with the decision recorded per entry |
| Experimental decisions unmistakably experimental | Every experimental member fails consumer compilation without suppression (`[Experimental("FS0017")]`); the exact experimental set is frozen by test and synced with the tracked inventory (`EveryExperimentalMemberCarriesTheTrackedStabilityDiagnostic`, `StabilityInventoryTracksExactlyTheExperimentalSurface`) |
| Deferred capabilities absent from the stable surface | The deferred list in `docs/product-contract.md` "Deliberate Deferrals" was reconciled with the delivered surface (§3); the no-smuggling test holds the absence |

## 3. Curation changes made by Goal 19

Goal 19 changes no `src/` surface — the audit found the delivered surface aligned with the
record — and closes four evidence gaps:

1. **Two-tier stability inventory** (`docs/stability-inventory.md`). The inventory now
   records the stable families with their decision references and delivering goals beside
   the experimental member table, so "stable versus experimental" is answerable from one
   tracked document instead of inferred from the decision matrix.
2. **Product-contract deferral reconciliation** (`docs/product-contract.md`). The deferral
   list still named "non-empty carriers and exact-vs-truncating zip" although Goal 17
   adopted them (`NonEmpty<T>`, `ZipExact`/`ZipExactOrNone`) after proving their safety
   value (decision E72's adoption condition). The list now records the adoption, and the
   current tier assignment points at the stability inventory. No capability changed hands;
   the recorded deferral and the delivered surface agree again.
3. **Frozen-surface curation tests**
   (`tests/FunnySharp.Tests/AdvancedPatternCurationTests.cs`, xUnit v3). Five tests freeze
   the curated advanced-pattern member sets, scan the whole public surface for rejected or
   deferred vocabulary (with the deciding reference attached to every forbidden name),
   forbid `Where` on carriers and conversion operators, and pin the experimental set to
   the tracked FS0017 family, including the inventory-document sync. A surface that drifts
   from the accepted record now fails the build's test gate instead of waiting for an audit.
4. **Comparative call-site evidence** (`docs/next-stage/call-sites-goal-19.md` +
   `call-sites-code/...`). The state family had no compiled comparison at all, and the
   optics family had none for the multi-site adoption scenario the record names. WF-19A and
   WF-19B fill both gaps against compiled idiomatic baselines (0 warnings, 0 errors), with
   the honest result that optics at two sites remain LOC-negative.

## 4. Performance evidence

The blocking performance contract for the advanced patterns is the approved observation in
`eng/performance/baseline.json`, rendered as generated tables in the guides:

- Effects: `docs/effects.md` — 10 scenarios vs direct .NET (e.g. `Map` composition 79.470 ns
  vs 3.765 ns, 0 B; `Using`/`UsingAsync` 0 B).
- State machines: `docs/state-machines.md` — left-associated `Then` chains at Count=8/64/256,
  disclosing the O(n²) concat cost (up to 518.87x, 187,296 B at 256) that Goal 20 owns.
- Immutable updates: `docs/immutable-updates.md` — 5 scenarios vs direct `with`/BCL
  operations (nested record replacement 1.87x, same 72 B; missing optional 1.17x, 0 B).
- Concurrency: `docs/concurrency.md` — parallel mapping/traversal and first-success vs the
  hand-rolled equivalents.

Reproduction run (Goal 19, 2026-09-23, SDK 10.0.400, .NET 10.0.11, AMD EPYC 7763,
Linux x64, ShortRun; logs and receipts under `artifacts/benchmarks-goal19/` in the evidence
workspace, not committed): the four advanced-pattern suites were rerun from the committed
benchmark sources. Every allocation figure that the approved tables state as an exact byte
count reproduced exactly: effects (24 B/80 B/208 B/88 B compositions and 0 B
`RunAsync`/`Map`/`Provide`/`Using` paths), state-machine `Then` chains (1,792 B at
Count=8, 22,176 B at 64, 187,296 B at 256), and optics (72 B nested replacement, 104 B
existing-key, 208 B batch, 0 B missing-optional and frozen lookup). Hosted timings reproduce
the approved ratios directionally (e.g. `Map` composition 20.81x vs the approved 21.10x;
`Then` Count=256 545.95x vs 518.87x; missing-optional 1.31x vs 1.17x) with ShortRun noise,
which is why hosted timing stays directional and the blocking contract remains the approved
allocation ceilings verified by the release gate's performance-verify step. The reproduction
confirms the published tables are regenerable from the committed sources; it is not a new
performance claim and does not replace the approved observation.

## 5. Deliberately unchanged

- **`StateTransitionExtensions.Then`** keeps its recorded behavior and its disclosed O(n²)
  long-chain cost; the resolve-or-exclude decision is Goal 20's completion criterion.
- **No analyzer, diagnostics package, or suppression guidance beyond the experimental
  markers** ships from Goal 19; Goal 21 owns analyzer coverage.
- **No typed-results/OpenAPI API** and no ASP.NET Core vertical-slice claims; Goal 22 owns
  them (`.Produces*` documentation and tests).
- **No committed API-compatibility baseline or release notes**; Goal 23 owns them. The
  stability inventory records tiers; it does not promise compatibility.
- **`Effect.FromValue` null-policy documentation** ("null is a value") is owned by Goal 23
  per the record.

## 6. Verification summary

- Build: `dotnet build -c Release FunnySharp.slnx` — 0 warnings, 0 errors (SDK 10.0.400).
- Tests: `dotnet test FunnySharp.slnx -c Release --no-build` — 643 passed, 0 failed
  (includes the five new `AdvancedPatternCurationTests`).
- Comparative evidence: both scratch projects rebuilt clean (§8 of
  `call-sites-goal-19.md`); no Goal 14/15/16 evidence file was modified.
- Benchmark reproduction: §4 above.
