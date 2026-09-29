module FunnySharp.Harness.Tests.ReproducibleBuildsTests

// xUnit port of the reproducibility cases in eng/tests/ReleaseProtocol.Tests.ps1
// plus a hermetic happy-path comparison that uses the test assembly itself as the
// "built" DLL, so no build output is required. git runs against real temporary
// repositories; nothing here depends on timing.

open System
open System.IO
open System.IO.Compression
open System.Text.Json
open Xunit
open FunnySharp.Harness
open FunnySharp.Harness.Proc
open FunnySharp.Harness.Tests.Support

let private runGit (root: string) (arguments: string list) : ProcessResult =
    runCaptureSync "git" ([ "-C"; root ] @ arguments)

let private git (root: string) (arguments: string list) : unit =
    let result = runGit root arguments

    if result.ExitCode <> 0 then
        failwithf "git %s failed in '%s': %s" (String.Join(" ", arguments)) root result.Stderr

let private gitText (root: string) (arguments: string list) : string =
    let result = runGit root arguments

    if result.ExitCode <> 0 then
        failwithf "git %s failed in '%s': %s" (String.Join(" ", arguments)) root result.Stderr

    result.Stdout.Trim()

let private runMain (arguments: string list) : int * string * string =
    use stdout = new StringWriter()
    use stderr = new StringWriter()
    let exitCode = ReproducibleBuilds.mainWith stdout stderr runGit arguments
    exitCode, stdout.ToString(), stderr.ToString()

let private writeFile (path: string) (text: string) : unit =
    File.WriteAllText(path, text)

/// Two clean, cloned roots with a valid reproducibility-input.json each.
type private ReproducibilityFixture() =
    let temp = new TempDirectory()
    let left = Path.Combine(temp.Path, "left")
    let right = Path.Combine(temp.Path, "right")

    do
        Directory.CreateDirectory left |> ignore
        git left [ "init"; "-q"; "-b"; "main" ]
        git left [ "config"; "user.email"; "fixture@example.com" ]
        git left [ "config"; "user.name"; "Fixture" ]
        writeFile (Path.Combine(left, ".gitignore")) "artifacts/\nbin/\nobj/\n"
        writeFile (Path.Combine(left, "FunnySharp.slnx")) "<Solution />"
        writeFile (Path.Combine(left, "global.json")) "{}"
        writeFile (Path.Combine(left, "Directory.Build.props")) "<Project />"
        writeFile (Path.Combine(left, "packages.lock.json")) "{}"
        git left [ "add"; "-A" ]
        git left [ "commit"; "-q"; "-m"; "fixture" ]

        let clone = runCaptureSync "git" [ "clone"; "-q"; left; right ]

        if clone.ExitCode <> 0 then
            failwithf "git clone failed: %s" clone.Stderr

        do
            Directory.CreateDirectory(Path.Combine(left, "artifacts", "packages")) |> ignore
            Directory.CreateDirectory(Path.Combine(right, "artifacts", "packages")) |> ignore
            Directory.CreateDirectory(Path.Combine(left, "artifacts", ".nuget")) |> ignore
            Directory.CreateDirectory(Path.Combine(right, "artifacts", ".nuget")) |> ignore

    member _.TempPath = temp.Path
    member _.Left = left
    member _.Right = right
    member _.Commit = gitText left [ "rev-parse"; "HEAD" ]

    /// Write the reproducibility input evidence with the given cache directory.
    member _.WriteInput(root: string, cacheDirectory: string) : unit =
        let commit = gitText root [ "rev-parse"; "HEAD" ]

        writeFile
            (Path.Combine(root, "artifacts", "reproducibility-input.json"))
            (sprintf
                "{\"schemaVersion\":1,\"candidateCommit\":\"%s\",\"configuration\":\"Release\",\"isolatedNuGetCache\":true,\"nugetPackagesDirectory\":\"%s\",\"packageDirectory\":\"artifacts/packages\"}\n"
                commit
                cacheDirectory)

    member this.WriteCleanInputs() : unit =
        this.WriteInput(left, "artifacts/.nuget")
        this.WriteInput(right, "artifacts/.nuget")

    interface IDisposable with
        member _.Dispose() = (temp :> IDisposable).Dispose()

let private packageNames =
    [ "FunnySharp.0.1.0.nupkg"
      "FunnySharp.0.1.0.snupkg"
      "FunnySharp.AspNetCore.0.1.0.nupkg"
      "FunnySharp.AspNetCore.0.1.0.snupkg" ]

let private modulePaths =
    [ "src/FunnySharp/bin/Release/net10.0/FunnySharp.dll"
      "src/FunnySharp.AspNetCore/bin/Release/net10.0/FunnySharp.AspNetCore.dll"
      "src/FunnySharp/bin/Release/net10.0/FunnySharp.pdb"
      "src/FunnySharp.AspNetCore/bin/Release/net10.0/FunnySharp.AspNetCore.pdb"
      "src/FunnySharp/bin/Release/net10.0/FunnySharp.xml"
      "src/FunnySharp.AspNetCore/bin/Release/net10.0/FunnySharp.AspNetCore.xml" ]

/// Populate one root with deterministic "build outputs" and packages. The two
/// DLLs are the test assembly itself, which is a real, parseable assembly.
let private writeBuildOutputs (root: string) : unit =
    let assembly = typeof<ReproducibilityFixture>.Assembly.Location

    for relativePath in modulePaths do
        let full = Path.Combine(root, relativePath)

        match Path.GetDirectoryName full with
        | null -> ()
        | directory -> Directory.CreateDirectory directory |> ignore

        if relativePath.EndsWith(".dll", StringComparison.Ordinal) then
            File.Copy(assembly, full, true)
        else
            writeFile full (relativePath + "\n")

    let packages = Path.Combine(root, "artifacts", "packages")
    Directory.CreateDirectory packages |> ignore
    let stamp = DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero)

    for name in packageNames do
        use stream =
            new FileStream(Path.Combine(packages, name), FileMode.Create, FileAccess.Write)

        use archive = new ZipArchive(stream, ZipArchiveMode.Create)

        for index in 1..3 do
            let entry =
                archive.CreateEntry(sprintf "lib/net10.0/file%d.txt" index, CompressionLevel.NoCompression)

            entry.LastWriteTime <- stamp
            use writer = new StreamWriter(entry.Open())
            writer.Write(sprintf "payload-%d\n" index)

type RejectionTests() =

    [<Fact>]
    member _.CompareReproducibleBuilds_MissingMandatoryParameter_UsageError() =
        let exitCode, _, stderr = runMain []
        Assert.Equal(2, exitCode)
        Assert.Contains("Missing mandatory parameter(s): -LeftRoot, -RightRoot.", stderr)

    [<Fact>]
    member _.CompareReproducibleBuilds_NotACheckout_Rejects() =
        use fixture = new ReproducibilityFixture()
        fixture.WriteCleanInputs()
        use empty = new TempDirectory()
        let exitCode, _, stderr = runMain [ "-LeftRoot"; fixture.Left; "-RightRoot"; empty.Path ]
        Assert.Equal(1, exitCode)
        Assert.Contains("Root is not a FunnySharp checkout: '" + empty.Path + "'.", stderr)

    [<Fact>]
    member _.CompareReproducibleBuilds_DirtyRoot_Rejects() =
        use fixture = new ReproducibilityFixture()
        fixture.WriteCleanInputs()
        writeFile (Path.Combine(fixture.Right, "global.json")) "{\"dirty\":true}"
        let exitCode, _, stderr = runMain [ "-LeftRoot"; fixture.Left; "-RightRoot"; fixture.Right ]
        Assert.Equal(1, exitCode)
        Assert.Contains("Reproducibility root must be clean: '" + fixture.Right + "'.", stderr)

    [<Fact>]
    member _.CompareReproducibleBuilds_NonIsolatedNuGetCache_Rejects() =
        use fixture = new ReproducibilityFixture()
        fixture.WriteInput(fixture.Left, "artifacts/.nuget")
        fixture.WriteInput(fixture.Right, "../shared-cache")
        let exitCode, _, stderr = runMain [ "-LeftRoot"; fixture.Left; "-RightRoot"; fixture.Right ]
        Assert.Equal(1, exitCode)
        Assert.Contains("NuGet cache is not isolated under the root's artifacts directory", stderr)

    [<Fact>]
    member _.CompareReproducibleBuilds_IdenticalRoots_Rejects() =
        use fixture = new ReproducibilityFixture()
        fixture.WriteCleanInputs()
        let exitCode, _, stderr = runMain [ "-LeftRoot"; fixture.Left; "-RightRoot"; fixture.Left ]
        Assert.Equal(1, exitCode)
        Assert.Contains("LeftRoot and RightRoot must be different directories.", stderr)

    [<Fact>]
    member _.CompareReproducibleBuilds_MismatchedCommitOrTree_Rejects() =
        use fixture = new ReproducibilityFixture()
        fixture.WriteInput(fixture.Left, "artifacts/.nuget")

        writeFile (Path.Combine(fixture.Right, "Directory.Build.props")) "<Project><PropertyGroup /></Project>"
        git fixture.Right [ "config"; "user.email"; "fixture@example.com" ]
        git fixture.Right [ "config"; "user.name"; "Fixture" ]
        git fixture.Right [ "add"; "-A" ]
        git fixture.Right [ "commit"; "-q"; "-m"; "different" ]
        fixture.WriteInput(fixture.Right, "artifacts/.nuget")

        let exitCode, _, stderr = runMain [ "-LeftRoot"; fixture.Left; "-RightRoot"; fixture.Right ]
        Assert.Equal(1, exitCode)
        Assert.Contains("Roots do not contain the same commit and source tree:", stderr)

    [<Fact>]
    member _.CompareReproducibleBuilds_MissingInputEvidence_Rejects() =
        use fixture = new ReproducibilityFixture()
        fixture.WriteInput(fixture.Left, "artifacts/.nuget")
        fixture.WriteInput(fixture.Right, "artifacts/.nuget")

        File.Delete(Path.Combine(fixture.Right, "artifacts", "reproducibility-input.json"))

        let exitCode, _, stderr = runMain [ "-LeftRoot"; fixture.Left; "-RightRoot"; fixture.Right ]
        Assert.Equal(1, exitCode)
        Assert.Contains("Reproducibility input evidence was not found:", stderr)

    [<Fact>]
    member _.CompareReproducibleBuilds_ControlledInputsDiffer_Rejects() =
        use fixture = new ReproducibilityFixture()
        fixture.WriteInput(fixture.Left, "artifacts/.nuget")
        fixture.WriteInput(fixture.Right, "artifacts/.nuget")

        // Same commit and tree, but the untracked input evidence differs.
        writeFile
            (Path.Combine(fixture.Right, "artifacts", "reproducibility-input.json"))
            (sprintf
                "{\"schemaVersion\":1,\"candidateCommit\":\"%s\",\"configuration\":\"Release\",\"isolatedNuGetCache\":true,\"nugetPackagesDirectory\":\"artifacts/.nuget\",\"packageDirectory\":\"artifacts/other\"}\n"
                fixture.Commit)

        let exitCode, _, stderr = runMain [ "-LeftRoot"; fixture.Left; "-RightRoot"; fixture.Right ]
        Assert.Equal(1, exitCode)
        Assert.Contains("Controlled build inputs differ between roots.", stderr)

type ComparisonTests() =

    [<Fact>]
    member _.CompareReproducibleBuilds_IdenticalBuilds_ReportsByteIdentical() =
        use fixture = new ReproducibilityFixture()
        fixture.WriteCleanInputs()
        writeBuildOutputs fixture.Left
        writeBuildOutputs fixture.Right
        let outputPath = Path.Combine(fixture.TempPath, "comparison.json")

        let exitCode, stdout, stderr =
            runMain [ "-LeftRoot"; fixture.Left; "-RightRoot"; fixture.Right; "-OutputPath"; outputPath ]

        Assert.Equal(0, exitCode)
        Assert.Equal("", stderr)
        Assert.Equal($"Reproducibility comparison written to '{outputPath}'. First difference: none.", stdout.Trim())
        Assert.True(File.Exists outputPath)

        use document = JsonDocument.Parse(File.ReadAllText outputPath)
        let root = document.RootElement
        Assert.True(root.GetProperty("byteIdentical").GetBoolean())
        Assert.Equal(JsonValueKind.Null, root.GetProperty("firstDifference").ValueKind)
        Assert.Equal(3, root.GetProperty("layers").GetArrayLength())

    [<Fact>]
    member _.CompareReproducibleBuilds_DifferingPackageStillExitsZero() =
        use fixture = new ReproducibilityFixture()
        fixture.WriteCleanInputs()
        writeBuildOutputs fixture.Left
        writeBuildOutputs fixture.Right

        // Same commit, tree and controlled inputs, but one package differs.
        do
            use stream =
                new FileStream(
                    Path.Combine(fixture.Right, "artifacts", "packages", "FunnySharp.0.1.0.nupkg"),
                    FileMode.Append
                )

            stream.WriteByte 0x00uy

        let outputPath = Path.Combine(fixture.TempPath, "comparison.json")

        let exitCode, stdout, _ =
            runMain [ "-LeftRoot"; fixture.Left; "-RightRoot"; fixture.Right; "-OutputPath"; outputPath ]

        Assert.Equal(0, exitCode)
        Assert.Contains("First difference: packages.", stdout)

        use document = JsonDocument.Parse(File.ReadAllText outputPath)
        let root = document.RootElement
        Assert.False(root.GetProperty("byteIdentical").GetBoolean())
        Assert.Equal("packages", root.GetProperty("firstDifference").GetString())
