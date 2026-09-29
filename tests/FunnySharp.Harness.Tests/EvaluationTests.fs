module FunnySharp.Harness.Tests.EvaluationTests

// Tests for the F# port of eng/evaluation/runner.py. The tool tests never run a
// real dotnet build: an injected child runner returns canned compiler/test output,
// so the copy/build/test/record/verdict wiring is exercised deterministically
// (no sleeps, no wall-clock waits). The oracle classes pin the 43 C# facts the
// runner compiles and runs, without modifying them.

open System
open System.IO
open System.Text
open System.Text.Json
open Xunit
open FunnySharp.Harness.Proc
open FunnySharp.Harness.Tests.Support
open FunnySharp.Harness.Evaluation

let private utf8NoBom = UTF8Encoding(false)

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
    Path.Combine(root, "artifacts", "evaluation", "builds", "aspnetcore-idiomatic-run-1")

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
    member this.VerifyWipesTheBuildTreeOnEveryRun() =
        let buildDir = buildDirOf root
        Directory.CreateDirectory buildDir |> ignore
        File.WriteAllText(Path.Combine(buildDir, "stale.txt"), "stale\n")
        this.GreenRun ()
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir ]
        Assert.Equal(0, code)
        Assert.False(File.Exists(Path.Combine(buildDir, "stale.txt")))

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
    member this.RoundIsRecordedFromTheFlag() =
        this.GreenRun ()
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir; "--round"; "3" ]
        Assert.Equal(0, code)
        Assert.Equal(3, (readRecord (Path.Combine(this.RunDir, "record.json"))).Round)
        let code, _, _ = this.Run [ "verify"; "aspnetcore"; "idiomatic"; this.RunDir; "--round=4" ]
        Assert.Equal(0, code)
        Assert.Equal(4, (readRecord (Path.Combine(this.RunDir, "record.json"))).Round)

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

// ---- the 43 C# oracle expectations -----------------------------------------

module EvaluationOracle =

    let private expect (area: string) (file: string) (name: string) : unit =
        let path = Path.Combine(repositoryRoot (), "eng", "evaluation", "tasks", area, "tests", file)
        let text = File.ReadAllText path
        // business-outcomes/collections declare `public void`; the async areas
        // declare `public async Task`.
        Assert.True(
            text.Contains("public void " + name + "(") || text.Contains("public async Task " + name + "("),
            sprintf "%s/%s does not declare the oracle fact %s" area file name
        )

    type BusinessOutcomes() =
        [<Fact>]
        member _.ValidOrderWithoutPromoPlacesTheOrder() =
            expect "business-outcomes" "PlaceOrderTests.cs" "ValidOrderWithoutPromoPlacesTheOrder"

        [<Fact>]
        member _.Save10PromoDiscountsTenPercent() =
            expect "business-outcomes" "PlaceOrderTests.cs" "Save10PromoDiscountsTenPercent"

        [<Fact>]
        member _.FreeshipPromoRemovesShipping() =
            expect "business-outcomes" "PlaceOrderTests.cs" "FreeshipPromoRemovesShipping"

        [<Fact>]
        member _.ValidationErrorsAccumulateInOrder() =
            expect "business-outcomes" "PlaceOrderTests.cs" "ValidationErrorsAccumulateInOrder"

        [<Fact>]
        member _.EmptyOrderReportsEmptyOrder() =
            expect "business-outcomes" "PlaceOrderTests.cs" "EmptyOrderReportsEmptyOrder"

        [<Fact>]
        member _.UnknownSkuIsReported() =
            expect "business-outcomes" "PlaceOrderTests.cs" "UnknownSkuIsReported"

        [<Fact>]
        member _.DeclinedCardStopsBeforePersistence() =
            expect "business-outcomes" "PlaceOrderTests.cs" "DeclinedCardStopsBeforePersistence"

        [<Fact>]
        member _.SaveFailureIsReportedAfterCharging() =
            expect "business-outcomes" "PlaceOrderTests.cs" "SaveFailureIsReportedAfterCharging"

    type Collections() =
        [<Fact>]
        member _.ValidFeedIsNormalizedAndImportedInOrder() =
            expect "collections" "CollectionsTests.cs" "ValidFeedIsNormalizedAndImportedInOrder"

        [<Fact>]
        member _.BlankRequiredFieldsAreDroppedInLineOrder() =
            expect "collections" "CollectionsTests.cs" "BlankRequiredFieldsAreDroppedInLineOrder"

        [<Fact>]
        member _.UnparsablePricesAreDroppedDeterministically() =
            expect "collections" "CollectionsTests.cs" "UnparsablePricesAreDroppedDeterministically"

        [<Fact>]
        member _.UnknownSkusAreDroppedAfterNormalization() =
            expect "collections" "CollectionsTests.cs" "UnknownSkusAreDroppedAfterNormalization"

        [<Fact>]
        member _.FirstRowPerSkuIsKeptAndLaterDuplicatesAreDropped() =
            expect "collections" "CollectionsTests.cs" "FirstRowPerSkuIsKeptAndLaterDuplicatesAreDropped"

        [<Fact>]
        member _.LengthMismatchIsReportedAndStopsTheClean() =
            expect "collections" "CollectionsTests.cs" "LengthMismatchIsReportedAndStopsTheClean"

        [<Fact>]
        member _.EmptyFeedProducesNoProductsAndNoAverage() =
            expect "collections" "CollectionsTests.cs" "EmptyFeedProducesNoProductsAndNoAverage"

        [<Fact>]
        member _.FeedWhereNothingSurvivesLeavesTheAverageAbsent() =
            expect "collections" "CollectionsTests.cs" "FeedWhereNothingSurvivesLeavesTheAverageAbsent"

        [<Fact>]
        member _.MixedFeedPartitionsEveryRowAndRoundsTheAverage() =
            expect "collections" "CollectionsTests.cs" "MixedFeedPartitionsEveryRowAndRoundsTheAverage"

    type AsyncStreams() =
        [<Fact>]
        member _.FilterMapKeepsOnlyTemperatureReadingsInCelsius() =
            expect "async-streams" "AsyncStreamsTests.cs" "FilterMapKeepsOnlyTemperatureReadingsInCelsius"

        [<Fact>]
        member _.RunningMaximaTrackTheMaximumSoFar() =
            expect "async-streams" "AsyncStreamsTests.cs" "RunningMaximaTrackTheMaximumSoFar"

        [<Fact>]
        member _.AllPlausibleReadingsCollectTheValidBatch() =
            expect "async-streams" "AsyncStreamsTests.cs" "AllPlausibleReadingsCollectTheValidBatch"

        [<Fact>]
        member _.ImplausibleReadingsAccumulateEveryFailure() =
            expect "async-streams" "AsyncStreamsTests.cs" "ImplausibleReadingsAccumulateEveryFailure"

        [<Fact>]
        member _.NonTemperatureReadingsProduceAnEmptyReport() =
            expect "async-streams" "AsyncStreamsTests.cs" "NonTemperatureReadingsProduceAnEmptyReport"

        [<Fact>]
        member _.EmptyStreamProducesAnEmptyReport() =
            expect "async-streams" "AsyncStreamsTests.cs" "EmptyStreamProducesAnEmptyReport"

        [<Fact>]
        member _.CancelledTokenSurfacesBeforeAnyReferenceCall() =
            expect "async-streams" "AsyncStreamsTests.cs" "CancelledTokenSurfacesBeforeAnyReferenceCall"

        [<Fact>]
        member _.SourceCancellationSurfacesAsOperationCanceledException() =
            expect "async-streams" "AsyncStreamsTests.cs" "SourceCancellationSurfacesAsOperationCanceledException"

    type Concurrency() =
        [<Fact>]
        member _.ResultsComeBackInSourceOrderWhenEarlyItemsFinishLast() =
            expect "concurrency" "ConcurrencyTests.cs" "ResultsComeBackInSourceOrderWhenEarlyItemsFinishLast"

        [<Fact>]
        member _.ChecksOverlapButNeverExceedMaxConcurrency() =
            expect "concurrency" "ConcurrencyTests.cs" "ChecksOverlapButNeverExceedMaxConcurrency"

        [<Fact>]
        member _.MaxConcurrencyOfOneRunsOneCheckAtATime() =
            expect "concurrency" "ConcurrencyTests.cs" "MaxConcurrencyOfOneRunsOneCheckAtATime"

        [<Fact>]
        member _.FirstAcceptingSupplierWinsEvenWhenDeclinesFinishFirst() =
            expect "concurrency" "ConcurrencyTests.cs" "FirstAcceptingSupplierWinsEvenWhenDeclinesFinishFirst"

        [<Fact>]
        member _.EveryDeclineIsListedInInputOrderWhenAllSuppliersFail() =
            expect "concurrency" "ConcurrencyTests.cs" "EveryDeclineIsListedInInputOrderWhenAllSuppliersFail"

        [<Fact>]
        member _.CancelledAvailabilityCheckSurfacesOperationCanceledException() =
            expect "concurrency" "ConcurrencyTests.cs" "CancelledAvailabilityCheckSurfacesOperationCanceledException"

        [<Fact>]
        member _.CancelledReservationSurfacesOperationCanceledException() =
            expect "concurrency" "ConcurrencyTests.cs" "CancelledReservationSurfacesOperationCanceledException"

        [<Fact>]
        member _.EmptyItemListStartsNoChecksAndReturnsNoResults() =
            expect "concurrency" "ConcurrencyTests.cs" "EmptyItemListStartsNoChecksAndReturnsNoResults"

        [<Fact>]
        member _.EmptySupplierListStartsNoProbesAndReservesNothing() =
            expect "concurrency" "ConcurrencyTests.cs" "EmptySupplierListStartsNoProbesAndReservesNothing"

    type Aspnetcore() =
        [<Fact>]
        member _.PostOrderAccumulatesValidationErrorsInOrder() =
            expect "aspnetcore" "AspnetcoreTests.cs" "PostOrderAccumulatesValidationErrorsInOrder"

        [<Fact>]
        member _.PostOrderReportsUnknownSku() =
            expect "aspnetcore" "AspnetcoreTests.cs" "PostOrderReportsUnknownSku"

        [<Fact>]
        member _.PostOrderStoresPricedOrderAndReturnsItsId() =
            expect "aspnetcore" "AspnetcoreTests.cs" "PostOrderStoresPricedOrderAndReturnsItsId"

        [<Fact>]
        member _.PostOrderAppliesPromoCodes() =
            expect "aspnetcore" "AspnetcoreTests.cs" "PostOrderAppliesPromoCodes"

        [<Fact>]
        member _.GetProductReturnsKnownProduct() =
            expect "aspnetcore" "AspnetcoreTests.cs" "GetProductReturnsKnownProduct"

        [<Fact>]
        member _.GetProductReportsUnknownProduct() =
            expect "aspnetcore" "AspnetcoreTests.cs" "GetProductReportsUnknownProduct"

        [<Fact>]
        member _.DeleteOrderReportsUnknownOrder() =
            expect "aspnetcore" "AspnetcoreTests.cs" "DeleteOrderReportsUnknownOrder"

        [<Fact>]
        member _.DeleteOrderKeepsPaidOrderAndRemovesUnpaidOrder() =
            expect "aspnetcore" "AspnetcoreTests.cs" "DeleteOrderKeepsPaidOrderAndRemovesUnpaidOrder"

        [<Fact>]
        member _.PayOrderReportsUnknownOrderDeclinesThenSucceeds() =
            expect "aspnetcore" "AspnetcoreTests.cs" "PayOrderReportsUnknownOrderDeclinesThenSucceeds"

    type Suite() =
        [<Fact>]
        member _.TheOracleHoldsFortyThreeFacts() =
            let areas =
                [ "business-outcomes", "PlaceOrderTests.cs"
                  "collections", "CollectionsTests.cs"
                  "async-streams", "AsyncStreamsTests.cs"
                  "concurrency", "ConcurrencyTests.cs"
                  "aspnetcore", "AspnetcoreTests.cs" ]

            let total =
                areas
                |> List.sumBy (fun (area, file) ->
                    let path = Path.Combine(repositoryRoot (), "eng", "evaluation", "tasks", area, "tests", file)
                    (File.ReadAllText path).Split("[Fact]").Length - 1)

            Assert.Equal(43, total)
