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

/// The repository root, or a failure before any work runs.
let repoRoot () : string =
    match tryFindRoot () with
    | Some root -> root
    | None -> failwithf "could not locate '%s' above '%s'" SolutionFileName Environment.CurrentDirectory
