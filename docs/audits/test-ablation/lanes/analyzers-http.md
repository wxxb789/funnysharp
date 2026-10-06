# Analyzer and HTTP test-ablation assessment

## Result and authority

Baseline: 0c5416e37e06db17c30c014659601d0c463b973a, branch ulw/test-ablation-g001, worktree Q:/repos/funnysharp/.omo/worktrees/test-ablation-g001. The initial worktree status was clean. This lane owns tests/FunnySharp.Analyzers.Tests and tests/FunnySharp.AspNetCore.Tests. It wrote only the three assigned reports. No test, implementation, dependency, frozen evidence, or git history was modified.

All 111 current source test methods have inspected bodies/assertions and a disposition: 96 retain, 15 simplify, 0 merge, 0 delete. Helpers: 28 owned units (13 standalone/nested types and 15 named supporting functions), plus 2 linked-definition usage rows owned by core-am. Owned helper dispositions: 27 retain, 1 simplify. No ownership gap was found.

The chosen approach is detector-set assessment, not similarity-based deletion. Source resemblance and repeated setup suggest work to validate, but cannot prove detector containment. Each CSV row gives a concrete fault class, inspected assertion rationale, preserved input partitions, and a method-filtered executable witness plan. No merge/deletion is promoted without independent remaining owners and old/new fault rejection evidence. No mutations or witness executions were authorized in this report-only lane, so all experiment fields are explicitly PLAN ONLY. Parent owns the unchanged full-solution gate in the primary checkout; this lane did not duplicate it or claim its result.

## Contracts and implementation actually inspected

Read root, docs, both test-directory, analyzer and HTTP AGENTS.md; Directory.Build.props; both test csproj files; global.json; docs/product-contract.md, docs/grammar.md, docs/analyzers.md, docs/aspnet-core.md and docs/harness.md. Product requirements retained: no shipping runtime compiler dependency, five suppressible stable-ID diagnostics, low false-positive boundaries and safe fixes; explicit caller HTTP policy; no global error conversion; exact RequestAborted forwarding; ordinary async exception/cancellation semantics; source-backed ValueTask single consumption; compiler nullable payload fidelity. Allocation/performance requirements remain unchanged and were not measured or relaxed here.

Read every local test/helper body, all five analyzer implementations, FunnySharpWellKnownTypes, Descriptors, DiagnosticIds, both code-fix providers, all 20 public HttpResultExtensions overloads and their core/guard helpers. Inspected the relevant Option/Result/Validation/UnitResult TryGet and Option.Some annotations, uninitialized guards and Effect FromValue/RunAsync behavior. Git log and bounded blame confirmed that 06f3962 introduced the stable HTTP pending-source and faulted-OCE regressions; historical test counts/material were not treated as current proof.

## Enumeration and completeness

The test CSV key is repo-relative source path with / separators plus :: plus exact source method name. The line is the method declaration line. There is exactly one row per attributed source method, not per InlineData. Every theory's complete literal InlineData list is preserved in obligation; internal table/loop partitions are described in rationale. The headers are the requested schema; all CSV values are RFC4180-escaped quoted fields.

| Source file | Methods | Facts | Theories | Discovered input cases |
| --- | ---: | ---: | ---: | ---: |
| AnalyzerPackagingTests.cs | 3 | 2 | 1 | 7 |
| BlockedValueTaskAnalyzerTests.cs | 13 | 9 | 4 | 57 |
| CodeFixTests.cs | 9 | 7 | 2 | 25 |
| DiscardedOutcomeAnalyzerTests.cs | 20 | 20 | 0 | 20 |
| ExampleProjectsAnalyzeCleanTests.cs | 3 | 3 | 0 | 3 |
| IgnoredTryGetResultAnalyzerTests.cs | 9 | 9 | 0 | 9 |
| StableNullabilityContractTests.cs | 5 | 3 | 2 | 9 |
| SyncDisposeAnalyzerTests.cs | 7 | 7 | 0 | 7 |
| UninitializedCarrierAnalyzerTests.cs | 15 | 12 | 3 | 31 |
| HttpResultExtensionsTests.cs | 17 | 17 | 0 | 17 |
| KestrelCancellationTests.cs | 1 | 1 | 0 | 1 |
| StableHttpContractTests.cs | 2 | 1 | 1 | 3 |
| UnitResultHttpResultExtensionsTests.cs | 7 | 7 | 0 | 7 |
| Total | 111 | 98 | 13 | 196 |

Analyzer subtotal: 84 methods, 72 Facts, 12 Theories, 168 input cases. HTTP subtotal: 27 methods, 26 Facts, 1 Theory, 28 input cases. These are source enumeration counts, not a test-run receipt. EmptyInitializersDoNotInitializeNondefaultableCarriers contains an additional three-creation loop per eight theory inputs: 24 independent snippet compilations/assertions, preserved in its row. Other meaningful internal matrices include 21 HTTP async argument guards, 8 Unit guards, 8 faulted-OCE mappings, and 4 carrier checks in each completed/pending ValueTask partition.

Compile inputs: SDK default local *.cs inclusion produces 11 analyzer source files and 4 HTTP source files. Analyzer local inputs: AnalyzerHarness.cs, AnalyzerTestAssert.cs, AnalyzerPackagingTests.cs, BlockedValueTaskAnalyzerTests.cs, CodeFixTests.cs, DiscardedOutcomeAnalyzerTests.cs, ExampleProjectsAnalyzeCleanTests.cs, IgnoredTryGetResultAnalyzerTests.cs, StableNullabilityContractTests.cs, SyncDisposeAnalyzerTests.cs, UninitializedCarrierAnalyzerTests.cs. HTTP local inputs: HttpResultExtensionsTests.cs, KestrelCancellationTests.cs, StableHttpContractTests.cs, UnitResultHttpResultExtensionsTests.cs. No local Program.cs/custom executable-check entrypoint or extra source file was found; OutputType=Exe uses xUnit v3/Microsoft.Testing.Platform generated entrypoints. SDK-generated global usings/assembly metadata are not hand-written detector methods.

Explicit analyzer Compile Include ../Shared/TestRepositoryRoot.cs links Shared/TestRepositoryRoot.cs; explicit HTTP Compile Include ../FunnySharp.Tests/CountingValueTaskSource.cs links TestSupport/CountingValueTaskSource.cs. Both definitions were inspected; core-am owns their disposition, and the helper CSV records usage only. TestRepositoryRoot supports all three example-source dogfood checks. CountingValueTaskSource supports the completed Option wire test and all four carriers in the completed/pending theory. There is no linked shared test method outside these ownership units.

The helper CSV includes every standalone helper file, every actual nested supporting type, and each named nontrivial supporting function. Trivial members/constructors of a helper type are assessed in its type row. Raw-string compilation-probe declarations are inputs inside their containing test, not separately compiled helper definitions or omitted source tests.

## Bounded candidate edits

### A. Remove eleven analyzer prose-fragment locks

Scope: AnalyzerTestAssert.cs:11-19 and the 11 simplify rows that call DiagnosticAsync with a messageFragment. Remove only that optional parameter and its conditional message Assert.Contains, plus the 11 corresponding arguments. Keep exactly-one diagnostic, exact ID and compilation validation. This avoids pinning explanatory English while retaining all direct/stored/configured/task/ValueTask/default/TryGet/dual-resource detector sources. Expected maintenance reduction: one conditional assertion branch and 11 wording dependencies; runtime savings are unmeasured and likely small because compilation dominates.

Retained representatives: DefaultResultExpressionIsReported; DiscardedUserCarrierCallIsReported; UnawaitedValueTaskFromFunnySharpMemberIsReported; AwaitedStoredTaskOfOutcomeIsReported; AwaitedConfiguredTaskOfOutcomeIsReported; IgnoredOptionTryGetValueIsReported; ResultOnDirectValueTaskIsReported; GetAwaiterGetResultOnDirectValueTaskIsReported; BothInterfacesIsReported. For each, replay its CSV mutant on old and simplified assertions and require both to fail. Separately reword only the descriptor message: old fragment assertions may fail while the simplified suite must pass. This planned contrast would establish that removed prose detectors are noncontractual, not that diagnostic sources are redundant. No severity/span/ID assertion is removed.

### B. Remove five UnitResult exception-text locks

Scope: UnitResultHttpResultExtensionsTests.cs:259-261 and :308-309. Keep three InvalidOperationException boundaries, the ordinary fault reference identity, all eight eager guard/ParamName entries, null/statusless-problem rejection and null-success-result rejection. These five English Equal assertions do not test a machine-consumed field and add wording maintenance. No test method is deleted and no guard exception type is broadened.

Old/new witnesses: bypass UnitResult initialization guard; swallow/wrap ordinary Task fault; defer a required mapper guard into its async core; accept statusless/null ProblemDetails. Each must be rejected by both versions. A message-only change in UnitResult or ValidateProblem must pass the proposed version. Savings: five assertion calls/text pins, not HTTP startup or product work. All witnesses remain unexecuted plans.

### C. Replace two code-fix formatting assertions with semantic RHS checks

Scope: CodeFixTests.cs:31 and :55. Keep ApplyFirstFixAsync, actual IDiscardOperation target, compiling fixed source and quiet analysis. Replace Contains spacing locks with exactly one expected Find/SaveAsync invocation as the assignment RHS, requiring the AwaitExpression/await operation in the awaited case. This is not permission to remove the RHS check: QuietAsync plus discard target alone could accept a replaced/deleted original expression.

Old/new faults must both reject no offered fix, underscore variable assignment, replaced RHS and removed await. Whitespace-only fixed output should pass the new semantic oracle. Comment preservation checks at :159-166 and :370 verify shipped transformation fidelity and remain. Expected burden: less formatting brittleness, potentially another Roslyn semantic inspection rather than a runtime saving.

All 15 simplify methods, with their full command/witness plans, are:

- `tests/FunnySharp.Analyzers.Tests/BlockedValueTaskAnalyzerTests.cs::ResultOnDirectValueTaskIsReported`
- `tests/FunnySharp.Analyzers.Tests/BlockedValueTaskAnalyzerTests.cs::GetAwaiterGetResultOnDirectValueTaskIsReported`
- `tests/FunnySharp.Analyzers.Tests/CodeFixTests.cs::DiscardedOutcomeFixMakesTheDiscardExplicit`
- `tests/FunnySharp.Analyzers.Tests/CodeFixTests.cs::DiscardedOutcomeFixPreservesAwaitedStatements`
- `tests/FunnySharp.Analyzers.Tests/DiscardedOutcomeAnalyzerTests.cs::DiscardedUserCarrierCallIsReported`
- `tests/FunnySharp.Analyzers.Tests/DiscardedOutcomeAnalyzerTests.cs::UnawaitedValueTaskFromFunnySharpMemberIsReported`
- `tests/FunnySharp.Analyzers.Tests/DiscardedOutcomeAnalyzerTests.cs::AwaitedTaskOfOutcomeIsReported`
- `tests/FunnySharp.Analyzers.Tests/DiscardedOutcomeAnalyzerTests.cs::AwaitedStoredTaskOfOutcomeIsReported`
- `tests/FunnySharp.Analyzers.Tests/DiscardedOutcomeAnalyzerTests.cs::AwaitedConfiguredTaskOfOutcomeIsReported`
- `tests/FunnySharp.Analyzers.Tests/DiscardedOutcomeAnalyzerTests.cs::AwaitedTaskOfNonOutcomeFromFunnySharpMemberIsReported`
- `tests/FunnySharp.Analyzers.Tests/IgnoredTryGetResultAnalyzerTests.cs::IgnoredOptionTryGetValueIsReported`
- `tests/FunnySharp.Analyzers.Tests/SyncDisposeAnalyzerTests.cs::BothInterfacesIsReported`
- `tests/FunnySharp.Analyzers.Tests/UninitializedCarrierAnalyzerTests.cs::DefaultResultExpressionIsReported`
- `tests/FunnySharp.AspNetCore.Tests/UnitResultHttpResultExtensionsTests.cs::UninitializedUnitResultThrowsFromSyncAndAwaitedMappings`
- `tests/FunnySharp.AspNetCore.Tests/UnitResultHttpResultExtensionsTests.cs::UnitResultGuardsRejectNullMappersAndInvalidProblems`

## Hypotheses not proved; retain current owners

1. HttpResultExtensionsTests.ValueTaskCarrierIsConsumedExactlyOnce overlaps the completed Option count check in StableHttpContractTests.HttpValueTaskCarriersAreConsumedOnceForCompletedAndPendingInputs. The latter is a concrete independent executable owner for counting, but not for the wire 200 status: it only checks nonnull IResult. A mutant returning a nonnull 500 after exactly one consume is a counterexample to full containment. Deletion remains unproved. If later moving its count assertion into the stable matrix, preserve the wire owner and execute both the double-consumption and wrong-status mutants before/after.
2. Basic safe-discard fix tests overlap one row in DiscardedOutcomeFixIsOfferedOnlyForATrueDiscard. The theory's action cardinality is stronger on one dimension, while the basic awaited expression preservation is currently textual and the theory does not assert identical RHS behavior. No method deletion is justified before semantic preservation witnesses run. Keep all six safe scope partitions and all twelve unsafe underscore partitions.
3. HttpResultExtensionsTests.TestApplication (:663-683) and UnitResultHttpResultExtensionsTests.TestApplication (:333-353) duplicate the same 21-line constructor/start/dispose structure. A common fixture could remove roughly one copy, but the existing private sibling is not an accessible remaining owner; a new shared fixture would require explicit integration and old/new wire, start/stop and cancellation witnesses. No helper merge/delete is proved. KestrelApplication uses actual TCP and cannot join this TestServer extraction.
4. Three NotFound helpers and paired structurally similar DomainError/Payload/EndpointEnvironment records are small private fixture repetition. Sharing would widen ownership/visibility and does not reduce HTTP host startup. No independently executed containment or cost result exists; keep them.

## Fault representatives and execution protocol

Run from the audited worktree root in a separately authorized disposable mutation checkout; do not edit frozen evidence or the assessed worktree for these plans. Every CSV row supplies a concrete mutant and exact method filter. Use xUnit v3's method filter via the self-executing project, not VSTest FullyQualifiedName filters under the pinned Microsoft.Testing.Platform runner. Example exact commands:

```shell
dotnet run --project tests/FunnySharp.Analyzers.Tests/FunnySharp.Analyzers.Tests.csproj -c Release -- --filter-method FunnySharp.Analyzers.Tests.BlockedValueTaskAnalyzerTests.UnprovenFastPathsKeepExactDiagnostics
dotnet run --project tests/FunnySharp.Analyzers.Tests/FunnySharp.Analyzers.Tests.csproj -c Release -- --filter-method FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeBatchFixChangesOnlySafeStatements
dotnet run --project tests/FunnySharp.AspNetCore.Tests/FunnySharp.AspNetCore.Tests.csproj -c Release -- --filter-method FunnySharp.AspNetCore.Tests.StableHttpContractTests.HttpValueTaskCarriersAreConsumedOnceForCompletedAndPendingInputs
dotnet run --project tests/FunnySharp.AspNetCore.Tests/FunnySharp.AspNetCore.Tests.csproj -c Release -- --filter-method FunnySharp.AspNetCore.Tests.StableHttpContractTests.HttpAwaitTurnsFaultedOperationCanceledExceptionIntoCancellation
dotnet run --project tests/FunnySharp.AspNetCore.Tests/FunnySharp.AspNetCore.Tests.csproj -c Release -- --filter-method FunnySharp.AspNetCore.Tests.KestrelCancellationTests.ClientDisconnectCancelsRequestAbortedAndForwardsItsExactTokenToEffect
```

For each candidate, record old source + relevant product fault = red and proposed assertions + the identical fault = red; require unchanged baseline and proposed no-fault controls green. A passing suite after deleting checks is not evidence. For any future merge/delete require D_removed to be contained in the union of concrete independent remaining detector subsets, including all source forms, case partitions, status/headers/body, identity, cancellation state and eager validation. The present lane claims no such completed experiment.

Representative faults that must remain meaningfully detected: FS1001 empty initializer bypass; FS1002 configured/stored awaited outcome loss; FS1003 ignored named output; FS1004 unsound same-value completion proof, double consumption and escape/reassignment; FS1005 wrong resource index in environment overload; unsafe underscore code fix and unsafe Fix All; compiler unsound nullable annotations; HTTP dropped problem fields/order, wrong Unit 204, faulted-OCE state drift, pending ValueTask double consumption, RequestAborted replacement and real TCP disconnect cancellation.

## Timing, cost and residual findings

No fixed sleep or polling delay is executed in owned tests. Task.Delay(0) is only in a Roslyn source string. Infinite Task.Delay in three cancellation operations is the operation under test, canceled only after subscribed start signals; it is not a fixed wait to hope the server started. Kestrel registers cancellation before disconnect, uses an ephemeral loopback port and waits on request/effect events. Ten/30-second bounds are failure deadlines, not synchronization guesses. Code-fix Fix All uses bounded cancellation. Stable pending ValueTask completion triggers the already installed await continuations without polling.

No SHA/hash assertion or documentary source-shape lock exists in these owned tests. AssertDiscardTargets/diagnostic spans and comment trivia are semantic/transformation obligations. HTTP serialized title/detail strings are caller-selected wire fields, not explanatory docs prose. The 11 diagnostic fragments and 5 Unit exception-text checks above are the identified noncontractual prose locks.

Current suite cost includes 168 analyzer discovery cases with real in-memory Roslyn compilations/fix work and three real example compilations, plus 28 HTTP cases including TestServer per-method startup and one real Kestrel TCP case. No measured elapsed-time receipt exists for this lane. Compiler reference construction is already cached by AnalyzerHarness; lowering compiler realism or removing real transport would reduce detection, not simply overhead. Fixture extraction reduces duplicated maintenance but not the number of host starts. Simplification does not claim lower product allocation requirements.

Completeness was verified by comparing all parsed current Fact/Theory method identities/lines with CSV rows and all theory literals with obligation text; uniqueness, headers/field cardinality, quoted-field round trip and helper units were checked. No omitted owned method/helper or Compile input remains. Missing runtime evidence is explicit: filtered plans and old/new fault experiments were not run because authority is report-only and parent owns baseline execution. Parent full gate result must be joined separately.

Residual coverage boundaries, not ownership omissions: current owned suppression witness is only FS1001 pragma; no dedicated NoWarn/editorconfig/SuppressMessage tests were found here despite documented support. Packaging reflection verifies absence of runtime tooling references, not actual nupkg embedding. Example ReadProjectSources enumerates top-level *.cs only, not evaluated recursive project Compile items. These do not authorize weakening or deleting existing detectors, and no unrelated repair was made.
