using FunnySharp;

namespace FunnySharp.TestSupport;

internal static class ValidationUnwrap
{
    internal static T Require<T, TError>(this Validation<T, TError> validation) =>
        validation.Match(
            static value => value,
            static errors => throw new InvalidOperationException(
                string.Join("; ", errors.Select(static error => error!.ToString()))));
}
