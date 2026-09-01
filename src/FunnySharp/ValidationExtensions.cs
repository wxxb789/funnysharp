namespace FunnySharp;

/// <summary>
/// Provides applicative operations for validations.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Applies a valid function to a valid argument, accumulating errors from the function before errors
    /// from the argument.
    /// </summary>
    /// <typeparam name="TValue">The argument value type.</typeparam>
    /// <typeparam name="TResult">The result value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="function">The validation containing the function.</param>
    /// <param name="argument">The validation containing the argument.</param>
    /// <returns>
    /// A valid result when both validations are valid; otherwise, an invalid validation containing all
    /// errors in function-then-argument order.
    /// </returns>
    public static Validation<TResult, TError> Apply<TValue, TResult, TError>(
        this Validation<Func<TValue, TResult>, TError> function,
        Validation<TValue, TError> argument) =>
        function.Zip(argument).Map(pair => pair.First(pair.Second));
}
