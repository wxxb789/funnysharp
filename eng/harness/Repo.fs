module FunnySharp.Harness.Repo

// Repository discovery and path helpers. The repository root is the nearest
// ancestor directory that holds FunnySharp.slnx.

open System
open System.IO
open FunnySharp.Harness.Output
open FunnySharp.Harness.Proc

[<Literal>]
let SolutionFileName = "FunnySharp.slnx"

/// Walk up from a directory until the solution marker is found.
let tryFindRootFrom (startDirectory: string) : string option =
    let rec loop (directory: string) =
        if File.Exists(Path.Combine(directory, SolutionFileName)) then
            Some directory
        else
            match Directory.GetParent directory with
            | null -> None
            | parent -> loop parent.FullName

    loop (Path.GetFullPath startDirectory)

/// Walk up from the current directory until the solution marker is found.
let tryFindRoot () : string option =
    tryFindRootFrom Environment.CurrentDirectory

let findRootFrom (startDirectory: string) : Result<string, HarnessError> =
    match tryFindRootFrom startDirectory with
    | Some root -> Ok root
    | None -> failError (sprintf "could not locate '%s' above '%s'" SolutionFileName startDirectory)

/// The repository root, or a failure before any work runs.
let repoRoot () : string =
    match tryFindRoot () with
    | Some root -> root
    | None -> failwithf "could not locate '%s' above '%s'" SolutionFileName Environment.CurrentDirectory

/// Join path segments.
let combine (parts: string list) : string =
    Path.Combine(List.toArray parts)

/// A path relative to the repository root.
let repoPath (relativePath: string) : string =
    Path.Combine(repoRoot (), relativePath)

/// The solution file path.
let solutionsPath () : string =
    repoPath SolutionFileName

/// git rev-parse --short "HEAD^{tree}", as a result step.
let treeHashResult () : Async<Result<string, HarnessError>> =
    async {
        let! result = runCapture "git" [ "rev-parse"; "--short"; "HEAD^{tree}" ]
        if result.ExitCode = 0 then
            return Ok(result.Stdout.Trim())
        else
            return
                Error(
                    Errors.withExitCode
                        result.ExitCode
                        (sprintf "git rev-parse failed: %s" (result.Stderr.Trim()))
                )
    }

/// git rev-parse --short "HEAD^{tree}", or a failure.
let treeHash () : string =
    match treeHashResult () |> Async.RunSynchronously with
    | Ok hash -> hash
    | Error err -> failwith err.Message
