#pragma warning disable FS0017

using FunnySharp;
using FunnySharp.TreeTransformations;

namespace FunnySharp.TestSupport.TreeComparison;

/// <summary>Builds the fixed diagnostics shared by every compared consumer.</summary>
internal static class ConfigErrors
{
    internal static ConfigError Path(ConfigOverride item, TreeEditErrorKind kind, Location location) =>
        new(
            kind == TreeEditErrorKind.IndexOutOfRange
                ? ConfigErrorKind.IndexOutOfRange
                : ConfigErrorKind.PathNotFound,
            location,
            $"The override path '{item.PathText}' did not resolve.");

    internal static ConfigError Path(ConfigOverride item, TreePathSegment segment, Location location) =>
        new(
            segment.Kind == TreePathSegmentKind.Index
                ? ConfigErrorKind.IndexOutOfRange
                : ConfigErrorKind.PathNotFound,
            location,
            $"The override path '{item.PathText}' did not resolve.");

    internal static ConfigError NotAValue(ConfigOverride item, Location location) =>
        new(
            ConfigErrorKind.NotAValue,
            location,
            $"The override path '{item.PathText}' targets a section.");

    internal static ConfigError InvalidValue(ConfigOverride item, Location location) =>
        new(
            ConfigErrorKind.InvalidValue,
            location,
            $"The value '{item.Value}' is not valid for '{item.PathText}'.");

    internal static ConfigError Duplicate(ConfigOverride item) =>
        new(
            ConfigErrorKind.DuplicatePath,
            item.Path.ToLocation(),
            $"The override path '{item.PathText}' appears more than once.");

    internal static ConfigError InvalidBase(string name, string value, Location location) =>
        new(
            ConfigErrorKind.InvalidBaseValue,
            location,
            $"The base value '{value}' is not valid for '{name}'.");
}

/// <summary>Creates the canonical outcome for an accumulated error list.</summary>
internal static class ConfigResults
{
    internal static Validation<ConfigNode, ConfigError> From(
        ConfigNode tree,
        List<ConfigError> errors) =>
        errors.Count == 0
            ? Validation<ConfigNode, ConfigError>.Valid(tree)
            : Validation<ConfigNode, ConfigError>.InvalidMany(errors);
}
