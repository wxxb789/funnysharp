module FunnySharp.Harness.Tests.CompatibilityTests

open System
open System.IO
open System.IO.Compression
open System.Text.Json
open Xunit
open FunnySharp.Harness.Compatibility
open FunnySharp.Harness.Tests.Support

let private defaultLanes = [ "net10.0"; "net11.0" ]

let private laneEntries (lanes: string list) (id: string) : string list =
    [ for lane in lanes do
          yield sprintf "lib/%s/%s.dll" lane id
          yield sprintf "lib/%s/%s.xml" lane id ]

let private analyzerEntries =
    [ "analyzers/dotnet/cs/FunnySharp.Analyzers.dll"
      "analyzers/dotnet/cs/FunnySharp.Analyzers.CodeFixes.dll" ]

/// Builds one package from explicit content entries, per-lane dependency groups, and the
/// lanes that carry the ASP.NET Core framework reference, so a test can drop or misdeclare
/// any single required lane asset or metadata group.
let private writePackage
    directory
    (id: string)
    (contentEntries: string list)
    (dependencyGroups: (string * string) list)
    (frameworkLanes: string list)
    =
    let path = Path.Combine(directory, id + ".1.0.0.nupkg")
    use archive = ZipFile.Open(path, ZipArchiveMode.Create)
    let add name text =
        let entry = archive.CreateEntry name
        use writer = new StreamWriter(entry.Open())
        writer.Write(text: string)
    let dependenciesXml =
        dependencyGroups
        |> List.map (fun (lane, inner) -> sprintf "<group targetFramework=\"%s\">%s</group>" lane inner)
        |> String.concat ""
    let frameworksXml =
        if id = "FunnySharp.AspNetCore" then
            frameworkLanes
            |> List.map (fun lane ->
                sprintf "<group targetFramework=\"%s\"><frameworkReference name=\"Microsoft.AspNetCore.App\"/></group>" lane)
            |> String.concat ""
            |> sprintf "<frameworkReferences>%s</frameworkReferences>"
        else
            ""
    add (id + ".nuspec") (sprintf "<package><metadata><id>%s</id><version>1.0.0</version><readme>README.md</readme><license type=\"expression\">MIT</license><dependencies>%s</dependencies>%s</metadata></package>" id dependenciesXml frameworksXml)
    for name in "README.md" :: contentEntries do
        add name "content"
    path

let private corePackageEntries (lanes: string list) (analyzers: bool) : string list =
    laneEntries lanes "FunnySharp" @ (if analyzers then analyzerEntries else [])

let private aspNetCorePackageEntries (lanes: string list) : string list =
    laneEntries lanes "FunnySharp.AspNetCore"

let private laneDependencyGroups (version: string) : (string * string) list =
    [ for lane in defaultLanes -> lane, sprintf "<dependency id=\"FunnySharp\" version=\"%s\"/>" version ]

let private writeValidPackages directory =
    writePackage directory "FunnySharp" (corePackageEntries defaultLanes true) [ for lane in defaultLanes -> lane, "" ] []
    |> ignore

    writePackage
        directory
        "FunnySharp.AspNetCore"
        (aspNetCorePackageEntries defaultLanes)
        (laneDependencyGroups "1.0.0")
        defaultLanes
    |> ignore

let private writeSymbolsPackage directory id =
    let path = Path.Combine(directory, id + ".1.0.0.snupkg")
    use archive = ZipFile.Open(path, ZipArchiveMode.Create)
    let add name text =
        let entry = archive.CreateEntry name
        use writer = new StreamWriter(entry.Open())
        writer.Write(text: string)
    add (id + ".nuspec") (sprintf "<package><metadata><id>%s</id><version>1.0.0</version></metadata></package>" id)
    for lane in defaultLanes do
        add (sprintf "lib/%s/%s.pdb" lane id) "symbols"

let private runFixture failure mutatePackages =
    use temp = new TempDirectory()
    File.WriteAllText(Path.Combine(temp.Path, "FunnySharp.slnx"), "")
    File.WriteAllText(
        Path.Combine(temp.Path, "global.json"),
        "{\"sdk\":{\"version\":\"11.0.100\",\"rollForward\":\"latestPatch\"}}"
    )
    let packages = Path.Combine(temp.Path, "packages")
    Directory.CreateDirectory packages |> ignore
    writeValidPackages packages
    for id in [ "FunnySharp"; "FunnySharp.AspNetCore" ] do
        writeSymbolsPackage packages id
    mutatePackages packages
    let calls = ResizeArray<string list>()
    let previous = processRunner
    let previousSupported = hostOsSupported
    hostOsSupported <- true
    processRunner <- fun _ env args ->
        calls.Add args
        let cache = env |> List.find (fun (key, _) -> key = "NUGET_PACKAGES") |> snd
        Assert.StartsWith(Path.Combine(temp.Path, "artifacts", "nuget"), cache)
        if args.Head = "dotnet" && args.[1] = "publish" then
            let index = args |> List.findIndex ((=) "--output")
            let directory = args.[index + 1]
            Directory.CreateDirectory directory |> ignore
            let suffix = if OperatingSystem.IsWindows() then ".exe" else ""
            File.WriteAllText(Path.Combine(directory, "FunnySharp.Compatibility.Core" + suffix), "app")
        if (args.Head = "dotnet" && args.[1] = failure) || (args.Head <> "dotnet" && failure = "run") then 8 else 0
    try
        let output = Path.Combine(temp.Path, "artifacts", "compatibility")
        use stdout = new StringWriter()
        use stderr = new StringWriter()
        let code = mainWith stdout stderr temp.Path [ "-RepositoryRoot"; temp.Path; "-PackageDirectory"; packages; "-OutputDirectory"; output; "-Scenario"; "CoreSmoke" ]
        let resultsPath = Path.Combine(output, "compatibility-results.json")
        let summary = if File.Exists resultsPath then File.ReadAllText resultsPath else stderr.ToString()
        code, calls |> Seq.toList, summary
    finally
        processRunner <- previous
        hostOsSupported <- previousSupported

type CompatibilityTests() =
    [<Fact>]
    member _.RestoresPublishesAndRunsConsumer() =
        let code, calls, summary = runFixture "" ignore
        Assert.Equal(0, code)
        Assert.Equal(3, calls.Length)
        Assert.Contains("restore", calls.[0])
        Assert.Contains("publish", calls.[1])
        Assert.Contains("--no-cache", calls.[0])
        Assert.Contains("--no-restore", calls.[1])
        use document = JsonDocument.Parse summary
        Assert.True(document.RootElement.GetProperty("Succeeded").GetBoolean())
        Assert.Equal("11.0.100", document.RootElement.GetProperty("SdkVersion").GetString())
        let scenario = document.RootElement.GetProperty("Scenarios")[0]
        Assert.Equal("net10.0", scenario.GetProperty("TargetFramework").GetString())

        Assert.Contains(
            "lib/net10.0/FunnySharp.dll",
            scenario.GetProperty("SelectedPackageAssets").EnumerateArray()
            |> Seq.choose (fun item -> item.GetString() |> Option.ofObj)
        )

    [<Theory>]
    [<InlineData("restore", 1)>]
    [<InlineData("publish", 2)>]
    [<InlineData("run", 3)>]
    member _.FailedSdkCommandFailsScenario(command, expectedCalls) =
        let code, calls, summary = runFixture command ignore
        Assert.Equal(1, code)
        Assert.Equal(expectedCalls, calls.Length)
        use document = JsonDocument.Parse summary
        Assert.False(document.RootElement.GetProperty("Succeeded").GetBoolean())

    [<Theory>]
    [<InlineData("missing-core-package")>]
    [<InlineData("missing-aspnetcore-package")>]
    [<InlineData("missing-core-symbols")>]
    [<InlineData("missing-aspnetcore-symbols")>]
    [<InlineData("extra-package")>]
    [<InlineData("extra-symbols")>]
    [<InlineData("duplicate-core-version")>]
    [<InlineData("mismatched-package-version")>]
    [<InlineData("mismatched-core-symbols")>]
    [<InlineData("mismatched-aspnetcore-symbols")>]
    member _.InvalidPackageInventoryRejectsBeforeRunningConsumers(case) =
        let mutate packages =
            let path name = Path.Combine(packages, name)
            let remove name = File.Delete(path name)
            let move name replacement = File.Move(path name, path replacement)
            match case with
            | "missing-core-package" -> remove "FunnySharp.1.0.0.nupkg"
            | "missing-aspnetcore-package" -> remove "FunnySharp.AspNetCore.1.0.0.nupkg"
            | "missing-core-symbols" -> remove "FunnySharp.1.0.0.snupkg"
            | "missing-aspnetcore-symbols" -> remove "FunnySharp.AspNetCore.1.0.0.snupkg"
            | "extra-package" -> File.WriteAllText(path "Other.1.0.0.nupkg", "extra")
            | "extra-symbols" -> File.WriteAllText(path "Other.1.0.0.snupkg", "extra")
            | "duplicate-core-version" -> File.Copy(path "FunnySharp.1.0.0.nupkg", path "FunnySharp.2.0.0.nupkg")
            | "mismatched-package-version" -> move "FunnySharp.AspNetCore.1.0.0.nupkg" "FunnySharp.AspNetCore.2.0.0.nupkg"
            | "mismatched-core-symbols" -> move "FunnySharp.1.0.0.snupkg" "FunnySharp.2.0.0.snupkg"
            | "mismatched-aspnetcore-symbols" -> move "FunnySharp.AspNetCore.1.0.0.snupkg" "FunnySharp.AspNetCore.2.0.0.snupkg"
            | _ -> invalidArg "case" case
        let code, calls, error = runFixture "" mutate
        Assert.Equal(2, code)
        Assert.Empty calls
        Assert.NotEmpty error

    [<Fact>]
    member _.InvalidPackageContentsRejectBeforeRunningConsumers() =
        let mutate packages =
            File.Delete(Path.Combine(packages, "FunnySharp.1.0.0.nupkg"))
            writePackage packages "FunnySharp" (corePackageEntries defaultLanes true) [ for lane in defaultLanes -> lane, "<dependency id=\"Other\" version=\"1.0.0\"/>" ] []
            |> ignore
        let code, calls, error = runFixture "" mutate
        Assert.Equal(2, code)
        Assert.Empty calls
        Assert.Contains("no runtime dependencies", error)

    [<Theory>]
    [<InlineData("missing-net10-dll")>]
    [<InlineData("missing-net10-xml")>]
    [<InlineData("missing-net11-dll")>]
    [<InlineData("missing-net11-xml")>]
    [<InlineData("missing-net11-dependency-group")>]
    [<InlineData("missing-net11-framework-reference")>]
    [<InlineData("wrong-net11-dependency-version")>]
    member _.PackageMissingEitherLaneAssetOrMetadataRejects(case) =
        let mutate packages =
            let coreEntries = corePackageEntries defaultLanes true
            let coreEntriesWithout entry = coreEntries |> List.filter ((<>) entry)

            let rewriteCore entries groups =
                File.Delete(Path.Combine(packages, "FunnySharp.1.0.0.nupkg"))
                writePackage packages "FunnySharp" entries groups [] |> ignore

            let rewriteAspNetCore groups frameworks =
                File.Delete(Path.Combine(packages, "FunnySharp.AspNetCore.1.0.0.nupkg"))

                writePackage packages "FunnySharp.AspNetCore" (aspNetCorePackageEntries defaultLanes) groups frameworks
                |> ignore

            match case with
            | "missing-net10-dll" ->
                rewriteCore (coreEntriesWithout "lib/net10.0/FunnySharp.dll") [ for lane in defaultLanes -> lane, "" ]
            | "missing-net10-xml" ->
                rewriteCore (coreEntriesWithout "lib/net10.0/FunnySharp.xml") [ for lane in defaultLanes -> lane, "" ]
            | "missing-net11-dll" ->
                rewriteCore (coreEntriesWithout "lib/net11.0/FunnySharp.dll") [ for lane in defaultLanes -> lane, "" ]
            | "missing-net11-xml" ->
                rewriteCore (coreEntriesWithout "lib/net11.0/FunnySharp.xml") [ for lane in defaultLanes -> lane, "" ]
            | "missing-net11-dependency-group" -> rewriteCore coreEntries [ "net10.0", "" ]
            | "missing-net11-framework-reference" -> rewriteAspNetCore (laneDependencyGroups "1.0.0") [ "net10.0" ]
            | "wrong-net11-dependency-version" ->
                rewriteAspNetCore
                    [ "net10.0", "<dependency id=\"FunnySharp\" version=\"1.0.0\"/>"
                      "net11.0", "<dependency id=\"FunnySharp\" version=\"2.0.0\"/>" ]
                    defaultLanes
            | _ -> invalidArg "case" case

        let code, calls, error = runFixture "" mutate
        Assert.Equal(2, code)
        Assert.Empty calls
        Assert.NotEmpty error

    [<Theory>]
    [<InlineData("", false)>]
    [<InlineData("<dependency id=\"Other\" version=\"1.0.0\"/>", true)>]
    member _.MissingAnalyzersOrRuntimeDependencyRejectsCorePackage(dependencies, analyzers) =
        use temp = new TempDirectory()

        let path =
            writePackage
                temp.Path
                "FunnySharp"
                (corePackageEntries defaultLanes analyzers)
                [ for lane in defaultLanes -> lane, dependencies ]
                []

        Assert.Throws<InvalidOperationException>(fun () -> validatePackage path "FunnySharp" "1.0.0" "1.0.0" |> ignore)
        |> ignore

    [<Fact>]
    member _.WrongAspNetCoreDependencyRejectsPackage() =
        use temp = new TempDirectory()

        let path =
            writePackage
                temp.Path
                "FunnySharp.AspNetCore"
                (aspNetCorePackageEntries defaultLanes)
                (laneDependencyGroups "2.0.0")
                defaultLanes

        Assert.Throws<InvalidOperationException>(fun () ->
            validatePackage path "FunnySharp.AspNetCore" "1.0.0" "1.0.0" |> ignore)
        |> ignore

    [<Fact>]
    member _.RejectsOutputOutsideArtifactsBeforeRunningCommands() =
        use temp = new TempDirectory()
        File.WriteAllText(Path.Combine(temp.Path, "FunnySharp.slnx"), "")
        use stdout = new StringWriter()
        use stderr = new StringWriter()
        let code = mainWith stdout stderr temp.Path [ "-RepositoryRoot"; temp.Path; "-PackageDirectory"; temp.Path; "-OutputDirectory"; Path.Combine(temp.Path, "outside") ]
        Assert.Equal(2, code)
        Assert.Contains("subdirectory", stderr.ToString())

    [<Fact>]
    member _.RejectsWrongHostRid() =
        use temp = new TempDirectory()
        File.WriteAllText(Path.Combine(temp.Path, "FunnySharp.slnx"), "")
        use stdout = new StringWriter()
        use stderr = new StringWriter()
        let code = mainWith stdout stderr temp.Path [ "-RepositoryRoot"; temp.Path; "-PackageDirectory"; temp.Path; "-OutputDirectory"; Path.Combine(temp.Path, "artifacts", "compatibility"); "-RuntimeIdentifier"; "wrong-rid" ]
        Assert.Equal(2, code)
        Assert.Contains("must match", stderr.ToString())
