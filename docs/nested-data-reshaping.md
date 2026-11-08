# Typed Nested Data Reshaping And Provenance

`FunnySharp` turns decoded nested records into ordinary typed rows while keeping the parent record,
the nested element position, and the failure location visible. Three shapes cover the common import:
`Expand` yields one row per nested child, `ExpandAligned` pairs two parallel child sequences by
index and rejects unequal lengths, and `ExpandCartesian` is the separately named explicit product.
Field extraction stays in the existing [`Decoder<T>`](decoding.md) boundary. Compiling examples are
in [NestedDataExamples.cs](../examples/FunnySharp.Examples/NestedDataExamples.cs); the reference
consumers, fixed failure locations, consumption ledgers, and measured costs are in
[nested-data-reshaping-evidence.md](nested-data-reshaping-evidence.md).

## API Shape

The expansion operations return the canonical `Validation` carrier. `RowOrigin` and
`CartesianRowOrigin` are provenance values, not carriers; `NestedRowError` is their diagnostic.
No new outcome or collection type is introduced.

| Member | Source / receiver | Result |
| --- | --- | --- |
| `Expand(children, combine)` | `IEnumerable<TParent>` | `Validation<IReadOnlyList<TResult>, NestedRowError>` |
| `ExpandAsync(children, combine, [token])` | `IAsyncEnumerable<TParent>` | `ValueTask<Validation<IReadOnlyList<TResult>, NestedRowError>>` |
| `ExpandAligned(firstChildren, secondChildren, combine)` | `IEnumerable<TParent>` | `Validation<IReadOnlyList<TResult>, NestedRowError>` |
| `ExpandAlignedAsync(firstChildren, secondChildren, combine, [token])` | `IAsyncEnumerable<TParent>` | `ValueTask<Validation<IReadOnlyList<TResult>, NestedRowError>>` |
| `ExpandCartesian(firstChildren, secondChildren, combine)` | `IEnumerable<TParent>` | `Validation<IReadOnlyList<TResult>, NestedRowError>` |
| `ExpandCartesianAsync(firstChildren, secondChildren, combine, [token])` | `IAsyncEnumerable<TParent>` | `ValueTask<Validation<IReadOnlyList<TResult>, NestedRowError>>` |

Every child selector returns `IReadOnlyList<TChild>`. The list is read during the call; it is not
cloned, snapshotted, cached, or enumerated twice. The combine callback is synchronous in every
form. It receives the parent, the child element or aligned pair, and a `RowOrigin` (or
`CartesianRowOrigin`) carrying the zero-based positions.

## From JSON To Typed Rows

The decoder owns field extraction. `Decoder.Field` distinguishes absence from explicit null,
`Decoder.List` owns array shape and element order, and `Decoder.List` over a record array prefixes
each error with the record index. The expansion layer starts from typed values.

<!-- documentation-sample: DocumentationSamples.NestedData.DecodeAndExpand -->
```csharp
var lineDecoder = Decoder.Object(
    Decoder.Field("sku", Decoder.NonEmptyString),
    Decoder.Field("quantity", Decoder.Int32),
    static (sku, quantity) => new NestedSampleLine(sku, quantity));
var orderDecoder = Decoder.Object(
    Decoder.Field("orderId", Decoder.NonEmptyString),
    Decoder.Field("customer", Decoder.NonEmptyString),
    Decoder.Field("lines", Decoder.List(lineDecoder)),
    static (orderId, customer, lines) =>
        new NestedSampleOrder(orderId, customer, lines));

using var document = JsonDocument.Parse(json);
var decoded = Decoder.List(orderDecoder).Decode(document.RootElement);

decoded.Match(
    orders =>
    {
        var expanded = orders.Expand(
            static order => order.Lines,
            static (order, line, origin) =>
                new NestedSampleRow(
                    order.OrderId,
                    order.Customer,
                    line.Sku,
                    line.Quantity,
                    origin));

        expanded.Match(
            static rows => Console.WriteLine($"{rows.Count} rows."),
            static errors => Console.WriteLine($"{errors.Count} expansion failures."));
    },
    static errors => Console.WriteLine($"{errors.Count} decode failures."));
```

The boundary policies are explicit and already belong to the decoder:

| Boundary | Policy | Observed result |
| --- | --- | --- |
| Missing required field | `Decoder.Field` | `Missing` at the field path, such as `[1].lines[0].quantity` |
| Explicit null field | `Decoder.Field` | `Null` at the field path |
| Absence versus null | `OptionalField` / `NullableField` | `None` for absence; `NullableField` also collapses explicit null to `None` |
| Wrong JSON kind | Built-in scalar/array decoders | `WrongKind` at the field path |
| Accepted kind, unparseable text | Built-in scalar decoders | `Malformed` at the field path |
| Empty string or empty array | `NonEmptyString` / `NonEmptyList` | `Empty`; `String`/`List` accept emptiness |
| Nested element errors | `Decoder.List` | Every element is decoded; errors accumulate in source order with `[index]` prefixes |
| Several independent field errors | `Decoder.Object` | Every field is checked; errors accumulate in field argument order |

Expansion is never invoked on a failed decode. A successful decode produces typed values that the
caller owns; the JSON document may be disposed before the rows are consumed.

## Expanding Nested Records

`Expand` walks the records in source order and, for each record, the selected child list in list
order. The output count is the sum of the child counts of successfully expanded records. Every row
receives the parent value, so `new OrderRow(order.Id, order.Customer, line.Sku, line.Quantity)`
retains the parent context without a second lookup.

A record with an empty child list contributes no rows and no error. A selector result of runtime
null is not empty: it is a `NestedRowErrorKind.NullChildSequence` failure with the record index.
The library does not coerce null to empty. A null child element inside a non-null list, and a null
parent record, are passed to the combine callback unchanged; the library does not test, filter, or
reject them.

The operation is eager and all-or-nothing. It materializes the complete row list and returns either
an exact-size array of rows or no rows plus every structural failure. A parent source is enumerated
exactly once; the child lists are read during that traversal.

If a failure is recorded, later records still have their child selector invoked so further null
failures are reported, but the combine callback is not invoked for later children. Output produced
before the first error is discarded; earlier combine invocations are not rolled back. That ledger is
deliberate: complete diagnostics are more useful than partial rows for a load boundary, and the
caller can expand record by record when partial output is genuinely wanted.

## Aligning Parallel Child Sequences

`ExpandAligned` takes two child sequences for each record and pairs them by index. Equal lengths
produce one row per index in increasing index order; two two-element lists produce two rows, never
four. Unequal lengths produce one `ChildCountMismatch` failure carrying the record index and both
counts, and the whole operation returns no rows.

<!-- documentation-sample: DocumentationSamples.NestedData.ExpandAligned -->
```csharp
var readings = meters.ExpandAligned(
    static meter => meter.Timestamps,
    static meter => meter.Values,
    static (meter, timestamp, value, origin) =>
        new NestedSampleReading(meter.MeterId, timestamp, value, origin));

readings.Match(
    static rows => Console.WriteLine($"{rows.Count} readings."),
    static errors => Console.WriteLine(errors[0].ToString()));
```

This is deliberately different from BCL `Enumerable.Zip`, which truncates to the shorter side and
silently drops the extra element. A compact wire format with parallel arrays is exactly where that
silent loss matters, so the aligned operation reports the mismatch instead. Both empty lists are
equal and contribute no rows. A null first or second list is
`NullFirstChildSequence` or `NullSecondChildSequence` with the record index; both selectors are
invoked for the record so both failures can be reported.

The aligned operation never infers a product. If the intended relation is a cross product, name it
with `ExpandCartesian`; if silent truncation is genuinely acceptable, use BCL `Zip` directly.

## Explicit Cartesian Expansion

`ExpandCartesian` is the explicit product. For each record it emits every first-sequence element
paired with every second-sequence element, ordered by first index and then second index. Unequal
lengths are valid and produce `first.Count * second.Count` rows; no mismatch error is possible. An
empty list on either side contributes no rows.

<!-- documentation-sample: DocumentationSamples.NestedData.CartesianDistinction -->
```csharp
var aligned = variants.ExpandAligned(
    static variant => variant.Sizes,
    static variant => variant.Colors,
    static (variant, size, color, _) => $"{variant.ProductId}:{size}:{color}");
var cartesian = variants.ExpandCartesian(
    static variant => variant.Sizes,
    static variant => variant.Colors,
    static (variant, size, color, _) => $"{variant.ProductId}:{size}:{color}");

aligned.Match(
    static rows => Console.WriteLine($"{rows.Count} aligned pairs."),
    static errors => Console.WriteLine(errors[0].ToString()));
cartesian.Match(
    static rows => Console.WriteLine($"{rows.Count} product rows."),
    static errors => Console.WriteLine(errors[0].ToString()));
```

With two sizes and two colors, the aligned operation yields two rows and the Cartesian operation
yields four. The relation is chosen by the method name; there is no boolean or enum mode parameter,
and no operation infers a product from data.

## Provenance And Rejected Items

`RowOrigin` carries `RecordIndex` and `ElementIndex`; `CartesianRowOrigin` carries `RecordIndex`,
`FirstElementIndex`, and `SecondElementIndex`. `ToString` renders `record[3].element[1]` and
`record[3].elements[1,2]` on demand. No path string or `Location` object is created per successful
row.

The minimal provenance form is to ignore the origin argument when only the record position of a
structural failure is needed: `NestedRowError` carries that record and, for a mismatch, both
counts, but no element. A caller that records a rejection, or needs the element position of a
failure, reads the origin or the decoder's `DecodeError.Path`. The full form stores the origin in
the caller's row record. Both forms are explicit in the combine signature, and both allocate no
library-owned per-row provenance object.

Rejection is application policy, not a hidden library filter. The expansion family is total: it
does not decide that a row is invalid. A caller that rejects an element uses the origin to record
the record and element positions, so the rejection is not silently dropped.

<!-- documentation-sample: DocumentationSamples.NestedData.ProvenanceRejection -->
```csharp
var expanded = orders.Expand(
    static order => order.Lines,
    static (order, line, origin) =>
        new NestedSampleRow(
            order.OrderId,
            order.Customer,
            line.Sku,
            line.Quantity,
            origin));

expanded.Match(
    static rows =>
    {
        foreach (var row in rows)
        {
            if (row.Quantity <= 0)
            {
                Console.WriteLine($"rejected {row.Origin}");
            }
        }
    },
    static errors => Console.WriteLine(errors[0].ToString()));
```

A rejected item is distinct from a structural failure and from absence. The caller owns the
rejection reason and its own error type; the library supplies the position and never turns the row
into a different outcome silently.

## Sync And Async Sources

The asynchronous forms acquire the parent records from `IAsyncEnumerable<TParent>` and otherwise
have the same semantics: one source enumeration, the same row and failure order, the same null and
length policies, and the same all-or-nothing output. They return `ValueTask` and forward the exact
caller token to `GetAsyncEnumerator` only. The selectors and combine callback are synchronous and
receive no token. Source, selector, combine, and disposal exceptions keep normal asynchronous
behavior; cancellation is never converted into a `NestedRowError`.

The asynchronous forms buffer the complete result exactly like the synchronous forms. If a caller
stops consuming before the value task completes, the source is still being enumerated by the
operation; there is no partially delivered row list. Partial output would require the separate
record-by-record composition described above.

A `JsonElement` is a borrowed view over a caller-owned document. Decode it to typed values before
waiting, and do not retain a `Decoder.Element` view after the document is disposed. The expansion
operations accept typed records, not `JsonElement` or spans, so no borrowed view crosses an await.
There is deliberately no `ReadOnlySpan<TChild>` overload: a span cannot cross an asynchronous
source, and a span-backed child view cannot outlive the call. Materialize the child list first when
the input is a span.

## Order, Count, And Buffering

| Operation | Output shape | Order | Cardinality | Buffering | Partial output |
| --- | --- | --- | --- | --- | --- |
| `Expand` | `IReadOnlyList<TResult>` or errors | record order, child order | sum of child counts | Complete rows and error list | None on failure |
| `ExpandAligned` | `IReadOnlyList<TResult>` or errors | record order, increasing index | sum of aligned lengths | Complete rows and error list | None on failure |
| `ExpandCartesian` | `IReadOnlyList<TResult>` or errors | record order, first index, second index | sum of `first * second` | Complete rows and error list | None on failure |
| Async forms | same as the synchronous shape | same | same | same; source enumerated once | None on failure or cancellation |

Structural failures are ordered by record index. Within one record, a null first-sequence failure
precedes a null second-sequence failure, and a mismatch is one failure. A record with a null list
does not also produce a length comparison; a record with a mismatch does not produce rows.

## Costs

- `Expand`, `ExpandAligned`, and `ExpandCartesian` allocate the output list, one exact-size array on
  success, and—only on failure—an error list. The error value is a readonly struct; its text is
  created only when `ToString` is called.
- `RowOrigin` and `CartesianRowOrigin` are readonly structs. Passing them to the combine callback
  does not allocate; storing them in the caller's row record adds only the struct fields.
- A complete per-row trace is a caller choice. Rendering a `Location` object or string for every
  row costs materially more than the origin struct; the measurement table records that disclosure
  and the default remains the small origin.
- The buffered `Validation` and exact-size output array are not free. Against a deferred-path
  hand-written import or an index-loop Cartesian product, the measured allocation is higher; the
  evidence records each gap rather than claiming an allocation win over competent hand-written code.
- The hand-written and decoder-based import allocations, source-enumeration ledgers, aligned
  mismatch costs, and provenance rows are in
  [nested-data-reshaping-evidence.md](nested-data-reshaping-evidence.md#measurements).

## Choosing The Operation

| Need | Operation |
| --- | --- |
| One row per nested child, with parent context | `Expand` |
| Two parallel child sequences that must have equal length | `ExpandAligned` |
| Two child sequences whose cross product is the intended relation | `ExpandCartesian` |
| Structural failures only need the record position and, for a mismatch, both counts | Expand and ignore the origin argument |
| A rejected element must retain its record and element position | Expand and record the origin in the rejection |
| Every row needs its record and element position in the output | Expand and store the origin |
| A plain flatten with no alignment, location, or provenance requirement | BCL `SelectMany` |
| Field extraction and JSON path diagnostics | Existing `Decoder` composition |
| Keyed association rather than nested shape | [Cardinality joins](cardinality-joins.md) |

For a simple nested list where every item is accepted and no failure location is needed,
`SelectMany` remains shorter. This family earns its place when parallel arrays must be aligned
exactly, a structural failure must identify its record and counts, or a rejected row must retain
its record and element position.

## Deliberate Boundaries

The family adds no second outcome carrier, no custom sequence or row type, no serializer, no
reflection binder, no schema registry, and no DataFrame engine. It does not evaluate expressions
dynamically, coerce values lossily, or infer a keyed relation from data. `ExpandAligned` never
produces a Cartesian product and `ExpandCartesian` never becomes an exact-length check; the
relation is always in the method name, never in a boolean or enum parameter.

It does not drop a row silently. Rejection remains caller policy: the library is total and supplies
the origin, while the caller records its own rejection reason. A caller that filters without
recording the origin has chosen to lose the location; the library does not hide that choice.

It provides no streaming partial-output shape. Every operation buffers the complete row list and
returns no rows on failure. A caller that needs partial output expands record by record with
ordinary sequence code. It also provides no asynchronous selector or combine callback, no span-based
child view, no more than two parallel child sequences, and no expansion over `JsonElement` without
decoding first; those shapes either cross a lifetime boundary or lack an evidenced consumer.

Field extraction, missing/null policy, and JSON paths belong to [`Decoder<T>`](decoding.md). The
expansion layer does not duplicate them, and it does not promise that `Decoder.Element` views remain
valid after the document is disposed. Keyed association and cardinality checks belong to
[cardinality joins](cardinality-joins.md); nested shape transformation does not build an index or
join.
