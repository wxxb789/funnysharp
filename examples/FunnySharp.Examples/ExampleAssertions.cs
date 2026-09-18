internal static class ExampleAssertions
{
    internal static async Task FaultIsPreserved(Task operation, Exception expected)
    {
        try
        {
            await operation;
            throw new InvalidOperationException("Expected the operation to preserve the task failure.");
        }
        catch (Exception actual) when (ReferenceEquals(actual, expected))
        {
        }
    }

    internal static async Task CancellationIsPreserved(Task operation)
    {
        try
        {
            await operation;
            throw new InvalidOperationException("Expected the operation to preserve task cancellation.");
        }
        catch (OperationCanceledException)
        {
        }
    }

    internal static void Equal<T>(T expected, T actual)
        where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected}', but received '{actual}'.");
        }
    }

    internal static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    internal static void SequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException("The sequences were not equal.");
        }
    }

    internal static void UninitializedThrows(Func<object?> read)
    {
        try
        {
            _ = read();
            throw new InvalidOperationException("Expected the uninitialized unit result to throw.");
        }
        catch (InvalidOperationException exception) when (
            exception.Message == "The unit result has not been initialized.")
        {
        }
    }
}
