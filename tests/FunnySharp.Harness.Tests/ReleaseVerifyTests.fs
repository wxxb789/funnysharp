module FunnySharp.Harness.Tests.ReleaseVerifyTests

// xUnit port of the assertions inside eng/Verify-Release.ps1 (lane C). Each fact
// pins one verdict rule the release-evidence audit owns: path safety, the protocol
// command table, the English log markers (and their rejection of the localized logs
// the PowerShell run captured), the benchmark-report multiset contract, the
// compatibility scenario contract, and the exact success/failure text.

open System
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open Xunit
open FunnySharp.Harness
open FunnySharp.Harness.Proc
open FunnySharp.Harness.ReleaseVerifySource
open FunnySharp.Harness.ReleaseVerifyArtifacts
open FunnySharp.Harness.Tests.Support

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let private expectFailure (substring: string) (action: unit -> unit) : unit =
    let exceptionThrown = Assert.Throws<ReleaseVerifyFailure>(fun () -> action ())
    Assert.Contains(substring, exceptionThrown.Message)

let private parse (json: string) : JsonDocument = JsonDocument.Parse json

let private utf8NoBom = UTF8Encoding(false)

// ---------------------------------------------------------------------------
// ANSI and sequence helpers
// ---------------------------------------------------------------------------

[<Fact>]
let ``RemoveAnsiControlSequences_StripsCsiSequences`` () =
    let coloured = "\u001b[32mBuild succeeded.\u001b[0m"
    Assert.Equal("Build succeeded.", removeAnsiControlSequences coloured)

[<Fact>]
let ``ExactStringSequence_MatchesOrdinalAndRejectsDrift`` () =
    Assert.True(exactStringSequence [ "a"; "b" ] [ "a"; "b" ])
    Assert.False(exactStringSequence [ "a"; "b" ] [ "b"; "a" ])
    Assert.False(exactStringSequence [ "a" ] [ "a"; "b" ])

// ---------------------------------------------------------------------------
// Path safety
// ---------------------------------------------------------------------------

[<Fact>]
let ``AssertSafeArtifactsSubdirectory_AcceptsProperChild`` () =
    use temp = new TempDirectory()
    let artifacts = Path.Combine(temp.Path, "artifacts")
    Directory.CreateDirectory(Path.Combine(artifacts, "release-evidence")) |> ignore

    let resolved =
        assertSafeArtifactsSubdirectory (Path.Combine(artifacts, "release-evidence")) artifacts

    Assert.Equal(Path.Combine(artifacts, "release-evidence"), resolved)

[<Fact>]
let ``AssertSafeArtifactsSubdirectory_RejectsArtifactsRootAndSibling`` () =
    use temp = new TempDirectory()
    let artifacts = Path.Combine(temp.Path, "artifacts")
    Directory.CreateDirectory artifacts |> ignore

    expectFailure "proper subdirectory" (fun () -> assertSafeArtifactsSubdirectory artifacts artifacts |> ignore)

    expectFailure "proper subdirectory" (fun () ->
        assertSafeArtifactsSubdirectory (Path.Combine(temp.Path, "outside")) artifacts |> ignore)

[<Fact>]
let ``GetSafeEvidenceFile_MissingFile_Throws`` () =
    use temp = new TempDirectory()
    let artifacts = Path.Combine(temp.Path, "artifacts")
    Directory.CreateDirectory artifacts |> ignore

    expectFailure "Evidence file was not found" (fun () ->
        getSafeEvidenceFile (Path.Combine(artifacts, "execution-evidence.json")) artifacts |> ignore)

// ---------------------------------------------------------------------------
// Protocol and canonical commands
// ---------------------------------------------------------------------------

let private benchmarkSkippedNames: string list =
    [ "clean"
      "restore"
      "build"
      "test"
      "examples"
      "aspnetcore-examples"
      "pack"
      "format"
      "performance-protocol-tests"
      "release-protocol-tests"
      "benchmark-preflight"
      "performance-docs-verify"
      "competitor-performance-docs-verify"
      "compatibility" ]

[<Fact>]
let ``ReadReleaseProtocol_RejectsMissingFile`` () =
    expectFailure "Release protocol was not found" (fun () ->
        readReleaseProtocol "/nonexistent/release-protocol.json" |> ignore)

[<Fact>]
let ``ExpectedReleaseCommands_BenchmarkSkippedMode_AreTheFourteenInOrder`` () =
    let root = repositoryRoot()
    let output = Path.Combine(root, "artifacts", "release-candidate", "probe", "attempt")

    let commands =
        getExpectedReleaseCommands output "https://api.nuget.org/v3/index.json" root "linux-x64" true

    Assert.Equal<string list>(benchmarkSkippedNames, commands |> List.map (fun command -> command.Name))

    let pack = commands |> List.find (fun command -> command.Name = "pack")

    Assert.Equal("dotnet", pack.FileName)
    Assert.Equal(root, pack.WorkingDirectory)
    Assert.Contains(Path.Combine(output, "packages"), pack.Arguments)

[<Fact>]
let ``ExpectedReleaseCommands_FullMode_AddsBenchmarkAndPerformanceVerify`` () =
    let root = repositoryRoot()
    let output = Path.Combine(root, "artifacts", "release-candidate", "probe", "attempt")

    let names =
        getExpectedReleaseCommands output "https://api.nuget.org/v3/index.json" root "linux-x64" false
        |> List.map (fun command -> command.Name)

    Assert.Equal(16, names.Length)
    Assert.Contains("benchmark", names)
    Assert.Contains("performance-verify", names)
    Assert.DoesNotContain("benchmark", benchmarkSkippedNames)

[<Fact>]
let ``AssertCanonicalReleaseCommand_RejectsArgumentDrift`` () =
    use commandDocument =
        parse """{"fileName":"dotnet","workingDirectory":"/r","arguments":["build","FunnySharp.slnx"]}"""

    let expected =
        { Name = "build"
          FileName = "dotnet"
          WorkingDirectory = "/r"
          Arguments = [ "build"; "FunnySharp.slnx" ] }

    assertCanonicalReleaseCommand commandDocument.RootElement expected "Execution receipt 'x'"

    let drifted =
        { expected with Arguments = [ "build"; "Other.slnx" ] }

    expectFailure "does not use the canonical command" (fun () ->
        assertCanonicalReleaseCommand commandDocument.RootElement drifted "Execution receipt 'x'")

// ---------------------------------------------------------------------------
// Captured-log markers
// ---------------------------------------------------------------------------

let private englishBuildLog =
    "  FunnySharp -> /tmp/FunnySharp.dll\nBuild succeeded.\n    0 Warning(s)\n    0 Error(s)\n"

[<Theory>]
[<InlineData("empty")>]
[<InlineData("utf8")>]
[<InlineData("utf8-bom")>]
[<InlineData("utf16-le")>]
[<InlineData("utf16-be")>]
[<InlineData("utf32-le")>]
[<InlineData("utf32-be")>]
[<InlineData("malformed-utf8")>]
let ``ExecutionLogText_BindsRawBytesAndPreservesDecoding`` (encodingName: string) =
    use temp = new TempDirectory()
    let artifacts = Path.Combine(temp.Path, "artifacts")
    let execution = Path.Combine(artifacts, "attempt")
    Directory.CreateDirectory(Path.Combine(execution, "logs")) |> ignore
    let relativePath = "logs/01-build.stdout.log"
    let path = Path.Combine(execution, relativePath)
    let prefix = "\u001b[32m" + englishBuildLog + "\u001b[0m"
    // Cross the reader's byte buffer with multibyte text and end in a surrogate pair.
    let suffix = String.replicate (1023 - prefix.Length) "a" + "\u00e9\u4e2d\U0001F642"
    let source = prefix + suffix
    let bytes, decoded, normalized =
        match encodingName with
        | "empty" -> [||], "", ""
        | "malformed-utf8" ->
            Array.append (Encoding.UTF8.GetBytes source) [| 0xC3uy; 0x28uy; 0xFFuy; 0xE2uy; 0x82uy |],
            source + "\ufffd(\ufffd\ufffd",
            englishBuildLog + suffix + "\ufffd(\ufffd\ufffd"
        | _ ->
            let encoding: Encoding =
                match encodingName with
                | "utf8" -> UTF8Encoding(false)
                | "utf8-bom" -> UTF8Encoding(true)
                | "utf16-le" -> UnicodeEncoding(false, true)
                | "utf16-be" -> UnicodeEncoding(true, true)
                | "utf32-le" -> UTF32Encoding(false, true)
                | "utf32-be" -> UTF32Encoding(true, true)
                | _ -> failwithf "Unknown encoding fixture: %s" encodingName
            Array.append (encoding.GetPreamble()) (encoding.GetBytes source), source, englishBuildLog + suffix

    File.WriteAllBytes(path, bytes)
    // Independent byte-array hash, not the production file/hash/text helpers.
    let expectedSha256 = Convert.ToHexString(SHA256.HashData bytes)
    Assert.Equal(decoded, File.ReadAllText path)
    let struct (actualPath, actualSha256, text) =
        getExecutionLogText execution relativePath expectedSha256 artifacts
    Assert.Equal(Path.GetFullPath path, actualPath)
    Assert.Equal(expectedSha256.ToLowerInvariant(), actualSha256)
    Assert.Equal(normalized, text)
    if encodingName <> "empty" then
        assertBuildMarkers text

    // Preserve every success marker but change the same file's raw-byte identity.
    File.WriteAllBytes(path, Array.append bytes [| 0x0Auy |])
    expectFailure "log hash does not match receipt" (fun () ->
        getExecutionLogText execution relativePath expectedSha256 artifacts |> ignore)

[<Theory>]
[<InlineData("../outside.log")>]
[<InlineData("logs/../outside.log")>]
[<InlineData("logs\\..\\outside.log")>]
let ``ExecutionLogText_RejectsParentTraversal`` (relativePath: string) =
    use temp = new TempDirectory()
    let artifacts = Path.Combine(temp.Path, "artifacts")
    let execution = Path.Combine(artifacts, "attempt")
    expectFailure "log path must be a relative child path" (fun () ->
        getExecutionLogText execution relativePath (String.replicate 64 "0") artifacts |> ignore)

[<Fact>]
let ``BuildMarkers_AcceptEnglishAndRejectLocalized`` () =
    assertBuildMarkers englishBuildLog

    // The captured PowerShell baseline ran without DOTNET_CLI_UI_LANGUAGE=en and
    // its logs are localized, so the English-only verdict must fail closed.
    let localized = "已成功生成。\n    0 个警告\n    0 个错误\n"

    expectFailure "Build evidence must report Build succeeded" (fun () -> assertBuildMarkers localized)

[<Fact>]
let ``TestMarkers_AcceptPositiveAllPassingSummary`` () =
    let root = repositoryRoot()

    let lines =
        [ "Test run summary: Passed!"
          "  total: 12"
          "  failed: 0"
          "  succeeded: 12"
          "  skipped: 0"
          Path.GetFullPath(Path.Combine(root, testAssemblyRelativePaths.[0]))
          + " (net10.0|x64) passed (5s 183ms)"
          Path.GetFullPath(Path.Combine(root, testAssemblyRelativePaths.[1]))
          + " (net10.0|x64) passed (2s 100ms)" ]

    Assert.Equal(12, assertTestMarkers (String.concat "\n" lines) root)

[<Fact>]
let ``TestMarkers_RejectZeroTotal`` () =
    let root = repositoryRoot()

    let log =
        "Test run summary: Passed!\n  total: 0\n  failed: 0\n  succeeded: 0\n  skipped: 0\n"

    expectFailure "positive all-passing count" (fun () -> assertTestMarkers log root |> ignore)

[<Fact>]
let ``ExampleMarkers_RequireBothSuccessLines`` () =
    assertExampleMarkers CoreExampleMarker AspNetCoreExampleMarker

    expectFailure "Examples evidence" (fun () -> assertExampleMarkers "nothing" AspNetCoreExampleMarker)

// ---------------------------------------------------------------------------
// Benchmark report contract
// ---------------------------------------------------------------------------

let private row (benchmarkClass: string) (category: string) (method: string) (parameters: string) =
    { BenchmarkClass = benchmarkClass
      Category = category
      Method = method
      Parameters = parameters }

[<Fact>]
let ``BenchmarkRows_MatchingMultiset_Passes`` () =
    let expected = [ row "Cls" "Cat" "A" "[N=1]"; row "Cls" "Cat" "B" "[N=2]" ]
    assertBenchmarkRows expected (List.rev expected) "Performance observation proposal"

[<Fact>]
let ``BenchmarkRows_MissingRow_ThrowsDoesNotMatch`` () =
    expectFailure "does not match the registered benchmark rows" (fun () ->
        assertBenchmarkRows [ row "Cls" "Cat" "A" "[N=1]" ] [] "Performance observation proposal")

[<Fact>]
let ``BenchmarkRows_UnregisteredMethod_ThrowsDoesNotMatch`` () =
    expectFailure "does not match the registered benchmark rows" (fun () ->
        assertBenchmarkRows
            [ row "Cls" "Cat" "A" "[N=1]" ]
            [ row "Cls" "Cat" "Z" "[N=1]" ]
            "Performance observation proposal")

[<Fact>]
let ``BenchmarkParameterNames_NoParameterClass_ReturnsNone`` () =
    use document = parse """[{"benchmarkClass":"Cls","parameters":""}]"""

    Assert.Equal(None, getBenchmarkParameterNames (arrayItems document.RootElement) "Cls")

[<Fact>]
let ``BenchmarkParameterNames_InconsistentIdentities_Throws`` () =
    use document =
        parse """[{"parameters":"[N=1, M=2]"},{"parameters":"[M=2, N=1]"}]"""

    expectFailure "inconsistent parameter identities" (fun () ->
        getBenchmarkParameterNames (arrayItems document.RootElement) "Cls" |> ignore)

// ---------------------------------------------------------------------------
// Package archive selection
// ---------------------------------------------------------------------------

[<Fact>]
let ``FindPackageArchive_SelectsExactlyOne`` () =
    let files =
        [ "/t/FunnySharp.0.1.0.nupkg"; "/t/FunnySharp.AspNetCore.0.1.0.nupkg" ]

    Assert.Equal("/t/FunnySharp.0.1.0.nupkg", findPackageArchive files "FunnySharp" "nupkg")

[<Fact>]
let ``FindPackageArchive_MultipleMatches_Throws`` () =
    let files = [ "/t/FunnySharp.0.1.0.nupkg"; "/t/FunnySharp.0.2.0.nupkg" ]

    expectFailure "Expected exactly one" (fun () -> findPackageArchive files "FunnySharp" "nupkg" |> ignore)

// ---------------------------------------------------------------------------
// Compatibility evidence
// ---------------------------------------------------------------------------

let private scenarioEntry (name: string) (coreHash: string) : string =
    sprintf
        """{"Scenario":"%s","Outcome":"Passed","RuntimeIdentifier":"linux-x64","CorePackageVersion":"0.1.0","AspNetCorePackageVersion":"0.1.0","CorePackageSha256":"%s","AspNetCorePackageSha256":"BB"}"""
        name
        coreHash

let private allScenarioNames =
    [ "AspNetCoreNativeAot"; "AspNetCoreTrimmed"; "CoreNativeAot"; "CoreTrimmed" ]

let private packageInventory: string =
    """{"core":{"version":"0.1.0","sha256":"aa"},"aspNetCore":{"version":"0.1.0","sha256":"bb"}}"""

[<Fact>]
let ``CompatibilityEvidence_CompletePassingSet_ReturnsScenarios`` () =
    let json =
        """{"SchemaVersion":1,"Succeeded":true,"Scenarios":["""
        + String.concat "," [ for name in allScenarioNames -> scenarioEntry name "AA" ]
        + "]}"

    use evidenceDocument = parse json
    use inventoryDocument = parse packageInventory

    let verified =
        assertCompatibilityEvidence evidenceDocument.RootElement inventoryDocument.RootElement "linux-x64"

    Assert.Equal(4, verified.Length)

[<Fact>]
let ``CompatibilityEvidence_MissingScenario_ThrowsExactScenarioSet`` () =
    let json =
        """{"SchemaVersion":1,"Succeeded":true,"Scenarios":["""
        + scenarioEntry "CoreTrimmed" "AA"
        + "]}"

    use evidenceDocument = parse json
    use inventoryDocument = parse packageInventory

    expectFailure "exactly these scenarios" (fun () ->
        assertCompatibilityEvidence evidenceDocument.RootElement inventoryDocument.RootElement "linux-x64"
        |> ignore)

[<Fact>]
let ``CompatibilityEvidence_StaleHash_Throws`` () =
    let json =
        """{"SchemaVersion":1,"Succeeded":true,"Scenarios":["""
        + String.concat
            ","
            [ for name in allScenarioNames ->
                  scenarioEntry name (if name = "CoreTrimmed" then "STALE" else "AA") ]
        + "]}"

    use evidenceDocument = parse json
    use inventoryDocument = parse packageInventory

    expectFailure "stale package hashes" (fun () ->
        assertCompatibilityEvidence evidenceDocument.RootElement inventoryDocument.RootElement "linux-x64"
        |> ignore)

// ---------------------------------------------------------------------------
// Fingerprints
// ---------------------------------------------------------------------------

[<Fact>]
let ``EquivalentSourceFingerprint_RequiresSchemaAlgorithmCountAndDigest`` () =
    let fingerprint (digest: string) =
        sprintf
            """{"schemaVersion":1,"algorithm":"sha256","fileCount":2,"digest":"%s"}"""
            digest

    use left = parse (fingerprint "abc")
    use right = parse (fingerprint "ABC")
    use different = parse (fingerprint "def")

    Assert.True(equivalentSourceFingerprint left.RootElement right.RootElement)
    Assert.False(equivalentSourceFingerprint left.RootElement different.RootElement)

[<Fact>]
let ``GetSourceFingerprint_IsDeterministicAndCaseInsensitiveOverAGitTree`` () =
    use temp = new TempDirectory()
    let root = temp.Path

    let git (arguments: string list) =
        let result = Proc.runCaptureIn (Some root) "git" arguments |> Async.RunSynchronously

        if result.ExitCode <> 0 then
            failwithf "git %s failed: %s" (String.Join(" ", arguments)) result.Stderr

    git [ "init"; "-q"; "-b"; "main" ]
    git [ "config"; "user.email"; "fixture@example.com" ]
    git [ "config"; "user.name"; "Fixture" ]
    File.WriteAllText(Path.Combine(root, "a.txt"), "hello", utf8NoBom)
    File.WriteAllText(Path.Combine(root, "Zeta.txt"), "world", utf8NoBom)
    git [ "add"; "-A" ]
    git [ "commit"; "-q"; "-m"; "fixture" ]

    let first = getSourceFingerprint root
    let second = getSourceFingerprint root

    Assert.Equal(serializeJson first, serializeJson second)

    use document = parse (serializeJson first)
    Assert.Equal(Some 2, propInt "fileCount" document.RootElement)
    Assert.Equal(64, (propText "digest" document.RootElement).Length)

// ---------------------------------------------------------------------------
// End-to-end failure text
// ---------------------------------------------------------------------------

[<Fact>]
let ``Main_AllChecksFail_WritesFailedReportAndNamesTheEvidenceReport`` () =
    use temp = new TempDirectory()
    let root = temp.Path
    File.WriteAllText(Path.Combine(root, "FunnySharp.slnx"), "<Solution />", utf8NoBom)
    Directory.CreateDirectory(Path.Combine(root, "artifacts")) |> ignore

    let output = Path.Combine(root, "artifacts", "release-evidence")
    let execution = Path.Combine(root, "artifacts", "release-evidence", "attempt")

    use stdout = new StringWriter()
    use stderr = new StringWriter()

    let exitCode =
        ReleaseVerify.mainWith
            stdout
            stderr
            root
            [ "--repository-root"
              root
              "--output-directory"
              output
              "--execution-evidence-directory"
              execution ]
            ReleaseVerify.defaultCollaborators

    Assert.Equal(1, exitCode)

    // The PowerShell failure line contains two literal backslashes before the
    // report name (`... See '<dir>\\release-evidence.md'.`).
    Assert.Equal(
        sprintf "Release evidence verification failed. See '%s\\\\release-evidence.md'." output,
        stderr.ToString().TrimEnd([| '\r'; '\n' |])
    )

    let report = File.ReadAllText(Path.Combine(output, "release-evidence.md"))
    Assert.Contains("Status: FAILED", report)
    Assert.Contains("## Failures", report)
    Assert.True(File.Exists(Path.Combine(output, "release-evidence.json")))

[<Fact>]
let ``Main_MissingRequiredArguments_ReturnsUsageFailure`` () =
    use stdout = new StringWriter()
    use stderr = new StringWriter()

    let exitCode =
        ReleaseVerify.mainWith stdout stderr Environment.CurrentDirectory [] ReleaseVerify.defaultCollaborators

    Assert.Equal(1, exitCode)
    Assert.Contains("usage:", stderr.ToString())
    Assert.Contains("parameters are required", stderr.ToString())

[<Fact>]
let ``XmlInventory_RejectsSummaryNodesWithoutAssemblyMetadata`` () =
    use temp = new TempDirectory()
    let path = Path.Combine(temp.Path, "Missing.dll.xml")
    File.WriteAllText(path, "<doc><members><member name=\"M:Fixture.Run\"><summary>Contract.</summary></member></members></doc>")
    expectFailure "assembly metadata" (fun () -> getXmlDocumentationInventory temp.Path [ path ] |> ignore)

[<Fact>]
let ``XmlInventory_RejectsDuplicateMemberIdentities`` () =
    use temp = new TempDirectory()
    let path = Path.Combine(temp.Path, "Duplicate.xml")
    File.WriteAllText(path, "<doc><members><member name=\"M:Fixture.Run\"><summary>Contract.</summary></member><member name=\"M:Fixture.Run\"><summary>Other.</summary></member></members></doc>")
    expectFailure "duplicate" (fun () -> getXmlDocumentationInventory temp.Path [ path ] |> ignore)
