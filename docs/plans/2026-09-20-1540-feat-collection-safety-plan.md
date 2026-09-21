---
title: Collection Safety And Expressiveness (Goal 17) - Plan
type: feat
date: 2026-09-20
deepened: 2026-09-20
artifact_contract: ce-unified-plan/v1
product_contract_source: docs/goals/archive/0017-goal.md
execution: code
---

# Collection Safety And Expressiveness (Goal 17) - Plan

## Goal Capsule

- **Objective:** FunnySharp callers can express total cardinality-sensitive operations, non-empty
  guarantees, selection and partitioning, fold and scan, exact versus truncating combination, and
  Option/Result/UnitResult/Validation traversal with index, key, item-name, property-path, and
  nested location context on standard .NET collections and sequence types, without a custom
  collection hierarchy.
- **Means:** Add the adopted Goal-17 decision surface (decision record E44/E56/E62/E69/E70/E72/E73,
  AD-5, G5/G6): the `*OrNone` cardinality family, `NonEmpty<T>` + `ToNonEmptyOrNone`,
  `Partition` (predicate/Option/Result/UnitResult), `ZipExact`/`ZipExactOrNone`, container
  Try->Option bridges, common `Parse*OrNone` bridges over `IParsable<T>`, `WhereNotNull`, and
  traversal context overloads (indexed, keyed, located) plus the experimental `Location` carrier.
- **Authority:** `docs/goals/archive/0017-goal.md` is the completion contract; `docs/product-contract.md`
  fixes the BCL-first boundaries; `docs/next-stage/decision-record.md` and
  `docs/next-stage/api-decisions.md` record the adopted/rejected decisions; `docs/grammar.md` is
  the verb table to extend.
- **Execution profile:** Source first (disjoint files), then tests, then examples/docs/benchmarks,
  then full verification. Span/Memory paths stay untouched and stay honestly synchronous.
- **Tail ownership:** Implementation owns code, tests, docs, examples, benchmark code, and local
  evidence runs. Release-candidate regeneration (`eng/performance/baseline.json`, Run-Release) is
  the separately authorized release path.
- **Stop conditions:** Stop for a new decision if completion would require a custom collection
  hierarchy, an async span/memory bridge, a second absence/error carrier, or a vocabulary change to
  existing 0.1.0 members.

---

## Adopted Surface (frozen signatures)

### Cardinality (`CardinalityExtensions.cs`)

| Member | Contract |
| --- | --- |
| `FirstOrNone()` / `FirstOrNone(predicate)` | `Option<T>`; `None` iff no element matches. Eager, single enumeration, source order. |
| `LastOrNone()` / `LastOrNone(predicate)` | `Option<T>`; `None` iff no element matches. |
| `SingleOrNone()` / `SingleOrNone(predicate)` | `Option<T>`; `Some` iff exactly one element (match); the empty-versus-multiple collapse is documented and `ToNonEmptyOrNone` distinguishes the cases. |
| `ElementAtOrNone(index)` | `Option<T>`; `None` for any out-of-range index (negative or >= count). |
| `MinOrNone()` / `MinOrNone(comparer)` | `Option<T>`; BCL `Min` null semantics (nulls skipped; all-null/empty -> `None`). |
| `MaxOrNone()` / `MaxOrNone(comparer)` | `Option<T>`; same null semantics. |

### Non-empty guarantee (`NonEmpty.cs`)

`NonEmpty<T>` readonly struct: `First`, `Rest` (`IReadOnlyList<T>`), `Count`,
seedless `Aggregate(Func<T,T,T>)` (cannot throw empty; BCL parity name), `ToReadOnlyList()`.
Default state throws `InvalidOperationException` like the other carriers. Created only through
`ToNonEmptyOrNone()` (`Option<NonEmpty<T>>`, `None` iff empty). Not a collection: no
`IEnumerable` implementation, no indexer.

### Partition (`PartitionExtensions.cs`)

- `source.Partition(predicate)` -> `Partition<T>` (`True`/`False` lists + deconstruct); single
  enumeration, order preserved within each side, eager.
- `options.Partition()` -> `OptionPartition<T>` (`Somes`/`Nones`).
- `results.Partition()` -> `ResultPartition<TValue, TError>` (`Passed` values /`Failed` errors).
- `unitResults.Partition()` -> `UnitResultPartition<TError>` (`Succeeded` count / `Failed` errors).
- Async: predicate + Result partitions only (`PartitionAsync`, E88: capability set, not every
  overload).

### Exact combination (`ExactZipExtensions.cs`)

- `first.ZipExactOrNone(second)` -> `Option<IReadOnlyList<(TFirst First, TSecond Second)>>`;
  `None` iff lengths differ.
- `first.ZipExact(second, lengthError)` -> `Result<..., TError>` where
  `lengthError(firstCount, secondCount)` runs only on unequal lengths; both sequences fully
  enumerated once; counts distinguish which side was longer.

### Container and parse bridges (`ContainerExtensions.cs`, `ParseExtensions.cs`)

- `Queue<T>.DequeueOrNone/PeekOrNone`, `Stack<T>.PopOrNone/PeekOrNone`,
  `PriorityQueue<TElement,TPriority>.DequeueOrNone/PeekOrNone` (empty -> `None`; mutating members
  mutate only on `Some`), `IList<T>.IndexOfOrNone(item)` (-1 -> `None`),
  `Dictionary<TKey,TValue>.RemoveOrNone(key)` -> `Option<TValue>`.
- `ParseOrNone<T>(string[, provider])` over `IParsable<T>` plus 16 common named bridges
  (`ParseBoolOrNone` ... `ParseGuidOrNone`), each delegating to the generic bridge.

### WhereNotNull (`EnumerablePipelineExtensions.cs`, `AsyncEnumerablePipelineExtensions.cs`)

Deferred single-pass filter of nulls, class and struct overloads on both sequence carriers.

### Traversal context (`SequenceExtensions.cs`, `AsyncSequenceExtensions.cs`)

- Indexed: `Traverse(source, (index, item) => carrier)` for the four carriers.
- Keyed (dictionary shape preserved): `Traverse(IReadOnlyDictionary<K,V>, (key, value) => carrier)`
  -> `carrier<IReadOnlyDictionary<K,R>>` for the four carriers.
- Located (experimental, `[Experimental("FS0017")]`): `Traverse(source, Location root,
  (location, item) => carrier)` and `Traverse(dict, Location root, (location, key, value) =>
  carrier)` for the four carriers; the traversal composes `root.At(index)` / `root.Key(key)` per
  item so nested traversals produce `customers[17].addresses[2].postalCode` compositionally.
- Async: located `TraverseAsync` (sync selector) and `TraverseValueAsync` (ValueTask selector) for
  the four carriers; indexed/keyed/dictionary async mirrors are deliberately not mirrored (E88).

### Location (`Location.cs`, experimental `FS0017`)

Value-based, immutable path carrier: `Root`, `At(index)` -> `[17]`, `Key(string)` ->
`["alice"]`, `Key(object)` -> `[key]`, `Property(name)` -> `.name` (bare at start),
`Nest(inner)` composition, cached `ToString()`, segment-wise value equality. Guards:
`At` rejects negative indexes; `Property`/`Key` reject null/empty. Not a collection or error
hierarchy.

## Deliberate Non-Goals (decided once here per E57)

- `Inspect` rejected (duplicates `Tap`); `Pairwise`/`SlidingWindow` rejected for this goal: no
  capability in the Goal 17 completion list needs adjacent-pair or window shapes, and the product
  contract forbids speculative feature APIs; `ToLookup`/`GroupBy` already distinguish duplicates.
- Async `ZipExact`, async indexed/keyed traversal mirrors, `AverageOrNone` towers, span parse
  bridges, concurrent containers: not mirrored this round; recorded as exclusions with rationale.
- Fold stays BCL `Aggregate`; `NonEmpty<T>.Aggregate` is the only seedless fold that cannot throw
  for emptiness.

## Verification Plan

1. xUnit v3 tests: `CardinalityOrNoneTests`, `NonEmptyTests`, `PartitionTests`,
   `ZipExactTests`, `ContainerBridgeTests`, `ParseOrNoneTests`, `LocationTests`,
   `TraversalContextTests`, `AsyncCollectionTests` — edge (empty/singleton/multiple/unequal/
   large-input 100k), law, and enumeration (single-pass, disposal, deferred) cases.
2. Data-cleaning and batch-validation examples in `examples/FunnySharp.Examples`.
3. Docs: new `docs/collections.md`, grammar.md verb rows, README section,
   `docs/stability-inventory.md` with the `FS0017` entry.
4. Benchmarks: `CollectionBenchmarks` comparing each family with equivalent LINQ/loops/BCL
   operations; local run recorded in the goal evidence; release regeneration stays with the
   release gate.
