# .github/workflows/ — CI pipelines

**Earned:** score ~8 — distinct domain (only CI surface; dense pinning/gate conventions).

## OVERVIEW

Two workflows. `release.yml` is the release gate; `tooling.yml` is informational matrix
coverage for the F# harness (`build.fsx`, Fun.Build 1.2.0) tooling layer.

All pipeline steps invoke the harness directly: `dotnet fsi build.fsx -- -p <name> [args]`.
The Python/uv tooling layer and the PowerShell protocol scripts are gone.

## WHERE TO LOOK

| Workflow | Jobs | What runs |
| --- | --- | --- |
| `release.yml` | `win-x64` → `linux-x64`, `osx-arm64`, `osx-x64-consumer` (all `needs: win-x64`) | Canonical: `dotnet fsi build.fsx -- -p release -AttemptId <id> -CompatibilityRuntimeIdentifier <rid> -CompatibilityPackageFeed <feed> -SkipBenchmarks`. Consumers run `-p compatibility -PackageDirectory ... -OutputDirectory ... -RuntimeIdentifier ... -PackageFeed ...` against the downloaded canonical packages (osx-x64-consumer adds `-Scenario CoreSmoke,AspNetCoreSmoke`) |
| `tooling.yml` | `tooling` (matrix: ubuntu-latest, windows-2025, macos-15) + `tooling-gate` | `dotnet test FunnySharp.slnx` (carries the ported protocol suites); `dotnet fsi build.fsx -- -p verify-tooling`; `-- -p verify-docs-snippets`; `-- -p check-action-pins` |

## CONVENTIONS

- Both: triggers `pull_request` + `push: main` + `workflow_dispatch`; `permissions:
  contents: read`; `concurrency: <name>-${{ github.ref }}` with `cancel-in-progress: true`;
  `env.DOTNET_NOLOGO: true`.
- Every remote `uses:` is pinned to a full 40-hex commit SHA with a `# vN` comment
  (enforced by `dotnet fsi build.fsx -- -p check-action-pins`, implemented in
  `eng/harness/ActionPins.fs`).
- Read-back: `build.fsx` accepts the legacy PowerShell parameter spellings verbatim
  (`-AttemptId`, `-CompatibilityRuntimeIdentifier`, `-PackageDirectory`, ...) plus the
  valueless switches (`-SkipBenchmarks`, `-Verify`, `--json`, ...). Release steps set
  `shell: bash` so `"$COMPATIBILITY_FEED"` expands (a bare pwsh default would read it as
  an undefined PowerShell variable).
- `tooling.yml` has NO `paths` filter on purpose — a future required check must always
  report a result instead of staying pending (KTD8).
- `tooling.yml` no longer sets up uv; `uv.toml` and `.python-version` are deleted with that
  step. The pin checker's setup-uv rule stays in place even though the step is gone.
- Every tooling step pipes through `tee` into `$RUNNER_TEMP/tooling-logs/<step>.log`;
  logs uploaded `if: always()`, `retention-days: 7`.
- `tooling-gate` is one stable aggregate check name over the matrix, for future promotion
  to required — today it is informational; the release contexts remain the only release gate.

## ANTI-PATTERNS

- NEVER unpin an action to a tag/branch — `check-action-pins` fails on any non-SHA remote `uses:`.
- NEVER re-check `release.yml`'s `actions/*` pins outside the harness release-protocol test
  suite (single owner rule, R14/KTD8).
- NEVER add a `paths` filter to `tooling.yml`.
- NEVER invoke `pwsh`, `uv`, or `python` from these workflows — the harness runs with `dotnet`.
- NEVER run benchmarks in CI — release runs with `-SkipBenchmarks` by design;
  benchmarks are a developer-machine activity.
- NEVER use flow-style (`{ }`) step maps — the pin checker treats them as findings.
