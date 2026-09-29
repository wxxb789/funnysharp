module FunnySharp.Harness.Tests.CheckActionPinsTests

open System
open System.IO
open Xunit
open FunnySharp.Harness.ActionPins
open FunnySharp.Harness.Tests.Support

let private checkFixture (workflows: (string * string) list) (uvPin: bool) : string list * Finding list =
    use temp = new TempDirectory()
    writeWorkflows temp.Path workflows
    if uvPin then writeUvPin temp.Path
    let scanned, findings = checkRepository temp.Path
    scanned |> List.map fileName, findings

let private runMain (argv: string list) : int * string * string =
    use stdout = new StringWriter()
    use stderr = new StringWriter()
    let exitCode = mainWith stdout stderr argv
    exitCode, stdout.ToString(), stderr.ToString()

type PinCheckFixtureTests() =

    [<Fact>]
    member _.TagReferenceFails() =
        let text = workflowText [ step [ "name: Checkout"; "uses: actions/checkout@v4" ] ]
        let scanned, findings = checkFixture [ "tags.yml", text ] false
        Assert.Equal<string list>([ "tags.yml" ], scanned)
        Assert.Equal(1, findings.Length)
        Assert.Equal(lineOf text "uses:", findings.[0].Line)
        Assert.Contains("actions/checkout@v4", findings.[0].Message)
        Assert.Contains("40-hex", findings.[0].Message)

    [<Fact>]
    member _.FullShaWithoutVersionCommentFails() =
        let text =
            workflowText [ step [ "name: Checkout"; "uses: actions/checkout@" + checkoutSha ] ]

        let _, findings = checkFixture [ "sha.yml", text ] false
        Assert.Equal(1, findings.Length)
        Assert.Contains("actions/checkout@" + checkoutSha, findings.[0].Message)
        Assert.Contains("comment", findings.[0].Message)

    [<Fact>]
    member _.UnpinnedActionFromOtherOwnerFails() =
        let text =
            workflowText [ step [ "name: Set up uv"; "uses: astral-sh/setup-uv@v10" ] ]

        let _, findings = checkFixture [ "other.yml", text ] false
        Assert.Equal(1, findings.Length)
        Assert.Contains("astral-sh/setup-uv@v10", findings.[0].Message)
        Assert.Contains("40-hex", findings.[0].Message)

    [<Fact>]
    member _.LocalActionPasses() =
        let text = workflowText [ step [ "name: Local"; "uses: ./.github/actions/build" ] ]
        let scanned, findings = checkFixture [ "local.yml", text ] false
        Assert.Equal<string list>([ "local.yml" ], scanned)
        Assert.Empty findings

    [<Fact>]
    member _.FullyValidFixturePasses() =
        let text =
            workflowText
                [ step [ "name: Checkout"; "uses: actions/checkout@" + checkoutSha + " # v4" ]
                  step [ "name: Set up .NET"; "uses: actions/setup-dotnet@" + setupDotnetSha + " # v4" ]
                  step [ "name: Local"; "uses: ./eng/actions/local" ] ]

        let scanned, findings = checkFixture [ "valid.yml", text ] false
        Assert.Equal<string list>([ "valid.yml" ], scanned)
        Assert.Empty findings

    [<Fact>]
    member _.SetupUvExplicitVersionFails() =
        let text =
            workflowText
                [ step
                      [ "name: Set up uv"
                        "uses: astral-sh/setup-uv@" + setupUvSha + " # v10.1.0"
                        "with:"
                        "  version: \"0.12.16\"" ] ]

        let _, findings = checkFixture [ "tooling.yml", text ] true
        Assert.Equal(1, findings.Length)
        Assert.Equal(lineOf text "version:", findings.[0].Line)
        Assert.Contains("must not set 'version:'", findings.[0].Message)
        Assert.Contains("uv.toml", findings.[0].Message)

    [<Fact>]
    member _.SetupUvVersionBeforeUsesFails() =
        let text =
            workflowText
                [ step
                      [ "name: Set up uv"
                        "with:"
                        "  version: \"0.12.16\""
                        "uses: astral-sh/setup-uv@" + setupUvSha + " # v10.1.0" ] ]

        let _, findings = checkFixture [ "tooling.yml", text ] true
        Assert.Equal(1, findings.Length)
        Assert.Equal(lineOf text "version:", findings.[0].Line)
        Assert.Contains("must not set 'version:'", findings.[0].Message)

    [<Fact>]
    member _.SetupUvFlowMappingVersionFails() =
        let text =
            workflowText
                [ step
                      [ "name: Set up uv"
                        "uses: astral-sh/setup-uv@" + setupUvSha + " # v10.1.0"
                        "with: {version: \"0.12.16\"}" ] ]

        let _, findings = checkFixture [ "tooling.yml", text ] true
        Assert.Equal(1, findings.Length)
        Assert.Equal(lineOf text "version:", findings.[0].Line)
        Assert.Contains("must not set 'version:'", findings.[0].Message)

    [<Fact>]
    member _.SetupUvQuotedVersionKeyFails() =
        let text =
            workflowText
                [ step
                      [ "name: Set up uv"
                        "uses: astral-sh/setup-uv@" + setupUvSha + " # v10.1.0"
                        "with:"
                        "\"version\": \"0.12.16\"" ] ]

        let _, findings = checkFixture [ "tooling.yml", text ] true
        Assert.Equal(1, findings.Length)
        Assert.Equal(lineOf text "\"version\":", findings.[0].Line)
        Assert.Contains("must not set 'version:'", findings.[0].Message)

    [<Fact>]
    member _.SetupUvWithoutVersionPasses() =
        let text =
            workflowText
                [ step
                      [ "name: Set up uv"
                        "uses: astral-sh/setup-uv@" + setupUvSha + " # v10.1.0"
                        "with:"
                        "enable-cache: false" ] ]

        let scanned, findings = checkFixture [ "tooling.yml", text ] true
        Assert.Equal<string list>([ "tooling.yml" ], scanned)
        Assert.Empty findings

    [<Fact>]
    member _.SetupUvVersionInAnotherStepPasses() =
        let text =
            workflowText
                [ step [ "name: Set up uv"; "uses: astral-sh/setup-uv@" + setupUvSha + " # v10.1.0" ]
                  step [ "name: Other"; "with:"; "  version: \"1.2.3\"" ] ]

        let scanned, findings = checkFixture [ "tooling.yml", text ] true
        Assert.Equal<string list>([ "tooling.yml" ], scanned)
        Assert.Empty findings

    [<Fact>]
    member _.ContinuationUsesValueFails() =
        let text = workflowText [ continuationStep "Checkout" "actions/checkout@v4" ]
        use temp = new TempDirectory()
        writeWorkflows temp.Path [ "continuation.yml", text ]
        let scanned, findings = checkRepository temp.Path
        let formatted = findings |> List.map (fun finding -> finding.Format temp.Path)
        Assert.Equal<string list>([ "continuation.yml" ], scanned |> List.map fileName)
        let line = lineOf text "uses:"

        Assert.Equal<string list>(
            [ sprintf
                  ".github/workflows/continuation.yml:%d: uses reference '' must be pinned as owner/repo@<40-hex-sha> with a '# <version>' comment"
                  line ],
            formatted
        )

    [<Fact>]
    member _.SetupUvWithoutUvPinFails() =
        let text =
            workflowText
                [ step [ "name: Set up uv"; "uses: astral-sh/setup-uv@" + setupUvSha + " # v10.1.0" ] ]

        let _, findings = checkFixture [ "tooling.yml", text ] false
        Assert.Equal(1, findings.Length)
        Assert.Contains("uv.toml", findings.[0].Message)
        Assert.Contains("required-version", findings.[0].Message)

    [<Fact>]
    member _.SetupUvRangePinFails() =
        let text =
            workflowText
                [ step [ "name: Set up uv"; "uses: astral-sh/setup-uv@" + setupUvSha + " # v10.1.0" ] ]

        use temp = new TempDirectory()
        writeWorkflows temp.Path [ "tooling.yml", text ]
        File.WriteAllText(Path.Combine(temp.Path, "uv.toml"), "required-version = \">=0.12\"\n")
        let _, findings = checkRepository temp.Path
        Assert.Equal(1, findings.Length)
        Assert.Contains("required-version", findings.[0].Message)
        Assert.Contains("exact", findings.[0].Message)

    [<Fact>]
    member _.SetupUvValuelessPinFails() =
        let text =
            workflowText
                [ step [ "name: Set up uv"; "uses: astral-sh/setup-uv@" + setupUvSha + " # v10.1.0" ] ]

        use temp = new TempDirectory()
        writeWorkflows temp.Path [ "tooling.yml", text ]
        File.WriteAllText(Path.Combine(temp.Path, "uv.toml"), "required-version =\n")
        let _, findings = checkRepository temp.Path
        Assert.Equal(1, findings.Length)
        Assert.Contains("required-version", findings.[0].Message)

    [<Fact>]
    member _.ReleaseWorkflowActionsOwnerIsDelegated() =
        let releaseText = workflowText [ step [ "name: Checkout"; "uses: actions/checkout@v4" ] ]
        let _, findings = checkFixture [ "release.yml", releaseText ] false
        // actions/* in release.yml stays with the frozen PowerShell protocol test.
        Assert.Empty findings

    [<Fact>]
    member _.ReleaseWorkflowThirdPartyTagFails() =
        let releaseText =
            workflowText [ step [ "name: Release"; "uses: softprops/action-gh-release@v2" ] ]

        let _, findings = checkFixture [ "release.yml", releaseText ] false
        Assert.Equal(1, findings.Length)
        Assert.Contains("softprops/action-gh-release@v2", findings.[0].Message)
        Assert.Contains("40-hex", findings.[0].Message)

    [<Fact>]
    member _.ReleaseWorkflowThirdPartyPinnedPasses() =
        let releaseText =
            workflowText
                [ step [ "name: Release"; "uses: softprops/action-gh-release@" + checkoutSha + " # v2" ] ]

        let scanned, findings = checkFixture [ "release.yml", releaseText ] false
        Assert.Equal<string list>([ "release.yml" ], scanned)
        Assert.Empty findings

type RealWorkflowTests() =

    [<Fact>]
    member _.RealWorkflowsPass() =
        let root = repositoryRoot ()
        let scanned, findings = checkRepository root
        Assert.Equal<string list>([], findings |> List.map (fun finding -> finding.Format root))
        let names = scanned |> List.map fileName
        Assert.Contains("tooling.yml", names)
        Assert.Contains("release.yml", names)

type CommandLineTests() =

    [<Fact>]
    member _.FindingsExitNonzeroWithFileAndLine() =
        let text = workflowText [ step [ "name: Checkout"; "uses: actions/checkout@v4" ] ]
        use temp = new TempDirectory()
        writeWorkflows temp.Path [ "tags.yml", text ]
        let exitCode, _, stderr = runMain [ "--repository-root"; temp.Path ]
        Assert.Equal(1, exitCode)
        Assert.Contains(".github/workflows/tags.yml:9: ", stderr)
        Assert.Contains("actions/checkout@v4", stderr)
        Assert.Contains("FAIL:", stderr)

    [<Fact>]
    member _.VerboseListsScannedFiles() =
        let text =
            workflowText [ step [ "name: Checkout"; "uses: actions/checkout@" + checkoutSha + " # v4" ] ]

        use temp = new TempDirectory()
        writeWorkflows temp.Path [ "valid.yml", text ]
        let exitCode, stdout, stderr = runMain [ "--verbose"; "--repository-root"; temp.Path ]
        Assert.Equal(0, exitCode)
        Assert.Equal("", stderr)
        Assert.Contains("checked .github/workflows/valid.yml", stdout)
        Assert.Contains("OK:", stdout)

    [<Fact>]
    member _.MissingWorkflowsDirectoryIsAnEnvironmentFailure() =
        use temp = new TempDirectory()
        let exitCode, _, stderr = runMain [ "--repository-root"; temp.Path ]
        Assert.Equal(2, exitCode)
        Assert.Contains("was not found", stderr)

type ToolingWorkflowStructureTests() =

    member private _.ToolingText() =
        File.ReadAllText(Path.Combine(repositoryRoot (), ".github", "workflows", "tooling.yml"))

    [<Fact>]
    member this.WorkflowIsNotNamedRelease() =
        Assert.StartsWith("name: tooling\n", this.ToolingText())

    [<Fact>]
    member this.MatrixGateJobIsStable() =
        Assert.Contains("\n  tooling-gate:\n", this.ToolingText())
        Assert.Contains("needs: tooling\n", this.ToolingText())

    [<Fact>]
    member this.WorkflowDoesNotShadowRequiredReleaseContexts() =
        let text = this.ToolingText()

        for jobId in [ "win-x64"; "linux-x64"; "osx-arm64"; "osx-x64-consumer" ] do
            Assert.DoesNotContain("\n  " + jobId + ":\n", text)
