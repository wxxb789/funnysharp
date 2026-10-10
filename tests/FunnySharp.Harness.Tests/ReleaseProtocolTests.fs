module FunnySharp.Harness.Tests.ReleaseProtocolTests

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
      "api-baseline"
      "docs"
      "benchmark-preflight"
      "benchmark"
      "performance-verify"
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
      "api-baseline"
      "docs"
      "compatibility" ]

let private loadProtocol () : ReleaseProtocol.Protocol =
    match ReleaseProtocol.readProtocol (Path.Combine(repositoryRoot (), ReleaseProtocol.ProtocolRelativePath)) with
    | Ok protocol -> protocol
    | Error err -> failwith err.Message

let private messageOf (result: Result<'T, HarnessError>) : string =
    match result with
    | Ok _ -> ""
    | Error err -> err.Message

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
    member _.ReleaseWorkflow_HasRequiredContextsAndShaPinnedActions() =
        let workflow =
            File.ReadAllText(Path.Combine(repositoryRoot (), ".github", "workflows", "release.yml"))

        for context in Ruleset.requiredContexts do
            Assert.Matches(
                Regex(@"^\s+name:\s*" + Regex.Escape context + @"\s*$", RegexOptions.Multiline),
                workflow
            )

        Assert.False(
            Regex.IsMatch(workflow, @"pull_request_target|continue-on-error:\s*true"),
            "privileged pull request execution or continue-on-error was found."
        )

        let unpinned =
            Regex(@"^\s*uses:\s+actions/[^@\s]+@(?![0-9a-f]{40}\s*(?:#|$))", RegexOptions.Multiline).Match workflow

        Assert.False(unpinned.Success, sprintf "unpinned action: '%s'." (unpinned.Value.Trim()))

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
