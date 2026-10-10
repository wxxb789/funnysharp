# The F# Build Harness

Run development tools from the repository root:

```bash
dotnet fsi build.fsx -- -p <pipeline> [args]
```

A bare invocation prints the available pipelines and runs nothing. `build.fsx`
is the FSI orchestration layer: it loads the testable F# source modules and executes
commands directly. No compiled harness executable or cached binary is launched.

## Ownership

- `build.fsx` defines command orchestration and help.
- `eng/harness/Cli.fs` owns pure argument transformations.
- `eng/release-protocol.json` owns release step order and modes.
- `eng/harness/*.fs` owns each tool's behavior.
- `tests/FunnySharp.Harness.Tests` tests the tools. Product behavior stays in the
  C# test projects; evaluation tasks retain their semantic oracles.
- `docs/` contains guides and contracts, not another executable verification layer.

There is no separate root `harness/` directory.

## Fast Development Iterations

Use the project's native incrementality. No custom cache or source hashing is needed.

```bash
# Build only the changed project and its dependencies.
dotnet fsi build.fsx -- -p build --project src/FunnySharp/FunnySharp.csproj --no-restore

# Explicit small test run after the relevant project is built.
dotnet tests/FunnySharp.Harness.Tests/bin/Debug/net10.0/FunnySharp.Harness.Tests.dll -class '*ProgramTests*'

# Source-loaded check: no preliminary harness build required.
dotnet fsi build.fsx -- -p verify-docs-snippets
```

FSI loads the F# modules and forwards gate arguments directly; it does not build
the harness project before running a command. The library project exists for module
tests and IDE checks.
`build`, `test`, and `format` accept `--project <path>`; remaining native options
pass through to `dotnet`. Without a project they target the solution. A bare
invocation prints help and runs nothing. Fun.Build and its transitive dependencies
are removed; one command registry owns dispatch and help.

Heavy tests, full pre-checks, package/platform/AOT scenarios, evaluation cohorts and
benchmarks are **manual only**. Do not run them automatically during Agent development.
`release.yml` is workflow-dispatch only. Automatic tooling CI builds the harness
and checks snippets/action references; it never runs test suites, the full pre-check
or benchmarks. `--offline` was removed because its old UV flag did not control .NET
network access. Use native restore options and an already-restored build as needed.

## Pipelines

| Pipeline | Purpose |
| --- | --- |
| `build` | Build the solution. |
| `test` | Run the solution tests. |
| `format` | Check C# formatting. |
| `check-action-pins` | Check Git commit references for remote actions. |
| `verify-docs-snippets` | Compare guide samples with compiling source regions. |
| `verify-api-baseline` | Compare the reflected public API with its committed baseline. |
| `verify-tooling` | Run contributor checks, not a release. |
| `generate-inventory` | Generate API inventories. |
| `vertical-slice` | Exercise package-only application consumers and their comparison. |
| `verify-performance` | Check measured allocations against policy budgets. |
| `generate-performance-docs` | Generate or check performance tables. |
| `verify-ruleset` | Check the required GitHub status contexts. |
| `compatibility` | Restore, build and run package consumers, including trim/AOT scenarios. |
| `release` | Execute release steps and retain command results and logs. |
| `eval-prep-feed` | Prepare evaluation packages. |
| `eval-verify` | Build and test an evaluation solution. |
| `eval-aggregate` | Summarize evaluation results. |
| `rawloc` | Count call-site lines. |
| `loc-extract` | Extract a named method body. |

Use `-p <pipeline> --help` for arguments. Gate exit codes remain command-specific;
the launcher returns the command's code without flattening failures.

## Git, Not File Fingerprints

Git identifies source revisions and reports tracked changes. The active harness
does not compute SHA checksums or verify file, log, package, source, or receipt
hashes. Git commit references, action pins, and NuGet's locked restore are not
custom file-hashing mechanisms and remain in use.

The independent release audit, provenance transport, frozen replay, XML build
bindings, reproducible-byte comparison, and standalone packet verifiers were
removed. They duplicated identity checks without establishing product behavior.
Actual API, documentation, package consumer, test, and allocation checks remain
at their owning tools. Historical evidence and frozen snapshots stay unchanged;
they are records, not current acceptance gates.

## Focused Verification

Run the checks affected by a change. A semantic test must fail when its behavior
is broken; a passing receipt or unchanged file is not proof of correctness.
See [harness-ablation.md](harness-ablation.md) for the removal boundary and results.

`dotnet format` checks C# only. F# still builds with warnings treated as errors.
