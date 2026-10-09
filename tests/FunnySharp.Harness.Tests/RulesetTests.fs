module FunnySharp.Harness.Tests.RulesetTests

// xUnit port of the ruleset cases in eng/tests/ReleaseProtocol.Tests.ps1. All the
// ruleset logic is exercised directly through checkRuleset with synthetic JSON and
// through mainWith with an injected fake `gh`; nothing touches the network.

open System
open System.IO
open System.Text.Json
open System.Text.Json.Nodes
open Xunit
open FunnySharp.Harness.Proc
open FunnySharp.Harness.Ruleset
open FunnySharp.Harness.Tests.Support

let private statusRule =
    "{\"type\":\"required_status_checks\",\"parameters\":{\"strict_required_status_checks_policy\":true,\"required_status_checks\":[{\"context\":\"release / win-x64\",\"integration_id\":12345},{\"context\":\"release / linux-x64\",\"integration_id\":12345},{\"context\":\"release / osx-arm64\",\"integration_id\":12345},{\"context\":\"release / osx-x64-consumer\",\"integration_id\":12345}]}}"

let private validRuleset =
    "{\"id\":42,\"name\":\"release-gate\",\"target\":\"branch\",\"enforcement\":\"active\",\"updated_at\":\"2026-09-01T00:00:00Z\",\"conditions\":{\"ref_name\":{\"include\":[\"~DEFAULT_BRANCH\"],\"exclude\":[]}},\"rules\":["
    + statusRule
    + "],\"bypass_actors\":[]}"

let private repositoryInfo =
    "{\"defaultBranchRef\":{\"name\":\"main\"},\"nameWithOwner\":\"wxxb789/funnysharp\"}"

let private parse (json: string) : JsonElement =
    use document = JsonDocument.Parse json
    document.RootElement.Clone()

let private check (json: string) : Result<RulesetFields, FunnySharp.Harness.Output.HarnessError> =
    checkRuleset 42L 12345L "main" (parse json)

let private collaboratorsWith (ruleset: string) : Collaborators =
    { HasGitHubCli = fun () -> true
      RunGitHubCli =
        fun arguments ->
            if arguments |> List.contains "view" then
                { ExitCode = 0; Stdout = repositoryInfo; Stderr = "" }
            else
                { ExitCode = 0; Stdout = ruleset; Stderr = "" } }

let private runMain (collaborators: Collaborators) (arguments: string list) : int * string * string =
    use stdout = new StringWriter()
    use stderr = new StringWriter()
    let exitCode = mainWith stdout stderr collaborators arguments
    exitCode, stdout.ToString(), stderr.ToString()

let private rejectionOf (result: Result<'T, FunnySharp.Harness.Output.HarnessError>) =
    match result with
    | Ok _ -> failwith "Expected a rejected ruleset."
    | Error err ->
        Assert.Equal(1, err.ExitCode)
        err

type StrictPolicyTests() =

    [<Fact>]
    member _.AssertStrictRequiredStatusChecksPolicy_Missing_Rejects() =
        let result = assertStrictRequiredStatusChecksPolicy (parse "{}")
        rejectionOf result |> ignore

    [<Fact>]
    member _.VerifyGitHubRuleset_DisabledStrictPolicy_Rejects() =
        let result = assertStrictRequiredStatusChecksPolicy (parse "{\"strict_required_status_checks_policy\":false}")
        rejectionOf result |> ignore

    [<Fact>]
    member _.VerifyGitHubRuleset_EnabledStrictPolicy_Passes() =
        let result = assertStrictRequiredStatusChecksPolicy (parse "{\"strict_required_status_checks_policy\":true}")
        Assert.True(result.IsOk)

    [<Fact>]
    member _.VerifyGitHubRuleset_MissingStrictPolicy_Rejects() =
        let json = validRuleset.Replace("\"strict_required_status_checks_policy\":true,", "")
        rejectionOf (check json) |> ignore

type RulesetCheckTests() =

    [<Theory>]
    [<InlineData("conditions")>]
    [<InlineData("parameters")>]
    [<InlineData("bypass_actors")>]
    member _.CheckRuleset_MissingPropertiesKeepTheirDefaultValidation(field: string) =
        let ruleset = Assert.IsType<JsonObject>(JsonNode.Parse validRuleset)
        let owner =
            if field = "parameters" then
                let rules = Assert.IsType<JsonArray>(ruleset.["rules"])
                Assert.IsType<JsonObject>(rules.[0])
            else
                ruleset
        Assert.True(owner.Remove field)

        match field, check (ruleset.ToJsonString()) with
        | "bypass_actors", Ok fields ->
            Assert.Equal(JsonValueKind.Array, fields.BypassActors.ValueKind)
            Assert.Equal(0, fields.BypassActors.GetArrayLength())
        | "conditions", Error err ->
            Assert.Equal(1, err.ExitCode)
            Assert.Contains("refs/heads/main", err.Message)
        | "parameters", Error err ->
            let expected = rejectionOf (assertStrictRequiredStatusChecksPolicy (parse "{}"))
            Assert.Equal(expected, err)
        | _ -> Assert.Fail "Unexpected default validation result."

    [<Fact>]
    member _.CheckRuleset_ValidRuleset_Passes() =
        match check validRuleset with
        | Error err -> failwith err.Message
        | Ok fields ->
            Assert.Equal(4, fields.RequiredChecks.Length)
            Assert.Equal<(string * int64) list>(
                [ "release / win-x64", 12345L
                  "release / linux-x64", 12345L
                  "release / osx-arm64", 12345L
                  "release / osx-x64-consumer", 12345L ],
                fields.RequiredChecks
            )

            Assert.Equal<string list>(
                [ "release / linux-x64"
                  "release / osx-arm64"
                  "release / osx-x64-consumer"
                  "release / win-x64" ],
                fields.RequiredContextsObserved
            )

    [<Fact>]
    member _.CheckRuleset_NonActiveEnforcement_Rejects() =
        let json = validRuleset.Replace("\"enforcement\":\"active\"", "\"enforcement\":\"evaluate\"")
        rejectionOf (check json) |> ignore

    [<Fact>]
    member _.CheckRuleset_NonBranchTarget_Rejects() =
        let json = validRuleset.Replace("\"target\":\"branch\"", "\"target\":\"tag\"")
        rejectionOf (check json) |> ignore

    [<Fact>]
    member _.CheckRuleset_MissingRequiredContext_Rejects() =
        let json =
            validRuleset.Replace("{\"context\":\"release / osx-arm64\",\"integration_id\":12345},", "")

        Assert.Contains("release / osx-arm64", (rejectionOf (check json)).Message)

    [<Fact>]
    member _.CheckRuleset_WrongIntegrationId_Rejects() =
        let json =
            validRuleset.Replace(
                "{\"context\":\"release / win-x64\",\"integration_id\":12345}",
                "{\"context\":\"release / win-x64\",\"integration_id\":999}"
            )

        let error = rejectionOf (check json)
        Assert.Contains("release / win-x64", error.Message)
        Assert.Contains("12345", error.Message)

    [<Fact>]
    member _.CheckRuleset_BypassActor_Rejects() =
        let json = validRuleset.Replace("\"bypass_actors\":[]", "\"bypass_actors\":[{\"actor_id\":1}]")

        rejectionOf (check json) |> ignore

    [<Fact>]
    member _.CheckRuleset_ExcludedRef_Rejects() =
        let json = validRuleset.Replace("\"exclude\":[]", "\"exclude\":[\"refs/heads/release\"]")

        rejectionOf (check json) |> ignore

    [<Fact>]
    member _.CheckRuleset_TargetRefNotIncluded_Rejects() =
        let json = validRuleset.Replace("\"include\":[\"~DEFAULT_BRANCH\"]", "\"include\":[\"refs/heads/other\"]")

        Assert.Contains("refs/heads/main", (rejectionOf (check json)).Message)

    [<Fact>]
    member _.CheckRuleset_TwoStatusRules_Rejects() =
        let json = validRuleset.Replace("\"rules\":[", "\"rules\":[" + statusRule + ",")

        rejectionOf (check json) |> ignore

type CommandLineTests() =

    [<Fact>]
    member _.VerifyGitHubRuleset_RecordsStrictPolicyEvidence() =
        use output = new TempDirectory()
        let outputPath = Path.Combine(output.Path, "ruleset.json")

        let exitCode, stdout, stderr =
            runMain
                (collaboratorsWith validRuleset)
                [ "-RulesetId"; "42"; "-ExpectedIntegrationId"; "12345"; "-OutputPath"; outputPath ]

        Assert.Equal(0, exitCode)
        Assert.Equal("", stderr)
        Assert.Matches(@"\b42\b", stdout)
        Assert.Contains(outputPath, stdout)

        use document = JsonDocument.Parse(File.ReadAllText outputPath)
        let root = document.RootElement
        Assert.True(root.GetProperty("strictRequiredStatusChecksPolicy").GetBoolean())
        Assert.True(root.GetProperty("requiredContextsSatisfied").GetBoolean())
        Assert.Equal(4, root.GetProperty("requiredContexts").GetArrayLength())
        Assert.Equal(4, root.GetProperty("requiredChecks").GetArrayLength())
        Assert.Equal(42, root.GetProperty("rulesetId").GetInt32())

    [<Fact>]
    member _.VerifyGitHubRuleset_NonActiveRuleset_Rejects() =
        let json = validRuleset.Replace("\"enforcement\":\"active\"", "\"enforcement\":\"disabled\"")
        use output = new TempDirectory()

        let exitCode, _, stderr =
            runMain
                (collaboratorsWith json)
                [ "-RulesetId"; "42"; "-ExpectedIntegrationId"; "12345"; "-OutputPath"; Path.Combine(output.Path, "r.json") ]

        Assert.Equal(1, exitCode)
        Assert.Contains("42", stderr)

    [<Fact>]
    member _.VerifyGitHubRuleset_MissingMandatoryParameter_UsageError() =
        let exitCode, _, stderr = runMain (collaboratorsWith validRuleset) [ "-RulesetId"; "42" ]
        Assert.Equal(2, exitCode)
        Assert.Contains("Missing mandatory parameter(s): -ExpectedIntegrationId, -OutputPath.", stderr)

    [<Fact>]
    member _.VerifyGitHubRuleset_ExpectedIntegrationIdMustBePositive() =
        use output = new TempDirectory()

        let exitCode, _, stderr =
            runMain
                (collaboratorsWith validRuleset)
                [ "-RulesetId"; "42"
                  "-ExpectedIntegrationId"; "0"
                  "-OutputPath"; Path.Combine(output.Path, "r.json") ]

        Assert.Equal(2, exitCode)
        Assert.Contains("must be in the range 1..9223372036854775807", stderr)

    [<Fact>]
    member _.VerifyGitHubRuleset_MissingCli_Rejects() =
        use output = new TempDirectory()

        let collaborators =
            { HasGitHubCli = fun () -> false
              RunGitHubCli = fun _ -> { ExitCode = 0; Stdout = ""; Stderr = "" } }

        let exitCode, _, stderr =
            runMain
                collaborators
                [ "-RulesetId"; "42"
                  "-ExpectedIntegrationId"; "12345"
                  "-OutputPath"; Path.Combine(output.Path, "r.json") ]

        Assert.Equal(1, exitCode)
        Assert.Contains("GitHub CLI is required to verify repository rules.", stderr)

    [<Fact>]
    member _.VerifyGitHubRuleset_RepositoryResolutionFailure_Rejects() =
        use output = new TempDirectory()

        let collaborators =
            { HasGitHubCli = fun () -> true
              RunGitHubCli = fun _ -> { ExitCode = 1; Stdout = ""; Stderr = "boom" } }

        let exitCode, _, stderr =
            runMain
                collaborators
                [ "-RulesetId"; "42"
                  "-ExpectedIntegrationId"; "12345"
                  "-OutputPath"; Path.Combine(output.Path, "r.json") ]

        Assert.Equal(1, exitCode)
        Assert.Contains("Could not resolve the GitHub repository and default branch.", stderr)
