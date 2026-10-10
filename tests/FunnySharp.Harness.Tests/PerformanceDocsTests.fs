module FunnySharp.Harness.Tests.PerformanceDocsTests

// Behaviour tests for the F# port of eng/Generate-PerformanceDocumentation.ps1.
// They mirror the retired PowerShell suite's documentation cases and add the
// byte-determinism and no-write contracts the port must keep.

open System
open System.Globalization
open System.IO
open System.Text
open System.Text.Json.Nodes
open Xunit
open FunnySharp.Harness.PerformanceDocs
open FunnySharp.Harness.Tests.Support

let private utf8NoBom = UTF8Encoding(false)

// ---- Fixture ----

let private policyText =
    """{
  "revision": "fixture-v1",
  "rows": [
    {
      "id": "Fixture|Category|Direct|",
      "benchmarkClass": "Fixture",
      "category": "Category",
      "method": "Direct",
      "parameters": "",
      "baseline": true,
      "carrier": "value",
      "completionPath": "synchronous",
      "expectedResult": "42",
      "comparisonGroup": "fixture",
      "included": true,
      "exclusionReason": null,
      "allocationBudgetBytes": 0
    },
    {
      "id": "Fixture|Category|Funny|",
      "benchmarkClass": "Fixture",
      "category": "Category",
      "method": "Funny",
      "parameters": "",
      "baseline": false,
      "carrier": "value",
      "completionPath": "synchronous",
      "expectedResult": "42",
      "comparisonGroup": "fixture",
      "included": true,
      "exclusionReason": null,
      "allocationBudgetBytes": 16
    },
    {
      "id": "Fixture|Category|Alternative|",
      "benchmarkClass": "Fixture",
      "category": "Category",
      "method": "Alternative",
      "parameters": "",
      "baseline": false,
      "carrier": "value",
      "completionPath": "synchronous",
      "expectedResult": "42",
      "comparisonGroup": "fixture",
      "included": true,
      "exclusionReason": null,
      "allocationBudgetBytes": 8
    },
    {
      "id": "excluded|fixture",
      "benchmarkClass": "Fixture",
      "category": "Unmeasured",
      "method": "",
      "parameters": "",
      "baseline": false,
      "carrier": "not-measured",
      "completionPath": "not-measured",
      "expectedResult": "not-measured",
      "comparisonGroup": "unmeasured fixture",
      "included": false,
      "exclusionReason": "No release claim depends on this scenario.",
      "allocationBudgetBytes": null
    }
  ]
}"""

let private guideText =
    "# Guide\n\n<!-- performance-table:start fixture -->\nold\n<!-- performance-table:end fixture -->\n\ntrailing prose\n"

let private observationRow
    (id: string)
    (benchmarkClass: string)
    (category: string)
    (method: string)
    (parameters: string)
    (baseline: bool)
    (timingState: string)
    (meanNanoseconds: string)
    (allocatedBytes: int)
    : string =
    sprintf
        """{"id": "%s", "benchmarkClass": "%s", "category": "%s", "method": "%s", "parameters": "%s", "baseline": %b, "timingState": "%s", "meanNanoseconds": %s, "allocatedBytesPerOperation": %d}"""
        id
        benchmarkClass
        category
        method
        parameters
        baseline
        timingState
        meanNanoseconds
        allocatedBytes

let private defaultObservationRows =
    [ observationRow "Fixture|Category|Direct|" "Fixture" "Category" "Direct" "" true "below-resolution" "null" 0
      observationRow "Fixture|Category|Funny|" "Fixture" "Category" "Funny" "" false "observed" "2.5" 16
      observationRow
          "Fixture|Category|Alternative|"
          "Fixture"
          "Category"
          "Alternative"
          ""
          false
          "unavailable"
          "null"
          8 ]

let private manifestText
    (observationRows: string list)
    : string =
    "{\n"
    + "  \"schemaVersion\": 1,\n"
    + "  \"benchmarkInput\": { \"files\": [\"a-input.txt\", \"Z-input.txt\"] },\n"
    + "  \"protocol\": { \"files\": [\"a-protocol.txt\", \"Z-protocol.txt\"] },\n"
    + "  \"policy\": "
    + policyText
    + ",\n"
    + "  \"observation\": {\n"
    + "    \"schemaVersion\": 1,\n"
    + "    \"policyRevision\": \"fixture-v1\",\n"
    + "    \"rows\": [\n      "
    + String.concat ",\n      " observationRows
    + "\n    ]\n"
    + "  },\n"
    + "  \"documentation\": [ { \"id\": \"fixture\", \"path\": \"guide.md\", \"benchmarkClasses\": [\"Fixture\"] } ]\n"
    + "}"

type private DocFixture =
    { Root: string
      ManifestPath: string
      GuidePath: string }

let private createFixture (root: string) (observationRows: string list) : DocFixture =
    File.WriteAllText(Path.Combine(root, "a-input.txt"), "input-a", utf8NoBom)
    File.WriteAllText(Path.Combine(root, "Z-input.txt"), "input-z", utf8NoBom)
    File.WriteAllText(Path.Combine(root, "a-protocol.txt"), "protocol-a", utf8NoBom)
    File.WriteAllText(Path.Combine(root, "Z-protocol.txt"), "protocol-z", utf8NoBom)

    let guidePath = Path.Combine(root, "guide.md")
    File.WriteAllText(guidePath, guideText, utf8NoBom)

    let manifestPath = Path.Combine(root, "baseline.json")

    File.WriteAllText(
        manifestPath,
        manifestText observationRows,
        utf8NoBom
    )

    { Root = root
      ManifestPath = manifestPath
      GuidePath = guidePath }

// ---- Runner helpers ----

let private run (root: string) (manifestPath: string) (verify: bool) : int * string * string =
    use stdout = new StringWriter()
    use stderr = new StringWriter()

    let arguments =
        [ "-RepositoryRoot"; root; "-ManifestPath"; manifestPath ]
        @ (if verify then [ "-Verify" ] else [])

    let exitCode = mainWith stdout stderr arguments
    exitCode, stdout.ToString(), stderr.ToString()

let private runDefaultManifest (root: string) (verify: bool) : int * string * string =
    use stdout = new StringWriter()
    use stderr = new StringWriter()

    let arguments = [ "-RepositoryRoot"; root ] @ (if verify then [ "-Verify" ] else [])
    let exitCode = mainWith stdout stderr arguments
    exitCode, stdout.ToString(), stderr.ToString()

type DocumentationFixtureTests() =

    [<Fact>]
    member _.GeneratePerformanceDocumentation_GroupsMixedSpellingsAndSortsGroupsByTheirComparer() =
        use temp = new TempDirectory()
        let categories = [ "Zulu"; "Alpha" ]
        let observations =
            categories
            |> List.collect (fun category ->
                defaultObservationRows |> List.map (fun row -> row.Replace("Category", category)))
        let fixture = createFixture temp.Path observations
        let policy = Assert.IsType<JsonObject>(JsonNode.Parse policyText)
        let originalRows =
            Assert.IsType<JsonArray>(policy.["rows"])
            |> Seq.map (fun row -> Assert.IsType<JsonObject>(row))
            |> Seq.toArray
        let rows = JsonArray()

        for category in categories do
            for index in 0..2 do
                let row = Assert.IsType<JsonObject>(originalRows.[index].DeepClone())
                let id = Assert.IsAssignableFrom<JsonValue>(row.["id"]).GetValue<string>()
                row.["id"] <- JsonValue.Create(id.Replace("Category", category))
                row.["category"] <- JsonValue.Create category
                row.["comparisonGroup"] <-
                    JsonValue.Create(if index = 1 then category.ToLowerInvariant() else category.ToUpperInvariant())
                rows.Add row

        rows.Add(originalRows.[3].DeepClone())
        policy.["rows"] <- rows
        let customPolicy = policy.ToJsonString()
        let manifest =
            (File.ReadAllText fixture.ManifestPath)
                .Replace(policyText, customPolicy)
        File.WriteAllText(fixture.ManifestPath, manifest, utf8NoBom)
        let exitCode, _, stderr = run fixture.Root fixture.ManifestPath false
        Assert.True((exitCode = 0), stderr)
        let scenarios =
            (File.ReadAllLines fixture.GuidePath)
            |> Array.filter (fun line -> line.StartsWith("| Alpha") || line.StartsWith("| Zulu"))
            |> Array.map (fun line -> line.Split('|').[1].Trim())
        Assert.Equal<string array>(
            [| "Alpha - Alternative"; "Alpha - Funny"; "Zulu - Alternative"; "Zulu - Funny" |],
            scenarios)
        let verifyExit, _, verifyErr = run fixture.Root fixture.ManifestPath true
        Assert.True((verifyExit = 0), verifyErr)

    [<Fact>]
    member _.GeneratePerformanceDocumentation_RejectsObservationDescriptorMutation() =
        use temp = new TempDirectory()
        let fixture = createFixture temp.Path defaultObservationRows
        let original = File.ReadAllText fixture.ManifestPath
        let row = defaultObservationRows[1]
        File.WriteAllText(fixture.ManifestPath, original.Replace(row, row.Replace("\"method\": \"Funny\"", "\"method\": \"Other\"")), utf8NoBom)
        let exitCode, _, stderr = run fixture.Root fixture.ManifestPath false
        Assert.Equal(1, exitCode)
        Assert.Contains("does not match policy field", stderr)
        Assert.Equal(guideText, File.ReadAllText fixture.GuidePath)

    [<Fact>]
    member _.GeneratePerformanceDocumentation_GeneratesThenVerifies_UnderInvariantCulture() =
        use temp = new TempDirectory()
        let fixture = createFixture temp.Path defaultObservationRows
        let previous = CultureInfo.CurrentCulture

        try
            CultureInfo.CurrentCulture <- CultureInfo.GetCultureInfo("fr-FR")

            let generateExit, _, generateErr = run fixture.Root fixture.ManifestPath false
            Assert.Equal(0, generateExit)
            Assert.Equal("", generateErr.Trim())

            let verifyExit, _, verifyErr = run fixture.Root fixture.ManifestPath true
            Assert.Equal(0, verifyExit)
            Assert.Equal("", verifyErr.Trim())

            let generated = File.ReadAllText fixture.GuidePath

            let candidateRows =
                generated.Split('\n') |> Array.filter (fun line -> line.StartsWith("| Category"))

            Assert.Equal(2, candidateRows.Length)
            Assert.Contains("2.500 ns", generated)
            Assert.Contains("| Category - Alternative | N/A | N/A | N/A | 0 B | 8 B |", generated)
            Assert.Contains("| Category - Funny | N/A | 2.500 ns | N/A | 0 B | 16 B |", generated)
            Assert.Contains("Excluded measurements:", generated)
            Assert.Contains("- unmeasured fixture: No release claim depends on this scenario.", generated)
            Assert.Contains("trailing prose", generated)
        finally
            CultureInfo.CurrentCulture <- previous

    [<Fact>]
    member _.GeneratePerformanceDocumentation_Verify_RejectsObservationRowCountMismatch() =
        use temp = new TempDirectory()

        let fixture =
            createFixture temp.Path (List.take 2 defaultObservationRows)

        let exitCode, stdout, stderr = run fixture.Root fixture.ManifestPath true
        Assert.Equal(1, exitCode)
        Assert.Equal("", stdout.Trim())
        Assert.Contains("row count", stderr)

    [<Fact>]
    member _.GeneratePerformanceDocumentation_Verify_DetectsManualEdit() =
        use temp = new TempDirectory()
        let fixture = createFixture temp.Path defaultObservationRows

        let generateExit, _, _ = run fixture.Root fixture.ManifestPath false
        Assert.Equal(0, generateExit)

        let edited = (File.ReadAllText fixture.GuidePath).Replace("2.500 ns", "manual edit")
        File.WriteAllText(fixture.GuidePath, edited, utf8NoBom)

        let exitCode, stdout, stderr = run fixture.Root fixture.ManifestPath true
        Assert.Equal(1, exitCode)
        Assert.Equal("", stdout.Trim())
        Assert.Contains("stale", stderr)
        // -Verify must not write: the manual edit is still present afterwards.
        Assert.Contains("manual edit", File.ReadAllText fixture.GuidePath)

    [<Fact>]
    member _.GeneratePerformanceDocumentation_VerifyDoesNotWrite() =
        use temp = new TempDirectory()
        let fixture = createFixture temp.Path defaultObservationRows

        let generateExit, _, _ = run fixture.Root fixture.ManifestPath false
        Assert.Equal(0, generateExit)

        let generated = File.ReadAllText(fixture.GuidePath).Replace("\r\n", "\n").Replace("\n", "\r\n")
        File.WriteAllText(fixture.GuidePath, generated)
        let verifyExit, _, _ = run fixture.Root fixture.ManifestPath true
        Assert.Equal(0, verifyExit)
        Assert.Equal(generated, File.ReadAllText fixture.GuidePath)

    [<Fact>]
    member _.GeneratePerformanceDocumentation_GenerateIsIdempotent() =
        use temp = new TempDirectory()
        let fixture = createFixture temp.Path defaultObservationRows

        let firstExit, _, _ = run fixture.Root fixture.ManifestPath false
        let first = File.ReadAllText fixture.GuidePath
        let secondExit, _, _ = run fixture.Root fixture.ManifestPath false
        Assert.Equal(0, firstExit)
        Assert.Equal(0, secondExit)
        Assert.Equal(first, File.ReadAllText fixture.GuidePath)
        Assert.DoesNotContain("\r", first)

    [<Fact>]
    member _.GeneratePerformanceDocumentation_MissingMarkers_Rejects() =
        use temp = new TempDirectory()
        let fixture = createFixture temp.Path defaultObservationRows

        File.WriteAllText(
            fixture.GuidePath,
            "# Guide\n\n<!-- performance-table:start fixture -->\nold\n",
            utf8NoBom
        )

        let exitCode, stdout, stderr = run fixture.Root fixture.ManifestPath false
        Assert.Equal(1, exitCode)
        Assert.Equal("", stdout.Trim())
        Assert.Contains("missing the 'fixture' generated-region markers", stderr)

    [<Fact>]
    member _.GeneratePerformanceDocumentation_ObservedBaselineFormatsRatioAndMicroseconds() =
        use temp = new TempDirectory()

        let rows =
            [ observationRow "Fixture|Category|Direct|" "Fixture" "Category" "Direct" "" true "observed" "1.0" 0
              observationRow "Fixture|Category|Funny|" "Fixture" "Category" "Funny" "" false "observed" "2.5" 16
              observationRow
                  "Fixture|Category|Alternative|"
                  "Fixture"
                  "Category"
                  "Alternative"
                  ""
                  false
                  "observed"
                  "2500"
                  8 ]

        let fixture = createFixture temp.Path rows
        let exitCode, _, _ = run fixture.Root fixture.ManifestPath false
        Assert.Equal(0, exitCode)

        let generated = File.ReadAllText fixture.GuidePath
        Assert.Contains("| Category - Funny | 1.000 ns | 2.500 ns | 2.50x | 0 B | 16 B |", generated)

        Assert.Contains(
            "| Category - Alternative | 1.000 ns | 2.500 us | 2,500.00x | 0 B | 8 B |",
            generated
        )

type DocumentationCommandLineTests() =

    [<Fact>]
    member _.UnknownArgumentIsUsageFailure() =
        use stdout = new StringWriter()
        use stderr = new StringWriter()
        let exitCode = mainWith stdout stderr [ "--bogus" ]
        Assert.Equal(2, exitCode)
        Assert.Equal("", stdout.ToString().Trim())
        Assert.Contains("unrecognized argument: --bogus", stderr.ToString())

    [<Fact>]
    member _.MissingManifestIsAVerificationFailure() =
        use temp = new TempDirectory()
        let manifestPath = Path.Combine(temp.Path, "missing-baseline.json")
        let exitCode, stdout, stderr = run temp.Path manifestPath true
        Assert.Equal(1, exitCode)
        Assert.Equal("", stdout.Trim())
        Assert.Contains("Performance manifest was not found", stderr)

type RealManifestTests() =

    [<Fact>]
    member _.BaselineManifestVerifiesElevenRegions() =
        let root = repositoryRoot ()
        let exitCode, _, stderr = runDefaultManifest root true
        Assert.Equal(0, exitCode)
        Assert.Equal("", stderr.Trim())

    [<Fact>]
    member _.CompetitorManifestReproducesProtocolStep13() =
        let root = repositoryRoot ()
        let manifestPath = Path.Combine(root, "eng/performance/competitor-baseline.json")
        let exitCode, _, stderr = run root manifestPath true
        Assert.Equal(0, exitCode)
        Assert.Equal("", stderr.Trim())
