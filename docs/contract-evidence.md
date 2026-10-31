# Contract Consumer Evidence

This record supports the maintainer's judgment for
[Goal 0027](goals/archive/0027-goal.md): whether one inspectable contract description can honestly drive
validation, decoding, explanation, schema metadata, generated test inputs, and shrinking, and
whether the consumer gain justifies the runtime and generation costs. It is comparison and
measurement evidence, not a completion claim or a new gate.

Source identity is the Goal 0027 working tree on top of commit `fb2000e`; the landing commit
becomes the durable identity. Measurements were taken on this host:

- Ubuntu 24.04.5 LTS, Linux 7.0.0-1017-azure, 2 logical cores.
- .NET SDK 10.0.403, runtime 10.0.14, x64.
- Date: 2026-10-31 (UTC).

## Compared Consumers

Both consumers produce the same nested `Order` domain records: a `Customer` with an optional note,
a non-empty list of `OrderLine` records, and a labelled `Payment` alternative. The contract
consumer declares the constraints once and reuses them for every interpretation; the ordinary
consumer implements decoding, validation, generation, shrinking, and a schema-facts description by
hand.

| Version | Consumer file | Lines | Shape |
| --- | --- | ---: | --- |
| Contract | [ContractOrderConsumer.cs](../tests/Shared/ContractComparison/ContractOrderConsumer.cs) | 327 | One nested contract: fields, ranges, cardinality, optional field, labelled alternatives, recursion-independent cross-field rule, opaque email rule with caller-supplied decoder/generator/shrinker, and `Widen` for payment cases. |
| Ordinary C# | [OrdinaryOrderConsumer.cs](../tests/Shared/ContractComparison/OrdinaryOrderConsumer.cs) | 693 | Manual `ValueKind` checks, `TryGetProperty`/`TryGetGuid`/`TryGetInt32`/`TryGetDecimal`, repeated range and cardinality checks, a hand-written generator, a hand-written shrinker, and a hand-written schema-fact list. |

The shared domain model and fixtures add
[OrderModels.cs](../tests/Shared/ContractComparison/OrderModels.cs) (194 lines) and the comparison
harness adds [OrderComparison.cs](../tests/Shared/ContractComparison/OrderComparison.cs) (333
lines); those are not counted as either consumer's implementation. The contract consumer is about
47% of the ordinary consumer's size while covering the same five uses.

The ordinary file is not one reuse of a shared description; it contains five separate hand-written
implementations:

| Use | Contract consumer | Ordinary consumer |
| --- | --- | --- |
| Decoding | `OrderContract.CreateDecoder()` | `Decode` plus field/element helpers |
| Validation | `OrderContract.Validate(order)` | `Validate`, `ValidateCustomer`, `ValidateLines`, `ValidatePayment` |
| Generation | `OrderContract.Generate(options)` | `Generate` |
| Shrinking | `OrderContract.Shrink(order)` | `Shrink`, `ReplaceLine` |
| Schema facts | `DescribeFacts` traversal of `ContractDescription` | A literal list repeating every field, range, and cardinality fact |

The ordinary baseline is deliberately complete, not a strawman: it mirrors the contract's
missing/null/wrong-kind/malformed/empty policies, accumulates the same ordered errors, respects the
same ranges and cardinality, generates valid orders, shrinks only valid candidates, and describes the
same structural facts. The contract library implementation itself is a one-time library cost
(`src/FunnySharp/Contract*.cs`, about 4,200 lines including XML documentation); it is not charged to
each consumer.

## Equivalence

The two consumers agree on every fixture in
[OrderModels.cs](../tests/Shared/ContractComparison/OrderModels.cs):

`valid`, `valid-cash`, `invalid-boundaries`, `invalid-values`, `total-exceeded`, `missing-fields`,
`wrong-kinds`, `root-null`, `root-array`, `note-null`, `line-null`, `lines-too-many`, and
`payment-method-missing`.

Agreement is not the only evidence. Fixed expected `OrderError` rows anchor seven fixtures so a
shared mistaken expectation cannot pass silently. The anchored rows include the cross-field total
error at the empty root path, nested alternatives (`payment.card.last4`,
`payment.card.currency`), collection cardinality (`lines`), optional-null policy
(`customer.note`), and independent field accumulation.

Typed-value validation is also compared for four cases: a valid order, an order whose nested values
violate empty/range/cardinality rules, an order whose line total exceeds the cross-field rule, and an
order whose payment subtype is not one of the labelled alternatives. Contract and ordinary
validation produce the same ordered paths, codes, and details.

Generation and shrinking are checked against both implementations:

- For seeds 1-10, every contract-generated order and every ordinary-generated order passes both
  validators and the independent `Total <= 1000` property. The ordinary implementation is the
  reference validator, not merely a second artifact derived from the same contract.
- Every contract shrink candidate and every ordinary shrink candidate passes both validators and
  differs from the input. Contract shrinking produced 19 candidates for the measured valid order;
  ordinary shrinking produced 8. The contract explores every field and nested value; the ordinary
  baseline uses a smaller fixed set.
- The reversed direction is also exercised: generated values are validated by the ordinary
  implementation and the fixed expected fixtures pin the business rules, so agreement among
  generated artifacts is not treated as proof of correctness.

Schema facts agree exactly between the contract's description traversal and the ordinary
hand-written list. The structural `OrderLine` contract has a complete schema; the full `Order`
contract reports an incomplete schema because the email rule is opaque, the card suffix is a
refinement, and the line-total rule is a cross-field refinement. That incompleteness is explicit,
not silently omitted.

## Unsupported Derivations And Counterexamples

The tests exercise the honest limits rather than only the happy path:

- An opaque even-integer contract reports four limitations: decoding, generation, shrinking, and
  schema. It has no decoder, generator, shrinker, or complete schema. Each failure names the matching
  extension method (`WithDecoder`, `WithGenerator`, `WithShrinker`, `WithSchema`). Supplying all four
  restores every capability, and the supplied decoder still runs contract validation.
- A range refined with `Ensure(value >= 5)` keeps validation, decoding, and shrinking but reports
  generation and schema as unavailable. Generation fails with an explicit limitation; it never
  performs rejection sampling. Shrinking returns only candidates that satisfy the predicate.
- A caller-supplied generator that returns a violating value fails with the contract's validation
  messages. A caller-supplied shrinker's invalid or duplicate candidates are filtered out.
- A one-of contract without an input selector reports decoding as unavailable with a root-path
  limitation naming `WithDecoder`.
- A recursive contract reports its schema as incomplete because the recursive body is represented by
  name. Generation terminates within `MaxDepth` and `MaxNodes`; a self-recursive contract with no
  terminal structure fails at the depth budget rather than looping.
- A declared minimum collection length larger than the generation width budget fails explicitly; a
  non-empty string with a zero width budget fails explicitly; a node-budget exhaustion fails
  explicitly.
- Duplicate object field names, invalid ranges, inconsistent collection bounds, empty alternative
  sets, duplicate alternative labels, and empty recursion names are rejected at construction.

The explanation text reports these capabilities and limitations deterministically, and the
description tree marks every opaque, refined, derived, and recursive node.

## Diagnostics

The contract decoder maps native `DecodeError` values to the application's `OrderError` code and
detail for exact comparison. For the `invalid-values` fixture the native diagnostics are:

| Path | Native diagnostic |
| --- | --- |
| `lines[0].sku` | `lines[0].sku: expected a non-empty string` |
| `lines[0].quantity` | `lines[0].quantity: value must be between 1 and 100` |
| `lines[0].unitPrice` | `lines[0].unitPrice: value must be between 0.01 and 5000` |
| `payment.card.last4` | `payment.card.last4: last4 must contain exactly four digits` |
| `payment.card.currency` | `payment.card.currency: expected a non-empty string` |

For `invalid-boundaries` the native paths are `id`, `customer.name`, `customer.email`,
`customer.note`, `lines`, and `payment`; the kinds are malformed, empty, invalid, null, empty, and
invalid respectively. The ordinary consumer reports the same paths, codes, and details through its
own error list. The contract's diagnostics are deterministic and reuse the Goal 0026
`DecodeError.Kind`/`Expected`/`Detail` model; no second decoder error model is introduced.

## Measurements

Command:

```bash
dotnet run --project tests/FunnySharp.ContractMeasurements/FunnySharp.ContractMeasurements.csproj -c Release
```

The harness first runs the two-way equivalence, generation, shrinking, schema, and capability checks,
then measures complete operations. Allocations use `GC.GetAllocatedBytesForCurrentThread`; timing is
the minimum of three rounds and is directional on this shared 2-core host.

| Operation | Implementation | Allocated B/op | Time ns/op |
| --- | --- | ---: | ---: |
| decode valid | contract | 752.0 | 15645.2 |
| decode valid | ordinary | 1112.0 | 3275.1 |
| decode invalid | contract | 4512.0 | 12819.8 |
| decode invalid | ordinary | 1184.0 | 34566.0 |
| validate valid | contract | 72.0 | 1144.2 |
| validate valid | ordinary | 112.0 | 1227.6 |
| validate invalid | contract | 7064.0 | 8800.7 |
| validate invalid | ordinary | 896.0 | 986.5 |
| generate one | contract | 792.0 | 3212.2 |
| generate one | ordinary | 688.0 | 2001.8 |
| generate ten | contract | 5792.0 | 30251.4 |
| generate ten | ordinary | 8480.0 | 35310.3 |
| shrink one | contract | 10054.6 | 36280.5 |
| shrink one | ordinary | 2888.0 | 9506.9 |
| schema facts | contract | 4440.1 | 5162.1 |
| schema facts | ordinary | 232.0 | 149.4 |
| explain | contract | 0.0 | 12.7 |

Observed trade-offs:

- On a successful decode the contract allocated fewer bytes than the ordinary implementation
  (752 vs 1112). Description and capability trees are cached and are not built on the decode path;
  decoded values are rechecked against their field contract, and that check allocates nothing on a
  valid object.
- On an invalid decode the contract allocated about 3.8 times the ordinary implementation. The
  difference is the structured `DecodeError` values, `Location` path composition, and the ordered
  error list. This is the measured cost of automatic structured diagnosis.
- Successful validation costs 72 B vs 112 B; invalid validation costs 7064 B vs 896 B for the same
  diagnostic content. The contract constructs `DecodeError` and path objects rather than application
  codes and strings.
- One generated order costs 792 B vs 688 B. Ten generated orders cost 5792 B vs 8480 B because the
  contract's cached description/capability state is amortized across the samples; every generated
  value is checked against the full contract before it is returned.
- One shrink step costs 10055 B vs 2888 B while producing 19 vs 8 candidates. Per candidate the
  cost is roughly 529 B vs 361 B; the contract explores every field and nested value and validates
  every candidate. This is useful for finding a smaller counterexample, not a promise of a cheaper
  fixed candidate set.
- Schema facts cost 4440 B vs 232 B because the contract consumer traverses the cached description
  and formats paths; the ordinary baseline returns a literal list. The library caches the
  description itself, and repeated `Explain()` calls allocate nothing after the first call.

Timing was not stable across runs; the allocation columns are the durable observation. No speed
winner is claimed.

## Stability And Surface

`Contract`, `Contract<T>`, contract field/alternative types, the description tree, capabilities,
limitations, derivation enum, and generation options are stable additions recorded in the
[stability inventory](stability-inventory.md). No new experimental member is introduced. The
existing experimental `Location` accessor on `DecodeError` remains experimental.

The implementation adds no runtime package dependency. `System.Random`, `System.Text.Json`, and
`IReadOnlyList<T>` are platform types; no test framework, compiler package, reflection binder, or
serializer enters the core package. Contract validation and decoding return the existing
`Validation<T, DecodeError>` carrier; the ASP.NET Core test maps contract validation through the
existing `Validation.ToHttpResult` boundary.

## Verification Runs

| Gate | Result |
| --- | --- |
| Full solution build (`dotnet build FunnySharp.slnx`) | 0 errors, 0 warnings. |
| Core xUnit suite (`dotnet test tests/FunnySharp.Tests`) | 1852 passed, 0 failed, 0 skipped. |
| Contract test slice (`Contract*`) | 33 passed across the four contract test classes. |
| ASP.NET Core suite (`dotnet test tests/FunnySharp.AspNetCore.Tests`) | 29 passed, 0 failed, 0 skipped; includes `ContractHttpResultTests`. |
| Example consumer (`dotnet run --project examples/FunnySharp.Examples -c Release`) | `FunnySharp examples passed.` |
| Contract measurement harness (Release) | Equivalence, generation, shrinking, schema, and capability checks passed; tables above. |
| Docs snippets (`verify-docs-snippets`) | 56 snippets across 11 primary guides. |
| Public API baseline (`verify-api-baseline`) | 67 types and 667 members across 2 assemblies; baseline regenerated for the stable types. |
| Format (`-p format`) | Exit 0 for C# projects; the two F# projects are not formatter targets. |

## Residual Items

- The new shipping source files were added to both performance manifests' `benchmarkInput.files`
  because the manifest coverage test requires every shipping source. That changes the committed
  benchmark input fingerprint. This goal did not start the heavy benchmark suite; the committed
  performance observation remains stale until a full benchmark re-record is explicitly authorized,
  exactly as recorded for Goal 0026. `verify-performance` and the release gate remain open.
- The public API baseline now includes the stable contract surface; `verify-api-baseline` passes.
- Timing remains directional until a fixed-hardware runner exists; allocation budgets remain the
  blocking performance evidence.

## Maintainer Evaluation

The maintainer judges acceptance from the consumer comparison, the fixed expected diagnostics, the
generated-value properties, the unsupported-derivation counterexamples, and the disclosed runtime and
generation costs. This record supplies that evidence; agreement between contract-derived artifacts is
not presented as proof of a business invariant.
