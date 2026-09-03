using FunnySharp;

namespace FunnySharp.DocumentationSamples;

internal static class EffectSamples
{
    private static async Task CreateAndRunAsync(CancellationToken cancellationToken)
    {
        // <snippet DocumentationSamples.Effects.CreateAndRun>
        var greeting = Effect.FromSync(() => "hello")
            .Map(text => text.ToUpperInvariant());

        var value = await greeting.RunAsync(cancellationToken);
        // </snippet>
    }

    private static async Task ProvideEnvironmentAsync()
    {
        // <snippet DocumentationSamples.Effects.ProvideEnvironment>
        var now = Effect
            .FromSync((TimeProvider clock) => clock.GetUtcNow())
            .Provide(TimeProvider.System);

        var observed = await now.RunAsync();
        // </snippet>
    }
}
