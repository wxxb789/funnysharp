module FunnySharp.Harness.Tests.ApiBaselineTests

// Behaviour tests for FunnySharp.Harness.ApiBaseline. The surface is always
// produced by ApiBaseline.getPublicApiText, so each
// fact exercises the module's real file-reading and comparison contract against
// a genuinely reflected assembly - the built harness itself - rather than a fake
// surface. The scratch repository roots mirror the fixed
// src/<name>/bin/Release/net10.0 layout the CLI resolves, without requiring a
// Release build of the shipping projects.

open System
open System.IO
open System.Text
open Xunit
open FunnySharp.Harness.ApiBaseline
open FunnySharp.Harness.Tests.Support

let private harnessAssemblyPath = Path.Combine(AppContext.BaseDirectory, "FunnySharp.Harness.dll")
let private utf8NoBom = UTF8Encoding(false)

/// Copy the built harness assembly into one shipping slot of a scratch root, so
/// mainWith's fixed layout resolves a real, loadable assembly. The slot path comes
/// from shippingAssemblies, so staging cannot drift from the layout the CLI resolves.
let private stageShippingAssemblies (repositoryRoot: string) : unit =
    for _, target in shippingAssemblies repositoryRoot do
        Directory.CreateDirectory(
            match Path.GetDirectoryName target with
            | null -> repositoryRoot
            | directory -> directory
        )
        |> ignore

        File.Copy(harnessAssemblyPath, target, true)

let private writeBaseline (path: string) (lines: string list) : unit =
    File.WriteAllLines(path, lines, utf8NoBom)

type ApiBaselineTests() =

    // Pure comparison: expected-only lines keep their order and are prefixed '-',
    // then actual-only lines keep their order and are prefixed '+'.
    [<Fact>]
    member _.DiffLines_ReportsRemovedThenAddedInOrder(): unit =
        let expected = [ "keep"; "removed-one"; "removed-two" ]
        let actual = [ "keep"; "added-one"; "added-two" ]

        Assert.Equal<string list>(
            [ "- removed-one"; "- removed-two"; "+ added-one"; "+ added-two" ],
            diffLines expected actual
        )

    [<Fact>]
    member _.DiffLines_IsEmptyWhenEqual(): unit =
        let lines =
            [ "ASSEMBLY FunnySharp, Version=1.0.0.0"
              "CLASS FunnySharp.Widget"
              "  METHOD Void Run()" ]

        Assert.Equal<string list>([], diffLines lines lines)

    // The documented path shape: <root>/eng/api-baseline/<name>.public-api.txt.
    [<Fact>]
    member _.BaselinePaths_AreUnderEngApiBaseline(): unit =
        Assert.Equal(Path.Combine("/repo", "eng", "api-baseline"), baselineDirectory "/repo")

        Assert.Equal(
            Path.Combine("/repo", "eng", "api-baseline", "FunnySharp.public-api.txt"),
            baselineFileName "/repo" "FunnySharp"
        )

    [<Fact>]
    member _.ShippingAssemblies_AreTheTwoFixedPathsInOrder(): unit =
        let shipping = shippingAssemblies "/repo"
        Assert.Equal(2, shipping.Length)
        Assert.Equal("FunnySharp", fst shipping.[0])
        Assert.Equal("FunnySharp.AspNetCore", fst shipping.[1])

        Assert.Equal(
            Path.Combine("/repo", "src/FunnySharp/bin/Release/net10.0/FunnySharp.dll"),
            snd shipping.[0]
        )

        Assert.Equal(
            Path.Combine("/repo", "src/FunnySharp.AspNetCore/bin/Release/net10.0/FunnySharp.AspNetCore.dll"),
            snd shipping.[1]
        )

    // The missing-baseline branch is decided from the file system before any
    // assembly is loaded, so it needs no built assembly: the path below does not
    // exist and must still yield the missing-file message.
    [<Fact>]
    member _.OutdatedBaselineMessages_ReportsMissingBaselineFile(): unit =
        use temp = new TempDirectory()

        let assemblyPath =
            Path.Combine(temp.Path, "src", "FunnySharp", "bin", "Release", "net10.0", "FunnySharp.dll")

        let messages = outdatedBaselineMessages temp.Path [ "FunnySharp", assemblyPath ]
        Assert.Equal(1, messages.Length)
        Assert.Contains("baseline file is missing", messages.Head)
        Assert.Contains(baselineFileName temp.Path "FunnySharp", messages.Head)

    // Match and drift branches against a genuinely reflected assembly (the built
    // harness), with the committed baseline written through the same rendering.
    [<Fact>]
    member _.OutdatedBaselineMessages_TracksRenderedSurface(): unit =
        use temp = new TempDirectory()
        Directory.CreateDirectory(baselineDirectory temp.Path) |> ignore
        let baselinePath = baselineFileName temp.Path "FunnySharp.Harness"

        let rendered = getPublicApiText [ harnessAssemblyPath ] temp.Path
        Assert.False(rendered.IsEmpty)
        writeBaseline baselinePath rendered

        Assert.Equal<string list>([], outdatedBaselineMessages temp.Path [ "FunnySharp.Harness", harnessAssemblyPath ])

        writeBaseline baselinePath (rendered @ [ "  METHOD Void Ghost()" ])

        let messages = outdatedBaselineMessages temp.Path [ "FunnySharp.Harness", harnessAssemblyPath ]
        Assert.Equal(1, messages.Length)
        Assert.Contains("differs from the committed baseline", messages.Head)
        Assert.Contains("-   METHOD Void Ghost()", messages.Head)

    // Usage failure: an unknown flag is exit 2 before the repository is touched.
    [<Fact>]
    member _.MainWith_RejectsUnknownFlag(): unit =
        use stdout = new StringWriter()
        use stderr = new StringWriter()
        let exitCode = mainWith stdout stderr [ "--nope" ]
        Assert.Equal(2, exitCode)
        Assert.Contains("error: unrecognized arguments: --nope", stderr.ToString())

    // Environment failure: a scratch root with no built shipping assemblies is
    // exit 2, not a verification failure.
    [<Fact>]
    member _.MainWith_ReportsUnbuiltShippingAssembly(): unit =
        use temp = new TempDirectory()
        use stdout = new StringWriter()
        use stderr = new StringWriter()
        let exitCode = mainWith stdout stderr [ "--repository-root"; temp.Path ]
        Assert.Equal(2, exitCode)
        Assert.Contains("has not been built", stderr.ToString())

    // CLI round trip on a scratch root whose shipping slots hold the built
    // harness: --write creates both baseline files, a clean verify exits 0 with
    // the single success line. Non-API bytes do not change the baseline verdict;
    // missing a real reflected method exits 1 while leaving
    // the committed baseline bytes untouched.
    [<Fact>]
    member _.WriteThenVerify_DoesNotWriteInVerifyMode(): unit =
        use temp = new TempDirectory()
        stageShippingAssemblies temp.Path

        let run (arguments: string list) =
            use stdout = new StringWriter()
            use stderr = new StringWriter()
            let exitCode = mainWith stdout stderr arguments
            exitCode, stdout.ToString(), stderr.ToString()

        let writeExit, _, writeErr = run [ "--repository-root"; temp.Path; "--write" ]
        Assert.Equal(0, writeExit)
        Assert.Equal("", writeErr)
        Assert.True(File.Exists(baselineFileName temp.Path "FunnySharp"))
        Assert.True(File.Exists(baselineFileName temp.Path "FunnySharp.AspNetCore"))

        let verifyExit, verifyOut, verifyErr = run [ "--repository-root"; temp.Path ]
        Assert.Equal(0, verifyExit)
        Assert.Equal("", verifyErr)
        Assert.StartsWith("Verified ", verifyOut.Trim())
        Assert.EndsWith("against eng/api-baseline.", verifyOut.Trim())

        let assemblyPaths = shippingAssemblies temp.Path |> List.map snd
        let originalSurface = getPublicApiText assemblyPaths temp.Path

        for path in assemblyPaths do
            use stream = new FileStream(path, FileMode.Append, FileAccess.Write)
            stream.Write [| 0uy; 1uy; 2uy; 3uy |]

        Assert.Equal<string list>(originalSurface, getPublicApiText assemblyPaths temp.Path)

        let appendExit, appendOut, appendErr = run [ "--repository-root"; temp.Path ]
        Assert.Equal(verifyExit, appendExit)
        Assert.Equal(verifyOut, appendOut)
        Assert.Equal(verifyErr, appendErr)

        // Omit a real method from one baseline; verify must reject the API difference.
        let baselinePath = baselineFileName temp.Path "FunnySharp"
        let baselineLines = File.ReadAllLines baselinePath |> Array.toList
        let omittedMethod =
            baselineLines |> List.find (fun line -> line.StartsWith("  METHOD ", StringComparison.Ordinal))
        writeBaseline baselinePath (baselineLines |> List.filter (fun line -> line <> omittedMethod))

        let bytesBefore = File.ReadAllBytes baselinePath
        let driftExit, driftOut, driftErr = run [ "--repository-root"; temp.Path ]
        Assert.Equal(1, driftExit)
        Assert.Equal("", driftOut)
        Assert.Contains("differs from the committed baseline", driftErr)
        Assert.Contains("+ " + omittedMethod, driftErr)
        Assert.Contains("--write", driftErr)
        Assert.Equal<byte list>(List.ofArray bytesBefore, List.ofArray(File.ReadAllBytes baselinePath))
