<#
.SYNOPSIS
Records and verifies the GitHub ruleset used by the Goal 13 release gate.
#>
[CmdletBinding()]
param(
    [string] $Repository,

    [string] $TargetBranch,

    [Parameter(Mandatory)]
    [long] $RulesetId,

    [Parameter(Mandatory)]
    [ValidateRange(1, [long]::MaxValue)]
    [long] $ExpectedIntegrationId,

    [Parameter(Mandatory)]
    [string] $OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw 'GitHub CLI is required to verify repository rules.'
}
$repositoryArgument = if ([string]::IsNullOrWhiteSpace($Repository)) { @() } else { @($Repository) }
$repositoryJson = & gh repo view @repositoryArgument --json nameWithOwner,defaultBranchRef
if ($LASTEXITCODE -ne 0) {
    throw 'Could not resolve the GitHub repository and default branch.'
}
$repositoryInfo = [string]::Join([Environment]::NewLine, @($repositoryJson)) | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($Repository)) {
    $Repository = [string] $repositoryInfo.nameWithOwner
}
if ([string]::IsNullOrWhiteSpace($TargetBranch)) {
    $TargetBranch = [string] $repositoryInfo.defaultBranchRef.name
}
if ([string]::IsNullOrWhiteSpace($Repository) -or [string]::IsNullOrWhiteSpace($TargetBranch)) {
    throw 'Could not resolve the GitHub repository or target branch.'
}
$defaultBranch = [string] $repositoryInfo.defaultBranchRef.name
if ($TargetBranch -cne $defaultBranch) {
    throw "TargetBranch '$TargetBranch' must be the repository default branch '$defaultBranch'."
}

$json = & gh api "repos/$Repository/rulesets/$RulesetId"
if ($LASTEXITCODE -ne 0) {
    throw "Could not read GitHub ruleset $RulesetId for '$Repository'."
}
$ruleset = [string]::Join([Environment]::NewLine, @($json)) | ConvertFrom-Json
if ($ruleset.enforcement -ne 'active') {
    throw "Ruleset $RulesetId is not active."
}
if ($ruleset.target -ne 'branch') {
    throw "Ruleset $RulesetId does not target branches."
}
$includedRefs = @($ruleset.conditions.ref_name.include | ForEach-Object { [string] $_ })
$excludedRefs = @($ruleset.conditions.ref_name.exclude | ForEach-Object { [string] $_ })
$targetRef = "refs/heads/$TargetBranch"
$includedTargetCount = @(@('~ALL', '~DEFAULT_BRANCH', $targetRef) | Where-Object { $includedRefs -contains $_ }).Count
if ($includedTargetCount -eq 0) {
    throw "Ruleset $RulesetId does not explicitly include '$targetRef' or the default branch."
}
if ($excludedRefs.Count -ne 0) {
    throw "Ruleset $RulesetId contains branch exclusions and cannot prove fail-closed default-branch coverage."
}

$required = @(
    'release / win-x64',
    'release / linux-x64',
    'release / osx-arm64',
    'release / osx-x64-consumer'
)
$statusRule = @($ruleset.rules | Where-Object type -eq 'required_status_checks')
if ($statusRule.Count -ne 1) {
    throw "Ruleset $RulesetId must contain exactly one required_status_checks rule."
}
$requiredChecks = @($statusRule[0].parameters.required_status_checks)
$actual = @($requiredChecks | ForEach-Object { [string] $_.context })
$missing = @($required | Where-Object { $actual -notcontains $_ })
if ($missing.Count -gt 0) {
    throw "Ruleset $RulesetId is missing required contexts: $($missing -join ', ')."
}
$bindings = foreach ($context in $required) {
    $matches = @($requiredChecks | Where-Object { [string] $_.context -ceq $context })
    if ($matches.Count -ne 1 -or [long] $matches[0].integration_id -ne $ExpectedIntegrationId) {
        throw "Ruleset $RulesetId must bind required context '$context' to GitHub App integration $ExpectedIntegrationId."
    }

    [pscustomobject] [ordered]@{
        context = $context
        integrationId = [long] $matches[0].integration_id
    }
}
if (@($ruleset.bypass_actors).Count -ne 0) {
    throw "Ruleset $RulesetId contains bypass actors and cannot support this candidate's PASS verdict."
}

$evidence = [pscustomobject] [ordered]@{
    schemaVersion = 1
    checkedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    repository = $Repository
    rulesetId = $ruleset.id
    rulesetName = $ruleset.name
    target = $ruleset.target
    targetBranch = $TargetBranch
    expectedIntegrationId = $ExpectedIntegrationId
    refConditions = $ruleset.conditions.ref_name
    enforcement = $ruleset.enforcement
    updatedAt = $ruleset.updated_at
    bypassActors = @($ruleset.bypass_actors)
    requiredContexts = @($actual | Sort-Object)
    requiredChecks = @($bindings)
    requiredContextsSatisfied = $true
}

$fullOutputPath = [IO.Path]::GetFullPath($OutputPath)
[IO.Directory]::CreateDirectory((Split-Path -Parent $fullOutputPath)) | Out-Null
[IO.File]::WriteAllText(
    $fullOutputPath,
    ($evidence | ConvertTo-Json -Depth 10) + [Environment]::NewLine,
    [Text.UTF8Encoding]::new($false))
Write-Output "Verified GitHub ruleset $RulesetId. Evidence: $fullOutputPath"
