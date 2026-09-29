module FunnySharp.Harness.ReleaseVerify

// A behaviour-identical F# port of eng/Verify-Release.ps1 (lane C): the
// heavyweight release-evidence audit. It runs ten checks in a fixed order,
// accumulates `{name,status,details}` and fails closed with remediation text,
// writes release-evidence.json / release-evidence.md and exits 0/1. It never
// mutates the evidence tree it verifies.
//
// Deviations recorded rather than silent:
//  * The documentation and compatibility steps run the ported F# verifiers
//    in-process through injectable runners; the original launched pwsh.
//  * DOTNET_CLI_UI_LANGUAGE=en is forced for this process (and every child),
//    because the captured build/test verdicts are English literals.
//  * Exit codes are exactly 0 pass / 1 any failure (the PowerShell surface had
//    no usage code).

open System
open System.IO
open System.Runtime.InteropServices
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open System.Text.RegularExpressions
open FunnySharp.Harness.Output
open FunnySharp.Harness.Repo
open FunnySharp.Harness.ReleaseVerifySource
open FunnySharp.Harness.ReleaseVerifyArtifacts

[<Literal>]
let SuccessPrefix = "Release evidence verification passed. Output: "

[<Literal>]
let DefaultCompatibilityPackageFeed = "https://api.nuget.org/v3/index.json"

/// The verifier steps that were PowerShell child processes now run through these
/// in-process runners; tests inject stubs.
type Collaborators =
    { DocumentationRunner: DocumentationRunner
      CompatibilityRunner: CompatibilityRunner }

let defaultCollaborators: Collaborators =
    { DocumentationRunner = defaultDocumentationRunner
      CompatibilityRunner = defaultCompatibilityRunner }

type CliOptions =
    { RepositoryRoot: string option
      OutputDirectory: string option
      PackageDirectory: string option
      DocumentationVerifier: string option
      CompatibilityEvidencePath: string option
      CompatibilityScript: string option
      CompatibilityRuntimeIdentifier: string option
      CompatibilityPackageFeed: string
      ExecutionEvidenceDirectory: string option
      SkipBenchmarks: bool
      Clean: bool
      Help: bool }

let private emptyOptions: CliOptions =
    { RepositoryRoot = None
      OutputDirectory = None
      PackageDirectory = None
      DocumentationVerifier = None
      CompatibilityEvidencePath = None
      CompatibilityScript = None
      CompatibilityRuntimeIdentifier = None
      CompatibilityPackageFeed = DefaultCompatibilityPackageFeed
      ExecutionEvidenceDirectory = None
      SkipBenchmarks = false
      Clean = false
      Help = false }

let private usageLine =
    "usage: Verify-Release.ps1 --output-directory <dir> --execution-evidence-directory <dir> "
    + "[--repository-root <dir>] [--package-directory <dir>] [--documentation-verifier <path>] "
    + "[--compatibility-evidence-path <path>] [--compatibility-script <path>] "
    + "[--compatibility-runtime-identifier <rid>] [--compatibility-package-feed <feed>] "
    + "[--skip-benchmarks] [--clean]"

let private helpText =
    String.concat
        "\n"
        [ usageLine
          ""
          "Collect reproducible release evidence for FunnySharp packages."
          ""
          "options:"
          "  -h, --help                             show this help message and exit"
          "  --output-directory <dir>               evidence output directory (required)."
          "  --execution-evidence-directory <dir>   Run-Release output directory (required)."
          "  --repository-root <dir>                repository root (default: resolved from the start directory)."
          "  --package-directory <dir>              directory holding the packed packages (default: <root>/artifacts/packages)."
          "  --documentation-verifier <path>        documentation-snippet verifier (default: the samples verifier)."
          "  --compatibility-evidence-path <path>   an existing compatibility-results.json to accept."
          "  --compatibility-script <path>          a compatibility verifier to run (mutually exclusive with the evidence path)."
          "  --compatibility-runtime-identifier <rid>  expected compatibility RID (default: host RID)."
          "  --compatibility-package-feed <feed>    upstream NuGet feed used by the compatibility script."
          "  --skip-benchmarks                      require benchmarkSkipped evidence."
          "  --clean                                replace the output directory contents." ]

let private parseArgs (argv: string list) : Result<CliOptions, string> =
    let rec loop (options: CliOptions) (remaining: string list) =
        match remaining with
        | [] -> Ok options
        | argument :: rest ->
            let separator =
                let equals = argument.IndexOf '='

                if equals > 0 then equals else -1

            let name, inlineValue =
                if separator > 0 then
                    argument.Substring(0, separator), Some(argument.Substring(separator + 1))
                else
                    argument, None

            let takeValue (apply: string -> CliOptions) =
                match inlineValue with
                | Some value -> loop (apply value) rest
                | None ->
                    match rest with
                    | value :: tail -> loop (apply value) tail
                    | [] -> Error(sprintf "argument %s: expected one argument" name)

            match name with
            | "-h"
            | "--help" -> Ok { options with Help = true }
            | "--skip-benchmarks" -> loop { options with SkipBenchmarks = true } rest
            | "--clean" -> loop { options with Clean = true } rest
            | "--repository-root" -> takeValue (fun value -> { options with RepositoryRoot = Some value })
            | "--output-directory" -> takeValue (fun value -> { options with OutputDirectory = Some value })
            | "--package-directory" -> takeValue (fun value -> { options with PackageDirectory = Some value })
            | "--documentation-verifier" ->
                takeValue (fun value -> { options with DocumentationVerifier = Some value })
            | "--compatibility-evidence-path" ->
                takeValue (fun value -> { options with CompatibilityEvidencePath = Some value })
            | "--compatibility-script" ->
                takeValue (fun value -> { options with CompatibilityScript = Some value })
            | "--compatibility-runtime-identifier" ->
                takeValue (fun value -> { options with CompatibilityRuntimeIdentifier = Some value })
            | "--compatibility-package-feed" ->
                takeValue (fun value -> { options with CompatibilityPackageFeed = value })
            | "--execution-evidence-directory" ->
                takeValue (fun value -> { options with ExecutionEvidenceDirectory = Some value })
            | _ -> Error("unrecognized arguments: " + argument)

    loop emptyOptions argv

let private asNode (node: JsonNode option) : JsonNode =
    match node with
    | Some value -> value
    | None -> jsonNull

let private asObject (node: JsonNode | null) (description: string) : JsonObject =
    match node with
    | :? JsonObject as value -> value
    | _ -> failNow (sprintf "%s is missing or malformed." description)

/// The verifier check accumulator: it records every check and never
/// short-circuits on the first failure.
type private CheckLog() =
    let checks = ResizeArray<string * string * JsonNode>()
    let failures = ResizeArray<string>()

    member _.Checks = List.ofSeq checks
    member _.Failures = List.ofSeq failures

    member _.Invoke(name: string, action: unit -> JsonNode) : JsonNode option =
        try
            let details = action ()
            checks.Add(name, "passed", details)
            Some details
        with
        | ReleaseVerifyFailure message ->
            checks.Add(name, "failed", jstr message :> JsonNode)
            failures.Add(name + ": " + message)
            None
        | ex ->
            checks.Add(name, "failed", jstr ex.Message :> JsonNode)
            failures.Add(name + ": " + ex.Message)
            None

/// Run the verifier writing to the supplied writers. Returns the exit code.
let mainWith
    (stdout: TextWriter)
    (stderr: TextWriter)
    (startDirectory: string)
    (argv: string list)
    (collaborators: Collaborators)
    : int =
    // The captured build/test verdicts are English literals, so every child must
    // emit English UI text.
    Environment.SetEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE", "en")

    match parseArgs argv with
    | Error message ->
        stderr.WriteLine usageLine
        stderr.WriteLine("Verify-Release.ps1: error: " + message)
        1
    | Ok options when options.Help ->
        stdout.WriteLine helpText
        0
    | Ok options ->
        let repositoryRoot =
            match options.RepositoryRoot with
            | Some root -> Path.GetFullPath root
            | None ->
                match tryFindRootFrom startDirectory with
                | Some root -> root
                | None -> Path.GetFullPath startDirectory

        let requestExitCode (action: unit -> int) : int =
            try
                action ()
            with
            | ReleaseVerifyFailure message ->
                stderr.WriteLine("Verify-Release.ps1: error: " + message)
                1
            | ex ->
                stderr.WriteLine("Verify-Release.ps1: error: " + ex.Message)
                1

        if options.OutputDirectory.IsNone || options.ExecutionEvidenceDirectory.IsNone then
            stderr.WriteLine usageLine

            stderr.WriteLine(
                "Verify-Release.ps1: error: the --output-directory and --execution-evidence-directory parameters are required."
            )

            1
        elif not (Directory.Exists repositoryRoot) then
            stderr.WriteLine(sprintf "Verify-Release.ps1: error: RepositoryRoot was not found: '%s'." repositoryRoot)
            1
        else
            requestExitCode (fun () ->
                let artifactsDirectory = Path.Combine(repositoryRoot, "artifacts")

                if Directory.Exists artifactsDirectory then
                    let attributes = File.GetAttributes artifactsDirectory

                    if hasFlag FileAttributes.ReparsePoint attributes then
                        failNow (
                            sprintf "ArtifactsDirectory '%s' cannot be a reparse point." (Path.GetFullPath artifactsDirectory)
                        )

                Directory.CreateDirectory artifactsDirectory |> ignore
                let artifactsDirectory = Path.GetFullPath artifactsDirectory

                let outputDirectory =
                    resolveFullPath repositoryRoot (defaultArg options.OutputDirectory "")

                let outputDirectory =
                    initializeEvidenceDirectory outputDirectory artifactsDirectory options.Clean

                let packageDirectory =
                    match options.PackageDirectory with
                    | Some value when not (String.IsNullOrWhiteSpace value) ->
                        resolveFullPath repositoryRoot value
                    | _ -> Path.Combine(repositoryRoot, "artifacts/packages")

                let documentationVerifier =
                    match options.DocumentationVerifier with
                    | Some value when not (String.IsNullOrWhiteSpace value) ->
                        resolveFullPath repositoryRoot value
                    | _ ->
                        // The documentation verifier is the ported F# module, run in process by
                        // defaultDocumentationRunner; this default only has to name a real file
                        // for the existence check below.
                        Path.Combine(repositoryRoot, "eng/harness/DocsSnippets.fs")

                let compatibilityEvidencePath =
                    options.CompatibilityEvidencePath
                    |> Option.map (fun value -> resolveFullPath repositoryRoot value)

                let compatibilityScript =
                    options.CompatibilityScript |> Option.map (fun value -> resolveFullPath repositoryRoot value)

                let executionEvidenceDirectory =
                    resolveFullPath repositoryRoot (defaultArg options.ExecutionEvidenceDirectory "")

                let compatibilityRuntimeIdentifier =
                    defaultArg options.CompatibilityRuntimeIdentifier RuntimeInformation.RuntimeIdentifier

                let checks = CheckLog()

                let environment =
                    checks.Invoke(
                        "Environment capture",
                        fun () ->
                            let value = getEnvironmentEvidence repositoryRoot
                            writeJsonFile (Path.Combine(outputDirectory, "environment.json")) value
                            value
                    )

                let executionEvidence =
                    checks.Invoke(
                        "Release execution evidence",
                        fun () ->
                            assertReleaseExecutionEvidence
                                executionEvidenceDirectory
                                artifactsDirectory
                                repositoryRoot
                                options.SkipBenchmarks
                                options.CompatibilityPackageFeed
                                compatibilityRuntimeIdentifier
                            :> JsonNode
                    )

                let apiInventory =
                    checks.Invoke(
                        "Public API inventory",
                        fun () ->
                            let assemblyPaths =
                                [ Path.Combine(repositoryRoot, "src/FunnySharp/bin/Release/net10.0/FunnySharp.dll")
                                  Path.Combine(
                                      repositoryRoot,
                                      "src/FunnySharp.AspNetCore/bin/Release/net10.0/FunnySharp.AspNetCore.dll"
                                  ) ]

                            for assemblyPath in assemblyPaths do
                                if not (File.Exists assemblyPath) then
                                    failNow (sprintf "Release assembly was not found: '%s'." assemblyPath)

                            let value = getPublicApiInventory assemblyPaths repositoryRoot

                            let notTrimmable =
                                [ for assembly in value do
                                      match assembly with
                                      | :? JsonObject as assemblyObject ->
                                          match assemblyObject.["isTrimmable"] with
                                          | :? JsonValue as trimmable when not (trimmable.GetValue<bool>()) ->
                                              match assemblyObject.["identity"] with
                                              | :? JsonValue as identity -> identity.ToString()
                                              | _ -> ""
                                          | _ -> ()
                                      | _ -> () ]

                            if not notTrimmable.IsEmpty then
                                failNow (
                                    sprintf
                                        "Shipping assemblies must declare IsTrimmable=True: %s."
                                        (String.concat ", " notTrimmable)
                                )

                            writeJsonFile (Path.Combine(outputDirectory, "public-api.json")) value

                            File.WriteAllLines(
                                Path.Combine(outputDirectory, "public-api.txt"),
                                renderPublicApiText value,
                                UTF8Encoding(false)
                            )

                            let baselineMessages =
                                FunnySharp.Harness.ApiBaseline.outdatedBaselineMessages repositoryRoot assemblyPaths

                            if not baselineMessages.IsEmpty then
                                failNow (String.concat " " baselineMessages)

                            let summary = JsonObject()
                            summary.["assemblies"] <- jint value.Count
                            summary.["output"] <- jstr "public-api.json"
                            summary.["baseline"] <- jstr "match"
                            summary :> JsonNode
                    )

                let xmlDocumentation =
                    checks.Invoke(
                        "XML documentation",
                        fun () ->
                            let paths =
                                [ Path.Combine(repositoryRoot, "src/FunnySharp/bin/Release/net10.0/FunnySharp.xml")
                                  Path.Combine(
                                      repositoryRoot,
                                      "src/FunnySharp.AspNetCore/bin/Release/net10.0/FunnySharp.AspNetCore.xml"
                                  ) ]

                            let value = getXmlDocumentationInventory paths
                            writeJsonFile (Path.Combine(outputDirectory, "xml-documentation.json")) value

                            let members =
                                value
                                |> Seq.sumBy (fun item ->
                                    match item with
                                    | :? JsonObject as itemObject ->
                                        match itemObject.["members"] with
                                        | :? JsonValue as memberCount -> memberCount.GetValue<int>()
                                        | _ -> 0
                                    | _ -> 0)

                            let summary = JsonObject()
                            summary.["files"] <- jint value.Count
                            summary.["members"] <- jint members
                            summary.["output"] <- jstr "xml-documentation.json"
                            summary :> JsonNode
                    )

                let mutable packages: JsonObject option = None

                let packageSet =
                    checks.Invoke(
                        "Package archive set",
                        fun () ->
                            if not (Directory.Exists packageDirectory) then
                                failNow (sprintf "Package directory was not found: '%s'." packageDirectory)

                            let allFiles =
                                Directory.GetFiles packageDirectory
                                |> Array.sortWith (fun left right ->
                                    StringComparer.OrdinalIgnoreCase.Compare(fileNameOf left, fileNameOf right))
                                |> List.ofArray

                            let nupkgs =
                                allFiles
                                |> List.filter (fun path ->
                                    path.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase)
                                    && not (path.EndsWith(".snupkg", StringComparison.OrdinalIgnoreCase)))

                            let snupkgs =
                                allFiles
                                |> List.filter (fun path -> path.EndsWith(".snupkg", StringComparison.OrdinalIgnoreCase))

                            if nupkgs.Length <> 2 || snupkgs.Length <> 2 then
                                failNow (
                                    sprintf
                                        "Expected exactly two .nupkg and two .snupkg archives; found %d and %d."
                                        nupkgs.Length
                                        snupkgs.Length
                                )

                            let core = findPackageArchive nupkgs "FunnySharp" "nupkg"
                            let aspNetCore = findPackageArchive nupkgs "FunnySharp.AspNetCore" "nupkg"
                            let coreSymbols = findPackageArchive snupkgs "FunnySharp" "snupkg"
                            let aspNetCoreSymbols = findPackageArchive snupkgs "FunnySharp.AspNetCore" "snupkg"

                            let expectedCoreSymbols =
                                Regex.Replace(fileNameOf core, "\\.nupkg$", ".snupkg")

                            let expectedAspNetCoreSymbols =
                                Regex.Replace(fileNameOf aspNetCore, "\\.nupkg$", ".snupkg")

                            if
                                not (equalsOrdinal (fileNameOf coreSymbols) expectedCoreSymbols)
                                || not (equalsOrdinal (fileNameOf aspNetCoreSymbols) expectedAspNetCoreSymbols)
                            then
                                failNow "Each symbol package must match the version of its corresponding NuGet package."

                            let value = JsonObject()
                            value.["core"] <- getPackageInspection core
                            value.["aspNetCore"] <- getPackageInspection aspNetCore

                            let symbolPackages = JsonArray()

                            for symbol in [ coreSymbols; aspNetCoreSymbols ] do
                                let symbolNode = JsonObject()
                                symbolNode.["fileName"] <- jstr (fileNameOf symbol)
                                symbolNode.["path"] <- jstr symbol
                                symbolNode.["sha256"] <- jstr (sha256File symbol)
                                symbolPackages.Add symbolNode

                            value.["symbolPackages"] <- symbolPackages
                            writeJsonFile (Path.Combine(outputDirectory, "package-inventory.json")) value
                            packages <- Some value

                            let summary = JsonObject()
                            summary.["nupkgCount"] <- jint nupkgs.Length
                            summary.["snupkgCount"] <- jint snupkgs.Length
                            summary.["output"] <- jstr "package-inventory.json"
                            summary :> JsonNode
                    )

                let coreLayout =
                    checks.Invoke(
                        "FunnySharp package layout and dependencies",
                        fun () ->
                            match packages with
                            | None -> failNow "Package archive inspection did not complete."
                            | Some inventory ->
                                let core = asObject inventory.["core"] "Package archive core inspection"

                                use document = JsonDocument.Parse(serializeJson core)

                                assertPackageLayout
                                    document.RootElement
                                    "FunnySharp"
                                    (Path.Combine(
                                        repositoryRoot,
                                        "src/FunnySharp/bin/Release/net10.0/FunnySharp.dll"
                                    ))
                                    (Path.Combine(
                                        repositoryRoot,
                                        "src/FunnySharp/bin/Release/net10.0/FunnySharp.xml"
                                    ))
                                    (Path.Combine(repositoryRoot, "README.md"))

                                assertCorePackageDependencies document.RootElement

                                let summary = JsonObject()
                                summary.["package"] <- jstr (propText "fileName" document.RootElement)
                                summary.["version"] <- jstr (propText "version" document.RootElement)
                                summary :> JsonNode
                    )

                let aspNetCoreLayout =
                    checks.Invoke(
                        "FunnySharp.AspNetCore package layout and dependencies",
                        fun () ->
                            match packages with
                            | None -> failNow "Package archive inspection did not complete."
                            | Some inventory ->
                                let core = asObject inventory.["core"] "Package archive core inspection"

                                let aspNetCore =
                                    asObject inventory.["aspNetCore"] "Package archive ASP.NET Core inspection"

                                use coreDocument = JsonDocument.Parse(serializeJson core)
                                use document = JsonDocument.Parse(serializeJson aspNetCore)

                                assertPackageLayout
                                    document.RootElement
                                    "FunnySharp.AspNetCore"
                                    (Path.Combine(
                                        repositoryRoot,
                                        "src/FunnySharp.AspNetCore/bin/Release/net10.0/FunnySharp.AspNetCore.dll"
                                    ))
                                    (Path.Combine(
                                        repositoryRoot,
                                        "src/FunnySharp.AspNetCore/bin/Release/net10.0/FunnySharp.AspNetCore.xml"
                                    ))
                                    (Path.Combine(repositoryRoot, "README.md"))

                                assertAspNetCorePackageDependencies
                                    document.RootElement
                                    (propText "version" coreDocument.RootElement)

                                let summary = JsonObject()
                                summary.["package"] <- jstr (propText "fileName" document.RootElement)
                                summary.["version"] <- jstr (propText "version" document.RootElement)
                                summary :> JsonNode
                    )

                let documentation =
                    checks.Invoke(
                        "Documentation snippets",
                        fun () ->
                            invokeDocumentationVerification
                                collaborators.DocumentationRunner
                                documentationVerifier
                                repositoryRoot
                                outputDirectory
                            :> JsonNode
                    )

                let runCompatibility (inventory: JsonElement) : JsonObject =
                    invokeCompatibilityVerification
                        collaborators.CompatibilityRunner
                        compatibilityScript
                        compatibilityEvidencePath
                        repositoryRoot
                        outputDirectory
                        packageDirectory
                        compatibilityRuntimeIdentifier
                        options.CompatibilityPackageFeed
                        inventory

                let compatibility =
                    checks.Invoke(
                        "Compatibility evidence",
                        fun () ->
                            match packages with
                            | None -> runCompatibility (JsonElement()) :> JsonNode
                            | Some value ->
                                use document = JsonDocument.Parse(serializeJson value)
                                runCompatibility document.RootElement :> JsonNode
                    )

                let sourceFingerprint =
                    checks.Invoke(
                        "Source fingerprint unchanged during verification",
                        fun () ->
                            match executionEvidence with
                            | None -> failNow "Release execution evidence did not validate."
                            | Some details ->
                                let currentFingerprint = getSourceFingerprint repositoryRoot

                                match details with
                                | :? JsonObject as detailsObject ->
                                    match detailsObject.["sourceFingerprint"] with
                                    | :? JsonObject as expected ->
                                        use expectedDocument = JsonDocument.Parse(serializeJson expected)
                                        use currentDocument = JsonDocument.Parse(serializeJson currentFingerprint)

                                        if
                                            not (
                                                equivalentSourceFingerprint
                                                    expectedDocument.RootElement
                                                    currentDocument.RootElement
                                            )
                                        then
                                            failNow "Source files changed while release verification ran."

                                        currentFingerprint :> JsonNode
                                    | _ -> failNow "Release execution evidence did not validate."
                                | _ -> failNow "Release execution evidence did not validate."
                    )

                // The package-set, layout and fingerprint checks record their own
                // verdicts; their bindings are kept only to keep the run ordered.
                ignore (packageSet, coreLayout, aspNetCoreLayout, sourceFingerprint)

                let checksArray = JsonArray()

                for name, status, details in checks.Checks do
                    let checkNode = JsonObject()
                    checkNode.["name"] <- jstr name
                    checkNode.["status"] <- jstr status
                    // A JsonNode can only have one parent, so the same details value
                    // stored in the top-level report is cloned here.
                    checkNode.["details"] <- JsonNode.Parse(details.ToJsonString())
                    checksArray.Add checkNode

                let failures = checks.Failures
                let failuresArray = JsonArray()

                for failure in failures do
                    failuresArray.Add(jstr failure)

                let releaseEvidence = JsonObject()
                releaseEvidence.["schemaVersion"] <- jint 2
                releaseEvidence.["succeeded"] <- jbool failures.IsEmpty
                releaseEvidence.["environment"] <- asNode environment
                releaseEvidence.["executionEvidence"] <- asNode executionEvidence
                releaseEvidence.["apiInventory"] <- asNode apiInventory
                releaseEvidence.["xmlDocumentation"] <- asNode xmlDocumentation
                releaseEvidence.["packageInventory"] <-
                    (if packages.IsNone then jsonNull else jstr "package-inventory.json")
                releaseEvidence.["documentation"] <- asNode documentation
                releaseEvidence.["compatibility"] <- asNode compatibility
                releaseEvidence.["checks"] <- checksArray
                releaseEvidence.["failures"] <- failuresArray

                writeJsonFile (Path.Combine(outputDirectory, "release-evidence.json")) releaseEvidence

                let summary = ResizeArray<string>()
                summary.Add "# FunnySharp Release Evidence"
                summary.Add ""
                summary.Add ("Status: " + (if failures.IsEmpty then "PASSED" else "FAILED"))
                summary.Add ""
                summary.Add "## Checks"

                for name, status, _ in checks.Checks do
                    summary.Add("- [" + (if status = "passed" then "x" else " ") + "] " + name)

                if not failures.IsEmpty then
                    summary.Add ""
                    summary.Add "## Failures"

                    for failure in failures do
                        summary.Add("- " + failure)

                File.WriteAllLines(
                    Path.Combine(outputDirectory, "release-evidence.md"),
                    summary,
                    UTF8Encoding(false)
                )

                if not failures.IsEmpty then
                    stderr.WriteLine(
                        sprintf "Release evidence verification failed. See '%s\\\\release-evidence.md'." outputDirectory
                    )

                    1
                else
                    stdout.WriteLine(SuccessPrefix + outputDirectory)
                    0
            )

/// Entry point mirroring eng/Verify-Release.ps1's parameter surface.
let main (argv: string array) : int =
    mainWith Console.Out Console.Error Environment.CurrentDirectory (List.ofArray argv) defaultCollaborators
