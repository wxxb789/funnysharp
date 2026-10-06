# Core N-Z test-ablation assessment

## Decision and authority

Baseline: `0c5416e37e06db17c30c014659601d0c463b973a`. Worktree: `Q:/repos/funnysharp/.omo/worktrees/test-ablation-g001`. This lane inspected current bodies, not historical ablation labels. Scope is the 48 top-level test-bearing files whose names start N through Z in `tests/FunnySharp.Tests`. Only these three reports were written. Tests, shipping code, frozen evidence, other lanes and git history were not changed.

The test CSV contains **569 source methods: 503 Facts and 66 Theories**. The theories declare **997 InlineData rows**, hence **1,500 declared source cases** including Facts, before any runtime discovery. These counts establish completeness, not redundancy or passing behavior. Dispositions are **567 retain, 2 simplify, 0 merge, 0 delete**. No deletion or merge has an executed detector-containment proof. Every row records concrete asserted outcomes, a contractual fault class, retained input partitions, and a **PLANNED NOT RUN** production fault witness. `test_id` is the exact repo-relative source path plus `::` plus the exact method name; `line` is its public declaration line.

`core-nz-helpers.csv` has **144 dependency records**: **69 non-test helper types** defined in the inspected files, **72 test-class helper methods**, and **3 external helper files**. Nested types have one row each, with enclosing type names to disambiguate Enumerator; trivial members are assessed within their type, not duplicated as separate records. Local callbacks/functions and ordinary fields are test inputs, not independently owned helper units. All helper decisions belong to **core-am**, as required by the assignment: this CSV records their inspected obligations and conservative retain dispositions as dependencies, not a second edit/ablation ownership assignment. No true ownership gap was found.

## Evidence and method

Read the applicable root, docs, core and test AGENTS rules, `docs/product-contract.md`, `docs/grammar.md`, `docs/harness.md`, the collection/concurrency/performance guides, both current performance manifests, every owned test body and in-file helper body, the three external helper files, and the 30 current `src/FunnySharp/*.cs` implementation files. Cross-file probes `CollectionMatrixProbe`, `StableEffectPendingSource<T>` and `StableCarrierAsyncMatrixTests.CheckToken` were traced to their definitions rather than inferred from callers. Source assessment used the runtime's default model completion cells plus direct checks; no additional task workers, build, test run, mutation, new dependency, SHA computation or timing sleep was started by this lane. The parent runs the unchanged full solution gate in the primary checkout. Its result is not claimed here.

A test is a detector subset over faults at a particular public entry point, input partition and lifecycle boundary. Necessary deletion/merge condition is containment of **all** of those detectors in concretely identified executable remaining methods, independently of the candidate. Shared helpers, shared production code, green deletion runs, case totals and coverage percentages do not establish it. Old/new witness comparisons must additionally expose representative contract faults to both sets and retain the same identities, tokens, states, call counts, traversal counts and budgets. Overlap candidates below are hypotheses only; unique/unproved detectors stay. Simplify proposals preserve every current product assertion and input partition.

## Compile and completeness inventory

`FunnySharp.Tests.csproj` is an SDK-style net10.0 executable (xunit.v3 4.0.0, global Xunit using). `Directory.Build.props` enables nullable analysis, implicit usings, deterministic builds and warnings as errors. Current project XML contains no Compile removal or additional conditional test inclusion. There are **69 top-level default-glob C# Compile inputs**: these 48 test files, the non-test `ProbeEnumerable.cs`, and the following 20 A-M inputs (core-am ownership):

` AdvancedPatternCurationTests.cs`, `AsyncCollectionTests.cs`, `AsyncCollectionTraversalTests.cs`, `AsyncEnumerablePipelineTests.cs`, `AsyncFunctionCompositionTests.cs`, `AsyncScanTests.cs`, `CardinalityOrNoneTests.cs`, `CollectionTraversalTests.cs`, `ContainerBridgeTests.cs`, `CountingValueTaskSource.cs`, `DefaultStateTests.cs`, `EffectResourceTests.cs`, `EffectTests.cs`, `EnumerablePipelineTests.cs`, `FirstSuccessTests.cs`, `FunctionCompositionTests.cs`, `GeneratedDelegateContractTests.cs`, `GeneratedRecordContractTests.cs`, `GrammarInferenceTests.cs`, `LocationTests.cs `

There is one explicit linked Compile input, `tests/Shared/TestRepositoryRoot.cs`, owned by core-am. The only test-project subdirectories are `bin` and `obj`, excluded by the SDK source glob; generated SDK assembly/global-using inputs are build artifacts, not current source-test methods. Thus there are **70 repository C# Compile inputs** to this project, of which 48 test-bearing inputs belong to this lane. `CountingValueTaskSource.cs`, `ProbeEnumerable.cs` and the linked root resolver are all explicitly recorded external dependencies, not omitted because their filenames fell outside test scope. Shipping ProjectReference is `src/FunnySharp/FunnySharp.csproj`; test dependencies remain non-packable.

Every Fact/Theory attribute was matched one-to-one to an inspected public method. All 66 theory signatures and every literal InlineData row are retained in CSV rationale; inner foreach/switch Cartesian partitions are covered by the same method row, not treated as independently deletable cases. Five methods lack a direct Assert token: the optics method is an intentional static type-inference witness; the other four call assertion-bearing helpers (source-backed ValueTask probes, large traversal values, uninitialized comparison diagnostics, and exact-zip probes). None was discarded as an apparent no-op.

| Source file | Facts | Theories | InlineData rows |
| --- | ---: | ---: | ---: |
| NonEmptyTests.cs | 7 | 0 | 0 |
| OpticsTests.cs | 9 | 0 | 0 |
| OptionAsyncTests.cs | 17 | 0 | 0 |
| OptionInteropTests.cs | 11 | 0 | 0 |
| OptionLinqTests.cs | 6 | 0 | 0 |
| OptionTests.cs | 31 | 0 | 0 |
| PackageBoundaryTests.cs | 1 | 0 | 0 |
| ParallelAsyncEnumerableTests.cs | 4 | 21 | 50 |
| ParallelAsyncSequenceTests.cs | 11 | 0 | 0 |
| ParseOrNoneTests.cs | 8 | 0 | 0 |
| PartitionTests.cs | 11 | 0 | 0 |
| PerformanceManifestTests.cs | 2 | 0 | 0 |
| ResultAsyncTests.cs | 13 | 0 | 0 |
| ResultBoundaryTests.cs | 15 | 0 | 0 |
| ResultInteropTests.cs | 3 | 0 | 0 |
| ResultTests.cs | 27 | 0 | 0 |
| ScanTests.cs | 7 | 0 | 0 |
| SpanPipelineTests.cs | 13 | 0 | 0 |
| StableCardinalityNullContractTests.cs | 2 | 1 | 4 |
| StableCarrierAsyncMatrixTests.cs | 24 | 2 | 157 |
| StableCarrierEqualityMatrixTests.cs | 23 | 0 | 0 |
| StableCarrierSyncMatrixTests.cs | 57 | 0 | 0 |
| StableCarrierValueMatrixTests.cs | 0 | 1 | 76 |
| StableCollectionCardinalityMatrixTests.cs | 1 | 6 | 77 |
| StableCollectionPipelineMatrixTests.cs | 2 | 6 | 88 |
| StableCollectionSyncMatrixTests.cs | 3 | 13 | 213 |
| StableCollectionTraversalMatrixTests.cs | 0 | 6 | 259 |
| StableDiagnosticOutputContractTests.cs | 2 | 0 | 0 |
| StableEffectBoundaryContractTests.cs | 6 | 0 | 0 |
| StableEffectConsumptionContractTests.cs | 6 | 0 | 0 |
| StableFunctionRuntimeContractTests.cs | 0 | 3 | 30 |
| StableNullablePayloadTests.cs | 1 | 0 | 0 |
| StableObjectEqualityTests.cs | 0 | 1 | 3 |
| StableParallelDefaultContractTests.cs | 2 | 0 | 0 |
| StableSpanFailureContractTests.cs | 4 | 0 | 0 |
| StateMachineTests.cs | 14 | 0 | 0 |
| StateTransitionTests.cs | 19 | 0 | 0 |
| TraversalContextTests.cs | 24 | 6 | 40 |
| UnitResultAsyncTests.cs | 11 | 0 | 0 |
| UnitResultBoundaryTests.cs | 14 | 0 | 0 |
| UnitResultInteropTests.cs | 5 | 0 | 0 |
| UnitResultTests.cs | 21 | 0 | 0 |
| UnitResultTraversalTests.cs | 16 | 0 | 0 |
| ValidationAsyncTests.cs | 9 | 0 | 0 |
| ValidationTests.cs | 20 | 0 | 0 |
| ValueTaskConsumptionTests.cs | 5 | 0 | 0 |
| WhereNotNullTests.cs | 6 | 0 | 0 |
| ZipExactTests.cs | 10 | 0 | 0 |

## Bounded candidates and exact commands

Commands below run the existing self-executing xUnit runner after the parent supplies a matching build. Read-only help was actually inspected on the primary Debug executable: xUnit v3 In-Process Runner 4.0.0 on .NET 10.0.12 supports `-method`, `-class`, `-failSkips` and `-failWarns`; help exits **2 by design**, not a test failure. It does not justify guessing `--filter-method` for this runner. Execute from the repository root that contains the tested build. Each CSV experiment contains its exact fully qualified `-method` command. Theory method filters retain **all** input rows, never only a convenient representative. For any future source perturbation, rebuild its isolated copy using the repository build gate before running a filter; do not point at the parent's unrelated binaries. No command below was run as a test by this lane.

### S1: remove an unnecessary completion relay, retaining the six-case matrix

`StableFunctionRuntimeContractTests.cs:109-122`, `ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion`: replace the completion TaskCompletionSource and ExecuteSynchronously ContinueWith plus separate observation with `Assert.Null(await returned.WaitAsync(TimeSpan.FromSeconds(10), token))`. Preserve subscription waits before Complete, the pending/zero-read/second-stage-zero assertions, exact token assertions, and final callback/read counts. This removes one TCS and one continuation per each of six cases without removing any detector or weakening the bounded completion guarantee. It is a concrete simplify proposal, not executed equivalence evidence.

`dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method "FunnySharp.Tests.StableFunctionRuntimeContractTests.ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion" -failSkips -failWarns`

Old/new witnesses: duplicate each stage's source GetResult, invoke the second stage before the first gated release, and substitute CancellationToken.None on token-aware routes. Both versions must fail the existing exact read/callback/token assertions in completed and pending-stage partitions. Also keep a mutant that never completes to verify the bounded deadline. A canceled outer operation must still fail the null-success assertion; direct awaiting must not swallow faults as the relay did only for signaling.

### S2: establish the promised pending cancellation branch deterministically

`ResultBoundaryTests.cs:305-333,393-398`, `PendingCanceledSourcesPreserveIdentityStackTokenAndStatus`: Task.Yield in CreateCanceledSourceAsync does not hold either source until boundary invocation, so the source may already be canceled when passed into TryAsync/TryValueAsync. Replace this scheduling assumption with explicit release signals created before starting sources; invoke both boundaries, assert incompletion, then release the source coroutine to EDI-throw the same prepared OCE. Await bounded subscription/completion signals rather than sleep. Preserve every original AssertCancellation check: exception identity, ThrowOriginalCancellation stack marker, exact token and canceled status. The helper's implementation ownership remains core-am. This is an assertion-preserving simplify/correction proposal; no code was changed.

`dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method "FunnySharp.Tests.ResultBoundaryTests.PendingCanceledSourcesPreserveIdentityStackTokenAndStatus" -failSkips -failWarns`

Old/new witnesses: mutate only deferred completion to TrySetException for canceled sources, reconstruct the OCE, or reset its original stack. Both versions should reject those faults whenever they actually enter the pending path; the controlled version additionally guarantees that path is reached. Do not claim the current version detects every pending-only fault on every scheduling run.

### Strong overlaps that are not proved redundant

- `NonEmptyTests.ToNonEmptyOrNoneRoundTripsSourceListsThroughToReadOnlyList` overlaps `ToNonEmptyOrNonePreservesOrderForMultipleItems` on ordered [1,2,3] materialization. The candidate also exercises empty conversion through Option.Map and mapped TryGetValue; the proposed owner has direct First/Rest/Count assertions instead. Retain. Compare reordered/truncated materialization, empty-conversion-as-presence, and mapped-presence faults on both methods before proposing any deletion.
- `OptionTests.SomeRejectsNullAndNullableConversionTreatsNullAsAbsence` overlaps `OptionInteropTests.NullableReferencesConvertThroughFactoryAndExtensionSyntax` only for reference FromNullable null/present inputs. Static/struct Some rejection and nullable-value conversion are separate paths; extension syntax is an additional owner obligation, not containment. Retain. Compare null-normalization, both Some guard removals and nullable HasValue faults.
- `UnitResultTests.ToStringDescribesSuccessFailureAndUninitialized` overlaps `DefaultUnitResultThrowsForEveryStateReadingMember` on Uninitialized text only. Success, string/int failure formatting and exception diagnostics are different detectors. Retain. Change each format/status/initialization diagnostic separately and compare.
- The stable source-backed ValueTask matrix rows **451/463/465/506** overlap the completed void/conversion/bind/validation consumption tests in `ValueTaskConsumptionTests`. The dedicated tests assert immediate **pre-await** counts (and different selected payloads 7/5); matrix probes add pending/faulted and subscription/callback assertions but observe final consumption through AsTask. Final counts do not establish immediate consumption containment. Keep both; compare omitted, double and deferred completed-source consumption, mapping corruption and binder-result corruption on corresponding no-token rows before any proposed merge. Token-taking rows remain independent.

For precise two-owner comparisons, use the CSV's full canonical source IDs and OR the corresponding two fully qualified `-method` options; independently record each owner's result, not just the combined exit code. These are the concrete executable owner candidates, not helpers or named future tests. No old/new detector comparison was executed, so every overlap candidate is retain.

## Meaningful retained fault representatives

Use these bounded executable checks in addition to every proposed candidate's unchanged filter. Their perturbations preserve the public requirement rather than changing tests to match a defect:

| Fault representative | Retained method and decisive assertion |
| --- | --- |
| Faulted OCE normalized into cancellation | ResultAsyncTests.AsyncCallbacksKeepFaultedOperationCanceledExceptionsFaulted: eight identities and IsFaulted true / IsCanceled false |
| Duplicate pending source consumption | StableCarrierValueMatrixTests.SourceBackedValueTasksAreConsumedOnceAfterControlledCompletion: 76 rows, subscription handshake and exactly-one reads |
| Drain skipped after terminal default Result | StableParallelDefaultContractTests.DefaultResultStopsAdmissionCancelsAndDrainsBeforePublishingTheFault: held sibling, no early operation completion/disposal, exactly two admissions |
| Validation default inspected too early | StableParallelDefaultContractTests.DefaultValidationIsInspectedOnlyAfterFullAdmissionDrainAndDisposal: all three admissions, uncanceled token and held disposal before fault |
| Completed work releases undelivered ordered capacity | ParallelAsyncEnumerableTests.SelectParallelValueAsyncPreservesOrderAndBoundsUndeliveredWork: degree two window and exact 10,20,30 order |
| Output compaction rollback or early tail clearing on fault | StableSpanFailureContractTests.WhereInPlaceRetainsCompactionAndDoesNotClearTheTailOnPredicateFailure: kept/kept/fault/tail identities and three calls |
| Exact zip stops at first mismatch rather than draining | ZipExactTests.ZipExactFailsWithTheFullCountsWhenLengthsDiffer: 5/3,3/5,0/4,4/0 counts and one callback each |
| Typed-null errors filtered in validation | StableCarrierSyncMatrixTests.Identity513NULLPAYLOADPreservesTheExactRuntimeBehavior: three retained null errors and invalid status |
| Composition ignores multicast invocation entries | StateTransitionTests.ThenPreservesEveryInvocationListEntryOfMulticastTransitions: both complete call logs and last-return outputs |
| Traversal loses equality comparer or location context | TraversalContextTests.ExplicitKeyedTraversePreservesOpaqueEqualityIncludingEmptyResults and NestedLocatedTraverseRendersTheCanonicalCustomersAddressPostalCodePath: comparer identity/lookups and customers[17].addresses[2].postalCode |

Their exact filters are in their CSV rows. Fault perturbations are future isolated experiments only, followed by the unchanged full gate; no successful baseline can replace fault evidence.

## Fixed waits, weak oracles and maintenance burden

No finite Task.Delay, Thread.Sleep, SpinWait, CancelAfter or stopwatch threshold was found in these current files. Infinite cancellable Task.Delay appears only as a cooperative cancellation subscription in the parallel probes, not an elapsed-silence test oracle. Do not remove that token-bound cancellation contract. Signal/WaitAsync deadlines of five, ten or fifteen seconds are bounded liveness watchdogs around explicit signals, not guessed sleeps. Ordinary async iterator Task.Yield is not an ordering assertion; ResultBoundary's pending-path assumption is the material exception noted in S2.

StateTransitionTests.ThenCompositionSupportsConcurrentInvocation uses Task.Run but no overlap barrier: tasks can serialize and a scratch-sharing race can escape. Retain the separate reentrant-concurrency contract; a future correction should register controlled entry/release gates in the transition callback so concurrent entry is observable without timing luck. The parallel traversal drain-success tests release siblings before the final drain assertion; a held completion gate can strengthen that detector, but a semantic sibling drain requirement must not be removed.

OptionTests.ToNullableBridgesValueOptionsWithoutChangingTheirEquality at line 463 requires hashes of None<int> and Some(7) to differ. .NET equality permits collisions; this is an overstrong probabilistic hash-distinctness oracle, not a product requirement. Retain the method and report the assertion for a separate authorized contract correction; never delete equality or equal-value hash consistency detectors to hide it. No collision was observed because no tests were run by this lane.

NonEmptyTests.AggregateFoldsLeftToRightAndCannotRunOnAnEmptySource includes the BCL empty Enumerable.Aggregate contrast; that assertion is not a FunnySharp detector. Its subtraction/string/singleton fold assertions still protect product behavior, so the method remains. PackageBoundaryTests uses an ordinal System. name prefix as its platform oracle: a non-platform assembly with that prefix can escape; current direct-reference detection remains meaningful and does not prove package provenance.

No test in this lane computes SHA or pins product prose/source text. PerformanceManifestTests validates **machine-consumed** input lists, membership, separators and strict ordinal uniqueness; these are evidence-fingerprint input obligations, not meaningless source-shape locks. Exact carrier diagnostics and initialization messages are observable runtime assertions. Stable consumption WriteLine evidence strings are emitted, not pinned by tests. ThrowOriginalCancellation/ThrowCancellation markers are meaningful exception-stack diagnostics, not mere production-source spelling locks.

ProbeEnumerable's iterator-finally count can advance on natural exhaustion even if explicit Dispose is removed. Whole-source exact-zip probe tests therefore do not establish disposal-call identity; early-break, competing-fault and custom enumerator tests provide distinct cleanup detectors. Retain them rather than assuming count equivalence. The matrix gates, CountingValueTaskSource, StableEffectPendingSource and completed void source share a BCL core but differ in pending/fault injection, pre-await timing, repeat rejection, atomic counters, subscription counts and void adapters; helper merging is unproved. The CheckToken forwarding wrapper is a bounded potential helper cleanup owned by core-am, not proof that token-aware tests are redundant.

Workloads of 100,000 items, 10,000 transition leaves, repeated immediate pulls (32 x 64), and repeated/concurrent compositions carry scale, recursion, admission or reentrancy detectors. Their elapsed time was not measured here; do not shrink them for runtime savings without detector evidence. Allocation budgets remain blocking under the current performance policy; hosted timing is directional. S1 removes only relay maintenance/allocation overhead. S2 adds deterministic scheduling control, not a reduced workload. Retaining 1,500 declared cases is justified by their particular overload, payload, fault-precedence, lifecycle and input boundaries rather than the case count itself.

## Validation and limits

Report validation checks quoted CSV parsing, exact headers, unique IDs, source method/line matches, one row for each of 569 methods, all 66 theory methods and 997 literal input rows, 144 unique helper rows, disposition counts, and inclusion of all 69 helper types plus 72 helper methods and three external definitions. It also checks that edits are restricted to the three assigned reports. No builds or test runs were duplicated; no mutants or old/new ablations were executed. Full-suite baseline results are the parent's responsibility. Consequently **zero proved deletion/merge candidates** is a source-grounded result, not a claim that every conceivable detector is unique. Future experiments have exact filters and faults; they must be run before any claimed detector removal.

