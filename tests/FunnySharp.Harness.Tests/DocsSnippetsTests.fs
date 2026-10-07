module FunnySharp.Harness.Tests.DocsSnippetsTests

// Behaviour tests for FunnySharp.Harness.DocsSnippets, ported from
// eng/tools/tests/test_docs_snippets_parity.py. The PowerShell-parity half of
// that suite becomes direct behaviour tests here: after the migration the F#
// module is the only implementation, so each fixture asserts the verdict the
// PowerShell reference produced (verified against it before this port landed).

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open Xunit
open FunnySharp.Harness.DocsSnippets
open FunnySharp.Harness.Tests.Support

let private samplesDirectory = Path.Combine("examples", "FunnySharp.DocumentationSamples")
let private guideUnderTest = "unit-result.md"
let private successLine = "Verified 56 C# documentation snippets across 11 primary guides."
let private emptyRegionName = "DocumentationSamples.UnitResult.DeleteOrNotify"
let private emptyRegionSample = "UnitResultSamples.cs"

type private FixtureTree =
    { Root: string
      Docs: string
      Samples: string }

// ---- Fixture staging (mirrors stage_fixture_tree) ----

let private stageFixtureTree (root: string) : FixtureTree =
    let docs = Path.Combine(root, "docs")
    let samples = Path.Combine(root, samplesDirectory)
    Directory.CreateDirectory docs |> ignore
    Directory.CreateDirectory samples |> ignore
    let repo = repositoryRoot ()

    for guide in primaryGuides do
        File.Copy(Path.Combine(repo, "docs", guide), Path.Combine(docs, guide))

    for source in
        Directory.GetFiles(Path.Combine(repo, samplesDirectory), "*.cs")
        |> Array.sort do
        match Path.GetFileName source with
        | null -> ()
        | name -> File.Copy(source, Path.Combine(samples, name))

    // A region under a build directory must be excluded from the scan: if it
    // were picked up, every otherwise-valid fixture tree would fail with an
    // unused-region error.
    for buildDirectory in [ "bin"; "obj" ] do
        let generated = Path.Combine(samples, buildDirectory, "Generated.cs")
        Directory.CreateDirectory(Path.Combine(samples, buildDirectory)) |> ignore

        File.WriteAllText(
            generated,
            "// <snippet DocumentationSamples.Generated.Ignored>\n"
            + "internal static class Generated { }\n"
            + "// </snippet>\n"
        )

    { Root = root
      Docs = docs
      Samples = samples }

// ---- Text helpers ----

let private readText (path: string) : string = File.ReadAllText path
let private writeText (path: string) (text: string) : unit = File.WriteAllText(path, text)
let private guidePath (tree: FixtureTree) : string = Path.Combine(tree.Docs, guideUnderTest)
let private splitLines (text: string) : string list = text.Split('\n') |> Array.toList

let private indexAfter (needle: string) (after: int) (lines: string list) : int =
    lines
    |> List.mapi (fun index line -> index, line)
    |> List.find (fun (index, line) -> index > after && line = needle)
    |> fst

let private firstFenceBlock (lines: string list) : string list =
    let fenceIndex = List.findIndex (fun line -> line = "```csharp") lines
    let endIndex = indexAfter "```" fenceIndex lines
    lines.[fenceIndex - 1 .. endIndex]

// ---- Fixture mutators (mirrors FIXTURE_CASES) ----

let private mutateDuplicateRegion (tree: FixtureTree) : unit =
    let path = Path.Combine(tree.Samples, "EffectSamples.cs")

    writeText
        path
        (readText path
         + "\n        // <snippet DocumentationSamples.Effects.CreateAndRun>\n"
         + "        // </snippet>\n")

let private mutateMissingClosingMarker (tree: FixtureTree) : unit =
    let path = Path.Combine(tree.Samples, "ValidationSamples.cs")

    readText path
    |> splitLines
    |> List.filter (fun line -> line.Trim() <> "// </snippet>")
    |> String.concat "\n"
    |> writeText path

let private mutateFenceWithoutMarker (tree: FixtureTree) : unit =
    let path = guidePath tree
    writeText path (readText(path).TrimEnd('\n') + "\n\n```csharp\nvar value = 1;\n```\n")

let private mutateReusedRegion (tree: FixtureTree) : unit =
    let path = guidePath tree
    let text = readText path
    let block = firstFenceBlock (splitLines text)
    writeText path (text.TrimEnd('\n') + "\n\n" + String.concat "\n" block + "\n")

let private mutateUnusedRegion (tree: FixtureTree) : unit =
    let path = guidePath tree
    let lines = splitLines (readText path)
    let fenceIndex = List.findIndex (fun line -> line = "```csharp") lines
    let endIndex = indexAfter "```" fenceIndex lines
    let remaining = lines.[0 .. fenceIndex - 2] @ lines.[endIndex + 1 ..]
    writeText path (String.concat "\n" remaining)

let private mutateCrlfGuide (tree: FixtureTree) : unit =
    let path = guidePath tree
    let text = readText path
    File.WriteAllText(path, text.Replace("\r\n", "\n").Replace("\n", "\r\n"))

let private mutateBomGuide (tree: FixtureTree) : unit =
    let path = guidePath tree
    let bom = [| 0xEFuy; 0xBBuy; 0xBFuy |]
    File.WriteAllBytes(path, Array.append bom (File.ReadAllBytes path))

let private mutateGuideWithoutTrailingNewline (tree: FixtureTree) : unit =
    let path = guidePath tree
    let bytes = File.ReadAllBytes path
    let mutable length = bytes.Length

    while length > 0 && (bytes.[length - 1] = 10uy || bytes.[length - 1] = 13uy) do
        length <- length - 1

    File.WriteAllBytes(path, bytes.[0 .. length - 1])

let private mutateIndentedFence (tree: FixtureTree) : unit =
    let path = guidePath tree
    let text = readText path
    let index = text.IndexOf("```csharp\n", StringComparison.Ordinal)
    writeText path (text.Substring(0, index) + "    " + text.Substring index)

let private mutateEmptyRegionAndFence (tree: FixtureTree) : unit =
    // Empty one source region body and its snippet fence body. PowerShell's
    // inclusive range counts down when the upper bound is below the lower
    // bound, so both sides become the pair of delimiter lines in reverse order
    // and cannot be equal: the verifier must fail. A naive ascending slice
    // would compare two empty lists and wrongly pass.
    let sample = Path.Combine(tree.Samples, emptyRegionSample)
    let lines = splitLines (readText sample)
    let startIndex =
        List.findIndex (fun (line: string) -> line.Trim() = "// <snippet " + emptyRegionName + ">") lines

    let trimmed = lines |> List.map (fun (line: string) -> line.Trim())
    let endIndex = indexAfter "// </snippet>" startIndex trimmed
    let sampleRemaining = lines.[0 .. startIndex] @ lines.[endIndex ..]
    writeText sample (String.concat "\n" sampleRemaining)

    let path = guidePath tree
    let guide = splitLines (readText path)

    let markerIndex =
        List.findIndex (fun line -> line = "<!-- documentation-sample: " + emptyRegionName + " -->") guide

    let fenceIndex = markerIndex + 1
    let endFence = indexAfter "```" fenceIndex guide
    let guideRemaining = guide.[0 .. fenceIndex] @ guide.[endFence ..]
    writeText path (String.concat "\n" guideRemaining)

// ---- Runner and assertions ----

let private runFixture (tree: FixtureTree) : int * string * string =
    use stdout = new StringWriter()
    use stderr = new StringWriter()

    let exitCode =
        mainWith stdout stderr [ "--repository-root"; tree.Root; "--samples-root"; tree.Samples ]

    exitCode, stdout.ToString(), stderr.ToString()

let private checkFixture
    (expectedExit: int)
    (expectedFragment: string option)
    (mutate: (FixtureTree -> unit) option)
    : unit =
    use temp = new TempDirectory()
    let tree = stageFixtureTree temp.Path
    mutate |> Option.iter (fun apply -> apply tree)
    let exitCode, stdout, stderr = runFixture tree
    Assert.Equal(expectedExit, exitCode)

    if expectedExit = 0 then
        Assert.Equal(successLine, stdout.Trim())
        Assert.Equal("", stderr)
    else
        Assert.False(String.IsNullOrEmpty stderr)

        for line in stderr.Split('\n') do
            if line <> "" then
                Assert.StartsWith("error: ", line)

    match expectedFragment with
    | Some fragment -> Assert.Contains(fragment, stderr)
    | None -> ()

type FixtureTests() =

    [<Fact>]
    member _.EqualLengthComparisonReportsOnlyTheFirstDifferingLine() =
        use temp = new TempDirectory()
        let tree = stageFixtureTree temp.Path
        let path = guidePath tree
        let lines = File.ReadAllLines path
        let marker = "<!-- documentation-sample: " + emptyRegionName + " -->"
        let bodyIndex = (lines |> Array.findIndex ((=) marker)) + 2
        lines.[bodyIndex] <- "first changed line"
        lines.[bodyIndex + 1] <- "second changed line"
        File.WriteAllLines(path, lines)
        let exitCode, _, stderr = runFixture tree
        Assert.Equal(1, exitCode)
        let failure = Assert.Single(stderr.Split('\n') |> Array.filter (fun line -> line.Contains emptyRegionName))
        let location = Regex.Match(failure, @"snippet line (\d+)\.")
        Assert.True(location.Success, failure)
        Assert.Equal("1", location.Groups.[1].Value)

    [<Fact>]
    member _.LongEqualSnippetIsComparedWithoutChangingTheVerdict() =
        use temp = new TempDirectory()
        let tree = stageFixtureTree temp.Path
        let body = [ for index in 1..1024 -> sprintf "var value%d = %d;" index index ]
        let sample = Path.Combine(tree.Samples, emptyRegionSample)
        let sourceLines = splitLines (readText sample)
        let start =
            sourceLines |> List.findIndex (fun line -> line.Trim() = "// <snippet " + emptyRegionName + ">")
        let finish = indexAfter "// </snippet>" start (sourceLines |> List.map (fun line -> line.Trim()))
        writeText sample (String.concat "\n" (
            sourceLines.[0..start] @ (body |> List.map (fun line -> "        " + line)) @ sourceLines.[finish..]))
        let path = guidePath tree
        let guide = splitLines (readText path)
        let marker = "<!-- documentation-sample: " + emptyRegionName + " -->"
        let fence = (guide |> List.findIndex ((=) marker)) + 1
        let endFence = indexAfter "```" fence guide
        writeText path (String.concat "\n" (guide.[0..fence] @ body @ guide.[endFence..]))
        let exitCode, stdout, stderr = runFixture tree
        Assert.Equal(0, exitCode)
        Assert.Equal("", stderr)
        Assert.Equal(successLine, stdout.Trim())

    [<Theory>]
    [<InlineData("ValidTree")>]
    [<InlineData("DuplicateRegion")>]
    [<InlineData("MissingClosingMarker")>]
    [<InlineData("FenceWithoutMarker")>]
    [<InlineData("ReusedRegion")>]
    [<InlineData("UnusedRegion")>]
    [<InlineData("CrlfGuideReadsAsUniversalNewlines")>]
    [<InlineData("Utf8BomGuide")>]
    [<InlineData("GuideWithoutTrailingNewline")>]
    [<InlineData("IndentedFenceIsTreatedAsBodyAndFails")>]
    [<InlineData("EmptyRegionAndFenceDescendingRangeFails")>]
    member _.EveryFixtureObligationIsExecuted(case: string): unit =
        // Explicit executable cases replace the count-only reflection guard.
        match case with
        | "ValidTree" -> checkFixture 0 None None
        | "DuplicateRegion" -> checkFixture 1 None (Some mutateDuplicateRegion)
        | "MissingClosingMarker" -> checkFixture 1 None (Some mutateMissingClosingMarker)
        | "FenceWithoutMarker" -> checkFixture 1 None (Some mutateFenceWithoutMarker)
        | "ReusedRegion" -> checkFixture 1 None (Some mutateReusedRegion)
        | "UnusedRegion" -> checkFixture 1 None (Some mutateUnusedRegion)
        | "CrlfGuideReadsAsUniversalNewlines" -> checkFixture 0 None (Some mutateCrlfGuide)
        | "Utf8BomGuide" -> checkFixture 0 None (Some mutateBomGuide)
        | "GuideWithoutTrailingNewline" -> checkFixture 0 None (Some mutateGuideWithoutTrailingNewline)
        | "IndentedFenceIsTreatedAsBodyAndFails" -> checkFixture 1 (Some emptyRegionName) (Some mutateIndentedFence)
        | "EmptyRegionAndFenceDescendingRangeFails" -> checkFixture 1 (Some emptyRegionName) (Some mutateEmptyRegionAndFence)
        | _ -> Assert.Fail("Unknown fixture case: " + case)

type RepositoryTests() =

    [<Fact>]
    member _.RealRepositoryPassesFromAnyWorkingDirectory(): unit =
        use stdout = new StringWriter()
        use stderr = new StringWriter()
        let exitCode = mainWith stdout stderr []
        Assert.Equal(0, exitCode)
        Assert.Equal("", stderr.ToString())
        Assert.Equal(successLine, stdout.ToString().Trim())

    [<Fact>]
    member _.MissingSamplesRootFailsWithRemediation(): unit =
        use temp = new TempDirectory()
        use stdout = new StringWriter()
        use stderr = new StringWriter()

        let exitCode =
            mainWith stdout stderr [ "--repository-root"; temp.Path; "--samples-root"; Path.Combine(temp.Path, "nope") ]

        Assert.Equal(1, exitCode)
        Assert.Contains("does not exist or is not a directory", stderr.ToString())
        Assert.Contains("--samples-root", stderr.ToString())

    [<Fact>]
    member _.DefaultRootOutsideGitRepositoryFailsWithRemediation(): unit =
        use temp = new TempDirectory()

        match defaultRepositoryRootFrom temp.Path with
        | Ok root -> Assert.True(false, sprintf "unexpected repository root '%s'" root)
        | Error _ -> ()

        use stdout = new StringWriter()
        use stderr = new StringWriter()
        let exitCode = runWithDefaultRoot stdout stderr (fun () -> Error temp.Path) []
        Assert.Equal(1, exitCode)
        Assert.Contains("not inside a git repository", stderr.ToString())
        Assert.Contains("--repository-root", stderr.ToString())
