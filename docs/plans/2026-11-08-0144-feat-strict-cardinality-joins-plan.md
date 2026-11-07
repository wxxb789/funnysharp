---
title: Strict-cardinality keyed lookups and exactly-one joins
type: feat
date: 2026-11-08
artifact_contract: ce-unified-plan/v1
product_contract_source: docs/goals/archive/0035-goal.md, docs/product-contract.md, docs/grammar.md
execution: code
---

# Strict Cardinality Joins Plan

## Goal Capsule

- **Objective:** Let a consumer state and enforce the cardinality of keyed lookup and association
  code instead of collapsing missing, duplicate, or ambiguous data into a convenient value. An
  order-to-customer enrichment must be expressible as "exactly one customer per order", must report
  structured duplicate-key and missing-key failures with their real source positions, and must
  preserve source order and the one-output-per-input relationship on success.
- **Means:** A materialized `LookupIndex<TKey, TValue>` that preserves every occurrence under an
  explicit equality policy; a `UniqueLookupIndex<TKey, TValue>` that carries the at-most-one
  guarantee over the complete indexed input; cardinality-named keyed reads
  (`GetMany`, `GetZeroOrOne`, `GetExactlyOne`, `GetOption`); a structured
  `LookupError<TKey>` value carrying the failure kind and source positions; a buffered
  `JoinExactlyOne` over either a raw lookup sequence or a prebuilt unique index; and a streaming
  `JoinExactlyOneResults` that yields one `Result` per source row from a prebuilt many index.
- **Authority:** [Goal 0035](../goals/archive/0035-goal.md) is the completion contract.
  [Product contract](../product-contract.md), [grammar](../grammar.md), and
  [core design](../core-design.md) are the current boundaries. Goal 0036 (nested shape
  transformation and provenance) remains out of scope: the combine callback returns the caller's
  flat typed record, and this goal adds no nesting, rectangling, or provenance API.
- **Execution profile:** Implement in
  `src/FunnySharp/LookupErrorKind.cs`, `LookupError.cs`, `LookupIndex.cs`,
  `UniqueLookupIndex.cs`, and `LookupExtensions.cs`; pin behavior in
  `tests/FunnySharp.Tests`; share the reference consumers and comparison in
  `tests/Shared/JoinComparison`; exercise through
  `examples/FunnySharp.Examples/JoinExamples.cs` and the documentation samples; measure through
  `tests/FunnySharp.JoinMeasurements`; record evidence in
  `docs/cardinality-joins-evidence.md`.
- **Tail ownership:** The maintainer judges whether the safer relation contract materially
  simplifies complete application code. The evidence records a judgment even if it is sceptical.
  A negative judgment returns to the lookup/join surface or the evidence framing; it does not
  narrow the goal contract.
- **Stop conditions:** Stop for a new decision if the design would need a second outcome carrier,
  a hidden enum/boolean cardinality switch, a dictionary overload that claims to reveal an opaque
  comparer, a many-to-many join or implicit Cartesian expansion, a database-integrity claim, an
  async mirror not named by the goal, or any nested-shape/provenance behavior owned by Goal 0036.

## Product Contract

**Included**

- `LookupErrorKind` with `NullKey`, `DuplicateKey`, and `MissingKey`.
- `LookupError<TKey>`: a small value carrying the kind, key, the probe/source position, the
  lookup-source position, and the first lookup-source position for a duplicate. Failures are
  created by the library and expose deterministic properties and text; no exception is converted
  into an error.
- `LookupIndex<TKey, TValue>`: a sealed, eagerly materialized index over
  `IEnumerable<TValue>`, keyed by a caller selector under an explicit comparer. It preserves every
  occurrence in lookup-source order and exposes:
  - `Comparer` — the equality policy actually used.
  - `DistinctCount` — number of distinct keys.
  - `EntryCount` — number of retained values.
  - `GetMany(key)` — all values for a key in lookup-source order, empty when absent.
  - `GetZeroOrOne(key)` — `Result<Option<TValue>, LookupError<TKey>>`; zero is `Some(None)`, one
    is `Some(Some(value))`, two or more is `DuplicateKey` with both lookup positions.
  - `GetExactlyOne(key)` — `Result<TValue, LookupError<TKey>>`; zero is `MissingKey`, one is the
    value, two or more is `DuplicateKey`.
  - `RequireUniqueKeys()` — `Validation<UniqueLookupIndex<TKey, TValue>, LookupError<TKey>>` over
    the complete retained index.
- `UniqueLookupIndex<TKey, TValue>`: a sealed wrapper proving at most one value per key over the
  complete indexed input. It exposes `Comparer`, `Count`, and `GetOption(key)`.
- `ToLookupIndex(source, keySelector, comparer = null)` and
  `ToUniqueLookupIndex(source, keySelector, comparer = null)` as extension factories returning
  `Validation` of the index or its structural failures. Both enumerate the lookup source once.
- `JoinExactlyOne(source, lookupSource, sourceKeySelector, lookupKeySelector, combine,
  comparer = null)`:
  - builds and validates a unique index over the complete lookup source;
  - probes the source once in source order;
  - returns `Validation<IReadOnlyList<TResult>, LookupError<TKey>>`;
  - on success returns exactly one combined output per source row in source order;
  - on failure returns no outputs and every lookup-source structural error followed by every
    source-row null/missing error, each with real positions.
- `JoinExactlyOne(source, lookup, sourceKeySelector, combine)` over a prebuilt
  `UniqueLookupIndex<TKey, TValue>`: the same buffered output and missing/null diagnostics without
  rebuilding or re-validating the index.
- `JoinExactlyOneResults(source, lookup, sourceKeySelector, combine)` over a prebuilt
  `LookupIndex<TKey, TValue>`: a deferred `IEnumerable<Result<TResult, LookupError<TKey>>>`
  yielding exactly one outcome per source row in source order. It checks uniqueness only for
  matched keys; a duplicate in an unprobed key is not a failure. It emits successes before a later
  failure, never throws or silently drops a data failure, and makes no all-or-nothing or
  complete-input guarantee.
- Explicit comparer support everywhere: `null` means `EqualityComparer<TKey>.Default`; the index
  exposes the selected comparer. There is deliberately no overload accepting
  `IReadOnlyDictionary<TKey, TValue>` and claiming to inherit its equality policy.
- Null-key policy: `TKey : notnull`. A lookup-source selector result that is null is a `NullKey`
  lookup failure; a probe-source selector result that is null is a `NullKey` source failure. A
  null key passed directly to a `Get*` member is an eager `ArgumentNullException`. A present null
  value is retained by `GetMany` and can be returned by `GetExactlyOne`, but the `Option`-shaped
  reads (`GetZeroOrOne`, `GetOption`) reject it with `ArgumentNullException`, matching the existing
  container bridges.
- Uniqueness scope is explicit per operation:
  - complete retained input: `RequireUniqueKeys`, `ToUniqueLookupIndex`, and the raw
    `JoinExactlyOne`;
  - matched keys only: `GetZeroOrOne`, `GetExactlyOne`, and `JoinExactlyOneResults` over a many
    index;
  - already established: `JoinExactlyOne` over a `UniqueLookupIndex`.
- Duplicate keys never multiply outputs. `GetMany` is the only explicit expansion primitive; a
  many-to-many join is not added.
- Success preserves source order and output count (`output.Count == source.Count` for the buffered
  join; one outcome per source row for the streaming join).
- The buffered join's `combine` callback runs only for a row whose key has exactly one match and
  only while no structural error has been recorded. Once an error is recorded, later rows are
  still checked for null/missing keys, but `combine` is not invoked. Earlier invocations are not
  rolled back. The streaming join invokes `combine` independently for each successful row.
- Mutable inputs are retained by reference under a stated lifetime contract: keys are read once
  during index construction; later key changes are not observed; later value mutations are
  visible; no clone, snapshot of values, or rollback is provided. The index is read-only after
  construction and adds no locking.
- A complete idiomatic-C#, hand-indexed, and FunnySharp comparison covers index construction,
  successful output materialization, missing-key error storage, duplicate-key error storage,
  non-default equality, single enumeration, and real locations.
- A measurement executable reports allocation and directional timing for index, output, and
  error-storage costs.

**Deferred to Follow-Up Work**

- A buffered zero-or-one join: `UniqueLookupIndex.GetOption` and `LookupIndex.GetZeroOrOne` express
  zero-or-one reads; a join has no single natural output shape for an absent match (dropped row vs
  `Option` per row), so no zero-or-one join overload is added.
- A many-to-many or one-to-many join: explicit expansion uses `GetMany` with ordinary LINQ; no
  operation infers a Cartesian product.
- Async `IAsyncEnumerable` mirrors: index construction and joining are in-memory calculations after
  the caller acquires data; async acquisition remains the caller's boundary.
- An index over `IReadOnlyDictionary<TKey, TValue>`: the interface cannot reveal its equality
  policy; callers pass the dictionary's pairs as a sequence and state the comparer explicitly.
- Database constraints, transactions, concurrency control, and stored-data integrity: the index
  validates only the in-memory lookup snapshot it was given.
- Nested output reshaping, rectangling, and provenance: Goal 0036.

**Outside this product's identity**

- A general SQL provider, ORM query translator, DataFrame universe, second collection hierarchy,
  new outcome carrier, custom equality-policy inference, or automatic Cartesian join.

## Planning Contract

### Requirements

- **R1:** Add the public `LookupErrorKind`, `LookupError<TKey>`, `LookupIndex<TKey, TValue>`,
  `UniqueLookupIndex<TKey, TValue>`, and `LookupExtensions` surface with complete XML
  documentation.
- **R2:** `ToLookupIndex` enumerates the lookup source exactly once, runs the key selector once per
  item, preserves every occurrence in source order, and reports each null key as a `NullKey`
  failure with its lookup-source position.
- **R3:** `LookupIndex` supports `GetMany`, `GetZeroOrOne`, and `GetExactlyOne` with the cardinality
  meanings above; `GetMany` is source-ordered and empty for an absent key; query members reject a
  null key argument eagerly.
- **R4:** `RequireUniqueKeys` validates the complete retained index, reports every occurrence after
  the first for each duplicate key with the first and duplicate lookup positions, and returns a
  `UniqueLookupIndex` only when no duplicate exists.
- **R5:** `ToUniqueLookupIndex` combines null-key failures and duplicate-key failures in
  lookup-source order and returns a `UniqueLookupIndex` only when both sets are empty.
- **R6:** `JoinExactlyOne` over a raw lookup sequence builds the index once, validates the complete
  lookup input for null and duplicate keys, probes the source once, and returns either exactly one
  output per source row in source order or no outputs plus all structural failures.
- **R7:** `JoinExactlyOne` over a `UniqueLookupIndex` probes the source once, returns exactly one
  output per source row in source order on success, and reports missing/null source keys as
  `MissingKey`/`NullKey` failures.
- **R8:** Buffered join failure diagnostics contain all lookup-source errors first, in lookup-source
  order, followed by all source-row errors in source order. The combine callback follows the
  no-work-after-the-first-error rule stated in the product contract.
- **R9:** `JoinExactlyOneResults` is deferred, enumerates the source once per iteration, yields one
  outcome per source row in source order, and reports matched-key duplicates with the source row
  and both lookup positions. It does not re-enumerate or re-validate the lookup source.
- **R10:** A null lookup key is a structural `NullKey` lookup failure; a null source key is a
  per-row `NullKey` source failure; direct null key arguments throw `ArgumentNullException`; a
  present null value remains visible through `GetMany` and `GetExactlyOne` and is rejected by
  `Option`-shaped reads exactly like the existing container bridges.
- **R11:** The comparer is always explicit or `EqualityComparer<TKey>.Default`; every index exposes
  it; the comparer never observes a null key; comparer exceptions propagate unchanged; no
  dictionary overload claims to know an opaque equality policy.
- **R12:** Duplicate lookup keys never produce more than one output for a source row; `GetMany` is
  the explicit one-to-many expansion primitive; no accidental Cartesian product is possible.
- **R13:** The index retains values by reference; keys are read once at construction; the guide and
  XML documentation state the mutation, lifetime, and concurrency limits, and provide no clone or
  rollback.
- **R14:** Source, selector, comparer, combine, and disposal exceptions keep ordinary .NET
  semantics and are never converted into `LookupError` values. Argument and delegate nullness is
  validated eagerly.
- **R15:** Add focused tests for empty and repeated keys, unmatched rows, ambiguous matches,
  non-default equality, null keys and values, exact source positions, single enumeration, output
  order/count, no expansion, mutable-value visibility, and both buffered and streaming shapes.
- **R16:** Add a complete idiomatic-C#, hand-indexed, and FunnySharp comparison with fixed expected
  reports and real consumption ledgers; add a measurement executable for index, output, and
  error-storage costs.
- **R17:** Ship a compiling example, a guide closing with `## Deliberate Boundaries`, an evidence
  record, API-baseline and performance-manifest updates, documentation-sample coverage, and
  reconciled product/grammar/stability/index/goal documents.
- **R18:** Add no new carrier, runtime dependency, SQL/ORM/DataFrame layer, database-integrity
  claim, implicit join type, or Goal 0036 shape behavior.

### Key Technical Decisions

- **KTD1 (governs R1, R18):** The index is a sealed materialized association description, not a
  carrier. Lookups return canonical `Option`/`Result` values and joins return canonical
  `Validation` values. `LookupError<TKey>` is a diagnostic value, like `BatchReadError<TError>`,
  not a second outcome channel.
- **KTD2 (governs R2, R4, R5):** `LookupIndex` stores a key dictionary plus one flat entry per
  retained value and a per-key linked order. This keeps the index allocation proportional to the
  lookup input, preserves source order in `GetMany`, and keeps both duplicate positions close at
  hand for structured errors. `UniqueLookupIndex` wraps the same index after a complete scan; it
  copies no values.
- **KTD3 (governs R3, R10):** Keyed reads name their cardinality. `GetMany` is the explicit
  one-to-many read; `GetZeroOrOne` reports ambiguity as a typed failure; `GetExactlyOne` requires
  a match; `GetOption` is the zero-or-one read on a proven unique index. No boolean or enum
  parameter hides the cardinality at a call site.
- **KTD4 (governs R4, R5, R6, R9):** Uniqueness scope is a property of the operation, not a global
  claim. Complete-input validation is used where the operation already buffers the lookup input
  (unique index construction, `RequireUniqueKeys`, raw buffered join). The streaming join uses a
  prebuilt many index and checks only matched keys, so it can honestly emit output without
  pretending unused duplicates were validated.
- **KTD5 (governs R6, R7, R8):** The buffered join returns `Validation`, not `Result`: duplicate
  lookup keys and missing source rows are independent structural failures, and a data-cleaning
  consumer needs every location to fix the input. The operation is all-or-nothing on outputs; it
  never returns a partial list as a success.
- **KTD6 (governs R8, R9):** The buffered join and the streaming join have deliberately different
  work and guarantee contracts. The buffered join buffers the index and the output list, validates
  the complete lookup input, and returns no output on any error. The streaming join buffers only
  the prebuilt index and yields per-row outcomes; successes can precede a later failure, and the
  consumer learns every reached failure as an item. Neither shape is documented as an alias for the
  other.
- **KTD7 (governs R9):** `JoinExactlyOneResults` is a deferred iterator over the source. It yields
  one `Result` per row, including failures, so the per-row count is stable and a duplicate never
  multiplies output. Early consumer disposal stops later source work and therefore stops later
  failure observation; that is part of the streaming contract, not a broken promise.
- **KTD8 (governs R10, R11):** Null keys are structural failures because the key domain is
  `notnull`. The comparer never receives a null. There is no dictionary-input overload: the
  interface cannot reveal whether its keys use the default, ordinal-ignore-case, culture-aware, or
  a custom policy. A caller who starts from a dictionary passes its pairs and states the comparer.
- **KTD9 (governs R12):** The join is many-to-one. Duplicate lookup keys either fail complete-input
  validation or produce one ambiguity failure for the probed row; they never produce additional
  output rows. Explicit one-to-many expansion remains `GetMany` plus ordinary `SelectMany`.
- **KTD10 (governs R13, R14):** The index never clones values. Keys are selected once during
  construction; value references are retained. Source, delegate, and comparer exceptions propagate;
  acquired enumerators are disposed by `foreach`. The index adds no locks and makes no claim for
  concurrent mutation of retained values.
- **KTD11 (governs R15, R16):** The comparison uses fixed expected reports for the critical failure
  locations, not only cross-consumer equality, so two consumers cannot agree on a wrong answer.
  Probes assert enumerator creation, `MoveNext`, `Current`, and `Dispose` counts for both inputs.
- **KTD12 (governs R17, R18):** The guide closes with `## Deliberate Boundaries`, uses byte-exact
  documentation samples in the existing corpus, and the measurement harness runs the comparison
  before printing costs. No new guide snippet lives outside the verifier's corpus.

### Open Decisions And Recommendations

1. **One index type with a uniqueness flag, or two types?** Options: a single `LookupIndex` with a
   nullable unique wrapper; two sealed types; a mode enum stored on the index. Recommendation: two
   types. `UniqueLookupIndex` carries the at-most-one fact in the type, so the buffered join cannot
   be called with an unvalidated index; a runtime flag would move the same check back into every
   call.
2. **Complete-input or matched-entry uniqueness?** Options: always complete; always matched;
   explicit per operation. Recommendation: explicit per operation, as in KTD4 and the scope table.
   Complete validation is the safer buffered contract; matched-only checking is the honest
   streaming contract. Claiming one for the other is the failure the goal warns about.
3. **Null keys: structured failures or exceptions?** Options: allow null as an ordinary key;
   throw everywhere; structured `NullKey` for data inputs and `ArgumentNullException` for direct
   query arguments. Recommendation: the third form. Null is a data-quality failure in a lookup
   source or probe source and deserves a position; a null key passed directly to a `Get*` member
   violates the declared `notnull` key domain and is an argument error.
4. **Buffered join carrier: `Result` or `Validation`?** Options: fail-fast `Result`; accumulating
   `Validation`; `Result` of a list of row outcomes. Recommendation: `Validation`. Duplicate and
   missing failures are independent structural facts; fail-fast would throw away locations the
   goal explicitly requires. The success shape is still all-or-nothing.
5. **Zero-or-one join?** Options: a buffered join returning `Option` per row; a join that drops
   unmatched rows; no zero-or-one join. Recommendation: no zero-or-one join. `GetOption` and
   `GetZeroOrOne` already express the lookup; a join would have to choose an absent-row output shape
   that is not implied by the goal, and dropping rows would violate the output-count promise.
6. **Streaming input: raw lookup sequence or prebuilt index?** Options: build the index lazily
   inside the stream; require a prebuilt many index; require a prebuilt unique index. Recommendation:
   require a prebuilt many index. The index build is the buffering/validation boundary; the stream
   then has a stable snapshot and can check matched keys honestly. Building inside the stream would
   either buffer before the first yield (not streaming) or repeatedly scan the lookup source.
7. **Async mirrors?** Options: add `IAsyncEnumerable` index/join forms; keep the family synchronous.
   Recommendation: synchronous only. The goal names lookup/association code and its evidence is
   single-enumeration and error-location evidence; asynchronous acquisition remains the caller's
   boundary, and a second overload family would add surface without an evidenced consumer.
8. **Dictionary input overload?** Options: accept `IReadOnlyDictionary` and use it directly;
   accept it only with an explicit comparer; no dictionary overload. Recommendation: no dictionary
   overload for index construction. The comparer parameter must be the stated policy; accepting a
   dictionary would invite the false claim that its opaque policy is known. A caller can enumerate
   its pairs into the sequence overload.
9. **Naming.** Options considered: `IndexBy`/`UniqueIndex`; `LookupIndex`/`UniqueLookupIndex`;
   `Join`/`JoinExactlyOne`/`Enrich`. Recommendation: `LookupIndex`/`UniqueLookupIndex` as the nouns,
   `ToLookupIndex`/`ToUniqueLookupIndex` as the factories, `JoinExactlyOne` for the cardinality in
   the name, and `JoinExactlyOneResults` for the per-row stream. `Enrich` was rejected because it
   suggests field addition rather than a relation contract; a bare `Join` was rejected because it
   hides the cardinality.

### Decision Log

- **D1 (2026-11-08):** Keep the two same-named `JoinExactlyOne` overloads' generic parameter
  orders: `<TSource, TLookup, TKey, TResult>` for the raw lookup-sequence overload and
  `<TSource, TKey, TValue, TResult>` for the prebuilt-unique-index overload. Their XML docs now
  state the inference order explicitly (inferable sequence/source parameters first; the lookup
  value follows its index/sequence argument). **Rejected alternative:** reorder the raw overload
  to `<TSource, TKey, TLookup, TResult>` for visual symmetry. Rejected because `TLookup` must stay
  generic so the overload family serves both `LookupIndex` and `UniqueLookupIndex` without
  duplicating the public surface; reordering would only move the inconsistency. This is a
  pre-release public-surface decision.

### Acceptance Examples

- **AE1:** `orders.JoinExactlyOne(customers, ...)` over three orders and three unique matching
  customers returns three outputs in order; the lookup and source sequences are each enumerated
  once.
- **AE2:** A missing customer at order index 1 yields a `Validation` failure with `Kind =
  MissingKey`, `Key = "C-9"`, `SourceIndex = 1`, and no outputs.
- **AE3:** A duplicated customer key yields a `DuplicateKey` failure with the duplicate and first
  lookup positions, even when no order uses that key; the raw buffered join fails on the complete
  lookup input.
- **AE4:** The same duplicated, unused key does not fail `JoinExactlyOneResults` rows that do not
  use it; those rows succeed or report their own missing key. A row that matches the duplicate key
  yields exactly one `DuplicateKey` outcome with the row position and both lookup positions.
- **AE5:** A non-default `StringComparer.OrdinalIgnoreCase` groups and matches keys case-
  insensitively, detects case-only duplicates, and is exposed as `index.Comparer`; the same data
  under `StringComparer.Ordinal` has no duplicate.
- **AE6:** A `Dictionary<string, Customer>` built with an opaque case-insensitive policy is passed
  as pairs with an explicit ordinal comparer; the index uses ordinal equality, proving the
  dictionary interface did not supply the policy.
- **AE7:** Empty lookup input with source rows yields one `MissingKey` per row; empty source yields
  a valid empty output list; an empty string key is an ordinary key and matches.
- **AE8:** Repeated source keys produce repeated outputs in source order; duplicate lookup keys
  never increase the output count; `GetMany` is the explicit expansion path.
- **AE9:** A null lookup key yields `NullKey` at its lookup position; a null source key yields
  `NullKey` at its source position; direct null key arguments throw `ArgumentNullException`.
- **AE10:** `JoinExactlyOneResults` yields successes before a later missing failure, yields one
  outcome per source row, and does not invoke `combine` for a failure row; early disposal stops
  later source work.
- **AE11:** The comparison's hand-indexed consumer, the FunnySharp consumer, and the fixed expected
  reports agree on outputs and failure positions; the idiomatic LINQ form produces the same output
  on success and throws without structured locations on duplicates and missing rows.
- **AE12:** The measurement executable reports index, output, and error-storage allocation and
  directional timing for the compared consumers, and the evidence records the maintainer judgment.

## Implementation Units

### U1. Add the error values and the many-occurrence index

- **Goal:** Add `LookupErrorKind`, `LookupError<TKey>`, and `LookupIndex<TKey, TValue>` with
  complete XML documentation.
- **Requirements:** R1, R2, R3, R10, R11, R13, R14.
- **Dependencies:** None.
- **Files:** `src/FunnySharp/LookupErrorKind.cs`, `LookupError.cs`, `LookupIndex.cs`.
- **Approach:** `LookupError` is a readonly struct with kind, nullable key, nullable source position,
  nullable lookup position, and nullable first-lookup position; factories for null lookup key, null
  source key, duplicate key, and missing key; value equality and deterministic rendering. The index
  stores a `Dictionary<TKey, GroupHead>` and a flat `List<Entry>`; each entry holds the value, its
  lookup-source position, and the next entry for the same key. `Build` is the single enumeration
  point and appends null-key failures to a supplied error list. `GetMany`, `GetZeroOrOne`, and
  `GetExactlyOne` use one internal group-state read. `RequireUniqueKeys` scans the complete index.
- **Execution note:** No LINQ in the build hot path. No public constructor; the factory methods are
  the only construction path. Do not store keys in entries; the dictionary key is the first key
  spelling under the comparer. Reject a null query key before dictionary access. Do not invoke the
  comparer for null keys.
- **Patterns to follow:** `BatchReadError<TError>` factories and equality; `CardinalityExtensions`
  single-enumeration and null wording; `Option<T>.Some` null rejection.
- **Test scenarios:** empty source, empty-string key, repeated keys, source-order `GetMany`,
  zero/one/multiple reads, exact duplicate positions, null lookup key, null key argument, null
  value visibility, comparer exposure, single enumeration, argument nullness. `(happy path)`,
  `(edge)`, `(error path)`.
- **Verification:** Focused `LookupIndexTests`; build the core project.

### U2. Add unique validation and the unique index

- **Goal:** Add `UniqueLookupIndex<TKey, TValue>` and the complete-input uniqueness path.
- **Requirements:** R4, R5, R11, R13.
- **Dependencies:** U1.
- **Files:** `src/FunnySharp/UniqueLookupIndex.cs`, `LookupIndex.cs`, `LookupExtensions.cs`.
- **Approach:** `RequireUniqueKeys` walks every group, emits one duplicate error per occurrence
  after the first with both positions, sorts the errors by duplicate lookup position, and returns
  `Validation.InvalidMany` or a wrapper around the same index. `ToUniqueLookupIndex` builds the
  many index while collecting null errors, calls `RequireUniqueKeys`, merges null and duplicate
  errors in lookup-source order, and returns a valid wrapper only when both are empty.
  `GetOption` on the wrapper uses the internal single-value read.
- **Execution note:** The wrapper copies no values and exposes no mutable storage. The index is
  immutable after construction; the wrapper is a proof over that snapshot.
- **Patterns to follow:** `Validation.InvalidMany` accumulation order;
  `BatchScope` cache/validation wording for complete-input checks.
- **Test scenarios:** all-unique success, one duplicate, three occurrences, duplicate under a
  non-default comparer, null plus duplicate error order, wrapper `GetOption`, empty index,
  `RequireUniqueKeys` repeated calls. `(happy path)`, `(edge)`, `(error path)`.
- **Verification:** Focused tests; `ToUniqueLookupIndex` and `RequireUniqueKeys` parity.

### U3. Add the buffered exactly-one join

- **Goal:** Add both `JoinExactlyOne` overloads with complete diagnostics and order/count guarantees.
- **Requirements:** R6, R7, R8, R12, R14.
- **Dependencies:** U1, U2.
- **Files:** `src/FunnySharp/LookupExtensions.cs`.
- **Approach:** The raw overload validates arguments, builds the many index with a shared error list,
  validates uniqueness, sorts lookup errors, then probes the source with a local index counter. It
  appends source null/missing errors in source order and stops invoking `combine` after the first
  recorded error while continuing to probe. The unique-index overload probes with the wrapper's
  internal single-value read and applies the same combine policy. Both return a valid exact-size
  array on success, or `Validation.InvalidMany` with lookup errors followed by source errors.
- **Execution note:** No partial output is returned. Do not sort the combined list across the two
  inputs; the documented order is lookup-source order then source order. Return a fresh array, not
  the mutable list.
- **Patterns to follow:** `FallibleReductionExtensions` single-pass wording;
  `Validation.InvalidMany`; `BatchScope` source-order diagnostics.
- **Test scenarios:** success order/count, repeated source keys, empty source/lookup, missing at
  first/middle/last, duplicate used/unused, duplicate plus missing error order, null lookup/source
  keys, non-default equality, prebuilt unique index, combine ledger, single enumeration, argument
  nullness. `(happy path)`, `(edge)`, `(error path)`.
- **Verification:** Focused `JoinTests`; shared comparison.

### U4. Add the streaming exactly-one join

- **Goal:** Add `JoinExactlyOneResults` over a prebuilt many index with honest per-row outcomes.
- **Requirements:** R9, R10, R12, R14.
- **Dependencies:** U1.
- **Files:** `src/FunnySharp/LookupExtensions.cs`.
- **Approach:** A non-iterator public method validates arguments and calls a private iterator. The
  iterator enumerates the source once, reads each key, yields a success with `combine`, a
  `MissingKey` failure, a `NullKey` failure, or a `DuplicateKey` failure carrying the source row and
  both lookup positions. It does not re-enumerate or validate the lookup index.
- **Execution note:** Yield failures as values, never throw for data. Do not buffer outcomes. The
  index snapshot is reused across enumerations; the source is enumerated again each time.
- **Patterns to follow:** `Choose`/`Scan` deferred-iterator wording; `EnumerablePipelineExtensions`
  argument-validation wrappers.
- **Test scenarios:** one outcome per row, later missing after successes, duplicate matched vs
  unused, null source key, empty source, deferred start, repeated enumeration, early disposal,
  combine ledger, no lookup re-enumeration. `(happy path)`, `(edge)`, `(error path)`.
- **Verification:** Focused `JoinStreamingTests`; shared streaming comparison.

### U5. Pin semantics with the xUnit suite

- **Goal:** Add focused tests for every contract edge and the shared comparison test.
- **Requirements:** R1-R14.
- **Dependencies:** U1-U4.
- **Files:** `tests/FunnySharp.Tests/LookupIndexTests.cs`, `JoinTests.cs`,
  `JoinStreamingTests.cs`, `JoinComparisonTests.cs`, `FunnySharp.Tests.csproj`.
- **Approach:** Attribute-only `[Fact]`/`[Theory]`; classic asserts; no skips. Use a small counting
  `JoinProbe<T>` for enumeration/disposal ledgers. Use fixed expected failures for key locations.
  The comparison test calls the shared verifier once.
- **Execution note:** No sleeps or timing asserts. Nullability suppressions only where the test
  deliberately violates the `notnull` annotation, with the repository's existing patterns.
- **Patterns to follow:** `FallibleReductionTests`/`SummaryTests` structure; `ProbeEnumerable.cs`
  counting conventions.
- **Test scenarios:** AE1-AE10 plus null arguments, comparer exceptions, mutable value visibility,
  repeated calls, empty-string keys, and explicit expansion via `GetMany`. `(happy path)`,
  `(edge)`, `(error path)`, `(integration)`.
- **Verification:** Focused run zero failed and zero skipped; full suite green.

### U6. Build the shared comparison, probes, and measurement harness

- **Goal:** Prove outcome parity, fixed failure locations, consumption counts, and costs.
- **Requirements:** R15, R16.
- **Dependencies:** U1-U5.
- **Files:** `tests/Shared/JoinComparison/JoinModels.cs`, `JoinFixtures.cs`,
  `OrdinaryJoinConsumer.cs`, `HandIndexedJoinConsumer.cs`, `FunnySharpJoinConsumer.cs`,
  `JoinComparison.cs`, `tests/Shared/FallibleReduction/ReductionProbes.cs`,
  `tests/Shared/ValidationUnwrap.cs`,
  `tests/FunnySharp.JoinMeasurements/FunnySharp.JoinMeasurements.csproj`, `Program.cs`,
  `JoinMeasurements.cs`, `packages.lock.json`, `FunnySharp.slnx`.
- **Approach:** Shared fixtures cover empty, unique success, repeated source keys, missing,
  duplicate used, duplicate unused, case-insensitive duplicate, ordinal contrast, empty-string key,
  empty lookup, and combined duplicate+missing. The hand-indexed consumer is the complete safe
  reference; the ordinary consumer is the idiomatic LINQ form that throws on duplicates/missing.
  The comparison asserts fixed expected reports, hand/FunnySharp parity, success-path ordinary
  parity, failure-path ordinary exceptions, streaming per-row parity, the unused-duplicate scope
  difference, and exact enumeration/disposal ledgers. The measurement executable runs the
  comparison first, then measures index construction, buffered success, missing error storage,
  duplicate error storage, streaming collection, and empty input at N=1024 with the shared harness.
- **Execution note:** Do not share one mutable error list between consumers. Do not weaken the probe
  to count only `combine` calls; source `MoveNext` counts are the acceptance-relevant comparison.
  Measurement is explicit-request evidence, not a CI gate.
- **Patterns to follow:** `SummaryComparison` verifier/probe structure;
  `Shared/MeasurementHarness.cs`.
- **Test scenarios:** all fixtures; fixed locations; non-default equality; unused duplicate;
  streaming later failure; single enumeration. `(integration)`.
- **Verification:** Comparison passes in xUnit and measurement; Release measurement run prints
  allocation and timing tables.

### U7. Add the compiling example and documentation samples

- **Goal:** Show the whole workflow and provide byte-exact guide snippets.
- **Requirements:** R17.
- **Dependencies:** U1-U4.
- **Files:** `examples/FunnySharp.Examples/JoinExamples.cs`,
  `examples/FunnySharp.Examples/Program.cs`,
  `examples/FunnySharp.DocumentationSamples/CardinalityJoinSamples.cs`.
- **Approach:** The example builds a unique index with `StringComparer.OrdinalIgnoreCase`, joins a
  valid order set, demonstrates duplicate and missing failures with positions, shows
  `GetZeroOrOne`/`GetMany`, and collects streaming results containing a later missing failure. The
  documentation sample provides the snippets used by the guide: unique index construction, buffered
  join with error handling, and per-row streaming. All outcomes are consumed through `Match`/
  `TryGet...`; the analyzer build stays clean.
- **Execution note:** The example project must build with zero `FS####` diagnostics. Do not discard
  a `Result`/`Validation`. Keep the snippet source and guide byte-identical.
- **Patterns to follow:** `FallibleReductionExamples`, `SummaryExamples`, and
  `OnePassSummarySamples` registration/region shape.
- **Test scenarios:** the example prints the exact `FunnySharp examples passed.` marker; the docs
  verifier compares every new snippet. `(integration)`.
- **Verification:** `dotnet run --project examples/FunnySharp.Examples -c Release`;
  `verify-docs-snippets`.

### U8. Reconcile docs, baseline, manifests, and goal state

- **Goal:** Ship coherent documentation and committed surface records.
- **Requirements:** R17, R18.
- **Dependencies:** U1-U7.
- **Files:** `docs/cardinality-joins.md`, `docs/cardinality-joins-evidence.md`,
  `docs/grammar.md`, `docs/product-contract.md`, `docs/stability-inventory.md`,
  `docs/performance.md`, `docs/collections.md`, `docs/data-pipelines.md`, `docs/AGENTS.md`,
  `docs/plans/AGENTS.md`, `docs/goals/README.md`, `docs/goals/AGENTS.md`,
  `docs/goals/archive/0035-goal.md`, `AGENTS.md`, `README.md`, `README_zh-cn.md`,
  `src/FunnySharp/AGENTS.md`, `tests/FunnySharp.Tests/AGENTS.md`,
  `examples/FunnySharp.Examples/AGENTS.md`,
  `examples/FunnySharp.DocumentationSamples/AGENTS.md`,
  `eng/api-baseline/FunnySharp.public-api.txt`, `eng/performance/baseline.json`,
  `eng/performance/competitor-baseline.json`, `eng/harness/DocsSnippets.fs`,
  `tests/FunnySharp.Harness.Tests/DocsSnippetsTests.fs`.
- **Approach:** The guide starts with `## API Shape`, defines the index/lookup/join surfaces,
  states key/comparer/null/duplicate/unmatched behavior and the uniqueness-scope table, contrasts
  whole and streaming shapes, covers order/count/no-expansion/lifetime, links the evidence, and
  closes with `## Deliberate Boundaries`. Grammar adds the family and reconciles the old
  "no duplicate-sensitive builder" absence. Product contract gains one stable-surface paragraph.
  Stability inventory adds the family row. Performance adds characteristic rows and the evidence
  exclusion. The docs guide joins the primary-guide list and the snippet corpus; the hardcoded
  snippet count and guide count in the harness tests and AGENTS files are updated. The API baseline
  is regenerated with `--write` after the surface is final. Both performance manifests add the new
  source paths in ordinal order. Goal 0035 moves unchanged to the archive and the index removes it
  from Next-Stage.
- **Execution note:** Do not hand-edit generated inventory, archived goal contents, or historical
  release evidence. Archive is a rename of the unchanged file. Do not modify Goal 0036.
- **Patterns to follow:** Goal 0033/0034 landing shape (guide/evidence/example/baseline/manifests/
  archive/index).
- **Test scenarios:** API baseline, docs snippets, format, manifest shape, example run, full test
  gate. `(integration)`.
- **Verification:** `verify-api-baseline`, `verify-docs-snippets`, `format`, full `test`.

## Verification Contract

| Gate | Command | Applies to | Completion evidence |
| --- | --- | --- | --- |
| Build | `dotnet fsi build.fsx -- -p build` | U1-U8 | exit 0, zero warnings/errors |
| Test | `dotnet fsi build.fsx -- -p test` | U1-U8 | all tests passed, zero failed, zero skipped |
| Format | `dotnet fsi build.fsx -- -p format` | U1-U8 | exit 0 |
| Docs snippets | `dotnet fsi build.fsx -- -p verify-docs-snippets` | U7, U8 | exit 0 with the new guide and snippets in the corpus |
| API baseline | `dotnet fsi build.fsx -- -p verify-api-baseline` (after `--write`) | U8 | exit 0 with only the new members |
| Example | `dotnet run --project examples/FunnySharp.Examples/FunnySharp.Examples.csproj -c Release` | U7 | exact `FunnySharp examples passed.` |
| Comparison and measurement | `dotnet run --project tests/FunnySharp.JoinMeasurements/FunnySharp.JoinMeasurements.csproj -c Release` | U6, U7 | comparison passes; index/output/error allocation and timing tables captured |
| Manifest shape | full test suite (`PerformanceManifestTests`) | U8 | source lists sorted and complete |

## Definition of Done

### Per-Unit Completion

| Unit | Done when |
| --- | --- |
| U1 | Index construction, source-order grouping, all three keyed reads, null-key handling, comparer exposure, and error positions pass focused tests; one source enumeration is pinned. |
| U2 | Complete-input uniqueness reports every duplicate occurrence with both positions; null plus duplicate order is deterministic; the unique wrapper exposes only the promised reads. |
| U3 | Both buffered joins return exact order/count or no output plus all lookup/source errors; duplicate keys never expand outputs; combine follows the documented ledger. |
| U4 | The streaming join is deferred, yields one outcome per row, reports matched-key ambiguity with row/lookup positions, and does not claim complete-input validation. |
| U5 | Focused tests pass with zero failed and zero skipped; the full suite remains green. |
| U6 | The comparison proves fixed failure positions, hand/FunnySharp parity, ordinary success parity and failure behavior, unused-duplicate scope, and exact consumption ledgers; the measurement run prints real costs. |
| U7 | The example prints the exact success marker; the guide snippets byte-match the documentation sample regions. |
| U8 | The guide closes with `## Deliberate Boundaries`; product contract, grammar, inventory, performance, baseline, manifests, READMEs, and goal archive agree; Goal 0036 is untouched. |

### Global Completion

- R1-R18 each map to an implementation artifact and an executed verification result.
- AE1-AE12 pass in the test suite, the comparison, or the evidence run.
- The evidence records empty and repeated keys, unmatched rows, ambiguous matches, non-default
  equality, single enumeration, real failure locations, and complete idiomatic-C# and hand-indexed
  comparisons with index, output, and error-storage costs.
- The evidence records the maintainer judgment on whether the safer relation contract materially
  simplifies complete application code, including a sceptical boundary if the added index
  validation outweighs the benefit for simple happy-path lookups.
- The whole-versus-streaming guarantee table is explicit and no shape claims the other's guarantee.
- No new carrier, runtime dependency, SQL/ORM/DataFrame layer, database-integrity claim, implicit
  Cartesian join, hidden comparative policy, or Goal 0036 behavior is introduced.
