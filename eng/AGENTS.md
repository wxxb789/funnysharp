# eng/ — build engineering, release & performance protocol

**Earned:** subtree root + distinct domain (release/performance protocol; score >15).

## OVERVIEW

PowerShell + JSON protocol layer that drives the canonical release and the performance
budget verification. Nothing here ships; everything here gates.

## WHERE TO LOOK

| Need | Location |
| --- | --- |
| Run the canonical release | `Run-Release.ps1` (entry; `-AttemptId`, `-SkipBenchmarks`, `-Clean`, `-OutputDirectory`) |
| Release step definitions | `release-protocol.json` (steps `clean`…`compatibility`; modes `full` / `benchmarkSkipped`) |
| Release orchestration logic | `ReleaseProtocol.psm1` (module consumed by the tests) |
| Release verification / audit | `Verify-Release.ps1` (~89 KB; the heavyweight checks) |
| Verify performance budgets | `Verify-Performance.ps1` (receipts vs. manifest; never edits policy) |
| Regenerate perf doc tables | `Generate-PerformanceDocumentation.ps1` |
| Reproducible-build proof | `Compare-ReproducibleBuilds.ps1` |
| GitHub ruleset check | `Verify-GitHubRuleset.ps1` |
| Perf budget manifests | `performance/` (see below) |
| Frozen protocol tests | `tests/` (see below) |
| Python contributor tooling | `eng/tools/AGENTS.md` (child) |
| Coding evaluation harness | `eng/evaluation/AGENTS.md` (child) |

## performance/ (no own file — data, not code)

- `baseline.json` — main suite policy + committed observation: 194 rows (182 included),
  allocation budgets are the blocking gate, timing is directional only.
- `competitor-baseline.json` — competitor suite: 40 rows (Option/Result carrier) pairing
  raw/direct baselines with `funcky` / FSharp.Core paths per `comparisonGroup`.
- Tracked manifests, NOT emitted artifacts. `policy` (budgets, exclusions) is
  verifier-immutable and editorially owned; `observation` is machine-generated
  (fingerprints: policy / benchmarkInput / protocol + `environmentKey`).
- Exclusions are first-class rows (`id` prefixed `excluded|`, `exclusionReason`), never omissions.

## tests/ (no own file — two frozen suites)

- `ReleaseProtocol.Tests.ps1`, `PerformanceProtocol.Tests.ps1` — plain PowerShell scripts,
  home-grown `Assert-Passes`/`Assert-Fails` framework, no Pester, `Set-StrictMode -Version Latest`.
- Wired into `release-protocol.json` steps `release-protocol-tests` / `performance-protocol-tests`.
- They are the ONLY owner of `release.yml` action pins — `check_action_pins.py` deliberately
  never re-checks them (avoid a divergent second owner).

## next-stage-inventory/ (no own file — evidence tooling)

Reflection dumper (`api-inventory.csproj` + `Program.cs`) listing public API surface as
markdown/JSON; driven by `eng/tools/inventory.py` to regenerate `docs/next-stage/inventory/generated/`.
Not in `FunnySharp.slnx`, not packable, must never become a release dependency.

## CONVENTIONS

- Scripts run from the repository root via `pwsh -NoProfile -File eng/<script>.ps1`.
- Verify scripts fail closed with remediation text; they never mutate what they verify.

## ANTI-PATTERNS

- NEVER treat `tooling.yml` as a release gate — only the `release.yml` contexts gate a
  release; tooling is informational (KTD8). Never promote `tooling-gate` casually.
- NEVER run benchmarks in CI — release runs `Run-Release.ps1 -SkipBenchmarks` (the
  `benchmarkSkipped` protocol mode); benchmarks are a developer-machine activity.
- NEVER hand-edit generated/frozen evidence: `docs/next-stage/inventory/generated/` comes
  from `eng/tools/inventory.py`, perf observations come from the exporters — regenerate,
  don't patch. Policy rows (`excluded|`, budgets) are verifier-immutable editorial content.
- NEVER restore without the lock — release protocol uses `--locked-mode --no-cache`;
  skipping it voids reproducibility proofs.

## COMMANDS

```bash
pwsh -NoProfile -File eng/tests/ReleaseProtocol.Tests.ps1        # frozen release protocol
pwsh -NoProfile -File eng/tests/PerformanceProtocol.Tests.ps1    # frozen perf protocol
pwsh -NoProfile -File eng/Verify-Performance.ps1 -ReceiptDirectory <dir>
pwsh -NoProfile -File eng/Verify-Performance.ps1 -ManifestPath eng/performance/competitor-baseline.json -ReceiptDirectory <dir>
pwsh -NoProfile -File eng/Generate-PerformanceDocumentation.ps1
pwsh -NoProfile -File eng/Run-Release.ps1 -AttemptId <id> -OutputDirectory artifacts/release-run -Clean
```
