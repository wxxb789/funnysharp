# Contributor Tooling

The release gate is the PowerShell protocol described in
[release-readiness.md](release-readiness.md). Beside it, the repository keeps a thin Python
tooling layer so contributors and agents on Windows x64, Linux x64 (including WSL2), and macOS
can run core verification, documentation-snippet checking, and inventory regeneration without
PowerShell. The tooling never enters the release path.

## Authority Split

- The PowerShell release protocol (`eng/Run-Release.ps1`, `eng/Verify-Release.ps1`,
  `eng/ReleaseProtocol.psm1`, `eng/release-protocol.json`, and `.github/workflows/release.yml`)
  is the only release gate. It keeps its four exact required contexts, and no Python tool is a
  release-protocol step.
- `eng/tools/verify_local.py` is a contributor pre-check. It reproduces the release verdict
  semantics for the steps it covers, but it is not release evidence, and the release gate never
  consumes its output.
- Python tools are not required checks. The informational `tooling-gate` workflow is not a merge
  gate; its promotion criteria are recorded below.
- The PowerShell documentation-snippet verifier
  (`examples/FunnySharp.DocumentationSamples/VerifyDocumentationSnippets.ps1`) remains the
  authoritative implementation for release. The Python verifier is a behavior-equivalent port
  bound by the same-change policy below.

## One-Time Setup

The tools are PEP 723 scripts with no third-party dependencies. Setup needs only uv, the pinned
Python interpreter, and the .NET SDK pinned by `global.json` (10.0.400).

Run every command from the repository root: that is where uv finds `uv.toml`, which enforces the
uv pin.

### Linux and macOS

```bash
curl -LsSf https://astral.sh/uv/install.sh | sh
# install .NET SDK 10.0.400 (the global.json pin)
uv --version        # expect 0.12.16; run `uv self update 0.12.16` if it differs
uv python install
```

### Windows

```powershell
powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"
# install .NET SDK 10.0.400 (the global.json pin)
uv --version        # expect 0.12.16; run `uv self update 0.12.16` if it differs
uv python install
```

Restart the shell after installing uv so the updated `PATH` finds it. Git Bash and WSL2 are not
required for any tool command on Windows; the `powershell` snippet above only runs the uv
installer.

The pinned versions live in one place: `uv.toml` holds `required-version = "==0.12.16"`,
`python-preference = "only-managed"`, and `python-downloads = "manual"`; `.python-version` holds
`3.12.14`. A mismatched uv fails with the required version and the exact update command instead
of drifting, and uv never downloads an interpreter implicitly.

If a package manager installed uv instead of the standalone installer, use that package
manager's upgrade path; installed binaries cannot self-update.

### WSL2

Treat the distro as Linux and follow the Linux instructions inside it.

- Install uv and the .NET SDK inside the distro and run the Linux `uv`.
- Keep the checkout on the Linux filesystem (for example `~/repos/funnysharp`), not on a
  `/mnt/c/...` mount: the mount is slow and mixes path semantics.
- Do not invoke `uv.exe`, a Windows Python, or a Windows .NET SDK through interop. The managed
  interpreter recorded by `.python-version` lives on the Linux side, and a Windows interpreter
  store is separate from it.
- `pwsh` is only needed for the release protocol and its tests; the tool commands on this page
  do not require it.

## Local Commands

### Local Pre-Check

```bash
uv run --no-project eng/tools/verify_local.py
```

Runs, in protocol order: locked-mode restore, Release build, the solution tests, both example
programs, the formatter check, and the documentation-snippet verifier. Output begins with
`FunnySharp local pre-check (not release evidence).` and states that it is not release evidence.

Flags:

- `--offline`: export `UV_OFFLINE=1` to child processes and never attempt network-dependent
  setup. dotnet restore and build may still contact configured feeds on first use.
- `--json`: print a machine-readable summary on stdout.
- `--skip-docs`, `--skip-format`: skip those two steps.
- `--repository-root PATH`: override the repository root resolved from the script location.

Exit codes: `0` pass, `1` check or marker-contract failure, `2` environment or usage failure.
Missing prerequisites (uv, the pinned interpreter, or a usable `dotnet`) fail with remediation
text, never a traceback and never a silent skip. The summary lists the release-protocol steps
outside local scope as not run, including pack, benchmarks, performance verification,
compatibility, and the PowerShell-only protocol steps.

### Documentation Snippets

```bash
uv run --no-project eng/tools/verify_docs_snippets.py
```

Verifies the eight primary guides against the snippet regions under
`examples/FunnySharp.DocumentationSamples`, prints
`Verified N C# documentation snippets across 8 primary guides.`, and exits `0` or `1`. It writes
nothing. `--repository-root` and `--samples-root` exist for fixture runs. The
PowerShell verifier remains authoritative for release.

### Inventory

```bash
uv run --no-project eng/tools/inventory.py --check-inputs
uv run --no-project eng/tools/inventory.py <baseline-root> <ref-pack-dir> <output-dir>
```

`--check-inputs` reports every required input and exits non-zero when any is missing, without
building or writing. Regeneration builds the C# dumper, runs the eleven targets plus the
language-ext type list, and writes only to the caller-chosen directory (default: a temp directory
outside the repository; the repository root itself is rejected); every dumper output and captured
stream is normalized to LF and UTF-8 without BOM. Any target failure exits non-zero with an
explicit incomplete-output statement. Inputs are never downloaded: acquisition and pin hashes are
in [next-stage/baselines.md](next-stage/baselines.md). Exit codes: `0` success, `1` build or target
failure, `2` usage or environment failure.

### Tooling Test Suite

```bash
uv run --no-project python -m unittest discover -s eng/tools/tests
```

Runs the unit, marker-contract, inventory, and snippet-parity suites. Parity cases that execute
the PowerShell verifier skip when `pwsh` is not on `PATH`; CI runs them on every OS and they
never skip there.

## Offline Operation

After the one-time setup, the tool scripts themselves need no network:

- `uv run --no-project --offline <script>` disables uv's network access for the run. Because the
  scripts declare `dependencies = []`, no package index is contacted.
- `verify_local.py --offline` exports `UV_OFFLINE=1` to its children. A fully offline pre-check
  additionally requires a previously restored and built tree, because dotnet restore and build
  keep their normal first-use feed behavior.
- `python-downloads = "manual"` means a missing interpreter is an explicit failure naming
  `uv python install`, not a download.

## Certificates, Proxies, And Mirrors

- Corporate TLS interception: `uv run --system-certs ...` or `UV_SYSTEM_CERTS=1` loads the
  platform's native certificate store instead of uv's bundled Mozilla roots. `UV_NATIVE_TLS=1`
  is the deprecated alias. `SSL_CERT_FILE` (one PEM bundle; when set, only its certificates are
  trusted) and `SSL_CERT_DIR` point uv at a custom CA.
- Proxies: uv respects `HTTP_PROXY`, `HTTPS_PROXY`, `ALL_PROXY`, and `NO_PROXY` (comma-separated
  hosts and patterns that bypass the proxy).
- Allowlisting and mirrors: first-time setup reaches the installer at `astral.sh`, uv release
  metadata and binaries at `releases.astral.sh` and GitHub release assets, and managed CPython
  from the `astral-sh/python-build-standalone` GitHub releases. `UV_ASTRAL_MIRROR_URL`,
  `UV_PYTHON_INSTALL_MIRROR`, and `UV_INSTALLER_GITHUB_BASE_URL` redirect those downloads to an
  internal mirror. No PyPI access is required at any point; NuGet feed access is a separate .NET
  concern.

## Exact-Pin Upgrade

uv and Python move together:

1. Choose the target uv release and a Python 3.12.x patch that it provides on Windows x64,
   Linux x64, and macOS.
2. Change `uv.toml` (`required-version`) and `.python-version` in one PR. CI reads the pin files
   rather than repeating the versions, so no workflow edit is needed.
3. On each OS run `uv python install`, then the tooling test suite and
   `uv run --no-project eng/tools/verify_local.py`.
4. Merge only after the tooling workflow is green on all three OSes.

Never move one pin file without the other. Contributors with the previous uv release get the
`required-version` failure, whose remediation names the exact `uv self update <version>` command.

## Policies

- **Parity.** The PowerShell snippet verifier is the reference. Any change to it must update
  `eng/tools/verify_docs_snippets.py` in the same change; differential fixtures in CI detect
  divergence.
- **Marker contract.** `verify_local.py` judges the build, test, and example steps with the
  frozen verifier's success markers and first asserts those literals still exist in
  `eng/Verify-Release.ps1`. If the verifier's verdict logic or the protocol step list changes,
  update the Python contract in the same change; otherwise the pre-check fails closed instead of
  judging with stale rules.
- **Same-change.** Changes to `eng/release-protocol.json`, the release verifier's markers, or the
  PowerShell snippet verifier must update the corresponding Python tooling in the same change.
  The release protocol family stays frozen; the Python layer adapts to it, never the reverse.
- **Historical records.** `eng/next-stage-inventory/generate.sh` was retired and replaced by
  `uv run --no-project eng/tools/inventory.py <baseline-root> <ref-pack-dir> <output-dir>`.
  Live guides point at the Python entry. Historical audit records keep their original text with
  a dated replacement note (see [review/final-review.md](next-stage/review/final-review.md));
  they are not rewritten, and no new work should invoke the retired path.
- **Clean tree.** Every tool run leaves `git status --porcelain --untracked-files=all` empty:
  Python artifacts are ignored and the tools write only to ignored or caller-chosen locations.
  A run that leaves tracked changes should be treated as a failure.

## Tooling Workflow And Promotion

`.github/workflows/tooling.yml` is informational. It runs the Python suites (including snippet
parity and the marker contract), the local pre-check, the action-pin check, and the existing
PowerShell protocol tests on Ubuntu, Windows, and macOS with `fail-fast: false`, and exposes one
stable aggregate job named `tooling-gate` for future promotion. It reads the uv and .NET pins
instead of repeating them, and it does not gate merges.

Promoting `tooling-gate` to a required check is a separate authorized ruleset change; the four
release contexts stay exact. Promote only when all of the following hold:

- Consecutive green matrix runs across all three OSes.
- Zero parity or marker-contract skips in those runs.
- No infrastructure-only reruns; each green reflects that run's own results.
- A bounded, predictable matrix duration.
- A named owner accepts the check.

While the workflow is informational, a red run is triaged within one working day and is never
left red beyond one week. A red `tooling-gate` does not block merges by itself, but it must not
be ignored.
