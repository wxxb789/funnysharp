module FunnySharp.Harness.Tests.ReleaseRunTests

open System
open System.IO
open System.Text.Json
open Xunit
open FunnySharp.Harness.ReleaseRun
open FunnySharp.Harness.Proc
open FunnySharp.Harness.Tests.Support

let private commit = String.replicate 40 "a"
let private ok = { ExitCode = 0; Stdout = "ok"; Stderr = "" }

let private runFixtureWithTest (dirty: bool) (change: bool) (exitCode: int) (published: bool) (skipped: bool) =
    use temp = new TempDirectory()
    File.WriteAllText(Path.Combine(temp.Path, "FunnySharp.slnx"), "")
    for name in [ "FunnySharp"; "FunnySharp.AspNetCore" ] do
        let directory = Path.Combine(temp.Path, "src", name)
        Directory.CreateDirectory directory |> ignore
        File.WriteAllText(Path.Combine(directory, name + ".csproj"), "<Project><PropertyGroup><VersionPrefix>1.0.0</VersionPrefix></PropertyGroup></Project>")
    let calls = ResizeArray<string>()
    let mutable statuses = 0
    let mutable environment = Map.empty
    let runner exe args env _ =
        if exe = "git" then
            let text =
                match args with
                | [ "rev-parse"; "--show-toplevel" ] -> temp.Path
                | [ "rev-parse"; "HEAD" ] -> commit
                | "status" :: _ ->
                    statuses <- statuses + 1
                    if dirty || (change && statuses > 1) then " M src/file.cs" else ""
                | _ -> failwithf "Unexpected Git command %A" args
            { ok with Stdout = text }
        else
            environment <- env
            calls.Add exe
            let output =
                if exe = "test" then
                    let summary = sprintf "Test run summary: Passed!\ntotal: 2 failed: 0 succeeded: %d skipped: %d\n" (if skipped then 1 else 2) (if skipped then 1 else 0)
                    summary + (FunnySharp.Harness.ToolingVerify.testAssemblyRelativePaths
                        |> List.map (fun relative -> sprintf "%s (net10.0|x64) passed (1 test)" (Path.GetFullPath(Path.Combine(temp.Path, relative))))
                        |> String.concat "\n")
                else "ok"
            { ok with ExitCode = exitCode; Stdout = output }
    let http (url: string) : HttpResponse =
        if url.EndsWith("index.json") && url <> "https://feed.example/index.json" then
            { StatusCode = 200; Content = if published then "{\"versions\":[\"1.0.0\"]}" else "{\"versions\":[]}" }
        else
            { StatusCode = 200; Content = "{\"resources\":[{\"@type\":\"PackageBaseAddress/3.0.0\",\"@id\":\"https://feed.example/flat/\"}]}" }
    let protocol =
        { defaultProtocolApi with
            LoadSteps = fun _ _ _ ->
                [ { Name = "build"; FileName = "build"; WorkingDirectory = temp.Path; Arguments = [] }
                  { Name = "test"; FileName = "test"; WorkingDirectory = temp.Path; Arguments = [] } ] }
    use stdout = new StringWriter()
    use stderr = new StringWriter()
    let code = mainWith stdout stderr temp.Path { Runner = runner; HttpGet = http; Protocol = protocol; UtcNow = fun () -> DateTime.UtcNow }
                [ "-AttemptId"; "attempt"; "-DistributionFeed"; "https://feed.example/index.json" ]
    let output = Path.Combine(temp.Path, "artifacts", "release-candidate", commit, "attempt")
    let summary = if File.Exists(Path.Combine(output, "release-summary.json")) then File.ReadAllText(Path.Combine(output, "release-summary.json")) else ""
    let logs = if Directory.Exists(Path.Combine(output, "logs")) then Directory.GetFiles(Path.Combine(output, "logs")).Length else 0
    code, calls |> Seq.toList, environment, summary, logs, stderr.ToString()

let private runFixture dirty change exitCode published =
    runFixtureWithTest dirty change exitCode published false

type ReleaseRunTests() =
    [<Fact>]
    member _.SuccessfulExitWithSkippedTestsFailsRelease() =
        let code, _, _, summary, _, error = runFixtureWithTest false false 0 false true
        Assert.Equal(1, code)
        Assert.NotEmpty error
        use document = JsonDocument.Parse summary
        Assert.False(document.RootElement.GetProperty("succeeded").GetBoolean())

    [<Fact>]
    member _.ExecutesOnceWithIsolatedCacheAndLogs() =
        let code, calls, env, summary, logs, error = runFixture false false 0 false
        Assert.Equal(0, code)
        Assert.Empty error
        Assert.Equal<string list>([ "build"; "test" ], calls)
        Assert.Contains("nuget-packages", env.["NUGET_PACKAGES"])
        Assert.Equal(commit, env.["FUNNYSHARP_CANDIDATE_COMMIT"])
        Assert.Equal(4, logs)
        use document = JsonDocument.Parse summary
        Assert.True(document.RootElement.GetProperty("succeeded").GetBoolean())

    [<Theory>]
    [<InlineData(true, false, 0, false, 0)>]
    [<InlineData(false, true, 0, false, 2)>]
    [<InlineData(false, false, 7, false, 1)>]
    [<InlineData(false, false, 0, true, 0)>]
    member _.RefusesDirtyChangedFailedOrPublishedCandidates(dirty, change, exitCode, published, expectedCalls) =
        let code, calls, _, _, _, error = runFixture dirty change exitCode published
        Assert.Equal(1, code)
        Assert.Equal(expectedCalls, calls.Length)
        Assert.NotEmpty error
