module FunnySharp.Harness.Loc

// A behaviour-identical F# port of the two call-site line counters:
//   * docs/next-stage/call-sites-code/tools/loc.py      -> extract
//   * docs/next-stage/call-sites-code/tools/rawloc.py   -> reportLines
// plus eng/evaluation/runner.py's consumer-side count_loc -> countLoc.
//
// The two counters are deliberately kept distinct (spec section 11):
//   * countLoc  strips /* */ with DOTALL before splitting and reads utf-8-sig
//     (a leading BOM is dropped), then counts non-blank, non-`//` lines.
//   * extract   does NOT strip block comments, reads utf-8 (a BOM is kept as
//     U+FEFF at the head of the first line), and splits on LF only, so a CRLF
//     file keeps its trailing '\r'.
//
// The rawloc report resolves its TARGETS paths against the call-sites-code
// directory (docs/next-stage/call-sites-code) rather than the process CWD, so
// it reproduces the committed fixture from any working directory.

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open FunnySharp.Harness.Output
open FunnySharp.Harness.Repo

// ---------------------------------------------------------------------------
// Text decoding / line splitting
// ---------------------------------------------------------------------------

// UTF-8 without BOM handling on decode: bytes EF BB BF become U+FEFF, matching
// Python's open(path, encoding="utf-8").
let private utf8 = UTF8Encoding(false)

let private readUtf8 (path: string) : string =
    utf8.GetString(File.ReadAllBytes path)

// UTF-8 with a single leading BOM stripped, matching encoding="utf-8-sig".
let private readUtf8Sig (path: string) : string =
    let bytes = File.ReadAllBytes path

    let start =
        if bytes.Length >= 3 && bytes.[0] = 0xEFuy && bytes.[1] = 0xBBuy && bytes.[2] = 0xBFuy then
            3
        else
            0

    utf8.GetString(bytes, start, bytes.Length - start)

/// Python's str.splitlines(): breaks on \n, \r, \r\n and the extra boundaries
/// \v \f \x1c \x1d \x1e \x85 \u2028 \u2029, never emitting a trailing empty
/// line for a trailing terminator.
let private splitLines (text: string) : string array =
    let lines = ResizeArray<string>()

    let isLineBreak (ch: char) =
        match int ch with
        | 10 | 13 | 11 | 12 | 28 | 29 | 30 | 133 | 8232 | 8233 -> true
        | _ -> false

    let mutable start = 0
    let mutable index = 0

    while index < text.Length do
        if isLineBreak text.[index] then
            lines.Add(text.Substring(start, index - start))

            if text.[index] = '\r' && index + 1 < text.Length && text.[index + 1] = '\n' then
                index <- index + 1

            index <- index + 1
            start <- index
        else
            index <- index + 1

    if start < text.Length then
        lines.Add(text.Substring start)

    lines.ToArray()

let private countChar (ch: char) (line: string) : int =
    let mutable total = 0

    for c in line do
        if c = ch then
            total <- total + 1

    total

// ---------------------------------------------------------------------------
// count_loc (runner.py) — consumer-side LOC
// ---------------------------------------------------------------------------

let private blockCommentPattern = Regex(@"/\*.*?\*/", RegexOptions.Singleline)

/// Count non-blank, non-`//` lines after stripping every `/* ... */` block
/// (DOTALL) from `text`.
let countLocText (text: string) : int =
    let withoutBlocks = blockCommentPattern.Replace(text, "")

    splitLines withoutBlocks
    |> Array.filter (fun line ->
        let trimmed = line.Trim()
        trimmed.Length > 0 && not (trimmed.StartsWith("//")))
    |> Array.length

/// Count the consumer-side LOC of one file (BOM stripped).
let countLoc (path: string) : int =
    countLocText (readUtf8Sig path)

// ---------------------------------------------------------------------------
// loc.py — extract a method body by name
// ---------------------------------------------------------------------------

/// Extract the body lines and the raw (non-blank, non-`//`) lines of the first
/// declaration-shaped line matching `name`. Returns None when no such line
/// exists, exactly like loc.py's extract().
let extract (path: string) (name: string) : (string list * string list) option =
    let lines = (readUtf8 path).Split('\n')
    let pattern = Regex(@"\b" + Regex.Escape name + @"\s*[<(]")

    let mutable start = -1
    let mutable probe = 0

    while start < 0 && probe < lines.Length do
        let line = lines.[probe]

        if
            pattern.IsMatch line
            && (line.Contains "static" || line.Contains "public" || line.Contains "private")
        then
            start <- probe

        probe <- probe + 1

    if start < 0 then
        None
    else
        let mutable depth = 0
        let mutable seenBrace = false
        let mutable endIndex = lines.Length - 1
        let mutable index = start
        let mutable stopped = false

        while not stopped && index < lines.Length do
            let line = lines.[index]
            depth <- depth + countChar '{' line - countChar '}' line

            if line.Contains "{" then
                seenBrace <- true

            if seenBrace && depth = 0 then
                endIndex <- index
                stopped <- true
            elif not seenBrace && line.Contains ";" then
                endIndex <- index
                stopped <- true
            else
                index <- index + 1

        let body = lines.[start..endIndex] |> Array.toList

        let raw =
            body
            |> List.filter (fun line ->
                let trimmed = line.Trim()
                trimmed.Length > 0 && not (trimmed.StartsWith("//")))

        Some(body, raw)

/// The two console shapes loc.py writes: the indexed body plus the raw count,
/// or the single `NOT FOUND` line. Returns the lines to write to stdout.
let extractLines (path: string) (name: string) : string list =
    match extract path name with
    | None -> [ "NOT FOUND: " + name ]
    | Some(body, raw) ->
        let indexed =
            body
            |> List.mapi (fun index line -> sprintf "%s| %s" (index.ToString().PadLeft 3) line)

        indexed @ [ sprintf "--- raw LOC (non-blank, non-comment): %d" raw.Length ]

// ---------------------------------------------------------------------------
// rawloc.py — batch raw-LOC report
// ---------------------------------------------------------------------------

/// The committed 29-row TARGETS table, in list order (label, path, names).
let targets : (string * string * string list) list =
    [ "W1  dictionary+fallback", "idiomatic/Workflows.cs", [ "TimeoutSeconds" ]
      "W1  dictionary+fallback", "funnysharp/Workflows.cs", [ "TimeoutSeconds" ]
      "W1  dictionary+fallback", "competitors/W1Funcky.cs", [ "TimeoutSeconds" ]
      "W2  fail-fast pipeline", "idiomatic/Workflows.cs", [ "CreateInvoiceAsync" ]
      "W2  fail-fast pipeline",
      "funnysharp/Workflows.cs",
      [ "CreateInvoiceAsync"; "FindCustomerAsync"; "InvalidQuantityFor" ]
      "W2  fail-fast pipeline", "competitors/W2Cfe.cs", [ "CreateInvoiceAsync"; "InvalidQuantityFor" ]
      "W3  accumulate fields", "idiomatic/Workflows.cs", [ "ValidateSignup" ]
      "W3  accumulate fields",
      "funnysharp/Workflows.cs",
      [ "ValidateSignup"; "ValidateEmail"; "ValidatePassword"; "ValidateAge" ]
      "W3  accumulate fields",
      "competitors/W3LanguageExt.cs",
      [ "ValidateSignup"; "ValidateEmail"; "ValidatePassword"; "ValidateAge" ]
      "W3b accumulate fields (CFE)",
      "competitors/W3bCfe.cs",
      [ "ValidateSignup"; "ValidateEmail"; "ValidatePassword"; "ValidateAge" ]
      "W4  unit-result", "idiomatic/Workflows.cs", [ "DeleteOrderAsync" ]
      "W4  unit-result", "funnysharp/Workflows.cs", [ "DeleteOrderAsync" ]
      "W4  unit-result", "competitors/W4Cfe.cs", [ "DeleteOrderAsync" ]
      "W5  tap/observation", "idiomatic/Workflows.cs", [ "TotalWithAudit" ]
      "W5  tap/observation", "funnysharp/Workflows.cs", [ "TotalWithAudit" ]
      "W5  tap/observation", "competitors/W5Cfe.cs", [ "TotalWithAudit" ]
      "W6  traversal+index", "idiomatic/Workflows.cs", [ "ParseAll"; "ParseLine" ]
      "W6  traversal+index", "funnysharp/Workflows.cs", [ "ParseAll"; "ParseLine" ]
      "W7  bounded parallel (op+consumer)",
      "idiomatic/Workflows.cs",
      [ "SelectParallelAsync"; "SumFetchedAsync" ]
      "W7  bounded parallel (consumer)", "funnysharp/Workflows.cs", [ "SumFetchedAsync" ]
      "W8  first success", "idiomatic/Workflows.cs", [ "FirstInvoiceAsync" ]
      "W8  first success", "funnysharp/Workflows.cs", [ "FirstInvoiceAsync" ]
      "W9  env+resource", "idiomatic/Workflows.cs", [ "LoadOrderTotalAsync" ]
      "W9  env+resource", "funnysharp/Workflows.cs", [ "LoadOrderTotalAsync" ]
      "W10 nested update", "idiomatic/Workflows.cs", [ "ReviewPostalCode" ]
      "W10 nested update", "funnysharp/Workflows.cs", [ "ReviewPostalCode" ]
      "W10 nested update", "competitors/W10LanguageExt.cs", [ "ReviewPostalCode" ]
      "W11 http idiomatic",
      "funnysharp-aspnet/Workflows.cs",
      [ "GetCustomerIdiomatic"; "CreateOrderIdiomaticAsync"; "SignUpIdiomatic" ]
      "W11 http funny",
      "funnysharp-aspnet/Workflows.cs",
      [ "GetCustomer"; "CreateOrder"; "SignUp"; "CreateOrderFromEffectAsync" ] ]

/// Render the rawloc fixture for the supplied targets against `baseDir`.
let reportFor (rowTargets: (string * string * string list) list) (baseDir: string) : string list =
    let lines = ResizeArray<string>()
    let mutable total = 0

    for label, relativePath, names in rowTargets do
        let fullPath = Path.Combine(baseDir, relativePath)
        let counts = ResizeArray<string>()
        let mutable value = 0

        for name in names do
            match extract fullPath name with
            | Some(_, raw) ->
                value <- value + raw.Length
                counts.Add(sprintf "%s=%d" name raw.Length)
            | None -> counts.Add(name + "=NOT_FOUND")

        total <- total + value

        lines.Add(
            sprintf
                "%s %s raw=%s  (%s)"
                (label.PadRight 34)
                (relativePath.PadRight 28)
                (value.ToString().PadLeft 3)
                (String.Join(", ", counts))
        )

    lines.Add(sprintf "TOTAL raw lines counted: %d" total)
    List.ofSeq lines

/// Render the committed rawloc report against `baseDir`.
let reportLines (baseDir: string) : string list =
    reportFor targets baseDir

/// Render the rawloc report, converting any read failure into a HarnessError.
let reportResult (baseDir: string) : Result<string list, HarnessError> =
    catchResult (fun () -> reportLines baseDir)

/// The call-sites-code directory the report's relative paths resolve against.
let callSitesCodeDirectory () : string =
    match tryFindRoot () with
    | Some root -> Path.Combine(root, "docs", "next-stage", "call-sites-code")
    | None -> Environment.CurrentDirectory

// ---------------------------------------------------------------------------
// CLI
// ---------------------------------------------------------------------------

let private usageLine =
    "usage: rawloc.py            (no arguments, resolves targets under docs/next-stage/call-sites-code)\n       loc.py <file> <method>"

/// Dispatch mirroring the two scripts: no arguments runs the rawloc report;
/// one or more arguments runs loc.py's `<file> <method>` extractor (extra
/// arguments are ignored, as in Python). Returns the legacy exit code.
let mainWith (stdout: TextWriter) (stderr: TextWriter) (argv: string list) : int =
    match argv with
    | [] ->
        match reportResult (callSitesCodeDirectory ()) with
        | Ok lines ->
            for line in lines do
                stdout.WriteLine line

            0
        | Error err ->
            stderr.WriteLine err.Message
            err.ExitCode
    | [ _ ] ->
        stderr.WriteLine "usage: loc.py <file> <method>"
        1
    | path :: name :: _ ->
        try
            for line in extractLines path name do
                stdout.WriteLine line

            0
        with ex ->
            stderr.WriteLine ex.Message
            1

let main (argv: string array) : int =
    mainWith Console.Out Console.Error (List.ofArray argv)
