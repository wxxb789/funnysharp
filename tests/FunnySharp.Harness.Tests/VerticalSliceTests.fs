module FunnySharp.Harness.Tests.VerticalSliceTests

// F# port of eng/tools/tests/test_vertical_slice.py. The suite never runs the
// real .NET pipeline: a fake step runner stands in for the SDK and returns canned
// output, so the step wiring, the summary parser and the failure aggregation are
// exercised deterministically. The fake records the commands it saw, which is how
// the tests prove the consumer projects are restored from the local feed with the
// injected package versions.

open System
open System.IO
open System.Text.Json
open System.Text.RegularExpressions
open Xunit
open FunnySharp.Harness.Proc
open FunnySharp.Harness.Tests.Support
open FunnySharp.Harness.VerticalSlice

// ---- A typed view of the receipt, so the assertions never dereference a
// nullable JsonNode. ----

type private StepRecord =
    { Step: string
      ExitCode: int
      StdoutTail: string
      StderrTail: string }

type private Receipt =
    { Status: string
      Failures: string list
      Steps: StepRecord list
      PackageVersions: Map<string, string>
      TestSummary: Map<string, int> option
      TestFailures: string list
      VerifyMarker: bool
      BaselineVerifyMarker: bool
      MeasurementsVerifyMarker: bool
      HasMeasurementsPath: bool }

let private readReceipt (path: string) : Receipt =
    use document = JsonDocument.Parse(File.ReadAllText path)
    let root = document.RootElement
    let textValue (element: JsonElement) = element.GetString() |> Option.ofObj |> Option.defaultValue ""

    let steps =
        root.GetProperty("steps").EnumerateArray()
        |> Seq.map (fun step ->
            { Step = textValue (step.GetProperty "step")
              ExitCode = step.GetProperty("exitCode").GetInt32()
              StdoutTail = textValue (step.GetProperty "stdoutTail")
              StderrTail = textValue (step.GetProperty "stderrTail") })
        |> Seq.toList

    let packageVersions =
        root.GetProperty("packageVersions").EnumerateObject()
        |> Seq.map (fun property -> property.Name, textValue property.Value)
        |> Map.ofSeq

    let testSummary =
        match root.TryGetProperty "testSummary" with
        | true, element when element.ValueKind = JsonValueKind.Object ->
            Some(element.EnumerateObject() |> Seq.map (fun property -> property.Name, property.Value.GetInt32()) |> Map.ofSeq)
        | _ -> None

    let testFailures =
        match root.TryGetProperty "testFailures" with
        | true, element when element.ValueKind = JsonValueKind.Array ->
            element.EnumerateArray() |> Seq.map textValue |> Seq.toList
        | _ -> []

    { Status = textValue (root.GetProperty "status")
      Failures =
          root.GetProperty("failures").EnumerateArray()
          |> Seq.map textValue
          |> Seq.toList
      TestFailures = testFailures
      Steps = steps
      PackageVersions = packageVersions
      TestSummary = testSummary
      VerifyMarker = root.GetProperty("verifyMarker").GetBoolean()
      BaselineVerifyMarker = root.GetProperty("baselineVerifyMarker").GetBoolean()
      MeasurementsVerifyMarker = root.GetProperty("measurementsVerifyMarker").GetBoolean()
      HasMeasurementsPath = root.TryGetProperty("measurementsPath") |> fst }

/// Canned process results standing in for the dotnet SDK, mirroring the shim the
/// Python suite puts on PATH.
/// The runner output a green consumer-tests step prints; a test replaces it to stand in for a
/// run the runner reports as failed.
let private defaultTestOutput = "Test run summary: Passed!\n  total: 37\n  failed: 0\n  succeeded: 37\n  skipped: 0\n"

type private FakeRunner() =
    let calls = ResizeArray<string list>()

    member val ExitCode = 0 with get, set
    member val PackVersion = "2.0.0" with get, set
    member val MeasurementsExitCode = 0 with get, set
    member val TestOutput = defaultTestOutput with get, set
    member _.Calls = List.ofSeq calls

    member this.Run
        (_workingDirectory: string)
        (_environment: (string * string) list)
        (_timeoutSeconds: int)
        (command: string list)
        : ProcessResult =
        calls.Add command
        let subcommand = if command.Length > 1 then command.[1] else ""
        let joined = String.concat " " command

        let stdout =
            match subcommand with
            | "restore" | "build" -> "  Restored 1 project.\n"
            | "run" ->
                if List.contains "--verify" command then
                    if joined.Contains "Baseline" then baselineMarker + "\n"
                    elif joined.Contains "Measurements" then measurementsMarker + "\n"
                    else verifyMarker + "\n"
                else
                    "measurements written\n"
            | "test" -> this.TestOutput
            | "pack" ->
                let mutable outDirectory = "."
                let mutable previous = ""

                for argument in command do
                    if previous = "-o" then outDirectory <- argument
                    previous <- argument

                File.WriteAllBytes(Path.Combine(outDirectory, sprintf "FunnySharp.%s.nupkg" this.PackVersion), [| 1uy |])

                File.WriteAllBytes(
                    Path.Combine(outDirectory, sprintf "FunnySharp.AspNetCore.%s.nupkg" this.PackVersion),
                    [| 2uy |]
                )

                "Successfully created package\n"
            | _ -> "packed\n"

        let isMeasurementsRun =
            subcommand = "run"
            && command.Length > 0
            && command.[command.Length - 1].EndsWith("measurements.json", StringComparison.Ordinal)

        { ExitCode = (if isMeasurementsRun then this.MeasurementsExitCode else this.ExitCode)
          Stdout = stdout
          Stderr = "" }

type VerticalSliceToolTests() =
    let originalRunner = stepRunner
    let temp = new TempDirectory()
    let feed = Path.Combine(temp.Path, "feed")
    let output = Path.Combine(temp.Path, "output")
    let runner = FakeRunner()

    do
        Directory.CreateDirectory feed |> ignore
        File.WriteAllBytes(Path.Combine(feed, "FunnySharp.1.2.3.nupkg"), [| 1uy |])
        File.WriteAllBytes(Path.Combine(feed, "FunnySharp.AspNetCore.1.2.3.nupkg"), [| 2uy |])
        File.WriteAllBytes(Path.Combine(feed, "FunnySharp.AspNetCore.1.2.3.snupkg"), [| 3uy |])
        stepRunner <- runner.Run

    member private _.BaseArgs =
        [ "--feed"; feed; "--output"; output; "--repository-root"; temp.Path ]

    member private this.RunCaptured(extra: string list) : int * string * string =
        use stdout = new StringWriter()
        use stderr = new StringWriter()
        let exitCode = mainWith stdout stderr ([ "--no-pack" ] @ this.BaseArgs @ extra)
        exitCode, stdout.ToString(), stderr.ToString()

    member private this.RunTool(extra: string list) : int =
        let exitCode, _, _ = this.RunCaptured extra
        exitCode

    member private _.Receipt() : Receipt =
        readReceipt (Path.Combine(output, "vertical-slice-results.json"))

    interface IDisposable with
        member _.Dispose() =
            stepRunner <- originalRunner
            (temp :> IDisposable).Dispose()

    [<Fact>]
    member _.PackageVersionsIgnoresTheLongerPackageId() =
        match packageVersions feed with
        | Ok versions ->
            Assert.Equal("1.2.3", versions.["FunnySharp"])
            Assert.Equal("1.2.3", versions.["FunnySharp.AspNetCore"])
        | Error err -> Assert.Fail err.Message

    [<Fact>]
    member _.PackageVersionsFailsClosedWhenAPackageIsMissing() =
        File.Delete(Path.Combine(feed, "FunnySharp.AspNetCore.1.2.3.nupkg"))

        match packageVersions feed with
        | Error err ->
            Assert.Equal(1, err.ExitCode)
            Assert.Contains("missing a package", err.Message)
        | Ok _ -> Assert.Fail "expected a typed failure for a missing package"

    [<Fact>]
    member _.PackageVersionsFailsClosedWhenTheFeedHoldsTwoVersions() =
        File.WriteAllBytes(Path.Combine(feed, "FunnySharp.0.9.0.nupkg"), [| 9uy |])

        match packageVersions feed with
        | Error err ->
            Assert.Equal(1, err.ExitCode)
            Assert.Contains("more than one", err.Message)
        | Ok _ -> Assert.Fail "expected a typed failure for two versions"

    [<Fact>]
    member this.PackingReplacesAStalePackageInAReusedFeed() =
        File.WriteAllBytes(Path.Combine(feed, "FunnySharp.0.9.0.nupkg"), [| 9uy |])
        File.WriteAllBytes(Path.Combine(feed, "FunnySharp.AspNetCore.0.9.0.nupkg"), [| 9uy |])
        runner.PackVersion <- "2.0.0"

        use stdout = new StringWriter()
        use stderr = new StringWriter()

        let exitCode =
            mainWith stdout stderr (this.BaseArgs @ [ "--skip-tests"; "--skip-measurements" ])

        Assert.Equal(0, exitCode)
        let receipt = this.Receipt()
        Assert.Equal("2.0.0", receipt.PackageVersions.["FunnySharp"])
        Assert.Equal("2.0.0", receipt.PackageVersions.["FunnySharp.AspNetCore"])
        Assert.False(File.Exists(Path.Combine(feed, "FunnySharp.0.9.0.nupkg")))
        Assert.False(File.Exists(Path.Combine(feed, "FunnySharp.AspNetCore.0.9.0.nupkg")))

    [<Fact>]
    member _.ParseSummaryReadsTheRealRunnerShapeAndBothLocalizations() =
        let real = "Test run summary: Passed!\n  total: 441\n  failed: 0\n  succeeded: 441\n  skipped: 0\n"
        let legacy = "Test run summary:\n  Total: 37\n  Failed: 0\n  Passed: 37\n  Skipped: 0\n"

        let chinese =
            "\u6D4B\u8BD5\u8FD0\u884C\u6458\u8981:\n  \u603B\u8BA1: 37\n  \u5931\u8D25: 0\n  \u6210\u529F: 37\n  \u5DF2\u8DF3\u8FC7: 0\n"

        let expected = Map [ "total", 37; "failed", 0; "passed", 37; "skipped", 0 ]

        Assert.Equal<Map<string, int>>(Map [ "total", 441; "failed", 0; "passed", 441; "skipped", 0 ], parseSummary real |> Option.get)
        Assert.Equal<Map<string, int>>(expected, parseSummary legacy |> Option.get)
        Assert.Equal<Map<string, int>>(expected, parseSummary chinese |> Option.get)
        Assert.True((parseSummary "no summary here").IsNone)
        // A summary missing one field is unparsable, never partially trusted.
        Assert.True((parseSummary "Test run summary: Passed!\n  total: 1\n  failed: 0\n").IsNone)

    [<Fact>]
    member this.AHungStepFailsTheToolInsteadOfHangingIt() =
        if OperatingSystem.IsWindows() then
            // The blocking child below needs a POSIX shell.
            ()
        else
            let fake = runner.Run

            stepRunner <-
                fun workingDirectory environment timeoutSeconds command ->
                    if command.Length > 1 && command.[1] = "restore" then
                        runStepProcess workingDirectory environment 1 [ "/bin/sh"; "-c"; "sleep 60" ]
                    else
                        fake workingDirectory environment timeoutSeconds command

            let exitCode, _, _ = this.RunCaptured [ "--skip-tests"; "--skip-measurements" ]
            Assert.Equal(1, exitCode)
            let receipt = this.Receipt()
            Assert.Equal("fail", receipt.Status)

            let restoreApi = receipt.Steps |> List.find (fun step -> step.Step = "restore-api")
            Assert.Equal(stepTimeoutReturnCode, restoreApi.ExitCode)
            Assert.Contains("timed out", restoreApi.StderrTail)

    [<Fact>]
    member _.VerifyMarkersMatchTheApplicationConstants() =
        let root = repositoryRoot ()

        let markers =
            [ "tests/FunnySharp.VerticalSlice/FunnySharp.VerticalSlice.Api/VerticalSliceApp.cs", verifyMarker
              "tests/FunnySharp.VerticalSlice/FunnySharp.VerticalSlice.Baseline/BaselineApp.cs", baselineMarker
              "tests/FunnySharp.VerticalSlice/FunnySharp.VerticalSlice.Measurements/Program.cs", measurementsMarker ]

        for relativePath, marker in markers do
            let text = File.ReadAllText(Path.Combine(root, relativePath))
            let found = Regex.Match(text, "VerifyMarker = \"([^\"]+)\"")
            Assert.True(found.Success, relativePath)
            Assert.Equal(marker, found.Groups.[1].Value)

    [<Fact>]
    member this.AGreenRunRecordsEveryStepAndPasses() =
        Assert.Equal(0, this.RunTool [])
        let receipt = this.Receipt()
        Assert.Equal("pass", receipt.Status)
        let summary = receipt.TestSummary |> Option.defaultWith (fun () -> failwith "missing testSummary")
        Assert.Equal(37, summary.["total"])
        Assert.Equal(0, summary.["failed"])
        Assert.Equal(37, summary.["passed"])
        Assert.Equal(0, summary.["skipped"])
        Assert.True receipt.VerifyMarker
        Assert.True receipt.BaselineVerifyMarker
        Assert.True receipt.MeasurementsVerifyMarker

        let steps = receipt.Steps |> List.map (fun step -> step.Step) |> Set.ofList

        let expected =
            Set.ofList
                [ "restore-api"; "restore-tests"; "restore-baseline"; "restore-measurements"
                  "build-api"; "build-tests"; "build-baseline"; "build-measurements"
                  "api-verify"; "baseline-verify"; "measurements-verify"; "measurements"; "consumer-tests" ]

        Assert.True(Set.isSubset expected steps, "the receipt is missing expected step names")

        Assert.True(
            receipt.Steps |> List.forall (fun step -> step.ExitCode = 0),
            "every recorded step must have exited 0"
        )

    [<Fact>]
    member this.ConsumerProjectsAreRestoredFromTheLocalFeedWithInjectedVersions() =
        Assert.Equal(0, this.RunTool [])

        let restore =
            runner.Calls
            |> List.find (fun command -> command.Length > 2 && command.[1] = "restore" && command.[2].Contains "Tests")

        let joined = String.concat " " restore
        Assert.Contains("--source " + Path.GetFullPath feed, joined)
        Assert.Contains("--source https://api.nuget.org/v3/index.json", joined)
        Assert.Contains("-p:FunnySharpPackageVersion=1.2.3", joined)
        Assert.Contains("-p:FunnySharpAspNetCorePackageVersion=1.2.3", joined)

    [<Fact>]
    member this.AFailingDotnetRunFailsTheTool() =
        runner.ExitCode <- 1
        Assert.Equal(1, this.RunTool [])
        let receipt = this.Receipt()
        Assert.Equal("fail", receipt.Status)
        Assert.NotEmpty receipt.Failures

    [<Fact>]
    member _.ParseFailingTestsNamesTheTestsTheRunnerReported() =
        let english =
            "failed FunnySharp.VerticalSlice.Tests.StreamingExportTests.ARowReachesTheClientBeforeTheNextOneExists (1 s)\nTest run summary: Failed!\n  total: 49\n  failed: 1\n  succeeded: 48\n  skipped: 0\n"

        let chinese =
            "\u5931\u8D25 FunnySharp.VerticalSlice.Tests.FlakeProbeTests.DiagnosticProbeFails (21ms)\n\u6D4B\u8BD5\u8FD0\u884C\u6458\u8981: \u5931\u8D25!\n  \u603B\u8BA1: 50\n  \u5931\u8D25: 1\n  \u6210\u529F: 49\n  \u5DF2\u8DF3\u8FC7: 0\n"

        Assert.Equal<string list>(
            [ "FunnySharp.VerticalSlice.Tests.StreamingExportTests.ARowReachesTheClientBeforeTheNextOneExists" ],
            parseFailingTests english
        )

        Assert.Equal<string list>(
            [ "FunnySharp.VerticalSlice.Tests.FlakeProbeTests.DiagnosticProbeFails" ],
            parseFailingTests chinese
        )

        let greenRun =
            defaultTestOutput

        Assert.Empty(parseFailingTests greenRun)

    [<Fact>]
    member this.MeasurementMismatchIsAFailureWithoutAMarkerProblem() =
        runner.MeasurementsExitCode <- 1
        Assert.Equal(1, this.RunTool [])
        let receipt = this.Receipt()
        Assert.Equal("fail", receipt.Status)
        Assert.False receipt.HasMeasurementsPath

        Assert.True(receipt.Failures |> List.exists (fun failure -> failure.Contains "behavioral difference"))

    [<Fact>]
    member this.AFailingConsumerTestIsNamedInTheReceipt() =
        runner.TestOutput <-
            "\u5931\u8D25 FunnySharp.VerticalSlice.Tests.StreamingExportTests.ARowReachesTheClientBeforeTheNextOneExists (1 s)\n\u6D4B\u8BD5\u8FD0\u884C\u6458\u8981: \u5931\u8D25!\n  \u603B\u8BA1: 49\n  \u5931\u8D25: 1\n  \u6210\u529F: 48\n  \u5DF2\u8DF3\u8FC7: 0\n"

        try
            Assert.Equal(1, this.RunTool [])
            let receipt = this.Receipt()
            Assert.Equal("fail", receipt.Status)

            Assert.Equal<string list>(
                [ "FunnySharp.VerticalSlice.Tests.StreamingExportTests.ARowReachesTheClientBeforeTheNextOneExists" ],
                receipt.TestFailures
            )

            Assert.Contains(
                "consumer-tests reported 1 failures: FunnySharp.VerticalSlice.Tests.StreamingExportTests.ARowReachesTheClientBeforeTheNextOneExists",
                receipt.Failures
            )
        finally
            runner.TestOutput <- defaultTestOutput
