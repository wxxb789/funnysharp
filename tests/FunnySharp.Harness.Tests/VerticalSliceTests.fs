module FunnySharp.Harness.Tests.VerticalSliceTests

// F# port of eng/tools/tests/test_vertical_slice.py. The suite never runs the
// real .NET pipeline: a fake step runner stands in for the SDK and returns canned
// output, so the step wiring, the summary parser and the failure aggregation are
// exercised deterministically. The fake validates the generated restore config
// and records commands and environments to verify sources, versions and cache.

open System
open System.IO
open System.Text.Json
open System.Text.RegularExpressions
open System.Xml.Linq
open Xunit
open FunnySharp.Harness.Proc
open FunnySharp.Harness.Tests.Support
open FunnySharp.Harness.VerticalSlice

// ---- A typed view of the receipt, so the assertions never dereference a
// nullable JsonNode. ----

type private StepRecord =
    { Step: string
      Command: string list
      ExitCode: int
      StdoutTail: string
      StderrTail: string }

type private Receipt =
    { Status: string
      Feed: string
      NugetPackages: string
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
              Command = step.GetProperty("command").EnumerateArray() |> Seq.map textValue |> Seq.toList
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
      Feed = textValue (root.GetProperty "feed")
      NugetPackages = textValue (root.GetProperty "nugetPackages")
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

/// The identity and the localized runner output a real failing consumer run prints: the
/// per-test failure line, then the summary.
let private failingTestName =
    "FunnySharp.VerticalSlice.Tests.StreamingExportTests.ARowReachesTheClientBeforeTheNextOneExists"

let private failingRunOutput =
    "\u5931\u8D25 "
    + failingTestName
    + " (1 s)\n\u6D4B\u8BD5\u8FD0\u884C\u6458\u8981: \u5931\u8D25!\n  \u603B\u8BA1: 49\n  \u5931\u8D25: 1\n  \u6210\u529F: 48\n  \u5DF2\u8DF3\u8FC7: 0\n"

let private restoreConfigPath (command: string list) : string option =
    command
    |> List.pairwise
    |> List.tryPick (fun (flag, value) -> if flag = "--configfile" then Some value else None)

let private readPackageSources (path: string) : string list * (string * string) list =
    let document = XDocument.Load path

    let packageSources =
        document.Elements(XName.Get "configuration")
        |> Seq.collect (fun root -> root.Elements(XName.Get "packageSources"))
        |> Seq.exactlyOne

    let order = packageSources.Elements() |> Seq.map (fun element -> element.Name.LocalName) |> Seq.toList

    let attributeValue (name: string) (element: XElement) =
        match element.Attribute(XName.Get name) with
        | null -> failwithf "package source is missing %s" name
        | attribute -> attribute.Value

    let sources =
        packageSources.Elements(XName.Get "add")
        |> Seq.map (fun element -> attributeValue "key" element, attributeValue "value" element)
        |> Seq.toList

    order, sources

type private FakeRunner(expectedFeed: string) =
    let calls = ResizeArray<string list>()
    let environments = ResizeArray<(string * string) list>()

    member val ExitCode = 0 with get, set
    member val PackVersion = "2.0.0" with get, set
    member val MeasurementsExitCode = 0 with get, set
    member val TestOutput = defaultTestOutput with get, set
    member val PackageFeed = "https://api.nuget.org/v3/index.json" with get, set
    member _.Calls = List.ofSeq calls
    member _.Environments = List.ofSeq environments

    member this.Run
        (_workingDirectory: string)
        (environment: (string * string) list)
        (_timeoutSeconds: int)
        (command: string list)
        : ProcessResult =
        calls.Add command
        environments.Add environment
        let subcommand = if command.Length > 1 then command.[1] else ""
        let joined = String.concat " " command

        let restoreError =
            if subcommand <> "restore" then
                None
            else
                match restoreConfigPath command with
                | Some path when File.Exists path ->
                    let order, sources = readPackageSources path

                    if order = [ "clear"; "add"; "add" ]
                       && sources = [ "local", expectedFeed; "nuget.org", this.PackageFeed ] then
                        None
                    else
                        Some "restore config does not reset the exact package sources"
                | _ -> Some "restore requires an existing --configfile"

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

        { ExitCode =
              match restoreError with
              | Some _ -> 1
              | None -> if isMeasurementsRun then this.MeasurementsExitCode else this.ExitCode
          Stdout = if restoreError.IsSome then "" else stdout
          Stderr = restoreError |> Option.defaultValue "" }

type VerticalSliceToolTests() =
    let originalRunner = stepRunner
    let temp = new TempDirectory()
    let feed = Path.Combine(temp.Path, "feed & packages 'candidate'")
    let output = Path.Combine(temp.Path, "output & receipts 'candidate'")
    let runner = FakeRunner(Path.GetFullPath feed)

    do
        Directory.CreateDirectory feed |> ignore
        File.WriteAllBytes(Path.Combine(feed, "FunnySharp.1.2.3.nupkg"), [| 1uy |])
        File.WriteAllBytes(Path.Combine(feed, "FunnySharp.AspNetCore.1.2.3.nupkg"), [| 2uy |])
        File.WriteAllBytes(Path.Combine(feed, "FunnySharp.AspNetCore.1.2.3.snupkg"), [| 3uy |])
        stepRunner <- runner.Run

    member private _.BaseArgs =
        [ "--feed"; Path.Combine("unused feed", "..", Path.GetRelativePath(temp.Path, feed))
          "--output"; output; "--repository-root"; temp.Path ]

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

    [<Theory>]
    [<InlineData(false)>]
    [<InlineData(true)>]
    member this.ConfiguredPackageFeedIsPreservedForEveryRestore(equalsForm: bool) =
        let upstream = "https://feed.example/v3/index.json?source=nuget&name='dependency'"
        runner.PackageFeed <- upstream
        let arguments =
            if equalsForm then [ "--package-feed=" + upstream ]
            else [ "--package-feed"; upstream ]
        Assert.Equal(0, this.RunTool arguments)
        let restores =
            runner.Calls
            |> List.filter (fun command -> command.Length > 2 && command.[1] = "restore")
        Assert.Equal(4, restores.Length)
        for command in restores do
            let config = restoreConfigPath command |> Option.get
            let order, sources = readPackageSources config
            Assert.Equal<string list>([ "clear"; "add"; "add" ], order)
            Assert.Equal<(string * string) list>(
                [ "local", Path.GetFullPath feed; "nuget.org", upstream ], sources)
            Assert.DoesNotContain("--source", command)

    [<Fact>]
    member this.ConsumerProjectsAreRestoredFromTheLocalFeedWithInjectedVersions() =
        let aspNetCoreVersion = "4.5.6-preview.7+audit.8"
        File.Delete(Path.Combine(feed, "FunnySharp.AspNetCore.1.2.3.nupkg"))
        File.WriteAllBytes(Path.Combine(feed, sprintf "FunnySharp.AspNetCore.%s.nupkg" aspNetCoreVersion), [| 2uy |])
        let exitCode = this.RunTool []

        let restores =
            runner.Calls
            |> List.filter (fun command -> command.Length > 2 && command.[1] = "restore")

        Assert.Equal(4, restores.Length)

        let expectedProjects =
            Set.ofList
                [ "FunnySharp.VerticalSlice.Api"; "FunnySharp.VerticalSlice.Tests"
                  "FunnySharp.VerticalSlice.Baseline"; "FunnySharp.VerticalSlice.Measurements" ]

        Assert.Equal<Set<string>>(
            expectedProjects,
            restores
            |> List.map (fun command ->
                match Path.GetFileNameWithoutExtension command.[2] with
                | null -> failwith "restore project path has no file name"
                | name -> name)
            |> Set.ofList
        )

        for command in restores do
            Assert.DoesNotContain("--source", command)
            Assert.Equal(1, command |> List.filter ((=) "--configfile") |> List.length)
            Assert.Contains("-p:FunnySharpPackageVersion=1.2.3", command)
            Assert.Contains("-p:FunnySharpAspNetCorePackageVersion=" + aspNetCoreVersion, command)

        let configs = restores |> List.map (restoreConfigPath >> Option.get) |> List.distinct
        Assert.Equal(1, configs.Length)
        let config = List.head configs
        Assert.True(Path.IsPathFullyQualified config)
        Assert.Equal(Path.GetFullPath output, Path.GetDirectoryName config)

        let order, sources = readPackageSources config
        Assert.Equal<string list>([ "clear"; "add"; "add" ], order)

        Assert.Equal<(string * string) list>(
            [ "local", Path.GetFullPath feed; "nuget.org", "https://api.nuget.org/v3/index.json" ],
            sources
        )

        let expectedCache = Path.Combine(Path.GetFullPath output, "nuget-packages")

        for environment in runner.Environments do
            Assert.Equal(expectedCache, (Map.ofList environment).["NUGET_PACKAGES"])

        Assert.Equal(0, exitCode)
        let receipt = this.Receipt()
        Assert.Equal("pass", receipt.Status)
        Assert.Equal(Path.GetFullPath feed, receipt.Feed)
        Assert.Equal(expectedCache, receipt.NugetPackages)
        Assert.Equal("1.2.3", receipt.PackageVersions.["FunnySharp"])
        Assert.Equal(aspNetCoreVersion, receipt.PackageVersions.["FunnySharp.AspNetCore"])

        Assert.Equal<string list list>(
            restores,
            receipt.Steps
            |> List.filter (fun step -> step.Step.StartsWith("restore-", StringComparison.Ordinal))
            |> List.map (fun step -> step.Command)
        )

    [<Fact>]
    member this.RestoreConfigurationsBelongToTheirOutputAttempts() =
        Assert.Equal(0, this.RunTool [])
        let firstConfigs = runner.Calls |> List.choose restoreConfigPath |> List.distinct
        Assert.Equal(1, firstConfigs.Length)
        let firstConfig = List.head firstConfigs
        let originalSources = readPackageSources firstConfig
        let nextOutput = Path.Combine(temp.Path, "second output & receipts 'candidate'")

        Assert.Equal(0, this.RunTool [ "--output"; nextOutput ])
        let configs = runner.Calls |> List.choose restoreConfigPath |> List.distinct
        Assert.Equal(2, configs.Length)
        let secondConfig = List.last configs
        Assert.NotEqual<string>(firstConfig, secondConfig)
        Assert.Equal(Path.GetFullPath output, Path.GetDirectoryName firstConfig)
        Assert.Equal(Path.GetFullPath nextOutput, Path.GetDirectoryName secondConfig)
        Assert.Equal<string list * (string * string) list>(originalSources, readPackageSources firstConfig)
        Assert.Equal<string list * (string * string) list>(originalSources, readPackageSources secondConfig)

        let caches =
            runner.Environments
            |> List.map (fun environment -> (Map.ofList environment).["NUGET_PACKAGES"])
            |> List.distinct

        Assert.Equal<string list>(
            [ Path.Combine(Path.GetFullPath output, "nuget-packages")
              Path.Combine(Path.GetFullPath nextOutput, "nuget-packages") ],
            caches
        )

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
            "failed " + failingTestName + " (1 s)\nTest run summary: Failed!\n  total: 49\n  failed: 1\n  succeeded: 48\n  skipped: 0\n"

        // A parameterized display name carries its own parentheses ahead of the duration.
        let parameterized =
            "failed FunnySharp.VerticalSlice.Tests.SomeTests.MyCase (net10.0) (21ms)\nTest run summary: Failed!\n  total: 3\n  failed: 1\n  succeeded: 2\n  skipped: 0\n"

        Assert.Equal<string list>([ failingTestName ], parseFailingTests english)
        Assert.Equal<string list>([ failingTestName ], parseFailingTests failingRunOutput)

        Assert.Equal<string list>(
            [ "FunnySharp.VerticalSlice.Tests.SomeTests.MyCase (net10.0)" ],
            parseFailingTests parameterized
        )

        Assert.Empty(parseFailingTests defaultTestOutput)

        Assert.Empty(
            parseFailingTests
                "Completed test run with non-success exit code: 2 (see: https://aka.ms/testingplatform/exitcodes)\n"
        )

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
        runner.TestOutput <- failingRunOutput

        Assert.Equal(1, this.RunTool [])
        let receipt = this.Receipt()
        Assert.Equal("fail", receipt.Status)
        Assert.Equal<string list>([ failingTestName ], receipt.TestFailures)

        let named = "consumer-tests reported 1 failed test: " + failingTestName
        Assert.Contains(named, receipt.Failures)

    [<Fact>]
    member this.MultipleFailingConsumerTestsAreAllNamed() =
        let secondName = "FunnySharp.VerticalSlice.Tests.OrderEndpointTests.APlacementFails"

        runner.TestOutput <-
            "\u5931\u8D25 "
            + failingTestName
            + " (1 s)\n\u5931\u8D25 "
            + secondName
            + " (12ms)\n\u6D4B\u8BD5\u8FD0\u884C\u6458\u8981: \u5931\u8D25!\n  \u603B\u8BA1: 49\n  \u5931\u8D25: 2\n  \u6210\u529F: 47\n  \u5DF2\u8DF3\u8FC7: 0\n"

        Assert.Equal(1, this.RunTool [])
        let receipt = this.Receipt()
        Assert.Equal("fail", receipt.Status)
        Assert.Equal<string list>([ failingTestName; secondName ], receipt.TestFailures)

        let named = "consumer-tests reported 2 failed tests: " + failingTestName + ", " + secondName
        Assert.Contains(named, receipt.Failures)

    [<Fact>]
    member this.AnUnnamedConsumerTestFailureKeepsTheMessageReadable() =
        runner.TestOutput <- "Test run summary: Failed!\n  total: 49\n  failed: 2\n  succeeded: 47\n  skipped: 0\n"

        Assert.Equal(1, this.RunTool [])
        let receipt = this.Receipt()
        Assert.Equal("fail", receipt.Status)
        Assert.Empty receipt.TestFailures

        Assert.Contains("consumer-tests reported 2 failed tests", receipt.Failures)
