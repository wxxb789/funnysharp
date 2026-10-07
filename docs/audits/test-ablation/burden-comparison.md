# Test ablation burden comparison

## Comparable scope

Baseline `0c5416e` and source candidate `03435bb` run on the same Windows x64 host,
SDK 10.0.401 and .NET 10.0.12. The main solution command is unchanged:
`dotnet fsi build.fsx -- -p test`. The final source is also independently evaluated
through MSBuild Compile, native method discovery and pre-enumerated case discovery.
No whole Fact/Theory method or input partition is removed.

| Measure | Baseline | Source candidate | Interpretation |
| --- | ---: | ---: | --- |
| Current source xUnit methods | 1440 | 1440 | Detection partitions retained; two POSIX-only source obligations remain separate |
| Named executable checks | 14 | 14 | Counts do not establish detector quality |
| Main solution executions | 2703 | 2703 | Both zero failed/skipped |
| Package-consumer executions | 50 | 50 | All 14 vertical steps passed |
| Current external kit executions | 123 | 123 | Both eleven current-package kits; final 44 restore/build/test steps passed |
| Compatibility scenarios | 6 | 6 | Actual smoke/trim/AOT consumers passed |
| Compiled/trusted source files | 206 | 206 | Same independently enumerated closure |
| Physical source LOC | 48350 | 48642 | +292; not claimed as a reduction |
| Source bytes | 2069099 | 2082529 | +13430; controlled concurrency adds necessary fixture code |
| Helper-only LOC lower bound | 2018 | 2089 | +71; mixed-file ownership is separately assessed |
| Helper-only bytes lower bound | 83826 | 86535 | +2709 |
| Main runner duration | 41.999 s | 27.288 s | One observation per candidate, not a causal speedup claim |
| Main harness pipeline duration | 75.113 s | 46.785 s | Same command; build/cache variability remains |
| Main command outer duration | not separately captured | 62.432 s | Includes FSI startup; not compared to the harness-only value |
| Vertical runner duration | 2.598 s | 1.945 s | Single observation; package/restore scope is separate |
| Compatibility harness duration | 103.339 s | see final receipt | Do not compare a native command outer clock to harness-only duration |

These measurements do not establish a repeatable throughput improvement. The
maintenance reduction is instead checkable in the actual diff and executed
experiment reports: seven scheduling timer sites are gone; six direct empty BCL
contrasts are gone; diagnostic and ordinary success-text pins are gone; an invalid
unequal-hash requirement and shallow digest-length oracle are gone; a redundant
composition completion continuation and scratch Git commit/configuration are gone.
Actual verdicts, source values, exception identity/token/stack/status, code-fix
semantic operations, resource disposal, order and concurrency bounds remain.

The fixture cost is a deliberate tradeoff: the causal concurrency Contract/tests
add 243 net lines, while reducing clock assumptions and exposing entered/release/
returned-task completion. State overlap similarly adds controls instead of making
the same outputs depend on ThreadPool scheduling luck. There is no deletion quota
and no claim that fewer LOC or fewer tests alone proves an improvement.

The final scan of current tests and trusted evaluation sources found no remaining
positive fixed scheduling delay. Remaining `Task.Delay(Timeout.InfiniteTimeSpan,
token)` sites are cancellation signals; `Task.Delay(0)` is an analyzer source
snippet, not a timing gate. The vertical API `SimulatedDelay` intentionally models
the measured delay/cancellation workload and is not a test wait.

## Exact receipts and independent joins

Baseline receipts are described in `baseline.md`. Final native command receipts,
actual package sources, compiler inputs and cleanup are under the session's
`a1/final-gates/` and the candidate's ignored
`artifacts/test-ablation/external-final-kits/` directories. The unmodified strict
validator returns 1454/1454 test/check rows, 2753 joined Windows native cases,
756 helper/usage rows, zero missing/duplicate cases and an empty problems array.
All 755 baseline helper mappings survive; the extra row owns new DrainAsync
cleanup. The helper validator remains explicitly file-source coverage only.

The frozen R9 packet is a separate input snapshot. The current-source invocation
correctly rejects `Current source/protocol bytes changed` after test-source oracle
changes; its catalog binds historical test witness source, not only shipping APIs.
The same current control script against clean recorded baseline `0c5416e` passes
56 controls with four disposable fixture writes, fixture removal and no network.
This is preserved as fixed-snapshot control evidence, not relabeled acceptance of
the changed candidate. Frozen catalogs/recordings, source/performance contracts and
the guard itself are unchanged; no new performance recording is claimed.
