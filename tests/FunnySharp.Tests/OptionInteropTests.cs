namespace FunnySharp.Tests;

public sealed class OptionInteropTests
{
    [Fact]
    public void NullableReferencesConvertThroughFactoryAndExtensionSyntax()
    {
        string? missing = null;
        string? present = "value";

        Assert.Equal(Option.None<string>(), Option.FromNullable(missing));
        Assert.Equal(Option.None<string>(), missing.ToOption());
        Assert.Equal(Option.Some("value"), Option.FromNullable(present));
        Assert.Equal(Option.Some("value"), present.ToOption());
    }

    [Fact]
    public void NullableValueTypesUnwrapWhileGenericOperationsPreserveTheirDeclaredType()
    {
        int? missing = null;
        int? zero = 0;

        Option<int> missingOption = missing.ToOption();
        Option<int> zeroOption = zero.ToOption();
        Option<int?> tryOption = Option.FromTry<int?>((out int? value) =>
        {
            value = 0;
            return true;
        });
        IReadOnlyDictionary<string, int?> values = new Dictionary<string, int?>
        {
            ["zero"] = 0,
        };
        Option<int?> dictionaryOption = values.GetOption("zero");

        Assert.True(missingOption.IsNone);
        Assert.Equal(Option.Some(0), zeroOption);
        Assert.Equal(Option.Some<int?>((int?)0), tryOption);
        Assert.Equal(Option.Some<int?>((int?)0), dictionaryOption);
    }

    [Fact]
    public void DirectNullableOptionStillRejectsRuntimeNull()
    {
        Assert.Throws<ArgumentNullException>(() => Option<int?>.Some(default!));
        Assert.Throws<ArgumentNullException>(() => Option<string?>.Some(null!));
        Assert.Equal(Option.Some<int?>((int?)0), Option<int?>.Some(0));
        Assert.Equal(Option.Some<string?>("value"), Option<string?>.Some("value"));
    }

    [Fact]
    public void TryGetValueAnnotationEstablishesTheRuntimeNonNullInvariant()
    {
        Option<string?> option = Option.Some<string?>("value");

        if (!option.TryGetValue(out var value))
        {
            Assert.Fail("Expected a present value.");
        }

        Assert.Equal(5, value.Length);
    }

    [Fact]
    public void FromTrySupportsMethodGroupsAndBoundInputLambdas()
    {
        static bool TryGetNumber(out int value)
        {
            value = 42;
            return true;
        }

        var fromMethodGroup = Option.FromTry<int>(TryGetNumber);
        var text = "123";
        var fromBoundInput = Option.FromTry<int>((out int value) => int.TryParse(text, out value));

        Assert.Equal(Option.Some(42), fromMethodGroup);
        Assert.Equal(Option.Some(123), fromBoundInput);
    }

    [Fact]
    public void FromTryUsesTheBooleanAndNormalizesSuccessfulNullOutputs()
    {
        var calls = 0;
        var failed = Option.FromTry<int>((out int value) =>
        {
            calls++;
            value = 42;
            return false;
        });
        var nullSuccess = Option.FromTry<string>((out string? value) =>
        {
            value = null;
            return true;
        });
        var nullableNullSuccess = Option.FromTry<int?>((out int? value) =>
        {
            value = null;
            return true;
        });

        Assert.True(failed.IsNone);
        Assert.True(nullSuccess.IsNone);
        Assert.True(nullableNullSuccess.IsNone);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void FromTryPreservesNestedAbsenceAndCallbackExceptions()
    {
        var expected = new InvalidOperationException("try failed");
        var nested = Option.FromTry<Option<int>>((out Option<int> value) =>
        {
            value = Option.None<int>();
            return true;
        });

        Assert.Equal(Option.Some(Option.None<int>()), nested);
        Assert.Throws<ArgumentNullException>(() => Option.FromTry<int>(null!));
        var actual = Assert.Throws<InvalidOperationException>(
            () => Option.FromTry<int>((out int _) => throw expected));
        Assert.Same(expected, actual);
    }

    [Fact]
    public void NullableConversionPreservesNestedAbsence()
    {
        Option<int>? nestedValue = Option.None<int>();

        var nested = Option.FromNullable(nestedValue);

        Assert.Equal(Option.Some(Option.None<int>()), nested);
    }

    [Fact]
    public void DictionaryLookupDistinguishesMissingDefaultNullAndNestedValues()
    {
        IReadOnlyDictionary<string, int> numbers = new Dictionary<string, int>
        {
            ["zero"] = 0,
        };
        IReadOnlyDictionary<string, string?> text = new Dictionary<string, string?>
        {
            ["null"] = null,
        };
        IReadOnlyDictionary<string, int?> nullableNumbers = new Dictionary<string, int?>
        {
            ["null"] = null,
            ["zero"] = 0,
        };
        IReadOnlyDictionary<string, Option<int>> nested = new Dictionary<string, Option<int>>
        {
            ["none"] = Option.None<int>(),
        };

        Assert.Equal(Option.Some(0), numbers.GetOption("zero"));
        Assert.True(numbers.GetOption("missing").IsNone);
        Assert.True(text.GetOption("null").IsNone);
        Assert.True(nullableNumbers.GetOption("null").IsNone);
        Assert.Equal(Option.Some<int?>((int?)0), nullableNumbers.GetOption("zero"));
        Assert.Equal(Option.Some(Option.None<int>()), nested.GetOption("none"));
    }

    [Fact]
    public void DictionaryLookupUsesTryGetValueOnceAndPassesTheKeyThrough()
    {
        var key = new object();
        var dictionary = new CountingReadOnlyDictionary<object, int>(key, 0, found: true);

        var result = dictionary.GetOption(key);

        Assert.Equal(Option.Some(0), result);
        Assert.Equal(1, dictionary.TryGetValueCalls);
        Assert.Same(key, dictionary.ObservedKey);
    }

    [Fact]
    public void DictionaryLookupValidatesOnlyTheReceiverAndPreservesDictionaryExceptions()
    {
        IReadOnlyDictionary<string, int> missing = null!;
        var expected = new InvalidOperationException("lookup failed");
        var dictionary = new CountingReadOnlyDictionary<string, int>(expected);

        Assert.Throws<ArgumentNullException>(() => missing.GetOption("key"));
        var actual = Assert.Throws<InvalidOperationException>(() => dictionary.GetOption("key"));

        Assert.Same(expected, actual);
        Assert.Equal(1, dictionary.TryGetValueCalls);
    }

    private sealed class CountingReadOnlyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        private readonly TKey? expectedKey;
        private readonly TValue? storedValue;
        private readonly bool found;
        private readonly Exception? exception;

        public CountingReadOnlyDictionary(TKey expectedKey, TValue storedValue, bool found)
        {
            this.expectedKey = expectedKey;
            this.storedValue = storedValue;
            this.found = found;
        }

        public CountingReadOnlyDictionary(Exception exception)
        {
            this.exception = exception;
        }

        public int TryGetValueCalls { get; private set; }

        public TKey? ObservedKey { get; private set; }

        public TValue this[TKey key] => throw new InvalidOperationException("Indexer must not be used.");

        public IEnumerable<TKey> Keys => throw new InvalidOperationException("Keys must not be used.");

        public IEnumerable<TValue> Values => throw new InvalidOperationException("Values must not be used.");

        public int Count => throw new InvalidOperationException("Count must not be used.");

        public bool ContainsKey(TKey key) => throw new InvalidOperationException("ContainsKey must not be used.");

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() =>
            throw new InvalidOperationException("Enumeration must not be used.");

        public bool TryGetValue(TKey key, out TValue value)
        {
            TryGetValueCalls++;
            ObservedKey = key;

            if (exception is not null)
            {
                throw exception;
            }

            value = storedValue!;
            return found && EqualityComparer<TKey>.Default.Equals(expectedKey!, key);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
