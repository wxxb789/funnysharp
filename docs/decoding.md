# Decoding Untrusted Input

`FunnySharp.Decoder<T>` turns a parsed BCL `JsonElement` into a caller-defined domain value and
reports structural or domain failures as `Validation<T, DecodeError>`. It is a callable decoder,
not a schema, serializer, or reflection layer. The compiling examples are in
[examples/FunnySharp.Examples/DecoderExamples.cs](../examples/FunnySharp.Examples/DecoderExamples.cs);
the three-way consumer comparison is in
[tests/Shared/DecoderComparison](../tests/Shared/DecoderComparison/), with its evidence in
[decoder-evidence.md](decoder-evidence.md).

## API Shape

```csharp
using System.Text.Json;
using FunnySharp;

var customerDecoder = Decoder.Field("name", Decoder.NonEmptyString)
    .Map(name => new Customer(name));
var lineDecoder = Decoder.Object(
    Decoder.Field("sku", Decoder.NonEmptyString),
    Decoder.Field(
        "quantity",
        Decoder.Int32.Ensure(quantity => quantity > 0, "quantity must be positive")),
    (sku, quantity) => new OrderLine(sku, quantity));
var orderDecoder = Decoder.Object(
    Decoder.Field("orderId", Decoder.Guid),
    Decoder.Field("customer", customerDecoder),
    Decoder.Field("lines", Decoder.NonEmptyList(lineDecoder)),
    Decoder.OptionalField("note", Decoder.String),
    (orderId, customer, lines, note) => new Order(orderId, customer, lines, note));

var json = """
    {
      "orderId": "0f8fad5b-d9cb-469f-a165-70867728950e",
      "customer": { "name": "Ada" },
      "lines": [ { "sku": "BOOK-1", "quantity": 2 } ]
    }
    """;

using var document = JsonDocument.Parse(json);
var decoded = orderDecoder.Decode(document.RootElement);

if (decoded.TryGetValue(out var value))
{
    Console.WriteLine($"{value.Lines.Count} lines");
}
else
{
    decoded.TryGetErrors(out var errors);
    foreach (var error in errors!)
    {
        Console.WriteLine($"{error.Path}: {error.Kind}");
    }
}

internal sealed record Customer(string Name);
internal sealed record OrderLine(string Sku, int Quantity);
internal sealed record Order(
    Guid OrderId,
    Customer Customer,
    IReadOnlyList<OrderLine> Lines,
    Option<string> Note);
```

A `Decoder<T>` is an immutable, reusable function. `Decode` returns `Validation<T, DecodeError>`;
the caller inspects the outcome once. There is no per-field unpacking and no path string assembly:
`Field`, `List`, and the object combinators compose paths from the decoder structure.

The factories are:

- Scalars: `Decoder.String`, `NonEmptyString`, `Boolean`, `Int32`, `Int64`, `Decimal`, `Double`,
  `Guid`, `DateTime`, `DateTimeOffset`.
- Raw input: `Decoder.Element` returns the current `JsonElement`, including an explicit null.
- Custom leaf: `Decoder.From<T>(Func<JsonElement, Validation<T, DecodeError>>)`.
- Structure: `Decoder.Field`, `OptionalField`, `NullableField`, `OptionalValue`, `List`,
  `NonEmptyList`, and `Object` arities 2-4.
- Composition: `Map`, `Bind`, `Ensure`, `Validate`.

## Input Boundary

The input is a `System.Text.Json.JsonElement`, which is a BCL view over a caller-owned
`JsonDocument` or `JsonNode`. The caller chooses how the text is parsed and owns the document
lifetime. Decoding reads the element during the call; built-in scalar and structural decoders
return values independent of the document. `Decoder.Element` is the exception: it returns a view
that must not outlive the document.

Object property names are case-sensitive. If a document repeats a name, `JsonElement.TryGetProperty`
resolves the first matching property in document order. `Decoder` does not normalize names,
perform case-insensitive matching, or merge duplicate properties.

Malformed JSON text fails in `JsonDocument.Parse` before a decoder is called. That parse failure is
an input-boundary exception, not a `DecodeError`. The decoder's error model starts from a valid
JSON tree.

## Error Model

`DecodeErrorKind` distinguishes the cases a consumer usually needs to handle differently:

| Kind | Meaning |
| --- | --- |
| `Missing` | A required object property is absent. |
| `Null` | An explicit JSON null was found where a value is required. |
| `WrongKind` | The JSON kind is not accepted, such as a string where a number is required. |
| `Malformed` | The kind is accepted but the text or number cannot be parsed as the target type. |
| `Empty` | An empty string or array was found where a non-empty value is required. |
| `InvalidValue` | A caller-supplied domain check rejected the value. |

`DecodeError.Path` is the stable rendered location, such as
`customer.addresses[1].postalCode` or `lines[1].quantity`. The root path is the empty string;
`ToString` displays it as `input`. `Expected` and `Detail` carry the expected shape and optional
diagnostic context.

`DecodeError.Location` is the same path as the experimental `Location` type
(`FS0017`). It is marked `[Experimental]` and carries no compatibility promise; `Path` is the
stable contract. The structured accessor is useful for tooling that already handles `Location`,
but production consumers should normally read `Path`, `Kind`, `Expected`, and `Detail`.

Errors are deterministic. Object fields are reported in decoder argument order, list elements in
source order, and each inner validation's own error order is preserved. Two independently created
errors with the same location, kind, expected text, and detail are equal and share a hash code.

## Optional, Missing, Null, And Empty

The policy is chosen by the combinator name:

| Input | `Field` | `OptionalField` | `NullableField` | `OptionalValue` |
| --- | --- | --- | --- | --- |
| Property absent | `Missing` error | `None` | `None` | Not applicable |
| Explicit null | Inner decoder (normally `Null`) | Inner decoder (normally `Null`) | `None` | `None` |
| Present value | Decode | Decode and `Some` | Decode and `Some` | Decode and `Some` |

`OptionalField` keeps explicit null distinguishable: only absence becomes `None`. `NullableField`
deliberately collapses absence and null; the name states that policy, and the inner decoder is not
invoked for a null property. `OptionalValue` is the value-level policy for array elements and
already-selected values. It does not treat a default `JsonElement` as absence.

Empty is a separate policy, not an optionality policy:

- `Decoder.String` accepts `""`; `Decoder.NonEmptyString` reports `Empty`.
- `Decoder.List` accepts `[]`; `Decoder.NonEmptyList` reports `Empty`.
- An empty domain rule can still reject a present value with `Ensure` or `Validate`.

A successful decoder must not produce a runtime-null reference value when it is wrapped by
`OptionalField`, `NullableField`, or `OptionalValue`. `Option.Some` rejects runtime null; that is a
programming error in the inner decoder and propagates as `ArgumentNullException`, not as absence.

## Independent And Dependent Steps

`Object` and `List` accumulate independent errors:

- `Object(first, second, ..., combine)` checks the object kind once, runs every field decoder even
  when an earlier field failed, and invokes `combine` only when every operand is valid. It supports
  2-4 fields; wider objects nest one `Object` inside another.
- `List` and `NonEmptyList` decode every reached element and accumulate element errors in source
  order. A list is materialized once, in source order.

`Bind`, `Ensure`, and `Validate` are dependent steps. Their callbacks run only after the preceding
decode succeeds. Dependent steps are fail-fast in the same sense as `Result`: a failed
prerequisite stops the step. Independent structure is applicative in the same sense as
`Validation`: every independent field and element is still checked.

The composition verbs are:

- `Map` transforms a successful value. It adds no errors and propagates selector exceptions.
- `Bind` selects the next decoder from the successful value and decodes the same input with it.
  This is how a discriminator field selects the rest of an object. The binder is skipped after a
  failure; a null decoder returned by the binder is an `InvalidOperationException`.
- `Ensure` applies one predicate and adds one `InvalidValue` error on rejection.
- `Validate` applies an accumulating domain validation and maps every returned message to an
  `InvalidValue` error at the decoder's location. The callback is skipped after a decode failure.

`Ensure` and `Validate` do not catch exceptions. A throwing callback, a throwing `Map` selector, or
a throwing `Bind` binder propagates unchanged. The decoder never manufactures a `DecodeError` from
a programming exception.

## Asynchronous Validation

The input is in-memory `JsonElement`, so decoding itself is synchronous. Asynchronous work is
meaningful only for a domain validation that actually awaits something, such as a uniqueness or
inventory check:

```csharp
var decoded = await order.DecodeValueAsync(
    document.RootElement,
    async (value, cancellationToken) =>
    {
        var inventory = await inventoryClient.CheckAsync(value.Lines, cancellationToken);
        return inventory.IsAvailable
            ? Validation<Order, string>.Valid(value)
            : Validation<Order, string>.Invalid("one or more lines are unavailable");
    },
    cancellationToken);
```

`DecodeAsync` takes a `Task`-returning validation; `DecodeValueAsync` takes a `ValueTask`-returning
validation. Both decode synchronously first. On an invalid decode they return the existing errors
without invoking the callback or inspecting the token. On success they invoke the callback at most
once, await it once, and map its messages to `InvalidValue` errors at the root of the decoder's
input. The supplied `CancellationToken` is forwarded unchanged. Cancellation and faults propagate
with their original exception and token.

There is deliberately no asynchronous input overload and no second asynchronous decoder type. Parse
or stream at the I/O boundary, then decode the resulting in-memory tree.

## Reuse And Concurrency

The built-in decoders hold only immutable delegates and factories. No invocation state, input
cache, or location is stored in the decoder. One decoder instance can be reused across requests and
across threads; each `Decode` call creates only local state. A custom `Decoder.From` delegate is the
caller's responsibility: if it captures mutable state, that state is shared in the same way as any
other delegate.

The location prefixes for a successful decode are not materialized. When an inner decode fails,
`Field` and `List` compose the relevant `Location` prefixes and the error path is attached. This
keeps happy-path decoding free of diagnostic path allocation while preserving useful failures.

## Stability And Cost

`Decoder`, `Decoder<T>`, `DecodeError`, and `DecodeErrorKind` are stable members of the current
surface. Only `DecodeError.Location` is experimental (`FS0017`); `Path` is stable. The structured
`Location` type remains experimental because its representation and allocation behavior are not
promised.

`Location` is not allocation-free. It is an immutable class; appending a segment creates a new
`Location` and copies the segment array, so a path built segment by segment has superlinear byte
cost with depth. The decoder avoids path construction on success, but a failure with a deep path
pays that cost. The [evidence record](decoder-evidence.md) contains the measured allocation and
directional timing tables for the three consumer versions and for `Location` construction at
several depths. The measurements are evidence, not a promise of zero allocation.

## Deliberate Boundaries

- No schema, constraint introspection, derivation, generated test inputs, or general contract
  representation. That is a separate outcome ([Goal 0027](goals/0027-goal.md)), not a property of
  `Decoder<T>`.
- No serializer, JSON converter, replacement JSON DOM, reflection-based binder, or
  `System.Text.Json` serialization integration. `JsonElement` is the input view; the caller owns
  parsing and lifetime.
- No general effect runtime, retry policy, async input model, or new outcome carrier. `Validation`
  remains the accumulation carrier; `Option` remains the absence carrier.
- No exception-to-error conversion. Malformed JSON text, disposed documents, callback faults,
  cancellation, and arbitrary programming exceptions retain their normal exception identity.
- No field-level asynchronous validation. Async validation runs on the decoder's decoded value at
  its input root; field paths are a synchronous structural concern. Decode a field separately if it
  needs an asynchronous rule of its own.
- No unbounded object arity. `Object` supports 2-4 independent fields; wider records compose nested
  objects or use `Bind` for a dependent shape.
- No parallel or streaming decode. Lists are traversed once and materialized; this is an eager
  boundary like `Validation.Traverse`.
- No promise that `Decoder.Element` values remain valid after the source document is disposed.
- No automatic mapping to a caller's final error type. Map `DecodeError` with
  `Validation.MapErrors`; the comparison consumer does so with its own `OrderError` type.
