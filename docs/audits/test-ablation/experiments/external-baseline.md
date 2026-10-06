# External current-package baseline

Status: completed baseline execution. All eleven disposable kits passed their complete unfiltered gates: 123 cases, zero failed and zero skipped. The parent took over the worker's incomplete first-wave handoff and executed every remaining kit; launched jobs were not counted as completed results.

## Scope and choice

Path B from the complete external assessment was selected over Path A. Path A would replay templates pinned to FunnySharp 0.1.0, whereas the requested baseline is an actual 0.2.0 package consumer. Eleven fresh projects are under `Q:/repos/funnysharp/artifacts/test-ablation/external-baseline-kits`; they link current trusted tests and read-only run-2 accepted consumer sources, or the four current function-grammar comparison files. No ProjectReference is present. No model study or frozen record is rewritten.

The worktree was `Q:/repos/funnysharp/.omo/worktrees/test-ablation-g001`, branch `ulw/test-ablation-g001`, actual observed HEAD `141f9d04adecc35a3851c6ca1d6812d6aa9f32ba`. The assessment cites earlier `0c5416e37e06db17c30c014659601d0c463b973a`; actual compile inputs, not that older label, identify this execution. SDK was 10.0.401 (the 10.0.400/latestPatch policy).

The package source is exclusively `Q:/repos/funnysharp/artifacts/test-ablation/baseline-vertical-mirror/feed` for FunnySharp*, both requested packages version 0.2.0. Ecosystem dependencies map to `https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json`. Every kit has its own .packages and .http-cache directory, NuGet.config, packages.lock.json, obj, bin and attempt receipts.

Source-derived counts are 8/9/8/9/9 per style (86), plus the comparison's 9 Facts and 28 InlineData cases (37), total 123. Historical run-2 acceptance is input provenance only; it is not a current pass. Parent-owned solution 2703, vertical 50 and compatibility 6 gates are not duplicated.

## Existing timing defects

Seven fixed-delay sites remain unchanged in current trusted inputs:

- `eng/evaluation/tasks/async-streams/tests/Contract.cs:25`: `await Task.Delay(1, cancellationToken);`.
- `eng/evaluation/tasks/async-streams/tests/AsyncStreamsTests.cs:18`: `await Task.Delay(1);`.
- `eng/evaluation/tasks/async-streams/tests/AsyncStreamsTests.cs:34`: `await Task.Delay(1);`.
- `eng/evaluation/tasks/async-streams/tests/AsyncStreamsTests.cs:42`: `await Task.Delay(1);`.
- `eng/evaluation/tasks/async-streams/tests/AsyncStreamsTests.cs:61`: `await Task.Delay(1);`.
- `eng/evaluation/tasks/concurrency/tests/Contract.cs:41`: `await Task.Delay(entry.CheckDelayMs, cancellationToken);`.
- `eng/evaluation/tasks/concurrency/tests/Contract.cs:87`: `await Task.Delay(offer.ProbeDelayMs, cancellationToken);`.

The concurrency fixtures request 5-60 ms completion ordering. Overlap can depend on work starting before 40 ms elapses, and a delayed cancellation continuation can lose to the 60 ms completion timer even after FirstCheckEntered/FirstProbeStarted. Async-streams uses five 1 ms suspension sites without a time-based contract. A passing baseline does not prove these fixtures deterministic. This lane records, but does not patch or hide, those pre-existing defects. Comparison WaitAsync(10 seconds) bounds an already subscribed operation; it is a failure deadline, not a fixed sleep.

## Actual baseline results

Every kit completed use-lock restore, locked restore, no-restore Release build,
and unfiltered no-build/no-restore tests with exit `0`. Every receipt records
the exact child arguments and `HasExited=true`. The source inputs remained
current trusted tests plus read-only accepted consumer implementations. The
FunnySharp styles resolved `FunnySharp/0.2.0` (and the ASP.NET Core package where
needed) exclusively from the named freshly packed local feed.

| Kit | Passed / failed / skipped | Complete sequential kit elapsed |
| --- | --- | ---: |
| business-outcomes/idiomatic | 8 / 0 / 0 | 23.497 s |
| business-outcomes/funnysharp | 8 / 0 / 0 | 22.228 s |
| collections/idiomatic | 9 / 0 / 0 | 21.779 s |
| collections/funnysharp | 9 / 0 / 0 | 23.226 s |
| async-streams/idiomatic | 8 / 0 / 0 | 21.127 s |
| async-streams/funnysharp | 8 / 0 / 0 | 22.202 s |
| concurrency/idiomatic | 9 / 0 / 0 | 21.458 s |
| concurrency/funnysharp | 9 / 0 / 0 | 21.083 s |
| aspnetcore/idiomatic | 9 / 0 / 0 | 18.751 s |
| aspnetcore/funnysharp | 9 / 0 / 0 | 19.144 s |
| function-grammar/comparison | 37 / 0 / 0 | 18.370 s |

These times include restores and build, not just test execution, and independent
kits ran in bounded waves. Summing them does not produce wall-clock duration or
an ablation speedup. The machine-readable summary is
`artifacts/test-ablation/external-baseline-kits/summary.json`; full commands,
stdout/stderr, compile inputs, reference paths, package assets and process
receipts remain under each kit's `receipts/attempt-1/`.

The initial worker returned while four jobs were outstanding. The parent read
their actual receipts (34 cases), ran the remaining seven kits (89 cases), and
verified all eleven result counts against the source matrix. No historical
receipt, zero-test invocation or merely started job contributes to these totals.

