module FunnySharp.Harness.Program

open Fun.Build
open FunnySharp.Harness
open System
open System.IO

// The single source of truth for the pipeline names the default help prints.
// Every entry registers one pipeline below, in the same order; adding a
// pipeline means adding exactly one entry here and one registration block.
let private pipelineNames =
    [ "build"
      "test"
      "format"
      "check-action-pins"
      "verify-docs-snippets"
      "verify-api-baseline"
      "verify-stable-api-contracts"
      "verify-tooling"
      "generate-inventory"
      "vertical-slice"
      "verify-performance"
      "generate-performance-docs"
      "compare-reproducible-builds"
      "verify-ruleset"
      "compatibility"
      "release"
      "release-verify"
      "eval-prep-feed"
      "eval-verify"
      "eval-aggregate"
      "rawloc"
      "loc-extract" ]
[<EntryPoint>]
let main (arguments: string array) =
    let stdout = Console.Out
    let helpOutput =
        if arguments |> Array.exists (fun argument -> argument = "-h" || argument = "--help") then
            Some(new StringWriter())
        else
            None
    helpOutput |> Option.iter (fun output -> Console.SetOut output)

    // Fun.Build reports any failed step as exit code 1, which would hide the 0/1/2 contract the
    // migrated gates keep from the scripts they replace (1 = verification failure, 2 = usage or
    // environment failure). A gate that reports a non-zero code ends the process with that exact
    // code, after flushing the verdict it just printed.
    let gate (code: int) : Result<unit, string> =
        if code = 0 then
            Ok()
        else
            System.Console.Out.Flush()
            System.Console.Error.Flush()
            System.Environment.Exit code
            Error(sprintf "gate reported exit code %d" code)

    // Command-line flags the migrated gates accept, read back from the pipeline args so each target
    // keeps the surface of the script it replaced. An absent flag reads as the empty string in
    // Fun.Build, so it is dropped instead of passed on.
    let flags (ctx: Fun.Build.Internal.StageContext) (names: string list) =
        names
        |> List.collect (fun name ->
            match ctx.GetCmdArg(name) with
            | "" -> []
            | value -> [ name; value ])

    // Valueless switches cannot be read back with ctx.GetCmdArg: Fun.Build returns the empty string
    // for a flag that carries no value, exactly as it does for an absent one. They are read from the
    // process arguments instead, where `dotnet fsi build.fsx -- -p <target> --flag` keeps `--flag`.
    let processArgs = System.Environment.GetCommandLineArgs()

    let switches (names: string list) =
        names |> List.filter (fun name -> processArgs |> Array.contains name)

    // Positional arguments have no Fun.Build accessor, so they are read from the process arguments:
    // everything after the pipeline target that is neither a named flag nor that flag's value.
    // Inventory.main accepts the same positional form its own help documents.
    let positionals (target: string) =
        let args = processArgs |> Array.toList

        let afterTarget =
            match args |> List.tryFindIndex (fun arg -> arg = target) with
            | Some index -> args |> List.skip (index + 1)
            | None -> []

        let rec collect (acc: string list) (remaining: string list) =
            match remaining with
            | [] -> List.rev acc
            | flag :: value :: rest when flag.StartsWith "--" && not (value.StartsWith "-") -> collect acc rest
            | flag :: rest when flag.StartsWith "-" -> collect acc rest
            | value :: rest -> collect (value :: acc) rest

        collect [] afterTarget

    pipeline "build" {
        description "Build the solution with dotnet build."
        stage "build" { run "dotnet build FunnySharp.slnx" }
        runIfOnlySpecified
    }

    pipeline "test" {
        description "Run the solution's test suites with dotnet test."
        stage "test" { run "dotnet test FunnySharp.slnx" }
        runIfOnlySpecified
    }

    pipeline "format" {
        description "Verify formatting with dotnet format."
        stage "format" {
            run "dotnet format FunnySharp.slnx --verify-no-changes --no-restore"
        }
        runIfOnlySpecified
    }

    pipeline "check-action-pins" {
        description "Check that every remote GitHub action is pinned to a full commit SHA."
        stage "check-action-pins" { run (fun _ -> gate (ActionPins.main [| "--verbose" |])) }
        runIfOnlySpecified
    }

    pipeline "verify-docs-snippets" {
        description "Verify every documentation sample against its source region."
        stage "verify-docs-snippets" {
            run (fun ctx ->
                gate (DocsSnippets.main (Array.ofList (flags ctx [ "--repository-root"; "--samples-root" ]))))
        }
        runIfOnlySpecified
    }

    pipeline "verify-api-baseline" {
        description "Verify the shipping assemblies' public API surface against the committed baseline."
        stage "verify-api-baseline" {
            run (fun ctx ->
                gate (
                    FunnySharp.Harness.ApiBaseline.main (
                        Array.ofList (flags ctx [ "--repository-root" ] @ switches [ "--write" ])
                    )
                ))
        }
        runIfOnlySpecified
    }

    pipeline "verify-stable-api-contracts" {
        description "Verify the exact final stable semantic proof index and its actual evidence."
        stage "verify-stable-api-contracts" {
            run (fun ctx ->
                let path = ctx.GetCmdArg "--proof-index"
                let release = ctx.GetCmdArg "--release-evidence"
                let args = if release <> "" then [| path; release |] elif path <> "" then [| path |] else [||]
                gate (StableApiContracts.main args))
        }
        runIfOnlySpecified
    }

    pipeline "verify-tooling" {
        description "Run the PowerShell-free local pre-check over the release protocol's local steps."
        stage "verify-tooling" {
            run (fun ctx ->
                // The documentation-snippet step is the one check verify_local used to run in a
                // child interpreter; here it is the ported F# verifier, wired through the
                // substitution point the module exposes so its output is captured in-process.
                ToolingVerify.docsVerifier <-
                    Some(fun repositoryRoot ->
                        use out = new System.IO.StringWriter()
                        use err = new System.IO.StringWriter()
                        let code = DocsSnippets.mainWith out err [ "--repository-root"; repositoryRoot ]

                        { ExitCode = code
                          Stdout = out.ToString()
                          Stderr = err.ToString() })

                gate (
                    ToolingVerify.main (
                        Array.ofList (flags ctx [ "--repository-root" ] @ switches [ "--offline"; "--json"; "--skip-docs"; "--skip-format" ])
                    )
                ))
        }
        runIfOnlySpecified
    }

    pipeline "generate-inventory" {
        description "Regenerate the next-stage evidence inventories."
        stage "generate-inventory" {
            run (fun ctx ->
                gate (
                    Inventory.main (
                        Array.ofList (
                            positionals "generate-inventory"
                            @ flags ctx [ "--baseline-root"; "--ref-pack-dir"; "--output-dir" ]
                            @ switches [ "--check-inputs" ]
                        )
                    )
                ))
        }
        runIfOnlySpecified
    }

    pipeline "vertical-slice" {
        description "Pack the solution and verify the vertical slice against the packages only."
        stage "vertical-slice" {
            run (fun ctx ->
                gate (
                    VerticalSlice.main (
                        Array.ofList (
                            flags ctx [ "--output"; "--feed"; "--package-feed"; "--repository-root" ]
                            @ switches [ "--no-pack"; "--skip-tests"; "--skip-measurements"; "--json" ]
                        )
                    )
                ))
        }
        runIfOnlySpecified
    }

    pipeline "verify-performance" {
        description "Verify performance receipts against the manifest's allocation budgets."
        stage "verify-performance" {
            run (fun ctx ->
                gate (
                    Performance.main (
                        Array.ofList (
                            flags ctx
                                [ "-RepositoryRoot"
                                  "-ReceiptDirectory"
                                  "-ObservationProposalPath"
                                  "-ManifestPath" ]
                        )
                    )
                ))
        }
        runIfOnlySpecified
    }

    pipeline "generate-performance-docs" {
        description "Generate or verify the performance documentation regions."
        stage "generate-performance-docs" {
            run (fun ctx ->
                gate (
                    PerformanceDocs.main (
                        Array.ofList (
                            flags ctx [ "-RepositoryRoot"; "-ManifestPath" ]
                            @ switches [ "-Verify" ]
                        )
                    )
                ))
        }
        runIfOnlySpecified
    }

    pipeline "compare-reproducible-builds" {
        description "Compare two build roots for byte-identical output."
        stage "compare-reproducible-builds" {
            run (fun ctx ->
                gate (
                    ReproducibleBuilds.main (Array.ofList (flags ctx [ "-LeftRoot"; "-RightRoot"; "-OutputPath" ]))
                ))
        }
        runIfOnlySpecified
    }

    pipeline "verify-ruleset" {
        description "Verify the repository's GitHub ruleset against the required status checks."
        stage "verify-ruleset" {
            run (fun ctx ->
                gate (
                    Ruleset.main (
                        Array.ofList (flags ctx [ "-RulesetId"; "-ExpectedIntegrationId"; "-OutputPath"; "-RepositoryRoot" ])
                    )
                ))
        }
        runIfOnlySpecified
    }

    pipeline "compatibility" {
        description "Verify the packed packages with the compatibility consumer scenarios."
        stage "compatibility" {
            run (fun ctx ->
                gate (
                    Compatibility.main (
                        Array.ofList (
                            flags ctx
                                [ "-RepositoryRoot"
                                  "-OutputDirectory"
                                  "-PackageDirectory"
                                  "-RuntimeIdentifier"
                                  "-PackageFeed"
                                  "-Scenario" ]
                        )
                    )
                ))
        }
        runIfOnlySpecified
    }

    // The release runner records the verifier command in PowerShell parameter spelling (the protocol
    // it replaces called the PowerShell verifier), while the ported verifier's CLI takes POSIX long
    // flags. This is the one translation point between them.
    let verifierFlagNames =
        [ "-RepositoryRoot", "--repository-root"
          "-OutputDirectory", "--output-directory"
          "-PackageDirectory", "--package-directory"
          "-CompatibilityEvidencePath", "--compatibility-evidence-path"
          "-CompatibilityRuntimeIdentifier", "--compatibility-runtime-identifier"
          "-CompatibilityPackageFeed", "--compatibility-package-feed"
          "-ExecutionEvidenceDirectory", "--execution-evidence-directory"
          "-DocumentationVerifier", "--documentation-verifier"
          "-CompatibilityScript", "--compatibility-script"
          "-Clean", "--clean"
          "-SkipBenchmarks", "--skip-benchmarks" ]

    let toVerifierArgs (arguments: string list) =
        arguments
        |> List.map (fun argument ->
            match verifierFlagNames |> List.tryFind (fun (legacy, _) -> legacy = argument) with
            | Some(_, current) -> current
            | None -> argument)

    pipeline "release" {
        description "Run the canonical release protocol (benchmarkSkipped mode with -SkipBenchmarks)."
        stage "release" {
            run (fun ctx ->
                // The release protocol re-runs its own evidence audit at the end. The PowerShell
                // original re-invoked pwsh through its own executable path, which cannot resolve on
                // this host; here the audit is the ported F# verifier, called in process with its
                // output captured into the same stream the runner records.
                ReleaseRun.verifierRunner <-
                    Some(fun arguments workingDirectory ->
                        use out = new System.IO.StringWriter()
                        use err = new System.IO.StringWriter()

                        let code =
                            ReleaseVerify.mainWith
                                out
                                err
                                workingDirectory
                                (toVerifierArgs arguments)
                                ReleaseVerify.defaultCollaborators

                        { ExitCode = code
                          Stdout = out.ToString()
                          Stderr = err.ToString() })

                gate (
                    ReleaseRun.main (
                        Array.ofList (
                            flags ctx
                                [ "-AttemptId"
                                  "-OutputDirectory"
                                  "-RepositoryRoot"
                                  "-CompatibilityRuntimeIdentifier"
                                  "-CompatibilityPackageFeed"
                                  "-DistributionFeed" ]
                            @ switches [ "-SkipBenchmarks" ]
                        )
                    )
                ))
        }
        runIfOnlySpecified
    }

    pipeline "release-verify" {
        description "Audit a release attempt's evidence tree."
        stage "release-verify" {
            run (fun ctx ->
                gate (
                    ReleaseVerify.main (
                        Array.ofList (
                            flags ctx
                                [ "--output-directory"
                                  "--execution-evidence-directory"
                                  "--package-directory"
                                  "--repository-root"
                                  "--documentation-verifier"
                                  "--compatibility-evidence-path"
                                  "--compatibility-script"
                                  "--compatibility-runtime-identifier"
                                  "--compatibility-package-feed" ]
                            @ switches [ "--skip-benchmarks"; "--clean" ]
                        )
                    )
                ))
        }
        runIfOnlySpecified
    }

    pipeline "release-provenance" {
        description "Freeze aggregate release evidence, stage independent attestation, or publish its external index."
        stage "release-provenance" {
            run (fun ctx ->
                gate (
                    ReleaseProvenance.main (
                        Array.ofList (flags ctx [ "-Mode"; "-InputPath"; "-OutputDirectory"; "-ArtifactUrl" ])
                    )
                ))
        }
        runIfOnlySpecified
    }

    pipeline "eval-prep-feed" {
        description "Prepare the evaluation feed used by the coding-evaluation harness."
        stage "eval-prep-feed" {
            run (fun ctx -> gate (Evaluation.main (Array.ofList ([ "prep-feed" ] @ flags ctx [ "--study" ]))))
        }
        runIfOnlySpecified
    }

    pipeline "eval-verify" {
        description "Verify one evaluation solution directory and write its record.json."
        stage "eval-verify" {
            run (fun ctx ->
                let round = ctx.GetCmdArg("--round")

                gate (
                    Evaluation.main (
                        Array.ofList (
                            [ "verify"
                              ctx.GetCmdArg("--task")
                              ctx.GetCmdArg("--style")
                              ctx.GetCmdArg("--run-dir") ]
                            @ (if round = "" then [] else [ "--round"; round ])
                            @ flags ctx [ "--study" ]
                            @ switches [ "--replay" ]
                        )
                    )
                ))
        }
        runIfOnlySpecified
    }

    pipeline "eval-aggregate" {
        description "Aggregate the recorded evaluation runs into one markdown table."
        stage "eval-aggregate" {
            run (fun ctx -> gate (Evaluation.main [| "aggregate"; ctx.GetCmdArg("--output") |]))
        }
        runIfOnlySpecified
    }

    pipeline "rawloc" {
        description "Report the raw line counts of the call-site targets."
        stage "rawloc" { run (fun _ -> gate (Loc.main [||])) }
        runIfOnlySpecified
    }

    pipeline "loc-extract" {
        description "Extract one method body from a call-site file by name."
        stage "loc-extract" {
            run (fun ctx -> gate (Loc.main [| ctx.GetCmdArg("--file"); ctx.GetCmdArg("--method") |]))
        }
        runIfOnlySpecified
    }

    pipeline "default" {
        description "Print the available pipelines."
        stage "help" {
            run (fun _ ->
                printfn "FunnySharp pipelines:"
                for name in pipelineNames do
                    printfn "  %s" name
                printfn ""
                printfn "Run one with:"
                printfn "  dotnet fsi build.fsx -- -p <name>")
        }
        // Fun.Build 1.2.0 registers a pipeline only through runIfOnlySpecified;
        // `false` marks this one as the pipeline a bare run executes.
        runIfOnlySpecified false
    }

    tryPrintPipelineCommandHelp ()

    // Fun.Build infers an FSI filename even for compiled callers; the public launcher owns this command prefix.
    match helpOutput with
    | Some output ->
        Console.SetOut stdout
        stdout.Write(output.ToString().Replace("dotnet fsi your_script.fsx", "dotnet fsi build.fsx", StringComparison.Ordinal))
        output.Dispose()
    | None -> ()

    0
