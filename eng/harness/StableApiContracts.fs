module FunnySharp.Harness.StableApiContracts

open System
open System.Collections.Generic
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open FunnySharp.Harness.ReleaseVerifyArtifacts

let private require condition message =
    if not condition then invalidOp ("Stable semantic proof: " + message)

let private stringValue (node: JsonElement) : string =
    match node.GetString() with
    | null -> invalidOp "Stable semantic proof: a required string is null."
    | value -> value

let private hashText (value: string) =
    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes value)).ToLowerInvariant()

let private hashFile path =
    use stream = File.OpenRead path
    Convert.ToHexString(SHA256.HashData stream).ToLowerInvariant()

/// Validate the exact proof references against authoritative baseline identities.
/// Source invariants remain source evidence, never an invented runtime execution.
let validateProofIndex
    (root: string)
    (expected: Map<string, string * string * string>)
    (index: JsonElement)
    : unit =
    let field (node: JsonElement) (name: string) = node.GetProperty name
    let text node name = stringValue (field node name)
    let items (node: JsonElement) = node.EnumerateArray() |> Seq.toArray
    let properties (node: JsonElement) = node.EnumerateObject() |> Seq.toArray
    let optionalItems (node: JsonElement) (name: string) =
        match node.TryGetProperty name with
        | true, value -> items value
        | _ -> [||]
    let resolve (path: string) = Path.Combine(root, path)
    let bound path sha =
        let absolute = resolve path
        require (File.Exists absolute && hashFile absolute = sha) ("missing or stale file: " + path)
        absolute
    let boundNode node = bound (text node "path") (text node "sha256")
    let sources = Dictionary<string, string[]>(StringComparer.Ordinal)
    let lines path =
        match sources.TryGetValue path with
        | true, value -> value
        | _ ->
            let value = File.ReadAllLines(resolve path)
            sources.Add(path, value)
            value
    let excerpt path first last =
        let source = lines path
        require (first >= 1 && last >= first && last <= source.Length) ("invalid source span: " + path)
        String.concat "\n" source.[first - 1 .. last - 1]
    let catalog name =
        (field index name).EnumerateObject()
        |> Seq.map (fun property -> property.Name, property.Value)
        |> Map.ofSeq
    let sourceUnits = catalog "sourceUnits"
    let cases = catalog "caseCatalog"
    let assertions = catalog "assertionUnits"
    let compilerProofs = catalog "compilerProofs"
    let xmlNodes = catalog "xmlNodes"
    let transfers = items (field index "sourceTransfers") |> Array.map (fun value -> text value "key", value) |> Map.ofArray
    let sourceRef (reference: JsonElement) =
        require (sourceUnits.ContainsKey(stringValue reference)) "missing source invariant or transfer span."
    let caseRef (reference: JsonElement) =
        require (cases.ContainsKey(stringValue reference)) "missing applicable runtime case proof."
    for property in properties (field index "fileHashes") do
        bound property.Name (stringValue property.Value) |> ignore
    for KeyValue(_, source) in sourceUnits do
        let path = text source "path"
        bound path (text source "fileSha256") |> ignore
        let code = excerpt path ((field source "startLine").GetInt32()) ((field source "endLine").GetInt32())
        require (code = text source "code" && hashText code = text source "codeSha256") "source invariant excerpt changed."
    for KeyValue(_, assertion) in assertions do
        let path = text assertion "path"
        bound path (text assertion "sha256") |> ignore
        let body = (excerpt path ((field assertion "start").GetInt32()) ((field assertion "end").GetInt32())).Trim()
        require (body = text assertion "methodBody") "runtime assertion method changed."
        for line in items (field assertion "assertionLines") do
            let number = (field line "line").GetInt32()
            require ((excerpt path number number).Trim() = (text line "text").Trim()) "runtime assertion line changed."
        for helper in items (field assertion "helperSpans") do
            let code = (excerpt path ((field helper "startLine").GetInt32()) ((field helper "endLine").GetInt32())).Trim()
            require (code = text helper "code") "runtime assertion helper changed or is truncated."
    let receipts = Dictionary<string, JsonElement>(StringComparer.Ordinal)
    for receipt in items (field index "finalReceipts") do
        let path = text receipt "path"
        use document = JsonDocument.Parse(File.ReadAllText(boundNode receipt))
        let results = field document.RootElement "results"
        let tests = items (field results "tests")
        let summary = field results "summary"
        require (tests.Length > 0 && tests |> Array.forall (fun test -> text test "status" = "passed")) "runtime receipt is not all passing."
        require ((field summary "tests").GetInt32() = tests.Length && (field summary "passed").GetInt32() = tests.Length) "runtime counts contradict actual cases."
        receipts.Add(path, document.RootElement.Clone())
    for KeyValue(_, case) in cases do
        require (assertions.ContainsKey(text case "assertionSource")) "runtime case has no actual assertion method."
        let executions = items (field case "executions")
        require (executions.Length > 0) "runtime case has no execution."
        for execution in executions do
            let path = text execution "receipt"
            require (receipts.ContainsKey path) "runtime case references a historical or unknown receipt."
            let tests = items (field (field receipts.[path] "results") "tests")
            let matches = tests |> Array.filter (fun test -> text (field test "extra") "id" = text execution "receiptCaseId")
            require (matches.Length = 1) "runtime case ID is missing or duplicated."
            let actual = matches.[0]
            require (text actual "status" = "passed" && text actual "name" = text case "name"
                     && text (field actual "extra") "type" = text case "type"
                     && text (field actual "extra") "method" = text case "method") "runtime case identity or status differs."
    for KeyValue(_, transfer) in transfers do
        require (expected.ContainsKey(text transfer "from") && expected.ContainsKey(text transfer "to")) "unknown source-transfer member."
        require (not (String.IsNullOrWhiteSpace(text transfer "mechanism"))) "source transfer has no bounded mechanism."
        for reference in items (field transfer "sourceRefs") do sourceRef reference
        for reference in items (field transfer "targetSourceRefs") do sourceRef reference
        let targetCases = items (field transfer "targetRuntimeCaseRefs")
        require (targetCases.Length > 0) "source transfer has no actual target execution."
        for reference in targetCases do caseRef reference
    use compilerDocument = JsonDocument.Parse(File.ReadAllText(boundNode (field index "finalCompiler")))
    let compilerRows = items (field compilerDocument.RootElement "rows") |> Array.map (fun row -> text row "identity", row) |> Map.ofArray
    for KeyValue(identity, proof) in compilerProofs do
        require (compilerRows.ContainsKey identity) "missing actual compiler member."
        let actual = compilerRows.[identity]
        let probes = items (field actual "proofs")
        require (probes.Length = (items (field proof "proofs")).Length && probes.Length > 0) "missing applicable compiler probe."
        for probe in probes do
            bound (text probe "Source") (text probe "SourceSha256") |> ignore
            let expectedIds = items (field probe "ExpectedDiagnostics") |> Array.collect (fun d -> Array.create ((field d "count").GetInt32()) (text d "id")) |> Array.sort
            let diagnostics = items (field probe "ActualDiagnostics")
            let actualIds = diagnostics |> Array.map (fun d -> text d "Id") |> Array.sort
            require (expectedIds = actualIds && text probe "Terminal" <> "FAIL") "compiler rejection diagnostics differ."
            for diagnostic in diagnostics do
                require (not ((field diagnostic "Suppressed").GetBoolean()) && (field diagnostic "Line").GetInt32() > 0
                         && (field diagnostic "Column").GetInt32() > 0) "compiler diagnostic is suppressed or unattributed."
            if expectedIds.Length = 0 then
                require ((field probe "EmitSucceeded").GetBoolean() && (field probe "ExactBindingObserved").GetBoolean()) "legal compiler use did not emit with exact binding."
    let dimensionNames =
        set [ "nameAndOutput"; "compilerNullability"; "defaultAndGuards"; "exceptionsCancellationStatusToken"
              "callbacksOrderingConsumption"; "counterpartOrAbsence"; "xmlAndAliases"; "requiredRuntimeClauses" ]
    let rows = items (field index "rows")
    let identities = rows |> Array.map (fun row -> text row "identity")
    require (identities.Length = (Set.ofArray identities).Count && Set.ofArray identities = (expected |> Map.keys |> Set.ofSeq)) "missing, duplicate or unknown stable member."
    for row in rows do
        let identity = text row "identity"
        let assembly, declaring, signature = expected.[identity]
        require (text row "assemblyHeader" = assembly && text row "typeHeader" = declaring && text row "memberLine" = signature) "exact baseline identity changed."
        let dimensions = properties (field row "dimensions")
        require (dimensions |> Array.map (fun d -> d.Name) |> Set.ofArray = dimensionNames) "missing semantic dimension."
        for dimension in dimensions do
            let value = dimension.Value
            let claim = field value "claim"
            if dimension.Name = "counterpartOrAbsence" && claim.ValueKind = JsonValueKind.Array then
                let counterparts = items claim
                require (counterparts.Length > 0 && counterparts |> Array.forall (fun target -> expected.ContainsKey(stringValue target))) "unknown counterpart identity."
            else
                require (not (String.IsNullOrWhiteSpace(stringValue claim))) "missing semantic contract."
            let applicability = text value "applicability"
            require (applicability = "applicable" || applicability = "notApplicable") "unresolved dimension applicability."
            let references = Array.append (optionalItems value "sourceRefs") (optionalItems value "sourceInvariantRefs")
            require (references.Length > 0) "dimension has no source proof."
            for reference in references do sourceRef reference
            for reference in optionalItems value "runtimeCaseRefs" do caseRef reference
            for proof in optionalItems value "proofs" do caseRef (field proof "caseRef")
            for reference in optionalItems value "sourceTransferRefs" do
                let key = stringValue reference
                require (transfers.ContainsKey key && text transfers.[key] "from" = identity) "unknown or unrelated source transfer."
        require (compilerProofs.ContainsKey identity) "missing member compiler proof."
        let runtime = field (field row "dimensions") "requiredRuntimeClauses"
        if text runtime "applicability" = "applicable" then
            require ((optionalItems runtime "proofs").Length > 0 || (optionalItems runtime "sourceTransferRefs").Length > 0) "missing applicable runtime proof."
        else
            let runtimeAbi = declaring.StartsWith("DELEGATE ", StringComparison.Ordinal) && signature.TrimStart().StartsWith("CONSTRUCTOR ", StringComparison.Ordinal)
            let enumStorage = declaring.StartsWith("ENUM ", StringComparison.Ordinal) && signature.Trim() = "FIELD System.Int32 value__"
            require (runtimeAbi || enumStorage) "runtime NA is unjustified by the exact metadata mechanism."
        let xml = field (field row "dimensions") "xmlAndAliases"
        bound (text xml "packageXmlPath") (text xml "packageXmlSha256") |> ignore
        let direct = field xml "directNode"
        let alias = field xml "alias"
        let nodeId = if direct.ValueKind = JsonValueKind.Object then text direct "id" else
                         require (alias.ValueKind = JsonValueKind.Object) "unresolved XML alias."
                         bound (text alias "policy") (text alias "policySha256") |> ignore
                         text alias "typeXmlId"
        require (xmlNodes.ContainsKey(text row "assembly" + "|" + nodeId)) "missing actual XML node or alias target."

/// Run the current index against the exact canonical baseline and installed XML identities.
let verify (root: string) (proofPath: string) : unit =
    use document = JsonDocument.Parse(File.ReadAllText proofPath)
    let index = document.RootElement
    let excluded = index.GetProperty("experimentalExclusions").EnumerateArray() |> Seq.map (fun row -> stringValue (row.GetProperty("identity"))) |> Set.ofSeq
    let expected =
        [ for key, name in [ "C", "FunnySharp"; "H", "FunnySharp.AspNetCore" ] do
              let lines = File.ReadAllLines(Path.Combine(root, "eng/api-baseline", name + ".public-api.txt"))
              let mutable declaring = ""
              for number in 1 .. lines.Length - 1 do
                  let line = lines.[number]
                  if line.StartsWith("  ", StringComparison.Ordinal) then
                      let identity = key + ":" + string (number + 1)
                      if not (excluded.Contains identity) then yield identity, (lines.[0], declaring, line)
                  elif not (String.IsNullOrWhiteSpace line) then declaring <- line ] |> Map.ofList
    require (expected.Count = 468 && excluded.Count = 31) "canonical468 stable/31 experimental census differs."
    let paths = index.GetProperty("rows").EnumerateArray() |> Seq.map (fun row -> stringValue (row.GetProperty("dimensions").GetProperty("xmlAndAliases").GetProperty("packageXmlPath"))) |> Seq.distinct |> Seq.map (fun path -> Path.Combine(root, path)) |> Seq.toList
    let inventory = getXmlDocumentationInventory root paths
    let count = inventory |> Seq.sumBy (fun node ->
        match node with
        | null -> invalidOp "Stable semantic proof: installed inventory is null."
        | value ->
            match value.["exportedMembers"] with
            | null -> invalidOp "Stable semantic proof: installed member count is missing."
            | members -> members.GetValue<int>())
    require (count = 499) "installed exported census differs."
    validateProofIndex root expected index

let main (args: string[]) : int =
    let root = Directory.GetCurrentDirectory()
    let path = if args.Length = 0 then Path.Combine(root, "docs/audits/goal-24-resolution/stable-api-semantic-proofs.json") else Path.GetFullPath args.[0]
    if args.Length > 1 then
        Console.Error.WriteLine("error: expected at most one semantic proof-index path.")
        2
    else
        try
            verify root path
            printfn "Verified468 exact stable semantic members and3744 dimensions; source-only evidence is not runtime execution."
            0
        with exceptionValue ->
            Console.Error.WriteLine("error: " + exceptionValue.Message)
            1
