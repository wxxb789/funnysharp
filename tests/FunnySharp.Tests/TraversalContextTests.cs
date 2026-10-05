#pragma warning disable FS0017

using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace FunnySharp.Tests;

public sealed class TraversalContextTests
{
    [Fact]
    public void IndexedTraverseRejectsNullArgumentsBeforeEnumeration()
    {
        IEnumerable<int>? source = null;
        var sourceEnumerated = new InvalidOperationException("The source must not be enumerated.");

        Assert.Throws<ArgumentNullException>(() => source!.Traverse(static (index, value) => Option.Some(value)));
        Assert.Throws<ArgumentNullException>(() =>
            source!.Traverse(static (index, value) => Result<int, string>.Success(value)));
        Assert.Throws<ArgumentNullException>(() =>
            source!.Traverse(static (index, value) => Validation<int, string>.Valid(value)));
        Assert.Throws<ArgumentNullException>(() =>
            new ThrowingEnumerable<int>(sourceEnumerated).Traverse((Func<int, int, Option<int>>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            new ThrowingEnumerable<int>(sourceEnumerated).Traverse((Func<int, int, Result<int, string>>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            new ThrowingEnumerable<int>(sourceEnumerated).Traverse((Func<int, int, Validation<int, string>>)null!));
    }

    [Fact]
    public void KeyedTraverseRejectsNullArgumentsBeforeEnumeration()
    {
        IReadOnlyDictionary<string, int>? dictionary = null;
        var dictionaryEnumerated = new InvalidOperationException("The dictionary must not be enumerated.");

        Assert.Throws<ArgumentNullException>(() => dictionary!.Traverse(static (key, value) => Option.Some(value)));
        Assert.Throws<ArgumentNullException>(() =>
            dictionary!.Traverse(static (key, value) => Validation<int, string>.Valid(value)));
        Assert.Throws<ArgumentNullException>(() =>
            new ThrowingDictionary<string, int>(dictionaryEnumerated)
                .Traverse((Func<string, int, Option<int>>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            new ThrowingDictionary<string, int>(dictionaryEnumerated)
                .Traverse((Func<string, int, Validation<int, string>>)null!));
    }

    [Fact]
    public void LocatedListTraverseRejectsNullArgumentsBeforeEnumeration()
    {
        IEnumerable<int>? source = null;
        var root = Location.Root;
        var sourceEnumerated = new InvalidOperationException("The source must not be enumerated.");

        Assert.Throws<ArgumentNullException>(() =>
            source!.Traverse(root, static (location, value) => Option.Some(value)));
        Assert.Throws<ArgumentNullException>(() =>
            source!.Traverse(root, static (location, value) => Result<int, string>.Success(value)));
        Assert.Throws<ArgumentNullException>(() =>
            source!.Traverse(root, static (location, value) => Validation<int, string>.Valid(value)));
        Assert.Throws<ArgumentNullException>(() =>
            new ThrowingEnumerable<int>(sourceEnumerated)
                .Traverse((Location)null!, static (location, value) => Option.Some(value)));
        Assert.Throws<ArgumentNullException>(() =>
            new ThrowingEnumerable<int>(sourceEnumerated)
                .Traverse((Location)null!, static (location, value) => Result<int, string>.Success(value)));
        Assert.Throws<ArgumentNullException>(() =>
            new ThrowingEnumerable<int>(sourceEnumerated)
                .Traverse((Location)null!, static (location, value) => Validation<int, string>.Valid(value)));
        Assert.Throws<ArgumentNullException>(() =>
            new ThrowingEnumerable<int>(sourceEnumerated).Traverse(root, (Func<Location, int, Option<int>>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            new ThrowingEnumerable<int>(sourceEnumerated)
                .Traverse(root, (Func<Location, int, Result<int, string>>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            new ThrowingEnumerable<int>(sourceEnumerated)
                .Traverse(root, (Func<Location, int, Validation<int, string>>)null!));
    }

    [Fact]
    public void LocatedDictionaryTraverseRejectsNullArgumentsBeforeEnumeration()
    {
        IReadOnlyDictionary<string, int>? dictionary = null;
        var root = Location.Root;
        var dictionaryEnumerated = new InvalidOperationException("The dictionary must not be enumerated.");

        Assert.Throws<ArgumentNullException>(() =>
            dictionary!.Traverse(root, static (location, key, value) => Option.Some(value)));
        Assert.Throws<ArgumentNullException>(() =>
            dictionary!.Traverse(root, static (location, key, value) => Validation<int, string>.Valid(value)));
        Assert.Throws<ArgumentNullException>(() =>
            new ThrowingDictionary<string, int>(dictionaryEnumerated)
                .Traverse((Location)null!, static (location, key, value) => Option.Some(value)));
        Assert.Throws<ArgumentNullException>(() =>
            new ThrowingDictionary<string, int>(dictionaryEnumerated)
                .Traverse((Location)null!, static (location, key, value) => Validation<int, string>.Valid(value)));
        Assert.Throws<ArgumentNullException>(() =>
            new ThrowingDictionary<string, int>(dictionaryEnumerated)
                .Traverse(root, (Func<Location, string, int, Option<int>>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            new ThrowingDictionary<string, int>(dictionaryEnumerated)
                .Traverse(root, (Func<Location, string, int, Validation<int, string>>)null!));
    }

    [Fact]
    public void IndexedTraverseDeliversZeroBasedIndicesInSourceOrder()
    {
        var optionCalls = new List<(int Index, string Item)>();
        var resultCalls = new List<(int Index, string Item)>();
        var validationCalls = new List<(int Index, string Item)>();

        var option = new[] { "a", "b", "c" }.Traverse((index, item) =>
        {
            optionCalls.Add((index, item));
            return Option.Some(item.ToUpperInvariant());
        });
        var result = new[] { "a", "b", "c" }.Traverse((index, item) =>
        {
            resultCalls.Add((index, item));
            return Result<string, string>.Success(item.ToUpperInvariant());
        });
        var validation = new[] { "a", "b", "c" }.Traverse((index, item) =>
        {
            validationCalls.Add((index, item));
            return Validation<string, string>.Valid(item.ToUpperInvariant());
        });

        Assert.Equal(["A", "B", "C"], GetOptionList(option));
        Assert.Equal(["A", "B", "C"], GetResultList(result));
        Assert.Equal(["A", "B", "C"], GetValidationList(validation));
        Assert.Equal([(0, "a"), (1, "b"), (2, "c")], optionCalls);
        Assert.Equal([(0, "a"), (1, "b"), (2, "c")], resultCalls);
        Assert.Equal([(0, "a"), (1, "b"), (2, "c")], validationCalls);
    }

    [Fact]
    public void IndexedTraverseFailsFastOnOptionAndResultAndAccumulatesOnValidation()
    {
        var optionSource = new ProbeEnumerable<int>([1, 2, 3]);
        var resultSource = new ProbeEnumerable<int>([1, 2, 3]);
        var validationSource = new ProbeEnumerable<int>([1, 2, 3]);
        var optionCalls = new List<int>();
        var resultCalls = new List<int>();
        var validationCalls = new List<int>();

        var option = optionSource.Traverse((index, value) =>
        {
            optionCalls.Add(index);
            return index == 0 ? Option.Some(value) : Option.None<int>();
        });
        var result = resultSource.Traverse((index, value) =>
        {
            resultCalls.Add(index);
            return index == 0
                ? Result<int, string>.Success(value)
                : Result<int, string>.Failure($"failed at {index}");
        });
        var validation = validationSource.Traverse((index, value) =>
        {
            validationCalls.Add(index);
            return index == 1
                ? Validation<int, string>.Valid(value)
                : Validation<int, string>.Invalid($"failed at {index}");
        });

        Assert.True(option.IsNone);
        Assert.True(result.TryGetError(out var resultError));
        Assert.Equal("failed at 1", resultError);
        Assert.True(validation.TryGetErrors(out var errors));
        Assert.Equal(["failed at 0", "failed at 2"], errors);
        Assert.Equal([0, 1], optionCalls);
        Assert.Equal([0, 1], resultCalls);
        Assert.Equal([0, 1, 2], validationCalls);
        AssertProbeCompleted(optionSource, 2);
        AssertProbeCompleted(resultSource, 2);
        AssertProbeCompleted(validationSource, 3);
    }

    [Fact]
    public void IndexedTraverseReturnsEmptySuccessesForAnEmptySource()
    {
        var empty = Array.Empty<int>();

        var option = empty.Traverse(static (index, value) => Option.Some(value));
        var result = empty.Traverse(static (index, value) => Result<int, string>.Success(value));
        var validation = empty.Traverse(static (index, value) => Validation<int, string>.Valid(value));

        Assert.Empty(GetOptionList(option));
        Assert.Empty(GetResultList(result));
        Assert.Empty(GetValidationList(validation));
    }

    [Fact]
    public void IndexedTraverseEnumeratesTheSourceOnceAndDisposesTheEnumerator()
    {
        var optionSource = new ProbeEnumerable<int>([1, 2, 3]);
        var resultSource = new ProbeEnumerable<int>([1, 2, 3]);
        var validationSource = new ProbeEnumerable<int>([1, 2, 3]);

        var option = optionSource.Traverse(static (index, value) => Option.Some(value));
        var result = resultSource.Traverse(static (index, value) => Result<int, string>.Success(value));
        var validation = validationSource.Traverse(static (index, value) => Validation<int, string>.Valid(value));

        Assert.Equal([1, 2, 3], GetOptionList(option));
        Assert.Equal([1, 2, 3], GetResultList(result));
        Assert.Equal([1, 2, 3], GetValidationList(validation));
        AssertProbeCompleted(optionSource, 3);
        AssertProbeCompleted(resultSource, 3);
        AssertProbeCompleted(validationSource, 3);
    }

    [Fact]
    public void IndexedTraverseHandlesLargeInputIteratively()
    {
        const int count = 100_000;

        var options = Enumerable.Range(0, count).Traverse(static (index, value) => Option.Some(value));
        var results = Enumerable.Range(0, count).Traverse(static (index, value) => Result<int, string>.Success(value));
        var validations = Enumerable.Range(0, count)
            .Traverse(static (index, value) => Validation<int, string>.Valid(value));

        AssertLargeValues(GetOptionList(options), count);
        AssertLargeValues(GetResultList(results), count);
        AssertLargeValues(GetValidationList(validations), count);
    }

    [Fact]
    public void KeyedTraverseDeliversKeysAndValuesPerPairInSourceOrder()
    {
        var source = new OrderedDictionary<string, int>(
        [
            new("one", 1),
            new("two", 2),
            new("three", 3),
        ]);
        var calls = new List<(string Key, int Value)>();

        var option = source.Traverse((key, value) =>
        {
            calls.Add((key, value));
            return Option.Some(value * 10);
        });

        var mapped = GetOptionDictionary(option);

        Assert.Equal([("one", 1), ("two", 2), ("three", 3)], calls);
        Assert.Equal(3, mapped.Count);
        Assert.Equal(10, mapped["one"]);
        Assert.Equal(20, mapped["two"]);
        Assert.Equal(30, mapped["three"]);
        Assert.Equal(1, source.EnumeratorCount);
    }

    [Fact]
    public void KeyedTraverseMapsValuesAndLeavesTheSourceDictionaryUnchanged()
    {
        var source = new Dictionary<string, int> { ["one"] = 1, ["two"] = 2 };

        var option = source.Traverse(static (key, value) => Option.Some(value * 10));
        var validation = source.Traverse(static (key, value) => Validation<string, string>.Valid($"{key}:{value}"));

        var optionMapped = GetOptionDictionary(option);
        var validationMapped = GetValidationDictionary(validation);

        Assert.Equal(2, optionMapped.Count);
        Assert.Equal(10, optionMapped["one"]);
        Assert.Equal(20, optionMapped["two"]);
        Assert.Equal(2, validationMapped.Count);
        Assert.Equal("one:1", validationMapped["one"]);
        Assert.Equal("two:2", validationMapped["two"]);
        Assert.Equal(2, source.Count);
        Assert.Equal(1, source["one"]);
        Assert.Equal(2, source["two"]);
    }

    [Fact]
    public void KeyedTraverseFailsFastOnOptionAndAccumulatesOnValidation()
    {
        var optionSource = new OrderedDictionary<string, int>(
        [
            new("one", 1),
            new("two", 2),
            new("three", 3),
        ]);
        var validationSource = new OrderedDictionary<string, int>(
        [
            new("one", 1),
            new("two", 2),
            new("three", 3),
        ]);
        var optionCalls = new List<string>();
        var validationCalls = new List<string>();

        var option = optionSource.Traverse((key, value) =>
        {
            optionCalls.Add(key);
            return key == "two" ? Option.None<int>() : Option.Some(value);
        });
        var validation = validationSource.Traverse((key, value) =>
        {
            validationCalls.Add(key);
            return key == "two"
                ? Validation<int, string>.Valid(value)
                : Validation<int, string>.Invalid($"failed at {key}");
        });

        Assert.True(option.IsNone);
        Assert.Equal(["one", "two"], optionCalls);
        Assert.True(validation.TryGetErrors(out var errors));
        Assert.Equal(["failed at one", "failed at three"], errors);
        Assert.Equal(["one", "two", "three"], validationCalls);
    }

    [Fact]
    public void KeyedTraverseReturnsEmptyDictionariesForAnEmptyDictionary()
    {
        var empty = new Dictionary<string, int>();

        var option = empty.Traverse(static (key, value) => Option.Some(value));
        var validation = empty.Traverse(static (key, value) => Validation<int, string>.Valid(value));

        Assert.Empty(GetOptionDictionary(option));
        Assert.Empty(GetValidationDictionary(validation));
    }

    [Fact]
    public void LocatedListTraverseDeliversRootIndexedLocationsPerItem()
    {
        var root = Location.Root.Property("rows");
        var optionLocations = new List<string>();
        var resultLocations = new List<string>();
        var validationLocations = new List<string>();

        var option = new[] { 10, 20, 30 }.Traverse(root, (location, value) =>
        {
            optionLocations.Add(location.ToString());
            return Option.Some(value / 10);
        });
        var result = new[] { 10, 20, 30 }.Traverse(root, (location, value) =>
        {
            resultLocations.Add(location.ToString());
            return Result<int, string>.Success(value / 10);
        });
        var validation = new[] { 10, 20, 30 }.Traverse(root, (location, value) =>
        {
            validationLocations.Add(location.ToString());
            return Validation<int, string>.Valid(value / 10);
        });

        Assert.Equal([1, 2, 3], GetOptionList(option));
        Assert.Equal([1, 2, 3], GetResultList(result));
        Assert.Equal([1, 2, 3], GetValidationList(validation));
        Assert.Equal(["rows[0]", "rows[1]", "rows[2]"], optionLocations);
        Assert.Equal(["rows[0]", "rows[1]", "rows[2]"], resultLocations);
        Assert.Equal(["rows[0]", "rows[1]", "rows[2]"], validationLocations);
    }

    [Fact]
    public void LocatedListTraverseFailsFastOnOptionAndResultAndAccumulatesOnValidation()
    {
        var optionSource = new ProbeEnumerable<int>([1, 2, 3]);
        var resultSource = new ProbeEnumerable<int>([1, 2, 3]);
        var validationSource = new ProbeEnumerable<int>([1, 2, 3]);
        var optionCalls = new List<string>();
        var resultCalls = new List<string>();
        var validationCalls = new List<string>();
        var root = Location.Root.Property("rows");

        var option = optionSource.Traverse(root, (location, value) =>
        {
            optionCalls.Add(location.ToString());
            return value == 2 ? Option.None<int>() : Option.Some(value);
        });
        var result = resultSource.Traverse(root, (location, value) =>
        {
            resultCalls.Add(location.ToString());
            return value == 1
                ? Result<int, string>.Success(value)
                : Result<int, string>.Failure($"failed at {location}");
        });
        var validation = validationSource.Traverse(root, (location, value) =>
        {
            validationCalls.Add(location.ToString());
            return value == 2
                ? Validation<int, string>.Valid(value)
                : Validation<int, string>.Invalid(location.ToString());
        });

        Assert.True(option.IsNone);
        Assert.True(result.TryGetError(out var resultError));
        Assert.Equal("failed at rows[1]", resultError);
        Assert.True(validation.TryGetErrors(out var errors));
        Assert.Equal(["rows[0]", "rows[2]"], errors);
        Assert.Equal(["rows[0]", "rows[1]"], optionCalls);
        Assert.Equal(["rows[0]", "rows[1]"], resultCalls);
        Assert.Equal(["rows[0]", "rows[1]", "rows[2]"], validationCalls);
        AssertProbeCompleted(optionSource, 2);
        AssertProbeCompleted(resultSource, 2);
        AssertProbeCompleted(validationSource, 3);
    }

    [Fact]
    public void LocatedListTraverseEnumeratesTheSourceOnceAndDisposesTheEnumerator()
    {
        var optionSource = new ProbeEnumerable<int>([1, 2]);
        var resultSource = new ProbeEnumerable<int>([1, 2]);
        var validationSource = new ProbeEnumerable<int>([1, 2]);
        var root = Location.Root.Property("rows");

        var option = optionSource.Traverse(root, static (location, value) => Option.Some(value));
        var result = resultSource.Traverse(root, static (location, value) => Result<int, string>.Success(value));
        var validation = validationSource.Traverse(root, static (location, value) => Validation<int, string>.Valid(value));

        Assert.Equal([1, 2], GetOptionList(option));
        Assert.Equal([1, 2], GetResultList(result));
        Assert.Equal([1, 2], GetValidationList(validation));
        AssertProbeCompleted(optionSource, 2);
        AssertProbeCompleted(resultSource, 2);
        AssertProbeCompleted(validationSource, 2);
    }

    [Fact]
    public void LocatedListTraverseMaterializesSuccessValuesInSourceOrder()
    {
        var validation = new[] { "a", "b", "c" }.Traverse(
            Location.Root.Property("rows"),
            static (location, value) => Validation<string, string>.Valid(value.ToUpperInvariant()));

        Assert.Equal(["A", "B", "C"], GetValidationList(validation));
    }

    [Fact]
    public void LocatedListValidationTraverseAccumulatesLargeInputErrorsIteratively()
    {
        const int count = 100_000;

        var validation = Enumerable.Range(0, count).Traverse(
            Location.Root.Property("rows"),
            static (location, value) => Validation<int, string>.Invalid(location.ToString()));

        Assert.True(validation.TryGetErrors(out var errors));
        Assert.Equal(count, errors.Count);
        Assert.Equal("rows[0]", errors[0]);
        Assert.Equal($"rows[{count - 1}]", errors[^1]);
    }

    [Fact]
    public void NestedLocatedTraverseAccumulatesComposedPathsInSourceOrder()
    {
        var customers = new List<CustomerRow>
        {
            new("first", [new AddressRow("1234"), new AddressRow("bad")]),
            new("second", [new AddressRow("nope"), new AddressRow("5678")]),
        };

        var validated = customers.Traverse(
            Location.Root.Property("customers"),
            (customerLocation, customer) => customer.Addresses
                .Traverse(customerLocation.Property("addresses"), ValidateAddress)
                .Map(addresses => new CustomerRecord(customer.Name, addresses)));

        Assert.True(validated.TryGetErrors(out var errors));
        Assert.Equal(
            ["customers[0].addresses[1].postalCode", "customers[1].addresses[0].postalCode"],
            errors.Select(static error => error.Location.ToString()));
    }

    [Fact]
    public void NestedLocatedTraverseRendersTheCanonicalCustomersAddressPostalCodePath()
    {
        var customers = new List<CustomerRow>();
        for (var index = 0; index < 18; index++)
        {
            customers.Add(index == 17
                ? new CustomerRow(
                    "final",
                    [new AddressRow("1234"), new AddressRow("5678"), new AddressRow("bad")])
                : new CustomerRow($"customer{index}", [new AddressRow("1234")]));
        }

        var validated = customers.Traverse(
            Location.Root.Property("customers"),
            (customerLocation, customer) => customer.Addresses
                .Traverse(customerLocation.Property("addresses"), ValidateAddress)
                .Map(addresses => new CustomerRecord(customer.Name, addresses)));

        Assert.True(validated.TryGetErrors(out var errors));
        Assert.Single(errors);
        Assert.Equal("customers[17].addresses[2].postalCode", errors[0].Location.ToString());
        Assert.Equal("Invalid postal code: bad.", errors[0].Message);
    }

    [Fact]
    public void NestedLocatedTraverseMaterializesValidatedRecordsInOrder()
    {
        var customers = new List<CustomerRow>
        {
            new("first", [new AddressRow("1111"), new AddressRow("2222")]),
            new("second", [new AddressRow("3333")]),
        };

        var validated = customers.Traverse(
            Location.Root.Property("customers"),
            (customerLocation, customer) => customer.Addresses
                .Traverse(customerLocation.Property("addresses"), ValidateAddress)
                .Map(addresses => new CustomerRecord(customer.Name, addresses)));

        var records = GetValidationList(validated);

        Assert.Equal(["first", "second"], records.Select(static customer => customer.Name));
        Assert.Equal(["1111", "2222"], records[0].Addresses.Select(static address => address.PostalCode));
        Assert.Equal(["3333"], records[1].Addresses.Select(static address => address.PostalCode));
    }

    [Fact]
    public void LocatedDictionaryTraverseReceivesQuotedKeyLocationsAndAccumulatesComposedErrorPaths()
    {
        var dictionary = new OrderedDictionary<string, string>(
        [
            new("alice", "alice@example.com"),
            new("bob", "invalid"),
            new("carol", "carol@example.com"),
        ]);
        var locations = new List<string>();
        var pairs = new List<(string Key, string Value)>();

        var validation = dictionary.Traverse(
            Location.Root.Property("customers"),
            (location, key, value) =>
            {
                locations.Add(location.ToString());
                pairs.Add((key, value));

                return value.Contains('@')
                    ? Validation<string, ImportError>.Valid(value)
                    : Validation<string, ImportError>.Invalid(
                        new ImportError($"Invalid email address for {key}.", Location.Root)
                            .At(location.Property("email")));
            });

        Assert.True(validation.TryGetErrors(out var errors));
        Assert.Equal(["customers[\"bob\"].email"], errors.Select(static error => error.Location.ToString()));
        Assert.Equal(
            ["customers[\"alice\"]", "customers[\"bob\"]", "customers[\"carol\"]"],
            locations);
        Assert.Equal(
            [("alice", "alice@example.com"), ("bob", "invalid"), ("carol", "carol@example.com")],
            pairs);
    }

    [Fact]
    public void LocatedDictionaryTraverseMapsValuesAndStopsAtTheFirstFailingKey()
    {
        var source = new Dictionary<string, int> { ["one"] = 1, ["two"] = 2 };

        var option = source.Traverse(
            Location.Root.Property("settings"),
            static (location, key, value) => Option.Some(value * 10));

        var mapped = GetOptionDictionary(option);

        Assert.Equal(2, mapped.Count);
        Assert.Equal(10, mapped["one"]);
        Assert.Equal(20, mapped["two"]);
        Assert.Equal(2, source.Count);
        Assert.Equal(1, source["one"]);
        Assert.Equal(2, source["two"]);

        var empty = new Dictionary<string, int>().Traverse(
            Location.Root.Property("settings"),
            static (location, key, value) => Option.Some(value));

        Assert.Empty(GetOptionDictionary(empty));

        var failing = new OrderedDictionary<string, int>(
        [
            new("one", 1),
            new("two", 2),
            new("three", 3),
        ]);
        var calls = new List<string>();

        var failed = failing.Traverse(
            Location.Root.Property("settings"),
            (location, key, value) =>
            {
                calls.Add(key);
                return key == "two" ? Option.None<int>() : Option.Some(value);
            });

        Assert.True(failed.IsNone);
        Assert.Equal(["one", "two"], calls);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    public void KeyedTraverseRetainsReferenceDistinctEqualTextKeys(int sourceKind, bool located)
    {
        var first = new string('x', 1);
        var second = new string('x', 1);
        var unseen = new string('x', 1);
        var dictionary = new Dictionary<string, int>(ReferenceEqualityComparer.Instance)
        {
            [first] = 1,
            [second] = 2,
        };
        var source = ToSupportedDictionary(dictionary, sourceKind);

        Assert.NotSame(first, second);
        Assert.Equal(first, second);

        foreach (var mapped in TraverseSuccessfulDictionaries(source, located))
        {
            Assert.Equal(2, mapped.Count);
            Assert.Equal(10, mapped[first]);
            Assert.Equal(20, mapped[second]);
            Assert.Contains(mapped.Keys, key => ReferenceEquals(key, first));
            Assert.Contains(mapped.Keys, key => ReferenceEquals(key, second));
            Assert.False(mapped.ContainsKey(unseen));
            Assert.False(mapped.TryGetValue(unseen, out _));
            Assert.Throws<KeyNotFoundException>(() => mapped[unseen]);
        }

        Assert.Equal(2, source.Count);
        Assert.Equal(1, source[first]);
        Assert.Equal(2, source[second]);
    }

    [Theory]
    [InlineData(0, false, false)]
    [InlineData(0, false, true)]
    [InlineData(0, true, false)]
    [InlineData(0, true, true)]
    [InlineData(1, false, false)]
    [InlineData(1, false, true)]
    [InlineData(1, true, false)]
    [InlineData(1, true, true)]
    [InlineData(2, false, false)]
    [InlineData(2, false, true)]
    [InlineData(2, true, false)]
    [InlineData(2, true, true)]
    public void KeyedTraversePreservesCaseInsensitiveLookupIncludingEmptyResults(
        int sourceKind,
        bool located,
        bool empty)
    {
        var dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (!empty)
        {
            dictionary.Add("alpha", 1);
        }

        var source = ToSupportedDictionary(dictionary, sourceKind);

        foreach (var mapped in TraverseSuccessfulDictionaries(source, located))
        {
            var resultDictionary = Assert.IsType<Dictionary<string, int>>(mapped);
            Assert.Same(dictionary.Comparer, resultDictionary.Comparer);
            Assert.Equal(source.Count, mapped.Count);

            if (empty)
            {
                resultDictionary.Add("alpha", 10);
            }

            Assert.True(mapped.ContainsKey("ALPHA"));
            Assert.True(mapped.TryGetValue("ALPHA", out var value));
            Assert.Equal(10, value);
            Assert.Equal(10, mapped["ALPHA"]);
        }

        Assert.Equal(empty ? 0 : 1, source.Count);
    }

    [Fact]
    public void LocatedDictionaryTraverseAcceptsEmptyKeysForAllFourCarriers()
    {
        var source = new OrderedDictionary<string, int>([new("", 7)]);
        var root = Location.Root.Property("settings");
        var calls = new List<(string Carrier, string Path, string Key, int Value)>();

        var option = source.Traverse(root, (location, key, value) =>
        {
            calls.Add(("option", location.ToString(), key, value));
            return Option.Some(value * 10);
        });
        var result = source.Traverse(root, (location, key, value) =>
        {
            calls.Add(("result", location.ToString(), key, value));
            return Result<int, string>.Success(value * 10);
        });
        var unitResult = source.Traverse(root, (location, key, value) =>
        {
            calls.Add(("unit", location.ToString(), key, value));
            return UnitResult<string>.Success();
        });
        var validation = source.Traverse(root, (location, key, value) =>
        {
            calls.Add(("validation", location.ToString(), key, value));
            return Validation<int, string>.Valid(value * 10);
        });

        Assert.Equal(70, GetOptionDictionary(option)[""]);
        Assert.Equal(70, GetResultDictionary(result)[""]);
        Assert.Equal(UnitResult<string>.Success(), unitResult);
        Assert.Equal(70, GetValidationDictionary(validation)[""]);
        Assert.Equal(
            [
                ("option", "settings[\"\"]", "", 7),
                ("result", "settings[\"\"]", "", 7),
                ("unit", "settings[\"\"]", "", 7),
                ("validation", "settings[\"\"]", "", 7),
            ],
            calls);
        Assert.Equal(4, source.EnumeratorCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void LocatedDictionaryTraversePreservesFailurePolicyAfterReachingAnEmptyKey(int carrier)
    {
        var source = new OrderedDictionary<string, int>(
        [
            new("first", 1),
            new("", 2),
            new("third", 3),
            new("last", 4),
        ]);
        var root = Location.Root.Property("settings");
        var calls = new List<string>();

        switch (carrier)
        {
            case 0:
                var option = source.Traverse(root, (location, key, value) =>
                {
                    calls.Add(location.ToString());
                    return value == 2 ? Option.None<int>() : Option.Some(value);
                });
                Assert.True(option.IsNone);
                break;
            case 1:
                var result = source.Traverse(root, (location, key, value) =>
                {
                    calls.Add(location.ToString());
                    return value == 2
                        ? Result<int, string>.Failure(location.ToString())
                        : Result<int, string>.Success(value);
                });
                Assert.True(result.TryGetError(out var error));
                Assert.Equal("settings[\"\"]", error);
                break;
            case 2:
                var unitResult = source.Traverse(root, (location, key, value) =>
                {
                    calls.Add(location.ToString());
                    return value == 2
                        ? UnitResult<string>.Failure(location.ToString())
                        : UnitResult<string>.Success();
                });
                Assert.True(unitResult.TryGetError(out var unitError));
                Assert.Equal("settings[\"\"]", unitError);
                break;
            case 3:
                var validation = source.Traverse(root, (location, key, value) =>
                {
                    calls.Add(location.ToString());
                    return value switch
                    {
                        2 => Validation<int, string>.InvalidMany(
                            [location.ToString(), location.Property("detail").ToString()]),
                        4 => Validation<int, string>.Invalid(location.ToString()),
                        _ => Validation<int, string>.Valid(value),
                    };
                });
                Assert.True(validation.TryGetErrors(out var errors));
                Assert.Equal(
                    ["settings[\"\"]", "settings[\"\"].detail", "settings[\"last\"]"],
                    errors);
                break;
        }

        Assert.Equal(
            carrier == 3
                ? ["settings[\"first\"]", "settings[\"\"]", "settings[\"third\"]", "settings[\"last\"]"]
                : new[] { "settings[\"first\"]", "settings[\"\"]" },
            calls);
        Assert.Equal(1, source.EnumeratorCount);
    }

    private static IReadOnlyDictionary<string, int> ToSupportedDictionary(
        Dictionary<string, int> source,
        int sourceKind) =>
        sourceKind switch
        {
            0 => source,
            1 => source.ToFrozenDictionary(source.Comparer),
            2 => source.ToImmutableDictionary(source.Comparer),
            _ => throw new ArgumentOutOfRangeException(nameof(sourceKind)),
        };

    private static IReadOnlyDictionary<string, int>[] TraverseSuccessfulDictionaries(
        IReadOnlyDictionary<string, int> source,
        bool located) =>
        located
            ?
            [
                GetOptionDictionary(source.Traverse(
                    Location.Root, static (location, key, value) => Option.Some(value * 10))),
                GetResultDictionary(source.Traverse(
                    Location.Root, static (location, key, value) => Result<int, string>.Success(value * 10))),
                GetValidationDictionary(source.Traverse(
                    Location.Root, static (location, key, value) => Validation<int, string>.Valid(value * 10))),
            ]
            :
            [
                GetOptionDictionary(source.Traverse(static (key, value) => Option.Some(value * 10))),
                GetResultDictionary(source.Traverse(static (key, value) => Result<int, string>.Success(value * 10))),
                GetValidationDictionary(source.Traverse(static (key, value) => Validation<int, string>.Valid(value * 10))),
            ];

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void ExplicitKeyedTraversePreservesOpaqueEqualityIncludingEmptyResults(
        bool located,
        bool identity,
        bool empty)
    {
        IEqualityComparer<string> comparer = identity
            ? ReferenceEqualityComparer.Instance
            : StringComparer.OrdinalIgnoreCase;
        var first = identity ? new string('x', 1) : "Alpha";
        var second = identity ? new string('x', 1) : "Beta";
        var lookup = identity ? new string('x', 1) : "ALPHA";
        var dictionary = new Dictionary<string, int>(comparer);
        if (!empty)
        {
            dictionary.Add(first, 1);
            dictionary.Add(second, 2);
        }

        var source = new ProbedDictionary<string, int>(
            new ReadOnlyDictionary<string, int>(dictionary));

        foreach (var mapped in TraverseSuccessfulDictionaries(source, located, comparer))
        {
            var resultDictionary = Assert.IsType<Dictionary<string, int>>(mapped);
            Assert.Same(comparer, resultDictionary.Comparer);
            Assert.Equal(dictionary.Count, mapped.Count);

            if (empty)
            {
                resultDictionary.Add(first, 10);
            }
            else
            {
                Assert.Equal(10, mapped[first]);
                Assert.Equal(20, mapped[second]);
                Assert.Contains(mapped.Keys, key => ReferenceEquals(key, first));
                Assert.Contains(mapped.Keys, key => ReferenceEquals(key, second));
            }

            Assert.Equal(!identity, mapped.ContainsKey(lookup));
            Assert.Equal(!identity, mapped.TryGetValue(lookup, out var value));
            if (identity)
            {
                Assert.Throws<KeyNotFoundException>(() => mapped[lookup]);
            }
            else
            {
                Assert.Equal(10, value);
                Assert.Equal(10, mapped[lookup]);
            }
        }

        Assert.Equal(3, source.EnumeratorCount);
        Assert.Equal(dictionary.Count * 3, source.ItemsYielded);
        Assert.Equal(3, source.DisposeCount);
        Assert.Equal(empty ? 0 : 2, dictionary.Count);

        if (!identity)
        {
            var legacySource = new ReadOnlyDictionary<string, int>(dictionary);
            foreach (var mapped in TraverseSuccessfulDictionaries(legacySource, located))
            {
                Assert.Same(
                    EqualityComparer<string>.Default,
                    Assert.IsType<Dictionary<string, int>>(mapped).Comparer);
                Assert.False(mapped.ContainsKey("ALPHA"));
            }
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ExplicitKeyedTraverseKeepsLazyBuffersFailureOrderAndOneEnumeration(
        bool located,
        bool failAtFirst)
    {
        var keys = Enumerable.Range(0, 4).Select(static _ => new string('x', 1)).ToArray();
        var dictionary = new Dictionary<string, int>(ReferenceEqualityComparer.Instance);
        for (var index = 0; index < keys.Length; index++)
        {
            dictionary.Add(keys[index], index + 1);
        }

        var optionSource = new ProbedDictionary<string, int>(dictionary);
        var resultSource = new ProbedDictionary<string, int>(dictionary);
        var validationSource = new ProbedDictionary<string, int>(dictionary);
        var optionComparer = new TrackingKeyComparer();
        var resultComparer = new TrackingKeyComparer();
        var validationComparer = new TrackingKeyComparer();
        var optionCalls = new List<int>();
        var resultCalls = new List<int>();
        var validationCalls = new List<int>();
        var failureValue = failAtFirst ? 1 : 2;
        var root = Location.Root.Property("settings");

        Option<int> SelectOption(string key, int value)
        {
            optionCalls.Add(value);
            return value == failureValue ? Option.None<int>() : Option.Some(value);
        }

        Result<int, string> SelectResult(string key, int value)
        {
            resultCalls.Add(value);
            return value == failureValue
                ? Result<int, string>.Failure("first")
                : Result<int, string>.Success(value);
        }

        Validation<int, string> SelectValidation(string key, int value)
        {
            validationCalls.Add(value);
            return value == failureValue
                ? Validation<int, string>.InvalidMany(["first", "second"])
                : value == 4
                    ? Validation<int, string>.Invalid("last")
                    : Validation<int, string>.Valid(value);
        }

        var option = located
            ? optionSource.Traverse(root, (location, key, value) => SelectOption(key, value), optionComparer)
            : optionSource.Traverse(SelectOption, optionComparer);
        var result = located
            ? resultSource.Traverse(root, (location, key, value) => SelectResult(key, value), resultComparer)
            : resultSource.Traverse(SelectResult, resultComparer);
        var validation = located
            ? validationSource.Traverse(root, (location, key, value) => SelectValidation(key, value), validationComparer)
            : validationSource.Traverse(SelectValidation, validationComparer);

        Assert.True(option.IsNone);
        Assert.True(result.TryGetError(out var error));
        Assert.Equal("first", error);
        Assert.True(validation.TryGetErrors(out var errors));
        Assert.Equal(["first", "second", "last"], errors);
        Assert.Equal(Enumerable.Range(1, failureValue), optionCalls);
        Assert.Equal(Enumerable.Range(1, failureValue), resultCalls);
        Assert.Equal([1, 2, 3, 4], validationCalls);

        foreach (var source in new[] { optionSource, resultSource, validationSource })
        {
            Assert.Equal(1, source.EnumeratorCount);
            Assert.Equal(1, source.DisposeCount);
        }

        Assert.Equal(failureValue, optionSource.ItemsYielded);
        Assert.Equal(failureValue, resultSource.ItemsYielded);
        Assert.Equal(4, validationSource.ItemsYielded);
        foreach (var comparer in new[] { optionComparer, resultComparer, validationComparer })
        {
            Assert.Equal(failAtFirst ? 0 : 1, comparer.HashedKeys.Count);
            if (!failAtFirst)
            {
                Assert.Same(keys[0], Assert.Single(comparer.HashedKeys));
            }
        }

        Assert.Equal(4, dictionary.Count);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    public void ExplicitKeyedTraverseKeepsGuardPrecedenceAndExceptionIdentity(int carrier, bool located)
    {
        var sourceException = new InvalidOperationException("source");
        var selectorException = new InvalidOperationException("selector");
        var throwingSource = new ThrowingDictionary<string, int>(sourceException);

        object Invoke(
            IReadOnlyDictionary<string, int>? source,
            Location? root,
            bool missingSelector,
            IEqualityComparer<string>? comparer,
            bool throwingSelector = false)
        {
            Func<string, int, Option<int>>? optionSelector = missingSelector
                ? null
                : (key, value) => throwingSelector ? throw selectorException : Option.Some(value);
            Func<string, int, Result<int, string>>? resultSelector = missingSelector
                ? null
                : (key, value) => throwingSelector ? throw selectorException : Result<int, string>.Success(value);
            Func<string, int, Validation<int, string>>? validationSelector = missingSelector
                ? null
                : (key, value) => throwingSelector ? throw selectorException : Validation<int, string>.Valid(value);
            Func<Location, string, int, Option<int>>? locatedOptionSelector = missingSelector
                ? null
                : (location, key, value) => optionSelector!(key, value);
            Func<Location, string, int, Result<int, string>>? locatedResultSelector = missingSelector
                ? null
                : (location, key, value) => resultSelector!(key, value);
            Func<Location, string, int, Validation<int, string>>? locatedValidationSelector = missingSelector
                ? null
                : (location, key, value) => validationSelector!(key, value);

            return carrier switch
            {
                0 => located
                    ? source!.Traverse(root!, locatedOptionSelector!, comparer!)
                    : source!.Traverse(optionSelector!, comparer!),
                1 => located
                    ? source!.Traverse(root!, locatedResultSelector!, comparer!)
                    : source!.Traverse(resultSelector!, comparer!),
                2 => located
                    ? source!.Traverse(root!, locatedValidationSelector!, comparer!)
                    : source!.Traverse(validationSelector!, comparer!),
                _ => throw new ArgumentOutOfRangeException(nameof(carrier)),
            };
        }

        Assert.Equal(
            "source",
            Assert.Throws<ArgumentNullException>(() => Invoke(null, null, true, null)).ParamName);
        if (located)
        {
            Assert.Equal(
                "root",
                Assert.Throws<ArgumentNullException>(() => Invoke(throwingSource, null, true, null)).ParamName);
        }

        Assert.Equal(
            "selector",
            Assert.Throws<ArgumentNullException>(() => Invoke(throwingSource, Location.Root, true, null)).ParamName);
        Assert.Equal(
            "comparer",
            Assert.Throws<ArgumentNullException>(() => Invoke(throwingSource, Location.Root, false, null)).ParamName);
        Assert.Same(
            sourceException,
            Assert.Throws<InvalidOperationException>(() =>
                Invoke(throwingSource, Location.Root, false, EqualityComparer<string>.Default)));

        var selectorSource = new ProbedDictionary<string, int>(
            new Dictionary<string, int> { ["first"] = 1 });
        Assert.Same(
            selectorException,
            Assert.Throws<InvalidOperationException>(() =>
                Invoke(selectorSource, Location.Root, false, EqualityComparer<string>.Default, true)));
        Assert.Equal(1, selectorSource.EnumeratorCount);
        Assert.Equal(1, selectorSource.ItemsYielded);
        Assert.Equal(1, selectorSource.DisposeCount);
    }

    private static IReadOnlyDictionary<string, int>[] TraverseSuccessfulDictionaries(
        IReadOnlyDictionary<string, int> source,
        bool located,
        IEqualityComparer<string> comparer) =>
        located
            ?
            [
                GetOptionDictionary(source.Traverse(
                    Location.Root, static (location, key, value) => Option.Some(value * 10), comparer)),
                GetResultDictionary(source.Traverse(
                    Location.Root, static (location, key, value) => Result<int, string>.Success(value * 10), comparer)),
                GetValidationDictionary(source.Traverse(
                    Location.Root, static (location, key, value) => Validation<int, string>.Valid(value * 10), comparer)),
            ]
            :
            [
                GetOptionDictionary(source.Traverse(static (key, value) => Option.Some(value * 10), comparer)),
                GetResultDictionary(source.Traverse(static (key, value) => Result<int, string>.Success(value * 10), comparer)),
                GetValidationDictionary(source.Traverse(static (key, value) => Validation<int, string>.Valid(value * 10), comparer)),
            ];

    private static IReadOnlyList<T> GetOptionList<T>(Option<IReadOnlyList<T>> option)
    {
        Assert.True(option.TryGetValue(out var values));
        return values!;
    }

    private static IReadOnlyList<T> GetResultList<T, TError>(Result<IReadOnlyList<T>, TError> result)
    {
        Assert.True(result.TryGetValue(out var values));
        return values!;
    }

    private static IReadOnlyList<T> GetValidationList<T, TError>(Validation<IReadOnlyList<T>, TError> validation)
    {
        Assert.True(validation.TryGetValue(out var values));
        return values!;
    }

    private static IReadOnlyDictionary<TKey, TValue> GetOptionDictionary<TKey, TValue>(
        Option<IReadOnlyDictionary<TKey, TValue>> option)
    {
        Assert.True(option.TryGetValue(out var values));
        return values!;
    }

    private static IReadOnlyDictionary<TKey, TValue> GetResultDictionary<TKey, TValue, TError>(
        Result<IReadOnlyDictionary<TKey, TValue>, TError> result)
    {
        Assert.True(result.TryGetValue(out var values));
        return values!;
    }

    private static IReadOnlyDictionary<TKey, TValue> GetValidationDictionary<TKey, TValue, TError>(
        Validation<IReadOnlyDictionary<TKey, TValue>, TError> validation)
    {
        Assert.True(validation.TryGetValue(out var values));
        return values!;
    }

    private static void AssertProbeCompleted<T>(ProbeEnumerable<T> probe, int itemCount)
    {
        Assert.Equal(1, probe.EnumeratorCount);
        Assert.Equal(itemCount, probe.ItemsYielded);
        Assert.Equal(1, probe.DisposeCount);
    }

    private static void AssertLargeValues(IReadOnlyList<int> values, int count)
    {
        Assert.Equal(count, values.Count);
        Assert.Equal(0, values[0]);
        Assert.Equal(count - 1, values[^1]);
    }

    private static Validation<AddressRecord, ImportError> ValidateAddress(Location addressLocation, AddressRow address) =>
        address.PostalCode.Length == 4 && address.PostalCode.All(static character => char.IsDigit(character))
            ? Validation<AddressRecord, ImportError>.Valid(new AddressRecord(address.PostalCode))
            : Validation<AddressRecord, ImportError>.Invalid(
                new ImportError($"Invalid postal code: {address.PostalCode}.", Location.Root)
                    .At(addressLocation.Property("postalCode")));

    private sealed record ImportError(string Message, Location Location)
    {
        public ImportError At(Location location) => new(Message, location);
    }

    private sealed record AddressRow(string PostalCode);

    private sealed record CustomerRow(string Name, IReadOnlyList<AddressRow> Addresses);

    private sealed record AddressRecord(string PostalCode);

    private sealed record CustomerRecord(string Name, IReadOnlyList<AddressRecord> Addresses);

    private sealed class ThrowingEnumerable<T>(Exception exception) : IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator() => throw exception;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class TrackingKeyComparer : IEqualityComparer<string>
    {
        public List<string> HashedKeys { get; } = [];

        public bool Equals(string? left, string? right) =>
            ReferenceEqualityComparer.Instance.Equals(left, right);

        public int GetHashCode(string key)
        {
            HashedKeys.Add(key);
            return ReferenceEqualityComparer.Instance.GetHashCode(key);
        }
    }

    private sealed class ProbedDictionary<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> source)
        : IReadOnlyDictionary<TKey, TValue>
        where TKey : notnull
    {
        public int EnumeratorCount { get; private set; }

        public int ItemsYielded { get; private set; }

        public int DisposeCount { get; private set; }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            EnumeratorCount++;
            return Enumerate().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private IEnumerable<KeyValuePair<TKey, TValue>> Enumerate()
        {
            try
            {
                foreach (var pair in source)
                {
                    ItemsYielded++;
                    yield return pair;
                }
            }
            finally
            {
                DisposeCount++;
            }
        }

        public int Count => source.Count;

        public bool ContainsKey(TKey key) => source.ContainsKey(key);

        public bool TryGetValue(TKey key, out TValue value) => source.TryGetValue(key, out value!);

        public TValue this[TKey key] => source[key];

        public IEnumerable<TKey> Keys => source.Keys;

        public IEnumerable<TValue> Values => source.Values;
    }

    private sealed class ThrowingDictionary<TKey, TValue>(Exception exception) : IReadOnlyDictionary<TKey, TValue>
    {
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => throw exception;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => throw exception;

        public bool ContainsKey(TKey key) => throw exception;

        public bool TryGetValue(TKey key, out TValue value) => throw exception;

        public TValue this[TKey key] => throw exception;

        public IEnumerable<TKey> Keys => throw exception;

        public IEnumerable<TValue> Values => throw exception;
    }

    private sealed class OrderedDictionary<TKey, TValue>(IReadOnlyList<KeyValuePair<TKey, TValue>> pairs)
        : IReadOnlyDictionary<TKey, TValue>
    {
        public int EnumeratorCount { get; private set; }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            EnumeratorCount++;
            return pairs.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => pairs.Count;

        public bool ContainsKey(TKey key) =>
            pairs.Any(pair => EqualityComparer<TKey>.Default.Equals(pair.Key, key));

        public bool TryGetValue(TKey key, out TValue value)
        {
            foreach (var pair in pairs)
            {
                if (EqualityComparer<TKey>.Default.Equals(pair.Key, key))
                {
                    value = pair.Value;
                    return true;
                }
            }

            value = default!;
            return false;
        }

        public TValue this[TKey key] =>
            pairs.First(pair => EqualityComparer<TKey>.Default.Equals(pair.Key, key)).Value;

        public IEnumerable<TKey> Keys => pairs.Select(static pair => pair.Key);

        public IEnumerable<TValue> Values => pairs.Select(static pair => pair.Value);
    }
}
