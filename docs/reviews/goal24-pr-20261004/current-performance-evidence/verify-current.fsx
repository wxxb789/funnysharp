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

let arguments = fsi.CommandLineArgs |> Array.skip 1
if arguments.Length > 1 then failwith "Usage: dotnet fsi verify-current.fsx -- [repository-root]"
let root = if arguments.Length = 1 then Path.GetFullPath arguments[0] else Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "../../../.."))
let directory = "docs/reviews/goal24-pr-20261004/current-performance-evidence"
let hash (bytes: byte array) = bytes |> SHA256.HashData |> Convert.ToHexStringLower
let catalogBytes = File.ReadAllBytes(Path.Combine(root, directory, "catalog.json"))
if hash catalogBytes <> "093523fa54b126b9f65dd34718371f7f2d11d4afc729cc85cf4b3e4f6d98d187" then failwith "Catalog hash mismatch"
let catalogDocument = JsonDocument.Parse catalogBytes
let catalog = catalogDocument.RootElement
let field (node: JsonElement) (name: string) = node.GetProperty name
let text node name = (field node name).GetString()
let items node name = (field node name).EnumerateArray() |> Seq.toArray
let require condition message = if not condition then failwith message
require (text catalog "schema" = "funnysharp-portable-current-performance/v1") "Unsupported catalog"
let original = (text catalog "originalWorkspace").Replace('\\', '/').TrimEnd('/')
let relative (value: string) =
    let value = value.Replace('\\', '/')
    let value = if value.StartsWith(original + "/", StringComparison.OrdinalIgnoreCase) then value.Substring(original.Length + 1) else value
    require (not (Path.IsPathFullyQualified value) && not (value.Split('/') |> Array.contains "..")) "Locator escapes catalog"
    value
let objects = Dictionary<string, byte array>(StringComparer.Ordinal)
for item in items catalog "objects" do
    let key = text item "sha256"
    require (not (objects.ContainsKey key)) "Duplicate object hash"
    let encoded = File.ReadAllBytes(Path.Combine(root, relative (text item "physicalPath")))
    require (hash encoded = text item "physicalSha256") "Physical object hash mismatch"
    let bytes = Convert.FromBase64String(Encoding.UTF8.GetString encoded)
    require (int64 bytes.Length = (field item "bytes").GetInt64() && hash bytes = key) "Decoded object hash mismatch"
    objects.Add(key, bytes)
let files = Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
for item in items catalog "files" do
    let path = relative (text item "path")
    require (not (files.ContainsKey path)) "Duplicate logical locator"
    let bytes = objects[text item "sha256"]
    require (int64 bytes.Length = (field item "bytes").GetInt64()) "Logical byte length mismatch"
    files.Add(path, item)
let load path = objects[text files[relative path] "sha256"]
let json path = JsonDocument.Parse(load path)
let checkBinding node = require (hash (load (text node "path")) = text node "sha256") "Evidence binding mismatch"
let sourceHash path = File.ReadAllBytes(Path.Combine(root, relative path)) |> hash
for binding in items catalog "sourceBindings" do
    require (sourceHash (text binding "path") = text binding "sha256") "Current source/protocol bytes changed"
let fingerprint (paths: JsonElement array) =
    let paths = paths |> Array.map (fun path -> path.GetString()) |> Array.sortWith (fun a b -> StringComparer.Ordinal.Compare(a, b))
    paths |> Array.map (fun path -> path.Replace('\\', '/') + "\u0000" + sourceHash path + "\n") |> String.concat "" |> Encoding.UTF8.GetBytes |> hash
let peIdentity bytes =
    use stream = new MemoryStream(bytes, false)
    use pe = new PEReader(stream)
    let reader = pe.GetMetadataReader()
    reader.GetString(reader.GetAssemblyDefinition().Name), reader.GetGuid(reader.GetModuleDefinition().Mvid).ToString()
let censusDocument = json (text catalog "censusPath")
let census = censusDocument.RootElement
require ((field census "members").GetInt32() = 499 && (field census "stableMembers").GetInt32() = 468 && (field census "experimentalMembers").GetInt32() = 31) "Current census drift"
let binaries = Dictionary<string, string>(StringComparer.Ordinal)
for assembly in items census "assemblies" do
    let bytes = load (text assembly "file")
    let name, mvid = peIdentity bytes
    require (name = text assembly "assembly" && mvid = text assembly "mvid" && hash bytes = text assembly "sha256") "Census PE binding mismatch"
    require (sourceHash ("eng/api-baseline/" + name + ".public-api.txt") = text assembly "baselineSha256") "Current baseline drift"
    binaries.Add(name, hash bytes)
let mutable totalRows = 0
let mutable totalLaunches = 0
for suite in items catalog "suites" do
    use manifestDocument = json (text suite "manifestPath")
    let manifest = manifestDocument.RootElement
    let policy = field manifest "policy"
    let observation = field manifest "observation"
    let policyHash = policy.GetRawText() |> Encoding.UTF8.GetBytes |> hash
    let inputHash = fingerprint (items (field manifest "benchmarkInput") "files")
    let protocolHash = fingerprint (items (field manifest "protocol") "files")
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
        let namedCheck node = require (hash (load (namedFile node)) = text node "sha256") "Receipt named file mismatch"
        for report in items receipt "reports" do namedCheck report
        let preflightBinding = field (field receipt "binding") "preflight"
        namedCheck preflightBinding
        use preflightDocument = json (namedFile preflightBinding)
        let preflight = preflightDocument.RootElement
        require (text preflight "candidateSnapshot" = text suite "expectedSnapshot") "Preflight snapshot mismatch"
        for row in items receipt "rows" do
            let id = text row "id"
            require (seen.Add id && byId.ContainsKey id) "Duplicate/unregistered row"
            let expected = byId[id]
            for name in [ "benchmarkClass"; "category"; "method"; "parameters"; "baseline" ] do
                require (JsonNode.DeepEquals(JsonNode.Parse((field row name).GetRawText()), JsonNode.Parse((field expected name).GetRawText()))) "Row policy identity mismatch"
            let allocation = (field row "allocatedBytesPerOperation").GetInt64()
            require (allocation >= 0L && allocation <= (field expected "allocationBudgetBytes").GetInt64()) "Allocation budget regression"
            let launches = items (field receipt "binding") "launches" |> Array.filter (fun launch -> text launch "rowId" = id)
            require (launches.Length = 1) "Expected one actual launch per row"
            for launch in launches do
                namedCheck launch
                let matched = Regex.Match(Encoding.UTF8.GetString(load (namedFile launch)), "funnysharp-workload:([^\\r\\n]+)")
                require matched.Success "Missing actual workload binding"
                use workloadDocument = JsonDocument.Parse(Convert.FromBase64String matched.Groups[1].Value)
                let workload = workloadDocument.RootElement
                require (text workload "runId" = text preflight "runId" && text workload "candidateSnapshot" = text suite "expectedSnapshot") "Launch/preflight join mismatch"
                let binary = field workload "workload"
                require (hash (load (text binary "file")) = text binary "sha256") "Workload bytes mismatch"
                for assembly in items workload "assemblies" do
                    let bytes = load (text assembly "file")
                    let name, mvid = peIdentity bytes
                    require (hash bytes = text assembly "sha256" && mvid = text assembly "mvid") "Loaded assembly bytes/MVID mismatch"
                    if binaries.ContainsKey name then require (hash bytes = binaries[name]) "Launch differs from current census"
                totalLaunches <- totalLaunches + 1
            totalRows <- totalRows + 1
    require (seen.Count = included.Length) "Missing measured rows"
let closureDocument = json (text catalog "closurePath")
let closure = closureDocument.RootElement
for binding in items closure "producerAndReportFiles" do checkBinding binding
checkBinding (field closure "metadataCensus")
checkBinding (field closure "benchmarkBindings")
for witness in items closure "witnesses" do
    checkBinding witness
    let execution = field witness "execution"
    checkBinding execution
    checkBinding (field execution "coreDll")
    require (text (field execution "coreDll") "sha256" = binaries["FunnySharp"]) "Witness used other core bytes"
    if text witness "kind" = "delegate-apm" then
        use reportDocument = json (text execution "path")
        let tests = items (field reportDocument.RootElement "results") "tests"
        require (tests.Length = 24 && tests |> Array.forall (fun item -> text item "status" = "passed")) "Current CTRF is not all passing"
        require (tests |> Array.exists (fun item -> text (field item "extra") "method" = text witness "member")) "Missing delegate witness"
    else
        let report = XDocument.Parse(Encoding.UTF8.GetString(load (text execution "path")))
        let tests = report.Descendants() |> Seq.filter (fun node -> node.Name.LocalName = "UnitTestResult") |> Seq.toArray
        require (tests.Length = 24 && tests |> Array.forall (fun node -> node.Attribute(XName.Get "outcome").Value = "Passed")) "Current TRX is not all passing"
        let matches = tests |> Array.filter (fun node -> node.Attribute(XName.Get "testName").Value.Contains(text witness "member", StringComparison.Ordinal))
        require (matches.Length = (field witness "cases").GetInt32()) "Resource case multiplicity drift"
require (totalRows = 230 && totalLaunches = 230) "Fresh pair row/launch count drift"
printfn "CURRENT_PERFORMANCE_PORTABLE_PASS objects=%d locators=%d rows=%d launches=%d census=499/468/31 runtime=24 writes=0 network=0" objects.Count files.Count totalRows totalLaunches

closureDocument.Dispose()
censusDocument.Dispose()
catalogDocument.Dispose()
