# Funcky 3.6.0 — capability inventory (Goal 14, baseline survey)

Scope: the pinned Funcky 3.6.0 NuGet package (binary + XML docs + analyzers + targets),
the pinned `polyadic/funcky` clone, and Funcky's own README/docs/tests. Every claim cites
a package path, a clone `path:line`, or a generated dump line. Capability tags use the
Goal 14 taxonomy F1–F11. "FunnySharp today" compares against commit
`4dbebd94b7b58648632112b7ca47c39cc517f153` (`docs/next-stage/inventory/generated/inv-funny-sharp-core.md`).

Generated dumps used as evidence:

- `docs/next-stage/inventory/generated/inv-funcky.md` (85 public types, reflection dump of
  `lib/net10.0/Funcky.dll`)
- `docs/next-stage/inventory/generated/inv-funcky-analyzers.md` (2 analyzer classes in metadata
  mode; the diagnostic descriptors are private and are not part of the dump — see §7)

## 1. Provenance

| Item | Value | Evidence |
| --- | --- | --- |
| Package | `Funcky` 3.6.0 (NuGet) | `Funcky.nuspec:5` |
| nupkg SHA256 | `1a7ab6c6595a2f4d3bdfa83768f9054450beaffd322817644f3dea65940cc32f` | verified with `sha256sum /tmp/opencode/baselines/funcky.3.6.0.nupkg`; matches brief |
| Assemblies | 9 lib TFMs: `net10.0`, `net9.0` … `net5.0`, `netcoreapp3.1`, `netstandard2.1`, `netstandard2.0` | `lib/` layout; `~/repos/funcky/Funcky/Funcky.csproj:4` |
| Surveyed assembly | `lib/net10.0/Funcky.dll`, assembly version `3.6.0.0` | `inv-funcky.md:3`; SHA256 `e515bf36a6fef5045dee35e9964bc8937c6ac654d023d50973f65d03951cbf00` |
| XML doc | `lib/net10.0/Funcky.xml` SHA256 `cd398cbbdf5630182383b849121239d3370ae989aa78a51fab958b37814e912c` |  |
| Repository commit of the shipped build | `7409d79217c732855f6768459021e5014fe04db5` (branch `release-3.6`) | `Funcky.nuspec` `<repository>` |
| 3.6.0 tag | `a1f80fd4814590c2e05de517eea063d7468542e7`; 11 commits before the shipped build, 32 before the clone HEAD | `git rev-parse 3.6.0^{commit}`, `git log --oneline 3.6.0..HEAD \| wc -l` |
| Clone | `~/repos/funcky` HEAD `133ba5bac4cb57861a3b2fbea49a98f0aa9a953a` (remote `polyadic/funcky`), `git describe` = `3.6.0-32-g133ba5ba` | clone |
| License | `MIT OR Apache-2.0` (dual) | `Funcky.nuspec:7`; `~/repos/funcky/LICENSE-MIT`, `LICENSE-Apache` |
| Analyzer assembly | `analyzers/dotnet/cs/Funcky.BuiltinAnalyzers.dll` SHA256 `5425bbda3df8c1d8f3487bdd5ba16335b045a52084ce492d9cae684c8c5c70cb` |  |
| Code-fix assembly | `analyzers/dotnet/cs/Funcky.BuiltinAnalyzers.CodeFixes.dll` SHA256 `798a0ff945731aca6e9d6feab6cd791698b81c2756606850c32f71795c775d8f` |  |

### Package dependencies (adoption-relevant)

`Funcky.nuspec` declares dependencies only for the legacy TFMs:

| TFM group | Dependencies | Evidence |
| --- | --- | --- |
| `.NETCoreApp3.1` | `System.Text.Json 5.0.2` | `Funcky.nuspec:17-19` |
| `.NETStandard2.0` | `Microsoft.Bcl.HashCode 1.1.1`, `System.Collections.Immutable 1.7.1`, `System.Text.Json 5.0.2` | `Funcky.nuspec:26-30` |
| `.NETStandard2.1` | `System.Collections.Immutable 1.7.1`, `System.Text.Json 5.0.2` | `Funcky.nuspec:31-34` |
| `net5.0`–`net10.0` | no dependencies | `Funcky.nuspec:20-25` |

Consequences:

- A `net10.0` consumer gets **no transitive package** from Funcky; `System.Text.Json` and
  `System.Collections.Immutable` come from the shared framework. The net10.0 assembly still
  references both (`strings lib/net10.0/Funcky.dll`).
- The pinned versions (`System.Text.Json 5.0.2`, `System.Collections.Immutable 1.7.1`) are
  the adoption-relevant liability of the multi-TFM matrix: they are forced on
  `netstandard2.x`/`netcoreapp3.1` consumers. Pins are centralized in
  `~/repos/funcky/Directory.Packages.props:8-9`.
- The `INTEGRATED_ASYNC` feature constant is defined **only for net10.0**
  (`~/repos/funcky/FrameworkFeatureConstants.props:25`), so the async surface in
  `inv-funcky.md` (e.g. `AsyncEnumerableExtensions`) exists only in the `net10.0` assembly.
  Older TFMs still get async through the separate `Funcky.Async` 1.4.1 package
  (`~/repos/funcky/Funcky.Async/Funcky.Async.csproj:3-5`), which depends on
  `System.Linq.Async [5.0.0, 7)` (`Directory.Packages.props:14`). The 3.6.0 changelog records
  this split: "Integrate Funcky.Async into Funcky … no longer a separate `Funcky.Async` package"
  (`~/repos/funcky/changelog.md:11-12`).

### Clone divergence from the shipped 3.6.0 binary

`git diff --stat 7409d79..HEAD` touches 18 files (202 insertions / 54 deletions). Capability
impact:

- **Unreleased new public API** (clone-only, in `PublicAPI.Unshipped.txt`): async `Chunk` with
  result selector — `Chunk<TSource,TResult>`, `ChunkAwait`, `ChunkAwaitWithCancellation`
  (`~/repos/funcky/Funcky/Extensions/AsyncEnumerableExtensions/Chunk.cs:21-38`;
  `~/repos/funcky/Funcky/PublicAPI.Unshipped.txt:2-4`; commits `61045cb1`, `49a74ebf`).
  Not present in `inv-funcky.md`.
- **Unreleased bug fix**: async `Merge` now propagates a `[EnumeratorCancellation]` token into
  `GetAsyncEnumerator` and `ToListAsync` (`git diff 7409d79..HEAD -- .../Merge.cs`, commit
  `7ccbe658`). The shipped 3.6.0 `Merge` creates enumerators with no token.
- Doc-cref fixes and dependabot `Documentation/yarn.lock` bumps; no other runtime change.

Everything below describes the **3.6.0 binary surface** unless it says "clone-only".

## 2. Packaging layout (F11)

```
lib/<9 TFMs>/Funcky.dll + Funcky.xml
analyzers/dotnet/cs/Funcky.BuiltinAnalyzers.dll
analyzers/dotnet/cs/Funcky.BuiltinAnalyzers.CodeFixes.dll
build/Funcky.targets, buildTransitive/Funcky.targets
tools/install.ps1, tools/uninstall.ps1
README.md, icon.png, .signature.p7s
```

- Analyzers are packed **into the main package** (`~/repos/funcky/Funcky/Funcky.csproj:48-49`),
  so they activate for every consumer with no extra install; the `analyzers/dotnet/cs` folder
  is Roslyn's auto-discovery convention.
- `build`/`buildTransitive` targets add four implicit usings when `FunckyImplicitUsings` is
  `true`/`enable`: `Funcky`, `Funcky.Extensions`, `Funcky.Monads`, and static `Funcky.Functional`
  (`build/Funcky.targets:3-8`; identical `buildTransitive/Funcky.targets`).
- `tools/*.ps1` are VS 2019 install scripts copied into the package
  (`~/repos/funcky/Funcky.Analyzers/Funcky.Analyzers.Package/tools/`).
- No `PackageReference` is added for consumers on `net10.0`; nothing is `DevelopmentDependency`
  in the main package.
- Source generator `Funcky.SourceGenerator` is **not shipped** (`IsPackable=false`,
  `~/repos/funcky/Funcky.SourceGenerator/Funcky.SourceGenerator.csproj:9`); it is referenced as
  an analyzer inside Funcky itself (`Funcky/Funcky.csproj:35`) and generates the
  `Parse*OrNone` family from 11 `TryParse` patterns
  (`Funcky/Extensions/ParseExtensions/ParseExtensions.Numbers.cs:5-16`).

## 3. Capability survey by family

### F1 — absence: `Option<T>` and bridges

Pin: `Funcky 3.6.0 / Funcky.Monads.Option<T>`, `Funcky.Monads.Option`.

Core shape (`~/repos/funcky/Funcky/Monads/Option/Option.Core.cs:8-105`):

- `public readonly partial struct Option<TItem> : IOption where TItem : notnull`; fields
  `bool _hasItem` + `TItem _item`; `None => default` (`:26`); constructors reject `null`
  (`:17-22`); `Some`/`Return` throw `ArgumentNullException` on null (`:99-110`).
- `TryGetValue([NotNullWhen(true)] out TItem?)` (`:32-36`) is `[EditorBrowsable(Never)]` and its
  XML doc says it is a "last resort" allowed only in loop conditions, Iterator `if` and
  `catch ... when` clause, enforced by analyzer λ0001 (`:28-30`).
- `Match<TResult>(TResult none, Func<TItem,TResult> some)`,
  `Match<TResult>(Func<TResult> none, Func<TItem,TResult> some)`, `Switch(Action none, Action<TItem> some)`
  (`:40-80`) — value/thunk overloads differ only by delegate-ness; arguments are named
  `[UseWithArgumentNames]`.
- `ToString` = `"None"` / `"Some(value)"` (`:83-86`).
- `Count` and `this[int index]` exist only for list-pattern syntax
  (`Option/Option.ListPattern.cs:16-27`), marked `[SyntaxSupportOnly("list pattern")]` and
  `[EditorBrowsable(Never)]`; misuse is analyzer λ0003, not a runtime error.
- Monad: `Select`, `SelectMany` (both arities) (`Option/Option.Monad.cs:5-22`); convenience:
  implicit conversion from `TItem` (`Option.Convenience.cs:6`), `Where`, `OrElse` (value +
  lazy), `GetOrElse` (value + lazy), `AndThen` (Func/Action/Option), `Inspect`, `InspectNone`,
  `ToEnumerable` (`Option.Convenience.cs:6-73`).
- Equality: `OptionEqualityComparer<TItem>.Default` (= `EqualityComparer<TItem>.Default`),
  custom comparer via `OptionEqualityComparer.Create(IEqualityComparer<T>)`; `None` hashes to 0
  (`Option/OptionEqualityComparer.cs:20,29`). No `IsSome`/`IsNone` properties exist.
- Ordering: `IComparable<Option<T>>`, `IComparable`; `None < Some(x)`, `Some` compared with the
  item's default comparer (`Option/Option.Comparable.cs`, `OptionComparer.cs`).
- Debugger proxy `OptionDebugView<T>` (`Option/Option.Debugger.cs`).
- Factories (`inv-funcky.md:1222-1232`): `Some`, `Return`, `FromNullable` (class/struct
  overloads), `FromBoolean(bool)` → `Option<Unit>`, `FromBoolean(bool, TItem)`,
  `FromBoolean(bool, Func<TItem>)` (the last two are `EditorBrowsable(Advanced)`).
- Bridges (`inv-funcky.md:1269-1288`): `Flatten`, `ToEither(left)`/`ToEither(Func<TLeft>)`,
  `ToNullable` (uses `RequireClass<T>`/`RequireStruct<T>` marker parameters,
  `~/repos/funcky/Funcky/RequireClass.cs`, `RequireStruct.cs`), `Sequence`/`Traverse` toward
  `Either`, `Result`, `Lazy`, `IEnumerable`, `Reader`.
- Serialization: `OptionJsonConverter : JsonConverterFactory`, `Some` written transparently,
  `None` written as JSON `null`; reads `null` → `None`; `[RequiresDynamicCode]` under AOT
  (`Option/OptionJsonConverter.cs:9-40`). Consumers must register it themselves; no attribute.
- Async bridge (`inv-funcky.md:1234-1250`): `Option<Task<T>>`/`Option<ValueTask<T>>` are
  awaitable to `Option<T>`; `GetAwaiter`, `ConfigureAwait`, `Sequence`, `Traverse`,
  `ToAsyncEnumerable`.
- Marker interface `IOption` with an internal member `InternalImplementationOnly()`
  (`Option/IOption.cs:7-11`); used only as a shape marker.
- `DownCast<TResult>.From(Option<T>)` returns `None` on failed cast; `UpCast<TResult>.From(...)`
  upcasts `Option`, `Either`, `Result`, `Lazy` (`~/repos/funcky/Funcky/DownCast.cs`,
  `UpCast.cs`).

FunnySharp today: `Option<T>` (unconstrained `T`) with `IsSome`/`IsNone`, `None` property,
`Some`, `FromNullable`, `FromTry`, `Map/Bind/Filter/Match/OrElse/Zip`, public `TryGetValue`,
async `MapAsync/BindAsync/ValueAsync`, `GetOption` for `IReadOnlyDictionary`, task bridges.
Funcky has: `AndThen`/`Inspect` vocabulary, implicit conversion, comparers, JSON converter,
awaitable `Option<Task<T>>`, list-pattern support, `FromBoolean`, `ToEither`, `FromTry` is
absent (Funcky gets Try-pattern bridges from `Parse*OrNone` and containers instead).

### F2 — fail-fast value outcome: `Result<T>`, `Either<TLeft,TRight>`

**`Result<TValidResult>`** (`~/repos/funcky/Funcky/Monads/Result/Result.Core.cs`):

- `[NonDefaultable] public readonly struct Result<TValidResult> : IEquatable<…> where TValidResult : notnull`,
  fields `TValidResult _result` + `Exception? _error` (`:15-19`). `Ok`/`Return` reject null
  (`:21-32`).
- Error channel is **`Exception` only**. `Result.Error(Exception)` sets the stack trace if
  absent via `ExceptionDispatchInfo.SetCurrentStackTrace` (`:53-79`) — a documented side effect
  on the caller's exception object.
- `Match<TResult>(Func<TValidResult,_>, Func<Exception,_>)`, `Switch` (`:82-102`);
  `Equals` compares result **and exception** (`:110-112`); `GetHashCode` matches either branch
  (`:116-121`); `ToString` = `Ok(value)` / `Error(type: message)` (`:124-127`).
- Convenience: implicit conversion from value, `Inspect`, `InspectError`, `OrElse`
  (value/lazy), `GetOrElse` (value/lazy), `GetOrThrow()` which rethrows the original exception
  via `ExceptionDispatchInfo` (`Result/Result.Convenience.cs:5-49`).
- Monad: `Select`, `SelectMany` ×2 (`Result/Result.Monad.cs`).
- Factories: `Result.Ok`, `Result.Return` (`inv-funcky.md:1376-1379`).
- `ResultExtensions` (`inv-funcky.md:1390-1403`): `Flatten`, `Sequence`/`Traverse` toward
  `Either`, `Option`, `Lazy`, `IEnumerable`, `Reader`, plus async variants
  (`ResultAsyncExtensions`, `inv-funcky.md:1381-1389`).
- `ResultPartitions<TValidResult>` = `(IReadOnlyList<Exception> Error, IReadOnlyList<TValidResult> Ok)`
  with `Deconstruct` (`inv-funcky.md:1026-1035`).

**`Either<TLeft, TRight>`** (`~/repos/funcky/Funcky/Monads/Either/Either.Core.cs`):

- `[NonDefaultable]`; 3-state `Side : byte { Uninitialized, Left, Right }`;
  any operation on `default` throws `NotSupportedException` (`:117-137`).
- `Left`/`Right` reject null; `Match`, `Switch`, `Equals` (side + both fields), `Flip`,
  `ToString` = `Left(x)`/`Right(x)` (`:44-108`); `GetHashCode` ignores which side holds the
  value (`:89-93`).
- Convenience: `SelectLeft`, `Inspect`, `InspectLeft`, `OrElse` (value + `Func<TLeft,…>`),
  `GetOrElse` (value + `Func<TLeft,TRight>`); `OrElse` **discards the left value**
  (`Either/Either.Convenience.cs:5-52`). Monad: `Select`, `SelectMany` ×2. Implicit conversion
  from `TRight` (`:6`). `Either<TLeft>.Return<TRight>` (`inv-funcky.md:1175-1178`).
- `EitherOrBoth<TLeft,TRight>` = left-only / right-only / both (`~/repos/funcky/Funcky/EitherOrBoth.cs`),
  plus `EitherOrBoth.FromOptions` and `EnumerableExtensions.ZipLongest`
  (`inv-funcky.md:53-66`, `:616-617`).
- Casting: `DownCast<TResult>.From(Result<T>)` fabricates `InvalidCastException`;
  `DownCast<TResult>.From(Either<…>)` takes a `Func<TLeft> failedCast` thunk.

FunnySharp today: `Result<TValue,TError>` (typed error, no `Exception` coupling, no stack-trace
side effect), `Try`/`TryAsync`/`TryValueAsync` with `OperationCanceledException` passthrough,
`Ensure`, `Recover`, `RecoverWith`, `MapError`, `Zip`, `ZipWith`, `ToOption`, `ToResult`.
Funcky has no typed error channel and no `Try` boundary; it has `Either`, `EitherOrBoth`,
`DownCast`/`UpCast`, and `Result`-as-`Exception`-carrier. FunnySharp has neither `Either` nor
`EitherOrBoth` in any form.

### F3 — accumulation

Funcky ships **no accumulation capability**: no `Validation`, no applicative `Apply` for
errors, no multi-error traversal. `EnumerableExtensions.Sequence`/`Traverse` are fail-fast and
stop at the first error (`~/repos/funcky/Funcky/Extensions/EnumerableExtensions/Sequence.cs:37-58`).
`EitherPartitions`/`ResultPartitions` are *post-hoc splits of an already-enumerated sequence*
(`EnumerableExtensions/PartitionEither.cs`), not error accumulation. FunnySharp's
`Validation<TValue,TError>` + `ValidationExtensions.Apply` + accumulating `Traverse` have no
Funcky counterpart to compare against; nothing to adopt here.

### F4 — function grammar, `Unit`, sequence constructors

**`Functional`** (static; `inv-funcky.md:70-212`) — 140+ members:

- `Identity`, `Fn`, `NoOperation` (0–8 args), `True`/`False` (0–4 args, always-constant
  predicates), `Not(predicate)`, `All`/`Any` (predicate folds), `Curry`/`Uncurry` (Func up to 8,
  Action up to 8), `Flip` (Func/Action up to 8), `ActionToUnit`/`UnitToAction` (0–8 args).
- `Apply` over `Unit` placeholders: 2..5-parameter functions × every subset of bound `Unit`
  positions — 81 overloads total (`FuncExtensions.Apply`, `inv-funcky.md:625-676`; mirror in
  `Functional`, `inv-funcky.md:83-134`). Parameter names in these overloads are `ω1..ω8`
  (`inv-funcky.md:150-153`).
- `Retry`/`RetryAsync` (see F7).

**`FuncExtensions`/`ActionExtensions`** (`inv-funcky.md:278-303`, `:623-699`): `Compose` (both
directions), `Curry`, `Flip`, `Uncurry`; `Apply` with `Unit` placeholders. There is no `Pipe`
and no `Partial`; composition is written `g.Compose(f)` = `f` then `g`
(`Extensions/FuncExtensions/Compose.cs`).

**`Sequence` constructors** (`inv-funcky.md:231-249`):

- `Return<T>(T)` → `IReadOnlyList<T>`; `Return<T>(params T[])` (CallerLineNumber is not used);
  `FromNullable` (class/struct); `Cycle<T>(T)` (infinite); `Successors` with Option-producing or
  total successor functions; `Concat` (params/params-of-sequences).
- `CycleRange(IEnumerable<T>)` → `IBuffer<T>` (infinite; buffers the source; empty sequence
  throws `InvalidOperationException` on enumeration) and `RepeatRange(source, count)` → `IBuffer<T>`;
  `CycleMaterialized(IReadOnlyCollection<T>)`/`RepeatMaterialized` are allocation-free variants
  (`~/repos/funcky/Funcky/Sequence/Sequence.CycleRange.cs`, `Sequence.CycleMaterialized.cs`).
- `AsyncSequence` mirrors this for `IAsyncEnumerable<T>` with `ValueTask` successors
  (`inv-funcky.md:18-36`).

**`Unit`/`Discard`** (`~/repos/funcky/Funcky/Unit.cs:3-40`, `Discard.cs`): `Unit` is a
`readonly struct` with `Value => default`, all `Unit`s equal, `GetHashCode() => 0`,
`IComparable`; `Discard.__` is a `public static readonly Unit` intended for `switch` expression
guards, explicitly named with two underscores to avoid the C# discard token.

FunnySharp today: `Pipe`, `Tap`, async `Tap/Compose`, 2-arity `Curry`/`Uncurry`/`Flip`/`Partial`,
no `Unit`, no `Identity`/`NoOperation`/`Not`/`All`/`Any`, no sequence constructors, no
`Unit`-placeholder `Apply`.

### F5 — collections and traversal

The largest family. `EnumerableExtensions` alone exposes ~150 methods (`inv-funcky.md:498-618`);
`AsyncEnumerableExtensions` is the async mirror (`inv-funcky.md:304-480`) with the
`X` / `XAwait` / `XAwaitWithCancellation` triple and `[EnumeratorCancellation]` plumbing.

Capability groups:

1. **Optional cardinality** — `FirstOrNone`, `LastOrNone`, `SingleOrNone`, `ElementAtOrNone`
   (index + `Index`), `None(predicate)`, `MinOrNone`/`MaxOrNone`/`MinByOrNone`/`MaxByOrNone`,
   `AverageOrNone` (10 element types × source/selector/Option-selector overloads ⇒ 20 sync +
   60 async overloads), `GetNonEnumeratedCountOrNone`, `PeekOrNone`/`DequeueOrNone`,
   `MoveNextOrNone`, `ReadByteOrNone`, `GetLengthOrNone`/`GetPositionOrNone`/
   `GetReadTimeoutOrNone`/`GetWriteTimeoutOrNone` (`inv-funcky.md:510-574`, `:994-1043`).
   Semi-lazy: the OrNone family forwards to BCL `FirstOrDefault`-style APIs and wraps the
   `default` in `Option`; no deferred side effects of their own.
2. **Deferred combinators** — `Inspect` (side effect at enumeration), `InspectEmpty`,
   `Pairwise` (one element less; disposes the enumerator), `Intersperse`, `Interleave`,
   `SlidingWindow(width)` (windows always full width; shorter source ⇒ empty result),
   `Split(separator)` (materializes each segment as `IReadOnlyList`), `AdjacentGroupBy`
   (consecutive-key groups, `IGrouping`), `Chunk(size[, selector])`, `TakeEvery(interval)`,
   `Transpose`, `PowerSet`, `Shuffle(Random)`, `ZipLongest` (`inv-funcky.md:530-617`;
   `Extensions/EnumerableExtensions/Pairwise.cs:27,36`, `SlidingWindow.cs:28-31`).
3. **Materializers** — `Materialize(source[, materializer])` → `IReadOnlyCollection<T>`
   (`inv-funcky.md:553-554`); the XML doc claims "if the underlying sequence is a collection
   type we do not actively enumerate them", but the sync implementation always calls the
   materializer (`Extensions/EnumerableExtensions/Materialize.cs`); the async twin contains a
   `source switch { _ => await materializer(...) }` no-op switch
   (`Extensions/AsyncEnumerableExtensions/Materialize.cs:31-34`). Both are effectively
   `ToList` with a pluggable factory.
4. **Single upstream pass** — `Memoize` → `IBuffer<T>` (`IDisposable`), which replays a
   single enumeration to multiple consumers, borrows an existing `IBuffer`, or wraps an
   `IList`/`ICollection` directly (`Extensions/EnumerableExtensions/Memoize.cs:16-26`,
   `Buffers/`). Not thread-safe; `Dispose` disposes the source enumerator and clears the buffer.
   The async twin is `IAsyncBuffer<T>` (`IAsyncDisposable`); it creates the source enumerator
   with **no cancellation token** (`Extensions/AsyncEnumerableExtensions/Memoize.cs:57`) and
   ignores the token passed to `GetAsyncEnumerator` (`:61-65`).
5. **k-way ordered merge** — `Merge` for 2/3/4 sources and for a sequence of sources, with an
   `Option<IComparer<T>>` default parameter; precondition: all inputs are sorted by the same
   comparer (`Extensions/EnumerableExtensions/Merge.cs`; async twin materializes inside the
   merge with `ToListAsync` and rebuilds an `ImmutableList` of enumerators per step,
   `Extensions/AsyncEnumerableExtensions/Merge.cs:69,73,87`).
6. **Sequence/Traverse** — `Sequence`/`Traverse` for `Option`, `Result`, `Either`, `Lazy`,
   `Reader`, over sync and async sources; fail-fast, builds `ImmutableArray` then `ToImmutable`
   (`Extensions/EnumerableExtensions/Sequence.cs:37-58`).
7. **Partitions** — `Partition(predicate)` → `Partitions<T>` (`True`, `False`,
   `Deconstruct`), `Partition(IEnumerable<Either<…>>)` → `EitherPartitions<TLeft,TRight>`,
   `Partition(IEnumerable<Result<T>>)` → `ResultPartitions<T>`; **materializes** the source
   (`inv-funcky.md:577-584`, `:983-992`, `:1026-1035`). Async twins are `PartitionAsync` /
   `PartitionAwaitAsync` / `PartitionAwaitWithCancellationAsync` returning `ValueTask<…>`
   (`Extensions/AsyncEnumerableExtensions/Partition.cs`).
8. **Container bridges with OrNone names** — `DictionaryExtensions.GetValueOrNone` /
   `RemoveOrNone` (`Extensions/DictionaryExtensions.cs`; `OverloadResolutionPriority(1)` on the
   `IReadOnlyDictionary` overload for the .NET 9 ambiguity fix), `ListExtensions
   FindIndexOrNone/FindLastIndexOrNone`, `ImmutableListExtensions
   IndexOfOrNone/LastIndexOfOrNone` (5 overloads each), `OrderedDictionaryExtensions.IndexOfOrNone`
   (net9+ only), `QueueExtensions.DequeueOrNone/PeekOrNone`, `PriorityQueueExtensions
   DequeueOrNone/PeekOrNone`, `StreamExtensions.*OrNone`,
   `HttpHeadersExtensions.GetValuesOrNone`,
   `HttpHeadersNonValidatedExtensions.GetValuesOrNone`, `JsonSerializerOptionsExtensions
   GetTypeInfoOrNone`, `QueryableExtensions.FirstOrNone/LastOrNone/SingleOrNone/ElementAtOrNone`
   with `Expression<Func<…>>` predicates (`inv-funcky.md:481-485`, `:701-749`, `:994-1043`).
9. **String extensions** — `SplitLazy` (char/string/params; deferred, `Ordinal` for string
   separators), `SplitLines`, `Chunk`, `SlidingWindow`, and `IndexOfOrNone`/`LastIndexOfOrNone`/
   `IndexOfAnyOrNone`/`LastIndexOfAnyOrNone` with `StringComparison` overloads
   (`Extensions/StringExtensions/`, `inv-funcky.md:1045-1079`).
10. **Range enumeration** — `GetEnumerator(this Range)`, `Select`, `SelectMany`
    (`inv-funcky.md:1017-1025`), enabling `foreach (var i in 1..5)`.
11. **Parse bridges** — ~200 `Parse*OrNone` extensions over `string?`/`ReadOnlySpan<char>`/
    `ReadOnlySpan<byte>` with `IFormatProvider`/`NumberStyles`/`DateTimeStyles`, generic
    `ParseOrNone<T> where T : IParsable<T>`/`ISpanParsable<T>`/`IUtf8SpanParsable<T>`, enum,
    `Guid`, `Version`, `BigInteger`, `TimeSpan`, `DateOnly`, `TimeOnly`, `DateTime(Offset)`,
    `IPAddress`/`IPNetwork`/`IPEndPoint`, `AssemblyNameInfo`/`TypeName`, and 20
    `System.Net.Http.Headers.*` types (`inv-funcky.md:750-982`). All are `TryParse`-backed and
    never throw on malformed input.
12. **Positioned-value structs** — `ValueWithIndex<T>`, `ValueWithFirst<T>`, `ValueWithLast<T>`,
    `ValueWithPrevious<T>` with `Deconstruct`, produced by `WithIndex`/`WithFirst`/`WithLast`/
    `WithPrevious` (`inv-funcky.md:1080-1108`).
13. **`Choose` equivalent** — `WhereSelect` (source → `Option<T>`), `WhereNotNull`, plus
    `Flatten`, `ConcatToString`, `JoinToString`, `ForEach` (returns `Unit`), `AnyOrElse`,
    `ExclusiveScan`/`InclusiveScan`, `GetNonEnumeratedCountOrNone` (`inv-funcky.md:532-617`).

FunnySharp today: `Choose` (sync + async + async ValueTask), `SelectTo`/`WhereTo`/`SelectInPlace`/
`WhereInPlace`/`ChooseTo` for span/memory, `Sequence`/`Traverse` for `Option`/`Result`/
`Validation` (sync + async), bounded parallel mapping and traversal, `SelectParallelValueAsync`.
FunnySharp has **no** optional-cardinality extensions, no deferred combinators, no containers
bridges, no parse bridges, no string extensions, no partitions, no `Memoize`/`Merge`.

### F6 — async, streaming, concurrency

- `AsyncEnumerableExtensions` (`inv-funcky.md:304-480`): async mirrors of every F5 group plus
  `Merge`, `MaterializeAsync`, `Memoize`, `PartitionAsync`, `ShuffleAsync`, `Split`,
  `SequenceAsync`, `TraverseAsync`, `WhereNotNull`, `AverageOrNoneAsync` family. Public
  signatures carry no `CancellationToken` parameter for sequence transformations: the token is
  the `[EnumeratorCancellation]` parameter of the private iterator, consumed through
  `.WithCancellation(cancellationToken)` or `GetAsyncEnumerator(cancellationToken)`
  (`Extensions/AsyncEnumerableExtensions/Inspect.cs:41-58`). Terminal operations
  (`MaterializeAsync`, `PartitionAsync`, `SequenceAsync`, `TraverseAsync`) do take a token.
- `Option<Task<T>>`/`Option<ValueTask<T>>` awaiters and `ConfigureAwait` (F1) —
  `Option.IsCompleted` is `true` for `None` so awaiting `None` completes without work
  (`Monads/Option/OptionTaskAwaiter.cs:15-23`).
- `AsyncFunctional.RetryAsync` uses `Task.Delay(delay, cancellationToken)` and
  `ThrowIfCancellationRequested` before each attempt (`Functional/RetryAsync.cs:12-45`,
  `RetryWithExceptionAsync.cs:35-50`); the sync `Functional.Retry` uses `Thread.Sleep`
  (`Functional/Retry.cs:39`, `RetryWithException.cs:23`).
- There is no `Channel`, no bounded fan-out, no parallel map, and no `TimeProvider` anywhere in
  the package surface. `Merge` is a k-way merge of sorted streams, not a concurrent fan-in.
- `AsyncSequence.CycleRange`/`RepeatRange` return `IAsyncBuffer<T>`; same single-consumer,
  no-token caveats as sync.

FunnySharp today: bounded parallel via `Channel` + `SelectParallelValueAsync`, parallel
traversal with Option/Result/Validation, `FirstSuccessAsync` with `TimeProvider` timeouts,
full cancellation discipline. Funcky has no concurrency capability at all; its async story is
sequence transformation + retry.

### F7 — effects, environment, retries

- **`Reader<TEnvironment, TResult>` is a delegate**, not a monad wrapper:
  `public delegate TResult Reader<in TEnvironment, out TResult>(TEnvironment environment)`
  (`Monads/Reader/Reader.Core.cs:3-6`), with `Reader<TEnvironment>.Return/FromFunc/FromAction`
  and `Select`/`SelectMany`/`Flatten` extensions
  (`inv-funcky.md:1356-1375`). No async, no cancellation, no resource scoping, no effect
  deferral marker other than "it's a function".
- **Retry policies** (`inv-funcky.md:1429-1461`): `IRetryPolicy { int MaxRetries; TimeSpan Delay(int retryCount); }`,
  `DoNotRetryPolicy` (MaxRetries 0), `NoDelayRetryPolicy(maxRetries)`,
  `ConstantDelayPolicy(maxRetries, delay)`, `LinearBackOffRetryPolicy(maxRetries, firstDelay)`
  (`firstDelay * retryCount`), `ExponentialBackOffRetryPolicy(maxRetries, firstDelay)`
  (`firstDelay * 1.5^retryCount`, `BaseFactor = 1.5`). No jitter, no max-delay cap, no
  `TimeProvider`; `ConstantDelayPolicy` is the only non-sealed policy.
- **Retry entry points**: `Functional.Retry(Func<Option<T>>)` **unbounded** (recursive until
  `Some`), `Retry(producer, policy)`, `Retry(producer, shouldRetry, policy)` and
  `Action`/async twins; attempts = `MaxRetries + 1` (tests assert this,
  `Funcky.Test/FunctionalClass/RetryTest.cs:38-42`). Sync paths block with `Thread.Sleep`;
  async paths use `Task.Delay(…, token)`. Delay indexing is inconsistent between the
  Option-producer variant (`Delay(0)` first, `Functional/Retry.cs`) and the Exception variant
  (`retryCount++` before sleeping, `RetryWithException.cs:23`), so `LinearBackOff` begins at
  `firstDelay` only for the Exception variant and at `0` for the Option variant.

FunnySharp today: `Effect<T>`/`Effect<TEnvironment,T>` thin struct wrappers with `RunAsync`,
`Bind/Map/Select/SelectMany/Provide/WithEnvironment`, resource scoping `Using`/`UsingAsync`,
`FirstSuccessAsync` with `TimeProvider`. No retry API exists in `src/FunnySharp`.

### F8 — state transitions and machines

Funcky has no state-machine, command, replay, or workflow capability. Nothing to survey.

### F9 — optics and immutability

Funcky has no Lens/Optional/prism/traversal; no immutable-collection policy. The only
`System.Collections.Immutable` usage is internal (`Grouping` for `AdjacentGroupBy`,
`ImmutableArray` builders in `Traverse`, `ImmutableList` in `Merge`) and in
`ImmutableListExtensions.IndexOfOrNone`. Immutable collections are not exposed as carriers.

### F10 — HTTP integration

Funcky has no ASP.NET Core integration, no `IResult` mapping, no ProblemDetails, no OpenAPI.
Only `System.Net.Http`-shaped helpers: `HttpHeadersExtensions.GetValuesOrNone`,
`HttpHeadersNonValidatedExtensions.GetValuesOrNone`, and the 20 `System.Net.Http.Headers.*`
parse bridges.

### F11 — cross-cutting design choices

- **Vocabulary**: `Select`/`SelectMany`/`Where` (LINQ-first), `Match`/`Switch` for elimination,
  `OrElse`/`GetOrElse`/`AndThen`/`Inspect`, `XOrNone` for Try-pattern bridges, `None` as a
  property, `Some`/`Return` factories. The README states the intent: "Funcky wants to be
  functional C#… uses the C# monadic interfaces as an advantage"
  (`README.md:92-93`).
- **Default states**: `Option.None == default` and is fully valid; `Either`, `EitherOrBoth`,
  `Result` are `[NonDefaultable]` and throw `NotSupportedException` at first use. The marker
  attribute is `internal` to the library (`Funcky/CodeAnalysis/NonDefaultableAttribute.cs`);
  enforcement is in the separate `Funcky.Analyzers` package (λ1009), not in the analyzers
  bundled with Funcky.
- **Trimming/AOT**: `IsTrimmable` for net6+, `IsAotCompatible` for net7+
  (`Funcky/Funcky.csproj:37-43`); extensive `DynamicallyAccessedMembers(PublicParameterlessConstructor)`
  annotations on `Lazy`/`UpCast`/`Select`/`Sequence`/`Traverse` overloads, `[RequiresDynamicCode]`
  on the JSON converter factory, `ILLink.LinkAttributes.xml`, and an `AOT` constant.
- **Compatibility**: 9 TFMs; feature-flag constants (`INDEX_OF_CHAR_COMPARISONTYPE_SUPPORTED`,
  `GENERIC_PARSABLE`, `RANGE_SUPPORTED`, …) drive `#if` variants; `CompatibilitySuppressions.xml`
  + `EnablePackageValidation` + `Microsoft.CodeAnalysis.PublicApiAnalyzers` with
  `PublicAPI.Shipped.txt` (1 262 lines) / `PublicAPI.Unshipped.txt`.
- **Source generator**: internal `OrNoneFromTryPattern` incremental generator removes the
  hand-written repetition of ~200 parse bridges (attributes on the partial class,
  `ParseExtensions.Numbers.cs:5-16`; `Funcky.SourceGenerator/OrNoneFromTryPatternGenerator.cs`).
- **Analyzers**: see §7.
- **Docs**: Docusaurus site under `Documentation/src`; `option.md` and `try-pattern.md` contain
  stale samples (`Option<int>.None()` is no longer valid; `"1234".ParseIntOrNone()` is not the
  shipped name `ParseInt32OrNone`) — evidence of doc drift that the compiler error +
  code fix now papers over.

## 4. Analyzer and code-fix inventory

The 3.6.0 `Funcky` package bundles two analyzers and one code fix. The Roslyn-based
`Funcky.Analyzers` package (`PackageId` `Funcky.Analyzers`, version prefix 1.4.1,
`~/repos/funcky/Funcky.Analyzers/Funcky.Analyzers.Package/Funcky.Analyzers.Package.csproj:14-18`)
is a **separate** package and is not part of the pinned Funcky nupkg; it is surveyed from the
clone and marked accordingly.

### 4.1 Bundled in `Funcky` 3.6.0 (`analyzers/dotnet/cs`)

| ID | Class | Severity | Trigger | Code fix |
| --- | --- | --- | --- | --- |
| `λ0001` | `TryGetValueAnalyzer` | Error, enabled, configurable | `Option<T>.TryGetValue` used outside a loop condition, a `catch … when` filter, or an iterator `if` containing `yield`; also fires on method references | none |
| `λ0003` | `SyntaxSupportOnlyAnalyzer` | Error, enabled, `NotConfigurable` | reading a property annotated `[SyntaxSupportOnly("list pattern")]` — currently only `Option<T>.Count` and the indexer | none |
| — | `OptionNoneInvocationCodeFix` | fixes `CS1955` | `Option.None()` / `Option<T>.None()` invocation of the property | "Replace with property access" (`WellKnownFixAllProviders.BatchFixer`) |

Evidence: `~/repos/funcky/Funcky.Analyzers/Funcky.BuiltinAnalyzers/TryGetValueAnalyzer.cs:13-20`
and `:35,43,52-61,86`; `SyntaxSupportOnlyAnalyzer.cs:15-21` and `:36,40-50`;
`Funcky.BuiltinAnalyzers.CodeFixes/OptionNoneInvocationCodeFix.cs:17-20`, `:51-56`;
release history `Funcky.BuiltinAnalyzers/AnalyzerReleases.Shipped.md:8-9` and
`AnalyzerReleases.Unshipped.md:8,10-13`. λ0002 (`OptionNoneMethodGroupAnalyzer`) was shipped
in 2.7 and later removed in favour of the compiler error + code fix (commit `66fce815`).
The generated dump `inv-funcky-analyzers.md` lists the two analyzer classes and their
interface members only; the diagnostic descriptors are private fields and were read from
source (`TryGetValueAnalyzer.cs:12-21`, `SyntaxSupportOnlyAnalyzer.cs:12-22`), not from the
compiled `SupportedDiagnostics`.

Design details worth recording as prior art:

- λ0001 is `EditorBrowsable(Never)`-driven: the API remains callable for library-internal use,
  and the analyzer whitelists exactly three imperative shapes.
- λ0003 is `NotConfigurable`, i.e. consumers can neither suppress nor downgrade it; the
  message includes the syntax feature name from the attribute.
- The code fix is registered for a compiler error, not for Funcky's own diagnostic — a
  deliberate replacement for a deprecation analyzer.

### 4.2 Separate `Funcky.Analyzers` package (clone source; 1.4.x line)

| ID | Analyzer | Severity | Rejects |
| --- | --- | --- | --- |
| `λ1001` | `EnumerableRepeatOnceAnalyzer` | Warning | `Enumerable.Repeat(x, 1)` → prefer `Sequence.Return(x)` |
| `λ1002` | `EnumerableRepeatNeverAnalyzer` | Warning | `Enumerable.Repeat(x, 0)` → prefer `Enumerable.Empty<T>()` |
| `λ1003` | `UseWithArgumentNamesAnalyzer` | Warning | calls to `[UseWithArgumentNames]` methods without argument names (e.g. `Match(none:, some:)`) |
| `λ1004` | `JoinToStringEmptyAnalyzer` | Warning | `JoinToString(string.Empty)` → prefer `ConcatToString()` |
| `λ1005` | `AlternativeMonadAnalyzer` (GetOrElse) | Warning | `Match` shaped like `GetOrElse` |
| `λ1006` | `AlternativeMonadAnalyzer` (OrElse) | Warning | `Match` shaped like `OrElse` |
| `λ1007` | `AlternativeMonadAnalyzer` (SelectMany) | Warning | `Match(none: None, some: A)` shaped like `SelectMany` |
| `λ1008` | `AlternativeMonadAnalyzer` (ToNullable) | Warning | `Match` shaped like `ToNullable` |
| `λ1009` | `NonDefaultableAnalyzer` | Error | `default(T)`/`new T()` for `[NonDefaultable]` structs |
| `λ1010` | `OptionListPatternAnalyzer` | Error | `Option<T>` list patterns that test for more than one element (**unshipped**) |
| `λ1101` | `FunctionalAssertAnalyzer` | Warning | two-argument xUnit asserts that `FunctionalAssert` can express (**unshipped**) |

Evidence: `Funcky.Analyzers/Funcky.Analyzers/AnalyzerReleases.Shipped.md:6-24`,
`Funcky.Analyzers/Funcky.Analyzers/AnalyzerReleases.Unshipped.md:7-8`; descriptors
`AlternativeMonad/AlternativeMonadAnalyzer.*.cs:9-18`,
`NonDefaultable/NonDefaultableAnalyzer.cs:11-18`, `OptionListPatternAnalyzer.cs:11-18`,
`FunctionalAssert/FunctionalAssertAnalyzer.cs:15-24`; `DiagnosticName.cs:5-9`.
Code fixes exist for λ1001 (`EnumerableRepeatNeverCodeFix`, `EnumerableRepeatOnceCodeFix`),
λ1003 (`AddArgumentNameCodeFix`), λ1004 (`JoinToStringEmptyCodeFix`), λ1101
(`FunctionalAssertFix`) and a refactoring `OptionSomeWhereToFromBooleanRefactoring`
(`Funcky.Analyzers.CodeFixes/`). Packaging: multi-Roslyn builds (`Roslyn4.0` csproj variants),
`DevelopmentDependency=true`, `buildTransitive/Funcky.Analyzers.targets`, VSIX project for local
development, and release tracking via `AnalyzerReleases.*.md` with `RS2008`
(`Funcky.Analyzers/Funcky.Analyzers/Funcky.Analyzers.targets:41-48`).

### 4.3 Packaging assessment for Goal 21 (prior art)

1. Main-package analyzers give zero-friction adoption (no extra install, no opt-in) at the cost
   of expanding the main package's blast radius: an Error-by-default diagnostic changes the
   build for every consumer, and `NotConfigurable` removes the escape hatch. Funcky accepts
   that trade for λ0003 only.
2. Versioning is independent (`Funcky.Analyzers 1.4.x` vs `Funcky 3.6.0`), and
   `DevelopmentDependency=true` prevents analyzer packages from flowing transitively to
   consuming libraries.
3. Roslyn-version compatibility is handled by multi-targeting project files per Roslyn line
   (3.x for VS 2019 compatibility + 4.x), not by a single build.
4. Analyzer release tracking (`AnalyzerReleases.Shipped/Unshipped.md`) and analyzer test
   projects (`Microsoft.CodeAnalysis.CSharp.Analyzer.Testing`,
   `Funcky.Analyzers.Test/TryGetValueTest.cs:263`) are the verification discipline.
5. Naming uses a non-ASCII prefix `λ`; tooling that assumes `[A-Z][A-Z0-9]+` diagnostic IDs
   would need to be checked (**UNVERIFIED**: no evidence in the repo that any consumer tooling
   breaks on `λ` IDs; `#pragma warning disable λ0001` is used in Funcky's own tests,
   `TryGetValueTest.cs:263`, which shows the compiler accepts it).

## 5. Representative call sites (pinned)

SS = semantic statements (binding, call, or branch that expresses meaning; imports, braces and
formatting excluded). Hand-rolled comparisons are author-constructed minimal direct-C# versions
of the same behavior and are reproducible from the cited snippets.

1. **README Option pipeline** (`README.md:36-43`):
   ```csharp
   Option<string> input = ...;
   var result = input
       .SelectMany(v => v.ParseInt32OrNone())
       .Where(n => n >= 0)
       .Select(n => $"Non-Zero: {n}");
   result.AndThen(Console.WriteLine);
   ```
   Funcky: 5 SS (declaration + 3 combinators + `AndThen`). Direct C#:
   ```csharp
   string? input = ...;                                        // 1
   int? parsed = int.TryParse(input, out var n) ? n : null;     // 1
   if (parsed is int value && value >= 0)                       // 1
       Console.WriteLine($"Non-Zero: {value}");                 // 1
   ```
   Direct C#: 4 SS. Note: at a terminal site the BCL form is not longer; Funcky's win is that
   the pipeline remains a composable `Option<string>` *value* (matters when it is one step of a
   larger flow) and that `null`/absent is not re-derived at each step.
2. **README enumerable pipeline** (`README.md:51-57`): `Sequence.Return(1,2,3,4)` + 6 chained
   calls (`Pairwise`, `Intersperse`, `Inspect`, `SlidingWindow`, `WhereSelect`, `JoinToString`)
   = 7 SS. A minimal direct-C# equivalent needs four intermediate `List<T>` allocations,
   two index loops, one `Where`/`Average` pass and one `string.Join`: ≥ 14 SS.
3. **Try-pattern bridge** (`Documentation/src/try-pattern.md:39`):
   `Option<int> number = "1234".ParseInt32OrNone();` = 1 SS vs `int.TryParse` + `if` + out
   handling = 2–3 SS. In 3.6.0 the shipped name is `ParseInt32OrNone` (`inv-funcky.md:883`),
   while the doc still says `ParseIntOrNone` — naming drift in docs.
4. **Dictionary bridge** (`Funcky.Test/Extensions/DictionaryExtensionTest.cs:7-9`):
   `dictionary.GetValueOrNone(key: "some")` = 1 SS vs `TryGetValue` + `if` = 2 SS, and the
   value flows as `Option<TValue>` without an out variable. The test at `:20-24` records the
   `Dictionary<string,string>` dual-interface ambiguity that forced
   `[OverloadResolutionPriority(1)]` on the `IReadOnlyDictionary` overload.
5. **Retry** (`Funcky.Test/FunctionalClass/RetryTest.cs:9-13`, `:38-42`): `Retry(() => Option.Some(value))`
   = 1 SS; policy runs assert `MaxRetries + 1` producer calls, and the sync API blocks via
   `Thread.Sleep` (`RetryWithException.cs:9-10` documents this and points at the async package).
6. **Option LINQ** (`Documentation/src/option.md:34-36`):
   `from number in someNumber from date in someDate select Tuple.Create(number, date)` = 3 SS,
   using BCL query syntax rather than a Funcky DSL — the canonical-expression argument for
   `Select`/`SelectMany`/`Where`.
7. **JSON** (`Funcky.Test/Monads/OptionJsonConverterTest.cs`): requires an explicit
   `new JsonSerializerOptions { Converters = { new OptionJsonConverter() } }`; `None` round-trips
   as `null`, so `null` and absent are indistinguishable after deserialization.

## 6. Funcky choices that must not be inherited (explicit list)

1. `Result<T>` whose error channel is `Exception`, and `Result.Error` mutating the caller's
   exception stack trace (`Result.Core.cs:63-79`).
2. `Either`/`EitherOrBoth`/`Result` invalid `default` state with deferred
   `NotSupportedException` (runtime, not compile-time, detection) and an analyzer available only
   in a separate optional package.
3. Async `Memoize`/`IAsyncBuffer` ignoring the caller's cancellation token and creating the
   source enumerator without one (`AsyncEnumerableExtensions/Memoize.cs:57-65`).
4. Sync retry by `Thread.Sleep`; unbounded `Retry(producer)` with no policy; per-variant
   `Delay(retryCount)` index inconsistency; no jitter, cap, or `TimeProvider`.
5. Multi-TFM support that drags `System.Text.Json 5.0.2` / `System.Collections.Immutable 1.7.1`
   into old-TFM consumers.
6. `Option<T>.Count` + indexer existing only to enable list patterns and requiring an
   Error/`NotConfigurable` analyzer to prevent misuse.
7. `Match` overload pairs that differ only by value-vs-thunk (`none: TResult` vs
   `none: Func<TResult>`), which makes overload resolution depend on argument shape.
8. `Apply`/`Curry`/`Not`/`True`/`False`/`NoOperation` combinatorial overload families (up to
   8-arity; 81 `Apply` overloads; Greek-letter parameter names `ω1..ω8`).
9. `RequireClass<T>`/`RequireStruct<T>` public marker classes for `ToNullable` overload
   disambiguation (an internal trick that leaks into the public surface and XML docs).
10. `Reader<TEnvironment,TResult>` as a bare delegate carrier overlapping a full effect type
    (`Effect<TEnvironment,T>`) in FunnySharp.
11. `Discard.__` and the `λ`-prefixed diagnostic ID convention.
12. `FunckyImplicitUsings` MSBuild targets that inject four global usings into consumer compilations.

## 7. Not examined

- **Evidence-only gaps**
  - `Funcky.Analyzers` 1.4.1 **package** (nupkg) was not downloaded; its diagnostics are
    surveyed from clone source. Exact packaged file list and TFM layout of that nupkg are
    **UNVERIFIED**.
  - The bundled analyzer dump is metadata-mode and contains only the two analyzer classes and
    their interface members; diagnostic IDs and severities were read from source, not from the
    compiled `SupportedDiagnostics`.
  - `Funcky.Xunit` / `Funcky.Xunit.v3` / `Funcky.EntityFrameworkCore` / `Funcky.DiscriminatedUnion`
    (separate repositories/packages) were not surveyed; the task scope is the pinned Funcky
    package and clone.
  - The clone's `Funcky.Async` project was inspected only for packaging facts, not member-by-member.
  - `Documentation/src/case-studies/*` and most per-extension docs were not read; only
    `option.md`, `try-pattern.md`, `analyzer-rules/λ0001.md`, and README samples were used.
  - Behavior claims are source-read, not executed: no Funcky test suite was run in this
    workstream, and no benchmarks were measured.
  - Allocation/throughput of Funcky combinators (`Merge` immutable-list stepping, `Memoize`
    buffering, `Sequence` `ImmutableArray` building, parse generated code) is **UNVERIFIED**:
    no measurement was performed; source reading only.
  - Funcky's `netstandard2.0`/`netcoreapp3.1` assemblies were not dumped; findings about API
    availability per TFM are inferred from `FrameworkFeatureConstants.props` constants, not from
    those binaries.
