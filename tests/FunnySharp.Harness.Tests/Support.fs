module FunnySharp.Harness.Tests.Support

open System
open System.IO
open System.Text
open FunnySharp.Harness.Repo

let checkoutSha = "11d5960a326750d5838078e36cf38b85af677262"
let setupDotnetSha = "67a3573c9a986a3f9c594539f4ab511d57bb3ce9"
let setupUvSha = "bec219d24cd3e171d82865faccec33120bb574f4"

let private stepIndent = "      "
let private keyIndent = "        "
let private utf8NoBom = UTF8Encoding(false)

/// Build one workflow step at the indentation used by these fixtures.
let step (lines: string list) : string list =
    match lines with
    | [] -> []
    | first :: rest ->
        (stepIndent + "- " + first)
        :: (rest |> List.map (fun line -> keyIndent + line))

/// One step whose `uses:` value sits on the next, more-indented line.
let continuationStep (name: string) (value: string) : string list =
    [ stepIndent + "- name: " + name
      keyIndent + "uses:"
      keyIndent + "  " + value ]

/// A minimal valid workflow carrying the given step blocks.
let workflowText (steps: string list list) : string =
    let header =
        [ "name: fixture"
          "on:"
          "  pull_request:"
          "jobs:"
          "  fixture:"
          "    runs-on: ubuntu-latest"
          "    steps:" ]

    String.concat "\n" (header @ List.concat steps) + "\n"

let writeWorkflows (root: string) (workflows: (string * string) list) : unit =
    let directory = Path.Combine(root, ".github", "workflows")
    Directory.CreateDirectory directory |> ignore

    for name, text in workflows do
        File.WriteAllText(Path.Combine(directory, name), text, utf8NoBom)

let writeUvPin (root: string) : unit =
    File.WriteAllText(Path.Combine(root, "uv.toml"), "required-version = \"==0.12.16\"\n", utf8NoBom)

/// The file name of a path, with the path itself as the null fallback.
let fileName (path: string) : string =
    match Path.GetFileName path with
    | null -> path
    | name -> name

/// The 1-based line number of the first line containing the needle.
let lineOf (text: string) (needle: string) : int =
    text.Split('\n')
    |> Array.findIndex (fun line -> line.Contains needle)
    |> (+) 1

/// A disposable temporary directory.
type TempDirectory() =
    let path =
        Path.Combine(Path.GetTempPath(), "funnysharp-harness-" + Guid.NewGuid().ToString("N"))

    do Directory.CreateDirectory path |> ignore

    member _.Path = path

    interface IDisposable with
        member _.Dispose() =
            try
                Directory.Delete(path, true)
            with _ ->
                ()

/// The repository root, resolved from the test assembly location.
let repositoryRoot () : string =
    match tryFindRootFrom AppContext.BaseDirectory with
    | Some root -> root
    | None ->
        match tryFindRoot () with
        | Some root -> root
        | None -> failwith "FunnySharp.slnx was not found above the test assembly."
