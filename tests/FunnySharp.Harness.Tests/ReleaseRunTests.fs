module FunnySharp.Harness.Tests.ReleaseRunTests

// Behaviour tests for FunnySharp.Harness.ReleaseRun, the F# port of
// eng/Run-Release.ps1. They pin the observable contract of the release-candidate
// orchestrator: the clean-tracked-tree refusal, attempt immutability, the
// default output layout and receipt/log naming, the child environment, the
// execution-evidence schema-2 success rule, the release-outcome shape, the
// verifier gate and the exact success/failure console lines. Each fixture is a
// self-contained git repository under a disposable temp directory; the
// distribution feed and the verifier are stubbed, so the tests never touch the
// network, pwsh, python, uv or node and never sleep.

open System
open System.IO
open System.Text.Json
open Xunit
open FunnySharp.Harness
open FunnySharp.Harness.Proc
open FunnySharp.Harness.ReleaseRun
open FunnySharp.Harness.Tests.Support

let private feedUrl = "https://feed.example/v3/index.json"

let private serviceJson =
    "{\"version\":\"3.0.0\",\"resources\":[{\"@id\":\"https://feed.example/v3-flatcontainer/\",\"@type\":\"PackageBaseAddress/3.0.0\"}]}"

/// A feed where the candidate version is unpublished (404 package index).
let private versionAbsent (url: string) : HttpResponse =
    if url = feedUrl then
        { StatusCode = 200; Content = serviceJson }
    else
        { StatusCode = 404; Content = "Not Found" }

let private runGit (root: string) (arguments: string list) : ProcessResult =
    runCaptureSync "git" ([ "-C"; root ] @ arguments)

let private git (root: string) (arguments: string list) : unit =
    let result = runGit root arguments

    if result.ExitCode <> 0 then
        failwithf "git %s failed in '%s': %s" (String.Join(" ", arguments)) root result.Stderr

let private gitText (root: string) (arguments: string list) : string =
    (runGit root arguments).Stdout.Trim()

let private runMain
    (root: string)
    (collaborators: Collaborators)
    (arguments: string list)
    : int * string * string =
    use stdout = new StringWriter()
    use stderr = new StringWriter()
    // Console verdicts are compared against captured baselines, so the capture is pinned to LF
    // rather than following the platform's newline.
    stdout.NewLine <- "\n"
    stderr.NewLine <- "\n"
    let exitCode = mainWith stdout stderr root collaborators arguments
    exitCode, stdout.ToString(), stderr.ToString()

let private readJson (path: string) : JsonDocument =
    JsonDocument.Parse(File.ReadAllText path)

let private stringItems (element: JsonElement) : string list =
    element.EnumerateArray()
    |> Seq.map (fun item -> defaultArg (Option.ofObj (item.GetString())) "")
    |> List.ofSeq

/// A committed scratch repository carrying a fixture protocol table.
type private ScratchRepo
    (fullSteps: string list, skippedSteps: string list, steps: (string * string * string list) list) =
    let temp = new TempDirectory()
    let root = Path.Combine(temp.Path, "repo")

    let write (relativePath: string) (text: string) : unit =
        let path = Path.Combine(root, relativePath)

        let directory =
            match Path.GetDirectoryName path with
            | null -> root
            | parent -> parent

        Directory.CreateDirectory directory |> ignore
        File.WriteAllText(path, text)

    let jsonNames (names: string list) =
        names |> List.map (fun name -> "\"" + name + "\"") |> String.concat ", "

    let stepsJson =
        steps
        |> List.map (fun (name, fileName, arguments) ->
            let argsJson = arguments |> List.map (fun arg -> "\"" + arg + "\"") |> String.concat ", "
            sprintf "\"%s\": { \"fileName\": \"%s\", \"workingDirectory\": \"{root}\", \"arguments\": [%s] }" name fileName argsJson)
        |> String.concat ",\n    "

    do
        Directory.CreateDirectory root |> ignore

        write ".gitignore" "artifacts/\nbin/\nobj/\n"
        write "FunnySharp.slnx" "<Solution />\n"
        write "src/FunnySharp/FunnySharp.csproj" "<Project><PropertyGroup><VersionPrefix>0.1.0</VersionPrefix></PropertyGroup></Project>\n"

        write
            "src/FunnySharp.AspNetCore/FunnySharp.AspNetCore.csproj"
            "<Project><PropertyGroup><VersionPrefix>0.1.0</VersionPrefix></PropertyGroup></Project>\n"

        write
            "eng/release-protocol.json"
            (sprintf
                "{\n  \"schemaVersion\": 1,\n  \"modes\": { \"full\": { \"steps\": [%s] }, \"benchmarkSkipped\": { \"steps\": [%s] } },\n  \"steps\": {\n    %s\n  }\n}\n"
                (jsonNames fullSteps)
                (jsonNames skippedSteps)
                stepsJson)

        git root [ "init"; "-q"; "-b"; "main" ]
        git root [ "config"; "user.email"; "fixture@example.com" ]
        git root [ "config"; "user.name"; "Fixture" ]
        git root [ "add"; "-A" ]
        git root [ "commit"; "-q"; "-m"; "fixture" ]

    let commit = gitText root [ "rev-parse"; "HEAD" ]

    member _.Root = root
    member _.Commit = commit
    member _.Attempt(attemptId: string) = Path.Combine(root, "artifacts", "release-candidate", commit, attemptId)

    interface IDisposable with
        member _.Dispose() = (temp :> IDisposable).Dispose()

let private stubVerifier (exitCode: int) (captured: ResizeArray<string list>) : (string list -> string -> ProcessResult) =
    fun arguments _ ->
        captured.Add arguments

        { ExitCode = exitCode
          Stdout = "stub verifier\n"
          Stderr = "" }

let private collaboratorsWith (httpGet: HttpGet) (verifier: (string list -> string -> ProcessResult)) =
    { defaultCollaborators () with
        HttpGet = httpGet
        Verifier = Some verifier }

let private happyRepo () =
    new ScratchRepo([ "hello" ], [ "hello" ], [ "hello", "git", [ "--version" ] ])

// ---- Clean tracked tree precondition (Run-Release.ps1:408-411) ----

[<Fact>]
let CleanTrackedTree_DirtyTrackedFile_RefusedBeforeAnyStep () =
    use repo = happyRepo ()
    File.AppendAllText(Path.Combine(repo.Root, "FunnySharp.slnx"), "<Dirty />\n")
    let code, stdout, stderr = runMain repo.Root (collaboratorsWith versionAbsent (stubVerifier 0 (ResizeArray()))) [ "-RepositoryRoot"; repo.Root; "-AttemptId"; "dirty"; "-DistributionFeed"; feedUrl ]
    Assert.Equal(1, code)
    Assert.Equal("", stdout)
    Assert.Contains("Authoritative release requires a clean tracked tree. Dirty paths:", stderr)
    Assert.Contains("FunnySharp.slnx", stderr)
    Assert.False(Directory.Exists(Path.Combine(repo.Root, "artifacts", "release-candidate")))

// ---- Attempt immutability (ReleaseProtocol.psm1:92-94) ----

[<Fact>]
let ExistingAttemptPath_SecondRun_ThrowsAlreadyExists () =
    use repo = happyRepo ()
    let collaborators = collaboratorsWith versionAbsent (stubVerifier 0 (ResizeArray()))
    let args = [ "-RepositoryRoot"; repo.Root; "-AttemptId"; "immutable"; "-DistributionFeed"; feedUrl ]
    let first, _, _ = runMain repo.Root collaborators args
    Assert.Equal(0, first)
    let second, _, stderr = runMain repo.Root collaborators args
    Assert.Equal(1, second)
    Assert.Contains("already exists and is immutable", stderr)

// ---- Happy path: layout, evidence, outcome, success line ----

[<Fact>]
let HappyPath_WritesReceiptLogsEvidenceAndOutcome () =
    use repo = happyRepo ()
    let stepEnvironment = ResizeArray<Map<string, string>>()

    let runner exe arguments environment workingDirectory =
        if exe = "git" && arguments = [ "--version" ] then
            stepEnvironment.Add environment

        (defaultCollaborators ()).Runner exe arguments environment workingDirectory

    let collaborators =
        { collaboratorsWith versionAbsent (stubVerifier 0 (ResizeArray())) with Runner = runner }

    let code, stdout, stderr = runMain repo.Root collaborators [ "-RepositoryRoot"; repo.Root; "-AttemptId"; "happy"; "-DistributionFeed"; feedUrl ]
    Assert.Equal(0, code)
    Assert.Equal("", stderr)
    let outDir = repo.Attempt "happy"
    Assert.Equal("Release run passed. Execution evidence: " + outDir + "\n", stdout)

    // Receipt/log naming: {0:D2}-<step>.json plus {NN}-<step>.{stdout,stderr}.log.
    Assert.True(File.Exists(Path.Combine(outDir, "receipts", "01-hello.json")))
    Assert.True(File.Exists(Path.Combine(outDir, "logs", "01-hello.stdout.log")))
    Assert.True(File.Exists(Path.Combine(outDir, "logs", "01-hello.stderr.log")))
    use receiptDocument = readJson (Path.Combine(outDir, "receipts", "01-hello.json"))
    let receipt = receiptDocument.RootElement
    Assert.Equal(1, receipt.GetProperty("schemaVersion").GetInt32())
    Assert.Equal("hello", receipt.GetProperty("name").GetString())
    Assert.Equal(0, receipt.GetProperty("exitCode").GetInt32())
    Assert.Equal("logs/01-hello.stdout.log", receipt.GetProperty("standardOutputLog").GetString())

    // Child environment: output-local NUGET_PACKAGES and the candidate commit.
    let stepEnvironment = Assert.Single stepEnvironment
    Assert.Equal(Path.Combine(outDir, "nuget-packages"), stepEnvironment.["NUGET_PACKAGES"])
    Assert.Equal(repo.Commit, stepEnvironment.["FUNNYSHARP_CANDIDATE_COMMIT"])

    use evidenceDocument = readJson (Path.Combine(outDir, "execution-evidence.json"))
    let evidence = evidenceDocument.RootElement
    Assert.Equal(2, evidence.GetProperty("schemaVersion").GetInt32())
    Assert.True(evidence.GetProperty("succeeded").GetBoolean())
    Assert.Equal("full", evidence.GetProperty("mode").GetString())
    Assert.True(evidence.GetProperty("isolatedNuGetCache").GetBoolean())
    Assert.Equal<string list>([ "hello" ], stringItems (evidence.GetProperty "candidateCommands"))

    use outcomeDocument = readJson (Path.Combine(outDir, "release-outcome.json"))
    let outcome = outcomeDocument.RootElement
    Assert.True(outcome.GetProperty("succeeded").GetBoolean())
    Assert.Equal("execution-evidence.json", outcome.GetProperty("executionEvidence").GetProperty("path").GetString())
    Assert.Equal(JsonValueKind.Null, outcome.GetProperty("releaseEvidence").ValueKind)
    Assert.Equal(JsonValueKind.Null, outcome.GetProperty("error").ValueKind)
    Assert.True(File.Exists(Path.Combine(outDir, "release-verifier.stdout.log")))
    Assert.True(File.Exists(Path.Combine(outDir, "release-verifier.stderr.log")))

// ---- SkipBenchmarks selects the benchmarkSkipped mode and reaches the verifier ----

[<Fact>]
let SkipBenchmarks_SelectsModeAndForwardsSwitch () =
    use repo =
        new ScratchRepo(
            [ "hello"; "benchmark" ],
            [ "hello" ],
            [ "hello", "git", [ "--version" ]; "benchmark", "git", [ "--version" ] ])

    let verifierArguments = ResizeArray<string list>()
    let code, _, _ = runMain repo.Root (collaboratorsWith versionAbsent (stubVerifier 0 verifierArguments)) [ "-RepositoryRoot"; repo.Root; "-AttemptId"; "skip"; "-DistributionFeed"; feedUrl; "-SkipBenchmarks" ]
    Assert.Equal(0, code)
    use evidenceDocument = readJson (Path.Combine(repo.Attempt "skip", "execution-evidence.json"))
    let evidence = evidenceDocument.RootElement
    Assert.Equal("benchmarkSkipped", evidence.GetProperty("mode").GetString())
    Assert.Equal<string list>([ "hello" ], stringItems (evidence.GetProperty "candidateCommands"))
    let capturedArguments = Assert.Single verifierArguments
    Assert.Contains("-SkipBenchmarks", capturedArguments)

// ---- Step failure stops the pipeline and fails closed ----

[<Fact>]
let StepFailure_RecordsReceiptAndFailsRun () =
    use repo = new ScratchRepo([ "boom" ], [ "boom" ], [ "boom", "git", [ "rev-parse"; "--verify"; "definitely-not-a-ref" ] ])
    let code, stdout, stderr = runMain repo.Root (collaboratorsWith versionAbsent (stubVerifier 0 (ResizeArray()))) [ "-RepositoryRoot"; repo.Root; "-AttemptId"; "fail"; "-DistributionFeed"; feedUrl ]
    Assert.Equal(1, code)
    Assert.Equal("", stdout)
    Assert.StartsWith("Release run failed: Release command 'boom' failed with exit code 128.", stderr)
    let outDir = repo.Attempt "fail"
    use receiptDocument = readJson (Path.Combine(outDir, "receipts", "01-boom.json"))
    let receipt = receiptDocument.RootElement
    Assert.Equal(128, receipt.GetProperty("exitCode").GetInt32())
    use evidenceDocument = readJson (Path.Combine(outDir, "execution-evidence.json"))
    Assert.False(evidenceDocument.RootElement.GetProperty("succeeded").GetBoolean())
    use outcomeDocument = readJson (Path.Combine(outDir, "release-outcome.json"))
    let outcome = outcomeDocument.RootElement
    Assert.False(outcome.GetProperty("succeeded").GetBoolean())
    Assert.Contains("Release command 'boom' failed with exit code 128.", outcome.GetProperty("error").GetString())

// ---- Version preflight is fail-closed with blocked-version-state evidence ----

[<Fact>]
let VersionPreflight_FeedFailure_WritesBlockedStateAndStops () =
    use repo = happyRepo ()
    let failing (_: string) : HttpResponse = raise (Exception "feed unreachable")
    let code, _, stderr = runMain repo.Root (collaboratorsWith failing (stubVerifier 0 (ResizeArray()))) [ "-RepositoryRoot"; repo.Root; "-AttemptId"; "preflight"; "-DistributionFeed"; feedUrl ]
    Assert.Equal(1, code)
    Assert.Contains("Distribution feed", stderr)
    let outDir = repo.Attempt "preflight"
    use preflightDocument = readJson (Path.Combine(outDir, "version-preflight.json"))
    let preflight = preflightDocument.RootElement
    Assert.Equal("blocked-version-state", preflight.GetProperty("status").GetString())
    Assert.Contains("feed unreachable", preflight.GetProperty("error").GetString())
    Assert.Empty(preflight.GetProperty("checks").EnumerateArray() |> Seq.toList)
    // Preflight fails before the run phase, so no outcome is written.
    Assert.False(File.Exists(Path.Combine(outDir, "release-outcome.json")))

// ---- Version final is fail-closed and still writes the outcome ----

[<Fact>]
let VersionFinal_FeedFailure_WritesBlockedStateAndFailedOutcome () =
    use repo = happyRepo ()
    let mutable calls = 0

    let flaky (url: string) : HttpResponse =
        calls <- calls + 1

        if calls <= 4 then
            versionAbsent url
        else
            raise (Exception "final feed unreachable")

    let code, _, stderr = runMain repo.Root (collaboratorsWith flaky (stubVerifier 0 (ResizeArray()))) [ "-RepositoryRoot"; repo.Root; "-AttemptId"; "final"; "-DistributionFeed"; feedUrl ]
    Assert.Equal(1, code)
    Assert.Contains("Release run failed:", stderr)
    let outDir = repo.Attempt "final"
    use finalDocument = readJson (Path.Combine(outDir, "version-final.json"))
    let finalState = finalDocument.RootElement
    Assert.Equal("blocked-version-state", finalState.GetProperty("status").GetString())
    use evidenceDocument = readJson (Path.Combine(outDir, "execution-evidence.json"))
    let evidence = evidenceDocument.RootElement
    Assert.False(evidence.GetProperty("succeeded").GetBoolean())
    Assert.Equal("version-final.json", evidence.GetProperty("versionFinal").GetString())
    use outcomeDocument = readJson (Path.Combine(outDir, "release-outcome.json"))
    let outcome = outcomeDocument.RootElement
    Assert.False(outcome.GetProperty("succeeded").GetBoolean())
    Assert.Contains("final feed unreachable", outcome.GetProperty("error").GetString())

// ---- The verifier is a gate; the baseline capture failed exactly here ----

[<Fact>]
let VerifierFailure_FailsRunWithVerifierExitCode () =
    use repo = happyRepo ()
    let verifier (_: string list) (_: string) : ProcessResult =
        { ExitCode = 1
          Stdout = ""
          Stderr = "verifier reported failures" }

    let code, stdout, stderr = runMain repo.Root (collaboratorsWith versionAbsent verifier) [ "-RepositoryRoot"; repo.Root; "-AttemptId"; "verify-fail"; "-DistributionFeed"; feedUrl ]
    Assert.Equal(1, code)
    Assert.Equal("", stdout)
    Assert.Contains("Release run failed: Release verifier failed with exit code 1.", stderr)
    let outDir = repo.Attempt "verify-fail"
    Assert.True(File.Exists(Path.Combine(outDir, "release-verifier.stderr.log")))
    use outcomeDocument = readJson (Path.Combine(outDir, "release-outcome.json"))
    let outcome = outcomeDocument.RootElement
    Assert.False(outcome.GetProperty("succeeded").GetBoolean())
    Assert.Equal("Release verifier failed with exit code 1.", outcome.GetProperty("error").GetString())
    Assert.Equal(JsonValueKind.Null, outcome.GetProperty("releaseEvidence").ValueKind)

// ---- Verifier argument list (Run-Release.ps1:681-709, minus the pwsh prefix) ----

[<Fact>]
let VerifierArguments_MirrorPowerShellParameterOrder () =
    let request =
        { RepositoryRoot = "/repo"
          VerificationDirectory = "/out/release-evidence"
          PackagesDirectory = "/out/packages"
          CompatibilityEvidencePath = "/out/compatibility-run/compatibility-results.json"
          CompatibilityRuntimeIdentifier = "linux-x64"
          CompatibilityPackageFeed = "https://api.nuget.org/v3/index.json"
          ExecutionEvidenceDirectory = "/out"
          SkipBenchmarks = false }

    Assert.Equal<string list>(
        [ "-RepositoryRoot"
          "/repo"
          "-OutputDirectory"
          "/out/release-evidence"
          "-PackageDirectory"
          "/out/packages"
          "-CompatibilityEvidencePath"
          "/out/compatibility-run/compatibility-results.json"
          "-CompatibilityRuntimeIdentifier"
          "linux-x64"
          "-CompatibilityPackageFeed"
          "https://api.nuget.org/v3/index.json"
          "-ExecutionEvidenceDirectory"
          "/out"
          "-Clean" ],
        verifierArguments request
    )

    Assert.Equal("-SkipBenchmarks", List.last (verifierArguments { request with SkipBenchmarks = true }))

// ---- Usage failures ----

[<Fact>]
let MissingAttemptId_IsUsageFailure () =
    use repo = happyRepo ()
    let code, _, stderr = runMain repo.Root (collaboratorsWith versionAbsent (stubVerifier 0 (ResizeArray()))) [ "-RepositoryRoot"; repo.Root ]
    Assert.Equal(2, code)
    Assert.Contains("the -AttemptId parameter is required.", stderr)

[<Fact>]
let UnknownParameter_IsUsageFailure () =
    use repo = happyRepo ()
    let code, _, stderr = runMain repo.Root (collaboratorsWith versionAbsent (stubVerifier 0 (ResizeArray()))) [ "-AttemptId"; "x"; "-Clean" ]
    Assert.Equal(2, code)
    Assert.Contains("unknown parameter '-Clean'", stderr)
