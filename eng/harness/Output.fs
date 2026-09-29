module FunnySharp.Harness.Output

// Shared error type, result railway helpers and the console verdict printers every
// FunnySharp gate uses. Plain ASCII, deterministic, no colour escapes.

open System

/// A harness failure carrying the message and the exit code a gate must surface.
type HarnessError =
    { Message: string
      ExitCode: int }

/// Constructors for <see cref="HarnessError"/>; kept in their own module so the
/// records stay plain data.
module Errors =

    /// A failure that surfaces as exit code 1.
    let create (message: string) : HarnessError =
        { Message = message; ExitCode = 1 }

    /// A failure with an explicit exit code (2 is the environment/usage failure).
    let withExitCode (exitCode: int) (message: string) : HarnessError =
        { Message = message; ExitCode = exitCode }

let ok (value: 'T) : Result<'T, HarnessError> = Ok value

let failError (message: string) : Result<'T, HarnessError> =
    Error(Errors.create message)

let failErrorWith (exitCode: int) (message: string) : Result<'T, HarnessError> =
    Error(Errors.withExitCode exitCode message)

/// Run a synchronous function, converting any exception into an Error.
let catchResult (f: unit -> 'T) : Result<'T, HarnessError> =
    try
        Ok(f ())
    with ex ->
        Error(Errors.create ex.Message)

/// Keep the first Error while collecting every Ok, in input order.
let collectResults (results: Result<'T, HarnessError> list) : Result<'T list, HarnessError> =
    let rec loop (acc: 'T list) (remaining: Result<'T, HarnessError> list) =
        match remaining with
        | [] -> Ok(List.rev acc)
        | Ok value :: rest -> loop (value :: acc) rest
        | Error err :: _ -> Error err

    loop [] results

let traverseResults (f: 'T -> Result<'U, HarnessError>) (values: 'T list) : Result<'U list, HarnessError> =
    values |> List.map f |> collectResults

// ---- Verdict printers (stdout for PASS/FAIL/VERIFY, stderr for ERROR) ----

let pass (name: string) : unit =
    Console.Out.WriteLine("PASS " + name)

let fail (name: string) (message: string) : unit =
    Console.Out.WriteLine("FAIL " + name + ": " + message)

let verify (name: string) : unit =
    Console.Out.WriteLine("VERIFY " + name)

let error (message: string) : unit =
    Console.Error.WriteLine("ERROR: " + message)
