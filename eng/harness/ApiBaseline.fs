module FunnySharp.Harness.ApiBaseline

// Verifies the shipping assemblies' public API surface against the committed
// baseline under eng/api-baseline, the committed half of the stability boundary
// docs/product-contract.md defines (EnablePackageValidation plus a committed API
// baseline). The surface is rendered only through the release audit's
// shared reflection and rendering path - ReleaseVerifyArtifacts.getPublicApiText,
// one assembly per file, without an unused byte digest - so every committed file is an
// `ASSEMBLY <identity>` line followed by that assembly's `KIND <TypeName>` and
// indented member lines, byte-identical to what Verify-Release writes to
// public-api.txt.
//
// Verify mode reads and compares only; --write is the single writer, and it
// refreshes the committed files from the built assemblies. The process contract:
//   0 - every shipping assembly matches its committed baseline (or was written)
//   1 - verification failure: drift from the baseline, or a missing baseline file
//   2 - usage failure (unknown flag) or environment failure (an assembly that has
//       not been built)
// Failure lines go to stderr prefixed 'error: '; the drift remediation names the
// command that may refresh the baseline only for an accepted goal.

open System
open System.Collections.Generic
open System.IO
open System.Text
open FunnySharp.Harness.Repo
open FunnySharp.Harness.ReleaseVerifyArtifacts

/// The directory, relative to the repository root, that holds the committed baselines.
[<Literal>]
let BaselineDirectoryName = "api-baseline"

/// Absolute path of the committed-baseline directory: <root>/eng/api-baseline.
let baselineDirectory (repositoryRoot: string) : string =
    Path.Combine(repositoryRoot, "eng", BaselineDirectoryName)

/// Absolute path of one assembly's committed baseline file: <baselineDirectory>/<simpleName>.public-api.txt
let baselineFileName (repositoryRoot: string) (assemblySimpleName: string) : string =
    Path.Combine(baselineDirectory repositoryRoot, assemblySimpleName + ".public-api.txt")

/// The two shipping assemblies: (simpleName, absolute path) for FunnySharp and
/// FunnySharp.AspNetCore at src/<name>/bin/Release/net10.0/<name>.dll, in that
/// fixed order.
let shippingAssemblies (repositoryRoot: string) : (string * string) list =
    // The relative part stays one forward-slash segment: the release audit records
    // these path strings verbatim in public-api.json, so the spelling is part of
    // that evidence on every host.
    let assemblyPath (name: string) : string =
        Path.Combine(repositoryRoot, sprintf "src/%s/bin/Release/net10.0/%s.dll" name name)

    [ "FunnySharp", assemblyPath "FunnySharp"
      "FunnySharp.AspNetCore", assemblyPath "FunnySharp.AspNetCore" ]

/// Lines present in one rendering but not the other, rendered as "- <line>"
/// (expected only) and "+ <line>" (actual only), in the order the lines appear.
/// Empty when the two are equal.
let diffLines (expected: string list) (actual: string list) : string list =
    let expectedSet = HashSet<string>(expected, StringComparer.Ordinal)
    let actualSet = HashSet<string>(actual, StringComparer.Ordinal)

    let removed =
        expected
        |> List.filter (fun line -> not (actualSet.Contains line))
        |> List.map (fun line -> "- " + line)

    let added =
        actual
        |> List.filter (fun line -> not (expectedSet.Contains line))
        |> List.map (fun line -> "+ " + line)

    removed @ added

// ---------------------------------------------------------------------------
// Rendering, comparison and counting
// ---------------------------------------------------------------------------

let private utf8NoBom = UTF8Encoding(false)

/// Render one assembly's public API through the release audit's rendering path.
/// A single-element assembly list keeps each rendering self-contained: its own
/// `ASSEMBLY` line followed by that assembly's types and members.
let private renderAssembly (repositoryRoot: string) (assemblyPath: string) : string list =
    getPublicApiText [ assemblyPath ] repositoryRoot

/// Read one committed baseline (BOM-aware, universal newlines) as its lines.
let private readBaselineLines (path: string) : string list =
    File.ReadAllLines(path, utf8NoBom) |> List.ofArray

/// (type count, member count) over one assembly's rendered lines, counted from the
/// very lines that get compared: renderPublicApiText writes one 'ASSEMBLY' line per
/// assembly, one unindented 'KIND <name>' line per type, and one two-space-indented
/// line per member - so the counts cannot describe a different surface than the
/// comparison does, and a new member kind is counted without a second list to keep
/// in step with the renderer.
let private surfaceCounts (rendered: string list) : int * int =
    let isAssemblyLine (line: string) = line.StartsWith("ASSEMBLY ", StringComparison.Ordinal)
    let isMemberLine (line: string) = line.StartsWith("  ", StringComparison.Ordinal)

    let typeCount =
        rendered
        |> List.filter (fun line -> not (isAssemblyLine line) && not (isMemberLine line))
        |> List.length

    let memberCount = rendered |> List.filter isMemberLine |> List.length

    typeCount, memberCount

/// The diff between one assembly's rendered surface and its committed baseline, or the
/// message saying the baseline file is missing. Nothing is rendered when that file is
/// missing: the file system decides this branch, never a loaded assembly.
let private baselineMismatch
    (repositoryRoot: string)
    (simpleName: string)
    (renderSurface: unit -> string list)
    : string option =
    let baselinePath = baselineFileName repositoryRoot simpleName

    if not (File.Exists baselinePath) then
        Some(sprintf "Public API baseline file is missing: '%s'." baselinePath)
    else
        let diff = diffLines (readBaselineLines baselinePath) (renderSurface ())

        if diff.IsEmpty then
            None
        else
            Some(
                sprintf
                    "Public API surface for '%s' differs from the committed baseline '%s':\n%s"
                    simpleName
                    baselinePath
                    (String.concat "\n" diff)
            )

/// Every baseline mismatch, one message per assembly in the given order. Empty when every
/// assembly matches its committed baseline in eng/api-baseline/. The release audit's entry
/// point.
let outdatedBaselineMessages (repositoryRoot: string) (assemblies: (string * string) list) : string list =
    assemblies
    |> List.choose (fun (simpleName, assemblyPath) ->
        baselineMismatch repositoryRoot simpleName (fun () -> renderAssembly repositoryRoot assemblyPath))

// ---------------------------------------------------------------------------
// Command line
// ---------------------------------------------------------------------------

type private CliOptions =
    { RepositoryRoot: string option
      Write: bool }

let private usageLine = "usage: verify-api-baseline [--repository-root <path>] [--write]"

let private relativeBaselineDirectory = "eng/" + BaselineDirectoryName

let private parseArgs (argv: string list) : Result<CliOptions, string> =
    let rec loop (options: CliOptions) (remaining: string list) =
        match remaining with
        | [] -> Ok options
        | "--write" :: rest -> loop { options with Write = true } rest
        | "--repository-root" :: value :: rest -> loop { options with RepositoryRoot = Some value } rest
        | [ "--repository-root" ] -> Error "argument --repository-root: expected one argument"
        | argument :: rest when argument.StartsWith("--repository-root=") ->
            loop { options with RepositoryRoot = Some(argument.Substring("--repository-root=".Length)) } rest
        | argument :: _ -> Error("unrecognized arguments: " + argument)

    loop { RepositoryRoot = None; Write = false } argv

let private resolveRepositoryRoot (options: CliOptions) : string =
    match options.RepositoryRoot with
    | Some root -> Path.GetFullPath root
    | None ->
        match tryFindRoot () with
        | Some root -> root
        | None -> Path.GetFullPath Environment.CurrentDirectory

/// Run the verifier (or the baseline writer) against the supplied writers.
/// Returns 0 verified, 1 verification failure, 2 usage or environment failure.
let mainWith (stdout: TextWriter) (stderr: TextWriter) (argv: string list) : int =
    match parseArgs argv with
    | Error message ->
        stderr.WriteLine usageLine
        stderr.WriteLine("error: " + message)
        2
    | Ok options ->
        let repositoryRoot = resolveRepositoryRoot options
        let assemblies = shippingAssemblies repositoryRoot

        let notBuilt =
            assemblies |> List.filter (fun (_, path) -> not (File.Exists path))

        if not notBuilt.IsEmpty then
            for name, path in notBuilt do
                stderr.WriteLine(sprintf "error: the %s shipping assembly has not been built: '%s'." name path)

            stderr.WriteLine(
                "error: build the shipping assemblies with 'dotnet build FunnySharp.slnx -c Release' first."
            )

            2
        else
            // One reflection pass per assembly, shared by writing, comparing and counting.
            let rendered =
                [ for name, path in assemblies -> name, path, renderAssembly repositoryRoot path ]

            let successLine () =
                let typeCount, memberCount =
                    rendered
                    |> List.map (fun (_, _, lines) -> surfaceCounts lines)
                    |> List.fold (fun (types, members) (moreTypes, moreMembers) -> types + moreTypes, members + moreMembers) (0, 0)

                sprintf
                    "Verified %d types and %d members across %d assemblies against %s."
                    typeCount
                    memberCount
                    assemblies.Length
                    relativeBaselineDirectory

            if options.Write then
                Directory.CreateDirectory(baselineDirectory repositoryRoot) |> ignore

                for name, _, lines in rendered do
                    File.WriteAllLines(baselineFileName repositoryRoot name, lines, utf8NoBom)

                stdout.WriteLine(successLine ())
                0
            else
                let messages =
                    rendered
                    |> List.choose (fun (simpleName, _, lines) ->
                        baselineMismatch repositoryRoot simpleName (fun () -> lines))

                if messages.IsEmpty then
                    stdout.WriteLine(successLine ())
                    0
                else
                    for message in messages do
                        stderr.WriteLine("error: " + message)

                    stderr.WriteLine(
                        "error: run 'dotnet fsi build.fsx -- -p verify-api-baseline --write' only when an accepted goal changes the public API surface."
                    )

                    1

/// Entry point for the gate.
let main (argv: string array) : int =
    mainWith Console.Out Console.Error (List.ofArray argv)
