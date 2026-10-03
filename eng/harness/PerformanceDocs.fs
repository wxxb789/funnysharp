module FunnySharp.Harness.PerformanceDocs

// A behaviour-identical F# port of eng/Generate-PerformanceDocumentation.ps1.
// Generates or verifies the marker-delimited performance tables in the guides a
// performance manifest names, in both modes. Verdict lines:
//   Generated <n> performance documentation regions.
//   Verified <n> performance documentation regions.
// The generated region is LF-only and byte-compared, so identical manifest inputs
// yield a byte-identical region and -Verify never writes.

open System
open System.Collections.Generic
open System.Globalization
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open FunnySharp.Harness.Output
open FunnySharp.Harness.Repo

let private usageLine =
    "usage: Generate-PerformanceDocumentation [-RepositoryRoot PATH] [-ManifestPath PATH] [-Verify]"

let private utf8NoBom = UTF8Encoding(false)

// ---- JSON helpers ----

let private tryProp (name: string) (element: JsonElement) : JsonElement option =
    let mutable value = Unchecked.defaultof<JsonElement>

    if element.ValueKind = JsonValueKind.Object && element.TryGetProperty(name, &value) then
        Some value
    else
        None

let private requireProp (name: string) (element: JsonElement) : JsonElement =
    match tryProp name element with
    | Some value -> value
    | None -> raise (KeyNotFoundException(sprintf "JSON property '%s' was not found." name))

let private stringValue (element: JsonElement) : string =
    if element.ValueKind = JsonValueKind.String then
        Option.ofObj (element.GetString()) |> Option.defaultValue ""
    else
        element.ToString()

let private stringOf (name: string) (element: JsonElement) : string =
    element |> tryProp name |> Option.map stringValue |> Option.defaultValue ""

let private boolOf (name: string) (element: JsonElement) : bool =
    match tryProp name element with
    | Some value -> value.ValueKind = JsonValueKind.True
    | None -> false

let private isNullElement (value: JsonElement option) : bool =
    match value with
    | None -> true
    | Some element -> element.ValueKind = JsonValueKind.Null

let private doubleValue (value: JsonElement option) : float =
    match value with
    | Some element when element.ValueKind = JsonValueKind.Number -> element.GetDouble()
    | Some element when element.ValueKind = JsonValueKind.String ->
        match Double.TryParse(stringValue element, NumberStyles.Float, CultureInfo.InvariantCulture) with
        | true, parsed -> parsed
        | _ -> 0.0
    | _ -> 0.0

let private elements (value: JsonElement option) : JsonElement list =
    match value with
    | Some element when element.ValueKind = JsonValueKind.Array -> element.EnumerateArray() |> List.ofSeq
    | _ -> []

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
    | Some element when element.ValueKind = JsonValueKind.String ->
        match Int32.TryParse(stringValue element, NumberStyles.Integer, CultureInfo.InvariantCulture) with
        | true, parsed -> Some parsed
        | _ -> None
    | _ -> None

/// PowerShell `-cne` over the manifest field types: case-sensitive ordinal strings,
/// booleans by kind, numbers by value, null equal only to null.
let private jsonEquals (left: JsonElement option) (right: JsonElement option) : bool =
    match left, right with
    | None, None -> true
    | Some a, Some b ->
        match a.ValueKind, b.ValueKind with
        | JsonValueKind.String, JsonValueKind.String ->
            String.Equals(stringValue a, stringValue b, StringComparison.Ordinal)
        | JsonValueKind.True, JsonValueKind.True -> true
        | JsonValueKind.False, JsonValueKind.False -> true
        | JsonValueKind.Number, JsonValueKind.Number -> a.GetDouble() = b.GetDouble()
        | JsonValueKind.Null, JsonValueKind.Null -> true
        | _ -> false
    | _ -> false

// ---- Fingerprints ----

let private toHex (bytes: byte array) : string =
    Convert.ToHexString(bytes).ToLowerInvariant()

let private sha256Hex (bytes: byte array) : string =
    toHex (SHA256.HashData bytes)

let private textSha256 (text: string) : string =
    sha256Hex (Encoding.UTF8.GetBytes text)

let private fileSha256 (path: string) : string =
    use stream = File.OpenRead path
    toHex (SHA256.HashData stream)

let private policyFingerprint (manifestPath: string) : string =
    use document = JsonDocument.Parse(File.ReadAllText manifestPath)
    textSha256 ((requireProp "policy" document.RootElement).GetRawText())

let private fileSetFingerprint (root: string) (files: string list) : Result<string, HarnessError> =
    let rootPath = (Path.GetFullPath root).TrimEnd('\\', '/')

    let comparison =
        if OperatingSystem.IsWindows() then
            StringComparison.OrdinalIgnoreCase
        else
            StringComparison.Ordinal

    let sorted = files |> List.sortWith (fun a b -> String.CompareOrdinal(a, b))
    use stream = new MemoryStream()

    let rec loop (remaining: string list) =
        match remaining with
        | [] -> Ok(sha256Hex (stream.ToArray()))
        | relativePath :: rest ->
            let segments = relativePath.Split([| '\\'; '/' |])

            if Path.IsPathFullyQualified relativePath || Array.contains ".." segments then
                Error(Errors.create (sprintf "Fingerprint path must be repository-relative: '%s'." relativePath))
            else
                let path = Path.GetFullPath(Path.Combine(rootPath, relativePath))
                let prefix = rootPath + string Path.DirectorySeparatorChar

                if not (path.StartsWith(prefix, comparison)) || not (File.Exists path) then
                    Error(
                        Errors.create(
                            sprintf "Fingerprint input was not found inside the repository: '%s'." relativePath
                        )
                    )
                else
                    let line = relativePath.Replace('\\', '/') + string '\000' + fileSha256 path + string '\n'
                    let bytes = Encoding.UTF8.GetBytes line
                    stream.Write(bytes, 0, bytes.Length)
                    loop rest

    loop sorted

// ---- Table formatting (invariant culture, LF only) ----

let private isObserved (state: string) : bool =
    String.Equals(state, "observed", StringComparison.OrdinalIgnoreCase)

let private formatMean (row: JsonElement) : string =
    let state = stringOf "timingState" row
    let mean = tryProp "meanNanoseconds" row

    if not (isObserved state) || isNullElement mean then
        "N/A"
    else
        let value = doubleValue mean

        if value >= 1000.0 then
            (value / 1000.0).ToString("N3", CultureInfo.InvariantCulture) + " us"
        else
            value.ToString("N3", CultureInfo.InvariantCulture) + " ns"

let private formatAllocation (row: JsonElement) : string =
    let bytes =
        match tryProp "allocatedBytesPerOperation" row with
        | Some element when element.ValueKind = JsonValueKind.Number ->
            let mutable parsed = 0L
            if element.TryGetInt64(&parsed) then parsed else int64 (element.GetDouble())
        | _ -> 0L

    bytes.ToString(CultureInfo.InvariantCulture) + " B"

let private formatRatio (baseline: JsonElement) (candidate: JsonElement) : string =
    let baselineState = stringOf "timingState" baseline
    let candidateState = stringOf "timingState" candidate
    let baselineMean = doubleValue (tryProp "meanNanoseconds" baseline)

    if isObserved baselineState && isObserved candidateState && baselineMean > 0.0 then
        (doubleValue (tryProp "meanNanoseconds" candidate) / baselineMean)
            .ToString("N2", CultureInfo.InvariantCulture)
        + "x"
    else
        "N/A"

let private resolvePath (basePath: string) (path: string) : string =
    if Path.IsPathFullyQualified path then
        Path.GetFullPath path
    else
        Path.GetFullPath(Path.Combine(basePath, path))

// ---- One documentation entry ----

let private generateDocument
    (verify: bool)
    (repositoryRoot: string)
    (policyRows: JsonElement list)
    (observationById: Dictionary<string, JsonElement>)
    (document: JsonElement)
    : Result<unit, HarnessError> =
    let relativePath = stringOf "path" document
    let path = resolvePath repositoryRoot relativePath

    if not (File.Exists path) then
        Error(Errors.create (sprintf "Performance guide was not found: '%s'." path))
    else
        let classes = document |> tryProp "benchmarkClasses" |> stringList

        let classContains (row: JsonElement) =
            let benchmarkClass = stringOf "benchmarkClass" row

            classes
            |> List.exists (fun name -> String.Equals(name, benchmarkClass, StringComparison.OrdinalIgnoreCase))

        let rows = policyRows |> List.filter classContains
        let included = rows |> List.filter (boolOf "included")
        let excluded = rows |> List.filter (fun row -> not (boolOf "included" row))

        // Group-Object + Sort-Object: first-seen keys, case-insensitive grouping, then a
        // case-insensitive culture-aware name sort (PowerShell Sort-Object's default).
        let nameComparer = StringComparer.InvariantCultureIgnoreCase
        let groupKeys = ResizeArray<string>()
        let groups = Dictionary<string, ResizeArray<JsonElement>>(nameComparer)

        for row in included do
            let key = stringOf "comparisonGroup" row
            let mutable bucket = ResizeArray<JsonElement>()

            if groups.TryGetValue(key, &bucket) then
                bucket.Add row
            else
                let created = ResizeArray<JsonElement>()
                created.Add row
                groups.[key] <- created
                groupKeys.Add key

        let orderedKeys =
            groupKeys
            |> Seq.toList
            |> List.sortWith (fun a b -> nameComparer.Compare(a, b))

        let generated = ResizeArray<string>()

        generated.Add
            "| Scenario | Baseline mean | Candidate mean | Ratio | Baseline allocation | Candidate allocation |"

        generated.Add "| --- | ---: | ---: | ---: | ---: | ---: |"

        let rec emitGroups (remaining: string list) : Result<unit, HarnessError> =
            match remaining with
            | [] -> Ok()
            | key :: rest ->
                let groupRows = groups.[key] |> Seq.toList
                let baselineRows = groupRows |> List.filter (boolOf "baseline")
                let candidateRows = groupRows |> List.filter (fun row -> not (boolOf "baseline" row))

                if baselineRows.Length <> 1 || candidateRows.IsEmpty then
                    Error(
                        Errors.create(
                            sprintf
                                "Comparison group '%s' must contain exactly one baseline and at least one candidate row."
                                key
                        )
                    )
                else
                    let baselinePolicy = baselineRows.[0]
                    let mutable baselineObservation = Unchecked.defaultof<JsonElement>

                    if not (observationById.TryGetValue(stringOf "id" baselinePolicy, &baselineObservation)) then
                        Error(Errors.create (sprintf "Comparison group '%s' is missing its baseline observation." key))
                    else
                        let candidates =
                            candidateRows
                            |> List.sortWith (fun a b ->
                                nameComparer.Compare(stringOf "method" a, stringOf "method" b))

                        let rec emitCandidates (remainingCandidates: JsonElement list) : Result<unit, HarnessError> =
                            match remainingCandidates with
                            | [] -> Ok()
                            | candidatePolicy :: candidateRest ->
                                let mutable candidateObservation = Unchecked.defaultof<JsonElement>

                                if
                                    not (
                                        observationById.TryGetValue(
                                            stringOf "id" candidatePolicy,
                                            &candidateObservation
                                        )
                                    )
                                then
                                    Error(
                                        Errors.create(
                                            sprintf "Comparison group '%s' is missing an approved observation." key
                                        )
                                    )
                                else
                                    let category = stringOf "category" baselinePolicy
                                    let parameters = stringOf "parameters" baselinePolicy

                                    let scenario =
                                        let baseScenario =
                                            if String.IsNullOrWhiteSpace parameters then
                                                category
                                            else
                                                category + " (" + parameters + ")"

                                        if candidateRows.Length > 1 then
                                            baseScenario + " - " + stringOf "method" candidatePolicy
                                        else
                                            baseScenario

                                    generated.Add(
                                        "| "
                                        + scenario
                                        + " | "
                                        + formatMean baselineObservation
                                        + " | "
                                        + formatMean candidateObservation
                                        + " | "
                                        + formatRatio baselineObservation candidateObservation
                                        + " | "
                                        + formatAllocation baselineObservation
                                        + " | "
                                        + formatAllocation candidateObservation
                                        + " |"
                                    )

                                    emitCandidates candidateRest

                        match emitCandidates candidates with
                        | Error err -> Error err
                        | Ok() -> emitGroups rest

        match emitGroups orderedKeys with
        | Error err -> Error err
        | Ok() ->
            if excluded.Length > 0 then
                generated.Add ""
                generated.Add "Excluded measurements:"

                let sortedExcluded =
                    excluded
                    |> List.sortWith (fun a b ->
                        nameComparer.Compare(stringOf "comparisonGroup" a, stringOf "comparisonGroup" b))

                for row in sortedExcluded do
                    generated.Add("- " + stringOf "comparisonGroup" row + ": " + stringOf "exclusionReason" row)

            let id = stringOf "id" document
            let startMarker = "<!-- performance-table:start " + id + " -->"
            let endMarker = "<!-- performance-table:end " + id + " -->"
            let content = File.ReadAllText path
            let startIndex = content.IndexOf(startMarker, StringComparison.Ordinal)
            let endIndex = content.IndexOf(endMarker, StringComparison.Ordinal)

            if startIndex < 0 || endIndex < 0 || endIndex < startIndex then
                Error(
                    Errors.create(
                        sprintf "Guide '%s' is missing the '%s' generated-region markers." relativePath id
                    )
                )
            else
                let replacement =
                    startMarker + "\n" + String.concat "\n" (List.ofSeq generated) + "\n" + endMarker

                if verify then
                    let existing = content.Substring(startIndex, endIndex + endMarker.Length - startIndex)

                    if String.Equals(existing, replacement, StringComparison.Ordinal) then
                        Ok()
                    else
                        Error(Errors.create (sprintf "Generated performance table is stale: '%s'." relativePath))
                else
                    let updated =
                        content.Substring(0, startIndex)
                        + replacement
                        + content.Substring(endIndex + endMarker.Length)

                    File.WriteAllText(path, updated, utf8NoBom)
                    Ok()

// ---- Command line ----

type private CliOptions =
    { RepositoryRoot: string option
      ManifestPath: string option
      Verify: bool }

let private parseArgs (argv: string list) : Result<CliOptions, string> =
    let empty = { RepositoryRoot = None; ManifestPath = None; Verify = false }

    let tryValueArg (prefix: string) (arg: string) : string option =
        if arg.StartsWith(prefix + "=", StringComparison.Ordinal) || arg.StartsWith(prefix + ":", StringComparison.Ordinal) then
            Some(arg.Substring(prefix.Length + 1))
        else
            None

    let rec loop (options: CliOptions) (remaining: string list) =
        match remaining with
        | [] -> Ok options
        | "-Verify" :: rest -> loop { options with Verify = true } rest
        | "-RepositoryRoot" :: value :: rest -> loop { options with RepositoryRoot = Some value } rest
        | [ "-RepositoryRoot" ] -> Error "argument -RepositoryRoot: expected one argument"
        | "-ManifestPath" :: value :: rest -> loop { options with ManifestPath = Some value } rest
        | [ "-ManifestPath" ] -> Error "argument -ManifestPath: expected one argument"
        | arg :: rest ->
            match tryValueArg "-RepositoryRoot" arg with
            | Some value -> loop { options with RepositoryRoot = Some value } rest
            | None ->
                match tryValueArg "-ManifestPath" arg with
                | Some value -> loop { options with ManifestPath = Some value } rest
                | None -> Error("unrecognized argument: " + arg)

    loop empty argv

// ---- Core ----

let private runCore (options: CliOptions) : Result<string, HarnessError> =
    let repositoryRoot =
        match options.RepositoryRoot with
        | Some root -> Path.GetFullPath root
        | None ->
            match tryFindRoot () with
            | Some root -> root
            | None -> Environment.CurrentDirectory

    let manifestPath =
        match options.ManifestPath with
        | Some value when not (String.IsNullOrWhiteSpace value) -> resolvePath repositoryRoot value
        | _ -> Path.Combine(repositoryRoot, "eng/performance/baseline.json")

    if not (File.Exists manifestPath) then
        Error(Errors.create (sprintf "Performance manifest was not found: '%s'." manifestPath))
    else
        use document = JsonDocument.Parse(File.ReadAllText manifestPath)
        let root = document.RootElement
        let manifestSchema = root |> tryProp "schemaVersion" |> intValue
        let observationSchema = root |> tryProp "observation" |> Option.bind (tryProp "schemaVersion") |> intValue

        if manifestSchema <> Some 1 || observationSchema <> Some 1 then
            Error(Errors.create "Performance manifest and observation must use schemaVersion 1.")
        else
            let policy = requireProp "policy" root
            let observation = requireProp "observation" root
            let computedPolicyFingerprint = policyFingerprint manifestPath

            let inputFiles =
                root |> tryProp "benchmarkInput" |> Option.bind (tryProp "files") |> stringList

            let protocolFiles = root |> tryProp "protocol" |> Option.bind (tryProp "files") |> stringList

            match fileSetFingerprint repositoryRoot inputFiles, fileSetFingerprint repositoryRoot protocolFiles with
            | Error err, _ -> Error err
            | _, Error err -> Error err
            | Ok inputFingerprint, Ok protocolFingerprint ->
                let matches (left: string) (right: string) =
                    String.Equals(left, right, StringComparison.OrdinalIgnoreCase)

                let policyRevision = stringOf "revision" policy

                if
                    not (matches (stringOf "policyRevision" observation) policyRevision)
                    || not (matches (stringOf "policyFingerprint" observation) computedPolicyFingerprint)
                    || not (matches (stringOf "benchmarkInputFingerprint" observation) inputFingerprint)
                    || not (matches (stringOf "protocolFingerprint" observation) protocolFingerprint)
                then
                    Error(
                        Errors.create
                            "The approved observation does not match the current performance policy, input, or protocol."
                    )
                else
                    let policyRows = policy |> tryProp "rows" |> elements
                    let includedPolicyRows = policyRows |> List.filter (boolOf "included")
                    let policyById = Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)

                    for row in includedPolicyRows do
                        policyById.[stringOf "id" row] <- row

                    let observationRows = observation |> tryProp "rows" |> elements
                    let observationById = Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)

                    let mismatchedFields =
                        [ "benchmarkClass"; "category"; "method"; "parameters"; "baseline" ]

                    let rec registerRows (remaining: JsonElement list) : Result<unit, HarnessError> =
                        match remaining with
                        | [] -> Ok()
                        | row :: rest ->
                            let id = stringOf "id" row

                            if
                                String.IsNullOrWhiteSpace id
                                || observationById.ContainsKey id
                                || not (policyById.ContainsKey id)
                            then
                                Error(
                                    Errors.create(
                                        sprintf
                                            "The approved observation contains a missing, duplicate, or unregistered row '%s'."
                                            id
                                    )
                                )
                            else
                                let policyRow = policyById.[id]

                                match
                                    mismatchedFields
                                    |> List.tryFind (fun name ->
                                        not (jsonEquals (tryProp name row) (tryProp name policyRow)))
                                with
                                | Some name ->
                                    Error(
                                        Errors.create(
                                            sprintf
                                                "The approved observation row '%s' does not match policy field '%s'."
                                                id
                                                name
                                        )
                                    )
                                | None ->
                                    observationById.[id] <- row
                                    registerRows rest

                    match registerRows observationRows with
                    | Error err -> Error err
                    | Ok() ->
                        if observationById.Count <> includedPolicyRows.Length then
                            Error(
                                Errors.create(
                                    sprintf
                                        "The approved observation row count %d does not match policy count %d."
                                        observationById.Count
                                        includedPolicyRows.Length
                                )
                            )
                        else
                            let documentation = root |> tryProp "documentation" |> elements

                            let rec generateDocuments (remaining: JsonElement list) : Result<unit, HarnessError> =
                                match remaining with
                                | [] -> Ok()
                                | documentElement :: rest ->
                                    match
                                        generateDocument
                                            options.Verify
                                            repositoryRoot
                                            policyRows
                                            observationById
                                            documentElement
                                    with
                                    | Error err -> Error err
                                    | Ok() -> generateDocuments rest

                            match generateDocuments documentation with
                            | Error err -> Error err
                            | Ok() ->
                                let verb = if options.Verify then "Verified" else "Generated"

                                Ok(sprintf "%s %d performance documentation regions." verb documentation.Length)

/// Run the generator/verifier writing to the supplied writers. Returns the legacy
/// mapping: 0 pass, 1 verification failure, 2 usage failure.
let mainWith (stdout: TextWriter) (stderr: TextWriter) (argv: string list) : int =
    match parseArgs argv with
    | Error message ->
        stderr.WriteLine usageLine
        stderr.WriteLine("Generate-PerformanceDocumentation: error: " + message)
        2
    | Ok options ->
        let result =
            try
                runCore options
            with ex ->
                Error(Errors.create ex.Message)

        match result with
        | Ok line ->
            stdout.WriteLine line
            0
        | Error err ->
            stderr.WriteLine("ERROR: " + err.Message)
            err.ExitCode

/// Entry point mirroring eng/Generate-PerformanceDocumentation.ps1's main().
let main (argv: string array) : int =
    mainWith Console.Out Console.Error (List.ofArray argv)
