# Cardinality-Checked Keyed Lookups And Joins

`FunnySharp` lets a consumer state the cardinality of keyed association code instead of
collapsing missing, duplicate, or ambiguous data into a convenient value. The family has two
shapes: a buffered exactly-one join that validates the complete lookup input and returns no
partial output on failure, and a streaming join that yields one honest outcome per source row.
Compiling examples are in [JoinExamples.cs](../examples/FunnySharp.Examples/JoinExamples.cs); the
reference consumers, fixed failure locations, consumption ledgers, and measured index/output/error
costs are in [cardinality-joins-evidence.md](cardinality-joins-evidence.md).

## API Shape

The index types are materialized association descriptions, not outcome carriers. Keyed reads use
the canonical `Option` and `Result` values; buffered joins use `Validation`, while the streaming
join yields `Result` outcomes; `LookupError<TKey>` is only a diagnostic value.

| Member | Source / receiver | Result |
| --- | --- | --- |
| `ToLookupIndex(keySelector[, comparer])` | `IEnumerable<TValue>` | `Validation<LookupIndex<TKey,TValue>, LookupError<TKey>>` |
| `ToUniqueLookupIndex(keySelector[, comparer])` | `IEnumerable<TValue>` | `Validation<UniqueLookupIndex<TKey,TValue>, LookupError<TKey>>` |
| `GetMany(key)` | `LookupIndex<TKey,TValue>` | `IReadOnlyList<TValue>` |
| `GetZeroOrOne(key)` | `LookupIndex<TKey,TValue>` | `Result<Option<TValue>, LookupError<TKey>>` |
| `GetExactlyOne(key)` | `LookupIndex<TKey,TValue>` | `Result<TValue, LookupError<TKey>>` |
| `RequireUniqueKeys()` | `LookupIndex<TKey,TValue>` | `Validation<UniqueLookupIndex<TKey,TValue>, LookupError<TKey>>` |
| `GetOption(key)` | `UniqueLookupIndex<TKey,TValue>` | `Option<TValue>` |
| `JoinExactlyOne(lookupSource, ...)` | `IEnumerable<TSource>` | `Validation<IReadOnlyList<TResult>, LookupError<TKey>>` |
| `JoinExactlyOne(uniqueIndex, ...)` | `IEnumerable<TSource>` | `Validation<IReadOnlyList<TResult>, LookupError<TKey>>` |
| `JoinExactlyOneResults(index, ...)` | `IEnumerable<TSource>` | `IEnumerable<Result<TResult, LookupError<TKey>>>` |

Every factory and read validates its arguments eagerly. The lookup source is enumerated exactly
once at index construction; keyed reads and both join shapes do not enumerate it again. The
buffered join enumerates the source once; the streaming join enumerates it once per consumer
enumeration.

## Building An Index

`ToLookupIndex` preserves every occurrence of a key in lookup-source order. `ToUniqueLookupIndex`
adds the at-most-one guarantee over the complete lookup input and returns the wrapper only when
that guarantee holds.

<!-- documentation-sample: DocumentationSamples.CardinalityJoins.BuildUniqueIndex -->
```csharp
var customersById = customers
    .ToUniqueLookupIndex(
        static customer => customer.Id,
        StringComparer.OrdinalIgnoreCase)
    .Match(
        static index => index,
        static errors => throw new InvalidOperationException(
            string.Join("; ", errors.Select(static error => error.ToString()))));
```

The comparer is always the explicit comparer or `EqualityComparer<TKey>.Default`; the index
exposes it as `Comparer`. There is deliberately no overload that accepts an
`IReadOnlyDictionary<TKey,TValue>` and inherits its equality policy: the interface does not reveal
whether its keys use the default, ordinal-ignore-case, culture-aware, or a custom comparer. A
caller starting from a dictionary passes its pairs as a sequence and states the comparer it means.

## Lookup Cardinality

| Read | Zero matches | One match | Multiple matches |
| --- | --- | --- | --- |
| `GetMany(key)` | empty list | one value | every value in lookup-source order |
| `GetZeroOrOne(key)` | `Success(None)` | `Success(Some(value))` | `Failure(DuplicateKey)` |
| `GetExactlyOne(key)` | `Failure(MissingKey)` | `Success(value)` | `Failure(DuplicateKey)` |
| `GetOption(key)` on a unique index | `None` | `Some(value)` | impossible by construction |

`GetMany` is the explicit one-to-many read. It is also the only expansion primitive: a join never
turns a duplicated key into extra output rows. `GetZeroOrOne` and `GetExactlyOne` check uniqueness
for the matched key only; duplicates elsewhere in the index are not inspected.

A duplicate failure carries the queried key, the repeated occurrence's lookup position, and the
first occurrence's lookup position. A missing failure from a direct keyed read has no source
position because the library has no source row; the joins supply the row position.

### Keys And Nulls

`TKey` is constrained to `notnull`; the key domain is explicit.

- A lookup-source selector result that is null is a `NullKey` failure with the lookup position.
  The invalid factory result retains no null-key entry.
- A probe-source selector result that is null is a `NullKey` failure with the source-row position.
- A null key passed directly to `GetMany`, `GetZeroOrOne`, `GetExactlyOne`, or `GetOption` is an
  eager `ArgumentNullException`.
- The comparer never observes a null key.
- An empty string is an ordinary key and matches normally.

A present null *value* follows the index's cardinality shape. `GetMany` returns it and
`GetExactlyOne` can return it as a successful `Result` payload, because `Result` permits null.
`GetZeroOrOne` and `GetOption` use `Option`, which cannot hold null, so they reject a present null
value with `ArgumentNullException`, matching the existing container bridges.

## Joining Exactly One

`JoinExactlyOne` requires exactly one lookup match for every source row. The raw overload builds
and validates the complete lookup index before success is possible; the prebuilt overload accepts
a `UniqueLookupIndex` whose guarantee was already established.

<!-- documentation-sample: DocumentationSamples.CardinalityJoins.JoinOrders -->
```csharp
var enriched = orders.JoinExactlyOne(
    customers,
    static order => order.CustomerId,
    static customer => customer.Id,
    static (order, customer) => new EnrichedOrder(order.Id, customer.Name),
    StringComparer.OrdinalIgnoreCase);

enriched.Match(
    static outputs => Console.WriteLine($"{outputs.Count} orders enriched."),
    static errors => Console.WriteLine($"{errors.Count} relation failures."));
```

On success the result is an `IReadOnlyList<TResult>` in source order with exactly one output for
every source row. On failure it contains no outputs and every structural error. The error order is
deterministic: lookup-source errors first, ordered by lookup position, followed by source-row
errors in source order. A duplicate lookup key is a whole-operation failure even when no source row
uses it.

`combine` runs only for a row with exactly one match and only while no error has been recorded.
After the first recorded error, later rows are still checked for null and missing keys, so the
diagnostics remain complete, but `combine` is not invoked. Calls that already happened are not
rolled back; the output list is discarded on failure. The streaming shape below has a different
ledger because each row is an independent outcome.

## Streaming Partial Output

`JoinExactlyOneResults` takes a prebuilt `LookupIndex` and yields one `Result` per source row in
source order. It buffers no output and does not enumerate the lookup source.

<!-- documentation-sample: DocumentationSamples.CardinalityJoins.StreamRows -->
```csharp
var index = customers
    .ToLookupIndex(static customer => customer.Id, StringComparer.OrdinalIgnoreCase)
    .Match(
        static value => value,
        static errors => throw new InvalidOperationException(
            string.Join("; ", errors.Select(static error => error.ToString()))));

foreach (var outcome in orders.JoinExactlyOneResults(
    index,
    static order => order.CustomerId,
    static (order, customer) => new EnrichedOrder(order.Id, customer.Name)))
{
    outcome.Match(
        static enriched => Console.WriteLine($"enriched {enriched.Id}"),
        static error => Console.WriteLine(error));
}
```

A success can be yielded before a later failure; a later success can follow a failure. Failures
are items, never thrown or silently dropped for data reasons. The per-row outcome count equals the
source count, including failure rows. A matched duplicate yields exactly one `DuplicateKey`
outcome carrying the source row and both lookup positions; it never yields multiple output rows.
The failure's key uses the probing source row's spelling; complete-index validation reports the
first stored spelling under the index's comparer. If the consumer stops early, later rows and
their failures are not observed. That is the streaming contract, not a broken promise.

## Uniqueness Scope

Uniqueness is a property of the operation, not a global claim about the data.

| Operation | Scope | On duplicate |
| --- | --- | --- |
| `ToLookupIndex` | no uniqueness requirement; every occurrence retained | valid; `GetMany` returns all values |
| `GetMany` | no uniqueness requirement | every value in source order |
| `GetZeroOrOne` / `GetExactlyOne` | matched key only | typed `DuplicateKey` failure |
| `RequireUniqueKeys` | complete retained index | every occurrence after the first, in lookup order |
| `ToUniqueLookupIndex` | complete lookup source | null and duplicate failures in lookup order |
| `JoinExactlyOne` over a raw lookup sequence | complete lookup source | invalid; no outputs |
| `JoinExactlyOne` over a unique index | already established | no duplicate can exist |
| `JoinExactlyOneResults` | matched keys only | one `DuplicateKey` outcome for that row |

The distinction is deliberate. The buffered join can afford to buffer the lookup input, so it
validates every entry. The streaming join uses a stable prebuilt index and checks only keys it
actually reaches; an unused duplicate does not fail its rows. Neither shape claims the other's
guarantee.

## Order, Count, And Expansion

- Index groups preserve lookup-source order.
- `GetMany` returns values in lookup-source order.
- A successful buffered join returns one output per source row in source order; its count equals
  the source count.
- The streaming join yields one outcome per source row in source order; its count equals the
  source count, failures included.
- Duplicate lookup keys never increase the output count. The explicit expansion path is
  `GetMany` plus ordinary sequence code such as `SelectMany`; no join infers a Cartesian product.

## Lifetime And Ownership

The index is an eager snapshot of key assignments and value references. The key selector runs once
per source item during construction. A later mutation of a value is visible through every index
read; a later change to a value's key is not observed. Values are not cloned, snapshotted, frozen,
or rolled back. The index is read-only after construction and adds no locks; concurrent reads are
safe only when no caller mutates the retained values concurrently.

The buffered join's output is a fresh exact-size array exposed as `IReadOnlyList<TResult>`; the
combine callback owns whatever it returns. The streaming join allocates one `Result` value per row while iterating and retains
nothing after the consumer moves on.

## Costs

- Building an index is O(n) in the lookup source with expected O(1) dictionary operations. The
  index stores one dictionary entry per distinct key and one flat entry per retained value; the
  complete-input uniqueness scan is O(n) over the retained index and allocates only on failure.
- `GetMany` allocates a fresh result array proportional to the group size; a missing key returns
  the shared empty array. `GetZeroOrOne` and `GetExactlyOne` allocate no result buffer.
- The buffered join materializes the complete unique index, the output list, and an exact-size
  output array on success. On failure it materializes the error list and the `Validation` snapshot;
  it returns no partial output. The combine callback may run for earlier rows before a later
  missing row is discovered, and those calls are not rolled back.
- The streaming join allocates no output buffer of its own; each consumer step receives one
  `Result` carrying a `LookupError` value. The prebuilt index is reused across enumerations; the
  source is enumerated again.
- The exact allocation and directional timing rows, including the idiomatic-C# and hand-indexed
  baselines, are in
  [cardinality-joins-evidence.md](cardinality-joins-evidence.md#measurements).

## Choosing The Operation

| Need | Operation |
| --- | --- |
| One typed output per input row, with complete relation diagnostics | `JoinExactlyOne` |
| Reuse a validated lookup across several joins | `ToUniqueLookupIndex` + `JoinExactlyOne(unique, ...)` |
| Stream per-row outcomes and tolerate partial output | `ToLookupIndex` + `JoinExactlyOneResults` |
| Zero-or-one keyed lookup | `UniqueLookupIndex.GetOption` or `LookupIndex.GetZeroOrOne` |
| Exactly-one keyed lookup without a source row | `LookupIndex.GetExactlyOne` |
| Explicit one-to-many expansion | `LookupIndex.GetMany` + ordinary LINQ |

For a plain happy-path dictionary lookup, BCL `ToDictionary` or `GetOption` remains shorter. This
family earns its place when missing, duplicate, or ambiguous data must be reported honestly with
positions instead of collapsing into absence, a thrown BCL exception, or a convenient first value.

## Deliberate Boundaries

The family adds no second outcome carrier, no hidden cardinality enum or boolean parameter, and no
implicit conversion. It provides no general SQL provider, ORM query translator, DataFrame universe,
database constraint, transaction, concurrency control, or claim that stored data satisfies the
same uniqueness: the index validates only the in-memory lookup snapshot it was given. It provides
no many-to-many or one-to-many join; explicit expansion uses `GetMany` and ordinary sequence code,
so a Cartesian product is never inferred. It provides no async `IAsyncEnumerable` index or join
mirror; asynchronous acquisition remains the caller's boundary. It does not infer an opaque
dictionary comparer, accept a dictionary as proof of its equality policy, clone or freeze retained
values, roll back combine calls, retry a failed row, cache the source, or convert cancellation and
programming exceptions into `LookupError` values. This family combines flat caller records only;
nested child expansion, exact parallel alignment, explicit Cartesian expansion, and row provenance
are separate in [nested data reshaping](nested-data-reshaping.md).
