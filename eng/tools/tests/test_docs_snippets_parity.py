"""Differential tests for the Python documentation-snippet verifier.

The Python port in ``eng/tools/verify_docs_snippets.py`` must stay
behavior-equivalent to the authoritative PowerShell verifier in
``examples/FunnySharp.DocumentationSamples/VerifyDocumentationSnippets.ps1``.

Every fixture tree is generated into a temporary directory: a staged copy of
the PowerShell verifier, a copy of the eight primary guides, a copy of the
snippet samples, and generated ``bin``/``obj`` regions that the scan must
ignore, with exactly one mutation applied per case. The PowerShell
side of the parity assertions only runs when ``pwsh`` is on PATH. Local skips
are allowed and explicit; ``PowerShellAvailabilityGuardTests`` fails the suite
when ``CI`` is set without ``pwsh`` instead of letting the parity guarantee
disappear silently.
"""

from __future__ import annotations

import codecs
import os
import shutil
import subprocess
import sys
import tempfile
import unittest
from collections.abc import Callable
from dataclasses import dataclass
from pathlib import Path

TOOLS_DIR = Path(__file__).resolve().parents[1]
REPOSITORY_ROOT = Path(__file__).resolve().parents[3]
VERIFIER_PATH = TOOLS_DIR / "verify_docs_snippets.py"
SAMPLES_DIR_NAME = Path("examples") / "FunnySharp.DocumentationSamples"
PS_VERIFIER_PATH = REPOSITORY_ROOT / SAMPLES_DIR_NAME / "VerifyDocumentationSnippets.ps1"
GUIDE_UNDER_TEST = "unit-result.md"
SUCCESS_LINE = "Verified 25 C# documentation snippets across 8 primary guides."

sys.path.insert(0, str(TOOLS_DIR))
import verify_docs_snippets as verifier  # noqa: E402  (sys.path is set above)

PWSH = shutil.which("pwsh")
PWSH_SKIP_REASON = (
    "pwsh is not on PATH, so the PowerShell reference verifier cannot run here. "
    "Local skips are allowed and explicit; PowerShellAvailabilityGuardTests "
    "fails the suite instead when CI is set without pwsh."
)


class PowerShellAvailabilityGuardTests(unittest.TestCase):
    """A missing pwsh may skip locally but must never silently skip in CI."""

    def test_pwsh_is_available_in_ci(self) -> None:
        if not os.environ.get("CI"):
            self.skipTest("CI is not set; the pwsh skip is an explicit local skip")
        self.assertIsNotNone(
            PWSH,
            "CI is set but pwsh is not on PATH, so the PowerShell parity cases "
            "would silently skip; install PowerShell on the runner.",
        )


@dataclass(frozen=True)
class FixtureTree:
    root: Path
    docs: Path
    samples: Path

    def python_command(self) -> list[str]:
        return [
            sys.executable,
            str(VERIFIER_PATH),
            "--repository-root",
            str(self.root),
            "--samples-root",
            str(self.samples),
        ]

    def pwsh_command(self) -> list[str]:
        return [
            str(PWSH),
            "-NoProfile",
            "-NonInteractive",
            "-File",
            str(self.samples / PS_VERIFIER_PATH.name),
            "-RepositoryRoot",
            str(self.root),
        ]


def stage_fixture_tree(base: Path) -> FixtureTree:
    """Stage a valid repository tree: eight guides, the samples, and the PS verifier."""
    docs = base / "docs"
    samples = base / SAMPLES_DIR_NAME
    docs.mkdir(parents=True)
    samples.mkdir(parents=True)
    for guide in verifier.PRIMARY_GUIDES:
        shutil.copyfile(REPOSITORY_ROOT / "docs" / guide, docs / guide)
    for source in sorted((REPOSITORY_ROOT / SAMPLES_DIR_NAME).glob("*.cs")):
        shutil.copyfile(source, samples / source.name)
    shutil.copyfile(PS_VERIFIER_PATH, samples / PS_VERIFIER_PATH.name)
    # A region under a build directory must be excluded from the scan: if either
    # verifier picked it up, every otherwise-valid fixture tree would fail with
    # an unused-region error.
    for build_directory in ("bin", "obj"):
        generated = samples / build_directory / "Generated.cs"
        generated.parent.mkdir()
        generated.write_text(
            "// <snippet DocumentationSamples.Generated.Ignored>\n"
            "internal static class Generated { }\n"
            "// </snippet>\n",
            encoding="utf-8",
            newline="\n",
        )
    return FixtureTree(root=base, docs=docs, samples=samples)


def _read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def _write_text(path: Path, text: str) -> None:
    path.write_text(text, encoding="utf-8", newline="\n")


def _guide_path(tree: FixtureTree) -> Path:
    return tree.docs / GUIDE_UNDER_TEST


def _first_fence_block(lines: list[str]) -> list[str]:
    """Marker + opening fence + body + closing fence of the guide's first snippet."""
    fence_index = lines.index("```csharp")
    end_index = lines.index("```", fence_index + 1)
    return lines[fence_index - 1 : end_index + 1]


def mutate_duplicate_region(tree: FixtureTree) -> None:
    path = tree.samples / "EffectSamples.cs"
    text = _read_text(path)
    text += (
        "\n        // <snippet DocumentationSamples.Effects.CreateAndRun>\n"
        "        // </snippet>\n"
    )
    _write_text(path, text)


def mutate_missing_closing_marker(tree: FixtureTree) -> None:
    path = tree.samples / "ValidationSamples.cs"
    lines = _read_text(path).split("\n")
    assert any(line.strip() == "// </snippet>" for line in lines)
    lines = [line for line in lines if line.strip() != "// </snippet>"]
    _write_text(path, "\n".join(lines))


def mutate_fence_without_marker(tree: FixtureTree) -> None:
    path = _guide_path(tree)
    text = _read_text(path).rstrip("\n")
    _write_text(path, text + "\n\n```csharp\nvar value = 1;\n```\n")


def mutate_reused_region(tree: FixtureTree) -> None:
    path = _guide_path(tree)
    text = _read_text(path)
    block = _first_fence_block(text.split("\n"))
    _write_text(path, text.rstrip("\n") + "\n\n" + "\n".join(block) + "\n")


def mutate_unused_region(tree: FixtureTree) -> None:
    path = _guide_path(tree)
    lines = _read_text(path).split("\n")
    fence_index = lines.index("```csharp")
    end_index = lines.index("```", fence_index + 1)
    del lines[fence_index - 1 : end_index + 1]
    _write_text(path, "\n".join(lines))


def mutate_crlf_guide(tree: FixtureTree) -> None:
    path = _guide_path(tree)
    data = path.read_bytes().replace(b"\r\n", b"\n").replace(b"\n", b"\r\n")
    path.write_bytes(data)


def mutate_bom_guide(tree: FixtureTree) -> None:
    path = _guide_path(tree)
    path.write_bytes(codecs.BOM_UTF8 + path.read_bytes())


def mutate_guide_without_trailing_newline(tree: FixtureTree) -> None:
    path = _guide_path(tree)
    path.write_bytes(path.read_bytes().rstrip(b"\r\n"))


def mutate_indented_fence(tree: FixtureTree) -> None:
    path = _guide_path(tree)
    text = _read_text(path)
    assert "```csharp\n" in text
    _write_text(path, text.replace("```csharp\n", "    ```csharp\n", 1))


EMPTY_REGION_NAME = "DocumentationSamples.UnitResult.DeleteOrNotify"
EMPTY_REGION_SAMPLE = "UnitResultSamples.cs"


def mutate_empty_region_and_fence(tree: FixtureTree) -> None:
    """Empty one source region body and its snippet fence body.

    PowerShell's inclusive range counts down when the upper bound is below the
    lower bound, so both the region content and the snippet become the pair of
    delimiter lines in reverse order (closing line, then opening line) instead
    of two empty lists. Those pairs cannot be equal, so the verifier must fail;
    a naive ascending slice would compare two empty lists and incorrectly pass.
    """
    sample = tree.samples / EMPTY_REGION_SAMPLE
    lines = _read_text(sample).split("\n")
    start_index = next(
        index
        for index, line in enumerate(lines)
        if line.strip() == f"// <snippet {EMPTY_REGION_NAME}>"
    )
    end_index = next(
        index
        for index in range(start_index + 1, len(lines))
        if lines[index].strip() == "// </snippet>"
    )
    del lines[start_index + 1 : end_index]
    _write_text(sample, "\n".join(lines))

    path = _guide_path(tree)
    guide = _read_text(path).split("\n")
    marker_index = guide.index(f"<!-- documentation-sample: {EMPTY_REGION_NAME} -->")
    fence_index = marker_index + 1
    assert guide[fence_index] == "```csharp"
    end_fence = guide.index("```", fence_index + 1)
    del guide[fence_index + 1 : end_fence]
    _write_text(path, "\n".join(guide))


@dataclass(frozen=True)
class FixtureCase:
    name: str
    mutate: Callable[[FixtureTree], None] | None
    expected_exit: int
    expected_stderr_fragment: str | None = None


FIXTURE_CASES: tuple[FixtureCase, ...] = (
    FixtureCase("valid_tree", None, 0),
    FixtureCase("duplicate_region", mutate_duplicate_region, 1),
    FixtureCase("missing_closing_marker", mutate_missing_closing_marker, 1),
    FixtureCase("fence_without_marker", mutate_fence_without_marker, 1),
    FixtureCase("reused_region", mutate_reused_region, 1),
    FixtureCase("unused_region", mutate_unused_region, 1),
    FixtureCase("crlf_guide", mutate_crlf_guide, 0),
    FixtureCase("bom_guide", mutate_bom_guide, 0),
    FixtureCase("guide_without_trailing_newline", mutate_guide_without_trailing_newline, 0),
    FixtureCase(
        "indented_fence",
        mutate_indented_fence,
        1,
        expected_stderr_fragment="DocumentationSamples.UnitResult.DeleteOrNotify",
    ),
    FixtureCase(
        "empty_region_and_fence",
        mutate_empty_region_and_fence,
        1,
        expected_stderr_fragment=EMPTY_REGION_NAME,
    ),
)


class _FixtureTestCase(unittest.TestCase):
    def prepare(self, case: FixtureCase) -> FixtureTree:
        temp = tempfile.TemporaryDirectory(prefix=f"docs_snippets_{case.name}_")
        self.addCleanup(temp.cleanup)
        tree = stage_fixture_tree(Path(temp.name))
        if case.mutate is not None:
            case.mutate(tree)
        return tree

    def run_python(self, tree: FixtureTree) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            tree.python_command(),
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            cwd=tree.root,
        )

    def check_python_case(self, case: FixtureCase) -> None:
        tree = self.prepare(case)
        result = self.run_python(tree)
        self.assertEqual(
            result.returncode,
            case.expected_exit,
            msg=f"python stdout:\n{result.stdout}\npython stderr:\n{result.stderr}",
        )
        if case.expected_exit == 0:
            self.assertEqual(result.stdout.strip(), SUCCESS_LINE)
            self.assertEqual(result.stderr, "")
        if case.expected_stderr_fragment is not None:
            self.assertIn(case.expected_stderr_fragment, result.stderr)


class RepositoryVerificationTests(unittest.TestCase):
    """The verifier against the real repository, independent of the working directory."""

    def test_real_repository_python_verifier_passes(self) -> None:
        with tempfile.TemporaryDirectory(prefix="docs_snippets_cwd_") as cwd:
            result = subprocess.run(
                [sys.executable, str(VERIFIER_PATH)],
                capture_output=True,
                text=True,
                encoding="utf-8",
                errors="replace",
                cwd=cwd,
            )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stderr, "")
        self.assertEqual(result.stdout.strip(), SUCCESS_LINE)

    def test_pep723_header(self) -> None:
        text = VERIFIER_PATH.read_text(encoding="utf-8")
        self.assertTrue(text.splitlines()[0].startswith("#!"))
        self.assertIn("# /// script", text)
        self.assertIn('requires-python = ">=3.12,<3.13"', text)
        self.assertIn("dependencies = []", text)

    def test_default_root_outside_git_repository_fails_with_remediation(self) -> None:
        with tempfile.TemporaryDirectory(prefix="docs_snippets_nongit_") as temp:
            base = Path(temp)
            if verifier.find_git_root(base) is not None:  # pragma: no cover
                self.skipTest(f"{base} is inside a git repository")
            nested = base / "eng" / "tools"
            nested.mkdir(parents=True)
            copied = nested / VERIFIER_PATH.name
            shutil.copyfile(VERIFIER_PATH, copied)
            # The verifier imports the shared root resolver from its own directory.
            shutil.copyfile(TOOLS_DIR / "_repo.py", nested / "_repo.py")
            result = subprocess.run(
                [sys.executable, str(copied)],
                capture_output=True,
                text=True,
                encoding="utf-8",
                errors="replace",
                cwd=base,
            )
        self.assertEqual(result.returncode, 1)
        self.assertIn("not inside a git repository", result.stderr)
        self.assertIn("--repository-root", result.stderr)


class PythonFixtureTests(_FixtureTestCase):
    """Each fixture case exercised against the Python verifier."""


@unittest.skipUnless(PWSH, PWSH_SKIP_REASON)
class PowerShellParityTests(_FixtureTestCase):
    """Exit-status parity between the Python port and the PowerShell reference."""

    def check_parity(self, case: FixtureCase) -> None:
        tree = self.prepare(case)
        python_result = self.run_python(tree)
        pwsh_result = subprocess.run(
            tree.pwsh_command(),
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            cwd=tree.root,
        )
        self.assertEqual(
            python_result.returncode,
            pwsh_result.returncode,
            msg=(
                f"exit-status parity failed for '{case.name}':\n"
                f"python ({python_result.returncode}):\n"
                f"{python_result.stdout}{python_result.stderr}\n"
                f"pwsh ({pwsh_result.returncode}):\n"
                f"{pwsh_result.stdout}{pwsh_result.stderr}"
            ),
        )
        self.assertEqual(
            python_result.returncode,
            case.expected_exit,
            msg=f"python stderr:\n{python_result.stderr}\npwsh stderr:\n{pwsh_result.stderr}",
        )
        if case.expected_exit == 0:
            self.assertEqual(python_result.stdout.strip(), pwsh_result.stdout.strip())
        if case.expected_stderr_fragment is not None:
            self.assertIn(case.expected_stderr_fragment, pwsh_result.stderr)

    def test_real_repository_parity(self) -> None:
        python_result = subprocess.run(
            [sys.executable, str(VERIFIER_PATH)],
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            cwd=REPOSITORY_ROOT,
        )
        pwsh_result = subprocess.run(
            [str(PWSH), "-NoProfile", "-NonInteractive", "-File", str(PS_VERIFIER_PATH)],
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            cwd=REPOSITORY_ROOT,
        )
        self.assertEqual(python_result.returncode, 0, python_result.stderr)
        self.assertEqual(pwsh_result.returncode, 0, pwsh_result.stderr)
        self.assertEqual(python_result.stdout.strip(), SUCCESS_LINE)
        self.assertEqual(python_result.stdout.strip(), pwsh_result.stdout.strip())


class FixtureGenerationGuardTests(unittest.TestCase):
    """The dynamic fixture registration must never leave parity coverage at zero."""

    def test_parity_tests_are_generated_for_every_fixture_case(self) -> None:
        # A refactor that drops the setattr loop below would otherwise leave CI
        # green with zero parity executions. countTestCases() is an instance
        # method on the class, so count through a loaded suite instead.
        suite = unittest.TestLoader().loadTestsFromTestCase(PowerShellParityTests)
        self.assertGreaterEqual(suite.countTestCases(), len(FIXTURE_CASES) + 1)


def _python_case_method(case: FixtureCase) -> Callable[[_FixtureTestCase], None]:
    def test(self: _FixtureTestCase) -> None:
        self.check_python_case(case)

    test.__name__ = f"test_{case.name}"
    test.__doc__ = f"Python verifier exit status for the '{case.name}' fixture."
    return test


def _parity_case_method(case: FixtureCase) -> Callable[[PowerShellParityTests], None]:
    def test(self: PowerShellParityTests) -> None:
        self.check_parity(case)

    test.__name__ = f"test_{case.name}_parity"
    test.__doc__ = f"Exit-status parity for the '{case.name}' fixture."
    return test


for _case in FIXTURE_CASES:
    _python_test = _python_case_method(_case)
    setattr(PythonFixtureTests, _python_test.__name__, _python_test)
    _parity_test = _parity_case_method(_case)
    setattr(PowerShellParityTests, _parity_test.__name__, _parity_test)


if __name__ == "__main__":
    unittest.main()
