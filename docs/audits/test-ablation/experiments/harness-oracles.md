# Harness oracle ablation experiment

## Executed matrix summary

The scoped test patch and executed proof are complete. The final restored
full suite passed 745/745 Windows cases, with Errors 0, Failed 0, Skipped 0,
Not Run 0, test exit 0. Its locked rebuild exited 0 with 0 warnings/errors
(16.02 seconds); the suite took 18.294 seconds. Permanent source
diff: three test files, 55 insertions and 41 deletions, plus this evidence
report. No test method or data partition was deleted. The 43-name census stays
explicitly source-only; no compiled C# oracle execution is claimed.

Numbers in this table are `total / failed / test exit`. Each retained fault
compiled successfully with 0 warnings and 0 errors before execution. All
filtered invocations use `-failSkips -failWarns -noLogo -noColor` and report
Errors 0, Skipped 0, Not Run 0. The exact commands, mutations, monitor/session
IDs and decisive output are recorded in the sections that follow.

| Fault/control | Old | Final candidate | Independent owner / interpretation |
| --- | --- | --- | --- |
| Enforcement, branch target, missing context, app binding, bypass actor, exclusion, unrelated ref, duplicate status rule (each isolated) | 1/1/1 each | 1/1/1 each | Independently authored valid Ruleset passes 1/0/0 in both versions for every fault. Missing context requires bypassing both existing defenses. |
| Strict policy: missing direct, false direct, missing through checkRuleset | 4/3/1 | 4/3/1 | Enabled strict policy remains the passing class partition; full valid Ruleset also passes. |
| Enforcement CLI exit | 1/1/1 | 1/1/1 | Expected 1, observed 0 under the same enforcement fault. |
| BOM partial skip 3 -> 1 | 1/0/0 | 1/1/1 | Existing LocTests core owner fails 1/1/1 in both versions, LOC 1 -> 2. |
| BOM retained, skip 3 -> 0, final blank-first-line fixture | 1/0/0 | 1/1/1 | Existing leading-comment core owner remains false-green 1/0/0; Windows culture-sensitive prefix comparison explains it. This limitation is not hidden. |
| Block stripping bypass | 1/0/0 | 1/1/1 | Existing LocTests core owner fails 1/1/1 in both versions, LOC 2 -> 4. Candidate wrapper observes 1 -> 3. |
| Fingerprint ignores file bytes | 1/0/0 | 1/1/1 | Controlled hello -> HELLO change has identical paths/count. Final negative-control version was revalidated. |
| Fingerprint freezes emitted fileCount at 2 | 1/0/0 | 1/1/1 | Added tracked file requires 3; final candidate observes Some(2) and fails. |
| Left schema, right schema, left algorithm, right algorithm, count equality (each isolated) | 1/0/0 each | 1/1/1 each | Valid peer plus malformed/different peer is compared in both directions. |
| Digest equality always true | 1/1/1 | 1/1/1 | Original digest distinction preserved. |
| Digest equality case-sensitive | 1/1/1 | 1/1/1 | Original abc/ABC machine equivalence preserved. |
| Deterministic call-state-dependent identity | 1/1/1 | 1/1/1 | Unchanged-input negative control preserves meaningful old determinism detection; no random timing/wait is used. |
| Diagnostic reword-only valid change | 19/12/1 | 19/0/0 | Logic, status, context names, app IDs and refs remain unchanged; valid Ruleset passes independently. |

Restoration before the full-suite run: full file-content comparisons against
the initial snapshots returned true for Ruleset.fs, Loc.fs, Evaluation.fs,
ReleaseVerifySource.fs and ReleaseVerify.fs. `git diff --exit-code --
eng/harness` and `git diff --check` exited 0. The worktree HEAD remains
`141f9d04adecc35a3851c6ca1d6812d6aa9f32ba`; only the three allowed test files and
this report appear in `git status --short`. Generated old/new binaries are
retained under ignored `artifacts/test-ablation/`; they are not source patches.

The unmodified old fingerprint fixture performs its existing commit inside a
disposable scratch Git repository. The candidate removes that unnecessary
fixture commit/configuration. No commit was made to the assigned worktree,
and no merge, remote write, dependency or framework was added.

## Scope and evidence standard

Parent commit gate additionally ran the complete repository formatter,
`dotnet fsi build.fsx -- -p format`: exit 0, 23.374 s. FSharp compiler diagnostics,
not unavailable fsautocomplete output, are the test-source authority.

Worktree: `Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles`, branch
`ulw/test-ablation-harness-oracles`, starting commit
`141f9d04adecc35a3851c6ca1d6812d6aa9f32ba`. The initial tree was clean.
Permanent edits are limited to `RulesetTests.fs`, `EvaluationTests.fs`,
`ReleaseVerifyTests.fs`, and this report. Temporary implementation mutants are
serial, compile before their test outcomes count, and are restored with
`apply_patch`. No commits, external writes, new dependencies, or frozen source
edits are part of this experiment.

Ruleset rejection is a typed `HarnessError` with exit code 1. Context names,
integration IDs, and target refs identify machine joins; English diagnostic
sentences are not the assertions under test. The source comments describe
PowerShell wording parity, but the task explicitly authorizes removing these
wording assertions, not changing production wording.

`Evaluation.countLoc` delegates to `Loc.countLoc`. BOM followed by code and an
inline block surrounded by code are weak controls: retaining the BOM or block
still counts one line. The corrected fixtures must change the result under
those faults, and the existing core LOC tests must independently reject them.

Source fingerprints are alteration detectors, not semantic or provenance
proof. This experiment compares emitted identity before and after controlled
file-byte changes; it does not recompute expected SHA256 or infer truth from
digest length or repeatability. Schema version, algorithm, count, and
case-insensitive digest equality remain the comparison helper's existing
machine contract.

The five-group, 43-name source-only oracle census is retained. No runner reads
those names as a machine contract: `Evaluation.fs` copies the actual oracle
files and uses expected test totals and exit/status evidence. The census can
detect a declaration rename/removal, but cannot establish executable identity
or semantics. Removal is not justified without independent compiled witnesses
for every group; retaining it does not upgrade its evidence claim.

## Execution log

The initial attempted command was:

```text
dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -c Debug --locked-mode -v minimal
```

It exited 1 with `MSB1001: Unknown switch`, `Switch: --locked-mode`. This CLI
spelling is a restore switch, not a `dotnet build` switch. All subsequent build
commands use `-p:RestoreLockedMode=true`. This failure is not mutant rejection.

F# LSP diagnostics could not run: `fsautocomplete` is not installed. No package
was added; compiler diagnostics are the validation surface.

Baseline compilation used `-p:RestoreLockedMode=true` and
`-p:OutputPath=artifacts/test-ablation/old/` (absolute path in the executed
command). It exited 0: `Build succeeded.`, `0 Warning(s)`, `0 Error(s)`, elapsed
21.75 seconds. The baseline test executable is retained separately from the
candidate under `artifacts/test-ablation/new/`.

The first candidate compilation exited 1 with FS0041 at ReleaseVerifyTests.fs
for an ambiguous `Assert.NotEqual` overload; adding `<string>` fixed the
overload. This is an implementation correction, not mutant evidence.

Runner probing established that `-help` is not valid (exit 3), whereas `--help`
prints the supported `-class`, `-method`, `-failSkips`, and `-failWarns` switches
(help exit 2). A combined class/method control command was discarded because
different simple filter types intersect, rather than unite. The first
PowerShell wrapper also failed before execution because the outer shell
expanded `$LASTEXITCODE`. Subsequent wrappers use UTF-16LE `-EncodedCommand`;
the decoded exact command is recorded with each completed experiment. None
of these setup failures counts as fault rejection.

Results below are populated only after execution.

### Unmutated controls

The corrected candidate compilation exited 0, with 0 warnings/errors (9.65 s).
The old and new executables were run independently, not using intersecting
class/method filter types. All runs used `-failSkips -failWarns -noLogo -noColor`.

| Filter | Old total/failed/exit | New total/failed/exit |
| --- | --- | --- |
| `-class FunnySharp.Harness.Tests.RulesetTests+*` | 19/0/0 | 19/0/0 |
| `-class FunnySharp.Harness.Tests.EvaluationTests+CountLocTests -class FunnySharp.Harness.Tests.LocTests+CountLocTests` | 8/0/0 | 8/0/0 |
| `-method *SourceFingerprint*` | 2/0/0 | 2/0/0 |

All six summaries reported Errors 0, Skipped 0, Not Run 0. Each command was
`dotnet <old-or-new-output>/FunnySharp.Harness.Tests.dll <filter> <switches>`
from the worktree root. Separate assemblies retain the actual old/new fixtures;
only the harness dependency is rebuilt for each following mutant.

### R-enforcement

Monitor `mon_C5GJFTXHW0T4EV3M`, session `bash_9`.

```diff
-    if not (String.Equals(enforcement, "active", StringComparison.OrdinalIgnoreCase)) then
+    if false then
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build eng/harness/FunnySharp.Harness.fsproj -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; & dotnet build eng/harness/FunnySharp.Harness.fsproj -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_NonActiveEnforcement_Rejects'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_NonActiveEnforcement_Rejects'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE);
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:09.18
BUILD EXIT=0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:01.41
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.262s
OLD-VALID EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.254s
NEW-VALID EXIT=0
    FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_NonActiveEnforcement_Rejects [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "Ruleset 42 is not active."
      Actual:   ""
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.265s
OLD-FAULT EXIT=1
    FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_NonActiveEnforcement_Rejects [FAIL]
      System.Exception : Expected a rejected ruleset.
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.252s
NEW-FAULT EXIT=1
```

### R-enforcement-cli

Monitor `mon_ENWTR3Q8XA620WJY`, session `bash_11`.



Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; Write-Output 'OLD-CLI'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+CommandLineTests.VerifyGitHubRuleset_NonActiveRuleset_Rejects'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-CLI EXIT=' + $LASTEXITCODE); Write-Output 'NEW-CLI'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+CommandLineTests.VerifyGitHubRuleset_NonActiveRuleset_Rejects'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-CLI EXIT=' + $LASTEXITCODE);
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
    FunnySharp.Harness.Tests.RulesetTests+CommandLineTests.VerifyGitHubRuleset_NonActiveRuleset_Rejects [FAIL]
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.305s
OLD-CLI EXIT=1
    FunnySharp.Harness.Tests.RulesetTests+CommandLineTests.VerifyGitHubRuleset_NonActiveRuleset_Rejects [FAIL]
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.432s
NEW-CLI EXIT=1
```

### R-target

Monitor `mon_RSXA7Z50YZ55M3MF`, session `bash_12`.

```diff
-        if not (String.Equals(target, "branch", StringComparison.OrdinalIgnoreCase)) then
+        if false then
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build eng/harness/FunnySharp.Harness.fsproj -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; & dotnet build eng/harness/FunnySharp.Harness.fsproj -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_NonBranchTarget_Rejects'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_NonBranchTarget_Rejects'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE);
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:09.16
BUILD EXIT=0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:02.12
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.355s
OLD-VALID EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.328s
NEW-VALID EXIT=0
    FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_NonBranchTarget_Rejects [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "Ruleset 42 does not target branches."
      Actual:   ""
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.358s
OLD-FAULT EXIT=1
    FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_NonBranchTarget_Rejects [FAIL]
      System.Exception : Expected a rejected ruleset.
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.339s
NEW-FAULT EXIT=1
```

### R-missing-context

Monitor `mon_YPFA48NW81ZFJ5EC`, session `bash_14`.

```diff
-                        if not missing.IsEmpty then
+                        if false then
```
```diff
-                                    | [ single ] when int64Member "integration_id" single = Some expectedIntegrationId ->
+                                    | [] -> Ok(context, expectedIntegrationId)
+                                    | [ single ] when int64Member "integration_id" single = Some expectedIntegrationId ->
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build eng/harness/FunnySharp.Harness.fsproj -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; & dotnet build eng/harness/FunnySharp.Harness.fsproj -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_MissingRequiredContext_Rejects'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_MissingRequiredContext_Rejects'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE);
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:12.25
BUILD EXIT=0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:02.25
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.430s
OLD-VALID EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.561s
NEW-VALID EXIT=0
    FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_MissingRequiredContext_Rejects [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "Ruleset 42 is missing required contexts: release /"···
      Actual:   ""
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.445s
OLD-FAULT EXIT=1
    FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_MissingRequiredContext_Rejects [FAIL]
      System.Exception : Expected a rejected ruleset.
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.284s
NEW-FAULT EXIT=1
```

### R-app-binding

Monitor `mon_Z6YBV64WNFKP8WPG`, session `bash_16`.

```diff
-                                    | [ single ] when int64Member "integration_id" single = Some expectedIntegrationId ->
+                                    | [ single ] ->
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build eng/harness/FunnySharp.Harness.fsproj -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; & dotnet build eng/harness/FunnySharp.Harness.fsproj -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_WrongIntegrationId_Rejects'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_WrongIntegrationId_Rejects'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE);
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:08.97
BUILD EXIT=0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:01.41
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.266s
OLD-VALID EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.276s
NEW-VALID EXIT=0
    FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_WrongIntegrationId_Rejects [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "Ruleset 42 must bind required context 'release / w"···
      Actual:   ""
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.269s
OLD-FAULT EXIT=1
    FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_WrongIntegrationId_Rejects [FAIL]
      System.Exception : Expected a rejected ruleset.
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.249s
NEW-FAULT EXIT=1
```

### R-bypass

Monitor `mon_KHZZRV5HZKD13SR1`, session `bash_18`.

```diff
-                                if bypassCount <> 0 then
+                                if false then
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build eng/harness/FunnySharp.Harness.fsproj -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; & dotnet build eng/harness/FunnySharp.Harness.fsproj -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_BypassActor_Rejects'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_BypassActor_Rejects'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE);
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:09.57
BUILD EXIT=0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:01.42
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.285s
OLD-VALID EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.356s
NEW-VALID EXIT=0
    FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_BypassActor_Rejects [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "Ruleset 42 contains bypass actors and cannot suppo"···
      Actual:   ""
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.392s
OLD-FAULT EXIT=1
    FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_BypassActor_Rejects [FAIL]
      System.Exception : Expected a rejected ruleset.
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.293s
NEW-FAULT EXIT=1
```

### R-exclusion

Monitor `mon_8T31BKBMXRQCKRCA`, session `bash_20`.

```diff
-            elif not excludedRefs.IsEmpty then
+            elif false then
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build eng/harness/FunnySharp.Harness.fsproj -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; & dotnet build eng/harness/FunnySharp.Harness.fsproj -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ExcludedRef_Rejects'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ExcludedRef_Rejects'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE);
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:09.27
BUILD EXIT=0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:02.22
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.312s
OLD-VALID EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.500s
NEW-VALID EXIT=0
    FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ExcludedRef_Rejects [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "Ruleset 42 contains branch exclusions and cannot p"···
      Actual:   ""
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.332s
OLD-FAULT EXIT=1
    FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ExcludedRef_Rejects [FAIL]
      System.Exception : Expected a rejected ruleset.
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.261s
NEW-FAULT EXIT=1
```

### R-ref

Monitor `mon_559854ZFXKQ07Z5P`, session `bash_22`.

```diff
-            if not includedTarget then
+            if false then
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build eng/harness/FunnySharp.Harness.fsproj -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; & dotnet build eng/harness/FunnySharp.Harness.fsproj -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_TargetRefNotIncluded_Rejects'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_TargetRefNotIncluded_Rejects'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE);
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:14.20
BUILD EXIT=0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:01.95
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.272s
OLD-VALID EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.268s
NEW-VALID EXIT=0
    FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_TargetRefNotIncluded_Rejects [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "Ruleset 42 does not explicitly include 'refs/heads"···
      Actual:   ""
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.262s
OLD-FAULT EXIT=1
    FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_TargetRefNotIncluded_Rejects [FAIL]
      System.Exception : Expected a rejected ruleset.
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.258s
NEW-FAULT EXIT=1
```

### R-duplicate

Monitor `mon_NZ1QFR68VK8AJ3PN`, session `bash_24`.

```diff
-                if statusRules.Length <> 1 then
+                if statusRules.IsEmpty then
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build eng/harness/FunnySharp.Harness.fsproj -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; & dotnet build eng/harness/FunnySharp.Harness.fsproj -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_TwoStatusRules_Rejects'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_TwoStatusRules_Rejects'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE);
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:08.88
BUILD EXIT=0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:01.92
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.358s
OLD-VALID EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.508s
NEW-VALID EXIT=0
    FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_TwoStatusRules_Rejects [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "Ruleset 42 must contain exactly one required_statu"···
      Actual:   ""
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.318s
OLD-FAULT EXIT=1
    FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_TwoStatusRules_Rejects [FAIL]
      System.Exception : Expected a rejected ruleset.
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.252s
NEW-FAULT EXIT=1
```

### R-strict

Monitor `mon_623AXCK3QJBHF6H9`, session `bash_25`.

```diff
-    if strict then
+    if true then
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build eng/harness/FunnySharp.Harness.fsproj -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; & dotnet build eng/harness/FunnySharp.Harness.fsproj -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -class 'FunnySharp.Harness.Tests.RulesetTests+StrictPolicyTests'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -class 'FunnySharp.Harness.Tests.RulesetTests+StrictPolicyTests'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE);
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:09.91
BUILD EXIT=0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:01.73
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.304s
OLD-VALID EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.274s
NEW-VALID EXIT=0
    FunnySharp.Harness.Tests.RulesetTests+StrictPolicyTests.VerifyGitHubRuleset_DisabledStrictPolicy_Rejects [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
    FunnySharp.Harness.Tests.RulesetTests+StrictPolicyTests.AssertStrictRequiredStatusChecksPolicy_Missing_Rejects [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
    FunnySharp.Harness.Tests.RulesetTests+StrictPolicyTests.VerifyGitHubRuleset_MissingStrictPolicy_Rejects [FAIL]
      Assert.Contains() Failure: Sub-string not found
   FunnySharp.Harness.Tests  Total: 4, Errors: 0, Failed: 3, Skipped: 0, Not Run: 0, Time: 0.429s
OLD-FAULT EXIT=1
    FunnySharp.Harness.Tests.RulesetTests+StrictPolicyTests.VerifyGitHubRuleset_DisabledStrictPolicy_Rejects [FAIL]
      System.Exception : Expected a rejected ruleset.
    FunnySharp.Harness.Tests.RulesetTests+StrictPolicyTests.AssertStrictRequiredStatusChecksPolicy_Missing_Rejects [FAIL]
      System.Exception : Expected a rejected ruleset.
    FunnySharp.Harness.Tests.RulesetTests+StrictPolicyTests.VerifyGitHubRuleset_MissingStrictPolicy_Rejects [FAIL]
      System.Exception : Expected a rejected ruleset.
   FunnySharp.Harness.Tests  Total: 4, Errors: 0, Failed: 3, Skipped: 0, Not Run: 0, Time: 0.313s
NEW-FAULT EXIT=1
```

### R-prose-valid

Monitor `mon_XX1K474HCG6SSMVZ`, session `bash_26`.

```diff
-        failError "Required status checks must require branches to be up to date before merging."
+        failError "Strict status policy is required."
```
```diff
-        failError (sprintf "Ruleset %d is not active." rulesetId)
+        failError (sprintf "Ruleset %d requires active enforcement." rulesetId)
```
```diff
-            failError (sprintf "Ruleset %d does not target branches." rulesetId)
+            failError (sprintf "Ruleset %d requires a branch target." rulesetId)
```
```diff
-                    sprintf "Ruleset %d does not explicitly include '%s' or the default branch." rulesetId targetRef
+                    sprintf "Ruleset %d must cover '%s'." rulesetId targetRef
```
```diff
-                        "Ruleset %d contains branch exclusions and cannot prove fail-closed default-branch coverage."
+                        "Ruleset %d must have no excluded refs."
```
```diff
-                        sprintf "Ruleset %d must contain exactly one required_status_checks rule." rulesetId
+                        sprintf "Ruleset %d requires one status rule." rulesetId
```
```diff
-                                    "Ruleset %d is missing required contexts: %s."
+                                    "Ruleset %d requires contexts: %s."
```
```diff
-                                                "Ruleset %d must bind required context '%s' to GitHub App integration %d."
+                                                "Ruleset %d requires '%s' from app %d."
```
```diff
-                                            "Ruleset %d contains bypass actors and cannot support this candidate's PASS verdict."
+                                            "Ruleset %d must have no bypass actors."
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build eng/harness/FunnySharp.Harness.fsproj -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; & dotnet build eng/harness/FunnySharp.Harness.fsproj -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -class 'FunnySharp.Harness.Tests.RulesetTests+*'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -class 'FunnySharp.Harness.Tests.RulesetTests+*'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE);
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
   FunnySharp.Harness.Tests  Total: 19, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.361s
NEW-FAULT EXIT=0
```

### L-bom

Monitor `mon_8BKBB8DNXBRZ3B8C`, session `bash_29`.

```diff
-            3
-        else
-            0
+            0
+        else
+            0
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build eng/harness/FunnySharp.Harness.fsproj -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; & dotnet build eng/harness/FunnySharp.Harness.fsproj -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.EvaluationTests+CountLocTests.StripsAByteOrderMark'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.EvaluationTests+CountLocTests.StripsAByteOrderMark'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE);
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:11.13
BUILD EXIT=0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:01.55
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.263s
OLD-VALID EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.260s
NEW-VALID EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.215s
OLD-FAULT EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.210s
NEW-FAULT EXIT=0
```

### L-block

Monitor `mon_FSQGC35Z53FZYHGA`, session `bash_31`.

```diff
-    let withoutBlocks = blockCommentPattern.Replace(text, "")
+    let withoutBlocks = text
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build eng/harness/FunnySharp.Harness.fsproj -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; & dotnet build eng/harness/FunnySharp.Harness.fsproj -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.EvaluationTests+CountLocTests.RemovesAnInlineBlockComment'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.EvaluationTests+CountLocTests.RemovesAnInlineBlockComment'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE);
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:08.37
BUILD EXIT=0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:01.44
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.267s
OLD-VALID EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.316s
NEW-VALID EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.268s
OLD-FAULT EXIT=0
    FunnySharp.Harness.Tests.EvaluationTests+CountLocTests.RemovesAnInlineBlockComment [FAIL]
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   3
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.259s
NEW-FAULT EXIT=1
```

### L-bom-consumer-old

Monitor `mon_3FQK1GRQ337NW4S1`, session `bash_32`.

```diff
-            3
-        else
-            0
+            0
+        else
+            0
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.EvaluationTests+CountLocTests.StripsAByteOrderMark'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); Write-Output 'OLD-CORE'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.LocTests+CountLocTests.CountLocStripsBomSoLeadingCommentIsExcluded'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-CORE EXIT=' + $LASTEXITCODE);
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:18.27
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.267s
OLD-VALID EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.209s
OLD-FAULT EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.206s
OLD-CORE EXIT=0
```

### L-bom-consumer-new

Monitor `mon_BJDY1T03XFY2T9F8`, session `bash_34`.

```diff
-            3
-        else
-            0
+            0
+        else
+            0
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.EvaluationTests+CountLocTests.StripsAByteOrderMark'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE); Write-Output 'NEW-CORE'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.LocTests+CountLocTests.CountLocStripsBomSoLeadingCommentIsExcluded'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-CORE EXIT=' + $LASTEXITCODE);
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:21.20
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.346s
NEW-VALID EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.310s
NEW-FAULT EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.335s
NEW-CORE EXIT=0
```

### L-bom-partial-consumer-old

Monitor `mon_ZSF8ZG785M0RQW9C`, session `bash_39`.

```diff
-            3
-        else
-            0
+            1
+        else
+            0
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.EvaluationTests+CountLocTests.StripsAByteOrderMark'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); Write-Output 'OLD-CORE'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.LocTests+CountLocTests.CountLocStripsBomSoLeadingCommentIsExcluded'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-CORE EXIT=' + $LASTEXITCODE);
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:19.87
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.321s
OLD-VALID EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.251s
OLD-FAULT EXIT=0
    FunnySharp.Harness.Tests.LocTests+CountLocTests.CountLocStripsBomSoLeadingCommentIsExcluded [FAIL]
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   2
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.293s
OLD-CORE EXIT=1
```

### L-bom-partial-consumer-new

Monitor `mon_GB7HRN0MYKSZ53GZ`, session `bash_40`.

```diff
-            3
-        else
-            0
+            1
+        else
+            0
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.EvaluationTests+CountLocTests.StripsAByteOrderMark'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE); Write-Output 'NEW-CORE'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.LocTests+CountLocTests.CountLocStripsBomSoLeadingCommentIsExcluded'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-CORE EXIT=' + $LASTEXITCODE);
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:20.13
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.261s
NEW-VALID EXIT=0
    FunnySharp.Harness.Tests.EvaluationTests+CountLocTests.StripsAByteOrderMark [FAIL]
      Assert.Equal() Failure: Values differ
      Expected: 0
      Actual:   1
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.267s
NEW-FAULT EXIT=1
    FunnySharp.Harness.Tests.LocTests+CountLocTests.CountLocStripsBomSoLeadingCommentIsExcluded [FAIL]
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   2
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.279s
NEW-CORE EXIT=1
```

### L-bom-retained-final-consumer-old

Monitor `mon_5AGMKJF1HK0MVR3M`, session `bash_42`.

```diff
-            3
-        else
-            0
+            0
+        else
+            0
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.EvaluationTests+CountLocTests.StripsAByteOrderMark'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); Write-Output 'OLD-CORE'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.LocTests+CountLocTests.CountLocStripsBomSoLeadingCommentIsExcluded'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-CORE EXIT=' + $LASTEXITCODE);
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:16.53
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.258s
OLD-VALID EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.213s
OLD-FAULT EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.209s
OLD-CORE EXIT=0
```

### L-bom-retained-final-consumer-new

Monitor `mon_QWKN27T10P5JSCSS`, session `bash_44`.

```diff
-            3
-        else
-            0
+            0
+        else
+            0
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.EvaluationTests+CountLocTests.StripsAByteOrderMark'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE); Write-Output 'NEW-CORE'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.LocTests+CountLocTests.CountLocStripsBomSoLeadingCommentIsExcluded'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-CORE EXIT=' + $LASTEXITCODE);
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:16.46
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.263s
NEW-VALID EXIT=0
    FunnySharp.Harness.Tests.EvaluationTests+CountLocTests.StripsAByteOrderMark [FAIL]
      Assert.Equal() Failure: Values differ
      Expected: 0
      Actual:   1
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.231s
NEW-FAULT EXIT=1
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.203s
NEW-CORE EXIT=0
```

### L-block-complete-consumer-old

Monitor `mon_E6TXKCDTCZK201R1`, session `bash_46`.

```diff
-    let withoutBlocks = blockCommentPattern.Replace(text, "")
+    let withoutBlocks = text
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.EvaluationTests+CountLocTests.RemovesAnInlineBlockComment'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); Write-Output 'OLD-CORE'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.LocTests+CountLocTests.CountLocStripsBlockCommentsAcrossLines'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-CORE EXIT=' + $LASTEXITCODE);
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:17.94
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.263s
OLD-VALID EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.201s
OLD-FAULT EXIT=0
    FunnySharp.Harness.Tests.LocTests+CountLocTests.CountLocStripsBlockCommentsAcrossLines [FAIL]
      Assert.Equal() Failure: Values differ
      Expected: 2
      Actual:   4
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.228s
OLD-CORE EXIT=1
```

### L-block-complete-consumer-new

Monitor `mon_GFFE6CZGWZP5CYHB`, session `bash_48`.

```diff
-    let withoutBlocks = blockCommentPattern.Replace(text, "")
+    let withoutBlocks = text
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.EvaluationTests+CountLocTests.RemovesAnInlineBlockComment'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE); Write-Output 'NEW-CORE'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.LocTests+CountLocTests.CountLocStripsBlockCommentsAcrossLines'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-CORE EXIT=' + $LASTEXITCODE);
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:17.32
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.311s
NEW-VALID EXIT=0
    FunnySharp.Harness.Tests.EvaluationTests+CountLocTests.RemovesAnInlineBlockComment [FAIL]
      Assert.Equal() Failure: Values differ
      Expected: 1
      Actual:   3
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.261s
NEW-FAULT EXIT=1
    FunnySharp.Harness.Tests.LocTests+CountLocTests.CountLocStripsBlockCommentsAcrossLines [FAIL]
      Assert.Equal() Failure: Values differ
      Expected: 2
      Actual:   4
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.251s
NEW-CORE EXIT=1
```

### F-bytes-consumer-old

Monitor `mon_CQJ7098RR9GG9S2J`, session `bash_50`.

```diff
-            entries.Add(relativePath.Replace('\\', '/'), sha256File fullPath)
+            entries.Add(relativePath.Replace('\\', '/'), "ignored")
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.ReleaseVerifyTests.GetSourceFingerprint_IsDeterministicAndCaseInsensitiveOverAGitTree'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); 
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:20.46
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.308s
OLD-VALID EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.751s
OLD-FAULT EXIT=0
```

### F-bytes-consumer-new

Monitor `mon_87H08TBEWDV316N1`, session `bash_51`.

```diff
-            entries.Add(relativePath.Replace('\\', '/'), sha256File fullPath)
+            entries.Add(relativePath.Replace('\\', '/'), "ignored")
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.ReleaseVerifyTests.GetSourceFingerprint_TracksFileCountAndChangedBytes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE); 
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:16.77
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.256s
NEW-VALID EXIT=0
    FunnySharp.Harness.Tests.ReleaseVerifyTests.GetSourceFingerprint_TracksFileCountAndChangedBytes [FAIL]
      Assert.NotEqual() Failure: Strings are equal
      Expected: Not "34aa683921ce4a84376ddb1b2c0020dcfbcf8e9f39abc63895"···
      Actual:       "34aa683921ce4a84376ddb1b2c0020dcfbcf8e9f39abc63895"···
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.478s
NEW-FAULT EXIT=1
```

### F-count-consumer-old

Monitor `mon_423R45AS5BVTT396`, session `bash_52`.

```diff
-    node.["fileCount"] <- jint ordered.Length
+    node.["fileCount"] <- jint 2
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.ReleaseVerifyTests.GetSourceFingerprint_IsDeterministicAndCaseInsensitiveOverAGitTree'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); 
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:16.65
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.254s
OLD-VALID EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.616s
OLD-FAULT EXIT=0
```

### F-count-consumer-new

Monitor `mon_2DMX7ZF9RKRTJFYV`, session `bash_54`.

```diff
-    node.["fileCount"] <- jint ordered.Length
+    node.["fileCount"] <- jint 2
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.ReleaseVerifyTests.GetSourceFingerprint_TracksFileCountAndChangedBytes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE); 
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:16.62
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.259s
NEW-VALID EXIT=0
    FunnySharp.Harness.Tests.ReleaseVerifyTests.GetSourceFingerprint_TracksFileCountAndChangedBytes [FAIL]
      Assert.Equal() Failure: Values differ
      Expected: Some(3)
      Actual:   Some(2)
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.547s
NEW-FAULT EXIT=1
```

### F-schema-left-consumer-old

Monitor `mon_0QDAENZ77593SHEG`, session `bash_56`.

```diff
-    propInt "schemaVersion" left = Some 1
+    true
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.ReleaseVerifyTests.EquivalentSourceFingerprint_RequiresSchemaAlgorithmCountAndDigest'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); 
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:16.63
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.298s
OLD-VALID EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.230s
OLD-FAULT EXIT=0
```

### F-schema-left-consumer-new

Monitor `mon_9BF6HNBPK4JZ19GZ`, session `bash_57`.

```diff
-    propInt "schemaVersion" left = Some 1
+    true
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.ReleaseVerifyTests.EquivalentSourceFingerprint_RequiresSchemaAlgorithmCountAndDigest'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE); 
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:19.30
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.258s
NEW-VALID EXIT=0
    FunnySharp.Harness.Tests.ReleaseVerifyTests.EquivalentSourceFingerprint_RequiresSchemaAlgorithmCountAndDigest [FAIL]
      Assert.False() Failure
      Expected: False
      Actual:   True
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.238s
NEW-FAULT EXIT=1
```

### F-schema-right-consumer-old

Monitor `mon_VDPPEAE04QRDHVFY`, session `bash_59`.

```diff
-    && propInt "schemaVersion" right = Some 1
+    && true
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.ReleaseVerifyTests.EquivalentSourceFingerprint_RequiresSchemaAlgorithmCountAndDigest'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); 
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:16.47
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.265s
OLD-VALID EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.208s
OLD-FAULT EXIT=0
```

### F-schema-right-consumer-new

Monitor `mon_G6CGHP3EW6JHRBS5`, session `bash_61`.

```diff
-    && propInt "schemaVersion" right = Some 1
+    && true
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.ReleaseVerifyTests.EquivalentSourceFingerprint_RequiresSchemaAlgorithmCountAndDigest'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE); 
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:17.58
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.256s
NEW-VALID EXIT=0
    FunnySharp.Harness.Tests.ReleaseVerifyTests.EquivalentSourceFingerprint_RequiresSchemaAlgorithmCountAndDigest [FAIL]
      Assert.False() Failure
      Expected: False
      Actual:   True
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.242s
NEW-FAULT EXIT=1
```

### F-algorithm-left-consumer-old

Monitor `mon_RDV9WX831RH6E6M2`, session `bash_62`.

```diff
-    && equalsIgnoreCase (propText "algorithm" left) "sha256"
+    && true
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.ReleaseVerifyTests.EquivalentSourceFingerprint_RequiresSchemaAlgorithmCountAndDigest'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); 
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:16.77
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.259s
OLD-VALID EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.216s
OLD-FAULT EXIT=0
```

### F-algorithm-left-consumer-new

Monitor `mon_8Q9GEAGJQ3ADF5A4`, session `bash_63`.

```diff
-    && equalsIgnoreCase (propText "algorithm" left) "sha256"
+    && true
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.ReleaseVerifyTests.EquivalentSourceFingerprint_RequiresSchemaAlgorithmCountAndDigest'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE); 
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:16.52
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.256s
NEW-VALID EXIT=0
    FunnySharp.Harness.Tests.ReleaseVerifyTests.EquivalentSourceFingerprint_RequiresSchemaAlgorithmCountAndDigest [FAIL]
      Assert.False() Failure
      Expected: False
      Actual:   True
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.243s
NEW-FAULT EXIT=1
```

### F-algorithm-right-consumer-old

Monitor `mon_SD58DQAT05MR2F5M`, session `bash_65`.

```diff
-    && equalsIgnoreCase (propText "algorithm" right) "sha256"
+    && true
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.ReleaseVerifyTests.EquivalentSourceFingerprint_RequiresSchemaAlgorithmCountAndDigest'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); 
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:16.41
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.286s
OLD-VALID EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.242s
OLD-FAULT EXIT=0
```

### F-algorithm-right-consumer-new

Monitor `mon_F7WWKHV2MVTFJH44`, session `bash_66`.

```diff
-    && equalsIgnoreCase (propText "algorithm" right) "sha256"
+    && true
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.ReleaseVerifyTests.EquivalentSourceFingerprint_RequiresSchemaAlgorithmCountAndDigest'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE); 
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:16.42
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.271s
NEW-VALID EXIT=0
    FunnySharp.Harness.Tests.ReleaseVerifyTests.EquivalentSourceFingerprint_RequiresSchemaAlgorithmCountAndDigest [FAIL]
      Assert.False() Failure
      Expected: False
      Actual:   True
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.243s
NEW-FAULT EXIT=1
```

### F-equivalent-count-consumer-old

Monitor `mon_A5KVJPS7SM111DD2`, session `bash_68`.

```diff
-    && propInt "fileCount" left = propInt "fileCount" right
+    && true
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.ReleaseVerifyTests.EquivalentSourceFingerprint_RequiresSchemaAlgorithmCountAndDigest'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); 
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:16.87
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.261s
OLD-VALID EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.215s
OLD-FAULT EXIT=0
```

### F-equivalent-count-consumer-new

Monitor `mon_G3KJQB0P7T3HD9GP`, session `bash_70`.

```diff
-    && propInt "fileCount" left = propInt "fileCount" right
+    && true
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.ReleaseVerifyTests.EquivalentSourceFingerprint_RequiresSchemaAlgorithmCountAndDigest'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE); 
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:16.46
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.257s
NEW-VALID EXIT=0
    FunnySharp.Harness.Tests.ReleaseVerifyTests.EquivalentSourceFingerprint_RequiresSchemaAlgorithmCountAndDigest [FAIL]
      Assert.False() Failure
      Expected: False
      Actual:   True
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.241s
NEW-FAULT EXIT=1
```

### F-digest-consumer-old

Monitor `mon_VV99B0K97QMPJ4N3`, session `bash_71`.

```diff
-    && equalsIgnoreCase (propText "digest" left) (propText "digest" right)
+    && true
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.ReleaseVerifyTests.EquivalentSourceFingerprint_RequiresSchemaAlgorithmCountAndDigest'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); 
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:20.57
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.340s
OLD-VALID EXIT=0
    FunnySharp.Harness.Tests.ReleaseVerifyTests.EquivalentSourceFingerprint_RequiresSchemaAlgorithmCountAndDigest [FAIL]
      Assert.False() Failure
      Expected: False
      Actual:   True
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.248s
OLD-FAULT EXIT=1
```

### F-digest-consumer-new

Monitor `mon_5RAX39DM9TFDD92X`, session `bash_73`.

```diff
-    && equalsIgnoreCase (propText "digest" left) (propText "digest" right)
+    && true
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.ReleaseVerifyTests.EquivalentSourceFingerprint_RequiresSchemaAlgorithmCountAndDigest'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE); 
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:17.37
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.275s
NEW-VALID EXIT=0
    FunnySharp.Harness.Tests.ReleaseVerifyTests.EquivalentSourceFingerprint_RequiresSchemaAlgorithmCountAndDigest [FAIL]
      Assert.False() Failure
      Expected: False
      Actual:   True
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.236s
NEW-FAULT EXIT=1
```

### F-digest-case-consumer-old

Monitor `mon_04NZB8DJ0XNZJR7S`, session `bash_75`.

```diff
-    && equalsIgnoreCase (propText "digest" left) (propText "digest" right)
+    && equalsOrdinal (propText "digest" left) (propText "digest" right)
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.ReleaseVerifyTests.EquivalentSourceFingerprint_RequiresSchemaAlgorithmCountAndDigest'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); 
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:17.12
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.269s
OLD-VALID EXIT=0
    FunnySharp.Harness.Tests.ReleaseVerifyTests.EquivalentSourceFingerprint_RequiresSchemaAlgorithmCountAndDigest [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.237s
OLD-FAULT EXIT=1
```

### F-digest-case-consumer-new

Monitor `mon_SBRWGDB80VTBGXHY`, session `bash_77`.

```diff
-    && equalsIgnoreCase (propText "digest" left) (propText "digest" right)
+    && equalsOrdinal (propText "digest" left) (propText "digest" right)
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.ReleaseVerifyTests.EquivalentSourceFingerprint_RequiresSchemaAlgorithmCountAndDigest'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE); 
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:17.35
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.253s
NEW-VALID EXIT=0
    FunnySharp.Harness.Tests.ReleaseVerifyTests.EquivalentSourceFingerprint_RequiresSchemaAlgorithmCountAndDigest [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.236s
NEW-FAULT EXIT=1
```

### F-call-state-consumer-old

Monitor `mon_FRAMBD54R17ND5H3`, session `bash_78`.

```diff
-let getSourceFingerprint (root: string) : JsonObject =
+let private fingerprintCalls = ref 0
+
+let getSourceFingerprint (root: string) : JsonObject =
```
```diff
-    node.["digest"] <- jstr (sha256Text canonical)
+    fingerprintCalls.Value <- fingerprintCalls.Value + 1
+    node.["digest"] <- jstr (if fingerprintCalls.Value % 2 = 0 then String.replicate 64 "b" else String.replicate 64 "a")
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.ReleaseVerifyTests.GetSourceFingerprint_IsDeterministicAndCaseInsensitiveOverAGitTree'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); 
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:19.50
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.269s
OLD-VALID EXIT=0
    FunnySharp.Harness.Tests.ReleaseVerifyTests.GetSourceFingerprint_IsDeterministicAndCaseInsensitiveOverAGitTree [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: ···"Count": 2,\r\n  "digest": "aaaaaaaaaaaaaaaaaaaaaaaaa"···
      Actual:   ···"Count": 2,\r\n  "digest": "bbbbbbbbbbbbbbbbbbbbbbbbb"···
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.627s
OLD-FAULT EXIT=1
```

### F-call-state-consumer-new

Monitor `mon_MB0WPQTV88F02RV7`, session `bash_80`.

```diff
-let getSourceFingerprint (root: string) : JsonObject =
+let private fingerprintCalls = ref 0
+
+let getSourceFingerprint (root: string) : JsonObject =
```
```diff
-    node.["digest"] <- jstr (sha256Text canonical)
+    fingerprintCalls.Value <- fingerprintCalls.Value + 1
+    node.["digest"] <- jstr (if fingerprintCalls.Value % 2 = 0 then String.replicate 64 "b" else String.replicate 64 "a")
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.ReleaseVerifyTests.GetSourceFingerprint_TracksFileCountAndChangedBytes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE); 
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:29.32
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.354s
NEW-VALID EXIT=0
    FunnySharp.Harness.Tests.ReleaseVerifyTests.GetSourceFingerprint_TracksFileCountAndChangedBytes [FAIL]
      Assert.True() Failure
      Expected: True
      Actual:   False
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.640s
NEW-FAULT EXIT=1
```

### R-prose-complete-consumer-old

Monitor `mon_0XQM7V7NCN9JM41A`, session `bash_82`.

```diff
-        failError "Required status checks must require branches to be up to date before merging."
+        failError "Strict status policy is required."
```
```diff
-        failError (sprintf "Ruleset %d is not active." rulesetId)
+        failError (sprintf "Ruleset %d requires active enforcement." rulesetId)
```
```diff
-            failError (sprintf "Ruleset %d does not target branches." rulesetId)
+            failError (sprintf "Ruleset %d requires a branch target." rulesetId)
```
```diff
-                    sprintf "Ruleset %d does not explicitly include '%s' or the default branch." rulesetId targetRef
+                    sprintf "Ruleset %d must cover '%s'." rulesetId targetRef
```
```diff
-                        "Ruleset %d contains branch exclusions and cannot prove fail-closed default-branch coverage."
+                        "Ruleset %d must have no excluded refs."
```
```diff
-                        sprintf "Ruleset %d must contain exactly one required_status_checks rule." rulesetId
+                        sprintf "Ruleset %d requires one status rule." rulesetId
```
```diff
-                                    "Ruleset %d is missing required contexts: %s."
+                                    "Ruleset %d requires contexts: %s."
```
```diff
-                                                "Ruleset %d must bind required context '%s' to GitHub App integration %d."
+                                                "Ruleset %d requires '%s' from app %d."
```
```diff
-                                            "Ruleset %d contains bypass actors and cannot support this candidate's PASS verdict."
+                                            "Ruleset %d must have no bypass actors."
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'OLD-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-VALID EXIT=' + $LASTEXITCODE); Write-Output 'OLD-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/old/FunnySharp.Harness.Tests.dll' -class 'FunnySharp.Harness.Tests.RulesetTests+*'  -failSkips -failWarns -noLogo -noColor; Write-Output ('OLD-FAULT EXIT=' + $LASTEXITCODE); 
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:25.61
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.281s
OLD-VALID EXIT=0
    FunnySharp.Harness.Tests.RulesetTests+StrictPolicyTests.VerifyGitHubRuleset_DisabledStrictPolicy_Rejects [FAIL]
      Assert.Contains() Failure: Sub-string not found
    FunnySharp.Harness.Tests.RulesetTests+StrictPolicyTests.AssertStrictRequiredStatusChecksPolicy_Missing_Rejects [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "Required status checks must require branches to be"···
      Actual:   "Strict status policy is required."
    FunnySharp.Harness.Tests.RulesetTests+StrictPolicyTests.VerifyGitHubRuleset_MissingStrictPolicy_Rejects [FAIL]
      Assert.Contains() Failure: Sub-string not found
    FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_BypassActor_Rejects [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "Ruleset 42 contains bypass actors and cannot suppo"···
      Actual:   "Ruleset 42 must have no bypass actors."
    FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_TwoStatusRules_Rejects [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "Ruleset 42 must contain exactly one required_statu"···
      Actual:   "Ruleset 42 requires one status rule."
    FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_NonBranchTarget_Rejects [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "Ruleset 42 does not target branches."
      Actual:   "Ruleset 42 requires a branch target."
    FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_NonActiveEnforcement_Rejects [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "Ruleset 42 is not active."
      Actual:   "Ruleset 42 requires active enforcement."
    FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_MissingRequiredContext_Rejects [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "Ruleset 42 is missing required contexts: release /"···
      Actual:   "Ruleset 42 requires contexts: release / osx-arm64."
    FunnySharp.Harness.Tests.RulesetTests+CommandLineTests.VerifyGitHubRuleset_NonActiveRuleset_Rejects [FAIL]
      Assert.Contains() Failure: Sub-string not found
    FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_WrongIntegrationId_Rejects [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "Ruleset 42 must bind required context 'release / w"···
      Actual:   "Ruleset 42 requires 'release / win-x64' from app 1"···
    FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_TargetRefNotIncluded_Rejects [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "Ruleset 42 does not explicitly include 'refs/heads"···
      Actual:   "Ruleset 42 must cover 'refs/heads/main'."
    FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ExcludedRef_Rejects [FAIL]
      Assert.Equal() Failure: Strings differ
      Expected: "Ruleset 42 contains branch exclusions and cannot p"···
      Actual:   "Ruleset 42 must have no excluded refs."
   FunnySharp.Harness.Tests  Total: 19, Errors: 0, Failed: 12, Skipped: 0, Not Run: 0, Time: 0.316s
OLD-FAULT EXIT=1
```

### R-prose-complete-consumer-new

Monitor `mon_ARNDFVMMPZA4W6K3`, session `bash_83`.

```diff
-        failError "Required status checks must require branches to be up to date before merging."
+        failError "Strict status policy is required."
```
```diff
-        failError (sprintf "Ruleset %d is not active." rulesetId)
+        failError (sprintf "Ruleset %d requires active enforcement." rulesetId)
```
```diff
-            failError (sprintf "Ruleset %d does not target branches." rulesetId)
+            failError (sprintf "Ruleset %d requires a branch target." rulesetId)
```
```diff
-                    sprintf "Ruleset %d does not explicitly include '%s' or the default branch." rulesetId targetRef
+                    sprintf "Ruleset %d must cover '%s'." rulesetId targetRef
```
```diff
-                        "Ruleset %d contains branch exclusions and cannot prove fail-closed default-branch coverage."
+                        "Ruleset %d must have no excluded refs."
```
```diff
-                        sprintf "Ruleset %d must contain exactly one required_status_checks rule." rulesetId
+                        sprintf "Ruleset %d requires one status rule." rulesetId
```
```diff
-                                    "Ruleset %d is missing required contexts: %s."
+                                    "Ruleset %d requires contexts: %s."
```
```diff
-                                                "Ruleset %d must bind required context '%s' to GitHub App integration %d."
+                                                "Ruleset %d requires '%s' from app %d."
```
```diff
-                                            "Ruleset %d contains bypass actors and cannot support this candidate's PASS verdict."
+                                            "Ruleset %d must have no bypass actors."
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -class 'FunnySharp.Harness.Tests.RulesetTests+*'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE); 
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:15.87
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.258s
NEW-VALID EXIT=0
   FunnySharp.Harness.Tests  Total: 19, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.274s
NEW-FAULT EXIT=0
```

### F-bytes-final-new

Monitor `mon_CB9ZKZC89T2RX0H5`, session `bash_85`.

```diff
-            entries.Add(relativePath.Replace('\\', '/'), sha256File fullPath)
+            entries.Add(relativePath.Replace('\\', '/'), "ignored")
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.ReleaseVerifyTests.GetSourceFingerprint_TracksFileCountAndChangedBytes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE); 
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:16.73
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.264s
NEW-VALID EXIT=0
    FunnySharp.Harness.Tests.ReleaseVerifyTests.GetSourceFingerprint_TracksFileCountAndChangedBytes [FAIL]
      Assert.NotEqual() Failure: Strings are equal
      Expected: Not "34aa683921ce4a84376ddb1b2c0020dcfbcf8e9f39abc63895"···
      Actual:       "34aa683921ce4a84376ddb1b2c0020dcfbcf8e9f39abc63895"···
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.511s
NEW-FAULT EXIT=1
```

### F-count-final-new

Monitor `mon_PPFD25GRFTASGVYF`, session `bash_86`.

```diff
-    node.["fileCount"] <- jint ordered.Length
+    node.["fileCount"] <- jint 2
```

Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; Write-Output 'NEW-VALID'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.RulesetTests+RulesetCheckTests.CheckRuleset_ValidRuleset_Passes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-VALID EXIT=' + $LASTEXITCODE); Write-Output 'NEW-FAULT'; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll' -method 'FunnySharp.Harness.Tests.ReleaseVerifyTests.GetSourceFingerprint_TracksFileCountAndChangedBytes'  -failSkips -failWarns -noLogo -noColor; Write-Output ('NEW-FAULT EXIT=' + $LASTEXITCODE); 
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:18.50
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 0.257s
NEW-VALID EXIT=0
    FunnySharp.Harness.Tests.ReleaseVerifyTests.GetSourceFingerprint_TracksFileCountAndChangedBytes [FAIL]
      Assert.Equal() Failure: Values differ
      Expected: Some(3)
      Actual:   Some(2)
   FunnySharp.Harness.Tests  Total: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0, Time: 0.596s
NEW-FAULT EXIT=1
```

### full-restored-harness

Monitor `mon_GD5ZR2RE8QDDT46B`, session `bash_87`.



Exact decoded PowerShell command (`powershell -NoProfile -EncodedCommand`):

```powershell
Set-Location 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles'; & dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj -t:Rebuild -c Debug -p:RestoreLockedMode=true -p:OutputPath='Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/' -v minimal; Write-Output ('BUILD EXIT=' + $LASTEXITCODE); if ($LASTEXITCODE -ne 0) { exit 1 }; & dotnet 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-harness-oracles/artifacts/test-ablation/new/FunnySharp.Harness.Tests.dll'  -failSkips -failWarns -noLogo -noColor; $testExit = $LASTEXITCODE; Write-Output ('FULL EXIT=' + $testExit); exit $testExit;
```

Executed output (decisive compiler, assertion, count and exit lines):

```text
status: completed exit_code: 0
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:16.02
BUILD EXIT=0
   FunnySharp.Harness.Tests  Total: 745, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 18.294s
FULL EXIT=0
```

## Final gate and cleanup receipt

The single final gate ran under monitor `mon_GD5ZR2RE8QDDT46B`, session
`bash_87`, after all five permitted temporary source files matched their
initial full-text snapshots. It was not restarted or duplicated. Its exact
decoded command and passing output appear in `full-restored-harness` above.
The outer command propagates the actual full-suite exit code.

The shipping source, product/performance budgets, 32 MiB boundaries, raw
source/receipt/transport joins, all original test partitions and both POSIX
source cases remain untouched. The Windows execution count stays 745; a count
is reported as gate coverage, not as proof that the 43 external C# oracle
semantics executed. All requested focused proof runs compiled before their
assertion failures were counted. Incomplete/setup attempts remain explicitly
identified and are not acceptance evidence.

Permanent files, relative to the assigned worktree:

- `tests/FunnySharp.Harness.Tests/RulesetTests.fs`
- `tests/FunnySharp.Harness.Tests/EvaluationTests.fs`
- `tests/FunnySharp.Harness.Tests/ReleaseVerifyTests.fs`
- `docs/audits/test-ablation/experiments/harness-oracles.md`

Temporary source restoration includes `eng/harness/Ruleset.fs`, `Loc.fs`,
`Evaluation.fs`, `ReleaseVerifySource.fs` and `ReleaseVerify.fs`. There is no
remaining source mutant, call-state counter, old-test-source transition or
pending build/test command. Ignored generated binaries remain as local
experiment output; final candidate binaries were rebuilt from restored source.

Residual limits: F# LSP was unavailable, so actual locked compiler diagnostics
were used. The unchanged source-only 43-name census proves only declaration
presence. The existing core BOM-leading-comment fixture does not reject a
fully retained U+FEFF on this Windows culture-sensitive path; its independent
partial-BOM fault rejection and the stronger final wrapper rejection are
recorded separately. No semantic truth or cryptographic provenance is inferred
from the two-view fingerprint comparison. No scope blocker remains for this
implementation unit. Parent integration and whole-repository final gates were
not performed by this worker.

## Interpretation boundaries

Before final byte/count revalidation, the candidate was already on disk. The
orchestrator redundantly attempted to reapply it from old text; all three
patches were rejected with no file actions. Full reads verified all three
files exactly match the candidate snapshot. The orchestrator now checks the
current snapshot and skips an unnecessary transition. This setup error is
not a mutant rejection and did not change the tested source.

The final file-byte detector keeps an unchanged-input identity comparison as
its negative control, alongside changed bytes and added tracked-file controls.
This preserves the old test's meaningful rejection of call-state-dependent
identity without pretending repeatability alone proves correctness. A
deterministic alternating 64-character identity mutant (no clock/random wait)
is used to execute the same fault against old and final candidate. No digest
length assertion or independently recomputed SHA is reintroduced.

### Windows BOM prefix finding and corrected boundary

Rebuilding both consumers did not make the original BOM-plus-leading-comment
fixture reject `start = 0`; the existing core leading-comment fixture also
passed. This was not a proven compilation/inlining problem. A direct .NET 10
probe established the actual reason: culture-sensitive `StartsWith("//")`
ignores leading U+FEFF even though Trim retains it and UTF-8 decoding emits it.
The final wrapper fixture therefore puts a blank line between BOM and the
comment-only line. Retaining BOM makes that first line count 1 instead of 0.
No product behavior or core test is changed to hide this finding.

The shared independently meaningful BOM fault uses an incorrect partial skip
(`start = 1` instead of 3), which corrupts the prefix. Both the final wrapper
and existing core fixture must reject this same fault; the old BOM-plus-code
fixture still counts 1. The separate retained-BOM fault is also run against
the final wrapper. The core fixture's retained-BOM false-green is reported as
a pre-existing limitation, not falsely claimed independent detection.

Actual probe command: `dotnet fsi --readline- --nologo --quiet`, with raw
UTF-8 stdin (no BOM), exit 0, empty stderr:

```fsharp
let bomComment = string (char 0xFEFF) + "// comment";;
printfn "BOM-TRIM=%d" (bomComment.Trim().Length);;
printfn "STARTS-CULTURE=%b ORDINAL=%b" (bomComment.StartsWith("//")) (bomComment.StartsWith("//", System.StringComparison.Ordinal));;
printfn "UTF8-DECODE-FIRST=%x" (int ((System.Text.UTF8Encoding(false).GetString [|0xEFuy;0xBBuy;0xBFuy;0x2Fuy|]).[0]));;
#quit;;
```

```text
BOM-TRIM=11
STARTS-CULTURE=true ORDINAL=false
UTF8-DECODE-FIRST=feff
```

Earlier PowerShell-to-FSI probe pipes emitted a BOM into stdin and exited 1
with FS0010 at the first character; those are setup failures, not proof.
The first raw-stdin probe emitted the same values but exited 1 at EOF;
explicit `#quit;;` supplied the clean exit above.

The first BOM harness-only rebuild produced old 1/0/0 and candidate 1/0/0,
so it did NOT prove the intended candidate detector. The first block run did
produce old 1/0/0 and candidate 1/1/1 (expected LOC 1, observed 3), but the
orchestration function then in use omitted the independent core filter. These
are recorded as incomplete experiments, not successful closure. Consumer
assemblies were rebuilt under each LOC/fingerprint mutant: old test source
was restored temporarily with `apply_patch`, the old consumer was rebuilt and
run, candidate test source was reapplied, and the candidate consumer was rebuilt
and run. This also avoided relying on whether F# optimization metadata inlines
thin wrappers from a previously compiled harness. No run overlaps another
build on the same project/output/cache. The full candidate source was retained
in memory while each old consumer was compiled and is restored on disk.

The first `R-prose-valid` run finished with the candidate Ruleset class at
19 total, 0 failures, exit 0. An early partial-output read was not retained,
so its earlier compiler/old/control output is unavailable in the saved report.
That incomplete run does not establish the old/new comparison. The command
completed before restoration and before the BOM run began. The later serial
`R-prose-complete` old/new consumer runs retained the complete output: old
19/12/1, candidate 19/0/0. They did not duplicate a live command.

The missing-context input encounters two independent checks in the same
production path: the missing-context census and the per-context binding list.
Disabling only the first still rejects the invalid ruleset through the second.
The `R-missing-context` fault therefore deliberately bypasses both defenses for
absent contexts; otherwise a diagnostic change would be mislabeled an
accept-invalid-input regression. Its independently authored valid ruleset must
still pass unchanged.

`Expected a rejected ruleset.` is the candidate test helper's intentional
assertion failure on `Ok`, not an unexpected production exception or setup
failure. CLI failures compare the actual returned exit code (expected 1,
observed 0). No fault is counted from compilation or fixture exceptions.
