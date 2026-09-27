namespace FunnySharp.Analyzers;

/// <summary>
/// The diagnostic identifiers and the shared category of the FunnySharp analyzer package.
/// </summary>
/// <remarks>
/// <c>FS1001</c> through <c>FS1005</c> are the package diagnostics delivered by Goal 21; they
/// never collide with the experimental-member diagnostic ids (<c>FS0017</c> and successors),
/// which the compiler generates from <see cref="System.Diagnostics.CodeAnalysis.ExperimentalAttribute"/>
/// and which are recorded separately in <c>docs/stability-inventory.md</c>.
/// </remarks>
public static class DiagnosticIds
{
    /// <summary>The category reported by every FunnySharp package diagnostic.</summary>
    public const string Category = "FunnySharp";

    /// <summary>An uninitialized semantic carrier was created through <c>default</c> or <c>new()</c>.</summary>
    public const string UninitializedCarrier = "FS1001";

    /// <summary>A FunnySharp outcome value was silently discarded as a statement.</summary>
    public const string DiscardedOutcome = "FS1002";

    /// <summary>The Boolean presence result of a <c>TryGet*</c> member was ignored.</summary>
    public const string IgnoredTryGetResult = "FS1003";

    /// <summary>A ValueTask returned by FunnySharp was blocked instead of awaited.</summary>
    public const string BlockedValueTask = "FS1004";

    /// <summary>A resource that also implements <c>IAsyncDisposable</c> was scoped with the synchronous <c>Using</c>.</summary>
    public const string SyncDisposeOfAsyncDisposable = "FS1005";
}
