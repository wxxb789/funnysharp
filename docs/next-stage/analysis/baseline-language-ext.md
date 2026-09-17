# Baseline analysis memo — LanguageExt.Core 4.4.9 (stable) and v5-dev `2f0e362` (Goal 14)

Scope: the pinned language-ext baselines only. Type-level facts and exact signatures are in
`docs/next-stage/inventory/language-ext.md`; raw surfaces are in
`docs/next-stage/inventory/generated/inv-language-ext*.{md,json}` and
`types-languageext-4.4.9.txt`. This memo gives **candidate** external-capability decisions for the
lead's `docs/next-stage/decision-record.md`. Decisions here are proposals, not final.

Provenance (short): `LanguageExt.Core` **4.4.9**, MIT, nupkg SHA256 `633636d9…47437`, only
`lib/netstandard2.0` (assembly 4.0.0.0); nuspec repository commit `1551a092` = tag **v4.4.9**
(2024-06-26). The local clone `~/repos/language-ext` HEAD `2f0e3628242889774d4141960a35671a0280051f`
(2026-07-29, `git describe` = `v5.0.0-beta-77-11-g2f0e3628`) is the **unreleased** v5 development
line; its `LanguageExt.Core` builds as net10.0 / `5.0.0-beta-77` and is never treated as a
compatibility target. Evidence classes: (E1) generated dumps of the pinned 4.4.9 DLL and of the v5
HEAD build; (E2) extracted package XML docs + nuspec; (E3) clone source at the v4.4.9 commit and at
v5 HEAD; (E4) runtime-reflection probe of the pinned DLL (`/tmp/opreflect`), which
independently corroborated the operator sets now present in the regenerated dumps; (E5) upstream README at both pinned commits and the wiki index (unpinned).

## 1. Baseline verdict in one paragraph

Language-ext is the most complete functional-programming surface in the survey (4.4.9: 1,823 types,
`Prelude` alone 2,284 members, 1,229 class-instance types, 10 package dependencies) and it is the
only baseline that attempts higher-kinded polymorphism in C#. That completeness is purchased with a
custom runtime (`Aff<RT>` + `HasCancel<RT>`), a typeclass/HKT hierarchy (`TypeClasses` 60 +
`ClassInstances` 1,229), a replacement collection universe (`Seq`/`Lst`/`Arr`/`Map`/`Set`/`HashMap`/
`HashSet`/`Que`/`Stck` plus 49 global-namespace extension classes that extend BCL `IEnumerable<T>`),
pervasive immutability, three overlapping failure carriers (`Either`, `Fin`, `Common.Result` plus
delegates `Try`/`TryAsync`/`TryOption`/`TryOptionAsync`), and a large implicit-conversion/operator
surface (`Either` alone has 41 operators; `Option<A>` has an undocumented `explicit operator A` and
`op_True`). Cancellation is modelled through the runtime type parameter rather than method
parameters: `Aff<A>.Run()` takes no `CancellationToken` (E1); `Try`/`TryAsync` capture
exceptions into a result value instead of propagating them (whether they capture
`OperationCanceledException` specifically is **UNVERIFIED**).
FunnySharp's existing surface already covers the useful capabilities canonically (absence, outcome,
accumulation, thin effects with explicit token, delegate lenses, traversal over BCL carriers, state
machines). This memo therefore recommends **no structural adoption**: adopt nothing from the runtime,
monad, collection, or typeclass layers; the only capability gap worth a maintainer decision is
effect retry/backoff/timeout (language-ext `Retry`/`Repeat`/`Timeout` + `Schedule`), and even there
the language-ext API shape should be rejected in favour of a minimal `TimeProvider`-based design if
it is adopted at all. The v5 line confirms the direction is *away* from a stable compatibility
target (types removed, carriers boxed behind `K<M,A>`, effects rebuilt on `IO`, streaming moved to
another package) and should not influence FunnySharp's public contract.

## 2. Candidate decisions

Decision vocabulary: **adopt** = take the capability and semantics largely as-is (into FunnySharp's
own API; no competitor dependency is in scope); **adapt** = take the capability, redesign the
API/semantics; **defer** = evidence insufficient or another workstream owns it; **reject** = do not
take. Criteria numbers are the brief's (1 usefulness, 2 semantic LOC, 3 signature clarity,
4 compile-time verifiability, 5 AI predictability, 6 BCL interop, 7 async/cancellation,
8 performance). "FunnySharp already has" cites the pinned 0.1.0 surface dump
(`generated/inv-funny-sharp-core.md`).

### F1 — absence

| # | Capability (pin) | Decision | Criteria | Rationale and evidence | Must NOT inherit |
| --- | --- | --- | --- | --- | --- |
| 1.1 | `Option<A>` Some/None with `Map`/`Bind`/`Match`/`IfNone`/`Filter` (4.4.9, inventory §3.1; 89 members incl. 12 operators) | **adopt** (as already present) | 1, 2, 3 | Every ordinary-C# absence case is covered by the existing `FunnySharp.Option<T>` (`Match`, `Map`, `Bind`, `Filter`, `OrElse`, `Zip`, `TryGetValue`); language-ext's equivalent is far larger (94 `Some`-matching Prelude members, `Case`/`MatchUntyped` union probing) without adding a capability. The capability is confirmed; the mechanism is not needed. | `Case` and `MatchUntyped*` union probing; 94-way constructor overloads; duplicate fluent `.Some(...).None(...)` matchers on top of `Match` |
| 1.2 | `Option<A>` implicit conversions and operators (E4: 12 operators: `implicit A→Option<A>`, `implicit OptionNone→Option<A>`, `explicit Option<A>→A`, `op_True/op_False`, equality/comparison operators, `op_BitwiseOr`) | **reject** | 3, 4, 5 | Assignment/branch/truthiness silently encode absence; `(A)option` is an undocumented unsafe extraction path; the conversion operators are absent from the XML docs (the operator set itself is now visible in the regenerated dumps), so readers and agents cannot infer behavior from signatures. FunnySharp's explicit `Some`/`None`/`FromNullable`/`ToOption`/`Match` vocabulary is the canonical one (0.1.0 surface). | All implicit conversions, truthiness, comparison and bitwise operators on absence carriers |
| 1.3 | `OptionAsync<A>` (115 members), `OptionUnsafe<A>` (79), `OptionT`/`OptionAsyncT` transformers (inventory §3.1) | **reject** | 3, 4, 5, 6, 7 | Three extra carriers for absence: `OptionAsync` re-expresses what FunnySharp already does with `MapAsync`/`BindAsync` on `Option<T>` (0.1.0 surface `OptionExtensions`), it returns `Task` (analysis below), implements `IAsyncEnumerable<A>`, and duplicates every operation name; `OptionUnsafe` permits `null` values, contradicting BCL nullability; transformers require the HKT hierarchy. | `OptionAsync`, `OptionUnsafe`, `OptionT`, `OptionAsyncT` carrier types and their `Async`-suffixed operator families |
| 1.4 | Null/`Nullable<T>`/dictionary bridges to absence (4.4.9 `OptionExtensions`/`NullableExtensions`; e.g. `Optional(T)`, `ToOption`) | **adopt** (as already present) | 1, 6 | FunnySharp already has `Option.FromNullable`, `ToOption`, `GetOption(IReadOnlyDictionary)`, `ToOptionAsync(Task)` (0.1.0 surface). This is the ordinary-C# bridge that matters; nothing new to take. | Nothing beyond the existing token/`ConfigureAwait` policy |
| 1.5 | Serialization of absence carriers (`Option<A> : ISerializable`, XML ctor `Option(IEnumerable<A>)`) | **reject** the mechanism | 5, 6 | Legacy `ISerializable` is not System.Text.Json; no converter or wire contract ships (inventory §3.11). FunnySharp's serialization policy stays separate from the carrier. | `ISerializable` on domain carriers; serialization ctors with enumerable-based "state" |

### F2 — fail-fast outcome

| # | Capability (pin) | Decision | Criteria | Rationale and evidence | Must NOT inherit |
| --- | --- | --- | --- | --- | --- |
| 2.1 | Explicit success/failure value with typed error (4.4.9 `Either`/`Fin`; FunnySharp `Result<TValue,TError>`) | **adopt** (as already present) | 1, 2, 3 | `Result<TValue,TError>` plus `Success`/`Failure`/`Map`/`Bind`/`Match`/`Recover*`/`Ensure`/`Zip` covers the capability (0.1.0 surface). Language-ext's `Either` adds no capability beyond `Result` when the left/right is an error channel, and its `Fin` is `Either<Error,A>` renamed. One canonical outcome shape serves criteria 5 and 2 better than three. | Parallel carriers (`Either`, `Fin`, `Common.Result`, `Common.OptionalResult`); `Bottom` states |
| 2.2 | `Either<L,R>` as a non-error two-value carrier | **defer** | 2, 5 | No FunnySharp call-site evidence produced in this workstream shows a need for a right-biased choice type distinct from `Result`; adding one now would create a second vocabulary for "two cases" and compete with `Result` in every signature (criterion 5). The lead should decide only if a concrete non-error use case exists. | `Either`-style `Left`/`Right` naming alongside `Result`'s success/failure in the same namespace |
| 2.3 | `Fin<A>` + `Common.Error`/`Errors`/`ErrorException` (4.4.9) | **reject** | 5, 6 | This is a library-specific error universe (`Error` carries messages/exception/code and composes through `ManyErrors`, `Expected`, `Exceptional`); adopting it would either duplicate `TError` or force FunnySharp to align vocabulary with a competitor's carrier. Criteria 5 and 6 also favour keeping errors caller-defined. | `Common.Error` family; `Fin`; `Fail(string)` stringly construction |
| 2.4 | `Try<A>`, `TryAsync<A>`, `TryOption<A>`, `TryOptionAsync<A>` delegates returning `Common.Result`/`OptionalResult` (inventory §3.2; XML: "captures exceptions") | **reject** | 3, 4, 5, 6, 7 | Four delegate aliases for "exception boundary", each with a different result carrier; `Try`/`TryAsync` have no `CancellationToken`; the delegate signature `Result<A> Invoke()` hides that invocation runs the side effect; `TryOption`'s three states duplicate `Option` + failure. FunnySharp already has a single boundary: `Result.Try`/`TryAsync`/`TryValueAsync` with an `errorMapper` and a `success`/`failure` value (0.1.0 surface). | All four `Try*` delegate types; `Common.Result`/`OptionalResult` as the boundary carrier; token-free Try boundaries |
| 2.5 | `Either.Filter`/`Where` producing **`Bottom`** (XML `M:LanguageExt.Either\`2.Filter`: "The Either won't return true for IsLeft or IsRight … IsBottom is True") | **reject** | 3, 4 | A third, invisible state that only appears after a filtering operation; the type system does not carry it and the XML tells callers to check for it. This is the clearest counter-example to compile-time verifiability in the baseline. | `Bottom`/`IsBottom` states on outcome carriers; `Match(..., Bottom)` overloads |
| 2.6 | Explicit extraction/comparison operators on outcome types (E4: `Either` 41 operators incl. `explicit operator L/R`; `Fin` 29; `Validation` 14 incl. `explicit operator SUCCESS`; XML documents `ValueIsNullException`) | **reject** | 3, 4, 6 | Conversions throw away the failure branch and are only partially documented; comparison/equality overloads against raw payloads and `EitherLeft`/`EitherRight` create 41-way surfaces whose behavior is not inferable from a signature. FunnySharp's non-throwing `TryGetValue`/`TryGetError`/`Match` path is the canonical access route. | Implicit conversions from payload/error into carriers; explicit throwing extraction; cross-type equality/comparison overloads |
| 2.7 | Exception-boundary capability (`Result.Try*` in FunnySharp vs `Try*` in language-ext) | **adopt** (as already present) | 2, 7 | Mapping exceptions to a typed failure at IO edges is the useful capability; FunnySharp's version is an explicit function returning `Result<TValue,TError>` with `errorMapper`, has `TryAsync`/`TryValueAsync`, and leaves cancellation policy to the caller. No language-ext mechanism is required. | Catch-all semantics that turn cancellation into a domain failure (language-ext `Try*` documents capture of exceptions generally; exact `OperationCanceledException` handling in 4.4.9 **UNVERIFIED: not exercised**) |

### F3 — accumulation

| # | Capability (pin) | Decision | Criteria | Rationale and evidence | Must NOT inherit |
| --- | --- | --- | --- | --- | --- |
| 3.1 | Accumulating validation with ordered error collection (4.4.9 `Validation<FAIL,SUCCESS>`; `Fail(Seq<FAIL>)`, `Disjunction`, `Match(Succ, Action<Seq<FAIL>>)`) | **adopt** (as already present) | 2, 3 | FunnySharp's `Validation<TValue,TError>` + `InvalidMany(IEnumerable<TError>)` + `Apply` + `Match(valid, IReadOnlyList<TError>)` is the same capability with a BCL error list; the semantic-LOC reduction over manual error plumbing is the main reason the family exists. | — |
| 3.2 | `Validation<MonoidFail,FAIL,SUCCESS>` — failure channel as a class-instance **type argument** (81 members; `MapFail<MonoidRet,Ret>` carries the instance) | **reject** | 4, 5 | Combining failures requires naming a monoid instance type at every call site and is resolved by generic constraint; misuse surfaces as constraint errors, not as a clear signature. FunnySharp's single-error-parameter form plus `InvalidMany` expresses the same result for ordinary C#. | Typeclass-instance type parameters in public signatures |
| 3.3 | `Seq<FAIL>` as the failure channel (4.4.9) and `ValidationData<FAIL,SUCCESS>` with `Lst<FAIL>` | **reject** | 3, 6 | This forces language-ext's replacement sequence type into every validation signature and its serialization DTO; FunnySharp already exposes `IReadOnlyList<TError>` (0.1.0 surface). | `Seq`/`Lst` in validation signatures or wire shapes |
| 3.4 | `ValidationT` transformer | **reject** | 4, 5 | Transformer requires the HKT/typeclass hierarchy; no ordinary-C# need. | `ValidationT` and every `*T` transformer |

### F4 — function grammar

| # | Capability (pin) | Decision | Criteria | Rationale and evidence | Must NOT inherit |
| --- | --- | --- | --- | --- | --- |
| 4.1 | Small function-grammar set: compose, curry, uncurry, partial, flip, pipe, tap (4.4.9 `Prelude`: `compose`, `curry` 9 overloads, `pipe` 8; `Flip` extensions) | **adopt** (as already present) | 1, 2 | FunnySharp `FunctionExtensions` already provides `Compose`, `ComposeAsync` (Task/ValueTask with token overloads), `Curry`, `Uncurry`, `Partial`, `Flip`, `Pipe`, `Tap`/`TapAsync`/`TapValueAsync` (0.1.0 surface) — a superset of the useful language-ext subset, with token-aware async forms. | Lowercase free functions (`map`, `filter`, `fold`), `Prelude`-style vocabulary |
| 4.2 | `LanguageExt.Prelude` as the free-function entry point (2,284 members; 85 unit fields; 174 `Eff`/179 `Aff` matches) | **reject** the surface; no adoption of its vocabulary | 3, 5 | A 2,284-member static class with overloads distinguished by delegate shape is not inferable for readers or agents; discovery requires guessing which of the 94 `Some`-matching members applies. FunnySharp's member-centric vocabulary keeps one canonical way per concept. | `Prelude`-style monolithic static surface; unit-alias fields (`m`, `cm`, `kg`, `hour`, `km2`, …) |
| 4.3 | 49 **global-namespace** extension classes adding `Map`/`Filter`/`Fold`/`Bind`/`Choose`/`Aggregate` to BCL `IEnumerable<T>` (`ListExtensions` alone: 254 methods, 72 on `IEnumerable<T>`; inventory §2, §3.4) | **reject** | 5, 6 | Referencing the assembly makes these extension methods candidates everywhere (global scope is the outermost searched namespace), so ordinary LINQ call sites can bind to language-ext overloads; the dump shows `Aggregate` overloads for both `Lst<T>` and `IEnumerable<T>` in the same class. This is a direct BCL-interop and AI-predictability hazard. | Extension classes without a namespace; BCL-carrier extension methods outside a named, opt-in namespace |
| 4.4 | `Combinators`/`CombinatorsDynamic`/`Compositions`/`Compositions`1` (tuple/composition plumbing) | **defer** | 1, 5 | No ordinary-C# consumer case was produced by this survey, and the naming does not map to a FunnySharp family. If a concrete call-site need appears, `FunctionExtensions` is the narrower home. | Dynamic/reflection-driven combinator helpers |

### F5 — collections and traversal

| # | Capability (pin) | Decision | Criteria | Rationale and evidence | Must NOT inherit |
| --- | --- | --- | --- | --- | --- |
| 5.1 | `Sequence`/`Traverse`/`Choose` over collection carriers with Option/Result/Validation (4.4.9 per-type + typeclass; FunnySharp sync/async/parallel versions) | **adopt** (as already present) | 2, 6, 7 | FunnySharp already sequences/traverses `IEnumerable` and `IAsyncEnumerable` for `Option`/`Result`/`Validation`, with `CancellationToken`-first `SequenceAsync`/`TraverseAsync`, bounded `TraverseParallelValueAsync`, and `Choose`/`ChooseValueAsync` (0.1.0 surface). This is the language-ext capability re-expressed on BCL carriers and it needs nothing from language-ext. | Traversal tied to a custom sequence type or to typeclass instances |
| 5.2 | Replacement collection universe: `Seq`, `Lst`, `Arr`, `Map`/`Map<OrdK,·,·>`, `Set`/`Set<OrdA,·>`, `HashMap`, `HashSet`, `Que`, `Stck`, `SpanArray`, `SeqLoan`, `ISeq` (inventory §3.5) | **reject** | 6, 8, 5 | Every carrier duplicates a BCL concept with different semantics (e.g. `Map` exposes `V Item(K key)` whose missing-key behavior is not visible in the signature; `Que.DequeueUnsafe` returns a tuple instead of a `TryDequeue` shape), forcing adapters and locking consumers into a parallel universe. Performance claims are not auditable from the pin (**UNVERIFIED: no benchmarks run; `Seq` is documented as "evaluate at-most-once" but no numbers were examined**). FunnySharp's contract already rejects a replacement collection universe and defers immutable updates to `System.Collections.Immutable`. | All language-ext collections and their extension classes; tuple-to-`Map` implicit conversions (19 overloads, inventory §3.14) |
| 5.3 | Lens-valued members on carriers (`Seq<A>.head`, `Lst<A>.headOrNone`, `Map<K,V>.item`) and `Prelude.get` | **reject** | 5, 3 | Attaching optics to collection types creates a second, type-specific way to read elements that does not generalize and forces every consumer to learn two access paradigms. | Per-carrier static lens properties |
| 5.4 | `Choose`/`GetOption`-style absence-aware lookup (4.4.9 `IEnumerable.Choose`; FunnySharp `GetOption(IReadOnlyDictionary)`) | **adopt** (as already present) | 1, 2 | Directly replaces `TryGetValue`/`FirstOrDefault` ambiguity on BCL carriers without a new collection type. | — |

### F6 — async, streaming, concurrency

| # | Capability (pin) | Decision | Criteria | Rationale and evidence | Must NOT inherit |
| --- | --- | --- | --- | --- | --- |
| 6.1 | Async operator parity on existing carriers (`MapAsync`/`BindAsync` with `Task` and `ValueTask`, token overloads) | **adopt** (as already present) | 3, 6, 7 | FunnySharp's async parity lives on the existing carriers (0.1.0 `OptionExtensions`, `ResultExtensions`) rather than in parallel `*Async` types; every token-carrying overload takes `CancellationToken` explicitly. Language-ext instead ships `OptionAsync`/`EitherAsync`/`TryAsync` **types** whose state is exposed as `Task`-typed properties (`Task<bool> IsSome`), so a state check becomes an async operation, and none of their operations take a token parameter. | `OptionAsync`/`EitherAsync`/`TryAsync` carriers; awaitable state properties; `Task`-returning `Run()` without a token |
| 6.2 | Effect retry / repeat / fold-until / backoff with schedules (`AffExtensions` 353 members: `Retry`, `RetryWhile/Until`, `Repeat`, `FoldWhile/Until`, `Schedule.Exponential/Fibonacci/Linear`, `Schedule` 70 members) | **defer**; if adopted, **adapt** a minimal subset | 2, 7, 8 | This is the largest semantic-LOC win in the baseline (inventory C4: one chained expression replaces an 8–15-statement retry loop with backoff), and FunnySharp has no equivalent operator (0.1.0 `Effect` has only `RunAsync`; timeouts exist only inside `FirstSuccessAsync`). But the language-ext shape violates criteria 3 and 7: `Aff<A>.Timeout(TimeSpan)` takes no `CancellationToken` or `TimeProvider` (E1), and `Schedule` is a 70-member DSL whose clock is hidden. Any adoption must be `Retry(attempts, delay, TimeProvider, CancellationToken)`/`Timeout(TimeSpan, TimeProvider, CancellationToken)` over `Effect`, measured against a hand-written loop (criterion 8) before shipping. | `Schedule`/`ScheduleTransformer` as public API; timeouts without a token/clock parameter; retry that swallows cancellation as a failure |
| 6.3 | Cancellation through runtime type-classes: `HasCancel<RT>` (`CancellationToken`, `CancellationTokenSource`, `LocalCancel`) and `where RT : struct, HasCancel<RT>` on every `Aff<RT>`/`Eff<RT>` operator; `Aff<A>.Run()` token-free | **reject** | 3, 4, 7 | Cancellation becomes a property of a type argument rather than a parameter; a caller reading `aff.Run()` cannot see whether cancellation is honoured or which token applies. FunnySharp's `RunAsync(CancellationToken)` on both `Effect` types is the canonical model. | Runtime traits; `HasCancel`; effect execution without an explicit token parameter |
| 6.4 | Runtime-free `Aff.Run()` returning `ValueTask<Fin<A>>` and `Fork()` (`Eff<RT, Eff<Unit>>`) | **reject** | 4, 5, 7 | Failure is baked into the effect carrier (`Fin`) instead of the caller's `Result`; `Fork` starts concurrent work with no bound, no token, and an `Eff`-nested result. FunnySharp's contract keeps failure explicit in `Result` and concurrency bounded. | Effect carriers with built-in failure state; unbounded `Fork` |
| 6.5 | Pipes/`Proxy<RT,UOut,UIn,DIn,DOut,B>` (63 types) and v5 `LanguageExt.Streaming` `Source`/`Sink`/`Conduit` (+`T` variants) | **reject** | 1, 5, 7 | Six-arity `Proxy` types and transducer pipelines are not ordinary-C# surface; BCL `IAsyncEnumerable<T>` + `Channel<T>` already provide bounded, cancellable streaming with a vocabulary agents know. v5's move of streaming to a separate package (and removal of `StreamT`) confirms even upstream does not consider it core. | `Proxy`/`Pipe`/`Producer`/`Consumer`/`Client`/`Server`; `SourceT`/`SinkT`/`ConduitT` |
| 6.6 | Bounded parallel mapping, first-success coordination, `TimeProvider` timeouts | not present in language-ext (no bounded/first-success operators; only `Aff.Fork`) | — | No external capability to decide; FunnySharp already has the stronger implementation (`SelectParallelValueAsync`, `FirstSuccessAsync` with `TimeProvider`) and keeps it. | `Fork`-style uncoordinated concurrency (covered by 6.4) |

### F7 — effects, resources, environment

| # | Capability (pin) | Decision | Criteria | Rationale and evidence | Must NOT inherit |
| --- | --- | --- | --- | --- | --- |
| 7.1 | Thin deferred effect with optional environment and explicit cancellation (FunnySharp `Effect<T>`/`Effect<TEnvironment,T>`; language-ext analogue `Eff<A>`/`Eff<RT,A>`) | **adopt** (as already present) | 3, 4, 7 | FunnySharp's effect is a `readonly struct` over `Func`/`Func<CancellationToken,·>` with `RunAsync(CancellationToken)` and `Provide(environment)`; language-ext's `Eff<RT,A>` needs a runtime type argument and a trait constraint, and `Eff<A>.Run()` has no token (E1). The capability is validated; the language-ext mechanism is not. | Runtime type parameters; token-free `Run`; effect-typed failure (`Fin`) |
| 7.2 | `Eff`/`Aff`/`IO` monads + `K<M,A>` + `LanguageExt.Traits` (v5: 150 trait types, `Monad<M>.Bind` as a static abstract interface member; core carriers implement `K<...>`; inventory §5) | **reject** | 4, 5, 6, 7, 8 | The trait layer requires every carrier to be an HKT participant and every generic function to be constrained (`where M : Monad<M>, Foldable<M>`); in v5 struct carriers such as `Option<A>` implement `K<Option,A>`, so crossing trait boundaries boxes them unless the JIT elides it (**UNVERIFIED: no measurement or disassembly; boxing mechanism is visible in the signatures**). Typeclass misuse surfaces as generic-constraint errors in v4 and as static-abstract-resolution errors in v5 — not as signature-level clarity. | `K<F,A>`, `Traits.*`, `Monad`/`Applicative`/`Foldable`/`Traversable`/`MonadIO`; v4 `TypeClasses`/`ClassInstances` |
| 7.3 | Runtime dependency-injection traits (`Has<M,TRAIT>`, `Local`, `Mutates`, `MonadIO.Token/TokenSource`, `LanguageExt.Sys` effects for `System.*`) | **reject** | 5, 6, 7 | This is a custom runtime and IoC story (`Has<RT, HasTime<RT>>`-style constraints) competing with the BCL `IServiceProvider`/`TimeProvider`; it also moves cancellation into effect values. FunnySharp keeps environment access to the caller's DI and passes `TimeProvider` explicitly. | Trait-based capability discovery; `LanguageExt.Sys` runtime model |
| 7.4 | Resource scoping (`Prelude.use`, `Aff`/`Eff` `use`; FunnySharp `Using`/`UsingAsync` over `IDisposable`/`IAsyncDisposable`) | **adopt** (as already present) | 2, 7 | Same capability with standard interfaces and token-aware ownership; nothing in language-ext's `use` improves on it. | — |
| 7.5 | `IO<A>` as a separate synchronous/asynchronous side-effect monad (v5 only; 72 members) | **reject** | 4, 5, 6 | Another carrier in the monad hierarchy; no released compatibility target; its DSL is only meaningful with the traits/`Eff` stack. | `IO<A>` and its DSL |

### F8 — state transitions and machines

| # | Capability (pin) | Decision | Criteria | Rationale and evidence | Must NOT inherit |
| --- | --- | --- | --- | --- | --- |
| 8.1 | `State`/`StateT`, `Reader`/`ReaderT`, `Writer`/`WriterT`, `RWS` (4.4.9 delegates; v5 records implementing `K<...>`) | **reject** | 1, 3, 5 | These solve language-level state threading that FunnySharp's `StateChange`/`StateTransition`/`StateMachine` already model for domain workflows (0.1.0 surface: `StateMachine<TState,TEvent,TOutput,TError>`, replay, `TransitionResult`). The tuple-flag delegate shape (`(A,S,bool) Invoke(S)`) hides failure in a bool; no consumer evidence supports adding monad transformers. | `StateT`/`ReaderT`/`WriterT`; tuple-flag result delegates; `RWSResult` |
| 8.2 | Atoms/STM/version vectors (`Atom`, `AtomHashMap`, `Ref`, `STM`, `VectorClock`, `VersionVector`, `VersionHashMap`, `TrackingHashMap`) | **reject** | 1, 6 | Out of the family taxonomy; competes with `System.Collections.Concurrent` and DI-scoped state; a separate concurrency runtime is explicitly out of scope. | All atomic/STM types |

### F9 — optics and immutability

| # | Capability (pin) | Decision | Criteria | Rationale and evidence | Must NOT inherit |
| --- | --- | --- | --- | --- | --- |
| 9.1 | Delegate-based `Lens<A,B>` with `New(Get,Set)`, `Get`/`Set`/`Update` and composition (`Prelude.lens`), caller-owned laws (4.4.9; v5 keeps the same shape) | **adopt** (as already present) | 2, 3, 4, 6 | FunnySharp `Lens<TSource,TFocus>` already has `Create`, `Get`, `Set`, `Update`, `Compose` and `Identity`, with no reflection, no property-path API, and no optics hierarchy (0.1.0 surface). Language-ext validates the shape rather than extending it. The 3-level composed lens is 3 semantic lines vs ≥4 statements of manual copying (inventory C3). | Attached lenses on collections; `Lens` as an inheritance/typeclass participant |
| 9.2 | `Prism<A,B>` / partial optics (4.4.9 `Prism<A,B>`: `Get : A → Option<B>` + `Set`; v5 unchanged) | **adopt** (capability already present as `FunnySharp.Optional<TSource,TFocus>`) | 1, 5 | FunnySharp's `Optional<TSource,TFocus>` is exactly the prism capability (`GetOption` + `Set` + `Compose` + `Update`); no new type or name is needed. Adding `Prism` alongside `Optional` would create two names for one concept. | A second partial-optic type; `Prism.New`'s three construction overloads (`Lens<A,B>`, `Lens<A,Option<B>>`, `Func`+`Func`) as a rival to `Optional.Create` |
| 9.3 | Optics hierarchy: `Traversal`, `Iso`, `Getter`, `Setter`, `Fold` | **reject** | 5, 1 | Not present in 4.4.9 or v5; explicitly excluded by the FunnySharp contract (`docs/product-contract.md`, "no optics hierarchy"). | Any optic beyond `Lens`/`Optional` |
| 9.4 | Immutable-by-default carriers and structural-sharing collections (`Seq`, `Lst`, `Map`, `HashMap`, …) as the recommended data types | **reject** | 6, 8, 5 | Pervasive immutability is explicitly rejected in the goal and contract; `System.Collections.Immutable`/`Frozen*` remain the update mechanism and callers choose where to pay for it. | Immutable-first collections; patch/diff types (`Patch`, `Change`, `Edit`) as public policy |

### F10 — HTTP integration

| # | Capability (pin) | Decision | Criteria | Rationale and evidence | Must NOT inherit |
| --- | --- | --- | --- | --- | --- |
| 10.1 | HTTP/ASP.NET integration | **not present** in LanguageExt.Core 4.4.9 or v5 Core (inventory §3.10) | — | Nothing to adopt. The community `LanguageExt.AspNetCore` package is not part of the pin and is out of scope; FunnySharp.AspNetCore stays the canonical HTTP surface. | Any HTTP adapter or ProblemDetails mapping sourced from language-ext types |

### F11 — cross-cutting

| # | Capability (pin) | Decision | Criteria | Rationale and evidence | Must NOT inherit |
| --- | --- | --- | --- | --- | --- |
| 11.1 | Canonical vocabulary (`Option`, `Result`, `Validation`, `Effect`, `Lens`, `Optional`, `StateMachine`) | **adopt** (keep the existing vocabulary; no language-ext naming concession) | 3, 5 | Language-ext offers a competing vocabulary per concept (`Some`/`None`, `Succ`/`Fail`, `Success`/`Fail`, `Fin`, `Eff`/`Aff`/`IO`, `head`/`headOrNone`). Adopting any of it would fragment the one-name-per-concept rule that criterion 5 depends on. | `Succ`/`Fail`, `Fin`, `Eff`/`Aff`, lowercase free functions, `*T` transformer suffixes |
| 11.2 | Serialization model (`ISerializable` on carriers; `DataTypes.Serialisation.EitherData/ValidationData` + `ToEither/ToFin/ToTry/…` conversion zoo) | **reject** | 5, 6 | No System.Text.Json integration ships in 4.4.9; the DTOs mirror the union shapes rather than documenting a wire contract. FunnySharp keeps serialization policy separate (F11) and error/value shapes STJ-friendly. | `ISerializable` carriers; `*Data` DTO mirrors; conversion-method zoo (`ToEither`/`ToFin`/`ToTry`/`ToEitherAsync`/…) |
| 11.3 | Higher-kinded types and typeclasses (`TypeClasses` 60 interfaces with arities up to 8, `ClassInstances` 1,229 types, v4 `default(TInstance)` resolution, v5 `K<F,A>` + static abstract members) | **reject** (confirm the existing rejection) | 4, 5, 8 | v4 callers thread instance type arguments through extra generic parameters (`MB Bind<MonadB,MB,B>(…) where MonadB : struct, Monad<MB,B>`; README `1551a092` lines 1766–1850), so misuse is a constraint error and JIT/instantiation behavior is hard to reason about; v5 replaces it with `K<M,A>` interface carriers, which boxes struct carriers at trait boundaries unless elided. Neither is needed for ordinary C# business code, and the goal explicitly rejects the hierarchy. | All HKT/typeclass machinery; law-testing traits (`MonadLaw`, `ApplicativeLaw`, …) |
| 11.4 | `Unit` type (`LanguageExt.Unit`; used pervasively in effects and folds) | **defer** | 1, 5 | FunnySharp's surface has no `Unit` and uses `void`/`Task`/`ValueTask` (0.1.0 surface). Language-ext's `Unit` exists to make effects first-class; a decision belongs with the effect design (F7) rather than this baseline. | `Unit` adopted only to mirror language-ext signatures |
| 11.5 | Compiler feedback / analyzers | **no capability** (language-ext ships no analyzers or source generators; package contains only `lib/netstandard2.0`) | — | Nothing to adopt; FunnySharp's analyzer policy is decided by its own evidence. | — |
| 11.6 | Packaging/dependency profile: netstandard2.0-only assets, 10 package dependencies (nuspec), no STJ/immutable-collections deps | **reject** the profile | 6 | `netstandard2.0` costs the library modern APIs and dependency weight; FunnySharp targets `net10.0` with zero runtime package dependencies. The baseline confirms that a functional library *can* be dependency-light, but its particular 2019-era dependency set is not a model. | netstandard-only assets; the 10-package dependency list |
| 11.7 | Stable-line maintenance signal: last stable 4.4.9 (2024-06-26); `origin/v4-latest` 2024-08-21; development is `5.0.0-beta-77` (2025-12-30) through HEAD (2026-07-29) | **reject** any compatibility/maintenance dependency on this baseline | 5 | The stable line is effectively frozen while the project invests in an unreleased redesign; adopting its vocabulary would import churn risk. **UNVERIFIED: no explicit deprecation/EOL statement was read; inference is from tag/commit dates only.** | Compatibility or migration promises toward language-ext (also excluded by the goal) |

## 3. What FunnySharp would gain if it adopted language-ext mechanisms (and why it should not)

- **Retry/backoff/timeout.** Real semantic-LOC reduction and the only genuine capability gap found
  (decision 6.2). Adopt only as a minimal `Effect` combinator with `TimeProvider` + token, or defer.
- **Exception boundary with rich error composition.** Language-ext's `Fin`/`Error` offer message,
  code, and exception composition. FunnySharp's generic `TError` plus `Result.Try` covers the
  ordinary case without importing a competitor carrier; keep.
- **Big operator/conversion surface.** Lets callers write `if (opt)`, `(A)opt`, `l | r`, tuple→`Map`
  conversions. This trades clarity and compile-time safety for brevity; reject.
- **HKT generality.** v5 can write algorithms over any monad. No FunnySharp consumer case in this
  survey needed it; the cost is a typeclass hierarchy, static abstract members, `K` boxes, and
  resolution errors that are hard for humans and agents to read. Reject.

## 4. Criteria synthesis (baseline-level)

- **Ordinary C# usefulness (1):** the useful subset is absence/outcome/accumulation/traversal/effect
  plumbing, all already present in FunnySharp. The rest (transformers, typeclasses, streaming
  pipes, atoms, units) serves a functional-programming dialect, not ordinary business C#.
- **Semantic LOC (2):** language-ext wins only on retry/timeout/backoff and on multi-error
  accumulation; FunnySharp matches accumulation and can match retry with a fraction of the surface.
  Single-step `Option`/`Result` matches are LOC-neutral vs modern C#.
- **Signature clarity (3):** weak. `Task<bool> IsSome { get; }` (awaitable property),
  `(A,S,bool) Invoke(S)` state delegates, `MB Bind<MonadB,MB,B>(…) where MonadB : struct, …`,
  6-arity `Proxy`, 94 `Some` overloads. Language-ext expresses semantics in docs, not signatures.
- **Compile-time verifiability (4):** weak. `Either` can be in a state (`Bottom`) that is not a
  member of its type; implicit/explicit conversions bypass branches; typeclass resolution is
  constraint-based; `TryOption` has three states where the delegate type shows one.
- **AI predictability (5):** low. One concept has multiple names and carriers, the largest entry
  point has 2,284 members, extension classes live in the global namespace, and a reader cannot
  choose between `map`/`Map`/`OptionExtensions.Map`/`SeqExtensions.Map` without documentation.
- **BCL interop (6):** poor by design. Custom `Option`/`Seq`/`Map`/`Error`/`Unit`, `Task`-based async
  carriers, `ISerializable`, BCL `IEnumerable<T>` extension methods, and 10 legacy dependencies.
- **Async/cancellation (7):** the weakest area. `Aff<A>.Run()`/`RunUnit()` take no token; `Try`
  converts exceptions to values; `Aff.Fork()` starts uncoordinated work; `OptionAsync` exposes state
  through `Task`-typed properties; `HasCancel<RT>` hides cancellation in type arguments.
- **Performance feasibility (8):** unmeasured in the pin. v4 HKT avoids boxing only through struct
  instances and monomorphised generics; v5 boxes struct carriers at `K<M,A>` boundaries
  (mechanism visible in signatures, cost **UNVERIFIED**). `Seq`'s at-most-once evaluation,
  `Aff.Memo()`, and delegate-heavy carriers all need measurement before any adoption.
- **Maintenance/strategy (not a numbered criterion):** stable line frozen since 2024-06; v5 in beta
  since at least 2025-12 with breaking removals (`OptionAsync`, `OptionUnsafe`, `TryOption`, `Aff`,
  `StreamT`, Pipes moved out). No compatibility target exists.

## 5. Independence guardrails (must remain rejected)

To remain independent, FunnySharp must reject, in language-ext terms:

1. **Custom runtime / scheduler:** `Aff<RT,A>`, `Eff<RT,A>` runtimes, `HasCancel<RT>`, `Has<M,TRAIT>`,
   `Mutates`, `Local`, `LanguageExt.Sys`, `IO` DSL as the execution substrate.
2. **Monad / transformer hierarchy:** `K<F,A>`, `LanguageExt.Traits.*`, v4
   `TypeClasses`/`ClassInstances`, `OptionT`/`EitherT`/`FinT`/`TryT`/`ValidationT`/`StateT`/`ReaderT`/
   `WriterT`/`RWST`.
3. **Replacement collection universe:** `Seq`/`Lst`/`Arr`/`Map`/`Set`/`HashMap`/`HashSet`/`Que`/
   `Stck`/`SpanArray`/`SeqLoan` and the 49 global-namespace extension classes.
4. **Pervasive immutability:** immutable-by-default carriers and patch/diff machinery as public
   policy.
5. **Competing vocabulary/naming:** `Some`/`None`-first naming, `Succ`/`Fail`, `Fin`, `Eff`/`Aff`,
   `head`/`headOrNone` lens properties, lowercase `Prelude` functions.
6. **Unsafe convenience semantics:** `Bottom` states, explicit throwing extraction, implicit
   payload conversions, truthiness operators, token-free `Run`/`Try`, `Fork` fan-out.
7. **Any dependency, carrier conversion, compatibility package, or migration promise** — language-ext
   types never appear in FunnySharp public signatures (goal constraint).

## 6. UNVERIFIED judgments and open questions

- **UNVERIFIED:** actual runtime/boxing/allocation costs of v4 `default(TInstance)` typeclass
  dispatch and v5 `K<M,A>` boxing; no benchmarks or disassembly were run, and no language-ext
  benchmark results are part of the pin.
- **UNVERIFIED:** exact exception semantics of `Try`/`TryAsync` (e.g. whether
  `OperationCanceledException` is captured or rethrown) were not exercised; only the XML summary
  ("captures exceptions") and delegate signatures were read.
- **UNVERIFIED:** `(A)option` behavior when `None` (the explicit conversion operator is
  undocumented in the XML); `op_True`/`op_False` semantics likewise.
- **UNVERIFIED:** the 4.4.9 build's nullable-annotation state (metadata mode does not decode it; the
  v4.4.9 csproj was not read).
- **UNVERIFIED:** `Map`/`Seq`/`HashMap` persistence and structural-sharing performance, and
  `Seq`'s "evaluate at-most-once" caching behavior, were not measured.
- **UNVERIFIED:** upstream maintenance intent (no deprecation/EOL statement was read; dates only).
- **Open (lead/maintainer decision):** adopt any effect retry/backoff/timeout operator (decision
  6.2) — yes/no, and if yes, `TimeProvider`-based minimal shape.
- **Open:** does any concrete FunnySharp consumer need a non-error two-value carrier distinct from
  `Result<TValue,TError>` (decision 2.2)?
- **Open:** should FunnySharp introduce a `Unit` type for effect terminals (decision 11.4)?
- **Open:** the brief's "338 types" figure for `inv-language-ext.md` is not
  reproducible (the dump has 159 non-generic types; `baselines.md` already records
  159); the complete 339-type LanguageExt namespace view is the supplementary
  `inv-language-ext-toplevel` dump.
- **Open:** the supplementary generated dumps are documented in
  `inventory/generated/README.md` (which now also records their member/operator
  counts, though its summary rows for the regenerated supplementary dumps are one
  generation stale); `eng/next-stage-inventory/generate.sh` and the
  `baselines.md` asset table still cover the canonical set only, so the lead may
  want to fold the supplementary commands in for reproducibility.

## 7. Not examined

- No consumer-side compile probes against the language-ext 4.4.9 DLL were run beyond the operator
  reflection probe (`/tmp/opreflect`); behavioral claims rest on signatures, XML docs, source, and
  the upstream README.
- `LanguageExt.ClassInstances` (1,229 types), `Pretty`, `UnitsOfMeasure`, and the concurrency/STM
  types were not inspected member-by-member (inventory §6).
- No benchmarks; `LanguageExt.Benchmarks` and upstream `Performance.md`/wiki Performance page were
  not run or read.
- Upstream wiki pages beyond the index were not read; GitHub issues/discussions were not surveyed.
- v5 `LanguageExt.Streaming`, `LanguageExt.Sys`, and samples were not surveyed beyond establishing
  the Core-package boundary changes; the v5 test project was not compiled.
- `LanguageExt.Sys`/`Parsec`/`FSharp`/`Rx`/`CodeGen`/`Transformers` packages were not surveyed.
