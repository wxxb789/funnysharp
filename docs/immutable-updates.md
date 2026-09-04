# Immutable Updates

`FunnySharp` provides a deliberately small optics surface for immutable domain updates:
`Lens<TSource, TFocus>` for a total focus and `Optional<TSource, TFocus>` for a focus that may be
absent. Both are `readonly struct` values created from ordinary getter and setter delegates.
They make nested record updates readable while leaving collection choice, copying, and mutation
policy with the caller. Compiling examples are in
[examples/FunnySharp.Examples/Program.cs](../examples/FunnySharp.Examples/Program.cs).

The API is intentionally limited to `Lens`, `Optional`, their factories, `Get` or `GetOption`,
`Set`, `Update`, and left-to-right `Compose`. It does not provide a `Traversal` hierarchy,
reflection, property-name or property-path APIs, a persistent collection ecosystem, or a hidden
copying layer. Use BCL collections and BCL update operations directly.

## Lenses

Create a lens when every source has the focus. The setter describes how to return an updated
source; for records, that is normally a `with` expression.

<!-- documentation-sample: DocumentationSamples.ImmutableUpdates.SetNestedLens -->
```csharp
var profile = Lens.Create<Customer, Profile>(
    customer => customer.Profile,
    (customer, value) => customer with { Profile = value });
var address = Lens.Create<Profile, Address>(
    value => value.Address,
    (value, next) => value with { Address = next });
var city = Lens.Create<Address, string>(
    value => value.City,
    (value, next) => value with { City = next });

var customerCity = profile.Compose(address).Compose(city);
var updated = customerCity.Set(customer, "Paris");
```

Composition is written and evaluated from left to right: the example follows `Customer`, then
`Profile`, then `Address`, then `string`. For ordinary record setters, this is equivalent to the
direct nested update:

<!-- documentation-sample: DocumentationSamples.ImmutableUpdates.UpdateNestedRecord -->
```csharp
var updated = customer with
{
    Profile = customer.Profile with
    {
        Address = customer.Profile.Address with { City = "Paris" },
    },
};
```

`Lens.Identity<T>()` focuses on the complete source. Its `Get` returns the source, `Set` returns
the replacement, and `Update` applies its updater to the source.

The familiar lens laws are useful caller obligations, not runtime validation. For a lawful getter
and setter, using the caller's equality semantics:

- Get-Put: `lens.Set(source, lens.Get(source)) == source`.
- Put-Get: `lens.Get(lens.Set(source, focus)) == focus`.
- Put-Put: `lens.Set(lens.Set(source, first), second) == lens.Set(source, second)`.

These laws, and law-like equivalence between differently parenthesized compositions, hold only
when caller delegates are lawful and do not make observable side effects. `FunnySharp` invokes
the delegates it receives; it does not repair inconsistent setters, clone mutable values, or
enforce purity.

## Optional Focuses

Create an `Optional` when the focus may not exist, such as a dictionary key or nullable record
member. `GetOption` reports absence as `Option<TFocus>.None`.

<!-- documentation-sample: DocumentationSamples.ImmutableUpdates.UpdateOptionalFocus -->
```csharp
var home = Optional.Create<ImmutableDictionary<string, Address>, Address>(
    addresses => addresses.TryGetValue("home", out var value)
        ? Option.Some(value)
        : Option.None<Address>(),
    (addresses, value) => addresses.SetItem("home", value));

var homeCity = addressBook.Compose(home).Compose(city);
var updated = homeCity.Update(customer, value => value.ToUpperInvariant());
```

`Option<T>` treats a null reference as absence. Model a nullable focus with
`Option.FromNullable(value)` so a null result becomes `None`; `Option.Some(null)` is invalid.

When an optional focus is absent, `Set` and `Update` return the exact original source. Neither
the configured setter nor the `Update` callback is invoked. This also applies to a composed
optional when any optional stage is absent. A present focus follows normal setter and updater
behavior.

## BCL Immutable Collections

Use `System.Collections.Immutable` operations inside the setter or updater supplied by the
caller. An optic does not choose a collection operation or implement collection persistence.

<!-- documentation-sample: DocumentationSamples.ImmutableUpdates.UpdateImmutableDictionary -->
```csharp
var addresses = Lens.Create<Customer, ImmutableDictionary<string, Address>>(
    customer => customer.Addresses,
    (customer, value) => customer with { Addresses = value });

var moved = addresses.Update(customer, values =>
    values.SetItem("home", new Address("Paris")));
```

The same rule applies to `ImmutableList<T>`, `ImmutableArray<T>`, and other BCL immutable
collection types: use their standard `SetItem`, `Add`, `Remove`, `Replace`, or equivalent
operations in caller code. For a batch of changes, use a BCL builder and replace the complete
collection once:

<!-- documentation-sample: DocumentationSamples.ImmutableUpdates.UpdateImmutableList -->
```csharp
var lineItems = Lens.Create<Order, ImmutableList<LineItem>>(
    order => order.LineItems,
    (order, value) => order with { LineItems = value });

var updated = lineItems.Update(order, items =>
{
    var builder = items.ToBuilder();
    builder[0] = builder[0] with { Quantity = 2 };
    builder.Add(new LineItem("SKU-2", 1));
    return builder.ToImmutable();
});
```

`FrozenDictionary<TKey, TValue>` and `FrozenSet<T>` are read-optimized snapshots, not
incrementally updatable persistent collections. Query them directly for read-heavy access, or
explicitly construct a replacement snapshot and set the complete value on its containing record.
Do not hide a frozen-collection rebuild inside a general-purpose optic setter.

<!-- documentation-sample: DocumentationSamples.ImmutableUpdates.ReplaceFrozenDictionary -->
```csharp
var numbers = Lens.Create<Snapshot, FrozenDictionary<string, int>>(
    snapshot => snapshot.Numbers,
    (snapshot, value) => snapshot with { Numbers = value });

var replacement = snapshot.Numbers
    .Select(pair => pair.Key == "answer"
        ? new KeyValuePair<string, int>(pair.Key, 43)
        : pair)
    .ToFrozenDictionary();
var updated = numbers.Set(snapshot, replacement);
```

`IReadOnlyCollection<T>` and `IReadOnlyDictionary<TKey, TValue>` are read-only views, not proof
that their backing data is immutable. A source can still expose a mutable collection through those
interfaces. Record `with` expressions and BCL collection updates are shallow with respect to
their elements, so mutable leaf objects may remain aliased between old and new sources. Purity,
defensive copying, and ownership of mutable leaves remain caller responsibilities.

FunnySharp intentionally supplies no borrowed-view adapters for these types. An optic retains its
getter and setter delegates only; it does not retain a source or a focus, borrow storage, or extend
any view lifetime.

## Errors And Initialization

`Lens.Create` and `Optional.Create` reject null getter or setter delegates, and `Update` rejects a
null updater. A default-initialized `Lens<TSource, TFocus>` or `Optional<TSource, TFocus>` has no
delegates and every operation, including `Compose`, throws `InvalidOperationException`.

Delegates run synchronously. Exceptions from getters, setters, and updaters propagate unchanged;
they are not wrapped, converted to `Option`, or otherwise translated. Composed operations may
invoke getters to read a focus and check optional presence, so delegates should remain pure and
should not rely on a particular invocation count beyond the documented missing-focus guarantee.

## Allocation And Performance

Factory creation and composition can allocate delegates or closures. Invocation adds delegate
dispatch overhead. The optics themselves do not copy a source or focus: record copying and
allocation originate in the caller's `with` expression, and collection allocation originates in
the BCL operation selected by the caller. A missing optional focus preserves source identity and
does not invoke a setter that could allocate a replacement.

`ImmutableUpdateBenchmarks` compares direct nested `with` expressions, BCL immutable collection
operations, and frozen lookups with equivalent `Lens`, `Optional`, and `Option` operations. Run the
focused benchmark with:

```shell
dotnet run --project benchmarks/FunnySharp.Benchmarks/FunnySharp.Benchmarks.csproj --configuration Release -- --filter '*ImmutableUpdateBenchmarks*'
```

The exact table below is generated from the approved observation in
`eng/performance/baseline.json`. Hosted timing is directional; allocation ceilings are the blocking
contract.

<!-- performance-table:start immutable-updates -->
| Scenario | Baseline mean | FunnySharp mean | Ratio | Baseline allocation | FunnySharp allocation |
| --- | ---: | ---: | ---: | ---: | ---: |
| FrozenDictionary lookup | 3.645 ns | 3.888 ns | 1.07x | 0 B | 0 B |
| Immutable collection batch update | 53.691 ns | 56.519 ns | 1.05x | 208 B | 208 B |
| ImmutableDictionary existing-key update | 63.338 ns | 65.545 ns | 1.03x | 104 B | 104 B |
| Missing optional update | 7.878 ns | 10.587 ns | 1.34x | 0 B | 0 B |
| Nested record replacement | 18.574 ns | 30.655 ns | 1.65x | 72 B | 72 B |

Excluded measurements:
- Unmeasured optics construction: Optics construction and frozen rebuild costs have no numeric release claim.
<!-- performance-table:end immutable-updates -->

The generated rows keep update and lookup costs explicit without claiming unmeasured construction
or rebuild behavior. Rerun timing on representative hardware before capacity decisions.
