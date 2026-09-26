#!/usr/bin/env -S uv run --no-project
# /// script
# requires-python = ">=3.12,<3.13"
# dependencies = []
# ///
"""PowerShell-free local verification entry (local pre-check, not release evidence).

This runs the subset of ``eng/release-protocol.json`` steps that works outside
the release environment, in protocol order: locked-mode restore, Release build,
tests, both examples, the formatter check, and the PowerShell-free
documentation-snippet verifier (``eng/tools/verify_docs_snippets.py``, executed
with this interpreter). It uses the ordinary NuGet cache and never the release
isolation flags.

The verdicts reproduce the frozen PowerShell verifier's semantics for those
steps rather than trusting ``dotnet`` exit codes alone: the build log must show
``Build succeeded.`` with zero warnings and errors, the test log must show a
positive all-passing ``Test run summary`` plus a passed result line for each
test assembly, both examples must print their established success lines, and
the documentation verifier must print its ``Verified N ...`` result line.
Because that logic is inline in ``eng/Verify-Release.ps1`` and the verifier is
frozen, ``MARKER_CONTRACT`` asserts its marker literals are still present before
any step runs; a verifier change trips this check instead of silently drifting.

Everything else in the release protocol (clean, pack, performance,
compatibility, and the PowerShell-only steps) is reported as not run. This tool
is a contributor pre-check: it is not release evidence, and it never replaces
the release gate.

Usage::

    uv run --no-project eng/tools/verify_local.py [--offline] [--json]
        [--skip-docs] [--skip-format] [--repository-root PATH]

Exit codes: 0 pass, 1 check failure, 2 environment or usage failure.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import subprocess
import sys
from collections.abc import Callable, Mapping, Sequence
from dataclasses import dataclass
from pathlib import Path

from _repo import default_repository_root, find_git_root

REQUIRED_PYTHON = (3, 12)

# The release-protocol steps this pre-check runs locally, in protocol order.
LOCAL_STEPS: tuple[str, ...] = (
    "restore",
    "build",
    "test",
    "examples",
    "aspnetcore-examples",
    "format",
    "docs",
)

# Every eng/release-protocol.json (full mode) step outside local scope, in
# protocol order. tests/test_verify_local.py proves this equals the protocol's
# step list minus the locally run steps, so a protocol change trips the suite.
NOT_RUN_STEPS: tuple[str, ...] = (
    "clean",
    "pack",
    "performance-protocol-tests",
    "release-protocol-tests",
    "benchmark-preflight",
    "benchmark",
    "performance-verify",
    "performance-docs-verify",
    "competitor-performance-docs-verify",
    "compatibility",
)

TEST_ASSEMBLY_RELATIVE_PATHS: tuple[str, ...] = (
    "tests/FunnySharp.Tests/bin/Release/net10.0/FunnySharp.Tests.dll",
    "tests/FunnySharp.AspNetCore.Tests/bin/Release/net10.0/FunnySharp.AspNetCore.Tests.dll",
)

VERIFIER_RELATIVE_PATH = Path("eng") / "Verify-Release.ps1"
DOCS_VERIFIER_RELATIVE_PATH = Path("eng") / "tools" / "verify_docs_snippets.py"

CORE_EXAMPLE_MARKER = "FunnySharp examples passed."
ASPNET_EXAMPLE_MARKER = "FunnySharp ASP.NET Core example endpoints mapped."

# Verdict-rule fragments mirrored from eng/Verify-Release.ps1 (lines ~904-927).
# They are the contract between this tool's parsing logic and the frozen
# verifier: MARKER_CONTRACT asserts each fragment is still present in the
# PowerShell source, and the compiled patterns below are built from the same
# constants so the guard and the parser cannot drift apart. Named groups keep
# PowerShell's ``(?<name>...)`` spelling; ``_python_fragment`` translates them
# at compile time.
BUILD_SUCCEEDED_FRAGMENT = r"Build succeeded\."
BUILD_WARNINGS_FRAGMENT = r"0 Warning\(s\)"
BUILD_ERRORS_FRAGMENT = r"0 Error\(s\)"
TEST_SUMMARY_FRAGMENT = r"Test run summary:\s*Passed!"
TEST_TOTAL_FRAGMENT = r"\btotal:\s*(?<total>\d+)"
TEST_FAILED_FRAGMENT = r"\bfailed:\s*0"
TEST_SUCCEEDED_FRAGMENT = r"\bsucceeded:\s*(?<succeeded>\d+)"
TEST_SKIPPED_FRAGMENT = r"\bskipped:\s*0"
TEST_ASSEMBLY_RESULT_FRAGMENT = r"\(net10\.0\|[^)]*\)\s+passed\s+\([^)]*\)\s*$"

# The PowerShell-free docs verifier's success line. Plan U3 requires this
# marker before the local docs step can pass; its exit status alone is not
# enough.
DOCS_VERDICT_FRAGMENT = (
    r"Verified \d+ C# documentation snippets across \d+ primary guides\."
)


def _python_fragment(fragment: str) -> str:
    """Translate PowerShell named-group syntax (``(?<name>``) to Python's spelling."""

    return fragment.replace("(?<", "(?P<")


BUILD_SUCCEEDED_PATTERN = re.compile(rf"(?im)^\s*{BUILD_SUCCEEDED_FRAGMENT}\s*$")
BUILD_WARNINGS_PATTERN = re.compile(rf"(?im)^\s*{BUILD_WARNINGS_FRAGMENT}\s*$")
BUILD_ERRORS_PATTERN = re.compile(rf"(?im)^\s*{BUILD_ERRORS_FRAGMENT}\s*$")
TEST_SUMMARY_PATTERN = re.compile(
    _python_fragment(
        r"(?is)"
        + TEST_SUMMARY_FRAGMENT
        + r".*?"
        + TEST_TOTAL_FRAGMENT
        + r".*?"
        + TEST_FAILED_FRAGMENT
        + r".*?"
        + TEST_SUCCEEDED_FRAGMENT
        + r".*?"
        + TEST_SKIPPED_FRAGMENT
    )
)
DOCS_VERDICT_PATTERN = re.compile(rf"(?m)^\s*{DOCS_VERDICT_FRAGMENT}\s*$")


@dataclass(frozen=True)
class MarkerRequirement:
    """One literal the frozen release verifier must still contain."""

    description: str
    fragment: str


# Fragments copied from eng/Verify-Release.ps1's verdict block. They are the
# contract between this tool's parsing logic and the frozen verifier: if any is
# missing, the Python verdict rules may no longer match the release gate.
MARKER_CONTRACT: tuple[MarkerRequirement, ...] = (
    MarkerRequirement("build success line", BUILD_SUCCEEDED_FRAGMENT),
    MarkerRequirement("build zero-warning line", BUILD_WARNINGS_FRAGMENT),
    MarkerRequirement("build zero-error line", BUILD_ERRORS_FRAGMENT),
    MarkerRequirement("test summary prefix", TEST_SUMMARY_FRAGMENT),
    MarkerRequirement("test total field", TEST_TOTAL_FRAGMENT),
    MarkerRequirement("test zero-failed field", TEST_FAILED_FRAGMENT),
    MarkerRequirement("test succeeded field", TEST_SUCCEEDED_FRAGMENT),
    MarkerRequirement("test zero-skipped field", TEST_SKIPPED_FRAGMENT),
    MarkerRequirement("test assembly result shape", TEST_ASSEMBLY_RESULT_FRAGMENT),
    MarkerRequirement("core example success line", CORE_EXAMPLE_MARKER),
    MarkerRequirement("ASP.NET Core example success line", ASPNET_EXAMPLE_MARKER),
    MarkerRequirement("core test assembly path", TEST_ASSEMBLY_RELATIVE_PATHS[0]),
    MarkerRequirement("ASP.NET Core test assembly path", TEST_ASSEMBLY_RELATIVE_PATHS[1]),
)


@dataclass(frozen=True)
class EnvironmentProblem:
    """A missing prerequisite or unusable environment with remediation."""

    summary: str
    remediation: str


@dataclass
class StepResult:
    """The outcome of one local step."""

    name: str
    status: str  # passed | failed | skipped
    exit_code: int | None = None
    message: str = ""
    output_tail: tuple[str, ...] = ()

    def as_dict(self) -> dict[str, object]:
        payload: dict[str, object] = {
            "name": self.name,
            "status": self.status,
            "exitCode": self.exit_code,
            "message": self.message,
        }
        if self.output_tail:
            payload["outputTail"] = list(self.output_tail)
        return payload


CommandRunner = Callable[
    [Sequence[str], Mapping[str, str], Path], subprocess.CompletedProcess[str]
]


# ---------------------------------------------------------------------------
# Repository and environment resolution
# ---------------------------------------------------------------------------


def find_executable(name: str, env: Mapping[str, str]) -> str | None:
    path = env.get("PATH", env.get("Path", ""))
    return shutil.which(name, path=path or None)


def environment_problems(
    env: Mapping[str, str], python_version: Sequence[int] | None = None
) -> list[EnvironmentProblem]:
    """Check the prerequisites this pre-check can verify without running dotnet."""

    version = sys.version_info if python_version is None else python_version
    problems: list[EnvironmentProblem] = []
    required = f"{REQUIRED_PYTHON[0]}.{REQUIRED_PYTHON[1]}"
    if (int(version[0]), int(version[1])) != REQUIRED_PYTHON:
        problems.append(
            EnvironmentProblem(
                f"Python {required} is required; this interpreter is {version[0]}.{version[1]}.",
                "run this tool through uv (`uv run --no-project eng/tools/verify_local.py`); "
                "the pinned interpreter is recorded in .python-version.",
            )
        )
    if find_executable("uv", env) is None:
        problems.append(
            EnvironmentProblem(
                "uv was not found on PATH.",
                "install the uv version pinned by uv.toml and run "
                "`uv run --no-project eng/tools/verify_local.py`.",
            )
        )
    if find_executable("dotnet", env) is None:
        problems.append(
            EnvironmentProblem(
                "dotnet was not found on PATH.",
                "install the .NET SDK pinned by global.json "
                "and make sure `dotnet --version` works.",
            )
        )
    return problems


# ---------------------------------------------------------------------------
# Commands and verdicts
# ---------------------------------------------------------------------------


def command_for_step(step: str, repository_root: Path) -> list[str]:
    """Mirror the release protocol's command forms, minus release isolation flags."""

    if step == "restore":
        return ["dotnet", "restore", "FunnySharp.slnx", "--locked-mode"]
    if step == "build":
        return [
            "dotnet",
            "build",
            "FunnySharp.slnx",
            "--configuration",
            "Release",
            "--no-restore",
        ]
    if step == "test":
        return [
            "dotnet",
            "test",
            "FunnySharp.slnx",
            "--configuration",
            "Release",
            "--no-build",
            "--no-restore",
        ]
    if step == "examples":
        return [
            "dotnet",
            "run",
            "--project",
            "examples/FunnySharp.Examples/FunnySharp.Examples.csproj",
            "--configuration",
            "Release",
            "--no-build",
            "--no-restore",
        ]
    if step == "aspnetcore-examples":
        return [
            "dotnet",
            "run",
            "--project",
            "examples/FunnySharp.AspNetCore.Examples/FunnySharp.AspNetCore.Examples.csproj",
            "--configuration",
            "Release",
            "--no-build",
            "--no-restore",
            "--",
            "--verify",
        ]
    if step == "format":
        return [
            "dotnet",
            "format",
            "FunnySharp.slnx",
            "--verify-no-changes",
            "--no-restore",
        ]
    if step == "docs":
        return [
            sys.executable,
            str(repository_root / DOCS_VERIFIER_RELATIVE_PATH),
            "--repository-root",
            str(repository_root),
        ]
    raise AssertionError(f"unknown local step: {step}")


def run_command(
    argv: Sequence[str], env: Mapping[str, str], cwd: Path
) -> subprocess.CompletedProcess[str]:
    """Run one child process with captured output; never through a shell."""

    return subprocess.run(
        list(argv),
        cwd=str(cwd),
        env=dict(env),
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        encoding="utf-8",
        errors="replace",
        check=False,
        shell=False,
    )


def _test_assembly_pattern(assembly: Path) -> re.Pattern[str]:
    # Mirrors eng/Verify-Release.ps1's per-assembly success pattern.
    return re.compile(
        r"(?im)^\s*"
        + re.escape(str(assembly))
        + r"\s+"
        + TEST_ASSEMBLY_RESULT_FRAGMENT
    )


def _test_failure(text: str, repository_root: Path) -> str | None:
    summary = TEST_SUMMARY_PATTERN.search(text)
    if summary is None:
        return (
            "test log does not contain a 'Test run summary: Passed!' shape "
            "with total, failed: 0, succeeded, and skipped: 0"
        )
    total = int(summary.group("total"))
    succeeded = int(summary.group("succeeded"))
    if total <= 0 or total != succeeded:
        return (
            "test summary must report a positive total equal to succeeded "
            f"(total: {total}, succeeded: {succeeded})"
        )
    for relative in TEST_ASSEMBLY_RELATIVE_PATHS:
        assembly = repository_root / relative
        if _test_assembly_pattern(assembly).search(text) is None:
            return f"test log does not contain a passed result line for {assembly}"
    return None


# Mirrors eng/Verify-Release.ps1's Remove-AnsiControlSequences, which strips
# ANSI CSI sequences from log text before the frozen verifier matches it.
# GitHub Actions sets CI=true, and the MTP terminal reporter then colors the
# result words, so the child output must be normalized before verdict parsing.
ANSI_ESCAPE_PATTERN = re.compile(r"\x1b\[[0-?]*[ -/]*[@-~]")


def _strip_ansi(text: str) -> str:
    """Remove ANSI CSI sequences so verdict matching sees plain text."""

    return ANSI_ESCAPE_PATTERN.sub("", text)


def step_failure(
    step: str, stdout: str, stderr: str, repository_root: Path
) -> str | None:
    """Return the release-equivalent verdict failure for a step, or None.

    Restore and format are judged by exit code alone; the release verifier
    requires exit code 0 for every command and adds no output shape of its own
    for those steps. The docs step is local-only and additionally must print
    the Python verifier's success line.
    """

    combined = ""
    if step in {"build", "test", "examples", "aspnetcore-examples"}:
        combined = f"{stdout}\n{stderr}"
    if step == "docs":
        if DOCS_VERDICT_PATTERN.search(stdout) is None:
            return (
                "docs output does not contain a 'Verified N C# documentation "
                "snippets across N primary guides.' line"
            )
        return None
    if step == "build":
        if BUILD_SUCCEEDED_PATTERN.search(combined) is None:
            return "build log does not contain a 'Build succeeded.' line"
        if BUILD_WARNINGS_PATTERN.search(combined) is None:
            return "build log does not contain a '0 Warning(s)' line"
        if BUILD_ERRORS_PATTERN.search(combined) is None:
            return "build log does not contain a '0 Error(s)' line"
        return None
    if step == "test":
        return _test_failure(combined, repository_root)
    if step == "examples":
        # The frozen verifier uses PowerShell -notmatch, which is case-insensitive.
        if CORE_EXAMPLE_MARKER.lower() not in combined.lower():
            return f"examples output does not contain '{CORE_EXAMPLE_MARKER}'"
        return None
    if step == "aspnetcore-examples":
        if ASPNET_EXAMPLE_MARKER.lower() not in combined.lower():
            return (
                "ASP.NET Core examples output does not contain "
                f"'{ASPNET_EXAMPLE_MARKER}'"
            )
        return None
    return None


OUTPUT_TAIL_LINES = 20


def _output_tail(text: str, limit: int = OUTPUT_TAIL_LINES) -> list[str]:
    """Return the bounded last-N lines of child output.

    Mirrors ``inventory._tail``'s last-N bound without a log file.
    """

    return text.strip("\n").splitlines()[-limit:]


def _print_output_tail(step: str, tail: Sequence[str]) -> None:
    """Print a failed step's bounded output tail for diagnosis without a rerun."""

    print(f"--- {step} output (last {OUTPUT_TAIL_LINES} lines) ---", file=sys.stderr)
    for line in tail:
        print(line, file=sys.stderr)


def run_steps(
    repository_root: Path,
    env: Mapping[str, str],
    runner: CommandRunner,
    skipped: set[str],
) -> tuple[list[StepResult], str | None]:
    """Run the local steps in order and stop at the first failed check."""

    results: list[StepResult] = []
    for step in LOCAL_STEPS:
        if step in skipped:
            results.append(
                StepResult(step, "skipped", None, f"requested by --skip-{step}")
            )
            continue
        argv = command_for_step(step, repository_root)
        completed = runner(argv, env, repository_root)
        stdout = _strip_ansi(completed.stdout or "")
        stderr = _strip_ansi(completed.stderr or "")
        failure = (
            f"exit code {completed.returncode}"
            if completed.returncode != 0
            else step_failure(step, stdout, stderr, repository_root)
        )
        if failure is not None:
            tail = _output_tail(f"{stdout}\n{stderr}")
            _print_output_tail(step, tail)
            results.append(
                StepResult(
                    step,
                    "failed",
                    completed.returncode,
                    failure,
                    output_tail=tuple(tail),
                )
            )
            return results, step
        results.append(StepResult(step, "passed", completed.returncode))
    return results, None


# ---------------------------------------------------------------------------
# Marker contract
# ---------------------------------------------------------------------------


def missing_marker_fragments(verifier_path: Path) -> list[str]:
    """Return the marker-contract descriptions missing from the verifier source."""

    text = verifier_path.read_text(encoding="utf-8", errors="replace")
    return [
        requirement.description
        for requirement in MARKER_CONTRACT
        if requirement.fragment not in text
    ]


# ---------------------------------------------------------------------------
# Reporting
# ---------------------------------------------------------------------------


def _skipped_steps(args: argparse.Namespace) -> set[str]:
    skipped: set[str] = set()
    if args.skip_docs:
        skipped.add("docs")
    if args.skip_format:
        skipped.add("format")
    return skipped


def make_report(
    args: argparse.Namespace,
    repository_root: Path,
    *,
    status: str,
    exit_code: int,
    steps: Sequence[StepResult] = (),
    failed_step: str | None = None,
    message: str = "",
) -> dict[str, object]:
    return {
        "tool": "verify_local.py",
        "status": status,
        "exitCode": exit_code,
        "releaseEvidence": False,
        "preCheck": True,
        "repositoryRoot": str(repository_root),
        "offline": bool(args.offline),
        "skipped": [step for step in LOCAL_STEPS if step in _skipped_steps(args)],
        "steps": [result.as_dict() for result in steps],
        "failedStep": failed_step,
        "notRun": list(NOT_RUN_STEPS),
        "message": message,
    }


def human_summary(report: Mapping[str, object]) -> str:
    lines = [
        "FunnySharp local pre-check (not release evidence).",
        f"Repository: {report['repositoryRoot']}",
        f"Offline: {'yes' if report['offline'] else 'no'}",
        "",
    ]
    for step in report["steps"]:  # type: ignore[union-attr]
        status = step["status"]
        if status == "passed":
            lines.append(f"PASS {step['name']}")
        elif status == "skipped":
            lines.append(f"SKIP {step['name']} ({step['message']})")
        else:
            lines.append(f"FAIL {step['name']}: {step['message']}")
    if report["status"] == "environment-failure":
        lines.append(f"ENVIRONMENT FAILURE: {report['message']}")
    elif report["status"] == "failed":
        if report["failedStep"]:
            lines.append(
                f"Local pre-check FAILED at '{report['failedStep']}': {report['message']}"
            )
        else:
            lines.append(f"Local pre-check FAILED: {report['message']}")
    else:
        lines.append("Local pre-check passed.")
    lines.append("")
    lines.append(
        "NOT RUN (release protocol steps outside local scope): "
        + ", ".join(report["notRun"])  # type: ignore[arg-type]
    )
    lines.append(
        "This is a local pre-check, not release evidence; run the PowerShell "
        "release protocol for the release gate."
    )
    return "\n".join(lines)


def emit(args: argparse.Namespace, report: dict[str, object]) -> int:
    if args.json:
        print(json.dumps(report, indent=2))
        if report["status"] != "passed":
            print(human_summary(report), file=sys.stderr)
    else:
        print(human_summary(report))
    return int(report["exitCode"])  # type: ignore[arg-type]


def _environment_failure(
    args: argparse.Namespace,
    repository_root: Path,
    problems: Sequence[EnvironmentProblem],
) -> int:
    for problem in problems:
        print(f"ERROR: {problem.summary}", file=sys.stderr)
        print(f"  Remediation: {problem.remediation}", file=sys.stderr)
    message = "; ".join(problem.summary for problem in problems)
    report = make_report(
        args, repository_root, status="environment-failure", exit_code=2, message=message
    )
    if args.json:
        print(json.dumps(report, indent=2))
        return 2
    print(human_summary(report))
    return 2


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="verify_local.py",
        description=(
            "Run the core FunnySharp local checks with release-equivalent verdicts. "
            "This is a local pre-check, not release evidence."
        ),
    )
    parser.add_argument(
        "--repository-root",
        type=Path,
        default=None,
        metavar="PATH",
        help=(
            "repository root (default: resolved from the script location; that "
            "location must be inside a git repository)."
        ),
    )
    parser.add_argument(
        "--offline",
        action="store_true",
        help=(
            "export UV_OFFLINE=1 to child processes and never attempt "
            "network-dependent setup (dotnet restore/build may still contact "
            "configured feeds on first use)."
        ),
    )
    parser.add_argument(
        "--json",
        action="store_true",
        help="print a machine-readable summary on stdout.",
    )
    parser.add_argument(
        "--skip-docs",
        action="store_true",
        help="skip the documentation-snippet step.",
    )
    parser.add_argument(
        "--skip-format",
        action="store_true",
        help="skip the formatter step.",
    )
    return parser


def main(
    argv: Sequence[str] | None = None,
    env: Mapping[str, str] | None = None,
    runner: CommandRunner | None = None,
) -> int:
    args_list = list(sys.argv[1:]) if argv is None else list(argv)
    base_env = dict(os.environ) if env is None else dict(env)
    command_runner = run_command if runner is None else runner

    parser = build_parser()
    args = parser.parse_args(args_list)
    skipped = _skipped_steps(args)

    if args.repository_root is None:
        repository_root = default_repository_root(Path(__file__))
        if find_git_root(repository_root) is None:
            return _environment_failure(
                args,
                repository_root,
                [
                    EnvironmentProblem(
                        f"'{repository_root}' is not inside a git repository, so the "
                        "repository root cannot be derived from the script location.",
                        "run this tool from a FunnySharp checkout or pass "
                        "--repository-root <path>.",
                    )
                ],
            )
    else:
        repository_root = Path(args.repository_root).resolve()

    problems = environment_problems(base_env)
    if problems:
        return _environment_failure(args, repository_root, problems)

    verifier_path = repository_root / VERIFIER_RELATIVE_PATH
    if not verifier_path.is_file():
        return _environment_failure(
            args,
            repository_root,
            [
                EnvironmentProblem(
                    f"the frozen release verifier '{verifier_path}' was not found.",
                    "restore the file or pass --repository-root pointing at a "
                    "FunnySharp checkout.",
                )
            ],
        )

    missing = missing_marker_fragments(verifier_path)
    if missing:
        report = make_report(
            args,
            repository_root,
            status="failed",
            exit_code=1,
            message=(
                "the release verifier's marker contract changed, so the local verdict "
                f"rules can no longer be trusted (missing: {', '.join(missing)})."
            ),
        )
        return emit(args, report)

    if "docs" not in skipped:
        docs_verifier = repository_root / DOCS_VERIFIER_RELATIVE_PATH
        if not docs_verifier.is_file():
            return _environment_failure(
                args,
                repository_root,
                [
                    EnvironmentProblem(
                        f"the documentation-snippet verifier '{docs_verifier}' was not found.",
                        "restore the file or pass --skip-docs to run the other local "
                        "checks.",
                    )
                ],
            )

    child_env = dict(base_env)
    # The frozen verifier's markers and the dotnet test summary are English.
    # Force the invariant UI language so a localized developer machine parses
    # exactly like CI instead of failing the verdict rules on translated output.
    child_env["DOTNET_CLI_UI_LANGUAGE"] = "en"
    if args.offline:
        child_env["UV_OFFLINE"] = "1"

    results, failed_step = run_steps(repository_root, child_env, command_runner, skipped)
    if failed_step is None:
        report = make_report(
            args,
            repository_root,
            status="passed",
            exit_code=0,
            steps=results,
            message="Local pre-check passed.",
        )
    else:
        report = make_report(
            args,
            repository_root,
            status="failed",
            exit_code=1,
            steps=results,
            failed_step=failed_step,
            message=results[-1].message,
        )
    return emit(args, report)


if __name__ == "__main__":
    raise SystemExit(main())
