# Domain Value Consumer Evidence

This record supports the maintainer's judgment for
[Goal 0030](goals/archive/0030-goal.md): whether tagged domain values, explicit state types, and typed
error composition reduce complete workflow code while preserving the distinctions that matter.
It is comparison and measurement evidence, not a completion claim or a new gate.

Source identity is the Goal 0030 working tree on top of commit `3381ae8`; the landing commit
becomes the durable identity. Measurements were taken on this host:

- Ubuntu 24.04.5 LTS, Linux 7.0.0-1017-azure, 4 logical cores.
- .NET SDK 10.0.401, runtime 10.0.12, x64.
- Date: 2026-11-04 (UTC).

The allocation column is stable across repeated runs within a few percent. Timing is directional
on this shared host.

## Compared Consumers

Both consumers run the same order workflow over the same fixture set:

1. Decode a request containing an order identifier, customer identifier, one line (SKU and
   quantity), and a payment token.
2. Call shared pricing, inventory, and payment modules.
3. Produce a placed state, then ship it with a tracking code.
4. Preserve module-specific failures and decode diagnostics for comparison.

| Version | Files | Raw lines | Shape |
| --- | --- | ---: | --- |
| FunnySharp domain values and explicit state | [DomainModels.cs](../tests/Shared/DomainComparison/DomainModels.cs), [FunnySharpOrderWorkflow.cs](../tests/Shared/DomainComparison/FunnySharpOrderWorkflow.cs) | 360 | Five tagged identifiers with derived contracts/decoders; sealed `DraftOrder`, `PlacedOrder`, `ShippedOrder`, `CancelledOrder`; module error records mapped to a workflow hierarchy. |
| Ordinary records and manual adapters | [OrdinaryOrderWorkflow.cs](../tests/Shared/DomainComparison/OrdinaryOrderWorkflow.cs) | 451 | Raw `string`/`int` fields; one record plus `OrdinaryOrderState` enum; runtime state guards; hand-written `JsonElement` field parsing, prefix checks, quantity parsing, error ordering, and error mapping. |

Both versions use the shared module logic, module error records, common error interface, and
neutral outcome records in `DomainModels.cs`. Those shared declarations occupy 60 raw lines and
are excluded from the per-version comparison below.

| Accounting | FunnySharp | Ordinary |
| --- | ---: | ---: |
| Consumer-specific declarations, adapters, conversions, and workflow | 300 | 451 |
| Shared module logic and neutral comparison types | 60 | 60 |
| Library implementation (one-time, not charged to a consumer) | 253 | 0 |

A separate adapter-only baseline, [HandWrittenDomainAdapter.cs](../tests/Shared/DomainComparison/HandWrittenDomainAdapter.cs),
hand-writes one tagged identifier in 35 raw lines (private constructor, validation, contract,
decoder, equality). The equivalent tag plus `Domain.For` plus decoder declaration in
`Domains` is about ten lines. This isolates the per-identifier declaration cost from the complete
workflow comparison; the hand-written hot path performs the same as the generic wrapper, so the
gain is maintenance and repetition, not allocation.

The FunnySharp consumer-specific side is 151 raw lines smaller even though it declares five
distinct tagged identifiers and four explicit state records. The ordinary side saves wrapper
declarations but pays for manual field decoding, prefix and range checks, enum-state guards, and
hand-written conversion from parsed values into records. The first workflow is larger in total when
the one-time 253-line library implementation is charged; the reduction accumulates when the same
boundary is reused across identifiers, modules, and workflows. Raw lines include braces and
comments; this is not a LOC target.

The library implementation is
[Domain.cs](../src/FunnySharp/Domain.cs) (115 lines) and
[DomainValue.cs](../src/FunnySharp/DomainValue.cs) (138 lines). It has no runtime dependency.

## Equivalence

Both consumers run all 18 fixtures in
[DomainFixtures.cs](../tests/Shared/DomainComparison/DomainFixtures.cs):

`valid`, `valid-pen`, `unknown-sku`, `out-of-stock`, `payment-declined`, `invalid-order-id`,
`invalid-customer-id`, `empty-sku`, `zero-quantity`, `missing-quantity`, `wrong-kind-quantity`,
`fractional-quantity`, `null-customer-id`, `root-array`, `empty-object`, `multiple-invalid`,
`extra-field`, `duplicate-order-id`.

The comparison test is
[DomainConsumerComparisonTests.cs](../tests/FunnySharp.Tests/DomainConsumerComparisonTests.cs). It
compares the final status, order identity, quantity, unit price, reservation/authorization/tracking
codes, cancellation reason, and the complete ordered failure list for every fixture, plus the
cancel path. A separate assertion checks that mapping a module failure to the workflow hierarchy
preserves the concrete `PricingFailure.Sku` and `PricingFailure.Quantity` payload, not just the
common source/code fields. It fails closed if either consumer disagrees. Fixed expected rows anchor the most
important distinctions:

| Fixture | Outcome | Source | Code / path | Detail |
| --- | --- | --- | --- | --- |
| `valid` | shipped, `ORD-100`, quantity 2, unit price 12.50 | — | tracking `TRK-ORD-100` | reservation and authorization retained |
| `unknown-sku` | rejected | `pricing` | `unknown-sku` | `SKU-UNKNOWN|2` |
| `out-of-stock` | rejected | `inventory` | `insufficient-stock` | `SKU-BOOK|9` |
| `payment-declined` | rejected | `payment` | `payment-declined` | `payment-declined` |
| `invalid-order-id` | rejected | `request` | `InvalidValue` at `orderId` | `order identifier must start with ORD-` |
| `fractional-quantity` | rejected | `request` | `Malformed` at `line.quantity` | `the value cannot be parsed as a 32-bit integer` |
| `multiple-invalid` | rejected | `request` | `orderId`, `customerId`, `line.sku`, `line.quantity`, `payment.token` | five errors in decoder order |
| `empty-object` | rejected | `request` | `orderId`, `customerId`, `line`, `payment` | four `Missing` errors |
| `duplicate-order-id` | shipped, `ORD-700` | — | first duplicate wins | both consumers follow the same policy |
| `extra-field` | shipped, `ORD-600` | — | unknown field ignored | both consumers accept it |

## Intentional Misuse Diagnostics

[DomainMisuseContractTests.cs](../tests/FunnySharp.Analyzers.Tests/DomainMisuseContractTests.cs)
compiles isolated snippets against the real assembly through the analyzer harness and asserts one
attributable error:

| Misuse | Diagnostic | Meaning |
| --- | --- | --- |
| Assign `DomainValue<string, OrderIdTag>` to `DomainValue<string, CustomerIdTag>` | `CS0029` | Confusable identifiers are distinct types; the message names both markers. |
| Pass a raw `string` to a method requiring `DomainValue<string, OrderIdTag>` | `CS1503` | The validated boundary cannot be skipped implicitly. |
| Call `new DomainValue<string, OrderIdTag>("ORD-1")` | `CS1729` | No public constructor takes the underlying value; validation is the only construction path. |

The runtime counterpart is covered too: the ordinary enum-state record accepts `Ship` at compile
time for every state and must return an `invalid-state` error for a draft. That guard is the
defensive code the explicit state types remove.

## Boundary Behavior

Core tests in [DomainValueTests.cs](../tests/FunnySharp.Tests/DomainValueTests.cs) pin:

- `default(DomainValue<...>)`, `new DomainValue<...>()`, `Activator.CreateInstance`, direct
  `System.Text.Json` deserialization, and deserialization of a consumer record containing a
  domain value produce an uninitialized wrapper rather than a valid one. `Value` throws,
  `TryGetValue` returns `false`, and equality distinguishes it from an initialized value.
- A runtime null underlying value is a `Null` decode/validation error; the internal construction
  path rejects null.
- A mutable payload is stored by reference. Mutating it after construction is observable through
  `Value`, and `Domain.Contract.Validate` then reports the current violation. The boundary
  guarantees historical construction success, not permanent payload validity.
- `Domain.CreateDecoder` preserves the underlying decoder's `Malformed`, `Empty`, and
  `InvalidValue` kinds and its field path; an underlying contract without a derivable decoder
  returns a `ContractLimitation` naming `Decoding` instead of a guessed decoder.
- `Domain.Contract` validates, decodes, generates, shrinks, and describes through the underlying
  contract.
- An uninitialized wrapper fails `Domain.Contract.Validate` with
  `not a validated <name>` rather than being accepted.
- `Domain.Contract` interoperates with `PatchSchema<T>`: a valid replacement applies, an empty
  replacement reports `Empty` at the field path, and candidate validation uses the same rule.
- Equality, hashing, diagnostic `ToString`, and eager argument validation behave deterministically.

## Measurements

Command:

```bash
dotnet run --project tests/FunnySharp.DomainMeasurements/FunnySharp.DomainMeasurements.csproj -c Release
```

The harness first runs the complete comparison, then measures the paths below. Allocations use
`GC.GetAllocatedBytesForCurrentThread`; timing is the minimum of three rounds.

| Operation | Allocated B/op | Time ns/op |
| --- | ---: | ---: |
| FunnySharp: valid decode + submit + ship | 1040.0 | 6401.5 |
| Ordinary: valid decode + submit + ship | 1088.0 | 1749.3 |
| FunnySharp: invalid decode | 3664.2 | 3215.5 |
| Ordinary: invalid decode | 1088.0 | 1845.0 |
| Raw string identity | 0.0 | 2.7 |
| Hand-written wrapper `Create` | 0.0 | 49.6 |
| `Domain.Validate` | 0.0 | 69.1 |
| `Domain.Contract.Validate` | 0.0 | 137.1 |
| Hand-written `Contract.Validate` | 0.0 | 58.5 |
| Domain decoder | 40.0 | 60.9 |
| Hand-written wrapper decoder | 40.0 | 124.0 |
| Domain `PlacedOrder` to `ShippedOrder` | 120.0 | 58.9 |
| Ordinary `with` copy to shipped | 96.0 | 63.2 |
| Ordinary wrong-state guard | 96.0 | 51.4 |

The hand-written wrapper baseline is [HandWrittenDomainAdapter.cs](../tests/Shared/DomainComparison/HandWrittenDomainAdapter.cs),
35 raw lines for one identifier: private constructor, validation, underlying contract, derived
contract, decoder, and equality. The equivalent tagged declaration in `Domains` is
about ten raw lines (marker, `Domain.For`, decoder), with the generic implementation counted once.
The hand-written wrapper matches the domain hot path in this run: 0 B for `Create`, 0 B for
validating an already-wrapped value through its contract, and 40 B for a successful decoder call. The gain is declaration and maintenance cost, not a fabricated runtime
advantage.

The domain wrapper itself adds no allocation over the underlying value: validation and wrapping
are struct-only. Its inline size is 16 B for a `string` value (one reference plus the
initialization flag and padding) and 8 B for an `int` value (the value plus the flag and padding),
versus 8 B for the hand-written wrapper and 8 B/4 B for the raw values. The flag is what
makes a default wrapper detectably uninitialized. The measured domain decode path allocates 40 B
per successful identifier decode; the wrapper itself is allocation-free. The complete domain
workflow allocates less than the ordinary workflow on the valid path in this run; its invalid path
allocates more because it materializes structured `DecodeError` values with locations while the
ordinary hand-written decoder stores short strings. The evidence discloses that cost rather than claiming a universal
allocation win.

State transitions copy once per state record. `PlacedOrder` to `ShippedOrder` allocates the new
`ShippedOrder` record (120 B measured after hoisting the validated tracking code); the ordinary
`with` copy allocates the new record (96 B measured). Explicit state types buy compile-time operation availability, not zero-copy
transitions.

## Guarantees Kept Separate

| Fact | Where it is established | What it does not establish |
| --- | --- | --- |
| Identifier and state types are distinct | Compiler | Does not establish identity, uniqueness, or existence in storage. |
| The wrapped value satisfied the contract | `Domain.Validate` or `Domain.CreateDecoder` | Does not establish that a mutable payload stays valid. |
| Decode categories and paths | `DecodeError` | Does not establish business authorization. |
| A transition produced a candidate state | Consumer method or `StateTransition` | Does not establish that a database row is unchanged or that a command ran once. |
| Module failures keep source and payload | `Result.MapError` into a record hierarchy | Does not establish retry, compensation, or transaction behavior. |

## Deliberate Non-Guarantees

The evidence does not claim:

- optimistic concurrency, ETag/version checks, row locking, or stale-state rejection;
- authorization, tenancy, or ownership enforcement;
- exactly-once execution, idempotency, or command deduplication;
- a transaction, rollback, outbox, or saga coordinator;
- a JSON converter or automatic serialization of the wrapper;
- a general discriminated union or a wrapper for every primitive;
- whole-program effect or exception safety.

Those remain application or infrastructure responsibilities.

## Native C# Decision

The implementation uses native C# distinct types (phantom markers and sealed records) and the
existing `Result`/`Validation`/`Contract` surfaces. A source generator was not added: the
demonstrated gap is the per-identifier contract, decoder, equality, and validation adapter, and the
generic wrapper plus `Contract.Widen` removes it without build-time machinery or a generated API
surface. Generation remains an option only if a later consumer shows a gap this design cannot
cover. No broad union ecosystem, preview runtime, or core runtime dependency is introduced.
