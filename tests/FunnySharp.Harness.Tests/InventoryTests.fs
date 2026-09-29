module FunnySharp.Harness.Tests.InventoryTests

// F# xUnit port of eng/tools/tests/test_inventory.py. The suite is structural
// and behavioural: it never builds the C# dumper, injecting a fake command
// runner instead. Byte-for-byte regeneration needs the pinned external inputs
// described in docs/next-stage/baselines.md and is out of scope here.

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open Xunit
open FunnySharp.Harness.Inventory
open FunnySharp.Harness.Tests.Support

let private baselineFiles =
    [ "funcky.3.6.0/lib/net10.0/Funcky.dll"
      "funcky.3.6.0/analyzers/dotnet/cs/Funcky.BuiltinAnalyzers.dll"
      "csharpfunctionalextensions.3.7.0/lib/net8.0/CSharpFunctionalExtensions.dll"
      "fsharp.core.10.1.401/lib/netstandard2.1/FSharp.Core.dll"
      "languageext.core.4.4.9/lib/netstandard2.0/LanguageExt.Core.dll" ]

let private refFiles =
    [ "System.Linq.dll"
      "System.Linq.AsyncEnumerable.dll"
      "System.Runtime.dll"
      "System.Collections.dll"
      "System.Memory.dll"
      "System.Buffers.dll"
      "System.Threading.Tasks.Extensions.dll"
      "System.Collections.Immutable.dll"
      "System.Collections.Concurrent.dll"
      "System.Threading.Channels.dll"
      "System.Threading.Tasks.Parallel.dll"
      "System.Threading.Tasks.dll"
      "System.Threading.dll"
      "System.Runtime.Extensions.dll"
      "System.Linq.Expressions.dll" ]

let private names =
    [ "funny-sharp-core"
      "funny-sharp-aspnetcore"
      "funcky"
      "funcky-analyzers"
      "csharpfunctionalextensions"
      "fsharp-core"
      "language-ext"
      "bcl-sequences-linq"
      "bcl-collections-immutable"
      "bcl-async-concurrency"
      "bcl-language-errors"
      TYPE_LIST_NAME ]

let private titles =
    Map.ofList
        [ "funny-sharp-core", "FunnySharp core public API (Goal 16 completion, 0.1.0)"
          "funny-sharp-aspnetcore", "FunnySharp.AspNetCore public API (Goal 16 completion, 0.1.0)"
          "funcky", "Public API inventory: funcky"
          "funcky-analyzers", "Funcky built-in analyzers (metadata mode)"
          "csharpfunctionalextensions", "Public API inventory: csharpfunctionalextensions"
          "fsharp-core", "Public API inventory: fsharp-core"
          "language-ext", "Public API inventory: language-ext"
          "bcl-sequences-linq", "Public API inventory: bcl-sequences-linq"
          "bcl-collections-immutable", "Public API inventory: bcl-collections-immutable"
          "bcl-async-concurrency", "Public API inventory: bcl-async-concurrency"
          "bcl-language-errors", "Public API inventory: bcl-language-errors" ]

let private modes =
    Map.ofList
        [ "funny-sharp-core", "runtime"
          "funny-sharp-aspnetcore", "runtime"
          "funcky", "runtime"
          "funcky-analyzers", "metadata"
          "csharpfunctionalextensions", "runtime"
          "fsharp-core", "runtime"
          "language-ext", "metadata"
          "bcl-sequences-linq", "metadata"
          "bcl-collections-immutable", "metadata"
          "bcl-async-concurrency", "metadata"
          "bcl-language-errors", "metadata"
          TYPE_LIST_NAME, "metadata" ]

let private includes =
    Map.ofList
        [ "fsharp-core",
          [ @"^Microsoft\.FSharp\.(Core\.(FSharpOption|FSharpValueOption|FSharpResult|FSharpChoice|FSharpFunc|FSharpType|FSharpValue|Unit|OptionModule|ValueOptionModule|ResultModule|Choice|Nullable|LanguagePrimitives|Operators|ExtraTopLevelOperators|FuncConvert|OptimizedClosures|Lazy|MatchFailureException|Printf)|Collections\.(SeqModule|ListModule|ArrayModule|SetModule|MapModule|FSharpList|FSharpSet|FSharpMap|ResizeArray|Seq)|Control\.(FSharpAsync|FSharpMailboxProcessor|TaskBuilder|EventModule|FSharpEvent|IEvent|LazyExtensions))" ]
          "language-ext", [ @"^LanguageExt\.[A-Za-z]+$" ]
          "bcl-sequences-linq",
          [ @"^System\.Linq\.(Enumerable|AsyncEnumerable|Lookup|IGrouping|IOrderedEnumerable|OrderedEnumerable)"
            @"^System\.Collections\.Generic\.(IEnumerable|IAsyncEnumerable|IReadOnlyList|IReadOnlyCollection|IReadOnlyDictionary|IList|IDictionary|List|Dictionary|HashSet|SortedSet|SortedDictionary|KeyValuePair|IEqualityComparer|IEnumerator|IAsyncEnumerator)"
            @"^System\.Span|^System\.ReadOnlySpan|^System\.Memory$|^System\.ReadOnlyMemory|^System\.MemoryExtensions|^System\.Array$|^System\.ArraySegment|^System\.Buffers\." ]
          "bcl-collections-immutable",
          [ @"^System\.Collections\.Immutable\.|^System\.Collections\.Frozen\.|^System\.Collections\.Concurrent\.|^System\.Collections\.ObjectModel\." ]
          "bcl-async-concurrency",
          [ @"^System\.Threading\.Tasks\.(Task|ValueTask|TaskCompletionSource|TaskFactory|TaskScheduler|Parallel|ParallelOptions|TaskCreationOptions|TaskContinuationOptions|TaskStatus|TaskCanceledException|ValueTaskSourceStatus|IValueTaskSource|TaskExtensions|TaskAsyncEnumerableExtensions|ParallelEnumerable)"
            @"^System\.Threading\.(Channels\.|CancellationToken|CancellationTokenSource|CancellationTokenRegistration|TimeProvider|ITimer|Timer|PeriodicTimer|Lock|Interlocked|Volatile|LazyInitializer|Timeout|WaitHandle|ManualResetEventSlim|SemaphoreSlim|CountdownEvent|Barrier|ReaderWriterLockSlim)"
            @"^System\.(IAsyncDisposable|IDisposable|IAsyncEnumerable|TimeProvider|TimeoutException|OperationCanceledException)"
            @"^System\.Runtime\.CompilerServices\.(AsyncTaskMethodBuilder|AsyncValueTaskMethodBuilder|ConfiguredTaskAwaitable|ConfiguredValueTaskAwaitable|IAsyncStateMachine|AsyncIteratorMethodBuilder|TaskAwaiter|ValueTaskAwaiter|PoolingAsyncValueTaskMethodBuilder|EnumeratorCancellationAttribute|AsyncMethodBuilderAttribute|INotifyCompletion|ICriticalNotifyCompletion)" ]
          "bcl-language-errors",
          [ @"^System\.(Nullable|Nullable`1|Func|Action|Predicate|Comparison|Converter|Lazy|Tuple|ValueTuple|Exception|AggregateException|SystemException|InvalidOperationException|ArgumentNullException|ArgumentException|ArgumentOutOfRangeException|OperationCanceledException|TimeoutException|ObjectDisposedException|NotSupportedException|NotImplementedException|FormatException|Environment|Math|Convert|String|StringComparison|DateTime|DateTimeOffset|TimeSpan|Guid|Uri|Random|Version|IEquatable|IComparable|IFormattable|ISpanFormattable|Comparison`1)"
            @"^System\.Diagnostics\.CodeAnalysis\.(MaybeNull|NotNull|AllowNull|DisallowNull|MaybeNullWhen|NotNullWhen|NotNullIfNotNull|MemberNotNull|DoesNotReturn|DoesNotReturnIf|SetsRequiredMembers|StringSyntax)"
            @"^System\.Runtime\.CompilerServices\.(NullableAttribute|NullableContextAttribute|IsReadOnlyAttribute|IsByRefLikeAttribute|RequiredMemberAttribute|CompilerFeatureRequiredAttribute)" ] ]

// ---- helpers ---------------------------------------------------------------

let private utf8 (text: string) : byte array = Encoding.UTF8.GetBytes text

let private bom = [| 0xEFuy; 0xBBuy; 0xBFuy |]

let private withBom (text: string) : byte array = Array.append bom (utf8 text)

let private pathVariable () : string =
    match Environment.GetEnvironmentVariable "PATH" with
    | null -> ""
    | value -> value

let private fileNameOf (path: string) : string =
    match Path.GetFileName path with
    | null -> path
    | name -> name

let private touch (path: string) : unit =
    match Path.GetDirectoryName path with
    | null -> ()
    | directory when directory = "" -> ()
    | directory -> Directory.CreateDirectory directory |> ignore

    File.WriteAllBytes(path, utf8 "stub")

/// Resolve symlinks in every existing component (mirrors os.path.realpath
/// closely enough for the symlink-free temp fixtures these tests need).
let private realPath (path: string) : string =
    let full = Path.GetFullPath path

    // Null-pattern matching narrows `root`; the `isNull root || root = ""`
    // form does not narrow under F# 10 nullness.
    match Path.GetPathRoot full with
    | null -> full
    | root when root = "" -> full
    | root ->
        let parts =
            full.Substring(root.Length)
                .Split(
                    [| Path.DirectorySeparatorChar; Path.AltDirectorySeparatorChar |],
                    StringSplitOptions.RemoveEmptyEntries
                )

        let mutable current = root

        for part in parts do
            current <- Path.Combine(current, part)

            try
                if Directory.Exists current then
                    match (DirectoryInfo current).ResolveLinkTarget true with
                    | null -> ()
                    | target -> current <- target.FullName
                elif File.Exists current then
                    match (FileInfo current).ResolveLinkTarget true with
                    | null -> ()
                    | target -> current <- target.FullName
            with _ ->
                ()

        current

let private repoRelative (relativePath: string) : string =
    Path.Combine(repositoryRoot (), relativePath)

let private argAfter (flag: string) (argv: string list) : string =
    argv |> List.skipWhile ((<>) flag) |> List.tail |> List.head

let private fakeTargetName (argv: string list) : string =
    if List.contains "--list-types" argv then
        TYPE_LIST_NAME
    else
        let name = fileNameOf (argAfter "--out-md" argv)
        name.Substring(4, name.Length - 4 - 3)

/// Records calls, writes dumper-like CRLF/BOM output, and never executes dotnet.
type private FakeDotnet(?failBuild: bool, ?failTargets: string list) =
    let failBuild = defaultArg failBuild false
    let failTargets = defaultArg failTargets []
    let calls = ResizeArray<string list * Map<string, string>>()

    member _.Calls = List.ofSeq calls

    member _.Runner : CommandRunner =
        fun argv env ->
            calls.Add(argv, env)

            if argv.[1] = "build" then
                { ExitCode = (if failBuild then 1 else 0)
                  Stdout = utf8 "build stdout\r\n"
                  Stderr = utf8 "build stderr\r\n" }
            else
                let name = fakeTargetName argv

                if List.contains "--out-md" argv then
                    File.WriteAllBytes(
                        argAfter "--out-md" argv,
                        withBom "# title\r\n\r\nType count: 1\r\n"
                    )

                    File.WriteAllBytes(argAfter "--out-json" argv, withBom "{\r\n  \"title\": \"x\"\r\n}\r\n")

                if List.contains name failTargets then
                    { ExitCode = 1
                      Stdout = utf8 "boom\r\nsecond\r\nthird\r\nfourth\r\nfifth\r\nsixth\r\n"
                      Stderr = utf8 "warn\r\n" }
                else
                    { ExitCode = 0
                      Stdout = utf8 "wrote file\r\nsecond line\r\n"
                      Stderr = utf8 "warn\r\n" }

type private Fixture() =
    let temporary = new TempDirectory()
    let temp = realPath temporary.Path
    let baseline = Path.Combine(temp, "baselines")
    let refDir = Path.Combine(temp, "ref", "net10.0")
    let fsbin = Path.Combine(temp, "fsbin")
    let aspnetbin = Path.Combine(temp, "aspnetbin")
    let dotnetRoot = Path.Combine(temp, "dotnet")

    do
        for rel in baselineFiles do
            touch (Path.Combine(baseline, rel))

        for rel in refFiles do
            touch (Path.Combine(refDir, rel))

        touch (Path.Combine(fsbin, "FunnySharp.dll"))
        touch (Path.Combine(aspnetbin, "FunnySharp.AspNetCore.dll"))

        Directory.CreateDirectory(
            Path.Combine(dotnetRoot, "shared", "Microsoft.AspNetCore.App", "10.0.9")
        )
        |> ignore

        Directory.CreateDirectory(
            Path.Combine(dotnetRoot, "shared", "Microsoft.AspNetCore.App", "10.0.11")
        )
        |> ignore

        Directory.CreateDirectory(Path.Combine(dotnetRoot, "sdk", "10.0.400", "Roslyn", "bincore"))
        |> ignore

    member _.Temp = temp
    member _.Baseline = baseline
    member _.Ref = refDir
    member _.Fsbin = fsbin
    member _.Aspnetbin = aspnetbin
    member _.DotnetRoot = dotnetRoot

    member _.Env =
        Map.ofList
            [ "HOME", Path.Combine(temp, "home")
              "DOTNET_ROOT", dotnetRoot
              "FUNNY_SHARP_BIN", fsbin
              "FUNNY_SHARP_ASPNET_BIN", aspnetbin
              "PATH", pathVariable () ]

    member this.Inputs() =
        resolveInputs (repositoryRoot ()) this.Env (Some baseline) (Some refDir)

    member this.GenerationArgv(outputDir: string) =
        [ "--baseline-root"
          baseline
          "--ref-pack-dir"
          refDir
          "--output-dir"
          outputDir ]

    interface IDisposable with
        member _.Dispose() = (temporary :> IDisposable).Dispose()

let private runCli
    (runner: CommandRunner)
    (env: Map<string, string>)
    (argv: string list)
    : int * string * string =
    use stdout = new StringWriter()
    use stderr = new StringWriter()
    let exitCode = mainWith stdout stderr env runner argv
    exitCode, stdout.ToString(), stderr.ToString()

let private allMissingEnv (fixture: Fixture) : Map<string, string> =
    Map.ofList
        [ "HOME", Path.Combine(fixture.Temp, "home")
          "DOTNET_ROOT", Path.Combine(fixture.Temp, "missing-dotnet")
          "FUNNY_SHARP_BIN", Path.Combine(fixture.Temp, "missing-fsbin")
          "FUNNY_SHARP_ASPNET_BIN", Path.Combine(fixture.Temp, "missing-aspnetbin")
          "PATH", pathVariable () ]

// ---- structural target table ----------------------------------------------

type TargetTableTests() =

    [<Fact>]
    member _.TargetNamesAndOrder() =
        Assert.Equal<string list>(names, TARGETS |> List.map (fun target -> target.Name))

    [<Fact>]
    member _.ElevenGenerationTargetsPlusTypeList() =
        Assert.Equal(12, TARGETS.Length)

        Assert.Equal(
            11,
            TARGETS |> List.filter (fun target -> not target.ListTypes) |> List.length
        )

        Assert.Equal<string list>(
            [ TYPE_LIST_NAME ],
            TARGETS |> List.filter (fun target -> target.ListTypes) |> List.map (fun target -> target.Name)
        )

    [<Fact>]
    member _.TitlesMatchLegacyInvocations() =
        let expected =
            names
            |> List.map (fun name ->
                name, (if name = TYPE_LIST_NAME then "" else titles.[name]))

        Assert.Equal<(string * string) list>(
            expected,
            TARGETS |> List.map (fun target -> target.Name, target.Title)
        )

    [<Fact>]
    member _.ModesMatchLegacyInvocations() =
        Assert.Equal<(string * string) list>(
            names |> List.map (fun name -> name, modes.[name]),
            TARGETS |> List.map (fun target -> target.Name, target.Mode)
        )

    [<Fact>]
    member _.IncludeFiltersMatchLegacyInvocations() =
        for target in TARGETS do
            Assert.Equal<string list>(
                Map.tryFind target.Name includes |> Option.defaultValue [],
                target.Includes
            )

    [<Fact>]
    member _.TargetArgvMatchesLegacyInvocations() =
        use fixture = new Fixture()
        let inputs = fixture.Inputs()
        let out = Path.Combine(fixture.Temp, "out")
        let j (parts: string list) = Path.Combine(List.toArray parts)
        let cp = repoRelative (Path.Combine("eng", "next-stage-inventory", "api-inventory.csproj"))
        let fs = fixture.Fsbin
        let asp = fixture.Aspnetbin
        let baseline = fixture.Baseline
        let refDir = fixture.Ref
        let rosl = Path.Combine(fixture.DotnetRoot, "sdk", "10.0.400", "Roslyn", "bincore")
        let af = Path.Combine(fixture.DotnetRoot, "shared", "Microsoft.AspNetCore.App", "10.0.11")

        let head () =
            [ "dotnet"; "run"; "-c"; "Release"; "--no-build"; "--project"; cp; "--" ]

        let outs name =
            [ "--out-md"
              j [ out; sprintf "inv-%s.md" name ]
              "--out-json"
              j [ out; sprintf "inv-%s.json" name ]
              "--title"
              titles.[name] ]

        let inc name =
            Map.tryFind name includes
            |> Option.defaultValue []
            |> List.collect (fun pattern -> [ "--include"; pattern ])

        let expected =
            [ "funny-sharp-core",
              head () @ [ "--assembly"; j [ fs; "FunnySharp.dll" ] ] @ outs "funny-sharp-core"
              "funny-sharp-aspnetcore",
              head ()
              @ [ "--assembly"; j [ asp; "FunnySharp.AspNetCore.dll" ] ]
              @ [ "--resolve-dir"; fs; "--resolve-dir"; af ]
              @ outs "funny-sharp-aspnetcore"
              "funcky",
              head ()
              @ [ "--assembly"; j [ baseline; "funcky.3.6.0"; "lib"; "net10.0"; "Funcky.dll" ] ]
              @ outs "funcky"
              "funcky-analyzers",
              head ()
              @ [ "--mode"
                  "metadata"
                  "--core-dir"
                  refDir
                  "--resolve-dir"
                  rosl
                  "--assembly"
                  j [ baseline
                      "funcky.3.6.0"
                      "analyzers"
                      "dotnet"
                      "cs"
                      "Funcky.BuiltinAnalyzers.dll" ] ]
              @ outs "funcky-analyzers"
              "csharpfunctionalextensions",
              head ()
              @ [ "--assembly"
                  j [ baseline
                      "csharpfunctionalextensions.3.7.0"
                      "lib"
                      "net8.0"
                      "CSharpFunctionalExtensions.dll" ] ]
              @ outs "csharpfunctionalextensions"
              "fsharp-core",
              head ()
              @ [ "--assembly"
                  j [ baseline; "fsharp.core.10.1.401"; "lib"; "netstandard2.1"; "FSharp.Core.dll" ] ]
              @ inc "fsharp-core"
              @ outs "fsharp-core"
              "language-ext",
              head ()
              @ [ "--mode"
                  "metadata"
                  "--core-dir"
                  refDir
                  "--assembly"
                  j [ baseline
                      "languageext.core.4.4.9"
                      "lib"
                      "netstandard2.0"
                      "LanguageExt.Core.dll" ] ]
              @ inc "language-ext"
              @ outs "language-ext"
              "bcl-sequences-linq",
              head ()
              @ [ "--mode"; "metadata"; "--core-dir"; refDir ]
              @ [ "--assembly"; j [ refDir; "System.Linq.dll" ]
                  "--assembly"; j [ refDir; "System.Linq.AsyncEnumerable.dll" ]
                  "--assembly"; j [ refDir; "System.Runtime.dll" ]
                  "--assembly"; j [ refDir; "System.Collections.dll" ]
                  "--assembly"; j [ refDir; "System.Memory.dll" ]
                  "--assembly"; j [ refDir; "System.Buffers.dll" ]
                  "--assembly"; j [ refDir; "System.Threading.Tasks.Extensions.dll" ] ]
              @ inc "bcl-sequences-linq"
              @ outs "bcl-sequences-linq"
              "bcl-collections-immutable",
              head ()
              @ [ "--mode"; "metadata"; "--core-dir"; refDir ]
              @ [ "--assembly"; j [ refDir; "System.Collections.Immutable.dll" ]
                  "--assembly"; j [ refDir; "System.Collections.dll" ]
                  "--assembly"; j [ refDir; "System.Collections.Concurrent.dll" ]
                  "--assembly"; j [ refDir; "System.Linq.dll" ] ]
              @ inc "bcl-collections-immutable"
              @ outs "bcl-collections-immutable"
              "bcl-async-concurrency",
              head ()
              @ [ "--mode"; "metadata"; "--core-dir"; refDir ]
              @ [ "--assembly"; j [ refDir; "System.Runtime.dll" ]
                  "--assembly"; j [ refDir; "System.Threading.Channels.dll" ]
                  "--assembly"; j [ refDir; "System.Threading.Tasks.Parallel.dll" ]
                  "--assembly"; j [ refDir; "System.Threading.Tasks.dll" ]
                  "--assembly"; j [ refDir; "System.Threading.dll" ] ]
              @ inc "bcl-async-concurrency"
              @ outs "bcl-async-concurrency"
              "bcl-language-errors",
              head ()
              @ [ "--mode"; "metadata"; "--core-dir"; refDir ]
              @ [ "--assembly"; j [ refDir; "System.Runtime.dll" ]
                  "--assembly"; j [ refDir; "System.Runtime.Extensions.dll" ]
                  "--assembly"; j [ refDir; "System.Linq.Expressions.dll" ] ]
              @ inc "bcl-language-errors"
              @ outs "bcl-language-errors"
              TYPE_LIST_NAME,
              head ()
              @ [ "--mode"
                  "metadata"
                  "--core-dir"
                  refDir
                  "--assembly"
                  j [ baseline
                      "languageext.core.4.4.9"
                      "lib"
                      "netstandard2.0"
                      "LanguageExt.Core.dll" ] ]
              @ [ "--list-types" ] ]

        let actual =
            TARGETS |> List.map (fun target -> target.Name, targetArgv target inputs out)

        Assert.Equal<(string * string list) list>(expected, actual)

// ---- environment resolution ------------------------------------------------

type EnvironmentResolutionTests() =

    [<Fact>]
    member _.RequiredInputsAllPresentWhenStaged() =
        use fixture = new Fixture()
        Assert.Empty(missingInputs (fixture.Inputs()))

    [<Fact>]
    member _.DotnetRootDefaultsToHomeDotnet() =
        use fixture = new Fixture()

        let env =
            fixture.Env |> Map.remove "DOTNET_ROOT"

        let inputs =
            resolveInputs (repositoryRoot ()) env (Some fixture.Baseline) (Some fixture.Ref)

        Assert.Equal(Path.Combine(fixture.Env.["HOME"], ".dotnet"), inputs.DotnetRoot)

    [<Fact>]
    member _.FunnySharpBinsDefaultToRepoPaths() =
        use fixture = new Fixture()

        let env =
            fixture.Env |> Map.remove "FUNNY_SHARP_BIN" |> Map.remove "FUNNY_SHARP_ASPNET_BIN"

        let inputs =
            resolveInputs (repositoryRoot ()) env (Some fixture.Baseline) (Some fixture.Ref)

        Assert.Equal(
            repoRelative (Path.Combine("src", "FunnySharp", "bin", "Release", "net10.0")),
            inputs.FunnySharpBin
        )

        Assert.Equal(
            repoRelative (
                Path.Combine("src", "FunnySharp.AspNetCore", "bin", "Release", "net10.0")
            ),
            inputs.FunnySharpAspnetBin
        )

    [<Fact>]
    member _.RoslynDefaultPrefersGlobalJsonPin() =
        use fixture = new Fixture()

        Directory.CreateDirectory(
            Path.Combine(fixture.DotnetRoot, "sdk", "10.0.500", "Roslyn", "bincore")
        )
        |> ignore

        Assert.Equal(
            Some(Path.Combine(fixture.DotnetRoot, "sdk", "10.0.400", "Roslyn", "bincore")),
            (fixture.Inputs()).RoslynDir
        )

    [<Fact>]
    member _.RoslynDefaultFallsBackToNewestSdk() =
        use fixture = new Fixture()
        let other = Path.Combine(fixture.Temp, "other-dotnet")

        Directory.CreateDirectory(Path.Combine(other, "sdk", "10.0.700", "Roslyn", "bincore"))
        |> ignore

        Directory.CreateDirectory(Path.Combine(other, "sdk", "10.0.800", "Roslyn", "bincore"))
        |> ignore

        let env = fixture.Env |> Map.add "DOTNET_ROOT" other

        let inputs =
            resolveInputs (repositoryRoot ()) env (Some fixture.Baseline) (Some fixture.Ref)

        Assert.Equal(
            Some(Path.Combine(other, "sdk", "10.0.800", "Roslyn", "bincore")),
            inputs.RoslynDir
        )

    [<Fact>]
    member _.RoslynEnvOverrideWins() =
        use fixture = new Fixture()
        let overrideDir = Path.Combine(fixture.Temp, "roslyn-override")
        let env = fixture.Env |> Map.add "ROSLYN_DIR" overrideDir

        let inputs =
            resolveInputs (repositoryRoot ()) env (Some fixture.Baseline) (Some fixture.Ref)

        Assert.Equal(Some overrideDir, inputs.RoslynDir)

    [<Fact>]
    member _.AspNetFrameworkUsesVersionSortNotLexicographic() =
        // "10.0.9" sorts after "10.0.11" as text; version sort must pick 10.0.11.
        use fixture = new Fixture()

        Assert.Equal(
            Some(Path.Combine(fixture.DotnetRoot, "shared", "Microsoft.AspNetCore.App", "10.0.11")),
            (fixture.Inputs()).AspnetFrameworkDir
        )

    [<Fact>]
    member _.AspNetFrameworkEnvOverrideWins() =
        use fixture = new Fixture()
        let overrideDir = Path.Combine(fixture.Temp, "aspnet-override")
        let env = fixture.Env |> Map.add "ASPNET_FRAMEWORK_DIR" overrideDir

        let inputs =
            resolveInputs (repositoryRoot ()) env (Some fixture.Baseline) (Some fixture.Ref)

        Assert.Equal(Some overrideDir, inputs.AspnetFrameworkDir)

// ---- input preflight and report-only mode ----------------------------------

type InputPreflightTests() =

    [<Fact>]
    member _.MissingInputsListsEveryPathAndExit2() =
        use fixture = new Fixture()
        let missingBaseline = Path.Combine(fixture.Temp, "missing-baselines")
        let missingRef = Path.Combine(fixture.Temp, "missing-ref")
        let missingFsbin = Path.Combine(fixture.Temp, "missing-fsbin")
        let missingAspnetbin = Path.Combine(fixture.Temp, "missing-aspnetbin")
        let out = Path.Combine(fixture.Temp, "out-not-created")
        let runner = FakeDotnet()
        let env = allMissingEnv fixture

        let code, stdout, stderr =
            runCli
                runner.Runner
                env
                [ "--baseline-root"
                  missingBaseline
                  "--ref-pack-dir"
                  missingRef
                  "--output-dir"
                  out ]

        Assert.Equal(2, code)
        Assert.Empty runner.Calls
        Assert.False(Directory.Exists out)

        let expected =
            Set.ofList (
                [ for rel in baselineFiles -> Path.Combine(missingBaseline, rel.Replace('/', Path.DirectorySeparatorChar)) ]
                @ [ for rel in refFiles -> Path.Combine(missingRef, rel.Replace('/', Path.DirectorySeparatorChar)) ]
                @ [ missingRef
                    missingFsbin
                    Path.Combine(missingFsbin, "FunnySharp.dll")
                    Path.Combine(missingAspnetbin, "FunnySharp.AspNetCore.dll")
                    "<DOTNET_ROOT>/sdk/<version>/Roslyn/bincore"
                    "<DOTNET_ROOT>/shared/Microsoft.AspNetCore.App/<version>" ]
            )

        let reported =
            Regex.Matches(stdout + stderr, @"^MISSING: (.+?) \(", RegexOptions.Multiline)
            |> Seq.cast<Match>
            |> Seq.map (fun matched -> matched.Groups.[1].Value)
            |> Set.ofSeq

        Assert.Equal<Set<string>>(expected, reported)
        Assert.Equal(26, reported.Count)
        Assert.Contains("docs/next-stage/baselines.md", stderr)

    [<Fact>]
    member _.CheckInputsReportsWithoutBuildingOrWriting() =
        use fixture = new Fixture()
        let out = Path.Combine(fixture.Temp, "out-check")
        let runner = FakeDotnet(failBuild = true)

        let code, stdout, stderr =
            runCli runner.Runner fixture.Env (fixture.GenerationArgv out @ [ "--check-inputs" ])

        Assert.Equal(0, code)
        Assert.Contains("INPUT CHECK OK: all", stdout)
        Assert.Equal("", stderr)
        Assert.Empty runner.Calls
        Assert.False(Directory.Exists out)

    [<Fact>]
    member _.CheckInputsWithMissingInputsExits2() =
        use fixture = new Fixture()
        let out = Path.Combine(fixture.Temp, "out-check-missing")
        let runner = FakeDotnet()

        let code, stdout, stderr =
            runCli
                runner.Runner
                (allMissingEnv fixture)
                [ "--baseline-root"
                  Path.Combine(fixture.Temp, "missing-baselines")
                  "--ref-pack-dir"
                  Path.Combine(fixture.Temp, "missing-ref")
                  "--output-dir"
                  out
                  "--check-inputs" ]

        Assert.Equal(2, code)
        Assert.Contains("MISSING:", stdout)
        Assert.Contains("INPUT CHECK FAILED", stderr)
        Assert.Contains("docs/next-stage/baselines.md", stderr)
        Assert.Empty runner.Calls
        Assert.False(Directory.Exists out)

    [<Fact>]
    member _.CheckInputsWithoutPositionalsUsesPlaceholders() =
        use fixture = new Fixture()
        let runner = FakeDotnet()

        let code, stdout, _ =
            runCli runner.Runner (allMissingEnv fixture) [ "--check-inputs" ]

        Assert.Equal(2, code)
        Assert.Contains("<baseline-root>/funcky.3.6.0/lib/net10.0/Funcky.dll", stdout)
        Assert.Contains("<ref-pack-dir>/System.Linq.dll", stdout)
        Assert.Empty runner.Calls

// ---- build, target aggregation and success path ----------------------------

type GenerationTests() =

    [<Fact>]
    member _.BuildFailureExitsImmediately() =
        use fixture = new Fixture()
        let out = Path.Combine(fixture.Temp, "out-buildfail")
        let runner = FakeDotnet(failBuild = true)
        let code, stdout, stderr = runCli runner.Runner fixture.Env (fixture.GenerationArgv out)
        Assert.Equal(1, code)
        Assert.Contains("FAILED: inventory tool build", stderr)
        Assert.True(Directory.Exists out)
        Assert.Empty(Directory.GetFileSystemEntries out)
        Assert.Single runner.Calls |> ignore

        let argv, env = runner.Calls.Head

        let expected =
            [ "dotnet"
              "build"
              repoRelative (Path.Combine("eng", "next-stage-inventory", "api-inventory.csproj"))
              "-c"
              "Release"
              "--nologo" ]

        Assert.Equal<string list>(expected, argv)
        Assert.Equal(fixture.DotnetRoot, env.["DOTNET_ROOT"])
        Assert.StartsWith(fixture.DotnetRoot + string Path.PathSeparator, env.["PATH"])
        Assert.Equal("1", env.["DOTNET_CLI_TELEMETRY_OPTOUT"])
        Assert.DoesNotContain("== funny-sharp-core", stdout)

    [<Fact>]
    member _.TargetFailureAggregatesAndReportsAllTargets() =
        use fixture = new Fixture()
        let out = Path.Combine(fixture.Temp, "out-targetfail")
        let runner = FakeDotnet(failTargets = [ "funcky" ])
        let code, stdout, stderr = runCli runner.Runner fixture.Env (fixture.GenerationArgv out)
        Assert.Equal(1, code)
        Assert.Equal(13, runner.Calls.Length)
        Assert.Contains("FAILED: funcky", stdout)
        Assert.Contains("== bcl-language-errors", stdout)
        Assert.Contains("== " + TYPE_LIST_NAME, stdout)

        Assert.Contains(
            sprintf "FAILED: one or more inventory targets failed; output in %s is incomplete" out,
            stderr
        )

        Assert.DoesNotContain("ALL DONE", stdout)

    [<Fact>]
    member _.SuccessPrintsAllDoneAndNormalizesOutputs() =
        use fixture = new Fixture()
        let out = Path.Combine(fixture.Temp, "out-success")
        let runner = FakeDotnet()
        let code, stdout, stderr = runCli runner.Runner fixture.Env (fixture.GenerationArgv out)
        Assert.Equal(0, code)
        Assert.Contains("ALL DONE", stdout)
        Assert.Equal("", stderr)
        Assert.Equal(13, runner.Calls.Length)

        let names =
            Directory.GetFileSystemEntries out |> Array.map fileNameOf |> Array.sort

        Assert.Equal(35, names.Length)
        Assert.Equal(11, names |> Array.filter (fun name -> name.EndsWith ".md") |> Array.length)
        Assert.Equal(11, names |> Array.filter (fun name -> name.EndsWith ".json") |> Array.length)
        Assert.Equal(11, names |> Array.filter (fun name -> name.EndsWith ".log") |> Array.length)

        for name in names do
            let data = File.ReadAllBytes(Path.Combine(out, name))
            Assert.False(data.Length >= 3 && data.[0] = 0xEFuy && data.[1] = 0xBBuy && data.[2] = 0xBFuy, name)
            Assert.False(Array.contains 0x0Duy data, name)

        let log = File.ReadAllBytes(Path.Combine(out, "inv-funcky.log"))
        let logText = Encoding.UTF8.GetString log
        Assert.Contains("wrote file\nsecond line\n", logText)
        Assert.Contains("warn\n", logText)
        let md = File.ReadAllBytes(Path.Combine(out, "inv-funcky.md"))
        Assert.StartsWith("# title\n\nType count: 1\n", Encoding.UTF8.GetString md)

// ---- output directory safety -----------------------------------------------

type OutputDirectoryTests() =

    [<Fact>]
    member _.RejectsRepositoryRootOutput() =
        use fixture = new Fixture()
        let runner = FakeDotnet()

        let code, _, stderr =
            runCli
                runner.Runner
                fixture.Env
                [ "--baseline-root"
                  fixture.Baseline
                  "--ref-pack-dir"
                  fixture.Ref
                  "--output-dir"
                  repositoryRoot () ]

        Assert.Equal(2, code)
        Assert.Contains("repository root", stderr)
        Assert.Contains("Remediation:", stderr)
        Assert.DoesNotContain("baselines.md", stderr)
        Assert.Empty runner.Calls

    [<Fact>]
    member _.RejectsSymlinkOutput() =
        use fixture = new Fixture()
        let real = Path.Combine(fixture.Temp, "real-out")
        Directory.CreateDirectory real |> ignore
        let link = Path.Combine(fixture.Temp, "link-out")

        try
            Directory.CreateSymbolicLink(link, real) |> ignore
        with ex ->
            Assert.Skip(sprintf "cannot create symlink: %s" ex.Message)

        let runner = FakeDotnet()

        for target in [ link; Path.Combine(link, "sub") ] do
            let code, _, stderr =
                runCli
                    runner.Runner
                    fixture.Env
                    [ "--baseline-root"
                      fixture.Baseline
                      "--ref-pack-dir"
                      fixture.Ref
                      "--output-dir"
                      target ]

            Assert.Equal(2, code)
            Assert.Contains("symlink or reparse point", stderr)

        Assert.Empty runner.Calls
        Assert.Empty(Directory.GetFileSystemEntries real)

    [<Fact>]
    member _.RejectsFileOutput() =
        use fixture = new Fixture()
        let filePath = Path.Combine(fixture.Temp, "not-a-dir")
        File.WriteAllBytes(filePath, utf8 "x")
        let runner = FakeDotnet()

        let code, _, stderr =
            runCli
                runner.Runner
                fixture.Env
                [ "--baseline-root"
                  fixture.Baseline
                  "--ref-pack-dir"
                  fixture.Ref
                  "--output-dir"
                  filePath ]

        Assert.Equal(2, code)
        Assert.Contains("not a directory", stderr)
        Assert.Empty runner.Calls

    [<Fact>]
    member _.UnlaunchableDotnetIsEnvironmentFailure() =
        use fixture = new Fixture()
        let out = Path.Combine(fixture.Temp, "out-unlaunchable")

        let raising: CommandRunner =
            fun _ _ -> raise (UnauthorizedAccessException "dotnet: Permission denied")

        let code, stdout, stderr = runCli raising fixture.Env (fixture.GenerationArgv out)
        Assert.Equal(2, code)
        Assert.Contains("cannot execute dotnet", stderr)
        Assert.Contains("global.json", stderr)
        Assert.DoesNotContain("Traceback", stdout + stderr)

    [<Fact>]
    member _.UnwritableOutputDirExits2WithoutTraceback() =
        use fixture = new Fixture()
        let out = Path.Combine(fixture.Temp, "out-unwritable")
        Directory.CreateDirectory out |> ignore

        // Occupy the first target's log path with a directory so the write fails
        // deterministically on every platform and UID: File.WriteAllBytes raises
        // UnauthorizedAccessException, which main() turns into exit 2 with the
        // remediation text.
        let first = TARGETS.Head
        Directory.CreateDirectory(Path.Combine(out, sprintf "inv-%s.log" first.Name)) |> ignore

        let runner = FakeDotnet()
        let code, stdout, stderr = runCli runner.Runner fixture.Env (fixture.GenerationArgv out)
        Assert.Equal(2, code)
        Assert.Contains("ERROR: cannot write inventory output", stderr)
        Assert.Contains("Remediation: choose a writable output directory", stderr)
        Assert.Contains(out, stderr)
        Assert.DoesNotContain("Traceback", stdout + stderr)

    [<Fact>]
    member _.DefaultOutputDirIsOutsideRepository() =
        let defaultDir = defaultOutputDir ()
        Assert.Equal("funnysharp-inventory", fileNameOf defaultDir)

        Assert.False(
            defaultDir.StartsWith(repositoryRoot () + string Path.DirectorySeparatorChar, StringComparison.Ordinal)
        )

// ---- LF / UTF-8 normalizer -------------------------------------------------

type NormalizerTests() =

    [<Fact>]
    member _.NormalizeBytesStripsBomAndCrlf() =
        Assert.Equal<byte array>(
            utf8 "a\nb\nc\n",
            normalizeBytes [| 0xEFuy; 0xBBuy; 0xBFuy; 0x61uy; 0x0Duy; 0x0Auy; 0x62uy; 0x0Duy; 0x63uy; 0x0Auy |]
        )

        Assert.Equal<byte array>(utf8 "plain\n", normalizeBytes (utf8 "plain\n"))
        Assert.Equal<byte array>([||], normalizeBytes [||])

    [<Fact>]
    member _.NormalizeFileRewritesBytes() =
        use fixture = new Fixture()
        let path = Path.Combine(fixture.Temp, "fixture.txt")
        File.WriteAllBytes(path, [| 0xEFuy; 0xBBuy; 0xBFuy; 0x66uy; 0x69uy; 0x72uy; 0x73uy; 0x74uy; 0x0Duy; 0x0Auy; 0x73uy; 0x65uy; 0x63uy; 0x6Fuy; 0x6Euy; 0x64uy; 0x0Duy; 0x0Auy |])
        Assert.True(normalizeFile path)
        Assert.Equal<byte array>(utf8 "first\nsecond\n", File.ReadAllBytes path)
        Assert.False(normalizeFile path)
