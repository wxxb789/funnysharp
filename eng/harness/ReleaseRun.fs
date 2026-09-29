module FunnySharp.Harness.ReleaseRun

// A behaviour-identical F# port of eng/Run-Release.ps1: the release-candidate
// orchestrator. It enforces the clean-tracked-tree precondition, runs the
// version preflight, executes the eng/release-protocol.json step table in order
// capturing every child's stdout/stderr into logs/ and writing one receipt per
// step, snapshots the git source fingerprint before and after, writes
// execution-evidence.json (schema 2), invokes the release verifier and always
// writes release-outcome.json.
//
// Console contract (release-protocol.md section 2.1): success prints exactly
//   Release run passed. Execution evidence: <OutputDirectory>
// on stdout and exits 0; a failure inside the run prints
//   Release run failed: <cause>
// on stderr and exits 1. Earlier precondition failures (bad repository root,
// dirty tracked tree, bad attempt path, mismatched package versions, blocked
// version preflight) exit 1 with their own message; the version preflight also
// writes version-preflight.json with status 'blocked-version-state'.
//
// Integration points, recorded rather than hidden:
//  * eng/harness/ReleaseProtocol.fs owns the step model; this module reads the step table
//    through ReleaseProtocol.readProtocol and releaseSteps.
//  * eng/harness/ReleaseVerify.fs is the closing audit; build.fsx wires it into this module
//    through `verifierRunner`, which runs it in process rather than through a child pwsh
//    (the PowerShell original re-invoked pwsh through its own executable path, which cannot
//    resolve on this host).
// No pwsh, python3, uv or node is invoked from this module.

open System
open System.Collections.Generic
open System.Diagnostics
open System.Globalization
open System.IO
open System.Net.Http
open System.Runtime.InteropServices
open System.Security.Cryptography
open System.Text
open System.Text.Encodings.Web
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
          "Run the FunnySharp release candidate pipeline from one source fingerprint and record"
          "immutable execution evidence."
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

// ---- JSON construction helpers ----

let private utf8NoBom = UTF8Encoding(false)
// ConvertTo-Json escapes neither '+' nor '\''; the relaxed encoder matches its output
// for these evidence files, which no HTML consumer reads.
let private jsonOptions =
    JsonSerializerOptions(WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping)

let private jstr (value: string) : JsonValue =
    Option.ofObj (JsonValue.Create value)
    |> Option.defaultWith (fun () -> failwith "JsonValue.Create returned null for a string")

let private jint (value: int) : JsonValue = JsonValue.Create value
let private jbool (value: bool) : JsonValue = JsonValue.Create value

/// F# cannot pass a literal null to a JsonObject setter; an uninitialized node
/// serializes as JSON null.
let private jsonNull: JsonNode = Unchecked.defaultof<JsonNode>

let private stringArray (values: string list) : JsonArray =
    let array = JsonArray()

    for value in values do
        array.Add(jstr value)

    array

let private optionalString (value: string option) : JsonNode =
    match value with
    | Some text -> jstr text :> JsonNode
    | None -> jsonNull

let private writeJsonFile (node: JsonNode) (path: string) : unit =
    File.WriteAllText(path, node.ToJsonString(jsonOptions) + "\n", utf8NoBom)

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

let private stringList (value: JsonElement option) : string list =
    match value with
    | Some element when element.ValueKind = JsonValueKind.Array ->
        element.EnumerateArray() |> Seq.map stringValue |> List.ofSeq
    | Some element when element.ValueKind = JsonValueKind.String -> [ stringValue element ]
    | _ -> []

let private intValue (value: JsonElement option) : int option =
    match value with
    | Some element when element.ValueKind = JsonValueKind.Number ->
        let mutable parsed = 0
        if element.TryGetInt32(&parsed) then Some parsed else None
    | _ -> None

// ---- SHA-256 helpers ----

let private sha256HexBytes (bytes: byte array) : string =
    Convert.ToHexString(SHA256.HashData bytes).ToLowerInvariant()

let private sha256HexFile (path: string) : string =
    use stream = File.OpenRead path
    Convert.ToHexString(SHA256.HashData stream).ToLowerInvariant()

let private sha256HexText (text: string) : string =
    sha256HexBytes (Encoding.UTF8.GetBytes text)

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

type ReleaseStep =
    { Name: string
      FileName: string
      WorkingDirectory: string
      Arguments: string list }

/// The protocol the runner executes: the step table comes from ReleaseProtocol.fs.
type ReleaseProtocolApi =
    { AssertAttemptId: string -> unit
      AssertNewAttemptPath: string -> string -> string -> string -> string
      ValidatedProjectOutputDirectories: string -> string list -> string list
      StepPrefix: int -> string -> string
      LoadSteps: string -> string -> (string * string) list -> ReleaseStep list }

let private assertAttemptId (attemptId: string) : unit =
    if not (Regex.IsMatch(attemptId, "^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$")) then
        raise (
            InvalidOperationException(
                sprintf "AttemptId '%s' must match ^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$." attemptId
            )
        )

let private assertNewReleaseAttemptPath
    (path: string)
    (artifactsDirectory: string)
    (commit: string)
    (attemptId: string)
    : string =
    assertAttemptId attemptId

    if not (Regex.IsMatch(commit, "^[0-9a-fA-F]{40}$")) then
        raise (InvalidOperationException(sprintf "Commit '%s' must be a full 40-character SHA." commit))

    let artifacts = Path.GetFullPath(artifactsDirectory).TrimEnd('\\', '/')
    let expected = Path.GetFullPath(Path.Combine(artifacts, "release-candidate", commit, attemptId))
    let actual = Path.GetFullPath path

    if not (actual.Equals(expected, pathComparison ())) then
        raise (
            InvalidOperationException(
                sprintf "OutputDirectory must equal '%s' for this candidate and attempt; got '%s'." expected actual
            )
        )

    if Directory.Exists actual || File.Exists actual then
        raise (InvalidOperationException(sprintf "Release attempt path already exists and is immutable: '%s'." actual))

    let relative = Path.GetRelativePath(artifacts, actual)
    let mutable current = artifacts

    for segment in relative.Split([| '\\'; '/' |], StringSplitOptions.RemoveEmptyEntries) do
        current <- Path.Combine(current, segment)

        if Directory.Exists current || File.Exists current then
            let attributes = File.GetAttributes current

            if attributes.HasFlag FileAttributes.ReparsePoint then
                raise (InvalidOperationException(sprintf "Release attempt path cannot contain a reparse point: '%s'." current))

            let resolved = resolveDirectoryPath current

            if not (pathAtOrWithin resolved artifacts) then
                raise (InvalidOperationException(sprintf "Release attempt path escapes '%s': '%s'." artifacts resolved))

            current <- resolved

    actual

let private assertRelativePath (path: string) (description: string) : unit =
    if Path.IsPathFullyQualified path || (path.Split([| '\\'; '/' |]) |> Array.contains "..") then
        raise (InvalidOperationException(sprintf "%s must be repository-relative: '%s'." description path))

let private validatedProjectOutputDirectories
    (repositoryRoot: string)
    (projectFiles: string list)
    : string list =
    let root = Path.GetFullPath(repositoryRoot).TrimEnd('\\', '/')
    let outputs = ResizeArray<string>()

    for projectFile in projectFiles |> List.distinct |> List.sort do
        assertRelativePath projectFile "Project path"
        let projectPath = Path.GetFullPath(Path.Combine(root, projectFile))

        if not (pathAtOrWithin projectPath root) || not (File.Exists projectPath) then
            raise (
                FileNotFoundException(
                    sprintf "Tracked project was not found inside the repository: '%s'." projectFile
                )
            )

        let projectDirectory =
            match Path.GetDirectoryName projectPath with
            | null -> raise (InvalidOperationException(sprintf "Project path has no directory: '%s'." projectPath))
            | directory -> directory

        for name in [ "bin"; "obj" ] do
            let output = Path.GetFullPath(Path.Combine(projectDirectory, name))

            if
                not (pathAtOrWithin output projectDirectory)
                || output.Equals(projectDirectory, pathComparison ())
            then
                raise (
                    InvalidOperationException(
                        sprintf "Generated output path is not a direct child of '%s': '%s'." projectDirectory output
                    )
                )

            if Directory.Exists output || File.Exists output then
                let attributes = File.GetAttributes output

                if attributes.HasFlag FileAttributes.ReparsePoint then
                    raise (InvalidOperationException(sprintf "Generated output path cannot be a reparse point: '%s'." output))

            outputs.Add output

    List.ofSeq outputs

let private stepPrefix (ordinal: int) (name: string) : string = sprintf "%02d-%s" ordinal name

/// The step table comes from eng/harness/ReleaseProtocol.fs, which owns the protocol model. The
/// path and prefix helpers stay local: they were verified against the same PowerShell source, and
/// swapping them would re-open ReleaseRunTests' message-level assertions for no behaviour change.
let private loadStepsFromProtocol
    (protocolPath: string)
    (mode: string)
    (tokens: (string * string) list)
    : ReleaseStep list =
    match ReleaseProtocol.readProtocol protocolPath with
    | Error error -> raise (InvalidOperationException error.Message)
    | Ok protocol ->
        match ReleaseProtocol.releaseSteps protocol mode (Map.ofList tokens) with
        | Error error -> raise (InvalidOperationException error.Message)
        | Ok steps ->
            let tokenMap = Map.ofList tokens

            let expand (value: string) =
                match ReleaseProtocol.expandProtocolValue tokenMap value with
                | Ok expanded -> expanded
                | Error error -> raise (InvalidOperationException error.Message)

            steps
            |> List.map (fun step ->
                { Name = step.Name
                  FileName = expand step.FileName
                  WorkingDirectory = expand step.WorkingDirectory
                  Arguments = step.Arguments |> List.map expand })

let defaultProtocolApi: ReleaseProtocolApi =
    { AssertAttemptId = assertAttemptId
      AssertNewAttemptPath = assertNewReleaseAttemptPath
      ValidatedProjectOutputDirectories = validatedProjectOutputDirectories
      StepPrefix = stepPrefix
      LoadSteps = loadStepsFromProtocol }

// ---- Evidence records ----

type VersionCheck =
    { CheckedAtUtc: string
      Feed: string
      PackageBaseAddress: string
      PackageId: string
      Version: string
      Status: string
      StatusCode: int
      ResponseSha256: string
      Versions: string list }

type VersionStateResult = { Status: string; Sha256: string }

type FingerprintFile = { Path: string; Sha256: string }

type SourceFingerprint =
    { SchemaVersion: int
      Algorithm: string
      FileCount: int
      Digest: string
      Files: FingerprintFile list }

type CleanupEntry =
    { Path: string
      Existed: bool
      Removed: bool }

type CommandReceipt =
    { SchemaVersion: int
      Name: string
      StartedAtUtc: string
      CompletedAtUtc: string
      FileName: string
      Arguments: string list
      WorkingDirectory: string
      ExitCode: int
      StandardOutputLog: string
      StandardErrorLog: string
      StandardOutputSha256: string
      StandardErrorSha256: string }

let private versionCheckJson (check: VersionCheck) : JsonNode =
    let node = JsonObject()
    node.["checkedAtUtc"] <- jstr check.CheckedAtUtc
    node.["feed"] <- jstr check.Feed
    node.["packageBaseAddress"] <- jstr check.PackageBaseAddress
    node.["packageId"] <- jstr check.PackageId
    node.["version"] <- jstr check.Version
    node.["status"] <- jstr check.Status
    node.["statusCode"] <- jint check.StatusCode
    node.["responseSha256"] <- jstr check.ResponseSha256
    node.["versions"] <- stringArray check.Versions
    node :> JsonNode

let private versionStateJson
    (status: string)
    (candidateCommit: string)
    (attemptId: string)
    (packageVersion: string)
    (checks: VersionCheck list)
    (error: string option)
    : JsonObject =
    let node = JsonObject()
    node.["schemaVersion"] <- jint 1
    node.["status"] <- jstr status
    node.["candidateCommit"] <- jstr candidateCommit
    node.["attemptId"] <- jstr attemptId
    node.["packageVersion"] <- jstr packageVersion
    let checksArray = JsonArray()

    for check in checks do
        checksArray.Add(versionCheckJson check)

    node.["checks"] <- checksArray

    match error with
    | Some message -> node.["error"] <- jstr message
    | None -> ()

    node

let private fingerprintFileJson (file: FingerprintFile) : JsonNode =
    let node = JsonObject()
    node.["path"] <- jstr file.Path
    node.["sha256"] <- jstr file.Sha256
    node :> JsonNode

let private sourceFingerprintJson (fingerprint: SourceFingerprint) : JsonNode =
    let node = JsonObject()
    node.["schemaVersion"] <- jint fingerprint.SchemaVersion
    node.["algorithm"] <- jstr fingerprint.Algorithm
    node.["fileCount"] <- jint fingerprint.FileCount
    node.["digest"] <- jstr fingerprint.Digest
    let filesArray = JsonArray()

    for file in fingerprint.Files do
        filesArray.Add(fingerprintFileJson file)

    node.["files"] <- filesArray
    node :> JsonNode

let private cleanupEntryJson (entry: CleanupEntry) : JsonNode =
    let node = JsonObject()
    node.["path"] <- jstr entry.Path
    node.["existed"] <- jbool entry.Existed
    node.["removed"] <- jbool entry.Removed
    node :> JsonNode

let private receiptJson (receipt: CommandReceipt) : JsonNode =
    let node = JsonObject()
    node.["schemaVersion"] <- jint receipt.SchemaVersion
    node.["name"] <- jstr receipt.Name
    node.["startedAtUtc"] <- jstr receipt.StartedAtUtc
    node.["completedAtUtc"] <- jstr receipt.CompletedAtUtc
    node.["fileName"] <- jstr receipt.FileName
    node.["arguments"] <- stringArray receipt.Arguments
    node.["workingDirectory"] <- jstr receipt.WorkingDirectory
    node.["exitCode"] <- jint receipt.ExitCode
    node.["standardOutputLog"] <- jstr receipt.StandardOutputLog
    node.["standardErrorLog"] <- jstr receipt.StandardErrorLog
    node.["standardOutputSha256"] <- jstr receipt.StandardOutputSha256
    node.["standardErrorSha256"] <- jstr receipt.StandardErrorSha256
    node :> JsonNode

let private equivalentFingerprints (left: SourceFingerprint) (right: SourceFingerprint) : bool =
    left.SchemaVersion = 1
    && right.SchemaVersion = 1
    && left.Algorithm = "sha256"
    && right.Algorithm = "sha256"
    && left.FileCount = right.FileCount
    && left.Digest = right.Digest

// ---- Collaborators ----

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

type VerifierRequest =
    { RepositoryRoot: string
      VerificationDirectory: string
      PackagesDirectory: string
      CompatibilityEvidencePath: string
      CompatibilityRuntimeIdentifier: string
      CompatibilityPackageFeed: string
      ExecutionEvidenceDirectory: string
      SkipBenchmarks: bool }

/// The release verifier's argument list. The ported F# verifier consumes the same
/// parameter names as Verify-Release.ps1; the pwsh-only `-NoProfile -File <path>`
/// prefix is gone because the verifier is launched as a plain process.
let verifierArguments (request: VerifierRequest) : string list =
    [ "-RepositoryRoot"; request.RepositoryRoot
      "-OutputDirectory"; request.VerificationDirectory
      "-PackageDirectory"; request.PackagesDirectory
      "-CompatibilityEvidencePath"; request.CompatibilityEvidencePath
      "-CompatibilityRuntimeIdentifier"; request.CompatibilityRuntimeIdentifier
      "-CompatibilityPackageFeed"; request.CompatibilityPackageFeed
      "-ExecutionEvidenceDirectory"; request.ExecutionEvidenceDirectory
      "-Clean" ]
    @ (if request.SkipBenchmarks then [ "-SkipBenchmarks" ] else [])

/// The verifier launch. Integration points this at lane C's ported F# verifier;
/// tests inject a stub. A `None` verifier fails the run closed before it starts.
let mutable verifierRunner: (string list -> string -> ProcessResult) option = None

type Collaborators =
    { Runner: CommandRunner
      HttpGet: HttpGet
      Verifier: (string list -> string -> ProcessResult) option
      Protocol: ReleaseProtocolApi
      UtcNow: unit -> DateTime }

let defaultCollaborators () : Collaborators =
    { Runner = runChildProcess
      HttpGet = httpGet
      Verifier = verifierRunner
      Protocol = defaultProtocolApi
      UtcNow = fun () -> DateTime.UtcNow }

// ---- Git, package versions, fingerprint, cleanup, version state ----

let private getGitText (runner: CommandRunner) (arguments: string list) (root: string) : string =
    let result = runner "git" arguments Map.empty root

    if result.ExitCode <> 0 then
        raise (
            InvalidOperationException(
                sprintf "git %s failed: %s" (String.Join(" ", arguments)) (result.Stderr.Trim())
            )
        )

    result.Stdout.Trim()

let private getSourceFingerprint (runner: CommandRunner) (root: string) : SourceFingerprint =
    let result = runner "git" [ "ls-files"; "--cached"; "--others"; "--exclude-standard"; "-z" ] Map.empty root

    if result.ExitCode <> 0 then
        raise (
            InvalidOperationException(
                sprintf "git ls-files failed with exit code %d: %s" result.ExitCode (result.Stderr.Trim())
            )
        )

    let files =
        result.Stdout.Split([| '\000' |])
        |> Array.filter (fun relativePath -> relativePath <> "")
        |> Array.map (fun relativePath ->
            let fullPath = Path.GetFullPath(Path.Combine(root, relativePath))

            if not (File.Exists fullPath) then
                raise (FileNotFoundException(sprintf "Source file listed by git was not found: '%s'." relativePath, fullPath))

            { Path = relativePath.Replace('\\', '/')
              Sha256 = sha256HexFile fullPath })
        |> Array.toList
        // Invariant-culture, case-insensitive ordering: the release verifier sorts the same file set
        // the same way (ReleaseVerifySource.fs), and the PowerShell original's Sort-Object is
        // culture-aware, so an ordinal sort here would produce a digest the verifier rejects.
        |> List.sortWith (fun a b -> StringComparer.InvariantCultureIgnoreCase.Compare(a.Path, b.Path))

    let canonical = StringBuilder()

    for file in files do
        canonical.Append(file.Path).Append('\000').Append(file.Sha256).Append('\n') |> ignore

    { SchemaVersion = 1
      Algorithm = "sha256"
      FileCount = files.Length
      Digest = sha256HexText (canonical.ToString())
      Files = files }

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
    (utcNow: unit -> DateTime)
    (feed: string)
    (packageId: string)
    (version: string)
    : VersionCheck =
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

    { CheckedAtUtc = DateTimeOffset(utcNow()).ToString("O", CultureInfo.InvariantCulture)
      Feed = feed
      PackageBaseAddress = baseAddress
      PackageId = packageId
      Version = version
      Status = "absent"
      StatusCode = statusCode
      ResponseSha256 = sha256HexText content
      Versions = versions }

let private removeProjectGeneratedOutputs
    (protocol: ReleaseProtocolApi)
    (runner: CommandRunner)
    (root: string)
    : CleanupEntry list =
    let projectListText = getGitText runner [ "ls-files"; "*.csproj" ] root

    let projectFiles =
        projectListText.Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.filter (fun path -> not (String.IsNullOrWhiteSpace path))
        |> Array.toList

    protocol.ValidatedProjectOutputDirectories root projectFiles
    |> List.map (fun path ->
        let existed = Directory.Exists path || File.Exists path

        if existed then
            if Directory.Exists path then
                Directory.Delete(path, true)
            elif File.Exists path then
                File.Delete path

        { Path = Path.GetRelativePath(root, path).Replace('\\', '/')
          Existed = existed
          Removed = not (Directory.Exists path || File.Exists path) })

// ---- CLI ----

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

// ---- Run ----

let private runRelease
    (stdout: TextWriter)
    (stderr: TextWriter)
    (startDirectory: string)
    (collaborators: Collaborators)
    (options: CliOptions)
    (attemptId: string)
    : int =
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
            getGitText runner [ "status"; "--porcelain=v1"; "--untracked-files=all" ] repositoryRoot

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
        let receiptsDirectory = Path.Combine(outputDirectory, "receipts")
        let packagesDirectory = Path.Combine(outputDirectory, "packages")
        let benchmarkArtifactsDirectory = Path.Combine(outputDirectory, "benchmark-artifacts")
        let compatibilityOutputDirectory = Path.Combine(outputDirectory, "compatibility-run")
        let verificationDirectory = Path.Combine(outputDirectory, "release-evidence")
        let nugetPackagesDirectory = Path.Combine(outputDirectory, "nuget-packages")

        for directory in [ logsDirectory; receiptsDirectory; packagesDirectory; nugetPackagesDirectory ] do
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

        let versionChecks = ResizeArray<VersionCheck>()

        try
            for feed in distributionFeeds do
                for packageId in [ "FunnySharp"; "FunnySharp.AspNetCore" ] do
                    versionChecks.Add(
                        getPackageVersionState collaborators.HttpGet collaborators.UtcNow feed packageId coreVersion
                    )
        with ex ->
            writeJsonFile
                (versionStateJson
                    "blocked-version-state"
                    candidateCommit
                    attemptId
                    coreVersion
                    (List.ofSeq versionChecks)
                    (Some ex.Message))
                (Path.Combine(outputDirectory, "version-preflight.json"))

            reraise ()

        writeJsonFile
            (versionStateJson "passed" candidateCommit attemptId coreVersion (List.ofSeq versionChecks) None)
            (Path.Combine(outputDirectory, "version-preflight.json"))

        let generatedCleanup = removeProjectGeneratedOutputs collaborators.Protocol runner repositoryRoot

        // The protocol's own verdicts and the release verifier both match English markers
        // ('Build succeeded.', '0 Warning(s)', 'Test run summary:'), so every child must emit
        // English UI text. The PowerShell original did not force this: on this zh-CN host its
        // captured build log reads '已成功生成。' and the audit that follows cannot see the marker.
        let childEnvironment =
            Map.ofList
                [ "NUGET_PACKAGES", nugetPackagesDirectory
                  "FUNNYSHARP_CANDIDATE_COMMIT", candidateCommit
                  "DOTNET_CLI_UI_LANGUAGE", "en"
                  "DOTNET_NOLOGO", "1" ]

        let protocolPath = Path.Combine(repositoryRoot, "eng/release-protocol.json")
        let mode = if options.SkipBenchmarks then "benchmarkSkipped" else "full"

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

        let releaseSteps = collaborators.Protocol.LoadSteps protocolPath mode tokens
        let expectedCandidateCommands = releaseSteps |> List.map (fun step -> step.Name)
        let protocolSha256 = sha256HexFile protocolPath
        let receipts = ResizeArray<CommandReceipt>()
        let sourceFingerprintBefore = getSourceFingerprint runner repositoryRoot
        let mutable sourceFingerprintAfter: SourceFingerprint option = None
        let mutable versionFinal: VersionStateResult option = None
        let mutable runFailure: string option = None
        let mutable executionEvidenceFrozen = false
        let now () = collaborators.UtcNow()

        let writeExecutionEvidence () =
            let succeeded =
                runFailure.IsNone
                && receipts.Count = expectedCandidateCommands.Length
                && receipts |> Seq.forall (fun receipt -> receipt.ExitCode = 0)
                && (versionFinal |> Option.exists (fun result -> result.Status = "passed"))
                && (sourceFingerprintAfter
                    |> Option.exists (fun after -> equivalentFingerprints sourceFingerprintBefore after))

            let node = JsonObject()
            node.["schemaVersion"] <- jint 2
            node.["succeeded"] <- jbool succeeded
            node.["attemptId"] <- jstr attemptId
            node.["mode"] <- jstr mode
            node.["candidateCommit"] <- jstr candidateCommit
            node.["repositoryRoot"] <- jstr repositoryRoot
            node.["outputDirectory"] <- jstr outputDirectory
            let protocolNode = JsonObject()
            protocolNode.["path"] <- jstr "eng/release-protocol.json"
            protocolNode.["sha256"] <- jstr protocolSha256
            node.["protocol"] <- protocolNode
            node.["versionPreflight"] <- jstr "version-preflight.json"

            node.["versionPreflightSha256"] <-
                jstr (sha256HexFile (Path.Combine(outputDirectory, "version-preflight.json")))

            node.["versionFinal"] <-
                (match versionFinal with
                 | Some _ -> jstr "version-final.json" :> JsonNode
                 | None -> jsonNull)

            node.["versionFinalSha256"] <-
                (match versionFinal with
                 | Some result -> jstr result.Sha256 :> JsonNode
                 | None -> jsonNull)

            let cleanupArray = JsonArray()

            for entry in generatedCleanup do
                cleanupArray.Add(cleanupEntryJson entry)

            node.["generatedCleanup"] <- cleanupArray
            node.["nugetPackagesDirectory"] <- jstr nugetPackagesDirectory
            node.["isolatedNuGetCache"] <- jbool (pathAtOrWithin nugetPackagesDirectory outputDirectory)
            let commandsArray = JsonArray()

            for command in expectedCandidateCommands do
                commandsArray.Add(jstr command)

            node.["candidateCommands"] <- commandsArray
            node.["sourceFingerprintBefore"] <- sourceFingerprintJson sourceFingerprintBefore

            node.["sourceFingerprintAfter"] <-
                (match sourceFingerprintAfter with
                 | Some after -> sourceFingerprintJson after
                 | None -> jsonNull)

            let receiptsArray = JsonArray()

            for receipt in receipts do
                receiptsArray.Add(receiptJson receipt)

            node.["commands"] <- receiptsArray
            writeJsonFile node (Path.Combine(outputDirectory, "execution-evidence.json"))

        let writeRunOutcome () =
            let executionEvidencePath = Path.Combine(outputDirectory, "execution-evidence.json")
            let releaseEvidencePath = Path.Combine(verificationDirectory, "release-evidence.json")

            let evidencePointer (path: string) (relativePath: string) : JsonNode =
                if File.Exists path then
                    let pointer = JsonObject()
                    pointer.["path"] <- jstr relativePath
                    pointer.["sha256"] <- jstr (sha256HexFile path)
                    pointer :> JsonNode
                else
                    jsonNull

            let node = JsonObject()
            node.["schemaVersion"] <- jint 1
            node.["succeeded"] <- jbool runFailure.IsNone
            node.["attemptId"] <- jstr attemptId
            node.["candidateCommit"] <- jstr candidateCommit

            node.["executionEvidence"] <-
                evidencePointer executionEvidencePath "execution-evidence.json"

            node.["releaseEvidence"] <-
                evidencePointer releaseEvidencePath "release-evidence/release-evidence.json"

            node.["error"] <- optionalString runFailure
            writeJsonFile node (Path.Combine(outputDirectory, "release-outcome.json"))

        let invokeReleaseStep (ordinal: int) (step: ReleaseStep) : unit =
            let prefix = collaborators.Protocol.StepPrefix ordinal step.Name
            let standardOutputRelative = "logs/" + prefix + ".stdout.log"
            let standardErrorRelative = "logs/" + prefix + ".stderr.log"
            let receiptRelative = "receipts/" + prefix + ".json"
            let standardOutputPath = Path.Combine(outputDirectory, standardOutputRelative)
            let standardErrorPath = Path.Combine(outputDirectory, standardErrorRelative)
            let receiptPath = Path.Combine(outputDirectory, receiptRelative)
            let startedAtUtc = now().ToString("O", CultureInfo.InvariantCulture)

            let result: ProcessResult =
                try
                    runner step.FileName step.Arguments childEnvironment step.WorkingDirectory
                with ex ->
                    { ExitCode = -1
                      Stdout = ""
                      Stderr = ex.ToString() }

            File.WriteAllText(standardOutputPath, result.Stdout, utf8NoBom)
            File.WriteAllText(standardErrorPath, result.Stderr, utf8NoBom)

            let receipt =
                { SchemaVersion = 1
                  Name = step.Name
                  StartedAtUtc = startedAtUtc
                  CompletedAtUtc = now().ToString("O", CultureInfo.InvariantCulture)
                  FileName = step.FileName
                  Arguments = step.Arguments
                  WorkingDirectory = step.WorkingDirectory
                  ExitCode = result.ExitCode
                  StandardOutputLog = standardOutputRelative
                  StandardErrorLog = standardErrorRelative
                  StandardOutputSha256 = sha256HexFile standardOutputPath
                  StandardErrorSha256 = sha256HexFile standardErrorPath }

            writeJsonFile (receiptJson receipt) receiptPath
            receipts.Add receipt
            writeExecutionEvidence ()

            if result.ExitCode <> 0 then
                raise (
                    InvalidOperationException(
                        sprintf
                            "Release command '%s' failed with exit code %d. See '%s' and '%s'."
                            step.Name
                            result.ExitCode
                            standardOutputPath
                            standardErrorPath
                    )
                )

        try
            for index in 0 .. releaseSteps.Length - 1 do
                invokeReleaseStep (index + 1) releaseSteps.[index]

            sourceFingerprintAfter <- Some(getSourceFingerprint runner repositoryRoot)

            if not (equivalentFingerprints sourceFingerprintBefore sourceFingerprintAfter.Value) then
                raise (InvalidOperationException "Source files changed while the release candidate pipeline ran.")

            writeExecutionEvidence ()

            let versionFinalChecks = ResizeArray<VersionCheck>()

            try
                for feed in distributionFeeds do
                    for packageId in [ "FunnySharp"; "FunnySharp.AspNetCore" ] do
                        versionFinalChecks.Add(
                            getPackageVersionState collaborators.HttpGet collaborators.UtcNow feed packageId coreVersion
                        )
            with ex ->
                let finalPath = Path.Combine(outputDirectory, "version-final.json")

                writeJsonFile
                    (versionStateJson
                        "blocked-version-state"
                        candidateCommit
                        attemptId
                        coreVersion
                        (List.ofSeq versionFinalChecks)
                        (Some ex.Message))
                    finalPath

                versionFinal <- Some { Status = "blocked-version-state"; Sha256 = sha256HexFile finalPath }
                reraise ()

            let finalPath = Path.Combine(outputDirectory, "version-final.json")

            writeJsonFile
                (versionStateJson "passed" candidateCommit attemptId coreVersion (List.ofSeq versionFinalChecks) None)
                finalPath

            versionFinal <- Some { Status = "passed"; Sha256 = sha256HexFile finalPath }
            sourceFingerprintAfter <- Some(getSourceFingerprint runner repositoryRoot)

            if not (equivalentFingerprints sourceFingerprintBefore sourceFingerprintAfter.Value) then
                raise (InvalidOperationException "Source files changed before final release verification.")

            writeExecutionEvidence ()
            executionEvidenceFrozen <- true

            let request =
                { RepositoryRoot = repositoryRoot
                  VerificationDirectory = verificationDirectory
                  PackagesDirectory = packagesDirectory
                  CompatibilityEvidencePath = Path.Combine(compatibilityOutputDirectory, "compatibility-results.json")
                  CompatibilityRuntimeIdentifier = compatibilityRid
                  CompatibilityPackageFeed = compatibilityFeed
                  ExecutionEvidenceDirectory = outputDirectory
                  SkipBenchmarks = options.SkipBenchmarks }

            let verificationArguments = verifierArguments request

            let verificationResult =
                match collaborators.Verifier with
                | Some run -> run verificationArguments repositoryRoot
                | None ->
                    { ExitCode = 2
                      Stdout = ""
                      Stderr = "the release verifier is not wired into this build." }

            File.WriteAllText(
                Path.Combine(outputDirectory, "release-verifier.stdout.log"),
                verificationResult.Stdout,
                utf8NoBom
            )

            File.WriteAllText(
                Path.Combine(outputDirectory, "release-verifier.stderr.log"),
                verificationResult.Stderr,
                utf8NoBom
            )

            if verificationResult.ExitCode <> 0 then
                raise (
                    InvalidOperationException(
                        sprintf "Release verifier failed with exit code %d." verificationResult.ExitCode
                    )
                )
        with ex ->
            runFailure <- Some ex.Message

        // finally-equivalent: recompute the fingerprint if needed, freeze the evidence if
        // the pipeline never reached that point, and always write the outcome.
        if sourceFingerprintAfter.IsNone then
            try
                sourceFingerprintAfter <- Some(getSourceFingerprint runner repositoryRoot)
            with ex ->
                if runFailure.IsNone then
                    runFailure <- Some ex.Message

        if not executionEvidenceFrozen then
            writeExecutionEvidence ()

        writeRunOutcome ()

        match runFailure with
        | Some message ->
            stderr.WriteLine("Release run failed: " + message)
            1
        | None ->
            stdout.WriteLine("Release run passed. Execution evidence: " + outputDirectory)
            0
    with ex ->
        // Precondition failure before the run phase: no outcome file, exit 1 like the script.
        stderr.WriteLine(ex.Message)
        1

/// Run the release pipeline writing to the supplied writers. `startDirectory`
/// seeds the default repository-root lookup so tests can inject a scratch root.
let mainWith
    (stdout: TextWriter)
    (stderr: TextWriter)
    (startDirectory: string)
    (collaborators: Collaborators)
    (argv: string list)
    : int =
    match parseArgs argv with
    | Error message ->
        stderr.WriteLine usageLine
        stderr.WriteLine("Run-Release.ps1: error: " + message)
        2
    | Ok options when options.Help ->
        stdout.WriteLine helpText
        0
    | Ok options ->
        match options.AttemptId with
        | Some attemptId when not (String.IsNullOrWhiteSpace attemptId) ->
            match collaborators.Verifier with
            | None ->
                stderr.WriteLine
                    "Run-Release.ps1: error: the release verifier is not wired into this build."

                2
            | Some _ -> runRelease stdout stderr startDirectory collaborators options attemptId
        | _ ->
            stderr.WriteLine usageLine
            stderr.WriteLine "Run-Release.ps1: error: the -AttemptId parameter is required."
            2

/// Entry point mirroring eng/Run-Release.ps1's parameter surface.
let main (argv: string array) : int =
    mainWith Console.Out Console.Error Environment.CurrentDirectory (defaultCollaborators ()) (List.ofArray argv)
