using FunnySharp;

namespace FunnySharp.DocumentationSamples;

internal static class FunctionCompositionSamples
{
    private static async Task PipeAsync()
    {
        // <snippet DocumentationSamples.FunctionComposition.PipeAsync>
        var result = await 4.Pipe(async value =>
        {
            await Task.Yield();
            return value * 3;
        });
        // </snippet>
    }
}
