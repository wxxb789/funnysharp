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
        'performance-protocol-tests',
        'release-protocol-tests',
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
        'performance-protocol-tests',
        'release-protocol-tests',
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

    $performanceManifest = Get-Content -LiteralPath (Join-Path $repositoryRoot 'eng/performance/baseline.json') -Raw | ConvertFrom-Json
    $fingerprintPaths = @(
        'eng/performance/baseline.json'
        @($performanceManifest.benchmarkInput.files)
        @($performanceManifest.protocol.files)
        @($performanceManifest.documentation.path)
    ) | Sort-Object -Unique
    foreach ($relativePath in $fingerprintPaths) {
        $attribute = & git -C $repositoryRoot check-attr eol -- $relativePath
        if ($LASTEXITCODE -ne 0 -or [string]::Join([Environment]::NewLine, @($attribute)) -notmatch ': eol: lf$') {
            throw "FAIL performance fingerprint EOL policy: '$relativePath' is not forced to LF."
        }

        $bytes = [IO.File]::ReadAllBytes((Join-Path $repositoryRoot $relativePath))
        for ($index = 1; $index -lt $bytes.Length; $index++) {
            if ($bytes[$index - 1] -eq 13 -and $bytes[$index] -eq 10) {
                throw "FAIL performance fingerprint EOL policy: '$relativePath' contains CRLF bytes."
            }
        }
    }
    $script:passed++
    Write-Output 'PASS performance fingerprint inputs force LF checkout'

    $expectedBenchmarkRows = @(
        [pscustomobject]@{ benchmarkClass = 'StateMachineBenchmarks'; category = 'Then'; method = 'Direct'; parameters = '[Count=8]' },
        [pscustomobject]@{ benchmarkClass = 'StateMachineBenchmarks'; category = 'Then'; method = 'FunnySharp'; parameters = '[Count=8]' }
    )
    Assert-Passes 'benchmark report row set' {
        Assert-BenchmarkReportRows -ExpectedRows $expectedBenchmarkRows -ActualRows @($expectedBenchmarkRows) -Description 'Fixture report'
    }
    Assert-Fails 'benchmark report rejects missing row' 'does not match' {
        Assert-BenchmarkReportRows -ExpectedRows $expectedBenchmarkRows -ActualRows @($expectedBenchmarkRows[0]) -Description 'Fixture report'
    }
    Assert-Fails 'benchmark report rejects unregistered method' 'does not match' {
        Assert-BenchmarkReportRows -ExpectedRows $expectedBenchmarkRows -ActualRows @(
            $expectedBenchmarkRows[0],
            [pscustomobject]@{ benchmarkClass = 'StateMachineBenchmarks'; category = 'Then'; method = 'Bogus'; parameters = '[Count=8]' }
        ) -Description 'Fixture report'
    }
    Assert-Fails 'benchmark report rejects wrong parameters' 'does not match' {
        Assert-BenchmarkReportRows -ExpectedRows $expectedBenchmarkRows -ActualRows @(
            [pscustomobject]@{ benchmarkClass = 'StateMachineBenchmarks'; category = 'Then'; method = 'Direct'; parameters = '[Count=64]' },
            $expectedBenchmarkRows[1]
        ) -Description 'Fixture report'
    }

    $verifyReleasePath = Join-Path $repositoryRoot 'eng/Verify-Release.ps1'
    $tokens = $null
    $parseErrors = $null
    $verifyReleaseAst = [System.Management.Automation.Language.Parser]::ParseFile(
        $verifyReleasePath,
        [ref] $tokens,
        [ref] $parseErrors)
    if ($parseErrors.Count -ne 0) {
        throw "FAIL release verifier parsing: $($parseErrors[0].Message)"
    }
    $benchmarkVerifierCalls = @($verifyReleaseAst.FindAll({
                param($node)
                $node -is [System.Management.Automation.Language.CommandAst] -and
                $node.GetCommandName() -ceq 'Assert-BenchmarkReports'
            }, $true))
    if ($benchmarkVerifierCalls.Count -ne 1) {
        throw "FAIL release verifier benchmark integration: expected one Assert-BenchmarkReports call, found $($benchmarkVerifierCalls.Count)."
    }
    $script:passed++
    Write-Output 'PASS release verifier invokes independent benchmark report validation'

    $ansiSanitizerFunction = $verifyReleaseAst.Find({
            param($node)
            $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and
            $node.Name -ceq 'Remove-AnsiControlSequences'
        }, $true)
    if ($null -eq $ansiSanitizerFunction) {
        throw 'FAIL release verifier ANSI normalization: Remove-AnsiControlSequences was not found.'
    }
    Invoke-Expression $ansiSanitizerFunction.Extent.Text
    $escape = [char] 27
    $coloredTestResult = "${escape}[mC:\tests\FunnySharp.Tests.dll (net10.0|x64) ${escape}[32mpassed${escape}[m ${escape}[90m(1s 598ms)${escape}[m"
    $normalizedTestResult = Remove-AnsiControlSequences -Text $coloredTestResult
    if ($normalizedTestResult -cne 'C:\tests\FunnySharp.Tests.dll (net10.0|x64) passed (1s 598ms)') {
        throw "FAIL release verifier ANSI normalization: '$normalizedTestResult'."
    }
    $script:passed++
    Write-Output 'PASS release verifier strips ANSI control sequences from logs'

    $compatibilityScriptPath = Join-Path $repositoryRoot 'tests/FunnySharp.Compatibility/Run-Compatibility.ps1'
    $compatibilityTokens = $null
    $compatibilityParseErrors = $null
    $compatibilityAst = [System.Management.Automation.Language.Parser]::ParseFile(
        $compatibilityScriptPath,
        [ref] $compatibilityTokens,
        [ref] $compatibilityParseErrors)
    if ($compatibilityParseErrors.Count -ne 0) {
        throw "FAIL compatibility script parsing: $($compatibilityParseErrors[0].Message)"
    }
    $cachePathFunction = $compatibilityAst.Find({
            param($node)
            $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and
            $node.Name -ceq 'Get-IsolatedNuGetPackagesDirectory'
        }, $true)
    if ($null -eq $cachePathFunction) {
        throw 'FAIL compatibility NuGet cache path: Get-IsolatedNuGetPackagesDirectory was not found.'
    }
    Invoke-Expression $cachePathFunction.Extent.Text

    $ciArtifactsRootLength = 'D:\a\funnysharp\funnysharp\artifacts'.Length
    $pathRoot = [IO.Path]::GetPathRoot($repositoryRoot)
    $ciArtifactsRoot = Join-Path $pathRoot ('a' * ($ciArtifactsRootLength - $pathRoot.Length))
    $ciOutputDirectory = Join-Path $ciArtifactsRoot 'release-candidate/0123456789abcdef0123456789abcdef01234567/33877879504-1-win-x64/compatibility-run'
    $cacheDirectory = Get-IsolatedNuGetPackagesDirectory `
        -ArtifactsDirectory $ciArtifactsRoot `
        -OutputDirectory $ciOutputDirectory
    $otherCacheDirectory = Get-IsolatedNuGetPackagesDirectory `
        -ArtifactsDirectory $ciArtifactsRoot `
        -OutputDirectory ($ciOutputDirectory + '-other')
    $nativeAotAsset = Join-Path $cacheDirectory 'microsoft.netcore.app.runtime.nativeaot.win-x64/10.0.11/runtimes/win-x64/native/System.Globalization.Native.Aot.lib'
    if ($nativeAotAsset.Length -ge 260) {
        throw "FAIL compatibility NuGet cache path: NativeAOT asset path is $($nativeAotAsset.Length) characters."
    }
    if ($cacheDirectory -ceq $otherCacheDirectory) {
        throw 'FAIL compatibility NuGet cache path: distinct outputs share a cache directory.'
    }
    $script:passed++
    Write-Output 'PASS compatibility NuGet cache stays short and isolated'

    $parameterFunction = $verifyReleaseAst.Find({
            param($node)
            $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and
            $node.Name -ceq 'Get-BenchmarkParameterNames'
        }, $true)
    Invoke-Expression $parameterFunction.Extent.Text
    $parameterNames = @(Get-BenchmarkParameterNames -PolicyRows @(
            [pscustomobject]@{ parameters = '' }
        ) -BenchmarkClass 'NoParameterBenchmarks')
    if ($parameterNames.Count -ne 0) {
        throw 'FAIL benchmark parameter discovery: a no-parameter class returned a parameter name.'
    }
    $script:passed++
    Write-Output 'PASS benchmark parameter discovery handles no-parameter classes'

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
    Assert-Passes 'empty package version list passes' {
        Assert-PackageVersionAbsent -PackageId 'FunnySharp' -Version '0.1.0' -Versions @()
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
