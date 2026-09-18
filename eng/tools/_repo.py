"""Shared repository-root resolution for the ``eng/tools`` entry-point scripts.

The entry-point scripts are PEP 723 standalone files; this module is a plain
stdlib helper that each of them imports from its own directory on ``sys.path``.
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
