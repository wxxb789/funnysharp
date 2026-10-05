// Offline verification of frozen evidence, not a producer or a new acceptance decision.
#r "Microsoft.VisualBasic.Core"
open System
open System.Collections.Generic
open System.IO
open System.IO.Compression
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Text.RegularExpressions
open Microsoft.VisualBasic.FileIO

let require condition message = if not condition then invalidOp message
let equal label expected actual = require (expected = actual) (sprintf "%s: expected %A; actual %A" label expected actual)
let prop (name: string) (node: JsonElement) = node.GetProperty name
let text name node = (prop name node).GetString() |> Option.ofObj |> Option.defaultValue ""
let number name node = (prop name node).GetInt64()
let flag name node = (prop name node).GetBoolean()
let items name node = (prop name node).EnumerateArray() |> Seq.toArray
let has (name: string) (node: JsonElement) = fst (node.TryGetProperty name)
let hex (bytes: byte array) = Convert.ToHexString(bytes).ToLowerInvariant()
let utf8 = UTF8Encoding(false, true)
let product = "d8744c934d86833f9817245ecc7c78b1177b8b76"
let portable = "docs/audits/goal-24-resolution/portable-evidence/"
let audit = "docs/audits/goal-24-resolution/"

let safeRelative (path: string) =
    require (path.Length > 0 && not (path.Contains '\\') && not (path.Contains ':')
             && not (Path.IsPathRooted path) && not (path |> Seq.exists Char.IsControl)) ("Unsafe relative path: " + path)
    for part in path.Split '/' do
        require (part <> "" && part <> "." && part <> ".." && not (part.EndsWith ".") && not (part.EndsWith " ")
                 && not (Regex.IsMatch(part, "^(CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9])([.]|$)", RegexOptions.IgnoreCase)))
            ("Unsafe path segment: " + path)
let under root relative = safeRelative relative; Path.Combine(root, relative)
let regularFile (path: string) =
    let full = Path.GetFullPath path
    require (File.Exists full) ("Missing regular file: " + full)
    let rec check (current: string) =
        require ((File.GetAttributes current &&& FileAttributes.ReparsePoint) = enum 0) ("Reparse point: " + current)
        match Directory.GetParent current with null -> () | parent -> check parent.FullName
    check full
    full
let hashStream label expectedLength (source: Stream) =
    require (expectedLength >= 0L) (label + ": negative length")
    use hash = IncrementalHash.CreateHash HashAlgorithmName.SHA256
    let buffer = Array.zeroCreate<byte> 65536
    let mutable length = 0L
    let mutable count = source.Read(buffer, 0, buffer.Length)
    while count > 0 do
        require (int64 count <= expectedLength - length) (label + ": unexpected extra bytes")
        hash.AppendData(buffer, 0, count)
        length <- length + int64 count
        count <- source.Read(buffer, 0, buffer.Length)
    equal (label + " bytes") expectedLength length
    hex (hash.GetHashAndReset())
let hashFile path expectedLength =
    use source = File.OpenRead(regularFile path)
    equal (path + " file length") expectedLength source.Length
    hashStream path expectedLength source
let rec uniqueJson (node: JsonElement) =
    match node.ValueKind with
    | JsonValueKind.Object ->
        let names = HashSet<string>(StringComparer.Ordinal)
        for field in node.EnumerateObject() do
            require (names.Add field.Name) ("Duplicate JSON property: " + field.Name)
            uniqueJson field.Value
    | JsonValueKind.Array -> for value in node.EnumerateArray() do uniqueJson value
    | _ -> ()
let parseJson (source: Stream) =
    let document = JsonDocument.Parse(source, JsonDocumentOptions(MaxDepth = 64))
    try uniqueJson document.RootElement; document
    with _ -> document.Dispose(); reraise()
let fileJson path =
    use source = File.OpenRead(regularFile path)
    parseJson source
let bytesJson (bytes: byte array) =
    use source = new MemoryStream(bytes, false)
    parseJson source
let inventory label expectedCount (archive: ZipArchive) =
    equal (label + " entry count") expectedCount archive.Entries.Count
    let entries = Dictionary<string, ZipArchiveEntry>(StringComparer.Ordinal)
    let aliases = HashSet<string>(StringComparer.OrdinalIgnoreCase)
    for entry in archive.Entries do
        safeRelative entry.FullName
        require (aliases.Add entry.FullName) (label + ": duplicate or case-alias entry: " + entry.FullName)
        let attributes = uint32 entry.ExternalAttributes
        let unixType = (attributes >>> 16) &&& 0xF000u
        require ((attributes &&& 0x10u) = 0u && (unixType = 0u || unixType = 0x8000u)) (label + ": non-regular entry: " + entry.FullName)
        entries.Add(entry.FullName, entry)
    entries
let entryHash (entry: ZipArchiveEntry) expectedLength =
    equal (entry.FullName + " declared ZIP length") expectedLength entry.Length
    use source = entry.Open()
    hashStream entry.FullName expectedLength source
let entryJson (entry: ZipArchiveEntry) = use source = entry.Open() in parseJson source

// ZipArchive needs seeking. Re-open and stream-discard on backward seeks instead
// of allowing it to materialize an entire nested archive or writing a scratch file.
type EntrySeekStream(entry: ZipArchiveEntry) =
    inherit Stream()
    let mutable source = entry.Open()
    let mutable position = 0L
    let buffer = Array.zeroCreate<byte> 65536
    override _.CanRead = true
    override _.CanSeek = true
    override _.CanWrite = false
    override _.Length = entry.Length
    override this.Position
        with get () = position
        and set value = this.Seek(value, SeekOrigin.Begin) |> ignore
    override _.Read(bytes, offset, count) =
        let n = source.Read(bytes, offset, count)
        position <- position + int64 n
        n
    override this.Seek(offset, origin) =
        let target = match origin with SeekOrigin.Begin -> offset | SeekOrigin.Current -> position + offset | SeekOrigin.End -> entry.Length + offset | _ -> invalidArg "origin" "Unknown seek origin"
        require (target >= 0L && target <= entry.Length) "Nested ZIP seek outside entry"
        if target < position then
            source.Dispose()
            source <- entry.Open()
            position <- 0L
        while position < target do
            let n = this.Read(buffer, 0, int (min (int64 buffer.Length) (target - position)))
            require (n > 0) "Truncated nested ZIP"
        position
    override _.Flush() = ()
    override _.SetLength _ = raise (NotSupportedException())
    override _.Write(_, _, _) = raise (NotSupportedException())
    override this.Dispose(disposing) =
        if disposing then source.Dispose()
        base.Dispose(disposing)

let verify repositoryRoot artifactDirectory =
    let root, artifacts = Path.GetFullPath repositoryRoot, Path.GetFullPath artifactDirectory
    require (Directory.Exists root && Directory.Exists artifacts) "Both explicit directories must exist"
    let manifestPath = under root (portable + "locations.json")
    // Freeze the locator policy as well as the objects it names.
    equal "locations.json SHA256" "b5e8754c668b6e3640acca0da12ff7af1baf8a69aea9c7830b95e99281889657"
        (hashFile manifestPath 426742L)
    use manifestDoc = fileJson manifestPath
    let manifest = manifestDoc.RootElement
    equal "manifest schema" "funnysharp-goal24-portable-evidence/v1" (text "schema" manifest)
    equal "frozen product" product (text "frozenProduct" manifest)
    let objects = items "objects" manifest
    equal "catalog objects" 191 objects.Length
    let byPath = Dictionary<string * string, JsonElement>()
    let byId = Dictionary<string, JsonElement>(StringComparer.Ordinal)
    for value in objects do
        require (byId.TryAdd(text "id" value, value)) "Duplicate catalog object ID"
        let hash = if (prop "sha256" value).ValueKind = JsonValueKind.Null then "" else text "sha256" value
        let key = text "path" value, hash
        match byPath.TryGetValue key with
        | true, previous ->
            equal "alias status" (text "status" previous) (text "status" value)
            equal "alias length" ((prop "bytes" previous).GetRawText()) ((prop "bytes" value).GetRawText())
        | _ -> byPath.Add(key, value)
    // Directory anchors have neither bytes nor authority and cannot satisfy pins.
    let directoryAnchors = objects |> Array.filter (fun o -> text "status" o = "directory")
    equal "non-authoritative directory anchors" 2 directoryAnchors.Length
    for anchor in directoryAnchors do
        require ((prop "sha256" anchor).ValueKind = JsonValueKind.Null && (prop "bytes" anchor).ValueKind = JsonValueKind.Null
                 && (items "referencedByRows" anchor).Length = 0) "Directory anchor masquerades as proof"
    let retained = items "retained" manifest
    equal "retained count" 62 retained.Length
    equal "reversible base64 count" 7 (retained |> Array.filter (fun r -> text "encoding" r = "base64") |> Array.length)
    let retainedBytes = Dictionary<string, byte array>(StringComparer.Ordinal)
    for record in retained do
        let relative = text "physicalDestination" record
        require (relative.StartsWith(portable, StringComparison.Ordinal)) "Retained locator outside portable evidence"
        let path = regularFile (under root relative)
        let physical = File.ReadAllBytes path
        equal (relative + " physical SHA256") (text "physicalSha256" record) (hex (SHA256.HashData physical))
        let decoded =
            match text "encoding" record with
            | "utf8" -> utf8.GetString physical |> ignore; physical
            | "base64" -> Convert.FromBase64String(utf8.GetString physical)
            | encoding -> invalidOp ("Unknown retained encoding: " + encoding)
        equal (relative + " decoded length") (number "bytes" record) decoded.LongLength
        equal (relative + " decoded SHA256") (text "sha256" record) (hex (SHA256.HashData decoded))
        require (retainedBytes.TryAdd(text "originalPath" record, decoded)) "Duplicate retained identity"
        if (prop "objectId" record).ValueKind <> JsonValueKind.Null then
            let obj = byId.[text "objectId" record]
            equal "retained object path" (text "path" obj) (text "originalPath" record)
            equal "retained object SHA256" (text "sha256" obj) (text "sha256" record)
            equal "retained object length" (number "bytes" obj) decoded.LongLength
    printfn "RETAINED verified=62 utf8=55 reversibleBase64=7 directoryAnchors=2 nonAuthoritative=true"

    let publication = prop "publishedEvidence" manifest
    equal "publication run" 37210014612L (number "runId" publication)
    equal "publication attempt" 1L (number "attempt" publication)
    let rawObjects = Dictionary<string, string * int64>(StringComparer.Ordinal)
    let openArtifact id name =
        let record = items "artifacts" publication |> Array.filter (fun a -> number "id" a = id) |> Array.exactlyOne
        let path = under artifacts name
        let length = number "bytes" record
        let hash = hashFile path length
        equal (name + " raw SHA256") (text "digest" record) ("sha256:" + hash)
        rawObjects.Add((if id = 11306431210L then "OBJ184" else "OBJ185"), (hash, length))
        printfn "RAW %s bytes=%d digest=%s" name (number "bytes" record) (text "digest" record)
        File.OpenRead(regularFile path)
    use provenanceStream = openArtifact 11306431210L "provenance.zip"
    use indexStream = openArtifact 11306386365L "index.zip"
    use provenanceZip = new ZipArchive(provenanceStream, ZipArchiveMode.Read, true)
    use indexZip = new ZipArchive(indexStream, ZipArchiveMode.Read, true)
    let entries = inventory "provenance.zip" 310 provenanceZip
    let indexEntries = inventory "index.zip" 1 indexZip
    equal "index entry inventory" (Set.singleton "index.json") (Set.ofSeq indexEntries.Keys)
    let verifiedEntries = Dictionary<string, string * int64>(StringComparer.Ordinal)
    let verifyEntry path hash length =
        match verifiedEntries.TryGetValue path with
        | true, previous -> equal (path + " reused exact entry") (hash, length) previous
        | _ ->
            require (entries.ContainsKey path) ("Missing actual bundle entry: " + path)
            equal (path + " SHA256") hash (entryHash entries.[path] length)
            verifiedEntries.Add(path, (hash, length))
    for id, entry in [ "OBJ151", "P.json"; "OBJ152", "A.json"; "OBJ183", "review.md" ] do
        let obj = byId.[id]
        verifyEntry entry (text "sha256" obj) (number "bytes" obj)
    use producerDoc = entryJson entries.["P.json"]
    use attestationDoc = entryJson entries.["A.json"]
    use indexDoc = entryJson indexEntries.["index.json"]
    let producer, attestation, index = producerDoc.RootElement, attestationDoc.RootElement, indexDoc.RootElement
    equal "P kind" "producer" (text "kind" producer)
    equal "A kind" "attestation" (text "kind" attestation)
    equal "I kind" "index" (text "kind" index)
    equal "P candidate" product (text "candidateCommit" (prop "input" producer))
    equal "A candidate" product (text "candidateCommit" attestation)
    equal "I candidate" product (text "candidateCommit" index)
    for node in [ attestation; index ] do equal "P binding" (text "sha256" byId.["OBJ151"]) (text "producerSha256" node)
    equal "I A binding" (text "sha256" byId.["OBJ152"]) (text "attestationSha256" index)
    equal "I entry SHA256" "266fe2363dc2d743d03cc91bb8cba1af0ec463f3d2fc2a9120e8c097594f1489"
        (entryHash indexEntries.["index.json"] indexEntries.["index.json"].Length)
    let attachmentByPath = Dictionary<string, JsonElement>(StringComparer.Ordinal)
    let payloads, bodies = items "evidence" producer, items "http" producer
    equal "P payload count" 258 payloads.Length
    equal "P HTTP body count" 49 bodies.Length
    let mutable totalBytes = 0L
    for isBody, node in Array.append (payloads |> Array.map (fun p -> false, p)) (bodies |> Array.map (fun p -> true, p)) do
        let path, hash = if isBody then text "bodyPath" node, text "bodySha256" node else text "path" node, text "sha256" node
        safeRelative path
        require (attachmentByPath.TryAdd(path, node)) ("Duplicate P attachment: " + path)
        require (entries.ContainsKey path) ("Missing P attachment: " + path)
        let length = entries.[path].Length
        for key in (if isBody then [ "length"; "bodyLength"; "byteLength"; "bodyByteLength"; "size_in_bytes" ] else [ "length"; "byteLength"; "size_in_bytes" ]) do
            if has key node then equal (path + " P " + key) length (number key node)
        verifyEntry path hash length
        totalBytes <- totalBytes + length
    equal "exact P-bound bundle inventory" (Set.ofSeq verifiedEntries.Keys) (Set.ofSeq entries.Keys)
    equal "attachment bytes" 1407188687L totalBytes
    printfn "BUNDLE roots=3 payloads=258 httpBodies=49 attachments=307 allActualBytesMatched=true"

    let verifiedObjects = Dictionary<string * string, string * int64>()
    let resolve path expectedHash =
        let key = path, expectedHash
        require (byPath.ContainsKey key) ("Unknown frozen identity/hash: " + path)
        let obj = byPath.[key]
        let virtualIndex = text "id" obj = "OBJ153" && text "status" obj = "virtual-entry"
        require (text "status" obj = "present" || virtualIndex) ("Non-file identity cannot satisfy proof: " + path)
        let hash = text "sha256" obj
        let length = if virtualIndex then indexEntries.["index.json"].Length else number "bytes" obj
        equal (path + " frozen object pin") hash expectedHash
        match verifiedObjects.TryGetValue key with
        | true, previous -> equal (path + " reused object") (hash, length) previous
        | _ ->
            let mutable matched = false
            if virtualIndex then
                equal "inner index alias SHA256" hash (entryHash indexEntries.["index.json"] length)
                matched <- true
            match retainedBytes.TryGetValue path with
            | true, bytes -> equal "retained proof bytes" hash (hex (SHA256.HashData bytes)); equal "retained proof length" length bytes.LongLength; matched <- true
            | _ -> ()
            if not matched then
                for locator in items "gitMatches" obj do
                    if not matched then
                        let candidate = under root (text "path" locator)
                        if File.Exists candidate then
                            let actual = hashFile candidate (FileInfo(regularFile candidate).Length)
                            if actual = hash then
                                equal "exact Git-current length" length (FileInfo(candidate).Length)
                                matched <- true
            if not matched then
                for locator in items "gitMatches" obj do
                    if not matched then
                        let blob = text "blob" locator
                        require (Regex.IsMatch(blob, "^[0-9a-f]{40}$")) "Invalid frozen Git blob identity"
                        use child = new System.Diagnostics.Process()
                        child.StartInfo.FileName <- "git"
                        child.StartInfo.UseShellExecute <- false
                        child.StartInfo.RedirectStandardOutput <- true
                        child.StartInfo.RedirectStandardError <- true
                        for argument in [ "-C"; root; "cat-file"; "blob"; blob ] do
                            child.StartInfo.ArgumentList.Add argument
                        require (child.Start()) "Could not read frozen Git blob"
                        let actual = hashStream ("Git blob " + blob) length child.StandardOutput.BaseStream
                        let error = child.StandardError.ReadToEnd()
                        require (child.WaitForExit(30000)) "Frozen Git blob read did not finish"
                        require (child.ExitCode = 0) ("Frozen Git blob unavailable: " + blob + " " + error)
                        equal "frozen Git blob SHA256" hash actual
                        matched <- true
            if not matched then
                for attachment in items "pAttachment" obj do
                    let entry = text "path" attachment
                    equal "catalog P attachment SHA256" hash (text "sha256" attachment)
                    equal "catalog P attachment length" length (number "length" attachment)
                    require (attachmentByPath.ContainsKey entry) ("Catalog path not bound by actual P: " + entry)
                    verifyEntry entry hash length
                    matched <- true
            if not matched then
                match text "id" obj with
                | "OBJ151" -> verifyEntry "P.json" hash length; matched <- true
                | "OBJ152" -> verifyEntry "A.json" hash length; matched <- true
                | "OBJ183" -> verifyEntry "review.md" hash length; matched <- true
                | "OBJ184" | "OBJ185" -> equal "actual raw archive object" (hash, length) rawObjects.[text "id" obj]; matched <- true
                | _ -> ()
            if not matched then
                let nested = items "canonicalNestedEntries" manifest |> Array.filter (fun n -> text "objectId" n = text "id" obj)
                for locator in nested do
                    equal "nested outer artifact" 11306431210L (number "outerArtifactId" locator)
                    let outer = text "outerEntry" locator
                    require (attachmentByPath.ContainsKey outer) "Nested archive is not bound by P"
                    verifyEntry outer (text "httpBodySha256" locator) entries.[outer].Length
                    use source = new EntrySeekStream(entries.[outer])
                    use archive = new ZipArchive(source, ZipArchiveMode.Read, true)
                    let inner = text "innerEntry" locator
                    safeRelative inner
                    let found = archive.Entries |> Seq.filter (fun e -> String.Equals(e.FullName, inner, StringComparison.OrdinalIgnoreCase)) |> Seq.toArray
                    require (found.Length = 1 && found.[0].FullName = inner) ("Missing/aliased nested entry: " + inner)
                    equal "nested locator hash" hash (text "sha256" locator)
                    equal "nested locator length" length (number "bytes" locator)
                    equal (inner + " nested SHA256") hash (entryHash found.[0] length)
                    matched <- true
            require matched ("Missing exact retained/Git-current/published bytes: " + path)
            verifiedObjects.Add(key, (hash, length))

    let ledgerPath = under root (audit + "findings.json")
    equal "current source ledger SHA256" (text "sourceLedgerSha256" manifest) (hashFile ledgerPath 950835L)
    use ledgerDoc = fileJson ledgerPath
    let ledger = ledgerDoc.RootElement
    let csvPath = under root (audit + "original-criterion-matrix.csv")
    equal "original CSV SHA256" (text "originalMatrixSha256" manifest) (hashFile csvPath 124071L)
    use parser = new TextFieldParser(csvPath)
    parser.TextFieldType <- FieldType.Delimited
    parser.SetDelimiters(",")
    parser.HasFieldsEnclosedInQuotes <- true
    parser.TrimWhiteSpace <- false
    let headers = parser.ReadFields()
    let column name = Array.findIndex ((=) name) headers
    let csvRows = ResizeArray<string array>()
    while not parser.EndOfData do csvRows.Add(parser.ReadFields())
    equal "original CSV rows" 311 csvRows.Count
    let originals = csvRows |> Seq.filter (fun r -> List.contains r.[column "verdict"] [ "FAIL"; "UNVERIFIED" ]) |> Seq.toArray
    equal "original FAIL" 40 (originals |> Array.filter (fun r -> r.[column "verdict"] = "FAIL") |> Array.length)
    equal "original UNVERIFIED" 29 (originals |> Array.filter (fun r -> r.[column "verdict"] = "UNVERIFIED") |> Array.length)
    let originalById = originals |> Array.map (fun r -> r.[column "criterion_id"], r) |> Map.ofArray
    equal "original unique IDs" 69 originalById.Count
    let proposalObject = byId.["OBJ001"]
    resolve (text "path" proposalObject) (text "sha256" proposalObject)
    use proposalDoc = bytesJson retainedBytes.[text "path" proposalObject]
    let proposalByTracking = items "rows" proposalDoc.RootElement |> Array.map (fun r -> text "tracking" r, text "criterionId" r) |> Map.ofArray
    let rows = items "findings" ledger
    equal "current rows" 69 rows.Length
    let expectedTracking = [ for n in 1..40 -> sprintf "F%02d" n ] @ [ for n in 1..29 -> sprintf "U%02d" n ] |> Set.ofList
    equal "current tracking identity set" expectedTracking (rows |> Array.map (text "tracking") |> Set.ofArray)
    equal "current criterion identity set" (Set.ofSeq originalById.Keys) (rows |> Array.map (text "criterionId") |> Set.ofArray)
    let rowPins = ResizeArray<string * string>()
    for row in rows do
        let tracking, id = text "tracking" row, text "criterionId" row
        equal (tracking + " proposal identity") proposalByTracking.[tracking] id
        let original = originalById.[id]
        equal (tracking + " criterion") original.[column "criterion"] (text "criterion" row)
        equal (tracking + " original verdict") original.[column "verdict"] (text "originalVerdict" row)
        require (flag "finalAccepted" row && (text "resolutionStatus" row).StartsWith("RESOLVED", StringComparison.Ordinal)) ("Unaccepted/open row: " + tracking)
        equal (tracking + " product") product (text "productCandidate" (prop "finalCandidateBinding" row))
        let evidence = items "evidence" (prop "finalResolution" row)
        require (evidence.Length > 0) ("Empty current evidence: " + tracking)
        for pin in evidence do
            let path, hash = text "path" pin, text "sha256" pin
            if has "bytes" pin then equal "row evidence length" (number "bytes" byPath.[(path, hash)]) (number "bytes" pin)
            resolve path hash
            rowPins.Add(path, hash)
    equal "current row pin entries" 998 rowPins.Count
    equal "distinct current row pins" 87 (rowPins |> Set.ofSeq |> Set.count)
    let reconciliation = prop "finalReconciliation" ledger
    require (flag "wholeGoalAccepted" reconciliation && flag "productOrReleasePASS" reconciliation) "Final aggregate acceptance false"
    equal "aggregate accepted" 69L (number "acceptedVerified" reconciliation)
    equal "aggregate open" 0L (number "open" reconciliation)
    equal "aggregate product" product (text "candidate" (prop "currentProduct" reconciliation))
    let gateBinding = prop "independentAggregateGateReview" reconciliation
    require (text "verdict" gateBinding = "APPROVE" && flag "wholeGoalApproval" gateBinding) "Missing whole-goal approval"
    resolve (text "path" gateBinding) (text "sha256" gateBinding)
    use gateDoc = bytesJson retainedBytes.[text "path" gateBinding]
    let gate = gateDoc.RootElement
    equal "native gate schema" "funnysharp-final-goal24-independent-gate/v1" (text "schema" gate)
    equal "native gate recommendation" "APPROVE" (text "recommendation" gate)
    equal "native gate reviewer role" "omo-native-gate-reviewer" (text "role" (prop "reviewer" gate))
    equal "native gate product" product (text "productCommit" (prop "identity" gate))
    equal "native gate blockers" 0 (items "blockers" gate).Length
    for key, value in [ "originalRows", 69L; "originalFAIL", 40L; "originalUNVERIFIED", 29L; "acceptedVerified", 69L; "open", 0L; "rejectedDispositions", 0L ] do
        equal ("native gate " + key) value (number key (prop "counts" gate))
    let dispositions = items "rowDispositions" gate
    equal "native gate dispositions" 69 dispositions.Length
    equal "native gate identity set" expectedTracking (dispositions |> Array.map (text "tracking") |> Set.ofArray)
    for disposition in dispositions do
        let tracking = text "tracking" disposition
        equal "native disposition" "ACCEPTED_VERIFIED" (text "disposition" disposition)
        equal "native criterion identity" proposalByTracking.[tracking] (text "criterionId" disposition)
        equal "native original verdict" originalById.[text "criterionId" disposition].[column "verdict"] (text "originalVerdict" disposition)
    printfn "EXACT69 rows=69 originalFAIL=40 originalUNVERIFIED=29 accepted=69 open=0 nativeWholeGoal=APPROVE currentProduct=%s rowPins=998 distinctRowPins=87" product

    let indexPins = items "indexPins" manifest
    equal "manifest final-index pins" 120 indexPins.Length
    let finalIndex = File.ReadAllText(regularFile (under root (audit + "final-evidence-index.md")), utf8)
    let parsedPins = Regex.Matches(finalIndex, "(?m)^\\| `([^`]+)` \\| `([a-f0-9]{64})` \\| ([^\\r\\n]+?) \\|\\r?$")
                     |> Seq.map (fun m -> m.Groups.[1].Value, m.Groups.[2].Value, m.Groups.[3].Value) |> Seq.toArray
    equal "actual final-index pin count" 120 parsedPins.Length
    equal "actual final-index exact pin triples"
        (indexPins |> Array.map (fun p -> text "path" p, text "sha256" p, text "role" p) |> Set.ofArray)
        (Set.ofArray parsedPins)
    for pin in indexPins do
        let obj = byId.[text "objectId" pin]
        equal "index catalog object identity" (text "path" obj) (text "path" pin)
        resolve (text "path" pin) (text "sha256" pin)
    // The historical U28 pin is not the current successor report's SHA256.
    equal "U28 current successor hash" "1bdae18fbbc4ff2e9a2a77ef4f62d8d8e74d69975defb772d04fbff5e7030018" (text "sha256" byId.["OBJ146"])
    equal "U28 original retained hash" "884a9707ca18f5edfd6e079e9bc357bac0b095398d0fe8248dbc058dfe6d6cdf" (text "sha256" byId.["OBJ147"])
    printfn "PORTABLE_EVIDENCE_PASS retained=62 originalIds=69 rowPins=998 distinctRowPins=87 finalIndexPins=120 actualBundle=true writes=0 networkCalls=0"

let usage () =
    printfn "Usage: dotnet fsi --warnaserror+ verify.fsx -- --repository-root <checkout> --artifact-directory <directory-containing-provenance.zip-and-index.zip>"
    printfn "Offline, read-only, fail-closed. --help compiles the CLI but does not verify evidence."
let args = fsi.CommandLineArgs |> Array.skip 1
let result =
    match args with
    | [| "--help" |] -> usage(); 0
    | [| "--repository-root"; root; "--artifact-directory"; artifacts |]
    | [| "--artifact-directory"; artifacts; "--repository-root"; root |] ->
        try verify root artifacts; 0
        with error -> eprintfn "PORTABLE_EVIDENCE_FAIL %s: %s" (error.GetType().Name) error.Message; 1
    | _ -> usage(); 2
exit result
