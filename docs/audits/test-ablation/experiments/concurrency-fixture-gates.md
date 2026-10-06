# Concurrency fixture causal gates

## Scope and executed old control

Base: 141f9d04adecc35a3851c6ca1d6812d6aa9f32ba. Worktree: Q:/repos/funnysharp/.omo/worktrees/test-ablation-concurrency-fixtures.
Assessment: external.md, strongest group 1, read from the assessment worktree.
Permanent edits: concurrency Contract.cs, ConcurrencyTests.cs, both current prompts, and this report. Accepted results/concurrency/{idiomatic,funnysharp}/run-2/solution/AvailabilityCoordinator.cs inputs are unchanged. No shipping source or frozen result is edited.

Disposable consumers: artifacts/test-ablation/concurrency-fixtures/{old,new}/{idiomatic,funnysharp}. Old tests are exact copies of the pre-edit trusted input; new projects link current trusted tests. Both compile disposable accepted coordinator copies with explicit Compile closure, no ProjectReference. xunit.v3 4.0.0, net10.0, standalone ConcurrencyKit.dll, warnings as errors. FunnySharp consumers substitute 0.2.0 for the stale template's 0.1.0. FunnySharp* maps exclusively to Q:/repos/funnysharp/artifacts/test-ablation/baseline-vertical-mirror/feed; other packages use https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json. Each consumer has isolated package/cache directories and a lock file. Initial and locked restores completed before healthy builds.

Old healthy controls: both styles compiled with 0 warnings and 0 errors, then passed 9/9, Errors 0, Failed 0, Skipped 0, Not Run 0, exit 0. Every old fault compiled with exit 0 and was rejected by the standalone runner with exit 1, never by compilation. Afterward both default-source builds and all nine old cases passed again. Candidate healthy-fixed controls also compile with 0 warnings and 0 errors and pass all 9 cases per style, exit 0, Errors/Failed/Skipped/Not Run all 0. The first candidate healthy build failed xUnit1051 at 20 new event waits per style; it ran no tests and is not fault evidence. Those waits now use TestContext.Current.CancellationToken without suppression; cleanup remains independently bounded. Original failed-build logs remain under new-<style>-healthy. Both identical candidate fault matrices and restored gates have now completed successfully.

## Detector obligations and retained partitions

No test or input partition is removed. Removed obligations are clock-specific fixture mechanics: 60/45/30/15 ms source-order delays plus immediate unknown stock; equal 40 ms overlap windows; four 10 ms degree-one completions; supplier 5/20/35 ms and 20/5/10 ms ordering; cancellation racing 60 ms timers; and unused empty-fixture 10 ms configurations. These are not elapsed-time product requirements. Two scheduling timer source sites are removed: WarehouseGateway.CheckAsync and SupplierGateway.ProbeAsync.

| Independent test owner | Preserved input and machine observation |
| --- | --- |
| ResultsComeBackInSourceOrderWhenEarlyItemsFinishLast | keyboard 40/quantity 2, mouse 15/1, monitor 8/9, trackball 3/1, unknown webcam 0/1; all five enter before release; actual gateway task completion webcam/trackball/monitor/mouse/keyboard; exact source-ordered SKU, OnHand and Sufficient arrays. |
| ChecksOverlapButNeverExceedMaxConcurrency | Six distinct inputs, degree 3; any two registered entries held concurrently prove overlap; in-flight range 2..3 before release and historical max <= 3 afterward; complete start membership and exact output SKU order. |
| MaxConcurrencyOfOneRunsOneCheckAtATime | Four inputs, mouse quantity2, degree 1; await whichever registered entry starts next, assert in-flight 1, release it, await actual completion, advance. Exact start count 4, complete membership and source-ordered output. |
| FirstAcceptingSupplierWinsEvenWhenDeclinesFinishFirst | north/south decline, east accepts res-3003; all three enter; release/await actual probes north/south/east. Exact east winner/reservation, no failures, all three starts and overlap. |
| EveryDeclineIsListedInInputOrderWhenAllSuppliersFail | Three declines; actual completion south/east/north; null winner/ID and exact north/south/east failures. |
| CancelledAvailabilityCheckSurfacesOperationCanceledException | Three items, degree 2; any two registered entries held with no completed check and pending coordinator before cancellation; OperationCanceledException, 2..3 unique input starts and max in-flight <= 2, no controlled completion and in-flight 0 after drain. |
| CancelledReservationSurfacesOperationCanceledException | north declines, south would accept res-2; both entered and held before cancel; exact two starts, no controlled completion, OperationCanceledException and in-flight 0 after drain. |
| EmptyItemListStartsNoChecksAndReturnsNoResults | Empty input; empty results, StartedChecks 0. |
| EmptySupplierListStartsNoProbesAndReservesNothing | Empty input; false/null/null/empty failures, StartedProbes 0. |

Entered/release pairs are registered before coordinator invocation. Entered carries the actual gateway Task, so completion observes that Task, not an earlier finally notification. Start/completion histories are copied under the existing lock. Cancellation interrupts the held release wait; accounting decrement remains in finally. Tests release every registered input in finally and drain the coordinator with a bounded deadline, including failed overlap and ignored-cancellation faults. OperationCanceledException is tolerated only during cleanup; acceptance assertions require it explicitly.

The 10-second WaitAsync deadline bounds missing entry/completion/cancellation events; it is never successful scheduling delay. A serial fault must time out at mouse entry while keyboard is held and then drain, not wait forever for all entries. No retries, polling, sleeps, dependencies, generic scheduler, or new framework.

CheckDelayMs and ProbeDelayMs are removed, not retained as dead fields. Both accepted sources consume only gateway replies; templates contain no source; current prompts describe those fields solely as fake scheduling controls. Prompts still describe timers and FirstCheckEntered/FirstProbeStarted, now replaced by per-input signals. Updating prompts is outside this worker's three-file permanent scope; the parent must resolve this mismatch before using the task as a new generation prompt.

## Commands and receipts

Let A be the worktree's artifacts/test-ablation/concurrency-fixtures, G old/new, S idiomatic/funnysharp. Disposable scripts preserve exact argument vectors, working directory, stdout, stderr, exit, deadline status and child HasExited in A/receipts/G-S-L/ for label L. These are executed machine output, not historical acceptance.

~~~~powershell
pwsh -NoProfile -File "$A/run.ps1" -Generation $G -Style $S -Label healthy -Mode prepare
pwsh -NoProfile -File "$A/matrix.ps1" -Generation $G -Style $S
~~~~

Prepare performs dotnet restore <kit> --configfile <config> --use-lock-file --no-cache, then --locked-mode --no-cache, then dotnet build <kit> -c Release --no-restore --disable-build-servers -nr:false -p:UseSharedCompilation=false -p:CoordinatorSource=AvailabilityCoordinator.cs, then:

~~~~text
dotnet <kit>/bin/Release/net10.0/ConcurrencyKit.dll -failSkips -failWarns -noLogo -class ConcurrencyTests
~~~~

Fault F uses the same no-restore build with -p:CoordinatorSource=faults/F.cs and standalone xUnit -method ConcurrencyTests.<exactMethod>, once per selected method. No assumed --filter-method. Child processes have an outer 120-second kill-tree bound, independent of 10-second event deadlines. Matrix requires build 0/test 1 and no outer timeout, then rebuilds unchanged accepted source and runs all 9.

| Fault | Same representative change in old/new | Exact filtered method suffix(es) | Old counts per style |
| --- | --- | --- | --- |
| serial | Replace requested availability degree with 1. | ChecksOverlapButNeverExceedMaxConcurrency | Total 1/Failed 1/Skipped 0 |
| over | Admit items.Count regardless of requested degree. | ChecksOverlapButNeverExceedMaxConcurrency; MaxConcurrencyOfOneRunsOneCheckAtATime | Total 2/Failed 2/Skipped 0 |
| order | BCL bounded checks append ItemAvailability to locked results on reply completion, returning completion order; reservation source stays accepted. | ResultsComeBackInSourceOrderWhenEarlyItemsFinishLast | Total 1/Failed 1/Skipped 0 |
| decline | Treat null reservation ID (declines) as accepting. | FirstAcceptingSupplierWinsEvenWhenDeclinesFinishFirst | Total 1/Failed 1/Skipped 0 |
| failures | Reverse reported all-decline failures. | EveryDeclineIsListedInInputOrderWhenAllSuppliersFail | Total 1/Failed 1/Skipped 0 |
| cancel | Assign CancellationToken.None at both workflow entry points. | CancelledAvailabilityCheckSurfacesOperationCanceledException; CancelledReservationSurfacesOperationCanceledException | Total 2/Failed 2/Skipped 0 |

Old selected faults give 8 failed cases/style. Old stdout: serial max in flight 1; over 6 versus cap 3 and 4 versus cap 1; order expected keyboard, actual webcam; decline expected east, actual north; failures expected north, actual east; cancellation No exception was thrown. Complete exact spacing, stack traces, assembly IDs, timings and summaries are retained in stdout logs.

## Initial candidate execution and cleanup

Both candidate styles reject every identical representative fault after a successful build: serial 1/1 failures, over 2/2, order 1/1, decline 1/1, failures 1/1, cancellation 2/2. Every fault runner exits 1 with Errors 0, Skipped 0, Not Run 0; no outer timeout occurs. Across old/new and both styles this is 24 successful fault builds and 32 contractual rejection cases.

Candidate stdout for serial is TimeoutException at the keyboard/mouse-entry WaitAsync (line 85), about 10.3 seconds per style; keyboard is still held until finally releases it and DrainAsync completes. This is a bounded missing-overlap event, not deadlock or process kill. Over-admission reports 6 in flight outside range 2..3 and 4 instead of degree 1. Completion-order reports webcam instead of keyboard; decline reports north instead of east; reversed failures report east instead of north. Both ignored-cancellation cases report expected OperationCanceledException versus actual TimeoutException at their 10-second cancellation-result deadline. The outer process remains responsive; finally releases held calls and drains it, and both cancellation cases finish in approximately 20.3 seconds combined per style.

Final restored candidate stdout is exactly:

~~~~text
ConcurrencyKit  Total: 9, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0
~~~~

The idiomatic restored runner reports Time 0.387s; FunnySharp reports 0.442s. Both restored builds exit 0 with 0 warnings and 0 errors, and both runners exit 0. Old restored all-nine gates were also green. Across initial healthy and restored old/new controls, 72 healthy case executions pass. These single-run timings do not establish overall speedup.

Receipt labels are old-{style}-healthy, old-{style}-{serial,over,order,decline,failures,cancel}, old-{style}-restored; new-{style}-healthy (the initial xUnit1051 build failure only), new-{style}-healthy-fixed, new-{style}-{serial,over,order,decline,failures,cancel}, and new-{style}-restored. All scripts await the exact dotnet child to HasExited=true and record timedOut=false. The copied default coordinator files were never mutated: the build selects a separately copied fault file through CoordinatorSource. After proof, all 24 fault copies were restored to accepted content with apply_patch. Byte-for-byte cmp verifies each of four default copies against its original frozen accepted source, and all six restored fault copies per consumer against that default (28 comparisons). It emits RESTORED_BYTES for old/new x idiomatic/funnysharp and exits 0. No frozen source is restored or rewritten.

A/mutations.patch preserves the exact 24 old/new fault source diffs for reproduction. To reproduce, feed that patch text to apply_patch, then run A/run.ps1 with a fresh receipt label (for example repeat-serial), Mode build, CoordinatorSource faults/serial.cs and the exact Methods suffix in the table. Each original receipt.json contains the complete argument vector; use it rather than inferring runner filters. Restore those disposable fault copies again with apply_patch afterward. Matrix's original labels are immutable and intentionally refuse receipt overwrite.

Final Win32_Process inspection finds no dotnet.exe or pwsh.exe command referring to this isolated worktree (excluding the inspection process itself), emits NO_WORKTREE_PROCESSES and exits 0. Both matrix gates and restoration checks have finished; no monitor/gate process remains active. git diff --check exits 0 and git status lists only the two allowed test files plus this new report. The test-file LSP diagnostics are clean; the initial Contract.cs LSP request timed out, but every current consumer build independently compiled it with 0 warnings and 0 errors.

The full solution/release gates and authoritative integration belong to the parent; this worker runs only the bounded package-consumer proof. Current generation prompts retain the stale fixture description noted above.

## Parent correction and final bounded acceptance

The parent found two additional assertions that were not product contracts:
exactly two starts after cancellation, and source-order gateway starts. A legal
SemaphoreSlim coordinator can start the third call with an already-canceled
token when a canceled held call releases its slot. Source-order results do not
require source-order admission. These new fixture locks were corrected rather
than weakening concurrency, output, input-membership or cancellation checks.

The current tests wait for any two already-registered entries and release the
actual next registered degree-one entry. Started histories use sorted membership
with exact counts where contractual; source-ordered replies remain exact.
Post-cancel starts are bounded 2..input-count with unique input membership,
historical concurrency <= 2, zero controlled completions, and zero remaining
in-flight calls. The returned workflow still must throw OperationCanceledException.

A deterministic valid coordinator that admits a third already-canceled call
passes the selected cancellation test (1/1). A valid coordinator that starts
availability calls in reverse order while returning source-order replies passes
the complete nine-test suite (9/9). One attempted control used concurrent builds
of the same kit and failed with CS2012; it is retained as an invalid experiment,
not a fault witness or acceptance result. The parent stopped that conflicting
run and completed the valid reverse-order control serially with zero compiler
warnings/errors. No two variants of one kit are subsequently built concurrently.

After those corrections the parent replayed all six original faults in both
styles using the exact preserved `mutations.patch`, not a new weaker mutation:
serial 1/1 failures, over-admission 2/2, completion-order 1/1, decline 1/1,
reversed failures 1/1, ignored cancellation 2/2. This adds **12 successful fault
builds and 16 actual rejection cases** on the final trusted test contents.
Every result has Errors/Skipped/Not Run 0, exit 1, timedOut false and HasExited
true. Both restored full suites pass 9/9, exit 0. Receipts are under
`artifacts/test-ablation/concurrency-fixtures/receipts/new-{style}-parent-corrected-{fault}/`;
`matrix-parent.ps1` uses fresh immutable labels and refuses overwrite.

All twelve replayed fault copies were restored to accepted code via apply_patch.
Both corrected test/helper files have zero CSharp LSP errors. The repository
formatter gate passes, exit 0, `37.545 s`. Both current prompts now describe
per-input entered/release/actual-completion controls and prohibit arbitrary
sleeps/polling. No ordinary wording-pinning test was introduced.

The preceding initial-candidate counts remain historical execution evidence.
The correction, legal controls and fresh same-fault replay above are the evidence
for the final test contents. Complete repository gates remain the parent's final
aggregate acceptance, not a claim made by this bounded unit.

## Burden tradeoff

Parent follow-up updated both current concurrency prompts to describe the actual
entered/release controls and removed delay fields. The workflow seam and business
rules are unchanged. This prose correction requires no new wording-pinning test;
the current trusted C# fixtures and executable gates own behavior.

Two scheduling timer sites and all 5-60 ms fixture dependencies removed, nine independent tests retained. Added: two TCS per controlled input, actual-task completion observation, lock-protected start/completion history, explicit releases, bounded event waits and failure cleanup. Signal and test code complexity increases. No overall speedup or allocation reduction claimed from runner timings. Deadline timers bound failures, not completion scheduling.
