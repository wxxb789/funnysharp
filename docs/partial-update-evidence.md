# Partial Update Consumer Evidence

This record supports the maintainer's judgment for
[Goal 0028](goals/archive/0028-goal.md): whether typed, inspectable partial changes reduce complete
consumer code while preserving the distinctions between omission, explicit clearing, and
replacement, and while validating the candidate state rather than only the supplied fields. It is
comparison and measurement evidence, not a completion claim or a new gate.

Source identity is the Goal 0028 working tree on top of commit `af3a642`; the landing commit
becomes the durable identity. Measurements were taken on this host:

- Ubuntu 24.04.5 LTS, Linux 7.0.0-1017-azure, 2 logical cores.
- .NET SDK 10.0.403, runtime 10.0.14, x64.
- Date: 2026-10-31 (UTC).

The table below is one run on the reviewed tree after `492d063`; that commit changed the patch
decode path, so the earlier patch-consumer row was re-measured. Repeated runs reproduce the
allocation column within a few percent; timing varied by up to an order of magnitude with host
load and is directional only.

## Compared Consumers

Both consumers update the same `Schedule` record:

```text
Schedule(string TimeZone, TimeOnly OpensAt, TimeOnly ClosesAt, Option<string> Note)
```

The candidate contract requires a non-empty time zone, valid `HH:mm` times, a non-empty optional
note, and `OpensAt < ClosesAt`. The patch schema allows replacements for `timeZone`, `opensAt`,
and `closesAt`, plus explicit clearing for `note`. `timeZone` is deliberately replacement-only, so
`"timeZone": null` is an error rather than a silent clear.

| Version | File | Lines | Shape |
| --- | --- | ---: | --- |
| Patch | [PatchScheduleConsumer.cs](../tests/Shared/PatchComparison/PatchScheduleConsumer.cs) | 40 | `PatchSchema.Decode` -> `PatchSchema.Validate` -> `PatchApplication.Candidate`, with one `DecodeError` mapping loop. |
| Ordinary C# | [OrdinaryScheduleConsumer.cs](../tests/Shared/PatchComparison/OrdinaryScheduleConsumer.cs) | 212 | Manual `ValueKind` checks, allowed-field set, duplicate set, non-clearable null policy, `TimeOnly` parsing, candidate construction, and hand-written field and cross-field validation. |

The patch consumer's request path is about one fifth of the ordinary update path. The reusable
field/schema declaration in [ScheduleModels.cs](../tests/Shared/PatchComparison/ScheduleModels.cs)
adds roughly 108 lines of contract, lens, field, and schema setup. Counting that declaration plus
the consumer gives about 148 patch-specific lines versus 212 ordinary lines, while the patch side
also supplies candidate inspection, field explanations, and reusable rules. The shared domain,
error, and result records add 30 lines to both sides and are not counted as either implementation.
Library implementation and XML documentation are a one-time cost, not charged to the consumer.

## Equivalence

Both consumers run all 19 fixtures in
[ScheduleFixtures.cs](../tests/Shared/PatchComparison/ScheduleFixtures.cs):

`unchanged-and-replacement`, `explicit-clear`, `empty-object`, `rejected-field`,
`non-clearable-null`, `cross-field-invalid`, `cross-field-valid`, `accumulated-errors`,
`unknown-and-null`, `root-null`, `root-array`, `duplicate-field`, `same-value`,
`unchanged-invalid-note`, `unchanged-invalid-range`, `note-empty`, `time-zone-wrong-kind`,
`note-wrong-kind`, `note-replacement`.

The comparison test is
[PatchConsumerComparisonTests.cs](../tests/FunnySharp.Tests/PatchConsumerComparisonTests.cs).
It compares the success state, the complete candidate record, and the exact ordered application
errors for every fixture. It fails closed if either consumer disagrees. Fixed expected rows also
anchor the most important distinctions, so agreement alone cannot hide a shared wrong expectation:

| Fixture | Path | Code | Detail |
| --- | --- | --- | --- |
| `rejected-field` | `ownerId` | `UnsupportedField` | `the field is not an allowed change` |
| `non-clearable-null` | `timeZone` | `Null` | `a replacement value; clearing is not allowed` |
| `cross-field-invalid` | *(root)* | `Invalid` | `opensAt must be before closesAt` |
| `duplicate-field` | `timeZone` | `DuplicateField` | `the field is supplied more than once` |
| `note-empty` | `note` | `Empty` | `a non-empty string` |

## Rule Reuse

`ScheduleRules.TimeOfDay`, `TimeZone`, and `Note` are the same `Contract<T>` values used in two
places:

- A patch field derives its replacement decoder from the field contract.
- The complete candidate contract uses the same contracts for every field, including unchanged
  fields.

The cross-field rule exists only in the complete candidate contract. The patch does not
reimplement it. This is exercised by both cross-field fixtures and by
`unchanged-invalid-note` / `unchanged-invalid-range`, where a patch that changes only `timeZone`
must still report an invalid unchanged `note` or an invalid unchanged opening/closing range.

The schema also reports a field whose contract cannot derive a decoder as a
`ContractLimitation` with the field name as its path. The core schema test exercises an opaque
integer contract and asserts `ContractDerivation.Decoding` and path `capacity`.

## Boundary Examples

The core tests in [PatchTests.cs](../tests/FunnySharp.Tests/PatchTests.cs),
[PatchSchemaTests.cs](../tests/FunnySharp.Tests/PatchSchemaTests.cs), and
[PatchCombineTests.cs](../tests/FunnySharp.Tests/PatchCombineTests.cs) pin the behavior behind the
consumer fixtures:

- `default(Patch<T>)` is empty and `default(PatchChange<T>)` is `Unchanged`; `Clear` is a distinct
  state.
- `PatchChange.Set(null)` throws; explicit clearing does not use null.
- `Patch<T>.With` rejects an unchanged field, a repeated field, and a clear instruction on a replacement-only field.
- Empty and same-value applications disclose source/candidate identity without mutating the source.
- Setter and clear-callback exceptions propagate, and later callbacks do not run after a failure.
- Any repeated field in `Combine` is a conflict, including equal writes, and no partial merged
  patch is returned.
- A patch containing a field binding from another schema is rejected by `Validate`.
- Root null, root array, unknown fields, repeated fields, and non-clearable nulls produce distinct
  errors with stable paths.
- Candidate validation includes unchanged fields and the cross-field constraint.

## Measurements

Command:

```bash
dotnet run --project tests/FunnySharp.PatchMeasurements/FunnySharp.PatchMeasurements.csproj -c Release
```

The harness first runs the complete patch-vs-ordinary comparison, then measures the update paths.
Allocations use `GC.GetAllocatedBytesForCurrentThread`; timing is the minimum of three rounds and
is directional on this shared 2-core host.

| Operation | Changed fields | Allocated B/op | Time ns/op |
| --- | ---: | ---: | ---: |
| Patch consumer: decode + validate + apply | 3 | 849.8 | 8411.5 |
| Ordinary consumer: parse + validate + apply | 3 | 792.0 | 2405.4 |
| Patch validate + apply (predecoded) | 3 | 200.0 | 240.7 |
| Patch apply only (predecoded) | 3 | 168.0 | 170.9 |
| Patch apply + inspect changes | 3 | 320.0 | 483.0 |
| Ordinary direct `with` expression | 3 | 56.0 | 216.4 |
| Patch apply only (one field) | 1 | 56.0 | 181.5 |
| Ordinary direct `with` expression (one field) | 1 | 56.0 | 335.4 |
| Patch apply only (empty) | 0 | 0.0 | 87.4 |
| Ordinary identity (empty) | 0 | 0.0 | 307.7 |

The copying result is deliberate: each changed field invokes one immutable lens setter, so a
three-field patch allocates three candidate records where a single `with` expression allocates one.
A one-field patch allocates one candidate, and an empty patch allocates nothing and preserves the
exact source reference. `Changes` is materialized only when inspected; reading it boxes replacement
values and adds the inspection array.

The complete patch consumer allocates more than the hand-written consumer because it builds typed
patch entries and runs the complete contract validation. The evidence discloses that cost rather
than claiming a patch is cheaper at runtime. The consumer gain measured here is expression and
inspectability, not allocation.

## Deliberate Non-Guarantees

The evidence does not claim:

- a database transaction, row locking, uniqueness check, or concurrent-write guarantee;
- an optimistic-concurrency version check, ETag comparison, or stale-state rejection;
- an inverse patch or rollback;
- a universal merge policy;
- a System.Text.Json converter or automatic DTO generation.

The ASP.NET Core example binds `JsonElement`, calls the same decode and validate operations, maps
`DecodeError` values to a validation problem, and persists the candidate into its own in-memory
store. HTTP policy, storage, and concurrency remain application decisions.
