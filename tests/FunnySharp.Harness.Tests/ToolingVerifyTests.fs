module FunnySharp.Harness.Tests.ToolingVerifyTests

// Ported assertions of eng/tools/tests/test_verify_local.py (855 lines) for the F#
// ToolingVerify module. Two fakes stand in for the real dotnet pipeline:
//
//  * FakeRunner is injected through mainWith and returns canned fixture logs so
//    every verdict arm is exercised deterministically;
//  * on POSIX, FakeDotnetExecutableTests puts a small executable `dotnet` shim on
//    PATH and runs the real runner, proving command dispatch, output capture,
//    exit-code propagation and environment forwarding without a real SDK.
//
// Deliberate divergences from the Python suite, both recorded in the module header:
// the uv and pinned-Python prerequisites are gone (the F# tool does not run under
// uv/Python), so UvIsNoLongerAPrerequisite asserts that the CLI still succeeds with
// a uv shim on PATH instead of the old environment failures; and the PEP 723 header
// test is replaced by MarkerGuardFailureOnMutatedVerifier, which mutates a verifier
// source and asserts the missing-marker report.

open System
open System.Collections.Generic
open System.IO
open System.Text
open System.Text.Json
open Xunit
open FunnySharp.Harness.Proc
open FunnySharp.Harness.ToolingVerify
open FunnySharp.Harness.Tests.Support

let private utf8NoBom = UTF8Encoding(false)

/// A non-null view of JsonElement.GetString() (absent values read as "").
let private text (value: string | null) : string =
    match value with
    | null -> ""
    | some -> some

// ---------------------------------------------------------------------------
// Fixtures
// ---------------------------------------------------------------------------

let private greenStdout (step: string) (repositoryRoot: string) : string =
    match step with
    | "restore" ->
        "  Determining projects to restore...\n"
        + "  Restored FunnySharp.slnx (in 1.23 sec).\n"
    | "build" ->
        "  FunnySharp -> src/FunnySharp/bin/Release/net10.0/FunnySharp.dll\n"
        + "Build succeeded.\n"
        + "    0 Warning(s)\n"
        + "    0 Error(s)\n"
        + "\n"
        + "Time Elapsed 00:00:04.00\n"
    | "test" ->
        let assemblyLines =
            testAssemblyRelativePaths
            |> List.map (fun relative ->
                sprintf "  %s (net10.0|net10.0) passed (1.2s)" (Path.Combine(repositoryRoot, relative)))

        String.concat "\n" assemblyLines
        + "\n"
        + "\n"
        + "Test run summary: Passed!\n"
        + "  total: 441\n"
        + "  failed: 0\n"
        + "  succeeded: 441\n"
        + "  skipped: 0\n"
        + "  duration: 00:00:12.3456789\n"
    | "examples" -> "FunnySharp examples passed.\n"
    | "aspnetcore-examples" -> "FunnySharp ASP.NET Core example endpoints mapped.\n"
    | "format" -> ""
    | "docs" -> "Verified 3 C# documentation snippets across 2 primary guides.\n"
    | _ -> failwithf "unexpected step %s" step

let private withoutAssemblyLine (log: string) (repositoryRoot: string) (index: int) : string =
    let assembly = Path.Combine(repositoryRoot, testAssemblyRelativePaths.[index])

    log.Split('\n')
    |> Array.filter (fun line -> not (line.Contains assembly))
    |> String.concat "\n"
    |> fun text -> text + "\n"

let private classifyCommand (command: StepCommand) : string =
    match command with
    | DocumentationSnippets _ -> "docs"
    | ChildProcess(_, args) ->
        match args with
        | ("restore" | "build" | "test" | "format") as verb :: _ -> verb
        | "run" :: rest when rest |> List.exists (fun arg -> arg.Contains "AspNetCore") -> "aspnetcore-examples"
        | "run" :: _ -> "examples"
        | other -> failwithf "unexpected command: %A" other

let private commandArgv (command: StepCommand) : string list =
    match command with
    | ChildProcess(exe, args) -> exe :: args
    | DocumentationSnippets _ -> []

type private FakeRun =
    { ReturnCode: int
      Stdout: string
      Stderr: string }

/// Injected command runner returning canned logs and recording calls.
type FakeRunner(repositoryRoot: string) =
    let calls = ResizeArray<StepCommand * Map<string, string> * string>()
    let overrides = Dictionary<string, FakeRun>()
    let mutable recordPath: string option = None

    member _.RepositoryRoot = repositoryRoot
    member _.Calls = List.ofSeq calls

    member _.Steps =
        calls |> Seq.map (fun (command, _, _) -> classifyCommand command) |> List.ofSeq

    member _.SetRecordPath(path: string) = recordPath <- Some path

    member _.Override(step: string, exitCode: int, stdout: string option, stderr: string) =
        overrides.[step] <-
            { ReturnCode = exitCode
              Stdout = defaultArg stdout (greenStdout step repositoryRoot)
              Stderr = stderr }

    member this.Override(step: string, ?exitCode: int, ?stdout: string, ?stderr: string) =
        this.Override(step, defaultArg exitCode 0, stdout, defaultArg stderr "")

    member _.Runner: CommandRunner =
        fun command env cwd ->
            let step = classifyCommand command
            calls.Add(command, env, cwd)

            match recordPath with
            | Some path ->
                let payload = Dictionary<string, obj>()
                payload.["step"] <- step
                payload.["argv"] <- List<string>(commandArgv command)
                let recordedEnv = Dictionary<string, string>()

                for KeyValue(key, value) in env do
                    recordedEnv.[key] <- value

                payload.["env"] <- recordedEnv
                File.AppendAllText(path, JsonSerializer.Serialize payload + "\n", utf8NoBom)
            | None -> ()

            match overrides.TryGetValue step with
            | true, run -> { ExitCode = run.ReturnCode; Stdout = run.Stdout; Stderr = run.Stderr }
            | _ -> { ExitCode = 0; Stdout = greenStdout step repositoryRoot; Stderr = "" }

let private installStub (binDirectory: string) (name: string) (content: string) : unit =
    Directory.CreateDirectory binDirectory |> ignore
    let path = Path.Combine(binDirectory, name)
    File.WriteAllText(path, content, utf8NoBom)

    if not (OperatingSystem.IsWindows()) then
        File.SetUnixFileMode(
            path,
            UnixFileMode.UserRead
            ||| UnixFileMode.UserWrite
            ||| UnixFileMode.UserExecute
            ||| UnixFileMode.GroupRead
            ||| UnixFileMode.GroupExecute
            ||| UnixFileMode.OtherRead
            ||| UnixFileMode.OtherExecute
        )

let private fakeDotnetScript =
    "#!/bin/sh\n"
    + "record=\"${FAKE_DOTNET_RECORD:-}\"\n"
    + "if [ -n \"$record\" ]; then\n"
    + "    {\n"
    + "        printf 'argv:'\n"
    + "        for argument in \"$@\"; do\n"
    + "            printf ' %s' \"$argument\"\n"
    + "        done\n"
    + "        echo\n"
    + "        echo \"UV_OFFLINE=${UV_OFFLINE:-<unset>}\"\n"
    + "    } >> \"$record\"\n"
    + "fi\n"
    + "step=restore\n"
    + "case \"$1\" in\n"
    + "    restore|build|test|format) step=\"$1\" ;;\n"
    + "    run)\n"
    + "        case \"$*\" in\n"
    + "            *AspNetCore*) step=aspnetcore-examples ;;\n"
    + "            *) step=examples ;;\n"
    + "        esac\n"
    + "        ;;\n"
    + "esac\n"
    + "if [ -f \"$FAKE_DOTNET_BEHAVIOR/$step.stdout\" ]; then\n"
    + "    cat \"$FAKE_DOTNET_BEHAVIOR/$step.stdout\"\n"
    + "fi\n"
    + "if [ -f \"$FAKE_DOTNET_BEHAVIOR/$step.stderr\" ]; then\n"
    + "    cat \"$FAKE_DOTNET_BEHAVIOR/$step.stderr\" >&2\n"
    + "fi\n"
    + "if [ -f \"$FAKE_DOTNET_BEHAVIOR/$step.code\" ]; then\n"
    + "    exit \"$(cat \"$FAKE_DOTNET_BEHAVIOR/$step.code\")\"\n"
    + "fi\n"
    + "exit 0\n"

// ---------------------------------------------------------------------------
// Recorded-call parsing (FakeRunner's --offline record)
// ---------------------------------------------------------------------------

type private RecordedCall =
    { Step: string
      Env: Map<string, string> }

let private readRecord (path: string) : RecordedCall list =
    File.ReadAllLines path
    |> Array.filter (fun line -> line.Trim() <> "")
    |> Array.map (fun line ->
        let document = JsonDocument.Parse line
        let root = document.RootElement

        let env =
            root.GetProperty("env").EnumerateObject()
            |> Seq.map (fun property -> property.Name, string (property.Value.GetString() |> text))
            |> Map.ofSeq

        { Step = root.GetProperty("step").GetString() |> text
          Env = env })
    |> List.ofArray

// ---------------------------------------------------------------------------
// Shared fixture base
// ---------------------------------------------------------------------------

let private environmentMap () : Map<string, string> =
    Environment.GetEnvironmentVariables()
    |> Seq.cast<System.Collections.DictionaryEntry>
    |> Seq.map (fun entry -> string entry.Key, string entry.Value)
    |> Map.ofSeq

/// The repository root, honouring the probe override used by the /tmp test project.
let private repoRoot () : string =
    match Environment.GetEnvironmentVariable "FUNNYSHARP_HARNESS_REPO_ROOT" with
    | null | "" -> repositoryRoot ()
    | value -> value

let private cannedDocsVerifier: (string -> ProcessResult) =
    fun _ ->
        { ExitCode = 0
          Stdout = "Verified 3 C# documentation snippets across 2 primary guides.\n"
          Stderr = "" }

/// The real child runner, adapting the command union (and the in-process docs
/// verifier) to the CommandRunner shape.
let private realRunner (docs: (string -> ProcessResult) option) : CommandRunner =
    fun command env cwd ->
        match command with
        | ChildProcess(exe, args) -> runChildProcess exe args env cwd
        | DocumentationSnippets root ->
            match docs with
            | Some verify -> verify root
            | None ->
                { ExitCode = 2
                  Stdout = ""
                  Stderr = "the documentation-snippet verifier is not wired into this build." }

type VerifyLocalTestBase() =
    let temp = new TempDirectory()
    let repo = repoRoot ()
    let binDirectory = Path.Combine(temp.Path, "bin")
    do installStub binDirectory "dotnet" "#!/bin/sh\nexit 0\n"
    let runner = FakeRunner repo

    let env =
        let baseEnv = environmentMap ()
        let realPath = baseEnv |> Map.tryFind "PATH" |> Option.defaultValue ""

        baseEnv
        |> Map.add
            "PATH"
            (if realPath = "" then
                 binDirectory
             else
                 binDirectory + string Path.PathSeparator + realPath)

    member _.Temp = temp.Path
    member _.Repo = repo
    member _.BinDirectory = binDirectory
    member _.Runner = runner
    member _.Env = env

    member _.WithPath(path: string) = env |> Map.add "PATH" path
    member _.RepoArgv(flags: string list) = ([ "--repository-root"; repo ] @ flags)

    member this.RunCli(argv: string list) : int * string * string =
        this.RunCli(argv, env, runner.Runner, Some cannedDocsVerifier)

    member this.RunCli(argv: string list, effectiveEnv: Map<string, string>) : int * string * string =
        this.RunCli(argv, effectiveEnv, runner.Runner, Some cannedDocsVerifier)

    member _.RunCli
        (argv: string list, effectiveEnv: Map<string, string>, effectiveRunner: CommandRunner, docs: (string -> ProcessResult) option)
        : int * string * string =
        use stdout = new StringWriter()
        use stderr = new StringWriter()

        let code =
            mainWith
                stdout
                stderr
                temp.Path
                argv
                { Env = effectiveEnv
                  Runner = effectiveRunner
                  DocsVerifier = docs }

        code, stdout.ToString(), stderr.ToString()

    /// Assert a step fails with the expected message; return the report message.
    member this.AssertStepFails
        (step: string, messageContains: string, ?stdoutOverride: string, ?exitCode: int, ?expectedSteps: string list)
        : string =
        runner.Override(step, ?exitCode = exitCode, ?stdout = stdoutOverride)
        let code, stdout, _ = this.RunCli(this.RepoArgv [ "--json" ])
        Assert.Equal(1, code)
        let document = JsonDocument.Parse stdout
        let report = document.RootElement
        Assert.Equal("failed", report.GetProperty("status").GetString() |> text)
        Assert.Equal(step, report.GetProperty("failedStep").GetString() |> text)
        let message = report.GetProperty("message").GetString() |> text
        Assert.Contains(messageContains, message)
        let steps = report.GetProperty("steps").EnumerateArray() |> Array.ofSeq
        Assert.Equal(step, steps.[steps.Length - 1].GetProperty("name").GetString() |> text)
        Assert.Equal("failed", steps.[steps.Length - 1].GetProperty("status").GetString() |> text)

        match expectedSteps with
        | Some expected -> Assert.Equal<string list>(expected, runner.Steps)
        | None -> ()

        message

    interface IDisposable with
        member _.Dispose() = (temp :> IDisposable).Dispose()

// ---------------------------------------------------------------------------
// Pipeline verdicts
// ---------------------------------------------------------------------------

type PipelineVerdictTests() =
    inherit VerifyLocalTestBase()

    [<Fact>]
    member this.AllGreenPipelinePasses() =
        let code, stdout, stderr = this.RunCli(this.RepoArgv [])
        Assert.Equal(0, code)

        for step in localSteps do
            Assert.Contains("PASS " + step, stdout)

        Assert.Equal<string list>(localSteps, this.Runner.Steps)
        Assert.Contains("local pre-check", stdout.ToLowerInvariant())
        Assert.Contains("not release evidence", stdout)
        Assert.Equal("", stderr)

    [<Fact>]
    member this.AnsiWrappedTestOutputPasses() =
        let escape = "\u001b"
        let colored =
            (greenStdout "test" this.Repo)
                .Replace(
                    "passed (1.2s)",
                    escape + "[32mpassed" + escape + "[m " + escape + "[90m(1.2s)" + escape + "[m"
                )

        this.Runner.Override("test", stdout = colored)
        let code, stdout, _ = this.RunCli(this.RepoArgv [])
        Assert.Equal(0, code)
        Assert.Contains("PASS test", stdout)

    [<Fact>]
    member this.BuildWarningFails() =
        let log = (greenStdout "build" this.Repo).Replace("    0 Warning(s)", "    1 Warning(s)")

        this.AssertStepFails("build", "0 Warning", stdoutOverride = log, expectedSteps = [ "restore"; "build" ])
        |> ignore

    [<Fact>]
    member this.BuildErrorLineFails() =
        let log = (greenStdout "build" this.Repo).Replace("    0 Error(s)", "    2 Error(s)")
        this.AssertStepFails("build", "0 Error", stdoutOverride = log) |> ignore

    [<Fact>]
    member this.BuildMissingSuccessLineFails() =
        let log = (greenStdout "build" this.Repo).Replace("Build succeeded.", "Build FAILED.")
        this.AssertStepFails("build", "Build succeeded", stdoutOverride = log) |> ignore

    [<Fact>]
    member this.RestoreFailureStopsPipeline() =
        this.AssertStepFails("restore", "exit code 1", exitCode = 1, expectedSteps = [ "restore" ])
        |> ignore

    [<Fact>]
    member this.TestSummaryZeroTotalFails() =
        let log =
            (greenStdout "test" this.Repo)
                .Replace("  total: 441", "  total: 0")
                .Replace("  succeeded: 441", "  succeeded: 0")

        this.AssertStepFails("test", "positive total", stdoutOverride = log) |> ignore

    [<Fact>]
    member this.TestSummaryTotalNotEqualSucceededFails() =
        let log = (greenStdout "test" this.Repo).Replace("  succeeded: 441", "  succeeded: 440")
        this.AssertStepFails("test", "positive total equal to succeeded", stdoutOverride = log) |> ignore

    [<Fact>]
    member this.TestSummarySkippedFails() =
        let log = (greenStdout "test" this.Repo).Replace("  skipped: 0", "  skipped: 1")
        this.AssertStepFails("test", "Test run summary: Passed!", stdoutOverride = log) |> ignore

    [<Fact>]
    member this.TestSummaryMissingFails() =
        this.AssertStepFails("test", "Test run summary: Passed!", stdoutOverride = "no summary here\n")
        |> ignore

    [<Fact>]
    member this.TestMissingCoreAssemblyLineFails() =
        let log = withoutAssemblyLine (greenStdout "test" this.Repo) this.Repo 0
        let message = this.AssertStepFails("test", "passed result line for", stdoutOverride = log)
        Assert.Contains("FunnySharp.Tests.dll", message)

    [<Fact>]
    member this.TestMissingAspNetAssemblyLineFails() =
        let log = withoutAssemblyLine (greenStdout "test" this.Repo) this.Repo 1
        let message = this.AssertStepFails("test", "passed result line for", stdoutOverride = log)
        Assert.Contains("FunnySharp.AspNetCore.Tests.dll", message)

    [<Fact>]
    member this.ExamplesMissingSuccessLineFails() =
        this.AssertStepFails("examples", "FunnySharp examples passed.", stdoutOverride = "nothing useful\n")
        |> ignore

    [<Fact>]
    member this.AspNetCoreExamplesMissingSuccessLineFails() =
        this.AssertStepFails(
            "aspnetcore-examples",
            "FunnySharp ASP.NET Core example endpoints mapped.",
            stdoutOverride = "nothing useful\n"
        )
        |> ignore

    [<Fact>]
    member this.FormatterFailureFails() =
        this.AssertStepFails(
            "format",
            "exit code 2",
            exitCode = 2,
            expectedSteps = [ "restore"; "build"; "test"; "examples"; "aspnetcore-examples"; "format" ]
        )
        |> ignore

    [<Fact>]
    member this.DocsFailureFails() =
        this.AssertStepFails("docs", "exit code 1", exitCode = 1) |> ignore

    [<Fact>]
    member this.DocsMissingSuccessLineFails() =
        this.AssertStepFails("docs", "Verified N C# documentation snippets", stdoutOverride = "no verdict line here\n")
        |> ignore

    [<Fact>]
    member this.DocsSuccessLinePasses() =
        this.Runner.Override("docs", stdout = "Verified 3 C# documentation snippets across 2 primary guides.\n")
        let code, stdout, _ = this.RunCli(this.RepoArgv [])
        Assert.Equal(0, code)
        Assert.Contains("PASS docs", stdout)

    [<Fact>]
    member this.FailedStepPrintsBoundedOutputTail() =
        let lines = [ for index in 1..30 -> sprintf "diagnostic %d" index ]
        this.Runner.Override("format", exitCode = 2, stdout = String.concat "\n" lines, stderr = "fatal: formatting failed")
        let code, stdout, stderr = this.RunCli(this.RepoArgv [ "--json" ])
        Assert.Equal(1, code)
        Assert.Contains("--- format output (last 20 lines) ---", stderr)
        Assert.Contains("diagnostic 30", stderr)
        Assert.Contains("fatal: formatting failed", stderr)
        Assert.DoesNotContain("diagnostic 10", stderr)
        let document = JsonDocument.Parse stdout
        let steps = document.RootElement.GetProperty("steps").EnumerateArray() |> Array.ofSeq
        let failed = steps.[steps.Length - 1]
        Assert.Equal("format", failed.GetProperty("name").GetString() |> text)
        Assert.Equal("failed", failed.GetProperty("status").GetString() |> text)

        let tail =
            failed.GetProperty("outputTail").EnumerateArray()
            |> Seq.map (fun entry -> entry.GetString() |> text)
            |> List.ofSeq

        Assert.Equal<string list>(lines.[11..] @ [ "fatal: formatting failed" ], tail)

        // Passed steps keep the original four-key report shape.
        let passedKeys =
            steps.[0].EnumerateObject() |> Seq.map (fun property -> property.Name) |> Set.ofSeq

        Assert.Equal<Set<string>>(Set.ofList [ "name"; "status"; "exitCode"; "message" ], passedKeys)

    [<Fact>]
    member this.VerdictFailurePrintsOutputTail() =
        this.Runner.Override("docs", stdout = "snippet check failed: missing region\n")
        let code, stdout, stderr = this.RunCli(this.RepoArgv [ "--json" ])
        Assert.Equal(1, code)
        Assert.Contains("--- docs output (last 20 lines) ---", stderr)
        Assert.Contains("snippet check failed: missing region", stderr)
        let document = JsonDocument.Parse stdout
        let steps = document.RootElement.GetProperty("steps").EnumerateArray() |> Array.ofSeq
        let failed = steps.[steps.Length - 1]
        Assert.Equal("docs", failed.GetProperty("name").GetString() |> text)
        Assert.Equal("failed", failed.GetProperty("status").GetString() |> text)

        let tail =
            failed.GetProperty("outputTail").EnumerateArray()
            |> Seq.map (fun entry -> entry.GetString() |> text)
            |> List.ofSeq

        Assert.Equal<string list>([ "snippet check failed: missing region" ], tail)

    [<Fact>]
    member this.StderrOutputIsUsedForVerdicts() =
        this.Runner.Override("examples", stdout = "", stderr = "FunnySharp examples passed.\n")
        let code, _, _ = this.RunCli(this.RepoArgv [])
        Assert.Equal(0, code)

// ---------------------------------------------------------------------------
// Protocol contract and marker guard
// ---------------------------------------------------------------------------

type ProtocolContractTests() =
    inherit VerifyLocalTestBase()

    member private this.Protocol() =
        JsonDocument.Parse(File.ReadAllText(Path.Combine(this.Repo, "eng", "release-protocol.json")))

    member private _.CommandArgvOf(command: StepCommand) =
        match command with
        | ChildProcess(exe, args) -> exe :: args
        | _ -> failwith "expected a child-process command"

    [<Fact>]
    member this.CommandsMirrorReleaseProtocolForms() =
        use protocol = this.Protocol()
        let steps = protocol.RootElement.GetProperty("steps")

        for name in [ "build"; "test"; "examples"; "aspnetcore-examples"; "format" ] do
            let expected =
                "dotnet"
                :: (steps.GetProperty(name).GetProperty("arguments").EnumerateArray()
                    |> Seq.map (fun entry -> entry.GetString() |> text)
                    |> List.ofSeq)

            Assert.Equal<string list>(expected, this.CommandArgvOf(commandForStep name this.Repo))

        let restore = this.CommandArgvOf(commandForStep "restore" this.Repo)
        Assert.Equal<string list>([ "dotnet"; "restore"; "FunnySharp.slnx"; "--locked-mode" ], restore)
        Assert.DoesNotContain("--no-cache", restore)
        Assert.DoesNotContain("--source", restore)

    [<Fact>]
    member this.DocsStepTargetsInProcessVerifier() =
        Assert.Equal(DocumentationSnippets this.Repo, commandForStep "docs" this.Repo)

    [<Fact>]
    member this.NotRunStepsMatchReleaseProtocol() =
        use protocol = this.Protocol()

        let full =
            protocol.RootElement.GetProperty("modes").GetProperty("full").GetProperty("steps").EnumerateArray()
            |> Seq.map (fun entry -> entry.GetString() |> text)
            |> List.ofSeq

        let expected = full |> List.filter (fun step -> not (List.contains step localSteps))
        Assert.Equal<string list>(notRunSteps, expected)

    [<Fact>]
    member this.LocalStepsAreProtocolOrderedSubsequence() =
        use protocol = this.Protocol()

        let full =
            protocol.RootElement.GetProperty("modes").GetProperty("full").GetProperty("steps").EnumerateArray()
            |> Seq.map (fun entry -> entry.GetString() |> text)
            |> List.ofSeq

        let protocolLocal = full |> List.filter (fun step -> not (List.contains step notRunSteps))
        let localFromProtocol = localSteps |> List.filter (fun step -> List.contains step full)
        Assert.Equal<string list>(protocolLocal, localFromProtocol)

        // The docs verifier is the one local-only step; it runs last.
        Assert.Equal<string list>([ "docs" ], localSteps |> List.filter (fun step -> not (List.contains step full)))

    [<Fact>]
    member this.NotRunSummaryListsEveryOutOfScopeStep() =
        let code, stdout, _ = this.RunCli(this.RepoArgv [])
        Assert.Equal(0, code)

        let summaryLine =
            stdout.Split('\n') |> Array.find (fun line -> line.StartsWith "NOT RUN")

        let listed = summaryLine.Substring(summaryLine.IndexOf(": ") + 2).Split(", ") |> List.ofArray
        Assert.Equal<string list>(notRunSteps, listed)

    [<Fact>]
    member this.MarkerContractPresentInRealVerifier() =
        let paths =
            VerifierRelativePaths
            |> List.map (fun relative -> Path.Combine(this.Repo, relative))

        Assert.Equal<string list>([], missingMarkerFragments paths)

    [<Fact>]
    member this.MarkerGuardFailureOnMutatedVerifier() =
        let fixture = Path.Combine(this.Temp, "mutated-repo")
        Directory.CreateDirectory(Path.Combine(fixture, "eng", "harness")) |> ignore

        let source =
            File.ReadAllText(Path.Combine(this.Repo, "eng", "harness", "ReleaseVerifySource.fs"))

        let mutated = source.Replace(@"0 Warning\(s\)", "0 Warnings")
        Assert.NotEqual<string>(source, mutated)

        File.WriteAllText(
            Path.Combine(fixture, "eng", "harness", "ReleaseVerifySource.fs"),
            mutated,
            utf8NoBom
        )

        let code, stdout, _ = this.RunCli([ "--repository-root"; fixture; "--json" ])
        Assert.Equal(1, code)
        Assert.Empty this.Runner.Calls
        let document = JsonDocument.Parse stdout
        let report = document.RootElement
        Assert.Equal("failed", report.GetProperty("status").GetString() |> text)
        Assert.Contains("build zero-warning line", report.GetProperty("message").GetString() |> text)

// ---------------------------------------------------------------------------
// Environment failures
// ---------------------------------------------------------------------------

type EnvironmentFailureTests() =
    inherit VerifyLocalTestBase()

    [<Fact>]
    member this.MissingDotnetIsEnvironmentFailure() =
        let uvOnly = Path.Combine(this.Temp, "uv-only-bin")
        installStub uvOnly "uv" "#!/bin/sh\nexit 0\n"
        let code, _, stderr = this.RunCli(this.RepoArgv [], this.WithPath uvOnly)
        Assert.Equal(2, code)
        Assert.Contains("dotnet was not found", stderr)
        Assert.Contains("Remediation", stderr)
        Assert.Empty this.Runner.Calls

    [<Fact>]
    member this.UvIsNoLongerAPrerequisite() =
        let code, _, _ = this.RunCli(this.RepoArgv [])
        Assert.Equal(0, code)

    [<Fact>]
    member this.OutsideGitRepositoryIsEnvironmentFailure() =
        let notARepo = Path.Combine(this.Temp, "not-a-repo")
        Directory.CreateDirectory notARepo |> ignore

        use stdout = new StringWriter()
        use stderr = new StringWriter()

        let code =
            mainWith
                stdout
                stderr
                notARepo
                []
                { Env = this.Env
                  Runner = this.Runner.Runner
                  DocsVerifier = Some cannedDocsVerifier }

        Assert.Equal(2, code)
        Assert.Contains("not inside a git repository", stderr.ToString())
        Assert.Contains("--repository-root", stderr.ToString())
        Assert.Empty this.Runner.Calls

    [<Fact>]
    member this.MissingVerifierIsEnvironmentFailure() =
        let fixture = Path.Combine(this.Temp, "repo-without-verifier")
        Directory.CreateDirectory fixture |> ignore
        let code, _, stderr = this.RunCli([ "--repository-root"; fixture ])
        Assert.Equal(2, code)
        Assert.Contains("the ported release verifier was not found", stderr)
        Assert.Contains("ReleaseVerifySource.fs", stderr)
        Assert.Empty this.Runner.Calls

    [<Fact>]
    member this.MissingDocsVerifierIsEnvironmentFailure() =
        let code, _, stderr =
            this.RunCli(this.RepoArgv [], this.Env, this.Runner.Runner, None)

        Assert.Equal(2, code)
        Assert.Contains("documentation-snippet verifier", stderr)
        Assert.Contains("--skip-docs", stderr)
        Assert.Empty this.Runner.Calls

// ---------------------------------------------------------------------------
// Reporting, skips, and offline mode
// ---------------------------------------------------------------------------

type ReportingTests() =
    inherit VerifyLocalTestBase()

    [<Fact>]
    member this.JsonSummaryShape() =
        let code, stdout, _ = this.RunCli(this.RepoArgv [ "--json" ])
        Assert.Equal(0, code)
        let document = JsonDocument.Parse stdout
        let report = document.RootElement

        let keys = report.EnumerateObject() |> Seq.map (fun property -> property.Name) |> Set.ofSeq

        Assert.Equal<Set<string>>(
            Set.ofList
                [ "tool"
                  "status"
                  "exitCode"
                  "releaseEvidence"
                  "preCheck"
                  "repositoryRoot"
                  "offline"
                  "skipped"
                  "steps"
                  "failedStep"
                  "notRun"
                  "message" ],
            keys
        )

        Assert.Equal("verify_local.py", report.GetProperty("tool").GetString() |> text)
        Assert.Equal("passed", report.GetProperty("status").GetString() |> text)
        Assert.Equal(0, report.GetProperty("exitCode").GetInt32())
        Assert.False(report.GetProperty("releaseEvidence").GetBoolean())
        Assert.True(report.GetProperty("preCheck").GetBoolean())
        Assert.Equal(this.Repo, report.GetProperty("repositoryRoot").GetString() |> text)
        Assert.False(report.GetProperty("offline").GetBoolean())
        Assert.Equal(0, report.GetProperty("skipped").GetArrayLength())
        Assert.Equal(JsonValueKind.Null, report.GetProperty("failedStep").ValueKind)

        let notRun =
            report.GetProperty("notRun").EnumerateArray() |> Seq.map (fun entry -> entry.GetString() |> text) |> List.ofSeq

        Assert.Equal<string list>(notRunSteps, notRun)

        let steps = report.GetProperty("steps").EnumerateArray() |> Array.ofSeq

        Assert.Equal<string list>(
            localSteps,
            steps |> Array.map (fun step -> step.GetProperty("name").GetString() |> text) |> List.ofArray
        )

        for step in steps do
            let stepKeys = step.EnumerateObject() |> Seq.map (fun property -> property.Name) |> Set.ofSeq

            Assert.Equal<Set<string>>(Set.ofList [ "name"; "status"; "exitCode"; "message" ], stepKeys)
            Assert.Equal("passed", step.GetProperty("status").GetString() |> text)

    [<Fact>]
    member this.SkipFlagsMarkStepsSkipped() =
        let code, stdout, _ = this.RunCli(this.RepoArgv [ "--skip-docs"; "--skip-format"; "--json" ])
        Assert.Equal(0, code)
        let document = JsonDocument.Parse stdout
        let report = document.RootElement

        let skipped =
            report.GetProperty("skipped").EnumerateArray() |> Seq.map (fun entry -> entry.GetString() |> text) |> List.ofSeq

        Assert.Equal<string list>([ "format"; "docs" ], skipped)

        let statuses =
            report.GetProperty("steps").EnumerateArray()
            |> Seq.map (fun step -> step.GetProperty("name").GetString() |> text, step.GetProperty("status").GetString() |> text)
            |> Map.ofSeq

        Assert.Equal("skipped", statuses.["docs"])
        Assert.Equal("skipped", statuses.["format"])

        Assert.Equal<string list>(
            [ "restore"; "build"; "test"; "examples"; "aspnetcore-examples" ],
            this.Runner.Steps
        )

        let _, human, _ = this.RunCli(this.RepoArgv [ "--skip-docs"; "--skip-format" ])
        Assert.Contains("SKIP docs", human)
        Assert.Contains("SKIP format", human)

    [<Fact>]
    member this.OfflineExportsUvOfflineToChildren() =
        let record = Path.Combine(this.Temp, "child-record.jsonl")
        this.Runner.SetRecordPath record
        let code, _, _ = this.RunCli(this.RepoArgv [ "--offline"; "--json" ])
        Assert.Equal(0, code)
        let entries = readRecord record
        Assert.Equal<string list>(localSteps, entries |> List.map (fun entry -> entry.Step))

        for entry in entries do
            Assert.Equal("1", entry.Env |> Map.find "UV_OFFLINE")

    [<Fact>]
    member this.OfflineFlagAbsentByDefault() =
        let record = Path.Combine(this.Temp, "child-record.jsonl")
        this.Runner.SetRecordPath record
        let code, _, _ = this.RunCli(this.RepoArgv [ "--json" ])
        Assert.Equal(0, code)
        let entries = readRecord record
        Assert.NotEmpty entries

        for entry in entries do
            Assert.False(entry.Env |> Map.containsKey "UV_OFFLINE")

// ---------------------------------------------------------------------------
// Real runner with a fake dotnet executable on PATH (POSIX only, compiled there)
// ---------------------------------------------------------------------------

#if POSIX
type FakeDotnetExecutableTests() =
    inherit VerifyLocalTestBase()

    let mutable behaviorDirectory = ""

    member private this.EnsureBehavior() =
        if behaviorDirectory = "" then
            let directory = Path.Combine(this.Temp, "behavior")
            Directory.CreateDirectory directory |> ignore

            for step in
                [ "restore"; "build"; "test"; "examples"; "aspnetcore-examples"; "format" ] do
                File.WriteAllText(Path.Combine(directory, step + ".stdout"), greenStdout step this.Repo, utf8NoBom)
                File.WriteAllText(Path.Combine(directory, step + ".code"), "0\n", utf8NoBom)

            behaviorDirectory <- directory

        behaviorDirectory

    member private this.RealEnv(record: string) =
        this.Env
        |> Map.add "FAKE_DOTNET_BEHAVIOR" (this.EnsureBehavior())
        |> Map.add "FAKE_DOTNET_RECORD" record

    member private this.InstallFakeDotnet() =
        installStub this.BinDirectory "dotnet" fakeDotnetScript

    [<Fact>]
    member this.RealRunnerUsesFakeDotnetAndForwardsOffline() =
        this.InstallFakeDotnet()
        let record = Path.Combine(this.Temp, "child-record.txt")
        let env = this.RealEnv record
        let code, stdout, _ =
            this.RunCli(this.RepoArgv [ "--offline"; "--json" ], env, realRunner (Some cannedDocsVerifier), Some cannedDocsVerifier)

        Assert.Equal(0, code)
        let document = JsonDocument.Parse stdout

        let statuses =
            document.RootElement.GetProperty("steps").EnumerateArray()
            |> Seq.map (fun step -> step.GetProperty("status").GetString() |> text)
            |> List.ofSeq

        Assert.Equal<string list>(List.replicate localSteps.Length "passed", statuses)
        let recorded = File.ReadAllText record
        Assert.Contains("argv: restore FunnySharp.slnx --locked-mode", recorded)
        Assert.Contains("argv: build FunnySharp.slnx --configuration Release --no-restore", recorded)
        Assert.Contains("argv: test FunnySharp.slnx --configuration Release --no-build --no-restore", recorded)
        Assert.Contains("UV_OFFLINE=1", recorded)

    [<Fact>]
    member this.RealRunnerPropagatesChildFailure() =
        this.InstallFakeDotnet()
        this.EnsureBehavior() |> ignore
        File.WriteAllText(Path.Combine(behaviorDirectory, "format.code"), "1\n", utf8NoBom)
        let record = Path.Combine(this.Temp, "child-record.txt")
        let env = this.RealEnv record
        let code, stdout, _ =
            this.RunCli(this.RepoArgv [ "--json" ], env, realRunner (Some cannedDocsVerifier), Some cannedDocsVerifier)

        Assert.Equal(1, code)
        let document = JsonDocument.Parse stdout
        let report = document.RootElement
        Assert.Equal("format", report.GetProperty("failedStep").GetString() |> text)
        Assert.Contains("exit code 1", report.GetProperty("message").GetString() |> text)
#endif
