# Analyzer oracle ablation experiment

## Executed result

Parent acceptance independently ran a locked solution restore, the actual analyzer
test project build and the complete 168-case runner: zero warnings/errors,
168 passed, zero failed/skipped/not-run, exit 0. CSharp LSP diagnostics scanned
the changed test directory with zero errors. The repository formatter gate exited
0 (`41.736 s`); `git diff --exit-code -- src` also exited 0. These are current
unit checks, not a claim that runner timing proves a speedup.

The bounded A/C ablation is complete. Healthy old control, healthy candidate control, and restored final candidate suite each pass all 168 cases with zero errors, failures, skips, or not-run cases. Every meaningful mutant builds with zero warnings and errors before it is exercised; no compile failure is counted as a rejection. All temporary production changes are restored; the delivered permanent diff is seven scoped test/helper files plus this report. CSharp LSP was exercised but its pre-existing isolated-worktree reference-loading limitation remains explained below; actual compiler builds are clean. Parent integration and repository-wide gates remain the parent session responsibility.

| Witness | Old observed cases | Candidate observed cases | Exit old / candidate |
| --- | --- | --- | --- |
| Healthy full suite | 168 pass | 168 pass | 0 / 0 |
| Missing report, FS1001-FS1005 and all 11 changed callers | 11 fail | 11 fail | 1 / 1 |
| Wrong discard target: 12 underscore negatives plus compiling unsafe FixAll | 13 fail | 13 fail | 1 / 1 |
| Replace original Find RHS with Option.Some(2) | 1 fail | 1 fail | 1 / 1 |
| Remove await but retain SaveAsync invocation | 1 fail | 1 fail | 1 / 1 |
| Descriptor messages only | 11 fail | 11 pass | 1 / 0 |
| Whitespace only after assignment equals | 2 fail | 2 pass | 1 / 0 |
| Whitespace-only complete CodeFixTests class | Not a required old control | 25 pass | - / 0 |
| Restored final locked restore/build and full suite | Baseline control above | 168 pass | - / 0 |

Command receipts below preserve combined stdout/stderr and exit status, including runner-produced abbreviated strings and stack traces. Runner-reported times are observations only, not controlled performance claims. Execution was direct in this bounded native worker, without secondary delegation, commits, merges, remote writes, or new dependencies.

## Scope and baseline

Worktree: `Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer`; branch `ulw/test-ablation-analyzer`; baseline `141f9d04adecc35a3851c6ca1d6812d6aa9f32ba`; initial git status clean. SDK: `10.0.401`; standalone xUnit v3 in-process runner `4.0.0`, .NET `10.0.12`. Assessment: `../lanes/analyzers-http.md`, groups A and C (read from the g001 assessment worktree).

Only the seven authorized test/helper files and this report receive permanent changes. Temporary faults affect only the five analyzer implementations, `Descriptors.cs`, and `DiscardedOutcomeCodeFixProvider.cs`. No commit, dependency change, source input deletion, test deletion, skip, sleep, or frozen evidence edit is authorized or performed.

## Executed initial controls

The first monitor used PowerShell condition syntax in the bash runner and exited 2 before executing dotnet; that launch is not build/test evidence. The corrected command used bash `&&`. Locked restore succeeded for all five referenced projects. `dotnet build tests/FunnySharp.Analyzers.Tests/FunnySharp.Analyzers.Tests.csproj --no-restore` succeeded with `0 Warning(s)` and `0 Error(s)`, build elapsed `00:00:05.91`. Runner help (`-?`) deliberately exits 2; it confirms `-method`, `-class`, repeatable OR filters, `-failSkips`, and `-failWarns`. No `--filter-method` is used.

Healthy old control:

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer && dotnet tests/FunnySharp.Analyzers.Tests/bin/Debug/net10.0/FunnySharp.Analyzers.Tests.dll -failSkips -failWarns -noColor
```

```text
status: completed exit_code: 0
xUnit.net v3 In-Process Runner v4.0.0+8bf043c053 (64-bit .NET 10.0.12)
  Discovering: FunnySharp.Analyzers.Tests
  Discovered:  FunnySharp.Analyzers.Tests
  Starting:    FunnySharp.Analyzers.Tests
  Finished:    FunnySharp.Analyzers.Tests (ID = '350466aa4480b7353f2ad47adb8e0ac50cd6d8f445b9bec96dbf0999d53a8532')
=== TEST EXECUTION SUMMARY ===
   FunnySharp.Analyzers.Tests  Total: 168, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 4.985s
EXIT_TEST=0
```

### Healthy old full analyzer suite

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer && dotnet tests/FunnySharp.Analyzers.Tests/bin/Debug/net10.0/FunnySharp.Analyzers.Tests.dll -failSkips -failWarns -noColor; r=$?; printf "EXIT_TEST=%s\n" "$r"; exit "$r"
```

```text
status: completed exit_code: 0
xUnit.net v3 In-Process Runner v4.0.0+8bf043c053 (64-bit .NET 10.0.12)
  Discovering: FunnySharp.Analyzers.Tests
  Discovered:  FunnySharp.Analyzers.Tests
  Starting:    FunnySharp.Analyzers.Tests
  Finished:    FunnySharp.Analyzers.Tests (ID = '350466aa4480b7353f2ad47adb8e0ac50cd6d8f445b9bec96dbf0999d53a8532')
=== TEST EXECUTION SUMMARY ===
   FunnySharp.Analyzers.Tests  Total: 168, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 4.985s
EXIT_TEST=0
```

### Old five-rule missing-diagnostic witnesses

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer && dotnet build tests/FunnySharp.Analyzers.Tests/FunnySharp.Analyzers.Tests.csproj --no-restore && dotnet tests/FunnySharp.Analyzers.Tests/bin/Debug/net10.0/FunnySharp.Analyzers.Tests.dll -failSkips -failWarns -noColor -method "*.DefaultResultExpressionIsReported" -method "*.DiscardedUserCarrierCallIsReported" -method "*.UnawaitedValueTaskFromFunnySharpMemberIsReported" -method "*.AwaitedTaskOfOutcomeIsReported" -method "*.AwaitedStoredTaskOfOutcomeIsReported" -method "*.AwaitedConfiguredTaskOfOutcomeIsReported" -method "*.AwaitedTaskOfNonOutcomeFromFunnySharpMemberIsReported" -method "*.IgnoredOptionTryGetValueIsReported" -method "*.ResultOnDirectValueTaskIsReported" -method "*.GetAwaiterGetResultOnDirectValueTaskIsReported" -method "*.BothInterfacesIsReported"; r=$?; printf "EXIT_TEST=%s\n" "$r"; exit "$r"
```

```text
status: exited_1 exit_code: 1
  FunnySharp.Analyzers -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers\bin\Debug\netstandard2.0\FunnySharp.Analyzers.dll
  FunnySharp.Analyzers.CodeFixes -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers.CodeFixes\bin\Debug\netstandard2.0\FunnySharp.Analyzers.CodeFixes.dll
  FunnySharp -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp\bin\Debug\net10.0\FunnySharp.dll
  FunnySharp.AspNetCore -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.AspNetCore\bin\Debug\net10.0\FunnySharp.AspNetCore.dll
  FunnySharp.Analyzers.Tests -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\tests\FunnySharp.Analyzers.Tests\bin\Debug\net10.0\FunnySharp.Analyzers.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.20
xUnit.net v3 In-Process Runner v4.0.0+8bf043c053 (64-bit .NET 10.0.12)
  Discovering: FunnySharp.Analyzers.Tests
  Discovered:  FunnySharp.Analyzers.Tests
  Starting:    FunnySharp.Analyzers.Tests
    FunnySharp.Analyzers.Tests.IgnoredTryGetResultAnalyzerTests.IgnoredOptionTryGetValueIsReported [FAIL]
      Assert.Single() Failure: The collection was empty
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(14,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId, String messageFragment)
        tests\FunnySharp.Analyzers.Tests\IgnoredTryGetResultAnalyzerTests.cs(8,0): at FunnySharp.Analyzers.Tests.IgnoredTryGetResultAnalyzerTests.IgnoredOptionTryGetValueIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.UninitializedCarrierAnalyzerTests.DefaultResultExpressionIsReported [FAIL]
      Assert.Single() Failure: The collection was empty
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(14,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId, String messageFragment)
        tests\FunnySharp.Analyzers.Tests\UninitializedCarrierAnalyzerTests.cs(8,0): at FunnySharp.Analyzers.Tests.UninitializedCarrierAnalyzerTests.DefaultResultExpressionIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.UnawaitedValueTaskFromFunnySharpMemberIsReported [FAIL]
      Assert.Single() Failure: The collection was empty
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(14,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId, String messageFragment)
        tests\FunnySharp.Analyzers.Tests\DiscardedOutcomeAnalyzerTests.cs(62,0): at FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.UnawaitedValueTaskFromFunnySharpMemberIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.SyncDisposeAnalyzerTests.BothInterfacesIsReported [FAIL]
      Assert.Single() Failure: The collection was empty
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(14,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId, String messageFragment)
        tests\FunnySharp.Analyzers.Tests\SyncDisposeAnalyzerTests.cs(31,0): at FunnySharp.Analyzers.Tests.SyncDisposeAnalyzerTests.BothInterfacesIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.BlockedValueTaskAnalyzerTests.GetAwaiterGetResultOnDirectValueTaskIsReported [FAIL]
      Assert.Single() Failure: The collection was empty
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(14,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId, String messageFragment)
        tests\FunnySharp.Analyzers.Tests\BlockedValueTaskAnalyzerTests.cs(49,0): at FunnySharp.Analyzers.Tests.BlockedValueTaskAnalyzerTests.GetAwaiterGetResultOnDirectValueTaskIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.DiscardedUserCarrierCallIsReported [FAIL]
      Assert.Single() Failure: The collection was empty
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(14,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId, String messageFragment)
        tests\FunnySharp.Analyzers.Tests\DiscardedOutcomeAnalyzerTests.cs(8,0): at FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.DiscardedUserCarrierCallIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.BlockedValueTaskAnalyzerTests.ResultOnDirectValueTaskIsReported [FAIL]
      Assert.Single() Failure: The collection was empty
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(14,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId, String messageFragment)
        tests\FunnySharp.Analyzers.Tests\BlockedValueTaskAnalyzerTests.cs(11,0): at FunnySharp.Analyzers.Tests.BlockedValueTaskAnalyzerTests.ResultOnDirectValueTaskIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.AwaitedTaskOfOutcomeIsReported [FAIL]
      Assert.Single() Failure: The collection was empty
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(14,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId, String messageFragment)
        tests\FunnySharp.Analyzers.Tests\DiscardedOutcomeAnalyzerTests.cs(99,0): at FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.AwaitedTaskOfOutcomeIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.AwaitedStoredTaskOfOutcomeIsReported [FAIL]
      Assert.Single() Failure: The collection was empty
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(14,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId, String messageFragment)
        tests\FunnySharp.Analyzers.Tests\DiscardedOutcomeAnalyzerTests.cs(119,0): at FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.AwaitedStoredTaskOfOutcomeIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.AwaitedConfiguredTaskOfOutcomeIsReported [FAIL]
      Assert.Single() Failure: The collection was empty
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(14,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId, String messageFragment)
        tests\FunnySharp.Analyzers.Tests\DiscardedOutcomeAnalyzerTests.cs(140,0): at FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.AwaitedConfiguredTaskOfOutcomeIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.AwaitedTaskOfNonOutcomeFromFunnySharpMemberIsReported [FAIL]
      Assert.Single() Failure: The collection was empty
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(14,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId, String messageFragment)
        tests\FunnySharp.Analyzers.Tests\DiscardedOutcomeAnalyzerTests.cs(160,0): at FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.AwaitedTaskOfNonOutcomeFromFunnySharpMemberIsReported()
        --- End of stack trace from previous location ---
  Finished:    FunnySharp.Analyzers.Tests (ID = '350466aa4480b7353f2ad47adb8e0ac50cd6d8f445b9bec96dbf0999d53a8532')
=== TEST EXECUTION SUMMARY ===
   FunnySharp.Analyzers.Tests  Total: 11, Errors: 0, Failed: 11, Skipped: 0, Not Run: 0, Time: 1.920s
EXIT_TEST=1
```

### Old wrong-discard-target witness

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer && dotnet build tests/FunnySharp.Analyzers.Tests/FunnySharp.Analyzers.Tests.csproj --no-restore && dotnet tests/FunnySharp.Analyzers.Tests/bin/Debug/net10.0/FunnySharp.Analyzers.Tests.dll -failSkips -failWarns -noColor -method "*.DiscardedOutcomeBatchFixChangesOnlySafeStatements" -method "*.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol"; r=$?; printf "EXIT_TEST=%s\n" "$r"; exit "$r"
```

```text
status: exited_1 exit_code: 1
  FunnySharp.Analyzers -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers\bin\Debug\netstandard2.0\FunnySharp.Analyzers.dll
  FunnySharp.Analyzers.CodeFixes -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers.CodeFixes\bin\Debug\netstandard2.0\FunnySharp.Analyzers.CodeFixes.dll
  FunnySharp -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp\bin\Debug\net10.0\FunnySharp.dll
  FunnySharp.AspNetCore -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.AspNetCore\bin\Debug\net10.0\FunnySharp.AspNetCore.dll
  FunnySharp.Analyzers.Tests -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\tests\FunnySharp.Analyzers.Tests\bin\Debug\net10.0\FunnySharp.Analyzers.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.10
xUnit.net v3 In-Process Runner v4.0.0+8bf043c053 (64-bit .NET 10.0.12)
  Discovering: FunnySharp.Analyzers.Tests
  Discovered:  FunnySharp.Analyzers.Tests
  Starting:    FunnySharp.Analyzers.Tests
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeBatchFixChangesOnlySafeStatements [FAIL]
      Assert.Single() Failure: The collection was empty
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(226,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeBatchFixChangesOnlySafeStatements()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(members: "void Use() { int _ = 0; Find(); GC.KeepAlive(_); }") [FAIL]
      Assert.Empty() Failure: Collection was not empty
      Collection: [DocumentChangeAction { CreatedFromFactoryMethod = True, EquivalenceKey = "MakeDiscardedOutcomeExplicit", IsInlinable = False, NestedActions = [], Priority = Default, ··· }]
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(92,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(String members)
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(members: "void Use() { Option<int> _ = Option.Some(2); Find("···) [FAIL]
      Assert.Empty() Failure: Collection was not empty
      Collection: [DocumentChangeAction { CreatedFromFactoryMethod = True, EquivalenceKey = "MakeDiscardedOutcomeExplicit", IsInlinable = False, NestedActions = [], Priority = Default, ··· }]
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(92,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(String members)
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(members: "void Use(int _) { Find(); }") [FAIL]
      Assert.Empty() Failure: Collection was not empty
      Collection: [DocumentChangeAction { CreatedFromFactoryMethod = True, EquivalenceKey = "MakeDiscardedOutcomeExplicit", IsInlinable = False, NestedActions = [], Priority = Default, ··· }]
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(92,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(String members)
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(members: "void Use(Option<int> _) { Find(); }") [FAIL]
      Assert.Empty() Failure: Collection was not empty
      Collection: [DocumentChangeAction { CreatedFromFactoryMethod = True, EquivalenceKey = "MakeDiscardedOutcomeExplicit", IsInlinable = False, NestedActions = [], Priority = Default, ··· }]
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(92,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(String members)
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(members: "async Task UseAsync(Option<int> _) { await Task.Fr"···) [FAIL]
      Assert.Empty() Failure: Collection was not empty
      Collection: [DocumentChangeAction { CreatedFromFactoryMethod = True, EquivalenceKey = "MakeDiscardedOutcomeExplicit", IsInlinable = False, NestedActions = [], Priority = Default, ··· }]
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(92,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(String members)
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(members: "void Use() { Option<int> _ = Option.Some(2); Actio"···) [FAIL]
      Assert.Empty() Failure: Collection was not empty
      Collection: [DocumentChangeAction { CreatedFromFactoryMethod = True, EquivalenceKey = "MakeDiscardedOutcomeExplicit", IsInlinable = False, NestedActions = [], Priority = Default, ··· }]
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(92,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(String members)
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(members: "void Use() { Action<int> call = _ => { Find(); }; "···) [FAIL]
      Assert.Empty() Failure: Collection was not empty
      Collection: [DocumentChangeAction { CreatedFromFactoryMethod = True, EquivalenceKey = "MakeDiscardedOutcomeExplicit", IsInlinable = False, NestedActions = [], Priority = Default, ··· }]
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(92,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(String members)
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(members: "int _ = 0; void Use() { Find(); }") [FAIL]
      Assert.Empty() Failure: Collection was not empty
      Collection: [DocumentChangeAction { CreatedFromFactoryMethod = True, EquivalenceKey = "MakeDiscardedOutcomeExplicit", IsInlinable = False, NestedActions = [], Priority = Default, ··· }]
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(92,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(String members)
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(members: "Option<int> _ = Option.Some(2); void Use() { Find("···) [FAIL]
      Assert.Empty() Failure: Collection was not empty
      Collection: [DocumentChangeAction { CreatedFromFactoryMethod = True, EquivalenceKey = "MakeDiscardedOutcomeExplicit", IsInlinable = False, NestedActions = [], Priority = Default, ··· }]
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(92,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(String members)
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(members: "Option<int> _ { get; set; } = Option.Some(2); void"···) [FAIL]
      Assert.Empty() Failure: Collection was not empty
      Collection: [DocumentChangeAction { CreatedFromFactoryMethod = True, EquivalenceKey = "MakeDiscardedOutcomeExplicit", IsInlinable = False, NestedActions = [], Priority = Default, ··· }]
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(92,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(String members)
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(members: "void Use() { Find(); int _ = 0; GC.KeepAlive(_); }") [FAIL]
      Assert.Empty() Failure: Collection was not empty
      Collection: [DocumentChangeAction { CreatedFromFactoryMethod = True, EquivalenceKey = "MakeDiscardedOutcomeExplicit", IsInlinable = False, NestedActions = [], Priority = Default, ··· }]
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(92,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(String members)
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(members: "void Use() { Option<int> @_ = Option.Some(2); Find"···) [FAIL]
      Assert.Empty() Failure: Collection was not empty
      Collection: [DocumentChangeAction { CreatedFromFactoryMethod = True, EquivalenceKey = "MakeDiscardedOutcomeExplicit", IsInlinable = False, NestedActions = [], Priority = Default, ··· }]
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(92,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(String members)
        --- End of stack trace from previous location ---
  Finished:    FunnySharp.Analyzers.Tests (ID = '350466aa4480b7353f2ad47adb8e0ac50cd6d8f445b9bec96dbf0999d53a8532')
=== TEST EXECUTION SUMMARY ===
   FunnySharp.Analyzers.Tests  Total: 13, Errors: 0, Failed: 13, Skipped: 0, Not Run: 0, Time: 3.372s
EXIT_TEST=1
```

## Fault definitions and observations

Every mutant is applied alone, except the five independent missing-report mutations (one per rule) and the five descriptor message edits. Before moving to another fault, the prior exact replacement is reversed with `apply_patch`. Candidate replay uses the identical behavioral replacements and representative method filters. Every fault build must succeed; compilation failures are not detection evidence.

- Missing diagnostic: in each analyzer reporting site, replace `context.ReportDiagnostic(Diagnostic.Create(...));` with `_ = Diagnostic.Create(...);`. This keeps registration, real compilation, descriptor ID/severity, and the reporting-site computation intact but never publishes the diagnostic. Eleven retained message-bearing inputs cover all five classes and every removed message argument. Each independently fails `Assert.Single` on an empty diagnostic collection in the old control.
- Wrong discard target: loosen the provider admission check from `ISimpleAssignmentOperation { Target: IDiscardOperation }` to `ISimpleAssignmentOperation`. The 12 underscore-binding negatives reject the offered unsafe action. The FixAll witness has a compiling `Option<int> _` shadowed parameter and independently rejects consuming its statement: the required remaining FS1002 diagnostic disappears. Its failure occurs after `AnalyzerHarness.GetDiagnosticsAsync(fixedCompilation)` validates that the fixed source compiles. No invalid fixed-source compiler failure is counted.
- Replaced RHS: replace `statement.Expression.WithoutLeadingTrivia()` with `SyntaxFactory.ParseExpression("FunnySharp.Option.Some(2)")` in the generated assignment. The sync fixed source compiles but loses `Find()`; the old text check rejects it.
- Removed await: retain the invocation but strip an `AwaitExpressionSyntax` before producing the assignment. The awaited fixed source compiles as `_ = SaveAsync();`; the old check rejects it. Compiler warnings in an in-memory consumer source are outside the existing harness error-only compilation contract and are not test warnings or failure evidence.
- Message-only reword: replace all five `messageFormat` values with `"Review this FunnySharp usage"`. Diagnostic IDs, severity, spans, suppression, and registration stay intact.
- Whitespace-only output: replace only the `SyntaxFactory.Space` trivia value after `=` with `SyntaxFactory.ElasticCarriageReturnLineFeed`. The target, RHS, awaits, calls, and source inputs stay intact; formatting acceptance is the intended contrast.

### Old replaced-RHS witness

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer && dotnet build tests/FunnySharp.Analyzers.Tests/FunnySharp.Analyzers.Tests.csproj --no-restore && dotnet tests/FunnySharp.Analyzers.Tests/bin/Debug/net10.0/FunnySharp.Analyzers.Tests.dll -failSkips -failWarns -noColor -method "*.DiscardedOutcomeFixMakesTheDiscardExplicit"; r=$?; printf "EXIT_TEST=%s\n" "$r"; exit "$r"
```

```text
status: exited_1 exit_code: 1
  FunnySharp.Analyzers -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers\bin\Debug\netstandard2.0\FunnySharp.Analyzers.dll
  FunnySharp.Analyzers.CodeFixes -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers.CodeFixes\bin\Debug\netstandard2.0\FunnySharp.Analyzers.CodeFixes.dll
  FunnySharp -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp\bin\Debug\net10.0\FunnySharp.dll
  FunnySharp.AspNetCore -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.AspNetCore\bin\Debug\net10.0\FunnySharp.AspNetCore.dll
  FunnySharp.Analyzers.Tests -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\tests\FunnySharp.Analyzers.Tests\bin\Debug\net10.0\FunnySharp.Analyzers.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.95
xUnit.net v3 In-Process Runner v4.0.0+8bf043c053 (64-bit .NET 10.0.12)
  Discovering: FunnySharp.Analyzers.Tests
  Discovered:  FunnySharp.Analyzers.Tests
  Starting:    FunnySharp.Analyzers.Tests
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixMakesTheDiscardExplicit [FAIL]
      Assert.Contains() Failure: Sub-string not found
      String:    "using FunnySharp;\nclass C\n{\n    Option<int> Find()"···
      Not found: "_ = Find();"
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(31,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixMakesTheDiscardExplicit()
        --- End of stack trace from previous location ---
  Finished:    FunnySharp.Analyzers.Tests (ID = '350466aa4480b7353f2ad47adb8e0ac50cd6d8f445b9bec96dbf0999d53a8532')
=== TEST EXECUTION SUMMARY ===
   FunnySharp.Analyzers.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 2.840s
EXIT_TEST=1
```

### Old removed-await witness

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer && dotnet build tests/FunnySharp.Analyzers.Tests/FunnySharp.Analyzers.Tests.csproj --no-restore && dotnet tests/FunnySharp.Analyzers.Tests/bin/Debug/net10.0/FunnySharp.Analyzers.Tests.dll -failSkips -failWarns -noColor -method "*.DiscardedOutcomeFixPreservesAwaitedStatements"; r=$?; printf "EXIT_TEST=%s\n" "$r"; exit "$r"
```

```text
status: exited_1 exit_code: 1
  FunnySharp.Analyzers -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers\bin\Debug\netstandard2.0\FunnySharp.Analyzers.dll
  FunnySharp.Analyzers.CodeFixes -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers.CodeFixes\bin\Debug\netstandard2.0\FunnySharp.Analyzers.CodeFixes.dll
  FunnySharp -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp\bin\Debug\net10.0\FunnySharp.dll
  FunnySharp.AspNetCore -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.AspNetCore\bin\Debug\net10.0\FunnySharp.AspNetCore.dll
  FunnySharp.Analyzers.Tests -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\tests\FunnySharp.Analyzers.Tests\bin\Debug\net10.0\FunnySharp.Analyzers.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:03.14
xUnit.net v3 In-Process Runner v4.0.0+8bf043c053 (64-bit .NET 10.0.12)
  Discovering: FunnySharp.Analyzers.Tests
  Discovered:  FunnySharp.Analyzers.Tests
  Starting:    FunnySharp.Analyzers.Tests
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixPreservesAwaitedStatements [FAIL]
      Assert.Contains() Failure: Sub-string not found
      String:    "using FunnySharp;\nclass C\n{\n    async Task UseAsyn"···
      Not found: "_ = await SaveAsync();"
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(55,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixPreservesAwaitedStatements()
        --- End of stack trace from previous location ---
  Finished:    FunnySharp.Analyzers.Tests (ID = '350466aa4480b7353f2ad47adb8e0ac50cd6d8f445b9bec96dbf0999d53a8532')
=== TEST EXECUTION SUMMARY ===
   FunnySharp.Analyzers.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 4.084s
EXIT_TEST=1
```

### Old descriptor-message-only contrast

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer && dotnet build tests/FunnySharp.Analyzers.Tests/FunnySharp.Analyzers.Tests.csproj --no-restore && dotnet tests/FunnySharp.Analyzers.Tests/bin/Debug/net10.0/FunnySharp.Analyzers.Tests.dll -failSkips -failWarns -noColor -method "*.DefaultResultExpressionIsReported" -method "*.DiscardedUserCarrierCallIsReported" -method "*.UnawaitedValueTaskFromFunnySharpMemberIsReported" -method "*.AwaitedTaskOfOutcomeIsReported" -method "*.AwaitedStoredTaskOfOutcomeIsReported" -method "*.AwaitedConfiguredTaskOfOutcomeIsReported" -method "*.AwaitedTaskOfNonOutcomeFromFunnySharpMemberIsReported" -method "*.IgnoredOptionTryGetValueIsReported" -method "*.ResultOnDirectValueTaskIsReported" -method "*.GetAwaiterGetResultOnDirectValueTaskIsReported" -method "*.BothInterfacesIsReported"; r=$?; printf "EXIT_TEST=%s\n" "$r"; exit "$r"
```

```text
status: exited_1 exit_code: 1
  FunnySharp.Analyzers -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers\bin\Debug\netstandard2.0\FunnySharp.Analyzers.dll
  FunnySharp.Analyzers.CodeFixes -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers.CodeFixes\bin\Debug\netstandard2.0\FunnySharp.Analyzers.CodeFixes.dll
  FunnySharp -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp\bin\Debug\net10.0\FunnySharp.dll
  FunnySharp.AspNetCore -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.AspNetCore\bin\Debug\net10.0\FunnySharp.AspNetCore.dll
  FunnySharp.Analyzers.Tests -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\tests\FunnySharp.Analyzers.Tests\bin\Debug\net10.0\FunnySharp.Analyzers.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:03.60
xUnit.net v3 In-Process Runner v4.0.0+8bf043c053 (64-bit .NET 10.0.12)
  Discovering: FunnySharp.Analyzers.Tests
  Discovered:  FunnySharp.Analyzers.Tests
  Starting:    FunnySharp.Analyzers.Tests
    FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.UnawaitedValueTaskFromFunnySharpMemberIsReported [FAIL]
      Assert.Contains() Failure: Sub-string not found
      String:    "Review this FunnySharp usage"
      Not found: "unawaited work"
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(18,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId, String messageFragment)
        tests\FunnySharp.Analyzers.Tests\DiscardedOutcomeAnalyzerTests.cs(62,0): at FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.UnawaitedValueTaskFromFunnySharpMemberIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.BlockedValueTaskAnalyzerTests.GetAwaiterGetResultOnDirectValueTaskIsReported [FAIL]
      Assert.Contains() Failure: Sub-string not found
      String:    "Review this FunnySharp usage"
      Not found: "GetAwaiter().GetResult()"
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(18,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId, String messageFragment)
        tests\FunnySharp.Analyzers.Tests\BlockedValueTaskAnalyzerTests.cs(49,0): at FunnySharp.Analyzers.Tests.BlockedValueTaskAnalyzerTests.GetAwaiterGetResultOnDirectValueTaskIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.SyncDisposeAnalyzerTests.BothInterfacesIsReported [FAIL]
      Assert.Contains() Failure: Sub-string not found
      String:    "Review this FunnySharp usage"
      Not found: "Both"
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(18,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId, String messageFragment)
        tests\FunnySharp.Analyzers.Tests\SyncDisposeAnalyzerTests.cs(31,0): at FunnySharp.Analyzers.Tests.SyncDisposeAnalyzerTests.BothInterfacesIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.UninitializedCarrierAnalyzerTests.DefaultResultExpressionIsReported [FAIL]
      Assert.Contains() Failure: Sub-string not found
      String:    "Review this FunnySharp usage"
      Not found: "Result<int, string>"
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(18,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId, String messageFragment)
        tests\FunnySharp.Analyzers.Tests\UninitializedCarrierAnalyzerTests.cs(8,0): at FunnySharp.Analyzers.Tests.UninitializedCarrierAnalyzerTests.DefaultResultExpressionIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.IgnoredTryGetResultAnalyzerTests.IgnoredOptionTryGetValueIsReported [FAIL]
      Assert.Contains() Failure: Sub-string not found
      String:    "Review this FunnySharp usage"
      Not found: "TryGetValue"
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(18,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId, String messageFragment)
        tests\FunnySharp.Analyzers.Tests\IgnoredTryGetResultAnalyzerTests.cs(8,0): at FunnySharp.Analyzers.Tests.IgnoredTryGetResultAnalyzerTests.IgnoredOptionTryGetValueIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.DiscardedUserCarrierCallIsReported [FAIL]
      Assert.Contains() Failure: Sub-string not found
      String:    "Review this FunnySharp usage"
      Not found: "outcome value"
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(18,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId, String messageFragment)
        tests\FunnySharp.Analyzers.Tests\DiscardedOutcomeAnalyzerTests.cs(8,0): at FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.DiscardedUserCarrierCallIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.BlockedValueTaskAnalyzerTests.ResultOnDirectValueTaskIsReported [FAIL]
      Assert.Contains() Failure: Sub-string not found
      String:    "Review this FunnySharp usage"
      Not found: "Result"
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(18,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId, String messageFragment)
        tests\FunnySharp.Analyzers.Tests\BlockedValueTaskAnalyzerTests.cs(11,0): at FunnySharp.Analyzers.Tests.BlockedValueTaskAnalyzerTests.ResultOnDirectValueTaskIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.AwaitedTaskOfOutcomeIsReported [FAIL]
      Assert.Contains() Failure: Sub-string not found
      String:    "Review this FunnySharp usage"
      Not found: "task of an outcome value"
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(18,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId, String messageFragment)
        tests\FunnySharp.Analyzers.Tests\DiscardedOutcomeAnalyzerTests.cs(99,0): at FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.AwaitedTaskOfOutcomeIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.AwaitedStoredTaskOfOutcomeIsReported [FAIL]
      Assert.Contains() Failure: Sub-string not found
      String:    "Review this FunnySharp usage"
      Not found: "task of an outcome value"
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(18,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId, String messageFragment)
        tests\FunnySharp.Analyzers.Tests\DiscardedOutcomeAnalyzerTests.cs(119,0): at FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.AwaitedStoredTaskOfOutcomeIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.AwaitedConfiguredTaskOfOutcomeIsReported [FAIL]
      Assert.Contains() Failure: Sub-string not found
      String:    "Review this FunnySharp usage"
      Not found: "task of an outcome value"
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(18,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId, String messageFragment)
        tests\FunnySharp.Analyzers.Tests\DiscardedOutcomeAnalyzerTests.cs(140,0): at FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.AwaitedConfiguredTaskOfOutcomeIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.AwaitedTaskOfNonOutcomeFromFunnySharpMemberIsReported [FAIL]
      Assert.Contains() Failure: Sub-string not found
      String:    "Review this FunnySharp usage"
      Not found: "unawaited work"
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(18,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId, String messageFragment)
        tests\FunnySharp.Analyzers.Tests\DiscardedOutcomeAnalyzerTests.cs(160,0): at FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.AwaitedTaskOfNonOutcomeFromFunnySharpMemberIsReported()
        --- End of stack trace from previous location ---
  Finished:    FunnySharp.Analyzers.Tests (ID = '350466aa4480b7353f2ad47adb8e0ac50cd6d8f445b9bec96dbf0999d53a8532')
=== TEST EXECUTION SUMMARY ===
   FunnySharp.Analyzers.Tests  Total: 11, Errors: 0, Failed: 11, Skipped: 0, Not Run: 0, Time: 2.131s
EXIT_TEST=1
```

### Old whitespace-only contrast

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer && dotnet build tests/FunnySharp.Analyzers.Tests/FunnySharp.Analyzers.Tests.csproj --no-restore && dotnet tests/FunnySharp.Analyzers.Tests/bin/Debug/net10.0/FunnySharp.Analyzers.Tests.dll -failSkips -failWarns -noColor -method "*.DiscardedOutcomeFixMakesTheDiscardExplicit" -method "*.DiscardedOutcomeFixPreservesAwaitedStatements"; r=$?; printf "EXIT_TEST=%s\n" "$r"; exit "$r"
```

```text
status: exited_1 exit_code: 1
  FunnySharp.Analyzers -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers\bin\Debug\netstandard2.0\FunnySharp.Analyzers.dll
  FunnySharp.Analyzers.CodeFixes -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers.CodeFixes\bin\Debug\netstandard2.0\FunnySharp.Analyzers.CodeFixes.dll
  FunnySharp -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp\bin\Debug\net10.0\FunnySharp.dll
  FunnySharp.AspNetCore -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.AspNetCore\bin\Debug\net10.0\FunnySharp.AspNetCore.dll
  FunnySharp.Analyzers.Tests -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\tests\FunnySharp.Analyzers.Tests\bin\Debug\net10.0\FunnySharp.Analyzers.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.99
xUnit.net v3 In-Process Runner v4.0.0+8bf043c053 (64-bit .NET 10.0.12)
  Discovering: FunnySharp.Analyzers.Tests
  Discovered:  FunnySharp.Analyzers.Tests
  Starting:    FunnySharp.Analyzers.Tests
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixMakesTheDiscardExplicit [FAIL]
      Assert.Contains() Failure: Sub-string not found
      String:    "using FunnySharp;\nclass C\n{\n    Option<int> Find()"···
      Not found: "_ = Find();"
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(31,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixMakesTheDiscardExplicit()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixPreservesAwaitedStatements [FAIL]
      Assert.Contains() Failure: Sub-string not found
      String:    "using FunnySharp;\nclass C\n{\n    async Task UseAsyn"···
      Not found: "_ = await SaveAsync();"
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(55,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixPreservesAwaitedStatements()
        --- End of stack trace from previous location ---
  Finished:    FunnySharp.Analyzers.Tests (ID = '350466aa4480b7353f2ad47adb8e0ac50cd6d8f445b9bec96dbf0999d53a8532')
=== TEST EXECUTION SUMMARY ===
   FunnySharp.Analyzers.Tests  Total: 2, Errors: 0, Failed: 2, Skipped: 0, Not Run: 0, Time: 2.720s
EXIT_TEST=1
```

### Healthy candidate full analyzer suite

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer && dotnet build tests/FunnySharp.Analyzers.Tests/FunnySharp.Analyzers.Tests.csproj --no-restore && dotnet tests/FunnySharp.Analyzers.Tests/bin/Debug/net10.0/FunnySharp.Analyzers.Tests.dll -failSkips -failWarns -noColor; r=$?; printf "EXIT_TEST=%s\n" "$r"; exit "$r"
```

```text
status: completed exit_code: 0
  FunnySharp.Analyzers -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers\bin\Debug\netstandard2.0\FunnySharp.Analyzers.dll
  FunnySharp.Analyzers.CodeFixes -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers.CodeFixes\bin\Debug\netstandard2.0\FunnySharp.Analyzers.CodeFixes.dll
  FunnySharp -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp\bin\Debug\net10.0\FunnySharp.dll
  FunnySharp.AspNetCore -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.AspNetCore\bin\Debug\net10.0\FunnySharp.AspNetCore.dll
  FunnySharp.Analyzers.Tests -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\tests\FunnySharp.Analyzers.Tests\bin\Debug\net10.0\FunnySharp.Analyzers.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.08
xUnit.net v3 In-Process Runner v4.0.0+8bf043c053 (64-bit .NET 10.0.12)
  Discovering: FunnySharp.Analyzers.Tests
  Discovered:  FunnySharp.Analyzers.Tests
  Starting:    FunnySharp.Analyzers.Tests
  Finished:    FunnySharp.Analyzers.Tests (ID = '350466aa4480b7353f2ad47adb8e0ac50cd6d8f445b9bec96dbf0999d53a8532')
=== TEST EXECUTION SUMMARY ===
   FunnySharp.Analyzers.Tests  Total: 168, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 3.973s
EXIT_TEST=0
```

### Candidate five-rule missing-diagnostic witnesses

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer && dotnet build tests/FunnySharp.Analyzers.Tests/FunnySharp.Analyzers.Tests.csproj --no-restore && dotnet tests/FunnySharp.Analyzers.Tests/bin/Debug/net10.0/FunnySharp.Analyzers.Tests.dll -failSkips -failWarns -noColor -method "*.DefaultResultExpressionIsReported" -method "*.DiscardedUserCarrierCallIsReported" -method "*.UnawaitedValueTaskFromFunnySharpMemberIsReported" -method "*.AwaitedTaskOfOutcomeIsReported" -method "*.AwaitedStoredTaskOfOutcomeIsReported" -method "*.AwaitedConfiguredTaskOfOutcomeIsReported" -method "*.AwaitedTaskOfNonOutcomeFromFunnySharpMemberIsReported" -method "*.IgnoredOptionTryGetValueIsReported" -method "*.ResultOnDirectValueTaskIsReported" -method "*.GetAwaiterGetResultOnDirectValueTaskIsReported" -method "*.BothInterfacesIsReported"; r=$?; printf "EXIT_TEST=%s\n" "$r"; exit "$r"
```

```text
status: exited_1 exit_code: 1
  FunnySharp.Analyzers -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers\bin\Debug\netstandard2.0\FunnySharp.Analyzers.dll
  FunnySharp.Analyzers.CodeFixes -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers.CodeFixes\bin\Debug\netstandard2.0\FunnySharp.Analyzers.CodeFixes.dll
  FunnySharp -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp\bin\Debug\net10.0\FunnySharp.dll
  FunnySharp.AspNetCore -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.AspNetCore\bin\Debug\net10.0\FunnySharp.AspNetCore.dll
  FunnySharp.Analyzers.Tests -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\tests\FunnySharp.Analyzers.Tests\bin\Debug\net10.0\FunnySharp.Analyzers.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.21
xUnit.net v3 In-Process Runner v4.0.0+8bf043c053 (64-bit .NET 10.0.12)
  Discovering: FunnySharp.Analyzers.Tests
  Discovered:  FunnySharp.Analyzers.Tests
  Starting:    FunnySharp.Analyzers.Tests
    FunnySharp.Analyzers.Tests.UninitializedCarrierAnalyzerTests.DefaultResultExpressionIsReported [FAIL]
      Assert.Single() Failure: The collection was empty
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(14,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId)
        tests\FunnySharp.Analyzers.Tests\UninitializedCarrierAnalyzerTests.cs(8,0): at FunnySharp.Analyzers.Tests.UninitializedCarrierAnalyzerTests.DefaultResultExpressionIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.SyncDisposeAnalyzerTests.BothInterfacesIsReported [FAIL]
      Assert.Single() Failure: The collection was empty
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(14,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId)
        tests\FunnySharp.Analyzers.Tests\SyncDisposeAnalyzerTests.cs(31,0): at FunnySharp.Analyzers.Tests.SyncDisposeAnalyzerTests.BothInterfacesIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.BlockedValueTaskAnalyzerTests.GetAwaiterGetResultOnDirectValueTaskIsReported [FAIL]
      Assert.Single() Failure: The collection was empty
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(14,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId)
        tests\FunnySharp.Analyzers.Tests\BlockedValueTaskAnalyzerTests.cs(48,0): at FunnySharp.Analyzers.Tests.BlockedValueTaskAnalyzerTests.GetAwaiterGetResultOnDirectValueTaskIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.UnawaitedValueTaskFromFunnySharpMemberIsReported [FAIL]
      Assert.Single() Failure: The collection was empty
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(14,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId)
        tests\FunnySharp.Analyzers.Tests\DiscardedOutcomeAnalyzerTests.cs(61,0): at FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.UnawaitedValueTaskFromFunnySharpMemberIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.IgnoredTryGetResultAnalyzerTests.IgnoredOptionTryGetValueIsReported [FAIL]
      Assert.Single() Failure: The collection was empty
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(14,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId)
        tests\FunnySharp.Analyzers.Tests\IgnoredTryGetResultAnalyzerTests.cs(8,0): at FunnySharp.Analyzers.Tests.IgnoredTryGetResultAnalyzerTests.IgnoredOptionTryGetValueIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.DiscardedUserCarrierCallIsReported [FAIL]
      Assert.Single() Failure: The collection was empty
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(14,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId)
        tests\FunnySharp.Analyzers.Tests\DiscardedOutcomeAnalyzerTests.cs(8,0): at FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.DiscardedUserCarrierCallIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.BlockedValueTaskAnalyzerTests.ResultOnDirectValueTaskIsReported [FAIL]
      Assert.Single() Failure: The collection was empty
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(14,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId)
        tests\FunnySharp.Analyzers.Tests\BlockedValueTaskAnalyzerTests.cs(11,0): at FunnySharp.Analyzers.Tests.BlockedValueTaskAnalyzerTests.ResultOnDirectValueTaskIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.AwaitedTaskOfOutcomeIsReported [FAIL]
      Assert.Single() Failure: The collection was empty
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(14,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId)
        tests\FunnySharp.Analyzers.Tests\DiscardedOutcomeAnalyzerTests.cs(97,0): at FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.AwaitedTaskOfOutcomeIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.AwaitedStoredTaskOfOutcomeIsReported [FAIL]
      Assert.Single() Failure: The collection was empty
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(14,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId)
        tests\FunnySharp.Analyzers.Tests\DiscardedOutcomeAnalyzerTests.cs(116,0): at FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.AwaitedStoredTaskOfOutcomeIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.AwaitedConfiguredTaskOfOutcomeIsReported [FAIL]
      Assert.Single() Failure: The collection was empty
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(14,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId)
        tests\FunnySharp.Analyzers.Tests\DiscardedOutcomeAnalyzerTests.cs(136,0): at FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.AwaitedConfiguredTaskOfOutcomeIsReported()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.AwaitedTaskOfNonOutcomeFromFunnySharpMemberIsReported [FAIL]
      Assert.Single() Failure: The collection was empty
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\AnalyzerTestAssert.cs(14,0): at FunnySharp.Analyzers.Tests.AnalyzerTestAssert.DiagnosticAsync(String source, String expectedId)
        tests\FunnySharp.Analyzers.Tests\DiscardedOutcomeAnalyzerTests.cs(155,0): at FunnySharp.Analyzers.Tests.DiscardedOutcomeAnalyzerTests.AwaitedTaskOfNonOutcomeFromFunnySharpMemberIsReported()
        --- End of stack trace from previous location ---
  Finished:    FunnySharp.Analyzers.Tests (ID = '350466aa4480b7353f2ad47adb8e0ac50cd6d8f445b9bec96dbf0999d53a8532')
=== TEST EXECUTION SUMMARY ===
   FunnySharp.Analyzers.Tests  Total: 11, Errors: 0, Failed: 11, Skipped: 0, Not Run: 0, Time: 1.931s
EXIT_TEST=1
```

## Preserved partitions and maintenance reduction

The seven scoped files retain all test methods and inputs. A direct comparison of the old and candidate file contents found every raw C# source string, every `[InlineData]` line, and every test method name identical: 73 raw source inputs, 85 InlineData entries, and 73 test methods across the six test classes. The counts are lexical input preservation checks, not claims that 73 methods equal 168 runtime cases. The full suite remains the runtime case authority.

| File | Raw source inputs | InlineData entries | Test methods | All three sequences equal |
| --- | ---: | ---: | ---: | --- |
| `AnalyzerTestAssert.cs` | 0 | 0 | 0 | true |
| `BlockedValueTaskAnalyzerTests.cs` | 13 | 48 | 13 | true |
| `DiscardedOutcomeAnalyzerTests.cs` | 20 | 0 | 20 | true |
| `IgnoredTryGetResultAnalyzerTests.cs` | 9 | 0 | 9 | true |
| `SyncDisposeAnalyzerTests.cs` | 7 | 0 | 7 | true |
| `UninitializedCarrierAnalyzerTests.cs` | 15 | 19 | 15 | true |
| `CodeFixTests.cs` | 9 | 18 | 9 | true |

Removed detection obligations: eleven noncontractual explanatory-message substrings (including member/type wording and advice) and two literal spacing forms. The optional `messageFragment` parameter and conditional `Assert.Contains` branch disappear. No diagnostic count, ID, suppression, span, or severity assertion is deleted; no source input or test method is deleted. This removes eleven wording dependencies and two formatting dependencies, not analyzer execution or real compilation.

Independent observations that survive: real in-memory source compilation with real built FunnySharp assemblies; exactly one diagnostic and exact FS1001-FS1005 ID for each positive; no diagnostic for each negative; the existing FS1004 multi-access cardinality/order/span/severity checks; codefix action cardinality; a compiling fixed source with the required assignment count and semantic `IDiscardOperation` target; exactly one zero-argument `C.Find` invocation as the synchronous assignment value; exactly one `IAwaitOperation` over a zero-argument `C.SaveAsync` invocation in the awaited case; quiet post-fix analysis; all twelve underscore-binding negatives and six safe-action input partitions; FixAll safe versus shadowed statement behavior; complete statement and semicolon comment/trivia equality. `AssertDiscardTargets` now returns its existing assignment-operation array for the two callers. There is no general assertion framework or new helper type.

Runtime receipts contain actual runner-reported durations. The healthy old suite reported 4.985s and the first healthy candidate reported 3.973s. They are single sequential runs, not a controlled benchmark; no runtime speedup or allocation claim is inferred. Candidate semantic checks intentionally retain Roslyn semantic work.

## CSharp LSP diagnostics

LSP was requested before the old build (two fresh-diagnostic requests timed out at 3000ms), on the old project directory (625 diagnostics including unavailable Roslyn/xUnit references), and on all seven candidate files before candidate build. These are not clean LSP results: this isolated worktree is not loaded with its project references in the shared language-server workspace. Typical exact messages are `The name 'Assert' does not exist in the current context`, `The type or namespace name 'Diagnostic' could not be found (are you missing a using directive or an assembly reference?)`, and `Predefined type 'System.Void' is not defined or imported`.

Candidate `lsp_diagnostics(filePath, severity: "error")` counts: `AnalyzerTestAssert.cs`: 4; `BlockedValueTaskAnalyzerTests.cs`: 130; `DiscardedOutcomeAnalyzerTests.cs`: 40; `IgnoredTryGetResultAnalyzerTests.cs`: 18; `SyncDisposeAnalyzerTests.cs`: 14; `UninitializedCarrierAnalyzerTests.cs`: 68; `CodeFixTests.cs`: 171. Build/compiler validation, not the unloaded LSP workspace, supplies the actual reference-resolved type check. The LSP limitation pre-dates these edits and is left for the parent workspace integration; no error is suppressed.

### Candidate wrong-discard-target witness

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer && dotnet build tests/FunnySharp.Analyzers.Tests/FunnySharp.Analyzers.Tests.csproj --no-restore && dotnet tests/FunnySharp.Analyzers.Tests/bin/Debug/net10.0/FunnySharp.Analyzers.Tests.dll -failSkips -failWarns -noColor -method "*.DiscardedOutcomeBatchFixChangesOnlySafeStatements" -method "*.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol"; r=$?; printf "EXIT_TEST=%s\n" "$r"; exit "$r"
```

```text
status: exited_1 exit_code: 1
  FunnySharp.Analyzers -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers\bin\Debug\netstandard2.0\FunnySharp.Analyzers.dll
  FunnySharp.Analyzers.CodeFixes -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers.CodeFixes\bin\Debug\netstandard2.0\FunnySharp.Analyzers.CodeFixes.dll
  FunnySharp -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp\bin\Debug\net10.0\FunnySharp.dll
  FunnySharp.AspNetCore -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.AspNetCore\bin\Debug\net10.0\FunnySharp.AspNetCore.dll
  FunnySharp.Analyzers.Tests -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\tests\FunnySharp.Analyzers.Tests\bin\Debug\net10.0\FunnySharp.Analyzers.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.01
xUnit.net v3 In-Process Runner v4.0.0+8bf043c053 (64-bit .NET 10.0.12)
  Discovering: FunnySharp.Analyzers.Tests
  Discovered:  FunnySharp.Analyzers.Tests
  Starting:    FunnySharp.Analyzers.Tests
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeBatchFixChangesOnlySafeStatements [FAIL]
      Assert.Single() Failure: The collection was empty
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(233,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeBatchFixChangesOnlySafeStatements()
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(members: "void Use() { int _ = 0; Find(); GC.KeepAlive(_); }") [FAIL]
      Assert.Empty() Failure: Collection was not empty
      Collection: [DocumentChangeAction { CreatedFromFactoryMethod = True, EquivalenceKey = "MakeDiscardedOutcomeExplicit", IsInlinable = False, NestedActions = [], Priority = Default, ··· }]
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(99,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(String members)
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(members: "void Use() { Option<int> _ = Option.Some(2); Find("···) [FAIL]
      Assert.Empty() Failure: Collection was not empty
      Collection: [DocumentChangeAction { CreatedFromFactoryMethod = True, EquivalenceKey = "MakeDiscardedOutcomeExplicit", IsInlinable = False, NestedActions = [], Priority = Default, ··· }]
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(99,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(String members)
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(members: "void Use(int _) { Find(); }") [FAIL]
      Assert.Empty() Failure: Collection was not empty
      Collection: [DocumentChangeAction { CreatedFromFactoryMethod = True, EquivalenceKey = "MakeDiscardedOutcomeExplicit", IsInlinable = False, NestedActions = [], Priority = Default, ··· }]
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(99,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(String members)
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(members: "void Use(Option<int> _) { Find(); }") [FAIL]
      Assert.Empty() Failure: Collection was not empty
      Collection: [DocumentChangeAction { CreatedFromFactoryMethod = True, EquivalenceKey = "MakeDiscardedOutcomeExplicit", IsInlinable = False, NestedActions = [], Priority = Default, ··· }]
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(99,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(String members)
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(members: "async Task UseAsync(Option<int> _) { await Task.Fr"···) [FAIL]
      Assert.Empty() Failure: Collection was not empty
      Collection: [DocumentChangeAction { CreatedFromFactoryMethod = True, EquivalenceKey = "MakeDiscardedOutcomeExplicit", IsInlinable = False, NestedActions = [], Priority = Default, ··· }]
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(99,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(String members)
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(members: "void Use() { Option<int> _ = Option.Some(2); Actio"···) [FAIL]
      Assert.Empty() Failure: Collection was not empty
      Collection: [DocumentChangeAction { CreatedFromFactoryMethod = True, EquivalenceKey = "MakeDiscardedOutcomeExplicit", IsInlinable = False, NestedActions = [], Priority = Default, ··· }]
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(99,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(String members)
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(members: "void Use() { Action<int> call = _ => { Find(); }; "···) [FAIL]
      Assert.Empty() Failure: Collection was not empty
      Collection: [DocumentChangeAction { CreatedFromFactoryMethod = True, EquivalenceKey = "MakeDiscardedOutcomeExplicit", IsInlinable = False, NestedActions = [], Priority = Default, ··· }]
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(99,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(String members)
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(members: "int _ = 0; void Use() { Find(); }") [FAIL]
      Assert.Empty() Failure: Collection was not empty
      Collection: [DocumentChangeAction { CreatedFromFactoryMethod = True, EquivalenceKey = "MakeDiscardedOutcomeExplicit", IsInlinable = False, NestedActions = [], Priority = Default, ··· }]
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(99,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(String members)
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(members: "Option<int> _ = Option.Some(2); void Use() { Find("···) [FAIL]
      Assert.Empty() Failure: Collection was not empty
      Collection: [DocumentChangeAction { CreatedFromFactoryMethod = True, EquivalenceKey = "MakeDiscardedOutcomeExplicit", IsInlinable = False, NestedActions = [], Priority = Default, ··· }]
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(99,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(String members)
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(members: "Option<int> _ { get; set; } = Option.Some(2); void"···) [FAIL]
      Assert.Empty() Failure: Collection was not empty
      Collection: [DocumentChangeAction { CreatedFromFactoryMethod = True, EquivalenceKey = "MakeDiscardedOutcomeExplicit", IsInlinable = False, NestedActions = [], Priority = Default, ··· }]
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(99,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(String members)
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(members: "void Use() { Find(); int _ = 0; GC.KeepAlive(_); }") [FAIL]
      Assert.Empty() Failure: Collection was not empty
      Collection: [DocumentChangeAction { CreatedFromFactoryMethod = True, EquivalenceKey = "MakeDiscardedOutcomeExplicit", IsInlinable = False, NestedActions = [], Priority = Default, ··· }]
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(99,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(String members)
        --- End of stack trace from previous location ---
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(members: "void Use() { Option<int> @_ = Option.Some(2); Find"···) [FAIL]
      Assert.Empty() Failure: Collection was not empty
      Collection: [DocumentChangeAction { CreatedFromFactoryMethod = True, EquivalenceKey = "MakeDiscardedOutcomeExplicit", IsInlinable = False, NestedActions = [], Priority = Default, ··· }]
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(99,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(String members)
        --- End of stack trace from previous location ---
  Finished:    FunnySharp.Analyzers.Tests (ID = '350466aa4480b7353f2ad47adb8e0ac50cd6d8f445b9bec96dbf0999d53a8532')
=== TEST EXECUTION SUMMARY ===
   FunnySharp.Analyzers.Tests  Total: 13, Errors: 0, Failed: 13, Skipped: 0, Not Run: 0, Time: 3.490s
EXIT_TEST=1
```

## Reproduction patches

The following are the canonical full-line patches for each independent fault. Apply one block with `apply_patch`, build and run the exact matching old/candidate command receipt, then apply its exact inverse before the next block. Old tests are the baseline at `141f9d0`; candidate tests are the seven-file patch delivered in this worktree. Initial exploratory patches omitted leading indentation; it was restored and full-line contexts were used for candidate replay. This neutral whitespace difference does not change the representative faults or compile/diagnostic observations. The whitespace contrast itself is the single linefeed-after-equals patch below.

### Five missing reports

```diff
*** Begin Patch
*** Update File: Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer/src/FunnySharp.Analyzers/UninitializedCarrierAnalyzer.cs
@@
-        context.ReportDiagnostic(Diagnostic.Create(
-            Descriptors.UninitializedCarrier,
-            context.Operation.Syntax.GetLocation(),
-            type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
+        _ = Diagnostic.Create(
+            Descriptors.UninitializedCarrier,
+            context.Operation.Syntax.GetLocation(),
+            type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
*** Update File: Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer/src/FunnySharp.Analyzers/DiscardedOutcomeAnalyzer.cs
@@
-        context.ReportDiagnostic(Diagnostic.Create(
-            Descriptors.DiscardedOutcome,
-            operation.Syntax.GetLocation(),
-            outcomeKind));
+        _ = Diagnostic.Create(
+            Descriptors.DiscardedOutcome,
+            operation.Syntax.GetLocation(),
+            outcomeKind);
*** Update File: Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer/src/FunnySharp.Analyzers/IgnoredTryGetResultAnalyzer.cs
@@
-            context.ReportDiagnostic(Diagnostic.Create(
-                Descriptors.IgnoredTryGetResult,
-                invocation.Syntax.GetLocation(),
-                method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
+            _ = Diagnostic.Create(
+                Descriptors.IgnoredTryGetResult,
+                invocation.Syntax.GetLocation(),
+                method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
*** Update File: Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer/src/FunnySharp.Analyzers/BlockedValueTaskAnalyzer.cs
@@
-        context.ReportDiagnostic(Diagnostic.Create(
-            Descriptors.BlockedValueTask,
-            context.Operation.Syntax.GetLocation(),
-            blockedMember));
+        _ = Diagnostic.Create(
+            Descriptors.BlockedValueTask,
+            context.Operation.Syntax.GetLocation(),
+            blockedMember);
*** Update File: Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer/src/FunnySharp.Analyzers/SyncDisposeAnalyzer.cs
@@
-        context.ReportDiagnostic(Diagnostic.Create(
-            Descriptors.SyncDisposeOfAsyncDisposable,
-            invocation.Syntax.GetLocation(),
-            resourceType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
+        _ = Diagnostic.Create(
+            Descriptors.SyncDisposeOfAsyncDisposable,
+            invocation.Syntax.GetLocation(),
+            resourceType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
*** End Patch
```

### Wrong discard target

```diff
*** Begin Patch
*** Update File: Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer/src/FunnySharp.Analyzers.CodeFixes/DiscardedOutcomeCodeFixProvider.cs
@@
-            if (semanticModel?.GetOperation(generatedAssignment, context.CancellationToken) is not
-                ISimpleAssignmentOperation { Target: IDiscardOperation })
+            if (semanticModel?.GetOperation(generatedAssignment, context.CancellationToken) is not
+                ISimpleAssignmentOperation)
*** End Patch
```

### Replaced RHS

```diff
*** Begin Patch
*** Update File: Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer/src/FunnySharp.Analyzers.CodeFixes/DiscardedOutcomeCodeFixProvider.cs
@@
-                statement.Expression.WithoutLeadingTrivia())
+                SyntaxFactory.ParseExpression("FunnySharp.Option.Some(2)"))
*** End Patch
```

### Removed await

```diff
*** Begin Patch
*** Update File: Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer/src/FunnySharp.Analyzers.CodeFixes/DiscardedOutcomeCodeFixProvider.cs
@@
-                statement.Expression.WithoutLeadingTrivia())
+                (statement.Expression is AwaitExpressionSyntax awaited
+                    ? awaited.Expression
+                    : statement.Expression).WithoutLeadingTrivia())
*** End Patch
```

### Descriptor messages only

```diff
*** Begin Patch
*** Update File: Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer/src/FunnySharp.Analyzers/Descriptors.cs
@@
-        messageFormat: "'{0}' has no valid default value: every member that reads a default {0} throws InvalidOperationException; create it with a factory instead",
+        messageFormat: "Review this FunnySharp usage",
*** Update File: Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer/src/FunnySharp.Analyzers/Descriptors.cs
@@
-        messageFormat: "This statement discards the {0} produced by the call; assign it, return it, await it, or discard it explicitly with '_ ='",
+        messageFormat: "Review this FunnySharp usage",
*** Update File: Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer/src/FunnySharp.Analyzers/Descriptors.cs
@@
-        messageFormat: "The Boolean result of '{0}' is ignored, so the out value may be default; use the result (for example in an if statement), declare the out argument as a discard, or discard the call explicitly",
+        messageFormat: "Review this FunnySharp usage",
*** Update File: Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer/src/FunnySharp.Analyzers/Descriptors.cs
@@
-        messageFormat: "Do not block on this ValueTask: it follows the single-consumption rule and may be backed by pooled resources, so await it once instead of accessing {0}",
+        messageFormat: "Review this FunnySharp usage",
*** Update File: Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer/src/FunnySharp.Analyzers/Descriptors.cs
@@
-        messageFormat: "'{0}' implements IAsyncDisposable, but Using runs only the synchronous Dispose; use UsingAsync so DisposeAsync runs",
+        messageFormat: "Review this FunnySharp usage",
*** End Patch
```

### Whitespace only

```diff
*** Begin Patch
*** Update File: Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer/src/FunnySharp.Analyzers.CodeFixes/DiscardedOutcomeCodeFixProvider.cs
@@
-                    .WithTrailingTrivia(SyntaxFactory.Space))
+                    .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed))
*** End Patch
```

### Candidate replaced-RHS witness

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer && dotnet build tests/FunnySharp.Analyzers.Tests/FunnySharp.Analyzers.Tests.csproj --no-restore && dotnet tests/FunnySharp.Analyzers.Tests/bin/Debug/net10.0/FunnySharp.Analyzers.Tests.dll -failSkips -failWarns -noColor -method "*.DiscardedOutcomeFixMakesTheDiscardExplicit"; r=$?; printf "EXIT_TEST=%s\n" "$r"; exit "$r"
```

```text
status: exited_1 exit_code: 1
  FunnySharp.Analyzers -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers\bin\Debug\netstandard2.0\FunnySharp.Analyzers.dll
  FunnySharp.Analyzers.CodeFixes -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers.CodeFixes\bin\Debug\netstandard2.0\FunnySharp.Analyzers.CodeFixes.dll
  FunnySharp -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp\bin\Debug\net10.0\FunnySharp.dll
  FunnySharp.AspNetCore -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.AspNetCore\bin\Debug\net10.0\FunnySharp.AspNetCore.dll
  FunnySharp.Analyzers.Tests -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\tests\FunnySharp.Analyzers.Tests\bin\Debug\net10.0\FunnySharp.Analyzers.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.58
xUnit.net v3 In-Process Runner v4.0.0+8bf043c053 (64-bit .NET 10.0.12)
  Discovering: FunnySharp.Analyzers.Tests
  Discovered:  FunnySharp.Analyzers.Tests
  Starting:    FunnySharp.Analyzers.Tests
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixMakesTheDiscardExplicit [FAIL]
      Assert.Equal() Failure: Strings differ
                 ↓ (pos 0)
      Expected: "Find"
      Actual:   "Some"
                 ↑ (pos 0)
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(33,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixMakesTheDiscardExplicit()
        --- End of stack trace from previous location ---
  Finished:    FunnySharp.Analyzers.Tests (ID = '350466aa4480b7353f2ad47adb8e0ac50cd6d8f445b9bec96dbf0999d53a8532')
=== TEST EXECUTION SUMMARY ===
   FunnySharp.Analyzers.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 3.257s
EXIT_TEST=1
```

### Candidate removed-await witness

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer && dotnet build tests/FunnySharp.Analyzers.Tests/FunnySharp.Analyzers.Tests.csproj --no-restore && dotnet tests/FunnySharp.Analyzers.Tests/bin/Debug/net10.0/FunnySharp.Analyzers.Tests.dll -failSkips -failWarns -noColor -method "*.DiscardedOutcomeFixPreservesAwaitedStatements"; r=$?; printf "EXIT_TEST=%s\n" "$r"; exit "$r"
```

```text
status: exited_1 exit_code: 1
  FunnySharp.Analyzers -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers\bin\Debug\netstandard2.0\FunnySharp.Analyzers.dll
  FunnySharp.Analyzers.CodeFixes -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers.CodeFixes\bin\Debug\netstandard2.0\FunnySharp.Analyzers.CodeFixes.dll
  FunnySharp -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp\bin\Debug\net10.0\FunnySharp.dll
  FunnySharp.AspNetCore -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.AspNetCore\bin\Debug\net10.0\FunnySharp.AspNetCore.dll
  FunnySharp.Analyzers.Tests -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\tests\FunnySharp.Analyzers.Tests\bin\Debug\net10.0\FunnySharp.Analyzers.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.37
xUnit.net v3 In-Process Runner v4.0.0+8bf043c053 (64-bit .NET 10.0.12)
  Discovering: FunnySharp.Analyzers.Tests
  Discovered:  FunnySharp.Analyzers.Tests
  Starting:    FunnySharp.Analyzers.Tests
    FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixPreservesAwaitedStatements [FAIL]
      Assert.IsAssignableFrom() Failure: Value is an incompatible type
      Expected: typeof(Microsoft.CodeAnalysis.Operations.IAwaitOperation)
      Actual:   typeof(Microsoft.CodeAnalysis.Operations.InvocationOperation)
      Stack Trace:
        tests\FunnySharp.Analyzers.Tests\CodeFixTests.cs(59,0): at FunnySharp.Analyzers.Tests.CodeFixTests.DiscardedOutcomeFixPreservesAwaitedStatements()
        --- End of stack trace from previous location ---
  Finished:    FunnySharp.Analyzers.Tests (ID = '350466aa4480b7353f2ad47adb8e0ac50cd6d8f445b9bec96dbf0999d53a8532')
=== TEST EXECUTION SUMMARY ===
   FunnySharp.Analyzers.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 2.695s
EXIT_TEST=1
```

Two recovery details are recorded rather than counted as new detector evidence: a substring-context patch stripped leading indentation, and an attempted whitespace patch partially applied only the operator-trivia hunk. The indentation was restored, later patches use full-line contexts, and the executed old/candidate whitespace contrast changes only the trivia after `=`. Neither recovery introduced a permanent production diff.

### Candidate descriptor-message-only contrast

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer && dotnet build tests/FunnySharp.Analyzers.Tests/FunnySharp.Analyzers.Tests.csproj --no-restore && dotnet tests/FunnySharp.Analyzers.Tests/bin/Debug/net10.0/FunnySharp.Analyzers.Tests.dll -failSkips -failWarns -noColor -method "*.DefaultResultExpressionIsReported" -method "*.DiscardedUserCarrierCallIsReported" -method "*.UnawaitedValueTaskFromFunnySharpMemberIsReported" -method "*.AwaitedTaskOfOutcomeIsReported" -method "*.AwaitedStoredTaskOfOutcomeIsReported" -method "*.AwaitedConfiguredTaskOfOutcomeIsReported" -method "*.AwaitedTaskOfNonOutcomeFromFunnySharpMemberIsReported" -method "*.IgnoredOptionTryGetValueIsReported" -method "*.ResultOnDirectValueTaskIsReported" -method "*.GetAwaiterGetResultOnDirectValueTaskIsReported" -method "*.BothInterfacesIsReported"; r=$?; printf "EXIT_TEST=%s\n" "$r"; exit "$r"
```

```text
status: completed exit_code: 0
  FunnySharp.Analyzers -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers\bin\Debug\netstandard2.0\FunnySharp.Analyzers.dll
  FunnySharp.Analyzers.CodeFixes -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers.CodeFixes\bin\Debug\netstandard2.0\FunnySharp.Analyzers.CodeFixes.dll
  FunnySharp -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp\bin\Debug\net10.0\FunnySharp.dll
  FunnySharp.AspNetCore -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.AspNetCore\bin\Debug\net10.0\FunnySharp.AspNetCore.dll
  FunnySharp.Analyzers.Tests -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\tests\FunnySharp.Analyzers.Tests\bin\Debug\net10.0\FunnySharp.Analyzers.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.32
xUnit.net v3 In-Process Runner v4.0.0+8bf043c053 (64-bit .NET 10.0.12)
  Discovering: FunnySharp.Analyzers.Tests
  Discovered:  FunnySharp.Analyzers.Tests
  Starting:    FunnySharp.Analyzers.Tests
  Finished:    FunnySharp.Analyzers.Tests (ID = '350466aa4480b7353f2ad47adb8e0ac50cd6d8f445b9bec96dbf0999d53a8532')
=== TEST EXECUTION SUMMARY ===
   FunnySharp.Analyzers.Tests  Total: 11, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 1.958s
EXIT_TEST=0
```

### Candidate whitespace-only contrast and codefix partitions

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer && dotnet build tests/FunnySharp.Analyzers.Tests/FunnySharp.Analyzers.Tests.csproj --no-restore && dotnet tests/FunnySharp.Analyzers.Tests/bin/Debug/net10.0/FunnySharp.Analyzers.Tests.dll -failSkips -failWarns -noColor -method "*.DiscardedOutcomeFixMakesTheDiscardExplicit" -method "*.DiscardedOutcomeFixPreservesAwaitedStatements" && dotnet tests/FunnySharp.Analyzers.Tests/bin/Debug/net10.0/FunnySharp.Analyzers.Tests.dll -failSkips -failWarns -noColor -class "FunnySharp.Analyzers.Tests.CodeFixTests"; r=$?; printf "EXIT_TEST=%s\n" "$r"; exit "$r"
```

```text
status: completed exit_code: 0
  FunnySharp.Analyzers -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers\bin\Debug\netstandard2.0\FunnySharp.Analyzers.dll
  FunnySharp.Analyzers.CodeFixes -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers.CodeFixes\bin\Debug\netstandard2.0\FunnySharp.Analyzers.CodeFixes.dll
  FunnySharp -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp\bin\Debug\net10.0\FunnySharp.dll
  FunnySharp.AspNetCore -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.AspNetCore\bin\Debug\net10.0\FunnySharp.AspNetCore.dll
  FunnySharp.Analyzers.Tests -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\tests\FunnySharp.Analyzers.Tests\bin\Debug\net10.0\FunnySharp.Analyzers.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.45
xUnit.net v3 In-Process Runner v4.0.0+8bf043c053 (64-bit .NET 10.0.12)
  Discovering: FunnySharp.Analyzers.Tests
  Discovered:  FunnySharp.Analyzers.Tests
  Starting:    FunnySharp.Analyzers.Tests
  Finished:    FunnySharp.Analyzers.Tests (ID = '350466aa4480b7353f2ad47adb8e0ac50cd6d8f445b9bec96dbf0999d53a8532')
=== TEST EXECUTION SUMMARY ===
   FunnySharp.Analyzers.Tests  Total: 2, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 3.104s
xUnit.net v3 In-Process Runner v4.0.0+8bf043c053 (64-bit .NET 10.0.12)
  Discovering: FunnySharp.Analyzers.Tests
  Discovered:  FunnySharp.Analyzers.Tests
  Starting:    FunnySharp.Analyzers.Tests
  Finished:    FunnySharp.Analyzers.Tests (ID = '350466aa4480b7353f2ad47adb8e0ac50cd6d8f445b9bec96dbf0999d53a8532')
=== TEST EXECUTION SUMMARY ===
   FunnySharp.Analyzers.Tests  Total: 25, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 4.138s
EXIT_TEST=0
```

## Restoration evidence

All temporary source replacements have been reversed. The tracked-production check and whitespace check both exit 0:

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer && git diff --exit-code -- src && git diff --check && git status --short
```

```text
status: completed exit_code: 0
 M tests/FunnySharp.Analyzers.Tests/AnalyzerTestAssert.cs
 M tests/FunnySharp.Analyzers.Tests/BlockedValueTaskAnalyzerTests.cs
 M tests/FunnySharp.Analyzers.Tests/CodeFixTests.cs
 M tests/FunnySharp.Analyzers.Tests/DiscardedOutcomeAnalyzerTests.cs
 M tests/FunnySharp.Analyzers.Tests/IgnoredTryGetResultAnalyzerTests.cs
 M tests/FunnySharp.Analyzers.Tests/SyncDisposeAnalyzerTests.cs
 M tests/FunnySharp.Analyzers.Tests/UninitializedCarrierAnalyzerTests.cs
?? docs/audits/test-ablation/experiments/analyzer-oracles.md
```

The seven source files were also re-inspected against the initially read content. The authoritative tracked-source comparison is the empty `git diff --exit-code -- src`; no production file remains modified. Only the seven authorized test/helper files and this report appear in the worktree status. No commits or remote writes were made.

### Restored final locked build and full analyzer suite

```bash
cd Q:/repos/funnysharp/.omo/worktrees/test-ablation-analyzer && dotnet restore tests/FunnySharp.Analyzers.Tests/FunnySharp.Analyzers.Tests.csproj --locked-mode && dotnet build tests/FunnySharp.Analyzers.Tests/FunnySharp.Analyzers.Tests.csproj --no-restore && dotnet tests/FunnySharp.Analyzers.Tests/bin/Debug/net10.0/FunnySharp.Analyzers.Tests.dll -failSkips -failWarns -noColor; r=$?; printf "EXIT_TEST=%s\n" "$r"; exit "$r"
```

```text
status: completed exit_code: 0
  Determining projects to restore...
  All projects are up-to-date for restore.
  FunnySharp.Analyzers -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers\bin\Debug\netstandard2.0\FunnySharp.Analyzers.dll
  FunnySharp.Analyzers.CodeFixes -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.Analyzers.CodeFixes\bin\Debug\netstandard2.0\FunnySharp.Analyzers.CodeFixes.dll
  FunnySharp -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp\bin\Debug\net10.0\FunnySharp.dll
  FunnySharp.AspNetCore -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\src\FunnySharp.AspNetCore\bin\Debug\net10.0\FunnySharp.AspNetCore.dll
  FunnySharp.Analyzers.Tests -> Q:\repos\funnysharp\.omo\worktrees\test-ablation-analyzer\tests\FunnySharp.Analyzers.Tests\bin\Debug\net10.0\FunnySharp.Analyzers.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.03
xUnit.net v3 In-Process Runner v4.0.0+8bf043c053 (64-bit .NET 10.0.12)
  Discovering: FunnySharp.Analyzers.Tests
  Discovered:  FunnySharp.Analyzers.Tests
  Starting:    FunnySharp.Analyzers.Tests
  Finished:    FunnySharp.Analyzers.Tests (ID = '350466aa4480b7353f2ad47adb8e0ac50cd6d8f445b9bec96dbf0999d53a8532')
=== TEST EXECUTION SUMMARY ===
   FunnySharp.Analyzers.Tests  Total: 168, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 3.766s
EXIT_TEST=0
```

## Completion

All scoped edits, old and candidate observations, cleanup, CSharp LSP requests, locked restore/build and the final 168-case suite have actually executed. There are no live experiment monitors or outstanding mutant/restoration steps. Repository-wide integration and final full gates are intentionally owned by the parent, not substituted by this analyzer-only evidence.

