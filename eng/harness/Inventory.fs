module FunnySharp.Harness.Inventory

// A behaviour-identical F# port of eng/tools/inventory.py: the cross-platform
// orchestrator that regenerates the next-stage evidence inventories. The C#
// dumper (eng/next-stage-inventory/api-inventory.csproj) stays the content
// engine; this module only resolves inputs, runs the eleven generation targets
// plus the language-ext type list, and normalizes every produced file and
// captured stream to LF + UTF-8 without BOM.
//
// Inputs fail closed: every missing path is listed with remediation and nothing
// is downloaded. Exit codes: 0 success, 1 build/target failure, 2 usage or
// environment failure.

open System
open System.Collections.Generic
open System.ComponentModel
open System.Diagnostics
open System.IO
open System.Text
open System.Text.Json
open System.Threading.Tasks
open FunnySharp.Harness.Output
open FunnySharp.Harness.Repo

/// Bad CLI usage or an unsafe output target.
exception UsageFailure of string

/// Missing tooling or an unusable environment.
exception EnvironmentFailure of string

// ---------------------------------------------------------------------------
// Constants and target table
// ---------------------------------------------------------------------------

[<Literal>]
let TYPE_LIST_NAME = "types-languageext-4.4.9"

let private rootBaseline = "baseline"
let private rootRef = "ref"
let private rootFunnySharpBin = "funny_sharp_bin"
let private rootFunnySharpAspnetBin = "funny_sharp_aspnet_bin"
let private rootAspnetFramework = "aspnet_framework"
let private rootRoslyn = "roslyn"

let private remediation =
    "Remediation: acquire and extract the pinned inputs described in "
    + "docs/next-stage/baselines.md; nothing is downloaded automatically."

let private fsharpInclude =
    @"^Microsoft\.FSharp\.(Core\.(FSharpOption|FSharpValueOption|FSharpResult|FSharpChoice|FSharpFunc|FSharpType|FSharpValue|Unit|OptionModule|ValueOptionModule|ResultModule|Choice|Nullable|LanguagePrimitives|Operators|ExtraTopLevelOperators|FuncConvert|OptimizedClosures|Lazy|MatchFailureException|Printf)|Collections\.(SeqModule|ListModule|ArrayModule|SetModule|MapModule|FSharpList|FSharpSet|FSharpMap|ResizeArray|Seq)|Control\.(FSharpAsync|FSharpMailboxProcessor|TaskBuilder|EventModule|FSharpEvent|IEvent|LazyExtensions))"

let private languageExtInclude = @"^LanguageExt\.[A-Za-z]+$"

let private bclSequencesLinqIncludes =
    [ @"^System\.Linq\.(Enumerable|AsyncEnumerable|Lookup|IGrouping|IOrderedEnumerable|OrderedEnumerable)"
      @"^System\.Collections\.Generic\.(IEnumerable|IAsyncEnumerable|IReadOnlyList|IReadOnlyCollection|IReadOnlyDictionary|IList|IDictionary|List|Dictionary|HashSet|SortedSet|SortedDictionary|KeyValuePair|IEqualityComparer|IEnumerator|IAsyncEnumerator)"
      @"^System\.Span|^System\.ReadOnlySpan|^System\.Memory$|^System\.ReadOnlyMemory|^System\.MemoryExtensions|^System\.Array$|^System\.ArraySegment|^System\.Buffers\." ]

let private bclCollectionsImmutableIncludes =
    [ @"^System\.Collections\.Immutable\.|^System\.Collections\.Frozen\.|^System\.Collections\.Concurrent\.|^System\.Collections\.ObjectModel\." ]

let private bclAsyncConcurrencyIncludes =
    [ @"^System\.Threading\.Tasks\.(Task|ValueTask|TaskCompletionSource|TaskFactory|TaskScheduler|Parallel|ParallelOptions|TaskCreationOptions|TaskContinuationOptions|TaskStatus|TaskCanceledException|ValueTaskSourceStatus|IValueTaskSource|TaskExtensions|TaskAsyncEnumerableExtensions|ParallelEnumerable)"
      @"^System\.Threading\.(Channels\.|CancellationToken|CancellationTokenSource|CancellationTokenRegistration|TimeProvider|ITimer|Timer|PeriodicTimer|Lock|Interlocked|Volatile|LazyInitializer|Timeout|WaitHandle|ManualResetEventSlim|SemaphoreSlim|CountdownEvent|Barrier|ReaderWriterLockSlim)"
      @"^System\.(IAsyncDisposable|IDisposable|IAsyncEnumerable|TimeProvider|TimeoutException|OperationCanceledException)"
      @"^System\.Runtime\.CompilerServices\.(AsyncTaskMethodBuilder|AsyncValueTaskMethodBuilder|ConfiguredTaskAwaitable|ConfiguredValueTaskAwaitable|IAsyncStateMachine|AsyncIteratorMethodBuilder|TaskAwaiter|ValueTaskAwaiter|PoolingAsyncValueTaskMethodBuilder|EnumeratorCancellationAttribute|AsyncMethodBuilderAttribute|INotifyCompletion|ICriticalNotifyCompletion)" ]

let private bclLanguageErrorsIncludes =
    [ @"^System\.(Nullable|Nullable`1|Func|Action|Predicate|Comparison|Converter|Lazy|Tuple|ValueTuple|Exception|AggregateException|SystemException|InvalidOperationException|ArgumentNullException|ArgumentException|ArgumentOutOfRangeException|OperationCanceledException|TimeoutException|ObjectDisposedException|NotSupportedException|NotImplementedException|FormatException|Environment|Math|Convert|String|StringComparison|DateTime|DateTimeOffset|TimeSpan|Guid|Uri|Random|Version|IEquatable|IComparable|IFormattable|ISpanFormattable|Comparison`1)"
      @"^System\.Diagnostics\.CodeAnalysis\.(MaybeNull|NotNull|AllowNull|DisallowNull|MaybeNullWhen|NotNullWhen|NotNullIfNotNull|MemberNotNull|DoesNotReturn|DoesNotReturnIf|SetsRequiredMembers|StringSyntax)"
      @"^System\.Runtime\.CompilerServices\.(NullableAttribute|NullableContextAttribute|IsReadOnlyAttribute|IsByRefLikeAttribute|RequiredMemberAttribute|CompilerFeatureRequiredAttribute)" ]

/// A path expressed as a root key plus a relative suffix.
type PathSpec =
    { Root: string
      Rel: string
      Kind: string }

/// One dumper invocation; `ListTypes` selects the type-list target shape.
type Target =
    { Name: string
      Title: string
      Mode: string
      Assemblies: PathSpec list
      CoreDir: PathSpec option
      ResolveDirs: PathSpec list
      Includes: string list
      ListTypes: bool }

let private fileSpec (root: string) (rel: string) : PathSpec =
    { Root = root; Rel = rel; Kind = "file" }

let private dirSpec (root: string) : PathSpec =
    { Root = root; Rel = ""; Kind = "dir" }

let private target
    (name: string)
    (title: string)
    (mode: string)
    (assemblies: PathSpec list)
    : Target =
    { Name = name
      Title = title
      Mode = mode
      Assemblies = assemblies
      CoreDir = None
      ResolveDirs = []
      Includes = []
      ListTypes = false }

// The FunnySharp self-dump labels are commit-bound and recorded deliberately in
// this table (they match the committed dumps); third-party survey titles stay
// pinned to their recorded baseline. Order is fixed: 11 generation targets then
// the language-ext type list.
let TARGETS : Target list =
    [ { (target "funny-sharp-core" "FunnySharp core public API (Goal 16 completion, 0.1.0)" "runtime" [ fileSpec rootFunnySharpBin "FunnySharp.dll" ]) with
          CoreDir = None
          ResolveDirs = []
          Includes = []
          ListTypes = false }
      { (target
             "funny-sharp-aspnetcore"
             "FunnySharp.AspNetCore public API (Goal 16 completion, 0.1.0)"
             "runtime"
             [ fileSpec rootFunnySharpAspnetBin "FunnySharp.AspNetCore.dll" ]) with
          ResolveDirs = [ dirSpec rootFunnySharpBin; dirSpec rootAspnetFramework ] }
      target "funcky" "Public API inventory: funcky" "runtime" [ fileSpec rootBaseline "funcky.3.6.0/lib/net10.0/Funcky.dll" ]
      { (target
             "funcky-analyzers"
             "Funcky built-in analyzers (metadata mode)"
             "metadata"
             [ fileSpec rootBaseline "funcky.3.6.0/analyzers/dotnet/cs/Funcky.BuiltinAnalyzers.dll" ]) with
          CoreDir = Some(dirSpec rootRef)
          ResolveDirs = [ dirSpec rootRoslyn ] }
      target
          "csharpfunctionalextensions"
          "Public API inventory: csharpfunctionalextensions"
          "runtime"
          [ fileSpec rootBaseline "csharpfunctionalextensions.3.7.0/lib/net8.0/CSharpFunctionalExtensions.dll" ]
      { (target
             "fsharp-core"
             "Public API inventory: fsharp-core"
             "runtime"
             [ fileSpec rootBaseline "fsharp.core.10.1.401/lib/netstandard2.1/FSharp.Core.dll" ]) with
          Includes = [ fsharpInclude ] }
      { (target
             "language-ext"
             "Public API inventory: language-ext"
             "metadata"
             [ fileSpec rootBaseline "languageext.core.4.4.9/lib/netstandard2.0/LanguageExt.Core.dll" ]) with
          CoreDir = Some(dirSpec rootRef)
          Includes = [ languageExtInclude ] }
      { (target
             "bcl-sequences-linq"
             "Public API inventory: bcl-sequences-linq"
             "metadata"
             [ fileSpec rootRef "System.Linq.dll"
               fileSpec rootRef "System.Linq.AsyncEnumerable.dll"
               fileSpec rootRef "System.Runtime.dll"
               fileSpec rootRef "System.Collections.dll"
               fileSpec rootRef "System.Memory.dll"
               fileSpec rootRef "System.Buffers.dll"
               fileSpec rootRef "System.Threading.Tasks.Extensions.dll" ]) with
          CoreDir = Some(dirSpec rootRef)
          Includes = bclSequencesLinqIncludes }
      { (target
             "bcl-collections-immutable"
             "Public API inventory: bcl-collections-immutable"
             "metadata"
             [ fileSpec rootRef "System.Collections.Immutable.dll"
               fileSpec rootRef "System.Collections.dll"
               fileSpec rootRef "System.Collections.Concurrent.dll"
               fileSpec rootRef "System.Linq.dll" ]) with
          CoreDir = Some(dirSpec rootRef)
          Includes = bclCollectionsImmutableIncludes }
      { (target
             "bcl-async-concurrency"
             "Public API inventory: bcl-async-concurrency"
             "metadata"
             [ fileSpec rootRef "System.Runtime.dll"
               fileSpec rootRef "System.Threading.Channels.dll"
               fileSpec rootRef "System.Threading.Tasks.Parallel.dll"
               fileSpec rootRef "System.Threading.Tasks.dll"
               fileSpec rootRef "System.Threading.dll" ]) with
          CoreDir = Some(dirSpec rootRef)
          Includes = bclAsyncConcurrencyIncludes }
      { (target
             "bcl-language-errors"
             "Public API inventory: bcl-language-errors"
             "metadata"
             [ fileSpec rootRef "System.Runtime.dll"
               fileSpec rootRef "System.Runtime.Extensions.dll"
               fileSpec rootRef "System.Linq.Expressions.dll" ]) with
          CoreDir = Some(dirSpec rootRef)
          Includes = bclLanguageErrorsIncludes }
      { (target
             TYPE_LIST_NAME
             ""
             "metadata"
             [ fileSpec rootBaseline "languageext.core.4.4.9/lib/netstandard2.0/LanguageExt.Core.dll" ]) with
          CoreDir = Some(dirSpec rootRef)
          ListTypes = true } ]

// ---------------------------------------------------------------------------
// Environment and inputs
// ---------------------------------------------------------------------------

/// The resolved environment inputs the orchestrator works from.
type Inputs =
    { BaselineRoot: string option
      RefPackDir: string option
      FunnySharpBin: string
      FunnySharpAspnetBin: string
      DotnetRoot: string
      AspnetFrameworkDir: string option
      RoslynDir: string option
      Csproj: string }

/// One required path with the purpose shown in preflight reports.
type RequiredInput =
    { Display: string
      Path: string option
      Kind: string
      Purpose: string }

    member this.Present =
        match this.Path with
        | None -> false
        | Some path -> if this.Kind = "dir" then Directory.Exists path else File.Exists path

// Natural version sort ("10.0.9" < "10.0.11"), matching Python's
// re.split(r"(\d+)", name) key of (0, text) / (1, int) pairs. The union case
// order (Text before Number) reproduces the 0/1 tuple ordering.
type private VersionPart =
    | Text of string
    | Number of int

let private versionKey (name: string) : VersionPart list =
    let parts = ResizeArray<VersionPart>()
    let buffer = StringBuilder()

    let flushText () =
        parts.Add(Text(buffer.ToString()))
        buffer.Clear() |> ignore

    let mutable index = 0

    while index < name.Length do
        let ch = name.[index]

        if Char.IsAsciiDigit ch then
            flushText ()
            let start = index

            while index < name.Length && Char.IsAsciiDigit name.[index] do
                index <- index + 1

            parts.Add(Number(int (name.Substring(start, index - start))))
        else
            buffer.Append ch |> ignore
            index <- index + 1

    flushText ()
    List.ofSeq parts

let private fileNameOf (path: string) : string =
    match Path.GetFileName path with
    | null -> path
    | name -> name

let private newestVersionDir (parent: string) : string option =
    if not (Directory.Exists parent) then
        None
    else
        let candidates = Directory.GetDirectories parent |> Array.toList

        match candidates with
        | [] -> None
        | _ -> Some(candidates |> List.maxBy (fun directory -> versionKey (fileNameOf directory)))

let private globalJsonSdkVersion (repoRoot: string) : string option =
    try
        let path = Path.Combine(repoRoot, "global.json")

        if not (File.Exists path) then
            None
        else
            use document = JsonDocument.Parse(File.ReadAllText path)

            match document.RootElement.TryGetProperty "sdk" with
            | true, sdk ->
                match sdk.TryGetProperty "version" with
                | true, version when version.ValueKind = JsonValueKind.String ->
                    match version.GetString() with
                    | null -> None
                    | text when text = "" -> None
                    | text -> Some text
                | _ -> None
            | _ -> None
    with _ ->
        None

let private defaultRoslynDir (repoRoot: string) (dotnetRoot: string) : string option =
    let sdkParent = Path.Combine(dotnetRoot, "sdk")
    let pinned = globalJsonSdkVersion repoRoot

    let sdk =
        match pinned with
        | Some version when Directory.Exists(Path.Combine(sdkParent, version)) -> Some(Path.Combine(sdkParent, version))
        | _ -> newestVersionDir sdkParent

    sdk |> Option.map (fun directory -> Path.Combine(directory, "Roslyn", "bincore"))

let private homeDirectory () : string =
    match Environment.GetEnvironmentVariable "HOME" with
    | null | "" -> Environment.GetFolderPath Environment.SpecialFolder.UserProfile
    | home -> home

/// Resolve the environment inputs the way inventory.py does, honouring the
/// injected environment map (HOME, DOTNET_ROOT, FUNNY_SHARP_BIN, ...).
let resolveInputs
    (repoRoot: string)
    (env: Map<string, string>)
    (baselineRoot: string option)
    (refPackDir: string option)
    : Inputs =
    let envValue key =
        match Map.tryFind key env with
        | Some value when value <> "" -> Some value
        | _ -> None

    let home =
        match envValue "HOME" with
        | Some value -> value
        | None -> homeDirectory ()

    let dotnetRoot =
        match envValue "DOTNET_ROOT" with
        | Some value -> value
        | None -> Path.Combine(home, ".dotnet")

    let funnySharpBin =
        match envValue "FUNNY_SHARP_BIN" with
        | Some value -> value
        | None -> Path.Combine(repoRoot, "src", "FunnySharp", "bin", "Release", "net10.0")

    let funnySharpAspnetBin =
        match envValue "FUNNY_SHARP_ASPNET_BIN" with
        | Some value -> value
        | None -> Path.Combine(repoRoot, "src", "FunnySharp.AspNetCore", "bin", "Release", "net10.0")

    let aspnetFrameworkDir =
        match envValue "ASPNET_FRAMEWORK_DIR" with
        | Some value -> Some value
        | None -> newestVersionDir (Path.Combine(dotnetRoot, "shared", "Microsoft.AspNetCore.App"))

    let roslynDir =
        match envValue "ROSLYN_DIR" with
        | Some value -> Some value
        | None -> defaultRoslynDir repoRoot dotnetRoot

    let nonEmpty value =
        match value with
        | Some text when text <> "" -> Some text
        | _ -> None

    { BaselineRoot = nonEmpty baselineRoot
      RefPackDir = nonEmpty refPackDir
      FunnySharpBin = funnySharpBin
      FunnySharpAspnetBin = funnySharpAspnetBin
      DotnetRoot = dotnetRoot
      AspnetFrameworkDir = aspnetFrameworkDir
      RoslynDir = roslynDir
      Csproj = Path.Combine(repoRoot, "eng", "next-stage-inventory", "api-inventory.csproj") }

let private rootFor (inputs: Inputs) (key: string) : string option =
    match key with
    | "baseline" -> inputs.BaselineRoot
    | "ref" -> inputs.RefPackDir
    | "funny_sharp_bin" -> Some inputs.FunnySharpBin
    | "funny_sharp_aspnet_bin" -> Some inputs.FunnySharpAspnetBin
    | "aspnet_framework" -> inputs.AspnetFrameworkDir
    | "roslyn" -> inputs.RoslynDir
    | _ -> None

let private combineRel (baseDirectory: string) (rel: string) : string =
    if rel = "" then
        baseDirectory
    else
        Path.Combine(baseDirectory, rel.Replace('/', Path.DirectorySeparatorChar))

/// The absolute-or-rooted path a spec resolves to, or None when its root is absent.
let resolveSpec (inputs: Inputs) (spec: PathSpec) : string option =
    rootFor inputs spec.Root |> Option.map (fun baseDirectory -> combineRel baseDirectory spec.Rel)

let private missingRootDisplay (key: string) : string =
    match key with
    | "baseline" -> "<baseline-root>"
    | "ref" -> "<ref-pack-dir>"
    | "aspnet_framework" -> "<DOTNET_ROOT>/shared/Microsoft.AspNetCore.App/<version>"
    | "roslyn" -> "<DOTNET_ROOT>/sdk/<version>/Roslyn/bincore"
    | other -> sprintf "<%s>" other

let private displaySpec (inputs: Inputs) (spec: PathSpec) : string =
    match rootFor inputs spec.Root with
    | Some baseDirectory -> combineRel baseDirectory spec.Rel
    | None ->
        let prefix = missingRootDisplay spec.Root
        if spec.Rel = "" then prefix else prefix + "/" + spec.Rel

/// Every required input in first-seen order, de-duplicated by (display, kind).
let requiredInputs (inputs: Inputs) : RequiredInput list =
    let required = ResizeArray<RequiredInput>()
    let seen = HashSet<string * string>()

    let add (display: string) (path: string option) (kind: string) (purpose: string) =
        if seen.Add(display, kind) then
            required.Add
                { Display = display
                  Path = path
                  Kind = kind
                  Purpose = purpose }

    add inputs.Csproj (Some inputs.Csproj) "file" "inventory dumper project"

    for target in TARGETS do
        for spec in target.Assemblies do
            add
                (displaySpec inputs spec)
                (resolveSpec inputs spec)
                spec.Kind
                (sprintf "required by target %s" target.Name)

        for spec in target.ResolveDirs do
            add
                (displaySpec inputs spec)
                (resolveSpec inputs spec)
                spec.Kind
                (sprintf "resolve directory for target %s" target.Name)

        match target.CoreDir with
        | Some spec ->
            add
                (displaySpec inputs spec)
                (resolveSpec inputs spec)
                spec.Kind
                (sprintf "core directory for target %s" target.Name)
        | None -> ()

    List.ofSeq required

let missingInputs (inputs: Inputs) : RequiredInput list =
    requiredInputs inputs |> List.filter (fun item -> not item.Present)

let printMissingInputs (stderr: TextWriter) (missing: RequiredInput list) : unit =
    for item in missing do
        stderr.WriteLine(sprintf "MISSING: %s (%s)" item.Display item.Purpose)

    stderr.WriteLine remediation

/// Report-only input status. Returns 0 when every input is present else 2.
let checkInputs (stdout: TextWriter) (stderr: TextWriter) (inputs: Inputs) : int =
    let items = requiredInputs inputs
    let presence = items |> List.map (fun item -> item, item.Present)

    for item, present in presence do
        let state = if present then "OK" else "MISSING"
        stdout.WriteLine(sprintf "%s: %s (%s)" state item.Display item.Purpose)

    let missing = presence |> List.filter (fun (_, present) -> not present) |> List.map fst

    if not missing.IsEmpty then
        stderr.WriteLine(
            sprintf "INPUT CHECK FAILED: %d of %d required inputs are missing." missing.Length items.Length
        )

        stderr.WriteLine remediation
        2
    else
        stdout.WriteLine(sprintf "INPUT CHECK OK: all %d required inputs present." items.Length)
        0

// ---------------------------------------------------------------------------
// Output directory safety
// ---------------------------------------------------------------------------

let private normCase (path: string) : string =
    if OperatingSystem.IsWindows() then
        path.ToLowerInvariant().Replace('/', '\\')
    else
        path

let private resolveOneLink (path: string) : string =
    try
        if Directory.Exists path then
            match (DirectoryInfo path).ResolveLinkTarget true with
            | null -> path
            | target -> target.FullName
        elif File.Exists path then
            match (FileInfo path).ResolveLinkTarget true with
            | null -> path
            | target -> target.FullName
        else
            path
    with _ ->
        path

/// Best-effort os.path.realpath: resolve symlinks in every existing component.
let private realPath (path: string) : string =
    let full = Path.GetFullPath path

    match Path.GetPathRoot full with
    | null -> full
    | root ->
        if root = "" then
            full
        else
            let separators = [| Path.DirectorySeparatorChar; Path.AltDirectorySeparatorChar |]

            let parts =
                full.Substring(root.Length).Split(separators, StringSplitOptions.RemoveEmptyEntries)

            let mutable current = root

            for part in parts do
                current <- Path.Combine(current, part)
                current <- resolveOneLink current

            current

let private samePath (left: string) (right: string) : bool =
    normCase (realPath left) = normCase (realPath right)

let private isReparsePoint (path: string) : bool =
    try
        let attributes = File.GetAttributes path

        if attributes.HasFlag FileAttributes.ReparsePoint then
            true
        elif Directory.Exists path then
            not (isNull (DirectoryInfo path).LinkTarget)
        elif File.Exists path then
            not (isNull (FileInfo path).LinkTarget)
        else
            false
    with _ ->
        false

let private pathComponents (absolute: string) : string list =
    let parents = ResizeArray<string>()
    let mutable current = absolute
    let mutable running = true

    while running do
        match Path.GetDirectoryName current with
        | null -> running <- false
        | parent when parent = current -> running <- false
        | parent when parent = "" -> running <- false
        | parent ->
            parents.Add parent
            current <- parent

    let ordered = ResizeArray<string>(Seq.rev parents)
    ordered.Add absolute
    List.ofSeq ordered

let private expandUser (path: string) : string =
    if path = "~" then
        homeDirectory ()
    elif path.StartsWith("~" + string Path.DirectorySeparatorChar, StringComparison.Ordinal)
         || path.StartsWith("~/", StringComparison.Ordinal) then
        Path.Combine(homeDirectory (), path.Substring 2)
    else
        path

/// Reject the repository root, symlinked/reparse components and existing files.
/// Raises UsageFailure; returns the absolute output directory otherwise.
let validateOutputDir (outputDir: string) (repoRoot: string) : string =
    let absolute = Path.GetFullPath(expandUser outputDir)

    if samePath absolute repoRoot then
        raise (UsageFailure(sprintf "output directory must not be the repository root: %s" absolute))

    for pathPart in pathComponents absolute do
        if isReparsePoint pathPart then
            raise (
                UsageFailure(
                    sprintf "output directory path contains a symlink or reparse point: %s" pathPart
                )
            )

    if File.Exists absolute && not (Directory.Exists absolute) then
        raise (UsageFailure(sprintf "output path exists and is not a directory: %s" absolute))

    absolute

/// The default output directory: <resolved temp root>/funnysharp-inventory.
let defaultOutputDir () : string =
    Path.Combine(realPath (Path.GetTempPath ()), "funnysharp-inventory")

// ---------------------------------------------------------------------------
// Text normalization
// ---------------------------------------------------------------------------

/// Strip a UTF-8 BOM and normalize CRLF / lone CR to LF.
let normalizeBytes (data: byte array) : byte array =
    let withoutBom =
        if data.Length >= 3 && data.[0] = 0xEFuy && data.[1] = 0xBBuy && data.[2] = 0xBFuy then
            data.[3..]
        else
            data

    let result = ResizeArray<byte>(withoutBom.Length)
    let mutable index = 0

    while index < withoutBom.Length do
        if withoutBom.[index] = 0x0Duy then
            result.Add 0x0Auy

            if index + 1 < withoutBom.Length && withoutBom.[index + 1] = 0x0Auy then
                index <- index + 2
            else
                index <- index + 1
        else
            result.Add withoutBom.[index]
            index <- index + 1

    result.ToArray()

/// Normalize a file in place; returns true when the bytes changed.
let normalizeFile (path: string) : bool =
    let data = File.ReadAllBytes path
    let normalized = normalizeBytes data

    if normalized <> data then
        File.WriteAllBytes(path, normalized)
        true
    else
        false

let private normalizeIfExists (path: string) : unit =
    if File.Exists path then
        normalizeFile path |> ignore

// ---------------------------------------------------------------------------
// Command execution
// ---------------------------------------------------------------------------

/// Captured process outcome: exit code plus the raw stdout and stderr bytes.
type CommandResult =
    { ExitCode: int
      Stdout: byte array
      Stderr: byte array }

type CommandRunner = string list -> Map<string, string> -> CommandResult

let private readAllBytes (stream: Stream) : byte array =
    use buffer = new MemoryStream()
    stream.CopyTo buffer
    buffer.ToArray()

let private defaultRunner : CommandRunner =
    fun argv env ->
        match argv with
        | [] -> raise (InvalidOperationException "empty command")
        | exe :: arguments ->
            let info = ProcessStartInfo exe
            info.UseShellExecute <- false
            info.RedirectStandardOutput <- true
            info.RedirectStandardError <- true
            info.Environment.Clear()

            for KeyValue(key, value) in env do
                info.Environment.[key] <- value

            for argument in arguments do
                info.ArgumentList.Add argument

            use proc = new Process()
            proc.StartInfo <- info

            if not (proc.Start()) then
                raise (InvalidOperationException(sprintf "failed to start process '%s'" exe))

            let stdoutTask = Task.Run(fun () -> readAllBytes proc.StandardOutput.BaseStream)
            let stderrTask = Task.Run(fun () -> readAllBytes proc.StandardError.BaseStream)
            proc.WaitForExit()
            let stdout = stdoutTask.GetAwaiter().GetResult()
            let stderr = stderrTask.GetAwaiter().GetResult()

            { ExitCode = proc.ExitCode
              Stdout = stdout
              Stderr = stderr }

let private childEnv (env: Map<string, string>) (dotnetRoot: string) : Map<string, string> =
    let currentPath =
        match Map.tryFind "PATH" env with
        | Some path -> path
        | None ->
            match Map.tryFind "Path" env with
            | Some path -> path
            | None -> ""

    let newPath = dotnetRoot + string Path.PathSeparator + currentPath

    let withPath =
        env |> Map.add "PATH" newPath

    let withPath =
        if OperatingSystem.IsWindows() then
            withPath |> Map.add "Path" newPath
        else
            withPath

    withPath
    |> Map.add "DOTNET_ROOT" dotnetRoot
    |> Map.add "DOTNET_CLI_TELEMETRY_OPTOUT" "1"

let private runDotnet
    (argv: string list)
    (inputs: Inputs)
    (env: Map<string, string>)
    (runner: CommandRunner)
    : CommandResult =
    let childEnvironment = childEnv env inputs.DotnetRoot

    let notFound (message: string) =
        EnvironmentFailure(
            sprintf "dotnet executable not found (%s); install the .NET SDK pinned by global.json" message
        )

    try
        runner argv childEnvironment
    with
    | :? FileNotFoundException as ex -> raise (notFound ex.Message)
    | :? DirectoryNotFoundException as ex -> raise (notFound ex.Message)
    | :? Win32Exception as ex when ex.NativeErrorCode = 2 -> raise (notFound ex.Message)
    | ex ->
        raise (
            EnvironmentFailure(
                sprintf "cannot execute dotnet (%s); install the .NET SDK pinned by global.json" ex.Message
            )
        )

// ---------------------------------------------------------------------------
// Target argv, build and generation
// ---------------------------------------------------------------------------

let private resolvedArgv (target: Target) (inputs: Inputs) (spec: PathSpec) : string =
    match resolveSpec inputs spec with
    | Some path -> path
    | None ->
        raise (
            EnvironmentFailure(
                sprintf "unresolved input for target %s: %s/%s" target.Name spec.Root spec.Rel
            )
        )

/// Build the dumper argv, preserving the exact established argument order.
let targetArgv (target: Target) (inputs: Inputs) (outDir: string) : string list =
    let head =
        [ "dotnet"; "run"; "-c"; "Release"; "--no-build"; "--project"; inputs.Csproj; "--" ]

    let assemblyArgs =
        target.Assemblies |> List.collect (fun spec -> [ "--assembly"; resolvedArgv target inputs spec ])

    let resolveArgs =
        target.ResolveDirs
        |> List.collect (fun spec -> [ "--resolve-dir"; resolvedArgv target inputs spec ])

    let body =
        if target.Mode = "metadata" then
            let mode = [ "--mode"; "metadata" ]

            let core =
                match target.CoreDir with
                | Some spec -> [ "--core-dir"; resolvedArgv target inputs spec ]
                | None -> []

            mode @ core @ resolveArgs @ assemblyArgs
        else
            assemblyArgs @ resolveArgs

    let withIncludes =
        body
        @ (target.Includes |> List.collect (fun pattern -> [ "--include"; pattern ]))

    if target.ListTypes then
        head @ withIncludes @ [ "--list-types" ]
    else
        head
        @ withIncludes
        @ [ "--out-md"
            Path.Combine(outDir, sprintf "inv-%s.md" target.Name)
            "--out-json"
            Path.Combine(outDir, sprintf "inv-%s.json" target.Name)
            "--title"
            target.Title ]

/// Python str.splitlines()[-count:], reading with UTF-8 replacement.
let private tailLines (path: string) (count: int) : string list =
    if not (File.Exists path) then
        []
    else
        let lines = splitLines (File.ReadAllText(path, Encoding.UTF8))
        let total = lines.Length

        if count >= total then
            List.ofArray lines
        else
            lines.[total - count ..] |> List.ofArray

let private buildDumper
    (stderr: TextWriter)
    (inputs: Inputs)
    (env: Map<string, string>)
    (runner: CommandRunner)
    : bool =
    let argv = [ "dotnet"; "build"; inputs.Csproj; "-c"; "Release"; "--nologo" ]
    let result = runDotnet argv inputs env runner

    if result.ExitCode <> 0 then
        stderr.WriteLine "FAILED: inventory tool build"

        let output =
            Array.append (normalizeBytes result.Stdout) (normalizeBytes result.Stderr)

        stderr.Write(Encoding.UTF8.GetString output)
        false
    else
        true

let private runTarget
    (stdout: TextWriter)
    (target: Target)
    (inputs: Inputs)
    (outDir: string)
    (env: Map<string, string>)
    (runner: CommandRunner)
    : bool =
    stdout.WriteLine(sprintf "== %s" target.Name)
    let argv = targetArgv target inputs outDir
    let result = runDotnet argv inputs env runner
    let outBytes = normalizeBytes result.Stdout
    let errBytes = normalizeBytes result.Stderr

    if target.ListTypes then
        let txt = Path.Combine(outDir, target.Name + ".txt")
        let err = Path.Combine(outDir, target.Name + ".err")
        File.WriteAllBytes(txt, outBytes)
        File.WriteAllBytes(err, errBytes)

        if result.ExitCode <> 0 then
            stdout.WriteLine(sprintf "FAILED: %s" target.Name)

            for line in tailLines err 5 do
                stdout.WriteLine line

            false
        else
            true
    else
        let log = Path.Combine(outDir, sprintf "inv-%s.log" target.Name)
        File.WriteAllBytes(log, Array.append outBytes errBytes)
        normalizeIfExists (Path.Combine(outDir, sprintf "inv-%s.md" target.Name))
        normalizeIfExists (Path.Combine(outDir, sprintf "inv-%s.json" target.Name))

        if result.ExitCode <> 0 then
            stdout.WriteLine(sprintf "FAILED: %s" target.Name)

            for line in tailLines log 5 do
                stdout.WriteLine line

            false
        else
            for line in tailLines log 2 do
                stdout.WriteLine line

            true

/// Build the dumper then run every target, aggregating failures.
let private generate
    (stdout: TextWriter)
    (stderr: TextWriter)
    (inputs: Inputs)
    (outDir: string)
    (env: Map<string, string>)
    (runner: CommandRunner)
    : int =
    if not (buildDumper stderr inputs env runner) then
        1
    else
        let mutable failed = false

        for target in TARGETS do
            if not (runTarget stdout target inputs outDir env runner) then
                failed <- true

        if failed then
            stderr.WriteLine(
                sprintf
                    "FAILED: one or more inventory targets failed; output in %s is incomplete"
                    outDir
            )

            1
        else
            stdout.WriteLine "ALL DONE"
            0

// ---------------------------------------------------------------------------
// CLI
// ---------------------------------------------------------------------------

type private CliOptions =
    { BaselineRoot: string option
      RefPackDir: string option
      OutputDir: string option
      CheckInputs: bool
      Help: bool }

let private usageLine =
    "usage: inventory.py [-h] [--baseline-root DIR] [--ref-pack-dir DIR] [--output-dir DIR] [--check-inputs] [baseline_root] [ref_pack_dir] [output_dir]"

let private helpText =
    String.concat
        "\n"
        [ usageLine
          ""
          "Regenerate the next-stage evidence inventories (see docs/next-stage/baselines.md)."
          ""
          "positional arguments:"
          "  baseline_root   extraction root of the pinned packages"
          "  ref_pack_dir    Microsoft.NETCore.App.Ref ref/net10.0 directory"
          "  output_dir      output directory (default outside the repo)"
          ""
          "options:"
          "  -h, --help      show this help message and exit"
          "  --baseline-root DIR"
          "  --ref-pack-dir DIR"
          "  --output-dir DIR"
          "  --check-inputs  report required-input status without building or writing" ]

let private parseArgs (argv: string list) : Result<CliOptions * string list, string> =
    let rec loop
        (options: CliOptions)
        (positionals: string list)
        (endOfOptions: bool)
        (remaining: string list)
        : Result<CliOptions * string list, string> =
        match remaining with
        | [] ->
            if positionals.Length > 3 then
                Error("unrecognized arguments: " + String.concat " " (positionals |> List.skip 3))
            else
                Ok(options, positionals)
        | arg :: rest when endOfOptions -> loop options (positionals @ [ arg ]) true rest
        | "--" :: rest -> loop options positionals true rest
        | ("-h" | "--help") :: _ -> Ok({ options with Help = true }, positionals)
        | "--check-inputs" :: rest ->
            loop { options with CheckInputs = true } positionals endOfOptions rest
        | "--baseline-root" :: value :: rest ->
            loop { options with BaselineRoot = Some value } positionals endOfOptions rest
        | [ "--baseline-root" ] -> Error "argument --baseline-root: expected one argument"
        | arg :: rest when arg.StartsWith("--baseline-root=", StringComparison.Ordinal) ->
            loop
                { options with BaselineRoot = Some(arg.Substring("--baseline-root=".Length)) }
                positionals
                endOfOptions
                rest
        | "--ref-pack-dir" :: value :: rest ->
            loop { options with RefPackDir = Some value } positionals endOfOptions rest
        | [ "--ref-pack-dir" ] -> Error "argument --ref-pack-dir: expected one argument"
        | arg :: rest when arg.StartsWith("--ref-pack-dir=", StringComparison.Ordinal) ->
            loop
                { options with RefPackDir = Some(arg.Substring("--ref-pack-dir=".Length)) }
                positionals
                endOfOptions
                rest
        | "--output-dir" :: value :: rest ->
            loop { options with OutputDir = Some value } positionals endOfOptions rest
        | [ "--output-dir" ] -> Error "argument --output-dir: expected one argument"
        | arg :: rest when arg.StartsWith("--output-dir=", StringComparison.Ordinal) ->
            loop
                { options with OutputDir = Some(arg.Substring("--output-dir=".Length)) }
                positionals
                endOfOptions
                rest
        | arg :: _ when arg.StartsWith("-", StringComparison.Ordinal) && arg <> "-" ->
            Error("unrecognized arguments: " + arg)
        | arg :: rest -> loop options (positionals @ [ arg ]) endOfOptions rest

    loop
        { BaselineRoot = None
          RefPackDir = None
          OutputDir = None
          CheckInputs = false
          Help = false }
        []
        false
        argv

let private pick
    (name: string)
    (flagValue: string option)
    (positionalValue: string option)
    : Result<string option, string> =
    match flagValue, positionalValue with
    | Some _, Some _ -> Error(sprintf "%s was given both positionally and as --%s" name name)
    | Some value, None -> Ok(Some value)
    | None, Some value -> Ok(Some value)
    | None, None -> Ok None

let private pickAll
    (options: CliOptions)
    (positionals: string list)
    : Result<string option * string option * string option, string> =
    let positional index =
        if positionals.Length > index then Some positionals.[index] else None

    match pick "baseline-root" options.BaselineRoot (positional 0) with
    | Error message -> Error message
    | Ok baselineRoot ->
        match pick "ref-pack-dir" options.RefPackDir (positional 1) with
        | Error message -> Error message
        | Ok refPackDir ->
            match pick "output-dir" options.OutputDir (positional 2) with
            | Error message -> Error message
            | Ok outputDir -> Ok(baselineRoot, refPackDir, outputDir)

let private markerRelative =
    Path.Combine("eng", "next-stage-inventory", "api-inventory.csproj")

/// Locate the repository root the way inventory.py does: marker-verified.
let private resolveRepositoryRoot () : Result<string, string> =
    let start = AppContext.BaseDirectory

    let candidate =
        match tryFindRootFrom start with
        | Some root -> Some root
        | None -> tryFindRoot ()

    match candidate with
    | Some root when File.Exists(Path.Combine(root, markerRelative)) -> Ok root
    | _ -> Error(sprintf "cannot locate the FunnySharp repository root from %s" start)

let private outputDirRemediation =
    "Remediation: choose a writable output directory outside the repository whose path contains no symlinks or reparse points."

let private tryValidateOutputDir (outputDir: string) (repoRoot: string) : Result<string, string> =
    try
        Ok(validateOutputDir outputDir repoRoot)
    with UsageFailure message ->
        Error message

let private tryCreateDirectory (path: string) : Result<unit, string> =
    try
        Directory.CreateDirectory path |> ignore
        Ok()
    with ex ->
        Error ex.Message

let private currentEnvironment () : Map<string, string> =
    let pairs = ResizeArray<string * string>()
    let variables = Environment.GetEnvironmentVariables()

    for key in variables.Keys do
        pairs.Add(string key, string (variables.[key]))

    Map.ofSeq pairs

/// Run the orchestrator writing to the supplied writers with an injected
/// environment and command runner. Returns the Python exit code.
let mainWith
    (stdout: TextWriter)
    (stderr: TextWriter)
    (env: Map<string, string>)
    (runner: CommandRunner)
    (argv: string list)
    : int =
    match parseArgs argv with
    | Error message ->
        stderr.WriteLine usageLine
        stderr.WriteLine("inventory.py: error: " + message)
        2
    | Ok (options, positionals) when options.Help ->
        stdout.WriteLine helpText
        0
    | Ok (options, positionals) ->
        match resolveRepositoryRoot () with
        | Error message ->
            stderr.WriteLine("ERROR: " + message)
            2
        | Ok repo ->
            match pickAll options positionals with
            | Error message ->
                stderr.WriteLine usageLine
                stderr.WriteLine("inventory.py: error: " + message)
                2
            | Ok (baselineRoot, refPackDir, outputDir) ->
                let inputs = resolveInputs repo env baselineRoot refPackDir

                if options.CheckInputs then
                    checkInputs stdout stderr inputs
                else
                    let requested =
                        match outputDir with
                        | Some value -> value
                        | None -> defaultOutputDir ()

                    match tryValidateOutputDir requested repo with
                    | Error message ->
                        stderr.WriteLine("ERROR: " + message)
                        stderr.WriteLine outputDirRemediation
                        2
                    | Ok outDir ->
                        let missing = missingInputs inputs

                        if not missing.IsEmpty then
                            printMissingInputs stderr missing
                            2
                        else
                            match tryCreateDirectory outDir with
                            | Error message ->
                                stderr.WriteLine(
                                    sprintf "ERROR: cannot create output directory %s: %s" outDir message
                                )

                                2
                            | Ok() ->
                                stdout.WriteLine(sprintf "== output %s" outDir)

                                try
                                    generate stdout stderr inputs outDir env runner
                                with
                                | EnvironmentFailure message ->
                                    stderr.WriteLine("ERROR: " + message)
                                    2
                                | ex ->
                                    stderr.WriteLine(
                                        sprintf
                                            "ERROR: cannot write inventory output in %s: %s"
                                            outDir
                                            ex.Message
                                    )

                                    stderr.WriteLine(
                                        sprintf
                                            "Remediation: choose a writable output directory outside the repository or fix the permissions of %s."
                                            outDir
                                    )

                                    2

/// Entry point mirroring inventory.py's main().
let main (argv: string array) : int =
    mainWith Console.Out Console.Error (currentEnvironment ()) defaultRunner (List.ofArray argv)
