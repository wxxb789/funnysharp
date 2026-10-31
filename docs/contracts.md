# Inspectable Data Contracts

`FunnySharp.Contract<T>` describes supported data constraints once and derives several
interpretations from that description: validation, decoding through
[`Decoder<T>`](decoding.md), a structured explanation, schema metadata, deterministic generated
test inputs, and shrinking. The compiling example is
[examples/FunnySharp.Examples/ContractExamples.cs](../examples/FunnySharp.Examples/ContractExamples.cs);
the consumer comparison and measured costs are in
[contract-evidence.md](contract-evidence.md).

A contract is metadata plus callable behavior, not a serializer, a universal application schema, or
proof of a business invariant. Arbitrary predicates and transformations are welcome, but their
unavailable derivations are reported explicitly instead of guessed.

## API Shape

```csharp
using System.Text.Json;
using FunnySharp;

var fields = Contract.For<OrderLine>();
var lineContract = Contract.Object(
    fields.Field("sku", Contract.NonEmptyString, static line => line.Sku),
    fields.Field("quantity", Contract.Int32Range(1, 100), static line => line.Quantity),
    fields.Field("price", Contract.DecimalRange(0.01m, 5000m), static line => line.Price),
    static (sku, quantity, price) => new OrderLine(sku, quantity, price));

// Validation of an existing value.
var validated = lineContract.Validate(new OrderLine("BOOK-1", 0, 12.50m));
if (validated.TryGetErrors(out var errors))
{
    foreach (var error in errors!)
    {
        Console.WriteLine($"{error.Path}: {error.Detail}");
    }
}

// Decoding reuses the Goal 0026 decoder and DecodeError diagnostics.
var created = lineContract.CreateDecoder();
if (created.TryGetValue(out var decoder))
{
    using var document = JsonDocument.Parse("""{ "sku": "BOOK-1", "quantity": 2, "price": 12.50 }""");
    var decoded = decoder!.Decode(document.RootElement);
    Console.WriteLine(decoded.IsValid);
}

// Structured explanation and schema metadata.
Console.WriteLine(lineContract.Explain());
var description = lineContract.Description;
var capabilities = lineContract.Capabilities;

// Deterministic generated values and valid shrink candidates.
var generated = lineContract.Generate(new ContractGenerationOptions(seed: 17));
if (generated.TryGetValue(out var line))
{
    var shrunk = lineContract.Shrink(line!);
}

internal sealed record OrderLine(string Sku, int Quantity, decimal Price);
```

`Contract.For<T>()` returns a stateless field set that fixes the parent type so accessor lambdas infer
naturally. `Contract.Field`, `OptionalField`, and `NullableField` are also available directly on the
static `Contract` class; those forms require the accessor's parameter type when the compiler cannot
infer the parent.

## One Description, Several Interpretations

| Interpretation | Member | Output | Unavailable when |
| --- | --- | --- | --- |
| Validation | `Validate(T)` | `Validation<T, DecodeError>` | Never; a runtime-null reference value is a `Null` error. |
| Decoding | `CreateDecoder()` | `Result<Decoder<T>, ContractLimitation>` | A rule has no derivable input shape, or a one-of contract has no input selector. |
| Explanation | `Description`, `Explain()` | `ContractDescription`, deterministic text | Never. |
| Schema metadata | `Description`, `Capabilities` | Structural node tree and completeness flag | Opaque or refined rules, and unexpanded recursive references, make the schema incomplete. |
| Generation | `Generate(options)`, `GenerateSamples(count, options)` | `Result<T, ContractLimitation>` / `Result<IReadOnlyList<T>, ContractLimitation>` | An opaque predicate or refinement, exhausted budget, or unsatisfiable declared constraint blocks it. |
| Shrinking | `Shrink(T)` | `Result<IReadOnlyList<T>, ContractLimitation>` | An opaque rule has no derived shrinker. |

Validation checks an existing value. A refinement callback's valid value is ignored; only its
messages are used. Decoding constructs values, generation constructs test inputs, and shrinking
proposes candidates. The field, alternative, range, and cardinality facts are declared once in the
contract and reused by every interpretation.

Object and collection validation accumulate independent errors in declaration order. Decoding uses
the same object field order and the same collection index order, so `Validate` and `Decode` report
the same paths and details for the same rule. A product decoder also rechecks the constructed value
against the product contract, so an inconsistent `combine` function cannot silently return a value
that bypasses its field rules.

## Capabilities By Rule

| Rule | Validation | Decoding | Generation | Shrinking | Complete schema |
| --- | --- | --- | --- | --- | --- |
| Scalar (`String`, `Int32`, `Guid`, ...) | Yes | Yes | Yes | Yes | Yes |
| Inclusive range | Yes | Yes | Yes | Yes | Yes |
| Object with 1-4 fields | Yes | Yes | Yes | Yes | Yes when every member is complete |
| Collection with cardinality | Yes | Yes | Yes | Yes | Yes when the element is complete |
| Optional value, optional field, nullable field | Yes | Yes | Yes | Yes | Yes when the present value is complete |
| Labelled one-of | Yes | Yes with an input selector | Yes | Yes | Yes when every alternative is complete |
| Deferred recursion | Yes | Yes | Yes within the depth and node budgets | Yes | No; the recursive body is represented by name |
| `Ensure` / `ValidateWith` refinement | Yes | Yes | No unless a generator is supplied | Yes, candidates are checked against the refinement | No; the predicate is opaque |
| `From` / arbitrary caller rule | Yes | No unless a decoder is supplied | No unless a generator is supplied | No unless a shrinker is supplied | No unless metadata is supplied |
| Derived (`Widen`) | Yes | Yes when the source decodes | Yes when the source generates | Yes when the source shrinks | Yes when the source is complete |

`Capabilities` reports the booleans and every limitation; each limitation names the derivation, the
contract-relative path, and the reason. `Explain()` renders the same information. A limitation path is
the empty string at the contract root, a field name such as `customer.email`, `[]` for collection
elements, or an alternative label such as `payment.card`.

## Unknown Rules And Explicit Extensions

An arbitrary predicate or transformation is usable immediately for validation:

```csharp
var even = Contract.From<int>(
    "even integer",
    static value => value % 2 == 0
        ? Validation<int, string>.Valid(value)
        : Validation<int, string>.Invalid("value must be even"));

var capabilities = even.Capabilities; // decoding, generation, shrinking, and schema are unavailable
```

The limitation reasons name the honest extension methods:

- `WithDecoder(Decoder<T>)` supplies the input shape. The decoded value is still checked against the
  contract, so the decoder cannot bypass the rule.
- `WithGenerator(Func<Random, T>)` supplies a generator. Every generated value is checked against the
  full contract; a value that violates the rule is a `ContractLimitation`, not a silent retry.
- `WithShrinker(Func<T, IEnumerable<T>>)` supplies candidates. Candidates that violate the contract
  or duplicate the input are removed.
- `WithSchema(ContractDescription)` supplies metadata when the caller knows the shape. It does not
  change validation, decoding, generation, or shrinking.

`Ensure(predicate, detail)` and `ValidateWith(validate, description)` add opaque refinements to an
existing contract. They keep validation, decoding, and shrinking, but generation becomes unavailable
until the caller supplies a generator. `Widen` projects a derived contract to a base type with a
caller-supplied partial inverse; because the inverse is supplied, every derivation the source
supports remains available. A transformation with no inverse is represented by an opaque `From`
contract for the target type plus the decoder and generator the caller owns; the contract never asks
the transformation to reveal an inverse.

No inverse, generator, purity proof, or complete schema is invented. A predicate does not reveal its
inverse, and a transformation does not reveal a generator. The caller either uses the rule for
validation alone or supplies the missing derivation. Callback purity remains the caller's obligation:
contracts invoke ordinary delegates and do not attempt to prove purity or replay side effects.

## Generation And Shrinking

Generation is deterministic for a seed and a `ContractGenerationOptions` value:

- `MaxDepth` bounds recursive descent. A deferred contract reached at the limit returns a
  `ContractLimitation` instead of looping. Lists with a zero minimum and optional fields stop
  naturally at the limit.
- `MaxCollectionLength` bounds generated collection and string width. A declared minimum length
  always wins over the width budget; when the two are inconsistent, generation fails with an explicit
  limitation rather than producing an invalid value.
- `MaxNodes` bounds the total generated structure. Exhaustion is an explicit limitation.

Generated values are checked against the whole contract before they are returned. Generated
primitives respect their declared ranges, non-empty strings are non-empty, collections respect their
cardinality, and one-of values select a declared alternative.

Shrinking is one step of candidates, not a recursive minimization loop. The input must satisfy the
contract; an out-of-domain value fails with a shrinking limitation. Derived shrinkers move numbers
toward a lower anchor, shorten strings and collections, remove present option values, and rebuild
objects one field at a time. Every candidate is checked against the whole contract, including
cross-field refinements, so shrinking cannot leak an invalid boundary input. At most 256 candidates
are materialized.

Cross-field rules are never implemented as hidden rejection sampling. A contract refined with an
`Ensure` rule reports generation as unavailable. A caller can supply a generator that knows the
cross-field constraint; a generated value that violates the rule fails explicitly with the validation
message. The evidence record shows both the unavailable derivation and the caller-supplied path.

## Recursion

`Contract.Defer(name, factory)` creates a named recursive reference. The factory is invoked at most
once. Validation, decoding, and shrinking use the recursive body directly, so a well-founded body
needs a terminal branch in the value shape, such as a list with a zero minimum or an optional field.
Generation descends within `MaxDepth` and `MaxNodes`; a list with a zero minimum or an optional field
terminates the recursion naturally at the depth limit.

A recursive description contains the recursion name and does not expand the body, so it reports an
incomplete schema rather than pretending to be a finite tree. `Explain()` shows the recursive
reference explicitly.

## Interoperability

- Validation and decoding return the existing `Validation<T, DecodeError>` carrier. Decode errors
  keep their stable `Path`, `Kind`, `Expected`, and `Detail`.
- Generation and shrinking use `Result<T, ContractLimitation>`; limitations are ordinary values, not
  exceptions.
- Contracts use BCL types: `JsonElement` for input, `IReadOnlyList<T>` for collections,
  `Func`/`Random` for caller behavior, and `Option<T>` for absence.
- The ASP.NET Core package maps contract validation through the existing
  `Validation<T, TError>.ToHttpResult` boundary. The application chooses the HTTP policy and error
  mapping; no HTTP knowledge enters the core.
- The core package has no new runtime dependency. Generation uses `System.Random`, which is part of
  the platform; no test framework, reflection binder, or compiler package is required.
- [Partial updates](partial-updates.md) reuse a field contract for replacement decoding and the
  complete object contract for candidate validation. A contract remains a description of one value
  type; it does not become a patch, DTO, or persistence schema.

## Deliberate Boundaries

- No universal schema for unrelated application code. A contract describes one value type; it does
  not become an application-wide registry or a JSON Schema implementation.
- No serializer, JSON converter, or automatic wire-format change. `Decoder<T>` remains the input
  boundary; contracts derive decoders only from their declared structure or a caller-supplied decoder.
- No reflection-based binder. Factories and accessors are explicit C# code.
- No generated test-framework dependency. Generation and shrinking return BCL values; an FsCheck,
  Hedgehog, or xUnit integration is a caller choice.
- No automatic predicate inversion or cross-field rejection sampling. Missing derivations are
  explicit limitations with caller-supplied extensions.
- No claim that type shape proves a business invariant. Structural contracts establish shape and
  declared constraints; `Ensure`, `ValidateWith`, and `From` carry business rules, and a complete
  schema means only that the metadata is complete.
- No custom language, TypeScript-style schema DSL, or parser. Contracts are ordinary C# values.
- No competitor API compatibility or feature-parity target. Clojure spec, MoonBit derive, and
  FsCheck are design references, not obligations.
