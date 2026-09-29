module FunnySharp.Harness.DocsSnippets

// A behaviour-identical F# port of eng/tools/verify_docs_snippets.py, which is
// itself the behaviour-equivalent port of the authoritative PowerShell verifier
// at examples/FunnySharp.DocumentationSamples/VerifyDocumentationSnippets.ps1.
//
// The console contract: on success one stdout line
//   Verified <n> C# documentation snippets across 10 primary guides.
// and exit 0; on any failure every failure line goes to stderr prefixed
// 'error: ' and the exit code is 1. Nothing is ever written.
//
// PowerShell semantics reproduced deliberately:
//   * region keys and compared lines are case-insensitive (-eq/-ne);
//   * an inclusive range whose upper bound is below its lower bound descends
//     (PowerShell 1..0 yields 1, 0), so an empty region/fence body compares the
//     closing marker line followed by the opening marker line - never equal, so
//     the empty-body case fails instead of passing as an empty slice;
//   * [System.IO.File]::ReadAllLines: BOM detection (UTF-8/16/32) and universal
//     newlines with no phantom trailing line;
//   * bin/ and obj/ directories are excluded case-insensitively.
//
// The default repository root is derived from the executing assembly location
// (the F# analogue of the Python port's script-location default), falling back
// to a walk up from the current directory.

open System
open System.Collections.Generic
open System.IO
open System.Text
open System.Text.RegularExpressions
open FunnySharp.Harness.Repo

/// The ten primary guides, in the fixed verifier order. No directory discovery.
let primaryGuides: string list =
    [ "analyzers.md"
      "aspnet-core.md"
      "collections.md"
      "concurrency.md"
      "effects.md"
      "function-composition.md"
      "immutable-updates.md"
      "state-machines.md"
      "unit-result.md"
      "validation.md" ]

let private regionStartPattern =
    Regex(@"^\s*//\s*<snippet\s+(?<name>DocumentationSamples\.[A-Za-z0-9.]+)>\s*$")

let private regionEndPattern = Regex(@"^\s*//\s*</snippet>\s*$")

let private markerPattern =
    Regex(@"^<!-- documentation-sample: (?<name>DocumentationSamples\.[A-Za-z0-9.]+) -->$")

let private csharpFencePattern = Regex(@"^```csharp\s*$")
let private fenceEndPattern = Regex(@"^```\s*$")

// PowerShell's -notmatch is case-insensitive, so 'Bin' and 'OBJ' are skipped too.
let private buildDirectoryPattern = Regex(@"[\\/](?:bin|obj)[\\/]", RegexOptions.IgnoreCase)
let private leadingWhitespacePattern = Regex(@"^\s*")
let private lineBreakPattern = Regex("\r\n|\r|\n")

/// One extracted source region: original name, absolute source path, 1-based
/// start line and the dedented body.
type private Region =
    { Name: string
      Path: string
      Line: int
      Content: string list }

// ---- Line reading ([System.IO.File]::ReadAllLines semantics) ----

let private utf8 = UTF8Encoding(false, false)
let private utf16LittleEndian = UnicodeEncoding(false, false, false)
let private utf16BigEndian = UnicodeEncoding(true, false, false)
let private utf32LittleEndian = UTF32Encoding(false, false, false)
let private utf32BigEndian = UTF32Encoding(true, false, false)

let private bomUtf8 = [| 0xEFuy; 0xBBuy; 0xBFuy |]
let private bomUtf32LittleEndian = [| 0xFFuy; 0xFEuy; 0x00uy; 0x00uy |]
let private bomUtf32BigEndian = [| 0x00uy; 0x00uy; 0xFEuy; 0xFFuy |]
let private bomUtf16LittleEndian = [| 0xFFuy; 0xFEuy |]
let private bomUtf16BigEndian = [| 0xFEuy; 0xFFuy |]

let private startsWithBytes (prefix: byte array) (data: byte array) : bool =
    data.Length >= prefix.Length
    && (let mutable ok = true
        let mutable index = 0

        while ok && index < prefix.Length do
            if data.[index] <> prefix.[index] then
                ok <- false

            index <- index + 1

        ok)

/// Decode bytes the way StreamReader does: BOM detection, replacement fallback.
let private decodeBytes (data: byte array) : string =
    if startsWithBytes bomUtf8 data then
        utf8.GetString(data, 3, data.Length - 3)
    // UTF-32 LE shares its first two bytes with the UTF-16 LE BOM, so check it first.
    elif startsWithBytes bomUtf32LittleEndian data then
        utf32LittleEndian.GetString(data, 4, data.Length - 4)
    elif startsWithBytes bomUtf32BigEndian data then
        utf32BigEndian.GetString(data, 4, data.Length - 4)
    elif startsWithBytes bomUtf16LittleEndian data then
        utf16LittleEndian.GetString(data, 2, data.Length - 2)
    elif startsWithBytes bomUtf16BigEndian data then
        utf16BigEndian.GetString(data, 2, data.Length - 2)
    else
        utf8.GetString data

/// Equivalent of [System.IO.File]::ReadAllLines: universal newlines, BOM aware,
/// and no phantom trailing line for a trailing terminator.
let private readAllLines (path: string) : string array =
    let text = decodeBytes (File.ReadAllBytes path)

    if text = "" then
        [||]
    else
        let lines = lineBreakPattern.Split text |> Array.toList

        match List.rev lines with
        | "" :: rest -> List.rev rest |> List.toArray
        | _ -> lines |> List.toArray

/// Python's _inclusive_range / PowerShell's `$a..$b`: descending when the upper
/// bound is below the lower bound.
let private inclusiveRange (lines: string array) (start: int) (endIndex: int) : string list =
    if start <= endIndex then
        [ for index in start..endIndex -> lines.[index] ]
    else
        [ for index in start .. -1 .. endIndex -> lines.[index] ]

/// Case-insensitive comparison matching PowerShell's -eq/-ne on strings.
let private linesEqual (left: string) (right: string) : bool =
    left = right || String.Equals(left, right, StringComparison.OrdinalIgnoreCase)

let private commonIndent (content: string list) : int =
    let indents =
        content
        |> List.filter (fun line -> not (String.IsNullOrWhiteSpace line))
        |> List.map (fun line -> leadingWhitespacePattern.Match(line).Length)

    if indents.IsEmpty then 0 else List.min indents

// ---- Source-region collection ----

let private iterSourceFiles (samplesRoot: string) : string list =
    Directory.EnumerateFiles(samplesRoot, "*", SearchOption.AllDirectories)
    |> Seq.filter (fun path ->
        match Path.GetFileName path with
        | null -> false
        | name -> name.EndsWith(".cs", StringComparison.Ordinal))
    |> Seq.filter (fun path -> not (buildDirectoryPattern.IsMatch path))
    |> Seq.filter File.Exists
    |> Seq.sortWith (fun left right -> String.CompareOrdinal(left, right))
    |> List.ofSeq

let private collectRegions
    (samplesRoot: string)
    (failures: ResizeArray<string>)
    : Region list * Dictionary<string, Region> =
    let ordered = ResizeArray<Region>()
    let byKey = Dictionary<string, Region>(StringComparer.OrdinalIgnoreCase)

    for sourcePath in iterSourceFiles samplesRoot do
        let sourceLines = readAllLines sourcePath
        let mutable lineIndex = 0

        while lineIndex < sourceLines.Length do
            let start = regionStartPattern.Match sourceLines.[lineIndex]

            if not start.Success then
                lineIndex <- lineIndex + 1
            else
                let name = start.Groups.["name"].Value

                if byKey.ContainsKey name then
                    failures.Add(sprintf "Duplicate source region '%s' in %s:%d." name sourcePath (lineIndex + 1))
                    lineIndex <- lineIndex + 1
                else
                    let mutable endIndex = lineIndex + 1

                    while endIndex < sourceLines.Length
                          && not (regionEndPattern.IsMatch sourceLines.[endIndex]) do
                        endIndex <- endIndex + 1

                    if endIndex = sourceLines.Length then
                        failures.Add(
                            sprintf
                                "Source region '%s' in %s:%d has no closing snippet marker."
                                name
                                sourcePath
                                (lineIndex + 1)
                        )

                        lineIndex <- lineIndex + 1
                    else
                        let content = inclusiveRange sourceLines (lineIndex + 1) (endIndex - 1)
                        let indent = commonIndent content

                        let dedented =
                            content
                            |> List.map (fun line ->
                                if String.IsNullOrWhiteSpace line then "" else line.Substring indent)

                        let region =
                            { Name = name
                              Path = sourcePath
                              Line = lineIndex + 1
                              Content = dedented }

                        byKey.[name] <- region
                        ordered.Add region
                        lineIndex <- endIndex + 1

    List.ofSeq ordered, byKey

// ---- Guide scanning and comparison ----

let private compareRegion
    (guide: string)
    (fenceLine: int)
    (endIndex: int)
    (name: string)
    (markdownLines: string array)
    (byKey: Dictionary<string, Region>)
    (usedRegions: HashSet<string>)
    (failures: ResizeArray<string>)
    : unit =
    // Mirrors the PowerShell reference: a region is recorded as used even when
    // it turns out to be missing, and only the first differing line is reported.
    if usedRegions.Contains name then
        failures.Add(sprintf "%s:%d reuses source region '%s'." guide fenceLine name)
    else
        usedRegions.Add name |> ignore

    match byKey.TryGetValue name with
    | false, _ ->
        failures.Add(sprintf "%s:%d references missing source region '%s'." guide fenceLine name)
    | true, source ->
        let snippetLines = inclusiveRange markdownLines fenceLine (endIndex - 1)

        if snippetLines.Length <> source.Content.Length then
            failures.Add(
                sprintf "%s:%d differs from '%s' in %s:%d." guide fenceLine name source.Path source.Line
            )
        else
            let mutable contentIndex = 0
            let mutable stopped = false

            while not stopped && contentIndex < snippetLines.Length do
                if linesEqual snippetLines.[contentIndex] source.Content.[contentIndex] then
                    contentIndex <- contentIndex + 1
                else
                    failures.Add(
                        sprintf
                            "%s:%d differs from '%s' in %s:%d at snippet line %d."
                            guide
                            fenceLine
                            name
                            source.Path
                            source.Line
                            (contentIndex + 1)
                    )

                    stopped <- true

let private verify (repositoryRoot: string) (samplesRoot: string) : ResizeArray<string> * int =
    let failures = ResizeArray<string>()
    let orderedRegions, byKey = collectRegions samplesRoot failures
    let usedRegions = HashSet<string>(StringComparer.OrdinalIgnoreCase)
    let mutable snippetCount = 0

    for guide in primaryGuides do
        let guidePath = Path.Combine(repositoryRoot, "docs", guide)

        if not (File.Exists guidePath || Directory.Exists guidePath) then
            failures.Add(sprintf "Missing primary guide '%s'." guidePath)
        else
            let markdownLines = readAllLines guidePath
            let mutable lineIndex = 0

            while lineIndex < markdownLines.Length do
                if not (csharpFencePattern.IsMatch markdownLines.[lineIndex]) then
                    lineIndex <- lineIndex + 1
                else
                    snippetCount <- snippetCount + 1

                    let marker =
                        if lineIndex > 0 then
                            Some(markerPattern.Match markdownLines.[lineIndex - 1])
                        else
                            None

                    let name =
                        match marker with
                        | Some m when m.Success -> Some m.Groups.["name"].Value
                        | _ -> None

                    if name.IsNone then
                        failures.Add(
                            sprintf
                                "%s:%d must immediately follow a documentation-sample marker."
                                guide
                                (lineIndex + 1)
                        )

                    let mutable endIndex = lineIndex + 1

                    while endIndex < markdownLines.Length
                          && not (fenceEndPattern.IsMatch markdownLines.[endIndex]) do
                        endIndex <- endIndex + 1

                    if endIndex = markdownLines.Length then
                        failures.Add(sprintf "%s:%d has no closing code fence." guide (lineIndex + 1))
                        lineIndex <- lineIndex + 1
                    else
                        match name with
                        | Some regionName ->
                            compareRegion
                                guide
                                (lineIndex + 1)
                                endIndex
                                regionName
                                markdownLines
                                byKey
                                usedRegions
                                failures
                        | None -> ()

                        lineIndex <- endIndex + 1

    for region in orderedRegions do
        if not (usedRegions.Contains region.Name) then
            failures.Add(
                sprintf
                    "Source region '%s' in %s:%d has no documentation fence."
                    region.Name
                    region.Path
                    region.Line
            )

    failures, snippetCount

// ---- Command line ----

type private CliOptions =
    { RepositoryRoot: string option
      SamplesRoot: string option
      Help: bool }

let private usageLine =
    "usage: verify_docs_snippets.py [-h] [--repository-root PATH] [--samples-root PATH]"

let private helpText =
    String.concat
        "\n"
        [ usageLine
          ""
          "Verify that documentation C# snippets match their source regions. Exit status is 0 on success and 1 on any failure."
          ""
          "options:"
          "  -h, --help            show this help message and exit"
          "  --repository-root PATH"
          "                        repository root containing docs/ (default: resolved from the assembly location)."
          "  --samples-root PATH"
          "                        directory recursively scanned for *.cs snippet regions (default: <repository-root>/examples/FunnySharp.DocumentationSamples)." ]

let private parseArgs (argv: string list) : Result<CliOptions, string> =
    let rec loop (options: CliOptions) (remaining: string list) =
        match remaining with
        | [] -> Ok options
        | ("-h" | "--help") :: _ -> Ok { options with Help = true }
        | "--repository-root" :: value :: rest ->
            loop { options with RepositoryRoot = Some value } rest
        | [ "--repository-root" ] -> Error "argument --repository-root: expected one argument"
        | "--samples-root" :: value :: rest -> loop { options with SamplesRoot = Some value } rest
        | [ "--samples-root" ] -> Error "argument --samples-root: expected one argument"
        | arg :: rest when arg.StartsWith("--repository-root=") ->
            loop { options with RepositoryRoot = Some(arg.Substring("--repository-root=".Length)) } rest
        | arg :: rest when arg.StartsWith("--samples-root=") ->
            loop { options with SamplesRoot = Some(arg.Substring("--samples-root=".Length)) } rest
        | arg :: _ -> Error("unrecognized arguments: " + arg)

    loop { RepositoryRoot = None; SamplesRoot = None; Help = false } argv

/// Resolve a default repository root from a starting directory (the nearest
/// ancestor holding FunnySharp.slnx); Error carries the starting directory for
/// the remediation message.
let defaultRepositoryRootFrom (startDirectory: string) : Result<string, string> =
    match tryFindRootFrom startDirectory with
    | Some root -> Ok root
    | None -> Error(Path.GetFullPath startDirectory)

let private resolveDefaultRoot () : Result<string, string> =
    match defaultRepositoryRootFrom AppContext.BaseDirectory with
    | Ok root -> Ok root
    | Error _ ->
        match tryFindRoot () with
        | Some root -> Ok root
        | None -> Error(Path.GetFullPath AppContext.BaseDirectory)

/// Run the verifier against the supplied writers with an injectable default-root
/// resolver (so the default-root failure path is testable).
let runWithDefaultRoot
    (stdout: TextWriter)
    (stderr: TextWriter)
    (defaultRoot: unit -> Result<string, string>)
    (argv: string list)
    : int =
    match parseArgs argv with
    | Error message ->
        stderr.WriteLine usageLine
        stderr.WriteLine("error: " + message)
        1
    | Ok options when options.Help ->
        stdout.WriteLine helpText
        0
    | Ok options ->
        let repositoryRootResult =
            match options.RepositoryRoot with
            | Some root -> Ok(Path.GetFullPath root)
            | None -> defaultRoot ()

        match repositoryRootResult with
        | Error candidate ->
            stderr.WriteLine(
                sprintf
                    "error: '%s' is not inside a git repository, so the repository root cannot be derived from the script location."
                    candidate
            )

            stderr.WriteLine("remediation: run this script from a FunnySharp checkout or pass --repository-root <path>.")
            1
        | Ok repositoryRoot ->
            let samplesRoot =
                match options.SamplesRoot with
                | Some root -> Path.GetFullPath root
                | None -> Path.Combine(repositoryRoot, "examples", "FunnySharp.DocumentationSamples")

            if not (Directory.Exists samplesRoot) then
                stderr.WriteLine(sprintf "error: samples root '%s' does not exist or is not a directory." samplesRoot)

                stderr.WriteLine(
                    "remediation: pass --samples-root <path> pointing at the FunnySharp.DocumentationSamples directory."
                )

                1
            else
                try
                    let failures, snippetCount = verify repositoryRoot samplesRoot

                    if failures.Count > 0 then
                        for failure in failures do
                            stderr.WriteLine("error: " + failure)

                        1
                    else
                        stdout.WriteLine(
                            sprintf
                                "Verified %d C# documentation snippets across %d primary guides."
                                snippetCount
                                primaryGuides.Length
                        )

                        0
                with ex ->
                    stderr.WriteLine("error: " + ex.Message)
                    1

/// Run the verifier against the supplied writers and the standard default root.
let mainWith (stdout: TextWriter) (stderr: TextWriter) (argv: string list) : int =
    runWithDefaultRoot stdout stderr resolveDefaultRoot argv

/// Entry point mirroring verify_docs_snippets.py's main().
let main (argv: string array) : int =
    mainWith Console.Out Console.Error (List.ofArray argv)
