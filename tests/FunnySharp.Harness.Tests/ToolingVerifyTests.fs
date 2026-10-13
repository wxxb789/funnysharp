module FunnySharp.Harness.Tests.ToolingVerifyTests

open System
open System.IO
open System.Text.Json
open Xunit
open FunnySharp.Harness.Proc
open FunnySharp.Harness.ToolingVerify
open FunnySharp.Harness.Tests.Support

let private greenOutput step root =
    match step with
    | "build" -> "Build succeeded.\n    0 Warning(s)\n    0 Error(s)\n"
    | "test" ->
        let assemblies =
            testAssemblyRelativePaths
            |> List.map (fun relative ->
                sprintf "%s (net10.0|x64) passed (1.2s)" (Path.GetFullPath(Path.Combine(root, relative))))
        String.concat "\n" assemblies
        + "\nTest run summary: Passed!\n  total: 12\n  failed: 0\n  succeeded: 12\n  skipped: 0\n"
    | "examples" -> CoreExampleMarker
    | "aspnetcore-examples" -> AspNetExampleMarker
    | "docs" -> "Verified 3 C# documentation snippets across 2 primary guides."
    | _ -> ""

let private stepName = function
    | DocumentationSnippets _ -> "docs"
    | ChildProcess(_, "run" :: args) ->
        if args |> List.exists (fun arg -> arg.Contains "AspNetCore") then "aspnetcore-examples" else "examples"
    | ChildProcess(_, name :: _) -> name
    | command -> failwithf "unexpected command: %A" command

let private success stdout = { ExitCode = 0; Stdout = stdout; Stderr = "" }

/// A source-independent checkout and runner; no real SDK or process-global changes.
type private Fixture() =
    let temp = new TempDirectory()
    let calls = ResizeArray<string * Map<string, string> * string>()
    let bin = Path.Combine(temp.Path, "bin")
    do
        File.WriteAllText(Path.Combine(temp.Path, "FunnySharp.slnx"), "<Solution />")
        Directory.CreateDirectory bin |> ignore
        let name = if OperatingSystem.IsWindows() then "dotnet.exe" else "dotnet"
        let executable = Path.Combine(bin, name)
        File.WriteAllText(executable, "#!/bin/sh\nexit 0\n")
        if not (OperatingSystem.IsWindows()) then
            File.SetUnixFileMode(executable, UnixFileMode.UserRead ||| UnixFileMode.UserWrite ||| UnixFileMode.UserExecute)

    member _.Root = temp.Path
    member _.Calls = List.ofSeq calls
    member _.Env = Map.ofList [ "PATH", bin ]
    member _.Run(flags, env, overrideRun: string -> ProcessResult option, docs) =
        use stdout = new StringWriter()
        use stderr = new StringWriter()
        let runner command childEnv cwd =
            let name = stepName command
            calls.Add(name, childEnv, cwd)
            overrideRun name |> Option.defaultWith (fun () -> success (greenOutput name temp.Path))
        let code =
            mainWith stdout stderr temp.Path flags
                { Env = env; Runner = runner; DocsVerifier = docs }
        code, stdout.ToString(), stderr.ToString()
    member this.Run(flags) =
        this.Run(flags, this.Env, (fun _ -> None), Some(fun _ -> success (greenOutput "docs" temp.Path)))
    interface IDisposable with
        member _.Dispose() = (temp :> IDisposable).Dispose()

type VerdictTests() =
    [<Theory>]
    [<InlineData("build")>]
    [<InlineData("test")>]
    [<InlineData("examples")>]
    [<InlineData("aspnetcore-examples")>]
    [<InlineData("docs")>]
    member _.AcceptsActualGreenOutputAndRejectsMissingVerdict(step: string) =
        use fixture = new Fixture()
        Assert.True(stepFailure step (greenOutput step fixture.Root) "" fixture.Root |> Option.isNone)
        Assert.True(stepFailure step "no verdict" "" fixture.Root |> Option.isSome)

    [<Theory>]
    [<InlineData("build", "0 Warning(s)", "1 Warning(s)")>]
    [<InlineData("build", "0 Error(s)", "1 Error(s)")>]
    [<InlineData("test", "failed: 0", "failed: 1")>]
    [<InlineData("test", "failed: 0", "failed: 01")>]
    [<InlineData("test", "skipped: 0", "skipped: 1")>]
    [<InlineData("test", "skipped: 0", "skipped: 01")>]
    [<InlineData("test", "total: 12", "total: 0")>]
    [<InlineData("test", "succeeded: 12", "succeeded: 11")>]
    member _.RejectsUnsuccessfulBuildAndTestOutput(step: string, before: string, after: string) =
        use fixture = new Fixture()
        let log = (greenOutput step fixture.Root).Replace(before, after)
        Assert.True(stepFailure step log "" fixture.Root |> Option.isSome)

    [<Fact>]
    member _.RejectsZeroTestsEvenWhenTotalEqualsSucceeded() =
        use fixture = new Fixture()
        let log = (greenOutput "test" fixture.Root).Replace("total: 12", "total: 0").Replace("succeeded: 12", "succeeded: 0")
        Assert.True(stepFailure "test" log "" fixture.Root |> Option.isSome)

    [<Theory>]
    [<InlineData(0)>]
    [<InlineData(1)>]
    member _.RequiresBothCoreAndAspNetTestAssemblyResults(index: int) =
        use fixture = new Fixture()
        let assembly = Path.GetFullPath(Path.Combine(fixture.Root, testAssemblyRelativePaths.[index]))
        let log =
            (greenOutput "test" fixture.Root).Split('\n')
            |> Array.filter (fun line -> not (line.Contains assembly))
            |> String.concat "\n"
        Assert.True(stepFailure "test" log "" fixture.Root |> Option.isSome)

    [<Fact>]
    member _.AcceptsAnsiAndStderrVerdicts() =
        use fixture = new Fixture()
        let colored = (greenOutput "test" fixture.Root).Replace("passed", "\u001b[32mpassed\u001b[m")
        Assert.True(stepFailure "test" colored "" fixture.Root |> Option.isNone)
        Assert.True(stepFailure "examples" "" CoreExampleMarker fixture.Root |> Option.isNone)

type PipelineTests() =
    [<Fact>]
    member _.GreenPipelineReportsActualChecksAndChildEnvironment() =
        use fixture = new Fixture()
        let code, stdout, stderr = fixture.Run [ "--json" ]
        Assert.Equal(0, code)
        Assert.Equal("", stderr)
        Assert.Equal<string list>(localSteps, fixture.Calls |> List.map (fun (step, _, _) -> step))
        for _, env, cwd in fixture.Calls do
            Assert.Equal("en", env.["DOTNET_CLI_UI_LANGUAGE"])
            Assert.Equal(fixture.Root, cwd)
        use document = JsonDocument.Parse stdout
        let report = document.RootElement
        Assert.Equal("passed", report.GetProperty("status").GetString())
        Assert.False(report.GetProperty("releaseEvidence").GetBoolean())
        Assert.Equal(JsonValueKind.Null, report.GetProperty("failedStep").ValueKind)
        Assert.Equal(localSteps.Length, report.GetProperty("steps").GetArrayLength())
        Assert.Equal<string list>(notRunSteps, report.GetProperty("notRun").EnumerateArray() |> Seq.map (fun value -> string (value.GetString())) |> List.ofSeq)

    [<Theory>]
    [<InlineData(false)>]
    [<InlineData(true)>]
    member _.FailureStopsPipelineAndRetainsDiagnostics(nonzeroExit: bool) =
        use fixture = new Fixture()
        let diagnostics = [ for index in 1..30 -> sprintf "diagnostic %d" index ] |> String.concat "\n"
        let overrideRun step =
            if step = "build" then
                Some { ExitCode = (if nonzeroExit then 3 else 0); Stdout = diagnostics; Stderr = "fatal diagnostic" }
            else None
        let code, stdout, stderr = fixture.Run([ "--json" ], fixture.Env, overrideRun, Some(fun _ -> success ""))
        Assert.Equal(1, code)
        Assert.Equal<string list>([ "restore"; "build" ], fixture.Calls |> List.map (fun (step, _, _) -> step))
        Assert.Contains("fatal diagnostic", stderr)
        Assert.DoesNotContain("diagnostic 10", stderr)
        use document = JsonDocument.Parse stdout
        let report = document.RootElement
        Assert.Equal("build", report.GetProperty("failedStep").GetString())
        let failed = report.GetProperty("steps").[1]
        Assert.Equal((if nonzeroExit then 3 else 0), failed.GetProperty("exitCode").GetInt32())
        Assert.Equal(20, failed.GetProperty("outputTail").GetArrayLength())

    [<Fact>]
    member _.ProcessStartFailureIsReportedInsteadOfCrashing() =
        use fixture = new Fixture()
        let code, stdout, stderr =
            fixture.Run([ "--json" ], fixture.Env, (fun _ -> raise (System.ComponentModel.Win32Exception("cannot start dotnet"))), Some(fun _ -> success ""))
        Assert.Equal(1, code)
        Assert.Contains("cannot start dotnet", stderr)
        use document = JsonDocument.Parse stdout
        Assert.Equal("restore", document.RootElement.GetProperty("failedStep").GetString())

    [<Fact>]
    member _.SkipFlagsSkipOnlyRequestedChecksAndAllowAbsentDocsVerifier() =
        use fixture = new Fixture()
        let code, stdout, _ = fixture.Run([ "--skip-docs"; "--skip-format" ], fixture.Env, (fun _ -> None), None)
        Assert.Equal(0, code)
        Assert.Equal<string list>(localSteps |> List.filter (fun step -> step <> "docs" && step <> "format"), fixture.Calls |> List.map (fun (step, _, _) -> step))
        Assert.Contains("SKIP docs", stdout)
        Assert.Contains("SKIP format", stdout)
        Assert.Contains("not release evidence", stdout)

    [<Theory>]
    [<InlineData("dotnet")>]
    [<InlineData("solution")>]
    [<InlineData("docs")>]
    member _.UnusableEnvironmentProducesStructuredFailureWithoutRunningChecks(missing: string) =
        use fixture = new Fixture()
        if missing = "solution" then File.Delete(Path.Combine(fixture.Root, "FunnySharp.slnx"))
        let env = if missing = "dotnet" then Map.empty else fixture.Env
        let docs = if missing = "docs" then None else Some(fun _ -> success "")
        let code, stdout, stderr = fixture.Run([ "--repository-root"; fixture.Root; "--json" ], env, (fun _ -> None), docs)
        Assert.Equal(2, code)
        Assert.Empty fixture.Calls
        Assert.Contains("Remediation", stderr)
        use document = JsonDocument.Parse stdout
        Assert.Equal("environment-failure", document.RootElement.GetProperty("status").GetString())
        Assert.Equal(0, document.RootElement.GetProperty("steps").GetArrayLength())

    [<Fact>]
    member _.ObsoleteOfflineFlagIsRejectedBeforeAnyCheck() =
        use fixture = new Fixture()
        let code, _, stderr = fixture.Run [ "--offline" ]
        Assert.Equal(2, code)
        Assert.Contains("not supported", stderr)
        Assert.Empty fixture.Calls

    [<Fact>]
    member _.HelpDoesNotRequireAnEnvironment() =
        use fixture = new Fixture()
        let code, stdout, _ = fixture.Run([ "--help" ], Map.empty, (fun _ -> None), None)
        Assert.Equal(0, code)
        Assert.Contains("comprehensive", stdout)
        Assert.Empty fixture.Calls

#if POSIX
type ProcessTests() =
    [<Fact>]
    member _.ChildRunnerCapturesBothStreamsAndForwardsExplicitEnvironment() =
        use temp = new TempDirectory()
        let executable = Path.Combine(temp.Path, "probe")
        File.WriteAllText(executable, "#!/bin/sh\nprintf '%s' \"$PROBE\"\nprintf 'stderr' >&2\nexit 7\n")
        File.SetUnixFileMode(executable, UnixFileMode.UserRead ||| UnixFileMode.UserWrite ||| UnixFileMode.UserExecute)
        let result = runChildProcess "probe" [] (Map.ofList [ "PATH", temp.Path; "PROBE", "forwarded" ]) temp.Path
        Assert.Equal(7, result.ExitCode)
        Assert.Equal("forwarded", result.Stdout)
        Assert.Equal("stderr", result.Stderr)
#endif
