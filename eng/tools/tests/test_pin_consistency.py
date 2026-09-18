"""Pin consistency across the Python tooling's version declarations.

``.python-version`` is the single exact pin for the interpreter; the PEP 723
``requires-python`` headers and ``verify_local.REQUIRED_PYTHON`` must stay
compatible with it, so the documented Exact-Pin Upgrade procedure in
docs/tooling.md actually moves every copy together (plan R10/KTD3).
"""

from __future__ import annotations

import re
import sys
import unittest
from pathlib import Path

TOOLS_DIR = Path(__file__).resolve().parents[1]
REPOSITORY_ROOT = Path(__file__).resolve().parents[3]

sys.path.insert(0, str(TOOLS_DIR))

import verify_local  # noqa: E402  (sys.path is set above)

REQUIRES_PYTHON_PATTERN = re.compile(
    r'^# requires-python = ">=(\d+)\.(\d+),<(\d+)\.(\d+)"$', re.MULTILINE
)

EXPECTED_HEADER_SCRIPTS = {
    "check_action_pins.py",
    "inventory.py",
    "verify_docs_snippets.py",
    "verify_local.py",
}


class PinConsistencyTests(unittest.TestCase):
    def test_pinned_minor_matches_runtime_contract_and_headers(self) -> None:
        pinned = (REPOSITORY_ROOT / ".python-version").read_text(encoding="utf-8")
        major, minor = (int(part) for part in pinned.strip().split(".")[:2])
        self.assertEqual((major, minor), verify_local.REQUIRED_PYTHON)

        header_scripts: set[str] = set()
        for script in sorted(TOOLS_DIR.glob("*.py")):
            match = REQUIRES_PYTHON_PATTERN.search(script.read_text(encoding="utf-8"))
            if match is None:
                continue  # plain helper modules carry no PEP 723 header
            header_scripts.add(script.name)
            lower = (int(match.group(1)), int(match.group(2)))
            upper = (int(match.group(3)), int(match.group(4)))
            self.assertEqual(lower, (major, minor), script.name)
            self.assertLess((major, minor), upper, script.name)

        self.assertEqual(EXPECTED_HEADER_SCRIPTS, header_scripts)


if __name__ == "__main__":
    unittest.main()
