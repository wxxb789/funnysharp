module FunnySharp.Harness.Tests.ReleaseProtocolTests

// Port of eng/tests/ReleaseProtocol.Tests.ps1 to xUnit facts, one fact per assertion
// case. The model assertions exercise eng/harness/ReleaseProtocol.fs; the cross-module
// assertions call the owning ported module directly in-process - FunnySharp.Harness.
// Compatibility and workflow controls. Ruleset and reproducibility duplicates
// are owned by their focused suites, which call the same production functions.
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

let private row (benchmarkClass: string) (category: string) (method: string) (parameters: string) : ReleaseProtocol.BenchmarkRow =
    { BenchmarkClass = benchmarkClass
      Category = category
      Method = method
      Parameters = parameters }

let private stateMachineRows =
    [ row "StateMachineBenchmarks" "Then" "Direct" "[Count=8]"
      row "StateMachineBenchmarks" "Then" "FunnySharp" "[Count=8]" ]

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
        use manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "eng/performance/baseline.json")))
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
                Regex(@"^\s+name:.*\|\| '" + Regex.Escape context + @"' \}\}\s*$", RegexOptions.Multiline),
                workflow
            )

        Assert.Contains("'provenance transport / win-x64'", workflow)
        Assert.Contains("'provenance transport / linux-x64'", workflow)
        Assert.Contains("'provenance transport / osx-arm64'", workflow)
        Assert.Contains("'provenance transport / osx-x64-consumer'", workflow)

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
// Workflow provenance input transport (machine configuration and shell behavior)
// ---------------------------------------------------------------------------

let private releaseWorkflow () =
    File.ReadAllText(Path.Combine(repositoryRoot (), ".github", "workflows", "release.yml")).Replace("\r\n", "\n")

let private workflowStep (workflow: string) id =
    Regex.Split(workflow, @"(?m)^      - ")
    |> Array.filter (fun block -> Regex.IsMatch(block, @"(?m)^        id: " + Regex.Escape id + "$"))
    |> Assert.Single

let private workflowScript (step: string) =
    Assert.Matches(@"(?m)^        shell: bash$", step)
    let matched = Regex.Match(step, @"(?m)^        run: ([|>])\n((?:          [^\n]*(?:\n|$))+)" )
    Assert.True(matched.Success, "Missing executable workflow run block.")
    let lines = matched.Groups.[2].Value.TrimEnd('\n').Split('\n') |> Array.map (fun line -> line.Substring 10)
    let script = String.Join((if matched.Groups.[1].Value = "|" then "\n" else " "), lines)
    Assert.DoesNotContain("${{", script)
    script

let private runWorkflowScript directory script (environment: (string * string) list) = task {
    use proc = new System.Diagnostics.Process()
    // Resolve PATH explicitly: Windows otherwise prefers System32/bash.exe (WSL).
    let bash =
        if OperatingSystem.IsWindows() then
            let path = Environment.GetEnvironmentVariable "PATH" |> Option.ofObj |> Option.defaultWith (fun () -> failwith "PATH is required to locate Bash.")
            path.Split(Path.PathSeparator)
            |> Seq.map (fun directory -> Path.Combine(directory, "bash.exe"))
            |> Seq.find File.Exists
        else "bash"
    proc.StartInfo <- System.Diagnostics.ProcessStartInfo(bash, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = directory)
    for argument in [ "--noprofile"; "--norc"; "-e"; "-o"; "pipefail"; "-c"; script ] do
        proc.StartInfo.ArgumentList.Add argument
    for key, value in environment do proc.StartInfo.Environment.[key] <- value
    Assert.True(proc.Start())
    use timeout = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds 15.)
    let stdout = proc.StandardOutput.ReadToEndAsync()
    let stderr = proc.StandardError.ReadToEndAsync()
    try
        do! proc.WaitForExitAsync timeout.Token
        let! output = stdout
        let! error = stderr
        return proc.ExitCode, output, error
    with ex ->
        if not proc.HasExited then
            use cleanupTimeout = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds 15.)
            let exited = proc.WaitForExitAsync cleanupTimeout.Token
            proc.Kill(true)
            do! exited
        let! _ = stdout
        let! _ = stderr
        return! System.Threading.Tasks.Task.FromException<_>(ex)
}

type ReleaseWorkflowProvenanceTests() =

    [<Fact>]
    member _.FileCheckoutIsSameRepositoryPinnedAndGuardedBeforeExecution() =
        let workflow = releaseWorkflow ()
        Assert.Matches(@"(?m)^      provenance_input_ref:\n(?:        [^\n]*\n)*        type: string\n        required: false$", workflow)
        let provenance = workflow.Substring(workflow.IndexOf("\n  provenance:\n", StringComparison.Ordinal))
        Assert.Matches(@"(?m)^      RELEASE_PROVENANCE_PAYLOAD: \$\{\{ inputs.provenance_payload \}\}$", provenance)
        Assert.Matches(@"(?m)^      PROVENANCE_INPUT_REF: \$\{\{ inputs.provenance_input_ref \}\}$", provenance)
        let guard = workflowStep provenance "provenance-input"
        let checkout = workflowStep provenance "provenance-input-checkout"
        let run = workflowStep provenance "provenance-run"
        Assert.DoesNotMatch(@"(?m)^        (?:if|continue-on-error):", guard)
        Assert.DoesNotMatch(@"(?m)^        (?:if|continue-on-error):", run)
        Assert.Matches(@"(?m)^        if: \$\{\{ steps.provenance-input.outputs.source == 'file' \}\}$", checkout)
        Assert.Matches(@"(?m)^        uses: actions/checkout@" + checkoutSha + @"(?:\s+#.*)?$", checkout)
        Assert.Matches(@"(?m)^          repository: \$\{\{ github.repository \}\}$", checkout)
        Assert.Matches(@"(?m)^          ref: \$\{\{ steps.provenance-input.outputs.ref \}\}$", checkout)
        Assert.Matches(@"(?m)^          path: artifacts/provenance-input$", checkout)
        Assert.Matches(@"(?m)^          persist-credentials: false$", checkout)
        Assert.Matches(@"(?m)^          PROVENANCE_INPUT_SOURCE: \$\{\{ steps.provenance-input.outputs.source \}\}$", run)
        Assert.Matches(@"(?m)^          PROVENANCE_MODE: \$\{\{ inputs.provenance_mode \}\}$", run)
        Assert.True(provenance.IndexOf(guard, StringComparison.Ordinal) < provenance.IndexOf(checkout, StringComparison.Ordinal))
        Assert.True(provenance.IndexOf(checkout, StringComparison.Ordinal) < provenance.IndexOf(run, StringComparison.Ordinal))
        let checkouts = Regex.Matches(provenance, @"(?m)^        uses: actions/checkout@")
        Assert.Equal(2, checkouts.Count)
        Assert.True(checkouts.[0].Index < provenance.IndexOf(guard, StringComparison.Ordinal))
        Assert.Contains("          ref: ${{ env.CANDIDATE_SHA }}", provenance.Substring(0, provenance.IndexOf(guard, StringComparison.Ordinal)))
        let ignored = Proc.runCaptureSync "git" [ "-C"; repositoryRoot (); "check-ignore"; "artifacts/provenance-input/input.json" ]
        Assert.Equal(0, ignored.ExitCode)

    [<Theory>]
    [<InlineData("inline", "opaque-inline", "", 0)>]
    [<InlineData("file", "", "0123456789abcdef0123456789abcdef01234567", 0)>]
    [<InlineData("", "", "", 1)>]
    [<InlineData("", "payload", "0123456789abcdef0123456789abcdef01234567", 1)>]
    [<InlineData("", "", "main", 1)>]
    [<InlineData("", "", "refs/heads/main", 1)>]
    [<InlineData("", "", "0123456", 1)>]
    [<InlineData("", "", "0123456789ABCDEF0123456789ABCDEF01234567", 1)>]
    [<InlineData("", "", "g123456789abcdef0123456789abcdef01234567", 1)>]
    [<InlineData("", "", "0123456789abcdef0123456789abcdef012345678", 1)>]
    [<InlineData("", "", "0123456789abcdef0123456789abcdef01234567\n", 1)>]
    [<InlineData("", "", "$(printf injected)", 1)>]
    member _.SourceSelectionExecutesWorkflowGuard(source: string, payload: string, reference: string, expectedExit: int) = task {
        use temp = new TempDirectory()
        let outputPath = Path.Combine(temp.Path, "github-output")
        let script = workflowStep (releaseWorkflow ()) "provenance-input" |> workflowScript
        let! code, stdout, error = runWorkflowScript temp.Path script [ "RELEASE_PROVENANCE_PAYLOAD", payload; "PROVENANCE_INPUT_REF", reference; "GITHUB_OUTPUT", outputPath.Replace('\\', '/') ]
        Assert.True((expectedExit = code), sprintf "Expected exit %d, got %d: %s" expectedExit code error)
        Assert.Empty stdout
        let outputs = if File.Exists outputPath then File.ReadAllLines outputPath |> Array.toList else []
        Assert.Equal<string list>((if source = "inline" then [ "source=inline" ] elif source = "file" then [ "source=file"; "ref=" + reference ] else []), outputs)
    }

    [<Theory>]
    [<InlineData("inline", "freeze-producer")>]
    [<InlineData("file", "freeze-producer")>]
    [<InlineData("inline", "stage-attestation")>]
    [<InlineData("file", "stage-attestation")>]
    member _.SelectedRoutePassesExactHarnessArguments(source: string, mode: string) = task {
        use temp = new TempDirectory()
        let script = workflowStep (releaseWorkflow ()) "provenance-run" |> workflowScript
        let! code, stdout, error = runWorkflowScript temp.Path ("dotnet() { printf '%s\\0' \"$@\"; }\n" + script) [ "PROVENANCE_INPUT_SOURCE", source; "PROVENANCE_MODE", mode; "PROVENANCE_DIRECTORY", "artifacts/output with spaces" ]
        Assert.True((code = 0), error)
        let expected = [ "fsi"; "build.fsx"; "--"; "-p"; "release-provenance"; "-Mode"; mode; "-OutputDirectory"; "artifacts/output with spaces" ] @ (if source = "file" then [ "-InputPath"; "artifacts/provenance-input/input.json" ] else [])
        Assert.Equal<string list>(expected, stdout.TrimEnd('\000').Split('\000') |> Array.toList)
    }

    [<Fact>]
    member _.FileInputReadsBeyondDispatchLimitWithoutPayloadEnvironment() = task {
        use temp = new TempDirectory()
        use stdout = new StringWriter()
        use stderr = new StringWriter()
        let requested = ResizeArray<string>()
        let environment = ResizeArray<string>()
        let inputPath = Path.Combine(temp.Path, "input.json")
        let output = Path.Combine(temp.Path, "output")
        let input = JsonSerializer.Serialize(dict [ "repository", box "owner/repo"; "producer", box (dict [ "artifactId", box 123; "entry", box "P.json"; "sha256", box (String.replicate 64 "a") ]); "attestation", box (dict [ "base64", box (Convert.ToBase64String(Array.zeroCreate<byte> 65536)); "sha256", box (String.replicate 64 "b") ]) ])
        File.WriteAllText(inputPath, input)
        Assert.True(FileInfo(inputPath).Length > 65535L)
        let collaborators: ReleaseProvenance.Collaborators =
            { Get = fun url -> requested.Add url; System.Threading.Tasks.Task.FromResult { StatusCode = 404; EffectiveUrl = url; Body = [||] }
              UtcNow = fun () -> DateTimeOffset.Parse "2026-10-03T12:00:00Z"
              Environment = fun name -> environment.Add name; None }
        let! code = ReleaseProvenance.mainWith stdout stderr collaborators [ "-Mode"; "stage-attestation"; "-InputPath"; inputPath; "-OutputDirectory"; output ]
        Assert.Equal(1, code)
        Assert.Equal<string list>([ "https://api.github.com/repos/owner/repo/actions/artifacts/123" ], requested |> Seq.toList)
        Assert.Empty environment
        Assert.False(File.Exists(Path.Combine(output, "P.json")))
        Assert.False(File.Exists(Path.Combine(output, "A.json")))
    }
