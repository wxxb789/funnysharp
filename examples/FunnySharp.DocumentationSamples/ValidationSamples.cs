using FunnySharp;

namespace FunnySharp.DocumentationSamples;

internal static class ValidationSamples
{
    private static async Task TraverseValueAsync(IAsyncEnumerable<string> source)
    {
        // <snippet DocumentationSamples.Validation.TraverseValueAsync>
        await source.TraverseValueAsync(item => new ValueTask<Option<string>>(LookupAsync(item)));
        // </snippet>
    }

    private static Task<Option<string>> LookupAsync(string item) =>
        Task.FromResult(Option.Some(item));
}
