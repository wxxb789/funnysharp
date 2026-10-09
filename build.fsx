open System
open System.Diagnostics
open System.IO

// The documented gate entrypoint (`dotnet fsi build.fsx -- -p <pipeline> [args]`,
// pinned by AGENTS.md, docs/harness.md and ProgramTests) forwards every argument
// to the compiled harness in eng/harness/Program.fs. The harness owns the real
// pipeline definitions; this shim exists only because release clean/build must
// not overwrite the running harness on Windows and the documented command shape
// must keep working unchanged.
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

// A hung child must fail the gate in bounded time, never hang it indefinitely:
// kill the whole process tree and report exit 124. The bound (2700s = 45min)
// matches the longest documented caller, the release workflow's win-x64 job
// (timeout-minutes: 45 in .github/workflows/release.yml), so a healthy full
// release run completes inside it while a wedged child still fails the gate.
let exitCode =
    use child = Process.Start startInfo

    if child.WaitForExit(2700_000) then
        child.ExitCode
    else
        (try
            child.Kill true
         with _ ->
            ())

        child.WaitForExit 5_000 |> ignore
        124

exit exitCode
