# eng/ — Development Tools

Shipping code is C#; this F# harness is development-only.

## Ownership

- `build.fsx` owns FSI orchestration and loads F# source modules directly.
- `harness/FunnySharp.Harness.fsproj` is a testable library, not an executable.
- `harness/Cli.fs` owns argument transforms; `release-protocol.json` owns release order.
- `harness/ReleaseRun.fs` executes commands. Git owns source revision/change identity.
- `harness/Compatibility.fs` owns package-only consumer checks.
- `harness/ApiBaseline.fs` owns public API reflection and baseline comparison.
- `harness/Performance.fs` checks allocation budgets; `PerformanceDocs.fs` renders tables.
- `harness/Evaluation.fs` runs task oracles; `evaluation/` holds tasks and historical studies.
- Harness tests live in `../tests/FunnySharp.Harness.Tests/`.

See `../docs/harness.md` for available pipelines and `../docs/release-readiness.md`
for release requirements. There is no independent release audit, provenance
transport, frozen replay, XML identity-binding or offline packet-verification layer.

## Rules

- Use Git for revisions and tracked change detection. Do not calculate or verify
  SHA file fingerprints, receipt/log hashes, or package/binary checksums.
- Preserve locked NuGet restore, compiler diagnostics, allocation budgets, semantic
  tests and package trust-boundary checks. They test behavior, not identity ceremony.
- Heavy tests, full pre-check/release, platform consumers and benchmarks are manual only; never auto-trigger them in CI or Agent iterations.
- Run focused checks for changed behavior. Add controlled failing cases where they
  prove the checker rejects a real failure, not a changed receipt literal.
- Only `.github/workflows/release.yml` gates release; tooling CI is informational.
- Benchmarks run locally; CI uses `release -SkipBenchmarks`.
- Historical evidence and frozen studies are read-only records, not current gates.
- Performance policy is editorially owned; the verifier never changes budgets.
- F# warnings fail builds. `dotnet format` covers C# only.

## Commands

```bash
dotnet fsi build.fsx -- -p verify-tooling
dotnet fsi build.fsx -- -p verify-api-baseline
dotnet fsi build.fsx -- -p verify-docs-snippets
dotnet fsi build.fsx -- -p verify-performance -ReceiptDirectory <results>
dotnet fsi build.fsx -- -p release -AttemptId <id> -SkipBenchmarks
```
