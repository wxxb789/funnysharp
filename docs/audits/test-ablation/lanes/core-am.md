# Core A-M test-ablation assessment

Baseline: `0c5416e37e06db17c30c014659601d0c463b973a`; branch `ulw/test-ablation-g001`; inspected worktree `Q:/repos/funnysharp/.omo/worktrees/test-ablation-g001`.

## Decision and evidence boundary

The ledger has **245 current source methods**: **243 retain, 2 simplify candidates, 0 merge, 0 delete**. Both simplify rows are assertion-oracle proposals with their product detectors retained; neither is an executed edit or approved removal. The helper ledger has **290 rows, all retain**. Similar helper bodies and overlapping tests are explicitly hypotheses, not detector-containment proofs. No test, production file, frozen evidence, git history, or other lane report was edited.

Each test row identifies inspected setup/assertions, contractual fault classes, relevant implementation, preserved theory inputs, and a concrete representative fault plus an exact native xUnit filtered command. `line` is the source method declaration, not its Fact/Theory attribute. The canonical identity is the repository-relative path with forward slashes plus `::` plus the exact method name. Helper rows name the actual definition or grouped local fixture scope, include inspected definition locations and real consumers where available, and carry witness commands. `none proved` in independent_owner means no independently executable retained detector set has established the required containment; it is not a claim that the method has no overlapping assertions.

Model `D(t)` as faults, partitioned by contractual class and call shape, for which the actual observations of test `t` reject the faulty behavior. Removing/merging a test requires `D(t)` to be contained in the union of **concrete independent retained executable** owners. Passing the reduced suite, lower counts, coverage, shared helper names, or a matching implementation body cannot establish that relation. Representative old/new fault experiments are mandatory evidence, including faults targeting the allegedly unique branch; none were authorized/executed in this reporting-only lane. Consequently all unproved unique detectors remain. The CSV's examples are witness plans, not claims that mutants have been compiled or killed.

Read authorities: root/tests/core/docs/eng AGENTS; `docs/product-contract.md`, `docs/grammar.md`, `docs/stability-inventory.md`, `docs/harness.md`; actual A-M test bodies and all current core/shared helper bodies; relevant carrier, cardinality, traversal, stream, container, function, location, Effect/resource/coordinator implementations. Current source, not previous ablation material, supplied every disposition.

## Completeness and Compile closure

Read-only `dotnet msbuild tests/FunnySharp.Tests/FunnySharp.Tests.csproj -getItem:Compile -nologo` exited **0** using the installed allowed patch SDK **10.0.401**. Its evaluated source input set is exactly **70 files**: **69 top-level core C# sources** plus the linked `tests/Shared/TestRepositoryRoot.cs`. There are **no core subdirectories**, hence no unassigned descendant tests. There are **19 A-M test-bearing files**, **234 Fact methods + 11 Theory methods = 245**. All 11 theories are in FirstSuccessTests; their **28 InlineData rows** are preserved below (234 + 28 = 262 source-derived cases, not an executed discovery receipt). No custom executable runner check or non-attributed top-level test body was found in owned sources.

The independent attribute scan equals the method ledger; canonical IDs are unique. The helper definition scan found **251 core declarations/definition units**, and the shared root file adds **1**. Add **38 test-local fixture scopes**, containing **57 named local functions**, for **290 ledger rows**. The core declaration units contain **111 non-test type/file units** and **140 test-class helper functions/data definitions**. Each nested helper type, including explicit enumerator types, has a row; its constructors/properties/ordinary members are inspected together, not inflated into individual rows. Locals inside a helper body belong to that helper's inspected implementation. Test-local trivial function families are grouped in one named scope row; anonymous callbacks and compiler-generated members belong to the enclosing test/type and are not fabricated extra helpers. All 251 scanned core definitions have a matching ledger definition location; the standalone file rows cover their file-level type. No missing or duplicate helper ID was found.

CSV quoting escapes embedded quotes and commas; the parsed records are checked for exact field width, unique IDs, canonical source/method joins, disposition vocabulary, and complete source inventory. The following table enumerates every evaluated Compile input; only A-M test dispositions are owned here, while **all helper rows** are owned here. Zero-helper files are deliberate, not omissions.

| Compile input | Owned test methods | Helper rows |
| --- | ---: | ---: |
| `tests/FunnySharp.Tests/AdvancedPatternCurationTests.cs` | 5 | 12 |
| `tests/FunnySharp.Tests/AsyncCollectionTests.cs` | 30 | 15 |
| `tests/FunnySharp.Tests/AsyncCollectionTraversalTests.cs` | 14 | 16 |
| `tests/FunnySharp.Tests/AsyncEnumerablePipelineTests.cs` | 11 | 5 |
| `tests/FunnySharp.Tests/AsyncFunctionCompositionTests.cs` | 21 | 3 |
| `tests/FunnySharp.Tests/AsyncScanTests.cs` | 11 | 5 |
| `tests/FunnySharp.Tests/CardinalityOrNoneTests.cs` | 13 | 2 |
| `tests/FunnySharp.Tests/CollectionTraversalTests.cs` | 9 | 12 |
| `tests/FunnySharp.Tests/ContainerBridgeTests.cs` | 13 | 0 |
| `tests/FunnySharp.Tests/CountingValueTaskSource.cs` | other lane | 1 |
| `tests/FunnySharp.Tests/DefaultStateTests.cs` | 12 | 11 |
| `tests/FunnySharp.Tests/EffectResourceTests.cs` | 14 | 5 |
| `tests/FunnySharp.Tests/EffectTests.cs` | 19 | 1 |
| `tests/FunnySharp.Tests/EnumerablePipelineTests.cs` | 5 | 1 |
| `tests/FunnySharp.Tests/FirstSuccessTests.cs` | 35 | 16 |
| `tests/FunnySharp.Tests/FunctionCompositionTests.cs` | 12 | 0 |
| `tests/FunnySharp.Tests/GeneratedDelegateContractTests.cs` | 1 | 0 |
| `tests/FunnySharp.Tests/GeneratedRecordContractTests.cs` | 1 | 2 |
| `tests/FunnySharp.Tests/GrammarInferenceTests.cs` | 9 | 2 |
| `tests/FunnySharp.Tests/LocationTests.cs` | 10 | 1 |
| `tests/FunnySharp.Tests/NonEmptyTests.cs` | other lane | 1 |
| `tests/FunnySharp.Tests/OpticsTests.cs` | other lane | 7 |
| `tests/FunnySharp.Tests/OptionAsyncTests.cs` | other lane | 0 |
| `tests/FunnySharp.Tests/OptionInteropTests.cs` | other lane | 2 |
| `tests/FunnySharp.Tests/OptionLinqTests.cs` | other lane | 0 |
| `tests/FunnySharp.Tests/OptionTests.cs` | other lane | 4 |
| `tests/FunnySharp.Tests/PackageBoundaryTests.cs` | other lane | 1 |
| `tests/FunnySharp.Tests/ParallelAsyncEnumerableTests.cs` | other lane | 20 |
| `tests/FunnySharp.Tests/ParallelAsyncSequenceTests.cs` | other lane | 12 |
| `tests/FunnySharp.Tests/ParseOrNoneTests.cs` | other lane | 2 |
| `tests/FunnySharp.Tests/PartitionTests.cs` | other lane | 3 |
| `tests/FunnySharp.Tests/PerformanceManifestTests.cs` | other lane | 0 |
| `tests/FunnySharp.Tests/ProbeEnumerable.cs` | other lane | 1 |
| `tests/FunnySharp.Tests/ResultAsyncTests.cs` | other lane | 1 |
| `tests/FunnySharp.Tests/ResultBoundaryTests.cs` | other lane | 8 |
| `tests/FunnySharp.Tests/ResultInteropTests.cs` | other lane | 0 |
| `tests/FunnySharp.Tests/ResultTests.cs` | other lane | 6 |
| `tests/FunnySharp.Tests/ScanTests.cs` | other lane | 1 |
| `tests/FunnySharp.Tests/SpanPipelineTests.cs` | other lane | 1 |
| `tests/FunnySharp.Tests/StableCardinalityNullContractTests.cs` | other lane | 1 |
| `tests/FunnySharp.Tests/StableCarrierAsyncMatrixTests.cs` | other lane | 2 |
| `tests/FunnySharp.Tests/StableCarrierEqualityMatrixTests.cs` | other lane | 1 |
| `tests/FunnySharp.Tests/StableCarrierSyncMatrixTests.cs` | other lane | 0 |
| `tests/FunnySharp.Tests/StableCarrierValueMatrixTests.cs` | other lane | 6 |
| `tests/FunnySharp.Tests/StableCollectionCardinalityMatrixTests.cs` | other lane | 10 |
| `tests/FunnySharp.Tests/StableCollectionPipelineMatrixTests.cs` | other lane | 7 |
| `tests/FunnySharp.Tests/StableCollectionSyncMatrixTests.cs` | other lane | 9 |
| `tests/FunnySharp.Tests/StableCollectionTraversalMatrixTests.cs` | other lane | 4 |
| `tests/FunnySharp.Tests/StableDiagnosticOutputContractTests.cs` | other lane | 1 |
| `tests/FunnySharp.Tests/StableEffectBoundaryContractTests.cs` | other lane | 1 |
| `tests/FunnySharp.Tests/StableEffectConsumptionContractTests.cs` | other lane | 4 |
| `tests/FunnySharp.Tests/StableFunctionRuntimeContractTests.cs` | other lane | 3 |
| `tests/FunnySharp.Tests/StableNullablePayloadTests.cs` | other lane | 0 |
| `tests/FunnySharp.Tests/StableObjectEqualityTests.cs` | other lane | 0 |
| `tests/FunnySharp.Tests/StableParallelDefaultContractTests.cs` | other lane | 1 |
| `tests/FunnySharp.Tests/StableSpanFailureContractTests.cs` | other lane | 0 |
| `tests/FunnySharp.Tests/StateMachineTests.cs` | other lane | 4 |
| `tests/FunnySharp.Tests/StateTransitionTests.cs` | other lane | 0 |
| `tests/FunnySharp.Tests/TraversalContextTests.cs` | other lane | 24 |
| `tests/FunnySharp.Tests/UnitResultAsyncTests.cs` | other lane | 1 |
| `tests/FunnySharp.Tests/UnitResultBoundaryTests.cs` | other lane | 2 |
| `tests/FunnySharp.Tests/UnitResultInteropTests.cs` | other lane | 0 |
| `tests/FunnySharp.Tests/UnitResultTests.cs` | other lane | 3 |
| `tests/FunnySharp.Tests/UnitResultTraversalTests.cs` | other lane | 6 |
| `tests/FunnySharp.Tests/ValidationAsyncTests.cs` | other lane | 1 |
| `tests/FunnySharp.Tests/ValidationTests.cs` | other lane | 10 |
| `tests/FunnySharp.Tests/ValueTaskConsumptionTests.cs` | other lane | 1 |
| `tests/FunnySharp.Tests/WhereNotNullTests.cs` | other lane | 3 |
| `tests/FunnySharp.Tests/ZipExactTests.cs` | other lane | 4 |
| `tests/Shared/TestRepositoryRoot.cs` | other lane | 1 |

### Theory partitions

| FirstSuccess source method | InlineData partitions |
| --- | --- |
| `CallerCancellationOverridesTimeoutSelectedBeforeCleanupCompletes` | `[InlineData(true)] [InlineData(false)]` |
| `CallerCancellationRemainsPrimaryWhenTimeoutCleanupAlsoFails` | `[InlineData(true)] [InlineData(false)]` |
| `LegacyFirstSuccessOverloadsAdmitOnlyThirtyTwoOf1024SynchronousSuccesses` | `[InlineData(0)] [InlineData(1)] [InlineData(2)]` |
| `FirstSuccessRetainsEveryExceptionRepresentedByASourceTask` | `[InlineData(false)] [InlineData(true)]` |
| `FirstSuccessKeepsCancellationOrTimeoutPrimaryWithoutDroppingCandidateFaults` | `[InlineData(false)] [InlineData(true)]` |
| `FirstSuccessStopsAdmissionWhenTheCallerCancelsBeforeOrDuringStartup` | `[InlineData(false)] [InlineData(true)]` |
| `ExplicitFirstSuccessBoundIsValidatedBeforeEnumeratingTheInput` | `[InlineData(0)] [InlineData(-1)]` |
| `ExplicitFirstSuccessBoundLimits1024HeldCandidatesAndRefillsExactlyOneSlot` | `[InlineData(1)] [InlineData(2)] [InlineData(32)]` |
| `ExplicitFirstSuccessBoundStopsAdmissionAndDrainsOnTimeoutOrCallerCancellation` | `[InlineData(1, false)] [InlineData(2, true)] [InlineData(32, false)] [InlineData(32, true)]` |
| `ExplicitFirstSuccessHandlesLargeSynchronousFailuresInOrderWithOneValueTaskConsumption` | `[InlineData(1)] [InlineData(32)] [InlineData(4096)]` |
| `ExplicitFirstSuccessAccountsForLargeStaggeredBatchesWithoutReorderingTypedFailures` | `[InlineData(2)] [InlineData(32)] [InlineData(1024)]` |

Preserve internal loop partitions too: TimeoutDoesNotReplaceAWinner tests late typed failure and late success99; CallerCancellationRemainsPrimaryWhenTimeoutCleanupAlsoFails crosses hasWinner true/false with faultDuringDrain false/true. Legacy overload0/1/2 covers three different API signatures. Explicit degrees 1/2/32 and 1/32/4096 or 2/32/1024 cover sequential, bounded, and full-window admission. A method row never collapses these into one abstract numeric case.

## Bounded candidates and required witnesses

**C1: simplify only the six direct BCL Throws assertions** in `ContainerBridgeTests.cs:144`, EmptyContainersReturnNoneWhereTheBclOperationsThrow. Those calls demonstrate the platform empty-container baseline rather than exercising FunnySharp. Keep all six bridge None assertions and unchanged empty-state assertions. Benefit: six assertion/delegate calls and associated framework-baseline maintenance removed, with no promised wall-clock saving. This is a source-grounded nonproduct-oracle candidate; it is not a proved redundant-test deletion.

`dotnet run --project tests/FunnySharp.Tests/FunnySharp.Tests.csproj -c Release -- -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips`

Before/after witness: mutate a failed TryPeek bridge to return Some(default), throw, or mutate a different element; old and candidate revised product assertions must both reject each affected Queue/Stack/PriorityQueue mutating/peek overload. The direct BCL control calls themselves cannot justify lowering bridge emptiness/atomicity requirements.

**C2: simplify the fixture-prose oracle, not error preservation**, in `AsyncCollectionTests.cs:627`, AsyncBatchValidationComposesNestedErrorLocations. Replace its pinned English error phrase with expected error payload obtained separately from the local ParsePostalCode(empty) fixture. Keep exact composed customers[1].addresses[0].postalCode, nonempty error cardinality, Message preservation, and valid-record/address values. Benefit: fixture copy can change in one place without weakening the integration; no runtime saving claimed.

`dotnet run --project tests/FunnySharp.Tests/FunnySharp.Tests.csproj -c Release -- -method "FunnySharp.Tests.AsyncCollectionTests.AsyncBatchValidationComposesNestedErrorLocations" -noLogo -noColor -failSkips`

Before/after witnesses: drop the customer segment; shift address index; or make ImportError.At lose Message while preserving Location. All must fail both old and revised tests. The expected payload must come from ParsePostalCode directly, not the same erroneous At path, avoiding a common-mode oracle. DefaultState result/validation message equality is a separate **retained hypothesis**, because the uninitialized diagnostic contract/wording requires a wider contract decision; `Uninitialized` ToString is documented diagnostic output, not arbitrary prose.

**H1: possible generic counting-source reuse**, no merge authorized: AsyncFunctionCompositionTests.CountingValueTaskSource and FirstSuccessTests.ControllableValueTaskSource<T> both use the same ManualResetValueTaskSourceCore behaviors as standalone CountingValueTaskSource<T>. Basic body similarity does not substitute for completed/pending/faulted/canceled once-only witnesses. Their current consumers are retained; StableEffectPendingSource, CollectionMatrixProbe.Gate and subscription-counting sources are not equivalent.

`dotnet run --project tests/FunnySharp.Tests/FunnySharp.Tests.csproj -c Release -- -class "FunnySharp.Tests.AsyncFunctionCompositionTests" -class "FunnySharp.Tests.FirstSuccessTests" -noLogo -noColor -failSkips`

Required old/new witnesses: double-consume first-stage or terminal source; return before source completion; drop callback/late loser fault. Preserve exact counts, exception identity, faulted/canceled state, and current constructor default behavior. Void helpers are a different interface; ValueTaskConsumptionTests defaults completed while AsyncFunctionComposition's void helper defaults pending. Do not hide that difference behind a factory shim.

**H2: duplicate async probes**, no merge authorized: AsyncCollection/AsyncCollectionTraversal share a richer cancellation/move/disposal probe; AsyncEnumerablePipeline/AsyncScan share a simpler move/disposal probe. Their explicit nested Enumerator rows remain. Other probes observe Current reads, post-values faults, public MoveNext readiness, token-linked cancellation, exhaustion, held disposal or gate subscription. Those dimensions are necessary detectors, not cleanup opportunities.

`dotnet run --project tests/FunnySharp.Tests/FunnySharp.Tests.csproj -c Release -- -class "FunnySharp.Tests.AsyncCollectionTests" -class "FunnySharp.Tests.AsyncCollectionTraversalTests" -class "FunnySharp.Tests.AsyncEnumerablePipelineTests" -class "FunnySharp.Tests.AsyncScanTests" -noLogo -noColor -failSkips`

Required old/new witnesses: extra source pull, second enumeration, missed disposal, token substitution, changed failure identity, terminal-false ValueTask double consumption. Bound any future edit to one duplicate probe family at a time. Helper consolidation promises fewer definitions, not fewer product checks or source suspensions.

**T1: nullable Queue dequeue overlap remains a hypothesis.** DequeueOrNoneMutatesWhenTheNullableFrontElementIsPresent uses the same CLR closed generic string as QueueDequeueOrNoneReturnsTheFrontElementAndRemovesIt. The latter additionally checks FIFO repeat/empty behavior. No executed fault containment was collected, so both method rows retain.

`dotnet run --project tests/FunnySharp.Tests/FunnySharp.Tests.csproj -c Release -- -method "FunnySharp.Tests.ContainerBridgeTests.DequeueOrNoneMutatesWhenTheNullableFrontElementIsPresent" -method "FunnySharp.Tests.ContainerBridgeTests.QueueDequeueOrNoneReturnsTheFrontElementAndRemovesIt" -noLogo -noColor -failSkips`

Necessary witnesses before proposing deletion: no dequeue, two dequeues, wrong front value, nullable-present wrongly treated as None, and thrown exception after partial mutation. Execute both old detectors independently; then execute the retained owner alone under the same witnesses. No theory/type boundary may be inferred away from annotation similarity alone.

## Meaningful retained representatives and burden

Retain CollectionTraversal.SequenceDefersCollectionCountUntilASuccessValueNeedsBuffering: its ICollection Count throws, so moving allocation preflight before a first failure is observable. Retain Cardinality.PredicateOverloadsStopInvokingPredicatesAtTheDecisiveMatch and the exact-yield probe tests: repeated/extra work is detected independently of final values. Retain Scan's noncommutative subtraction and distinct seed: summed examples alone could hide reversed arguments. Retain Location.LabelledKey-backed HashSet test: equal keys with different rendering expose hashing the rendered path. Retain generated record shallow-clone/property-comparer behavior: it detects copying or collection-enumerating equality, not source text. Generated delegate BeginInvoke/EndInvoke checks are framework dominated but lack a concrete independent remaining executable owner; no deletion by intuition.

For FirstSuccess, the meaningful independent observations are admitted-start counts, maximum active work, fully drained active 0, exact caller versus linked tokens, source Task faulted/canceled state, all fault identities/order, and gated UsingAsync disposal. Callback-entered or source-disposal-started is explicitly not coordinator quiescence. Current test bodies subscribe cancellation/start/disposal signals before triggering completion. ManualTimeProvider advances logical time instead of sleeping. The bounded WaitAsync deadlines are hang protection; Task.Delay(InfiniteTimeSpan, token) in other core helpers waits the cancellation event, not a fixed duration. Task.Yield fixtures exercise suspension. No fixed positive sleep, polling, wall-clock race assertion or meaningless SHA computation was found in owned A-M tests. No source-shape text or arbitrary prose-hash pin was used to prove redundancy. Reflection-based exported names, Experimental IDs/overload counts and parsed inventory cells remain contractual machine values.

Runtime burden is source-grounded, **not measured here**: sync cardinality/Sequence and async batch integration exercise 100000-item loops; async collection traversal exercises 10000 items; FirstSuccess uses 1024 held/shared-task inputs and 4096 source-backed completed failures across explicit degree partitions. These enforce stack safety, finite snapshotting and admission/once-only behavior. Do not shrink counts, substitute eager arrays for lazy probes, delete pending-path subscriptions, or discard allocation gates to improve reported test duration. Helper cost lies chiefly in repeated probe implementations and long matrix routers, not a demonstrated product-test runtime budget breach. Allocation budgets remain blocking; hosted timing remains directional under the product contract.

## Validation, omissions and stopping boundary

No baseline build or test was duplicated. The parent owns the unchanged full-solution gate in the primary checkout. This lane ran only read-only git inventory, Compile evaluation and native runner help; it parsed its own report inventory. The existing primary Debug runner's help identifies xUnit v3 runner 4.0.0 and documents exact `-method`, repeated `-class` OR filtering and `-failSkips`. Help is a usage query (exit2), not a test result. An initial Release help path was absent; using the actual Debug runner resolved CLI evidence without building anything. Proposed Release commands are deliberately **unexecuted**. No mutants, old/new ablation experiments, timing/allocation measurements or independent full-gate pass are claimed.

Inspection was completed directly. Three read-only subtasks remained queued and were canceled before starting; no child output is represented as inspection evidence. No ownership gaps, uninspected owned methods/helpers, or remaining artifact blockers are recorded. Remaining experimental proof is a restriction on future deletion/merge, not deferred assessment work. Only `core-am.csv`, `core-am-helpers.csv` and this `core-am.md` are assigned output paths.
