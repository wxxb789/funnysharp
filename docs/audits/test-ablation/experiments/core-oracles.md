# Core oracle ablation experiment

## Scope and contract decisions

Baseline: `141f9d04adecc35a3851c6ca1d6812d6aa9f32ba`. Worktree: `Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles`. Read the `core-am` and `core-nz` assessments, applicable rules, actual selected test bodies, shipping bodies and guides.

Exact default/uninitialized English messages are explicitly contractual in `docs/result.md:14-15`, `docs/unit-result.md:81-82`, and `docs/validation.md:12`. Those assertions remain intact in DefaultStateTests, ResultTests, UnitResultAsyncTests, UnitResultInteropTests, UnitResultTests, UnitResultTraversalTests and ValidationAsyncTests. The conditional instruction to remove only unpromised wording does not authorize changing these contracts. Caller-selected messages and diagnostic state strings remain data. ResultBoundaryTests was not edited.

The candidate removes the noncontractual concurrent MoveNext and null-Task wording checks, the invalid Option hash-distinctness assertion, and six direct empty BCL Throws-only contrasts. All six FunnySharp bridge None checks and three empty-state observations remain. AsyncBatchValidationComposesNestedErrorLocations independently calls ParsePostalCode(empty) to obtain the original payload; it does not derive its expectation through MapErrors, At or traversal. Its exact location, error cardinality and successful records remain asserted. TraversalContextTests retains its caller-selected payload literal. No test method or InlineData row is deleted.

## Build prerequisites

The initial `dotnet build ... --locked-mode` attempt exited 1 with `MSB1001: Unknown switch`; no test rejection is counted from it. Correct builds use `-p:RestoreLockedMode=true`.

Executed from the worktree:

```powershell
dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/old -p:UseSharedCompilation=false -nologo
dotnet build tests/FunnySharp.Tests/FunnySharp.Tests.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/new -p:UseSharedCompilation=false -nologo
```

Both initial kits built with exit 0, 0 warnings and 0 errors (19.99 s old, 10.65 s revised). LSP reports duplicate generated assembly attributes and duplicate CountingValueTaskSource symbols in this nested worktree; these are present before the patch and are not suppressed. Changed OptionTests, UnitResultBoundaryTests and ContainerBridgeTests have no LSP errors. Compiler builds are the independently executed authority for isolated kits.

## Deterministic concurrent probe cleanup

The original concurrent MoveNext probe releases its selector only after its exception assertions. A wording/type/guard mutant can fail before release; await-using disposal then drains a permanently held selector and hides the failed assertion behind an unbounded cleanup wait. Both compared assertion sets therefore receive the same `try/finally` selector release before their kits are rebuilt. The old kit keeps every old assertion, including the English message pin; the revised kit removes only that pin. The final patch ships this failure-safe fixture cleanup. It does not weaken exact exception type, subscribed selector-start ordering, or the first-move success observation. This necessary harness correction is explicitly distinguished from the oracle ablation. Both theory rows remain.

Each mutant is applied to one shipping file through apply_patch and checked with LSP before a locked Release compiler build. Only after exit 0 is its compiled FunnySharp.dll copied into the two independently built assertion kits. The old and revised native runner invocations execute serially. Source is restored through the inverse apply_patch before the next mutant. Copying generated binaries is artifact setup, not a shipping edit. No builds run concurrently against the same project, output or cache.

## Executed harness baseline and filters

Native `dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -?` exits 2 by design and reports xUnit v3 In-Process Runner 4.0.0 on .NET 10.0.12. It documents native `-method`, `-class`, repeated-method OR, cross-filter AND, `-failSkips`, and `-failWarns`; no guessed filter or test timeout flag is used.

Old fault-safe kit rebuild: exit 0, 0 warnings/errors, 12.38 s. Revised fault-safe kit rebuild: exit 0, 0 warnings/errors, 9.40 s. An intervening attempt used PowerShell syntax in the bash monitor and exited 2 at parse time (`syntax error near unexpected token {`); neither build nor tests started and no rejection is counted. Commands below use the actual bash shell.

Focused baseline runs OR the option, concurrent, nullTask, bridge and nested filters below. Each executed **7 cases, 0 errors, 0 failed, 0 skipped, 0 not run**, exit 0 (old 0.259 s, revised 0.251 s).

| Filter key | Exact native filter |
| --- | --- |
| option | `-method "FunnySharp.Tests.OptionTests.ToNullableBridgesValueOptionsWithoutChangingTheirEquality"` |
| concurrent | `-method "FunnySharp.Tests.ParallelAsyncEnumerableTests.RejectsConcurrentMoveNextCalls"` |
| nullTask | `-method "FunnySharp.Tests.UnitResultBoundaryTests.AsyncTryRejectsNullTasksWithoutMappingThem"` |
| bridge | `-method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow"` |
| nested | `-method "FunnySharp.Tests.AsyncCollectionTests.AsyncBatchValidationComposesNestedErrorLocations" -method "FunnySharp.Tests.TraversalContextTests.NestedLocatedTraverseRendersTheCanonicalCustomersAddressPostalCodePath"` |

Each matrix run executes this exact command pattern from the worktree, replacing `<filter>` with the row filter and never starting the next source mutation before this run finishes:

```bash
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?
printf "BUILD_EXIT=0\n"
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll <filter> -noLogo -noColor -failSkips -failWarns
printf "OLD_EXIT=%s\n" "$?"
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll <filter> -noLogo -noColor -failSkips -failWarns
printf "NEW_EXIT=%s\n" "$?"
```

Every accepted fault result requires the compiler receipt `Build succeeded`, `0 Warning(s)`, `0 Error(s)` and `BUILD_EXIT=0`. Expected test failures are recorded separately; the shell exits 0 after both runs to permit their observation, not to redefine the runner exits.

## Executed old/new matrix

Rows below record runtime totals and runner exit codes independently. Control rows are semantically legal changes. Every source mutation is restored through apply_patch immediately after its result is recorded.

| Experiment | Filter | Build exit | Old total/failed/exit | Revised total/failed/exit | Result |
| --- | --- | ---: | --- | --- | --- |
| option-collision-control | option | 0 | 1/1/1 | 1/0/0 | old rejects valid control; revised permits |
| option-presence-loss | option | 0 | 1/1/1 | 1/1/1 | both reject fault |
| option-nullable-loss | option | 0 | 1/1/1 | 1/1/1 | both reject fault |
| option-equality-loss | option | 0 | 1/1/1 | 1/1/1 | both reject fault |
| result-default-guard | `-method "FunnySharp.Tests.DefaultStateTests.DefaultResultThrowsForStateAccessAndReportsUninitializedText"` | 0 | 1/1/1 | 1/1/1 | both reject fault |
| validation-default-guard | `-method "FunnySharp.Tests.DefaultStateTests.DefaultValidationThrowsForStateAccessAndReportsUninitializedText"` | 0 | 1/1/1 | 1/1/1 | both reject fault |
| unit-default-guard | `-method "FunnySharp.Tests.UnitResultTests.DefaultUnitResultThrowsForEveryStateReadingMember" -method "FunnySharp.Tests.UnitResultAsyncTests.DefaultCarrierThrowsSynchronouslyForValidDelegates" -method "FunnySharp.Tests.UnitResultTraversalTests.SyncTraversalRejectsNullArgumentsEagerlyAndThrowsForDefaultElements" -method "FunnySharp.Tests.UnitResultTraversalTests.AsyncTraversalFaultsForDefaultElements"` | 0 | 4/4/1 | 4/4/1 | both reject fault |
| nulltask-message-control | nullTask | 0 | 1/1/1 | 1/0/0 | old rejects valid control; revised permits |
| nulltask-wrong-type | nullTask | 0 | 1/1/1 | 1/1/1 | both reject fault |
| nulltask-mapper-invoked | nullTask | 0 | 1/1/1 | 1/1/1 | both reject fault |
| concurrent-message-control | concurrent | 0 | 2/2/1 | 2/0/0 | old rejects valid control; revised permits |
| concurrent-wrong-type | concurrent | 0 | 2/2/1 | 2/2/1 | both reject fault |
| location-customer-loss | nested | 0 | 2/2/1 | 2/2/1 | both reject fault |
| location-index-shift | nested | 0 | 2/2/1 | 2/2/1 | both reject fault |
| payload-message-loss | nested | 0 | 2/1/1 | 2/1/1 | both reject fault |
| bridge-queue-dequeue-some-default | bridge | 0 | 1/1/1 | 1/1/1 | both reject fault |
| bridge-queue-dequeue-throw | bridge | 0 | 1/1/1 | 1/1/1 | both reject fault |
| bridge-queue-dequeue-mutate-empty | bridge | 0 | 1/1/1 | 1/1/1 | both reject fault |
| bridge-queue-peek-some-default | bridge | 0 | 1/1/1 | 1/1/1 | both reject fault |
| bridge-queue-peek-throw | bridge | 0 | 1/1/1 | 1/1/1 | both reject fault |
| bridge-queue-peek-mutate-empty | bridge | 0 | 1/1/1 | 1/1/1 | both reject fault |
| bridge-stack-pop-some-default | bridge | 0 | 1/1/1 | 1/1/1 | both reject fault |
| bridge-stack-pop-throw | bridge | 0 | 1/1/1 | 1/1/1 | both reject fault |
| bridge-stack-pop-mutate-empty | bridge | 0 | 1/1/1 | 1/1/1 | both reject fault |
| bridge-stack-peek-some-default | bridge | 0 | 1/1/1 | 1/1/1 | both reject fault |
| bridge-stack-peek-throw | bridge | 0 | 1/1/1 | 1/1/1 | both reject fault |
| bridge-stack-peek-mutate-empty | bridge | 0 | 1/1/1 | 1/1/1 | both reject fault |
| bridge-priority-dequeue-some-default | bridge | 0 | 1/1/1 | 1/1/1 | both reject fault |
| bridge-priority-dequeue-throw | bridge | 0 | 1/1/1 | 1/1/1 | both reject fault |
| bridge-priority-dequeue-mutate-empty | bridge | 0 | 1/1/1 | 1/1/1 | both reject fault |
| bridge-priority-peek-some-default | bridge | 0 | 1/1/1 | 1/1/1 | both reject fault |
| bridge-priority-peek-throw | bridge | 0 | 1/1/1 | 1/1/1 | both reject fault |
| bridge-priority-peek-mutate-empty | bridge | 0 | 1/1/1 | 1/1/1 | both reject fault |
| concurrent-guard-removed | concurrent | 0 | 2/2/1 | 2/2/1 | both reject fault |

## Reproducible production deltas

The first stack-pop-mutate-empty application matched a repeated empty-branch context in Queue.DequeueOrNone and failed compilation with CS1061 (`Queue<T>` has no `Push`), exit 1, 0 warnings, 1 error. No test result was counted. The actual file was inspected and restored; subsequent patch hunks include the method declaration and surrounding type-specific context. Existing accepted bridge witnesses were checked against their actual ContainerBridgeTests assertion call-site lines, not inferred from experiment names.

The first payload-message-loss compiler attempt failed with IL2090 because its temporary generic reflection access lacked a PublicProperties preservation requirement (exit 1, 0 warnings, 1 error). No runtime rejection is counted. That source was restored before retry. The isolated retry explicitly annotates the temporary MapErrors generic parameter with DynamicallyAccessedMembers(PublicProperties), declaring the BCL reflection requirement rather than suppressing diagnostics. Both the mutation and annotation are restored afterwards.

Every row changes only the named shipping member; no test assertion is changed during a production fault run. All original code is restored before applying the next row.

- `option-collision-control`: Option<T>.GetHashCode returns the existing None hash for Some<int>(7) only; every other payload/case keeps the original hash formula. Equality, presence and nullable conversion are unchanged. This isolated collision is legal under the documented equal-values/equal-hashes contract. No altered hash ships.
- `option-presence-loss`: the Option<T> constructor assigns `IsSome = false` rather than true. `option-nullable-loss`: ToNullable returns default(T?) even after a successful TryGetValue. `option-equality-loss`: typed Equals additionally requires `!IsSome`.
- `result-default-guard` and `validation-default-guard`: replace the original throw in EnsureInitialized with return. Their filters are respectively `-method "FunnySharp.Tests.DefaultStateTests.DefaultResultThrowsForStateAccessAndReportsUninitializedText"` and `-method "FunnySharp.Tests.DefaultStateTests.DefaultValidationThrowsForStateAccessAndReportsUninitializedText"`.
- `unit-default-guard`: replace the original throw in UnitResult<TError>.ThrowIfUninitialized with return. Exact filter: `-method "FunnySharp.Tests.UnitResultTests.DefaultUnitResultThrowsForEveryStateReadingMember" -method "FunnySharp.Tests.UnitResultAsyncTests.DefaultCarrierThrowsSynchronouslyForValidDelegates" -method "FunnySharp.Tests.UnitResultTraversalTests.SyncTraversalRejectsNullArgumentsEagerlyAndThrowsForDefaultElements" -method "FunnySharp.Tests.UnitResultTraversalTests.AsyncTraversalFaultsForDefaultElements"`. All four original methods are run.
- `nulltask-message-control`: change only Result.NullTaskMessage to `The asynchronous operation did not return a Task.`. `nulltask-wrong-type`: use ArgumentException instead of InvalidOperationException in the UnitResult.TryAsyncCore null branch. `nulltask-mapper-invoked`: call errorMapper once in that branch but retain the original InvalidOperationException and original message, isolating the mapper-count detector.
- `concurrent-message-control`: change only the concurrent MoveNext diagnostic to `An asynchronous move is already pending.`. `concurrent-wrong-type`: retain its message but return ArgumentException. `concurrent-guard-removed`: return completed false from the concurrent branch instead of its exception. Both delivery-order theory rows always run, with the same failure-safe finally release in both kits.
- `location-customer-loss`: Location.Property("customers") returns the receiver rather than appending its segment. `location-index-shift`: Location.At appends index+1.
- `payload-message-loss`: Validation.MapErrors invokes the original mapper, then the isolated mutant uses existing BCL reflection to overwrite the mapped record Message with `corrupted fixture payload`; Location is untouched. The independently parsed original fixture does not traverse MapErrors or At, so the revised expectation cannot reproduce this fault. Reflection is temporary mutation machinery only and does not ship.
- The 18 `bridge-<route>-<fault>` rows are six independent entry points (queue-dequeue, queue-peek, stack-pop, stack-peek, priority-dequeue, priority-peek), each with exactly one of three empty-path substitutions. `some-default` returns Some(default!) on empty rather than None; `throw` throws InvalidOperationException("Empty bridge fault"); `mutate-empty` first Enqueue(default!) / Push(default!) / priority Enqueue(default!,default!), then returns the original None. Each mutation changes only its named method. Nonempty paths and all other bridge entry points retain original code. Some(default) must fail the affected None observation, throws must fail the bridge call, and an empty mutation must fail a later presence/empty-state observation. None of those observations depends on the deleted direct BCL contrasts.

## Exact command and output receipts

## Bridge detector containment

All 18 separately compiled bridge mutants fail old and revised on retained FunnySharp observations. The six direct BCL Throws calls occur after all six bridge calls and all three empty-state checks, so they cannot cause these rejections. Each route was perturbed independently, and the observed stack traces were checked rather than relying on experiment names.

| Entry point | Some(default) / throw trace line (old / revised) | Wrong empty mutation trace line (old / revised) |
| --- | --- | --- |
| queue-dequeue | `ContainerBridgeTests.cs:150 / 148` | `ContainerBridgeTests.cs:151 / 151` |
| queue-peek | `ContainerBridgeTests.cs:151 / 151` | `ContainerBridgeTests.cs:156 / 156` |
| stack-pop | `ContainerBridgeTests.cs:152 / 152` | `ContainerBridgeTests.cs:153 / 153` |
| stack-peek | `ContainerBridgeTests.cs:153 / 153` | `ContainerBridgeTests.cs:157 / 157` |
| priority-dequeue | `ContainerBridgeTests.cs:154 / 154` | `ContainerBridgeTests.cs:155 / 155` |
| priority-peek | `ContainerBridgeTests.cs:155 / 155` | `ContainerBridgeTests.cs:158 / 158` |

The revised Release trace reports line 148 for the first two Queue.Dequeue mutants, while its source first None assertion remains line 150. That earlier sequence-point location is recorded as observed, not described as a different assertion. Queue.DequeueOrNone is the first product operation in the method: its Some(default) produces the first Assert.True failure, and its throw exits that call. Source call order identifies that detector without inferring a detector from the Release line number alone. Every other bridge witness reports the exact same retained assertion line in both kits.

Empty dequeue/pop mutations are caught by the following peek presence observation; empty peek mutations are caught by Queue/Stack Assert.Empty or priority Count 0. All six None observations and all three state observations remain unchanged. This supports removing the six nonproduct platform contrasts only, not deletion of any method or input partition.

Each receipt includes the actual monitor handle, source file, exact command executed, compiler success diagnostics, old/revised runner exits and decisive output lines. Stack frames are retained where they establish the bridge assertion call site; unrelated framework frames and discovery banners are omitted. The summary table above records every count.

### option-collision-control

Source: `src/FunnySharp/Option.cs`. Monitor: `mon_RSEJVGNTB18JN1DS`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.OptionTests.ToNullableBridgesValueOptionsWithoutChangingTheirEquality" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.OptionTests.ToNullableBridgesValueOptionsWithoutChangingTheirEquality" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.OptionTests.ToNullableBridgesValueOptionsWithoutChangingTheirEquality [FAIL]
      Assert.NotEqual() Failure: Values are equal
      Expected: Not 1240656657
      Actual:       1240656657
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.227s
OLD_EXIT=1
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.200s
NEW_EXIT=0
```

### option-presence-loss

Source: `src/FunnySharp/Option.cs`. Monitor: `mon_B0KZ1KQAJKDH8GK7`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.OptionTests.ToNullableBridgesValueOptionsWithoutChangingTheirEquality" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.OptionTests.ToNullableBridgesValueOptionsWithoutChangingTheirEquality" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.OptionTests.ToNullableBridgesValueOptionsWithoutChangingTheirEquality [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.286s
OLD_EXIT=1
    FunnySharp.Tests.OptionTests.ToNullableBridgesValueOptionsWithoutChangingTheirEquality [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.270s
NEW_EXIT=1
```

### option-nullable-loss

Source: `src/FunnySharp/OptionExtensions.cs`. Monitor: `mon_G9HATC9FRX6JA504`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.OptionTests.ToNullableBridgesValueOptionsWithoutChangingTheirEquality" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.OptionTests.ToNullableBridgesValueOptionsWithoutChangingTheirEquality" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.OptionTests.ToNullableBridgesValueOptionsWithoutChangingTheirEquality [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.240s
OLD_EXIT=1
    FunnySharp.Tests.OptionTests.ToNullableBridgesValueOptionsWithoutChangingTheirEquality [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.224s
NEW_EXIT=1
```

### option-equality-loss

Source: `src/FunnySharp/Option.cs`. Monitor: `mon_DN09DJ0GEMETVAZV`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.OptionTests.ToNullableBridgesValueOptionsWithoutChangingTheirEquality" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.OptionTests.ToNullableBridgesValueOptionsWithoutChangingTheirEquality" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.OptionTests.ToNullableBridgesValueOptionsWithoutChangingTheirEquality [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.238s
OLD_EXIT=1
    FunnySharp.Tests.OptionTests.ToNullableBridgesValueOptionsWithoutChangingTheirEquality [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.248s
NEW_EXIT=1
```

### result-default-guard

Source: `src/FunnySharp/Result.cs`. Monitor: `mon_X91DWPJ1088SQ4XK`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.DefaultStateTests.DefaultResultThrowsForStateAccessAndReportsUninitializedText" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.DefaultStateTests.DefaultResultThrowsForStateAccessAndReportsUninitializedText" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.DefaultStateTests.DefaultResultThrowsForStateAccessAndReportsUninitializedText [FAIL]
      Assert.Throws() Failure: No exception was thrown
      Expected: typeof(System.InvalidOperationException)
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.235s
OLD_EXIT=1
    FunnySharp.Tests.DefaultStateTests.DefaultResultThrowsForStateAccessAndReportsUninitializedText [FAIL]
      Assert.Throws() Failure: No exception was thrown
      Expected: typeof(System.InvalidOperationException)
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.251s
NEW_EXIT=1
```

### validation-default-guard

Source: `src/FunnySharp/Validation.cs`. Monitor: `mon_65X19K6ZS019V7Z7`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.DefaultStateTests.DefaultValidationThrowsForStateAccessAndReportsUninitializedText" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.DefaultStateTests.DefaultValidationThrowsForStateAccessAndReportsUninitializedText" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.DefaultStateTests.DefaultValidationThrowsForStateAccessAndReportsUninitializedText [FAIL]
      Assert.Throws() Failure: No exception was thrown
      Expected: typeof(System.InvalidOperationException)
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.266s
OLD_EXIT=1
    FunnySharp.Tests.DefaultStateTests.DefaultValidationThrowsForStateAccessAndReportsUninitializedText [FAIL]
      Assert.Throws() Failure: No exception was thrown
      Expected: typeof(System.InvalidOperationException)
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.239s
NEW_EXIT=1
```

### unit-default-guard

Source: `src/FunnySharp/UnitResult.cs`. Monitor: `mon_JQXYHXWPVNHDXN2F`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.UnitResultTests.DefaultUnitResultThrowsForEveryStateReadingMember" -method "FunnySharp.Tests.UnitResultAsyncTests.DefaultCarrierThrowsSynchronouslyForValidDelegates" -method "FunnySharp.Tests.UnitResultTraversalTests.SyncTraversalRejectsNullArgumentsEagerlyAndThrowsForDefaultElements" -method "FunnySharp.Tests.UnitResultTraversalTests.AsyncTraversalFaultsForDefaultElements" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.UnitResultTests.DefaultUnitResultThrowsForEveryStateReadingMember" -method "FunnySharp.Tests.UnitResultAsyncTests.DefaultCarrierThrowsSynchronouslyForValidDelegates" -method "FunnySharp.Tests.UnitResultTraversalTests.SyncTraversalRejectsNullArgumentsEagerlyAndThrowsForDefaultElements" -method "FunnySharp.Tests.UnitResultTraversalTests.AsyncTraversalFaultsForDefaultElements" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.UnitResultAsyncTests.DefaultCarrierThrowsSynchronouslyForValidDelegates [FAIL]
      Assert.Throws() Failure: No exception was thrown
      Expected: typeof(System.InvalidOperationException)
    FunnySharp.Tests.UnitResultTraversalTests.SyncTraversalRejectsNullArgumentsEagerlyAndThrowsForDefaultElements [FAIL]
      Assert.Throws() Failure: No exception was thrown
      Expected: typeof(System.InvalidOperationException)
    FunnySharp.Tests.UnitResultTests.DefaultUnitResultThrowsForEveryStateReadingMember [FAIL]
      Assert.Throws() Failure: No exception was thrown
      Expected: typeof(System.InvalidOperationException)
    FunnySharp.Tests.UnitResultTraversalTests.AsyncTraversalFaultsForDefaultElements [FAIL]
      Assert.Throws() Failure: No exception was thrown
      Expected: typeof(System.InvalidOperationException)
   FunnySharp.Tests  Total: 4, Errors: 0, Failed: 4, Skipped: 0, Not Run: 0, Time: 0.269s
OLD_EXIT=1
    FunnySharp.Tests.UnitResultTests.DefaultUnitResultThrowsForEveryStateReadingMember [FAIL]
      Assert.Throws() Failure: No exception was thrown
      Expected: typeof(System.InvalidOperationException)
    FunnySharp.Tests.UnitResultTraversalTests.SyncTraversalRejectsNullArgumentsEagerlyAndThrowsForDefaultElements [FAIL]
      Assert.Throws() Failure: No exception was thrown
      Expected: typeof(System.InvalidOperationException)
    FunnySharp.Tests.UnitResultAsyncTests.DefaultCarrierThrowsSynchronouslyForValidDelegates [FAIL]
      Assert.Throws() Failure: No exception was thrown
      Expected: typeof(System.InvalidOperationException)
    FunnySharp.Tests.UnitResultTraversalTests.AsyncTraversalFaultsForDefaultElements [FAIL]
      Assert.Throws() Failure: No exception was thrown
      Expected: typeof(System.InvalidOperationException)
   FunnySharp.Tests  Total: 4, Errors: 0, Failed: 4, Skipped: 0, Not Run: 0, Time: 0.277s
NEW_EXIT=1
```

### nulltask-message-control

Source: `src/FunnySharp/Result.cs`. Monitor: `mon_A5Z3580M5SFDDYXN`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.UnitResultBoundaryTests.AsyncTryRejectsNullTasksWithoutMappingThem" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.UnitResultBoundaryTests.AsyncTryRejectsNullTasksWithoutMappingThem" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.UnitResultBoundaryTests.AsyncTryRejectsNullTasksWithoutMappingThem [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "The operation returned a null task."
      Actual:   "The asynchronous operation did not return a Task."
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.267s
OLD_EXIT=1
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.242s
NEW_EXIT=0
```

### nulltask-wrong-type

Source: `src/FunnySharp/UnitResult.cs`. Monitor: `mon_2B9B6NJN8JHEPKSE`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.UnitResultBoundaryTests.AsyncTryRejectsNullTasksWithoutMappingThem" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.UnitResultBoundaryTests.AsyncTryRejectsNullTasksWithoutMappingThem" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.UnitResultBoundaryTests.AsyncTryRejectsNullTasksWithoutMappingThem [FAIL]
      Assert.Throws() Failure: Exception type was not an exact match
      Expected: typeof(System.InvalidOperationException)
      Actual:   typeof(System.ArgumentException)
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.232s
OLD_EXIT=1
    FunnySharp.Tests.UnitResultBoundaryTests.AsyncTryRejectsNullTasksWithoutMappingThem [FAIL]
      Assert.Throws() Failure: Exception type was not an exact match
      Expected: typeof(System.InvalidOperationException)
      Actual:   typeof(System.ArgumentException)
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.272s
NEW_EXIT=1
```

### nulltask-mapper-invoked

Source: `src/FunnySharp/UnitResult.cs`. Monitor: `mon_BQ1J78EMVQEQW9DG`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.UnitResultBoundaryTests.AsyncTryRejectsNullTasksWithoutMappingThem" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.UnitResultBoundaryTests.AsyncTryRejectsNullTasksWithoutMappingThem" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.UnitResultBoundaryTests.AsyncTryRejectsNullTasksWithoutMappingThem [FAIL]
      Assert.Equal() Failure: Values differ
      Expected: 0
      Actual:   1
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.230s
OLD_EXIT=1
    FunnySharp.Tests.UnitResultBoundaryTests.AsyncTryRejectsNullTasksWithoutMappingThem [FAIL]
      Assert.Equal() Failure: Values differ
      Expected: 0
      Actual:   1
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.263s
NEW_EXIT=1
```

### concurrent-message-control

Source: `src/FunnySharp/ParallelAsyncEnumerableExtensions.cs`. Monitor: `mon_VPAFS6VM0RTNVXNH`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.ParallelAsyncEnumerableTests.RejectsConcurrentMoveNextCalls" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.ParallelAsyncEnumerableTests.RejectsConcurrentMoveNextCalls" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.ParallelAsyncEnumerableTests.RejectsConcurrentMoveNextCalls(completionOrder: False) [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "Concurrent MoveNextAsync calls are not supported."
      Actual:   "An asynchronous move is already pending."
    FunnySharp.Tests.ParallelAsyncEnumerableTests.RejectsConcurrentMoveNextCalls(completionOrder: True) [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "Concurrent MoveNextAsync calls are not supported."
      Actual:   "An asynchronous move is already pending."
   FunnySharp.Tests  Total: 2, Errors: 0, Failed: 2, Skipped: 0, Not Run: 0, Time: 0.416s
OLD_EXIT=1
   FunnySharp.Tests  Total: 2, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.430s
NEW_EXIT=0
```

### concurrent-wrong-type

Source: `src/FunnySharp/ParallelAsyncEnumerableExtensions.cs`. Monitor: `mon_6HRQWFGJDC2A3X1A`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.ParallelAsyncEnumerableTests.RejectsConcurrentMoveNextCalls" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.ParallelAsyncEnumerableTests.RejectsConcurrentMoveNextCalls" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.ParallelAsyncEnumerableTests.RejectsConcurrentMoveNextCalls(completionOrder: False) [FAIL]
      Assert.Throws() Failure: Exception type was not an exact match
      Expected: typeof(System.InvalidOperationException)
      Actual:   typeof(System.ArgumentException)
    FunnySharp.Tests.ParallelAsyncEnumerableTests.RejectsConcurrentMoveNextCalls(completionOrder: True) [FAIL]
      Assert.Throws() Failure: Exception type was not an exact match
      Expected: typeof(System.InvalidOperationException)
      Actual:   typeof(System.ArgumentException)
   FunnySharp.Tests  Total: 2, Errors: 0, Failed: 2, Skipped: 0, Not Run: 0, Time: 0.277s
OLD_EXIT=1
    FunnySharp.Tests.ParallelAsyncEnumerableTests.RejectsConcurrentMoveNextCalls(completionOrder: False) [FAIL]
      Assert.Throws() Failure: Exception type was not an exact match
      Expected: typeof(System.InvalidOperationException)
      Actual:   typeof(System.ArgumentException)
    FunnySharp.Tests.ParallelAsyncEnumerableTests.RejectsConcurrentMoveNextCalls(completionOrder: True) [FAIL]
      Assert.Throws() Failure: Exception type was not an exact match
      Expected: typeof(System.InvalidOperationException)
      Actual:   typeof(System.ArgumentException)
   FunnySharp.Tests  Total: 2, Errors: 0, Failed: 2, Skipped: 0, Not Run: 0, Time: 0.280s
NEW_EXIT=1
```

### location-customer-loss

Source: `src/FunnySharp/Location.cs`. Monitor: `mon_9YA9GZ7938VQAE0R`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.AsyncCollectionTests.AsyncBatchValidationComposesNestedErrorLocations" -method "FunnySharp.Tests.TraversalContextTests.NestedLocatedTraverseRendersTheCanonicalCustomersAddressPostalCodePath" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.AsyncCollectionTests.AsyncBatchValidationComposesNestedErrorLocations" -method "FunnySharp.Tests.TraversalContextTests.NestedLocatedTraverseRendersTheCanonicalCustomersAddressPostalCodePath" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.TraversalContextTests.NestedLocatedTraverseRendersTheCanonicalCustomersAddressPostalCodePath [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "customers[17].addresses[2].postalCode"
      Actual:   "[17].addresses[2].postalCode"
    FunnySharp.Tests.AsyncCollectionTests.AsyncBatchValidationComposesNestedErrorLocations [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "customers[1].addresses[0].postalCode"
      Actual:   "[1].addresses[0].postalCode"
   FunnySharp.Tests  Total: 2, Errors: 0, Failed: 2, Skipped: 0, Not Run: 0, Time: 0.369s
OLD_EXIT=1
    FunnySharp.Tests.AsyncCollectionTests.AsyncBatchValidationComposesNestedErrorLocations [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "customers[1].addresses[0].postalCode"
      Actual:   "[1].addresses[0].postalCode"
    FunnySharp.Tests.TraversalContextTests.NestedLocatedTraverseRendersTheCanonicalCustomersAddressPostalCodePath [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "customers[17].addresses[2].postalCode"
      Actual:   "[17].addresses[2].postalCode"
   FunnySharp.Tests  Total: 2, Errors: 0, Failed: 2, Skipped: 0, Not Run: 0, Time: 0.334s
NEW_EXIT=1
```

### location-index-shift

Source: `src/FunnySharp/Location.cs`. Monitor: `mon_S6KFKBKSB727BHJK`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.AsyncCollectionTests.AsyncBatchValidationComposesNestedErrorLocations" -method "FunnySharp.Tests.TraversalContextTests.NestedLocatedTraverseRendersTheCanonicalCustomersAddressPostalCodePath" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.AsyncCollectionTests.AsyncBatchValidationComposesNestedErrorLocations" -method "FunnySharp.Tests.TraversalContextTests.NestedLocatedTraverseRendersTheCanonicalCustomersAddressPostalCodePath" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.AsyncCollectionTests.AsyncBatchValidationComposesNestedErrorLocations [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "customers[1].addresses[0].postalCode"
      Actual:   "customers[2].addresses[1].postalCode"
    FunnySharp.Tests.TraversalContextTests.NestedLocatedTraverseRendersTheCanonicalCustomersAddressPostalCodePath [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "customers[17].addresses[2].postalCode"
      Actual:   "customers[18].addresses[3].postalCode"
   FunnySharp.Tests  Total: 2, Errors: 0, Failed: 2, Skipped: 0, Not Run: 0, Time: 0.315s
OLD_EXIT=1
    FunnySharp.Tests.TraversalContextTests.NestedLocatedTraverseRendersTheCanonicalCustomersAddressPostalCodePath [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "customers[17].addresses[2].postalCode"
      Actual:   "customers[18].addresses[3].postalCode"
    FunnySharp.Tests.AsyncCollectionTests.AsyncBatchValidationComposesNestedErrorLocations [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "customers[1].addresses[0].postalCode"
      Actual:   "customers[2].addresses[1].postalCode"
   FunnySharp.Tests  Total: 2, Errors: 0, Failed: 2, Skipped: 0, Not Run: 0, Time: 0.372s
NEW_EXIT=1
```

### payload-message-loss

Source: `src/FunnySharp/Validation.cs`. Monitor: `mon_4AZAAGSF8VQFC4WN`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.AsyncCollectionTests.AsyncBatchValidationComposesNestedErrorLocations" -method "FunnySharp.Tests.TraversalContextTests.NestedLocatedTraverseRendersTheCanonicalCustomersAddressPostalCodePath" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.AsyncCollectionTests.AsyncBatchValidationComposesNestedErrorLocations" -method "FunnySharp.Tests.TraversalContextTests.NestedLocatedTraverseRendersTheCanonicalCustomersAddressPostalCodePath" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.AsyncCollectionTests.AsyncBatchValidationComposesNestedErrorLocations [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "The postal code must be five digits."
      Actual:   "corrupted fixture payload"
   FunnySharp.Tests  Total: 2, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.256s
OLD_EXIT=1
    FunnySharp.Tests.AsyncCollectionTests.AsyncBatchValidationComposesNestedErrorLocations [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "The postal code must be five digits."
      Actual:   "corrupted fixture payload"
   FunnySharp.Tests  Total: 2, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.251s
NEW_EXIT=1
```

### bridge-queue-dequeue-some-default

Source: `src/FunnySharp/ContainerExtensions.cs`. Monitor: `mon_P9Y23NEAN763335G`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(150,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.245s
OLD_EXIT=1
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(148,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.235s
NEW_EXIT=1
```

### bridge-queue-dequeue-throw

Source: `src/FunnySharp/ContainerExtensions.cs`. Monitor: `mon_KHYGRV9ZTPSZGP6M`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      System.InvalidOperationException : Empty bridge fault
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(150,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.257s
OLD_EXIT=1
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      System.InvalidOperationException : Empty bridge fault
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(148,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.304s
NEW_EXIT=1
```

### bridge-queue-dequeue-mutate-empty

Source: `src/FunnySharp/ContainerExtensions.cs`. Monitor: `mon_6TYSZXZERG9F4BR8`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(151,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.256s
OLD_EXIT=1
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(151,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.249s
NEW_EXIT=1
```

### bridge-queue-peek-some-default

Source: `src/FunnySharp/ContainerExtensions.cs`. Monitor: `mon_D3GETMSAHYXX26SM`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(151,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.261s
OLD_EXIT=1
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(151,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.219s
NEW_EXIT=1
```

### bridge-queue-peek-throw

Source: `src/FunnySharp/ContainerExtensions.cs`. Monitor: `mon_GEGMMWTKWFKBGPM9`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      System.InvalidOperationException : Empty bridge fault
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(151,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.208s
OLD_EXIT=1
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      System.InvalidOperationException : Empty bridge fault
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(151,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.224s
NEW_EXIT=1
```

### bridge-queue-peek-mutate-empty

Source: `src/FunnySharp/ContainerExtensions.cs`. Monitor: `mon_0VV7FPWSBDXHNDVV`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      Assert.Empty() Failure: Collection was not empty
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(156,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.231s
OLD_EXIT=1
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      Assert.Empty() Failure: Collection was not empty
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(156,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.216s
NEW_EXIT=1
```

### bridge-stack-pop-some-default

Source: `src/FunnySharp/ContainerExtensions.cs`. Monitor: `mon_2A88A4T1009Y8SW7`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(152,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.212s
OLD_EXIT=1
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(152,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.205s
NEW_EXIT=1
```

### bridge-stack-pop-throw

Source: `src/FunnySharp/ContainerExtensions.cs`. Monitor: `mon_SNXF5QM39RWX7K95`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      System.InvalidOperationException : Empty bridge fault
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(152,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.227s
OLD_EXIT=1
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      System.InvalidOperationException : Empty bridge fault
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(152,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.290s
NEW_EXIT=1
```

### bridge-stack-pop-mutate-empty

Source: `src/FunnySharp/ContainerExtensions.cs`. Monitor: `mon_HZ0A575NZR7T8JV4`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(153,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.280s
OLD_EXIT=1
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(153,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.343s
NEW_EXIT=1
```

### bridge-stack-peek-some-default

Source: `src/FunnySharp/ContainerExtensions.cs`. Monitor: `mon_01468MHPG5RD4X21`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(153,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.226s
OLD_EXIT=1
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(153,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.256s
NEW_EXIT=1
```

### bridge-stack-peek-throw

Source: `src/FunnySharp/ContainerExtensions.cs`. Monitor: `mon_H8AGTRJ9G092W80A`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      System.InvalidOperationException : Empty bridge fault
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(153,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.214s
OLD_EXIT=1
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      System.InvalidOperationException : Empty bridge fault
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(153,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.231s
NEW_EXIT=1
```

### bridge-stack-peek-mutate-empty

Source: `src/FunnySharp/ContainerExtensions.cs`. Monitor: `mon_V90YYFVG7XQVDMTM`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      Assert.Empty() Failure: Collection was not empty
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(157,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.234s
OLD_EXIT=1
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      Assert.Empty() Failure: Collection was not empty
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(157,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.240s
NEW_EXIT=1
```

### bridge-priority-dequeue-some-default

Source: `src/FunnySharp/ContainerExtensions.cs`. Monitor: `mon_04ER4WVYPJYAT788`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(154,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.244s
OLD_EXIT=1
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(154,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.257s
NEW_EXIT=1
```

### bridge-priority-dequeue-throw

Source: `src/FunnySharp/ContainerExtensions.cs`. Monitor: `mon_02KNAGGM757Z8NH9`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      System.InvalidOperationException : Empty bridge fault
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(154,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.213s
OLD_EXIT=1
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      System.InvalidOperationException : Empty bridge fault
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(154,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.215s
NEW_EXIT=1
```

### bridge-priority-dequeue-mutate-empty

Source: `src/FunnySharp/ContainerExtensions.cs`. Monitor: `mon_5VZTVNQHN7782B72`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(155,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.239s
OLD_EXIT=1
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(155,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.224s
NEW_EXIT=1
```

### bridge-priority-peek-some-default

Source: `src/FunnySharp/ContainerExtensions.cs`. Monitor: `mon_W0N4GFNYK95EXVZX`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(155,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.311s
OLD_EXIT=1
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(155,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.226s
NEW_EXIT=1
```

### bridge-priority-peek-throw

Source: `src/FunnySharp/ContainerExtensions.cs`. Monitor: `mon_YDV2HTF5V3V6EQ7S`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      System.InvalidOperationException : Empty bridge fault
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(155,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.235s
OLD_EXIT=1
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      System.InvalidOperationException : Empty bridge fault
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(155,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.233s
NEW_EXIT=1
```

### bridge-priority-peek-mutate-empty

Source: `src/FunnySharp/ContainerExtensions.cs`. Monitor: `mon_FTAGBSP5YPYEV2XR`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      Assert.Equal() Failure: Values differ
      Expected: 0
      Actual:   1
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(158,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.241s
OLD_EXIT=1
    FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow [FAIL]
      Assert.Equal() Failure: Values differ
      Expected: 0
      Actual:   1
        tests\FunnySharp.Tests\ContainerBridgeTests.cs(158,0): at FunnySharp.Tests.ContainerBridgeTests.EmptyContainersReturnNoneWhereTheBclOperationsThrow()
   FunnySharp.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.281s
NEW_EXIT=1
```

### concurrent-guard-removed

Source: `src/FunnySharp/ParallelAsyncEnumerableExtensions.cs`. Monitor: `mon_XNMVQ01XMJDM7N7K`.

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/mutant -p:UseSharedCompilation=false -nologo || exit $?;
printf "BUILD_EXIT=0\n";
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/mutant/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/old/FunnySharp.Tests.dll -method "FunnySharp.Tests.ParallelAsyncEnumerableTests.RejectsConcurrentMoveNextCalls" -noLogo -noColor -failSkips -failWarns;
printf "OLD_EXIT=%s\n" "$?";
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -method "FunnySharp.Tests.ParallelAsyncEnumerableTests.RejectsConcurrentMoveNextCalls" -noLogo -noColor -failSkips -failWarns;
printf "NEW_EXIT=%s\n" "$?";
exit 0
```

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0
    FunnySharp.Tests.ParallelAsyncEnumerableTests.RejectsConcurrentMoveNextCalls(completionOrder: False) [FAIL]
      Assert.Throws() Failure: No exception was thrown
      Expected: typeof(System.InvalidOperationException)
    FunnySharp.Tests.ParallelAsyncEnumerableTests.RejectsConcurrentMoveNextCalls(completionOrder: True) [FAIL]
      Assert.Throws() Failure: No exception was thrown
      Expected: typeof(System.InvalidOperationException)
   FunnySharp.Tests  Total: 2, Errors: 0, Failed: 2, Skipped: 0, Not Run: 0, Time: 0.283s
OLD_EXIT=1
    FunnySharp.Tests.ParallelAsyncEnumerableTests.RejectsConcurrentMoveNextCalls(completionOrder: False) [FAIL]
      Assert.Throws() Failure: No exception was thrown
      Expected: typeof(System.InvalidOperationException)
    FunnySharp.Tests.ParallelAsyncEnumerableTests.RejectsConcurrentMoveNextCalls(completionOrder: True) [FAIL]
      Assert.Throws() Failure: No exception was thrown
      Expected: typeof(System.InvalidOperationException)
   FunnySharp.Tests  Total: 2, Errors: 0, Failed: 2, Skipped: 0, Not Run: 0, Time: 0.279s
NEW_EXIT=1
```

## Matrix outcome

All **34 successfully compiled experiments** are executed: **31 meaningful faults** and **3 semantically legal controls**, with **43 cases in each assertion kit** (86 matrix case executions). Old: **42 failed, 1 passed**; revised: **38 failed, 5 passed**. Each meaningful fault returns runner exit 1 in both kits. The controls represent four theory-expanded cases: all four fail old and pass revised. Each accepted experiment has compiler exit 0, 0 warnings and 0 errors; no compiler failure is a detector rejection.

The one unaffected passing case under a meaningful mutant is the sync nested fixture during payload-message-loss: it does not call Validation.MapErrors. The async nested fixture fails on the exact original Message payload in both versions while its Location remains correct. No claim is made that the unaffected path rejects a fault it never executes. The 18 bridge faults each fail one full-method case in each kit and preserve all original None/state checks.

All five changed test files retain their **113 methods and 142 declared cases**; across the 13 assigned files the unchanged inventory is **244 methods and 307 declared cases**, including every InlineData row. No deletion or merge of a whole method is claimed. This is an oracle simplification with a necessary deterministic cleanup improvement, not a reduced-input suite. Shipping hashes and all shipping bodies are restored, so no library performance change is shipped.

## Restoration, final gate and patch

After the final concurrent guard mutant, all shipping files were restored by inverse apply_patch. The following exact monitored command verified the empty shipping diff, rebuilt pristine shipping binaries with locked restore, replaced both kit DLLs, and ran the entire revised core assembly without a method/class filter:

```bash
cd "Q:/repos/funnysharp/.omo/worktrees/test-ablation-core-oracles";
git diff --exit-code -- src/FunnySharp || exit $?;
printf "SOURCE_RESTORED_EXIT=0\n";
dotnet build src/FunnySharp/FunnySharp.csproj -c Release -p:RestoreLockedMode=true -o .omo/core-oracle-proof/restored -p:UseSharedCompilation=false -nologo || exit $?;
cp .omo/core-oracle-proof/restored/FunnySharp.dll .omo/core-oracle-proof/old/FunnySharp.dll;
cp .omo/core-oracle-proof/restored/FunnySharp.dll .omo/core-oracle-proof/new/FunnySharp.dll;
dotnet .omo/core-oracle-proof/new/FunnySharp.Tests.dll -noLogo -noColor -failSkips -failWarns;
code=$?; printf "FULL_CORE_EXIT=%s\n" "$code"; exit "$code"
```

Monitor: `mon_BCAQ1KM5D3VDTNTN`. Actual output:

```text
status: completed exit_code: 0
SOURCE_RESTORED_EXIT=0
  Determining projects to restore...
  All projects are up-to-date for restore.
  FunnySharp.Analyzers -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-core-oracles\.omo\core-oracle-proof\restored\FunnySharp.Analyzers.dll
  FunnySharp.Analyzers.CodeFixes -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-core-oracles\.omo\core-oracle-proof\restored\FunnySharp.Analyzers.CodeFixes.dll
  FunnySharp -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-core-oracles\.omo\core-oracle-proof\restored\FunnySharp.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:08.07
  Discovering: FunnySharp.Tests
  Discovered:  FunnySharp.Tests
  Starting:    FunnySharp.Tests
  Finished:    FunnySharp.Tests (ID = '866e69da0d314da6d6d491ef99bfd4dc84959c50bf45fef32f20415b8df659b6')
=== TEST EXECUTION SUMMARY ===
   FunnySharp.Tests  Total: 1762, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.947s
FULL_CORE_EXIT=0
```

The subsequent cleanup copied the restored PDBs into both kits, removed only the generated `.omo/core-oracle-proof/mutant` directory, verified its absence and reran `git diff --exit-code -- src/FunnySharp`. It returned exit 0 and `MUTANT_ARTIFACT_CLEANUP_EXIT=0`. The old and revised kits retain pristine shipping binaries; no temporary source mutation, reflection annotation, collision hash or mutant binary directory remains. No git commit, merge, remote write, dependency or framework addition occurred.

`git diff --check` exited 0. The permanent test diff is exactly **5 files, 13 insertions, 18 deletions**:

```text
tests/FunnySharp.Tests/AsyncCollectionTests.cs         |  3 ++-
tests/FunnySharp.Tests/ContainerBridgeTests.cs         |  7 -------
tests/FunnySharp.Tests/OptionTests.cs                  |  1 -
tests/FunnySharp.Tests/ParallelAsyncEnumerableTests.cs | 14 +++++++++-----
tests/FunnySharp.Tests/UnitResultBoundaryTests.cs      |  6 ++----
```

The only additional permanent file is this English experiment report. All default-message pins promised by the current guides remain; ResultBoundaryTests and every other unassigned file remain untouched. Full core **1762/1762** passed in one restored run. No skip, warning suppression, fixed sleep, polling, SHA computation, input shrink or deletion of a method is used. The nested worktree LSP duplicate-symbol diagnostics noted above remain pre-existing; actual locked compiler builds are clean. The parent owns integration and the full-solution/final gates outside this worker scope.

Parent commit gate: the complete repository `dotnet fsi build.fsx -- -p format`
exited 0 in 35.250 s on these final contents. Fresh per-file CSharp LSP requests
for all five edited files timed out after 3000 ms; this is not recorded as zero
diagnostics. The independently compiled final 1762-case suite above remains the
actual compiler/runtime evidence. Shipping diff remains empty.
