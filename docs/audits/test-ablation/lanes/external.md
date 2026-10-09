# External test-ablation lane

Baseline: `0c5416e37e06db17c30c014659601d0c463b973a`, inspected in `Q:/repos/funnysharp/.omo/worktrees/test-ablation-g001`. This is an assessment, not an applied ablation or a passing execution receipt. Only the three assigned lane reports were written. No baseline builds, model study, dependency installation, package publication, history mutation, or test/production edit was performed.

## Decision and completeness

The ledger has **65 test/body rows**: **52 Facts + 6 Theories = 58 xUnit source methods**, plus **4 Compatibility source bodies/methods**, plus **3 live standalone control script bodies**. The helper ledger has **23 rows**: 9 standalone helper/oracle implementation files, 9 nested helper types, and 5 local-function groups. Test dispositions: **52 retain, 13 simplify, 0 merge, 0 delete**. Helper dispositions: **19 retain, 4 simplify, 0 merge, 0 delete**. Simplify means the stated obligation remains executable; it is not permission to weaken an assertion or remove an input partition.

| Current Compile surface | Source methods | Expanded xUnit cases | Compile inputs inspected |
| --- | ---: | ---: | --- |
| business-outcomes | 8 Facts | 8 per style | tests/Contract.cs + tests/PlaceOrderTests.cs |
| collections | 9 Facts | 9 per style | tests/Contract.cs + tests/CollectionsTests.cs |
| async-streams | 8 Facts | 8 per style | tests/Contract.cs + tests/AsyncStreamsTests.cs |
| concurrency | 9 Facts | 9 per style | tests/Contract.cs + tests/ConcurrencyTests.cs |
| aspnetcore | 9 Facts | 9 per style | tests/Contract.cs + tests/AspnetcoreTests.cs |
| function-grammar comparison | 9 Facts + 6 Theories | 37 | Contract.cs, FunnySharpWorkflows.cs, IdiomaticWorkflows.cs, WorkflowEquivalenceTests.cs |
| Compatibility.Core | top-level + 2 Verify methods | not xUnit | Program.cs; SDK default Compile glob |
| Compatibility.AspNetCore | top-level | not xUnit | Program.cs; generated CompatibilityJsonContext |
| live offline controls | 3 top-level bodies | not xUnit | offline-packet-controls.fsx, measure-offline.ps1, portable-available-controls.ps1; #load CurrentPacket.fs for packet controls |

All paths in the first five rows are under `eng/evaluation/tasks/<area>/`; comparison paths are under `eng/evaluation/comparisons/function-grammar/`. Every current test body/assertion, helper body, and both current comparison implementations was read. The inventories came from actual attributes/declarations and bodies, not method names. CSV test IDs are repo-relative source path + `::` + exact source method name. Top-level C#/F#/PowerShell bodies have no declared source method name; the explicitly documented `::<top-level>` sentinel is used once per body, with every executable subcheck enumerated in its row or the control ledger below. No named test method is collapsed.

The 6 comparison Theories carry 28 InlineData rows: WF-1 3, WF-3 4, WF-4 3, WF-5 completed/pending 8, WF-5 faults 4, WF-7 6. Nine Facts give 37 expanded cases. Internal partitions also remain: WF-3 observer identity 2 styles x 2 exception types x 2 parse paths; WF-5 fault theory 7 modes per InlineData; WF-6 16 masks. Evaluation has no Theory. Five areas across both styles give **86** evaluation executions; adding the comparison gives **123** xUnit executions before package-smoke/control executions. These are source-derived expected counts, not reported runs.

Ten templates were inspected: both `template-idiomatic/<Kit>.csproj` and `template-funnysharp/<Kit>.csproj` for each area. Kit names are `BusinessOutcomesKit`, `CollectionsKit`, `AsyncStreamsKit`, `ConcurrencyKit`, `AspnetcoreKit`. All use net10.0, OutputType=Exe, IsTestProject=true, xunit.v3 4.0.0, global Xunit using, nullable and implicit usings. Templates contain **zero .cs** and no explicit Compile includes: the runner copies the two current trusted .cs files and all selected solution/*.cs into a transient directory, then SDK default Compile includes them plus generated xUnit runner/source-generator inputs. Both aspnetcore templates additionally reference Microsoft.AspNetCore.App and TestHost 10.0.11. FunnySharp templates pin FunnySharp **0.1.0**; aspnetcore also pins FunnySharp.AspNetCore **0.1.0**. The comparison pins FunnySharp **0.2.0** and xunit.v3 4.0.0, uses default Compile for its 4 files, and has no ProjectReference.

Discovery searched all current project/script names outside bin/obj, then actual build/CI/harness callers. `FunnySharp.slnx` has 13 projects, not Compatibility, evaluation kits, the function-grammar comparison, competitor benchmarks, or inventory dumper. The previously unlisted **function-grammar project (15 methods, not merely the first grep page)** and **live eng/verification control scripts** are included here. The inventory dumper is generation tooling, competitor projects are performance tooling; their executable gate invocations are recorded below but their algorithms are not test methods. The VerticalSlice tests/measurement gate are the separate lane. Ordinary solution examples and benchmark-preflight are separate solution lanes, not duplicated test dispositions here.

Excluded: frozen `eng/evaluation/results/` test/evidence copies, `eng/evaluation/studies/` snapshots, archived goals, generated or frozen docs evidence copies. The accepted run-2 solution sources/record metadata were read only to identify replay inputs; they were not assigned new dispositions. Live offline verifier implementations are gate code, not newly invented xUnit tests; their entrypoints and all shared validator bodies were inspected. Historical scripts invoked by live paired controls remain fixed read-only inputs.

## Detector model and bounded candidates

For a test T, D(T) is the subset of contractual faults its actual assertions and fixture observe, including branch/input/scheduling and package-host boundaries. A deletion requires D(T) contained in a union of named, independently executable remaining owners, with old/new fault-witness rejection. Same-looking output, green-after-deletion, counts, or coverage do not establish that containment. No entire-test containment proof was established here, so none is marked merge/delete. Each CSV experiment names a meaningful mutation and a filtered command; those are **plans, not executed mutation results**. `independent_owner=none proved` is deliberate rather than an invented substitute owner.

Two approaches were considered: compress similar scenario tests into shared theories/helpers, or preserve their detector partitions and remove nonsemantic scheduling burden. The second wins. For example, mixed feed does not own null/whitespace price, both-blank precedence or empty-source branches; successful reservation does not own all-decline ordering; HTTP equivalence does not own packed AOT/trimmed execution. Most apparent overlap is narrower than the candidate detector set.

1. **Concurrency fake scheduling (strongest).** `tasks/concurrency/tests/Contract.cs` uses Task.Delay at CheckAsync and ProbeAsync, with 5-60 ms fixtures. Delay order is requested, not observed. Degree3 overlap depends on starting work before 40 ms expires; FirstCheckEntered/FirstProbeStarted cancellation signals do not prevent the 60 ms timer beating a delayed test continuation. Replace clock completion with per-call entered/release signals, registering signals before triggering coordinator work. Release source-order witnesses in an explicitly different order; retain both overlap and upper-bound assertions. A serial mutant must fail a bounded overlap expectation, not deadlock waiting for all calls. Degree1 releases each entered call in turn. Canceled calls remain held until cancellation, and finally still decrements in-flight counters. Do not change gateway result/count/token obligations. This changes supplied fake scheduling mechanics and needs a coherent update to both variants' current briefs; it is not an authorized edit in this report.
2. **Async fake suspension.** `AsyncStreamsTests` has Delay1 in Stream, TokenAwareStream, CancellingStream and RecordingSource; `ReferenceService` has Delay1(token). There is no elapsed-time acceptance criterion. Non-clock async suspension can remove timer cost, but must preserve source-owned post-first-yield OCE, EnumeratorCancellation, ordered reference calls, exact tokens, cancellation before any reference call and yield-count accounting. Do not turn all fixtures into synchronously completed enumerables, which would erase async-boundary detectors.
3. **Portable observation wrapper.** `portable-available-controls.ps1` prints four native exit codes but never asserts them. `$ErrorActionPreference='Stop'` does not ordinarily make native nonzero exit a terminating PowerShell error. Retain all probes; make expected old/new/help/usage exit tuple 0/0/0/2 explicit when genuine archives are supplied, and require missing-archive/injected failure to reject. This is a current detector gap, not redundancy.
4. **Local assertion-only hypotheses.** In BlankRequiredFields and MixedFeed, exact Products/Dropped list equality already implies their Count sums 4 and 7. Removing only the sums has a local logical containment argument, but no independent whole-test owner. Candidate status is hypothesis until old/new duplicate/omitted-row witnesses reject under the retained exact sequence assertions. It offers negligible runtime savings and is lower priority than timers.

The existing comparison subscription TCS and Core SmokeTimeProvider are useful retained representatives. Comparison Bounded(10 seconds) and Core loserCanceled.WaitAsync(30 seconds) are failure deadlines, not sleeps to make tests pass. Keep them. No xUnit test here pins arbitrary prose, a meaningless SHA, or production source shape. Product names/error codes are parsed DTO output. Packet hashes compare bytes to fixed catalog/consumer expectations; removing them based on prior loader success would admit another valid object at a later binding. Retain swapped-valid-object/PE, allocation-budget-plus-one, unsafe recording path, receiver/producer partition and physical-tamper controls. Allocation/performance policy remains unchanged.

Burden: 7 fixed Task.Delay source sites (2 concurrency + 5 async) amplify timer continuations when run in both styles. Published Compatibility executables use no fixed sleep. Four default package scenarios require separate restore/publish/run and native toolchains: this high cost buys a boundary no project-reference test owns. Comparison has 37 fast cases plus causal async controls; no timing threshold. Packet controls load/verify fixed encoded objects and PE metadata, then mutate copied JSON; measure-offline repeats expensive packet verification in 7 processes for an actual paired-cost/acceptance check. No elapsed/runtime/allocation measurement was invented here.

## Safe current evaluation execution

Current `Evaluation.fs:591-695` differs from the stale AGENTS runner description. Without --study, runVerify rejects paths under historical results or any run dir already containing record.json (604-607), claims contiguous immutable round directories, copies CURRENT trusted tests, records actual process input/output and writes fresh round record/feedback/receipt. `--study` activates frozen snapshot, preregistered cohort/run path and producer requirements (bindStudy around 507-575); `--replay` requires --study (parseVerifyArgs), so neither is needed or appropriate for a simple local no-study replay. Do not invoke on historical results and do not initiate another model study.

All ten `eng/evaluation/results/<area>/<style>/run-2/record.json` metadata records say compilation/semanticCorrectness.ok=true, no FS diagnostics, expected totals 8/9/8/9/9. They are historical acceptance of inputs, not current test execution. Their retained solution files were inspected:

| Area | idiomatic run-2 solution files | funnysharp run-2 solution files |
| --- | --- | --- |
| business-outcomes | OrderWorkflow.cs | OrderWorkflow.cs |
| collections | FeedCleaner.cs | FeedCleaner.cs |
| async-streams | SensorStream.cs | SensorStream.cs |
| concurrency | AvailabilityCoordinator.cs | AvailabilityCoordinator.cs |
| aspnetcore | Api.cs | Api.cs + Shop.cs |

**Path A: no-study local replay with current templates.** Parent may create fresh solution directories under `artifacts/test-ablation/external-baseline/<area>/<style>/solution/` from the accepted sources without touching originals, then use:

`dotnet fsi build.fsx -- -p eval-verify --task <area> --style <style> --run-dir artifacts/test-ablation/external-baseline/<area>/<style> --round 1`

Expand with exactly the 5 areas and 2 styles in the tables. For final execution use `external-final`, never reuse a GREEN round or overwrite baseline records. No --study/--replay. Runner child commands are exactly `dotnet build --configuration Release -bl:build.binlog -p:ErrorLog=diagnostics.sarif -p:RestorePackagesWithLockFile=true`, then `dotnet test --configuration Release --no-build` in the captured transient build working directory. No-study does not add RestoreLockedMode=true; it uses an evaluation-local cache/config, but the 0.1.0 FunnySharp package must really be available. `eval-prep-feed` packs the **current 0.2.0** source, so it alone cannot satisfy the unchanged 0.1.0 template. An upstream/cache resolution is not current-package proof. Idiomatic variants need no FunnySharp package.

**Path B: disposable current package consumer (recommended for the full requested baseline/final).** Parent may create new projects/configs under `artifacts/test-ablation/external-baseline-kits/<area>/<style>/`, with Compile links to CURRENT trusted tests and retained accepted solution inputs. Use the template's assembly name/framework/Using/TestHost inputs, EnableDefaultCompileItems=false, IsPackable=false, and use **0.2.0** candidate nupkgs from the parent's local feed. No ProjectReference. Explicit compile closure is:

`<Compile Include="$(RepositoryRoot)/eng/evaluation/tasks/$(TaskArea)/tests/*.cs" />`

`<Compile Include="$(RepositoryRoot)/eng/evaluation/results/$(TaskArea)/$(Style)/run-2/solution/*.cs" />`

Keep TaskArea/Style/AssemblyName fixed per project; for funnysharp use PackageReference FunnySharp 0.2.0, plus FunnySharp.AspNetCore 0.2.0 only for aspnetcore. Map FunnySharp* exclusively to the canonical local source in isolated NuGet.config; xunit/TestHost/runtime dependencies to the selected upstream. Give each project its own NUGET_PACKAGES and lock file. Do first restore with --use-lock-file, then locked restore; capture the package source/assets identity instead of accepting a stale global package. This is an internal verification harness, not generation study/evidence rewrite. Transient version substitution must be disclosed; CURRENT trusted .cs must be byte-identical input. Current package can reveal old solution API incompatibility; report/fix only the transient accepted-solution adapter if needed, never claim an old receipt proves it passes.

For each fixed `$kit` .csproj path from that prepared matrix and `$config` matching its isolated config, exact commands are:

~~~powershell
dotnet restore "$kit" --configfile "$config" --use-lock-file --no-cache
dotnet restore "$kit" --configfile "$config" --locked-mode --no-cache
dotnet build "$kit" --configuration Release --no-restore
dotnet test "$kit" --configuration Release --no-build --no-restore
~~~

CSV filters use `dotnet test "$kit" --configuration Release --no-build --filter "FullyQualifiedName~<Class>.<exactMethod>"`. `$kit` means the corresponding prepared current kit, or the exact workingDirectory in the runner's `rounds/0001/test.process.json`; do not guess its Guid. The CSV provides every exact Class/Method suffix. Run both styles before/after helper simplification and the mutant representatives, then the unfiltered 8/9/8/9/9 cases. A filtered subset is never the final semantic-count receipt.

The additional comparison is not called by eval-verify, release or tooling: run separately after a canonical 0.2.0 feed is available. For no source-tree outputs, use a disposable consumer with Compile links to its **four current files**, same net10/xunit4/assembly and explicit 0.2.0 local mapping, or override its BaseIntermediateOutputPath/BaseOutputPath to an artifacts-owned directory. The exact existing project test invocation is `dotnet test eng/evaluation/comparisons/function-grammar/FunctionGrammarComparisons.csproj --configuration Release`; all 37 cases are required. Per-method CSV filters assume its corresponding Release build. Its historical 37-case receipt is context, not this audit's baseline.

## Current invocation / gate matrix

All harness entries start through build.fsx's locked Debug compiled-harness launcher; it propagates the real child/module exit, not an old binary. Program.fs registers opt-in pipelines. The following is the complete relevant release invocation table from CURRENT eng/release-protocol.json, not historical PowerShell commands. Tokens root/packages/compatibilityRid/feed/output are supplied by ReleaseRun.

| Release step | Exact command arguments after dotnet | Mode |
| --- | --- | --- |
| clean | clean FunnySharp.slnx --configuration Release | both |
| restore | restore FunnySharp.slnx --locked-mode --no-cache --source {compatibilityFeed} | both |
| build | build FunnySharp.slnx --configuration Release --no-restore | both |
| test | test FunnySharp.slnx --configuration Release --no-build --no-restore | both |
| examples | run --project examples/FunnySharp.Examples/FunnySharp.Examples.csproj --configuration Release --no-build --no-restore | both |
| aspnetcore-examples | run --project examples/FunnySharp.AspNetCore.Examples/FunnySharp.AspNetCore.Examples.csproj --configuration Release --no-build --no-restore -- --verify | both |
| pack | pack FunnySharp.slnx --configuration Release --no-build --no-restore --output {packages} | both |
| format | format FunnySharp.slnx --verify-no-changes --no-restore | both |
| performance-protocol-tests | test tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj --configuration Release --no-build --no-restore --filter FullyQualifiedName~PerformanceProtocolTests | both |
| release-protocol-tests | test tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj --configuration Release --no-build --no-restore --filter FullyQualifiedName~ReleaseProtocolTests | both |
| benchmark-preflight | run --project benchmarks/FunnySharp.Benchmarks/FunnySharp.Benchmarks.csproj --configuration Release --no-build --no-restore -- --preflight | both |
| benchmark | run --project FunnySharp.Benchmarks.csproj --configuration Release --no-build --no-restore -- --filter * --artifacts {benchmarkArtifacts}, cwd={benchmarkRoot} | full only |
| performance-verify | fsi build.fsx -- -p verify-performance -RepositoryRoot {root} -ReceiptDirectory {benchmarkResults} -ObservationProposalPath {performanceObservationProposal} | full only |
| performance-docs-verify | fsi build.fsx -- -p generate-performance-docs -RepositoryRoot {root} -Verify | both |
| competitor-performance-docs-verify | fsi build.fsx -- -p generate-performance-docs -RepositoryRoot {root} -ManifestPath eng/performance/competitor-baseline.json -Verify | both |
| compatibility | fsi build.fsx -- -p compatibility -RepositoryRoot {root} -PackageDirectory {packages} -OutputDirectory {compatibilityOutput} -RuntimeIdentifier {compatibilityRid} -PackageFeed {compatibilityFeed} | both |

Thus full=16 steps, benchmarkSkipped=14; mode changes do not skip package compatibility or budget policy. verify-tooling runs the protocol's local restore/build/test/examples/aspnetcore-examples/format plus docs verifier; it explicitly marks compatibility/pack/benchmarks/filtered protocol runs not run. `-p test` invokes `dotnet test FunnySharp.slnx`. Neither full solution gate owns external kits/comparison/Compatibility runs.

| CI/other current entry | Actual invocation / scope |
| --- | --- |
| release.yml win-x64 | -p release -AttemptId <run>-<attempt>-win-x64 -CompatibilityRuntimeIdentifier win-x64 -CompatibilityPackageFeed "$COMPATIBILITY_FEED" -SkipBenchmarks; then verify-stable-api-contracts with current-proof-index and actual release-evidence |
| release.yml linux-x64 / osx-arm64 | same release flags with matching RID; then -p compatibility -PackageDirectory artifacts/canonical-win/packages -OutputDirectory artifacts/canonical-consumer/<rid> -RuntimeIdentifier <rid> -PackageFeed "$COMPATIBILITY_FEED" |
| release.yml osx-x64-consumer | same downloaded canonical package compatibility command with RID osx-x64 and -Scenario CoreSmoke,AspNetCoreSmoke |
| tooling.yml 3 OS informational matrix | dotnet test FunnySharp.slnx; -p verify-tooling; -p verify-docs-snippets; -p check-action-pins. Not the release gate; no external kits/comparison/compatibility invocation. |
| current evaluation | -p eval-prep-feed; -p eval-verify --task <area> --style <style> --run-dir <fresh-run> [--round N]; optional study rules as described above. -p eval-aggregate is reporting, not test execution. |
| vertical-slice | -p vertical-slice [--output/--feed/--package-feed/--repository-root]; separate lane. Parent baseline path artifacts/test-ablation/baseline-vertical; do not duplicate. |
| function-grammar | manual package-only project dotnet test command above; no build/CI/harness caller found in current pipeline registration or workflows. |
| competitor benchmarks | dotnet run --project benchmarks/FunnySharp.CompetitorBenchmarks --configuration Release -- --preflight; measurements -- --filter * --artifacts <dir>; verify-performance -ManifestPath eng/performance/competitor-baseline.json -ReceiptDirectory <dir>/results. Performance, not xUnit. |
| inventory dumper | -p generate-inventory drives eng/next-stage-inventory/api-inventory.csproj; generation, not a test gate. |
| live packet verification | dotnet fsi --warnaserror+ eng/verification/verify-current-performance.fsx -- .; dotnet fsi --warnaserror+ eng/verification/verify-traversal-r9-performance.fsx -- . |
| live packet controls | dotnet fsi --warnaserror+ eng/verification/offline-packet-controls.fsx -- . |
| live paired controls | pwsh -NoProfile -File eng/verification/measure-offline.ps1 -RepositoryRoot . -BaselineRoot "$baselineRoot" |
| live portable verification | dotnet fsi --warnaserror+ eng/verification/verify-portable-history.fsx -- --repository-root . --artifact-directory "$rawArchives" |
| live portable probes | pwsh -NoProfile -File eng/verification/portable-available-controls.ps1 -RepositoryRoot . -ArtifactDirectory "$rawArchives" |
| live packet cost wrapper | dotnet fsi --warnaserror+ eng/verification/measure-current-packet.fsx -- .; calls the real R9 verify and only reports CPU/alloc/time. No extra independent test. |
| frozen replay | eng/evaluation/replay-frozen.fsx; separate historical input-gated boundary, not current external test inventory. |

### Compatibility exact scenarios and prerequisites

`Compatibility.fs:77-118,481-578` is current authority. It packs nothing. Both csproj files reference package versions injected as FunnySharpPackageVersion/FunnySharpAspNetCorePackageVersion; not source projects. Package directory must contain both matching-version FunnySharp and FunnySharp.AspNetCore nupkgs; output must be a safe proper subdirectory under repo artifacts and RID must match runnable host. Local package source maps FunnySharp packages, remote source supplies runtime/ILLink/test ecosystem; cache is isolated by output root. Core SDK is Microsoft.NET.Sdk; web uses Microsoft.NET.Sdk.Web. Both enable trim/AOT analyzers and are non-packable.

| Scenarios | Extra PublishProperties | Required execution |
| --- | --- | --- |
| CoreSmoke / AspNetCoreSmoke | none | self-contained consumer executable; available shipping DLL hashes compared with canonical nupkg assemblies |
| CoreTrimmed / AspNetCoreTrimmed | -p:PublishTrimmed=true -p:TrimMode=full -p:RootShippingAssemblies=true | trimmed executable |
| CoreNativeAot / AspNetCoreNativeAot | -p:PublishTrimmed=true -p:TrimMode=full -p:PublishAot=true -p:IsAotCompatible=true -p:RootShippingAssemblies=false | native executable |

Default set is the **four trim/AOT** scenarios, not the two Smoke ones. Every scenario performs `dotnet restore <project> --configfile <config> --no-cache --runtime <rid>` with common properties `-p:FunnySharpPackageVersion=<coreVersion> -p:FunnySharpAspNetCorePackageVersion=<aspVersion> -p:SelfContained=true -p:BaseIntermediateOutputPath=<scenario>/obj/ -p:BaseOutputPath=<scenario>/bin/` plus the scenario properties, then `dotnet publish <project> --configuration Release --runtime <rid> --self-contained true --no-restore --output <scenario>/publish` with the same properties, then runs the actual published executable and requires exit0. Exit propagation is the real gate; the smoke programs print pass markers, but current invokePublishedApplication checks exit rather than parsing the marker.

C-Core filtered scenario command: `dotnet fsi build.fsx -- -p compatibility -PackageDirectory "$feed" -OutputDirectory artifacts/test-ablation/external-core -RuntimeIdentifier win-x64 -Scenario CoreSmoke,CoreTrimmed,CoreNativeAot`.

C-Asp: `dotnet fsi build.fsx -- -p compatibility -PackageDirectory "$feed" -OutputDirectory artifacts/test-ablation/external-asp -RuntimeIdentifier win-x64 -Scenario AspNetCoreSmoke,AspNetCoreTrimmed,AspNetCoreNativeAot`.

Complete local default: `dotnet fsi build.fsx -- -p compatibility -PackageDirectory "$feed" -OutputDirectory artifacts/test-ablation/external-compatibility -RuntimeIdentifier win-x64`. Parent will run compatibility after its local feed is available; these are witness plans, not duplicate baseline runs started here.

SDK is 10.0.400/latestPatch, no preview; .NET runtime packs/ILLink packages must restore from the selected upstream, and native publishing needs the host Native AOT toolchain (Windows x64 C++ compiler/linker and Windows SDK; Linux clang/zlib development libraries; macOS Xcode command-line compiler/linker tools). Toolchain availability was not probed in this read-only lane. Warnings-as-errors and all trim/AOT warnings remain; no suppression, dependency/framework replacement, cross-RID execution, or rooting shortcut is proposed.

### Live controls: exact partition ledger

`offline-packet-controls.fsx` has 37 named reject partitions when CURRENT main manifest has recordingIdentity (it does in inspected baseline):

- 13 unconditional decoded-data rejects: later-consumer-another-preverified-object; stale-source-expectation; wrong-row-policy; allocation-budget-plus-one; duplicate-launch-id; missing-launch-id; unregistered-launch-id; wrong-launch-preflight-join; wrong-loaded-mvid; wrong-r9-workload-mvid; later-assembly-other-valid-pe; wrong-semantic-delegate-member; wrong-semantic-xml-multiplicity.
- 24 conditional/fixture rejects: new-unlisted-build-input; changed-producing-source; changed-shipping-input; changed-build-input; changed-current-semantic-source; producer-as-receiver; missing-receiver; unknown-receiver; wrong-recorded-source-hash; missing-recorded-protocol-file; unsafe-recorded-protocol-path; wrong-recording-catalog-pin; changed-current-policy; changed-current-input-list; unknown-protocol-member; changed-current-base; changed-approved-snapshot; independent-snapshot-mismatch; wrong-approved-receipt-hash; wrong-approved-receipt-file; missing-approved-receipt; tampered-catalog; missing-object; tampered-physical-object. The final3 fixtures are unconditional, not inside recordingIdentity.

Positive records: genuine-receipt-index, genuine-row, genuine-workload; **8** genuine-witness-<member> entries; current-manifest-main/competitor; approved-receipts-main/competitor; current-source-partition; current-coverage; current-input-inventory; independently-reconstructed-recorded-snapshot = **19** positives, hence **56** expected records. The fixed decoded closure lists8 witnesses spanning24 executed historical cases: delegate1, long-input3+1, cancel/drain1+4+8, linear first-success3+3. These fixed closure references are not current fresh execution or new owned methods. Only the actual fixed-pin loader and validators executing now can establish current control pass; merely decoding metadata did not do so. Entry also requires exactly1 root arg, genuine workload sentinel, and removal of disposable integrity fixtures. Probe/wrapper counts are respectively4 labeled exits and7 subprocess invocations. No fresh dynamic count receipt is claimed.

## Verification status and residual boundaries

Read-only source/metadata inspection and method/InlineData inventory reconciliation were performed; final report validation checks CSV headers, quote parsing, ID uniqueness, line positions, attribute inventory equality, complete theory partition lists and helper row count. No tests/builds/native publishes/mutants were run by this worker because the parent owns baseline/final execution and instructed no duplicate baseline builds. Parent's full solution, vertical-slice and forthcoming compatibility runs do not by themselves execute current evaluation/comparison bodies; the explicit fresh-kit matrix above is needed. No archived record was written.

No proved redundancy and no missing owned test/helper after expanded discovery. Residual evidence gaps are execution, not omitted assessment: baseline/final current kits, 37-case comparison, old/new fault-witness runs, Native AOT toolchain/feed availability and actual live offline/portable controls. Historical pass metadata cannot close them. No contract requirement, performance allocation budget, case, assertion or gate was lowered. Broader product gaps visible in fixtures (for example no two-acceptor race winner test or no explicit lower-bound plausible-temperature test) are not ablation candidates and were left unexplored.
