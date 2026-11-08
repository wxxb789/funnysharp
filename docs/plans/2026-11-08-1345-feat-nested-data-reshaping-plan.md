---
title: Typed nested data reshaping and provenance
type: feat
date: 2026-11-08
artifact_contract: ce-unified-plan/v1
product_contract_source: docs/goals/archive/0036-goal.md, docs/product-contract.md, docs/grammar.md
execution: code
---

# Nested Data Reshaping Plan

## Goal Capsule

- **Objective:** Let a consumer turn a decoded nested record set into ordinary typed rows with an
  explicit relationship between each row and its source record and nested element. A realistic
  import must be able to state what happens for a missing field, an explicit null, an empty child
  collection, unequal parallel child lengths, and a caller-rejected element; failures must identify
  the record and element without the application assembling path strings.
- **Means:** A small sequence-expansion family over typed records. `Expand` yields one row per
  nested child, `ExpandAligned` pairs two parallel child sequences position by position and rejects
  unequal lengths, and `ExpandCartesian` is the separately named explicit product. Each combine
  callback receives a small `RowOrigin` (or `CartesianRowOrigin`) value carrying the record and
  child indices, so callers who only need failure locations ignore it and callers who need a
  per-row trace store it. Failures are structured `NestedRowError` values carrying the record
  index, the child sequence, and both lengths where they differ. Synchronous and asynchronous
  parent sources share the same buffered, all-or-nothing contract. Existing `Decoder`/
  `DecodeError` composition extracts the typed fields and supplies the JSON property paths.
- **Authority:** [Goal 0036](../goals/archive/0036-goal.md) is the completion contract.
  [Product contract](../product-contract.md), [grammar](../grammar.md), and
  [core design](../core-design.md) are the current boundaries. Goal 0035's indexes and joins are
  reused only when a relation is genuinely keyed: expansion is a positional/nested-shape
  operation and does not build a lookup index. Goal 0037 remains out of scope.
- **Execution profile:** Implement in `src/FunnySharp/RowOrigin.cs`,
  `CartesianRowOrigin.cs`, `NestedRowError.cs`, `NestedRowErrorKind.cs`, and
  `NestedDataExtensions.cs`; pin behavior in `tests/FunnySharp.Tests`; share reference consumers,
  fixtures, and the comparison in `tests/Shared/NestedDataComparison`; exercise through
  `examples/FunnySharp.Examples/NestedDataExamples.cs` and the documentation samples; measure
  through `tests/FunnySharp.NestedDataMeasurements`; record evidence in
  `docs/nested-data-reshaping-evidence.md`.
- **Tail ownership:** The maintainer judges whether the expansion family makes a complete nested
  import materially smaller and more truthful. The evidence records a judgment even if it is
  sceptical, for example when a plain `SelectMany` plus `JsonSerializer` is shorter because no
  location, alignment, or provenance guarantee is needed.
- **Stop conditions:** Stop for a new decision if the design would need a second outcome carrier,
  a `JsonElement` or `Span`-retaining result, an R-style dynamic evaluation model, a serializer or
  DataFrame engine, implicit lossy coercion, a hidden boolean/enum that changes aligned versus
  Cartesian semantics, a general schema/reflection binder, or a keyed relation that belongs to
  Goal 0035.

## Product Contract

**Included**

- `RowOrigin`: a small readonly value carrying the zero-based `RecordIndex` and `ElementIndex` of
  the parent record and nested child that produced a row. Deterministic `ToString` renders
  `record[i].element[j]`.
- `CartesianRowOrigin`: a small readonly value carrying `RecordIndex`, `FirstElementIndex`, and
  `SecondElementIndex`. Deterministic `ToString` renders `record[i].elements[a,b]`.
- `NestedRowErrorKind`: `NullChildSequence`, `NullFirstChildSequence`, `NullSecondChildSequence`,
  and `ChildCountMismatch`.
- `NestedRowError`: a readonly diagnostic value with `Kind`, `RecordIndex`, and nullable
  `FirstElementCount`/`SecondElementCount` for a mismatch. Factories validate their arguments;
  equal counts cannot construct a mismatch. Deterministic `ToString` renders the record position
  and, for a mismatch, both counts. It is a diagnostic, not an outcome carrier.
- `NestedDataExtensions`:
  - `Expand(records, children, combine)` over `IEnumerable<TParent>`: one row per child of each
    record, in record order then child order, retaining the parent value for the combine callback.
  - `ExpandAligned(records, firstChildren, secondChildren, combine)`: for each record, require
    `firstChildren.Count == secondChildren.Count`; yield one row per index, pairing the two child
    sequences positionally. A mismatch is a `ChildCountMismatch` failure for that record.
  - `ExpandCartesian(records, firstChildren, secondChildren, combine)`: for each record, yield the
    explicit `first × second` product in first-index then second-index order. It is separately
    named; `ExpandAligned` never produces a Cartesian product.
  - `ExpandAsync`, `ExpandAlignedAsync`, and `ExpandCartesianAsync` over
    `IAsyncEnumerable<TParent>` with the same semantics, returning
    `ValueTask<Validation<...>>` and forwarding the caller token to the asynchronous enumerator.
- All six operations return `Validation<IReadOnlyList<TResult>, NestedRowError>`. Success is an
  exact-size array exposed as `IReadOnlyList<TResult>`; failure returns no rows and every
  structural error in record order.
- Child selectors return `IReadOnlyList<TChild>`. The list is read during the call and is not
  cloned, snapshotted, or cached.
- A selector result of runtime null is a structured failure, not empty and not an exception.
  Empty is an ordinary collection and yields zero rows for that record.
- `combine` is invoked once per output row in the documented order. Once a structural error has
  been recorded, later records are still inspected for null and length failures, but `combine` is
  not invoked; earlier invocations are not rolled back. The output list is discarded on failure.
- `Decoder`, `DecodeError`, and `Decoder.List` remain the field-extraction boundary. The family
  does not duplicate decoding, serializer behavior, or the stable `DecodeError.Path` contract.
- A complete comparison covers the same import through idiomatic `JsonSerializer` + LINQ, a
  hand-written `JsonDocument`/loop consumer with manual locations, and the proposed
  decoder/expansion consumer; it asserts fixed expected rows, rejections, and failure positions,
  plus source enumeration counts. A measurement executable reports import, expansion, provenance,
  and location-storage allocations and directional timing.
- Documentation states output shape, ordering, cardinality, enumeration, buffering, and
  partial-output behavior for every operation, plus the lifetime limits of `JsonElement` and span
  views.

**Deferred to Follow-Up Work**

- A streaming per-row expansion shape (`IAsyncEnumerable` or `IEnumerable<Result<...>>`): the goal
  does not demand partial output, and the buffered `Validation` shape makes the all-or-nothing
  guarantee easy to state and test. A streaming shape returns only with a consumer that needs
  partial rows and a contract for later failures.
- Three or four parallel child sequences: two sequences establish the aligned-versus-Cartesian
  distinction; wider shapes compose by forming an intermediate caller record or by applying
  ordinary `ZipExact` at the call site. No unbounded arity tower is added.
- Span/`ReadOnlySpan` expansion and zero-copy child views: a span cannot cross an asynchronous
  source, and a borrowed view cannot outlive the document or list. The typed list is the lifetime
  boundary.
- Asynchronous `combine` or child selectors: asynchronous work belongs to input acquisition.
  Decode or fetch first, then reshape typed values synchronously.
- A row-level rejection operator: rejection remains caller policy. The library is total and
  supplies the origin; a consumer that rejects a row records the rejection itself with the origin.
  This keeps rejection distinct from absence and failure.
- A JSON-aware or schema-aware expansion API: no serializer, reflection binder, schema registry,
  DataFrame engine, or R-style evaluation is added.
- A keyed nested expansion: keyed association belongs to Goal 0035's `LookupIndex`/
  `JoinExactlyOne` family; expansion is positional and never infers a relation from values.

**Outside this product's identity**

- A second outcome carrier, a custom collection universe, pervasive immutability, automatic lossy
  coercion, a serializer, a DataFrame engine, dynamic evaluation, or a database-level integrity
  claim.

## Planning Contract

### Requirements

- **R1:** Add the public `RowOrigin`, `CartesianRowOrigin`, `NestedRowErrorKind`,
  `NestedRowError`, and `NestedDataExtensions` surface with complete XML documentation.
- **R2:** `Expand` enumerates the record source exactly once, invokes the child selector once per
  record, and yields one row per child in record order then child order, passing the parent value,
  the child value, and a `RowOrigin` to `combine`.
- **R3:** `ExpandAligned` invokes both child selectors once per record and requires equal lengths.
  It yields one row per index in increasing index order on success; a mismatch yields one
  `ChildCountMismatch` for that record with both counts and no rows for that record.
- **R4:** `ExpandCartesian` invokes both child selectors once per record and yields the explicit
  product in record order, first-index ascending, second-index ascending. It never reports a
  length mismatch; unequal lengths are valid.
- **R5:** The aligned and Cartesian operations are separate names and separate semantics. No
  boolean, enum, or mode parameter selects between them, and `ExpandAligned` never produces more
  than one row per child index.
- **R6:** A runtime-null child list is a `NullChildSequence`, `NullFirstChildSequence`, or
  `NullSecondChildSequence` failure with the record index. Null is not treated as empty. Empty
  child collections yield zero rows and no error; two empty aligned lists are equal.
- **R7:** On any structural failure the operation returns no rows plus every reached structural
  error. Errors are ordered by record index; within a record, first-sequence errors precede
  second-sequence errors, and a mismatch is one error. `combine` is not invoked for a failing
  record, and it is not invoked for later records after the first recorded error. Earlier output
  and combine calls are discarded, not rolled back.
- **R8:** A successful result is an exact-size array exposed as `IReadOnlyList<TResult>`; the
  success count is the sum of expanded elements. Ordering and cardinality are deterministic.
- **R9:** `ExpandAsync`, `ExpandAlignedAsync`, and `ExpandCartesianAsync` have the same semantics
  over `IAsyncEnumerable<TParent>`, enumerate the source once, perform no blocking, forward the
  exact token to `GetAsyncEnumerator`, and propagate cancellation and exceptions unchanged.
- **R10:** All arguments are validated eagerly. Source, selector, combine, enumerator, and disposal
  exceptions keep ordinary .NET identity and are never converted into `NestedRowError` values.
- **R11:** `RowOrigin` and `CartesianRowOrigin` are value-equal readonly structs with no heap
  allocation. `NestedRowError` is a value-equal readonly struct whose string rendering allocates
  only when requested. No `Location` object is created per successful row.
- **R12:** `NestedRowError` factories validate non-negative indices/counts and reject a
  `ChildCountMismatch` whose counts are equal.
- **R13:** The typed field-extraction path reuses `Decoder.Field`, `OptionalField`,
  `NullableField`, `List`, and `DecodeError.Path`. No new decoder, serializer, schema, or
  JSON-specific row type is added.
- **R14:** Focused tests cover empty records, empty children, one/many children, order and count,
  aligned success, aligned mismatch with both counts, Cartesian product versus aligned pairing,
  null typed lists, non-null selectors, combine ledgers, single enumeration, async parity,
  cancellation, disposal, exceptions, and argument nullness.
- **R15:** A shared three-consumer comparison fixes expected rows, rejections, and failure
  locations; asserts source enumeration counts and combine ledgers; compares the ordinary form on
  success and its silent truncation/data loss on the failure fixtures; and asserts hand-written
  and FunnySharp parity.
- **R16:** A measurement executable reports allocation and directional timing for the full import,
  expansion shapes, provenance storage, and location storage. Allocation must reproduce across two
  runs.
- **R17:** Ship a compiling example, a guide closing with `## Deliberate Boundaries`, byte-exact
  documentation samples, an evidence record, API-baseline and performance-manifest updates, and
  reconciled product/grammar/stability/index/goal documents.
- **R18:** Add no new carrier, runtime dependency, serializer, reflection binder, DataFrame engine,
  automatic Cartesian inference, async row callback, span-retaining result, or Goal 0037
  behavior.

### Key Technical Decisions

- **KTD1 (governs R1, R18):** The expansion family returns canonical `Validation` and plain
  caller rows. `RowOrigin`/`CartesianRowOrigin` are provenance values and `NestedRowError` is a
  diagnostic; neither is a new outcome channel.
- **KTD2 (governs R2-R4):** Operations are row-shape descriptions, not keyed associations. A
  record order is the outer order and child order is the inner order. Aligned pairing is a
  position-wise relation; Cartesian is a separately named nested product.
- **KTD3 (governs R5, R7):** The relation is in the name. `ExpandAligned` rejects unequal lengths
  instead of truncating or multiplying; `ExpandCartesian` is the only operation that expands the
  product. A caller cannot obtain a product from the aligned name by passing a flag.
- **KTD4 (governs R6):** Null, empty, and unequal are distinct. Null is a structural failure with
  a location; empty is a valid zero-row record; unequal is a length failure. The library does not
  coerce null to empty or truncate to the shorter side.
- **KTD5 (governs R7, R8):** The buffered `Validation` shape gives an all-or-nothing output. This
  matches the realistic import's need to correct input before loading rows. The contract states
  that no partial output exists; callers that need partial rows expand record by record.
- **KTD6 (governs R9):** Async changes input acquisition only. The child selectors and combiner
  stay synchronous, so no borrowed `JsonElement` or span crosses an await. The token is forwarded
  only to the asynchronous enumerator, matching the repository's async conventions.
- **KTD7 (governs R11, R16):** Provenance is an index pair passed by value. The minimal form is to
  ignore the origin; the full form is to store it in the caller's row record. The library does not
  build a `Location` or string per row. Failures carry a struct and render text only on demand.
- **KTD8 (governs R13, R18):** Decoding stays in `Decoder`. `Decoder.List` already composes
  `[recordIndex]` and `[elementIndex]` into `DecodeError.Path`; the expansion layer starts from
  typed values and does not re-parse JSON.
- **KTD9 (governs R15):** The hand-written consumer is the complete safe reference; the ordinary
  `JsonSerializer` + LINQ form is the honest happy-path baseline that throws or silently truncates
  on the failure fixtures. Fixed expected locations prevent two implementations from agreeing on a
  wrong answer.
- **KTD10 (governs R5, R17):** The guide and grammar state the aligned-versus-Cartesian distinction,
  the buffering/all-or-nothing behavior, and the two-valued provenance story explicitly. The
  measurement table biases toward the cheap minimal form and discloses the per-row trace cost.

### Open Decisions And Recommendations

1. **Buffered `Validation` or streaming per-row outcomes?** Options: buffered all-or-nothing
   `Validation`; deferred `IEnumerable<Result<Row, NestedRowError>>`; both. Recommendation:
   buffered only. The goal asks each operation to state partial-output behavior, not to provide a
   partial shape. A realistic import wants a complete report before loading; a streaming shape
   would add a second public contract and a second failure ledger without an evidenced consumer.
2. **How is provenance represented?** Options: a `NestedRow<T>` wrapper; a `RowOrigin` argument to
   `combine`; a `Location`/string per row. Recommendation: the origin argument. A wrapper adds a
   type and a `.Value` hop; a `Location` per row allocates and duplicates the experimental
   traversal type. The origin is a value-equal struct, and ignoring it is the minimal form.
3. **Ship Cartesian expansion or only aligned expansion?** Options: aligned only; aligned plus a
   separately named Cartesian operation. Recommendation: both. The goal requires aligned
   expansion to be distinguishable from Cartesian expansion; a second named operation makes the
   distinction executable and testable, and 0035 already permits explicitly requested relations.
   The aligned name still never infers a product.
4. **Null child list: structured failure or exception?** Options: `ArgumentNullException`;
   `InvalidOperationException` naming the record; structured `NestedRowError`. Recommendation:
   structured failure. The selector runs over data, and a null list can be a data-quality fact;
   the record index is useful, and the operation already has an error channel.
5. **Rejected items: a library rejection API or caller policy?** Options: a fallible combine
   returning `Result<Row, TError>`; a predicate with fail/skip policy; caller-owned composition.
   Recommendation: caller-owned. The library cannot choose the rejection error type or policy.
   The origin makes the rejection locatable, the library never silently drops a row, and a
   fallible-combine overload family would add a second generic error type for little gain.
6. **Two child sequences or 3-4?** Options: two only; 3-4 overloads; an array of typed sequences.
   Recommendation: two only. The aligned/Cartesian distinction needs exactly two; wider shapes
   compose with an intermediate record or `ZipExact`. No tuple/arity tower is added without a
   consumer.
7. **Child selector type: `IReadOnlyList<T>` or `IEnumerable<T>`?** Options: `IEnumerable<T>` with
   single enumeration into local lists; `IReadOnlyList<T>` supplied by the caller; `IReadOnlyCollection<T>`.
   Recommendation: `IReadOnlyList<T>`. Exact alignment needs both lengths before combining, and
   indexing avoids a second materialization; lazy sources are the caller's materialization point.
8. **Async shape: `ValueTask<Validation>` or `IAsyncEnumerable`?** Options: buffered
   `ValueTask<Validation>`; streaming `IAsyncEnumerable<Result<...>>`. Recommendation: buffered
   `ValueTask<Validation>`, matching the synchronous contract and the goal's source-acquisition
   framing. Cancellation and disposal follow ordinary async enumeration.
9. **Names.** Options considered: `Unnest`/`UnnestAligned`; `Flatten`/`FlattenAligned`;
   `Expand`/`ExpandAligned`/`ExpandCartesian`; `Rows`/`RowsAligned`. Recommendation:
   `Expand`/`ExpandAligned`/`ExpandCartesian`. `Expand` names the one-to-many shape; `Aligned` and
   `Cartesian` state the relation in the name, never in a flag. `Unnest` is accurate but less
   discoverable in C#; `Flatten` suggests sequence-of-sequences rather than nested-record
   rectangling.

### Acceptance Examples

- **AE1:** A JSON order array decodes through `Decoder.List` into typed records, and `Expand`
  yields one `ImportRow` per line in record and line order, retaining the order id and customer.
- **AE2:** A missing required line field produces `DecodeError` `Missing` at
  `[1].lines[0].quantity`; an explicit null produces `Null` at the same path; a wrong kind produces
  `WrongKind`; the expansion is never invoked on a failed decode.
- **AE3:** A record with an empty line list yields zero rows and no expansion error; other records
  still produce their rows.
- **AE4:** `ExpandAligned` over equal parallel arrays yields one row per index in increasing index
  order; a two-element pair set yields two rows, not four.
- **AE5:** `ExpandAligned` over lengths 1 and 2 yields one `ChildCountMismatch` with
  `RecordIndex = 1`, `FirstElementCount = 1`, and `SecondElementCount = 2`, and no rows.
- **AE6:** BCL `Zip` over the same unequal arrays yields one row and silently drops the extra
  element; the aligned operation reports the mismatch. The comparison asserts both behaviors.
- **AE7:** With two two-element child sequences, `ExpandAligned` yields two rows while
  `ExpandCartesian` yields four rows; the rows and order are fixed expectations.
- **AE8:** A typed child selector returning null yields a `NullChildSequence` (or the first/second
  variant) with the record index; it is never treated as an empty list.
- **AE9:** A caller rejects a row (for example a non-positive quantity) using the `RowOrigin`;
  the rejection records the record and element indices, and the library never silently drops the
  row. The comparison's hand-written and FunnySharp consumers agree on the rejection report.
- **AE10:** The `RowOrigin` passed to `combine` matches the row's record and element positions;
  the same operation with a combine that ignores the origin allocates no per-row provenance
  object, and storing the origin in the caller's record adds only the struct fields. A per-row
  `Location` string baseline allocates materially more.
- **AE11:** `ExpandAsync` over an `IAsyncEnumerable` source produces the same rows and failures as
  the synchronous form, forwards the exact token to `GetAsyncEnumerator`, disposes the enumerator,
  and propagates cancellation without converting it to `NestedRowError`.
- **AE12:** The comparison's fixed reports, enumeration ledgers, and measured tables agree across
  the hand-written and FunnySharp consumers; the ordinary form matches on success and throws or
  truncates on the failure fixtures.

## Implementation Units

### U1. Add provenance and diagnostic values

- **Goal:** Add `RowOrigin`, `CartesianRowOrigin`, `NestedRowErrorKind`, and `NestedRowError` with
  complete XML documentation and value semantics.
- **Requirements:** R1, R11, R12.
- **Dependencies:** None.
- **Files:** `src/FunnySharp/RowOrigin.cs`, `CartesianRowOrigin.cs`, `NestedRowErrorKind.cs`,
  `NestedRowError.cs`.
- **Approach:** Follow `LookupError<TKey>`: readonly struct, internal constructor, public
  properties, value equality, deterministic `ToString`, public validated factories for errors.
  Counts are nullable and present only for a mismatch. Rendered forms are
  `record[i].element[j]`, `record[i].elements[a,b]`, and compact failure text.
- **Execution note:** No `Location` or string is materialized by construction. Do not add equality
  operators to these non-carrier values; the repository reserves operators for carriers.
- **Patterns to follow:** `LookupError<TKey>`, `LookupErrorKind`, `BatchReadError<TError>`.
- **Test scenarios:** property values, value equality, hash equality, `ToString`, negative index
  and count rejection, equal-count mismatch rejection, null-free factories. `(happy path)`,
  `(edge)`, `(error path)`.
- **Verification:** Focused `NestedDataErrorTests`; core build.

### U2. Add the synchronous expansion operations

- **Goal:** Add `Expand`, `ExpandAligned`, and `ExpandCartesian` over `IEnumerable<TParent>`.
- **Requirements:** R2-R8, R10.
- **Dependencies:** U1.
- **Files:** `src/FunnySharp/NestedDataExtensions.cs`.
- **Approach:** One private core per shape or a shared record walk with shape-specific inner loops.
  Build rows in a presized `List<TResult>`; collect `NestedRowError` values in record order.
  Check selectors once per record; stop invoking `combine` once an error is recorded but keep
  checking later records. Return an exact-size array on success and no rows on failure.
  `ExpandAligned` compares counts before combining; `ExpandCartesian` uses nested loops.
- **Execution note:** Do not enumerate child lists more than once per record. Do not clone them.
  Do not run `combine` for a failing record or after the first recorded error. A null record
  argument is an eager `ArgumentNullException`; a null child list is data.
- **Patterns to follow:** `JoinExactlyOne` allocate/output/error discipline; `ExactZipExtensions`
  exact-length wording; `SequenceExtensions` presizing.
- **Test scenarios:** empty source, empty children, one/many children, order and count, aligned
  success/mismatch, Cartesian product, null lists, combine ledger, source single enumeration,
  argument nullness, selector/combine exceptions. `(happy path)`, `(edge)`, `(error path)`.
- **Verification:** Focused `NestedDataExpansionTests` and `NestedDataAlignmentTests`.

### U3. Add the asynchronous source forms

- **Goal:** Add `ExpandAsync`, `ExpandAlignedAsync`, and `ExpandCartesianAsync` over
  `IAsyncEnumerable<TParent>`.
- **Requirements:** R9, R10.
- **Dependencies:** U2.
- **Files:** `src/FunnySharp/NestedDataExtensions.cs`.
- **Approach:** `async`/`await foreach` over `source.WithCancellation(cancellationToken)` with
  `ConfigureAwait(false)`; reuse the per-record logic. Return `ValueTask` from an argument-validating
  wrapper, as `AggregateResultAsync` does. The source is enumerated once; no blocking.
- **Execution note:** Forward the token only to the enumerator; selectors and `combine` are
  synchronous and receive no token. Discard local rows/errors on cancellation or exception.
- **Patterns to follow:** `AsyncFallibleReductionExtensions.AggregateResultAsync`.
- **Test scenarios:** parity with sync, empty source, deferred start, cancellation, token forwarding,
  dispose, source fault, selector/combine exception, null arguments. `(happy path)`, `(edge)`,
  `(error path)`.
- **Verification:** Focused `NestedDataAsyncTests`.

### U4. Pin semantics with the xUnit suite

- **Goal:** Add focused tests for every contract boundary and the shared comparison.
- **Requirements:** R1-R14.
- **Dependencies:** U1-U3.
- **Files:** `tests/FunnySharp.Tests/NestedDataErrorTests.cs`,
  `NestedDataExpansionTests.cs`, `NestedDataAlignmentTests.cs`, `NestedDataAsyncTests.cs`,
  `NestedDataComparisonTests.cs`, `FunnySharp.Tests.csproj`.
- **Approach:** Attribute-only `[Fact]`/`[Theory]`, classic asserts, no skips. Use
  `ReductionProbe<T>`/`AsyncReductionProbe<T>` for source enumeration ledgers. Fixed expected
  origins and errors. The comparison test calls the shared verifier once.
- **Execution note:** Deliberate null violations use `null!` in test-local selectors. No sleeps or
  timing asserts. No analyzer suppressions outside deliberate-misuse diagnostics.
- **Patterns to follow:** `JoinTests`, `JoinStreamingTests`, `SummaryAsyncTests`.
- **Test scenarios:** AE1-AE11 plus repeated calls, empty strings, negative counts, combine ledgers,
  and disposition. `(happy path)`, `(edge)`, `(error path)`, `(integration)`.
- **Verification:** Focused run zero failed and zero skipped; full suite green.

### U5. Build the shared comparison, probes, and measurement harness

- **Goal:** Prove row/error parity, fixed locations, source consumption, and costs.
- **Requirements:** R15, R16.
- **Dependencies:** U1-U4.
- **Files:** `tests/Shared/NestedDataComparison/NestedModels.cs`,
  `NestedFixtures.cs`, `OrdinaryNestedConsumer.cs`, `HandWrittenNestedConsumer.cs`,
  `FunnySharpNestedConsumer.cs`, `NestedComparison.cs`,
  `tests/FunnySharp.NestedDataMeasurements/FunnySharp.NestedDataMeasurements.csproj`,
  `Program.cs`, `NestedDataMeasurements.cs`, `packages.lock.json`, `FunnySharp.slnx`.
- **Approach:** Shared fixtures cover valid nested lines, empty lines, missing/null/wrong-kind
  fields, rejected lines, aligned equal/unequal/empty arrays, Cartesian variants, null typed
  lists, and async sources. The ordinary consumer uses `JsonSerializer` + `SelectMany`/`Zip`; the
  hand-written consumer uses `JsonDocument` + loops + explicit paths; the FunnySharp consumer uses
  `Decoder.List` + `Expand`/`ExpandAligned`/`ExpandCartesian` with rejection via `RowOrigin`.
  Fixed expected reports cover rows, rejections, and failure positions. The comparison asserts
  source enumeration/selector/combine counts and the aligned-versus-Cartesian row count.
  The measurement executable runs the comparison first, then measures the full import, expansion
  success/failure, provenance storage, and location-string baselines at N=1024.
- **Execution note:** Do not share mutable reports. Measure both provenance forms with symmetric
  input construction. Allocation is the stable comparison; timing is directional. The measurement
  project disables tiered compilation.
- **Patterns to follow:** `tests/Shared/JoinComparison`, `JoinMeasurements.cs`,
  `MeasurementHarness`.
- **Test scenarios:** all fixtures, fixed positions, ordinary success/truncation, rejection
  locations, consumption ledgers, measurement reproduction. `(integration)`.
- **Verification:** Comparison passes in xUnit and measurement; Release run prints allocation and
  timing tables twice with identical allocation rows.

### U6. Add the compiling example and documentation samples

- **Goal:** Show the whole workflow and provide byte-exact guide snippets.
- **Requirements:** R17.
- **Dependencies:** U1-U3.
- **Files:** `examples/FunnySharp.Examples/NestedDataExamples.cs`,
  `examples/FunnySharp.Examples/Program.cs`,
  `examples/FunnySharp.DocumentationSamples/NestedDataSamples.cs`.
- **Approach:** The example parses a nested order document, decodes typed records, expands lines
  with origins, reports a mismatch and a rejection, shows aligned-versus-Cartesian counts, and
  runs the async form. The documentation sample provides the guide snippets: decode-and-expand,
  aligned expansion, provenance/rejection, and the aligned/Cartesian distinction. All outcomes are
  consumed through `Match`/`TryGet...`; the analyzer build stays clean.
- **Execution note:** The example project must build with zero `FS####` diagnostics. Do not discard
  a `Result`/`Validation`. Keep snippet source and guide byte-identical.
- **Patterns to follow:** `JoinExamples`, `CardinalityJoinSamples`, sample registration shape.
- **Test scenarios:** the example prints the exact `FunnySharp examples passed.` marker; the docs
  verifier compares every new snippet. `(integration)`.
- **Verification:** `dotnet run --project examples/FunnySharp.Examples -c Release`;
  `verify-docs-snippets`.

### U7. Reconcile docs, baseline, manifests, and goal state

- **Goal:** Ship coherent documentation and committed surface records.
- **Requirements:** R17, R18.
- **Dependencies:** U1-U6.
- **Files:** `docs/nested-data-reshaping.md`,
  `docs/nested-data-reshaping-evidence.md`, `docs/grammar.md`, `docs/product-contract.md`,
  `docs/stability-inventory.md`, `docs/performance.md`, `docs/data-pipelines.md`,
  `docs/decoding.md`, `docs/AGENTS.md`, `docs/plans/AGENTS.md`, `docs/goals/README.md`,
  `docs/goals/AGENTS.md`, `docs/goals/archive/0036-goal.md`, `AGENTS.md`, `README.md`,
  `README_zh-cn.md`, `src/FunnySharp/AGENTS.md`, `tests/FunnySharp.Tests/AGENTS.md`,
  `examples/FunnySharp.Examples/AGENTS.md`,
  `examples/FunnySharp.DocumentationSamples/AGENTS.md`,
  `eng/api-baseline/FunnySharp.public-api.txt`, `eng/performance/baseline.json`,
  `eng/performance/competitor-baseline.json`, `eng/harness/DocsSnippets.fs`,
  `tests/FunnySharp.Harness.Tests/DocsSnippetsTests.fs`.
- **Approach:** The guide starts with `## API Shape`, covers decoding, expansion, alignment,
  Cartesian distinction, provenance/cost, async/lifetime, the operation-choice table, and closes
  with `## Deliberate Boundaries`. Grammar adds the family and reconciles the older "no
  duplicate-sensitive builder"/"no expansion" boundary wording without weakening Goal 0035.
  Product contract gains one stable-surface paragraph and vocabulary clarification. Stability
  inventory adds the family row. Performance adds characteristic rows and the evidence exclusion.
  The guide joins the primary-guide and snippet corpus; hardcoded counts in harness tests and
  AGENTS files are updated. The API baseline is regenerated with `--write` after the surface is
  final. Both performance manifests add the new source paths in ordinal order. Goal 0036 moves
  unchanged to the archive and the index removes it from Next-Stage.
- **Execution note:** Do not hand-edit generated inventory, archived goal contents, or historical
  release evidence. Archive is a rename of the unchanged file. Do not modify Goal 0037.
- **Patterns to follow:** Goal 0035 landing shape (guide/evidence/example/baseline/manifests/
  archive/index).
- **Test scenarios:** API baseline, docs snippets, format, manifest shape, example run, full test
  gate. `(integration)`.
- **Verification:** `verify-api-baseline`, `verify-docs-snippets`, `format`, full `test`.

## Verification Contract

| Gate | Command | Applies to | Completion evidence |
| --- | --- | --- | --- |
| Build | `dotnet fsi build.fsx -- -p build` | U1-U7 | exit 0, zero warnings/errors |
| Test | `dotnet fsi build.fsx -- -p test` | U1-U7 | all tests passed, zero failed, zero skipped |
| Format | `dotnet fsi build.fsx -- -p format` | U1-U7 | exit 0 |
| Docs snippets | `dotnet fsi build.fsx -- -p verify-docs-snippets` | U6, U7 | exit 0 with the new guide and snippets in the corpus |
| API baseline | `dotnet fsi build.fsx -- -p verify-api-baseline` (after `--write`) | U7 | exit 0 with only the new members |
| Example | `dotnet run --project examples/FunnySharp.Examples/FunnySharp.Examples.csproj -c Release` | U6 | exact `FunnySharp examples passed.` |
| Comparison and measurement | `dotnet run --project tests/FunnySharp.NestedDataMeasurements/FunnySharp.NestedDataMeasurements.csproj -c Release` | U5, U6 | comparison passes; allocation and timing tables captured twice with identical allocation |
| Manifest shape | full test suite (`PerformanceManifestTests`) | U7 | source lists sorted and complete |

## Definition of Done

### Per-Unit Completion

| Unit | Done when |
| --- | --- |
| U1 | Origin/error values expose deterministic properties and text; equality and validation pass focused tests; construction allocates nothing. |
| U2 | All three synchronous shapes produce exact order/count on success and no rows plus all record-ordered errors on failure; null, empty, and mismatch are distinct; source is enumerated once. |
| U3 | Async forms match sync semantics, enumerate once, forward the exact token, dispose the enumerator, and propagate cancellation/faults. |
| U4 | Focused tests pass with zero failed and zero skipped; the full suite remains green. |
| U5 | The comparison proves fixed rows/rejections/locations, hand/FunnySharp parity, ordinary success and truncation behavior, source/selector/combine ledgers, and aligned-versus-Cartesian counts; the measurement run prints real costs twice. |
| U6 | The example prints the exact success marker; guide snippets byte-match the documentation sample regions; analyzer diagnostics are clean. |
| U7 | The guide closes with `## Deliberate Boundaries`; product contract, grammar, inventory, performance, baseline, manifests, READMEs, and goal archive agree; Goal 0037 is untouched. |

### Global Completion

- R1-R18 each map to an implementation artifact and an executed verification result.
- AE1-AE12 pass in the test suite, the comparison, or the evidence run.
- The evidence records missing/null fields, empty children, unequal child lengths, rejected items,
  source enumeration counts, provenance cost, and complete ordinary and hand-written comparisons.
- The evidence records the maintainer judgment on whether the expansion family materially reduces
  complete import code, including a sceptical boundary for plain `SelectMany` cases.
- The aligned-versus-Cartesian distinction is explicit in the API, guide, grammar, tests, and
  comparison; no flag hides it.
- No new carrier, runtime dependency, serializer, reflection binder, DataFrame engine, implicit
  Cartesian expansion, span-retaining result, async row callback, keyed relation, or Goal 0037
  behavior is introduced.
