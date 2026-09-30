# Goal 23 Release Evidence

Date: September 29, 2026

This record maps every clause of the Goal 23 contract (`docs/goals/archive/0023-goal.md`, line 2) to
the evidence the candidate produced, and states what the candidate does not claim. The evidence
itself is the generated release bundle under
`artifacts/release-candidate/e742d2b09933c335746f6401acbe8d7952055b7b/goal23-local-1/`, which is
build output and therefore not tracked; the hashes below are recorded so the tracked record can be
checked against a bundle.

## Candidate Identity

| Item | Value |
| --- | --- |
| Branch | `goal-0023-release-hardening` |
| Commit | `e742d2b09933c335746f6401acbe8d7952055b7b` |
| Source tree | `d4fe7a0` (clean before and after the run) |
| Source fingerprint | 561 files, sha256 `bcdacbee4f5cc81acbac4efebfc9819616ab289f8e9226348c95cc213dc90bb5` |
| Attempt | `goal23-local-1`, mode `benchmarkSkipped` |
| SDK / runtime | .NET SDK `10.0.401` / `Microsoft.NETCore.App 10.0.12` |
| Host | Ubuntu 24.04.5 LTS, `linux-x64` |
| Packages | `FunnySharp` and `FunnySharp.AspNetCore` `0.1.0` (2 `.nupkg` + 2 `.snupkg`) |

Bundle hashes: `release-outcome.json` (succeeded `true`),
`execution-evidence.json` sha256 `12a9a8341fb86a51b492764578dd7efbfb609169f3ff0ff1276eae734a9f370b`,
`release-evidence/release-evidence.json` sha256
`0538a10cca3c60a21a4ed93bbdf20d9508a60c7128ad10ef743fe41021677844`, and
`release-evidence/compatibility/compatibility-results.json` sha256
`49686f63a968038a84b13c34418fcb3131a1a39a9973b45a010d59cec138fbad`.

## Clause Coverage

| Contract clause | Evidence | Artifact |
| --- | --- | --- |
| Release-quality preview candidate whose packages, public API, documentation, and evidence express the accepted constitution, without expanding scope during hardening | The release audit passed every material check (`succeeded: true`, `failures: []`); no public member was added, removed, or changed - the committed baseline still matches the built surface | `release-evidence/release-evidence.json`; `release-evidence/public-api.txt`; `eng/api-baseline/` |
| Stable APIs have intentional names, generic parameter order, nullability, exception and cancellation contracts, sync/async relationships, XML documentation, and compatibility commitments | The baseline records every exported type and declared member with generic parameter order, parameter order, nullability, return carrier, and `CancellationToken` placement; the documentation inventory reports 2 files and 500 members with no member lacking documentation; the versioning page states the compatibility commitments | `eng/api-baseline/FunnySharp.public-api.txt`, `eng/api-baseline/FunnySharp.AspNetCore.public-api.txt`; `release-evidence/xml-documentation.json`; `docs/versioning.md` |
| Experimental APIs are unmistakable and do not silently acquire the stability promise | Every experimental member carries `[Experimental("FS####")]` with its diagnostic ID, is listed in the tracked inventory, and the versioning page states it carries no compatibility promise; the frozen curation tests enforce the tier split | `docs/stability-inventory.md`; `docs/versioning.md`; `tests/FunnySharp.Tests/AdvancedPatternCurationTests.cs` |
| Quick starts and primary guides present the canonical Option, Result, UnitResult, Validation, functional-grammar, collection, async/streaming, concurrency, advanced-pattern, analyzer, performance, and ASP.NET Core usage, and every documented sample compiles | The quick start joined the byte-exact snippet contract as the eleventh primary guide, so all 56 samples across 11 guides are compiled from `examples/FunnySharp.DocumentationSamples` and byte-compared; the release audit ran the same verifier and passed it; a fourteen-row coverage map shows every named area resolving to an existing, README-linked guide | `docs/quick-start.md`; `release-evidence/documentation-snippets.log`; the release audit's `Documentation snippets` check |
| Package dependency boundaries stay BCL-first with no competitor dependency or compatibility surface | The core package's nuspec declares an empty `net10.0` dependency group and ships its analyzers as `analyzers/dotnet/cs` content; the ASP.NET Core package declares exactly one dependency (`FunnySharp` `0.1.0`) and one framework reference (`Microsoft.AspNetCore.App`); no competitor package appears anywhere | `release-evidence/package-inventory.json`; the release audit's two package layout and dependency checks |
| Package metadata, versioning, API-compatibility baseline, release notes, trimming, Native AOT, and supported-runtime claims verified through external package consumers | The compatibility suite consumed the packed packages only (never project references) in four scenarios - `CoreTrimmed`, `CoreNativeAot`, `AspNetCoreTrimmed`, `AspNetCoreNativeAot` - all `Succeeded: true` on `linux-x64`; the version preflight confirmed `0.1.0` absent from the feed before the run; the baseline, versioning rules, release notes, and runtime statement are tracked documents | `release-evidence/compatibility/compatibility-results.json`; `version-preflight.json`; `docs/versioning.md`; `docs/release-notes.md`; `eng/api-baseline/` |
| Benchmark and AI-usability claims link to their reproducible evidence and do not exceed it | Both generated-documentation verifications passed, so every published table is regenerated from the approved observation in the tracked manifest and its policy/benchmark-input/protocol fingerprints match the tree; the analyzer guide links the AI-usability evidence | `release-evidence/` step receipts 12 and 13; `docs/performance.md`; `docs/analyzers.md` |
| General discriminated unions stay outside the candidate | No union type appears in the recorded public surface; the contract rejects them and the curation tests enforce the absence | `release-evidence/public-api.txt`; `docs/product-contract.md`; `tests/FunnySharp.Tests/AdvancedPatternCurationTests.cs` |
| Evidence includes clean restore/build/xUnit v3/package results, compiled documentation, public API and dependency inventories, package-consumer compatibility, trim and Native AOT evidence where claimed, and a fail-closed release-readiness record | All fourteen protocol steps exited 0 with no retry from an isolated NuGet cache: clean, locked restore, Release build, the full xUnit v3 suite, both examples, pack, formatter, both protocol suites, benchmark preflight, both documentation verifications, and compatibility; the bundle carries the API, documentation, and package inventories; this record is the fail-closed verdict | `artifacts/release-candidate/e742d2b09933c335746f6401acbe8d7952055b7b/goal23-local-1/` (`release-outcome.json`, `execution-evidence.json`, `receipts/`, `logs/`) |
| Actual publication to an external feed requires separate explicit authorization and is not implied | Nothing was published: the version preflight and final check both read `0.1.0` as absent from `api.nuget.org` (HTTP 404), no push or release was performed, and the branch is local | `version-preflight.json`; `version-final.json` |

## What This Record Does Not Claim

- **One platform.** The attempt is a single local `linux-x64` run. The four release-gate contexts
  (`release / win-x64`, `release / linux-x64`, `release / osx-arm64`, `release / osx-x64-consumer`)
  have not been exercised from this host; no repository ruleset readback is part of this record.
- **No refreshed benchmark observation.** The run used the `benchmarkSkipped` mode: it verified the
  approved observation and its fingerprints rather than re-measuring. Hosted timing remains
  directional, and allocation budgets remain the blocking contract.
- **`Effect.FromValue`'s null policy is documented in its guide, not in its XML documentation.**
  `src/FunnySharp/Effect.cs` is a benchmark-input fingerprint file, so a comment-only edit there
  invalidates the approved performance observation and would require a re-measurement for no
  behavioral change. The policy is stated in `docs/effects.md` beside the factory list, which is how
  `docs/result.md` and `docs/validation.md` state their carriers' null policies.
- **No `IsAotCompatible` claim.** Native AOT evidence is the consumer publish described above; the
  packages do not declare the AOT-compatibility property.
- **Trimming and AOT evidence is RID-scoped.** It applies to the recorded SDK, runtime patch,
  operating system, RID, and package hashes only.
