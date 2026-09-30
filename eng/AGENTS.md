# eng/ — build engineering, release & performance protocol

**Earned:** subtree root + distinct domain (release/performance protocol; score >15).

## OVERVIEW

The F# build harness. A repo-root `build.fsx` (Fun.Build) defines one pipeline per gate and
`eng/harness/*.fs` implement them: release, release audit, performance budget verification,
performance-doc generation, reproducible-build comparison, ruleset check, compatibility,
inventory, vertical slice, docs snippets, action pins, the local pre-check, and the
coding-evaluation harness. Nothing here ships; everything here gates.

## WHERE TO LOOK

| Need | Location |
| --- | --- |
| Run any gate | root `build.fsx`: `dotnet fsi build.fsx -- -p <pipeline> [args]` |
| Harness contract (invocation, pipelines, exit codes) | `../docs/harness.md` |
| Gate implementations | `harness/*.fs` (`Output`, `Proc`, `Repo`, … `Compatibility`, `Release*`, `ApiBaseline`, `Loc`, `Evaluation`) |
| Committed public-API baseline | `../eng/api-baseline/` compared by `harness/ApiBaseline.fs` (`-p verify-api-baseline`) and by the release audit |
| Release step definitions | `release-protocol.json` (steps `clean`…`compatibility`; modes `full` / `benchmarkSkipped`) |
| Release orchestration logic | `harness/ReleaseProtocol.fs`, `harness/ReleaseRun.fs` |
| Release verification / audit | `harness/ReleaseVerify*.fs` (the heavyweight checks) |
| Verify performance budgets | `harness/Performance.fs` (receipts vs. manifest; never edits policy) |
| Regenerate perf doc tables | `harness/PerformanceDocs.fs` |
| Reproducible-build proof | `harness/ReproducibleBuilds.fs` |
| GitHub ruleset check | `harness/Ruleset.fs` |
| Perf budget manifests | `performance/` (see below) |
| Harness tests | `../tests/FunnySharp.Harness.Tests/` (xUnit v3) |
| Coding evaluation harness | `harness/Evaluation.fs`, `evaluation/AGENTS.md` (child) |

## performance/ (no own file — data, not code)

- `baseline.json` — main suite policy + committed observation: 201 rows (190 included),
  allocation budgets are the blocking gate, timing is directional only.
- `competitor-baseline.json` — competitor suite: 40 rows (Option/Result carrier) pairing
  raw/direct baselines with `funcky` / FSharp.Core paths per `comparisonGroup`.
- Tracked manifests, NOT emitted artifacts. `policy` (budgets, exclusions) is
  verifier-immutable and editorially owned; `observation` is machine-generated
  (fingerprints: policy / benchmarkInput / protocol + `environmentKey`).
- Exclusions are first-class rows (`id` prefixed `excluded|`, `exclusionReason`), never omissions.

## tests/ (no own file — harness test project)

The frozen protocol suites now live in `../tests/FunnySharp.Harness.Tests/` as F# xUnit v3
tests (`ReleaseProtocolTests.fs`, `PerformanceProtocolTests.fs`, and one file per harness
module), run by `dotnet test FunnySharp.slnx` and by the `test` pipeline. They own the
`release.yml` action pins; the `check-action-pins` pipeline deliberately never re-checks them
(avoid a divergent second owner).

## next-stage-inventory/ (no own file — evidence tooling)

Reflection dumper (`api-inventory.csproj` + `Program.cs`) listing public API surface as
markdown/JSON; driven by the `generate-inventory` pipeline (`harness/Inventory.fs`) to
regenerate `docs/next-stage/inventory/generated/`. Not in `FunnySharp.slnx`, not packable, must
never become a release dependency.

## CONVENTIONS

- Gates run from the repository root as `dotnet fsi build.fsx -- -p <pipeline> [args]`.
- The `gate` helper in `build.fsx` preserves the 0/1/2 exit contract (0 pass, 1 verification
  failure, 2 usage/environment) that Fun.Build's single failure code would otherwise flatten.
- Verify gates fail closed with remediation text; they never mutate what they verify.
- `dotnet format` cannot check F# projects, so the `format` pipeline covers C# only; the harness
  F# sources are not format-checked.

## ANTI-PATTERNS

- NEVER treat `tooling.yml` as a release gate — only the `release.yml` contexts gate a
  release; tooling is informational (KTD8). Never promote `tooling-gate` casually.
- NEVER run benchmarks in CI — the release pipeline runs with `-SkipBenchmarks` (the
  `benchmarkSkipped` protocol mode); benchmarks are a developer-machine activity.
- NEVER hand-edit generated/frozen evidence: `docs/next-stage/inventory/generated/` comes
  from the `generate-inventory` pipeline, perf observations come from the exporters — regenerate,
  don't patch. Policy rows (`excluded|`, budgets) are verifier-immutable editorial content.
- NEVER restore without the lock — the release protocol uses `--locked-mode --no-cache`;
  skipping it voids reproducibility proofs.

## COMMANDS

```bash
dotnet fsi build.fsx -- -p verify-tooling                  # local pre-check (release protocol's local steps)
dotnet fsi build.fsx -- -p verify-docs-snippets            # 11 primary guides, byte-exact
dotnet fsi build.fsx -- -p check-action-pins               # full-SHA workflow pins
dotnet fsi build.fsx -- -p verify-performance -ReceiptDirectory <dir>
dotnet fsi build.fsx -- -p verify-performance -ManifestPath eng/performance/competitor-baseline.json -ReceiptDirectory <dir>
dotnet fsi build.fsx -- -p generate-performance-docs
dotnet fsi build.fsx -- -p release -AttemptId <id> -OutputDirectory artifacts/release-run
```
