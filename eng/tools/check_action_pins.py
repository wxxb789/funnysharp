#!/usr/bin/env -S uv run --no-project
# /// script
# requires-python = ">=3.12,<3.13"
# dependencies = []
# ///
"""Check that every remote workflow action is pinned to a full commit SHA.

Text-level checks over ``.github/workflows/*.yml`` and ``*.yaml`` (no YAML
parser and no third-party dependencies):

* a remote ``uses:`` reference must be ``owner/repo@<40-hex-sha>`` followed by
  a ``# <version>`` comment; local ``./path`` actions are exempt;
* ``tooling.yml`` must take the uv version from ``uv.toml``'s
  ``required-version`` instead of repeating a ``version:`` input anywhere in
  its ``astral-sh/setup-uv`` step.

The check is deliberately text-level and fails closed on forms it cannot parse:

* an empty ``uses:`` value (the block-continuation form, where the value sits
  on the following indented line) is a finding, not a silent skip;
* flow-style list items (``- {uses: owner/repo@sha}``) are outside the text
  model this check recognizes; write workflow steps in block style so they are
  scanned.

``release.yml`` is scanned for third-party owners only: the frozen PowerShell
protocol test (``eng/tests/ReleaseProtocol.Tests.ps1``) owns its ``actions/*``
pins, so this check never creates a second owner for the same pins (KTD8, R14)
while still catching an unpinned non-``actions`` action there.

Run with::

    uv run --no-project eng/tools/check_action_pins.py [--verbose]

Exit codes: 0 all checks pass, 1 findings, 2 environment or usage failure.
"""

from __future__ import annotations

import argparse
import re
import sys
from collections.abc import Sequence
from dataclasses import dataclass
from pathlib import Path

from _repo import default_repository_root

WORKFLOWS_RELATIVE_PATH = Path(".github") / "workflows"
WORKFLOW_SUFFIXES = (".yml", ".yaml")

# release.yml's `actions/*` pins stay with the frozen PowerShell protocol test;
# checking them here would create a divergent second owner for the same pins
# (R14). Third-party owners in that workflow are still checked.
RELEASE_WORKFLOW_NAMES = frozenset({"release.yml", "release.yaml"})
FROZEN_ACTION_OWNER = "actions"

TOOLING_WORKFLOW_NAME = "tooling.yml"
UV_SETUP_ACTION = "astral-sh/setup-uv"
UV_PIN_FILE_NAME = "uv.toml"
UV_REQUIRED_VERSION_PATTERN = re.compile(
    r"^[ \t]*required-version[ \t]*=[ \t]*['\"]==\d+\.\d+\.\d+['\"][ \t]*$",
    re.MULTILINE,
)

USES_PATTERN = re.compile(
    r"^(?P<indent>[ \t]*)(?P<dash>-\s+)?uses\s*:\s*(?P<value>.*?)[ \t]*$"
)
PINNED_REFERENCE_PATTERN = re.compile(
    r"^(?P<owner>[^/@\s]+)/(?P<repo>[^/@\s]+)@(?P<sha>[0-9a-f]{40})$"
)
LIST_ITEM_PATTERN = re.compile(r"^(?P<indent>[ \t]*)-[ \t]")
# Matches ``version`` as a mapping key in block style (``version:`` or
# ``"version":``) or inside a flow mapping (``with: {version: ...}``).
VERSION_KEY_PATTERN = re.compile(
    r"(?:^|[{\[,])[ \t]*[\"']?version[\"']?[ \t]*:"
)


@dataclass(frozen=True)
class Finding:
    """One pinning violation at a precise file:line location."""

    path: Path
    line: int
    message: str

    def format(self, repository_root: Path) -> str:
        try:
            relative = self.path.relative_to(repository_root).as_posix()
        except ValueError:
            relative = self.path.as_posix()
        return f"{relative}:{self.line}: {self.message}"


@dataclass(frozen=True)
class UsesReference:
    """A ``uses:`` reference parsed from one workflow line."""

    line: int
    indent: int
    reference: str
    comment: str

    @property
    def is_local(self) -> bool:
        return self.reference.startswith("./")


def unquote(value: str) -> str:
    if len(value) >= 2 and value[0] == value[-1] and value[0] in {"'", '"'}:
        return value[1:-1]
    return value


def split_uses_value(raw: str) -> tuple[str, str]:
    """Split a ``uses:`` value into (reference, inline comment).

    YAML starts a comment at a ``#`` that is preceded by whitespace; quoting is
    honored so a ``#`` inside a quoted scalar is not treated as a comment.
    """

    quote = ""
    for index, character in enumerate(raw):
        if quote:
            if character == quote:
                quote = ""
        elif character in {"'", '"'}:
            quote = character
        elif character == "#" and (index == 0 or raw[index - 1].isspace()):
            return unquote(raw[:index].rstrip()), raw[index:].strip()
    return unquote(raw.strip()), ""


def iter_uses_references(lines: Sequence[str]) -> list[UsesReference]:
    """Return every ``uses:`` reference in one workflow's lines, in file order."""

    references: list[UsesReference] = []
    for line_number, line in enumerate(lines, start=1):
        match = USES_PATTERN.match(line)
        if match is None:
            continue
        value, comment = split_uses_value(match.group("value"))
        # An empty value (the block-continuation form ``uses:`` followed by an
        # indented value line) is kept so it fails the pin check below instead
        # of being silently skipped.
        # Column where the ``uses`` token itself starts, so ``- uses:`` and
        # ``uses:`` forms both anchor their step block correctly.
        indent = len(match.group("indent")) + (2 if match.group("dash") else 0)
        references.append(UsesReference(line_number, indent, value, comment))
    return references


def step_block_lines(
    lines: Sequence[str], uses: UsesReference
) -> list[tuple[int, str]]:
    """Return the (line number, text) pairs of the step block owning ``uses``.

    The block starts at the list item that introduces the step (the ``uses``
    line itself for ``- uses:``) and ends before the next list item at that
    indentation or any dedent out of the item, so a ``version:`` input written
    before ``uses:`` is still part of the block.
    """

    dash_indent = uses.indent - 2
    first = uses.line
    for line_number in range(uses.line, 0, -1):
        line = lines[line_number - 1]
        item = LIST_ITEM_PATTERN.match(line)
        if item is not None:
            item_indent = len(item.group("indent"))
            if item_indent < uses.indent:
                first = line_number
                dash_indent = item_indent
                break

    block: list[tuple[int, str]] = []
    for line_number in range(first, len(lines) + 1):
        line = lines[line_number - 1]
        if line_number > first and line.strip():
            indent = len(line) - len(line.lstrip(" \t"))
            if indent <= dash_indent:
                break
        block.append((line_number, line))
    return block


def check_workflow_text(
    path: Path,
    references: Sequence[UsesReference],
    *,
    ignored_owners: frozenset[str] = frozenset(),
) -> list[Finding]:
    """Return every pinning finding for one workflow's parsed references.

    ``ignored_owners`` exempts owners another checker owns (the frozen test owns
    ``actions/*`` in release.yml).
    """

    findings: list[Finding] = []
    for uses in references:
        if uses.is_local:
            continue
        if uses.reference.split("/", 1)[0] in ignored_owners:
            continue
        if PINNED_REFERENCE_PATTERN.match(uses.reference) is None:
            findings.append(
                Finding(
                    path,
                    uses.line,
                    f"uses reference '{uses.reference}' must be pinned as "
                    "owner/repo@<40-hex-sha> with a '# <version>' comment",
                )
            )
            continue
        if not uses.comment.lstrip("#").strip():
            findings.append(
                Finding(
                    path,
                    uses.line,
                    f"uses reference '{uses.reference}' is missing the trailing "
                    "'# <version>' comment",
                )
            )
    return findings


def check_tooling_pins(
    path: Path,
    references: Sequence[UsesReference],
    lines: Sequence[str],
    repository_root: Path,
) -> list[Finding]:
    """Assert tooling.yml takes the uv version from uv.toml, not a version input."""

    findings: list[Finding] = []
    setup_uv = [
        uses
        for uses in references
        if uses.reference == UV_SETUP_ACTION
        or uses.reference.startswith(UV_SETUP_ACTION + "@")
    ]
    for uses in setup_uv:
        for line_number, line in step_block_lines(lines, uses):
            if VERSION_KEY_PATTERN.search(line):
                findings.append(
                    Finding(
                        path,
                        line_number,
                        "the setup-uv step must not set 'version:'; the uv "
                        "version comes from uv.toml's required-version",
                    )
                )
    if setup_uv:
        uv_pin_file = repository_root / UV_PIN_FILE_NAME
        try:
            uv_pin_text = uv_pin_file.read_text(encoding="utf-8", errors="replace")
        except OSError:
            declared = False
        else:
            declared = UV_REQUIRED_VERSION_PATTERN.search(uv_pin_text) is not None
        if not declared:
            findings.append(
                Finding(
                    path,
                    setup_uv[0].line,
                    f"{TOOLING_WORKFLOW_NAME} must take the uv version from "
                    f"'{UV_PIN_FILE_NAME}' required-version, but that pin is "
                    "missing or is not an exact '==x.y.z' specification",
                )
            )
    return findings


def workflow_files(workflows_directory: Path) -> list[Path]:
    """Return the workflow files to scan, sorted by name."""

    return sorted(
        (
            path
            for path in workflows_directory.iterdir()
            if path.is_file() and path.suffix in WORKFLOW_SUFFIXES
        ),
        key=lambda path: path.name,
    )


def check_repository(repository_root: Path) -> tuple[list[Path], list[Finding]]:
    """Check every workflow under ``repository_root``.

    Returns the scanned files (in scope) and all findings; ``release.yml`` is
    scanned for third-party owners while its ``actions/*`` pins stay with the
    frozen PowerShell protocol test.
    """

    workflows_directory = repository_root / WORKFLOWS_RELATIVE_PATH
    scanned: list[Path] = []
    findings: list[Finding] = []
    for path in workflow_files(workflows_directory):
        text = path.read_text(encoding="utf-8", errors="replace")
        lines = text.splitlines()
        references = iter_uses_references(lines)
        scanned.append(path)
        if path.name in RELEASE_WORKFLOW_NAMES:
            findings.extend(
                check_workflow_text(
                    path,
                    references,
                    ignored_owners=frozenset({FROZEN_ACTION_OWNER}),
                )
            )
            continue
        findings.extend(check_workflow_text(path, references))
        if path.name == TOOLING_WORKFLOW_NAME:
            findings.extend(check_tooling_pins(path, references, lines, repository_root))
    return scanned, findings


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="check_action_pins.py",
        description=(
            "Check that remote workflow actions are pinned to full commit SHAs "
            "with version comments, and that tooling.yml reads the uv pin from "
            "uv.toml's required-version."
        ),
    )
    parser.add_argument(
        "--verbose",
        action="store_true",
        help="print every scanned workflow file, including files without findings.",
    )
    parser.add_argument(
        "--repository-root",
        type=Path,
        default=None,
        metavar="PATH",
        help="repository root (default: resolved from the script location).",
    )
    return parser


def main(argv: Sequence[str] | None = None) -> int:
    args = build_parser().parse_args(sys.argv[1:] if argv is None else list(argv))
    repository_root = (
        default_repository_root(Path(__file__))
        if args.repository_root is None
        else Path(args.repository_root).resolve()
    )
    workflows_directory = repository_root / WORKFLOWS_RELATIVE_PATH
    if not workflows_directory.is_dir():
        print(
            f"ERROR: workflow directory '{workflows_directory}' was not found.",
            file=sys.stderr,
        )
        return 2
    scanned, findings = check_repository(repository_root)
    if args.verbose:
        for path in scanned:
            print(f"checked {path.relative_to(repository_root).as_posix()}")
    if findings:
        for finding in findings:
            print(finding.format(repository_root), file=sys.stderr)
        print(
            f"FAIL: {len(findings)} pinning finding(s) in {len(scanned)} "
            "workflow file(s).",
            file=sys.stderr,
        )
        return 1
    print(
        f"OK: {len(scanned)} workflow file(s) checked; every remote action is "
        "pinned to a full commit SHA with a '# <version>' comment."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
