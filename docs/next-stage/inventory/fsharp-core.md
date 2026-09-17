# FSharp.Core 10.1.401 — capability inventory (Goal 14)

Scope: the pinned FSharp.Core baseline survey for funnySharp Goal 14. Evidence was collected
on 2026-09-17 against the extracted NuGet package and the repository copy of the generated
API dump. Citation aliases used throughout this document:

| Alias | Resolves to |
| --- | --- |
| `focus:NNN` | `docs/next-stage/inventory/generated/inv-fsharp-core-focus.md` line NNN (survey focus dump; the shared `inv-fsharp-core.md` produced by `generate-inventories.sh` uses a different filter and omits the `ValueOption` module) |
| `xml:NNN` | `/tmp/opencode/baselines/extracted/fsharp.core.10.1/401/lib/netstandard2.1/FSharp.Core.xml` line NNN |
| `csharp-out:NNN` | `/tmp/opencode/scratch/csharp-consumer-output.txt` line NNN (C# harness run output) |
| `harness` | `/tmp/opencode/scratch/fsharp-consumer/Program.cs` (C#, compiles and runs) |
| `fails` | `/tmp/opencode/scratch/fsharp-fails/fails/*.cs` (deliberately non-compiling C# probes) |
| `fs-samples` | `/tmp/opencode/scratch/fsharp-samples/Program.fs` (F#, compiled against the pinned DLL) |

The harness/sample projects are session-local scratch artifacts under `/tmp/opencode/scratch/`;
they are evidence, not committed deliverables.

## 1. Provenance and pin

| Item | Value | Source / check |
| --- | --- | --- |
| Package | `FSharp.Core` 10.1.401 | `FSharp.Core.nuspec` (extracted package) |
| nupkg | `/tmp/opencode/baselines/fsharp.core.10.1.401.nupkg` | brief pin, re-hashed `sha256sum` |
| nupkg SHA256 | `cf1f4e69fc2d1351af9fa9923ac90ed466088730a00867cbe0eb1b3b2c68963e` | `sha256sum` |
| Surveyed asset | `lib/netstandard2.1/FSharp.Core.dll` | package listing |
| DLL SHA256 | `39b0b7f06c11bedd93f94b6f101fffd645f1c4437a5a6d3ee79259f855aeb11a` | `sha256sum` |
| XML docs SHA256 | `54f72815016fc2906f3cb38cfb9f9eb89175ce0785832dc8eb5ad1f5e1e6bc72` | `sha256sum` |
| TFM in package | `netstandard2.0`, `netstandard2.1`; repo target is `net10.0` | `lib/` layout + nuspec dependency groups |
| Assembly identity | `FSharp.Core, Version=10.1.0.0` | `focus:3` |
| Package description | "FSharp.Core redistributables from F# Tools version 15.2.401 For F# 10.0" | `FSharp.Core.nuspec` |
| Upstream tag | `dotnet/fsharp` `v15.2.401` | `https://api.github.com/repos/dotnet/fsharp/tags` (2026-09-17) |
| Upstream commit | `36bf12fb8cadd1ce1e41e153e9e1cee1f51e9c2f` | tags API entry for `v15.2.401` |
| Version proof in source | `FSCorePackageVersionValue = 10.1.401` (FSMajorVersion 10, FSharp.Core minor forced to 1, FSBuildVersion 401); `FSToolsVersion 15.2.401` | `https://raw.githubusercontent.com/dotnet/fsharp/v15.2.401/eng/Versions.props` |
| nuspec repository commit | `dotnet/dotnet` VMR commit `e34a38d2ae1fc26406a317517196e55c68ff83ab` | `FSharp.Core.nuspec` |
| VMR cross-check | `src/fsharp/eng/Versions.props` at that VMR commit declares the same `10.1.401` / `15.2.401` values | `https://raw.githubusercontent.com/dotnet/dotnet/e34a38d2ae1fc26406a317517196e55c68ff83ab/src/fsharp/eng/Versions.props` |
| NuGet line | 10.1.401 is the newest 10.x stable; `11.0.100` stable and `11.0.101-*` previews/rc exist | `https://api.nuget.org/v3-flatcontainer/fsharp.core/index.json` (2026-09-17) |
| Docs pin (options) | git revision `e3ae3bdeecda6b7f300fe22a5fcd53e22d0c43c8` | `https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/options` |
| Docs pin (results) | git revision `e7add279618e90c6d3a3e379dd4d502e1017c88d` | `https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/results` |
| Docs pin (async expressions) | git revision `156931bb4ec1e81b028c76ea983553f2e9778bdd` | `https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/async-expressions` |

Provenance caveats:

- The nuspec `releaseNotes` URL
  `https://github.com/dotnet/fsharp/blob/main/docs/release-notes/.FSharp.Core/10.0.401.md`
  returns HTTP 404 (checked 2026-09-17). The `v15.2.401` tree contains release notes for
  `10.0.100.md`, `10.0.200.md`, `10.0.300.md`, `11.0.100.md`, and older files, but no
  `10.1.401.md`.
- The tag-to-VMR commit mapping is inferred from identical `eng/Versions.props` content
  (`10.1.401`), not by reading a submodule pin. **UNVERIFIED: exact `dotnet/dotnet` VMR →
  `dotnet/fsharp` commit ancestry for `e34a38d`; both declared versions agree.**
- 11.0.100 exists on NuGet but is the .NET 11 toolchain line. FunnySharp targets `net10.0`, so
  10.1.401 is the correct latest-10.x pin; 11.x was not surveyed.

### Dump regeneration and a filter correction

The repository's canonical FSharp.Core dump (`docs/next-stage/inventory/generated/inv-fsharp-core.md`,
102 types) is produced by `/tmp/opencode/tools/generate-inventories.sh`. Its filter entry
`ValueOptionModule` matches no type, so that dump omits every `Microsoft.FSharp.Core.ValueOption`
module function; the tool was also rebuilt mid-survey, which changed its output line layout.
This survey therefore cites a separate, byte-stable focus dump (`generated/inv-fsharp-core-focus.md`,
96 types, verified identical across two runs with the same command). The regeneration used:

```
dotnet run -c Release --no-build --project /tmp/opencode/tools/api-inventory -- \
  --assembly /tmp/opencode/baselines/extracted/fsharp.core.10.1/401/lib/netstandard2.1/FSharp.Core.dll \
  --include '^Microsoft\.FSharp\.(Core\.(FSharpOption|FSharpValueOption|FSharpResult|FSharpChoice|FSharpFunc|Unit$|OptionModule|ValueOption|ResultModule|LanguagePrimitives|Operators|ExtraTopLevelOperators|FuncConvert|OptimizedClosures|MatchFailureException)|Collections\.(SeqModule|ListModule|ArrayModule|SetModule|MapModule|FSharpList|FSharpSet|FSharpMap)|Control\.(FSharpAsync|FSharpMailboxProcessor|TaskBuilder|EventModule|IEvent))' \
  --out-md  docs/next-stage/inventory/generated/inv-fsharp-core-focus.md \
  --out-json docs/next-stage/inventory/generated/inv-fsharp-core-focus.json \
  --title "Public API inventory: FSharp.Core 10.1.401 (lib/netstandard2.1) — focus set"
```

## 2. F1 absence — `FSharpOption<T>`

Type shape (`focus:1248`): `class [sealed]`, implements `IEquatable<FSharpOption<T>>`,
`IStructuralEquatable`, `IComparable<FSharpOption<T>>`, `IComparable`, `IStructuralComparable`.
Generic parameter is not `class`-constrained; F# requires a null-compatible `'T` only for the
`Some null` hazard.

| Member (focus line) | Signature | C# consumer notes |
| --- | --- | --- |
| `:1250` | `public FSharpOption`1(T value)` | public ctor; accepts `null` payloads |
| `:1265` | `public static FSharpOption<T> Some(T value)` | allocates; no payload validation |
| `:1253` | `public static FSharpOption<T> None { get; }` | **the property returns `null`** |
| `:1254,1268` | `public T Value { get; }` / `get_Value()` | instance accessor; throws NRE on None (`xml:21857`, `csharp-out:16`) |
| `:1251–1252` | `IsNone` / `IsSome` emitted as **static** parameterized properties | C# cannot write `o.IsSome`; error CS1546 (`fails/f1_option_instance_issome.cs`) |
| `:1259–1268` | `Equals`, `GetHashCode`, `CompareTo`, `ToString` | structural `Equals` between non-null options; `Equals(object)`; `==` is reference equality |

Authoritative representation contract (`xml:21878`):

> "The type of optional values. When used from other CLI languages the empty option is the null
> value. … None values will appear as the value null to other CLI languages. Instance methods on
> this type will appear as static methods to other CLI languages due to the use of null as a
> value representation."

Observed C# semantics (`csharp-out:4–20`):

- `FSharpOption<int>.None is null` → `True`; `default(FSharpOption<int>) is null` → `True`.
- `FSharpOption<int>.None.ToString()` and `.GetHashCode()` both throw `NullReferenceException`.
- `some == FSharpOption<int>.Some(42)` → `False` (reference comparison), while
  `some.Equals(FSharpOption<int>.Some(42))` → `True` and
  `EqualityComparer<FSharpOption<int>>.Default.Equals(...)` → `True`.
- `OptionModule.GetValue(None)` throws `ArgumentException` (`xml:28461`, source
  `get = match None -> invalidArg` in `https://raw.githubusercontent.com/dotnet/fsharp/v15.2.401/src/FSharp.Core/option.fs`).
- No nullable reference-type metadata is emitted: the dump prints `(nullability: Unknown)` for
  `None`, `Value`, and most module signatures, so C# NRT analysis cannot see `None` as null.

`FSharpOption<T>` is a class; every `Some` allocates. Measured 24 bytes per `Some(int)` in the
C# harness (Section 14).

## 3. F1 absence — `FSharpValueOption<T>`

Type shape (`focus:1295`): `struct`, implements the same equality/comparison interfaces as
`FSharpOption<T>`.

| Member (focus line) | Signature | C# consumer notes |
| --- | --- | --- |
| `:1305` | `public static FSharpValueOption<T> ValueNone { get; }` | default value; `default(FSharpValueOption<int>).IsValueNone` → `True` (`csharp-out:29`) |
| `:1302,1315–1316` | `None`, `NewValueSome(T)`, `Some(T)` | `Some` and `NewValueSome` are equivalent factories |
| `:1304` | `public T Value { get; }` | **throws `InvalidOperationException`** on `ValueNone` (`csharp-out:36`), while `ValueOption.GetValue` throws `ArgumentException` (`csharp-out:37`) |
| `:1299–1300` | `IsValueNone` / `IsValueSome` | instance properties, normal C# access |
| `:1297–1298` | `IsNone` / `IsSome` | retained aliases, instance properties (`xml:21799`); two names for the same test |
| `:1303` | `Tag` | `ValueSome` = 1, `ValueNone` = 0 (`csharp-out:33`) |

`FSharpValueOption<T>` is a struct with no operator `==`/`!=`: `vSome == vNone` fails to compile
with CS0019 (`fails/f3_valueoption_eq.cs`). It has no nullable-annotation metadata either.

Module `Microsoft.FSharp.Core.ValueOption` (`focus:1972–2006`) mirrors `OptionModule`: `GetValue`,
`IsSome`, `IsNone`, `DefaultValue`, `DefaultWith`, `OrElse`, `OrElseWith`, `Count`, `Fold`,
`FoldBack`, `Exists`, `ForAll`, `Contains`, `Iterate`, `Map`, `Map2`, `Map3`, `Bind`, `Flatten`,
`Filter`, `ToArray`, `ToList`, `ToNullable`, `OfNullable`, `OfObj`, `ToObj`, `OfOption`,
`ToOption`. Implementation is at `src/FSharp.Core/option.fs` in `v15.2.401`; the source notes
that `ValueOption.defaultWith` deliberately omits `InlineIfLambda` because "benchmarked code ends
up slightly slower" on .NET 8 preview.

## 4. F1 module functions — `OptionModule`

All 28 module functions (`focus:1839–1872`) are static methods, take `FSharpFunc` delegates, and
put the function argument **first** and the option **last**. Representative signatures with
line numbers:

| Function | Signature (focus line) | Observed behavior |
| --- | --- | --- |
| `OfNullable` | `:1859` `FSharpOption<T> OfNullable<T>(T?? value)` | `null → None`, value → `Some` (`csharp-out:21`) |
| `ToNullable` | `:1868` `T? ToNullable<T>(FSharpOption<T>)` | `None → HasValue=false` (`csharp-out:23`) |
| `OfObj` | `:1861` `FSharpOption<T> OfObj<T>(T value)` | `null → None` (`csharp-out:26`) |
| `ToObj` | `:1870` `T ToObj<T>(FSharpOption<T> value)` | `None → null` (`csharp-out:27`) |
| `OfValueOption` / `ToValueOption` | `:1863` / `:1872` | carrier conversions between the two option types |
| `GetValue` | `:1852` | `None → ArgumentException` |
| `DefaultValue` / `DefaultWith` | `:1844` / `:1845` | eager value vs thunk evaluated only for `None` |
| `Map` / `Bind` / `Filter` / `Flatten` | `:1856` / `:1841` / `:1847` / `:1848` | short-circuit on `None` (source `option.fs` match) |
| `Map2` / `Map3` | `:1857` / `:1858` | curried `FSharpFunc` arguments; `None` if any input is `None` |
| `Count` | `:1843` | `None → 0`, `Some → 1`; `ToArray`/`ToList` likewise 0/1 elements (`:1866`, `:1867`) |
| `Fold`/`FoldBack`/`Exists`/`ForAll`/`Contains`/`Iterate` | `:1849–1850`, `:1846`, `:1851`, `:1842`, `:1855` | collection-shaped, `ForAll None = true` |
| `IsNone`/`IsSome` | `:1853–1854` | plain static predicates, the C#-friendly test |
| `OrElse` / `OrElseWith` | `:1864` / `:1865` | second option wins only when the first is `None` |

`OptionModule` accepts only `FSharpFunc` columns; a C# lambda cannot convert (CS1660, CS0411:
`fails/f2_option_lambda.cs`, `fails/f7_option_lambda_explicit.cs`). C# call sites require
`FuncConvert.FromFunc<...>(...)` at every callback (`harness` Section 1, `csharp-out:16`).

## 5. F2 fail-fast — `FSharpResult<T, TError>` and `ResultModule`

Type shape (`focus:1271`): `struct`; `IEquatable`, `IStructuralEquatable`, `IComparable`,
`IStructuralComparable`. Documentation: "Helper type for error handling without exceptions"
(`xml:21758`); the DU shape is `Ok of ResultValue:'T | Error of ErrorValue:'TError` with
`[<Struct>]` and structural equality (`https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/results`,
git `e7add279…`).

| Member (focus line) | Signature | Observed |
| --- | --- | --- |
| `:1287–1288` | `NewOk(T)` / `NewError(TError)` | the only public factories |
| `:1274–1275,1277` | `IsOk`, `IsError`, `Tag` | `Tag`: Ok 0, Error 1 (`csharp-out:41`) |
| `:1273,1276` | `ErrorValue`, `ResultValue` | **never case-checked**: `NewOk(1).ErrorValue` returns `null`; `NewError("bad").ResultValue` returns `0`; `default(Result).ErrorValue` returns `null` (`csharp-out:47–49`) |
| `:1281–1284` | `Equals` overloads | structural equality; no `==`/`!=` operator (CS0019, `fails/f4_result_eq.cs`) |
| default state | `default(FSharpResult<int,string>)` | `Tag=0`, `IsOk=true`, `ResultValue=0` (`csharp-out:44–46`) — an uninitialized struct is a *successful* result |

`ResultModule` (`focus:1874–1893`) has 18 functions: `Bind`, `Contains`, `Count`,
`DefaultValue`, `DefaultWith` (takes an `FSharpFunc<TError,T>`), `Exists`, `Fold`, `FoldBack`,
`ForAll`, `IsError`, `IsOk`, `Iterate`, `Map`, `MapError`, `ToArray`, `ToList`, `ToOption`,
`ToValueOption`. There is no `Ensure`/`Where`, `Recover`, `Zip`, `ZipWith`, `Map2`, `Swap`, or
async composition in `ResultModule`.

## 6. F3 accumulation — `FSharpChoice<...>`

`FSharpChoice<T1,T2>` … `FSharpChoice<T1..T7>` are abstract classes
(`focus:1042,1062,1085,1111,1140,1172`) with per-arity case classes nested in the generic type
(`Microsoft.FSharp.Core.FSharpChoice`2+Choice1Of2`, types listing) and `Tags` constant classes
(`focus:1895–1938`). Each arity exposes `IsChoice1OfN…IsChoiceNOfN`, `Tag`, `NewChoice1OfN…`, and
case-class `Item` properties. C# can test cases via the nested type names
(`c1 is FSharpChoice<int,string>.Choice1Of2`, `harness` Section 5, `csharp-out:58`), but the case
payload is only reachable through a cast. There is no `Validation`/accumulation type in
FSharp.Core; `Choice` is an active-pattern helper, not an error-accumulation type.

## 7. F4 function grammar — `FSharpFunc`, `FuncConvert`, `Operators`

- `FSharpFunc<T,TResult>` (`focus:1207`): abstract class, `Invoke(T)`,
  `FromConverter(Converter<T,TResult>)`, `ToConverter(FSharpFunc)`, and static `InvokeFast`
  overloads for 2–5 curried arguments (`:1212–1215`, XML `xml:22068`). `InvokeFast` is
  "more efficient application than applying the arguments successively" (`xml:22068`).
- `FSharpFunc<T1,T2,TResult>` … `FSharpFunc<T1..T5,TResult>` (`focus:1220–1247`) are abstract
  classes deriving from the curried nesting, with `Invoke` overloads and
  `Adapt(FSharpFunc<T1,FSharpFunc<T2,TResult>>)` returning the
  `Microsoft.FSharp.Core.OptimizedClosures+FSharpFunc<...>` fast-application type.
- `FuncConvert` (`focus:1325–1345`): `FromFunc`/`FromAction` (2–5 arity),
  `ToFSharpFunc(Converter<T,TResult>)`/`ToFSharpFunc(Action<T>)`, `FuncFromTupled`.
- `Operators` (`focus:1634–1835`) contains numeric/conversion helpers, `Fst`/`Snd`
  (`System.Tuple` based), `DefaultArg`, `Ignore`, `Using`, `Lock`, `TryUnbox`, `Identity`,
  `NameOf`, `Box`/`Unbox`, and pipe/compose functions. The dump lists `op_PipeRight`,
  `op_PipeRight2/3`, `op_PipeLeft`, `op_PipeLeft2/3`, `op_ComposeRight`, and `op_ComposeLeft`
  as public static methods on `Microsoft.FSharp.Core.Operators` (`:1794` for `op_ComposeRight`,
  `:1820–1825` for the pipe family; `op_PipeRight` carries `CompilationArgumentCountsAttribute`
  per reflection). They are callable from C#
  (`Operators.op_PipeRight(5, FuncConvert.FromFunc<int,int>(x => x + 1))` → `6`,
  `csharp-out:71`) but the F# operator syntax `|>`, `>>`, `<<` does not exist in C#.
- `Operators`/`ExtraTopLevelOperators` do not define a C#-usable pipeline or composition
  *grammar*; calling `op_PipeRight` is equivalent to `f(x)`.
- `Microsoft.FSharp.Core.Unit` (`focus:1967`) is a sealed class with no public constructor,
  `Equals`, and `GetHashCode`; `default(Unit)` is `null`. Many async APIs return
  `FSharpAsync<Unit>`.

## 8. F5 collections and traversal

| Type / module (dump) | Carrier shape | Representative signatures |
| --- | --- | --- |
| `FSharpList<T>` (`:151`) | sealed class, `IReadOnlyList<T>`, `IEquatable`, `IComparable`; ctor `(T head, FSharpList<T> tail)` (`:153`); `Empty` (`:154`), `Head`/`Tail`/`HeadOrDefault`/`TailOrNull` (`:155–163`), `Length`/`Tag`/`IsEmpty`/`IsCons`, indexer via `Item(int)` | `new FSharpList<int>(1, FSharpList<int>.Empty)` works from C# (`csharp-out:77`) |
| `FSharpList` static (`:147`) | `Create<T>(ReadOnlySpan<T>)` (`:149`) | span factory |
| `ListModule` (`:226`) | eager, returns `FSharpList<T>` or BCL types | `Map` (`:274`), `Choose` (`:234`), `Fold` (`:253`), `TryFind` (`:334`), `Zip` returns `FSharpList<Tuple<T1,T2>>` |
| `SeqModule` (`:427`) | lazy `IEnumerable<T>` | `Map` (`:481`), `Choose` (`:437`), `ChunkBySize` (`:438`), `TryFind` (`:540`), `Fold` (`:459`) |
| `ArrayModule` (`:9`) | eager arrays; nested `Parallel` module (`:385`) | `Map` (`:62`), `TryFind` (`:129`), `Partition` returns `System.Tuple<T[],T[]>` (`:76`) |
| `MapModule` (`:351`) | `FSharpMap<TKey,TValue>`, persistent sorted map; `IReadOnlyDictionary`/`IDictionary`/`IComparable`/`IStructuralEquatable` (`:184`) | ctor `IEnumerable<Tuple<TKey,TValue>>` (`:186`), `Add`, `Remove`, `Change` (`:193`), `TryGetValue` (`:200`), `TryFind` returns `FSharpOption<TValue>` (`:199`); `MapModule.Map` takes a `(key,value)` curried function |
| `SetModule` (`:555`) + `FSharpSet<T>` (`:206`) | persistent sorted set; `ICollection`/`IReadOnlyCollection` | `Union` (`:588`), `Intersect` (`:567`), `IsSubsetOf` etc. |

All module functions are function-first with `FSharpFunc` arguments; tuple-returning functions
return `System.Tuple<...>` (heap `Tuple`, not `ValueTuple`) and C# deconstruction works through
`System.TupleExtensions.Deconstruct` (`csharp-out:84`). `SeqModule` output is a lazy
`IEnumerable<T>`; `ListModule`/`ArrayModule` output is materialized.

## 9. F6 async, task CE, events

### `FSharpAsync` static class (`focus:620–657`)

Async values are `FSharpAsync<T>` objects that have **no members** (`focus:678`) and are not
awaitable (CS1061, `fails/f5_await_fsharpasync.cs`). The static surface includes:

| Group | Members (focus lines) | Contract notes |
| --- | --- | --- |
| Starters | `Start`, `StartImmediate`, `StartAsTask`, `StartImmediateAsTask`, `StartWithContinuations`, `StartChild`, `StartChildAsTask`, `RunSynchronously` (`:647–653`, `:643`) | `StartAsTask` "Executes a computation in the thread pool … If no cancellation token is provided then the default cancellation token is used" (`xml:17644`); `RunSynchronously` blocks the current thread and uses the default token if none is supplied (`xml:17800`) |
| Cancellation | `CancellationToken` async property (`:622`), `DefaultCancellationToken` (`:623`), `CancelDefaultToken()` (`:631`), `TryCancelled`, `OnCancel` (`:640`, `:657`) | ambient token is read with `let! ct = Async.CancellationToken`; the default token is a process-wide source that `CancelDefaultToken` replaces (`xml` for `CancelDefaultToken`) |
| Task bridges | `AwaitTask(Task)`, `AwaitTask<T>(Task<T>)`, `AwaitIAsyncResult`, `AwaitWaitHandle`, `AwaitEvent` (`:625`, `:627–630`) | one-way bridge in; the outbound bridge is `StartAsTask`/`StartImmediateAsTask` |
| Composition | `Sequential`, `Parallel` (with optional max degree), `Choice`, `Catch`, `Ignore`, `Sleep` (`:632–633`, `:639`, `:641–646`) | `Parallel` is fork/join and "If any child computation raises an exception … cancel the others … If cancelled … cancel any remaining child computations but will still wait for the other child computations to complete" (`xml` for `Parallel`); `Choice` returns the first `Some` and cancels siblings (`xml`) |
| Scheduling | `SwitchToThreadPool`, `SwitchToNewThread`, `SwitchToContext` (`:654–656`) | explicit thread-context switching is part of the runtime model |
| Continuations | `FromContinuations`, `FromBeginEnd`, `AsBeginEnd` (`:634–638`, `:624`) | the underlying CPS representation |

`Async.Catch` returns `FSharpChoice<T,Exception>` for *exceptions* only: "If this computation
completes successfully then return Choice1Of2 … If … raises an exception … return Choice2Of2"
(`xml` for `Catch`). Cancellation is delivered through the separate cancellation continuation
(`StartWithContinuations`, `:653`) or as a canceled `Task` from `StartAsTask`
(observed: pre-canceled token → `Status = Canceled`, await throws `TaskCanceledException`,
`csharp-out:95–97`).

### `FSharpAsyncBuilder` (`focus:659–672`) — not constructible from C#

The async computation-expression builder has `Bind`, `Combine`, `Delay`, `For`, `Return`,
`ReturnFrom`, `TryFinally`, `TryWith`, `Using`, `While`, `Zero`, but its only constructor is
`Assembly` (internal; reflection check) and there is no public builder instance anywhere in the
public surface. `new FSharpAsyncBuilder()` fails with CS1729 (`fails/f6_async_builder.cs`).
C# can compose async values only through the static combinators above; the `async { }` syntax is
compiler-inlined F# and unavailable.

### `TaskBuilder` / `task { }` (`focus:710–733`)

`TaskBuilderModule.task` and `.backgroundTask` are public static properties (`focus:730–731`), and
`TaskBuilder`/`TaskBuilderBase` execute `ResumableCode<TaskStateMachineData<T>, T>` values that
only the F# compiler emits (`focus:710–727`). `TaskBuilder`'s constructor is also `Assembly`
(internal; reflection check). The C# consumer uses native `async`/`await` instead; the F# docs
themselves say task expressions are "preferred when interoperating extensively with .NET
libraries that create or consume .NET tasks" (`async-expressions`, git `156931bb…`).

### `EventModule` / `IEvent` (`focus:598–618`, `:705`)

`EventModule` provides `Add`, `Choose`, `Filter`, `Map`, `Merge`, `Pairwise`, `Partition`, `Scan`,
`Split`, all taking `FSharpFunc` callbacks and `IEvent<TDelegate,TArgs>` (which extends
`IObservable<TArgs>`). The F# `event`/`IEvent` model is an alias over BCL events/observables; the
dump shows no C#-oriented adapter.

## 10. F8 agent boundary — `FSharpMailboxProcessor<TMsg>` (`focus:681–703`)

| Member (focus line) | Signature |
| --- | --- |
| `:683–684` | ctor `(FSharpFunc<FSharpMailboxProcessor<TMsg>, FSharpAsync<Unit>> body, bool isThrowExceptionAfterDisposed, FSharpOption<CancellationToken>)` (plus 2-arg overload) |
| `:695–699` | static `Start(body, token)`, `Start(body, isThrowExceptionAfterDisposed, token)`, `StartImmediate(...)` |
| `:688,689,692` | `Post(TMsg)`, `PostAndAsyncReply<TReply>(FSharpFunc<FSharpAsyncReplyChannel<TReply>,TMsg>, option timeout)`, `Receive(option timeout?)` |
| `:690,691,700` | `PostAndReply<TReply>` (blocks the calling thread), `PostAndTryAsyncReply`, `TryPostAndReply` |
| `:693,701,702` | `Scan`, `TryReceive`, `TryScan` |
| `:685,686,687,703` | `CurrentQueueLength`, `DefaultTimeout` (meaningful only for `PostAndReply` variants), `Dispose()`, `Error` event |

Contract (`xml:18785`): "The agent encapsulates a message queue that supports multiple-writers
and a single reader agent…"; `Receive` "will consume the first message in arrival order",
timeout raises `TimeoutException`, and "For each agent, at most one concurrent reader may be
active" (`xml:18684`). Cancellation is constructor-supplied (`FSharpOption<CancellationToken>`);
`PostAndReply`/`TryPostAndReply` perform sync-over-async blocking with `DefaultTimeout`.

Constructing a real agent body from C# is not practical: the body must be a
`FSharpFunc<..., FSharpAsync<Unit>>`, receive-loops require recursive composition through the
`async` builder, and the builder is unreachable (internal ctor). The harness demonstrates only a
one-shot body: `FSharpAsync.Ignore(mb.Receive(None))` with no loop (`harness` Section 9,
`csharp-out:104–105`).

## 11. F11 cross-cutting facts

### `LanguagePrimitives` (`focus:1411–1491`)

- Generic equality/comparison: `GenericEquality`, `GenericEqualityER` (NaN = NaN equivalence
  relation; `xml` for `GenericEqualityER`), `GenericComparison`, `GenericHash`,
  `GenericHashWithComparer`, `GenericLimitedHash`, `PhysicalEquality`, per-type
  `*Dynamic` operators.
- Comparer objects: `GenericEqualityComparer`, `GenericEqualityERComparer` (IEqualityComparer
  instances), `GenericComparer`; plus generic static factories
  `FastGenericEqualityComparer<T>()`, `FastGenericEqualityComparerFromTable<T>()`,
  `FastGenericComparer<T>()`, `FastGenericComparerFromTable<T>()`,
  `FastLimitedGenericEqualityComparer<T>(int limit)` (`:1437–1441`).
- Measured with `int`: 1M `GenericEquality(i,i)` allocated 208 bytes total and a 20M loop took
  153 ms in the final run (204–392 ms across three runs) vs 46 ms for
  `EqualityComparer<int>.Default` (58–94 ms across runs) (`csharp-out:115,117–118`,
  single-machine indicative measurement; see Section 14).

### Equality, hash, and structural comparison

`FSharpOption<T>`, `FSharpValueOption<T>`, `FSharpResult<T,TError>`, `FSharpChoice<...>`,
`FSharpList<T>`, `FSharpMap<K,V>`, and `FSharpSet<T>` all implement
`IStructuralEquatable`/`IStructuralComparable` and expose `Equals` overloads with a
`System.Collections.IEqualityComparer`; none of the surveyed struct types define
`operator ==`/`!=`, and `FSharpOption<T>.==` is reference equality.

### Serialization (System.Text.Json, .NET 10 runtime)

| Value | `JsonSerializer.Serialize` result (`csharp-out:120–126`) |
| --- | --- |
| `FSharpOption<int>.Some(1)` | `1` |
| `FSharpOption<int>.None` | `null` |
| `FSharpValueOption<int>.Some(1)` | `1` |
| `FSharpValueOption<int>.ValueNone` | `null` |
| `FSharpResult<int,string>.NewOk(1)` | throws `NotSupportedException`: "F# discriminated union serialization is not supported. Consider authoring a custom converter for the type." |
| `FSharpList<int>` `[1,2,3]` | `[1,2,3]` |
| `FSharpMap<string,int>` `{a=1}` | `{"a":1}` |

`Some(null)` and `None` both serialize to `null`, so option round-trips cannot distinguish them.

### Naming, argument order, and nullability

- Module functions are static classes with the F# name (`Map`, `Bind`, `Filter`, `Choose`,
  `TryFind`, `Fold`); `List.map` becomes `ListModule.Map`, etc. There are no LINQ-style extension
  methods, so C# code is `<Module>.<Function>(callback, value)`.
- Function arguments precede data arguments; multi-argument functions are curried through
  `FSharpFunc<T, FSharpFunc<T2, TResult>>`, and tupled arguments use `System.Tuple<...>`.
- Parameter names are mostly lost in the CLR metadata (`func`, `arg1`, …), and the assembly
  carries no `[Nullable]` metadata on this TFM; C# sees `(nullability: Unknown)` in the dump for
  the members that can actually return null.

## 12. C# consumption findings (compiled evidence)

1. **`None` is null and null is everywhere.** `FSharpOption<T>.None` is `null`; `ToString`,
   `GetHashCode`, and `Value` all throw `NullReferenceException` on it; `default` is null. NRT
   metadata is absent. (`csharp-out:4–5,11,14–16`; `xml:21878`; `fails/f1_option_instance_issome.cs`.)
2. **Instance members become static.** `IsSome`/`IsNone` on `FSharpOption<T>` are static
   parameterized properties; C# gets CS1546 and must call
   `FSharpOption<int>.get_IsSome(o)` or `OptionModule.IsSome(o)`. (`fails/f1…`.)
3. **Every callback needs a converter.** C# lambdas do not convert to `FSharpFunc`: CS1660 with
   explicit type arguments, CS0411 inference failure without them.
   (`fails/f2_option_lambda.cs`, `fails/f7…`; working form `harness` §1.)
4. **`==` is not structural and sometimes does not exist.** `FSharpOption<T> ==` compiles but
   compares references (`False` for two `Some(42)`); `FSharpValueOption<T>` and
   `FSharpResult<T,TError>` structs have no `==` at all (CS0019). Equality requires `.Equals` or
   `EqualityComparer<T>.Default`. (`csharp-out:11`; `fails/f3…`, `fails/f4…`.)
5. **`FSharpResult` accessors are not case-checked.** `NewOk(1).ErrorValue` returns `null`,
   `NewError("bad").ResultValue` returns `0`, and `default(FSharpResult<int,string>)` reports
   `IsOk = true` with `ResultValue = 0`. The compiler does not prevent reading the wrong case;
   the property silently returns `default`. (`csharp-out:44–49`.)
6. **`FSharpValueOption<T>.Value` and `GetValue` disagree on failure type**
   (`InvalidOperationException` vs `ArgumentException`), and `FSharpOption<T>.None.Value` is an
   NRE. There is no single documented absent-value failure mode across the three accessors.
7. **`FSharpAsync<T>` is not awaitable.** JS await requires `StartAsTask`/`StartImmediateAsTask`;
   the async builder cannot be constructed from C# (internal ctor), so composition from C# is
   limited to the static combinators. (`fails/f5…`, `fails/f6…`; `harness` §8.)
8. **Cancellation plumbing is token-optional and uses a shared default.** `StartAsTask`,
   `Start`, `RunSynchronously`, `MailboxProcessor` all accept
   `FSharpOption<CancellationToken>`; when omitted they use the process-wide default token that
   `CancelDefaultToken()` can cancel for unrelated work. A pre-canceled token produces a canceled
   `Task` (`Status = Canceled`), not a fault. (`xml:17644`, `xml:17800`; `csharp-out:95–97`.)
9. **Synchronous over async is part of the surface.** `Async.RunSynchronously` blocks the caller
   thread and may queue to the thread pool; `MailboxProcessor.PostAndReply` blocks with a timeout.
   (`xml:17800`; `focus:690`, `:643`.)
10. **Actor bodies are F#-shaped.** The `MailboxProcessor` body type requires a recursive
    `FSharpAsync<Unit>` built with the F# CE; a C# one-shot body compiles but a stateful loop
    cannot be expressed without the builder. (`focus:683`; `harness` §9.)
11. **Choice cases are nested and payload access is a cast.** `c1 is FSharpChoice<int,string>.Choice1Of2`
    compiles, but reading `Item` requires the case cast; there is no C# switch exhaustiveness or
    deconstruction. (`csharp-out:58–59`.)
12. **F# collections interoperate with BCL read interfaces.** `FSharpList<T>` is
    `IReadOnlyList<T>`, `FSharpMap<K,V>` is `IReadOnlyDictionary<K,V>`, and `SeqModule` returns
    `IEnumerable<T>`; `FSharpMap`/`FSharpSet` construct from `IEnumerable<Tuple<...>>`, but
    module functions still take `FSharpFunc` and function-first ordering. (`focus:151,184,206`;
    `csharp-out:74–90`.)
13. **JSON works for options/lists/maps and fails for Result/Choice.** `FSharpResult` throws
    `NotSupportedException` from `System.Text.Json` on .NET 10; `FSharpOption<int>.Some(1)`
    serializes as `1`, `None` as `null` (indistinguishable from `Some(null)`).
    (`csharp-out:120–126`.)
14. **Nullability is invisible.** No NRT attributes on `netstandard2.1`; `None`, `TryFind`,
    `TryGetValue` results are marked `Unknown`, so C# consumers get no compiler help.

## 13. Representative F# call sites and C# equivalents (semantic LOC)

`fs-samples` compiles and runs against the pinned 10.1.401 DLL
(`DisableImplicitFSharpCoreReference=true` plus an explicit `Reference`). The C# equivalents in
`ExamplesCSharp.cs` compile in the same harness. "Semantic LOC" here counts the meaning-bearing
operations/stages/arms of each snippet, excluding braces, formatting, and type signatures; the
counts are directional evidence for criterion 2, not a formal metric.

### A. Option normalization pipeline

F# (5): `Option.ofObj` → `Option.filter` → `Option.map` → `Option.defaultValue` inside one
binding (`fs-samples` lines 6–11).

```fsharp
let optionCallSite (input: string) =
    input
    |> Option.ofObj
    |> Option.filter (fun s -> s.Length > 2)
    |> Option.map (fun s -> s.Trim())
    |> Option.defaultValue "n/a"
```

C# with the FSharp.Core carrier (5) — every callback wrapped:

```csharp
var o = OptionModule.OfObj(input);
o = OptionModule.Filter(FuncConvert.FromFunc<string, bool>(s => s.Length > 2), o);
o = OptionModule.Map(FuncConvert.FromFunc<string, string>(s => s.Trim()), o);
return OptionModule.DefaultValue("n/a", o);
```

C# with modern nullable C# (1):

```csharp
public static string OptionCallSiteNullable(string? input) =>
    input is { Length: > 2 } ? input.Trim() : "n/a";
```

### B. `Option.map2` vs nullable arithmetic

F# (1): `Option.map2 (+) a b`. C# (1):
`a.HasValue && b.HasValue ? a.Value + b.Value : null`.

### C. Result railway

F# (8): 3 for the parse match (match + 2 arms), 2 for the parity check, 3 for
`parse |> Result.bind half |> Result.map`.

```fsharp
let process (s: string) =
    parse s |> Result.bind half |> Result.map (fun x -> x * 2)
```

C# with `FSharpResult` (10 counting `FuncConvert` wrappers; 8 ignoring them):

```csharp
public static FSharpResult<int, string> ProcessInput(string s) =>
    ResultModule.Map(
        FuncConvert.FromFunc<int, int>(x => x * 2),
        ResultModule.Bind(FuncConvert.FromFunc<int, FSharpResult<int, string>>(Half), Parse(s)));
```

### D. Cancellation-aware async

F# (5): `Async.CancellationToken`, `use`, `use!`, `Async.AwaitTask`, `return`.
C# (5): `using`, `using`, `await GetAsync`, `await ReadAsStringAsync`, `return`
(`ExamplesCSharp.FetchAsync`). The F# version still needs `StartAsTask` at the C# boundary, so
the boundary count for a C# consumer is higher.

### E. MailboxProcessor agent loop

F# (~13 semantic units in 16 physical lines, `fs-samples:40–55`): the message DU (1 + 2 cases),
the `MailboxProcessor.Start` binding, the `loop` recursion binding, `async`, `let! Receive`, the
match and its three arms, `reply.Reply`, and two `return! loop` calls. C# equivalent using
FSharp.Core: not expressible (no public async builder; see §12.7/§12.10). The BCL-first
substitute is a `Channel<T>` plus a worker loop (`ExamplesCSharp.Counter`, ~7 semantic
operations).

### F. Seq pipeline

F# (6): `Split` + `Seq.filter` + `Seq.countBy` + `Seq.sortByDescending` + `Seq.truncate` +
`Seq.toList`. C# LINQ (`ExamplesCSharp.TopWords`, 7 operations): `Split`, `Where`, `GroupBy`,
`OrderByDescending`, `Take`, `Select`, `ToList`. Comparable; the F# version returns an
immutable cons list, the C# version a `List<T>`.

### G. `task { }` vs `async`

F# (4): `task`, one `let!` over `Task.WhenAll`, `return`, the mapping lambda.
C# (4): `Task.WhenAll`, `await`, `Sum`, the selector lambda. Equivalent; C# needs no extra type.

## 14. Measured indicators (single machine, Release, indicative only)

| Measurement | FSharp.Core carrier | BCL equivalent | Ratio |
| --- | --- | --- | --- |
| Allocations, 1M constructions | `FSharpOption<int>.Some(i)`: 24,000,000 bytes | `int?`: 0 bytes | 24 B/op |
| Allocations, 1M 2-arg calls | curried `add.Invoke(i).Invoke(i)`: 32,000,000 bytes | `Func<int,int,int>`: 0 bytes; `InvokeFast`: 0 bytes | 32 B/op without fast-curry |
| 20M invocations | `FSharpFunc<int,int>.Invoke`: 100 ms (range 100–234 ms over three runs) | `Func<int,int>`: 108 ms (108–262 ms) | ~1× |
| 20M comparisons | `LanguagePrimitives.GenericEquality(i,i)`: 153 ms (153–392 ms) | `EqualityComparer<int>.Default`: 46 ms (46–94 ms) | 2.2–4.2× |
| 1M comparisons | `GenericEquality`: 208 bytes | `EqualityComparer<int>.Default`: 0 bytes | small but nonzero |

These are single-run microbenchmarks on the survey host (`csharp-out:108–118`); they are not
the repository's benchmark harness and should not be quoted as authoritative. They are enough to
show direction: absence as a class costs an allocation per value; curried partial application
costs an allocation per call unless `InvokeFast` is used; the F# generic equality path is slower
than BCL comparers.

## 15. Not examined

- F#-language-level facilities that have no CLR surface to survey: computation-expression syntax,
  `inline`/SRTP constraints (e.g. `Operators.Average$W` witnesses), active patterns, units of
  measure (`Microsoft.FSharp.Data.UnitSystems.SI.*`), quotations (`Microsoft.FSharp.Quotations`),
  reflection helpers (`Microsoft.FSharp.Reflection`), `QueryBuilder`/LINQ-to-objects query
  expressions, `NativeInterop`, `PrintfModule` formatting, and the compiler-services namespace.
- `lib/netstandard2.0/FSharp.Core.dll` was not diffed against `netstandard2.1`; the survey uses
  only the 2.1 asset.
- `FSharp.Core` 11.0.100 (and 11.0.101 previews) were confirmed to exist on NuGet but not
  downloaded or inspected.
- `dotnet/fsharp` source files beyond `eng/Versions.props` and `src/FSharp.Core/option.fs` were
  not read; implementation-level claims rest on the shipped XML docs, reflection, and observed
  behavior.
- `FSharp.Control.CommonExtensions` (Stream.AsyncRead/Write), `LazyExtensions`,
  `NullableModule`/`NullableOperators`, `Array2D/3D/4D` modules, `StringModule`,
  `ExtraTopLevelOperators`, `Checked` arithmetic, and `Unchecked` were outside the focus filter.
- Performance of `Async.StartAsTask`/`MailboxProcessor` under load, thread-pool growth, and
  cancellation propagation timing were not benchmarked; only deterministic state assertions were
  tested.
- Windows/macOS behavior and other SDK locales were not exercised; the harness ran on the
  survey host (Linux, .NET SDK 10.0.400 / runtime 10.0.11).
