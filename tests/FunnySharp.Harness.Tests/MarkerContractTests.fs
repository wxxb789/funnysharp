module FunnySharp.Harness.Tests.MarkerContractTests

open System
open System.IO
open FunnySharp.Harness.ToolingVerify
open FunnySharp.Harness.Tests.Support
open Xunit

type MarkerContractTests() =
    let testLog root =
        MarkerContract.CanonicalTestLog + "\n" +
        (testAssemblyRelativePaths
         |> List.map (fun relative -> sprintf "%s (net10.0|x64) passed (1762 tests)" (Path.GetFullPath(Path.Combine(root, relative))))
         |> String.concat "\n")

    [<Fact>]
    member _.CanonicalBuildAndTestResultsPass() =
        let root = repositoryRoot ()
        Assert.True(stepFailure "build" MarkerContract.CanonicalBuildLog "" root |> Option.isNone)
        Assert.True(stepFailure "test" (testLog root) "" root |> Option.isNone)

    [<Theory>]
    [<InlineData("build", "0 Warning(s)", "1 Warning(s)")>]
    [<InlineData("build", "0 Error(s)", "1 Error(s)")>]
    [<InlineData("test", "failed: 0", "failed: 1")>]
    [<InlineData("test", "skipped: 0", "skipped: 1")>]
    [<InlineData("test", "succeeded: 1762", "succeeded: 1761")>]
    member _.UnsuccessfulResultsFail(step: string, before: string, after: string) =
        let root = repositoryRoot ()
        let log = if step = "build" then MarkerContract.CanonicalBuildLog else testLog root
        Assert.True(stepFailure step (log.Replace(before, after)) "" root |> Option.isSome)
