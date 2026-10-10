module FunnySharp.Harness.ReleaseRun

// Executes the release protocol once. Git owns source identity; child exit codes own verdicts.

open System
open System.Collections.Generic
open System.Diagnostics
open System.Globalization
open System.IO
open System.Net.Http
open System.Runtime.InteropServices
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open System.Text.RegularExpressions
open System.Xml.Linq
open FunnySharp.Harness.Output
open FunnySharp.Harness.Proc
open FunnySharp.Harness.Repo

let private usageLine =
    "usage: Run-Release.ps1 -AttemptId ID [-RepositoryRoot PATH] [-OutputDirectory PATH]"
    + " [-CompatibilityRuntimeIdentifier RID] [-CompatibilityPackageFeed URL]"
    + " [-DistributionFeed URL] [-SkipBenchmarks]"

let private helpText =
    String.concat
        "\n"
        [ usageLine
          ""
          "Run the FunnySharp release candidate pipeline from one Git commit and record"
          "the command results."
          ""
          "parameters:"
          "  -AttemptId ID                          (mandatory) release attempt id."
          "  -RepositoryRoot PATH                   repository root (default: nearest FunnySharp.slnx)."
          "  -OutputDirectory PATH                  attempt directory (default: artifacts/release-candidate/<commit>/<id>)."
          "  -CompatibilityRuntimeIdentifier RID    default: host runtime identifier."
          "  -CompatibilityPackageFeed URL          default: https://api.nuget.org/v3/index.json."
          "  -DistributionFeed URL                  may repeat; default: https://api.nuget.org/v3/index.json."
          "  -SkipBenchmarks                        select the benchmarkSkipped protocol mode."
          "  -h, --help                             show this help." ]

let private utf8NoBom = UTF8Encoding(false)

// ---- JSON reading helpers (same shapes as PerformanceDocs) ----

let private tryProp (name: string) (element: JsonElement) : JsonElement option =
    let mutable value = Unchecked.defaultof<JsonElement>

    if element.ValueKind = JsonValueKind.Object && element.TryGetProperty(name, &value) then
        Some value
    else
        None

let private stringValue (element: JsonElement) : string =
    if element.ValueKind = JsonValueKind.String then
        Option.ofObj (element.GetString()) |> Option.defaultValue ""
    else
        element.ToString()

let private stringOf (name: string) (element: JsonElement) : string =
    element |> tryProp name |> Option.map stringValue |> Option.defaultValue ""

// ---- Path helpers (the security boundary, ported from the script) ----

let private pathComparison () : StringComparison =
    if OperatingSystem.IsWindows() then
        StringComparison.OrdinalIgnoreCase
    else
        StringComparison.Ordinal

let private pathAtOrWithin (childPath: string) (parentPath: string) : bool =
    let comparison = pathComparison ()
    let prefix = parentPath.TrimEnd('\\', '/') + string Path.DirectorySeparatorChar
    childPath.Equals(parentPath, comparison) || childPath.StartsWith(prefix, comparison)

let private resolveFullPath (basePath: string) (path: string) : string =
    if Path.IsPathFullyQualified path then
        Path.GetFullPath path
    else
        Path.GetFullPath(Path.Combine(basePath, path))

/// PowerShell's Resolve-Path for the artifacts directory.
let private resolveDirectoryPath (path: string) : string =
    try
        match Directory.ResolveLinkTarget(path, true) with
        | null -> Path.GetFullPath path
        | target -> target.FullName
    with _ ->
        Path.GetFullPath path

/// Resolve a symlink in every component, so two spellings of one directory compare equal.
/// The runtime's ResolveLinkTarget only follows the final component, and macOS reaches its temp
/// roots through /var -> /private/var while git reports the resolved spelling.
let private canonicalDirectoryPath (path: string) : string =
    let full = Path.GetFullPath path

    match Path.GetPathRoot full with
    | null -> full
    | root ->
        let parts =
            full.Substring(root.Length)
                .Split(
                    [| Path.DirectorySeparatorChar; Path.AltDirectorySeparatorChar |],
                    StringSplitOptions.RemoveEmptyEntries
                )
            |> List.ofArray

        let rec walk (current: string) (remaining: string list) =
            match remaining with
            | [] -> current
            | part :: rest ->
                let next = Path.Combine(current, part)

                let resolved =
                    try
                        match Directory.ResolveLinkTarget(next, true) with
                        | null -> next
                        | target -> target.FullName
                    with _ ->
                        next

                walk resolved rest

        walk root parts

/// A proper, non-reparse subdirectory of the artifacts directory; fails closed.
let private assertSafeArtifactsSubdirectory (artifactsDirectory: string) (path: string) : string =
    if not (Directory.Exists artifactsDirectory) then
        raise (DirectoryNotFoundException(sprintf "ArtifactsDirectory '%s' was not found." artifactsDirectory))

    let artifactsAttributes = File.GetAttributes artifactsDirectory

    if artifactsAttributes.HasFlag FileAttributes.ReparsePoint then
        raise (InvalidOperationException(sprintf "ArtifactsDirectory '%s' cannot be a reparse point." (resolveDirectoryPath artifactsDirectory)))

    let resolvedArtifacts = resolveDirectoryPath artifactsDirectory
    let fullPath = Path.GetFullPath path

    if
        fullPath.Equals(resolvedArtifacts, pathComparison ())
        || not (pathAtOrWithin fullPath resolvedArtifacts)
    then
        raise (
            InvalidOperationException(
                sprintf "Path must be a proper subdirectory of '%s': '%s'." resolvedArtifacts fullPath
            )
        )

    let relative = Path.GetRelativePath(resolvedArtifacts, fullPath)

    let segments =
        relative.Split(
            [| Path.DirectorySeparatorChar; Path.AltDirectorySeparatorChar |],
            StringSplitOptions.RemoveEmptyEntries
        )

    let mutable current = resolvedArtifacts

    for segment in segments do
        current <- Path.Combine(current, segment)

        if Directory.Exists current || File.Exists current then
            let item = File.GetAttributes current

            if item.HasFlag FileAttributes.ReparsePoint then
                raise (InvalidOperationException(sprintf "Path cannot contain a reparse point: '%s'." current))

            let resolvedCurrent = resolveDirectoryPath current

            if not (pathAtOrWithin resolvedCurrent resolvedArtifacts) then
                raise (InvalidOperationException(sprintf "Resolved path escapes '%s': '%s'." resolvedArtifacts resolvedCurrent))

            current <- resolvedCurrent

    fullPath

/// Create the immutable attempt directory; it must not pre-exist.
let private initializeOutputDirectory (artifactsDirectory: string) (path: string) : string =
    let path = assertSafeArtifactsSubdirectory artifactsDirectory path

    if Directory.Exists path || File.Exists path then
        raise (InvalidOperationException(sprintf "OutputDirectory '%s' already exists; release attempts are immutable." path))

    Directory.CreateDirectory path |> ignore
    path

// ---- Protocol model, step table and attempt-directory helpers (lane A seam) ----

type ReleaseStep = ReleaseProtocol.ReleaseStep

/// The protocol the runner executes: the step table comes from ReleaseProtocol.fs.
type ReleaseProtocolApi =
    { AssertAttemptId: string -> unit
      AssertNewAttemptPath: string -> string -> string -> string -> string
      StepPrefix: int -> string -> string
      LoadSteps: string -> string -> (string * string) list -> ReleaseStep list }

/// ReleaseProtocol owns step expansion and output-path validation.
let private loadStepsFromProtocol (protocolPath: string) (mode: string) (tokens: (string * string) list) : ReleaseStep list =
    match ReleaseProtocol.readProtocol protocolPath
          |> Result.bind (fun protocol -> ReleaseProtocol.releaseSteps protocol mode (Map.ofList tokens)) with
    | Ok steps -> steps
    | Error error -> invalidOp error.Message

let defaultProtocolApi: ReleaseProtocolApi =
    { AssertAttemptId = fun value ->
        match ReleaseProtocol.assertReleaseAttemptId value with
        | Ok _ -> ()
        | Error error -> invalidOp error.Message
      AssertNewAttemptPath = fun path artifacts commit attempt ->
        match ReleaseProtocol.assertNewReleaseAttemptPath path artifacts commit attempt with
        | Ok value -> value
        | Error error -> invalidOp error.Message
      StepPrefix = ReleaseProtocol.stepPrefix
      LoadSteps = loadStepsFromProtocol }

/// exe -> arguments -> extra child environment -> working directory -> captured result.
type CommandRunner = string -> string list -> Map<string, string> -> string -> ProcessResult

type HttpResponse = { StatusCode: int; Content: string }

type HttpGet = string -> HttpResponse

let private runChildProcess
    (exe: string)
    (arguments: string list)
    (extraEnvironment: Map<string, string>)
    (workingDirectory: string)
    : ProcessResult =
    use proc = new Process()
    let info = ProcessStartInfo exe
    info.UseShellExecute <- false
    info.RedirectStandardOutput <- true
    info.RedirectStandardError <- true
    info.WorkingDirectory <- workingDirectory

    for argument in arguments do
        info.ArgumentList.Add argument

    for entry in extraEnvironment do
        info.Environment.[entry.Key] <- entry.Value

    proc.StartInfo <- info

    if not (proc.Start()) then
        raise (InvalidOperationException(sprintf "failed to start process '%s'" exe))

    let stdoutTask = proc.StandardOutput.ReadToEndAsync()
    let stderrTask = proc.StandardError.ReadToEndAsync()
    proc.WaitForExit()
    let stdout = stdoutTask.GetAwaiter().GetResult()
    let stderr = stderrTask.GetAwaiter().GetResult()

    { ExitCode = proc.ExitCode
      Stdout = stdout
      Stderr = stderr }

let private httpGet (url: string) : HttpResponse =
    use client = new HttpClient()
    use response = client.GetAsync(url).GetAwaiter().GetResult()
    let content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    { StatusCode = int response.StatusCode; Content = content }

type Collaborators =
    { Runner: CommandRunner
      HttpGet: HttpGet
      Protocol: ReleaseProtocolApi
      UtcNow: unit -> DateTime }

let defaultCollaborators () : Collaborators =
    { Runner = runChildProcess
      HttpGet = httpGet
      Protocol = defaultProtocolApi
      UtcNow = fun () -> DateTime.UtcNow }

let private getGitText (runner: CommandRunner) (arguments: string list) (root: string) : string =
    let result = runner "git" arguments Map.empty root

    if result.ExitCode <> 0 then
        raise (
            InvalidOperationException(
                sprintf "git %s failed: %s" (String.Join(" ", arguments)) (result.Stderr.Trim())
            )
        )

    result.Stdout.Trim()

let private getPackageVersion (projectPath: string) : string =
    if not (File.Exists projectPath) then
        raise (FileNotFoundException(sprintf "Project '%s' does not declare VersionPrefix." projectPath, projectPath))

    let document = XDocument.Load projectPath

    let version =
        document.Descendants(XName.Get "VersionPrefix")
        |> Seq.map (fun element -> element.Value)
        |> Seq.tryFind (fun value -> not (String.IsNullOrWhiteSpace value))

    match version with
    | Some value -> value.Trim()
    | None -> raise (InvalidOperationException(sprintf "Project '%s' does not declare VersionPrefix." projectPath))

let private getPackageVersionState
    (httpGet: HttpGet)
    (feed: string)
    (packageId: string)
    (version: string)
    : unit =
    let service =
        try
            httpGet feed
        with ex ->
            raise (InvalidOperationException(sprintf "Distribution feed '%s' is inaccessible: %s" feed ex.Message, ex))

    if service.StatusCode < 200 || service.StatusCode >= 300 then
        raise (
            InvalidOperationException(
                sprintf "Distribution feed '%s' is inaccessible: the remote server returned an error: (%d)." feed service.StatusCode
            )
        )

    use serviceDocument = JsonDocument.Parse service.Content
    let serviceRoot = serviceDocument.RootElement

    let baseResources =
        match tryProp "resources" serviceRoot with
        | Some resources when resources.ValueKind = JsonValueKind.Array ->
            resources.EnumerateArray()
            |> Seq.filter (fun resource ->
                match tryProp "@type" resource with
                | Some resourceType -> Regex.IsMatch(stringValue resourceType, "^PackageBaseAddress/3\\.0\\.0")
                | None -> false)
            |> Seq.toList
        | _ -> []

    let baseAddress =
        match baseResources with
        | [ single ] when not (String.IsNullOrWhiteSpace(stringOf "@id" single)) -> stringOf "@id" single
        | _ ->
            raise (
                InvalidOperationException(
                    sprintf "Distribution feed '%s' has an ambiguous PackageBaseAddress resource." feed
                )
            )

    let packageIndex = baseAddress.TrimEnd('/') + "/" + packageId.ToLowerInvariant() + "/index.json"

    let packageResponse =
        try
            httpGet packageIndex
        with ex ->
            raise (
                InvalidOperationException(
                    sprintf "Package version state for '%s' at '%s' is inaccessible: %s" packageId feed ex.Message, ex
                )
            )

    let statusCode, content =
        if packageResponse.StatusCode = 404 then
            404, "{\"versions\":[]}"
        elif packageResponse.StatusCode >= 200 && packageResponse.StatusCode < 300 then
            packageResponse.StatusCode, packageResponse.Content
        else
            raise (
                InvalidOperationException(
                    sprintf
                        "Package version state for '%s' at '%s' is inaccessible: the remote server returned an error: (%d)."
                        packageId
                        feed
                        packageResponse.StatusCode
                )
            )

    use packageDocument = JsonDocument.Parse content
    let packageRoot = packageDocument.RootElement

    let versions =
        match tryProp "versions" packageRoot with
        | Some versionsElement when versionsElement.ValueKind = JsonValueKind.Array ->
            versionsElement.EnumerateArray() |> Seq.map stringValue |> Seq.toList
        | _ ->
            raise (
                InvalidOperationException(
                    sprintf "Package version state for '%s' at '%s' is ambiguous." packageId feed
                )
            )

    match ReleaseProtocol.assertPackageVersionAbsent packageId version (Some versions) with
    | Ok () -> ()
    | Error error -> raise (InvalidOperationException error.Message)

type CliOptions =
    { RepositoryRoot: string option
      OutputDirectory: string option
      AttemptId: string option
      CompatibilityRuntimeIdentifier: string option
      CompatibilityPackageFeed: string option
      DistributionFeed: string list
      SkipBenchmarks: bool
      Help: bool }

let private splitParameter (argument: string) : string * string option =
    let separatorIndex = argument.IndexOfAny([| ':'; '=' |], 1)

    if separatorIndex > 1 then
        argument.Substring(0, separatorIndex), Some(argument.Substring(separatorIndex + 1))
    else
        argument, None

let private parseArgs (argv: string list) : Result<CliOptions, string> =
    let empty =
        { RepositoryRoot = None
          OutputDirectory = None
          AttemptId = None
          CompatibilityRuntimeIdentifier = None
          CompatibilityPackageFeed = None
          DistributionFeed = []
          SkipBenchmarks = false
          Help = false }

    let rec loop (options: CliOptions) (remaining: string list) =
        match remaining with
        | [] -> Ok options
        | argument :: rest when argument.StartsWith("-") && argument.Length > 1 ->
            let name, inlineValue = splitParameter argument

            let takeValue (setter: string -> CliOptions) =
                match inlineValue, rest with
                | Some value, _ -> loop (setter value) rest
                | None, value :: rest' -> loop (setter value) rest'
                | None, [] -> Error(sprintf "missing an argument for parameter '%s'" name)

            match name.ToLowerInvariant() with
            | "-h" | "--help" | "-help" -> Ok { options with Help = true }
            | "-repositoryroot" -> takeValue (fun value -> { options with RepositoryRoot = Some value })
            | "-outputdirectory" -> takeValue (fun value -> { options with OutputDirectory = Some value })
            | "-attemptid" -> takeValue (fun value -> { options with AttemptId = Some value })
            | "-compatibilityruntimeidentifier" ->
                takeValue (fun value -> { options with CompatibilityRuntimeIdentifier = Some value })
            | "-compatibilitypackagefeed" ->
                takeValue (fun value -> { options with CompatibilityPackageFeed = Some value })
            | "-distributionfeed" ->
                takeValue (fun value ->
                    { options with
                        DistributionFeed =
                            options.DistributionFeed
                            @ (value.Split(',') |> Array.toList |> List.filter (fun item -> item <> "")) })
            | "-skipbenchmarks" ->
                match inlineValue with
                | Some value when value.Equals("false", StringComparison.OrdinalIgnoreCase) ->
                    loop { options with SkipBenchmarks = false } rest
                | _ -> loop { options with SkipBenchmarks = true } rest
            | _ -> Error(sprintf "unknown parameter '%s'" argument)
        | argument :: _ -> Error(sprintf "unexpected argument '%s'" argument)

    loop empty argv

let private runRelease (stdout: TextWriter) (stderr: TextWriter) (startDirectory: string)
    (collaborators: Collaborators) (options: CliOptions) (attemptId: string) : int =
    let runner = collaborators.Runner
    try
        let repositoryRoot =
            match options.RepositoryRoot with
            | Some root when not (String.IsNullOrWhiteSpace root) -> resolveFullPath startDirectory root
            | _ ->
                match tryFindRootFrom startDirectory with
                | Some root -> root
                | None -> Path.GetFullPath startDirectory

        if not (Directory.Exists repositoryRoot) then
            raise (DirectoryNotFoundException(sprintf "RepositoryRoot was not found: '%s'." repositoryRoot))

        let gitTopLevel = getGitText runner [ "rev-parse"; "--show-toplevel" ] repositoryRoot

        if
            not (
                canonicalDirectoryPath(gitTopLevel)
                    .Equals(canonicalDirectoryPath(repositoryRoot), pathComparison ())
            )
        then
            raise (
                InvalidOperationException(
                    sprintf "RepositoryRoot '%s' is not the active Git checkout '%s'." repositoryRoot gitTopLevel
                )
            )

        let candidateCommit = getGitText runner [ "rev-parse"; "HEAD" ] repositoryRoot

        let candidateStatus =
            getGitText runner [ "status"; "--porcelain=v1"; "--untracked-files=no" ] repositoryRoot

        if not (String.IsNullOrWhiteSpace candidateStatus) then
            raise (
                InvalidOperationException(
                    "Authoritative release requires a clean tracked tree. Dirty paths:"
                    + Environment.NewLine
                    + candidateStatus
                )
            )

        collaborators.Protocol.AssertAttemptId attemptId

        let artifactsDirectory =
            let raw = Path.Combine(repositoryRoot, "artifacts")
            Directory.CreateDirectory raw |> ignore
            resolveDirectoryPath raw

        let outputDirectoryRaw =
            match options.OutputDirectory with
            | Some value when not (String.IsNullOrWhiteSpace value) -> resolveFullPath repositoryRoot value
            | _ -> Path.Combine(artifactsDirectory, "release-candidate", candidateCommit, attemptId)

        let outputDirectory =
            collaborators.Protocol.AssertNewAttemptPath outputDirectoryRaw artifactsDirectory candidateCommit attemptId

        let outputDirectory = initializeOutputDirectory artifactsDirectory outputDirectory
        let logsDirectory = Path.Combine(outputDirectory, "logs")
        let packagesDirectory = Path.Combine(outputDirectory, "packages")
        let benchmarkArtifactsDirectory = Path.Combine(outputDirectory, "benchmark-artifacts")
        let compatibilityOutputDirectory = Path.Combine(outputDirectory, "compatibility-run")
        let nugetPackagesDirectory = Path.Combine(outputDirectory, "nuget-packages")

        for directory in [ logsDirectory; packagesDirectory; nugetPackagesDirectory ] do
            Directory.CreateDirectory directory |> ignore

        let coreVersion = getPackageVersion (Path.Combine(repositoryRoot, "src/FunnySharp/FunnySharp.csproj"))

        let aspNetCoreVersion =
            getPackageVersion (Path.Combine(repositoryRoot, "src/FunnySharp.AspNetCore/FunnySharp.AspNetCore.csproj"))

        if coreVersion <> aspNetCoreVersion then
            raise (
                InvalidOperationException(
                    sprintf
                        "Package versions must match; found FunnySharp %s and FunnySharp.AspNetCore %s."
                        coreVersion
                        aspNetCoreVersion
                )
            )

        let distributionFeeds =
            (if options.DistributionFeed.IsEmpty then
                 [ "https://api.nuget.org/v3/index.json" ]
             else
                 options.DistributionFeed)
            |> List.filter (fun feed -> not (String.IsNullOrWhiteSpace feed))
            |> List.distinct
            |> List.sort

        if distributionFeeds.IsEmpty then
            raise (InvalidOperationException "At least one distribution feed is required.")

        let compatibilityFeed =
            options.CompatibilityPackageFeed
            |> Option.filter (fun value -> not (String.IsNullOrWhiteSpace value))
            |> Option.defaultValue "https://api.nuget.org/v3/index.json"

        let compatibilityRid =
            options.CompatibilityRuntimeIdentifier
            |> Option.filter (fun value -> not (String.IsNullOrWhiteSpace value))
            |> Option.defaultValue RuntimeInformation.RuntimeIdentifier

        let checkVersions () =
            for feed in distributionFeeds do
                for packageId in [ "FunnySharp"; "FunnySharp.AspNetCore" ] do
                    getPackageVersionState collaborators.HttpGet feed packageId coreVersion

        let assertSourceUnchanged () =
            let commit = getGitText runner [ "rev-parse"; "HEAD" ] repositoryRoot
            let status = getGitText runner [ "status"; "--porcelain=v1"; "--untracked-files=no" ] repositoryRoot
            if commit <> candidateCommit || not (String.IsNullOrWhiteSpace status) then
                invalidOp "Source changed while the release candidate pipeline ran."

        let childEnvironment =
            Map.ofList
                [ "NUGET_PACKAGES", nugetPackagesDirectory
                  "FUNNYSHARP_CANDIDATE_COMMIT", candidateCommit
                  "DOTNET_CLI_UI_LANGUAGE", "en"
                  "DOTNET_NOLOGO", "1" ]
        let tokens =
            [ "root", repositoryRoot
              "compatibilityFeed", compatibilityFeed
              "packages", packagesDirectory
              "benchmarkRoot", Path.Combine(repositoryRoot, "benchmarks/FunnySharp.Benchmarks")
              "benchmarkArtifacts", benchmarkArtifactsDirectory
              "benchmarkResults", Path.Combine(benchmarkArtifactsDirectory, "results")
              "performanceObservationProposal", Path.Combine(outputDirectory, "performance-observation-proposal.json")
              "compatibilityOutput", compatibilityOutputDirectory
              "compatibilityRid", compatibilityRid ]
        let steps = collaborators.Protocol.LoadSteps (Path.Combine(repositoryRoot, ReleaseProtocol.ProtocolRelativePath))
                        (ReleaseProtocol.modeFor options.SkipBenchmarks) tokens
        let commands = JsonArray()
        let mutable failure: string option = None
        try
            checkVersions ()
            for index in 0 .. steps.Length - 1 do
                let step = steps.[index]
                let prefix = collaborators.Protocol.StepPrefix (index + 1) step.Name
                let result = runner step.FileName step.Arguments childEnvironment step.WorkingDirectory
                File.WriteAllText(Path.Combine(logsDirectory, prefix + ".stdout.log"), result.Stdout, utf8NoBom)
                File.WriteAllText(Path.Combine(logsDirectory, prefix + ".stderr.log"), result.Stderr, utf8NoBom)
                let command = JsonObject()
                command.["name"] <- JsonValue.Create step.Name
                command.["exitCode"] <- JsonValue.Create result.ExitCode
                commands.Add command
                if result.ExitCode <> 0 then
                    invalidOp (sprintf "Release command '%s' failed with exit code %d. See '%s'." step.Name result.ExitCode logsDirectory)
                if step.Name = "test" then
                    match ToolingVerify.stepFailure "test" result.Stdout result.Stderr repositoryRoot with
                    | Some message -> invalidOp message
                    | None -> ()
            checkVersions ()
            assertSourceUnchanged ()
        with ex -> failure <- Some ex.Message
        let summary = JsonObject()
        summary.["candidateCommit"] <- JsonValue.Create candidateCommit
        summary.["attemptId"] <- JsonValue.Create attemptId
        summary.["succeeded"] <- JsonValue.Create failure.IsNone
        summary.["commands"] <- commands
        match failure with
        | Some message -> summary.["error"] <- JsonValue.Create message
        | None -> ()
        File.WriteAllText(Path.Combine(outputDirectory, "release-summary.json"), summary.ToJsonString(JsonSerializerOptions(WriteIndented = true)), utf8NoBom)
        match failure with
        | Some message -> stderr.WriteLine("Release run failed: " + message); 1
        | None -> stdout.WriteLine("Release run passed. Output: " + outputDirectory); 0
    with ex -> stderr.WriteLine(ex.Message); 1

let mainWith (stdout: TextWriter) (stderr: TextWriter) (startDirectory: string)
    (collaborators: Collaborators) (argv: string list) : int =
    match parseArgs argv with
    | Error message -> stderr.WriteLine(usageLine + "\n" + message); 2
    | Ok options when options.Help -> stdout.WriteLine helpText; 0
    | Ok options ->
        match options.AttemptId with
        | Some attemptId when not (String.IsNullOrWhiteSpace attemptId) ->
            runRelease stdout stderr startDirectory collaborators options attemptId
        | _ -> stderr.WriteLine(usageLine + "\nThe -AttemptId parameter is required."); 2

let main (argv: string array) : int =
    mainWith Console.Out Console.Error Environment.CurrentDirectory (defaultCollaborators ()) (List.ofArray argv)
