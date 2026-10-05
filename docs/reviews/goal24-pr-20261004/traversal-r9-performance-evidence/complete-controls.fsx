open System
open System.IO
open System.Diagnostics
open System.Text.Json
open System.Security.Cryptography

let root = fsi.CommandLineArgs |> Array.skip 1 |> Array.exactlyOne |> Path.GetFullPath
let packet = "docs/reviews/goal24-pr-20261004/traversal-r9-performance-evidence"
let directory = Path.Combine(root, packet)
let hash (bytes: byte array) = bytes |> SHA256.HashData |> Convert.ToHexStringLower
let require condition message = if not condition then failwith message
let verifierBytes = File.ReadAllBytes(Path.Combine(directory, "verify-current.fsx"))
require (hash verifierBytes = "8b942a874e29d368824cb3e55a224d2e3bbb167ef8c394be19935d3a789da4b9") "Landed verifier changed after completed controls"
let red = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory, "scanner-red-01.json")))
let prior = red.RootElement.GetProperty("exactControlRecords").GetProperty("records").EnumerateArray() |> Seq.toArray
let controls = prior |> Array.filter (fun item -> item.GetProperty("name").GetString() <> "credential-pattern-scan")
require (controls.Length = 4) "Missing original real controls"
let positive = controls |> Array.find (fun item -> item.GetProperty("name").GetString() = "landed-positive")
require (positive.GetProperty("exitCode").GetInt32() = 0 && positive.GetProperty("stdout").GetString().Contains("CURRENT_PERFORMANCE_PORTABLE_PASS", StringComparison.Ordinal)) "Original positive did not pass"
for name, error in [ "missing-object", "Could not find"; "tampered-catalog", "Catalog hash mismatch"; "tampered-physical-object", "Physical object hash mismatch" ] do
    let item = controls |> Array.find (fun item -> item.GetProperty("name").GetString() = name)
    let out = item.GetProperty("stdout").GetString()
    let err = item.GetProperty("stderr").GetString()
    require (item.GetProperty("exitCode").GetInt32() <> 0 && not (out.Contains("CURRENT_PERFORMANCE_PORTABLE_PASS", StringComparison.Ordinal)) && (out + err).Contains(error, StringComparison.OrdinalIgnoreCase)) "Original negative did not reject intended mutation"
let info = ProcessStartInfo("dotnet")
info.WorkingDirectory <- root
info.UseShellExecute <- false
info.RedirectStandardOutput <- true
info.RedirectStandardError <- true
for argument in [ "fsi"; "--warnaserror+"; Path.Combine(directory, "scan-credentials.fsx"); "--"; root ] do info.ArgumentList.Add argument
let child = new Process()
child.StartInfo <- info
if not (child.Start()) then failwith "Unable to start corrected scanner"
let stdout = child.StandardOutput.ReadToEndAsync()
let stderr = child.StandardError.ReadToEndAsync()
child.WaitForExit()
let out = stdout.GetAwaiter().GetResult()
let err = stderr.GetAwaiter().GetResult()
let mutable valid = child.ExitCode = 0
if valid then
    use scan = JsonDocument.Parse out
    valid <- scan.RootElement.GetProperty("objects").GetInt32() = 522 && scan.RootElement.GetProperty("decodedBytes").GetInt64() = 48750487L && scan.RootElement.GetProperty("embeddedPayloads").GetInt32() >= 230
let exitCode = if valid then 0 else 1
printfn "%s" (JsonSerializer.Serialize({| schema = "funnysharp-portable-traverse-r9-controls/v1"; runnerExitCode = exitCode; controlRecords = controls; controlsRerun = false; correctedScan = {| name = "credential-pattern-scan"; executable = "dotnet"; arguments = info.ArgumentList |> Seq.toArray; exitCode = child.ExitCode; stdout = out; stderr = err |}; priorRunnerRed = "scanner-red-01.json"; verifiedSourceSha256 = hash verifierBytes; workloadReruns = 0; dataVerifierWrites = 0; network = 0 |}))
printfn "RETENTION_CONTROL_RUNNER_EXIT=%d" exitCode
exit exitCode
