using System.Collections;

namespace FunnySharp.Tests;

public sealed class GeneratedRecordContractTests
{
    [Fact]
    public void RawPartitionRecordsPreserveReferencesSupportShallowInitCopiesAndUsePropertyComparers()
    {
        var first = new ComparisonList();
        var second = new ComparisonList();
        var partition = new Partition<int>(first, first);
        var options = new OptionPartition<int>(first, -1);
        var results = new ResultPartition<int, int>(first, first);
        var units = new UnitResultPartition<int>(-1, first);

        CheckRecord(partition, partition with { True = second }, [first, first]);
        CheckRecord(options, options with { Somes = second }, [first, -1]);
        CheckRecord(results, results with { Passed = second }, [first, first]);
        CheckRecord(units, units with { Failed = second }, [-1, first]);
        Assert.True(partition == partition with { True = second });
        Assert.False(partition != partition with { True = second });
        Assert.True(options == options with { Somes = second });
        Assert.False(options != options with { Somes = second });
        Assert.True(results == results with { Passed = second });
        Assert.False(results != results with { Passed = second });
        Assert.True(units == units with { Failed = second });
        Assert.False(units != units with { Failed = second });
    }

    private static void CheckRecord<T>(T original, T replaced, object[] positionalValues)
        where T : class, IEquatable<T>
    {
        var cloneMethod = typeof(T).GetMethod("<Clone>$");
        Assert.NotNull(cloneMethod);
        var clone = Assert.IsType<T>(cloneMethod.Invoke(original, []));
        Assert.NotSame(original, clone);
        Assert.True(original.Equals(clone));
        Assert.True(original.Equals(replaced));
        Assert.True(original.Equals((object)clone));
        Assert.False(original.Equals(new object()));
        Assert.Equal(original.GetHashCode(), clone.GetHashCode());
        Assert.Equal(original.GetHashCode(), replaced.GetHashCode());
        foreach (var property in typeof(T).GetProperties())
        {
            Assert.Equal(property.GetValue(original), property.GetValue(clone));
            if (property.PropertyType != typeof(int))
            {
                Assert.Same(property.GetValue(original), property.GetValue(clone));
            }
        }

        var deconstruct = typeof(T).GetMethod("Deconstruct");
        Assert.NotNull(deconstruct);
        var output = new object?[2];
        _ = deconstruct.Invoke(original, output);
        Assert.Equal(positionalValues.Length, output.Length);
        for (var index = 0; index < positionalValues.Length; index++)
        {
            if (positionalValues[index] is ComparisonList)
            {
                Assert.Same(positionalValues[index], output[index]);
            }
            else
            {
                Assert.Equal(positionalValues[index], output[index]);
            }
        }

        Assert.Same(ComparisonList.FormattingFailure,
            Assert.Throws<InvalidOperationException>(() => original.ToString()));
        var nullArguments = positionalValues.Select(value => value is int ? value : null).ToArray();
        var rawNull = Assert.IsType<T>(Activator.CreateInstance(typeof(T), nullArguments));
        foreach (var property in typeof(T).GetProperties().Where(property => property.PropertyType != typeof(int)))
        {
            Assert.Null(property.GetValue(rawNull));
        }
    }

    private sealed class ComparisonList : IReadOnlyList<int>
    {
        internal static readonly InvalidOperationException FormattingFailure = new("Formatting witness.");
        public int Count => throw new InvalidOperationException("Unexpected collection access.");
        public int this[int index] => throw new InvalidOperationException("Unexpected collection access.");
        public IEnumerator<int> GetEnumerator() => throw new InvalidOperationException("Unexpected enumeration.");
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public override bool Equals(object? obj) => obj is ComparisonList;
        public override int GetHashCode() => 23;
        public override string ToString() => throw FormattingFailure;
    }
}
