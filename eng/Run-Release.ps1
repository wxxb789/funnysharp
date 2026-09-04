<#
.SYNOPSIS
Runs the FunnySharp release candidate pipeline and records immutable execution evidence.

.DESCRIPTION
Runs every release gate from one source fingerprint. Command output, exit codes,
and per-command receipts are written beneath an ignored artifacts subdirectory.
The final verifier binds the package and API evidence to this execution receipt.

.EXAMPLE
pwsh -NoProfile -File eng/Run-Release.ps1 -AttemptId local-full-1 `
  -CompatibilityRuntimeIdentifier win-x64 `
  -CompatibilityPackageFeed https://packagefeedproxy.microsoft.io/nuget/v3/index.json `
  -DistributionFeed https://packagefeedproxy.microsoft.io/nuget/v3/index.json
#>
[CmdletBinding()]
param(
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot),

    [string] $OutputDirectory,

    [Parameter(Mandatory)]
    [string] $AttemptId,

    [string] $CompatibilityRuntimeIdentifier = [System.Runtime.InteropServices.RuntimeInformation]::RuntimeIdentifier,

    [string] $CompatibilityPackageFeed = 'https://api.nuget.org/v3/index.json',

    [string[]] $DistributionFeed = @('https://api.nuget.org/v3/index.json'),

    [switch] $SkipBenchmarks
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'ReleaseProtocol.psm1') -Force

function Resolve-FullPath {
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [string] $BasePath
    )

    if ([System.IO.Path]::IsPathFullyQualified($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $BasePath $Path))
}

function Get-PathComparison {
    if ($IsWindows) {
        return [System.StringComparison]::OrdinalIgnoreCase
    }

    return [System.StringComparison]::Ordinal
}

function Test-PathAtOrWithin {
    param(
        [Parameter(Mandatory)] [string] $ChildPath,
        [Parameter(Mandatory)] [string] $ParentPath
    )

    $comparison = Get-PathComparison
    $separator = [System.IO.Path]::DirectorySeparatorChar
    $normalizedParent = $ParentPath.TrimEnd('\', '/') + $separator
    return $ChildPath.Equals($ParentPath, $comparison) -or $ChildPath.StartsWith($normalizedParent, $comparison)
}

function Assert-SafeArtifactsSubdirectory {
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [string] $ArtifactsDirectory
    )

    $artifactsItem = Get-Item -LiteralPath $ArtifactsDirectory -Force
    if (($artifactsItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "ArtifactsDirectory '$($artifactsItem.FullName)' cannot be a reparse point."
    }

    $resolvedArtifacts = (Resolve-Path -LiteralPath $artifactsItem.FullName).Path
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if ($fullPath.Equals($resolvedArtifacts, (Get-PathComparison)) -or
        -not (Test-PathAtOrWithin -ChildPath $fullPath -ParentPath $resolvedArtifacts)) {
        throw "Path must be a proper subdirectory of '$resolvedArtifacts': '$fullPath'."
    }

    $relative = [System.IO.Path]::GetRelativePath($resolvedArtifacts, $fullPath)
    $current = $resolvedArtifacts
    foreach ($segment in $relative.Split(
            @([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar),
            [System.StringSplitOptions]::RemoveEmptyEntries)) {
        $current = Join-Path $current $segment
        if (-not (Test-Path -LiteralPath $current)) {
            continue
        }

        $item = Get-Item -LiteralPath $current -Force
        if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Path cannot contain a reparse point: '$($item.FullName)'."
        }

        $resolvedCurrent = (Resolve-Path -LiteralPath $item.FullName).Path
        if (-not (Test-PathAtOrWithin -ChildPath $resolvedCurrent -ParentPath $resolvedArtifacts)) {
            throw "Resolved path escapes '$resolvedArtifacts': '$resolvedCurrent'."
        }
        $current = $resolvedCurrent
    }

    return $fullPath
}

function Initialize-OutputDirectory {
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [string] $ArtifactsDirectory
    )

    $Path = Assert-SafeArtifactsSubdirectory -Path $Path -ArtifactsDirectory $ArtifactsDirectory
    if (Test-Path -LiteralPath $Path) {
        throw "OutputDirectory '$Path' already exists; release attempts are immutable."
    }

    [System.IO.Directory]::CreateDirectory($Path) | Out-Null
    return $Path
}

function Write-JsonFile {
    param(
        [Parameter(Mandatory)] $Value,
        [Parameter(Mandatory)] [string] $Path
    )

    $json = $Value | ConvertTo-Json -Depth 100
    [System.IO.File]::WriteAllText($Path, $json + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
}

function Get-Sha256 {
    param([Parameter(Mandatory)] [string] $Path)

    $stream = [System.IO.File]::OpenRead($Path)
    $hashAlgorithm = [System.Security.Cryptography.SHA256]::Create()
    try {
        return [System.Convert]::ToHexString($hashAlgorithm.ComputeHash($stream)).ToLowerInvariant()
    }
    finally {
        $hashAlgorithm.Dispose()
        $stream.Dispose()
    }
}

function Invoke-ExternalProcess {
    param(
        [Parameter(Mandatory)] [string] $FileName,
        [string[]] $Arguments = @(),
        [Parameter(Mandatory)] [string] $WorkingDirectory
    )

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $FileName
    $startInfo.WorkingDirectory = $WorkingDirectory
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    foreach ($argument in $Arguments) {
        [void] $startInfo.ArgumentList.Add($argument)
    }

    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    try {
        [void] $process.Start()
        $standardOutputTask = $process.StandardOutput.ReadToEndAsync()
        $standardErrorTask = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        return [pscustomobject] [ordered]@{
            exitCode = $process.ExitCode
            standardOutput = $standardOutputTask.GetAwaiter().GetResult()
            standardError = $standardErrorTask.GetAwaiter().GetResult()
        }
    }
    finally {
        $process.Dispose()
    }
}

function Get-SourceFingerprint {
    param([Parameter(Mandatory)] [string] $Root)

    $result = Invoke-ExternalProcess -FileName 'git' -Arguments @('ls-files', '--cached', '--others', '--exclude-standard', '-z') -WorkingDirectory $Root
    if ($result.exitCode -ne 0) {
        throw "git ls-files failed with exit code $($result.exitCode): $($result.standardError.Trim())"
    }

    $files = [System.Collections.Generic.List[object]]::new()
    foreach ($relativePath in ($result.standardOutput -split [char] 0)) {
        if ([string]::IsNullOrEmpty($relativePath)) {
            continue
        }

        $fullPath = [System.IO.Path]::GetFullPath((Join-Path $Root $relativePath))
        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
            throw "Source file listed by git was not found: '$relativePath'."
        }

        $files.Add([pscustomobject] [ordered]@{
                path = $relativePath.Replace('\', '/')
                sha256 = Get-Sha256 -Path $fullPath
            })
    }

    $orderedFiles = @($files | Sort-Object path)
    $canonicalLines = $orderedFiles | ForEach-Object { $_.path + [char] 0 + $_.sha256 + "`n" }
    $digestBytes = [System.Text.Encoding]::UTF8.GetBytes(($canonicalLines -join ''))
    $hashAlgorithm = [System.Security.Cryptography.SHA256]::Create()
    try {
        $digest = [System.Convert]::ToHexString($hashAlgorithm.ComputeHash($digestBytes)).ToLowerInvariant()
    }
    finally {
        $hashAlgorithm.Dispose()
    }

    return [pscustomobject] [ordered]@{
        schemaVersion = 1
        algorithm = 'sha256'
        fileCount = $orderedFiles.Count
        digest = $digest
        files = $orderedFiles
    }
}

function Test-EquivalentSourceFingerprint {
    param(
        [Parameter(Mandatory)] $Left,
        [Parameter(Mandatory)] $Right
    )

    return $Left.schemaVersion -eq 1 -and
        $Right.schemaVersion -eq 1 -and
        $Left.algorithm -eq 'sha256' -and
        $Right.algorithm -eq 'sha256' -and
        $Left.fileCount -eq $Right.fileCount -and
        $Left.digest -eq $Right.digest
}

function Get-GitText {
    param(
        [Parameter(Mandatory)] [string[]] $Arguments,
        [Parameter(Mandatory)] [string] $Root
    )

    $result = Invoke-ExternalProcess -FileName 'git' -Arguments $Arguments -WorkingDirectory $Root
    if ($result.exitCode -ne 0) {
        throw "git $($Arguments -join ' ') failed: $($result.standardError.Trim())"
    }

    return $result.standardOutput.Trim()
}

function Get-PackageVersion {
    param([Parameter(Mandatory)] [string] $ProjectPath)

    [xml] $project = Get-Content -LiteralPath $ProjectPath -Raw
    $version = [string] $project.Project.PropertyGroup.VersionPrefix
    if ([string]::IsNullOrWhiteSpace($version)) {
        throw "Project '$ProjectPath' does not declare VersionPrefix."
    }

    return $version
}

function Get-PackageVersionState {
    param(
        [Parameter(Mandatory)] [string] $Feed,
        [Parameter(Mandatory)] [string] $PackageId,
        [Parameter(Mandatory)] [string] $Version
    )

    try {
        $serviceResponse = Invoke-WebRequest -Uri $Feed -UseBasicParsing
    }
    catch {
        throw "Distribution feed '$Feed' is inaccessible: $($_.Exception.Message)"
    }

    $service = $serviceResponse.Content | ConvertFrom-Json
    $baseResources = @($service.resources | Where-Object {
            [string] $_.'@type' -match '^PackageBaseAddress/3\.0\.0'
        })
    if ($baseResources.Count -ne 1 -or [string]::IsNullOrWhiteSpace([string] $baseResources[0].'@id')) {
        throw "Distribution feed '$Feed' has an ambiguous PackageBaseAddress resource."
    }

    $packageIndex = ([string] $baseResources[0].'@id').TrimEnd('/') + '/' +
        $PackageId.ToLowerInvariant() + '/index.json'
    $statusCode = 200
    $content = $null
    try {
        $packageResponse = Invoke-WebRequest -Uri $packageIndex -UseBasicParsing
        $content = [string] $packageResponse.Content
    }
    catch {
        $response = $_.Exception.Response
        if ($null -ne $response -and [int] $response.StatusCode -eq 404) {
            $statusCode = 404
            $content = '{"versions":[]}'
        }
        else {
            throw "Package version state for '$PackageId' at '$Feed' is inaccessible: $($_.Exception.Message)"
        }
    }

    $parsed = $content | ConvertFrom-Json
    if ($null -eq $parsed.versions) {
        throw "Package version state for '$PackageId' at '$Feed' is ambiguous."
    }
    $versions = @($parsed.versions | ForEach-Object { [string] $_ })
    Assert-PackageVersionAbsent -PackageId $PackageId -Version $Version -Versions $versions

    return [pscustomobject] [ordered]@{
        checkedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
        feed = $Feed
        packageBaseAddress = [string] $baseResources[0].'@id'
        packageId = $PackageId
        version = $Version
        status = 'absent'
        statusCode = $statusCode
        responseSha256 = [Convert]::ToHexString(
            [System.Security.Cryptography.SHA256]::HashData(
                [System.Text.Encoding]::UTF8.GetBytes($content))).ToLowerInvariant()
        versions = $versions
    }
}

function Remove-ProjectGeneratedOutputs {
    param([Parameter(Mandatory)] [string] $Root)

    $projectFiles = @((Get-GitText -Arguments @('ls-files', '*.csproj') -Root $Root) -split '\r?\n' |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    $outputs = @(Get-ValidatedProjectOutputDirectories -RepositoryRoot $Root -ProjectFiles $projectFiles)
    $removed = [System.Collections.Generic.List[object]]::new()
    foreach ($path in $outputs) {
        $existed = Test-Path -LiteralPath $path
        if ($existed) {
            Remove-Item -LiteralPath $path -Recurse -Force
        }
        $removed.Add([pscustomobject] [ordered]@{
                path = [System.IO.Path]::GetRelativePath($Root, $path).Replace('\', '/')
                existed = $existed
                removed = -not (Test-Path -LiteralPath $path)
            })
    }

    return @($removed)
}

function Expand-ProtocolValue {
    param(
        [Parameter(Mandatory)] [string] $Value,
        [Parameter(Mandatory)] [hashtable] $Tokens
    )

    $expanded = $Value
    foreach ($token in $Tokens.Keys) {
        $expanded = $expanded.Replace('{' + $token + '}', [string] $Tokens[$token])
    }
    if ($expanded -match '\{[A-Za-z][A-Za-z0-9]*\}') {
        throw "Release protocol value contains an unknown token: '$expanded'."
    }

    return $expanded
}

function Get-ReleaseSteps {
    param(
        [Parameter(Mandatory)] $Protocol,
        [Parameter(Mandatory)] [string] $Mode,
        [Parameter(Mandatory)] [hashtable] $Tokens
    )

    $steps = [System.Collections.Generic.List[object]]::new()
    foreach ($name in @($Protocol.modes.$Mode.steps)) {
        $definition = $Protocol.steps.PSObject.Properties[[string] $name].Value
        $steps.Add([pscustomobject] [ordered]@{
                name = [string] $name
                fileName = Expand-ProtocolValue -Value ([string] $definition.fileName) -Tokens $Tokens
                workingDirectory = Expand-ProtocolValue -Value ([string] $definition.workingDirectory) -Tokens $Tokens
                arguments = @($definition.arguments | ForEach-Object {
                        Expand-ProtocolValue -Value ([string] $_) -Tokens $Tokens
                    })
            })
    }

    return @($steps)
}

$RepositoryRoot = Resolve-FullPath -Path $RepositoryRoot -BasePath (Get-Location).Path
if (-not (Test-Path -LiteralPath $RepositoryRoot -PathType Container)) {
    throw "RepositoryRoot was not found: '$RepositoryRoot'."
}

$gitTopLevel = Get-GitText -Arguments @('rev-parse', '--show-toplevel') -Root $RepositoryRoot
if (-not [System.IO.Path]::GetFullPath($gitTopLevel).Equals($RepositoryRoot, (Get-PathComparison))) {
    throw "RepositoryRoot '$RepositoryRoot' is not the active Git checkout '$gitTopLevel'."
}
$candidateCommit = Get-GitText -Arguments @('rev-parse', 'HEAD') -Root $RepositoryRoot
$candidateStatus = Get-GitText -Arguments @('status', '--porcelain=v1', '--untracked-files=all') -Root $RepositoryRoot
if (-not [string]::IsNullOrWhiteSpace($candidateStatus)) {
    throw ("Authoritative release requires a clean tracked tree. Dirty paths:" + [Environment]::NewLine + $candidateStatus)
}

Assert-ReleaseAttemptId $AttemptId | Out-Null
$artifactsDirectory = Join-Path $RepositoryRoot 'artifacts'
[System.IO.Directory]::CreateDirectory($artifactsDirectory) | Out-Null
$artifactsDirectory = (Resolve-Path -LiteralPath $artifactsDirectory).Path
$OutputDirectory = if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    Join-Path $artifactsDirectory (Join-Path 'release-candidate' (Join-Path $candidateCommit $AttemptId))
}
else {
    Resolve-FullPath -Path $OutputDirectory -BasePath $RepositoryRoot
}
$attemptArguments = @{
    Path = $OutputDirectory
    ArtifactsDirectory = $artifactsDirectory
    Commit = $candidateCommit
    AttemptId = $AttemptId
}
$OutputDirectory = Assert-NewReleaseAttemptPath @attemptArguments
$OutputDirectory = Initialize-OutputDirectory -Path $OutputDirectory -ArtifactsDirectory $artifactsDirectory
$logsDirectory = Join-Path $OutputDirectory 'logs'
$receiptsDirectory = Join-Path $OutputDirectory 'receipts'
$packagesDirectory = Join-Path $OutputDirectory 'packages'
$benchmarkArtifactsDirectory = Join-Path $OutputDirectory 'benchmark-artifacts'
$compatibilityOutputDirectory = Join-Path $OutputDirectory 'compatibility-run'
$verificationDirectory = Join-Path $OutputDirectory 'release-evidence'
$nugetPackagesDirectory = Join-Path $OutputDirectory 'nuget-packages'
foreach ($directory in @($logsDirectory, $receiptsDirectory, $packagesDirectory, $nugetPackagesDirectory)) {
    [System.IO.Directory]::CreateDirectory($directory) | Out-Null
}

$coreVersion = Get-PackageVersion -ProjectPath (Join-Path $RepositoryRoot 'src/FunnySharp/FunnySharp.csproj')
$aspNetCoreVersion = Get-PackageVersion -ProjectPath (Join-Path $RepositoryRoot 'src/FunnySharp.AspNetCore/FunnySharp.AspNetCore.csproj')
if ($coreVersion -ne $aspNetCoreVersion) {
    throw "Package versions must match; found FunnySharp $coreVersion and FunnySharp.AspNetCore $aspNetCoreVersion."
}
if (@($DistributionFeed | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count -eq 0) {
    throw 'At least one distribution feed is required.'
}
$versionState = [System.Collections.Generic.List[object]]::new()
try {
    foreach ($feed in @($DistributionFeed | Sort-Object -Unique)) {
        foreach ($packageId in @('FunnySharp', 'FunnySharp.AspNetCore')) {
            $versionState.Add((Get-PackageVersionState -Feed $feed -PackageId $packageId -Version $coreVersion))
        }
    }
}
catch {
    Write-JsonFile -Value ([pscustomobject] [ordered]@{
            schemaVersion = 1
            status = 'blocked-version-state'
            candidateCommit = $candidateCommit
            attemptId = $AttemptId
            packageVersion = $coreVersion
            checks = @($versionState)
            error = $_.Exception.Message
        }) -Path (Join-Path $OutputDirectory 'version-preflight.json')
    throw
}
Write-JsonFile -Value ([pscustomobject] [ordered]@{
        schemaVersion = 1
        status = 'passed'
        candidateCommit = $candidateCommit
        attemptId = $AttemptId
        packageVersion = $coreVersion
        checks = @($versionState)
    }) -Path (Join-Path $OutputDirectory 'version-preflight.json')

$generatedCleanup = Remove-ProjectGeneratedOutputs -Root $RepositoryRoot
$env:NUGET_PACKAGES = $nugetPackagesDirectory
$env:FUNNYSHARP_CANDIDATE_COMMIT = $candidateCommit

$protocolPath = Join-Path $RepositoryRoot 'eng/release-protocol.json'
$protocol = Read-ReleaseProtocol -Path $protocolPath
$mode = if ($SkipBenchmarks) { 'benchmarkSkipped' } else { 'full' }
$tokens = @{
    root = $RepositoryRoot
    compatibilityFeed = $CompatibilityPackageFeed
    packages = $packagesDirectory
    benchmarkRoot = Join-Path $RepositoryRoot 'benchmarks/FunnySharp.Benchmarks'
    benchmarkArtifacts = $benchmarkArtifactsDirectory
    benchmarkResults = Join-Path $benchmarkArtifactsDirectory 'results'
    performanceObservationProposal = Join-Path $OutputDirectory 'performance-observation-proposal.json'
    compatibilityOutput = $compatibilityOutputDirectory
    compatibilityRid = $CompatibilityRuntimeIdentifier
}
$releaseSteps = @(Get-ReleaseSteps -Protocol $protocol -Mode $mode -Tokens $tokens)
$expectedCandidateCommands = @($releaseSteps.name)
$receipts = [System.Collections.Generic.List[object]]::new()
$sourceFingerprintBefore = Get-SourceFingerprint -Root $RepositoryRoot
$sourceFingerprintAfter = $null
$versionFinal = $null
$runFailure = $null
$executionEvidenceFrozen = $false

function Write-ExecutionEvidence {
    $candidateSucceeded = $null -eq $runFailure -and
        $receipts.Count -eq $expectedCandidateCommands.Count -and
        @($receipts | Where-Object exitCode -ne 0).Count -eq 0 -and
        $null -ne $versionFinal -and
        $versionFinal.status -eq 'passed' -and
        $null -ne $sourceFingerprintAfter -and
        (Test-EquivalentSourceFingerprint -Left $sourceFingerprintBefore -Right $sourceFingerprintAfter)
    $evidence = [pscustomobject] [ordered]@{
        schemaVersion = 2
        succeeded = $candidateSucceeded
        attemptId = $AttemptId
        mode = $mode
        candidateCommit = $candidateCommit
        repositoryRoot = $RepositoryRoot
        outputDirectory = $OutputDirectory
        protocol = [pscustomobject] [ordered]@{
            path = 'eng/release-protocol.json'
            sha256 = Get-Sha256 -Path $protocolPath
        }
        versionPreflight = 'version-preflight.json'
        versionPreflightSha256 = Get-Sha256 -Path (Join-Path $OutputDirectory 'version-preflight.json')
        versionFinal = if ($null -eq $versionFinal) { $null } else { 'version-final.json' }
        versionFinalSha256 = if ($null -eq $versionFinal) { $null } else { Get-Sha256 -Path (Join-Path $OutputDirectory 'version-final.json') }
        generatedCleanup = $generatedCleanup
        nugetPackagesDirectory = $nugetPackagesDirectory
        isolatedNuGetCache = (Test-PathAtOrWithin -ChildPath $nugetPackagesDirectory -ParentPath $OutputDirectory)
        candidateCommands = $expectedCandidateCommands
        sourceFingerprintBefore = $sourceFingerprintBefore
        sourceFingerprintAfter = $sourceFingerprintAfter
        commands = @($receipts)
    }
    Write-JsonFile -Value $evidence -Path (Join-Path $OutputDirectory 'execution-evidence.json')
}

function Write-RunOutcome {
    $executionEvidencePath = Join-Path $OutputDirectory 'execution-evidence.json'
    $releaseEvidencePath = Join-Path $verificationDirectory 'release-evidence.json'
    $outcome = [pscustomobject] [ordered]@{
        schemaVersion = 1
        succeeded = $null -eq $runFailure
        attemptId = $AttemptId
        candidateCommit = $candidateCommit
        executionEvidence = if (Test-Path -LiteralPath $executionEvidencePath -PathType Leaf) {
            [pscustomobject] [ordered]@{
                path = 'execution-evidence.json'
                sha256 = Get-Sha256 -Path $executionEvidencePath
            }
        }
        else {
            $null
        }
        releaseEvidence = if (Test-Path -LiteralPath $releaseEvidencePath -PathType Leaf) {
            [pscustomobject] [ordered]@{
                path = 'release-evidence/release-evidence.json'
                sha256 = Get-Sha256 -Path $releaseEvidencePath
            }
        }
        else {
            $null
        }
        error = $runFailure
    }
    Write-JsonFile -Value $outcome -Path (Join-Path $OutputDirectory 'release-outcome.json')
}

function Invoke-ReleaseStep {
    param(
        [Parameter(Mandatory)] [int] $Ordinal,
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [string] $FileName,
        [Parameter(Mandatory)] [string[]] $Arguments,
        [Parameter(Mandatory)] [string] $WorkingDirectory
    )

    $prefix = '{0:D2}-{1}' -f $Ordinal, $Name
    $standardOutputRelative = 'logs/' + $prefix + '.stdout.log'
    $standardErrorRelative = 'logs/' + $prefix + '.stderr.log'
    $receiptRelative = 'receipts/' + $prefix + '.json'
    $standardOutputPath = Join-Path $OutputDirectory $standardOutputRelative
    $standardErrorPath = Join-Path $OutputDirectory $standardErrorRelative
    $receiptPath = Join-Path $OutputDirectory $receiptRelative
    $startedAtUtc = [DateTime]::UtcNow.ToString('O')

    try {
        $result = Invoke-ExternalProcess -FileName $FileName -Arguments $Arguments -WorkingDirectory $WorkingDirectory
    }
    catch {
        $result = [pscustomobject] [ordered]@{
            exitCode = -1
            standardOutput = ''
            standardError = $_.Exception.ToString()
        }
    }

    [System.IO.File]::WriteAllText($standardOutputPath, $result.standardOutput, [System.Text.UTF8Encoding]::new($false))
    [System.IO.File]::WriteAllText($standardErrorPath, $result.standardError, [System.Text.UTF8Encoding]::new($false))
    $receipt = [pscustomobject] [ordered]@{
        schemaVersion = 1
        name = $Name
        startedAtUtc = $startedAtUtc
        completedAtUtc = [DateTime]::UtcNow.ToString('O')
        fileName = $FileName
        arguments = @($Arguments)
        workingDirectory = $WorkingDirectory
        exitCode = $result.exitCode
        standardOutputLog = $standardOutputRelative
        standardErrorLog = $standardErrorRelative
        standardOutputSha256 = Get-Sha256 -Path $standardOutputPath
        standardErrorSha256 = Get-Sha256 -Path $standardErrorPath
    }
    Write-JsonFile -Value $receipt -Path $receiptPath
    $receipts.Add($receipt)
    Write-ExecutionEvidence

    if ($result.exitCode -ne 0) {
        throw "Release command '$Name' failed with exit code $($result.exitCode). See '$standardOutputPath' and '$standardErrorPath'."
    }
}

try {
    for ($index = 0; $index -lt $releaseSteps.Count; $index++) {
        $step = $releaseSteps[$index]
        $stepArguments = @{
            Ordinal = $index + 1
            Name = $step.name
            FileName = $step.fileName
            Arguments = @($step.arguments)
            WorkingDirectory = $step.workingDirectory
        }
        Invoke-ReleaseStep @stepArguments
    }

    $sourceFingerprintAfter = Get-SourceFingerprint -Root $RepositoryRoot
    if (-not (Test-EquivalentSourceFingerprint -Left $sourceFingerprintBefore -Right $sourceFingerprintAfter)) {
        throw 'Source files changed while the release candidate pipeline ran.'
    }
    Write-ExecutionEvidence

    $versionFinalChecks = [System.Collections.Generic.List[object]]::new()
    try {
        foreach ($feed in @($DistributionFeed | Sort-Object -Unique)) {
            foreach ($packageId in @('FunnySharp', 'FunnySharp.AspNetCore')) {
                $versionFinalChecks.Add((Get-PackageVersionState -Feed $feed -PackageId $packageId -Version $coreVersion))
            }
        }
    }
    catch {
        $versionFinal = [pscustomobject] [ordered]@{
            schemaVersion = 1
            status = 'blocked-version-state'
            candidateCommit = $candidateCommit
            attemptId = $AttemptId
            packageVersion = $coreVersion
            checks = @($versionFinalChecks)
            error = $_.Exception.Message
        }
        Write-JsonFile -Value $versionFinal -Path (Join-Path $OutputDirectory 'version-final.json')
        throw
    }
    $versionFinal = [pscustomobject] [ordered]@{
        schemaVersion = 1
        status = 'passed'
        candidateCommit = $candidateCommit
        attemptId = $AttemptId
        packageVersion = $coreVersion
        checks = @($versionFinalChecks)
    }
    Write-JsonFile -Value $versionFinal -Path (Join-Path $OutputDirectory 'version-final.json')

    $sourceFingerprintAfter = Get-SourceFingerprint -Root $RepositoryRoot
    if (-not (Test-EquivalentSourceFingerprint -Left $sourceFingerprintBefore -Right $sourceFingerprintAfter)) {
        throw 'Source files changed before final release verification.'
    }
    Write-ExecutionEvidence
    $executionEvidenceFrozen = $true

    $verificationArguments = [System.Collections.Generic.List[string]]::new()
    foreach ($argument in @(
        '-NoProfile',
        '-File', (Join-Path $RepositoryRoot 'eng/Verify-Release.ps1'),
        '-RepositoryRoot', $RepositoryRoot,
        '-OutputDirectory', $verificationDirectory,
        '-PackageDirectory', $packagesDirectory,
        '-CompatibilityEvidencePath', (Join-Path $compatibilityOutputDirectory 'compatibility-results.json'),
        '-CompatibilityRuntimeIdentifier', $CompatibilityRuntimeIdentifier,
        '-CompatibilityPackageFeed', $CompatibilityPackageFeed,
        '-ExecutionEvidenceDirectory', $OutputDirectory,
        '-Clean'
    )) {
        $verificationArguments.Add($argument)
    }
    if ($SkipBenchmarks) {
        $verificationArguments.Add('-SkipBenchmarks')
    }
    $verificationInvocation = @{
        FileName = (Get-Process -Id $PID).Path
        Arguments = $verificationArguments
        WorkingDirectory = $RepositoryRoot
    }
    $verificationResult = Invoke-ExternalProcess @verificationInvocation
    [System.IO.File]::WriteAllText(
        (Join-Path $OutputDirectory 'release-verifier.stdout.log'),
        $verificationResult.standardOutput,
        [System.Text.UTF8Encoding]::new($false))
    [System.IO.File]::WriteAllText(
        (Join-Path $OutputDirectory 'release-verifier.stderr.log'),
        $verificationResult.standardError,
        [System.Text.UTF8Encoding]::new($false))
    if ($verificationResult.exitCode -ne 0) {
        throw "Release verifier failed with exit code $($verificationResult.exitCode)."
    }
}
catch {
    $runFailure = $_.Exception.Message
}
finally {
    if ($null -eq $sourceFingerprintAfter) {
        try {
            $sourceFingerprintAfter = Get-SourceFingerprint -Root $RepositoryRoot
        }
        catch {
            if ($null -eq $runFailure) {
                $runFailure = $_.Exception.Message
            }
        }
    }
    if (-not $executionEvidenceFrozen) {
        Write-ExecutionEvidence
    }
    Write-RunOutcome
}

if ($null -ne $runFailure) {
    Write-Error "Release run failed: $runFailure"
    exit 1
}

Write-Output "Release run passed. Execution evidence: $OutputDirectory"
