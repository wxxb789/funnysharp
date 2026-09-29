# eng/tools/ — contributor tooling (now the F# harness)

**Earned:** score ~12 — distinct domain (contributor pre-check; harness-driven).

## OVERVIEW

This directory held the uv/Python contributor tooling layer. That layer was replaced by the F#
harness (Goal 07, workstream B): each tool is now a pipeline of
`dotnet fsi build.fsx -- -p <pipeline>`, implemented in `../harness/`. The directory is kept as
this pointer; the Python scripts, `uv.toml`, and `.python-version` are gone.

## WHERE TO LOOK

| Retired tool | Replaced by |
| --- | --- |
| `verify_local.py` | `dotnet fsi build.fsx -- -p verify-tooling` (`harness/ToolingVerify.fs`); flags `--offline`, `--json`, `--skip-docs`, `--skip-format`, `--repository-root` |
| `verify_docs_snippets.py` | `dotnet fsi build.fsx -- -p verify-docs-snippets` (`harness/DocsSnippets.fs`) |
| `check_action_pins.py` | `dotnet fsi build.fsx -- -p check-action-pins` (`harness/ActionPins.fs`) |
| `vertical_slice.py` | `dotnet fsi build.fsx -- -p vertical-slice` (`harness/VerticalSlice.fs`) |
| `inventory.py` | `dotnet fsi build.fsx -- -p generate-inventory` (`harness/Inventory.fs`) |
| `tests/` (unittest suites) | `../../tests/FunnySharp.Harness.Tests/` (xUnit v3) |
| `_repo.py` | `harness/Repo.fs` |

## CONVENTIONS

- Invoke from the repository root: `dotnet fsi build.fsx -- -p <pipeline> [args]`; the full
  contract is in [../../docs/harness.md](../../docs/harness.md).
- Exit codes are the harness contract: `0` pass, `1` verification failure, `2` usage or
  environment failure.
- Fail closed: print remediation text and exit non-zero; never raise a traceback at the user.
- The harness never downloads inputs and never writes outside caller-chosen directories.

## ANTI-PATTERNS

- NEVER reintroduce a Python/uv tool path here — the harness is the single tooling runtime.
- NEVER route commands through a shell; the harness invokes children as list-args processes.
- NEVER let these gates become anything other than the release/contributor checks they replace.
- NEVER let the `check-action-pins` pipeline re-check `release.yml`'s action pins — they stay
  owned by the frozen harness protocol tests (R14/KTD8).

## COMMANDS

```bash
dotnet fsi build.fsx -- -p verify-tooling
dotnet fsi build.fsx -- -p verify-docs-snippets
dotnet fsi build.fsx -- -p check-action-pins
dotnet fsi build.fsx -- -p generate-inventory --check-inputs
dotnet test FunnySharp.slnx
```
