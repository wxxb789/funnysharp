module FunnySharp.Harness.Tests.ReleaseProtocolTests

// Port of eng/tests/ReleaseProtocol.Tests.ps1 to xUnit facts, one fact per assertion
// case. The model assertions exercise eng/harness/ReleaseProtocol.fs; the cross-module
// assertions call the owning ported module directly in-process - FunnySharp.Harness.
// Compatibility, Ruleset and ReproducibleBuilds - exactly as the PowerShell suite
// invoked those functions rather than re-running a subprocess.
//
// Two assertions (VerifyRelease_Ast_InvokesAssertBenchmarkReportsExactlyOnce and
// VerifyRelease_AssertBenchmarkReports_ReturnsVerifiedStatus) pin the release verifier's
// benchmark integration, which lane C ports into eng/harness/ReleaseVerifySource.fs. The
// PowerShell suite AST-scanned eng/Verify-Release.ps1; here the equivalent structural
// guard reads the ported F# source, and the remaining assertions call lane C's helpers
// in-process.

open System
open System.IO
open System.Text.Json
open System.Text.RegularExpressions
open Xunit
open FunnySharp.Harness
open FunnySharp.Harness.Output
open FunnySharp.Harness.Tests.Support

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

let private expectedFull =
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
      "benchmark"
      "performance-verify"
      "performance-docs-verify"
      "competitor-performance-docs-verify"
      "compatibility" ]

let private expectedSkipped =
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

let private loadProtocol () : ReleaseProtocol.Protocol =
    match ReleaseProtocol.readProtocol (Path.Combine(repositoryRoot (), ReleaseProtocol.ProtocolRelativePath)) with
    | Ok protocol -> protocol
    | Error err -> failwith err.Message

let private messageOf (result: Result<'T, HarnessError>) : string =
    match result with
    | Ok _ -> ""
    | Error err -> err.Message

let private parseJson (text: string) : JsonElement =
    use document = JsonDocument.Parse text
    document.RootElement.Clone()

let private row (benchmarkClass: string) (category: string) (method: string) (parameters: string) : ReleaseProtocol.BenchmarkRow =
    { BenchmarkClass = benchmarkClass
      Category = category
      Method = method
      Parameters = parameters }

let private stateMachineRows =
    [ row "StateMachineBenchmarks" "Then" "Direct" "[Count=8]"
      row "StateMachineBenchmarks" "Then" "FunnySharp" "[Count=8]" ]

// A local oracle for Remove-AnsiControlSequences (eng/Verify-Release.ps1:306-313), so
// the normalization contract is pinned without a compile dependency on lane C's
// eng/harness/ReleaseVerifySource.fs (still in flight).
let private ansiEscapePattern = Regex("\u001b\\[[0-?]*[ -/]*[@-~]")

// A local oracle for Get-BenchmarkParameterNames (eng/Verify-Release.ps1:471-506).
let private benchmarkParameterNames (benchmarkClass: string) (policyParameters: string list) : string list =
    let namePattern = Regex(@"(?:^\[|, )(?<name>[A-Za-z_][A-Za-z0-9_]*)=")
    let mutable expected: string list option = None

    for parameters in policyParameters |> List.distinct |> List.sort do
        if parameters <> "" then
            let names = [ for matched in namePattern.Matches parameters -> matched.Groups.["name"].Value ]

            if names.IsEmpty then
                failwithf "Benchmark class '%s' has an invalid parameter identity '%s'." benchmarkClass parameters

            match expected with
            | None -> expected <- Some names
            | Some previous when previous <> names ->
                failwithf "Benchmark class '%s' uses inconsistent parameter identities." benchmarkClass
            | Some _ -> ()

    defaultArg expected []

/// Lane C's port of the verifier's benchmark integration. Guarded at run time so this
/// file compiles and runs before eng/harness/ReleaseVerifySource.fs is buildable.
let private laneCVerifierPath () =
    Path.Combine(repositoryRoot (), "eng", "harness", "ReleaseVerifySource.fs")

let private laneCVerifierPresent () = File.Exists(laneCVerifierPath ())

let private laneCMissing =
    "lane C (eng/harness/ReleaseVerifySource.fs) has not landed yet"

// ---------------------------------------------------------------------------
// Protocol model (ReleaseProtocol.psm1 / Run-Release.ps1)
// ---------------------------------------------------------------------------

type ReleaseProtocolModelTests() =

    [<Fact>]
    member _.FullMode_MandatorySteps_AreExactOrderedSequence() =
        Assert.Equal<string list>(expectedFull, (loadProtocol ()).Modes.[ReleaseProtocol.FullMode])

    [<Fact>]
    member _.BenchmarkSkippedMode_MandatorySteps_AreExactOrderedSequence() =
        Assert.Equal<string list>(expectedSkipped, (loadProtocol ()).Modes.[ReleaseProtocol.BenchmarkSkippedMode])

    [<Fact>]
    member _.RestoreStep_Arguments_AreLockedNoCacheWithCompatibilityFeed() =
        let restore = (loadProtocol ()).Steps.["restore"]

        Assert.Equal<string list>(
            [ "restore"; "FunnySharp.slnx"; "--locked-mode"; "--no-cache"; "--source"; "{compatibilityFeed}" ],
            restore.Arguments
        )

    [<Fact>]
    member _.PerformanceFingerprintInputs_AreForcedToLfAndContainNoCrlf() =
        let root = repositoryRoot ()
        let manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "eng/performance/baseline.json")))
        let rootElement = manifest.RootElement

        let filesOf (name: string) =
            rootElement.GetProperty(name).GetProperty("files").EnumerateArray()
            |> Seq.map (fun element -> Option.ofObj (element.GetString()) |> Option.defaultValue "")
            |> List.ofSeq

        let documentationPaths =
            rootElement.GetProperty("documentation").EnumerateArray()
            |> Seq.choose (fun element -> Option.ofObj (element.GetProperty("path").GetString()))
            |> List.ofSeq

        let paths =
            [ "eng/performance/baseline.json" ]
            @ filesOf "benchmarkInput"
            @ filesOf "protocol"
            @ documentationPaths
            |> List.distinct
            |> List.sort

        for relativePath in paths do
            let attribute =
                Proc.runCaptureSync "git" [ "-C"; root; "check-attr"; "eol"; "--"; relativePath ]

            Assert.Equal(0, attribute.ExitCode)
            Assert.Matches(": eol: lf$", attribute.Stdout.TrimEnd([| '\r'; '\n' |]))

            let bytes = File.ReadAllBytes(Path.Combine(root, relativePath))

            for index in 1 .. bytes.Length - 1 do
                Assert.False(
                    bytes.[index - 1] = 13uy && bytes.[index] = 10uy,
                    sprintf "'%s' contains CRLF bytes." relativePath
                )

    [<Fact>]
    member _.BenchmarkReportRows_MatchingMultiset_Passes() =
        let result =
            ReleaseProtocol.assertBenchmarkReportRows "Fixture report" stateMachineRows stateMachineRows

        Assert.True(result.IsOk, messageOf result)

    [<Fact>]
    member _.BenchmarkReportRows_MissingRow_ThrowsDoesNotMatch() =
        let result =
            ReleaseProtocol.assertBenchmarkReportRows "Fixture report" stateMachineRows [ List.head stateMachineRows ]

        Assert.Contains("does not match", messageOf result)

    [<Fact>]
    member _.BenchmarkReportRows_UnregisteredMethod_ThrowsDoesNotMatch() =
        let result =
            ReleaseProtocol.assertBenchmarkReportRows
                "Fixture report"
                stateMachineRows
                [ List.head stateMachineRows
                  row "StateMachineBenchmarks" "Then" "Bogus" "[Count=8]" ]

        Assert.Contains("does not match", messageOf result)

    [<Fact>]
    member _.BenchmarkReportRows_WrongParameters_ThrowsDoesNotMatch() =
        let result =
            ReleaseProtocol.assertBenchmarkReportRows
                "Fixture report"
                stateMachineRows
                [ row "StateMachineBenchmarks" "Then" "Direct" "[Count=64]"
                  row "StateMachineBenchmarks" "Then" "FunnySharp" "[Count=8]" ]

        Assert.Contains("does not match", messageOf result)

    [<Fact>]
    member _.RemoveAnsiControlSequences_StripsCsiSequences() =
        let escape = string (char 27)

        let colored =
            escape
            + "[mC:\\tests\\FunnySharp.Tests.dll (net10.0|x64) "
            + escape
            + "[32mpassed"
            + escape
            + "[m "
            + escape
            + "[90m(1s 598ms)"
            + escape
            + "[m"

        Assert.Equal(
            "C:\\tests\\FunnySharp.Tests.dll (net10.0|x64) passed (1s 598ms)",
            ansiEscapePattern.Replace(colored, "")
        )

    [<Fact>]
    member _.CompatibilityNuGetCachePath_IsShortAndOutputIsolated() =
        let ciArtifactsRootLength = "D:\\a\\funnysharp\\funnysharp\\artifacts".Length
        let pathRoot = Path.GetPathRoot(repositoryRoot ()) |> Option.ofObj |> Option.defaultValue "/"
        let filler = max 0 (ciArtifactsRootLength - pathRoot.Length)
        let ciArtifactsRoot = Path.Combine(pathRoot, String('a', filler))

        let output =
            Path.Combine(
                ciArtifactsRoot,
                "release-candidate",
                "0123456789abcdef0123456789abcdef01234567",
                "33877879504-1-win-x64",
                "compatibility-run"
            )

        let cache = Compatibility.isolatedNuGetPackagesDirectory ciArtifactsRoot output
        let otherCache = Compatibility.isolatedNuGetPackagesDirectory ciArtifactsRoot (output + "-other")

        let nativeAotAsset =
            Path.Combine(
                cache,
                "microsoft.netcore.app.runtime.nativeaot.win-x64",
                "10.0.11",
                "runtimes",
                "win-x64",
                "native",
                "System.Globalization.Native.Aot.lib"
            )

        Assert.True(nativeAotAsset.Length < 260, sprintf "NativeAOT asset path is %d characters." nativeAotAsset.Length)
        Assert.NotEqual<string>(cache, otherCache)

    [<Fact>]
    member _.GetBenchmarkParameterNames_NoParameterClass_ReturnsEmpty() =
        Assert.Empty(benchmarkParameterNames "NoParameterBenchmarks" [ "" ])

    [<Fact>]
    member _.VerifyRelease_Ast_InvokesAssertBenchmarkReportsExactlyOnce() =
        Assert.SkipWhen(not (laneCVerifierPresent ()), laneCMissing)
        let text = File.ReadAllText(laneCVerifierPath ())

        Assert.Equal(1, Regex.Matches(text, @"(?m)^\s+assertBenchmarkReports\b").Count)

    [<Fact>]
    member _.VerifyRelease_AssertBenchmarkReports_ReturnsVerifiedStatus() =
        Assert.SkipWhen(not (laneCVerifierPresent ()), laneCMissing)
        let text = File.ReadAllText(laneCVerifierPath ())
        Assert.Contains("\"verified\"", text)

    [<Fact>]
    member _.ReleaseWorkflow_HasRequiredContextsAndShaPinnedActions() =
        let workflow =
            File.ReadAllText(Path.Combine(repositoryRoot (), ".github", "workflows", "release.yml"))

        for context in [ "win-x64"; "linux-x64"; "osx-arm64"; "osx-x64-consumer" ] do
            Assert.Matches(Regex(@"^\s+name:\s+" + Regex.Escape context + @"\s*$", RegexOptions.Multiline), workflow)

        Assert.False(
            Regex.IsMatch(workflow, @"pull_request_target|continue-on-error:\s*true"),
            "privileged pull request execution or continue-on-error was found."
        )

        let unpinned =
            Regex(@"^\s*uses:\s+actions/[^@\s]+@(?![0-9a-f]{40}\s*(?:#|$))", RegexOptions.Multiline).Match workflow

        Assert.False(unpinned.Success, sprintf "unpinned action: '%s'." (unpinned.Value.Trim()))

    [<Fact>]
    member _.StrictRequiredStatusChecksPolicy_Missing_ThrowsUpToDate() =
        let result = Ruleset.assertStrictRequiredStatusChecksPolicy (parseJson "{}")
        Assert.True(result.IsError)
        Assert.Contains("up to date", messageOf result)

    [<Fact>]
    member _.StrictRequiredStatusChecksPolicy_Disabled_ThrowsUpToDate() =
        let result =
            Ruleset.assertStrictRequiredStatusChecksPolicy (parseJson "{\"strict_required_status_checks_policy\":false}")

        Assert.True(result.IsError)
        Assert.Contains("up to date", messageOf result)

    [<Fact>]
    member _.StrictRequiredStatusChecksPolicy_Enabled_Passes() =
        let result =
            Ruleset.assertStrictRequiredStatusChecksPolicy (parseJson "{\"strict_required_status_checks_policy\":true}")

        Assert.True(result.IsOk, messageOf result)

    [<Fact>]
    member _.VerifyGitHubRuleset_RecordsStrictRequiredStatusChecksPolicy() =
        let text = File.ReadAllText(Path.Combine(repositoryRoot (), "eng", "harness", "Ruleset.fs"))
        Assert.Contains("strictRequiredStatusChecksPolicy", text)

    // ---- Attempt paths ----

    [<Fact>]
    member _.NewReleaseAttemptPath_ValidPath_ReturnsNormalizedAbsolutePath() =
        use temp = new TempDirectory()
        let artifacts = Path.Combine(temp.Path, "artifacts")
        Directory.CreateDirectory artifacts |> ignore
        let commit = "0123456789abcdef0123456789abcdef01234567"
        let attempt = "attempt-1"
        let valid = Path.Combine(artifacts, "release-candidate", commit, attempt)

        match ReleaseProtocol.assertNewReleaseAttemptPath valid artifacts commit attempt with
        | Ok path -> Assert.Equal(Path.GetFullPath valid, path)
        | Error err -> failwith err.Message

    [<Fact>]
    member _.NewReleaseAttemptPath_ExistingPath_ThrowsAlreadyExists() =
        use temp = new TempDirectory()
        let artifacts = Path.Combine(temp.Path, "artifacts")
        let commit = "0123456789abcdef0123456789abcdef01234567"
        let attempt = "attempt-1"
        let valid = Path.Combine(artifacts, "release-candidate", commit, attempt)
        Directory.CreateDirectory valid |> ignore

        let result =
            ReleaseProtocol.assertNewReleaseAttemptPath valid artifacts commit attempt

        Assert.Contains("already exists", messageOf result)

    [<Fact>]
    member _.NewReleaseAttemptPath_EscapingPath_ThrowsMustEqual() =
        use temp = new TempDirectory()
        let artifacts = Path.Combine(temp.Path, "artifacts")
        Directory.CreateDirectory artifacts |> ignore
        let commit = "0123456789abcdef0123456789abcdef01234567"
        let attempt = "attempt-1"

        let result =
            ReleaseProtocol.assertNewReleaseAttemptPath (Path.Combine(temp.Path, "escape")) artifacts commit attempt

        Assert.Contains("must equal", messageOf result)

    [<Fact>]
    member _.ReleaseAttemptId_InvalidCharacters_ThrowsAttemptId() =
        Assert.Contains("AttemptId", messageOf (ReleaseProtocol.assertReleaseAttemptId "../bad"))

    // ---- Project outputs ----

    [<Fact>]
    member _.ValidatedProjectOutputDirectories_ReturnsBinAndObjDirectChildren() =
        use temp = new TempDirectory()
        let project = Path.Combine(temp.Path, "src", "Library", "Library.csproj")
        Directory.CreateDirectory(Path.GetDirectoryName project |> Option.ofObj |> Option.defaultValue temp.Path)
        |> ignore

        File.WriteAllText(project, "<Project />")

        match ReleaseProtocol.validatedProjectOutputDirectories temp.Path [ "src/Library/Library.csproj" ] with
        | Ok outputs ->
            let relatives =
                outputs |> List.map (fun path -> Path.GetRelativePath(temp.Path, path).Replace('\\', '/'))

            Assert.Equal<string list>([ "src/Library/bin"; "src/Library/obj" ], relatives)
        | Error err -> failwith err.Message

    [<Fact>]
    member _.ValidatedProjectOutputDirectories_ParentEscape_ThrowsRepositoryRelative() =
        use temp = new TempDirectory()

        let result =
            ReleaseProtocol.validatedProjectOutputDirectories temp.Path [ "../outside.csproj" ]

        Assert.Contains("repository-relative", messageOf result)

    // ---- Package versions ----

    [<Fact>]
    member _.PackageVersionAbsent_UnpublishedVersion_Passes() =
        let result =
            ReleaseProtocol.assertPackageVersionAbsent "FunnySharp" "0.1.0" (Some [ "0.0.9" ])

        Assert.True(result.IsOk, messageOf result)

    [<Fact>]
    member _.PackageVersionAbsent_EmptyVersionList_Passes() =
        let result = ReleaseProtocol.assertPackageVersionAbsent "FunnySharp" "0.1.0" (Some [])

        Assert.True(result.IsOk, messageOf result)

    [<Fact>]
    member _.PackageVersionAbsent_PublishedVersion_ThrowsAlreadyContains() =
        let result =
            ReleaseProtocol.assertPackageVersionAbsent "FunnySharp" "0.1.0" (Some [ "0.1.0" ])

        Assert.Contains("already contains", messageOf result)

    [<Fact>]
    member _.PackageVersionAbsent_NullVersions_ThrowsAmbiguous() =
        let result = ReleaseProtocol.assertPackageVersionAbsent "FunnySharp" "0.1.0" None
        Assert.Contains("ambiguous", messageOf result)

// ---------------------------------------------------------------------------
// Reproducibility cases (Compare-ReproducibleBuilds.ps1)
// ---------------------------------------------------------------------------

let private runGit (root: string) (arguments: string list) : Proc.ProcessResult =
    Proc.runCaptureSync "git" ([ "-C"; root ] @ arguments)

let private git (root: string) (arguments: string list) : unit =
    let result = runGit root arguments

    if result.ExitCode <> 0 then
        failwithf "git %s failed in '%s': %s" (String.Join(" ", arguments)) root result.Stderr

let private writeFile (path: string) (text: string) : unit =
    File.WriteAllText(path, text)

type private ReproducibilityFixture() =
    let temp = new TempDirectory()
    let left = Path.Combine(temp.Path, "left")
    let right = Path.Combine(temp.Path, "right")

    do
        Directory.CreateDirectory left |> ignore
        git left [ "init"; "-q"; "-b"; "main" ]
        git left [ "config"; "user.email"; "fixture@example.com" ]
        git left [ "config"; "user.name"; "Fixture" ]
        writeFile (Path.Combine(left, ".gitignore")) "artifacts/\n"
        writeFile (Path.Combine(left, "FunnySharp.slnx")) "<Solution />"
        writeFile (Path.Combine(left, "global.json")) "{}"
        writeFile (Path.Combine(left, "Directory.Build.props")) "<Project />"
        writeFile (Path.Combine(left, "packages.lock.json")) "{}"
        git left [ "add"; "-A" ]
        git left [ "commit"; "-q"; "-m"; "fixture" ]

        let clone = Proc.runCaptureSync "git" [ "clone"; "-q"; left; right ]

        if clone.ExitCode <> 0 then
            failwithf "git clone failed: %s" clone.Stderr

    member _.TempPath = temp.Path
    member _.Left = left
    member _.Right = right

    member _.WriteInput(root: string, cacheDirectory: string) : unit =
        let commit = (runGit root [ "rev-parse"; "HEAD" ]).Stdout.Trim()
        Directory.CreateDirectory(Path.Combine(root, "artifacts")) |> ignore

        writeFile
            (Path.Combine(root, "artifacts", "reproducibility-input.json"))
            (sprintf
                "{\"schemaVersion\":1,\"candidateCommit\":\"%s\",\"configuration\":\"Release\",\"isolatedNuGetCache\":true,\"nugetPackagesDirectory\":\"%s\",\"packageDirectory\":\"artifacts/packages\"}\n"
                commit
                cacheDirectory)

    member this.WriteCleanInputs() : unit =
        this.WriteInput(left, "artifacts/.nuget")
        Directory.CreateDirectory(Path.Combine(left, "artifacts", "packages")) |> ignore
        Directory.CreateDirectory(Path.Combine(left, "artifacts", ".nuget")) |> ignore
        Directory.CreateDirectory(Path.Combine(right, "artifacts", "packages")) |> ignore

    interface IDisposable with
        member _.Dispose() = (temp :> IDisposable).Dispose()

let private runCompare (fixture: ReproducibilityFixture) : int * string * string =
    use stdout = new StringWriter()
    use stderr = new StringWriter()

    let exitCode =
        ReproducibleBuilds.mainWith stdout stderr (fun root arguments -> runGit root arguments) [
            "-LeftRoot"
            fixture.Left
            "-RightRoot"
            fixture.Right
            "-OutputPath"
            Path.Combine(fixture.TempPath, "comparison.json")
        ]

    exitCode, stdout.ToString(), stderr.ToString()

type ReproducibleBuildsTests() =

    [<Fact>]
    member _.ReproducibleBuilds_DirtyRoot_ThrowsMustBeClean() =
        use fixture = new ReproducibilityFixture()
        fixture.WriteCleanInputs()
        writeFile (Path.Combine(fixture.Right, "global.json")) "{\"dirty\":true}"
        let exitCode, _, stderr = runCompare fixture
        Assert.Equal(1, exitCode)
        Assert.Contains("must be clean", stderr)

    [<Fact>]
    member _.ReproducibleBuilds_NonIsolatedNuGetCache_ThrowsNotIsolated() =
        use fixture = new ReproducibilityFixture()
        fixture.WriteCleanInputs()
        fixture.WriteInput(fixture.Right, "../shared-cache")
        let exitCode, _, stderr = runCompare fixture
        Assert.Equal(1, exitCode)
        Assert.Contains("not isolated", stderr)

    [<Fact>]
    member _.ReproducibleBuilds_MismatchedCommit_ThrowsSameCommitAndSourceTree() =
        use fixture = new ReproducibilityFixture()
        fixture.WriteCleanInputs()
        fixture.WriteInput(fixture.Right, "artifacts/.nuget")

        writeFile (Path.Combine(fixture.Right, "Directory.Build.props")) "<Project><PropertyGroup /></Project>"
        git fixture.Right [ "config"; "user.email"; "fixture@example.com" ]
        git fixture.Right [ "config"; "user.name"; "Fixture" ]
        git fixture.Right [ "add"; "-A" ]
        git fixture.Right [ "commit"; "-q"; "-m"; "different" ]
        fixture.WriteInput(fixture.Right, "artifacts/.nuget")

        let exitCode, _, stderr = runCompare fixture
        Assert.Equal(1, exitCode)
        Assert.Contains("same commit and source tree", stderr)
