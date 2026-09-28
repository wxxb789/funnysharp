# eng/tools/ — Python contributor tooling layer

**Earned:** score ~12 — distinct domain (uv/PEP 723 tooling; PowerShell-free pre-check).

## OVERVIEW

Four standalone Python tools plus a shared repo-root helper; the contributor-facing
pre-check layer. Explicitly NOT the release gate (that is `eng/Run-Release.ps1`).

## WHERE TO LOOK

| Tool | Role |
| --- | --- |
| `verify_local.py` | Contributor pre-check: restore/build/test/examples/aspnetcore-examples/format/docs; flags `--offline`, `--json`, `--skip-docs`, `--skip-format`, `--repository-root` |
| `verify_docs_snippets.py` | Verifies the 10 primary guides' `csharp` blocks against `examples/FunnySharp.DocumentationSamples` |
| `check_action_pins.py` | Scans `.github/workflows/*` for full-SHA action pins; uv version must come from `uv.toml`, never a repeated `version:` input |
| `inventory.py` | Next-stage evidence-inventory orchestrator (11 targets + language-ext list); `--check-inputs` validates without building |
| `_repo.py` | Shared `default_repository_root` / `find_git_root`; imported as a plain sibling via `sys.path` |
| `tests/` | Stdlib `unittest` coverage for all four tools |

## CONVENTIONS

- Every tool is a PEP 723 script: `#!/usr/bin/env -S uv run --no-project`,
  `requires-python = ">=3.12,<3.13"`, empty `dependencies`,
  `from __future__ import annotations`. Stdlib only, ever.
- Invoked from the repository root: `uv run --no-project eng/tools/<tool>.py`.
  A project-local helper would break the no-project model.
- Fail closed: print remediation text and exit non-zero; never raise a traceback at the user.
- `run_command` never goes through a shell; tools never download inputs and never write
  outside caller-chosen directories.

## ANTI-PATTERNS

- NEVER add third-party dependencies (uv `--no-project`, stdlib only).
- NEVER route commands through a shell; always list-args `subprocess`.
- NEVER make these tools a release gate or let them replace `Run-Release.ps1`.
- NEVER let `check_action_pins.py` re-check `release.yml`'s action pins — they stay owned
  by the frozen `ReleaseProtocol.Tests.ps1` (R14/KTD8).
- Workflow steps in scanned YAML must be block style; flow lists and empty `uses:` are findings, not skips.

## COMMANDS

```bash
uv run --no-project eng/tools/verify_local.py
uv run --no-project eng/tools/verify_docs_snippets.py
uv run --no-project eng/tools/check_action_pins.py --verbose
uv run --no-project eng/tools/inventory.py --check-inputs
uv run --no-project python -m unittest discover -s eng/tools/tests
```

## NOTES

- Unit tests never invoke real `dotnet`/`pwsh` — fakes inject command execution; the C#
  dumper is never built in unit runs.
- CI runs the discovery suite on 3 OSes (see `.github/workflows/tooling.yml`).
- `verify_local` uses the ordinary NuGet cache — never the release isolation flags.
