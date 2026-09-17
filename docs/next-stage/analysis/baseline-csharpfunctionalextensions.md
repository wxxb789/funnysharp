# Baseline analysis memo — CSharpFunctionalExtensions 3.7.0 (Goal 14)

Scope: the pinned CFE 3.7.0 baseline only. Facts, signatures and probe results are inventoried in
`docs/next-stage/inventory/csharpfunctionalextensions.md`; raw surface in
`docs/next-stage/inventory/generated/inv-csharpfunctionalextensions.md`. This memo gives **candidate** external-capability
decisions for the lead's `docs/next-stage/decision-record.md`. Decisions here are proposals, not final.

Provenance (short): CSharpFunctionalExtensions 3.7.0, nupkg SHA256 `3a7c3d59…9723c8`, `lib/net8.0` assembly,
upstream tag `v3.7.0` @ commit `e0fc908ecf0b006b0b5fdfea1d028137c9e5c5ae` (2026-03-02). Evidence classes:
(E1) generated public-surface dump; (E2) pinned source; (E3) compile/runtime probes against the pinned DLL
(`/tmp/opencode/probe-cfe`, net10.0 consumer).

> Citation note (lead, post-audit): `inventory:NNN` references in this memo point to
> `docs/next-stage/inventory/generated/inv-csharpfunctionalextensions.md` (the generated
> dump), not to the shorter human-readable inventory. The dump was regenerated with
> operator support after this memo was written, so line numbers may drift by roughly ten
> lines; locate cited content by the named type/member.

## 1. Baseline verdict in one paragraph

CFE 3.7.0 proves demand for a Result/Option vocabulary that de-nests ordinary C# error handling and removes manual
message plumbing: 1,841 public members, 1,664 extension methods, no dependencies, System.Text.Json integration.
The same evidence shows the cost of growing that vocabulary without a canonical decision: four outcome shapes
(`Result`, `Result<T>`, `Result<T,E>`, `UnitResult<E>`), two names per operation (`Ensure`/`Check`,
`Compensate`/`OnFailureCompensate`, `MapError`/`TapError`, `Execute`/`Tap`), six operand-split async extension classes
plus two mirrored sync classes expressing one composition concept, 64 overloads of `MapError`, an implicit-conversion
surface that silently chooses success vs failure, catch-all `Try` that swallows cancellation, and zero `CancellationToken`
parameters in the whole `Result` pipeline. The recommendation is to adapt a *small* canonical subset of the capability
(absence, outcome, guard/observe/recover, aggregation, Task/ValueTask parity with tokens) and reject the rest of the
mechanism (implicit conversions, throwing accessors as the primary API, global configuration, operand-class split,
alias and anti-alias families, `ICombine`, `BindZip`, value-object base classes).

## 2. Candidate decisions

Decision vocabulary: **adopt** = take the capability and semantics largely as-is; **adapt** = take the capability,
redesign the API/semantics; **defer** = evidence insufficient or another workstream owns it; **reject** = do not take.
Criteria numbers are the brief's (1 usefulness, 2 semantic LOC, 3 signature clarity, 4 compile-time verifiability,
5 AI predictability, 6 BCL interop, 7 async/cancellation, 8 performance).

### F1 — absence

| # | Capability (pin) | Decision | Criteria | Rationale and evidence | Must NOT inherit |
| --- | --- | --- | --- | --- | --- |
| 1.1 | `Maybe<T>`/`Maybe` value type (`inventory:469-646`) | **adapt** | 1, 3, 4, 6 | `default(Maybe<T>)` is `None` by layout (`Maybe/Maybe.cs#L15-L21`; probe), so no unsafe default state; `TryGetValue` carries `[NotNullWhen(true), MaybeNullWhen(false)]`; `None` is explicit and cheap; dictionary/`TryFirst`/`TryLast`/`Choose` bridges remove the value-type-default ambiguity of `FirstOrDefault` (README `#L402-L434`). This is the cleanest part of the baseline. | Implicit `T? → Maybe<T>` (`Maybe/Maybe.cs#L109-L117`) makes `null` and a legitimately absent computation indistinguishable at the call site; implicit `Maybe → Maybe<T>` (`#L119`); `==`/`!=` overloads that compare `Maybe<T>` with raw `T`/`object` (`#L147-L176`) hide the absence check; `Value` throwing (`#L77-L80`) and `IMaybe<T>` exposing it; `GetValueOrThrow(string?)` with an ambient default message |
| 1.2 | `MaybeExtensions` pipeline (`inventory:483-626`, 137 members) | **adapt** | 1, 2, 5 | `Map`/`Bind`/`Where`/`Or`/`Tap`/`Match`/`Choose` are the ordinary-C#-useful subset and reduce semantic LOC (CS5). Keep one canonical name per operation. | The 24 `Match` overloads incl. `KeyValuePair` specialisations and `(context, token)` permutations; `GetValueOrDefault` 13-way overload set; `Execute`/`ExecuteNoValue` obsolete aliases retained in 3.7.0 (`Maybe/Extensions/Execute.cs#L13`); `Task` + `ValueTask` mirroring in two namespaces |
| 1.3 | `NullableExtensions` (`inventory:648-661`) | **adapt** | 1, 6, 7 | Nullable → outcome is a real bridge in ordinary C#; synchronous, `Task`, `ValueTask` variants exist. Redesign: consistent `ConfigureAwait` policy and token propagation. | Hard-coded `ConfigureAwait(false)` (`Result/NullableExtensions.cs#L26-L31`) inconsistent with `Configuration.DefaultConfigureAwait`; `ToResultAsync` on a synchronous nullable receiver; `ValueTask`-returning sync method (allocation/consumption risk, criterion 7) |
| 1.4 | `MaybeEqualityComparer<T>` (`inventory:477-481`) | **defer** | 1, 6 | Valid niche for custom comparisons; no evidence in README/tests that it is a common need, and its `None`-hash-0 convention is a policy FunnySharp must own. | — |

### F2 — fail-fast outcome

| # | Capability (pin) | Decision | Criteria | Rationale and evidence | Must NOT inherit |
| --- | --- | --- | --- | --- | --- |
| 2.1 | `Result` / `Result<T>` explicit success/failure factories (`inventory:663-736`, `1317-1344`) | **adapt** core; **reject** default semantics | 3, 4, 5 | Explicit factories (`Result.Success`, `Result.Failure`, `Result.Of`) are the useful part. But `IsFailure` is a `bool` with `false` default, so `default(Result)` is **success** and `Error` then throws `ResultSuccessException` (probe; `Result/Internal/ResultCommonLogic.cs#L83-L84`). A sentinel/uninitialized state that means success is an unsafe default (criteria 3, 4). | `default` = success; `Error` throwing; success-forbids-error / failure-forbids-null-error guards that make construction a runtime check rather than a type; string-only error channel on `Result` and `IResult<T> : IResult<T,string>` (`Result/IResult.cs#L18`) |
| 2.2 | `Value`/`Error` throwing accessors + `GetValueOrDefault` (`Result/ResultT.cs#L18-L31`, `inventory:1325`) | **adapt** with redesign | 3, 4 | Informative exceptions (`ResultFailureException` with the error) are good diagnostics, and `TryGetValue`/`TryGetError` exist. As a primary API, property access that throws on half the states is runtime-checked correctness and invisible in signatures; `GetValueOrDefault` then silently converts failure to `default(T)` (probe: 42). FunnySharp should require a non-throwing accessor and/or a compiler-visible path to the value. | Throwing `Value`/`Error` properties; silent `GetValueOrDefault()` returning `default`; `ResultFailureException`/`ResultSuccessException` as flow control; `IValue<out T>`/`IError<out E>` covariance exposing throwing getters |
| 2.3 | Implicit conversions: `T→Result<T>`, `T/E→Result<T,E>`, `Result<T>→Result`, `→UnitResult<string>` (`Result/ResultT.cs#L47-L75`, `ResultTE.cs#L47-L82`, `Result.cs#L34`, `UnitResult.cs#L40`) | **reject** | 3, 4, 5, 6 | Conversion direction encodes the branch: `Result<string> r = "boom"` is **success** (probe), while the README's `Result r = "boom"` example does not compile at all (`CS0029`, README `#L524-L530`), and `Result<int,int> r = 5` fails `CS0457` (ambiguous user-defined conversion). `E→Result<T,E>` inverts meaning for the same-looking assignment. These are exactly the "impossible to infer from the signature" states criteria 3–5 forbid, and they break AI code generation (the README itself is wrong). | All implicit operators in the list above; `SimpleValueObject<T>→T` (`ValueObject/SimpleValueObject.cs#L27`) |
| 2.4 | Four outcome shapes + `IResult`/`IValue`/`IError`/`IUnitResult` (`inventory:437-467`, `663`, `1317`, `1332`, `1365`) | **reject** the shape sprawl; **adapt** the constraints idea | 1, 5, 6 | Four near-identical types plus four interfaces makes vocabulary selection ambiguous (criterion 5) and forces generic constraints to pick among them; `IResult<T>` inherits `IResult<T,string>` so string errors leak into every non-generic path. Keep one canonical outcome shape (with an error type parameter) that can express unit and value cases; provide a separate non-generic alias only if call-site evidence demands it. | Parallel `Result`/`Result<T>`/`Result<T,E>`/`UnitResult<E>` public surfaces; `IError<out E>`/`IValue<out T>` interfaces with throwing getters; `UnitResult` static/`UnitResult<E>` struct duality |
| 2.5 | `Ensure`, `Check`/`CheckIf`, `Tap`/`TapIf`/`TapTry`/`TapIfTry`, `TapError`/`TapErrorIf`, `Map`/`MapIf`, `MapError`, `Bind`/`BindIf`, `Compensate`/`OnFailureCompensate`, `Finally` (`inventory:738-1305`) | **adapt** the operation grammar; **reject** aliases and `Try` variants | 1, 2, 5 | Guard→observe→recover→terminal is the useful vocabulary (CS1/CS2: −1 to −3 semantic LOC, less plumbing). But the baseline has two names for one intent (`Ensure` vs `Check`, `Compensate` vs `OnFailureCompensate`, `MapError` vs `TapError`) and `TapTry`/`TapIfTry` conflate a side-effect step with an exception boundary. AI discovery and reader inference degrade with 32–64 overloads per operation name. Keep one canonical set with explicit async forms. | `Check`*(alias), `OnFailureCompensate`, `MapError`/`TapError` duplication, `*Try` observation variants, `MapIf`/`BindIf`/`TapIf` condition-flag permutations; `WithTransactionScope` is separately deferred (F7) |
| 2.6 | `Result.Try` boundaries (`inventory:728-735`, `Result/Methods/Try.cs`, `Try.Task.cs`) | **adapt** with redesign | 3, 7 | Exception→outcome at IO edges is the highest-value boundary capability. But `catch (Exception)` maps `OperationCanceledException` and `OutOfMemoryException` to domain failures (probe), silently violating criterion 7; `errorHandler ??= Configuration.DefaultTryErrorHandler` is process-global mutable policy; overload sets are ambiguous for throwing lambdas (probe `CS0121`); no token parameter exists. Redesign: per-call handler, exception filter list, `OperationCanceledException` rethrow/forward, token-first signatures. | Catch-all `catch (Exception)`; global `DefaultTryErrorHandler`; `Configuration.DefaultConfigureAwait`; token-free `Try`; multiple `Func<T>`/`Func<Task<T>>` overloads distinguishable only by lambda inference |
| 2.7 | System.Text.Json converters + `CSharpFunctionalExtensionsJsonSerializerOptions` (`inventory:1388-1403`) | **adapt** minimal; **reject** the shipped options singleton and error defaults | 3, 6, 11 | Interop with System.Text.Json is required for criterion 6 and CFE shows a workable DTO shape (`{isSuccess,value,error}`). Must be opt-in per `JsonSerializerOptions`, with a documented wire contract. | Mutable/global `JsonSerializerOptions Options` singleton (hidden shared state); leaked `C2i.Common.C2iCSharpFunctionalExtensions.FunctionalApiResult` namespace in the package source (`JsonSerializerOptionsExtensionMethods.cs#L1`, `ResultOfTEJsonConverterFactory.cs#L8`); malformed JSON with reference error type → `ArgumentNullException` (probe); silent default error text; `JsonException`/cancellation collapsed into one domain failure (`HttpResponseMessageJsonExtensions.cs#L14-L36`) |
| 2.8 | `HttpResponseMessageJsonExtensions` (`inventory:1392-1399`) | **defer** to F10 | 6, 10 | It is the only HTTP surface and is thin; the F10 workstream (Minimal API/ProblemDetails) should decide the shape. | `bool ensureSuccessStatusCode` parameter; `where E : new()` error synthesis; token-less `ReadAsStringAsync` on the error path; status-code-to-string-error conversion |
| 2.9 | `Result.FailureIf`/`SuccessIf` (bool/`Func<bool>`/`Task`/`ValueTask` × 4 shapes, `inventory:690-726`) | **defer** | 1, 3, 5 | Boolean-flag constructors can read as `FailureIf(condition, error)` boolean traps; some call-site demand exists but it is unclear it beats an ordinary `if`. The lead should test against FunnySharp call sites before adopting. | Boolean-parameter factories without named arguments; four-shape × four-predicate matrix |

### F3 — accumulation

| # | Capability (pin) | Decision | Criteria | Rationale and evidence | Must NOT inherit |
| --- | --- | --- | --- | --- | --- |
| 3.1 | `Combine` over `params`/`IEnumerable` + `FirstFailureOrSuccess` (`inventory:668-685`, `882-892`, README `#L35-L45`) | **adapt** | 2, 3, 4 | Validation aggregation is the largest semantic-LOC win (CS4: 3 vs 6 for three checks, linear in N). Redesign the contract: explicit result ordering, explicit separator parameter, per-error identity so callers can map errors to fields. | Global `Configuration.ErrorMessagesSeparator`; comma-joined single message as the only attribution; eager `params` evaluation without a documented ordering contract; `Result<System.Boolean,E>` as the internal combine shape leaking into overloads (`inventory:672`) |
| 3.2 | `ICombine` typed-error composition (`inventory:437-439`) | **reject** | 4, 5, 7, 8 | `ICombine Combine(ICombine value)` erases the error type, requires runtime casts (probe), boxes, and makes `where E : ICombine` constraints viral on `Combine`/`Compensate` APIs. A generic `ICombinable<TError>`-style constraint or an explicit composer delegate is strictly better. | Non-generic self-referential `ICombine`; `where E : ICombine` requirements |
| 3.3 | Async `Combine` fan-out (`inventory:146-186`, `Combine.Task.cs#L14`) | **reject** default; **adapt** a bounded/ordered alternative | 7, 8 | `Task.WhenAll(tasks)` starts all operations with no degree limit and no token; no overload accepts a token. `CombineInOrder` proves the design tension (sequential fix) but there is no bounded-concurrency option. FunnySharp's default accumulation should be ordered or bounded with token propagation. | Unbounded `WhenAll`; no concurrency limit; no token; `CompleteInOrder` as the only ordered primitive |

### F4 — function grammar

| # | Capability (pin) | Decision | Criteria | Rationale and evidence | Must NOT inherit |
| --- | --- | --- | --- | --- | --- |
| 4.1 | `Tap`/`TapError` observation, `Match` terminal, `Deconstruct`, LINQ `Select`/`SelectMany` (`inventory:1178-1305`, `933-940`, README `#L618-L655`) | **adapt** | 1, 2, 5 | These are the readability backbone of the chains; LINQ query syntax reduces binds (−2 per extra bind, CS3). Redesign `Tap` to reject delegate return values or provide an explicit `TapIgnoringResult` to prevent silent swallowing. | `Tap` accepting `Action<T>` while upstream examples pass `Result`-returning calls that are silently discarded (README `#L63`; `AsyncUsageExamples.cs#L27-L28`; probe: compiles with no warning); the 24-overload `Match` surface; `Execute`/`ExecuteNoValue` obsolete aliases |
| 4.2 | `Finally` terminal (`inventory:55-58` etc.) | **adapt** | 3, 7 | Good terminal for both branches; must document task ownership and ensure no orphaned work (criterion 7). | Sync-over-async or fire-and-forget shapes; implicit `ConfigureAwait` policy |

### F5 — collections and traversal

| # | Capability (pin) | Decision | Criteria | Rationale and evidence | Must NOT inherit |
| --- | --- | --- | --- | --- | --- |
| 5.1 | `Choose`, `TryFirst`/`TryLast`/`TryFind`, `ToList` (`inventory:505-506`, `618-622`, `605`) | **adopt** capability into FunnySharp vocabulary | 1, 2, 4 | Direct replacement for `FirstOrDefault`/`TryGetValue` null-ambiguity; deferred LINQ, no surprises. | Nothing specific; keep the absence type canonical |
| 5.2 | `BindZip` arity 2..8 with nested `ValueTuple` (`inventory:811-866`, 56 overloads) | **reject** | 3, 5 | Tuple-shape proliferation is discoverability poison; the last arity uses `(T1..T7,ValueTuple<K>)` (`inventory:823`). Use sequential binds or a tuple-of-results adapter decided at design time. | All `BindZip` overloads |
| 5.3 | Traverse/span/memory/error-location context | not present in CFE | — | No capability to adopt; a gap the FunnySharp F5 design must close on its own evidence. | — |

### F6 — async, streaming, concurrency

| # | Capability (pin) | Decision | Criteria | Rationale and evidence | Must NOT inherit |
| --- | --- | --- | --- | --- | --- |
| 6.1 | Task + ValueTask composition | **adapt** capability; **reject** structure | 3, 5, 6, 7 | Both TFM task types matter (criterion 6) and the library proves a uniform operator grammar works. The implementation strategy (six public static classes named by operand position, plus `ValueTasks` mirrors) is an artifact: the six async operand classes hold 664 members (662 extension methods), with ~10 left/right/both permutations per operation. | `AsyncResultExtensionsLeftOperand`/`RightOperand`/`BothOperands` public naming and split; duplicated `Task`/`ValueTask` method universes; operand-position classes as the discoverable surface |
| 6.2 | Cancellation | **reject** the baseline's coverage; **adopt** token propagation as a design requirement | 6, 7 | `CancellationToken` appears in only 20 extension methods and never in a `Result` operator (inventory §2; 16 are `Maybe.Match`); `Try` intentionally converts `OperationCanceledException` into a failure (probe). A pipeline surface without tokens cannot satisfy criterion 7. | Token-free `Result` operators; cancellation-as-domain-failure; token only on terminals |
| 6.3 | `ConfigureAwait` policy (`Result.Configuration.DefaultConfigureAwait`) | **reject** | 3, 5, 7 | Process-global mutable flag; `NullableExtensions` ignores it. Library-level `ConfigureAwait` policy should be fixed and documented, not user-global. | Global mutable configuration |
| 6.4 | No hidden `Task.Run`; no hidden parallelism outside `Combine` | **adopt** as invariant | 7 | Grep of the pinned snapshot finds no `Task.Run`; the only fan-out is `Combine`'s `WhenAll` (6.5/3.3). | — |
| 6.5 | `Combine` fan-out | see 3.3 | 7, 8 | — | Unbounded concurrency |
| 6.6 | `IAsyncEnumerable`/Channels/`TimeProvider` | not present in CFE | — | No external capability to decide; FunnySharp design owns streaming/clock policy. | — |

### F7 — effects, resources, environment

| # | Capability (pin) | Decision | Criteria | Rationale and evidence | Must NOT inherit |
| --- | --- | --- | --- | --- | --- |
| 7.1 | `BindWithTransactionScope`/`MapWithTransactionScope` (49 public members across sync + `ValueTasks`, `inventory:791-810`, `1132-1139`, `1825-1839`, `1984-1989`) | **defer** | 1, 7 | Useful only if FunnySharp ships a data-access story; ambient `TransactionScope` hidden inside an extension makes resource ownership invisible in the signature. No retry/timeout/`TimeProvider`/DI capability exists at all, so there is no baseline to adopt (criterion 7 asks for explicit resource semantics). | Ambient transaction creation hidden in an operator; effect semantics not in the type system |

### F8 — state transitions and machines

No CFE capability exists (generated dump: no state/machine/replay types). This baseline offers nothing to adopt; it
corroborates that a state-machine surface is not required for ordinary C# Result/Option work. No candidate decision.

### F9 — optics and immutability

| # | Capability (pin) | Decision | Criteria | Rationale and evidence | Must NOT inherit |
| --- | --- | --- | --- | --- | --- |
| 9.1 | `ValueObject`/`ValueObject<T>`/`ComparableValueObject`/`SimpleValueObject<T>`/`EnumValueObject`/`Entity` (`inventory:385-435`, `1346-1351`, `1374-1384`) | **reject** implementation; **defer** a FunnySharp value-object/serialization policy to F11 | 1, 3, 4, 5, 8 | Structural equality via `SequenceEqual` and a mutable cached hash on an abstract base (`ValueObject.cs#L16-L57`); proxy-type detection by class-name string (`"Castle.Proxies."`, `"Proxy"`); `IComparable` over an abstract base; `SimpleValueObject<T>` implicitly converts to `T`, erasing the wrapper (`SimpleValueObject.cs#L27`). None of this earns compile-time verifiability or clarity, and it is orthogonal to the goal's rejection of pervasive immutability. | All six base classes; implicit `SimpleValueObject<T>→T`; string-based proxy detection; cached-hash mutable state on an abstract base; `Entity` `IComparable` |
| 9.2 | Lens/Prism/Traversal | not present | — | Nothing to decide. | — |

### F10 — HTTP integration

| # | Capability (pin) | Decision | Criteria | Rationale and evidence | Must NOT inherit |
| --- | --- | --- | --- | --- | --- |
| 10.1 | CFE's HTTP surface (4 members) + README pointers to third-party HttpResults/ProblemDetails packages (`README.md#L696-L735`) | **defer** to the lead's F10 workstream | 6, 10 | CFE deliberately does not own ProblemDetails/Minimal API mapping; comparing it here would prejudge the F10 baseline set. Note the third-party dependency precedent is out of scope for FunnySharp (independence constraint). | Status-code → string-error conversion; `bool ensureSuccessStatusCode`; `new()`-synthesised errors; token-less error reads |

### F11 — cross-cutting

| # | Capability (pin) | Decision | Criteria | Rationale and evidence | Must NOT inherit |
| --- | --- | --- | --- | --- | --- |
| 11.1 | XML documentation policy (shipped `.xml`: 1,076 summaries, 7 type docs, 117 `<returns>`, 122 `<param>`; `NoWarn 1591`) | **adapt** a stricter policy | 3, 5, 11 | Criterion 3 demands that evaluation order, exceptions and cancellation are inferable from the signature **and its documented contract**; CFE's params/returns coverage is ~11% and several type docs are copy-paste garbage (`ResultExtensions`, `UnitResult`, `Maybe`). FunnySharp should require param/return docs, no 1591 suppression, and doc-tested README snippets. | Missing param/return docs; misplaced/copy-pasted summaries; README examples that do not compile (`Result = "..."`), reference removed APIs (`Unwrap`), and omit `[Obsolete]` |
| 11.2 | Nullability annotations | **adapt** a uniform policy | 3, 4 | `Maybe` files are annotated; `Result` files are oblivious, so `Value`/`Error`/factory args surface as `Unknown` in metadata (inventory §4.5). Inconsistent annotations are an AI-predictability and consumer-warning hazard. | Partial `#nullable enable`; oblivious public value/error members |
| 11.3 | Analyzers and compiler feedback | **reject** as a source (none shipped); record as a FunnySharp gap | 4, 5, 11 | The nupkg has no analyzers/props/targets (`unzip -l`), so misuse (ignored `Result`, implicit conversions, ambiguous `Try`) is unguarded. Funcky's analyzer surface is the comparative baseline for the lead's F11 decision; CFE provides no capability to adopt. | Shipping no misuse analyzers while exposing conversion-heavy APIs |
| 11.4 | Obsolete-retention policy | **reject** | 5, 11 | `Maybe.Execute`/`ExecuteNoValue` are `[Obsolete]` but public and still documented in 3.7.0, plus four `Execute*` families; alias retention widens the discovery surface for no benefit. FunnySharp's stability boundary should delete aliases at the documented break, not keep both vocabularies forever. | Long-lived aliases; two names for one operation (`Ensure`/`Check`, `Compensate`/`OnFailureCompensate`) |
| 11.5 | Packaging/dependency boundary | **adopt** property | 6, 11 | Single package, zero runtime dependencies, SourceLink, JSON trimmed from `netstandard2.0`, TFMs `netstandard2.0;net6.0;net8.0`. This matches FunnySharp independence and criterion 6. | Shipping analyzers/vendored BCL copies/dependency chains; global static configuration fields |

## 3. Must-not-inherit list (condensed)

1. Implicit conversions that choose a branch: `T→Result<T>`, `T/E→Result<T,E>`, `E→UnitResult<E>`, `SimpleValueObject<T>→T`
   (`Result/ResultT.cs#L47`, `ResultTE.cs#L47-L71`, `UnitResult.cs#L40`, `SimpleValueObject.cs#L27`).
2. Unsafe default: `default(Result*)` is success with a throwing `Error`; throwing `Value`/`Error` as the primary accessors;
   `GetValueOrDefault` silently producing `default(T)`.
3. Global mutable configuration: `Configuration.ErrorMessagesSeparator`, `DefaultConfigureAwait`, `DefaultTryErrorHandler`
   (`Result/Result.Configuration.cs#L7-L11`).
4. Catch-all `Try` that maps cancellation and fatal exceptions to domain failures; token-free async operators.
5. Operand-class explosion: `AsyncResultExtensions{Left,Right,Both}Operands` × `Task`/`ValueTask` × namespace mirror (664 methods).
6. Alias/anti-alias families and their overload matrices: `Ensure`/`Check`, `Compensate`/`OnFailureCompensate`,
   `MapError`/`TapError`, `Map`/`MapIf`, `Bind`/`BindIf`, `Tap`/`TapTry`, `BindZip` (56), `MapError` (64), `TapErrorIf` (64).
7. `ICombine` type-erasing combination; `where E : ICombine` constraints.
8. Unbounded `Task.WhenAll` accumulation with no token/concurrency limit.
9. `Tap(Action<T>)` silently discarding a returned `Result` (compiles with no warning; probe; README `#L63`).
10. Value-object base classes with cached mutable hash, string proxy detection and implicit unwrapping.
11. JSON policy defects: leaked `C2i.Common.*` namespace, global options singleton, `ArgumentNullException` on malformed
    JSON with reference errors, cancellation/`JsonException` collapse, token-less error reads.
12. Documentation drift and doc gaps: 7/42 type docs, 117 returns, 122 params, `NoWarn 1591`, non-compiling README examples,
    README referencing a removed `Unwrap`.

## 4. Semantic-LOC findings (criterion 2)

Using the rule and counts in the inventory (§14; snippets pinned at `e0fc908`):

- Guard-heavy validation chains save the most: `Combine` of three results is 3 semantic LOC vs 6 (CS4, README `#L35-L45`);
  each additional validation adds ~1 unit of savings.
- Multi-bind validation via LINQ query syntax saves ~2 units per two binds (CS3, README `#L618-L655`).
- A guard/observe/recover chain over a `Maybe`-returning API is roughly at parity once the equivalent explicit branches are
  counted (10 vs 11, CS1) but removes manual error plumbing and nesting; the async variant is −3 (CS2, `AsyncUsageExamples.cs#L19-L28`).
- Absence bridges (`TryFind`, `TryFirst`, `Choose`) are 1–2 units vs 1–3 for the BCL pattern; their real gain is
  type-level absence (criterion 4), not LOC.
- Conclusion: criterion 2 supports a Result/Option **pipeline surface**, not the 1,664-method surface; the marginal
  operator families (alias pairs, `*If`, `*Try`, `BindZip`, KVP `Match`) return ~no LOC and cost readability/discovery.

**UNVERIFIED:** criterion 2 does not define a counting unit. The counts above are reproducible only under the stated rule
and remain analyst judgment; the raw snippets are pinned so the lead can recount or substitute a stricter rule.

## 5. Open questions for the lead

1. Canonical outcome shape: one parametric outcome with unit/value cases, or the `Result`/`Result<T>` pair? (2.4)
2. Value/absence accessor contract: `TryGetValue` only, or a compile-time-proven "extracted" value type; and what is the
   sanctioned way to read `default`-state values? (2.1, 2.2)
3. `Try` policy: which exception classes map to domain errors, which rethrow, and how `OperationCanceledException` is
   distinguished; per-call or per-pipeline handler. (2.6)
4. Accumulation contract: is a joined error message acceptable, or does FunnySharp require structured per-input errors and
   a bounded parallel default? (3.1, 3.3)
5. Task/ValueTask scope: full parity mirror or only at I/O boundaries, and where the operand-class split is replaced by
   plain `Task`/`ValueTask`-returning delegates. (6.1)
6. Cancellation and `ConfigureAwait` policy for library code (fixed vs configurable). (6.2, 6.3)
7. JSON wire contract for outcome types, including the malformed-input and reference-error cases; is a converter package
   in scope at all? (2.7)
8. Whether Funcky-style analyzers are in scope for FunnySharp to close the misuse gaps CFE leaves open. (11.3)

## 6. Not examined (analysis scope)

Same list as the inventory's "Not examined", plus: no FunnySharp-side call sites were compared (the lead's
`call-sites.md` owns side-by-side); no benchmark of CFE was run, so criterion-8 claims here are static (allocations in
`Combine`/`Choose`/`ICombine`, `SequenceEqual` value-object equality) and marked **UNVERIFIED: no benchmark executed**;
the net6.0/netstandard2.0 surfaces were not diffed; the strong-named package, CI pipeline, upstream issues and the
third-party FluentAssertions/HttpResults packages were not inspected.
