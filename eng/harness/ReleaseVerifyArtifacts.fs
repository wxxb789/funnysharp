module FunnySharp.Harness.ReleaseVerifyArtifacts

// Behaviour-identical F# port of the artifact-facing half of
// eng/Verify-Release.ps1 (lane C): environment capture, the reflected public-API
// inventory, XML documentation coverage, NuGet package inspection/layout, and the
// compatibility evidence contract. The two verifier steps that PowerShell ran as
// child processes (documentation snippets, compatibility script) run the ported
// F# verifiers in-process through injectable runners, so no pwsh is invoked.

open System
open System.Collections.Generic
open System.Globalization
open System.IO
open System.IO.Compression
open System.Reflection
open System.Runtime.InteropServices
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open System.Text.RegularExpressions
open System.Xml.Linq
open FunnySharp.Harness.Output
open FunnySharp.Harness.Proc
open FunnySharp.Harness.ReleaseVerifySource

/// A value the runtime may leave null even though the API is annotated non-null
/// (NullabilityInfo.ElementType, NullabilityInfoContext.Create results).
let private someOrNull (value: 'T | null) : 'T option =
    match value with
    | null -> None
    | present -> Some present

let private objText (value: obj | null) : string =
    if isNull value then "" else string value

// ---------------------------------------------------------------------------
// Environment capture
// ---------------------------------------------------------------------------

let private externalValue (root: string) (fileName: string) (arguments: string list) : string =
    let commandText =
        if arguments.IsEmpty then fileName else fileName + " " + String.concat " " arguments

    let result =
        Proc.runCaptureIn (Some root) fileName arguments |> Async.RunSynchronously

    if result.ExitCode = 0 then
        result.Stdout.Trim()
    else
        let message = sprintf "%s exited with code %d." commandText result.ExitCode
        let standardError = result.Stderr.Trim()

        if String.IsNullOrWhiteSpace standardError then
            failNow message
        else
            failNow (message + " " + standardError)

/// Get-EnvironmentEvidence.
let getEnvironmentEvidence (root: string) : JsonObject =
    let dotnetVersion = externalValue root "dotnet" [ "--version" ]
    let dotnetInfo = externalValue root "dotnet" [ "--info" ]
    let commit = externalValue root "git" [ "rev-parse"; "HEAD" ]
    let branch = externalValue root "git" [ "branch"; "--show-current" ]
    let worktree = externalValue root "git" [ "rev-parse"; "--show-toplevel" ]
    let status = externalValue root "git" [ "status"; "--porcelain=v1" ]

    let node = JsonObject()
    node.["collectedAtUtc"] <- jstr (DateTime.UtcNow.ToString("O"))
    node.["repositoryRoot"] <- jstr root
    node.["worktree"] <- jstr worktree
    node.["commit"] <- jstr commit
    node.["branch"] <- jstr branch
    node.["worktreeStatus"] <- jstr status
    node.["operatingSystem"] <- jstr RuntimeInformation.OSDescription
    node.["operatingSystemArchitecture"] <- jstr (RuntimeInformation.OSArchitecture.ToString())
    node.["processArchitecture"] <- jstr (RuntimeInformation.ProcessArchitecture.ToString())
    node.["runtimeIdentifier"] <- jstr RuntimeInformation.RuntimeIdentifier
    node.["frameworkDescription"] <- jstr RuntimeInformation.FrameworkDescription
    node.["powershellVersion"] <- jstr ""
    node.["dotnetVersion"] <- jstr dotnetVersion
    node.["dotnetInfo"] <- jstr dotnetInfo
    node

/// Get-SharedFrameworkDirectory: the highest 10.x shared framework of a name.
let getSharedFrameworkDirectory (frameworkName: string) (root: string) : string =
    let result =
        Proc.runCaptureIn (Some root) "dotnet" [ "--list-runtimes" ] |> Async.RunSynchronously

    if result.ExitCode <> 0 then
        failNow (sprintf "dotnet --list-runtimes failed with exit code %d." result.ExitCode)

    let pattern = Regex("^" + Regex.Escape frameworkName + @" (?<version>10\.[^ ]+) \[(?<path>.+)\]$")

    let candidates =
        result.Stdout.Split('\n')
        |> Array.choose (fun rawLine ->
            let line = rawLine.TrimEnd('\r')
            let matched = pattern.Match line

            if matched.Success then
                let version = matched.Groups.["version"].Value
                let parsed = Version.Parse((version.Split('-').[0]))
                Some(struct (version, parsed, matched.Groups.["path"].Value))
            else
                None)
        |> Array.sortByDescending (fun (struct (_, parsed, _)) -> parsed)

    if candidates.Length = 0 then
        failNow (sprintf "No .NET 10 '%s' shared framework was found." frameworkName)

    let struct (version, _, path) = candidates.[0]
    Path.Combine(path, version)

// ---------------------------------------------------------------------------
// Public API inventory (reflection)
// ---------------------------------------------------------------------------

let private getNullabilityInfo (provider: obj | null) (context: NullabilityInfoContext) : NullabilityInfo option =
    try
        match provider with
        | :? ParameterInfo as value -> someOrNull (context.Create value)
        | :? PropertyInfo as value -> someOrNull (context.Create value)
        | :? FieldInfo as value -> someOrNull (context.Create value)
        | :? EventInfo as value -> someOrNull (context.Create value)
        | _ -> None
    with _ ->
        None

/// Format-ApiType with nullable annotations.
let rec formatApiType (apiType: Type) (nullability: NullabilityInfo option) : string =
    let suffix =
        match nullability with
        | Some info when info.ReadState = NullabilityState.Nullable -> "?"
        | _ -> ""

    let elementNullability =
        match nullability with
        | Some info -> someOrNull info.ElementType
        | None -> None

    if apiType.IsByRef then
        match apiType.GetElementType() with
        | null -> apiType.Name
        | element -> formatApiType element elementNullability + "&"
    elif apiType.IsPointer then
        match apiType.GetElementType() with
        | null -> apiType.Name
        | element -> formatApiType element elementNullability + "*"
    elif apiType.IsArray then
        let rank = apiType.GetArrayRank()
        let arraySuffix = if rank = 1 then "[]" else "[" + String(',', rank - 1) + "]"

        match apiType.GetElementType() with
        | null -> apiType.Name
        | element -> formatApiType element elementNullability + arraySuffix + suffix
    elif apiType.IsGenericParameter then
        apiType.Name + suffix
    elif apiType.IsGenericType then
        let genericName =
            Regex.Replace(
                (Option.ofObj (apiType.GetGenericTypeDefinition().FullName) |> Option.defaultValue apiType.Name),
                "`[0-9]+$",
                ""
            )

        let typeArguments = apiType.GetGenericArguments()

        let nullabilityArguments =
            match nullability with
            | Some info -> info.GenericTypeArguments
            | None -> [||]

        let arguments =
            [ for index in 0 .. typeArguments.Length - 1 ->
                let argumentNullability =
                    if index < nullabilityArguments.Length then
                        someOrNull nullabilityArguments.[index]
                    else
                        None

                formatApiType typeArguments.[index] argumentNullability ]

        genericName + "<" + String.concat ", " arguments + ">" + suffix
    else
        (Option.ofObj apiType.FullName |> Option.defaultValue apiType.Name) + suffix

let private formatApiParameter (parameter: ParameterInfo) (context: NullabilityInfoContext) : string =
    let modifier =
        if parameter.IsOut then "out "
        elif parameter.ParameterType.IsByRef then "ref "
        else ""

    let typeName = formatApiType parameter.ParameterType (getNullabilityInfo (box parameter) context)
    modifier + typeName + " " + parameter.Name

let private sortIgnoreCase (values: string array) : string array =
    Array.sortWith (fun left right -> StringComparer.OrdinalIgnoreCase.Compare(left, right)) values

/// Get-PublicApiInventory.
let getPublicApiInventory (assemblyPaths: string list) (root: string) : JsonArray =
    let context = NullabilityInfoContext()

    let searchDirectories =
        (assemblyPaths
         |> List.map (fun path ->
             match Path.GetDirectoryName path with
             | null -> ""
             | directory -> directory))
        @ [ getSharedFrameworkDirectory "Microsoft.AspNetCore.App" root ]
        |> List.distinct

    let resolver =
        ResolveEventHandler(fun _ eventArgs ->
            let requested = eventArgs.Name

            if String.IsNullOrEmpty requested then
                Unchecked.defaultof<Assembly>
            else
                let simpleName =
                    match AssemblyName(requested).Name with
                    | null -> ""
                    | name -> name

                let rec tryDirectories directories =
                    match directories with
                    | [] -> Unchecked.defaultof<Assembly>
                    | directory :: rest ->
                        let candidate = Path.Combine(directory, simpleName + ".dll")

                        if simpleName <> "" && File.Exists candidate then
                            Assembly.LoadFrom candidate
                        else
                            tryDirectories rest

                tryDirectories searchDirectories)

    AppDomain.CurrentDomain.add_AssemblyResolve resolver

    try
        let inventories = JsonArray()

        for assemblyPath in
            List.sortWith (fun left right -> StringComparer.OrdinalIgnoreCase.Compare(left, right)) assemblyPaths do
            let assembly = Assembly.LoadFrom assemblyPath

            let types =
                assembly.GetExportedTypes()
                |> Array.sortWith (fun left right ->
                    StringComparer.OrdinalIgnoreCase.Compare(
                        (Option.ofObj left.FullName |> Option.defaultValue ""),
                        (Option.ofObj right.FullName |> Option.defaultValue "")
                    ))

            let metadata =
                assembly.GetCustomAttributesData()
                |> Seq.filter (fun attribute -> attribute.AttributeType = typeof<AssemblyMetadataAttribute>)
                |> Seq.map (fun attribute ->
                    objText attribute.ConstructorArguments.[0].Value,
                    objText attribute.ConstructorArguments.[1].Value)
                |> Seq.toList

            let isTrimmable =
                metadata |> List.exists (fun (key, value) -> key = "IsTrimmable" && value = "True")

            let typeInventories = JsonArray()

            let flags =
                BindingFlags.Public ||| BindingFlags.Instance ||| BindingFlags.Static ||| BindingFlags.DeclaredOnly

            for apiType in types do
                let constructors =
                    apiType.GetConstructors flags
                    |> Array.map (fun constructor ->
                        let parameters =
                            constructor.GetParameters()
                            |> Array.map (fun parameter -> formatApiParameter parameter context)
                            |> String.concat ", "

                        "ctor(" + parameters + ")")
                    |> sortIgnoreCase
                    |> jsonStringArray

                let methods =
                    apiType.GetMethods flags
                    |> Array.filter (fun method ->
                        not method.IsSpecialName || method.Name.StartsWith("op_", StringComparison.Ordinal))
                    |> Array.map (fun method ->
                        let parameters =
                            method.GetParameters()
                            |> Array.map (fun parameter -> formatApiParameter parameter context)
                            |> String.concat ", "

                        let genericParameters = method.GetGenericArguments() |> Array.map (fun argument -> argument.Name)

                        let genericSuffix =
                            if genericParameters.Length = 0 then
                                ""
                            else
                                "<" + String.concat ", " genericParameters + ">"

                        let returnType =
                            formatApiType method.ReturnType (getNullabilityInfo (box method.ReturnParameter) context)

                        let staticPrefix = if method.IsStatic then "static " else ""
                        staticPrefix + returnType + " " + method.Name + genericSuffix + "(" + parameters + ")")
                    |> sortIgnoreCase
                    |> jsonStringArray

                let properties =
                    apiType.GetProperties flags
                    |> Array.filter (fun property -> not (isNull property.GetMethod) || not (isNull property.SetMethod))
                    |> Array.map (fun property ->
                        let indexParameters =
                            property.GetIndexParameters()
                            |> Array.map (fun parameter -> formatApiParameter parameter context)
                            |> String.concat ", "

                        let name =
                            if indexParameters.Length = 0 then
                                property.Name
                            else
                                property.Name + "[" + indexParameters + "]"

                        formatApiType property.PropertyType (getNullabilityInfo (box property) context) + " " + name)
                    |> sortIgnoreCase
                    |> jsonStringArray

                let fields =
                    apiType.GetFields flags
                    |> Array.map (fun field ->
                        let staticPrefix = if field.IsStatic then "static " else ""

                        // A literal's value is part of the surface: a caller that persists or
                        // serializes the constant observes a change the name and type alone hide.
                        let constantSuffix =
                            if field.IsLiteral then
                                " = " + Convert.ToString(field.GetRawConstantValue(), CultureInfo.InvariantCulture)
                            else
                                ""

                        staticPrefix
                        + formatApiType field.FieldType (getNullabilityInfo (box field) context)
                        + " "
                        + field.Name
                        + constantSuffix)
                    |> sortIgnoreCase
                    |> jsonStringArray

                let events =
                    apiType.GetEvents flags
                    |> Array.map (fun eventInfo ->
                        match eventInfo.EventHandlerType with
                        | null -> ""
                        | handlerType ->
                            formatApiType handlerType (getNullabilityInfo (box eventInfo) context)
                            + " "
                            + eventInfo.Name)
                    |> sortIgnoreCase
                    |> jsonStringArray

                let kind =
                    if apiType.IsInterface then "interface"
                    elif apiType.IsEnum then "enum"
                    elif apiType.IsValueType then "struct"
                    elif (match apiType.BaseType with
                          | null -> false
                          | baseType -> baseType = typeof<MulticastDelegate>) then
                        "delegate"
                    else "class"

                let typeNode = JsonObject()
                typeNode.["name"] <- jstr (formatApiType apiType None)
                typeNode.["kind"] <- jstr kind
                typeNode.["constructors"] <- constructors
                typeNode.["methods"] <- methods
                typeNode.["properties"] <- properties
                typeNode.["fields"] <- fields
                typeNode.["events"] <- events
                typeInventories.Add typeNode

            let assemblyNode = JsonObject()
            assemblyNode.["path"] <- jstr assemblyPath
            assemblyNode.["sha256"] <- jstr (sha256File assemblyPath)
            assemblyNode.["identity"] <- jstr (Option.ofObj assembly.FullName |> Option.defaultValue "")
            assemblyNode.["isTrimmable"] <- jbool isTrimmable
            assemblyNode.["types"] <- typeInventories
            inventories.Add assemblyNode

        inventories
    finally
        AppDomain.CurrentDomain.remove_AssemblyResolve resolver

/// An assembly full name with its `Version=` component removed: a patch release
/// rewrites the version while the documented no-surface-change path holds, so the
/// version is not part of the surface the baseline records. Name, culture and
/// public key token stay - those are compatibility-relevant.
let private withoutAssemblyVersion (identity: string) : string =
    identity.Split(", ")
    |> Array.filter (fun part -> not (part.StartsWith("Version=", StringComparison.Ordinal)))
    |> String.concat ", "

/// The `public-api.txt` rendering.
let renderPublicApiText (inventory: JsonArray) : string list =
    let lines = ResizeArray<string>()

    for assembly in inventory do
        match assembly with
        | :? JsonObject as assemblyObject ->
            let identity =
                match assemblyObject.["identity"] with
                | :? JsonValue as v -> v.ToString()
                | _ -> ""

            lines.Add("ASSEMBLY " + withoutAssemblyVersion identity)

            match assemblyObject.["types"] with
            | :? JsonArray as types ->
                for apiType in types do
                    match apiType with
                    | :? JsonObject as typeObject ->
                        let kind =
                            match typeObject.["kind"] with
                            | :? JsonValue as v -> v.ToString().ToUpperInvariant()
                            | _ -> ""

                        let name = match typeObject.["name"] with | :? JsonValue as v -> v.ToString() | _ -> ""
                        lines.Add(kind + " " + name)

                        for memberKind in [ "constructors"; "methods"; "properties"; "fields"; "events" ] do
                            match typeObject.[memberKind] with
                            | :? JsonArray as members ->
                                for memberNode in members do
                                    match memberNode with
                                    | :? JsonValue as v ->
                                        lines.Add("  " + memberKind.TrimEnd('s').ToUpperInvariant() + " " + v.ToString())
                                    | _ -> ()
                            | _ -> ()
                    | _ -> ()
            | _ -> ()
        | _ -> ()

    List.ofSeq lines

// ---------------------------------------------------------------------------
// XML documentation inventory
// ---------------------------------------------------------------------------

/// Get-XmlDocumentationInventory.
let getXmlDocumentationInventory (paths: string list) : JsonArray =
    let inventories = JsonArray()

    for path in List.sortWith (fun left right -> StringComparer.OrdinalIgnoreCase.Compare(left, right)) paths do
        if not (File.Exists path) then
            failNow (sprintf "XML documentation file was not found: '%s'." path)

        let document = XDocument.Parse(File.ReadAllText path)

        let members =
            match document.Root with
            | null -> []
            | root ->
                root.Descendants()
                |> Seq.filter (fun element -> element.Name.LocalName = "member")
                |> Seq.toList

        let missing =
            members
            |> List.filter (fun memberElement ->
                not (memberElement.Elements() |> Seq.exists (fun child -> child.Name.LocalName = "summary"))
                && not (memberElement.Elements() |> Seq.exists (fun child -> child.Name.LocalName = "inheritdoc")))
            |> List.map (fun memberElement ->
                match memberElement.Attribute(XName.Get "name") with
                | null -> ""
                | attribute -> attribute.Value)

        if not missing.IsEmpty then
            failNow (
                sprintf
                    "XML documentation '%s' has %d member(s) without summary or inheritdoc: %s."
                    path
                    missing.Length
                    (String.concat ", " missing)
            )

        let node = JsonObject()
        node.["path"] <- jstr path
        node.["sha256"] <- jstr (sha256File path)
        node.["members"] <- jint members.Length
        node.["missingSummaryOrInheritdoc"] <- jint 0
        inventories.Add node

    inventories

// ---------------------------------------------------------------------------
// NuGet package inspection
// ---------------------------------------------------------------------------

let private elementByName (name: string) (parent: XElement) : XElement option =
    parent.Elements() |> Seq.tryFind (fun element -> element.Name.LocalName = name)

let private elementsByName (name: string) (parent: XElement) : XElement list =
    parent.Elements() |> Seq.filter (fun element -> element.Name.LocalName = name) |> Seq.toList

let private attributeValue (name: string) (element: XElement) : string =
    match element.Attribute(XName.Get name) with
    | null -> ""
    | attribute -> attribute.Value

let private normalizedTargetFramework (targetFramework: string) : string =
    if targetFramework = ".NETCoreApp10.0" then "net10.0" else targetFramework

/// Get-PackageInspection.
let getPackageInspection (packagePath: string) : JsonObject =
    use archive = ZipFile.OpenRead packagePath

    let entries =
        archive.Entries
        |> Seq.sortWith (fun left right -> StringComparer.OrdinalIgnoreCase.Compare(left.FullName, right.FullName))
        |> Seq.toList

    let nuspecEntries = entries |> List.filter (fun entry -> Regex.IsMatch(entry.FullName, "^[^/]+\\.nuspec$"))

    if nuspecEntries.Length <> 1 then
        failNow (
            sprintf "Package '%s' must contain exactly one root nuspec, found %d." packagePath nuspecEntries.Length
        )

    let nuspecText =
        use reader = new StreamReader(nuspecEntries.[0].Open())
        reader.ReadToEnd()

    let nuspec = XDocument.Parse nuspecText
    let packageRoot = nuspec.Root

    let metadata =
        match packageRoot with
        | null -> failNow (sprintf "Package '%s' has no nuspec metadata." packagePath)
        | root ->
            match elementByName "metadata" root with
            | Some value -> value
            | None -> failNow (sprintf "Package '%s' has no nuspec metadata." packagePath)

    let dependencyGroups = JsonArray()

    for group in elementsByName "dependencies" metadata |> List.collect (elementsByName "group") do
        let dependencies = JsonArray()

        for dependency in elementsByName "dependency" group do
            let dependencyNode = JsonObject()
            dependencyNode.["id"] <- jstr (attributeValue "id" dependency)
            dependencyNode.["version"] <- jstr (attributeValue "version" dependency)
            dependencyNode.["exclude"] <- jstr (attributeValue "exclude" dependency)
            dependencies.Add dependencyNode

        let groupNode = JsonObject()

        groupNode.["targetFramework"] <-
            jstr (normalizedTargetFramework (attributeValue "targetFramework" group))

        groupNode.["dependencies"] <- dependencies
        dependencyGroups.Add groupNode

    let frameworkReferenceGroups = JsonArray()

    for group in elementsByName "frameworkReferences" metadata |> List.collect (elementsByName "group") do
        let references =
            elementsByName "frameworkReference" group
            |> List.map (fun reference -> attributeValue "name" reference)
            |> sortedIgnoreCase
            |> jsonStringArray

        let groupNode = JsonObject()

        groupNode.["targetFramework"] <-
            jstr (normalizedTargetFramework (attributeValue "targetFramework" group))

        groupNode.["references"] <- references
        frameworkReferenceGroups.Add groupNode

    let entryInventories = JsonArray()

    for entry in entries do
        use entryStream = entry.Open()
        let entryNode = JsonObject()
        entryNode.["path"] <- jstr entry.FullName
        entryNode.["length"] <- JsonValue.Create entry.Length
        entryNode.["sha256"] <- jstr (Convert.ToHexString(SHA256.HashData entryStream).ToLowerInvariant())

        entryInventories.Add entryNode

    let license = elementByName "license" metadata

    let node = JsonObject()
    node.["path"] <- jstr packagePath
    node.["fileName"] <- jstr (fileNameOf packagePath)
    node.["sha256"] <- jstr (sha256File packagePath)

    node.["id"] <-
        jstr (match elementByName "id" metadata with | Some value -> value.Value | None -> "")

    node.["version"] <-
        jstr (match elementByName "version" metadata with | Some value -> value.Value | None -> "")

    node.["readme"] <-
        jstr (match elementByName "readme" metadata with | Some value -> value.Value | None -> "")

    node.["licenseType"] <-
        match license with
        | Some value -> jstr (attributeValue "type" value) :> JsonNode
        | None -> jsonNull

    node.["licenseExpression"] <-
        match license with
        | Some value -> jstr value.Value :> JsonNode
        | None -> jsonNull

    node.["entries"] <- entryInventories
    node.["dependencyGroups"] <- dependencyGroups
    node.["frameworkReferenceGroups"] <- frameworkReferenceGroups
    node

let private entryPaths (package: JsonElement) : string list =
    propArray "entries" package |> List.map (propText "path")

/// Assert-PackageLayout.
let assertPackageLayout
    (package: JsonElement)
    (expectedId: string)
    (assemblyPath: string)
    (xmlDocumentationPath: string)
    (readmePath: string)
    : unit =
    let packageId = propText "id" package

    if not (equalsOrdinal packageId expectedId) then
        failNow (sprintf "Expected package id '%s', found '%s'." expectedId packageId)

    let expectedFileName = sprintf "%s.%s.nupkg" expectedId (propText "version" package)

    if not (equalsOrdinal (propText "fileName" package) expectedFileName) then
        failNow (
            sprintf
                "Package filename '%s' does not match nuspec identity '%s'."
                (propText "fileName" package)
                expectedFileName
        )

    let paths = entryPaths package

    if not (equalsOrdinal (propText "readme" package) "README.md") || not (List.contains "README.md" paths) then
        failNow (sprintf "Package '%s' must declare and include root README.md." expectedId)

    for path in [ sprintf "lib/net10.0/%s.dll" expectedId; sprintf "lib/net10.0/%s.xml" expectedId ] do
        if not (List.contains path paths) then
            failNow (sprintf "Package '%s' is missing '%s'." expectedId path)

    let expectedHashes =
        [ sprintf "lib/net10.0/%s.dll" expectedId, sha256File assemblyPath
          sprintf "lib/net10.0/%s.xml" expectedId, sha256File xmlDocumentationPath
          "README.md", sha256File readmePath ]

    for entryPath, expectedHash in expectedHashes do
        let actual =
            propArray "entries" package
            |> List.tryPick (fun entry -> if equalsOrdinal (propText "path" entry) entryPath then Some(propText "sha256" entry) else None)

        match actual with
        | Some hash when equalsIgnoreCase hash expectedHash -> ()
        | _ ->
            failNow (
                sprintf "Package '%s' entry '%s' does not match the release candidate file." expectedId entryPath
            )

    if
        not (equalsOrdinal (propText "licenseType" package) "expression")
        || not (equalsOrdinal (propText "licenseExpression" package) "MIT")
    then
        failNow (sprintf "Package '%s' must declare <license type=\"expression\">MIT</license>." expectedId)

/// Assert-CorePackageDependencies.
let assertCorePackageDependencies (package: JsonElement) : unit =
    let groups = propArray "dependencyGroups" package

    if groups.Length <> 1 || not (equalsOrdinal (propText "targetFramework" groups.[0]) "net10.0") then
        failNow "FunnySharp must have exactly one net10.0 dependency group."

    if not (propArray "dependencies" groups.[0]).IsEmpty then
        failNow "FunnySharp net10.0 dependency group must be empty."

    if not (propArray "frameworkReferenceGroups" package).IsEmpty then
        failNow "FunnySharp must not declare framework references in its NuGet package."

/// Assert-AspNetCorePackageDependencies.
let assertAspNetCorePackageDependencies (package: JsonElement) (coreVersion: string) : unit =
    let groups = propArray "dependencyGroups" package

    if groups.Length <> 1 || not (equalsOrdinal (propText "targetFramework" groups.[0]) "net10.0") then
        failNow "FunnySharp.AspNetCore must have exactly one net10.0 dependency group."

    let dependencies = propArray "dependencies" groups.[0]

    if dependencies.Length <> 1 || not (equalsOrdinal (propText "id" dependencies.[0]) "FunnySharp") then
        failNow "FunnySharp.AspNetCore must have FunnySharp as its only package dependency."

    if not (equalsOrdinal (propText "version" dependencies.[0]) coreVersion) then
        failNow (sprintf "FunnySharp.AspNetCore must depend on FunnySharp version '%s'." coreVersion)

    let frameworkGroups = propArray "frameworkReferenceGroups" package

    if frameworkGroups.Length <> 1 || not (equalsOrdinal (propText "targetFramework" frameworkGroups.[0]) "net10.0") then
        failNow "FunnySharp.AspNetCore must have exactly one net10.0 framework reference group."

    let references = propArray "references" frameworkGroups.[0]

    if references.Length <> 1 || not (equalsOrdinal (jsonText references.[0]) "Microsoft.AspNetCore.App") then
        failNow "FunnySharp.AspNetCore must have Microsoft.AspNetCore.App as its only framework reference."

/// Find-PackageArchive.
let findPackageArchive (files: string list) (packageId: string) (extension: string) : string =
    let pattern =
        "^" + Regex.Escape packageId + @"\.(?<version>[0-9][^/]*)\.(?<extension>" + Regex.Escape extension + ")$"

    let matches = files |> List.filter (fun path -> Regex.IsMatch(fileNameOf path, pattern))

    if matches.Length <> 1 then
        failNow (sprintf "Expected exactly one %s archive for '%s', found %d." extension packageId matches.Length)

    matches.Head

// ---------------------------------------------------------------------------
// Compatibility evidence
// ---------------------------------------------------------------------------

let private scenarioValues (evidence: JsonElement) : JsonElement list =
    let scenarios = getPropCI "Scenarios" evidence

    if scenarios.ValueKind = JsonValueKind.Array then
        scenarios.EnumerateArray() |> Seq.toList
    else
        []

/// Assert-CompatibilityEvidence; returns the scenario list.
let assertCompatibilityEvidence
    (evidence: JsonElement)
    (packageInventory: JsonElement)
    (runtimeIdentifier: string)
    : JsonElement list =
    if propIntCI "SchemaVersion" evidence <> Some 1 || propBoolCI "Succeeded" evidence <> Some true then
        failNow "Compatibility evidence must use schema version 1 and report Succeeded=true."

    let expectedScenarios =
        [ "AspNetCoreNativeAot"; "AspNetCoreTrimmed"; "CoreNativeAot"; "CoreTrimmed" ]

    let scenarios = scenarioValues evidence

    let actualScenarios =
        scenarios |> List.map (propTextCI "Scenario") |> distinctIgnoreCase

    let setsMatch =
        scenarios.Length = expectedScenarios.Length
        && actualScenarios.Length = expectedScenarios.Length
        && (sameStringSet actualScenarios expectedScenarios)

    if not setsMatch then
        failNow (
            sprintf "Compatibility evidence must contain exactly these scenarios: %s." (String.concat ", " expectedScenarios)
        )

    let core = getPropCI "core" packageInventory
    let aspNetCore = getPropCI "aspNetCore" packageInventory

    for scenario in scenarios do
        let scenarioName = propTextCI "Scenario" scenario

        if not (equalsIgnoreCase (propTextCI "Outcome" scenario) "Passed") then
            failNow (sprintf "Compatibility scenario '%s' did not pass." scenarioName)

        if not (equalsIgnoreCase (propTextCI "RuntimeIdentifier" scenario) runtimeIdentifier) then
            failNow (
                sprintf
                    "Compatibility scenario '%s' used RID '%s' instead of '%s'."
                    scenarioName
                    (propTextCI "RuntimeIdentifier" scenario)
                    runtimeIdentifier
            )

        if
            not (equalsIgnoreCase (propTextCI "CorePackageVersion" scenario) (propTextCI "version" core))
            || not (equalsIgnoreCase (propTextCI "AspNetCorePackageVersion" scenario) (propTextCI "version" aspNetCore))
        then
            failNow (sprintf "Compatibility scenario '%s' used stale package versions." scenarioName)

        if
            not (equalsIgnoreCase (propTextCI "CorePackageSha256" scenario) (propTextCI "sha256" core))
            || not (equalsIgnoreCase (propTextCI "AspNetCorePackageSha256" scenario) (propTextCI "sha256" aspNetCore))
        then
            failNow (sprintf "Compatibility scenario '%s' used stale package hashes." scenarioName)

    scenarios

// ---------------------------------------------------------------------------
// Verifier steps (in-process, no pwsh)
// ---------------------------------------------------------------------------

type DocumentationRunner = string -> string -> ProcessResult

type CompatibilityInvocation =
    { ScriptPath: string
      Root: string
      OutputDirectory: string
      Packages: string
      RuntimeIdentifier: string
      PackageFeed: string }

type CompatibilityRunner = CompatibilityInvocation -> ProcessResult

let private captureMain (run: TextWriter -> TextWriter -> int) : ProcessResult =
    use stdout = new StringWriter()
    use stderr = new StringWriter()
    let exitCode = run stdout stderr

    { ExitCode = exitCode
      Stdout = stdout.ToString()
      Stderr = stderr.ToString() }

/// Default documentation runner: the ported FunnySharp.Harness.DocsSnippets.
let defaultDocumentationRunner (scriptPath: string) (root: string) : ProcessResult =
    ignore scriptPath

    captureMain (fun stdout stderr -> DocsSnippets.mainWith stdout stderr [ "--repository-root"; root ])

/// Default compatibility runner. Supplied-evidence mode is what the release protocol uses and
/// never calls this runner; script mode is not wired to FunnySharp.Harness.Compatibility yet, so
/// it fails closed with remediation rather than pretending to verify.
let defaultCompatibilityRunner (_invocation: CompatibilityInvocation) : ProcessResult =
    failNow (
        "Compatibility script mode is not wired into this build; "
        + "supply CompatibilityEvidencePath instead of CompatibilityScript."
    )

/// Invoke-DocumentationVerification.
let invokeDocumentationVerification
    (runner: DocumentationRunner)
    (scriptPath: string)
    (root: string)
    (evidenceDirectory: string)
    : JsonObject =
    if not (File.Exists scriptPath) then
        failNow (sprintf "Documentation verifier was not found: '%s'." scriptPath)

    let snippetOutput = Path.Combine(evidenceDirectory, "documentation-snippets")
    Directory.CreateDirectory snippetOutput |> ignore

    let result = runner scriptPath root

    let logPath = Path.Combine(evidenceDirectory, "documentation-snippets.log")
    File.WriteAllText(logPath, result.Stdout + result.Stderr, UTF8Encoding(false))

    if result.ExitCode <> 0 then
        failNow (sprintf "Documentation verifier failed with exit code %d. See '%s'." result.ExitCode logPath)

    let node = JsonObject()
    node.["script"] <- jstr scriptPath
    node.["exitCode"] <- jint result.ExitCode
    node.["log"] <- jstr logPath
    node

/// Invoke-CompatibilityVerification (script mode or supplied-evidence mode).
let invokeCompatibilityVerification
    (runner: CompatibilityRunner)
    (scriptPath: string option)
    (evidencePath: string option)
    (root: string)
    (evidenceDirectory: string)
    (packages: string)
    (runtimeIdentifier: string)
    (packageFeed: string)
    (packageInventory: JsonElement)
    : JsonObject =
    if scriptPath.IsSome && evidencePath.IsSome then
        failNow "Specify either CompatibilityScript or CompatibilityEvidencePath, not both."

    let compatibilityDirectory = Path.Combine(evidenceDirectory, "compatibility")
    Directory.CreateDirectory compatibilityDirectory |> ignore

    match scriptPath with
    | Some script ->
        if not (File.Exists script) then
            failNow (sprintf "Compatibility script was not found: '%s'." script)

        let invocation =
            { ScriptPath = script
              Root = root
              OutputDirectory = compatibilityDirectory
              Packages = packages
              RuntimeIdentifier = runtimeIdentifier
              PackageFeed = packageFeed }

        let result = runner invocation

        let logPath = Path.Combine(compatibilityDirectory, "compatibility.log")
        File.WriteAllText(logPath, result.Stdout + result.Stderr, UTF8Encoding(false))

        if result.ExitCode <> 0 then
            failNow (sprintf "Compatibility script failed with exit code %d. See '%s'." result.ExitCode logPath)

        let resultsPath = Path.Combine(compatibilityDirectory, "compatibility-results.json")

        if not (File.Exists resultsPath) then
            failNow (sprintf "Compatibility script did not produce '%s'." resultsPath)

        use resultsDocument = JsonDocument.Parse(File.ReadAllText resultsPath)

        let scenarios =
            assertCompatibilityEvidence resultsDocument.RootElement packageInventory runtimeIdentifier

        let node = JsonObject()
        node.["status"] <- jstr "script-passed"
        node.["script"] <- jstr script
        node.["log"] <- jstr logPath
        node.["results"] <- jstr resultsPath
        node.["scenarios"] <- jint scenarios.Length
        node
    | None ->
        match evidencePath with
        | None -> failNow "Compatibility evidence is required. Supply CompatibilityScript or CompatibilityEvidencePath."
        | Some evidence ->
            if not (File.Exists evidence) then
                failNow (sprintf "Compatibility evidence was not found: '%s'." evidence)

            let destination = Path.Combine(compatibilityDirectory, fileNameOf evidence)
            File.Copy(evidence, destination, true)
            use destinationDocument = JsonDocument.Parse(File.ReadAllText destination)

            let scenarios =
                assertCompatibilityEvidence destinationDocument.RootElement packageInventory runtimeIdentifier

            let node = JsonObject()
            node.["status"] <- jstr "evidence-copied"
            node.["path"] <- jstr destination
            node.["sha256"] <- jstr (sha256File destination)
            node.["scenarios"] <- jint scenarios.Length
            node
