# Release Readiness

This is the evergreen, fail-closed release checklist for FunnySharp. It contains no candidate-specific
PASS claim. A release attempt is accepted only from its immutable generated evidence and independent
Goal 13 review.

## Candidate Identity

- [ ] The tracked tree is clean before any output cleanup, restore, build, or pack step.
- [ ] The attempt uses a new `artifacts/release-candidate/<commit>/<attempt-id>` directory.
- [ ] Candidate commit, source fingerprint, workflow revision, event, run ID, attempt, job, RID,
  runner image, SDK, and runtime are recorded.
- [ ] `FunnySharp` and `FunnySharp.AspNetCore` version `0.1.0` are unambiguously absent from every
  intended distribution feed before the first pack and again before the final verdict.

## Fresh Build State

- [ ] Only validated direct `bin` and `obj` children of tracked projects are removed.
- [ ] Reparse points, repository escapes, ancestors, siblings, and drive roots are rejected.
- [ ] Restore uses locked mode, no cache, and an initially empty attempt-local
  `NUGET_PACKAGES` directory.
- [ ] Release build succeeds with zero warnings and errors; all discovered xUnit v3 tests pass with
  zero failures and skips.
- [ ] Core and ASP.NET Core examples, formatter verification, documentation snippets, API inventory,
  XML documentation, package layout, and dependency checks pass.

## Performance Evidence

Benchmarks are a developer-machine activity, not a CI step: every release-candidate run (including
the Windows job) uses the benchmark-skipped protocol and verifies the approved observation it ships
instead of re-measuring on hosted runners. Run `Run-Release.ps1` without `-SkipBenchmarks` locally
to refresh evidence.

- [ ] Benchmark semantic preflight passes before measurement (`benchmark-preflight` still runs in
  every mode).
- [ ] Benchmark receipts measured on a developer machine produce the observation, with integer
  allocation receipts for every included policy row.
- [ ] Missing, nonnumeric, semantically mismatched, or over-budget allocation rows fail verification.
- [ ] Timing is directional only; `below-resolution` and `unavailable` produce `N/A` and do not fail
  the release.
- [ ] Every exact guide table is generated from the approved observation in
  `eng/performance/baseline.json`; verify mode detects manual drift.
- [ ] The approved observation's policy, benchmark-input, and protocol fingerprints match the
  current tree (checked by `performance-docs-verify` on every run).
- [ ] Every intentionally unmeasured surface is an explicit exclusion with rationale and no numeric
  claim.
- [ ] Competitor comparisons against pinned packages run only in the isolated
  `FunnySharp.CompetitorBenchmarks` project outside `FunnySharp.slnx`; their receipts are
  verified against `eng/performance/competitor-baseline.json` with the same verifier on the
  developer machine, and they remain performance evidence only - never API-compatibility or
  release-acceptance evidence.

## Required Platform Gates

The exact required GitHub check contexts are:

- `release / win-x64`: full source, package, correctness, BenchmarkDotNet, allocation, trimming, and
  Native AOT proof; this job produces the canonical packages.
- `release / linux-x64`: benchmark-skipped source/package/trim/Native AOT proof plus consumption of
  the Windows-produced canonical package hashes.
- `release / osx-arm64`: benchmark-skipped source/package/trim/Native AOT proof plus consumption of
  the same canonical package hashes.
- `release / osx-x64-consumer`: bounded Intel macOS package smoke, RID assertion, core and ASP.NET
  Core execution, Goal 04/09 regressions, and package/loaded-DLL hash checks.

Any unavailable matching host or failed context blocks merge and release. Matrix `fail-fast` may be
disabled to retain evidence, but no required job may continue on error.

## Repository Rules

- [ ] An authorized ruleset readback names all four exact contexts above.
- [ ] Normal merge and release actors have no bypass.
- [ ] A deliberate failing test pull request proves each context blocks merge.
- [ ] Failed ruleset activation restores the prior snapshot.
- [ ] Exercising an emergency bypass invalidates the candidate and requires a new audit.

Workflow code can be reviewed locally without control-plane authorization. Product acceptance cannot
PASS until the ruleset readback and blocking test are present in the attempt evidence.

## Package Consumers

- [ ] Exactly two `.nupkg` and two matching `.snupkg` files are produced.
- [ ] Every consumer restores from the canonical local package set and an isolated cache.
- [ ] Package and loaded assembly hashes match the canonical Windows producer.
- [ ] Result cancellation preserves exception identity, established stack, token, and canceled state.
- [ ] `FirstSuccessAsync` publishes caller cancellation when it occurs during timeout-selected cleanup.
- [ ] Trimming and Native AOT scenarios execute successfully on their matching hosts.

## Evidence Freeze And Verdict

1. Assemble immutable evidence bundle `P` only after the final feed check, all required jobs, package
   hashes, and ruleset proof are final.
2. A named read-only reviewer evaluates `P` and Goals 01-13, replays the Goal 04/09 package probes,
   and emits immutable attestation `A` referencing only `P` and goal-contract hashes.
3. The GitHub job summary or run provenance publishes external index `I` containing the hash of `A`
   and artifact locations. `P` and `A` never back-reference later objects.

`Audit status: COMPLETE` means every criterion was evaluated and every material gap was recorded.
`Product acceptance: PASS` additionally requires every material criterion to pass. Missing
authorization, expired evidence needed for byte inspection, any required failure, or any material
`UNVERIFIED` result keeps `Product acceptance: FAIL`.

## Commands

Full local Windows attempt:

```powershell
pwsh -NoProfile -File eng/Run-Release.ps1 `
  -AttemptId local-full-1 `
  -CompatibilityRuntimeIdentifier win-x64 `
  -CompatibilityPackageFeed https://packagefeedproxy.microsoft.io/nuget/v3/index.json `
  -DistributionFeed https://packagefeedproxy.microsoft.io/nuget/v3/index.json
```

Benchmark-skipped platform attempt:

```powershell
pwsh -NoProfile -File eng/Run-Release.ps1 `
  -AttemptId local-platform-1 `
  -CompatibilityRuntimeIdentifier <matching-rid> `
  -CompatibilityPackageFeed https://packagefeedproxy.microsoft.io/nuget/v3/index.json `
  -DistributionFeed https://packagefeedproxy.microsoft.io/nuget/v3/index.json `
  -SkipBenchmarks
```

Refresh performance evidence on a developer machine when benchmark inputs change:

```powershell
# 1. Measure (allocation budgets are the contract; timing is directional)
dotnet run --project benchmarks/FunnySharp.Benchmarks --configuration Release -- --preflight
dotnet run --project benchmarks/FunnySharp.Benchmarks --configuration Release -- `
  --filter "*" --artifacts <artifacts-dir>
# 2. Verify the receipts and write the reviewable observation proposal
pwsh -NoProfile -File eng/Verify-Performance.ps1 -RepositoryRoot . `
  -ReceiptDirectory <artifacts-dir>/results `
  -ObservationProposalPath <artifacts-dir>/performance-observation-proposal.json
# 3. Approve the proposal into eng/performance/baseline.json, then regenerate and verify the guides
pwsh -NoProfile -File eng/Generate-PerformanceDocumentation.ps1 -RepositoryRoot .
pwsh -NoProfile -File eng/Generate-PerformanceDocumentation.ps1 -RepositoryRoot . -Verify
```

Cross-path byte equality is diagnosed with `eng/Compare-ReproducibleBuilds.ps1`. It is not a current
release blocker; all evidence remains bound to the exact package hashes actually consumed.

The PowerShell commands above remain the only release gate. `uv run --no-project
eng/tools/verify_local.py` is an optional, PowerShell-free local pre-check: it runs a subset of
the same steps, labels itself a pre-check, and produces no release evidence (see
[tooling](tooling.md)).
