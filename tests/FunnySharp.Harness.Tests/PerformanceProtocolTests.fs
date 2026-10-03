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


type PerformanceProtocolTests() =

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
