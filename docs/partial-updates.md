# Partial Updates

`FunnySharp.Patch<T>` describes an allowed partial update as typed, ordered field changes.
`PatchChange<T>` keeps omission, an explicit clearing instruction, and a replacement value
distinct; `PatchSchema<T>` decodes a JSON object into a patch, validates the complete candidate
against a `Contract<T>`, and applies the changes to a caller-supplied current value. The
compiling example is
[examples/FunnySharp.Examples/PatchExamples.cs](../examples/FunnySharp.Examples/PatchExamples.cs);
the consumer comparison and measured costs are in
[partial-update-evidence.md](partial-update-evidence.md).

A patch is a description, not a carrier and not a persistence command. It composes with the
existing `Decoder`, `Contract`, `Validation`, `Lens`, and `Option` vocabulary. It does not
execute storage, open a transaction, or promise that an update is safe under concurrent writers.

## API Shape

```csharp
using System.Text.Json;
using FunnySharp;

var patchFields = Patch.For<Schedule>();
var contractFields = Contract.For<Schedule>();

var closesAt = Lens.Create<Schedule, int>(
    static value => value.ClosesAt,
    static (value, next) => value with { ClosesAt = next });
var note = Lens.Create<Schedule, Option<string>>(
    static value => value.Note,
    static (value, next) => value with { Note = next });

var scheduleContract = Contract.Object(
        contractFields.Field("timeZone", Contract.NonEmptyString, static value => value.TimeZone),
        contractFields.Field("opensAt", Contract.Int32Range(0, 1440), static value => value.OpensAt),
        contractFields.Field("closesAt", Contract.Int32Range(0, 1440), static value => value.ClosesAt),
        contractFields.OptionalField("note", Contract.NonEmptyString, static value => value.Note),
        static (timeZone, opensAt, closesAt, note) =>
            new Schedule(timeZone, opensAt, closesAt, note))
    .Ensure(static value => value.OpensAt < value.ClosesAt, "opensAt must be before closesAt");

var created = PatchSchema<Schedule>.Create(
    scheduleContract,
    patchFields.Field("closesAt", Contract.Int32Range(0, 1440), closesAt),
    patchFields.ClearableField(
        "note",
        Contract.Optional(Contract.NonEmptyString),
        note,
        static value => value with { Note = Option<string>.None }));

if (created.TryGetError(out var limitation))
{
    throw new InvalidOperationException(limitation.ToString());
}

_ = created.TryGetValue(out var schema);

var current = new Schedule("Europe/Paris", 9 * 60, 17 * 60, Option.Some("front desk"));
using var document = JsonDocument.Parse("""{"closesAt":1080,"note":null}""");

var decoded = schema.Decode(document.RootElement);
if (!decoded.TryGetValue(out var patch))
{
    _ = decoded.TryGetErrors(out var decodeErrors);
    throw new InvalidOperationException(string.Join("; ", decodeErrors!));
}

var validated = schema.Validate(patch, current);
if (!validated.TryGetValue(out var application))
{
    _ = validated.TryGetErrors(out var validationErrors);
    throw new InvalidOperationException(string.Join("; ", validationErrors!));
}

Console.WriteLine(application.Candidate.ClosesAt); // 1080
Console.WriteLine(application.Candidate.Note.IsNone); // True

internal sealed record Schedule(
    string TimeZone,
    int OpensAt,
    int ClosesAt,
    Option<string> Note);
```

The schema is immutable and reusable. `Create` returns a `Result<PatchSchema<T>, ContractLimitation>`
because each field's replacement decoder is derived from its `Contract<TValue>`; a field whose
contract cannot derive a decoder is reported with the field name as the limitation path.

## Three Change States

`PatchChange<T>` is a `readonly struct` with three states:

| State | Default | Meaning | Applied by |
| --- | --- | --- | --- |
| `Unchanged` | Yes | The property was absent; keep the current field. | No callback |
| `Set(value)` | No | The property carried a replacement value. | The field's `Lens.Set` |
| `Clear` | No | The property was an explicit JSON null on a clearable field. | The field's clear callback |

`Set` rejects a runtime-null value with `ArgumentNullException`; it does not silently become
`None` or a clearing instruction. This preserves `Option.Some`'s existing null contract. Use
`Clear` to remove a value, or make the field's domain type `Option<T>` and replace it with
`PatchChange<Option<T>>.Set(Option.None)` when absence is the domain value rather than a wire
clearing instruction.

`Patch<T>.With(field, change)` records a `Set` or `Clear`; an `Unchanged` change and a repeated
field name are rejected eagerly. `default(Patch<T>)` is the empty patch, and
`default(PatchChange<T>)` is `Unchanged`.

## Allowed Fields And Wire Semantics

`PatchSchema<T>.Decode(JsonElement)` reads one JSON object. Names are case-sensitive.

| Input | Patch state | Error |
| --- | --- | --- |
| Property absent | `Unchanged` for that field | None |
| Property value `null`, field clearable | `Clear` | None |
| Property value `null`, field replacement-only | None | `Null`, expected `a replacement value; clearing is not allowed` |
| Property has a non-null value | `Set(decoded)` | Field decode errors use the contract's `Decoder` and field path |
| Property name is not an allowed field | None | `UnsupportedField`, path is the rejected name |
| Property name occurs more than once | First occurrence is decoded; later occurrences are rejected | `DuplicateField` |
| Object has no properties | Empty patch | None |
| Root is `null` | None | `Null` at the root |
| Root is not an object | None | `WrongKind` at the root |

Field decode errors accumulate in document order. An unknown field does not stop another field
from being decoded. An empty object is a valid empty patch; applying it preserves the source
reference identity.

`PatchSchema<T>.Fields` exposes the allowed names, value descriptions, and clearing policy.
`Explain()` renders those fields and the candidate contract rules deterministically.

## Candidate Validation

`PatchSchema<T>.Validate(patch, current)` first applies the patch to `current`, then validates the
complete candidate with `PatchSchema<T>.CandidateContract`. It does not validate only the supplied
fields.

This matters for two cases:

- An unchanged field can already violate its rule; the candidate is still invalid.
- A cross-field rule may depend on both a submitted field and an existing field. For example, a
  patch that changes only `opensAt` is checked against the existing `closesAt`.

A successful validation returns `PatchApplication<T>` with the exact `Source`, the `Candidate`,
and the recorded `Changes`. A failed validation returns every error from the full candidate.
`PatchSchema<T>.Validate` also rejects a programmatic patch containing a field binding that does
not belong to the schema, and `Patch<T>.With` rejects a clearing instruction on a replacement-only
field.

## Applying A Change

`Patch<T>.ApplyTo(source)` invokes each recorded field's `Lens.Set` or clear callback once, in
insertion order, and returns `PatchApplication<T>`.

- It creates a candidate value. It does not mutate or clone the source itself. A record `with`
  setter creates a new value; a caller-supplied setter that mutates its input is caller policy.
- An empty patch invokes no callback and returns the exact source reference as the candidate.
- Each immutable setter receives the candidate produced by the previous setter, so applying a
  patch copies once per changed field. A single ordinary `with` expression copies once for all
  fields; the patch trades that copy for reusable typed field composition.
- A setter, clear callback, or contract predicate that throws propagates unchanged. No application
  result is returned and earlier callbacks are not undone. There is no hidden rollback.
- Validation applies to the exact `source` argument. The schema reads no storage, snapshots no
  version, and checks no ETag or rowversion. A caller that needs optimistic concurrency must
  compare its own version before validating, or include the version in the candidate contract.
  Database uniqueness, concurrent writes, and external side effects remain subject to the real
  storage or execution guarantee.

`Patch<T>.ApplyTo` performs no validation; it is the preview/application primitive. Callers that
need a trustworthy candidate use `PatchSchema<T>.Validate`, which applies the same patch and then
checks the complete candidate against `CandidateContract`. Both paths return
`PatchApplication<T>`, so the same candidate and change view can be inspected before persistence.

`PatchApplication<T>.IsIdentity` compares source and candidate with
`EqualityComparer<T>.Default`. `Changes` is an inspection view; reading it materializes an array
and boxes replacement values.

## Combining And Conflicts

`Patch<T>.Combine(other)` returns `Validation<Patch<T>, PatchConflict>`. Any field name present in
both operands is a conflict, even when both writes carry the same value. A conflicted combine
returns every conflict and no partially merged patch. There is no last-writer-wins rule, no
universal merge policy, and no implicit "merge patch" semantics.

The patches do not retain the previous field values. They therefore provide no inverse change and
no rollback promise. If a caller needs rollback, prior values and transactional execution must
come from its storage or execution layer.

## Defaults, Equality, And Null

| Value | Default behavior | Equality |
| --- | --- | --- |
| `Patch<T>` | Empty patch | Ordered field bindings, change states, and replacement values; field-binding identity is part of equality |
| `PatchChange<T>` | `Unchanged` | State plus `EqualityComparer<T>.Default` for a `Set` value; `Unchanged` and `Clear` ignore the default payload |
| `PatchEntry` | Empty name and `Unchanged` | Name, state, and boxed value using the value's own equality |
| `PatchConflict` | Constructed only by a combine | Field name and both entries |
| `PatchApplication<T>` | Empty patch and default source/candidate | No value equality |

A patch built from separately constructed but equivalent field descriptors is not guaranteed to
be equal to one built from the original descriptors. Replacement values that are arrays or mutable
collections use their own equality, which is normally reference equality. A `PatchChange<T>.Set`
value must not be null; the clearing state is the distinct way to remove a value.

## Request Binding And HTTP

The core package stays serializer-free. `PatchSchema<T>.Decode` accepts a parsed
`System.Text.Json.JsonElement`, so an ASP.NET Core endpoint can bind the request body as
`JsonElement`, decode it, validate the candidate, and map `DecodeError` values through the
existing `Validation<...>.ToHttpResult` boundary. See the patch endpoint in
[examples/FunnySharp.AspNetCore.Examples/Program.cs](../examples/FunnySharp.AspNetCore.Examples/Program.cs).

The wire contract is explicit:

- Missing property means unchanged.
- `null` means clear only for a clearable field.
- A non-null value is a replacement decoded by the field's contract.
- Unknown and repeated properties are errors.
- Error paths are the stable `DecodeError.Path` values.

There is no converter, reflection binder, global exception handler, or HTTP policy in the core
package. The application chooses status codes, problem details, storage, and concurrency policy.

## Performance

Measured on the host recorded in [partial-update-evidence.md](partial-update-evidence.md), the
three-field schedule update shows:

| Operation | Changed fields | Allocated B/op |
| --- | ---: | ---: |
| Patch consumer: decode + validate + apply | 3 | 1106.7 |
| Ordinary consumer: parse + validate + apply | 3 | 792.0 |
| Patch validate + apply (predecoded) | 3 | 200.0 |
| Patch apply only (predecoded) | 3 | 168.0 |
| Ordinary direct `with` expression | 3 | 56.0 |
| Patch apply only (one field) | 1 | 56.0 |
| Patch apply only (empty) | 0 | 0.0 |

The patch application copies once per changed field because it invokes one immutable lens setter
per change. It does not materialize the inspection `Changes` array unless read. Decoding allocates
the typed patch entries and the decoder's validation state; candidate validation reuses the
contract's ordinary validation path. Timing is directional and belongs to the evidence record.

## Deliberate Boundaries

- No RFC 6902 JSON Patch or RFC 7386 JSON Merge Patch implementation, operation vocabulary, or
  path language. This is a typed allowed-field change, not a generic document diff.
- No serializer, reflection binder, DTO generator, or universal application schema. The caller
  supplies field names, contracts, lenses, and clear callbacks.
- No persistence, unit of work, transaction, optimistic-concurrency token, retry, or rollback
  engine. Those belong to the storage or execution boundary.
- No automatic merge policy or inverse. Combining reports conflicts; prior values are not
  retained.
- No second absence carrier and no weakening of `Option.Some`'s null contract. Clearing is a patch
  instruction, not a null replacement.
- No global HTTP policy. The optional ASP.NET Core example uses the existing caller-selected
  result mapping.
