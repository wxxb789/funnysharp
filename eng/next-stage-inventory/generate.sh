#!/usr/bin/env bash
# Regenerates the Goal 14 next-stage evidence inventories.
#
# Usage:
#   generate.sh <baseline-root> <ref-pack-dir> <output-dir>
#
# <baseline-root> must contain one directory per package named after the .nupkg file
# without its extension, as produced by extracting each pinned package into its own
# directory, for example:
#
#   <baseline-root>/funcky.3.6.0/lib/net10.0/Funcky.dll
#   <baseline-root>/csharpfunctionalextensions.3.7.0/lib/net8.0/CSharpFunctionalExtensions.dll
#   <baseline-root>/fsharp.core.10.1.401/lib/netstandard2.1/FSharp.Core.dll
#   <baseline-root>/languageext.core.4.4.9/lib/netstandard2.0/LanguageExt.Core.dll
#
# <ref-pack-dir> is the Microsoft.NETCore.App.Ref ref/net10.0 directory of the pinned SDK.
# <output-dir> receives every generated .md/.json inventory plus the language-ext type list.
#
# The FunnySharp inventories are generated from the repository's Release build outputs.
# Override their location with:
#   FUNNY_SHARP_BIN          (default <repo>/src/FunnySharp/bin/Release/net10.0)
#   FUNNY_SHARP_ASPNET_BIN   (default <repo>/src/FunnySharp.AspNetCore/bin/Release/net10.0)
#   DOTNET_ROOT              (default $HOME/.dotnet; used to locate the ASP.NET shared framework)
#   ASPNET_FRAMEWORK_DIR     (default the newest $DOTNET_ROOT/shared/Microsoft.AspNetCore.App/*)
#
# Any failed target is reported and the script exits non-zero; it never claims success
# for a partial evidence set.
set -uo pipefail

BASELINE_ROOT=${1:?usage: generate.sh <baseline-root> <ref-pack-dir> <output-dir>}
REF=${2:?usage: generate.sh <baseline-root> <ref-pack-dir> <output-dir>}
OUT=${3:?usage: generate.sh <baseline-root> <ref-pack-dir> <output-dir>}

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1

TOOL=$(cd "$(dirname "$0")" && pwd)
REPO_ROOT=$(cd "$TOOL/../.." && pwd)
ROSLYN="${ROSLYN_DIR:-$DOTNET_ROOT/sdk/10.0.400/Roslyn/bincore}"
FUNNY_SHARP_BIN="${FUNNY_SHARP_BIN:-$REPO_ROOT/src/FunnySharp/bin/Release/net10.0}"
FUNNY_SHARP_ASPNET_BIN="${FUNNY_SHARP_ASPNET_BIN:-$REPO_ROOT/src/FunnySharp.AspNetCore/bin/Release/net10.0}"
ASPNET_FRAMEWORK_DIR="${ASPNET_FRAMEWORK_DIR:-$(ls -d "$DOTNET_ROOT"/shared/Microsoft.AspNetCore.App/* 2>/dev/null | sort -V | tail -1)}"

FAILED=0

mkdir -p "$OUT"
dotnet build "$TOOL/api-inventory.csproj" -c Release --nologo >/dev/null || {
  echo "FAILED: inventory tool build" >&2
  exit 1
}

run() {
  local name=$1; local title=$2; shift 2
  echo "== $name"
  dotnet run -c Release --no-build --project "$TOOL/api-inventory.csproj" -- "$@" \
    --out-md "$OUT/inv-$name.md" --out-json "$OUT/inv-$name.json" \
    --title "$title" \
    > "$OUT/inv-$name.log" 2>&1 || { echo "FAILED: $name"; tail -5 "$OUT/inv-$name.log"; FAILED=1; return 1; }
  tail -2 "$OUT/inv-$name.log"
}

run funny-sharp-core "FunnySharp core public API (commit 4dbebd9, 0.1.0)" \
  --assembly "$FUNNY_SHARP_BIN/FunnySharp.dll"

run funny-sharp-aspnetcore "FunnySharp.AspNetCore public API (commit 4dbebd9, 0.1.0)" \
  --assembly "$FUNNY_SHARP_ASPNET_BIN/FunnySharp.AspNetCore.dll" \
  --resolve-dir "$FUNNY_SHARP_BIN" \
  --resolve-dir "$ASPNET_FRAMEWORK_DIR"

run funcky "Public API inventory: funcky" \
  --assembly "$BASELINE_ROOT/funcky.3.6.0/lib/net10.0/Funcky.dll"

run funcky-analyzers "Funcky built-in analyzers (metadata mode)" \
  --mode metadata --core-dir "$REF" --resolve-dir "$ROSLYN" \
  --assembly "$BASELINE_ROOT/funcky.3.6.0/analyzers/dotnet/cs/Funcky.BuiltinAnalyzers.dll"

run csharpfunctionalextensions "Public API inventory: csharpfunctionalextensions" \
  --assembly "$BASELINE_ROOT/csharpfunctionalextensions.3.7.0/lib/net8.0/CSharpFunctionalExtensions.dll"

run fsharp-core "Public API inventory: fsharp-core" \
  --assembly "$BASELINE_ROOT/fsharp.core.10.1.401/lib/netstandard2.1/FSharp.Core.dll" \
  --include '^Microsoft\.FSharp\.(Core\.(FSharpOption|FSharpValueOption|FSharpResult|FSharpChoice|FSharpFunc|FSharpType|FSharpValue|Unit|OptionModule|ValueOptionModule|ResultModule|Choice|Nullable|LanguagePrimitives|Operators|ExtraTopLevelOperators|FuncConvert|OptimizedClosures|Lazy|MatchFailureException|Printf)|Collections\.(SeqModule|ListModule|ArrayModule|SetModule|MapModule|FSharpList|FSharpSet|FSharpMap|ResizeArray|Seq)|Control\.(FSharpAsync|FSharpMailboxProcessor|TaskBuilder|EventModule|FSharpEvent|IEvent|LazyExtensions))'

run language-ext "Public API inventory: language-ext" \
  --mode metadata --core-dir "$REF" \
  --assembly "$BASELINE_ROOT/languageext.core.4.4.9/lib/netstandard2.0/LanguageExt.Core.dll" \
  --include '^LanguageExt\.[A-Za-z]+$'

run bcl-sequences-linq "Public API inventory: bcl-sequences-linq" \
  --mode metadata --core-dir "$REF" \
  --assembly "$REF/System.Linq.dll" --assembly "$REF/System.Linq.AsyncEnumerable.dll" \
  --assembly "$REF/System.Runtime.dll" --assembly "$REF/System.Collections.dll" \
  --assembly "$REF/System.Memory.dll" --assembly "$REF/System.Buffers.dll" \
  --assembly "$REF/System.Threading.Tasks.Extensions.dll" \
  --include '^System\.Linq\.(Enumerable|AsyncEnumerable|Lookup|IGrouping|IOrderedEnumerable|OrderedEnumerable)' \
  --include '^System\.Collections\.Generic\.(IEnumerable|IAsyncEnumerable|IReadOnlyList|IReadOnlyCollection|IReadOnlyDictionary|IList|IDictionary|List|Dictionary|HashSet|SortedSet|SortedDictionary|KeyValuePair|IEqualityComparer|IEnumerator|IAsyncEnumerator)' \
  --include '^System\.Span|^System\.ReadOnlySpan|^System\.Memory$|^System\.ReadOnlyMemory|^System\.MemoryExtensions|^System\.Array$|^System\.ArraySegment|^System\.Buffers\.'

run bcl-collections-immutable "Public API inventory: bcl-collections-immutable" \
  --mode metadata --core-dir "$REF" \
  --assembly "$REF/System.Collections.Immutable.dll" --assembly "$REF/System.Collections.dll" \
  --assembly "$REF/System.Collections.Concurrent.dll" --assembly "$REF/System.Linq.dll" \
  --include '^System\.Collections\.Immutable\.|^System\.Collections\.Frozen\.|^System\.Collections\.Concurrent\.|^System\.Collections\.ObjectModel\.'

run bcl-async-concurrency "Public API inventory: bcl-async-concurrency" \
  --mode metadata --core-dir "$REF" \
  --assembly "$REF/System.Runtime.dll" --assembly "$REF/System.Threading.Channels.dll" \
  --assembly "$REF/System.Threading.Tasks.Parallel.dll" --assembly "$REF/System.Threading.Tasks.dll" \
  --assembly "$REF/System.Threading.dll" \
  --include '^System\.Threading\.Tasks\.(Task|ValueTask|TaskCompletionSource|TaskFactory|TaskScheduler|Parallel|ParallelOptions|TaskCreationOptions|TaskContinuationOptions|TaskStatus|TaskCanceledException|ValueTaskSourceStatus|IValueTaskSource|TaskExtensions|TaskAsyncEnumerableExtensions|ParallelEnumerable)' \
  --include '^System\.Threading\.(Channels\.|CancellationToken|CancellationTokenSource|CancellationTokenRegistration|TimeProvider|ITimer|Timer|PeriodicTimer|Lock|Interlocked|Volatile|LazyInitializer|Timeout|WaitHandle|ManualResetEventSlim|SemaphoreSlim|CountdownEvent|Barrier|ReaderWriterLockSlim)' \
  --include '^System\.(IAsyncDisposable|IDisposable|IAsyncEnumerable|TimeProvider|TimeoutException|OperationCanceledException)' \
  --include '^System\.Runtime\.CompilerServices\.(AsyncTaskMethodBuilder|AsyncValueTaskMethodBuilder|ConfiguredTaskAwaitable|ConfiguredValueTaskAwaitable|IAsyncStateMachine|AsyncIteratorMethodBuilder|TaskAwaiter|ValueTaskAwaiter|PoolingAsyncValueTaskMethodBuilder|EnumeratorCancellationAttribute|AsyncMethodBuilderAttribute|INotifyCompletion|ICriticalNotifyCompletion)'

run bcl-language-errors "Public API inventory: bcl-language-errors" \
  --mode metadata --core-dir "$REF" \
  --assembly "$REF/System.Runtime.dll" --assembly "$REF/System.Runtime.Extensions.dll" \
  --assembly "$REF/System.Linq.Expressions.dll" \
  --include '^System\.(Nullable|Nullable`1|Func|Action|Predicate|Comparison|Converter|Lazy|Tuple|ValueTuple|Exception|AggregateException|SystemException|InvalidOperationException|ArgumentNullException|ArgumentException|ArgumentOutOfRangeException|OperationCanceledException|TimeoutException|ObjectDisposedException|NotSupportedException|NotImplementedException|FormatException|Environment|Math|Convert|String|StringComparison|DateTime|DateTimeOffset|TimeSpan|Guid|Uri|Random|Version|IEquatable|IComparable|IFormattable|ISpanFormattable|Comparison`1)' \
  --include '^System\.Diagnostics\.CodeAnalysis\.(MaybeNull|NotNull|AllowNull|DisallowNull|MaybeNullWhen|NotNullWhen|NotNullIfNotNull|MemberNotNull|DoesNotReturn|DoesNotReturnIf|SetsRequiredMembers|StringSyntax)' \
  --include '^System\.Runtime\.CompilerServices\.(NullableAttribute|NullableContextAttribute|IsReadOnlyAttribute|IsByRefLikeAttribute|RequiredMemberAttribute|CompilerFeatureRequiredAttribute)'

echo "== types-languageext-4.4.9"
dotnet run -c Release --no-build --project "$TOOL/api-inventory.csproj" -- \
  --mode metadata --core-dir "$REF" \
  --assembly "$BASELINE_ROOT/languageext.core.4.4.9/lib/netstandard2.0/LanguageExt.Core.dll" \
  --list-types > "$OUT/types-languageext-4.4.9.txt" 2> "$OUT/types-languageext-4.4.9.err" \
  || { echo "FAILED: types-languageext-4.4.9"; tail -5 "$OUT/types-languageext-4.4.9.err"; FAILED=1; }

if [ "$FAILED" -ne 0 ]; then
  echo "FAILED: one or more inventory targets failed; output in $OUT is incomplete" >&2
  exit 1
fi

echo "ALL DONE"
