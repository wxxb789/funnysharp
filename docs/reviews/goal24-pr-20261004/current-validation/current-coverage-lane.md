# Current package/runtime coverage witness closure

Status: COMPLETE_WITHIN_BOUNDED_EVIDENCE_SCOPE. Candidate: `1fbb50df35e6b1a4cd423460eb6d2d149a64517f`. No source/protocol/observation/original proof changes were made by this lane.

Integration root: `Q:/repos/funnysharp/.worktrees/goal24-complete-pr`.
Evidence root: `Q:/repos/funnysharp/.worktrees/goal24-complete-pr/artifacts/pr-validation/1fbb50d-performance-20261004-01/current-coverage`.
All artifact paths below are relative to the evidence root unless stated otherwise. `commands.json` records all eleven evidence commands with full argv-form command text, monitor identifiers and actual exits. The deep-low child performed this work directly; no further delegation was used.

## Actual fresh binary bindings

Every one of the 190 main rows and 40 competitor rows has exactly one hash-bound actual launch log. The verifier read their base64 workload bindings, joined them to the corresponding preflight runId/candidateSnapshot, checked retained workload and loaded-assembly bytes, checked the receipt-bound report/preflight/log hashes and rejected duplicate row IDs. Fifteen successful receipts were inspected: thirteen main plus two competitor. The successful binding verifier checked 338 bound path locators; an independent PowerShell SHA256 traversal checked 368 bound path locators across the census, binding proof and witness closure (path-spelling aliases are counted, so these are not asserted to be 368 unique physical files).

| Assembly | SHA256 | MVID | Surface |
| --- | --- | --- | --- |
| FunnySharp.dll | `978447b469a7368c22eec343996bbb586bdb9716ec20cb3bf26154324477adbf` | `118337a4-8968-4a78-931a-f21df93fcceb` | 48 types, 479 members, 448 stable, 31 Experimental |
| FunnySharp.AspNetCore.dll | `91d970c508d223722689b8d6556632d8a5376d15f0ebe7141f865ce7dcef8102` | `67eec3d2-ad5c-4837-9195-615965dcc726` | 1 type, 20 stable members |

Census DLL locators are the retained actual main-launch binaries under `artifacts/performance-preflight/f23b5e1f97e64a328e77bf3e2687121d/binaries/<sha256>/`. Competitor's retained core under run `e61ac7c1dfd8412195a92fafaacab291` has the same SHA256. Main launches also bind the exact HTTP DLL above. The witness project's copied `attempt-01/bin/Release/net10.0/FunnySharp.dll` independently hashes to the same core bytes. Historical `6a903f737356fdf67845c9cd39385e4a5e2b39bcae0ee92ff2882480ecb7e49d` package bytes were not reused or relabeled as current.

`census.fsx` reuses the built canonical `ReleaseVerifyArtifacts.getPublicApiInventory` and `renderPublicApiText` producer. It requires actual baseline line equality for both retained DLLs, reflects complete public DeclaredOnly member identities/metadata tokens and Experimental diagnostics, and rejects totals other than 49 types / 499 members / 468 stable / 31 Experimental. Both actual rendered surfaces are retained as `FunnySharp.actual-public-api.txt` and `FunnySharp.AspNetCore.actual-public-api.txt`. Baseline SHA256 values are `57f80c4aa428a6a45cd0ca54c1fc0b4244a07d6047ed01a50e789b0cd4ea9cb5` and `90afec795c3ddc3ca2266bf8e80e3412f00aad223e5c70a22103b8140d46c794`.

## Actual runtime witnesses

The existing producers identified by the historical Compile includes were reused as current source links: the current core suite wildcard from `PackagedCoreContracts.csproj`, including the identical `GeneratedDelegateContractTests.cs` named in `PackagedStableContracts.csproj`. The fresh project uses a direct reference to the retained benchmark-bound core DLL, rather than substituting an old FunnySharp NuGet package or rebuilding source. Its xUnit 4.0.0 dependency graph is the old locked core graph with only the replaced FunnySharp PackageReference node removed. Actual `dotnet restore --locked-mode --no-cache` succeeded against the verified TLS proxy and SDK library packs. The build succeeded with zero warnings and zero errors.

The selected eight methods produced 24 passing cases, 0 failures, 0 errors, 0 skipped and 0 not run on .NET 10.0.12. Seven mandatory resource methods contribute 23 cases; the delegate Fact contributes one case. Both CTRF and TRX are retained and their complete passing outcomes, method names and exact case multiplicities are independently joined by `verify-witnesses.fsx`.

| Kind | Existing method | Cases | Current source range |
| --- | --- | --- | --- |
| delegate-apm | `ModernRuntimeRejectsGeneratedDelegateAsyncMembersWithoutInvokingTargetsOrCallbacks` | 1 | `tests/FunnySharp.Tests/GeneratedDelegateContractTests.cs:6-38` |
| long-input-bound | `ExplicitFirstSuccessBoundLimits1024HeldCandidatesAndRefillsExactlyOneSlot` | 3 | `tests/FunnySharp.Tests/FirstSuccessTests.cs:875-898` |
| long-input-bound | `SelectParallelCompletionOrderValueAsyncBoundsUndeliveredWorkAndKeepsStreaming` | 1 | `tests/FunnySharp.Tests/ParallelAsyncEnumerableTests.cs:930-1013` |
| cancel-drain | `FirstSuccessAwaitsRealUsingAsyncDisposalAndPropagatesItsFailureAfterAWinner` | 1 | `tests/FunnySharp.Tests/FirstSuccessTests.cs:617-658` |
| cancel-drain | `ExplicitFirstSuccessBoundStopsAdmissionAndDrainsOnTimeoutOrCallerCancellation` | 4 | `tests/FunnySharp.Tests/FirstSuccessTests.cs:904-940` |
| cancel-drain | `ExternalCancellationDoesNotPublishANonCooperatingSelectorAndCleansUpOnce` | 8 | `tests/FunnySharp.Tests/ParallelAsyncEnumerableTests.cs:318-416` |
| linear-first-success | `ExplicitFirstSuccessHandlesLargeSynchronousFailuresInOrderWithOneValueTaskConsumption` | 3 | `tests/FunnySharp.Tests/FirstSuccessTests.cs:945-961` |
| linear-first-success | `ExplicitFirstSuccessAccountsForLargeStaggeredBatchesWithoutReorderingTypedFailures` | 3 | `tests/FunnySharp.Tests/FirstSuccessTests.cs:966-997` |

The delegate Fact actually invokes all six generated BeginInvoke/EndInvoke methods on `TryOperation<int>`, `StateTransition<int, int>` and `StateMachine<int, int, int, string>`, requires `PlatformNotSupportedException`, and asserts zero target invocations and zero callback invocations. It does not claim asynchronous delegate execution succeeds on modern .NET.

For the parent's coverage binding: `metadataCensus` should use `package-inventory.json`; generated-member `runtimeWitness` can use `attempt-01/contracts-01.json` with the method above, current generated-delegate source hash and the copied core DLL hash. All seven mandatory witness `execution` bindings can use `attempt-01/contracts-01.trx` and the same copied core DLL. `witness-closure.json` supplies exact source/hash/range/method/case/report/coreDll objects for these joins. This lane did not modify stable-members.json.

## Commands and exits

The explicit verified RestoreSources were `https://packagefeedproxy.microsoft.io/nuget/v3/index.json;C:\Program Files\dotnet\library-packs`; no TLS, auditing, warning or restore-lock bypass was used. `run.js` fixes the integration cwd and those restore sources. Commands ran via native monitor/background sessions; no foreground sleep/poll loop or benchmark rerun occurred.

| Step | Command after runner prefix where applicable | Actual exit | Result |
| --- | --- | --- | --- |
| Locked restore | `dotnet restore attempt-01/CurrentCoreContracts.csproj --locked-mode --no-cache --configfile attempt-01/NuGet.Config` | 0 | Locked xUnit graph restored |
| Metadata census | `dotnet fsi census.fsx` | 0 | 49/499/468/31, both baseline renderings equal |
| Build | `dotnet build attempt-01/CurrentCoreContracts.csproj -c Release --no-restore` | 0 | 0 warnings, 0 errors |
| Runner help | `dotnet attempt-01/bin/Release/net10.0/CurrentCoreContracts.dll --help` | 2 | Actual xUnit help and result/filter syntax; usage exit, not a test failure |
| Binding verifier attempt 1 | `bun verify-bindings.js` | 1 | ArrayBuffer representation error preserved |
| Selected tests | Full eight `-method` filters in `commands.json`; `-failSkips -failWarns -result-ctrf attempt-01/contracts-01.json -result-trx attempt-01/contracts-01.trx` | 0 | 24/24 pass |
| Binding verifier attempt 2 | `bun verify-bindings.js` | 0 | 190+40 rows/launches, 338 bound locators |
| Witness verifier attempt 1 | `dotnet fsi verify-witnesses.fsx` | 1 | F# FS0597 indexing compile failure preserved |
| Witness verifier attempt 2 | `dotnet fsi verify-witnesses.fsx` | 1 | F# FS0597 chained indexing compile failure preserved |
| Witness verifier attempt 3 | `dotnet fsi verify-witnesses.fsx` | 0 | 24 exact cases, 7 resource methods, 6 delegate APM methods |
| Independent hashes | `pwsh -NoProfile -File check-hashes.ps1` | 0 | 368 bound locators; closure SHA256 `a8c0d9e86701cd3441b481e0fe2b9bdcde374d6ff5ad759e9c5ec0de87e6df0e` |

Eleven evidence commands: seven exits 0, three preserved verifier failures with exit 1, and one help/usage exit 2. Failed sources and failure receipts are retained beside the passing producers; no failing test was edited or retried. The final FSI and Bun producers passed after syntax/input-representation fixes. F# LSP was unavailable because fsautocomplete is not installed; the JS LSP request timed out. No install was attempted outside this lane. Actual FSI compilation, Bun execution and the zero-warning .NET build provide executed validation rather than an LSP-clean claim.

## Artifact SHA256

| Evidence-root-relative path | SHA256 |
| --- | --- |
| `package-inventory.json` | `891ef1a7ffcba37dcb23f2b3ae607f6a9f56d6fe98de23e0a9be6213e8fc6c45` |
| `binding-check.json` | `2a3390305593a89d67513999f88a21eb787eaec26ec158fb2fb0705f72c1c9db` |
| `witness-closure.json` | `a8c0d9e86701cd3441b481e0fe2b9bdcde374d6ff5ad759e9c5ec0de87e6df0e` |
| `independent-hash-check.json` | `a4f52d50d853705f9a0d3fec1eb941a73aeade4ed47cc5388e906bffab5d2b4f` |
| `attempt-01/contracts-01.json` | `a25c969af023687f01860b39b07839a467667bcc5175b9aba7661ea1df3864d9` |
| `attempt-01/contracts-01.trx` | `82771db93caa5a66c6ff5b98dd4d2f2fee336f2d440d5d4392dad0e77d0164f6` |
| `attempt-01/bin/Release/net10.0/CurrentCoreContracts.dll` | `8e376cb94b07641c63a4abb1788cd6f695b19e1d8bcb6c71b11ca17875554981` |
| `commands.json` | `fd0460e1355fc9c7236a7b2f88d8424b2b4cf575140dcf487808933f0aeb5717` |
| `census.fsx` | `d07def1a476d82be777aa753108ed97ac6090d215b8fe899a634037cc93edc36` |
| `verify-bindings.js` | `8e9c9b854c3cd2dfa058c37545e88b917748eb19cf8641c7409b6f9d21ad6a13` |
| `verify-witnesses.fsx` | `a115a8cc3a730b90a13433a4c9a754f08f084bb9b5f177d6aebebc6b232c0310` |
| `check-hashes.ps1` | `d7e0f8a4ddd01ad425de415b9c517156c8f31d65e3fd0bea4ebeeeb0d0fd9b11` |
| `verify-bindings.failed-arraybuffer.js` | `d5b306b955b2e6e534667e7b7a18d62215ffb43424f58cc172e2df771777009c` |
| `verify-witnesses.failed-indexing.fsx` | `a46d537059e21dd66a7c92848bc7e4f18dd780391297732e6cae2a842d46f054` |
| `verify-witnesses.failed-chained-indexing.fsx` | `019e6eeb79e92d21b424bf197b0cac9a37eb35cf81ae35067ed02d3e94d18a17` |

## Scope, assumptions and limits

The relevant current contracts ask for exact DLL bytes compatible with actual fresh benchmark launch bindings; direct references to those retained DLLs meet that byte boundary without manufacturing or altering a NuGet archive. The census is actual shipping-assembly metadata, not a new nupkg layout/feed/publication proof. Current source links are the original tests, with no copied-source adaptation; their hashes/ranges are in the closure. Experimental classification comes from actual assembly metadata, not policy exclusions. The original census script was not located, so the canonical harness rendering producer was reused with bounded reflection glue instead of treating the historical census as current.

No claim is made about three new numeric repetitions, full release acceptance, a full core suite rerun, source mapping correctness, policy/observation update completion, package publication, remote CI, model/HTTP cohorts or an overall final gate. Parent owns those integrations and final verification. At the final read-only worktree probe the only tracked integration delta was the parent's fourteen-line stable-members.json mapping update; this child authored only current-coverage artifacts and this handoff Markdown. The bounded witness closure is complete and independently hash-checked.
