---
title: "fix: Resolve the Goal 24 audit findings"
type: fix
date: "2026-10-01T03:03:53.580Z"
artifact_contract: ce-unified-plan/v1
product_contract_source: "docs/audits/goal-24/criterion-matrix.csv; docs/goals/0024-goal.md; docs/goals/archive/0013-goal.md; accepted Goals 14-23"
execution: code
---

# Goal 24 Audit Resolution

## Goal Capsule

**Objective:** Consumers and release reviewers can verify the current candidate resolves all 40 FAIL and 29 UNVERIFIED judgments without losing the original audit or inventing evidence.

**Means:** Repair narrow shared mechanisms first, make measurement and generation evidence trustworthy, then validate the final packages and authorized release surfaces (KTD1-KTD6).

**Authority:** Current user decisions, immutable accepted goals, current product contract and grammar, and the original Goal 24 matrix. Historical audit completion is not product acceptance.

**Execution profile:** Heavy, evidence-led delivery in one isolated repair branch; independent bounded units may run in parallel, overlapping writes serialize.

**Tail ownership:** The main session owns integration, manual QA, exact 69-row closure and final review. Local commits/merge, push/PR/CI, GitHub ruleset/proof operations and external index writes require explicit owner authorization; none is inferred from permission to edit.

**Stop conditions:** Stop immediately when every original finding has an accepted verified resolution at the frozen candidate, applicable gates and real surfaces are clean, and the complete ledger/report is delivered. Stop for an owner decision when source equality cannot be inferred, actual comparative outcomes contradict required ambition, release authority/access is unavailable, or a release feed/version conflict is observed.

## Product Contract

### Problem Frame

The frozen audit evaluated 311 clauses: 242 PASS, 40 FAIL and 29 UNVERIFIED. These are overlapping judgments, not69 independent defects. Its audited commit is a8863473fd53eddc7cb47201508426db2e454f84 and tree 88e8a528017b0a5f68710cc1d955fda64486877f. Reusing its 1,099-test/49-consumer-test PASS cannot prove changed package behavior or repair missing history.

### Requirements

- R1. Account for every original FAIL/UNVERIFIED ID once in a separate current-resolution ledger, retaining the original audit and archived contracts.
- R2. Preserve existing lazy Recover and Lens.Compose(Optional) APIs while accurately superseding current authority prose and completing stable vocabulary.
- R3. Valid keyed traversal preserves the accepted key-equality policy and accepts empty-string location keys; arbitrary opaque equality requires an explicit owner-set contract.
- R4. Preserve cancellation before ordered publication; bound FirstSuccess admission, handle every completion once, propagate independent faults and drain all started work, retaining Validation and existing signatures.
- R5. Packaged analyzers diagnose empty invalid initializers, offer only semantically safe discard fixes and keep proven completed/single-consumption ValueTask paths quiet.
- R6. Actual typed reconciliation 500 appears in the same application OpenAPI; official slice verification restores through a faithful isolated package-source configuration.
- R7. Current performance characteristics, equivalent baselines, source/workload binding and available raw receipts support every claim while preserving allocation ceilings.
- R8. Equivalent function studies and fresh independent AI runs retain complete inputs, corrections, outcomes and attributable review; negative results remain negative.
- R9. Every stable public member has intentional names, nullability, exceptions, sync/async relationships and adequate shipped XML joined to package/source/test evidence.
- R10. Required release contexts enforce merge denial without bypass and exact immutable package/attestation/index evidence is retrievable and candidate-bound.

### Accepted Decisions

- D1. Fresh traceable evidence may supplement missing historical records while preserving their gaps (session-settled: user-directed - chosen over requiring unrecoverable original records). Governs R1, R7, R8.
- D2. Keep current Recover and partial-optic API behavior and formally correct current documents (session-settled: user-directed - chosen over changing public APIs to match incorrect old prose). Governs R2.
- D3. FirstSuccess is bounded and independent loser cleanup faults propagate even after a winner (session-settled: user-directed - chosen over start-all/ignore-loser-fault semantics). Governs R4.

### Scope Boundaries

**Included:** All69 original judgments and necessary verification dependencies, including the observed slice restore defect and evaluation provenance/oracle integrity.

**Outside this product identity:** New carriers, Where on carriers, general lifetime analyzers, custom runtimes/schedulers, replacement collections, convenience APIs to manufacture density wins, net11.0 and unrelated cleanup. Package publication is not requested.

### Open Owner Questions

- O1. Accept explicit-comparer overloads for opaque dictionaries while preserving supported publicly available comparers; requested during initial investigation. U2 waits on this contract.
- O2. Preserve broad measured ambition or formally qualify current promises if the honest comparisons do not establish them; requested during initial investigation. Until decided, keep original acceptance and retain unresolved outcomes.
- O3. Explicit authority for local atomic commits/integration and separately push/PR/CI, rulesets/deliberate blocking proof and external P/A/I index publication. Local repair continues independently.
- O4. Attributable maintainer review of exact fresh variants and complete intended distribution-feed inventory are required for final acceptance; AI gate review is not maintainer acceptance.

## Planning Contract

### Key Technical Decisions

- KTD1. Preserve historical inputs and create a separate source/package-bound successor ledger; never relabel old output. Implements D1/R1.
- KTD2. Fix defects at shared ownership layers, pair each change with its regression and package proof, and avoid broad abstractions. Governs R3-R6.
- KTD3. Keep finite eager FirstSuccess snapshot semantics, use indexed completion notifications, and adopt a documented fixed compatibility cap 32 with an unambiguous explicit-bound additive path. The cap is policy, not tuning evidence. Implements D3/R4.
- KTD4. Complete concrete cost/API crosswalks rather than pretending a family-level benchmark or XML count covers all members. Governs R7/R9.
- KTD5. Freeze trusted oracles, curated guide snapshots and receipt protocols before measuring or generating; keep failed/capped runs and all comparative losses. Implements D1/R8.
- KTD6. Keep release producer evidence P, independent attestation A and external index I as a one-way immutable DAG with actual owner authority and matching-host canonical packages. Governs R10.

### High-Level Technical Design

```text
accepted contracts + original 69 IDs
           |
           v
narrow source/analyzer/HTTP repairs ---- trusted harness + equivalence/binding repairs
           |                                      |
           +-------------- package proof ---------+
                                  |
                  frozen packages/docs/oracles
                           /              \
           performance repetitions       independent AI/workflow study
                           \              /
                     complete semantic/evidence crosswalk
                                  |
               authorized final candidate + four-host release/enforcement
                                  |
                         immutable P -> A -> external I
                                  |
                     69-ID closure + main QA + gate review
```

FirstSuccess lifecycle is Snapshot -> Admit(up toK) -> Observe indexed ready batch -> Refill or Select -> Cancel -> Drain candidates/observers -> Publish. Each candidate is started, consumed and accounted at most once; only the final typed-error pass visits all input positions. Winner observation never conceals prior or later independent faults.

Evidence lifecycle is Planned -> Failing/Missing witness -> Repair -> Real-surface capture -> Cleanup receipt -> Candidate/tree-bound verdict. Changed tracked inputs invalidate dependent proof; new artifacts are captured, never relabeled.

### Assumptions And Ordering

- A local 0.2.0 preview candidate follows the existing versioning policy for additive APIs and documented behavior changes; it is not published by this task.
- Preserve the .NET10/BCL-only core and embedded analyzer dependency boundaries.
- Reuse repository entry points; new studies are development evidence, not shipping dependencies or alternative harnesses.
- Independent source units U1/U4/U5/U7 and the two U8 scopes can start after the plan; U2 follows O1. U2/U3 share tests and U4/U6 share tests, so those writes serialize.
- U9/U10/U12 evidence mechanisms follow settled source semantics; U11/U13/U14 use frozen final packages. U15 requires all material local outcomes and remote authority. U16 runs last.
- Do not benchmark while other lanes build/test on this host. Measurements are serial and retain full receipts.
- No timers, polling delays or timed absence in tests unless time itself is the contract; subscribe before triggering and use timeouts only as bounded failure detection.

## Implementation Units

### U1. Correct the accepted grammar and remediation authority

**Goal:** Correct the accepted grammar and remediation authority.

**Requirements:** R1, R2.

**Dependencies:** None.

**Files:** docs/goals/0025-goal.md; docs/product-contract.md; docs/grammar.md; docs/release-notes.md; src/FunnySharp/FunnySharp.csproj; src/FunnySharp.AspNetCore/FunnySharp.AspNetCore.csproj.

**Approach:** Retain lazy value-producing Recover and partial optic composition. Complete stable operational vocabulary, including span/memory mutation, Try boundaries, FromBoolean and Identity. Record the accepted follow-up rather than changing archived goals. Prepare a 0.2.0 local preview candidate because additive APIs and changed documented concurrency behavior are not patch-release changes.

**Execution note:** Capture the applicable failing-first or missing-evidence condition before repair, then verify the same observable contract.

**Patterns to follow:** Existing carrier/optic source XML, authoritative naming table, and versioning policy.

**Test scenarios:** Package calls show success skips Recover, failure invokes it once, Lens plus Optional yields Optional, and every stable workflow verb has an accurate table entry. Prose receives review rather than wording-pinning tests.

**Verification:** Source/signature inventory, existing semantic tests, docs snippets and API baseline after the additive units.

### U2. Preserve keyed traversal equality explicitly

**Goal:** Preserve keyed traversal equality explicitly.

**Requirements:** R3.

**Dependencies:** Opaque-source decision; U1 for accepted API additions.

**Files:** src/FunnySharp/SequenceExtensions.cs; tests/FunnySharp.Tests/TraversalContextTests.cs; tests/FunnySharp.Tests/AdvancedPatternCurationTests.cs; docs/collections.md; docs/stability-inventory.md.

**Approach:** Preserve publicly available equality comparers for supported BCL dictionaries and add explicit non-null comparer paths for three value-producing carriers and their located variants. Thread the comparer through empty and populated results. Opaque source equality cannot be inferred; document the accepted fallback/explicit requirement. No comparer parameter on valueless UnitResult and no private reflection.

**Execution note:** Capture the applicable failing-first or missing-evidence condition before repair, then verify the same observable contract.

**Patterns to follow:** Lazy success buffers and existing located forwarding overloads.

**Test scenarios:** Reference-distinct equal-text keys remain distinct; case-insensitive lookup survives; opaque wrappers work through an explicit comparer; empty results retain policy; Option/Result short-circuit and Validation accumulates. Compile both old and new overload shapes.

**Verification:** Failing-before regression, focused core tests, packed C# consumer and trim/AOT compatibility.

### U3. Accept empty string dictionary locations

**Goal:** Accept empty string dictionary locations.

**Requirements:** R3.

**Dependencies:** None; combine final located probes with U2.

**Files:** src/FunnySharp/Location.cs; tests/FunnySharp.Tests/LocationTests.cs; tests/FunnySharp.Tests/TraversalContextTests.cs.

**Approach:** Reject null but allow an empty string in Key(string). Keep Property nonempty, location equality/rendering and experimental status. Serialize the shared TraversalContextTests write with U2.

**Execution note:** Capture the applicable failing-first or missing-evidence condition before repair, then verify the same observable contract.

**Patterns to follow:** Existing string versus object key rendering and location guards.

**Test scenarios:** Empty key renders [""]; boxed and string forms agree; Root remains distinct; all four located keyed carriers call their selectors and preserve outcomes; null keys and empty properties still reject.

**Verification:** Focused tests and packed all-carrier located traversal scenarios.

### U4. Prevent ordered publication after cancellation

**Goal:** Prevent ordered publication after cancellation.

**Requirements:** R4.

**Dependencies:** None.

**Files:** src/FunnySharp/ParallelAsyncEnumerableExtensions.cs; tests/FunnySharp.Tests/ParallelAsyncEnumerableTests.cs.

**Approach:** Await into a local, check operation cancellation before Current assignment and cleanup-registry removal, then retain existing cancellation mapping and drain. Do not return early while non-cooperating work remains undrained.

**Execution note:** Capture the applicable failing-first or missing-evidence condition before repair, then verify the same observable contract.

**Patterns to follow:** Existing primary failure, exact caller-token mapping, StartedWork identity, and cleanup aggregation.

**Test scenarios:** Synchronous source and pending selector ignoring cancellation: cancel then release; no value publishes, caller token survives and cleanup happens once. Cover token-aware/unaware selectors, completion-order control and independent cleanup faults.

**Verification:** Same failing-before/passing-after direct-enumerator package probe, focused tests and no outstanding work.

### U5. Bound FirstSuccess with linear completion accounting

**Goal:** Bound FirstSuccess with linear completion accounting.

**Requirements:** R4.

**Dependencies:** U1. U10 follows this unit and is not its prerequisite.

**Files:** src/FunnySharp/ConcurrentEffectExtensions.cs; tests/FunnySharp.Tests/FirstSuccessTests.cs; docs/concurrency.md; examples/FunnySharp.DocumentationSamples/ConcurrencySamples.cs if snippets change.

**Approach:** Keep Validation and all existing signatures. Retain finite one-pass eager snapshot semantics. Use a documented compatibility cap 32 and one additive explicit-bound overload whose required arguments cannot rebind existing default-literal calls. Admit at most K unobserved candidates, enqueue one indexed completion per candidate, account once, stop new admission after selection, cancel and drain. Propagate independent faults observed before selection and during cleanup; only identifiable internal cancellation artifacts are suppressed.

**Execution note:** Capture the applicable failing-first or missing-evidence condition before repair, then verify the same observable contract.

**Patterns to follow:** Controllable ValueTask sources, manual TimeProvider, cold Effect execution and BCL Channel coordination.

**Test scenarios:** Legacy and explicit bounds with 1,024 held candidates; positive refill events; all typed failures in input order; winner plus pending disposal fault; fault already observed before winner; caller cancellation/timeout precedence; shared Tasks with distinct work identities; one consumption; large synchronous/staggered inputs with K fixed andK=N.

**Verification:** Static linear-work invariant plus public package cases, focused concurrency tests, equivalent bounded benchmark preflight and unchanged allocation budgets.

### U6. Replace timed ordering evidence with causal signals

**Goal:** Replace timed ordering evidence with causal signals.

**Requirements:** R4.

**Dependencies:** U4; shared test file is serialized.

**Files:** tests/FunnySharp.Tests/ParallelAsyncEnumerableTests.cs.

**Approach:** Remove SourceOrderGracePeriod and timed absence. Use a synchronous two-item source with a completed second selector and a positive source-exhaustion signal before releasing the first selector. Test the public ordered and completion-order routes, both selector overloads, and always release/drain in cleanup.

**Execution note:** Capture the applicable failing-first or missing-evidence condition before repair, then verify the same observable contract.

**Patterns to follow:** Synchronous ProbeAsyncEnumerable and existing asynchronous-continuation gates.

**Test scenarios:** The source-order route yields10 then20; a deliberately wrong route must yield20 first and fail without elapsed silence. Completion-order control confirms the fixture distinguishes routing.

**Verification:** Single reliable focused run and packed ordering witnesses with bounded failure timeouts only.

### U7. Repair packaged analyzer diagnostics and safe fixes

**Goal:** Repair packaged analyzer diagnostics and safe fixes.

**Requirements:** R5.

**Dependencies:** None; documentation consolidation follows all three repairs.

**Files:** src/FunnySharp.Analyzers/UninitializedCarrierAnalyzer.cs; src/FunnySharp.Analyzers/BlockedValueTaskAnalyzer.cs; src/FunnySharp.Analyzers.CodeFixes/DiscardedOutcomeCodeFixProvider.cs; corresponding tests/FunnySharp.Analyzers.Tests files; AnalyzerHarness.cs; docs/analyzers.md; affected XML/unshipped notes.

**Approach:** FS1001 recognizes empty initializers. FS1002 registers a fix only if the candidate assignment semantically targets IDiscardOperation, otherwise leaves the diagnostic without an unsafe action. FS1004 suppresses only a provably completed-success, same-value, single-consumption local/parameter path; unsupported or escaped values remain diagnosed. Keep a bounded local proof, not a general lifetime analyzer. Split the three independent file scopes, then consolidate docs.

**Execution note:** Capture the applicable failing-first or missing-evidence condition before repair, then verify the same observable contract.

**Patterns to follow:** Compilation-start type binding, Roslyn operation analysis, real CodeFixContext and candidate recompilation.

**Test scenarios:** All eight nondefaultable carrier initializer forms diagnose; Option/default-valid controls remain quiet. Local/parameter/member underscore binding gets no unsafe action; safe awaited/comment-bearing fixes compile and truly discard. Correct completed branches are quiet; wrong receiver, reassignment, duplicate consumption, escape and unguarded paths still warn.

**Verification:** Focused analyzer suite, changed-file diagnostics, example dogfooding, SDK PackageReference negative controls and real packaged code-fix compilation.

### U8. Make reconciliation and official consumer verification faithful

**Goal:** Make reconciliation and official consumer verification faithful.

**Requirements:** R6.

**Dependencies:** Harness and endpoint edits are independent; final package checks follow U1-U7.

**Files:** tests/FunnySharp.VerticalSlice/FunnySharp.VerticalSlice.Api/Endpoints/OrderEndpoints.cs; tests/FunnySharp.VerticalSlice/FunnySharp.VerticalSlice.Tests/OpenApiTests.cs and narrow test support; eng/harness/VerticalSlice.fs; tests/FunnySharp.Harness.Tests/VerticalSliceTests.fs.

**Approach:** Declare the existing typed 500 outcome using the timeline metadata pattern. Reproduce HistoryDiverged through only the public IOrderStore seam and verify behavior/OpenAPI from the same Kestrel host. Replace repeated --source restore overrides with one attempt-local XML NuGet config containing clear, local canonical feed and existing upstream URL.

**Execution note:** Capture the applicable failing-first or missing-evidence condition before repair, then verify the same observable contract.

**Patterns to follow:** SliceHost/GateOrderStore, ProducesProblem, isolated package cache and XML construction.

**Test scenarios:** Reconciliation really returns typed 500 and OpenAPI contains 200/404/500/503 with ProblemDetails. All four restores use the parsed config; spaces/XML-sensitive paths preserve exact feed values, package versions and cache. Existing status/state behavior remains intact.

**Verification:** Failing-before metadata and command-shape tests, actual official package-consuming slice pipeline, 49-test baseline plus new cases, ten paired scenarios and listener cleanup.

### U9. Publish accurate member-specific cost guidance

**Goal:** Publish accurate member-specific cost guidance.

**Requirements:** R7.

**Dependencies:** Final U2-U8 mechanisms; U13 numeric evidence.

**Files:** docs/performance.md; only affected current topic-guide prose; eng/harness/PerformanceDocs.fs; tests/FunnySharp.Harness.Tests/PerformanceDocsTests.fs.

**Approach:** Correct exact-zip full enumeration/materialization, Validation arrays/wrappers and error count, library-owned effect captures, PriorityQueue removal, receiver-dependent lookup/search, indexed parallel traversal sorting, UnitResult valueless traversal and partition shapes. Distinguish work from elapsed parallelism, construction from execution and numeric exclusions from semantic obligations. Generated comparisons use neutral candidate attribution.

**Execution note:** Capture the applicable failing-first or missing-evidence condition before repair, then verify the same observable contract.

**Patterns to follow:** Current source/XML implementations and existing generated-table verifier.

**Test scenarios:** Source-backed review discriminates allocating and nonallocating paths; generated rows attribute each actual library/method correctly. No prose or prompt wording tests.

**Verification:** Complete cost coverage joined with U13/U14, machine-consumed generator tests and fresh documentation verification.

### U10. Make benchmark equivalence and binding fail closed

**Goal:** Make benchmark equivalence and binding fail closed.

**Requirements:** R7.

**Dependencies:** U4-U5 for final concurrency semantics; U1 version inputs.

**Files:** benchmarks/FunnySharp.Benchmarks/Program.cs; ConcurrencyBenchmarks.cs; narrowly required main/competitor preflight class files; shared receipt exporter; eng/performance/baseline.json; competitor-baseline.json; eng/harness/Performance.fs and protocol tests.

**Approach:** Assign completion-order output slots after selector completion. Validate actual benchmark methods and full semantic outputs across every included policy/parameter row and account explicitly for exclusions. Run preflight before measurement. Bind complete shipping/project/import/lock inputs, candidate snapshot, and actual BenchmarkDotNet child workload DLLs, not merely exporter-host assemblies. Keep policy ceilings unchanged and historical evidence separately identifiable.

**Execution note:** Capture the applicable failing-first or missing-evidence condition before repair, then verify the same observable contract.

**Patterns to follow:** Competitor completeness reflection, current sorted fingerprint algorithm, existing raw report hash checks.

**Test scenarios:** Two gated selectors expose old admission ordering; publication signal precedes release of the earlier item. Missing row, wrong baseline, stale UnitResult/source/project/lock input, wrong measured DLL, mixed/null candidate, incomplete preflight and tampered report are rejected. Simultaneous completion ties are not falsely ordered.

**Verification:** Actual main and competitor --preflight entry points, deterministic protocol fixtures and immutable preflight/run-binding receipts.

### U11. Execute equivalent function-workflow comparisons

**Goal:** Execute equivalent function-workflow comparisons.

**Requirements:** R8.

**Dependencies:** U1-U7 and final canonical packages; product-claim disposition pending.

**Files:** eng/evaluation/comparisons/function-grammar/ new package-only project, neutral Contract, two workflow implementations and equivalence tests; docs/next-stage/call-sites-audit-resolution.md; attributable acceptance supplement.

**Approach:** Retain WF-1 through WF-7 and freeze equal inputs, outputs, exception/cancellation policies, materialization and construction lifetime before counting. Include helpers, validators, boundary conversions and one-shot/reuse costs on both sides. Retain every win, tie and loss; no convenience APIs or favorable subset to manufacture an advantage.

**Execution note:** Capture the applicable failing-first or missing-evidence condition before repair, then verify the same observable contract.

**Patterns to follow:** Existing GrammarInference/AsyncFunctionComposition/Scan/counting-ValueTask tests and package-consumer seam.

**Test scenarios:** Overflow versus FormatException;16 field-validity combinations with equal value/errors; missing/present Option output conversion; seed exclusion/one enumeration; async stage order, exact tokens and single consumption; reusable function construction counted equally.

**Verification:** All seven equivalent pairs run through final packages, auditable semantic-unit ledger, raw LOC separated and maintainer review of exact source hashes. Reproducibility alone does not close the material-density claim.

### U12. Create traceable deterministic AI evidence

**Goal:** Create traceable deterministic AI evidence.

**Requirements:** R8.

**Dependencies:** U1/U7 public-doc/analyzer fixes; U5 concurrency semantics; final package snapshot.

**Files:** eng/harness/Evaluation.fs; tests/FunnySharp.Harness.Tests/EvaluationTests.fs; build.fsx only required argument forwarding; eng/evaluation/studies/audit-resolution-v1/; fresh results subtree; current evaluation README and additive claim report.

**Approach:** Keep the runner file-based. Freeze curated public docs, prompts, neutral oracles/templates, packages and locks. Reject solution collisions with oracle files, missing/contradictory/zero/skipped counts and overwritten rounds. Store immutable solution/input/diagnostic/output/feedback receipts with at most initial plus two corrections. Replace timed fakes in the fresh study and add causally observable multiple-success cancel-and-drain cases. Generate a preregistered 20-session cohort using actual native producer routes and retain failures.

**Execution note:** Capture the applicable failing-first or missing-evidence condition before repair, then verify the same observable contract.

**Patterns to follow:** Existing eval pipelines, raw replay process receipts, event-gated cancellation/resource test fixtures and generated Api.MapApi seam.

**Test scenarios:** Duplicate/fourth/missing-predecessor rounds, oracle replacement, missing analyzer control and wrong bindings fail. Incorrect fixed/last winner and hidden side effects fail deterministic oracles. Every actual generation/correction retains session/model/context/package/output/feedback identity; unknown provider fields remain explicit.

**Verification:** Real eval runner plus fixed suites for every cohort run, analyzer negative control, Kestrel HTTP for generated ASP.NET solutions, exact-input attributable maintainer review and truthful comparative report.

### U13. Verify fresh performance receipts and resource coverage

**Goal:** Verify fresh performance receipts and resource coverage.

**Requirements:** R7.

**Dependencies:** U2-U10 and frozen package/workload inputs.

**Files:** eng/performance/coverage/stable-members.json and narrow coverage verifier/tests; fresh performance receipt directories and evidence index; approved observations/current generated tables only through the existing harness.

**Approach:** Reconcile every exported stable member to cost/boxing/resource characteristics and exact benchmark or honest exclusion. Execute three serial independent full main and competitor repetitions, retaining raw CSV/markdown/HTML triplets, outputs and failures. Preserve190 main/40 competitor rows and all allocation ceilings. Timing remains directional on this shared host. Historical15 receipts remain missing, never reconstructed.

**Execution note:** Capture the applicable failing-first or missing-evidence condition before repair, then verify the same observable contract.

**Patterns to follow:** Current policy/exclusion inventories, PerformanceProtocol tests and actual package resource witnesses.

**Test scenarios:** Missing/duplicate/stale member, unknown row, empty exclusion rationale, misclassified experimental member or incomplete cost fields fail. Long-input bound, cleanup and linear FirstSuccess witnesses remain mandatory despite numeric exclusions. Every raw report and workload binding verifies.

**Verification:** Actual receipt verifier, three paired repeats with unchanged budgets, full raw availability, candidate/source/binary binding, and current claim-to-evidence index.

### U14. Close stable API semantic and XML crosswalks

**Goal:** Close stable API semantic and XML crosswalks.

**Requirements:** R9.

**Dependencies:** U1-U8 and final public API/package bytes.

**Files:** docs/audits/goal-24-resolution/stable-api-contracts.json; eng/api-baseline/FunnySharp.public-api.txt and FunnySharp.AspNetCore.public-api.txt through the existing generator; narrow inventory/coverage verifier and harness tests; only proven missing source XML contracts; package-only compiler/runtime probes.

**Approach:** After the accepted additive API units settle, regenerate and verify the API baseline through the existing pipeline; inspect the exact diff rather than using regeneration to hide drift. Join exact exported identities including generated/plumbing members to stability, accepted name/output, conditional nullability, eager/deferred guards, exception/cancellation behavior, meaningful sync/async counterparts or deliberate absence, resolved XML/inheritdoc and existing tests/probes. Prefer evidence and minimal documentation fixes over broad API redesign.

**Execution note:** Capture the applicable failing-first or missing-evidence condition before repair, then verify the same observable contract.

**Patterns to follow:** Actual assembly/API inventories, source XML, default-state/boundary tests and package compiler seams.

**Test scenarios:** Option.Some/null Map/TryGet nonnull flow; nullable Result/Validation payloads; Effect nullable value; required versus optional HTTP mappers; faulted OCE versus natural-await cancellation; callback exception identity; one ValueTask consumption. Missing member and unresolved inheritdoc cannot pass.

**Verification:** Exact-set crosswalk validation, packaged nullable compiler diagnostics and runtime probes, shipped XML review and compiling documentation.

### U15. Prove authorized release enforcement and P A I provenance

**Goal:** Prove authorized release enforcement and P A I provenance.

**Requirements:** R10.

**Dependencies:** All material local findings, clean authorized candidate, owner remote authority.

**Files:** Existing Ruleset.fs/RulesetTests.fs; release workflow/protocol only if necessary; new immutable release attempt and P/A/I artifacts; docs/release-readiness.md truthful wording.

**Approach:** Use four exact strict release contexts, trusted integration identity and zero bypass actors. Obtain before/readback evidence and real non-bypass merge denial for each deliberate failure using authorized disposable proof work. Collect same-candidate four-platform consumers and aggregate final feed checks. Freeze P, obtain independent attributable A bound only to P/contracts, then publish external I bound to A and retrievable artifact locations. No package publication is included.

**Execution note:** Capture the applicable failing-first or missing-evidence condition before repair, then verify the same observable contract.

**Patterns to follow:** Existing ruleset verifier and canonical package/consumer release protocol.

**Test scenarios:** Missing context/host, wrong app, bypass, stale/ambiguous feed state, substituted package/source/workflow/contract bytes, missing artifact, reused attempt or self/later-object hash rejects. A material FAIL/UNVERIFIED cannot coexist with product PASS.

**Verification:** Real GitHub control-plane/blocking evidence, canonical package hashes on matching hosts, final feed receipt, independently retrievable/hash-verified P -> A -> I.

### U16. Reconcile all findings and freeze the final verdict

**Goal:** Reconcile all findings and freeze the final verdict.

**Requirements:** R1-R10.

**Dependencies:** U1-U15.

**Files:** docs/audits/goal-24-resolution/findings.json; resolution report and evidence index; final main-session QA and gate review artifacts.

**Approach:** Join every original ID to its verified resolution without rewriting original verdicts. Recheck proofs invalidated by tracked changes at the frozen candidate. Have the main session exercise real surfaces and one independent gate reviewer audit the full evidence. Keep audit completion distinct from product acceptance and record any owner-approved superseding contract explicitly rather than asserting historical PASS.

**Execution note:** Capture the applicable failing-first or missing-evidence condition before repair, then verify the same observable contract.

**Patterns to follow:** Original311-clause matrix and fail-closed Goal 13 rules.

**Test scenarios:** Exactly69 unique original IDs,40 FAIL and 29 UNVERIFIED origins; missing/stale/unread/empty artifact cannot close a row; all cleanup receipts exist; aggregate PASS requires all applicable criteria.

**Verification:** Scoped diagnostics, complete relevant tests/build/release/manual gates, exact closure ledger and one frozen-tree gate-review receipt.

## Verification Contract

| Gate | Command / real action | Applies to | Completion evidence |
| --- | --- | --- | --- |
| Diagnostics | Language-server diagnostics before build | Every changed source/test file | Clean diagnostics; exact missing-server limitation is named, never suppressed |
| Core/analyzers/harness focused tests | Repository MTP project commands and harness filters matching touched behavior | U2-U10/U12 | Failing-before case and single reliable passing run with no skipped tests |
| Build/full suites | dotnet fsi build.fsx -- -p build; dotnet fsi build.fsx -- -p test | Cross-cutting repair | Exit0, warnings/errors zero, exact pass/fail/skip counts |
| Docs/API | verify-docs-snippets; verify-api-baseline; format pipelines | U1/U2/U5/U7/U9/U14 | Snippet equality, accepted generated baseline and formatter receipt |
| Package consumers | compatibility pipeline and fresh package-only probes | Core/analyzers/HTTP | Canonical nupkg/DLL identities, actual binaries and cleanup |
| Slice | vertical-slice pipeline with actual final feed/output; same-host Kestrel requests/OpenAPI | U8 | Every step/marker, tests, equivalent scenarios, typed 500 and closed listener |
| Benchmark semantics | Both benchmark projects with --preflight | U10 | Complete policy/parameter coverage, outcomes, workload/source binding and exit 0 |
| Performance | Three serial full main/competitor runs; verify-performance over each actual receipt directory | U13 |190/40 rows per repetition, raw reports available/hash-verified, unchanged allocation ceilings |
| Evaluation | eval-prep-feed, real eval-verify/aggregate with selected fresh study | U11/U12 |20 actual independent initial sessions, bounded append-only corrections, full counts/diagnostics/feedback and same-package replay |
| Stable contracts | Full package/source/XML/compiler/runtime crosswalk validator | U14 | Exact-set coverage and semantic evidence for each applicable dimension |
| Release | Applicable release pipeline, actual four-platform checks and authorized enforcement/blocking proof | U15 | Clean candidate, canonical package consumer identities and control-plane receipts |
| Final provenance | Retrieve/hash P, independent A and external I | U15/U16 | A names P/contracts; I names A/locations; no circular/self references |
| Final review | Main-session real-surface QA and one independent goal/gate reviewer | U16 | Frozen candidate/tree, nonempty evidence, cleanup and approval |

## Definition of Done

A source unit is complete only when its assigned original IDs have the actual regression/evidence check, bounded writes, diagnostics, package-real-surface proof where required and retained cleanup. A report or green unrelated suite is not completion.

Global completion requires exactly 69 original IDs with accepted verified resolutions; every original audit artifact intact; no fabricated historical provenance or comparative advantage; all applicable final-candidate gates and real surfaces verified; complete stable-member cost/contract crosswalks; unchanged allocation thresholds; authorized enforcement and retrievable P/A/I; main QA and one final gate reviewer; and the delivered resolution report. Owner-approved contract supersession is recorded as supersession, not historical PASS.

### Original Finding Traceability

The live progress ledger is docs/audits/goal-24-resolution/findings.json. The table below is a fixed scope map, not execution status.

| Tracking | Original criterion ID | Original verdict | Primary unit | Original criterion |
| --- | --- | --- | --- | --- |
| F01 | G14-01 | FAIL | U1 | Authoritative next-stage product constitution |
| F02 | G14-12 | FAIL | U1 | One canonical vocabulary defined |
| F03 | G16-01 | FAIL | U1 | Small orthogonal predictable functional grammar |
| F04 | G16-04 | FAIL | U1 | Every stable verb has primary meaning |
| F05 | G16-05 | FAIL | U1 | Predictable output shape across valid carriers |
| F06 | G16-12 | FAIL | U1 | Canonical recovery |
| F07 | G16-15 | FAIL | U1 | One documented canonical usage for operations |
| F08 | G16-E1 | FAIL | U1 | Authoritative grammar table |
| F09 | G17-11 | FAIL | U2 | Traversal does not lose keyed collection shape |
| F10 | G17-19 | FAIL | U3 | Key context retained on valid BCL keys |
| F11 | G18-06 | FAIL | U5 | Bounded concurrency without unbounded fan-out |
| F12 | G18-08 | FAIL | U5 | Race or first-success safely |
| F13 | G18-13 | FAIL | U5 | No swallowed exceptions |
| F14 | G19-01 | FAIL | U5 | Align all advanced families with accepted Goal14 decisions |
| F15 | G20-01 | FAIL | U9 | Normal stable APIs impose no undisclosed performance burden |
| F16 | G20-02 | FAIL | U9 | Every performance-relevant stable operation has documented algorithmic complexity |
| F17 | G20-03 | FAIL | U9 | Enumeration characteristics are documented correctly |
| F18 | G20-04 | FAIL | U9 | Allocation characteristics are documented correctly |
| F19 | G20-06 | FAIL | U9 | Materialization characteristics are documented correctly |
| F20 | G20-07 | FAIL | U9 | Buffering characteristics are documented correctly |
| F21 | G20-09 | FAIL | U10 | Semantically equivalent raw BCL comparisons |
| F22 | G20-13 | FAIL | U5 | No known stable algorithmic cliff |
| F23 | G20-15 | FAIL | U5 | No hidden repeated work |
| F24 | G20-19 | FAIL | U10 | Raw handwritten reference is honest rather than a manufactured win |
| F25 | G20-22 | FAIL | U10 | Source-bound benchmark definitions and results |
| F26 | G20-25 | FAIL | U10 | Equivalence justifications support all comparison groups |
| F27 | G20-27 | FAIL | U9 | Generated public performance guidance is trustworthy |
| F28 | G20-28 | FAIL | U9 | No unsupported zero-cost always-faster or faster-IO blanket claims |
| F29 | G21-03 | FAIL | U7 | Common misuse rejected by types or local diagnostics |
| F30 | G21-04 | FAIL | U7 | Safe fixes where transformation is unambiguous |
| F31 | G21-05 | FAIL | U7 | Invalid and uninitialized carrier coverage |
| F32 | G21-08 | FAIL | U7 | Async hazards diagnosed with acceptably low false-positive risk |
| F33 | G21-12 | FAIL | U7 | Correct ordinary code remains quiet |
| F34 | G21-19 | FAIL | U7 | Diagnostic documentation is precise |
| F35 | G22-22.14 | FAIL | U8 | Typed-result metadata fidelity |
| F36 | G22-22.15 | FAIL | U8 | OpenAPI accurately reflects possible outcomes |
| F37 | G23-23.01 | FAIL | U15 | Release-quality preview consistently expresses accepted constitution |
| F38 | G23-23.36 | FAIL | U15 | Fail-closed release-readiness record |
| F39 | G23-R.04 | FAIL | U15 | Authorized four-context ruleset and deliberate blocking proof |
| F40 | G18-CANCELLATION-PUBLICATION | FAIL | U4 | Preserve consumer cancellation during an in-flight ordered pull |
| U01 | G16-02 | UNVERIFIED | U11 | Materially less consumer semantic LOC |
| U02 | G16-E4 | UNVERIFIED | U11 | Representative comparisons demonstrate shorter FunnySharp workflows |
| U03 | G17-32 | UNVERIFIED | U13 | Equivalent performance/allocation comparisons |
| U04 | G18-14 | UNVERIFIED | U4 | No cancellation converted to domain failure |
| U05 | G18-19 | UNVERIFIED | U13 | Measurable IO workflow value |
| U06 | G18-21 | UNVERIFIED | U6 | Deterministic xUnit scheduling-independent semantics |
| U07 | G18-23 | UNVERIFIED | U13 | Reproducible latency throughput allocation concurrency BCL comparisons |
| U08 | G19-11 | UNVERIFIED | U13 | Acceptable measured cost |
| U09 | G19-21 | UNVERIFIED | U13 | Relevant allocation and throughput results |
| U10 | G20-05 | UNVERIFIED | U13 | Boxing characteristics and stable-operation coverage are complete |
| U11 | G20-10 | UNVERIFIED | U13 | Appropriate idiomatic high-level BCL or LINQ comparisons |
| U12 | G20-14 | UNVERIFIED | U13 | No undisclosed unbounded resource behavior |
| U13 | G20-16 | UNVERIFIED | U13 | Material regressions removed or justified by visible semantics |
| U14 | G20-23 | UNVERIFIED | U13 | Raw result receipts independently available and verifiable |
| U15 | G21-01 | UNVERIFIED | U12 | Measurably friendly to agents and humans |
| U16 | G21-24 | UNVERIFIED | U12 | Semantic correctness recorded and reproducible |
| U17 | G21-25 | UNVERIFIED | U12 | Correction feedback recorded |
| U18 | G21-27 | UNVERIFIED | U12 | API misuse recorded |
| U19 | G21-29 | UNVERIFIED | U12 | More than one curated prompt or one model run as evidence |
| U20 | G21-30 | UNVERIFIED | U12 | Task model run solution and reviewer provenance |
| U21 | G21-31 | UNVERIFIED | U12 | AI friendliness from consistent types rather than hidden prompt instructions |
| U22 | G23-23.03 | UNVERIFIED | U14 | Stable API intentional names |
| U23 | G23-23.05 | UNVERIFIED | U14 | Stable API intentional nullability |
| U24 | G23-23.06 | UNVERIFIED | U14 | Stable API exception contracts |
| U25 | G23-23.08 | UNVERIFIED | U14 | Stable API sync/async relationships |
| U26 | G23-23.09 | UNVERIFIED | U14 | Stable API XML documentation |
| U27 | G23-23.28 | UNVERIFIED | U13 | Benchmark claims link to reproducible evidence and do not exceed it |
| U28 | G23-23.29 | UNVERIFIED | U12 | AI claims link to reproducible evidence and do not exceed it |
| U29 | G23-R.06 | UNVERIFIED | U15 | Immutable bundle independent attestation and external index |

## Appendix

Research evidence: .omo/audit-resolution/research/core-design.md, analyzer-design.md, performance-design.md, usability-design.md and release-design.md. These reports distinguish inspected historical receipts from executions; none ran product validators during planning. Source authority: docs/goals/archive/0013-goal.md and 0014-goal.md through 0023-goal.md, docs/goals/0024-goal.md, docs/product-contract.md, docs/grammar.md and docs/versioning.md.
