# Current-test ablation baseline

The unchanged integration baseline is `refactor/code-simplify` at
`0c5416e37e06db17c30c014659601d0c463b973a` (Git tree `c232a99`). These receipts
establish the starting behavior and cost. They do not prove that an ablation
preserves independent fault detection.

## Environment and measurement boundaries

The host is Windows x64 with 16 exposed CPU cores. `global.json` specifies
SDK `10.0.400` with `latestPatch`; the actual SDK is `10.0.401`, with .NET and
ASP.NET Core runtime `10.0.12`. Warnings-as-errors and normal package auditing
remain enabled.

Runner duration, assembly duration, complete pipeline duration, and consumer
restore/publish duration are different measurements. Each observation below is
one execution, not a demonstrated causal speedup. Maintenance reduction can be
established independently from source/helper structure and detector ownership.

## Main solution

```text
dotnet fsi build.fsx -- -p test
```

The command exited `0`: **2,703 succeeded, 0 failed, 0 skipped**. Runner duration
was `41.999 s`; complete harness duration was `75.113 s`. Assembly durations
were `3.975 s` for ASP.NET Core tests, `6.709 s` for core tests, `10.468 s` for
analyzer tests, and `41.048 s` for F# harness tests. This command does not run
the current outside-solution consumer or evaluation tests.

The primary-session receipt is
`.omo/evidence/ulw/01a111dc-d4c4-7a26-bedd-b50ee7f966db/G001-outcome-on-funnysharp-branch-refacto/a1/baseline-solution.md`.

## Package-consumer vertical slice

The initial normal-feed invocation failed with `NU1301` and TLS
`HandshakeFailure` during restore. The failure remains in
`artifacts/test-ablation/baseline-vertical/vertical-slice-results.json`.
Successful `--no-build` marker output later in that failed attempt is not
fresh-candidate acceptance evidence.

The existing supported feed option recovered the complete gate:

```text
dotnet fsi build.fsx -- -p vertical-slice --output artifacts/test-ablation/baseline-vertical-mirror --package-feed https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json
```

The command exited `0`. All 14 steps succeeded: pack, four restores, four builds,
three application/measurement markers, measurements, and consumer tests.
The consumer suite ran **50 succeeded, 0 failed, 0 skipped**, with runner
duration `2.598 s` and assembly duration `1.967 s`. The older `49` count in the
subtree knowledge file is not the current discovery count.

The structured receipt is
`artifacts/test-ablation/baseline-vertical-mirror/vertical-slice-results.json`.
The consumer projects retain their package references and consume freshly
packed `FunnySharp` and `FunnySharp.AspNetCore` version `0.2.0`. No test or
measurement switch was skipped.

## Compatibility executables

```text
dotnet fsi build.fsx -- -p compatibility -PackageDirectory artifacts/test-ablation/baseline-vertical/feed -OutputDirectory artifacts/test-ablation/baseline-compatibility-mirror -PackageFeed https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json -Scenario CoreSmoke,CoreTrimmed,CoreNativeAot,AspNetCoreSmoke,AspNetCoreTrimmed,AspNetCoreNativeAot
```

The command exited `0` with `Succeeded: true` and **6/6 scenarios Passed**.
Both packed-package consumer surfaces ran as smoke, trimmed, and Native AOT
executables, including their actual assertion bodies. The complete harness
duration was `103.339 s`.

| Scenario | Observed elapsed time |
| --- | ---: |
| CoreSmoke | 11.289 s |
| CoreTrimmed | 11.151 s |
| CoreNativeAot | 25.404 s |
| AspNetCoreSmoke | 7.587 s |
| AspNetCoreTrimmed | 19.656 s |
| AspNetCoreNativeAot | 28.044 s |

The structured receipt is
`artifacts/test-ablation/baseline-compatibility-mirror/compatibility-results.json`.
Its consumer feed contains packages freshly packed from the unchanged baseline
in the first attempt; the earlier remote restore failure did not fail the pack.

## Current evaluation and grammar tests

Fresh disposable package consumers executed all five current trusted evaluation
areas in both idiomatic and FunnySharp styles, plus the current function-grammar
comparison. All **123 cases passed, 0 failed, 0 skipped** after use-lock restore,
locked restore and actual Release build. The FunnySharp inputs use the current
`0.2.0` packages; the unchanged normal evaluation templates still pin `0.1.0`.
That transient current-package verification choice is explicit and does not
rewrite historical templates, results or study records.

See `experiments/external-baseline.md` and
`artifacts/test-ablation/external-baseline-kits/summary.json` for every command,
source closure, package source, count and process receipt. This gives **2,876
current local xUnit case executions** across solution, vertical slice and the
external matrix, plus the six separately counted compatibility scenarios.

## Scope and remaining proof

The independent baseline inventory has 1,440 source xUnit methods, including
two POSIX-only harness methods, plus 14 separately typed executable checks.
All 1,454 entries now join to reasoned dispositions; all 2,753 cases from the five
available Windows runners join without omissions. There are 755 helper/usage
rows. Source-file helper coverage is verified; it is not a claim that a simple
validator proves every mixed-file helper's semantics. Historical copies remain
explicitly excluded. Fault correspondence and final changed-tree acceptance are
still separate from these positive baseline gates and disposition joins.

The confirmed Windows no-op in `AHungStepFailsTheToolInsteadOfHangingIt` is a
baseline detection defect, despite the main suite being green. Its isolated
experiment must demonstrate old false-green and corrected fault rejection;
the final suite will be re-run after any accepted test changes.

Cleanup: all three parent gate commands completed and their native monitor
processes exited. Package/build outputs and structured receipts are retained in
ignored artifact directories; no server or listening port is retained by these
gates. Historical outputs and worktrees were not deleted or rewritten.
