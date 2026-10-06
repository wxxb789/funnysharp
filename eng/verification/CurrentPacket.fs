module CurrentPacket

open System
open System.IO
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open System.Collections.Generic
open System.Security.Cryptography
open System.Reflection.Metadata
open System.Reflection.PortableExecutable
open System.Text.RegularExpressions
open System.Xml.Linq

type PacketKind = Current | TraversalR9
let field (node: JsonElement) (name: string) = node.GetProperty name
let text node name = (field node name).GetString()
let items node name = (field node name).EnumerateArray() |> Seq.toArray
let require condition message = if not condition then failwith message

// Each instance owns one explicit current-input snapshot and immutable decoded objects.
// No result survives this invocation. Every consumer compares its expected digest.
type Packet(root: string, kind: PacketKind) =
    let directory, pin =
        match kind with
        | Current -> "docs/reviews/goal24-pr-20261004/current-performance-evidence", "093523fa54b126b9f65dd34718371f7f2d11d4afc729cc85cf4b3e4f6d98d187"
        | TraversalR9 -> "docs/reviews/goal24-pr-20261004/traversal-r9-performance-evidence", "6508608e4b1d0ae8ce41d176cb45afbbb240c0004c730a69ab1867cd3a5d1b80"
    let mutable hashCount = 0
    let mutable hashBytes = 0L
    let hash (bytes: byte array) =
        hashCount <- hashCount + 1
        hashBytes <- hashBytes + bytes.LongLength
        bytes |> SHA256.HashData |> Convert.ToHexStringLower
    let catalogBytes = File.ReadAllBytes(Path.Combine(root, directory, "catalog.json"))
    do require (hash catalogBytes = pin) "Catalog hash mismatch"
    let catalogDocument = JsonDocument.Parse catalogBytes
    let catalog = catalogDocument.RootElement
    do require (text catalog "schema" = "funnysharp-portable-current-performance/v1") "Unsupported catalog"
    let original = (text catalog "originalWorkspace").Replace('\\', '/').TrimEnd('/')
    let locatorRoots = if kind = TraversalR9 then items catalog "additionalLocatorRoots" else [||]
    let relative (value: string) =
        let value = value.Replace('\\', '/')
        let value =
            if value.StartsWith(original + "/", StringComparison.OrdinalIgnoreCase) then value.Substring(original.Length + 1)
            else
                locatorRoots |> Array.tryPick (fun node ->
                    let prefix = (text node "original").TrimEnd('/') + "/"
                    if value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) then Some(text node "logical" + "/" + value.Substring(prefix.Length)) else None)
                |> Option.defaultValue value
        require (not (Path.IsPathFullyQualified value) && not (value.Split('/') |> Array.contains "..")) "Locator escapes catalog"
        value
    let objects = Dictionary<string, byte array * string>(StringComparer.Ordinal)
    do
        for item in items catalog "objects" do
            let key = text item "sha256"
            require (not (objects.ContainsKey key)) "Duplicate object hash"
            let encoded = File.ReadAllBytes(Path.Combine(root, relative (text item "physicalPath")))
            require (hash encoded = text item "physicalSha256") "Physical object hash mismatch"
            let bytes = Convert.FromBase64String(Encoding.UTF8.GetString encoded)
            let actual = hash bytes
            require (int64 bytes.Length = (field item "bytes").GetInt64() && actual = key) "Decoded object hash mismatch"
            objects.Add(key, (bytes, actual))
    let files = Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
    do
        for item in items catalog "files" do
            let path = relative (text item "path")
            require (not (files.ContainsKey path)) "Duplicate logical locator"
            let bytes, _ = objects[text item "sha256"]
            require (int64 bytes.Length = (field item "bytes").GetInt64()) "Logical byte length mismatch"
            files.Add(path, item)
    let objectAt path = objects[text files[relative path] "sha256"]
    let sourceHashes = Dictionary<string, string>(StringComparer.Ordinal)
    let identities = Dictionary<string, string * string>(StringComparer.Ordinal)
    let mutable currentDocuments = 0
    let mutable inputInventories = 0
    member _.Kind = kind
    member _.Catalog = catalog
    member _.CatalogIdentity = {| path = directory + "/catalog.json"; sha256 = pin |}
    member _.Relative path = relative path
    member _.CurrentFileExists path = File.Exists(Path.Combine(root, relative path))
    member _.Hash bytes = hash bytes
    member _.Digest path = objectAt path |> snd
    member internal _.Load path = objectAt path |> fst
    member _.Json path = JsonDocument.Parse(objectAt path |> fst)
    member _.CurrentJson path =
        currentDocuments <- currentDocuments + 1
        JsonDocument.Parse(File.ReadAllText(Path.Combine(root, relative path)))
    member _.CurrentInputPaths project =
        inputInventories <- inputInventories + 1
        // The exact recorded project/import bytes are checked separately. Discover
        // newly introduced build inputs as well, rather than trusting a stale list.
        [| for directory in [ Path.Combine(root, "src"); Path.GetDirectoryName(Path.Combine(root, project)) ] do
               for path in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories) do
                   let path = Path.GetRelativePath(root, path).Replace('\\', '/')
                   let name = Path.GetFileName path
                   if not (path.Split('/') |> Array.exists (fun part -> part = "bin" || part = "obj"))
                      && ([ ".cs"; ".csproj"; ".props"; ".targets" ] |> List.exists (fun extension -> path.EndsWith(extension, StringComparison.Ordinal))
                          || name = "packages.lock.json" || name.StartsWith("AnalyzerReleases.", StringComparison.Ordinal)) then yield path
           for pattern in [ "*.props"; "*.targets" ] do
               for path in Directory.EnumerateFiles(root, pattern) do yield Path.GetRelativePath(root, path).Replace('\\', '/')
           yield! [| "Directory.Build.props"; "README.md"; "global.json" |] |]
    member _.SourceHash path =
        let resolved = Path.GetFullPath(Path.Combine(root, relative path))
        match sourceHashes.TryGetValue resolved with
        | true, digest -> digest
        | _ ->
            let digest = File.ReadAllBytes resolved |> hash
            sourceHashes.Add(resolved, digest)
            digest
    member _.PeIdentity path =
        let bytes, actual = objectAt path
        match identities.TryGetValue actual with
        | true, identity -> identity
        | _ ->
            use stream = new MemoryStream(bytes, false)
            use pe = new PEReader(stream)
            let reader = pe.GetMetadataReader()
            let identity = reader.GetString(reader.GetAssemblyDefinition().Name), reader.GetGuid(reader.GetModuleDefinition().Mvid).ToString()
            identities.Add(actual, identity)
            identity
    member _.Stats = {| objects = objects.Count; locators = files.Count; hashCalls = hashCount; hashBytes = hashBytes; sourceReads = sourceHashes.Count; currentDocuments = currentDocuments; inputInventories = inputInventories; peParses = identities.Count; decodedBytes = objects.Values |> Seq.sumBy (fun (bytes, _) -> bytes.LongLength) |}
    interface IDisposable with
        member _.Dispose() = catalogDocument.Dispose()

let checkBinding (packet: Packet) node = require (packet.Digest(text node "path") = text node "sha256") "Evidence binding mismatch"
let checkSource (packet: Packet) node = require (packet.SourceHash(text node "path") = text node "sha256") "Current source/protocol bytes changed"

let fingerprint (packet: Packet) digest (paths: string array) =
    paths
    |> Array.sortWith (fun a b -> StringComparer.Ordinal.Compare(a, b))
    |> Array.map (fun path -> path.Replace('\\', '/') + "\u0000" + digest path + "\n")
    |> String.concat "" |> Encoding.UTF8.GetBytes |> packet.Hash

let validateInputClosure manifest (required: string array) =
    let listed = items (field manifest "benchmarkInput") "files" |> Array.map (fun node -> node.GetString()) |> Set.ofArray
    require (required |> Array.forall listed.Contains) "Current benchmark input closure changed"

// This is admission of the single fixed R9 recording, not a generic old-hash
// fallback. All non-metadata manifest content must still be the recorded content.
// The projection is built from actual trusted source objects, never receipts.
let validateRecordingManifest (packet: Packet) suite (manifest: JsonElement) =
    use recordedDocument = packet.Json(text suite "manifestPath")
    let recorded = recordedDocument.RootElement
    let comparable = JsonNode.Parse(manifest.GetRawText())
    let protocol = comparable["protocol"].AsObject()
    let mutable receivers = Set.empty
    match (field manifest "protocol").TryGetProperty "recordingIdentity" with
    | true, metadata ->
        require (packet.Kind = TraversalR9) "Recording identity requires the fixed R9 packet"
        let receiverFiles =
            match text suite "name" with
            | "main" -> [| "eng/harness/Performance.fs"; "eng/harness/PerformanceDocs.fs"; "tests/FunnySharp.Harness.Tests/PerformanceProtocolTests.fs" |]
            | "competitor" -> [| "eng/harness/Performance.fs"; "eng/harness/PerformanceDocs.fs" |]
            | _ -> failwith "Unknown R9 suite"
        let paths = items (field recorded "protocol") "files" |> Array.map (fun node -> node.GetString())
        let expected =
            JsonSerializer.SerializeToNode
                {| schemaVersion = 1; receiverFiles = receiverFiles
                   files = paths |> Array.map (fun path -> {| path = path; sha256 = packet.Digest path |})
                   provenance = packet.CatalogIdentity |}
        require (JsonNode.DeepEquals(JsonNode.Parse(metadata.GetRawText()), expected)) "Recording projection differs from fixed source evidence"
        require (receiverFiles |> Array.forall packet.CurrentFileExists) "Current receiver file is missing"
        receivers <- Set.ofArray receiverFiles
        protocol.Remove "recordingIdentity" |> ignore
    | _ -> ()
    require (JsonNode.DeepEquals(comparable, JsonNode.Parse(recorded.GetRawText()))) "Current manifest differs from immutable recording"
    // Raw policy formatting is fingerprint-bearing even when JSON values match.
    require ((field manifest "policy").GetRawText() = (field recorded "policy").GetRawText()) "Current raw policy differs from immutable recording"
    receivers

let validateSourceBindings (packet: Packet) (receivers: Set<string>) bindings =
    for binding in bindings do
        if receivers.Contains(packet.Relative(text binding "path")) then checkBinding packet binding
        else checkSource packet binding

let validateApprovedReceipts (packet: Packet) suite observation =
    let actual =
        items suite "receiptPaths"
        |> Array.map (fun path -> let path = path.GetString() in Path.GetFileName path, packet.Digest path)
        |> Map.ofArray
    let approved = items observation "receipts" |> Array.map (fun node -> text node "file", text node "sha256")
    require (approved.Length = actual.Count && Map.ofArray approved = actual) "Approved receipt file/hash set mismatch"

let validateSnapshot (packet: Packet) suite manifest policy input protocol =
    let runBinding = field manifest "runBinding"
    let snapshot = "snapshot:" + (String.concat "\u0000" [text runBinding "baseCommit"; text runBinding "baseTree"; policy; input; protocol] |> Encoding.UTF8.GetBytes |> packet.Hash)
    require (snapshot = text suite "expectedSnapshot" && text (field manifest "observation") "candidateCommit" = snapshot) "Source snapshot identity mismatch"

let validateCurrentCoverage (packet: Packet) =
    let path = "eng/performance/coverage/stable-members.json"
    require (packet.SourceHash path = packet.Digest path) "Current coverage differs from immutable recording"
    use document = packet.Json path
    for binding in items document.RootElement "sourceFiles" do checkSource packet binding
    checkBinding packet (field document.RootElement "metadataCensus")

let indexLaunches receipt =
    let byId = Dictionary<string, JsonElement>(StringComparer.Ordinal)
    for launch in items (field receipt "binding") "launches" do
        require (byId.TryAdd(text launch "rowId", launch)) "Expected one actual launch per row"
    let rowIds = items receipt "rows" |> Array.map (fun row -> text row "id") |> Set.ofArray
    require (byId.Count = rowIds.Count && byId.Keys |> Seq.forall rowIds.Contains) "Expected one actual launch per row"
    byId

let validateRow row expected =
    for name in [ "benchmarkClass"; "category"; "method"; "parameters"; "baseline" ] do
        require (JsonNode.DeepEquals(JsonNode.Parse((field row name).GetRawText()), JsonNode.Parse((field expected name).GetRawText()))) "Row policy identity mismatch"
    let allocation = (field row "allocatedBytesPerOperation").GetInt64()
    require (allocation >= 0L && allocation <= (field expected "allocationBudgetBytes").GetInt64()) "Allocation budget regression"

let validateWorkload (packet: Packet) snapshot runId (binaries: Dictionary<string, string>) workload =
    require (text workload "runId" = runId && text workload "candidateSnapshot" = snapshot) "Launch/preflight join mismatch"
    let binary = field workload "workload"
    if packet.Kind = TraversalR9 then
        let workloadName, workloadMvid = packet.PeIdentity(text binary "file")
        require (packet.Digest(text binary "file") = text binary "sha256" && workloadMvid = text binary "mvid" && (text binary "identity").StartsWith(workloadName + ",", StringComparison.Ordinal)) "Workload bytes/MVID mismatch"
    else require (packet.Digest(text binary "file") = text binary "sha256") "Workload bytes mismatch"
    for assembly in items workload "assemblies" do
        let path = text assembly "file"
        let name, mvid = packet.PeIdentity path
        require (packet.Digest path = text assembly "sha256" && mvid = text assembly "mvid") "Loaded assembly bytes/MVID mismatch"
        if binaries.ContainsKey name then require (packet.Digest path = binaries[name]) "Launch differs from current census"

let validateWitness (packet: Packet) (binaries: Dictionary<string, string>) witness =
    checkBinding packet witness
    if packet.Kind = TraversalR9 then checkSource packet witness
    let execution = field witness "execution"
    checkBinding packet execution
    checkBinding packet (field execution "coreDll")
    require (text (field execution "coreDll") "sha256" = binaries["FunnySharp"]) "Witness used other core bytes"
    if text witness "kind" = "delegate-apm" then
        use reportDocument = packet.Json(text execution "path")
        let tests = items (field reportDocument.RootElement "results") "tests"
        require (tests.Length = 24 && tests |> Array.forall (fun item -> text item "status" = "passed")) "Current CTRF is not all passing"
        require (tests |> Array.exists (fun item -> text (field item "extra") "method" = text witness "member")) "Missing delegate witness"
    else
        let report = XDocument.Parse(Encoding.UTF8.GetString(packet.Load(text execution "path")))
        let tests = report.Descendants() |> Seq.filter (fun node -> node.Name.LocalName = "UnitTestResult") |> Seq.toArray
        require (tests.Length = 24 && tests |> Array.forall (fun node -> node.Attribute(XName.Get "outcome").Value = "Passed")) "Current TRX is not all passing"
        let matches = tests |> Array.filter (fun node -> node.Attribute(XName.Get "testName").Value.Contains(text witness "member", StringComparison.Ordinal))
        require (matches.Length = (field witness "cases").GetInt32()) "Resource case multiplicity drift"

let verify (packet: Packet) =
    let catalog = packet.Catalog
    let load = packet.Load
    let json = packet.Json
    let hash = packet.Hash
    let sourceHash = packet.SourceHash
    let manifests =
        items catalog "suites" |> Array.map (fun suite ->
            let document = if packet.Kind = TraversalR9 then packet.CurrentJson(text suite "manifestPath") else json (text suite "manifestPath")
            let receivers = if packet.Kind = TraversalR9 then validateRecordingManifest packet suite document.RootElement else Set.empty
            suite, document, receivers)
    let receivers = manifests |> Array.map (fun (_, _, receivers) -> receivers) |> Set.unionMany
    validateSourceBindings packet receivers (items catalog "sourceBindings")
    if packet.Kind = TraversalR9 then validateCurrentCoverage packet
    let censusDocument = json (text catalog "censusPath")
    let census = censusDocument.RootElement
    require ((field census "members").GetInt32() = 499 && (field census "stableMembers").GetInt32() = 468 && (field census "experimentalMembers").GetInt32() = 31) "Current census drift"
    let binaries = Dictionary<string, string>(StringComparer.Ordinal)
    for assembly in items census "assemblies" do
        let path = text assembly "file"
        let name, mvid = packet.PeIdentity path
        require (name = text assembly "assembly" && mvid = text assembly "mvid" && packet.Digest path = text assembly "sha256") "Census PE binding mismatch"
        require (sourceHash ("eng/api-baseline/" + name + ".public-api.txt") = text assembly "baselineSha256") "Current baseline drift"
        binaries.Add(name, packet.Digest path)
    let mutable totalRows = 0
    let mutable totalLaunches = 0
    for suite, manifestDocument, receivers in manifests do
        let manifest = manifestDocument.RootElement
        let policy = field manifest "policy"
        let observation = field manifest "observation"
        let policyHash = policy.GetRawText() |> Encoding.UTF8.GetBytes |> hash
        let paths section = items (field manifest section) "files" |> Array.map (fun path -> path.GetString())
        let inputHash = fingerprint packet sourceHash (paths "benchmarkInput")
        let protocolHash = fingerprint packet (fun path -> if receivers.Contains path then packet.Digest path else sourceHash path) (paths "protocol")
        if packet.Kind = TraversalR9 then
            validateInputClosure manifest (packet.CurrentInputPaths(text (field manifest "runBinding") "benchmarkProject"))
            validateSnapshot packet suite manifest policyHash inputHash protocolHash
            validateApprovedReceipts packet suite observation
        require (text observation "policyFingerprint" = policyHash && text observation "benchmarkInputFingerprint" = inputHash && text observation "protocolFingerprint" = protocolHash) "Observation fingerprint mismatch"
        use proposalDocument = json (text suite "proposalPath")
        require (JsonNode.DeepEquals(JsonNode.Parse(observation.GetRawText()), JsonNode.Parse(proposalDocument.RootElement.GetRawText()))) "Observation is not exact verifier proposal"
        let policyRows = items policy "rows"
        let included = policyRows |> Array.filter (fun row -> (field row "included").GetBoolean())
        require (included.Length = (field suite "expectedRows").GetInt32() && policyRows.Length - included.Length = (field suite "expectedExclusions").GetInt32()) "Policy row count drift"
        let byId = Dictionary<string, JsonElement>(StringComparer.Ordinal)
        for row in included do byId.Add(text row "id", row)
        let seen = HashSet<string>(StringComparer.Ordinal)
        for receiptPath in items suite "receiptPaths" |> Array.map (fun item -> item.GetString()) do
            use receiptDocument = json receiptPath
            let receipt = receiptDocument.RootElement
            require ((field receipt "succeeded").GetBoolean() && text receipt "candidateCommit" = text suite "expectedSnapshot") "Receipt failure/snapshot drift"
            require (text receipt "policyFingerprint" = policyHash && text receipt "benchmarkInputFingerprint" = inputHash && text receipt "protocolFingerprint" = protocolHash) "Receipt fingerprint drift"
            let receiptDirectory = Path.GetDirectoryName receiptPath
            let namedFile node = receiptDirectory.Replace('\\', '/') + "/" + text node "file"
            let namedCheck node = require (packet.Digest(namedFile node) = text node "sha256") "Receipt named file mismatch"
            for report in items receipt "reports" do namedCheck report
            let preflightBinding = field (field receipt "binding") "preflight"
            namedCheck preflightBinding
            use preflightDocument = json (namedFile preflightBinding)
            let preflight = preflightDocument.RootElement
            require (text preflight "candidateSnapshot" = text suite "expectedSnapshot") "Preflight snapshot mismatch"
            let launchesById = indexLaunches receipt
            for row in items receipt "rows" do
                let id = text row "id"
                require (seen.Add id && byId.ContainsKey id) "Duplicate/unregistered row"
                let expected = byId[id]
                validateRow row expected
                require (launchesById.ContainsKey id) "Expected one actual launch per row"
                let launches = [| launchesById[id] |]
                for launch in launches do
                    namedCheck launch
                    let matched = Regex.Match(Encoding.UTF8.GetString(load (namedFile launch)), "funnysharp-workload:([^\\r\\n]+)")
                    require matched.Success "Missing actual workload binding"
                    use workloadDocument = JsonDocument.Parse(Convert.FromBase64String matched.Groups[1].Value)
                    let workload = workloadDocument.RootElement
                    validateWorkload packet (text suite "expectedSnapshot") (text preflight "runId") binaries workload
                    totalLaunches <- totalLaunches + 1
                totalRows <- totalRows + 1
        require (seen.Count = included.Length) "Missing measured rows"
        manifestDocument.Dispose()
    let closureDocument = json (text catalog "closurePath")
    let closure = closureDocument.RootElement
    for binding in items closure "producerAndReportFiles" do checkBinding packet binding
    checkBinding packet (field closure "metadataCensus")
    checkBinding packet (field closure "benchmarkBindings")
    for witness in items closure "witnesses" do validateWitness packet binaries witness
    require (totalRows = 230 && totalLaunches = 230) "Fresh pair row/launch count drift"
    printfn "CURRENT_PERFORMANCE_PORTABLE_PASS objects=%d locators=%d rows=%d launches=%d census=499/468/31 runtime=24 writes=0 network=0" packet.Stats.objects packet.Stats.locators totalRows totalLaunches
    closureDocument.Dispose()
    censusDocument.Dispose()
    
