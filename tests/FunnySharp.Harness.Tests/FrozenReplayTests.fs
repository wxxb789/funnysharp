module FunnySharp.Harness.Tests.FrozenReplayTests

open System
open System.IO
open System.Security.Cryptography
open System.Text.Json.Nodes
open Xunit
open FunnySharp.Harness.Proc
open FunnySharp.Harness.Tests.Support

let private hash (path: string) =
    use stream = File.OpenRead path
    SHA256.HashData stream |> Convert.ToHexString |> fun value -> value.ToLowerInvariant()

let private files (directory: string) =
    let result = JsonArray()
    for path in Directory.GetFiles(directory, "*", SearchOption.AllDirectories) |> Array.sort do
        let row = JsonObject()
        row.["path"] <- JsonValue.Create(Path.GetRelativePath(directory, path).Replace('\\', '/'))
        row.["sha256"] <- JsonValue.Create(hash path)
        result.Add row
    result

let private write (path: string) (text: string) =
    Directory.CreateDirectory(Path.GetDirectoryName path |> Option.ofObj |> Option.get) |> ignore
    File.WriteAllText(path, text)

let private readJson (path: string) =
    JsonNode.Parse(File.ReadAllText path) |> Option.ofObj |> Option.get

let private textField (name: string) (value: JsonNode) =
    (value.[name] |> Option.ofObj |> Option.get).GetValue<string>()

[<CollectionDefinition("Frozen replay", DisableParallelization = true)>]
type FrozenReplayCollection() = class end

// Synthetic metadata and injected dotnet outcomes test the replay plumbing only.
[<Collection("Frozen replay")>]
type FrozenReplayToolTests() =
    let temp = new TempDirectory()
    let root = Path.Combine(temp.Path, "repository")
    let study = Path.Combine(root, "eng", "evaluation", "studies", "audit-resolution-v6")
    let snapshot = Path.Combine(study, "snapshot")
    let source = Path.Combine(root, "source", "solution")
    let output = Path.Combine(temp.Path, "replay")
    let calls = ResizeArray<string list>()
    let launches = ResizeArray<string list>()
    let mutable buildExit = 0
    let originalRunner = FunnySharp.Harness.Evaluation.childRunner
    let originalClock = FunnySharp.Harness.Evaluation.clock

    do
        for relative, text in
            [ "tasks/aspnetcore/tests/Contract.cs", "// neutral contract\n"
              "tasks/aspnetcore/tests/XTests.cs", "// oracle\n"
              "tasks/aspnetcore/template-idiomatic/Kit.csproj", "<Project />"
              "tasks/aspnetcore/template-idiomatic/packages.lock.json", "{}"
              "feed/FunnySharp.0.2.0.nupkg", "candidate package fixture"
              "analyzer-control/control.sarif", "{}" ] do
            write (Path.Combine(snapshot, relative)) text
        for relative, text in
            [ "global.json", "{}"
              "Directory.Build.props", "<Project />"
              "build.fsx", "// frozen build"
              "eng/harness/Evaluation.fs", "// frozen runner" ] do
            write (Path.Combine(root, relative)) text
            write (Path.Combine(snapshot, "environment", relative)) text
        for name in [ "Output.fs"; "Proc.fs"; "Repo.fs"; "Loc.fs" ] do
            write (Path.Combine(root, "eng", "harness", name)) ("// support fixture " + name)
        for relative in [ "eng/harness/FrozenReplay.fs"; "eng/evaluation/replay-frozen.fsx" ] do
            write (Path.Combine(root, relative)) "// driver fixture"
        write (Path.Combine(study, "plan.json")) "{}"
        let manifest = JsonObject()
        manifest.["schema"] <- JsonValue.Create "funnysharp-evaluation-study/v1"
        manifest.["study"] <- JsonValue.Create "audit-resolution-v6"
        manifest.["files"] <- files snapshot
        manifest.["planSha256"] <- JsonValue.Create(hash (Path.Combine(study, "plan.json")))
        manifest.["expectedTests"] <- JsonNode.Parse "{\"aspnetcore\":9}"
        manifest.["cohort"] <- JsonNode.Parse "[\"aspnetcore/idiomatic/run-1\"]"
        write (Path.Combine(study, "manifest.json")) (manifest.ToJsonString())
        write (Path.Combine(source, "Solution.cs")) "public sealed class Solution {}\n"
        FunnySharp.Harness.Evaluation.clock <- fun () -> 1.0
        FunnySharp.Harness.Evaluation.childRunner <- fun _ _ command ->
            calls.Add command
            { ExitCode = if command.[1] = "test" then 0 else buildExit
              Stdout =
                if command.[1] = "test" then "Test run summary: Passed!\ntotal: 9 failed: 0 succeeded: 9 skipped: 0\n"
                else "Build succeeded.\n"
              Stderr = "" }

    let expectedManifestHash = hash (Path.Combine(study, "manifest.json"))

    let execute isolatedRoot command =
        launches.Add command
        Assert.Equal<string list>(
            [ "dotnet"; "fsi"; "--exec"; Path.Combine(isolatedRoot, "replay.fsx"); "--"
              "verify"; "aspnetcore"; "idiomatic"; Path.Combine(output, "run")
              "--study"; "audit-resolution-v6"; "--replay" ], command)
        use stdout = new StringWriter()
        use stderr = new StringWriter()
        let code = FunnySharp.Harness.Evaluation.mainWith stdout stderr isolatedRoot (command |> List.skip 5)
        { ExitCode = code; Stdout = stdout.ToString(); Stderr = stderr.ToString() }

    let run outputPath runner =
        use stdout = new StringWriter()
        use stderr = new StringWriter()
        FunnySharp.Harness.FrozenReplay.mainWith stdout stderr root runner
            [ "aspnetcore"; "idiomatic"; source; outputPath; expectedManifestHash ]

    interface IDisposable with
        member _.Dispose() =
            FunnySharp.Harness.Evaluation.childRunner <- originalRunner
            FunnySharp.Harness.Evaluation.clock <- originalClock
            (temp :> IDisposable).Dispose()

    [<Fact>]
    member _.FrozenReplaySurvivesUnrelatedLiveBuildChange() =
        let before = (files study).ToJsonString()
        write (Path.Combine(root, "build.fsx")) "// unrelated current pipeline change"
        write (Path.Combine(root, "eng", "harness", "Evaluation.fs")) "// unrelated current runner change"
        use stdout = new StringWriter()
        use stderr = new StringWriter()
        for suffix in [ []; [ "--replay" ] ] do
            let code = FunnySharp.Harness.Evaluation.mainWith stdout stderr root
                           ([ "verify"; "aspnetcore"; "idiomatic"; source; "--study"; "audit-resolution-v6" ] @ suffix)
            Assert.Equal(1, code)
        Assert.Empty calls
        Assert.Equal(0, run output execute)
        Assert.Equal(2, calls.Count)
        Assert.Single launches |> ignore
        Assert.Contains("-p:RestoreLockedMode=true", calls.[0])
        let isolatedRoot = Path.Combine(output, "frozen-root")
        for relative in [ "global.json"; "Directory.Build.props"; "build.fsx"; "eng/harness/Evaluation.fs" ] do
            Assert.Equal(hash (Path.Combine(snapshot, "environment", relative)), hash (Path.Combine(isolatedRoot, relative)))
        let binding = readJson (Path.Combine(output, "replay-bindings.json"))
        Assert.Equal(expectedManifestHash, textField "studySha256" binding)
        Assert.Equal("contemporary-not-historical", textField "supportBinding" binding)
        let declared = (binding.["files"] |> Option.ofObj |> Option.get).AsArray()
        for relative in [ "eng/harness/Output.fs"; "eng/harness/Proc.fs"; "eng/harness/Repo.fs"; "eng/harness/Loc.fs" ] do
            let row = declared |> Seq.map (fun row -> row |> Option.ofObj |> Option.get)
                      |> Seq.find (fun row -> textField "path" row = "frozen-root/" + relative)
            Assert.Equal(hash (Path.Combine(root, relative)), textField "sha256" row)
        let round = Path.Combine(output, "run", "rounds", "0001")
        let record = readJson (Path.Combine(round, "record.json"))
        Assert.Equal("replay", textField "evidenceKind" record)
        Assert.False(File.Exists(Path.Combine(round, "producer-receipt.json")))
        let inputs = readJson (Path.Combine(round, "inputs.json"))
        Assert.Equal(expectedManifestHash, textField "studySha256" inputs)
        Assert.Equal(hash (Path.Combine(source, "Solution.cs")), hash (Path.Combine(round, "solution", "Solution.cs")))
        Assert.Equal(before, (files study).ToJsonString())
        Assert.False(Directory.Exists(Path.Combine(root, "eng", "evaluation", "results")))
        Assert.False(Directory.Exists(Path.Combine(isolatedRoot, "eng", "evaluation", "results")))
        Assert.Equal(1, run output execute)
        Assert.Single launches |> ignore

    [<Theory>]
    [<InlineData("environment/build.fsx")>]
    [<InlineData("environment/eng/harness/Evaluation.fs")>]
    [<InlineData("environment/global.json")>]
    [<InlineData("environment/Directory.Build.props")>]
    [<InlineData("feed/FunnySharp.0.2.0.nupkg")>]
    [<InlineData("tasks/aspnetcore/tests/Contract.cs")>]
    [<InlineData("tasks/aspnetcore/template-idiomatic/packages.lock.json")>]
    [<InlineData("analyzer-control/control.sarif")>]
    member _.AlteredFrozenInputFailsBeforeReservation(relative: string) =
        File.AppendAllText(Path.Combine(snapshot, relative), "changed")
        Assert.Equal(1, run output execute)
        Assert.Empty launches
        Assert.False(Directory.Exists output)

    [<Theory>]
    [<InlineData("manifest.json")>]
    [<InlineData("plan.json")>]
    member _.AlteredRetainedBindingFailsBeforeReservation(relative: string) =
        File.AppendAllText(Path.Combine(study, relative), " ")
        Assert.Equal(1, run output execute)
        Assert.Empty launches
        Assert.False(Directory.Exists output)

    [<Theory>]
    [<InlineData(true)>]
    [<InlineData(false)>]
    member _.AddedOrRemovedFrozenFileFailsBeforeReservation(added: bool) =
        if added then write (Path.Combine(snapshot, "extra.json")) "{}"
        else File.Delete(Path.Combine(snapshot, "analyzer-control", "control.sarif"))
        Assert.Equal(1, run output execute)
        Assert.Empty launches
        Assert.False(Directory.Exists output)

    [<Theory>]
    [<InlineData(true)>]
    [<InlineData(false)>]
    member _.ExistingEmptyOrInterruptedOutputIsReadOnly(interrupted: bool) =
        Directory.CreateDirectory output |> ignore
        if interrupted then write (Path.Combine(output, "replay.started.json")) "{}"
        let before = (files output).ToJsonString()
        Assert.Equal(1, run output execute)
        Assert.Equal(before, (files output).ToJsonString())
        Assert.Empty launches

    [<Fact>]
    member _.OutputCannotConsumeACohortSlot() =
        let cohort = Path.Combine(root, "eng", "evaluation", "results", "audit-resolution-v6", "aspnetcore", "idiomatic", "run-1")
        Assert.Equal(1, run cohort execute)
        Assert.Empty launches
        Assert.False(Directory.Exists cohort)

    [<Fact>]
    member _.StagedSupportAlterationFailsClosed() =
        let corrupt isolatedRoot command =
            write (Path.Combine(isolatedRoot, "eng", "harness", "Loc.fs")) "// changed support"
            execute isolatedRoot command
        Assert.Equal(1, run output corrupt)
        Assert.Single launches |> ignore

    [<Fact>]
    member _.ConsumerFailureIsRetainedAsReplayRed() =
        buildExit <- 1
        Assert.Equal(1, run output execute)
        Assert.Single calls |> ignore
        let record = readJson (Path.Combine(output, "run", "rounds", "0001", "record.json"))
        Assert.Equal("replay", textField "evidenceKind" record)
        Assert.Equal("RED", textField "verdict" record)
        let processReceipt = readJson (Path.Combine(output, "runner.process.json"))
        Assert.Equal(1, (processReceipt.["exitCode"] |> Option.ofObj |> Option.get).GetValue<int>())
        Assert.Equal(1, run output execute)
        Assert.Single launches |> ignore
