# Harness Simplification and Ablation

## Design Boundary

Source revision and tracked changes belong to Git. Build, test, documentation,
package consumer, API compatibility, and allocation correctness belong to their
own checks. Evidence files do not need another verification subsystem.

Removed from the active harness:

- SHA calculation and comparison for source files, binaries, packages, logs,
  receipts, snapshots and policy fingerprints.
- Independent release audit and provenance transport.
- XML build identity bindings and stable-API evidence index replay.
- Frozen replay and standalone offline packet verification.
- Reproducible-byte comparison.

The public C# API, semantic evaluation oracles, package-only scenarios, allocation
budgets, Git action references, locked restore and trust-boundary path checks
remain. Historical documents and frozen evidence retain their original bytes;
Git also retains the removed tools. They are not active verification requirements.

Net tracked executable/test source reduction: **14,531 lines**
(982 added, 15,513 removed), measured with `git diff --numstat`.
This is code size, not a coverage percentage or a latency benchmark.

## Ablation Method

Do not run the removed checksum verifiers as a baseline. Their forbidden work is
removed by design, not measured again. Compare tracked code size through Git.

Exercise the surviving checks with controlled failures:

- API drift must fail while unrelated assembly bytes do not change its verdict.
- Failed or missing evaluation test results must stay failed; round history and
  task/style isolation remain intact.
- Missing, duplicate, invalid or over-budget allocation rows must fail.
- Failing release commands must stop the attempt and retain diagnostics.
- Package consumers must actually restore, build and execute.

Deleting checksum-only tests lowers raw test count. Meaningful coverage is the
set of behaviors rejected by these controls, not the number of receipt fields or
source literals asserted. No numeric coverage increase is claimed without a
coverage measurement.

## Verification Results

The solution Release build, core tests (1,762), ASP.NET Core tests (28), analyzer
tests (168), public API baseline (49 types / 499 members), documentation snippets
(56 / 11 guides), and benchmark semantic preflight pass. All product test runs
report zero skips. The 166 focused harness behavior controls pass with zero failures
and zero skips after the integration fixes. Benchmark and competitor projects compile
with zero warnings and errors.

Ablation controls found two real regressions: cross-class benchmark launch filename
collisions and Native AOT cache paths beyond the Windows limit. Plain class-qualified
filenames and short unique cache directories fix them without fingerprints. Release
also reuses the local test-output check to reject successful-exit skipped test runs.

Full reflection-derived XML documentation coverage was removed with the audit.
Compiler documentation generation and warning checks remain, but they do not prove
complete XML coverage. No coverage percentage or end-to-end release PASS is claimed.
Full release requires a clean committed candidate and matching platform runners.

## Fast Iteration Follow-up

The launcher now dispatches through one BCL-only command registry; Fun.Build,
Fun.Result and Spectre.Console are removed from development dependencies.
Native build/test/format support project targets and pass through native options.
Release step expansion has one owner; orphaned audit/cleanup helpers are removed.
The comprehensive pre-check is explicit, not the default development loop.

Heavy tests and benchmarks are manual-only. Push/PR tooling CI builds the harness
and checks snippets/action pins; release/platform tests need manual dispatch.
Locked restore and the harness/test-project build pass with zero warnings/errors.
The 88 small CLI, fake-runner and protocol behavior checks pass with zero skips.
No heavy test, full release, evaluation or benchmark was run in this follow-up.
Remote branch rules requiring former automatic release contexts are not changed
by the local workflow edit and may require an authorized control-plane update.

## FSI Orchestration

`build.fsx` now owns command registration, help and orchestration and loads the
F# source modules directly. `FunnySharp.Harness.fsproj` is a library for tests;
there is no Program.fs executable or subprocess bootstrap. Pure argument transforms
live in `Cli.fs`. CI and release child commands use the same FSI entrypoint.

Source-loaded FSI has startup/typechecking cost (one observed help run: about 14s);
this change is architectural, not a claimed startup speedup. Native project builds
remain incremental. Heavy tests and benchmarks remain manually selected only.
