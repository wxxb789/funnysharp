# Independent test inventory and disposition join

## Result and evidence boundary

At baseline `0c5416e37e06db17c30c014659601d0c463b973a`, the independently evaluated Compile/source universe contains **1,440 xUnit methods**: 1,330 Windows solution methods, 50 package-consumer vertical-slice methods, two additional POSIX-only harness methods, 43 current evaluation-oracle methods, and 15 function-grammar comparison methods. `inventory.json` is the method universe, not a disposition authoring file.

The five available built Windows runners independently discover **1,380 methods and 2,753 cases**. Every executable method and every pre-enumerated case joins to exactly one source method. All five method joins and all five case joins have zero omissions, zero unexpected methods, and zero static InlineData multiplicity differences. The four solution assemblies account for **2,703 cases**; vertical slice accounts for **50**, not the stale 49 in its `AGENTS.md`.

The parent ran the unchanged full solution baseline: 2,703 succeeded, zero failed, zero skipped, 41s999ms. It also reported the vertical-slice consumer baseline passing 50 cases with zero skips. This worker did not repeat either execution, build, restore, or runner discovery. It reused the independent executable receipts and performed read-only MSBuild evaluation, lexical source extraction, and CSV joins.

Evidence directory:

```text
Q:/repos/funnysharp/.omo/evidence/ulw/01a111dc-d4c4-7a26-bedd-b50ee7f966db/G001-outcome-on-funnysharp-branch-refacto/a1/
  runtime-discovery.json
  runtime-case-discovery.json
  inventory/compile-inputs.json
  inventory/inventory.py
  inventory/validator-controls.json
  inventory/lane-snapshot-join.json
```

Both runtime receipts report all five discovery commands exiting 0 without stderr. Discovery is not test execution. The method receipt identifies .NET SDK 10.0.401/runtime 10.0.12 on Windows x64, with xUnit v3 in-process runner 4.0.0. `--help` usage exiting 2 is not a test failure and was not used as validation evidence.

## Enumeration method

Two approaches were considered: counting attributes by recursive regular expression, or evaluating actual Compile inputs and then reconciling token-aware source declarations with independent runner metadata. The second approach was used. The first counts `[Fact]`/`[Theory]` inside Roslyn fixture strings, historical payloads, and disabled platform regions as if they were locally executable tests.

The source parser first blanks comments and ordinary, verbatim, raw/interpolated-raw, and F# triple-quoted strings while preserving offsets and newlines. Only then does it extract declaration attributes and exact method names. A callable `selfcheck` proves fake attributes inside raw strings, verbatim strings, InlineData strings, and comments are excluded; InlineData multiplicity, F# double-backtick names, nested module identity, and POSIX separation survive. This is a deliberately bounded source extractor, not a replacement C#/F# compiler. Exact bidirectional compiled joins falsify omissions on the five available Windows assemblies.

MSBuild was invoked with `-getItem:Compile` and `-getProperty:TargetFramework,OutputType,IsTestProject,DefineConstants,AssemblyName`; no target was requested. The receipt captures evaluated source inputs, links, and assembly properties for eleven current projects. SDK-generated `obj`/`bin` payloads are excluded. The F# project explicitly lists 22 Compile sources. Evaluating it with `-p:OS=Unix` exits 0, retains 22 inputs, and changes `DefineConstants` from `TRACE;DEBUG` to `TRACE;POSIX;DEBUG`.

Canonical xUnit identity is `relative/source/path::ExactMethodName`, with `/` separators and only F# double-backtick delimiters removed. Class/module-qualified runtime identities are retained separately. `line` is the declaration line, not its preceding attribute line. For example, the live nested F# method is `FunnySharp.Harness.Tests.EvaluationTests+EvaluationOracle+DeclarationTests.EveryOriginalDeclarationIsPresent`, source line 1123. Its five MemberData cases were independently discovered; they are not five separate disposition rows.

`runnerCasesWindows` retains every discovered display case under its source method. Display strings are metadata, not guaranteed unique xUnit case IDs: multiplicity is retained rather than collapsing equal display text into a set. The join uses exact runtime method identity followed by the case argument delimiter, not a method-name substring search.

## Current activity, not historical copies

- `FunnySharp.slnx` includes the four solution test projects. Their evaluated `IsTestProject=true`, `OutputType=Exe`, and assembly identities agree with the real runners.
- `eng/harness/Evaluation.fs:634` defaults the oracle root to current `eng/evaluation/tasks`; lines 646 and 684 select and copy current trusted `*.cs` files into the transient kit. Each template is an SDK test executable with default Compile inclusion. The five current oracles therefore remain active even though templates alone intentionally contain no solution implementation. Both styles share the same source obligations; the inventory does not double-count them.
- `eng/evaluation/comparisons/function-grammar/FunctionGrammarComparisons.csproj` is a current package-referenced xUnit executable outside the solution. Its evaluated Compile inputs are `Contract.cs`, both independent workflow implementations, and `WorkflowEquivalenceTests.cs`. No built executable was available in the supplied root runner surface, so its 15 methods/37 static cases are source evidence, not claimed runtime discovery.
- `eng/harness/Compatibility.fs:79-120` resolves the two live package-consumer projects and six accepted smoke/trimmed/NativeAot scenarios. Core `Program.cs` checks Option, Result, Validation, Effect, and awaits the two named cancellation checks before the success marker. HTTP `Program.cs` executes four actual `IResult` mappings against host services. These are executable smoke checks, not xUnit cases or six independent source methods per project. Their execution was not repeated.
- `eng/harness/VerticalSlice.fs` resolves and runs the package consumer tests, both composition checks, and measurements. API/baseline `Program.cs` build before their `--verify` markers. Measurements `Main --verify` is only a marker branch; normal execution seeds both hosts and compares ten scenario status/payload pairs. `Same` and `SeedAsync` are separately inventoried named verification/check methods, with their current callers recorded.
- `eng/verification/measure-offline.ps1:14` calls current `offline-packet-controls.fsx`, which `#load`s current `CurrentPacket.fs`. `portable-available-controls.ps1` calls the current portable successor and the explicitly historical comparison entry. These current callable control scripts remain checks; historical verifier bodies themselves are excluded from the mutable test universe. Their scripts were inspected, not executed by this worker.

Frozen `eng/evaluation/results/**`, study snapshots/alternate historical payloads, archived goals, generated build payloads, and artifact copies are excluded. A historical solution body cannot become an active test merely because its text contains a current method name.

## Method/case counts and source burden

Counts below are source methods and independently discovered Windows cases where available. `static` means source InlineData/Fact multiplicity, not runtime execution. LOC is physical source lines including blanks/comments; bytes are actual file bytes including the checked-out line endings. Helper columns count files with no xUnit method in the test/trusted-comparison input surface, not arbitrary helper lines inside mixed test files.

| Project/surface | Windows methods | Windows cases | POSIX source methods | Source LOC | Source bytes | Helper-only LOC | Helper-only bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| FunnySharp.Tests | 814 | 1762 discovered | 814 | 24662 | 1040146 | 105 | 2721 |
| FunnySharp.Analyzers.Tests | 84 | 168 discovered | 84 | 2305 | 76684 | 327 | 12639 |
| FunnySharp.AspNetCore.Tests | 27 | 28 discovered | 27 | 1287 | 63515 | 41 | 1105 |
| FunnySharp.Harness.Tests | 405 | 745 discovered | 407 | 11587 | 557303 | 97 | 3261 |
| FunnySharp.VerticalSlice.Tests | 50 | 50 discovered | 50 | 1993 | 81803 | 555 | 20289 |
| FunctionGrammarComparisons | 15 | 37 static | 15 | 854 | 33971 | 339 | 13007 |
| evaluation/aspnetcore | 9 | 9 static | 9 | 348 | 13287 | 82 | 2663 |
| evaluation/async-streams | 8 | 8 static | 8 | 225 | 8976 | 28 | 1127 |
| evaluation/business-outcomes | 8 | 8 static | 8 | 191 | 6688 | 55 | 1667 |
| evaluation/collections | 9 | 9 static | 9 | 242 | 8953 | 23 | 863 |
| evaluation/concurrency | 9 | 9 static | 9 | 286 | 10687 | 104 | 3637 |
| Compatibility.Core smoke | 0 xUnit | not xUnit | 0 xUnit | 170 | 5611 | mixed-file fixtures | mixed-file fixtures |
| Compatibility.AspNetCore smoke | 0 xUnit | not xUnit | 0 xUnit | 80 | 2552 | mixed-file fixtures | mixed-file fixtures |
| VerticalSlice.Api consumer | 0 xUnit | not xUnit | 0 xUnit | 2585 | 100145 | consumer implementation | consumer implementation |
| VerticalSlice.Baseline consumer | 0 xUnit | not xUnit | 0 xUnit | 1198 | 44648 | independent implementation | independent implementation |
| VerticalSlice.Measurements | 0 xUnit | ten comparison scenarios | 0 xUnit | 406 | 16076 | mixed-file fixtures | mixed-file fixtures |

The 206 unique evaluated/trusted source files total **48,350 LOC / 2,069,099 bytes**. The 25 unique helper-only source units, including the separately loaded 331-line `CurrentPacket.fs`, total **2,018 LOC / 83,826 bytes**. Per-project figures include linked files in each consuming project; the unique source summary counts a physical file once. `TestRepositoryRoot.cs` is linked into core and analyzer tests. HTTP links the core `CountingValueTaskSource.cs`; these are helper inputs, not extra test methods.

These helper-only totals are a lower bound on total fixture burden, not a claim that mixed-file helpers cost zero. `files[].declaredSymbols` records named fixture types and F# bindings in mixed files; it does not promise an exhaustive inventory of every local C# function, property, or closure. Both independent function-grammar implementations are counted as helper-only input units for bookkeeping, but that classification is not a redundancy finding or deletion recommendation. No before/after source saving or runtime saving is inferred.

The two POSIX-only methods are `ToolingVerifyTests.fs::RealRunnerUsesFakeDotnetAndForwardsOffline` and `ToolingVerifyTests.fs::RealRunnerPropagatesChildFailure`, under `#if POSIX`. Both remain required source dispositions. On POSIX, the harness has 407 source methods and 742 statically enumerated cases plus its one MemberData method; actual POSIX cases are not discovered here. Windows has 740 static cases plus the independently discovered five MemberData cases = 745. No POSIX test is described as a Windows skipped case.

There is also a distinct runtime-conditional baseline obligation: `VerticalSliceTests.fs::AHungStepFailsTheToolInsteadOfHangingIt` is compiled and counted on Windows at `0c5416e`, but its Windows branch returns unit without running the POSIX timeout assertions. It is not an xUnit skip and must not be credited as an executed Windows timeout detector merely because the overall baseline passed. This differs from the two compile-excluded POSIX methods above.

## Callable validator and negative controls

The Python CLI uses only the standard library and writes only stdout. It computes no SHA, adds no dependency/framework, and creates no production disposition. Exact test CSV header:

```text
test_id,source,line,disposition,obligation,fault_class,independent_owner,rationale,experiment
```

Exact helper CSV header:

```text
helper_id,source,symbol,disposition,obligation,independent_owner,rationale
```

The validator rejects missing/duplicate method rows, unknown test IDs, source/line mismatches, malformed headers/fields, and duplicate helper IDs. `--require-helpers` additionally requires disposition coverage for each helper-only source file. Helper IDs are unique author-supplied IDs; the task did not prescribe a canonical helper naming rule, so the validator does not invent one. It checks helper source existence and nonempty symbol/obligation fields, but **does not prove per-symbol completeness in mixed files or the semantic adequacy of dispositions**.

`checks[]` is separate from canonical xUnit `tests[]`. Non-method entries use `check_id=source::<top-level>`, never pretend that `<top-level>` is an exact C#/F# method name. `--include-checks` explicitly accepts and requires those typed entry/check rows in the test-header ledger. Without that flag, only canonical xUnit methods are accepted. The inventory has fourteen such check entries, including the API/baseline source wrappers as well as their named predicates; this is bookkeeping, not fourteen xUnit cases.

Five disposable fixture controls were executed against the final validator:

| Control | Expected/actual exit | Observable result |
| --- | --- | --- |
| Two complete method rows + helper source row | 0 / 0 | `ok: true`, no problems |
| Remove F# method row | 1 / 1 | `missing disposition: fixture/Real.fs::name with spaces` |
| Duplicate C# method row | 1 / 1 | `duplicate disposition (2): fixture/Real.cs::Alpha` |
| Remove helper source row | 1 / 1 | `missing helper-source disposition: fixture/Support.cs` |
| Pass helper file twice | 1 / 1 | `duplicate helper disposition (2): fixture/Support.cs::<file>` |

`validator-controls.json` preserves exact fixture contents, commands, stdout-equivalent parsed results, exits, and empty stderr. The five disposable files were cleaned after capture. The successful fixture is not a claim that unfinished real lane files are complete.

The captured lane snapshot contained ten CSV files, 883 method/check rows, and 611 helper rows. With `--include-checks --require-helpers`, the validator expected 1,454 rows and exited 1 for **571 missing dispositions**, covering 1,500 still-unjoined Windows cases. It joined 1,253 Windows cases. There were no unknown IDs, duplicate rows, line mismatches, malformed headers, or uncovered helper-only source files in that captured snapshot. This is an in-progress snapshot; `lane-snapshot-join.json` lists every missing identity and the exact input files. Lane authors own all changes to their CSVs.

## Reproduction

From PowerShell, set the exact working and evidence paths:

```powershell
$repo = 'Q:/repos/funnysharp/.omo/worktrees/test-ablation-g001'
$evidence = 'Q:/repos/funnysharp/.omo/evidence/ulw/01a111dc-d4c4-7a26-bedd-b50ee7f966db/G001-outcome-on-funnysharp-branch-refacto/a1'
$scratch = "$evidence/inventory"
$baseline = 'Q:/repos/funnysharp' # Still clean at 0c5416e when baseline-boundary.json was captured.

# Evaluation only; repeat for each project key in compile-inputs.json.
dotnet msbuild "$repo/tests/FunnySharp.Tests/FunnySharp.Tests.csproj" -getItem:Compile -getProperty:TargetFramework,OutputType,IsTestProject,DefineConstants,AssemblyName
dotnet msbuild "$repo/tests/FunnySharp.Harness.Tests/FunnySharp.Harness.Tests.fsproj" -p:OS=Unix -getProperty:DefineConstants -getItem:Compile

# Generates JSON on stdout; persist only through apply_patch if updating the artifact.
# --root must be a clean checkout of the exact discovery baseline, not a later HEAD.
python "$scratch/inventory.py" generate --root $baseline --compile "$scratch/compile-inputs.json" --runtime "$evidence/runtime-discovery.json" --cases "$evidence/runtime-case-discovery.json"
python "$scratch/inventory.py" selfcheck

# All real lane files; a nonzero exit is expected while authors are still working.
$csvs = @(Get-ChildItem "$repo/docs/audits/test-ablation/lanes/*.csv" | Sort-Object FullName | ForEach-Object FullName)
python "$scratch/inventory.py" validate --inventory "$repo/docs/audits/test-ablation/inventory.json" --include-checks --require-helpers @csvs
```

To reproduce disposable controls, recreate only the five named fixture inputs from `validator-controls.json.fixtures` with `apply_patch`, run each recorded command, compare its exit and `problems`, then delete those five files with `apply_patch`. No production lane may be replaced by a generated all-retain fixture.

## Limits and blockers

POSIX compiled discovery is unavailable on this Windows host. Evaluation and function-grammar compiled discovery are unavailable in the supplied built runner surface; no historical execution receipt is promoted to current runtime evidence. The exact Windows method/case cross-check is complete for all five available assemblies. Full-solution and vertical-slice executions are parent evidence, not executions by this worker.

Python `basedpyright` LSP diagnostics could not run because `basedpyright-langserver` is not installed. No installation was made. The CLI was executed successfully, its lexical controls passed, its negative controls rejected the intended faults, and inventory generation exited 0 with every runtime join clean.

Real disposition completion remains outside this worker's authoring scope. The snapshot is deliberately fail-closed. This worker changed only this report, `inventory.json`, and the assigned scratch directory, using `apply_patch`; source, tests, frozen evidence, other reports, and git history remain read-only. No commit was made.

### Concurrent worktree advancement

After the baseline inventory and runner joins had been captured, final read-only status inspection observed the shared worktree at `141f9d04adecc35a3851c6ca1d6812d6aa9f32ba`. The only changed baseline Compile source in `git diff --name-only 0c5416e HEAD -- tests eng Directory.Build.props Directory.Build.targets FunnySharp.slnx` was `tests/FunnySharp.Harness.Tests/VerticalSliceTests.fs`. Its change enables the hung-step detector on Windows and adds eight physical lines; it does not add or remove an attributed method. This is another session's change and was neither reverted nor authored here.

The inventory remains explicitly the `0c5416e` baseline: its old source line/LOC/byte values and execution receipts are not a verification of the later method body. The generator now refuses a later HEAD or tracked input edits with exit 2 instead of labeling fresh source bytes with stale discovery provenance. Reproduction of baseline generation requires an existing clean `0c5416e` checkout at `--root`; the disposition validator can still consume the captured baseline inventory without executing or rebuilding that checkout. The mismatch rejection was exercised on the advanced shared worktree and captured in `inventory/baseline-boundary.json`.

The same guarded generator also ran against the unchanged root checkout `Q:/repos/funnysharp`, exiting 0 with all original counts and both sets of runner joins unchanged. This was source inventory generation, not a duplicate baseline build/test run. The baseline runtime-conditional timeout obligation is separately recorded in `inventory.json.runtimeConditionalObligations`.
