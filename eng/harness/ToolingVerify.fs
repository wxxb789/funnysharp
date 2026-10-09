module FunnySharp.Harness.ToolingVerify

// A behaviour-identical F# port of eng/tools/verify_local.py: the PowerShell-free
// contributor pre-check over the local subset of eng/release-protocol.json steps.
// It runs restore, Release build, tests, both examples, the formatter check and the
// documentation-snippet verifier in protocol order, reproducing the release
// verifier's verdicts for those steps, and reports everything else as
// not run. First and last human lines, step lines, the failed-step output-tail
// header, the environment-failure shape, the report JSON key set and the 0/1/2 exit
// codes are contract text and must stay byte-exact.
//
// Verdicts are checked against actual step output. Source-literal presence cannot
// establish equivalent behavior: comments can satisfy it and harmless refactors can fail it.
//
// Documentation step: the docs step's verifier is FunnySharp.Harness.DocsSnippets. The single
// point of substitution is `docsVerifier`; build.fsx sets it to a function that runs that module
// in process and captures its stdout/stderr/exit code. Until it is wired, `main` reports the
// docs step as an unavailable prerequisite (exit 2) unless --skip-docs is passed.
//
// Deviations from verify_local.py, recorded rather than silent:
//  * The F# tool no longer runs under uv/Python, so the uv and pinned-interpreter
//    prerequisites are gone; only `dotnet` on PATH is checked.
//  * The repository root defaults to the nearest FunnySharp.slnx ancestor instead
//    of the script's parents[2].
//  * Proc.runCaptureIn exposes no environment parameter, so the child runner here
//    builds ProcessStartInfo directly to forward the per-child environment
//    (DOTNET_CLI_UI_LANGUAGE=en, and UV_OFFLINE=1 under --offline) without mutating
//    this process' environment.

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open FunnySharp.Harness.Proc
open FunnySharp.Harness.Repo
open FunnySharp.Harness.Output

// ---------------------------------------------------------------------------
// Contract constants
// ---------------------------------------------------------------------------

[<Literal>]
let ToolName = "verify_local.py"

/// The release-protocol steps this pre-check runs locally, in protocol order.
let localSteps: string list =
    [ "restore"; "build"; "test"; "examples"; "aspnetcore-examples"; "format"; "docs" ]

/// Every eng/release-protocol.json (full mode) step outside local scope, in order.
let notRunSteps: string list =
    [ "clean"
      "pack"
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

// Verdict-rule fragments used to parse actual build and test output.
[<Literal>]
let BuildSucceededFragment = @"Build succeeded\."

[<Literal>]
let BuildWarningsFragment = @"0 Warning\(s\)"

[<Literal>]
let BuildErrorsFragment = @"0 Error\(s\)"

[<Literal>]
let TestSummaryFragment = @"Test run summary:\s*Passed!"

[<Literal>]
let TestTotalFragment = @"\btotal:\s*(?<total>\d+)"

[<Literal>]
let TestFailedFragment = @"\bfailed:\s*0"

[<Literal>]
let TestSucceededFragment = @"\bsucceeded:\s*(?<succeeded>\d+)"

[<Literal>]
let TestSkippedFragment = @"\bskipped:\s*0"

[<Literal>]
let TestAssemblyResultFragment = @"\(net10\.0\|[^)]*\)\s+passed\s+\([^)]*\)\s*$"

/// The PowerShell-free docs verifier's success line; exit status alone is not enough.
[<Literal>]
let DocsVerdictFragment = @"Verified \d+ C# documentation snippets across \d+ primary guides\."

let private buildSucceededPattern =
    Regex(@"^\s*" + BuildSucceededFragment + @"\s*$", RegexOptions.IgnoreCase ||| RegexOptions.Multiline)

let private buildWarningsPattern =
    Regex(@"^\s*" + BuildWarningsFragment + @"\s*$", RegexOptions.IgnoreCase ||| RegexOptions.Multiline)

let private buildErrorsPattern =
    Regex(@"^\s*" + BuildErrorsFragment + @"\s*$", RegexOptions.IgnoreCase ||| RegexOptions.Multiline)

let private testSummaryPattern =
    Regex(
        TestSummaryFragment
        + @".*?"
        + TestTotalFragment
        + @".*?"
        + TestFailedFragment
        + @".*?"
        + TestSucceededFragment
        + @".*?"
        + TestSkippedFragment,
        RegexOptions.IgnoreCase ||| RegexOptions.Singleline
    )

let private docsVerdictPattern = Regex(@"^\s*" + DocsVerdictFragment + @"\s*$", RegexOptions.Multiline)

// Mirrors eng/Verify-Release.ps1's Remove-AnsiControlSequences: GitHub Actions sets
// CI=true and the MTP reporter colors result words, so logs are normalized first.
let private ansiEscapePattern = Regex("\u001b\\[[0-?]*[ -/]*[@-~]")

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

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
      Offline: bool
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

/// The documentation-snippet step's verifier. Integration points this at lane A's
/// FunnySharp.Harness.DocsSnippets; tests inject Collaborators.DocsVerifier instead.
let mutable docsVerifier: (string -> ProcessResult) option = None

// ---------------------------------------------------------------------------
// Repository and environment resolution
// ---------------------------------------------------------------------------

/// shutil.which: resolve a command name on PATH (executable bit honoured on POSIX).
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

// ---------------------------------------------------------------------------
// Commands and verdicts
// ---------------------------------------------------------------------------

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
        @"^\s*" + Regex.Escape assembly + @"\s+" + TestAssemblyResultFragment,
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
                // The real test log prints the resolved path, so the pattern must use the same spelling
                // the release verifier uses (ReleaseVerifySource.assertTestMarkers).
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
        | "build" | "test" | "examples" | "aspnetcore-examples" -> stdout + "\n" + stderr
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
        // The frozen verifier uses PowerShell -notmatch, which is case-insensitive.
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

/// The bounded last-N lines of child output (mirrors inventory._tail's bound).
let outputTail (text: string) : string list =
    let lines = List.ofArray (Output.splitLines (text.Trim('\n')))
    let count = lines.Length

    if count <= outputTailLines then
        lines
    else
        lines |> List.skip (count - outputTailLines)

// ---------------------------------------------------------------------------
// Step execution
// ---------------------------------------------------------------------------

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
            let completed = runner (commandForStep step repositoryRoot) env repositoryRoot
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

// ---------------------------------------------------------------------------
// Reporting
// ---------------------------------------------------------------------------

type Report =
    { Options: CliOptions
      RepositoryRoot: string
      Status: string
      ExitCode: int
      Steps: StepResult list
      FailedStep: string option
      Message: string }

// The report JSON is rendered by hand to match Python's json.dumps(report, indent=2)
// key order and value forms (including a JSON null failedStep) exactly, without
// depending on JsonNode APIs that the nullable-reference-type checker rejects.
let private pad (level: int) : string = String.replicate (level * 2) " "

let private jsonEscape (value: string) : string =
    let builder = StringBuilder()

    for ch in value do
        match ch with
        | '"' -> builder.Append "\\\"" |> ignore
        | '\\' -> builder.Append "\\\\" |> ignore
        | '\n' -> builder.Append "\\n" |> ignore
        | '\r' -> builder.Append "\\r" |> ignore
        | '\t' -> builder.Append "\\t" |> ignore
        | c when int c < 0x20 -> builder.Append(sprintf "\\u%04x" (int c)) |> ignore
        | c -> builder.Append c |> ignore

    builder.ToString()

let private jsonString (value: string) : string = "\"" + jsonEscape value + "\""

let private skippedSteps (options: CliOptions) : Set<string> =
    let mutable skipped = Set.empty
    if options.SkipDocs then skipped <- skipped.Add "docs"
    if options.SkipFormat then skipped <- skipped.Add "format"
    skipped

/// Render an array whose elements are already-escaped values at `level` elements deep.
let private appendArray (builder: StringBuilder) (level: int) (elements: string list) =
    if elements.IsEmpty then
        builder.Append "[]" |> ignore
    else
        builder.Append "[\n" |> ignore

        elements
        |> List.iteri (fun index element ->
            builder.Append(pad (level + 1)).Append(element).Append(if index = elements.Length - 1 then "\n" else ",\n")
            |> ignore)

        builder.Append(pad level).Append "]" |> ignore

let reportJson (report: Report) : string =
    let builder = StringBuilder()
    let line (level: int) (text: string) = builder.Append(pad level).Append(text).Append('\n') |> ignore
    let key (level: int) (name: string) = builder.Append(pad level).Append(jsonString name).Append ": " |> ignore

    let boolText value = if value then "true" else "false"
    builder.Append "{\n" |> ignore
    line 1 (jsonString "tool" + ": " + jsonString ToolName + ",")
    line 1 (jsonString "status" + ": " + jsonString report.Status + ",")
    line 1 (jsonString "exitCode" + ": " + string report.ExitCode + ",")
    line 1 (jsonString "releaseEvidence" + ": false,")
    line 1 (jsonString "preCheck" + ": true,")
    line 1 (jsonString "repositoryRoot" + ": " + jsonString report.RepositoryRoot + ",")
    line 1 (jsonString "offline" + ": " + boolText report.Options.Offline + ",")

    let skipSet = skippedSteps report.Options
    let skipped = localSteps |> List.filter skipSet.Contains

    key 1 "skipped"
    appendArray builder 1 (skipped |> List.map jsonString)
    builder.Append ",\n" |> ignore

    key 1 "steps"

    if report.Steps.IsEmpty then
        builder.Append "[],\n" |> ignore
    else
        builder.Append "[\n" |> ignore

        report.Steps
        |> List.iteri (fun index step ->
            builder.Append(pad 2).Append "{\n" |> ignore
            line 3 (jsonString "name" + ": " + jsonString step.Name + ",")
            line 3 (jsonString "status" + ": " + jsonString step.Status + ",")
            line 3 (jsonString "exitCode" + ": " + (match step.ExitCode with Some code -> string code | None -> "null") + ",")
            line 3 (jsonString "message" + ": " + jsonString step.Message + (if step.OutputTail.IsEmpty then "" else ","))

            if not step.OutputTail.IsEmpty then
                builder.Append(pad 3).Append(jsonString "outputTail").Append ": " |> ignore
                appendArray builder 3 (step.OutputTail |> List.map jsonString)
                builder.Append "\n" |> ignore

            builder.Append(pad 2).Append(if index = report.Steps.Length - 1 then "}\n" else "},\n") |> ignore)

        builder.Append(pad 1).Append "]" |> ignore
        builder.Append ",\n" |> ignore

    line 1 (jsonString "failedStep" + ": " + (match report.FailedStep with Some step -> jsonString step | None -> "null") + ",")
    key 1 "notRun"
    appendArray builder 1 (notRunSteps |> List.map jsonString)
    builder.Append ",\n" |> ignore
    line 1 (jsonString "message" + ": " + jsonString report.Message)
    builder.Append "}" |> ignore
    builder.ToString()

let humanSummary (report: Report) : string =
    let lines = ResizeArray<string>()
    lines.Add "FunnySharp local pre-check (not release evidence)."
    lines.Add(sprintf "Repository: %s" report.RepositoryRoot)
    lines.Add(sprintf "Offline: %s" (if report.Options.Offline then "yes" else "no"))
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
        "This is a local pre-check, not release evidence; run the PowerShell release protocol for the release gate."

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

    if options.Json then
        stdout.WriteLine(reportJson report)
        2
    else
        stdout.WriteLine(humanSummary report)
        2

// ---------------------------------------------------------------------------
// Child processes
// ---------------------------------------------------------------------------

/// Capturing run with an explicit child environment; never through a shell. The
/// per-child env (DOTNET_CLI_UI_LANGUAGE / UV_OFFLINE) is a contract, and
/// Proc.runCaptureIn has no environment parameter, so this builds the start info.
let runChildProcess
    (exe: string)
    (args: string list)
    (env: Map<string, string>)
    (cwd: string)
    : ProcessResult =
    use proc = new System.Diagnostics.Process()

    // Resolve the executable with the *child* environment's PATH (as Python's
    // subprocess does), not this process' PATH; otherwise an injected PATH cannot
    // select a stub executable.
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
        failwithf "failed to start process '%s'" exe

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

// ---------------------------------------------------------------------------
// CLI
// ---------------------------------------------------------------------------

let private usageLine =
    "usage: verify_local.py [-h] [--repository-root PATH] [--offline] [--json] [--skip-docs] [--skip-format]"

let private helpText =
    String.concat
        "\n"
        [ usageLine
          ""
          "Run the core FunnySharp local checks with release-equivalent verdicts. This is a local pre-check, not release evidence."
          ""
          "options:"
          "  -h, --help            show this help message and exit"
          "  --repository-root PATH"
          "                        repository root (default: resolved from the current directory;"
          "                        that location must be inside a FunnySharp checkout)."
          "  --offline             export UV_OFFLINE=1 to child processes and never attempt"
          "                        network-dependent setup."
          "  --json                print a machine-readable summary on stdout."
          "  --skip-docs           skip the documentation-snippet step."
          "  --skip-format         skip the formatter step." ]

let private defaultOptions =
    { RepositoryRoot = None
      Offline = false
      Json = false
      SkipDocs = false
      SkipFormat = false
      Help = false }

let private parseArgs (argv: string list) : Result<CliOptions, string> =
    let rec loop (options: CliOptions) (remaining: string list) =
        match remaining with
        | [] -> Ok options
        | "-h" :: _ | "--help" :: _ -> Ok { options with Help = true }
        | "--offline" :: rest -> loop { options with Offline = true } rest
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
        stderr.WriteLine("verify_local.py: error: " + message)
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
                            "'%s' is not inside a git repository, so the repository root cannot be derived from the script location."
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
                let childEnv =
                    collaborators.Env
                    |> Map.add "DOTNET_CLI_UI_LANGUAGE" "en"
                    |> fun environment ->
                        if options.Offline then
                            Map.add "UV_OFFLINE" "1" environment
                        else
                            environment

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

/// Entry point mirroring verify_local.py's main().
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
