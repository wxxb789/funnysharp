# Goal 12 Release Evidence

Date: September 2, 2026

This record accompanies the reproducible checks in `docs/release-readiness.md`. Generated command
logs, package archives, publish directories, JSON inventories, and public API text are written under
`artifacts/` by the release verifier. This tracked summary records the release-relevant outcomes and
limits without treating transient build output as source control.

## Environment

| Item | Value |
| --- | --- |
| Source branch | `codex/goal-12-release-readiness` |
| Base commit | `4d0b744a30e996c9f2fcf507439fc591370d2dfb` |
| SDK | `10.0.400` |
| Host runtime | `Microsoft.NETCore.App 10.0.11` |
| ASP.NET Core runtime | `Microsoft.AspNetCore.App 10.0.11` |
| PowerShell | `7.6.4` on `.NET 10.0.10` |
| Operating system | Windows `10.0.26200`, x64 |
| Compatibility RID | `win-x64` |

`global.json` pins SDK feature band `10.0.400` with same-band patch roll-forward. The compatibility
run used the machine's configured Microsoft package-feed proxy because direct `nuget.org` TLS failed
before restore; the exact feed is recorded in the generated compatibility JSON.

## Build, Tests, And Examples

| Check | Result |
| --- | --- |
| Clean Release build | Passed, 0 warnings and 0 errors |
| xUnit v3 through Microsoft.Testing.Platform | Passed: 310 total, 310 succeeded, 0 failed, 0 skipped |
| Core executable examples | Passed: `FunnySharp examples passed.` |
| ASP.NET Core example `--verify` | Passed: endpoint mappings completed |
| Formatter | Passed with no changes |

The two test executables contributed 292 core tests and 18 ASP.NET Core tests. The first documentation
sample build intentionally failed on two unused local functions (`CS8321`); the sample host was fixed
without changing the displayed snippets, after which the full Release build passed.

## Documentation And API Inventory

| Evidence | Result |
| --- | --- |
| Primary C# fences | 20 snippets across 7 guides, each matched exactly to one compiled source region |
| Core XML documentation | 275 members; no member lacked `summary` or `inheritdoc` |
| ASP.NET Core XML documentation | 16 members; no member lacked `summary` or `inheritdoc` |
| Core public API | 33 exported types, 255 declared public members |
| ASP.NET Core public API | 1 exported type, 15 declared public members |

The generated API inventory preserves type names, generic parameter order, parameter order,
nullability visible through reflection, return carriers, and `CancellationToken` placement. Source
review found no required rename, generic reorder, or public overload expansion. `Result.TryAsync` and
`TryValueAsync` remain deliberately tokenless exception boundaries; callers pass cancellation inside
the supplied delegate.

## Package And Dependency Inventory

The canonical runner produces exactly two `.nupkg` and two matching `.snupkg` files. Both package archives contain
the repository README, generated XML documentation, and their `lib/net10.0` assembly. Both declare
the MIT license.

| Package | Dependencies |
| --- | --- |
| `FunnySharp` | Empty `net10.0` dependency group; no framework references |
| `FunnySharp.AspNetCore` | `FunnySharp` `0.1.0`; `Microsoft.AspNetCore.App` framework reference |

The release verifier records the final SHA-256 values and same-candidate dependency inventory in
`release-evidence/package-inventory.json`. The hashes are not duplicated in this tracked document,
which avoids changing the source fingerprint merely to copy generated values back into source.

## Trimming And Native AOT

The canonical compatibility run restores from its produced local packages, publishes self-contained
for `win-x64`, and executes the resulting application. Every scenario record is bound to the final
package versions and SHA-256 values. Compatibility restores intentionally consume the freshly packed
candidate rather than a committed lock: NuGet locks the content hash of a local package, while each
canonical pack creates a new candidate archive. The solution and tool projects still use committed
locks; compatibility reproducibility is bounded by the recorded package hashes, SDK/runtime, feed,
RID, and publish properties.

| Scenario | Rooting model | Result |
| --- | --- | --- |
| Core trimming | Complete `FunnySharp` assembly root, `TrimMode=full` | Passed |
| Core Native AOT | Representative closed generic Option/Result/Validation/Effect paths | Passed |
| ASP.NET Core trimming | Complete core and integration assembly roots, `TrimMode=full` | Passed |
| ASP.NET Core Native AOT | Closed generic mappings plus source-generated ProblemDetails JSON metadata | Passed |

The first direct run also provided useful negative evidence:

- A consumer without ASP.NET Core request services cannot execute `IResult`; the smoke now uses a
  slim application service provider.
- Trimmed/AOT ProblemDetails execution requires consumer-owned source-generated JSON metadata. The
  integration library does not install global serialization policy.
- Rooting every open generic member for Native AOT causes the .NET 10.0.11 compiler to synthesize
  unsupported deeply nested `ValueTuple` closures. Therefore the packages declare `IsTrimmable` but
  do not declare `IsAotCompatible`; only the recorded closed generic paths are verified.

These results do not imply validation on another OS, architecture, runtime patch, future .NET
runtime, or unexercised ASP.NET Core serialization policy.

## Benchmarks

The complete BenchmarkDotNet `ShortRun` suite executed 126 cases in 26 minutes 38 seconds on the
environment above. Every benchmark process exited successfully and the runner reported zero
remaining cases. The generated reports compare FunnySharp operations with their declared direct C#
or BCL baselines. Hyper-V and `ShortRun` limitations remain explicit; zero-measurement warnings on
sub-nanosecond baselines are not interpreted as performance wins or capacity guarantees.

## Canonical Command

The recorded run on this machine used its configured Microsoft package-feed proxy because direct
`nuget.org` TLS failed before restore:

```powershell
pwsh -NoProfile -File eng/Run-Release.ps1 `
  -OutputDirectory artifacts/release-run `
  -CompatibilityPackageFeed https://packagefeedproxy.microsoft.io/nuget/v3/index.json `
  -Clean
```

## Deferred Scope

First-party analyzers and a general discriminated-union implementation remain deferred. The product
contract records the current C# 15/.NET 11 status and the evidence required before either native
unions or a `net10.0` compatibility layer can be reconsidered.
