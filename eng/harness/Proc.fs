module FunnySharp.Harness.Proc

// Process helpers over System.Diagnostics.Process. Every child is started with
// ArgumentList so no caller ever builds a shell string.

open System
open System.Diagnostics
open FunnySharp.Harness.Output

/// Captured process outcome: exit code plus the decoded stdout and stderr.
type ProcessResult =
    { ExitCode: int
      Stdout: string
      Stderr: string }

let private createStartInfo
    (exe: string)
    (args: string list)
    (workingDirectory: string option)
    (redirect: bool)
    =
    let info = ProcessStartInfo(exe)
    info.UseShellExecute <- false
    info.RedirectStandardOutput <- redirect
    info.RedirectStandardError <- redirect
    workingDirectory |> Option.iter (fun dir -> info.WorkingDirectory <- dir)
    for arg in args do
        info.ArgumentList.Add arg
    info

/// Run a child process inheriting this process' stdin/stdout/stderr and return its
/// exit code. The child's output streams straight to the parent console.
let runIn (workingDirectory: string option) (exe: string) (args: string list) : Async<int> =
    async {
        use proc = new Process()
        proc.StartInfo <- createStartInfo exe args workingDirectory false
        if not (proc.Start()) then
            return raise (InvalidOperationException(sprintf "failed to start process '%s'" exe))
        do! proc.WaitForExitAsync() |> Async.AwaitTask
        return proc.ExitCode
    }

/// Inherit-stdio run in the current directory.
let run (exe: string) (args: string list) : Async<int> =
    runIn None exe args

/// Run a child process with stdout and stderr captured, returning them with the
/// exit code whatever that code is.
let runCaptureIn (workingDirectory: string option) (exe: string) (args: string list) : Async<ProcessResult> =
    async {
        use proc = new Process()
        proc.StartInfo <- createStartInfo exe args workingDirectory true
        if not (proc.Start()) then
            return raise (InvalidOperationException(sprintf "failed to start process '%s'" exe))
        let stdoutTask = proc.StandardOutput.ReadToEndAsync()
        let stderrTask = proc.StandardError.ReadToEndAsync()
        let! stdout = stdoutTask |> Async.AwaitTask
        let! stderr = stderrTask |> Async.AwaitTask
        do! proc.WaitForExitAsync() |> Async.AwaitTask
        return
            { ExitCode = proc.ExitCode
              Stdout = stdout
              Stderr = stderr }
    }

/// Capturing run in the current directory.
let runCapture (exe: string) (args: string list) : Async<ProcessResult> =
    runCaptureIn None exe args

/// Run and fail the railway when the exit code is not zero.
let runCheckedIn
    (workingDirectory: string option)
    (exe: string)
    (args: string list)
    : Async<Result<unit, HarnessError>> =
    async {
        let! exitCode = runIn workingDirectory exe args
        if exitCode = 0 then
            return Ok()
        else
            return Error(Errors.withExitCode exitCode (sprintf "process '%s' exited with code %d" exe exitCode))
    }

let runChecked (exe: string) (args: string list) : Async<Result<unit, HarnessError>> =
    runCheckedIn None exe args

/// Capture and fail the railway when the exit code is not zero; the stderr tail
/// becomes the error message.
let runCaptureCheckedIn
    (workingDirectory: string option)
    (exe: string)
    (args: string list)
    : Async<Result<ProcessResult, HarnessError>> =
    async {
        let! result = runCaptureIn workingDirectory exe args
        if result.ExitCode = 0 then
            return Ok result
        else
            let stderr = result.Stderr.Trim()
            let message =
                if stderr = "" then
                    sprintf "process '%s' exited with code %d" exe result.ExitCode
                else
                    sprintf "process '%s' exited with code %d: %s" exe result.ExitCode stderr
            return Error(Errors.withExitCode result.ExitCode message)
    }

let runCaptureChecked (exe: string) (args: string list) : Async<Result<ProcessResult, HarnessError>> =
    runCaptureCheckedIn None exe args

// ---- Synchronous conveniences for fsi call sites ----

let runSync (exe: string) (args: string list) : int =
    run exe args |> Async.RunSynchronously

let runCaptureSync (exe: string) (args: string list) : ProcessResult =
    runCapture exe args |> Async.RunSynchronously
