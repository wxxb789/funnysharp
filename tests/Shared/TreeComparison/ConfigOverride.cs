using FunnySharp.TreeTransformations;

namespace FunnySharp.TestSupport.TreeComparison;

/// <summary>One environment override: a parsed path and its replacement value.</summary>
internal sealed record ConfigOverride(string PathText, TreePath Path, string Value);
