[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$modulePath = Join-Path $repositoryRoot 'eng/ReleaseProtocol.psm1'
$protocolPath = Join-Path $repositoryRoot 'eng/release-protocol.json'
$reproducibilityComparer = Join-Path $repositoryRoot 'eng/Compare-ReproducibleBuilds.ps1'
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('funnysharp-release-tests-' + [Guid]::NewGuid().ToString('N'))
$passed = 0

function Assert-EqualSequence {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [string[]] $Actual,
        [Parameter(Mandatory)] [string[]] $Expected
    )

    if ($Actual.Count -ne $Expected.Count -or
        [string]::Join([char] 0, $Actual) -cne [string]::Join([char] 0, $Expected)) {
        throw ("FAIL {0}: expected '{1}', got '{2}'." -f $Name, ($Expected -join ', '), ($Actual -join ', '))
    }

    $script:passed++
    Write-Output "PASS $Name"
}

function Assert-Passes {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [scriptblock] $Action
    )

    & $Action | Out-Null
    $script:passed++
    Write-Output "PASS $Name"
}

function Assert-Fails {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [string] $Pattern,
        [Parameter(Mandatory)] [scriptblock] $Action
    )

    try {
        & $Action | Out-Null
    }
    catch {
        if ($_.Exception.Message -notmatch $Pattern) {
            throw ("FAIL {0}: expected '{1}', got '{2}'." -f $Name, $Pattern, $_.Exception.Message)
        }

        $script:passed++
        Write-Output "PASS $Name"
        return
    }

    throw ("FAIL {0}: expected failure matching '{1}'." -f $Name, $Pattern)
}

function Invoke-Git {
    param(
        [Parameter(Mandatory)] [string] $Root,
        [Parameter(Mandatory)] [string[]] $Arguments
    )

    & git -C $Root @Arguments | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed in '$Root'."
    }
}

function Write-ReproducibilityInput {
    param(
        [Parameter(Mandatory)] [string] $Root,
        [Parameter(Mandatory)] [string] $CachePath
    )

    $commit = (& git -C $Root rev-parse HEAD).Trim()
    $artifacts = Join-Path $Root 'artifacts'
    [IO.Directory]::CreateDirectory((Join-Path $artifacts '.nuget')) | Out-Null
    [IO.Directory]::CreateDirectory((Join-Path $artifacts 'packages')) | Out-Null
    [IO.File]::WriteAllText(
        (Join-Path $artifacts 'reproducibility-input.json'),
        ([pscustomobject] [ordered]@{
                schemaVersion = 1
                candidateCommit = $commit
                configuration = 'Release'
                isolatedNuGetCache = $true
                nugetPackagesDirectory = $CachePath
                packageDirectory = 'artifacts/packages'
                properties = [pscustomobject]@{ ContinuousIntegrationBuild = $true }
            } | ConvertTo-Json -Depth 5) + [Environment]::NewLine)
}

try {
    Import-Module $modulePath -Force
    [System.IO.Directory]::CreateDirectory($tempRoot) | Out-Null

    $protocol = Read-ReleaseProtocol -Path $protocolPath
    $expectedFull = @(
        'clean',
        'restore',
        'build',
        'test',
        'examples',
        'aspnetcore-examples',
        'pack',
        'format',
        'benchmark-preflight',
        'benchmark',
        'performance-verify',
        'performance-docs-verify',
        'compatibility'
    )
    $expectedSkipped = @(
        'clean',
        'restore',
        'build',
        'test',
        'examples',
        'aspnetcore-examples',
        'pack',
        'format',
        'benchmark-preflight',
        'performance-docs-verify',
        'compatibility'
    )
    Assert-EqualSequence 'full mode mandatory steps' @($protocol.modes.full.steps) $expectedFull
    Assert-EqualSequence 'benchmark-skipped mandatory steps' @($protocol.modes.benchmarkSkipped.steps) $expectedSkipped
    Assert-EqualSequence 'locked no-cache restore arguments' @($protocol.steps.restore.arguments) @(
        'restore',
        'FunnySharp.slnx',
        '--locked-mode',
        '--no-cache',
        '--source',
        '{compatibilityFeed}'
    )

    $workflowPath = Join-Path $repositoryRoot '.github/workflows/release.yml'
    $workflow = [IO.File]::ReadAllText($workflowPath)
    foreach ($context in @('win-x64', 'linux-x64', 'osx-arm64', 'osx-x64-consumer')) {
        if ($workflow -notmatch ('(?m)^\s+name:\s+' + [regex]::Escape($context) + '\s*$')) {
            throw "FAIL required workflow context: '$context' is missing."
        }
    }
    if ($workflow -match 'pull_request_target|continue-on-error:\s*true') {
        throw 'FAIL workflow safety: privileged pull request execution or continue-on-error was found.'
    }
    $unpinnedAction = [regex]::Match($workflow, '(?m)^\s*uses:\s+actions/[^@\s]+@(?![0-9a-f]{40}\s*(?:#|$))')
    if ($unpinnedAction.Success) {
        throw "FAIL workflow action pinning: '$($unpinnedAction.Value.Trim())'."
    }
    $script:passed++
    Write-Output 'PASS required workflow contexts and action pinning'

    $artifacts = Join-Path $tempRoot 'artifacts'
    [System.IO.Directory]::CreateDirectory($artifacts) | Out-Null
    $commit = '0123456789abcdef0123456789abcdef01234567'
    $attempt = 'attempt-1'
    $valid = Join-Path $artifacts "release-candidate/$commit/$attempt"
    Assert-Passes 'new attempt path' {
        Assert-NewReleaseAttemptPath -Path $valid -ArtifactsDirectory $artifacts -Commit $commit -AttemptId $attempt
    }
    [System.IO.Directory]::CreateDirectory($valid) | Out-Null
    Assert-Fails 'attempt path is immutable' 'already exists' {
        Assert-NewReleaseAttemptPath -Path $valid -ArtifactsDirectory $artifacts -Commit $commit -AttemptId $attempt
    }
    Assert-Fails 'attempt path cannot escape' 'must equal' {
        Assert-NewReleaseAttemptPath -Path (Join-Path $artifacts '../escape') -ArtifactsDirectory $artifacts -Commit $commit -AttemptId $attempt
    }
    Assert-Fails 'attempt id is constrained' 'AttemptId' {
        Assert-ReleaseAttemptId '../bad'
    }

    $project = Join-Path $tempRoot 'src/Library/Library.csproj'
    [System.IO.Directory]::CreateDirectory((Split-Path -Parent $project)) | Out-Null
    [System.IO.File]::WriteAllText($project, '<Project />')
    $outputs = @(Get-ValidatedProjectOutputDirectories -RepositoryRoot $tempRoot -ProjectFiles @('src/Library/Library.csproj'))
    Assert-EqualSequence 'project outputs are direct children' @($outputs | ForEach-Object { [System.IO.Path]::GetRelativePath($tempRoot, $_).Replace('\', '/') }) @('src/Library/bin', 'src/Library/obj')
    Assert-Fails 'project path cannot escape' 'repository-relative' {
        Get-ValidatedProjectOutputDirectories -RepositoryRoot $tempRoot -ProjectFiles @('../outside.csproj')
    }

    Assert-Passes 'unpublished version passes' {
        Assert-PackageVersionAbsent -PackageId 'FunnySharp' -Version '0.1.0' -Versions @('0.0.9')
    }
    Assert-Fails 'published version blocks' 'already contains' {
        Assert-PackageVersionAbsent -PackageId 'FunnySharp' -Version '0.1.0' -Versions @('0.1.0')
    }
    Assert-Fails 'ambiguous version response blocks' 'ambiguous' {
        Assert-PackageVersionAbsent -PackageId 'FunnySharp' -Version '0.1.0' -Versions $null
    }

    $leftRoot = Join-Path $tempRoot 'repro-left'
    $rightRoot = Join-Path $tempRoot 'repro-right'
    [IO.Directory]::CreateDirectory($leftRoot) | Out-Null
    Invoke-Git $leftRoot @('init', '-b', 'main')
    Invoke-Git $leftRoot @('config', 'user.email', 'fixture@example.com')
    Invoke-Git $leftRoot @('config', 'user.name', 'Fixture')
    [IO.File]::WriteAllText((Join-Path $leftRoot '.gitignore'), 'artifacts/' + [Environment]::NewLine)
    [IO.File]::WriteAllText((Join-Path $leftRoot 'FunnySharp.slnx'), '<Solution />')
    [IO.File]::WriteAllText((Join-Path $leftRoot 'global.json'), '{}')
    [IO.File]::WriteAllText((Join-Path $leftRoot 'Directory.Build.props'), '<Project />')
    [IO.File]::WriteAllText((Join-Path $leftRoot 'packages.lock.json'), '{}')
    Invoke-Git $leftRoot @('add', '.')
    Invoke-Git $leftRoot @('commit', '-m', 'fixture')
    & git clone --quiet $leftRoot $rightRoot
    if ($LASTEXITCODE -ne 0) {
        throw 'git clone failed for reproducibility fixtures.'
    }
    Write-ReproducibilityInput $leftRoot 'artifacts/.nuget'
    Write-ReproducibilityInput $rightRoot 'artifacts/.nuget'

    [IO.File]::WriteAllText((Join-Path $rightRoot 'global.json'), '{"dirty":true}')
    Assert-Fails 'reproducibility rejects dirty roots' 'must be clean' {
        & $reproducibilityComparer -LeftRoot $leftRoot -RightRoot $rightRoot
    }
    [IO.File]::Copy((Join-Path $leftRoot 'global.json'), (Join-Path $rightRoot 'global.json'), $true)

    Write-ReproducibilityInput $rightRoot '../shared-cache'
    Assert-Fails 'reproducibility rejects non-isolated cache' 'not isolated' {
        & $reproducibilityComparer -LeftRoot $leftRoot -RightRoot $rightRoot
    }
    Write-ReproducibilityInput $rightRoot 'artifacts/.nuget'

    [IO.File]::WriteAllText((Join-Path $rightRoot 'Directory.Build.props'), '<Project><PropertyGroup /></Project>')
    Invoke-Git $rightRoot @('config', 'user.email', 'fixture@example.com')
    Invoke-Git $rightRoot @('config', 'user.name', 'Fixture')
    Invoke-Git $rightRoot @('add', 'Directory.Build.props')
    Invoke-Git $rightRoot @('commit', '-m', 'different')
    Write-ReproducibilityInput $rightRoot 'artifacts/.nuget'
    Assert-Fails 'reproducibility rejects mismatched commits' 'same commit and source tree' {
        & $reproducibilityComparer -LeftRoot $leftRoot -RightRoot $rightRoot
    }
}
finally {
    Remove-Module ReleaseProtocol -ErrorAction SilentlyContinue
    $resolvedTemp = [System.IO.Path]::GetFullPath($tempRoot)
    $systemTemp = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    if ($resolvedTemp.StartsWith($systemTemp, [System.StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTemp)) {
        Remove-Item -LiteralPath $resolvedTemp -Recurse -Force
    }
}

Write-Output "Release protocol tests passed: $passed."
