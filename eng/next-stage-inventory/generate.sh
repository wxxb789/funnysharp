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
# Pin hashes and acquisition commands are in docs/next-stage/baselines.md.
set -uo pipefail

BASELINE_ROOT=${1:?usage: generate.sh <baseline-root> <ref-pack-dir> <output-dir>}
REF=${2:?usage: generate.sh <baseline-root> <ref-pack-dir> <output-dir>}
OUT=${3:?usage: generate.sh <baseline-root> <ref-pack-dir> <output-dir>}

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1

TOOL=$(cd "$(dirname "$0")" && pwd)
ROSLYN="${ROSLYN_DIR:-$DOTNET_ROOT/sdk/10.0.400/Roslyn/bincore}"

mkdir -p "$OUT"
dotnet build "$TOOL/api-inventory.csproj" -c Release --nologo >/dev/null || exit 1

run() {
  local name=$1; shift
  echo "== $name"
  dotnet run -c Release --no-build --project "$TOOL/api-inventory.csproj" -- "$@" \
    --out-md "$OUT/inv-$name.md" --out-json "$OUT/inv-$name.json" \
    --title "Public API inventory: $name" \
    > "$OUT/inv-$name.log" 2>&1 || { echo "FAILED: $name"; tail -5 "$OUT/inv-$name.log"; return 1; }
  tail -2 "$OUT/inv-$name.log"
}

run funcky \
  --assembly "$BASELINE_ROOT/funcky.3.6.0/lib/net10.0/Funcky.dll"

run funcky-analyzers \
  --mode metadata --core-dir "$REF" --resolve-dir "$ROSLYN" \
  --assembly "$BASELINE_ROOT/funcky.3.6.0/analyzers/dotnet/cs/Funcky.BuiltinAnalyzers.dll"

run csharpfunctionalextensions \
  --assembly "$BASELINE_ROOT/csharpfunctionalextensions.3.7.0/lib/net8.0/CSharpFunctionalExtensions.dll"

run fsharp-core \
  --assembly "$BASELINE_ROOT/fsharp.core.10.1.401/lib/netstandard2.1/FSharp.Core.dll" \
  --include '^Microsoft\.FSharp\.(Core\.(FSharpOption|FSharpValueOption|FSharpResult|FSharpChoice|FSharpFunc|FSharpType|FSharpValue|Unit|OptionModule|ValueOptionModule|ResultModule|Choice|Nullable|LanguagePrimitives|Operators|ExtraTopLevelOperators|FuncConvert|OptimizedClosures|Lazy|MatchFailureException|Printf)|Collections\.(SeqModule|ListModule|ArrayModule|SetModule|MapModule|FSharpList|FSharpSet|FSharpMap|ResizeArray|Seq)|Control\.(FSharpAsync|FSharpMailboxProcessor|TaskBuilder|EventModule|FSharpEvent|IEvent|LazyExtensions))'

run language-ext \
  --mode metadata --core-dir "$REF" \
  --assembly "$BASELINE_ROOT/languageext.core.4.4.9/lib/netstandard2.0/LanguageExt.Core.dll" \
  --include '^LanguageExt\.[A-Za-z]+$'

run bcl-sequences-linq \
  --mode metadata --core-dir "$REF" \
  --assembly "$REF/System.Linq.dll" --assembly "$REF/System.Linq.AsyncEnumerable.dll" \
  --assembly "$REF/System.Runtime.dll" --assembly "$REF/System.Collections.dll" \
  --assembly "$REF/System.Memory.dll" --assembly "$REF/System.Buffers.dll" \
  --assembly "$REF/System.Threading.Tasks.Extensions.dll" \
  --include '^System\.Linq\.(Enumerable|AsyncEnumerable|Lookup|IGrouping|IOrderedEnumerable|OrderedEnumerable)' \
  --include '^System\.Collections\.Generic\.(IEnumerable|IAsyncEnumerable|IReadOnlyList|IReadOnlyCollection|IReadOnlyDictionary|IList|IDictionary|List|Dictionary|HashSet|SortedSet|SortedDictionary|KeyValuePair|IEqualityComparer|IEnumerator|IAsyncEnumerator)' \
  --include '^System\.Span|^System\.ReadOnlySpan|^System\.Memory$|^System\.ReadOnlyMemory|^System\.MemoryExtensions|^System\.Array$|^System\.ArraySegment|^System\.Buffers\.'

run bcl-collections-immutable \
  --mode metadata --core-dir "$REF" \
  --assembly "$REF/System.Collections.Immutable.dll" --assembly "$REF/System.Collections.dll" \
  --assembly "$REF/System.Collections.Concurrent.dll" --assembly "$REF/System.Linq.dll" \
  --include '^System\.Collections\.Immutable\.|^System\.Collections\.Frozen\.|^System\.Collections\.Concurrent\.|^System\.Collections\.ObjectModel\.'

run bcl-async-concurrency \
  --mode metadata --core-dir "$REF" \
  --assembly "$REF/System.Runtime.dll" --assembly "$REF/System.Threading.Channels.dll" \
  --assembly "$REF/System.Threading.Tasks.Parallel.dll" --assembly "$REF/System.Threading.Tasks.dll" \
  --assembly "$REF/System.Threading.dll" \
  --include '^System\.Threading\.Tasks\.(Task|ValueTask|TaskCompletionSource|TaskFactory|TaskScheduler|Parallel|ParallelOptions|TaskCreationOptions|TaskContinuationOptions|TaskStatus|TaskCanceledException|ValueTaskSourceStatus|IValueTaskSource|TaskExtensions|TaskAsyncEnumerableExtensions|ParallelEnumerable)' \
  --include '^System\.Threading\.(Channels\.|CancellationToken|CancellationTokenSource|CancellationTokenRegistration|TimeProvider|ITimer|Timer|PeriodicTimer|Lock|Interlocked|Volatile|LazyInitializer|Timeout|WaitHandle|ManualResetEventSlim|SemaphoreSlim|CountdownEvent|Barrier|ReaderWriterLockSlim)' \
  --include '^System\.(IAsyncDisposable|IDisposable|IAsyncEnumerable|TimeProvider|TimeoutException|OperationCanceledException)' \
  --include '^System\.Runtime\.CompilerServices\.(AsyncTaskMethodBuilder|AsyncValueTaskMethodBuilder|ConfiguredTaskAwaitable|ConfiguredValueTaskAwaitable|IAsyncStateMachine|AsyncIteratorMethodBuilder|TaskAwaiter|ValueTaskAwaiter|PoolingAsyncValueTaskMethodBuilder|EnumeratorCancellationAttribute|AsyncMethodBuilderAttribute|INotifyCompletion|ICriticalNotifyCompletion)'

run bcl-language-errors \
  --mode metadata --core-dir "$REF" \
  --assembly "$REF/System.Runtime.dll" --assembly "$REF/System.Runtime.Extensions.dll" \
  --assembly "$REF/System.Linq.Expressions.dll" \
  --include '^System\.(Nullable|Nullable`1|Func|Action|Predicate|Comparison|Converter|Lazy|Tuple|ValueTuple|Exception|AggregateException|SystemException|InvalidOperationException|ArgumentNullException|ArgumentException|ArgumentOutOfRangeException|OperationCanceledException|TimeoutException|ObjectDisposedException|NotSupportedException|NotImplementedException|FormatException|Environment|Math|Convert|String|StringComparison|DateTime|DateTimeOffset|TimeSpan|Guid|Uri|Random|Version|IEquatable|IComparable|IFormattable|ISpanFormattable|Comparison`1)' \
  --include '^System\.Diagnostics\.CodeAnalysis\.(MaybeNull|NotNull|AllowNull|DisallowNull|MaybeNullWhen|NotNullWhen|NotNullIfNotNull|MemberNotNull|DoesNotReturn|DoesNotReturnIf|SetsRequiredMembers|StringSyntax)' \
  --include '^System\.Runtime\.CompilerServices\.(NullableAttribute|NullableContextAttribute|IsReadOnlyAttribute|IsByRefLikeAttribute|RequiredMemberAttribute|CompilerFeatureRequiredAttribute)'

echo "ALL DONE"
