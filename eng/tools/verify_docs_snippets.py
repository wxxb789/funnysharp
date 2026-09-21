#!/usr/bin/env -S uv run --no-project
# /// script
# requires-python = ">=3.12,<3.13"
# dependencies = []
# ///
"""Verify documentation C# snippets against their source regions.

This is a behavior-equivalent Python port of the authoritative PowerShell
verifier at ``examples/FunnySharp.DocumentationSamples/VerifyDocumentationSnippets.ps1``.
The PowerShell verifier remains the reference implementation; a change to it
must update this port in the same change.

The port keeps the PowerShell contract:

* an explicit primary-guide list (no directory discovery);
* a recursive ``*.cs`` scan of the samples root, excluding ``bin`` and ``obj``
  directories (case-insensitively, like PowerShell's ``-notmatch``);
* snippet regions delimited by ``// <snippet NAME>`` / ``// </snippet>`` with a
  common-indent dedent over the non-blank lines;
* every ```` ```csharp ```` fence in a guide must immediately follow a
  ``<!-- documentation-sample: NAME -->`` marker, and every region must be
  referenced by exactly one fence;
* line-by-line equality with ``File.ReadAllLines`` semantics (universal
  newlines, BOM detection, no phantom line for a trailing newline);
* PowerShell string and hashtable semantics where they are observable: region
  names and compared lines are case-insensitive, and empty region/fence bodies
  reproduce PowerShell's descending inclusive-range behavior;
* exit status 0 on success, 1 on any failure. Nothing is ever written.

Run with::

    uv run --no-project eng/tools/verify_docs_snippets.py
"""

from __future__ import annotations

import argparse
import codecs
import re
import sys
from dataclasses import dataclass
from pathlib import Path

from _repo import default_repository_root, find_git_root

PRIMARY_GUIDES: tuple[str, ...] = (
    "aspnet-core.md",
    "collections.md",
    "concurrency.md",
    "effects.md",
    "function-composition.md",
    "immutable-updates.md",
    "state-machines.md",
    "unit-result.md",
    "validation.md",
)

REGION_START_PATTERN = re.compile(
    r"^\s*//\s*<snippet\s+(?P<name>DocumentationSamples\.[A-Za-z0-9.]+)>\s*$"
)
REGION_END_PATTERN = re.compile(r"^\s*//\s*</snippet>\s*$")
MARKER_PATTERN = re.compile(
    r"^<!-- documentation-sample: (?P<name>DocumentationSamples\.[A-Za-z0-9.]+) -->$"
)
CSHARP_FENCE_PATTERN = re.compile(r"^```csharp\s*$")
FENCE_END_PATTERN = re.compile(r"^```\s*$")
# PowerShell's -notmatch is case-insensitive, so 'Bin' and 'OBJ' are skipped too.
BUILD_DIRECTORY_PATTERN = re.compile(r"[\\/](?:bin|obj)[\\/]", re.IGNORECASE)
LEADING_WHITESPACE_PATTERN = re.compile(r"^\s*")
# Universal newlines, matching StreamReader.ReadLine/ReadAllLines. Python's
# str.splitlines() would also split on characters .NET treats as content.
LINE_BREAK_PATTERN = re.compile(r"\r\n|\r|\n")


def _decode_bytes(data: bytes) -> str:
    """Decode bytes the way StreamReader does: BOM detection, replacement fallback."""
    if data.startswith(codecs.BOM_UTF8):
        return data[len(codecs.BOM_UTF8) :].decode("utf-8", errors="replace")
    # UTF-32 LE shares its first two bytes with the UTF-16 LE BOM, so check it first.
    if data.startswith(codecs.BOM_UTF32_LE):
        return data[len(codecs.BOM_UTF32_LE) :].decode("utf-32-le", errors="replace")
    if data.startswith(codecs.BOM_UTF32_BE):
        return data[len(codecs.BOM_UTF32_BE) :].decode("utf-32-be", errors="replace")
    if data.startswith(codecs.BOM_UTF16_LE):
        return data[len(codecs.BOM_UTF16_LE) :].decode("utf-16-le", errors="replace")
    if data.startswith(codecs.BOM_UTF16_BE):
        return data[len(codecs.BOM_UTF16_BE) :].decode("utf-16-be", errors="replace")
    return data.decode("utf-8", errors="replace")


def read_all_lines(path: Path) -> list[str]:
    """Equivalent of [System.IO.File]::ReadAllLines: universal newlines, BOM aware.

    A trailing line terminator does not produce a final empty line, and an empty
    file produces no lines.
    """
    text = _decode_bytes(path.read_bytes())
    if text == "":
        return []
    lines = LINE_BREAK_PATTERN.split(text)
    if lines[-1] == "":
        lines.pop()
    return lines


def _inclusive_range(lines: list[str], start: int, end: int) -> list[str]:
    """Index lines with an inclusive PowerShell range.

    PowerShell emits a descending sequence when the upper bound is below the
    lower bound (``1..0`` yields ``1, 0``), so a zero-length region or fence body
    yields the closing marker line followed by the opening marker line instead
    of an empty list. The port reproduces that for exact parity.
    """
    if start <= end:
        return lines[start : end + 1]
    return [lines[index] for index in range(start, end - 1, -1)]


def _name_key(name: str) -> str:
    """Match PowerShell's case-insensitive hashtable keys (-eq is also case-insensitive)."""
    return name.lower()


def _lines_equal(left: str, right: str) -> bool:
    """Match PowerShell's case-insensitive -eq/-ne string comparison."""
    return left == right or left.lower() == right.lower()


def _common_indent(content: list[str]) -> int:
    indents = [
        len(LEADING_WHITESPACE_PATTERN.match(line).group(0))
        for line in content
        if line.strip() != ""
    ]
    return min(indents) if indents else 0


@dataclass(frozen=True)
class Region:
    name: str
    path: Path
    line: int
    content: list[str]


def _iter_source_files(samples_root: Path) -> list[Path]:
    return sorted(
        path
        for path in samples_root.rglob("*.cs")
        if path.is_file() and BUILD_DIRECTORY_PATTERN.search(str(path)) is None
    )


def _collect_regions(samples_root: Path, failures: list[str]) -> dict[str, Region]:
    regions: dict[str, Region] = {}
    for source_path in _iter_source_files(samples_root):
        source_lines = read_all_lines(source_path)
        line_index = 0
        while line_index < len(source_lines):
            start = REGION_START_PATTERN.match(source_lines[line_index])
            if start is None:
                line_index += 1
                continue

            name = start.group("name")
            key = _name_key(name)
            if key in regions:
                failures.append(
                    f"Duplicate source region '{name}' in {source_path}:{line_index + 1}."
                )
                line_index += 1
                continue

            end_index = line_index + 1
            while (
                end_index < len(source_lines)
                and REGION_END_PATTERN.match(source_lines[end_index]) is None
            ):
                end_index += 1

            if end_index == len(source_lines):
                failures.append(
                    f"Source region '{name}' in {source_path}:{line_index + 1} "
                    "has no closing snippet marker."
                )
                line_index += 1
                continue

            content = _inclusive_range(source_lines, line_index + 1, end_index - 1)
            indent = _common_indent(content)
            content = ["" if line.strip() == "" else line[indent:] for line in content]
            regions[key] = Region(
                name=name, path=source_path, line=line_index + 1, content=content
            )
            line_index = end_index + 1

    return regions


def _compare_region(
    guide: str,
    fence_line: int,
    end_index: int,
    name: str,
    markdown_lines: list[str],
    regions: dict[str, Region],
    used_regions: set[str],
    failures: list[str],
) -> None:
    """Compare one marked fence body with its source region, like PowerShell.

    Mirrors the reference verifier: a region is recorded as used even when it
    turns out to be missing, and only the first differing snippet line is
    reported (the PowerShell comparison loop breaks on its first mismatch).
    """
    key = _name_key(name)
    if key in used_regions:
        failures.append(f"{guide}:{fence_line} reuses source region '{name}'.")
    else:
        used_regions.add(key)

    if key not in regions:
        failures.append(
            f"{guide}:{fence_line} references missing source region '{name}'."
        )
        return

    snippet_lines = _inclusive_range(markdown_lines, fence_line, end_index - 1)
    source = regions[key]
    if len(snippet_lines) != len(source.content):
        failures.append(
            f"{guide}:{fence_line} differs from '{name}' "
            f"in {source.path}:{source.line}."
        )
        return

    for content_index, snippet_line in enumerate(snippet_lines):
        if _lines_equal(snippet_line, source.content[content_index]):
            continue
        failures.append(
            f"{guide}:{fence_line} differs from '{name}' "
            f"in {source.path}:{source.line} "
            f"at snippet line {content_index + 1}."
        )
        break


def _verify(
    repository_root: Path, samples_root: Path, guides: tuple[str, ...]
) -> tuple[list[str], int]:
    failures: list[str] = []
    regions = _collect_regions(samples_root, failures)
    used_regions: set[str] = set()
    snippet_count = 0

    for guide in guides:
        guide_path = repository_root / "docs" / guide
        if not guide_path.exists():
            failures.append(f"Missing primary guide '{guide_path}'.")
            continue

        markdown_lines = read_all_lines(guide_path)
        line_index = 0
        while line_index < len(markdown_lines):
            if CSHARP_FENCE_PATTERN.match(markdown_lines[line_index]) is None:
                line_index += 1
                continue

            snippet_count += 1
            marker = (
                MARKER_PATTERN.match(markdown_lines[line_index - 1])
                if line_index > 0
                else None
            )
            name = marker.group("name") if marker is not None else None

            if name is None:
                failures.append(
                    f"{guide}:{line_index + 1} must immediately follow a "
                    "documentation-sample marker."
                )

            end_index = line_index + 1
            while (
                end_index < len(markdown_lines)
                and FENCE_END_PATTERN.match(markdown_lines[end_index]) is None
            ):
                end_index += 1

            if end_index == len(markdown_lines):
                failures.append(f"{guide}:{line_index + 1} has no closing code fence.")
                line_index += 1
                continue

            if name is not None:
                _compare_region(
                    guide,
                    line_index + 1,
                    end_index,
                    name,
                    markdown_lines,
                    regions,
                    used_regions,
                    failures,
                )

            line_index = end_index + 1

    for key, source in regions.items():
        if key not in used_regions:
            failures.append(
                f"Source region '{source.name}' in {source.path}:{source.line} "
                "has no documentation fence."
            )

    return failures, snippet_count


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="verify_docs_snippets.py",
        description=(
            "Verify that documentation C# snippets match their source regions. "
            "Exit status is 0 on success and 1 on any failure."
        ),
    )
    parser.add_argument(
        "--repository-root",
        type=Path,
        default=None,
        metavar="PATH",
        help=(
            "repository root containing docs/ (default: resolved from the script "
            "location; fails with remediation when that is not inside a git repo). "
            "Fixture runs may point this at a temporary tree."
        ),
    )
    parser.add_argument(
        "--samples-root",
        type=Path,
        default=None,
        metavar="PATH",
        help=(
            "directory recursively scanned for *.cs snippet regions "
            "(default: <repository-root>/examples/FunnySharp.DocumentationSamples)."
        ),
    )
    return parser


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)

    if args.repository_root is None:
        repository_root = default_repository_root(Path(__file__))
        if find_git_root(repository_root) is None:
            print(
                f"error: '{repository_root}' is not inside a git repository, so the "
                "repository root cannot be derived from the script location.",
                file=sys.stderr,
            )
            print(
                "remediation: run this script from a FunnySharp checkout or pass "
                "--repository-root <path>.",
                file=sys.stderr,
            )
            return 1
    else:
        repository_root = args.repository_root.resolve()

    samples_root = (
        args.samples_root.resolve()
        if args.samples_root is not None
        else repository_root / "examples" / "FunnySharp.DocumentationSamples"
    )
    if not samples_root.is_dir():
        print(
            f"error: samples root '{samples_root}' does not exist or is not a directory.",
            file=sys.stderr,
        )
        print(
            "remediation: pass --samples-root <path> pointing at the "
            "FunnySharp.DocumentationSamples directory.",
            file=sys.stderr,
        )
        return 1

    try:
        failures, snippet_count = _verify(repository_root, samples_root, PRIMARY_GUIDES)
    except OSError as error:
        print(f"error: {error}", file=sys.stderr)
        return 1

    if failures:
        for failure in failures:
            print(f"error: {failure}", file=sys.stderr)
        return 1

    print(
        f"Verified {snippet_count} C# documentation snippets across "
        f"{len(PRIMARY_GUIDES)} primary guides."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
