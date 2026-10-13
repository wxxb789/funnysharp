module FunnySharp.Harness.Cli

open System
open FunnySharp.Harness

/// Build the native argument list without starting dotnet.
let nativeArguments (pipeline: string) (arguments: string list) : Result<string list, string> =
    let rec collect (target: string option) (forwarded: string list) (remaining: string list) =
        let setTarget (value: string) rest =
            if target.IsSome then Error "--project may be specified only once"
            elif String.IsNullOrWhiteSpace value || value.StartsWith "-" then
                Error "argument --project: expected one argument"
            else collect (Some value) forwarded rest
        match remaining with
        | [] ->
            let targetArguments =
                match pipeline, target with
                | "test", Some project -> [ "--project"; project ]
                | "test", None -> [ "--solution"; Repo.SolutionFileName ]
                | _, Some project -> [ project ]
                | _, None -> [ Repo.SolutionFileName ]
            let defaults = if pipeline = "format" then [ "--verify-no-changes"; "--no-restore" ] else []
            Ok ([ pipeline ] @ targetArguments @ defaults @ List.rev forwarded)
        | "--project" :: value :: rest -> setTarget value rest
        | [ "--project" ] -> Error "argument --project: expected one argument"
        | argument :: rest when argument.StartsWith("--project=", StringComparison.Ordinal) ->
            setTarget (argument.Substring "--project=".Length) rest
        | argument :: rest -> collect target (argument :: forwarded) rest
    collect None [] arguments

// Only the evaluation and LOC modules use positional arguments behind named launcher options.
let adaptNamed (values: string list) (switches: string list) (required: string list) (argv: string array) =
    let rec loop options forwarded remaining =
        let takeValue name (value: string) rest =
            if String.IsNullOrWhiteSpace value || value.StartsWith "-" then
                Error(sprintf "argument %s: expected one argument" name)
            elif Map.containsKey name options then Error(sprintf "argument %s: specified more than once" name)
            else loop (Map.add name value options) forwarded rest
        match remaining with
        | [] ->
            match required |> List.tryFind (fun name -> not (Map.containsKey name options)) with
            | Some name -> Error(sprintf "argument %s is required" name)
            | None ->
                let positional = required |> List.map (fun name -> Map.find name options)
                let optional = values |> List.filter (fun name -> not (List.contains name required))
                               |> List.collect (fun name -> Map.tryFind name options |> Option.map (fun value -> [ name; value ]) |> Option.defaultValue [])
                Ok(Array.ofList (positional @ optional @ List.rev forwarded))
        | name :: rest when List.contains name switches -> loop options (name :: forwarded) rest
        | name :: value :: rest when List.contains name values -> takeValue name value rest
        | [ name ] when List.contains name values -> Error(sprintf "argument %s: expected one argument" name)
        | argument :: rest ->
            let equals = argument.IndexOf '='
            if equals > 0 && List.contains (argument.Substring(0, equals)) values then
                takeValue (argument.Substring(0, equals)) (argument.Substring(equals + 1)) rest
            else Error("unrecognized argument: " + argument)
    loop Map.empty [] (List.ofArray argv)

