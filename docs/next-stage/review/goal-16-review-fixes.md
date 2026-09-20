# Goal 16 review fixes

Review: `20260919-195030-4ecddc6b`, reviewed commit `73b01f1`.

## Finding disposition

| Finding | Resolution |
| --- | --- |
| #1 | Replace retired Windows timing claims with an interpretation of the current generated concurrency table. Recheck after accepting the new observation. |
| #2 | Recalibrate four Linux-sensitive allocation ceilings from repeated measurements; independently validate the entire policy before accepting the new observation. |
| #3 | Describe the value-carrier pair tuple and combined-value forms separately from UnitResult output. |
| #4 | Restore the G16-5 heading and use G16-1..G16-10 in acceptance instructions. |
| #5 | Record all five UnitResult conversion renames in API decisions and reconcile the removed-signature statement. |
| #6 | Include Effect in the LINQ bridge assessment. |
| #7 (advisory) | Use neutral snippet counts in verifier fixtures instead of mirroring the real documentation count. |
| #8 | Pin original exception identity for Select, SelectMany binder/projector, and query syntax; also cover default Option short-circuiting. |
| #9 (advisory) | Share the identical synchronous ProbeEnumerable between Scan and Choose tests. |

Related review warnings are addressed by documenting streaming ValueAsync return shapes, reserving future *OrNone names explicitly, clarifying that Bind does not imply a query bridge, and compiling the documented instance-method-group composition shape. Async Scan tests also cover cancellation on a later pull and the AggregateAsync equivalence.

Current references affected by inserted documentation sections now use stable section anchors; historical pinned audit records retain their original citations.

## Allocation calibration

Three separate BenchmarkDotNet ShortRun invocations measured all four affected methods (both input sizes each). Source and protocol fingerprints match the prior approved observation. Host: AMD EPYC 7763, 1 physical / 2 logical cores, Linux x64, SDK 10.0.400, .NET 10.0.11. Some calibration overlapped local verification work; scheduler/load variability is part of these empirical observations. Three repetitions do not establish a statistical flake rate.

Policy v6 uses `ceil((max(run1, run2, run3, priorApproved) * 1.25 + 32) / 8) * 8` bytes. Retaining a larger prior approved observation avoids discarding existing evidence. No runtime or benchmark source changed.

| Method / input | Run 1 B | Run 2 B | Run 3 B | Prior approved B | Old budget B | New budget B |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `FunnySharpSelectParallelValueAsync[Count=1024]` | 458459 | 462802 | 473943 | 504056 | 572992 | 630104 |
| `FunnySharpFirstSuccessAsync[CandidateCount=4]` | 2751 | 2723 | 3088 | 2657 | 3184 | 3896 |
| `FunnySharpParallelOptionTraversal[Count=1024]` | 278739 | 266117 | 503605 | 284892 | 358824 | 629544 |
| `FunnySharpParallelValidationTraversal[Count=1024]` | 283325 | 336286 | 544678 | 343286 | 362000 | 680880 |

Calibration command (repeat with separate artifacts directories):

```shell
dotnet run --project benchmarks/FunnySharp.Benchmarks -c Release -- --filter '*FunnySharpSelectParallelValueAsync*' '*FunnySharpFirstSuccessAsync*' '*FunnySharpParallelOptionTraversal*' '*FunnySharpParallelValidationTraversal*' --artifacts <run-directory>
```

Receipt SHA-256 values (raw receipts and reports are in the local `/tmp/funnysharp-review-fixes/calibration-{1,2,3}/results/` artifact directories):

| Run | Receipt | SHA-256 |
| --- | --- | --- |
| 1 | `ConcurrencyBenchmarks-performance-receipt.json` | `860c51aa2a98bb64ef214b5a69758a6b2bcdedc2897673b9e200cdb8f431b50d` |
| 1 | `FirstSuccessConcurrencyBenchmarks-performance-receipt.json` | `b13a78fbc282f800021fc2d6f1eee75d8e2a23399ce99f5cc55d7ba197e773aa` |
| 1 | `ParallelTraverseConcurrencyBenchmarks-performance-receipt.json` | `9f7e0f4537d4acce5e64755c1462324d932df23530b50fd0f79ac8d701b3e6d0` |
| 2 | `ConcurrencyBenchmarks-performance-receipt.json` | `140ac4a589d62c13b031d5cbf998bb019a02e82eb1926f83a1c6a8e7eb23f764` |
| 2 | `FirstSuccessConcurrencyBenchmarks-performance-receipt.json` | `7e5c9b6977b99b73133d6622795c2cd1ba5f09de6d8f94822c256e4b13f1f44a` |
| 2 | `ParallelTraverseConcurrencyBenchmarks-performance-receipt.json` | `eb63ade19f995eba6f208d3ef4ea820c1dbbe4cfe106fac18ccb8521edc6d48f` |
| 3 | `ConcurrencyBenchmarks-performance-receipt.json` | `2dbec106136b68930ed97e58d136eea7fc56f8aece2619ac33da630b793162b6` |
| 3 | `FirstSuccessConcurrencyBenchmarks-performance-receipt.json` | `fc5f76caf65204f118c4bc7a20ae05ab961d7d257000eab28f56525e46fca96c` |
| 3 | `ParallelTraverseConcurrencyBenchmarks-performance-receipt.json` | `6da45e30971cd1f04e1183dbccf0ea63cb76627dd057032c0f096f738c9bf95f` |

Environment key: `8c8e4eb1992841b98347f23a9baba967960bc52eda242439099472f7805f59bd`.
Benchmark input fingerprint: `69587527894e7a80673b60858de46c2a50171eaa17302a744db130061b878260`.
Protocol fingerprint: `774ad64b9fb8ecafcbee3280a5c5ba6a6f7d4657625b989f6da6f738ce21d578`.

## Validation

- Local pre-check: restore, Release build, tests, examples, format verification, and documentation snippets passed. The explicit .NET test run passed all 485 tests with zero failures or skips.
- Python tooling: 126 tests ran; 125 passed, with one non-CI availability guard skipped. PowerShell documentation parity checks executed.
- Performance protocol: 14 tests passed.
- Release protocol: 31 tests passed.
- Benchmark semantic preflight passed.

## Independent acceptance

- Full-suite acceptance run: all 138 benchmarks executed on the same host class (24 min 59 s); `Verify-Performance.ps1` verified 138 included rows and 12 explicit exclusions across 11 receipts against the v6 budgets with zero over-budget rows. The proposed observation was applied to `eng/performance/baseline.json`, and the 9 generated performance documentation regions were regenerated and re-verified.
- Post-acceptance recheck: the `docs/concurrency.md` interpretive paragraph was rewritten against the accepted table (the accepted run flipped three directional claims: the Option coordinator is faster at 16 items, both coordinators now allocate more at 1,024 items, and first-success timing directions reversed at both candidate counts). The other eight guides' surrounding prose is direction-neutral and needed no change.
- Final gates on the accepted tree: the local pre-check passed all seven steps again (restore, build, test, examples, aspnetcore-examples, format, docs), the Python tooling suite passed with the same single availability-guard skip, the performance protocol passed 14, the release protocol passed 31, and the performance documentation verify plus benchmark semantic preflight passed after the observation swap.
