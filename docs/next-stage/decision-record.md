# External Capability Decision Record (Goal 14)

This record gives every relevant external capability from the pinned baselines one decision:
**adopt**, **adapt**, **defer**, or **reject**. Decisions are justified against the goal criteria
C1–C8 (`api-decisions.md`), and evidence is cited to the pinned inventories and analysis memos.

Pins and provenance: [`baselines.md`](baselines.md). Analyses:
[`analysis/baseline-csharpfunctionalextensions.md`](analysis/baseline-csharpfunctionalextensions.md)
(CFE), [`analysis/baseline-funcky.md`](analysis/baseline-funcky.md) (FU),
[`analysis/baseline-fsharp-core.md`](analysis/baseline-fsharp-core.md) (FS),
[`analysis/baseline-language-ext.md`](analysis/baseline-language-ext.md) (LE),
[`analysis/baseline-bcl.md`](analysis/baseline-bcl.md) (BCL),
[`analysis/funny-sharp-surface.md`](analysis/funny-sharp-surface.md) (OWN),
[`call-sites.md`](call-sites.md) (CS).

Meaning of the decisions: **adopt** = take the capability (FunnySharp implements it itself; no
competitor dependency); **adapt** = take the capability with a redesigned API or semantics;
**defer** = not now, with a recorded trigger; **reject** = do not take.

## Summary

| Family | adopt | adapt | defer | reject | total |
| --- | ---: | ---: | ---: | ---: | ---: |
| F1 absence | 6 | 3 | 7 | 8 | 24 |
| F2 fail-fast | 3 | 1 | 4 | 14 | 22 |
| F3 accumulation | 2 | 1 | 1 | 5 | 9 |
| F4 function grammar | 3 | 0 | 8 | 13 | 24 |
| F5 collections | 6 | 2 | 1 | 3 | 12 |
| F6 async/concurrency | 5 | 2 | 0 | 11 | 18 |
| F7 effects/resources | 2 | 0 | 2 | 8 | 12 |
| F8 state | 1 | 0 | 1 | 3 | 5 |
| F9 optics/immutability | 2 | 0 | 0 | 4 | 6 |
| F10 HTTP | 2 | 0 | 1 | 3 | 6 |
| F11 cross-cutting | 5 | 3 | 2 | 12 | 22 |
| **Total** | **37** | **12** | **27** | **84** | **160** |

## F1 — absence

| ID | Capability (pin) | Decision | Rationale and evidence |
| --- | --- | --- | --- |
| E1 | `Option<T>` as the single absence carrier (OWN 0.1.0) | adopt (present) | Safest surveyed design: `readonly struct`, `default = None`, no throwing accessor, no implicit conversion (OWN §2.1; C1, C4). |
| E2 | `Maybe<T>`/`Maybe` (CFE 3.7.0) | reject | Second absence vocabulary; its safe default is already matched by `Option<T>` (C5; CFE 1.1). |
| E3 | `FSharpOption<T>` (FSharp.Core 10.1.401) | reject | None is `null`, unchecked `Value`, no NRT metadata, 24 B/Some (FS #1; C1, C4, C6, C8). |
| E4 | `FSharpValueOption<T>` (FSharp.Core) | reject | Second absence type with two accessor failure modes (FS #2; C1, C5). |
| E5 | null↔absence conversion semantics (FS `OfNullable`/`ToNullable`/`OfObj`) | adapt | FunnySharp already mirrors the semantics through `FromNullable`/`ToOption`; adopt the documented rules, not the carriers (FS #3; C1, C6). |
| E6 | `Option.FromBoolean(bool[, value/selector])` (FU) | adopt | Removes the `condition ? Some : None` pattern; selector overload avoids wasted work (C1, C2; FU F1.2). Value/factory overloads only; the bool-only overload needs `Unit`, which is rejected (E98). |
| E7 | `Option.ToNullable()` (FU) | adopt | One explicit call replaces a `Match` dance; plain constrained overloads, no marker classes (C2, C6; FU F1.3). |
| E8 | `OptionEqualityComparer`/`OptionComparer`/`IComparable` (FU) | defer | No observed demand; default structural equality exists (C1, C5; FU F1.4). Trigger: `Option<T>` needed as a comparer-keyed dictionary or sorted key. |
| E9 | `await Option<Task<T>>` awaiters (FU) | defer | Eight awaiter types and a second await path; `ToOptionAsync` covers the common flow (C3, C5; FU F1.6). Trigger: recurring `Option<Task<T>>` consumers. |
| E10 | Option JSON converter (FU, CFE) | defer | Wire format is caller policy; FU's reflection factory is `[RequiresDynamicCode]` and CFE's singleton is global mutable state (C6, C8; FU F1.5, E121; CFE 2.7). Triggers: a goal that fixes the wire contract and proves trim/AOT behavior. The Funcky memo recommended adapt; the lead deferred the capability because the wire contract is unset and the reflection factory conflicts with the AOT boundary (E137 adapts only the honesty obligation). |
| E11 | LINQ aliases on absence/outcome carriers (`Select`/`SelectMany`) | adopt | Parity with `Result`/`Effect` improves AI predictability; secondary to the canonical vocabulary (C3, C5; OWN §2.10). `Validation` stays without them (no `Bind`). |
| E12 | `Where`/`Filter`-as-`Where` on carriers (FU `Where`, CFE `Where`) | reject | A bare predicate cannot produce absence or an error; `Filter`/`Ensure` carry the meaning (C3, C4; WS §2.10). |
| E13 | List-pattern support + guard analyzer (FU) | defer (pattern) / adapt (analyzer) | The capability is niche and semantically ambiguous; the analyzer mechanism is Goal 21 prior art (C4, C5; FU F1.7). |
| E14 | `DownCast`/`UpCast` monad casting (FU) | defer | No call-site evidence; error-factory variants would need a policy (C4, C5; FU F1.9). The Funcky memo recommended adapt; the lead deferred until a cast policy has a consumer and a contract. |
| E15 | Try-exhaustion analyzer (`TryGetValue` restriction, FU λ0001) | adapt | Adopt into Goal 21 with documented escape hatches (loop/iterator/catch-when) (C1, C3, C5; FU F1.8). |
| E16 | Container Try→`Option` bridges (FU family: dictionary/list/immutable/queue/stack/priority-queue/enumerator/queryable) | adopt | Removes `TryGetValue`/`FirstOrDefault` null ambiguity on standard carriers; trim overloads, no new types (C2, C3, C4, C6; FU F5.3). |
| E17 | `Parse*OrNone` bridges (FU ~200 overloads) | adapt | Common set plus a generic `IParsable` bridge; reject the overload tower (C2, C3, C6, C8; FU F5.4). |
| E18 | `Sequence.Return/FromNullable/Successors` (FU) | reject (`Sequence` carrier) | Carrier rejected (E64); `Successors` as unfold is deferred until a consumer needs it (C1, C8; FU F4.6). The Funcky memo recommended adapting the `Sequence` carrier; the lead rejected it with E64. |
| E19 | `TryGetValue`-style nullability annotations (`[NotNullWhen]`, `[MaybeNull]`) | adopt (present) | FunnySharp already annotates; CFE/FS show the value (C4, C6; CFE 1.1, FS #26). |
| E143 | `OptionUnsafe<A>`, `OptionT`/`OptionAsyncT` transformers (LE 1.3) | reject | Two more absence carriers plus HKT transformers; `OptionAsync`/`EitherAsync`/`TryAsync` are rejected in E83 (C3, C5, C6). |
| E144 | F# `Option.count`/`toArray`/`toList` zero-or-one view (FS #5) | reject | No C# idiom benefit; `Sequence`/`Traverse` handle real collections (C1, C2; FS #5). |
| E145 | F# `EventModule`/`IEvent` event algebra (FS #22) | defer | BCL events and `IObservable<T>` cover the need; `IEvent` is already `IObservable`-compatible if a consumer passes one (C1, C6; FS #22). |
| E146 | F# `LanguagePrimitives` generic equality/comparison objects (FS #23) | reject | BCL `EqualityComparer<T>.Default`/`Comparer<T>.Default` are faster (2.4-4.2x) and allocation-free (C6, C8; FS #23). |
| E147 | `MaybeEqualityComparer<T>` (CFE 1.4) | defer | Same trigger as E8: a consumer that needs a custom comparer for a carrier (C1, C6; CFE 1.4). |

## F2 — fail-fast outcome

| ID | Capability (pin) | Decision | Rationale and evidence |
| --- | --- | --- | --- |
| E20 | `Result<TValue,TError>` as the single fail-fast carrier (OWN) | adopt (present) | One error-typed outcome shape; typed-error parameter confirmed by FS (#7) (C1, C4). |
| E21 | `UnitResult<TError>` (CFE prior art; OWN Goal 15) | adopt | The canonical no-value outcome; removes dummy payloads; CS W4 shows the gap (C1, C2, C4; CFE 2.4; CS §16 item 5). |
| E22 | `Result`/`Result<T>` with fixed or string errors (CFE, FU) | reject | One error-typing model only; string errors leak into every non-generic path (C4, C5, C6; CFE 2.4; FU F2.1). |
| E23 | `Either<L,R>`/`Fin<A>`/`Common.Result` (LE) | reject as carriers; `Either` as non-error choice **defer** | Three names for two cases fragment the vocabulary; a non-error two-value case has no call-site evidence yet (C2, C5; LE 2.1–2.3). Trigger: a concrete non-error two-outcome workflow. |
| E24 | `Try`/`TryAsync`/`TryOption` delegates (LE) | reject | Four delegates over different carriers, no `CancellationToken`, side effects hidden in `Invoke`; `Result.Try*` is the single boundary (C3, C4, C7; LE 2.4; OWN §2.8). |
| E25 | Throwing accessors and `GetValueOrThrow` (CFE, LE) | reject | Runtime-checked correctness invisible in signatures; non-throwing `TryGet*`/`Match` are canonical (C3, C4; CFE 2.2; LE 2.6). |
| E26 | Implicit conversions into/out of outcome carriers (CFE, LE, FU) | reject | Assignment/branch silently encodes the case; CFE's own README example does not compile (C3, C4, C5; CFE 2.3; LE 1.2). |
| E27 | Alias operation families (`Check`, `OnFailureCompensate`, `MapIf`/`BindIf`/`TapIf`, `Execute`) | reject | One canonical name per intent; aliases destroy AI predictability (C3, C5; CFE 2.5). |
| E28 | Guard/observe/recover vocabulary (`Ensure`, `Tap`/`TapError`, `MapError`, `Compensate`→`Recover`, `Finally`) | adapt | Adopt the canonical set where it maps to FunnySharp's existing verbs; `Finally` deferred until ownership semantics are specified (C1, C2, C5; CFE 2.5, 4.2). |
| E29 | `IResult`/`IValue`/`IError`/`IUnitResult` interfaces (CFE) | reject | Interface hierarchy for one concept; 2026 CFE still ships four interfaces (C4, C5, C6; CFE 2.4). |
| E30 | `Try` catch-all without token, global `DefaultTryErrorHandler` (CFE) | reject | `OperationCanceledException` becomes a domain failure and policy is process-global (C3, C7; CFE 2.6). |
| E31 | `FailureIf`/`SuccessIf` boolean factories (CFE) | defer | Boolean-parameter factories read as traps; no call-site demand (C1, C3, C5; CFE 2.9). |
| E32 | CFE JSON converters + shipped `JsonSerializerOptions` singleton | defer (capability) / reject (singleton) | Same wire-contract trigger as E10; global mutable options must never ship (C6; CFE 2.7). |
| E33 | `Bottom` third state after `Either.Filter` (LE) | reject | An invisible state the type system does not carry; the clearest counter-example to C4 (LE 2.5). |
| E34 | Explicit extraction/comparison operator surfaces (LE: 41 `Either`, 29 `Fin`, 14 `Validation` operators) | reject | Throwing extraction and 41-way operator surfaces are not inferable from signatures (C3, C4, C6; LE 2.6). |
| E35 | `WithTransactionScope` (CFE) | defer | Transaction policy belongs to the application/infrastructure; no goal requires it (C1, C6; CFE F7). |
| E36 | Equality/hash/`ToString` diagnostic contracts (all baselines) | adopt (present) | Structural equality with case included; `ToString` explicitly diagnostic, never a serialization contract (C3, C6; OWN docs). |
| E151 | `Result.GetOrThrow` (FU F2.4) | reject | A throwing terminal is the accessor pattern E25 rejects; a policy goal would need to define error-to-exception semantics first (C3, C4; FU F2.4). |
| E152 | `ResultModule` function-first `FSharpFunc` API (FS #8) | reject | Subset of FunnySharp composition; callbacks need converters and no semantic LOC is saved (C1, C2, C6; FS #8). |
| E153 | `FSharpResult<T,TError>` carrier (FS #6) | reject | Unchecked case accessors and a false-success default; `Result<TValue,TError>` is the safer carrier (C4, C6; FS #6). |
| E154 | `FSharpChoice<...>` 2-to-7 way branching (FS #9) | reject (see rationale) | No exhaustive switch/deconstruction in C# and adjacent to the rejected general DU; reading an existing `FSharpChoice` stays possible via reflection-free field checks if a consumer must (C1, C4, C5; FS #9). |
| E148 | `EitherOrBoth`/`ZipLongest` (FU F2.3) | defer | No demand evidence; an `Option` pair or tuple expresses the case without a new carrier (C1, C5, C8; FU F2.3). |

## F3 — accumulation

| ID | Capability (pin) | Decision | Rationale and evidence |
| --- | --- | --- | --- |
| E37 | `Validation<TValue,TError>` with ordered accumulation (OWN) | adopt (present) | The only fully BCL-free capability; BCL has no equivalent (OWN AD-3; BCL F3; C2, C3). |
| E38 | `Validation<MonoidFail,FAIL,SUCCESS>` class-instance argument (LE) | reject | Failure channel as a type-class argument surfaces constraint errors, not signatures (C4, C5; LE 3.2). |
| E39 | `Seq<FAIL>` failure channel / `ValidationData` DTOs (LE) | reject | Forces the competitor's sequence type into every validation signature (C3, C6; LE 3.3). |
| E40 | `ValidationT` transformer (LE) | reject | Requires HKT/typeclass machinery (C4, C5; LE 3.4). |
| E41 | `ICombine` self-referential interface (CFE) | reject | Type-erasing `Combine(ICombine)` needs runtime casts and boxes (C4, C5, C7, C8; CFE 3.2). |
| E42 | `Combine` with global error separator and comma-joined messages (CFE) | adapt | Keep accumulation; require structured per-input errors and explicit ordering instead of global string policy (C3, C4; CFE 3.1). |
| E43 | Unbounded `Task.WhenAll` accumulation (CFE) | reject | No token, no degree bound; FunnySharp's bounded/ordered traversal is the model (C7, C8; CFE 3.3). |
| E44 | Traversal location/path context (OWN Goal 17) | adopt | `customers[17].addresses[2].postalCode` must be produced compositionally; CS W6 shows the workaround cannot express nesting (C2, C4; CS §16 item 6). |
| E45 | `ICombine`-style cross-carrier structural `CombineInOrder` (CFE) | defer | Ordered sequential combine is already expressible with `Bind`/`Traverse` (C2, C8; CFE 3.3). |

## F4 — function grammar

| ID | Capability (pin) | Decision | Rationale and evidence |
| --- | --- | --- | --- |
| E46 | `Pipe`/`Compose`/`Curry`/`Uncurry`/`Partial`/`Flip`/`Tap` (OWN) | adopt (present) | Superset of the useful LE subset with token-aware async forms (C1, C2; LE 4.1). |
| E47 | `Prelude` free-function universe (LE, 2,284 members) | reject | Discovery by guessing among 94 `Some` matches; destroys one-canonical-way (C3, C5; LE 4.2). |
| E48 | Global-namespace extension classes on BCL carriers (LE, 49 classes) | reject | Ordinary LINQ call sites can bind to competitor overloads; direct BCL-interop and AI hazard (C5, C6; LE 4.3). |
| E49 | `FSharpFunc`/currying as the function model (FS) | reject | C# lambdas do not convert, 32 B per curried call, `FuncConvert` boilerplate (C1, C2, C6, C8; FS #10). |
| E50 | `FuncConvert`/`FromConverter` adapters (FS) | defer | Only if a concrete consumer must hand callbacks to an F# API (C1, C6; FS #11). |
| E51 | Pipe/compose operators (`\|>`, `>>`) (FS) | reject | Semantically identical to application/composition; no C# grammar, no LOC win (C1, C2, C5; FS #12). |
| E52 | Arity > 2 currying/partial/flip and `Apply`-over-`Unit` towers (FU, 81 overloads) | reject | Overload towers with no ordinary-C# need; binary limit stays documented (C1, C5, C8; FU F4.3; OWN §2.6). |
| E53 | `Identity`, `NoOperation`, `Not` (FU) | defer | Trivial lambdas already express these; add only with consumer evidence (C1, C2, C5; FU F4.1). The Funcky memo recommended adopt; the lead deferred because the helpers save no semantic LOC today. |
| E54 | `Compose` receiver direction (FU) | reject | Keep FunnySharp's left-to-right pipeline direction (C5; FU F4.4). |
| E55 | `WhereSelect` (FU) | reject | Duplicate of `Choose` (C5; FU F5.5). |
| E56 | `WhereNotNull` (FU) | adopt | Removes a common null-filter dance without a new carrier (C2, C5; FU F5.6). |
| E57 | `Inspect`/`Pairwise`/`SlidingWindow` (FU) | defer | `Pairwise`/`SlidingWindow` are Goal 17 design candidates; `Inspect` duplicates `Tap` (C1, C2, C8; FU F5.7). The Funcky memo recommended adopt/adopt/adapt; the lead assigned the sequence scope to Goal 17 so the collection surface is decided once. |
| E58 | `Split`, `Intersperse`, `Interleave`, `TakeEvery`, `Transpose`, `AdjacentGroupBy`, `InspectEmpty` (FU) | defer | No demand evidence; several are BCL-trivial (C1, C2; FU F5.8). |
| E59 | `PowerSet`, `Shuffle`, `Chunk`, `JoinToString`, `Materialize`, `Range` enumeration (FU) | reject | BCL or trivial; `Chunk` already exists in .NET 10 async LINQ (C1, C6, C8; FU F5.9; BCL F5). |
| E60 | `Memoize` capability + `IBuffer`/`IAsyncBuffer` carrier (FU) | defer (capability) / reject (carrier) | Caching policy belongs to the caller; the carrier is a collection universe (C3, C7, C8; FU F5.10). |
| E61 | k-way `Merge` (FU) | defer | Needs measured demand and a bounded contract (C2, C8; FU F5.11). |
| E62 | `Partition` (predicate / Result / Either) (FU) | adopt | Predicate and typed-result partitions remove dual-loop plumbing (C2, C3, C6; FU F5.12). `Either` variant skipped with E23. |
| E63 | `ValueWithIndex`/`First`/`Last`/`Previous` positioned values (FU) | defer | No demand evidence; location context (E44) covers the traversal case (C1, C5; FU F5.13). |
| E64 | `Sequence`/`AsyncSequence` carriers (FU) | reject | Replacement collection universe with different semantics per carrier (C5, C6; FU F4.6, F5.10). |
| E65 | language-ext `Seq`/`Lst`/`Arr`/`Map`/`Set`/`HashMap`/`Que`/`Stck`/`ISeq` (LE) | reject | Parallel universe duplicating BCL concepts; contract already rejects it (C5, C6, C8; LE 5.2). |
| E66 | F# collections and `System.Tuple` return shapes (FS) | reject | BCL read interfaces and `ValueTuple` are the C# idioms (C2, C6, C8; FS #13, #15). |
| E67 | `BindZip` arity 2..8 tuple overloads (CFE, 56 overloads) | reject | Tuple-shape proliferation is discoverability poison (C3, C5; CFE 5.2). |
| E155 | `Combinators`/`CombinatorsDynamic`/`Compositions` tuple plumbing (LE 4.4) | defer | No ordinary-C# consumer case; `FunctionExtensions` is the narrower home if one appears (C1, C5; LE 4.4). |
| E149 | `True`/`False` constant predicates, `All`/`Any` folds (FU F4.2) | reject | Trivial lambdas and BCL `All`/`Any` already express these (C1, C5; FU F4.2). |

## F5 — traversal and cardinality

| ID | Capability (pin) | Decision | Rationale and evidence |
| --- | --- | --- | --- |
| E68 | `Sequence`/`Traverse` for Option/Result/Validation (OWN) | adopt (present) | BCL has no equivalent; fail-fast vs accumulate split is the key contract (C2, C6; BCL F5). |
| E69 | `*OrNone` cardinality access (`FirstOrNone`, `LastOrNone`, `SingleOrNone`, `ElementAtOrNone`) (FU) | adopt | Direct replacement for `FirstOrDefault` null ambiguity, one naming rule (C2, C3, C4, C6; FU F5.1). |
| E70 | `MinOrNone`/`MaxOrNone` (FU) | adopt | Same rationale; `AverageOrNone` overload towers rejected (C2, C5, C8; FU F5.2). |
| E71 | `Choose`/`GetOption` absence-aware filter/lookup (OWN) | adopt (present) | `Choose` beats LINQ Where+Select with fewer allocations; `GetOption` replaces dictionary ambiguity (C1, C2, C8; OWN AD-5). |
| E72 | Exact-vs-truncating zip and non-empty guarantees (OWN Goal 17) | defer | Adopt only if Goal 17 proves safety value without a collection universe (C1, C4; OWN G5). |
| E73 | `Scan` (running aggregate) (OWN Goal 16/17) | adopt (narrow) | BCL `Aggregate` covers fold, not scan; one operator, no pipeline hierarchy (C1, C2; OWN G4). |
| E74 | Fallible-function composition as a new pipeline hierarchy (OWN Goal 16) | adapt | `Bind` remains the canonical composition; document, do not add a wrapper type (C3, C5; OWN G4). |
| E75 | `SplitLazy`, string `IndexOfOrNone` family (FU) | adapt / adopt | Small LOC wins on BCL carriers (C2, C6, C8; FU F5.14). |
| E76 | Span/Memory `*To`/`*InPlace` (OWN) | adopt (present) | Honest lifetimes, caller-owned storage (C4; OWN AD-5). |
| E77 | Per-carrier lens properties (LE `Seq.head`, `Map.item`) | reject | Two access paradigms for the same data, no generalization (C3, C5; LE 5.3). |
| E78 | Tuple-to-`Map` implicit conversions and patch/diff types (LE) | reject | Implicit conversions and a parallel update model (C3, C5, C6; LE 5.2, 9.4). |
| E150 | `Cycle`/`CycleRange`/`RepeatRange`/`CycleMaterialized`/`Concat` (FU F4.7) | reject | Sequence-universe helpers with buffering semantics; BCL covers the ordinary cases (C1, C5, C8; FU F4.7). |

## F6 — async, streaming, concurrency

| ID | Capability (pin) | Decision | Rationale and evidence |
| --- | --- | --- | --- |
| E79 | Bounded ordered parallel streaming map (OWN `SelectParallelValueAsync`) | adopt (present) | No BCL equivalent; Channel backpressure plus drain-on-dispose (C1, C7; BCL F6; CS W7). |
| E80 | Bounded parallel traversal with fail-fast vs accumulate (OWN) | adopt (present) | Explicit policy split; no unbounded fan-out (C1, C3, C7; OWN AD-6). |
| E81 | First-success coordination over cold effects with `TimeProvider` timeout (OWN) | adopt (present; return shape kept per maintainer A-2=B) | Drains started work, caller cancellation precedence; `Validation` stays as the race outcome carrier and the race contract is documented explicitly (C1, C3, C7; CS W8). |
| E82 | Completion-order coordination (OWN Goal 18) | adopt (constrained) | Only over `IAsyncEnumerable<T>` and cold effects; no started-Task racing (C4, C7; OWN G7; BCL has `Task.WhenEach` for raw tasks). |
| E83 | `OptionAsync`/`EitherAsync`/`TryAsync` carriers (LE) | reject | Awaitable state properties (`Task<bool> IsSome`) and no token parameters (C3, C6, C7; LE 6.1). |
| E84 | `Aff<RT>`/`Eff<RT>` cancellation traits (`HasCancel<RT>`) and token-free `Run()` (LE) | reject | Cancellation as a type argument is invisible at `Run()` (C3, C4, C7; LE 6.3). |
| E85 | `Fork` and unbounded effect concurrency (LE) | reject | Unbounded, untracked concurrent work (C4, C5, C7; LE 6.4). |
| E86 | Pipes/`Proxy`/`Source`/`Sink`/`Conduit` (LE) | reject | Six-arity proxies and transducer pipelines; BCL streams+channels suffice (C1, C5, C7; LE 6.5). |
| E87 | Async `...Await...`/`WithCancellation` grammar (FU) | adopt (present) | FunnySharp already uses the token-forwarding grammar (C5, C7; FU F6.1). |
| E88 | Sync `OrNone`/`Partition` mirrored to async sources (FU) | adapt | Adopt the sync-capability set on async carriers; do not mirror every overload (C2, C7; FU F6.2). |
| E89 | Async `Merge`, async `Memoize` (FU) | reject | Token loss / caching policy per E60 (C7, C8; FU F6.3). |
| E90 | `ShuffleAsync` (FU) | reject | Trivial and not coordination work (C1, C8; FU F6.4). |
| E91 | `FSharpAsync` runtime, `async {}`/`task {}` builders (FS) | reject | Not awaitable from C#, ambient token, hidden scheduling (C1, C6, C7; FS #16, #19). |
| E92 | F# `Async.Parallel` bounded-with-drain semantics (FS) | adapt | Confirms the existing bounded/drain contract; nothing to import (C7, C8; FS #18). |
| E93 | `MailboxProcessor` actor runtime (FS) | reject; `Channel<T>` substitute adopt (present) | Actor runtimes excluded; Channel backpressure is the boundary (C1, C5, C7; FS #20–#21). |
| E94 | BCL `Task.WhenEach`/`Parallel.ForEachAsync` wrappers (BCL 10) | reject | Already clear BCL primitives; wrapping adds nothing (C1, C6; BCL F6). |
| E95 | Unbounded fan-out/`Task.Run` hiding anywhere (all baselines) | reject | Contract invariant (C7; OWN AD-6). |
| E96 | Awaitable/async wrapper types (`AsyncResult` etc.) | reject | Await at the C# boundary; no second carrier (C3, C6; OWN AD-6). |

## F7 — effects, resources, environment

| ID | Capability (pin) | Decision | Rationale and evidence |
| --- | --- | --- | --- |
| E97 | Thin `Effect<T>`/`Effect<TEnvironment,T>` over delegates (OWN) | adopt (present) | Deferred `ValueTask`, explicit environment and token, no runtime (C3, C4, C7; LE 7.1 validates the shape). |
| E98 | Public `Unit` type (FU, LE) | reject | `UnitResult` and `void`/`Task` cover no-value outcomes; a Unit carrier exists only to mirror foreign signatures (C1, C6; FU F4.5; FS #24). |
| E99 | Eff/Aff/IO monads and effect failure carriers (`Fin`) (LE) | reject | Custom runtime/effect universe; failure must stay explicit in `Result` (C4, C5, C6, C7; LE 7.2, 7.5). |
| E100 | `Reader<TEnvironment,TResult>` delegate (FU) | reject | `Effect<TEnvironment,T>` already covers environment access (C3, C5; FU F7.3). |
| E101 | Runtime DI traits (`Has<RT,Trait>`, `LanguageExt.Sys`) (LE) | reject | Competing DI/runtime model; caller-owned environments only (C5, C6, C7; LE 7.3). |
| E102 | Retry policies (`IRetryPolicy` + constant/linear/exponential) (FU) and `Aff` retry/schedule DSL (LE) | defer | Largest LOC-win candidate, but requires `TimeProvider`, explicit token, cap/jitter rules, and measured comparison to a hand-written loop before any stable surface (C2, C7, C8; FU F7.1; LE 6.2). Maintainer may downgrade to reject. |
| E103 | Sync `Thread.Sleep` retry and unbounded producer retry (FU) | reject | Blocking and unbounded (C1, C7; FU F7.2). |
| E104 | `Functional.Retry`-style global function surface (FU) | reject | Free-function universe (E47) (C3, C5; FU F7.2). |
| E105 | `WithTransactionScope` (CFE) | defer | Infrastructure policy, not a library capability (C1, C6; CFE F7). |
| E106 | `ForEach`/`Discard` side-effect helpers (FU) | reject | `foreach` and explicit discard express them; no semantic gain (C1, C5; FU F4.5). |
| E107 | `Lazy` wrapper and `System.Lazy` monad extensions (FU) | reject | BCL `Lazy<T>` suffices (C6; FU F9.1). The Funcky memo deferred the wrapper; the lead rejected it because the capability is already covered by the BCL. |
| E108 | `Using`/`UsingAsync` resource scoping (OWN) | adopt (present) | Exactly-once release across success/failure/exception/cancellation with BCL precedence (C2, C7; LE 7.4 validates). |

## F8 — state

| ID | Capability (pin) | Decision | Rationale and evidence |
| --- | --- | --- | --- |
| E109 | `StateChange`/`StateTransition`/`StateMachine`/`TransitionResult` (OWN) | adopt (present; `Then` redesigned) | Four-status pure model with replay; O(n²) `Then` fixed per AD-8 (C1, C3, C4; LE 8.1). |
| E110 | `State`/`StateT`/`ReaderT`/`WriterT`/`RWS` (LE) | reject | Monad transformer hierarchy; tuple-flag results hide failure (C1, C3, C5; LE 8.1). |
| E111 | Atoms/STM/version vectors (LE) | reject | Separate concurrency runtime; `System.Collections.Concurrent` and DI-scoped state are the BCL answer (C1, C6; LE 8.2). |
| E112 | Persistence, event sourcing, workflow engines | reject | Caller owns storage and execution; the transition core stays pure (C1, C5; OWN AD-8). |
| E113 | State/command serialization policy | defer | Wire format is caller policy until a goal fixes it (C6; OWN AD-8). |

## F9 — optics and immutability

| ID | Capability (pin) | Decision | Rationale and evidence |
| --- | --- | --- | --- |
| E114 | `Lens<TSource,TFocus>`/`Optional<TSource,TFocus>` (OWN) | adopt (present) | LE validates the shape; no reflection, no hierarchy, caller-owned laws (C2, C3, C4, C6; LE 9.1–9.2; CS W10). |
| E115 | A separate `Prism` type (LE) | reject | `Optional<TSource,TFocus>` already is the partial optic; two names for one concept (C5; LE 9.2). |
| E116 | Traversal/`Iso`/`Getter`/`Setter`/`Fold` hierarchy (LE) | reject | Explicit contract exclusion; not needed for ordinary C# updates (C1, C5; LE 9.3). |
| E117 | `System.Collections.Immutable`/`Frozen*` as the update mechanism (BCL) | adopt (present) | BCL carries persistence; optics do not choose collection operations (C6; OWN AD-9). |
| E118 | Pervasive immutability / immutable-first carriers (LE) | reject | Immutability stays opt-in (C5, C6, C8; LE 9.4). |
| E119 | Reflection/property-path optics | reject | Caller delegates only (C4; OWN AD-9). |

## F10 — HTTP integration

| ID | Capability (pin) | Decision | Rationale and evidence |
| --- | --- | --- | --- |
| E120 | Explicit `IResult`/`ProblemDetails` mapping with `RequestAborted` (OWN) | adopt (present) | Caller-selected policy, no DI/global handler; pure win when composed (CS W11) (C1, C3, C6, C7; BCL F10). |
| E121 | `UnitResult<TError>` HTTP mapping | adopt | The fourth carrier must cross the HTTP boundary (C3, C4; OWN G1). |
| E122 | First-party TypedResults/OpenAPI API | defer | Adapter returns `IResult` and loses inference; document `.Produces*` and test OpenAPI in the Goal 22 slice instead (C3, C6; OWN G12; BCL F10). |
| E123 | Filters/middleware/global exception policy | reject | Domain stays HTTP-free; policy stays at the endpoint (C1, C6; OWN AD-10). |
| E124 | `LanguageExt.AspNetCore` community adapter (LE) | reject | Out of pin and scope; FunnySharp.AspNetCore is canonical (C5; LE 10.1). |
| E125 | CFE `HttpResponseMessage` JSON helpers | reject | Client-side HTTP library behavior, not Minimal API mapping; tokenless error path (C3, C7; CFE 2.8). |

## F11 — cross-cutting

| ID | Capability (pin) | Decision | Rationale and evidence |
| --- | --- | --- | --- |
| E126 | First-party analyzer package and code fixes (FU prior art; OWN Goal 21) | adopt | Compiler feedback is the only route to C4 for discarded outcomes and default states; FU proves packaging (C4, C5; FU §4; OWN G9). |
| E127 | Analyzer packaging in the main package with build assets (FU) | adapt | Adopt the zero-install shape with mitigations: all diagnostics suppressible by default except impossible states, ASCII IDs, no `NotConfigurable` (C1, C4, C5; FU F11.1–F11.2). |
| E128 | `PublicAPI.Shipped/Unshipped` + PublicApiAnalyzers process (FU) | adopt | Committed API baseline enforces the stability boundary (C4; FU F11.3; OWN G14). |
| E129 | Source generators (FU internal technique) | adopt as internal technique, not a shipped feature | Repetitive bridge APIs may be generated internally; no consumer-facing generator (C5, C8; FU F11.6). |
| E130 | Syntax-only list-pattern analyzer (FU λ0003) | adapt | Adopt the pattern into Goal 21 if a carrier surface needs it; the analyzer mechanism is the value (C4, C5; FU F1.7). |
| E131 | `NotConfigurable` diagnostics by default (FU) | reject | Reserve only for impossible states (C1, C5; FU F11.2). |
| E132 | HKT emulation, typeclasses, `ClassInstances`, monad transformers (LE, CFE `ICombine`) | reject | Constraint-error semantics and JIT/boxing opacity; explicit goal exclusion (C4, C5, C8; LE 11.3). |
| E133 | General discriminated unions / native union dependency | defer | Until .NET 11 language and runtime contracts are stable and a separate goal compares focused types (C4, C5; `docs/product-contract.md §"Deliberate Deferrals"`). |
| E134 | Competitor dependencies, carrier conversion, compatibility packages, naming concessions, migration promises | reject | Independence constraint; no competitor vocabulary is imported (C5; goal text; LE 11.1). |
| E135 | Multi-TFM/dependency pin profile (FU `System.Text.Json` 5.0.2, `Immutable` 1.7.1; LE netstandard2.0 + 10 deps) | reject | FunnySharp targets `net10.0` with zero runtime package dependencies (C6, C8; FU F11.4; LE 11.6). |
| E136 | Build-transitive implicit usings/targets (FU `FunckyImplicitUsings`) | reject | Consumer-visible build magic; no hidden imports (C5; FU F11.5). |
| E137 | `[RequiresDynamicCode]`-honest reflection converters (FU) | adapt | Any future reflection-based feature must carry the same honesty and trim/AOT notes (C6; FU F1.5). |
| E138 | F# "no nullable annotations on the wire" profile (FS) | reject the lesson; keep annotating | FunnySharp keeps its own NRT metadata precise and does not rely on foreign annotations (C4, C6; FS #26). |
| E139 | Diagnostic `ToString` text as a contract (FS, all) | reject | Diagnostics only; no serialization/display promise (C3, C6; FS #28). |
| E140 | language-ext stable-line maintenance risk | reject any compatibility engagement | Stable line frozen while v5 development continues; adopting vocabulary would import churn (C5; LE 11.7). **UNVERIFIED: no EOL/deprecation statement was read; this is inferred from tag and commit dates.** |
| E141 | BCL `JsonSerializerOptions.Strict` for DTO-only policies (BCL 10) | adopt as documentation guidance | Converts "DTO-only" advice into an executable option where callers want it; no package dependency (C6; BCL F11). |
| E142 | Serialization converters for carriers (all baselines) | defer | Same wire-contract trigger as E10/E32; callers use DTOs today (C6; `api-decisions.md` G18; BCL F11). |
| E156 | `ISerializable` carrier serialization and `*Data` DTO mirrors (LE 11.2) | reject | Legacy serialization on domain carriers and mirror DTOs instead of a wire contract; E142 already defers converters (C5, C6; LE 11.2). |
| E157 | Global mutable configuration (`DefaultConfigureAwait`, `DefaultTryErrorHandler`, error separators) (CFE 6.3, FU) | reject | Process-global policy changes behavior by ambient state; configuration stays per call or per composition (C3, C5, C7; CFE 6.3). |
| E158 | Value-object base classes (`ValueObject`, `Entity`, `SimpleValueObject`, `EnumValueObject`) (CFE 9.1) | reject | Inheritance-based DDD scaffolding and implicit conversions; FunnySharp stays composition-first (C1, C4, C5; CFE 9.1). |
| E159 | Obsolete-retention alias policy (CFE `Execute`/`ExecuteNoValue` kept as obsolete) (CFE 11.4) | reject | Aliases are removed with the accepted redesigns instead of being carried forever; no permanent alias layer (C3, C5; CFE 11.4). |
| E160 | Competitor benchmark baselines for Goal 20 (FSharp.Core, Funcky, CFE, language-ext) | adopt (see rationale) | Goal 20 requires comparisons; the packages may be referenced only in a non-packable, non-shipping evidence project outside `FunnySharp.slnx`. Results are performance evidence, never API-compatibility or acceptance evidence (C8; `docs/goals/0020-goal.md`; standing-constraints C5, resolved by this record). |

## Memo reconciliation

The final decisions above supersede or absorb every decision-bearing row in the analysis memos.
Where the lead deviated from a memo recommendation, the row says so inline. This table lists the
rows that the independent audit (V-04, V-05) found unrepresented and their final home.

| Memo row | Memo decision | Final decision | Where |
| --- | --- | --- | --- |
| FU F2.3 `EitherOrBoth`/`ZipLongest` | defer | defer | E148 |
| FU F2.4 `Result.GetOrThrow` | defer | reject | E151 |
| FU F4.2 `True`/`False`/`All`/`Any` | defer | reject | E149 |
| FU F4.7 `Cycle`/`RepeatRange`/`Concat` | reject | reject | E150 |
| FU F1.5 JSON converter | adapt | defer | E10 (note) |
| FU F1.9 `DownCast`/`UpCast` | adapt | defer | E14 (note) |
| FU F4.1 `Identity`/`NoOperation`/`Not` | adopt | defer | E53 (note) |
| FU F4.6 `Sequence.Return/FromNullable/Successors` | adapt | reject carrier / defer unfold | E18 (note) |
| FU F5.7 `Inspect`/`Pairwise`/`SlidingWindow` | adopt/adapt | defer to Goal 17 | E57 (note) |
| FU F9.1 `Lazy` wrapper | defer | reject | E107 (note) |
| FU F7.1 retry policies | adapt | defer | E102 (note) |
| FU F1.8 `TryGetValue` restriction | adapt in Goal 21 | adapt | E15 |
| FS #5 `Option.count`/`toArray`/`toList` | reject | reject | E144 |
| FS #6 `FSharpResult` carrier | reject | reject | E153 |
| FS #8 `ResultModule` function-first API | reject | reject | E152 |
| FS #22 `EventModule`/`IEvent` | defer | defer | E145 |
| FS #23 `LanguagePrimitives` | reject | reject | E146 |
| LE 1.3 `OptionUnsafe`/`OptionT`/`OptionAsyncT` | reject | reject | E143 |
| LE 4.4 `Combinators` | defer | defer | E155 |
| LE 11.2 `ISerializable`/`*Data` DTOs | reject | reject | E156 |
| LE 11.4 `Unit` | defer | reject | E98 (note below) |
| CFE 1.4 `MaybeEqualityComparer` | defer | defer | E147 |
| CFE 6.3 global `ConfigureAwait` policy | reject | reject | E157 |
| CFE 9.1 value-object base classes | reject | reject | E158 |
| CFE 11.4 obsolete-retention policy | reject | reject | E159 |
| OWN G7 completion-order concurrency | defer | adopt (constrained) | E82; Goal 18 explicitly requires completion-order results, so the memo recommendation is superseded |

Additional reconciliations:

- E98 (`Unit`): the Funcky and language-ext memos both left this open/deferred; the lead rejects a
  public `Unit` because `UnitResult<TError>` and the ordinary `void`/`Task`/`ValueTask` forms
  cover every recorded call site.
- OWN G9 says "a separate analyzer package"; the final decision is in-package analyzers shipped
  inside the `FunnySharp` package under `analyzers/dotnet/cs` (Funcky model), because the goal
  requires no runtime dependency and zero-install feedback.
- OWN §2.3 recommended redesigning the `FirstSuccessAsync` return shape; the maintainer accepted
  A-2=B (keep `Validation`, document the race contract), so the memo recommendation is superseded
  by the recorded decision.

## Independence guardrails (must remain rejected)

1. No competitor `PackageReference` in the core, ASP.NET Core package, tests, or examples.
   Comparison tooling may reference pinned competitor packages only in an isolated, non-packable,
   non-shipping project that is never part of `FunnySharp.slnx` or any release artifact
   (standing-constraints C5, resolved by this record).
2. No carrier conversion, compatibility package, or migration promise toward CFE, Funcky,
   language-ext, or FSharp.Core.
3. No naming concession: FunnySharp names stay as decided in `api-decisions.md`.
4. No custom runtime, scheduler, fiber layer, HKT/typeclass hierarchy, replacement collection
   universe, pervasive immutability, actor system, or workflow engine.
5. No general discriminated-union system before the .NET 11 language/runtime contracts are
   stable and a separate accepted goal evaluates it.

## Unverified judgments in this record

- ~~Maintainer acceptance of the vocabulary and stability boundary~~ **Resolved 2026-09-17**:
  recorded in `maintainer-acceptance.md` (A-1=A, A-2=B, A-3=A, A-4=A, A-5=A, A-6=A, A-7=A, A-8=A).
- Retry policy choice (E102): deferred on the lead's recommendation, with the Funcky memo's
  narrower adapt recorded as the alternative; the maintainer accepted the deferral (A-7=A).
- Closed-generic AOT-safe JSON converters (E10/E32/E142) were not prototyped.
- language-ext `Try*` 4.4.9 cancellation handling was not exercised (LE UNVERIFIED).
- No competitor benchmarks were run; LOC and API evidence exist, timing does not (CS §18).
- The analysis memos carry the full per-workstream `UNVERIFIED` inventory; this list is the
  decision-relevant subset (audit V-20).
- E140's language-ext maintenance inference is flagged in its row.