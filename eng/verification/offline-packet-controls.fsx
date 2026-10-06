// Fresh controls over copied decoded data, after the genuine fixed-pin loader.
// This is not a catalog replacement, and the production entry has no bypass switch.
#load "CurrentPacket.fs"
open System
open System.IO
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open System.Text.RegularExpressions
open System.Collections.Generic
open CurrentPacket

let args = fsi.CommandLineArgs |> Array.skip 1
require (args.Length = 1) "Expected valid current repository root"
let root = Path.GetFullPath args.[0]
let packet = new Packet(root, TraversalR9)
let catalog = packet.Catalog
let suite = (items catalog "suites")[0]
let receiptPath = ((items suite "receiptPaths")[0]).GetString()
let receiptDoc = packet.Json receiptPath
let receipt = receiptDoc.RootElement
let manifestDoc = packet.Json(text suite "manifestPath")
let row = (items receipt "rows")[0]
let policyRow = items (field manifestDoc.RootElement "policy") "rows" |> Array.find (fun x -> text x "id" = text row "id")
let launch = (items (field receipt "binding") "launches")[0]
let launchPath = Path.GetDirectoryName(receiptPath).Replace('\\','/') + "/" + text launch "file"
let launchBytes = packet.Load launchPath
let matched = Regex.Match(Encoding.UTF8.GetString launchBytes, "funnysharp-workload:([^\\r\\n]+)")
require matched.Success "Expected genuine workload"
let workloadDoc = JsonDocument.Parse(Convert.FromBase64String matched.Groups.[1].Value)
let workload = workloadDoc.RootElement
let censusDoc = packet.Json(text catalog "censusPath")
let binaries = Dictionary<string,string>(StringComparer.Ordinal)
for assembly in items censusDoc.RootElement "assemblies" do binaries.Add(text assembly "assembly", text assembly "sha256")
let closureDoc = packet.Json(text catalog "closurePath")
let witnesses = items closureDoc.RootElement "witnesses"
let records = ResizeArray<obj>()
let copied (value: JsonElement) = JsonNode.Parse(value.GetRawText())
let element (node: JsonNode) = JsonSerializer.SerializeToElement node
let reject (name: string) (expected: string) action =
    let mutable message = ""
    try action() with error -> message <- error.Message
    require (message.Contains(expected, StringComparison.Ordinal)) (name + ": expected rejection " + expected + "; got " + message)
    records.Add(box {| name = name; decision = "reject"; message = message |})
let positive name action =
    action()
    records.Add(box {| name = name; decision = "pass" |})
positive "genuine-receipt-index" (fun () -> indexLaunches receipt |> ignore)
positive "genuine-row" (fun () -> validateRow row policyRow)
let validate node = validateWorkload packet (text suite "expectedSnapshot") (text workload "runId") binaries node
positive "genuine-workload" (fun () -> validate workload)
for witness in witnesses do positive ("genuine-witness-" + text witness "member") (fun () -> validateWitness packet binaries witness)
let badBinding = copied (field closureDoc.RootElement "metadataCensus")
badBinding.["path"] <- JsonValue.Create(text catalog "closurePath")
reject "later-consumer-another-preverified-object" "Evidence binding mismatch" (fun () -> checkBinding packet (element badBinding))
let badSource = copied ((items catalog "sourceBindings").[0])
badSource.["sha256"] <- JsonValue.Create(String.replicate 64 "0")
reject "stale-source-expectation" "Current source/protocol bytes changed" (fun () -> checkSource packet (element badSource))
let badRow = copied row
badRow.["method"] <- JsonValue.Create("wrong-method")
reject "wrong-row-policy" "Row policy identity mismatch" (fun () -> validateRow (element badRow) policyRow)
let badAllocation = copied row
badAllocation.["allocatedBytesPerOperation"] <- JsonValue.Create(((field policyRow "allocationBudgetBytes").GetInt64()) + 1L)
reject "allocation-budget-plus-one" "Allocation budget regression" (fun () -> validateRow (element badAllocation) policyRow)
let duplicate = copied receipt
let duplicateLaunches = duplicate.["binding"].["launches"].AsArray()
duplicateLaunches.Add(duplicateLaunches.[0].DeepClone())
reject "duplicate-launch-id" "Expected one actual launch per row" (fun () -> indexLaunches (element duplicate) |> ignore)
let missing = copied receipt
missing.["binding"].["launches"].AsArray().RemoveAt(0)
reject "missing-launch-id" "Expected one actual launch per row" (fun () -> indexLaunches (element missing) |> ignore)
let unregistered = copied receipt
unregistered.["binding"].["launches"].[0].["rowId"] <- JsonValue.Create("unregistered-row")
reject "unregistered-launch-id" "Expected one actual launch per row" (fun () -> indexLaunches (element unregistered) |> ignore)
let badJoin = copied workload
badJoin.["runId"] <- JsonValue.Create("wrong-run")
reject "wrong-launch-preflight-join" "Launch/preflight join mismatch" (fun () -> validate (element badJoin))
let badMvid = copied workload
badMvid.["assemblies"].[0].["mvid"] <- JsonValue.Create(Guid.Empty.ToString())
reject "wrong-loaded-mvid" "Loaded assembly bytes/MVID mismatch" (fun () -> validate (element badMvid))
let badWorkloadMvid = copied workload
badWorkloadMvid.["workload"].["mvid"] <- JsonValue.Create(Guid.Empty.ToString())
reject "wrong-r9-workload-mvid" "Workload bytes/MVID mismatch" (fun () -> validate (element badWorkloadMvid))
let swapped = copied workload
swapped.["assemblies"].[0].["file"] <- JsonValue.Create(text (field workload "workload") "file")
reject "later-assembly-other-valid-pe" "Loaded assembly bytes/MVID mismatch" (fun () -> validate (element swapped))
let badDelegate = copied (witnesses |> Array.find (fun w -> text w "kind" = "delegate-apm"))
badDelegate.["member"] <- JsonValue.Create("nonexistent-member")
reject "wrong-semantic-delegate-member" "Missing delegate witness" (fun () -> validateWitness packet binaries (element badDelegate))
let badResource = copied (witnesses |> Array.find (fun w -> text w "kind" <> "delegate-apm"))
badResource.["cases"] <- JsonValue.Create(999)
reject "wrong-semantic-xml-multiplicity" "Resource case multiplicity drift" (fun () -> validateWitness packet binaries (element badResource))
let currentManifestDoc = packet.CurrentJson(text suite "manifestPath")
let currentManifest = currentManifestDoc.RootElement
if (field currentManifest "protocol").TryGetProperty "recordingIdentity" |> fst then
    let receivers = validateRecordingManifest packet suite currentManifest
    for currentSuite in items catalog "suites" do
        use document = packet.CurrentJson(text currentSuite "manifestPath")
        positive ("current-manifest-" + text currentSuite "name") (fun () -> validateRecordingManifest packet currentSuite document.RootElement |> ignore)
        positive ("approved-receipts-" + text currentSuite "name") (fun () -> validateApprovedReceipts packet currentSuite (field document.RootElement "observation"))
    positive "current-source-partition" (fun () -> validateSourceBindings packet receivers (items catalog "sourceBindings"))
    positive "current-coverage" (fun () -> validateCurrentCoverage packet)
    let inputPaths = packet.CurrentInputPaths(text (field currentManifest "runBinding") "benchmarkProject")
    positive "current-input-inventory" (fun () -> validateInputClosure currentManifest inputPaths)
    reject "new-unlisted-build-input" "Current benchmark input closure changed" (fun () -> validateInputClosure currentManifest (Array.append inputPaths [| "src/FunnySharp/NewInput.cs" |]))
    for name, path in [ "changed-producing-source", "benchmarks/FunnySharp.Benchmarks/ReceiptExporterCore.cs"; "changed-shipping-input", "src/FunnySharp/Option.cs"; "changed-build-input", "Directory.Build.props"; "changed-current-semantic-source", "tests/FunnySharp.Tests/GeneratedDelegateContractTests.cs" ] do
        let binding = JsonSerializer.SerializeToElement {| path = path; sha256 = packet.Hash(Array.append (packet.Load path) (Encoding.UTF8.GetBytes "\n// changed source\n")) |}
        reject name "Current source/protocol bytes changed" (fun () -> validateSourceBindings packet receivers [| binding |])
    for name, change in
        [ "producer-as-receiver", (fun (node: JsonNode) -> node.["protocol"].["recordingIdentity"].["receiverFiles"].AsArray().Add(JsonValue.Create("benchmarks/FunnySharp.Benchmarks/ReceiptExporterCore.cs")))
          "missing-receiver", (fun node -> node.["protocol"].["recordingIdentity"].["receiverFiles"].AsArray().RemoveAt(0))
          "unknown-receiver", (fun node -> node.["protocol"].["recordingIdentity"].["receiverFiles"].[0] <- JsonValue.Create("eng/harness/Other.fs"))
          "wrong-recorded-source-hash", (fun node -> node.["protocol"].["recordingIdentity"].["files"].[0].["sha256"] <- JsonValue.Create(String.replicate 64 "0"))
          "missing-recorded-protocol-file", (fun node -> node.["protocol"].["recordingIdentity"].["files"].AsArray().RemoveAt(0))
          "unsafe-recorded-protocol-path", (fun node -> node.["protocol"].["recordingIdentity"].["files"].[0].["path"] <- JsonValue.Create("../outside.cs"))
          "wrong-recording-catalog-pin", (fun node -> node.["protocol"].["recordingIdentity"].["provenance"].["sha256"] <- JsonValue.Create(String.replicate 64 "0")) ] do
        let changed = copied currentManifest
        change changed
        reject name "Recording projection differs from fixed source evidence" (fun () -> validateRecordingManifest packet suite (element changed) |> ignore)
    for name, change in
        [ "changed-current-policy", (fun (node: JsonNode) -> node.["policy"].["rows"].[0].["allocationBudgetBytes"] <- JsonValue.Create(999999))
          "changed-current-input-list", (fun node -> node.["benchmarkInput"].["files"].AsArray().RemoveAt(0))
          "unknown-protocol-member", (fun node -> node.["protocol"].["files"].AsArray().Add(JsonValue.Create("eng/calibration.txt")))
          "changed-current-base", (fun node -> node.["runBinding"].["baseTree"] <- JsonValue.Create(String.replicate 40 "0"))
          "changed-approved-snapshot", (fun node -> node.["observation"].["candidateCommit"] <- JsonValue.Create("snapshot:" + String.replicate 64 "0")) ] do
        let changed = copied currentManifest
        change changed
        reject name "Current manifest differs from immutable recording" (fun () -> validateRecordingManifest packet suite (element changed) |> ignore)
    let observation = field currentManifest "observation"
    let policyHash = packet.Hash(Encoding.UTF8.GetBytes((field currentManifest "policy").GetRawText()))
    let inputHash = fingerprint packet packet.SourceHash (items (field currentManifest "benchmarkInput") "files" |> Array.map (fun node -> node.GetString()))
    let protocolHash = fingerprint packet packet.Digest (items (field currentManifest "protocol") "files" |> Array.map (fun node -> node.GetString()))
    positive "independently-reconstructed-recorded-snapshot" (fun () -> validateSnapshot packet suite currentManifest policyHash inputHash protocolHash)
    let badSnapshot = copied currentManifest
    badSnapshot.["observation"].["candidateCommit"] <- JsonValue.Create("snapshot:" + String.replicate 64 "0")
    reject "independent-snapshot-mismatch" "Source snapshot identity mismatch" (fun () -> validateSnapshot packet suite (element badSnapshot) policyHash inputHash protocolHash)
    for name, change in
        [ "wrong-approved-receipt-hash", (fun (node: JsonNode) -> node.["receipts"].[0].["sha256"] <- JsonValue.Create(String.replicate 64 "0"))
          "wrong-approved-receipt-file", (fun node -> node.["receipts"].[0].["file"] <- JsonValue.Create("unknown-performance-receipt.json"))
          "missing-approved-receipt", (fun node -> node.["receipts"].AsArray().RemoveAt(0)) ] do
        let changed = copied observation
        change changed
        reject name "Approved receipt file/hash set mismatch" (fun () -> validateApprovedReceipts packet suite (element changed))
// Exercise the actual fixed-pin loader, without changing a shared checkout or
// re-signing a catalog. Each minimal disposable root stops at the intended guard.
let fixtures = Path.Combine(Path.GetTempPath(), "funnysharp-current-packet-controls-" + Guid.NewGuid().ToString("N"))
let catalogPath = packet.CatalogIdentity.path
let catalogBytes = File.ReadAllBytes(Path.Combine(root, catalogPath))
try
    for name, expected in [ "tampered-catalog", "Catalog hash mismatch"; "missing-object", "Could not find"; "tampered-physical-object", "Physical object hash mismatch" ] do
        let fixture = Path.Combine(fixtures, name)
        let targetCatalog = Path.Combine(fixture, catalogPath)
        Directory.CreateDirectory(Path.GetDirectoryName targetCatalog) |> ignore
        File.WriteAllBytes(targetCatalog, if name = "tampered-catalog" then Array.append catalogBytes [| byte ' ' |] else catalogBytes)
        if name = "tampered-physical-object" then
            let firstObject = (items catalog "objects").[0]
            let physical = packet.Relative(text firstObject "physicalPath")
            let target = Path.Combine(fixture, physical)
            Directory.CreateDirectory(Path.GetDirectoryName target) |> ignore
            File.WriteAllBytes(target, Array.append (File.ReadAllBytes(Path.Combine(root, physical))) [| byte 'A' |])
        reject name expected (fun () -> use invalid = new Packet(fixture, TraversalR9) in invalid.Stats |> ignore)
finally
    if Directory.Exists fixtures then Directory.Delete(fixtures, true)
require (not (Directory.Exists fixtures)) "Integrity fixtures were not cleaned up"
printfn "%s" (JsonSerializer.Serialize({| schema = "funnysharp-offline-successor-controls/v1"; trust = "unchanged fixed-pin catalog and genuinely verified objects; parsed copies and disposable integrity fixtures mutated"; root = root; records = records.ToArray(); stats = packet.Stats; sharedWrites = 0; fixtureWrites = 4; fixturesRemoved = true; network = 0 |}))
printfn "OFFLINE_PACKET_CONTROLS_PASS controls=%d" records.Count
for doc in [receiptDoc; manifestDoc; workloadDoc; censusDoc; closureDoc; currentManifestDoc] do doc.Dispose()
(packet :> IDisposable).Dispose()
