module FunnySharp.Harness.Performance

// Behaviour-identical F# port of eng/Verify-Performance.ps1: verifies benchmark
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
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Reflection
open System.Reflection.Metadata
open System.Reflection.PortableExecutable
open System.Xml.Linq
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

// ---- Hashing ----

/// Lowercase-hex SHA-256 of a file, mirroring Get-FileHash.
let fileSha256 (path: string) : string =
    use stream = File.OpenRead path
    Convert.ToHexString(SHA256.HashData stream).ToLowerInvariant()

/// Lowercase-hex SHA-256 of the UTF-8 bytes of a string, mirroring Get-TextSha256.
let textSha256 (value: string) : string =
    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes value)).ToLowerInvariant()

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

// ---- Fingerprints and the environment key ----

/// The ordinal-sorted `relpath/NUL/hexsha/LF` line list hash, mirroring
/// Get-FileSetFingerprint.
let fileSetFingerprint (root: string) (files: string list) : Result<string, HarnessError> =
    try
        let rootPath = Path.GetFullPath(root).TrimEnd([| '\\'; '/' |])

        let comparison =
            if OperatingSystem.IsWindows() then
                StringComparison.OrdinalIgnoreCase
            else
                StringComparison.Ordinal

        use stream = new MemoryStream()

        let relativePaths =
            files |> List.sortWith (fun a b -> StringComparer.Ordinal.Compare(a, b))

        for relativePath in relativePaths do
            if
                Path.IsPathFullyQualified relativePath
                || (relativePath.Split([| '\\'; '/' |]) |> Array.contains "..")
            then
                failNow (sprintf "Fingerprint path must be repository-relative: '%s'." relativePath)

            let path = Path.GetFullPath(Path.Combine(rootPath, relativePath))
            let prefix = rootPath + string Path.DirectorySeparatorChar

            if
                not (path.StartsWith(prefix, comparison))
                || not (File.Exists path)
            then
                failNow (sprintf "Fingerprint input was not found inside the repository: '%s'." relativePath)

            let line = relativePath.Replace('\\', '/') + "\000" + fileSha256 path + "\n"
            let bytes = Encoding.UTF8.GetBytes line
            stream.Write(bytes, 0, bytes.Length)

        Ok(Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant())
    with
    | PerformanceFailure error -> Error error
    | ex -> Error(Errors.create ex.Message)

/// SHA-256 of the 8 NUL-joined environment fields with booleans lowercased,
/// mirroring Get-EnvironmentKey.
let environmentKey (environment: JsonElement) : Result<string, HarnessError> =
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

            Ok(textSha256 (String.concat "\000" values))
        | _ -> failNow "Performance environment contains invalid GC data."
    with
    | PerformanceFailure error -> Error error
    | ex -> Error(Errors.create ex.Message)

// ---- The verifier ----

let private buildInputPath (path: string) : bool =
    let name =
        match Path.GetFileName path with
        | null -> failNow "A benchmark build input must have a filename."
        | value -> value
    path.EndsWith(".cs", StringComparison.Ordinal)
    || path.EndsWith(".csproj", StringComparison.Ordinal)
    || path.EndsWith(".props", StringComparison.Ordinal)
    || path.EndsWith(".targets", StringComparison.Ordinal)
    || name = "packages.lock.json"
    || name.StartsWith("AnalyzerReleases.", StringComparison.Ordinal)

let private projectInputPath (path: string) : bool =
    path.EndsWith(".csproj", StringComparison.Ordinal)
    || path.EndsWith(".props", StringComparison.Ordinal)
    || path.EndsWith(".targets", StringComparison.Ordinal)

let private containingDirectory (path: string) : string =
    match Path.GetDirectoryName path with
    | null -> failNow "A bound project must have a parent directory."
    | directory -> directory

let private requireInputClosure (root: string) (manifest: JsonElement) (files: string list) : unit =
    let binding = getPropertyOrNull manifest "runBinding"
    let project = scalarText (getPropertyOrNull binding "benchmarkProject")
    if String.IsNullOrWhiteSpace project then
        failNow "Fresh performance binding is missing its benchmark project."
    let listed = HashSet<string>(files, StringComparer.Ordinal)
    let required = HashSet<string>(StringComparer.Ordinal)
    let benchmarkDirectory = containingDirectory (Path.Combine(root, project))
    for directory in [ Path.Combine(root, "src"); benchmarkDirectory ] do
        for path in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories) do
            let relative = Path.GetRelativePath(root, path).Replace('\\', '/')
            if not (relative.Split('/') |> Array.exists (fun part -> part = "bin" || part = "obj"))
               && buildInputPath path then
                required.Add(relative) |> ignore
    for pattern in [ "*.props"; "*.targets" ] do
        for path in Directory.EnumerateFiles(root, pattern) do
            required.Add(Path.GetRelativePath(root, path).Replace('\\', '/')) |> ignore
    for relative in [ "Directory.Build.props"; "README.md"; "global.json" ] do
        required.Add(relative) |> ignore
    let pending = Queue<string>(required |> Seq.filter projectInputPath)
    let visited = HashSet<string>(StringComparer.Ordinal)
    while pending.Count > 0 do
        let relative = pending.Dequeue()
        if visited.Add relative then
            let path = Path.Combine(root, relative)
            let document = XDocument.Load path
            for element in document.Descendants() do
                if element.Name.LocalName = "Import" then
                    let value =
                        (match element.Attribute(XName.Get "Project") with
                         | null -> failNow "A benchmark import is missing its Project attribute."
                         | attribute -> attribute.Value).Replace(
                            "$(MSBuildThisFileDirectory)",
                            containingDirectory path + string Path.DirectorySeparatorChar,
                            StringComparison.Ordinal)
                    if value.Contains("$(", StringComparison.Ordinal)
                       || value.Contains('*') || value.Contains('?') then
                        failNow "An explicit benchmark build import has an unresolved path."
                    let imported = Path.GetFullPath(Path.Combine(containingDirectory path, value))
                    let importedRelative = Path.GetRelativePath(root, imported).Replace('\\', '/')
                    if importedRelative.Split('/') |> Array.contains ".."
                       || Path.IsPathFullyQualified importedRelative || not (File.Exists imported) then
                        failNow "An explicit benchmark build import is outside the repository or missing."
                    required.Add(importedRelative) |> ignore
                    pending.Enqueue importedRelative
    let missing = required |> Seq.filter (listed.Contains >> not) |> Seq.toList
    if not missing.IsEmpty then
        failNow ("Benchmark input closure omits: " + String.concat ", " missing)

let private snapshotIdentity
    (manifest: JsonElement) (policy: string) (input: string) (protocol: string) : string =
    let binding = getPropertyOrNull manifest "runBinding"
    let commit = scalarText (getPropertyOrNull binding "baseCommit")
    let tree = scalarText (getPropertyOrNull binding "baseTree")
    let validHash length (value: string) =
        value.Length = length && value |> Seq.forall Uri.IsHexDigit
    if not (validHash 40 commit && validHash 40 tree) then
        failNow "Fresh performance binding requires a base commit and tree."
    "snapshot:" + textSha256 (String.concat "\000" [ commit; tree; policy; input; protocol ])

/// A projection of one approved recording, not an allowlist of receipt-selected
/// historical hashes. Provenance is audited at migration; ordinary consumers
/// reconstruct the identity without opening the catalog or its archived objects.
type RecordingIdentity =
    { Protocol: string
      Snapshot: string
      Receipts: Map<string, string>
      IsCurrent: bool }

let recordingIdentity
    (root: string) (manifest: JsonElement) (inputFiles: string list) (protocolFiles: string list)
    (policy: string) (input: string) : Result<RecordingIdentity option, HarnessError> =
    try
        let protocol = getPropertyOrNull manifest "protocol"
        let metadata = getPropertyOrNull protocol "recordingIdentity"
        if metadata.ValueKind = JsonValueKind.Undefined then Ok None
        else
            let reject message = failNow ("Performance recording identity: " + message)
            let fields (element: JsonElement) (expected: string list) =
                if element.ValueKind <> JsonValueKind.Object then reject "expected an object."
                let names = element.EnumerateObject() |> Seq.map (fun item -> item.Name) |> Seq.toList
                if names.Length <> (Set.ofList names).Count
                   || (not expected.IsEmpty && Set.ofList names <> Set.ofList expected) then
                    reject "missing, duplicate or unknown metadata field."
            let text (element: JsonElement) name =
                let value = getPropertyOrNull element name
                if value.ValueKind <> JsonValueKind.String || String.IsNullOrWhiteSpace(scalarText value) then
                    reject ("missing text field '" + name + "'.")
                scalarText value
            let path (value: string) =
                if String.IsNullOrWhiteSpace value || Path.IsPathRooted value
                   || value |> Seq.exists (fun ch -> Char.IsControl ch || "\\:<>\"|?*".Contains ch)
                   || value.Split('/') |> Array.exists (fun part ->
                       part = "" || part = "." || part = ".." || part.EndsWith('.') || part.EndsWith(' ')) then
                    reject "paths must be canonical repository-relative paths."
                value
            let sha (element: JsonElement) name =
                let value = text element name
                if value.Length <> 64 || value |> Seq.exists (fun ch -> not ((ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f'))) then
                    reject "SHA-256 must be 64 lowercase hexadecimal digits."
                value
            let items (element: JsonElement) name =
                let value = getPropertyOrNull element name
                if value.ValueKind <> JsonValueKind.Array then reject ("missing array '" + name + "'.")
                arrayItems value
            let uniquePaths values =
                let seen = HashSet<string>(StringComparer.OrdinalIgnoreCase)
                for value in values do
                    path value |> ignore
                    if not (seen.Add value) then reject "duplicate path."
                Set.ofList values
            fields protocol []
            fields metadata [ "schemaVersion"; "receiverFiles"; "files"; "provenance" ]
            if tryJsonInteger (getPropertyOrNull metadata "schemaVersion") <> Some 1m then
                reject "unsupported schemaVersion."
            let declared = uniquePaths protocolFiles
            let inputs = uniquePaths inputFiles
            let receivers =
                items metadata "receiverFiles"
                |> List.map (fun value ->
                    if value.ValueKind <> JsonValueKind.String then reject "receiver path must be text."
                    scalarText value)
                |> uniquePaths
            if receivers.IsEmpty || not (Set.isSubset receivers declared)
               || not (Set.intersect receivers inputs).IsEmpty then
                reject "receiver partition must name only declared, non-input protocol files."
            let recorded =
                items metadata "files"
                |> List.map (fun binding ->
                    fields binding [ "path"; "sha256" ]
                    path (text binding "path"), sha binding "sha256")
            if uniquePaths (List.map fst recorded) <> declared then
                reject "recorded hashes must cover every protocol file exactly once."
            let provenance = getPropertyOrNull metadata "provenance"
            fields provenance [ "path"; "sha256" ]
            path (text provenance "path") |> ignore
            sha provenance "sha256" |> ignore
            let recordedProtocol =
                recorded
                |> List.sortWith (fun (a, _) (b, _) -> StringComparer.Ordinal.Compare(a, b))
                |> List.map (fun (relative, hash) -> relative + "\000" + hash + "\n")
                |> String.concat ""
                |> textSha256
            let observation = getPropertyOrNull manifest "observation"
            fields observation []
            if tryJsonInteger (getPropertyOrNull observation "schemaVersion") <> Some 1m
               || sha observation "protocolFingerprint" <> recordedProtocol then
                reject "recorded source hashes do not derive the approved protocol fingerprint."
            let approvedReceipts =
                items observation "receipts"
                |> List.map (fun receipt ->
                    fields receipt []
                    let name = path (text receipt "file")
                    if name.Contains('/') || not (name.EndsWith("-performance-receipt.json", StringComparison.Ordinal)) then
                        reject "approved receipt must have a receipt filename."
                    name, sha receipt "sha256")
            uniquePaths (List.map fst approvedReceipts) |> ignore
            if approvedReceipts.IsEmpty then reject "approved receipt set is empty."
            requireInputClosure root manifest inputFiles
            let snapshot = snapshotIdentity manifest policy input recordedProtocol
            let mutable current =
                text observation "policyRevision" = text (getPropertyOrNull manifest "policy") "revision"
                && sha observation "policyFingerprint" = policy
                && sha observation "benchmarkInputFingerprint" = input
                && text observation "candidateCommit" = snapshot
            for relative, recordedHash in recorded do
                let file = Path.Combine(root, relative)
                if not (File.Exists file) then reject ("missing protocol file '" + relative + "'.")
                if not (receivers.Contains relative) && fileSha256 file <> recordedHash then current <- false
            Ok(Some { Protocol = recordedProtocol; Snapshot = snapshot; Receipts = Map.ofList approvedReceipts; IsCurrent = current })
    with
    | PerformanceFailure error -> Error error
    | ex -> Error(Errors.create ex.Message)

let private evidencePath (directory: string) (evidence: JsonElement) : string =
    let name = scalarText (getPropertyOrNull evidence "file")
    if String.IsNullOrWhiteSpace name || name.Contains('/') || name.Contains('\\') then
        failNow "Run binding contains an invalid evidence path."
    let path = Path.Combine(directory, name)
    if not (File.Exists path)
       || fileSha256 path <> scalarText (getPropertyOrNull evidence "sha256") then
        failNow "Run binding evidence is missing or its hash does not match."
    path

let private verifyBinary (root: string) (binary: JsonElement) : string * string =
    let path = scalarText (getPropertyOrNull binary "file")
    let fullPath = Path.GetFullPath path
    let rootPrefix = Path.GetFullPath(root).TrimEnd('\\', '/') + string Path.DirectorySeparatorChar
    let comparison =
        if OperatingSystem.IsWindows() then StringComparison.OrdinalIgnoreCase
        else StringComparison.Ordinal
    if not (Path.IsPathFullyQualified path)
       || not (fullPath.StartsWith(rootPrefix, comparison))
       || not (File.Exists fullPath) then
        failNow "Bound workload binary is missing or outside the retained repository."
    let hash = fileSha256 fullPath
    if hash <> scalarText (getPropertyOrNull binary "sha256") then
        failNow "Bound workload binary hash does not match."
    use stream = File.OpenRead fullPath
    use pe = new PEReader(stream)
    if pe.HasMetadata then
        let reader = pe.GetMetadataReader()
        let mvid = reader.GetGuid(reader.GetModuleDefinition().Mvid).ToString()
        let identity = AssemblyName.GetAssemblyName(fullPath).FullName
        if mvid <> scalarText (getPropertyOrNull binary "mvid")
           || identity <> scalarText (getPropertyOrNull binary "identity") then
            failNow "Bound workload binary metadata does not match."
    elif not (String.IsNullOrEmpty(scalarText (getPropertyOrNull binary "mvid")))
         || not (String.IsNullOrEmpty(scalarText (getPropertyOrNull binary "identity"))) then
        failNow "A native binary cannot declare managed assembly metadata."
    let name =
        match Path.GetFileName fullPath with
        | null -> failNow "A bound binary must have a filename."
        | value -> value
    name, hash

let private verifyRunBinding
    (root: string) (directory: string) (manifest: JsonElement)
    (receipt: JsonElement) (snapshot: string)
    (policyFingerprintValue: string) (inputFingerprint: string) (protocolFingerprint: string)
    : Dictionary<string, string> =
    let binding = getPropertyOrNull receipt "binding"
    if binding.ValueKind <> JsonValueKind.Object then
        failNow "Fresh receipt is missing its preflight/workload binding."
    if scalarText (getPropertyOrNull receipt "candidateCommit") <> snapshot then
        failNow "Fresh receipt is missing or has the wrong candidate snapshot."
    let preflightPath = evidencePath directory (getPropertyOrNull binding "preflight")
    use preflightDocument = JsonDocument.Parse(File.ReadAllText preflightPath)
    let preflight = preflightDocument.RootElement
    if scalarText (getPropertyOrNull preflight "schemaVersion") <> "1"
       || scalarText (getPropertyOrNull preflight "candidateSnapshot") <> snapshot
       || scalarText (getPropertyOrNull preflight "policyFingerprint") <> policyFingerprintValue
       || scalarText (getPropertyOrNull preflight "benchmarkInputFingerprint") <> inputFingerprint
       || scalarText (getPropertyOrNull preflight "protocolFingerprint") <> protocolFingerprint then
        failNow "Preflight does not match the candidate, policy, source, or protocol."
    let declared = getPropertyOrNull manifest "runBinding"
    for name in [ "baseCommit"; "baseTree" ] do
        if scalarText (getPropertyOrNull preflight name) <> scalarText (getPropertyOrNull declared name) then
            failNow "Preflight has the wrong source-snapshot base."
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
        let outcome = scalarText (getPropertyOrNull row "outcomeSha256")
        if outcome.Length <> 64 || not (outcome |> Seq.forall Uri.IsHexDigit) then
            failNow "Preflight is missing a semantic outcome."
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
    let hostBinaries = Dictionary<string, string>(StringComparer.Ordinal)
    for binary in arrayItems (getPropertyOrNull preflight "assemblies") do
        let name, hash = verifyBinary root binary
        hostBinaries.Add(name, hash)
    if hostBinaries.Count = 0 then
        failNow "Preflight is missing its actual benchmark binaries."
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
           || scalarText (getPropertyOrNull child "candidateSnapshot") <> snapshot
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
        let workload = getPropertyOrNull child "workload"
        let _, workloadHash = verifyBinary root workload
        if hostBinaries.Values |> Seq.contains workloadHash then
            failNow "The measured workload binding names an exporter-host assembly."
        let childBinaries = Dictionary<string, string>(StringComparer.Ordinal)
        for binary in arrayItems (getPropertyOrNull child "assemblies") do
            let name, hash = verifyBinary root binary
            childBinaries.Add(name, hash)
        for KeyValue(name, hash) in hostBinaries do
            match childBinaries.TryGetValue name with
            | true, actual when actual = hash -> ()
            | _ -> failNow "Measured child binaries differ from the preflight binaries."
        launched.Add(rowId) |> ignore
    if not (launched.SetEquals receiptRows.Keys) then
        failNow "A measured row has no actual child workload binding."
    hostBinaries


// Exact member identity is assembly + declaring type + complete baseline line.
// The independent package census supplies Experimental classification; policy
// exclusions are never a stability oracle. This gate is required in a real
// checkout, while the original small legacy receipt fixtures remain synthetic.
let private verifyStableMemberCoverage (root: string) : Map<string, string> =
    let reject message = failNow ("Stable performance coverage: " + message)
    let relativePath (relative: string) =
        if String.IsNullOrWhiteSpace relative || Path.IsPathFullyQualified relative
           || (relative.Split([| '\\'; '/' |]) |> Array.contains "..") then
            reject "evidence path must be repository-relative."
        Path.Combine(root, relative)
    let boundPath (binding: JsonElement) =
        let path = relativePath (scalarText (getPropertyOrNull binding "path"))
        if not (File.Exists path) then
            reject "missing or stale hash-bound evidence."
        let hash = fileSha256 path
        if hash <> scalarText (getPropertyOrNull binding "sha256") then
            reject "missing or stale hash-bound evidence."
        path, hash
    // Only source excerpts share this operation-local snapshot. Hash and decode
    // the same bytes; StreamReader preserves ReadAllLines' BOM and line semantics.
    let sourceLines = Dictionary<string, string * string array>(StringComparer.Ordinal)
    let boundSourceLines (binding: JsonElement) =
        let path = relativePath (scalarText (getPropertyOrNull binding "path"))
        let hash, lines =
            match sourceLines.TryGetValue path with
            | true, value -> value
            | _ ->
                if not (File.Exists path) then reject "missing or stale hash-bound evidence."
                let bytes = File.ReadAllBytes path
                let hash = Convert.ToHexString(SHA256.HashData bytes).ToLowerInvariant()
                use stream = new MemoryStream(bytes)
                use reader = new StreamReader(stream, Encoding.UTF8, true)
                let lines = ResizeArray<string>()
                let mutable line = reader.ReadLine()
                while not (isNull line) do
                    match line with
                    | null -> ()
                    | value -> lines.Add value
                    line <- reader.ReadLine()
                let value = hash, lines.ToArray()
                sourceLines.Add(path, value)
                value
        if hash <> scalarText (getPropertyOrNull binding "sha256") then
            reject "missing or stale hash-bound evidence."
        lines
    let requiredText (element: JsonElement) name =
        let field = getPropertyOrNull element name
        let value = scalarText field
        if field.ValueKind <> JsonValueKind.String || String.IsNullOrWhiteSpace value then
            reject ("missing nonempty field '" + name + "'.")
        value
    let excerpt (element: JsonElement) (lines: string array) =
        let first = tryJsonInteger (getPropertyOrNull element "firstLine")
        let last = tryJsonInteger (getPropertyOrNull element "lastLine")
        match first, last with
        | Some a, Some b when a >= 1m && b >= a && b <= decimal lines.Length ->
            lines[int a - 1 .. int b - 1] |> String.concat "\n"
        | _ -> reject "source range is missing or outside its bound file."
    let path = Path.Combine(root, "eng", "performance", "coverage", "stable-members.json")
    let canonical = Directory.Exists(Path.Combine(root, "src", "FunnySharp"))
    let baselineDirectory = Path.Combine(root, "eng", "api-baseline")
    if not canonical && not (Directory.Exists baselineDirectory) && not (File.Exists path) then Map.empty
    else
        if not (File.Exists path) then reject "stable-members.json is required."
        use document = JsonDocument.Parse(File.ReadAllText path)
        let coverage = document.RootElement
        if scalarText (getPropertyOrNull coverage "schema") <> "funnysharp-stable-performance-coverage/v1" then
            reject "unsupported schema."
        let baselines = getPropertyOrNull coverage "baselines"
        let expected = Dictionary<string, string * string * string>(StringComparer.Ordinal)
        let baselineHashes = Dictionary<string, string>(StringComparer.Ordinal)
        for key, name in [ "C", "FunnySharp"; "H", "FunnySharp.AspNetCore" ] do
            let binding = getPropertyOrNull baselines key
            let relative = "eng/api-baseline/" + name + ".public-api.txt"
            if scalarText (getPropertyOrNull binding "path") <> relative then reject "wrong baseline path."
            let baseline, hash = boundPath binding
            let lines = File.ReadAllLines baseline
            if lines.Length = 0 || not (lines[0].StartsWith("ASSEMBLY " + name + ",", StringComparison.Ordinal)) then
                reject "wrong baseline assembly."
            baselineHashes.Add(name, hash)
            let mutable declaring = ""
            for index in 1 .. lines.Length - 1 do
                let line = lines[index]
                if line.StartsWith("  ", StringComparison.Ordinal) then
                    if String.IsNullOrWhiteSpace declaring then reject "member without declaring type."
                    expected.Add(key + ":" + string (index + 1), (lines[0], declaring, line))
                elif not (String.IsNullOrWhiteSpace line) then declaring <- line
        if expected.Count = 0 then reject "empty exported surface."
        let sourceFiles = Dictionary<string, string array>(StringComparer.Ordinal)
        for source in arrayItems (getPropertyOrNull coverage "sourceFiles") do
            let relative = requiredText source "path"
            if sourceFiles.ContainsKey relative then reject "duplicate source binding."
            sourceFiles.Add(relative, boundSourceLines source)
        // Nullability and parameter names are retained in the exact baseline
        // identity above. Only this metadata join removes those source spelling
        // differences; full overload/generic/ByRef shapes remain in the key.
        let normalizeType (value: string) =
            let value = System.Text.RegularExpressions.Regex.Replace(value.Replace("?", ""), @"[\x60][0-9]+", "")
            let value = value.Replace('<', '[').Replace('>', ']')
            let value = System.Text.RegularExpressions.Regex.Replace(value, @"(?<!\.)\b(Void|Int32|Int64|Boolean|String|Object|Double|Char|Single|Byte|IntPtr)\b", "System.$1")
            let value = System.Text.RegularExpressions.Regex.Replace(value, @"\s+ByRef", "&")
            System.Text.RegularExpressions.Regex.Replace(value, @"\s", "")
        let declaringKey (value: string) =
            let value = System.Text.RegularExpressions.Regex.Replace(value, @"^(CLASS|STRUCT|INTERFACE|ENUM|DELEGATE) ", "")
            normalizeType (System.Text.RegularExpressions.Regex.Replace(value, @"<.*>", ""))
        let memberKey canonical (value: string) =
            let value = System.Text.RegularExpressions.Regex.Replace(value.Trim(), @"^(METHOD|CONSTRUCTOR|PROPERTIE|FIELD|EVENT) ", "")
            let value = System.Text.RegularExpressions.Regex.Replace(value, @"^static | = .*$", "")
            let value = if canonical && value.StartsWith("ctor(", StringComparison.Ordinal) then "System.Void .ctor" + value.Substring(4) else value
            let opening = value.IndexOf('(')
            if opening < 0 then normalizeType value
            else
                let parameters = value.Substring(opening + 1, value.Length - opening - 2)
                let parts = ResizeArray<string>()
                let mutable depth = 0
                let mutable start = 0
                for index in 0 .. parameters.Length - 1 do
                    match parameters[index] with
                    | '<' | '[' -> depth <- depth + 1
                    | '>' | ']' -> depth <- depth - 1
                    | ',' when depth = 0 ->
                        parts.Add(parameters.Substring(start, index - start))
                        start <- index + 1
                    | _ -> ()
                if start < parameters.Length then parts.Add(parameters.Substring start)
                let parameters =
                    parts |> Seq.map (fun part ->
                        let part = if canonical then System.Text.RegularExpressions.Regex.Replace(part.Trim(), @" \w+$", "") else part
                        let part = System.Text.RegularExpressions.Regex.Replace(part, @"^(out|ref|in) ", "")
                        normalizeType part) |> String.concat ","
                normalizeType (value.Substring(0, opening)) + "(" + parameters + ")"
        let metadata = Dictionary<string, JsonElement>(StringComparer.Ordinal)
        let packageBinaries = Dictionary<string, string>(StringComparer.Ordinal)
        use censusDocument = JsonDocument.Parse(File.ReadAllText(fst (boundPath (getPropertyOrNull coverage "metadataCensus"))))
        for assembly in arrayItems (getPropertyOrNull censusDocument.RootElement "assemblies") do
            let name = requiredText assembly "assembly"
            if not (baselineHashes.ContainsKey name) || baselineHashes[name] <> scalarText (getPropertyOrNull assembly "baselineSha256") then
                reject "package census has a stale or unknown baseline."
            if canonical then
                let file = requiredText assembly "file"
                let binary = relativePath (Path.GetRelativePath(root, file))
                if not (File.Exists binary) then reject "package census DLL bytes are stale."
                let hash = fileSha256 binary
                if hash <> requiredText assembly "sha256" then
                    reject "package census DLL bytes are stale."
                use stream = File.OpenRead binary
                use pe = new PEReader(stream)
                let reader = pe.GetMetadataReader()
                if reader.GetGuid(reader.GetModuleDefinition().Mvid).ToString() <> requiredText assembly "mvid"
                   || AssemblyName.GetAssemblyName(binary).Name <> name then
                    reject "package census DLL metadata is stale."
                packageBinaries.Add(name + ".dll", hash)
            for item in arrayItems (getPropertyOrNull assembly "metadata") do
                let key = name + "|" + declaringKey (requiredText item "declaringType") + "|" + memberKey false (requiredText item "member")
                if metadata.ContainsKey key then reject "duplicate package census member identity."
                metadata.Add(key, item)
        if metadata.Count <> expected.Count || (canonical && packageBinaries.Count <> 2) then
            reject "package census does not cover the exact exported surface."
        let benchmarkRows = HashSet<string>(StringComparer.Ordinal)
        let manifestPaths = HashSet<string>(StringComparer.Ordinal)
        for binding in arrayItems (getPropertyOrNull coverage "benchmarkManifests") do
            let relative = requiredText binding "path"
            if not (manifestPaths.Add relative) then reject "duplicate benchmark manifest."
            let manifestPath = relativePath relative
            if not (File.Exists manifestPath) then reject "stale benchmark policy binding."
            use manifestDocument = JsonDocument.Parse(File.ReadAllText manifestPath)
            let policy = getPropertyOrNull manifestDocument.RootElement "policy"
            if policy.ValueKind = JsonValueKind.Undefined then
                failNow "Performance manifest is missing its policy section."
            if textSha256 (policy.GetRawText()) <> requiredText binding "policySha256" then
                reject "stale benchmark policy binding."
            for row in arrayItems (getPropertyOrNull policy "rows") do
                if tryJsonBool (getPropertyOrNull row "included") = Some true then
                    benchmarkRows.Add(requiredText row "id") |> ignore
        if canonical && not (manifestPaths.SetEquals [ "eng/performance/baseline.json"; "eng/performance/competitor-baseline.json" ]) then
            reject "both actual benchmark policies must be bound."
        let seen = HashSet<string>(StringComparer.Ordinal)
        let seenMetadata = HashSet<string>(StringComparer.Ordinal)
        let mutable experimentalCount = 0
        for row in arrayItems (getPropertyOrNull coverage "members") do
            let identity = requiredText row "identity"
            if not (seen.Add identity) || not (expected.ContainsKey identity) then reject "missing, duplicate or unknown member identity."
            let assembly, declaring, memberLine = expected[identity]
            if requiredText row "assembly" <> assembly || requiredText row "declaringType" <> declaring
               || requiredText row "member" <> memberLine then reject ("stale exact member identity: " + identity)
            let name = assembly.Substring("ASSEMBLY ".Length).Split(',')[0]
            let key = name + "|" + declaringKey declaring + "|" + memberKey true memberLine
            if not (metadata.ContainsKey key) || not (seenMetadata.Add key) then reject ("member has no unique package metadata identity: " + identity)
            let item = metadata[key]
            let projected = getPropertyOrNull row "metadata"
            for field in [ "declaringType"; "memberKind"; "member"; "metadataToken"; "experimentalDiagnostic" ] do
                if not (scalarEquals (getPropertyOrNull projected field) (getPropertyOrNull item field)) then
                    reject ("stale package member metadata: " + identity)
            let experimental = not (String.IsNullOrWhiteSpace(scalarText (getPropertyOrNull item "experimentalDiagnostic")))
            if requiredText row "stability" <> (if experimental then "experimental" else "stable") then
                reject ("Experimental misclassification: " + identity)
            let source = getPropertyOrNull row "source"
            let relative = requiredText source "path"
            if not (sourceFiles.ContainsKey relative) then reject ("member source has no bound file: " + identity)
            excerpt source sourceFiles[relative] |> ignore
            if not experimental then
                for field in [ "allocation"; "cost"; "boxing"; "resources" ] do requiredText row field |> ignore
            else experimentalCount <- experimentalCount + 1
            if canonical && (memberLine.Contains(" BeginInvoke(", StringComparison.Ordinal)
                             || memberLine.Contains(" EndInvoke(", StringComparison.Ordinal)) then
                let witness = getPropertyOrNull row "runtimeWitness"
                boundPath (getPropertyOrNull witness "source") |> ignore
                let _, hash = boundPath (getPropertyOrNull witness "coreDll")
                if hash <> packageBinaries["FunnySharp.dll"] then reject "APM witness used different package bytes."
                use proof = JsonDocument.Parse(File.ReadAllText(fst (boundPath witness)))
                let results = getPropertyOrNull proof.RootElement "results"
                let cases = arrayItems (getPropertyOrNull results "tests")
                let memberName = requiredText witness "member"
                if cases.IsEmpty || cases |> List.exists (fun test -> scalarText (getPropertyOrNull test "status") <> "passed")
                   || not (cases |> List.exists (fun test ->
                       scalarText (getPropertyOrNull (getPropertyOrNull test "extra") "method") = memberName)) then
                    reject "generated delegate APM runtime witness did not pass."
            let rows = getPropertyOrNull row "benchmarkRows"
            if rows.ValueKind <> JsonValueKind.Array then reject ("missing benchmark row array: " + identity)
            let ids = arrayItems rows |> List.map scalarText
            if ids.Length <> (Set.ofList ids).Count || ids |> List.exists (fun id -> not (benchmarkRows.Contains id)) then
                reject ("duplicate or unknown benchmark row: " + identity)
            if experimental && not ids.IsEmpty then reject "Experimental member cannot claim stable benchmark coverage."
            if ids.IsEmpty then requiredText row "exclusionReason" |> ignore
        if not (seen.SetEquals expected.Keys) || not (seenMetadata.SetEquals metadata.Keys) then reject "missing exported member."
        if canonical && (expected.Count <> 499 || experimentalCount <> 31) then reject "canonical 499/468/31 member census drifted."
        let witnessKinds = HashSet<string>(StringComparer.Ordinal)
        let witnessMembers = HashSet<string>(StringComparer.Ordinal)
        for witness in arrayItems (getPropertyOrNull coverage "mandatoryWitnesses") do
            let kind = requiredText witness "kind"
            witnessKinds.Add kind |> ignore
            let memberName = requiredText witness "member"
            witnessMembers.Add(kind + "|" + memberName) |> ignore
            if not ((excerpt witness (boundSourceLines witness)).Contains(memberName, StringComparison.Ordinal)) then
                reject "mandatory witness does not resolve to its bound source."
            if canonical then
                let execution = getPropertyOrNull witness "execution"
                let report = XDocument.Load(fst (boundPath execution))
                let results = report.Descendants() |> Seq.filter (fun node -> node.Name.LocalName = "UnitTestResult") |> Seq.toArray
                if results.Length = 0 || results |> Array.exists (fun node ->
                    match node.Attribute(XName.Get "outcome") with
                    | null -> true
                    | value -> value.Value <> "Passed") then reject "mandatory resource test execution is not all passing."
                if not (results |> Array.exists (fun node ->
                    match node.Attribute(XName.Get "testName") with
                    | null -> false
                    | value -> value.Value.Contains(memberName, StringComparison.Ordinal))) then
                    reject "mandatory resource test was not executed."
                let _, hash = boundPath (getPropertyOrNull execution "coreDll")
                if hash <> packageBinaries["FunnySharp.dll"] then reject "resource witness used different core package bytes."
        if not (witnessKinds.SetEquals [ "long-input-bound"; "cancel-drain"; "linear-first-success" ]) then
            reject "long-input bound, cancel/drain and linear FirstSuccess witnesses are mandatory."
        if canonical then
            for witness in
                [ "long-input-bound|ExplicitFirstSuccessBoundLimits1024HeldCandidatesAndRefillsExactlyOneSlot"
                  "long-input-bound|SelectParallelCompletionOrderValueAsyncBoundsUndeliveredWorkAndKeepsStreaming"
                  "cancel-drain|FirstSuccessAwaitsRealUsingAsyncDisposalAndPropagatesItsFailureAfterAWinner"
                  "cancel-drain|ExplicitFirstSuccessBoundStopsAdmissionAndDrainsOnTimeoutOrCallerCancellation"
                  "cancel-drain|ExternalCancellationDoesNotPublishANonCooperatingSelectorAndCleansUpOnce"
                  "linear-first-success|ExplicitFirstSuccessHandlesLargeSynchronousFailuresInOrderWithOneValueTaskConsumption"
                  "linear-first-success|ExplicitFirstSuccessAccountsForLargeStaggeredBatchesWithoutReorderingTypedFailures" ] do
                if not (witnessMembers.Contains witness) then reject ("missing mandatory resource witness: " + witness)
        packageBinaries |> Seq.map (fun pair -> pair.Key, pair.Value) |> Map.ofSeq


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
      Sha256: string
      EnvironmentElement: JsonElement }

/// Serialise the observation-only proposal with the ordered key set from A1.
let private buildProposal
    (generatedAtUtc: string)
    (candidateCommit: string option)
    (policyRevision: string)
    (policyFingerprintValue: string)
    (inputFingerprint: string)
    (protocolFingerprint: string)
    (environmentKeyValue: string)
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
    writer.WriteString("policyFingerprint", policyFingerprintValue)
    writer.WriteString("benchmarkInputFingerprint", inputFingerprint)
    writer.WriteString("protocolFingerprint", protocolFingerprint)
    writer.WriteString("environmentKey", environmentKeyValue)
    writer.WriteStartArray "receipts"

    for receipt in receipts do
        writer.WriteStartObject()
        writer.WriteString("file", receipt.File)
        writer.WriteString("sha256", receipt.Sha256)
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

        let coverageBinaries = verifyStableMemberCoverage repositoryRoot

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

        let policyFingerprintValue = textSha256 (policyElement.GetRawText())

        let inputFiles =
            arrayItems (getPropertyOrNull (getPropertyOrNull manifest "benchmarkInput") "files")
            |> List.map scalarText

        let protocolFiles =
            arrayItems (getPropertyOrNull (getPropertyOrNull manifest "protocol") "files")
            |> List.map scalarText

        let inputFingerprint = unwrap (fileSetFingerprint repositoryRoot inputFiles)
        let recording = unwrap (recordingIdentity repositoryRoot manifest inputFiles protocolFiles policyFingerprintValue inputFingerprint)
        let currentProtocol =
            if recording.IsNone then Some(unwrap (fileSetFingerprint repositoryRoot protocolFiles))
            else None
        // Existing synthetic legacy fixtures remain readable. The real checkout,
        // and any manifest opting into runBinding, require fresh bound evidence.
        let fresh =
            Directory.Exists(Path.Combine(repositoryRoot, "src"))
            || (getPropertyOrNull manifest "runBinding").ValueKind = JsonValueKind.Object
        let currentSnapshot =
            if fresh && recording.IsNone then
                requireInputClosure repositoryRoot manifest inputFiles
                Some(snapshotIdentity manifest policyFingerprintValue inputFingerprint (Option.get currentProtocol))
            else None

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

        // One admission decision, computed once: an approved historical recording
        // (its receipts verified against the recorded set), a fresh current-protocol
        // run, or a legacy manifest. The receipts sequence and the protocol
        // fingerprint/snapshot pair both derive from this single decision instead
        // of re-testing recording.IsSome at each use site.
        let admission =
            let recordedReceipts =
                (if recording.IsSome then receiptFiles else [||]) |> Array.map (fun file ->
                    // Hash and parse the same acquisition, preserving ReadAllText's
                    // BOM-aware decoding. The whole set chooses one identity; never
                    // mix snapshots.
                    let bytes = File.ReadAllBytes file
                    let hash = Convert.ToHexString(SHA256.HashData bytes).ToLowerInvariant()
                    use stream = new MemoryStream(bytes)
                    use reader = new StreamReader(stream, Encoding.UTF8, true)
                    use document = JsonDocument.Parse(reader.ReadToEnd())
                    file, document.RootElement.Clone(), Some hash)
            match recording with
            | Some identity
                when identity.Receipts.Count = recordedReceipts.Length
                     && recordedReceipts |> Array.forall (fun (file, _, hash) ->
                         Map.tryFind (fileNameOf file) identity.Receipts = hash) ->
                if not identity.IsCurrent then
                    failNow "The approved recording does not match the current performance policy, input, protocol, or snapshot."
                {| Receipts = recordedReceipts :> seq<_>
                   ProtocolFingerprint = identity.Protocol
                   Snapshot = Some identity.Snapshot |}
            | _ ->
                // Legacy manifests keep their streaming validation and failure order;
                // recording-admission parses the whole approved set up front so the
                // recording identity can be compared atomically.
                let protocol, snapshot =
                    match currentProtocol with
                    | Some current -> current, currentSnapshot
                    | None ->
                        let legacy = unwrap (fileSetFingerprint repositoryRoot protocolFiles)
                        legacy, (if fresh then Some(snapshotIdentity manifest policyFingerprintValue inputFingerprint legacy) else None)
                {| Receipts = receiptFiles |> Seq.map (fun file ->
                       use document = JsonDocument.Parse(File.ReadAllText file)
                       file, document.RootElement.Clone(), None)
                   ProtocolFingerprint = protocol
                   Snapshot = snapshot |}
        let receipts = admission.Receipts
        let protocolFingerprint = admission.ProtocolFingerprint
        let snapshot = admission.Snapshot

        let observedById = Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
        let receiptSummaries = ResizeArray<ReceiptSummary>()
        let candidateCommitOrder = ResizeArray<string>()
        let candidateCommitSet = HashSet<string>(StringComparer.Ordinal)
        let environmentKeys = HashSet<string>(StringComparer.Ordinal)

        for receiptFile, receipt, receiptHash in receipts do
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
            match snapshot with
            | Some expected ->
                let binaries =
                    verifyRunBinding repositoryRoot receiptDirectory manifest receipt expected
                        policyFingerprintValue inputFingerprint protocolFingerprint
                if not coverageBinaries.IsEmpty then
                    for KeyValue(name, hash) in coverageBinaries do
                        match binaries.TryGetValue name with
                        | false, _ when name = "FunnySharp.dll" ->
                            failNow "Stable performance coverage: actual workload has no core census binary."
                        | true, actual when actual <> hash ->
                            failNow "Stable performance coverage: actual workload differs from packaged metadata census."
                        | _ -> ()
            | None ->
                if (getPropertyOrNull receipt "binding").ValueKind <> JsonValueKind.Undefined then
                    failNow "A fresh receipt cannot be verified using an unbound legacy manifest."

            if
                not (psTextEquals (scalarText (getPropertyOrNull receipt "policyRevision")) policyRevision)
                || not (psTextEquals (scalarText (getPropertyOrNull receipt "policyFingerprint")) policyFingerprintValue)
            then
                failNow (sprintf "Performance receipt '%s' was not measured under the current policy." receiptName)

            if
                not (psTextEquals (scalarText (getPropertyOrNull receipt "benchmarkInputFingerprint")) inputFingerprint)
                || not (psTextEquals (scalarText (getPropertyOrNull receipt "protocolFingerprint")) protocolFingerprint)
            then
                failNow (
                    sprintf
                        "Performance receipt '%s' does not match the current benchmark input or verifier protocol."
                        receiptName
                )

            let environmentElement = getPropertyOrNull receipt "environment"
            let environmentKeyValue = unwrap (environmentKey environmentElement)

            if not (psTextEquals (scalarText (getPropertyOrNull receipt "environmentKey")) environmentKeyValue) then
                failNow (sprintf "Performance receipt '%s' has an invalid environment key." receiptName)

            environmentKeys.Add environmentKeyValue |> ignore

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
                    || not (psTextEquals (fileSha256 reportPath) (scalarText (getPropertyOrNull report "sha256")))
                then
                    failNow (
                        sprintf "Performance report '%s' is missing or its hash does not match the receipt." reportName
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
                      Sha256 = receiptHash |> Option.defaultWith (fun () -> fileSha256 receiptFile)
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
                    policyFingerprintValue
                    inputFingerprint
                    protocolFingerprint
                    (environmentKeys |> Seq.head)
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
