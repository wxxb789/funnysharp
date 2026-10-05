open System
open System.IO
open System.Diagnostics
open System.Text.Json
open System.Collections.Generic

let arguments = fsi.CommandLineArgs |> Array.skip 1
if arguments.Length <> 2 then failwith "Usage: dotnet fsi run-controls.fsx -- repository-root fixture-root"
let root = Path.GetFullPath arguments[0]
let fixtures = Path.GetFullPath arguments[1]
let packet = "docs/reviews/goal24-pr-20261004/traversal-r9-performance-evidence"
let records = ResizeArray<obj>()
let run (name: string) (script: string) (targetRoot: string) =
    let info = ProcessStartInfo("dotnet")
    info.WorkingDirectory <- root
    info.UseShellExecute <- false
    info.RedirectStandardOutput <- true
    info.RedirectStandardError <- true
    for argument in [ "fsi"; "--warnaserror+"; script; "--"; targetRoot ] do info.ArgumentList.Add argument
    use child = new Process()
    child.StartInfo <- info
    if not (child.Start()) then failwith "Unable to start data verifier"
    let stdout = child.StandardOutput.ReadToEndAsync()
    let stderr = child.StandardError.ReadToEndAsync()
    child.WaitForExit()
    let out = stdout.GetAwaiter().GetResult()
    let err = stderr.GetAwaiter().GetResult()
    records.Add(box {| name = name; executable = "dotnet"; arguments = info.ArgumentList |> Seq.toArray; exitCode = child.ExitCode; stdout = out; stderr = err |})
    child.ExitCode, out, err
let positiveExit, positiveOut, _ = run "landed-positive" (Path.Combine(root, packet, "verify-current.fsx")) root
let mutable valid = positiveExit = 0 && positiveOut.Contains("CURRENT_PERFORMANCE_PORTABLE_PASS", StringComparison.Ordinal)
if valid then
    for name, expectedError in [ "missing-object", "Could not find"; "tampered-catalog", "Catalog hash mismatch"; "tampered-physical-object", "Physical object hash mismatch" ] do
        let target = Path.Combine(fixtures, name)
        let exit, out, err = run name (Path.Combine(target, packet, "verify-current.fsx")) target
        valid <- valid && exit <> 0 && not (out.Contains("CURRENT_PERFORMANCE_PORTABLE_PASS", StringComparison.Ordinal)) && (out + err).Contains(expectedError, StringComparison.OrdinalIgnoreCase)
    let scanExit, scanOut, _ = run "credential-pattern-scan" (Path.Combine(root, packet, "scan-credentials.fsx")) root
    valid <- valid && scanExit = 0
    if scanExit = 0 then
        use scan = JsonDocument.Parse scanOut
        valid <- valid && scan.RootElement.GetProperty("objects").GetInt32() = 522 && scan.RootElement.GetProperty("decodedBytes").GetInt64() = 48750487L
let runnerExit = if valid then 0 else 1
printfn "%s" (JsonSerializer.Serialize({| schema = "funnysharp-portable-traverse-r9-controls/v1"; runnerExitCode = runnerExit; records = records.ToArray(); workloadReruns = 0; network = 0; dataVerifierWrites = 0 |}))
printfn "RETENTION_CONTROL_RUNNER_EXIT=%d" runnerExit
exit runnerExit
