// Paired-cost measurement driver for the offline performance packets: runs the
// historical and successor verifiers over the same fixed inputs, records each
// child's exit code, timing and launcher cost as one JSON line, then checks the
// expected exit/verdict tuple. F# migration of measure-offline.ps1.
//
// usage: dotnet fsi measure-offline.fsx -- --repository-root <path> --baseline-root <path>
open System
open System.Diagnostics
open System.IO
open System.Text.Json

let arguments = fsi.CommandLineArgs |> Array.skip 1

let rec parse (args: string list) (named: Map<string, string>) =
    match args with
    | [] -> named
    | name :: value :: rest when name.StartsWith "--" -> parse rest (named.Add(name.[2..], value))
    | _ -> failwith "Usage: dotnet fsi measure-offline.fsx -- --repository-root <path> --baseline-root <path>"

let named = parse (List.ofArray arguments) Map.empty

let required name =
    match named.TryFind name with
    | Some value -> value
    | None -> failwithf "Missing required argument --%s" name

let repositoryRoot = required "repository-root"
let baselineRoot = required "baseline-root"
let here = __SOURCE_DIRECTORY__

let runs =
    [ "old-r9-first-observed",
      Path.Combine(baselineRoot, "docs/reviews/goal24-pr-20261004/traversal-r9-performance-evidence/verify-current.fsx"),
      baselineRoot
      "new-r9-first-observed", Path.Combine(here, "verify-traversal-r9-performance.fsx"), repositoryRoot
      "old-r9-warm",
      Path.Combine(baselineRoot, "docs/reviews/goal24-pr-20261004/traversal-r9-performance-evidence/verify-current.fsx"),
      baselineRoot
      "new-r9-warm", Path.Combine(here, "verify-traversal-r9-performance.fsx"), repositoryRoot
      "old-c-stale",
      Path.Combine(baselineRoot, "docs/reviews/goal24-pr-20261004/current-performance-evidence/verify-current.fsx"),
      baselineRoot
      "new-c-stale", Path.Combine(here, "verify-current-performance.fsx"), repositoryRoot
      "decoded-copy-controls", Path.Combine(here, "offline-packet-controls.fsx"), repositoryRoot ]

type RunRecord =
    { Name: string
      ExitCode: int
      Stdout: string
      Stderr: string }

let records = ResizeArray<RunRecord>()

for name, script, root in runs do
    let info = ProcessStartInfo("dotnet")
    info.UseShellExecute <- false
    info.RedirectStandardOutput <- true
    info.RedirectStandardError <- true

    for argument in [ "fsi"; "--warnaserror+"; script; "--"; root ] do
        info.ArgumentList.Add argument

    use child = new Process()
    child.StartInfo <- info
    let watch = Stopwatch.StartNew()

    if not (child.Start()) then
        failwith "Unable to start dotnet"

    let stdout = child.StandardOutput.ReadToEndAsync()
    let stderr = child.StandardError.ReadToEndAsync()
    child.WaitForExit() |> ignore
    watch.Stop()

    let record =
        {| name = name
           executable = "dotnet"
           arguments = [| for argument in info.ArgumentList -> argument |]
           exitCode = child.ExitCode
           elapsedMs = watch.Elapsed.TotalMilliseconds
           launcherCpuMs = child.TotalProcessorTime.TotalMilliseconds
           launcherPeakWorkingSetBytes = child.PeakWorkingSet64
           stdout = stdout.Result
           stderr = stderr.Result |}

    records.Add { Name = name; ExitCode = child.ExitCode; Stdout = record.stdout; Stderr = record.stderr }
    printfn "%s" (JsonSerializer.Serialize(record))

let mutable valid = true

for record in records do
    let ok =
        if record.Name.Contains "r9" then
            record.ExitCode = 0
            && record.Stdout.Contains "CURRENT_PERFORMANCE_PORTABLE_PASS objects=522 locators=547 rows=230 launches=230 census=499/468/31 runtime=24 writes=0 network=0"
        elif record.Name.Contains "c-stale" then
            record.ExitCode <> 0
            && record.Stderr.Contains "Current source/protocol bytes changed"
            && not (record.Stdout.Contains "CURRENT_PERFORMANCE_PORTABLE_PASS")
        else
            record.ExitCode = 0
            && record.Stdout.Contains "OFFLINE_PACKET_CONTROLS_PASS"

    valid <- valid && ok

if not valid then
    printfn "OFFLINE_PAIRED_COST_FAIL"
    exit 1

printfn "OFFLINE_PAIRED_COST_PASS"
