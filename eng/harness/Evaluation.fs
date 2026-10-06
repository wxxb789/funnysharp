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
// caches. Historical results are read-only. New verification attempts reserve
// immutable round directories; --study binds fresh independent generation inputs.

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

let private runPrepFeed (repositoryRoot: string) (stdout: TextWriter) (stderr: TextWriter) (feedOverride: string option) : int =
    let feedDir = feedOverride |> Option.defaultValue (Path.Combine(repositoryRoot, "artifacts", "evaluation", "feed"))
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
        @"total:\s*(\d+)\s+failed:\s*(\d+)\s+succeeded:\s*(\d+)\s+skipped:\s*(\d+)",
        RegexOptions.Singleline
    )

let private writeNugetConfig (repositoryRoot: string) (buildDir: string) (upstreamFeed: string option) : unit =
    let feedDir = Path.Combine(repositoryRoot, "artifacts", "evaluation", "feed")
    let relativeFeed = Path.GetRelativePath(buildDir, feedDir).Replace('\\', '/')
    let upstreamAttribute =
        System.Xml.Linq.XAttribute(
            System.Xml.Linq.XName.Get "value",
            upstreamFeed |> Option.defaultValue "https://api.nuget.org/v3/index.json"
        ).ToString()
    let selectedSources =
        match upstreamFeed with
        | None -> ""
        | Some _ ->
            "  <disabledPackageSources>\n"
            + "    <clear />\n"
            + "  </disabledPackageSources>\n"

    let content =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
        + "<configuration>\n"
        + "  <packageSources>\n"
        + "    <clear />\n"
        + sprintf "    <add key=\"evaluation-feed\" value=\"%s\" />\n" relativeFeed
        + "    <add key=\"nuget.org\" " + upstreamAttribute + " />\n"
        + "  </packageSources>\n"
        + selectedSources
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

// Receipt hashes cover bytes, not deserialized/reformatted JSON. FileMode.CreateNew
// is also the reservation mechanism: an interrupted attempt is retained, never reused.
let private hashFile (path: string) =
    use stream = File.OpenRead path
    SHA256.HashData stream |> Convert.ToHexString |> fun value -> value.ToLowerInvariant()

let private writeNew (path: string) (text: string) =
    use stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)
    use writer = new StreamWriter(stream, UTF8Encoding(false))
    writer.Write text

let private requiredNode (value: JsonNode | null) : JsonNode =
    match value with
    | null -> invalidArg "JSON" "required JSON node is missing"
    | value -> value

let private field (node: JsonNode) (name: string) =
    match node.[name] with
    | null -> invalidArg name (name + " is required")
    | value -> value

let private parentDirectory (path: string) =
    match Path.GetDirectoryName path with
    | null -> invalidArg "path" "path has no parent directory"
    | directory -> directory

let private xmlAttribute (element: System.Xml.Linq.XElement) (name: string) =
    match element.Attribute(System.Xml.Linq.XName.Get name) with
    | null -> invalidArg name (name + " attribute is missing")
    | attribute -> attribute.Value

let private readJson (path: string) =
    (JsonNode.Parse(File.ReadAllText path) |> requiredNode).AsObject()

let private textField (node: JsonNode | null) (name: string) =
    let value = (field (requiredNode node) name).GetValue<string>()
    if String.IsNullOrWhiteSpace value then invalidArg name (name + " is required")
    value

let private filesReceipt (directory: string) =
    let result = JsonArray()
    for path in Directory.GetFiles(directory, "*", SearchOption.AllDirectories) |> Array.sort do
        let file = JsonObject()
        file.["path"] <- jstr (Path.GetRelativePath(directory, path).Replace('\\', '/'))
        file.["sha256"] <- jstr (hashFile path)
        result.Add file
    result

let private checkFiles (directory: string) (files: JsonArray) =
    let actual = filesReceipt directory
    if actual.ToJsonString() <> files.ToJsonString() then
        invalidArg "receipt" ("files changed in " + directory)

let private studyRoot repositoryRoot study =
    if study <> "audit-resolution-v1" && study <> "audit-resolution-v2" && study <> "audit-resolution-v3" && study <> "audit-resolution-v4" && study <> "audit-resolution-v5" && study <> "audit-resolution-v6" then invalidArg "study" "unknown study"
    Path.Combine(repositoryRoot, "eng", "evaluation", "studies", study)

let private upstreamPackageFeed (plan: JsonObject) =
    match plan.["upstreamPackageFeed"] with
    | null -> None
    | value -> Some(value.GetValue<string>())

// Freeze is an explicit pre-generation action on the existing prep-feed route.
// Neither historical tasks nor their prompts/results are modified. Only a curated
// allowlist is copied; the producer is not given the repository or parent context.
let private freezeStudy repositoryRoot study (feed: string) =
    let root = studyRoot repositoryRoot study
    let plan = readJson (Path.Combine(root, "plan.json"))
    let upstreamFeed = upstreamPackageFeed plan
    let snapshot = Path.Combine(root, "snapshot")
    if Directory.Exists snapshot then invalidArg "study" "snapshot already exists; retain it"
    Directory.CreateDirectory snapshot |> ignore
    writeNew (Path.Combine(snapshot, "freeze.started.json")) (plan.ToJsonString jsonOptions)
    let copy (source: string) (target: string) =
        Directory.CreateDirectory(parentDirectory target) |> ignore
        File.Copy(source, target, false)
    for guide in (field plan "guides").AsArray() do
        let relative = (requiredNode guide).GetValue<string>()
        copy (Path.Combine(repositoryRoot, relative)) (Path.Combine(snapshot, "guides", relative))
    for relative in [ "global.json"; "Directory.Build.props"; "build.fsx"; "eng/harness/Evaluation.fs" ] do
        copy (Path.Combine(repositoryRoot, relative)) (Path.Combine(snapshot, "environment", relative))
    let packages = sortedNupkgs feed
    if packages.Length <> 2 then invalidArg "feed" "freeze requires exactly the two candidate packages"
    let versions =
        packages |> List.map (fun name ->
            let m = nupkgName.Match name
            if not m.Success then invalidArg "feed" "invalid package name"
            m.Groups.["id"].Value, m.Groups.["version"].Value) |> Map.ofList
    if not (versions.ContainsKey "FunnySharp" && versions.ContainsKey "FunnySharp.AspNetCore") then
        invalidArg "feed" "wrong package identities"
    for package in packages do copy (Path.Combine(feed, package)) (Path.Combine(snapshot, "feed", package))
    for taskNode in (field plan "tasks").AsArray() do
        let task = (requiredNode taskNode).GetValue<string>()
        let historical = Path.Combine(repositoryRoot, "eng", "evaluation", "tasks", task)
        let destination = Path.Combine(snapshot, "tasks", task)
        let proposed = Path.Combine(root, "tasks", task)
        let proposedTests = Path.Combine(proposed, "tests")
        let trusted = Directory.GetFiles(Path.Combine(historical, "tests"), "*.cs")
        for file in trusted do
            let replacement = Path.Combine(proposedTests, pathName file)
            copy (if File.Exists replacement then replacement else file) (Path.Combine(destination, "tests", pathName file))
        if Directory.Exists proposedTests then
            for file in Directory.GetFiles(proposedTests, "*.cs") do
                let target = Path.Combine(destination, "tests", pathName file)
                if not (File.Exists target) then copy file target
        let brief =
            if File.Exists(Path.Combine(proposed, "brief.md")) then File.ReadAllText(Path.Combine(proposed, "brief.md"))
            else
                let prompt = File.ReadAllText(Path.Combine(historical, "prompt-idiomatic.md"))
                let index = prompt.IndexOf("## Style:", StringComparison.Ordinal)
                if index < 0 then invalidArg "prompt" "missing style boundary"
                prompt.Substring(0, index)
        for style in styles do
            let directive =
                if style = "idiomatic" then "Use idiomatic C#, .NET 10 BCL only."
                else "Use the supplied FunnySharp packages and public-guide snapshot. Ordinary C# remains available."
            writeNew (Path.Combine(destination, "prompt-" + style + ".md"))
                (brief + "\n## Style\n\n" + directive + "\n\nDeliver C# files only in solution/. Do not read repository source, historical solutions, audits, plans or other sessions.\n")
            for file in Directory.GetFiles(Path.Combine(historical, "template-" + style)) do
                let target = Path.Combine(destination, "template-" + style, pathName file)
                Directory.CreateDirectory(parentDirectory target) |> ignore
                let mutable template = File.ReadAllText file
                for KeyValue(id, version) in versions do
                    template <- template.Replace(sprintf "Include=\"%s\" Version=\"0.1.0\"" id,
                                                 sprintf "Include=\"%s\" Version=\"%s\"" id version)
                writeNew target template
    // Freeze package locks before producers see the kits. These are protocol
    // preparation processes, not generation/correction rounds.
    let cacheRoot = Path.Combine(repositoryRoot, "artifacts", "evaluation", "freeze-" + Guid.NewGuid().ToString("N"))
    let environment = childEnvironment repositoryRoot |> List.map (fun (key, value) ->
        if key = "NUGET_PACKAGES" || key = "NUGET_HTTP_CACHE_PATH" then key, Path.Combine(cacheRoot, key)
        else key, value)
    let preparation (directory: string) (name: string) command =
        let config = Path.Combine(directory, "NuGet.config")
        if File.Exists config then
            let xml = System.Xml.Linq.XDocument.Load config
            let add = xml.Descendants(System.Xml.Linq.XName.Get "add") |> Seq.find (fun node -> xmlAttribute node "key" = "evaluation-feed")
            add.SetAttributeValue(System.Xml.Linq.XName.Get "value", Path.Combine(snapshot, "feed"))
            xml.Save config
        let result = childRunner directory environment command
        writeNew (Path.Combine(directory, name + ".stdout.log")) result.Stdout
        writeNew (Path.Combine(directory, name + ".stderr.log")) result.Stderr
        let processReceipt = JsonObject()
        processReceipt.["command"] <- JsonSerializer.SerializeToNode(command)
        processReceipt.["environment"] <- JsonSerializer.SerializeToNode(environment)
        processReceipt.["workingDirectory"] <- jstr directory
        processReceipt.["exitCode"] <- jint result.ExitCode
        writeNew (Path.Combine(directory, name + ".process.json")) (processReceipt.ToJsonString jsonOptions + "\n")
        result
    let sdk = preparation snapshot "sdk" [ "dotnet"; "--info" ]
    if sdk.ExitCode <> 0 then invalidArg "SDK" "SDK environment capture failed"
    for taskNode in (field plan "tasks").AsArray() do
        for style in styles do
            let kit = Path.Combine(snapshot, "tasks", (requiredNode taskNode).GetValue<string>(), "template-" + style)
            writeNugetConfig repositoryRoot kit upstreamFeed
            copy (Path.Combine(repositoryRoot, "Directory.Build.props")) (Path.Combine(kit, "Directory.Build.props"))
            let result = preparation kit "restore" [ "dotnet"; "restore"; "--use-lock-file" ]
            if result.ExitCode <> 0 then invalidArg "freeze" "kit restore failed; retain interrupted snapshot"
    let control = Path.Combine(snapshot, "analyzer-control")
    Directory.CreateDirectory control |> ignore
    writeNew (Path.Combine(control, "Control.csproj"))
        ("<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup><ItemGroup><PackageReference Include=\"FunnySharp\" Version=\"" + versions.["FunnySharp"] + "\" /></ItemGroup></Project>")
    writeNew (Path.Combine(control, "Control.cs")) "public static class Control { public static FunnySharp.Result<int,string> Invalid() => default(FunnySharp.Result<int,string>); }\n"
    writeNugetConfig repositoryRoot control upstreamFeed
    let result = preparation control "control" [ "dotnet"; "build"; "-p:ErrorLog=control.sarif"; "-bl:control.binlog" ]
    if not (Regex.IsMatch(result.Stdout + result.Stderr, @"\bFS1001\b")) then
        invalidArg "freeze" "same-package FS1001 control missing; retain failed snapshot"
    let manifest = JsonObject()
    manifest.["schema"] <- jstr "funnysharp-evaluation-study/v1"
    manifest.["study"] <- jstr study
    manifest.["condition"] <- jstr "unassisted-public-guides"
    manifest.["planSha256"] <- jstr (hashFile (Path.Combine(root, "plan.json")))
    manifest.["files"] <- filesReceipt snapshot
    manifest.["expectedTests"] <- (field plan "expectedTests").DeepClone()
    manifest.["cohort"] <- (field plan "cohort").DeepClone()
    writeNew (Path.Combine(root, "manifest.json")) (manifest.ToJsonString jsonOptions + "\n")

type private StudyBinding =
    { TaskRoot: string
      Snapshot: string
      ManifestHash: string
      ExpectedTests: int
      ProducerPath: string }

// Only use after checkFiles has verified this snapshot in the current pre-child phase.
let private snapshotHash (manifest: JsonObject) relative =
    (field manifest "files").AsArray()
    |> Seq.find (fun file -> textField file "path" = relative)
    |> fun file -> textField file "sha256"

let private loadStudy repositoryRoot study =
    let root = studyRoot repositoryRoot study
    let manifestPath = Path.Combine(root, "manifest.json")
    let manifest = readJson manifestPath
    let snapshot = Path.Combine(root, "snapshot")
    checkFiles snapshot ((field manifest "files").AsArray())
    for relative in [ "global.json"; "Directory.Build.props"; "build.fsx"; "eng/harness/Evaluation.fs" ] do
        if hashFile (Path.Combine(repositoryRoot, relative)) <> snapshotHash manifest ("environment/" + relative) then
            invalidArg "environment" "runner/build configuration changed after freeze"
    if not (File.Exists(Path.Combine(snapshot, "analyzer-control", "control.sarif"))) then
        invalidArg "analyzer" "frozen negative control is missing"
    if hashFile (Path.Combine(root, "plan.json")) <> textField manifest "planSha256" then
        invalidArg "study" "study plan changed"
    root, manifest, snapshot

let private bindStudy repositoryRoot study task style runDir roundNumber previous =
    let root, manifest, snapshot = loadStudy repositoryRoot study
    let manifestPath = Path.Combine(root, "manifest.json")
    let results = Path.Combine(repositoryRoot, "eng", "evaluation", "results", study)
    let expectedRun = Path.GetFullPath(Path.Combine(results, task, style, pathName runDir))
    if not (String.Equals(Path.GetFullPath runDir, expectedRun, StringComparison.OrdinalIgnoreCase)) then
        invalidArg "run-dir" "study runs must use their separate results subtree"
    if not ((field manifest "cohort").AsArray() |> Seq.exists (fun row -> (requiredNode row).GetValue<string>() = task + "/" + style + "/" + pathName runDir)) then
        invalidArg "run-dir" "run is not preregistered"
    let producerPath = Path.Combine(runDir, "producer", sprintf "%04d.json" roundNumber)
    let producer = readJson producerPath
    let manifestHash = hashFile manifestPath
    checkFiles (Path.Combine(runDir, "solution")) ((field producer "solutionFiles").AsArray())
    if textField producer "studySha256" <> manifestHash then invalidArg "producer" "wrong study binding"
    if textField producer "task" <> task || textField producer "style" <> style then
        invalidArg "producer" "wrong task/style binding"
    for field in [ "sessionId"; "invocationId"; "route"; "producerKind"; "requestedModel"; "startedUtc"; "finishedUtc"; "status" ] do
        textField producer field |> ignore
    if textField producer "producerKind" <> "ai" then invalidArg "producer" "cohort requires actual AI producers"
    let provider = field producer "providerModel"
    if isNull provider.["value"] then textField provider "unknownReason" |> ignore
    else textField provider "value" |> ignore
    let contextDir = Path.Combine(runDir, "producer", sprintf "%04d-context" roundNumber)
    let context = readJson (Path.Combine(contextDir, "context.json"))
    checkFiles (Path.Combine(contextDir, "payload")) ((field context "files").AsArray())
    if hashFile (Path.Combine(contextDir, "invocation.json")) <> textField producer "invocationSha256" then
        invalidArg "invocation" "raw route/request/response receipt binding is missing or changed"
    if hashFile (Path.Combine(contextDir, "context.json")) <> textField producer "contextSha256" then
        invalidArg "context" "wrong supplied context binding"
    // Every supplied file must be a frozen public input, the same session's last
    // solution, or its exact last feedback. Arbitrary parent/audit context fails closed.
    let feedbackHash = if previous = "" then "" else hashFile (Path.Combine(previous, "feedback.json"))
    let allowed =
        [ for file in (field manifest "files").AsArray() do
              let path = textField file "path"
              if path.StartsWith("guides/", StringComparison.Ordinal)
                 || path.StartsWith("tasks/" + task + "/tests/", StringComparison.Ordinal)
                 || path.StartsWith("tasks/" + task + "/template-" + style + "/", StringComparison.Ordinal)
                 || path = "tasks/" + task + "/prompt-" + style + ".md"
                 || path.StartsWith("feed/", StringComparison.Ordinal) then
                  yield textField file "sha256"
          if previous <> "" then
              yield feedbackHash
              for file in Directory.GetFiles(Path.Combine(previous, "solution"), "*.cs") do yield hashFile file ] |> Set.ofList
    for file in (field context "files").AsArray() do
        if not (allowed.Contains(textField file "sha256")) then invalidArg "context" "unapproved supplied context"
    let promptHash = snapshotHash manifest ("tasks/" + task + "/prompt-" + style + ".md")
    if not ((field context "files").AsArray() |> Seq.exists (fun file -> textField file "sha256" = promptHash)) then
        invalidArg "context" "rendered prompt was not supplied"
    if previous <> "" then
        if textField (readJson (Path.Combine(previous, "inputs.json"))) "studySha256" <> manifestHash then
            invalidArg "study" "predecessor used a different study"
        if not ((field context "files").AsArray() |> Seq.exists (fun file -> textField file "sha256" = feedbackHash)) then
            invalidArg "feedback" "exact predecessor feedback was not supplied"
        let prior = readJson (Path.Combine(previous, "producer-receipt.json"))
        if textField prior "sessionId" <> textField producer "sessionId" then invalidArg "producer" "correction changed session"
        if textField producer "feedbackSha256" <> feedbackHash then
            invalidArg "feedback" "correction did not bind exact predecessor feedback"
    if Directory.Exists results then
        for path in Directory.GetFiles(results, "producer-receipt.json", SearchOption.AllDirectories) do
            let prior = readJson path
            if textField prior "invocationId" = textField producer "invocationId" then invalidArg "producer" "duplicate invocation"
            if textField prior "sessionId" = textField producer "sessionId"
               && not (path.StartsWith(Path.GetFullPath(runDir) + string Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) then
                invalidArg "producer" "initial session was reused by another run"
    { TaskRoot = Path.Combine(snapshot, "tasks")
      Snapshot = snapshot
      ManifestHash = manifestHash
      ExpectedTests = (field (field manifest "expectedTests") task).GetValue<int>()
      ProducerPath = producerPath }

// Manual oracle controls and later package replays use the same runner/snapshot,
// but never claim AI generation or consume a preregistered cohort slot.
let private bindReplay repositoryRoot study task runDir =
    let root, manifest, snapshot = loadStudy repositoryRoot study
    let results = Path.GetFullPath(Path.Combine(repositoryRoot, "eng", "evaluation", "results")) + string Path.DirectorySeparatorChar
    if (Path.GetFullPath runDir).StartsWith(results, StringComparison.OrdinalIgnoreCase) then
        invalidArg "replay" "replays must be outside the historical and cohort results subtrees"
    { TaskRoot = Path.Combine(snapshot, "tasks")
      Snapshot = snapshot
      ManifestHash = hashFile (Path.Combine(root, "manifest.json"))
      ExpectedTests = (field (field manifest "expectedTests") task).GetValue<int>()
      ProducerPath = "" }

let private runVerify
    (repositoryRoot: string)
    (stdout: TextWriter)
    (stderr: TextWriter)
    (task: string)
    (style: string)
    (runDir: string)
    (roundNumber: int)
    (study: string option)
    (replay: bool)
    : int =
    let evaluationRoot = Path.Combine(repositoryRoot, "eng", "evaluation")
    let historical = Path.GetFullPath(Path.Combine(evaluationRoot, "results")) + string Path.DirectorySeparatorChar
    let fullRun = Path.GetFullPath runDir
    if study.IsNone && (fullRun.StartsWith(historical, StringComparison.OrdinalIgnoreCase)
                         || File.Exists(Path.Combine(runDir, "record.json"))) then
        invalidArg "run-dir" "historical results are read-only; select a fresh study"
    if roundNumber < 1 || roundNumber > 3 then invalidArg "round" "round must be 1..3"
    let rounds = Path.Combine(runDir, "rounds")
    let existing = if Directory.Exists rounds then Directory.GetDirectories rounds |> Array.sort else [||]
    if roundNumber <> existing.Length + 1 then invalidArg "round" "duplicate or missing predecessor round"
    // Validate the entire chain, including file manifests, not merely the latest hash.
    let mutable previousHash = ""
    for index in 0 .. existing.Length - 1 do
        let directory = existing.[index]
        if pathName directory <> sprintf "%04d" (index + 1) then invalidArg "rounds" "non-contiguous history"
        let receiptPath = Path.Combine(directory, "receipt.json")
        let receipt = readJson receiptPath
        let content = Directory.GetFiles(directory, "*", SearchOption.AllDirectories) |> Array.filter (fun path -> path <> receiptPath)
        let declared = (field receipt "files").AsArray()
        if content.Length <> declared.Count then invalidArg "history" "round files added or removed"
        for file in declared do
            if hashFile (Path.Combine(directory, textField file "path")) <> textField file "sha256" then
                invalidArg "history" "round bytes overwritten"
        if (field receipt "previousReceiptSha256").GetValue<string>() <> previousHash then
            invalidArg "history" "broken predecessor chain"
        previousHash <- hashFile receiptPath
        let priorRecord = readJson (Path.Combine(directory, "record.json"))
        if textField priorRecord "verdict" = "GREEN" then invalidArg "round" "completed run cannot be corrected"
    let previous = if existing.Length = 0 then "" else Array.last existing
    let binding = study |> Option.map (fun value ->
        if replay then bindReplay repositoryRoot value task runDir
        else bindStudy repositoryRoot value task style runDir roundNumber previous)
    let taskRoot = binding |> Option.map (fun value -> value.TaskRoot) |> Option.defaultValue (Path.Combine(evaluationRoot, "tasks"))
    let taskDir = Path.Combine(taskRoot, task)
    let solutionDir = Path.Combine(runDir, "solution")
    let templateDir = Path.Combine(taskDir, "template-" + style)

    if not (Directory.Exists taskDir) then
        stderr.WriteLine(sprintf "unknown task %s" task)
        2
    elif not (Directory.Exists solutionDir) || (Directory.GetFiles(solutionDir, "*.cs").Length = 0) then
        stderr.WriteLine(sprintf "%s contains no solution files" solutionDir)
        2
    else
        let oracleFiles = sortedCsFiles (Path.Combine(taskDir, "tests"))
        let solutionFiles = sortedCsFiles solutionDir
        let reserved = System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)
        for file in oracleFiles @ (Directory.GetFiles templateDir |> List.ofArray) do reserved.Add(pathName file) |> ignore
        for file in solutionFiles do
            if not (reserved.Add(pathName file)) then invalidArg "solution" "solution filename collides with trusted input"
        let roundDir = Path.Combine(rounds, sprintf "%04d" roundNumber)
        Directory.CreateDirectory roundDir |> ignore
        writeNew (Path.Combine(roundDir, "attempt.json")) (sprintf "{\"round\":%d}\n" roundNumber)
        let snapshotSolution = Path.Combine(roundDir, "solution")
        Directory.CreateDirectory snapshotSolution |> ignore
        for file in solutionFiles do File.Copy(file, Path.Combine(snapshotSolution, pathName file), false)
        match binding with
        | Some value when value.ProducerPath <> "" ->
            checkFiles snapshotSolution ((field (readJson value.ProducerPath) "solutionFiles").AsArray())
            File.Copy(value.ProducerPath, Path.Combine(roundDir, "producer-receipt.json"), false)
            let contextDir = Path.Combine(runDir, "producer", sprintf "%04d-context" roundNumber)
            for file in Directory.GetFiles(contextDir, "*", SearchOption.AllDirectories) do
                let target = Path.Combine(roundDir, "context", Path.GetRelativePath(contextDir, file))
                Directory.CreateDirectory(parentDirectory target) |> ignore
                File.Copy(file, target, false)
        | _ -> ()
        let buildRoot = Path.Combine(repositoryRoot, "artifacts", "evaluation", "builds")
        let buildDir = Path.Combine(buildRoot, sprintf "%s-%s-%s-%s" task style (pathName runDir) (Guid.NewGuid().ToString("N")))

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

        for solutionFile in sortedCsFiles snapshotSolution do
            File.Copy(solutionFile, Path.Combine(buildDir, pathName solutionFile), false)

        let upstreamFeed =
            study |> Option.bind (fun name ->
                readJson (Path.Combine(studyRoot repositoryRoot name, "plan.json")) |> upstreamPackageFeed)
        writeNugetConfig repositoryRoot buildDir upstreamFeed

        let environment =
            match binding with
            | None -> childEnvironment repositoryRoot
            | Some value ->
                // Per-round cache: no stale candidate can be satisfied from globals.
                let config = Path.Combine(buildDir, "NuGet.config")
                let xml = System.Xml.Linq.XDocument.Load config
                let add = xml.Descendants(System.Xml.Linq.XName.Get "add") |> Seq.find (fun node -> xmlAttribute node "key" = "evaluation-feed")
                add.SetAttributeValue(System.Xml.Linq.XName.Get "value", Path.Combine(value.Snapshot, "feed"))
                xml.Save config
                childEnvironment repositoryRoot |> List.map (fun (key, path) ->
                    if key = "NUGET_PACKAGES" || key = "NUGET_HTTP_CACHE_PATH" then key, Path.Combine(buildDir, key.ToLowerInvariant())
                    else key, path)
        match binding with
        | Some value ->
            for relative in [ "global.json"; "Directory.Build.props" ] do
                File.Copy(Path.Combine(value.Snapshot, "environment", relative), Path.Combine(buildDir, relative), true)
        | None -> ()
        let inputs = JsonObject()
        inputs.["studySha256"] <- jstr (binding |> Option.map (fun value -> value.ManifestHash) |> Option.defaultValue "replay-only")
        inputs.["solution"] <- filesReceipt snapshotSolution
        inputs.["buildInputs"] <- filesReceipt buildDir
        let trustedInputs = Path.Combine(roundDir, "trusted-inputs")
        Directory.CreateDirectory trustedInputs |> ignore
        for file in Directory.GetFiles buildDir do File.Copy(file, Path.Combine(trustedInputs, pathName file), false)
        writeNew (Path.Combine(roundDir, "inputs.json")) (inputs.ToJsonString jsonOptions + "\n")
        let capture name command =
            let result = childRunner buildDir environment command
            writeNew (Path.Combine(roundDir, name + ".stdout.log")) result.Stdout
            writeNew (Path.Combine(roundDir, name + ".stderr.log")) result.Stderr
            let receipt = JsonObject()
            receipt.["command"] <- JsonSerializer.SerializeToNode(command)
            receipt.["workingDirectory"] <- jstr buildDir
            receipt.["environment"] <- JsonSerializer.SerializeToNode(environment)
            receipt.["exitCode"] <- jint result.ExitCode
            writeNew (Path.Combine(roundDir, name + ".process.json")) (receipt.ToJsonString jsonOptions + "\n")
            result
        let started = clock ()
        let buildCommand = [ "dotnet"; "build"; "--configuration"; "Release"; "-bl:build.binlog"; "-p:ErrorLog=diagnostics.sarif"; "-p:RestorePackagesWithLockFile=true" ] @ (if binding.IsSome then [ "-p:RestoreLockedMode=true" ] else [])
        let build = capture "build" buildCommand
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
        let mutable testSucceeded = 0
        let mutable testSkipped = 0

        if build.ExitCode = 0 then
            let test =
                capture "test" [ "dotnet"; "test"; "--configuration"; "Release"; "--no-build" ]

            testOutput <- test.Stdout + test.Stderr
            let summary = testSummary.Match testOutput
            let counts = testCounts.Match testOutput
            if counts.Success then
                let parsed = [ for index in 1 .. 4 ->
                                   match Int32.TryParse counts.Groups.[index].Value with
                                   | true, value -> Some value
                                   | false, _ -> None ]
                match parsed with
                | [ Some total; Some failed; Some succeeded; Some skipped ] ->
                    testTotal <- total
                    testFailed <- failed
                    testSucceeded <- succeeded
                    testSkipped <- skipped
                | _ -> ()
            testOk <- Some(test.ExitCode = 0 && testSummary.Matches(testOutput).Count = 1
                           && summary.Groups.[1].Value = "Passed" && testCounts.Matches(testOutput).Count = 1
                           && testTotal > 0 && testFailed = 0 && testSkipped = 0 && testSucceeded = testTotal
                           && (binding |> Option.forall (fun value -> value.ExpectedTests = testTotal)))

        let loc = sortedCsFiles snapshotSolution |> List.sumBy countLoc

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
        semantic.["succeeded"] <- jint testSucceeded
        semantic.["skipped"] <- jint testSkipped

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

        // SARIF retains location, severity, ID and message; full stdout/stderr
        // retain every occurrence even when MSBuild repeats a diagnostic.
        let outputs = Path.Combine(roundDir, "verification-assets")
        Directory.CreateDirectory outputs |> ignore
        for file in Directory.GetFiles(buildDir, "*", SearchOption.AllDirectories) do
            let relative = Path.GetRelativePath(buildDir, file)
            if file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
               || file.EndsWith(".sarif", StringComparison.OrdinalIgnoreCase)
               || file.EndsWith(".binlog", StringComparison.OrdinalIgnoreCase)
               || file.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase)
               || file.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
               || file.EndsWith(".deps.json", StringComparison.OrdinalIgnoreCase)
               || file.EndsWith(".runtimeconfig.json", StringComparison.OrdinalIgnoreCase)
               || pathName file = "project.assets.json" || pathName file = "packages.lock.json" then
                let target = Path.Combine(outputs, relative)
                Directory.CreateDirectory(parentDirectory target) |> ignore
                File.Copy(file, target, false)
        let diagnosticReceipt = JsonObject()
        let occurrences = JsonArray()
        for matched in Regex.Matches(buildOutput, @"^(?<location>.*?): (?<severity>warning|error) (?<id>[A-Z]+[0-9]+): (?<message>.*)$", RegexOptions.Multiline) do
            let occurrence = JsonObject()
            for field in [ "location"; "severity"; "id"; "message" ] do occurrence.[field] <- jstr matched.Groups.[field].Value
            occurrences.Add occurrence
        diagnosticReceipt.["occurrences"] <- occurrences
        diagnosticReceipt.["fsDiagnosticIds"] <- diagnostics.DeepClone()
        diagnosticReceipt.["assets"] <- filesReceipt outputs
        writeNew (Path.Combine(roundDir, "diagnostics.json")) (diagnosticReceipt.ToJsonString jsonOptions + "\n")
        match binding with
        | Some value -> checkFiles value.Snapshot ((field (readJson (Path.Combine(studyRoot repositoryRoot study.Value, "manifest.json"))) "files").AsArray())
        | None -> ()
        let green = build.ExitCode = 0 && testOk = Some true
        record.["verdict"] <- jstr (if green then "GREEN" else "RED")
        record.["evidenceKind"] <- jstr (if replay || study.IsNone then "replay" else "generation-verification")

        let serialized = record.ToJsonString jsonOptions
        let recordPath = Path.Combine(roundDir, "record.json")
        writeNew recordPath (serialized + "\n")
        // This is the exact machine payload the orchestrator must deliver, not a
        // claim that the file-based runner invoked a model or delivered feedback.
        let feedback = JsonObject()
        feedback.["record"] <- record.DeepClone()
        feedback.["buildStdout"] <- jstr build.Stdout
        feedback.["buildStderr"] <- jstr build.Stderr
        feedback.["testOutput"] <- jstr testOutput
        writeNew (Path.Combine(roundDir, "feedback.json")) (feedback.ToJsonString jsonOptions + "\n")
        let receipt = JsonObject()
        receipt.["schema"] <- jstr "funnysharp-evaluation-round/v1"
        receipt.["previousReceiptSha256"] <- jstr previousHash
        receipt.["files"] <- filesReceipt roundDir
        writeNew (Path.Combine(roundDir, "receipt.json")) (receipt.ToJsonString jsonOptions + "\n")
        stdout.WriteLine serialized

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
    | PrepStudy of study: string
    | Verify of task: string * style: string * runDir: string * roundNumber: int * study: string option * replay: bool
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
    let mutable study = None
    let mutable replay = false
    let mutable error : string option = None
    let mutable remaining = args

    while error.IsNone && not remaining.IsEmpty do
        match remaining with
        | "--replay" :: rest ->
            replay <- true
            remaining <- rest
        | "--study" :: value :: rest ->
            study <- Some value
            remaining <- rest
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
                if roundNumber < 1 || roundNumber > 3 then Error "round must be 1..3"
                elif replay && study.IsNone then Error "--replay requires --study"
                else Ok(Verify(task, style, runDir, roundNumber, study, replay))
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
    | "prep-feed" :: "--study" :: study :: [] -> Ok(PrepStudy study)
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
            | PrepFeed -> runPrepFeed root stdout stderr None
            | PrepStudy study ->
                let feed = Path.Combine(root, "artifacts", "evaluation", "feed-" + study)
                let studyPath = studyRoot root study
                if Directory.Exists(Path.Combine(studyPath, "snapshot")) then invalidArg "study" "snapshot already exists"
                let code = runPrepFeed root stdout stderr (Some feed)
                if code = 0 then freezeStudy root study feed
                code
            | Verify(task, style, runDir, roundNumber, study, replay) -> runVerify root stdout stderr task style runDir roundNumber study replay
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
