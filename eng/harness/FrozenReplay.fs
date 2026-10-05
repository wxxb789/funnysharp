module FunnySharp.Harness.FrozenReplay

// The historical manifest froze Evaluation.fs, but not its support modules.
// Load that runner in a fresh root; retain and bind today's support separately.

open System
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open FunnySharp.Harness.Proc

let private study = "audit-resolution-v6"
let private environmentFiles = [ "global.json"; "Directory.Build.props"; "build.fsx"; "eng/harness/Evaluation.fs" ]
let private supportFiles = [ "eng/harness/Output.fs"; "eng/harness/Proc.fs"; "eng/harness/Repo.fs"; "eng/harness/Loc.fs" ]
let private driverFiles = [ "eng/harness/FrozenReplay.fs"; "eng/evaluation/replay-frozen.fsx" ]

let private hash (path: string) =
    use stream = File.OpenRead path
    SHA256.HashData stream |> Convert.ToHexString |> fun value -> value.ToLowerInvariant()

let private node (value: JsonNode | null) =
    match value with
    | null -> invalidArg "JSON" "required JSON node is missing"
    | value -> value

let private readJson (path: string) = JsonNode.Parse(File.ReadAllText path) |> node

let private files (directory: string) =
    let result = JsonArray()
    for path in Directory.GetFiles(directory, "*", SearchOption.AllDirectories) |> Array.sort do
        let row = JsonObject()
        row.["path"] <- JsonValue.Create(Path.GetRelativePath(directory, path).Replace('\\', '/'))
        row.["sha256"] <- JsonValue.Create(hash path)
        result.Add row
    result

let private checkFiles directory (expected: JsonNode) =
    if (files directory).ToJsonString() <> expected.ToJsonString() then
        invalidArg "inputs" ("files changed in " + directory)

let private writeNew (path: string) (text: string) =
    use stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)
    use writer = new StreamWriter(stream, UTF8Encoding(false))
    writer.Write text

let private copy (source: string) (target: string) =
    Directory.CreateDirectory(Path.GetDirectoryName target |> Option.ofObj |> Option.get) |> ignore
    File.Copy(source, target, false)

let private within (directory: string) (path: string) =
    let directory = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
    let path = Path.GetFullPath path
    String.Equals(directory, path, StringComparison.OrdinalIgnoreCase)
    || path.StartsWith(directory + string Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)

let private replay (repositoryRoot: string) (task: string) (style: string) solutionDirectory outputDirectory expectedManifestHash
    (runner: string -> string list -> ProcessResult) =
    if style <> "idiomatic" && style <> "funnysharp" then invalidArg "style" "unknown style"
    let studyRoot = Path.Combine(repositoryRoot, "eng", "evaluation", "studies", study)
    let manifestPath = Path.Combine(studyRoot, "manifest.json")
    if hash manifestPath <> expectedManifestHash then invalidArg "manifest" "wrong retained manifest SHA256"
    let manifest = readJson manifestPath
    if (node manifest.["schema"]).GetValue<string>() <> "funnysharp-evaluation-study/v1"
       || (node manifest.["study"]).GetValue<string>() <> study then invalidArg "manifest" "wrong study"
    let snapshot = Path.Combine(studyRoot, "snapshot")
    checkFiles snapshot (node manifest.["files"])
    if hash (Path.Combine(studyRoot, "plan.json")) <> (node manifest.["planSha256"]).GetValue<string>() then
        invalidArg "plan" "study plan changed"
    (node (node manifest.["expectedTests"]).[task]).GetValue<int>() |> ignore
    let solutionDirectory = Path.GetFullPath solutionDirectory
    let solutionFiles = Directory.GetFiles(solutionDirectory, "*.cs") |> Array.sort
    if solutionFiles.Length = 0 then invalidArg "solution" "no C# solution files"
    let outputDirectory = Path.GetFullPath outputDirectory
    if outputDirectory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
       |> Array.exists (fun segment -> String.Equals(segment, "results", StringComparison.OrdinalIgnoreCase)) then
        invalidArg "output" "replays must be outside results subtrees"
    if within studyRoot outputDirectory || within solutionDirectory outputDirectory then
        invalidArg "output" "replay output overlaps retained inputs"
    if Directory.Exists outputDirectory || File.Exists outputDirectory then
        invalidArg "output" "replay output already exists; retain it"
    Directory.CreateDirectory outputDirectory |> ignore
    // CreateNew also arbitrates concurrent reservations of the same fresh path.
    writeNew (Path.Combine(outputDirectory, "replay.started.json")) "{\"evidenceKind\":\"replay\"}\n"
    let isolatedRoot = Path.Combine(outputDirectory, "frozen-root")
    let isolatedStudy = Path.Combine(isolatedRoot, "eng", "evaluation", "studies", study)
    for relative in [ "plan.json"; "manifest.json" ] do
        copy (Path.Combine(studyRoot, relative)) (Path.Combine(isolatedStudy, relative))
    for source in Directory.GetFiles(snapshot, "*", SearchOption.AllDirectories) do
        copy source (Path.Combine(isolatedStudy, "snapshot", Path.GetRelativePath(snapshot, source)))
    for relative in environmentFiles do
        copy (Path.Combine(snapshot, "environment", relative)) (Path.Combine(isolatedRoot, relative))
    for relative in supportFiles do
        copy (Path.Combine(repositoryRoot, relative)) (Path.Combine(isolatedRoot, relative))
    let driverDirectory = Path.Combine(outputDirectory, "driver")
    for relative in driverFiles do
        copy (Path.Combine(repositoryRoot, relative)) (Path.Combine(driverDirectory, relative))
    let runDirectory = Path.Combine(outputDirectory, "run")
    let solutionBinding = JsonArray()
    for source in solutionFiles do
        let name = Path.GetFileName source |> Option.ofObj |> Option.get
        let row = JsonObject()
        row.["path"] <- JsonValue.Create name
        row.["sha256"] <- JsonValue.Create(hash source)
        solutionBinding.Add row
        copy source (Path.Combine(runDirectory, "solution", name))
    checkFiles (Path.Combine(runDirectory, "solution")) solutionBinding
    let script = Path.Combine(isolatedRoot, "replay.fsx")
    let loads = supportFiles @ [ "eng/harness/Evaluation.fs" ] |> List.map (fun path -> "#load " + JsonSerializer.Serialize path)
    writeNew script (String.concat "\n" loads + "\nSystem.Environment.Exit(FunnySharp.Harness.Evaluation.mainWith System.Console.Out System.Console.Error __SOURCE_DIRECTORY__ (fsi.CommandLineArgs |> Array.skip 1 |> Array.toList))\n")
    let binding = JsonObject()
    binding.["schema"] <- JsonValue.Create "funnysharp-frozen-replay/v1"
    binding.["evidenceKind"] <- JsonValue.Create "replay"
    binding.["studySha256"] <- JsonValue.Create expectedManifestHash
    binding.["solutionDirectory"] <- JsonValue.Create solutionDirectory
    binding.["solutionFiles"] <- solutionBinding
    binding.["supportBinding"] <- JsonValue.Create "contemporary-not-historical"
    binding.["files"] <- files outputDirectory
    writeNew (Path.Combine(outputDirectory, "replay-bindings.json")) (binding.ToJsonString(JsonSerializerOptions(WriteIndented = true)) + "\n")
    let command = [ "dotnet"; "fsi"; "--exec"; script; "--"; "verify"; task; style; runDirectory; "--study"; study; "--replay" ]
    let result = runner isolatedRoot command
    writeNew (Path.Combine(outputDirectory, "runner.stdout.log")) result.Stdout
    writeNew (Path.Combine(outputDirectory, "runner.stderr.log")) result.Stderr
    let processReceipt = JsonObject()
    processReceipt.["command"] <- JsonSerializer.SerializeToNode command
    processReceipt.["workingDirectory"] <- JsonValue.Create isolatedRoot
    processReceipt.["exitCode"] <- JsonValue.Create result.ExitCode
    writeNew (Path.Combine(outputDirectory, "runner.process.json")) (processReceipt.ToJsonString() + "\n")
    // Reject changes during execution as well as before the reservation.
    checkFiles snapshot (node manifest.["files"])
    if hash manifestPath <> expectedManifestHash then invalidArg "manifest" "retained manifest changed during replay"
    if hash (Path.Combine(studyRoot, "plan.json")) <> (node manifest.["planSha256"]).GetValue<string>() then
        invalidArg "plan" "retained plan changed during replay"
    for row in (node binding.["files"]).AsArray() do
        let row = node row
        if hash (Path.Combine(outputDirectory, (node row.["path"]).GetValue<string>())) <> (node row.["sha256"]).GetValue<string>() then
            invalidArg "inputs" "staged replay input changed"
    result

/// The collaborator starts only the staged FSI runner. Tests inject the existing
/// Evaluation.mainWith with fake dotnet outcomes, never a compiler or a model.
let mainWith (stdout: TextWriter) (stderr: TextWriter) repositoryRoot
    (runner: string -> string list -> ProcessResult) argv =
    match argv with
    | [ task; style; solutionDirectory; outputDirectory; expectedManifestHash ] ->
        try
            let result = replay (Path.GetFullPath repositoryRoot) task style solutionDirectory outputDirectory expectedManifestHash runner
            stdout.Write result.Stdout
            stderr.Write result.Stderr
            result.ExitCode
        with ex ->
            stderr.WriteLine ex.Message
            1
    | _ ->
        stderr.WriteLine "usage: replay-frozen.fsx <task> <idiomatic|funnysharp> <solution-directory> <new-output-directory> <retained-manifest-sha256>"
        2

let main repositoryRoot argv =
    mainWith Console.Out Console.Error repositoryRoot
        (fun directory command -> runCaptureIn (Some directory) command.Head command.Tail |> Async.RunSynchronously)
        (List.ofArray argv)
