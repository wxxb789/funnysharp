module FunnySharp.Harness.VerticalSlice

// Package-consumer verification for the ASP.NET Core vertical slice (Goal 22
// evidence): a behaviour-identical F# port of eng/tools/vertical_slice.py.
//
// Packs the solution, then restores, builds and tests the vertical-slice bundle
// against the packed .nupkg files only. Every step runs with an isolated
// NUGET_PACKAGES directory and the local feed listed before any remote feed, so
// a version drift cannot silently resolve to something else. The receipt is
// written to <output>/vertical-slice-results.json and the run exits 1 on any
// failed step.
//
// Exit codes: 0 pass, 1 check failure, 2 environment or usage failure.

open System
open System.Diagnostics
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open System.Text.RegularExpressions
open FunnySharp.Harness.Output
open FunnySharp.Harness.Proc

[<Literal>]
let private publicFeed = "https://api.nuget.org/v3/index.json"

/// One dotnet step may not run forever: a hung restore/build/test must fail the
/// receipt, not the tool. The default is 1800 s; tests lower it before a run.
let mutable stepTimeoutSeconds = 1800

/// The exit code a step reports when it exceeded <see cref="stepTimeoutSeconds"/>.
[<Literal>]
let stepTimeoutReturnCode = 124

let verifyMarker = "FunnySharp ASP.NET Core vertical slice endpoints mapped."
let baselineMarker = "Baseline (idiomatic C#) vertical slice endpoints mapped."
let measurementsMarker = "FunnySharp vertical slice measurements ready."

/// Package ids in the order their versions are reported. FunnySharp.AspNetCore
/// is matched before FunnySharp because its file name extends the shorter id.
let packageIds = [ "FunnySharp"; "FunnySharp.AspNetCore" ]

let private projectPath (parts: string list) : string =
    Path.Combine(List.toArray parts)

let private sliceRoot = [ "tests"; "FunnySharp.VerticalSlice" ]

let private apiProject =
    projectPath (sliceRoot @ [ "FunnySharp.VerticalSlice.Api"; "FunnySharp.VerticalSlice.Api.csproj" ])

let private testProject =
    projectPath (sliceRoot @ [ "FunnySharp.VerticalSlice.Tests"; "FunnySharp.VerticalSlice.Tests.csproj" ])

let private baselineProject =
    projectPath (sliceRoot @ [ "FunnySharp.VerticalSlice.Baseline"; "FunnySharp.VerticalSlice.Baseline.csproj" ])

let private measurementsProject =
    projectPath
        (sliceRoot @ [ "FunnySharp.VerticalSlice.Measurements"; "FunnySharp.VerticalSlice.Measurements.csproj" ])

// ---- dotnet discovery ----

let private executableNames (name: string) : string list =
    if OperatingSystem.IsWindows() then [ name + ".exe"; name + ".cmd"; name + ".bat" ]
    else [ name ]

let private findOnPath (name: string) : string option =
    match Environment.GetEnvironmentVariable "PATH" with
    | null -> None
    | pathVariable ->
        pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
        |> Array.collect (fun directory ->
            executableNames name
            |> List.map (fun fileName -> Path.Combine(directory, fileName))
            |> List.toArray)
        |> Array.tryFind File.Exists

/// The dotnet entry point, preferring an explicit DOTNET_ROOT installation and
/// falling back to ~/.dotnet, mirroring vertical_slice.py's dotnet().
let findDotnet () : string option =
    match findOnPath "dotnet" with
    | Some path -> Some path
    | None ->
        let fromRoot =
            match Environment.GetEnvironmentVariable "DOTNET_ROOT" with
            | null -> None
            | dotnetRoot ->
                let candidate = Path.Combine(dotnetRoot, "dotnet")
                if File.Exists candidate then Some candidate else None

        match fromRoot with
        | Some path -> Some path
        | None ->
            let home = Environment.GetFolderPath Environment.SpecialFolder.UserProfile
            let userInstall = Path.Combine(home, ".dotnet", "dotnet")
            if File.Exists userInstall then Some userInstall else None

// ---- process runner ----

let private tail (length: int) (text: string) : string =
    if text.Length <= length then text else text.Substring(text.Length - length)

/// Run one dotnet step with captured output and a hard timeout. A hung step is
/// killed and reported as return code 124 with a "timed out" stderr, so the tool
/// fails rather than hanging.
let runStepProcess
    (workingDirectory: string)
    (environment: (string * string) list)
    (timeoutSeconds: int)
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

        for arg in args do
            info.ArgumentList.Add arg

        for key, value in environment do
            info.Environment.[key] <- value

        proc.StartInfo <- info

        if not (proc.Start()) then
            failwithf "failed to start process '%s'" exe

        let stdoutTask = proc.StandardOutput.ReadToEndAsync()
        let stderrTask = proc.StandardError.ReadToEndAsync()

        if proc.WaitForExit(timeoutSeconds * 1000) then
            { ExitCode = proc.ExitCode
              Stdout = stdoutTask.GetAwaiter().GetResult()
              Stderr = stderrTask.GetAwaiter().GetResult() }
        else
            (try
                proc.Kill true
             with _ ->
                ())

            proc.WaitForExit 5000 |> ignore

            let stdout =
                try
                    stdoutTask.GetAwaiter().GetResult()
                with _ ->
                    ""

            { ExitCode = stepTimeoutReturnCode
              Stdout = stdout
              Stderr = sprintf "step timed out after %ds" timeoutSeconds }

/// The step runner mainWith uses. Tests replace this to inject canned results.
let mutable stepRunner =
    fun (workingDirectory: string) (environment: (string * string) list) (timeoutSeconds: int) (command: string list) ->
        runStepProcess workingDirectory environment timeoutSeconds command

// ---- feed inspection ----

let private versionShape = Regex @"^\d+\.\d+\.\d+[A-Za-z0-9.\-+]*$"

let private sortedPackages (feed: string) : string list =
    Directory.GetFiles(feed, "*.nupkg")
    |> Array.map (fun path ->
        match Path.GetFileName path with
        | null -> path
        | name -> name)
    |> Array.sortWith (fun left right -> String.CompareOrdinal(left, right))
    |> List.ofArray

/// The package ids ordered longest-first, so FunnySharp.AspNetCore matches before
/// FunnySharp for a file named FunnySharp.AspNetCore.<version>.nupkg.
let private packageIdMatchOrder =
    packageIds |> List.sortWith (fun left right -> compare right.Length left.Length)

/// Reads the version of the single package each id has in the feed. The feed is
/// an output directory that gets reused, so more than one version for an id fails
/// closed instead of silently picking either of them.
let packageVersions (feed: string) : Result<Map<string, string>, HarnessError> =
    let found = System.Collections.Generic.Dictionary<string, ResizeArray<string>>()

    for name in sortedPackages feed do
        let mutable matched = false

        for packageId in packageIdMatchOrder do
            if not matched then
                let prefix = packageId + "."

                if name.StartsWith(prefix, StringComparison.Ordinal) then
                    let endIndex = name.Length - ".nupkg".Length

                    let version =
                        if endIndex >= prefix.Length then name.Substring(prefix.Length, endIndex - prefix.Length)
                        else ""

                    if versionShape.IsMatch version then
                        match found.TryGetValue packageId with
                        | true, candidates -> candidates.Add version
                        | false, _ ->
                            let candidates = ResizeArray<string>()
                            candidates.Add version
                            found.[packageId] <- candidates

                        matched <- true

    let versions = ResizeArray<string * string>()

    let mutable failure : HarnessError option = None

    for packageId in packageIds do
        if failure.IsNone then
            match found.TryGetValue packageId with
            | false, _ -> failure <- Some(Errors.create (sprintf "%s is missing a package for: %s." feed packageId))
            | true, candidates ->
                if candidates.Count > 1 then
                    let ordered =
                        candidates |> List.ofSeq |> List.sortWith (fun l r -> String.CompareOrdinal(l, r))

                    failure <-
                        Some(
                            Errors.create (
                                sprintf
                                    "%s holds more than one %s version (%s); a feed must describe one run's output -- let the tool pack (which clears the feed first), or pass --no-pack with a feed that holds a single version per package."
                                    feed
                                    packageId
                                    (String.concat ", " ordered)
                            )
                        )
                else
                    versions.Add(packageId, candidates.[0])

    match failure with
    | Some err -> Error err
    | None -> Ok(Map.ofSeq versions)

/// The sorted {file, sha256} fingerprint of every package in the feed.
let fingerprint (feed: string) : (string * string) list =
    [ for name in sortedPackages feed ->
          use stream = File.OpenRead(Path.Combine(feed, name))
          use sha = SHA256.Create()
          let digest = sha.ComputeHash stream |> Array.map (fun b -> b.ToString "x2") |> String.concat ""
          name, digest ]

// ---- test summary parsing ----

// "Test run summary" / "测试运行摘要" (the latter written as \u escapes to keep this
// file ASCII). Microsoft.Testing.Platform prints the marker followed by total:/
// failed:/succeeded:/skipped: fields, localized on non-English hosts.
let private summaryMarker = Regex @"(?:Test run summary|\u6D4B\u8BD5\u8FD0\u884C\u6458\u8981)"

let private fieldPattern (alternation: string) : Regex =
    Regex(@"(?:" + alternation + @")\s*[:\uFF1A]\s*(\d+)", RegexOptions.IgnoreCase)

let private parseFields =
    [ "total", fieldPattern @"Total|\u603B\u8BA1"
      "failed", fieldPattern @"Failed|\u5931\u8D25"
      "passed", fieldPattern @"Passed|Succeeded|\u6210\u529F"
      "skipped", fieldPattern @"Skipped|\u5DF2\u8DF3\u8FC7" ]

/// Parse the last test-run summary in a stream, accepting the English and Chinese
/// field shapes. A summary missing a field is unparsable, never partially trusted.
let parseSummary (output: string) : Map<string, int> option =
    let matches = summaryMarker.Matches output

    if matches.Count = 0 then
        None
    else
        let last = matches.[matches.Count - 1]
        let text = output.Substring(last.Index + last.Length)
        let mutable result : Map<string, int> option = Some Map.empty

        for name, pattern in parseFields do
            match result with
            | None -> ()
            | Some current ->
                let found = pattern.Match text

                if not found.Success then result <- None
                else result <- Some(current.Add(name, Int32.Parse found.Groups.[1].Value))

        result

// Microsoft.Testing.Platform prints one line per failed test ahead of the summary, as
// "failed <name> (21ms)" or its Chinese form (written as \u escapes to keep this file
// ASCII). A summary field line ("failed: 1") has no parenthesized duration, so it never
// matches. The receipt keeps only a tail of the runner output, which is exactly where the
// failing identities used to get lost.
let private failingTestLine =
    Regex(@"^\s*(?:failed|\u5931\u8D25)\s+(?<name>[^\r\n(]+?)\s*\(\d", RegexOptions.IgnoreCase)

/// Extract the identities of the tests the runner reported as failed, in first-seen order.
let parseFailingTests (output: string) : string list =
    output.Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)
    |> Array.choose (fun line ->
        let found = failingTestLine.Match line

        if found.Success then
            Some(found.Groups.["name"].Value.Trim())
        else
            None)
    |> Array.distinct
    |> List.ofArray

// ---- CLI ----

type private CliOptions =
    { Output: string
      Feed: string option
      NoPack: bool
      SkipTests: bool
      SkipMeasurements: bool
      Json: bool
      RepositoryRoot: string option
      Help: bool }

let private defaultOptions =
    { Output = "artifacts/vertical-slice/consumer-run"
      Feed = None
      NoPack = false
      SkipTests = false
      SkipMeasurements = false
      Json = false
      RepositoryRoot = None
      Help = false }

let private usageText =
    String.concat
        "\n"
        [ "usage: vertical_slice.py [-h] [--output DIR] [--feed DIR] [--no-pack] [--skip-tests]"
          "                            [--skip-measurements] [--json] [--repository-root PATH]" ]

let private parseArgs (argv: string list) : Result<CliOptions, string> =
    let rec loop (options: CliOptions) (remaining: string list) =
        match remaining with
        | [] -> Ok options
        | ("-h" | "--help") :: _ -> Ok { options with Help = true }
        | "--no-pack" :: rest -> loop { options with NoPack = true } rest
        | "--skip-tests" :: rest -> loop { options with SkipTests = true } rest
        | "--skip-measurements" :: rest -> loop { options with SkipMeasurements = true } rest
        | "--json" :: rest -> loop { options with Json = true } rest
        | "--output" :: value :: rest -> loop { options with Output = value } rest
        | [ "--output" ] -> Error "argument --output: expected one argument"
        | "--feed" :: value :: rest -> loop { options with Feed = Some value } rest
        | [ "--feed" ] -> Error "argument --feed: expected one argument"
        | "--repository-root" :: value :: rest -> loop { options with RepositoryRoot = Some value } rest
        | [ "--repository-root" ] -> Error "argument --repository-root: expected one argument"
        | arg :: rest when arg.StartsWith("--output=", StringComparison.Ordinal) ->
            loop { options with Output = arg.Substring "--output=".Length } rest
        | arg :: rest when arg.StartsWith("--feed=", StringComparison.Ordinal) ->
            loop { options with Feed = Some(arg.Substring "--feed=".Length) } rest
        | arg :: rest when arg.StartsWith("--repository-root=", StringComparison.Ordinal) ->
            loop { options with RepositoryRoot = Some(arg.Substring "--repository-root=".Length) } rest
        | arg :: _ -> Error("unrecognized arguments: " + arg)

    loop defaultOptions argv

// ---- receipt encoding ----

let private jsonOptions = JsonSerializerOptions(WriteIndented = true)

let private jstr (value: string) : JsonValue =
    Option.ofObj (JsonValue.Create value)
    |> Option.defaultWith (fun () -> failwith "JsonValue.Create returned null for a string")

let private jint (value: int) : JsonValue = JsonValue.Create value

let private jbool (value: bool) : JsonValue = JsonValue.Create value

/// F# cannot pass a literal null to the JsonObject setter; an uninitialized node
/// serializes as JSON null, matching Python's `testSummary: null`.
let private jsonNull : JsonNode = Unchecked.defaultof<JsonNode>

let private stringArray (values: string list) : JsonNode =
    let array = JsonArray()
    for value in values do
        array.Add(jstr value)
    array :> JsonNode

let private summaryJson (counts: Map<string, int>) : JsonNode =
    let node = JsonObject()
    for name in [ "total"; "failed"; "passed"; "skipped" ] do
        node.[name] <- jint counts.[name]
    node :> JsonNode

let private formatVersions (versions: Map<string, string>) : string =
    let entries = packageIds |> List.map (fun packageId -> sprintf "'%s': '%s'" packageId versions.[packageId])
    "{" + String.concat ", " entries + "}"

/// Run the verification writing to the supplied writers. Returns 0 pass, 1 check
/// failure, 2 environment or usage failure.
let mainWith (stdout: TextWriter) (stderr: TextWriter) (argv: string list) : int =
    match parseArgs argv with
    | Error message ->
        stderr.WriteLine usageText
        stderr.WriteLine("vertical_slice.py: error: " + message)
        2
    | Ok options when options.Help ->
        stdout.WriteLine usageText
        0
    | Ok options ->
        let repositoryRoot =
            match options.RepositoryRoot with
            | Some root -> Ok(Path.GetFullPath root)
            | None ->
                match Repo.tryFindRoot () with
                | Some root -> Ok root
                | None ->
                    Error(
                        Errors.withExitCode
                            2
                            (sprintf "%s is not inside a git repository." Environment.CurrentDirectory)
                    )

        match repositoryRoot with
        | Error err ->
            stderr.WriteLine("error: " + err.Message)
            err.ExitCode
        | Ok root ->
            let outputDirectory = Path.GetFullPath(Path.Combine(root, options.Output))

            let feedDirectory =
                match options.Feed with
                | Some feed -> Path.GetFullPath(Path.Combine(root, feed))
                | None -> Path.Combine(outputDirectory, "feed")

            let packagesDirectory = Path.Combine(outputDirectory, "nuget-packages")
            Directory.CreateDirectory outputDirectory |> ignore
            Directory.CreateDirectory feedDirectory |> ignore
            Directory.CreateDirectory packagesDirectory |> ignore

            match findDotnet () with
            | None ->
                stderr.WriteLine("error: dotnet was not found on PATH or under DOTNET_ROOT.")
                2
            | Some dotnetPath ->
                let childEnvironment =
                    [ "DOTNET_CLI_TELEMETRY_OPTOUT", "1"
                      "DOTNET_NOLOGO", "1"
                      "DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "1"
                      "NUGET_PACKAGES", packagesDirectory ]

                let receipt = JsonObject()
                receipt.["schemaVersion"] <- jint 1
                receipt.["objective"] <- jstr "docs/goals/archive/0022-goal.md"
                receipt.["configuration"] <- jstr "Release"
                receipt.["output"] <- jstr outputDirectory
                receipt.["feed"] <- jstr feedDirectory
                receipt.["nugetPackages"] <- jstr packagesDirectory
                let steps = JsonArray()
                receipt.["steps"] <- steps
                let failures = ResizeArray<string>()

                let record (step: string) (command: string list) (result: ProcessResult) =
                    let entry = JsonObject()
                    entry.["step"] <- jstr step
                    entry.["command"] <- stringArray command
                    entry.["exitCode"] <- jint result.ExitCode
                    entry.["stdoutTail"] <- jstr (tail 8000 result.Stdout)
                    entry.["stderrTail"] <- jstr (tail 4000 result.Stderr)
                    steps.Add(entry :> JsonNode)

                    if result.ExitCode <> 0 then
                        failures.Add(sprintf "%s exited %d" step result.ExitCode)

                if not options.NoPack then
                    // The feed is an output directory that may be reused across runs;
                    // clearing it keeps the receipt about the packages this run produced.
                    for stale in Directory.GetFiles(feedDirectory, "*.nupkg") do
                        File.Delete stale

                    let command =
                        [ dotnetPath; "pack"; "FunnySharp.slnx"; "-c"; "Release"; "-o"; feedDirectory ]

                    let result = stepRunner root childEnvironment stepTimeoutSeconds command
                    record "pack" command result

                    if result.ExitCode <> 0 then
                        stderr.WriteLine(tail 4000 result.Stdout)
                        stderr.WriteLine(tail 4000 result.Stderr)

                match packageVersions feedDirectory with
                | Error err ->
                    stderr.WriteLine("error: " + err.Message)
                    err.ExitCode
                | Ok versions ->
                    let packagesJson = JsonArray()

                    for name, digest in fingerprint feedDirectory do
                        let row = JsonObject()
                        row.["file"] <- jstr name
                        row.["sha256"] <- jstr digest
                        packagesJson.Add(row :> JsonNode)

                    receipt.["packages"] <- packagesJson
                    let versionsJson = JsonObject()

                    for packageId in packageIds do
                        versionsJson.[packageId] <- jstr versions.[packageId]

                    receipt.["packageVersions"] <- versionsJson

                    let versionArgs =
                        [ sprintf "-p:FunnySharpPackageVersion=%s" versions.["FunnySharp"]
                          sprintf "-p:FunnySharpAspNetCorePackageVersion=%s" versions.["FunnySharp.AspNetCore"] ]

                    let projects =
                        [ "api", apiProject
                          "tests", testProject
                          "baseline", baselineProject
                          "measurements", measurementsProject ]

                    for name, project in projects do
                        let command =
                            [ dotnetPath; "restore"; project; "--source"; feedDirectory; "--source"; publicFeed ]
                            @ versionArgs

                        record (sprintf "restore-%s" name) command (stepRunner root childEnvironment stepTimeoutSeconds command)

                    for name, project in projects do
                        let command =
                            [ dotnetPath; "build"; project; "-c"; "Release"; "--no-restore" ] @ versionArgs

                        record (sprintf "build-%s" name) command (stepRunner root childEnvironment stepTimeoutSeconds command)

                    let verify =
                        [ dotnetPath
                          "run"
                          "--project"
                          apiProject
                          "-c"
                          "Release"
                          "--no-build"
                          "--no-restore" ]
                        @ versionArgs
                        @ [ "--"; "--verify" ]

                    let verifyResult = stepRunner root childEnvironment stepTimeoutSeconds verify
                    record "api-verify" verify verifyResult
                    receipt.["verifyMarker"] <- jbool (verifyResult.Stdout.Contains verifyMarker)

                    if not (verifyResult.Stdout.Contains verifyMarker) then
                        failures.Add "api-verify did not print its success marker"

                    let baselineVerify =
                        [ dotnetPath
                          "run"
                          "--project"
                          baselineProject
                          "-c"
                          "Release"
                          "--no-build"
                          "--no-restore"
                          "--"
                          "--verify" ]

                    let baselineResult = stepRunner root childEnvironment stepTimeoutSeconds baselineVerify
                    record "baseline-verify" baselineVerify baselineResult
                    receipt.["baselineVerifyMarker"] <- jbool (baselineResult.Stdout.Contains baselineMarker)

                    if not (baselineResult.Stdout.Contains baselineMarker) then
                        failures.Add "baseline-verify did not print its success marker"

                    let measurementsVerify =
                        [ dotnetPath
                          "run"
                          "--project"
                          measurementsProject
                          "-c"
                          "Release"
                          "--no-build"
                          "--no-restore" ]
                        @ versionArgs
                        @ [ "--"; "--verify" ]

                    let measurementsVerifyResult = stepRunner root childEnvironment stepTimeoutSeconds measurementsVerify
                    record "measurements-verify" measurementsVerify measurementsVerifyResult
                    receipt.["measurementsVerifyMarker"] <- jbool (measurementsVerifyResult.Stdout.Contains measurementsMarker)

                    if not (measurementsVerifyResult.Stdout.Contains measurementsMarker) then
                        failures.Add "measurements-verify did not print its success marker"

                    if not options.SkipMeasurements then
                        let measurementsPath = Path.Combine(outputDirectory, "measurements.json")

                        let measurements =
                            [ dotnetPath
                              "run"
                              "--project"
                              measurementsProject
                              "-c"
                              "Release"
                              "--no-build"
                              "--no-restore" ]
                            @ versionArgs
                            @ [ "--"; "--output"; measurementsPath ]

                        let measurementsResult = stepRunner root childEnvironment stepTimeoutSeconds measurements
                        record "measurements" measurements measurementsResult

                        if measurementsResult.ExitCode <> 0 then
                            failures.Add
                                "measurements reported a behavioral difference between the slice and the comparison app"
                        else
                            receipt.["measurementsPath"] <- jstr measurementsPath

                    if not options.SkipTests then
                        let test =
                            [ dotnetPath; "test"; testProject; "-c"; "Release"; "--no-build" ] @ versionArgs

                        let testResult = stepRunner root childEnvironment stepTimeoutSeconds test
                        record "consumer-tests" test testResult
                        let failingTests = parseFailingTests (testResult.Stdout + testResult.Stderr)
                        let summary = parseSummary (testResult.Stdout + testResult.Stderr)
                        receipt.["testSummary"] <- (match summary with Some counts -> summaryJson counts | None -> jsonNull)
                        receipt.["testFailures"] <- stringArray failingTests

                        match summary with
                        | None -> failures.Add "consumer-tests produced no parsable test summary"
                        | Some counts ->
                            if counts.["failed"] <> 0 then
                                let named =
                                    if failingTests.IsEmpty then
                                        ""
                                    else
                                        ": " + String.concat ", " failingTests

                                failures.Add(sprintf "consumer-tests reported %d failures%s" counts.["failed"] named)

                            if counts.["skipped"] <> 0 then
                                failures.Add(sprintf "consumer-tests reported %d skipped tests" counts.["skipped"])

                            if counts.["passed"] = 0 then
                                failures.Add "consumer-tests ran no passing test"

                    receipt.["status"] <- jstr (if failures.Count > 0 then "fail" else "pass")
                    receipt.["failures"] <- stringArray (List.ofSeq failures)

                    let receiptPath = Path.Combine(outputDirectory, "vertical-slice-results.json")

                    File.WriteAllText(receiptPath, receipt.ToJsonString(jsonOptions) + "\n", UTF8Encoding(false))

                    if options.Json then
                        stdout.WriteLine(receipt.ToJsonString jsonOptions)
                    else
                        stdout.WriteLine("packages: " + formatVersions versions)
                        stdout.WriteLine("receipt:  " + receiptPath)

                        for failure in failures do
                            stderr.WriteLine("FAIL: " + failure)

                        stdout.WriteLine(
                            "vertical slice consumer verification: "
                            + (if failures.Count > 0 then "FAIL" else "PASS")
                        )

                    if failures.Count > 0 then 1 else 0

/// Entry point mirroring vertical_slice.py's main().
let main (argv: string array) : int =
    mainWith Console.Out Console.Error (List.ofArray argv)
