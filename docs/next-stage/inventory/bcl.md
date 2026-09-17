# BCL capability inventory for net10.0 (Goal 14)

Scope: what a `net10.0` C# developer gets from the pinned .NET/BCL baseline,
organized by the Goal 14 family taxonomy F1–F11. This file summarizes and indexes
the machine-generated dumps; it does not replace them. Per-family analysis and the
"BCL coverage verdict" live in
[`../analysis/baseline-bcl.md`](../analysis/baseline-bcl.md).

## Provenance

| Item | Pin | Evidence |
| --- | --- | --- |
| SDK | .NET SDK **10.0.400** (`dotnet --version`) | survey host; matches `docs/next-stage/baselines.md:16` |
| Host runtime | **10.0.11** (`Microsoft.NETCore.App 10.0.11`) | `dotnet --list-runtimes` |
| Base reference pack | `$HOME/.dotnet/packs/Microsoft.NETCore.App.Ref/10.0.11/ref/net10.0` | dump inputs |
| ASP.NET Core reference pack | `$HOME/.dotnet/packs/Microsoft.AspNetCore.App.Ref/10.0.11/ref/net10.0` | `inv-aspnetcore-http.*` only (F10) |
| Repo target | `net10.0` | `Directory.Build.props:3` |
| FunnySharp pin | commit `4dbebd94b7b58648632112b7ca47c39cc517f153`, version 0.1.0 | `inv-funny-sharp-core.*` |

### How the dumps were generated

Tool: `eng/next-stage-inventory/` (the repo copy of the reflection/metadata dump
tool), `--mode metadata` against reference assemblies. Mode semantics are in
`docs/next-stage/baselines.md:46-51`: metadata mode is required for reference
assemblies, does not load code, and does not report nullability annotations.

Canonical regeneration: `eng/next-stage-inventory/generate.sh <baseline-root>
<ref-pack-dir> <output-dir>`. The BCL runs are:

- `generate.sh:63-71` → `inv-bcl-sequences-linq`
- `generate.sh:73-77` → `inv-bcl-collections-immutable`
- `generate.sh:79-87` → `inv-bcl-async-concurrency`
- `generate.sh:89-96` → `inv-bcl-language-errors`

The survey run executed the equivalent commands from
`/tmp/opencode/tools/generate-inventories.sh` (§44–76) with
`REF=$HOME/.dotnet/packs/Microsoft.NETCore.App.Ref/10.0.11/ref/net10.0`; the
committed copies in `inventory/generated/` are byte-identical to the survey outputs.

### Committed dumps

| Dump (`inventory/generated/`) | Inputs | Types | Members | Operators | `[ext]` | SHA256 (`.md`) |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| `inv-bcl-sequences-linq.md/.json` | `System.Linq`, `System.Linq.AsyncEnumerable`, `System.Runtime`, `System.Collections`, `System.Memory`, `System.Buffers`, `System.Threading.Tasks.Extensions` | 73 | 1502 | 17 | 680 | `1f687d5ed1d424d6c3630a83d40546228b0cf92ea9a58d1cf82b898c11972b0c` |
| `inv-bcl-collections-immutable.md/.json` | `System.Collections.Immutable`, `System.Collections`, `System.Collections.Concurrent`, `System.Linq` | 55 | 843 | 4 | 52 | `05227d830b133f3bb1f809ab3b2a8e1f89dbeeff3fd1abe7a473d3fa4dcf0fd3` |
| `inv-bcl-async-concurrency.md/.json` | `System.Runtime`, `System.Threading.Channels`, `System.Threading.Tasks.Parallel`, `System.Threading.Tasks`, `System.Threading` | 86 | 919 | 12 | 8 | `980781048cb1b4384050711ea93a1ca0e75ed5c67a2cf4994a984f351ac27431` |
| `inv-bcl-language-errors.md/.json` | `System.Runtime`, `System.Runtime.Extensions`, `System.Linq.Expressions` | 127 | 1956 | 52 | 70 | `04cbdb5d88e1036c22a52fbdfaf13322a4bd0da5375c2eedf8eeb2e7c0e40022` |
| `inv-bcl-supplement.md/.json` | `System.Runtime`, `System.Threading`, `System.ComponentModel`, `System.Collections`, `System.Text.Json` | 27 | 359 | 0 | 25 | `353000c9874a27a0f313d39b2c5cba3cac2e2a437c57bc56a0401baf7a85724d` |
| `inv-bcl-supplement-f2f3f11.md/.json` | `System.ComponentModel.Annotations`, `System.Runtime`, `System.Linq` | 10 | 104 | 0 | 0 | `b789d4d0ce4a8035d586751d0b536d1bcda72b0b896ba178ed17b920b9688e9d` |
| `inv-bcl-sse.md/.json` | `System.Net.ServerSentEvents` | 5 | 18 | 0 | 0 | `bcb4da4dbdc22433183a66004b4aeb1d420f55efeb89f36c38c081575891e93b` |
| `inv-aspnetcore-http.md/.json` | `Microsoft.AspNetCore.Http.Abstractions`, `...Http.Results`, `...Http` (framework pack 10.0.11) | 11 | 223 | 0 | 0 | `721c95a7a2594b69ab54c1023ef478dfc130eb207aeed481ed965a93af1b1dc1` |

The four `inv-bcl-*` rows are the pinned core dumps named in the brief; the four
supplementary rows were generated during this survey with the same tool, mode, and
ref pack, and are retained because this inventory and the analysis cite them (same
practice as the language-ext supplementary dumps documented in
`inventory/generated/README.md:23-27`). `inv-aspnetcore-http.*` uses the ASP.NET
Core framework reference pack, not the base pack; it is marked as such above.

Member documentation below comes from the ref-pack XML docs
(`Microsoft.NETCore.App.Ref/10.0.11/ref/net10.0/*.xml`), probed by member ID;
Microsoft Learn pages are cited with an explicit `?view=net-10.0` or a pinned raw
release-note URL.

### Reading the dumps

- A type line is `### <full name> (<kind>)`; member lines are the public/protected
  surface as printed signatures. Operator members (`kind: "operator"` in JSON) are
  listed like methods and counted in the Members column above; the canonical BCL
  dumps include them, while the four supplementary dumps were generated before
  operator support and have none.
- Enum members are printed as `<enum-value>` without names in JSON; enum names below
  come from the ref-pack XML docs.
- `[ext]` marks an extension method.
- Metadata signatures show `System.Nullable<T>` as `T?` only where the compiler
  encoded it in metadata; absence of `?` in the dumps is not evidence of
  non-nullability (metadata mode does not decode annotations).
- `System.Linq.AsyncEnumerable.dll` and `System.Net.ServerSentEvents.dll` are
  present in the 10.0.11 base ref pack, so both are in-box for `net10.0` — no NuGet
  package reference is needed. (When each was introduced is not established in this
  survey; see "Not examined".)

---

## F1 — Absence: NRT, `Nullable<T>`, Try-pattern, OrDefault, nullability attributes

BCL coverage is broad. The idiomatic C# absence carrier is the nullable reference
annotation / `Nullable<T>` pair, and every standard lookup exposes a Try method.

### Nullable value type

`System.Nullable<T>` (`inv-bcl-language-errors.md`): `HasValue`, `Value`,
`GetValueOrDefault()`, `GetValueOrDefault(T defaultValue)`, `Equals`, `GetHashCode`,
`ToString`. `System.Nullable` (static): `Compare<T>`, `Equals<T>`,
`GetUnderlyingType`, `GetValueRefOrDefaultRef`. Language behavior on top of the
type: `T?` shorthand, lifted operators/conversions, `??`, `??=`, `?.`, `is null`,
`is not null`, pattern `x is T v`, `!.` suppression, `[MaybeNull]`-driven flow
analysis. This is a language+BCL combination, not a library feature.

### Nullable reference types (NRT) and flow analysis

NRT is a compiler analysis. The metadata that make it work across assemblies are in
the dump: `System.Runtime.CompilerServices.NullableAttribute`,
`NullableContextAttribute`, `RequiredMemberAttribute`, `CompilerFeatureRequiredAttribute`,
`IsReadOnlyAttribute`, `IsByRefLikeAttribute`. The repo enables `<Nullable>enable</Nullable>`
(`Directory.Build.props:5`) and `TreatWarningsAsErrors` (`Directory.Build.props:6`),
so FunnySharp's own NRT surface is compiler-checked.

### `System.Diagnostics.CodeAnalysis` contract attributes (13 in dump)

`AllowNull`, `DisallowNull`, `MaybeNull`, `NotNull`, `MaybeNullWhen`,
`NotNullWhen`, `NotNullIfNotNull`, `MemberNotNull`, `MemberNotNullWhen`,
`DoesNotReturn`, `DoesNotReturnIf`, `SetsRequiredMembers`, `StringSyntax`.
Semantics that matter for API design:

| Attribute | Contract |
| --- | --- |
| `[NotNullWhen(true)] out T value` | flow state after `TryX(...) == true` matches non-null `value` |
| `[MaybeNullWhen(false)] out T value` | `false` branch may be `default` even for non-nullable `T` |
| `[NotNullIfNotNull("input")]` | return non-null iff the named parameter is non-null |
| `[MemberNotNull(nameof(_field))]` | instance members initialized by this method |
| `[DoesNotReturn]`, `[DoesNotReturnIf(bool)]` | unreachable-after / conditionally terminal |
| `[StringSyntax(StringSyntaxAttribute.Json)]` | editor tooling for string arguments; 16 predefined syntax constants (`inv-bcl-language-errors.md`, `StringSyntaxAttribute`) |

### Try-pattern

The framework convention is `bool TryX(..., out T value)` with a
`[MaybeNullWhen(false)]` or `[NotNullWhen(true)]` annotation. Inventory by area
(names are in the dumps):

- parse/convert: `T.TryParse` via `System.IParsable<T>` (`System.Runtime.xml`),
  `System.Convert` Try/From methods (`Convert`, 331 members in
  `inv-bcl-language-errors.md`);
- dictionary/lookup: `Dictionary<TKey,TValue>.TryGetValue`, `TryAdd`,
  `IDictionary<TKey,TValue>` extension `Remove(key, out value)`,
  `CollectionExtensions.TryAdd`, `ConcurrentDictionary.TryGetValue/TryAdd/TryRemove`;
- collections: `ConcurrentQueue<T>.TryDequeue/TryPeek`, `ImmutableDictionary.TryGetValue`,
  `FrozenDictionary.TryGetValue`;
- channels: `ChannelReader<T>.TryRead/TryPeek`, `ChannelWriter<T>.TryWrite/TryComplete`
  (`inv-bcl-async-concurrency.md`);
- LINQ: `Enumerable.TryGetNonEnumeratedCount` (`inv-bcl-sequences-linq.md`);
- tasks: `TaskCompletionSource.TrySetResult/TrySetException/TrySetCanceled`.

### `...OrDefault` and dictionary default access

Presence and exact failure semantics (from `System.Linq.xml` docs):

| Member | Empty sequence | >1 match | Out of range |
| --- | --- | --- | --- |
| `First`, `Single`, `Last` | `InvalidOperationException` | `Single` throws `InvalidOperationException` | — |
| `FirstOrDefault`, `LastOrDefault`, `SingleOrDefault` | default value | `SingleOrDefault` throws `InvalidOperationException` | — |
| `ElementAt`, `ElementAtOrDefault` (incl. `Index`) | — | — | `ElementAt` throws `ArgumentOutOfRangeException`; `...OrDefault` returns default |
| `CollectionExtensions.GetValueOrDefault(dict, key)` / `(dict, key, defaultValue)` | returns `default` / supplied default; no `KeyNotFoundException` | — | — |

`GetValueOrDefault` is in `System.Collections.dll` and is the no-exception
dictionary read (`inv-bcl-supplement.md`). `Dictionary`'s indexer and
`IDictionary`'s `Remove` keep throwing/failing semantics.

### Bridges the BCL already has

`Nullable<T>` ↔ reference conversion via boxing; `Dictionary` value-or-default;
`KeyValuePair<TKey,TValue>.Deconstruct` and `TupleExtensions.Deconstruct`
(`inv-bcl-language-errors.md`, 63 members). There is **no `Option<T>` type, no
`Try` delegate, no `Maybe`** in the base ref pack.

---

## F2 — Fail-fast value outcomes: exceptions, Try conventions, no `IResult`

### Exception hierarchy (all present in `inv-bcl-language-errors.md`)

`System.Exception` (Message, InnerException, StackTrace, Data, HResult, HelpLink,
Source, TargetSite, GetBaseException, ToString), `SystemException`,
`ArgumentException`, `ArgumentNullException`, `ArgumentOutOfRangeException`,
`InvalidOperationException`, `NotSupportedException`, `NotImplementedException`,
`FormatException`, `ObjectDisposedException`, `OperationCanceledException`,
`TimeoutException`, `AggregateException` (14 members), plus derived
`TaskCanceledException` (`: OperationCanceledException`) and `LockRecursionException`,
`BarrierPostPhaseException`, `WaitHandleCannotBeOpenedException`, `TaskSchedulerException`.

Infrastructure relevant to outcome types:

- `System.Runtime.ExceptionServices.ExceptionDispatchInfo` (`inv-bcl-supplement-f2f3f11.md`):
  `Capture`, `Throw`, `SourceException`, static `Throw(Exception)`,
  `SetCurrentStackTrace` — the supported way to rethrow with the original stack
  preserved inside a wrapper's `Throw()`.
- `[DoesNotReturn]` / `[DoesNotReturnIf]` for terminal members.
- `AggregateException.Flatten()` / `Handle(Func<Exception,bool>)` for task
  multi-failure inspection.

### Fail-fast conventions

- Return `bool` + `out` for expected absence/failure (F1 table).
- Throw for contract violations: `ArgumentNullException.ThrowIfNull(...)`
  (`ArgumentNullException` 6 members), `ArgumentException.ThrowIfNullOrEmpty`,
  `ObjectDisposedException.ThrowIf`, etc.
- Cancellation is an **exception**, not a value: `OperationCanceledException`
  propagates; helpers convert to task cancellation (`Task.FromCanceled`,
  `TaskCompletionSource.TrySetCanceled`).
- There is **no `IResult` in the base BCL**. `Microsoft.AspNetCore.Http.IResult` is
  a framework-reference type (F10). "IResult-less error handling" in the base BCL
  means: the only outcome channels are return values, `bool`+`out`, and exceptions.

---

## F3 — Accumulation: no generic accumulation type; DataAnnotations is the only collector

Finding: the base ref pack contains **no `Validation<T, TError>`, no
`ValidationResult<T>` generic, and no applicative composition operator**. The
closest BCL capability is data-annotations validation:

- `System.ComponentModel.DataAnnotations.Validator`
  (`inv-bcl-supplement-f2f3f11.md`): `TryValidateObject(instance, context,
  ICollection<ValidationResult> results[, bool validateAllProperties])`,
  `TryValidateProperty`, `TryValidateValue`, plus throwing `ValidateObject` variants.
  The `ICollection<ValidationResult>` parameter is the accumulation channel; the
  return value is a bool.
- `ValidationResult`: `ErrorMessage`, `MemberNames` (path-like member names),
  `static ValidationResult Success`.
- `IValidatableObject.Validate(ValidationContext)` returns
  `IEnumerable<ValidationResult>` — a type-level hook to add multiple failures.
- `ValidationAttribute.GetValidationResult` / `IsValid`.
- `ValidationException` carries one `ValidationResult`, one attribute, and the value.
- `AggregateException` accumulates task failures only (F2).

What is missing relative to applicative accumulation: a typed error parameter, a
value-or-errors carrier, composition that merges error lists, and compilation that
forces callers to handle the invalid branch. `Validator` runs over object graphs via
reflection and returns untyped results (`string` messages + member names), so it is
a list collector, not an applicative algebra. The ASP.NET Core Minimal API surface
adds `HttpValidationProblemDetails.Errors` (`IDictionary<string,string[]>`) and
`AddValidation`/`DisableValidation` (F10), but those live in the framework
reference.

---

## F4 — Function grammar: delegates, method groups, `Lazy`

Present in `inv-bcl-language-errors.md`:

| Type | Arity | Notes |
| --- | --- | --- |
| `System.Action` … `System.Action<T1..T16>` | 0–16 | void return; covariance none |
| `System.Func<TResult>` … `Func<T1..T16,TResult>` | 0–16 in, 1 out | `Func<in T, out TResult>` variance |
| `Predicate<T>` | 1 | `bool`-returning; used by `Array.Find*`, `List<T>.Find*` |
| `Comparison<T>` | 2 | sort delegate for `List<T>.Sort`, `Array.Sort` |
| `Converter<TInput,TOutput>` | 1 | `Array.ConvertAll` |
| `System.Lazy<T>` | — | `Value`, `IsValueCreated`; ctors `(value)`, `(Func<T>)`, `(bool isThreadSafe)`, `(LazyThreadSafetyMode)`; default mode `ExecutionAndPublication` |
| `System.Lazy<T,TMetadata>` | — | `Metadata` + `Value`; used by DI/factories |
| `System.Threading.LazyInitializer` | 5 | lock-free `EnsureInitialized` |
| `System.Tuple` / `ValueTuple` | 1–8/1–7+rest | `TupleExtensions` converts `ValueTuple` ↔ `Tuple` and deconstructs |

Language-level grammar that has no BCL type but is the honest baseline:
method-group → delegate conversion, lambdas, static lambdas (compiler-cached),
local functions, and expression trees. `System.Linq.Expressions.dll` was an input
assembly to the language-errors dump, but no expression-tree type matched that
dump's type filters, so its surface is not inventoried here. There is
**no `Pipe`, `Compose`, `Curry`, `Partial`, `Flip`, `Tap`** in the BCL. Fluent
chaining via extension methods and LINQ method syntax are the ordinary-C#
equivalents.

```csharp
// ordinary C# "pipe"
var name = Trim(raw).ToUpperInvariant().Substring(0, 3);
// ordinary C# composition of delegates
Func<string, int> parseThenIncrement = s => int.Parse(s) + 1;
```

---

## F5 — Collections and traversal

### Sequences: `System.Linq.Enumerable` (228 members) and interfaces

`IEnumerable<T>`, `IEnumerator<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>`,
`IReadOnlyDictionary<TKey,TValue>`, `IList<T>`, `IDictionary<TKey,TValue>`,
`IReadOnlySet<T>` (`inv-bcl-supplement-f2f3f11.md`), `IEqualityComparer<T>`,
`IOrderedEnumerable<T>`, `IGrouping<TKey,TElement>`, `ILookup<TKey,TElement>`,
`Lookup<TKey,TElement>`.

`Enumerable` operators (names verified in `inv-bcl-sequences-linq.md`):
projection/filter (`Select`, `SelectMany`, `Where`, `OfType`, `Cast`),
quantifiers (`All`, `Any`, `Contains`, `SequenceEqual`),
cardinality (`Count`, `LongCount`, `TryGetNonEnumeratedCount`),
retrieval (`First/Last/Single/ElementAt` and `...OrDefault` overloads, with
`Index` overloads),
set (`Distinct`, `DistinctBy`, `Except`, `ExceptBy`, `Intersect`, `IntersectBy`,
`Union`, `UnionBy`),
ordering (`Order`, `OrderBy`, `OrderByDescending`, `OrderDescending`, `ThenBy`,
`ThenByDescending`, `Reverse`),
partition (`Skip`, `SkipLast`, `SkipWhile`, `Take`, `TakeLast`, `TakeWhile`,
`Chunk`, `Index`),
group/join (`GroupBy`, `GroupJoin`, `Join`, `ToLookup`, `CountBy`, `AggregateBy`),
aggregation (`Aggregate`, `Average`, `Max`, `MaxBy`, `Min`, `MinBy`, `Sum`),
materialization (`ToArray`, `ToDictionary`, `ToHashSet`, `ToList`),
generation (`Empty`, `Range`, `Repeat`, `InfiniteSequence`, `Sequence`, `Shuffle`),
sources (`Append`, `Prepend`, `Concat`, `DefaultIfEmpty`),
and operators present in the 10.0.11 ref pack that a `net9.0` reader would not
find in the same place: `LeftJoin`, `RightJoin`, `InfiniteSequence`, `Sequence`,
`Shuffle` (which of the listed operators are new in 10 versus earlier releases is
not established in this survey; see "Not examined").
`Aggregate` has only the 3 classic overloads (no `AggregateBy` merge semantics for
error accumulation). `Chunk(size)` materializes arrays.

### Async sequences: `System.Linq.AsyncEnumerable` (188 members)

`System.Linq.AsyncEnumerable.dll` ships in the base ref pack. It mirrors the sync
operator set (`Where`, `Select`, `SelectMany`, `AggregateAsync`, `AllAsync`,
`AnyAsync`, `ContainsAsync`, `CountAsync`, `FirstAsync`, `FirstOrDefaultAsync`,
`LastAsync`, `SingleAsync`, `SingleOrDefaultAsync`, `ElementAtAsync`,
`ElementAtOrDefaultAsync`, `MaxAsync`, `MinAsync`, `SumAsync`, `AverageAsync`,
`ToArrayAsync`, `ToListAsync`, `ToHashSetAsync`, `ToDictionaryAsync`,
`ToLookupAsync`, `GroupBy`, `Join`, `LeftJoin`, `RightJoin`, `AggregateBy`,
`CountBy`, `Distinct`, `DistinctBy`, `Except`, `ExceptBy`, `Intersect`,
`IntersectBy`, `Union`, `UnionBy`, `OrderBy`, `ThenBy`, `Reverse`, `Skip*`,
`Take*`, `Chunk`, `Index`, `InfiniteSequence`, `Sequence`, `Shuffle`, `Zip`,
`DefaultIfEmpty`, `Concat`, `Append`, `Prepend`, `Cast`, `OfType`, `Range`,
`Repeat`, `Empty`), plus `ToAsyncEnumerable` and
`SequenceEqualAsync`. Every operator takes a `CancellationToken` and, for
delegates, 1- and 2-argument forms plus `CancellationToken`-aware `ValueTask`
predicate/selector overloads (e.g. `Where<TSource>(IAsyncEnumerable<TSource>,
Func<TSource,CancellationToken,ValueTask<bool>>)`). Operators are streaming and
single-pass; grouping/ordering materialize. `ToAsyncEnumerable` bridges a sync
sequence into this surface; `TaskAsyncEnumerableExtensions.ToBlockingEnumerable`
is the blocking reverse bridge. There is no non-blocking sync bridge.

### Span and memory

`Span<T>` (25), `ReadOnlySpan<T>` (23), `Memory<T>`, `ReadOnlyMemory<T>` (18),
`ArraySegment<T>` (20), `System.MemoryExtensions` (264 members), `Array` (108 members:
`Empty`, `Fill`, `Find*`, `Resize`, `Sort`, `BinarySearch`, `AsReadOnly`).

`MemoryExtensions` highlights: `AsSpan`, `ToArray`, `Contains`, `IndexOf`,
`LastIndexOf`, `IndexOfAny`, `StartsWith`, `EndsWith`, `SequenceEqual`, `Overlaps`,
`CommonPrefixLength`, `Reverse`, `ToUpper/ToLower/ToUpperInvariant/ToLowerInvariant`,
`Trim*`, `Split`/`SplitAny` returning `MemoryExtensions.SpanSplitEnumerator<T>`
(`inv-bcl-sequences-linq.md` / `System.SpanSplitEnumerator`1`),
`TryWrite`-style `TryWriteInterpolatedStringHandler` formatting into a span, and
`IsWhiteSpace`/`Count`-style helpers. Span is a `ref struct` (`IsByRefLikeAttribute`),
cannot cross `await`/`yield` boundaries, and cannot be stored in heap fields.

Buffers: `ArrayPool<T>` (`Shared`, `Rent`, `Return`), `MemoryPool<T>`
(`Shared`, `Rent`), `ArrayBufferWriter<T>`, `IBufferWriter<T>`, `IMemoryOwner<T>`,
`MemoryManager<T>`, `ReadOnlySequence<T>` (27), `SequenceReader<T>` (36),
`SearchValues<T>`, `BinaryPrimitives` (126), `Utf8Parser`/`Utf8Formatter`,
`Base64`/`Base64Url`.

### Collections

`List<T>` (54), `Dictionary<TKey,TValue>` (30), `HashSet<T>` (37),
`SortedSet<T>` (32), `SortedDictionary<TKey,TValue>` (17), `KeyValuePair<TKey,TValue>`,
`AlternateLookup<TAlternateKey>` for `Dictionary`/`HashSet` (alternate-key lookups
without allocation). `System.Collections.Immutable`, `System.Collections.Frozen`,
and `System.Collections.Concurrent` are inventoried under F9; `CollectionExtensions`
(`inv-bcl-supplement.md`) provides the working default-access helpers:
`GetValueOrDefault` (2), `TryAdd`, `Remove(key, out value)`, `AsReadOnly(IList<T>)`,
`AsReadOnly(ISet<T>)` returning `System.Collections.ObjectModel.ReadOnlySet<T>`,
`AddRange(List<T>, ReadOnlySpan<T>)`, `InsertRange(List<T>, int, ReadOnlySpan<T>)`,
`CopyTo(List<T>, Span<T>)`.

### What is absent

No `IEnumerable`-compatible error-accumulating traversal, no `Traverse`/`Sequence`,
no error-location context (element index/key/element value) attached to failures in
the base BCL. `JsonNode.GetPath()` is a document path, not a pipeline error path.
There is no `Choose` (Option-aware filter/map); the BCL idiom is
`source.Where(...).Select(...)` plus nullable/`TryGetValue` checks.

---

## F6 — Async, streaming, concurrency

### Task and ValueTask

`inv-bcl-async-concurrency.md`: `Task` (111 members), `Task<TResult>` (38),
`ValueTask` (21), `ValueTask<TResult>` (18), `TaskExtensions` (`Unwrap`),
`TaskAsyncEnumerableExtensions` (`ConfigureAwait`, `WithCancellation`,
`ToBlockingEnumerable`), `ValueTaskSourceStatus`, `IValueTaskSource<TResult>`,
`TaskScheduler`, `TaskFactory`.

Rules visible from the surface + XML docs:

- `Task.Run`/`Task.Factory.StartNew` are the only CPU-offload primitives; library
  code should not hide them (contract: `docs/product-contract.md §"Product Direction"`).
- `ValueTask`/`ValueTask<TResult>` "wraps a Task or a source, only one of which is
  used" (`System.Runtime.xml`, `T:System.Threading.Tasks.ValueTask`1`). Consume
  exactly once (a second `await` is undefined behavior unless the value is
  preserved); use `AsTask()` to await multiple times or store; `Preserve()` exists
  on both `ValueTask` and `ValueTask<TResult>` in the dump; `IsCompletedSuccessfully`
  enables synchronous fast paths. No compiler rule enforces single consumption.
- `Task` static factories: `FromResult`, `FromException`, `FromCanceled`,
  `CompletedTask`, `Delay` (incl. `TimeProvider` overloads), `Yield`.
- Instance: `WaitAsync(CancellationToken|TimeSpan|TimeProvider)`,
  `ConfigureAwait(bool)`, `ConfigureAwait(ConfigureAwaitOptions)`.
  `ConfigureAwaitOptions`: `None`, `ContinueOnCapturedContext`, `SuppressThrowing`,
  `ForceYielding` (names from `System.Runtime.xml`).
- `TaskStatus`: `Created`, `WaitingForActivation`, `WaitingToRun`, `Running`,
  `WaitingForChildrenToComplete`, `RanToCompletion`, `Canceled`, `Faulted`.
- `TaskCreationOptions`: `None`, `PreferFairness`, `LongRunning`,
  `AttachedToParent`, `DenyChildAttach`, `HideScheduler`,
  `RunContinuationsAsynchronously`. `RunContinuationsAsynchronously` is the flag
  that prevents TCS continuations running on the completing thread.

### Composition

- `Task.WhenAll` (6 overloads incl. `ReadOnlySpan<Task>`): completes when all
  complete; on failure the task is faulted with the **first** exception, the rest
  are observable through `Task.Exception`/`AggregateException`
  (`System.Runtime.xml`).
- `Task.WhenAny` (14 overloads): returns the completed task; the others are not
  cancelled or observed.
- `Task.WhenEach` (6 overloads; `IAsyncEnumerable<Task>` / `Task<TResult>`):
  "Creates an [IAsyncEnumerable] that will yield the supplied tasks as those tasks
  complete" (`System.Runtime.xml`, `M:System.Threading.Tasks.Task.WhenEach`).
  This is the .NET-native completion-order stream.
- `Parallel.ForEachAsync` (6 overloads; `IEnumerable<T>` and **`IAsyncEnumerable<T>`**
  sources, `ParallelOptions`, body `Func<T,CancellationToken,ValueTask>`),
  `Parallel.ForAsync<T>` (3), plus sync `Parallel.For`/`ForEach` with partitioners.
  `ParallelOptions`: `CancellationToken`, `MaxDegreeOfParallelism` only. This is the
  bounded-concurrency primitive; it consumes and does not produce a stream.
- `ParallelLoopResult` / `ParallelLoopState` (break/stop semantics).
- `ConcurrentExclusiveSchedulerPair`? *Not in the dump* (not part of the base
  reference assembly set dumped); do not assume it.

### Async streams

`IAsyncEnumerable<T>` (1 member: `GetAsyncEnumerator(CancellationToken)`) and
`IAsyncEnumerator<T>` (`Current`, `MoveNextAsync`, plus inherited `IAsyncDisposable`
`DisposeAsync`). `System.Runtime.CompilerServices.EnumeratorCancellationAttribute`:
"marks the parameter that should receive the cancellation token value from
`IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken)`"
(`System.Runtime.xml`). Without the attribute, a token parameter inside an async
iterator is an ordinary argument and the `WithCancellation`/`GetAsyncEnumerator`
token is not propagated — a common pitfall. `WithCancellation(CancellationToken)`
and `ConfigureAwait(bool)` on `IAsyncEnumerable<T>` return
`ConfiguredCancelableAsyncEnumerable<T>` (in `inv-bcl-supplement-f2f3f11.md`) with
its own `GetAsyncEnumerator()`/`WithCancellation`/`ConfigureAwait`.
`await foreach` calls `DisposeAsync` (including on `break`/exception), so generators'
`finally` blocks run. There is no `IAsyncEnumerable`-to-`IEnumerable` sync bridge
except `ToBlockingEnumerable` (blocking, not for request paths).

### Channels

`System.Threading.Channels` (`inv-bcl-async-concurrency.md`):
`Channel.CreateUnbounded`, `CreateUnboundedPrioritized`,
`CreateBounded(BoundedChannelOptions)`; `ChannelReader<T>` (`ReadAsync`,
`ReadAllAsync`, `TryRead`, `TryPeek`, `WaitToReadAsync`, `Count`,
`CanCount`, `CanPeek`, `Completion`), `ChannelWriter<T>` (`WriteAsync`,
`TryWrite`, `WaitToWriteAsync`, `Complete`, `TryComplete`).
`ChannelOptions`: `SingleReader`, `SingleWriter`, `AllowSynchronousContinuations`.
`BoundedChannelOptions`: `Capacity`, `FullMode`. `BoundedChannelFullMode`:
`Wait`, `DropNewest`, `DropOldest`, `DropWrite` (names from
`System.Threading.Channels.xml`). `Wait` (the default) is the backpressure mode:
`WriteAsync`/`WaitToWriteAsync` do not complete until space exists; the drop modes
allow `TryWrite` to discard without blocking. `ChannelClosedException` reports
writes to a completed channel. Channels are unbounded fan-in/fan-out primitives;
ordering across writers is not guaranteed beyond FIFO per channel.

### Cancellation

`CancellationToken` (struct): `None`, `CanBeCanceled`, `IsCancellationRequested`,
`ThrowIfCancellationRequested`, `Register` (5 overloads), `UnsafeRegister` (no
`ExecutionContext` capture), `WaitHandle`. `CancellationTokenSource`:
`Token`, `Cancel()`, `Cancel(bool throwOnFirstException)`, `CancelAsync()`,
`CancelAfter(int|TimeSpan)`, `TryReset`, `CreateLinkedTokenSource` (token, token1,
token2, `params`, `ReadOnlySpan<CancellationToken>`), `Dispose`.
`CancellationTokenRegistration`: dispose/unregister semantics; in a race, a
registration may run concurrently with its `Dispose`.

### Time

- `System.TimeProvider` (9 members): `System` (static), `GetUtcNow`, `GetLocalNow`,
  `GetTimestamp`, `TimestampFrequency`, `GetElapsedTime` (2), `LocalTimeZone`,
  `CreateTimer(TimerCallback, object, TimeSpan dueTime, TimeSpan period)` returning
  `ITimer`. `ITimer.Change(dueTime, period)`.
- `PeriodicTimer(TimeSpan[, TimeProvider])`: `Period`, `WaitForNextTickAsync(token)`,
  `Dispose`. Single-consumer contract; default period ticks may coalesce.
- `System.Threading.Timer` (13 members) and `TimerCallback` for callback-style
  timing. `Task.Delay(delay, TimeProvider)` integrates delay with the abstraction.
- `System.Threading.Timeout`: `InfiniteTimeSpan`, `Infinite`.

### Other concurrency primitives present

`Lock` (C# 13 `lock` object), `Interlocked` (52), `Volatile` (30),
`SemaphoreSlim` (19), `ManualResetEventSlim`, `CountdownEvent`, `Barrier`,
`ReaderWriterLockSlim`, `WaitHandle`, `Monitor` (not dumped), `SpinLock`/`SpinWait`
(not dumped).

---

## F7 — Effects, resources, environment

- `System.IDisposable.Dispose()`; `System.IAsyncDisposable.DisposeAsync()`
  returning `ValueTask` (`inv-bcl-async-concurrency.md`).
- Language forms: `using`, `await using`, `using` declarations, `try/finally`.
  `await foreach` disposes the async enumerator (`IAsyncEnumerator<T> :
  IAsyncDisposable`).
- `System.Threading.AsyncLocal<T>` (`inv-bcl-supplement.md`): `Value` get/set and an
  optional `Action<AsyncLocalValueChangedArgs<T>>`; "ambient data ... local to a
  given asynchronous control flow" (`System.Threading.xml`). It flows through
  `await` via `ExecutionContext`; mutations inside an async method are local to
  that flow.
- Environment/DI boundary: `System.IServiceProvider.GetService(Type)` is in the
  **base BCL** (`System.ComponentModel.dll`, `inv-bcl-supplement.md`).
  `Microsoft.Extensions.DependencyInjection.Abstractions` types such as
  `IServiceScope`/`IServiceScopeFactory` are **not** in the base ref pack
  (checked by XML member lookup: no match). `docs/product-contract.md §"Package And Dependency Boundary"`
  therefore keeps `Microsoft.Extensions` out of the core and makes
  `TimeProvider`/services caller-provided environment values.
- No effect/IO/retry/timeout type. Timeouts are expressed with
  `CancellationTokenSource.CancelAfter` + linked tokens or `Task.WaitAsync`;
  retries are hand-written loops.

---

## F8 — State transitions and machines

**Nothing in the base BCL.** There is no state-machine/transition/command-replay
type, no actor runtime, no workflow scheduler, and no serialization contract for
typed transitions. `System.Runtime.CompilerServices.IAsyncStateMachine` (2 members)
is compiler plumbing for `async` methods, not a user-facing machine API. The only
BCL-assisted parts of this family are the delegates/`Task` carriers used to
implement transitions and `System.Text.Json` (F11) used to serialize state and
events.

---

## F9 — Optics and immutability

### Immutable collections (`inv-bcl-collections-immutable.md`, 36 types)

`ImmutableArray<T>` (73 members, `struct`), `ImmutableList<T>` (52),
`ImmutableDictionary<TKey,TValue>` (24), `ImmutableHashSet<T>` (22),
`ImmutableSortedSet<T>` (28), `ImmutableSortedDictionary<TKey,TValue>` (25),
`ImmutableQueue<T>`, `ImmutableStack<T>`; interfaces `IImmutableList<T>`,
`IImmutableSet<T>`, `IImmutableDictionary<TKey,TValue>`, `IImmutableQueue<T>`,
`IImmutableStack<T>`; static factories `ImmutableArray.Create*`,
`ImmutableList.Create*`, `ToImmutableArray/List/Dictionary/HashSet`;
`ImmutableInterlocked` (19) for CAS-style updates of immutable fields;
`Builder` nested types for bulk construction.

Key semantics and traps:

- `ImmutableArray<T>` is a struct whose `default` is **uninitialized**;
  `IsDefault`, `IsDefaultOrEmpty`, `IsEmpty` distinguish it. `default(ImmutableArray<T>)`
  is not an empty array; use `ImmutableArray<T>.Empty`. `AsSpan()` (3) exposes
  `ReadOnlySpan<T>`, so immutable arrays feed span pipelines without copying.
- `Add`/`SetItem`/`Remove` return new instances; each mutation copies at least the
  path needed (tree structures for list/dictionary/set).
- `Builder.ToImmutable()` is the bulk path; builders are mutable and not
  thread-safe.

### Frozen collections

`FrozenDictionary<TKey,TValue>` (14) and `FrozenSet<T>` (17), plus
`ToFrozenDictionary`/`ToFrozenSet` and `Create` overloads. "Immutable, read-only
dictionary optimized for fast lookup and enumeration"
(`System.Collections.Immutable.xml`, `T:System.Collections.Frozen.FrozenDictionary`2`).
Creation is proportional to input and optimizes the representation; the result is
not a `Dictionary` and is not updated (no `Add`). `Empty` singleton;
`GetAlternateLookup<TAlternateKey>` supports alternate-key lookups.

### Concurrent collections

`ConcurrentDictionary<TKey,TValue>` (`GetOrAdd`, `AddOrUpdate` incl. `TArg` forms,
`TryRemove(key, out value)`, `GetAlternateLookup`),
`ConcurrentQueue<T>`/`ConcurrentStack<T>`/`ConcurrentBag<T>`,
`BlockingCollection<T>`, `IProducerConsumerCollection<T>`, `Partitioner`/
`OrderablePartitioner` for PLINQ/`Parallel`.

### Optics

No lens/prism/traversal/iso/optional types in the BCL. Immutable updates are
expressed by constructing a modified copy (`with` for records, `SetItem` for
immutable collections, `ToBuilder` for bulk edits). `record` `with` expressions are
compiler+`System.Runtime.CompilerServices.IsExternalInit`-based shallow copies.

---

## F10 — HTTP integration (framework reference, not the base ref pack)

Pin note: this section describes `Microsoft.AspNetCore.App.Ref/10.0.11` types reached
through the `Microsoft.AspNetCore.App` framework reference
(`src/FunnySharp.AspNetCore/FunnySharp.AspNetCore.csproj:23`). They are not in the
`Microsoft.NETCore.App` ref pack. `inv-aspnetcore-http.md` is a filtered dump of 11
relevant types; the full framework surface is not inventoried here.

- `Microsoft.AspNetCore.Http.IResult`: one member,
  `Task ExecuteAsync(HttpContext)`. `Results` (68 static factories) returns
  `IResult`; `TypedResults` (68 factories) returns concrete `HttpResults` types
  (e.g. `Ok<TValue>`, `BadRequest`, `NotFound`, `Created<TValue>`) that
  Minimal-API metadata and OpenAPI can read. Typed-results unions
  `Microsoft.AspNetCore.Http.HttpResults.Results<TResult1..TResult6>` exist
  (`Microsoft.AspNetCore.Http.Results.xml`, type IDs `Results`2`…`Results`6`) and
  are the compile-time way to declare "this endpoint returns one of N typed
  results". `IStatusCodeHttpResult.StatusCode` and `IValueHttpResult.Value` expose
  status/body without an `IResult` cast.
- `Microsoft.AspNetCore.Mvc.ProblemDetails` (7 members: `Type`, `Title`, `Status`,
  `Detail`, `Instance`, `Extensions`) and
  `Microsoft.AspNetCore.Http.HttpValidationProblemDetails` (`Errors` as
  `IDictionary<string,string[]>`). `IProblemDetailsService`
  (`WriteAsync`/`TryWriteAsync(ProblemDetailsContext)`, `ValueTask<bool>`) and
  `ProblemDetailsContext` let middleware/services render failures consistently.
  `StatusCodes` (66 constants) for status codes.
- .NET 10 Minimal API validation: `AddValidation`/`DisableValidation`, attribute-
  and `IValidatableObject`-based validation, automatic 400 responses, and
  `IProblemDetailsService` customization of validation errors (ASP.NET Core .NET 10
  release notes, pinned raw:
  `https://raw.githubusercontent.com/dotnet/AspNetCore.Docs/live/aspnetcore/release-notes/aspnetcore-10/includes/ValidationSupportMinAPI.md`
  and `.../validation-with-problem.md`).
- .NET 10 SSE: `TypedResults.ServerSentEvents(IAsyncEnumerable<SseItem<T>>, ...)`
  (same release-notes source, `includes/sse.md`). `SseItem<T>`
  (`Data`, `EventType`, `EventId`, `ReconnectionInterval`) and `SseParser<T>`
  (`Enumerate()`, `EnumerateAsync(token)`) are in the **base BCL**
  (`System.Net.ServerSentEvents.dll`, `inv-bcl-sse.md`). This is the native
  streaming-HTTP path; the framework does not require a third-party abstraction.
- OpenAPI in .NET 10: OpenAPI 3.1 documents with JSON Schema draft 2020-12,
  YAML serving, XML doc-comment population, endpoint-specific operation
  transformers, and a Microsoft.OpenApi 2.0 upgrade with breaking changes
  (same release-notes source, `includes/openApi.md`,
  `includes/OpenApiPopulateXMLDocComments.md`,
  `includes/responseDescProducesResponseType.md`).
- FunnySharp.AspNetCore currently exposes one type,
  `FunnySharp.AspNetCore.HttpResultExtensions` (15 members,
  `inv-funny-sharp-aspnetcore.md`), mapping `Option`/`Result`/`Validation`/
  `Effect` to `IResult` with caller-supplied `ProblemDetails` factories.

---

## F11 — Cross-cutting: diagnostics, allocation counters, interop, trimming/AOT

### Time and allocation measurement

- `System.Diagnostics.Stopwatch` (`inv-bcl-supplement.md`): `StartNew`,
  `GetTimestamp`, `GetElapsedTime(ts[, ts])`, `Frequency`, `IsHighResolution`,
  `Elapsed`, `ElapsedTicks`, `Reset`/`Restart`. `GetTimestamp`-based elapsed
  measurement avoids `Stopwatch` allocation entirely.
- `System.GC` (39 members): `GetTotalAllocatedBytes(bool precise)` ("bytes
  allocated over the lifetime of the process ... excludes native allocations",
  `System.Runtime.xml`), `GetAllocatedBytesForCurrentThread()`,
  `CollectionCount(int)`, `GetTotalMemory(bool)`, `GetGCMemoryInfo([GCKind])`,
  `GetTotalPauseDuration()`, `AllocateArray<T>`/`AllocateUninitializedArray<T>`,
  `GetConfigurationVariables()`. The per-thread counter is the low-overhead
  allocation receipt for benchmarks; the existing repo receipts live in
  `eng/performance/baseline.json` (see `benchmarks/FunnySharp.Benchmarks/*.cs`,
  `eng/performance/baseline.json:5-18`).

### Boxing and generic specialization

`RuntimeHelpers.Box(ref byte, RuntimeTypeHandle)`,
`RuntimeHelpers.IsReferenceOrContainsReferences<T>()`,
`RuntimeHelpers.GetUninitializedObject`, `RuntimeHelpers.GetSubArray<T>(T[], Range)`;
`System.Runtime.CompilerServices.Unsafe` (45 members in
`inv-bcl-supplement-f2f3f11.md`): `Unbox<T>(object)`, `As<T>(object)`,
`SizeOf<T>`, `AsRef`, `BitCast`, block copy/init. These are the supported
low-level tools for avoiding boxing/interface dispatch in generic code; they are
unsafe and should be confined to measured hot paths.

### Trimming and Native AOT annotations

`System.Diagnostics.CodeAnalysis`: `RequiresUnreferencedCodeAttribute`
(`Message`, `Url`, `ExcludeStatics`), `RequiresDynamicCodeAttribute`,
`RequiresAssemblyFilesAttribute`, `DynamicallyAccessedMembersAttribute`
(+ `DynamicallyAccessedMemberTypes` 30 values), `UnconditionalSuppressMessageAttribute`
(`Category`, `CheckId`, `Justification`, `MessageId`, `Scope`, `Target`),
`ExperimentalAttribute` (`DiagnosticId`), `FeatureGuardAttribute`,
`FeatureSwitchDefinitionAttribute`, `ConstantExpectedAttribute`.
`System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported` /
`IsDynamicCodeCompiled` plus `IsSupported(string)` feature switches are the
runtime gates. The repo policy is in `docs/product-contract.md §"Trimming And Native AOT Policy"`: both
packages set `IsTrimmable` (`src/FunnySharp/FunnySharp.csproj:17`,
`src/FunnySharp.AspNetCore/FunnySharp.AspNetCore.csproj:17`) and deliberately do
not claim `IsAotCompatible` for the open generic surface. No first-party Roslyn
analyzers exist (deferral: `docs/product-contract.md §"Analyzers And Compiler Feedback"`); `StringSyntax`,
`ConstantExpected`, `Obsolete`/`Experimental` are the compiler-visible hints the
BCL offers.

### Serialization policy inputs

`System.Text.Json` (`inv-bcl-supplement.md`, 104 `JsonSerializer` members):
`JsonSerializerOptions` (42 members) with `JsonSerializerDefaults`, `Web`
preset, and .NET 10 additions `Strict` preset and `AllowDuplicateProperties`;
source-generation path via `JsonSerializerContext`, `JsonTypeInfo<T>`,
`JsonSourceGenerationOptionsAttribute`, `JsonDerivedTypeAttribute`,
`JsonIgnoreCondition`, `JsonNumberHandling`; streaming
`DeserializeAsyncEnumerable`. A type can opt into read-only collection
deserialization only if it exposes appropriate constructors/interfaces; there is
no built-in converter for `Option`/`Result`/`Validation` (F1/F2/F3 carriers would
need converters or a documented "serialize the DTO, not the carrier" policy).

---

## What the BCL does **not** provide

Verified against the pinned ref pack (dumps above; "not present" means not in the
dumped types and not found by targeted XML lookup):

1. `Option<T>` / `Maybe<T>` / `Try<T>` carrier. NRT, `Nullable<T>`, and Try
   methods cover absence only with a different programming model.
2. `Result<TValue,TError>` / `UnitResult<TError>` / `Try<T>` value carriers;
   exceptions and `bool`+`out` are the only fail-fast channels.
3. Validation accumulation: no `Validation<T,TError>`, no applicative
   composition, no typed error list. DataAnnotations collects
   `ValidationResult` objects only.
4. `Unit`/`Nothing` type (`void`, `Task`, `ValueTask` are the carriers).
5. Effects/IO/retry/timeout policy types. `TimeProvider` + `CancellationTokenSource`
   + `Task.WaitAsync` are the building blocks, not a policy layer.
6. Optics: no Lens/Optional/Prism/Traversal/Iso hierarchy or combinators.
7. State machines/transitions/command replay/actors: none.
8. Path-context traversal: no index/key/path context attached to per-element
   failures; `JsonNode.GetPath()` is the only "path" (JSON document nodes).
9. Bounded parallel **streaming** map with backpressure: `Parallel.ForEachAsync`
   bounds concurrency but returns one `Task`; `Channel` provides the raw pieces; no
   library combinator composes them into an ordered `IAsyncEnumerable` with
   bounded fan-out.
10. First-success/race-with-drain: `Task.WhenAny` returns the first completed task
    and leaves the others unobserved; there is no "run N, return first success,
    observe/drain the rest" primitive.
11. Function grammar: no `Pipe`/`Compose`/`Curry`/`Partial`/`Flip`/`Tap`.
12. Discriminated-union carriers (until/unless future language unions and any
    associated library types ship; see `docs/product-contract.md §"Deliberate Deferrals"`).
13. Ambient DI container/scope: only `IServiceProvider.GetService(Type)` is BCL;
    `IServiceScope`/`IServiceScopeFactory` and service registration are
    `Microsoft.Extensions.*` (framework/package), intentionally outside the core.

---

## Not examined

- Non-base BCL assemblies not listed in the dumps: `System.Text.RegularExpressions`,
  `System.Net.Http`, `System.IO.Pipelines` (types appear only transitively),
  `System.Numerics`, `System.Security.Cryptography`, `System.Globalization`,
  `System.Diagnostics.DiagnosticSource` metrics/tracing, `System.Formats.*`,
  `System.Runtime.Serialization`, `System.Linq.Parallel` (PLINQ) internals.
- Every member's runtime behavior. Signatures and XML summaries were read; operator
  evaluation order, exception timing, and allocation behavior were not executed for
  each API.
- Full ASP.NET Core surface: only 11 HTTP result types were dumped; MVC, endpoint
  routing, middleware, auth, and the OpenAPI package internals are covered from
  Microsoft Learn only.
- Which .NET 10 preview introduced `System.Linq.AsyncEnumerable` in-box, and which
  async LINQ operators are new in 10 versus carried from the former
  `System.Linq.Async` package. The 10.0.11 ref pack presence is verified; the
  history is not.
- C# 14 language features beyond those cited (extension operators for tensors,
  `field` keyword, etc.) are out of scope for a BCL inventory.
- Nullability annotations of BCL members: metadata mode does not decode them; the
  XML docs and the runtime-mode output where available are the only annotation
  evidence.
- Performance characteristics of BCL APIs (no microbenchmarks were run for this
  inventory); only the measurement APIs themselves were inventoried.
