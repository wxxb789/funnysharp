---
title: Composable decoding into trusted domain values
type: feat
date: 2026-10-30
artifact_contract: ce-unified-plan/v1
product_contract_source: docs/goals/archive/0026-goal.md, docs/product-contract.md, docs/grammar.md
execution: code
---

# Composable Decoding Plan

## Goal Capsule

- **Objective:** Deliver a callable decoder capability that turns a nested BCL `JsonElement` into caller-defined domain values, accumulates independent field and element errors with deterministic locations, skips dependent steps after prerequisite failure, and makes missing, explicit-null, malformed, and empty input deliberately distinguishable.
- **Means:** A stable `Decoder<T>` function wrapper over `Validation<T, DecodeError>`; `DecodeError` exposes a stable rendered `Path` and an experimental structured `Location`; primitives, field, object, list, optional, dependent, and domain-validation combinators; a `JsonElement` input boundary; async only for an actual asynchronous domain-validation callback.
- **Authority:** [Goal 0026](../goals/archive/0026-goal.md) is the completion contract. [Product contract](../product-contract.md) and [grammar](../grammar.md) are the current product boundaries; [stability inventory](../stability-inventory.md) owns the stable/experimental split. Goal 0027 owns inspectable contract derivation and is out of scope.
- **Execution profile:** Implement in `src/FunnySharp`, test in `tests/FunnySharp.Tests`, exercise through `examples/FunnySharp.Examples`, and record comparison/measurement evidence in `docs/decoder-evidence.md`.
- **Tail ownership:** The maintainer judges expression and diagnosis improvement from the comparison evidence. A negative or partially positive judgment returns to the affected unit; it does not silently narrow the contract.
- **Stop conditions:** Stop for a new decision if the maintainer rejects the stable/experimental split, if `JsonElement` is rejected as the BCL input, if a required async form would force a second decoder model, or if Location cannot represent a required path without changing the existing located-traversal contract.

## Product Contract

**Included**

- `Decoder<T>` decodes one `System.Text.Json.JsonElement` into `Validation<T, DecodeError>`.
- `DecodeErrorKind` distinguishes `Missing`, `Null`, `WrongKind`, `Malformed`, `Empty`, and `InvalidValue`.
- `DecodeError.Path` is a stable rendered path such as `customer.addresses[1].postalCode`; `DecodeError.Location` is the same path as the existing experimental `Location` (`FS0017`).
- Independent field and element decoding accumulates every reached error in decoder order (fields), source order (elements), and inner error order.
- `Object` combines 2-4 independently decoded fields and invokes its combine function only when every operand is valid.
- `Bind` sequences a decoder-producing function only after the prerequisite decode succeeds.
- `Ensure` is one dependent predicate; `Validate` applies an accumulating domain validation and maps its messages to `InvalidValue` at the decoder's location.
- `Field` is required (missing is `Missing`, explicit null reaches the inner decoder and is normally `Null`); `OptionalField` maps missing to `None` but reports explicit null; `NullableField` deliberately maps both missing and null to `None`; `OptionalValue` maps an explicit null to `None`.
- `List` accepts an empty array; `NonEmptyList` reports `Empty` for it. `String` accepts an empty string; `NonEmptyString` reports `Empty` for it.
- Async means an actual asynchronous domain-validation callback: `DecodeAsync` (Task) and `DecodeValueAsync` (ValueTask) decode synchronously, then await the callback only on success and forward cancellation exactly. No async input overload is added for an in-memory `JsonElement`.
- Decoders are immutable, reusable, and hold no per-invocation state. They never catch callback, parse-boundary, cancellation, or programming exceptions to manufacture `DecodeError`.
- A comparison consumer implements one nested order request three ways: the decoder, ordinary C#, and direct existing `Validation`. All three produce the same domain values and the same mapped `OrderError` lists on the discriminating fixtures.
- Measurements disclose managed allocation and directional execution cost for the three complete consumer versions and for `Location` path construction at several depths.

**Deferred to Follow-Up Work**

- Schema or constraint introspection, generation, explanations, and serializer integration belong to [Goal 0027](../goals/archive/0027-goal.md).
- Typed caller error hierarchies composed across modules belong to [Goal 0030](../goals/0030-goal.md).
- ASP.NET Core endpoint mapping of decode errors belongs to [Goal 0031](../goals/0031-goal.md).
- Partial-change omission/null/replacement semantics belong to [Goal 0028](../goals/0028-goal.md).

**Outside this product's identity**

- A new serializer, a JSON DOM replacement, reflection-based converters, a general effect runtime, an async obstacle wrapper, or a second absence carrier.

## Planning Contract

### Requirements

- **R1:** Decode a nested order request with independently invalid fields and list elements into a caller-defined domain object.
- **R2:** Return deterministic `DecodeError` values with field and element paths; the path string is stable and does not require the caller to assemble it.
- **R3:** Accumulate independent errors across an object's fields and across list elements; preserve decoder field order, element source order, and each inner validation's order.
- **R4:** Do not run a step that requires a successfully decoded value when its prerequisite failed (`Object` combine, `Bind` continuation, `Ensure` predicate, `Validate` callback).
- **R5:** Make optional, missing, explicit-null, malformed, and empty input deliberately distinguishable, with documented combinators for each policy.
- **R6:** Reuse one decoder instance across inputs and concurrently without invocation-state leakage.
- **R7:** Never translate cancellation, callback faults, disposed-document faults, or other programming exceptions into ordinary invalid input.
- **R8:** Support async only for actual asynchronous validation; forward the exact `CancellationToken`; suppress the callback when decoding already failed.
- **R9:** Build on the shipped `Validation` and located `Traverse`/`Location` semantics rather than introducing a second location or error carrier.
- **R10:** Record whether the experimental `Location` surface honestly supports decoder diagnostics, with allocation/copying evidence; keep the structured accessor experimental unless promotion is explicitly decided.
- **R11:** Provide equivalent ordinary-C# and direct-Validation consumer versions including construction, adapters, and error mapping.
- **R12:** Reconcile product contract, grammar, stability inventory, API baseline, and the consumer comparison documentation within this goal.

### Key Technical Decisions

- **KTD1 (governs R1-R5):** A decoder is an immutable `sealed class Decoder<T>` holding `Func<JsonElement, Validation<T, DecodeError>>`. The public surface is a function plus combinators, not an inspectable schema.
- **KTD2 (governs R2, R9, R10):** Errors start relative to the current input. `Field` and `List` compose prefixes with the existing `Location` (`Property`, `At`, and `Nest` semantics) only after an inner decode reports errors, so a successful decode allocates no `Location` nodes. `DecodeError.Path` renders the final location; `DecodeError.Location` is `[Experimental("FS0017")]`. A path of depth N still allocates one `Location` per composition step and copies the previous segment array on the error path, so costs are measured and disclosed rather than claimed allocation-free.
- **KTD3 (governs R3, R4):** `Object` and `List` use existing `Validation` applicative composition (`Zip`, located `Traverse`), so independent errors accumulate. `Bind`, `Ensure`, and `Validate` inspect first and skip their callback on failure.
- **KTD4 (governs R5):** Required `Field` reports `Missing`; explicit null reaches the inner decoder, where primitives and structural decoders report `Null`. `OptionalField` maps only missing to `None`; `NullableField` deliberately maps both; `OptionalValue` maps null to `None`. Empty policies are named primitives (`NonEmptyString`, `NonEmptyList`).
- **KTD5 (governs R6, R7):** Decoders are immutable delegates over local invocation state. No `try/catch` converts exceptions; kind checks precede every `JsonElement` accessor that throws on a mismatched kind.
- **KTD6 (governs R8):** `DecodeAsync`/`DecodeValueAsync` are decode-then-validate boundaries, not asynchronous input models. They accept a validation callback returning `Validation<T, string>`, map messages to `InvalidValue` at the root, await once, and forward cancellation.
- **KTD7 (governs R10, R11):** The comparison maps `DecodeError` to a caller-defined `OrderError` through `Validation.MapErrors`; ordinary C# and direct Validation build the same `OrderError` lists manually. The measurement harness counts the complete mapped operation, not only the happy path.
- **KTD8 (governs R12):** Stable surface: `Decoder`, `Decoder<T>`, `DecodeError`, `DecodeErrorKind`. Experimental surface: only `DecodeError.Location` (`FS0017`). `Decoder<T>` is not a semantic outcome carrier and does not change the four-carrier vocabulary.
- **KTD9 (governs R1, R2):** Duplicate property names resolve to the first occurrence in document order. That is the intended policy for the `JsonElement` input boundary, and it deliberately differs from `JsonElement.TryGetProperty`, which resolves the last duplicate. The decoder does not merge, reject, or prefer last-match duplicates; `docs/decoding.md` records the difference so consumers are not surprised.

### Acceptance Examples

- **AE1:** A valid nested order fixture returns `Valid(Order)` with exact domain values and no error allocation beyond the returned validation and domain objects.
- **AE2:** A fixture with an invalid customer email, a null postal code in `customer.addresses[1]`, a zero quantity in `lines[1]`, and an empty SKU in `lines[2]` returns exactly those four `DecodeError`s in decoder/element order with the expected paths.
- **AE3:** A missing required field returns `Missing`; the same field present as null returns `Null`; a string where a number is required returns `WrongKind`; a malformed GUID string returns `Malformed`; an empty string/array under a non-empty combinator returns `Empty`.
- **AE4:** A `Bind` continuation, an `Object` combine, an `Ensure` predicate, and a `Validate` callback each record zero invocations after their prerequisite decode fails.
- **AE5:** The same decoder instance decodes valid and invalid inputs concurrently; each result has only its own paths/errors.
- **AE6:** A throwing mapper, a throwing validation callback, a cancelled async validation, and a disposed `JsonDocument` propagate their original exception; none become `DecodeError`.
- **AE7:** The decoder, ordinary-C#, and direct-Validation consumers agree on success values and mapped `OrderError` lists for every discriminating fixture.
- **AE8:** The measurement report exposes allocation per operation for all three versions and superlinear `Location` path-construction bytes/copies by depth.

## Implementation Units

### U1. Add DecodeError and Location-backed diagnostics

- **Goal:** Add `DecodeErrorKind`, `DecodeError`, the stable `Path`, and the experimental `Location` accessor.
- **Requirements:** R2, R5, R9, R10.
- **Dependencies:** None.
- **Files:** `src/FunnySharp/DecodeError.cs`, `src/FunnySharp/DecodeErrorKind.cs`, `docs/stability-inventory.md`.
- **Approach:** Internal constructor owns an absolute `Location`; public `Create` produces a relative fault for `Decoder.From`; `Path` renders lazily through `Location.ToString`; equality includes location, kind, expected, and detail; `ToString` uses `input` for the root.
- **Execution note:** `DecodeError` is stable; only its `Location` property carries `[Experimental("FS0017")]`. Suppress FS0017 inside implementation files that construct absolute errors.
- **Patterns to follow:** `Validation` XML-doc density, value equality, deterministic ordering, and explicit argument checks.
- **Test scenarios:** Path rendering, equality/hash, relative vs absolute creation, root subject, enum rejection, experimental attribute discovery. `(happy path)`, `(edge)`, `(error path)`.
- **Verification:** `dotnet build FunnySharp.slnx`; decoder unit tests.

### U2. Compose the core decoder and combinators

- **Goal:** Add `Decoder<T>` and `Decoder` with primitives, `From`, `Map`, `Bind`, `Ensure`, `Validate`, `Field`, optional policies, `Object`, `List`, and `NonEmptyList`.
- **Requirements:** R1-R7, R9.
- **Dependencies:** U1.
- **Files:** `src/FunnySharp/Decoder.cs`, `src/FunnySharp/Decoder` primitives as one file family or partial files named by primary type.
- **Approach:** `Object` checks the object kind once and returns one object-kind error before running any field decoder; `Field` and the optional variants check the kind themselves when they are the entry point. `Object` uses `Validation.Zip` arities 2-4. `List` uses `Validation.Traverse`; `Field`/`List` prefix inner errors with `Location` only after failure, preserving located-traversal composition without happy-path location allocations. `Bind`/`Ensure`/`Validate` skip callbacks on failure. No catch blocks.
- **Execution note:** Keep the one-type-family-per-file convention; if the file becomes unwieldy, split `DecoderPrimitives.cs` only for named primitive properties, not for public types.
- **Patterns to follow:** `Result`/`Validation` XML docs; `ArgumentNullException` at entry; selector exceptions propagate; `Option.Some` rejects runtime null.
- **Test scenarios:** Object field accumulation/order, list element accumulation/order, nested paths, missing/null/wrong-kind/malformed/empty, optional policies, dependent-step skip counts, reuse/concurrency, exception propagation. `(happy path)`, `(edge)`, `(error path)`, `(integration)`.
- **Verification:** targeted `dotnet test` for decoder tests; full `dotnet build`.

### U3. Add the async validation boundary

- **Goal:** Add `DecodeAsync` and `DecodeValueAsync` for one actual asynchronous domain-validation callback.
- **Requirements:** R7, R8.
- **Dependencies:** U2.
- **Files:** `src/FunnySharp/Decoder.cs` (async methods).
- **Approach:** Decode synchronously; on failure return the existing invalid validation without invoking the callback or inspecting the token; on success invoke once, await once, map string messages to root `InvalidValue` errors, and return the callback's valid value. Do not catch OCE or faults.
- **Execution note:** No async input overload and no Task-to-ValueTask conversion internally; provide both callback shapes to match the existing `Validation.MapAsync`/`MapValueAsync` convention.
- **Patterns to follow:** `ValidationExtensions` async short-circuit and exact-token forwarding tests.
- **Test scenarios:** valid async validation, multi-error async validation, invalid decode skips callback, exact token forwarding, cancellation propagates, fault propagates, callback invoked once. `(happy path)`, `(edge)`, `(error path)`.
- **Verification:** async decoder tests; `dotnet build` with analyzer warnings as errors.

### U4. Pin decoder behavior with a test suite

- **Goal:** Pin every boundary and composition law with xUnit v3 tests.
- **Requirements:** R1-R10.
- **Dependencies:** U1-U3.
- **Files:** `tests/FunnySharp.Tests/DecoderTests.cs`, `DecoderBoundaryTests.cs`, `DecoderAsyncTests.cs`, `DecoderLocationTests.cs`, plus shared helpers if needed.
- **Approach:** Attribute-only `[Fact]`/`[Theory]`; classic asserts; no skips; use `ProbeEnumerable` only if a decoder enumerates a source (it should not). Use deterministic JSON fixtures. For async cancellation, use a pre-cancelled token or a `TaskCompletionSource` signal, never a sleep.
- **Execution note:** Add `#pragma warning disable FS0017` only in the file that reads `DecodeError.Location`; stable-path tests do not need it.
- **Patterns to follow:** `tests/FunnySharp.Tests/AGENTS.md` naming and classic asserts.
- **Test scenarios:** AE1-AE6; error order; path segments; reuse; concurrent decode; disposed document; throwing map/bind/ensure/validate; optional matrix. `(happy path)`, `(edge)`, `(error path)`, `(integration)`.
- **Verification:** `dotnet test tests/FunnySharp.Tests/FunnySharp.Tests.csproj`; no skipped tests.

### U5. Build the consumer comparison and publish measurements

- **Goal:** Build the nested order consumer three ways, prove equivalence, and publish allocation/execution evidence.
- **Requirements:** R11, R10.
- **Dependencies:** U1-U4.
- **Files:** `tests/Shared/DecoderComparison/*.cs`, Compile-included by `tests/FunnySharp.Tests` and `tests/FunnySharp.DecoderMeasurements`; `tests/FunnySharp.DecoderMeasurements/Program.cs`; `docs/decoder-evidence.md`.
- **Approach:** Shared domain records (`Order`, `OrderId`, `Customer`, `EmailAddress`, `Address`, `OrderLine`, `OrderError`), fixtures, decoder consumer, ordinary-C# consumer, direct-Validation consumer, and equivalence checks. The shared consumers are Compile-included only by `tests/FunnySharp.Tests` and `tests/FunnySharp.DecoderMeasurements`; the measurement project is a normal, non-packable solution member, so the build gate compiles it and the format gate checks it. Measure complete mapped operations and `Location` depths with `GC.GetAllocatedBytesForCurrentThread`; report directional `Stopwatch` timing separately.
- **Execution note:** Measurements are explicit-request evidence, not a CI gate. The measurement executable runs the three-way equivalence check first, then reports allocation, copying, and directional timing. The default examples run covers decoder usage and must keep the exact `FunnySharp examples passed.` marker.
- **Patterns to follow:** `tests/Shared/` Compile-include convention; direct fixture assertions; no sleeps and no timing assertions.
- **Test scenarios:** AE1-AE3, AE7, AE8; valid/invalid/missing/null/malformed/empty fixtures; all three versions agree; measurement output captured in the evidence doc. `(happy path)`, `(edge)`, `(error path)`, `(integration)`.
- **Verification:** `dotnet run --project examples/FunnySharp.Examples -c Release`; `dotnet run --project tests/FunnySharp.DecoderMeasurements/FunnySharp.DecoderMeasurements.csproj -c Release`; equivalence tests.

### U6. Reconcile the contract, guide, and baseline

- **Goal:** Ship the capability with authoritative docs and a committed API baseline.
- **Requirements:** R10-R12.
- **Dependencies:** U1-U5.
- **Files:** `docs/decoding.md`, `docs/decoder-evidence.md`, `docs/grammar.md`, `docs/product-contract.md`, `docs/stability-inventory.md`, `docs/AGENTS.md`, `docs/plans/AGENTS.md`, `eng/api-baseline/FunnySharp.public-api.txt`, `README.md` if the guide index needs it, `src/FunnySharp/AGENTS.md`, `tests/FunnySharp.Tests/AGENTS.md`.
- **Approach:** Stable/experimental split documented; `JsonElement` input boundary; no schema/serializer promise; allocation/copying disclosure; comparison conclusion and maintainer-judgment request.
- **Execution note:** Regenerate the API baseline only after the surface is final; update the plan index and guide index in the same pass.
- **Patterns to follow:** Guide body shape `## API Shape` then semantics then `## Deliberate Boundaries`; lowercase-hyphen names; no restating the whole goal contract in other files.
- **Test scenarios:** API baseline gate, docs snippet gate (no new byte-exact snippets unless mirrored), example run, broken-link check if available. `(integration)`.
- **Verification:** `verify-api-baseline`, `verify-docs-snippets`, example run, `dotnet build`.

## Verification Contract

| Gate | Command | Applies to | Completion evidence |
| --- | --- | --- | --- |
| Build | `dotnet build FunnySharp.slnx` | U1-U6 | exit 0, zero warnings/errors |
| Decoder tests | `dotnet test tests/FunnySharp.Tests/FunnySharp.Tests.csproj` | U1-U5 | all passed, zero skipped |
| Example consumer | `dotnet run --project examples/FunnySharp.Examples -c Release` | U5 | exact `FunnySharp examples passed.` |
| Measurements | `dotnet run --project tests/FunnySharp.DecoderMeasurements/FunnySharp.DecoderMeasurements.csproj -c Release` | U5, U6 | captured table with allocation/copying/timing |
| API baseline | `dotnet fsi build.fsx -- -p verify-api-baseline` | U6 | exit 0 after `--write` |
| Docs snippets | `dotnet fsi build.fsx -- -p verify-docs-snippets` | U6 | exit 0 |
| Format | `dotnet fsi build.fsx -- -p format` | U1-U6 | exit 0 |
| Contract audit | Read goal 0026 against evidence | U1-U6 | every clause mapped to an artifact; maintainer judgment recorded |

## Definition of Done

### Per-Unit Completion

| Unit | Done when |
| --- | --- |
| U1 | Path rendering, equality and hashing, relative and absolute creation, the root subject, enum rejection, and experimental-attribute discovery pass, and the solution build is warning-free. |
| U2 | Field and element accumulation order, nested paths, missing/null/wrong-kind/malformed/empty, optional policies, dependent-step skip counts, reuse and concurrency, and exception propagation pass against the core decoder. |
| U3 | Valid, multi-error, skipped-callback, exact-token, cancellation, and fault paths pass for both `DecodeAsync` and `DecodeValueAsync`, with the callback invoked at most once. |
| U4 | AE1-AE6 pass in `tests/FunnySharp.Tests` with zero skipped tests, including error order, path segments, reuse, concurrent decode, disposed documents, throwing callbacks, and the optional matrix. |
| U5 | The three consumer versions agree on every discriminating fixture (AE7), and the measurement project reports allocation, copying, and directional timing for all three versions and for `Location` depths (AE8). |
| U6 | The API baseline, docs snippets, guide, and contract audit pass, and every goal clause maps to an artifact with the maintainer judgment recorded. |

### Global Completion

- All R1-R12 have a named implementation artifact and an executed verification result.
- AE1-AE8 pass in the test suite or the evidence run; the three consumer versions agree on every fixture.
- `DecodeError.Path` is deterministic and documented; `DecodeError.Location` is the only new experimental member and is tracked in the stability inventory.
- No serializer, schema, reflection, effect runtime, or second absence carrier is introduced.
- The evidence document states the measured Location allocation/copying behavior and does not claim allocation-free paths.
- The maintainer has an explicit comparison and is asked to judge expression and diagnosis improvement; a negative judgment leaves the goal active.
