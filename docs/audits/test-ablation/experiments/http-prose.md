# HTTP exception-prose ablation

## Scope and contract

This experiment removes exactly five English `Assert.Equal(..., exception.Message)` checks from
`UnitResultHttpResultExtensionsTests.cs`: three checks for the uninitialized sync, `Task`, and
`ValueTask` mappings, and two checks for null/statusless `ProblemDetails`. The three
`InvalidOperationException` type checks remain. Ordinary fault reference identity, all eight
eager `ArgumentNullException`/`ParamName` partitions, null/statusless problem rejection, null
success-result rejection, and all HTTP wire/status assertions remain. The product contract
promises semantic exception types and HTTP behavior; it does not promise human English wording.

The removed obligations are five wording dependencies only. Carrier default
messages promised by current guides retain their independent core owners;
the repeated HTTP fragments are not unique checks. No structured token or wire payload
assertion was removed. The independently surviving observations are exception type, exception
identity, eager timing, parameter names, null/status validation, HTTP status, content type,
serialized fields, mapper-call counts, and success result behavior.

## Initial worker controls and mutants

Commands were run from `Q:/repos/funnysharp/.omo/worktrees/test-ablation-http-prose` with the
standalone xUnit v3 runner. `-class` and `-method` are the runner's actual filters; every run
used `-failSkips -failWarns`.

Healthy old control, before the edit:

```text
dotnet run --project tests/FunnySharp.AspNetCore.Tests/FunnySharp.AspNetCore.Tests.csproj -c Release --no-restore -- -failSkips -failWarns
exit 0; Total: 28, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0
```

Healthy candidate control:

```text
dotnet run --project tests/FunnySharp.AspNetCore.Tests/FunnySharp.AspNetCore.Tests.csproj -c Release --no-restore -- -class FunnySharp.AspNetCore.Tests.UnitResultHttpResultExtensionsTests -failSkips -failWarns
exit 0; Total: 7, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0
```

Representative product faults were applied only temporarily and restored immediately after each
run. These are behavior mutants, not compile-failure probes:

1. `HttpResultExtensions.ValidateProblem`: changed `problem is null || problem.Status is null`
   to `problem is null`. The exact guard test was run with:
   `dotnet run --project tests/FunnySharp.AspNetCore.Tests/FunnySharp.AspNetCore.Tests.csproj -c Release --no-restore -- -method FunnySharp.AspNetCore.Tests.UnitResultHttpResultExtensionsTests.UnitResultGuardsRejectNullMappersAndInvalidProblems -failSkips -failWarns`.
   Result: `exit 1`, `Total: 1, Errors: 0, Failed: 1`; `Assert.Throws` reported that no
   exception was thrown for a statusless problem.
2. `UnitResult<TError>.ThrowIfUninitialized`: changed its state condition to `if (false)`.
   The exact uninitialized test was run with:
   `dotnet run --project tests/FunnySharp.AspNetCore.Tests/FunnySharp.AspNetCore.Tests.csproj -c Release --no-restore -- -method FunnySharp.AspNetCore.Tests.UnitResultHttpResultExtensionsTests.UninitializedUnitResultThrowsFromSyncAndAwaitedMappings -failSkips -failWarns`.
   Result: `exit 1`, `Total: 1, Errors: 0, Failed: 1`; the first expected
   `InvalidOperationException` was absent.
3. The eager `Task<UnitResult<TError>>` mapper guard was temporarily removed. The exact guard
   test used the command in item 1 and failed with `exit 1`, `Total: 1, Errors: 0, Failed: 1`;
   `Assert.Throws` reported no exception for the null mapper. The guard was restored before
   the next action.

These failures demonstrate candidate detection of invalid `ProblemDetails`, uninitialized
mapping, and missing eager mapper validation. The candidate retains the same machine-observable
partitions as the old test; the removed prose itself is intentionally not replayed as a contract.
The ordinary fault identity assertion was retained and was covered by the candidate class run;
no wrapping mutant was left in the tree.

## Initial worker cleanup and verification

All temporary mutations were restored exactly in `src/FunnySharp/UnitResult.cs` and
`src/FunnySharp.AspNetCore/HttpResultExtensions.cs`. No source mutant remains. The final full
HTTP suite command is the 28-case command above; the candidate class run is green with zero
skips/failures. The parent session owns the final locked build, diagnostics, and full UnitResult
class gate. This report makes no timing or speed claim.

## Parent independent same-fault experiment

The parent completed the missing old/new comparisons in serial Release builds,
using the exact pre-edit test source or revised source, with identical source
faults. `artifacts/test-ablation/http-parent/run.ps1` preserves argument vectors,
working directory, build/test exits, bounded process completion and actual
stdout/stderr. Each class invocation discovers all seven cases, with zero
errors/skips/not-run; compile failures do not count as rejection.

| Fault | Old class failed / total | Initial revised class failed / total |
| --- | --- | --- |
| Accept statusless ProblemDetails | 1/7 | 1/7 |
| Bypass UnitResult uninitialized guard | 1/7 | 1/7 |
| Remove eager Task failure mapper guard | 1/7 | 1/7 |
| Reconstruct selected failure-mapper exception | 1/7 | **0/7: insufficient detector** |

Every fault build exits 0 with zero warnings/errors. Expected test rejections
exit 1. The final row was not accepted as proof: the old failure was an ordinary
validation-prose equality, not a caller mapper identity check. The existing
identity assertion covered a faulted input Task only; it could not detect a
selected mapper exception being reconstructed inside ToHttpResultCore.

The parent therefore added three direct prepared-exception Same assertions to
the existing boundary Fact: synchronous UnitResult, Task<UnitResult>, and
ValueTask<UnitResult> failure mapping. No wording pin or extra test method was
added. On the **same** preserved reconstruction mutant the corrected class
fails 1/7, exit 1, after a zero-warning/error build. This failure is now the
prepared exception reference comparison, not an invalid-problem message.
The prior revised false-green attempt remains retained at
`receipts/new-wrapping-parent/`; the actual new rejection is
`receipts/corrected-wrapping-parent/`.

Pure message-only control changes only the ordinary success-null and
invalid-problem diagnostic strings; carrier default messages explicitly
promised by current guides are untouched. The corrected class passes 7/7,
exit 0. On the same message-only change the old class fails 1/7, exit 1. The
old failure is its removed diagnostic string equality, not a behavior change.

## Parent final restoration and commit gate

The parent restored every shipping mutation, verified `git diff --exit-code -- src`
exits 0, and completed an explicit locked restore against the supported public
mirror. The final restored Release compiler reports zero warnings/errors and
exits 0. The entire HTTP suite passes **28/28**, Errors/Failed/Skipped/Not Run 0,
exit 0 (`0.701 s` standalone observation, not a speedup claim). The actual
receipt is `artifacts/test-ablation/http-parent/receipts/final-restored-parent/receipt.json`.
Every launched child has HasExited true and timedOut false.

The complete repository formatter exits 0 (`25.701 s`). Fresh CSharp LSP
diagnostics for the changed test file timed out at 3000 ms; this is reported as
unavailable, not zero errors. The final locked compiler/runtime proof above is
clean. Permanent edits are the single test file and this report; seven Fact
methods and all original input partitions remain. Shipping source, dependencies
and frozen files are unchanged. The final three added identity observations
replace an incidental prose detector with direct contractual behavior checks.
