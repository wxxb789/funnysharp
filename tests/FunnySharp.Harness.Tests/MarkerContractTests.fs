module FunnySharp.Harness.Tests.MarkerContractTests

open System
open System.IO
open System.Text.RegularExpressions
open FunnySharp.Harness.ToolingVerify
open FunnySharp.Harness.ReleaseVerifySource
open FunnySharp.Harness.Tests.Support
open Xunit

// The marker contract binds the local ToolingVerify verdict rules and the
// ReleaseVerifySource verdict rules to the same canonical log shapes. Each
// Fact below fails when either rule set drifts away from the shared shape,
// so a fragment edit can no longer pass the local pre-check while the
// release gate rejects the same log (the green/red split).
type MarkerContractTests() =

    // Reuse the shared root finder (anchored on FunnySharp.slnx).
    let repositoryRoot () = Support.repositoryRoot ()
    let fakePassedAssemblyLine root (relative: string) =
        let assembly = Path.GetFullPath(Path.Combine(root, relative))
        sprintf "%s (net10.0|x64) passed (1762 tests)" assembly

    let canonicalTestLogWithAssemblies root =
        MarkerContract.CanonicalTestLog
        + "\n"
        + (testAssemblyRelativePaths |> List.map (fakePassedAssemblyLine root) |> String.concat "\n")

    [<Fact>]
    member _.MarkerContractPresentInRealVerifier() =
        // The release verifier source must keep carrying the same verdict
        // fragments the local parser is built from; a reworded summary line in
        // either file is the silent green/red split this contract forbids.
        let root = repositoryRoot ()
        let source = File.ReadAllText(Path.Combine(root, "eng", "harness", "ReleaseVerifySource.fs"))

        for fragment in [ "Build succeeded\\."; "0 Warning\\(s\\)"; "0 Error\\(s\\)" ] do
            Assert.Contains(fragment, source)

        // Both rule sets must accept the canonical log shapes.
        let buildLog = MarkerContract.CanonicalBuildLog
        let testLog = canonicalTestLogWithAssemblies root

        assertBuildMarkers buildLog
        assertTestMarkers testLog root |> ignore

        Assert.True(Option.isNone (stepFailure "build" buildLog "" root))
        Assert.True(Option.isNone (stepFailure "test" testLog "" root))

    [<Fact>]
    member _.MarkerGuardFailureOnMutatedVerifier() =
        // A single-point mutation of each canonical shape must be rejected by
        // the local verdict rules: the guard must be able to fail, not just pass.
        let root = repositoryRoot ()

        Assert.True(
            stepFailure "build" (MarkerContract.CanonicalBuildLog.Replace("Build succeeded.", "Build completed.")) "" root
            |> Option.isSome,
            "a reworded build success line must fail the local build verdict"
        )

        Assert.True(
            stepFailure "build" (MarkerContract.CanonicalBuildLog.Replace("0 Warning(s)", "1 Warning(s)")) "" root
            |> Option.isSome,
            "a warning count above zero must fail the local build verdict"
        )

        Assert.True(
            stepFailure "test" (MarkerContract.CanonicalTestLog.Replace("Failed:     0", "Failed:     1")) "" root
            |> Option.isSome,
            "a failed count above zero must fail the local test verdict"
        )

        Assert.True(
            stepFailure "test" (MarkerContract.CanonicalTestLog.Replace("Skipped:     0", "Skipped:     2")) "" root
            |> Option.isSome,
            "a skipped count above zero must fail the local test verdict"
        )

    [<Fact>]
    member _.BothVerdictRuleSetsAcceptTheSameCanonicalLogs() =
        // The shared fixture: the exact same log bytes go through the release
        // verifier's verdict functions and the local parser's stepFailure, and
        // both must agree they pass.
        let root = repositoryRoot ()
        let buildLog = MarkerContract.CanonicalBuildLog
        let testLog = canonicalTestLogWithAssemblies root

        assertBuildMarkers buildLog
        assertTestMarkers testLog root |> ignore

        Assert.True(
            Option.isNone (stepFailure "build" buildLog "" root),
            "local build verdict must accept the canonical build log"
        )

        Assert.True(
            Option.isNone (stepFailure "test" testLog "" root),
            "local test verdict must accept the canonical test log"
        )
