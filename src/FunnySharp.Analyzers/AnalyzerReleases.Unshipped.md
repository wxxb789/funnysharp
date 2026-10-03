; Unshipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
FS1001  | FunnySharp | Error   | A carrier with no valid default was created through default or parameterless new(), including an empty object initializer (Goal 21).
FS1002  | FunnySharp | Warning | A FunnySharp outcome value was silently discarded; its code fix requires a true discard target (Goal 21).
FS1003  | FunnySharp | Warning | The Boolean presence result of a FunnySharp TryGet* member was ignored (Goal 21).
FS1004  | FunnySharp | Warning | A relevant ValueTask is accessed without a completed-success, single-consumption local proof (Goal 21).
FS1005  | FunnySharp | Warning | A resource that also implements IAsyncDisposable was scoped with the synchronous Using (Goal 21).
