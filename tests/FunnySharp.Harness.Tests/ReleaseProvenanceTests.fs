module FunnySharp.Harness.Tests.ReleaseProvenanceTests

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.IO.Compression
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open System.Threading
open System.Threading.Tasks
open Xunit
open FunnySharp.Harness.ReleaseProvenance
open FunnySharp.Harness.Tests.Support

let private bytes (value: string) = Encoding.UTF8.GetBytes value
let private encode value = JsonSerializer.SerializeToUtf8Bytes value
let private obj (fields: (string * objnull) list) = dict fields
let private node (value: byte array) = JsonNode.Parse(value) |> Option.ofObj |> Option.defaultWith (fun () -> failwith "null JSON")
let private at (name: string) (value: JsonNode) = value.[name] |> Option.ofObj |> Option.defaultWith (fun () -> failwith ("missing " + name))
let private present (value: JsonNode | null) = value |> Option.ofObj |> Option.defaultWith (fun () -> failwith "missing array item")
let private str (value: string) : JsonNode = (JsonValue.Create value |> Option.ofObj |> Option.defaultWith (fun () -> failwith "null string")) :> JsonNode
let private num (value: int64) : JsonNode = JsonValue.Create value
let private boolean value : JsonNode = JsonValue.Create(value: bool)
let private reference content = obj [ "base64", box (Convert.ToBase64String content); "sha256", box (sha256 content) ]
let private candidate = String.replicate 40 "a"
let private source = String.replicate 64 "b"
let private now = DateTimeOffset.Parse "2026-10-03T12:00:00Z"
let private contexts = FunnySharp.Harness.Ruleset.requiredContexts
let private api = "https://api.github.com/repos/owner/repo"
let private feed = "https://api.nuget.org/v3/index.json"
let private flat = "https://api.nuget.org/v3-flatcontainer/"

let private metadataPaths =
    [ ".gitattributes"; ".gitignore"; "docs/next-stage/call-sites-code/NuGet.config"
      "docs/next-stage/call-sites-code/tools/.gitignore"; "docs/next-stage/inventory/generated/.gitignore"
      "FunnySharp.slnx"; "LICENSE" ]
let private sourceFiles crlf =
    [ for path in metadataPaths -> path, bytes (path + (if crlf then "\r\n" else "\n")) ]
    @ [ for path in [ "src/FunnySharp/Option.cs"; "tests/FunnySharp.Tests/OptionTests.cs"; "eng/harness/ReleaseRun.fs";
                     "eng/performance/baseline.json"; "Directory.Build.props"; "src/FunnySharp/packages.lock.json"; "README.md" ] ->
            path, bytes (path + "\n") ]
let private fingerprint files =
    let ordered = files |> List.sortWith (fun (a, _) (b, _) -> StringComparer.InvariantCultureIgnoreCase.Compare(a, b))
    let hashes = ordered |> List.map (fun (path, content) -> path, sha256 content)
    node (encode (obj [ "schemaVersion", box 1; "algorithm", box "sha256"; "fileCount", box hashes.Length
                        "digest", box (sha256 (bytes (hashes |> List.map (fun (path, hash) -> path + "\000" + hash + "\n") |> String.concat "")))
                        "files", box (hashes |> List.map (fun (path, hash) -> obj [ "path", box path; "sha256", box hash ])) ]))
let private contentsUrl path = api + "/contents/" + path + "?ref=" + candidate
let private gitBlobSha (content: byte array) =
    Convert.ToHexString(SHA1.HashData(Array.append (bytes (sprintf "blob %d\000" content.Length)) content)).ToLowerInvariant()

let private archive (entries: (string * byte array) list) =
    use stream = new MemoryStream()
    do
        use zip = new ZipArchive(stream, ZipArchiveMode.Create, true)
        for path, content in entries do
            let entry = zip.CreateEntry path
            entry.LastWriteTime <- DateTimeOffset.Parse "2026-01-01T00:00:00Z"
            use output = entry.Open()
            output.Write content
    stream.ToArray()

let private package id symbols =
    archive [ id + ".nuspec", bytes (sprintf "<package><metadata><id>%s</id><version>0.2.0</version></metadata></package>" id);
              "lib/net10.0/" + id + (if symbols then ".pdb" else ".dll"), bytes (id + (if symbols then " symbols" else " assembly")) ]

type private Fixture(?productionAttempt: int64, ?differentHostFingerprints: bool) =
    let productionAttempt = defaultArg productionAttempt 1L
    let differentHostFingerprints = defaultArg differentHostFingerprints false
    let canonicalFingerprint = fingerprint (sourceFiles true)
    let temp = new TempDirectory()
    let responses = Dictionary<string, HttpResponse>()
    let requested = ResizeArray<string>()
    let environment = Dictionary<string, string>()
    let mutable currentTime = now
    let evidence = ResizeArray<obj>()
    let content = Dictionary<string, byte array>()
    let json url value = responses.[url] <- { StatusCode = 200; EffectiveUrl = url; Body = encode value }
    let add id data =
        content.Add(id, data)
        evidence.Add(obj [ "id", box id; "bytes", box (reference data) ])
        id
    let addJson id value = add id (encode value)
    let makeRun id head (failure: string option) =
        let attempt = if id = 10L then productionAttempt else 1L
        let run = obj [ "id", box id; "run_attempt", box attempt; "head_sha", box head; "name", box "release";
                        "path", box ".github/workflows/release.yml"; "status", box "completed";
                        "conclusion", box (if failure.IsSome then "failure" else "success"); "event", box "workflow_dispatch" ]
        json (api + sprintf "/actions/runs/%d/attempts/%d" id attempt) run
        json (api + sprintf "/actions/runs/%d" id) run
        let jobs =
            contexts |> List.mapi (fun index context ->
                let conclusion = if failure = Some context then "failure" else "success"
                let checkUrl = api + sprintf "/check-runs/%d" (id * 10L + int64 index)
                json checkUrl (obj [ "id", box (id * 10L + int64 index); "name", box context; "head_sha", box head;
                                    "status", box "completed"; "conclusion", box conclusion; "app", box (obj [ "id", box 15368L ]) ])
                obj [ "id", box (id * 10L + int64 index); "name", box context; "run_id", box id; "run_attempt", box attempt;
                      "head_sha", box head; "status", box "completed"; "conclusion", box conclusion;
                      "check_run_url", box checkUrl; "completed_at", box "2026-10-03T10:00:00Z" ])
        json (api + sprintf "/actions/runs/%d/attempts/%d/jobs?per_page=100" id attempt) (obj [ "total_count", box 4; "jobs", box jobs ])
    let artifact id runId data =
        let name =
            if id = 100L then sprintf "canonical-candidate-%d-%d" runId productionAttempt
            elif id = 101L then sprintf "release-evidence-linux-x64-%d-%d" runId productionAttempt
            elif id = 102L then sprintf "release-evidence-osx-arm64-%d-%d" runId productionAttempt
            elif id = 103L then sprintf "release-evidence-osx-x64-%d-%d" runId productionAttempt
            else "staged-provenance"
        json (api + sprintf "/actions/artifacts/%d" id)
            (obj [ "id", box id; "name", box name; "expired", box false; "expires_at", box "2026-11-03T12:00:00Z"; "digest", box ("sha256:" + sha256 data);
                   "workflow_run", box (obj [ "id", box runId; "head_sha", box candidate ]) ])
        responses.[api + sprintf "/actions/artifacts/%d/zip" id] <- { StatusCode = 200; EffectiveUrl = "https://artifact.example/immutable"; Body = data }
    let core, asp = package "FunnySharp" false, package "FunnySharp.AspNetCore" false
    let canonicalFields =
        [ "CorePackageVersion", box "0.2.0"; "AspNetCorePackageVersion", box "0.2.0";
          "CorePackageSha256", box ((sha256 core).ToUpperInvariant()); "AspNetCorePackageSha256", box ((sha256 asp).ToUpperInvariant());
          "CoreAssemblySha256", box (sha256 (bytes "FunnySharp assembly")); "AspNetCoreAssemblySha256", box (sha256 (bytes "FunnySharp.AspNetCore assembly")) ]
    let mutable input = Unchecked.defaultof<JsonNode>
    do
        makeRun 10L candidate None
        for path, content in sourceFiles false |> List.filter (fun (path, _) -> List.contains path metadataPaths) do
            json (contentsUrl path) (obj [ "type", box "file"; "path", box path; "sha", box (gitBlobSha content);
                                          "encoding", box "base64"; "content", box (Convert.ToBase64String content);
                                          "size", box content.Length; "url", box (contentsUrl path) ])
        let contracts = [ for goal in 1 .. 13 -> obj [ "goal", box goal; "evidenceId", box (add (sprintf "goal-%02d" goal) (bytes (sprintf "contract %d" goal))) ] ]
        let packages =
            [ for id, data in [ "FunnySharp", core; "FunnySharp.AspNetCore", asp ] ->
                obj [ "id", box id; "version", box "0.2.0"; "evidenceId", box (add (id + ".nupkg") data);
                      "symbolsEvidenceId", box (add (id + ".snupkg") (package id true)) ] ]
        let hosts =
            contexts |> List.mapi (fun index context ->
                let rid = context.Substring("release / ".Length).Replace("-consumer", "")
                let artifactId = 100L + int64 index
                let scenarioNames = if rid = "osx-x64" then [ "CoreSmoke"; "AspNetCoreSmoke" ] else FunnySharp.Harness.Compatibility.defaultScenarios
                let attemptId = sprintf "10-%d-%s" productionAttempt rid
                let scenarios =
                    scenarioNames |> List.map (fun scenario -> obj (canonicalFields @ [ "Scenario", box scenario; "Outcome", box "Passed"; "RuntimeIdentifier", box rid;
                                                                                    "PublishedCoreAssemblySha256", box (sha256 (bytes "FunnySharp assembly"));
                                                                                    "PublishedAspNetCoreAssemblySha256", box (sha256 (bytes "FunnySharp.AspNetCore assembly")) ]))
                let consumer = addJson (rid + "-consumer") (obj (canonicalFields @ [ "Succeeded", box true; "RuntimeIdentifier", box rid; "Scenarios", box scenarios ]))
                let runtime = addJson (rid + "-runtime") (obj [ "runtimeIdentifier", box rid; "candidateCommit", box candidate; "sdk", box "10.0.400"; "runtime", box ".NET 10.0"; "runnerImage", box rid ])
                let common = [ "context", box context; "runtimeIdentifier", box rid; "artifactId", box artifactId; "consumerId", box consumer; "runtimeId", box runtime ]
                if rid = "osx-x64" then
                    artifact artifactId 10L (archive [ consumer, content.[consumer] ])
                    obj common
                else
                    let hostFingerprint = if differentHostFingerprints && rid <> "win-x64" then fingerprint (sourceFiles false) else canonicalFingerprint
                    let preflight = addJson (rid + "-preflight") (obj [ "status", box "passed"; "candidateCommit", box candidate; "attemptId", box attemptId;
                                                                      "checks", box ([ "FunnySharp"; "FunnySharp.AspNetCore" ] |> List.map (fun id -> obj [ "feed", box feed; "packageId", box id; "version", box "0.2.0"; "status", box "absent" ])) ])
                    let execution = addJson (rid + "-execution") (obj [ "succeeded", box true; "candidateCommit", box candidate; "attemptId", box attemptId; "mode", box "benchmarkSkipped";
                                                                      "sourceFingerprintBefore", box hostFingerprint; "sourceFingerprintAfter", box hostFingerprint;
                                                                      "versionPreflightSha256", box (sha256 content.[preflight]) ])
                    let verification = addJson (rid + "-verification") (obj [ "succeeded", box true; "environment", box (obj [ "commit", box candidate ]);
                                                                             "failures", box [||]; "checks", box [ for n in 1 .. 10 -> obj [ "name", box n; "status", box "passed" ] ] ])
                    let outcome = addJson (rid + "-outcome") (obj [ "succeeded", box true; "candidateCommit", box candidate; "attemptId", box attemptId;
                                                                  "executionEvidence", box (obj [ "sha256", box (sha256 content.[execution]) ]);
                                                                  "releaseEvidence", box (obj [ "sha256", box (sha256 content.[verification]) ]) ])
                    let entries = [ consumer; preflight; execution; verification; outcome ] @ (if rid = "win-x64" then [ "FunnySharp.nupkg"; "FunnySharp.snupkg"; "FunnySharp.AspNetCore.nupkg"; "FunnySharp.AspNetCore.snupkg" ] else [])
                    artifact artifactId 10L (archive (entries |> List.map (fun id -> id, content.[id])))
                    obj (common @ [ "outcomeId", box outcome; "executionId", box execution; "verificationId", box verification; "preflightId", box preflight ]))
        json (api + "/rulesets/42") (obj [ "id", box 42; "name", box "strict-release"; "target", box "branch"; "enforcement", box "active";
                                          "conditions", box (obj [ "ref_name", box (obj [ "include", box [ "refs/heads/main" ]; "exclude", box [||] ]) ]);
                                          "bypass_actors", box [||]; "rules", box [ obj [ "type", box "required_status_checks"; "parameters", box (obj [ "strict_required_status_checks_policy", box true; "required_status_checks", box (contexts |> List.map (fun context -> obj [ "context", box context; "integration_id", box 15368 ])) ]) ] ] ])
        let denials =
            contexts |> List.mapi (fun index context ->
                let id, head = 20L + int64 index, String.replicate 40 (string (index + 1))
                makeRun id head (Some context)
                let raw = add (sprintf "denial-%d-body" index) (encode (obj [ "message", box ("Required status check failed: " + context) ]))
                let receipt = addJson (sprintf "denial-%d" index) (obj [ "context", box context; "headSha", box head; "runId", box id; "runAttempt", box 1;
                                                                       "rulesetId", box 42; "targetBranch", box "main"; "statusCode", box 405;
                                                                       "responseEvidenceId", box raw; "responseSha256", box (sha256 content.[raw]); "actor", box "ordinary" ])
                let actor = addJson (sprintf "actor-%d" index) (obj [ "actor", box "ordinary"; "bypass", box false; "headSha", box head; "rulesetId", box 42 ])
                obj [ "context", box context; "runId", box id; "runAttempt", box 1; "headSha", box head; "receiptId", box receipt; "actorEvidenceId", box actor ])
        let local = add "local-proof" (bytes "retained local proof")
        let findings = add "findings" (bytes "finding accounting")
        json feed (obj [ "resources", box [ obj [ "@type", box "PackageBaseAddress/3.0.0"; "@id", box flat ] ] ])
        for id in [ "funnysharp"; "funnysharp.aspnetcore" ] do
            let url = flat + id + "/index.json"
            responses.[url] <- { StatusCode = 404; EffectiveUrl = url; Body = bytes "actual origin not found response" }
        input <- node (encode (obj [ "schemaVersion", box 1; "repository", box "owner/repo"; "candidateCommit", box candidate; "runId", box 10; "runAttempt", box productionAttempt;
                                    "producerIdentity", box "producer"; "sourceFingerprint", box ((at "digest" canonicalFingerprint).GetValue<string>()); "expectedIntegrationId", box 15368; "targetBranch", box "main"; "rulesetId", box 42;
                                    "evidence", box evidence; "contracts", box contracts; "packages", box packages; "hosts", box hosts; "denials", box denials;
                                    "distributionFeeds", box [ feed ]; "localProofIds", box [ local ]; "findingAccountingId", box findings ]))
    member _.Root = temp.Path
    member _.Input = input
    member _.Responses = responses
    member _.Requested = requested
    member _.Environment = environment
    member _.CurrentTime with get() = currentTime and set value = currentTime <- value
    member _.Artifact(id, runId, data) = artifact id runId data
    member _.RunMetadata(id) = makeRun id candidate None
    member _.ChangeResponse(url, change: JsonNode -> unit) =
        let response = responses.[url]
        let value = node response.Body
        change value
        responses.[url] <- { response with Body = bytes (value.ToJsonString()) }
    member _.ReplaceEvidence(id, change: JsonNode -> unit) =
        let entries = at "evidence" input
        let entry = entries.AsArray() |> Seq.choose Option.ofObj |> Seq.find (fun entry -> (at "id" entry).GetValue<string>() = id)
        let current = at "bytes" entry
        let value = node (Convert.FromBase64String((at "base64" current).GetValue<string>()))
        change value
        entry.["bytes"] <- node (encode (reference (bytes (value.ToJsonString()))))
    member _.EvidenceBytes(id) =
        let entry = (at "evidence" input).AsArray() |> Seq.choose Option.ofObj |> Seq.find (fun entry -> (at "id" entry).GetValue<string>() = id)
        Convert.FromBase64String((at "bytes" entry |> at "base64").GetValue<string>())
    member this.ReplaceHostExecution(rid, change: JsonNode -> unit) =
        this.ReplaceEvidence(rid + "-execution", change)
        this.ReplaceEvidence(rid + "-outcome", fun value -> (at "executionEvidence" value).["sha256"] <- str (sha256 (this.EvidenceBytes(rid + "-execution"))))
        let entries = [ for suffix in [ "consumer"; "preflight"; "execution"; "verification"; "outcome" ] -> rid + "-" + suffix ]
                      @ (if rid = "win-x64" then [ "FunnySharp.nupkg"; "FunnySharp.snupkg"; "FunnySharp.AspNetCore.nupkg"; "FunnySharp.AspNetCore.snupkg" ] else [])
        let index = contexts |> List.findIndex (fun context -> context = "release / " + rid)
        artifact (100L + int64 index) 10L (archive (entries |> List.map (fun id -> id, this.EvidenceBytes id)))
    member _.Run(mode, directory, value: JsonNode, ?artifactUrl: string, ?payload: bool) = task {
        use stdout = new StringWriter()
        use stderr = new StringWriter()
        let collaborators =
            { Get = fun url -> requested.Add url; Task.FromResult responses.[url]
              UtcNow = fun () -> currentTime
              Environment = fun name -> match environment.TryGetValue name with | true, value -> Some value | _ -> None }
        let inputBytes = bytes (value.ToJsonString())
        let inputPath = Path.Combine(temp.Path, Guid.NewGuid().ToString("N") + ".json")
        let usePayload = defaultArg payload false
        if usePayload then
            use buffer = new MemoryStream()
            do
                use gzip = new GZipStream(buffer, CompressionMode.Compress, true)
                gzip.Write inputBytes
            environment.["RELEASE_PROVENANCE_PAYLOAD"] <- Convert.ToBase64String(buffer.ToArray())
        else File.WriteAllBytes(inputPath, inputBytes)
        let args = [ "-Mode"; mode; "-OutputDirectory"; directory ] @ (if usePayload || mode = "write-index" then [] else [ "-InputPath"; inputPath ]) @ (match artifactUrl with | Some url -> [ "-ArtifactUrl"; url ] | None -> [])
        let! code = mainWith stdout stderr collaborators args
        return code, stderr.ToString()
    }
    interface IDisposable with member _.Dispose() = (temp :> IDisposable).Dispose()

let private attestation (producer: byte array) =
    let p = node producer
    let input = at "input" p
    let evidence = (at "evidence" p).AsArray() |> Seq.choose Option.ofObj |> Seq.toList
    let hash id = evidence |> List.find (fun e -> (at "id" e).GetValue<string>() = id) |> at "sha256" |> fun n -> n.GetValue<string>()
    let contracts = [ for goal in 1 .. 13 -> obj [ "goal", box goal; "sha256", box (hash (sprintf "goal-%02d" goal)) ] ]
    let receipt goal =
        obj [ "goal", box goal; "exitCode", box 0; "producerSha256", box (sha256 producer); "reviewerIdentity", box "independent-reviewer";
              "startedAtUtc", box "2026-10-03T12:00:01Z"; "finishedAtUtc", box "2026-10-03T12:00:02Z"; "command", box "dotnet package-probe.dll" ] |> encode |> reference
    node (encode (obj [ "schemaVersion", box 1; "kind", box "attestation"; "createdAtUtc", box "2026-10-03T12:00:03Z";
                        "reviewer", box (obj [ "name", box "Named reviewer"; "identity", box "independent-reviewer"; "readOnly", box true ]);
                        "producerSha256", box (sha256 producer); "candidateCommit", box ((at "candidateCommit" input).GetValue<string>());
                        "contracts", box contracts; "verdict", box "APPROVE"; "auditStatus", box "COMPLETE"; "productAcceptance", box "PASS";
                        "goals", box [ for goal in 1 .. 13 -> obj [ "goal", box goal; "status", box "PASS"; "evidenceIds", box [ "local-proof" ] ] ];
                        "replays", box [ for goal in [ 4; 9 ] -> obj [ "goal", box goal; "inputEvidenceIds", box [ "FunnySharp.nupkg" ]; "receipt", box (receipt goal) ] ];
                        "report", box (reference (bytes "actual retained reviewer report\n")) ]))

// A small local P/A seam: no producer rerun or network is needed to test transport.
type private StageBundleFixture(?root: string, ?bodyLength: int) =
    let temp = new TempDirectory()
    let root = defaultArg root temp.Path
    let producerDirectory = Path.Combine(root, "original")
    let files = Dictionary<string, byte array>()
    do
        Directory.CreateDirectory producerDirectory |> ignore
        for id in [ for goal in 1 .. 13 -> sprintf "goal-%02d" goal ] @ [ "local-proof"; "FunnySharp.nupkg" ] do
            files.Add("payload/" + id, bytes (id + "\r\n"))
        files.Add("http/0000.body", Array.init (defaultArg bodyLength 8193) (fun i -> byte (i % 251)))
        files.Add("http/0001.body", [||])
        let evidence = [ for pair in files do if pair.Key.StartsWith "payload/" then yield obj [ "id", box (pair.Key.Substring 8); "path", box pair.Key; "sha256", box (sha256 pair.Value) ] ]
        let producer = encode (obj [ "schemaVersion", box 1; "kind", box "producer"; "createdAtUtc", box (now.ToString("O"));
                                    "input", box (obj [ "candidateCommit", box candidate; "producerIdentity", box "producer";
                                                        "contracts", box [ for goal in 1 .. 13 -> obj [ "goal", box goal; "evidenceId", box (sprintf "goal-%02d" goal) ] ] ]);
                                    "evidence", box evidence;
                                    "http", box [ for pair in files do if pair.Key.StartsWith "http/" then yield obj [ "bodyPath", box pair.Key; "bodySha256", box (sha256 pair.Value) ] ] ])
        // Noncanonical whitespace proves P/A are transported, not serialized again.
        let producer = Array.concat [ bytes " \r\n"; producer; bytes "\r\n" ]
        files.Add("P.json", producer)
        files.Add("A.json", Array.concat [ bytes "\r\n"; bytes ((attestation producer).ToJsonString()); bytes "\r\n" ])
        for pair in files do
            let path = Path.Combine(producerDirectory, pair.Key)
            Directory.CreateDirectory(Path.GetDirectoryName path |> Option.ofObj |> Option.get) |> ignore
            File.WriteAllBytes(path, pair.Value)
    member _.Root = root
    member _.Files = files
    member _.LocalInput =
        let reference name = obj [ "path", box (Path.Combine(producerDirectory, name)); "sha256", box (sha256 files.[name]) ]
        node (encode (obj [ "repository", box "owner/repo"; "producer", box (reference "P.json"); "attestation", box (reference "A.json") ]))
    member _.Bundle() =
        let entries =
            [ for index, pair in files |> Seq.indexed do
                let chunks = if pair.Value.Length = 0 then [| [||] |] else Array.chunkBySize 4096 pair.Value
                let parts =
                    [ for part, content in chunks |> Array.indexed do
                        let path = sprintf "parts/%04d/%04d.stage-part" index part
                        let full = Path.Combine(root, path)
                        Directory.CreateDirectory(Path.GetDirectoryName full |> Option.ofObj |> Option.get) |> ignore
                        File.WriteAllBytes(full, content)
                        yield obj [ "path", box path; "length", box content.LongLength; "sha256", box (sha256 content) ] ]
                yield obj [ "path", box pair.Key; "length", box pair.Value.LongLength; "sha256", box (sha256 pair.Value); "parts", box parts ] ]
        node (encode (obj [ "repository", box "owner/repo"; "bundle", box (obj [ "schemaVersion", box 1; "files", box entries ]) ]))
    member _.Run(mode, input: JsonNode, output, ?payload: bool) = task {
        use stdout = new StringWriter()
        use stderr = new StringWriter()
        let inputBytes = bytes (input.ToJsonString())
        let path = Path.Combine(root, "input.json")
        File.WriteAllBytes(path, inputBytes)
        use compressed = new MemoryStream()
        do
            use gzip = new GZipStream(compressed, CompressionMode.Compress, true)
            gzip.Write inputBytes
        let collaborators =
            { Get = fun _ -> failwith "Local bundle transport must not request remote state."
              UtcNow = fun () -> now.AddSeconds 10.
              Environment = fun name -> if name = "RELEASE_PROVENANCE_PAYLOAD" then Some (Convert.ToBase64String(compressed.ToArray())) else None }
        let! code = mainWith stdout stderr collaborators ([ "-Mode"; mode; "-OutputDirectory"; output ] @ if defaultArg payload false then [] else [ "-InputPath"; path ])
        return code, stderr.ToString()
    }
    member _.AssertStage(output) =
        for pair in files do Assert.Equal<byte>(pair.Value, File.ReadAllBytes(Path.Combine(output, pair.Key)))
        let report = node files.["A.json"] |> at "report" |> at "base64" |> fun value -> Convert.FromBase64String(value.GetValue<string>())
        Assert.Equal<byte>(report, File.ReadAllBytes(Path.Combine(output, "review.md")))
        Assert.False(Directory.Exists(Path.Combine(output, "transport")))
    interface IDisposable with member _.Dispose() = (temp :> IDisposable).Dispose()

type ReleaseProvenanceTests() =
    [<Fact>]
    member _.StageBundlePreservesLegacyRootArtifactAndInlineInput() = task {
        use fixture = new Fixture()
        let original = Path.Combine(fixture.Root, "producer")
        let! freezeCode, freezeError = fixture.Run("freeze-producer", original, fixture.Input)
        Assert.True((freezeCode = 0), freezeError)
        let producer = File.ReadAllBytes(Path.Combine(original, "P.json"))
        let attestation = bytes ((attestation producer).ToJsonString())
        let entries = Directory.GetFiles(original, "*", SearchOption.AllDirectories) |> Array.map (fun path -> Path.GetRelativePath(original, path).Replace('\\', '/'), File.ReadAllBytes path) |> Array.toList
        fixture.Artifact(200L, 30L, archive entries)
        fixture.CurrentTime <- now.AddSeconds 10.
        let input = node (encode (obj [ "repository", box "owner/repo"; "producer", box (obj [ "artifactId", box 200L; "entry", box "P.json"; "sha256", box (sha256 producer) ]); "attestation", box (reference attestation) ]))
        let output = Path.Combine(fixture.Root, "stage")
        let! code, error = fixture.Run("stage-attestation", output, input, payload = true)
        Assert.True((code = 0), error)
        for path, content in entries |> List.filter (fun (path, _) -> path <> ".claim") do Assert.Equal<byte>(content, File.ReadAllBytes(Path.Combine(output, path)))
        Assert.Equal<byte>(attestation, File.ReadAllBytes(Path.Combine(output, "A.json")))
        Assert.Contains(api + "/actions/artifacts/200/zip", fixture.Requested)
    }

    [<Theory>]
    [<InlineData(0)>]
    [<InlineData(33554432)>]
    [<InlineData(33554433)>]
    member _.StageBundlePackerHonorsPartBoundary(length: int) = task {
        use fixture = new StageBundleFixture(bodyLength = length)
        let packed = Path.Combine(fixture.Root, "packed")
        let! code, error = fixture.Run("pack-stage-bundle", fixture.LocalInput, packed)
        Assert.True((code = 0), error)
        let descriptor = node (File.ReadAllBytes(Path.Combine(packed, "input.json")))
        let files = descriptor |> at "bundle" |> at "files" |> fun value -> value.AsArray()
        Assert.Equal(fixture.Files.Count, files.Count)
        let body = files |> Seq.choose Option.ofObj |> Seq.find (fun file -> (at "path" file).GetValue<string>() = "http/0000.body")
        let parts = (at "parts" body).AsArray() |> Seq.choose Option.ofObj |> Seq.toList
        Assert.Equal((if length <= 33554432 then 1 else 2), parts.Length)
        Assert.Equal(int64 (min length 33554432), (at "length" parts.Head).GetValue<int64>())
        for part in parts do
            let content = File.ReadAllBytes(Path.Combine(packed, (at "path" part).GetValue<string>()))
            Assert.Equal(content.LongLength, (at "length" part).GetValue<int64>())
            Assert.Equal(sha256 content, (at "sha256" part).GetValue<string>())
        // Resolve relative to the descriptor, not the process working directory.
        use stdout = new StringWriter()
        use stderr = new StringWriter()
        let collaborators = { defaultCollaborators with Get = fun _ -> failwith "Unexpected network access." }
        let! stageCode = mainWith stdout stderr collaborators [ "-Mode"; "stage-attestation"; "-InputPath"; Path.Combine(packed, "input.json"); "-OutputDirectory"; Path.Combine(fixture.Root, "stage") ]
        Assert.True((stageCode = 0), stderr.ToString())
        fixture.AssertStage(Path.Combine(fixture.Root, "stage"))
        let! again, _ = fixture.Run("pack-stage-bundle", fixture.LocalInput, packed)
        Assert.NotEqual(0, again)
        // A declared or actual part over the boundary must fail before final output.
        if length = 33554433 then
            let first = parts.Head
            let full = Path.Combine(packed, (at "path" first).GetValue<string>())
            File.WriteAllBytes(full, fixture.Files.["http/0000.body"])
            first.["length"] <- num (int64 length)
            first.["sha256"] <- str (sha256 fixture.Files.["http/0000.body"])
            (at "parts" body).AsArray().RemoveAt 1
            File.WriteAllText(Path.Combine(packed, "input.json"), descriptor.ToJsonString())
            let rejected = Path.Combine(fixture.Root, "oversized")
            let! oversized = mainWith stdout stderr collaborators [ "-Mode"; "stage-attestation"; "-InputPath"; Path.Combine(packed, "input.json"); "-OutputDirectory"; rejected ]
            Assert.NotEqual(0, oversized)
            Assert.False(File.Exists(Path.Combine(rejected, "P.json")))
    }

    [<Theory>]
    [<InlineData("base64")>]
    [<InlineData("artifact")>]
    [<InlineData("hash")>]
    [<InlineData("attachment")>]
    [<InlineData("reparse")>]
    [<InlineData("inline")>]
    member _.StageBundlePackerRejectsNonlocalOrChangedSources(kind: string) = task {
        use fixture = new StageBundleFixture()
        let input = fixture.LocalInput
        let producer = at "producer" input
        match kind with
        | "base64" -> input.["producer"] <- node (encode (reference fixture.Files.["P.json"]))
        | "artifact" -> producer.AsObject().Remove("path") |> ignore; producer.["artifactId"] <- num 100L; producer.["entry"] <- str "P.json"
        | "hash" -> producer.["sha256"] <- str source
        | "attachment" -> File.WriteAllText(Path.Combine(fixture.Root, "original/http/0000.body"), "changed")
        | "reparse" ->
            let path = (at "path" producer).GetValue<string>()
            File.Move(path, path + ".real")
            File.CreateSymbolicLink(path, path + ".real") |> ignore
        | _ -> ()
        let output = Path.Combine(fixture.Root, "packed")
        let! code, _ = fixture.Run("pack-stage-bundle", input, output, payload = (kind = "inline"))
        Assert.NotEqual(0, code)
        Assert.False(File.Exists(Path.Combine(output, "input.json")))
    }

    [<Fact>]
    member _.StageBundleCliPackStageRoundTrip() = task {
        let retainedRoot = Environment.GetEnvironmentVariable "FUNNYSHARP_STAGE_BUNDLE_FIXTURE" |> Option.ofObj
        match retainedRoot with
        | Some root -> Assert.False(Directory.Exists root, "CLI fixture output must be a new directory.")
        | None -> ()
        use fixture = new StageBundleFixture(?root = retainedRoot, bodyLength = 33554433)
        let script = Path.Combine(fixture.Root, "cli.fsx")
        let assembly = typeof<HttpResponse>.Assembly.Location.Replace("\\", "/")
        File.WriteAllText(script, sprintf "#r @\"%s\"\nexit (FunnySharp.Harness.ReleaseProvenance.main fsi.CommandLineArgs.[1..])\n" assembly)
        let localInput = Path.Combine(fixture.Root, "local.json")
        File.WriteAllText(localInput, fixture.LocalInput.ToJsonString())
        let run mode input output = task {
            let start = ProcessStartInfo("dotnet", UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true)
            for argument in [ "fsi"; "--exec"; script; "-Mode"; mode; "-InputPath"; input; "-OutputDirectory"; output ] do start.ArgumentList.Add argument
            use child = new Process(StartInfo = start)
            Assert.True(child.Start())
            let stdout, stderr = child.StandardOutput.ReadToEndAsync(), child.StandardError.ReadToEndAsync()
            use timeout = new CancellationTokenSource(TimeSpan.FromMinutes 2.)
            try
                do! child.WaitForExitAsync timeout.Token
            with :? OperationCanceledException as error ->
                child.Kill(true)
                raise error
            let! stdout = stdout
            let! stderr = stderr
            File.WriteAllText(Path.Combine(fixture.Root, mode + ".log"), stdout + stderr)
            Assert.True((child.ExitCode = 0), stdout + stderr)
        }
        let packed = Path.Combine(fixture.Root, "packed")
        let staged = Path.Combine(fixture.Root, "staged")
        do! run "pack-stage-bundle" localInput packed
        do! run "stage-attestation" (Path.Combine(packed, "input.json")) staged
        fixture.AssertStage staged
        for pair in fixture.Files do Assert.Equal<byte>(pair.Value, File.ReadAllBytes(Path.Combine(fixture.Root, "original", pair.Key)))
    }

    [<Theory>]
    [<InlineData("valid")>]
    [<InlineData("schema")>]
    [<InlineData("mixed")>]
    [<InlineData("payload")>]
    [<InlineData("missing-file")>]
    [<InlineData("extra-file")>]
    [<InlineData("duplicate-file")>]
    [<InlineData("case-alias")>]
    [<InlineData("file-length")>]
    [<InlineData("file-hash")>]
    [<InlineData("file-traversal")>]
    [<InlineData("missing-part")>]
    [<InlineData("missing-part-entry")>]
    [<InlineData("extra-part")>]
    [<InlineData("duplicate-part")>]
    [<InlineData("part-length")>]
    [<InlineData("negative-length")>]
    [<InlineData("part-hash")>]
    [<InlineData("malformed-hash")>]
    [<InlineData("order")>]
    [<InlineData("part-traversal")>]
    [<InlineData("part-absolute")>]
    [<InlineData("part-backslash")>]
    [<InlineData("part-reparse")>]
    [<InlineData("input-reparse")>]
    [<InlineData("part-bytes")>]
    [<InlineData("actual-length")>]
    [<InlineData("unbound-body")>]
    [<InlineData("goal04")>]
    [<InlineData("goal09")>]
    member _.StageBundleChecksExactFilesAndOrderedParts(kind: string) = task {
        use fixture = new StageBundleFixture()
        let input = fixture.Bundle()
        let! validCode, validError = fixture.Run("stage-attestation", input, Path.Combine(fixture.Root, "valid"))
        Assert.True((validCode = 0), validError)
        fixture.AssertStage(Path.Combine(fixture.Root, "valid"))
        let bundle = at "bundle" input
        let files = (at "files" bundle).AsArray()
        let body = files |> Seq.choose Option.ofObj |> Seq.find (fun file -> (at "path" file).GetValue<string>() = "http/0000.body")
        let parts = (at "parts" body).AsArray()
        let part = parts.[0] |> present
        let partPath = Path.Combine(fixture.Root, (at "path" part).GetValue<string>())
        match kind with
        | "schema" -> bundle.["schemaVersion"] <- num 2L
        | "mixed" -> input.["producer"] <- (at "producer" fixture.LocalInput).DeepClone()
        | "missing-file" -> files.RemoveAt 0
        | "extra-file" ->
            let extra = body.DeepClone()
            extra.["path"] <- str "http/9999.body"
            for p in (at "parts" extra).AsArray() |> Seq.choose Option.ofObj do
                let old = Path.Combine(fixture.Root, (at "path" p).GetValue<string>())
                let path = (at "path" p).GetValue<string>() + ".extra"
                File.Copy(old, Path.Combine(fixture.Root, path))
                p.["path"] <- str path
            files.Add extra
        | "duplicate-file" -> files.Add(body.DeepClone())
        | "case-alias" -> let extra = body.DeepClone() in extra.["path"] <- str "HTTP/0000.body"; files.Add extra
        | "file-length" -> body.["length"] <- num 8194L
        | "file-hash" -> body.["sha256"] <- str source
        | "file-traversal" -> body.["path"] <- str "../escape.body"
        | "missing-part" -> File.Delete partPath
        | "missing-part-entry" -> parts.RemoveAt 1
        | "extra-part" ->
            File.WriteAllBytes(Path.Combine(fixture.Root, "extra.stage-part"), [||])
            parts.Add(node (encode (obj [ "path", box "extra.stage-part"; "length", box 0L; "sha256", box (sha256 [||]) ])))
        | "duplicate-part" -> (parts.[1] |> present).["path"] <- (at "path" part).DeepClone()
        | "part-length" -> part.["length"] <- num 4095L
        | "negative-length" -> part.["length"] <- num -1L
        | "part-hash" -> part.["sha256"] <- str source
        | "malformed-hash" -> part.["sha256"] <- str "invalid"
        | "order" -> parts.RemoveAt 0; parts.Add part
        | "part-traversal" -> part.["path"] <- str "../outside.stage-part"
        | "part-absolute" -> part.["path"] <- str partPath
        | "part-backslash" -> part.["path"] <- str "parts\\escape.stage-part"
        | "part-reparse" ->
            let parent = Path.GetDirectoryName partPath |> Option.ofObj |> Option.get
            Directory.Move(parent, parent + "-real")
            Directory.CreateSymbolicLink(parent, parent + "-real") |> ignore
        | "input-reparse" ->
            let path = Path.Combine(fixture.Root, "input.json")
            File.Move(path, path + ".real")
            File.CreateSymbolicLink(path, path + ".real") |> ignore
        | "part-bytes" -> File.WriteAllBytes(partPath, Array.create 4096 0uy)
        | "actual-length" -> File.AppendAllText(partPath, "x")
        | "unbound-body" ->
            File.WriteAllBytes(partPath, Array.create 4096 0uy)
            part.["sha256"] <- str (sha256 (File.ReadAllBytes partPath))
            let content = parts |> Seq.choose Option.ofObj |> Seq.collect (fun p -> File.ReadAllBytes(Path.Combine(fixture.Root, (at "path" p).GetValue<string>()))) |> Seq.toArray
            body.["sha256"] <- str (sha256 content)
        | "goal04" | "goal09" ->
            let a = node fixture.Files.["A.json"]
            (at "replays" a).AsArray().RemoveAt(if kind = "goal04" then 0 else 1)
            fixture.Files.["A.json"] <- bytes (a.ToJsonString())
            let replacement = fixture.Bundle() |> at "bundle" |> at "files" |> fun f -> f.AsArray() |> Seq.choose Option.ofObj |> Seq.find (fun file -> (at "path" file).GetValue<string>() = "A.json")
            let old = files |> Seq.choose Option.ofObj |> Seq.find (fun file -> (at "path" file).GetValue<string>() = "A.json")
            files.[files.IndexOf old] <- replacement.DeepClone()
        | _ -> ()
        let output = Path.Combine(fixture.Root, "stage")
        let! code, error = fixture.Run("stage-attestation", input, output, payload = (kind = "payload"))
        if kind = "valid" then
            Assert.True((code = 0), error)
            fixture.AssertStage output
        else
            Assert.NotEqual(0, code)
            for name in [ "P.json"; "A.json"; "review.md" ] do Assert.False(File.Exists(Path.Combine(output, name)))
    }

    [<Theory>]
    [<InlineData(false)>]
    [<InlineData(true)>]
    member _.FreezeBindsDifferentRawHostFingerprintsToCandidateBlobs(crlfBlob: bool) = task {
        use fixture = new Fixture(differentHostFingerprints = true)
        if crlfBlob then
            for path, content in sourceFiles true |> List.filter (fun (path, _) -> List.contains path metadataPaths) do
                fixture.ChangeResponse(contentsUrl path, fun blob ->
                    blob.["content"] <- str (Convert.ToBase64String content)
                    blob.["size"] <- num (int64 content.Length)
                    blob.["sha"] <- str (gitBlobSha content))
        // The canonical Windows host is selected by identity, not input order.
        let hosts = (at "hosts" fixture.Input).AsArray()
        let first = hosts.[0] |> present
        hosts.RemoveAt 0
        hosts.Add first
        let output = Path.Combine(fixture.Root, "producer")
        let! code, error = fixture.Run("freeze-producer", output, fixture.Input)
        Assert.True((code = 0), error)
        let producer = node (File.ReadAllBytes(Path.Combine(output, "P.json")))
        Assert.Equal((at "sourceFingerprint" fixture.Input).GetValue<string>(), (at "input" producer |> at "sourceFingerprint").GetValue<string>())
        let retained rid = File.ReadAllBytes(Path.Combine(output, "payload", rid + "-execution"))
        for rid in [ "win-x64"; "linux-x64"; "osx-arm64" ] do
            let original = (at "evidence" fixture.Input).AsArray() |> Seq.choose Option.ofObj |> Seq.find (fun entry -> (at "id" entry).GetValue<string>() = rid + "-execution")
            Assert.Equal<byte>(Convert.FromBase64String((at "bytes" original |> at "base64").GetValue<string>()), retained rid)
        let rawDigest rid = (node (retained rid) |> at "sourceFingerprintBefore" |> at "digest").GetValue<string>()
        Assert.NotEqual<string>(rawDigest "win-x64", rawDigest "linux-x64")
        Assert.Equal(rawDigest "linux-x64", rawDigest "osx-arm64")
        for path in metadataPaths do
            let url = contentsUrl path
            Assert.Equal(1, fixture.Requested |> Seq.filter ((=) url) |> Seq.length)
            let captured = (at "http" producer).AsArray() |> Seq.choose Option.ofObj |> Seq.find (fun entry -> (at "url" entry).GetValue<string>() = url)
            Assert.Equal<byte>(fixture.Responses.[url].Body, File.ReadAllBytes(Path.Combine(output, (at "bodyPath" captured).GetValue<string>())))
    }

    [<Theory>]
    [<InlineData("schema")>]
    [<InlineData("algorithm")>]
    [<InlineData("file-count")>]
    [<InlineData("digest")>]
    [<InlineData("file-hash")>]
    [<InlineData("extra-field")>]
    [<InlineData("missing-file")>]
    [<InlineData("extra-file")>]
    [<InlineData("duplicate-path")>]
    [<InlineData("order")>]
    [<InlineData("during-run")>]
    [<InlineData("canonical-scalar")>]
    member _.RejectsMalformedOrChangedRawFingerprints(kind: string) = task {
        use fixture = new Fixture(differentHostFingerprints = true)
        let before = fingerprint (sourceFiles false)
        let files = (at "files" before).AsArray()
        match kind with
        | "schema" -> before.["schemaVersion"] <- num 2L
        | "algorithm" -> before.["algorithm"] <- str "sha1"
        | "file-count" -> before.["fileCount"] <- num 999L
        | "digest" -> before.["digest"] <- str source
        | "file-hash" -> (files.[0] |> present).["sha256"] <- str "invalid"
        | "extra-field" -> before.["normalizedDigest"] <- str source
        | "missing-file" -> files.RemoveAt(files.Count - 1)
        | "extra-file" -> files.Add(node (encode (obj [ "path", box "zzz-new-file"; "sha256", box source ])))
        | "duplicate-path" -> files.Add((files.[0] |> present).DeepClone())
        | "order" -> let first = files.[0] |> present in files.RemoveAt 0; files.Add first
        | "canonical-scalar" -> fixture.Input.["sourceFingerprint"] <- (at "digest" before).DeepClone()
        | _ -> ()
        if List.contains kind [ "missing-file"; "extra-file"; "duplicate-path"; "order" ] then
            before.["fileCount"] <- num (int64 files.Count)
            let raw = files |> Seq.choose Option.ofObj |> Seq.map (fun file -> (at "path" file).GetValue<string>() + "\000" + (at "sha256" file).GetValue<string>() + "\n") |> String.concat ""
            before.["digest"] <- str (sha256 (bytes raw))
        fixture.ReplaceHostExecution("linux-x64", fun execution ->
            execution.["sourceFingerprintBefore"] <- before.DeepClone()
            execution.["sourceFingerprintAfter"] <- if kind = "during-run" then fingerprint (sourceFiles true) else before.DeepClone())
        let output = Path.Combine(fixture.Root, "rejected")
        let! code, _ = fixture.Run("freeze-producer", output, fixture.Input)
        Assert.Equal(1, code)
        Assert.False(File.Exists(Path.Combine(output, "P.json")))
        Assert.DoesNotContain(feed, fixture.Requested)
    }

    [<Theory>]
    [<InlineData(".gitattributes")>]
    [<InlineData("src/FunnySharp/Option.cs")>]
    [<InlineData("tests/FunnySharp.Tests/OptionTests.cs")>]
    [<InlineData("eng/harness/ReleaseRun.fs")>]
    [<InlineData("eng/performance/baseline.json")>]
    [<InlineData("Directory.Build.props")>]
    [<InlineData("src/FunnySharp/packages.lock.json")>]
    [<InlineData("README.md")>]
    member _.RejectsRecomputedFingerprintsWithUnapprovedFileChanges(path: string) = task {
        use fixture = new Fixture(differentHostFingerprints = true)
        let changed = sourceFiles false |> List.map (fun (name, content) -> name, if name = path then bytes (name + " changed\n") else content) |> fingerprint
        fixture.ReplaceHostExecution("linux-x64", fun execution ->
            execution.["sourceFingerprintBefore"] <- changed.DeepClone()
            execution.["sourceFingerprintAfter"] <- changed.DeepClone())
        let output = Path.Combine(fixture.Root, "rejected")
        let! code, _ = fixture.Run("freeze-producer", output, fixture.Input)
        Assert.Equal(1, code)
        Assert.False(File.Exists(Path.Combine(output, "P.json")))
        Assert.DoesNotContain(feed, fixture.Requested)
    }

    [<Theory>]
    [<InlineData("candidate")>]
    [<InlineData("path")>]
    [<InlineData("type")>]
    [<InlineData("sha")>]
    [<InlineData("size")>]
    [<InlineData("encoding")>]
    [<InlineData("base64")>]
    [<InlineData("content")>]
    [<InlineData("utf8")>]
    [<InlineData("binary")>]
    [<InlineData("bare-cr")>]
    [<InlineData("redirect")>]
    [<InlineData("unavailable")>]
    member _.RejectsUnboundOrNonTextCandidateBlobs(kind: string) = task {
        use fixture = new Fixture(differentHostFingerprints = true)
        let path = ".gitattributes"
        let url = contentsUrl path
        fixture.ChangeResponse(url, fun blob ->
            match kind with
            | "candidate" -> blob.["url"] <- str (url.Replace(candidate, String.replicate 40 "c"))
            | "path" -> blob.["path"] <- str "LICENSE"
            | "type" -> blob.["type"] <- str "symlink"
            | "sha" -> blob.["sha"] <- str (String.replicate 40 "c")
            | "size" -> blob.["size"] <- num 999L
            | "encoding" -> blob.["encoding"] <- str "none"
            | "base64" -> blob.["content"] <- str "not base64"
            | "content" | "utf8" | "binary" | "bare-cr" ->
                let content = match kind with | "utf8" -> [| 0xffuy |] | "binary" -> bytes "a\000b\n" | "bare-cr" -> bytes "a\rb\n" | _ -> bytes "different candidate content\n"
                blob.["content"] <- str (Convert.ToBase64String content)
                blob.["size"] <- num (int64 content.Length)
                blob.["sha"] <- str (gitBlobSha content)
                if kind <> "content" then
                    for rid, crlf in [ "win-x64", true; "linux-x64", false; "osx-arm64", false ] do
                        let hostBytes = if crlf then bytes (Encoding.UTF8.GetString(content).Replace("\r\n", "\n").Replace("\n", "\r\n")) else content
                        let host = sourceFiles crlf |> List.map (fun (name, original) -> name, if name = path then hostBytes else original) |> fingerprint
                        if rid = "win-x64" then fixture.Input.["sourceFingerprint"] <- (at "digest" host).DeepClone()
                        fixture.ReplaceHostExecution(rid, fun execution ->
                            execution.["sourceFingerprintBefore"] <- host.DeepClone()
                            execution.["sourceFingerprintAfter"] <- host.DeepClone())
            | _ -> ())
        if kind = "redirect" then fixture.Responses.[url] <- { fixture.Responses.[url] with EffectiveUrl = url.Replace(candidate, "main") }
        if kind = "unavailable" then fixture.Responses.[url] <- { fixture.Responses.[url] with StatusCode = 404 }
        let output = Path.Combine(fixture.Root, "rejected")
        let! code, _ = fixture.Run("freeze-producer", output, fixture.Input)
        Assert.Equal(1, code)
        Assert.Contains(url, fixture.Requested)
        Assert.False(File.Exists(Path.Combine(output, "P.json")))
        Assert.DoesNotContain(feed, fixture.Requested)
    }

    [<Theory>]
    [<InlineData("success")>]
    [<InlineData("wrong-url")>]
    [<InlineData("wrong-uploaded-bytes")>]
    [<InlineData("missing-uploaded-report")>]
    member _.StagesExactBytesAndIndexesOnlyActualUploadedArtifact(kind: string) = task {
        use fixture = new Fixture()
        let producerDir = Path.Combine(fixture.Root, "producer")
        let! freezeCode, freezeError = fixture.Run("freeze-producer", producerDir, fixture.Input)
        Assert.True((freezeCode = 0), freezeError)
        let producer = File.ReadAllBytes(Path.Combine(producerDir, "P.json"))
        let a = attestation producer
        let aBytes = bytes (a.ToJsonString())
        fixture.CurrentTime <- now.AddSeconds 10.
        let input = node (encode (obj [ "repository", box "owner/repo"; "producer", box (obj [ "path", box (Path.Combine(producerDir, "P.json")); "sha256", box (sha256 producer) ]); "attestation", box (reference aBytes) ]))
        let staged = Path.Combine(fixture.Root, "stage")
        let! stageCode, stageError = fixture.Run("stage-attestation", staged, input)
        Assert.True((stageCode = 0), stageError)
        Assert.Equal<byte>(producer, File.ReadAllBytes(Path.Combine(staged, "P.json")))
        Assert.Equal<byte>(aBytes, File.ReadAllBytes(Path.Combine(staged, "A.json")))
        let entries =
            Directory.GetFiles(staged, "*", SearchOption.AllDirectories)
            |> Array.map (fun path -> Path.GetRelativePath(staged, path).Replace('\\', '/'), File.ReadAllBytes path)
            |> Array.toList
            |> List.filter (fun (path, _) -> kind <> "missing-uploaded-report" || path <> "review.md")
            |> List.map (fun (path, data) -> path, if kind = "wrong-uploaded-bytes" && path = "A.json" then bytes "{}" else data)
        fixture.RunMetadata 30L
        fixture.Artifact(200L, 30L, archive entries)
        fixture.Environment.["GITHUB_REPOSITORY"] <- "owner/repo"
        fixture.Environment.["GITHUB_RUN_ID"] <- "30"
        fixture.Environment.["GITHUB_RUN_ATTEMPT"] <- "1"
        fixture.Environment.["GITHUB_SHA"] <- candidate
        let summary = Path.Combine(fixture.Root, "summary.md")
        fixture.Environment.["GITHUB_STEP_SUMMARY"] <- summary
        let url = if kind = "wrong-url" then "https://github.com/owner/repo/actions/runs/31/artifacts/200" else "https://github.com/owner/repo/actions/runs/30/artifacts/200"
        let! code, error = fixture.Run("write-index", staged, input, artifactUrl = url)
        if kind = "success" then
            Assert.True((code = 0), error)
            let index = node (File.ReadAllBytes(Path.Combine(staged, "index.json")))
            Assert.Equal(sha256 aBytes, (at "attestationSha256" index).GetValue<string>())
            Assert.Equal(url, (at "artifactUrl" index).GetValue<string>())
            Assert.True(File.Exists summary)
            let! again, _ = fixture.Run("write-index", staged, input, artifactUrl = url)
            Assert.Equal(1, again)
        else
            Assert.Equal(1, code)
            Assert.False(File.Exists(Path.Combine(staged, "index.json")))
            Assert.False(File.Exists summary)
        Assert.Equal<byte>(producer, File.ReadAllBytes(Path.Combine(staged, "P.json")))
        Assert.Equal<byte>(aBytes, File.ReadAllBytes(Path.Combine(staged, "A.json")))
        let! restage, _ = fixture.Run("stage-attestation", staged, input)
        Assert.Equal(1, restage)
    }

    [<Fact>]
    member _.FreezeJoinsActualReadbacksAndPreservesRawFeedBytes() = task {
        use fixture = new Fixture()
        let output = Path.Combine(fixture.Root, "producer")
        let! code, error = fixture.Run("freeze-producer", output, fixture.Input, payload = true)
        Assert.True((code = 0), error)
        let producer = node (File.ReadAllBytes(Path.Combine(output, "P.json")))
        let checks = (at "finalFeedChecks" producer).AsArray()
        Assert.Equal(2, checks.Count)
        let first = checks.[0] |> present
        Assert.Equal(404L, (at "statusCode" first).GetValue<int64>())
        Assert.Equal(sha256 (bytes "actual origin not found response"), (at "rawBodySha256" first).GetValue<string>())
        Assert.NotEqual<string>((at "rawBodySha256" first).GetValue<string>(), (at "normalizedVersionsSha256" first).GetValue<string>())
        Assert.Equal<string list>([ feed; flat + "funnysharp/index.json"; flat + "funnysharp.aspnetcore/index.json" ], fixture.Requested |> Seq.toList |> List.rev |> List.take 3 |> List.rev)
        Assert.False(File.Exists(Path.Combine(output, "A.json")))
        let original = File.ReadAllBytes(Path.Combine(output, "P.json"))
        let! again, _ = fixture.Run("freeze-producer", output, fixture.Input)
        Assert.Equal(1, again)
        Assert.Equal<byte>(original, File.ReadAllBytes(Path.Combine(output, "P.json")))
    }

    [<Theory>]
    [<InlineData("valid")>]
    [<InlineData("enabled")>]
    [<InlineData("wrong-actor")>]
    [<InlineData("wrong-reason")>]
    member _.ReadOnlyMergeControlProvesNamedCheckDenial(kind: string) = task {
        use fixture = new Fixture()
        fixture.ReplaceEvidence("denial-0-body", fun value ->
            value.["actor"] <- str (if kind = "wrong-actor" then "someone-else" else "ordinary")
            value.["headSha"] <- str (String.replicate 40 "1")
            value.["targetBranch"] <- str "main"
            value.["mergeControlDisabled"] <- boolean (kind <> "enabled")
            value.["blockingReason"] <- str (if kind = "wrong-reason" then "Review is missing" else "Required status check failed: release / win-x64")
            value.["url"] <- str "https://github.com/owner/repo/pull/12")
        fixture.ReplaceEvidence("denial-0", fun value ->
            value.["method"] <- str "disabled-merge-control"
            value.["statusCode"] <- num 200L
            let bodyReference =
                (at "evidence" fixture.Input).AsArray()
                |> Seq.choose Option.ofObj
                |> Seq.find (fun entry -> (at "id" entry).GetValue<string>() = "denial-0-body")
            value.["responseSha256"] <- str ((at "sha256" (at "bytes" bodyReference)).GetValue<string>()))
        let! code, error = fixture.Run("freeze-producer", Path.Combine(fixture.Root, "producer"), fixture.Input)
        if kind = "valid" then Assert.True((code = 0), error) else Assert.Equal(1, code)
    }

    [<Theory>]
    [<InlineData("current")>]
    [<InlineData("win-artifact")>]
    [<InlineData("intel-artifact")>]
    [<InlineData("outcome")>]
    [<InlineData("execution")>]
    [<InlineData("preflight")>]
    member _.RerunUsesOnlySelectedAttemptEvidence(kind: string) = task {
        use fixture = new Fixture(productionAttempt = 2L)
        let evidenceEntries () = (at "evidence" fixture.Input).AsArray() |> Seq.choose Option.ofObj |> Seq.toList
        let evidenceHash id =
            evidenceEntries ()
            |> List.find (fun entry -> (at "id" entry).GetValue<string>() = id)
            |> at "bytes" |> at "sha256" |> fun value -> value.GetValue<string>()
        if kind = "win-artifact" || kind = "intel-artifact" then
            let id, name = if kind = "win-artifact" then 100, "canonical-candidate-10-1" else 103, "release-evidence-osx-x64-10-1"
            fixture.ChangeResponse(api + sprintf "/actions/artifacts/%d" id, fun value -> value.["name"] <- str name)
        elif kind <> "current" then
            fixture.ReplaceEvidence("win-x64-" + kind, fun value -> value.["attemptId"] <- str "10-1-win-x64")
            if kind = "preflight" then
                fixture.ReplaceEvidence("win-x64-execution", fun value -> value.["versionPreflightSha256"] <- str (evidenceHash "win-x64-preflight"))
            if kind <> "outcome" then
                fixture.ReplaceEvidence("win-x64-outcome", fun value -> (at "executionEvidence" value).["sha256"] <- str (evidenceHash "win-x64-execution"))
            let retainedBytes =
                evidenceEntries ()
                |> List.map (fun entry ->
                    (at "id" entry).GetValue<string>(),
                    Convert.FromBase64String((at "base64" (at "bytes" entry)).GetValue<string>()))
            fixture.Artifact(100L, 10L, archive retainedBytes)
        let output = Path.Combine(fixture.Root, "producer")
        let! code, error = fixture.Run("freeze-producer", output, fixture.Input)
        if kind = "current" then Assert.True((code = 0), error) else Assert.Equal(1, code)
    }

    [<Theory>]
    [<InlineData("missing")>]
    [<InlineData("failed")>]
    [<InlineData("wrong-name")>]
    [<InlineData("candidate")>]
    [<InlineData("app")>]
    [<InlineData("run")>]
    [<InlineData("pagination")>]
    member _.RejectsIncompleteOrUntrustedActualJobs(kind: string) = task {
        use fixture = new Fixture()
        let url = api + "/actions/runs/10/attempts/1/jobs?per_page=100"
        if kind = "app" then fixture.ChangeResponse(api + "/check-runs/100", fun value -> (at "app" value).["id"] <- num 1L)
        else fixture.ChangeResponse(url, fun value ->
            let jobs = (at "jobs" value).AsArray()
            let job = jobs.[0] |> present
            match kind with
            | "missing" -> jobs.RemoveAt 0; value.["total_count"] <- num 3L
            | "failed" -> job.["conclusion"] <- str "failure"
            | "wrong-name" -> job.["name"] <- str "provenance transport / win-x64"
            | "candidate" -> job.["head_sha"] <- str (String.replicate 40 "d")
            | "run" -> job.["run_id"] <- num 11L
            | _ -> value.["total_count"] <- num 101L)
        let! code, _ = fixture.Run("freeze-producer", Path.Combine(fixture.Root, "rejected"), fixture.Input)
        Assert.Equal(1, code)
        Assert.DoesNotContain(feed, fixture.Requested)
    }

    [<Theory>]
    [<InlineData("strict")>]
    [<InlineData("bypass")>]
    [<InlineData("denial-gap")>]
    [<InlineData("denial-success")>]
    [<InlineData("actor-bypass")>]
    [<InlineData("consumer-hash")>]
    [<InlineData("feed-collision")>]
    [<InlineData("feed-failed")>]
    [<InlineData("later-reference")>]
    member _.RejectsIncompleteProducerEvidence(kind: string) = task {
        use fixture = new Fixture()
        match kind with
        | "strict" -> fixture.ChangeResponse(api + "/rulesets/42", fun value -> ((at "rules" value).AsArray().[0] |> present |> at "parameters").["strict_required_status_checks_policy"] <- boolean false)
        | "bypass" -> fixture.ChangeResponse(api + "/rulesets/42", fun value -> (at "bypass_actors" value).AsArray().Add(node (bytes "{\"actor_id\":1}")))
        | "denial-gap" -> (at "denials" fixture.Input).AsArray().RemoveAt 0
        | "denial-success" -> fixture.ReplaceEvidence("denial-0", fun value -> value.["statusCode"] <- num 200L)
        | "actor-bypass" -> fixture.ReplaceEvidence("actor-0", fun value -> value.["bypass"] <- boolean true)
        | "consumer-hash" -> fixture.ReplaceEvidence("linux-x64-consumer", fun value -> value.["CorePackageSha256"] <- str source)
        | "feed-collision" -> fixture.Responses.[flat + "funnysharp/index.json"] <- { StatusCode = 200; EffectiveUrl = flat; Body = bytes "{\"versions\":[\"0.2.0\"]}" }
        | "feed-failed" -> fixture.Responses.[flat + "funnysharp/index.json"] <- { StatusCode = 503; EffectiveUrl = flat; Body = bytes "unavailable" }
        | _ -> fixture.Input.["attestationSha256"] <- str source
        let output = Path.Combine(fixture.Root, "rejected")
        let! code, _ = fixture.Run("freeze-producer", output, fixture.Input)
        Assert.Equal(1, code)
        Assert.False(File.Exists(Path.Combine(output, "P.json")))
    }

    [<Theory>]
    [<InlineData("producer-hash")>]
    [<InlineData("attestation-hash")>]
    [<InlineData("reviewer")>]
    [<InlineData("goal04")>]
    [<InlineData("goal09")>]
    [<InlineData("later-reference")>]
    [<InlineData("contract")>]
    [<InlineData("before-P")>]
    member _.RejectsInvalidIndependentAttestation(kind: string) = task {
        use fixture = new Fixture()
        let producerDir = Path.Combine(fixture.Root, "producer")
        let! freezeCode, freezeError = fixture.Run("freeze-producer", producerDir, fixture.Input)
        Assert.True((freezeCode = 0), freezeError)
        let producer = File.ReadAllBytes(Path.Combine(producerDir, "P.json"))
        let a = attestation producer
        match kind with
        | "producer-hash" -> a.["producerSha256"] <- str source
        | "reviewer" -> (at "reviewer" a).["identity"] <- str "producer"
        | "goal04" -> (at "replays" a).AsArray().RemoveAt 0
        | "goal09" -> (at "replays" a).AsArray().RemoveAt 1
        | "later-reference" -> a.["indexUrl"] <- str "https://example.test/index"
        | "contract" -> ((at "contracts" a).AsArray().[0] |> present).["sha256"] <- str source
        | "before-P" -> a.["createdAtUtc"] <- str "2026-10-03T11:00:00Z"
        | _ -> ()
        let aRef = node (encode (reference (bytes (a.ToJsonString()))))
        if kind = "attestation-hash" then aRef.["sha256"] <- str source
        let input = node (encode (obj [ "repository", box "owner/repo"; "producer", box (obj [ "path", box (Path.Combine(producerDir, "P.json")); "sha256", box (sha256 producer) ]); "attestation", box aRef ]))
        let output = Path.Combine(fixture.Root, "stage")
        let! code, _ = fixture.Run("stage-attestation", output, input)
        Assert.Equal(1, code)
        Assert.False(File.Exists(Path.Combine(output, "A.json")))
    }
