module FunnySharp.Harness.ToolingVerify

// Comprehensive local checks. Release-only steps remain outside this pre-check.

open System
open System.IO
open System.Text.Json
open System.Text.RegularExpressions
open FunnySharp.Harness.Proc
open FunnySharp.Harness.Repo
open FunnySharp.Harness.Output

[<Literal>]
let ToolName = "verify-tooling"

/// The release-protocol steps this pre-check runs locally, in protocol order.
let localSteps: string list =
    [ "restore"; "build"; "test"; "examples"; "aspnetcore-examples"; "format"; "docs" ]

/// Every eng/release-protocol.json (full mode) step outside local scope, in order.
let notRunSteps: string list =
    [ "clean"
      "pack"
      "api-baseline"
      "benchmark-preflight"
      "benchmark"
      "performance-verify"
      "compatibility" ]

let testAssemblyRelativePaths: string list =
    [ "tests/FunnySharp.Tests/bin/Release/net10.0/FunnySharp.Tests.dll"
      "tests/FunnySharp.AspNetCore.Tests/bin/Release/net10.0/FunnySharp.AspNetCore.Tests.dll" ]

[<Literal>]
let CoreExampleMarker = "FunnySharp examples passed."

[<Literal>]
let AspNetExampleMarker = "FunnySharp ASP.NET Core example endpoints mapped."

let private buildSucceededPattern = Regex(@"^\s*Build succeeded\.\s*$", RegexOptions.IgnoreCase ||| RegexOptions.Multiline)
let private buildWarningsPattern = Regex(@"^\s*0 Warning\(s\)\s*$", RegexOptions.IgnoreCase ||| RegexOptions.Multiline)
let private buildErrorsPattern = Regex(@"^\s*0 Error\(s\)\s*$", RegexOptions.IgnoreCase ||| RegexOptions.Multiline)

let private testSummaryPattern =
    Regex(
        @"Test run summary:\s*Passed!.*?\btotal:\s*(?<total>\d+).*?\bfailed:\s*0\b.*?\bsucceeded:\s*(?<succeeded>\d+).*?\bskipped:\s*0\b",
        RegexOptions.IgnoreCase ||| RegexOptions.Singleline
    )

let private docsVerdictPattern =
    Regex(@"^\s*Verified \d+ C# documentation snippets across \d+ primary guides\.\s*$", RegexOptions.Multiline)

// MTP colors result words in CI logs.
let private ansiEscapePattern = Regex("\u001b\\[[0-?]*[ -/]*[@-~]")

/// A missing prerequisite or unusable environment with remediation.
type EnvironmentProblem =
    { Summary: string
      Remediation: string }

/// One local step's command: a captured child process, or the in-process docs verifier.
type StepCommand =
    | ChildProcess of exe: string * args: string list
    | DocumentationSnippets of repositoryRoot: string

type CommandRunner = StepCommand -> Map<string, string> -> string -> ProcessResult

/// The outcome of one local step.
type StepResult =
    { Name: string
      Status: string // passed | failed | skipped
      ExitCode: int option
      Message: string
      OutputTail: string list }

type CliOptions =
    { RepositoryRoot: string option
      Json: bool
      SkipDocs: bool
      SkipFormat: bool
      Help: bool }

/// Injected collaborators, so tests never touch process-global state.
type Collaborators =
    { Env: Map<string, string>
      Runner: CommandRunner
      /// The docs verifier's in-process entry, or None when it is not wired in.
      DocsVerifier: (string -> ProcessResult) option }

/// Wired to DocsSnippets by Program; tests inject Collaborators.DocsVerifier.
let mutable docsVerifier: (string -> ProcessResult) option = None

/// Resolve a command on PATH; honour executable bits on POSIX.
let findExecutable (name: string) (env: Map<string, string>) : string option =
    let path =
        match Map.tryFind "PATH" env with
        | Some value -> value
        | None -> Map.tryFind "Path" env |> Option.defaultValue ""

    let extensions =
        if OperatingSystem.IsWindows() then
            let pathext =
                defaultArg (Option.ofObj (Environment.GetEnvironmentVariable "PATHEXT")) ".EXE;.CMD;.BAT;.COM"

            (if String.IsNullOrEmpty pathext then ".EXE;.CMD;.BAT;.COM" else pathext).Split(';')
        else
            [||]

    let isExecutable (candidate: string) =
        if not (File.Exists candidate) then
            false
        elif OperatingSystem.IsWindows() then
            true
        else
            try
                let mode = File.GetUnixFileMode candidate

                mode.HasFlag UnixFileMode.UserExecute
                || mode.HasFlag UnixFileMode.GroupExecute
                || mode.HasFlag UnixFileMode.OtherExecute
            with _ ->
                true

    let tryDirectory (directory: string) =
        let baseDirectory = if directory = "" then "." else directory

        let candidates =
            if OperatingSystem.IsWindows() then
                [ for extension in extensions -> Path.Combine(baseDirectory, name + extension) ]
            else
                [ Path.Combine(baseDirectory, name) ]

        candidates |> List.tryFind isExecutable

    path.Split(Path.PathSeparator) |> Array.tryPick tryDirectory

/// Check the prerequisites this pre-check can verify without running dotnet.
let environmentProblems (env: Map<string, string>) : EnvironmentProblem list =
    [ if findExecutable "dotnet" env |> Option.isNone then
          { Summary = "dotnet was not found on PATH."
            Remediation =
              "install the .NET SDK pinned by global.json "
              + "and make sure `dotnet --version` works." } ]

/// Mirror the release protocol's command forms, minus release isolation flags.
let commandForStep (step: string) (repositoryRoot: string) : StepCommand =
    match step with
    | "restore" -> ChildProcess("dotnet", [ "restore"; "FunnySharp.slnx"; "--locked-mode" ])
    | "build" ->
        ChildProcess(
            "dotnet",
            [ "build"; "FunnySharp.slnx"; "--configuration"; "Release"; "--no-restore" ]
        )
    | "test" ->
        ChildProcess(
            "dotnet",
            [ "test"; "FunnySharp.slnx"; "--configuration"; "Release"; "--no-build"; "--no-restore" ]
        )
    | "examples" ->
        ChildProcess(
            "dotnet",
            [ "run"
              "--project"
              "examples/FunnySharp.Examples/FunnySharp.Examples.csproj"
              "--configuration"
              "Release"
              "--no-build"
              "--no-restore" ]
        )
    | "aspnetcore-examples" ->
        ChildProcess(
            "dotnet",
            [ "run"
              "--project"
              "examples/FunnySharp.AspNetCore.Examples/FunnySharp.AspNetCore.Examples.csproj"
              "--configuration"
              "Release"
              "--no-build"
              "--no-restore"
              "--"
              "--verify" ]
        )
    | "format" ->
        ChildProcess("dotnet", [ "format"; "FunnySharp.slnx"; "--verify-no-changes"; "--no-restore" ])
    | "docs" -> DocumentationSnippets repositoryRoot
    | _ -> failwithf "unknown local step: %s" step

let private stripAnsi (text: string) : string =
    ansiEscapePattern.Replace(text, "")

let private testAssemblyPattern (assembly: string) : Regex =
    Regex(
        @"^\s*" + Regex.Escape assembly + @"\s+\(net10\.0\|[^)]*\)\s+passed\s+\([^)]*\)\s*$",
        RegexOptions.IgnoreCase ||| RegexOptions.Multiline
    )

let private testFailure (text: string) (repositoryRoot: string) : string option =
    let summary = testSummaryPattern.Match text

    if not summary.Success then
        Some(
            "test log does not contain a 'Test run summary: Passed!' shape "
            + "with total, failed: 0, succeeded, and skipped: 0"
        )
    else
        let total = Int32.Parse(summary.Groups.["total"].Value)
        let succeeded = Int32.Parse(summary.Groups.["succeeded"].Value)

        if total <= 0 || total <> succeeded then
            Some(
                sprintf
                    "test summary must report a positive total equal to succeeded (total: %d, succeeded: %d)"
                    total
                    succeeded
            )
        else
            testAssemblyRelativePaths
            |> List.tryPick (fun relative ->
                // The real test log prints the resolved assembly path.
                let assembly = Path.GetFullPath(Path.Combine(repositoryRoot, relative))

                if (testAssemblyPattern assembly).IsMatch text then
                    None
                else
                    Some(sprintf "test log does not contain a passed result line for %s" assembly))

/// The release-equivalent verdict failure for a step, or None when it passes.
/// Restore and format are judged by exit code alone; docs must print its verdict line.
let stepFailure (step: string) (stdout: string) (stderr: string) (repositoryRoot: string) : string option =
    let combined =
        match step with
        | "build" | "test" | "examples" | "aspnetcore-examples" -> stripAnsi (stdout + "\n" + stderr)
        | _ -> ""

    match step with
    | "docs" ->
        if docsVerdictPattern.IsMatch stdout then
            None
        else
            Some "docs output does not contain a 'Verified N C# documentation snippets across N primary guides.' line"
    | "build" ->
        if not (buildSucceededPattern.IsMatch combined) then
            Some "build log does not contain a 'Build succeeded.' line"
        elif not (buildWarningsPattern.IsMatch combined) then
            Some "build log does not contain a '0 Warning(s)' line"
        elif not (buildErrorsPattern.IsMatch combined) then
            Some "build log does not contain a '0 Error(s)' line"
        else
            None
    | "test" -> testFailure combined repositoryRoot
    | "examples" ->
        if combined.ToLowerInvariant().Contains(CoreExampleMarker.ToLowerInvariant()) then
            None
        else
            Some(sprintf "examples output does not contain '%s'" CoreExampleMarker)
    | "aspnetcore-examples" ->
        if combined.ToLowerInvariant().Contains(AspNetExampleMarker.ToLowerInvariant()) then
            None
        else
            Some(sprintf "ASP.NET Core examples output does not contain '%s'" AspNetExampleMarker)
    | _ -> None

let outputTailLines = 20

/// Keep a bounded tail for diagnostics.
let outputTail (text: string) : string list =
    let lines = List.ofArray (Output.splitLines (text.Trim('\n')))
    let count = lines.Length

    if count <= outputTailLines then
        lines
    else
        lines |> List.skip (count - outputTailLines)

/// Run the local steps in order and stop at the first failed check.
let runSteps
    (repositoryRoot: string)
    (env: Map<string, string>)
    (runner: CommandRunner)
    (skipped: Set<string>)
    (stderr: TextWriter)
    : StepResult list * string option =
    let results = ResizeArray<StepResult>()
    let mutable failed: string option = None
    let mutable index = 0

    while failed.IsNone && index < localSteps.Length do
        let step = localSteps.[index]

        if skipped.Contains step then
            results.Add
                { Name = step
                  Status = "skipped"
                  ExitCode = None
                  Message = sprintf "requested by --skip-%s" step
                  OutputTail = [] }
        else
            let completed =
                try
                    runner (commandForStep step repositoryRoot) env repositoryRoot
                with
                | :? System.ComponentModel.Win32Exception as error ->
                    { ExitCode = 1; Stdout = ""; Stderr = "Could not start check: " + error.Message }
                | :? IOException as error ->
                    { ExitCode = 1; Stdout = ""; Stderr = "Could not run check: " + error.Message }
                | :? UnauthorizedAccessException as error ->
                    { ExitCode = 1; Stdout = ""; Stderr = "Could not run check: " + error.Message }
                | :? InvalidOperationException as error ->
                    { ExitCode = 1; Stdout = ""; Stderr = "Could not run check: " + error.Message }
            let stdout = stripAnsi completed.Stdout
            let stderrText = stripAnsi completed.Stderr

            let failure =
                if completed.ExitCode <> 0 then
                    Some(sprintf "exit code %d" completed.ExitCode)
                else
                    stepFailure step stdout stderrText repositoryRoot

            match failure with
            | Some message ->
                let tail = outputTail (stdout + "\n" + stderrText)
                stderr.WriteLine(sprintf "--- %s output (last %d lines) ---" step outputTailLines)

                for line in tail do
                    stderr.WriteLine line

                results.Add
                    { Name = step
                      Status = "failed"
                      ExitCode = Some completed.ExitCode
                      Message = message
                      OutputTail = tail }

                failed <- Some step
            | None ->
                results.Add
                    { Name = step
                      Status = "passed"
                      ExitCode = Some completed.ExitCode
                      Message = ""
                      OutputTail = [] }

        index <- index + 1

    List.ofSeq results, failed

type Report =
    { Options: CliOptions
      RepositoryRoot: string
      Status: string
      ExitCode: int
      Steps: StepResult list
      FailedStep: string option
      Message: string }

let private skippedSteps (options: CliOptions) : Set<string> =
    [ if options.SkipDocs then "docs"
      if options.SkipFormat then "format" ]
    |> Set.ofList

let reportJson (report: Report) : string =
    let optional value = value |> Option.map box |> Option.defaultValue Unchecked.defaultof<obj>
    let steps =
        report.Steps
        |> List.map (fun step ->
            dict [ "name", box step.Name
                   "status", box step.Status
                   "exitCode", optional step.ExitCode
                   "message", box step.Message
                   "outputTail", box (List.toArray step.OutputTail) ])
        |> List.toArray

    let payload =
        dict [ "tool", box ToolName
               "status", box report.Status
               "exitCode", box report.ExitCode
               "releaseEvidence", box false
               "preCheck", box true
               "repositoryRoot", box report.RepositoryRoot
               "skipped", box (localSteps |> List.filter (skippedSteps report.Options).Contains |> List.toArray)
               "steps", box steps
               "failedStep", optional report.FailedStep
               "notRun", box (List.toArray notRunSteps)
               "message", box report.Message ]

    JsonSerializer.Serialize(payload, JsonSerializerOptions(WriteIndented = true))

let humanSummary (report: Report) : string =
    let lines = ResizeArray<string>()
    lines.Add "FunnySharp local pre-check (not release evidence)."
    lines.Add(sprintf "Repository: %s" report.RepositoryRoot)
    lines.Add ""

    for step in report.Steps do
        match step.Status with
        | "passed" -> lines.Add(sprintf "PASS %s" step.Name)
        | "skipped" -> lines.Add(sprintf "SKIP %s (%s)" step.Name step.Message)
        | _ -> lines.Add(sprintf "FAIL %s: %s" step.Name step.Message)

    if report.Status = "environment-failure" then
        lines.Add(sprintf "ENVIRONMENT FAILURE: %s" report.Message)
    elif report.Status = "failed" then
        match report.FailedStep with
        | Some failed ->
            lines.Add(sprintf "Local pre-check FAILED at '%s': %s" failed report.Message)
        | None -> lines.Add(sprintf "Local pre-check FAILED: %s" report.Message)
    else
        lines.Add "Local pre-check passed."

    lines.Add ""
    lines.Add("NOT RUN (release protocol steps outside local scope): " + String.concat ", " notRunSteps)

    lines.Add
        "This is a local pre-check, not release evidence; run `dotnet fsi build.fsx -- -p release` for the release gate."

    String.concat "\n" lines

let private emit (stdout: TextWriter) (stderr: TextWriter) (options: CliOptions) (report: Report) : int =
    if options.Json then
        stdout.WriteLine(reportJson report)

        if report.Status <> "passed" then
            stderr.WriteLine(humanSummary report)
    else
        stdout.WriteLine(humanSummary report)

    report.ExitCode

let private environmentFailure
    (stdout: TextWriter)
    (stderr: TextWriter)
    (options: CliOptions)
    (repositoryRoot: string)
    (problems: EnvironmentProblem list)
    : int =
    for problem in problems do
        stderr.WriteLine("ERROR: " + problem.Summary)
        stderr.WriteLine("  Remediation: " + problem.Remediation)

    let message = problems |> List.map (fun problem -> problem.Summary) |> String.concat "; "

    let report =
        { Options = options
          RepositoryRoot = repositoryRoot
          Status = "environment-failure"
          ExitCode = 2
          Steps = []
          FailedStep = None
          Message = message }

    emit stdout stderr options report

let runChildProcess
    (exe: string)
    (args: string list)
    (env: Map<string, string>)
    (cwd: string)
    : ProcessResult =
    use proc = new System.Diagnostics.Process()

    // Resolve against the child PATH, not the parent environment.
    let resolvedExe =
        match findExecutable exe env with
        | Some path -> path
        | None -> exe

    let info = System.Diagnostics.ProcessStartInfo resolvedExe
    info.UseShellExecute <- false
    info.RedirectStandardOutput <- true
    info.RedirectStandardError <- true
    info.WorkingDirectory <- cwd

    for arg in args do
        info.ArgumentList.Add arg

    info.Environment.Clear()

    for KeyValue(key, value) in env do
        info.Environment.[key] <- value

    proc.StartInfo <- info

    if not (proc.Start()) then
        invalidOp (sprintf "failed to start process '%s'" exe)

    let stdout = proc.StandardOutput.ReadToEndAsync()
    let stderr = proc.StandardError.ReadToEndAsync()

    // A hung child must fail this gate step in bounded time (600s), never hang
    // it indefinitely: kill the whole process tree and report exit 124.
    let exitCode =
        if proc.WaitForExit(600_000) then
            proc.ExitCode
        else
            (try
                proc.Kill true
             with _ ->
                ())

            proc.WaitForExit 5_000 |> ignore
            124

    { ExitCode = exitCode
      Stdout = stdout.GetAwaiter().GetResult()
      Stderr = stderr.GetAwaiter().GetResult() }

let private environmentMap () : Map<string, string> =
    Environment.GetEnvironmentVariables()
    |> Seq.cast<System.Collections.DictionaryEntry>
    |> Seq.map (fun entry -> string entry.Key, string entry.Value)
    |> Map.ofSeq

let private usageLine =
    "usage: verify-tooling [-h] [--repository-root PATH] [--json] [--skip-docs] [--skip-format]"

let private helpText =
    String.concat
        "\n"
        [ usageLine
          ""
          "Run comprehensive FunnySharp local checks. This is a local pre-check, not release evidence."
          ""
          "options:"
          "  -h, --help            show this help message and exit"
          "  --repository-root PATH"
          "                        repository root (default: resolved from the current directory;"
          "                        that location must be inside a FunnySharp checkout)."
          "  --json                print a machine-readable summary on stdout."
          "  --skip-docs           skip the documentation-snippet step."
          "  --skip-format         skip the formatter step." ]

let private defaultOptions =
    { RepositoryRoot = None
      Json = false
      SkipDocs = false
      SkipFormat = false
      Help = false }

let private parseArgs (argv: string list) : Result<CliOptions, string> =
    let rec loop (options: CliOptions) (remaining: string list) =
        match remaining with
        | [] -> Ok options
        | "-h" :: _ | "--help" :: _ -> Ok { options with Help = true }
        | "--offline" :: _ -> Error "--offline is not supported; NuGet restore may require network access"
        | "--json" :: rest -> loop { options with Json = true } rest
        | "--skip-docs" :: rest -> loop { options with SkipDocs = true } rest
        | "--skip-format" :: rest -> loop { options with SkipFormat = true } rest
        | "--repository-root" :: value :: rest -> loop { options with RepositoryRoot = Some value } rest
        | [ "--repository-root" ] -> Error "argument --repository-root: expected one argument"
        | arg :: rest when arg.StartsWith "--repository-root=" ->
            loop { options with RepositoryRoot = Some(arg.Substring("--repository-root=".Length)) } rest
        | arg :: _ -> Error("unrecognized arguments: " + arg)

    loop defaultOptions argv

/// Run the pre-check writing to the supplied writers and injected collaborators.
let mainWith
    (stdout: TextWriter)
    (stderr: TextWriter)
    (startDirectory: string)
    (argv: string list)
    (collaborators: Collaborators)
    : int =
    match parseArgs argv with
    | Error message ->
        stderr.WriteLine usageLine
        stderr.WriteLine(ToolName + ": error: " + message)
        2
    | Ok options when options.Help ->
        stdout.WriteLine helpText
        0
    | Ok options ->
        let skipped = skippedSteps options

        let repositoryRoot =
            match options.RepositoryRoot with
            | Some root -> Ok(Path.GetFullPath root)
            | None ->
                match tryFindRootFrom startDirectory with
                | Some root -> Ok root
                | None ->
                    Error(
                        sprintf
                            "'%s' is not inside a FunnySharp checkout; the repository root could not be found."
                            startDirectory
                    )

        match repositoryRoot with
        | Error summary ->
            environmentFailure
                stdout
                stderr
                options
                startDirectory
                [ { Summary = summary
                    Remediation =
                      "run this tool from a FunnySharp checkout or pass --repository-root <path>." } ]
        | Ok root ->
            let problems = environmentProblems collaborators.Env

            if not problems.IsEmpty then
                environmentFailure stdout stderr options root problems
            elif not (File.Exists(Path.Combine(root, SolutionFileName))) then
                environmentFailure
                    stdout
                    stderr
                    options
                    root
                    [ { Summary = sprintf "the repository root does not contain %s." SolutionFileName
                        Remediation =
                          "pass --repository-root pointing at a FunnySharp checkout." } ]
            elif not (skipped.Contains "docs") && collaborators.DocsVerifier.IsNone then
                environmentFailure
                    stdout
                    stderr
                    options
                    root
                    [ { Summary = "the documentation-snippet verifier was not found."
                        Remediation =
                          "wire FunnySharp.Harness.DocsSnippets into the docs verifier, or pass "
                          + "--skip-docs to run the other local checks." } ]
            else
                let childEnv = collaborators.Env |> Map.add "DOTNET_CLI_UI_LANGUAGE" "en"

                let results, failedStep =
                    runSteps root childEnv collaborators.Runner skipped stderr

                let report =
                    match failedStep with
                    | None ->
                        { Options = options
                          RepositoryRoot = root
                          Status = "passed"
                          ExitCode = 0
                          Steps = results
                          FailedStep = None
                          Message = "Local pre-check passed." }
                    | Some step ->
                        { Options = options
                          RepositoryRoot = root
                          Status = "failed"
                          ExitCode = 1
                          Steps = results
                          FailedStep = Some step
                          Message = (List.last results).Message }

                emit stdout stderr options report

/// CLI entry point.
let main (argv: string array) : int =
    let collaborators =
        { Env = environmentMap ()
          Runner =
            fun command childEnv cwd ->
                match command with
                | ChildProcess(exe, args) -> runChildProcess exe args childEnv cwd
                | DocumentationSnippets root ->
                    match docsVerifier with
                    | Some verify -> verify root
                    | None ->
                        { ExitCode = 2
                          Stdout = ""
                          Stderr = "the documentation-snippet verifier is not wired into this build." }
          DocsVerifier = docsVerifier }

    mainWith Console.Out Console.Error Environment.CurrentDirectory (List.ofArray argv) collaborators
