module FunnySharp.Harness.Tests.XmlBuildBindingsTests

open System
open System.IO
open System.Reflection
open System.Reflection.Emit
open System.Security.Cryptography
open System.Text
open System.Text.Json.Nodes
open System.Threading
open Xunit
open FunnySharp.Harness
open FunnySharp.Harness.Tests.Support

let private sha path =
    Convert.ToHexString(SHA256.HashData(File.ReadAllBytes path)).ToLowerInvariant()

let private present (node: JsonNode | null) =
    match node with
    | null -> invalidOp "Missing controlled fixture node."
    | value -> value

let private parse text = JsonNode.Parse(text: string) |> present
let private get (node: JsonNode) (key: string) = present node.[key]
let mutable private unique = 0
let private commit = String.replicate 40 "a"
let private clock = DateTimeOffset(2026, 10, 3, 0, 0, 0, TimeSpan.Zero)
let private names = [ "FunnySharp"; "FunnySharp.AspNetCore" ]

type private Fixture(root: string) =
    let directory = Path.Combine(root, "artifacts", "attempt")
    let write (relative: string) (text: string) =
        let path = Path.Combine(root, relative)
        Directory.CreateDirectory(Path.GetDirectoryName path |> Option.ofObj |> Option.get) |> ignore
        File.WriteAllText(path, text)
    let path name extension = "src/" + name + "/bin/Release/net10.0/" + name + extension
    let policy = parse """{"schema":"funnysharp-explicit-xml-bindings/v1","assemblies":[],"aliases":[],"inheritance":[],"framework":{}}"""
    let assemblies = (get policy "assemblies").AsArray()
    let inherited = (get policy "inheritance").AsArray()
    let id = Interlocked.Increment(&unique)
    let typeName (name: string) = "XmlFixture." + name.Replace(".", "") + "Value" + string id
    let emit name =
        let identity = AssemblyName name
        identity.Version <- Version(1, 0, id, 0)
        let builder = PersistedAssemblyBuilder(identity, typeof<obj>.Assembly)
        let apiType = builder.DefineDynamicModule(name).DefineType(typeName name, TypeAttributes.Public ||| TypeAttributes.Abstract ||| TypeAttributes.Sealed)
        let method = apiType.DefineMethod("Read", MethodAttributes.Public ||| MethodAttributes.Static, typeof<int>, Type.EmptyTypes)
        let il = method.GetILGenerator()
        il.Emit(OpCodes.Ldc_I4_1)
        il.Emit OpCodes.Ret
        apiType.CreateType() |> ignore
        builder.Save(Path.Combine(root, path name ".dll"))
    do
        Directory.CreateDirectory(Path.Combine(directory, "receipts")) |> ignore
        write "global.json" "{}"
        write "Directory.Build.props" "<Project />"
        for name in names do
            write ("src/" + name + "/" + name + ".csproj") "<Project />"
            write ("src/" + name + "/Value.cs") "public static int Read() => 1;"
            let xml =
                [ "<doc><members>"
                  "<member name=\"T:" + typeName name + "\"><summary>Value.</summary></member>"
                  "<member name=\"M:" + typeName name + ".Read\"><summary>Read.</summary></member>"
                  "</members></doc>"
                  "" ]
                |> String.concat "\r\n"
            write (path name ".xml") xml
            write (path name ".pdb") "sealed PDB"
            emit name
            let row = JsonObject()
            row.["name"] <- JsonValue.Create name
            row.["dllSha256"] <- JsonValue.Create(sha (Path.Combine(root, path name ".dll")))
            row.["xmlSha256"] <- JsonValue.Create(sha (Path.Combine(root, path name ".xml")))
            assemblies.Add row
        // This synthetic policy explicitly reviews the fixture machine's reference bytes.
        // It must not assume the production policy's historical pack is installed.
        let runtime = DirectoryInfo(Path.GetDirectoryName typeof<obj>.Assembly.Location |> Option.ofObj |> Option.get)
        let mutable installationDirectory = runtime
        for _ in 1 .. 3 do
            installationDirectory <- installationDirectory.Parent |> Option.ofObj |> Option.get
        let installation = installationDirectory.FullName
        let frameworkPath =
            Directory.GetDirectories(Path.Combine(installation, "packs/Microsoft.NETCore.App.Ref"), "10.0.*")
            |> Array.sort
            |> Array.map (fun directory -> Path.Combine(directory, "ref/net10.0/System.Runtime.xml"))
            |> Array.find File.Exists
        let framework = get policy "framework"
        framework.["relativePath"] <- JsonValue.Create(Path.GetRelativePath(installation, frameworkPath).Replace('\\', '/'))
        framework.["sha256"] <- JsonValue.Create(sha frameworkPath)
        write "eng/api-baseline/xml-contract-bindings.json" (policy.ToJsonString())
        let anchor = parse """{"schema":"funnysharp-reviewed-xml-inputs/v2","scope":"","reviewBasis":{},"historicalExactReviewPreserved":"","files":[],"assemblies":[],"reviewedPolicy":{}}"""
        let inputs = (get anchor "files").AsArray()
        for relative in [ yield "global.json"; yield "Directory.Build.props"
                          for name in names do
                              yield "src/" + name + "/" + name + ".csproj"
                              yield "src/" + name + "/Value.cs" ] do
            let row = JsonObject()
            row.["path"] <- JsonValue.Create relative
            row.["sha256"] <- JsonValue.Create(sha (Path.Combine(root, relative)))
            inputs.Add row
        let reviewedPolicy = get anchor "reviewedPolicy"
        reviewedPolicy.["path"] <- JsonValue.Create "eng/api-baseline/xml-contract-bindings.json"
        reviewedPolicy.["sha256"] <- JsonValue.Create(sha (Path.Combine(root, "eng/api-baseline/xml-contract-bindings.json")))
        for row in assemblies do
            let assembly = JsonObject()
            assembly.["name"] <- (get (present row) "name").DeepClone()
            assembly.["xmlSha256"] <- (get (present row) "xmlSha256").DeepClone()
            let xml = File.ReadAllText(Path.Combine(root, path ((get (present row) "name").ToString()) ".xml"))
            assembly.["lfXmlSha256"] <- JsonValue.Create(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(xml.Replace("\r\n", "\n")))).ToLowerInvariant())
            (get anchor "assemblies").AsArray().Add assembly
        write XmlBuildBindings.anchorPath (anchor.ToJsonString())
        let build = parse """{"schemaVersion":1,"name":"build","fileName":"dotnet","arguments":["build","FunnySharp.slnx","--configuration","Release","--no-restore"],"workingDirectory":"","exitCode":0,"completedAtUtc":"2026-10-03T00:00:00.0000000+00:00"}"""
        build.["workingDirectory"] <- JsonValue.Create root
        File.WriteAllText(Path.Combine(directory, "receipts/03-build.json"), build.ToJsonString())

    member _.Directory = directory
    member _.Path name extension = Path.Combine(root, path name extension)
    member _.TypeName name = typeName name
    member _.Policy = policy
    member _.Inheritance = inherited
    member _.Write relative text = write relative text
    member _.Capture() = XmlBuildBindings.capture root directory commit "attempt" "receipts/03-build.json" clock
    member _.SealAt(pointer: JsonObject, executionDirectory: string) =
        let manifest = parse """{"candidateCommit":"","attemptId":"attempt","commands":[{"name":"build"},{"name":"test","startedAtUtc":"2026-10-03T00:00:01.0000000+00:00"}]}"""
        manifest.["candidateCommit"] <- JsonValue.Create commit
        manifest.["xmlBuildBindings"] <- pointer.DeepClone()
        File.WriteAllText(Path.Combine(executionDirectory, "execution-evidence.json"), manifest.ToJsonString())
        let summary = JsonObject()
        summary.["directory"] <- JsonValue.Create executionDirectory
        summary.["manifest"] <- JsonValue.Create "execution-evidence.json"
        summary.["manifestSha256"] <- JsonValue.Create(sha (Path.Combine(executionDirectory, "execution-evidence.json")))
        let command = JsonObject()
        command.["name"] <- JsonValue.Create "build"
        command.["receipt"] <- JsonValue.Create "receipts/03-build.json"
        command.["receiptSha256"] <- JsonValue.Create(sha (Path.Combine(executionDirectory, "receipts/03-build.json")))
        let commands = JsonArray()
        commands.Add command
        summary.["commands"] <- commands
        summary
    member this.Seal(pointer: JsonObject) = this.SealAt(pointer, directory)
    member _.RebindPolicy() =
        write "eng/api-baseline/xml-contract-bindings.json" (policy.ToJsonString())
        let anchor = parse (File.ReadAllText(Path.Combine(root, XmlBuildBindings.anchorPath)))
        (get anchor "reviewedPolicy").["sha256"] <- JsonValue.Create(sha (Path.Combine(root, "eng/api-baseline/xml-contract-bindings.json")))
        write XmlBuildBindings.anchorPath (anchor.ToJsonString())

[<Fact>]
let DistinctPostBuildBytesWithUnchangedReviewedInputsPassAndLegacyRemainsExact () =
    use temp = new TempDirectory()
    let fixture = Fixture temp.Path
    let before = sha (fixture.Path "FunnySharp" ".dll")
    File.AppendAllText(fixture.Path "FunnySharp" ".dll", "commit-dependent PE bytes")
    File.AppendAllText(fixture.Path "FunnySharp" ".pdb", "commit-dependent PDB bytes")
    Assert.NotEqual<string>(before, sha (fixture.Path "FunnySharp" ".dll"))
    let pointer = fixture.Capture()
    let summary = fixture.Seal pointer
    let hashes = XmlBuildBindings.validate temp.Path fixture.Directory commit summary
    Assert.Equal(2, hashes.Count)
    let paths = names |> List.map (fun name -> fixture.Path name ".xml")
    Assert.Equal(2, (ReleaseVerifyArtifacts.getReleaseXmlDocumentationInventory temp.Path paths fixture.Directory commit summary).Count)
    let error = Assert.Throws<ReleaseVerifySource.ReleaseVerifyFailure>(fun () -> ReleaseVerifyArtifacts.getXmlDocumentationInventory temp.Path paths |> ignore)
    Assert.Contains("reviewed contract binding", error.Message)
    // Reuse the loaded fixture metadata to check both historical and release policy paths.
    (present (get fixture.Policy "assemblies").[0]).["dllSha256"] <- JsonValue.Create(sha (fixture.Path "FunnySharp" ".dll"))
    fixture.RebindPolicy()
    Assert.Equal(2, (ReleaseVerifyArtifacts.getXmlDocumentationInventory temp.Path paths).Count)
    for path in paths do File.WriteAllText(path, File.ReadAllText(path).Replace("\r\n", "\n"))
    let lfDirectory = Path.Combine(temp.Path, "artifacts", "lf")
    Directory.CreateDirectory(Path.Combine(lfDirectory, "receipts")) |> ignore
    File.Copy(Path.Combine(fixture.Directory, "receipts/03-build.json"), Path.Combine(lfDirectory, "receipts/03-build.json"))
    let lfPointer = XmlBuildBindings.capture temp.Path lfDirectory commit "attempt" "receipts/03-build.json" clock
    let lfSummary = fixture.SealAt(lfPointer, lfDirectory)
    let lfHashes = XmlBuildBindings.validate temp.Path lfDirectory commit lfSummary
    Assert.Equal(sha (fixture.Path "FunnySharp" ".xml"), snd lfHashes.["FunnySharp"])
    let inventory = ReleaseVerifyArtifacts.getReleaseXmlDocumentationInventory temp.Path paths lfDirectory commit lfSummary
    Assert.Equal(2, inventory.Count)
    for row in inventory do
        let node = present row
        Assert.Equal(sha ((get node "path").ToString()), (get node "sha256").ToString())
        Assert.Equal(sha ((get node "frameworkPath").ToString()), (get node "frameworkSha256").ToString())
    let legacyXml = Assert.Throws<ReleaseVerifySource.ReleaseVerifyFailure>(fun () -> ReleaseVerifyArtifacts.getXmlDocumentationInventory temp.Path paths |> ignore)
    Assert.Contains("reviewed contract binding", legacyXml.Message)
    // Both representations are reviewed, but a seal permits only the actual captured bytes.
    for path in paths do File.WriteAllText(path, File.ReadAllText(path).Replace("\n", "\r\n"))
    Assert.Throws<InvalidOperationException>(fun () -> XmlBuildBindings.validate temp.Path lfDirectory commit lfSummary |> ignore) |> ignore
    for kind in [ "alias"; "inheritance" ] do
        if kind = "alias" then
            let alias = parse """{"assembly":"FunnySharp","xmlId":"M:Unused.Read","typeXmlId":"T:Unused","kind":"synthesized-record-method","rationale":"fixture"}"""
            (get fixture.Policy "aliases").AsArray().Add alias
        else
            (get fixture.Policy "aliases").AsArray().Clear()
            fixture.Inheritance.Add(parse """{"xmlId":"M:Unused.ToString","target":"M:System.Object.ToString"}""")
        fixture.RebindPolicy()
        let next = Path.Combine(temp.Path, "artifacts", kind)
        Directory.CreateDirectory(Path.Combine(next, "receipts")) |> ignore
        File.Copy(Path.Combine(fixture.Directory, "receipts/03-build.json"), Path.Combine(next, "receipts/03-build.json"))
        let pointer = XmlBuildBindings.capture temp.Path next commit "attempt" "receipts/03-build.json" clock
        let summary = fixture.SealAt(pointer, next)
        let legacy = Assert.Throws<ReleaseVerifySource.ReleaseVerifyFailure>(fun () -> ReleaseVerifyArtifacts.getXmlDocumentationInventory temp.Path paths |> ignore)
        let release = Assert.Throws<ReleaseVerifySource.ReleaseVerifyFailure>(fun () -> ReleaseVerifyArtifacts.getReleaseXmlDocumentationInventory temp.Path paths next commit summary |> ignore)
        Assert.Contains("unconsumed", legacy.Message)
        Assert.Contains("unconsumed", release.Message)
    (get fixture.Policy "aliases").AsArray().Clear()
    fixture.Inheritance.Clear()
    (get fixture.Policy "framework").["relativePath"] <- JsonValue.Create "packs/Microsoft.NETCore.App.Ref/10.0.999/ref/net10.0/System.Runtime.xml"
    fixture.RebindPolicy()
    let frameworkDirectory = Path.Combine(temp.Path, "artifacts", "framework")
    Directory.CreateDirectory(Path.Combine(frameworkDirectory, "receipts")) |> ignore
    File.Copy(Path.Combine(fixture.Directory, "receipts/03-build.json"), Path.Combine(frameworkDirectory, "receipts/03-build.json"))
    let frameworkPointer = XmlBuildBindings.capture temp.Path frameworkDirectory commit "attempt" "receipts/03-build.json" clock
    let frameworkSummary = fixture.SealAt(frameworkPointer, frameworkDirectory)
    Assert.Equal(2, (ReleaseVerifyArtifacts.getReleaseXmlDocumentationInventory temp.Path paths frameworkDirectory commit frameworkSummary).Count)
    let legacyFramework = Assert.Throws<ReleaseVerifySource.ReleaseVerifyFailure>(fun () -> ReleaseVerifyArtifacts.getXmlDocumentationInventory temp.Path paths |> ignore)
    Assert.Contains("framework bytes are absent or stale", legacyFramework.Message)

[<Theory>]
[<InlineData("source")>]
[<InlineData("policy")>]
[<InlineData("xml")>]
[<InlineData("xml-content")>]
[<InlineData("xml-mixed-newlines")>]
[<InlineData("xml-bom")>]
[<InlineData("xml-primary-review")>]
[<InlineData("unexpected-source")>]
[<InlineData("unexpected-build-input")>]
[<InlineData("missing-anchor")>]
[<InlineData("failed-build")>]
let DriftFailsBeforeSeal(kind: string) =
    use temp = new TempDirectory()
    let fixture = Fixture temp.Path
    match kind with
    | "source" -> fixture.Write "src/FunnySharp/Value.cs" "changed source"
    | "policy" -> fixture.Write "eng/api-baseline/xml-contract-bindings.json" "{}"
    | "xml" -> File.AppendAllText(fixture.Path "FunnySharp" ".xml", "changed XML")
    | "xml-content" ->
        let path = fixture.Path "FunnySharp" ".xml"
        File.WriteAllText(path, File.ReadAllText(path).Replace("Read.", "Changed."))
    | "xml-mixed-newlines" ->
        let path = fixture.Path "FunnySharp" ".xml"
        File.WriteAllText(path, File.ReadAllText(path).Replace("<doc><members>\r\n", "<doc><members>\n"))
    | "xml-bom" ->
        let path = fixture.Path "FunnySharp" ".xml"
        File.WriteAllText(path, File.ReadAllText(path), UTF8Encoding(true))
    | "xml-primary-review" ->
        let path = Path.Combine(temp.Path, XmlBuildBindings.anchorPath)
        let anchor = parse (File.ReadAllText path)
        let assembly = present (get anchor "assemblies").[0]
        assembly.["xmlSha256"] <- (get assembly "lfXmlSha256").DeepClone()
        let xmlPath = fixture.Path "FunnySharp" ".xml"
        File.WriteAllText(xmlPath, File.ReadAllText(xmlPath).Replace("\r\n", "\n"))
        File.WriteAllText(path, anchor.ToJsonString())
    | "unexpected-source" -> fixture.Write "src/FunnySharp/New.cs" "new shipping input"
    | "unexpected-build-input" -> fixture.Write "Directory.Build.targets" "<Project />"
    | "missing-anchor" -> File.Delete(Path.Combine(temp.Path, XmlBuildBindings.anchorPath))
    | "failed-build" ->
        let path = Path.Combine(fixture.Directory, "receipts/03-build.json")
        let node = parse (File.ReadAllText path)
        node.["exitCode"] <- JsonValue.Create 1
        File.WriteAllText(path, node.ToJsonString())
    | _ -> invalidArg "kind" kind
    Assert.True(XmlBuildBindings.required temp.Path)
    Assert.Throws<InvalidOperationException>(fun () -> fixture.Capture() |> ignore) |> ignore
    Assert.False(File.Exists(Path.Combine(fixture.Directory, "xml-build-bindings.json")))

[<Theory>]
[<InlineData("candidate")>]
[<InlineData("attempt")>]
[<InlineData("build-hash")>]
[<InlineData("build-receipt")>]
[<InlineData("binding-hash")>]
[<InlineData("missing-receipt")>]
[<InlineData("dll")>]
[<InlineData("pdb")>]
[<InlineData("source")>]
[<InlineData("policy")>]
[<InlineData("xml")>]
[<InlineData("anchor")>]
[<InlineData("capture-time")>]
[<InlineData("receipt-path")>]
let SealedEvidenceRejectsTampering(kind: string) =
    use temp = new TempDirectory()
    let fixture = Fixture temp.Path
    let pointer = fixture.Capture()
    let bindingPath = Path.Combine(fixture.Directory, "xml-build-bindings.json")
    let binding = parse (File.ReadAllText bindingPath)
    match kind with
    | "candidate" -> binding.["candidateCommit"] <- JsonValue.Create(String.replicate 40 "b")
    | "attempt" -> binding.["attemptId"] <- JsonValue.Create "other-attempt"
    | "build-hash" -> (get binding "buildCommandReceipt").["sha256"] <- JsonValue.Create(String.replicate 64 "0")
    | "build-receipt" -> File.AppendAllText(Path.Combine(fixture.Directory, "receipts/03-build.json"), " ")
    | "binding-hash" -> pointer.["sha256"] <- JsonValue.Create(String.replicate 64 "0")
    | "missing-receipt" -> File.Delete bindingPath
    | "dll" | "pdb" | "xml" -> File.AppendAllText(fixture.Path "FunnySharp" ("." + kind), "tampered")
    | "source" -> fixture.Write "src/FunnySharp/Value.cs" "changed"
    | "policy" -> fixture.Write "eng/api-baseline/xml-contract-bindings.json" "{}"
    | "anchor" -> File.Delete(Path.Combine(temp.Path, XmlBuildBindings.anchorPath))
    | "capture-time" -> binding.["capturedAtUtc"] <- JsonValue.Create "2026-10-03T00:00:02.0000000+00:00"
    | "receipt-path" -> pointer.["path"] <- JsonValue.Create "../xml-build-bindings.json"
    | _ -> invalidArg "kind" kind
    if [ "candidate"; "attempt"; "build-hash"; "capture-time" ] |> List.contains kind then
        File.WriteAllText(bindingPath, binding.ToJsonString())
        pointer.["sha256"] <- JsonValue.Create(sha bindingPath)
    let summary = fixture.Seal pointer
    Assert.Throws<InvalidOperationException>(fun () -> XmlBuildBindings.validate temp.Path fixture.Directory commit summary |> ignore) |> ignore

[<Fact>]
let ExplicitDirectoryRootsWithTrailingSeparatorsPass () =
    use temp = new TempDirectory()
    let fixture = Fixture temp.Path
    let root = temp.Path + string Path.DirectorySeparatorChar
    let directory = fixture.Directory + string Path.DirectorySeparatorChar
    let pointer = XmlBuildBindings.capture root directory commit "attempt" "receipts/03-build.json" clock
    let summary = fixture.Seal pointer
    let hashes = XmlBuildBindings.validate root directory commit summary
    Assert.Equal(2, hashes.Count)

[<Fact>]
let CaptureCannotOverwriteAnExistingSeal () =
    use temp = new TempDirectory()
    let fixture = Fixture temp.Path
    fixture.Capture() |> ignore
    let original = sha (Path.Combine(fixture.Directory, "xml-build-bindings.json"))
    Assert.Throws<IOException>(fun () -> fixture.Capture() |> ignore) |> ignore
    Assert.Equal(original, sha (Path.Combine(fixture.Directory, "xml-build-bindings.json")))

[<Fact>]
let ReleaseLocatesOnlyIdenticalReviewedReferenceBytesInInstalledNet10Pack () =
    use temp = new TempDirectory()
    let primary = "packs/Microsoft.NETCore.App.Ref/10.0.12/ref/net10.0/System.Runtime.xml"
    let actual = Path.Combine(temp.Path, "packs/Microsoft.NETCore.App.Ref/10.0.11/ref/net10.0/System.Runtime.xml")
    Directory.CreateDirectory(Path.GetDirectoryName actual |> Option.ofObj |> Option.get) |> ignore
    File.WriteAllText(actual, "<doc><members><member name=\"M:System.Object.ToString\"><summary>Text.</summary></member></members></doc>")
    let expectedHash = sha actual
    Assert.Equal(Path.GetFullPath actual, Path.GetFullPath(ReleaseVerifyArtifacts.resolveReleaseFrameworkXml temp.Path primary expectedHash))
    let preferred = Path.Combine(temp.Path, primary)
    Directory.CreateDirectory(Path.GetDirectoryName preferred |> Option.ofObj |> Option.get) |> ignore
    File.Copy(actual, preferred)
    Assert.Equal(Path.GetFullPath preferred, Path.GetFullPath(ReleaseVerifyArtifacts.resolveReleaseFrameworkXml temp.Path primary expectedHash))

[<Theory>]
[<InlineData("absent")>]
[<InlineData("stale")>]
[<InlineData("wrong-tfm")>]
[<InlineData("wrong-major")>]
[<InlineData("stale-primary")>]
let ReferenceLookupRejectsAbsentStaleOrUnrelatedPackBytes(kind: string) =
    use temp = new TempDirectory()
    let primary = "packs/Microsoft.NETCore.App.Ref/10.0.12/ref/net10.0/System.Runtime.xml"
    let reviewed = "<doc><members /></doc>"
    let expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes reviewed)).ToLowerInvariant()
    let write (relative: string) (content: string) =
        let path = Path.Combine(temp.Path, relative)
        Directory.CreateDirectory(Path.GetDirectoryName path |> Option.ofObj |> Option.get) |> ignore
        File.WriteAllText(path, content)
    match kind with
    | "absent" -> ()
    | "stale" -> write "packs/Microsoft.NETCore.App.Ref/10.0.11/ref/net10.0/System.Runtime.xml" "stale"
    | "wrong-tfm" -> write "packs/Microsoft.NETCore.App.Ref/10.0.11/ref/net9.0/System.Runtime.xml" reviewed
    | "wrong-major" -> write "packs/Microsoft.NETCore.App.Ref/9.0.11/ref/net10.0/System.Runtime.xml" reviewed
    | "stale-primary" ->
        write primary "stale"
        write "packs/Microsoft.NETCore.App.Ref/10.0.11/ref/net10.0/System.Runtime.xml" reviewed
    | _ -> invalidArg "kind" kind
    Assert.Throws<ReleaseVerifySource.ReleaseVerifyFailure>(fun () -> ReleaseVerifyArtifacts.resolveReleaseFrameworkXml temp.Path primary expectedHash |> ignore) |> ignore

