#r "Microsoft.VisualBasic.Core"
open System
open System.IO
open System.Security.Cryptography
open System.Text.Json
open Microsoft.VisualBasic.FileIO

let require condition message = if not condition then invalidOp message
let prop (name: string) (node: JsonElement) = node.GetProperty name
let text name node = (prop name node).GetString() |> Option.ofObj |> Option.defaultValue ""
let flag name node = (prop name node).GetBoolean()
let digestFile path =
    use source = File.OpenRead path
    use hash = SHA256.Create()
    Convert.ToHexString(hash.ComputeHash source).ToLowerInvariant()
let root = "Q:/repos/funnysharp-goal24-resolution/"
let originalPath = root + "docs/audits/goal-24-resolution/original-criterion-matrix.csv"
let originalRows () =
    require (digestFile originalPath = "2a3cb04787c36c2a05fafd054fa7bc24cddce473b7b65febcdf9fe865ee02d6d") "Original audit CSV bytes changed"
    use parser = new TextFieldParser(originalPath)
    parser.TextFieldType <- FieldType.Delimited
    parser.SetDelimiters(",")
    parser.HasFieldsEnclosedInQuotes <- true
    parser.TrimWhiteSpace <- false
    let headers = parser.ReadFields()
    let column name = Array.findIndex ((=) name) headers
    let rows = ResizeArray<string array>()
    while not parser.EndOfData do rows.Add(parser.ReadFields())
    require (rows.Count = 311) "Original audit row count changed"
    rows |> Seq.filter (fun row -> List.contains row.[column "verdict"] [ "FAIL"; "UNVERIFIED" ])
         |> Seq.map (fun row -> row.[column "criterion_id"], (row.[column "criterion"], row.[column "verdict"]))
         |> Map.ofSeq
let verify path requireAccepted =
    let original = originalRows()
    use document = JsonDocument.Parse(File.ReadAllText path)
    let rootNode = document.RootElement
    let rows = (prop "findings" rootNode).EnumerateArray() |> Seq.toArray
    let expected = [ for n in 1..40 -> sprintf "F%02d" n ] @ [ for n in 1..29 -> sprintf "U%02d" n ] |> Set.ofList
    require (rows.Length = 69 && original.Count = 69) "Exactly69 rows required"
    require (Set.ofArray (rows |> Array.map (text "tracking")) = expected) "Original tracking identity set mismatch"
    require (Set.count (Set.ofArray(rows |> Array.map (text "criterionId"))) = 69) "Duplicate original criterion identity"
    let proposalPath = root + "artifacts/audit-resolution/resume-20261004/final69-review-proposal.json"
    require (digestFile proposalPath = "f60ecc86a40c987e8146c9d30681b5123e00af540cb41dfc3e7d3e902929e642") "Reviewed69 proposal bytes changed"
    use proposal = JsonDocument.Parse(File.ReadAllText proposalPath)
    let originalTracking = (prop "rows" proposal.RootElement).EnumerateArray() |> Seq.map (fun row -> text "tracking" row, text "criterionId" row) |> Map.ofSeq
    for row in rows do
        let id = text "criterionId" row
        require (originalTracking.[text "tracking" row] = id) "Original tracking-to-criterion mapping changed"
        let expectedCriterion, expectedVerdict = original.[id]
        require (text "criterion" row = expectedCriterion && text "originalVerdict" row = expectedVerdict) ("Original identity/wording/verdict changed: " + text "tracking" row)
    let accepted = rows |> Array.filter (flag "finalAccepted")
    let openRows = rows |> Array.filter (fun row -> text "resolutionStatus" row = "OPEN" || not (flag "finalAccepted" row))
    printfn "EXACT69_ACCOUNTING rows=%d uniqueIds=69 originalFAIL=%d originalUNVERIFIED=%d accepted=%d openOrUnaccepted=%d originalSha256=%s" rows.Length (rows |> Array.filter (fun r -> text "originalVerdict" r = "FAIL") |> Array.length) (rows |> Array.filter (fun r -> text "originalVerdict" r = "UNVERIFIED") |> Array.length) accepted.Length openRows.Length (digestFile originalPath)
    if requireAccepted then
        require (accepted.Length = 69 && openRows.Length = 0) "Final closure requires69 accepted and zero OPEN/unaccepted"
        let reconciliation = prop "finalReconciliation" rootNode
        require (flag "wholeGoalAccepted" reconciliation && flag "productOrReleasePASS" reconciliation) "Final aggregate acceptance flags are false"
        let gate = prop "independentAggregateGateReview" reconciliation
        require (text "verdict" gate = "APPROVE" && flag "wholeGoalApproval" gate) "Actual independent wholeGoal24 gate approval missing"
        for row in rows do
            require ((text "resolutionStatus" row).StartsWith("RESOLVED", StringComparison.Ordinal)) ("Unaccepted resolution status: " + text "tracking" row)
            let binding = prop "finalCandidateBinding" row
            require (text "productCandidate" binding = "d8744c934d86833f9817245ecc7c78b1177b8b76") ("Current product binding missing: " + text "tracking" row)
            let evidence = (prop "evidence" (prop "finalResolution" row)).EnumerateArray() |> Seq.toArray
            require (evidence.Length > 0) ("No actual row evidence: " + text "tracking" row)
            for entry in evidence do
                let path = text "path" entry
                let full = if Path.IsPathRooted path then path else Path.Combine(root, path)
                require (File.Exists full && FileInfo(full).Length > 0L) ("Missing/empty row evidence: " + path)
                require (digestFile full = text "sha256" entry) ("Stale row evidence SHA256: " + path)
        printfn "EXACT69_FINAL_PASS original69identities preserved;zeroOPEN/stale/unaccepted;actualfullgoalAPPROVE;currentproduct/evidencehashes matched."
let args = fsi.CommandLineArgs |> Array.skip 1
let result =
    match args with
    | [| "--help" |] -> printfn "Usage: verify-final69.fsx <findings.json> [--require-accepted]"; 0
    | [| path |] -> try verify path false; 0 with e -> eprintfn "%s" e.Message; 1
    | [| path; "--require-accepted" |] -> try verify path true; 0 with e -> eprintfn "%s" e.Message; 1
    | _ -> eprintfn "Expected findings path and optional --require-accepted"; 2
exit result
