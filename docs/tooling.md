# Contributor Tooling

Every development gate in FunnySharp runs through the F# build harness described in
[harness.md](harness.md): one command form, `dotnet fsi build.fsx -- -p <pipeline> [args]`, over
the gate implementations in `eng/harness/*.fs`. This page covers the contributor-facing commands,
the authority split between the release gate and the local pre-check, and the policies the harness
enforces. The uv/Python tooling layer this page once described was replaced by the harness
(Goal 07, workstream B; the retired plan is kept, marked superseded, at
[plans/2026-09-18-1508-feat-cross-platform-uv-tooling-plan.md](plans/2026-09-18-1508-feat-cross-platform-uv-tooling-plan.md)).

## Authority Split

- The release gate is the harness `release` pipeline
  (`dotnet fsi build.fsx -- -p release`), together with `eng/release-protocol.json` and
  `.github/workflows/release.yml`. It keeps its four exact required contexts, and `release-verify`
  audits a release attempt's evidence tree.
- `verify-tooling` is a contributor pre-check. It reproduces the release verdict semantics for the
  local steps it covers, but it is not release evidence, and the release gate never consumes its
  output.
- Docs-snippet verification is the `verify-docs-snippets` pipeline. There is a single
  implementation (`eng/harness/DocsSnippets.fs`) rather than two verifiers to keep in parity.
- The `tooling.yml` workflow is informational; `tooling-gate` is not a merge gate. Its promotion
  criteria are recorded below.

## One-Time Setup

The only prerequisite is the .NET SDK pinned by `global.json` (`10.0.400`). `build.fsx` references
`Fun.Build` 1.2.0 through `#r "nuget: Fun.Build, 1.2.0"`, which the first run restores into the
ordinary NuGet cache. Run every command from the repository root: that is where `build.fsx` and
the repository layout are resolved from.

```bash
dotnet --version   # expect 10.0.400
```

## Local Commands

### Local Pre-Check

```bash
dotnet fsi build.fsx -- -p verify-tooling
```

Runs, in protocol order: locked-mode restore, Release build, the solution tests, both example
programs, the formatter check, and the documentation-snippet verifier. Output begins with
`FunnySharp local pre-check (not release evidence).` and states that it is not release evidence.

Flags:

- `--offline`: never attempt network-dependent setup in the tool steps; dotnet restore and build
  may still contact configured feeds on first use.
- `--json`: print a machine-readable summary on stdout.
- `--skip-docs`, `--skip-format`: skip those two steps.
- `--repository-root PATH`: override the repository root.

Exit codes: `0` pass, `1` check or marker-contract failure, `2` environment or usage failure.
Missing prerequisites fail with remediation text, never a traceback and never a silent skip. The
summary lists the release-protocol steps outside local scope as not run, including pack,
benchmarks, performance verification, and compatibility.

### Documentation Snippets

```bash
dotnet fsi build.fsx -- -p verify-docs-snippets
```

Verifies the eleven primary guides against the snippet regions under
`examples/FunnySharp.DocumentationSamples`, prints
`Verified 56 C# documentation snippets across 11 primary guides.`, and exits `0` or `1`. It writes
nothing. `--repository-root` and `--samples-root` exist for fixture runs.

### Inventory

```bash
dotnet fsi build.fsx -- -p generate-inventory --check-inputs
dotnet fsi build.fsx -- -p generate-inventory --baseline-root <root> --ref-pack-dir <dir> --output-dir <dir>
```

`--check-inputs` reports every required input and exits non-zero when any is missing, without
building or writing. Regeneration builds the C# dumper, runs the eleven targets plus the
language-ext type list, and writes only to the caller-chosen directory (default: a temp directory
outside the repository; the repository root itself is rejected); every dumper output and captured
stream is normalized to LF and UTF-8 without BOM. Any target failure exits non-zero with an
explicit incomplete-output statement. Inputs are never downloaded: acquisition and pin hashes are
in [next-stage/baselines.md](next-stage/baselines.md). Exit codes: `0` success, `1` build or target
failure, `2` usage or environment failure.

### Harness Test Suite

```bash
dotnet test FunnySharp.slnx
```

Runs the harness tests (`tests/FunnySharp.Harness.Tests/`, xUnit v3 `4.0.0`) — the ported protocol,
snippet, inventory, vertical-slice, and marker-contract suites — alongside the product test
suites.

### Performance And Reproducible Builds

```bash
dotnet fsi build.fsx -- -p verify-performance -ReceiptDirectory <results-dir>
dotnet fsi build.fsx -- -p generate-performance-docs -Verify
dotnet fsi build.fsx -- -p compare-reproducible-builds -LeftRoot <a> -RightRoot <b>
```

See [performance.md](performance.md) and [harness.md](harness.md) for the receipt/manifest contract
and the full flag surface.

## Offline Operation

After the first restore, the harness itself needs no network beyond what its child `dotnet`
commands require:

- `verify-tooling --offline` forwards an offline environment to its children. A fully offline
  pre-check additionally requires a previously restored and built tree, because dotnet restore and
  build keep their normal first-use feed behavior.
- The harness never downloads inputs for inventory regeneration; missing pinned inputs are an
  explicit failure naming them.

## Policies

- **Single runtime.** Development gates run through `build.fsx`. No PowerShell or Python tooling
  remains; do not add a parallel script entry point beside the harness.
- **Exit codes.** `0` pass, `1` verification failure, `2` usage or environment failure. The `gate`
  helper in `build.fsx` preserves them; Fun.Build alone would report every failure as `1`.
- **Formatting boundary.** `dotnet format` cannot check F# projects, so the `format` pipeline
  covers C# only; the harness F# sources are not format-checked.
- **Historical records.** Generated and append-only evidence (`docs/next-stage/inventory/generated/`,
  `docs/release-evidence/`, `docs/next-stage/review/`, `eng/evaluation/results/`) is never
  hand-edited. The retired uv-tooling plan keeps its body with a superseded banner.
- **Same-change.** A change to `eng/release-protocol.json` or the harness verifiers must update the
  corresponding harness tests in the same change; the protocol stays frozen and the tests adapt to
  it, never the reverse.

## Tooling Workflow And Promotion

`.github/workflows/tooling.yml` is informational. It runs the .NET test suites, the
`verify-tooling` pre-check, `verify-docs-snippets`, and `check-action-pins` on Ubuntu, Windows, and
macOS with `fail-fast: false`, and exposes one stable aggregate job named `tooling-gate` for future
promotion. It does not gate merges.

Promoting `tooling-gate` to a required check is a separate authorized ruleset change; the four
release contexts stay exact. Promote only when all of the following hold:

- Consecutive green matrix runs across all three OSes.
- Zero skipped harness tests in those runs.
- No infrastructure-only reruns; each green reflects that run's own results.
- A bounded, predictable matrix duration.
- A named owner accepts the check.

While the workflow is informational, a red run is triaged within one working day and is never left
red beyond one week. A red `tooling-gate` does not block merges by itself, but it must not be
ignored.
