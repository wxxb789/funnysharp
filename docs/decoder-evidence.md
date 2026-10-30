# Decoder Consumer Evidence

This record supports the maintainer's judgment for
[Goal 0026](goals/archive/0026-goal.md): whether composable decoding improves expression and diagnosis
enough to accept the capability. It is comparison and measurement evidence, not a completion claim
or a new gate.

Source identity is the Goal 0026 working tree on top of commit `83a3175`; the landing commit becomes
the durable identity. Measurements were taken on this host:

- Ubuntu 24.04.5 LTS, Linux 7.0.0-1017-azure, 2 logical cores.
- .NET SDK 10.0.403, runtime 10.0.14, x64.
- Date: 2026-10-30 (CST).

## Compared Consumers

All three consumers produce the same `Order` domain records and the same caller-defined
`OrderError` list. They include domain construction, adapters, and error mapping. The shared models,
fixtures, and equality harness are separate so the comparison counts the consumer code itself.

| Version | Consumer file | Lines | Shape |
| --- | --- | ---: | --- |
| Decoder | [DecoderOrderConsumer.cs](../tests/Shared/DecoderComparison/DecoderOrderConsumer.cs) | 100 | `Decoder.Object`/`Field`/`List`/`NonEmptyList`/`OptionalField`/`Ensure`/`Validate`, then one `MapErrors` call. |
| Ordinary C# | [OrdinaryOrderConsumer.cs](../tests/Shared/DecoderComparison/OrdinaryOrderConsumer.cs) | 406 | Manual `ValueKind` checks, `TryGetProperty`/`TryGetGuid`/`TryGetInt32`/`TryGetDecimal`, nullable locals, `List<OrderError>`, and dot/bracket path strings. |
| Direct Validation | [ValidationOrderConsumer.cs](../tests/Shared/DecoderComparison/ValidationOrderConsumer.cs) | 369 | Existing `Validation.Zip` and `Validation.Traverse`, a local dependent `Then` helper, and manual path strings. |

The shared domain/error model, fixtures, and comparison harness add
[OrderModels.cs](../tests/Shared/DecoderComparison/OrderModels.cs),
[OrderFixtures.cs](../tests/Shared/DecoderComparison/OrderFixtures.cs), and
[OrderComparison.cs](../tests/Shared/DecoderComparison/OrderComparison.cs). Those are not counted
as any one consumer's implementation.

The decoder consumer is roughly one quarter of both baseline implementations (100 lines versus
406 and 369). The ordinary and Validation versions are deliberately complete, not strawmen: the
Validation version uses the carrier's applicative and traversal operations, and the ordinary
version uses the normal BCL parsing APIs.

## Equivalence

The three consumers agree on `Value`, `IsSuccess`, and the exact ordered `OrderError` list for all
16 fixtures:

`valid`, `valid-with-note`, `valid-empty-addresses`, `empty-note`, `missing-id`, `null-id`,
`wrong-kinds`, `invalid`, `empty-name-and-lines`, `missing-addresses`, `null-addresses`,
`non-object-customer`, `malformed-line-numbers`, `empty-email`, `root-null`, `root-array`.

The comparison test is
[DecoderConsumerComparisonTests.cs](../tests/FunnySharp.Tests/DecoderConsumerComparisonTests.cs).
It failed closed if any pair disagreed. It also anchors the main invalid fixture to explicit expected
errors, so agreement alone cannot hide a shared wrong expectation:

| Path | Code | Detail |
| --- | --- | --- |
| `orderId` | `Malformed` | `malformed` |
| `customer.name` | `Empty` | `empty` |
| `customer.email` | `Invalid` | `email must contain a local part and domain` |
| `customer.addresses[1].postalCode` | `Null` | `null` |
| `lines[1].sku` | `Empty` | `empty` |
| `lines[1].quantity` | `Invalid` | `quantity must be positive` |
| `note` | `Null` | `null` |

The decoder's native diagnostics for the same fixture, before mapping to `OrderError`, render as:

| Path | Native diagnostic |
| --- | --- |
| `orderId` | `orderId: expected a GUID string (the value cannot be parsed as a GUID string)` |
| `customer.name` | `customer.name: expected a non-empty string` |
| `customer.email` | `customer.email: email must contain a local part and domain` |
| `customer.addresses[1].postalCode` | `customer.addresses[1].postalCode: value must not be null; expected a non-empty string` |
| `lines[1].sku` | `lines[1].sku: expected a non-empty string` |
| `lines[1].quantity` | `lines[1].quantity: quantity must be positive` |
| `note` | `note: value must not be null; expected a string` |

The ordinary and direct Validation consumers produce the same application code and detail in
`OrderError`; the decoder's native form additionally carries the expected shape. The shared
`OrderError` mapping deliberately reduces the native text to an application code and a stable
detail for exact three-way comparison.

Boundary behavior is pinned separately in the decoder tests:

- Missing, explicit null, wrong kind, malformed text, and empty string/array produce distinct
  `DecodeErrorKind` values.
- `Field`, `OptionalField`, `NullableField`, and `OptionalValue` implement the documented missing
  and null matrix.
- Duplicate property names resolve to the first matching property in document order across the
  field combinators; repeated nested segments such as `a.a` and `[0][0]` are preserved.
- `Double` rejects JSON numbers that parse to a non-finite value, such as `1e400` and `-1e400`.
- Empty addresses are valid under `List`; empty lines are `Empty` under `NonEmptyList`.
- `Object` checks the object kind once and skips field decoders for a non-object input.
- Independent field and element errors accumulate; `Object` combine, `Bind`, `Ensure`, and
  `Validate` do not run after a prerequisite failure.
- The same decoder instance serves two field locations and concurrent valid/invalid decodes
  without leaking paths or errors.
- A disposed `JsonDocument`, a throwing `Map`/`Bind`/`Ensure`/`Validate`/`From`/`Object` combine,
  a binder returning null, and a successful null wrapped by `Option.Some` all preserve their
  programming-exception identity.
- `DecodeAsync`/`DecodeValueAsync` invoke the validation callback once after success, skip it after
  a decode failure without inspecting the token, forward the exact token, propagate cancellation,
  faults, and synchronous callback throws, and reject a null task as a programming error.

## Measurements

Command:

```bash
dotnet run --project tests/FunnySharp.DecoderMeasurements/FunnySharp.DecoderMeasurements.csproj -c Release
```

The harness first runs the three-way equivalence check, then measures the complete mapped operation.
Each implementation decodes the same already-parsed fixture repeatedly. Allocations are measured
with `GC.GetAllocatedBytesForCurrentThread`; timing is the minimum of three rounds and is
directional on this shared 2-core host.

| Implementation | Case | Allocated B/op | Time ns/op |
| --- | --- | ---: | ---: |
| decoder | valid | 1144.0 | 5683.1 |
| decoder | invalid | 7288.0 | 14947.0 |
| ordinary | valid | 1704.0 | 6794.8 |
| ordinary | invalid | 2656.0 | 3985.3 |
| validation | valid | 2152.9 | 7586.8 |
| validation | invalid | 3840.0 | 9941.2 |

Allocation rows were stable across repeated runs, apart from roughly 100 B of variation on the
Validation rows. Timing was not stable: across two runs ordinary valid measured 3.8-6.8 us,
validation valid 3.7-7.6 us, and decoder valid 5.6-5.7 us; decoder invalid measured 9.5-14.9 us.
The allocation comparison is the decisive evidence here; no timing winner is claimed.

Observed trade-off:

- On the successful path the decoder allocated fewer bytes than both baselines. It does not
  materialize diagnostic location prefixes for a successful decode.
- On the invalid path the decoder allocated about 2.7 times the ordinary implementation and 1.9
  times the direct Validation implementation. The difference is `DecodeError` objects, the
  experimental `Location` path, intermediate composed prefixes, and the caller's mapped error
  objects. This is the measured cost of automatic structured diagnosis.
- The ordinary and direct Validation versions build path strings only for errors they report, but
  they require explicit path formatting, kind checks, and per-field null/parse handling in every
  consumer.

## Location Cost

`Location` is an immutable class. Each `At`/`Property`/`Key` allocates a new location and copies the
previous segment array; `Nest` composes another path. The measured construction cost grows
superlinearly with path depth, which is the copying behavior the design must not hide:

| Path depth | Construction B/op | Construction ns/op | Render-inclusive B/op | Render-inclusive ns/op |
| --- | ---: | ---: | ---: | ---: |
| 1 | 80.0 | 138.6 | 208.0 | 128.6 |
| 2 | 184.0 | 235.6 | 320.0 | 364.7 |
| 4 | 464.0 | 677.5 | 608.0 | 774.0 |
| 8 | 1312.0 | 976.6 | 1584.0 | 1173.4 |
| 16 | 4160.0 | 2896.8 | 4616.0 | 1819.0 |

The construction bytes grow faster than doubling as depth doubles (80, 184, 464, 1312, 4160),
which is consistent with repeated segment-array copying rather than a constant-size append.
Rendering adds the cached string and builder work. Timing for this table was also noisy; the byte
growth is the durable observation.

Conclusion for Goal 0026: the experimental `Location` surface can honestly represent the decoder's
field and element paths, but it is not allocation-free and its per-segment copying is material at
depth. The decoder therefore keeps `DecodeError.Location` experimental while `DecodeError.Path` is
the stable contract. A future promotion or representation change of `Location` is a separate
decision; this goal does not assume one.

## Stability And Surface

`Decoder`, `Decoder<T>`, `DecodeError`, and `DecodeErrorKind` are stable additions.
`DecodeError.Location` is the only new experimental member (`FS0017`) and is tracked in
[stability-inventory.md](stability-inventory.md). The stable `Path` property renders the same
information without requiring a suppression.

The implementation adds no runtime package dependency. `System.Text.Json` is the BCL input view on
the supported .NET 10 baseline; the core package still has no package dependency group and no
serializer integration.

## Verification Runs

| Gate | Result |
| --- | --- |
| Full solution build (`dotnet build FunnySharp.slnx`) | 0 warnings, 0 errors |
| Core xUnit suite (`dotnet test tests/FunnySharp.Tests`) | 1812 passed, 0 failed, 0 skipped |
| Full solution tests (`dotnet fsi build.fsx -- -p test`) | 2346 passed, 1 pre-existing harness failure at the goal-completion head; the review-fix tests are covered by the current core and decoder rows below |
| PR gate harness build (`dotnet build eng/harness/FunnySharp.Harness.fsproj -p:RestoreLockedMode=true`) | 0 warnings, 0 errors |
| PR gate docs snippets (`verify-docs-snippets`) | 56 snippets across 11 primary guides |
| PR gate action pins (`check-action-pins`) | 2 workflows checked; all actions SHA-pinned |
| Decoder test slice (`FullyQualifiedName~Decoder`) | 50 passed, 0 failed, 0 skipped |
| Example consumer (`dotnet run --project examples/FunnySharp.Examples -c Release`) | `FunnySharp examples passed.` |
| Docs snippets (`verify-docs-snippets`) | 56 snippets across 11 primary guides |
| Public API baseline (`verify-api-baseline`) | 53 types and 544 members across 2 assemblies |
| Format (`-p format`) | exit 0; F# projects are not C# formatter targets |

The measurement harness is run by the command above; it also executes the three-way equivalence
check before printing its tables.

The full solution test run has one pre-existing, unrelated failure:
`ReleaseProtocolTests.ReleaseWorkflow_HasRequiredContextsAndShaPinnedActions` expects a
`tooling-gate` context in `release.yml`, which the unchanged workflow does not contain. This goal
did not modify `.github/workflows/`; the core, analyzer, and ASP.NET Core test projects all passed
in the same run.

## Residual Items

- The new shipping source files were added to both performance manifests' `benchmarkInput.files`
  because the manifest coverage test requires every shipping source. That changes the committed
  benchmark input fingerprint. The committed performance observation is therefore stale until a
  full benchmark re-record is explicitly authorized; this goal did not start the heavy benchmark
  suite. `verify-performance` and the release gate remain open until that re-record lands.
- The public API baseline now includes the stable decoder surface and the experimental
  `DecodeError.Location`; `verify-api-baseline` passes.
- The maintainer's completion directive is recorded in the decision section below.

## Maintainer Decision

On 2026-10-30 the maintainer directed completion of Goals 0024, 0025, and 0026 in one pull
request. That directive is the acceptance judgment for Goal 0026: the stable decoder surface with
`DecodeError.Location` remaining experimental is accepted, and the decoder/Location revision
alternatives in the prior question were not selected. The evidence in this record remains the
basis for that judgment; the performance observation re-record remains release-preparation work,
not a new decoder scope.

The evidence supports a real expression gain: a nested order decoder is 100 lines versus 406 for
the ordinary version and 369 for the direct Validation version, with automatic deterministic paths
and a clear missing/null/empty policy. The cost is concentrated on invalid input: the decoder's
structured diagnostics allocate more than either baseline, and `Location` copying grows with path
depth. Successful decoding allocates less, but timing did not show a stable speed advantage.
