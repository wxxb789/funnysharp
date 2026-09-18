# Next-Stage Inventory Tool

This directory is Goal 14 evidence tooling, not a shipping artifact.

- `api-inventory.csproj` / `Program.cs` — a small reflection dumper that lists the
  public surface (declared public members; protected members are not emitted) of one or more assemblies as markdown and JSON.
  It supports runtime loading (with nullable reference annotations via
  `NullabilityInfoContext`) and `MetadataLoadContext` mode for reference assemblies
  and dependency-incomplete packages.
- `uv run --no-project eng/tools/inventory.py` — reproduces the committed dumps under
  `docs/next-stage/inventory/generated/` from pinned external packages and the
  repository's Release build outputs. It exits non-zero when any target fails, so a
  partial evidence set is never reported as success.

The project is intentionally **not** part of `FunnySharp.slnx`, is not packable,
and must never become a dependency of the shipping packages or the release
pipeline. It exists so an auditor can regenerate the Goal 14 inventories from the
pinned inputs recorded in `docs/next-stage/baselines.md`.

```bash
# Usage
uv run --no-project eng/tools/inventory.py <baseline-root> <ref-pack-dir> <output-dir>

# Example for the 2026-09-17 survey
uv run --no-project eng/tools/inventory.py \
  /tmp/next-stage-baselines \
  "$HOME/.dotnet/packs/Microsoft.NETCore.App.Ref/10.0.11/ref/net10.0" \
  ~/next-stage-evidence
```

`<baseline-root>` contains one extracted directory per package named after the
`.nupkg` file without its extension. See `docs/next-stage/baselines.md` for
versions, SHA256 hashes, and acquisition instructions.

The FunnySharp inventories are generated from the repository's Release build outputs.
Override their location and the ASP.NET shared framework with `FUNNY_SHARP_BIN`,
`FUNNY_SHARP_ASPNET_BIN`, `DOTNET_ROOT`, or `ASPNET_FRAMEWORK_DIR` when the defaults
do not apply.
