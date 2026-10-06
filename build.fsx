open System
open System.Diagnostics
open System.IO

// Release clean/build must not overwrite the running harness on Windows.
let startInfo = ProcessStartInfo("dotnet")
startInfo.UseShellExecute <- false
startInfo.WorkingDirectory <- Environment.CurrentDirectory

for argument in
    [ "run"
      "--project"
      Path.Combine(__SOURCE_DIRECTORY__, "eng", "harness", "FunnySharp.Harness.fsproj")
      "--configuration"
      "Debug"
      "--no-launch-profile"
      "--verbosity"
      "quiet"
      "--property:RestoreLockedMode=true"
      "--" ] do
    startInfo.ArgumentList.Add argument

for argument in fsi.CommandLineArgs |> Array.skip 1 do
    startInfo.ArgumentList.Add argument

let exitCode =
    use child = Process.Start startInfo
    child.WaitForExit()
    child.ExitCode

exit exitCode
