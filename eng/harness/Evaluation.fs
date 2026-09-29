module FunnySharp.Harness.Evaluation

// A behaviour-identical F# port of eng/evaluation/runner.py: the model-agnostic
// coding-evaluation harness. It packs the two FunnySharp packages into the local
// evaluation feed, verifies one recorded solution directory (copy template +
// tests + solution into a transient build tree, build, test, record) and
// aggregates every record under results/ into a markdown table.
//
// CLI surface preserved exactly:
//   prep-feed
//   verify <task> <idiomatic|funnysharp> <run_dir> [--round N]
//   aggregate <out>
// Exit codes: 0 pass/green, 1 red or pack failure, 2 usage/unknown-task/no-solution.
//
// Every dotnet child runs with DOTNET_CLI_UI_LANGUAGE=en because the summary
// regexes are English- and order-sensitive, and with the repo's isolated NuGet
// caches. results/ is append-only: verify writes <run_dir>/record.json only.

open System
open System.Diagnostics
open System.IO
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open System.Text.RegularExpressions
open FunnySharp.Harness.Output
open FunnySharp.Harness.Proc
open FunnySharp.Harness.Repo

let private styles = [ "idiomatic"; "funnysharp" ]

// ---- Python text helpers ---------------------------------------------------

/// Python's str.splitlines(): breaks on every line boundary (including the extra
/// unicode ones) and never emits a trailing empty line for a trailing terminator.
let private splitLines (text: string) : string array =
    let lines = ResizeArray<string>()

    let isLineBreak (ch: char) =
        match int ch with
        | 10 | 13 | 11 | 12 | 28 | 29 | 30 | 133 | 8232 | 8233 -> true
        | _ -> false

    let mutable start = 0
    let mutable index = 0

    while index < text.Length do
        if isLineBreak text.[index] then
            lines.Add(text.Substring(start, index - start))

            if text.[index] = '\r' && index + 1 < text.Length && text.[index + 1] = '\n' then
                index <- index + 1

            index <- index + 1
            start <- index
        else
            index <- index + 1

    if start < text.Length then
        lines.Add(text.Substring start)

    lines.ToArray()

/// The last <paramref name="length"/> characters of a string (Python s[-n:]).
let private tailChars (length: int) (text: string) : string =
    if text.Length <= length then text else text.Substring(text.Length - length)

/// Python's "\n".join(text.strip().splitlines()[-n:]).
let private tailLines (count: int) (text: string) : string =
    let lines = splitLines (text.Trim())

    let last =
        if lines.Length <= count then lines
        else lines.[lines.Length - count ..]

    String.Join("\n", last)

/// Python's Path(x).name, tolerant of a trailing separator.
let private pathName (path: string) : string =
    let trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)

    match Path.GetFileName trimmed with
    | null -> ""
    | name -> name

// ---- consumer-side LOC -----------------------------------------------------

/// Count non-blank, non-comment lines of a C# file. The counting rule lives in
/// FunnySharp.Harness.Loc (it is also loc.py's counter), so the evaluation record and
/// the call-site tool cannot drift apart.
let countLoc (path: string) : int = Loc.countLoc path

// ---- child process ---------------------------------------------------------

/// Run a child process with the harness environment, capturing UTF-8 output.
let private runProcessWithEnv
    (workingDirectory: string)
    (environment: (string * string) list)
    (command: string list)
    : ProcessResult =
    match command with
    | [] -> invalidArg "command" "the command list must not be empty"
    | exe :: args ->
        use proc = new Process()
        let info = ProcessStartInfo exe
        info.UseShellExecute <- false
        info.RedirectStandardOutput <- true
        info.RedirectStandardError <- true
        info.WorkingDirectory <- workingDirectory
        info.StandardOutputEncoding <- UTF8Encoding(false)
        info.StandardErrorEncoding <- UTF8Encoding(false)

        for arg in args do
            info.ArgumentList.Add arg

        for key, value in environment do
            info.Environment.[key] <- value

        proc.StartInfo <- info

        if not (proc.Start()) then
            failwithf "failed to start process '%s'" exe

        let stdoutTask = proc.StandardOutput.ReadToEndAsync()
        let stderrTask = proc.StandardError.ReadToEndAsync()
        proc.WaitForExit()
        let stdout = stdoutTask.GetAwaiter().GetResult()
        let stderr = stderrTask.GetAwaiter().GetResult()

        { ExitCode = proc.ExitCode
          Stdout = stdout
          Stderr = stderr }

/// The process runner. Tests replace this to inject canned dotnet output.
let mutable childRunner : string -> (string * string) list -> string list -> ProcessResult =
    fun workingDirectory environment command -> runProcessWithEnv workingDirectory environment command

/// Monotonic clock in seconds. Tests replace this to make buildSeconds deterministic.
let mutable clock : unit -> float =
    fun () -> float (Stopwatch.GetTimestamp()) / float Stopwatch.Frequency

/// The environment every dotnet child sees, mirroring runner.DOTNET_ENV.
let childEnvironment (repositoryRoot: string) : (string * string) list =
    let home = Environment.GetFolderPath Environment.SpecialFolder.UserProfile
    let path = Environment.GetEnvironmentVariable "PATH"

    let extendedPath =
        match path with
        | null -> Path.Combine(home, ".dotnet")
        | value -> value + string Path.PathSeparator + Path.Combine(home, ".dotnet")

    [ "DOTNET_CLI_TELEMETRY_OPTOUT", "1"
      "DOTNET_NOLOGO", "1"
      "DOTNET_CLI_UI_LANGUAGE", "en"
      "NUGET_PACKAGES", Path.Combine(repositoryRoot, "artifacts", ".nuget-packages-dev")
      "NUGET_HTTP_CACHE_PATH", Path.Combine(repositoryRoot, "artifacts", ".nuget-http-dev")
      "PATH", extendedPath ]

// ---- prep-feed -------------------------------------------------------------

let private nugetPackagesDir (repositoryRoot: string) : string =
    let value =
        childEnvironment repositoryRoot
        |> List.tryFind (fun (key, _) -> key = "NUGET_PACKAGES")

    match value with
    | Some(_, path) -> path
    | None -> Path.Combine(repositoryRoot, "artifacts", ".nuget-packages-dev")

let private nupkgName =
    Regex(
        @"^(?<id>.+)\.(?<version>\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)*)\.nupkg$"
    )

let private sortedNupkgs (feedDir: string) : string list =
    Directory.GetFiles(feedDir, "*.nupkg")
    |> Array.map (fun path ->
        match Path.GetFileName path with
        | null -> path
        | name -> name)
    |> Array.sortWith (fun left right -> String.CompareOrdinal(left, right))
    |> List.ofArray

let private runPrepFeed (repositoryRoot: string) (stdout: TextWriter) (stderr: TextWriter) : int =
    let feedDir = Path.Combine(repositoryRoot, "artifacts", "evaluation", "feed")
    Directory.CreateDirectory feedDir |> ignore
    let environment = childEnvironment repositoryRoot

    let projects =
        [ "src/FunnySharp/FunnySharp.csproj"
          "src/FunnySharp.AspNetCore/FunnySharp.AspNetCore.csproj" ]

    let mutable failed = false

    for project in projects do
        if not failed then
            let command =
                [ "dotnet"; "pack"; project; "--configuration"; "Release"; "--output"; feedDir ]

            let result = childRunner repositoryRoot environment command

            if result.ExitCode <> 0 then
                stdout.WriteLine(tailChars 4000 result.Stdout)
                stdout.WriteLine(tailChars 4000 result.Stderr)
                stderr.WriteLine(sprintf "prep-feed failed for %s" project)
                failed <- true

    if failed then
        1
    else
        let packages = sortedNupkgs feedDir
        let packagesCache = nugetPackagesDir repositoryRoot

        for package in packages do
            let matched = nupkgName.Match package

            if matched.Success then
                let cached =
                    Path.Combine(packagesCache, matched.Groups.["id"].Value.ToLowerInvariant(), matched.Groups.["version"].Value.ToLowerInvariant())

                if Directory.Exists cached then
                    Directory.Delete(cached, true)

        stdout.WriteLine("Prepared evaluation feed:")

        for package in packages do
            stdout.WriteLine(sprintf "  %s" package)

        0

// ---- verify ----------------------------------------------------------------

let private errorLine = Regex @": error [A-Z]+[0-9]+:"
let private fsDiagnostic = Regex @": (warning|error) (FS[0-9]{4})"
let private testSummary = Regex @"Test run summary: (Passed|Failed)!"

let private testCounts =
    Regex(
        @"total:\s*(\d+).*failed:\s*(\d+).*succeeded:\s*(\d+).*skipped:\s*(\d+)",
        RegexOptions.Singleline
    )

let private writeNugetConfig (repositoryRoot: string) (buildDir: string) : unit =
    let feedDir = Path.Combine(repositoryRoot, "artifacts", "evaluation", "feed")
    let relativeFeed = Path.GetRelativePath(buildDir, feedDir).Replace('\\', '/')

    let content =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
        + "<configuration>\n"
        + "  <packageSources>\n"
        + "    <clear />\n"
        + sprintf "    <add key=\"evaluation-feed\" value=\"%s\" />\n" relativeFeed
        + "    <add key=\"nuget.org\" value=\"https://api.nuget.org/v3/index.json\" />\n"
        + "  </packageSources>\n"
        + "</configuration>\n"

    File.WriteAllText(Path.Combine(buildDir, "NuGet.config"), content, UTF8Encoding(false))

/// Sorted *.cs files in a directory: full-path ordinal order, matching sorted(glob).
let private sortedCsFiles (directory: string) : string list =
    Directory.GetFiles(directory, "*.cs")
    |> Array.sortWith (fun left right -> String.CompareOrdinal(left, right))
    |> List.ofArray

let private jsonOptions = JsonSerializerOptions(WriteIndented = true)

let private jstr (value: string) : JsonValue =
    Option.ofObj (JsonValue.Create value)
    |> Option.defaultWith (fun () -> failwith "JsonValue.Create returned null for a string")

let private jint (value: int) : JsonValue = JsonValue.Create value

let private jbool (value: bool) : JsonValue = JsonValue.Create value

/// Python json.dumps(round(x, 1)): a float always keeps a decimal point ("7.0").
let private jfloat (value: float) : JsonNode =
    let text = value.ToString("0.0", Globalization.CultureInfo.InvariantCulture)

    match Option.ofObj (JsonNode.Parse text) with
    | Some node -> node
    | None -> failwith "JsonNode.Parse returned null for a float"

/// F# cannot pass a literal null to a JsonObject setter; an uninitialized node
/// serializes as JSON null, matching Python's `ok: null`.
let private jsonNull : JsonNode = Unchecked.defaultof<JsonNode>

let private runVerify
    (repositoryRoot: string)
    (stdout: TextWriter)
    (stderr: TextWriter)
    (task: string)
    (style: string)
    (runDir: string)
    (roundNumber: int)
    : int =
    let evaluationRoot = Path.Combine(repositoryRoot, "eng", "evaluation")
    let taskDir = Path.Combine(evaluationRoot, "tasks", task)
    let solutionDir = Path.Combine(runDir, "solution")
    let templateDir = Path.Combine(taskDir, "template-" + style)

    if not (Directory.Exists taskDir) then
        stderr.WriteLine(sprintf "unknown task %s" task)
        2
    elif not (Directory.Exists solutionDir) || (Directory.GetFiles(solutionDir, "*.cs").Length = 0) then
        stderr.WriteLine(sprintf "%s contains no solution files" solutionDir)
        2
    else
        let buildRoot = Path.Combine(repositoryRoot, "artifacts", "evaluation", "builds")
        let buildDir = Path.Combine(buildRoot, sprintf "%s-%s-%s" task style (pathName runDir))

        if Directory.Exists buildDir then
            Directory.Delete(buildDir, true)

        Directory.CreateDirectory buildDir |> ignore

        for item in Directory.GetFiles templateDir do
            let name =
                match Path.GetFileName item with
                | null -> item
                | value -> value

            File.Copy(item, Path.Combine(buildDir, name), true)

        for testFile in sortedCsFiles (Path.Combine(taskDir, "tests")) do
            File.Copy(testFile, Path.Combine(buildDir, pathName testFile), true)

        for solutionFile in sortedCsFiles solutionDir do
            File.Copy(solutionFile, Path.Combine(buildDir, pathName solutionFile), true)

        writeNugetConfig repositoryRoot buildDir

        let environment = childEnvironment repositoryRoot
        let started = clock ()
        let build = childRunner buildDir environment [ "dotnet"; "build"; "--configuration"; "Release" ]
        let buildSeconds = clock () - started
        let buildOutput = build.Stdout + build.Stderr
        let compileErrors = errorLine.Matches(buildOutput).Count

        let fsDiagnostics =
            fsDiagnostic.Matches(buildOutput)
            |> Seq.map (fun matched -> matched.Groups.[2].Value)
            |> Seq.distinct
            |> Seq.sortWith (fun left right -> String.CompareOrdinal(left, right))
            |> List.ofSeq

        let mutable testOk : bool option = None
        let mutable testTotal = 0
        let mutable testFailed = 0
        let mutable testOutput = ""

        if build.ExitCode = 0 then
            let test =
                childRunner buildDir environment [ "dotnet"; "test"; "--configuration"; "Release"; "--no-build" ]

            testOutput <- test.Stdout + test.Stderr
            let summary = testSummary.Match testOutput
            let counts = testCounts.Match testOutput
            testOk <- Some(test.ExitCode = 0 && summary.Success && summary.Groups.[1].Value = "Passed")

            if counts.Success then
                testTotal <- Int32.Parse counts.Groups.[1].Value
                testFailed <- Int32.Parse counts.Groups.[2].Value

        let loc = sortedCsFiles solutionDir |> List.sumBy countLoc

        let compilation = JsonObject()
        compilation.["ok"] <- jbool (build.ExitCode = 0)
        compilation.["errors"] <- jint compileErrors
        compilation.["buildSeconds"] <- jfloat (Math.Round(buildSeconds, 1))

        let semantic = JsonObject()

        semantic.["ok"] <-
            (match testOk with
             | Some value -> jbool value :> JsonNode
             | None -> jsonNull)

        semantic.["total"] <- jint testTotal
        semantic.["failed"] <- jint testFailed

        let apiMisuse = JsonObject()
        let diagnostics = JsonArray()

        for diagnostic in fsDiagnostics do
            diagnostics.Add(jstr diagnostic)

        apiMisuse.["fsDiagnostics"] <- diagnostics

        let record = JsonObject()
        record.["task"] <- jstr task
        record.["style"] <- jstr style
        record.["run"] <- jstr (pathName runDir)
        record.["round"] <- jint roundNumber
        record.["compilation"] <- compilation
        record.["semanticCorrectness"] <- semantic
        record.["apiMisuse"] <- apiMisuse
        record.["consumerLoc"] <- jint loc

        let serialized = record.ToJsonString jsonOptions
        let recordPath = Path.Combine(runDir, "record.json")
        File.WriteAllText(recordPath, serialized + "\n", UTF8Encoding(false))
        stdout.WriteLine serialized

        let green = build.ExitCode = 0 && testOk = Some true

        if not green then
            stdout.WriteLine("--- build output (tail) ---")
            stdout.WriteLine(tailLines 40 buildOutput)

            if build.ExitCode = 0 then
                stdout.WriteLine("--- test output (tail) ---")
                stdout.WriteLine(tailLines 40 testOutput)

        stdout.WriteLine("VERDICT: " + (if green then "GREEN" else "RED"))
        if green then 0 else 1

// ---- aggregate -------------------------------------------------------------

let private pythonBool (value: bool) : string = if value then "True" else "False"

let private pythonBoolOption (value: bool option) : string =
    match value with
    | Some v -> pythonBool v
    | None -> "None"

let private stringValue (element: JsonElement) : string =
    match element.GetString() with
    | null -> ""
    | value -> value

let private recordPaths (repositoryRoot: string) : string list =
    let resultsRoot = Path.Combine(repositoryRoot, "eng", "evaluation", "results")

    if not (Directory.Exists resultsRoot) then
        []
    else
        [ for area in Directory.GetDirectories resultsRoot do
              for style in Directory.GetDirectories area do
                  for run in Directory.GetDirectories style do
                      if pathName(run).StartsWith("run-", StringComparison.Ordinal) then
                          let recordPath = Path.Combine(run, "record.json")

                          if File.Exists recordPath then
                              yield recordPath ]
        |> List.sortWith (fun left right -> String.CompareOrdinal(left, right))

let private runAggregate (repositoryRoot: string) (stdout: TextWriter) (outPath: string) : int =
    let rows =
        [ for recordPath in recordPaths repositoryRoot ->
              use document = JsonDocument.Parse(File.ReadAllText recordPath)
              let root = document.RootElement
              let compilation = root.GetProperty "compilation"
              let semantic = root.GetProperty "semanticCorrectness"
              let semanticOkElement = semantic.GetProperty "ok"

              let semanticOk =
                  match semanticOkElement.ValueKind with
                  | JsonValueKind.Null -> None
                  | JsonValueKind.True -> Some true
                  | JsonValueKind.False -> Some false
                  | _ -> failwith "semanticCorrectness.ok must be a boolean or null"

              let diagnostics =
                  root.GetProperty("apiMisuse").GetProperty("fsDiagnostics").EnumerateArray()
                  |> Seq.map stringValue
                  |> List.ofSeq

              ( stringValue (root.GetProperty "task"),
                stringValue (root.GetProperty "style"),
                stringValue (root.GetProperty "run"),
                compilation.GetProperty("ok").GetBoolean(),
                compilation.GetProperty("errors").GetInt32(),
                semanticOk,
                semantic.GetProperty("total").GetInt32(),
                semantic.GetProperty("failed").GetInt32(),
                diagnostics,
                root.GetProperty("consumerLoc").GetInt32() ) ]

    let lines = ResizeArray<string>()

    lines.Add "# Goal 21 evaluation aggregate"
    lines.Add ""
    lines.Add "Generated by `eng/evaluation/runner.py aggregate` from the recorded run records."
    lines.Add ""
    lines.Add "| task | style | run | compiled | errors | tests ok | total | failed | FS | LOC |"
    lines.Add "| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |"

    for task, style, run, compiled, errors, testsOk, testsTotal, testsFailed, diagnostics, loc in rows do
        let fs =
            match diagnostics with
            | [] -> "-"
            | values -> String.concat "," values

        lines.Add(
            sprintf
                "| %s | %s | %s | %s | %d | %s | %d | %d | %s | %d |"
                task
                style
                run
                (pythonBool compiled)
                errors
                (pythonBoolOption testsOk)
                testsTotal
                testsFailed
                fs
                loc
        )

    File.WriteAllText(outPath, String.Join("\n", lines) + "\n", UTF8Encoding(false))
    stdout.WriteLine(sprintf "wrote %s (%d runs)" outPath rows.Length)
    0

// ---- CLI -------------------------------------------------------------------

type private Command =
    | PrepFeed
    | Verify of task: string * style: string * runDir: string * roundNumber: int
    | Aggregate of outPath: string
    | Help

let private usageText =
    String.concat
        "\n"
        [ "usage: runner.py [-h] {prep-feed,verify,aggregate} ..."
          ""
          "Goal 21 model-agnostic coding evaluation runner."
          ""
          "commands:"
          "  prep-feed"
          "  verify    <task> <idiomatic|funnysharp> <run_dir> [--round ROUND_NUMBER]"
          "  aggregate <out>" ]

let private parsePositiveInt (value: string) : int option =
    match Int32.TryParse(value, Globalization.NumberStyles.AllowLeadingSign, Globalization.CultureInfo.InvariantCulture) with
    | true, parsed -> Some parsed
    | false, _ -> None

let private parseVerifyArgs (args: string list) : Result<Command, string> =
    let positionals = ResizeArray<string>()
    let mutable roundNumber = 1
    let mutable error : string option = None
    let mutable remaining = args

    while error.IsNone && not remaining.IsEmpty do
        match remaining with
        | "--round" :: value :: rest ->
            match parsePositiveInt value with
            | Some parsed ->
                roundNumber <- parsed
                remaining <- rest
            | None ->
                error <- Some(sprintf "argument --round: invalid int value: '%s'" value)

        | [ "--round" ] -> error <- Some "argument --round: expected one argument"
        | arg :: rest when arg.StartsWith("--round=", StringComparison.Ordinal) ->
            let value = arg.Substring "--round=".Length

            match parsePositiveInt value with
            | Some parsed ->
                roundNumber <- parsed
                remaining <- rest
            | None -> error <- Some(sprintf "argument --round: invalid int value: '%s'" value)

        | arg :: _ when arg.StartsWith("-", StringComparison.Ordinal) && arg <> "-" ->
            error <- Some("unrecognized arguments: " + arg)

        | value :: rest ->
            positionals.Add value
            remaining <- rest

        | [] -> ()

    match error with
    | Some message -> Error message
    | None ->
        match List.ofSeq positionals with
        | [ task; style; runDir ] ->
            if List.contains style styles then
                Ok(Verify(task, style, runDir, roundNumber))
            else
                Error(
                    sprintf
                        "argument style: invalid choice: '%s' (choose from 'idiomatic', 'funnysharp')"
                        style
                )
        | _ -> Error "the following arguments are required: task, style, run_dir"

let private parseArgs (argv: string list) : Result<Command, string> =
    match argv with
    | [] -> Error "the following arguments are required: command"
    | ("-h" | "--help") :: _ -> Ok Help
    | "prep-feed" :: [] -> Ok PrepFeed
    | "prep-feed" :: rest -> Error("unrecognized arguments: " + String.concat " " rest)
    | "verify" :: rest -> parseVerifyArgs rest
    | ("aggregate" :: outPath :: []) -> Ok(Aggregate outPath)
    | "aggregate" :: [] -> Error "the following arguments are required: out"
    | "aggregate" :: rest -> Error("unrecognized arguments: " + String.concat " " rest)
    | unknown :: _ -> Error(sprintf "argument command: invalid choice: '%s' (choose from 'prep-feed', 'verify', 'aggregate')" unknown)

/// Run the harness writing to the supplied writers, resolving every path from
/// <paramref name="repositoryRoot"/>. Returns 0 pass, 1 red/pack failure, 2 usage.
let mainWith
    (stdout: TextWriter)
    (stderr: TextWriter)
    (repositoryRoot: string)
    (argv: string list)
    : int =
    match parseArgs argv with
    | Error message ->
        stderr.WriteLine usageText
        stderr.WriteLine("runner.py: error: " + message)
        2
    | Ok Help ->
        stdout.WriteLine usageText
        0
    | Ok command ->
        let root = Path.GetFullPath repositoryRoot

        try
            match command with
            | PrepFeed -> runPrepFeed root stdout stderr
            | Verify(task, style, runDir, roundNumber) -> runVerify root stdout stderr task style runDir roundNumber
            | Aggregate outPath -> runAggregate root stdout outPath
            | Help -> 0
        with ex ->
            stderr.WriteLine ex.Message
            1

/// Entry point mirroring runner.py's main().
let main (argv: string array) : int =
    let root =
        match tryFindRoot () with
        | Some found -> found
        | None -> Environment.CurrentDirectory

    mainWith Console.Out Console.Error root (List.ofArray argv)
