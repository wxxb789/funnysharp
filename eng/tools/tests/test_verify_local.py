"""Structural and behavioral tests for eng/tools/verify_local.py.

The suite is stdlib-only and never runs the real .NET pipeline. Two fakes stand
in for ``dotnet``:

* ``FakeRunner`` is injected through ``verify_local.main`` and returns canned
  fixture logs so every verdict arm can be exercised deterministically;
* on POSIX, ``FakeDotnetExecutableTests`` puts a small executable ``dotnet``
  shim on PATH and runs the real ``subprocess`` code path, proving command
  dispatch, output capture, exit-code propagation, and environment forwarding
  (including ``UV_OFFLINE``) without a real SDK.

Every fixture log mirrors the shapes the frozen ``eng/Verify-Release.ps1``
verifier accepts.
"""

from __future__ import annotations

import io
import json
import os
import stat
import subprocess
import sys
import tempfile
import unittest
from contextlib import redirect_stderr, redirect_stdout
from dataclasses import dataclass
from pathlib import Path
from unittest import mock

TOOLS_DIR = Path(__file__).resolve().parents[1]
REPOSITORY_ROOT = Path(__file__).resolve().parents[3]

sys.path.insert(0, str(TOOLS_DIR))
import verify_local  # noqa: E402  (sys.path is set above)

_UNSET = object()


def green_stdout(step: str, repository_root: Path) -> str:
    """Canned successful output for one step, shaped like the real tools' output."""

    if step == "restore":
        return (
            "  Determining projects to restore...\n"
            "  Restored FunnySharp.slnx (in 1.23 sec).\n"
        )
    if step == "build":
        return (
            "  FunnySharp -> src/FunnySharp/bin/Release/net10.0/FunnySharp.dll\n"
            "Build succeeded.\n"
            "    0 Warning(s)\n"
            "    0 Error(s)\n"
            "\n"
            "Time Elapsed 00:00:04.00\n"
        )
    if step == "test":
        lines = [
            f"  {repository_root / relative} (net10.0|net10.0) passed (1.2s)"
            for relative in verify_local.TEST_ASSEMBLY_RELATIVE_PATHS
        ]
        lines += [
            "",
            "Test run summary: Passed!",
            "  total: 441",
            "  failed: 0",
            "  succeeded: 441",
            "  skipped: 0",
            "  duration: 00:00:12.3456789",
        ]
        return "\n".join(lines) + "\n"
    if step == "examples":
        return "FunnySharp examples passed.\n"
    if step == "aspnetcore-examples":
        return "FunnySharp ASP.NET Core example endpoints mapped.\n"
    if step == "format":
        return ""
    if step == "docs":
        return "Verified 25 C# documentation snippets across 8 primary guides.\n"
    raise AssertionError(f"unexpected step {step}")


def without_assembly_line(log: str, repository_root: Path, index: int) -> str:
    assembly = str(repository_root / verify_local.TEST_ASSEMBLY_RELATIVE_PATHS[index])
    return "\n".join(line for line in log.splitlines() if assembly not in line) + "\n"


@dataclass
class FakeRun:
    returncode: int
    stdout: str
    stderr: str = ""


def classify_argv(argv: list[str], repository_root: Path) -> str:
    if any(str(item).endswith("verify_docs_snippets.py") for item in argv):
        return "docs"
    if argv and Path(argv[0]).name in {"dotnet", "dotnet.exe", "dotnet.cmd"}:
        if len(argv) > 1:
            if argv[1] == "run":
                if "--project" in argv:
                    project = argv[argv.index("--project") + 1]
                    return "aspnetcore-examples" if "AspNetCore" in project else "examples"
            elif argv[1] in {"restore", "build", "test", "format"}:
                return argv[1]
    raise AssertionError(f"unexpected command: {argv!r}")


class FakeRunner:
    """Injected command runner returning canned logs and recording calls."""

    def __init__(self, repository_root: Path) -> None:
        self.repository_root = repository_root
        self.calls: list[tuple[list[str], dict[str, str], Path]] = []
        self.overrides: dict[str, FakeRun] = {}
        self.record_path: Path | None = None

    def override(
        self,
        step: str,
        *,
        exit_code: int = 0,
        stdout: str | None = None,
        stderr: str = "",
    ) -> None:
        if stdout is None:
            stdout = green_stdout(step, self.repository_root)
        self.overrides[step] = FakeRun(exit_code, stdout, stderr)

    def __call__(self, argv, env, cwd):
        argv = [str(item) for item in argv]
        env = dict(env)
        cwd = Path(cwd)
        step = classify_argv(argv, self.repository_root)
        self.calls.append((argv, env, cwd))
        if self.record_path is not None:
            with self.record_path.open("a", encoding="utf-8") as handle:
                handle.write(
                    json.dumps({"step": step, "argv": argv, "env": env}) + "\n"
                )
        run = self.overrides.get(step)
        if run is None:
            run = FakeRun(0, green_stdout(step, self.repository_root))
        return subprocess.CompletedProcess(argv, run.returncode, run.stdout, run.stderr)

    @property
    def steps(self) -> list[str]:
        return [classify_argv(argv, self.repository_root) for argv, _, _ in self.calls]


def install_stub(bin_dir: Path, name: str, content: str = "#!/bin/sh\nexit 0\n") -> None:
    """Place a PATH-visible stub for ``name`` (executable on POSIX, .cmd on Windows)."""

    bin_dir.mkdir(parents=True, exist_ok=True)
    if os.name == "nt":
        # shutil.which honors PATHEXT on Windows; the installed runner never
        # executes this file, so a .cmd shim is enough for presence checks.
        (bin_dir / f"{name}.cmd").write_text(
            "@echo off\r\nexit /b 0\r\n", encoding="utf-8", newline="\r\n"
        )
        return
    path = bin_dir / name
    path.write_text(content, encoding="utf-8", newline="\n")
    path.chmod(path.stat().st_mode | stat.S_IXUSR | stat.S_IXGRP | stat.S_IXOTH)


FAKE_DOTNET_SCRIPT = """#!/bin/sh
record="${FAKE_DOTNET_RECORD:-}"
if [ -n "$record" ]; then
    {
        printf 'argv:'
        for argument in "$@"; do
            printf ' %s' "$argument"
        done
        echo
        echo "UV_OFFLINE=${UV_OFFLINE:-<unset>}"
    } >> "$record"
fi
step=restore
case "$1" in
    restore|build|test|format) step="$1" ;;
    run)
        case "$*" in
            *AspNetCore*) step=aspnetcore-examples ;;
            *) step=examples ;;
        esac
        ;;
esac
if [ -f "$FAKE_DOTNET_BEHAVIOR/$step.stdout" ]; then
    cat "$FAKE_DOTNET_BEHAVIOR/$step.stdout"
fi
if [ -f "$FAKE_DOTNET_BEHAVIOR/$step.stderr" ]; then
    cat "$FAKE_DOTNET_BEHAVIOR/$step.stderr" >&2
fi
if [ -f "$FAKE_DOTNET_BEHAVIOR/$step.code" ]; then
    exit "$(cat "$FAKE_DOTNET_BEHAVIOR/$step.code")"
fi
exit 0
"""


class _VerifyLocalTestCase(unittest.TestCase):
    maxDiff = None

    def setUp(self) -> None:
        tmp = tempfile.TemporaryDirectory(prefix="verify-local-test-")
        self.addCleanup(tmp.cleanup)
        self.tmp = Path(tmp.name)
        self.repo = REPOSITORY_ROOT
        self.bin_dir = self.tmp / "bin"
        install_stub(self.bin_dir, "uv")
        install_stub(self.bin_dir, "dotnet")
        self.record = self.tmp / "child-record.jsonl"
        self.runner = FakeRunner(self.repo)
        real_path = os.environ.get("PATH", "")
        path_key = "Path" if os.name == "nt" else "PATH"
        self.env = {
            **os.environ,
            path_key: str(self.bin_dir)
            + (os.pathsep + real_path if real_path else ""),
        }

    def repo_argv(self, *flags: str) -> list[str]:
        return ["--repository-root", str(self.repo), *flags]

    def run_cli(self, argv, env=None, runner=_UNSET):
        effective_runner = self.runner if runner is _UNSET else runner
        effective_env = dict(self.env if env is None else env)
        stdout, stderr = io.StringIO(), io.StringIO()
        with redirect_stdout(stdout), redirect_stderr(stderr):
            code = verify_local.main(
                list(argv), env=effective_env, runner=effective_runner
            )
        return code, stdout.getvalue(), stderr.getvalue()

    def assert_step_fails(
        self,
        step: str,
        *,
        message_contains: str,
        stdout_override: str | None = None,
        exit_code: int = 0,
        expected_steps: list[str] | None = None,
    ) -> dict:
        self.runner.override(
            step, exit_code=exit_code, stdout=stdout_override
        )
        code, stdout, stderr = self.run_cli(self.repo_argv("--json"))
        self.assertEqual(1, code, stderr)
        report = json.loads(stdout)
        self.assertEqual("failed", report["status"])
        self.assertEqual(step, report["failedStep"])
        self.assertIn(message_contains, report["message"])
        self.assertEqual(step, report["steps"][-1]["name"])
        self.assertEqual("failed", report["steps"][-1]["status"])
        if expected_steps is not None:
            self.assertEqual(expected_steps, self.runner.steps)
        return report


# ---------------------------------------------------------------------------
# Pipeline verdicts
# ---------------------------------------------------------------------------


class PipelineVerdictTests(_VerifyLocalTestCase):
    def test_all_green_pipeline_passes(self) -> None:
        code, stdout, stderr = self.run_cli(self.repo_argv())
        self.assertEqual(0, code, stderr)
        for step in verify_local.LOCAL_STEPS:
            self.assertIn(f"PASS {step}", stdout)
        self.assertEqual(list(verify_local.LOCAL_STEPS), self.runner.steps)
        self.assertIn("local pre-check", stdout.lower())
        self.assertIn("not release evidence", stdout)
        self.assertEqual("", stderr)

    def test_build_warning_fails(self) -> None:
        log = green_stdout("build", self.repo).replace(
            "    0 Warning(s)", "    1 Warning(s)"
        )
        self.assert_step_fails(
            "build",
            message_contains="0 Warning",
            stdout_override=log,
            expected_steps=["restore", "build"],
        )

    def test_build_error_line_fails(self) -> None:
        log = green_stdout("build", self.repo).replace(
            "    0 Error(s)", "    2 Error(s)"
        )
        self.assert_step_fails("build", message_contains="0 Error", stdout_override=log)

    def test_build_missing_success_line_fails(self) -> None:
        log = green_stdout("build", self.repo).replace(
            "Build succeeded.", "Build FAILED."
        )
        self.assert_step_fails(
            "build", message_contains="Build succeeded", stdout_override=log
        )

    def test_restore_failure_stops_pipeline(self) -> None:
        self.assert_step_fails(
            "restore",
            message_contains="exit code 1",
            exit_code=1,
            expected_steps=["restore"],
        )

    def test_test_summary_zero_total_fails(self) -> None:
        log = green_stdout("test", self.repo)
        log = log.replace("  total: 441", "  total: 0").replace(
            "  succeeded: 441", "  succeeded: 0"
        )
        self.assert_step_fails(
            "test", message_contains="positive total", stdout_override=log
        )

    def test_test_summary_total_not_equal_succeeded_fails(self) -> None:
        log = green_stdout("test", self.repo).replace(
            "  succeeded: 441", "  succeeded: 440"
        )
        self.assert_step_fails(
            "test",
            message_contains="positive total equal to succeeded",
            stdout_override=log,
        )

    def test_test_summary_skipped_fails(self) -> None:
        log = green_stdout("test", self.repo).replace("  skipped: 0", "  skipped: 1")
        self.assert_step_fails(
            "test", message_contains="Test run summary: Passed!", stdout_override=log
        )

    def test_test_summary_missing_fails(self) -> None:
        self.assert_step_fails(
            "test",
            message_contains="Test run summary: Passed!",
            stdout_override="no summary here\n",
        )

    def test_test_missing_core_assembly_line_fails(self) -> None:
        log = without_assembly_line(green_stdout("test", self.repo), self.repo, 0)
        report = self.assert_step_fails(
            "test", message_contains="passed result line for", stdout_override=log
        )
        self.assertIn("FunnySharp.Tests.dll", report["message"])

    def test_test_missing_aspnet_assembly_line_fails(self) -> None:
        log = without_assembly_line(green_stdout("test", self.repo), self.repo, 1)
        report = self.assert_step_fails(
            "test", message_contains="passed result line for", stdout_override=log
        )
        self.assertIn("FunnySharp.AspNetCore.Tests.dll", report["message"])

    def test_examples_missing_success_line_fails(self) -> None:
        self.assert_step_fails(
            "examples",
            message_contains="FunnySharp examples passed.",
            stdout_override="nothing useful\n",
        )

    def test_aspnetcore_examples_missing_success_line_fails(self) -> None:
        self.assert_step_fails(
            "aspnetcore-examples",
            message_contains="FunnySharp ASP.NET Core example endpoints mapped.",
            stdout_override="nothing useful\n",
        )

    def test_formatter_failure_fails(self) -> None:
        self.assert_step_fails(
            "format",
            message_contains="exit code 2",
            exit_code=2,
            expected_steps=[
                "restore",
                "build",
                "test",
                "examples",
                "aspnetcore-examples",
                "format",
            ],
        )

    def test_docs_failure_fails(self) -> None:
        self.assert_step_fails(
            "docs", message_contains="exit code 1", exit_code=1
        )

    def test_stderr_output_is_used_for_verdicts(self) -> None:
        # The frozen verifier concatenates stdout and stderr; so does this tool.
        self.runner.override("examples", stdout="", stderr="FunnySharp examples passed.\n")
        code, stdout, stderr = self.run_cli(self.repo_argv())
        self.assertEqual(0, code, stderr)


# ---------------------------------------------------------------------------
# Protocol contract and marker guard
# ---------------------------------------------------------------------------


class ProtocolContractTests(_VerifyLocalTestCase):
    def test_pep723_header(self) -> None:
        text = (self.repo / "eng" / "tools" / "verify_local.py").read_text(
            encoding="utf-8"
        )
        self.assertTrue(text.splitlines()[0].startswith("#!"))
        self.assertIn("# /// script", text)
        self.assertIn('requires-python = ">=3.12,<3.13"', text)
        self.assertIn("dependencies = []", text)

    def test_commands_mirror_release_protocol_forms(self) -> None:
        protocol = json.loads(
            (self.repo / "eng" / "release-protocol.json").read_text(encoding="utf-8")
        )
        for name in ("build", "test", "examples", "aspnetcore-examples", "format"):
            self.assertEqual(
                ["dotnet", *protocol["steps"][name]["arguments"]],
                verify_local.command_for_step(name, self.repo),
                name,
            )
        restore = verify_local.command_for_step("restore", self.repo)
        self.assertEqual(
            ["dotnet", "restore", "FunnySharp.slnx", "--locked-mode"], restore
        )
        for isolation_flag in ("--no-cache", "--source"):
            self.assertNotIn(isolation_flag, restore)

    def test_docs_step_uses_same_interpreter(self) -> None:
        argv = verify_local.command_for_step("docs", self.repo)
        self.assertEqual(sys.executable, argv[0])
        self.assertEqual(
            str(self.repo / "eng" / "tools" / "verify_docs_snippets.py"), argv[1]
        )
        self.assertIn("--repository-root", argv)
        self.assertIn(str(self.repo), argv)

    def test_run_command_captures_output_without_a_shell(self) -> None:
        captured: dict[str, object] = {}

        def fake_run(argv, **kwargs):
            captured["argv"] = argv
            captured["kwargs"] = kwargs
            return subprocess.CompletedProcess(argv, 0, "", "")

        with mock.patch.object(verify_local.subprocess, "run", fake_run):
            verify_local.run_command(["dotnet", "--version"], {"PATH": ""}, self.repo)
        self.assertEqual(["dotnet", "--version"], captured["argv"])
        kwargs = captured["kwargs"]
        self.assertIs(False, kwargs["shell"])
        self.assertEqual(subprocess.PIPE, kwargs["stdout"])
        self.assertEqual(subprocess.PIPE, kwargs["stderr"])
        self.assertEqual(str(self.repo), kwargs["cwd"])
        self.assertEqual({"PATH": ""}, kwargs["env"])

    def test_not_run_steps_match_release_protocol(self) -> None:
        protocol = json.loads(
            (self.repo / "eng" / "release-protocol.json").read_text(encoding="utf-8")
        )
        full = protocol["modes"]["full"]["steps"]
        expected = [step for step in full if step not in verify_local.LOCAL_STEPS]
        self.assertEqual(list(verify_local.NOT_RUN_STEPS), expected)
        self.assertEqual(
            set(full),
            set(verify_local.NOT_RUN_STEPS)
            | {step for step in verify_local.LOCAL_STEPS if step in full},
        )
        self.assertEqual(
            len(verify_local.NOT_RUN_STEPS), len(set(verify_local.NOT_RUN_STEPS))
        )

    def test_not_run_summary_lists_every_out_of_scope_step(self) -> None:
        code, stdout, stderr = self.run_cli(self.repo_argv())
        self.assertEqual(0, code, stderr)
        summary_line = next(
            line for line in stdout.splitlines() if line.startswith("NOT RUN")
        )
        listed = summary_line.partition(": ")[2].split(", ")
        self.assertEqual(list(verify_local.NOT_RUN_STEPS), listed)

    def test_marker_contract_present_in_real_verifier(self) -> None:
        self.assertEqual(
            [],
            verify_local.missing_marker_fragments(
                self.repo / "eng" / "Verify-Release.ps1"
            ),
        )

    def test_marker_guard_failure_on_mutated_verifier(self) -> None:
        fixture = self.tmp / "mutated-repo"
        (fixture / "eng").mkdir(parents=True)
        source = (self.repo / "eng" / "Verify-Release.ps1").read_text(
            encoding="utf-8"
        )
        mutated = source.replace(r"0 Warning\(s\)", "0 Warnings", 1)
        self.assertNotEqual(source, mutated)
        (fixture / "eng" / "Verify-Release.ps1").write_text(
            mutated, encoding="utf-8", newline="\n"
        )
        code, stdout, stderr = self.run_cli(
            ["--repository-root", str(fixture), "--json"]
        )
        self.assertEqual(1, code)
        self.assertEqual([], self.runner.calls)
        report = json.loads(stdout)
        self.assertEqual("failed", report["status"])
        self.assertIn("build zero-warning line", report["message"])


# ---------------------------------------------------------------------------
# Environment failures
# ---------------------------------------------------------------------------


class EnvironmentFailureTests(_VerifyLocalTestCase):
    def test_missing_uv_is_environment_failure(self) -> None:
        bin_dir = self.tmp / "dotnet-only-bin"
        install_stub(bin_dir, "dotnet")
        code, stdout, stderr = self.run_cli(
            self.repo_argv(), env={**self.env, "PATH": str(bin_dir)}
        )
        self.assertEqual(2, code)
        self.assertIn("uv was not found", stderr)
        self.assertIn("Remediation", stderr)
        self.assertEqual([], self.runner.calls)

    def test_missing_dotnet_is_environment_failure(self) -> None:
        bin_dir = self.tmp / "uv-only-bin"
        install_stub(bin_dir, "uv")
        code, stdout, stderr = self.run_cli(
            self.repo_argv(), env={**self.env, "PATH": str(bin_dir)}
        )
        self.assertEqual(2, code)
        self.assertIn("dotnet was not found", stderr)
        self.assertIn("Remediation", stderr)
        self.assertEqual([], self.runner.calls)

    def test_wrong_python_version_is_environment_failure(self) -> None:
        problems = verify_local.environment_problems(
            dict(self.env), python_version=(3, 11, 0)
        )
        self.assertTrue(
            any("Python 3.12" in problem.summary for problem in problems)
        )
        with mock.patch.object(sys, "version_info", (3, 11, 0)):
            code, stdout, stderr = self.run_cli(self.repo_argv())
        self.assertEqual(2, code)
        self.assertIn("Python 3.12", stderr)
        self.assertEqual([], self.runner.calls)

    def test_outside_git_repository_is_environment_failure(self) -> None:
        not_a_repo = self.tmp / "not-a-repo"
        not_a_repo.mkdir()
        with mock.patch.object(
            verify_local, "default_repository_root", return_value=not_a_repo
        ):
            code, stdout, stderr = self.run_cli([])
        self.assertEqual(2, code)
        self.assertIn("not inside a git repository", stderr)
        self.assertIn("--repository-root", stderr)
        self.assertEqual([], self.runner.calls)

    def test_missing_verifier_is_environment_failure(self) -> None:
        fixture = self.tmp / "repo-without-verifier"
        fixture.mkdir()
        code, stdout, stderr = self.run_cli(["--repository-root", str(fixture)])
        self.assertEqual(2, code)
        self.assertIn("Verify-Release.ps1", stderr)
        self.assertEqual([], self.runner.calls)

    def test_missing_docs_verifier_is_environment_failure(self) -> None:
        fixture = self.tmp / "repo-without-docs-verifier"
        (fixture / "eng").mkdir(parents=True)
        source = self.repo / "eng" / "Verify-Release.ps1"
        (fixture / "eng" / "Verify-Release.ps1").write_text(
            source.read_text(encoding="utf-8"), encoding="utf-8", newline="\n"
        )
        code, stdout, stderr = self.run_cli(["--repository-root", str(fixture)])
        self.assertEqual(2, code)
        self.assertIn("documentation-snippet verifier", stderr)
        self.assertIn("--skip-docs", stderr)
        self.assertEqual([], self.runner.calls)


# ---------------------------------------------------------------------------
# Reporting, skips, and offline mode
# ---------------------------------------------------------------------------


class ReportingTests(_VerifyLocalTestCase):
    def test_json_summary_shape(self) -> None:
        code, stdout, stderr = self.run_cli(self.repo_argv("--json"))
        self.assertEqual(0, code, stderr)
        report = json.loads(stdout)
        self.assertEqual(
            {
                "tool",
                "status",
                "exitCode",
                "releaseEvidence",
                "preCheck",
                "repositoryRoot",
                "offline",
                "skipped",
                "steps",
                "failedStep",
                "notRun",
                "message",
            },
            set(report),
        )
        self.assertEqual("verify_local.py", report["tool"])
        self.assertEqual("passed", report["status"])
        self.assertEqual(0, report["exitCode"])
        self.assertIs(False, report["releaseEvidence"])
        self.assertIs(True, report["preCheck"])
        self.assertEqual(str(self.repo), report["repositoryRoot"])
        self.assertIs(False, report["offline"])
        self.assertEqual([], report["skipped"])
        self.assertIsNone(report["failedStep"])
        self.assertEqual(list(verify_local.NOT_RUN_STEPS), report["notRun"])
        self.assertEqual(
            list(verify_local.LOCAL_STEPS),
            [entry["name"] for entry in report["steps"]],
        )
        for entry in report["steps"]:
            self.assertEqual(
                {"name", "status", "exitCode", "message"}, set(entry)
            )
            self.assertEqual("passed", entry["status"])

    def test_skip_flags_mark_steps_skipped(self) -> None:
        code, stdout, stderr = self.run_cli(
            self.repo_argv("--skip-docs", "--skip-format", "--json")
        )
        self.assertEqual(0, code, stderr)
        report = json.loads(stdout)
        self.assertEqual(["format", "docs"], report["skipped"])
        statuses = {entry["name"]: entry["status"] for entry in report["steps"]}
        self.assertEqual("skipped", statuses["docs"])
        self.assertEqual("skipped", statuses["format"])
        self.assertEqual(
            [
                "restore",
                "build",
                "test",
                "examples",
                "aspnetcore-examples",
            ],
            self.runner.steps,
        )
        code, stdout, stderr = self.run_cli(
            self.repo_argv("--skip-docs", "--skip-format")
        )
        self.assertIn("SKIP docs", stdout)
        self.assertIn("SKIP format", stdout)

    def test_offline_exports_uv_offline_to_children(self) -> None:
        self.runner.record_path = self.record
        code, stdout, stderr = self.run_cli(
            self.repo_argv("--offline", "--json")
        )
        self.assertEqual(0, code, stderr)
        entries = [
            json.loads(line)
            for line in self.record.read_text(encoding="utf-8").splitlines()
            if line.strip()
        ]
        self.assertEqual(list(verify_local.LOCAL_STEPS), [e["step"] for e in entries])
        for entry in entries:
            self.assertEqual("1", entry["env"].get("UV_OFFLINE"))

    def test_offline_flag_absent_by_default(self) -> None:
        self.runner.record_path = self.record
        code, stdout, stderr = self.run_cli(self.repo_argv("--json"))
        self.assertEqual(0, code, stderr)
        entries = [
            json.loads(line)
            for line in self.record.read_text(encoding="utf-8").splitlines()
            if line.strip()
        ]
        self.assertTrue(entries)
        for entry in entries:
            self.assertNotIn("UV_OFFLINE", entry["env"])


# ---------------------------------------------------------------------------
# Real subprocess path with a fake dotnet executable on PATH
# ---------------------------------------------------------------------------


@unittest.skipIf(
    os.name == "nt",
    "a PATH shim cannot be executed as 'dotnet' on Windows (CreateProcess "
    "requires a PE image); the injected runner covers dispatch there.",
)
class FakeDotnetExecutableTests(_VerifyLocalTestCase):
    def setUp(self) -> None:
        super().setUp()
        self.behavior_dir = self.tmp / "behavior"
        self.behavior_dir.mkdir(parents=True, exist_ok=True)
        for step in (
            "restore",
            "build",
            "test",
            "examples",
            "aspnetcore-examples",
            "format",
        ):
            (self.behavior_dir / f"{step}.stdout").write_text(
                green_stdout(step, self.repo), encoding="utf-8", newline="\n"
            )
            (self.behavior_dir / f"{step}.code").write_text(
                "0\n", encoding="utf-8", newline="\n"
            )
        install_stub(self.bin_dir, "dotnet", FAKE_DOTNET_SCRIPT)

    def real_env(self) -> dict[str, str]:
        return {
            **self.env,
            "FAKE_DOTNET_BEHAVIOR": str(self.behavior_dir),
            "FAKE_DOTNET_RECORD": str(self.record),
        }

    def test_real_runner_uses_fake_dotnet_and_forwards_offline(self) -> None:
        code, stdout, stderr = self.run_cli(
            self.repo_argv("--offline", "--json"),
            env=self.real_env(),
            runner=verify_local.run_command,
        )
        self.assertEqual(0, code, stderr)
        report = json.loads(stdout)
        self.assertEqual(
            ["passed"] * len(verify_local.LOCAL_STEPS),
            [entry["status"] for entry in report["steps"]],
        )
        record = self.record.read_text(encoding="utf-8")
        self.assertIn("argv: restore FunnySharp.slnx --locked-mode", record)
        self.assertIn(
            "argv: build FunnySharp.slnx --configuration Release --no-restore",
            record,
        )
        self.assertIn(
            "argv: test FunnySharp.slnx --configuration Release --no-build --no-restore",
            record,
        )
        self.assertIn("UV_OFFLINE=1", record)

    def test_real_runner_propagates_child_failure(self) -> None:
        (self.behavior_dir / "format.code").write_text("1\n", encoding="utf-8")
        code, stdout, stderr = self.run_cli(
            self.repo_argv("--json"),
            env=self.real_env(),
            runner=verify_local.run_command,
        )
        self.assertEqual(1, code)
        report = json.loads(stdout)
        self.assertEqual("format", report["failedStep"])
        self.assertIn("exit code 1", report["message"])


if __name__ == "__main__":
    unittest.main()
