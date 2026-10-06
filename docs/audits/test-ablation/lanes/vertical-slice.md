# Vertical-slice test-ablation assessment

Baseline: 0c5416e37e06db17c30c014659601d0c463b973a. Worktree: Q:/repos/funnysharp/.omo/worktrees/test-ablation-g001, branch ulw/test-ablation-g001. Assessment only: no tests, production sources, package references, generated/frozen evidence or git history changed. Only the three assigned lane reports were written.

## Completeness and evidence

- Current source contains exactly 50 Fact methods in 13 test files, zero Theory/InlineData/MemberData methods, zero skipped or explicit methods. Every method body and assertion was read, including multi-input checks inside Facts. CSV identities are repo-relative source + :: + exact method name; line is the method declaration, not attribute.
- Test CSV has 55 rows: 50 Facts (48 retain, 2 simplify) plus 5 named executable checks (all retain): Api/Baseline IsVerifyRun, Measurements Main, Equivalence.Same and HostedApp.SeedAsync. No whole method is marked delete or merge without executed detector containment witnesses.
- Helper CSV has 23 units: 10 standalone TestSupport files, the nested CapturingLogger, 7 in-test helper units (Measurement factory; Respond; SchemaOf; five lifecycle fixture methods grouped; AvailabilityAsync; GatedRowsAsync; CancellableRowsAsync), and 5 Measurements units (Program helpers; Equivalence; Measurement; ScenarioResult; HostedApp). Dispositions: 19 retain, 4 simplify. Trivial helper members are included in their owning unit, not inflated into rows.
- Facts per file: BoundedParallelism 4; Cancellation 2; MeasurementsEquivalence 3; OpenApi 3; OrderEndpoint 5; OrderLifecycle 8; PaymentGuard 2; PlacementAtomicity 3; ProblemMapping 7; StreamingExport 3; TimelineIntegrity 1; UnexpectedException 2; Validation 7. Their union is the 50 method rows, not the stale 49 claim in AGENTS.md.
- Read the four project definitions, Directory.Build.props/global.json, applicable root/subtree AGENTS.md, product-contract/grammar, eng/harness/VerticalSlice.fs, every owned test/support body, complete measurement helper bodies, relevant Api/Baseline implementations, and packaged HTTP/concurrency implementation sections. Read current history: 72d95c8 removed yield/synchronization overhead without changing gate ownership; measurement pairing logic predates it (809da64d). No prior ablation report was used as proof.
- Read the actual parent-run receipt at primary checkout artifacts/test-ablation/baseline-vertical-mirror/vertical-slice-results.json: all 14 steps exit 0; status pass; three verify markers true; consumer tests total=50, passed=50, failed=0, skipped=0; both packages version 0.2.0. Parent supplied SDK 10.0.401 and runtime 10.0.12 (runner help independently displayed .NET 10.0.12). No baseline build/test was repeated here.
- Four read-only dotnet msbuild -getItem:Compile evaluations exited 0: Api 53, Baseline 6, Measurements 1, Tests 23, total 83 repository source inputs. Exact lists are below. No explicit linked Compile Include/Remove exists in these project files; no extra root Directory.Build.targets exists. SDK/package-generated compiler plumbing is outside owned source: evaluation is not a build-time generated-input capture and is not represented as additional hand-authored tests.
- Two additional executable checks have no source method name: Api/Program.cs lines 3-8 and Baseline/Program.cs lines 3-8 are top-level composition/marker smokes. Both are retain. They call Build before printing a harness-checked marker; they do not start HTTP serving or verify that routes really exist. The corresponding named IsVerifyRun CSV rows preserve the source predicate; no invented compiler-generated method identity is used.

## Detector model and strongest candidates

For a contractual fault universe F, D(t) is the subset of faults the actual assertions of t reject. Similar subjects, passing counts, coverage and green-after-deletion are not containment evidence. A whole-method deletion requires D(t) contained in the union of concrete independent executable retained owners, with representative old/new faults rejected in both suites. CSV experiments are future bounded plans, not executed mutation results.

Two approaches were considered: collapsing apparent pure/HTTP/comparison overlap by names, versus inspecting concrete assertions and retaining unproved unique detectors. The latter was chosen because packaged endpoints can be miswired while core unit tests remain green, canned conflicts cannot verify the real store revision guard, and fully materialized comparison bodies cannot verify streaming/disconnect behavior.

1. **Whole-method candidate remains a hypothesis.** UnexpectedExceptionTests.ADomainFailureNeverBecomesAnUnexpectedError checks 404 and a negative unexpected-error type for valid absent ORD-123456. OrderEndpointTests.AnUnknownOrderIsANotFoundProblem independently checks the stronger exact correct type and 404 for valid absent ORD-999999. OrderId.Create and FindOrderAsync use the same branch for these two syntactically valid missing IDs. Proposed future removal is bounded to the weaker method only. First run old and remaining owner against (a) absence mapped to 500 generic exception and (b) absence mapped to 404 with unexpected-error type. Both methods must fail in both faults before deleting; keep the malformed-ID branch test and throwing-dependency/log test. This audit did not execute faults, so disposition remains retain. Savings would be one default TestServer host/request, not quantified runtime.
2. **Bounded simplification, not detector deletion.** AThrowingDependencyProducesAGenericFiveHundredAndIsLogged asserts exact unexpected-error Type then DoesNotContain(order-not-found). Exact equality logically entails the negative substring check. Remove only that extra predicate if implementing; keep status, type, one logged exception, exception type and message. Missing=>404 and no-log fault witnesses still fail retained assertions. This saves assertion noise, not a test or meaningful runtime.
3. **Prose lock simplification.** EveryInvalidPlacementFieldIsReportedInInputOrder must retain its ordered four located keys, customer/SKU failures and both quantity partitions 0/99. The fixed English maximum sentence is incidental copy rather than a frozen API contract. Candidate replace exact sentence with nonempty message while retaining upper-limit field/error evidence; if introducing machine error codes is not authorized, do not invent them. Keep the comparison harness's real cross-application payload equality. Fail-fast/reversed-key/quantity99-accepted faults must remain rejected. This removes copy-coupled maintenance only, not validation requirements.
4. **Helpers: preserve behavior while fixing ownership/observation.** SliceHost and HostedApp create HttpClient objects but only dispose WebApplication; explicit disposal of the owned client is a bounded helper-lifetime candidate. GateSupplierGateway exposes Started as a mutable List: snapshotting under its existing lock avoids a read/write race during failing admission mutants. Correct current capped/held gates make the asserted admitted list stable; no observed baseline flake is claimed. Preserve gate signals, peak count, real store/inventory delegation and both TestServer/Kestrel paths.
5. **Measurement helper concern, not redundancy.** Program.MeasureAsync pairs first-warmup canonical body with last-measured status and never verifies intermediate body changes. A transient response mismatch can pass. Program.Append inserts decoded strings without JSON escaping: different object payloads can collide (for example {"revision":"x\",revision2:\"y"} versus {"revision":"x","revision2":"y"} both become {revision:"x",revision2:"y"}). Candidate make comparisons coherent per response and use unambiguous string encoding without dropping ten scenarios or measurement observations. Witness plan: change only an intermediate baseline response payload while first warmup and final status remain unchanged; current Main may pass, revised Main must reject. Also feed the concrete canonicalization collision through normal scenario requests. Comparator Facts alone do not detect normalization faults; no deletion is justified by these three tests.

## Retained boundaries and known limits

Admission/refill at cap 2, controlled completion order, first export row before a later row exists, TCP reset reaching Kestrel RequestAborted/store enumeration, request-token equality, typed failure versus cancellation compensation, inverse cancellation rollback and unwanted payment gateway calls are independently retained. Every cancellation/stream gate waits on a signal before triggering release/cancel. No fixed sleeps, polling delays, SHA locks or source-layout locks were found in owned tests. Infinite cancellable Task.Delay is a deliberately held dependency, not timing luck; 10s WaitAsync values bound failures and never delay success. Task.Yield in supplier discovery is not a clock wait.

The current suite does not explicitly count exactly-once enumerator DisposeAsync, test all dependencies despite a cancellation method's name, cover multiple lines despite CancellationReleasesEveryLine's name, test reverse supplier discovery for ordinal tie-breaking, or prove zero gateway calls in the unsupported-method validation test. Do not claim these missing detectors or weaken existing ones. Generated OpenAPI route/schema/status assertions concern machine-readable output, not prose/source shape. No release allocation budgets or performance requirements are reduced: application measurements stay observational, separate from eng/performance allocation gates. Baseline replay is a weaker status/revision comparison and its streaming supplier path is unbounded; unmeasured equivalence must not be inferred from ten measured scenarios. These are limitations, not additional edit scope.

## Executable plans and feed prerequisites

All commands below are plans unless explicitly noted. Run from an authorized experiment checkout containing built candidate packages; never run a stale primary DLL to claim a worktree mutant was killed. Full solution tests do not include these projects. Preserve PackageReference injection; Baseline has no FunnySharp PackageReference. A feed holds exactly one FunnySharp and FunnySharp.AspNetCore nupkg version; ambiguous/missing versions fail closed. Fresh runs pack automatically and clear their own output feed, restore four projects into isolated NUGET_PACKAGES, build Release, launch three markers, execute measurements and consumer tests. Do not use skip-tests or skip-measurements for ablation proof.

Canonical full consumer gate (default official remote feed):
```powershell
dotnet fsi build.fsx -- -p vertical-slice --output artifacts/test-ablation/vertical-candidate
```

The parent's actual successful mirror uses the supported package-feed knob after official NuGet TLS HandshakeFailure; do not change package versions or disable TLS:

```powershell
dotnet fsi build.fsx -- -p vertical-slice --output artifacts/test-ablation/vertical-candidate-mirror --package-feed https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json
```

For an already packed single-version feed, the exact harness option is --feed <absolute-feed-directory> --no-pack. Version properties are read from that feed, not guessed. Current parent feed/cache/config live under Q:/repos/funnysharp/artifacts/test-ablation/baseline-vertical-mirror; they establish baseline provenance only. No new SHA calculation was performed.

Filtered future faults use the built self-executing xUnit runner. Its help was inspected read-only (help exits 2; not a failed test run) and supports -method and -failSkips/-failWarns, not an assumed --filter-method switch:

```powershell
$TestsDll = 'tests/FunnySharp.VerticalSlice/FunnySharp.VerticalSlice.Tests/bin/Release/net10.0/FunnySharp.VerticalSlice.Tests.dll'
function F([string]$Method) {
    dotnet $TestsDll -method "*.$Method" -failSkips -failWarns
    if ($LASTEXITCODE -ne 0) { throw "Filtered witness failed: $Method" }
}
# Current source method names are unique; F(name) in CSV selects exactly that Fact.
F 'AThrowingDependencyProducesAGenericFiveHundredAndIsLogged'
F 'EveryInvalidPlacementFieldIsReportedInInputOrder'
# Independent old/remaining-owner pair for the unproved removal candidate:
dotnet $TestsDll -method '*ADomainFailureNeverBecomesAnUnexpectedError' -method '*AnUnknownOrderIsANotFoundProblem' -failSkips -failWarns
# Representative mandatory retained integration classes after any helper edit:
dotnet $TestsDll -class 'FunnySharp.VerticalSlice.Tests.BoundedParallelismTests' -class 'FunnySharp.VerticalSlice.Tests.CancellationTests' -class 'FunnySharp.VerticalSlice.Tests.StreamingExportTests' -class 'FunnySharp.VerticalSlice.Tests.PlacementAtomicityTests' -class 'FunnySharp.VerticalSlice.Tests.PaymentGuardTests' -class 'FunnySharp.VerticalSlice.Tests.OpenApiTests' -failSkips -failWarns
# Comparator positive and both negative axes:
dotnet $TestsDll -class 'FunnySharp.VerticalSlice.Tests.MeasurementsEquivalenceTests' -failSkips -failWarns
```

For fault experiments, expected mutant nonzero is evidence only when the named assertion/fault correspondence is recorded; F's throw is expected then. On the clean retained suite the same commands must pass with no skips. After implementing any candidate, run the complete consumer gate once, compare old/new rejected fault representatives, and check the measured scenario receipt. Do not count a green suite with a test absent as proof of containment.

Exact launch/measurement commands after consumer restore/build, with versions currently observed (replace only from candidate feed discovery if versions differ):

```powershell
dotnet run --project tests/FunnySharp.VerticalSlice/FunnySharp.VerticalSlice.Api -c Release --no-build --no-restore -p:FunnySharpPackageVersion=0.2.0 -p:FunnySharpAspNetCorePackageVersion=0.2.0 -- --verify
dotnet run --project tests/FunnySharp.VerticalSlice/FunnySharp.VerticalSlice.Baseline -c Release --no-build --no-restore -- --verify
dotnet run --project tests/FunnySharp.VerticalSlice/FunnySharp.VerticalSlice.Measurements -c Release --no-build --no-restore -p:FunnySharpPackageVersion=0.2.0 -p:FunnySharpAspNetCorePackageVersion=0.2.0 -- --verify
dotnet run --project tests/FunnySharp.VerticalSlice/FunnySharp.VerticalSlice.Measurements -c Release --no-build --no-restore -p:FunnySharpPackageVersion=0.2.0 -p:FunnySharpAspNetCorePackageVersion=0.2.0 -- --output artifacts/test-ablation/vertical-witness-measurements.json
```

Maintenance/runtime burden: 21 default-port TestServer methods, 16 injected-port TestServer methods and 2 injected-port Kestrel methods = 39 hosted Facts; 8 pure lifecycle/store and 3 comparator Facts = 11 unhosted Facts. Gates add no fixed delay when passing. Measurements run 9 nonstream scenarios at 20 warmups+200 measured calls each and 1 stream scenario at 3+15; cancellation additionally places an order each iteration; 40 seed requests across two hosts. No per-test runtime saving was measured. Full measurement cost must not be traded for --verify-only green markers.

## Evaluated Compile input inventory

All paths below are relative to tests/FunnySharp.VerticalSlice/. MSBuild evaluation command for each project was dotnet msbuild <project.csproj> -p:FunnySharpPackageVersion=0.2.0 -p:FunnySharpAspNetCorePackageVersion=0.2.0 -getItem:Compile (exit 0, no build). No source input was omitted from this list.

```text
FunnySharp.VerticalSlice.Api/Application/AsyncEnumerableBridge.cs
FunnySharp.VerticalSlice.Api/Application/IInventoryService.cs
FunnySharp.VerticalSlice.Api/Application/IOrderEventPublisher.cs
FunnySharp.VerticalSlice.Api/Application/IOrderStore.cs
FunnySharp.VerticalSlice.Api/Application/IPaymentGateway.cs
FunnySharp.VerticalSlice.Api/Application/ISupplierGateway.cs
FunnySharp.VerticalSlice.Api/Application/OrderCommandExecutor.cs
FunnySharp.VerticalSlice.Api/Application/OrderExport.cs
FunnySharp.VerticalSlice.Api/Application/OrderService.cs
FunnySharp.VerticalSlice.Api/Application/Reconciliation.cs
FunnySharp.VerticalSlice.Api/Application/SupplierQuoting.cs
FunnySharp.VerticalSlice.Api/Application/VerticalSliceOptions.cs
FunnySharp.VerticalSlice.Api/Domain/CancelOrderCommand.cs
FunnySharp.VerticalSlice.Api/Domain/CustomerId.cs
FunnySharp.VerticalSlice.Api/Domain/InputError.cs
FunnySharp.VerticalSlice.Api/Domain/Money.cs
FunnySharp.VerticalSlice.Api/Domain/Order.cs
FunnySharp.VerticalSlice.Api/Domain/OrderCommand.cs
FunnySharp.VerticalSlice.Api/Domain/OrderError.cs
FunnySharp.VerticalSlice.Api/Domain/OrderEvent.cs
FunnySharp.VerticalSlice.Api/Domain/OrderId.cs
FunnySharp.VerticalSlice.Api/Domain/OrderLifecycle.cs
FunnySharp.VerticalSlice.Api/Domain/OrderLine.cs
FunnySharp.VerticalSlice.Api/Domain/OrderPlan.cs
FunnySharp.VerticalSlice.Api/Domain/OrderRecord.cs
FunnySharp.VerticalSlice.Api/Domain/OrderStatus.cs
FunnySharp.VerticalSlice.Api/Domain/OrderTimeline.cs
FunnySharp.VerticalSlice.Api/Domain/PayOrderCommand.cs
FunnySharp.VerticalSlice.Api/Domain/PaymentAuthorization.cs
FunnySharp.VerticalSlice.Api/Domain/PaymentMethods.cs
FunnySharp.VerticalSlice.Api/Domain/PlaceOrderCommand.cs
FunnySharp.VerticalSlice.Api/Domain/Quantity.cs
FunnySharp.VerticalSlice.Api/Domain/ShipOrderCommand.cs
FunnySharp.VerticalSlice.Api/Domain/Sku.cs
FunnySharp.VerticalSlice.Api/Domain/SupplierSourcing.cs
FunnySharp.VerticalSlice.Api/Domain/TrackingCode.cs
FunnySharp.VerticalSlice.Api/Endpoints/ExportEndpoints.cs
FunnySharp.VerticalSlice.Api/Endpoints/OrderEndpoints.cs
FunnySharp.VerticalSlice.Api/Endpoints/SupplierEndpoints.cs
FunnySharp.VerticalSlice.Api/Http/JsonLines.cs
FunnySharp.VerticalSlice.Api/Http/OrderContracts.cs
FunnySharp.VerticalSlice.Api/Http/OrderRequestValidation.cs
FunnySharp.VerticalSlice.Api/Http/ProblemMappings.cs
FunnySharp.VerticalSlice.Api/Http/UnexpectedExceptionHandler.cs
FunnySharp.VerticalSlice.Api/Infrastructure/InMemoryOrderStore.cs
FunnySharp.VerticalSlice.Api/Infrastructure/SimulatedDelay.cs
FunnySharp.VerticalSlice.Api/Infrastructure/SimulatedInventoryService.cs
FunnySharp.VerticalSlice.Api/Infrastructure/SimulatedOrderEventPublisher.cs
FunnySharp.VerticalSlice.Api/Infrastructure/SimulatedPaymentGateway.cs
FunnySharp.VerticalSlice.Api/Infrastructure/SimulatedSupplierGateway.cs
FunnySharp.VerticalSlice.Api/Infrastructure/StableSimulationHash.cs
FunnySharp.VerticalSlice.Api/Program.cs
FunnySharp.VerticalSlice.Api/VerticalSliceApp.cs
FunnySharp.VerticalSlice.Baseline/BaselineApp.cs
FunnySharp.VerticalSlice.Baseline/Domain.cs
FunnySharp.VerticalSlice.Baseline/Endpoints.cs
FunnySharp.VerticalSlice.Baseline/Lifecycle.cs
FunnySharp.VerticalSlice.Baseline/Problems.cs
FunnySharp.VerticalSlice.Baseline/Program.cs
FunnySharp.VerticalSlice.Measurements/Program.cs
FunnySharp.VerticalSlice.Tests/BoundedParallelismTests.cs
FunnySharp.VerticalSlice.Tests/CancellationTests.cs
FunnySharp.VerticalSlice.Tests/MeasurementsEquivalenceTests.cs
FunnySharp.VerticalSlice.Tests/OpenApiTests.cs
FunnySharp.VerticalSlice.Tests/OrderEndpointTests.cs
FunnySharp.VerticalSlice.Tests/OrderLifecycleTests.cs
FunnySharp.VerticalSlice.Tests/PaymentGuardTests.cs
FunnySharp.VerticalSlice.Tests/PlacementAtomicityTests.cs
FunnySharp.VerticalSlice.Tests/ProblemMappingTests.cs
FunnySharp.VerticalSlice.Tests/StreamingExportTests.cs
FunnySharp.VerticalSlice.Tests/TestSupport/CapturingLoggerProvider.cs
FunnySharp.VerticalSlice.Tests/TestSupport/GateInventoryService.cs
FunnySharp.VerticalSlice.Tests/TestSupport/GateOrderStore.cs
FunnySharp.VerticalSlice.Tests/TestSupport/GateSupplierGateway.cs
FunnySharp.VerticalSlice.Tests/TestSupport/RecordingPaymentGateway.cs
FunnySharp.VerticalSlice.Tests/TestSupport/Refined.cs
FunnySharp.VerticalSlice.Tests/TestSupport/ResponseExtensions.cs
FunnySharp.VerticalSlice.Tests/TestSupport/SliceClient.cs
FunnySharp.VerticalSlice.Tests/TestSupport/SliceHost.cs
FunnySharp.VerticalSlice.Tests/TestSupport/StreamReaderExtensions.cs
FunnySharp.VerticalSlice.Tests/TimelineIntegrityTests.cs
FunnySharp.VerticalSlice.Tests/UnexpectedExceptionTests.cs
FunnySharp.VerticalSlice.Tests/ValidationTests.cs
```

## Audit validation and blockers

Validated CSV parsing, header widths, unique canonical IDs, exact source-method/line matches, 50-Fact set equality, 23 helper units including nested type and in-test helpers, and equality of the 83-item file inventory with four evaluated Compile sets. Current baseline behavior is proven by the parent's existing consumer receipt, not by a second test run. This worker did not run mutations or candidate edits, so whole-method redundancy remains unproved. No ownership gap or blocker prevents this assessment; implementing hypotheses requires a separately authorized code-changing experiment. No commitments, skipped assertions, new dependencies or benchmark/performance policy edits are part of this handoff.
