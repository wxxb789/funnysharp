# CI Workflows

- `tooling.yml`: push/PR automatic **light checks only** — incremental harness build,
  documentation snippets and Git action pins. No tests, pre-check or benchmarks.
  Its aggregate `tooling-gate` is the required check for ordinary PR merges.
- `release.yml`: **manual workflow_dispatch only** — canonical package build,
  full tests, platform consumers, trimming and AOT. Benchmarks are not run in CI.

Heavy tests and benchmarks require explicit human selection. Agents do not trigger
these workflows or full checks as part of routine development iterations.

Remote actions retain full Git commit references. Commands execute through
`dotnet fsi build.fsx -- -p <pipeline>`; FSI loads modules without a bootstrap build.
Keep log upload for troubleshooting; no custom hash verification.

Default-branch merge rules require only `tooling-gate`, bound to GitHub Actions,
with strict up-to-date checks and zero bypass actors. Verify the remote ruleset;
local YAML edits do not change branch protection.

Manual release contexts `release / win-x64`, `release / linux-x64`,
`release / osx-arm64`, and `release / osx-x64-consumer` must pass for release
acceptance. They are not required checks for ordinary PR merges.
