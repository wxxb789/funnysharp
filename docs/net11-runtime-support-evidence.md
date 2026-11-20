# Stable .NET 11 Runtime Support Evidence

This record supports the runtime-support item in [TODO](../TODO.md): stable `net11.0` support
alongside `net10.0` for the `FunnySharp` core and `FunnySharp.AspNetCore` packages, without
changing the existing .NET 10 contract, the committed public API, the core package's zero runtime
dependencies, or the Roslyn analyzer/code-fix surface.

Code identity is commit `e12ae4f` (`feat: add net11.0 support alongside net10.0`). The release
protocol ran from that commit with attempt id `net11-win-x64-2`; the receipts are retained under
`artifacts/release-candidate/e12ae4fe71597f01a4c4a7c742dae5c3ac127720/net11-win-x64-2/` (build
outputs are not tracked). This document is a documentation-only follow-up; the authoritative
release gate re-runs the same protocol on the final candidate.

## What Changed

- `FunnySharp` and `FunnySharp.AspNetCore` multi-target `net10.0;net11.0`; their package assets,
  dependency groups, and framework-reference groups are produced per lane.
- The main test suites (`FunnySharp.Tests`, `FunnySharp.AspNetCore.Tests`,
  `FunnySharp.Analyzers.Tests`) and the package-only compatibility consumers run on both lanes.
- `global.json` pins SDK `11.0.100` (`rollForward: latestPatch`); each lane pins its SDK-matching
  `Microsoft.NET.ILLink.Tasks` version (`10.0.12` for `net10.0`, `11.0.0` for `net11.0`).
- The release test verdict now requires both lanes, and the public API baseline compares both
  lanes against the committed baseline.
- F# harness sources were adjusted only where the .NET 11 compiler reports new nullness
  diagnostics as errors; no warning suppression was added.

## Verified Locally (Windows x64)

Host and toolchain: Windows x64 (`win-x64`), .NET SDK `11.0.100`, runtimes
`Microsoft.NETCore.App` 10.0.15 and 11.0.0, `Microsoft.AspNetCore.App` 10.0.15 and 11.0.0.

The gate uses only local package sources and a loopback version index:

```powershell
$env:POWERSHELL_TELEMETRY_OPTOUT = '1'
pwsh -NoLogo -NoProfile -File Q:/repos/cockpit/local/fs-bench/Invoke-OfflineGate.ps1 -AttemptId net11-win-x64-2
```

| Release step | Result | Evidence |
| --- | --- | --- |
| Locked restore | pass | `logs/02-restore.*` |
| Release build, warnings as errors | pass, `0 Warning(s)` / `0 Error(s)` | `logs/03-build.*` |
| Tests, both lanes | pass, 5,515 passed / 0 failed / 0 skipped | `logs/04-test.*` |
| Examples and ASP.NET Core examples | pass | `logs/05-*`, `logs/06-*` |
| Pack | pass | `packages/` |
| Format and documentation snippets | pass | `logs/08-format.*`, `logs/10-docs.*` |
| Public API baseline | pass, 230 types / 1,978 members across 4 assemblies (two packages, two lanes) | `logs/09-api-baseline.*` |
| Package-only compatibility, 8 scenarios | pass | `logs/11-compatibility.*`, `compatibility-run/compatibility-results.json` |

`compatibility-results.json` records, for every scenario, the SDK (`11.0.100`), the selected
package assets, the target framework, the RID (`win-x64`), and the runtime frameworks found in the
published `runtimeconfig.json`.

## Package Assets And Dependency Groups

Both packed packages contain `lib/net10.0/<id>.dll` plus `.xml` and `lib/net11.0/<id>.dll` plus
`.xml`. The core package embeds both analyzer/code-fix assemblies under `analyzers/dotnet/cs` and
keeps both dependency groups empty. Each ASP.NET Core dependency group declares only the matching
`FunnySharp` version, and each lane's framework-reference group declares only
`Microsoft.AspNetCore.App`, exactly as the .NET 10 group did before.

The compatibility gate reads the archive entries and `.nuspec` directly and refuses to run any
consumer when a lane's DLL, XML, dependency group, or framework-reference group is missing, or
when a per-lane core dependency version is wrong. `FunnySharp.Harness.Tests` covers those negative
controls (missing `net10.0`/`net11.0` DLL or XML, missing `net11.0` dependency group, missing
`net11.0` framework-reference group, wrong `net11.0` dependency version); each exits 2 before a
consumer command runs. Package inventory, version, README/license, and analyzer defects are
rejected the same way.

## Package-Only Trim And Native AOT (Windows x64)

Every scenario references the packed `.nupkg` files only (no `ProjectReference`), restores with an
isolated cache, publishes with `--self-contained true` and `-r win-x64`, and runs the produced
executable. Trim and AOT analyzers run with the repository-wide `TreatWarningsAsErrors=true` and
no `NoWarn` or suppression additions; both scenario logs contain zero warning lines.

| Scenario | Lane | Outcome | Runtime in publish output |
| --- | --- | --- | --- |
| `CoreTrimmed` | `net10.0` | pass | `Microsoft.NETCore.App` 10.0.15 |
| `CoreTrimmedNet11` | `net11.0` | pass | `Microsoft.NETCore.App` 11.0.0 |
| `CoreNativeAot` | `net10.0` | pass | native image (no shared runtime recorded) |
| `CoreNativeAotNet11` | `net11.0` | pass | native image (no shared runtime recorded) |
| `AspNetCoreTrimmed` | `net10.0` | pass | Core 10.0.15 + ASP.NET Core 10.0.15 |
| `AspNetCoreTrimmedNet11` | `net11.0` | pass | Core 11.0.0 + ASP.NET Core 11.0.0 |
| `AspNetCoreNativeAot` | `net10.0` | pass | native image (no shared runtime recorded) |
| `AspNetCoreNativeAotNet11` | `net11.0` | pass | native image (no shared runtime recorded) |

Native AOT toolchains are selected per lane by the SDK (`Microsoft.DotNet.ILCompiler` `10.0.15`
and `11.0.0`, both from the local package source). The packages still make no `IsAotCompatible`
claim and the full-assembly-rooting limitation in the
[product contract](product-contract.md#trimming-and-native-aot-policy) is unchanged; Native AOT
remains validated with representative closed generic usages only.

## Platform Coverage Statement

Only `win-x64` was executed in this record. `linux-x64` and `osx-arm64` remain the existing
required release contexts in [release readiness](release-readiness.md), dispatched manually
through `.github/workflows/release.yml`; every such job runs the same two-lane restore, build,
test, pack, and compatibility protocol. No Linux or macOS result is claimed here.

## Reproduce

```bash
dotnet fsi build.fsx -- -p build
dotnet fsi build.fsx -- -p test
dotnet fsi build.fsx -- -p compatibility -PackageDirectory <packed-packages> -OutputDirectory artifacts/<sub> -RuntimeIdentifier win-x64 -PackageFeed <local-feed>
dotnet fsi build.fsx -- -p verify-api-baseline
dotnet fsi build.fsx -- -p verify-docs-snippets
```
