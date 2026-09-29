module FunnySharp.Harness.Tests.CompatibilityTests

// Behaviour tests for FunnySharp.Harness.Compatibility, the F# port of
// tests/FunnySharp.Compatibility/Run-Compatibility.ps1. The suite never runs the
// real SDK: a fake process runner stands in for `dotnet restore`/`publish` and
// for the published app, so the CLI validation, the NuGet.Config/NuGet cache
// layout, the scenario wiring and the evidence JSON are exercised
// deterministically and without network.

open System
open System.Globalization
open System.IO
open System.IO.Compression
open System.Text.Json
open Xunit
open FunnySharp.Harness.Compatibility
open FunnySharp.Harness.Tests.Support

// ---- fixture helpers ----

let private writePackage (path: string) (entryPath: string) (bytes: byte[]) : unit =
    use stream = File.Create path
    use archive = new ZipArchive(stream, ZipArchiveMode.Create)
    let entry = archive.CreateEntry entryPath
    use entryStream = entry.Open()
    entryStream.Write(bytes, 0, bytes.Length)

let private flagValue (name: string) (args: string list) : string =
    let rec loop (remaining: string list) =
        match remaining with
        | flag :: value :: _ when flag = name -> value
        | _ :: rest -> loop rest
        | [] -> failwithf "missing flag %s" name

    loop args

let private runFrom (startDirectory: string) (argv: string list) : int * string * string =
    use stdout = new StringWriter()
    use stderr = new StringWriter()
    let exitCode = mainWith stdout stderr startDirectory argv
    exitCode, stdout.ToString(), stderr.ToString()

// ---- typed view of the evidence JSON ----

type private ScenarioView =
    { Scenario: string
      Outcome: string
      StartedAtUtc: string
      FinishedAtUtc: string
      Project: string
      PublishProperties: string list
      CorePackageSha256: string
      CoreAssemblySha256: string
      PublishedCoreAssemblySha256: string option
      PublishedAspNetCoreAssemblySha256: string option
      Error: string option }

type private EvidenceView =
    { SchemaVersion: int
      Succeeded: bool
      RuntimeIdentifier: string
      CorePackageSha256: string
      AspNetCorePackageSha256: string
      CoreAssemblySha256: string
      AspNetCoreAssemblySha256: string
      ScenarioNames: string list
      Scenarios: ScenarioView list
      TopLevelKeys: string list }

let private text (element: JsonElement) : string =
    match element.ValueKind with
    | JsonValueKind.Null -> ""
    | _ -> element.GetString() |> Option.ofObj |> Option.defaultValue ""

let private optionalText (element: JsonElement) : string option =
    match element.ValueKind with
    | JsonValueKind.Null -> None
    | _ -> element.GetString() |> Option.ofObj

let private readEvidence (path: string) : EvidenceView =
    use document = JsonDocument.Parse(File.ReadAllText path)
    let root = document.RootElement

    let scenarios =
        root.GetProperty("Scenarios").EnumerateArray()
        |> Seq.map (fun scenario ->
            { Scenario = text (scenario.GetProperty "Scenario")
              Outcome = text (scenario.GetProperty "Outcome")
              StartedAtUtc = text (scenario.GetProperty "StartedAtUtc")
              FinishedAtUtc = text (scenario.GetProperty "FinishedAtUtc")
              Project = text (scenario.GetProperty "Project")
              PublishProperties =
                  scenario.GetProperty("PublishProperties").EnumerateArray()
                  |> Seq.map text
                  |> List.ofSeq
              CorePackageSha256 = text (scenario.GetProperty "CorePackageSha256")
              CoreAssemblySha256 = text (scenario.GetProperty "CoreAssemblySha256")
              PublishedCoreAssemblySha256 = optionalText (scenario.GetProperty "PublishedCoreAssemblySha256")
              PublishedAspNetCoreAssemblySha256 =
                  optionalText (scenario.GetProperty "PublishedAspNetCoreAssemblySha256")
              Error = optionalText (scenario.GetProperty "Error") })
        |> Seq.toList

    { SchemaVersion = root.GetProperty("SchemaVersion").GetInt32()
      Succeeded = root.GetProperty("Succeeded").GetBoolean()
      RuntimeIdentifier = text (root.GetProperty "RuntimeIdentifier")
      CorePackageSha256 = text (root.GetProperty "CorePackageSha256")
      AspNetCorePackageSha256 = text (root.GetProperty "AspNetCorePackageSha256")
      CoreAssemblySha256 = text (root.GetProperty "CoreAssemblySha256")
      AspNetCoreAssemblySha256 = text (root.GetProperty "AspNetCoreAssemblySha256")
      ScenarioNames = scenarios |> List.map (fun scenario -> scenario.Scenario)
      Scenarios = scenarios
      TopLevelKeys = root.EnumerateObject() |> Seq.map (fun property -> property.Name) |> List.ofSeq }

// ---- fake process runner standing in for the SDK ----

type private FakeRunner(coreBytes: byte[], aspBytes: byte[]) =
    let calls = ResizeArray<string list>()
    let environments = ResizeArray<(string * string) list>()

    member val CoreBytes = coreBytes with get, set
    member val AspNetCoreScenarioCoreBytes = coreBytes with get, set
    member val AspNetCoreBytes = aspBytes with get, set
    member val PublishFailureProject: string option = None with get, set
    member val AppExitCode = 0 with get, set

    member _.Calls: string list list = List.ofSeq calls
    member _.Environments: (string * string) list list = List.ofSeq environments

    member this.Run
        (_workingDirectory: string)
        (environment: (string * string) list)
        (command: string list)
        : int =
        calls.Add command
        environments.Add environment

        match command with
        | "dotnet" :: "restore" :: _ -> 0
        | "dotnet" :: "publish" :: rest ->
            let project = List.head rest

            match this.PublishFailureProject with
            | Some fragment when project.Contains fragment -> 1
            | _ ->
                let output = flagValue "--output" rest
                Directory.CreateDirectory output |> ignore
                let isAspNetCoreProject = project.Contains "AspNetCore"

                let assemblyName =
                    if isAspNetCoreProject then
                        "FunnySharp.Compatibility.AspNetCore"
                    else
                        "FunnySharp.Compatibility.Core"

                let coreBytesForProject =
                    if isAspNetCoreProject then this.AspNetCoreScenarioCoreBytes else this.CoreBytes

                File.WriteAllBytes(Path.Combine(output, "FunnySharp.dll"), coreBytesForProject)

                if isAspNetCoreProject then
                    File.WriteAllBytes(Path.Combine(output, "FunnySharp.AspNetCore.dll"), this.AspNetCoreBytes)

                File.WriteAllBytes(Path.Combine(output, assemblyName), [| 0uy |])
                0
        | [ _application ] -> this.AppExitCode
        | _ -> 0

// ---- fixture ----

type CompatibilityRunnerTests() =
    let originalRunner = processRunner
    let originalHostOsSupported = hostOsSupported
    let temp = new TempDirectory()
    let root = Path.Combine(temp.Path, "repo")
    let packages = Path.Combine(root, "packages")
    let artifacts = Path.Combine(root, "artifacts")
    let coreBytes = [| 1uy; 3uy; 5uy; 7uy |]
    let aspBytes = [| 2uy; 4uy; 6uy; 8uy |]
    let runner = new FakeRunner(coreBytes, aspBytes)

    do
        Directory.CreateDirectory root |> ignore
        Directory.CreateDirectory packages |> ignore
        Directory.CreateDirectory artifacts |> ignore
        writePackage (Path.Combine(packages, "FunnySharp.0.1.0.nupkg")) "lib/net10.0/FunnySharp.dll" coreBytes

        writePackage
            (Path.Combine(packages, "FunnySharp.AspNetCore.0.1.0.nupkg"))
            "lib/net10.0/FunnySharp.AspNetCore.dll"
            aspBytes

        processRunner <- runner.Run

    interface IDisposable with
        member _.Dispose() =
            processRunner <- originalRunner
            hostOsSupported <- originalHostOsSupported
            (temp :> IDisposable).Dispose()

    member private _.OutputDirectory = Path.Combine(artifacts, "compat-run")

    member private _.BaseArgs =
        [ "-RepositoryRoot"; root
          "-PackageDirectory"; packages
          "-OutputDirectory"; Path.Combine(artifacts, "compat-run")
          "-RuntimeIdentifier"; hostRuntimeIdentifier ]

    member private _.Evidence =
        readEvidence (Path.Combine(Path.Combine(artifacts, "compat-run"), "compatibility-results.json"))

    // ---- scenario definitions ----

    [<Fact>]
    member _.ScenarioDefinitionsCoverEveryAcceptedName() =
        let definitions = scenarioDefinitions root
        Assert.Equal(6, definitions.Count)

        Assert.Equal<string list>(
            [ "AspNetCoreNativeAot"
              "AspNetCoreSmoke"
              "AspNetCoreTrimmed"
              "CoreNativeAot"
              "CoreSmoke"
              "CoreTrimmed" ],
            definitions |> Map.toList |> List.map fst |> List.sort
        )

        let coreProject =
            Path.Combine(
                root,
                "tests",
                "FunnySharp.Compatibility",
                "FunnySharp.Compatibility.Core",
                "FunnySharp.Compatibility.Core.csproj"
            )

        Assert.Equal(coreProject, definitions.["CoreTrimmed"].Project)
        Assert.Equal("FunnySharp.Compatibility.Core", definitions.["CoreTrimmed"].AssemblyName)

        Assert.Equal<string list>(
            [ "-p:PublishTrimmed=true"; "-p:TrimMode=full"; "-p:RootShippingAssemblies=true" ],
            definitions.["CoreTrimmed"].PublishProperties
        )

        Assert.Contains(definitions.["CoreNativeAot"].PublishProperties, fun property -> property = "-p:PublishAot=true")

    [<Fact>]
    member this.DefaultScenariosAreTheFourTrimAndAotScenarios() =
        let exitCode, _, _ = runFrom root this.BaseArgs
        Assert.Equal(0, exitCode)

        let evidence = this.Evidence

        Assert.Equal<string list>(
            [ "CoreTrimmed"; "CoreNativeAot"; "AspNetCoreTrimmed"; "AspNetCoreNativeAot" ],
            evidence.ScenarioNames
        )

    [<Fact>]
    member this.CommaSeparatedScenariosRunInOrder() =
        let exitCode, _, _ = runFrom root (this.BaseArgs @ [ "-Scenario"; "CoreSmoke,AspNetCoreSmoke" ])
        Assert.Equal(0, exitCode)
        Assert.Equal<string list>([ "CoreSmoke"; "AspNetCoreSmoke" ], this.Evidence.ScenarioNames)

    // ---- argument validation ----

    [<Fact>]
    member this.UnknownScenarioIsUsageFailure() =
        let exitCode, _, stderr = runFrom root (this.BaseArgs @ [ "-Scenario"; "CoreSmoke,Nonexistent" ])
        Assert.Equal(2, exitCode)
        Assert.Contains("Unknown compatibility scenario 'Nonexistent'.", stderr)

    [<Fact>]
    member this.OutputDirectoryOutsideArtifactsIsUsageFailure() =
        let outside = Path.Combine(temp.Path, "outside")

        let exitCode, _, stderr =
            runFrom
                root
                [ "-RepositoryRoot"; root
                  "-PackageDirectory"; packages
                  "-OutputDirectory"; outside
                  "-RuntimeIdentifier"; hostRuntimeIdentifier ]

        Assert.Equal(2, exitCode)
        Assert.Contains("Path must be a proper subdirectory of", stderr)

    [<Fact>]
    member this.ArtifactsRootIsNotAProperSubdirectory() =
        let exitCode, _, stderr =
            runFrom
                root
                [ "-RepositoryRoot"; root
                  "-PackageDirectory"; packages
                  "-OutputDirectory"; artifacts
                  "-RuntimeIdentifier"; hostRuntimeIdentifier ]

        Assert.Equal(2, exitCode)
        Assert.Contains("Path must be a proper subdirectory of", stderr)

    [<Fact>]
    member this.WrongRuntimeIdentifierIsUsageFailure() =
        let otherRid = if hostRuntimeIdentifier = "linux-x64" then "win-x64" else "linux-x64"

        let exitCode, _, stderr =
            runFrom
                root
                [ "-RepositoryRoot"; root
                  "-PackageDirectory"; packages
                  "-OutputDirectory"; this.OutputDirectory
                  "-RuntimeIdentifier"; otherRid ]

        Assert.Equal(2, exitCode)
        Assert.Contains("must match the runnable host RID", stderr)

    [<Fact>]
    member this.MissingPackageDirectoryIsUsageFailure() =
        let missing = Path.Combine(temp.Path, "absent")

        let exitCode, _, stderr =
            runFrom
                root
                [ "-RepositoryRoot"; root
                  "-PackageDirectory"; missing
                  "-OutputDirectory"; this.OutputDirectory
                  "-RuntimeIdentifier"; hostRuntimeIdentifier ]

        Assert.Equal(2, exitCode)
        Assert.Contains("Package directory does not exist: " + missing, stderr)

    [<Fact>]
    member this.UnsupportedHostOperatingSystemIsUsageFailure() =
        hostOsSupported <- false
        let exitCode, _, stderr = runFrom root this.BaseArgs
        Assert.Equal(2, exitCode)
        Assert.Contains("Unsupported host operating system.", stderr)

    [<Fact>]
    member _.MissingPackageIsUsageFailure() =
        let packagesOnlyCore = Path.Combine(Path.GetTempPath(), "funnysharp-compat-" + Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory packagesOnlyCore |> ignore
        File.Copy(Path.Combine(packages, "FunnySharp.0.1.0.nupkg"), Path.Combine(packagesOnlyCore, "FunnySharp.0.1.0.nupkg"))

        try
            let exitCode, _, stderr =
                runFrom
                    root
                    [ "-RepositoryRoot"; root
                      "-PackageDirectory"; packagesOnlyCore
                      "-OutputDirectory"; Path.Combine(artifacts, "missing-package-run")
                      "-RuntimeIdentifier"; hostRuntimeIdentifier ]

            Assert.Equal(2, exitCode)
            Assert.Contains("Expected exactly one FunnySharp.AspNetCore package", stderr)
            Assert.Contains("but found 0.", stderr)
        finally
            Directory.Delete(packagesOnlyCore, true)

    // ---- NuGet config and isolated cache ----

    [<Fact>]
    member this.WritesNuGetConfigWithoutBomAndMapsLocalFeedFirst() =
        let exitCode, _, _ = runFrom root (this.BaseArgs @ [ "-Scenario"; "CoreSmoke" ])
        Assert.Equal(0, exitCode)

        let configPath = Path.Combine(this.OutputDirectory, "NuGet.Config")
        let bytes = File.ReadAllBytes configPath
        Assert.Equal(int '<', int bytes.[0])
        Assert.Equal(int '\n', int bytes.[bytes.Length - 1])
        let content = File.ReadAllText configPath
        Assert.Contains("<clear />", content)
        Assert.Contains("key=\"local-release-packages\" value=\"" + packages.Replace("&", "&amp;") + "\"", content)
        Assert.Contains("key=\"upstream\" value=\"https://api.nuget.org/v3/index.json\"", content)
        Assert.Contains("<package pattern=\"FunnySharp\" />", content)
        Assert.Contains("<package pattern=\"FunnySharp.AspNetCore\" />", content)
        Assert.Contains("<package pattern=\"NETStandard.Library\" />", content)

        let localIndex = content.IndexOf("local-release-packages", StringComparison.Ordinal)
        let upstreamIndex = content.IndexOf("key=\"upstream\"", StringComparison.Ordinal)
        Assert.True(localIndex < upstreamIndex)

    [<Fact>]
    member this.IsolatedNuGetCacheIsShortAndOutputSpecific() =
        let cache = isolatedNuGetPackagesDirectory artifacts this.OutputDirectory
        let other = isolatedNuGetPackagesDirectory artifacts (this.OutputDirectory + "-other")
        Assert.NotEqual<string>(cache, other)
        Assert.StartsWith(Path.Combine(artifacts, ".nuget-packages"), cache)

        let key = fileName cache
        Assert.Equal(16, key.Length)
        Assert.Equal(key.ToLowerInvariant(), key)
        Assert.True(key |> Seq.forall (fun ch -> Char.IsAsciiHexDigit ch))

        let nativeAotAsset =
            Path.Combine(cache, "microsoft.netcore.app.runtime.nativeaot.win-x64", "10.0.11", "runtimes", "win-x64", "native", "System.Globalization.Native.Aot.lib")

        Assert.True(nativeAotAsset.Length < 260, sprintf "NativeAOT asset path is %d characters" nativeAotAsset.Length)

    [<Fact>]
    member this.RestoreAndPublishUseIsolatedCacheAndRedirectedPaths() =
        let outputDirectory = Path.Combine(artifacts, "compat-run")
        let exitCode, _, _ = runFrom root (this.BaseArgs @ [ "-Scenario"; "CoreSmoke" ])
        Assert.Equal(0, exitCode)

        let expectedCache = isolatedNuGetPackagesDirectory (Path.GetFullPath artifacts) (Path.GetFullPath outputDirectory)
        Assert.NotEmpty runner.Environments

        for environment in runner.Environments do
            Assert.Equal(expectedCache, List.find (fun (key, _) -> key = "NUGET_PACKAGES") environment |> snd)

        let restore =
            runner.Calls
            |> List.find (fun command -> command.Length > 1 && command.[0] = "dotnet" && command.[1] = "restore")

        Assert.Contains("--configfile", restore)
        Assert.Contains("--no-cache", restore)
        Assert.Contains(Path.Combine(outputDirectory, "NuGet.Config"), restore)
        Assert.Contains("-p:SelfContained=true", restore)
        Assert.Contains("-p:FunnySharpPackageVersion=0.1.0", restore)
        Assert.Contains("-p:FunnySharpAspNetCorePackageVersion=0.1.0", restore)
        Assert.Contains(
            "-p:BaseIntermediateOutputPath="
            + Path.Combine(outputDirectory, "CoreSmoke", "obj")
            + string Path.DirectorySeparatorChar,
            restore
        )

        let publish =
            runner.Calls
            |> List.find (fun command -> command.Length > 1 && command.[0] = "dotnet" && command.[1] = "publish")

        Assert.Contains("--self-contained", publish)
        Assert.Contains("--no-restore", publish)
        Assert.Contains(Path.Combine(outputDirectory, "CoreSmoke", "publish"), publish)

        let app = List.last runner.Calls
        Assert.Equal(1, app.Length)
        Assert.StartsWith(Path.Combine(outputDirectory, "CoreSmoke", "publish"), app.[0])

    // ---- evidence JSON ----

    [<Fact>]
    member this.HappyPathWritesPascalCaseEvidenceWithCorrectHashCase() =
        let exitCode, _, stderr = runFrom root (this.BaseArgs @ [ "-Scenario"; "CoreSmoke" ])
        Assert.Equal(0, exitCode)
        Assert.Equal("", stderr)

        let evidence = this.Evidence

        Assert.Equal<string list>(
            [ "SchemaVersion"
              "Succeeded"
              "RuntimeIdentifier"
              "PackageFeed"
              "CorePackageVersion"
              "AspNetCorePackageVersion"
              "CorePackageSha256"
              "AspNetCorePackageSha256"
              "CoreAssemblySha256"
              "AspNetCoreAssemblySha256"
              "Scenarios" ],
            evidence.TopLevelKeys
        )

        Assert.Equal(1, evidence.SchemaVersion)
        Assert.True(evidence.Succeeded)
        Assert.Equal(hostRuntimeIdentifier, evidence.RuntimeIdentifier)
        Assert.NotEqual<string>("", evidence.CorePackageSha256)
        Assert.Equal(evidence.CorePackageSha256.ToUpperInvariant(), evidence.CorePackageSha256)
        Assert.Equal(evidence.CoreAssemblySha256.ToLowerInvariant(), evidence.CoreAssemblySha256)

        let scenario = List.exactlyOne evidence.Scenarios
        Assert.Equal("CoreSmoke", scenario.Scenario)
        Assert.Equal("Passed", scenario.Outcome)
        Assert.True(scenario.StartedAtUtc.EndsWith("+00:00", StringComparison.Ordinal))
        Assert.True(scenario.FinishedAtUtc.EndsWith("+00:00", StringComparison.Ordinal))
        Assert.True(DateTimeOffset.TryParse(scenario.StartedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind) |> fst)
        Assert.True(Option.isSome scenario.PublishedCoreAssemblySha256)
        Assert.Equal(scenario.CoreAssemblySha256, defaultArg scenario.PublishedCoreAssemblySha256 "")
        Assert.True(Option.isNone scenario.PublishedAspNetCoreAssemblySha256)
        Assert.True(Option.isNone scenario.Error)

    [<Fact>]
    member this.AspNetCoreSmokeHashesBothPublishedAssemblies() =
        let exitCode, _, _ = runFrom root (this.BaseArgs @ [ "-Scenario"; "AspNetCoreSmoke" ])
        Assert.Equal(0, exitCode)
        let scenario = List.exactlyOne this.Evidence.Scenarios
        Assert.Equal("Passed", scenario.Outcome)
        Assert.True(Option.isSome scenario.PublishedCoreAssemblySha256)
        Assert.True(Option.isSome scenario.PublishedAspNetCoreAssemblySha256)

    [<Fact>]
    member this.SmokeHashMismatchFailsButStillWritesJsonForEveryScenario() =
        runner.CoreBytes <- [| 9uy; 9uy; 9uy |]

        let exitCode, _, stderr =
            runFrom root (this.BaseArgs @ [ "-Scenario"; "CoreSmoke,AspNetCoreSmoke" ])

        Assert.Equal(1, exitCode)
        Assert.Contains("CoreSmoke failed: Published FunnySharp.dll does not match the canonical package assembly.", stderr)

        let evidence = this.Evidence
        Assert.False(evidence.Succeeded)
        Assert.Equal<string list>([ "CoreSmoke"; "AspNetCoreSmoke" ], evidence.ScenarioNames)
        Assert.Equal("Failed", evidence.Scenarios.[0].Outcome)
        Assert.Equal(
            Some "Published FunnySharp.dll does not match the canonical package assembly.",
            evidence.Scenarios.[0].Error
        )
        Assert.Equal("Passed", evidence.Scenarios.[1].Outcome)

    [<Fact>]
    member this.PublishFailureIsReportedAsAFailedScenario() =
        runner.PublishFailureProject <- Some "Compatibility.Core"

        let exitCode, _, stderr = runFrom root (this.BaseArgs @ [ "-Scenario"; "CoreSmoke" ])
        Assert.Equal(1, exitCode)
        Assert.Contains("CoreSmoke failed: dotnet exited with code 1.", stderr)

        let scenario = List.exactlyOne this.Evidence.Scenarios
        Assert.Equal("Failed", scenario.Outcome)
        Assert.Equal(Some "dotnet exited with code 1.", scenario.Error)
        Assert.True(Option.isNone scenario.PublishedCoreAssemblySha256)
