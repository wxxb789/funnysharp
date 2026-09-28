namespace FunnySharp.Analyzers;

using Microsoft.CodeAnalysis;

/// <summary>
/// The diagnostic descriptors of the FunnySharp analyzer package.
/// </summary>
/// <remarks>
/// Every diagnostic is enabled by default, configurable, and suppressible through the standard
/// mechanisms (<c>#pragma warning disable</c>, <c>NoWarn</c>, <c>SuppressMessageAttribute</c>),
/// following the recorded packaging decision that no diagnostic may be
/// <c>NotConfigurable</c>. Severities: <c>FS1001</c> is an error because every read of an
/// uninitialized carrier throws at run time; the remaining diagnostics are warnings because they
/// describe silent-behavior hazards rather than guaranteed failures.
/// </remarks>
internal static class Descriptors
{
    private const string HelpLinkBase = "https://github.com/wxxb789/funnysharp/blob/main/docs/analyzers.md";

    /// <summary>FS1001: a carrier with no valid default was created through default or new().</summary>
    public static readonly DiagnosticDescriptor UninitializedCarrier = new(
        id: DiagnosticIds.UninitializedCarrier,
        title: "Do not create a FunnySharp carrier with default or new()",
        messageFormat: "'{0}' has no valid default value: every member that reads a default {0} throws InvalidOperationException; create it with a factory instead",
        category: DiagnosticIds.Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Result<TValue, TError>, UnitResult<TError>, Validation<TValue, TError>, NonEmpty<T>, Effect<T>, Effect<TEnvironment, T>, Lens<TSource, TFocus>, and Optional<TSource, TFocus> are uninitialized in their default state, and every member that reads them throws InvalidOperationException. Use the factories (Success/Failure, Valid/Invalid, ToNonEmptyOrNone, the Effect factories, Create) instead. Option<T> (default is None) and TransitionResult (default is Undefined) are excluded because their defaults are valid.",
        helpLinkUri: HelpLinkBase + "#fs1001-uninitialized-semantic-carrier");

    /// <summary>FS1002: an outcome value was silently discarded as a statement.</summary>
    public static readonly DiagnosticDescriptor DiscardedOutcome = new(
        id: DiagnosticIds.DiscardedOutcome,
        title: "Do not silently discard a FunnySharp outcome",
        messageFormat: "This statement discards the {0} produced by the call; assign it, return it, await it, or discard it explicitly with '_ ='",
        category: DiagnosticIds.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A statement whose value is an Option, Result, UnitResult, Validation, TransitionResult, Effect, composed StateTransition or StateMachine delegate, or a Task/ValueTask of one of those, drops absence, failure, accumulated-validation, transition, or deferred-work information without any trace. Unawaited Task/ValueTask results of FunnySharp members additionally mean the work never runs. Assign the outcome, return it, await it, or mark the discard explicit with '_ = expression;'.",
        helpLinkUri: HelpLinkBase + "#fs1002-silently-discarded-outcome");

    /// <summary>FS1003: the Boolean presence result of a TryGet* member was ignored.</summary>
    public static readonly DiagnosticDescriptor IgnoredTryGetResult = new(
        id: DiagnosticIds.IgnoredTryGetResult,
        title: "Use the Boolean result of a FunnySharp TryGet* member",
        messageFormat: "The Boolean result of '{0}' is ignored, so the out value may be default; use the result (for example in an if statement), declare the out argument as a discard, or discard the call explicitly",
        category: DiagnosticIds.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "FunnySharp TryGet* members follow the Try pattern: the Boolean result reports presence, success, or failure, and the out value is default when it is false. Calling such a member as a bare statement loses that information and reads a default value. Use the Boolean result, declare the out argument as '_' when only the side effect matters, or discard the whole call explicitly.",
        helpLinkUri: HelpLinkBase + "#fs1003-ignored-tryget-presence-result");

    /// <summary>FS1004: a ValueTask returned by FunnySharp was blocked instead of awaited.</summary>
    public static readonly DiagnosticDescriptor BlockedValueTask = new(
        id: DiagnosticIds.BlockedValueTask,
        title: "Await the ValueTask returned by FunnySharp instead of blocking on it",
        messageFormat: "Do not block on this ValueTask: it follows the single-consumption rule and may be backed by pooled resources, so await it once instead of accessing {0}",
        category: DiagnosticIds.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "FunnySharp's *ValueAsync members return ValueTask<TResult> under the documented single-consumption rule, and the underlying value may be backed by pooled resources. Accessing Result or GetAwaiter().GetResult() on such a ValueTask blocks and may consume it incorrectly. Await the ValueTask once, or convert it with AsTask when an awaitable Task is required.",
        helpLinkUri: HelpLinkBase + "#fs1004-blocked-valuetask");

    /// <summary>FS1005: an async-disposable resource was scoped with the synchronous Using.</summary>
    public static readonly DiagnosticDescriptor SyncDisposeOfAsyncDisposable = new(
        id: DiagnosticIds.SyncDisposeOfAsyncDisposable,
        title: "Use UsingAsync for a resource that implements IAsyncDisposable",
        messageFormat: "'{0}' implements IAsyncDisposable, but Using runs only the synchronous Dispose; use UsingAsync so DisposeAsync runs",
        category: DiagnosticIds.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "EffectResourceExtensions.Using scopes the resource lifetime with the synchronous Dispose, so any asynchronous cleanup implemented through IAsyncDisposable never runs. When the acquired resource type implements IAsyncDisposable, UsingAsync disposes it with DisposeAsync instead.",
        helpLinkUri: HelpLinkBase + "#fs1005-synchronous-dispose-of-an-async-disposable-resource");
}
