module FunnySharp.Harness.Tests.CompatibilityTests

open System
open System.IO
open System.IO.Compression
open System.Text.Json
open Xunit
open FunnySharp.Harness.Compatibility
open FunnySharp.Harness.Tests.Support

let private writePackage directory id dependencies analyzers =
    let path = Path.Combine(directory, id + ".1.0.0.nupkg")
    use archive = ZipFile.Open(path, ZipArchiveMode.Create)
    let add name text =
        let entry = archive.CreateEntry name
        use writer = new StreamWriter(entry.Open())
        writer.Write(text: string)
    let frameworks = if id = "FunnySharp.AspNetCore" then "<frameworkReferences><group targetFramework=\"net10.0\"><frameworkReference name=\"Microsoft.AspNetCore.App\"/></group></frameworkReferences>" else ""
    add (id + ".nuspec") (sprintf "<package><metadata><id>%s</id><version>1.0.0</version><readme>README.md</readme><license type=\"expression\">MIT</license><dependencies><group targetFramework=\"net10.0\">%s</group></dependencies>%s</metadata></package>" id dependencies frameworks)
    for name in [ "README.md"; sprintf "lib/net10.0/%s.dll" id; sprintf "lib/net10.0/%s.xml" id ] do add name "content"
    if analyzers then
        add "analyzers/dotnet/cs/FunnySharp.Analyzers.dll" "analyzer"
        add "analyzers/dotnet/cs/FunnySharp.Analyzers.CodeFixes.dll" "fixes"
    path

let private runFixture failure =
    use temp = new TempDirectory()
    File.WriteAllText(Path.Combine(temp.Path, "FunnySharp.slnx"), "")
    let packages = Path.Combine(temp.Path, "packages")
    Directory.CreateDirectory packages |> ignore
    writePackage packages "FunnySharp" "" true |> ignore
    writePackage packages "FunnySharp.AspNetCore" "<dependency id=\"FunnySharp\" version=\"1.0.0\"/>" false |> ignore
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
        code, calls |> Seq.toList, File.ReadAllText(Path.Combine(output, "compatibility-results.json"))
    finally
        processRunner <- previous
        hostOsSupported <- previousSupported

type CompatibilityTests() =
    [<Fact>]
    member _.RestoresPublishesAndRunsConsumer() =
        let code, calls, summary = runFixture ""
        Assert.Equal(0, code)
        Assert.Equal(3, calls.Length)
        Assert.Contains("restore", calls.[0])
        Assert.Contains("publish", calls.[1])
        Assert.Contains("--no-cache", calls.[0])
        Assert.Contains("--no-restore", calls.[1])
        use document = JsonDocument.Parse summary
        Assert.True(document.RootElement.GetProperty("Succeeded").GetBoolean())

    [<Theory>]
    [<InlineData("restore", 1)>]
    [<InlineData("publish", 2)>]
    [<InlineData("run", 3)>]
    member _.FailedSdkCommandFailsScenario(command, expectedCalls) =
        let code, calls, summary = runFixture command
        Assert.Equal(1, code)
        Assert.Equal(expectedCalls, calls.Length)
        use document = JsonDocument.Parse summary
        Assert.False(document.RootElement.GetProperty("Succeeded").GetBoolean())

    [<Theory>]
    [<InlineData("", false)>]
    [<InlineData("<dependency id=\"Other\" version=\"1.0.0\"/>", true)>]
    member _.MissingAnalyzersOrRuntimeDependencyRejectsCorePackage(dependencies, analyzers) =
        use temp = new TempDirectory()
        let path = writePackage temp.Path "FunnySharp" dependencies analyzers
        Assert.Throws<InvalidOperationException>(fun () -> validatePackage path "FunnySharp" "1.0.0" "1.0.0") |> ignore

    [<Fact>]
    member _.WrongAspNetCoreDependencyRejectsPackage() =
        use temp = new TempDirectory()
        let path = writePackage temp.Path "FunnySharp.AspNetCore" "<dependency id=\"FunnySharp\" version=\"2.0.0\"/>" false
        Assert.Throws<InvalidOperationException>(fun () -> validatePackage path "FunnySharp.AspNetCore" "1.0.0" "1.0.0") |> ignore

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
