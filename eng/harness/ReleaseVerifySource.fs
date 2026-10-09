module FunnySharp.Harness.ReleaseVerifySource

// Behaviour-identical F# port of the source-, command- and benchmark-facing half
// of eng/Verify-Release.ps1 (lane C). This file owns path safety, the source
// fingerprint, the release-protocol command table, the captured-log verdicts and
// the benchmark report contract. The artifact half lives in
// ReleaseVerifyArtifacts.fs and the CLI/orchestrator in ReleaseVerify.fs.
//
// Every failure message here is the literal PowerShell text: the evidence audit is
// judged by those strings, so they must stay byte-exact. Checks never mutate the
// evidence tree they read.

open System
open System.Collections.Generic
open System.Globalization
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open System.Text.RegularExpressions
open FunnySharp.Harness.Output
open FunnySharp.Harness.Proc

/// A verifier failure carrying the exact PowerShell message. The orchestrator
/// catches it and records `<check>: <message>` in the failures list.
exception ReleaseVerifyFailure of string

let failNow (message: string) : 'T =
    raise (ReleaseVerifyFailure message)

// ---------------------------------------------------------------------------
// JSON helpers (the `[string]`/`$null` casts PowerShell applies to JSON values)
// ---------------------------------------------------------------------------

/// The text PowerShell's `[string]` cast produces for a JSON scalar.
let jsonText (element: JsonElement) : string =
    match element.ValueKind with
    | JsonValueKind.String -> Option.ofObj (element.GetString()) |> Option.defaultValue ""
    | JsonValueKind.Undefined
    | JsonValueKind.Null -> ""
    | JsonValueKind.True -> "True"
    | JsonValueKind.False -> "False"
    | _ -> element.GetRawText()

/// The named child of a JSON object, or a default (Undefined) element.
let getProp (name: string) (parent: JsonElement) : JsonElement =
    match parent.TryGetProperty name with
    | true, value -> value
    | _ -> JsonElement()

let hasProp (name: string) (parent: JsonElement) : bool =
    parent.ValueKind = JsonValueKind.Object && parent.TryGetProperty name |> fst

let propText (name: string) (parent: JsonElement) : string =
    getProp name parent |> jsonText

let propInt (name: string) (parent: JsonElement) : int option =
    let element = getProp name parent

    if element.ValueKind = JsonValueKind.Number then
        match element.TryGetInt32() with
        | true, value -> Some value
        | _ -> None
    else
        None

let propBool (name: string) (parent: JsonElement) : bool option =
    match (getProp name parent).ValueKind with
    | JsonValueKind.True -> Some true
    | JsonValueKind.False -> Some false
    | _ -> None

let arrayItems (element: JsonElement) : JsonElement list =
    if element.ValueKind = JsonValueKind.Array then
        element.EnumerateArray() |> Seq.toList
    else
        []

/// The final path component, falling back to the path itself.
let fileNameOf (path: string) : string =
    match Path.GetFileName path with
    | null -> path
    | name -> name

let propArray (name: string) (parent: JsonElement) : JsonElement list =
    getProp name parent |> arrayItems

let private jsonOptions = JsonSerializerOptions(WriteIndented = true)

let jstr (value: string) : JsonValue =
    Option.ofObj (JsonValue.Create value)
    |> Option.defaultWith (fun () -> failwith "JsonValue.Create returned null for a string")

let jint (value: int) : JsonValue = JsonValue.Create value

let jbool (value: bool) : JsonValue = JsonValue.Create value

/// F# cannot pass a literal null to the JsonObject setter; an uninitialized node
/// serializes as JSON null.
let jsonNull: JsonNode = Unchecked.defaultof<JsonNode>

let jsonStringArray (values: seq<string>) : JsonArray =
    let array = JsonArray()

    for value in values do
        array.Add(jstr value)

    array

/// Serialize a node the way Write-JsonFile does: indented JSON + a trailing
/// newline, UTF-8 without a BOM.
let serializeJson (node: JsonNode) : string =
    node.ToJsonString jsonOptions

let writeJsonFile (path: string) (node: JsonNode) : unit =
    File.WriteAllText(path, serializeJson node + Environment.NewLine, UTF8Encoding(false))

// ---------------------------------------------------------------------------
// Comparison helpers
// ---------------------------------------------------------------------------

let equalsOrdinal (left: string) (right: string) : bool =
    String.Equals(left, right, StringComparison.Ordinal)

let equalsIgnoreCase (left: string) (right: string) : bool =
    String.Equals(left, right, StringComparison.OrdinalIgnoreCase)

/// Test-ExactStringSequence: element-wise ordinal equality.
let exactStringSequence (actual: string list) (expected: string list) : bool =
    actual.Length = expected.Length && List.forall2 equalsOrdinal actual expected

let distinctIgnoreCase (values: string list) : string list =
    let seen = HashSet<string>(StringComparer.OrdinalIgnoreCase)
    values |> List.filter seen.Add

/// Case-insensitive sort mirroring PowerShell's Sort-Object (culture-aware, so
/// punctuation sorts before letters). InvariantCulture keeps it host-independent.
let sortedIgnoreCase (values: string list) : string list =
    values |> List.sortWith (fun left right -> StringComparer.InvariantCultureIgnoreCase.Compare(left, right))

/// A case-insensitive multiset comparison, mirroring Compare-Object on sorted sets.
let sameStringSet (actual: string list) (expected: string list) : bool =
    exactStringSequence (sortedIgnoreCase actual) (sortedIgnoreCase expected)

/// A case-insensitive property lookup: compatibility-results.json is PascalCase
/// while the verifier reads it with PowerShell's case-insensitive member access.
let getPropCI (name: string) (parent: JsonElement) : JsonElement =
    if parent.ValueKind <> JsonValueKind.Object then
        JsonElement()
    else
        match parent.EnumerateObject() |> Seq.tryFind (fun property -> equalsIgnoreCase property.Name name) with
        | Some property -> property.Value
        | None -> JsonElement()

let propTextCI (name: string) (parent: JsonElement) : string =
    getPropCI name parent |> jsonText

let propIntCI (name: string) (parent: JsonElement) : int option =
    let element = getPropCI name parent

    if element.ValueKind = JsonValueKind.Number then
        match element.TryGetInt32() with
        | true, value -> Some value
        | _ -> None
    else
        None

let propBoolCI (name: string) (parent: JsonElement) : bool option =
    match (getPropCI name parent).ValueKind with
    | JsonValueKind.True -> Some true
    | JsonValueKind.False -> Some false
    | _ -> None

// ---------------------------------------------------------------------------
// Hashing and path safety
// ---------------------------------------------------------------------------

let sha256File (path: string) : string =
    use stream = File.OpenRead path
    Convert.ToHexString(SHA256.HashData stream).ToLowerInvariant()

let sha256Text (value: string) : string =
    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes value)).ToLowerInvariant()

let pathComparison : StringComparison =
    if OperatingSystem.IsWindows() then StringComparison.OrdinalIgnoreCase else StringComparison.Ordinal

/// Test-PathAtOrWithin.
let pathAtOrWithin (child: string) (parent: string) : bool =
    let separator = string Path.DirectorySeparatorChar
    let normalizedParent = parent.TrimEnd('\\', '/') + separator
    child.Equals(parent, pathComparison) || child.StartsWith(normalizedParent, pathComparison)

let resolveFullPath (basePath: string) (path: string) : string =
    if Path.IsPathFullyQualified path then Path.GetFullPath path else Path.GetFullPath(Path.Combine(basePath, path))

/// Test one FileAttributes flag bit.
let hasFlag (flag: FileAttributes) (attributes: FileAttributes) : bool =
    (attributes &&& flag) <> enum<FileAttributes> 0

let private directorySeparators = [| Path.DirectorySeparatorChar; Path.AltDirectorySeparatorChar |]

/// Assert-SafeArtifactsSubdirectory: a proper, reparse-free subdirectory of the
/// artifact root.
let assertSafeArtifactsSubdirectory (path: string) (artifactsDirectory: string) : string =
    let resolvedArtifacts = Path.GetFullPath artifactsDirectory
    let fullPath = Path.GetFullPath path

    if fullPath.Equals(resolvedArtifacts, pathComparison) || not (pathAtOrWithin fullPath resolvedArtifacts) then
        failNow (sprintf "Path must be a proper subdirectory of '%s': '%s'." resolvedArtifacts fullPath)

    let relative = Path.GetRelativePath(resolvedArtifacts, fullPath)
    let mutable current = resolvedArtifacts

    for segment in relative.Split(directorySeparators, StringSplitOptions.RemoveEmptyEntries) do
        current <- Path.Combine(current, segment)

        if File.Exists current || Directory.Exists current then
            let attributes = File.GetAttributes current

            if hasFlag FileAttributes.ReparsePoint attributes then
                failNow (sprintf "Path cannot contain a reparse point: '%s'." (Path.GetFullPath current))

            let resolvedCurrent = Path.GetFullPath current

            if not (pathAtOrWithin resolvedCurrent resolvedArtifacts) then
                failNow (sprintf "Resolved path escapes '%s': '%s'." resolvedArtifacts resolvedCurrent)

            current <- resolvedCurrent

    fullPath

/// Get-SafeEvidenceFile: an existing, non-reparse leaf inside the artifact root.
let getSafeEvidenceFile (path: string) (artifactsDirectory: string) : string =
    assertSafeArtifactsSubdirectory path artifactsDirectory |> ignore

    if not (File.Exists path) then
        failNow (sprintf "Evidence file was not found: '%s'." path)

    let attributes = File.GetAttributes path

    if hasFlag FileAttributes.ReparsePoint attributes then
        failNow (sprintf "Evidence file cannot be a reparse point: '%s'." (Path.GetFullPath path))

    Path.GetFullPath path

/// Initialize-EvidenceDirectory; returns the resolved directory path.
let initializeEvidenceDirectory (path: string) (artifactsDirectory: string) (allowClean: bool) : string =
    let resolved = assertSafeArtifactsSubdirectory path artifactsDirectory

    if Directory.Exists resolved then
        let outputAttributes = File.GetAttributes resolved

        if hasFlag FileAttributes.ReparsePoint outputAttributes then
            failNow (sprintf "OutputDirectory '%s' cannot be a reparse point." resolved)

        let existing = Directory.GetFileSystemEntries resolved

        if existing.Length > 0 && not allowClean then
            failNow (sprintf "OutputDirectory '%s' is not empty. Supply -Clean to replace its contents." resolved)

        if allowClean then
            for item in existing do
                assertSafeArtifactsSubdirectory item artifactsDirectory |> ignore
                let attributes = File.GetAttributes item

                if hasFlag FileAttributes.Directory attributes then
                    Directory.Delete(item, true)
                else
                    File.Delete item
    elif File.Exists resolved then
        failNow (sprintf "OutputDirectory '%s' cannot be a reparse point." resolved)
    else
        Directory.CreateDirectory resolved |> ignore

    resolved

// ---------------------------------------------------------------------------
// Source fingerprint
// ---------------------------------------------------------------------------

/// Get-SourceFingerprint over `git ls-files --cached --others --exclude-standard`.
/// The canonical digest sorts case-insensitively (PowerShell Sort-Object) and is
/// SHA-256 over `path\0sha256\n` lines in UTF-8.
let getSourceFingerprint (root: string) : JsonObject =
    let result =
        runCaptureIn (Some root) "git" [ "ls-files"; "--cached"; "--others"; "--exclude-standard"; "-z" ]
        |> Async.RunSynchronously

    if result.ExitCode <> 0 then
        failNow (sprintf "git ls-files failed with exit code %d: %s" result.ExitCode (result.Stderr.Trim()))

    let entries = ResizeArray<string * string>()

    for relativePath in result.Stdout.Split('\000') do
        if not (String.IsNullOrEmpty relativePath) then
            let fullPath = Path.GetFullPath(Path.Combine(root, relativePath))

            if not (File.Exists fullPath) then
                failNow (sprintf "Source file listed by git was not found: '%s'." relativePath)

            entries.Add(relativePath.Replace('\\', '/'), sha256File fullPath)

    let ordered =
        entries
        |> Seq.sortWith (fun (left, _) (right, _) -> StringComparer.InvariantCultureIgnoreCase.Compare(left, right))
        |> Seq.toList

    let canonical = ordered |> List.map (fun (path, hash) -> path + "\000" + hash + "\n") |> String.concat ""

    let node = JsonObject()
    node.["schemaVersion"] <- jint 1
    node.["algorithm"] <- jstr "sha256"
    node.["fileCount"] <- jint ordered.Length
    node.["digest"] <- jstr (sha256Text canonical)

    let files = JsonArray()

    for path, hash in ordered do
        let entry = JsonObject()
        entry.["path"] <- jstr path
        entry.["sha256"] <- jstr hash
        files.Add entry

    node.["files"] <- files
    node

/// Test-EquivalentSourceFingerprint.
let equivalentSourceFingerprint (left: JsonElement) (right: JsonElement) : bool =
    propInt "schemaVersion" left = Some 1
    && propInt "schemaVersion" right = Some 1
    && equalsIgnoreCase (propText "algorithm" left) "sha256"
    && equalsIgnoreCase (propText "algorithm" right) "sha256"
    && propInt "fileCount" left = propInt "fileCount" right
    && equalsIgnoreCase (propText "digest" left) (propText "digest" right)

// ---------------------------------------------------------------------------
// ANSI stripping and log text
// ---------------------------------------------------------------------------

let private ansiEscapePattern = Regex("\u001b\\[[0-?]*[ -/]*[@-~]")

/// Remove-AnsiControlSequences: GitHub Actions colours MTP output.
let removeAnsiControlSequences (text: string) : string =
    ansiEscapePattern.Replace(text, "")

/// Get-ExecutionLogText: a relative child path whose bytes match the receipt hash.
let getExecutionLogText
    (executionDirectory: string)
    (relativePath: string)
    (expectedSha256: string)
    (artifactsDirectory: string)
    : struct (string * string * string) =
    if Path.IsPathFullyQualified relativePath || Regex.IsMatch(relativePath, @"(^|[\\/])\.\.([\\/]|$)") then
        failNow (sprintf "Execution evidence log path must be a relative child path: '%s'." relativePath)

    let path = Path.GetFullPath(Path.Combine(executionDirectory, relativePath))

    if not (pathAtOrWithin path executionDirectory) then
        failNow (sprintf "Execution evidence log path escapes its directory: '%s'." relativePath)

    let path = getSafeEvidenceFile path artifactsDirectory
    use stream = File.OpenRead path
    let actualSha256 = Convert.ToHexString(SHA256.HashData stream).ToLowerInvariant()

    if not (equalsIgnoreCase actualSha256 expectedSha256) then
        failNow (sprintf "Execution evidence log hash does not match receipt: '%s'." relativePath)

    // Reuse the same regular-file handle, but reject bad bytes before allocating
    // decoded text. Both passes remain bounded and see the same opened file.
    stream.Position <- 0L
    use reader = new StreamReader(stream)
    struct (path, actualSha256, removeAnsiControlSequences(reader.ReadToEnd()))

// ---------------------------------------------------------------------------
// Release protocol and the canonical command table
// ---------------------------------------------------------------------------

type ProtocolStep =
    { FileName: string
      WorkingDirectory: string
      Arguments: string list }

type ReleaseProtocol =
    { FullSteps: string list
      BenchmarkSkippedSteps: string list
      Steps: Map<string, ProtocolStep> }

type ExpectedCommand =
    { Name: string
      FileName: string
      WorkingDirectory: string
      Arguments: string list }

/// Read-ReleaseProtocol: schema 1, both modes non-empty and duplicate-free, every
/// named step defined.
let readReleaseProtocol (path: string) : ReleaseProtocol =
    if not (File.Exists path) then
        failNow (sprintf "Release protocol was not found: '%s'." path)

    use document = JsonDocument.Parse(File.ReadAllText path)
    let root = document.RootElement

    if propInt "schemaVersion" root <> Some 1 then
        failNow "Release protocol must use schemaVersion 1."

    let modes = getProp "modes" root
    let stepsElement = getProp "steps" root

    let readSteps (modeName: string) : string list =
        let names = propArray "steps" (getProp modeName modes) |> List.map jsonText

        if names.IsEmpty || distinctIgnoreCase(names).Length <> names.Length then
            failNow (sprintf "Release protocol mode '%s' has no steps or contains duplicates." modeName)

        for name in names do
            if not (hasProp name stepsElement) then
                failNow (sprintf "Release protocol mode '%s' refers to undefined step '%s'." modeName name)

        names

    let full = readSteps "full"
    let benchmarkSkipped = readSteps "benchmarkSkipped"

    let steps =
        [ for property in stepsElement.EnumerateObject() ->
            let definition = property.Value

            property.Name,
            { FileName = propText "fileName" definition
              WorkingDirectory = propText "workingDirectory" definition
              Arguments = propArray "arguments" definition |> List.map jsonText } ]
        |> Map.ofList

    { FullSteps = full
      BenchmarkSkippedSteps = benchmarkSkipped
      Steps = steps }

/// Get-ExpectedReleaseCommands: expand the `{token}` placeholders for the mode.
let getExpectedReleaseCommands
    (executionDirectory: string)
    (compatibilityPackageFeed: string)
    (root: string)
    (compatibilityRuntimeIdentifier: string)
    (benchmarksSkipped: bool)
    : ExpectedCommand list =
    let packagesDirectory = Path.Combine(executionDirectory, "packages")
    let benchmarkArtifactsDirectory = Path.Combine(executionDirectory, "benchmark-artifacts")
    let protocol = readReleaseProtocol (Path.Combine(root, "eng/release-protocol.json"))
    let names = if benchmarksSkipped then protocol.BenchmarkSkippedSteps else protocol.FullSteps

    let tokens =
        [ "root", root
          "compatibilityFeed", compatibilityPackageFeed
          "packages", packagesDirectory
          "benchmarkRoot", Path.Combine(root, "benchmarks/FunnySharp.Benchmarks")
          "benchmarkArtifacts", benchmarkArtifactsDirectory
          "benchmarkResults", Path.Combine(benchmarkArtifactsDirectory, "results")
          "performanceObservationProposal", Path.Combine(executionDirectory, "performance-observation-proposal.json")
          "compatibilityOutput", Path.Combine(executionDirectory, "compatibility-run")
          "compatibilityRid", compatibilityRuntimeIdentifier
          "harnessDll", Path.Combine(root, "eng/harness/bin/Debug/net10.0/FunnySharp.Harness.dll") ]
        |> Map.ofList

    let expand (value: string) : string =
        tokens |> Map.fold (fun (state: string) key token -> state.Replace("{" + key + "}", token)) value

    names
    |> List.map (fun name ->
        let definition = protocol.Steps.[name]

        let fromProtocol =
            { Name = name
              FileName = expand definition.FileName
              WorkingDirectory = expand definition.WorkingDirectory
              Arguments = definition.Arguments |> List.map expand }

        fromProtocol)

/// Assert-CanonicalReleaseCommand (case-sensitive, ordered arguments).
let assertCanonicalReleaseCommand (command: JsonElement) (expected: ExpectedCommand) (description: string) : unit =
    if
        not (equalsOrdinal (propText "fileName" command) expected.FileName)
        || not (equalsOrdinal (propText "workingDirectory" command) expected.WorkingDirectory)
        || not (exactStringSequence (propArray "arguments" command |> List.map jsonText) expected.Arguments)
    then
        failNow (
            sprintf
                "%s does not use the canonical command, working directory, and ordered arguments for '%s'."
                description
                expected.Name
        )

// ---------------------------------------------------------------------------
// Captured-step markers
// ---------------------------------------------------------------------------

let private buildSucceededPattern = Regex(@"^\s*Build succeeded\.\s*$", RegexOptions.IgnoreCase ||| RegexOptions.Multiline)
let private buildWarningsPattern = Regex(@"^\s*0 Warning\(s\)\s*$", RegexOptions.IgnoreCase ||| RegexOptions.Multiline)
let private buildErrorsPattern = Regex(@"^\s*0 Error\(s\)\s*$", RegexOptions.IgnoreCase ||| RegexOptions.Multiline)

let private testSummaryPattern =
    Regex(
        @"Test run summary:\s*Passed!.*?\btotal:\s*(?<total>\d+).*?\bfailed:\s*0.*?\bsucceeded:\s*(?<succeeded>\d+).*?\bskipped:\s*0",
        RegexOptions.IgnoreCase ||| RegexOptions.Singleline
    )

let private benchmarkFailurePattern = Regex(@"(?i)benchmark has failed|failed benchmarks?|build error|error\s+CS\d+")
let private benchmarkFinishPattern = Regex(@"(?i)BenchmarkRunner:\s*Finish")
let private executedBenchmarksPattern = Regex(@"(?i)executed benchmarks:\s*(?<count>\d+)")

[<Literal>]
let CoreExampleMarker = "FunnySharp examples passed."

[<Literal>]
let AspNetCoreExampleMarker = "FunnySharp ASP.NET Core example endpoints mapped."

let testAssemblyRelativePaths: string list =
    [ "tests/FunnySharp.Tests/bin/Release/net10.0/FunnySharp.Tests.dll"
      "tests/FunnySharp.AspNetCore.Tests/bin/Release/net10.0/FunnySharp.AspNetCore.Tests.dll" ]

/// Build evidence: `Build succeeded.` with 0 warnings and 0 errors.
let assertBuildMarkers (buildLog: string) : unit =
    if
        not (buildSucceededPattern.IsMatch buildLog)
        || not (buildWarningsPattern.IsMatch buildLog)
        || not (buildErrorsPattern.IsMatch buildLog)
    then
        failNow "Build evidence must report Build succeeded with 0 Warning(s) and 0 Error(s)."

/// Test evidence: one positive all-passing summary plus a passed result line for
/// each of the two test assemblies. Returns the total.
let assertTestMarkers (testLog: string) (root: string) : int =
    let summary = testSummaryPattern.Match testLog

    let valid =
        summary.Success
        && (summary.Groups.["total"].Value |> Int32.Parse) > 0
        && summary.Groups.["total"].Value = summary.Groups.["succeeded"].Value

    if not valid then
        failNow "Test evidence must report a positive all-passing count with 0 failed and 0 skipped."

    let total = Int32.Parse summary.Groups.["total"].Value

    for relative in testAssemblyRelativePaths do
        let assembly = Path.GetFullPath(Path.Combine(root, relative))

        let pattern =
            Regex(
                @"^\s*" + Regex.Escape assembly + @"\s+\(net10\.0\|[^)]*\)\s+passed\s+\([^)]*\)\s*$",
                RegexOptions.IgnoreCase ||| RegexOptions.Multiline
            )

        if not (pattern.IsMatch testLog) then
            failNow (sprintf "Test evidence does not contain a successful result line for '%s'." assembly)

    total

/// Examples evidence markers.
let assertExampleMarkers (coreLog: string) (aspNetCoreLog: string) : unit =
    if not (coreLog.Contains(CoreExampleMarker, StringComparison.Ordinal)) then
        failNow "Examples evidence did not contain the success message."

    if not (aspNetCoreLog.Contains(AspNetCoreExampleMarker, StringComparison.Ordinal)) then
        failNow "ASP.NET Core examples evidence did not contain the success message."

/// Benchmark evidence markers; returns the maximum reported executed count.
let assertBenchmarkMarkers (benchmarkLog: string) : int =
    let counts = [ for m in executedBenchmarksPattern.Matches benchmarkLog -> Int32.Parse m.Groups.["count"].Value ]
    let maximum = if counts.IsEmpty then 0 else List.max counts

    if
        not (benchmarkFinishPattern.IsMatch benchmarkLog)
        || benchmarkFailurePattern.IsMatch benchmarkLog
        || maximum <= 0
    then
        failNow "Benchmark evidence must report completed benchmarks with no failures."

    maximum

// ---------------------------------------------------------------------------
// Benchmark report contract
// ---------------------------------------------------------------------------

/// One benchmark row identity, independent of its JSON source.
type BenchmarkRow =
    { BenchmarkClass: string
      Category: string
      Method: string
      Parameters: string }

let benchmarkRowFromElement (row: JsonElement) : BenchmarkRow =
    { BenchmarkClass = propText "benchmarkClass" row
      Category = propText "category" row
      Method = propText "method" row
      Parameters = propText "parameters" row }

/// Assert-BenchmarkReportRows: multiset equality keyed by
/// `class\0category\0method\0parameters`.
let assertBenchmarkRows (expectedRows: BenchmarkRow list) (actualRows: BenchmarkRow list) (description: string) : unit =
    let countsOf (rows: BenchmarkRow list) : Dictionary<string, int> =
        let counts = Dictionary<string, int>()

        for row in rows do
            if
                String.IsNullOrWhiteSpace row.BenchmarkClass
                || String.IsNullOrWhiteSpace row.Category
                || String.IsNullOrWhiteSpace row.Method
            then
                failNow (sprintf "%s contains a row with missing benchmark identity." description)

            let key =
                row.BenchmarkClass + "\000" + row.Category + "\000" + row.Method + "\000" + row.Parameters

            match counts.TryGetValue key with
            | true, count -> counts.[key] <- count + 1
            | _ -> counts.[key] <- 1

        counts

    let expectedCounts = countsOf expectedRows
    let actualCounts = countsOf actualRows

    let matches =
        expectedCounts.Count = actualCounts.Count
        && expectedCounts |> Seq.forall (fun pair ->
            match actualCounts.TryGetValue pair.Key with
            | true, count -> count = pair.Value
            | _ -> false)

    if not matches then
        failNow (sprintf "%s does not match the registered benchmark rows." description)

let assertBenchmarkReportRows (expectedRows: JsonElement list) (actualRows: JsonElement list) (description: string) : unit =
    assertBenchmarkRows
        (expectedRows |> List.map benchmarkRowFromElement)
        (actualRows |> List.map benchmarkRowFromElement)
        description

/// Get-BenchmarkParameterNames: the ordered parameter identity of a class, or None
/// when every policy row has empty parameters.
let getBenchmarkParameterNames (policyRows: JsonElement list) (benchmarkClass: string) : string list option =
    let identities = policyRows |> List.map (propText "parameters") |> sortedIgnoreCase |> distinctIgnoreCase
    let mutable expectedNames: string list option = None

    for parameters in identities do
        if not (String.IsNullOrEmpty parameters) then
            if not (Regex.IsMatch(parameters, @"^\[.*\]$")) then
                failNow (sprintf "Benchmark class '%s' has an invalid parameter identity '%s'." benchmarkClass parameters)

            let names =
                [ for m in Regex.Matches(parameters, @"(?:^\[|, )(?<name>[A-Za-z_][A-Za-z0-9_]*)=") -> m.Groups.["name"].Value ]

            if names.IsEmpty then
                failNow (sprintf "Benchmark class '%s' has an invalid parameter identity '%s'." benchmarkClass parameters)

            match expectedNames with
            | None -> expectedNames <- Some names
            | Some existing when not (exactStringSequence names existing) ->
                failNow (sprintf "Benchmark class '%s' uses inconsistent parameter identities." benchmarkClass)
            | Some _ -> ()

    expectedNames

type BenchmarkCategoryManifest =
    { BenchmarkClass: string
      Category: string
      MethodCount: int
      BaselineCount: int }

type BenchmarkSourceManifest =
    { Classes: string list
      Categories: BenchmarkCategoryManifest list }

/// Get-BenchmarkSourceManifest: parse `[Benchmark]`/`[BenchmarkCategory]` attributes
/// from the benchmark sources.
let getBenchmarkSourceManifest (root: string) : BenchmarkSourceManifest =
    let benchmarkDirectory = Path.Combine(root, "benchmarks/FunnySharp.Benchmarks")

    let sourceFiles =
        if Directory.Exists benchmarkDirectory then
            Directory.GetFiles(benchmarkDirectory, "*.cs")
            |> Array.filter (fun path -> fileNameOf path <> "Program.cs")
            |> Array.sortWith (fun left right ->
                StringComparer.OrdinalIgnoreCase.Compare(fileNameOf left, fileNameOf right))
        else
            [||]

    let classes = ResizeArray<string>()
    let categories = Dictionary<string, BenchmarkCategoryManifest>()

    let classPattern =
        Regex(@"^public\s+(?:sealed\s+|abstract\s+)?class\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\b")

    let methodPattern =
        Regex(
            @"^(?:public|protected|internal|private)\s+(?:static\s+)?(?:async\s+)?[A-Za-z_][A-Za-z0-9_<>,?.\[\]\s]*\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\("
        )

    let benchmarkAttribute = Regex(@"\[Benchmark(?:\s*\([^\]]*\))?\]")
    let categoryAttribute = Regex(@"\[BenchmarkCategory\(\s*""(?<category>[^""]+)""\s*\)\]")
    let baselineAttribute = Regex(@"\[Benchmark\(\s*Baseline\s*=\s*true\s*\)\]")

    for sourceFile in sourceFiles do
        let mutable currentClass = ""
        let attributeLines = ResizeArray<string>()

        for line in File.ReadAllLines sourceFile do
            let trimmed = line.Trim()
            let classMatch = classPattern.Match trimmed

            if classMatch.Success then
                currentClass <- classMatch.Groups.["name"].Value
                classes.Add currentClass
                attributeLines.Clear()
            elif trimmed.StartsWith("[", StringComparison.Ordinal) then
                attributeLines.Add trimmed
            else
                let methodMatch = methodPattern.Match trimmed

                if not methodMatch.Success then
                    if not (String.IsNullOrWhiteSpace trimmed) && not (trimmed.StartsWith("///", StringComparison.Ordinal)) then
                        attributeLines.Clear()
                else
                    let attributes = String.Join(Environment.NewLine, attributeLines)
                    attributeLines.Clear()

                    if String.IsNullOrEmpty currentClass || not (benchmarkAttribute.IsMatch attributes) then
                        ()
                    else
                        let categoryMatches = categoryAttribute.Matches attributes

                        if categoryMatches.Count = 0 then
                            failNow (
                                sprintf
                                    "Benchmark '%s.%s' has no BenchmarkCategory declaration."
                                    currentClass
                                    methodMatch.Groups.["name"].Value
                            )

                        let isBaseline = baselineAttribute.IsMatch attributes

                        for categoryMatch in categoryMatches do
                            let category = categoryMatch.Groups.["category"].Value
                            let key = currentClass + "\000" + category

                            match categories.TryGetValue key with
                            | true, existing ->
                                categories.[key] <-
                                    { existing with
                                        MethodCount = existing.MethodCount + 1
                                        BaselineCount = existing.BaselineCount + (if isBaseline then 1 else 0) }
                            | _ ->
                                categories.[key] <-
                                    { BenchmarkClass = currentClass
                                      Category = category
                                      MethodCount = 1
                                      BaselineCount = (if isBaseline then 1 else 0) }

    let orderedCategories =
        categories.Values
        |> Seq.sortWith (fun left right ->
            let byClass = StringComparer.OrdinalIgnoreCase.Compare(left.BenchmarkClass, right.BenchmarkClass)
            if byClass <> 0 then byClass else StringComparer.OrdinalIgnoreCase.Compare(left.Category, right.Category))
        |> Seq.toList

    { Classes = sortedIgnoreCase (List.ofSeq classes) |> distinctIgnoreCase
      Categories = orderedCategories }

/// Minimal RFC-4180 CSV reader (Import-Csv parity for BenchmarkDotNet reports).
let parseCsvRows (text: string) : string list list =
    let rows = ResizeArray<string list>()
    let row = ResizeArray<string>()
    let field = StringBuilder()
    let mutable inQuotes = false
    let mutable index = 0

    while index < text.Length do
        let character = text.[index]

        if inQuotes then
            if character = '"' then
                if index + 1 < text.Length && text.[index + 1] = '"' then
                    field.Append('"') |> ignore
                    index <- index + 1
                else
                    inQuotes <- false
            else
                field.Append character |> ignore
        else
            match character with
            | '"' -> inQuotes <- true
            | ',' ->
                row.Add(field.ToString())
                field.Clear() |> ignore
            | '\r' -> ()
            | '\n' ->
                row.Add(field.ToString())
                field.Clear() |> ignore
                rows.Add(List.ofSeq row)
                row.Clear()
            | _ -> field.Append character |> ignore

        index <- index + 1

    if field.Length > 0 || row.Count > 0 then
        row.Add(field.ToString())
        rows.Add(List.ofSeq row)

    List.ofSeq rows

/// Assert-BenchmarkReports.
let assertBenchmarkReports
    (executionDirectory: string)
    (artifactsDirectory: string)
    (root: string)
    (observationRows: JsonElement list)
    : JsonObject =
    let manifestPath = Path.Combine(root, "eng/performance/baseline.json")
    use manifestDocument = JsonDocument.Parse(File.ReadAllText manifestPath)
    let includedPolicyRows = propArray "rows" (getProp "policy" manifestDocument.RootElement) |> List.filter (fun row -> propBool "included" row = Some true)

    let expectedReports =
        includedPolicyRows
        |> List.map (propText "benchmarkClass")
        |> sortedIgnoreCase
        |> distinctIgnoreCase
        |> List.map (fun benchmarkClass ->
            {| BenchmarkClass = benchmarkClass
               FileName = sprintf "FunnySharp.Benchmarks.%s-report.csv" benchmarkClass
               ReceiptName = sprintf "%s-performance-receipt.json" benchmarkClass |})

    assertBenchmarkReportRows includedPolicyRows observationRows "Performance observation proposal"

    let benchmarkArtifactsDirectory =
        assertSafeArtifactsSubdirectory (Path.Combine(executionDirectory, "benchmark-artifacts")) artifactsDirectory

    let resultsDirectory = assertSafeArtifactsSubdirectory (Path.Combine(benchmarkArtifactsDirectory, "results")) artifactsDirectory

    if not (Directory.Exists resultsDirectory) then
        failNow (sprintf "Benchmark results directory was not found: '%s'." resultsDirectory)

    let actualCsvNames =
        Directory.GetFiles resultsDirectory
        |> Array.filter (fun path -> equalsOrdinal (Option.ofObj (Path.GetExtension path) |> Option.defaultValue "") ".csv")
        |> Array.map fileNameOf
        |> Array.sortWith (fun left right -> StringComparer.OrdinalIgnoreCase.Compare(left, right))
        |> Array.toList

    let expectedCsvNames = expectedReports |> List.map (fun report -> report.FileName) |> sortedIgnoreCase

    if not (exactStringSequence actualCsvNames expectedCsvNames) then
        failNow (sprintf "Benchmark results must contain exactly these CSV reports: %s." (String.concat ", " expectedCsvNames))

    let actualReceiptNames =
        Directory.GetFiles(resultsDirectory, "*-performance-receipt.json")
        |> Array.map fileNameOf
        |> Array.sortWith (fun left right -> StringComparer.OrdinalIgnoreCase.Compare(left, right))
        |> Array.toList

    let expectedReceiptNames = expectedReports |> List.map (fun report -> report.ReceiptName) |> sortedIgnoreCase

    if not (exactStringSequence actualReceiptNames expectedReceiptNames) then
        failNow (sprintf "Benchmark results must contain exactly these receipts: %s." (String.concat ", " expectedReceiptNames))

    let sourceManifest = getBenchmarkSourceManifest root
    let expectedClasses = expectedReports |> List.map (fun report -> report.BenchmarkClass) |> sortedIgnoreCase

    if not (exactStringSequence (sortedIgnoreCase sourceManifest.Classes) expectedClasses) then
        failNow (sprintf "Benchmark source classes must exactly match the report set: %s." (String.concat ", " expectedClasses))

    let sourceCategories = Dictionary<string, BenchmarkCategoryManifest>()

    for sourceCategory in sourceManifest.Categories do
        sourceCategories.[sourceCategory.BenchmarkClass + "\000" + sourceCategory.Category] <- sourceCategory

    let reportSummaries = ResizeArray<string * JsonObject>()
    let reportCategories = Dictionary<string, int * string>()
    let mutable rowCount = 0

    for expectedReport in expectedReports do
        let classPolicyRows =
            includedPolicyRows |> List.filter (fun row -> equalsIgnoreCase (propText "benchmarkClass" row) expectedReport.BenchmarkClass)

        let parameterNames = getBenchmarkParameterNames classPolicyRows expectedReport.BenchmarkClass |> Option.defaultValue []

        let reportPath =
            getSafeEvidenceFile (Path.Combine(resultsDirectory, expectedReport.FileName)) artifactsDirectory

        let receiptPath =
            getSafeEvidenceFile (Path.Combine(resultsDirectory, expectedReport.ReceiptName)) artifactsDirectory

        use receiptDocument = JsonDocument.Parse(File.ReadAllText receiptPath)
        let receipt = receiptDocument.RootElement

        if propInt "schemaVersion" receipt <> Some 1 || propBool "succeeded" receipt <> Some true then
            failNow (sprintf "Benchmark receipt '%s' is malformed or unsuccessful." expectedReport.ReceiptName)

        let receiptClasses =
            propArray "rows" receipt |> List.map (propText "benchmarkClass") |> distinctIgnoreCase

        if receiptClasses.Length <> 1 || not (equalsIgnoreCase receiptClasses.Head expectedReport.BenchmarkClass) then
            failNow (sprintf "Benchmark receipt '%s' does not contain '%s'." expectedReport.ReceiptName expectedReport.BenchmarkClass)

        let declaredReports = propArray "reports" receipt
        let declaredReportNames = declaredReports |> List.map (propText "file") |> sortedIgnoreCase

        let expectedDeclaredReports =
            [ sprintf "FunnySharp.Benchmarks.%s-report-github.md" expectedReport.BenchmarkClass
              expectedReport.FileName
              sprintf "FunnySharp.Benchmarks.%s-report.html" expectedReport.BenchmarkClass ]
            |> sortedIgnoreCase

        if not (exactStringSequence declaredReportNames expectedDeclaredReports) then
            failNow (sprintf "Benchmark receipt '%s' does not declare the complete report set." expectedReport.ReceiptName)

        for declaredReport in declaredReports do
            let fileName = propText "file" declaredReport

            let declaredPath =
                getSafeEvidenceFile (Path.Combine(resultsDirectory, fileName)) artifactsDirectory

            if not (equalsIgnoreCase (sha256File declaredPath) (propText "sha256" declaredReport)) then
                failNow (sprintf "Benchmark report '%s' does not match its receipt." fileName)

        let reportSha256 = sha256File reportPath
        let rows = parseCsvRows (File.ReadAllText reportPath)

        let header =
            match rows with
            | headerRow :: _ -> headerRow
            | [] -> []

        if
            rows.IsEmpty
            || not (header |> List.exists (equalsIgnoreCase "Method"))
            || not (header |> List.exists (equalsIgnoreCase "Categories"))
        then
            failNow (
                sprintf
                    "Benchmark report '%s' must contain Method and Categories columns with at least one row."
                    expectedReport.FileName
            )

        let columnIndex name = header |> List.tryFindIndex (equalsOrdinal name)

        let methodIndex = columnIndex "Method"
        let categoriesIndex = columnIndex "Categories"

        let reportRows = ResizeArray<BenchmarkRow>()

        let bodyRows =
            match rows with
            | _ :: rest -> rest
            | [] -> []

        rowCount <- rowCount + bodyRows.Length

        for row in bodyRows do
            let cell index =
                if index < row.Length then row.[index] else ""

            let method = cell (defaultArg methodIndex 0)
            let categoriesText = cell (defaultArg categoriesIndex 0)

            if String.IsNullOrWhiteSpace method || String.IsNullOrWhiteSpace categoriesText then
                failNow (
                    sprintf "Benchmark report '%s' contains a row without Method or Categories." expectedReport.FileName
                )

            let categoryValues = Regex.Split(categoriesText, @"\s*;\s*")

            if categoryValues.Length <> 1 then
                failNow (sprintf "Benchmark report '%s' must contain exactly one category per row." expectedReport.FileName)

            for category in categoryValues do
                if String.IsNullOrWhiteSpace category then
                    failNow (sprintf "Benchmark report '%s' contains an empty category." expectedReport.FileName)

                let key = expectedReport.BenchmarkClass + "\000" + category

                match reportCategories.TryGetValue key with
                | true, (count, report) -> reportCategories.[key] <- (count + 1, report)
                | _ -> reportCategories.[key] <- (1, expectedReport.FileName)

                let parameterValues =
                    [ for parameterName in parameterNames ->
                        match columnIndex parameterName with
                        | Some position -> parameterName + "=" + cell position
                        | None ->
                            failNow (
                                sprintf
                                    "Benchmark report '%s' is missing parameter column '%s'."
                                    expectedReport.FileName
                                    parameterName
                            ) ]

                reportRows.Add(
                    { BenchmarkClass = expectedReport.BenchmarkClass
                      Category = category
                      Method = method
                      Parameters =
                        (if parameterValues.IsEmpty then
                             ""
                         else
                             "[" + String.concat ", " parameterValues + "]") }
                )

        assertBenchmarkReportRows
            classPolicyRows
            (propArray "rows" receipt)
            (sprintf "Benchmark receipt '%s'" expectedReport.ReceiptName)

        assertBenchmarkRows
            (propArray "rows" receipt |> List.map benchmarkRowFromElement)
            (List.ofSeq reportRows)
            (sprintf "Benchmark report '%s'" expectedReport.FileName)

        let summary = JsonObject()

        summary.["benchmarkClass"] <- jstr expectedReport.BenchmarkClass
        summary.["report"] <- jstr expectedReport.FileName
        summary.["sha256"] <- jstr reportSha256
        summary.["receipt"] <- jstr expectedReport.ReceiptName
        summary.["receiptSha256"] <- jstr (sha256File receiptPath)
        summary.["rowCount"] <- jint bodyRows.Length
        reportSummaries.Add(expectedReport.BenchmarkClass, summary)

    let categorySummaries = ResizeArray<string * string * JsonObject>()

    for pair in reportCategories |> Seq.sortWith (fun left right -> StringComparer.OrdinalIgnoreCase.Compare(left.Key, right.Key)) do
        let count, report = pair.Value
        let benchmarkClass, category = pair.Key.Split('\000').[0], pair.Key.Split('\000').[1]

        if count < 2 then
            failNow (
                sprintf "Benchmark category '%s: %s' has fewer than two comparison rows." benchmarkClass category
            )

        match sourceCategories.TryGetValue pair.Key with
        | false, _ ->
            failNow (
                sprintf "Benchmark category '%s: %s' is not declared in current benchmark sources." benchmarkClass category
            )
        | true, sourceCategory ->
            if sourceCategory.BaselineCount <> 1 then
                failNow (
                    sprintf
                        "Benchmark category '%s: %s' must declare exactly one Benchmark(Baseline = true) method, found %d."
                        benchmarkClass
                        category
                        sourceCategory.BaselineCount
                )

            let summary = JsonObject()
            summary.["benchmarkClass"] <- jstr benchmarkClass
            summary.["category"] <- jstr category
            summary.["report"] <- jstr report
            summary.["reportRowCount"] <- jint count
            summary.["sourceMethodCount"] <- jint sourceCategory.MethodCount
            summary.["sourceBaselineCount"] <- jint sourceCategory.BaselineCount
            categorySummaries.Add(benchmarkClass, category, summary)

    for pair in sourceCategories do
        if not (reportCategories.ContainsKey pair.Key) then
            failNow (
                sprintf
                    "Benchmark source category '%s: %s' has no report rows."
                    pair.Value.BenchmarkClass
                    pair.Value.Category
            )

    let reports = JsonArray()

    reportSummaries
    |> Seq.sortWith (fun (leftKey, _) (rightKey, _) -> StringComparer.OrdinalIgnoreCase.Compare(leftKey, rightKey))
    |> Seq.iter (fun (_, node) -> reports.Add node)

    let categoryArray = JsonArray()

    categorySummaries
    |> Seq.sortWith (fun (leftClass, leftCategory, _) (rightClass, rightCategory, _) ->
        let byClass = StringComparer.OrdinalIgnoreCase.Compare(leftClass, rightClass)
        if byClass <> 0 then byClass else StringComparer.OrdinalIgnoreCase.Compare(leftCategory, rightCategory))
    |> Seq.iter (fun (_, _, node) -> categoryArray.Add node)

    let node = JsonObject()
    node.["status"] <- jstr "verified"
    node.["artifactsDirectory"] <- jstr benchmarkArtifactsDirectory
    node.["resultsDirectory"] <- jstr resultsDirectory
    node.["reportCount"] <- jint reportSummaries.Count
    node.["rowCount"] <- jint rowCount
    node.["categoryCount"] <- jint categorySummaries.Count
    node.["reports"] <- reports
    node.["categories"] <- categoryArray
    node

// ---------------------------------------------------------------------------
// Assert-ReleaseExecutionEvidence (the heavyweight evidence audit)
// ---------------------------------------------------------------------------

/// Validate the Run-Release evidence bundle against the current protocol, the
/// captured logs and the on-disk receipt/log sets. Returns the execution summary.
let assertReleaseExecutionEvidence
    (directory: string)
    (artifactsDirectory: string)
    (root: string)
    (skipBenchmarks: bool)
    (compatibilityPackageFeed: string)
    (compatibilityRuntimeIdentifier: string)
    : JsonObject =
    let executionDirectory = assertSafeArtifactsSubdirectory directory artifactsDirectory

    if not (Directory.Exists executionDirectory) then
        failNow (sprintf "ExecutionEvidenceDirectory was not found: '%s'." executionDirectory)

    let evidencePath =
        getSafeEvidenceFile (Path.Combine(executionDirectory, "execution-evidence.json")) artifactsDirectory

    use evidenceDocument =
        try
            JsonDocument.Parse(File.ReadAllText evidencePath)
        with _ ->
            failNow (sprintf "Execution evidence is not valid JSON: '%s'." evidencePath)

    let evidence = evidenceDocument.RootElement

    if propInt "schemaVersion" evidence <> Some 2 || propBool "succeeded" evidence <> Some true then
        failNow "Execution evidence must use schema version 2 and report succeeded=true."

    let expectedMode = if skipBenchmarks then "benchmarkSkipped" else "full"
    let candidateCommit = propText "candidateCommit" evidence

    if
        not (equalsIgnoreCase (propText "mode" evidence) expectedMode)
        || not (Regex.IsMatch(candidateCommit, "^[0-9a-f]{40}$"))
    then
        failNow (sprintf "Execution evidence mode or candidate commit is invalid for '%s'." expectedMode)

    let protocolPath = Path.Combine(root, "eng/release-protocol.json")

    if
        not (equalsIgnoreCase (propText "path" (getProp "protocol" evidence)) "eng/release-protocol.json")
        || not (equalsIgnoreCase (propText "sha256" (getProp "protocol" evidence)) (sha256File protocolPath))
    then
        failNow "Execution evidence does not match the current release protocol."

    if
        propBool "isolatedNuGetCache" evidence <> Some true
        || not (pathAtOrWithin (propText "nugetPackagesDirectory" evidence) executionDirectory)
    then
        failNow "Execution evidence does not prove an output-local isolated NuGet package cache."

    let versionPasses (versionEvidence: JsonElement) : bool =
        propInt "schemaVersion" versionEvidence = Some 1
        && equalsIgnoreCase (propText "status" versionEvidence) "passed"
        && equalsIgnoreCase (propText "candidateCommit" versionEvidence) candidateCommit
        && not (propArray "checks" versionEvidence).IsEmpty
        && (propArray "checks" versionEvidence
            |> List.forall (fun check -> equalsIgnoreCase (propText "status" check) "absent"))

    let versionPreflightPath =
        getSafeEvidenceFile (Path.Combine(executionDirectory, propText "versionPreflight" evidence)) artifactsDirectory

    if not (equalsIgnoreCase (propText "versionPreflightSha256" evidence) (sha256File versionPreflightPath)) then
        failNow "Execution evidence does not match the package-version preflight bytes."

    use versionPreflightDocument = JsonDocument.Parse(File.ReadAllText versionPreflightPath)

    if not (versionPasses versionPreflightDocument.RootElement) then
        failNow "Execution evidence does not contain a complete passing package-version preflight."

    let versionFinalPath =
        getSafeEvidenceFile (Path.Combine(executionDirectory, propText "versionFinal" evidence)) artifactsDirectory

    if not (equalsIgnoreCase (propText "versionFinalSha256" evidence) (sha256File versionFinalPath)) then
        failNow "Execution evidence does not match the final package-version check bytes."

    use versionFinalDocument = JsonDocument.Parse(File.ReadAllText versionFinalPath)

    if not (versionPasses versionFinalDocument.RootElement) then
        failNow "Execution evidence does not contain a complete passing final package-version check."

    if
        not (
            equivalentSourceFingerprint
                (getProp "sourceFingerprintBefore" evidence)
                (getProp "sourceFingerprintAfter" evidence)
        )
    then
        failNow "Execution evidence source fingerprints do not match."

    let currentFingerprint = getSourceFingerprint root
    use currentFingerprintDocument = JsonDocument.Parse(serializeJson currentFingerprint)

    if
        not (
            equivalentSourceFingerprint
                (getProp "sourceFingerprintBefore" evidence)
                currentFingerprintDocument.RootElement
        )
    then
        failNow "The current source fingerprint does not match the release execution evidence."

    let expectedCommands =
        getExpectedReleaseCommands
            executionDirectory
            compatibilityPackageFeed
            root
            compatibilityRuntimeIdentifier
            skipBenchmarks

    let expectedNames = expectedCommands |> List.map (fun command -> command.Name)
    let candidateCommands = propArray "candidateCommands" evidence |> List.map jsonText

    if not (exactStringSequence candidateCommands expectedNames) then
        failNow (
            sprintf
                "Execution evidence must declare exactly these candidate commands: %s."
                (String.concat ", " expectedNames)
        )

    let commands = propArray "commands" evidence

    if commands.Length <> expectedNames.Length then
        failNow (sprintf "Execution evidence must contain exactly %d candidate command receipts." expectedNames.Length)

    let verifiedCommands = JsonArray()
    let logTextByName = Dictionary<string, string>()

    for index in 0 .. expectedNames.Length - 1 do
        let ordinal = index + 1
        let name = expectedNames.[index]
        let expectedCommand = expectedCommands.[index]
        let command = commands.[index]
        let prefix = sprintf "%02d-%s" ordinal name
        let expectedOutputLog = "logs/" + prefix + ".stdout.log"
        let expectedErrorLog = "logs/" + prefix + ".stderr.log"
        let expectedReceipt = "receipts/" + prefix + ".json"

        if
            propInt "schemaVersion" command <> Some 1
            || not (equalsIgnoreCase (propText "name" command) name)
            || propInt "exitCode" command <> Some 0
            || not (equalsOrdinal (propText "standardOutputLog" command) expectedOutputLog)
            || not (equalsOrdinal (propText "standardErrorLog" command) expectedErrorLog)
        then
            failNow (sprintf "Execution receipt for '%s' is missing, out of order, malformed, or unsuccessful." name)

        assertCanonicalReleaseCommand command expectedCommand (sprintf "Execution manifest command '%s'" name)

        let receiptPath =
            getSafeEvidenceFile (Path.Combine(executionDirectory, expectedReceipt)) artifactsDirectory

        use receiptDocument =
            try
                JsonDocument.Parse(File.ReadAllText receiptPath)
            with _ ->
                failNow (sprintf "Execution receipt is not valid JSON: '%s'." receiptPath)

        let receipt = receiptDocument.RootElement

        if
            propInt "schemaVersion" receipt <> Some 1
            || not (equalsIgnoreCase (propText "name" receipt) name)
            || propInt "exitCode" receipt <> Some 0
            || not (equalsOrdinal (propText "standardOutputLog" receipt) expectedOutputLog)
            || not (equalsOrdinal (propText "standardErrorLog" receipt) expectedErrorLog)
            || not (equalsIgnoreCase (propText "standardOutputSha256" receipt) (propText "standardOutputSha256" command))
            || not (equalsIgnoreCase (propText "standardErrorSha256" receipt) (propText "standardErrorSha256" command))
        then
            failNow (sprintf "Execution receipt '%s' does not match the execution manifest." expectedReceipt)

        assertCanonicalReleaseCommand receipt expectedCommand (sprintf "Execution receipt '%s'" expectedReceipt)

        let struct (_, stdoutSha256, stdoutText) =
            getExecutionLogText
                executionDirectory
                expectedOutputLog
                (propText "standardOutputSha256" command)
                artifactsDirectory

        let struct (_, stderrSha256, stderrText) =
            getExecutionLogText
                executionDirectory
                expectedErrorLog
                (propText "standardErrorSha256" command)
                artifactsDirectory

        logTextByName.[name] <- stdoutText + Environment.NewLine + stderrText

        let entry = JsonObject()
        entry.["name"] <- jstr name
        entry.["fileName"] <- jstr expectedCommand.FileName
        entry.["arguments"] <- jsonStringArray expectedCommand.Arguments
        entry.["workingDirectory"] <- jstr expectedCommand.WorkingDirectory
        entry.["receipt"] <- jstr expectedReceipt
        entry.["receiptSha256"] <- jstr (sha256File receiptPath)
        entry.["standardOutputLog"] <- jstr expectedOutputLog
        entry.["standardOutputSha256"] <- jstr stdoutSha256
        entry.["standardErrorLog"] <- jstr expectedErrorLog
        entry.["standardErrorSha256"] <- jstr stderrSha256
        verifiedCommands.Add entry

    let expectedLogPaths =
        [ for index in 0 .. expectedNames.Length - 1 do
              let prefix = sprintf "%02d-%s" (index + 1) expectedNames.[index]
              yield "logs/" + prefix + ".stdout.log"
              yield "logs/" + prefix + ".stderr.log" ]

    let logsDirectory = assertSafeArtifactsSubdirectory (Path.Combine(executionDirectory, "logs")) artifactsDirectory

    let actualLogNames =
        Directory.GetFileSystemEntries logsDirectory
        |> Array.map (fun item ->
            let attributes = File.GetAttributes item

            if
                hasFlag FileAttributes.Directory attributes
                || hasFlag FileAttributes.ReparsePoint attributes
            then
                failNow (sprintf "Execution logs directory contains an invalid entry: '%s'." (Path.GetFullPath item))

            fileNameOf item)
        |> Array.sortWith (fun left right -> StringComparer.OrdinalIgnoreCase.Compare(left, right))
        |> Array.toList

    let expectedLogNames = expectedLogPaths |> List.map fileNameOf |> sortedIgnoreCase

    if actualLogNames.Length <> expectedLogNames.Length || not (exactStringSequence actualLogNames expectedLogNames) then
        failNow "Execution logs directory does not contain exactly the expected fixed log set."

    let receiptsDirectory =
        assertSafeArtifactsSubdirectory (Path.Combine(executionDirectory, "receipts")) artifactsDirectory

    let actualReceiptNames =
        Directory.GetFileSystemEntries receiptsDirectory
        |> Array.map (fun item ->
            let attributes = File.GetAttributes item

            if
                hasFlag FileAttributes.Directory attributes
                || hasFlag FileAttributes.ReparsePoint attributes
            then
                failNow (sprintf "Execution receipts directory contains an invalid entry: '%s'." (Path.GetFullPath item))

            fileNameOf item)
        |> Array.sortWith (fun left right -> StringComparer.OrdinalIgnoreCase.Compare(left, right))
        |> Array.toList

    let expectedReceiptNames =
        [ for index in 0 .. expectedNames.Length - 1 ->
              sprintf "%02d-%s.json" (index + 1) expectedNames.[index] ]

    if
        actualReceiptNames.Length <> expectedReceiptNames.Length
        || not (sameStringSet actualReceiptNames expectedReceiptNames)
    then
        failNow "Execution receipts directory does not contain exactly the expected fixed receipt set."

    let logFor (name: string) : string =
        match logTextByName.TryGetValue name with
        | true, value -> value
        | _ -> ""

    assertBuildMarkers (logFor "build")
    let testCount = assertTestMarkers (logFor "test") root
    assertExampleMarkers (logFor "examples") (logFor "aspnetcore-examples")

    let mutable benchmarkEvidence = JsonObject()
    benchmarkEvidence.["status"] <- jstr "skipped-by-protocol"

    if not skipBenchmarks then
        let executed = assertBenchmarkMarkers (logFor "benchmark")

        let proposalPath =
            getSafeEvidenceFile
                (Path.Combine(executionDirectory, "performance-observation-proposal.json"))
                artifactsDirectory

        use proposalDocument = JsonDocument.Parse(File.ReadAllText proposalPath)
        let proposal = proposalDocument.RootElement

        if propInt "schemaVersion" proposal <> Some 1 || (propArray "rows" proposal).IsEmpty then
            failNow "Performance observation proposal is missing or malformed."

        let reports =
            assertBenchmarkReports executionDirectory artifactsDirectory root (propArray "rows" proposal)

        reports.["executed"] <- jint executed
        reports.["observationProposal"] <- jstr "performance-observation-proposal.json"
        reports.["observationProposalSha256"] <- jstr (sha256File proposalPath)
        benchmarkEvidence <- reports

    let buildNode = JsonObject()
    buildNode.["warnings"] <- jint 0
    buildNode.["errors"] <- jint 0

    let testsNode = JsonObject()
    testsNode.["total"] <- jint testCount
    testsNode.["succeeded"] <- jint testCount
    testsNode.["failed"] <- jint 0
    testsNode.["skipped"] <- jint 0

    testsNode.["assemblies"] <-
        jsonStringArray (testAssemblyRelativePaths |> List.map (fun relative -> Path.GetFullPath(Path.Combine(root, relative))))

    let examplesNode = JsonObject()
    examplesNode.["core"] <- jstr "passed"
    examplesNode.["aspNetCore"] <- jstr "passed"

    let formatterNode = JsonObject()
    formatterNode.["exitCode"] <- jint 0

    let node = JsonObject()
    node.["directory"] <- jstr executionDirectory
    node.["manifest"] <- jstr "execution-evidence.json"
    node.["manifestSha256"] <- jstr (sha256File evidencePath)
    node.["sourceFingerprint"] <- currentFingerprint
    node.["commands"] <- verifiedCommands
    node.["build"] <- buildNode
    node.["tests"] <- testsNode
    node.["examples"] <- examplesNode
    node.["formatter"] <- formatterNode
    node.["benchmarks"] <- benchmarkEvidence
    node
