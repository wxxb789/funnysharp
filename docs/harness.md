# The F# Build Harness

Every development gate in this repository — build, test, format, docs-snippet parity, action
pins, inventory, vertical slice, performance, reproducible builds, ruleset, compatibility,
release, and the coding-evaluation harness — runs as one command form:

```bash
dotnet fsi build.fsx -- -p <pipeline> [args]
```

A bare run (`dotnet fsi build.fsx`, no `-p`) prints the pipeline list instead of running a gate; every pipeline is opt-in via `-p`.

## Pipelines

| Pipeline | Purpose |
| --- | --- |
| `build` | Build the solution with `dotnet build`. |
| `test` | Run the solution's test suites with `dotnet test`. |
| `format` | Verify formatting with `dotnet format`. |
| `check-action-pins` | Check that every remote GitHub action is pinned to a full commit SHA. |
| `verify-docs-snippets` | Verify every documentation sample against its source region. |
| `verify-api-baseline` | Verify the shipping assemblies' public API surface against the committed baseline. |
| `verify-stable-api-contracts` | Verify the final 468-member semantic index and its hash-bound source, compiler, runtime, and XML references. |
| `verify-tooling` | Run the local pre-check over the release protocol's local steps. |
| `generate-inventory` | Regenerate the next-stage evidence inventories. |
| `vertical-slice` | Pack the solution and verify the vertical slice against the packages only. |
| `verify-performance` | Verify performance receipts against the manifest's allocation budgets. |
| `generate-performance-docs` | Generate or verify the performance documentation regions. |
| `compare-reproducible-builds` | Compare two build roots for byte-identical output. |
| `verify-ruleset` | Verify the repository's GitHub ruleset against the required status checks. |
| `compatibility` | Verify the packed packages with the compatibility consumer scenarios. |
| `release` | Run the canonical release protocol (`benchmarkSkipped` mode with `-SkipBenchmarks`). |
| `release-verify` | Audit a release attempt's evidence tree. |
| `eval-prep-feed` | Prepare the evaluation feed used by the coding-evaluation harness. |
| `eval-verify` | Verify one evaluation solution directory and write its `record.json`. |
| `eval-aggregate` | Aggregate the recorded evaluation runs into one markdown table. |
| `rawloc` | Report the raw line counts of the call-site targets. |
| `loc-extract` | Extract one method body from a call-site file by name. |

Each pipeline keeps the command surface of the script it replaced, so flags such as
`-ReceiptDirectory`, `-ManifestPath`, `-OutputDirectory`, or `--repository-root` still apply.
The `vertical-slice` pipeline also accepts `--package-feed <URL>` for its upstream NuGet
source, defaulting to `https://api.nuget.org/v3/index.json`. The isolated restore config keeps
the newly packed local feed first and records the selected upstream; package vulnerability
auditing and warnings-as-errors remain enabled.
Run `dotnet fsi build.fsx` with no `-p` to print this list; each pipeline's `description` carries
a one-line summary.

`verify-stable-api-contracts` reads the current
`docs/audits/goal-24-resolution/stable-api-semantic-proofs.json`, or the path supplied with
`--proof-index <path>`. Its final package/compiler/runtime receipts must already exist. It
checks exact identities, all eight dimensions, source/assertion spans and hashes, actual
case IDs and statuses, compiler diagnostics/emissions, and explicit XML targets. Source
invariants remain source evidence, not executed runtime cases; runtime NA is limited to
the delegate ABI constructor and enum-storage mechanisms. The gate never regenerates
evidence or infers maintainer acceptance from passing checks.

## Exit-Code Contract

Migrated gates preserve the scripts' three-way contract:

- `0` — pass.
- `1` — verification failure (a check ran and reported a problem).
- `2` — usage or environment failure (bad arguments, missing prerequisite).

Fun.Build reports any failed step as exit code `1`, which would collapse `2` into `1`. The
`gate` helper in `build.fsx` therefore flushes the verdict just printed and exits the process
with the exact code the gate returned, so `0`/`1`/`2` survive the pipeline wrapper.

## Sources

- `build.fsx` (repository root) — pipeline definitions, the `gate` helper, and the CLI flag
  plumbing; references `Fun.Build` **1.2.0** (`#r "nuget: Fun.Build, 1.2.0"`).
- `eng/harness/*.fs` — the gate implementations (`Output`, `Proc`, `Repo`, `ActionPins`,
  `DocsSnippets`, `Inventory`, `VerticalSlice`, `ToolingVerify`, `Performance`,
  `PerformanceDocs`, `ReproducibleBuilds`, `Ruleset`, `Compatibility`, the `Release*` family,
  `Loc`, `Evaluation`). `build.fsx` `#load`s them in dependency order.
- `tests/FunnySharp.Harness.Tests/` — xUnit v3 **4.0.0** tests for the harness, in
  `FunnySharp.slnx`.

`eng/release-protocol.json` still defines the release step list and modes; the `release` and
`verify-tooling` pipelines consume it rather than re-defining policy.

## Known Boundaries

- **`dotnet format` cannot check F# projects.** The `format` pipeline verifies only the C#
  solution; formatting of `build.fsx` and `eng/harness/*.fs` is not automated. A warning in the
  F# harness is still a build failure (`TreatWarningsAsErrors` applies to the harness projects).
- **Raw-LOC drift.** `docs/next-stage/review/verification.md` is append-only review output and
  records a 489-raw-line total for the call-sites document; the ported `rawloc` pipeline reports
  505 for the current tree. The review body is left untouched; treat the harness output as the
  live number.
