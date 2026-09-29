module FunnySharp.Harness.Ruleset

// A behaviour-identical F# port of eng/Verify-GitHubRuleset.ps1: records and
// verifies the GitHub repository ruleset that the release gate depends on.
//
// It resolves the repository and default branch through `gh`, fetches the ruleset
// through the GitHub API, then fails closed unless: enforcement is active, the
// ruleset targets branches, the target ref is included with no exclusions, there
// is exactly one required_status_checks rule whose strict policy is true, each of
// the four hard-coded release contexts is bound exactly once to
// -ExpectedIntegrationId, and there are zero bypass actors.
//
// The PowerShell original prompts interactively for missing mandatory parameters;
// this port never prompts. A missing mandatory parameter, an unparsable integer or
// an out-of-range ExpectedIntegrationId is a typed usage failure (exit 2).
//
// Every failure message, the evidence key set and the verdict line are byte-exact
// with the original with the single exception of the usage messages above, which
// had no non-interactive form in PowerShell.

open System
open System.Globalization
open System.IO
open System.Text
open System.Text.Encodings.Web
open System.Text.Json
open FunnySharp.Harness.Output
open FunnySharp.Harness.Proc

// ---------------------------------------------------------------------------
// JSON helpers
// ---------------------------------------------------------------------------

let private tryMember (name: string) (element: JsonElement) : JsonElement option =
    if element.ValueKind <> JsonValueKind.Object then
        None
    else
        match element.TryGetProperty name with
        | true, value -> Some value
        | _ -> None

let private stringMember (name: string) (element: JsonElement) : string option =
    tryMember name element
    |> Option.bind (fun value ->
        if value.ValueKind = JsonValueKind.String then
            match value.GetString() with
            | null -> None
            | text -> Some text
        else
            None)

let private int64Member (name: string) (element: JsonElement) : int64 option =
    tryMember name element
    |> Option.bind (fun value ->
        if value.ValueKind = JsonValueKind.Number then
            match value.TryGetInt64() with
            | true, number -> Some number
            | _ -> None
        else
            None)

let private stringArray (element: JsonElement) : string list =
    if element.ValueKind = JsonValueKind.Array then
        element.EnumerateArray()
        |> Seq.choose (fun item ->
            if item.ValueKind = JsonValueKind.String then
                match item.GetString() with
                | null -> None
                | value -> Some value
            else
                None)
        |> List.ofSeq
    else
        []

/// A structural JSON node copied out of a document so it outlives the document.
let private clone (element: JsonElement) : JsonElement = element.Clone()

// ---------------------------------------------------------------------------
// Core checks
// ---------------------------------------------------------------------------

/// The four required release workflow contexts, in the original's fixed order.
let requiredContexts: string list =
    [ "release / win-x64"
      "release / linux-x64"
      "release / osx-arm64"
      "release / osx-x64-consumer" ]

/// The strict-up-to-date policy requirement, exposed by name like the
/// PowerShell Assert-StrictRequiredStatusChecksPolicy function and its tests.
let assertStrictRequiredStatusChecksPolicy
    (parameters: JsonElement)
    : Result<unit, HarnessError> =
    let strict =
        tryMember "strict_required_status_checks_policy" parameters
        |> Option.exists (fun value -> value.ValueKind = JsonValueKind.True)

    if strict then
        Ok()
    else
        failError "Required status checks must require branches to be up to date before merging."

/// Validated fields copied out of a ruleset for the evidence object.
type RulesetFields =
    { Id: int64
      Name: string
      Target: string
      Enforcement: string
      UpdatedAt: string
      RefConditions: JsonElement
      BypassActors: JsonElement
      RequiredContextsObserved: string list
      RequiredChecks: (string * int64) list }

/// Validate a fetched ruleset, returning the fields the evidence object needs.
let checkRuleset
    (rulesetId: int64)
    (expectedIntegrationId: int64)
    (targetBranch: string)
    (ruleset: JsonElement)
    : Result<RulesetFields, HarnessError> =
    let enforcement = stringMember "enforcement" ruleset |> Option.defaultValue ""

    if not (String.Equals(enforcement, "active", StringComparison.OrdinalIgnoreCase)) then
        failError (sprintf "Ruleset %d is not active." rulesetId)
    else
        let target = stringMember "target" ruleset |> Option.defaultValue ""

        if not (String.Equals(target, "branch", StringComparison.OrdinalIgnoreCase)) then
            failError (sprintf "Ruleset %d does not target branches." rulesetId)
        else
            let refConditions =
                tryMember "conditions" ruleset
                |> Option.bind (tryMember "ref_name")
                |> Option.defaultValue (JsonDocument.Parse("{}").RootElement.Clone())

            let includedRefs =
                tryMember "include" refConditions |> Option.map stringArray |> Option.defaultValue []

            let excludedRefs =
                tryMember "exclude" refConditions |> Option.map stringArray |> Option.defaultValue []

            let targetRef = "refs/heads/" + targetBranch

            let includedTarget =
                [ "~ALL"; "~DEFAULT_BRANCH"; targetRef ]
                |> List.exists (fun candidate ->
                    includedRefs
                    |> List.exists (fun reference ->
                        String.Equals(reference, candidate, StringComparison.OrdinalIgnoreCase)))

            if not includedTarget then
                failError (
                    sprintf "Ruleset %d does not explicitly include '%s' or the default branch." rulesetId targetRef
                )
            elif not excludedRefs.IsEmpty then
                failError (
                    sprintf
                        "Ruleset %d contains branch exclusions and cannot prove fail-closed default-branch coverage."
                        rulesetId
                )
            else
                let rules =
                    tryMember "rules" ruleset
                    |> Option.map (fun value ->
                        if value.ValueKind = JsonValueKind.Array then
                            value.EnumerateArray() |> List.ofSeq
                        else
                            [])
                    |> Option.defaultValue []

                let statusRules =
                    rules
                    |> List.filter (fun rule ->
                        match stringMember "type" rule with
                        | Some ruleType -> String.Equals(ruleType, "required_status_checks", StringComparison.OrdinalIgnoreCase)
                        | None -> false)

                if statusRules.Length <> 1 then
                    failError (
                        sprintf "Ruleset %d must contain exactly one required_status_checks rule." rulesetId
                    )
                else
                    let statusRule = statusRules.[0]

                    let parameters =
                        tryMember "parameters" statusRule
                        |> Option.defaultValue (JsonDocument.Parse("{}").RootElement.Clone())

                    match assertStrictRequiredStatusChecksPolicy parameters with
                    | Error err -> Error err
                    | Ok () ->
                        let requiredStatusChecks =
                            tryMember "required_status_checks" parameters
                            |> Option.map (fun value ->
                                if value.ValueKind = JsonValueKind.Array then
                                    value.EnumerateArray() |> List.ofSeq
                                else
                                    [])
                            |> Option.defaultValue []

                        let observedContexts =
                            requiredStatusChecks
                            |> List.choose (fun check -> stringMember "context" check)

                        let missing =
                            requiredContexts
                            |> List.filter (fun required ->
                                not (
                                    observedContexts
                                    |> List.exists (fun observed ->
                                        String.Equals(observed, required, StringComparison.OrdinalIgnoreCase))))

                        if not missing.IsEmpty then
                            failError (
                                sprintf
                                    "Ruleset %d is missing required contexts: %s."
                                    rulesetId
                                    (String.Join(", ", missing))
                            )
                        else
                            let bindings =
                                requiredContexts
                                |> List.map (fun context ->
                                    let matches =
                                        requiredStatusChecks
                                        |> List.filter (fun check ->
                                            match stringMember "context" check with
                                            | Some observed -> String.Equals(observed, context, StringComparison.Ordinal)
                                            | None -> false)

                                    match matches with
                                    | [ single ] when int64Member "integration_id" single = Some expectedIntegrationId ->
                                        Ok(context, expectedIntegrationId)
                                    | _ ->
                                        failError (
                                            sprintf
                                                "Ruleset %d must bind required context '%s' to GitHub App integration %d."
                                                rulesetId
                                                context
                                                expectedIntegrationId
                                        ))

                            match collectResults bindings with
                            | Error err -> Error err
                            | Ok requiredChecks ->
                                let bypassActors =
                                    tryMember "bypass_actors" ruleset
                                    |> Option.defaultValue (JsonDocument.Parse("[]").RootElement.Clone())

                                let bypassCount =
                                    if bypassActors.ValueKind = JsonValueKind.Array then
                                        bypassActors.GetArrayLength()
                                    else
                                        0

                                if bypassCount <> 0 then
                                    failError (
                                        sprintf
                                            "Ruleset %d contains bypass actors and cannot support this candidate's PASS verdict."
                                            rulesetId
                                    )
                                else
                                    Ok
                                        { Id = int64Member "id" ruleset |> Option.defaultValue rulesetId
                                          Name = stringMember "name" ruleset |> Option.defaultValue ""
                                          Target = target
                                          Enforcement = enforcement
                                          UpdatedAt = stringMember "updated_at" ruleset |> Option.defaultValue ""
                                          RefConditions = clone refConditions
                                          BypassActors = clone bypassActors
                                          RequiredContextsObserved =
                                            observedContexts
                                            |> List.sortWith (fun a b -> String.CompareOrdinal(a, b))
                                          RequiredChecks = requiredChecks }

// ---------------------------------------------------------------------------
// Evidence
// ---------------------------------------------------------------------------

let private serializeEvidence
    (repository: string)
    (targetBranch: string)
    (expectedIntegrationId: int64)
    (checkedAtUtc: string)
    (fields: RulesetFields)
    : string =
    use stream = new MemoryStream()

    let options =
        JsonWriterOptions(
            Indented = true,
            NewLine = Environment.NewLine,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        )

    use writer = new Utf8JsonWriter(stream, options)
    writer.WriteStartObject()
    writer.WriteNumber("schemaVersion", 1)
    writer.WriteString("checkedAtUtc", checkedAtUtc)
    writer.WriteString("repository", repository)
    writer.WriteNumber("rulesetId", fields.Id)
    writer.WriteString("rulesetName", fields.Name)
    writer.WriteString("target", fields.Target)
    writer.WriteString("targetBranch", targetBranch)
    writer.WriteNumber("expectedIntegrationId", expectedIntegrationId)
    writer.WritePropertyName "refConditions"
    fields.RefConditions.WriteTo writer
    writer.WriteString("enforcement", fields.Enforcement)
    writer.WriteString("updatedAt", fields.UpdatedAt)
    writer.WritePropertyName "bypassActors"
    fields.BypassActors.WriteTo writer
    writer.WriteStartArray("requiredContexts")

    for context in fields.RequiredContextsObserved do
        writer.WriteStringValue context

    writer.WriteEndArray()
    writer.WriteStartArray("requiredChecks")

    for context, integrationId in fields.RequiredChecks do
        writer.WriteStartObject()
        writer.WriteString("context", context)
        writer.WriteNumber("integrationId", integrationId)
        writer.WriteEndObject()

    writer.WriteEndArray()
    writer.WriteBoolean("requiredContextsSatisfied", true)
    writer.WriteBoolean("strictRequiredStatusChecksPolicy", true)
    writer.WriteEndObject()
    writer.Flush()

    Encoding.UTF8.GetString(stream.ToArray()) + Environment.NewLine

// ---------------------------------------------------------------------------
// Command line
// ---------------------------------------------------------------------------

type CliOptions =
    { Repository: string option
      TargetBranch: string option
      RulesetId: int64 option
      ExpectedIntegrationId: int64 option
      OutputPath: string option
      Help: bool }

/// The run-time collaborators, so tests can supply a fake `gh` without a network.
type Collaborators =
    { HasGitHubCli: unit -> bool
      RunGitHubCli: string list -> ProcessResult }

let private tryFindExecutable (name: string) : string option =
    match Environment.GetEnvironmentVariable "PATH" with
    | null -> None
    | pathValue ->
        pathValue.Split(Path.PathSeparator)
        |> Array.tryPick (fun directory ->
            if String.IsNullOrWhiteSpace directory then
                None
            else
                let candidate = Path.Combine(directory, name)
                if File.Exists candidate then Some candidate else None)

let defaultCollaborators: Collaborators =
    { HasGitHubCli = fun () -> (tryFindExecutable "gh").IsSome
      RunGitHubCli = fun arguments -> runCaptureSync "gh" arguments }

let private usageLine =
    "usage: Verify-GitHubRuleset.ps1 [-Repository <owner/name>] [-TargetBranch <branch>] -RulesetId <id> -ExpectedIntegrationId <id> -OutputPath <path>"

let private helpText =
    String.concat
        Environment.NewLine
        [ usageLine
          ""
          "Records and verifies the GitHub ruleset used by the release gate."
          ""
          "parameters:"
          "  -Repository <owner/name>          (optional) defaults to `gh repo view`."
          "  -TargetBranch <branch>            (optional) defaults to the repository default branch."
          "  -RulesetId <id>                   (mandatory) the ruleset id."
          "  -ExpectedIntegrationId <id>       (mandatory) the GitHub App integration id (>= 1)."
          "  -OutputPath <path>                (mandatory) the evidence JSON path."
          "  -h, --help                        show this help." ]

let private tryParseInt64 (value: string) : int64 option =
    match Int64.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture) with
    | true, number -> Some number
    | _ -> None

let private splitParameter (argument: string) : string * string option =
    let separatorIndex = argument.IndexOfAny([| ':'; '=' |], 1)
    if separatorIndex > 1 then
        argument.Substring(0, separatorIndex), Some(argument.Substring(separatorIndex + 1))
    else
        argument, None

let private parseArgs (argv: string list) : Result<CliOptions, string> =
    let empty =
        { Repository = None
          TargetBranch = None
          RulesetId = None
          ExpectedIntegrationId = None
          OutputPath = None
          Help = false }

    let rec loop (options: CliOptions) (remaining: string list) =
        match remaining with
        | [] -> Ok options
        | "-h" :: _ | "--help" :: _ -> Ok { options with Help = true }
        | argument :: rest when argument.StartsWith("-") && argument.Length > 1 ->
            let name, inlineValue = splitParameter argument

            let takeValue (setter: string -> CliOptions) =
                match inlineValue, rest with
                | Some value, _ -> loop (setter value) rest
                | None, value :: rest' -> loop (setter value) rest'
                | None, [] -> Error(sprintf "missing an argument for parameter '%s'" name)

            match name.ToLowerInvariant() with
            | "-repository" -> takeValue (fun value -> { options with Repository = Some value })
            | "-targetbranch" -> takeValue (fun value -> { options with TargetBranch = Some value })
            | "-rulesetid" -> takeValue (fun value -> { options with RulesetId = tryParseInt64 value })
            | "-expectedintegrationid" ->
                takeValue (fun value -> { options with ExpectedIntegrationId = tryParseInt64 value })
            | "-outputpath" -> takeValue (fun value -> { options with OutputPath = Some value })
            | _ -> Error(sprintf "unknown parameter '%s'" argument)
        | argument :: _ -> Error(sprintf "unexpected argument '%s'" argument)

    loop empty argv

let private missingMandatory (options: CliOptions) : string list =
    [ if options.RulesetId.IsNone then
          yield "-RulesetId"
      if options.ExpectedIntegrationId.IsNone then
          yield "-ExpectedIntegrationId"
      if options.OutputPath.IsNone then
          yield "-OutputPath" ]

// ---------------------------------------------------------------------------
// Orchestration
// ---------------------------------------------------------------------------

let private verify
    (collaborators: Collaborators)
    (options: CliOptions)
    : Result<{| OutputPath: string; RulesetId: int64 |}, HarnessError> =
    let requestedRepository = Option.defaultValue "" options.Repository
    let requestedBranch = Option.defaultValue "" options.TargetBranch
    let rulesetId = Option.defaultValue 0L options.RulesetId
    let expectedIntegrationId = Option.defaultValue 0L options.ExpectedIntegrationId
    let outputPath = Option.defaultValue "" options.OutputPath

    if not (collaborators.HasGitHubCli ()) then
        failError "GitHub CLI is required to verify repository rules."
    elif expectedIntegrationId < 1L then
        failErrorWith 2 "Parameter '-ExpectedIntegrationId' must be in the range 1..9223372036854775807."
    else
        let repositoryArguments =
            [ "repo"; "view" ]
            @ (if String.IsNullOrWhiteSpace requestedRepository then
                   []
               else
                   [ requestedRepository ])
            @ [ "--json"; "nameWithOwner,defaultBranchRef" ]

        let repositoryResult = collaborators.RunGitHubCli repositoryArguments

        if repositoryResult.ExitCode <> 0 then
            failError "Could not resolve the GitHub repository and default branch."
        else
            let repositoryInfo =
                use document = JsonDocument.Parse(repositoryResult.Stdout)
                document.RootElement.Clone()

            let repository =
                if String.IsNullOrWhiteSpace requestedRepository then
                    stringMember "nameWithOwner" repositoryInfo |> Option.defaultValue ""
                else
                    requestedRepository

            let defaultBranch =
                tryMember "defaultBranchRef" repositoryInfo
                |> Option.bind (stringMember "name")
                |> Option.defaultValue ""

            let targetBranch =
                if String.IsNullOrWhiteSpace requestedBranch then
                    defaultBranch
                else
                    requestedBranch

            if String.IsNullOrWhiteSpace repository || String.IsNullOrWhiteSpace targetBranch then
                failError "Could not resolve the GitHub repository or target branch."
            elif not (String.Equals(targetBranch, defaultBranch, StringComparison.Ordinal)) then
                failError (
                    sprintf
                        "TargetBranch '%s' must be the repository default branch '%s'."
                        targetBranch
                        defaultBranch
                )
            else
                let rulesetResult =
                    collaborators.RunGitHubCli [ "api"; sprintf "repos/%s/rulesets/%d" repository rulesetId ]

                if rulesetResult.ExitCode <> 0 then
                    failError (sprintf "Could not read GitHub ruleset %d for '%s'." rulesetId repository)
                else
                    let ruleset =
                        use document = JsonDocument.Parse(rulesetResult.Stdout)
                        document.RootElement.Clone()

                    checkRuleset rulesetId expectedIntegrationId targetBranch ruleset
                    |> Result.bind (fun fields ->
                        let fullOutputPath = Path.GetFullPath outputPath

                        catchResult (fun () ->
                            match Path.GetDirectoryName fullOutputPath with
                            | null -> ()
                            | parent -> Directory.CreateDirectory parent |> ignore

                            let evidence =
                                serializeEvidence
                                    repository
                                    targetBranch
                                    expectedIntegrationId
                                    (DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture))
                                    fields

                            File.WriteAllText(fullOutputPath, evidence, UTF8Encoding(false)))
                        |> Result.map (fun () ->
                            {| OutputPath = fullOutputPath
                               RulesetId = rulesetId |}))

/// Run the ruleset verification writing to the supplied writers. Exit codes follow
/// the harness convention: 0 success, 1 verification failure, 2 usage failure.
let mainWith
    (stdout: TextWriter)
    (stderr: TextWriter)
    (collaborators: Collaborators)
    (argv: string list)
    : int =
    match parseArgs argv with
    | Error message ->
        stderr.WriteLine usageLine
        stderr.WriteLine("ERROR: " + sprintf "Verify-GitHubRuleset.ps1: %s" message)
        2
    | Ok options when options.Help ->
        stdout.WriteLine helpText
        0
    | Ok options ->
        match missingMandatory options with
        | missing when not missing.IsEmpty ->
            stderr.WriteLine usageLine
            stderr.WriteLine(
                "ERROR: "
                + sprintf "Missing mandatory parameter(s): %s." (String.Join(", ", missing))
            )
            2
        | _ ->
            match catchResult (fun () -> verify collaborators options) with
            | Ok (Ok outcome) ->
                stdout.WriteLine(
                    sprintf "Verified GitHub ruleset %d. Evidence: %s" outcome.RulesetId outcome.OutputPath
                )

                0
            | Ok (Error err)
            | Error err ->
                stderr.WriteLine("ERROR: " + err.Message)
                err.ExitCode

/// Entry point mirroring Verify-GitHubRuleset.ps1's main flow.
let main (argv: string array) : int =
    mainWith Console.Out Console.Error defaultCollaborators (List.ofArray argv)
