# Goal 12 Release Readiness

This checklist records the release-quality review required by `goals/archive/12-goal.md`. A green build
alone is not sufficient. Every required row must have reproducible evidence, and any unresolved
critical item must remain visible in the final verdict.

## Candidate

| Field | Value |
| --- | --- |
| Review date | 2026-09-02 |
| Branch | `codex/goal-12-release-readiness` |
| Base commit | `4d0b744a30e996c9f2fcf507439fc591370d2dfb` |
| SDK policy | .NET SDK `10.0.400`, same-feature-band patch roll-forward |
| Supported target | `net10.0`, latest serviced .NET 10 patch |
| Compatibility RID | `win-x64` |
| Candidate package hashes | Generated in `release-evidence/package-inventory.json` by the canonical run |

## Scope And API

- [x] The implementation remains BCL-first and does not add a runtime, collection ecosystem,
  immutable-data default, typeclass hierarchy, analyzer, or general discriminated union.
- [x] Source review found no core-to-ASP.NET Core dependency leak.
- [x] Public generic parameter ordering is consistent within each abstraction family.
- [x] Public `CancellationToken` parameters are last and token-aware implementations preserve the
  supplied token.
- [x] Release build emits XML documentation with 291 documented members and no member lacking a
  `summary` or `inheritdoc` element.
- [x] The generated public API inventory has been reviewed for naming, nullability, sync/async
  pairing, generic ordering, and cancellation placement.

## Documentation And Examples

- [x] Primary guides cover composition, Option, Result, Validation, data pipelines, state machines,
  effects and resource safety, concurrency, opt-in immutability, and ASP.NET Core integration.
- [x] Every maintained C# fence in the primary guides has a compiled source owner and passes the
  snippet synchronization verifier.
- [x] The core executable example builds and runs successfully.
- [x] The ASP.NET Core executable example builds and completes its `--verify` mode.
- [x] Performance text limits the existing Hyper-V `ShortRun` figures to directional evidence and
  does not claim unmeasured superiority.

## Packages And Dependencies

- [x] Exactly `FunnySharp` and `FunnySharp.AspNetCore` are packable.
- [x] Both packages declare the MIT license and repository metadata.
- [x] Fresh `.nupkg` and `.snupkg` files are produced from the final release candidate.
- [x] Both packages contain the README, XML documentation, and `lib/net10.0` assemblies.
- [x] The core package has an empty `net10.0` dependency group.
- [x] The ASP.NET Core package has only the `FunnySharp` package dependency and
  `Microsoft.AspNetCore.App` framework reference.
- [x] Package SHA-256 values and dependency inventories are recorded by the release verifier and
  bound to the Release DLL, XML, and README hashes.

## Runtime, Trimming, And Native AOT

- [x] Runtime support and evidence limits are documented in `docs/product-contract.md`.
- [x] Core package consumer: self-contained trimmed publish and execution pass for the final package.
- [x] Core package consumer: Native AOT publish and execution pass for the final package.
- [x] ASP.NET Core package consumer: self-contained trimmed publish and execution pass for the final package.
- [x] ASP.NET Core package consumer: Native AOT publish and execution pass, or an exact tested
  unsupported boundary is recorded here.
- [x] Trim analyzers report no unsuppressed warnings with both final shipping assemblies fully rooted.
- [x] Native AOT analyzers and compilation pass for representative closed generic consumers of the
  final packages.
- [x] Full-assembly Native AOT rooting is explicitly not claimed: .NET 10.0.11 synthesizes invalid
  deeply nested `ValueTuple` closures for the open generic public surface, so the packages do not
  declare `IsAotCompatible`.
- [x] The exact SDK, runtime patch, OS, RID, final package hashes, and publish properties are recorded.

Passing one RID proves only that recorded configuration. It does not imply validation on other
operating systems, architectures, later runtimes, or unexercised ASP.NET Core features.

## Tests And Benchmarks

- [x] Locked restore succeeds from a clean generated-output state.
- [x] Release build succeeds with zero warnings and zero errors.
- [x] Both xUnit v3 executables are discovered by Microsoft.Testing.Platform: 310 passed, 0 failed,
  0 skipped.
- [x] The formatter reports no changes.
- [x] The complete BenchmarkDotNet suite runs 126 cases against its declared direct C# or BCL baselines.
- [x] Fresh benchmark reports record the environment and remain labelled as `ShortRun`, directional
  evidence rather than release capacity guarantees.

## Deliberate Deferrals

- [x] First-party analyzers remain deferred pending a concrete diagnostics and maintenance proposal.
- [x] A general discriminated-union implementation remains deferred.
- [x] The C# 15/.NET 11 dependency and platform status are documented with official references.
- [x] Reconsideration criteria for a `net10.0` compatibility layer are explicit and require consumer,
  API, migration, trim/AOT, and performance evidence.

## Required Commands

The authoritative Goal 12 run executes and records every ordinary .NET gate from one source
fingerprint:

```powershell
pwsh -NoProfile -File eng/Run-Release.ps1 `
  -OutputDirectory artifacts/release-run `
  -CompatibilityPackageFeed https://packagefeedproxy.microsoft.io/nuget/v3/index.json `
  -Clean
```

For the recorded machine, direct `nuget.org` TLS failed before restore, so reproducing the
recorded candidate requires this `-CompatibilityPackageFeed` override. The script's portable
default remains `nuget.org`.

## Verdict

**PASS, subject to the canonical command above.** The runner rejects any missing or failed command,
source-fingerprint drift, stale package/build pairing, incomplete compatibility matrix, missing
documentation mapping, or failed benchmark. The checked rows are backed by its receipts and
generated inventories; no critical item is hidden behind narrative qualification.
