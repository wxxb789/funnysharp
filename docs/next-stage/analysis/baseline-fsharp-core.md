# Analysis — FSharp.Core 10.1.401 as an external-capability baseline (Goal 14)

Survey date 2026-09-17. Pin: `FSharp.Core` 10.1.401, nupkg
`cf1f4e69fc2d1351af9fa9923ac90ed466088730a00867cbe0eb1b3b2c68963e`, DLL
`39b0b7f06c11bedd93f94b6f101fffd645f1c4437a5a6d3ee79259f855aeb11a`, upstream
`dotnet/fsharp` tag `v15.2.401` = `36bf12fb8cadd1ce1e41e153e9e1cee1f51e9c2f`.
Full provenance and member-by-member evidence live in
[`docs/next-stage/inventory/fsharp-core.md`](../inventory/fsharp-core.md); this memo cites that
document by section as `inv:§N` (section N of the inventory), or the C# harness output as
`csharp-out:NNN`. Candidate decisions are for the lead; this memo does not change any
FunnySharp API.

Scope note: every recommendation below is about a capability *idea* or *semantic*, because the
FunnySharp independence constraints forbid adopting FSharp.Core types, carriers, or a
compatible dependency surface. "Adopt" therefore never means "reference FSharp.Core".

## 1. Method and confidence

Evidence sources, strongest first:

1. Pinned binary + XML docs: reflection dump of the exact DLL
   (`docs/next-stage/inventory/generated/inv-fsharp-core-focus.md`, 96 types; see the inventory's
   §1 for why the shared 102-type `inv-fsharp-core.md` is not line-cited) and the shipped XML
   documentation; behavior proven by compiling and running C# and F# programs against that DLL
   (`/tmp/opencode/scratch/fsharp-consumer`, `/tmp/opencode/scratch/fsharp-fails`,
   `/tmp/opencode/scratch/fsharp-samples`).
2. Expected-failure probes: six C# snippets that must not compile; their diagnostic codes are
   recorded in `inv:§12` (`fails/f1…f7`).
3. Upstream source tag `v15.2.401`: `eng/Versions.props`, `src/FSharp.Core/option.fs`.
4. Microsoft Learn conceptual docs, pinned by their git revision (`inv:§1`).
5. A single-machine microbenchmark in the harness, explicitly indicative (`inv:§14`).

Confidence: the API shapes, representation rules, and C#-side ergonomics are directly verified.
Performance numbers are directional only. Source-level implementation reading was limited to
`option.fs`; nothing below depends on unread implementation detail.

## 2. What FSharp.Core is, in FunnySharp terms

FSharp.Core is not an F#-flavored standard library that happens to be mappable to C#. It is the
F# language runtime surface: `FSharpOption`/`FSharpValueOption`/`FSharpResult`/`FSharpChoice`
are the compiled forms of language constructs; `FSharpFunc` is the representation of curried
functions; `FSharpAsync` is the operand type of the `async` computation-expression syntax;
`TaskBuilder` and `FSharpAsyncBuilder` are compiler-inlining targets. The C# consumer sees the
*encoding*, not the feature:

- `None` is `null`, and instance members are emitted as static methods
  (`inv:§2`, XML doc "None values will appear as the value null to other CLI languages").
- Callbacks are `FSharpFunc` classes, so C# lambdas need `FuncConvert.FromFunc` wrappers
  (CS1660/CS0411 probes).
- `FSharpAsync<T>` has no members and no `GetAwaiter` (CS1061 probe).
- Reading the wrong `FSharpResult` case is not prevented and silently yields `default`
  (`csharp-out:44–49`).

Consequence: almost every decision below is "reject as a FunnySharp surface, but mine the
semantics". The two genuinely useful classes of evidence are (a) the *semantics of absence and
failure* that FunnySharp already mirrors, and (b) the *negative* ergonomics that validate the
BCL-first contract.

## 3. Key findings

### 3.1 Default and uninitialized state

| Type | `default(T)` | Consequence |
| --- | --- | --- |
| `FSharpOption<T>` | `null` = `None` | safe as absence, unsafe as a receiver: `GetHashCode`/`ToString`/`Value` throw NRE (`csharp-out:14–16`) |
| `FSharpValueOption<T>` | `ValueNone` (Tag 0) | safe absence baseline; the struct encoding FunnySharp's `Option<T>` resembles |
| `FSharpResult<T,TError>` | `Tag 0` = **Ok** with `ResultValue = default` | **uninitialized value is a success** (`csharp-out:44–46`) |
| FunnySharp `Option<T>` | `None` | already safe (docs/option.md:8) |
| FunnySharp `Result<TValue,TError>` | failure with `default(TError)` | deliberately opposite of F# (docs/result.md:13) |

This is the single most transferable finding: a value type that models failure must choose an
uninitialized state that cannot be mistaken for success. F# chose `ValueNone` correctly for
`ValueOption` and incorrectly for `Result`. FunnySharp's shipped choices (default `Option` =
None, default `Result` = failure) align with the safe half of the evidence and should be kept
and stated as a cross-cutting rule, not left implicit per type.

### 3.2 Absence: `FSharpOption<T>` vs `FSharpValueOption<T>`

- `FSharpOption<T>` is a class: 24 bytes allocated per `Some(int)`, measured
  (`csharp-out:108`); `None` is `null`, so absence and uninitialized state are indistinguishable,
  nullability metadata is absent, and null-safety analysis cannot help C# consumers (`inv:§12.1`).
- `FSharpValueOption<T>` is a struct with `default = ValueNone`; but it doubles the vocabulary
  (`IsSome`/`IsNone` aliases next to `IsValueSome`/`IsValueNone`) and its `Value` accessor throws
  `InvalidOperationException` while the module's `GetValue` throws `ArgumentException`
  (`csharp-out:36–37`) — two failure types for one condition.
- F# permits `Some null`; the docs warn it is "generally to be avoided"
  (options doc, git `e3ae3bde…`). F# conversions (`OfObj`, `OfNullable`, `ToObj`, `ToNullable`)
  normalize null to absence (`inv:§4`), mirroring FunnySharp's normalization rule
  (docs/option.md:31).
- The zero-or-one collection view (`Option.count`, `toArray`, `toList`, `ForAll`, `Fold`) is
  coherent in F# but has no C# idiom; it does not reduce semantic LOC versus `TryGetValue` and
  friends.

FunnySharp implication: keep exactly one absence type, a `readonly struct` with a safe default
and no throwing accessor. No need for a second "value option" name or an `OptionStruct` alias.
FunnySharp already advertises the rule "no throwing `Value` property"
(docs/option.md:10); the FSharp.Core evidence shows why that rule matters.

### 3.3 Failure: `FSharpResult<T,TError>`

- Same-type-parameter error typing matches FunnySharp (`FSharpResult<T,TError>`), and
  `ResultModule` supports `Map`, `Bind`, `MapError`, `DefaultValue`, `DefaultWith`, `ToOption`,
  `ToValueOption` — a subset of FunnySharp's surface (docs/result.md:19–32): FunnySharp has
  `Ensure`, `Recover`, `RecoverWith`, `Zip`, `ZipWith`, LINQ `Select`/`SelectMany`, and the
  async composition that `ResultModule` lacks entirely (`inv:§5`).
- `ResultModule` exposes `IsError`/`IsOk`, `ToArray`/`ToList` (0-or-1), `Fold`, `Iterate`,
  `Contains`, `Count`. None reduces C# semantic LOC relative to `Match`/`TryGetValue`.
- The case accessors are the anti-pattern to avoid: `ResultValue` on `Error` and `ErrorValue`
  on `Ok` return `default` instead of throwing or being unreachable
  (`csharp-out:47–49`). FunnySharp's decision to expose no throwing value/error accessor
  (docs/result.md:16) is stronger than F#'s.
- No JSON support: `System.Text.Json` throws `NotSupportedException` on `FSharpResult`
  ("F# discriminated union serialization is not supported…", `csharp-out:124`). FunnySharp's
  serialization policy remains an open decision (`inv:§12.13`), but the direction is clear:
  a custom-converter-free default is not available for DU-shaped values in the BCL.

### 3.4 Async and cancellation

Verified semantics worth extracting into the FunnySharp async policy:

- `FSharpAsync<T>` is not awaitable from C#; `StartAsTask`/`StartImmediateAsTask` are the
  boundary (`inv:§12.7`). The `async` builder cannot be constructed from C#
  (internal ctor, CS1729), so C# composition is limited to static combinators.
- `StartAsTask` "Executes a computation in the thread pool" and, when no token is supplied,
  uses a **process-wide default token** that `Async.CancelDefaultToken()` can cancel — ambient
  cancellation, not caller-owned (`inv:§9`). FunnySharp's contract requires the exact caller
  token and no hidden scheduling; the FSharp.Core default-token model is the counter-example.
- `RunSynchronously` blocks the caller thread and may queue work to the thread pool
  (`xml:17800`); `MailboxProcessor.PostAndReply` does sync-over-async with a timeout
  (`focus:690`). Both violate FunnySharp criterion 7 and should remain rejected patterns.
- Cancellation is not a domain failure: `StartAsTask` with a pre-canceled token yields a
  `Canceled` task (await throws `TaskCanceledException`), and `Async.Catch` only wraps
  *exceptions* into `Choice2Of2`; cancellation flows through a separate continuation
  (`inv:§9`). FunnySharp's existing rule — faults and cancellation are not absence/failure and
  keep the caller's token (docs/option.md:45, docs/result.md:65–72) — matches the correct part
  of the model.
- `Parallel` has bounded degree (`Async.Parallel(…, maxDegreeOfParallelism)`) and a documented
  fork/join + cancel-siblings contract, including "will still wait for the other child
  computations to complete" on cancellation (XML for `Parallel`, `inv:§9`). This is supporting
  evidence for FunnySharp's bounded-parallel contract (docs/concurrency.md), not a new feature.

### 3.5 Mailbox/actor boundary

`MailboxProcessor<TMsg>` is an agent runtime: single-reader queue, constructor-supplied
`CancellationToken`, `Post`, `Receive`/`Scan`/`TryReceive`, `PostAndAsyncReply`,
`PostAndReply` (blocking), `Error` event, `Dispose` (`inv:§10`). The body type is
`FSharpFunc<MailboxProcessor<TMsg>, FSharpAsync<Unit>>`; real bodies are recursive `async`
computation expressions. A C# consumer cannot construct the builder and therefore cannot write
a receive loop, only a one-shot body (`csharp-out:104–105`). Three independent reasons to reject:
the actor runtime is out of scope (product contract rejects schedulers/actor runtimes), the
interop shape is unusable from C#, and the reply paths pull in sync-over-async and a shared
default token. If a FunnySharp consumer needs the boundary, the BCL-first shape is
`Channel<T>` + an explicit cancellation token plus the existing state-machine types
(docs/state-machines.md:79 records the async boundary policy).

### 3.6 Function grammar, collections, choice, and the rest

- `FSharpFunc` currying costs 32 bytes per partial application measured, versus 0 for a C#
  delegate, and `InvokeFast` is needed to avoid it (`csharp-out:112–114`). `FSharpFunc` call
  overhead is comparable to a direct `Func` call (~1×), so the cost is representation, not
  dispatch. Nothing here justifies a non-delegate function model in FunnySharp.
- Curried `FSharpFunc` arguments force `FuncConvert.FromFunc` at every call site; the resulting
  C# code is not shorter than direct `Func`-based code (`inv:§13`).
- F# collections: `FSharpList<T>` is a cons list implementing `IReadOnlyList<T>`,
  `FSharpMap<K,V>` implements `IReadOnlyDictionary<K,V>`, `SeqModule` returns
  `IEnumerable<T>`, and tuple results are `System.Tuple` (heap). Module functions are
  function-first and `FSharpFunc`-typed. This validates FunnySharp's carrier policy: read-only
  BCL interfaces are the interop boundary; the persistent structures themselves are not needed.
- `FSharpChoice<...>` is a 2-to-7-way branching helper for F# active patterns. C# can name the
  nested case types and cast, but has no exhaustive switch or deconstruction; and a multi-way
  choice type is adjacent to the general discriminated-union system the contract rejects.
- `Operators` pipe/compose methods exist as public statics (`op_PipeRight`, `op_ComposeRight`, …)
  and are callable, but they encode only function application/composition; they add no C#
  grammar and no semantic reduction (`inv:§7`). FunnySharp's `Pipe`/`Compose` extensions over
  `Func` remain the canonical surface (docs/function-composition.md).
- `LanguagePrimitives` generic equality is 2.2–4.2× slower than
  `EqualityComparer<int>.Default` in the survey's microbenchmark and allocates slightly, while
  `FastGenericEqualityComparer<T>()` exists precisely to recover comparer performance
  (`inv:§11`, `inv:§14`). The BCL-first equivalent is `EqualityComparer<T>.Default` /
  `Comparer<T>.Default`.
- `Unit` is a null-valued class with no public constructor; it is the return type of
  `Async.Sleep`-style APIs. In C# it buys nothing over `void`/`ValueTask`.

## 4. Candidate decisions

Decision vocabulary per the Goal 14 brief: adopt / adapt / defer / reject. Criteria: C1 ordinary
C# usefulness, C2 consumer-side semantic LOC, C3 clarity from signatures, C4 compile-time
verifiability, C5 AI predictability, C6 BCL interoperability, C7 async/cancellation semantics,
C8 credible performance feasibility. All references are to the pinned evidence; none of these
decisions authorize implementation.

| # | External capability | Family | Decision | Criteria | One-line rationale |
| --- | --- | --- | --- | --- | --- |
| 1 | `FSharpOption<T>` as an absence carrier | F1 | reject | C1,C4,C6,C8 | null-as-None, static indexer members, NRE receivers, 24 B/Some; FunnySharp `Option<T>` is strictly safer |
| 2 | `FSharpValueOption<T>` as an absence carrier | F1 | reject | C1,C2,C5 | a second absence vocabulary with two accessor failure modes; FunnySharp has one `Option<T>` |
| 3 | null↔absence and `Nullable<T>`↔absence semantics (`OfNullable`/`ToNullable`/`OfObj`/`ToObj`) | F1 | adapt | C1,C4,C6 | semantics already match FunnySharp (`FromNullable`, `ToOption`, normalization); keep as documented rules, not carriers |
| 4 | "options never contain runtime null; absence has one failure type" | F1,F11 | adopt | C4,C5,C6 | F# allows `Some null` and mixes NRE/`InvalidOperationException`/`ArgumentException`; FunnySharp's rule is stricter and testable |
| 5 | `Option.count`/`toArray`/`toList` zero-or-one collection view | F1,F5 | reject | C1,C2 | no C# idiom benefit; FunnySharp has Sequence/Traverse for real collections |
| 6 | `FSharpResult<T,TError>` as a failure carrier | F2 | reject | C4,C6 | unchecked case accessors silently return `default`; `default(Result)` is Ok; STJ throws |
| 7 | Same-type-parameter error typing and `MapError`/`ToOption` boundaries | F2 | adopt | C1,C4 | already FunnySharp's shape; F# confirms error typing is first-class, not `Exception`-only |
| 8 | `ResultModule` function-first `FSharpFunc` API shape | F2 | reject | C1,C2,C6 | subset of FunnySharp's surface; callbacks need converters; no reduction in semantic LOC |
| 9 | `FSharpChoice<...>` 2-to-7 way branching | F3 | reject as API; defer interop-only reading | C1,C4,C5 | no exhaustive switch/deconstruction in C#; adjacent to the rejected general DU system |
| 10 | `FSharpFunc`/currying/`InvokeFast` as a function model | F4 | reject | C1,C2,C6,C8 | C# lambdas do not convert; 32 B per curried call; `Func`/`Action` are canonical |
| 11 | `FuncConvert`/`FromConverter` as interop adapters | F4 | defer | C1,C6 | useful only if a concrete consumer must hand callbacks to an F# API; no such consumer now |
| 12 | Operators pipe/compose (`|>`, `>>`) | F4 | reject | C1,C2,C5 | methods callable but semantically identical to application/composition; no C# grammar |
| 13 | F# collections as data carriers | F5 | reject | C2,C6,C8 | product contract already rejects a replacement collection universe; BCL read interfaces suffice |
| 14 | `SeqModule` lazy `IEnumerable` semantics | F5,F6 | reject | C5,C6 | FunnySharp pipelines already use `IEnumerable`/`IAsyncEnumerable` with documented single-pass rules |
| 15 | `System.Tuple` return shapes from library functions | F5,F11 | reject | C1,C6 | heap tuples; C# deconstruction only via `TupleExtensions`; `ValueTuple` is idiomatic |
| 16 | `FSharpAsync` as an async runtime | F6 | reject | C6,C7 | not awaitable, internal builder, thread-pool start, ambient default token, blocking `RunSynchronously` |
| 17 | Explicit-token-only cancellation, token forwarding, cancellation ≠ failure | F6 | adopt | C4,C7 | F# demonstrates the failure modes to avoid and the correct cancellation taxonomy |
| 18 | Bounded fan-out with wait-for-started-work on cancellation (`Async.Parallel`) | F6 | adapt | C7,C8 | confirms the existing bounded-parallel contract; keep `Channel` backpressure and drain semantics |
| 19 | `async { }` and `task { }` computation expressions | F6 | reject | C1,C6 | compiler-inlined language features; C# has `async`/`await`; F# itself prefers `task` for BCL interop |
| 20 | `MailboxProcessor` actor runtime | F8 | reject | C1,C7,C5 | body unreachable from C#, sync `PostAndReply`, constructor token; contract rejects actor runtimes |
| 21 | `Channel<T>` + explicit token as the actor-boundary substitute | F8,F6 | adopt | C6,C7 | BCL-first, already FunnySharp's concurrency carrier; use only when a concrete consumer needs it |
| 22 | `EventModule`/`IEvent` event algebra | F6 | defer | C1,C6 | BCL events and `IObservable<T>` cover the need; `IEvent` is already `IObservable`-compatible if a consumer passes one |
| 23 | `LanguagePrimitives` generic equality/comparison/comparer objects | F11 | reject; adapt as policy | C6,C8 | BCL `EqualityComparer<T>.Default`/`Comparer<T>.Default` are faster (2.2–4.2×) and allocation-free |
| 24 | `Unit` as a value-less result type | F4 | reject | C1,C6 | `void`, `Task`, `ValueTask` are the C# forms |
| 25 | Safe default/uninitialized state rule | F11 | adopt | C4,C5 | `ValueOption` default is safe, `Result` default is a false success; FunnySharp must state one rule |
| 26 | "No nullable annotations on the wire" lesson | F11 | adapt | C4,C6 | FSharp.Core emits none; FunnySharp should keep annotating its own surface and not rely on foreign annotations |
| 27 | DU/struct serialization policy lesson | F11 | adapt | C6 | STJ cannot serialize DU-shaped values without a converter; FunnySharp needs an explicit policy, not an assumed default |
| 28 | Diagnostic `ToString` text (`Some(x)`, `[1;2]`) as a contract | F11 | reject | C3,C6 | docs call these diagnostics only; FunnySharp already refuses to promise serialization/display formats |

### 4.1 Rationale detail for the load-bearing decisions

**#1/#2 absence carriers — reject.** Criterion 1: C# consumers cannot write `o.IsSome` (CS1546)
and must remember `null` is `None`. Criterion 4: `None` is not type-distinct from uninitialized
or mis-set references; the compiler cannot reject `none.Value`, and the failure is an NRE.
Criterion 6: no NRT metadata, so nullable analysis is blind. Criterion 8: 24 bytes per `Some`
and a class allocation on every present value. FunnySharp's `readonly struct` `Option<T>` with
`default = None`, `TryGetValue`, `Match`, and no throwing accessor already implements the safest
combination of both F# types (docs/option.md:8–17). Nothing in FSharp.Core improves on it.

**#3 semantics — adapt.** The conversions are small and their behavior is well-defined and
already mirrored: `OfNullable(null) → None`; `ToNullable(None).HasValue == false`;
`ToObj(None) → null`; `OfObj(null) → None` (`csharp-out:21–27`). FunnySharp's `FromNullable`,
`ToOption`, and null-normalization rules (docs/option.md:21–31) cover the same ground. The
adapt value is documentary: cite FSharp.Core as precedent for null→absence and the
"normalize null in, no null inside" rule.

**#6/#7 Result — reject the carrier, adopt the typing.** Error typing by generic parameter is
the shape FunnySharp already publishes; `MapError` and `ToOption`/`ToValueOption` boundaries
correspond to the shipped `MapError` and `ToOption` (docs/result.md:23, 36–41). The rejected
carrier is the unsafe accessor design and the false-success default. Keep the FunnySharp
surface, which is a strict superset in composition terms.

**#10 function grammar — reject.** The relevant measured numbers are 32 B per curried partial
application versus 0 for a C# delegate, and 24 B per `Some`. Any FunnySharp API whose signature
is `FSharpFunc`-shaped would push that cost and the `FuncConvert` boilerplate onto consumers
while reducing neither semantic LOC nor ambiguity. Criterion 5 also suffers: an agent asked to
call an F#-shaped library from C# has to remember converter calls that no C# intuition supplies.

**#16 async — reject.** The C# consumer cannot even `await` the type (CS1061), cannot build the
builder (CS1729), and gets ambient cancellation by default. The correct extraction is the
positive rule set (#17): caller-owned token forwarded exactly, cancellation stays cancellation,
no hidden `Task.Run`. `StartAsTask` starting "in the thread pool" is exactly the
hidden-scheduling pattern FunnySharp's contract rejects.

**#20/#21 actor boundary — reject the runtime, adopt only the substitute.** A MailboxProcessor
loop is ~13 semantic units of idiomatic async (inventory `inv:§13.E`) and is not expressible
from C#; the closest truthful statement is "use `Channel<T>`". Because the product contract
already commits to `Channel` backpressure and rejects schedulers/actor runtimes, this memo does
not propose new work — it strengthens the decision record with a pinned counter-example.

## 5. Data for the FunnySharp decision record

1. **Default/uninitialized rule (F11):** carry one explicit sentence across `Option`,
   `Result`, `Validation`, and state types — a default value must be the non-success / absent
   case where that is the safe reading, and no accessor may return `default` for the inactive
   case. Evidence: `FSharpValueOption` default = `ValueNone` (safe) vs `FSharpResult` default =
   `Ok(default)` (unsafe) vs `FSharpResult.ErrorValue`/`ResultValue` silently returning default.
   FunnySharp currently satisfies this; state it as a rule.
2. **Option/OptionStruct:** no second absence type. `FSharpOption` vs `FSharpValueOption`
   duplicated vocabulary (`IsSome`+`IsValueSome`) and failure modes; one `Option<T>` struct with
   `None` default is the reduced surface. `OptionStruct` should only be reconsidered if an
   unmanaged/`ref struct` consumer requirement appears; no such requirement is in evidence.
3. **Result error typing:** keep `Result<TValue, TError>` with an unconstrained `TError` and
   no exception coupling. F# proves error typing is workable without exceptions; it also
   demonstrates `DefaultWith(error → value)` as a legitimate recovery shape already present in
   FunnySharp (`Recover`). No change requested.
4. **Async cancellation model:** keep task/`ValueTask` + explicit `CancellationToken`; never
   introduce a default/ambient token; `StartAsTask`-style firing on the thread pool must remain
   outside the core. F# also confirms `IAsyncEnumerable` is not part of the async model —
   FunnySharp is already ahead there.
5. **Mailbox/actor boundary:** keep rejected as a runtime; if a future goal needs it, the
   boundary is `Channel<T>` + explicit token + the state-machine types, not an agent type.
6. **Do not imitate:** null-as-absent reference option; `FSharpFunc`-based currying and
   function-first module methods; `FSharpAsync` as a runtime; `MailboxProcessor`; `Choice`
   multi-way branching; unchecked case accessors; ambient/default cancellation tokens;
   `System.Tuple` returns; `FSharpList`/`FSharpMap`/`FSharpSet` as required carriers.
7. **Vocabulary/stability boundary (candidate input):** if the decision record needs a sentence
   for why FunnySharp does not mirror F# shapes, the sharpest one is "F# shapes are the
   compiled encoding of language syntax; C# consumers see the encoding, not the syntax." The
   six expected-failure probes plus the measured 24 B/32 B allocation penalties are the
   concrete backing.

## 6. Open questions

1. Should the contract explicitly name the default-state rule as a cross-type invariant, or is
   the per-type documentation (docs/option.md:8, docs/result.md:13) sufficient? The F# baseline
   shows the invariant matters, but Goal 14 owns the wording.
2. Is any first-class *reading* of F# data (not carriers) worth a later goal — e.g. accepting
   `IEnumerable<T>` is already universal, and `IReadOnlyList<T>`/`IReadOnlyDictionary<T,K>`
   cover `FSharpList`/`FSharpMap`, so is a documented "F# interop note" enough without code?
3. Serialization policy: `System.Text.Json` handles `FSharpOption`/`FSharpList`/`FSharpMap` but
   not `FSharpResult`/`FSharpChoice` (`csharp-out:120–126`). If FunnySharp ever ships
   converters, the option-shaped case (`Some(null)` vs `None` both `null`) needs an explicit
   rule. This memo takes no position on shipping converters.
4. Microbenchmarking `LanguagePrimitives.GenericEquality` versus BCL comparers was indicative
   (204–392 ms vs 58–94 ms over 20M). If the decision record cites these numbers, a proper
   BenchmarkDotNet run in `benchmarks/` is required.
5. FSharp.Core 11.0.100 was out of scope by pin; if FunnySharp later targets .NET 11, a
   re-pin and diff is needed (not now).

## 7. Not examined / UNVERIFIED

- **UNVERIFIED: exact `dotnet/dotnet` VMR → `dotnet/fsharp` tag commit ancestry.** The nuspec
  names VMR commit `e34a38d2…`; its `src/fsharp/eng/Versions.props` declares the same
  `10.1.401`/`15.2.401` values as tag `v15.2.401` (`36bf12fb…`), but no submodule-pin
  verification was performed.
- **UNVERIFIED: FSharp.Core 10.1.401 release notes.** The nuspec `releaseNotes` URL 404s and the
  `v15.2.401` release-notes tree has no `10.1.401.md`; the version identity rests on
  `eng/Versions.props`.
- Not examined: F# language-level features without a CLR surface (SRTP/inline, quotation, type
  providers, units of measure, query CE, printf formatting, `Array2D/3D/4D`,
  `StringModule`, `NativeInterop`); `lib/netstandard2.0` diff; FSharp.Core 11.x; deep source
  review of async/list/map/set implementations; load behavior of `MailboxProcessor` and
  `Async` thread-pool growth; Windows/macOS runtime behavior.
- Microbenchmarks are single-run, single-host, and not produced by the repository benchmark
  harness; treat them as directional (`inv:§14`).
