"""Structural and behavioral tests for eng/tools/inventory.py.

The suite is stdlib-only and never builds the C# dumper: command execution is
injected as a fake runner. Real dumper builds and byte-for-byte regeneration need
the pinned external inputs described in docs/next-stage/baselines.md and are
intentionally out of scope for the unit suite.
"""

from __future__ import annotations

import codecs
import io
import os
import re
import subprocess
import sys
import tempfile
import unittest
from contextlib import redirect_stderr, redirect_stdout
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import inventory  # noqa: E402

BASELINE_FILES = (
    "funcky.3.6.0/lib/net10.0/Funcky.dll",
    "funcky.3.6.0/analyzers/dotnet/cs/Funcky.BuiltinAnalyzers.dll",
    "csharpfunctionalextensions.3.7.0/lib/net8.0/CSharpFunctionalExtensions.dll",
    "fsharp.core.10.1.401/lib/netstandard2.1/FSharp.Core.dll",
    "languageext.core.4.4.9/lib/netstandard2.0/LanguageExt.Core.dll",
)

REF_FILES = (
    "System.Linq.dll",
    "System.Linq.AsyncEnumerable.dll",
    "System.Runtime.dll",
    "System.Collections.dll",
    "System.Memory.dll",
    "System.Buffers.dll",
    "System.Threading.Tasks.Extensions.dll",
    "System.Collections.Immutable.dll",
    "System.Collections.Concurrent.dll",
    "System.Threading.Channels.dll",
    "System.Threading.Tasks.Parallel.dll",
    "System.Threading.Tasks.dll",
    "System.Threading.dll",
    "System.Runtime.Extensions.dll",
    "System.Linq.Expressions.dll",
)

NAMES = (
    "funny-sharp-core",
    "funny-sharp-aspnetcore",
    "funcky",
    "funcky-analyzers",
    "csharpfunctionalextensions",
    "fsharp-core",
    "language-ext",
    "bcl-sequences-linq",
    "bcl-collections-immutable",
    "bcl-async-concurrency",
    "bcl-language-errors",
    inventory.TYPE_LIST_NAME,
)

TITLES = {
    "funny-sharp-core": "FunnySharp core public API (Goal 15 completion, 0.1.0)",
    "funny-sharp-aspnetcore": "FunnySharp.AspNetCore public API (Goal 15 completion, 0.1.0)",
    "funcky": "Public API inventory: funcky",
    "funcky-analyzers": "Funcky built-in analyzers (metadata mode)",
    "csharpfunctionalextensions": "Public API inventory: csharpfunctionalextensions",
    "fsharp-core": "Public API inventory: fsharp-core",
    "language-ext": "Public API inventory: language-ext",
    "bcl-sequences-linq": "Public API inventory: bcl-sequences-linq",
    "bcl-collections-immutable": "Public API inventory: bcl-collections-immutable",
    "bcl-async-concurrency": "Public API inventory: bcl-async-concurrency",
    "bcl-language-errors": "Public API inventory: bcl-language-errors",
}

MODES = {
    "funny-sharp-core": "runtime",
    "funny-sharp-aspnetcore": "runtime",
    "funcky": "runtime",
    "funcky-analyzers": "metadata",
    "csharpfunctionalextensions": "runtime",
    "fsharp-core": "runtime",
    "language-ext": "metadata",
    "bcl-sequences-linq": "metadata",
    "bcl-collections-immutable": "metadata",
    "bcl-async-concurrency": "metadata",
    "bcl-language-errors": "metadata",
    inventory.TYPE_LIST_NAME: "metadata",
}

INCLUDES = {
    "fsharp-core": (
        r"^Microsoft\.FSharp\.(Core\.(FSharpOption|FSharpValueOption|FSharpResult|FSharpChoice|FSharpFunc|FSharpType|FSharpValue|Unit|OptionModule|ValueOptionModule|ResultModule|Choice|Nullable|LanguagePrimitives|Operators|ExtraTopLevelOperators|FuncConvert|OptimizedClosures|Lazy|MatchFailureException|Printf)|Collections\.(SeqModule|ListModule|ArrayModule|SetModule|MapModule|FSharpList|FSharpSet|FSharpMap|ResizeArray|Seq)|Control\.(FSharpAsync|FSharpMailboxProcessor|TaskBuilder|EventModule|FSharpEvent|IEvent|LazyExtensions))",
    ),
    "language-ext": (r"^LanguageExt\.[A-Za-z]+$",),
    "bcl-sequences-linq": (
        r"^System\.Linq\.(Enumerable|AsyncEnumerable|Lookup|IGrouping|IOrderedEnumerable|OrderedEnumerable)",
        r"^System\.Collections\.Generic\.(IEnumerable|IAsyncEnumerable|IReadOnlyList|IReadOnlyCollection|IReadOnlyDictionary|IList|IDictionary|List|Dictionary|HashSet|SortedSet|SortedDictionary|KeyValuePair|IEqualityComparer|IEnumerator|IAsyncEnumerator)",
        r"^System\.Span|^System\.ReadOnlySpan|^System\.Memory$|^System\.ReadOnlyMemory|^System\.MemoryExtensions|^System\.Array$|^System\.ArraySegment|^System\.Buffers\.",
    ),
    "bcl-collections-immutable": (
        r"^System\.Collections\.Immutable\.|^System\.Collections\.Frozen\.|^System\.Collections\.Concurrent\.|^System\.Collections\.ObjectModel\.",
    ),
    "bcl-async-concurrency": (
        r"^System\.Threading\.Tasks\.(Task|ValueTask|TaskCompletionSource|TaskFactory|TaskScheduler|Parallel|ParallelOptions|TaskCreationOptions|TaskContinuationOptions|TaskStatus|TaskCanceledException|ValueTaskSourceStatus|IValueTaskSource|TaskExtensions|TaskAsyncEnumerableExtensions|ParallelEnumerable)",
        r"^System\.Threading\.(Channels\.|CancellationToken|CancellationTokenSource|CancellationTokenRegistration|TimeProvider|ITimer|Timer|PeriodicTimer|Lock|Interlocked|Volatile|LazyInitializer|Timeout|WaitHandle|ManualResetEventSlim|SemaphoreSlim|CountdownEvent|Barrier|ReaderWriterLockSlim)",
        r"^System\.(IAsyncDisposable|IDisposable|IAsyncEnumerable|TimeProvider|TimeoutException|OperationCanceledException)",
        r"^System\.Runtime\.CompilerServices\.(AsyncTaskMethodBuilder|AsyncValueTaskMethodBuilder|ConfiguredTaskAwaitable|ConfiguredValueTaskAwaitable|IAsyncStateMachine|AsyncIteratorMethodBuilder|TaskAwaiter|ValueTaskAwaiter|PoolingAsyncValueTaskMethodBuilder|EnumeratorCancellationAttribute|AsyncMethodBuilderAttribute|INotifyCompletion|ICriticalNotifyCompletion)",
    ),
    "bcl-language-errors": (
        r"^System\.(Nullable|Nullable`1|Func|Action|Predicate|Comparison|Converter|Lazy|Tuple|ValueTuple|Exception|AggregateException|SystemException|InvalidOperationException|ArgumentNullException|ArgumentException|ArgumentOutOfRangeException|OperationCanceledException|TimeoutException|ObjectDisposedException|NotSupportedException|NotImplementedException|FormatException|Environment|Math|Convert|String|StringComparison|DateTime|DateTimeOffset|TimeSpan|Guid|Uri|Random|Version|IEquatable|IComparable|IFormattable|ISpanFormattable|Comparison`1)",
        r"^System\.Diagnostics\.CodeAnalysis\.(MaybeNull|NotNull|AllowNull|DisallowNull|MaybeNullWhen|NotNullWhen|NotNullIfNotNull|MemberNotNull|DoesNotReturn|DoesNotReturnIf|SetsRequiredMembers|StringSyntax)",
        r"^System\.Runtime\.CompilerServices\.(NullableAttribute|NullableContextAttribute|IsReadOnlyAttribute|IsByRefLikeAttribute|RequiredMemberAttribute|CompilerFeatureRequiredAttribute)",
    ),
}


class FakeDotnet:
    """Records calls, writes dumper-like CRLF/BOM output, and never executes dotnet."""

    def __init__(self, fail_build: bool = False, fail_targets: tuple[str, ...] = ()) -> None:
        self.calls: list[tuple[list[str], dict[str, str]]] = []
        self.fail_build = fail_build
        self.fail_targets = set(fail_targets)

    def __call__(self, argv, env):
        argv = [str(item) for item in argv]
        self.calls.append((argv, dict(env)))
        if argv[1] == "build":
            code = 1 if self.fail_build else 0
            return subprocess.CompletedProcess(
                argv, code, stdout=b"build stdout\r\n", stderr=b"build stderr\r\n"
            )
        name = self._target_name(argv)
        if "--out-md" in argv:
            Path(argv[argv.index("--out-md") + 1]).write_bytes(
                b"\xef\xbb\xbf# title\r\n\r\nType count: 1\r\n"
            )
            Path(argv[argv.index("--out-json") + 1]).write_bytes(
                b'\xef\xbb\xbf{\r\n  "title": "x"\r\n}\r\n'
            )
        if name in self.fail_targets:
            stdout = b"boom\r\nsecond\r\nthird\r\nfourth\r\nfifth\r\nsixth\r\n"
            return subprocess.CompletedProcess(argv, 1, stdout=stdout, stderr=b"warn\r\n")
        return subprocess.CompletedProcess(
            argv, 0, stdout=b"wrote file\r\nsecond line\r\n", stderr=b"warn\r\n"
        )

    @staticmethod
    def _target_name(argv: list[str]) -> str:
        if "--list-types" in argv:
            return inventory.TYPE_LIST_NAME
        md = argv[argv.index("--out-md") + 1]
        return Path(md).name[len("inv-") : -len(".md")]


class InventoryTestCase(unittest.TestCase):
    maxDiff = None

    def setUp(self) -> None:
        tmp = tempfile.TemporaryDirectory(prefix="inventory-test-")
        self.addCleanup(tmp.cleanup)
        # Resolve the scratch root once: on macOS the temp directory lives under
        # /var, which is a symlink to /private/var, and the product intentionally
        # rejects output paths containing symlinks or reparse points.
        self.tmp = Path(tmp.name).resolve()
        self.repo = inventory.repository_root()
        self.baseline = self.tmp / "baselines"
        self.ref = self.tmp / "ref" / "net10.0"
        self.fsbin = self.tmp / "fsbin"
        self.aspnetbin = self.tmp / "aspnetbin"
        self.dotnet_root = self.tmp / "dotnet"
        self._stage_inputs()
        self.env = {
            "HOME": str(self.tmp / "home"),
            "DOTNET_ROOT": str(self.dotnet_root),
            "FUNNY_SHARP_BIN": str(self.fsbin),
            "FUNNY_SHARP_ASPNET_BIN": str(self.aspnetbin),
            "PATH": os.environ.get("PATH", ""),
        }

    def _stage_inputs(self) -> None:
        for rel in BASELINE_FILES:
            self._touch(self.baseline / rel)
        for rel in REF_FILES:
            self._touch(self.ref / rel)
        self._touch(self.fsbin / "FunnySharp.dll")
        self._touch(self.aspnetbin / "FunnySharp.AspNetCore.dll")
        aspnet = self.dotnet_root / "shared" / "Microsoft.AspNetCore.App"
        (aspnet / "10.0.9").mkdir(parents=True, exist_ok=True)
        (aspnet / "10.0.11").mkdir(parents=True, exist_ok=True)
        (self.dotnet_root / "sdk" / "10.0.400" / "Roslyn" / "bincore").mkdir(
            parents=True, exist_ok=True
        )

    @staticmethod
    def _touch(path: Path) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(b"stub")

    def inputs(self):
        return inventory.resolve_inputs(self.repo, self.env, self.baseline, self.ref)

    def generation_argv(self, out_dir: Path) -> list[str]:
        return [
            "--baseline-root",
            str(self.baseline),
            "--ref-pack-dir",
            str(self.ref),
            "--output-dir",
            str(out_dir),
        ]

    def run_cli(self, runner, argv, env=None):
        stdout, stderr = io.StringIO(), io.StringIO()
        with redirect_stdout(stdout), redirect_stderr(stderr):
            code = inventory.main(
                list(argv),
                env=dict(self.env if env is None else env),
                runner=runner,
            )
        return code, stdout.getvalue(), stderr.getvalue()

    # ------------------------------------------------------------------
    # Structural target table
    # ------------------------------------------------------------------

    def test_target_names_and_order(self) -> None:
        self.assertEqual(NAMES, tuple(target.name for target in inventory.TARGETS))

    def test_eleven_generation_targets_plus_type_list(self) -> None:
        self.assertEqual(12, len(inventory.TARGETS))
        self.assertEqual(11, len([t for t in inventory.TARGETS if not t.list_types]))
        self.assertEqual(
            [inventory.TYPE_LIST_NAME],
            [t.name for t in inventory.TARGETS if t.list_types],
        )

    def test_titles_match_legacy_invocations(self) -> None:
        expected = dict(TITLES)
        expected[inventory.TYPE_LIST_NAME] = ""
        self.assertEqual(
            expected, {target.name: target.title for target in inventory.TARGETS}
        )

    def test_modes_match_legacy_invocations(self) -> None:
        self.assertEqual(MODES, {target.name: target.mode for target in inventory.TARGETS})

    def test_include_filters_match_legacy_invocations(self) -> None:
        for target in inventory.TARGETS:
            self.assertEqual(
                INCLUDES.get(target.name, ()), tuple(target.includes), target.name
            )

    def test_target_argv_matches_legacy_invocations(self) -> None:
        inputs = self.inputs()
        out = self.tmp / "out"
        j = os.path.join
        cp = str(self.repo / "eng" / "next-stage-inventory" / "api-inventory.csproj")
        fs = str(self.fsbin)
        asp = str(self.aspnetbin)
        base = str(self.baseline)
        ref = str(self.ref)
        rosl = str(self.dotnet_root / "sdk" / "10.0.400" / "Roslyn" / "bincore")
        af = str(self.dotnet_root / "shared" / "Microsoft.AspNetCore.App" / "10.0.11")

        def head() -> list[str]:
            return ["dotnet", "run", "-c", "Release", "--no-build", "--project", cp, "--"]

        def outs(name: str) -> list[str]:
            return [
                "--out-md",
                str(out / f"inv-{name}.md"),
                "--out-json",
                str(out / f"inv-{name}.json"),
                "--title",
                TITLES[name],
            ]

        def inc(name: str) -> list[str]:
            result: list[str] = []
            for pattern in INCLUDES.get(name, ()):
                result += ["--include", pattern]
            return result

        expected = {
            "funny-sharp-core": head()
            + ["--assembly", j(fs, "FunnySharp.dll")]
            + outs("funny-sharp-core"),
            "funny-sharp-aspnetcore": head()
            + ["--assembly", j(asp, "FunnySharp.AspNetCore.dll")]
            + ["--resolve-dir", fs, "--resolve-dir", af]
            + outs("funny-sharp-aspnetcore"),
            "funcky": head()
            + ["--assembly", j(base, "funcky.3.6.0", "lib", "net10.0", "Funcky.dll")]
            + outs("funcky"),
            "funcky-analyzers": head()
            + [
                "--mode",
                "metadata",
                "--core-dir",
                ref,
                "--resolve-dir",
                rosl,
                "--assembly",
                j(base, "funcky.3.6.0", "analyzers", "dotnet", "cs", "Funcky.BuiltinAnalyzers.dll"),
            ]
            + outs("funcky-analyzers"),
            "csharpfunctionalextensions": head()
            + [
                "--assembly",
                j(
                    base,
                    "csharpfunctionalextensions.3.7.0",
                    "lib",
                    "net8.0",
                    "CSharpFunctionalExtensions.dll",
                ),
            ]
            + outs("csharpfunctionalextensions"),
            "fsharp-core": head()
            + [
                "--assembly",
                j(base, "fsharp.core.10.1.401", "lib", "netstandard2.1", "FSharp.Core.dll"),
            ]
            + inc("fsharp-core")
            + outs("fsharp-core"),
            "language-ext": head()
            + [
                "--mode",
                "metadata",
                "--core-dir",
                ref,
                "--assembly",
                j(base, "languageext.core.4.4.9", "lib", "netstandard2.0", "LanguageExt.Core.dll"),
            ]
            + inc("language-ext")
            + outs("language-ext"),
            "bcl-sequences-linq": head()
            + ["--mode", "metadata", "--core-dir", ref]
            + [
                "--assembly",
                j(ref, "System.Linq.dll"),
                "--assembly",
                j(ref, "System.Linq.AsyncEnumerable.dll"),
                "--assembly",
                j(ref, "System.Runtime.dll"),
                "--assembly",
                j(ref, "System.Collections.dll"),
                "--assembly",
                j(ref, "System.Memory.dll"),
                "--assembly",
                j(ref, "System.Buffers.dll"),
                "--assembly",
                j(ref, "System.Threading.Tasks.Extensions.dll"),
            ]
            + inc("bcl-sequences-linq")
            + outs("bcl-sequences-linq"),
            "bcl-collections-immutable": head()
            + ["--mode", "metadata", "--core-dir", ref]
            + [
                "--assembly",
                j(ref, "System.Collections.Immutable.dll"),
                "--assembly",
                j(ref, "System.Collections.dll"),
                "--assembly",
                j(ref, "System.Collections.Concurrent.dll"),
                "--assembly",
                j(ref, "System.Linq.dll"),
            ]
            + inc("bcl-collections-immutable")
            + outs("bcl-collections-immutable"),
            "bcl-async-concurrency": head()
            + ["--mode", "metadata", "--core-dir", ref]
            + [
                "--assembly",
                j(ref, "System.Runtime.dll"),
                "--assembly",
                j(ref, "System.Threading.Channels.dll"),
                "--assembly",
                j(ref, "System.Threading.Tasks.Parallel.dll"),
                "--assembly",
                j(ref, "System.Threading.Tasks.dll"),
                "--assembly",
                j(ref, "System.Threading.dll"),
            ]
            + inc("bcl-async-concurrency")
            + outs("bcl-async-concurrency"),
            "bcl-language-errors": head()
            + ["--mode", "metadata", "--core-dir", ref]
            + [
                "--assembly",
                j(ref, "System.Runtime.dll"),
                "--assembly",
                j(ref, "System.Runtime.Extensions.dll"),
                "--assembly",
                j(ref, "System.Linq.Expressions.dll"),
            ]
            + inc("bcl-language-errors")
            + outs("bcl-language-errors"),
            inventory.TYPE_LIST_NAME: head()
            + [
                "--mode",
                "metadata",
                "--core-dir",
                ref,
                "--assembly",
                j(base, "languageext.core.4.4.9", "lib", "netstandard2.0", "LanguageExt.Core.dll"),
                "--list-types",
            ],
        }
        got = {
            target.name: inventory.target_argv(target, inputs, out)
            for target in inventory.TARGETS
        }
        self.assertEqual(expected, got)

    def test_pep723_header(self) -> None:
        text = (self.repo / "eng" / "tools" / "inventory.py").read_text(encoding="utf-8")
        self.assertTrue(text.splitlines()[0].startswith("#!"))
        self.assertIn("# /// script", text)
        self.assertIn('requires-python = ">=3.12,<3.13"', text)
        self.assertIn("dependencies = []", text)

    # ------------------------------------------------------------------
    # Environment resolution
    # ------------------------------------------------------------------

    def test_required_inputs_all_present_when_staged(self) -> None:
        self.assertEqual([], inventory.missing_inputs(self.inputs()))

    def test_dotnet_root_defaults_to_home_dotnet(self) -> None:
        env = dict(self.env)
        del env["DOTNET_ROOT"]
        inputs = inventory.resolve_inputs(self.repo, env, self.baseline, self.ref)
        self.assertEqual(Path(self.env["HOME"]) / ".dotnet", inputs.dotnet_root)

    def test_funny_sharp_bins_default_to_repo_paths(self) -> None:
        env = dict(self.env)
        del env["FUNNY_SHARP_BIN"]
        del env["FUNNY_SHARP_ASPNET_BIN"]
        inputs = inventory.resolve_inputs(self.repo, env, self.baseline, self.ref)
        self.assertEqual(
            self.repo / "src" / "FunnySharp" / "bin" / "Release" / "net10.0",
            inputs.funny_sharp_bin,
        )
        self.assertEqual(
            self.repo / "src" / "FunnySharp.AspNetCore" / "bin" / "Release" / "net10.0",
            inputs.funny_sharp_aspnet_bin,
        )

    def test_roslyn_default_prefers_global_json_pin(self) -> None:
        (self.dotnet_root / "sdk" / "10.0.500" / "Roslyn" / "bincore").mkdir(
            parents=True, exist_ok=True
        )
        self.assertEqual(
            self.dotnet_root / "sdk" / "10.0.400" / "Roslyn" / "bincore",
            self.inputs().roslyn_dir,
        )

    def test_roslyn_default_falls_back_to_newest_sdk(self) -> None:
        other = self.tmp / "other-dotnet"
        (other / "sdk" / "10.0.700" / "Roslyn" / "bincore").mkdir(parents=True)
        (other / "sdk" / "10.0.800" / "Roslyn" / "bincore").mkdir(parents=True)
        env = dict(self.env, DOTNET_ROOT=str(other))
        inputs = inventory.resolve_inputs(self.repo, env, self.baseline, self.ref)
        self.assertEqual(other / "sdk" / "10.0.800" / "Roslyn" / "bincore", inputs.roslyn_dir)

    def test_roslyn_env_override_wins(self) -> None:
        override = self.tmp / "roslyn-override"
        env = dict(self.env, ROSLYN_DIR=str(override))
        inputs = inventory.resolve_inputs(self.repo, env, self.baseline, self.ref)
        self.assertEqual(override, inputs.roslyn_dir)

    def test_aspnet_framework_uses_version_sort_not_lexicographic(self) -> None:
        # "10.0.9" sorts after "10.0.11" as text; version sort must pick 10.0.11.
        self.assertEqual(
            self.dotnet_root / "shared" / "Microsoft.AspNetCore.App" / "10.0.11",
            self.inputs().aspnet_framework_dir,
        )

    def test_aspnet_framework_env_override_wins(self) -> None:
        override = self.tmp / "aspnet-override"
        env = dict(self.env, ASPNET_FRAMEWORK_DIR=str(override))
        inputs = inventory.resolve_inputs(self.repo, env, self.baseline, self.ref)
        self.assertEqual(override, inputs.aspnet_framework_dir)

    # ------------------------------------------------------------------
    # Input preflight and report-only mode
    # ------------------------------------------------------------------

    def _all_missing_env(self):
        return {
            "HOME": str(self.tmp / "home"),
            "DOTNET_ROOT": str(self.tmp / "missing-dotnet"),
            "FUNNY_SHARP_BIN": str(self.tmp / "missing-fsbin"),
            "FUNNY_SHARP_ASPNET_BIN": str(self.tmp / "missing-aspnetbin"),
            "PATH": os.environ.get("PATH", ""),
        }

    def test_missing_inputs_lists_every_path_and_exit_2(self) -> None:
        missing_baseline = self.tmp / "missing-baselines"
        missing_ref = self.tmp / "missing-ref"
        missing_fsbin = self.tmp / "missing-fsbin"
        missing_aspnetbin = self.tmp / "missing-aspnetbin"
        env = self._all_missing_env()
        out = self.tmp / "out-not-created"
        runner = FakeDotnet()
        code, stdout, stderr = self.run_cli(
            runner,
            [
                "--baseline-root",
                str(missing_baseline),
                "--ref-pack-dir",
                str(missing_ref),
                "--output-dir",
                str(out),
            ],
            env=env,
        )
        self.assertEqual(2, code)
        self.assertEqual([], runner.calls)
        self.assertFalse(out.exists())

        expected = {str(missing_baseline / rel) for rel in BASELINE_FILES}
        expected |= {str(missing_ref / rel) for rel in REF_FILES}
        expected |= {
            str(missing_ref),
            str(missing_fsbin),
            str(missing_fsbin / "FunnySharp.dll"),
            str(missing_aspnetbin / "FunnySharp.AspNetCore.dll"),
            "<DOTNET_ROOT>/sdk/<version>/Roslyn/bincore",
            "<DOTNET_ROOT>/shared/Microsoft.AspNetCore.App/<version>",
        }
        reported = set(re.findall(r"^MISSING: (.+?) \(", stdout + stderr, re.M))
        self.assertEqual(expected, reported)
        self.assertEqual(26, len(reported))
        self.assertIn("docs/next-stage/baselines.md", stderr)

    def test_check_inputs_reports_without_building_or_writing(self) -> None:
        out = self.tmp / "out-check"
        runner = FakeDotnet(fail_build=True)
        code, stdout, stderr = self.run_cli(
            runner, [*self.generation_argv(out), "--check-inputs"]
        )
        self.assertEqual(0, code)
        self.assertIn("INPUT CHECK OK: all", stdout)
        self.assertEqual("", stderr)
        self.assertEqual([], runner.calls)
        self.assertFalse(out.exists())

    def test_check_inputs_with_missing_inputs_exits_2(self) -> None:
        out = self.tmp / "out-check-missing"
        runner = FakeDotnet()
        code, stdout, stderr = self.run_cli(
            runner,
            [
                "--baseline-root",
                str(self.tmp / "missing-baselines"),
                "--ref-pack-dir",
                str(self.tmp / "missing-ref"),
                "--output-dir",
                str(out),
                "--check-inputs",
            ],
            env=self._all_missing_env(),
        )
        self.assertEqual(2, code)
        self.assertIn("MISSING:", stdout)
        self.assertIn("INPUT CHECK FAILED", stderr)
        self.assertIn("docs/next-stage/baselines.md", stderr)
        self.assertEqual([], runner.calls)
        self.assertFalse(out.exists())

    def test_check_inputs_without_positionals_uses_placeholders(self) -> None:
        runner = FakeDotnet()
        code, stdout, stderr = self.run_cli(
            runner, ["--check-inputs"], env=self._all_missing_env()
        )
        self.assertEqual(2, code)
        self.assertIn("<baseline-root>/funcky.3.6.0/lib/net10.0/Funcky.dll", stdout)
        self.assertIn("<ref-pack-dir>/System.Linq.dll", stdout)
        self.assertEqual([], runner.calls)

    # ------------------------------------------------------------------
    # Build, target aggregation, and success path
    # ------------------------------------------------------------------

    def test_build_failure_exits_immediately(self) -> None:
        out = self.tmp / "out-buildfail"
        runner = FakeDotnet(fail_build=True)
        code, stdout, stderr = self.run_cli(runner, self.generation_argv(out))
        self.assertEqual(1, code)
        self.assertIn("FAILED: inventory tool build", stderr)
        self.assertTrue(out.is_dir())
        self.assertEqual([], list(out.iterdir()))
        self.assertEqual(1, len(runner.calls))
        argv, env = runner.calls[0]
        self.assertEqual(
            [
                "dotnet",
                "build",
                str(self.repo / "eng" / "next-stage-inventory" / "api-inventory.csproj"),
                "-c",
                "Release",
                "--nologo",
            ],
            argv,
        )
        self.assertEqual(str(self.dotnet_root), env["DOTNET_ROOT"])
        self.assertTrue(env["PATH"].startswith(str(self.dotnet_root) + os.pathsep))
        self.assertEqual("1", env["DOTNET_CLI_TELEMETRY_OPTOUT"])
        self.assertNotIn("== funny-sharp-core", stdout)

    def test_target_failure_aggregates_and_reports_all_targets(self) -> None:
        out = self.tmp / "out-targetfail"
        runner = FakeDotnet(fail_targets=("funcky",))
        code, stdout, stderr = self.run_cli(runner, self.generation_argv(out))
        self.assertEqual(1, code)
        self.assertEqual(13, len(runner.calls))
        self.assertIn("FAILED: funcky", stdout)
        self.assertIn("== bcl-language-errors", stdout)
        self.assertIn("== " + inventory.TYPE_LIST_NAME, stdout)
        self.assertIn(
            f"FAILED: one or more inventory targets failed; output in {out} is incomplete",
            stderr,
        )
        self.assertNotIn("ALL DONE", stdout)

    def test_success_prints_all_done_and_normalizes_outputs(self) -> None:
        out = self.tmp / "out-success"
        runner = FakeDotnet()
        code, stdout, stderr = self.run_cli(runner, self.generation_argv(out))
        self.assertEqual(0, code)
        self.assertIn("ALL DONE", stdout)
        self.assertEqual("", stderr)
        self.assertEqual(13, len(runner.calls))

        names = sorted(path.name for path in out.iterdir())
        self.assertEqual(35, len(names))
        self.assertEqual(11, sum(1 for name in names if name.endswith(".md")))
        self.assertEqual(11, sum(1 for name in names if name.endswith(".json")))
        self.assertEqual(11, sum(1 for name in names if name.endswith(".log")))
        for name in names:
            data = (out / name).read_bytes()
            self.assertFalse(data.startswith(codecs.BOM_UTF8), name)
            self.assertNotIn(b"\r", data, name)
        log = (out / "inv-funcky.log").read_bytes()
        self.assertIn(b"wrote file\nsecond line\n", log)
        self.assertIn(b"warn\n", log)
        md = (out / "inv-funcky.md").read_bytes()
        self.assertTrue(md.startswith(b"# title\n\nType count: 1\n"))

    # ------------------------------------------------------------------
    # Output directory safety
    # ------------------------------------------------------------------

    def test_rejects_repository_root_output(self) -> None:
        runner = FakeDotnet()
        code, stdout, stderr = self.run_cli(
            runner,
            [
                "--baseline-root",
                str(self.baseline),
                "--ref-pack-dir",
                str(self.ref),
                "--output-dir",
                str(self.repo),
            ],
        )
        self.assertEqual(2, code)
        self.assertIn("repository root", stderr)
        self.assertIn("Remediation:", stderr)
        self.assertNotIn("baselines.md", stderr)
        self.assertEqual([], runner.calls)

    def test_rejects_symlink_output(self) -> None:
        real = self.tmp / "real-out"
        real.mkdir()
        link = self.tmp / "link-out"
        try:
            link.symlink_to(real, target_is_directory=True)
        except (OSError, NotImplementedError) as exc:  # pragma: no cover - platform dependent
            self.skipTest(f"cannot create symlink: {exc}")
        runner = FakeDotnet()
        for target in (link, link / "sub"):
            code, stdout, stderr = self.run_cli(
                runner,
                [
                    "--baseline-root",
                    str(self.baseline),
                    "--ref-pack-dir",
                    str(self.ref),
                    "--output-dir",
                    str(target),
                ],
            )
            self.assertEqual(2, code, target)
            self.assertIn("symlink or reparse point", stderr)
        self.assertEqual([], runner.calls)
        self.assertEqual([], list(real.iterdir()))

    def test_rejects_file_output(self) -> None:
        file_path = self.tmp / "not-a-dir"
        file_path.write_bytes(b"x")
        runner = FakeDotnet()
        code, stdout, stderr = self.run_cli(
            runner,
            [
                "--baseline-root",
                str(self.baseline),
                "--ref-pack-dir",
                str(self.ref),
                "--output-dir",
                str(file_path),
            ],
        )
        self.assertEqual(2, code)
        self.assertIn("not a directory", stderr)
        self.assertEqual([], runner.calls)

    @unittest.skipIf(
        os.name == "nt",
        "read-only directory permissions do not block writes on Windows; POSIX mode bits only",
    )
    def test_unwritable_output_dir_exits_2_without_traceback(self) -> None:
        out = self.tmp / "out-readonly"
        out.mkdir()
        out.chmod(0o500)
        self.addCleanup(out.chmod, 0o700)
        runner = FakeDotnet()
        code, stdout, stderr = self.run_cli(runner, self.generation_argv(out))
        self.assertEqual(2, code)
        self.assertIn("ERROR: cannot write inventory output", stderr)
        self.assertIn("Remediation: choose a writable output directory", stderr)
        self.assertIn(str(out), stderr)
        self.assertNotIn("Traceback", stdout + stderr)

    def test_default_output_dir_is_outside_repository(self) -> None:
        default = inventory.default_output_dir()
        self.assertEqual("funnysharp-inventory", default.name)
        self.assertFalse(str(default).startswith(str(self.repo) + os.sep))

    # ------------------------------------------------------------------
    # LF / UTF-8 normalizer
    # ------------------------------------------------------------------

    def test_normalize_bytes_strips_bom_and_crlf(self) -> None:
        self.assertEqual(
            b"a\nb\nc\n", inventory.normalize_bytes(b"\xef\xbb\xbfa\r\nb\rc\n")
        )
        self.assertEqual(b"plain\n", inventory.normalize_bytes(b"plain\n"))
        self.assertEqual(b"", inventory.normalize_bytes(b""))

    def test_normalize_file_rewrites_bytes(self) -> None:
        path = self.tmp / "fixture.txt"
        path.write_bytes(b"\xef\xbb\xbffirst\r\nsecond\r\n")
        self.assertTrue(inventory.normalize_file(path))
        self.assertEqual(b"first\nsecond\n", path.read_bytes())
        self.assertFalse(inventory.normalize_file(path))


if __name__ == "__main__":
    unittest.main()
