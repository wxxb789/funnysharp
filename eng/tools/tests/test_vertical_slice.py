"""Structural and behavioral tests for eng/tools/vertical_slice.py.

The suite is stdlib-only and never runs the real .NET pipeline. A small executable ``dotnet`` shim
on ``PATH`` stands in for the SDK and prints canned output, so the real ``subprocess`` path, the
step wiring, the summary parser, and the failure aggregation are all exercised deterministically.
The shim records the arguments it saw, which is how the tests prove that the consumer projects are
restored from the local feed with the injected package versions.
"""

from __future__ import annotations

import json
import os
import re
import stat
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock

TOOLS_DIR = Path(__file__).resolve().parents[1]
REPOSITORY_ROOT = Path(__file__).resolve().parents[3]

sys.path.insert(0, str(TOOLS_DIR))
import vertical_slice  # noqa: E402  (sys.path is set above)

VERIFY_MARKER = vertical_slice.VERIFY_MARKER
BASELINE_MARKER = vertical_slice.BASELINE_MARKER
MEASUREMENTS_MARKER = vertical_slice.MEASUREMENTS_MARKER

SHIM = """#!/usr/bin/env python3
import os
import sys

import time

arguments = sys.argv[1:]
if os.environ.get("SHIM_SLEEP") and "restore" in arguments:
    time.sleep(float(os.environ["SHIM_SLEEP"]))
with open(os.environ["SHIM_LOG"], "a", encoding="utf-8") as log:
    print(" ".join(arguments), file=log)

command = arguments[0] if arguments else ""
if command == "restore" or command == "build":
    print("  Restored 1 project.")
elif command == "run":
    if "--verify" in arguments:
        if "Baseline" in " ".join(arguments):
            print("%s")
        elif "Measurements" in " ".join(arguments):
            print("%s")
        else:
            print("%s")
    else:
        print("measurements written")
elif command == "test":
    print("Test run summary: Passed!")
    print("  total: 37")
    print("  failed: 0")
    print("  succeeded: 37")
    print("  skipped: 0")
else:
    print("packed")
sys.exit(int(os.environ.get("SHIM_EXIT_CODE", "0")))
""" % (BASELINE_MARKER, MEASUREMENTS_MARKER, VERIFY_MARKER)


class VerticalSliceToolTests(unittest.TestCase):
    def setUp(self) -> None:
        self.directory = tempfile.TemporaryDirectory()
        self.root = Path(self.directory.name)
        self.shim_directory = self.root / "bin"
        self.shim_directory.mkdir()
        self.shim = self.shim_directory / "dotnet"
        self.shim.write_text(SHIM, encoding="utf-8")
        self.shim.chmod(self.shim.stat().st_mode | stat.S_IEXEC)
        self.log = self.root / "shim.log"

        self.feed = self.root / "feed"
        self.feed.mkdir()
        (self.feed / "FunnySharp.1.2.3.nupkg").write_bytes(b"core")
        (self.feed / "FunnySharp.AspNetCore.1.2.3.nupkg").write_bytes(b"aspnet")
        (self.feed / "FunnySharp.AspNetCore.1.2.3.snupkg").write_bytes(b"symbols")

        self.environment = mock.patch.dict(
            os.environ,
            {
                "PATH": f"{self.shim_directory}{os.pathsep}{os.environ['PATH']}",
                "SHIM_LOG": str(self.log),
            },
        )
        self.environment.start()
        self.addCleanup(self.environment.stop)
        self.addCleanup(self.directory.cleanup)

    def run_tool(self, *extra: str) -> int:
        return vertical_slice.main(
            [
                "--no-pack",
                "--feed",
                str(self.feed),
                "--output",
                str(self.root / "output"),
                *extra,
            ]
        )

    def receipt(self) -> dict:
        return json.loads((self.root / "output" / "vertical-slice-results.json").read_text(encoding="utf-8"))

    def test_package_versions_ignores_the_longer_package_id(self) -> None:
        versions = vertical_slice.package_versions(self.feed)

        self.assertEqual({"FunnySharp": "1.2.3", "FunnySharp.AspNetCore": "1.2.3"}, versions)

    def test_package_versions_fails_closed_when_a_package_is_missing(self) -> None:
        (self.feed / "FunnySharp.AspNetCore.1.2.3.nupkg").unlink()

        with self.assertRaises(SystemExit):
            vertical_slice.package_versions(self.feed)

    def test_parse_summary_reads_the_real_runner_shape_and_both_localizations(self) -> None:
        # Shape Microsoft.Testing.Platform actually prints (see eng/tools/verify_local.py fixtures).
        real = "Test run summary: Passed!\n  total: 441\n  failed: 0\n  succeeded: 441\n  skipped: 0\n"
        legacy = "Test run summary:\n  Total: 37\n  Failed: 0\n  Passed: 37\n  Skipped: 0\n"
        chinese = "测试运行摘要:\n  总计: 37\n  失败: 0\n  成功: 37\n  已跳过: 0\n"
        expected = {"total": 37, "failed": 0, "passed": 37, "skipped": 0}

        self.assertEqual({"total": 441, "failed": 0, "passed": 441, "skipped": 0}, vertical_slice.parse_summary(real))
        self.assertEqual(expected, vertical_slice.parse_summary(legacy))
        self.assertEqual(expected, vertical_slice.parse_summary(chinese))
        self.assertIsNone(vertical_slice.parse_summary("no summary here"))
        # A summary missing one field is unparsable, never partially trusted.
        self.assertIsNone(vertical_slice.parse_summary("Test run summary: Passed!\n  total: 1\n  failed: 0\n"))

    def test_a_hung_step_fails_the_tool_instead_of_hanging_it(self) -> None:
        os.environ["SHIM_SLEEP"] = "5"
        self.addCleanup(os.environ.pop, "SHIM_SLEEP", None)
        bounded = unittest.mock.patch.object(vertical_slice, "STEP_TIMEOUT_SECONDS", 1)
        bounded.start()
        self.addCleanup(bounded.stop)

        self.assertEqual(1, self.run_tool("--skip-tests", "--skip-measurements"))

        receipt = self.receipt()
        self.assertEqual("fail", receipt["status"])
        restore = next(step for step in receipt["steps"] if step["step"] == "restore-api")
        self.assertEqual(vertical_slice.STEP_TIMEOUT_RETURNCODE, restore["exitCode"])
        self.assertIn("timed out", restore["stderrTail"])

    def test_verify_markers_match_the_application_constants(self) -> None:
        markers = {
            "tests/FunnySharp.VerticalSlice/FunnySharp.VerticalSlice.Api/VerticalSliceApp.cs": vertical_slice.VERIFY_MARKER,
            "tests/FunnySharp.VerticalSlice/FunnySharp.VerticalSlice.Baseline/BaselineApp.cs": vertical_slice.BASELINE_MARKER,
            "tests/FunnySharp.VerticalSlice/FunnySharp.VerticalSlice.Measurements/Program.cs": vertical_slice.MEASUREMENTS_MARKER,
        }
        for path, marker in markers.items():
            text = (REPOSITORY_ROOT / path).read_text(encoding="utf-8")
            match = re.search(r'VerifyMarker = "([^"]+)"', text)
            self.assertIsNotNone(match, path)
            self.assertEqual(marker, match.group(1), path)

    def test_a_green_run_records_every_step_and_passes(self) -> None:
        self.assertEqual(0, self.run_tool())

        receipt = self.receipt()
        self.assertEqual("pass", receipt["status"])
        self.assertEqual({"total": 37, "failed": 0, "passed": 37, "skipped": 0}, receipt["testSummary"])
        self.assertTrue(receipt["verifyMarker"])
        self.assertTrue(receipt["baselineVerifyMarker"])
        self.assertTrue(receipt["measurementsVerifyMarker"])
        steps = {step["step"] for step in receipt["steps"]}
        self.assertLessEqual(
            {
                "restore-api",
                "restore-tests",
                "restore-baseline",
                "restore-measurements",
                "build-api",
                "build-tests",
                "build-baseline",
                "build-measurements",
                "api-verify",
                "baseline-verify",
                "measurements-verify",
                "measurements",
                "consumer-tests",
            },
            steps,
        )
        self.assertTrue(all(step["exitCode"] == 0 for step in receipt["steps"]))

    def test_consumer_projects_are_restored_from_the_local_feed_with_injected_versions(self) -> None:
        self.assertEqual(0, self.run_tool())

        calls = self.log.read_text(encoding="utf-8").splitlines()
        restore = next(call for call in calls if call.startswith("restore") and "Tests" in call)
        self.assertIn(f"--source {self.feed}", restore)
        self.assertIn("--source https://api.nuget.org/v3/index.json", restore)
        self.assertIn("-p:FunnySharpPackageVersion=1.2.3", restore)
        self.assertIn("-p:FunnySharpAspNetCorePackageVersion=1.2.3", restore)

    def test_a_failing_dotnet_run_fails_the_tool(self) -> None:
        os.environ["SHIM_EXIT_CODE"] = "1"
        self.addCleanup(os.environ.pop, "SHIM_EXIT_CODE", None)

        self.assertEqual(1, self.run_tool())

        receipt = self.receipt()
        self.assertEqual("fail", receipt["status"])
        self.assertTrue(receipt["failures"])

    def test_measurement_mismatch_is_a_failure_without_a_marker_problem(self) -> None:
        shim = self.shim.read_text(encoding="utf-8")
        self.shim.write_text(shim.replace('sys.exit(int(os.environ.get("SHIM_EXIT_CODE", "0")))',
                                          'sys.exit(1 if "measurements.json" in arguments[-1] else 0)'), encoding="utf-8")
        self.shim.chmod(self.shim.stat().st_mode | stat.S_IEXEC)

        self.assertEqual(1, self.run_tool())

        receipt = self.receipt()
        self.assertEqual("fail", receipt["status"])
        self.assertNotIn("measurementsPath", receipt)
        self.assertTrue(any("behavioral difference" in failure for failure in receipt["failures"]))


if __name__ == "__main__":
    unittest.main()
