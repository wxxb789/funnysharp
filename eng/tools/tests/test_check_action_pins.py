"""Tests for eng/tools/check_action_pins.py.

Fixture workflows are written into a temporary directory and the checker is
pointed at that directory, so no real workflow needs to be mutated. The suite
also runs the checker over the real ``.github/workflows/`` tree: third-party
owners are checked everywhere, while ``release.yml``'s ``actions/*`` pins stay
with the frozen PowerShell protocol test (KTD8, R14).

The suite is stdlib-only and never parses YAML; the checker itself is
text-level, and the fixtures keep the YAML shapes the checker must recognize.
"""

from __future__ import annotations

import contextlib
import io
import sys
import tempfile
import unittest
from pathlib import Path

TOOLS_DIR = Path(__file__).resolve().parents[1]
REPOSITORY_ROOT = Path(__file__).resolve().parents[3]

sys.path.insert(0, str(TOOLS_DIR))

import check_action_pins  # noqa: E402  (sys.path is set above)

CHECKOUT_SHA = "11d5960a326750d5838078e36cf38b85af677262"
SETUP_DOTNET_SHA = "67a3573c9a986a3f9c594539f4ab511d57bb3ce9"
SETUP_UV_SHA = "bec219d24cd3e171d82865faccec33120bb574f4"

STEP_INDENT = "      "
KEY_INDENT = "        "


def step(*lines: str) -> list[str]:
    """Build one workflow step at the indentation used by these fixtures."""

    block = [f"{STEP_INDENT}- {lines[0]}"]
    block.extend(f"{KEY_INDENT}{line}" for line in lines[1:])
    return block


def continuation_step(name: str, value: str) -> list[str]:
    """One step whose ``uses:`` value sits on the next, more-indented line."""

    return [
        f"{STEP_INDENT}- name: {name}",
        f"{KEY_INDENT}uses:",
        f"{KEY_INDENT}  {value}",
    ]


def workflow_text(*steps: list[str]) -> str:
    """A minimal valid workflow carrying the given step blocks."""

    lines = [
        "name: fixture",
        "on:",
        "  pull_request:",
        "jobs:",
        "  fixture:",
        "    runs-on: ubuntu-latest",
        "    steps:",
    ]
    for block in steps:
        lines.extend(block)
    return "\n".join(lines) + "\n"


def write_workflows(root: Path, workflows: dict[str, str]) -> None:
    directory = root / ".github" / "workflows"
    directory.mkdir(parents=True, exist_ok=True)
    for name, text in workflows.items():
        (directory / name).write_text(text, encoding="utf-8", newline="\n")


def write_uv_pin(root: Path) -> None:
    (root / "uv.toml").write_text(
        'required-version = "==0.12.16"\n', encoding="utf-8", newline="\n"
    )


def line_of(text: str, needle: str) -> int:
    for number, line in enumerate(text.splitlines(), start=1):
        if needle in line:
            return number
    raise AssertionError(f"{needle!r} not found in fixture text")


class FixtureCheckMixin:
    """Run the checker against fixture workflows in a temp directory."""

    def check_fixture(
        self, workflows: dict[str, str], *, uv_pin: bool = False
    ) -> tuple[list[str], list[check_action_pins.Finding]]:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            write_workflows(root, workflows)
            if uv_pin:
                write_uv_pin(root)
            scanned, findings = check_action_pins.check_repository(root)
            return [path.name for path in scanned], findings


class PinCheckFixtureTests(FixtureCheckMixin, unittest.TestCase):
    def test_tag_reference_fails(self) -> None:
        text = workflow_text(step("name: Checkout", "uses: actions/checkout@v4"))
        scanned, findings = self.check_fixture({"tags.yml": text})
        self.assertEqual(scanned, ["tags.yml"])
        self.assertEqual(len(findings), 1)
        self.assertEqual(findings[0].line, line_of(text, "uses:"))
        self.assertIn("actions/checkout@v4", findings[0].message)
        self.assertIn("40-hex", findings[0].message)

    def test_full_sha_without_version_comment_fails(self) -> None:
        _, findings = self.check_fixture(
            {
                "sha.yml": workflow_text(
                    step("name: Checkout", f"uses: actions/checkout@{CHECKOUT_SHA}")
                )
            }
        )
        self.assertEqual(len(findings), 1)
        self.assertIn("actions/checkout@" + CHECKOUT_SHA, findings[0].message)
        self.assertIn("comment", findings[0].message)

    def test_unpinned_action_from_other_owner_fails(self) -> None:
        _, findings = self.check_fixture(
            {
                "other.yml": workflow_text(
                    step("name: Set up uv", "uses: astral-sh/setup-uv@v10")
                )
            }
        )
        self.assertEqual(len(findings), 1)
        self.assertIn("astral-sh/setup-uv@v10", findings[0].message)
        self.assertIn("40-hex", findings[0].message)

    def test_local_action_passes(self) -> None:
        scanned, findings = self.check_fixture(
            {
                "local.yml": workflow_text(
                    step("name: Local", "uses: ./.github/actions/build")
                )
            }
        )
        self.assertEqual(scanned, ["local.yml"])
        self.assertEqual(findings, [])

    def test_fully_valid_fixture_passes(self) -> None:
        scanned, findings = self.check_fixture(
            {
                "valid.yml": workflow_text(
                    step(
                        "name: Checkout",
                        f"uses: actions/checkout@{CHECKOUT_SHA} # v4",
                    ),
                    step(
                        "name: Set up .NET",
                        f"uses: actions/setup-dotnet@{SETUP_DOTNET_SHA} # v4",
                    ),
                    step("name: Local", "uses: ./eng/actions/local"),
                )
            }
        )
        self.assertEqual(scanned, ["valid.yml"])
        self.assertEqual(findings, [])

    def test_setup_uv_explicit_version_fails(self) -> None:
        text = workflow_text(
            step(
                "name: Set up uv",
                f"uses: astral-sh/setup-uv@{SETUP_UV_SHA} # v10.1.0",
                "with:",
                '  version: "0.12.16"',
            )
        )
        _, findings = self.check_fixture({"tooling.yml": text}, uv_pin=True)
        self.assertEqual(len(findings), 1)
        self.assertEqual(findings[0].line, line_of(text, "version:"))
        self.assertIn("must not set 'version:'", findings[0].message)
        self.assertIn("uv.toml", findings[0].message)

    def test_setup_uv_version_before_uses_fails(self) -> None:
        text = workflow_text(
            step(
                "name: Set up uv",
                "with:",
                '  version: "0.12.16"',
                f"uses: astral-sh/setup-uv@{SETUP_UV_SHA} # v10.1.0",
            )
        )
        _, findings = self.check_fixture({"tooling.yml": text}, uv_pin=True)
        self.assertEqual(len(findings), 1)
        self.assertEqual(findings[0].line, line_of(text, "version:"))
        self.assertIn("must not set 'version:'", findings[0].message)

    def test_setup_uv_flow_mapping_version_fails(self) -> None:
        text = workflow_text(
            step(
                "name: Set up uv",
                f"uses: astral-sh/setup-uv@{SETUP_UV_SHA} # v10.1.0",
                'with: {version: "0.12.16"}',
            )
        )
        _, findings = self.check_fixture({"tooling.yml": text}, uv_pin=True)
        self.assertEqual(len(findings), 1)
        self.assertEqual(findings[0].line, line_of(text, "version:"))
        self.assertIn("must not set 'version:'", findings[0].message)

    def test_setup_uv_quoted_version_key_fails(self) -> None:
        text = workflow_text(
            step(
                "name: Set up uv",
                f"uses: astral-sh/setup-uv@{SETUP_UV_SHA} # v10.1.0",
                "with:",
                '"version": "0.12.16"',
            )
        )
        _, findings = self.check_fixture({"tooling.yml": text}, uv_pin=True)
        self.assertEqual(len(findings), 1)
        self.assertEqual(findings[0].line, line_of(text, '"version":'))
        self.assertIn("must not set 'version:'", findings[0].message)

    def test_setup_uv_without_version_passes(self) -> None:
        text = workflow_text(
            step(
                "name: Set up uv",
                f"uses: astral-sh/setup-uv@{SETUP_UV_SHA} # v10.1.0",
                "with:",
                "enable-cache: false",
            )
        )
        scanned, findings = self.check_fixture({"tooling.yml": text}, uv_pin=True)
        self.assertEqual(scanned, ["tooling.yml"])
        self.assertEqual(findings, [])

    def test_setup_uv_version_in_another_step_passes(self) -> None:
        text = workflow_text(
            step(
                "name: Set up uv",
                f"uses: astral-sh/setup-uv@{SETUP_UV_SHA} # v10.1.0",
            ),
            step("name: Other", "with:", '  version: "1.2.3"'),
        )
        scanned, findings = self.check_fixture({"tooling.yml": text}, uv_pin=True)
        self.assertEqual(scanned, ["tooling.yml"])
        self.assertEqual(findings, [])

    def test_continuation_uses_value_fails(self) -> None:
        text = workflow_text(continuation_step("Checkout", "actions/checkout@v4"))
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            write_workflows(root, {"continuation.yml": text})
            scanned, findings = check_action_pins.check_repository(root)
            formatted = [finding.format(root) for finding in findings]
        self.assertEqual([path.name for path in scanned], ["continuation.yml"])
        line = line_of(text, "uses:")
        self.assertEqual(
            formatted,
            [
                f".github/workflows/continuation.yml:{line}: uses reference '' "
                "must be pinned as owner/repo@<40-hex-sha> with a "
                "'# <version>' comment"
            ],
        )

    def test_setup_uv_without_uv_pin_fails(self) -> None:
        _, findings = self.check_fixture(
            {
                "tooling.yml": workflow_text(
                    step(
                        "name: Set up uv",
                        f"uses: astral-sh/setup-uv@{SETUP_UV_SHA} # v10.1.0",
                    )
                )
            }
        )
        self.assertEqual(len(findings), 1)
        self.assertIn("uv.toml", findings[0].message)
        self.assertIn("required-version", findings[0].message)

    def test_setup_uv_range_pin_fails(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            write_workflows(
                root,
                {
                    "tooling.yml": workflow_text(
                        step(
                            "name: Set up uv",
                            f"uses: astral-sh/setup-uv@{SETUP_UV_SHA} # v10.1.0",
                        )
                    )
                },
            )
            (root / "uv.toml").write_text(
                'required-version = ">=0.12"\n', encoding="utf-8", newline="\n"
            )
            _, findings = check_action_pins.check_repository(root)
        self.assertEqual(len(findings), 1)
        self.assertIn("required-version", findings[0].message)
        self.assertIn("exact", findings[0].message)

    def test_setup_uv_valueless_pin_fails(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            write_workflows(
                root,
                {
                    "tooling.yml": workflow_text(
                        step(
                            "name: Set up uv",
                            f"uses: astral-sh/setup-uv@{SETUP_UV_SHA} # v10.1.0",
                        )
                    )
                },
            )
            (root / "uv.toml").write_text(
                "required-version =\n", encoding="utf-8", newline="\n"
            )
            _, findings = check_action_pins.check_repository(root)
        self.assertEqual(len(findings), 1)
        self.assertIn("required-version", findings[0].message)

    def test_release_workflow_actions_owner_is_delegated(self) -> None:
        release_text = workflow_text(
            step("name: Checkout", "uses: actions/checkout@v4")
        )
        _, findings = self.check_fixture({"release.yml": release_text})
        # actions/* in release.yml stays with the frozen PowerShell protocol
        # test; this checker deliberately ignores that owner there.
        self.assertEqual(findings, [])

    def test_release_workflow_third_party_tag_fails(self) -> None:
        release_text = workflow_text(
            step("name: Release", "uses: softprops/action-gh-release@v2")
        )
        _, findings = self.check_fixture({"release.yml": release_text})
        self.assertEqual(len(findings), 1)
        self.assertIn("softprops/action-gh-release@v2", findings[0].message)
        self.assertIn("40-hex", findings[0].message)

    def test_release_workflow_third_party_pinned_passes(self) -> None:
        release_text = workflow_text(
            step(
                "name: Release",
                f"uses: softprops/action-gh-release@{CHECKOUT_SHA} # v2",
            )
        )
        scanned, findings = self.check_fixture({"release.yml": release_text})
        self.assertEqual(scanned, ["release.yml"])
        self.assertEqual(findings, [])


class RealWorkflowTests(unittest.TestCase):
    def test_real_workflows_pass(self) -> None:
        scanned, findings = check_action_pins.check_repository(REPOSITORY_ROOT)
        self.assertEqual(
            [finding.format(REPOSITORY_ROOT) for finding in findings], []
        )
        names = [path.name for path in scanned]
        self.assertIn("tooling.yml", names)
        self.assertIn("release.yml", names)


class CommandLineTests(FixtureCheckMixin, unittest.TestCase):
    def run_main(self, argv: list[str]) -> tuple[int, str, str]:
        stdout = io.StringIO()
        stderr = io.StringIO()
        with contextlib.redirect_stdout(stdout), contextlib.redirect_stderr(stderr):
            exit_code = check_action_pins.main(argv)
        return exit_code, stdout.getvalue(), stderr.getvalue()

    def test_findings_exit_nonzero_with_file_and_line(self) -> None:
        text = workflow_text(step("name: Checkout", "uses: actions/checkout@v4"))
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            write_workflows(root, {"tags.yml": text})
            exit_code, _, stderr = self.run_main(
                ["--repository-root", str(root)]
            )
        self.assertEqual(exit_code, 1)
        self.assertIn(".github/workflows/tags.yml:9: ", stderr)
        self.assertIn("actions/checkout@v4", stderr)
        self.assertIn("FAIL:", stderr)

    def test_verbose_lists_scanned_files(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            write_workflows(
                root,
                {
                    "valid.yml": workflow_text(
                        step(
                            "name: Checkout",
                            f"uses: actions/checkout@{CHECKOUT_SHA} # v4",
                        )
                    )
                },
            )
            exit_code, stdout, stderr = self.run_main(
                ["--verbose", "--repository-root", str(root)]
            )
        self.assertEqual(exit_code, 0)
        self.assertEqual(stderr, "")
        self.assertIn("checked .github/workflows/valid.yml", stdout)
        self.assertIn("OK:", stdout)

    def test_missing_workflows_directory_is_an_environment_failure(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            exit_code, _, stderr = self.run_main(
                ["--repository-root", temporary]
            )
        self.assertEqual(exit_code, 2)
        self.assertIn("was not found", stderr)


class ToolingWorkflowStructureTests(unittest.TestCase):
    """The workflow's promotion-facing shape, asserted at the text level."""

    @classmethod
    def setUpClass(cls) -> None:
        cls.path = REPOSITORY_ROOT / ".github" / "workflows" / "tooling.yml"
        cls.text = cls.path.read_text(encoding="utf-8")

    def test_workflow_is_not_named_release(self) -> None:
        self.assertTrue(self.text.startswith("name: tooling\n"))

    def test_matrix_gate_job_is_stable(self) -> None:
        self.assertIn("\n  tooling-gate:\n", self.text)
        self.assertIn("needs: tooling\n", self.text)

    def test_workflow_does_not_shadow_required_release_contexts(self) -> None:
        for job_id in ("win-x64", "linux-x64", "osx-arm64", "osx-x64-consumer"):
            self.assertNotIn(f"\n  {job_id}:\n", self.text)


if __name__ == "__main__":
    unittest.main()
