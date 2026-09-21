# Standing Constraints Audit (Goal 14 analysis)

Audited revision: `4dbebd94b7b58648632112b7ca47c39cc517f153` (main, 0.1.0), matching the
Goal 14 pinned FunnySharp baseline. This memo is evidence and classification only. It does not
recommend new features and does not authorize any change to `docs/product-contract.md`.

Classification vocabulary used per item:

- **(a) restate** — still binding; the next-stage constitution must restate it.
- **(b) deliberate change** — Goals 15-23 supersede or materially extend it; the constitution must
  change only through an explicit accepted decision, not by drift.
- **(c) drop** — obsolete, historical-only, or duplicated by another tracked source of truth.

An archived goal's whole contract text lives on line 2 of its file, so citations read
`docs/goals/archive/000N-goal.md:2`.

---

## 1. Binding constraints

### 1.1 Foundation and dependency boundary

1. **BCL-only core.** `FunnySharp` has no `PackageReference`, and its runtime surface is limited to
   platform assemblies shipped with .NET. Core must not depend on ASP.NET Core.
   Source: `docs/product-contract.md §"Package And Dependency Boundary"`; `src/FunnySharp/FunnySharp.csproj:1-25` (no
   package ItemGroup); `tests/FunnySharp.Tests/PackageBoundaryTests.cs:8-23` (loads the built
   assembly and asserts every referenced assembly is `System*`/`mscorlib`/`netstandard`);
   `docs/goals/archive/0001-goal.md:2`. Classification: **(a) restate**.

2. **Exactly two shipping packages.** `FunnySharp` and `FunnySharp.AspNetCore`, both `net10.0`. The
   ASP.NET Core package depends only on `FunnySharp` plus the `Microsoft.AspNetCore.App` framework
   reference; it adds no package dependency to the core. Exactly two `.nupkg`/`.snupkg` pairs exist
   and only two projects are packable.
   Source: `docs/product-contract.md §"Package And Dependency Boundary"`; `src/FunnySharp.AspNetCore/FunnySharp.AspNetCore.csproj:22-25`;
   `eng/Verify-Release.ps1:1499-1541,1868-1880`; `docs/release-evidence/goal-12.md:67-71`.
   Classification: **(a) restate**.

3. **Non-packable test/example/benchmark isolation.** Test, example, and benchmark dependencies never
   flow into the public packages; `FunnySharp.Tests` and `FunnySharp.AspNetCore.Tests` are
   non-packable, and the solution contains no competitor package.
   Source: `docs/product-contract.md §"Package And Dependency Boundary"`; `tests/FunnySharp.Tests/FunnySharp.Tests.csproj:4-5`;
   `FunnySharp.slnx:1-10`; `benchmarks/FunnySharp.Benchmarks/FunnySharp.Benchmarks.csproj:4,12-13`.
   Classification: **(a) restate** (see contradiction C5 for the working-tree call-site projects).

4. **Package metadata contract.** MIT license expression, repository README inside each package, XML
   documentation per shipping assembly, symbol packages, and equal package versions.
   Source: `docs/product-contract.md §"Package And Dependency Boundary"`; `eng/Verify-Release.ps1:1454-1497`;
   `Run-Release.ps1:442-446` (core/AspNetCore versions must match); `docs/release-readiness.md:68`.
   Classification: **(a) restate**.

5. **Supported platform matrix and evidence binding.** Windows x64, Linux x64, macOS arm64 are full
   supported hosts; Intel macOS is a bounded package-consumer smoke. Every required host consumes the
   exact canonical hashes produced by the Windows full job; passing one host implies nothing about
   another; evidence is bound to recorded SDK, runtime patch, RID, OS, workflow revision, package and
   loaded-assembly hashes.
   Source: `docs/product-contract.md §"Package And Dependency Boundary"`; `docs/release-readiness.md:39-53`;
   `.github/workflows/release.yml:22-201`; `eng/Verify-Release.ps1:1602-1640`.
   Classification: **(a) restate**.

6. **Trimming claim discipline.** Trim compatibility is evaluated from self-contained consumers built
   from produced packages with trim analyzers run without suppressions; trimming roots both shipping
   assemblies; each published executable must actually execute representative scenarios on the
   recorded RID. Both shipping projects set `IsTrimmable` because that evidence passes, and the
   verifier fails if a shipping assembly is not `IsTrimmable`.
   Source: `docs/product-contract.md §"Trimming And Native AOT Policy"`; `src/FunnySharp/FunnySharp.csproj:17`;
   `src/FunnySharp.AspNetCore/FunnySharp.AspNetCore.csproj:17`;
   `eng/Verify-Release.ps1:1824-1827`; `tests/FunnySharp.Compatibility/Run-Compatibility.ps1:40-61`.
   Classification: **(a) restate**.

7. **Native AOT claim limit.** Full-assembly Native AOT rooting is not a supported claim for the
   current release: with the .NET 10.0.11 compiler, rooting every open generic member of `Option`,
   `Result`, and `Validation` produces unsupported recursively nested `ValueTuple` instantiations.
   The packages therefore do not set `IsAotCompatible`; only representative closed generic consumers
   are published and executed, and no other path is implied.
   Source: `docs/product-contract.md §"Trimming And Native AOT Policy"`; `docs/release-evidence/goal-12.md:93-104`;
   `tests/FunnySharp.Compatibility/Run-Compatibility.ps1:50-61`.
   Classification: **(a) restate**, with the specific restriction re-decided deliberately whenever the
   API surface or toolchain changes (Goals 19, 20, 21, 23 touch that surface).

8. **SDK/target pinning and no preview support.** `global.json` pins SDK `10.0.400` with
   `latestPatch` roll-forward and `allowPrerelease: false`; target is `net10.0`; `net11.0` targeting is
   deferred until .NET 11 GA with the full validation matrix; preview support is explicitly not
   release evidence.
   Source: `global.json:1-6`; `Directory.Build.props:3`; `docs/product-contract.md §"Deliberate Deferrals"`;
   `TODO.md:11-15`; `docs/goals/archive/0012-goal.md:2`.
   Classification: **(a) restate**.

### 1.2 Performance policy

9. **Allocation budgets are the blocking contract; hosted timing is directional.** The Windows job
   must emit integer allocation receipts for every included policy row; missing, nonnumeric,
   mismatched, or over-budget allocation data fails the release. `below-resolution` and `unavailable`
   timing produce `N/A` and do not fail; timing guidance is explicitly directional and must be rerun
   on representative hardware. Every intentionally unmeasured surface is an explicit exclusion with
   no numeric claim.
   Source: `docs/release-readiness.md:29-37`; `TODO.md:5-9`; `eng/Verify-Performance.ps1:278-297`
   (over-budget allocation throws; timing states whitelisted);
   `docs/option.md:74`, `docs/result.md:133`,
   [function-composition.md — Performance Evidence](../../function-composition.md#performance-evidence),
   `docs/validation.md:127`, `docs/data-pipelines.md:81`, `docs/effects.md:119`,
   `docs/concurrency.md:163`, `docs/immutable-updates.md:183`.
   Classification: **(a) restate**.

10. **Generated performance documentation is candidate-bound.** Every guide table is generated from
    the approved observation in `eng/performance/baseline.json`; verify mode fails on manual drift;
    the observation records policy/benchmark/protocol fingerprints, environment, and receipt hashes.
    Source: `docs/release-readiness.md:34-35`; `eng/performance/baseline.json:2329-2337,3927-3993`;
    `eng/release-protocol.json:190-216`; `docs/release-evidence/goal-12.md:106-112`.
    Classification: **(a) restate**.

11. **StateTransition long-chain behavior is currently measured and budgeted, not fixed.**
    `StateMachineBenchmarks|Left-associated Then chain|FunnySharpThenChain` rows are `included: true`
    with allocation budgets (Count=256: 187,296 B observed vs 234,152 B budget), and the guide calls
    the numbers "a bounded characterization of repeated output copying, not a new throughput promise".
    Source: `eng/performance/baseline.json:2280-2294`; `docs/state-machines.md:103-115`.
    Classification: **(b) deliberate change** — Goal 20 states that no stable default path may retain
    a known algorithmic cliff and that "the existing long-chain StateTransition composition issue must
    therefore be resolved or excluded from the stable contract" (`docs/goals/0020-goal.md:2`). The
    next-stage constitution must record the resolution or the exclusion explicitly.

12. **No performance claim without demonstrated data.** "Performance claims require measurements
    against equivalent direct C# or BCL code"; the repository may not claim performance the data does
    not show; unsupported blanket claims such as zero cost, always faster, or faster IO are
    prohibited by the next-stage performance goal.
    Source: `docs/product-contract.md §"Product Direction"`; `docs/goals/archive/0012-goal.md:2`;
    `docs/goals/0020-goal.md:2`; `docs/release-evidence/goal-12.md:110-112`.
    Classification: **(a) restate**; Goal 20 materially extends what must be measured (see items 13
    and C4).

13. **Per-operation performance characteristics must be documented.** Goal 20 requires documented
    algorithmic complexity, enumeration, allocation, boxing, materialization, buffering, and async
    scheduling characteristics for every performance-relevant stable operation, with comparisons
    against raw BCL, idiomatic high-level BCL/LINQ, FSharp.Core where comparable, and relevant
    Funcky/CFE/language-ext operations. Current `baseline.json` policy rows carry allocation budgets
    and an `expectedResult` string but no complexity/boxing/scheduling fields.
    Source: `docs/goals/0020-goal.md:2`; `eng/performance/baseline.json:52-84`;
    `docs/release-readiness.md:36-37`.
    Classification: **(b) deliberate change** — the performance contract must be extended by an
    accepted Goal 20 decision; it also creates the C5 dependency question.

### 1.3 Evidence and release mechanics

14. **Zero-warning, all-passing, non-skipped verification bar.** Release build must report
    `0 Warning(s)` and `0 Error(s)` (`TreatWarningsAsErrors` is on globally); the xUnit v3 test run
    through Microsoft.Testing.Platform must report a positive total with 0 failed and 0 skipped;
    core and ASP.NET Core examples must compile and run with their success markers; the formatter must
    pass `--verify-no-changes`.
    Source: `docs/release-readiness.md:22-25`; `Directory.Build.props:6`; `global.json:7-9`;
    `eng/Verify-Release.ps1:902-935`; `eng/release-protocol.json:75-138`;
    `docs/release-evidence/goal-12.md:33-39`.
    Classification: **(a) restate**.

15. **Compiled documentation evidence.** Primary-guide `csharp` fences must match exactly one compiled
    source region in `examples/FunnySharp.DocumentationSamples`; the verifier currently covers seven
    guides (`aspnet-core`, `concurrency`, `effects`, `function-composition`, `immutable-updates`,
    `state-machines`, `validation`). Core and ASP.NET Core XML documentation must contain no member
    without `summary`/`inheritdoc`; the verifier throws on any such member.
    Source: `examples/FunnySharp.DocumentationSamples/VerifyDocumentationSnippets.ps1:8-16,38-71`;
    `eng/Verify-Release.ps1:1329-1339,1845-1857,1922-1924`;
    `docs/release-evidence/goal-12.md:47-53`.
    Classification: **(a) restate**.

16. **Public API inventory is candidate-bound.** The verifier reflects the Release assemblies and
    records exported types, declared members, generic/parameter order, nullability, return carriers,
    and `CancellationToken` placement into immutable evidence (`public-api.json`/`public-api.txt`)
    with the candidate fingerprint.
    Source: `eng/Verify-Release.ps1:1812-1843`; `docs/release-evidence/goal-12.md:52-59`;
    `docs/next-stage/inventory/generated/inv-funny-sharp-core.md:1-5` (33 types at this commit).
    Classification: **(a) restate**.

17. **Versioning, API-compatibility baseline, release notes, and experimental marking are required by
    the next stage.** Goal 23 requires intentional names/generic order/nullability/exception and
    cancellation contracts for stable APIs, unmistakable experimental APIs, package metadata,
    versioning, an API-compatibility baseline, release notes, and external package-consumer
    verification. Current state: `VersionPrefix` 0.1.0 and `EnablePackageValidation` only; no
    `PackageValidationBaselineVersion` or api-baseline file exists anywhere in the tree; the product
    contract has no experimental tier.
    Source: `docs/goals/0023-goal.md:2`; `src/FunnySharp/FunnySharp.csproj:5,18`;
    `docs/product-contract.md §"Deliberate Deferrals"`; `docs/release-evidence/goal-12.md:61-74`.
    Classification: **(b) deliberate change** — add the stability boundary deliberately; do not infer
    it from the current package.

18. **Candidate identity, fingerprint, and immutability.** A release attempt requires a clean tracked
    tree (including untracked paths), a commit-bound attempt directory
    `artifacts/release-candidate/<commit>/<attempt-id>`, refusal to reuse an attempt directory, a new
    and isolated NuGet cache, locked no-cache restore, version-absence preflight and final check
    against the distribution feed, and an unchanged source fingerprint before/after execution.
    Source: `docs/release-readiness.md:7-14,16-21`; `eng/Run-Release.ps1:403-430,450-477,500-513`;
    `docs/product-contract.md §"Baseline Verification"`; `eng/release-protocol.json:52-62`.
    Classification: **(a) restate**.

19. **Fail-closed verdict and required contexts.** `Audit status: COMPLETE` may coexist with a failed
    product; `Product acceptance: PASS` requires every material criterion to pass; any material FAIL
    or UNVERIFIED keeps acceptance failed. Four exact GitHub contexts are required
    (`release / win-x64`, `release / linux-x64`, `release / osx-arm64`, `release / osx-x64-consumer`);
    ruleset readback must show no bypass actors; the reviewer attestation references only the frozen
    evidence bundle and contract hashes.
    Source: `docs/release-readiness.md:39-64,75-87`; `docs/goals/archive/0013-goal.md:6-9`;
    `eng/Verify-GitHubRuleset.ps1:87-111`; `.github/workflows/release.yml:22-201`.
    Classification: **(a) restate**.

20. **Historical candidate evidence is not normative.** `docs/release-evidence/goal-12.md` is
    explicitly marked historical and is not a current acceptance verdict; its test counts, API
    numbers, and hashes must not be cited as current constraints.
    Source: `docs/release-evidence/goal-12.md:5-7`.
    Classification: **(c) drop** as a normative source; keep only as historical provenance.

### 1.4 Dispositions that constrain the next-stage constitution

The product contract's `Deliberate Deferrals` section (`docs/product-contract.md §"Deliberate Deferrals"`) contains
items 21-24 below; item 25 restates the no-speculative-API rule stated in the same section
(`docs/product-contract.md §"Deliberate Deferrals"`).

21. **First-party analyzers were out of scope, with reopening conditions.** "First-party analyzers
    remain out of scope. Reconsidering them requires a later goal with concrete diagnostics,
    false-positive policy, versioning rules, and measured maintenance cost." Guides repeat that
    analyzers and source generators remain outside each surface.
    Source: `docs/product-contract.md §"Analyzers And Compiler Feedback"`; `docs/release-evidence/goal-12.md:126-130`;
    `docs/option.md:58-59`; `docs/result.md:115-116`; `docs/validation.md:106-107`.
    Classification: **(b) deliberate change** — Goal 21 is the later goal that reopens analyzers:
    completion requires diagnostics for uninitialized semantic carriers, silently discarded
    outcomes, deprecated/ambiguous forms, and async/cancellation/enumeration/resource hazards, with
    low false-positive risk, documented severity/suppressions, and no runtime dependency added to the
    core (`docs/goals/0021-goal.md:2`). The constitution must record the reopening and the fact that
    Goal 21 does not mention versioning rules or measured maintenance cost.

22. **General discriminated unions are rejected.** FunnySharp stays on `net10.0` until .NET 11 is
    generally available and language/metadata contracts are stable; preview targeting does not count
    as support; a general union framework stays out of scope.
    Source: `docs/product-contract.md §"Deliberate Deferrals"`; `docs/goals/archive/0001-goal.md:2`;
    `docs/goals/archive/0015-goal.md:2`; `docs/goals/0023-goal.md:2`; `docs/goals/archive/0012-goal.md:2`.
    Classification: **(a) restate**.

23. **No `net10.0` union compatibility layer under current conditions.** A compatibility layer is
    considered only if real consumers must stay on .NET 10 LTS while needing the same general-union
    source model, and only with a fixed public API/semantics, source and binary migration, removal
    plan, trim/AOT results, allocation/throughput comparisons, and evidence that focused types are
    insufficient. Until every condition is met, no compatibility layer or general union API is added.
    Source: `docs/product-contract.md §"Deliberate Deferrals"`.
    Classification: **(a) restate**.

24. **Independence: no competitor dependency, carrier conversion, compatibility package, naming
    concession, or migration promise.** No competitor compatibility is required or permitted as
    acceptance evidence; a general union framework and competitor compatibility APIs are out of scope.
    Source: `docs/goals/archive/0014-goal.md:2`; `/tmp/opencode/next-stage-brief.md:80-85` (goal brief);
    `docs/goals/archive/0015-goal.md:2`; `docs/goals/0022-goal.md:2`; `docs/goals/0023-goal.md:2`.
    Classification: **(a) restate**; the benchmark-comparison tension is C5.

25. **No speculative feature API.** The foundation exposes no speculative feature API; public APIs
    require a later goal with usage, behavior, and verification evidence. README repeats the rule.
    Source: `docs/product-contract.md §"Deliberate Deferrals"`; `README.md:4`; `docs/goals/archive/0001-goal.md:2`.
    Classification: **(b) deliberate change** — the rule survives, but Goal 23 requires a formal
    experimental tier ("experimental APIs are unmistakable and do not silently acquire the same
    stability promise", `docs/goals/0023-goal.md:2`), so the stability boundary must be stated rather
    than implied.

26. **Rejected runtime/universe list.** No custom runtime, scheduler/fiber layer, DI container,
    alternative concurrency carrier, replacement collection universe, pervasive immutability,
    HKT/monad-transformer hierarchy, large IO universe, workflow engine, actor runtime, persistence
    layer, or distributed orchestrator.
    Source: `docs/product-contract.md §"Package And Dependency Boundary"`; `docs/goals/archive/0007-goal.md:2` (state),
    `0008-goal.md:2` (effects), `0009-goal.md:2` (concurrency), `0010-goal.md:2` (optics),
    `0011-goal.md:2` (HTTP); `docs/goals/archive/0015-goal.md:2`, `0016-goal.md:2`, `0019-goal.md:2`.
    Classification: **(a) restate**.

27. **Opt-in immutability and small optics.** Immutable data is opt-in; `Lens`/`Optional` stay a
    deliberately small delegate-based surface with no hierarchy, traversal API, reflection,
    property-path API, persistent-collection ecosystem, or hidden copying. `System.Collections.Immutable`
    is the update mechanism; `FrozenDictionary`/`FrozenSet` are replace-whole snapshots;
    `IReadOnlyCollection`/`IReadOnlyDictionary` are views, not immutable backing storage.
    Source: `docs/product-contract.md §"Product Direction"`; `docs/immutable-updates.md:10-13,91-153`;
    `docs/goals/archive/0010-goal.md:2`; `tests/FunnySharp.Tests/OpticsTests.cs:9,164`.
    Classification: **(a) restate** (Goal 19 explicitly defers advanced-pattern curation to the
    Goal 14 decision record, `docs/goals/0019-goal.md:2`).

28. **Async and cancellation invariants.** Exact caller token forwarding without eager cancellation;
    no sync-over-async, hidden `Task.Run`, unbounded fan-out, orphaned or unobserved work; `ValueTask`
    consumed once; faults and cancellation never converted into absence or domain failure; exception
    identity, established stack, canceled status, and token preserved; `ConfigureAwait(false)` inside
    helpers.
    Source: `docs/product-contract.md §"Product Direction"`; `docs/option.md:43-47`; `docs/result.md:64-72,85-97`;
    `docs/effects.md:63-73`; `docs/concurrency.md:44-49`; `docs/goals/0018-goal.md:2`;
    tests: `tests/FunnySharp.Tests/ResultBoundaryTests.cs:46,139,158,304`,
    `tests/FunnySharp.Tests/CollectionTraversalTests.cs:73`,
    `tests/FunnySharp.Tests/AsyncCollectionTraversalTests.cs:193,296`,
    `tests/FunnySharp.Tests/FirstSuccessTests.cs:42,139,322`,
    `tests/FunnySharp.Tests/ParallelAsyncEnumerableTests.cs:93,170`,
    `tests/FunnySharp.Tests/EffectResourceTests.cs:6,215,294`.
    Classification: **(a) restate**.

29. **Deliberate tokenless exception boundaries.** `Result.TryAsync`/`TryValueAsync` accept tokenless
    delegates by design; the caller captures its `CancellationToken` and passes it inside the
    delegate. This is recorded Goal 12 evidence and tested.
    Source: `docs/result.md:92-97`; `docs/release-evidence/goal-12.md:57-59`;
    `tests/FunnySharp.Tests/ResultBoundaryTests.cs:46,139,357`.
    Classification: **(a) restate**; Goal 18 must either honor this boundary or change it deliberately
    (see C9).

30. **Absence/failure/accumulation channel separation.** `Option` represents absence, `Result`
    fail-fast typed failure, `Validation` deterministic accumulation of independent errors. There is
    no `Where` on `Result`, no `Bind`/`SelectMany`/`ZipWith` on `Validation`, no throwing accessors,
    no implicit conversions, no async wrapper type, and no general union machinery. Traversal is
    fail-fast for Option/Result and fully accumulating for Validation in source order; empty inputs
    succeed empty; sources are enumerated once; large sequences are iterative.
    Source: `docs/result.md:29-30,107-116`; `docs/validation.md:25-33,43-66,103-110`;
    `docs/option.md:10,49-59`; `docs/goals/archive/0015-goal.md:2`; `docs/goals/archive/0017-goal.md:2`;
    tests: `tests/FunnySharp.Tests/ResultTests.cs:325`,
    `tests/FunnySharp.Tests/ValidationTests.cs:175,212`,
    `tests/FunnySharp.Tests/CollectionTraversalTests.cs:73,277`.
    Classification: **(a) restate**.

31. **UnitResult is the canonical third carrier and `Maybe<T>` is prohibited.** Goal 15 defines
    exactly four canonical semantic carriers (`Option<T>`, `Result<TValue,TError>`,
    `UnitResult<TError>`, `Validation<TValue,TError>`), prohibits a second absence carrier, and Goal
    23 requires guides to present `UnitResult`. Current API has 33 types and no `UnitResult`; README
    has no `UnitResult` section.
    Source: `docs/goals/archive/0015-goal.md:2`; `docs/goals/archive/0017-goal.md:2`; `docs/goals/0023-goal.md:2`;
    `docs/next-stage/inventory/generated/inv-funny-sharp-core.md:1-7`;
    `README.md:29-37`.
    Classification: **(b) deliberate change** — a new canonical carrier and vocabulary decision.

32. **Default/uninitialized carrier semantics will change.** Current contract: `default(Option<T>)`
    is a valid `None`; `default(Result<TValue,TError>)` is a failure containing `default(TError)`;
    `default(Validation<TValue,TError>)` is `Invalid([default(TError)])`. Goal 15 requires that
    default and uninitialized `Result`, `UnitResult`, and `Validation` values must not masquerade as
    legitimate domain outcomes, while `Option`'s default stays a valid `None`.
    Source: `docs/option.md:8`; `docs/result.md:13`; `docs/validation.md:9-11`;
    `tests/FunnySharp.Tests/OptionTests.cs:62`; `tests/FunnySharp.Tests/ResultTests.cs:6`;
    `tests/FunnySharp.Tests/ValidationTests.cs:6`; `docs/goals/archive/0015-goal.md:2`.
    Classification: **(b) deliberate change** — this is an explicit semantic redesign of two shipped
    defaults with law and misuse tests to be rewritten.

33. **Concurrency shape.** Bounded parallel mapping streams ordered `IAsyncEnumerable<T>` with
    `Channel` backpressure and linked operation cancellation; parallel traversal materializes ordered
    values and distinguishes fail-fast from accumulation; first-success accepts only cold
    `Effect<Result<TValue,TError>>` values, drains all started work, uses typed failures only for
    explicit `Result` failures, and supports cooperative `TimeProvider` timeouts; no naked
    started-`Task` racing API, no scheduler, no alternative concurrency carrier.
    Source: `docs/product-contract.md §"Product Direction"`; `docs/concurrency.md:10-53,88-138`;
    `tests/FunnySharp.Tests/ParallelAsyncSequenceTests.cs:180`,
    `tests/FunnySharp.Tests/FirstSuccessTests.cs:25,139,407`.
    Classification: **(a) restate**; Goal 18 wording pressure is C9.

34. **State machines stay a pure library abstraction.** Transition core is deterministic and can be
    tested without executing effects; `Applied`/`Rejected`/`Failed`/`Undefined` are distinct
    (undefined is not silent success); `OrElse` falls back only on `Undefined`; replay stops at the
    first non-applied result; no workflow engine, actor runtime, persistence, distributed
    orchestration, async executor, or general union framework.
    Source: `docs/state-machines.md:38-57,59-77,79-101,117-123`;
    `tests/FunnySharp.Tests/StateMachineTests.cs:128,232`;
    `docs/goals/archive/0007-goal.md:2`.
    Classification: **(a) restate**.

35. **Effects stay thin and explicit.** `readonly struct` wrappers over standard delegates; deferred
    until `RunAsync`; `ValueTask` return; explicit environment/cancellation/resource lifetime;
    ordinary factories do not wrap exceptions into `Result`; `Using`/`UsingAsync` release exactly once
    and follow natural C# `using`/`await using` precedence without custom aggregation; no retries,
    timeouts, supervision, implicit concurrency, DI container, or effect runtime.
    Source: `docs/effects.md:11-14,16-17,61-92,94-102`; `docs/product-contract.md §"Product Direction"`;
    `tests/FunnySharp.Tests/EffectTests.cs:6,157,220`;
    `tests/FunnySharp.Tests/EffectResourceTests.cs:6,215,294`;
    `docs/goals/archive/0008-goal.md:2`.
    Classification: **(a) restate**.

36. **ASP.NET Core boundary.** Separate optional package; explicit problem mappers required with
    `Status` set; optional success mapper; no DI registrations, service location, middleware, global
    error policy, or exception handler; environment-dependent effects require caller-supplied
    environment; effects run with exactly `context.RequestAborted`; compiled guide and `--verify`
    example.
    Source: `docs/aspnet-core.md:7-13,17-20,71-88`; `docs/product-contract.md §"Package And Dependency Boundary"`;
    `docs/goals/archive/0011-goal.md:2`.
    Classification: **(a) restate**.

37. **HTTP mapping has no numeric performance claim today.** The performance manifest records an
    explicit `excluded|aspnet-mapping` row with the rationale "No numeric release claim is made
    without a representative application pipeline and accepted comparison contract"; the guide says
    overhead is intentionally unassigned and caller-owned.
    Source: `eng/performance/baseline.json:56-70`; `docs/aspnet-core.md:116-121`.
    Classification: **(b) deliberate change** — Goal 22 requires "relevant end-to-end allocation,
    throughput, or latency measurements" from a realistic package-consuming vertical slice
    (`docs/goals/0022-goal.md:2`), which is the representative pipeline the exclusion names as its
    precondition. The lead must decide whether the exclusion is revised or the measurements are
    explicitly scoped to application end-to-end behavior (see C6).

38. **Guide/README coverage is currently narrower than Goal 23.** Goal 23 requires quick starts and
    primary guides for Option, Result, UnitResult, Validation, functional grammar, collection,
    async/streaming, concurrency, advanced-pattern, analyzer, performance, and ASP.NET Core usage,
    with every documented sample compiling. Current `docs/*.md` covers option, result, validation,
    function-composition, data-pipelines, concurrency, effects, immutable-updates, state-machines,
    aspnet-core; there is no UnitResult, collection, async/streaming, analyzer, or performance guide,
    and `VerifyDocumentationSnippets.ps1:8-16` compiles samples for only seven guides.
    Source: `docs/goals/0023-goal.md:2`; the directories `docs/` and
    `examples/FunnySharp.DocumentationSamples/`; `README.md:11-105`.
    Classification: **(b) deliberate change** — the documentation contract and compiled-sample set
    must be extended deliberately.

---

## 2. Contradictions and unresolved tensions requiring lead resolution

Each entry names both sides.

- **C1 — Analyzers: contract says out of scope, Goal 21 requires them.**
  `docs/product-contract.md §"Analyzers And Compiler Feedback"` plus `docs/release-evidence/goal-12.md:126-130` and
  `docs/option.md:59`/`docs/result.md:116`/`docs/validation.md:106` vs `docs/goals/0021-goal.md:2`
  ("Completion means a coding agent can discover one canonical way ... common misuse is rejected by
  types or reported locally by diagnostics", with analyzer coverage including uninitialized carriers
  and silently discarded outcomes). Reopening is anticipated, but Goal 21 does not restate the
  contract's "versioning rules, and measured maintenance cost" conditions
  (`docs/product-contract.md §"Analyzers And Compiler Feedback"`). Decision needed: carry those conditions forward into the revised
  contract or supersede them explicitly.

- **C2 — Default carrier semantics: current shipped defaults vs Goal 15.**
  `docs/result.md:13` and `docs/validation.md:9-11` plus tests
  `tests/FunnySharp.Tests/ResultTests.cs:6` and `tests/FunnySharp.Tests/ValidationTests.cs:6` vs
  `docs/goals/archive/0015-goal.md:2` ("Default and uninitialized Result, UnitResult, and Validation values
  must not masquerade as legitimate domain outcomes; Option's default remains a valid None"). The
  current defaults are meaningful domain outcomes. This is an accepted semantic redesign, not yet
  reflected in the contract; tests and guides encode the old contract.

- **C3 — `UnitResult` is canonical in Goals 15/17/23 but absent from the product.**
  `docs/goals/archive/0015-goal.md:2` (four canonical carriers), `docs/goals/archive/0017-goal.md:2` (traversal over
  UnitResult), `docs/goals/0023-goal.md:2` (guides must present UnitResult) vs
  `docs/next-stage/inventory/generated/inv-funny-sharp-core.md:1-7` (33 types, no UnitResult) and
  `README.md:29-37`. The constitution must state whether `Result<TValue,TError>` with a unit value
  substitutes, or `UnitResult<TError>` is a new canonical type.

- **C4 — StateTransition cliff: measured-and-budgeted vs Goal 20's resolve-or-exclude rule.**
  `docs/state-machines.md:103-115` characterizes the left-associated `Then` chain as bounded
  characterization and `eng/performance/baseline.json:2280-2294` keeps it `included: true` with
  budgets vs `docs/goals/0020-goal.md:2` ("No stable default path may retain a known algorithmic
  cliff ... the existing long-chain StateTransition composition issue must therefore be resolved or
  excluded from the stable contract"). The current 457x/187 KB profile at Count=256 is on the
  stable surface. Goal 20 requires an explicit resolution or exclusion before this path can remain a
  stable default.

- **C5 — Competitor comparisons required by Goal 20 vs the independence constraint.**
  `docs/goals/0020-goal.md:2` requires reproducible comparisons against "FSharp.Core where the
  representation and semantics are comparable, and relevant Funcky, CSharpFunctionalExtensions, or
  language-ext operations" vs `docs/goals/archive/0014-goal.md:2`, `/tmp/opencode/next-stage-brief.md:80-85`,
  `docs/goals/archive/0015-goal.md:2`, `docs/goals/0022-goal.md:2`, and `docs/goals/0023-goal.md:2`
  ("no competitor dependency", "No compatibility ... permitted as acceptance evidence"). Current
  state: `benchmarks/FunnySharp.Benchmarks/FunnySharp.Benchmarks.csproj:12-13` references only
  `BenchmarkDotNet`; the working tree contains an untracked call-site project
  `docs/next-stage/call-sites-code/competitors/Competitors.csproj:13-15` referencing
  `CSharpFunctionalExtensions 3.7.0`, `Funcky 3.6.0`, and `LanguageExt.Core 4.4.9`, outside
  `FunnySharp.slnx:1-10`. Decision needed: whether analysis/benchmark tooling may reference
  competitor assemblies while shipping packages and the solution never do, and whether that must be
  recorded in the constitution.

- **C6 — HTTP performance exclusion vs Goal 22 measurement evidence.**
  `eng/performance/baseline.json:56-70` (exclusion rationale: no representative application pipeline
  and accepted comparison contract) and `docs/aspnet-core.md:116-121` vs
  `docs/goals/0022-goal.md:2` (evidence includes "relevant end-to-end allocation, throughput, or
  latency measurements"). The vertical slice is exactly the missing precondition; the lead must
  decide the scope of the claim and whether the manifest exclusion changes.

- **C7 — Guide/README coverage vs Goal 23's required guide set.**
  `README.md:11-105` lists ten feature sections with no UnitResult, collection, async/streaming,
  analyzer, or performance guide; `examples/FunnySharp.DocumentationSamples/VerifyDocumentationSnippets.ps1:8-16`
  compiles samples for seven guides vs `docs/goals/0023-goal.md:2` requiring canonical usage of all
  listed areas with every documented sample compiling. This is a deliberate documentation expansion;
  it also interacts with C1 (analyzer docs cannot precede analyzers) and item 38.

- **C8 — Archived Goal 1 couples analyzers to the union design; Goal 21 decouples them.**
  `docs/goals/archive/0001-goal.md:2` ("General discriminated unions and analyzers are explicitly out
  of scope until revisited after the official C# 15/.NET 11 union design stabilizes.") vs
  `docs/goals/0021-goal.md:2` (analyzers as a standalone AI-usability goal with no union
  dependency) and `docs/goals/archive/0015-goal.md:2`/`docs/goals/0023-goal.md:2` (unions stay rejected
  independently). The coupling in the archived goal is obsolete; the constitution should record
  analyzer work and union rejection as separate decisions.

- **C9 — Async-race scope wording.**
  `docs/product-contract.md §"Product Direction"` and `docs/concurrency.md:135-138` restrict first-success to cold
  `Effect<Result<TValue,TError>>` with no naked started-`Task` racing API, while
  `docs/goals/0018-goal.md:2` asks for workflows that "race or select first success safely" and
  "choose ordered or completion-order results". The current implementation already returns first
  success in completion order (`docs/concurrency.md:96-98`), but Goal 18's wording could be read as
  broadening the accepted carrier set. The lead should confirm the cold-`Effect`-only boundary is
  the accepted vocabulary, since `ConcurrentEffectExtensions` in the current inventory
  (`docs/next-stage/inventory/generated/inv-funny-sharp-core.md:30-34`) encodes it.

- **C10 — Working-tree evidence files block the fail-closed clean-tree gate.**
  `eng/Run-Release.ps1:407-411` rejects a dirty tree including untracked files
  (`--untracked-files=all`), while Goal 14 analysis artifacts and call-site projects are currently
  untracked (`git status` at commit `4dbebd9` shows untracked `docs/next-stage/**` and
  `docs/next-stage/call-sites-code/**`). Until the goal's artifacts are committed or explicitly
  excluded, any release attempt fails the candidate-identity gate before building. This is a
  procedural interaction, not a policy contradiction, but it must be resolved before Goal 23's
  release-hardening run.

- **C11 — `Maybe<T>` prohibition is consistent (recorded, not a conflict).** Goal 15 forbids a second absence
  carrier (`docs/goals/archive/0015-goal.md:2`), and no `Maybe<T>` exists in the current API
  (`docs/next-stage/inventory/generated/inv-funny-sharp-core.md:1-7`). No contradiction; recorded so
  the constitution can restate the prohibition with a source.

- **C12 — No API-compatibility baseline exists despite package validation being enabled.**
  `src/FunnySharp/FunnySharp.csproj:18` and `src/FunnySharp.AspNetCore/FunnySharp.AspNetCore.csproj:18`
  set `EnablePackageValidation` but no `PackageValidationBaselineVersion` or baseline file exists;
  `docs/goals/0023-goal.md:2` requires an "API-compatibility baseline" verified through external
  package consumers. Extension, not conflict; the lead must specify the baseline mechanism and its
  relationship to the stability boundary from item 17.

---

## 3. Not examined

- **Goal 13 verdict artifacts.** No tracked `Audit status`/`Product acceptance` record exists under
  `docs/`; the ruleset control plane was not queried. Current acceptance state is
  **UNVERIFIED: no evidence artifact in the repository; `eng/Verify-GitHubRuleset.ps1:59` requires an
  authorized `gh api` call that was not made.**
- **Source bodies.** `src/**/*.cs` implementations were not read; semantic claims are based on
  guides, tests, generated API dumps, and goal-12 evidence. A defect in an implementation that its
  tests do not cover would not be visible to this audit.
- **Build/test execution.** No `dotnet build`/`test` was run, so the current test count, warning
  state, and docs-table drift are unverified. Historical Goal 12 numbers (310 tests, 126 benchmarks)
  were not re-measured and must not be treated as current.
- **Benchmark and compatibility internals.** `benchmarks/**` bodies, `tests/FunnySharp.Compatibility/**`
  program bodies, `eng/Verify-Release.ps1` in full (skimmed around cited regions),
  `eng/ReleaseProtocol.psm1`, `eng/Compare-ReproducibleBuilds.ps1`, and
  `eng/tests/*.Tests.ps1` were not line-audited.
- **Historical planning documents.** `docs/plans/2026-08-31-1518-feat-focused-option-abstraction-plan.md`
  and `docs/plans/2026-09-03-2223-fix-release-acceptance-plan.md` were located but not read in full.
- **Parallel Goal 14 workstreams.** The other analysis memos
  (`docs/next-stage/analysis/baseline-*.md`, `funny-sharp-surface.md`), the human-readable
  inventories, and `docs/next-stage/baselines.md` were grepped for cross-reference only; their
  findings were not independently re-derived, and the pinned baseline packages were not re-hashed.
- **`.github/**` beyond `release.yml`** and any repository settings, environments, or secrets were not
  inspected.
- **Whether the .NET 10.0.11 `ValueTuple` AOT limitation persists on later runtime patches**
  **UNVERIFIED: no other toolchain was available for testing.**
- **Whether the docs performance tables currently match `baseline.json`**
  **UNVERIFIED: requires running `eng/Generate-PerformanceDocumentation.ps1 -Verify`.**
- **Whether shipping analyzers can satisfy Goal 21's "no runtime dependency" condition**
  **UNVERIFIED: Goal 21 is forward-looking; no analyzer package exists at this commit.**
