module FunnySharp.Harness.ReleaseProtocol

// Release step parsing, token expansion, attempt paths and package version checks.

open System
open System.IO
open System.Text.Json
open System.Text.RegularExpressions
open FunnySharp.Harness.Output

// ---------------------------------------------------------------------------
// Contract constants
// ---------------------------------------------------------------------------

[<Literal>]
let ProtocolRelativePath = "eng/release-protocol.json"

[<Literal>]
let SchemaVersion = 1

[<Literal>]
let FullMode = "full"

[<Literal>]
let BenchmarkSkippedMode = "benchmarkSkipped"

[<Literal>]
let ReleaseCandidateDirectoryName = "release-candidate"

/// The attempt id constraint from eng/ReleaseProtocol.psm1:64-66.
[<Literal>]
let AttemptIdPattern = "^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$"

/// The placeholder names the runner substitutes into a step's values. The set is the
/// token map of eng/Run-Release.ps1:486-496; any `{name}` left after substitution is an
/// error (:370-372).
let placeholders: string list =
    [ "root"
      "compatibilityFeed"
      "packages"
      "benchmarkRoot"
      "benchmarkArtifacts"
      "benchmarkResults"
      "performanceObservationProposal"
      "compatibilityOutput"
      "compatibilityRid" ]

let private unknownTokenPattern = Regex(@"\{[A-Za-z][A-Za-z0-9]*\}")

// ---------------------------------------------------------------------------
// Parsed model
// ---------------------------------------------------------------------------

/// One step definition exactly as read from eng/release-protocol.json.
type StepDefinition =
    { FileName: string
      WorkingDirectory: string
      Arguments: string list }

/// The parsed protocol: the schema version, each mode's ordered step names, and the
/// step definition table.
type Protocol =
    { SchemaVersion: int
      Modes: Map<string, string list>
      Steps: Map<string, StepDefinition> }

/// One materialised step: its name plus the token-expanded command to run.
type ReleaseStep =
    { Name: string
      FileName: string
      WorkingDirectory: string
      Arguments: string list }

// ---------------------------------------------------------------------------
// JSON helpers (null-safe under <Nullable>enable)
// ---------------------------------------------------------------------------

let private tryProperty (name: string) (element: JsonElement) : JsonElement option =
    let mutable value = Unchecked.defaultof<JsonElement>

    if element.ValueKind = JsonValueKind.Object && element.TryGetProperty(name, &value) then
        Some value
    else
        None

/// PowerShell's `[string] $value` cast for a JSON scalar: strings verbatim, other
/// scalars via their invariant text, missing structures as the empty string.
let private jsonText (element: JsonElement) : string =
    if element.ValueKind = JsonValueKind.String then
        Option.ofObj (element.GetString()) |> Option.defaultValue ""
    else
        element.ToString()

let private jsonStringArray (element: JsonElement) : string list =
    match element.ValueKind with
    | JsonValueKind.Array -> element.EnumerateArray() |> Seq.map jsonText |> List.ofSeq
    | JsonValueKind.String -> [ jsonText element ]
    | _ -> []

// ---------------------------------------------------------------------------
// Reading the protocol
// ---------------------------------------------------------------------------

let private parseStepDefinitions (root: JsonElement) : Map<string, StepDefinition> =
    match tryProperty "steps" root with
    | Some steps when steps.ValueKind = JsonValueKind.Object ->
        [ for definition in steps.EnumerateObject() do
              let fileName =
                  definition.Value |> tryProperty "fileName" |> Option.map jsonText |> Option.defaultValue ""

              let workingDirectory =
                  definition.Value
                  |> tryProperty "workingDirectory"
                  |> Option.map jsonText
                  |> Option.defaultValue ""

              let arguments =
                  definition.Value
                  |> tryProperty "arguments"
                  |> Option.map jsonStringArray
                  |> Option.defaultValue []

              yield definition.Name, { FileName = fileName; WorkingDirectory = workingDirectory; Arguments = arguments } ]
        |> Map.ofList
    | _ -> Map.empty

/// Read and validate eng/release-protocol.json, mirroring Read-ReleaseProtocol.
let readProtocol (path: string) : Result<Protocol, HarnessError> =
    if not (File.Exists path) then
        failError (sprintf "Release protocol was not found: '%s'." path)
    else
        try
            use document = JsonDocument.Parse(File.ReadAllText path)
            let root = document.RootElement

            let schemaVersion =
                match tryProperty "schemaVersion" root with
                | Some value when value.ValueKind = JsonValueKind.Number ->
                    let mutable parsed = 0
                    if value.TryGetInt32(&parsed) then parsed else 0
                | Some value ->
                    match Int32.TryParse(jsonText value) with
                    | true, parsed -> parsed
                    | _ -> 0
                | None -> 0

            if schemaVersion <> SchemaVersion then
                failError "Release protocol must use schemaVersion 1."
            else
                let steps = parseStepDefinitions root

                let modeSteps (modeName: string) : string list =
                    root
                    |> tryProperty "modes"
                    |> Option.bind (tryProperty modeName)
                    |> Option.bind (tryProperty "steps")
                    |> Option.map jsonStringArray
                    |> Option.defaultValue []

                let validateMode (modeName: string) : Result<string list, HarnessError> =
                    let names = modeSteps modeName

                    if names.IsEmpty || (names |> List.distinct).Length <> names.Length then
                        failError (
                            sprintf "Release protocol mode '%s' has no steps or contains duplicates." modeName
                        )
                    else
                        match names |> List.tryFind (fun stepName -> not (Map.containsKey stepName steps)) with
                        | Some stepName ->
                            failError (
                                sprintf "Release protocol mode '%s' refers to undefined step '%s'." modeName stepName
                            )
                        | None -> Ok names

                validateMode FullMode
                |> Result.bind (fun fullNames ->
                    validateMode BenchmarkSkippedMode
                    |> Result.map (fun skippedNames ->
                        { SchemaVersion = schemaVersion
                          Modes = Map.ofList [ FullMode, fullNames; BenchmarkSkippedMode, skippedNames ]
                          Steps = steps }))
        with ex ->
            failError ex.Message

/// Read the protocol from a repository root.
let readProtocolFromRoot (repositoryRoot: string) : Result<Protocol, HarnessError> =
    readProtocol (Path.Combine(repositoryRoot, ProtocolRelativePath))

// ---------------------------------------------------------------------------
// Token expansion and step materialisation
// ---------------------------------------------------------------------------

/// Substitute every token then reject any unknown `{name}` left behind
/// (Expand-ProtocolValue, eng/Run-Release.ps1:358-373).
let expandProtocolValue (tokens: Map<string, string>) (value: string) : Result<string, HarnessError> =
    let mutable expanded = value

    for KeyValue(token, replacement) in tokens do
        expanded <- expanded.Replace("{" + token + "}", replacement)

    if unknownTokenPattern.IsMatch expanded then
        failError (sprintf "Release protocol value contains an unknown token: '%s'." expanded)
    else
        ok expanded

/// The protocol mode selected by the `-SkipBenchmarks` switch.
let modeFor (skipBenchmarks: bool) : string =
    if skipBenchmarks then BenchmarkSkippedMode else FullMode

/// Materialise one mode's ordered steps, expanding tokens in the definition.
let releaseSteps (protocol: Protocol) (mode: string) (tokens: Map<string, string>) : Result<ReleaseStep list, HarnessError> =
    match Map.tryFind mode protocol.Modes with
    | None -> failError (sprintf "Release protocol mode '%s' was not found." mode)
    | Some names ->
        names
        |> traverseResults (fun name ->
            match Map.tryFind name protocol.Steps with
            | None -> failError (sprintf "Release protocol mode '%s' refers to undefined step '%s'." mode name)
            | Some definition ->
                expandProtocolValue tokens definition.FileName
                |> Result.bind (fun fileName ->
                    expandProtocolValue tokens definition.WorkingDirectory
                    |> Result.bind (fun workingDirectory ->
                        definition.Arguments
                        |> traverseResults (expandProtocolValue tokens)
                        |> Result.map (fun arguments ->
                            { Name = name
                              FileName = fileName
                              WorkingDirectory = workingDirectory
                              Arguments = arguments }))))

// ---------------------------------------------------------------------------
// Attempt directory resolution
// ---------------------------------------------------------------------------

let artifactsDirectory (repositoryRoot: string) : string =
    Path.Combine(Path.GetFullPath repositoryRoot, "artifacts")

let private trimSeparators (path: string) : string = path.TrimEnd([| '\\'; '/' |])

let private pathComparison () : StringComparison =
    if OperatingSystem.IsWindows() then
        StringComparison.OrdinalIgnoreCase
    else
        StringComparison.Ordinal

/// Test-PathAtOrWithin (eng/ReleaseProtocol.psm1:12-22).
let pathAtOrWithin (childPath: string) (parentPath: string) : bool =
    let comparison = pathComparison()
    let separator = string Path.DirectorySeparatorChar
    let normalizedParent = trimSeparators parentPath + separator
    childPath.Equals(parentPath, comparison) || childPath.StartsWith(normalizedParent, comparison)

/// Validate the attempt id, mirroring Assert-ReleaseAttemptId.
let assertReleaseAttemptId (attemptId: string) : Result<string, HarnessError> =
    if Regex.IsMatch(attemptId, AttemptIdPattern) then
        ok attemptId
    else
        failError (sprintf "AttemptId '%s' must match %s." attemptId AttemptIdPattern)

/// The expected `<artifacts>/release-candidate/<commit>/<attemptId>` path.
let expectedAttemptPath (artifacts: string) (commit: string) (attemptId: string) : string =
    Path.GetFullPath(Path.Combine(trimSeparators artifacts, ReleaseCandidateDirectoryName, commit, attemptId))

/// Validate a new, immutable attempt path, mirroring Assert-NewReleaseAttemptPath
/// (eng/ReleaseProtocol.psm1:72-115): exact expected path, must not exist, no reparse
/// point, and no segment escaping the artifacts directory.
let assertNewReleaseAttemptPath
    (path: string)
    (artifacts: string)
    (commit: string)
    (attemptId: string)
    : Result<string, HarnessError> =
    assertReleaseAttemptId attemptId
    |> Result.bind (fun _ ->
        if not (Regex.IsMatch(commit, "^[0-9a-fA-F]{40}$")) then
            failError (sprintf "Commit '%s' must be a full 40-character SHA." commit)
        else
            let artifactsRoot = trimSeparators (Path.GetFullPath artifacts)
            let expected = expectedAttemptPath artifactsRoot commit attemptId
            let actual = Path.GetFullPath path

            if not (actual.Equals(expected, pathComparison())) then
                failError (
                    sprintf "OutputDirectory must equal '%s' for this candidate and attempt; got '%s'." expected actual
                )
            elif Directory.Exists actual || File.Exists actual then
                failError (sprintf "Release attempt path already exists and is immutable: '%s'." actual)
            else
                let relative = Path.GetRelativePath(artifactsRoot, actual)
                let segments = relative.Split([| '\\'; '/' |])
                let mutable current = artifactsRoot
                let mutable failure: HarnessError option = None

                for segment in segments do
                    if failure.IsNone then
                        current <- Path.Combine(current, segment)

                        if Directory.Exists current || File.Exists current then
                            let attributes =
                                if Directory.Exists current then
                                    DirectoryInfo(current).Attributes
                                else
                                    FileInfo(current).Attributes

                            if attributes.HasFlag FileAttributes.ReparsePoint then
                                failure <-
                                    Some(
                                        Errors.create (
                                            sprintf "Release attempt path cannot contain a reparse point: '%s'." current
                                        )
                                    )
                            else
                                let resolved = Path.GetFullPath current

                                if not (pathAtOrWithin resolved artifactsRoot) then
                                    failure <-
                                        Some(
                                            Errors.create (
                                                sprintf "Release attempt path escapes '%s': '%s'." artifactsRoot resolved
                                            )
                                        )
                                else
                                    current <- resolved

                match failure with
                | Some error -> Error error
                | None -> ok actual)

// ---------------------------------------------------------------------------
// Package version validation
// ---------------------------------------------------------------------------

/// Assert-PackageVersionAbsent (eng/ReleaseProtocol.psm1:155-167). `versions` is None
/// when the response could not prove the version is absent (the `$null` case).
let assertPackageVersionAbsent
    (packageId: string)
    (version: string)
    (versions: string list option)
    : Result<unit, HarnessError> =
    match versions with
    | None -> failError (sprintf "Package version state for '%s' is ambiguous." packageId)
    | Some values when
        values
        |> List.exists (fun value -> String.Equals(value, version, StringComparison.OrdinalIgnoreCase))
        ->
        failError (sprintf "Package '%s' already contains version '%s'." packageId version)
    | Some _ -> ok ()

/// The `{NN}-{name}` ordinal prefix, two digits and numbered from 01.
let stepPrefix (ordinal: int) (name: string) : string =
    sprintf "%02d-%s" ordinal name
