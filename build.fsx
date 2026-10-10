// FSI owns command orchestration; loaded F# modules own testable behavior.
#load "eng/harness/Output.fs"
#load "eng/harness/Proc.fs"
#load "eng/harness/Repo.fs"
#load "eng/harness/ActionPins.fs"
#load "eng/harness/DocsSnippets.fs"
#load "eng/harness/Inventory.fs"
#load "eng/harness/VerticalSlice.fs"
#load "eng/harness/ToolingVerify.fs"
#load "eng/harness/Performance.fs"
#load "eng/harness/PerformanceDocs.fs"
#load "eng/harness/Ruleset.fs"
#load "eng/harness/Compatibility.fs"
#load "eng/harness/ReleaseProtocol.fs"
#load "eng/harness/ReleaseRun.fs"
#load "eng/harness/ApiBaseline.fs"
#load "eng/harness/Loc.fs"
#load "eng/harness/Evaluation.fs"
#load "eng/harness/Cli.fs"

open FunnySharp.Harness
open System
open System.IO

let private native pipeline arguments =
    match Cli.nativeArguments pipeline (List.ofArray arguments) with
    | Error message -> Console.Error.WriteLine message; 2
    | Ok command -> Proc.runIn (Repo.repoRoot ()) "dotnet" command

let private adapted prefix values switches required run argv =
    match Cli.adaptNamed values switches required argv with
    | Error message -> Console.Error.WriteLine message; 2
    | Ok arguments -> run (Array.append prefix arguments)

let private tooling arguments =
    ToolingVerify.docsVerifier <-
        Some(fun repositoryRoot ->
            use stdout = new StringWriter()
            use stderr = new StringWriter()
            let code = DocsSnippets.mainWith stdout stderr [ "--repository-root"; repositoryRoot ]
            { ExitCode = code; Stdout = stdout.ToString(); Stderr = stderr.ToString() })
    ToolingVerify.main arguments

type private Command =
    { Name: string
      Description: string
      Options: string
      Run: string array -> int }

let private commands =
    [ { Name = "build"; Description = "Build the solution or one project."; Options = "[--project <path>] [dotnet build options]"; Run = native "build" }
      { Name = "test"; Description = "Run the solution or one project's tests."; Options = "[--project <path>] [dotnet test options]"; Run = native "test" }
      { Name = "format"; Description = "Verify formatting for the solution or one project."; Options = "[--project <path>] [dotnet format options]"; Run = native "format" }
      { Name = "check-action-pins"; Description = "Check remote GitHub action pins."; Options = "[--repository-root <path>] [--verbose]"; Run = fun args -> ActionPins.main (Array.append [| "--verbose" |] args) }
      { Name = "verify-docs-snippets"; Description = "Verify documentation samples against source regions."; Options = "[--repository-root <path>] [--samples-root <path>]"; Run = DocsSnippets.main }
      { Name = "verify-api-baseline"; Description = "Verify the public API baseline."; Options = "[--repository-root <path>] [--write]"; Run = ApiBaseline.main }
      { Name = "verify-tooling"; Description = "Run the local release pre-check."; Options = "[--repository-root <path>] [--json] [--skip-docs] [--skip-format]"; Run = tooling }
      { Name = "generate-inventory"; Description = "Regenerate evidence inventories."; Options = "[repository-root] [baseline-root] [ref-pack-dir] [--baseline-root <path>] [--ref-pack-dir <path>] [--output-dir <path>] [--check-inputs]"; Run = Inventory.main }
      { Name = "vertical-slice"; Description = "Verify the package-only vertical slice."; Options = "[--output <path>] [--feed <path>] [--package-feed <url>] [--repository-root <path>] [--no-pack] [--skip-tests] [--skip-measurements] [--json]"; Run = VerticalSlice.main }
      { Name = "verify-performance"; Description = "Verify performance allocation budgets."; Options = "[-RepositoryRoot <path>] [-ReceiptDirectory <path>] [-ObservationProposalPath <path>] [-ManifestPath <path>]"; Run = Performance.main }
      { Name = "generate-performance-docs"; Description = "Generate or verify performance documentation."; Options = "[-RepositoryRoot <path>] [-ManifestPath <path>] [-Verify]"; Run = PerformanceDocs.main }
      { Name = "verify-ruleset"; Description = "Verify required GitHub status checks."; Options = "-RulesetId <id> -ExpectedIntegrationId <id> -OutputPath <path> [-Repository <owner/repo>] [-TargetBranch <branch>]"; Run = Ruleset.main }
      { Name = "compatibility"; Description = "Verify package compatibility consumers."; Options = "-PackageDirectory <path> -OutputDirectory <path> [-RepositoryRoot <path>] [-RuntimeIdentifier <rid>] [-PackageFeed <url>] [-Scenario <name>]"; Run = Compatibility.main }
      { Name = "release"; Description = "Run the canonical release protocol."; Options = "-AttemptId <id> [-OutputDirectory <path>] [-RepositoryRoot <path>] [-CompatibilityRuntimeIdentifier <rid>] [-CompatibilityPackageFeed <url>] [-DistributionFeed <url>] [-SkipBenchmarks]"; Run = ReleaseRun.main }
      { Name = "eval-prep-feed"; Description = "Prepare the coding-evaluation feed."; Options = "[--study <name>]"; Run = adapted [| "prep-feed" |] [ "--study" ] [] [] Evaluation.main }
      { Name = "eval-verify"; Description = "Verify one evaluation solution directory."; Options = "--task <area> --style <style> --run-dir <path> [--round <number>] [--study <name>] [--replay]"; Run = adapted [| "verify" |] [ "--task"; "--style"; "--run-dir"; "--round"; "--study" ] [ "--replay" ] [ "--task"; "--style"; "--run-dir" ] Evaluation.main }
      { Name = "eval-aggregate"; Description = "Aggregate recorded evaluation runs."; Options = "--output <path>"; Run = adapted [| "aggregate" |] [ "--output" ] [] [ "--output" ] Evaluation.main }
      { Name = "rawloc"; Description = "Report call-site line counts."; Options = ""; Run = adapted [||] [] [] [] Loc.main }
      { Name = "loc-extract"; Description = "Extract one named method body."; Options = "--file <path> --method <name>"; Run = adapted [||] [ "--file"; "--method" ] [] [ "--file"; "--method" ] Loc.main } ]

let private printHelp selected =
    printfn "FunnySharp pipelines:"
    for command in selected do
        printfn "  %s: %s" command.Name command.Description
        printfn "    Options: %s" command.Options
        printfn "    dotnet fsi build.fsx -- -p %s --help" command.Name

let main (arguments: string array) =
    let usageError message =
        Console.Error.WriteLine("error: " + message)
        Console.Error.WriteLine "Run: dotnet fsi build.fsx -- --help"
        2
    match List.ofArray arguments with
    | [] | [ "-h" ] | [ "--help" ] -> printHelp commands; 0
    | "-p" :: name :: args ->
        match commands |> List.tryFind (fun command -> command.Name = name) with
        | None -> usageError ("unknown pipeline: " + name)
        | Some command when args |> List.exists (fun arg -> arg = "-h" || arg = "--help") ->
            printHelp [ command ]; 0
        | Some command -> command.Run (Array.ofList args)
    | [ "-p" ] -> usageError "argument -p: expected a pipeline name"
    | argument :: _ -> usageError ("unrecognized argument: " + argument)

exit (main (fsi.CommandLineArgs |> Array.skip 1))
