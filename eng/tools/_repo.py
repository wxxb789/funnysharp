"""Shared repository-root resolution for the ``eng/tools`` entry-point scripts.

``verify_local.py``, ``verify_docs_snippets.py`` and ``check_action_pins.py``
import this plain stdlib helper from their own directory on ``sys.path`` (each
is a PEP 723 standalone file). ``inventory.py`` resolves its root with its own
project-marker validation instead of using this module.
"""

from __future__ import annotations

from pathlib import Path


def default_repository_root(script_file: Path) -> Path:
    # eng/tools/<script>.py -> repository root
    return script_file.resolve().parents[2]


def find_git_root(start: Path) -> Path | None:
    current = start
    while True:
        if (current / ".git").exists():
            return current
        if current.parent == current:
            return None
        current = current.parent
