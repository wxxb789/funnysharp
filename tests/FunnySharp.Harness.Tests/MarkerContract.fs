// The local ToolingVerify parser and the ReleaseVerifySource verdict functions
// parse the same canonical build/test log shapes. These fixtures are the exact
// bytes both rule sets must keep recognizing; a divergence between them is a
// silent green/red split between the local pre-check and the release gate.
module FunnySharp.Harness.Tests.MarkerContract

/// Canonical `dotnet build` success output.
[<Literal>]
let CanonicalBuildLog =
    "MSBuild version 17.14.0 for .NET 10\n\
     Restore complete (0.9s)\n\
     FunnySharp -> eng\\harness\\bin\\Debug\\net10.0\\FunnySharp.Harness.dll\n\
     \n\
     Build succeeded.\n\
         0 Warning(s)\n\
         0 Error(s)\n\
     \n\
     Time Elapsed 00:00:02.11\n"

/// Canonical Microsoft.Testing.Platform `dotnet test` summary output.
[<Literal>]
let CanonicalTestLog =
    "Test run summary: Passed!\n  total: 1762\n  failed: 0\n  succeeded: 1762\n  skipped: 0\n"
