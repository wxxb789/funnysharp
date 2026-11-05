# Domain Values and Explicit State

`DomainValue<TValue, TTag>` is a validated, phantom-tagged value. `Domain<TValue, TTag>` is the
validated boundary that creates it, validates it, and derives a `Contract<DomainValue<...>>` and
decoder from one underlying contract. Distinct marker types keep otherwise confusable identifiers
apart; ordinary sealed records keep state-specific operations on the states that support them.
The compiling example is
[examples/FunnySharp.Examples/DomainExamples.cs](../examples/FunnySharp.Examples/DomainExamples.cs);
the consumer comparison, misuse diagnostics, and measured costs are in
[domain-value-evidence.md](domain-value-evidence.md).

The family is not a fifth outcome carrier and not a discriminated-union system. It composes with
the existing `Option`, `Result`, `UnitResult`, `Validation`, `Decoder`, `Contract`, and `Patch`
surfaces. The marker has no runtime value; it is never instantiated per wrapped value.

## API Shape

```csharp
using System.Text.Json;
using FunnySharp;

public readonly struct OrderIdTag;

public readonly struct QuantityTag;

var orderIds = Domain.For<string, OrderIdTag>(
    Contract.NonEmptyString.Ensure(
        value => value.StartsWith("ORD-", StringComparison.Ordinal),
        "order identifier must start with ORD-"),
    "order identifier");

var quantities = Domain.For<int, QuantityTag>(Contract.Int32Range(1, 100), "quantity");

// Validate and wrap an existing value.
var validated = orderIds.Validate("ORD-100");
if (validated.TryGetValue(out var orderId))
{
    Console.WriteLine(orderId.Value);
}

// Derive a decoder; the underlying contract's diagnostics and locations are preserved.
var decoder = orderIds.CreateDecoder();
if (decoder.TryGetValue(out var orderIdDecoder))
{
    using var document = JsonDocument.Parse("\"ORD-100\"");
    var decoded = orderIdDecoder.Decode(document.RootElement);
    Console.WriteLine(decoded.IsValid);
}

// The tagged contract supports validation, decoding, generation, shrinking, and description.
var contract = orderIds.Contract;
Console.WriteLine(contract.Explain());
```

`Domain.For` requires a non-null `Contract<TValue>` and a non-empty human-readable name. The
boundary is immutable and reusable. Validation and decoding never mutate the underlying value.

## Distinct Identifiers

Two domain values with the same underlying type and different markers are different types:

```csharp
public readonly struct CustomerIdTag;

var customerIds = Domain.For<string, CustomerIdTag>(Contract.NonEmptyString, "customer identifier");

DomainValue<string, OrderIdTag> orderId = orderIds.Validate("ORD-100").Match(
    value => value,
    errors => throw new InvalidOperationException(errors[0].ToString()));
DomainValue<string, CustomerIdTag> customerId = customerIds.Validate("CUS-100").Match(
    value => value,
    errors => throw new InvalidOperationException(errors[0].ToString()));

// Does not compile: the markers differ.
// DomainValue<string, CustomerIdTag> wrong = orderId;
```

The wrapper is a `readonly struct` with private construction, structural equality, and no implicit
conversion to or from `TValue`. Read the underlying value with `Value` (throws for an
uninitialized wrapper) or `TryGetValue` (returns `false`). No public constructor accepts the
underlying value, so an identifier cannot skip its boundary accidentally. A raw `string` cannot be assigned to a
`DomainValue<string, OrderIdTag>` without a deliberate validation call.

Use a domain value where confusing two identifiers would be a defect. Do not wrap every primitive;
ordinary `int`, `string`, `Guid`, and records remain the right choice when the distinction has no
meaningful boundary or operation difference.

## Validated Construction and Decoding

`Domain<TValue, TTag>.Validate` returns `Validation<DomainValue<TValue, TTag>, DecodeError>`. It
uses the same contract rules as `Contract.Validate`, including the accumulated error order and
paths. A runtime null is a `Null` error.

`Domain<TValue, TTag>.CreateDecoder` returns
`Result<Decoder<DomainValue<TValue, TTag>>, ContractLimitation>`. The decoder is the underlying
contract's decoder, so missing, null, wrong-kind, malformed, empty, and invalid-value diagnostics
keep their locations. The tag is added only after the underlying decode and validation succeed.
The `Result` makes an unavailable underlying decoder an explicit limitation rather than a runtime
guess.

`Domain<TValue, TTag>.Contract` is a `Contract<DomainValue<TValue, TTag>>` derived through
`Contract<T>.Widen`. It accepts only initialized wrappers, revalidates their underlying values,
and reuses the underlying contract's decoder, generator, shrinker, and description where those
interpretations exist. An unavailable derivation remains an explicit `ContractLimitation`.

This is the interoperability point for patches, object contracts, and generated tests:

```csharp
var candidateContract = Contract.Object(
    Contract.For<Item>().Field("orderId", orderIds.Contract, item => item.OrderId),
    id => new Item(id));

var field = Patch.Field<Item, DomainValue<string, OrderIdTag>>(
    "orderId",
    orderIds.Contract,
    Lens.Create<Item, DomainValue<string, OrderIdTag>>(
        item => item.OrderId,
        (item, value) => item with { OrderId = value }));

var schema = PatchSchema<Item>.Create(candidateContract, field);
```

The field decoder, candidate validation, and any downstream contract check use the same domain
rule. A malformed replacement is reported at the patch field path; a candidate with an
uninitialized domain value fails contract validation.

## Explicit State Variants

State-specific operations are expressed as ordinary sealed record types. The compiler, not a
runtime guard, decides which operation is available:

```csharp
public sealed record DraftOrder(OrderId Id, CustomerId CustomerId, Sku Sku, Quantity Quantity)
{
    public Result<PlacedOrder, OrderError> Submit() { /* pricing, inventory, payment */ }
    public CancelledOrder Cancel(string reason) => new(Id, reason);
}

public sealed record PlacedOrder(OrderId Id, Sku Sku, Quantity Quantity, decimal UnitPrice)
{
    public ShippedOrder Ship(TrackingCode tracking) => new(Id, tracking);
    public CancelledOrder Cancel(string reason) => new(Id, reason);
}

public sealed record ShippedOrder(OrderId Id, TrackingCode Tracking);
```

`Ship` does not exist on `DraftOrder`, `Cancel` is not offered by `ShippedOrder`, and a
`PlacedOrder` cannot be passed where a `ShippedOrder` is required. The ordinary alternative is a
single record with an enum state and runtime guards inside every operation. That alternative still
has to decide what to do when `Ship` is called on a draft, and the compiler cannot point at the
mistake. The state-specific operation is rejected by the compiler because the member does not
exist on the other state; the consumer comparison pins the runtime alternative with an enum-state
guard.

State types are ordinary C# records. FunnySharp does not add a union type, a state wrapper, or a
generic transition hierarchy. Construct transitions with ordinary methods, `Result`, or the
existing `StateTransition`/`StateMachine` surface when a same-state machine is the better fit.

## Typed Error Composition Across Modules

Module functions keep their own error types. The workflow maps each module failure once with
`Result.MapError` into a common record hierarchy, so the source and its specific payload survive:

```csharp
public abstract record WorkflowError
{
    public abstract string Source { get; }
    public abstract string Code { get; }
}

public sealed record PricingRejected(PricingFailure Failure) : WorkflowError
{
    public override string Source => "pricing";
    public override string Code => Failure.Code;
}

public sealed record InventoryRejected(InventoryFailure Failure) : WorkflowError
{
    public override string Source => "inventory";
    public override string Code => Failure.Code;
}

var result = pricing.Price(order.Sku, order.Quantity)
    .MapError(failure => (WorkflowError)new PricingRejected(failure))
    .Zip(inventory.Reserve(order.Sku, order.Quantity)
        .MapError(failure => (WorkflowError)new InventoryRejected(failure)))
    .Bind(values => payment.Authorize(order.PaymentToken, values.First * order.Quantity.Value)
        .MapError(failure => (WorkflowError)new PaymentRejected(failure))
        .Map(authorization => new PlacedOrder(/* ... */)));
```

The common interface or base type carries the uniform facts (`Source`, `Code`); pattern matching
recovers the source-specific fields (`PricingFailure.Sku`, `InventoryFailure.Quantity`, and so on).
No new error carrier or union is needed. `Result` remains fail-fast across modules,
`Validation` remains available for independent checks, and unexpected exceptions and cancellation
keep their ordinary .NET behavior.

## Guarantees and Limits

| Fact | Enforced by | Meaning and limit |
| --- | --- | --- |
| Two tagged identifiers are distinct types | Compiler | Different `TTag` types do not assign or convert implicitly. |
| A state operation exists only on its state | Compiler | `Ship` is a member of `PlacedOrder`, not `DraftOrder`; state data itself is application-owned. |
| The wrapped value satisfied its contract | Construction or decoding | True at the moment `Validate`/`Decode` succeeds; a mutable payload can change later. |
| Decode failures keep their category and path | Decoding | `DecodeError` preserves `Kind`, `Path`, `Expected`, and `Detail`. |
| Unavailable contract interpretations are visible | Contract | `CreateDecoder`, `Generate`, and `Shrink` return explicit limitations; `Capabilities` lists every unavailable derivation. |
| A database row is unchanged | Not guaranteed | A state value is not a version, ETag, transaction, or row lock. |
| The caller is authorized | Not guaranteed | Authorization remains an application boundary. |
| An operation ran exactly once | Not guaranteed | Exactly-once execution requires idempotency, storage, or a coordinator; a value cannot prove it. |
| The wrapper round-trips through JSON automatically | Not guaranteed | No converter ships; use a DTO plus the domain decoder. |

## Default, Null, and Alternative Construction

`default(DomainValue<TValue, TTag>)`, `new DomainValue<TValue, TTag>()`, and
`Activator.CreateInstance<DomainValue<TValue, TTag>>()` are uninitialized. `IsInitialized` is
`false`, `TryGetValue` returns `false`, and `Value` throws `InvalidOperationException`. Equality
treats two uninitialized wrappers as equal and never equal to an initialized wrapper.

`TValue` is constrained to `notnull`. A runtime null supplied to `Domain.Validate` is a `Null`
validation error, and the internal construction path rejects null. The wrapper is a struct, so
there is no null wrapper reference to dereference.

The wrapper holds `TValue` without copying it. If `TValue` is a mutable reference, later mutation
is visible through `Value` and can make the current payload violate the contract; the boundary
records that the value passed at construction, not that it stays valid forever. Revalidate with
`Domain.Contract.Validate` when current validity matters, and prefer immutable payloads where the
distinction matters.

Direct `System.Text.Json` deserialization of the wrapper, or of a record containing one, yields an
uninitialized wrapper rather than an unchecked valid value. Reflection, `Unsafe`, and deliberately
corrupted memory are outside the published guarantee; no managed wrapper can defend against them.

## Deliberate Boundaries

- No general discriminated union, `Either`, `OneOf`, or state-union hierarchy. State variants are
  ordinary consumer records; typed error composition uses ordinary records plus `Result`.
- No source generator. The generic tagged wrapper and `Contract.Widen` cover the demonstrated
  identifier and decoder boilerplate; adding generation requires a later consumer-driven gap.
- No wrapper requirement for `int`, `string`, `Guid`, or other primitives. Domain values are
  opt-in and earn their place through a boundary or a distinction.
- No `System.Text.Json` converter, serializer, reflection binder, or schema introspection on the
  domain boundary.
- No persistence, transaction, row-version, authorization, idempotency, or exactly-once promise.
  Those remain application and infrastructure responsibilities.
- No core runtime dependency and no preview-runtime requirement. The implementation uses the BCL
  and the shipped `Contract`, `Decoder`, `Validation`, and `Result` surfaces.
- No whole-program effect or exception-safety guarantee. The goal supplies domain interoperability
  for decoding, changesets, and domain programs, not their complete implementations.
