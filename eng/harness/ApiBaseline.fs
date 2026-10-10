module FunnySharp.Harness.ApiBaseline

// Compares the shipping assemblies' reflected public surface with eng/api-baseline.
// Verify mode reads only; --write refreshes baselines for an accepted API change.
// Exit codes: 0 verified/written, 1 drift or missing baseline, 2 usage/unbuilt assembly.

open System
open System.Collections.Generic
open System.Globalization
open System.IO
open System.Reflection
open System.Text
open System.Text.RegularExpressions
open FunnySharp.Harness.Repo

/// A value the runtime may leave null even though the API is annotated non-null
/// (NullabilityInfo.ElementType, NullabilityInfoContext.Create results).
let private someOrNull (value: 'T | null) : 'T option =
    match value with
    | null -> None
    | present -> Some present

/// Get-SharedFrameworkDirectory: the highest 10.x shared framework of a name.
let private getSharedFrameworkDirectory (frameworkName: string) (root: string) : string =
    let result =
        Proc.runCaptureIn (Some root) "dotnet" [ "--list-runtimes" ] |> Async.RunSynchronously

    if result.ExitCode <> 0 then
        raise (InvalidOperationException(sprintf "dotnet --list-runtimes failed with exit code %d." result.ExitCode))

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
        raise (InvalidOperationException(sprintf "No .NET 10 '%s' shared framework was found." frameworkName))

    let struct (version, _, path) = candidates.[0]
    Path.Combine(path, version)

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

/// An assembly full name with its `Version=` component removed: a patch release
/// rewrites the version while the documented no-surface-change path holds, so the
/// version is not part of the surface the baseline records. Name, culture and
/// public key token stay - those are compatibility-relevant.
let private withoutAssemblyVersion (identity: string) : string =
    identity.Split(", ")
    |> Array.filter (fun part -> not (part.StartsWith("Version=", StringComparison.Ordinal)))
    |> String.concat ", "

/// Reflect the public API as baseline lines, without inspecting non-API assembly bytes.
let getPublicApiText (assemblyPaths: string list) (root: string) : string list =
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
        let lines = ResizeArray<string>()

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

            let identity = Option.ofObj assembly.FullName |> Option.defaultValue ""
            lines.Add("ASSEMBLY " + withoutAssemblyVersion identity)

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

                let kind =
                    if apiType.IsInterface then "interface"
                    elif apiType.IsEnum then "enum"
                    elif apiType.IsValueType then "struct"
                    elif (match apiType.BaseType with
                          | null -> false
                          | baseType -> baseType = typeof<MulticastDelegate>) then
                        "delegate"
                    else "class"

                lines.Add(kind.ToUpperInvariant() + " " + formatApiType apiType None)

                for memberKind, members in
                    [ "CONSTRUCTOR", constructors
                      "METHOD", methods
                      "PROPERTIE", properties // Preserve the committed renderer's TrimEnd('s') label.
                      "FIELD", fields
                      "EVENT", events ] do
                    for memberText in members do
                        lines.Add("  " + memberKind + " " + memberText)

        List.ofSeq lines
    finally
        AppDomain.CurrentDomain.remove_AssemblyResolve resolver

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

/// Render one assembly's public API.
/// A single-element assembly list keeps each rendering self-contained: its own
/// `ASSEMBLY` line followed by that assembly's types and members.
let private renderAssembly (repositoryRoot: string) (assemblyPath: string) : string list =
    getPublicApiText [ assemblyPath ] repositoryRoot

/// Read one committed baseline (BOM-aware, universal newlines) as its lines.
let private readBaselineLines (path: string) : string list =
    File.ReadAllLines(path, utf8NoBom) |> List.ofArray

/// (type count, member count) over one assembly's rendered lines, counted from the
/// very lines that get compared: getPublicApiText writes one 'ASSEMBLY' line per
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
/// assembly matches its committed baseline in eng/api-baseline/.
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
