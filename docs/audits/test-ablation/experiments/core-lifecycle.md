# Core lifecycle test-ablation execution

## Authority and baseline

This bounded lane started at `141f9d04adecc35a3851c6ca1d6812d6aa9f32ba` on `ulw/test-ablation-core-lifecycle` in `Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-lifecycle`. Permanent edits are limited to the three named core test files and this report. The S1/S2 assessment in `test-ablation-g001/docs/audits/test-ablation/lanes/core-nz.md` was read alongside actual source bodies. No commits, remote writes, dependency changes, skipped tests or frozen-source changes are authorized.

SDK: 10.0.401; runner: xUnit v3 4.0.0, .NET 10.0.12. Old baseline command: `dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal && dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -class FunnySharp.Tests.ResultBoundaryTests -class FunnySharp.Tests.StableFunctionRuntimeContractTests -class FunnySharp.Tests.StateTransitionTests -failSkips -failWarns` from the lane root. Exit 0; build 0 warnings/errors; Total 64, Errors 0, Failed 0, Skipped 0, Not Run 0.

Two setup attempts did not run tests and are not fault evidence: a PowerShell-style conditional was rejected by the actual bash shell (exit 2), and `dotnet build --locked-mode` produced MSB1001 (exit 1). Correct builds use `-p:RestoreLockedMode=true`. LSP was requested before builds, but this host resolves the nested worktree without xUnit generated global usings/references and reports missing Fact/Assert; actual compiler builds are the authoritative diagnostics.

## Mechanisms

S1 awaits the already-converted Task directly with the existing 10-second watchdog. All six matrix rows, source subscription-before-release, zero reads while pending, no early second callback, callback counts, token equality and one GetResult per source remain.

S2 starts each cancellation producer with its own pre-created release Task, invokes both boundaries, checks both producers and returned operations are incomplete, then releases. Prepared OCE identity, original ThrowOriginalCancellation marker, exact token and IsCanceled are preserved. Producer and returned-operation waits are bounded. Only two ordinary null-task English Message pins are removed; exact InvalidOperationException and zero mapper calls remain.

State overlap holds exactly one evaluation at the second transition callback on a dedicated LongRunning task after one output has been accumulated. It observes entry before scheduling all remaining original ProcessorCount * 2 - 1 invocations, observes another entry and drains those invocations while the first is still held, releases in finally, and checks every state/output plus the full entry count. This does not require all workers to block and does not constrain the library's internal start order. The 256-transition chain, existing 10,000-leaf scale test, repeated invocation and composition immutability contracts are retained.

## Execution ledger

The candidate unmutated baseline also passed all 64 cases, exit 0, with 0 compiler warnings/errors.

Each temporary test file selects its original body under `#if ABLATION_OLD` and its candidate under `#else`. The same source fault is compiled old then new, each with RestoreLockedMode. The exact method filter and -failSkips -failWarns are used. Every source perturbation is restored immediately after its serial old/new run. No concurrent builds use this project. A failed wrapper installation caused one run to be killed and excluded before the corrected wrapper was installed; it is not rejection evidence.

## Exact perturbations

- `double-first` in `src/FunnySharp/FunctionExtensions.cs`: Save first ValueTask, await it twice before advancing; exact source GetResult assertion rejects two reads (later source subscription can time out after that fault).
- `double-second` in `src/FunnySharp/FunctionExtensions.cs`: Save second ValueTask and await it twice; exact second GetResult assertion rejects two reads.
- `early-second` in `src/FunnySharp/FunctionExtensions.cs`: Start second(default) immediately after obtaining first ValueTask, before awaiting first. Null input is valid in this matrix, so the pending-first callback count exposes early execution.
- `wrong-token` in `src/FunnySharp/FunctionExtensions.cs`: Pass CancellationToken.None to the cancellation-aware ValueTask callbacks; plain routes are unchanged.
- `never-completes` in `src/FunnySharp/FunctionExtensions.cs`: Consume both source stages, then await an unreleased TaskCompletionSource. The matrix watchdog must reject it.
- `outer-canceled` in `src/FunnySharp/FunctionExtensions.cs`: Consume both sources, then return a canceled outer operation instead of successful null.
- `pending-faulted` in `src/FunnySharp/Result.cs`: Only the pending TransformTask continuation completes canceled source operations with TrySetException(original OCE); already-completed branch is untouched.
- `pending-reconstructed` in `src/FunnySharp/Result.cs`: Only the pending continuation rebuilds a new OCE with the original token and transfers its canceled task; identity is lost.
- `stack-loss` in `src/FunnySharp/Result.cs`: CreateCanceledTask directly throws the original cancellation instead of ExceptionDispatchInfo.Throw. Identity/token/status survive, but ThrowOriginalCancellation disappears.
- `shared-output-count` in `src/FunnySharp/StateTransition.cs`: Move outputCount from per-evaluation local to one Composition instance field; resets and accumulated output counts can corrupt overlapping invocations.
- `missing-repeated-outputs` in `src/FunnySharp/StateTransition.cs`: Remember that one evaluation emitted outputs and suppress materialized outputs on subsequent calls to the same Composition.
- `wrong-unrelated-token` in `src/FunnySharp/FunctionExtensions.cs`: Pass an unrelated already-canceled token to cancellation-aware ValueTask callbacks; plain routes remain unchanged.
- `wrong-none-token-after-explicit-token` in `src/FunnySharp/FunctionExtensions.cs`: Repeat the None substitution after candidate selects an explicit non-default token; old still uses the runner token.
- `pending-faulted-precompleted-control` in `src/FunnySharp/Result.cs`: Install the same pending-only TrySetException source mutant while old copied test awaits both source Tasks before boundary invocation; candidate remains explicitly held.

## Valid-change and scheduling controls

The three valid internal controls introduce a local for the second awaited function result, change Result continuation scheduling from ExecuteSynchronously to None, and change State pool rentals from 8 to 16. Both old and new must pass. The null-task wording control changes only ordinary exception English text; old is expected to reject its prose pin while candidate still verifies exact InvalidOperationException and mapperCalls 0.

The precompleted scheduling control awaits both old copied source Tasks and asserts IsCanceled before boundary invocation. It leaves all original boundary identity/token/stack/status assertions intact. The same pending-only fault remains installed; a green old result demonstrates the Task.Yield producer can miss that branch, while candidate release gates cannot.

### double-first

Executed command (lane root; serial old/new):

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-lifecycle; echo EXPERIMENT double-first OLD; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal -p:DefineConstants=ABLATION_OLD; b=$?; echo BUILD_OLD_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StableFunctionRuntimeContractTests.ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion' -failSkips -failWarns; echo TEST_OLD_EXIT=$?; echo EXPERIMENT double-first NEW; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal; b=$?; echo BUILD_NEW_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StableFunctionRuntimeContractTests.ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion' -failSkips -failWarns; echo TEST_NEW_EXIT=$?
```

Observed output excerpts:

```text
    0 Warning(s)
    0 Error(s)
BUILD_OLD_EXIT=0
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   2
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   2
      System.TimeoutException : The operation has timed out.
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   2
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   2
      System.TimeoutException : The operation has timed out.
   FunnySharp.Tests  Total: 6, Errors: 0, Failed: 6, Skipped: 0, Not Run: 0, Time: 20.331s
TEST_OLD_EXIT=1
    0 Warning(s)
    0 Error(s)
BUILD_NEW_EXIT=0
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   2
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   2
      System.TimeoutException : The operation has timed out.
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   2
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   2
      System.TimeoutException : The operation has timed out.
   FunnySharp.Tests  Total: 6, Errors: 0, Failed: 6, Skipped: 0, Not Run: 0, Time: 20.433s
TEST_NEW_EXIT=1
```

### double-second

Executed command (lane root; serial old/new):

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-lifecycle; echo EXPERIMENT double-second OLD; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal -p:DefineConstants=ABLATION_OLD; b=$?; echo BUILD_OLD_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StableFunctionRuntimeContractTests.ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion' -failSkips -failWarns; echo TEST_OLD_EXIT=$?; echo EXPERIMENT double-second NEW; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal; b=$?; echo BUILD_NEW_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StableFunctionRuntimeContractTests.ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion' -failSkips -failWarns; echo TEST_NEW_EXIT=$?
```

Observed output excerpts:

```text
    0 Warning(s)
    0 Error(s)
BUILD_OLD_EXIT=0
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   2
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   2
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   2
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   2
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   2
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   2
   FunnySharp.Tests  Total: 6, Errors: 0, Failed: 6, Skipped: 0, Not Run: 0, Time: 0.283s
TEST_OLD_EXIT=1
    0 Warning(s)
    0 Error(s)
BUILD_NEW_EXIT=0
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   2
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   2
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   2
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   2
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   2
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   2
   FunnySharp.Tests  Total: 6, Errors: 0, Failed: 6, Skipped: 0, Not Run: 0, Time: 0.271s
TEST_NEW_EXIT=1
```

### early-second

Executed command (lane root; serial old/new):

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-lifecycle; echo EXPERIMENT early-second OLD; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal -p:DefineConstants=ABLATION_OLD; b=$?; echo BUILD_OLD_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StableFunctionRuntimeContractTests.ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion' -failSkips -failWarns; echo TEST_OLD_EXIT=$?; echo EXPERIMENT early-second NEW; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal; b=$?; echo BUILD_NEW_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StableFunctionRuntimeContractTests.ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion' -failSkips -failWarns; echo TEST_NEW_EXIT=$?
```

Observed output excerpts:

```text
    0 Warning(s)
    0 Error(s)
BUILD_OLD_EXIT=0
      Assert.Equal() Failure: Values differ
      Expected: 0
      Actual:   1
      Assert.Equal() Failure: Values differ
      Expected: 0
      Actual:   1
   FunnySharp.Tests  Total: 6, Errors: 0, Failed: 2, Skipped: 0, Not Run: 0, Time: 0.288s
TEST_OLD_EXIT=1
    0 Warning(s)
    0 Error(s)
BUILD_NEW_EXIT=0
      Assert.Equal() Failure: Values differ
      Expected: 0
      Actual:   1
      Assert.Equal() Failure: Values differ
      Expected: 0
      Actual:   1
   FunnySharp.Tests  Total: 6, Errors: 0, Failed: 2, Skipped: 0, Not Run: 0, Time: 0.304s
TEST_NEW_EXIT=1
```

### never-completes

Executed command (lane root; serial old/new):

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-lifecycle; echo EXPERIMENT never-completes OLD; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal -p:DefineConstants=ABLATION_OLD; b=$?; echo BUILD_OLD_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StableFunctionRuntimeContractTests.ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion' -failSkips -failWarns; echo TEST_OLD_EXIT=$?; echo EXPERIMENT never-completes NEW; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal; b=$?; echo BUILD_NEW_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StableFunctionRuntimeContractTests.ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion' -failSkips -failWarns; echo TEST_NEW_EXIT=$?
```

Observed output excerpts:

```text
    0 Warning(s)
    0 Error(s)
BUILD_OLD_EXIT=0
      System.TimeoutException : The operation has timed out.
      System.TimeoutException : The operation has timed out.
      System.TimeoutException : The operation has timed out.
      System.TimeoutException : The operation has timed out.
      System.TimeoutException : The operation has timed out.
   FunnySharp.Tests  Total: 6, Errors: 0, Failed: 6, Skipped: 0, Not Run: 0, Time: 60.361s
TEST_OLD_EXIT=1
    0 Warning(s)
    0 Error(s)
BUILD_NEW_EXIT=0
      System.TimeoutException : The operation has timed out.
      System.TimeoutException : The operation has timed out.
      System.TimeoutException : The operation has timed out.
      System.TimeoutException : The operation has timed out.
   FunnySharp.Tests  Total: 6, Errors: 0, Failed: 6, Skipped: 0, Not Run: 0, Time: 60.318s
TEST_NEW_EXIT=1
```

### outer-canceled

Executed command (lane root; serial old/new):

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-lifecycle; echo EXPERIMENT outer-canceled OLD; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal -p:DefineConstants=ABLATION_OLD; b=$?; echo BUILD_OLD_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StableFunctionRuntimeContractTests.ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion' -failSkips -failWarns; echo TEST_OLD_EXIT=$?; echo EXPERIMENT outer-canceled NEW; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal; b=$?; echo BUILD_NEW_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StableFunctionRuntimeContractTests.ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion' -failSkips -failWarns; echo TEST_NEW_EXIT=$?
```

Observed output excerpts:

```text
    0 Warning(s)
    0 Error(s)
BUILD_OLD_EXIT=0
      System.Threading.Tasks.TaskCanceledException : A task was canceled.
      System.Threading.Tasks.TaskCanceledException : A task was canceled.
      System.Threading.Tasks.TaskCanceledException : A task was canceled.
      System.Threading.Tasks.TaskCanceledException : A task was canceled.
      System.Threading.Tasks.TaskCanceledException : A task was canceled.
      System.Threading.Tasks.TaskCanceledException : A task was canceled.
   FunnySharp.Tests  Total: 6, Errors: 0, Failed: 6, Skipped: 0, Not Run: 0, Time: 0.371s
TEST_OLD_EXIT=1
    0 Warning(s)
    0 Error(s)
BUILD_NEW_EXIT=0
      System.Threading.Tasks.TaskCanceledException : A task was canceled.
      System.Threading.Tasks.TaskCanceledException : A task was canceled.
      System.Threading.Tasks.TaskCanceledException : A task was canceled.
      System.Threading.Tasks.TaskCanceledException : A task was canceled.
      System.Threading.Tasks.TaskCanceledException : A task was canceled.
      System.Threading.Tasks.TaskCanceledException : A task was canceled.
   FunnySharp.Tests  Total: 6, Errors: 0, Failed: 6, Skipped: 0, Not Run: 0, Time: 0.399s
TEST_NEW_EXIT=1
```

### wrong-unrelated-token

Executed command (lane root; serial old/new):

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-lifecycle; echo EXPERIMENT wrong-unrelated-token OLD; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal -p:DefineConstants=ABLATION_OLD; b=$?; echo BUILD_OLD_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StableFunctionRuntimeContractTests.ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion' -failSkips -failWarns; echo TEST_OLD_EXIT=$?; echo EXPERIMENT wrong-unrelated-token NEW; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal; b=$?; echo BUILD_NEW_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StableFunctionRuntimeContractTests.ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion' -failSkips -failWarns; echo TEST_NEW_EXIT=$?
```

Observed output excerpts:

```text
    0 Warning(s)
    0 Error(s)
BUILD_OLD_EXIT=0
      Assert.Equal() Failure: Values differ
      Expected: System.Threading.CancellationToken
      Actual:   System.Threading.CancellationToken
      System.TimeoutException : The operation has timed out.
      System.TimeoutException : The operation has timed out.
   FunnySharp.Tests  Total: 6, Errors: 0, Failed: 3, Skipped: 0, Not Run: 0, Time: 20.402s
TEST_OLD_EXIT=1
    0 Warning(s)
    0 Error(s)
BUILD_NEW_EXIT=0
      Assert.Equal() Failure: Values differ
      Expected: System.Threading.CancellationToken
      Actual:   System.Threading.CancellationToken
      System.TimeoutException : The operation has timed out.
      System.TimeoutException : The operation has timed out.
   FunnySharp.Tests  Total: 6, Errors: 0, Failed: 3, Skipped: 0, Not Run: 0, Time: 20.414s
TEST_NEW_EXIT=1
```

### wrong-none-token-after-explicit-token

Executed command (lane root; serial old/new):

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-lifecycle; echo EXPERIMENT wrong-none-token-after-explicit-token OLD; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal -p:DefineConstants=ABLATION_OLD; b=$?; echo BUILD_OLD_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StableFunctionRuntimeContractTests.ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion' -failSkips -failWarns; echo TEST_OLD_EXIT=$?; echo EXPERIMENT wrong-none-token-after-explicit-token NEW; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal; b=$?; echo BUILD_NEW_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StableFunctionRuntimeContractTests.ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion' -failSkips -failWarns; echo TEST_NEW_EXIT=$?
```

Observed output excerpts:

```text
    0 Warning(s)
    0 Error(s)
BUILD_OLD_EXIT=0
      Assert.Equal() Failure: Values differ
      Expected: System.Threading.CancellationToken
      Actual:   System.Threading.CancellationToken
      System.TimeoutException : The operation has timed out.
      System.TimeoutException : The operation has timed out.
   FunnySharp.Tests  Total: 6, Errors: 0, Failed: 3, Skipped: 0, Not Run: 0, Time: 20.300s
TEST_OLD_EXIT=1
    0 Warning(s)
    0 Error(s)
BUILD_NEW_EXIT=0
      Assert.Equal() Failure: Values differ
      Expected: System.Threading.CancellationToken
      Actual:   System.Threading.CancellationToken
      System.TimeoutException : The operation has timed out.
      System.TimeoutException : The operation has timed out.
   FunnySharp.Tests  Total: 6, Errors: 0, Failed: 3, Skipped: 0, Not Run: 0, Time: 20.335s
TEST_NEW_EXIT=1
```

### pending-faulted

Executed command (lane root; serial old/new):

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-lifecycle; echo EXPERIMENT pending-faulted OLD; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal -p:DefineConstants=ABLATION_OLD; b=$?; echo BUILD_OLD_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.ResultBoundaryTests.PendingCanceledSourcesPreserveIdentityStackTokenAndStatus' -failSkips -failWarns; echo TEST_OLD_EXIT=$?; echo EXPERIMENT pending-faulted NEW; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal; b=$?; echo BUILD_NEW_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.ResultBoundaryTests.PendingCanceledSourcesPreserveIdentityStackTokenAndStatus' -failSkips -failWarns; echo TEST_NEW_EXIT=$?
```

Observed output excerpts:

```text
    0 Warning(s)
    0 Error(s)
BUILD_OLD_EXIT=0
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.321s
TEST_OLD_EXIT=0
    0 Warning(s)
    0 Error(s)
BUILD_NEW_EXIT=0
      Expected: True
      Actual:   False
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.401s
TEST_NEW_EXIT=1
```

### pending-faulted-precompleted-control

Executed command (lane root; serial old/new):

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-lifecycle; echo EXPERIMENT pending-faulted-precompleted-control OLD; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal -p:DefineConstants=ABLATION_OLD; b=$?; echo BUILD_OLD_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.ResultBoundaryTests.PendingCanceledSourcesPreserveIdentityStackTokenAndStatus' -failSkips -failWarns; echo TEST_OLD_EXIT=$?; echo EXPERIMENT pending-faulted-precompleted-control NEW; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal; b=$?; echo BUILD_NEW_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.ResultBoundaryTests.PendingCanceledSourcesPreserveIdentityStackTokenAndStatus' -failSkips -failWarns; echo TEST_NEW_EXIT=$?
```

Observed output excerpts:

```text
    0 Warning(s)
    0 Error(s)
BUILD_OLD_EXIT=0
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.228s
TEST_OLD_EXIT=0
    0 Warning(s)
    0 Error(s)
BUILD_NEW_EXIT=0
      Expected: True
      Actual:   False
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.232s
TEST_NEW_EXIT=1
```

### pending-reconstructed

Executed command (lane root; serial old/new):

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-lifecycle; echo EXPERIMENT pending-reconstructed OLD; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal -p:DefineConstants=ABLATION_OLD; b=$?; echo BUILD_OLD_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.ResultBoundaryTests.PendingCanceledSourcesPreserveIdentityStackTokenAndStatus' -failSkips -failWarns; echo TEST_OLD_EXIT=$?; echo EXPERIMENT pending-reconstructed NEW; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal; b=$?; echo BUILD_NEW_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.ResultBoundaryTests.PendingCanceledSourcesPreserveIdentityStackTokenAndStatus' -failSkips -failWarns; echo TEST_NEW_EXIT=$?
```

Observed output excerpts:

```text
    0 Warning(s)
    0 Error(s)
BUILD_OLD_EXIT=0
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.279s
TEST_OLD_EXIT=0
    0 Warning(s)
    0 Error(s)
BUILD_NEW_EXIT=0
      Assert.Same() Failure: Values are not the same instance
      Expected: System.OperationCanceledException: prepared cancellation
      Actual:   System.OperationCanceledException: The operation was canceled.
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.309s
TEST_NEW_EXIT=1
```

### stack-loss

Executed command (lane root; serial old/new):

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-lifecycle; echo EXPERIMENT stack-loss OLD; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal -p:DefineConstants=ABLATION_OLD; b=$?; echo BUILD_OLD_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.ResultBoundaryTests.PendingCanceledSourcesPreserveIdentityStackTokenAndStatus' -failSkips -failWarns; echo TEST_OLD_EXIT=$?; echo EXPERIMENT stack-loss NEW; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal; b=$?; echo BUILD_NEW_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.ResultBoundaryTests.PendingCanceledSourcesPreserveIdentityStackTokenAndStatus' -failSkips -failWarns; echo TEST_NEW_EXIT=$?
```

Observed output excerpts:

```text
    0 Warning(s)
    0 Error(s)
BUILD_OLD_EXIT=0
      Assert.Contains() Failure: Sub-string not found
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.330s
TEST_OLD_EXIT=1
    0 Warning(s)
    0 Error(s)
BUILD_NEW_EXIT=0
      Assert.Contains() Failure: Sub-string not found
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.275s
TEST_NEW_EXIT=1
```

### shared-output-count

Executed command (lane root; serial old/new):

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-lifecycle; echo EXPERIMENT shared-output-count OLD; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal -p:DefineConstants=ABLATION_OLD; b=$?; echo BUILD_OLD_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StateTransitionTests.ThenCompositionSupportsConcurrentInvocation' -failSkips -failWarns; echo TEST_OLD_EXIT=$?; echo EXPERIMENT shared-output-count NEW; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal; b=$?; echo BUILD_NEW_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StateTransitionTests.ThenCompositionSupportsConcurrentInvocation' -failSkips -failWarns; echo TEST_NEW_EXIT=$?
```

Observed output excerpts:

```text
    0 Warning(s)
    0 Error(s)
BUILD_OLD_EXIT=0
      System.ArgumentException : Destination array was not long enough. Check the destination index, length, and the array's lower bounds. (Parameter 'destinationArray')
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.264s
TEST_OLD_EXIT=1
    0 Warning(s)
    0 Error(s)
BUILD_NEW_EXIT=0
      System.ArgumentException : Destination array was not long enough. Check the destination index, length, and the array's lower bounds. (Parameter 'destinationArray')
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.231s
TEST_NEW_EXIT=1
```

### missing-repeated-outputs

Executed command (lane root; serial old/new):

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-lifecycle; echo EXPERIMENT missing-repeated-outputs OLD; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal -p:DefineConstants=ABLATION_OLD; b=$?; echo BUILD_OLD_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StateTransitionTests.ThenCompositionSupportsConcurrentInvocation' -failSkips -failWarns; echo TEST_OLD_EXIT=$?; echo EXPERIMENT missing-repeated-outputs NEW; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal; b=$?; echo BUILD_NEW_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StateTransitionTests.ThenCompositionSupportsConcurrentInvocation' -failSkips -failWarns; echo TEST_NEW_EXIT=$?
```

Observed output excerpts:

```text
    0 Warning(s)
    0 Error(s)
BUILD_OLD_EXIT=0
      Assert.All() Failure: 32 out of 32 items in the collection did not pass.
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
            Error: Assert.Equal() Failure: Collections differ
                   Expected: [1, 2, 3, 4, 5, ···]
                   Actual:   []
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.267s
TEST_OLD_EXIT=1
    0 Warning(s)
    0 Error(s)
BUILD_NEW_EXIT=0
      Assert.Equal() Failure: Collections differ
      Expected: [1, 2, 3, 4, 5, ···]
      Actual:   []
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.267s
TEST_NEW_EXIT=1
```

### valid-function-local

Executed command (lane root; serial old/new):

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-lifecycle; echo EXPERIMENT valid-function-local OLD; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal -p:DefineConstants=ABLATION_OLD; b=$?; echo BUILD_OLD_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StableFunctionRuntimeContractTests.ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion' -failSkips -failWarns; echo TEST_OLD_EXIT=$?; echo EXPERIMENT valid-function-local NEW; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal; b=$?; echo BUILD_NEW_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StableFunctionRuntimeContractTests.ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion' -failSkips -failWarns; echo TEST_NEW_EXIT=$?
```

Observed output excerpts:

```text
    0 Warning(s)
    0 Error(s)
BUILD_OLD_EXIT=0
   FunnySharp.Tests  Total: 6, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.248s
TEST_OLD_EXIT=0
    0 Warning(s)
    0 Error(s)
BUILD_NEW_EXIT=0
   FunnySharp.Tests  Total: 6, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.240s
TEST_NEW_EXIT=0
```

### valid-continuation-scheduling

Executed command (lane root; serial old/new):

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-lifecycle; echo EXPERIMENT valid-continuation-scheduling OLD; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal -p:DefineConstants=ABLATION_OLD; b=$?; echo BUILD_OLD_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.ResultBoundaryTests.PendingCanceledSourcesPreserveIdentityStackTokenAndStatus' -failSkips -failWarns; echo TEST_OLD_EXIT=$?; echo EXPERIMENT valid-continuation-scheduling NEW; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal; b=$?; echo BUILD_NEW_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.ResultBoundaryTests.PendingCanceledSourcesPreserveIdentityStackTokenAndStatus' -failSkips -failWarns; echo TEST_NEW_EXIT=$?
```

Observed output excerpts:

```text
    0 Warning(s)
    0 Error(s)
BUILD_OLD_EXIT=0
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.280s
TEST_OLD_EXIT=0
    0 Warning(s)
    0 Error(s)
BUILD_NEW_EXIT=0
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.300s
TEST_NEW_EXIT=0
```

### valid-scratch-capacity

Executed command (lane root; serial old/new):

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-lifecycle; echo EXPERIMENT valid-scratch-capacity OLD; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal -p:DefineConstants=ABLATION_OLD; b=$?; echo BUILD_OLD_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StateTransitionTests.ThenCompositionSupportsConcurrentInvocation' -failSkips -failWarns; echo TEST_OLD_EXIT=$?; echo EXPERIMENT valid-scratch-capacity NEW; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal; b=$?; echo BUILD_NEW_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StateTransitionTests.ThenCompositionSupportsConcurrentInvocation' -failSkips -failWarns; echo TEST_NEW_EXIT=$?
```

Observed output excerpts:

```text
    0 Warning(s)
    0 Error(s)
BUILD_OLD_EXIT=0
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.272s
TEST_OLD_EXIT=0
    0 Warning(s)
    0 Error(s)
BUILD_NEW_EXIT=0
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.237s
TEST_NEW_EXIT=0
```

### valid-null-task-wording

Executed command (lane root; serial old/new):

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-lifecycle; echo EXPERIMENT valid-null-task-wording OLD; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal -p:DefineConstants=ABLATION_OLD; b=$?; echo BUILD_OLD_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.ResultBoundaryTests.TryAsyncRejectsNullTasksWithoutMappingThem' -failSkips -failWarns; echo TEST_OLD_EXIT=$?; echo EXPERIMENT valid-null-task-wording NEW; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal; b=$?; echo BUILD_NEW_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.ResultBoundaryTests.TryAsyncRejectsNullTasksWithoutMappingThem' -failSkips -failWarns; echo TEST_NEW_EXIT=$?
```

Observed output excerpts:

```text
    0 Warning(s)
    0 Error(s)
BUILD_OLD_EXIT=0
      Assert.Equal() Failure: Strings differ
      Expected: "The operation returned a null task."
      Actual:   "The boundary received a null task."
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.240s
TEST_OLD_EXIT=1
    0 Warning(s)
    0 Error(s)
BUILD_NEW_EXIT=0
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.223s
TEST_NEW_EXIT=0
```

## Corrected token experiment installation

Three initially green token runs are excluded from meaningful fault evidence: the minimal patch context matched the Task-returning ComposeAsync overload instead of the intended ValueTask overload. The actual file was inspected after the unexpected green results, and this mismatch was observed directly. Therefore the earlier hypothesis about a None runner token was not established by those runs and is withdrawn. Source was re-read and restored, and subsequent patches replace the full exact captured file to avoid overload ambiguity. The candidate uses an explicit uncanceled CancellationTokenSource for the value-composition matrix; this makes the intended token contract independent of the runner. The corrected same-source token experiments follow below.

The shell envelope intentionally prints both runner exit codes and finishes with echo (envelope exit 0); a mutant rejection is the TEST_OLD_EXIT/TEST_NEW_EXIT value 1 with nonzero Failed count after BUILD_*_EXIT 0, not the envelope code. All completed runs report zero skips, not-run cases and runner errors. The never-completes test executes all six rows per version: 6/6 watchdog rejections old (60.361s) and new (60.318s). These deadlines are existing bounded liveness oracles, not guessed scheduling waits.

`outer-canceled` was rejected 6/6 by both versions after both stage sources were consumed. Awaiting returned directly does not swallow cancellation; candidate and old each return runner exit 1 after successful builds.

The same audit also found that the prior short test-token patch selected the old copied branch. One additional in-flight token run was canceled and excluded. All three actual temporary test files were read; Result and State matched their intended branch contents, and StableFunction was restored to exact original old body plus final candidate body using a full-file patch. Subsequent source/test mutations use full-file replacement only.

## Excluded installation runs

The following commands were actually launched but do not establish detector evidence because their installed overload or copied branch was wrong. They are retained as execution history rather than counted as fault rejections.

- `wrong-token` (`bash_11`): Builds 0; both runners 0 and 6/6 green, on the wrong target; excluded.

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-lifecycle; echo EXPERIMENT wrong-token OLD; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal -p:DefineConstants=ABLATION_OLD; b=$?; echo BUILD_OLD_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StableFunctionRuntimeContractTests.ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion' -failSkips -failWarns; echo TEST_OLD_EXIT=$?; echo EXPERIMENT wrong-token NEW; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal; b=$?; echo BUILD_NEW_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StableFunctionRuntimeContractTests.ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion' -failSkips -failWarns; echo TEST_NEW_EXIT=$?
```

- `wrong-unrelated-token` (`bash_14`): Builds 0; both runners 0 and 6/6 green, on the wrong target; excluded.

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-lifecycle; echo EXPERIMENT wrong-unrelated-token OLD; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal -p:DefineConstants=ABLATION_OLD; b=$?; echo BUILD_OLD_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StableFunctionRuntimeContractTests.ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion' -failSkips -failWarns; echo TEST_OLD_EXIT=$?; echo EXPERIMENT wrong-unrelated-token NEW; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal; b=$?; echo BUILD_NEW_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StableFunctionRuntimeContractTests.ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion' -failSkips -failWarns; echo TEST_NEW_EXIT=$?
```

- `wrong-none-token-after-explicit-token` (`bash_15`): Builds 0; both runners 0 and 6/6 green, on the wrong target; excluded.

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-lifecycle; echo EXPERIMENT wrong-none-token-after-explicit-token OLD; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal -p:DefineConstants=ABLATION_OLD; b=$?; echo BUILD_OLD_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StableFunctionRuntimeContractTests.ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion' -failSkips -failWarns; echo TEST_OLD_EXIT=$?; echo EXPERIMENT wrong-none-token-after-explicit-token NEW; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal; b=$?; echo BUILD_NEW_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StableFunctionRuntimeContractTests.ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion' -failSkips -failWarns; echo TEST_NEW_EXIT=$?
```

- `wrong-unrelated-token` (`bash_16`): Canceled; no rejection count.

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-lifecycle; echo EXPERIMENT wrong-unrelated-token OLD; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal -p:DefineConstants=ABLATION_OLD; b=$?; echo BUILD_OLD_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StableFunctionRuntimeContractTests.ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion' -failSkips -failWarns; echo TEST_OLD_EXIT=$?; echo EXPERIMENT wrong-unrelated-token NEW; dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal; b=$?; echo BUILD_NEW_EXIT=$b; if [ $b -ne 0 ]; then exit $b; fi; dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -method 'FunnySharp.Tests.StableFunctionRuntimeContractTests.ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion' -failSkips -failWarns; echo TEST_NEW_EXIT=$?
```

The correctly installed `wrong-none-token-after-explicit-token` fault was rejected 3/6 by BOTH versions (runner exit 1; build exit 0). Thus the original runner token was distinguishable from None in the actual valid run; the earlier default-token hypothesis is conclusively unsupported. Only token-aware routes fail; all three plain routes pass. The final candidate uses a source-owned token to make that distinguishability explicit.

`pending-faulted` completed with old 1/1 green (runner 0), candidate 1/1 rejected (runner 1), both builds 0 with no warnings/errors. The mutant changes only deferred continuation completion to Faulted, leaving already-completed cancellation untouched. Therefore the old run did not establish the promised pending path; this is an observed false-green, not an old detector guarantee. Candidate held both producers and both returned operations before release and rejected IsCanceled loss. The explicit precompleted control is now executed against the same mutant.

The explicit precompleted-source control passed old 1/1 and rejected candidate 1/1 against the SAME pending-only status fault. The old copied test awaited both source Tasks before boundary invocation and asserted both were canceled, while all original boundary identity/token/stack/status assertions remained intact. This confirms the scheduling distinction without a guessed wait. Both temporary test control and source mutant were restored after the run.

`pending-reconstructed` also passed the uncontrolled old method (1/1, runner 0) while the held candidate rejected it (1/1, runner 1). The candidate failure is Assert.Same: new OCE construction preserves the token and canceled status but loses the prepared exception identity. This is recorded as another missed pending branch in the old execution, not as an old identity oracle regression.

`stack-loss` was rejected 1/1 by old and candidate after successful builds. The exception is still the same instance, retains its exact token and is canceled, but direct throw resets its prepared stack; Assert.Contains(ThrowOriginalCancellation, StackTrace) detects the missing origin. No cancellation Message text is pinned.

`shared-output-count` was rejected 1/1 by both executed versions. The old Task.Run workload happened to overlap in this run, so it caught shared scratch state, but it still has no overlap guarantee. The candidate observes a held callback and a second entry before completing the original remaining invocation workload, so serial Task.Run scheduling cannot erase that overlap. This distinction is structural signal evidence, not a claim based on elapsed time or thread count.

Actual workload probe (exit 0): `printf '%s\n' 'printfn "CORE_PROCESSOR_COUNT=%d" System.Environment.ProcessorCount;;' '#quit;;' | dotnet fsi --readline-` printed `CORE_PROCESSOR_COUNT=16`. The State concurrent method therefore retains exactly 32 evaluated invocations: one held callback plus 31 original remaining invocations, each a 256-transition chain, in addition to its unchanged expected-result invocation. No workload was shrunk and only one callback blocks.

The legal function-local control passed all six rows in old and candidate (both runner exit 0; both builds 0, no warnings/errors). It changes no public behavior and demonstrates the single-consumption detector tolerates harmless result-local structure.

The legal Result continuation-scheduling control passed 1/1 in old and candidate (both build and runner exits 0). Therefore the held cancellation producers and identity/token/stack/status oracles do not depend on ExecuteSynchronously or a particular internal continuation start order.

## Final restoration and verification receipt

All three temporarily mutated source files were read and compared exactly to their captured pre-mutation text: equal. All conditional old/new test wrappers and the precompleted scheduling test-only control were removed. The final candidate keeps 15 ResultBoundary cases, 30 StableFunctionRuntimeContract matrix cases (including all six value-composition rows), and 19 StateTransition cases. No helper fixture/framework file was added.

Fresh final LSP diagnostics timed out at 3000ms per test file; earlier host LSP probes could not resolve nested-worktree xUnit references/global usings. Final compiler diagnostics are clean. The following command was executed ONCE after restoration:

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-lifecycle && dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -p:RestoreLockedMode=true -v minimal && dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -class FunnySharp.Tests.ResultBoundaryTests -class FunnySharp.Tests.StableFunctionRuntimeContractTests -class FunnySharp.Tests.StateTransitionTests -failSkips -failWarns && dotnet tests/FunnySharp.Tests/bin/Debug/net10.0/FunnySharp.Tests.dll -failSkips -failWarns && dotnet format whitespace tests/FunnySharp.Tests/FunnySharp.Tests.csproj --verify-no-changes --no-restore --include tests/FunnySharp.Tests/ResultBoundaryTests.cs tests/FunnySharp.Tests/StableFunctionRuntimeContractTests.cs tests/FunnySharp.Tests/StateTransitionTests.cs && git diff --check && git diff --exit-code -- src/FunnySharp && git status --short
```

Observed final output:

```text
status: completed exit_code: 0
  Determining projects to restore...
  All projects are up-to-date for restore.
  FunnySharp.Analyzers -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-core-lifecycle\src\FunnySharp.Analyzers\bin\Debug\netstandard2.0\FunnySharp.Analyzers.dll
  FunnySharp.Analyzers.CodeFixes -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-core-lifecycle\src\FunnySharp.Analyzers.CodeFixes\bin\Debug\netstandard2.0\FunnySharp.Analyzers.CodeFixes.dll
  FunnySharp -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-core-lifecycle\src\FunnySharp\bin\Debug\net10.0\FunnySharp.dll
  FunnySharp.Tests -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-core-lifecycle\tests\FunnySharp.Tests\bin\Debug\net10.0\FunnySharp.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:04.67
xUnit.net v3 In-Process Runner v4.0.0+8bf043c053 (64-bit .NET 10.0.12)
  Discovering: FunnySharp.Tests
  Discovered:  FunnySharp.Tests
  Starting:    FunnySharp.Tests
  Finished:    FunnySharp.Tests (ID = 'eeb18c8e6dbdc9237a1dab0f7c39cfcd7da86a9feef44ddc0f057bafda2208e1')
=== TEST EXECUTION SUMMARY ===
   FunnySharp.Tests  Total: 64, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.308s
xUnit.net v3 In-Process Runner v4.0.0+8bf043c053 (64-bit .NET 10.0.12)
  Discovering: FunnySharp.Tests
  Discovered:  FunnySharp.Tests
  Starting:    FunnySharp.Tests
  Finished:    FunnySharp.Tests (ID = 'eeb18c8e6dbdc9237a1dab0f7c39cfcd7da86a9feef44ddc0f057bafda2208e1')
=== TEST EXECUTION SUMMARY ===
   FunnySharp.Tests  Total: 1762, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.910s
 M tests/FunnySharp.Tests/ResultBoundaryTests.cs
 M tests/FunnySharp.Tests/StableFunctionRuntimeContractTests.cs
 M tests/FunnySharp.Tests/StateTransitionTests.cs
?? docs/audits/test-ablation/experiments/core-lifecycle.md
```

The complete chained command exited 0. Locked compiler build: 0 warnings, 0 errors. Relevant full classes: 64/64 pass. Entire core suite: 1762/1762 pass, zero errors, failures, skips or not-run cases. The scoped dotnet format whitespace --verify-no-changes check, git diff --check and git diff --exit-code -- src/FunnySharp all succeeded (the && chain reached git status, exit 0). Only the three authorized test files and this report remain changed. No product source, frozen evidence, lock file, dependency, commit or remote write remains changed. Parent integration and repository-wide final gates are outside this lane receipt.

Parent commit gate additionally ran the complete repository formatter,
`dotnet fsi build.fsx -- -p format`, on these final files: exit 0, 34.114 s.
