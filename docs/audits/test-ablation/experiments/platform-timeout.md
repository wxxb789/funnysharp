# Platform timeout detector ablation

## Scope

This experiment checks that `AHungStepFailsTheToolInsteadOfHangingIt` executes a
real timeout path on Windows. The worktree baseline was `0c5416e`.

## Change

The Windows branch now launches the bundled `powershell.exe` with:

```text
-NoLogo -NoProfile -NonInteractive -Command [System.Threading.Thread]::Sleep([System.Threading.Timeout]::Infinite)
```

Only `restore-api` receives the real process. The other restore commands retain
the existing fake runner. POSIX retains its native `/bin/sh -c "sleep 60"`
child. The test continues to assert tool exit `1`, receipt status `fail`,
`restore-api` exit code `124`, and timeout diagnostics.

## Evidence

Environment: Windows x64, .NET SDK `10.0.401` (runtime `10.0.12`), xUnit.net
v3 In-Process Runner `v4.0.0+8bf043c053`.

Commands were run from `Q:/repos/funnysharp/.omo/worktrees/test-ablation-timeout`:

1. `dotnet restore tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj --locked-mode`
2. `dotnet build tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj --no-restore`
3. Baseline Windows no-op test, with the temporary production timeout return
   mutant (`ExitCode = 0`): **1 total, 0 failed, 0 skipped**. This is a
   false-green detector failure: the test did not exercise a Windows timeout.
4. Corrected test, with the same active mutant: **1 total, 1 failed, 0
   skipped**. The assertion observed `Expected: 1`, `Actual: 0`.
5. Production timeout branch restored exactly to `ExitCode =
   stepTimeoutReturnCode`; corrected focused test: **1 total, 0 failed, 0
   skipped**.
6. Corrected `*VerticalSliceTests*` class: **18 total, 0 failed, 0 skipped**.

The timeout process is killed recursively and waited on by `runStepProcess`
before the result is returned. The observed timeout receipt path therefore
includes child cleanup. Structural process cost is one real child/timeout
invocation after the correction instead of four (the prior POSIX-only shape's
four potential restore launches); this report makes no runtime-speed claim.

## Final state

Permanent changes are limited to this report and
`tests/FunnySharp.Harness.Tests/VerticalSliceTests.fs`. `eng/harness/VerticalSlice.fs`
has no remaining diff from the baseline production behavior.

## Independent parent checks

The parent inspected the actual diff and ran the corrected complete class:

```text
tests/FunnySharp.Harness.Tests/bin/Debug/net10.0/FunnySharp.Harness.Tests.exe -class "*VerticalSliceTests*" -noColor -failSkips -failWarns
FunnySharp.Harness.Tests Total: 18, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 1.690s
```

The command exited `0`. A separate Windows process inspection found
`timeout_fixture_children=0` and exited `0`; no infinite-wait child remained.
The locked solution restore and repository formatter gate also exited `0`:

```text
dotnet restore FunnySharp.slnx --locked-mode --source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json
dotnet fsi build.fsx -- -p format
dotnet format FunnySharp.slnx --verify-no-changes --no-restore
PIPELINE format is finished in 37424 ms
```

The F# language server (`fsautocomplete`) is unavailable. The actual locked
compiler build passed with zero warnings/errors; F# is not covered by the
repository's C# formatter. No language-server dependency was added for this unit.
