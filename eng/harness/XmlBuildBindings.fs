module FunnySharp.Harness.XmlBuildBindings

open System
open System.Globalization
open System.IO
open System.Reflection
open System.Reflection.Metadata
open System.Reflection.PortableExecutable
open System.Security.Cryptography
open System.Text.Json
open System.Text.Json.Nodes
open System.Text.RegularExpressions

let anchorPath = "eng/api-baseline/xml-reviewed-inputs.json"
let private policyPath = "eng/api-baseline/xml-contract-bindings.json"
let private receiptName = "xml-build-bindings.json"
let private names = [ "FunnySharp"; "FunnySharp.AspNetCore" ]

let private require condition message =
    if not condition then invalidOp ("XML build binding: " + message)

let private field (key: string) (node: JsonElement) =
    require (node.ValueKind = JsonValueKind.Object) "expected an object."
    match node.TryGetProperty key with
    | true, value -> value
    | _ -> invalidOp ("XML build binding: missing field " + key)

let private text key node =
    let value = field key node
    require (value.ValueKind = JsonValueKind.String) ("expected string " + key)
    value.GetString() |> Option.ofObj |> Option.defaultValue ""

let private rows key node =
    let value = field key node
    require (value.ValueKind = JsonValueKind.Array) ("expected array " + key)
    value.EnumerateArray() |> Seq.toList

let private fields (expected: string list) (node: JsonElement) =
    require (node.ValueKind = JsonValueKind.Object) "expected an object."
    let actual = node.EnumerateObject() |> Seq.map (fun value -> value.Name) |> Seq.toList
    require (actual.Length = expected.Length && Set.ofList actual = Set.ofList expected) "unexpected or duplicate machine fields."

let private hash (path: string) =
    use stream = File.OpenRead path
    Convert.ToHexString(SHA256.HashData stream).ToLowerInvariant()

let private safeFile root (relative: string) =
    require (not (Path.IsPathRooted relative) && not (relative.Contains '\\')) "path must be repository-relative."
    require (relative.Split('/') |> Array.forall (fun part -> part <> "" && part <> "." && part <> "..")) "path contains traversal."
    let full = Path.GetFullPath(Path.Combine(root, relative))
    require (File.Exists full) ("file is missing: " + relative)
    let mutable entry: FileSystemInfo = FileInfo full
    let boundary = Path.TrimEndingDirectorySeparator(Path.GetFullPath root)
    while entry.FullName <> boundary do
        require ((entry.Attributes &&& FileAttributes.ReparsePoint) = enum 0) ("linked path: " + relative)
        entry <-
            match Directory.GetParent entry.FullName with
            | null -> invalidOp "XML build binding: path escaped its root."
            | parent -> parent
    full

let private checkedHash root path (expected: string) =
    require (Regex.IsMatch(expected, "^[0-9a-f]{64}$")) "invalid SHA256."
    let full = safeFile root path
    require (hash full = expected) ("bytes changed: " + path)
    full

let required root =
    File.Exists(Path.Combine(root, anchorPath))
    || File.Exists(Path.Combine(root, policyPath))
    || File.Exists(Path.Combine(root, "src/FunnySharp/Option.cs"))

let private assemblyPath name extension =
    "src/" + name + "/bin/Release/net10.0/" + name + extension

/// Detect cached metadata as well as a substituted loaded location, without normalizing PE bytes.
let assertLoadedAssembly (assembly: Assembly) path expectedHash =
    require (hash assembly.Location = expectedHash) "loaded assembly bytes differ from the seal."
    use stream = File.OpenRead path
    use pe = new PEReader(stream)
    let metadata = pe.GetMetadataReader()
    require (assembly.ManifestModule.ModuleVersionId = metadata.GetGuid(metadata.GetModuleDefinition().Mvid)) "loaded assembly metadata differs from the sealed PE."

let private reviewedInputs root =
    let anchor = safeFile root anchorPath
    use document = JsonDocument.Parse(File.ReadAllText anchor)
    let node = document.RootElement
    fields [ "schema"; "scope"; "reviewedPolicy"; "files"; "assemblies"; "reviewBasis"; "historicalExactReviewPreserved" ] node
    require (text "schema" node = "funnysharp-reviewed-xml-inputs/v2") "unexpected reviewed-input schema."
    let policy = field "reviewedPolicy" node
    fields [ "path"; "sha256" ] policy
    require (text "path" policy = policyPath) "wrong reviewed policy path."
    checkedHash root policyPath (text "sha256" policy) |> ignore
    use policyDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, policyPath)))
    let files = rows "files" node
    let expected = files |> List.map (fun file ->
        fields [ "path"; "sha256" ] file
        let path = text "path" file
        checkedHash root path (text "sha256" file) |> ignore
        path)
    require (Set.count (Set.ofList expected) = expected.Length) "duplicate reviewed input."
    let rec shippingFiles directory =
        [ for file in Directory.GetFiles directory do
              if Path.GetExtension file <> ".md" then
                  yield Path.GetRelativePath(root, file).Replace('\\', '/')
          for child in Directory.GetDirectories directory do
              if Path.GetFileName child <> "bin" && Path.GetFileName child <> "obj" then
                  require ((File.GetAttributes child &&& FileAttributes.ReparsePoint) = enum 0) "linked shipping directory."
                  yield! shippingFiles child ]
    let actual =
        shippingFiles (Path.Combine(root, "src"))
        @ [ "global.json" ]
        @ (Directory.GetFiles root |> Array.filter (fun path ->
            let extension = Path.GetExtension path
            extension = ".props" || extension = ".targets" || extension = ".rsp")
           |> Array.map (fun path -> Path.GetFileName path |> Option.ofObj |> Option.defaultWith (fun () -> invalidOp "XML build binding: unnamed input.")) |> Array.toList)
    require (Set.ofList expected = Set.ofList actual) "shipping input inventory changed."
    let assemblies = rows "assemblies" node
    require (assemblies.Length = names.Length && (assemblies |> List.map (text "name") |> Set.ofList) = Set.ofList names) "reviewed assembly inventory changed."
    for assembly in assemblies do
        fields [ "name"; "xmlSha256"; "lfXmlSha256" ] assembly
        let name = text "name" assembly
        let primary = text "xmlSha256" assembly
        let representations = [ primary; text "lfXmlSha256" assembly ]
        require (representations |> List.forall (fun value -> Regex.IsMatch(value, "^[0-9a-f]{64}$"))) "invalid reviewed XML SHA256."
        let historical = rows "assemblies" policyDocument.RootElement |> List.filter (fun value -> text "name" value = name)
        require (historical.Length = 1 && text "xmlSha256" historical.Head = primary) "primary XML review differs from the historical policy."
        let path = assemblyPath name ".xml"
        require (List.contains (hash (safeFile root path)) representations) ("bytes changed: " + path)
    hash anchor, text "sha256" policy

let private buildReceipt root directory relative =
    require (relative = "receipts/03-build.json") "wrong required build receipt path."
    let path = safeFile directory relative
    use document = JsonDocument.Parse(File.ReadAllText path)
    let node = document.RootElement
    require (text "name" node = "build" && text "fileName" node = "dotnet"
             && (field "exitCode" node).GetInt32() = 0 && (field "schemaVersion" node).GetInt32() = 1
             && Path.TrimEndingDirectorySeparator(Path.GetFullPath(text "workingDirectory" node)) = Path.TrimEndingDirectorySeparator(Path.GetFullPath root)
             && (rows "arguments" node |> List.map (fun value -> value.GetString() |> Option.ofObj |> Option.defaultValue "")) =
                 [ "build"; "FunnySharp.slnx"; "--configuration"; "Release"; "--no-restore" ]) "required build did not succeed with the canonical command."
    hash path, DateTimeOffset.Parse(text "completedAtUtc" node, CultureInfo.InvariantCulture)

let private identity (commit: string) (attempt: string) =
    require (Regex.IsMatch(commit, "^[0-9a-f]{40}$") && Regex.IsMatch(attempt, "^[A-Za-z0-9][A-Za-z0-9._-]*$")) "invalid candidate or attempt."

/// Called only after the required build has returned zero and its command receipt is on disk.
let capture root directory commit attempt buildRelative (capturedAt: DateTimeOffset) : JsonObject =
    identity commit attempt
    let anchorSha, policySha = reviewedInputs root
    let buildSha, completedAt = buildReceipt root directory buildRelative
    require (capturedAt >= completedAt) "capture predates build completion."
    let artifactRows =
        [| for name in names do
               yield {| name = name
                        dllPath = assemblyPath name ".dll"
                        dllSha256 = hash (safeFile root (assemblyPath name ".dll"))
                        xmlPath = assemblyPath name ".xml"
                        xmlSha256 = hash (safeFile root (assemblyPath name ".xml"))
                        pdbPath = assemblyPath name ".pdb"
                        pdbSha256 = hash (safeFile root (assemblyPath name ".pdb")) |} |]
    let receipt =
        {| schema = "funnysharp-xml-build-bindings/v1"
           candidateCommit = commit
           attemptId = attempt
           capturedAtUtc = capturedAt.ToString("O", CultureInfo.InvariantCulture)
           reviewedInputs = {| path = anchorPath; sha256 = anchorSha |}
           reviewedPolicy = {| path = policyPath; sha256 = policySha |}
           buildCommandReceipt = {| path = buildRelative; sha256 = buildSha |}
           assemblies = artifactRows |}
    use stream = new FileStream(Path.Combine(directory, receiptName), FileMode.CreateNew, FileAccess.Write, FileShare.None)
    JsonSerializer.Serialize(stream, receipt, JsonSerializerOptions(WriteIndented = true))
    stream.Flush()
    stream.Dispose()
    let pointer = JsonObject()
    pointer.["path"] <- JsonValue.Create receiptName
    pointer.["sha256"] <- JsonValue.Create(hash (Path.Combine(directory, receiptName)))
    pointer

/// Consume external sealed evidence; never create or refresh it during verification.
let validate root directory candidate (validatedExecution: JsonNode) : Map<string, string * string> =
    let manifestPath = safeFile directory "execution-evidence.json"
    use executionDocument = JsonDocument.Parse(validatedExecution.ToJsonString())
    let execution = executionDocument.RootElement
    require (Path.TrimEndingDirectorySeparator(text "directory" execution) = Path.TrimEndingDirectorySeparator(Path.GetFullPath directory)
             && text "manifest" execution = "execution-evidence.json"
             && text "manifestSha256" execution = hash manifestPath) "execution manifest was not validated."
    use manifestDocument = JsonDocument.Parse(File.ReadAllText manifestPath)
    let manifest = manifestDocument.RootElement
    let attempt = text "attemptId" manifest
    identity candidate attempt
    require (text "candidateCommit" manifest = candidate) "wrong execution candidate."
    let pointer = field "xmlBuildBindings" manifest
    fields [ "path"; "sha256" ] pointer
    require (text "path" pointer = receiptName) "wrong binding receipt path."
    let receiptPath = checkedHash directory receiptName (text "sha256" pointer)
    use document = JsonDocument.Parse(File.ReadAllText receiptPath)
    let node = document.RootElement
    fields [ "schema"; "candidateCommit"; "attemptId"; "capturedAtUtc"; "reviewedInputs"; "reviewedPolicy"; "buildCommandReceipt"; "assemblies" ] node
    require (text "schema" node = "funnysharp-xml-build-bindings/v1"
             && text "candidateCommit" node = candidate && text "attemptId" node = attempt) "wrong binding schema, candidate or attempt."
    let anchorSha, policySha = reviewedInputs root
    for key, path, sha in [ "reviewedInputs", anchorPath, anchorSha; "reviewedPolicy", policyPath, policySha ] do
        let binding = field key node
        fields [ "path"; "sha256" ] binding
        require (text "path" binding = path && text "sha256" binding = sha) ("stale " + key)
    let build = field "buildCommandReceipt" node
    fields [ "path"; "sha256" ] build
    let buildRelative = text "path" build
    let buildSha, completedAt = buildReceipt root directory buildRelative
    require (text "sha256" build = buildSha) "build command receipt changed."
    let commands = rows "commands" execution |> List.filter (fun command -> text "name" command = "build")
    require (commands.Length = 1 && text "receipt" commands.Head = buildRelative
             && text "receiptSha256" commands.Head = buildSha) "build receipt is not in validated execution."
    let capturedAt = DateTimeOffset.Parse(text "capturedAtUtc" node, CultureInfo.InvariantCulture)
    require (capturedAt >= completedAt) "capture predates build completion."
    let subsequent = rows "commands" manifest |> List.skipWhile (fun command -> text "name" command <> "build") |> List.skip 1
    match subsequent with
    | next :: _ -> require (capturedAt <= DateTimeOffset.Parse(text "startedAtUtc" next, CultureInfo.InvariantCulture)) "capture was not immediately after build."
    | [] -> ()
    let assemblies = rows "assemblies" node
    require (assemblies.Length = names.Length && (assemblies |> List.map (text "name") |> Set.ofList) = Set.ofList names) "sealed assembly inventory changed."
    assemblies |> List.map (fun assembly ->
        fields [ "name"; "dllPath"; "dllSha256"; "xmlPath"; "xmlSha256"; "pdbPath"; "pdbSha256" ] assembly
        let name = text "name" assembly
        for kind in [ "dll"; "xml"; "pdb" ] do
            let path = assemblyPath name ("." + kind)
            require (text (kind + "Path") assembly = path) "wrong sealed assembly path."
            checkedHash root path (text (kind + "Sha256") assembly) |> ignore
        name, (text "dllSha256" assembly, text "xmlSha256" assembly))
    |> Map.ofList
