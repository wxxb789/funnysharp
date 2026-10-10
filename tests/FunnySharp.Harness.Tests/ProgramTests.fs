module FunnySharp.Harness.Tests.ProgramTests

open Xunit
open FunnySharp.Harness

type ProgramTests() =
    [<Theory>]
    [<InlineData("build")>]
    [<InlineData("test")>]
    [<InlineData("format")>]
    member _.NativeCommandsUseSolutionDefaults(pipeline: string) =
        let expected =
            match pipeline with
            | "test" -> [ "test"; "--solution"; "FunnySharp.slnx" ]
            | "format" -> [ "format"; "FunnySharp.slnx"; "--verify-no-changes"; "--no-restore" ]
            | _ -> [ "build"; "FunnySharp.slnx" ]
        Assert.Equal<Result<string list, string>>(Ok expected, FunnySharp.Harness.Cli.nativeArguments pipeline [])

    [<Theory>]
    [<InlineData("build")>]
    [<InlineData("test")>]
    [<InlineData("format")>]
    member _.NativeCommandsPreserveTargetAndForwardOptions(pipeline: string) =
        let target = "tests/Project With Spaces/Test.fsproj"
        let options = [ "--no-restore"; "--verbosity"; "minimal"; "-c"; "Release" ]
        let arguments = [ "--no-restore"; "--project"; target; "--verbosity"; "minimal"; "-c"; "Release" ]
        let prefix =
            match pipeline with
            | "test" -> [ "test"; "--project"; target ]
            | "format" -> [ "format"; target; "--verify-no-changes"; "--no-restore" ]
            | _ -> [ "build"; target ]
        Assert.Equal<Result<string list, string>>(Ok(prefix @ options), FunnySharp.Harness.Cli.nativeArguments pipeline arguments)
        Assert.Equal<Result<string list, string>>(Ok prefix, FunnySharp.Harness.Cli.nativeArguments pipeline [ "--project=" + target ])

    [<Theory>]
    [<InlineData("build")>]
    [<InlineData("test")>]
    [<InlineData("format")>]
    member _.NativeCommandsRejectMalformedProjectTargets(pipeline: string) =
        for arguments in [ [ "--project" ]; [ "--project=" ]; [ "--project"; "--no-restore" ]; [ "--project"; "first"; "--project"; "second" ] ] do
            match FunnySharp.Harness.Cli.nativeArguments pipeline arguments with
            | Error _ -> ()
            | Ok command -> Assert.Fail("accepted malformed project arguments: " + String.concat " " command)


    [<Fact>]
    member _.NamedEvaluationArgumentsKeepPositionalOrder() =
        let result = Cli.adaptNamed [ "--task"; "--style"; "--run-dir"; "--round" ] [ "--replay" ]
                         [ "--task"; "--style"; "--run-dir" ]
                         [| "--round"; "2"; "--style=funnysharp"; "--task"; "concurrency"; "--run-dir"; "path with spaces"; "--replay" |]
        Assert.Equal<Result<string array, string>>(Ok [| "concurrency"; "funnysharp"; "path with spaces"; "--round"; "2"; "--replay" |], result)

    [<Theory>]
    [<InlineData("--unknown")>]
    [<InlineData("--task")>]
    member _.NamedArgumentsRejectMalformedInput(argument: string) =
        let result = Cli.adaptNamed [ "--task" ] [] [ "--task" ] [| argument |]
        Assert.True(Result.isError result)
