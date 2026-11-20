module FunnySharp.Harness.Compatibility

// Restores, publishes and runs package consumers using an isolated cache.
// The package archive contract is checked directly, without evidence replay.

open System
open System.Globalization
open System.IO
open System.IO.Compression
open System.Runtime.InteropServices
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open System.Text.RegularExpressions
open FunnySharp.Harness.Output

[<Literal>]
let private defaultPackageFeed = "https://api.nuget.org/v3/index.json"

/// The host RID used for the default -RuntimeIdentifier and the equality check.
/// Tests override this to exercise the mismatch path deterministically.
let mutable hostRuntimeIdentifier = RuntimeInformation.RuntimeIdentifier

/// Whether the current host is a platform the runner supports. Tests override.
let mutable hostOsSupported =
    OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()

/// One consumer scenario: the project to publish, the app assembly name, the target
/// framework lane, and the MSBuild properties that turn trimming / Native AOT on.
type ScenarioDefinition =
    { Name: string
      Project: string
      AssemblyName: string
      TargetFramework: string
      PublishProperties: string list }

/// The supported runtime lanes every compatibility run covers.
let targetFrameworks = [ "net10.0"; "net11.0" ]

/// The default scenario set: both package surfaces, trimming and Native AOT, in both lanes.
let defaultScenarios =
    [ "CoreTrimmed"; "CoreTrimmedNet11"
      "CoreNativeAot"; "CoreNativeAotNet11"
      "AspNetCoreTrimmed"; "AspNetCoreTrimmedNet11"
      "AspNetCoreNativeAot"; "AspNetCoreNativeAotNet11" ]

let private smokeScenarios =
    [ "CoreSmoke"; "CoreSmokeNet11"; "AspNetCoreSmoke"; "AspNetCoreSmokeNet11" ]

let private knownScenarioNames = Set.ofList (defaultScenarios @ smokeScenarios)

/// Every accepted scenario name mapped to its consumer project under
/// <paramref name="repositoryRoot"/>.
let scenarioDefinitions (repositoryRoot: string) : Map<string, ScenarioDefinition> =
    let compatibilityRoot =
        Path.Combine(repositoryRoot, "tests", "FunnySharp.Compatibility")

    let coreProject =
        Path.Combine(compatibilityRoot, "FunnySharp.Compatibility.Core", "FunnySharp.Compatibility.Core.csproj")

    let aspNetCoreProject =
        Path.Combine(
            compatibilityRoot,
            "FunnySharp.Compatibility.AspNetCore",
            "FunnySharp.Compatibility.AspNetCore.csproj"
        )

    let trimmed =
        [ "-p:PublishTrimmed=true"; "-p:TrimMode=full"; "-p:RootShippingAssemblies=true" ]

    let nativeAot =
        [ "-p:PublishTrimmed=true"
          "-p:TrimMode=full"
          "-p:PublishAot=true"
          "-p:IsAotCompatible=true"
          "-p:RootShippingAssemblies=false" ]

    let definition name project assemblyName targetFramework props =
        { Name = name
          Project = project
          AssemblyName = assemblyName
          TargetFramework = targetFramework
          PublishProperties = props }

    let core name targetFramework props =
        definition name coreProject "FunnySharp.Compatibility.Core" targetFramework props

    let aspNetCore name targetFramework props =
        definition name aspNetCoreProject "FunnySharp.Compatibility.AspNetCore" targetFramework props

    [ "CoreSmoke", core "CoreSmoke" "net10.0" []
      "CoreSmokeNet11", core "CoreSmokeNet11" "net11.0" []
      "CoreTrimmed", core "CoreTrimmed" "net10.0" trimmed
      "CoreTrimmedNet11", core "CoreTrimmedNet11" "net11.0" trimmed
      "CoreNativeAot", core "CoreNativeAot" "net10.0" nativeAot
      "CoreNativeAotNet11", core "CoreNativeAotNet11" "net11.0" nativeAot
      "AspNetCoreSmoke", aspNetCore "AspNetCoreSmoke" "net10.0" []
      "AspNetCoreSmokeNet11", aspNetCore "AspNetCoreSmokeNet11" "net11.0" []
      "AspNetCoreTrimmed", aspNetCore "AspNetCoreTrimmed" "net10.0" trimmed
      "AspNetCoreTrimmedNet11", aspNetCore "AspNetCoreTrimmedNet11" "net11.0" trimmed
      "AspNetCoreNativeAot", aspNetCore "AspNetCoreNativeAot" "net10.0" nativeAot
      "AspNetCoreNativeAotNet11", aspNetCore "AspNetCoreNativeAotNet11" "net11.0" nativeAot ]
    |> Map.ofList

// ---- process runner (injectable so tests never touch the SDK) ----

/// Runs one child process with inherited stdio and the supplied environment
/// overrides, returning its exit code. The default starts the process directly
/// (never a shell); tests replace it with a fake.
let mutable processRunner : string -> (string * string) list -> string list -> int =
    fun workingDirectory environment command ->
        match command with
        | [] -> invalidArg "command" "the command list must not be empty"
        | exe :: args ->
            use proc = new Diagnostics.Process()
            let info = Diagnostics.ProcessStartInfo exe
            info.UseShellExecute <- false
            info.WorkingDirectory <- workingDirectory

            for arg in args do
                info.ArgumentList.Add arg

            for key, value in environment do
                info.Environment.[key] <- value

            proc.StartInfo <- info

            if not (proc.Start()) then
                failwithf "failed to start process '%s'" exe

            proc.WaitForExit()
            proc.ExitCode

// ---- small helpers ----

type private RailBuilder() =
    member _.Bind(value: Result<'T, HarnessError>, f: 'T -> Result<'U, HarnessError>) : Result<'U, HarnessError> =
        Result.bind f value

    member _.Return(value: 'T) : Result<'T, HarnessError> = Ok value
    member _.ReturnFrom(value: Result<'T, HarnessError>) : Result<'T, HarnessError> = value
    member _.Zero() : Result<unit, HarnessError> = Ok()
    member _.Delay(f: unit -> Result<'T, HarnessError>) : unit -> Result<'T, HarnessError> = f
    member _.Run(f: unit -> Result<'T, HarnessError>) : Result<'T, HarnessError> = f ()

let private rail = RailBuilder()

let private usageError (message: string) : Result<'T, HarnessError> =
    Error(Errors.withExitCode 2 message)

let private usageFail (message: string) : Result<unit, HarnessError> =
    Error(Errors.withExitCode 2 message)

/// PowerShell's [System.Security.SecurityElement]::Escape.
/// The five XML text escapes, delegating to the same framework call the PowerShell original used
/// (SecurityElement.Escape escapes &, <, >, " and ' in one pass, so an already-escaped entity is
/// escaped again exactly as before).
let private escapeXml (value: string) : string = System.Security.SecurityElement.Escape value

/// Resolve a directory path to its canonical, symlink-free form, mirroring
/// PowerShell's Resolve-Path for the artifacts directory.
let private resolveDirectoryPath (path: string) : string =
    try
        match Directory.ResolveLinkTarget(path, true) with
        | null -> Path.GetFullPath path
        | target -> target.FullName
    with _ ->
        Path.GetFullPath path

let private pathComparison () : StringComparison =
    if OperatingSystem.IsWindows() then StringComparison.OrdinalIgnoreCase else StringComparison.Ordinal

let private pathAtOrWithin (childPath: string) (parentPath: string) : bool =
    let comparison = pathComparison ()
    let prefix = parentPath.TrimEnd('\\', '/') + string Path.DirectorySeparatorChar
    childPath.Equals(parentPath, comparison) || childPath.StartsWith(prefix, comparison)

/// The security boundary: <paramref name="path"/> must be a proper, non-reparse
/// subdirectory of the artifacts directory. Fails closed otherwise.
let private assertSafeArtifactsSubdirectory (artifactsDirectory: string) (path: string) : Result<unit, HarnessError> =
    let fullPath = Path.GetFullPath path

    if fullPath.Equals(artifactsDirectory, pathComparison ()) || not (pathAtOrWithin fullPath artifactsDirectory) then
        usageFail (sprintf "Path must be a proper subdirectory of '%s': '%s'." artifactsDirectory fullPath)
    else
        let relative = Path.GetRelativePath(artifactsDirectory, fullPath)

        let segments =
            relative.Split(
                [| Path.DirectorySeparatorChar; Path.AltDirectorySeparatorChar |],
                StringSplitOptions.RemoveEmptyEntries
            )

        let mutable current = artifactsDirectory
        let mutable failure: HarnessError option = None

        for segment in segments do
            if failure.IsNone then
                current <- Path.Combine(current, segment)

                if Directory.Exists current || File.Exists current then
                    let attributes = File.GetAttributes current

                    if attributes.HasFlag FileAttributes.ReparsePoint then
                        failure <-
                            Some(
                                Errors.withExitCode
                                    2
                                    (sprintf "Path cannot contain a reparse point: '%s'." current)
                            )
                    else
                        let resolved = Path.GetFullPath current

                        if not (pathAtOrWithin resolved artifactsDirectory) then
                            failure <-
                                Some(
                                    Errors.withExitCode
                                        2
                                        (sprintf "Resolved path escapes '%s': '%s'." artifactsDirectory resolved)
                                )
                        else
                            current <- resolved

        match failure with
        | Some err -> Error err
        | None -> Ok()

let private resetDirectory (artifactsDirectory: string) (path: string) : unit =
    if Directory.Exists path then
        match assertSafeArtifactsSubdirectory artifactsDirectory path with
        | Error err -> failwith err.Message
        | Ok() -> Directory.Delete(path, true)
    elif File.Exists path then
        match assertSafeArtifactsSubdirectory artifactsDirectory path with
        | Error err -> failwith err.Message
        | Ok() -> File.Delete path

    Directory.CreateDirectory path |> ignore

/// The archived entries the package gate requires for one package, in lane order.
let private requiredPackageEntries (expectedId: string) : string list =
    let lanes =
        targetFrameworks
        |> List.collect (fun targetFramework ->
            [ sprintf "lib/%s/%s.dll" targetFramework expectedId
              sprintf "lib/%s/%s.xml" targetFramework expectedId ])

    if expectedId = "FunnySharp" then
        lanes
        @ [ "analyzers/dotnet/cs/FunnySharp.Analyzers.dll"
            "analyzers/dotnet/cs/FunnySharp.Analyzers.CodeFixes.dll" ]
    else
        lanes

/// Validate one packed package against the two-lane contract and return the verified
/// assembly/metadata entries. Rejects a package that drops either lane's DLL or metadata.
let validatePackage (path: string) (expectedId: string) (expectedVersion: string) (coreVersion: string) : string list =
    use archive = ZipFile.OpenRead path
    let requiredEntry name =
        match archive.GetEntry name with
        | null -> invalidOp (sprintf "Package '%s' is missing '%s'." expectedId name)
        | entry -> entry
    let nuspecs = archive.Entries |> Seq.filter (fun entry -> entry.FullName.EndsWith(".nuspec", StringComparison.Ordinal)) |> Seq.toList
    let nuspec =
        match nuspecs with
        | [ entry ] -> entry
        | _ -> invalidOp "Package must contain exactly one nuspec."
    use stream = nuspec.Open()
    let document = System.Xml.Linq.XDocument.Load stream
    let elements name = document.Descendants() |> Seq.filter (fun item -> item.Name.LocalName = name) |> Seq.toList
    let text name = elements name |> List.tryHead |> Option.map (fun item -> item.Value) |> Option.defaultValue ""
    let attribute name (item: System.Xml.Linq.XElement) =
        match item.Attribute(System.Xml.Linq.XName.Get name) with
        | null -> ""
        | value -> value.Value
    if text "id" <> expectedId || text "version" <> expectedVersion then invalidOp "Package nuspec identity does not match its filename."
    for entry in "README.md" :: requiredPackageEntries expectedId do
        requiredEntry entry |> ignore
    if text "readme" <> "README.md" || text "license" <> "MIT"
       || (elements "license" |> List.exists (fun item -> attribute "type" item <> "expression")) then
        invalidOp "Package must declare its README and MIT license."
    let dependencyGroups = elements "dependencies" |> List.collect (fun item -> item.Elements() |> Seq.toList)
    let dependencyFrameworks = dependencyGroups |> List.map (attribute "targetFramework") |> Set.ofList
    if dependencyGroups.Length <> targetFrameworks.Length
       || dependencyFrameworks <> Set.ofList targetFrameworks then
        invalidOp (
            sprintf "Package must have exactly the dependency groups for %s." (String.concat ", " targetFrameworks)
        )
    for group in dependencyGroups do
        let lane = attribute "targetFramework" group
        let dependencies = group.Elements() |> Seq.toList
        if expectedId = "FunnySharp" then
            if not dependencies.IsEmpty then
                invalidOp (sprintf "FunnySharp must have no runtime dependencies in its %s group." lane)
        else if dependencies.Length <> 1 || attribute "id" dependencies.[0] <> "FunnySharp"
                || attribute "version" dependencies.[0] <> coreVersion then
            invalidOp (
                sprintf
                    "FunnySharp.AspNetCore must depend only on the candidate FunnySharp version in its %s group."
                    lane
            )
    let frameworkGroups = elements "frameworkReferences" |> List.collect (fun item -> item.Elements() |> Seq.toList)
    if expectedId = "FunnySharp" then
        if not frameworkGroups.IsEmpty then invalidOp "FunnySharp must have no framework references."
    else
        let frameworkFrameworks = frameworkGroups |> List.map (attribute "targetFramework") |> Set.ofList
        if frameworkGroups.Length <> targetFrameworks.Length
           || frameworkFrameworks <> Set.ofList targetFrameworks then
            invalidOp (
                sprintf
                    "FunnySharp.AspNetCore must have one framework reference group per lane: %s."
                    (String.concat ", " targetFrameworks)
            )
        for group in frameworkGroups do
            let references = group.Elements() |> Seq.toList
            if references.Length <> 1 || attribute "name" references.[0] <> "Microsoft.AspNetCore.App" then
                invalidOp (
                    sprintf
                        "FunnySharp.AspNetCore must reference only Microsoft.AspNetCore.App in its %s group."
                        (attribute "targetFramework" group)
                )
    requiredPackageEntries expectedId

let isolatedNuGetPackagesDirectory (artifactsDirectory: string) (_outputDirectory: string) : string =
    // Native AOT package assets exceed Windows path limits under nested attempt directories.
    Path.Combine(artifactsDirectory, "nuget", Guid.NewGuid().ToString("N"))

let private packageVersion (packageDirectory: string) (packageId: string) : Result<string, HarnessError> =
    let pattern =
        Regex("^" + Regex.Escape packageId + @"\.(?<version>[0-9][0-9A-Za-z.+-]*)\.nupkg$", RegexOptions.IgnoreCase)

    let matching =
        Directory.GetFiles packageDirectory
        |> Array.map Path.GetFileName
        |> Array.filter (fun name -> pattern.IsMatch name)

    if matching.Length <> 1 then
        usageError (
            sprintf "Expected exactly one %s package in '%s', but found %d." packageId packageDirectory matching.Length
        )
    else
        Ok(pattern.Match(matching.[0]).Groups.["version"].Value)

let private validatePackageInventory (packageDirectory: string) (version: string) : Result<unit, HarnessError> =
    let expected =
        [ for id in [ "FunnySharp"; "FunnySharp.AspNetCore" ] do
              for extension in [ ".nupkg"; ".snupkg" ] do
                  yield id + "." + version + extension ]
        |> Set.ofList
    let actual =
        Directory.GetFiles packageDirectory
        |> Array.filter (fun path ->
            let extension = Path.GetExtension path
            String.Equals(extension, ".nupkg", StringComparison.OrdinalIgnoreCase)
            || String.Equals(extension, ".snupkg", StringComparison.OrdinalIgnoreCase))
        |> Array.map Path.GetFileName
        |> Set.ofArray
    if actual <> expected then
        usageFail (
            sprintf
                "Package inventory must contain exactly the two candidate nupkg files and their matching snupkg files. Missing: [%s]. Unexpected: [%s]."
                (String.concat ", " (Set.difference expected actual))
                (String.concat ", " (Set.difference actual expected))
        )
    else
        Ok()

/// The NuGet.Config the runner writes into the output directory. The local feed
/// comes first and package-source mapping keeps FunnySharp* resolving only there.
let nuGetConfigText (localSource: string) (upstreamSource: string) : string =
    String.concat
        "\n"
        [ "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
          "<configuration>"
          "  <packageSources>"
          "    <clear />"
          sprintf "    <add key=\"local-release-packages\" value=\"%s\" />" (escapeXml localSource)
          sprintf "    <add key=\"upstream\" value=\"%s\" />" (escapeXml upstreamSource)
          "  </packageSources>"
          "  <packageSourceMapping>"
          "    <packageSource key=\"local-release-packages\">"
          "      <package pattern=\"FunnySharp\" />"
          "      <package pattern=\"FunnySharp.AspNetCore\" />"
          "    </packageSource>"
          "    <packageSource key=\"upstream\">"
          "      <package pattern=\"Microsoft.*\" />"
          "      <package pattern=\"runtime.*\" />"
          "      <package pattern=\"System.*\" />"
          "      <package pattern=\"NETStandard.Library\" />"
          "      <package pattern=\"NuGet.*\" />"
          "    </packageSource>"
          "  </packageSourceMapping>"
          "</configuration>" ]
    + "\n"

// ---- results model and JSON encoding ----

type private ScenarioResult =
    { Scenario: string
      Outcome: string
      StartedAtUtc: string
      FinishedAtUtc: string
      TargetFramework: string
      RuntimeIdentifier: string
      PackageFeed: string
      Project: string
      PublishProperties: string list
      CorePackageVersion: string
      AspNetCorePackageVersion: string
      SelectedPackageAssets: string list
      RuntimeFrameworks: (string * string) list
      Error: string option }

type private Evidence =
    { SchemaVersion: int
      Succeeded: bool
      SdkVersion: string
      RuntimeIdentifier: string
      PackageFeed: string
      CorePackageVersion: string
      AspNetCorePackageVersion: string
      Scenarios: ScenarioResult list }

let private jsonOptions = JsonSerializerOptions(WriteIndented = true)

let private jstr (value: string) : JsonValue = JsonValue.Create value

let private jint (value: int) : JsonValue = JsonValue.Create value

let private jbool (value: bool) : JsonValue = JsonValue.Create value

/// F# cannot pass a literal null to the JsonObject setter; an uninitialized node
/// serializes as JSON null.
let private jsonNull: JsonNode = Unchecked.defaultof<JsonNode>

let private stringArray (values: string list) : JsonArray =
    let array = JsonArray()

    for value in values do
        array.Add(jstr value)

    array

let private runtimeFrameworksArray (values: (string * string) list) : JsonArray =
    let array = JsonArray()

    for name, version in values do
        let item = JsonObject()
        item.["Name"] <- jstr name
        item.["Version"] <- jstr version
        array.Add(item)

    array

let private optionalString (value: string option) : JsonNode =
    match value with
    | Some text -> jstr text :> JsonNode
    | None -> jsonNull

let private scenarioJson (result: ScenarioResult) : JsonNode =
    let node = JsonObject()
    node.["Scenario"] <- jstr result.Scenario
    node.["Outcome"] <- jstr result.Outcome
    node.["StartedAtUtc"] <- jstr result.StartedAtUtc
    node.["FinishedAtUtc"] <- jstr result.FinishedAtUtc
    node.["TargetFramework"] <- jstr result.TargetFramework
    node.["RuntimeIdentifier"] <- jstr result.RuntimeIdentifier
    node.["PackageFeed"] <- jstr result.PackageFeed
    node.["Project"] <- jstr result.Project
    node.["PublishProperties"] <- stringArray result.PublishProperties
    node.["CorePackageVersion"] <- jstr result.CorePackageVersion
    node.["AspNetCorePackageVersion"] <- jstr result.AspNetCorePackageVersion
    node.["SelectedPackageAssets"] <- stringArray result.SelectedPackageAssets
    node.["RuntimeFrameworks"] <- runtimeFrameworksArray result.RuntimeFrameworks
    node.["Error"] <- optionalString result.Error
    node :> JsonNode

let private evidenceJson (evidence: Evidence) : JsonObject =
    let node = JsonObject()
    node.["SchemaVersion"] <- jint evidence.SchemaVersion
    node.["Succeeded"] <- jbool evidence.Succeeded
    node.["SdkVersion"] <- jstr evidence.SdkVersion
    node.["RuntimeIdentifier"] <- jstr evidence.RuntimeIdentifier
    node.["PackageFeed"] <- jstr evidence.PackageFeed
    node.["CorePackageVersion"] <- jstr evidence.CorePackageVersion
    node.["AspNetCorePackageVersion"] <- jstr evidence.AspNetCorePackageVersion
    let scenarios = JsonArray()

    for result in evidence.Scenarios do
        scenarios.Add(scenarioJson result)

    node.["Scenarios"] <- scenarios
    node

// ---- scenario execution ----

/// Read an optional string property without treating a missing or non-string value as an error.
let private jsonStringOrEmpty (element: JsonElement) (name: string) : string =
    let mutable value = Unchecked.defaultof<JsonElement>

    if element.ValueKind = JsonValueKind.Object && element.TryGetProperty(name, &value)
       && value.ValueKind = JsonValueKind.String then
        match value.GetString() with
        | null -> ""
        | text -> text
    else
        ""

/// The SDK the repository pins in global.json, or the empty string when it is not declared.
let private globalJsonSdkVersion (repositoryRoot: string) : string =
    let path = Path.Combine(repositoryRoot, "global.json")

    if not (File.Exists path) then
        ""
    else
        try
            use document = JsonDocument.Parse(File.ReadAllText path)
            let mutable sdk = Unchecked.defaultof<JsonElement>

            if document.RootElement.TryGetProperty("sdk", &sdk) then
                jsonStringOrEmpty sdk "version"
            else
                ""
        with _ ->
            ""

/// The runtime frameworks a self-contained publish records in its runtimeconfig.json.
let private readRuntimeFrameworks (publishDirectory: string) (assemblyName: string) : (string * string) list =
    let path = Path.Combine(publishDirectory, assemblyName + ".runtimeconfig.json")

    if not (File.Exists path) then
        []
    else
        try
            use document = JsonDocument.Parse(File.ReadAllText path)
            let options = document.RootElement.GetProperty("runtimeOptions")

            [ "frameworks"; "includedFrameworks" ]
            |> List.choose (fun name ->
                let mutable value = Unchecked.defaultof<JsonElement>

                if options.TryGetProperty(name, &value) && value.ValueKind = JsonValueKind.Array then
                    Some value
                else
                    None)
            |> List.collect (fun array ->
                [ for item in array.EnumerateArray() ->
                      jsonStringOrEmpty item "name", jsonStringOrEmpty item "version" ])
        with _ ->
            []

type private Context =
    { RepositoryRoot: string
      ArtifactsDirectory: string
      OutputRoot: string
      NuGetConfigPath: string
      RuntimeIdentifier: string
      PackageFeed: string
      SdkVersion: string
      CoreVersion: string
      AspNetCoreVersion: string
      CorePackageAssets: string list
      AspNetCorePackageAssets: string list
      Definitions: Map<string, ScenarioDefinition>
      Environment: (string * string) list }

let private invokeDotNet (context: Context) (stdout: TextWriter) (arguments: string list) : unit =
    stdout.WriteLine("+ dotnet " + String.concat " " arguments)
    let exitCode = processRunner context.RepositoryRoot context.Environment ("dotnet" :: arguments)

    if exitCode <> 0 then
        failwithf "dotnet exited with code %d." exitCode

let private invokePublishedApplication
    (context: Context)
    (stdout: TextWriter)
    (publishDirectory: string)
    (assemblyName: string)
    : unit =
    let suffix = if OperatingSystem.IsWindows() then ".exe" else ""
    let application = Path.Combine(publishDirectory, assemblyName + suffix)

    if not (File.Exists application) then
        failwithf "Published application was not found: %s" application

    stdout.WriteLine("+ " + application)
    let exitCode = processRunner context.RepositoryRoot context.Environment [ application ]

    if exitCode <> 0 then
        failwithf "Published application exited with code %d." exitCode

let private runScenario
    (context: Context)
    (stdout: TextWriter)
    (stderr: TextWriter)
    (name: string)
    : ScenarioResult =
    let definition = context.Definitions.[name]
    let scenarioRoot = Path.Combine(context.OutputRoot, name)
    let publishDirectory = Path.Combine(scenarioRoot, "publish")
    let intermediateDirectory = Path.Combine(scenarioRoot, "obj")
    let binaryDirectory = Path.Combine(scenarioRoot, "bin")
    let startedAt = DateTimeOffset.UtcNow
    let mutable runtimeFrameworks : (string * string) list = []

    let build (outcome: string) (error: string option) =
        { Scenario = name
          Outcome = outcome
          StartedAtUtc = startedAt.ToString("O", CultureInfo.InvariantCulture)
          FinishedAtUtc = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
          TargetFramework = definition.TargetFramework
          RuntimeIdentifier = context.RuntimeIdentifier
          PackageFeed = context.PackageFeed
          Project = definition.Project
          PublishProperties = definition.PublishProperties
          CorePackageVersion = context.CoreVersion
          AspNetCorePackageVersion = context.AspNetCoreVersion
          SelectedPackageAssets =
            if definition.AssemblyName = "FunnySharp.Compatibility.Core" then
                context.CorePackageAssets
            else
                context.AspNetCorePackageAssets
          RuntimeFrameworks = runtimeFrameworks
          Error = error }

    try
        resetDirectory context.ArtifactsDirectory scenarioRoot
        let separator = string Path.DirectorySeparatorChar

        let commonProperties =
            [ sprintf "-p:TargetFramework=%s" definition.TargetFramework
              sprintf "-p:FunnySharpPackageVersion=%s" context.CoreVersion
              sprintf "-p:FunnySharpAspNetCorePackageVersion=%s" context.AspNetCoreVersion
              "-p:SelfContained=true"
              sprintf "-p:BaseIntermediateOutputPath=%s%s" intermediateDirectory separator
              sprintf "-p:BaseOutputPath=%s%s" binaryDirectory separator ]

        invokeDotNet
            context
            stdout
            ([ "restore"
               definition.Project
               "--configfile"
               context.NuGetConfigPath
               "--no-cache"
               "--runtime"
               context.RuntimeIdentifier ]
             @ commonProperties
             @ definition.PublishProperties)

        invokeDotNet
            context
            stdout
            ([ "publish"
               definition.Project
               "--configuration"
               "Release"
               "--runtime"
               context.RuntimeIdentifier
               "--self-contained"
               "true"
               "--no-restore"
               "--output"
               publishDirectory ]
             @ commonProperties
             @ definition.PublishProperties)

        invokePublishedApplication context stdout publishDirectory definition.AssemblyName

        runtimeFrameworks <- readRuntimeFrameworks publishDirectory definition.AssemblyName

        build "Passed" None
    with ex ->
        stderr.WriteLine(sprintf "%s failed: %s" name ex.Message)
        build "Failed" (Some ex.Message)

// ---- command line ----

type private CliOptions =
    { RepositoryRoot: string option
      PackageDirectory: string option
      OutputDirectory: string option
      RuntimeIdentifier: string option
      PackageFeed: string option
      Scenario: string list
      Help: bool }

let private usageLine =
    "usage: Run-Compatibility.ps1 -PackageDirectory <dir> -OutputDirectory <dir> [-RepositoryRoot <dir>] [-RuntimeIdentifier <rid>] [-PackageFeed <feed>] [-Scenario <list>]"

let private helpText =
    String.concat
        "\n"
        [ usageLine
          ""
          "Restore, publish and run the compatibility consumers against the packed FunnySharp packages."
          ""
          "options:"
          "  -h, -Help                      show this help message and exit"
          "  -RepositoryRoot <dir>          repository root (default: resolved from the current directory)."
          "  -PackageDirectory <dir>        directory holding the packed FunnySharp*.nupkg files (required)."
          "  -OutputDirectory <dir>         proper subdirectory of <RepositoryRoot>/artifacts (required)."
          sprintf "  -RuntimeIdentifier <rid>       runnable host RID (default: %s)." hostRuntimeIdentifier
          sprintf "  -PackageFeed <feed>            upstream NuGet feed (default: %s)." defaultPackageFeed
          "  -Scenario <list>               comma-separated scenario names (default: the four trim/AOT scenarios)." ]

let private knownFlags =
    [ "repositoryroot"; "packagedirectory"; "outputdirectory"; "runtimeidentifier"; "packagefeed"; "scenario" ]

let private setFlag (options: CliOptions) (key: string) (value: string) : CliOptions =
    match key with
    | "repositoryroot" -> { options with RepositoryRoot = Some value }
    | "packagedirectory" -> { options with PackageDirectory = Some value }
    | "outputdirectory" -> { options with OutputDirectory = Some value }
    | "runtimeidentifier" -> { options with RuntimeIdentifier = Some value }
    | "packagefeed" -> { options with PackageFeed = Some value }
    | "scenario" -> { options with Scenario = options.Scenario @ [ value ] }
    | _ -> options

let private parseArgs (argv: string list) : Result<CliOptions, string> =
    let empty =
        { RepositoryRoot = None
          PackageDirectory = None
          OutputDirectory = None
          RuntimeIdentifier = None
          PackageFeed = None
          Scenario = []
          Help = false }

    let rec loop (options: CliOptions) (remaining: string list) =
        match remaining with
        | [] -> Ok options
        | argument :: rest ->
            let separator =
                let equals = argument.IndexOf '='
                let colon = argument.IndexOf ':'
                if equals > 0 then equals elif colon > 0 then colon else -1

            let name, inlineValue =
                if separator > 0 then
                    argument.Substring(0, separator), Some(argument.Substring(separator + 1))
                else
                    argument, None

            let key = name.TrimStart('-').ToLowerInvariant()

            if key = "h" || key = "help" then
                Ok { options with Help = true }
            elif List.contains key knownFlags then
                match inlineValue with
                | Some value -> loop (setFlag options key value) rest
                | None ->
                    match rest with
                    | value :: tail -> loop (setFlag options key value) tail
                    | [] -> Error(sprintf "argument %s: expected one argument" name)
            else
                Error("unrecognized arguments: " + argument)

    loop empty argv

let private splitScenarioArguments (values: string list) : string list =
    values
    |> List.collect (fun value -> value.Split(',') |> List.ofArray)
    |> List.filter (fun name -> not (String.IsNullOrWhiteSpace name))

let private validateScenarios (names: string list) : Result<string list, HarnessError> =
    match names |> List.tryFind (fun name -> not (knownScenarioNames.Contains name)) with
    | Some unknown -> usageError (sprintf "Unknown compatibility scenario '%s'." unknown)
    | None -> Ok names

let private resolveRepositoryRoot
    (startDirectory: string)
    (configured: string option)
    : Result<string, HarnessError> =
    match configured with
    | Some root ->
        let full = Path.GetFullPath root

        if Directory.Exists full then
            Ok(resolveDirectoryPath full)
        else
            usageError (sprintf "Repository root was not found: '%s'." full)
    | None ->
        match Repo.tryFindRootFrom startDirectory with
        | Some root -> Ok root
        | None ->
            match Repo.tryFindRootFrom AppContext.BaseDirectory with
            | Some root -> Ok root
            | None -> usageError (sprintf "could not locate '%s' above '%s'" Repo.SolutionFileName startDirectory)

let private prepare
    (startDirectory: string)
    (repositoryRootOption: string option)
    (packageDirectoryRaw: string)
    (outputDirectoryRaw: string)
    (runtimeIdentifier: string)
    (packageFeed: string)
    : Result<Context, HarnessError> =
    rail {
        let! repositoryRoot = resolveRepositoryRoot startDirectory repositoryRootOption
        let artifactsDirectory = Path.GetFullPath(Path.Combine(repositoryRoot, "artifacts"))
        Directory.CreateDirectory artifactsDirectory |> ignore
        let artifactsDirectory = resolveDirectoryPath artifactsDirectory
        let outputFull = Path.GetFullPath outputDirectoryRaw
        do! assertSafeArtifactsSubdirectory artifactsDirectory outputFull
        let packageDirectory = Path.GetFullPath packageDirectoryRaw

        do!
            (if Directory.Exists packageDirectory then
                 Ok()
             else
                 usageFail (sprintf "Package directory does not exist: %s" packageDirectory))

        do!
            (if hostOsSupported then
                 Ok()
             else
                 usageFail "Unsupported host operating system.")

        do!
            (if String.Equals(runtimeIdentifier, hostRuntimeIdentifier, StringComparison.OrdinalIgnoreCase) then
                 Ok()
             else
                 usageFail (
                     sprintf
                         "RuntimeIdentifier '%s' must match the runnable host RID '%s'."
                         runtimeIdentifier
                         hostRuntimeIdentifier
                 ))

        let! coreVersion = packageVersion packageDirectory "FunnySharp"
        let! aspNetCoreVersion = packageVersion packageDirectory "FunnySharp.AspNetCore"
        do!
            if coreVersion <> aspNetCoreVersion then
                usageFail "Package versions must match."
            else
                validatePackageInventory packageDirectory coreVersion

        let corePackagePath = Path.Combine(packageDirectory, sprintf "FunnySharp.%s.nupkg" coreVersion)

        let aspNetCorePackagePath =
            Path.Combine(packageDirectory, sprintf "FunnySharp.AspNetCore.%s.nupkg" aspNetCoreVersion)


        let! coreAssets =
            try
                Ok(validatePackage corePackagePath "FunnySharp" coreVersion coreVersion)
            with ex ->
                usageError ex.Message

        let! aspNetCoreAssets =
            try
                Ok(validatePackage aspNetCorePackagePath "FunnySharp.AspNetCore" aspNetCoreVersion coreVersion)
            with ex ->
                usageError ex.Message

        Directory.CreateDirectory outputFull |> ignore
        let outputRoot = resolveDirectoryPath outputFull
        let nugetConfigPath = Path.Combine(outputRoot, "NuGet.Config")
        File.WriteAllText(nugetConfigPath, nuGetConfigText packageDirectory packageFeed, UTF8Encoding(false))
        let nugetPackagesDirectory = isolatedNuGetPackagesDirectory artifactsDirectory outputRoot
        resetDirectory artifactsDirectory nugetPackagesDirectory

        return
            { RepositoryRoot = repositoryRoot
              ArtifactsDirectory = artifactsDirectory
              OutputRoot = outputRoot
              NuGetConfigPath = nugetConfigPath
              RuntimeIdentifier = runtimeIdentifier
              PackageFeed = packageFeed
              SdkVersion = globalJsonSdkVersion repositoryRoot
              CoreVersion = coreVersion
              AspNetCoreVersion = aspNetCoreVersion
              CorePackageAssets = coreAssets
              AspNetCorePackageAssets = aspNetCoreAssets
              Definitions = scenarioDefinitions repositoryRoot
              Environment = [ ("NUGET_PACKAGES", nugetPackagesDirectory) ] }
    }

let private execute (context: Context) (stdout: TextWriter) (stderr: TextWriter) (names: string list) : int =
    let results =
        names |> List.map (fun name -> runScenario context stdout stderr name)

    let succeeded = not (results |> List.exists (fun result -> result.Outcome = "Failed"))

    let evidence =
        { SchemaVersion = 1
          Succeeded = succeeded
          SdkVersion = context.SdkVersion
          RuntimeIdentifier = context.RuntimeIdentifier
          PackageFeed = context.PackageFeed
          CorePackageVersion = context.CoreVersion
          AspNetCorePackageVersion = context.AspNetCoreVersion
          Scenarios = results }

    let resultsPath = Path.Combine(context.OutputRoot, "compatibility-results.json")

    File.WriteAllText(resultsPath, (evidenceJson evidence).ToJsonString(jsonOptions) + "\n", UTF8Encoding(false))
    stdout.WriteLine("results: " + resultsPath)

    for result in results do
        match result.Outcome with
        | "Passed" -> stdout.WriteLine(sprintf "PASS %s" result.Scenario)
        | _ -> stdout.WriteLine(sprintf "FAIL %s: %s" result.Scenario (defaultArg result.Error ""))

    let passed = results |> List.filter (fun result -> result.Outcome = "Passed") |> List.length

    stdout.WriteLine(
        sprintf
            "compatibility: %s (%d/%d scenarios passed)"
            (if succeeded then "PASS" else "FAIL")
            passed
            results.Length
    )

    if succeeded then 0 else 1

let private report (stderr: TextWriter) (err: HarnessError) : int =
    stderr.WriteLine("error: " + err.Message)
    err.ExitCode

/// Run the compatibility suite writing to the supplied writers. `startDirectory`
/// seeds the default repository-root lookup so tests can inject a fixture root.
/// Returns 0 pass, 1 a scenario verification failure, 2 usage/environment failure.
let mainWith (stdout: TextWriter) (stderr: TextWriter) (startDirectory: string) (argv: string list) : int =
    match parseArgs argv with
    | Error message ->
        stderr.WriteLine usageLine
        stderr.WriteLine("Run-Compatibility.ps1: error: " + message)
        2
    | Ok options when options.Help ->
        stdout.WriteLine helpText
        0
    | Ok options ->
        match options.PackageDirectory, options.OutputDirectory with
        | Some packageDirectory, Some outputDirectory when
            not (String.IsNullOrWhiteSpace packageDirectory)
            && not (String.IsNullOrWhiteSpace outputDirectory)
            ->
            let scenarioNames =
                match splitScenarioArguments options.Scenario with
                | [] -> defaultScenarios
                | names -> names

            match validateScenarios scenarioNames with
            | Error err -> report stderr err
            | Ok scenarioNames ->
                let runtimeIdentifier = Option.defaultValue hostRuntimeIdentifier options.RuntimeIdentifier
                let packageFeed = Option.defaultValue defaultPackageFeed options.PackageFeed

                match
                    prepare
                        startDirectory
                        options.RepositoryRoot
                        packageDirectory
                        outputDirectory
                        runtimeIdentifier
                        packageFeed
                with
                | Error err -> report stderr err
                | Ok context -> execute context stdout stderr scenarioNames
        | _ ->
            stderr.WriteLine usageLine

            stderr.WriteLine
                "Run-Compatibility.ps1: error: the -PackageDirectory and -OutputDirectory parameters are required."

            2

/// Entry point mirroring Run-Compatibility.ps1.
let main (argv: string array) : int =
    mainWith Console.Out Console.Error Environment.CurrentDirectory (List.ofArray argv)
