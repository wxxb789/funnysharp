# Contributor Tooling

See [harness.md](harness.md) for command ownership and fast project-scoped iteration.
The only runtime prerequisite is the SDK pinned by `global.json`.

## Daily Work

Build the changed project with `build --project <path>`. Reuse .NET incremental
outputs and `--no-restore` after dependencies are restored. Run a small relevant
test selection only when it proves changed behavior. `dotnet fsi build.fsx -- -p <pipeline>`
loads the F# module sources and orchestrates the command directly. The harness
project is a library for unit tests, not a command executable.

## Manual Comprehensive Checks

`verify-tooling`, full test suites, package-only vertical slices, compatibility,
trim/AOT, evaluation and benchmark measurements are explicit manual commands.
Do not automatically run them in Agent iterations or push/PR CI.

`verify-tooling` runs locked restore, Release build, tests, both examples, format
and snippets. It is not release evidence. `--json`, `--skip-docs`, `--skip-format`
and `--repository-root` remain available. The obsolete `--offline` flag is rejected;
it previously only set a Python/uv variable and did not make .NET offline.

## CI

Automatic `tooling.yml` builds the harness and checks snippets and Git action pins.
`release.yml` runs package/platform tests only when manually dispatched. Benchmarks
remain developer-machine commands and are not triggered by CI.
