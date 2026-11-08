# Compile-Time Fused Execution Evidence

This record supports the maintainer's judgment for
[Goal 0039](goals/archive/0039-goal.md): whether a bounded, explicit subset of FunnySharp's
composable calculations can be translated by a C# source generator into specialized execution
that preserves the promised semantics and how that changes the consumer-expression versus
runtime-cost trade-off. It records the chosen subset, the executable prototype, the semantic
ledger, the comparison against a competent delegated implementation, a fair hand-written loop,
LINQ, and the generation/build/code-size costs.

**Decision: ADAPT (summary-only), not shipped in this goal.** The generated summary removes
FunnySharp's own delegate/state composition tax: it matches a hand-written loop and is about 2.1x
faster than the delegated `Summary.Zip` on the recorded workload, while LINQ's one-pass
`Aggregate` remains allocation-free and close in time. It is not a general performance win. A
future product goal may adopt an additive, opt-in source generator for the summary subset
described below. Decoder fusion is deferred: its valid path is faster, but its invalid path is
slower and its safe subset excludes the nested object shapes that made Goal 0026 representative.
No public capability, package, grammar, product contract, stability entry, or API baseline
changed in this research outcome.

Source identity is this Goal 0039 working tree on top of commit `99ed3c8`; the landing commit
becomes the durable identity. Measurements were taken on this host:

- Ubuntu 24.04.5 LTS, Linux 7.0.0-1017-azure x86_64, 4 logical cores.
- .NET SDK 10.0.401, runtime 10.0.12 x64; Release build; `TieredCompilation=false`.
- Date: 2026-11-08 UTC.

Allocation is the blocking comparison; timing is directional on this shared host. Generation
time and build time are reported separately from runtime rows.

## Chosen Calculations And Why

Goal 0034's summary composition is the primary representative. `Summary.Zip` over `Count`, `Sum`,
`Min`, and `Max` is a complete combined folding workload with value results and no error-path
construction. Its promised behavior is observable end to end: one source enumeration, source
order, checked arithmetic, empty extrema, null-key skipping, first-key tie retention, component
order, exceptions, and disposal.

Goal 0026's decoder is the secondary validation calculation. The prototype supports a flat object
with required/optional/nullable scalar fields and lists of supported leaves. The generated success path parses
the same `JsonElement` value; every input that cannot be proven valid before construction calls the
declared runtime decoder, so the exact `DecodeError` locations, kinds, order, and text come from
the authoritative implementation.

Goal 0027's `Contract<T>` was assessed and not prototyped. A generated `Validate` would need to
construct and prefix `DecodeError` locations, which is internal to the core. A valid-fast-path plus
runtime fallback would either call contract getters twice on failure or require a new public
error-prefixing API. Both are worse research artifacts than the decoder's immutable `JsonElement`
fallback; this is a non-adoption, not evidence that contract fusion is beneficial.

## Prototype Shape

The generator is a non-packable, non-shipping research project:
[FunnySharp.FusedExecution.Generator](../tests/FunnySharp.FusedExecution.Generator/). It injects
two internal marker attributes into the consuming compilation and processes opt-in partial methods.
The existing static description remains the source of truth:

```csharp
[FusedSummary(nameof(Statistics))]
public static partial (int Count, decimal Total, Option<decimal> Min, Option<decimal> Max)
    Evaluate(IEnumerable<OrderLine> source);

[FusedDecoding(nameof(OrderDecoder))]
public static partial Validation<OrderInput, DecodeError> Decode(JsonElement input);
```

- The named field must be `static readonly` with an inline initializer, and nothing else in the type
  may write it. Helper-built descriptions, variables, and runtime-composed delegates are not
  analyzed; a write from a static constructor or another field initializer is rejected because the
  delegated path would use the replacement while the generated method kept the initializer's shape.
- Every unsupported construct produces an error diagnostic and the build fails; the generator
  never silently substitutes a different guarantee. Recognizer rejections (`FS3901`, `FS3902`,
  `FS3904`) also emit a throwing body; method-shape rejections (`FS3903`) emit no implementation,
  because a second method declaration would not compile.
- Unsupported declarations: `FS3901` (summary shape), `FS3902` (decoder shape), `FS3903` (method
  signature), `FS3904` (field declaration). The rules are listed in
  [AnalyzerReleases.Unshipped.md](../tests/FunnySharp.FusedExecution.Generator/AnalyzerReleases.Unshipped.md).

## Supported Construct Table

| Area | Supported | Generated behavior |
| --- | --- | --- |
| Method declaration | `static partial`, supported accessibility, one non-scoped by-value parameter, non-generic, non-nested, non-file-local containing partial class | Generated implementation; other modifiers or shapes are `FS3903` |
| Summary source | `IEnumerable<TSource>` | One `foreach`, `ArgumentNullException` before enumeration |
| Summary composition | One `Summary.Zip` of 2–4 components | Components evaluated in operand order per item |
| Summary components | `Summary<TSource>.Count()`, `Sum(selector)`, `Min(selector)`, `Max(selector)` | Checked count/sum; Option extrema with null skip and first tie |
| Summary selectors | `static` expression lambdas with one named, non-scoped by-value parameter | Body bound in a static local function with the declared parameter and return types; invoked once per reached item per component. Only the accumulation addition is `checked`. |
| Summary result | The field's `TResult` tuple | Tuple literal with the same element types |
| Decoder source | `JsonElement` | Object-kind check, then first-match property lookup |
| Decoder composition | One `Decoder.Object` of 2–4 fields | Fields parsed in argument order; object constructed once after all pass |
| Decoder fields | `Field`, `OptionalField`, `NullableField` with a constant name over supported leaves/lists; combine parameter type exactly equal to the field result type | Required/optional/nullable absence policy reproduced |
| Decoder leaves | `String`, `NonEmptyString`, `Boolean`, `Int32`, `Int64`, `Decimal`, `Double`, `Guid`, `DateTime`, `DateTimeOffset`, `Element` | Same `JsonElement` parse API and kind checks as the runtime leaf |
| Decoder lists | `List`/`NonEmptyList` of a supported leaf, including `Element` | Array kind, optional non-empty check, source-order construction exposed as `Array.Empty<T>()` for empty arrays and `AsReadOnly()` otherwise |
| Decoder combine | `static` expression lambda whose body is `new T(...)` (or `new T(...) { ... }`) | Bound in a static local function with the lambda's declared parameter types; construction after all fields are proven valid |
| Decoder invalid input | Any input the fast path does not prove valid | `return <field>.Decode(input);` — exact runtime result |

## Unsupported And Dynamic Treatment

| Construct | Treatment |
| --- | --- |
| `Summary.Create`, `MapResult`, nested `Zip`, field/variable components | `FS3901`; no generated execution |
| Custom extrema comparer | `FS3901`; the generated path would use `Comparer<TKey>.Default` |
| Non-static, statement-bodied, or method-group selectors | `FS3901`; no capture or purity inference |
| `Sum` over a numeric type other than `int`, `long`, `decimal`, `double`, `float` | `FS3901`; the prototype avoids promotion and custom `INumber` zero semantics |
| `IAsyncEnumerable<T>`, span, and memory summary sources | `FS3903`; not prototyped. Synchronous `IEnumerable<T>` is the supported streaming form. |
| `Decoder.From`, `Map`, `Bind`, `Ensure`, `Validate`, `OptionalValue` | `FS3902`; arbitrary delegates are opaque |
| Nested `Decoder.Object`, lists of objects | `FS3902`; a fallback-based fast path cannot preserve nested combine invocation counts |
| Combine bodies that are not object creation | `FS3902`; no arbitrary callback is inlined |
| Named `Zip`/`Object` arguments, named `Field`/`OptionalField`/`NullableField` arguments, helper-built fields, mutable fields | `FS3901`/`FS3902`/`FS3904` |
| Write to the description field from a static constructor or another field initializer | `FS3904`; the runtime field would no longer match the generated shape |
| Attribute on a local function, accessor, constructor, or operator | `FS3903`; no generated implementation |
| `scoped` parameter, extension `this` parameter, or by-ref return | `FS3903`; the generated signature cannot reproduce the modifier |
| Contract validation generation | Not prototyped; see the non-adoption above |

The decoder's invalid-input path is not a silent fallback to a different guarantee: it calls the
declared decoder, which is the authoritative implementation, and therefore returns the identical
`Validation` value. The only difference is execution cost (a second parse on invalid input).

## Semantic Preservation Ledger

The shared parity verifier runs 129 checks over the fixtures and is invoked by the xUnit suite and
the measurement harness. The deterministic tests additionally pin fault and boundary behavior.

| Contract | Evidence |
| --- | --- |
| Summary result equality | Empty, single, mixed, tied-price, and null-note fixtures match the delegated `Summary.Zip` result on every fixture. |
| One source enumeration | `CountingSequence` records 1 `GetEnumerator`, `Count + 1` `MoveNext`, `Count` `Current`, and 1 `Dispose` for both paths. |
| Selector count and order | `FusedSelectorLedger` records `quantity`, `price` per item in component order for both paths; a separate extrema ledger records `Min` and `Max` selector calls in component order. |
| Component fault order | A throwing quantity selector stops the later price component and both paths dispose once. |
| Source faults | `MoveNext`, `Current`, and `Dispose` faults propagate the same exception from both paths and both dispose once. |
| Checked arithmetic | The accumulation addition is `checked` in both paths: `decimal.MaxValue` addition throws `OverflowException` from both. Selector arithmetic keeps the project's checked/unchecked context: an unchecked `int`/`long` selector that overflows wraps identically in both paths. |
| First-key tie retention | `Min`/`Max` over comparer-equal but reference-distinct string keys keep the first instance in both paths, verified by reference identity. |
| Null source | Both paths throw `ArgumentNullException` with `ParamName == "source"`. |
| Empty source | Generated returns the same defaults (count 0, sum 0, `None` extrema) without selector calls; delegated allocates evaluation state. |
| Decoder valid values | Every valid fixture produces the same content as the runtime decoder for the `Boolean`, `Int64`, `Double`, `Guid`, `DateTime`, `DateTimeOffset`, `Element`, `String`, and plain `List` leaves, and reproduces the runtime read-only list shape. |
| Decoder invalid errors | Missing, null, empty, wrong-kind, malformed, empty-list, list-element, duplicate-property, root-array, and root-null fixtures produce identical `Path`/`Kind`/`Expected`/`Detail` sequences. |
| Duplicate properties | First matching property wins, matching `Decoder.TryGetFirstProperty`; a later duplicate is ignored. |
| Absence policy | `OptionalField`: missing is `None`, present null is an error. `NullableField`: missing and null are `None`. |
| No user callback duplication | Supported field decoders contain no user delegates; the combine lambda is bound as a static local function and runs once after all fields are valid. Unsupported nested combines are diagnosed. |
| Construction fault | A construction counter observes exactly one construction on the valid path and zero on an invalid path; a throwing constructor propagates its exception from that single site in both paths. |

## Comparison Consumers

The consumers run the same models, fixtures, and report construction. Shared definitions, fixtures,
and the verifier are not charged to any consumer.

| Consumer | File | Shape |
| --- | --- | --- |
| Generated summary | [FusedConsumers.cs](../tests/Shared/FusedExecutionComparison/FusedConsumers.cs) | Calls the generated partial method. |
| Delegated summary | same | Calls `Summary.Zip(...).Evaluate(...)`. |
| Hand-written summary | same | One `foreach` with local count/total/extrema state. |
| LINQ one-pass | same | `Aggregate` over a tuple with the same count/total/extrema rule. |
| LINQ separate | same | `Count`, `Sum`, `Min`, `Max`, four enumerations. |
| Generated decoder | same | Calls the generated partial method. |
| Delegated decoder | same | Calls `Decoder.Object(...).Decode(...)`. |
| Hand-written decoder | same | Direct `JsonElement` valid-path parse; no error model, so it is compared only on valid inputs. |

The shared generated definitions are
[FusedDefinitions.cs](../tests/Shared/FusedExecutionComparison/FusedDefinitions.cs); the verifier is
[FusedComparison.cs](../tests/Shared/FusedExecutionComparison/FusedComparison.cs). The prototype
source is 2,062 lines across the generator project (2,068 including the six-line `IsExternalInit`
shim); the generator assembly is 59,904 bytes. Using the representative 4/4 sample (a
four-component `Count`/`Sum`/`Min`/`Max` summary and a four-field decoder), generated source is
6,258 bytes / 155 lines in total: 729 B / 20 lines for the injected attributes, 1,832 B / 38 lines
for the summary declaration, and 3,697 B / 97 lines for the decoder declaration. The harness also
reports a minimal 2/2 sample separately: 3,453 B / 103 lines in total, with 739 B / 24 lines for
the summary and 1,985 B / 59 lines for the decoder. Per-file sizes are approximate because the
harness counts source lines with `SourceText.Lines` and includes a BOM. The summary field's
one-time static description allocates 1,760 bytes when its static constructor is forced; the
harness does this explicitly, and a `beforefieldinit` caller may initialize earlier or never.

Hyperlinq is not a fair combined-summary competitor: it specializes individual LINQ operators and
has no shared one-pass multi-summary protocol. No competitor package was added. `System.Text.Json`
source generation is not a fair decoder baseline either: it consumes UTF-8 bytes and does not
produce FunnySharp's accumulated `DecodeError` contract.

## Runtime Measurements

The harness executes complete consumers through one allocation counter
(`GC.GetTotalAllocatedBytes(precise: true)`), one JIT tier, and three internal rounds; the table
reports the best round per row. Two complete harness runs are shown.

Run 1:

| Scenario | Consumer | Allocated B/op | Time ns/op |
| --- | --- | ---: | ---: |
| Summary N=1024 | Generated | 32 | 27,594 |
| Summary N=1024 | Delegated `Summary.Zip` | 240 | 58,285 |
| Summary N=1024 | Hand-written loop | 32 | 29,133 |
| Summary N=1024 | LINQ one-pass `Aggregate` | 0 | 32,722 |
| Summary N=1024 | LINQ separate `Count/Sum/Min/Max` | 96 | 45,909 |
| Summary empty | Generated | 0 | 33 |
| Summary empty | Delegated `Summary.Zip` | 208 | 84 |
| Summary empty | Hand-written loop | 0 | 32 |
| Decoder valid | Generated | 256 | 835 |
| Decoder valid | Delegated | 352 | 1,155 |
| Decoder valid | Hand-written | 232 | 823 |
| Decoder invalid | Generated + fallback | 1,040 | 2,441 |
| Decoder invalid | Delegated | 904 | 1,584 |

Run 2:

| Scenario | Consumer | Allocated B/op | Time ns/op |
| --- | --- | ---: | ---: |
| Summary N=1024 | Generated | 32 | 27,454 |
| Summary N=1024 | Delegated `Summary.Zip` | 240 | 58,628 |
| Summary N=1024 | Hand-written loop | 32 | 26,985 |
| Summary N=1024 | LINQ one-pass `Aggregate` | 0 | 31,470 |
| Summary N=1024 | LINQ separate `Count/Sum/Min/Max` | 96 | 45,456 |
| Summary empty | Generated | 0 | 33 |
| Summary empty | Delegated `Summary.Zip` | 208 | 93 |
| Summary empty | Hand-written loop | 0 | 33 |
| Decoder valid | Generated | 256 | 835 |
| Decoder valid | Delegated | 352 | 1,141 |
| Decoder valid | Hand-written | 232 | 875 |
| Decoder invalid | Generated + fallback | 1,040 | 2,418 |
| Decoder invalid | Delegated | 904 | 1,558 |

Honest direction:

- **Summary:** the generated loop is about 2.1x faster than the delegated composition on this
  workload (240 → 32 B/op, about 87% less) and is within about 6% of the hand-written loop. The
  gain is the removal of delegate/virtual/state dispatch, not a different algorithm. The 32 B/op
  is the `IEnumerable<T>` enumerator, shared with the hand-written loop; the LINQ `Aggregate` row
  reports 0 B because `Enumerable.Aggregate`'s array fast path avoids that enumerator on this
  input. The 0 B is a LINQ array-path property, not a general `IEnumerable<T>` promise.
- **Summary empty source:** the delegated path allocates 208 B of evaluation state even with no
  items; the generated path allocates nothing. This is a real composition tax removed.
- **One-time summary description:** the required `Summary` field allocates 1,760 B when its type's
  static constructor runs. At the measured 208 B/op saving, it breaks even after about nine
  evaluations for a used type; a caller that never touches the type need not pay the allocation.
  The field is still required as the static description; the generated method does not read it at
  runtime. The measured static-constructor wall time (0.90–0.93 ms) includes first-time JIT and is
  not a pure construction cost.
- **Decoder valid:** the generated fast path is faster and lower-allocation than the delegated
  decoder (352 → 256 B/op, about 27% less; 1.37–1.38x faster). It is 24 B/op above the
  hand-written valid-only parse (232 B) because it exposes the runtime read-only list shape; time
  is within noise of that parse (within about 5%). The gain over the delegated path comes from
  skipping delegate/validation-state composition, not from a different parse.
- **Decoder invalid:** the generated path is **worse** than the delegated decoder (about 15% more
  allocation; 1.54–1.55x slower) because it performs the fast check and then re-parses through
  the runtime decoder. This is disclosed, not hidden. The errors are the runtime decoder's.
- **Copying and buffering:** the summary path passes each source item once into the selector local
  function, which is the same by-value argument passing the runtime delegate receives; it buffers
  nothing. On the valid decoder fast path a list field is buffered into a `List<T>` and exposed as
  `AsReadOnly()` (or `Array.Empty<T>()` when empty), matching the runtime shape; on an element
  failure the partial buffer is discarded and the runtime decoder builds its own list. The input
  document is not copied; the invalid path re-reads the same `JsonElement` through the runtime
  decoder. No span, destination-capacity, or partial-write path exists in this prototype.
- **Timing caveat:** the host is shared; timing is directional. Allocation is the stable signal.

Run the harness with:

```shell
dotnet run --project tests/FunnySharp.FusedExecutionMeasurements/FunnySharp.FusedExecutionMeasurements.csproj -c Release
```

Inspect generated code by building the functional test or measurement project with
`-p:EmitCompilerGeneratedFiles=true -p:CompilerGeneratedFilesOutputPath=<path>`.

## Generation And Build Cost

- Generator execution over the representative 4/4 sample: 4.24–4.26 ms min / 4.38–4.78 ms mean
  across the two runs (10 measured runs after 2 warmups). The minimal 2/2 sample runs 2.74–2.75 ms
  min / 2.86–2.90 ms mean. Each sample produces 3 files with 0 diagnostics.
- Clean build A/B on a minimal consumer with the generator versus the same source with the
  generated files committed and the generator removed: with-generator runs 1.81 s, 1.68 s, 2.15 s;
  pre-generated runs 2.13 s, 1.89 s, 1.96 s. The difference is below run-to-run noise; the
  measured generator execution is a few milliseconds, so no separate build-time penalty is
  claimed. Warm builds were also within noise (1.3–2.8 s).
- Generated code is visible in the standard compiler-generated file location. There is no source
  mapping (`#line`) back to the summary field, so a debugger steps through the generated file.
  A shipping form would need to decide whether that is acceptable or add mapping.
- Diagnostics are compile-time errors with stable ASCII IDs. Unsupported constructs fail the build
  rather than producing a silently different runtime path.

## Span, Async, And AOT Truthfulness

- The prototype supports only synchronous `IEnumerable<T>` and `JsonElement` forms. `Span<T>`,
  `Memory<T>`, and `IAsyncEnumerable<T>` are unsupported and diagnosed. Synchronous
  `IEnumerable<T>` is the supported streaming form. No destination-capacity, partial-write,
  cancellation, or async lifetime claim is made for generated code.
- No shipping assembly changed, so no trim or Native AOT claim is made. The generated code uses
  only statically referenced BCL and FunnySharp public types, no reflection, no `dynamic`, and no
  runtime code generation, so the supported summary form is trim/AOT-friendly in principle. We did
  not run a trim or Native AOT publish because no shipping package or consumer claim exists;
  package-level trimming/AOT evidence remains a condition for any later product goal, not a result
  of this research prototype.

## Maintainer Judgment

**ADAPT (summary-only).** The generated summary removes FunnySharp's own delegate/state
composition tax: on the recorded workload it matches a hand-written loop and is about 2.1x faster
than the delegated `Summary.Zip` (240 → 32 B/op). It is not a general performance win: LINQ's
one-pass `Aggregate` remains allocation-free on this array-backed input and stays close in time,
with the generated path roughly 13–16% faster in the recorded runs while allocating 32 B vs 0 B.
The technique is additive and opt-in: the existing `Summary` description stays the source of
truth, the generated method is a new callable path, and unsupported shapes fail the build. The
one-time description allocation and the duplicated semantic logic in the generator are the visible
costs.

The decoder path is **deferred**, not adopted. It improves valid inputs but regresses invalid
inputs, and its safe subset excludes nested objects and lists of objects. Adopting it would trade a
success-path win for a failure-path tax and a narrower representative shape. On valid input the
generated path now reproduces the runtime read-only list shape, which costs one wrapper allocation
relative to the hand-written valid-only parse.

A blanket **DEFER** is also defensible: the measured summary workload is a synthetic 1,024-item
loop, the generated path matches a hand-written loop rather than beating it, and the generator
duplicates semantic logic. The recommendation is ADAPT rather than DEFER because the generated
path removes a measured composition tax without changing the consumer's description, while
packaging and product adoption remain a separate, explicit decision with the conditions below.

Non-adoptions:

- No decoder or `Contract<T>` generation in this outcome.
- No universal expression compiler, runtime code generation, custom language, or rewriting of
  arbitrary call sites.
- No purity/associativity inference from arbitrary delegates; only the explicit static subset is
  analyzed.
- No async/span/memory generated form.
- No Hyperlinq or other competitor dependency; no wholesale LINQ replacement.
- No public package, API baseline, grammar, product contract, or stability-inventory change.

Conditions for a later product goal:

1. Choose packaging: analyzer inside the core package versus a separate optional package; keep the
   core runtime dependency set empty.
2. Freeze the supported subset and diagnostics in a public contract; add source mapping or an
   explicit debugging note.
3. Decide whether the runtime `Summary` field's one-time allocation is acceptable, or whether a
   description form that does not require a runtime object is worth a new surface.
4. Run package-level trim/Native AOT and multi-OS build evidence for the chosen packaging.
5. Revisit decoder fusion only if a real consumer profile justifies a valid-only fast path or a
   design can generate exact error paths without a fallback.

## Residual Risks And Open Questions

- The measured summary win is a synthetic 1,024-item loop; real applications may be dominated by
  I/O or other work. The absolute saving is about 30–31 µs and 208 B per 1,024-item evaluation on
  this host.
- The generator duplicates `Summary` semantics. A future change to `Sum`, extrema, or checked
  arithmetic must update both implementations. The parity suite covers the recorded fixtures and
  supported shapes; it is not a proof of universal equivalence.
- Selector bodies are recompiled inside generated static local functions. Evaluation count and
  order are preserved by the parity suite, but a future change to selector evaluation order would
  require a generator update.
- The runtime field is dead weight when only the generated path is used. The measured 1,760 B is
  charged when the type's static constructor runs; a caller that never touches the type need not
  pay it, so "once per type" is an upper bound for used types.
- Generated code has no `#line` mapping; debugging shows the generated file.
- The generator has only been exercised on the recorded host and SDK; cross-SDK Roslyn behavior
  and multi-OS build cost remain open.

## Gate Evidence

Final gate results for this working tree:

- `build`: 0 warnings, 0 errors.
- `test`: 2,881 passed, 0 failed, 0 skipped across all test projects in `FunnySharp.slnx`; three
  consecutive runs produced the same total.
- `format`: clean; only the F# harness projects are not format-checked by the tool.
- `verify-docs-snippets`: 81 snippets across 15 primary guides; unchanged because no guide was
  added.
- `verify-api-baseline`: 115 types and 989 members across 2 assemblies, 0 delta; no shipping API
  changed.
- Examples executable: `FunnySharp examples passed.`
- Measurement harness: two complete Release runs; both verified 129 parity checks and printed the
  tables above.
