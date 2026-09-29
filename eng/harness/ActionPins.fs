module FunnySharp.Harness.ActionPins

// A behaviour-identical F# port of eng/tools/check_action_pins.py. Text-level
// checks over .github/workflows/*.yml and *.yaml: every remote `uses:` must be
// pinned as owner/repo@<40-hex-sha> with a `# <version>` comment, local actions
// are exempt, release.yml's `actions/*` pins stay with the frozen PowerShell
// protocol test, and tooling.yml takes the uv version from uv.toml.

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open FunnySharp.Harness.Output
open FunnySharp.Harness.Repo

let private workflowSuffixes = [ ".yml"; ".yaml" ]

// release.yml's `actions/*` pins stay with the frozen PowerShell protocol test;
// checking them here would create a divergent second owner for the same pins.
let private releaseWorkflowNames = Set.ofList [ "release.yml"; "release.yaml" ]
let private frozenActionOwner = "actions"
let private toolingWorkflowName = "tooling.yml"
let private uvSetupAction = "astral-sh/setup-uv"
let private uvPinFileName = "uv.toml"

let private uvRequiredVersionPattern =
    Regex(
        @"^[ \t]*required-version[ \t]*=[ \t]*['""]==\d+\.\d+\.\d+['""][ \t]*$",
        RegexOptions.Multiline
    )

let private usesPattern =
    Regex(@"^(?<indent>[ \t]*)(?<dash>-\s+)?uses\s*:\s*(?<value>.*?)[ \t]*$")

let private pinnedReferencePattern =
    Regex(@"^(?<owner>[^/@\s]+)/(?<repo>[^/@\s]+)@(?<sha>[0-9a-f]{40})$")

let private listItemPattern = Regex(@"^(?<indent>[ \t]*)-[ \t]")

// Matches `version` as a mapping key in block style or inside a flow mapping.
let private versionKeyPattern =
    Regex(@"(?:^|[{\[,])[ \t]*[""']?version[""']?[ \t]*:")

let private toPosix (path: string) : string =
    path.Replace('\\', '/')

/// Python's Path.relative_to(...).as_posix(), falling back to the full path when
/// the target is not under the root.
let private relativePosix (repositoryRoot: string) (path: string) : string =
    let root = Path.GetFullPath repositoryRoot
    let full = Path.GetFullPath path
    let relative = Path.GetRelativePath(root, full)
    if relative.StartsWith("..") || Path.IsPathRooted relative then
        toPosix full
    else
        toPosix relative

/// Python's str.splitlines(): breaks on every line boundary and never emits a
/// trailing empty line for a trailing terminator.
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

let private readTextReplacing (path: string) : string =
    // Encoding.UTF8 uses replacement fallback, mirroring read_text(errors="replace").
    File.ReadAllText(path, Encoding.UTF8)

/// One pinning violation at a precise file:line location.
type Finding =
    { Path: string
      Line: int
      Message: string }

    member this.Format(repositoryRoot: string) : string =
        let relative = relativePosix repositoryRoot this.Path
        sprintf "%s:%d: %s" relative this.Line this.Message

/// A `uses:` reference parsed from one workflow line.
type UsesReference =
    { Line: int
      Indent: int
      Reference: string
      Comment: string }

    member this.IsLocal: bool =
        this.Reference.StartsWith("./")

let private unquote (value: string) : string =
    if
        value.Length >= 2
        && value.[0] = value.[value.Length - 1]
        && (value.[0] = '\'' || value.[0] = '"')
    then
        value.Substring(1, value.Length - 2)
    else
        value

/// Split a `uses:` value into (reference, inline comment). YAML starts a comment
/// at a `#` preceded by whitespace; quotes are honoured.
let private splitUsesValue (raw: string) : string * string =
    let mutable quote = '\000'
    let mutable commentAt = -1
    let mutable index = 0

    while commentAt < 0 && index < raw.Length do
        let ch = raw.[index]

        if quote <> '\000' then
            if ch = quote then
                quote <- '\000'
        elif ch = '\'' || ch = '"' then
            quote <- ch
        elif ch = '#' && (index = 0 || Char.IsWhiteSpace raw.[index - 1]) then
            commentAt <- index

        index <- index + 1

    if commentAt >= 0 then
        unquote (raw.Substring(0, commentAt).TrimEnd()), raw.Substring(commentAt).Trim()
    else
        unquote (raw.Trim()), ""

/// Every `uses:` reference in one workflow's lines, in file order.
let private iterUsesReferences (lines: string list) : UsesReference list =
    let references = ResizeArray<UsesReference>()

    lines
    |> List.iteri (fun lineIndex line ->
        let matched = usesPattern.Match line

        if matched.Success then
            let value, comment = splitUsesValue (matched.Groups.["value"].Value)

            // Column where the `uses` token starts, so `- uses:` and `uses:` forms
            // both anchor their step block correctly.
            let indent =
                matched.Groups.["indent"].Value.Length
                + (if matched.Groups.["dash"].Success then 2 else 0)

            references.Add
                { Line = lineIndex + 1
                  Indent = indent
                  Reference = value
                  Comment = comment })

    List.ofSeq references

/// The (line number, text) pairs of the step block owning `uses`.
let private stepBlockLines (lines: string array) (uses: UsesReference) : (int * string) list =
    let mutable dashIndent = uses.Indent - 2
    let mutable first = uses.Line
    let mutable search = uses.Line
    let mutable anchored = false

    while not anchored && search >= 1 do
        let line = lines.[search - 1]
        let item = listItemPattern.Match line

        if item.Success then
            let itemIndent = item.Groups.["indent"].Value.Length

            if itemIndent < uses.Indent then
                first <- search
                dashIndent <- itemIndent
                anchored <- true

        search <- search - 1

    let block = ResizeArray<int * string>()
    let mutable lineIndex = first
    let mutable broken = false

    while not broken && lineIndex <= lines.Length do
        let line = lines.[lineIndex - 1]

        if lineIndex > first && line.Trim() <> "" then
            let indent = line.Length - line.TrimStart([| ' '; '\t' |]).Length

            if indent <= dashIndent then
                broken <- true

        if not broken then
            block.Add(lineIndex, line)

        lineIndex <- lineIndex + 1

    List.ofSeq block

/// Every pinning finding for one workflow's parsed references.
let private checkWorkflowText
    (path: string)
    (references: UsesReference list)
    (ignoredOwners: Set<string>)
    : Finding list =
    [ for uses in references do
          if
              not uses.IsLocal
              && not (ignoredOwners.Contains(uses.Reference.Split([| '/' |], 2).[0]))
          then
              if not (pinnedReferencePattern.IsMatch uses.Reference) then
                  yield
                      { Path = path
                        Line = uses.Line
                        Message =
                          sprintf
                              "uses reference '%s' must be pinned as owner/repo@<40-hex-sha> with a '# <version>' comment"
                              uses.Reference }
              elif uses.Comment.TrimStart('#').Trim() = "" then
                  yield
                      { Path = path
                        Line = uses.Line
                        Message = sprintf "uses reference '%s' is missing the trailing '# <version>' comment" uses.Reference } ]

/// Assert tooling.yml takes the uv version from uv.toml, not a `version:` input.
let private checkToolingPins
    (path: string)
    (references: UsesReference list)
    (lines: string array)
    (repositoryRoot: string)
    : Finding list =
    let findings = ResizeArray<Finding>()

    let setupUv =
        references
        |> List.filter (fun uses ->
            uses.Reference = uvSetupAction
            || uses.Reference.StartsWith(uvSetupAction + "@"))

    for uses in setupUv do
        for lineNumber, line in stepBlockLines lines uses do
            if versionKeyPattern.IsMatch line then
                findings.Add
                    { Path = path
                      Line = lineNumber
                      Message =
                        "the setup-uv step must not set 'version:'; the uv version comes from uv.toml's required-version" }

    if not setupUv.IsEmpty then
        let uvPinPath = Path.Combine(repositoryRoot, uvPinFileName)

        let declared =
            try
                if File.Exists uvPinPath then
                    uvRequiredVersionPattern.IsMatch(readTextReplacing uvPinPath)
                else
                    false
            with _ ->
                false

        if not declared then
            findings.Add
                { Path = path
                  Line = (List.head setupUv).Line
                  Message =
                    sprintf
                        "%s must take the uv version from '%s' required-version, but that pin is missing or is not an exact '==x.y.z' specification"
                        toolingWorkflowName
                        uvPinFileName }

    List.ofSeq findings

/// The file name of a path, with the repository-relative fallback for a null result.
let private fileNameOf (path: string) : string =
    match Path.GetFileName path with
    | null -> path
    | name -> name

let private extensionOf (path: string) : string =
    match Path.GetExtension path with
    | null -> ""
    | extension -> extension

/// The workflow files to scan, sorted by name.
let private workflowFiles (workflowsDirectory: string) : string list =
    if not (Directory.Exists workflowsDirectory) then
        []
    else
        Directory.GetFiles workflowsDirectory
        |> Array.filter (fun path -> List.contains (extensionOf path) workflowSuffixes)
        |> Array.sortBy fileNameOf
        |> List.ofArray

/// Check every workflow under `repositoryRoot`.
/// Returns the scanned files (in scope) and all findings.
let checkRepository (repositoryRoot: string) : string list * Finding list =
    let workflowsDirectory = Path.Combine(repositoryRoot, ".github", "workflows")
    let scanned = ResizeArray<string>()
    let findings = ResizeArray<Finding>()

    for path in workflowFiles workflowsDirectory do
        let lines = splitLines (readTextReplacing path)
        let references = iterUsesReferences (List.ofArray lines)
        let name = fileNameOf path
        scanned.Add path

        if releaseWorkflowNames.Contains name then
            findings.AddRange(checkWorkflowText path references (Set.singleton frozenActionOwner))
        else
            findings.AddRange(checkWorkflowText path references Set.empty)

            if name = toolingWorkflowName then
                findings.AddRange(checkToolingPins path references lines repositoryRoot)

    List.ofSeq scanned, List.ofSeq findings

// ---- Command line ----

type private CliOptions =
    { Verbose: bool
      RepositoryRoot: string option
      Help: bool }

let private usageLine = "usage: check_action_pins.py [-h] [--verbose] [--repository-root PATH]"

let private helpText =
    String.concat
        "\n"
        [ usageLine
          ""
          "Check that remote workflow actions are pinned to full commit SHAs with version comments, and that tooling.yml reads the uv pin from uv.toml's required-version."
          ""
          "options:"
          "  -h, --help            show this help message and exit"
          "  --verbose             print every scanned workflow file, including files without findings."
          "  --repository-root PATH"
          "                        repository root (default: resolved from the script location)." ]

let private parseArgs (argv: string list) : Result<CliOptions, string> =
    let rec loop (options: CliOptions) (remaining: string list) =
        match remaining with
        | [] -> Ok options
        | "-h" :: _ | "--help" :: _ -> Ok { options with Help = true }
        | "--verbose" :: rest -> loop { options with Verbose = true } rest
        | "--repository-root" :: value :: rest ->
            loop { options with RepositoryRoot = Some value } rest
        | [ "--repository-root" ] -> Error "argument --repository-root: expected one argument"
        | arg :: rest when arg.StartsWith("--repository-root=") ->
            loop { options with RepositoryRoot = Some(arg.Substring("--repository-root=".Length)) } rest
        | arg :: _ -> Error("unrecognized arguments: " + arg)

    loop { Verbose = false; RepositoryRoot = None; Help = false } argv

/// Run the check writing to the supplied writers. Returns the Python exit code:
/// 0 all checks pass, 1 findings, 2 environment or usage failure.
let mainWith (stdout: TextWriter) (stderr: TextWriter) (argv: string list) : int =
    match parseArgs argv with
    | Error message ->
        stderr.WriteLine usageLine
        stderr.WriteLine("check_action_pins.py: error: " + message)
        2
    | Ok options when options.Help ->
        stdout.WriteLine helpText
        0
    | Ok options ->
        let repositoryRoot =
            match options.RepositoryRoot with
            | Some root -> Path.GetFullPath root
            | None ->
                match tryFindRoot () with
                | Some root -> root
                | None -> Environment.CurrentDirectory

        let workflowsDirectory = Path.Combine(repositoryRoot, ".github", "workflows")

        if not (Directory.Exists workflowsDirectory) then
            stderr.WriteLine(sprintf "ERROR: workflow directory '%s' was not found." workflowsDirectory)
            2
        else
            let scanned, findings = checkRepository repositoryRoot

            if options.Verbose then
                for path in scanned do
                    stdout.WriteLine(sprintf "checked %s" (relativePosix repositoryRoot path))

            if not findings.IsEmpty then
                for finding in findings do
                    stderr.WriteLine(finding.Format repositoryRoot)

                stderr.WriteLine(
                    sprintf "FAIL: %d pinning finding(s) in %d workflow file(s)." findings.Length scanned.Length
                )

                1
            else
                stdout.WriteLine(
                    sprintf
                        "OK: %d workflow file(s) checked; every remote action is pinned to a full commit SHA with a '# <version>' comment."
                        scanned.Length
                )

                0

/// Entry point mirroring check_action_pins.py's main().
let main (argv: string array) : int =
    mainWith Console.Out Console.Error (List.ofArray argv)
