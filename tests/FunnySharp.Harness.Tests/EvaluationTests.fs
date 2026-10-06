module FunnySharp.Harness.Tests.EvaluationTests

// Tests for the F# port of eng/evaluation/runner.py. The tool tests never run a
// real dotnet build: an injected child runner returns canned compiler/test output,
// so the copy/build/test/record/verdict wiring is exercised deterministically
// (no sleeps, no wall-clock waits). The grouped declaration census checks all
// 43 C# oracle method identifiers without executing their semantics.

open System
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open Xunit
open FunnySharp.Harness.Proc
open FunnySharp.Harness.Tests.Support
open FunnySharp.Harness.Evaluation

let private utf8NoBom = UTF8Encoding(false)

let private requiredNode (value: JsonNode | null) : JsonNode =
    match value with
    | null -> failwith "Required fixture JSON node is missing."
    | value -> value

let private readNode (path: string) =
    JsonNode.Parse(File.ReadAllText path) |> requiredNode

let private nodeText (name: string) (node: JsonNode) =
    (requiredNode node.[name]).GetValue<string>()

let private xmlAttributeText name (element: System.Xml.Linq.XElement) =
    match element.Attribute(System.Xml.Linq.XName.Get name) with
    | null -> failwith ("Required fixture XML attribute is missing: " + name)
    | attribute -> attribute.Value

let private parentDirectory (path: string) =
    match Path.GetDirectoryName path with
    | null -> failwith "Fixture path has no parent directory."
    | directory -> directory

// ---- typed view of a written record ---------------------------------------

type private Record =
    { Task: string
      Style: string
      Run: string
      Round: int
      CompilationOk: bool
      Errors: int
      BuildSeconds: float
      SemanticOk: bool option
      Total: int
      Failed: int
      FsDiagnostics: string list
      ConsumerLoc: int }

let private readRecord (path: string) : Record =
    let path =
        if File.Exists path then path
        else Path.Combine(parentDirectory path, "rounds", "0001", "record.json")
    use document = JsonDocument.Parse(File.ReadAllText path)
    let root = document.RootElement

    let text (element: JsonElement) : string =
        match element.GetString() with
        | null -> ""
        | value -> value

    let compilation = root.GetProperty "compilation"
    let semantic = root.GetProperty "semanticCorrectness"
    let semanticOkElement = semantic.GetProperty "ok"

    let semanticOk =
        match semanticOkElement.ValueKind with
        | JsonValueKind.Null -> None
        | JsonValueKind.True -> Some true
        | JsonValueKind.False -> Some false
        | _ -> failwith "semanticCorrectness.ok was neither a boolean nor null"

    { Task = text (root.GetProperty "task")
      Style = text (root.GetProperty "style")
      Run = text (root.GetProperty "run")
      Round = root.GetProperty("round").GetInt32()
      CompilationOk = compilation.GetProperty("ok").GetBoolean()
      Errors = compilation.GetProperty("errors").GetInt32()
      BuildSeconds = compilation.GetProperty("buildSeconds").GetDouble()
      SemanticOk = semanticOk
      Total = semantic.GetProperty("total").GetInt32()
      Failed = semantic.GetProperty("failed").GetInt32()
      FsDiagnostics =
          root.GetProperty("apiMisuse").GetProperty("fsDiagnostics").EnumerateArray()
          |> Seq.map text
          |> Seq.toList
      ConsumerLoc = root.GetProperty("consumerLoc").GetInt32() }

// ---- fixtures --------------------------------------------------------------

type private FakeDotnet() =
    let calls = ResizeArray<string list>()

    member val Build = { ExitCode = 0; Stdout = ""; Stderr = "" } with get, set
    member val Test = { ExitCode = 0; Stdout = ""; Stderr = "" } with get, set
    member _.Calls = List.ofSeq calls

    member this.Run
        (_workingDirectory: string)
        (_environment: (string * string) list)
        (command: string list)
        : ProcessResult =
        calls.Add command

        match command with
        | "dotnet" :: "test" :: _ -> this.Test
        | _ -> this.Build

let private seedTask (root: string) (task: string) (style: string) : unit =
    let taskDir = Path.Combine(root, "eng", "evaluation", "tasks", task)
    let templateDir = Path.Combine(taskDir, "template-" + style)
    let testsDir = Path.Combine(taskDir, "tests")
    Directory.CreateDirectory templateDir |> ignore
    Directory.CreateDirectory testsDir |> ignore
    File.WriteAllText(Path.Combine(templateDir, "Kit.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>\n", utf8NoBom)
    File.WriteAllText(Path.Combine(testsDir, "Contract.cs"), "// contract\n", utf8NoBom)
    File.WriteAllText(Path.Combine(testsDir, "XTests.cs"), "public sealed class XTests { }\n", utf8NoBom)

let private seedRun (runDir: string) (solution: string) : unit =
    let solutionDir = Path.Combine(runDir, "solution")
    Directory.CreateDirectory solutionDir |> ignore
    File.WriteAllText(Path.Combine(solutionDir, "Solution.cs"), solution, utf8NoBom)

let private buildDirOf (root: string) : string =
    let directory = Path.Combine(root, "artifacts", "evaluation", "builds")
    if not (Directory.Exists directory) then Path.Combine(directory, "aspnetcore-idiomatic-run-1")
    else
        Directory.GetDirectories(directory, "aspnetcore-idiomatic-run-1*")
        |> Array.sort
        |> Array.tryLast
        |> Option.defaultValue (Path.Combine(directory, "aspnetcore-idiomatic-run-1"))

let private hash (path: string) =
    use stream = File.OpenRead path
    SHA256.HashData stream |> Convert.ToHexString |> fun value -> value.ToLowerInvariant()

let private manifestFiles (directory: string) =
    let files = JsonArray()
    for path in Directory.GetFiles(directory, "*", SearchOption.AllDirectories) |> Array.sort do
        let file = JsonObject()
        file.["path"] <- JsonValue.Create(Path.GetRelativePath(directory, path).Replace('\\', '/'))
        file.["sha256"] <- JsonValue.Create(hash path)
        files.Add file
    files

/// The deterministic clock: two readings 2.34 apart, so every verify records 2.3.
let private installClock () : unit =
    let mutable tick = 0

    clock <-
        fun () ->
            tick <- tick + 1
            if tick % 2 = 1 then 5.0 else 7.34

// ---- tool behaviour --------------------------------------------------------

type EvaluationToolTests() =
    let originalRunner = childRunner
    let originalClock = clock
    let temp = new TempDirectory()
    let root = temp.Path
    let fake = FakeDotnet()

    do
        seedTask root "aspnetcore" "idiomatic"
        seedTask root "aspnetcore" "funnysharp"
        installClock ()
        childRunner <- fake.Run

    interface IDisposable with
        member _.Dispose() =
            childRunner <- originalRunner
            clock <- originalClock
            (temp :> IDisposable).Dispose()

    member private _.Run(args: string list) : int * string * string =
        use stdout = new StringWriter()
        use stderr = new StringWriter()
        // Console verdicts are compared against captured baselines, so the capture is pinned to LF
        // rather than following the platform's newline.
        stdout.NewLine <- "\n"
        stderr.NewLine <- "\n"
        let code = mainWith stdout stderr root args
        code, stdout.ToString(), stderr.ToString()

    member private _.RunDir = Path.Combine(root, "run-1")

    member private this.GreenRun() : unit =
        seedRun this.RunDir "line1\n\n// comment\nline2\n"
        fake.Build <- { ExitCode = 0; Stdout = "Build succeeded.\n"; Stderr = "" }

        fake.Test <-
            { ExitCode = 0
              Stdout = "Test run summary: Passed!\n  total: 9\n  failed: 0\n  succeeded: 9\n  skipped: 0\n"
              Stderr = "" }

    [<Fact>]
    member this.GreenVerifyWritesTheRecordAndPrintsTheVerdict() =
        this.GreenRun ()
        let code, out, err = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir ]
        Assert.Equal(0, code)
        Assert.Contains("VERDICT: GREEN", out)
        Assert.Equal("", err)

        let record = readRecord (Path.Combine(this.RunDir, "record.json"))
        Assert.Equal("aspnetcore", record.Task)
        Assert.Equal("idiomatic", record.Style)
        Assert.Equal("run-1", record.Run)
        Assert.Equal(1, record.Round)
        Assert.True record.CompilationOk
        Assert.Equal(0, record.Errors)
        Assert.Equal(2.3, record.BuildSeconds, 6)
        Assert.Equal(Some true, record.SemanticOk)
        Assert.Equal(9, record.Total)
        Assert.Equal(0, record.Failed)
        Assert.Empty record.FsDiagnostics
        Assert.Equal(2, record.ConsumerLoc)
        Assert.Contains("\"task\": \"aspnetcore\"", out)

    [<Fact>]
    member this.VerifyCopiesTheTemplateTestsAndSolutionIntoTheBuildTree() =
        this.GreenRun ()
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir ]
        Assert.Equal(0, code)
        let buildDir = buildDirOf root
        Assert.True(File.Exists(Path.Combine(buildDir, "Kit.csproj")))
        Assert.True(File.Exists(Path.Combine(buildDir, "Contract.cs")))
        Assert.True(File.Exists(Path.Combine(buildDir, "XTests.cs")))
        Assert.True(File.Exists(Path.Combine(buildDir, "Solution.cs")))
        Assert.True(File.Exists(Path.Combine(buildDir, "NuGet.config")))

    [<Fact>]
    member this.VerifyUsesAFreshBuildTreeWithoutDestroyingAnotherAttempt() =
        let buildDir = buildDirOf root
        Directory.CreateDirectory buildDir |> ignore
        File.WriteAllText(Path.Combine(buildDir, "stale.txt"), "stale\n")
        this.GreenRun ()
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir ]
        Assert.Equal(0, code)
        Assert.True(File.Exists(Path.Combine(buildDir, "stale.txt")))
        Assert.NotEqual<string>(buildDir, buildDirOf root)

    [<Fact>]
    member this.NuGetConfigMatchesThePythonBytes() =
        this.GreenRun ()
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir ]
        Assert.Equal(0, code)

        let expected =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
            + "<configuration>\n"
            + "  <packageSources>\n"
            + "    <clear />\n"
            + "    <add key=\"evaluation-feed\" value=\"../../feed\" />\n"
            + "    <add key=\"nuget.org\" value=\"https://api.nuget.org/v3/index.json\" />\n"
            + "  </packageSources>\n"
            + "</configuration>\n"

        Assert.Equal(expected, File.ReadAllText(Path.Combine(buildDirOf root, "NuGet.config")))

    [<Fact>]
    member this.BuildFailureLeavesSemanticCorrectnessNullAndPrintsTheBuildTail() =
        seedRun this.RunDir "code\n"

        fake.Build <-
            { ExitCode = 1
              Stdout = "oops\n: error CS0103: bad\n: warning FS0044: w\n: error FS0044: e\n"
              Stderr = "" }

        fake.Test <- { ExitCode = 0; Stdout = "should not run\n"; Stderr = "" }
        let code, out, err = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir ]
        Assert.Equal(1, code)
        Assert.Contains("--- build output (tail) ---", out)
        Assert.DoesNotContain("--- test output (tail) ---", out)
        Assert.Contains("VERDICT: RED", out)
        Assert.Equal("", err)

        let record = readRecord (Path.Combine(this.RunDir, "record.json"))
        Assert.False record.CompilationOk
        Assert.Equal(2, record.Errors)
        Assert.Equal(None, record.SemanticOk)
        Assert.Equal(0, record.Total)
        Assert.Equal(0, record.Failed)
        Assert.Equal<string list>([ "FS0044" ], record.FsDiagnostics)
        let diagnosticReceipt = readNode (Path.Combine(this.RunDir, "rounds", "0001", "diagnostics.json"))
        let fsOccurrences =
            (requiredNode diagnosticReceipt.["occurrences"]).AsArray()
            |> Seq.map requiredNode
            |> Seq.filter (fun row -> nodeText "id" row = "FS0044")
            |> Seq.toArray
        Assert.Equal(2, fsOccurrences.Length)
        Assert.Equal("warning", nodeText "severity" fsOccurrences.[0])
        Assert.Equal("error", nodeText "severity" fsOccurrences.[1])
        Assert.Single fake.Calls |> ignore

    [<Fact>]
    member this.TestFailurePrintsTheTestTailAndIsRed() =
        this.GreenRun ()

        fake.Test <-
            { ExitCode = 1
              Stdout = "Test run summary: Failed!\n  total: 9\n  failed: 2\n  succeeded: 7\n  skipped: 0\n"
              Stderr = "" }

        let code, out, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir ]
        Assert.Equal(1, code)
        // Not green, so Python prints the build tail; the build succeeded, so the
        // test tail follows it.
        Assert.Contains("--- build output (tail) ---", out)
        Assert.Contains("--- test output (tail) ---", out)
        Assert.Contains("VERDICT: RED", out)

        let record = readRecord (Path.Combine(this.RunDir, "record.json"))
        Assert.Equal(Some false, record.SemanticOk)
        Assert.Equal(9, record.Total)
        Assert.Equal(2, record.Failed)

    [<Fact>]
    member this.TestOkRequiresThePassedSummary() =
        this.GreenRun ()
        fake.Test <- { ExitCode = 0; Stdout = "all good, but no summary\n"; Stderr = "" }
        let code, out, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir ]
        Assert.Equal(1, code)
        Assert.Contains("VERDICT: RED", out)
        Assert.Equal(Some false, (readRecord (Path.Combine(this.RunDir, "record.json"))).SemanticOk)

    [<Fact>]
    member this.TestOkRequiresAReturnCodeOfZero() =
        this.GreenRun ()

        fake.Test <-
            { ExitCode = 2
              Stdout = "Test run summary: Passed!\n  total: 9\n  failed: 0\n  succeeded: 9\n  skipped: 0\n"
              Stderr = "" }

        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir ]
        Assert.Equal(1, code)
        Assert.Equal(Some false, (readRecord (Path.Combine(this.RunDir, "record.json"))).SemanticOk)

    [<Fact>]
    member this.MissingPredecessorIsRejectedBeforeAnyChild() =
        this.GreenRun ()
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir; "--round"; "3" ]
        Assert.Equal(1, code)
        Assert.Empty fake.Calls

    [<Theory>]
    [<InlineData("4")>]
    [<InlineData("0")>]
    [<InlineData("-1")>]
    member this.InvalidRoundIsUsageError(value: string) =
        this.GreenRun ()
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir; "--round=" + value ]
        Assert.Equal(2, code)
        Assert.Empty fake.Calls

    [<Theory>]
    [<InlineData("Contract.cs")>]
    [<InlineData("XTests.cs")>]
    [<InlineData("contract.cs")>]
    member this.SolutionCannotOverwriteAnOracle(filename: string) =
        this.GreenRun ()
        let trusted = Path.Combine(root, "eng", "evaluation", "tasks", "aspnetcore", "tests", "Contract.cs")
        let before = hash trusted
        File.WriteAllText(Path.Combine(this.RunDir, "solution", filename), "// malicious replacement\n")
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir ]
        Assert.Equal(1, code)
        Assert.Empty fake.Calls
        Assert.Equal(before, hash trusted)

    [<Theory>]
    [<InlineData("Test run summary: Passed!\n")>]
    [<InlineData("Test run summary: Passed!\ntotal: 0 failed: 0 succeeded: 0 skipped: 0\n")>]
    [<InlineData("Test run summary: Passed!\ntotal: 9 failed: 0 succeeded: 8 skipped: 0\n")>]
    [<InlineData("Test run summary: Passed!\ntotal: 9 failed: 1 succeeded: 8 skipped: 0\n")>]
    [<InlineData("Test run summary: Passed!\ntotal: 9 failed: 0 succeeded: 8 skipped: 1\n")>]
    [<InlineData("Test run summary: Passed!\ntotal: -9 failed: 0 succeeded: 9 skipped: 0\n")>]
    [<InlineData("Test run summary: Passed!\ntotal: 9 failed: 0 succeeded: 9\n")>]
    [<InlineData("Test run summary: Passed!\ntotal: 9 failed: 0 succeeded: 9 skipped: 0\nTest run summary: Passed!\n")>]
    [<InlineData("Test run summary: Passed!\ntotal: 9 failed: 0 succeeded: 9 skipped: 0\ntotal: 9 failed: 0 succeeded: 9 skipped: 0\n")>]
    [<InlineData("Test run summary: Passed!\ntotal: 99999999999999 failed: 0 succeeded: 99999999999999 skipped: 0\n")>]
    member this.MalformedOrEmptyOrInconsistentOrSkippedCountsAreRed(output: string) =
        this.GreenRun ()
        fake.Test <- { ExitCode = 0; Stdout = output; Stderr = "" }
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir ]
        Assert.Equal(1, code)
        Assert.Equal(Some false, (readRecord (Path.Combine(this.RunDir, "record.json"))).SemanticOk)

    [<Fact>]
    member this.DuplicateAttemptCannotOverwriteAnyRoundBytes() =
        this.GreenRun ()
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir ]
        Assert.Equal(0, code)
        let rounds = Path.Combine(this.RunDir, "rounds")
        let before = (manifestFiles rounds).ToJsonString()
        seedRun this.RunDir "different\n"
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir ]
        Assert.Equal(1, code)
        Assert.Equal(before, (manifestFiles rounds).ToJsonString())
        Assert.Equal(2, fake.Calls.Length)

    [<Theory>]
    [<InlineData("record.json")>]
    [<InlineData("feedback.json")>]
    [<InlineData("build.stdout.log")>]
    [<InlineData("solution/Solution.cs")>]
    member this.OverwrittenHistoryRejectsACorrection(relative: string) =
        this.GreenRun ()
        fake.Build <- { ExitCode = 1; Stdout = ": error CS0103: failure\n"; Stderr = "full stderr\n" }
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir ]
        Assert.Equal(1, code)
        File.AppendAllText(Path.Combine(this.RunDir, "rounds", "0001", relative), "altered")
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir; "--round"; "2" ]
        Assert.Equal(1, code)
        Assert.Single fake.Calls |> ignore

    [<Fact>]
    member this.FullOutputsSolutionsAndFeedbackAreHashedAndCorrectionLinksThem() =
        this.GreenRun ()
        fake.Build <- { ExitCode = 1; Stdout = String.replicate 5000 "x" + ": error CS0103: missing\n"; Stderr = ": warning FS1001: fixture\n" }
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir ]
        Assert.Equal(1, code)
        let first = Path.Combine(this.RunDir, "rounds", "0001")
        Assert.Equal(fake.Build.Stdout, File.ReadAllText(Path.Combine(first, "build.stdout.log")))
        Assert.Equal(fake.Build.Stderr, File.ReadAllText(Path.Combine(first, "build.stderr.log")))
        let receipt = readNode (Path.Combine(first, "receipt.json"))
        for file in (requiredNode receipt.["files"]).AsArray() |> Seq.map requiredNode do
            Assert.Equal(nodeText "sha256" file, hash (Path.Combine(first, nodeText "path" file)))
        let solutionHash = hash (Path.Combine(first, "solution", "Solution.cs"))
        let predecessor = hash (Path.Combine(first, "receipt.json"))
        this.GreenRun ()
        seedRun this.RunDir "corrected\n"
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir; "--round"; "2" ]
        Assert.Equal(0, code)
        let second = readNode (Path.Combine(this.RunDir, "rounds", "0002", "receipt.json"))
        Assert.Equal(predecessor, nodeText "previousReceiptSha256" second)
        Assert.Equal(solutionHash, hash (Path.Combine(first, "solution", "Solution.cs")))

    [<Fact>]
    member this.AllThreeFailedRoundsRemainAndFourthIsRejected() =
        this.GreenRun ()
        fake.Build <- { ExitCode = 1; Stdout = ": error CS0103: failure\n"; Stderr = "" }
        for round in 1 .. 3 do
            let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir; "--round"; string round ]
            Assert.Equal(1, code)
        Assert.Equal(3, Directory.GetDirectories(Path.Combine(this.RunDir, "rounds")).Length)
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir; "--round"; "4" ]
        Assert.Equal(2, code)
        Assert.Equal(3, fake.Calls.Length)

    [<Fact>]
    member this.InterruptedAttemptIsNotReused() =
        this.GreenRun ()
        childRunner <- fun _ _ _ -> raise (IOException("fixture child launch failed"))
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir ]
        Assert.Equal(1, code)
        childRunner <- fake.Run
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir ]
        Assert.Equal(1, code)
        Assert.Empty fake.Calls

    [<Fact>]
    member this.HistoricalRunIsReadOnly() =
        let historical = Path.Combine(root, "eng", "evaluation", "results", "aspnetcore", "idiomatic", "run-1")
        seedRun historical "historical\n"
        File.WriteAllText(Path.Combine(historical, "record.json"), "{}")
        let before = (manifestFiles historical).ToJsonString()
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; historical ]
        Assert.Equal(1, code)
        Assert.Equal(before, (manifestFiles historical).ToJsonString())
        Assert.Empty fake.Calls

    // Synthetic metadata exercises joins only. It is never producer evidence.
    member private this.StudyFixture(run: string, session: string) =
        let studyRoot = Path.Combine(root, "eng", "evaluation", "studies", "audit-resolution-v1")
        let snapshot = Path.Combine(studyRoot, "snapshot")
        Directory.CreateDirectory(Path.Combine(snapshot, "tasks", "aspnetcore", "tests")) |> ignore
        Directory.CreateDirectory(Path.Combine(snapshot, "tasks", "aspnetcore", "template-idiomatic")) |> ignore
        for relative, content in
            [ "tasks/aspnetcore/tests/Contract.cs", "// neutral contract\n"
              "tasks/aspnetcore/tests/XTests.cs", "// oracle\n"
              "tasks/aspnetcore/template-idiomatic/Kit.csproj", "<Project />"
              "tasks/aspnetcore/template-idiomatic/packages.lock.json", "{}"
              "tasks/aspnetcore/prompt-idiomatic.md", "fixture brief"
              "feed/FunnySharp.0.2.0.nupkg", "candidate fixture bytes"
              "analyzer-control/control.sarif", "{\"fixture\":\"FS1001\"}" ] do
            let path = Path.Combine(snapshot, relative)
            Directory.CreateDirectory(parentDirectory path) |> ignore
            if not (File.Exists path) then File.WriteAllText(path, content)
        for relative in [ "global.json"; "Directory.Build.props"; "build.fsx"; "eng/harness/Evaluation.fs" ] do
            let source = Path.Combine(root, relative)
            Directory.CreateDirectory(parentDirectory source) |> ignore
            if not (File.Exists source) then File.WriteAllText(source, "fixture")
            let target = Path.Combine(snapshot, "environment", relative)
            Directory.CreateDirectory(parentDirectory target) |> ignore
            if not (File.Exists target) then File.Copy(source, target)
        let planPath = Path.Combine(studyRoot, "plan.json")
        File.WriteAllText(planPath, "{}")
        let studyManifest = JsonObject()
        studyManifest.["files"] <- manifestFiles snapshot
        studyManifest.["planSha256"] <- JsonValue.Create(hash planPath)
        studyManifest.["expectedTests"] <- JsonNode.Parse("{\"aspnetcore\":9}")
        studyManifest.["cohort"] <- JsonNode.Parse("[\"aspnetcore/idiomatic/run-1\",\"aspnetcore/idiomatic/run-2\"]")
        let manifestPath = Path.Combine(studyRoot, "manifest.json")
        File.WriteAllText(manifestPath, studyManifest.ToJsonString())
        let runDir = Path.Combine(root, "eng", "evaluation", "results", "audit-resolution-v1", "aspnetcore", "idiomatic", run)
        seedRun runDir "fixture solution\n"
        let contextDir = Path.Combine(runDir, "producer", "0001-context")
        let payload = Path.Combine(contextDir, "payload")
        Directory.CreateDirectory payload |> ignore
        File.Copy(Path.Combine(snapshot, "tasks", "aspnetcore", "prompt-idiomatic.md"), Path.Combine(payload, "prompt.md"))
        let context = JsonObject()
        context.["files"] <- manifestFiles payload
        let contextPath = Path.Combine(contextDir, "context.json")
        File.WriteAllText(contextPath, context.ToJsonString())
        let invocationPath = Path.Combine(contextDir, "invocation.json")
        File.WriteAllText(invocationPath, "{\"route\":\"fixture-only\",\"request\":\"fixture\",\"response\":\"fixture\"}")
        let producer = JsonObject()
        for key, value in
            [ "studySha256", hash manifestPath; "task", "aspnetcore"; "style", "idiomatic"
              "sessionId", session; "invocationId", "fixture-" + run; "route", "fixture-only"
              "producerKind", "ai"; "requestedModel", "fixture-not-a-model"
              "startedUtc", "2026-10-01T00:00:00Z"; "finishedUtc", "2026-10-01T00:00:01Z"
              "status", "fixture"; "contextSha256", hash contextPath; "invocationSha256", hash invocationPath ] do producer.[key] <- JsonValue.Create value
        producer.["providerModel"] <- JsonNode.Parse("{\"value\":null,\"unknownReason\":\"synthetic fixture\"}")
        producer.["solutionFiles"] <- manifestFiles (Path.Combine(runDir, "solution"))
        let producerPath = Path.Combine(runDir, "producer", "0001.json")
        File.WriteAllText(producerPath, producer.ToJsonString())
        fake.Build <- { ExitCode = 0; Stdout = "Build succeeded.\n"; Stderr = "" }
        fake.Test <- { ExitCode = 0; Stdout = "Test run summary: Passed!\ntotal: 9 failed: 0 succeeded: 9 skipped: 0\n"; Stderr = "" }
        runDir, studyRoot, producerPath

    [<Theory>]
    [<InlineData("studySha256")>]
    [<InlineData("contextSha256")>]
    [<InlineData("sessionId")>]
    [<InlineData("requestedModel")>]
    member this.WrongOrMissingProducerBindingsRejectBeforeChild(field: string) =
        let runDir, _, path = this.StudyFixture("run-1", "fixture-session-1")
        let producer = readNode path
        producer.[field] <- JsonValue.Create ""
        File.WriteAllText(path, producer.ToJsonString())
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; runDir; "--study"; "audit-resolution-v1" ]
        Assert.Equal(1, code)
        Assert.Empty fake.Calls

    [<Theory>]
    [<InlineData("feed/FunnySharp.0.2.0.nupkg")>]
    [<InlineData("tasks/aspnetcore/tests/Contract.cs")>]
    [<InlineData("analyzer-control/control.sarif")>]
    [<InlineData("tasks/aspnetcore/template-idiomatic/packages.lock.json")>]
    [<InlineData("tasks/aspnetcore/prompt-idiomatic.md")>]
    [<InlineData("environment/global.json")>]
    [<InlineData("environment/Directory.Build.props")>]
    [<InlineData("environment/build.fsx")>]
    [<InlineData("environment/eng/harness/Evaluation.fs")>]
    member this.ChangedPackageOracleAnalyzerOrLockInvalidatesStudy(relative: string) =
        let runDir, studyRoot, _ = this.StudyFixture("run-1", "fixture-session-1")
        File.AppendAllText(Path.Combine(studyRoot, "snapshot", relative), "changed")
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; runDir; "--study"; "audit-resolution-v1" ]
        Assert.Equal(1, code)
        Assert.Empty fake.Calls

    [<Theory>]
    [<InlineData("global.json")>]
    [<InlineData("Directory.Build.props")>]
    [<InlineData("build.fsx")>]
    [<InlineData("eng/harness/Evaluation.fs")>]
    member this.ChangedLiveEnvironmentRejectsBeforeChild(relative: string) =
        let runDir, _, _ = this.StudyFixture("run-1", "fixture-session-1")
        File.AppendAllText(Path.Combine(root, relative), "changed")
        let code, _, err = this.Run [ "verify"; "aspnetcore"; "idiomatic"; runDir; "--study"; "audit-resolution-v1" ]
        Assert.Equal(1, code)
        Assert.Contains("runner/build configuration changed after freeze", err)
        Assert.Empty fake.Calls

    [<Theory>]
    [<InlineData("add")>]
    [<InlineData("remove")>]
    member this.SnapshotMembershipChangeRejectsBeforeChild(change: string) =
        let runDir, study, _ = this.StudyFixture("run-1", "fixture-session-1")
        let snapshot = Path.Combine(study, "snapshot")
        if change = "add" then File.WriteAllText(Path.Combine(snapshot, "extra.txt"), "extra")
        else File.Delete(Path.Combine(snapshot, "tasks", "aspnetcore", "prompt-idiomatic.md"))
        let code, _, err = this.Run [ "verify"; "aspnetcore"; "idiomatic"; runDir; "--study"; "audit-resolution-v1" ]
        Assert.Equal(1, code)
        Assert.Contains("files changed in", err)
        Assert.Empty fake.Calls

    [<Theory>]
    [<InlineData("environment")>]
    [<InlineData("prompt")>]
    [<InlineData("add")>]
    [<InlineData("remove")>]
    member this.SnapshotMutationDuringChildIsRejected(change: string) =
        let runDir, study, _ = this.StudyFixture("run-1", "fixture-session-1")
        let snapshot = Path.Combine(study, "snapshot")
        childRunner <- fun directory environment command ->
            let result = fake.Run directory environment command
            if command.[1] = "build" then
                match change with
                | "environment" -> File.AppendAllText(Path.Combine(snapshot, "environment", "global.json"), "changed")
                | "prompt" -> File.AppendAllText(Path.Combine(snapshot, "tasks", "aspnetcore", "prompt-idiomatic.md"), "changed")
                | "add" -> File.WriteAllText(Path.Combine(snapshot, "extra.txt"), "extra")
                | _ -> File.Delete(Path.Combine(snapshot, "tasks", "aspnetcore", "prompt-idiomatic.md"))
            result
        let code, _, err = this.Run [ "verify"; "aspnetcore"; "idiomatic"; runDir; "--study"; "audit-resolution-v1" ]
        Assert.Equal(1, code)
        Assert.Contains("files changed in", err)
        Assert.Equal(2, fake.Calls.Length)
        Assert.False(File.Exists(Path.Combine(runDir, "rounds", "0001", "receipt.json")))

    [<Fact>]
    member this.PromptMustBeSuppliedEvenWithOtherAllowedBytes() =
        let runDir, study, producerPath = this.StudyFixture("run-1", "fixture-session-1")
        let contextDir = Path.Combine(runDir, "producer", "0001-context")
        let payload = Path.Combine(contextDir, "payload")
        File.Delete(Path.Combine(payload, "prompt.md"))
        File.Copy(Path.Combine(study, "snapshot", "tasks", "aspnetcore", "tests", "Contract.cs"), Path.Combine(payload, "contract.cs"))
        let context = readNode (Path.Combine(contextDir, "context.json"))
        context.["files"] <- manifestFiles payload
        File.WriteAllText(Path.Combine(contextDir, "context.json"), context.ToJsonString())
        let producer = readNode producerPath
        producer.["contextSha256"] <- JsonValue.Create(hash (Path.Combine(contextDir, "context.json")))
        File.WriteAllText(producerPath, producer.ToJsonString())
        let code, _, err = this.Run [ "verify"; "aspnetcore"; "idiomatic"; runDir; "--study"; "audit-resolution-v1" ]
        Assert.Equal(1, code)
        Assert.Contains("rendered prompt was not supplied", err)
        Assert.Empty fake.Calls

    [<Fact>]
    member this.SuppliedAuditContextIsRejectedEvenWhenItsOwnHashesMatch() =
        let runDir, _, path = this.StudyFixture("run-1", "fixture-session-1")
        let contextDir = Path.Combine(runDir, "producer", "0001-context")
        File.WriteAllText(Path.Combine(contextDir, "payload", "audit.md"), "non-public answer fixture")
        let context = JsonObject()
        context.["files"] <- manifestFiles (Path.Combine(contextDir, "payload"))
        let contextPath = Path.Combine(contextDir, "context.json")
        File.WriteAllText(contextPath, context.ToJsonString())
        let producer = readNode path
        producer.["contextSha256"] <- JsonValue.Create(hash contextPath)
        File.WriteAllText(path, producer.ToJsonString())
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; runDir; "--study"; "audit-resolution-v1" ]
        Assert.Equal(1, code)
        Assert.Empty fake.Calls

    [<Fact>]
    member this.ProducerOutputCannotBeReplacedBeforeVerification() =
        let runDir, _, _ = this.StudyFixture("run-1", "fixture-session-1")
        seedRun runDir "changed after invocation"
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; runDir; "--study"; "audit-resolution-v1" ]
        Assert.Equal(1, code)
        Assert.Empty fake.Calls

    [<Fact>]
    member this.ReusedInitialSessionIsNotIndependent() =
        let first, _, _ = this.StudyFixture("run-1", "fixture-session-shared")
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; first; "--study"; "audit-resolution-v1" ]
        Assert.Equal(0, code)
        let second, _, _ = this.StudyFixture("run-2", "fixture-session-shared")
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; second; "--study"; "audit-resolution-v1" ]
        Assert.Equal(1, code)
        Assert.Equal(2, fake.Calls.Length)

    [<Theory>]
    [<InlineData("feedbackSha256")>]
    [<InlineData("sessionId")>]
    [<InlineData("invocationId")>]
    [<InlineData("contextSha256")>]
    [<InlineData("")>]
    [<InlineData("missing-feedback")>]
    [<InlineData("changed-feedback")>]
    member this.CorrectionMustBindFeedbackAndContinueOnlyItsOwnSession(field: string) =
        let runDir, _, producerPath = this.StudyFixture("run-1", "fixture-session-1")
        fake.Build <- { ExitCode = 1; Stdout = ": error CS0103: fixture failure\n"; Stderr = "" }
        let code, out, err = this.Run [ "verify"; "aspnetcore"; "idiomatic"; runDir; "--study"; "audit-resolution-v1" ]
        let diagnostic = sprintf "Initial correction fixture verify: exit=%d; calls=%A; stdout=%s; stderr=%s" code fake.Calls out err
        Assert.True(code = 1 && err = "" && fake.Calls.Length = 1, diagnostic)
        Assert.Equal(1, code)
        Assert.Equal("dotnet", fake.Calls.Head.[0])
        Assert.Equal("build", fake.Calls.Head.[1])
        Assert.Contains("VERDICT: RED", out)
        let roundDir = Path.Combine(runDir, "rounds", "0001")
        let feedback = Path.Combine(roundDir, "feedback.json")
        Assert.True(File.Exists feedback && File.Exists(Path.Combine(roundDir, "receipt.json")), diagnostic)
        let initial = readRecord (Path.Combine(roundDir, "record.json"))
        Assert.False initial.CompilationOk
        Assert.Equal(1, initial.Errors)
        Assert.Equal(None, initial.SemanticOk)
        let retainedFeedback = readNode feedback
        Assert.Equal(fake.Build.Stdout, nodeText "buildStdout" retainedFeedback)
        Assert.Equal(fake.Build.Stderr, nodeText "buildStderr" retainedFeedback)
        let contextDir = Path.Combine(runDir, "producer", "0002-context")
        let payload = Path.Combine(contextDir, "payload")
        Directory.CreateDirectory payload |> ignore
        File.Copy(Path.Combine(runDir, "producer", "0001-context", "payload", "prompt.md"), Path.Combine(payload, "prompt.md"))
        File.Copy(feedback, Path.Combine(payload, "feedback.json"))
        if field = "missing-feedback" then File.Delete(Path.Combine(payload, "feedback.json"))
        elif field = "changed-feedback" then File.AppendAllText(Path.Combine(payload, "feedback.json"), "changed")
        let context = JsonObject()
        context.["files"] <- manifestFiles payload
        let contextPath = Path.Combine(contextDir, "context.json")
        File.WriteAllText(contextPath, context.ToJsonString())
        let invocationPath = Path.Combine(contextDir, "invocation.json")
        File.WriteAllText(invocationPath, "{\"route\":\"fixture-only-correction\"}")
        let producer = readNode producerPath
        producer.["contextSha256"] <- JsonValue.Create(hash contextPath)
        producer.["invocationSha256"] <- JsonValue.Create(hash invocationPath)
        producer.["feedbackSha256"] <- JsonValue.Create(hash feedback)
        producer.["invocationId"] <- JsonValue.Create "fixture-correction"
        if field <> "" && field <> "missing-feedback" && field <> "changed-feedback" then
            producer.[field] <- JsonValue.Create(if field = "invocationId" then "fixture-run-1" else "wrong-binding")
        File.WriteAllText(Path.Combine(runDir, "producer", "0002.json"), producer.ToJsonString())
        fake.Build <- { ExitCode = 0; Stdout = "Build succeeded.\n"; Stderr = "" }
        let code, _, err = this.Run [ "verify"; "aspnetcore"; "idiomatic"; runDir; "--round"; "2"; "--study"; "audit-resolution-v1" ]
        if field = "" then
            Assert.Equal(0, code)
            Assert.Equal(3, fake.Calls.Length)
            let second = Path.Combine(runDir, "rounds", "0002")
            let receipt = readNode (Path.Combine(second, "receipt.json"))
            Assert.Equal(hash (Path.Combine(roundDir, "receipt.json")), nodeText "previousReceiptSha256" receipt)
            let retained = readNode (Path.Combine(second, "producer-receipt.json"))
            Assert.Equal(hash feedback, nodeText "feedbackSha256" retained)
            Assert.Equal("fixture-session-1", nodeText "sessionId" retained)
            for file in (requiredNode receipt.["files"]).AsArray() |> Seq.map requiredNode do
                Assert.Equal(nodeText "sha256" file, hash (Path.Combine(second, nodeText "path" file)))
        else
            Assert.Equal(1, code)
            Assert.Single fake.Calls |> ignore
            if field = "missing-feedback" then Assert.Contains("exact predecessor feedback was not supplied", err)
            elif field = "changed-feedback" then Assert.Contains("unapproved supplied context", err)

    [<Fact>]
    member this.StudyPreparationHonorsExplicitUpstreamFeed() =
        let study = Path.Combine(root, "eng", "evaluation", "studies", "audit-resolution-v1")
        let feed = Path.Combine(root, "artifacts", "evaluation", "feed-audit-resolution-v1")
        Directory.CreateDirectory study |> ignore
        Directory.CreateDirectory feed |> ignore
        for name in [ "FunnySharp.0.2.0.nupkg"; "FunnySharp.AspNetCore.0.2.0.nupkg" ] do
            File.WriteAllText(Path.Combine(feed, name), "fixture package")
        for relative in [ "global.json"; "Directory.Build.props"; "build.fsx"; "eng/harness/Evaluation.fs" ] do
            let path = Path.Combine(root, relative)
            Directory.CreateDirectory(parentDirectory path) |> ignore
            File.WriteAllText(path, "fixture environment")
        File.WriteAllText(
            Path.Combine(root, "eng", "evaluation", "tasks", "aspnetcore", "prompt-idiomatic.md"),
            "fixture brief\n## Style: idiomatic\n"
        )
        let upstream = "https://packagefeedproxy.microsoft.io/nuget/v3/index.json"
        let plan = JsonNode.Parse("{\"guides\":[],\"tasks\":[\"aspnetcore\"],\"expectedTests\":{\"aspnetcore\":9},\"cohort\":[]}") |> requiredNode
        plan.["upstreamPackageFeed"] <- JsonValue.Create upstream
        File.WriteAllText(Path.Combine(study, "plan.json"), plan.ToJsonString())
        fake.Build <- { ExitCode = 0; Stdout = "fixture FS1001 analyzer signal"; Stderr = "" }
        let code, _, _ = this.Run [ "prep-feed"; "--study"; "audit-resolution-v1" ]
        Assert.Equal(0, code)
        for relative in
            [ "tasks/aspnetcore/template-idiomatic/NuGet.config"
              "tasks/aspnetcore/template-funnysharp/NuGet.config"
              "analyzer-control/NuGet.config" ] do
            let config = System.Xml.Linq.XDocument.Load(Path.Combine(study, "snapshot", relative))
            let source = config.Descendants(System.Xml.Linq.XName.Get "add") |> Seq.find (fun add -> xmlAttributeText "key" add = "nuget.org")
            Assert.Equal(upstream, xmlAttributeText "value" source)

    [<Theory>]
    [<InlineData("https://packagefeedproxy.microsoft.io/nuget/v3/index.json")>]
    [<InlineData("https://feed.example.test/index.json?first=1&second=2")>]
    member this.StudyRoundHonorsFrozenUpstreamFeed(upstream: string) =
        let runDir, study, producerPath = this.StudyFixture("run-1", "fixture-session-1")
        let planPath = Path.Combine(study, "plan.json")
        let plan = readNode planPath
        plan.["upstreamPackageFeed"] <- JsonValue.Create upstream
        File.WriteAllText(planPath, plan.ToJsonString())
        let manifestPath = Path.Combine(study, "manifest.json")
        let manifest = readNode manifestPath
        manifest.["planSha256"] <- JsonValue.Create(hash planPath)
        File.WriteAllText(manifestPath, manifest.ToJsonString())
        let producer = readNode producerPath
        producer.["studySha256"] <- JsonValue.Create(hash manifestPath)
        File.WriteAllText(producerPath, producer.ToJsonString())
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; runDir; "--study"; "audit-resolution-v1" ]
        Assert.Equal(0, code)
        let config = System.Xml.Linq.XDocument.Load(Path.Combine(buildDirOf root, "NuGet.config"))
        let source = config.Descendants(System.Xml.Linq.XName.Get "add") |> Seq.find (fun add -> xmlAttributeText "key" add = "nuget.org")
        Assert.Equal(upstream, xmlAttributeText "value" source)

    [<Fact>]
    member this.ExplicitStudyFeedOverridesInheritedSourceDisableFlags() =
        let runDir, study, _ = this.StudyFixture("run-1", "fixture-session-1")
        let planPath = Path.Combine(study, "plan.json")
        let plan = readNode planPath
        plan.["upstreamPackageFeed"] <- JsonValue.Create "https://packagefeedproxy.microsoft.io/nuget/v3/index.json"
        File.WriteAllText(planPath, plan.ToJsonString())
        let manifestPath = Path.Combine(study, "manifest.json")
        let manifest = readNode manifestPath
        manifest.["planSha256"] <- JsonValue.Create(hash planPath)
        File.WriteAllText(manifestPath, manifest.ToJsonString())
        let replay = Path.Combine(root, "explicit-source-control")
        seedRun replay (File.ReadAllText(Path.Combine(runDir, "solution", "Solution.cs")))
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; replay; "--study"; "audit-resolution-v1"; "--replay" ]
        Assert.Equal(0, code)
        let buildRoot = Path.Combine(root, "artifacts", "evaluation", "builds")
        let buildDir = Directory.GetDirectories(buildRoot) |> Array.exactlyOne
        let config = System.Xml.Linq.XDocument.Load(Path.Combine(buildDir, "NuGet.config"))
        let disabled = config.Descendants(System.Xml.Linq.XName.Get "disabledPackageSources") |> Seq.exactlyOne
        Assert.Single(disabled.Elements(System.Xml.Linq.XName.Get "clear")) |> ignore
        Assert.Empty(disabled.Elements(System.Xml.Linq.XName.Get "add"))

    [<Fact>]
    member this.RegisteredSuccessorStudyReplaysWithoutRewritingInterruptedStudy() =
        let runDir, originalStudy, _ = this.StudyFixture("run-1", "fixture-session-1")
        let successorStudy = Path.Combine(parentDirectory originalStudy, "audit-resolution-v2")
        for source in Directory.GetFiles(originalStudy, "*", SearchOption.AllDirectories) do
            let target = Path.Combine(successorStudy, Path.GetRelativePath(originalStudy, source))
            Directory.CreateDirectory(parentDirectory target) |> ignore
            File.Copy(source, target)
        let originalManifestHash = hash (Path.Combine(originalStudy, "manifest.json"))
        let replay = Path.Combine(root, "successor-study-control")
        seedRun replay (File.ReadAllText(Path.Combine(runDir, "solution", "Solution.cs")))
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; replay; "--study"; "audit-resolution-v2"; "--replay" ]
        Assert.Equal(0, code)
        Assert.Equal(originalManifestHash, hash (Path.Combine(originalStudy, "manifest.json")))
        Assert.False(Directory.Exists(Path.Combine(runDir, "rounds")))

    [<Theory>]
    [<InlineData("audit-resolution-v3")>]
    [<InlineData("audit-resolution-v4")>]
    [<InlineData("audit-resolution-v5")>]
    [<InlineData("audit-resolution-v6")>]
    member this.RegisteredTransportSuccessorStudyRetainsReplayControlClassification(study: string) =
        let runDir, originalStudy, _ = this.StudyFixture("run-1", "fixture-session-1")
        let failedStudy = Path.Combine(parentDirectory originalStudy, "audit-resolution-v2")
        let successorStudy = Path.Combine(parentDirectory originalStudy, study)
        for study in [ failedStudy; successorStudy ] do
            for source in Directory.GetFiles(originalStudy, "*", SearchOption.AllDirectories) do
                let target = Path.Combine(study, Path.GetRelativePath(originalStudy, source))
                Directory.CreateDirectory(parentDirectory target) |> ignore
                File.Copy(source, target)
        let priorStudies =
            [ originalStudy; failedStudy ]
            |> List.map (fun study -> study, (manifestFiles study).ToJsonString())
        let replay = Path.Combine(root, "transport-successor-control")
        seedRun replay (File.ReadAllText(Path.Combine(runDir, "solution", "Solution.cs")))
        let code, out, err = this.Run [ "verify"; "aspnetcore"; "idiomatic"; replay; "--study"; study; "--replay" ]
        Assert.True((code = 0), err)
        Assert.Contains("VERDICT: GREEN", out)
        let round = Path.Combine(replay, "rounds", "0001")
        let record = readNode (Path.Combine(round, "record.json"))
        Assert.Equal("replay", nodeText "evidenceKind" record)
        Assert.False(File.Exists(Path.Combine(round, "producer-receipt.json")))
        Assert.False(Directory.Exists(Path.Combine(runDir, "rounds")))
        Assert.False(Directory.Exists(Path.Combine(root, "eng", "evaluation", "results", study)))
        for study, before in priorStudies do
            Assert.Equal(before, (manifestFiles study).ToJsonString())
        let calls = fake.Calls.Length
        let unregistered = Path.Combine(root, "unregistered-study-control")
        seedRun unregistered "fixture solution\n"
        let code, _, err = this.Run [ "verify"; "aspnetcore"; "idiomatic"; unregistered; "--study"; "audit-resolution-unregistered"; "--replay" ]
        Assert.Equal(1, code)
        Assert.Contains("unknown study", err)
        Assert.Equal(calls, fake.Calls.Length)

    [<Fact>]
    member this.ManualStudyReplayDoesNotRequireOrInventProducerIdentity() =
        let cohort, _, _ = this.StudyFixture("run-1", "fixture-session-1")
        let replay = Path.Combine(root, "oracle-control-1")
        seedRun replay (File.ReadAllText(Path.Combine(cohort, "solution", "Solution.cs")))
        Directory.Delete(Path.Combine(cohort, "producer"), true)
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; replay; "--study"; "audit-resolution-v1"; "--replay" ]
        Assert.Equal(0, code)
        let record = readNode (Path.Combine(replay, "rounds", "0001", "record.json"))
        Assert.Equal("replay", nodeText "evidenceKind" record)
        Assert.False(File.Exists(Path.Combine(replay, "rounds", "0001", "producer-receipt.json")))
        Assert.False(Directory.Exists(Path.Combine(cohort, "rounds")))

    [<Fact>]
    member this.ReplayCannotConsumeACohortSlot() =
        let cohort, _, _ = this.StudyFixture("run-1", "fixture-session-1")
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; cohort; "--study"; "audit-resolution-v1"; "--replay" ]
        Assert.Equal(1, code)
        Assert.Empty fake.Calls

    [<Fact>]
    member this.MissingFeedbackMakesHistoryIncompleteRatherThanStartingACorrection() =
        this.GreenRun ()
        fake.Build <- { ExitCode = 1; Stdout = ": error CS0103: failure\n"; Stderr = "" }
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir ]
        Assert.Equal(1, code)
        File.Delete(Path.Combine(this.RunDir, "rounds", "0001", "feedback.json"))
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir; "--round"; "2" ]
        Assert.Equal(1, code)
        Assert.Single fake.Calls |> ignore

    [<Fact>]
    member this.WrongExpectedDiscoveryCountIsRed() =
        let runDir, _, _ = this.StudyFixture("run-1", "fixture-session-1")
        fake.Test <- { ExitCode = 0; Stdout = "Test run summary: Passed!\ntotal: 8 failed: 0 succeeded: 8 skipped: 0\n"; Stderr = "" }
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; runDir; "--study"; "audit-resolution-v1" ]
        Assert.Equal(1, code)

    [<Fact>]
    member this.UnknownTaskIsExitTwo() =
        let code, out, err = this.Run [ "verify"; "nosuchtask"; "idiomatic"; this.RunDir ]
        Assert.Equal(2, code)
        Assert.Equal("", out)
        Assert.Equal("unknown task nosuchtask\n", err)

    [<Fact>]
    member this.MissingSolutionIsExitTwo() =
        let missing = Path.Combine(root, "no-such-run")
        let code, out, err = this.Run [ "verify"; "aspnetcore"; "idiomatic"; missing ]
        Assert.Equal(2, code)
        Assert.Equal("", out)
        Assert.Equal(sprintf "%s contains no solution files\n" (Path.Combine(missing, "solution")), err)

    [<Fact>]
    member this.EmptySolutionIsExitTwo() =
        Directory.CreateDirectory(Path.Combine(this.RunDir, "solution")) |> ignore
        let code, _, err = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir ]
        Assert.Equal(2, code)
        Assert.Contains("contains no solution files", err)

    [<Fact>]
    member this.BadStyleIsExitTwo() =
        let code, _, err = this.Run [ "verify"; "aspnetcore"; "bogus"; this.RunDir ]
        Assert.Equal(2, code)
        Assert.Contains("invalid choice", err)

    [<Fact>]
    member this.NoCommandIsExitTwo() =
        let code, _, err = this.Run []
        Assert.Equal(2, code)
        Assert.Contains("required: command", err)

    [<Fact>]
    member this.UnknownCommandIsExitTwo() =
        let code, _, err = this.Run [ "frobnicate" ]
        Assert.Equal(2, code)
        Assert.Contains("invalid choice", err)

    [<Fact>]
    member this.PrepFeedPrintsSortedPackagesAndEvictsTheCache() =
        let feedDir = Path.Combine(root, "artifacts", "evaluation", "feed")
        let cache = Path.Combine(root, "artifacts", ".nuget-packages-dev")
        Directory.CreateDirectory feedDir |> ignore
        File.WriteAllBytes(Path.Combine(feedDir, "FunnySharp.0.1.0.nupkg"), [| 1uy |])
        File.WriteAllBytes(Path.Combine(feedDir, "FunnySharp.AspNetCore.0.1.0.nupkg"), [| 1uy |])
        Directory.CreateDirectory(Path.Combine(cache, "funnysharp", "0.1.0")) |> ignore
        Directory.CreateDirectory(Path.Combine(cache, "funnysharp.aspnetcore", "0.1.0")) |> ignore
        childRunner <- fun _ _ _ -> { ExitCode = 0; Stdout = ""; Stderr = "" }

        let code, out, err = this.Run [ "prep-feed" ]
        Assert.Equal(0, code)
        Assert.Equal("Prepared evaluation feed:\n  FunnySharp.0.1.0.nupkg\n  FunnySharp.AspNetCore.0.1.0.nupkg\n", out)
        Assert.Equal("", err)
        Assert.False(Directory.Exists(Path.Combine(cache, "funnysharp", "0.1.0")))
        Assert.False(Directory.Exists(Path.Combine(cache, "funnysharp.aspnetcore", "0.1.0")))

    [<Fact>]
    member this.PrepFeedFailurePrintsTheTailsAndExitsOne() =
        let mutable calls = 0

        childRunner <-
            fun _ _ _ ->
                calls <- calls + 1
                { ExitCode = 1; Stdout = "STDOUT"; Stderr = "STDERR" }

        let code, out, err = this.Run [ "prep-feed" ]
        Assert.Equal(1, code)
        Assert.Equal(1, calls)
        Assert.Equal("STDOUT\nSTDERR\n", out)
        Assert.Equal("prep-feed failed for src/FunnySharp/FunnySharp.csproj\n", err)

    [<Fact>]
    member this.AggregateWritesTheExactTable() =
        let recordOne =
            Path.Combine(root, "eng", "evaluation", "results", "aspnetcore", "funnysharp", "run-1")

        let recordTwo =
            Path.Combine(root, "eng", "evaluation", "results", "aspnetcore", "idiomatic", "run-2")

        Directory.CreateDirectory recordOne |> ignore
        Directory.CreateDirectory recordTwo |> ignore

        File.WriteAllText(
            Path.Combine(recordOne, "record.json"),
            """{"task":"aspnetcore","style":"funnysharp","run":"run-1","round":1,"compilation":{"ok":true,"errors":0,"buildSeconds":2.5},"semanticCorrectness":{"ok":true,"total":9,"failed":0},"apiMisuse":{"fsDiagnostics":["FS0044","FS0043"]},"consumerLoc":175}""",
            utf8NoBom
        )

        File.WriteAllText(
            Path.Combine(recordTwo, "record.json"),
            """{"task":"aspnetcore","style":"idiomatic","run":"run-2","round":1,"compilation":{"ok":false,"errors":3,"buildSeconds":1.0},"semanticCorrectness":{"ok":null,"total":0,"failed":0},"apiMisuse":{"fsDiagnostics":[]},"consumerLoc":100}""",
            utf8NoBom
        )

        let outPath = Path.Combine(root, "aggregate.md")
        let code, out, _ = this.Run [ "aggregate"; outPath ]
        Assert.Equal(0, code)
        Assert.Equal(sprintf "wrote %s (2 runs)\n" outPath, out)

        let expected =
            String.concat
                "\n"
                [ "# Goal 21 evaluation aggregate"
                  ""
                  "Generated by `eng/evaluation/runner.py aggregate` from the recorded run records."
                  ""
                  "| task | style | run | compiled | errors | tests ok | total | failed | FS | LOC |"
                  "| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |"
                  "| aspnetcore | funnysharp | run-1 | True | 0 | True | 9 | 0 | FS0044,FS0043 | 175 |"
                  "| aspnetcore | idiomatic | run-2 | False | 3 | None | 0 | 0 | - | 100 |" ]
            + "\n"

        Assert.Equal(expected, File.ReadAllText outPath)

// ---- consumer-side LOC -----------------------------------------------------

type CountLocTests() =
    [<Fact>]
    member _.StripsBlockCommentsAcrossLines() =
        use temp = new TempDirectory()
        let path = Path.Combine(temp.Path, "A.cs")
        File.WriteAllText(path, "a\n/* x\ny */\nb\n", utf8NoBom)
        Assert.Equal(2, countLoc path)

    [<Fact>]
    member _.CountsOnlyNonBlankNonLineCommentLines() =
        use temp = new TempDirectory()
        let path = Path.Combine(temp.Path, "A.cs")
        File.WriteAllText(path, "// c\ncode; // trailing\n\n  \n/// doc\n", utf8NoBom)
        Assert.Equal(1, countLoc path)

    [<Fact>]
    member _.StripsAByteOrderMark() =
        use temp = new TempDirectory()
        let path = Path.Combine(temp.Path, "A.cs")
        File.WriteAllBytes(path, Array.append [| 0xEFuy; 0xBBuy; 0xBFuy |] (Encoding.UTF8.GetBytes "x\n"))
        Assert.Equal(1, countLoc path)

    [<Fact>]
    member _.RemovesAnInlineBlockComment() =
        use temp = new TempDirectory()
        let path = Path.Combine(temp.Path, "A.cs")
        File.WriteAllText(path, "a/**/b\n", utf8NoBom)
        Assert.Equal(1, countLoc path)

// ---- the 43 C# oracle declaration obligations ------------------------------

module EvaluationOracle =

    let private declares (text: string) name =
        text.Contains("public void " + name + "(") || text.Contains("public async Task " + name + "(")

    type DeclarationTests() =
        static member Cases : objnull array seq =
            seq {
                yield [| box "business-outcomes"; box "PlaceOrderTests.cs"; box [|
                    "ValidOrderWithoutPromoPlacesTheOrder"
                    "Save10PromoDiscountsTenPercent"
                    "FreeshipPromoRemovesShipping"
                    "ValidationErrorsAccumulateInOrder"
                    "EmptyOrderReportsEmptyOrder"
                    "UnknownSkuIsReported"
                    "DeclinedCardStopsBeforePersistence"
                    "SaveFailureIsReportedAfterCharging"
                |] |]
                yield [| box "collections"; box "CollectionsTests.cs"; box [|
                    "ValidFeedIsNormalizedAndImportedInOrder"
                    "BlankRequiredFieldsAreDroppedInLineOrder"
                    "UnparsablePricesAreDroppedDeterministically"
                    "UnknownSkusAreDroppedAfterNormalization"
                    "FirstRowPerSkuIsKeptAndLaterDuplicatesAreDropped"
                    "LengthMismatchIsReportedAndStopsTheClean"
                    "EmptyFeedProducesNoProductsAndNoAverage"
                    "FeedWhereNothingSurvivesLeavesTheAverageAbsent"
                    "MixedFeedPartitionsEveryRowAndRoundsTheAverage"
                |] |]
                yield [| box "async-streams"; box "AsyncStreamsTests.cs"; box [|
                    "FilterMapKeepsOnlyTemperatureReadingsInCelsius"
                    "RunningMaximaTrackTheMaximumSoFar"
                    "AllPlausibleReadingsCollectTheValidBatch"
                    "ImplausibleReadingsAccumulateEveryFailure"
                    "NonTemperatureReadingsProduceAnEmptyReport"
                    "EmptyStreamProducesAnEmptyReport"
                    "CancelledTokenSurfacesBeforeAnyReferenceCall"
                    "SourceCancellationSurfacesAsOperationCanceledException"
                |] |]
                yield [| box "concurrency"; box "ConcurrencyTests.cs"; box [|
                    "ResultsComeBackInSourceOrderWhenEarlyItemsFinishLast"
                    "ChecksOverlapButNeverExceedMaxConcurrency"
                    "MaxConcurrencyOfOneRunsOneCheckAtATime"
                    "FirstAcceptingSupplierWinsEvenWhenDeclinesFinishFirst"
                    "EveryDeclineIsListedInInputOrderWhenAllSuppliersFail"
                    "CancelledAvailabilityCheckSurfacesOperationCanceledException"
                    "CancelledReservationSurfacesOperationCanceledException"
                    "EmptyItemListStartsNoChecksAndReturnsNoResults"
                    "EmptySupplierListStartsNoProbesAndReservesNothing"
                |] |]
                yield [| box "aspnetcore"; box "AspnetcoreTests.cs"; box [|
                    "PostOrderAccumulatesValidationErrorsInOrder"
                    "PostOrderReportsUnknownSku"
                    "PostOrderStoresPricedOrderAndReturnsItsId"
                    "PostOrderAppliesPromoCodes"
                    "GetProductReturnsKnownProduct"
                    "GetProductReportsUnknownProduct"
                    "DeleteOrderReportsUnknownOrder"
                    "DeleteOrderKeepsPaidOrderAndRemovesUnpaidOrder"
                    "PayOrderReportsUnknownOrderDeclinesThenSucceeds"
                |] |]
            }

        [<Theory>]
        [<MemberData(nameof DeclarationTests.Cases)>]
        member _.EveryOriginalDeclarationIsPresent(area: string, file: string, names: string array) =
            let path = Path.Combine(repositoryRoot (), "eng", "evaluation", "tasks", area, "tests", file)
            let text = File.ReadAllText path
            for name in names do
                Assert.True(declares text name, sprintf "%s/%s does not declare the oracle fact %s" area file name)
                // This is declaration detection, not execution of C# semantics.
                let renamed =
                    text.Replace("public void " + name + "(", "public void Missing_" + name + "(")
                        .Replace("public async Task " + name + "(", "public async Task Missing_" + name + "(")
                Assert.False(declares renamed name, sprintf "renamed declaration %s was still accepted" name)


