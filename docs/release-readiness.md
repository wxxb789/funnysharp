# Release Readiness

A release is accepted from actual command results for the Git candidate, not from
checksums of evidence files. This checklist makes no candidate-specific PASS claim.

## Candidate and Build

- [ ] Git reports a clean tracked tree before and after the attempt, at the same commit.
- [ ] Output uses a new `artifacts/release-candidate/<commit>/<attempt-id>` directory.
- [ ] Core and ASP.NET Core package versions agree and are available for publication.
- [ ] Restore uses locked packages and isolated NuGet caches; compatibility caches use short paths for Native AOT.
- [ ] Release build succeeds with warnings treated as errors.
- [ ] Product and harness tests pass; no tests are silently skipped.
- [ ] Both examples, formatting, documentation snippets, and public API baseline pass.
- [ ] Package layout/dependencies and package-only consumers pass, including
  matching-host trimming and Native AOT scenarios.

## Performance

Benchmarks run on developer machines, not hosted CI. CI uses `-SkipBenchmarks`.
A full local attempt includes benchmark preflight, measurement and allocation checks.

- [ ] Every included policy row has a valid measured allocation within its budget.
- [ ] Missing, nonnumeric, incomplete, or over-budget measurements fail verification.
- [ ] Exclusions have an explicit reason and make no numeric performance claim.
- [ ] Performance tables agree with the approved observation.

Timing remains informational until fixed-hardware measurement exists. Compiler,
SDK and NuGet validation remain enabled; removing custom checksum checks does not
remove package restore integrity or security checks.

## PR Merge Gate

Ordinary PR merges require the automatic `tooling-gate` check, bound to the GitHub
Actions integration, with strict up-to-date checks and zero bypass actors.
`verify-ruleset` verifies this merge policy. Manual release contexts are not
required for ordinary merges, and a passing PR check is not release acceptance.

The tooling matrix builds the harness and checks documentation snippets and action
pins. It does not run product tests, package consumers, trimming or Native AOT.

## Required Release Platform Gates

The manually dispatched release workflow reports these contexts:

- `release / win-x64`: build, tests, examples and canonical packages.
- `release / linux-x64`: source/platform checks and canonical package consumption.
- `release / osx-arm64`: source/platform checks and canonical package consumption.
- `release / osx-x64-consumer`: bounded canonical-package smoke on Intel macOS.

These contexts run only on manual dispatch. All four must pass for release
acceptance; a failed or missing manual release check blocks release, not an
ordinary PR merge. Cross-platform consumers use the downloaded canonical packages
directly; no custom package or loaded-DLL checksum comparison is needed.

## Results

Keep command exit codes, diagnostics and logs associated with the Git commit and
attempt. Review failures and material gaps directly. There is no separate
hash-bound producer/attestation/index chain or mandatory evidence replay.
Historical goal evidence remains historical; it cannot substitute for current
builds and tests.

## Commands

```bash
# Hosted CI / local release without benchmarks
dotnet fsi build.fsx -- -p release -AttemptId local-1 -SkipBenchmarks

# Full developer-machine release including benchmarks
dotnet fsi build.fsx -- -p release -AttemptId local-full-1

# Consume an existing package set
dotnet fsi build.fsx -- -p compatibility -PackageDirectory <packages> -OutputDirectory artifacts/compatibility-local -RuntimeIdentifier <rid>
```
