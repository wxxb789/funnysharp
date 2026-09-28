# .github/workflows/ — CI pipelines

**Earned:** score ~8 — distinct domain (only CI surface; dense pinning/gate conventions).

## OVERVIEW

Two workflows. `release.yml` is the release gate; `tooling.yml` is informational matrix
coverage for the Python/uv tooling layer and frozen PowerShell protocol tests.

## WHERE TO LOOK

| Workflow | Jobs | What runs |
| --- | --- | --- |
| `release.yml` | `win-x64` → `linux-x64`, `osx-arm64`, `osx-x64-consumer` (all `needs: win-x64`) | Canonical: `./eng/Run-Release.ps1 -AttemptId <id> -CompatibilityRuntimeIdentifier <rid> -CompatibilityPackageFeed <feed> -SkipBenchmarks`. Consumers run `./tests/FunnySharp.Compatibility/Run-Compatibility.ps1` against the downloaded canonical packages (osx-x64-consumer adds `-Scenario CoreSmoke,AspNetCoreSmoke`) |
| `tooling.yml` | `tooling` (matrix: ubuntu-latest, windows-2025, macos-15) + `tooling-gate` | `uv python install`; `uv run --no-project python -m unittest discover -s eng/tools/tests`; `uv run --no-project eng/tools/verify_local.py`; `pwsh -NoProfile -File eng/tests/ReleaseProtocol.Tests.ps1`; `pwsh -NoProfile -File eng/tests/PerformanceProtocol.Tests.ps1`; `uv run --no-project eng/tools/check_action_pins.py --verbose` |

## CONVENTIONS

- Both: triggers `pull_request` + `push: main` + `workflow_dispatch`; `permissions:
  contents: read`; `concurrency: <name>-${{ github.ref }}` with `cancel-in-progress: true`;
  `env.DOTNET_NOLOGO: true`.
- Every remote `uses:` is pinned to a full 40-hex commit SHA with a `# vN` comment
  (enforced by `eng/tools/check_action_pins.py`).
- `tooling.yml` has NO `paths` filter on purpose — a future required check must always
  report a result instead of staying pending (KTD8).
- uv version is never repeated as a `version:` input — `setup-uv` reads `uv.toml`'s
  `required-version` (single source of truth, KTD3).
- Every tooling step pipes through `tee` into `$RUNNER_TEMP/tooling-logs/<step>.log`;
  logs uploaded `if: always()`, `retention-days: 7`.
- `tooling-gate` is one stable aggregate check name over the matrix, for future promotion
  to required — today it is informational; the release contexts remain the only release gate.

## ANTI-PATTERNS

- NEVER unpin an action to a tag/branch — `check_action_pins.py` fails on any non-SHA remote `uses:`.
- NEVER re-check `release.yml`'s action pins outside the frozen `ReleaseProtocol.Tests.ps1`
  (single owner rule, R14/KTD8).
- NEVER add a `paths` filter to `tooling.yml`.
- NEVER run benchmarks in CI — release runs with `-SkipBenchmarks` by design;
  benchmarks are a developer-machine activity.
- NEVER use flow-style (`{ }`) step maps — the pin checker treats them as findings.
