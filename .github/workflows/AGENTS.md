# CI Workflows

- `tooling.yml`: push/PR automatic **light checks only** — incremental harness build,
  documentation snippets and Git action pins. No tests, pre-check or benchmarks.
- `release.yml`: **manual workflow_dispatch only** — canonical package build,
  full tests, platform consumers, trimming and AOT. Benchmarks are not run in CI.

Heavy tests and benchmarks require explicit human selection. Agents do not trigger
these workflows or full checks as part of routine development iterations.

Remote actions retain full Git commit references. Commands execute through
`dotnet fsi build.fsx -- -p <pipeline>`; FSI loads modules without a bootstrap build.
Keep log upload for troubleshooting; no custom hash verification.

Manual release context names remain `release / win-x64`, `release / linux-x64`,
`release / osx-arm64`, and `release / osx-x64-consumer`. They are no longer emitted
on every push/PR. Existing remote rulesets requiring them may need an authorized
update; do not claim branch protection changed from local YAML edits.
