#!/usr/bin/env -S uv run --no-project
# /// script
# requires-python = ">=3.12,<3.13"
# dependencies = []
# ///
"""Goal 21 model-agnostic coding evaluation runner.

The evaluation compares idiomatic C# (BCL only) against FunnySharp across five task areas:
business outcomes, collections, async streams, concurrency, and ASP.NET Core. The harness is
model-agnostic: it consumes solution files that any coding model produced from the task prompts,
then deterministically measures each run:

* compilation - build success and compiler error count;
* semantic correctness - the fixed xUnit suite shipped with the task;
* correction feedback - the number of verify rounds needed until the run is green;
* consumer-side LOC - non-blank, non-comment lines of the solution files;
* API misuse - the FunnySharp analyzer diagnostics (FS####) emitted while building.

Recorded runs live under results/<task>/<style>/run-<n>/ (solution + record.json). Transient
build trees are written under the repository artifacts directory and never committed.

Commands:
  prep-feed                Pack FunnySharp and FunnySharp.AspNetCore into the evaluation feed.
  verify TASK STYLE DIR    Verify one solution directory and write record.json inside it.
  aggregate OUT.md         Aggregate every recorded run into a markdown table.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import subprocess
import sys
import time
from pathlib import Path

REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
EVALUATION_ROOT = Path(__file__).resolve().parent
FEED_DIR = REPOSITORY_ROOT / "artifacts" / "evaluation" / "feed"
BUILD_ROOT = REPOSITORY_ROOT / "artifacts" / "evaluation" / "builds"
STYLES = ("idiomatic", "funnysharp")

DOTNET_ENV = {
    **os.environ,
    "DOTNET_CLI_TELEMETRY_OPTOUT": "1",
    "DOTNET_NOLOGO": "1",
    "DOTNET_CLI_UI_LANGUAGE": "en",
    "NUGET_PACKAGES": str(REPOSITORY_ROOT / "artifacts" / ".nuget-packages-dev"),
    "NUGET_HTTP_CACHE_PATH": str(REPOSITORY_ROOT / "artifacts" / ".nuget-http-dev"),
    "PATH": os.environ.get("PATH", "") + os.pathsep + str(Path.home() / ".dotnet"),
}


def run_command(command: list[str], cwd: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        command,
        cwd=cwd,
        env=DOTNET_ENV,
        capture_output=True,
        text=True,
        check=False,
    )


def prep_feed() -> int:
    FEED_DIR.mkdir(parents=True, exist_ok=True)
    for project in ("src/FunnySharp/FunnySharp.csproj", "src/FunnySharp.AspNetCore/FunnySharp.AspNetCore.csproj"):
        result = run_command(
            ["dotnet", "pack", project, "--configuration", "Release", "--output", str(FEED_DIR)],
            REPOSITORY_ROOT,
        )
        if result.returncode != 0:
            print(result.stdout[-4000:])
            print(result.stderr[-4000:])
            print(f"prep-feed failed for {project}", file=sys.stderr)
            return 1
    packages = sorted(p.name for p in FEED_DIR.glob("*.nupkg"))
    print("Prepared evaluation feed:")
    for package in packages:
        print(f"  {package}")
    return 0


ERROR_LINE = re.compile(r": error [A-Z]+[0-9]+:")
FS_DIAGNOSTIC = re.compile(r": (warning|error) (FS[0-9]{4})")
TEST_SUMMARY = re.compile(r"Test run summary: (Passed|Failed)!")
TEST_COUNTS = re.compile(r"total:\s*(\d+).*failed:\s*(\d+).*succeeded:\s*(\d+).*skipped:\s*(\d+)", re.DOTALL)


def count_loc(path: Path) -> int:
    """Count non-blank, non-comment lines of a C# file (block comments stripped crudely)."""
    text = path.read_text(encoding="utf-8-sig")
    text = re.sub(r"/\*.*?\*/", "", text, flags=re.DOTALL)
    count = 0
    for line in text.splitlines():
        stripped = line.strip()
        if stripped and not stripped.startswith("//"):
            count += 1
    return count


def write_nuget_config(build_dir: Path) -> None:
    relative_feed = os.path.relpath(FEED_DIR, build_dir).replace(os.sep, "/")
    build_dir.joinpath("NuGet.config").write_text(
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
        "<configuration>\n"
        "  <packageSources>\n"
        "    <clear />\n"
        f"    <add key=\"evaluation-feed\" value=\"{relative_feed}\" />\n"
        "    <add key=\"nuget.org\" value=\"https://api.nuget.org/v3/index.json\" />\n"
        "  </packageSources>\n"
        "</configuration>\n",
        encoding="utf-8",
    )


def verify(task: str, style: str, run_dir: Path, round_number: int) -> int:
    task_dir = EVALUATION_ROOT / "tasks" / task
    solution_dir = run_dir / "solution"
    template_dir = task_dir / f"template-{style}"
    if not task_dir.is_dir():
        print(f"unknown task {task}", file=sys.stderr)
        return 2
    if not solution_dir.is_dir() or not any(solution_dir.glob("*.cs")):
        print(f"{solution_dir} contains no solution files", file=sys.stderr)
        return 2

    build_dir = BUILD_ROOT / f"{task}-{style}-{run_dir.name}"
    if build_dir.exists():
        shutil.rmtree(build_dir)
    build_dir.mkdir(parents=True)

    for item in template_dir.iterdir():
        if item.is_file():
            shutil.copy2(item, build_dir / item.name)
    for test_file in sorted((task_dir / "tests").glob("*.cs")):
        shutil.copy2(test_file, build_dir / test_file.name)
    for solution_file in sorted(solution_dir.glob("*.cs")):
        shutil.copy2(solution_file, build_dir / solution_file.name)
    write_nuget_config(build_dir)

    started = time.monotonic()
    build = run_command(["dotnet", "build", "--configuration", "Release"], build_dir)
    build_seconds = time.monotonic() - started
    build_output = build.stdout + build.stderr
    compile_errors = ERROR_LINE.findall(build_output)
    fs_diagnostics = sorted(set(m.group(2) for m in FS_DIAGNOSTIC.finditer(build_output)))

    test_ok: bool | None = None
    test_total = 0
    test_failed = 0
    if build.returncode == 0:
        test = run_command(["dotnet", "test", "--configuration", "Release", "--no-build"], build_dir)
        test_output = test.stdout + test.stderr
        summary = TEST_SUMMARY.search(test_output)
        counts = TEST_COUNTS.search(test_output)
        test_ok = test.returncode == 0 and summary is not None and summary.group(1) == "Passed"
        if counts:
            test_total = int(counts.group(1))
            test_failed = int(counts.group(2))

    loc = sum(count_loc(path) for path in sorted(solution_dir.glob("*.cs")))
    record = {
        "task": task,
        "style": style,
        "run": run_dir.name,
        "round": round_number,
        "compilation": {
            "ok": build.returncode == 0,
            "errors": len(compile_errors),
            "buildSeconds": round(build_seconds, 1),
        },
        "semanticCorrectness": {
            "ok": test_ok,
            "total": test_total,
            "failed": test_failed,
        },
        "apiMisuse": {"fsDiagnostics": fs_diagnostics},
        "consumerLoc": loc,
    }
    record_path = run_dir / "record.json"
    record_path.write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(record, indent=2))
    green = build.returncode == 0 and test_ok is True
    if not green:
        # Correction feedback: echo the compiler and test signals the producer must fix.
        print("--- build output (tail) ---")
        print("\n".join(build_output.strip().splitlines()[-40:]))
        if build.returncode == 0:
            print("--- test output (tail) ---")
            print("\n".join(test_output.strip().splitlines()[-40:]))
    print("VERDICT: " + ("GREEN" if green else "RED"))
    return 0 if green else 1


def aggregate(out_path: Path) -> int:
    rows = []
    for record_path in sorted((EVALUATION_ROOT / "results").glob("*/*/run-*/record.json")):
        record = json.loads(record_path.read_text(encoding="utf-8"))
        row = {
            "task": record["task"],
            "style": record["style"],
            "run": record["run"],
            "compiled": record["compilation"]["ok"],
            "errors": record["compilation"]["errors"],
            "testsOk": record["semanticCorrectness"]["ok"],
            "testsTotal": record["semanticCorrectness"]["total"],
            "testsFailed": record["semanticCorrectness"]["failed"],
            "fsDiagnostics": ",".join(record["apiMisuse"]["fsDiagnostics"]) or "-",
            "loc": record["consumerLoc"],
        }
        rows.append(row)

    lines = [
        "# Goal 21 evaluation aggregate",
        "",
        "Generated by `eng/evaluation/runner.py aggregate` from the recorded run records.",
        "",
        "| task | style | run | compiled | errors | tests ok | total | failed | FS | LOC |",
        "| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |",
    ]
    for row in rows:
        lines.append(
            "| {task} | {style} | {run} | {compiled} | {errors} | {testsOk} | {testsTotal} | {testsFailed} | {fsDiagnostics} | {loc} |".format(**row)
        )
    out_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"wrote {out_path} ({len(rows)} runs)")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)
    sub.add_parser("prep-feed")
    verify_parser = sub.add_parser("verify")
    verify_parser.add_argument("task")
    verify_parser.add_argument("style", choices=STYLES)
    verify_parser.add_argument("run_dir", type=Path)
    verify_parser.add_argument("--round", type=int, default=1, dest="round_number")
    aggregate_parser = sub.add_parser("aggregate")
    aggregate_parser.add_argument("out", type=Path)
    arguments = parser.parse_args()

    if arguments.command == "prep-feed":
        return prep_feed()
    if arguments.command == "verify":
        return verify(arguments.task, arguments.style, arguments.run_dir, arguments.round_number)
    return aggregate(arguments.out)


if __name__ == "__main__":
    raise SystemExit(main())
