# Portable verifier exit-control ablation

## Scope

This experiment covers `eng/verification/portable-available-controls.ps1`.
The wrapper invokes the historical verifier, the live offline verifier, its
help probe, and its usage probe. The owned negative fixture is an empty
directory: no archive was downloaded or synthesized.

## Fault and correction

The old wrapper set `$ErrorActionPreference = 'Stop'`, but native `dotnet fsi`
nonzero exits only set `$LASTEXITCODE`; they did not stop the wrapper. It
printed all four markers and returned success even when both evidence verifiers
reported missing input.

The corrected wrapper captures `$LASTEXITCODE` immediately after every native
probe, preserves all four markers, and requires the machine tuple
`OLD_PORTABLE_EXIT=0`, `NEW_PORTABLE_EXIT=0`, `NEW_HELP_EXIT=0`,
`NEW_USAGE_EXIT=2`. A mismatch writes an error and exits `1`. No ordinary
error-prose matching, archive skipping, retry, framework, or SHA computation
was added.

## Executed evidence

Environment: Windows x64, SDK selected by the repository `global.json`.
Commands ran from `Q:/repos/funnysharp/.omo/worktrees/test-ablation-portable-control`.

Owned fixture setup was performed by PowerShell by removing and recreating
`artifacts/test-ablation/portable-control-empty`; it was an empty directory.

Old wrapper body (the assertion block was temporarily absent, then restored
exactly) was invoked as:

```text
powershell -NoProfile -ExecutionPolicy Bypass -File eng/verification/portable-available-controls.ps1 -RepositoryRoot Q:/repos/funnysharp/.omo/worktrees/test-ablation-portable-control -ArtifactDirectory artifacts/test-ablation/portable-control-empty
```

Observed markers and verifier diagnostics:

```text
OLD_PORTABLE_EXIT=1
NEW_PORTABLE_EXIT=1
NEW_HELP_EXIT=0
NEW_USAGE_EXIT=2
PORTABLE_EVIDENCE_FAIL InvalidOperationException: Missing regular file: ...portable-control-empty\provenance.zip
```

The old wrapper process exit was **0**. This is the demonstrated false-green:
the two evidence probes failed, but the wrapper did not reject their native
nonzero exits. The help and usage probes remained the expected `0` and `2`.

The corrected wrapper was invoked with the same exact argv. It emitted the same
four markers and the same two missing-archive diagnostics, then emitted:

```text
Portable verifier exit tuple mismatch: expected 0,0,0,2; actual 1,1,0,2
```

The corrected wrapper process exit was **1**. Thus the same absent/empty input
now refuses acceptance while the help and usage controls remain `0` and `2`.

## Positive-input availability

No genuine `provenance.zip` and `index.zip` pair was available in this
worktree's retained artifacts or repository paths inspected for this experiment.
The negative proof therefore does not claim missing-archive full publication
acceptance; only the wrapper's fail-closed control behavior is established.

## Parent positive decision-seam control

The parent executed the actual revised wrapper with a controlled `dotnet`
function that supplies the four native-exit observations `0,0,0,2`. The wrapper
is not copied or reimplemented. The control verifies exactly four `fsi`
invocations and removes its process-local function afterward. This isolates
the newly added exit-tuple decision without inventing a positive archive.

```text
OLD_PORTABLE_EXIT=0
NEW_PORTABLE_EXIT=0
NEW_HELP_EXIT=0
NEW_USAGE_EXIT=2
CONTROLLED_POSITIVE_TUPLE=0,0,0,2
PROBE_COUNT=4
ARCHIVE_ACCEPTANCE_CLAIM=false
```

Command: `pwsh -NoLogo -NoProfile -File artifacts/test-ablation/portable-positive-tuple.ps1 -RepositoryRoot Q:/repos/funnysharp/.omo/worktrees/test-ablation-portable-control`.
Monitor `mon_J08WREZR6P4CVZBY`, process `bash_81`, completed with exit 0.
The process-local stub is intentional experiment machinery only; real native
negative verifier/help/usage invocations above establish the actual failing
tuple. This positive control proves the decision accepts its legitimate tuple,
not archive, package, publication or release acceptance. No stub is shipped.

## Syntax and cleanup

Parent complete repository formatter: `dotnet fsi build.fsx -- -p format`,
exit 0, 32.933 s. This is a commit gate, not an archive acceptance result.

The wrapper was syntax-checked and executed through PowerShell's direct `-File`
path as part of the evidence command above. The final direct invocation exited
`1` for the intentional tuple mismatch. The owned empty fixture was removed
after the run. No process or server was retained. No files outside the two
permanent paths were changed.

## Final state

Permanent changes are limited to this report and
`eng/verification/portable-available-controls.ps1`. The wrapper's four probes,
markers, and expected tuple are all retained in the final source.
