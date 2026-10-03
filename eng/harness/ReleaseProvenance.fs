module FunnySharp.Harness.ReleaseProvenance

// P is a frozen aggregate, A is supplied by an independent reviewer, and only I
// names A's digest. This module only reads remote state; transport owns uploads.
open System
open System.Collections.Generic
open System.IO
open System.IO.Compression
open System.Net.Http
open System.Net.Http.Headers
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open System.Text.RegularExpressions
open System.Threading.Tasks
open System.Xml.Linq

type HttpResponse =
    { StatusCode: int
      EffectiveUrl: string
      Body: byte array }

type Collaborators =
    { Get: string -> Task<HttpResponse>
      UtcNow: unit -> DateTimeOffset
      Environment: string -> string option }

let sha256 (bytes: byte array) = Convert.ToHexString(SHA256.HashData bytes).ToLowerInvariant()
let private require condition message = if not condition then invalidOp message
let private parse (bytes: byte array) =
    use document = JsonDocument.Parse(ReadOnlyMemory bytes)
    document.RootElement.Clone()
let private prop name (node: JsonElement) = node.GetProperty(name: string)
let private text name node = (prop name node).GetString() |> Option.ofObj |> Option.defaultValue ""
let private nonempty name node =
    let value = text name node
    require (not (String.IsNullOrWhiteSpace value)) ("Missing " + name)
    value
let private number name node = (prop name node).GetInt64()
let private items name node = (prop name node).EnumerateArray() |> Seq.toList
let private flag name node = (prop name node).GetBoolean()
let private time name node = DateTimeOffset.Parse(nonempty name node, Globalization.CultureInfo.InvariantCulture)
let private has name (node: JsonElement) = match node.TryGetProperty(name: string) with | true, _ -> true | _ -> false
let private encode value = JsonSerializer.SerializeToUtf8Bytes(value, JsonSerializerOptions(WriteIndented = true))
let private obj (fields: (string * objnull) list) = dict fields
let private digest name node =
    let value = nonempty name node
    require (Regex.IsMatch(value, "^[a-f0-9]{64}$")) ("Invalid SHA256: " + name)
    value
let private commit name node =
    let value = nonempty name node
    require (Regex.IsMatch(value, "^[a-f0-9]{40}$")) ("Invalid commit: " + name)
    value
let private exactFields allowed (node: JsonElement) =
    require (node.ValueKind = JsonValueKind.Object) "Expected JSON object."
    let names = node.EnumerateObject() |> Seq.map (fun p -> p.Name) |> Seq.toList
    require (names.Length = (Set.ofList names).Count) "Duplicate JSON fields."
    require (names |> List.forall (fun name -> List.contains name allowed)) "Unexpected field or later-object reference."
let private safeName (value: string) =
    require (Regex.IsMatch(value, "^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$")) "Invalid evidence identifier."
    value
let private sameSet expected actual =
    require (List.length actual = Set.count (Set.ofList actual) && Set.ofList actual = Set.ofList expected) "Missing, duplicate, or unexpected evidence inventory."

let private defaultGet url =
    let handler = new HttpClientHandler(AllowAutoRedirect = false)
    let client = new HttpClient(handler, Timeout = TimeSpan.FromMinutes 5.)
    let rec get address redirects = task {
        require (redirects <= 5) "Too many HTTP redirects."
        let uri = Uri address
        require (uri.Scheme = "https") "Evidence URLs must use HTTPS."
        use request = new HttpRequestMessage(HttpMethod.Get, uri)
        request.Headers.UserAgent.ParseAdd("FunnySharp-ReleaseProvenance/1")
        if uri.Host = "api.github.com" then
            match Environment.GetEnvironmentVariable "GH_TOKEN" with
            | null | "" -> ()
            | token -> request.Headers.Authorization <- AuthenticationHeaderValue("Bearer", token)
            request.Headers.Add("X-GitHub-Api-Version", "2022-11-28")
        use! response = client.SendAsync request
        let status = int response.StatusCode
        if status >= 300 && status < 400 then
            match response.Headers.Location with
            | null -> return invalidOp "Redirect has no location."
            | location -> return! get (Uri(uri, location).AbsoluteUri) (redirects + 1)
        else
            let! body = response.Content.ReadAsByteArrayAsync()
            return { StatusCode = status; EffectiveUrl = uri.AbsoluteUri; Body = body }
    }
    task {
        use _handler = handler
        use _client = client
        return! get url 0
    }

let defaultCollaborators =
    { Get = defaultGet
      UtcNow = fun () -> DateTimeOffset.UtcNow
      Environment = fun name -> Environment.GetEnvironmentVariable name |> Option.ofObj }

// Each operation claims an unused directory before any remote request. A failed
// attempt remains allocated: no retry can silently replace partial evidence.
let private claimDirectory path =
    let full = Path.GetFullPath path
    let rec checkParent directory =
        if Directory.Exists directory then
            require ((File.GetAttributes directory &&& FileAttributes.ReparsePoint) = enum 0) "Output parent is a reparse point."
        match Directory.GetParent directory with
        | null -> ()
        | parent -> checkParent parent.FullName
    checkParent full
    require (not (Directory.Exists full || File.Exists full)) "Output directory already exists."
    Directory.CreateDirectory full |> ignore
    use claim = new FileStream(Path.Combine(full, ".claim"), FileMode.CreateNew, FileAccess.Write, FileShare.None)
    full

let private writeNew root name (bytes: byte array) =
    let path = Path.Combine(root, name)
    match Path.GetDirectoryName path with | null -> () | parent -> Directory.CreateDirectory parent |> ignore
    use stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)
    stream.Write bytes

type private Capture(collaborators: Collaborators, output: string, repository: string, ?retainResponses: bool) =
    let captures = ResizeArray<obj>()
    let archives = Dictionary<int64, JsonElement * byte array>()
    member _.Captures = captures.ToArray()
    member _.Api = "https://api.github.com/repos/" + repository
    member _.Read(url: string) = task {
        let! response = collaborators.Get url
        let path = sprintf "http/%04d.body" captures.Count
        let retained = defaultArg retainResponses true
        if retained then writeNew output path response.Body
        captures.Add(obj ([ "url", box url; "effectiveUrl", box response.EffectiveUrl; "statusCode", box response.StatusCode
                            "bodySha256", box (sha256 response.Body); "observedAtUtc", box (collaborators.UtcNow().ToString("O")) ]
                          @ if retained then [ ("bodyPath", box path) ] else []))
        return response
    }
    member this.Json(url: string) = task {
        let! response = this.Read url
        require (response.StatusCode = 200) (sprintf "GET %s returned %d." url response.StatusCode)
        return parse response.Body
    }
    member this.Archive(id: int64) = task {
        match archives.TryGetValue id with
        | true, cached -> return cached
        | _ ->
            let! metadata = this.Json(this.Api + sprintf "/actions/artifacts/%d" id)
            require (number "id" metadata = id && not (flag "expired" metadata)) "Artifact is missing or expired."
            require (time "expires_at" metadata > collaborators.UtcNow()) "Artifact retention expired."
            let! response = this.Read(this.Api + sprintf "/actions/artifacts/%d/zip" id)
            require (response.StatusCode = 200) "Artifact download failed."
            if has "digest" metadata && (prop "digest" metadata).ValueKind = JsonValueKind.String then
                require (text "digest" metadata = "sha256:" + sha256 response.Body) "Artifact archive digest mismatch."
            let pair = metadata, response.Body
            archives.Add(id, pair)
            return pair
    }
    member this.Blob(reference: JsonElement) = task {
        exactFields [ "path"; "base64"; "artifactId"; "entry"; "sha256" ] reference
        require ([ "path"; "base64"; "artifactId" ] |> List.filter (fun key -> has key reference) |> List.length = 1) "A byte reference must have exactly one source."
        let! bytes = task {
            if has "path" reference then return File.ReadAllBytes(nonempty "path" reference)
            elif has "base64" reference then return Convert.FromBase64String(nonempty "base64" reference)
            else
                let! _, archiveBytes = this.Archive(number "artifactId" reference)
                use stream = new MemoryStream(archiveBytes, false)
                use archive = new ZipArchive(stream, ZipArchiveMode.Read)
                let matching = archive.Entries |> Seq.filter (fun entry -> entry.FullName = text "entry" reference) |> Seq.toList
                require (matching.Length = 1) "Artifact entry is missing or ambiguous."
                use content = matching.Head.Open()
                use buffer = new MemoryStream()
                do! content.CopyToAsync buffer
                return buffer.ToArray()
        }
        require (sha256 bytes = digest "sha256" reference) "Evidence byte hash mismatch."
        return bytes
    }

let private readRun (capture: Capture) runId attempt candidate = task {
    let! run = capture.Json(capture.Api + sprintf "/actions/runs/%d/attempts/%d" runId attempt)
    require (number "id" run = runId && number "run_attempt" run = attempt && text "head_sha" run = candidate) "Run candidate or attempt mismatch."
    require (text "name" run = "release" && text "path" run = ".github/workflows/release.yml") "Wrong release workflow."
    require (text "status" run = "completed") "Release run has not completed."
    nonempty "event" run |> ignore
    let! page = capture.Json(capture.Api + sprintf "/actions/runs/%d/attempts/%d/jobs?per_page=100" runId attempt)
    let jobs = items "jobs" page
    require (number "total_count" page = int64 jobs.Length) "Incomplete jobs readback."
    return run, jobs
}

let private checkJobs (capture: Capture) runId attempt candidate app failureContext jobs = task {
    let mutable finished = DateTimeOffset.MinValue
    for context in Ruleset.requiredContexts do
        let matching = jobs |> List.filter (fun job -> text "name" job = context)
        require (matching.Length = 1) ("Missing or duplicate exact job: " + context)
        let job = matching.Head
        require (number "run_id" job = runId && number "run_attempt" job = attempt && text "head_sha" job = candidate) "Job candidate/run/attempt mismatch."
        let expected = if Some context = failureContext then "failure" else "success"
        require (text "status" job = "completed" && text "conclusion" job = expected) ("Job is not " + expected + ": " + context)
        let checkUrl = nonempty "check_run_url" job
        require (checkUrl.StartsWith(capture.Api + "/check-runs/", StringComparison.Ordinal)) "Check URL is outside repository."
        let! check = capture.Json checkUrl
        require (text "name" check = context && text "head_sha" check = candidate && text "conclusion" check = expected
                 && text "status" check = "completed" && number "id" (prop "app" check) = app) "Trusted check binding mismatch."
        finished <- max finished (time "completed_at" job)
    return finished
}

let private packageIdentity (bytes: byte array) expectedId expectedVersion requireAssembly =
    use stream = new MemoryStream(bytes, false)
    use archive = new ZipArchive(stream, ZipArchiveMode.Read)
    let nuspecs = archive.Entries |> Seq.filter (fun entry -> not (entry.FullName.Contains '/') && entry.FullName.EndsWith ".nuspec") |> Seq.toList
    require (nuspecs.Length = 1) "Package must contain one nuspec."
    use content = nuspecs.Head.Open()
    let xml = XDocument.Load content
    let value name = xml.Descendants() |> Seq.filter (fun e -> e.Name.LocalName = name) |> Seq.exactlyOne |> fun e -> e.Value
    require (value "id" = expectedId && value "version" = expectedVersion) "Canonical package identity mismatch."
    let assemblies = archive.Entries |> Seq.filter (fun entry -> entry.FullName = "lib/net10.0/" + expectedId + ".dll") |> Seq.toList
    if requireAssembly then
        require (assemblies.Length = 1) "Canonical package assembly is missing."
        use dll = assemblies.Head.Open()
        Convert.ToHexString(SHA256.HashData dll).ToLowerInvariant()
    else
        require (archive.Entries |> Seq.exists (fun entry -> entry.FullName.EndsWith ".pdb")) "Symbols package is missing PDB bytes."
        ""

let private joinSourceFingerprints (capture: Capture) candidate canonicalDigest (executions: (string * JsonElement) list) = task {
    let validate fingerprint =
        exactFields [ "schemaVersion"; "algorithm"; "fileCount"; "digest"; "files" ] fingerprint
        require (number "schemaVersion" fingerprint = 1L && text "algorithm" fingerprint = "sha256") "Unsupported source fingerprint schema or algorithm."
        let files = items "files" fingerprint |> List.map (fun file ->
            exactFields [ "path"; "sha256" ] file
            let path = nonempty "path" file
            require (not (path.Contains '\\') && not (path |> Seq.exists Char.IsControl)
                     && (path.Split '/' |> Array.forall (fun part -> part <> "" && part <> "." && part <> ".."))) "Invalid source fingerprint path."
            path, digest "sha256" file)
        require (not files.IsEmpty && number "fileCount" fingerprint = int64 files.Length) "Source fingerprint file count mismatch."
        let paths = files |> List.map fst
        sameSet paths paths
        require (files = (files |> List.sortWith (fun (a, _) (b, _) -> StringComparer.InvariantCultureIgnoreCase.Compare(a, b)))) "Source fingerprint file order mismatch."
        let raw = files |> List.map (fun (path, hash) -> path + "\000" + hash + "\n") |> String.concat "" |> Encoding.UTF8.GetBytes
        require (sha256 raw = digest "digest" fingerprint) "Source fingerprint digest mismatch."
        files
    sameSet [ "win-x64"; "linux-x64"; "osx-arm64" ] (executions |> List.map fst)
    let fingerprints = executions |> List.map (fun (rid, execution) ->
        let before, after = prop "sourceFingerprintBefore" execution, prop "sourceFingerprintAfter" execution
        let files = validate before
        validate after |> ignore
        require (JsonElement.DeepEquals(before, after)) "Source fingerprint changed during the run."
        if rid = "win-x64" then require (digest "digest" before = canonicalDigest) "Canonical Windows source fingerprint mismatch."
        rid, files)
    let canonical = fingerprints |> List.find (fun (rid, _) -> rid = "win-x64") |> snd
    for _, files in fingerprints do
        require (List.map fst files = List.map fst canonical) "Source fingerprint path inventory mismatch."
    let hosts = fingerprints |> List.map (snd >> Map.ofList)
    for path, _ in canonical do
        let hashes = hosts |> List.map (fun files -> files.[path]) |> List.distinct
        if hashes.Length > 1 then
            require (List.contains path [ ".gitattributes"; ".gitignore"; "docs/next-stage/call-sites-code/NuGet.config"
                                          "docs/next-stage/call-sites-code/tools/.gitignore"; "docs/next-stage/inventory/generated/.gitignore"
                                          "FunnySharp.slnx"; "LICENSE" ]) ("Non-exempt source fingerprint difference: " + path)
            let url = capture.Api + "/contents/" + path + "?ref=" + candidate
            let! response = capture.Read url
            require (response.StatusCode = 200 && response.EffectiveUrl = url) "Candidate source blob request failed or redirected."
            let blob = parse response.Body
            require (text "type" blob = "file" && text "path" blob = path && text "url" blob = url
                     && text "encoding" blob = "base64") "Candidate source blob binding mismatch."
            let content = Convert.FromBase64String(text "content" blob)
            require (number "size" blob = content.LongLength) "Candidate source blob size mismatch."
            let gitBytes = Array.append (Encoding.UTF8.GetBytes(sprintf "blob %d\000" content.Length)) content
            require (Convert.ToHexString(SHA1.HashData gitBytes).ToLowerInvariant() = commit "sha" blob) "Candidate source blob SHA mismatch."
            let utf8 = UTF8Encoding(false, true)
            let decoded =
                try utf8.GetString content
                with :? DecoderFallbackException -> invalidOp "Candidate source blob is not valid UTF-8."
            let lf = decoded.Replace("\r\n", "\n")
            require (not (lf |> Seq.exists (fun c -> Char.IsControl c && c <> '\n' && c <> '\t'))) "Candidate source blob contains binary data or bare CR."
            let representations = [ sha256 content; sha256 (utf8.GetBytes lf); sha256 (utf8.GetBytes(lf.Replace("\n", "\r\n"))) ]
            require (hashes |> List.forall (fun hash -> List.contains hash representations)) ("Source fingerprint is not an exact candidate blob representation: " + path)
}

let private freeze collaborators output input = task {
    exactFields [ "schemaVersion"; "repository"; "candidateCommit"; "runId"; "runAttempt"; "producerIdentity"; "sourceFingerprint"
                  "expectedIntegrationId"; "targetBranch"; "rulesetId"; "evidence"; "contracts"; "packages"; "hosts"; "denials"
                  "distributionFeeds"; "localProofIds"; "findingAccountingId" ] input
    require (number "schemaVersion" input = 1L) "Unsupported input schema."
    let repository = nonempty "repository" input
    require (Regex.IsMatch(repository, "^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$")) "Invalid repository."
    let candidate = commit "candidateCommit" input
    let runId, attempt, app = number "runId" input, number "runAttempt" input, number "expectedIntegrationId" input
    require (runId > 0L && attempt > 0L && app > 0L) "Invalid GitHub identity."
    nonempty "producerIdentity" input |> ignore
    digest "sourceFingerprint" input |> ignore
    let capture = Capture(collaborators, output, repository)
    let! run, jobs = readRun capture runId attempt candidate
    require (text "conclusion" run = "success") "Aggregate run did not succeed."
    let! completed = checkJobs capture runId attempt candidate app None jobs
    require (completed <= collaborators.UtcNow()) "Jobs completed in the future."
    let evidence = Dictionary<string, byte array>()
    let manifest = ResizeArray<obj>()
    for entry in items "evidence" input do
        exactFields [ "id"; "bytes" ] entry
        let id = safeName (nonempty "id" entry)
        require (not (evidence.ContainsKey id)) "Duplicate evidence id."
        let! bytes = capture.Blob(prop "bytes" entry)
        evidence.Add(id, bytes)
        let path = "payload/" + id
        writeNew output path bytes
        manifest.Add(obj [ "id", box id; "path", box path; "length", box bytes.LongLength; "sha256", box (sha256 bytes) ])
    let get id = require (evidence.ContainsKey id) ("Missing evidence: " + id); evidence.[id]
    let document id = parse (get id)
    let contracts = items "contracts" input
    sameSet [ 1L .. 13L ] (contracts |> List.map (number "goal"))
    for contract in contracts do
        exactFields [ "goal"; "evidenceId" ] contract
        get (nonempty "evidenceId" contract) |> ignore
    let localProofs = items "localProofIds" input
    require (not localProofs.IsEmpty) "Local proof inventory is empty."
    for proof in localProofs do get (proof.GetString() |> Option.ofObj |> Option.defaultValue "") |> ignore
    get (nonempty "findingAccountingId" input) |> ignore
    let packages = items "packages" input
    sameSet [ "FunnySharp"; "FunnySharp.AspNetCore" ] (packages |> List.map (text "id"))
    let canonical = Dictionary<string, string * string * string>()
    for package in packages do
        exactFields [ "id"; "version"; "evidenceId"; "symbolsEvidenceId" ] package
        let id, version = text "id" package, nonempty "version" package
        let bytes = get (text "evidenceId" package)
        let assemblyHash = packageIdentity bytes id version true
        get (text "symbolsEvidenceId" package) |> fun symbols -> packageIdentity symbols id version false |> ignore
        canonical.Add(id, (version, sha256 bytes, assemblyHash))
    let hosts = items "hosts" input
    sameSet Ruleset.requiredContexts (hosts |> List.map (text "context"))
    let sourceExecutions = ResizeArray<string * JsonElement>()
    for host in hosts do
        exactFields [ "context"; "runtimeIdentifier"; "artifactId"; "consumerId"; "runtimeId"; "outcomeId"; "executionId"; "verificationId"; "preflightId" ] host
        let context = text "context" host
        let rid = context.Substring("release / ".Length).Replace("-consumer", "")
        require (text "runtimeIdentifier" host = rid) "Host RID mismatch."
        let! metadata, archiveBytes = capture.Archive(number "artifactId" host)
        let lineage = prop "workflow_run" metadata
        require (number "id" lineage = runId && text "head_sha" lineage = candidate) "Host artifact candidate/run mismatch."
        let expectedArtifactName =
            if rid = "win-x64" then sprintf "canonical-candidate-%d-%d" runId attempt
            else sprintf "release-evidence-%s-%d-%d" rid runId attempt
        require (text "name" metadata = expectedArtifactName) "Host artifact belongs to another run attempt."
        use archiveStream = new MemoryStream(archiveBytes, false)
        use hostArchive = new ZipArchive(archiveStream, ZipArchiveMode.Read)
        let archivedHashes =
            hostArchive.Entries |> Seq.map (fun entry ->
                use stream = entry.Open()
                Convert.ToHexString(SHA256.HashData stream).ToLowerInvariant()) |> Set.ofSeq
        let archived id = require (archivedHashes.Contains(sha256 (get id))) ("Evidence is not present in the declared host artifact: " + id)
        archived (nonempty "consumerId" host)
        if rid = "win-x64" then
            for package in packages do
                archived (text "evidenceId" package)
                archived (text "symbolsEvidenceId" package)
        let consumer = document (nonempty "consumerId" host)
        require (flag "Succeeded" consumer && text "RuntimeIdentifier" consumer = rid) "Consumer failed or ran on wrong host."
        for id, prefix in [ "FunnySharp", "Core"; "FunnySharp.AspNetCore", "AspNetCore" ] do
            let version, packageHash, assemblyHash = canonical.[id]
            require (text (prefix + "PackageVersion") consumer = version && (text (prefix + "PackageSha256") consumer).ToLowerInvariant() = packageHash
                     && text (prefix + "AssemblySha256") consumer = assemblyHash) "Consumer does not bind canonical Windows package bytes."
        let scenarios = items "Scenarios" consumer
        let expected = if rid = "osx-x64" then [ "CoreSmoke"; "AspNetCoreSmoke" ] else Compatibility.defaultScenarios
        sameSet expected (scenarios |> List.map (text "Scenario"))
        for scenario in scenarios do
            require (text "Outcome" scenario = "Passed" && text "RuntimeIdentifier" scenario = rid) "Consumer scenario failed."
            for id, prefix in [ "FunnySharp", "Core"; "FunnySharp.AspNetCore", "AspNetCore" ] do
                let version, packageHash, assemblyHash = canonical.[id]
                require (text (prefix + "PackageVersion") scenario = version && (text (prefix + "PackageSha256") scenario).ToLowerInvariant() = packageHash
                         && text (prefix + "AssemblySha256") scenario = assemblyHash) "Scenario canonical byte binding mismatch."
            if text "Scenario" scenario = "CoreSmoke" then
                let _, _, hash = canonical.["FunnySharp"]
                require (text "PublishedCoreAssemblySha256" scenario = hash) "Core smoke DLL mismatch."
            if text "Scenario" scenario = "AspNetCoreSmoke" then
                let _, _, hash = canonical.["FunnySharp.AspNetCore"]
                require (text "PublishedAspNetCoreAssemblySha256" scenario = hash) "ASP.NET smoke DLL mismatch."
        let runtime = document (nonempty "runtimeId" host)
        require (text "runtimeIdentifier" runtime = rid && text "candidateCommit" runtime = candidate) "Runtime evidence identity mismatch."
        for key in [ "sdk"; "runtime"; "runnerImage" ] do nonempty key runtime |> ignore
        if rid <> "osx-x64" then
            for key in [ "outcomeId"; "executionId"; "verificationId"; "preflightId" ] do archived (nonempty key host)
            let outcome = document (nonempty "outcomeId" host)
            let executionBytes = get (nonempty "executionId" host)
            let verificationBytes = get (nonempty "verificationId" host)
            let expectedAttemptId = sprintf "%d-%d-%s" runId attempt rid
            require (flag "succeeded" outcome && text "candidateCommit" outcome = candidate
                     && text "attemptId" outcome = expectedAttemptId) "Producer outcome failed or belongs to another attempt."
            require (digest "sha256" (prop "executionEvidence" outcome) = sha256 executionBytes
                     && digest "sha256" (prop "releaseEvidence" outcome) = sha256 verificationBytes) "Outcome byte pointers mismatch."
            let execution, verification = parse executionBytes, parse verificationBytes
            require (flag "succeeded" execution && text "candidateCommit" execution = candidate && text "mode" execution = "benchmarkSkipped"
                     && text "attemptId" execution = expectedAttemptId) "Producer execution mismatch."
            sourceExecutions.Add(rid, execution)
            require (flag "succeeded" verification && text "commit" (prop "environment" verification) = candidate
                     && (items "failures" verification).IsEmpty && (items "checks" verification).Length = 10
                     && (items "checks" verification |> List.forall (fun c -> text "status" c = "passed"))) "Producer verifier failed."
            let preflightBytes = get (nonempty "preflightId" host)
            require (sha256 preflightBytes = digest "versionPreflightSha256" execution) "Preflight byte binding mismatch."
            let preflight = parse preflightBytes
            require (text "status" preflight = "passed" && text "candidateCommit" preflight = candidate
                     && text "attemptId" preflight = expectedAttemptId) "Preflight failed or belongs to another attempt."
            let checks = items "checks" preflight
            for feed in items "distributionFeeds" input do
                for package in packages do
                    let feedUrl = feed.GetString() |> Option.ofObj |> Option.defaultValue ""
                    let matching = checks |> List.filter (fun c -> text "feed" c = feedUrl && text "packageId" c = text "id" package && text "version" c = text "version" package)
                    require (matching.Length = 1 && text "status" matching.Head = "absent") "Preflight feed/package inventory gap."
    do! joinSourceFingerprints capture candidate (text "sourceFingerprint" input) (List.ofSeq sourceExecutions)
    let ruleId = number "rulesetId" input
    let! rule = capture.Json(capture.Api + sprintf "/rulesets/%d" ruleId)
    require (number "id" rule = ruleId && (prop "bypass_actors" rule).ValueKind = JsonValueKind.Array) "Incomplete ruleset readback."
    match Ruleset.checkRuleset ruleId app (nonempty "targetBranch" input) rule with
    | Error error -> invalidOp error.Message
    | Ok _ -> ()
    let denials = items "denials" input
    sameSet Ruleset.requiredContexts (denials |> List.map (text "context"))
    for denial in denials do
        exactFields [ "context"; "runId"; "runAttempt"; "headSha"; "receiptId"; "actorEvidenceId" ] denial
        let context, proofHead = text "context" denial, commit "headSha" denial
        let proofId, proofAttempt = number "runId" denial, number "runAttempt" denial
        let! _, proofJobs = readRun capture proofId proofAttempt proofHead
        let! _ = checkJobs capture proofId proofAttempt proofHead app (Some context) proofJobs
        let receipt = document (nonempty "receiptId" denial)
        require (text "headSha" receipt = proofHead && number "runId" receipt = proofId && number "runAttempt" receipt = proofAttempt
                 && text "context" receipt = context && number "rulesetId" receipt = ruleId && text "targetBranch" receipt = text "targetBranch" input) "Denial lineage mismatch."
        let raw = get (nonempty "responseEvidenceId" receipt)
        require (sha256 raw = digest "responseSha256" receipt && Encoding.UTF8.GetString(raw).Contains(context, StringComparison.Ordinal)) "Denial does not name the blocking context."
        let status = number "statusCode" receipt
        if has "method" receipt && text "method" receipt = "disabled-merge-control" then
            let observation = parse raw
            require (status = 200L && flag "mergeControlDisabled" observation
                     && text "actor" observation = nonempty "actor" receipt
                     && text "headSha" observation = proofHead && text "targetBranch" observation = text "targetBranch" input
                     && (nonempty "blockingReason" observation).Contains(context, StringComparison.Ordinal)
                     && (nonempty "url" observation).StartsWith("https://github.com/" + repository + "/pull/", StringComparison.Ordinal)) "Normal merge control does not refuse this required check for the bound actor."
        else
            require (status = 403L || status = 405L || status = 409L || status = 422L) "Denial capture is not a platform refusal."
        let actor = document (nonempty "actorEvidenceId" denial)
        require (nonempty "actor" actor = nonempty "actor" receipt && not (flag "bypass" actor)
                 && number "rulesetId" actor = ruleId && text "headSha" actor = proofHead) "Ordinary actor/no-bypass proof is missing."
    let feeds = items "distributionFeeds" input |> List.map (fun f -> f.GetString() |> Option.ofObj |> Option.defaultValue "")
    require (not feeds.IsEmpty && (Set.ofList feeds).Count = feeds.Length) "Distribution feed inventory is empty or duplicated."
    let finalChecks = ResizeArray<obj>()
    for feed in feeds do
        let! service = capture.Read feed
        require (service.StatusCode = 200) "Final distribution service read failed."
        let resources = items "resources" (parse service.Body) |> List.filter (fun r -> text "@type" r = "PackageBaseAddress/3.0.0")
        require (resources.Length = 1) "Ambiguous distribution PackageBaseAddress."
        let address = nonempty "@id" resources.Head
        for package in packages do
            let id, version = text "id" package, text "version" package
            let! response = capture.Read(address.TrimEnd('/') + "/" + id.ToLowerInvariant() + "/index.json")
            require (response.StatusCode = 200 || response.StatusCode = 404) "Final distribution package read failed."
            let versions = if response.StatusCode = 404 then [] else items "versions" (parse response.Body) |> List.map (fun v -> v.GetString() |> Option.ofObj |> Option.defaultValue "")
            require (versions |> List.forall (fun v -> not (String.IsNullOrWhiteSpace v))) "Malformed version list."
            match ReleaseProtocol.assertPackageVersionAbsent id version (Some versions) with | Error e -> invalidOp e.Message | Ok () -> ()
            let normalized = versions |> List.map (fun v -> v.ToLowerInvariant()) |> List.distinct |> List.sort
            finalChecks.Add(obj [ "feed", box feed; "effectiveServiceUrl", box service.EffectiveUrl; "packageBaseAddress", box address
                                  "packageId", box id; "version", box version; "status", box "absent"; "statusCode", box response.StatusCode
                                  "rawBodySha256", box (sha256 response.Body); "normalizedVersionsSha256", box (sha256 (encode normalized))
                                  "versions", box normalized; "checkedAtUtc", box (collaborators.UtcNow().ToString("O")) ])
    let producer = obj [ "schemaVersion", box 1; "kind", box "producer"; "createdAtUtc", box (collaborators.UtcNow().ToString("O"))
                         "input", box input; "run", box run; "jobs", box jobs; "evidence", box (manifest.ToArray())
                         "http", box capture.Captures; "finalFeedChecks", box (finalChecks.ToArray()) ]
    writeNew output "P.json" (encode producer)
}

let private validateProducer (producer: JsonElement) =
    exactFields [ "schemaVersion"; "kind"; "createdAtUtc"; "input"; "run"; "jobs"; "evidence"; "http"; "finalFeedChecks" ] producer
    require (number "schemaVersion" producer = 1L && text "kind" producer = "producer") "Not a producer manifest."

let private validateAttestation (producerBytes: byte array) (attestationBytes: byte array) =
    let producer, attestation = parse producerBytes, parse attestationBytes
    validateProducer producer
    exactFields [ "schemaVersion"; "kind"; "createdAtUtc"; "reviewer"; "producerSha256"; "candidateCommit"; "contracts"
                  "verdict"; "auditStatus"; "productAcceptance"; "goals"; "replays"; "report" ] attestation
    require (number "schemaVersion" attestation = 1L && text "kind" attestation = "attestation") "Not an attestation."
    require (digest "producerSha256" attestation = sha256 producerBytes) "Attestation references wrong P bytes."
    require (time "createdAtUtc" attestation > time "createdAtUtc" producer) "Independent attestation must follow P."
    let input = prop "input" producer
    require (text "candidateCommit" attestation = text "candidateCommit" input) "Attestation candidate mismatch."
    let reviewer = prop "reviewer" attestation
    exactFields [ "name"; "identity"; "readOnly" ] reviewer
    nonempty "name" reviewer |> ignore
    require (nonempty "identity" reviewer <> nonempty "producerIdentity" input && flag "readOnly" reviewer) "Independent read-only reviewer is required."
    require (text "verdict" attestation = "APPROVE" && text "auditStatus" attestation = "COMPLETE" && text "productAcceptance" attestation = "PASS") "Attestation does not approve product acceptance."
    let evidence = items "evidence" producer
    let lookup id = evidence |> List.find (fun e -> text "id" e = id)
    let contracts = items "contracts" attestation
    sameSet [ 1L .. 13L ] (contracts |> List.map (number "goal"))
    for contract in contracts do
        exactFields [ "goal"; "sha256" ] contract
        let original = items "contracts" input |> List.find (fun c -> number "goal" c = number "goal" contract)
        require (digest "sha256" contract = digest "sha256" (lookup (text "evidenceId" original))) "Goal contract hash mismatch."
    let goals = items "goals" attestation
    sameSet [ 1L .. 13L ] (goals |> List.map (number "goal"))
    for goal in goals do
        exactFields [ "goal"; "status"; "evidenceIds" ] goal
        require (text "status" goal = "PASS" && not (items "evidenceIds" goal).IsEmpty) "Material goal failed or lacks evidence."
        for reference in items "evidenceIds" goal do lookup (reference.GetString() |> Option.ofObj |> Option.defaultValue "") |> ignore
    let embedded node =
        exactFields [ "base64"; "sha256" ] node
        let bytes = Convert.FromBase64String(nonempty "base64" node)
        require (bytes.Length > 0 && sha256 bytes = digest "sha256" node) "Embedded reviewer evidence hash mismatch."
        bytes
    let replays = items "replays" attestation
    sameSet [ 4L; 9L ] (replays |> List.map (number "goal"))
    for replay in replays do
        exactFields [ "goal"; "inputEvidenceIds"; "receipt" ] replay
        require (not (items "inputEvidenceIds" replay).IsEmpty) "Replay lacks P-bound inputs."
        for reference in items "inputEvidenceIds" replay do lookup (reference.GetString() |> Option.ofObj |> Option.defaultValue "") |> ignore
        let receipt = embedded (prop "receipt" replay) |> parse
        exactFields [ "goal"; "exitCode"; "producerSha256"; "reviewerIdentity"; "command"; "startedAtUtc"; "finishedAtUtc" ] receipt
        require (number "goal" receipt = number "goal" replay && number "exitCode" receipt = 0L
                 && text "producerSha256" receipt = sha256 producerBytes && text "reviewerIdentity" receipt = text "identity" reviewer
                 && time "startedAtUtc" receipt > time "createdAtUtc" producer && time "finishedAtUtc" receipt >= time "startedAtUtc" receipt
                 && time "finishedAtUtc" receipt <= time "createdAtUtc" attestation) "Replay receipt is not successful post-P reviewer execution."
        nonempty "command" receipt |> ignore
    embedded (prop "report" attestation)

let private stage collaborators output input = task {
    exactFields [ "producer"; "attestation"; "repository" ] input
    let capture = Capture(collaborators, Path.Combine(output, "transport"), nonempty "repository" input)
    let! producer = capture.Blob(prop "producer" input)
    let! attestation = capture.Blob(prop "attestation" input)
    let report = validateAttestation producer attestation
    require (time "createdAtUtc" (parse attestation) <= collaborators.UtcNow()) "Attestation is from the future."
    let source = prop "producer" input
    let attachments =
        [ for entry in items "evidence" (parse producer) do yield text "path" entry, digest "sha256" entry
          for entry in items "http" (parse producer) do yield text "bodyPath" entry, digest "bodySha256" entry ]
    for path, hash in attachments do
        require (Regex.IsMatch(path, "^(payload/[A-Za-z0-9][A-Za-z0-9._-]{0,127}|http/[0-9]+[.]body)$")) "Invalid P attachment path."
        let reference = JsonObject()
        reference.["sha256"] <- JsonValue.Create hash
        if has "path" source then
            let parent = Path.GetDirectoryName(Path.GetFullPath(text "path" source)) |> Option.ofObj |> Option.defaultWith (fun () -> invalidOp "P path has no parent.")
            reference.["path"] <- JsonValue.Create(Path.Combine(parent, path))
        else
            require (has "artifactId" source && text "entry" source = "P.json") "P attachments require a file or root artifact P.json."
            reference.["artifactId"] <- JsonValue.Create(number "artifactId" source)
            reference.["entry"] <- JsonValue.Create path
        let! bytes = capture.Blob(parse (Encoding.UTF8.GetBytes(reference.ToJsonString())))
        writeNew output path bytes
    writeNew output "P.json" producer
    writeNew output "A.json" attestation
    writeNew output "review.md" report
}

let private index collaborators output artifactUrl = task {
    require (Directory.Exists output && not (File.Exists(Path.Combine(output, "index.json")))) "Index output must be an existing stage without index.json."
    let env name = collaborators.Environment name |> Option.defaultWith (fun () -> invalidOp ("Missing GitHub provenance environment: " + name))
    let repository = env "GITHUB_REPOSITORY"
    let capture = Capture(collaborators, output, repository, retainResponses = false)
    let url = Uri artifactUrl
    let parts = url.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries)
    require (url.Scheme = "https" && url.Host = "github.com" && parts.Length = 7 && parts.[2] = "actions" && parts.[3] = "runs" && parts.[5] = "artifacts") "Invalid artifact URL."
    let artifactId = Int64.Parse parts.[6]
    let! metadata, archiveBytes = capture.Archive artifactId
    let provenance = prop "workflow_run" metadata
    let runId = number "id" provenance
    let expectedUrl = sprintf "https://github.com/%s/actions/runs/%d/artifacts/%d" repository runId artifactId
    require (artifactUrl = expectedUrl) "ArtifactUrl is not the actual uploaded artifact location."
    let! run = capture.Json(capture.Api + sprintf "/actions/runs/%d" runId)
    require (number "id" run = runId && text "head_sha" run = text "head_sha" provenance) "Uploaded artifact run provenance mismatch."
    require (env "GITHUB_REPOSITORY" = repository && env "GITHUB_RUN_ID" = string runId
             && env "GITHUB_RUN_ATTEMPT" = string (number "run_attempt" run) && env "GITHUB_SHA" = text "head_sha" run) "Index must run in the actual upload run."
    use stream = new MemoryStream(archiveBytes, false)
    use archive = new ZipArchive(stream, ZipArchiveMode.Read)
    let readEntry name =
        let entries = archive.Entries |> Seq.filter (fun e -> e.FullName = name) |> Seq.toList
        require (entries.Length = 1) ("Uploaded artifact is missing " + name)
        use entry = entries.Head.Open()
        use buffer = new MemoryStream()
        entry.CopyTo buffer
        buffer.ToArray()
    let producer, attestation = readEntry "P.json", readEntry "A.json"
    require (sha256 producer = sha256 (File.ReadAllBytes(Path.Combine(output, "P.json")))
             && sha256 attestation = sha256 (File.ReadAllBytes(Path.Combine(output, "A.json")))) "Uploaded P/A byte hash mismatch."
    let report = validateAttestation producer attestation
    require (sha256 (readEntry "review.md") = sha256 report) "Uploaded report bytes differ from A."
    for entry in items "evidence" (parse producer) do
        require (sha256 (readEntry (text "path" entry)) = digest "sha256" entry) "Uploaded P payload hash mismatch."
    for entry in items "http" (parse producer) do
        require (sha256 (readEntry (text "bodyPath" entry)) = digest "bodySha256" entry) "Uploaded P raw response hash mismatch."
    let result = obj [ "schemaVersion", box 1; "kind", box "index"; "createdAtUtc", box (collaborators.UtcNow().ToString("O"));
                      "producerSha256", box (sha256 producer); "attestationSha256", box (sha256 attestation);
                      "artifactUrl", box artifactUrl; "artifact", box metadata; "publicationRun", box run; "http", box capture.Captures;
                      "candidateCommit", box (text "candidateCommit" (prop "input" (parse producer))) ]
    writeNew output "index.json" (encode result)
    let summary = env "GITHUB_STEP_SUMMARY"
    File.AppendAllText(summary, sprintf "\n## Release provenance\n\nArtifact: %s\n\nP SHA256: `%s`\n\nA SHA256: `%s`\n\n```json\n%s\n```\n" artifactUrl (sha256 producer) (sha256 attestation) (Encoding.UTF8.GetString(encode result)), UTF8Encoding(false))
}

let mainWith (stdout: TextWriter) (stderr: TextWriter) collaborators (argv: string list) =
    let options = Dictionary<string, string>()
    let rec arguments remaining =
        match remaining with
        | [] -> ()
        | name :: value :: rest when List.contains name [ "-Mode"; "-InputPath"; "-OutputDirectory"; "-ArtifactUrl" ] && not (options.ContainsKey name) ->
            options.Add(name, value)
            arguments rest
        | _ -> invalidArg "argv" "Expected -Mode MODE [-InputPath JSON] -OutputDirectory NEW [-ArtifactUrl URL]."
    task {
      try
        arguments argv
        let option name = match options.TryGetValue name with | true, value -> value | _ -> invalidArg name ("Missing " + name)
        let mode = option "-Mode"
        require (List.contains mode [ "freeze-producer"; "stage-attestation"; "write-index" ]) "Unknown provenance mode."
        let inputBytes =
            if mode = "write-index" then Encoding.UTF8.GetBytes "{}" else
            match options.TryGetValue "-InputPath" with
            | true, path -> File.ReadAllBytes path
            | _ ->
                let payload = collaborators.Environment "RELEASE_PROVENANCE_PAYLOAD" |> Option.defaultWith (fun () -> invalidArg "-InputPath" "InputPath or RELEASE_PROVENANCE_PAYLOAD is required.")
                use input = new MemoryStream(Convert.FromBase64String payload)
                use gzip = new GZipStream(input, CompressionMode.Decompress)
                use buffer = new MemoryStream()
                gzip.CopyTo buffer
                buffer.ToArray()
        let input = parse inputBytes
        let output = if mode = "write-index" then Path.GetFullPath(option "-OutputDirectory") else claimDirectory (option "-OutputDirectory")
        match mode with
        | "freeze-producer" -> do! freeze collaborators output input
        | "stage-attestation" -> do! stage collaborators output input
        | _ -> do! index collaborators output (option "-ArtifactUrl")
        stdout.WriteLine("Release provenance passed: " + Path.Combine(output, if mode = "freeze-producer" then "P.json" elif mode = "stage-attestation" then "A.json" else "index.json"))
        return 0
      with
      | :? ArgumentException as error -> stderr.WriteLine error.Message; return 2
      | error -> stderr.WriteLine("Release provenance failed: " + error.Message); return 1
    }

let main (argv: string array) =
    mainWith Console.Out Console.Error defaultCollaborators (List.ofArray argv)
    |> Async.AwaitTask
    |> Async.RunSynchronously
