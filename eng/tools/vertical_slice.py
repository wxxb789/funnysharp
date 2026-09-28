#!/usr/bin/env -S uv run --no-project
# /// script
# requires-python = ">=3.12,<3.13"
# dependencies = []
# ///
"""Package-consumer verification for the ASP.NET Core vertical slice (Goal 22 evidence).

Packs the solution, then restores, builds, and tests the vertical-slice bundle against the
packed ``.nupkg`` files only: the consumer projects never reference the source projects, so a
green run here is evidence about the produced packages and the public API.

Every step runs with an isolated ``NUGET_PACKAGES`` directory and the local feed listed before
any remote feed, so a version drift cannot silently resolve to something else. The tool writes
``vertical-slice-results.json`` next to its output and exits 1 on any failed step.

Usage::

    uv run --no-project eng/tools/vertical_slice.py [--output DIR] [--feed DIR] [--no-pack]
        [--skip-tests] [--skip-measurements] [--json] [--repository-root PATH]

Exit codes: 0 pass, 1 check failure, 2 environment or usage failure.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from _repo import default_repository_root, find_git_root  # noqa: E402

PUBLIC_FEED = "https://api.nuget.org/v3/index.json"

# One dotnet step may not run forever: a hung restore/build/test must fail the receipt, not the tool.
STEP_TIMEOUT_SECONDS = 1800
STEP_TIMEOUT_RETURNCODE = 124

SLICE_ROOT = Path("tests/FunnySharp.VerticalSlice")
API_PROJECT = SLICE_ROOT / "FunnySharp.VerticalSlice.Api/FunnySharp.VerticalSlice.Api.csproj"
TEST_PROJECT = SLICE_ROOT / "FunnySharp.VerticalSlice.Tests/FunnySharp.VerticalSlice.Tests.csproj"
BASELINE_PROJECT = SLICE_ROOT / "FunnySharp.VerticalSlice.Baseline/FunnySharp.VerticalSlice.Baseline.csproj"
MEASUREMENTS_PROJECT = SLICE_ROOT / "FunnySharp.VerticalSlice.Measurements/FunnySharp.VerticalSlice.Measurements.csproj"

VERIFY_MARKER = "FunnySharp ASP.NET Core vertical slice endpoints mapped."
BASELINE_MARKER = "Baseline (idiomatic C#) vertical slice endpoints mapped."
MEASUREMENTS_MARKER = "FunnySharp vertical slice measurements ready."

SUMMARY_MARKER = re.compile(r"(?:Test run summary|测试运行摘要)")

SUMMARY_FIELDS = {
    # Microsoft.Testing.Platform prints "Test run summary: Passed!" followed by lowercase
    # "total: / failed: / succeeded: / skipped:" fields (localized on non-English hosts), which is the
    # same shape eng/tools/verify_local.py already parses. Capitalized variants are accepted too.
    "total": re.compile(r"(?:Total|总计)\s*[:\uff1a]\s*(\d+)", re.IGNORECASE),
    "failed": re.compile(r"(?:Failed|失败)\s*[:\uff1a]\s*(\d+)", re.IGNORECASE),
    "passed": re.compile(r"(?:Passed|Succeeded|成功)\s*[:\uff1a]\s*(\d+)", re.IGNORECASE),
    "skipped": re.compile(r"(?:Skipped|已跳过)\s*[:\uff1a]\s*(\d+)", re.IGNORECASE),
}


def dotnet() -> str:
    """Returns the dotnet entry point, preferring an explicit DOTNET_ROOT installation."""
    found = shutil.which("dotnet")
    if found:
        return found
    root = os.environ.get("DOTNET_ROOT")
    if root:
        candidate = Path(root) / "dotnet"
        if candidate.exists():
            return str(candidate)
    user_install = Path.home() / ".dotnet" / "dotnet"
    if user_install.exists():
        return str(user_install)
    print("error: dotnet was not found on PATH or under DOTNET_ROOT.", file=sys.stderr)
    raise SystemExit(2)


def run(command: list[str], cwd: Path, env: dict[str, str]) -> subprocess.CompletedProcess[str]:
    """Runs one dotnet step; a hung step becomes a failed step rather than a hung tool."""
    try:
        return subprocess.run(
            command, cwd=cwd, env=env, capture_output=True, text=True, check=False, timeout=STEP_TIMEOUT_SECONDS
        )
    except subprocess.TimeoutExpired as expired:
        return subprocess.CompletedProcess(
            command,
            STEP_TIMEOUT_RETURNCODE,
            stdout=expired.stdout if isinstance(expired.stdout, str) else "",
            stderr=f"step timed out after {STEP_TIMEOUT_SECONDS}s",
        )


PACKAGE_IDS = ("FunnySharp", "FunnySharp.AspNetCore")


def package_versions(feed: Path) -> dict[str, str]:
    """Reads the version of the single package each id has in the feed.

    The feed is an output directory that gets reused, so a leftover package from an earlier run must
    never be mistaken for the one just produced: more than one version for an id fails closed instead of
    silently picking either of them (a lexicographic pick would choose 0.9.0 over 0.10.0).
    """
    found: dict[str, list[str]] = {}
    for package in sorted(feed.glob("*.nupkg")):
        for package_id in sorted(PACKAGE_IDS, key=len, reverse=True):
            prefix = f"{package_id}."
            if not package.name.startswith(prefix):
                continue
            version = package.name[len(prefix) : -len(".nupkg")]
            if not re.fullmatch(r"\d+\.\d+\.\d+[A-Za-z0-9.\-+]*", version):
                continue
            found.setdefault(package_id, []).append(version)
            break

    versions: dict[str, str] = {}
    for package_id in PACKAGE_IDS:
        candidates = found.get(package_id, [])
        if not candidates:
            print(f"error: {feed} is missing a package for: {package_id}.", file=sys.stderr)
            raise SystemExit(1)
        if len(candidates) > 1:
            print(
                f"error: {feed} holds more than one {package_id} version ({', '.join(sorted(candidates))}); "
                "a feed must describe one run's output -- let the tool pack (which clears the feed "
                "first), or pass --no-pack with a feed that holds a single version per package.",
                file=sys.stderr,
            )
            raise SystemExit(1)
        versions[package_id] = candidates[0]
    return versions


def fingerprint(feed: Path) -> list[dict[str, str]]:
    rows = []
    for package in sorted(feed.glob("*.nupkg")):
        digest = hashlib.sha256(package.read_bytes()).hexdigest()
        rows.append({"file": package.name, "sha256": digest})
    return rows


def parse_summary(output: str) -> dict[str, int] | None:
    match = None
    for match in SUMMARY_MARKER.finditer(output):
        pass
    if match is None:
        return None
    tail = output[match.end() :]
    counts: dict[str, int] = {}
    for name, pattern in SUMMARY_FIELDS.items():
        found = pattern.search(tail)
        if found is None:
            return None
        counts[name] = int(found.group(1))
    return counts


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Vertical-slice package-consumer verification.")
    parser.add_argument("--output", default="artifacts/vertical-slice/consumer-run")
    parser.add_argument("--feed", default=None, help="package feed directory (defaults to <output>/feed)")
    parser.add_argument("--no-pack", action="store_true", help="reuse an existing feed instead of packing")
    parser.add_argument("--skip-tests", action="store_true", help="build the consumer bundle but do not run its tests")
    parser.add_argument(
        "--skip-measurements",
        action="store_true",
        help="build the comparison projects but do not run the measurement and equivalence harness",
    )
    parser.add_argument("--json", action="store_true", help="print the receipt as JSON on stdout")
    parser.add_argument("--repository-root", default=None)
    args = parser.parse_args(argv)

    if args.repository_root:
        root = Path(args.repository_root).resolve()
    else:
        root = default_repository_root(Path(__file__))
        if find_git_root(root) is None:
            print(f"error: {root} is not inside a git repository.", file=sys.stderr)
            return 2
    output = (root / args.output).resolve()
    feed = (root / args.feed).resolve() if args.feed else output / "feed"
    packages = output / "nuget-packages"
    feed.mkdir(parents=True, exist_ok=True)
    packages.mkdir(parents=True, exist_ok=True)

    dotnet_path = dotnet()
    env = dict(os.environ)
    env.update(
        {
            "DOTNET_CLI_TELEMETRY_OPTOUT": "1",
            "DOTNET_NOLOGO": "1",
            "DOTNET_SKIP_FIRST_TIME_EXPERIENCE": "1",
            "NUGET_PACKAGES": str(packages),
        }
    )

    receipt: dict[str, object] = {
        "schemaVersion": 1,
        "objective": "docs/goals/0022-goal.md",
        "configuration": "Release",
        "output": str(output),
        "feed": str(feed),
        "nugetPackages": str(packages),
        "steps": [],
    }
    failures: list[str] = []

    def record(step: str, command: list[str], result: subprocess.CompletedProcess[str]) -> None:
        entry = {
            "step": step,
            "command": command,
            "exitCode": result.returncode,
            "stdoutTail": result.stdout[-8000:],
            "stderrTail": result.stderr[-4000:],
        }
        cast = receipt["steps"]
        assert isinstance(cast, list)
        cast.append(entry)
        if result.returncode != 0:
            failures.append(f"{step} exited {result.returncode}")

    if not args.no_pack:
        # The feed is an output directory that may be reused across runs; clearing it keeps the
        # receipt about the packages this run produced.
        for stale in feed.glob("*.nupkg"):
            stale.unlink()
        command = [dotnet_path, "pack", "FunnySharp.slnx", "-c", "Release", "-o", str(feed)]
        result = run(command, root, env)
        record("pack", command, result)
        if result.returncode != 0:
            print(result.stdout[-4000:], file=sys.stderr)
            print(result.stderr[-4000:], file=sys.stderr)

    versions = package_versions(feed)
    receipt["packages"] = fingerprint(feed)
    receipt["packageVersions"] = versions
    version_args = [
        f"-p:FunnySharpPackageVersion={versions['FunnySharp']}",
        f"-p:FunnySharpAspNetCorePackageVersion={versions['FunnySharp.AspNetCore']}",
    ]

    projects = [
        ("api", API_PROJECT),
        ("tests", TEST_PROJECT),
        ("baseline", BASELINE_PROJECT),
        ("measurements", MEASUREMENTS_PROJECT),
    ]
    for name, project in projects:
        restore = [
            dotnet_path, "restore", str(project), "--source", str(feed), "--source", PUBLIC_FEED, *version_args,
        ]
        record(f"restore-{name}", restore, run(restore, root, env))

    for name, project in projects:
        build = [dotnet_path, "build", str(project), "-c", "Release", "--no-restore", *version_args]
        record(f"build-{name}", build, run(build, root, env))

    verify = [
        dotnet_path, "run", "--project", str(API_PROJECT), "-c", "Release", "--no-build", "--no-restore",
        *version_args, "--", "--verify",
    ]
    verify_result = run(verify, root, env)
    record("api-verify", verify, verify_result)
    receipt["verifyMarker"] = VERIFY_MARKER in verify_result.stdout
    if VERIFY_MARKER not in verify_result.stdout:
        failures.append("api-verify did not print its success marker")

    baseline_verify = [
        dotnet_path, "run", "--project", str(BASELINE_PROJECT), "-c", "Release", "--no-build", "--no-restore",
        "--", "--verify",
    ]
    baseline_result = run(baseline_verify, root, env)
    record("baseline-verify", baseline_verify, baseline_result)
    receipt["baselineVerifyMarker"] = BASELINE_MARKER in baseline_result.stdout
    if BASELINE_MARKER not in baseline_result.stdout:
        failures.append("baseline-verify did not print its success marker")

    measurements_verify = [
        dotnet_path, "run", "--project", str(MEASUREMENTS_PROJECT), "-c", "Release", "--no-build", "--no-restore",
        *version_args, "--", "--verify",
    ]
    measurements_result = run(measurements_verify, root, env)
    record("measurements-verify", measurements_verify, measurements_result)
    receipt["measurementsVerifyMarker"] = MEASUREMENTS_MARKER in measurements_result.stdout
    if MEASUREMENTS_MARKER not in measurements_result.stdout:
        failures.append("measurements-verify did not print its success marker")

    if not args.skip_measurements:
        measurements = [
            dotnet_path, "run", "--project", str(MEASUREMENTS_PROJECT), "-c", "Release", "--no-build", "--no-restore",
            *version_args, "--", "--output", str(output / "measurements.json"),
        ]
        measurements_result = run(measurements, root, env)
        record("measurements", measurements, measurements_result)
        if measurements_result.returncode != 0:
            failures.append("measurements reported a behavioral difference between the slice and the comparison app")
        else:
            receipt["measurementsPath"] = str(output / "measurements.json")

    if not args.skip_tests:
        test = [dotnet_path, "test", str(TEST_PROJECT), "-c", "Release", "--no-build", *version_args]
        test_result = run(test, root, env)
        record("consumer-tests", test, test_result)
        summary = parse_summary(test_result.stdout + test_result.stderr)
        receipt["testSummary"] = summary
        if summary is None:
            failures.append("consumer-tests produced no parsable test summary")
        else:
            if summary["failed"] != 0:
                failures.append(f"consumer-tests reported {summary['failed']} failures")
            if summary["skipped"] != 0:
                failures.append(f"consumer-tests reported {summary['skipped']} skipped tests")
            if summary["passed"] == 0:
                failures.append("consumer-tests ran no passing test")

    receipt["status"] = "fail" if failures else "pass"
    receipt["failures"] = failures
    receipt_path = output / "vertical-slice-results.json"
    receipt_path.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")

    if args.json:
        print(json.dumps(receipt, indent=2))
    else:
        print(f"packages: {versions}")
        print(f"receipt:  {receipt_path}")
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        print(f"vertical slice consumer verification: {'FAIL' if failures else 'PASS'}")

    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
