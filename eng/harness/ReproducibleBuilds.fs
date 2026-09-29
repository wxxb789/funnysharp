module FunnySharp.Harness.ReproducibleBuilds

// A behaviour-identical F# port of eng/Compare-ReproducibleBuilds.ps1: the
// diagnostic that compares two externally prepared, clean FunnySharp checkouts
// layer by layer (assemblies, pdb/xml, packages) and writes a JSON report.
// A non-identical result is diagnostic, not a gate: byteIdentical:false still
// exits 0. Only the fail-closed preconditions produce exit 1.
//
// Contract text (verdict line and every precondition message) is byte-exact with
// the PowerShell original. The single deliberate deviation: a root that does not
// exist and a root without FunnySharp.slnx both fail with the PowerShell
// `Root is not a FunnySharp checkout: '<resolved>'.` message instead of
// Resolve-Path's own error, so a missing root is a typed harness failure.

open System
open System.Globalization
open System.IO
open System.IO.Compression
open System.Reflection
open System.Reflection.Metadata
open System.Reflection.PortableExecutable
open System.Security.Cryptography
open System.Text
open System.Text.Encodings.Web
open System.Text.Json
open System.Text.RegularExpressions
open FunnySharp.Harness.Output
open FunnySharp.Harness.Proc
open FunnySharp.Harness.Repo

// ---------------------------------------------------------------------------
// Evidence records
// ---------------------------------------------------------------------------

/// One hashed file: the absolute path, its byte length and lowercase-hex SHA-256.
type FileEvidence =
    { Path: string
      Length: int64
      Sha256: string }

/// An assembly adds the module version id and the assembly identity.
type AssemblyEvidence =
    { File: FileEvidence
      Mvid: string
      Identity: string }

/// One ZIP entry's metadata plus the hash of its decompressed content.
type ZipEntryEvidence =
    { Name: string
      Length: int64
      CompressedLength: int64
      LastWriteTimeUtc: string
      Sha256: string }

type ZipEvidence =
    { File: FileEvidence
      Entries: ZipEntryEvidence list }

type LockEvidence =
    { Path: string
      Sha256: string }

/// A prepared root: paths, commit/tree, controlled-input fingerprints.
type PreparedRoot =
    { Root: string
      Commit: string
      Tree: string
      InputPath: string
      Input: JsonElement
      InputSha256: string
      PackageDirectory: string
      SdkPolicy: FileEvidence
      BuildProps: FileEvidence
      Locks: LockEvidence list }

type AssemblyPair =
    { Path: string
      SameBytes: bool
      SameMvid: bool
      SameIdentity: bool
      Left: AssemblyEvidence
      Right: AssemblyEvidence }

type FilePair =
    { Path: string
      SameBytes: bool
      Left: FileEvidence
      Right: FileEvidence }

type PackagePair =
    { Name: string
      SameBytes: bool
      SameEntries: bool
      Left: ZipEvidence
      Right: ZipEvidence }

type Layer =
    | AssemblyLayer of AssemblyPair list
    | FileLayer of FilePair list
    | PackageLayer of PackagePair list

// ---------------------------------------------------------------------------
// Low-level helpers
// ---------------------------------------------------------------------------

let private toPosix (path: string) : string = path.Replace('\\', '/')

let private lowercaseHex (bytes: byte array) : string =
    Convert.ToHexString(bytes).ToLowerInvariant()

let private sha256File (path: string) : string =
    use stream = File.OpenRead path
    lowercaseHex (SHA256.HashData stream)

let private fileLength (path: string) : int64 = FileInfo(path).Length

let private gitArgs (arguments: string list) : string = String.Join(" ", arguments)

/// Run `git -C <root> <args>` and require success, mirroring Invoke-GitText's
/// stderr-merged, trimmed text result.
let private gitText
    (runGit: string -> string list -> ProcessResult)
    (root: string)
    (arguments: string list)
    : Result<string, HarnessError> =
    let result = runGit root arguments
    if result.ExitCode <> 0 then
        let output = (result.Stdout + result.Stderr).Trim()
        failError (sprintf "git %s failed in '%s': %s" (gitArgs arguments) root output)
    else
        let combined =
            [ result.Stdout; result.Stderr ] |> List.filter (fun text -> text <> "")

        Ok(String.Join(Environment.NewLine, combined).Trim())

let private jsonString (element: JsonElement) : string option =
    if element.ValueKind = JsonValueKind.String then
        match element.GetString() with
        | null -> None
        | value -> Some value
    else
        None

let private tryProperty (name: string) (element: JsonElement) : JsonElement option =
    if element.ValueKind <> JsonValueKind.Object then
        None
    else
        match element.TryGetProperty name with
        | true, value -> Some value
        | _ -> None

let private equalsIgnoringCase (left: string) (right: string) : bool =
    String.Equals(left, right, StringComparison.OrdinalIgnoreCase)

/// Structural JSON equality over parsed nodes, order-sensitive like the original
/// ConvertTo-Json -Compress + -ceq comparison.
let rec private jsonEqual (left: JsonElement) (right: JsonElement) : bool =
    match left.ValueKind, right.ValueKind with
    | JsonValueKind.Object, JsonValueKind.Object ->
        let leftProperties = left.EnumerateObject() |> Seq.map (fun p -> p.Name) |> List.ofSeq

        let rightProperties =
            right.EnumerateObject() |> Seq.map (fun p -> p.Name) |> List.ofSeq

        leftProperties = rightProperties
        && leftProperties
           |> List.forall (fun name ->
               match left.GetProperty name, right.GetProperty name with
               | l, r -> jsonEqual l r)
    | JsonValueKind.Array, JsonValueKind.Array ->
        let leftItems = left.EnumerateArray() |> List.ofSeq
        let rightItems = right.EnumerateArray() |> List.ofSeq

        leftItems.Length = rightItems.Length
        && List.forall2 jsonEqual leftItems rightItems
    | _ -> left.GetRawText() = right.GetRawText()

// ---------------------------------------------------------------------------
// Evidence collection
// ---------------------------------------------------------------------------

let private fileEvidence (path: string) : Result<FileEvidence, HarnessError> =
    if not (File.Exists path) then
        failError (sprintf "Build output was not found: '%s'." path)
    else
        Ok
            { Path = path
              Length = fileLength path
              Sha256 = sha256File path }

let private assemblyEvidence (path: string) : Result<AssemblyEvidence, HarnessError> =
    if not (File.Exists path) then
        failError (sprintf "Assembly was not found: '%s'." path)
    else
        let file =
            { Path = path
              Length = fileLength path
              Sha256 = sha256File path }

        // Metadata is read without LoadFile, which would lock the file and load
        // the compared assemblies into this process; PEReader yields the same
        // MVID and assembly identity.
        use stream = File.OpenRead path
        use peReader = new PEReader(stream)
        let metadataReader = peReader.GetMetadataReader()
        let mvid = metadataReader.GetGuid(metadataReader.GetModuleDefinition().Mvid).ToString()
        let identity = AssemblyName.GetAssemblyName(path).FullName

        Ok
            { File = file
              Mvid = mvid
              Identity = identity }

let private zipEvidence (path: string) : Result<ZipEvidence, HarnessError> =
    if not (File.Exists path) then
        failError (sprintf "Package was not found: '%s'." path)
    else
        let file =
            { Path = path
              Length = fileLength path
              Sha256 = sha256File path }

        use archive = ZipFile.OpenRead path

        let entries =
            archive.Entries
            |> Seq.sortBy (fun entry -> entry.FullName)
            |> Seq.map (fun entry ->
                use entryStream = entry.Open()
                let entryHash = lowercaseHex (SHA256.HashData entryStream)

                { Name = entry.FullName
                  Length = entry.Length
                  CompressedLength = entry.CompressedLength
                  LastWriteTimeUtc =
                    entry.LastWriteTime.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)
                  Sha256 = entryHash })
            |> List.ofSeq

        Ok { File = file; Entries = entries }

let private lockFilePattern = Regex(@"[\\/](?:bin|obj|artifacts)[\\/]", RegexOptions.IgnoreCase)

let private collectLocks (root: string) : LockEvidence list =
    Directory.EnumerateFiles(root, "packages.lock.json", SearchOption.AllDirectories)
    |> Seq.filter (fun fullName -> not (lockFilePattern.IsMatch fullName))
    |> Seq.sortWith (fun a b -> String.CompareOrdinal(a, b))
    |> Seq.map (fun fullName ->
        { Path = toPosix (Path.GetRelativePath(root, fullName))
          Sha256 = sha256File fullName })
    |> List.ofSeq

// ---------------------------------------------------------------------------
// Root preparation
// ---------------------------------------------------------------------------

let private prepareRoot
    (runGit: string -> string list -> ProcessResult)
    (root: string)
    : Result<PreparedRoot, HarnessError> =
    let resolved = Path.GetFullPath root

    if not (File.Exists(Path.Combine(resolved, SolutionFileName))) then
        failError (sprintf "Root is not a FunnySharp checkout: '%s'." resolved)
    else
        let prepared =
            gitText runGit resolved [ "status"; "--porcelain=v1"; "--untracked-files=all" ]
            |> Result.bind (fun status ->
                if not (String.IsNullOrWhiteSpace status) then
                    failError (sprintf "Reproducibility root must be clean: '%s'." resolved)
                else
                    let inputPath = Path.Combine(resolved, "artifacts", "reproducibility-input.json")

                    if not (File.Exists inputPath) then
                        failError (sprintf "Reproducibility input evidence was not found: '%s'." inputPath)
                    else
                        let inputText = File.ReadAllText inputPath

                        let input =
                            use document = JsonDocument.Parse inputText
                            document.RootElement.Clone()

                        gitText runGit resolved [ "rev-parse"; "HEAD" ]
                        |> Result.bind (fun commit ->
                            let schemaVersion =
                                tryProperty "schemaVersion" input
                                |> Option.bind (fun value ->
                                    if value.ValueKind = JsonValueKind.Number then
                                        match value.TryGetInt64() with
                                        | true, number -> Some number
                                        | _ -> None
                                    else
                                        None)

                            let candidateCommit =
                                tryProperty "candidateCommit" input |> Option.bind jsonString

                            let configuration =
                                tryProperty "configuration" input |> Option.bind jsonString

                            let isolatedCache =
                                tryProperty "isolatedNuGetCache" input
                                |> Option.map (fun value -> value.ValueKind = JsonValueKind.True)
                                |> Option.defaultValue false

                            let stale =
                                schemaVersion <> Some 1L
                                || not (
                                    candidateCommit
                                    |> Option.exists (fun value -> equalsIgnoringCase value commit)
                                )
                                || not (
                                    configuration
                                    |> Option.exists (fun value -> equalsIgnoringCase value "Release")
                                )
                                || not isolatedCache

                            if stale then
                                failError (
                                    sprintf "Reproducibility input evidence is incomplete or stale in '%s'." resolved
                                )
                            else
                                let cacheRelative =
                                    tryProperty "nugetPackagesDirectory" input
                                    |> Option.bind jsonString
                                    |> Option.defaultValue ""

                                let packageRelative =
                                    tryProperty "packageDirectory" input
                                    |> Option.bind jsonString
                                    |> Option.defaultValue ""

                                let cachePath = Path.GetFullPath(Path.Combine(resolved, cacheRelative))

                                let artifactsPath =
                                    Path.GetFullPath(Path.Combine(resolved, "artifacts"))
                                        .TrimEnd('\\', '/')

                                let boundary =
                                    artifactsPath + string Path.DirectorySeparatorChar

                                if not (cachePath.StartsWith(boundary, StringComparison.OrdinalIgnoreCase)) then
                                    failError (
                                        sprintf
                                            "NuGet cache is not isolated under the root's artifacts directory: '%s'."
                                            cachePath
                                    )
                                else
                                    let sdkPolicy = fileEvidence (Path.Combine(resolved, "global.json"))

                                    let buildProps =
                                        fileEvidence (Path.Combine(resolved, "Directory.Build.props"))

                                    match sdkPolicy, buildProps with
                                    | Error err, _ -> Error err
                                    | _, Error err -> Error err
                                    | Ok sdk, Ok props ->
                                        gitText runGit resolved [ "rev-parse"; "HEAD^{tree}" ]
                                        |> Result.map (fun tree ->
                                            { Root = resolved
                                              Commit = commit
                                              Tree = tree
                                              InputPath = inputPath
                                              Input = input
                                              InputSha256 = sha256File inputPath
                                              PackageDirectory =
                                                Path.GetFullPath(Path.Combine(resolved, packageRelative))
                                              SdkPolicy = sdk
                                              BuildProps = props
                                              Locks = collectLocks resolved })))

        prepared

// ---------------------------------------------------------------------------
// Comparison
// ---------------------------------------------------------------------------

let private assemblyRelativePaths =
    [ "src/FunnySharp/bin/Release/net10.0/FunnySharp.dll"
      "src/FunnySharp.AspNetCore/bin/Release/net10.0/FunnySharp.AspNetCore.dll" ]

let private fileRelativePaths =
    [ "src/FunnySharp/bin/Release/net10.0/FunnySharp.pdb"
      "src/FunnySharp.AspNetCore/bin/Release/net10.0/FunnySharp.AspNetCore.pdb"
      "src/FunnySharp/bin/Release/net10.0/FunnySharp.xml"
      "src/FunnySharp.AspNetCore/bin/Release/net10.0/FunnySharp.AspNetCore.xml" ]

let private packageNames =
    [ "FunnySharp.0.1.0.nupkg"
      "FunnySharp.0.1.0.snupkg"
      "FunnySharp.AspNetCore.0.1.0.nupkg"
      "FunnySharp.AspNetCore.0.1.0.snupkg" ]

let private collectAssemblies (left: PreparedRoot) (right: PreparedRoot) : Result<AssemblyPair list, HarnessError> =
    let collect =
        assemblyRelativePaths
        |> List.map (fun relativePath ->
            match assemblyEvidence (Path.Combine(left.Root, relativePath)) with
            | Error err -> Error err
            | Ok leftEvidence ->
                match assemblyEvidence (Path.Combine(right.Root, relativePath)) with
                | Error err -> Error err
                | Ok rightEvidence ->
                    Ok
                        { Path = relativePath
                          SameBytes = leftEvidence.File.Sha256 = rightEvidence.File.Sha256
                          SameMvid = leftEvidence.Mvid = rightEvidence.Mvid
                          SameIdentity = leftEvidence.Identity = rightEvidence.Identity
                          Left = leftEvidence
                          Right = rightEvidence })

    collectResults collect

let private collectFiles (left: PreparedRoot) (right: PreparedRoot) : Result<FilePair list, HarnessError> =
    let collect =
        fileRelativePaths
        |> List.map (fun relativePath ->
            match fileEvidence (Path.Combine(left.Root, relativePath)) with
            | Error err -> Error err
            | Ok leftEvidence ->
                match fileEvidence (Path.Combine(right.Root, relativePath)) with
                | Error err -> Error err
                | Ok rightEvidence ->
                    Ok
                        { Path = relativePath
                          SameBytes = leftEvidence.Sha256 = rightEvidence.Sha256
                          Left = leftEvidence
                          Right = rightEvidence })

    collectResults collect

let private collectPackages (left: PreparedRoot) (right: PreparedRoot) : Result<PackagePair list, HarnessError> =
    let collect =
        packageNames
        |> List.map (fun packageName ->
            match zipEvidence (Path.Combine(left.PackageDirectory, packageName)) with
            | Error err -> Error err
            | Ok leftEvidence ->
                match zipEvidence (Path.Combine(right.PackageDirectory, packageName)) with
                | Error err -> Error err
                | Ok rightEvidence ->
                    Ok
                        { Name = packageName
                          SameBytes = leftEvidence.File.Sha256 = rightEvidence.File.Sha256
                          SameEntries = leftEvidence.Entries = rightEvidence.Entries
                          Left = leftEvidence
                          Right = rightEvidence })

    collectResults collect

let private assemblyLayerSame (pairs: AssemblyPair list) : bool =
    pairs |> List.forall (fun pair -> pair.SameBytes && pair.SameMvid && pair.SameIdentity)

let private fileLayerSame (pairs: FilePair list) : bool = pairs |> List.forall (fun pair -> pair.SameBytes)

let private packageLayerSame (pairs: PackagePair list) : bool =
    pairs |> List.forall (fun pair -> pair.SameBytes && pair.SameEntries)

let private layerName (layer: Layer) : string =
    match layer with
    | AssemblyLayer _ -> "assemblies"
    | FileLayer _ -> "pdb-and-xml"
    | PackageLayer _ -> "packages"

let private layerSame (layer: Layer) : bool =
    match layer with
    | AssemblyLayer pairs -> assemblyLayerSame pairs
    | FileLayer pairs -> fileLayerSame pairs
    | PackageLayer pairs -> packageLayerSame pairs

// ---------------------------------------------------------------------------
// Report serialization
// ---------------------------------------------------------------------------

let private writeFileEvidence (writer: Utf8JsonWriter) (evidence: FileEvidence) : unit =
    writer.WriteStartObject()
    writer.WriteString("path", evidence.Path)
    writer.WriteNumber("length", evidence.Length)
    writer.WriteString("sha256", evidence.Sha256)
    writer.WriteEndObject()

let private writeAssemblyEvidence (writer: Utf8JsonWriter) (evidence: AssemblyEvidence) : unit =
    writer.WriteStartObject()
    writer.WriteString("path", evidence.File.Path)
    writer.WriteNumber("length", evidence.File.Length)
    writer.WriteString("sha256", evidence.File.Sha256)
    writer.WriteString("mvid", evidence.Mvid)
    writer.WriteString("identity", evidence.Identity)
    writer.WriteEndObject()

let private writeZipEvidence (writer: Utf8JsonWriter) (evidence: ZipEvidence) : unit =
    writer.WriteStartObject()
    writer.WriteString("path", evidence.File.Path)
    writer.WriteNumber("length", evidence.File.Length)
    writer.WriteString("sha256", evidence.File.Sha256)
    writer.WriteStartArray("entries")

    for entry in evidence.Entries do
        writer.WriteStartObject()
        writer.WriteString("name", entry.Name)
        writer.WriteNumber("length", entry.Length)
        writer.WriteNumber("compressedLength", entry.CompressedLength)
        writer.WriteString("lastWriteTimeUtc", entry.LastWriteTimeUtc)
        writer.WriteString("sha256", entry.Sha256)
        writer.WriteEndObject()

    writer.WriteEndArray()
    writer.WriteEndObject()

let private writeLayer (writer: Utf8JsonWriter) (layer: Layer) : unit =
    writer.WriteStartObject()
    writer.WriteString("name", layerName layer)
    writer.WriteBoolean("same", layerSame layer)
    writer.WriteStartArray("items")

    match layer with
    | AssemblyLayer pairs ->
        for pair in pairs do
            writer.WriteStartObject()
            writer.WriteString("path", pair.Path)
            writer.WriteBoolean("sameBytes", pair.SameBytes)
            writer.WriteBoolean("sameMvid", pair.SameMvid)
            writer.WriteBoolean("sameIdentity", pair.SameIdentity)
            writer.WritePropertyName "left"
            writeAssemblyEvidence writer pair.Left
            writer.WritePropertyName "right"
            writeAssemblyEvidence writer pair.Right
            writer.WriteEndObject()
    | FileLayer pairs ->
        for pair in pairs do
            writer.WriteStartObject()
            writer.WriteString("path", pair.Path)
            writer.WriteBoolean("sameBytes", pair.SameBytes)
            writer.WritePropertyName "left"
            writeFileEvidence writer pair.Left
            writer.WritePropertyName "right"
            writeFileEvidence writer pair.Right
            writer.WriteEndObject()
    | PackageLayer pairs ->
        for pair in pairs do
            writer.WriteStartObject()
            writer.WriteString("name", pair.Name)
            writer.WriteBoolean("sameBytes", pair.SameBytes)
            writer.WriteBoolean("sameEntries", pair.SameEntries)
            writer.WritePropertyName "left"
            writeZipEvidence writer pair.Left
            writer.WritePropertyName "right"
            writeZipEvidence writer pair.Right
            writer.WriteEndObject()

    writer.WriteEndArray()
    writer.WriteEndObject()

type private ComparisonReport =
    { GeneratedAtUtc: string
      CandidateCommit: string
      LeftRoot: string
      RightRoot: string
      SdkPolicySha256: string
      BuildPropsSha256: string
      LockFiles: LockEvidence list
      InputEvidenceSha256: string
      FirstDifference: string option
      ByteIdentical: bool
      Layers: Layer list }

let private serializeReport (report: ComparisonReport) : string =
    use stream = new MemoryStream()

    let options =
        JsonWriterOptions(
            Indented = true,
            NewLine = Environment.NewLine,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        )

    use writer = new Utf8JsonWriter(stream, options)
    writer.WriteStartObject()
    writer.WriteNumber("schemaVersion", 1)
    writer.WriteString("generatedAtUtc", report.GeneratedAtUtc)
    writer.WriteString("candidateCommit", report.CandidateCommit)
    writer.WriteString("leftRoot", report.LeftRoot)
    writer.WriteString("rightRoot", report.RightRoot)
    writer.WriteStartObject("controlledInputs")
    writer.WriteBoolean("same", true)
    writer.WriteString("sdkPolicySha256", report.SdkPolicySha256)
    writer.WriteString("buildPropsSha256", report.BuildPropsSha256)
    writer.WriteStartArray("lockFiles")

    for lock in report.LockFiles do
        writer.WriteStartObject()
        writer.WriteString("path", lock.Path)
        writer.WriteString("sha256", lock.Sha256)
        writer.WriteEndObject()

    writer.WriteEndArray()
    writer.WriteString("inputEvidenceSha256", report.InputEvidenceSha256)
    writer.WriteEndObject()

    writer.WritePropertyName "firstDifference"

    match report.FirstDifference with
    | Some name -> writer.WriteStringValue name
    | None -> writer.WriteNullValue()

    writer.WriteBoolean("byteIdentical", report.ByteIdentical)
    writer.WriteStartArray("layers")

    for layer in report.Layers do
        writeLayer writer layer

    writer.WriteEndArray()
    writer.WriteEndObject()
    writer.Flush()

    Encoding.UTF8.GetString(stream.ToArray()) + Environment.NewLine

// ---------------------------------------------------------------------------
// Orchestration
// ---------------------------------------------------------------------------

type CliOptions =
    { LeftRoot: string option
      RightRoot: string option
      OutputPath: string option
      Help: bool }

type private Outcome =
    { OutputPath: string
      FirstDifference: string option }

let private compare
    (runGit: string -> string list -> ProcessResult)
    (options: CliOptions)
    : Result<Outcome, HarnessError> =
    let leftRoot = Option.defaultValue "" options.LeftRoot
    let rightRoot = Option.defaultValue "" options.RightRoot

    match prepareRoot runGit leftRoot, prepareRoot runGit rightRoot with
    | Error err, _ -> Error err
    | _, Error err -> Error err
    | Ok left, Ok right ->
        if String.Equals(left.Root, right.Root, StringComparison.OrdinalIgnoreCase) then
            failError "LeftRoot and RightRoot must be different directories."
        elif left.Commit <> right.Commit || left.Tree <> right.Tree then
            failError (
                sprintf
                    "Roots do not contain the same commit and source tree: '%s' vs '%s'."
                    left.Commit
                    right.Commit
            )
        elif
            not (jsonEqual left.Input right.Input)
            || left.Locks <> right.Locks
            || left.SdkPolicy.Sha256 <> right.SdkPolicy.Sha256
            || left.BuildProps.Sha256 <> right.BuildProps.Sha256
        then
            failError "Controlled build inputs differ between roots."
        else
            let assembled =
                collectAssemblies left right
                |> Result.bind (fun pairs ->
                    collectFiles left right
                    |> Result.bind (fun files -> collectPackages left right |> Result.map (fun packages -> pairs, files, packages)))

            assembled
            |> Result.bind (fun (assemblyPairs, filePairs, packagePairs) ->
                let layers =
                    [ AssemblyLayer assemblyPairs
                      FileLayer filePairs
                      PackageLayer packagePairs ]

                let firstDifference =
                    layers |> List.tryFind (fun layer -> not (layerSame layer))

                let firstDifferenceName = firstDifference |> Option.map layerName
                let byteIdentical = firstDifference.IsNone

                let outputPath =
                    match options.OutputPath with
                    | Some path when not (String.IsNullOrWhiteSpace path) -> Path.GetFullPath path
                    | _ -> Path.Combine(left.Root, "artifacts", "reproducibility-comparison.json")

                let report =
                    { GeneratedAtUtc = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
                      CandidateCommit = left.Commit
                      LeftRoot = left.Root
                      RightRoot = right.Root
                      SdkPolicySha256 = left.SdkPolicy.Sha256
                      BuildPropsSha256 = left.BuildProps.Sha256
                      LockFiles = left.Locks
                      InputEvidenceSha256 = left.InputSha256
                      FirstDifference = firstDifferenceName
                      ByteIdentical = byteIdentical
                      Layers = layers }

                catchResult (fun () ->
                    match Path.GetDirectoryName outputPath with
                    | null -> ()
                    | parent -> Directory.CreateDirectory parent |> ignore

                    File.WriteAllText(outputPath, serializeReport report, UTF8Encoding(false)))

                |> Result.map (fun () ->
                    { OutputPath = outputPath
                      FirstDifference = firstDifferenceName }))

// ---------------------------------------------------------------------------
// Command line
// ---------------------------------------------------------------------------

let private usageLine =
    "usage: Compare-ReproducibleBuilds.ps1 -LeftRoot <path> -RightRoot <path> [-OutputPath <path>]"

let private helpText =
    String.concat
        Environment.NewLine
        [ usageLine
          ""
          "Compares two externally prepared clean FunnySharp checkouts by evidence layer."
          ""
          "parameters:"
          "  -LeftRoot <path>     (mandatory) the first clean checkout."
          "  -RightRoot <path>    (mandatory) the second clean checkout."
          "  -OutputPath <path>   report path (default: <LeftRoot>/artifacts/reproducibility-comparison.json)."
          "  -h, --help           show this help." ]

let private splitParameter (argument: string) : string * string option =
    let separatorIndex =
        argument.IndexOfAny([| ':'; '=' |], 1)
        |> fun index -> index

    if separatorIndex > 1 then
        argument.Substring(0, separatorIndex), Some(argument.Substring(separatorIndex + 1))
    else
        argument, None

let private parseArgs (argv: string list) : Result<CliOptions, string> =
    let empty =
        { LeftRoot = None
          RightRoot = None
          OutputPath = None
          Help = false }

    let rec loop (options: CliOptions) (remaining: string list) =
        match remaining with
        | [] -> Ok options
        | "-h" :: _ | "--help" :: _ -> Ok { options with Help = true }
        | argument :: rest when argument.StartsWith("-") && argument.Length > 1 ->
            let name, inlineValue = splitParameter argument

            let takeValue (setter: string -> CliOptions) =
                match inlineValue, rest with
                | Some value, _ -> loop (setter value) rest
                | None, value :: rest' -> loop (setter value) rest'
                | None, [] -> Error(sprintf "missing an argument for parameter '%s'" name)

            match name.ToLowerInvariant() with
            | "-leftroot" -> takeValue (fun value -> { options with LeftRoot = Some value })
            | "-rightroot" -> takeValue (fun value -> { options with RightRoot = Some value })
            | "-outputpath" -> takeValue (fun value -> { options with OutputPath = Some value })
            | _ -> Error(sprintf "unknown parameter '%s'" argument)
        | argument :: _ -> Error(sprintf "unexpected argument '%s'" argument)

    loop empty argv

let private missingMandatory (options: CliOptions) : string list =
    [ if options.LeftRoot.IsNone then
          yield "-LeftRoot"
      if options.RightRoot.IsNone then
          yield "-RightRoot" ]

/// Run the comparison writing to the supplied writers. Exit codes follow the
/// harness convention: 0 success (including a non-identical comparison),
/// 1 verification failure, 2 usage or environment failure.
let mainWith
    (stdout: TextWriter)
    (stderr: TextWriter)
    (runGit: string -> string list -> ProcessResult)
    (argv: string list)
    : int =
    match parseArgs argv with
    | Error message ->
        stderr.WriteLine usageLine
        stderr.WriteLine("ERROR: " + sprintf "Compare-ReproducibleBuilds.ps1: %s" message)
        2
    | Ok options when options.Help ->
        stdout.WriteLine helpText
        0
    | Ok options ->
        match missingMandatory options with
        | missing when not missing.IsEmpty ->
            stderr.WriteLine usageLine
            stderr.WriteLine(
                "ERROR: "
                + sprintf "Missing mandatory parameter(s): %s." (String.Join(", ", missing))
            )
            2
        | _ ->
            match catchResult (fun () -> compare runGit options) with
            | Ok (Ok outcome) ->
                stdout.WriteLine(
                    sprintf
                        "Reproducibility comparison written to '%s'. First difference: %s."
                        outcome.OutputPath
                        (Option.defaultValue "none" outcome.FirstDifference)
                )

                0
            | Ok (Error err)
            | Error err ->
                stderr.WriteLine("ERROR: " + err.Message)
                err.ExitCode

/// Entry point mirroring Compare-ReproducibleBuilds.ps1's main flow.
let main (argv: string array) : int =
    let runGit root arguments =
        runCaptureSync "git" ([ "-C"; root ] @ arguments)

    mainWith Console.Out Console.Error runGit (List.ofArray argv)
