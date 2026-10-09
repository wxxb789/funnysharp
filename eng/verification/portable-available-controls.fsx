// Availability control driver for the portable history verifier: runs the
// historical and successor portable verifiers over the same raw archives, plus
// the successor's help and usage contract, then checks the expected exit tuple.
// F# migration of portable-available-controls.ps1.
//
// usage: dotnet fsi portable-available-controls.fsx -- --repository-root <path> --artifact-directory <path>
open System
open System.Diagnostics
open System.IO

let arguments = fsi.CommandLineArgs |> Array.skip 1

let rec parse (args: string list) (named: Map<string, string>) =
    match args with
    | [] -> named
    | name :: value :: rest when name.StartsWith "--" -> parse rest (named.Add(name.[2..], value))
    | _ -> failwith "Usage: dotnet fsi portable-available-controls.fsx -- --repository-root <path> --artifact-directory <path>"

let named = parse (List.ofArray arguments) Map.empty

let required name =
    match named.TryFind name with
    | Some value -> value
    | None -> failwithf "Missing required argument --%s" name

let repositoryRoot = required "repository-root"
let artifactDirectory = required "artifact-directory"

let oldVerifier = Path.Combine(repositoryRoot, "docs/audits/goal-24-resolution/portable-evidence/verify.fsx")
let newVerifier = Path.Combine(__SOURCE_DIRECTORY__, "verify-portable-history.fsx")

let run (arguments: string list) : int =
    let info = ProcessStartInfo("dotnet")
    info.UseShellExecute <- false

    for argument in "fsi" :: "--warnaserror+" :: arguments do
        info.ArgumentList.Add argument

    use child = new Process()
    child.StartInfo <- info

    if not (child.Start()) then
        failwith "Unable to start dotnet"

    child.WaitForExit() |> ignore
    child.ExitCode

let oldExit = run [ oldVerifier; "--"; "--repository-root"; repositoryRoot; "--artifact-directory"; artifactDirectory ]
printfn "OLD_PORTABLE_EXIT=%d" oldExit

let newExit =
    run [ newVerifier; "--"; "--repository-root"; repositoryRoot; "--artifact-directory"; artifactDirectory ]

printfn "NEW_PORTABLE_EXIT=%d" newExit

let helpExit = run [ newVerifier; "--"; "--help" ]
printfn "NEW_HELP_EXIT=%d" helpExit

let usageExit = run [ newVerifier; "--" ]
printfn "NEW_USAGE_EXIT=%d" usageExit

let expected = [ 0; 0; 0; 2 ]
let actual = [ oldExit; newExit; helpExit; usageExit ]

if actual <> expected then
    eprintfn "Portable verifier exit tuple mismatch: expected %A; actual %A" expected actual
    exit 1
