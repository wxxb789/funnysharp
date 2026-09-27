; Unshipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
FS1001  | FunnySharp | Error   | A carrier with no valid default was created through default or new() (Goal 21).
FS1002  | FunnySharp | Warning | A FunnySharp outcome value was silently discarded as a statement (Goal 21).
FS1003  | FunnySharp | Warning | The Boolean presence result of a FunnySharp TryGet* member was ignored (Goal 21).
FS1004  | FunnySharp | Warning | A ValueTask returned by a FunnySharp member was blocked instead of awaited (Goal 21).
FS1005  | FunnySharp | Warning | A resource that also implements IAsyncDisposable was scoped with the synchronous Using (Goal 21).
