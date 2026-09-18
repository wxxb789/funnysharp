#!/usr/bin/env -S uv run --no-project
# /// script
# requires-python = ">=3.12,<3.13"
# dependencies = []
# ///
"""Cross-platform orchestration for the next-stage evidence inventories.

This Python port preserves the established orchestration behavior:

* the C# dumper (`eng/next-stage-inventory/api-inventory.csproj`) stays the content
  engine; this script only orchestrates the eleven targets plus the language-ext
  type list,
* the targets are explicit data (assemblies, modes, core/resolve directories,
  include filters, titles, argument order),
* inputs fail closed: every missing path is listed with remediation and nothing is
  downloaded,
* every dumper-produced file and captured stream is normalized to LF and UTF-8
  without BOM so regeneration is byte-stable across operating systems.

Usage:
    uv run --no-project eng/tools/inventory.py [<baseline-root> <ref-pack-dir> [<output-dir>]]

Flags:
    --baseline-root DIR   extraction root of the pinned baseline packages
    --ref-pack-dir DIR    Microsoft.NETCore.App.Ref .../ref/net10.0 directory
    --output-dir DIR      output directory (default: a temp directory outside the repo)
    --check-inputs        report required-input status only; never builds or writes

Legacy positional order is preserved: <baseline-root> <ref-pack-dir> [<output-dir>].
Exit codes: 0 success, 1 build/target failure, 2 usage or environment failure.
See docs/next-stage/baselines.md for input acquisition and pin hashes.
"""

from __future__ import annotations

import argparse
import codecs
import json
import os
import re
import stat
import subprocess
import sys
import tempfile
from collections.abc import Callable, Mapping, Sequence
from dataclasses import dataclass
from pathlib import Path

CommandRunner = Callable[
    [Sequence[str], Mapping[str, str]], subprocess.CompletedProcess[bytes]
]


# ---------------------------------------------------------------------------
# Target table
# ---------------------------------------------------------------------------

ROOT_BASELINE = "baseline"
ROOT_REF = "ref"
ROOT_FUNNY_SHARP_BIN = "funny_sharp_bin"
ROOT_FUNNY_SHARP_ASPNET_BIN = "funny_sharp_aspnet_bin"
ROOT_ASPNET_FRAMEWORK = "aspnet_framework"
ROOT_ROSLYN = "roslyn"

TYPE_LIST_NAME = "types-languageext-4.4.9"

REMEDIATION = (
    "Remediation: acquire and extract the pinned inputs described in "
    "docs/next-stage/baselines.md; nothing is downloaded automatically."
)

FSHARP_INCLUDE = (
    r"^Microsoft\.FSharp\.(Core\.(FSharpOption|FSharpValueOption|FSharpResult|FSharpChoice|"
    r"FSharpFunc|FSharpType|FSharpValue|Unit|OptionModule|ValueOptionModule|ResultModule|Choice|"
    r"Nullable|LanguagePrimitives|Operators|ExtraTopLevelOperators|FuncConvert|"
    r"OptimizedClosures|Lazy|MatchFailureException|Printf)|"
    r"Collections\.(SeqModule|ListModule|ArrayModule|SetModule|MapModule|FSharpList|FSharpSet|"
    r"FSharpMap|ResizeArray|Seq)|"
    r"Control\.(FSharpAsync|FSharpMailboxProcessor|TaskBuilder|EventModule|FSharpEvent|IEvent|"
    r"LazyExtensions))"
)

LANGUAGE_EXT_INCLUDE = r"^LanguageExt\.[A-Za-z]+$"

BCL_SEQUENCES_LINQ_INCLUDES = (
    r"^System\.Linq\.(Enumerable|AsyncEnumerable|Lookup|IGrouping|IOrderedEnumerable|OrderedEnumerable)",
    r"^System\.Collections\.Generic\.(IEnumerable|IAsyncEnumerable|IReadOnlyList|IReadOnlyCollection|IReadOnlyDictionary|IList|IDictionary|List|Dictionary|HashSet|SortedSet|SortedDictionary|KeyValuePair|IEqualityComparer|IEnumerator|IAsyncEnumerator)",
    r"^System\.Span|^System\.ReadOnlySpan|^System\.Memory$|^System\.ReadOnlyMemory|^System\.MemoryExtensions|^System\.Array$|^System\.ArraySegment|^System\.Buffers\.",
)

BCL_COLLECTIONS_IMMUTABLE_INCLUDES = (
    r"^System\.Collections\.Immutable\.|^System\.Collections\.Frozen\.|^System\.Collections\.Concurrent\.|^System\.Collections\.ObjectModel\.",
)

BCL_ASYNC_CONCURRENCY_INCLUDES = (
    r"^System\.Threading\.Tasks\.(Task|ValueTask|TaskCompletionSource|TaskFactory|TaskScheduler|Parallel|ParallelOptions|TaskCreationOptions|TaskContinuationOptions|TaskStatus|TaskCanceledException|ValueTaskSourceStatus|IValueTaskSource|TaskExtensions|TaskAsyncEnumerableExtensions|ParallelEnumerable)",
    r"^System\.Threading\.(Channels\.|CancellationToken|CancellationTokenSource|CancellationTokenRegistration|TimeProvider|ITimer|Timer|PeriodicTimer|Lock|Interlocked|Volatile|LazyInitializer|Timeout|WaitHandle|ManualResetEventSlim|SemaphoreSlim|CountdownEvent|Barrier|ReaderWriterLockSlim)",
    r"^System\.(IAsyncDisposable|IDisposable|IAsyncEnumerable|TimeProvider|TimeoutException|OperationCanceledException)",
    r"^System\.Runtime\.CompilerServices\.(AsyncTaskMethodBuilder|AsyncValueTaskMethodBuilder|ConfiguredTaskAwaitable|ConfiguredValueTaskAwaitable|IAsyncStateMachine|AsyncIteratorMethodBuilder|TaskAwaiter|ValueTaskAwaiter|PoolingAsyncValueTaskMethodBuilder|EnumeratorCancellationAttribute|AsyncMethodBuilderAttribute|INotifyCompletion|ICriticalNotifyCompletion)",
)

BCL_LANGUAGE_ERRORS_INCLUDES = (
    r"^System\.(Nullable|Nullable`1|Func|Action|Predicate|Comparison|Converter|Lazy|Tuple|ValueTuple|Exception|AggregateException|SystemException|InvalidOperationException|ArgumentNullException|ArgumentException|ArgumentOutOfRangeException|OperationCanceledException|TimeoutException|ObjectDisposedException|NotSupportedException|NotImplementedException|FormatException|Environment|Math|Convert|String|StringComparison|DateTime|DateTimeOffset|TimeSpan|Guid|Uri|Random|Version|IEquatable|IComparable|IFormattable|ISpanFormattable|Comparison`1)",
    r"^System\.Diagnostics\.CodeAnalysis\.(MaybeNull|NotNull|AllowNull|DisallowNull|MaybeNullWhen|NotNullWhen|NotNullIfNotNull|MemberNotNull|DoesNotReturn|DoesNotReturnIf|SetsRequiredMembers|StringSyntax)",
    r"^System\.Runtime\.CompilerServices\.(NullableAttribute|NullableContextAttribute|IsReadOnlyAttribute|IsByRefLikeAttribute|RequiredMemberAttribute|CompilerFeatureRequiredAttribute)",
)


@dataclass(frozen=True)
class PathSpec:
    """A path expressed as a root key plus a relative suffix."""

    root: str
    rel: str = ""
    kind: str = "file"

    def resolve(self, inputs: Inputs) -> Path | None:
        base = inputs.root(self.root)
        if base is None:
            return None
        return base / self.rel if self.rel else base

    def display(self, inputs: Inputs) -> str:
        base = inputs.root(self.root)
        if base is None:
            prefix = inputs.missing_root_display(self.root)
            return f"{prefix}/{self.rel}" if self.rel else prefix
        return str(base / self.rel) if self.rel else str(base)


@dataclass(frozen=True)
class Target:
    """One dumper invocation; `list_types` selects the type-list target shape."""

    name: str
    title: str
    mode: str
    assemblies: tuple[PathSpec, ...]
    core_dir: PathSpec | None = None
    resolve_dirs: tuple[PathSpec, ...] = ()
    includes: tuple[str, ...] = ()
    list_types: bool = False


# The FunnySharp self-dump labels are commit-bound and recorded deliberately in
# this table (they match the committed dumps); third-party survey titles stay
# pinned to their recorded baseline.
TARGETS: tuple[Target, ...] = (
    Target(
        name="funny-sharp-core",
        title="FunnySharp core public API (Goal 15 completion, 0.1.0)",
        mode="runtime",
        assemblies=(PathSpec(ROOT_FUNNY_SHARP_BIN, "FunnySharp.dll"),),
    ),
    Target(
        name="funny-sharp-aspnetcore",
        title="FunnySharp.AspNetCore public API (Goal 15 completion, 0.1.0)",
        mode="runtime",
        assemblies=(PathSpec(ROOT_FUNNY_SHARP_ASPNET_BIN, "FunnySharp.AspNetCore.dll"),),
        resolve_dirs=(
            PathSpec(ROOT_FUNNY_SHARP_BIN, "", "dir"),
            PathSpec(ROOT_ASPNET_FRAMEWORK, "", "dir"),
        ),
    ),
    Target(
        name="funcky",
        title="Public API inventory: funcky",
        mode="runtime",
        assemblies=(PathSpec(ROOT_BASELINE, "funcky.3.6.0/lib/net10.0/Funcky.dll"),),
    ),
    Target(
        name="funcky-analyzers",
        title="Funcky built-in analyzers (metadata mode)",
        mode="metadata",
        assemblies=(
            PathSpec(ROOT_BASELINE, "funcky.3.6.0/analyzers/dotnet/cs/Funcky.BuiltinAnalyzers.dll"),
        ),
        core_dir=PathSpec(ROOT_REF, "", "dir"),
        resolve_dirs=(PathSpec(ROOT_ROSLYN, "", "dir"),),
    ),
    Target(
        name="csharpfunctionalextensions",
        title="Public API inventory: csharpfunctionalextensions",
        mode="runtime",
        assemblies=(
            PathSpec(
                ROOT_BASELINE,
                "csharpfunctionalextensions.3.7.0/lib/net8.0/CSharpFunctionalExtensions.dll",
            ),
        ),
    ),
    Target(
        name="fsharp-core",
        title="Public API inventory: fsharp-core",
        mode="runtime",
        assemblies=(PathSpec(ROOT_BASELINE, "fsharp.core.10.1.401/lib/netstandard2.1/FSharp.Core.dll"),),
        includes=(FSHARP_INCLUDE,),
    ),
    Target(
        name="language-ext",
        title="Public API inventory: language-ext",
        mode="metadata",
        assemblies=(
            PathSpec(ROOT_BASELINE, "languageext.core.4.4.9/lib/netstandard2.0/LanguageExt.Core.dll"),
        ),
        core_dir=PathSpec(ROOT_REF, "", "dir"),
        includes=(LANGUAGE_EXT_INCLUDE,),
    ),
    Target(
        name="bcl-sequences-linq",
        title="Public API inventory: bcl-sequences-linq",
        mode="metadata",
        assemblies=(
            PathSpec(ROOT_REF, "System.Linq.dll"),
            PathSpec(ROOT_REF, "System.Linq.AsyncEnumerable.dll"),
            PathSpec(ROOT_REF, "System.Runtime.dll"),
            PathSpec(ROOT_REF, "System.Collections.dll"),
            PathSpec(ROOT_REF, "System.Memory.dll"),
            PathSpec(ROOT_REF, "System.Buffers.dll"),
            PathSpec(ROOT_REF, "System.Threading.Tasks.Extensions.dll"),
        ),
        core_dir=PathSpec(ROOT_REF, "", "dir"),
        includes=BCL_SEQUENCES_LINQ_INCLUDES,
    ),
    Target(
        name="bcl-collections-immutable",
        title="Public API inventory: bcl-collections-immutable",
        mode="metadata",
        assemblies=(
            PathSpec(ROOT_REF, "System.Collections.Immutable.dll"),
            PathSpec(ROOT_REF, "System.Collections.dll"),
            PathSpec(ROOT_REF, "System.Collections.Concurrent.dll"),
            PathSpec(ROOT_REF, "System.Linq.dll"),
        ),
        core_dir=PathSpec(ROOT_REF, "", "dir"),
        includes=BCL_COLLECTIONS_IMMUTABLE_INCLUDES,
    ),
    Target(
        name="bcl-async-concurrency",
        title="Public API inventory: bcl-async-concurrency",
        mode="metadata",
        assemblies=(
            PathSpec(ROOT_REF, "System.Runtime.dll"),
            PathSpec(ROOT_REF, "System.Threading.Channels.dll"),
            PathSpec(ROOT_REF, "System.Threading.Tasks.Parallel.dll"),
            PathSpec(ROOT_REF, "System.Threading.Tasks.dll"),
            PathSpec(ROOT_REF, "System.Threading.dll"),
        ),
        core_dir=PathSpec(ROOT_REF, "", "dir"),
        includes=BCL_ASYNC_CONCURRENCY_INCLUDES,
    ),
    Target(
        name="bcl-language-errors",
        title="Public API inventory: bcl-language-errors",
        mode="metadata",
        assemblies=(
            PathSpec(ROOT_REF, "System.Runtime.dll"),
            PathSpec(ROOT_REF, "System.Runtime.Extensions.dll"),
            PathSpec(ROOT_REF, "System.Linq.Expressions.dll"),
        ),
        core_dir=PathSpec(ROOT_REF, "", "dir"),
        includes=BCL_LANGUAGE_ERRORS_INCLUDES,
    ),
    Target(
        name=TYPE_LIST_NAME,
        title="",
        mode="metadata",
        assemblies=(
            PathSpec(ROOT_BASELINE, "languageext.core.4.4.9/lib/netstandard2.0/LanguageExt.Core.dll"),
        ),
        core_dir=PathSpec(ROOT_REF, "", "dir"),
        list_types=True,
    ),
)


# ---------------------------------------------------------------------------
# Environment and inputs
# ---------------------------------------------------------------------------


@dataclass(frozen=True)
class Inputs:
    baseline_root: Path | None
    ref_pack_dir: Path | None
    funny_sharp_bin: Path
    funny_sharp_aspnet_bin: Path
    dotnet_root: Path
    aspnet_framework_dir: Path | None
    roslyn_dir: Path | None
    csproj: Path

    def root(self, key: str) -> Path | None:
        return {
            ROOT_BASELINE: self.baseline_root,
            ROOT_REF: self.ref_pack_dir,
            ROOT_FUNNY_SHARP_BIN: self.funny_sharp_bin,
            ROOT_FUNNY_SHARP_ASPNET_BIN: self.funny_sharp_aspnet_bin,
            ROOT_ASPNET_FRAMEWORK: self.aspnet_framework_dir,
            ROOT_ROSLYN: self.roslyn_dir,
        }[key]

    def missing_root_display(self, key: str) -> str:
        return {
            ROOT_BASELINE: "<baseline-root>",
            ROOT_REF: "<ref-pack-dir>",
            ROOT_ASPNET_FRAMEWORK: "<DOTNET_ROOT>/shared/Microsoft.AspNetCore.App/<version>",
            ROOT_ROSLYN: "<DOTNET_ROOT>/sdk/<version>/Roslyn/bincore",
        }.get(key, f"<{key}>")


@dataclass(frozen=True)
class RequiredInput:
    display: str
    path: Path | None
    kind: str
    purpose: str

    @property
    def present(self) -> bool:
        if self.path is None:
            return False
        return self.path.is_dir() if self.kind == "dir" else self.path.is_file()


class UsageFailure(Exception):
    """Bad CLI usage or an unsafe output target."""


class EnvironmentFailure(Exception):
    """Missing tooling or an unusable environment."""


def repository_root() -> Path:
    root = Path(__file__).resolve().parents[2]
    if not (root / "eng" / "next-stage-inventory" / "api-inventory.csproj").is_file():
        raise EnvironmentFailure(
            f"cannot locate the FunnySharp repository root from {Path(__file__).resolve()}"
        )
    return root


def _version_key(name: str) -> tuple[tuple[int, object], ...]:
    # Version sort ("10.0.9" < "10.0.11"), matching `sort -V`.
    parts = re.split(r"(\d+)", name)
    return tuple((1, int(part)) if part.isdigit() else (0, part) for part in parts)


def _newest_version_dir(parent: Path) -> Path | None:
    if not parent.is_dir():
        return None
    candidates = [entry for entry in parent.iterdir() if entry.is_dir()]
    if not candidates:
        return None
    return max(candidates, key=lambda entry: _version_key(entry.name))


def _global_json_sdk_version(repo_root: Path) -> str | None:
    try:
        data = json.loads((repo_root / "global.json").read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return None
    version = data.get("sdk", {}).get("version")
    return version if isinstance(version, str) and version else None


def _default_roslyn_dir(repo_root: Path, dotnet_root: Path) -> Path | None:
    sdk_parent = dotnet_root / "sdk"
    pinned = _global_json_sdk_version(repo_root)
    sdk: Path | None = None
    if pinned is not None and (sdk_parent / pinned).is_dir():
        sdk = sdk_parent / pinned
    if sdk is None:
        sdk = _newest_version_dir(sdk_parent)
    if sdk is None:
        return None
    return sdk / "Roslyn" / "bincore"


def resolve_inputs(
    repo_root: Path,
    env: Mapping[str, str],
    baseline_root: str | os.PathLike[str] | None = None,
    ref_pack_dir: str | os.PathLike[str] | None = None,
) -> Inputs:
    home = Path(env["HOME"]) if env.get("HOME") else Path.home()
    dotnet_root = Path(env.get("DOTNET_ROOT") or home / ".dotnet")
    funny_sharp_bin = Path(
        env.get("FUNNY_SHARP_BIN")
        or repo_root / "src" / "FunnySharp" / "bin" / "Release" / "net10.0"
    )
    funny_sharp_aspnet_bin = Path(
        env.get("FUNNY_SHARP_ASPNET_BIN")
        or repo_root / "src" / "FunnySharp.AspNetCore" / "bin" / "Release" / "net10.0"
    )
    aspnet_framework_dir = (
        Path(env["ASPNET_FRAMEWORK_DIR"])
        if env.get("ASPNET_FRAMEWORK_DIR")
        else _newest_version_dir(dotnet_root / "shared" / "Microsoft.AspNetCore.App")
    )
    roslyn_dir = (
        Path(env["ROSLYN_DIR"])
        if env.get("ROSLYN_DIR")
        else _default_roslyn_dir(repo_root, dotnet_root)
    )
    return Inputs(
        baseline_root=Path(baseline_root) if baseline_root else None,
        ref_pack_dir=Path(ref_pack_dir) if ref_pack_dir else None,
        funny_sharp_bin=funny_sharp_bin,
        funny_sharp_aspnet_bin=funny_sharp_aspnet_bin,
        dotnet_root=dotnet_root,
        aspnet_framework_dir=aspnet_framework_dir,
        roslyn_dir=roslyn_dir,
        csproj=repo_root / "eng" / "next-stage-inventory" / "api-inventory.csproj",
    )


def required_inputs(inputs: Inputs) -> list[RequiredInput]:
    required: list[RequiredInput] = []
    seen: set[tuple[str, str]] = set()

    def add(display: str, path: Path | None, kind: str, purpose: str) -> None:
        key = (display, kind)
        if key in seen:
            return
        seen.add(key)
        required.append(RequiredInput(display=display, path=path, kind=kind, purpose=purpose))

    add(
        str(inputs.csproj),
        inputs.csproj,
        "file",
        "inventory dumper project",
    )
    for target in TARGETS:
        for spec in target.assemblies:
            add(
                spec.display(inputs),
                spec.resolve(inputs),
                "file",
                f"required by target {target.name}",
            )
        for spec in target.resolve_dirs:
            add(
                spec.display(inputs),
                spec.resolve(inputs),
                "dir",
                f"resolve directory for target {target.name}",
            )
        if target.core_dir is not None:
            add(
                target.core_dir.display(inputs),
                target.core_dir.resolve(inputs),
                "dir",
                f"core directory for target {target.name}",
            )
    return required


def missing_inputs(inputs: Inputs) -> list[RequiredInput]:
    return [item for item in required_inputs(inputs) if not item.present]


def print_missing_inputs(missing: Sequence[RequiredInput]) -> None:
    for item in missing:
        print(f"MISSING: {item.display} ({item.purpose})", file=sys.stderr)
    print(REMEDIATION, file=sys.stderr)


def check_inputs(inputs: Inputs) -> int:
    items = required_inputs(inputs)
    presence = [(item, item.present) for item in items]
    for item, present in presence:
        state = "OK" if present else "MISSING"
        print(f"{state}: {item.display} ({item.purpose})")
    missing = [item for item, present in presence if not present]
    if missing:
        print(
            f"INPUT CHECK FAILED: {len(missing)} of {len(items)} required inputs are missing.",
            file=sys.stderr,
        )
        print(REMEDIATION, file=sys.stderr)
        return 2
    print(f"INPUT CHECK OK: all {len(items)} required inputs present.")
    return 0


# ---------------------------------------------------------------------------
# Output directory safety and text normalization
# ---------------------------------------------------------------------------


def _same_path(left: Path, right: Path) -> bool:
    left_real = os.path.normcase(os.path.realpath(left))
    right_real = os.path.normcase(os.path.realpath(right))
    return left_real == right_real


def _is_reparse_point(path: Path) -> bool:
    if path.is_symlink():
        return True
    if os.name == "nt":
        try:
            attributes = path.stat(follow_symlinks=False).st_file_attributes
        except OSError:
            return False
        return bool(attributes & stat.FILE_ATTRIBUTE_REPARSE_POINT)
    return False


def validate_output_dir(output_dir: str | os.PathLike[str], repo_root: Path) -> Path:
    absolute = Path(os.path.abspath(str(Path(output_dir).expanduser())))
    if _same_path(absolute, repo_root):
        raise UsageFailure(f"output directory must not be the repository root: {absolute}")
    for component in [*reversed(absolute.parents), absolute]:
        if _is_reparse_point(component):
            raise UsageFailure(
                f"output directory path contains a symlink or reparse point: {component}"
            )
    if absolute.exists() and not absolute.is_dir():
        raise UsageFailure(f"output path exists and is not a directory: {absolute}")
    return absolute


def default_output_dir() -> Path:
    # Resolve the system temp root so a symlinked /tmp does not trip the
    # reparse-point rejection for the default path.
    return Path(tempfile.gettempdir()).resolve() / "funnysharp-inventory"


def normalize_bytes(data: bytes) -> bytes:
    if data.startswith(codecs.BOM_UTF8):
        data = data[len(codecs.BOM_UTF8) :]
    return data.replace(b"\r\n", b"\n").replace(b"\r", b"\n")


def normalize_file(path: Path) -> bool:
    data = path.read_bytes()
    fixed = normalize_bytes(data)
    if fixed != data:
        path.write_bytes(fixed)
        return True
    return False


def _normalize_if_exists(path: Path) -> None:
    try:
        normalize_file(path)
    except (FileNotFoundError, IsADirectoryError):
        pass


# ---------------------------------------------------------------------------
# Command execution
# ---------------------------------------------------------------------------


def _child_env(env: Mapping[str, str], dotnet_root: Path) -> dict[str, str]:
    child = dict(env)
    current_path = env.get("PATH", env.get("Path", ""))
    new_path = str(dotnet_root) + os.pathsep + current_path
    child["PATH"] = new_path
    if os.name == "nt":
        child["Path"] = new_path
    child["DOTNET_ROOT"] = str(dotnet_root)
    child["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
    return child


def run_command(
    argv: Sequence[str], env: Mapping[str, str]
) -> subprocess.CompletedProcess[bytes]:
    return subprocess.run(
        list(argv),
        env=dict(env),
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=False,
    )


def target_argv(target: Target, inputs: Inputs, out_dir: Path) -> list[str]:
    """Build the dumper argv, preserving the established argument order."""

    def resolved(spec: PathSpec) -> str:
        path = spec.resolve(inputs)
        if path is None:  # preflight rejects unresolved roots before this point
            raise EnvironmentFailure(f"unresolved input for target {target.name}: {spec}")
        return str(path)

    argv = ["dotnet", "run", "-c", "Release", "--no-build", "--project", str(inputs.csproj), "--"]
    if target.mode == "metadata":
        argv += ["--mode", "metadata"]
        if target.core_dir is not None:
            argv += ["--core-dir", resolved(target.core_dir)]
        for spec in target.resolve_dirs:
            argv += ["--resolve-dir", resolved(spec)]
        for spec in target.assemblies:
            argv += ["--assembly", resolved(spec)]
    else:
        for spec in target.assemblies:
            argv += ["--assembly", resolved(spec)]
        for spec in target.resolve_dirs:
            argv += ["--resolve-dir", resolved(spec)]
    for pattern in target.includes:
        argv += ["--include", pattern]
    if target.list_types:
        argv += ["--list-types"]
    else:
        argv += [
            "--out-md",
            str(out_dir / f"inv-{target.name}.md"),
            "--out-json",
            str(out_dir / f"inv-{target.name}.json"),
            "--title",
            target.title,
        ]
    return argv


def _tail(path: Path, count: int) -> list[str]:
    if not path.is_file():
        return []
    text = path.read_text(encoding="utf-8", errors="replace")
    return text.splitlines()[-count:]


def build_dumper(
    inputs: Inputs,
    env: Mapping[str, str],
    runner: CommandRunner,
) -> bool:
    argv = ["dotnet", "build", str(inputs.csproj), "-c", "Release", "--nologo"]
    try:
        result = runner(argv, _child_env(env, inputs.dotnet_root))
    except FileNotFoundError as exc:
        raise EnvironmentFailure(
            f"dotnet executable not found ({exc}); install the .NET SDK pinned by global.json"
        ) from exc
    if result.returncode != 0:
        print("FAILED: inventory tool build", file=sys.stderr)
        output = normalize_bytes(result.stdout or b"") + normalize_bytes(result.stderr or b"")
        sys.stderr.write(output.decode("utf-8", errors="replace"))
        return False
    return True


def run_target(
    target: Target,
    inputs: Inputs,
    out_dir: Path,
    env: Mapping[str, str],
    runner: CommandRunner,
) -> bool:
    print(f"== {target.name}")
    argv = target_argv(target, inputs, out_dir)
    try:
        result = runner(argv, _child_env(env, inputs.dotnet_root))
    except FileNotFoundError as exc:
        raise EnvironmentFailure(
            f"dotnet executable not found ({exc}); install the .NET SDK pinned by global.json"
        ) from exc
    stdout = normalize_bytes(result.stdout or b"")
    stderr = normalize_bytes(result.stderr or b"")
    if target.list_types:
        txt = out_dir / f"{target.name}.txt"
        err = out_dir / f"{target.name}.err"
        txt.write_bytes(stdout)
        err.write_bytes(stderr)
        if result.returncode != 0:
            print(f"FAILED: {target.name}")
            for line in _tail(err, 5):
                print(line)
            return False
        return True
    log = out_dir / f"inv-{target.name}.log"
    log.write_bytes(stdout + stderr)
    _normalize_if_exists(out_dir / f"inv-{target.name}.md")
    _normalize_if_exists(out_dir / f"inv-{target.name}.json")
    if result.returncode != 0:
        print(f"FAILED: {target.name}")
        for line in _tail(log, 5):
            print(line)
        return False
    for line in _tail(log, 2):
        print(line)
    return True


def generate(
    inputs: Inputs,
    out_dir: Path,
    env: Mapping[str, str],
    runner: CommandRunner,
) -> int:
    if not build_dumper(inputs, env, runner):
        return 1
    failed = False
    for target in TARGETS:
        if not run_target(target, inputs, out_dir, env, runner):
            failed = True
    if failed:
        print(
            f"FAILED: one or more inventory targets failed; output in {out_dir} is incomplete",
            file=sys.stderr,
        )
        return 1
    print("ALL DONE")
    return 0


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="inventory.py",
        description="Regenerate the next-stage evidence inventories (see docs/next-stage/baselines.md).",
    )
    parser.add_argument("baseline_root", nargs="?", help="extraction root of the pinned packages")
    parser.add_argument("ref_pack_dir", nargs="?", help="Microsoft.NETCore.App.Ref ref/net10.0 directory")
    parser.add_argument("output_dir", nargs="?", help="output directory (default outside the repo)")
    parser.add_argument("--baseline-root", dest="baseline_root_flag", metavar="DIR")
    parser.add_argument("--ref-pack-dir", dest="ref_pack_dir_flag", metavar="DIR")
    parser.add_argument("--output-dir", dest="output_dir_flag", metavar="DIR")
    parser.add_argument(
        "--check-inputs",
        action="store_true",
        help="report required-input status without building or writing",
    )
    return parser


def _pick(
    flag_value: str | None,
    positional_value: str | None,
    name: str,
    parser: argparse.ArgumentParser,
) -> str | None:
    if flag_value is not None and positional_value is not None:
        parser.error(f"{name} was given both positionally and as --{name}")
    return flag_value if flag_value is not None else positional_value


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
    try:
        repo = repository_root()
    except EnvironmentFailure as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 2

    baseline_root = _pick(args.baseline_root_flag, args.baseline_root, "baseline-root", parser)
    ref_pack_dir = _pick(args.ref_pack_dir_flag, args.ref_pack_dir, "ref-pack-dir", parser)
    output_dir = _pick(args.output_dir_flag, args.output_dir, "output-dir", parser)

    inputs = resolve_inputs(repo, base_env, baseline_root, ref_pack_dir)
    if args.check_inputs:
        return check_inputs(inputs)

    try:
        out_dir = validate_output_dir(
            output_dir if output_dir is not None else default_output_dir(), repo
        )
    except UsageFailure as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        print(
            "Remediation: choose a writable output directory outside the repository "
            "whose path contains no symlinks or reparse points.",
            file=sys.stderr,
        )
        return 2

    missing = missing_inputs(inputs)
    if missing:
        print_missing_inputs(missing)
        return 2

    try:
        out_dir.mkdir(parents=True, exist_ok=True)
    except OSError as exc:
        print(f"ERROR: cannot create output directory {out_dir}: {exc}", file=sys.stderr)
        return 2
    print(f"== output {out_dir}")
    try:
        return generate(inputs, out_dir, base_env, command_runner)
    except EnvironmentFailure as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 2
    except OSError as exc:
        print(f"ERROR: cannot write inventory output in {out_dir}: {exc}", file=sys.stderr)
        print(
            "Remediation: choose a writable output directory outside the repository "
            f"or fix the permissions of {out_dir}.",
            file=sys.stderr,
        )
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
