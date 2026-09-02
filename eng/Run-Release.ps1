<#
.SYNOPSIS
Runs the FunnySharp release candidate pipeline and records immutable execution evidence.

.DESCRIPTION
Runs every release gate from one source fingerprint. Command output, exit codes,
and per-command receipts are written beneath an ignored artifacts subdirectory.
The final verifier binds the package and API evidence to this execution receipt.

.EXAMPLE
pwsh -NoProfile -File eng/Run-Release.ps1 -OutputDirectory artifacts/goal12-release-run `
  -CompatibilityPackageFeed https://packagefeedproxy.microsoft.io/nuget/v3/index.json -Clean
#>
[CmdletBinding()]
param(
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot),

    [Parameter(Mandatory)]
    [string] $OutputDirectory,

    [string] $CompatibilityRuntimeIdentifier = [System.Runtime.InteropServices.RuntimeInformation]::RuntimeIdentifier,

    [string] $CompatibilityPackageFeed = 'https://api.nuget.org/v3/index.json',

    [switch] $Clean
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

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
        [Parameter(Mandatory)] [string] $ArtifactsDirectory,
        [Parameter(Mandatory)] [bool] $AllowClean
    )

    $Path = Assert-SafeArtifactsSubdirectory -Path $Path -ArtifactsDirectory $ArtifactsDirectory
    if (-not (Test-Path -LiteralPath $Path)) {
        [System.IO.Directory]::CreateDirectory($Path) | Out-Null
        return $Path
    }

    $outputItem = Get-Item -LiteralPath $Path -Force
    if (-not $outputItem.PSIsContainer) {
        throw "OutputDirectory '$Path' must be a directory."
    }
    if (($outputItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "OutputDirectory '$Path' cannot be a reparse point."
    }

    $existingItems = @(Get-ChildItem -LiteralPath $Path -Force)
    if ($existingItems.Count -gt 0 -and -not $AllowClean) {
        throw "OutputDirectory '$Path' is not empty. Supply -Clean to replace its contents."
    }

    if ($AllowClean) {
        foreach ($item in $existingItems) {
            Assert-SafeArtifactsSubdirectory -Path $item.FullName -ArtifactsDirectory $ArtifactsDirectory | Out-Null
            $currentItem = Get-Item -LiteralPath $item.FullName -Force
            if (($currentItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Refusing to delete reparse point '$($currentItem.FullName)'."
            }
            Remove-Item -LiteralPath $currentItem.FullName -Force -Recurse:$currentItem.PSIsContainer
        }
    }

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

$RepositoryRoot = Resolve-FullPath -Path $RepositoryRoot -BasePath (Get-Location).Path
if (-not (Test-Path -LiteralPath $RepositoryRoot -PathType Container)) {
    throw "RepositoryRoot was not found: '$RepositoryRoot'."
}

$artifactsDirectory = Join-Path $RepositoryRoot 'artifacts'
[System.IO.Directory]::CreateDirectory($artifactsDirectory) | Out-Null
$artifactsDirectory = (Resolve-Path -LiteralPath $artifactsDirectory).Path
$OutputDirectory = Resolve-FullPath -Path $OutputDirectory -BasePath $RepositoryRoot
$OutputDirectory = Initialize-OutputDirectory -Path $OutputDirectory -ArtifactsDirectory $artifactsDirectory -AllowClean $Clean.IsPresent
$CompatibilityScript = Join-Path $RepositoryRoot 'tests/FunnySharp.Compatibility/Run-Compatibility.ps1'

$logsDirectory = Join-Path $OutputDirectory 'logs'
$receiptsDirectory = Join-Path $OutputDirectory 'receipts'
$packagesDirectory = Join-Path $OutputDirectory 'packages'
$benchmarkArtifactsDirectory = Join-Path $OutputDirectory 'benchmark-artifacts'
$verificationDirectory = Join-Path $OutputDirectory 'release-evidence'
foreach ($directory in @($logsDirectory, $receiptsDirectory, $packagesDirectory)) {
    [System.IO.Directory]::CreateDirectory($directory) | Out-Null
}

$expectedCandidateCommands = @(
    'clean',
    'restore',
    'build',
    'test',
    'examples',
    'aspnetcore-examples',
    'pack',
    'format',
    'benchmark'
)
$receipts = [System.Collections.Generic.List[object]]::new()
$sourceFingerprintBefore = Get-SourceFingerprint -Root $RepositoryRoot
$sourceFingerprintAfter = $null
$runFailure = $null

function Write-ExecutionEvidence {
    $candidateSucceeded = $null -eq $runFailure -and
        $receipts.Count -eq $expectedCandidateCommands.Count -and
        @($receipts | Where-Object exitCode -ne 0).Count -eq 0 -and
        $null -ne $sourceFingerprintAfter -and
        (Test-EquivalentSourceFingerprint -Left $sourceFingerprintBefore -Right $sourceFingerprintAfter)
    $evidence = [pscustomobject] [ordered]@{
        schemaVersion = 1
        succeeded = $candidateSucceeded
        repositoryRoot = $RepositoryRoot
        outputDirectory = $OutputDirectory
        candidateCommands = $expectedCandidateCommands
        sourceFingerprintBefore = $sourceFingerprintBefore
        sourceFingerprintAfter = $sourceFingerprintAfter
        commands = @($receipts)
    }
    Write-JsonFile -Value $evidence -Path (Join-Path $OutputDirectory 'execution-evidence.json')
}

function Invoke-ReleaseStep {
    param(
        [Parameter(Mandatory)] [int] $Ordinal,
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [string] $FileName,
        [Parameter(Mandatory)] [string[]] $Arguments
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
        $result = Invoke-ExternalProcess -FileName $FileName -Arguments $Arguments -WorkingDirectory $RepositoryRoot
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
    Invoke-ReleaseStep -Ordinal 1 -Name 'clean' -FileName 'dotnet' -Arguments @('clean', 'FunnySharp.slnx', '--configuration', 'Release')
    Invoke-ReleaseStep -Ordinal 2 -Name 'restore' -FileName 'dotnet' -Arguments @('restore', 'FunnySharp.slnx', '--locked-mode')
    Invoke-ReleaseStep -Ordinal 3 -Name 'build' -FileName 'dotnet' -Arguments @('build', 'FunnySharp.slnx', '--configuration', 'Release', '--no-restore')
    Invoke-ReleaseStep -Ordinal 4 -Name 'test' -FileName 'dotnet' -Arguments @('test', 'FunnySharp.slnx', '--configuration', 'Release', '--no-build', '--no-restore')
    Invoke-ReleaseStep -Ordinal 5 -Name 'examples' -FileName 'dotnet' -Arguments @('run', '--project', 'examples/FunnySharp.Examples/FunnySharp.Examples.csproj', '--configuration', 'Release', '--no-build', '--no-restore')
    Invoke-ReleaseStep -Ordinal 6 -Name 'aspnetcore-examples' -FileName 'dotnet' -Arguments @('run', '--project', 'examples/FunnySharp.AspNetCore.Examples/FunnySharp.AspNetCore.Examples.csproj', '--configuration', 'Release', '--no-build', '--no-restore', '--', '--verify')
    Invoke-ReleaseStep -Ordinal 7 -Name 'pack' -FileName 'dotnet' -Arguments @('pack', 'FunnySharp.slnx', '--configuration', 'Release', '--no-build', '--no-restore', '--output', $packagesDirectory)
    Invoke-ReleaseStep -Ordinal 8 -Name 'format' -FileName 'dotnet' -Arguments @('format', 'FunnySharp.slnx', '--verify-no-changes', '--no-restore')
    Invoke-ReleaseStep -Ordinal 9 -Name 'benchmark' -FileName 'dotnet' -Arguments @('run', '--project', 'benchmarks/FunnySharp.Benchmarks/FunnySharp.Benchmarks.csproj', '--configuration', 'Release', '--no-build', '--no-restore', '--', '--filter', '*', '--artifacts', $benchmarkArtifactsDirectory)

    $sourceFingerprintAfter = Get-SourceFingerprint -Root $RepositoryRoot
    if (-not (Test-EquivalentSourceFingerprint -Left $sourceFingerprintBefore -Right $sourceFingerprintAfter)) {
        throw 'Source files changed while the release candidate pipeline ran.'
    }
    Write-ExecutionEvidence

    $verificationResult = Invoke-ExternalProcess -FileName (Get-Process -Id $PID).Path -Arguments @(
        '-NoProfile',
        '-File', (Join-Path $RepositoryRoot 'eng/Verify-Release.ps1'),
        '-RepositoryRoot', $RepositoryRoot,
        '-OutputDirectory', $verificationDirectory,
        '-PackageDirectory', $packagesDirectory,
        '-CompatibilityScript', $CompatibilityScript,
        '-CompatibilityRuntimeIdentifier', $CompatibilityRuntimeIdentifier,
        '-CompatibilityPackageFeed', $CompatibilityPackageFeed,
        '-ExecutionEvidenceDirectory', $OutputDirectory,
        '-Clean'
    ) -WorkingDirectory $RepositoryRoot
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

    $sourceFingerprintAfter = Get-SourceFingerprint -Root $RepositoryRoot
    if (-not (Test-EquivalentSourceFingerprint -Left $sourceFingerprintBefore -Right $sourceFingerprintAfter)) {
        throw 'Source files changed while release verification ran.'
    }
    Write-ExecutionEvidence
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
    Write-ExecutionEvidence
}

if ($null -ne $runFailure) {
    Write-Error "Release run failed: $runFailure"
    exit 1
}

Write-Output "Release run passed. Execution evidence: $OutputDirectory"
