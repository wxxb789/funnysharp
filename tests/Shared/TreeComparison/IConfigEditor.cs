using FunnySharp;
using FunnySharp.TreeTransformations;

namespace FunnySharp.TestSupport.TreeComparison;

/// <summary>The complete configuration operation compared across every consumer.</summary>
internal interface IConfigEditor
{
    string Name { get; }

    ConfigEditStats Stats { get; }

    void ResetStats();

    Validation<ConfigNode, ConfigError> Apply(IReadOnlyList<ConfigOverride> overrides);

    Result<ConfigNode, ConfigError> ApplyFirst(IReadOnlyList<ConfigOverride> overrides);

    Validation<ConfigNode, ConfigError> Validate();
}
