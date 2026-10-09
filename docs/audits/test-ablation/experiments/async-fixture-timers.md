# Async-stream fixture timer ablation

## Scope and change

The assessment identified five `Task.Delay(1)` sites: four async-stream source fixtures in
`AsyncStreamsTests.cs` and the `ReferenceService` check in `Contract.cs`. No test asserts elapsed
time. Each timer was replaced with `await Task.Yield()`, preserving an actual asynchronous
suspension. `ReferenceService.IsPlausibleAsync` now checks cancellation before and after the
suspension. `TokenAwareStream` likewise checks before and after suspension. The source-owned
post-first-yield `OperationCanceledException`, exact token forwarding, source order, one source
enumeration, and yield counters remain unchanged. No test case, input partition, or assertion was
removed; no helper abstraction was added.

## Executed healthy controls

Disposable consumers were created under `artifacts/test-ablation/async-kits/`, with explicit
Compile links to the current trusted tests and accepted `SensorStream.cs` sources. The FunnySharp
consumer used package `FunnySharp 0.2.0` from
`Q:/repos/funnysharp/artifacts/test-ablation/baseline-vertical-mirror/feed`; xUnit and runtime
packages came from the public mirror. Both consumers completed first restore, locked restore, and
Release build with exit `0` and `0 Warning(s), 0 Error(s)`.

Commands (each kit, exact path substituted):

```text
dotnet restore <kit> --configfile <NuGet.config> --use-lock-file --no-cache
dotnet restore <kit> --configfile <NuGet.config> --locked-mode --no-cache
dotnet build <kit> --configuration Release --no-restore
dotnet test <kit> --configuration Release --no-build --no-restore
```

The first test invocation incorrectly forwarded `-- -failSkips -failWarns` to the standalone
runner and produced zero tests (exit 5); it was not treated as evidence. The corrected standalone
command above completed:

| style | total | passed | failed | skipped | exit |
| --- | ---: | ---: | ---: | ---: | ---: |
| idiomatic | 8 | 8 | 0 | 0 | 0 |
| FunnySharp | 8 | 8 | 0 | 0 | 0 |

The runner's actual standalone output was `Test run summary: Passed!` for both styles. The eight
tests cover filter/map, running maxima/order, all-plausible validation, accumulated failures,
non-temperature input, empty input, pre-canceled input, and source-owned cancellation.

## Initial worker correspondence argument

The retained oracle independently observes the representative obligations: missing temperature
filtering changes exact temperature output and reference-check count; wrong maxima/order changes
exact sequences; fail-fast validation changes the complete error partition and ordered checks;
ignored cancellation changes the pre-call cancellation assertion and exact token checks; swallowed
source faults changes the required `OperationCanceledException` and zero reference checks. These
observations do not depend on timer elapsed time. The candidate and old accepted `SensorStream.cs`
inputs are byte-identical apart from the fixture change, so the same fault classes are expected to
remain rejected by the same eight assertions in each style. Disposable fault-mutant execution was
not completed in this worker; this is the correspondence argument, not executed mutant output. No
compile failure is counted as fault evidence.

## Parent executed same-fault proof

The parent completed the previously missing experiment. Four disjoint kits under
`artifacts/test-ablation/async-proof/{old,new}/{idiomatic,funnysharp}` link the
actual old or new trusted fixtures and compile disposable accepted consumer
copies against the current local 0.2.0 package. Each kit completed both first and
locked restores, a zero-warning/error Release build, and all eight healthy tests.

Five identical fault classes were then exercised in both styles and both fixture
generations. Every fault successfully compiled before its selected real test
failed with `Total: 1, Errors: 0, Failed: 1, Skipped: 0`, exit 1:

| Fault | Retained independent test |
| --- | --- |
| Admit non-temperature input | FilterMapKeepsOnlyTemperatureReadingsInCelsius |
| Replace running maximum with current value | RunningMaximaTrackTheMaximumSoFar |
| Stop/truncate accumulated errors | ImplausibleReadingsAccumulateEveryFailure |
| Substitute CancellationToken.None at reference call | AllPlausibleReadingsCollectTheValidBatch |
| Swallow source OperationCanceledException into empty report | SourceCancellationSurfacesAsOperationCanceledException |

This is **20 actual fault rejections** after 20 successful fault builds. All four
restored full suites then passed **8/8**, with zero errors/failures/skips/not-run.
`matrix.ps1` preserves exact argument vectors and requires those count/exit
conditions; each `receipts/{generation}-{style}/receipt.json` and stdout/stderr
log retains the actual output. Compile failure never counts as detection.

All twenty temporary fault copies were restored through apply_patch to the
accepted consumer contents. Direct text/byte-equivalence checks (no SHA) confirm
all twenty restorations. No frozen result, template or shipping source changed.
Each launched child exited and no matrix process remains active. The parent
queried CSharp LSP for both changed test/helper files: zero errors. The final
repository formatter gate exited 0 (`39.731 s`).

The restored standalone times were old 0.756/0.755 s and new 0.321/0.353 s for
idiomatic/FunnySharp. These single-run observations are not an overall speedup
claim. The deterministic structural reduction is five scheduling timer sites;
the fixtures still genuinely suspend and preserve cancellation observations.

## Burden and limits

The five fixed waits are eliminated. No paired timing was recorded: the requested evidence is
semantic and timer duration is not a contract. The permanent helper/source burden changes by five
one-line waits to five one-line `Task.Yield` suspensions plus the required cancellation checks;
there is no new fixture helper or framework abstraction. Disposable kit files and generated build
outputs are not evidence and must be removed by the owning integration run; no historical results,
templates, or accepted source files were edited.
