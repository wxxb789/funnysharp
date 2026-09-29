module FunnySharp.Harness.Tests.PerformanceTests

// Behaviour tests for FunnySharp.Harness.Performance, the F# port of
// eng/Verify-Performance.ps1. Each fixture is a self-contained repository root
// with one manifest and one receipt; the tests mutate the receipt or manifest
// and assert the exact verdict from the PowerShell parity checklist (section 8
// of .omo/harness-port/performance.md). No pwsh, no sleeps: every fact builds a
// fresh fixture under a disposable temp directory and calls the module in
// process.

open System
open System.IO
open System.Text.Json
open Xunit
open FunnySharp.Harness.Output
open FunnySharp.Harness.Performance
open FunnySharp.Harness.Tests.Support

let private csvName = "FunnySharp.Benchmarks.Fixture-report.csv"
let private githubName = "FunnySharp.Benchmarks.Fixture-report-github.md"
let private htmlName = "FunnySharp.Benchmarks.Fixture-report.html"
let private reportNames = [ csvName; githubName; htmlName ]

let private environmentJson =
    "{\"os\":\"fixture-os\",\"architecture\":\"X64\",\"sdkVersion\":\"10.0.400\",\"runtime\":\".NET 10.0.11\",\"jit\":\"RyuJIT\",\"gcServer\":false,\"gcConcurrent\":true,\"gcAllocationQuantum\":8}"

// Independently derived (printf + sha256sum) SHA-256 of the 8 NUL-joined
// environment fields, so a bug in environmentKey cannot hide behind a fixture
// that used the same helper.
let private expectedEnvironmentKey =
    "fa9d5fbfd3bc1100485289c2225ae6046aa5409609b4a6ec94e5f376c4fe7ca8"

let private successLine =
    "Verified 2 included performance rows and 1 explicit exclusions across 1 receipts."

let private quote (value: string) : string =
    JsonSerializer.Serialize<string> value

let private policyJson (funnyBudget: string) : string =
    "{\"revision\":\"fixture-v1\",\"rows\":["
    + "{\"id\":\"Fixture|Category|Direct|\",\"benchmarkClass\":\"Fixture\",\"category\":\"Category\",\"method\":\"Direct\",\"parameters\":\"\",\"baseline\":true,\"carrier\":\"value\",\"completionPath\":\"synchronous\",\"expectedResult\":\"42\",\"comparisonGroup\":\"fixture\",\"included\":true,\"exclusionReason\":null,\"allocationBudgetBytes\":0},"
    + "{\"id\":\"Fixture|Category|Funny|\",\"benchmarkClass\":\"Fixture\",\"category\":\"Category\",\"method\":\"Funny\",\"parameters\":\"\",\"baseline\":false,\"carrier\":\"value\",\"completionPath\":\"synchronous\",\"expectedResult\":\"42\",\"comparisonGroup\":\"fixture\",\"included\":true,\"exclusionReason\":null,\"allocationBudgetBytes\":"
    + funnyBudget
    + "},"
    + "{\"id\":\"excluded|fixture\",\"benchmarkClass\":\"Fixture\",\"category\":\"Unmeasured\",\"method\":\"\",\"parameters\":\"\",\"baseline\":false,\"carrier\":\"not-measured\",\"completionPath\":\"not-measured\",\"expectedResult\":\"not-measured\",\"comparisonGroup\":\"unmeasured fixture\",\"included\":false,\"exclusionReason\":\"No release claim depends on this scenario.\",\"allocationBudgetBytes\":null}"
    + "]}"

let private manifestJson (policy: string) : string =
    "{\"schemaVersion\":1,"
    + "\"benchmarkInput\":{\"algorithm\":\"sha256\",\"files\":[\"a-input.txt\",\"Z-input.txt\"]},"
    + "\"protocol\":{\"algorithm\":\"sha256\",\"files\":[\"a-protocol.txt\",\"Z-protocol.txt\"]},"
    + "\"policy\":"
    + policy
    + ",\"observation\":null,\"documentation\":[]}"

let private row0Text =
    "{\"id\":\"Fixture|Category|Direct|\",\"benchmarkClass\":\"Fixture\",\"category\":\"Category\",\"method\":\"Direct\",\"parameters\":\"\",\"baseline\":true,\"timingState\":\"below-resolution\",\"meanNanoseconds\":null,\"allocatedBytesPerOperation\":0},"

let private receiptJson
    (policyFingerprint: string)
    (inputFingerprint: string)
    (protocolFingerprint: string)
    (environmentKey: string)
    (reports: string)
    : string =
    "{\"schemaVersion\":1,\"generatedAtUtc\":\"2026-01-01T00:00:00.0000000+00:00\",\"succeeded\":true,\"candidateCommit\":\"fixture\","
    + "\"policyRevision\":\"fixture-v1\","
    + "\"policyFingerprint\":"
    + quote policyFingerprint
    + ","
    + "\"benchmarkInputFingerprint\":"
    + quote inputFingerprint
    + ","
    + "\"protocolFingerprint\":"
    + quote protocolFingerprint
    + ","
    + "\"environmentKey\":"
    + quote environmentKey
    + ","
    + "\"environment\":"
    + environmentJson
    + ",\"reports\":["
    + reports
    + "],\"rows\":["
    + row0Text
    + "{\"id\":\"Fixture|Category|Funny|\",\"benchmarkClass\":\"Fixture\",\"category\":\"Category\",\"method\":\"Funny\",\"parameters\":\"\",\"baseline\":false,\"timingState\":\"observed\",\"meanNanoseconds\":2.5,\"allocatedBytesPerOperation\":16}"
    + "]}"

type private Fixture =
    { Root: string
      ReceiptsDirectory: string
      ManifestPath: string
      ReceiptPath: string
      mutable ManifestText: string
      mutable ReceiptText: string }

let private orFail (result: Result<'T, HarnessError>) : 'T =
    match result with
    | Ok value -> value
    | Error error -> failwith error.Message

let private createFixture (parent: string) (name: string) : Fixture =
    let root = Path.Combine(parent, name)
    let receipts = Path.Combine(root, "receipts")
    Directory.CreateDirectory receipts |> ignore
    File.WriteAllText(Path.Combine(root, "Z-input.txt"), "input-z")
    File.WriteAllText(Path.Combine(root, "a-input.txt"), "input-a")
    File.WriteAllText(Path.Combine(root, "a-protocol.txt"), "protocol-a")
    File.WriteAllText(Path.Combine(root, "Z-protocol.txt"), "protocol-z")

    let manifestPath = Path.Combine(root, "baseline.json")
    let manifestText = manifestJson (policyJson "16")
    File.WriteAllText(manifestPath, manifestText)

    let policyFingerprintValue = orFail (policyFingerprint manifestPath)
    let inputFingerprint = orFail (fileSetFingerprint root [ "a-input.txt"; "Z-input.txt" ])
    let protocolFingerprint = orFail (fileSetFingerprint root [ "a-protocol.txt"; "Z-protocol.txt" ])

    let environmentKeyValue =
        use document = JsonDocument.Parse environmentJson
        orFail (environmentKey document.RootElement)

    for reportName in reportNames do
        File.WriteAllText(Path.Combine(receipts, reportName), reportName)

    let reports =
        reportNames
        |> List.map (fun reportName ->
            sprintf
                "{\"file\":%s,\"sha256\":%s}"
                (quote reportName)
                (quote (fileSha256 (Path.Combine(receipts, reportName)))))
        |> String.concat ","

    let receiptText =
        receiptJson policyFingerprintValue inputFingerprint protocolFingerprint environmentKeyValue reports

    let receiptPath = Path.Combine(receipts, "Fixture-performance-receipt.json")
    File.WriteAllText(receiptPath, receiptText)

    { Root = root
      ReceiptsDirectory = receipts
      ManifestPath = manifestPath
      ReceiptPath = receiptPath
      ManifestText = manifestText
      ReceiptText = receiptText }

let private rewriteManifest (fixture: Fixture) : unit =
    File.WriteAllText(fixture.ManifestPath, fixture.ManifestText)

let private rewriteReceipt (fixture: Fixture) : unit =
    File.WriteAllText(fixture.ReceiptPath, fixture.ReceiptText)

let private invoke (fixture: Fixture) (extra: string list) : int * string * string =
    use stdout = new StringWriter()
    use stderr = new StringWriter()

    let argv =
        [ "-RepositoryRoot"; fixture.Root
          "-ManifestPath"; fixture.ManifestPath
          "-ReceiptDirectory"; fixture.ReceiptsDirectory ]
        @ extra

    let exitCode = mainWith stdout stderr fixture.Root argv
    exitCode, stdout.ToString(), stderr.ToString()

let private withFixture (name: string) (body: Fixture -> unit) : unit =
    use temp = new TempDirectory()
    body (createFixture temp.Path name)

let private assertSucceeds (fixture: Fixture) : unit =
    let exitCode, stdout, stderr = invoke fixture []
    Assert.Equal(0, exitCode)
    Assert.Equal(successLine, stdout.Trim())
    Assert.Equal("", stderr.Trim())

let private assertFails (fixture: Fixture) (expected: string) : unit =
    let exitCode, stdout, stderr = invoke fixture []
    Assert.Equal(1, exitCode)
    Assert.Equal("", stdout)
    Assert.Contains(expected, stderr)

type PerformanceVerifierTests() =

    [<Fact>]
    member _.VerifyPerformance_ValidPolicyAndReceipt_Succeeds() =
        withFixture "valid" assertSucceeds

    [<Fact>]
    member _.VerifyPerformance_ZeroBudgetRegressedToNonZero_Rejects() =
        withFixture
            "zero-regression"
            (fun fixture ->
                fixture.ReceiptText <-
                    fixture.ReceiptText.Replace(
                        "\"allocatedBytesPerOperation\":0}",
                        "\"allocatedBytesPerOperation\":1}"
                    )

                rewriteReceipt fixture
                assertFails fixture "above its 0 B budget")

    [<Fact>]
    member _.VerifyPerformance_AboveNonZeroBudget_Rejects() =
        withFixture
            "ceiling-regression"
            (fun fixture ->
                fixture.ReceiptText <-
                    fixture.ReceiptText.Replace(
                        "\"allocatedBytesPerOperation\":16}",
                        "\"allocatedBytesPerOperation\":17}"
                    )

                rewriteReceipt fixture
                assertFails fixture "above its 16 B budget")

    [<Fact>]
    member _.VerifyPerformance_MissingIncludedRow_Rejects() =
        withFixture
            "missing-row"
            (fun fixture ->
                fixture.ReceiptText <- fixture.ReceiptText.Replace(row0Text, "")
                rewriteReceipt fixture
                assertFails fixture "Required performance rows are missing")

    [<Fact>]
    member _.VerifyPerformance_FractionalAllocation_Rejects() =
        withFixture
            "fractional-allocation"
            (fun fixture ->
                fixture.ReceiptText <-
                    fixture.ReceiptText.Replace(
                        "\"allocatedBytesPerOperation\":16}",
                        "\"allocatedBytesPerOperation\":1.5}"
                    )

                rewriteReceipt fixture
                assertFails fixture "non-integer allocation data")

    [<Fact>]
    member _.VerifyPerformance_UnavailableTiming_IsNonBlocking() =
        withFixture
            "unavailable-timing"
            (fun fixture ->
                fixture.ReceiptText <-
                    fixture.ReceiptText.Replace(
                        "\"timingState\":\"observed\",\"meanNanoseconds\":2.5",
                        "\"timingState\":\"unavailable\",\"meanNanoseconds\":null"
                    )

                rewriteReceipt fixture
                assertSucceeds fixture)

    [<Fact>]
    member _.VerifyPerformance_EnvironmentKeyMismatch_Rejects() =
        withFixture
            "environment-drift"
            (fun fixture ->
                fixture.ReceiptText <-
                    fixture.ReceiptText.Replace("\"runtime\":\".NET 10.0.11\"", "\"runtime\":\".NET changed\"")

                rewriteReceipt fixture
                assertFails fixture "invalid environment key")

    [<Fact>]
    member _.VerifyPerformance_ReportHashMismatch_Rejects() =
        withFixture
            "report-drift"
            (fun fixture ->
                File.WriteAllText(Path.Combine(fixture.ReceiptsDirectory, csvName), "changed")
                assertFails fixture "hash does not match")

    [<Fact>]
    member _.VerifyPerformance_BenchmarkInputFingerprintDrift_Rejects() =
        withFixture
            "input-drift"
            (fun fixture ->
                File.WriteAllText(Path.Combine(fixture.Root, "a-input.txt"), "changed")
                assertFails fixture "does not match the current benchmark input")

    [<Fact>]
    member _.VerifyPerformance_PolicyMutationAfterMeasurement_Rejects() =
        withFixture
            "policy-mutation"
            (fun fixture ->
                fixture.ManifestText <-
                    fixture.ManifestText.Replace("\"allocationBudgetBytes\":16}", "\"allocationBudgetBytes\":32}")

                rewriteManifest fixture
                assertFails fixture "was not measured under the current policy")

    [<Fact>]
    member _.VerifyPerformance_ExcludedRowInReceipt_Rejects() =
        withFixture
            "excluded-row"
            (fun fixture ->
                fixture.ReceiptText <-
                    fixture.ReceiptText.Replace(
                        "\"id\":\"Fixture|Category|Direct|\"",
                        "\"id\":\"excluded|fixture\""
                    )

                rewriteReceipt fixture
                assertFails fixture "unregistered or excluded row")

    [<Fact>]
    member _.VerifyPerformance_EnvironmentKeyAnchor_MatchesIndependentHash() =
        use document = JsonDocument.Parse environmentJson
        let computed = orFail (environmentKey document.RootElement)
        Assert.Equal(expectedEnvironmentKey, computed)

    [<Fact>]
    member _.VerifyPerformance_MissingReceiptDirectory_IsUsageFailure() =
        use temp = new TempDirectory()

        let fixture = createFixture temp.Path "usage"
        use stdout = new StringWriter()
        use stderr = new StringWriter()

        let exitCode =
            mainWith stdout stderr fixture.Root [ "-RepositoryRoot"; fixture.Root; "-ManifestPath"; fixture.ManifestPath ]

        Assert.Equal(2, exitCode)
        Assert.Equal("", stdout.ToString())
        Assert.Contains("required", stderr.ToString())

    [<Fact>]
    member _.VerifyPerformance_UnrecognizedArgument_IsUsageFailure() =
        use temp = new TempDirectory()
        let fixture = createFixture temp.Path "unknown"
        let exitCode, _, stderr = invoke fixture [ "-NotAFlag"; "x" ]
        Assert.Equal(2, exitCode)
        Assert.Contains("unrecognized", stderr)

    [<Fact>]
    member _.VerifyPerformance_ObservationProposal_HasTheOrderedKeySet() =
        withFixture
            "proposal"
            (fun fixture ->
                let proposalPath = Path.Combine(fixture.Root, "artifacts", "observation.json")

                let exitCode, stdout, _ =
                    invoke fixture [ "-ObservationProposalPath"; proposalPath ]

                Assert.Equal(0, exitCode)
                Assert.Equal(successLine, stdout.Trim())
                Assert.True(File.Exists proposalPath)

                use document = JsonDocument.Parse(File.ReadAllText proposalPath)

                let names =
                    document.RootElement.EnumerateObject() |> Seq.map (fun property -> property.Name) |> Seq.toList

                Assert.Equal<string list>(
                    [ "schemaVersion"
                      "generatedAtUtc"
                      "candidateCommit"
                      "policyRevision"
                      "policyFingerprint"
                      "benchmarkInputFingerprint"
                      "protocolFingerprint"
                      "environmentKey"
                      "receipts"
                      "rows" ],
                    names
                )
            )
