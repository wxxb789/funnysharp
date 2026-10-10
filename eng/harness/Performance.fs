module FunnySharp.Harness.Performance

// Verifies current benchmark
// receipts against the tracked allocation policy. The tracked manifest owns
// policy; the receipts own observations. Allocation is blocking evidence and
// timing is directional (never blocking); the verifier never edits a budget or
// an exclusion and only writes the optional observation proposal.
//
// Exit-code mapping matches the harness contract: 0 pass, 1 verification
// failure (every PowerShell throw), 2 usage/environment failure.

open System
open System.Collections.Generic
open System.Globalization
open System.IO
open System.Text
open System.Text.Json
open FunnySharp.Harness.Output
open FunnySharp.Harness.Repo

/// The internal, exception-flavoured failure transport. `run` converts it back
/// to the harness `Result` before returning, so the public surface stays typed.
exception PerformanceFailure of HarnessError

let private failNow (message: string) : 'T =
    raise (PerformanceFailure(Errors.create message))

let private unwrap (result: Result<'T, HarnessError>) : 'T =
    match result with
    | Ok value -> value
    | Error error -> raise (PerformanceFailure error)

// ---- JSON scalar helpers (conversion-safe and nullness-safe) ----

let private getPropertyOrNull (parent: JsonElement) (name: string) : JsonElement =
    match parent.TryGetProperty name with
    | true, value -> value
    | _ -> JsonElement()

/// The text PowerShell's `[string]` cast would produce for a JSON value.
let private scalarText (element: JsonElement) : string =
    match element.ValueKind with
    | JsonValueKind.String -> Option.ofObj (element.GetString()) |> Option.defaultValue ""
    | JsonValueKind.Undefined
    | JsonValueKind.Null -> ""
    | JsonValueKind.True -> "True"
    | JsonValueKind.False -> "False"
    | _ -> element.GetRawText()

let private tryJsonBool (element: JsonElement) : bool option =
    match element.ValueKind with
    | JsonValueKind.True -> Some true
    | JsonValueKind.False -> Some false
    | _ -> None

let private tryJsonNumber (element: JsonElement) : float option =
    if element.ValueKind = JsonValueKind.Number then
        match element.TryGetDouble() with
        | true, value -> Some value
        | _ -> None
    else
        None

/// A JSON number that would deserialize to a .NET integer type (no fraction or
/// exponent), mirroring the script's Test-Integer.
let private tryJsonInteger (element: JsonElement) : decimal option =
    if element.ValueKind = JsonValueKind.Number then
        let raw = element.GetRawText()

        if raw.IndexOfAny([| '.'; 'e'; 'E' |]) >= 0 then
            None
        else
            match Decimal.TryParse(raw, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture) with
            | true, value -> Some value
            | _ -> None
    else
        None

/// PowerShell's `-cne` for scalar values.
let private scalarEquals (left: JsonElement) (right: JsonElement) : bool =
    match left.ValueKind, right.ValueKind with
    | JsonValueKind.String, JsonValueKind.String ->
        String.Equals(left.GetString(), right.GetString(), StringComparison.Ordinal)
    | JsonValueKind.True, JsonValueKind.True -> true
    | JsonValueKind.False, JsonValueKind.False -> true
    | JsonValueKind.Number, JsonValueKind.Number ->
        match left.TryGetDouble(), right.TryGetDouble() with
        | (true, a), (true, b) -> a = b
        | _ -> left.GetRawText() = right.GetRawText()
    | JsonValueKind.Null, JsonValueKind.Null -> true
    | JsonValueKind.Undefined, JsonValueKind.Undefined -> true
    | JsonValueKind.Null, JsonValueKind.Undefined
    | JsonValueKind.Undefined, JsonValueKind.Null -> true
    | _ -> false

/// PowerShell's `-ne` for strings (case-insensitive).
let private psTextEquals (left: string) (right: string) : bool =
    String.Equals(left, right, StringComparison.OrdinalIgnoreCase)

let private arrayItems (element: JsonElement) : JsonElement list =
    if element.ValueKind = JsonValueKind.Array then
        element.EnumerateArray() |> Seq.toList
    else
        []

let private formatInteger (value: decimal) : string =
    value.ToString("0", CultureInfo.InvariantCulture)

let private fileNameOf (path: string) : string =
    match Path.GetFileName path with
    | null -> path
    | name -> name

let private distinctIgnoreCase (values: string list) : string list =
    let seen = HashSet<string>(StringComparer.OrdinalIgnoreCase)
    values |> List.filter seen.Add

let environmentIdentity (environment: JsonElement) : Result<string, HarnessError> =
    try
        let requireNonBlank (name: string) : string =
            let value = scalarText (getPropertyOrNull environment name)

            if String.IsNullOrWhiteSpace value then
                failNow (sprintf "Performance environment is missing '%s'." name)

            value

        let os = requireNonBlank "os"
        let architecture = requireNonBlank "architecture"
        let sdkVersion = requireNonBlank "sdkVersion"
        let runtime = requireNonBlank "runtime"
        let jit = requireNonBlank "jit"

        let gcServer = tryJsonBool (getPropertyOrNull environment "gcServer")
        let gcConcurrent = tryJsonBool (getPropertyOrNull environment "gcConcurrent")

        let gcAllocationQuantum =
            match tryJsonInteger (getPropertyOrNull environment "gcAllocationQuantum") with
            | Some value when value >= 0m -> Some value
            | _ -> None

        match gcServer, gcConcurrent, gcAllocationQuantum with
        | Some server, Some concurrent, Some quantum ->
            let values =
                [ os
                  architecture
                  sdkVersion
                  runtime
                  jit
                  (if server then "true" else "false")
                  (if concurrent then "true" else "false")
                  formatInteger quantum ]

            Ok(String.concat "\000" values)
        | _ -> failNow "Performance environment contains invalid GC data."
    with
    | PerformanceFailure error -> Error error
    | ex -> Error(Errors.create ex.Message)

let private evidencePath (directory: string) (evidence: JsonElement) : string =
    let name = scalarText (getPropertyOrNull evidence "file")
    if String.IsNullOrWhiteSpace name || name.Contains('/') || name.Contains('\\') then
        failNow "Run binding contains an invalid evidence path."
    let path = Path.Combine(directory, name)
    if not (File.Exists path) then
        failNow "Run binding evidence is missing."
    path

let private verifyRunBinding
    (root: string) (directory: string) (manifest: JsonElement)
    (receipt: JsonElement) (commit: string)
    : unit =
    let binding = getPropertyOrNull receipt "binding"
    if binding.ValueKind <> JsonValueKind.Object then
        failNow "Fresh receipt is missing its preflight/workload binding."
    if scalarText (getPropertyOrNull receipt "candidateCommit") <> commit then
        failNow "Receipt is missing or has the wrong Git source commit."
    let preflightPath = evidencePath directory (getPropertyOrNull binding "preflight")
    use preflightDocument = JsonDocument.Parse(File.ReadAllText preflightPath)
    let preflight = preflightDocument.RootElement
    if scalarText (getPropertyOrNull preflight "schemaVersion") <> "1"
       || scalarText (getPropertyOrNull preflight "candidateCommit") <> commit
       || scalarText (getPropertyOrNull preflight "policyRevision") <>
            scalarText (getPropertyOrNull (getPropertyOrNull manifest "policy") "revision") then
        failNow "Preflight does not match the candidate or policy."
    let runId = scalarText (getPropertyOrNull preflight "runId")
    if runId.Length <> 32 || not (runId |> Seq.forall Uri.IsHexDigit) then
        failNow "Preflight has an invalid run identity."
    let runtime = scalarText (getPropertyOrNull preflight "runtime")
    if String.IsNullOrWhiteSpace runtime then
        failNow "Preflight is missing its runtime."
    let completed = DateTimeOffset.Parse(
        scalarText (getPropertyOrNull preflight "completedAtUtc"), CultureInfo.InvariantCulture)
    let generated = DateTimeOffset.Parse(
        scalarText (getPropertyOrNull receipt "generatedAtUtc"), CultureInfo.InvariantCulture)
    let policyRows =
        arrayItems (getPropertyOrNull (getPropertyOrNull manifest "policy") "rows")
    let included =
        policyRows |> List.filter (fun row -> tryJsonBool (getPropertyOrNull row "included") = Some true)
    let expected = Dictionary<string, JsonElement>(StringComparer.Ordinal)
    for row in included do
        expected.Add(scalarText (getPropertyOrNull row "id"), row)
    let seen = HashSet<string>(StringComparer.Ordinal)
    for row in arrayItems (getPropertyOrNull preflight "rows") do
        let id = scalarText (getPropertyOrNull row "id")
        if not (seen.Add id) || not (expected.ContainsKey id) then
            failNow "Preflight contains duplicate or unknown rows."
        for name in [ "benchmarkClass"; "category"; "method"; "parameters"; "baseline" ] do
            if not (scalarEquals (getPropertyOrNull row name) (getPropertyOrNull expected[id] name)) then
                failNow "Preflight does not match a policy descriptor or baseline."
        if tryJsonBool (getPropertyOrNull row "semanticsChecked") <> Some true then
            failNow "Preflight is missing a semantic outcome check."
    if not (seen.SetEquals expected.Keys) then
        failNow "Preflight does not cover every included policy/parameter row."
    let expectedCases =
        included
        |> List.map (fun row ->
            scalarText (getPropertyOrNull row "benchmarkClass") + "|"
            + scalarText (getPropertyOrNull row "parameters"))
        |> Set.ofList
    let actualCases = arrayItems (getPropertyOrNull preflight "semanticCases") |> List.map scalarText
    if Set.ofList actualCases <> expectedCases || actualCases.Length <> expectedCases.Count then
        failNow "Preflight has incomplete or duplicate semantic cases."
    let expectedExclusions =
        policyRows
        |> List.filter (fun row -> tryJsonBool (getPropertyOrNull row "included") <> Some true)
        |> List.map (fun row -> scalarText (getPropertyOrNull row "id"))
        |> Set.ofList
    let actualExclusions = arrayItems (getPropertyOrNull preflight "excludedIds") |> List.map scalarText
    if Set.ofList actualExclusions <> expectedExclusions
       || actualExclusions.Length <> expectedExclusions.Count then
        failNow "Preflight does not account for every explicit exclusion."
    let rows = arrayItems (getPropertyOrNull receipt "rows")
    let receiptRows = Dictionary<string, JsonElement>(StringComparer.Ordinal)
    for row in rows do
        receiptRows.Add(scalarText (getPropertyOrNull row "id"), row)
    let launched = HashSet<string>(StringComparer.Ordinal)
    let launchFiles = HashSet<string>(StringComparer.Ordinal)
    for launch in arrayItems (getPropertyOrNull binding "launches") do
        let rowId = scalarText (getPropertyOrNull launch "rowId")
        if not (receiptRows.ContainsKey rowId) then
            failNow "Workload launch refers to an unknown receipt row."
        let path = evidencePath directory launch
        if not (launchFiles.Add path) then
            failNow "Workload launch evidence was reused."
        let prefix = "// funnysharp-workload:"
        let markers =
            File.ReadAllLines path
            |> Array.filter (fun line -> line.StartsWith(prefix, StringComparison.Ordinal))
        if markers.Length <> 1 then
            failNow "Every actual child launch must contain exactly one workload binding."
        let payload = Encoding.UTF8.GetString(Convert.FromBase64String(markers[0].Substring(prefix.Length)))
        use childDocument = JsonDocument.Parse payload
        let child = childDocument.RootElement
        let row = receiptRows[rowId]
        if scalarText (getPropertyOrNull child "runId") <> runId
           || scalarText (getPropertyOrNull child "candidateCommit") <> commit
           || scalarText (getPropertyOrNull child "runtime") <> runtime
           || scalarText (getPropertyOrNull child "benchmarkClass") <>
                scalarText (getPropertyOrNull row "benchmarkClass")
           || scalarText (getPropertyOrNull child "parameters") <>
                scalarText (getPropertyOrNull row "parameters") then
            failNow "Measured child has the wrong run, candidate, runtime, class, or parameters."
        match tryJsonInteger (getPropertyOrNull child "processId") with
        | Some value when value > 0m -> ()
        | _ -> failNow "Measured child has no process identity."
        let captured = DateTimeOffset.Parse(
            scalarText (getPropertyOrNull child "capturedAtUtc"), CultureInfo.InvariantCulture)
        if captured < completed || captured > generated then
            failNow "Measurement did not follow the bound preflight."
        if String.IsNullOrWhiteSpace(scalarText (getPropertyOrNull child "workloadAssembly")) then
            failNow "Measured child has no workload assembly identity."
        launched.Add(rowId) |> ignore
    if not (launched.SetEquals receiptRows.Keys) then
        failNow "A measured row has no actual child workload binding."
type private PolicyRow =
    { Id: string
      BenchmarkClass: JsonElement
      Category: JsonElement
      Method: JsonElement
      Parameters: JsonElement
      Baseline: JsonElement
      Included: bool
      AllocationBudget: decimal }

type private ReceiptSummary =
    { File: string
      EnvironmentElement: JsonElement }

/// Serialise the observation-only proposal with the ordered key set from A1.
let private buildProposal
    (generatedAtUtc: string)
    (candidateCommit: string option)
    (policyRevision: string)
    (receipts: ReceiptSummary list)
    (rows: JsonElement list)
    : string =
    use stream = new MemoryStream()
    let options = JsonWriterOptions(Indented = true)
    use writer = new Utf8JsonWriter(stream, options)
    writer.WriteStartObject()
    writer.WriteNumber("schemaVersion", 1)
    writer.WriteString("generatedAtUtc", generatedAtUtc)

    match candidateCommit with
    | Some value -> writer.WriteString("candidateCommit", value)
    | None -> writer.WriteNull "candidateCommit"

    writer.WriteString("policyRevision", policyRevision)
    writer.WriteStartArray "receipts"

    for receipt in receipts do
        writer.WriteStartObject()
        writer.WriteString("file", receipt.File)
        writer.WritePropertyName "environment"
        receipt.EnvironmentElement.WriteTo writer
        writer.WriteEndObject()

    writer.WriteEndArray()
    writer.WriteStartArray "rows"

    for row in rows do
        row.WriteTo writer

    writer.WriteEndArray()
    writer.WriteEndObject()
    writer.Flush()
    Encoding.UTF8.GetString(stream.ToArray())

/// Verify every `*-performance-receipt.json` in `receiptDirectory` against the
/// manifest under `repositoryRoot`. Returns the single success line, or the
/// typed failure. `observationProposalPath` is the only file this can write.
let run
    (repositoryRoot: string)
    (manifestPath: string)
    (receiptDirectory: string)
    (observationProposalPath: string option)
    : Result<string, HarnessError> =
    try
        if not (File.Exists manifestPath) then
            failNow (sprintf "Performance manifest was not found: '%s'." manifestPath)

        if not (Directory.Exists receiptDirectory) then
            failNow (sprintf "Performance receipt directory was not found: '%s'." receiptDirectory)

        let git = FunnySharp.Harness.Proc.runCaptureIn (Some repositoryRoot) "git" [ "rev-parse"; "HEAD" ] |> Async.RunSynchronously
        if git.ExitCode <> 0 then failNow "Performance verification requires a Git checkout."
        let commit = git.Stdout.Trim()

        let manifest =
            use document = JsonDocument.Parse(File.ReadAllText manifestPath)
            document.RootElement.Clone()

        match tryJsonNumber (getPropertyOrNull manifest "schemaVersion") with
        | Some value when value = 1.0 -> ()
        | _ -> failNow "Performance manifest must use schemaVersion 1."

        let policyElement = getPropertyOrNull manifest "policy"
        let policyRevision = scalarText (getPropertyOrNull policyElement "revision")

        if String.IsNullOrWhiteSpace policyRevision then
            failNow "Performance policy revision is required."

        let policyRows = ResizeArray<PolicyRow>()
        let policyById = Dictionary<string, PolicyRow>(StringComparer.OrdinalIgnoreCase)

        for row in arrayItems (getPropertyOrNull policyElement "rows") do
            let id = scalarText (getPropertyOrNull row "id")

            if String.IsNullOrWhiteSpace id || policyById.ContainsKey id then
                failNow (sprintf "Performance policy contains a missing or duplicate row id: '%s'." id)

            let comparisonGroup = scalarText (getPropertyOrNull row "comparisonGroup")
            let carrier = scalarText (getPropertyOrNull row "carrier")
            let completionPath = scalarText (getPropertyOrNull row "completionPath")
            let expectedResult = scalarText (getPropertyOrNull row "expectedResult")

            if
                String.IsNullOrWhiteSpace comparisonGroup
                || String.IsNullOrWhiteSpace carrier
                || String.IsNullOrWhiteSpace completionPath
                || String.IsNullOrWhiteSpace expectedResult
            then
                failNow (sprintf "Performance policy row '%s' is missing comparison semantics." id)

            let included = tryJsonBool (getPropertyOrNull row "included") |> Option.defaultValue false
            let exclusionReason = scalarText (getPropertyOrNull row "exclusionReason")

            let allocationBudget =
                if included then
                    let budget =
                        match tryJsonInteger (getPropertyOrNull row "allocationBudgetBytes") with
                        | Some value when value >= 0m -> value
                        | _ ->
                            failNow (
                                sprintf
                                    "Included performance row '%s' must have a non-negative integer allocation budget."
                                    id
                            )

                    if not (String.IsNullOrWhiteSpace exclusionReason) then
                        failNow (sprintf "Included performance row '%s' cannot have an exclusion reason." id)

                    budget
                else
                    if String.IsNullOrWhiteSpace exclusionReason then
                        failNow (sprintf "Excluded performance row '%s' must explain the exclusion." id)

                    0m

            let policyRow =
                { Id = id
                  BenchmarkClass = getPropertyOrNull row "benchmarkClass"
                  Category = getPropertyOrNull row "category"
                  Method = getPropertyOrNull row "method"
                  Parameters = getPropertyOrNull row "parameters"
                  Baseline = getPropertyOrNull row "baseline"
                  Included = included
                  AllocationBudget = allocationBudget }

            policyRows.Add policyRow
            policyById[id] <- policyRow

        let includedRows = policyRows |> Seq.filter (fun row -> row.Included) |> Seq.toList

        if includedRows.IsEmpty then
            failNow "Performance policy must include at least one measured row."
        let groups =
            arrayItems (getPropertyOrNull policyElement "rows")
            |> List.filter (fun row -> tryJsonBool (getPropertyOrNull row "included") = Some true)
            |> List.groupBy (fun row -> scalarText (getPropertyOrNull row "comparisonGroup"))
        for _, rows in groups do
            if rows.Length < 2
               || (rows |> List.filter (fun row ->
                    tryJsonBool (getPropertyOrNull row "baseline") = Some true)).Length <> 1 then
                failNow "Every included comparison group requires exactly one baseline and a candidate."

        let receiptFiles =
            Directory.GetFiles(receiptDirectory, "*-performance-receipt.json")
            |> Array.sortWith (fun a b -> StringComparer.Ordinal.Compare(fileNameOf a, fileNameOf b))

        if receiptFiles.Length = 0 then
            failNow (sprintf "No performance receipt files were found in '%s'." receiptDirectory)

        let receipts = receiptFiles |> Seq.map (fun file ->
            use document = JsonDocument.Parse(File.ReadAllText file)
            file, document.RootElement.Clone())

        let observedById = Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
        let receiptSummaries = ResizeArray<ReceiptSummary>()
        let candidateCommitOrder = ResizeArray<string>()
        let candidateCommitSet = HashSet<string>(StringComparer.Ordinal)
        let environmentKeys = HashSet<string>(StringComparer.Ordinal)

        for receiptFile, receipt in receipts do
            let receiptName = fileNameOf receiptFile

            let schemaOk =
                match tryJsonNumber (getPropertyOrNull receipt "schemaVersion") with
                | Some value when value = 1.0 -> true
                | _ -> false

            let succeeded =
                match tryJsonBool (getPropertyOrNull receipt "succeeded") with
                | Some value -> value
                | None -> false

            if not schemaOk || not succeeded then
                failNow (
                    sprintf "Performance receipt '%s' is unsuccessful or uses an unsupported schema." receiptName
                )
            verifyRunBinding repositoryRoot receiptDirectory manifest receipt commit
            if not (psTextEquals (scalarText (getPropertyOrNull receipt "policyRevision")) policyRevision) then
                failNow (sprintf "Performance receipt '%s' was not measured under the current policy." receiptName)

            let environmentElement = getPropertyOrNull receipt "environment"
            let environmentIdentityValue = unwrap (environmentIdentity environmentElement)
            environmentKeys.Add environmentIdentityValue |> ignore

            let rowElements = arrayItems (getPropertyOrNull receipt "rows")

            let benchmarkClasses =
                rowElements
                |> List.map (fun row -> scalarText (getPropertyOrNull row "benchmarkClass"))
                |> distinctIgnoreCase

            if benchmarkClasses.Length <> 1 then
                failNow (sprintf "Performance receipt '%s' must contain exactly one benchmark class." receiptName)

            let reportPrefix = sprintf "FunnySharp.Benchmarks.%s-report" benchmarkClasses.[0]

            let expectedReports =
                [ reportPrefix + ".csv"; reportPrefix + "-github.md"; reportPrefix + ".html" ]

            let reports = arrayItems (getPropertyOrNull receipt "reports")

            let actualReports =
                reports
                |> List.map (fun report -> scalarText (getPropertyOrNull report "file"))
                |> List.sortWith (fun a b -> StringComparer.Ordinal.Compare(a, b))

            let expectedReportsSorted =
                expectedReports |> List.sortWith (fun a b -> StringComparer.Ordinal.Compare(a, b))

            if reports.Length <> expectedReports.Length || actualReports <> expectedReportsSorted then
                failNow (sprintf "Performance receipt '%s' does not declare the required raw reports." receiptName)

            for report in reports do
                let reportName = scalarText (getPropertyOrNull report "file")

                if reportName.Contains '/' || reportName.Contains '\\' then
                    failNow (
                        sprintf "Performance receipt '%s' contains an invalid report path '%s'." receiptName reportName
                    )

                let reportPath = Path.Combine(receiptDirectory, reportName)

                if
                    not (File.Exists reportPath)
                then
                    failNow (
                        sprintf "Performance report '%s' is missing." reportName
                    )

            let candidateCommit = scalarText (getPropertyOrNull receipt "candidateCommit")

            if not (String.IsNullOrWhiteSpace candidateCommit) && candidateCommitSet.Add candidateCommit then
                candidateCommitOrder.Add candidateCommit

            for rowElement in rowElements do
                let id = scalarText (getPropertyOrNull rowElement "id")

                if String.IsNullOrWhiteSpace id || observedById.ContainsKey id then
                    failNow (sprintf "Performance receipts contain a missing or duplicate row id: '%s'." id)

                match policyById.TryGetValue id with
                | false, _ -> failNow (sprintf "Performance receipt contains unregistered or excluded row '%s'." id)
                | true, policyRow ->
                    if not policyRow.Included then
                        failNow (sprintf "Performance receipt contains unregistered or excluded row '%s'." id)

                    let fields =
                        [ "benchmarkClass", policyRow.BenchmarkClass
                          "category", policyRow.Category
                          "method", policyRow.Method
                          "parameters", policyRow.Parameters
                          "baseline", policyRow.Baseline ]

                    for fieldName, policyValue in fields do
                        if not (scalarEquals (getPropertyOrNull rowElement fieldName) policyValue) then
                            failNow (
                                sprintf "Performance row '%s' does not match policy field '%s'." id fieldName
                            )

                    match tryJsonInteger (getPropertyOrNull rowElement "allocatedBytesPerOperation") with
                    | Some allocated when allocated >= 0m ->
                        if allocated > policyRow.AllocationBudget then
                            failNow (
                                sprintf
                                    "Performance row '%s' allocated %s B, above its %s B budget."
                                    id
                                    (formatInteger allocated)
                                    (formatInteger policyRow.AllocationBudget)
                            )

                        if policyRow.AllocationBudget = 0m && allocated <> 0m then
                            failNow (
                                sprintf
                                    "Zero-allocation performance row '%s' regressed to %s B."
                                    id
                                    (formatInteger allocated)
                            )

                    | _ ->
                        failNow (
                            sprintf
                                "Performance row '%s' has missing, nonnumeric, rounded, or non-integer allocation data."
                                id
                        )

                    let timingState = scalarText (getPropertyOrNull rowElement "timingState")
                    let states = [ "observed"; "below-resolution"; "unavailable" ]

                    if not (states |> List.exists (psTextEquals timingState)) then
                        failNow (sprintf "Performance row '%s' has invalid timing state '%s'." id timingState)

                    let meanElement = getPropertyOrNull rowElement "meanNanoseconds"

                    if psTextEquals timingState "observed" then
                        match tryJsonNumber meanElement with
                        | Some mean when mean > 0.0 -> ()
                        | _ ->
                            failNow (
                                sprintf
                                    "Observed performance row '%s' must contain a positive meanNanoseconds value."
                                    id
                            )
                    elif
                        meanElement.ValueKind <> JsonValueKind.Null
                        && meanElement.ValueKind <> JsonValueKind.Undefined
                    then
                        failNow (
                            sprintf "Performance row '%s' cannot contain a mean for timing state '%s'." id timingState
                        )

                    observedById[id] <- rowElement

            if observationProposalPath.IsSome then
                receiptSummaries.Add(
                    { File = receiptName
                      EnvironmentElement = environmentElement }
                )

        let missingRows =
            includedRows
            |> List.filter (fun row -> not (observedById.ContainsKey row.Id))
            |> List.map (fun row -> row.Id)

        if not missingRows.IsEmpty then
            failNow (sprintf "Required performance rows are missing: %s." (String.concat ", " missingRows))

        if observedById.Count <> includedRows.Length then
            failNow (
                sprintf
                    "Performance receipt row count %d does not match policy count %d."
                    observedById.Count
                    includedRows.Length
            )

        if candidateCommitSet.Count > 1 then
            failNow (
                sprintf
                    "Performance receipts refer to multiple candidate commits: %s."
                    (String.concat ", " (List.ofSeq candidateCommitOrder))
            )

        if environmentKeys.Count <> 1 then
            failNow (sprintf "Performance receipts refer to %d environments instead of one." environmentKeys.Count)

        match observationProposalPath with
        | Some proposalPath ->
            match Option.ofObj (Path.GetDirectoryName proposalPath) with
            | Some directory when directory <> "" -> Directory.CreateDirectory directory |> ignore
            | _ -> ()

            let candidateCommit =
                if candidateCommitSet.Count = 1 then
                    Some candidateCommitOrder.[0]
                else
                    None

            let observedRows =
                observedById.Values
                |> Seq.toList
                |> List.sortBy (fun row ->
                    (scalarText (getPropertyOrNull row "benchmarkClass"),
                     scalarText (getPropertyOrNull row "category"),
                     scalarText (getPropertyOrNull row "method"),
                     scalarText (getPropertyOrNull row "parameters")))

            let proposalJson =
                buildProposal
                    (DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture))
                    candidateCommit
                    policyRevision
                    (List.ofSeq receiptSummaries)
                    observedRows

            File.WriteAllText(proposalPath, proposalJson + Environment.NewLine, UTF8Encoding(false))
        | None -> ()

        Ok(
            sprintf
                "Verified %d included performance rows and %d explicit exclusions across %d receipts."
                includedRows.Length
                (policyRows.Count - includedRows.Length)
                receiptFiles.Length
        )
    with
    | PerformanceFailure error -> Error error
    | ex -> Error(Errors.create ex.Message)

// ---- Command line ----

type private CliOptions =
    { RepositoryRoot: string option
      ManifestPath: string option
      ReceiptDirectory: string option
      ObservationProposalPath: string option
      Help: bool }

let private usageLine =
    "usage: Verify-Performance.ps1 -ReceiptDirectory <dir> [-RepositoryRoot <dir>] [-ManifestPath <path>] [-ObservationProposalPath <path>]"

let private helpText =
    String.concat
        "\n"
        [ usageLine
          ""
          "Verify benchmark receipts against the tracked allocation policy."
          ""
          "options:"
          "  -h, -Help                     show this help message and exit"
          "  -RepositoryRoot <dir>         repository root (default: resolved from the script location)."
          "  -ManifestPath <path>          policy manifest (default: <RepositoryRoot>/eng/performance/baseline.json)."
          "  -ReceiptDirectory <dir>       directory holding *-performance-receipt.json (required)."
          "  -ObservationProposalPath <p>  write the reviewable observation proposal to <p>." ]

let private knownFlags =
    [ "repositoryroot"; "manifestpath"; "receiptdirectory"; "observationproposalpath" ]

let private setFlag (options: CliOptions) (key: string) (value: string) : CliOptions =
    match key with
    | "repositoryroot" -> { options with RepositoryRoot = Some value }
    | "manifestpath" -> { options with ManifestPath = Some value }
    | "receiptdirectory" -> { options with ReceiptDirectory = Some value }
    | "observationproposalpath" -> { options with ObservationProposalPath = Some value }
    | _ -> options

let private parseArgs (argv: string list) : Result<CliOptions, string> =
    let empty =
        { RepositoryRoot = None
          ManifestPath = None
          ReceiptDirectory = None
          ObservationProposalPath = None
          Help = false }

    let rec loop (options: CliOptions) (remaining: string list) =
        match remaining with
        | [] -> Ok options
        | argument :: rest ->
            let separator =
                let equals = argument.IndexOf '='
                let colon = argument.IndexOf ':'
                if equals > 0 then equals elif colon > 0 then colon else -1

            let name, inlineValue =
                if separator > 0 then
                    argument.Substring(0, separator), Some(argument.Substring(separator + 1))
                else
                    argument, None

            let key = name.TrimStart('-').ToLowerInvariant()

            if key = "h" || key = "help" then
                Ok { options with Help = true }
            elif List.contains key knownFlags then
                match inlineValue with
                | Some value -> loop (setFlag options key value) rest
                | None ->
                    match rest with
                    | value :: tail -> loop (setFlag options key value) tail
                    | [] -> Error(sprintf "argument %s: expected one argument" name)
            else
                Error("unrecognized arguments: " + argument)

    loop empty argv

let private resolveFullPath (basePath: string) (path: string) : string =
    if Path.IsPathFullyQualified path then
        Path.GetFullPath path
    else
        Path.GetFullPath(Path.Combine(basePath, path))

/// Run the verifier against the supplied writers. `startDirectory` seeds the
/// default repository-root lookup so tests can inject a fixture root.
let mainWith
    (stdout: TextWriter)
    (stderr: TextWriter)
    (startDirectory: string)
    (argv: string list)
    : int =
    match parseArgs argv with
    | Error message ->
        stderr.WriteLine usageLine
        stderr.WriteLine("Verify-Performance.ps1: error: " + message)
        2
    | Ok options when options.Help ->
        stdout.WriteLine helpText
        0
    | Ok options ->
        let repositoryRoot =
            match options.RepositoryRoot with
            | Some root -> Path.GetFullPath root
            | None ->
                match tryFindRootFrom startDirectory with
                | Some root -> root
                | None -> Path.GetFullPath startDirectory

        let receiptDirectory =
            match options.ReceiptDirectory with
            | Some value when not (String.IsNullOrWhiteSpace value) -> Some(resolveFullPath repositoryRoot value)
            | _ -> None

        match receiptDirectory with
        | None ->
            stderr.WriteLine usageLine
            stderr.WriteLine "Verify-Performance.ps1: error: the -ReceiptDirectory parameter is required."
            2
        | Some receiptPath ->
            let manifestPath =
                match options.ManifestPath with
                | Some value when not (String.IsNullOrWhiteSpace value) -> resolveFullPath repositoryRoot value
                | _ -> Path.Combine(repositoryRoot, "eng/performance/baseline.json")

            let proposalPath =
                match options.ObservationProposalPath with
                | Some value when not (String.IsNullOrWhiteSpace value) ->
                    Some(resolveFullPath repositoryRoot value)
                | _ -> None

            match run repositoryRoot manifestPath receiptPath proposalPath with
            | Ok line ->
                stdout.WriteLine line
                0
            | Error error ->
                stderr.WriteLine error.Message
                error.ExitCode

/// Entry point mirroring eng/Verify-Performance.ps1's parameter surface.
let main (argv: string array) : int =
    mainWith Console.Out Console.Error Environment.CurrentDirectory (List.ofArray argv)
