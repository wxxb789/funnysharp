module FunnySharp.Harness.Tests.PerformanceProtocolTests

// Port of eng/tests/PerformanceProtocol.Tests.ps1 to xUnit facts (lane D of goal 04).
// Each fact rebuilds its fixture in a fresh temporary directory; no timing, no
// sleeps, no skips. The fingerprint and environment-key helpers below are a local
// re-implementation (mirroring the PowerShell suite) so the fixtures stay an
// independent oracle; the assertions call the F# Performance (verifier) and
// PerformanceDocs (generator) modules in-process rather than through a subprocess.

open System
open System.Globalization
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Text.RegularExpressions
open Xunit
open FunnySharp.Harness.Tests.Support

// ---- Compact JSON builders matching the PowerShell fixture's shape ----

let private utf8NoBom = UTF8Encoding(false)
let private newline = Environment.NewLine

let private jsonString (value: string) : string =
    let builder = StringBuilder()
    builder.Append('"') |> ignore

    for ch in value do
        match ch with
        | '"' -> builder.Append("\\\"") |> ignore
        | '\\' -> builder.Append("\\\\") |> ignore
        | '\n' -> builder.Append("\\n") |> ignore
        | '\r' -> builder.Append("\\r") |> ignore
        | '\t' -> builder.Append("\\t") |> ignore
        | c when c < ' ' -> builder.AppendFormat("\\u{0:x4}", int c) |> ignore
        | c -> builder.Append(c) |> ignore

    builder.Append('"') |> ignore
    builder.ToString()

let private jsonObjectText (pairs: (string * string) list) : string =
    "{" + String.concat "," (pairs |> List.map (fun (name, value) -> jsonString name + ":" + value)) + "}"

let private jsonArrayText (items: string list) : string =
    "[" + String.concat "," items + "]"

let private jsonBool (value: bool) : string =
    if value then "true" else "false"

let private jsonNumber (value: float) : string =
    value.ToString("R", CultureInfo.InvariantCulture)

let private writeText (path: string) (text: string) : unit =
    File.WriteAllText(path, text, utf8NoBom)

let private writeJson (path: string) (json: string) : unit =
    File.WriteAllText(path, json + newline, utf8NoBom)

// ---- Local fingerprint / environment-key helpers (mirror the PS test suite) ----

let private textSha256 (value: string) : string =
    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes value)).ToLowerInvariant()

let private fileSha256 (path: string) : string =
    Convert.ToHexString(SHA256.HashData(File.ReadAllBytes path)).ToLowerInvariant()

let private policyFingerprint (path: string) : string =
    use document = JsonDocument.Parse(File.ReadAllText path)
    textSha256 (document.RootElement.GetProperty("policy").GetRawText())

let private fileSetFingerprint (root: string) (files: string list) : string =
    let ordered = files |> List.sortWith (fun left right -> String.CompareOrdinal(left, right))
    use stream = new MemoryStream()

    for relativePath in ordered do
        let path = Path.Combine(root, relativePath)
        let line = relativePath.Replace('\\', '/') + string (char 0) + fileSha256 path + string (char 10)
        let bytes = Encoding.UTF8.GetBytes line
        stream.Write(bytes, 0, bytes.Length)

    Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant()

// ---- Fixture data types ----

type private PolicyRow =
    { Id: string
      BenchmarkClass: string
      Category: string
      Method: string
      Parameters: string
      Baseline: bool
      Carrier: string
      CompletionPath: string
      ExpectedResult: string
      ComparisonGroup: string
      Included: bool
      ExclusionReason: string option
      AllocationBudgetBytes: int option }

    member this.Json =
        jsonObjectText
            [ "id", jsonString this.Id
              "benchmarkClass", jsonString this.BenchmarkClass
              "category", jsonString this.Category
              "method", jsonString this.Method
              "parameters", jsonString this.Parameters
              "baseline", jsonBool this.Baseline
              "carrier", jsonString this.Carrier
              "completionPath", jsonString this.CompletionPath
              "expectedResult", jsonString this.ExpectedResult
              "comparisonGroup", jsonString this.ComparisonGroup
              "included", jsonBool this.Included
              "exclusionReason",
              (match this.ExclusionReason with
               | Some reason -> jsonString reason
               | None -> "null")
              "allocationBudgetBytes",
              (match this.AllocationBudgetBytes with
               | Some budget -> string budget
               | None -> "null") ]

type private ObservationRow =
    { Id: string
      BenchmarkClass: string
      Category: string
      Method: string
      Parameters: string
      Baseline: bool
      TimingState: string
      MeanNanoseconds: float option
      AllocatedBytesPerOperation: float }

    member this.Json =
        jsonObjectText
            [ "id", jsonString this.Id
              "benchmarkClass", jsonString this.BenchmarkClass
              "category", jsonString this.Category
              "method", jsonString this.Method
              "parameters", jsonString this.Parameters
              "baseline", jsonBool this.Baseline
              "timingState", jsonString this.TimingState
              "meanNanoseconds",
              (match this.MeanNanoseconds with
               | Some mean -> jsonNumber mean
               | None -> "null")
              "allocatedBytesPerOperation", jsonNumber this.AllocatedBytesPerOperation ]

type private FixtureEnvironment =
    { Os: string
      Architecture: string
      SdkVersion: string
      Runtime: string
      Jit: string
      GcServer: bool
      GcConcurrent: bool
      GcAllocationQuantum: int }

    member this.Json =
        jsonObjectText
            [ "os", jsonString this.Os
              "architecture", jsonString this.Architecture
              "sdkVersion", jsonString this.SdkVersion
              "runtime", jsonString this.Runtime
              "jit", jsonString this.Jit
              "gcServer", jsonBool this.GcServer
              "gcConcurrent", jsonBool this.GcConcurrent
              "gcAllocationQuantum", string this.GcAllocationQuantum ]

    member this.Key =
        textSha256 (
            String.concat
                (string (char 0))
                [ this.Os
                  this.Architecture
                  this.SdkVersion
                  this.Runtime
                  this.Jit
                  jsonBool this.GcServer
                  jsonBool this.GcConcurrent
                  string this.GcAllocationQuantum ]
        )

type private ObservationData =
    { SchemaVersion: int
      PolicyRevision: string
      PolicyFingerprint: string
      BenchmarkInputFingerprint: string
      ProtocolFingerprint: string
      EnvironmentKey: string
      Rows: ObservationRow list }

    member this.Json =
        jsonObjectText
            [ "schemaVersion", string this.SchemaVersion
              "policyRevision", jsonString this.PolicyRevision
              "policyFingerprint", jsonString this.PolicyFingerprint
              "benchmarkInputFingerprint", jsonString this.BenchmarkInputFingerprint
              "protocolFingerprint", jsonString this.ProtocolFingerprint
              "environmentKey", jsonString this.EnvironmentKey
              "rows", jsonArrayText (this.Rows |> List.map (fun row -> row.Json)) ]

type private DocumentationEntry =
    { Id: string
      Path: string
      BenchmarkClasses: string list }

    member this.Json =
        jsonObjectText
            [ "id", jsonString this.Id
              "path", jsonString this.Path
              "benchmarkClasses", jsonArrayText (this.BenchmarkClasses |> List.map jsonString) ]

type private Fixture =
    { Root: string
      ManifestPath: string
      ReceiptPath: string
      ReceiptsDir: string
      Environment: FixtureEnvironment
      EnvironmentKey: string
      CandidateCommit: string
      PolicyRevision: string
      PolicyFingerprint: string
      InputFingerprint: string
      ProtocolFingerprint: string
      ReportHashes: (string * string) list
      ReceiptRows: ObservationRow list
      PolicyRows: PolicyRow list
      Documentation: DocumentationEntry list
      Observation: ObservationData option }

// ---- Serialisation ----

let private manifestJson
    (policyRows: PolicyRow list)
    (observation: ObservationData option)
    (documentation: DocumentationEntry list)
    : string =
    jsonObjectText
        [ "schemaVersion", "1"
          "benchmarkInput",
          jsonObjectText [ "files", jsonArrayText [ jsonString "a-input.txt"; jsonString "Z-input.txt" ] ]
          "protocol",
          jsonObjectText [ "files", jsonArrayText [ jsonString "a-protocol.txt"; jsonString "Z-protocol.txt" ] ]
          "policy",
          jsonObjectText
              [ "revision", jsonString "fixture-v1"
                "rows", jsonArrayText (policyRows |> List.map (fun row -> row.Json)) ]
          "observation",
          (match observation with
           | Some value -> value.Json
           | None -> "null")
          "documentation", jsonArrayText (documentation |> List.map (fun entry -> entry.Json)) ]

let private receiptJson (fixture: Fixture) : string =
    jsonObjectText
        [ "schemaVersion", "1"
          "generatedAtUtc", jsonString "2026-09-25T00:00:00.0000000+00:00"
          "succeeded", "true"
          "candidateCommit", jsonString fixture.CandidateCommit
          "policyRevision", jsonString fixture.PolicyRevision
          "policyFingerprint", jsonString fixture.PolicyFingerprint
          "benchmarkInputFingerprint", jsonString fixture.InputFingerprint
          "protocolFingerprint", jsonString fixture.ProtocolFingerprint
          "environmentKey", jsonString fixture.EnvironmentKey
          "environment", fixture.Environment.Json
          "reports",
          jsonArrayText (
              fixture.ReportHashes
              |> List.map (fun (name, hash) ->
                  jsonObjectText [ "file", jsonString name; "sha256", jsonString hash ])
          )
          "rows", jsonArrayText (fixture.ReceiptRows |> List.map (fun row -> row.Json)) ]

let private writeManifest (fixture: Fixture) : unit =
    writeJson fixture.ManifestPath (manifestJson fixture.PolicyRows fixture.Observation fixture.Documentation)

let private writeReceipt (fixture: Fixture) : unit =
    writeJson fixture.ReceiptPath (receiptJson fixture)

// ---- Fixture construction ----

let private defaultEnvironment =
    { Os = "fixture-os"
      Architecture = "X64"
      SdkVersion = "10.0.400"
      Runtime = ".NET 10.0.11"
      Jit = "RyuJIT"
      GcServer = false
      GcConcurrent = true
      GcAllocationQuantum = 8 }

let private basePolicyRows =
    [ { Id = "Fixture|Category|Direct|"
        BenchmarkClass = "Fixture"
        Category = "Category"
        Method = "Direct"
        Parameters = ""
        Baseline = true
        Carrier = "value"
        CompletionPath = "synchronous"
        ExpectedResult = "42"
        ComparisonGroup = "fixture"
        Included = true
        ExclusionReason = None
        AllocationBudgetBytes = Some 0 }
      { Id = "Fixture|Category|Funny|"
        BenchmarkClass = "Fixture"
        Category = "Category"
        Method = "Funny"
        Parameters = ""
        Baseline = false
        Carrier = "value"
        CompletionPath = "synchronous"
        ExpectedResult = "42"
        ComparisonGroup = "fixture"
        Included = true
        ExclusionReason = None
        AllocationBudgetBytes = Some 16 }
      { Id = "excluded|fixture"
        BenchmarkClass = "Fixture"
        Category = "Unmeasured"
        Method = ""
        Parameters = ""
        Baseline = false
        Carrier = "not-measured"
        CompletionPath = "not-measured"
        ExpectedResult = "not-measured"
        ComparisonGroup = "unmeasured fixture"
        Included = false
        ExclusionReason = Some "No release claim depends on this scenario."
        AllocationBudgetBytes = None } ]

let private baseReceiptRows =
    [ { Id = "Fixture|Category|Direct|"
        BenchmarkClass = "Fixture"
        Category = "Category"
        Method = "Direct"
        Parameters = ""
        Baseline = true
        TimingState = "below-resolution"
        MeanNanoseconds = None
        AllocatedBytesPerOperation = 0.0 }
      { Id = "Fixture|Category|Funny|"
        BenchmarkClass = "Fixture"
        Category = "Category"
        Method = "Funny"
        Parameters = ""
        Baseline = false
        TimingState = "observed"
        MeanNanoseconds = Some 2.5
        AllocatedBytesPerOperation = 16.0 } ]

let private newFixture (root: string) : Fixture =
    let receiptsDir = Path.Combine(root, "receipts")
    Directory.CreateDirectory receiptsDir |> ignore
    writeText (Path.Combine(root, "Z-input.txt")) "input-z"
    writeText (Path.Combine(root, "a-input.txt")) "input-a"
    writeText (Path.Combine(root, "Z-protocol.txt")) "protocol-z"
    writeText (Path.Combine(root, "a-protocol.txt")) "protocol-a"

    let reportNames =
        [ "FunnySharp.Benchmarks.Fixture-report.csv"
          "FunnySharp.Benchmarks.Fixture-report-github.md"
          "FunnySharp.Benchmarks.Fixture-report.html" ]

    for reportName in reportNames do
        writeText (Path.Combine(receiptsDir, reportName)) reportName

    let reportHashes =
        reportNames
        |> List.map (fun reportName -> reportName, fileSha256 (Path.Combine(receiptsDir, reportName)))

    let environment = defaultEnvironment
    let manifestPath = Path.Combine(root, "baseline.json")
    let receiptPath = Path.Combine(receiptsDir, "Fixture-performance-receipt.json")

    let skeleton =
        { Root = root
          ManifestPath = manifestPath
          ReceiptPath = receiptPath
          ReceiptsDir = receiptsDir
          Environment = environment
          EnvironmentKey = environment.Key
          CandidateCommit = "fixture"
          PolicyRevision = "fixture-v1"
          PolicyFingerprint = ""
          InputFingerprint = ""
          ProtocolFingerprint = ""
          ReportHashes = reportHashes
          ReceiptRows = baseReceiptRows
          PolicyRows = basePolicyRows
          Documentation = []
          Observation = None }

    writeManifest skeleton

    let fixture =
        { skeleton with
            PolicyFingerprint = policyFingerprint manifestPath
            InputFingerprint = fileSetFingerprint root [ "a-input.txt"; "Z-input.txt" ]
            ProtocolFingerprint = fileSetFingerprint root [ "a-protocol.txt"; "Z-protocol.txt" ] }

    writeReceipt fixture
    fixture

let private newDocumentationFixture (root: string) : Fixture * string =
    let fixture = newFixture root
    let guidePath = Path.Combine(root, "guide.md")

    writeText
        guidePath
        (String.concat
            newline
            [ "# Guide"
              ""
              "<!-- performance-table:start fixture -->"
              "old"
              "<!-- performance-table:end fixture -->"
              "" ])

    let alternativePolicy =
        { Id = "Fixture|Category|Alternative|"
          BenchmarkClass = "Fixture"
          Category = "Category"
          Method = "Alternative"
          Parameters = ""
          Baseline = false
          Carrier = "value"
          CompletionPath = "synchronous"
          ExpectedResult = "42"
          ComparisonGroup = "fixture"
          Included = true
          ExclusionReason = None
          AllocationBudgetBytes = Some 8 }

    let alternativeObservation =
        { Id = "Fixture|Category|Alternative|"
          BenchmarkClass = "Fixture"
          Category = "Category"
          Method = "Alternative"
          Parameters = ""
          Baseline = false
          TimingState = "unavailable"
          MeanNanoseconds = None
          AllocatedBytesPerOperation = 8.0 }

    let documentation = [ { Id = "fixture"; Path = "guide.md"; BenchmarkClasses = [ "Fixture" ] } ]

    let withDocumentation =
        { fixture with
            PolicyRows = fixture.PolicyRows @ [ alternativePolicy ]
            Documentation = documentation
            Observation = None }

    writeManifest withDocumentation
    let measuredPolicyFingerprint = policyFingerprint fixture.ManifestPath

    let observation =
        { SchemaVersion = 1
          PolicyRevision = "fixture-v1"
          PolicyFingerprint = measuredPolicyFingerprint
          BenchmarkInputFingerprint = fixture.InputFingerprint
          ProtocolFingerprint = fixture.ProtocolFingerprint
          EnvironmentKey = fixture.EnvironmentKey
          Rows = baseReceiptRows @ [ alternativeObservation ] }

    let withObservation = { withDocumentation with Observation = Some observation }
    writeManifest withObservation
    let finalPolicyFingerprint = policyFingerprint fixture.ManifestPath

    if finalPolicyFingerprint = measuredPolicyFingerprint then
        withObservation, guidePath
    else
        let corrected =
            { withObservation with
                Observation = Some { observation with PolicyFingerprint = finalPolicyFingerprint } }

        writeManifest corrected
        corrected, guidePath

// ---- Harness invocation and assertions ----

let private runVerifier (fixture: Fixture) : int * string * string =
    use stdout = new StringWriter()
    use stderr = new StringWriter()

    let exitCode =
        FunnySharp.Harness.Performance.mainWith
            stdout
            stderr
            fixture.Root
            [ "-RepositoryRoot"; fixture.Root
              "-ManifestPath"; fixture.ManifestPath
              "-ReceiptDirectory"; fixture.ReceiptsDir ]

    exitCode, stdout.ToString(), stderr.ToString()

let private runGenerator (fixture: Fixture) (verify: bool) : int * string * string =
    use stdout = new StringWriter()
    use stderr = new StringWriter()

    let argv =
        [ "-RepositoryRoot"; fixture.Root; "-ManifestPath"; fixture.ManifestPath ]
        @ (if verify then [ "-Verify" ] else [])

    let exitCode = FunnySharp.Harness.PerformanceDocs.mainWith stdout stderr argv
    exitCode, stdout.ToString(), stderr.ToString()

let private assertPassed (exitCode: int) (stderr: string) : unit =
    Assert.True((exitCode = 0), sprintf "expected success, got exit code %d (stderr: %s)" exitCode stderr)

let private assertFailsWith (pattern: string) (exitCode: int) (stderr: string) : unit =
    Assert.True((exitCode <> 0), sprintf "expected a failure, got exit code 0 (stderr: %s)" stderr)

    Assert.True(
        Regex.IsMatch(stderr, pattern, RegexOptions.IgnoreCase),
        sprintf "expected failure matching '%s', got '%s'" pattern stderr
    )

// ---- Facts ----

type PerformanceProtocolTests() =

    [<Fact>]
    member _.VerifyPerformance_ValidPolicyAndReceipt_Succeeds() =
        use temp = new TempDirectory()
        let fixture = newFixture (Path.Combine(temp.Path, "valid"))
        let exitCode, stdout, stderr = runVerifier fixture
        assertPassed exitCode stderr

        Assert.Contains(
            "Verified 2 included performance rows and 1 explicit exclusions across 1 receipts.",
            stdout
        )

    [<Fact>]
    member _.VerifyPerformance_ZeroBudgetRegressedToNonZero_Rejects() =
        use temp = new TempDirectory()
        let fixture = newFixture (Path.Combine(temp.Path, "zero-regression"))

        let mutated =
            { fixture with
                ReceiptRows =
                    [ { fixture.ReceiptRows.[0] with AllocatedBytesPerOperation = 1.0 }
                      fixture.ReceiptRows.[1] ] }

        writeReceipt mutated
        let exitCode, _, stderr = runVerifier mutated
        assertFailsWith "above its 0 B budget|Zero-allocation" exitCode stderr

    [<Fact>]
    member _.VerifyPerformance_AboveNonZeroBudget_Rejects() =
        use temp = new TempDirectory()
        let fixture = newFixture (Path.Combine(temp.Path, "ceiling-regression"))

        let mutated =
            { fixture with
                ReceiptRows =
                    [ fixture.ReceiptRows.[0]
                      { fixture.ReceiptRows.[1] with AllocatedBytesPerOperation = 17.0 } ] }

        writeReceipt mutated
        let exitCode, _, stderr = runVerifier mutated
        assertFailsWith "above its 16 B budget" exitCode stderr

    [<Fact>]
    member _.VerifyPerformance_MissingIncludedRow_Rejects() =
        use temp = new TempDirectory()
        let fixture = newFixture (Path.Combine(temp.Path, "missing-row"))
        let mutated = { fixture with ReceiptRows = [ fixture.ReceiptRows.[0] ] }
        writeReceipt mutated
        let exitCode, _, stderr = runVerifier mutated
        assertFailsWith "Required performance rows are missing" exitCode stderr

    [<Fact>]
    member _.VerifyPerformance_FractionalAllocation_Rejects() =
        use temp = new TempDirectory()
        let fixture = newFixture (Path.Combine(temp.Path, "fractional-allocation"))

        let mutated =
            { fixture with
                ReceiptRows =
                    [ fixture.ReceiptRows.[0]
                      { fixture.ReceiptRows.[1] with AllocatedBytesPerOperation = 1.5 } ] }

        writeReceipt mutated
        let exitCode, _, stderr = runVerifier mutated
        assertFailsWith "non-integer allocation data" exitCode stderr

    [<Fact>]
    member _.VerifyPerformance_UnavailableTiming_IsNonBlocking() =
        use temp = new TempDirectory()
        let fixture = newFixture (Path.Combine(temp.Path, "unavailable-timing"))

        let mutated =
            { fixture with
                ReceiptRows =
                    [ fixture.ReceiptRows.[0]
                      { fixture.ReceiptRows.[1] with
                          TimingState = "unavailable"
                          MeanNanoseconds = None } ] }

        writeReceipt mutated
        let exitCode, stdout, stderr = runVerifier mutated
        assertPassed exitCode stderr
        Assert.Contains("Verified 2 included performance rows", stdout)

    [<Fact>]
    member _.VerifyPerformance_EnvironmentKeyMismatch_Rejects() =
        use temp = new TempDirectory()
        let fixture = newFixture (Path.Combine(temp.Path, "environment-drift"))

        let mutated =
            { fixture with Environment = { fixture.Environment with Runtime = ".NET changed" } }

        writeReceipt mutated
        let exitCode, _, stderr = runVerifier mutated
        assertFailsWith "invalid environment key" exitCode stderr

    [<Fact>]
    member _.VerifyPerformance_ReportHashMismatch_Rejects() =
        use temp = new TempDirectory()
        let fixture = newFixture (Path.Combine(temp.Path, "report-drift"))

        writeText (Path.Combine(fixture.ReceiptsDir, "FunnySharp.Benchmarks.Fixture-report.csv")) "changed"

        let exitCode, _, stderr = runVerifier fixture
        assertFailsWith "hash does not match" exitCode stderr

    [<Fact>]
    member _.VerifyPerformance_BenchmarkInputFingerprintDrift_Rejects() =
        use temp = new TempDirectory()
        let fixture = newFixture (Path.Combine(temp.Path, "input-drift"))
        writeText (Path.Combine(fixture.Root, "a-input.txt")) "changed"
        let exitCode, _, stderr = runVerifier fixture
        assertFailsWith "does not match the current benchmark input" exitCode stderr

    [<Fact>]
    member _.VerifyPerformance_PolicyMutationAfterMeasurement_Rejects() =
        use temp = new TempDirectory()
        let fixture = newFixture (Path.Combine(temp.Path, "policy-mutation"))

        let mutated =
            { fixture with
                PolicyRows =
                    [ fixture.PolicyRows.[0]
                      { fixture.PolicyRows.[1] with AllocationBudgetBytes = Some 32 }
                      fixture.PolicyRows.[2] ] }

        writeManifest mutated
        let exitCode, _, stderr = runVerifier mutated
        assertFailsWith "was not measured under the current policy" exitCode stderr

    [<Fact>]
    member _.GeneratePerformanceDocumentation_GeneratesThenVerifies_UnderInvariantCulture() =
        use temp = new TempDirectory()
        let fixture, guidePath = newDocumentationFixture (Path.Combine(temp.Path, "documentation"))
        let originalCulture = CultureInfo.CurrentCulture

        try
            CultureInfo.CurrentCulture <- CultureInfo.GetCultureInfo("fr-FR")

            let generateExit, generateStdout, generateStderr = runGenerator fixture false
            assertPassed generateExit generateStderr
            Assert.Contains("Generated 1 performance documentation regions.", generateStdout)

            let verifyExit, verifyStdout, verifyStderr = runGenerator fixture true
            assertPassed verifyExit verifyStderr
            Assert.Contains("Verified 1 performance documentation regions.", verifyStdout)

            let generated = File.ReadAllText guidePath

            let candidateRows =
                generated.Split('\n')
                |> Array.filter (fun line ->
                    line.TrimEnd('\r').StartsWith("| Category", StringComparison.Ordinal))

            Assert.Equal(2, candidateRows.Length)
            Assert.Contains("2.500 ns", generated)
        finally
            CultureInfo.CurrentCulture <- originalCulture

    [<Fact>]
    member _.GeneratePerformanceDocumentation_Verify_RejectsFingerprintDrift() =
        use temp = new TempDirectory()
        let fixture, _ = newDocumentationFixture (Path.Combine(temp.Path, "input-drift"))
        writeText (Path.Combine(fixture.Root, "a-input.txt")) "changed"
        let exitCode, _, stderr = runGenerator fixture true
        assertFailsWith "policy, input, or protocol" exitCode stderr

    [<Fact>]
    member _.GeneratePerformanceDocumentation_Verify_RejectsObservationRowCountMismatch() =
        use temp = new TempDirectory()
        let fixture, _ = newDocumentationFixture (Path.Combine(temp.Path, "observation-rows"))

        match fixture.Observation with
        | Some observation ->
            let mutated =
                { fixture with
                    Observation = Some { observation with Rows = observation.Rows |> List.take 2 } }

            writeManifest mutated
            let exitCode, _, stderr = runGenerator mutated true
            assertFailsWith "row count" exitCode stderr
        | None -> failwith "documentation fixture did not carry an observation"

    [<Fact>]
    member _.GeneratePerformanceDocumentation_Verify_DetectsManualEdit() =
        use temp = new TempDirectory()
        let fixture, guidePath = newDocumentationFixture (Path.Combine(temp.Path, "manual-drift"))
        let generateExit, _, generateStderr = runGenerator fixture false
        assertPassed generateExit generateStderr

        writeText guidePath ((File.ReadAllText guidePath).Replace("2.500 ns", "manual edit"))

        let exitCode, _, stderr = runGenerator fixture true
        assertFailsWith "stale" exitCode stderr
