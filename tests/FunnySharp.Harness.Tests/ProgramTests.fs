module FunnySharp.Harness.Tests.ProgramTests

open System
open Xunit
open FunnySharp.Harness.Output
open FunnySharp.Harness.Proc
open FunnySharp.Harness.Tests.Support

let private assertHelpCommands arguments = task {
    let assembly = typeof<ProcessResult>.Assembly.Location
    let! result = runCaptureIn (Some(repositoryRoot ())) "dotnet" (assembly :: arguments) |> Async.StartAsTask
    Assert.True(result.ExitCode = 0, result.Stdout + result.Stderr)
    let commands =
        splitLines result.Stdout
        |> Array.map (fun line -> line.Trim())
        |> Array.filter (fun line -> line.StartsWith("dotnet fsi ", StringComparison.Ordinal))
    Assert.NotEmpty commands
    for command in commands do
        let words = command.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        Assert.Equal("build.fsx", words.[2])
}

type ProgramTests() =
    [<Theory>]
    [<InlineData("test", "-h", false)>]
    [<InlineData("verify-docs-snippets", "--help", false)>]
    [<InlineData("release-verify", "-h", true)>]
    member _.SelectedPipelineHelpPrintsExecutableCommands(pipeline: string, help: string, verbose: bool) =
        assertHelpCommands ([ "-p"; pipeline; help ] @ if verbose then [ "--verbose" ] else [])

    [<Fact>]
    member _.GlobalHelpPrintsExecutableCommands() =
        assertHelpCommands [ "--help" ]
