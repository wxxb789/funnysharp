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

// ---- Synchronous conveniences for fsi call sites ----

let runCaptureSync (exe: string) (args: string list) : ProcessResult =
    runCapture exe args |> Async.RunSynchronously

/// Run a command with its output streamed directly to the terminal.
let runIn (workingDirectory: string) (exe: string) (args: string list) : int =
    use proc = new Process()
    proc.StartInfo <- createStartInfo exe args (Some workingDirectory) false
    if not (proc.Start()) then
        invalidOp (sprintf "failed to start process '%s'" exe)
    proc.WaitForExit()
    proc.ExitCode
