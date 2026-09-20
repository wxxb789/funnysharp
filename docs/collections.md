# Collections

FunnySharp keeps standard .NET collections and sequence types as the ecosystem: `List<T>`,
arrays, `IReadOnlyList<T>`, `IReadOnlyDictionary<TKey, TValue>`, `IEnumerable<T>`,
`IAsyncEnumerable<T>`, and span/memory views. This guide covers the Goal 17 additions that make
collection programming safer and more expressive on those carriers:
[cardinality](#cardinality), the [non-empty guarantee](#the-non-empty-guarantee),
[selection and partitioning](#selection-and-partitioning),
[exact versus truncating combination](#exact-versus-truncating-combination),
[container and parse bridges](#container-and-parse-bridges), and
[traversal context](#traversal-context). A custom collection hierarchy is deliberately excluded
([product contract](product-contract.md)).

Every operation below states its ordering, short-circuiting, accumulation, allocation,
buffering, disposal, and cancellation behavior; the
[carrier behavior matrix](#carrier-behavior-matrix) summarizes them. Span and memory operations
stay exactly as [data pipelines](data-pipelines.md) defines them — immediate, caller-owned
storage, never deferred, and never falsely unified with asynchronous streams — and no Goal 17
operation spans an `await` boundary or returns a span.

## Cardinality

The `*OrNone` family replaces the `*OrDefault` null ambiguity: absence is a value, and each
member's `None` has exactly one meaning.

| Member | `Some` | `None` |
| --- | --- | --- |
| `source.FirstOrNone()` | the first item | the source is empty |
| `source.FirstOrNone(predicate)` | the first matching item | no item matches |
| `source.LastOrNone()` | the last item | the source is empty |
| `source.LastOrNone(predicate)` | the last matching item | no item matches |
| `source.SingleOrNone()` | exactly one item exists | the source is empty **or** has multiple items |
| `source.SingleOrNone(predicate)` | exactly one item matches | no item or multiple items match |
| `source.ElementAtOrNone(index)` | the item at `index` | `index` is out of range (negative or beyond the end) |
| `source.MinOrNone()` / `MaxOrNone()` | the smallest/largest non-null item | the source is empty or every item is null |
| `source.MinOrNone(comparer)` / `MaxOrNone(comparer)` | the smallest/largest non-null item under the comparer | same |

Contracts:

- Eager; the source is enumerated exactly once; source order is followed; arguments are validated
  eagerly; implementations are iterative, so large inputs cannot overflow.
- `FirstOrNone(predicate)` invokes the predicate once per item in source order and stops at the
  first match. `LastOrNone` and `SingleOrNone` predicates run once per item because the total
  cardinality decides the answer; `SingleOrNone` stops reading once a second match is seen.
- `ElementAtOrNone` treats any out-of-range index — negative or beyond the end — as absence,
  matching `ElementAtOrDefault`'s absence behavior without its default-value ambiguity.
- `MinOrNone`/`MaxOrNone` follow BCL `Min`/`Max` null semantics for .NET 10: null items of a
  nullable reference type are skipped, a source that is empty or all-null is `None`, and the
  comparer never sees a null item.
- `SingleOrNone` collapses empty and multiple into `None` (the at-most-one idiom). Empty,
  singleton, and multiple stay distinguishable through the
  [non-empty guarantee](#the-non-empty-guarantee): `source.ToNonEmptyOrNone()` is `None` exactly
  when the source is empty, and `rest.Count == 0` on the non-empty value distinguishes a
  singleton from multiple items. The distinction is deliberate and documented instead of
  collapsed into convenient but lossy absence.

Async mirrors on `IAsyncEnumerable<T>`: `FirstOrNoneAsync`, `LastOrNoneAsync`,
`SingleOrNoneAsync`, `ElementAtOrNoneAsync`, `MinOrNoneAsync`, `MaxOrNoneAsync`, and
`ToNonEmptyOrNoneAsync` mirror the capability set without the predicate/comparer overload
towers (decision E88). They are eager awaitables, enumerate the source once, forward the
caller's `CancellationToken` to the enumerator, and never convert cancellation into absence or an
error — cancellation stays an exception.

## The Non-Empty Guarantee

`ToNonEmptyOrNone` converts a sequence into `Option<NonEmpty<T>>`: `Some` proves at least one
item exists; `None` means the sequence was empty.

<!-- documentation-sample: DocumentationSamples.Collections.NonEmptyAggregate -->
```csharp
Option<NonEmpty<int>> amounts = quantities.ToNonEmptyOrNone();
int total = amounts.Match(
    nonEmpty => nonEmpty.Aggregate(static (left, right) => left + right),
    () => 0);
```

`NonEmpty<T>` is a guarantee, not a collection:

- `First` — the first item, non-default by construction.
- `Rest` — the remaining items in source order, as an `IReadOnlyList<T>` that is empty for a
  singleton.
- `Count` — `1 + Rest.Count`; `Rest.Count == 0` distinguishes a singleton from multiple.
- `Aggregate(func)` — the seedless left fold that **cannot** encounter emptiness: BCL
  `Enumerable.Aggregate` throws `InvalidOperationException` on an empty sequence, while this fold
  cannot because the type carries the proof. Folding with a seed stays BCL `Aggregate` —
  FunnySharp never duplicates it. Delegate exceptions propagate unchanged.
- `ToReadOnlyList()` — materializes one fresh ordered list; the allocation is disclosed.

`NonEmpty<T>` implements no `IEnumerable` and no indexer, so it never becomes a collection
hierarchy. The only public constructor path is `ToNonEmptyOrNone` /
`ToNonEmptyOrNoneAsync`; `default(NonEmpty<T>)` is an uninitialized value that throws on
access, like the other FunnySharp carriers
([default states](option.md), Goal 15 mechanism). The guarantee is about cardinality, not
nullness: a `NonEmpty<string?>.First` may still be a null element.

## Selection and Partitioning

Selection is the cardinality family above. `Partition` splits a sequence in one enumeration into
meaning-bearing results; two `Where` passes would enumerate twice.

| Member | Result | Members |
| --- | --- | --- |
| `source.Partition(predicate)` | `Partition<T>` | `True`, `False` |
| `options.Partition()` | `OptionPartition<T>` | `Somes` (values), `Nones` (count) |
| `results.Partition()` | `ResultPartition<TValue, TError>` | `Passed` (values), `Failed` (errors) |
| `unitResults.Partition()` | `UnitResultPartition<TError>` | `Succeeded` (count), `Failed` (errors) |

<!-- documentation-sample: DocumentationSamples.Collections.Partition -->
```csharp
var (shippable, cancelled) = orders.Partition(order => order.IsPaid);
```

Contracts: eager; single enumeration; source order preserved within each side; arguments
validated eagerly; predicate exceptions propagate unchanged and an already-appended prefix is not
rolled back. `OptionPartition` and `UnitResultPartition` count their valueless side because
absence and unit success have no value to list. `ResultPartition` keeps the errors of failed
results, not their values — the error carries what the caller needs (decision E62/F5.12).

Async: `PartitionAsync` mirrors the predicate and Result forms only (decision E88), forwarding
the caller's token to the enumerator through `WithCancellation`; cancellation and source faults
flow through normal `await foreach` behavior without wrapping.

Duplicates stay on BCL operations: `ToLookup`/`GroupBy` group duplicate keys without loss and
`DistinctBy` removes them; FunnySharp adds no duplicate-sensitive dictionary builder in this
goal.

## Exact Versus Truncating Combination

BCL `Enumerable.Zip` truncates to the shorter sequence. The exact forms make unequal lengths a
distinguishable outcome instead of a silent drop:

<!-- documentation-sample: DocumentationSamples.Collections.ZipExact -->
```csharp
Option<IReadOnlyList<(string First, int Second)>> paired = skus.ZipExactOrNone(quantities);

Result<IReadOnlyList<(string First, int Second)>, string> strict = skus.ZipExact(
    quantities,
    (skuCount, quantityCount) => $"skus ({skuCount}) and quantities ({quantityCount}) differ");
```

- `ZipExactOrNone` is `Some` with the pairs in order only when both sequences have equal
  lengths; `None` can only mean unequal lengths.
- `ZipExact` succeeds identically, or fails with `lengthError(firstCount, secondCount)`. The
  delegate receives both total counts, so which side was longer stays observable; it is validated
  eagerly and invoked only on unequal lengths.
- Both forms enumerate each source exactly once and dispose both enumerators on every path.
  Reporting exact totals requires finishing the longer side after the shorter one ends, so the
  unequal-length path reads both sequences to their end; the equal path stops when both end
  together. Delegate exceptions propagate unchanged.

## Container and Parse Bridges

Container Try-pattern operations and parse failures become options instead of sentinel values
(`-1`, `InvalidOperationException`, `TryX` + `if` ceremony). Keyed dictionary lookups keep
`GetOption` and value conversions keep `ToOption`; the members below use the `*OrNone`
cardinality suffix.

| Member | `None` means |
| --- | --- |
| `queue.DequeueOrNone()` / `queue.PeekOrNone()` | the queue is empty (dequeues only when `Some`) |
| `stack.PopOrNone()` / `stack.PeekOrNone()` | the stack is empty (pops only when `Some`) |
| `priorityQueue.DequeueOrNone()` / `priorityQueue.PeekOrNone()` | the priority queue is empty |
| `list.IndexOfOrNone(item)` | the item is not in the list (`IList<T>`, including `List<T>` and `ImmutableList<T>`) |
| `dictionary.RemoveOrNone(key)` | the key is absent (removes and returns the value in one operation when `Some`) |

Parse bridges never throw for parse failures; a null input string is still a programming error.
The generic `IParsable<T>` bridge is canonical, and the named common set delegates to it:

<!-- documentation-sample: DocumentationSamples.Collections.ParseBridges -->
```csharp
Option<int> quantity = text.ParseIntOrNone();
Option<DateTime> stamped = text.ParseOrNone<DateTime>(CultureInfo.InvariantCulture);
```

Named bridges: `ParseBoolOrNone`, `ParseByteOrNone`, `ParseSByteOrNone`, `ParseShortOrNone`,
`ParseUShortOrNone`, `ParseIntOrNone`, `ParseUIntOrNone`, `ParseLongOrNone`,
`ParseULongOrNone`, `ParseFloatOrNone`, `ParseDoubleOrNone`, `ParseDecimalOrNone`,
`ParseDateTimeOrNone`, `ParseDateTimeOffsetOrNone`, `ParseTimeSpanOrNone`,
`ParseGuidOrNone` — each parses with the current culture, exactly like `T.Parse`; pass an
`IFormatProvider` through the generic overload for explicit cultures. Exceptions thrown by a
custom `IParsable<T>` implementation propagate unchanged.

`WhereNotNull` completes the deferred family: a single-pass, deferred filter of nulls on
`IEnumerable<T?>` and `IAsyncEnumerable<T?>` (class and struct overloads), with the same
per-enumeration contract as `Choose` and `Scan` — the source is enumerated once per consumer
enumeration and nothing is cached.

## Traversal Context

`Sequence`/`Traverse` keep their fail-fast (`Option`, `Result`, `UnitResult`) versus
accumulate (`Validation`) split on every carrier. Goal 17 adds three context families so
traversal failures retain **where** they happened instead of losing it:

- **Indexed**: `source.Traverse((index, item) => ...)` passes each reached item's zero-based
  index. The index counts reached items; a fail-fast carrier stops at the first failure.
- **Keyed**: `dictionary.Traverse((key, value) => ...)` traverses the values of an
  `IReadOnlyDictionary<TKey, TValue>` and produces
  `carrier<IReadOnlyDictionary<TKey, TResult>>` on success — the dictionary shape is preserved
  instead of collapsing to a list. Errors accumulate in source enumeration order.
- **Located** (experimental, `FS0017`; suppress with `#pragma warning disable FS0017`):
  `source.Traverse(root, (location, item) => ...)` passes each item's composed `Location`. The
  caller names each level once; the traversal threads the per-item context, so a nested
  validation produces `customers[17].addresses[2].postalCode` compositionally:

<!-- documentation-sample: DocumentationSamples.Collections.LocatedTraverse -->
```csharp
#pragma warning disable FS0017

Validation<IReadOnlyList<CustomerRecord>, ImportError> validated = customers.Traverse(
    Location.Root.Property("customers"),
    (customerLocation, customer) => customer.Addresses
        .Traverse(customerLocation.Property("addresses"), ValidateAddress)
        .Map(addresses => new CustomerRecord([.. addresses])));

static Validation<AddressRecord, ImportError> ValidateAddress(
    Location addressLocation, AddressRow address) =>
    ParsePostalCode(address.PostalCode)
        .MapErrors(error => error.At(addressLocation.Property("postalCode")))
        .Map(postalCode => new AddressRecord(postalCode));
```

Each level contributes only its own names: the outer root names `customers`, the inner root
names `.addresses` under the composed `customers[17]`, and the innermost error decorator names
`.postalCode`. The traversal threads every index and nesting level, so the accumulated failure
carries `customers[17].addresses[2].postalCode` without application code assembling it.
`ImportError.At` above is the caller's own domain error decorating itself with a `Location`;
FunnySharp never attaches a location to a caller-typed error.

Async: located `TraverseAsync` (synchronous selector) and `TraverseValueAsync` (`ValueTask`
selector) mirror the four carriers, forward the caller's token to the enumerator, and await each
`ValueTask` exactly once. The indexed and keyed families are deliberately not mirrored to async
carriers this round (decision E88).

### Location

`Location` is a small, immutable, value-equal path carrier
([experimental](stability-inventory.md), `FS0017`):

| Member | Renders |
| --- | --- |
| `Location.Root` | the empty string |
| `location.At(index)` | `[17]` |
| `location.Key("id")` | `["id"]` (string keys are quoted, so `["17"]` stays distinct from `[17]`) |
| `location.Key(key)` | `[key]` through the key's string representation |
| `location.Property(name)` | `.name`, or the bare `name` as the first segment |
| `outer.Nest(inner)` | the outer path followed by the inner path |

Guards: `At` rejects negative indexes; `Property`/`Key` reject null and empty names/keys.
Equality is segment-wise, so an index is never equal to a key. Nesting is the composition rule
for traversal context: the outer level nests the inner level's locations under the outer item's
location, and each level names only its own segments. The rendered path is cached after the first
`ToString` call.

## Carrier Behavior Matrix

| Capability | Ordering | Short-circuiting | Accumulation | Allocation | Buffering | Disposal | Cancellation |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `*OrNone` cardinality (sync) | source order | first match stops the predicate | none | one option per call; no list | none | enumerator disposed on every path | n/a (synchronous) |
| `*OrNoneAsync` | source order | first match | none | one option | none | enumerator disposed | caller token forwarded to the enumerator; cancellation is an exception, never absence |
| `ToNonEmptyOrNone`(`Async`) | source order | none | none | one list + one wrapper | materializes the items once | enumerator disposed | async form forwards the token |
| `Partition`/`PartitionAsync` | source order per side | none | both sides materialized | two lists + one result | materializes | enumerator disposed | async form forwards the token |
| `ZipExact`/`ZipExactOrNone` | source order | equal path stops when both end | none | one pair list on success | pairs materialized on success | both enumerators disposed on every path | n/a (synchronous) |
| Container/parse bridges | n/a | n/a | none | one option | none | n/a | n/a |
| `WhereNotNull` | source order | none | none | deferred; none until enumeration | none | per-enumeration disposal | async form: consumer enumeration cancellation |
| Indexed/keyed `Traverse` | source order / dictionary order | fail-fast carriers stop at first failure; `Validation` reads everything | `Validation` accumulates every error in order | result list or dictionary on success | materializes on success | enumerator disposed | n/a (synchronous) |
| Located `Traverse`/`TraverseAsync` | source order | same fail-fast/accumulate split | same split | result list plus one `Location` per item | materializes on success | enumerator disposed | token forwarded to the enumerator; never inspected eagerly |

## Deliberate Exclusions (decided once, this goal)

| Exclusion | Rationale |
| --- | --- |
| `Inspect`, `Pairwise`, `SlidingWindow` | `Inspect` duplicates `Tap`; no Goal 17 capability needs adjacent-pair or window shapes, and the product contract forbids speculative feature APIs (decision E57 is decided here). |
| `AverageOrNone` towers | Overload towers per numeric type (decision E70); average-with-emptiness composes from `Choose` + BCL `Average` when a consumer appears. |
| Async `ZipExact`, async indexed/keyed traversal mirrors | Decision E88: adopt the capability set on async carriers, not every overload; the exact-combination safety story is the synchronous data-cleaning path. |
| Concurrent container bridges | `ConcurrentQueue`/`ConcurrentStack`/`ConcurrentDictionary` already expose Try-pattern members; wrapping them adds no safety (outside the adopted container list). |
| Span parse bridges (`ISpanParsable`) | The generic string bridge is canonical this round; span parsing stays a direct BCL call. |
| Duplicate-key dictionary building | BCL `ToLookup`/`GroupBy` already distinguish duplicates without loss. |
| A second fold | BCL `Aggregate`/`AggregateAsync` is canonical; only `NonEmpty<T>.Aggregate` adds the cannot-be-empty guarantee. |
| A custom collection hierarchy | Excluded by the goal and the product contract; every operation above stays on BCL carriers. |

## Performance Evidence

`CollectionBenchmarks` compares each family with equivalent LINQ, loops, and BCL collection
operations:

- `*OrNone` cardinality with `FirstOrDefault` + null checks, the two-pass `Single` idiom, and
  the nullable-`Min` idiom.
- `Partition` with two `Where` passes.
- `ToNonEmptyOrNone` + `Aggregate` with the `Any` + `Aggregate` double-enumeration idiom.
- `ZipExactOrNone` with `Zip` + count checks.
- `DequeueOrNone` with `TryDequeue` + `if`, `ParseIntOrNone` with `int.TryParse` + `if`,
  `WhereNotNull` with `Where(v => v is not null)`, and the located `Traverse` with the
  hand-written index-tracking loop it replaces.

Run the focused benchmark with:

```shell
dotnet run --project benchmarks/FunnySharp.Benchmarks/FunnySharp.Benchmarks.csproj --configuration Release -- --filter '*CollectionBenchmarks*'
```

ShortRun results are directional and should be rerun on deployment hardware before capacity
decisions; the release tables are generated from the approved observation in
`eng/performance/baseline.json` by the release gate
([release readiness](release-readiness.md)), which this guide does not modify.

Local observation (2026-09-20, Linux x64, .NET SDK 10.0.400 / runtime 10.0.11, BenchmarkDotNet
v0.15.8 ShortRun, 40 benchmarks over `Count=16` and `Count=1024`): at `Count=1024`, `Partition`
ran at ≈0.47x of two `Where` passes, `MinOrNone` at ≈0.05x of the `Cast<int?>().Min()` idiom
with zero allocation against its boxing, and `ParseIntOrNone` at ≈0.85x of `int.TryParse` +
`if`; `FirstOrNone` ran at ≈1.26x with zero allocation, `WhereNotNull` at ≈0.97x, `ZipExactOrNone`
at ≈1.04x mean while materializing the pair list the LINQ count-check comparison avoids, and
`ToNonEmptyOrNone` + `Aggregate` at ≈1.8x with the disclosed single-pass materialization;
`SingleOrNone` ran at ≈0.44x for `Count=16` and ≈2.7x for `Count=1024` (both scan to the unique
match at the end of the fixture), and the located `Traverse` at ≈4.7x–7.6x of the hand-written
index-tracking loop with the per-item `Location` allocations it replaces — the compositional-path
cost of `customers[17].addresses[2].postalCode` without application bookkeeping. These numbers
are a directional local run, not the release baseline.
