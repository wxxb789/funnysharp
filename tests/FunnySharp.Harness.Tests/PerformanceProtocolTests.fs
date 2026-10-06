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
open System.Text.Json.Nodes
open System.Reflection
open System.Reflection.Metadata
open System.Reflection.Metadata.Ecma335
open System.Reflection.PortableExecutable
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

let private parseObject (text: string) : JsonObject =
    match JsonNode.Parse(text) with
    | null -> failwith "Expected a JSON object."
    | node -> node.AsObject()

let private requiredNode (node: JsonNode | null) : JsonNode =
    match node with
    | null -> failwith "Missing trusted fixture node."
    | value -> value

let private nodeField (node: JsonNode) (name: string) : JsonNode = requiredNode node[name]
let private nodeItem (node: JsonNode) (index: int) : JsonNode = requiredNode node[index]

let private binaryJson (path: string) : string =
    use stream = File.OpenRead path
    use pe = new PEReader(stream)
    let reader = pe.GetMetadataReader()
    let mvid = reader.GetGuid(reader.GetModuleDefinition().Mvid).ToString()
    jsonObjectText
        [ "file", jsonString (Path.GetFullPath path)
          "sha256", jsonString (fileSha256 path)
          "mvid", jsonString mvid
          "identity", jsonString (AssemblyName.GetAssemblyName(path).FullName) ]

let private boundFixture (root: string) : Fixture =
    let initial = newFixture root
    let sourceDir = Path.Combine(root, "src", "Fixture")
    let benchmarkDir = Path.Combine(root, "benchmarks", "Fixture")
    Directory.CreateDirectory sourceDir |> ignore
    Directory.CreateDirectory benchmarkDir |> ignore

    let extraFiles =
        [ "Directory.Build.props", "<Project />"
          "README.md", "fixture package readme"
          "global.json", "{}"
          "src/Fixture/UnitResult.cs", "fixture source"
          "src/Fixture/Fixture.csproj", "<Project />"
          "src/Fixture/packages.lock.json", "{}"
          "benchmarks/Fixture/Fixture.csproj", "<Project />"
          "benchmarks/Fixture/Fixture.cs", "fixture benchmark"
          "benchmarks/Fixture/packages.lock.json", "{}" ]

    for relative, text in extraFiles do
        writeText (Path.Combine(root, relative)) text

    let files =
        [ "a-input.txt"; "Z-input.txt" ]
        @ (extraFiles |> List.map fst)
        |> List.sortWith (fun left right -> String.CompareOrdinal(left, right))

    let manifest = parseObject (File.ReadAllText initial.ManifestPath)
    (nodeField manifest "benchmarkInput")["files"] <-
        JsonNode.Parse(jsonArrayText (files |> List.map jsonString))

    manifest["runBinding"] <-
        JsonNode.Parse(
            jsonObjectText
                [ "baseCommit", jsonString (String.replicate 40 "a")
                  "baseTree", jsonString (String.replicate 40 "b")
                  "benchmarkProject", jsonString "benchmarks/Fixture/Fixture.csproj" ])

    writeJson initial.ManifestPath (manifest.ToJsonString())
    let inputFingerprint = fileSetFingerprint root files
    let protocolFingerprint = initial.ProtocolFingerprint
    let policyFingerprintValue = policyFingerprint initial.ManifestPath
    let snapshot =
        "snapshot:"
        + textSha256 (
            String.concat "\000"
                [ String.replicate 40 "a"
                  String.replicate 40 "b"
                  policyFingerprintValue
                  inputFingerprint
                  protocolFingerprint ])

    let fixture =
        { initial with
            CandidateCommit = snapshot
            InputFingerprint = inputFingerprint
            PolicyFingerprint = policyFingerprintValue }

    writeReceipt fixture

    // Real, distinct managed PE files preserve the hash/MVID checks under test.
    let benchmarkDll = Path.Combine(fixture.ReceiptsDir, "fixture-benchmark.dll")
    let workloadDll = Path.Combine(fixture.ReceiptsDir, "fixture-workload.dll")
    File.Copy(typeof<FunnySharp.Harness.Output.HarnessError>.Assembly.Location, benchmarkDll)
    File.Copy(typeof<JsonDocument>.Assembly.Location, workloadDll)

    let runId = "11111111111111111111111111111111"
    let preflightPath = Path.Combine(fixture.ReceiptsDir, runId + "-preflight.json")
    let rows =
        fixture.PolicyRows
        |> List.filter (fun row -> row.Included)
        |> List.map (fun row ->
            let node = parseObject row.Json
            node["outcomeSha256"] <- JsonValue.Create(textSha256 "42")
            node.ToJsonString())

    let preflight =
        jsonObjectText
            [ "schemaVersion", "1"
              "runId", jsonString runId
              "completedAtUtc", jsonString "2026-09-24T00:00:00Z"
              "candidateSnapshot", jsonString snapshot
              "baseCommit", jsonString (String.replicate 40 "a")
              "baseTree", jsonString (String.replicate 40 "b")
              "policyFingerprint", jsonString policyFingerprintValue
              "benchmarkInputFingerprint", jsonString inputFingerprint
              "protocolFingerprint", jsonString protocolFingerprint
              "runtime", jsonString fixture.Environment.Runtime
              "rows", jsonArrayText rows
              "semanticCases", jsonArrayText [ jsonString "Fixture|" ]
              "excludedIds", jsonArrayText [ jsonString "excluded|fixture" ]
              "assemblies", jsonArrayText [ binaryJson benchmarkDll ] ]

    writeJson preflightPath preflight
    let child =
        jsonObjectText
            [ "runId", jsonString runId
              "candidateSnapshot", jsonString snapshot
              "capturedAtUtc", jsonString "2026-09-24T01:00:00Z"
              "processId", "1234"
              "runtime", jsonString fixture.Environment.Runtime
              "benchmarkClass", jsonString "Fixture"
              "parameters", jsonString ""
              "workload", binaryJson workloadDll
              "assemblies", jsonArrayText [ binaryJson benchmarkDll ] ]

    let marker =
        "// funnysharp-workload:"
        + Convert.ToBase64String(Encoding.UTF8.GetBytes child)

    let launches =
        fixture.ReceiptRows
        |> List.mapi (fun index row ->
            let name = sprintf "fixture-launch-%d.log" index
            let path = Path.Combine(fixture.ReceiptsDir, name)
            writeText path marker
            jsonObjectText
                [ "rowId", jsonString row.Id
                  "file", jsonString name
                  "sha256", jsonString (fileSha256 path) ])

    let receipt = parseObject (File.ReadAllText fixture.ReceiptPath)
    receipt["binding"] <-
        JsonNode.Parse(
            jsonObjectText
                [ "preflight",
                  jsonObjectText
                      [ "file", jsonString (match Path.GetFileName preflightPath with null -> failwith "Missing preflight filename." | name -> name)
                        "sha256", jsonString (fileSha256 preflightPath) ]
                  "launches", jsonArrayText launches ])
    writeJson fixture.ReceiptPath (receipt.ToJsonString())
    fixture

let private approveRecording (fixture: Fixture) : unit =
    let manifest = parseObject (File.ReadAllText fixture.ManifestPath)
    let protocol = nodeField manifest "protocol"
    let files =
        (nodeField protocol "files").AsArray()
        |> Seq.map (fun node ->
            let path = (requiredNode node).GetValue<string>()
            jsonObjectText [ "path", jsonString path; "sha256", jsonString (fileSha256 (Path.Combine(fixture.Root, path))) ])
        |> Seq.toList
    protocol["recordingIdentity"] <- JsonNode.Parse(
        jsonObjectText
            [ "schemaVersion", "1"
              "receiverFiles", jsonArrayText [ jsonString "a-protocol.txt" ]
              "files", jsonArrayText files
              "provenance", jsonObjectText [ "path", jsonString "recording/catalog.json"; "sha256", jsonString (textSha256 "fixture catalog") ] ])
    let observation = parseObject (File.ReadAllText fixture.ReceiptPath)
    observation["receipts"] <- JsonNode.Parse(
        jsonArrayText [ jsonObjectText [ "file", jsonString "Fixture-performance-receipt.json"; "sha256", jsonString (fileSha256 fixture.ReceiptPath) ] ])
    manifest["observation"] <- observation
    writeJson fixture.ManifestPath (manifest.ToJsonString())

let private mutateBoundPreflight (fixture: Fixture) (mutate: JsonObject -> unit) : unit =
    let receipt = parseObject (File.ReadAllText fixture.ReceiptPath)
    let evidence = (nodeField (nodeField receipt "binding") "preflight")
    let path = Path.Combine(fixture.ReceiptsDir, (nodeField evidence "file").GetValue<string>())
    let preflight = parseObject (File.ReadAllText path)
    mutate preflight
    writeJson path (preflight.ToJsonString())
    evidence["sha256"] <- JsonValue.Create(fileSha256 path)
    writeJson fixture.ReceiptPath (receipt.ToJsonString())

let private mutateBoundLaunch (fixture: Fixture) (mutate: JsonObject -> unit) : unit =
    let receipt = parseObject (File.ReadAllText fixture.ReceiptPath)
    let evidence = (nodeItem (nodeField (nodeField receipt "binding") "launches") 0)
    let path = Path.Combine(fixture.ReceiptsDir, (nodeField evidence "file").GetValue<string>())
    let marker = File.ReadAllText path
    let payload = marker.Substring("// funnysharp-workload:".Length)
    let child = parseObject (Encoding.UTF8.GetString(Convert.FromBase64String payload))
    mutate child
    writeText path (
        "// funnysharp-workload:"
        + Convert.ToBase64String(Encoding.UTF8.GetBytes(child.ToJsonString())))
    evidence["sha256"] <- JsonValue.Create(fileSha256 path)
    writeJson fixture.ReceiptPath (receipt.ToJsonString())


// Coverage fixtures preserve legacy receipt semantics while independently binding
// a small exact API surface, source declarations and mandatory resource witnesses.
let private coverageFixture (root: string) : Fixture =
    let fixture = newFixture root
    let baselineDir = Path.Combine(root, "eng", "api-baseline")
    let coverageDir = Path.Combine(root, "eng", "performance", "coverage")
    Directory.CreateDirectory baselineDir |> ignore
    Directory.CreateDirectory coverageDir |> ignore
    let core = "ASSEMBLY FunnySharp, Culture=neutral, PublicKeyToken=null\nCLASS FunnySharp.Fixture\n  METHOD static System.Int32 Value()\nCLASS FunnySharp.Location\n  METHOD static FunnySharp.Location Root()\n"
    let http = "ASSEMBLY FunnySharp.AspNetCore, Culture=neutral, PublicKeyToken=null\nCLASS FunnySharp.AspNetCore.HttpResultExtensions\n  METHOD static System.Int32 Value()\n"
    let corePath = "eng/api-baseline/FunnySharp.public-api.txt"
    let httpPath = "eng/api-baseline/FunnySharp.AspNetCore.public-api.txt"
    writeText (Path.Combine(root, corePath)) core
    writeText (Path.Combine(root, httpPath)) http
    let sourcePath = "coverage-source.txt"
    writeText (Path.Combine(root, sourcePath)) "public static int Value() => 42;\n[Experimental(\"FS0017\")]\npublic sealed class Location { public static Location Root() => new(); }\n"
    let witnessPath = "coverage-witness.txt"
    writeText (Path.Combine(root, witnessPath)) "LongInputBound\nCancelAndDrain\nLinearFirstSuccess\n"
    let boundFile path =
        jsonObjectText [ "path", jsonString path; "sha256", jsonString (fileSha256 (Path.Combine(root, path))) ]
    let source first last =
        jsonObjectText [ "path", jsonString sourcePath; "firstLine", string first; "lastLine", string last ]
    let row identity assembly declaring memberLine stability first last =
        jsonObjectText
            [ "identity", jsonString identity
              "assembly", jsonString assembly
              "declaringType", jsonString declaring
              "member", jsonString memberLine
              "stability", jsonString stability
              "source", source first last
              "allocation", jsonString "Immediate scalar return; no managed storage."
              "cost", jsonString "One constant scalar return."
              "boxing", jsonString "No conversion to object or interface."
              "resources", jsonString "No resource acquisition, admission, disposal or awaitable consumption."
              "benchmarkRows", if stability = "stable" then jsonArrayText [ jsonString fixture.PolicyRows[0].Id ] else "[]"
              "exclusionReason", if stability = "experimental" then jsonString "Experimental FS0017 Location declaration; no stable promise." else "null" ]
    let rows =
        [ row "C:3" (core.Split('\n')[0]) "CLASS FunnySharp.Fixture" "  METHOD static System.Int32 Value()" "stable" 1 1
          row "C:5" (core.Split('\n')[0]) "CLASS FunnySharp.Location" "  METHOD static FunnySharp.Location Root()" "experimental" 2 3
          row "H:3" (http.Split('\n')[0]) "CLASS FunnySharp.AspNetCore.HttpResultExtensions" "  METHOD static System.Int32 Value()" "stable" 1 1 ]
    // This small census is deliberately synthetic, independent of the coverage
    // stability flags. The actual checkout requires both real package DLLs.
    let metadata declaring memberText token experimental =
        jsonObjectText
            [ "declaringType", jsonString declaring; "memberKind", jsonString "Method"
              "member", jsonString memberText; "metadataToken", string token
              "experimentalDiagnostic", if experimental then jsonString "FS0017" else "null" ]
    let coreMetadata =
        [ metadata "FunnySharp.Fixture" "Int32 Value()" 100663297 false
          metadata "FunnySharp.Location" "FunnySharp.Location Root()" 100663298 true ]
    let httpMetadata = [ metadata "FunnySharp.AspNetCore.HttpResultExtensions" "Int32 Value()" 100663297 false ]
    let censusPath = "coverage-census.json"
    let assembly name baseline members =
        jsonObjectText
            [ "assembly", jsonString name; "baselineSha256", jsonString (fileSha256 (Path.Combine(root, baseline)))
              "metadata", jsonArrayText members ]
    writeJson (Path.Combine(root, censusPath)) (
        jsonObjectText
            [ "assemblies", jsonArrayText
                  [ assembly "FunnySharp" corePath coreMetadata
                    assembly "FunnySharp.AspNetCore" httpPath httpMetadata ] ])
    let rows =
        List.zip rows (coreMetadata @ httpMetadata)
        |> List.map (fun (row, memberMetadata) ->
            let node = parseObject row
            node["metadata"] <- JsonNode.Parse memberMetadata
            node.ToJsonString())
    let witness kind name line =
        jsonObjectText
            [ "kind", jsonString kind; "path", jsonString witnessPath
              "sha256", jsonString (fileSha256 (Path.Combine(root, witnessPath)))
              "member", jsonString name; "firstLine", string line; "lastLine", string line ]
    let coverage =
        jsonObjectText
            [ "schema", jsonString "funnysharp-stable-performance-coverage/v1"
              "baselines", jsonObjectText [ "C", boundFile corePath; "H", boundFile httpPath ]
              "metadataCensus", boundFile censusPath
              "sourceFiles", jsonArrayText [ boundFile sourcePath ]
              "benchmarkManifests", jsonArrayText [ jsonObjectText [ "path", jsonString (Path.GetRelativePath(root, fixture.ManifestPath)); "policySha256", jsonString (policyFingerprint fixture.ManifestPath) ] ]
              "members", jsonArrayText rows
              "mandatoryWitnesses", jsonArrayText [ witness "long-input-bound" "LongInputBound" 1; witness "cancel-drain" "CancelAndDrain" 2; witness "linear-first-success" "LinearFirstSuccess" 3 ] ]
    writeJson (Path.Combine(coverageDir, "stable-members.json")) coverage
    fixture

let private coveragePath (fixture: Fixture) =
    Path.Combine(fixture.Root, "eng", "performance", "coverage", "stable-members.json")

// These tiny PE files exercise the real hash/MVID/assembly-name checks. The
// census remains a synthetic protocol fixture, not a claim about shipped APIs.
let private writeFixtureAssembly (path: string) (name: string) (mvid: Guid) =
    let metadata = MetadataBuilder()
    metadata.AddModule(0, metadata.GetOrAddString(name + ".dll"), metadata.GetOrAddGuid mvid, Unchecked.defaultof<GuidHandle>, Unchecked.defaultof<GuidHandle>) |> ignore
    metadata.AddAssembly(metadata.GetOrAddString name, Version(1, 0, 0, 0), Unchecked.defaultof<StringHandle>, Unchecked.defaultof<BlobHandle>, enum<AssemblyFlags> 0, AssemblyHashAlgorithm.None) |> ignore
    metadata.AddTypeDefinition(TypeAttributes.NotPublic, Unchecked.defaultof<StringHandle>, metadata.GetOrAddString "<Module>", Unchecked.defaultof<EntityHandle>, MetadataTokens.FieldDefinitionHandle 1, MetadataTokens.MethodDefinitionHandle 1) |> ignore
    let pe = ManagedPEBuilder(PEHeaderBuilder(), MetadataRootBuilder metadata, BlobBuilder())
    let bytes = BlobBuilder()
    pe.Serialize bytes |> ignore
    File.WriteAllBytes(path, bytes.ToArray())

let private canonicalCoverageFixture (root: string) : Fixture =
    coverageFixture root |> ignore
    let fixture = boundFixture root
    Directory.CreateDirectory(Path.Combine(root, "src", "FunnySharp")) |> ignore
    let coverage = parseObject (File.ReadAllText(coveragePath fixture))
    let bound relative =
        parseObject (jsonObjectText [ "path", jsonString relative; "sha256", jsonString (fileSha256 (Path.Combine(root, relative))) ])
    let binaries =
        [ for name in [ "FunnySharp"; "FunnySharp.AspNetCore" ] do
              let relative = "receipts/" + name + ".dll"
              writeFixtureAssembly (Path.Combine(root, relative)) name (Guid "11111111-1111-1111-1111-111111111111")
              yield name, relative ]
    let template = nodeItem (nodeField coverage "members") 0
    let rows = JsonArray()
    let assemblies = JsonArray()
    for name, relative in binaries do
        let core = name = "FunnySharp"
        let count = if core then 498 else 1
        let declaring = if core then "FunnySharp.Fixture" else "FunnySharp.AspNetCore.HttpResultExtensions"
        let header = "ASSEMBLY " + name + ", Culture=neutral, PublicKeyToken=null"
        let methods = [ for i in 0 .. count - 1 -> if core && i = 0 then "BeginInvoke" else "M" + string i ]
        let baseline = "eng/api-baseline/" + name + ".public-api.txt"
        writeText (Path.Combine(root, baseline)) (header + "\nCLASS " + declaring + "\n" + (methods |> List.map (fun method -> "  METHOD static System.Int32 " + method + "()\n") |> String.concat ""))
        (nodeField coverage "baselines")[if core then "C" else "H"] <- bound baseline
        let members = JsonArray()
        for i, method in List.indexed methods do
            let experimental = core && i >= 467
            let item = parseObject (jsonObjectText
                [ "declaringType", jsonString declaring; "memberKind", jsonString "Method"
                  "member", jsonString ("Int32 " + method + "()"); "metadataToken", string (100663297 + i)
                  "experimentalDiagnostic", if experimental then jsonString "FS0017" else "null" ])
            members.Add(item.DeepClone())
            let row = template.DeepClone()
            row["identity"] <- JsonValue.Create((if core then "C:" else "H:") + string (i + 3))
            row["assembly"] <- JsonValue.Create header
            row["declaringType"] <- JsonValue.Create("CLASS " + declaring)
            row["member"] <- JsonValue.Create("  METHOD static System.Int32 " + method + "()")
            row["metadata"] <- item
            row["stability"] <- JsonValue.Create(if experimental then "experimental" else "stable")
            if experimental then
                row["benchmarkRows"] <- JsonArray()
                row["exclusionReason"] <- JsonValue.Create("Experimental fixture member.")
            if method = "BeginInvoke" then
                let proofPath = "apm-proof.json"
                writeJson (Path.Combine(root, proofPath)) "{\"results\":{\"tests\":[{\"status\":\"passed\",\"extra\":{\"method\":\"BeginInvoke\"}}]}}"
                let proof = bound proofPath
                proof["source"] <- bound "coverage-source.txt"
                proof["coreDll"] <- bound relative
                proof["member"] <- JsonValue.Create method
                row["runtimeWitness"] <- proof
            rows.Add row
        let assembly = parseObject (binaryJson (Path.Combine(root, relative)))
        assembly["assembly"] <- JsonValue.Create name
        assembly["baselineSha256"] <- JsonValue.Create(fileSha256 (Path.Combine(root, baseline)))
        assembly["metadata"] <- members
        assemblies.Add assembly
    coverage["members"] <- rows
    let censusPath = "coverage-census.json"
    let census = JsonObject()
    census["assemblies"] <- assemblies
    writeJson (Path.Combine(root, censusPath)) (census.ToJsonString())
    coverage["metadataCensus"] <- bound censusPath
    let manifests = JsonArray()
    for relative in [ "eng/performance/baseline.json"; "eng/performance/competitor-baseline.json" ] do
        writeText (Path.Combine(root, relative)) (File.ReadAllText fixture.ManifestPath)
        manifests.Add(parseObject (jsonObjectText [ "path", jsonString relative; "policySha256", jsonString (policyFingerprint fixture.ManifestPath) ]))
    coverage["benchmarkManifests"] <- manifests
    let witnesses =
        [ "long-input-bound", "ExplicitFirstSuccessBoundLimits1024HeldCandidatesAndRefillsExactlyOneSlot"
          "long-input-bound", "SelectParallelCompletionOrderValueAsyncBoundsUndeliveredWorkAndKeepsStreaming"
          "cancel-drain", "FirstSuccessAwaitsRealUsingAsyncDisposalAndPropagatesItsFailureAfterAWinner"
          "cancel-drain", "ExplicitFirstSuccessBoundStopsAdmissionAndDrainsOnTimeoutOrCallerCancellation"
          "cancel-drain", "ExternalCancellationDoesNotPublishANonCooperatingSelectorAndCleansUpOnce"
          "linear-first-success", "ExplicitFirstSuccessHandlesLargeSynchronousFailuresInOrderWithOneValueTaskConsumption"
          "linear-first-success", "ExplicitFirstSuccessAccountsForLargeStaggeredBatchesWithoutReorderingTypedFailures" ]
    writeText (Path.Combine(root, "coverage-witness.txt")) (witnesses |> List.map snd |> String.concat "\n")
    writeText (Path.Combine(root, "resource.trx")) ("<TestRun><Results>" + (witnesses |> List.map (fun (_, name) -> "<UnitTestResult outcome=\"Passed\" testName=\"" + name + "\" />") |> String.concat "") + "</Results></TestRun>")
    let mandatory = JsonArray()
    for i, (kind, name) in List.indexed witnesses do
        let witness = bound "coverage-witness.txt"
        witness["kind"] <- JsonValue.Create kind
        witness["member"] <- JsonValue.Create name
        witness["firstLine"] <- JsonValue.Create(i + 1)
        witness["lastLine"] <- JsonValue.Create(i + 1)
        let execution = bound "resource.trx"
        execution["coreDll"] <- bound "receipts/FunnySharp.dll"
        witness["execution"] <- execution
        mandatory.Add witness
    coverage["mandatoryWitnesses"] <- mandatory
    writeJson (coveragePath fixture) (coverage.ToJsonString())
    let assemblyNodes () = binaries |> List.map (fun (_, relative) -> parseObject (binaryJson (Path.Combine(root, relative))) :> JsonNode)
    mutateBoundPreflight fixture (fun node -> for binary in assemblyNodes () do (nodeField node "assemblies").AsArray().Add binary)
    let receipt = parseObject (File.ReadAllText fixture.ReceiptPath)
    for launch in (nodeField (nodeField receipt "binding") "launches").AsArray() do
        let launch = requiredNode launch
        let path = Path.Combine(fixture.ReceiptsDir, (nodeField launch "file").GetValue<string>())
        let child = parseObject (Encoding.UTF8.GetString(Convert.FromBase64String((File.ReadAllText path).Substring("// funnysharp-workload:".Length))))
        for binary in assemblyNodes () do (nodeField child "assemblies").AsArray().Add binary
        writeText path ("// funnysharp-workload:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(child.ToJsonString())))
        launch["sha256"] <- JsonValue.Create(fileSha256 path)
    writeJson fixture.ReceiptPath (receipt.ToJsonString())
    fixture


type PerformanceProtocolTests() =

    [<Theory>]
    [<InlineData("version")>]
    [<InlineData("null-metadata")>]
    [<InlineData("missing-hashes")>]
    [<InlineData("partial-hashes")>]
    [<InlineData("duplicate-hash-path")>]
    [<InlineData("wrong-hash")>]
    [<InlineData("malformed-hash")>]
    [<InlineData("duplicate-receiver")>]
    [<InlineData("unknown-receiver")>]
    [<InlineData("input-receiver")>]
    [<InlineData("empty-receivers")>]
    [<InlineData("unsafe-path")>]
    [<InlineData("alias-path")>]
    [<InlineData("unknown-protocol-member")>]
    [<InlineData("duplicate-protocol-member")>]
    [<InlineData("missing-provenance")>]
    [<InlineData("bad-provenance-hash")>]
    [<InlineData("unsafe-provenance")>]
    [<InlineData("duplicate-provenance-field")>]
    [<InlineData("duplicate-metadata-field")>]
    [<InlineData("duplicate-hash-field")>]
    [<InlineData("unknown-metadata-field")>]
    [<InlineData("duplicate-approved-receipt")>]
    [<InlineData("empty-approved-receipts")>]
    [<InlineData("unsafe-approved-receipt")>]
    [<InlineData("bad-approved-hash")>]
    member _.RecordingIdentity_MalformedProjectionRejectsAtBothConsumers(mutation: string) =
        use temp = new TempDirectory()
        let fixture = boundFixture (Path.Combine(temp.Path, mutation))
        approveRecording fixture
        let manifest = parseObject (File.ReadAllText fixture.ManifestPath)
        let protocol = nodeField manifest "protocol"
        let metadata = nodeField protocol "recordingIdentity"
        let hashes = (nodeField metadata "files").AsArray()
        let receivers = (nodeField metadata "receiverFiles").AsArray()
        let provenance = nodeField metadata "provenance"
        let receipts = (nodeField (nodeField manifest "observation") "receipts").AsArray()
        match mutation with
        | "version" -> metadata["schemaVersion"] <- JsonValue.Create(2)
        | "null-metadata" -> protocol["recordingIdentity"] <- null
        | "missing-hashes" -> metadata.AsObject().Remove("files") |> ignore
        | "partial-hashes" -> hashes.RemoveAt(1)
        | "duplicate-hash-path" -> hashes.Add((nodeItem hashes 0).DeepClone())
        | "wrong-hash" -> (nodeItem hashes 0)["sha256"] <- JsonValue.Create(String.replicate 64 "0")
        | "malformed-hash" -> (nodeItem hashes 0)["sha256"] <- JsonValue.Create("not-a-hash")
        | "duplicate-receiver" -> receivers.Add(JsonValue.Create("a-protocol.txt"))
        | "unknown-receiver" -> receivers.Add(JsonValue.Create("unknown.txt"))
        | "input-receiver" -> (nodeField (nodeField manifest "benchmarkInput") "files").AsArray().Add(JsonValue.Create("a-protocol.txt"))
        | "empty-receivers" -> receivers.Clear()
        | "unsafe-path" -> (nodeItem hashes 0)["path"] <- JsonValue.Create("../a-protocol.txt")
        | "alias-path" -> (nodeItem hashes 0)["path"] <- JsonValue.Create("./a-protocol.txt")
        | "unknown-protocol-member" ->
            writeText (Path.Combine(fixture.Root, "new-protocol.txt")) "calibration"
            (nodeField protocol "files").AsArray().Add(JsonValue.Create("new-protocol.txt"))
        | "duplicate-protocol-member" -> (nodeField protocol "files").AsArray().Add(JsonValue.Create("a-protocol.txt"))
        | "missing-provenance" -> metadata.AsObject().Remove("provenance") |> ignore
        | "bad-provenance-hash" -> provenance["sha256"] <- JsonValue.Create("bad")
        | "unsafe-provenance" -> provenance["path"] <- JsonValue.Create("../catalog.json")
        | "unknown-metadata-field" -> metadata["allowHistorical"] <- JsonValue.Create(true)
        | "duplicate-approved-receipt" -> receipts.Add((nodeItem receipts 0).DeepClone())
        | "empty-approved-receipts" -> receipts.Clear()
        | "unsafe-approved-receipt" -> (nodeItem receipts 0)["file"] <- JsonValue.Create("../Fixture-performance-receipt.json")
        | "bad-approved-hash" -> (nodeItem receipts 0)["sha256"] <- JsonValue.Create("bad")
        | "duplicate-provenance-field" | "duplicate-metadata-field" | "duplicate-hash-field" -> ()
        | _ -> failwith "Unknown recording metadata mutation."
        let text = manifest.ToJsonString()
        let text =
            match mutation with
            | "duplicate-provenance-field" -> text.Replace("\"provenance\":{", "\"provenance\":{\"path\":\"other/catalog.json\",")
            | "duplicate-metadata-field" -> text.Replace("\"recordingIdentity\":{", "\"recordingIdentity\":{\"schemaVersion\":1,")
            | "duplicate-hash-field" -> text.Replace("\"path\":\"a-protocol.txt\",", "\"path\":\"a-protocol.txt\",\"sha256\":\"" + String.replicate 64 "0" + "\",")
            | _ -> text
        writeJson fixture.ManifestPath text
        writeText (Path.Combine(fixture.Root, "a-protocol.txt")) "receiver edit"
        let exitCode, _, stderr = runVerifier fixture
        Assert.Equal(1, exitCode)
        Assert.Contains("recording identity", stderr)
        let docsExit, _, docsErr = runGenerator fixture true
        Assert.Equal(1, docsExit)
        Assert.Contains("recording identity", docsErr)

    [<Theory>]
    [<InlineData("producer")>]
    [<InlineData("input")>]
    [<InlineData("policy")>]
    [<InlineData("revision")>]
    [<InlineData("snapshot")>]
    [<InlineData("new-source")>]
    [<InlineData("new-import")>]
    [<InlineData("missing-receiver")>]
    member _.RecordingIdentity_CurrentMeasurementAndSnapshotDriftRejects(mutation: string) =
        use temp = new TempDirectory()
        let fixture = boundFixture (Path.Combine(temp.Path, mutation))
        approveRecording fixture
        if mutation <> "snapshot" then
            writeText (Path.Combine(fixture.Root, "a-protocol.txt")) "receiver edit"
        let manifest = parseObject (File.ReadAllText fixture.ManifestPath)
        match mutation with
        | "producer" -> writeText (Path.Combine(fixture.Root, "Z-protocol.txt")) "changed calibration outside input list"
        | "input" -> writeText (Path.Combine(fixture.Root, "src/Fixture/UnitResult.cs")) "changed producer input"
        | "policy" -> (nodeItem (nodeField (nodeField manifest "policy") "rows") 1)["allocationBudgetBytes"] <- JsonValue.Create(32)
        | "revision" -> (nodeField manifest "policy")["revision"] <- JsonValue.Create("next")
        | "snapshot" -> (nodeField manifest "observation")["candidateCommit"] <- JsonValue.Create("snapshot:" + String.replicate 64 "0")
        | "new-source" -> writeText (Path.Combine(fixture.Root, "src/Fixture/New.cs")) "new input"
        | "new-import" ->
            Directory.CreateDirectory(Path.Combine(fixture.Root, "imports")) |> ignore
            writeText (Path.Combine(fixture.Root, "imports/New.props")) "<Project />"
            writeText (Path.Combine(fixture.Root, "Directory.Build.props")) "<Project><Import Project=\"imports/New.props\" /></Project>"
        | "missing-receiver" -> File.Delete(Path.Combine(fixture.Root, "a-protocol.txt"))
        | _ -> failwith "Unknown measurement mutation."
        writeJson fixture.ManifestPath (manifest.ToJsonString())
        let exitCode, _, _ = runVerifier fixture
        let docsExit, _, _ = runGenerator fixture true
        Assert.Equal(1, exitCode)
        Assert.Equal(1, docsExit)

    [<Theory>]
    [<InlineData("actual-bytes")>]
    [<InlineData("wrong-approved-hash")>]
    [<InlineData("unexpected-name")>]
    [<InlineData("extra-receipt")>]
    member _.RecordingIdentity_HistoricalAdmissionRequiresExactActualReceiptSet(mutation: string) =
        use temp = new TempDirectory()
        let fixture = boundFixture (Path.Combine(temp.Path, mutation))
        approveRecording fixture
        writeText (Path.Combine(fixture.Root, "a-protocol.txt")) "receiver edit"
        match mutation with
        | "actual-bytes" -> File.AppendAllText(fixture.ReceiptPath, " ", utf8NoBom)
        | "wrong-approved-hash" ->
            let manifest = parseObject (File.ReadAllText fixture.ManifestPath)
            (nodeItem (nodeField (nodeField manifest "observation") "receipts") 0)["sha256"] <- JsonValue.Create(String.replicate 64 "0")
            writeJson fixture.ManifestPath (manifest.ToJsonString())
        | "unexpected-name" -> File.Move(fixture.ReceiptPath, Path.Combine(fixture.ReceiptsDir, "Other-performance-receipt.json"))
        | "extra-receipt" -> File.Copy(fixture.ReceiptPath, Path.Combine(fixture.ReceiptsDir, "Other-performance-receipt.json"))
        | _ -> failwith "Unknown receipt mutation."
        let proposal = Path.Combine(fixture.Root, "proposal.json")
        let result = FunnySharp.Harness.Performance.run fixture.Root fixture.ManifestPath fixture.ReceiptsDir (Some proposal)
        Assert.True(Result.isError result)
        Assert.False(File.Exists proposal)

    [<Theory>]
    [<InlineData("preflight")>]
    [<InlineData("child")>]
    [<InlineData("mvid")>]
    [<InlineData("allocation")>]
    [<InlineData("report")>]
    [<InlineData("environment")>]
    member _.RecordingIdentity_ApprovedBytesStillRequireCurrentRuntimeValidation(mutation: string) =
        use temp = new TempDirectory()
        let fixture = boundFixture (Path.Combine(temp.Path, mutation))
        approveRecording fixture
        writeText (Path.Combine(fixture.Root, "a-protocol.txt")) "receiver edit"
        match mutation with
        | "preflight" -> mutateBoundPreflight fixture (fun node -> node["protocolFingerprint"] <- JsonValue.Create(String.replicate 64 "0"))
        | "child" -> mutateBoundLaunch fixture (fun node -> node["candidateSnapshot"] <- JsonValue.Create("snapshot:" + String.replicate 64 "0"))
        | "mvid" -> mutateBoundLaunch fixture (fun node -> (nodeField node "workload")["mvid"] <- JsonValue.Create(Guid.Empty.ToString()))
        | "allocation" | "environment" ->
            let receipt = parseObject (File.ReadAllText fixture.ReceiptPath)
            if mutation = "allocation" then (nodeItem (nodeField receipt "rows") 0)["allocatedBytesPerOperation"] <- JsonValue.Create(1)
            else (nodeField receipt "environment")["runtime"] <- JsonValue.Create("other")
            writeJson fixture.ReceiptPath (receipt.ToJsonString())
        | "report" -> writeText (Path.Combine(fixture.ReceiptsDir, "FunnySharp.Benchmarks.Fixture-report.csv")) "changed"
        | _ -> failwith "Unknown runtime mutation."
        // Deliberately approve the changed receipt to prove that byte admission
        // cannot replace any current nested, row, environment or report check.
        let manifest = parseObject (File.ReadAllText fixture.ManifestPath)
        (nodeItem (nodeField (nodeField manifest "observation") "receipts") 0)["sha256"] <- JsonValue.Create(fileSha256 fixture.ReceiptPath)
        writeJson fixture.ManifestPath (manifest.ToJsonString())
        let exitCode, _, stderr = runVerifier fixture
        Assert.Equal(1, exitCode)
        Assert.Contains(
            (match mutation with
             | "preflight" -> "Preflight does not match"
             | "child" -> "Measured child has the wrong"
             | "mvid" -> "binary metadata does not match"
             | "allocation" -> "above its 0 B budget"
             | "report" -> "hash does not match"
             | _ -> "invalid environment key"), stderr)

    [<Fact>]
    member _.RecordingIdentity_CurrentCoverageIsStillRequiredAfterAdmission() =
        use temp = new TempDirectory()
        let fixture = canonicalCoverageFixture (Path.Combine(temp.Path, "coverage-recording"))
        approveRecording fixture
        writeText (Path.Combine(fixture.Root, "a-protocol.txt")) "receiver edit"
        let exitCode, _, stderr = runVerifier fixture
        assertPassed exitCode stderr
        File.AppendAllText(Path.Combine(fixture.Root, "coverage-source.txt"), "changed")
        let changedExit, _, changedErr = runVerifier fixture
        Assert.Equal(1, changedExit)
        Assert.Contains("Stable performance coverage", changedErr)

    [<Fact>]
    member _.RecordingIdentity_DocsDoesNotLoadRuntimeOrProvenanceEvidence() =
        use temp = new TempDirectory()
        let fixture = boundFixture (Path.Combine(temp.Path, "docs-recording"))
        approveRecording fixture
        writeText (Path.Combine(fixture.Root, "a-protocol.txt")) "receiver edit"
        Directory.Delete(fixture.ReceiptsDir, true)
        let exitCode, _, stderr = runGenerator fixture true
        assertPassed exitCode stderr

    [<Theory>]
    [<InlineData("receiver")>]
    [<InlineData("producer")>]
    [<InlineData("policy")>]
    member _.RecordingIdentity_UnapprovedEvidenceUsesOnlyStrictCurrentIdentity(change: string) =
        use temp = new TempDirectory()
        let fixture = boundFixture (Path.Combine(temp.Path, "new-" + change))
        approveRecording fixture
        writeText (Path.Combine(fixture.Root, "a-protocol.txt")) "current receiver"
        if change = "producer" then writeText (Path.Combine(fixture.Root, "src/Fixture/UnitResult.cs")) "current producer"
        if change = "policy" then
            let manifest = parseObject (File.ReadAllText fixture.ManifestPath)
            (nodeField manifest "policy")["revision"] <- JsonValue.Create("fixture-v2")
            writeJson fixture.ManifestPath (manifest.ToJsonString())
        let manifest = parseObject (File.ReadAllText fixture.ManifestPath)
        let fileNames name =
            (nodeField (nodeField manifest name) "files").AsArray()
            |> Seq.map (fun node -> (requiredNode node).GetValue<string>()) |> Seq.toList
        let input = fileSetFingerprint fixture.Root (fileNames "benchmarkInput")
        let protocol = fileSetFingerprint fixture.Root (fileNames "protocol")
        let policy = policyFingerprint fixture.ManifestPath
        let snapshot = "snapshot:" + textSha256 (String.concat "\000" [ String.replicate 40 "a"; String.replicate 40 "b"; policy; input; protocol ])
        mutateBoundPreflight fixture (fun node ->
            node["candidateSnapshot"] <- JsonValue.Create snapshot
            node["policyFingerprint"] <- JsonValue.Create policy
            node["benchmarkInputFingerprint"] <- JsonValue.Create input
            node["protocolFingerprint"] <- JsonValue.Create protocol)
        let receipt = parseObject (File.ReadAllText fixture.ReceiptPath)
        receipt["candidateCommit"] <- JsonValue.Create snapshot
        receipt["policyRevision"] <- (nodeField (nodeField manifest "policy") "revision").DeepClone()
        receipt["policyFingerprint"] <- JsonValue.Create policy
        receipt["benchmarkInputFingerprint"] <- JsonValue.Create input
        receipt["protocolFingerprint"] <- JsonValue.Create protocol
        for launch in (nodeField (nodeField receipt "binding") "launches").AsArray() do
            let launch = requiredNode launch
            let path = Path.Combine(fixture.ReceiptsDir, (nodeField launch "file").GetValue<string>())
            let child = parseObject (Encoding.UTF8.GetString(Convert.FromBase64String((File.ReadAllText path).Substring("// funnysharp-workload:".Length))))
            child["candidateSnapshot"] <- JsonValue.Create snapshot
            writeText path ("// funnysharp-workload:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(child.ToJsonString())))
            launch["sha256"] <- JsonValue.Create(fileSha256 path)
        writeJson fixture.ReceiptPath (receipt.ToJsonString())
        let originalManifest = File.ReadAllText fixture.ManifestPath
        let exitCode, _, stderr = runVerifier fixture
        assertPassed exitCode stderr
        Assert.Equal(originalManifest, File.ReadAllText fixture.ManifestPath)
        // The receipt is not the approved byte set: no self-selected old hash
        // can select the historical path, even when all local hashes are updated.
        receipt["protocolFingerprint"] <- JsonValue.Create(fixture.ProtocolFingerprint)
        writeJson fixture.ReceiptPath (receipt.ToJsonString())
        let badExit, _, _ = runVerifier fixture
        Assert.Equal(1, badExit)

    [<Fact>]
    member _.VerifyPerformance_ApprovedRecordingSurvivesReceiverEditsAndPreservesProposal() =
        use temp = new TempDirectory()
        let fixture = boundFixture (Path.Combine(temp.Path, "recording"))
        approveRecording fixture
        let manifestBefore = File.ReadAllText fixture.ManifestPath
        let receiptHash = fileSha256 fixture.ReceiptPath
        for edit in [ "receiver edit"; "further receiver edit" ] do
            writeText (Path.Combine(fixture.Root, "a-protocol.txt")) edit
            let exitCode, _, stderr = runVerifier fixture
            assertPassed exitCode stderr
            let docsExit, _, docsErr = runGenerator fixture true
            assertPassed docsExit docsErr
        let proposal = Path.Combine(fixture.Root, "proposal.json")
        match FunnySharp.Harness.Performance.run fixture.Root fixture.ManifestPath fixture.ReceiptsDir (Some proposal) with
        | Error error -> failwith error.Message
        | Ok _ -> ()
        use document = JsonDocument.Parse(File.ReadAllText proposal)
        Assert.Equal(fixture.ProtocolFingerprint, document.RootElement.GetProperty("protocolFingerprint").GetString())
        Assert.Equal(fixture.CandidateCommit, document.RootElement.GetProperty("candidateCommit").GetString())
        let proposedReceipt = document.RootElement.GetProperty("receipts")[0]
        Assert.Equal(receiptHash, proposedReceipt.GetProperty("sha256").GetString())
        Assert.Equal(manifestBefore, File.ReadAllText fixture.ManifestPath)

    [<Fact>]
    member _.VerifyPerformance_U13_CanonicalCensusAndActualWorkloadJoin_Succeeds() =
        use temp = new TempDirectory()
        let fixture = canonicalCoverageFixture (Path.Combine(temp.Path, "canonical"))
        let exitCode, _, stderr = runVerifier fixture
        assertPassed exitCode stderr

    [<Theory>]
    [<InlineData("missing-core")>]
    [<InlineData("different-core")>]
    [<InlineData("apm-different-core")>]
    [<InlineData("resource-different-core")>]
    [<InlineData("apm-failed")>]
    [<InlineData("resource-failed")>]
    member _.VerifyPerformance_U13_CanonicalJoinAndWitnessesRejectMutation(mutation: string) =
        use temp = new TempDirectory()
        let fixture = canonicalCoverageFixture (Path.Combine(temp.Path, mutation))
        let coverage = parseObject (File.ReadAllText(coveragePath fixture))
        let differentCore () =
            let relative = "receipts/other/FunnySharp.dll"
            Directory.CreateDirectory(Path.Combine(fixture.Root, "receipts", "other")) |> ignore
            writeFixtureAssembly (Path.Combine(fixture.Root, relative)) "FunnySharp" (Guid "22222222-2222-2222-2222-222222222222")
            parseObject (jsonObjectText [ "path", jsonString relative; "sha256", jsonString (fileSha256 (Path.Combine(fixture.Root, relative))) ])
        match mutation with
        | "missing-core" ->
            mutateBoundPreflight fixture (fun node -> (nodeField node "assemblies").AsArray().RemoveAt(1))
        | "different-core" ->
            let alternate = differentCore ()
            let binary = parseObject (binaryJson (Path.Combine(fixture.Root, (nodeField alternate "path").GetValue<string>())))
            mutateBoundPreflight fixture (fun node -> (nodeField node "assemblies")[1] <- binary.DeepClone())
            // Child declarations must agree, so the census join is the failure owner.
            let receipt = parseObject (File.ReadAllText fixture.ReceiptPath)
            for launch in (nodeField (nodeField receipt "binding") "launches").AsArray() do
                let launch = requiredNode launch
                let path = Path.Combine(fixture.ReceiptsDir, (nodeField launch "file").GetValue<string>())
                let child = parseObject (Encoding.UTF8.GetString(Convert.FromBase64String((File.ReadAllText path).Substring("// funnysharp-workload:".Length))))
                (nodeField child "assemblies")[1] <- binary.DeepClone()
                writeText path ("// funnysharp-workload:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(child.ToJsonString())))
                launch["sha256"] <- JsonValue.Create(fileSha256 path)
            writeJson fixture.ReceiptPath (receipt.ToJsonString())
        | "apm-different-core" -> (nodeField (nodeItem (nodeField coverage "members") 0) "runtimeWitness")["coreDll"] <- differentCore ()
        | "resource-different-core" -> (nodeField (nodeItem (nodeField coverage "mandatoryWitnesses") 6) "execution")["coreDll"] <- differentCore ()
        | "apm-failed" ->
            let path = Path.Combine(fixture.Root, "apm-proof.json")
            writeText path ((File.ReadAllText path).Replace("passed", "failed"))
            (nodeField (nodeItem (nodeField coverage "members") 0) "runtimeWitness")["sha256"] <- JsonValue.Create(fileSha256 path)
        | "resource-failed" ->
            let path = Path.Combine(fixture.Root, "resource.trx")
            writeText path ((File.ReadAllText path).Replace("Passed", "Failed"))
            for witness in (nodeField coverage "mandatoryWitnesses").AsArray() do
                (nodeField (requiredNode witness) "execution")["sha256"] <- JsonValue.Create(fileSha256 path)
        | _ -> failwith "Unknown canonical mutation."
        writeJson (coveragePath fixture) (coverage.ToJsonString())
        let exitCode, _, stderr = runVerifier fixture
        Assert.Equal(1, exitCode)
        Assert.Contains(
            (match mutation with
             | "missing-core" -> "actual workload has no core census binary"
             | "different-core" -> "actual workload differs from packaged metadata census"
             | "apm-different-core" -> "APM witness used different package bytes"
             | "resource-different-core" -> "resource witness used different core package bytes"
             | "apm-failed" -> "APM runtime witness did not pass"
             | _ -> "resource test execution is not all passing"), stderr)

    [<Fact>]
    member _.VerifyPerformance_U13_CompleteExactCoverage_Succeeds() =
        use temp = new TempDirectory()
        let fixture = coverageFixture (Path.Combine(temp.Path, "coverage"))
        let exitCode, _, stderr = runVerifier fixture
        assertPassed exitCode stderr

    [<Theory>]
    [<InlineData("missing-map")>]
    [<InlineData("missing-member")>]
    [<InlineData("duplicate-member")>]
    [<InlineData("stale-identity")>]
    [<InlineData("stale-baseline")>]
    [<InlineData("stale-source")>]
    [<InlineData("late-source-range")>]
    [<InlineData("late-witness-range")>]
    [<InlineData("late-witness-hash")>]
    [<InlineData("stale-policy-binding")>]
    [<InlineData("stale-census-baseline")>]
    [<InlineData("missing-census")>]
    [<InlineData("stale-census")>]
    [<InlineData("stale-metadata")>]
    [<InlineData("unknown-row")>]
    [<InlineData("empty-exclusion")>]
    [<InlineData("experimental-as-stable")>]
    [<InlineData("stable-as-experimental")>]
    [<InlineData("missing-allocation")>]
    [<InlineData("missing-cost")>]
    [<InlineData("missing-boxing")>]
    [<InlineData("missing-resources")>]
    [<InlineData("missing-long-input-witness")>]
    [<InlineData("missing-drain-witness")>]
    [<InlineData("missing-linear-witness")>]
    member _.VerifyPerformance_U13_IncompleteOrStaleCoverage_Rejects(mutation: string) =
        use temp = new TempDirectory()
        let fixture = coverageFixture (Path.Combine(temp.Path, mutation))
        let path = coveragePath fixture
        let coverage = parseObject (File.ReadAllText path)
        let members = (nodeField coverage "members").AsArray()
        let stable = nodeItem members 0
        let experimental = nodeItem members 1
        match mutation with
        | "missing-map" -> File.Delete path
        | "missing-member" -> members.RemoveAt(0)
        | "duplicate-member" -> members.Add(stable.DeepClone())
        | "stale-identity" -> stable["member"] <- JsonValue.Create("  METHOD static System.Int64 Value()")
        | "stale-baseline" -> File.AppendAllText(Path.Combine(fixture.Root, "eng", "api-baseline", "FunnySharp.public-api.txt"), "changed")
        | "stale-source" -> File.AppendAllText(Path.Combine(fixture.Root, "coverage-source.txt"), "changed")
        | "late-source-range" -> (nodeField (nodeItem members 2) "source")["lastLine"] <- JsonValue.Create(4)
        | "late-witness-range" -> (nodeItem (nodeField coverage "mandatoryWitnesses") 2)["lastLine"] <- JsonValue.Create(4)
        | "late-witness-hash" -> (nodeItem (nodeField coverage "mandatoryWitnesses") 2)["sha256"] <- JsonValue.Create(String.replicate 64 "0")
        | "stale-policy-binding" -> (nodeItem (nodeField coverage "benchmarkManifests") 0)["policySha256"] <- JsonValue.Create(String.replicate 64 "0")
        | "stale-census-baseline" ->
            let censusPath = Path.Combine(fixture.Root, "coverage-census.json")
            let census = parseObject (File.ReadAllText censusPath)
            (nodeItem (nodeField census "assemblies") 1)["baselineSha256"] <- JsonValue.Create(String.replicate 64 "0")
            writeJson censusPath (census.ToJsonString())
            (nodeField coverage "metadataCensus")["sha256"] <- JsonValue.Create(fileSha256 censusPath)
        | "missing-census" -> File.Delete(Path.Combine(fixture.Root, "coverage-census.json"))
        | "stale-census" -> File.AppendAllText(Path.Combine(fixture.Root, "coverage-census.json"), "changed")
        | "stale-metadata" -> (nodeField stable "metadata")["metadataToken"] <- JsonValue.Create(100663299)
        | "unknown-row" -> stable["benchmarkRows"] <- JsonNode.Parse("[\"unknown-row\"]")
        | "empty-exclusion" ->
            stable["benchmarkRows"] <- JsonNode.Parse("[]")
            stable["exclusionReason"] <- JsonValue.Create(" ")
        | "experimental-as-stable" -> experimental["stability"] <- JsonValue.Create("stable")
        | "stable-as-experimental" -> stable["stability"] <- JsonValue.Create("experimental")
        | "missing-allocation" | "missing-cost" | "missing-boxing" | "missing-resources" ->
            stable.AsObject().Remove(mutation.Substring("missing-".Length)) |> ignore
        | "missing-long-input-witness" -> (nodeField coverage "mandatoryWitnesses").AsArray().RemoveAt(0)
        | "missing-drain-witness" -> (nodeField coverage "mandatoryWitnesses").AsArray().RemoveAt(1)
        | "missing-linear-witness" -> (nodeField coverage "mandatoryWitnesses").AsArray().RemoveAt(2)
        | _ -> failwith "Unknown coverage mutation."
        if mutation <> "missing-map" then writeJson path (coverage.ToJsonString())
        let exitCode, _, stderr = runVerifier fixture
        assertFailsWith "Stable performance coverage" exitCode stderr

    [<Fact>]
    member _.VerifyPerformance_U13_ActualSourceCannotUseLegacyCoverageBypass() =
        use temp = new TempDirectory()
        let fixture = newFixture (Path.Combine(temp.Path, "actual-source"))
        Directory.CreateDirectory(Path.Combine(fixture.Root, "src", "FunnySharp")) |> ignore
        let exitCode, _, stderr = runVerifier fixture
        assertFailsWith "Stable performance coverage: stable-members.json is required" exitCode stderr

    [<Fact>]
    member _.VerifyPerformance_U13_SourceLinesPreserveBomCrLfCrAndUnterminatedLine() =
        use temp = new TempDirectory()
        let fixture = coverageFixture (Path.Combine(temp.Path, "line-semantics"))
        let path = coveragePath fixture
        let coverage = parseObject (File.ReadAllText path)
        let sourcePath = Path.Combine(fixture.Root, "coverage-source.txt")
        File.WriteAllText(sourcePath, "first\r\nsecond\rthird", Encoding.Unicode)
        (nodeItem (nodeField coverage "sourceFiles") 0)["sha256"] <- JsonValue.Create(fileSha256 sourcePath)
        let witnessPath = Path.Combine(fixture.Root, "coverage-witness.txt")
        File.WriteAllText(witnessPath, "LongInputBound\r\nCancelAndDrain\rLinearFirstSuccess", Encoding.UTF8)
        for witness in (nodeField coverage "mandatoryWitnesses").AsArray() do
            (requiredNode witness)["sha256"] <- JsonValue.Create(fileSha256 witnessPath)
        writeJson path (coverage.ToJsonString())
        let exitCode, _, stderr = runVerifier fixture
        assertPassed exitCode stderr
        // A later invocation must acquire new bytes, not reuse this epoch's lines.
        File.AppendAllText(sourcePath, "changed", Encoding.Unicode)
        let changedExit, _, changedErr = runVerifier fixture
        Assert.Equal(1, changedExit)
        Assert.Contains("missing or stale hash-bound evidence", changedErr)

    [<Theory>]
    [<InlineData("duplicate")>]
    [<InlineData("unknown")>]
    [<InlineData("excluded")>]
    member _.VerifyPerformance_AdmittedRowsRejectDuplicateUnknownAndExcluded(mutation: string) =
        use temp = new TempDirectory()
        let fixture = newFixture (Path.Combine(temp.Path, mutation))
        let rows =
            match mutation with
            | "duplicate" -> fixture.ReceiptRows @ [ fixture.ReceiptRows[0] ]
            | "unknown" -> fixture.ReceiptRows @ [ { fixture.ReceiptRows[0] with Id = "unknown" } ]
            | "excluded" -> fixture.ReceiptRows @ [ { fixture.ReceiptRows[0] with Id = "excluded|fixture" } ]
            | _ -> failwith "Unknown row mutation."
        writeReceipt { fixture with ReceiptRows = rows }
        let exitCode, _, stderr = runVerifier fixture
        Assert.Equal(1, exitCode)
        Assert.Contains((if mutation = "duplicate" then "duplicate row id" else "unregistered or excluded row"), stderr)



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
    member _.VerifyPerformance_U10_CompleteBinding_SucceedsWithoutChangingPolicy() =
        use temp = new TempDirectory()
        let fixture = boundFixture (Path.Combine(temp.Path, "bound"))
        let before = policyFingerprint fixture.ManifestPath
        let exitCode, _, stderr = runVerifier fixture
        assertPassed exitCode stderr
        Assert.Equal(before, policyFingerprint fixture.ManifestPath)

    [<Theory>]
    [<InlineData("missing-binding")>]
    [<InlineData("missing-preflight")>]
    [<InlineData("missing-launch")>]
    [<InlineData("incomplete-preflight")>]
    [<InlineData("duplicate-preflight")>]
    [<InlineData("wrong-baseline")>]
    [<InlineData("missing-semantic-case")>]
    [<InlineData("wrong-preflight-policy")>]
    [<InlineData("wrong-preflight-source")>]
    [<InlineData("wrong-preflight-protocol")>]
    [<InlineData("wrong-workload")>]
    [<InlineData("wrong-mvid")>]
    [<InlineData("wrong-run")>]
    [<InlineData("null-candidate")>]
    [<InlineData("wrong-candidate")>]
    [<InlineData("null-launch-candidate")>]
    [<InlineData("tampered-preflight")>]
    [<InlineData("tampered-workload")>]
    [<InlineData("tampered-launch")>]
    [<InlineData("new-source")>]
    [<InlineData("new-import")>]
    member _.VerifyPerformance_U10_MissingWrongOrTamperedBinding_Rejects(mutation: string) =
        use temp = new TempDirectory()
        let fixture = boundFixture (Path.Combine(temp.Path, mutation))
        let receipt = parseObject (File.ReadAllText fixture.ReceiptPath)
        match mutation with
        | "missing-binding" ->
            receipt.Remove("binding") |> ignore
            writeJson fixture.ReceiptPath (receipt.ToJsonString())
        | "missing-preflight" ->
            File.Delete(Path.Combine(
                fixture.ReceiptsDir,
                (nodeField (nodeField (nodeField receipt "binding") "preflight") "file").GetValue<string>()))
        | "missing-launch" ->
            (nodeField (nodeField receipt "binding") "launches").AsArray().RemoveAt(0)
            writeJson fixture.ReceiptPath (receipt.ToJsonString())
        | "incomplete-preflight" ->
            mutateBoundPreflight fixture (fun node -> (nodeField node "rows").AsArray().RemoveAt(0))
        | "duplicate-preflight" ->
            mutateBoundPreflight fixture (fun node ->
                (nodeField node "rows").AsArray().Add((nodeItem (nodeField node "rows") 0).DeepClone()))
        | "wrong-baseline" ->
            mutateBoundPreflight fixture (fun node ->
                (nodeItem (nodeField node "rows") 0)["baseline"] <- JsonValue.Create(false))
        | "missing-semantic-case" ->
            mutateBoundPreflight fixture (fun node -> (nodeField node "semanticCases").AsArray().Clear())
        | "wrong-preflight-policy" ->
            mutateBoundPreflight fixture (fun node ->
                node["policyFingerprint"] <- JsonValue.Create(String.replicate 64 "0"))
        | "wrong-preflight-source" ->
            mutateBoundPreflight fixture (fun node ->
                node["benchmarkInputFingerprint"] <- JsonValue.Create(String.replicate 64 "0"))
        | "wrong-preflight-protocol" ->
            mutateBoundPreflight fixture (fun node ->
                node["protocolFingerprint"] <- JsonValue.Create(String.replicate 64 "0"))
        | "wrong-workload" ->
            mutateBoundLaunch fixture (fun node ->
                node["workload"] <- (nodeItem (nodeField node "assemblies") 0).DeepClone())
        | "wrong-mvid" ->
            mutateBoundLaunch fixture (fun node ->
                (nodeField node "workload")["mvid"] <- JsonValue.Create(Guid.Empty.ToString()))
        | "wrong-run" ->
            mutateBoundLaunch fixture (fun node ->
                node["runId"] <- JsonValue.Create("22222222222222222222222222222222"))
        | "null-candidate" ->
            receipt["candidateCommit"] <- null
            writeJson fixture.ReceiptPath (receipt.ToJsonString())
        | "wrong-candidate" ->
            receipt["candidateCommit"] <- JsonValue.Create("snapshot:" + String.replicate 64 "0")
            writeJson fixture.ReceiptPath (receipt.ToJsonString())
        | "null-launch-candidate" ->
            mutateBoundLaunch fixture (fun node -> node["candidateSnapshot"] <- null)
        | "tampered-preflight" ->
            let path = Path.Combine(
                fixture.ReceiptsDir,
                (nodeField (nodeField (nodeField receipt "binding") "preflight") "file").GetValue<string>())
            File.AppendAllText(path, " ")
        | "tampered-workload" ->
            File.AppendAllText(Path.Combine(fixture.ReceiptsDir, "fixture-workload.dll"), "changed")
        | "tampered-launch" ->
            let path = Path.Combine(
                fixture.ReceiptsDir,
                (nodeField (nodeItem (nodeField (nodeField receipt "binding") "launches") 0) "file").GetValue<string>())
            File.AppendAllText(path, "changed")
        | "new-source" ->
            writeText (Path.Combine(fixture.Root, "src", "Fixture", "New.cs")) "new shipping input"
        | "new-import" ->
            writeText (Path.Combine(fixture.Root, "New.props")) "<Project />"
        | _ -> failwith "Unknown mutation."
        let exitCode, _, stderr = runVerifier fixture
        Assert.Equal(1, exitCode)
        Assert.False(String.IsNullOrWhiteSpace stderr)

    [<Theory>]
    [<InlineData("src/Fixture/UnitResult.cs")>]
    [<InlineData("src/Fixture/Fixture.csproj")>]
    [<InlineData("src/Fixture/packages.lock.json")>]
    [<InlineData("Directory.Build.props")>]
    member _.VerifyPerformance_U10_StaleShippingBuildInput_Rejects(relative: string) =
        use temp = new TempDirectory()
        let fixture = boundFixture (Path.Combine(temp.Path, "stale"))
        File.AppendAllText(Path.Combine(fixture.Root, relative), "changed")
        let exitCode, _, stderr = runVerifier fixture
        Assert.Equal(1, exitCode)
        Assert.False(String.IsNullOrWhiteSpace stderr)

    [<Fact>]
    member _.VerifyPerformance_U10_TwoBaselinesInAComparisonGroup_Rejects() =
        use temp = new TempDirectory()
        let fixture = newFixture (Path.Combine(temp.Path, "two-baselines"))
        let mutated =
            { fixture with
                PolicyRows =
                    [ fixture.PolicyRows[0]
                      { fixture.PolicyRows[1] with Baseline = true }
                      fixture.PolicyRows[2] ]
                ReceiptRows =
                    [ fixture.ReceiptRows[0]
                      { fixture.ReceiptRows[1] with Baseline = true } ] }
        writeManifest mutated
        let repinned = { mutated with PolicyFingerprint = policyFingerprint mutated.ManifestPath }
        writeReceipt repinned
        let exitCode, _, stderr = runVerifier repinned
        Assert.Equal(1, exitCode)
        Assert.False(String.IsNullOrWhiteSpace stderr)

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
