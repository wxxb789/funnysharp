// Read-only, offline verification of the specific actual publication retrieval.
open System
open System.Collections.Generic
open System.Globalization
open System.IO
open System.IO.Compression
open System.Security.Cryptography
open System.Text.Json
open System.Text.RegularExpressions

let require condition message = if not condition then invalidOp message
let prop (name: string) (node: JsonElement) = node.GetProperty name
let text name node = (prop name node).GetString() |> Option.ofObj |> Option.defaultValue ""
let number name node = (prop name node).GetInt64()
let flag name node = (prop name node).GetBoolean()
let items name node = (prop name node).EnumerateArray() |> Seq.toList
let has (name: string) (node: JsonElement) = match node.TryGetProperty name with true, _ -> true | _ -> false
let time name node = DateTimeOffset.Parse(text name node, CultureInfo.InvariantCulture)
let equal label expected actual = require (expected = actual) (sprintf "%s: expected %A; actual %A" label expected actual)
let hex (bytes: byte array) = Convert.ToHexString(bytes).ToLowerInvariant()
let digest name node =
    let value = text name node
    require (Regex.IsMatch(value, "^[a-f0-9]{64}$")) ("Invalid SHA256 field: " + name)
    value
let rec uniqueJson (node: JsonElement) =
    match node.ValueKind with
    | JsonValueKind.Object ->
        let names = HashSet<string>(StringComparer.Ordinal)
        for field in node.EnumerateObject() do
            require (names.Add field.Name) ("Duplicate JSON property: " + field.Name)
            uniqueJson field.Value
    | JsonValueKind.Array -> for value in node.EnumerateArray() do uniqueJson value
    | _ -> ()
let fields expected (node: JsonElement) =
    let actual = node.EnumerateObject() |> Seq.map (fun field -> field.Name) |> Set.ofSeq
    equal "JSON fields" (Set.ofList expected) actual
let regularFile (path: string) =
    let full = Path.GetFullPath path
    require (File.Exists full) ("Missing regular file: " + full)
    let rec check (current: string) =
        require ((File.GetAttributes current &&& FileAttributes.ReparsePoint) = enum 0) ("Reparse point: " + current)
        match Directory.GetParent current with null -> () | parent -> check parent.FullName
    check full
    full
let hashStream label limit (source: Stream) =
    use hash = IncrementalHash.CreateHash HashAlgorithmName.SHA256
    let buffer = Array.zeroCreate<byte> 65536
    let mutable length = 0L
    let mutable count = source.Read(buffer, 0, buffer.Length)
    while count > 0 do
        require (int64 count <= limit - length) (label + ": byte limit exceeded")
        hash.AppendData(buffer, 0, count)
        length <- length + int64 count
        count <- source.Read(buffer, 0, buffer.Length)
    length, hex (hash.GetHashAndReset())
let parseStream label limit length (source: Stream) =
    require (length >= 0L && length <= limit) (label + ": JSON byte limit exceeded")
    // Only structured JSON is materialized, never ZIP or attachment payloads.
    let document = JsonDocument.Parse(source, JsonDocumentOptions(MaxDepth = 64))
    try uniqueJson document.RootElement; document
    with _ -> document.Dispose(); reraise()
let readJson path limit =
    use source = new FileStream(regularFile path, FileMode.Open, FileAccess.Read, FileShare.Read)
    parseStream path limit source.Length source
let safePath (path: string) =
    require (path.Length > 0 && path.Length <= 260 && not (path.Contains '\\')
             && not (path |> Seq.exists Char.IsControl)) ("Unsafe ZIP path: " + path)
    for segment in path.Split '/' do
        require (Regex.IsMatch(segment, "^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$")
                 && segment <> "." && segment <> ".." && not (segment.EndsWith ".")
                 && not (Regex.IsMatch(segment, "^(CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9])([.]|$)", RegexOptions.IgnoreCase)))
            ("Unsafe ZIP path segment: " + path)
let inventory label maxEntries (archive: ZipArchive) =
    require (archive.Entries.Count <= maxEntries) (label + ": too many ZIP entries")
    let entries = Dictionary<string, ZipArchiveEntry>(StringComparer.Ordinal)
    let aliases = HashSet<string>(StringComparer.OrdinalIgnoreCase)
    for entry in archive.Entries do
        safePath entry.FullName
        require (aliases.Add entry.FullName) (label + ": duplicate or case-alias ZIP entry: " + entry.FullName)
        // Regular files only: reject Unix symlinks and directory attribute bits.
        let attributes = uint32 entry.ExternalAttributes
        let unixType = (attributes >>> 16) &&& 0xF000u
        require ((attributes &&& 0x10u) = 0u && (unixType = 0u || unixType = 0x8000u))
            (label + ": non-regular ZIP entry: " + entry.FullName)
        entries.Add(entry.FullName, entry)
    entries
let entryHash (entry: ZipArchiveEntry) limit =
    require (entry.Length >= 0L && entry.Length <= limit) (entry.FullName + ": ZIP entry byte limit exceeded")
    use source = entry.Open()
    let length, hash = hashStream entry.FullName limit source
    equal (entry.FullName + " ZIP length") entry.Length length
    length, hash
let entryJson (entry: ZipArchiveEntry) limit =
    use source = entry.Open()
    parseStream entry.FullName limit entry.Length source

let repository = "wxxb789/funnysharp"
let repositoryId = 1351541235L
let publicationRunId = 37210014612L
let publicationHead = "e7aea9995bac6c87a69db14a8b04c6925cb45d4c"
let publicationBranch = "audit/goal24-stage-bundle"
let candidate = "d8744c934d86833f9817245ecc7c78b1177b8b76"
let producerHash = "51d55b6d1d328c3a0caa69f2ec42ba03b7fdd37e3c6475f8dcefaa2435897d72"
let attestationHash = "6ef82eafbf03e00ef3191f9a5541b9280ec3962ae20bf9953dd8b72d942165a5"
let reportHash = "1ce284acac64c06768a7c6b5dea0bf698581b0dd6404d88d5520e598460326b5"
let reviewerIdentity = "omo-senpi-task:st_01a10644:session:01a10644-f1d8-7caa-920f-5ea255f09db3"
let api = "https://api.github.com/repos/" + repository
let runUrl = sprintf "https://github.com/%s/actions/runs/%d" repository publicationRunId
let mib = 1024L * 1024L
let maxPayload = 2048L * mib

let checkRun (run: JsonElement) =
    equal "publication run ID" publicationRunId (number "id" run)
    equal "publication run attempt" 1L (number "run_attempt" run)
    for key, value in [ "name", "release"; "path", ".github/workflows/release.yml";
                        "head_sha", publicationHead; "head_branch", publicationBranch;
                        "event", "workflow_dispatch"; "html_url", runUrl;
                        "url", api + sprintf "/actions/runs/%d" publicationRunId ] do
        equal ("publication run " + key) value (text key run)
    equal "workflow ID" 350183365L (number "workflow_id" run)
    for name in [ "repository"; "head_repository" ] do
        let repo = prop name run
        equal (name + " ID") repositoryId (number "id" repo)
        equal (name + " full_name") repository (text "full_name" repo)
let checkArtifact id name (artifact: JsonElement) =
    equal "artifact ID" id (number "id" artifact)
    equal "artifact name" name (text "name" artifact)
    require (not (flag "expired" artifact)) (name + ": expired")
    require (time "expires_at" artifact > DateTimeOffset.UtcNow) (name + ": retention expired")
    equal "artifact API URL" (api + sprintf "/actions/artifacts/%d" id) (text "url" artifact)
    equal "artifact download URL" (api + sprintf "/actions/artifacts/%d/zip" id) (text "archive_download_url" artifact)
    let run = prop "workflow_run" artifact
    equal "artifact workflow run ID" publicationRunId (number "id" run)
    equal "artifact repository ID" repositoryId (number "repository_id" run)
    equal "artifact head repository ID" repositoryId (number "head_repository_id" run)
    equal "artifact publication head" publicationHead (text "head_sha" run)
    equal "artifact publication branch" publicationBranch (text "head_branch" run)

let verify (metadataPath: string) (retrievalDirectory: string) =
    let root = Path.GetDirectoryName(Path.GetFullPath metadataPath)
    let retrieval = Path.GetFullPath retrievalDirectory
    use metadataDoc = readJson metadataPath (2L * mib)
    use dispatchDoc = readJson (Path.Combine(root, "stage-bundle", "actual-publication-dispatch.json")) mib
    use retrievalDoc = readJson (Path.Combine(retrieval, "raw-retrieval.json")) mib
    let metadata, dispatch = metadataDoc.RootElement, dispatchDoc.RootElement
    let run = prop "run" metadata
    checkRun run
    equal "current publication status" "completed" (text "status" run)
    equal "current publication conclusion" "success" (text "conclusion" run)
    equal "dispatch schema" "funnysharp-actual-publication-dispatch/v1" (text "schema" dispatch)
    equal "dispatch repository" repository (text "repository" dispatch)
    equal "dispatch branch" publicationBranch (text "branch" dispatch)
    equal "dispatch code commit" publicationHead (text "codeCommit" dispatch)
    equal "dispatch mode" "stage-attestation" (text "mode" dispatch)
    let selected = prop "selectedRun" dispatch
    equal "selected run ID" publicationRunId (number "databaseId" selected)
    equal "selected run head" publicationHead (text "headSha" selected)
    equal "selected run URL" runUrl (text "url" selected)
    equal "selected run creation" (time "created_at" run) (time "createdAt" selected)
    require (time "created_at" run > time "dispatchedAfterUtc" dispatch) "Publication run predates dispatch"
    let retrievedAt = time "retrievedAtUtc" metadata
    require (retrievedAt >= time "updated_at" run) "Current metadata retrieval predates completed publication"
    let artifactsPage = prop "artifacts" metadata
    let artifacts = items "artifacts" artifactsPage
    equal "artifact API total_count" 2L (number "total_count" artifactsPage)
    equal "artifact inventory count" 2 artifacts.Length
    let findArtifact id = artifacts |> List.filter (fun artifact -> number "id" artifact = id) |> List.exactlyOne
    let provenance = findArtifact 11306431210L
    let indexArtifact = findArtifact 11306386365L
    checkArtifact 11306431210L "release-provenance-stage-attestation-37210014612-1" provenance
    checkArtifact 11306386365L "release-index-37210014612-1" indexArtifact
    let records = retrievalDoc.RootElement.EnumerateArray() |> Seq.toList
    equal "raw retrieval record count" 2 records.Length
    let openArchive name (artifact: JsonElement) =
        let path = regularFile (Path.Combine(retrieval, name))
        let source = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)
        try
            let limit = if name = "index.zip" then 2L * mib else maxPayload
            require (source.Length <= limit) (name + ": archive byte limit exceeded")
            let length, hash = hashStream name limit source
            equal (name + " API length") (number "size_in_bytes" artifact) length
            equal (name + " API digest") (text "digest" artifact) ("sha256:" + hash)
            let record = records |> List.filter (fun value -> number "artifactId" value = number "id" artifact) |> List.exactlyOne
            equal (name + " retrieval name") (text "name" artifact) (text "name" record)
            equal (name + " retrieval path") path (Path.GetFullPath(text "path" record))
            equal (name + " retrieval length") length (number "bytes" record)
            equal (name + " retrieval SHA256") hash (digest "sha256" record)
            require (flag "metadataDigestMatched" record) (name + ": retrieval digest receipt is false")
            printfn "RAW %s bytes=%d sha256=%s artifactId=%d" name length hash (number "id" artifact)
            source.Position <- 0L
            source, hash
        with _ -> source.Dispose(); reraise()
    let provenanceStream, provenanceZipHash = openArchive "provenance.zip" provenance
    use _provenanceStream = provenanceStream
    let indexStream, _indexZipHash = openArchive "index.zip" indexArtifact
    use _indexStream = indexStream
    use provenanceZip = new ZipArchive(provenanceStream, ZipArchiveMode.Read, true)
    use indexZip = new ZipArchive(indexStream, ZipArchiveMode.Read, true)
    let provenanceEntries = inventory "provenance.zip" 310 provenanceZip
    let indexEntries = inventory "index.zip" 1 indexZip
    equal "index ZIP inventory" (Set.singleton "index.json") (Set.ofSeq indexEntries.Keys)
    let _, indexHash = entryHash indexEntries.["index.json"] (2L * mib)
    use indexDoc = entryJson indexEntries.["index.json"] (2L * mib)
    let index = indexDoc.RootElement
    fields [ "schemaVersion"; "kind"; "createdAtUtc"; "producerSha256"; "attestationSha256";
             "artifactUrl"; "artifact"; "publicationRun"; "http"; "candidateCommit" ] index
    equal "I schemaVersion" 1L (number "schemaVersion" index)
    equal "I kind" "index" (text "kind" index)
    equal "I candidate" candidate (text "candidateCommit" index)
    equal "I P SHA256" producerHash (digest "producerSha256" index)
    equal "I A SHA256" attestationHash (digest "attestationSha256" index)
    equal "I artifact URL" (runUrl + "/artifacts/11306431210") (text "artifactUrl" index)
    let embeddedArtifact = prop "artifact" index
    checkArtifact 11306431210L (text "name" provenance) embeddedArtifact
    for key in [ "id"; "size_in_bytes" ] do equal ("I artifact " + key) (number key provenance) (number key embeddedArtifact)
    for key in [ "name"; "node_id"; "url"; "archive_download_url"; "digest" ] do
        equal ("I artifact " + key) (text key provenance) (text key embeddedArtifact)
    for key in [ "created_at"; "expires_at" ] do equal ("I artifact " + key) (time key provenance) (time key embeddedArtifact)
    let embeddedRun = prop "publicationRun" index
    checkRun embeddedRun
    equal "I publication run number" (number "run_number" run) (number "run_number" embeddedRun)
    equal "I publication run creation" (time "created_at" run) (time "created_at" embeddedRun)
    // status, conclusion, updated_at and jobs URLs are historical, mutable fields.
    // I legitimately captured this run before it completed; never compare them.
    let indexCreated = time "createdAtUtc" index
    // GitHub artifact timestamps have whole-second precision; I retains fractions.
    require (indexCreated >= time "created_at" provenance
             && indexCreated.ToUnixTimeSeconds() <= (time "created_at" indexArtifact).ToUnixTimeSeconds()
             && indexCreated <= retrievedAt) "I creation is outside actual publication chronology"
    let captures = items "http" index
    equal "I HTTP capture count" 3 captures.Length
    let captureUrls = [ text "url" provenance; text "archive_download_url" provenance; text "url" run ]
    equal "I HTTP URL inventory" (Set.ofList captureUrls) (captures |> List.map (text "url") |> Set.ofList)
    for capture in captures do
        fields [ "url"; "effectiveUrl"; "statusCode"; "bodySha256"; "observedAtUtc" ] capture
        equal "I HTTP status" 200L (number "statusCode" capture)
        digest "bodySha256" capture |> ignore
        require ((Uri(text "effectiveUrl" capture)).Scheme = "https") "I effective capture URL is not HTTPS"
        require (time "observedAtUtc" capture >= time "created_at" provenance
                 && time "observedAtUtc" capture <= indexCreated) "I HTTP capture lies outside publication chronology"
        if text "url" capture = text "archive_download_url" provenance then
            equal "I captured raw ZIP SHA256" provenanceZipHash (digest "bodySha256" capture)
    for name in [ "P.json"; "A.json"; "review.md" ] do
        require (provenanceEntries.ContainsKey name) ("Missing ZIP entry: " + name)
    let pLength, pHash = entryHash provenanceEntries.["P.json"] (80L * mib)
    equal "P length" 67642132L pLength
    equal "P SHA256" producerHash pHash
    let aLength, aHash = entryHash provenanceEntries.["A.json"] mib
    equal "A length" 58112L aLength
    equal "A SHA256" attestationHash aHash
    let reportLength, actualReportHash = entryHash provenanceEntries.["review.md"] mib
    equal "review.md SHA256" reportHash actualReportHash
    use producerDoc = entryJson provenanceEntries.["P.json"] (80L * mib)
    use attestationDoc = entryJson provenanceEntries.["A.json"] mib
    let producer, attestation = producerDoc.RootElement, attestationDoc.RootElement
    equal "P schemaVersion" 1L (number "schemaVersion" producer)
    equal "P kind" "producer" (text "kind" producer)
    equal "A schemaVersion" 1L (number "schemaVersion" attestation)
    equal "A kind" "attestation" (text "kind" attestation)
    let input = prop "input" producer
    equal "P repository" repository (text "repository" input)
    equal "P candidate" candidate (text "candidateCommit" input)
    equal "A candidate" candidate (text "candidateCommit" attestation)
    equal "A P binding" producerHash (digest "producerSha256" attestation)
    require (time "createdAtUtc" attestation > time "createdAtUtc" producer
             && indexCreated > time "createdAtUtc" attestation
             && time "created_at" run > time "createdAtUtc" attestation) "P/A/I chronological binding failed"
    let reviewer = prop "reviewer" attestation
    equal "A reviewer name" "senpi-task child st_01a10644" (text "name" reviewer)
    equal "A reviewer identity" reviewerIdentity (text "identity" reviewer)
    require (flag "readOnly" reviewer && reviewerIdentity <> text "producerIdentity" input) "A reviewer is not independently read-only"
    for key, expected in [ "verdict", "APPROVE"; "auditStatus", "COMPLETE"; "productAcceptance", "PASS" ] do
        equal ("existing A " + key) expected (text key attestation)
    let evidence, responses = items "evidence" producer, items "http" producer
    equal "P evidence count" 258 evidence.Length
    equal "P HTTP body count" 49 responses.Length
    let evidenceById = Dictionary<string, JsonElement>(StringComparer.Ordinal)
    let expectedEntries = HashSet<string>([ "P.json"; "A.json"; "review.md" ], StringComparer.Ordinal)
    let aliases = HashSet<string>(expectedEntries, StringComparer.OrdinalIgnoreCase)
    let attachments =
        [ for entry in evidence do
            let id = text "id" entry
            require (id.Length > 0 && evidenceById.TryAdd(id, entry)) ("Duplicate/empty P evidence ID: " + id)
            yield text "path" entry, digest "sha256" entry, entry, [ "length"; "byteLength"; "size_in_bytes" ]
          for response in responses do
            yield text "bodyPath" response, digest "bodySha256" response, response,
                  [ "length"; "bodyLength"; "byteLength"; "bodyByteLength"; "size_in_bytes" ] ]
    for path, _, _, _ in attachments do
        safePath path
        require (Regex.IsMatch(path, "^(payload/[A-Za-z0-9][A-Za-z0-9._-]{0,127}|http/[0-9]+[.]body)$")) ("Invalid P attachment path: " + path)
        require (expectedEntries.Add path && aliases.Add path) ("Duplicate or case-alias P attachment: " + path)
    equal "provenance ZIP exact inventory" (Set.ofSeq expectedEntries) (Set.ofSeq provenanceEntries.Keys)
    let mutable attachmentBytes = 0L
    for path, hash, entry, lengthFields in attachments do
        let length, actualHash = entryHash provenanceEntries.[path] (maxPayload - attachmentBytes)
        equal (path + " SHA256") hash actualHash
        for key in lengthFields do if has key entry then equal (path + " P " + key) (number key entry) length
        attachmentBytes <- attachmentBytes + length
    equal "attachment count" 307 attachments.Length
    equal "attachment bytes" 1407188687L attachmentBytes
    let contracts = items "contracts" attestation
    let originalContracts = items "contracts" input
    equal "A contract count" 13 contracts.Length
    equal "P contract count" 13 originalContracts.Length
    let goalSet values = values |> List.map (number "goal") |> Set.ofList
    equal "A contract goal inventory" (Set.ofList [ 1L .. 13L ]) (goalSet contracts)
    equal "P contract goal inventory" (Set.ofList [ 1L .. 13L ]) (goalSet originalContracts)
    for contract in contracts do
        let original = originalContracts |> List.filter (fun c -> number "goal" c = number "goal" contract) |> List.exactlyOne
        let id = text "evidenceId" original
        require (evidenceById.ContainsKey id) ("P contract evidence ID missing: " + id)
        equal (sprintf "A goal %d contract SHA256" (number "goal" contract)) (digest "sha256" evidenceById.[id]) (digest "sha256" contract)
    let goals = items "goals" attestation
    equal "A goal count" 13 goals.Length
    equal "A goal inventory" (Set.ofList [ 1L .. 13L ]) (goalSet goals)
    let checkIds name node =
        let ids = items name node
        require (not ids.IsEmpty) ("A has empty " + name)
        for id in ids do
            let value = id.GetString() |> Option.ofObj |> Option.defaultValue ""
            require (evidenceById.ContainsKey value) ("A evidence ID does not bind P: " + value)
    for goal in goals do
        equal "existing A goal status" "PASS" (text "status" goal)
        checkIds "evidenceIds" goal
    let embeddedBytes name node =
        let value = prop name node
        let bytes = Convert.FromBase64String(text "base64" value)
        require (bytes.Length > 0 && int64 bytes.Length <= mib) ("Embedded A " + name + " byte limit exceeded")
        equal ("embedded A " + name + " SHA256") (digest "sha256" value) (hex (SHA256.HashData bytes))
        bytes
    let report = embeddedBytes "report" attestation
    equal "A embedded report SHA256" reportHash (hex (SHA256.HashData report))
    equal "A embedded report length" reportLength report.LongLength
    let replays = items "replays" attestation
    equal "A replay count" 2 replays.Length
    equal "A replay goal inventory" (Set.ofList [ 4L; 9L ]) (goalSet replays)
    for replay in replays do
        checkIds "inputEvidenceIds" replay
        let bytes = embeddedBytes "receipt" replay
        use source = new MemoryStream(bytes, false)
        use receiptDoc = parseStream "A replay receipt" mib bytes.LongLength source
        let receipt = receiptDoc.RootElement
        equal "A replay goal" (number "goal" replay) (number "goal" receipt)
        equal "A replay exitCode" 0L (number "exitCode" receipt)
        equal "A replay P binding" producerHash (digest "producerSha256" receipt)
        equal "A replay reviewer binding" reviewerIdentity (text "reviewerIdentity" receipt)
        require (not (String.IsNullOrWhiteSpace(text "command" receipt))
                 && time "startedAtUtc" receipt > time "createdAtUtc" producer
                 && time "finishedAtUtc" receipt >= time "startedAtUtc" receipt
                 && time "finishedAtUtc" receipt <= time "createdAtUtc" attestation) "A replay chronology or command binding failed"
    printfn "IDENTITY repository=%s run=%d attempt=1 event=workflow_dispatch publicationHead=%s candidate=%s" repository publicationRunId publicationHead candidate
    printfn "INDEX entries=%d sha256=%s artifactUrl=%s createdAtUtc=%s" indexEntries.Count indexHash (text "artifactUrl" index) (text "createdAtUtc" index)
    printfn "MANIFEST P.bytes=%d P.sha256=%s A.bytes=%d A.sha256=%s review.bytes=%d review.sha256=%s" pLength pHash aLength aHash reportLength actualReportHash
    printfn "INVENTORY files=%d evidence=%d httpBodies=%d attachments=%d attachmentBytes=%d contracts=%d reviewer=%s" provenanceEntries.Count evidence.Length responses.Length attachments.Length attachmentBytes contracts.Length reviewerIdentity
    printfn "VERIFIED actual raw ZIP digests, exact inventory, all attachment bytes, and I/P/A bindings; no artifacts written."

let usage () =
    printfn "Usage: dotnet fsi --warnaserror+ verify-publication-retrieval.fsx -- <actual-publication-metadata.json> <retrieval-directory>"
    printfn "Read-only and offline. Retrieval directory must contain provenance.zip, index.zip, and raw-retrieval.json."
    printfn "The metadata sibling stage-bundle/actual-publication-dispatch.json is also required."

let arguments = fsi.CommandLineArgs |> Array.skip 1
let exitCode =
    match arguments with
    | [| "--help" |] -> usage(); 0
    | [| metadata; retrieval |] ->
        try verify metadata retrieval; 0
        with error -> eprintfn "Publication retrieval verification failed: %s: %s" (error.GetType().Name) error.Message; 1
    | _ -> usage(); 2
exit exitCode
