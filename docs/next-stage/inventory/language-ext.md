# LanguageExt.Core baseline inventory (stable 4.4.9 + v5-dev)

Goal 14 evidence. Survey date: 2026-09-17. This file is an agent-authored,
human-readable inventory; machine dumps are in `inventory/generated/`. All
nontrivial claims cite a pin: the extracted 4.4.9 package tree
(`/tmp/opencode/baselines/extracted/languageext.core.4.4/9/`), the generated
dumps under `docs/next-stage/inventory/generated/`, or the local clone
`~/repos/language-ext` (v5 development line).

The stable release and the v5 development line are strictly separated below.
Nothing in the v5 section is a compatibility target: v5 exists only as
unreleased `5.0.0-beta-*` builds and the clone's HEAD commit.

---

## 1. Provenance

| Item | Value | Source |
| --- | --- | --- |
| Package id / version | `LanguageExt.Core` **4.4.9** (stable; the historical `language-ext` package id is gone from NuGet) | `/tmp/opencode/baselines/extracted/languageext.core.4.4/9/LanguageExt.Core.nuspec` |
| `.nupkg` SHA256 | `633636d9d9cb9be75581910b503fb1aac54181a10a6b8cfe9d0a3f0118347437` | `/tmp/opencode/baselines/languageext.core.4.4.9.nupkg` (re-verified with `sha256sum` during this survey) |
| Assembly | `lib/netstandard2.0/LanguageExt.Core.dll`, AssemblyVersion `4.0.0.0` | dump header `inv-language-ext-toplevel.md`; `sha256sum` |
| DLL SHA256 | `0abfbbe708f176c95a9facc804207a6b9ecc394e7c10a49528c4dff147e9a8a0` | `/tmp/opencode/baselines/extracted/languageext.core.4.4/9/lib/netstandard2.0/LanguageExt.Core.dll` |
| XML docs SHA256 | `c621ccfd23d9b8c27f12da19158b27b7afa004ef42402162e96822ca184a8c21` (4.87 MB) | same directory `LanguageExt.Core.xml` |
| TFM | `netstandard2.0` only (no net6.0+/net10.0 asset) | nuspec `<group targetFramework=".NETStandard2.0">`; package `lib/` has one folder |
| License | MIT (`license type="expression"`, `licenseUrl` licenses.nuget.org/MIT) | nuspec |
| Author / project | Paul Louth; https://github.com/louthy/language-ext | nuspec |
| Source commit for the package | `1551a09271baf172abf845dd554fad43b31c0cbc` (tag **v4.4.9**, 2024-06-26, `git log` in the clone) | nuspec `<repository commit=...>`; verified `git cat-file -t` and `git log` in `~/repos/language-ext` |
| Package contents | only `lib/netstandard2.0/LanguageExt.Core.dll` + XML + icon. **No analyzers, no source generators, no build targets** | extracted tree listing |
| Stable line status | 4.4.9 is the newest stable on NuGet; the v5 line is published only as `5.0.0-beta-*` | `docs/next-stage/baselines.md`; clone tags `v5.0.0-beta-77` |

### 1.1 Dependencies (nuspec, all in the `netstandard2.0` group)

`Microsoft.Bcl.AsyncInterfaces 7.0.0`, `Microsoft.CSharp 4.7.0`,
`System.Diagnostics.Contracts 4.3.0`, `System.Linq 4.3.0`,
`System.Linq.Queryable 4.3.0`, `System.Memory 4.5.5`,
`System.Reflection.Emit 4.7.0`, `System.Reflection.Emit.Lightweight 4.7.0`,
`System.Threading.Tasks.Extensions 4.5.4`, `System.ValueTuple 4.5.0`
(all `exclude="Build,Analyzers"`).

Nine of the ten are legacy netstandard shims; two matter on modern TFMs:
`Microsoft.Bcl.AsyncInterfaces` (for `IAsyncEnumerable`/`IAsyncDisposable` on
netstandard2.0) and `System.Memory`. The package has no dependency on
`System.Text.Json`, `System.Collections.Immutable`, or any Microsoft.Extensions
package. `LanguageExt.Sys`, `LanguageExt.Parsec`, `LanguageExt.CodeGen`,
`LanguageExt.Transformers`, `LanguageExt.FSharp`, `LanguageExt.Rx` are separate
packages and are *not* dependencies of Core (upstream README at commit
`1551a092`, lines 69–79).

### 1.2 v5 development clone (unreleased; not a compatibility target)

| Item | Value |
| --- | --- |
| Clone | `~/repos/language-ext` |
| HEAD | `2f0e3628242889774d4141960a35671a0280051f`, 2026-07-29, "Merge pull request #1551 from pikammmmm/fix/typo-behviours" |
| `git describe --tags` | `v5.0.0-beta-77-11-g2f0e3628` (11 commits after tag `v5.0.0-beta-77`, 2025-12-30) |
| Working tree | clean before this survey (build artifacts `bin/`/`obj/` created by this survey; not committed) |
| `LanguageExt.Core/LanguageExt.Core.csproj` | `<TargetFramework>net10.0</TargetFramework>`, `<PackageVersion>5.0.0-beta-77</PackageVersion>`, `AssemblyVersion 5.0.0.0`, `Nullable enable`, PackageId `LanguageExt.Core` — read lines 8, 12, 27 |
| Built for evidence | `dotnet build LanguageExt.Core/LanguageExt.Core.csproj -c Release` with SDK 10.0.400 → `LanguageExt.Core/bin/Release/net10.0/LanguageExt.Core.dll`, SHA256 `a0f97431ec512c6a68423a697063b319897e461a4d73ba68b1946b2647a3ba98` |
| Layout divergence | The task brief said `src/LanguageExt.Core`; the clone has no `src/` — projects are at repository root (`LanguageExt.Core/`, `LanguageExt.Tests/`, `LanguageExt.Streaming/`, `LanguageExt.Sys/`, …; 10 csproj + `language-ext.sln` with 24 `Project(` entries) |
| v4 source availability in the clone | commit `1551a092` is present locally (tag `v4.4.9`) and is **not** an ancestor of `main`; `origin/v4-latest` is at `c8def85` (2024-08-21). v4 call sites can be read with `git show 1551a092:<path>` |

---

## 2. Evidence files and coverage

All dumps were produced with the repository tool
`eng/next-stage-inventory`. Metadata mode was used for the 4.4.9 dumps because
the package is a netstandard2.0 assembly whose transitive dependencies are not
extracted; `Microsoft.Bcl.AsyncInterfaces` 6.0.0 from the local NuGet cache was
passed via `--resolve-dir` for generic-type interface resolution. The 4.4.9
supplementary dumps were regenerated on 2026-09-17 with the operator-aware
build of the tool (see the two evidence-integrity notes below).

| Committed dump | Input | Filter | Types |
| --- | --- | --- | --- |
| `generated/inv-language-ext.md/.json` | 4.4.9 DLL | `^LanguageExt\.[A-Za-z]+$` (canonical, from the lead's `generate.sh`) | **159** non-generic types |
| `generated/inv-language-ext-toplevel.md/.json` | 4.4.9 DLL | `^LanguageExt\.[A-Za-z0-9_`+]+$` (supplementary: complete top-level namespace, all arities + nested) | **339** types |
| `generated/inv-language-ext-globalns.md/.json` | 4.4.9 DLL | `^[A-Za-z0-9_`+]+$` (global-namespace extension classes) | **49** types |
| `generated/inv-language-ext-typeclasses.md/.json` | 4.4.9 DLL | `^LanguageExt\.(TypeClasses\|Common\|Thunks\|DataTypes)\.` | **85** types |
| `generated/inv-language-ext-pipes.md/.json` | 4.4.9 DLL | `^LanguageExt\.Pipes\.` | **63** types |
| `generated/inv-language-ext-effects.md/.json` | 4.4.9 DLL | `^LanguageExt\.Effects\.` | **1** type (`HasCancel<RT>`) |
| `generated/types-languageext-4.4.9.txt` | 4.4.9 DLL | `--list-types` (all namespaces) | **1823** names |
| `generated/inv-language-ext-v5-toplevel.md/.json` | v5-dev build | `^LanguageExt\.[A-Za-z0-9_`+]+$` (unreleased; evidence only) | **444** types |
| `generated/inv-language-ext-v5-traits.md/.json` | v5-dev build | `^LanguageExt\.Traits\.` (unreleased; evidence only) | **150** types |

`generated/README.md` (lead-maintained) lists member, operator and extension
counts for every dump. Its summary rows for the supplementary `language-ext-*`
dumps predate the operator-aware regeneration of those files; measured from the
committed JSONs now, the regenerated dumps carry: `inv-language-ext-toplevel`
339 types / 10,966 members / **736 operators** / 2,939 extension members,
`inv-language-ext-typeclasses` 85 / 610 / 34 / 32, `inv-language-ext-pipes`
63 / 1,050 / 38 / 141, `inv-language-ext-globalns` 49 / 2,555 / 0 / 2,544,
and the canonical `inv-language-ext` 159 / 6,745 / 115 / 2,939. The counts
quoted below are from these regenerated dumps.

Supplementary regeneration command (same tool; the v5 dumps additionally
require the clone build and are recorded in `generated/README.md`):

```bash
dotnet run -c Release --no-build --project eng/next-stage-inventory/api-inventory.csproj -- \
  --assembly <LanguageExt.Core.dll> --mode metadata --core-dir <ref-pack>/net10.0 \
  [--resolve-dir <Microsoft.Bcl.AsyncInterfaces>/lib/netstandard2.0] \
  --include '<filter>' --out-md <out>.md --out-json <out>.json --title "<title>"
```

**Evidence-integrity note (counts).** The shared brief describes
`inv-language-ext.md` as covering "the 338 types in the top-level `LanguageExt`
namespace"; that number is not reproducible from the dump: its include regex
`^LanguageExt\.[A-Za-z]+$` matches **159** non-generic types.
`docs/next-stage/baselines.md` already records the dump as 159 types. The
complete LanguageExt namespace count is **339**, covered member-by-member by
`inv-language-ext-toplevel.md`. The sub-namespace counts below come from
`types-languageext-4.4.9.txt` and remain recorded by name only, as the lead
intended.

**Evidence-integrity note (operators).** A pre-operator build of the inventory
tool omitted `op_*` members on generic type definitions; a fresh build
(2026-09-17, same `Program.cs` as `eng/next-stage-inventory`) emits them in both
metadata and runtime mode. The 4.4.9 supplementary dumps in this directory were
regenerated with the fresh build (e.g. `LanguageExt.Option`1` now lists its 12
operators; the canonical 159-type dump lists 115 operator members per
`generated/README.md`), and a runtime-reflection probe against the pinned DLL
independently confirmed the operator sets; §3.14 records them. The v5
supplementary dumps predate operator support (`generated/README.md`), so they
are not operator evidence.

### 2.1 Type universe by namespace (4.4.9, from `types-languageext-4.4.9.txt`)

| Namespace | Types (incl. generic arities and nested) | Member dump |
| --- | --- | --- |
| `LanguageExt` (top level) | 339 | yes — `inv-language-ext-toplevel` |
| global namespace (no namespace!) | 49 | yes — `inv-language-ext-globalns` |
| `LanguageExt.ClassInstances` | 1229 | names only |
| `LanguageExt.Pipes` | 63 | yes — `inv-language-ext-pipes` |
| `LanguageExt.TypeClasses` | 60 | yes — `inv-language-ext-typeclasses` |
| `LanguageExt.Pretty` | 38 | names only |
| `LanguageExt.UnitsOfMeasure` | 16 | names only |
| `LanguageExt.Common` | 14 | yes — `inv-language-ext-typeclasses` |
| `LanguageExt.Thunks` | 6 | yes — `inv-language-ext-typeclasses` |
| `LanguageExt.DataTypes.Serialisation` | 5 | yes — `inv-language-ext-typeclasses` |
| `LanguageExt.Attributes`, `LanguageExt.Effects.Traits`, `LanguageExt.SomeHelp`, `LanguageExt.UnsafeValueAccess` | 1 each | `Effects.Traits` yes; rest names only |
| **Total** | **1823** | |

Surface-size facts (same dump, member counts):

- `LanguageExt.Prelude`: **2284 members** (2195 methods, 85 fields, 4 properties).
- Global-namespace extension classes add methods to BCL `IEnumerable<T>`:
  `ListExtensions` 254 methods (72 on `IEnumerable<T>`), `SeqExtensions` 203,
  `ArrExtensions` 168, `TryOptionAsyncExtensions` 161, `SetExtensions` 159,
  `TryAsyncExtensions` 104, `OptionAsyncExtensions` 97, `TryOptionExtensions` 92,
  `TaskOptionAsyncExtensions` 87, `TryExtensions` 80, `TryOptionExtensionsAsync` 75,
  `TaskTryOptionExtensions` 71, `TaskTryExtensions` 62, `TryExtensionsAsync` 57,
  `EitherExtensions` 52, `EitherUnsafeExtensions` 51, `WriterExtensions` 43,
  `OptionUnsafeExtensions` 41, `ReaderExt` 30, `EitherAsyncExtensions` 28, …
- `LanguageExt.AffExtensions` 353 members, `LanguageExt.AffT` 165,
  `LanguageExt.EffExtensions` 74, `LanguageExt.EffT` 19.

---

## 3. Capability survey — stable 4.4.9

Signature shorthand: `X: dump-name / member`. All signatures are quoted from
the dumps; XML doc summaries are from `LanguageExt.Core.xml` in the extracted
package.

### 3.1 F1 absence — Option and nullable/bridge types

| Capability | Pin / type | Members | Representative exact signatures |
| --- | --- | --- | --- |
| `readonly struct Option<A>` | 4.4.9 / `LanguageExt.Option`1` | 89 (77 methods/properties + 12 operators) | `public static LanguageExt.Option<A> None`; `public static LanguageExt.Option<A> Some(A value)`; `public LanguageExt.Option<B> Map<B>(System.Func<A, B> f)`; `Bind<B>(Func<A, Option<B>>)`; `B Match<B>(System.Func<A, B> Some, System.Func<B> None)`; `B Match<B>(Func<A,B> Some, B None)`; `A IfNone(A noneValue)` / `IfNone(Func<A>)`; `Unit IfSome(Action<A>)`; `Option<A> Filter(Func<A,bool>)`; `Where`, `Select`, `SelectMany`, `Exists`, `ForAll`, `Iter`, `Do`, `Count()`, `ToOptionUnsafe()` |
| `OptionAsync<A>` | 4.4.9 / `LanguageExt.OptionAsync`1` | 115 | `static OptionAsync<A> None`; `Task<bool> IsSome { get; }`; `Task<A> Value { get; }`; `OptionAsync<B> Map<B>(Func<A,B>)`; `MapAsync<B>(Func<A,Task<B>>)`; `OptionAsync<B> Bind<B>(Func<A,OptionAsync<B>>)`; `BindAsync<B>(Func<A,Task<OptionAsync<B>>>)`; `Task<A> IfNone(A)` / `IfNone(Func<A>)` / `IfNoneAsync(Func<Task<A>>)`; `Task<Unit> IfSome(Action<A>)`; implements `IAsyncEnumerable<A>` |
| `OptionUnsafe<A>` | 4.4.9 / `LanguageExt.OptionUnsafe`1` | 79 | mirrors `Option<A>` but permits `null` values (`MatchUnsafe`, `IfNoneUnsafe`, `IfSomeUnsafe`, `ToOptionUnsafe` back-conversion comes from `Option`) |
| `OptionAsync/Unsafe` bridges | 4.4.9 / `LanguageExt.OptionNone`, `OptionT`, `OptionAsyncT`, `NullableExtensions`, `OptionExtensions`, `OptionAsyncExtensions` (global ns), `OptionUnsafeExtensions` | — | `Option<T> Optional<T>(T value)` (null → None); `NullableExtensions` convert `T?`; `OptionT`/`OptionAsyncT` are transformer modules (`LanguageExt.OptionT`, `LanguageExt.OptionAsyncT` in the dump) |
| Prelude vocabulary | 4.4.9 / `LanguageExt.Prelude` | 94 `Some*`-matching and 90 `None`-matching members | `Some<A>(A value)`, `None`, `SomeAsync<T>(T)`, `SomeAsync<T>(Task<T>)` |
| Serialization | 4.4.9 / `Option<A>` bases | — | implements `System.Runtime.Serialization.ISerializable`; ctor `Option(IEnumerable<A>)` exists for deserialization |

Documented semantics: filtering an `Either` can produce `Bottom`
(`T:LanguageExt.Either`2`, XML remarks: "This may give unpredictable results
for a filtered value. The Either won't return true for IsLeft or IsRight."),
while `Option.Filter` simply yields `None` (`M:LanguageExt.Option`1.Filter`:
"None otherwise"). `Option<A>` is documented as a discriminated union of
`Some(a)`/`None` with available typeclass instances listed in its XML docs
(`T:LanguageExt.Option`1`: BiFoldable/Eq/Foldable/Functor/MonadPlus/Optional/Ord).

### 3.2 F2 fail-fast value outcome — Either/Fin/Try and Common

| Capability | Pin / type | Members | Representative exact signatures |
| --- | --- | --- | --- |
| `Either<L,R>` struct | 4.4.9 / `LanguageExt.Either`2` | 128 (87 non-operator + 41 operators) | `static Either<L,R> Left(L)` / `Right(R)`; `bool IsLeft/IsRight/IsBottom`; `Either<L,B> Map<B>(Func<R,B>)`; `MapLeft<B>(Func<L,B>)`; `Bind<B>(Func<R,Either<L,B>>)`; `BindLeft<B>(Func<L,Either<B,R>>)`; `Ret Match<Ret>(Func<R,Ret> Right, Func<L,Ret> Left, Func<Ret> Bottom)`; `R IfLeft(R rightValue)`; `L IfRight(L leftValue)`; `Filter(Func<R,bool>)`; left projections `LeftToSeq/LeftToList/LeftToArray` |
| `EitherAsync<L,R>` | 4.4.9 / `LanguageExt.EitherAsync`2` | 125 | `Task<bool> IsLeft { get; }` (awaitable state); `MapAsync`, `BiMapAsync`, `BindAsync`, `MatchAsync`, `IfLeftAsync`, `FilterAsync`; explicit `Bottom` |
| `EitherUnsafe<L,R>` | 4.4.9 / `LanguageExt.EitherUnsafe`2` | 92 | `Unsafe` variants (`MatchUnsafe`, `IfLeftUnsafe`, `IfRightUnsafe`) |
| `EitherStatus` | 4.4.9 / enum | — | `Left/Right/Bottom` |
| `Fin<A>` struct | 4.4.9 / `LanguageExt.Fin`1` | 78 (49 non-operator + 29 operators) | `static Fin<A> Succ(A)` / `Fail(Error)` / `Fail(string)`; `bool IsSucc/IsFail/IsBottom`; `Map`, `Bind`, `BiMap`, `BiBind`, `Match`, `IfFail`, `IfSucc`, `Iter`, `ThrowIfFail()`; selectors `Select/SelectMany` |
| `Try<A>` **delegate** | 4.4.9 / `LanguageExt.Try`1` | delegate `LanguageExt.Common.Result<A> Invoke()` | factory `Try<A>(Func<A> f)`, `Try<A>(A v)`, `Try<A>(Exception ex)` (`LanguageExt.Prelude` in `inv-language-ext-toplevel.md`); XML: "captures exceptions … to cancel the computation"; "To invoke directly, call x.Try()" |
| `TryAsync<A>` **delegate** | 4.4.9 / `LanguageExt.TryAsync`1` | delegate `Task<Common.Result<A>> Invoke()` | factories `TryAsync<A>(Func<Task<A>>)`, `TryAsync<A>(Task<A>)`, `TryAsyncSucc`, `TryAsyncFail` |
| `TryOption<A>` / `TryOptionAsync<A>` **delegates** | 4.4.9 / `LanguageExt.TryOption`1`, `TryOptionAsync`1` | delegates returning `Common.OptionalResult<A>` | three states: Some/None/Failure (XML `T:LanguageExt.TryOption`1`) |
| `Common.Result<A>` struct | 4.4.9 / `LanguageExt.Common.Result`1` | 25 | ctors `Result<A>(A)`, `Result<A>(Exception)`; `IsSuccess/IsFaulted/IsBottom`; `Bottom` |
| `Common.OptionalResult<A>` struct | 4.4.9 / `LanguageExt.Common.OptionalResult`1` | 31 | `Result` + `IsNone/IsSome/IsFaultedOrNone` |
| `Common.Error` | 4.4.9 / class | 38 (plus `Errors`/`ErrorException` 18, `ManyErrors` 24, `Expected`, `Exceptional`, `BottomError`, `BottomException`) | string/exception/`Error` composition used by `Fin`, `Eff`, `Aff` |
| Task bridges | 4.4.9 / global ns `TaskTryExtensions` 62, `TaskTryOptionExtensions` 71, `TaskOptionAsyncExtensions` 87, `TaskEitherAsyncExtensions` 14 | — | `Task<T>`-first overloads of the try/either families |

### 3.3 F3 accumulation — Validation

| Capability | Pin / type | Members | Representative exact signatures |
| --- | --- | --- | --- |
| `Validation<FAIL,SUCCESS>` struct | 4.4.9 / `LanguageExt.Validation`2` | 82 (68 non-operator + 14 operators) | `static Validation<FAIL,SUCCESS> Fail(Seq<FAIL>)`; `bool IsFail/IsSuccess`; `Map<Ret>(Func<SUCCESS,Ret>)`; `MapFail<Ret>(Func<FAIL,Ret>)`; `Bind<U>(Func<SUCCESS,Validation<FAIL,U>>)`; `BiMap`; `Disjunction<SUCCESSB>`; `Match(Action<SUCCESS>, Action<Seq<FAIL>>)`; `IfSuccess/IfFail`; `Filter` |
| `Validation<MonoidFail,FAIL,SUCCESS>` struct | 4.4.9 / `LanguageExt.Validation`3` | 81 (68 non-operator + 13 operators) | same shape but `FAIL` is combined through a `MonoidFail` class-instance (e.g. `MapFail<MonoidRet,Ret>`) |
| Transformer | 4.4.9 / `LanguageExt.ValidationT` | 106 methods (static) | lift/map/bind shapes for `Validation` over an inner monad |
| Sequence bridges | 4.4.9 / `LanguageExt.ValidationSeqExtensions`, `ValidationSeqGuardExtensions` (namespace `LanguageExt`) | — | collection traversal into `Validation` |
| XML semantics | `T:LanguageExt.Validation`2`: "Like `Either` but collects the failed values" | | |

Note: the failure channel is `Seq<FAIL>` in `Validation<FAIL,SUCCESS>`; the
three-parameter form replaces the sequence with an explicit monoid instance
type argument. `ValidationT` and the class-instance machinery are part of the
typeclass universe.

### 3.4 F4 function grammar — Prelude, Combinators, composition

- `LanguageExt.Prelude` 2284 members is the single largest surface. Locally
  renamed functions are lowercase: `map`, `filter`, `fold`, `pipe`, `curry`,
  `compose`, `lens`, `retry`, `use`, plus `Some`/`Left`/`Right`/`Success`
  constructors.
- Exact composition signatures: `B pipe<A,B>(A x, Func<A,B> f)`,
  `C pipe<A,B,C>(A x, Func<A,B> f, Func<B,C> g)`,
  `Func<T1,Func<T2,R>> curry<T1,T2,R>(Func<T1,T2,R> f)`,
  `Func<T1,T3> compose<T1,T2,T3>(Func<T1,T2> a, Func<T2,T3> b)`.
- `LanguageExt.Combinators`, `Combinators`1/`2/`3`, `CombinatorsDynamic`,
  `ComposeExtensions`, `Compositions`/`Compositions`1`, `CompositionsExt`
  (global ns) provide combinator plumbing; `Flip` exists as extension methods
  (`Func<B,Func<A,R>> Flip<A,B,R>`), there is **no** `Tap` in 4.4.9.
- Naming conventions observed: PascalCase on members (`Map`, `Bind`, `IfNone`,
  `Match`); lowercase on Prelude functions and lens-valued properties
  (`Seq<A>.head`, `Lst<A>.headOrNone`); constructors partly duplicated as
  English words (`Succ`/`Fail` in `Fin`, `Success`/`Fail` in `Validation`,
  `Some`/`None` in `Option`).
- Global-namespace extension classes (49) add `Map`, `Filter`, `Fold`, `Bind`,
  `Choose` to BCL `IEnumerable<T>` (`ListExtensions` in
  `inv-language-ext-globalns.md`), e.g.
  `IEnumerable<R> Map<T,R>(this IEnumerable<T> list, Func<T,R> map)`.

### 3.5 F5 collections and traversal

| Collection | Kind | Members | Selected exact signatures / notes |
| --- | --- | --- | --- |
| `Seq<A>` | struct | 91 | `Empty`, `Count`, `Head`, `Tail`, `Init`, `Tails`, `Inits`, `Length`, `Item(int)` (indexer), `static Lens<Seq<A>,A> head`; implements `ISeq<A>`, `IEnumerable<A>` |
| `Lst<A>` | struct | 61 | immutable list; `Add/AddRange/Append/Clear/At(int)→Option<A>`; implements `IReadOnlyList<A>`; lens properties `head`, `tail`, `headOrNone`, `tailOrNone` |
| `Arr<A>` | struct | 56 | immutable array; `Bind`, `Add`, `AddRange`, `Append`, `Item(int)`, `Length`; implements `IReadOnlyList<A>` |
| `Map<K,V>` / `Map<OrdK,K,V>` | struct | 142 / 138 | `Add`, `AddOrUpdate`, `Remove`, `Find`; `IEnumerable<ValueTuple<K,V>>`; lens `Map<int,Appt>.item(id)` used in v4 lens tests |
| `Set<A>` / `Set<OrdA,A>` | struct | 76 / 69 | `Add`, `AddRange`, `Bind`, `Max`, `Min`; `IReadOnlyCollection<A>` |
| `HashMap<K,V>` / `HashMap<EqK,K,V>` | struct | 140 / 136 | hash-keyed persistent map; `AddRange` overloads for `Tuple`, `ValueTuple`, `KeyValuePair` |
| `HashSet<A>` / `HashSet<EqA,A>` | struct | 61 / 62 | persistent hash set |
| `Que<A>` | struct | 27 | `Enqueue`, `Dequeue`, `DequeueUnsafe() → (Que<A>, A)`, `Peek` |
| `Stck<A>` | struct | 34 | `Push`, `Pop`, `Peek(Action<A>, Action)` |
| `SpanArray<A>` | struct | 15 | `New`, `Slice`, `Skip`, `Take`, `UnsafeCopy`; indexer with setter |
| `SeqLoan<A>` | class | 24 | pooled/loaned sequence (`Rent(int)`, `Dispose()`) |
| `SeqEmpty` | struct | 1 | `Default` |
| extension packages | global ns | `SeqExtensions` 203, `SetExtensions` 159, `ArrExtensions` 168, `HashMapExtensions` 57, `ListExtensions` 254 | LINQ-style `Aggregate`, `Map`, `Filter`, `Fold`, `Choose`, `Sequence`/`Traverse` for tuples |

There is no `Stream` or `StreamT` type in 4.4.9 (type list and dumps). There is
no `Iterable` type in 4.4.9 (that is v5 only). Collection traversal in 4.4.9 is
expressed through per-type instance methods plus the typeclass
`Foldable`/`Traversable` implementations in `ClassInstances`; `Sequence` exists
as tuple-arity overloads (global-ns dump, e.g.
`Option<(A,B)> Sequence(A,B)(this (Option<A>,Option<B>))`).

### 3.6 F6 async, streaming, concurrency primitives

- `OptionAsync<A>` and `EitherAsync<L,R>` return `Task`/`Task<T>` rather than
  `ValueTask` throughout; both expose `*Async` variants of every operation
  (e.g. `EitherAsync.BindAsync`, `BiFoldAsync` — 4 overloads).
- `Aff<A>` / `Aff<RT,A>` (`readonly struct`): `static Aff<A> Effect(Func<ValueTask<A>>)`,
  `static Aff<A> Effect(Func<ValueTask>)`, `EffectMaybe(Func<ValueTask<Fin<A>>>)`,
  `Success`, `Fail(Error)`, `Memo()`, `Timeout(TimeSpan)`,
  `WithRuntime<RT>()`, `Run() → ValueTask<Fin<A>>`,
  `RunUnit() → ValueTask<Unit>`, `Fold`, `Exists`, `ForAll`.
  `Aff<RT,A>.Fork() → Eff<RT, Eff<Unit>>` (`inv-language-ext-toplevel.md`).
- `AffExtensions` 353 members: `Retry`, `RetryWhile/Until`, `Repeat`,
  `RepeatWhile/Until`, `FoldWhile/Until`, `Catch`, `Timeout`,
  `Schedule`-driven variants.
- Cancellation is carried by the runtime type parameter only:
  `LanguageExt.Effects.Traits.HasCancel`1` supplies
  `CancellationToken CancellationToken { get; }`,
  `CancellationTokenSource CancellationTokenSource { get; }`,
  `RT LocalCancel { get; }`; every `Aff<RT,...>`/`Eff<RT,...>` extension is
  `where RT : struct, HasCancel<RT>`. The runtime-free `Aff<A>.Run()` takes no
  `CancellationToken` parameter (signature above).
- `Schedule` (class, 70 members): `Forever`, `Never`, `Once`, `Exponential(Duration,double)`,
  `Fibonacci(Duration)`, `Linear`, `Append`, `Intersect`, `Interleave`, `Bind`,
  `Filter`, transformers `Identity`, `NoDelayOnFirst`, `RepeatForever`.
- No `IAsyncEnumerable` operators beyond `OptionAsync<A> : IAsyncEnumerable<A>`;
  `System.Linq.AsyncEnumerable` did not exist at the 4.4.9 release.
- `LanguageExt.Pipes` 63 types (see §3.13) is the in-package streaming surface;
  it is built on `RT` runtimes and `Proxy<RT,UOut,UIn,DIn,DOut,B>` with arity 6.

### 3.7 F7 effects, resources, environment

- `Eff<A>` / `Eff<RT,A>` (`readonly struct`): `Effect(Func<A>)`,
  `EffectMaybe(Func<Fin<A>>)`, `Success`, `Fail`, `Memo()`, `ToAff()`,
  `ToAffWithRuntime<RT>()`, `WithRuntime<RT>()`; `Eff` XML summary: "Synchronous
  IO monad".
- There is **no `IO` type in 4.4.9** (type list and both dumps); IO effects are
  represented by `Eff`/`Aff`, and `IO` appears only in v5.
- Resource scoping is via `Prelude.use` (24 overloads, e.g.
  `Aff<R> use<H,R>(Aff<H> Acq, Func<H,Aff<R>> Use)`) and `Aff`/`Eff` extension
  `use` overloads; there is no `Bracket`/`Resource` type in the Core dump.
- Environment is `Eff<RT,A>`/`Aff<RT,A>` plus `HasCancel<RT>`; the DI-style
  `Has<M,TRAIT>`/`Local`/`Readable` traits from v5 do not exist in 4.4.9.
- `LanguageExt.Sys` (not a Core dependency) is the upstream package that wraps
  `System.*` as `Eff`/`Aff` (upstream README at `1551a092`, line 79).

### 3.8 F8 state transitions

- `State<S,A>`, `Reader<Env,A>`, `Writer<W,A>`, `RWS<MonoidW,R,W,S,A>` are
  **delegates**, not structs:
  `delegate (A,S,bool) Invoke(S state)` (`State`2`),
  `delegate ReaderResult<A> Invoke(Env env)` (`Reader`2`),
  `delegate (A,W,bool) Invoke()` (`Writer`3`),
  `delegate RWSResult<MonoidW,R,W,S,A> Invoke(R env, S state)` (`RWS`5`).
  The third tuple flag is the failure bit.
- `StateT`, `ReaderT`, `WriterT` are static transformer classes (23 methods
  each); `RWSExtensions` (global ns, 25), `StateExtensions` (22),
  `ReaderExt` (30), `WriterExtensions` (43).
- Result carriers: `StateResult` (5 statics: `Return`, `Fail`, `ToState`),
  `ReaderResult<A>` (struct, 30; `Match`, `IfFail`, `ToEither`, `ToEitherAsync`),
  `RWSState<W,S>` (struct, 4: `Output`, `State`, `IsFaulted`),
  `RWSResult<MonoidW,R,W,S,A>` (struct, 37; `New`, `Match`, `ToEither`, …).
- Concurrency types present in the type list but not member-dumped here:
  `Atom`, `AtomHashMap`, `AtomSeq`, `AtomQue`, `Ref`, `STM`, `VectorClock`,
  `VersionVector`, `VersionHashMap`, `TrackingHashMap`, `Patch`, `Change`,
  `Edit` (names in `types-languageext-4.4.9.txt`). These are well outside the
  FunnySharp family taxonomy and are recorded for completeness.

### 3.9 F9 optics

| Capability | Pin / type | Members | Exact signatures |
| --- | --- | --- | --- |
| `Lens<A,B>` | 4.4.9 / `LanguageExt.Lens`2` (readonly struct) | 6 | fields `Func<A,B> Get`, `Func<B,Func<A,A>> SetF`; `static Lens<A,B> New(Func<A,B> Get, Func<B,Func<A,A>> Set)`; `A Set(B value, A cont)`; `Func<A,A> Update(Func<B,B> f)`; `A Update(Func<B,B> f, A value)` |
| `Prism<A,B>` | 4.4.9 / `LanguageExt.Prism`2` | 10 | `New(Lens<A,B>)`, `New(Lens<A,Option<B>>)`, `New(Func<A,Option<B>> Get, Func<B,Func<A,A>> Set)`; `Set`, `Update` |
| Optional optics | 4.4.9 / `LanguageExt.Optional`, `OptionalAsync`, `OptionalUnsafe`, `OptionalUnsafeAsync`, `IOptional`, `IOptionalAsync` | — | `Optional` is a class holding getter/setter delegates; `IOptional` is the marker interface implemented by `Option<A>`; `IOptionalAsync` by `OptionAsync<A>` |
| Composition | 4.4.9 / `LanguageExt.Prelude` | 14 lens-matching members | `Lens<A,C> lens<A,B,C>(Lens<A,B> la, Lens<B,C> lb)`; `State<A,B> get<A,B>(Lens<A,B> la)` |
| Collection lenses | 4.4.9 / `Seq<A>`, `Lst<A>`, `Arr<A>`, `Map<K,V>` | — | static lens properties `head`, `headOrNone`, `tail`, `last`, `lastOrNone`; `Map<int,Appt>.item(id)` (used in `LensTests.cs` at `1551a092`) |
| Not present | `Traversal`, `Iso`, `Getter`, `Setter`, `Fold` (optics), `Optics` namespace | — | absent from the 4.4.9 type list; `Lens/Prism/Optional` is the whole optics surface |

The `Lens<A,B>` surface is delegate-based: the caller supplies getter and
setter; the library supplies composition and update, with no reflection or
property-path API (XML `T:LanguageExt.Lens`2`: "Primitive lens type for
creating well-behaved bidirectional transformations").

### 3.10 F10 HTTP integration

No ASP.NET Core types, attributes, `IActionResult` bridges, `ProblemDetails`
mappings, or middleware exist in `LanguageExt.Core` 4.4.9 (type list and both
dumps). Upstream keeps an out-of-tree `LanguageExt.AspNetCore` community
package (wiki Home, related projects, unpinned).

### 3.11 F11 cross-cutting

- **Naming.** PascalCase members; lowercase Prelude functions; `Succ`/`Fail`
  (Fin, Try) vs `Success`/`Fail` (Validation) vs `Some`/`None` (Option);
  duplicate vocabulary for the same concept across families
  (`Option.map` vs `Option.Map` vs `map(Option)`).
- **Namespaces.** 49 extension classes have **no namespace** (global scope) —
  they are found without a `using` when the assembly is referenced. This is
  discoverability noise and an overload-resolution hazard for BCL
  `IEnumerable<T>` code (proved by `ListExtensions.Aggregate(this Lst<T>, …)`
  next to `ListExtensions.Aggregate<TSource>(this IEnumerable<TSource>, …)`).
- **Serialization.** `Option<A>`, `Either<L,R>`, `EitherUnsafe<L,R>`, `Fin<A>`,
  `Validation<FAIL,SUCCESS>` and `Validation<MonoidFail,FAIL,SUCCESS>` implement
  `System.Runtime.Serialization.ISerializable` (their bases lists; the async
  carriers `OptionAsync`/`EitherAsync` do not);
  `LanguageExt.DataTypes.Serialisation` provides
  `EitherData<L,R>` (fields `State`/`Left`/`Right`, `StateType` enum) and
  `ValidationData<FAIL,SUCCESS>` (fields `State`/`Fail:Lst<FAIL>`/`Success`) plus
  `EitherDataExtensions` round-trip methods
  (`ToEither`, `ToEitherAsync`, `ToEitherUnsafe`, `ToFin`, `ToTry`, `ToTryAsync`).
  There is no `System.Text.Json` converter and no JSON-specific package
  dependency.
- **Nullability.** netstandard2.0 assembly; XML docs exist (4.87 MB) but the
  metadata dump cannot report nullable annotations (`--mode metadata`). Whether
  the v4.4.9 build has `<Nullable>enable` is **UNVERIFIED: the v4.4.9 csproj
  was not read; the netstandard2.0 assembly may carry Nullable attributes but
  the dump does not decode them.**
- **Diagnostics / analyzers.** None shipped. There is no Roslyn analyzer or
  source generator in the package (contrast: Funcky 3.6.0 ships analyzers).
- **Docs coverage.** Very broad XML doc coverage of core types; the dumps show
  `[ext]` attributes on extension methods and `[readonly struct]` on carrier
  types, which is enough for tooling.
- **Dependency boundary.** 10 package dependencies, none transitive to modern
  TFMs except `Microsoft.Bcl.AsyncInterfaces` and `System.Memory`; no
  `Microsoft.Extensions.*`, no serializer, no immutable-collections package.

### 3.12 HKT / typeclass machinery footprint (4.4.9)

- `LanguageExt.TypeClasses`: 60 types / 50 distinct names. Interface arities go
  up to **8** type parameters
  (`Applicative`8`, `ApplicativeAsync`8`, `BiFunctorAsync`6`, `MonadRWS`5`,
  `MonadTransAsyncSync`5`, …). Example: `Monad<MA,A>` exposes only
  `MA Return(A x)`; the real work is in `MonadAsync`4` (9 members) and the
  `Monad*` family; `Foldable`2`/`FoldableAsync`2` etc. carry the collection
  operators.
- `LanguageExt.ClassInstances`: **1229** types, the instance universe:
  `MOption`1/`2`, `MEither`2`, `MEitherAsync`2`, `MFin`1`, `MLst`1`, `MSeq`1,
  `MArr`1`, `MMap`2`, `MSet`1`, `MHashMap`2`, `MHashSet`1`, `MQue`1`, `MStck`1`,
  `MTry`1`, `MTryAsync`1`, `MTryOption`1`, `MTryOptionAsync`1`, `MTask`1`,
  `MValueTask`1`, `MValidation`2/`3`, `MReader`2`, `MState`2`, `MRWS`5`,
  `MEnumerable`1`, `MNullable`1`, `MUnit`, plus `Eq*`, `Ord*`, `Hashable*`
  instance families.
- The README (upstream, commit `1551a092`, lines 1766–1850) documents the
  encoding: instance interfaces such as
  `interface Monad<MA,A> { MB Bind<MonadB,MB,B>(MA ma, Func<A,MB> bind) where MonadB : struct, Monad<MB,B>; MA Return(A a); }`
  and calls written as `default(MonadB).Return(x)` — callers must thread the
  instance type argument explicitly, and mis-resolution surfaces as generic
  constraint failures.
- The remaining sub-namespaces that carry the machinery footprint:
  `LanguageExt.Common` 14 types (`Error`, `Errors`, `ErrorException`,
  `Result`, `OptionalResult`, `Expected`, `Exceptional`, `ManyErrors`,
  `ManyExceptions`, `BottomError`, `BottomException`, `ResultState`),
  `LanguageExt.Thunks` 6 (`Thunk`, `Thunk`1`, `Thunk`2`, `ThunkAsync`1`,
  `ThunkAsync`2`, `ThunkExt`), `LanguageExt.DataTypes.Serialisation` 5
  (`EitherData`, `EitherData`2`, `EitherDataExtensions`, `ValidationData`2`,
  `ValidationData`3`). All are dumped in `inv-language-ext-typeclasses.md`.
- Typeclass resolution has a runtime cost only where the struct instance is
  threaded through generic constraints; the README claims "it's type-safe, it's
  efficient" but no measurement is provided in the pinned artifacts
  (**UNVERIFIED: no benchmark data was examined for these paths**).

### 3.13 Pipes / streaming (4.4.9, in-package)

63 types under `LanguageExt.Pipes` (`inv-language-ext-pipes.md`):
`Proxy<RT,UOut,UIn,DIn,DOut,B>` (14 members: `Bind`, `Map`, `For`, `Reflect`,
`Observe`, `PairEachRequestWithRespond`, `ReplaceRequest`, `ReplaceRespond`),
wrappers `Producer<RT,OUT,A>` (28), `Consumer<RT,IN,A>` (29), `Pipe<RT,IN,OUT,A>` (31),
`Client<RT,REQ,RES,A>` (22), `Server<RT,REQ,RES,A>` (21), `Lift<RT,A>`,
`Effect<RT,A>`, `Enumerate`, `Queue`, `Void`, `Pure`, `Release`, `Request`,
`Respond`, `Use`. Everything is parameterised by an effect runtime `RT`; there
is no `StreamT`. This surface is a direct Haskell-pipes port and lives in Core
(adding to the Core surface and its type argument burden).

### 3.14 Operator and conversion surface

Operator counts are per declaring type, from the regenerated dumps
(`inv-language-ext-toplevel.md`, which now includes them) and independently
confirmed by runtime reflection against the pinned DLL. XML summaries are from
`LanguageExt.Core.xml`.

| Type | Operators | Safety-relevant examples | Documented behavior |
| --- | --- | --- | --- |
| `Option<A>` | 12 | `explicit operator A`; `implicit operator Option<A>(A)`; `implicit operator Option<A>(OptionNone)`; `op_True/op_False`; `op_BitwiseOr`; comparison operators | `Option` conversion docs absent; XML for the pattern exists in the upstream README (implicit Some/None construction, lines 425–460 at `1551a092`) but the `explicit` extraction operator is undocumented. **UNVERIFIED: runtime behavior of `(A)option` when None — not exercised here.** |
| `OptionAsync<A>` | 10 | `implicit operator OptionAsync<A>(A)` / `(Task<A>)` / `(OptionNone)`; `op_BitwiseOr`; equality and comparisons returning **`Task<bool>`** | not documented in the extracted XML |
| `Either<L,R>` | 41 | `explicit operator L` / `explicit operator R`; `implicit operator Either<L,R>(R)` / `(L)`; equality/comparison against `EitherLeft<L>`/`EitherRight<R>`; `op_True/op_False`; `op_BitwiseOr` (coalesce) | XML: "Explicit conversion operator from `Either` to `R` … Value, must not be null … `ValueIsNullException`" |
| `EitherAsync<L,R>` | 11 | `implicit operator EitherAsync<L,R>(L)`/`(R)`/`(Task<L>)`/`(Task<R>)`; equality/comparisons returning **`Task<bool>`**; `op_BitwiseOr` | not documented in the extracted XML |
| `EitherUnsafe<L,R>` | 15 | `explicit operator L` / `explicit operator R`; `implicit` from `L`/`R`/`EitherLeft<L>`/`EitherRight<R>`; `op_True/op_False`; comparisons | XML: "Value, must not be null … `ValueIsNullException`" |
| `Fin<A>` | 29 | `explicit operator A`; `explicit operator Error`; `implicit operator Fin<A>(A)` / `(Error)`; comparisons against `A` and `Error`; `op_True/op_False`; `op_BitwiseOr` | explicit operators not documented |
| `Validation<FAIL,SUCCESS>` | 14 | `explicit operator SUCCESS`; `explicit operator Seq<FAIL>`; `implicit operator Validation<FAIL,SUCCESS>(SUCCESS)` / `(FAIL)` / `(Seq<FAIL>)`; `op_True/op_False`; `op_BitwiseOr` | XML: "from `Validation` to `SUCCESS` … must not be null … `ValueIsNullException`" |
| `Validation<MonoidFail,FAIL,SUCCESS>` | 13 | same plus `explicit operator FAIL` | same |
| `Seq<A>` | 8 | `op_Addition` (concat), comparisons, `implicit operator Seq<A>(SeqEmpty)` | |
| `Lst<A>` | 11 | `op_Addition`, comparisons | |
| `Map<K,V>` | 25 | `op_Addition`, `op_Subtraction`, comparisons, and 19 `implicit operator Map<K,V>(ValueTuple<…>)` overloads from 1–8 pairs (nested 8-tuple forms included) | |
| `Set<A>` / `HashMap<K,V>` / `HashSet<A>` / `Que<A>` / `Stck<A>` | 9 / 21 / 5 / 5 / 5 | set/map algebra (`op_Addition`, `op_Subtraction`), ordering comparisons on `Set`, tuple-to-map implicit conversions on `HashMap`, `implicit` from `SeqEmpty` | |
| `Try<A>`, `TryOption<A>` | 0 (delegates) | | |

Consequences for assessment: the carriers are not merely union types; they are
arithmetic/boolean/comparison/conversion participants. `if (opt)`,
`(A)opt`, `a | b`, `a + b` on collections, and tuple-to-map implicit
conversion all compile. Only a fraction of these operators is documented in the
XML file (the conversion operators of `Option`, `Fin`, and the collection
operators are undocumented), the conversion operators drop the empty/failed
state through exceptions (`ValueIsNullException`), and the async carriers make
equality/comparison return `Task<bool>`, so `a == b` is a task that must be
awaited rather than a boolean.

---

### 3.15 Primitive/branded types

`LanguageExt.UnitsOfMeasure` (16 types: `Length`, `Mass`, `Time`, `Velocity`,
`Area`, `Temperature`, …) and `LanguageExt.Pretty` (38 types: `Doc`, layout
combinators) are present in the type list; `Prelude` exposes 85 fields, many of
them unit aliases (`m`, `cm`, `kg`, `hour`, `km2`, …). Not member-dumped beyond
the Prelude fields.

---

## 4. Representative call sites with semantic-LOC counts

Counts below are "meaning-bearing statements/expressions" in the shown snippet
(declarations, assertions and ceremony excluded), not raw line counts.

**C1 — Option matching (4.4.9 README, commit `1551a092`, lines 376–399).**
```csharp
int x = optional.Match(Some: v => v * 2, None: () => 0);   // 1 expression
int x = match(optional, Some: v => v * 2, None: () => 0);  // 1 expression (Prelude)
int x = optional.Some(v => v * 2).None(() => 0);           // 1 fluent chain
```
Idiomatic C# (nullable or `TryGetValue`): 1–2 statements
(`x = optional.HasValue ? optional.Value * 2 : 0;`). Net semantic-LOC effect
for a single step: none to negative; the gain appears when several dependent
steps share one absent-propagation path (`Map`/`Filter`/`Bind` chains).

**C2 — Collection pipeline (4.4.9 README, commit `1551a092`, lines 860–863).**
```csharp
var res = List(1, 2, 3, 4, 5).Map(x => x * 10).Filter(x => x > 20).Fold(0, (x, s) => s + x);
```
1 expression (3 chained operations) vs idiomatic LINQ/lists `Select`+`Where`+
`Aggregate` — also 1 expression in modern C# (`Enumerable`), so the semantic-LOC
gain over `LINQ` is ~0; the difference is the immutable carrier and the
Prelude-style free functions.

**C3 — Composed lens update (v4 test `LanguageExt.Tests/LensTests.cs:16–18` at
`1551a092`).**
```csharp
var bookEditorCarMileage = lens(Book.editor, Editor.car, Car.mileage);
var mileage = bookEditorCarMileage.Get(book);
var book2   = bookEditorCarMileage.Set(25000, book);
```
3 semantic lines for a 3-level update. Manual immutable C# with `with`-style
copying requires re-constructing each level (≥4 statements for this shape) or
mitigating with `record` `with` expressions (then ~1 statement:
`book with { Editor = book.Editor with { Car = book.Editor.Car with { Mileage = 25000 } } }`),
so the LOC advantage over modern C# records is **negative-to-neutral**; the
advantage is only for caller-provided abstractions that are not records.

**C4 — Retry/timeout on an effect (4.4.9 surface; `AffExtensions.Retry*`,
`Schedule.Exponential`).**
```csharp
var result = await ParseAsync().ToAff().Retry(Schedule.exponential(TimeSpan.FromMilliseconds(100), 2.0)).Run();
```
1 expression vs a hand-written retry loop with delay/backoff (≈8–15 statements,
plus correct exception/cancellation handling). Large semantic-LOC reduction,
with the caveats that scheduling and cancellation are carried by `Schedule` and
the runtime, and that `Run()` returns `ValueTask<Fin<A>>` — a language-ext
result carrier.

**C5 — v5 traits-based traversal (v5 clone, `LanguageExt.Tests/MemoryConsoleTests.cs:22–24`).**
```csharp
var xs = lines.Traverse(Either.Right<Unit, string>);
var comp = lines.Traverse(Console.writeLine).As();   // Traverse from traits
```
2 expressions for "sequence a list of effects", only available in the v5 dev
line via `LanguageExt.Traits.Traversable` (`inv-language-ext-v5-traits.md`,
`Traversable`1`: `Sequence`, `Traverse`, `TraverseM`). 4.4.9 has tuple-arity
`Sequence` overloads but not a general `IEnumerable` traverse in the dumped
surface.

**C6 — v5 LINQ over trait-compatible carriers (v5 clone, `LinqTests.cs:15–21`).**
```csharp
var res = from a in opt.ToIterable()
          from x in list.ToIterable()
          from y in x
          select a + y;
```
4 clauses; the same shape in BCL LINQ over nullable/lists needs explicit
guards/`SelectMany` (≈4+ statements). Available in v5 only, via `Iterable<A>`
and trait `SelectMany` operators.

---

## 5. v5 development line (`~/repos/language-ext` @ `2f0e362`, unreleased)

Everything in this section is **observed in the clone and its build output**.
v5 is published only as `5.0.0-beta-*`; it is not a compatibility target and
its APIs may still change.

### 5.1 Structural changes vs 4.4.9 (2f0e362)

| Area | 4.4.9 (stable) | v5-dev | Evidence |
| --- | --- | --- | --- |
| HKT carrier | none; struct class-instances + extra generic params | `LanguageExt.Traits.K<in F, A>` marker interface plus `K<in F,A,B>`; carriers implement it (`Option<A> : K<Option,A>`) | `LanguageExt.Core/Traits/K.cs:12–28`; `.../Option/Option.cs:24–31` |
| Typeclasses | 60 `TypeClasses.*` interfaces, resolved via `default(TInstance)` | 150 `LanguageExt.Traits.*` types using C# static abstract interface members (`public static abstract K<M,B> Bind<A,B>(K<M,A> ma, Func<A,K<M,B>> f)`) | `inv-language-ext-v5-traits`; `Traits/Monads/Monad/Monad.Trait.cs:12,22,32` |
| `Either<L,R>` | `readonly struct` | `abstract partial record` class; `IsLeft/IsRight`; implements `K<Either<L>,R>`, `K<Either,L,R>` | `.../Either/Either.cs:31–39` |
| `Fin<A>` | `readonly struct` | `abstract partial class`; `IsSucc/IsFail`, `SuccSpan/FailSpan` | `.../Fin/Fin.cs:17–21,50` |
| `Validation<F,A>` | `struct` + `Validation<MonoidFail,F,A>` | `abstract partial record` `Validation<F,A>` (+ nested `Success`/`Fail` records); the 3-arity form is gone, failure is `F` with monoid traits | `.../Validation/Validation.cs:36`; `inv-language-ext-v5-toplevel` |
| `Try<A>` | **delegate** `Result<A> Invoke()` | `record Try<A>(Func<Fin<A>> runTry) : K<Try,A>` | `.../Try/Try.cs:14–15` |
| `TryAsync`, `TryOption`, `TryOptionAsync` | delegates | `TryAsync`/`TryOption` removed from the built Core assembly (no source files; tests reference legacy names); transformer `TryT<M,A>` remains | v5 build dump (444 types) has no such names; `find` returned no `TryOption*.cs` |
| `OptionAsync`, `OptionUnsafe` | structs | **removed** from the built Core assembly (no source files) | v5 build dump; `find` returned no files |
| `Aff`, `Aff<RT>` | `readonly struct` | **not in the built Core assembly**; `Eff<A>`/`Eff<RT,A>` are the effect types; test file `AffTests.cs` is now an `Eff` test using `liftEff` | v5 build dump; `LanguageExt.Tests/AffTests.cs:1–12` |
| `IO` | absent | `abstract record IO<A>` with DSL (`IOCatch`, operators, extensions) — 72 members | `Effects/IO/IO.cs:31` |
| `Eff<A>`/`Eff<RT,A>` | structs with delegate-style `Effect`/`Run` | `record Eff<RT,A>(ReaderT<RT, IO, A> effect) : K<Eff<RT>,A>`; `record Eff<A>(Eff<MinRT,A> effect)`; implement traits (`Fallible`, `Alternative`, `Choice`, `Applicative`, …) | `Effects/Eff/Eff with runtime/Eff.cs:16`; `Effects/Eff/Eff no runtime/Eff.cs:19` |
| `State`/`Reader`/`Writer` | delegates | `record State<S,A>(Func<S,(A Value,S State)> runState) : K<State<S>,A>`; `record Reader<Env,A>(Func<Env,A>) : K<Reader<Env>,A>`; `record Writer<W,A>(Func<W,(A,W)>) : K<Writer<W>,A>` | `Monads/State and Environment Monads/State/State/State.cs:13`; `.../Reader/Reader/Reader.cs:12`; `.../Writer/Writer/Writer.cs:13` |
| `RWS` | delegate `RWS<MonoidW,R,W,S,A>` | `RWST<R,W,S,M,A>` (`RWST`4`/`RWST`5`) | `.../RWS/RWST/RWST.Module.cs:8`; v5 dump |
| Transformers | `OptionT`, `EitherT`, `FinT`, `TryT`, `ValidationT`, `StateT`, `ReaderT`, `WriterT` | all present as generic records/modules (`OptionT`1`, `EitherT`3`, `FinT`2`, `TryT`2`, `ValidationT`3`, `StateT`3`, `ReaderT`3, `WriterT`3) | v5 dump |
| Collections | `Seq/Lst/Arr/Map/Set/HashMap/HashSet/Que/Stck/SpanArray/SeqLoan` | plus `Iterable<A>`, `IterableNE<A>`, `Iterator<A>`, `IteratorAsync<A>`, `TrieMap`, `TrieSet`, `BiMap`; `Seq<A>` gains `ReadOnlySpan<A>` ctor, `Head` is `Option<A>`; `Lst` gets `ReadOnlySpan` ctor | `LanguageExt.Core/Immutable Collections/*`; v5 dump (`Iterable`1`, `Iterator`1`, `IteratorAsync`1`) |
| Streaming | `LanguageExt.Pipes` inside Core (63 types) | moved to package **`LanguageExt.Streaming`** (`RootNamespace LanguageExt.Pipes`); types `Source`/`Sink`/`Conduit` (+`T` effect variants) and `Transducers`; no `StreamT` type | `LanguageExt.Streaming/LanguageExt.Streaming.csproj`; `Streaming/Source/Source.cs:13`, `Sink/Sink.cs:14`, `Conduit/Conduit.cs:31`; v5 Core dump contains no `Pipes` types |
| Optics | `Lens`, `Prism`, `Optional*` | unchanged shape (`Lens<A,B>` struct 6 members; `Prism<A,B>` struct 8) — still no `Traversal`/`Iso`/`Getter`/`Setter`/optics `Fold` | v5 dump (`LanguageExt.Lens`2`, `Prism`2`); `Traits/Foldable/Fold.cs:15` is Foldable's fold-step type, not optics |
| Runtime DI | none | `Has<M,TRAIT>`, `Local<M,E>`, `Mutates<M,OUTER,INNER>`, `MonadIO<M>` with `Token`, `TokenSource`, `SyncContext`, `EnvIO` | v5 dump (`inv-language-ext-v5-traits`); upstream README at `2f0e362`, lines 239–266 |
| Value traits | none | `Traits/Domain`: `DomainType`, `Identifier`, `VectorSpace`, `Amount`, `Locus` (README marks them in-flux) | upstream README at `2f0e362`, lines 267–279 |
| TFM / version | netstandard2.0 | net10.0; `5.0.0-beta-77` | csproj lines 8, 12 |

### 5.2 v5 trait machinery shape

- `Monad<M>` (7 members) includes static abstract `Bind`, `Flatten`,
  `SelectMany`, and tail-recursion combinators `Loop`/`Recur`/`Done` over
  `LanguageExt.Next<A,B>` (`Traits/Monads/Monad/Monad.Trait.cs`; v5 traits
  dump).
- `Applicative<F>` has 16 members covering `Apply`, `Action`, `Actions`,
  `BackAction` and `Memo` overloads; `Foldable<T>` has **58** members
  (aggregation surface moved into the trait); `Traversable<T> : Functor<T>,
  Foldable<T>` adds `Sequence`, `Traverse`, `TraverseM`
  (`inv-language-ext-v5-traits.md`).
- Trait law helpers exist (`ApplicativeLaw`1`, `MonadLaw`1`, `AlternativeLaw`1`,
  `ChoiceLaw`1`, `EqLaw`1`, …) — i.e. the typeclass hierarchy includes its own
  law-testing kit.
- Instances are constraint-based: code is written
  `where M : Monad<M>, Foldable<M>` (v5 test
  `LanguageExt.Tests/EqualityTests.cs:172`) instead of v4's
  `default(MonadB).Bind(...)`.
- `MonadIO<M>` exposes `CancellationToken Token`,
  `CancellationTokenSource TokenSource`, `SynchronizationContext? SyncContext`
  as effect values — cancellation is modelled in the trait rather than as
  method parameters.
- `K<F,A>` is `interface K<in F, A>;` with no members
  (`Traits/K.cs:16`); carrier structs implementing it are boxed whenever they
  cross a `K<M,A>` boundary (e.g. `Option<A>` is a struct implementing
  `K<Option,A>`; the v5 `Monad<M>.Bind` signature takes `K<M,A>` and returns
  `K<M,B>`). Whether the JIT elides these boxes under generic instantiation is
  **UNVERIFIED: no measurement or disassembly was done**.

### 5.3 v5 call sites

- Traits-constrained generic code: `where M : Monad<M>, Foldable<M>`
  (`LanguageExt.Tests/EqualityTests.cs:172`), custom instance
  `public class Maybe : Monad<Maybe>` (`LanguageExt.Tests/TESTING.cs:239`).
- `IO` DSL: `IO.pure(5)`, `IO.fail<int>(Error.New("Failed IO"))`,
  `ioFunction.Apply(ioValue).Run()` (`LanguageExt.Tests/IOTests/ApplyTests.cs:18–22`).
- `Iterable` LINQ and `Traverse` (`LinqTests.cs:15–21`,
  `MemoryConsoleTests.cs:22–24`).
- Streaming samples live in `Samples/PipesExamples` and
  `LanguageExt.Streaming` (transducers) rather than Core.

---

## 6. Not examined

- `LanguageExt.ClassInstances` (1229 types) was not dumped member-by-member;
  only names/counts and representative instance names are recorded here.
- `LanguageExt.Pretty` (38), `LanguageExt.UnitsOfMeasure` (16),
  `LanguageExt.Attributes` (1), `LanguageExt.SomeHelp` (1),
  `LanguageExt.UnsafeValueAccess` (1) were not member-dumped.
- Concurrency/STM/atom types (`Atom`, `Ref`, `STM`, `VectorClock`,
  `VersionVector`, `VersionHashMap`, `TrackingHashMap`, `Patch`, `Change`,
  `Edit`) were not inspected beyond their names in the type list.
- `LanguageExt.Sys`, `LanguageExt.Parsec`, `LanguageExt.Megaparsec`,
  `LanguageExt.FSharp`, `LanguageExt.Rx`, `LanguageExt.CodeGen`,
  `LanguageExt.Transformers` were not surveyed (out of scope for the Core
  baseline; only their existence and package split is recorded).
- v5 `LanguageExt.Streaming`, `LanguageExt.Sys`, and samples were inspected only
  to the extent needed to establish that Pipes/streaming moved out of Core.
- No benchmarks were run; `LanguageExt.Benchmarks` in the clone was not
  executed or read. Upstream `Performance.md` and the wiki Performance page
  were not read.
- Upstream wiki pages beyond the Home page index were not read (unpinned,
  time-varying); no GitHub issue/discussion content was surveyed.
- NuGet download counts, release cadence, and consumer surveys were not
  examined.
- The v5 test project was not compiled; individual v5 test files were read as
  call-site evidence only. Some v5 test files (`AffTests.cs`) visibly lag the
  v5 type removals.
- `System.Text.Json`/serializer behavior of the `ISerializable` implementations
  was not tested; only the presence of serialization members and
  `DataTypes.Serialisation` carriers was recorded.
- Nullability annotations of the 4.4.9 assembly could not be decoded in
  metadata mode and the v4.4.9 csproj was not read.
