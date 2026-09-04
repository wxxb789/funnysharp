<#
.SYNOPSIS
Collects reproducible release evidence for FunnySharp packages.

.DESCRIPTION
Consumes a Run-Release execution receipt, then inventories public APIs from the
Release assemblies, validates the two NuGet packages, records SHA256 hashes, and
runs the documentation and compatibility verifiers.

.EXAMPLE
pwsh ./eng/Run-Release.ps1 -OutputDirectory ./artifacts/goal12-release-run `
  -CompatibilityPackageFeed https://packagefeedproxy.microsoft.io/nuget/v3/index.json -Clean

.EXAMPLE
pwsh ./eng/Verify-Release.ps1 -OutputDirectory ./artifacts/goal12-release-run/release-evidence `
  -PackageDirectory ./artifacts/goal12-release-run/packages `
  -CompatibilityPackageFeed https://packagefeedproxy.microsoft.io/nuget/v3/index.json `
  -CompatibilityScript ./tests/FunnySharp.Compatibility/Run-Compatibility.ps1 `
  -ExecutionEvidenceDirectory ./artifacts/goal12-release-run -Clean

.PARAMETER OutputDirectory
An output subdirectory under <repository>/artifacts. Existing contents are only
removed when -Clean is supplied.

.PARAMETER CompatibilityEvidencePath
An existing schema-versioned compatibility result. The file is accepted only
when it contains the complete passing scenario set for the inspected packages.

.PARAMETER CompatibilityScript
An optional PowerShell compatibility verifier. It runs in a separate pwsh
process before the final report. Supported RepositoryRoot and OutputDirectory
parameters are passed when the script declares them.

.PARAMETER ExecutionEvidenceDirectory
The Run-Release output directory containing execution-evidence.json, fixed
command logs, and per-command receipts for this release candidate.
#>
[CmdletBinding()]
param(
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot),

    [Parameter(Mandatory)]
    [string] $OutputDirectory,

    [string] $PackageDirectory,

    [string] $DocumentationVerifier,

    [string] $CompatibilityEvidencePath,

    [string] $CompatibilityScript,

    [string] $CompatibilityRuntimeIdentifier = [System.Runtime.InteropServices.RuntimeInformation]::RuntimeIdentifier,

    [string] $CompatibilityPackageFeed = 'https://api.nuget.org/v3/index.json',

    [Parameter(Mandatory)]
    [string] $ExecutionEvidenceDirectory,

    [switch] $SkipBenchmarks,

    [switch] $Clean
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

    $resolvedArtifacts = (Resolve-Path -LiteralPath $ArtifactsDirectory).Path
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

        $resolvedCurrent = (Resolve-Path -LiteralPath $current).Path
        if (-not (Test-PathAtOrWithin -ChildPath $resolvedCurrent -ParentPath $resolvedArtifacts)) {
            throw "Resolved path escapes '$resolvedArtifacts': '$resolvedCurrent'."
        }
        $current = $resolvedCurrent
    }

    return $fullPath
}

function Initialize-EvidenceDirectory {
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [string] $ArtifactsDirectory,
        [Parameter(Mandatory)] [bool] $AllowClean
    )

    $Path = Assert-SafeArtifactsSubdirectory -Path $Path -ArtifactsDirectory $ArtifactsDirectory

    if (Test-Path -LiteralPath $Path -PathType Container) {
        $outputItem = Get-Item -LiteralPath $Path -Force
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
                $recurse = $item.PSIsContainer
                Remove-Item -LiteralPath $item.FullName -Force -Recurse:$recurse
            }
        }
    }
    else {
        [System.IO.Directory]::CreateDirectory($Path) | Out-Null
    }
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

function Get-SourceFingerprint {
    param([Parameter(Mandatory)] [string] $Root)

    $result = Invoke-ExternalProcess -FileName 'git' -Arguments @('ls-files', '--cached', '--others', '--exclude-standard', '-z') -WorkingDirectory $Root
    if ($result.ExitCode -ne 0) {
        throw "git ls-files failed with exit code $($result.ExitCode): $($result.StandardError.Trim())"
    }

    $files = [System.Collections.Generic.List[object]]::new()
    foreach ($relativePath in ($result.StandardOutput -split [char] 0)) {
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
        [string]::Equals([string] $Left.digest, [string] $Right.digest, [System.StringComparison]::OrdinalIgnoreCase)
}

function Get-SafeEvidenceFile {
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [string] $ArtifactsDirectory
    )

    Assert-SafeArtifactsSubdirectory -Path $Path -ArtifactsDirectory $ArtifactsDirectory | Out-Null
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Evidence file was not found: '$Path'."
    }

    $item = Get-Item -LiteralPath $Path -Force
    if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Evidence file cannot be a reparse point: '$($item.FullName)'."
    }

    return $item.FullName
}

function Get-ExecutionLogText {
    param(
        [Parameter(Mandatory)] [string] $ExecutionDirectory,
        [Parameter(Mandatory)] [string] $RelativePath,
        [Parameter(Mandatory)] [string] $ExpectedSha256,
        [Parameter(Mandatory)] [string] $ArtifactsDirectory
    )

    if ([System.IO.Path]::IsPathFullyQualified($RelativePath) -or $RelativePath -match '(^|[\\/])\.\.([\\/]|$)') {
        throw "Execution evidence log path must be a relative child path: '$RelativePath'."
    }

    $path = [System.IO.Path]::GetFullPath((Join-Path $ExecutionDirectory $RelativePath))
    if (-not (Test-PathAtOrWithin -ChildPath $path -ParentPath $ExecutionDirectory)) {
        throw "Execution evidence log path escapes its directory: '$RelativePath'."
    }

    $path = Get-SafeEvidenceFile -Path $path -ArtifactsDirectory $ArtifactsDirectory
    $actualSha256 = Get-Sha256 -Path $path
    if (-not [string]::Equals($actualSha256, $ExpectedSha256, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Execution evidence log hash does not match receipt: '$RelativePath'."
    }

    return [pscustomobject] [ordered]@{
        path = $path
        sha256 = $actualSha256
        text = [System.IO.File]::ReadAllText($path)
    }
}

function Test-ExactStringSequence {
    param(
        [Parameter(Mandatory)] [object[]] $Actual,
        [Parameter(Mandatory)] [string[]] $Expected
    )

    if ($Actual.Count -ne $Expected.Count) {
        return $false
    }

    for ($index = 0; $index -lt $Expected.Count; $index++) {
        if (-not [string]::Equals([string] $Actual[$index], $Expected[$index], [System.StringComparison]::Ordinal)) {
            return $false
        }
    }

    return $true
}

function Get-ExpectedReleaseCommands {
    param(
        [Parameter(Mandatory)] [string] $ExecutionDirectory,
        [Parameter(Mandatory)] [string] $CompatibilityPackageFeed,
        [Parameter(Mandatory)] [string] $Root,
        [Parameter(Mandatory)] [string] $CompatibilityRuntimeIdentifier,
        [Parameter(Mandatory)] [bool] $BenchmarksSkipped
    )

    $packagesDirectory = Join-Path $ExecutionDirectory 'packages'
    $benchmarkArtifactsDirectory = Join-Path $ExecutionDirectory 'benchmark-artifacts'
    $protocolPath = Join-Path $Root 'eng/release-protocol.json'
    $protocol = Read-ReleaseProtocol -Path $protocolPath
    $mode = if ($BenchmarksSkipped) { 'benchmarkSkipped' } else { 'full' }
    $tokens = @{
        root = $Root
        compatibilityFeed = $CompatibilityPackageFeed
        packages = $packagesDirectory
        benchmarkRoot = Join-Path $Root 'benchmarks/FunnySharp.Benchmarks'
        benchmarkArtifacts = $benchmarkArtifactsDirectory
        benchmarkResults = Join-Path $benchmarkArtifactsDirectory 'results'
        performanceObservationProposal = Join-Path $ExecutionDirectory 'performance-observation-proposal.json'
        compatibilityOutput = Join-Path $ExecutionDirectory 'compatibility-run'
        compatibilityRid = $CompatibilityRuntimeIdentifier
    }
    $commands = [System.Collections.Generic.List[object]]::new()
    foreach ($name in @($protocol.modes.$mode.steps)) {
        $definition = $protocol.steps.PSObject.Properties[[string] $name].Value
        $expand = {
            param([string] $Value)
            $expanded = $Value
            foreach ($token in $tokens.Keys) {
                $expanded = $expanded.Replace('{' + $token + '}', [string] $tokens[$token])
            }
            return $expanded
        }
        $commands.Add([pscustomobject] [ordered]@{
                name = [string] $name
                fileName = & $expand ([string] $definition.fileName)
                workingDirectory = & $expand ([string] $definition.workingDirectory)
                arguments = @($definition.arguments | ForEach-Object { & $expand ([string] $_) })
            })
    }

    return @($commands)
}

function Assert-CanonicalReleaseCommand {
    param(
        [Parameter(Mandatory)] $Command,
        [Parameter(Mandatory)] $Expected,
        [Parameter(Mandatory)] [string] $Description
    )

    if ($Command.fileName -cne $Expected.fileName -or
        $Command.workingDirectory -cne $Expected.workingDirectory -or
        -not (Test-ExactStringSequence -Actual @($Command.arguments) -Expected @($Expected.arguments))) {
        throw "$Description does not use the canonical command, working directory, and ordered arguments for '$($Expected.name)'."
    }
}

function Get-BenchmarkSourceManifest {
    param([Parameter(Mandatory)] [string] $Root)

    $benchmarkDirectory = Join-Path $Root 'benchmarks/FunnySharp.Benchmarks'
    $sourceFiles = @(
        Get-ChildItem -LiteralPath $benchmarkDirectory -File -Filter '*.cs' |
            Where-Object Name -ne 'Program.cs' |
            Sort-Object Name
    )
    $classes = [System.Collections.Generic.List[string]]::new()
    $categories = @{}

    foreach ($sourceFile in $sourceFiles) {
        $currentClass = $null
        $attributeLines = [System.Collections.Generic.List[string]]::new()
        foreach ($line in [System.IO.File]::ReadAllLines($sourceFile.FullName)) {
            $trimmed = $line.Trim()
            $classMatch = [regex]::Match($trimmed, '^public\s+(?:sealed\s+|abstract\s+)?class\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\b')
            if ($classMatch.Success) {
                $currentClass = $classMatch.Groups['name'].Value
                $classes.Add($currentClass)
                $attributeLines.Clear()
                continue
            }

            if ($trimmed.StartsWith('[', [System.StringComparison]::Ordinal)) {
                $attributeLines.Add($trimmed)
                continue
            }

            $methodMatch = [regex]::Match($trimmed, '^(?:public|protected|internal|private)\s+(?:static\s+)?(?:async\s+)?[A-Za-z_][A-Za-z0-9_<>,?.\[\]\s]*\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\(')
            if (-not $methodMatch.Success) {
                if (-not [string]::IsNullOrWhiteSpace($trimmed) -and -not $trimmed.StartsWith('///', [System.StringComparison]::Ordinal)) {
                    $attributeLines.Clear()
                }
                continue
            }

            $attributes = $attributeLines -join [Environment]::NewLine
            $attributeLines.Clear()
            if ([string]::IsNullOrEmpty($currentClass) -or -not [regex]::IsMatch($attributes, '\[Benchmark(?:\s*\([^\]]*\))?\]')) {
                continue
            }

            $categoryMatches = [regex]::Matches($attributes, '\[BenchmarkCategory\(\s*"(?<category>[^"]+)"\s*\)\]')
            if ($categoryMatches.Count -eq 0) {
                throw "Benchmark '$currentClass.$($methodMatch.Groups['name'].Value)' has no BenchmarkCategory declaration."
            }

            $isBaseline = [regex]::IsMatch($attributes, '\[Benchmark\(\s*Baseline\s*=\s*true\s*\)\]')
            foreach ($categoryMatch in $categoryMatches) {
                $category = $categoryMatch.Groups['category'].Value
                $key = $currentClass + [char] 0 + $category
                if (-not $categories.ContainsKey($key)) {
                    $categories[$key] = [pscustomobject] [ordered]@{
                        benchmarkClass = $currentClass
                        category = $category
                        methodCount = 0
                        baselineCount = 0
                    }
                }

                $categories[$key].methodCount++
                if ($isBaseline) {
                    $categories[$key].baselineCount++
                }
            }
        }
    }

    return [pscustomobject] [ordered]@{
        classes = @($classes | Sort-Object -Unique)
        categories = @($categories.Values | Sort-Object benchmarkClass, category)
    }
}

function Get-BenchmarkParameterNames {
    param(
        [Parameter(Mandatory)] [object[]] $PolicyRows,
        [Parameter(Mandatory)] [string] $BenchmarkClass
    )

    $expectedNames = $null
    foreach ($parameters in @($PolicyRows | ForEach-Object { [string] $_.parameters } | Sort-Object -Unique)) {
        if ([string]::IsNullOrEmpty($parameters)) {
            continue
        }

        if ($parameters -notmatch '^\[.*\]$') {
            throw "Benchmark class '$BenchmarkClass' has an invalid parameter identity '$parameters'."
        }
        $names = @(
            [regex]::Matches($parameters, '(?:^\[|, )(?<name>[A-Za-z_][A-Za-z0-9_]*)=') |
                ForEach-Object { $_.Groups['name'].Value }
        )
        if ($names.Count -eq 0) {
            throw "Benchmark class '$BenchmarkClass' has an invalid parameter identity '$parameters'."
        }
        if ($null -eq $expectedNames) {
            $expectedNames = $names
            continue
        }
        if (-not (Test-ExactStringSequence -Actual $names -Expected $expectedNames)) {
            throw "Benchmark class '$BenchmarkClass' uses inconsistent parameter identities."
        }
    }

    if ($null -eq $expectedNames) {
        return
    }

    return $expectedNames
}

function Assert-BenchmarkReports {
    param(
        [Parameter(Mandatory)] [string] $ExecutionDirectory,
        [Parameter(Mandatory)] [string] $ArtifactsDirectory,
        [Parameter(Mandatory)] [string] $Root,
        [Parameter(Mandatory)] [object[]] $ObservationRows
    )

    $manifestPath = Join-Path $Root 'eng/performance/baseline.json'
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $includedPolicyRows = @($manifest.policy.rows | Where-Object included)
    $expectedReports = @(
        $includedPolicyRows |
            ForEach-Object { [string] $_.benchmarkClass } |
            Sort-Object -Unique |
            ForEach-Object {
                [pscustomobject]@{
                    benchmarkClass = $_
                    fileName = "FunnySharp.Benchmarks.$_-report.csv"
                    receiptName = "$_-performance-receipt.json"
                }
            }
    )
    Assert-BenchmarkReportRows `
        -ExpectedRows $includedPolicyRows `
        -ActualRows $ObservationRows `
        -Description 'Performance observation proposal'
    $benchmarkArtifactsDirectory = Assert-SafeArtifactsSubdirectory -Path (Join-Path $ExecutionDirectory 'benchmark-artifacts') -ArtifactsDirectory $ArtifactsDirectory
    $resultsDirectory = Assert-SafeArtifactsSubdirectory -Path (Join-Path $benchmarkArtifactsDirectory 'results') -ArtifactsDirectory $ArtifactsDirectory
    if (-not (Test-Path -LiteralPath $resultsDirectory -PathType Container)) {
        throw "Benchmark results directory was not found: '$resultsDirectory'."
    }

    $actualCsvItems = @(Get-ChildItem -LiteralPath $resultsDirectory -Force | Where-Object {
            -not $_.PSIsContainer -and
            ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -eq 0 -and
            $_.Extension -ceq '.csv'
        })
    $actualCsvNames = @($actualCsvItems.Name | Sort-Object)
    $expectedCsvNames = @($expectedReports.fileName | Sort-Object)
    if (-not (Test-ExactStringSequence -Actual $actualCsvNames -Expected $expectedCsvNames)) {
        throw "Benchmark results must contain exactly these CSV reports: $($expectedCsvNames -join ', ')."
    }

    $actualReceiptNames = @(
        Get-ChildItem -LiteralPath $resultsDirectory -File -Filter '*-performance-receipt.json' |
            ForEach-Object Name |
            Sort-Object
    )
    $expectedReceiptNames = @($expectedReports.receiptName | Sort-Object)
    if (-not (Test-ExactStringSequence -Actual $actualReceiptNames -Expected $expectedReceiptNames)) {
        throw "Benchmark results must contain exactly these receipts: $($expectedReceiptNames -join ', ')."
    }

    $sourceManifest = Get-BenchmarkSourceManifest -Root $Root
    $expectedClasses = @($expectedReports.benchmarkClass | Sort-Object)
    if (-not (Test-ExactStringSequence -Actual @($sourceManifest.classes | Sort-Object) -Expected $expectedClasses)) {
        throw "Benchmark source classes must exactly match the report set: $($expectedClasses -join ', ')."
    }

    $sourceCategories = @{}
    foreach ($sourceCategory in $sourceManifest.categories) {
        $sourceCategories[$sourceCategory.benchmarkClass + [char] 0 + $sourceCategory.category] = $sourceCategory
    }

    $reportSummaries = [System.Collections.Generic.List[object]]::new()
    $reportCategories = @{}
    $rowCount = 0
    foreach ($expectedReport in $expectedReports) {
        $classPolicyRows = @($includedPolicyRows | Where-Object benchmarkClass -ceq $expectedReport.benchmarkClass)
        $parameterNames = @(Get-BenchmarkParameterNames -PolicyRows $classPolicyRows -BenchmarkClass $expectedReport.benchmarkClass)
        $reportPath = Get-SafeEvidenceFile -Path (Join-Path $resultsDirectory $expectedReport.fileName) -ArtifactsDirectory $ArtifactsDirectory
        $receiptPath = Get-SafeEvidenceFile -Path (Join-Path $resultsDirectory $expectedReport.receiptName) -ArtifactsDirectory $ArtifactsDirectory
        $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
        if ($receipt.schemaVersion -ne 1 -or $receipt.succeeded -ne $true) {
            throw "Benchmark receipt '$($expectedReport.receiptName)' is malformed or unsuccessful."
        }
        $receiptClasses = @($receipt.rows | ForEach-Object { [string] $_.benchmarkClass } | Sort-Object -Unique)
        if ($receiptClasses.Count -ne 1 -or $receiptClasses[0] -cne $expectedReport.benchmarkClass) {
            throw "Benchmark receipt '$($expectedReport.receiptName)' does not contain '$($expectedReport.benchmarkClass)'."
        }
        $declaredReports = @($receipt.reports)
        $declaredReportNames = @($declaredReports | ForEach-Object { [string] $_.file } | Sort-Object)
        $expectedDeclaredReports = @(
            "FunnySharp.Benchmarks.$($expectedReport.benchmarkClass)-report-github.md",
            $expectedReport.fileName,
            "FunnySharp.Benchmarks.$($expectedReport.benchmarkClass)-report.html"
        ) | Sort-Object
        if (-not (Test-ExactStringSequence -Actual $declaredReportNames -Expected $expectedDeclaredReports)) {
            throw "Benchmark receipt '$($expectedReport.receiptName)' does not declare the complete report set."
        }
        foreach ($declaredReport in $declaredReports) {
            $declaredPath = Get-SafeEvidenceFile -Path (Join-Path $resultsDirectory ([string] $declaredReport.file)) -ArtifactsDirectory $ArtifactsDirectory
            if ((Get-Sha256 -Path $declaredPath) -ne [string] $declaredReport.sha256) {
                throw "Benchmark report '$($declaredReport.file)' does not match its receipt."
            }
        }

        try {
            $rows = @(Import-Csv -LiteralPath $reportPath)
        }
        catch {
            throw "Benchmark report is not valid CSV: '$reportPath'."
        }

        if ($rows.Count -eq 0 -or $rows[0].PSObject.Properties.Name -notcontains 'Method' -or $rows[0].PSObject.Properties.Name -notcontains 'Categories') {
            throw "Benchmark report '$($expectedReport.fileName)' must contain Method and Categories columns with at least one row."
        }

        $reportRows = [System.Collections.Generic.List[object]]::new()

        $rowCount += $rows.Count
        foreach ($row in $rows) {
            if ([string]::IsNullOrWhiteSpace([string] $row.Method) -or [string]::IsNullOrWhiteSpace([string] $row.Categories)) {
                throw "Benchmark report '$($expectedReport.fileName)' contains a row without Method or Categories."
            }

            $categories = @([string] $row.Categories -split '\s*;\s*')
            if ($categories.Count -ne 1) {
                throw "Benchmark report '$($expectedReport.fileName)' must contain exactly one category per row."
            }
            foreach ($category in $categories) {
                if ([string]::IsNullOrWhiteSpace($category)) {
                    throw "Benchmark report '$($expectedReport.fileName)' contains an empty category."
                }

                $key = $expectedReport.benchmarkClass + [char] 0 + $category
                if (-not $reportCategories.ContainsKey($key)) {
                    $reportCategories[$key] = [pscustomobject] [ordered]@{
                        benchmarkClass = $expectedReport.benchmarkClass
                        category = $category
                        rowCount = 0
                        report = $expectedReport.fileName
                    }
                }
                $reportCategories[$key].rowCount++
                $parameterValues = @(
                    foreach ($parameterName in $parameterNames) {
                        $property = $row.PSObject.Properties[$parameterName]
                        if ($null -eq $property) {
                            throw "Benchmark report '$($expectedReport.fileName)' is missing parameter column '$parameterName'."
                        }
                        $parameterName + '=' + [string] $property.Value
                    }
                )
                $reportRows.Add([pscustomobject]@{
                        benchmarkClass = $expectedReport.benchmarkClass
                        category = $category
                        method = [string] $row.Method
                        parameters = if ($parameterValues.Count -eq 0) { '' } else { '[' + ($parameterValues -join ', ') + ']' }
                    })
            }
        }

        Assert-BenchmarkReportRows `
            -ExpectedRows $classPolicyRows `
            -ActualRows @($receipt.rows) `
            -Description "Benchmark receipt '$($expectedReport.receiptName)'"
        Assert-BenchmarkReportRows `
            -ExpectedRows @($receipt.rows) `
            -ActualRows @($reportRows) `
            -Description "Benchmark report '$($expectedReport.fileName)'"

        $reportSummaries.Add([pscustomobject] [ordered]@{
            benchmarkClass = $expectedReport.benchmarkClass
            report = $expectedReport.fileName
            sha256 = Get-Sha256 -Path $reportPath
            receipt = $expectedReport.receiptName
            receiptSha256 = Get-Sha256 -Path $receiptPath
            rowCount = $rows.Count
        })
    }

    $categorySummaries = [System.Collections.Generic.List[object]]::new()
    foreach ($key in @($reportCategories.Keys | Sort-Object)) {
        $reportCategory = $reportCategories[$key]
        if ($reportCategory.rowCount -lt 2) {
            throw "Benchmark category '$($reportCategory.benchmarkClass): $($reportCategory.category)' has fewer than two comparison rows."
        }
        if (-not $sourceCategories.ContainsKey($key)) {
            throw "Benchmark category '$($reportCategory.benchmarkClass): $($reportCategory.category)' is not declared in current benchmark sources."
        }

        $sourceCategory = $sourceCategories[$key]
        if ($sourceCategory.baselineCount -ne 1) {
            throw "Benchmark category '$($reportCategory.benchmarkClass): $($reportCategory.category)' must declare exactly one Benchmark(Baseline = true) method, found $($sourceCategory.baselineCount)."
        }
        $categorySummaries.Add([pscustomobject] [ordered]@{
                benchmarkClass = $reportCategory.benchmarkClass
                category = $reportCategory.category
                report = $reportCategory.report
                reportRowCount = $reportCategory.rowCount
                sourceMethodCount = $sourceCategory.methodCount
                sourceBaselineCount = $sourceCategory.baselineCount
            })
    }

    foreach ($key in $sourceCategories.Keys) {
        if (-not $reportCategories.ContainsKey($key)) {
            $sourceCategory = $sourceCategories[$key]
            throw "Benchmark source category '$($sourceCategory.benchmarkClass): $($sourceCategory.category)' has no report rows."
        }
    }

    return [pscustomobject] [ordered]@{
        artifactsDirectory = $benchmarkArtifactsDirectory
        resultsDirectory = $resultsDirectory
        reportCount = $reportSummaries.Count
        rowCount = $rowCount
        categoryCount = $categorySummaries.Count
        reports = @($reportSummaries | Sort-Object benchmarkClass)
        categories = @($categorySummaries | Sort-Object benchmarkClass, category)
    }
}

function Assert-ReleaseExecutionEvidence {
    param(
        [Parameter(Mandatory)] [string] $Directory,
        [Parameter(Mandatory)] [string] $ArtifactsDirectory,
        [Parameter(Mandatory)] [string] $Root
    )

    $executionDirectory = Assert-SafeArtifactsSubdirectory -Path $Directory -ArtifactsDirectory $ArtifactsDirectory
    if (-not (Test-Path -LiteralPath $executionDirectory -PathType Container)) {
        throw "ExecutionEvidenceDirectory was not found: '$executionDirectory'."
    }

    $evidencePath = Get-SafeEvidenceFile -Path (Join-Path $executionDirectory 'execution-evidence.json') -ArtifactsDirectory $ArtifactsDirectory
    try {
        $evidence = Get-Content -LiteralPath $evidencePath -Raw | ConvertFrom-Json
    }
    catch {
        throw "Execution evidence is not valid JSON: '$evidencePath'."
    }

    if ($evidence.schemaVersion -ne 2 -or $evidence.succeeded -ne $true) {
        throw 'Execution evidence must use schema version 2 and report succeeded=true.'
    }
    $expectedMode = if ($SkipBenchmarks) { 'benchmarkSkipped' } else { 'full' }
    if ($evidence.mode -ne $expectedMode -or $evidence.candidateCommit -notmatch '^[0-9a-f]{40}$') {
        throw "Execution evidence mode or candidate commit is invalid for '$expectedMode'."
    }
    $protocolPath = Join-Path $Root 'eng/release-protocol.json'
    if ($evidence.protocol.path -ne 'eng/release-protocol.json' -or
        $evidence.protocol.sha256 -ne (Get-Sha256 -Path $protocolPath)) {
        throw 'Execution evidence does not match the current release protocol.'
    }
    if ($evidence.isolatedNuGetCache -ne $true -or
        -not (Test-PathAtOrWithin -ChildPath ([string] $evidence.nugetPackagesDirectory) -ParentPath $executionDirectory)) {
        throw 'Execution evidence does not prove an output-local isolated NuGet package cache.'
    }
    $versionPreflightPath = Get-SafeEvidenceFile -Path (Join-Path $executionDirectory ([string] $evidence.versionPreflight)) -ArtifactsDirectory $ArtifactsDirectory
    if ($evidence.versionPreflightSha256 -ne (Get-Sha256 -Path $versionPreflightPath)) {
        throw 'Execution evidence does not match the package-version preflight bytes.'
    }
    $versionPreflight = Get-Content -LiteralPath $versionPreflightPath -Raw | ConvertFrom-Json
    if ($versionPreflight.schemaVersion -ne 1 -or
        $versionPreflight.status -ne 'passed' -or
        $versionPreflight.candidateCommit -ne $evidence.candidateCommit -or
        @($versionPreflight.checks).Count -eq 0 -or
        @($versionPreflight.checks | Where-Object status -ne 'absent').Count -ne 0) {
        throw 'Execution evidence does not contain a complete passing package-version preflight.'
    }
    $versionFinalPath = Get-SafeEvidenceFile -Path (Join-Path $executionDirectory ([string] $evidence.versionFinal)) -ArtifactsDirectory $ArtifactsDirectory
    if ($evidence.versionFinalSha256 -ne (Get-Sha256 -Path $versionFinalPath)) {
        throw 'Execution evidence does not match the final package-version check bytes.'
    }
    $versionFinal = Get-Content -LiteralPath $versionFinalPath -Raw | ConvertFrom-Json
    if ($versionFinal.schemaVersion -ne 1 -or
        $versionFinal.status -ne 'passed' -or
        $versionFinal.candidateCommit -ne $evidence.candidateCommit -or
        @($versionFinal.checks).Count -eq 0 -or
        @($versionFinal.checks | Where-Object status -ne 'absent').Count -ne 0) {
        throw 'Execution evidence does not contain a complete passing final package-version check.'
    }
    if (-not (Test-EquivalentSourceFingerprint -Left $evidence.sourceFingerprintBefore -Right $evidence.sourceFingerprintAfter)) {
        throw 'Execution evidence source fingerprints do not match.'
    }

    $currentFingerprint = Get-SourceFingerprint -Root $Root
    if (-not (Test-EquivalentSourceFingerprint -Left $evidence.sourceFingerprintBefore -Right $currentFingerprint)) {
        throw 'The current source fingerprint does not match the release execution evidence.'
    }

    $expectedCommandArguments = @{
        ExecutionDirectory = $executionDirectory
        CompatibilityPackageFeed = $CompatibilityPackageFeed
        Root = $Root
        CompatibilityRuntimeIdentifier = $CompatibilityRuntimeIdentifier
        BenchmarksSkipped = $SkipBenchmarks.IsPresent
    }
    $expectedCommands = Get-ExpectedReleaseCommands @expectedCommandArguments
    $expectedNames = @($expectedCommands | ForEach-Object name)
    $candidateCommands = @($evidence.candidateCommands)
    if (-not (Test-ExactStringSequence -Actual $candidateCommands -Expected $expectedNames)) {
        throw "Execution evidence must declare exactly these candidate commands: $($expectedNames -join ', ')."
    }

    $commands = @($evidence.commands)
    if ($commands.Count -ne $expectedNames.Count) {
        throw "Execution evidence must contain exactly $($expectedNames.Count) candidate command receipts."
    }

    $logPaths = [System.Collections.Generic.List[string]]::new()
    $verifiedCommands = [System.Collections.Generic.List[object]]::new()
    $logTextByName = @{}
    for ($index = 0; $index -lt $expectedNames.Count; $index++) {
        $ordinal = $index + 1
        $name = $expectedNames[$index]
        $expectedCommand = $expectedCommands[$index]
        $command = $commands[$index]
        $prefix = '{0:D2}-{1}' -f $ordinal, $name
        $expectedOutputLog = 'logs/' + $prefix + '.stdout.log'
        $expectedErrorLog = 'logs/' + $prefix + '.stderr.log'
        $expectedReceipt = 'receipts/' + $prefix + '.json'
        if ($command.schemaVersion -ne 1 -or $command.name -ne $name -or $command.exitCode -ne 0 -or
            $command.standardOutputLog -ne $expectedOutputLog -or $command.standardErrorLog -ne $expectedErrorLog) {
            throw "Execution receipt for '$name' is missing, out of order, malformed, or unsuccessful."
        }
        Assert-CanonicalReleaseCommand -Command $command -Expected $expectedCommand -Description "Execution manifest command '$name'"

        $receiptPath = Get-SafeEvidenceFile -Path (Join-Path $executionDirectory $expectedReceipt) -ArtifactsDirectory $ArtifactsDirectory
        try {
            $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
        }
        catch {
            throw "Execution receipt is not valid JSON: '$receiptPath'."
        }
        if ($receipt.schemaVersion -ne 1 -or $receipt.name -ne $name -or $receipt.exitCode -ne 0 -or
            $receipt.standardOutputLog -ne $expectedOutputLog -or $receipt.standardErrorLog -ne $expectedErrorLog -or
            $receipt.standardOutputSha256 -ne $command.standardOutputSha256 -or
            $receipt.standardErrorSha256 -ne $command.standardErrorSha256) {
            throw "Execution receipt '$expectedReceipt' does not match the execution manifest."
        }
        Assert-CanonicalReleaseCommand -Command $receipt -Expected $expectedCommand -Description "Execution receipt '$expectedReceipt'"

        $standardOutput = Get-ExecutionLogText -ExecutionDirectory $executionDirectory -RelativePath $expectedOutputLog -ExpectedSha256 $command.standardOutputSha256 -ArtifactsDirectory $ArtifactsDirectory
        $standardError = Get-ExecutionLogText -ExecutionDirectory $executionDirectory -RelativePath $expectedErrorLog -ExpectedSha256 $command.standardErrorSha256 -ArtifactsDirectory $ArtifactsDirectory
        $logPaths.Add($expectedOutputLog)
        $logPaths.Add($expectedErrorLog)
        $logTextByName[$name] = $standardOutput.text + [Environment]::NewLine + $standardError.text
        $verifiedCommands.Add([pscustomobject] [ordered]@{
                name = $name
                fileName = $expectedCommand.fileName
                arguments = $expectedCommand.arguments
                workingDirectory = $expectedCommand.workingDirectory
                receipt = $expectedReceipt
                receiptSha256 = Get-Sha256 -Path $receiptPath
                standardOutputLog = $expectedOutputLog
                standardOutputSha256 = $standardOutput.sha256
                standardErrorLog = $expectedErrorLog
                standardErrorSha256 = $standardError.sha256
            })
    }

    $expectedLogPaths = for ($index = 0; $index -lt $expectedNames.Count; $index++) {
        $prefix = '{0:D2}-{1}' -f ($index + 1), $expectedNames[$index]
        'logs/' + $prefix + '.stdout.log'
        'logs/' + $prefix + '.stderr.log'
    }
    if ($logPaths.Count -ne $expectedLogPaths.Count -or (Compare-Object -ReferenceObject $expectedLogPaths -DifferenceObject @($logPaths))) {
        throw 'Execution evidence does not contain the expected fixed log set.'
    }

    $logsDirectory = Assert-SafeArtifactsSubdirectory -Path (Join-Path $executionDirectory 'logs') -ArtifactsDirectory $ArtifactsDirectory
    $actualLogItems = @(Get-ChildItem -LiteralPath $logsDirectory -Force)
    $expectedLogNames = @($expectedLogPaths | ForEach-Object { [System.IO.Path]::GetFileName($_) } | Sort-Object)
    $actualLogNames = @($actualLogItems | ForEach-Object {
            if ($_.PSIsContainer -or ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Execution logs directory contains an invalid entry: '$($_.FullName)'."
            }
            $_.Name
        } | Sort-Object)
    if ($actualLogNames.Count -ne $expectedLogNames.Count -or (Compare-Object -ReferenceObject $expectedLogNames -DifferenceObject $actualLogNames)) {
        throw 'Execution logs directory does not contain exactly the expected fixed log set.'
    }

    $receiptsDirectory = Assert-SafeArtifactsSubdirectory -Path (Join-Path $executionDirectory 'receipts') -ArtifactsDirectory $ArtifactsDirectory
    $actualReceiptItems = @(Get-ChildItem -LiteralPath $receiptsDirectory -Force)
    $expectedReceiptNames = for ($index = 0; $index -lt $expectedNames.Count; $index++) {
        '{0:D2}-{1}.json' -f ($index + 1), $expectedNames[$index]
    }
    $actualReceiptNames = @($actualReceiptItems | ForEach-Object {
            if ($_.PSIsContainer -or ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Execution receipts directory contains an invalid entry: '$($_.FullName)'."
            }
            $_.Name
        } | Sort-Object)
    if ($actualReceiptNames.Count -ne $expectedReceiptNames.Count -or (Compare-Object -ReferenceObject ($expectedReceiptNames | Sort-Object) -DifferenceObject $actualReceiptNames)) {
        throw 'Execution receipts directory does not contain exactly the expected fixed receipt set.'
    }

    $buildLog = $logTextByName['build']
    if ($buildLog -notmatch '(?im)^\s*Build succeeded\.\s*$' -or
        $buildLog -notmatch '(?im)^\s*0 Warning\(s\)\s*$' -or
        $buildLog -notmatch '(?im)^\s*0 Error\(s\)\s*$') {
        throw 'Build evidence must report Build succeeded with 0 Warning(s) and 0 Error(s).'
    }

    $testLog = $logTextByName['test']
    $testSummary = [regex]::Match(
        $testLog,
        '(?is)Test run summary:\s*Passed!.*?\btotal:\s*(?<total>\d+).*?\bfailed:\s*0.*?\bsucceeded:\s*(?<succeeded>\d+).*?\bskipped:\s*0')
    if (-not $testSummary.Success -or
        [int] $testSummary.Groups['total'].Value -le 0 -or
        $testSummary.Groups['total'].Value -ne $testSummary.Groups['succeeded'].Value) {
        throw 'Test evidence must report a positive all-passing count with 0 failed and 0 skipped.'
    }
    $testCount = [int] $testSummary.Groups['total'].Value
    $testAssemblies = @(
        [System.IO.Path]::GetFullPath((Join-Path $Root 'tests/FunnySharp.Tests/bin/Release/net10.0/FunnySharp.Tests.dll')),
        [System.IO.Path]::GetFullPath((Join-Path $Root 'tests/FunnySharp.AspNetCore.Tests/bin/Release/net10.0/FunnySharp.AspNetCore.Tests.dll'))
    )
    foreach ($testAssembly in $testAssemblies) {
        $successPattern = '(?im)^\s*' + [regex]::Escape($testAssembly) + '\s+\(net10\.0\|[^)]*\)\s+passed\s+\([^)]*\)\s*$'
        if ($testLog -notmatch $successPattern) {
            throw "Test evidence does not contain a successful result line for '$testAssembly'."
        }
    }

    if ($logTextByName['examples'] -notmatch [regex]::Escape('FunnySharp examples passed.')) {
        throw 'Examples evidence did not contain the success message.'
    }
    if ($logTextByName['aspnetcore-examples'] -notmatch [regex]::Escape('FunnySharp ASP.NET Core example endpoints mapped.')) {
        throw 'ASP.NET Core examples evidence did not contain the success message.'
    }

    $benchmarkEvidence = [pscustomobject] [ordered]@{ status = 'skipped-by-protocol' }
    if (-not $SkipBenchmarks) {
        $benchmarkLog = $logTextByName['benchmark']
        $completed = [regex]::Matches($benchmarkLog, '(?i)executed benchmarks:\s*(?<count>\d+)') |
            ForEach-Object { [int] $_.Groups['count'].Value } |
            Measure-Object -Maximum
        if ($benchmarkLog -notmatch '(?i)BenchmarkRunner:\s*Finish' -or
            $benchmarkLog -match '(?i)benchmark has failed|failed benchmarks?|build error|error\s+CS\d+' -or
            $null -eq $completed.Maximum -or $completed.Maximum -le 0) {
            throw 'Benchmark evidence must report completed benchmarks with no failures.'
        }

        $proposalPath = Get-SafeEvidenceFile -Path (Join-Path $executionDirectory 'performance-observation-proposal.json') -ArtifactsDirectory $ArtifactsDirectory
        $proposal = Get-Content -LiteralPath $proposalPath -Raw | ConvertFrom-Json
        if ($proposal.schemaVersion -ne 1 -or @($proposal.rows).Count -le 0) {
            throw 'Performance observation proposal is missing or malformed.'
        }
        $benchmarkEvidence = Assert-BenchmarkReports `
            -ExecutionDirectory $executionDirectory `
            -ArtifactsDirectory $ArtifactsDirectory `
            -Root $Root `
            -ObservationRows @($proposal.rows)
        $benchmarkEvidence.status = 'verified'
        $benchmarkEvidence | Add-Member -NotePropertyName executed -NotePropertyValue $completed.Maximum
        $benchmarkEvidence | Add-Member -NotePropertyName observationProposal -NotePropertyValue 'performance-observation-proposal.json'
        $benchmarkEvidence | Add-Member -NotePropertyName observationProposalSha256 -NotePropertyValue (Get-Sha256 -Path $proposalPath)
    }

    return [pscustomobject] [ordered]@{
        directory = $executionDirectory
        manifest = 'execution-evidence.json'
        manifestSha256 = Get-Sha256 -Path $evidencePath
        sourceFingerprint = $currentFingerprint
        commands = $verifiedCommands
        build = [pscustomobject] [ordered]@{ warnings = 0; errors = 0 }
        tests = [pscustomobject] [ordered]@{ total = $testCount; succeeded = $testCount; failed = 0; skipped = 0; assemblies = $testAssemblies }
        examples = [pscustomobject] [ordered]@{ core = 'passed'; aspNetCore = 'passed' }
        formatter = [pscustomobject] [ordered]@{ exitCode = 0 }
        benchmarks = $benchmarkEvidence
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
            ExitCode = $process.ExitCode
            StandardOutput = $standardOutputTask.GetAwaiter().GetResult()
            StandardError = $standardErrorTask.GetAwaiter().GetResult()
        }
    }
    finally {
        $process.Dispose()
    }
}
function Get-ExternalValue {
    param(
        [Parameter(Mandatory)] [string] $FileName,
        [string[]] $Arguments = @(),
        [Parameter(Mandatory)] [string] $WorkingDirectory
    )

    $commandText = if ($Arguments.Count -eq 0) {
        $FileName
    }
    else {
        $FileName + ' ' + ($Arguments -join ' ')
    }

    try {
        $result = Invoke-ExternalProcess -FileName $FileName -Arguments $Arguments -WorkingDirectory $WorkingDirectory
    }
    catch {
        throw "Failed to capture environment via '$commandText': $($_.Exception.Message)"
    }

    if ($result.ExitCode -eq 0) {
        return $result.StandardOutput.Trim()
    }

    $message = "$commandText exited with code $($result.ExitCode)."
    $standardError = $result.StandardError.Trim()
    if (-not [string]::IsNullOrWhiteSpace($standardError)) {
        $message += " $standardError"
    }

    throw $message
}

function Get-EnvironmentEvidence {
    param([Parameter(Mandatory)] [string] $Root)

    $dotnetVersion = Get-ExternalValue -FileName 'dotnet' -Arguments @('--version') -WorkingDirectory $Root
    $dotnetInfo = Get-ExternalValue -FileName 'dotnet' -Arguments @('--info') -WorkingDirectory $Root
    $commit = Get-ExternalValue -FileName 'git' -Arguments @('rev-parse', 'HEAD') -WorkingDirectory $Root
    $branch = Get-ExternalValue -FileName 'git' -Arguments @('branch', '--show-current') -WorkingDirectory $Root
    $worktree = Get-ExternalValue -FileName 'git' -Arguments @('rev-parse', '--show-toplevel') -WorkingDirectory $Root
    $status = Get-ExternalValue -FileName 'git' -Arguments @('status', '--porcelain=v1') -WorkingDirectory $Root

    return [pscustomobject] [ordered]@{
        collectedAtUtc = [DateTime]::UtcNow.ToString('O')
        repositoryRoot = $Root
        worktree = $worktree
        commit = $commit
        branch = $branch
        worktreeStatus = $status
        operatingSystem = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription
        operatingSystemArchitecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
        processArchitecture = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString()
        runtimeIdentifier = [System.Runtime.InteropServices.RuntimeInformation]::RuntimeIdentifier
        frameworkDescription = [System.Runtime.InteropServices.RuntimeInformation]::FrameworkDescription
        powershellVersion = $PSVersionTable.PSVersion.ToString()
        dotnetVersion = $dotnetVersion
        dotnetInfo = $dotnetInfo
    }
}

function Get-SharedFrameworkDirectory {
    param(
        [Parameter(Mandatory)] [string] $FrameworkName,
        [Parameter(Mandatory)] [string] $Root
    )

    $result = Invoke-ExternalProcess -FileName 'dotnet' -Arguments @('--list-runtimes') -WorkingDirectory $Root
    if ($result.ExitCode -ne 0) {
        throw "dotnet --list-runtimes failed with exit code $($result.ExitCode)."
    }

    $pattern = '^' + [regex]::Escape($FrameworkName) + ' (?<version>10\.[^ ]+) \[(?<path>.+)\]$'
    $candidates = @(
        $result.StandardOutput -split "`r?`n" |
            ForEach-Object {
                $match = [regex]::Match($_, $pattern)
                if ($match.Success) {
                    [pscustomobject]@{
                        Version = $match.Groups['version'].Value
                        ParsedVersion = [Version]::Parse(($match.Groups['version'].Value -split '-')[0])
                        Root = $match.Groups['path'].Value
                    }
                }
            } |
            Sort-Object ParsedVersion -Descending
    )

    if ($candidates.Count -eq 0) {
        throw "No .NET 10 '$FrameworkName' shared framework was found."
    }

    return Join-Path $candidates[0].Root $candidates[0].Version
}

function Get-NullabilityInfo {
    param(
        [Parameter(Mandatory)] $Provider,
        [Parameter(Mandatory)] [System.Reflection.NullabilityInfoContext] $Context
    )

    try {
        return $Context.Create($Provider)
    }
    catch {
        # Older metadata and some member kinds do not expose nullable annotations.
        return $null
    }
}

function Format-ApiType {
    param(
        [Parameter(Mandatory)] [System.Type] $Type,
        $NullabilityInfo
    )

    $nullabilitySuffix = if ($null -ne $NullabilityInfo -and
        $NullabilityInfo.ReadState -eq [System.Reflection.NullabilityState]::Nullable) { '?' } else { '' }
    $elementNullability = if ($null -eq $NullabilityInfo) { $null } else { $NullabilityInfo.ElementType }

    if ($Type.IsByRef) {
        return (Format-ApiType -Type $Type.GetElementType() -NullabilityInfo $elementNullability) + '&'
    }

    if ($Type.IsPointer) {
        return (Format-ApiType -Type $Type.GetElementType() -NullabilityInfo $elementNullability) + '*'
    }

    if ($Type.IsArray) {
        $rank = $Type.GetArrayRank()
        $suffix = if ($rank -eq 1) { '[]' } else { '[' + ([string]::new(',', $rank - 1)) + ']' }
        return (Format-ApiType -Type $Type.GetElementType() -NullabilityInfo $elementNullability) + $suffix + $nullabilitySuffix
    }

    if ($Type.IsGenericParameter) {
        return $Type.Name + $nullabilitySuffix
    }

    if ($Type.IsGenericType) {
        $genericName = $Type.GetGenericTypeDefinition().FullName -replace '`[0-9]+$', ''
        $typeArguments = @($Type.GetGenericArguments())
        $nullabilityArguments = @()
        if ($null -ne $NullabilityInfo) {
            $nullabilityArguments = @($NullabilityInfo.GenericTypeArguments)
        }
        $arguments = for ($index = 0; $index -lt $typeArguments.Count; $index++) {
            $argumentNullability = if ($index -lt $nullabilityArguments.Count) { $nullabilityArguments[$index] } else { $null }
            Format-ApiType -Type $typeArguments[$index] -NullabilityInfo $argumentNullability
        }
        return $genericName + '<' + ($arguments -join ', ') + '>' + $nullabilitySuffix
    }

    return ($Type.FullName ?? $Type.Name) + $nullabilitySuffix
}

function Format-ApiParameter {
    param(
        [Parameter(Mandatory)] [System.Reflection.ParameterInfo] $Parameter,
        [Parameter(Mandatory)] [System.Reflection.NullabilityInfoContext] $NullabilityContext
    )

    $modifier = if ($Parameter.IsOut) {
        'out '
    }
    elseif ($Parameter.ParameterType.IsByRef) {
        'ref '
    }
    else {
        ''
    }

    $typeName = Format-ApiType `
        -Type $Parameter.ParameterType `
        -NullabilityInfo (Get-NullabilityInfo -Provider $Parameter -Context $NullabilityContext)
    return $modifier + $typeName + ' ' + $Parameter.Name
}

function Get-PublicApiInventory {
    param(
        [Parameter(Mandatory)] [string[]] $AssemblyPaths,
        [Parameter(Mandatory)] [string] $Root
    )

    $nullabilityContext = [System.Reflection.NullabilityInfoContext]::new()
    $assemblyInventories = [System.Collections.Generic.List[object]]::new()

    $searchDirectories = @(
        $AssemblyPaths | ForEach-Object { Split-Path -Parent $_ }
        Get-SharedFrameworkDirectory -FrameworkName 'Microsoft.AspNetCore.App' -Root $Root
    ) | Select-Object -Unique
    $resolver = [ResolveEventHandler] {
        param($sender, $eventArgs)

        $assemblyName = [Reflection.AssemblyName]::new($eventArgs.Name).Name
        foreach ($directory in $searchDirectories) {
            $candidate = Join-Path $directory "$assemblyName.dll"
            if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                return [Reflection.Assembly]::LoadFrom($candidate)
            }
        }

        return $null
    }

    [AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)

    try {
        foreach ($assemblyPath in $AssemblyPaths | Sort-Object) {
            $assembly = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
            $types = @($assembly.GetExportedTypes() | Sort-Object FullName)
            $typeInventories = [System.Collections.Generic.List[object]]::new()
            $assemblyMetadata = @(
                $assembly.GetCustomAttributesData() |
                    Where-Object AttributeType -eq ([System.Reflection.AssemblyMetadataAttribute]) |
                    ForEach-Object {
                        [pscustomobject]@{
                            key = $_.ConstructorArguments[0].Value
                            value = $_.ConstructorArguments[1].Value
                        }
                    }
            )

            foreach ($type in $types) {
            $constructors = @(
                $type.GetConstructors([System.Reflection.BindingFlags]'Public, Instance, Static, DeclaredOnly') |
                ForEach-Object {
                    $parameters = @($_.GetParameters() | ForEach-Object { Format-ApiParameter -Parameter $_ -NullabilityContext $nullabilityContext }) -join ', '
                    'ctor(' + $parameters + ')'
                } |
                Sort-Object
            )

            $methods = @(
                $type.GetMethods([System.Reflection.BindingFlags]'Public, Instance, Static, DeclaredOnly') |
                Where-Object { -not $_.IsSpecialName -or $_.Name.StartsWith('op_', [System.StringComparison]::Ordinal) } |
                ForEach-Object {
                    $parameters = @($_.GetParameters() | ForEach-Object { Format-ApiParameter -Parameter $_ -NullabilityContext $nullabilityContext }) -join ', '
                    $genericParameters = @($_.GetGenericArguments() | ForEach-Object { $_.Name })
                    $genericSuffix = if ($genericParameters.Count -eq 0) { '' } else { '<' + ($genericParameters -join ', ') + '>' }
                    $returnType = Format-ApiType `
                        -Type $_.ReturnType `
                        -NullabilityInfo (Get-NullabilityInfo -Provider $_.ReturnParameter -Context $nullabilityContext)
                    $staticPrefix = if ($_.IsStatic) { 'static ' } else { '' }
                    $staticPrefix + $returnType + ' ' + $_.Name + $genericSuffix + '(' + $parameters + ')'
                } |
                Sort-Object
            )

            $properties = @(
                $type.GetProperties([System.Reflection.BindingFlags]'Public, Instance, Static, DeclaredOnly') |
                Where-Object { $_.GetMethod -or $_.SetMethod } |
                ForEach-Object {
                    $indexParameters = @($_.GetIndexParameters() | ForEach-Object { Format-ApiParameter -Parameter $_ -NullabilityContext $nullabilityContext }) -join ', '
                    $name = if ($indexParameters.Length -eq 0) { $_.Name } else { $_.Name + '[' + $indexParameters + ']' }
                    (Format-ApiType -Type $_.PropertyType -NullabilityInfo (Get-NullabilityInfo -Provider $_ -Context $nullabilityContext)) + ' ' + $name
                } |
                Sort-Object
            )

            $fields = @(
                $type.GetFields([System.Reflection.BindingFlags]'Public, Instance, Static, DeclaredOnly') |
                ForEach-Object {
                    $staticPrefix = if ($_.IsStatic) { 'static ' } else { '' }
                    $staticPrefix + (Format-ApiType -Type $_.FieldType -NullabilityInfo (Get-NullabilityInfo -Provider $_ -Context $nullabilityContext)) + ' ' + $_.Name
                } |
                Sort-Object
            )

            $events = @(
                $type.GetEvents([System.Reflection.BindingFlags]'Public, Instance, Static, DeclaredOnly') |
                ForEach-Object {
                    (Format-ApiType -Type $_.EventHandlerType -NullabilityInfo (Get-NullabilityInfo -Provider $_ -Context $nullabilityContext)) + ' ' + $_.Name
                } |
                Sort-Object
            )

                $typeInventories.Add([pscustomobject] [ordered]@{
                    name = Format-ApiType -Type $type
                    kind = if ($type.IsInterface) { 'interface' } elseif ($type.IsEnum) { 'enum' } elseif ($type.IsValueType) { 'struct' } elseif ($type.BaseType -eq [System.MulticastDelegate]) { 'delegate' } else { 'class' }
                    constructors = $constructors
                    methods = $methods
                    properties = $properties
                    fields = $fields
                    events = $events
                    })
            }

            $assemblyInventories.Add([pscustomobject] [ordered]@{
                    path = $assemblyPath
                    sha256 = Get-Sha256 -Path $assemblyPath
                    identity = $assembly.FullName
                    isTrimmable = (($assemblyMetadata | Where-Object key -eq 'IsTrimmable').value -eq 'True')
                    types = $typeInventories
                })
        }
    }
    finally {
        [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolver)
    }

    return $assemblyInventories
}

function Get-XmlDocumentationInventory {
    param([Parameter(Mandatory)] [string[]] $Paths)

    $inventories = [System.Collections.Generic.List[object]]::new()
    foreach ($path in $Paths | Sort-Object) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "XML documentation file was not found: '$path'."
        }

        [xml] $document = Get-Content -LiteralPath $path -Raw
        $members = @($document.doc.members.member)
        $missing = @(
            $members |
                Where-Object {
                    $null -eq $_.SelectSingleNode('summary') -and
                    $null -eq $_.SelectSingleNode('inheritdoc')
                } |
                ForEach-Object { $_.GetAttribute('name') }
        )
        if ($missing.Count -gt 0) {
            throw "XML documentation '$path' has $($missing.Count) member(s) without summary or inheritdoc: $($missing -join ', ')."
        }

        $inventories.Add([pscustomobject] [ordered]@{
                path = $path
                sha256 = Get-Sha256 -Path $path
                members = $members.Count
                missingSummaryOrInheritdoc = 0
            })
    }

    return $inventories
}

function ConvertTo-NormalizedNuspecTargetFramework {
    param([string] $TargetFramework)

    if ($TargetFramework -eq '.NETCoreApp10.0') {
        return 'net10.0'
    }

    return $TargetFramework
}

function Get-PackageInspection {
    param([Parameter(Mandatory)] [string] $PackagePath)

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
    try {
        $entries = @($archive.Entries | Sort-Object FullName)
        $nuspecEntries = @($entries | Where-Object { $_.FullName -match '^[^/]+\.nuspec$' })
        if ($nuspecEntries.Count -ne 1) {
            throw "Package '$PackagePath' must contain exactly one root nuspec, found $($nuspecEntries.Count)."
        }

        $reader = [System.IO.StreamReader]::new($nuspecEntries[0].Open())
        try {
            [xml] $nuspec = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        $metadata = $nuspec.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']")
        if ($null -eq $metadata) {
            throw "Package '$PackagePath' has no nuspec metadata."
        }

        $dependencyGroups = [System.Collections.Generic.List[object]]::new()
        foreach ($group in @($metadata.SelectNodes("*[local-name()='dependencies']/*[local-name()='group']"))) {
            $dependencies = [System.Collections.Generic.List[object]]::new()
            foreach ($dependency in @($group.SelectNodes("*[local-name()='dependency']"))) {
                $dependencies.Add([pscustomobject] [ordered]@{
                        id = $dependency.GetAttribute('id')
                        version = $dependency.GetAttribute('version')
                        exclude = $dependency.GetAttribute('exclude')
                    })
            }

            $dependencyGroups.Add([pscustomobject] [ordered]@{
                    targetFramework = ConvertTo-NormalizedNuspecTargetFramework $group.GetAttribute('targetFramework')
                    dependencies = $dependencies
                })
        }

        $frameworkReferenceGroups = [System.Collections.Generic.List[object]]::new()
        foreach ($group in @($metadata.SelectNodes("*[local-name()='frameworkReferences']/*[local-name()='group']"))) {
            $references = [System.Collections.Generic.List[string]]::new()
            foreach ($reference in @($group.SelectNodes("*[local-name()='frameworkReference']"))) {
                $references.Add($reference.GetAttribute('name'))
            }

            $frameworkReferenceGroups.Add([pscustomobject] [ordered]@{
                    targetFramework = ConvertTo-NormalizedNuspecTargetFramework $group.GetAttribute('targetFramework')
                    references = @($references | Sort-Object)
                })
        }

        $entryInventories = [System.Collections.Generic.List[object]]::new()
        foreach ($entry in $entries) {
            $entryStream = $entry.Open()
            $hashAlgorithm = [System.Security.Cryptography.SHA256]::Create()
            try {
                $entryInventories.Add([pscustomobject] [ordered]@{
                        path = $entry.FullName
                        length = $entry.Length
                        sha256 = [System.Convert]::ToHexString($hashAlgorithm.ComputeHash($entryStream)).ToLowerInvariant()
                    })
            }
            finally {
                $hashAlgorithm.Dispose()
                $entryStream.Dispose()
            }
        }

        $license = $metadata.SelectSingleNode("*[local-name()='license']")
        return [pscustomobject] [ordered]@{
            path = $PackagePath
            fileName = [System.IO.Path]::GetFileName($PackagePath)
            sha256 = Get-Sha256 -Path $PackagePath
            id = $metadata.SelectSingleNode("*[local-name()='id']").InnerText
            version = $metadata.SelectSingleNode("*[local-name()='version']").InnerText
            readme = $metadata.SelectSingleNode("*[local-name()='readme']").InnerText
            licenseType = if ($null -eq $license) { $null } else { $license.GetAttribute('type') }
            licenseExpression = if ($null -eq $license) { $null } else { $license.InnerText }
            entries = $entryInventories
            dependencyGroups = $dependencyGroups
            frameworkReferenceGroups = $frameworkReferenceGroups
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Assert-PackageLayout {
    param(
        [Parameter(Mandatory)] $Package,
        [Parameter(Mandatory)] [string] $ExpectedId,
        [Parameter(Mandatory)] [string] $AssemblyPath,
        [Parameter(Mandatory)] [string] $XmlDocumentationPath,
        [Parameter(Mandatory)] [string] $ReadmePath
    )

    if ($Package.id -ne $ExpectedId) {
        throw "Expected package id '$ExpectedId', found '$($Package.id)'."
    }

    $expectedFileName = "$ExpectedId.$($Package.version).nupkg"
    if ($Package.fileName -ne $expectedFileName) {
        throw "Package filename '$($Package.fileName)' does not match nuspec identity '$expectedFileName'."
    }

    if ($Package.readme -ne 'README.md' -or $Package.entries.path -notcontains 'README.md') {
        throw "Package '$ExpectedId' must declare and include root README.md."
    }

    foreach ($path in @("lib/net10.0/$ExpectedId.dll", "lib/net10.0/$ExpectedId.xml")) {
        if ($Package.entries.path -notcontains $path) {
            throw "Package '$ExpectedId' is missing '$path'."
        }
    }

    $expectedHashes = @{
        "lib/net10.0/$ExpectedId.dll" = Get-Sha256 -Path $AssemblyPath
        "lib/net10.0/$ExpectedId.xml" = Get-Sha256 -Path $XmlDocumentationPath
        'README.md' = Get-Sha256 -Path $ReadmePath
    }
    foreach ($entryPath in $expectedHashes.Keys) {
        $entry = $Package.entries | Where-Object path -eq $entryPath
        if ($entry.sha256 -ne $expectedHashes[$entryPath]) {
            throw "Package '$ExpectedId' entry '$entryPath' does not match the release candidate file."
        }
    }

    if ($Package.licenseType -ne 'expression' -or $Package.licenseExpression -ne 'MIT') {
        throw "Package '$ExpectedId' must declare <license type=`"expression`">MIT</license>."
    }
}

function Assert-CorePackageDependencies {
    param([Parameter(Mandatory)] $Package)

    if ($Package.dependencyGroups.Count -ne 1 -or $Package.dependencyGroups[0].targetFramework -ne 'net10.0') {
        throw 'FunnySharp must have exactly one net10.0 dependency group.'
    }

    if ($Package.dependencyGroups[0].dependencies.Count -ne 0) {
        throw 'FunnySharp net10.0 dependency group must be empty.'
    }

    if ($Package.frameworkReferenceGroups.Count -ne 0) {
        throw 'FunnySharp must not declare framework references in its NuGet package.'
    }
}

function Assert-AspNetCorePackageDependencies {
    param(
        [Parameter(Mandatory)] $Package,
        [Parameter(Mandatory)] [string] $CoreVersion
    )

    if ($Package.dependencyGroups.Count -ne 1 -or $Package.dependencyGroups[0].targetFramework -ne 'net10.0') {
        throw 'FunnySharp.AspNetCore must have exactly one net10.0 dependency group.'
    }

    $dependencies = @($Package.dependencyGroups[0].dependencies)
    if ($dependencies.Count -ne 1 -or $dependencies[0].id -ne 'FunnySharp') {
        throw 'FunnySharp.AspNetCore must have FunnySharp as its only package dependency.'
    }
    if ($dependencies[0].version -ne $CoreVersion) {
        throw "FunnySharp.AspNetCore must depend on FunnySharp version '$CoreVersion'."
    }

    if ($Package.frameworkReferenceGroups.Count -ne 1 -or $Package.frameworkReferenceGroups[0].targetFramework -ne 'net10.0') {
        throw 'FunnySharp.AspNetCore must have exactly one net10.0 framework reference group.'
    }

    $references = @($Package.frameworkReferenceGroups[0].references)
    if ($references.Count -ne 1 -or $references[0] -ne 'Microsoft.AspNetCore.App') {
        throw 'FunnySharp.AspNetCore must have Microsoft.AspNetCore.App as its only framework reference.'
    }
}

function Find-PackageArchive {
    param(
        [Parameter(Mandatory)] [System.IO.FileInfo[]] $Files,
        [Parameter(Mandatory)] [string] $PackageId,
        [Parameter(Mandatory)] [string] $Extension
    )

    $pattern = '^' + [regex]::Escape($PackageId) + '\.(?<version>[0-9][^/]*)\.' + [regex]::Escape($Extension) + '$'
    $matches = @($Files | Where-Object { $_.Name -match $pattern })
    if ($matches.Count -ne 1) {
        throw "Expected exactly one $Extension archive for '$PackageId', found $($matches.Count)."
    }

    return $matches[0]
}

function Invoke-DocumentationVerification {
    param(
        [Parameter(Mandatory)] [string] $ScriptPath,
        [Parameter(Mandatory)] [string] $Root,
        [Parameter(Mandatory)] [string] $EvidenceDirectory
    )

    if (-not (Test-Path -LiteralPath $ScriptPath -PathType Leaf)) {
        throw "Documentation verifier was not found: '$ScriptPath'."
    }

    $command = Get-Command -Name $ScriptPath -CommandType ExternalScript
    $arguments = [System.Collections.Generic.List[string]]::new()
    $arguments.Add('-NoProfile')
    $arguments.Add('-File')
    $arguments.Add($ScriptPath)

    if ($command.Parameters.ContainsKey('RepositoryRoot')) {
        $arguments.Add('-RepositoryRoot')
        $arguments.Add($Root)
    }

    if ($command.Parameters.ContainsKey('OutputDirectory')) {
        $snippetOutput = Join-Path $EvidenceDirectory 'documentation-snippets'
        [System.IO.Directory]::CreateDirectory($snippetOutput) | Out-Null
        $arguments.Add('-OutputDirectory')
        $arguments.Add($snippetOutput)
    }

    $result = Invoke-ExternalProcess -FileName (Get-Process -Id $PID).Path -Arguments $arguments -WorkingDirectory $Root
    $logPath = Join-Path $EvidenceDirectory 'documentation-snippets.log'
    [System.IO.File]::WriteAllText($logPath, $result.StandardOutput + $result.StandardError, [System.Text.UTF8Encoding]::new($false))
    if ($result.ExitCode -ne 0) {
        throw "Documentation verifier failed with exit code $($result.ExitCode). See '$logPath'."
    }

    return [pscustomobject] [ordered]@{
        script = $ScriptPath
        exitCode = $result.ExitCode
        log = $logPath
    }
}

function Assert-CompatibilityEvidence {
    param(
        [Parameter(Mandatory)] $Evidence,
        [Parameter(Mandatory)] $PackageInventory,
        [Parameter(Mandatory)] [string] $RuntimeIdentifier
    )

    if ($Evidence.SchemaVersion -ne 1 -or $Evidence.Succeeded -ne $true) {
        throw 'Compatibility evidence must use schema version 1 and report Succeeded=true.'
    }

    $expectedScenarios = @('AspNetCoreNativeAot', 'AspNetCoreTrimmed', 'CoreNativeAot', 'CoreTrimmed')
    $scenarios = @($Evidence.Scenarios)
    $actualScenarios = @($scenarios | ForEach-Object Scenario | Sort-Object -Unique)
    if ($scenarios.Count -ne $expectedScenarios.Count -or
        $actualScenarios.Count -ne $expectedScenarios.Count -or
        (Compare-Object -ReferenceObject $expectedScenarios -DifferenceObject $actualScenarios)) {
        throw "Compatibility evidence must contain exactly these scenarios: $($expectedScenarios -join ', ')."
    }

    foreach ($scenario in $scenarios) {
        if ($scenario.Outcome -ne 'Passed') {
            throw "Compatibility scenario '$($scenario.Scenario)' did not pass."
        }
        if ($scenario.RuntimeIdentifier -ne $RuntimeIdentifier) {
            throw "Compatibility scenario '$($scenario.Scenario)' used RID '$($scenario.RuntimeIdentifier)' instead of '$RuntimeIdentifier'."
        }
        if ($scenario.CorePackageVersion -ne $PackageInventory.core.version -or
            $scenario.AspNetCorePackageVersion -ne $PackageInventory.aspNetCore.version) {
            throw "Compatibility scenario '$($scenario.Scenario)' used stale package versions."
        }
        if (-not $scenario.CorePackageSha256.Equals($PackageInventory.core.sha256, [StringComparison]::OrdinalIgnoreCase) -or
            -not $scenario.AspNetCorePackageSha256.Equals($PackageInventory.aspNetCore.sha256, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Compatibility scenario '$($scenario.Scenario)' used stale package hashes."
        }
    }

    return $scenarios
}

function Invoke-CompatibilityVerification {
    param(
        [string] $ScriptPath,
        [string] $EvidencePath,
        [Parameter(Mandatory)] [string] $Root,
        [Parameter(Mandatory)] [string] $EvidenceDirectory,
        [Parameter(Mandatory)] [string] $Packages,
        [Parameter(Mandatory)] [string] $RuntimeIdentifier,
        [Parameter(Mandatory)] [string] $PackageFeed,
        [Parameter(Mandatory)] $PackageInventory
    )

    if ($ScriptPath -and $EvidencePath) {
        throw 'Specify either CompatibilityScript or CompatibilityEvidencePath, not both.'
    }

    $compatibilityDirectory = Join-Path $EvidenceDirectory 'compatibility'
    [System.IO.Directory]::CreateDirectory($compatibilityDirectory) | Out-Null

    if ($ScriptPath) {
        if (-not (Test-Path -LiteralPath $ScriptPath -PathType Leaf)) {
            throw "Compatibility script was not found: '$ScriptPath'."
        }

        $command = Get-Command -Name $ScriptPath -CommandType ExternalScript
        $arguments = [System.Collections.Generic.List[string]]::new()
        $arguments.Add('-NoProfile')
        $arguments.Add('-File')
        $arguments.Add($ScriptPath)
        if ($command.Parameters.ContainsKey('RepositoryRoot')) {
            $arguments.Add('-RepositoryRoot')
            $arguments.Add($Root)
        }
        if ($command.Parameters.ContainsKey('OutputDirectory')) {
            $arguments.Add('-OutputDirectory')
            $arguments.Add($compatibilityDirectory)
        }
        if ($command.Parameters.ContainsKey('PackageDirectory')) {
            $arguments.Add('-PackageDirectory')
            $arguments.Add($Packages)
        }
        if ($command.Parameters.ContainsKey('RuntimeIdentifier')) {
            $arguments.Add('-RuntimeIdentifier')
            $arguments.Add($RuntimeIdentifier)
        }
        if ($command.Parameters.ContainsKey('PackageFeed')) {
            $arguments.Add('-PackageFeed')
            $arguments.Add($PackageFeed)
        }

        $result = Invoke-ExternalProcess -FileName (Get-Process -Id $PID).Path -Arguments $arguments -WorkingDirectory $Root
        $logPath = Join-Path $compatibilityDirectory 'compatibility.log'
        [System.IO.File]::WriteAllText($logPath, $result.StandardOutput + $result.StandardError, [System.Text.UTF8Encoding]::new($false))
        if ($result.ExitCode -ne 0) {
            throw "Compatibility script failed with exit code $($result.ExitCode). See '$logPath'."
        }

        $resultsPath = Join-Path $compatibilityDirectory 'compatibility-results.json'
        if (-not (Test-Path -LiteralPath $resultsPath -PathType Leaf)) {
            throw "Compatibility script did not produce '$resultsPath'."
        }

        $parsed = Get-Content -LiteralPath $resultsPath -Raw | ConvertFrom-Json
        $scenarios = Assert-CompatibilityEvidence -Evidence $parsed -PackageInventory $PackageInventory -RuntimeIdentifier $RuntimeIdentifier

        return [pscustomobject] [ordered]@{
            status = 'script-passed'
            script = $ScriptPath
            log = $logPath
            results = $resultsPath
            scenarios = $scenarios.Count
        }
    }

    if ($EvidencePath) {
        if (-not (Test-Path -LiteralPath $EvidencePath -PathType Leaf)) {
            throw "Compatibility evidence was not found: '$EvidencePath'."
        }

        $destination = Join-Path $compatibilityDirectory ([System.IO.Path]::GetFileName($EvidencePath))
        [System.IO.File]::Copy($EvidencePath, $destination, $true)
        $parsed = Get-Content -LiteralPath $destination -Raw | ConvertFrom-Json
        $scenarios = Assert-CompatibilityEvidence -Evidence $parsed -PackageInventory $PackageInventory -RuntimeIdentifier $RuntimeIdentifier

        return [pscustomobject] [ordered]@{
            status = 'evidence-copied'
            path = $destination
            sha256 = Get-Sha256 -Path $destination
            scenarios = $scenarios.Count
        }
    }

    throw 'Compatibility evidence is required. Supply CompatibilityScript or CompatibilityEvidencePath.'
}

$RepositoryRoot = Resolve-FullPath -Path $RepositoryRoot -BasePath (Get-Location).Path
if (-not (Test-Path -LiteralPath $RepositoryRoot -PathType Container)) {
    throw "RepositoryRoot was not found: '$RepositoryRoot'."
}

$artifactsDirectory = Join-Path $RepositoryRoot 'artifacts'
if (Test-Path -LiteralPath $artifactsDirectory) {
    $artifactsItem = Get-Item -LiteralPath $artifactsDirectory -Force
    if (($artifactsItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "ArtifactsDirectory '$($artifactsItem.FullName)' cannot be a reparse point."
    }
}
[System.IO.Directory]::CreateDirectory($artifactsDirectory) | Out-Null
$artifactsDirectory = (Resolve-Path -LiteralPath $artifactsDirectory).Path
$OutputDirectory = Resolve-FullPath -Path $OutputDirectory -BasePath $RepositoryRoot
Initialize-EvidenceDirectory -Path $OutputDirectory -ArtifactsDirectory $artifactsDirectory -AllowClean $Clean.IsPresent

if ([string]::IsNullOrWhiteSpace($PackageDirectory)) {
    $PackageDirectory = Join-Path $RepositoryRoot 'artifacts/packages'
}
else {
    $PackageDirectory = Resolve-FullPath -Path $PackageDirectory -BasePath $RepositoryRoot
}

if ([string]::IsNullOrWhiteSpace($DocumentationVerifier)) {
    $DocumentationVerifier = Join-Path $RepositoryRoot 'examples/FunnySharp.DocumentationSamples/VerifyDocumentationSnippets.ps1'
}
else {
    $DocumentationVerifier = Resolve-FullPath -Path $DocumentationVerifier -BasePath $RepositoryRoot
}

if ($CompatibilityEvidencePath) {
    $CompatibilityEvidencePath = Resolve-FullPath -Path $CompatibilityEvidencePath -BasePath $RepositoryRoot
}

if ($CompatibilityScript) {
    $CompatibilityScript = Resolve-FullPath -Path $CompatibilityScript -BasePath $RepositoryRoot
}

$ExecutionEvidenceDirectory = Resolve-FullPath -Path $ExecutionEvidenceDirectory -BasePath $RepositoryRoot

$checks = [System.Collections.Generic.List[object]]::new()
$failures = [System.Collections.Generic.List[string]]::new()
function Invoke-EvidenceCheck {
    param(
        [Parameter(Mandatory)] [string] $Name,
        [Parameter(Mandatory)] [scriptblock] $Action
    )

    try {
        $details = & $Action
        $checks.Add([pscustomobject] [ordered]@{ name = $Name; status = 'passed'; details = $details })
        return $details
    }
    catch {
        $message = $_.Exception.Message
        $checks.Add([pscustomobject] [ordered]@{ name = $Name; status = 'failed'; details = $message })
        $failures.Add($Name + ': ' + $message)
        return $null
    }
}

$environment = Invoke-EvidenceCheck -Name 'Environment capture' -Action {
    $value = Get-EnvironmentEvidence -Root $RepositoryRoot
    Write-JsonFile -Value $value -Path (Join-Path $OutputDirectory 'environment.json')
    return $value
}

$executionEvidence = Invoke-EvidenceCheck -Name 'Release execution evidence' -Action {
    Assert-ReleaseExecutionEvidence `
        -Directory $ExecutionEvidenceDirectory `
        -ArtifactsDirectory $artifactsDirectory `
        -Root $RepositoryRoot
}

$apiInventory = Invoke-EvidenceCheck -Name 'Public API inventory' -Action {
    $assemblyPaths = @(
        (Join-Path $RepositoryRoot 'src/FunnySharp/bin/Release/net10.0/FunnySharp.dll'),
        (Join-Path $RepositoryRoot 'src/FunnySharp.AspNetCore/bin/Release/net10.0/FunnySharp.AspNetCore.dll')
    )
    foreach ($assemblyPath in $assemblyPaths) {
        if (-not (Test-Path -LiteralPath $assemblyPath -PathType Leaf)) {
            throw "Release assembly was not found: '$assemblyPath'."
        }
    }

    $value = Get-PublicApiInventory -AssemblyPaths $assemblyPaths -Root $RepositoryRoot
    $notTrimmable = @($value | Where-Object { -not $_.isTrimmable })
    if ($notTrimmable.Count -gt 0) {
        throw "Shipping assemblies must declare IsTrimmable=True: $($notTrimmable.identity -join ', ')."
    }
    Write-JsonFile -Value $value -Path (Join-Path $OutputDirectory 'public-api.json')
    $lines = [System.Collections.Generic.List[string]]::new()
    foreach ($assembly in $value) {
        $lines.Add('ASSEMBLY ' + $assembly.identity)
        foreach ($type in $assembly.types) {
            $lines.Add($type.kind.ToUpperInvariant() + ' ' + $type.name)
            foreach ($memberKind in @('constructors', 'methods', 'properties', 'fields', 'events')) {
                foreach ($member in $type.$memberKind) {
                    $lines.Add('  ' + $memberKind.TrimEnd('s').ToUpperInvariant() + ' ' + $member)
                }
            }
        }
    }
    [System.IO.File]::WriteAllLines((Join-Path $OutputDirectory 'public-api.txt'), $lines, [System.Text.UTF8Encoding]::new($false))
    return [pscustomobject] [ordered]@{ assemblies = $value.Count; output = 'public-api.json' }
}

$xmlDocumentation = Invoke-EvidenceCheck -Name 'XML documentation' -Action {
    $paths = @(
        (Join-Path $RepositoryRoot 'src/FunnySharp/bin/Release/net10.0/FunnySharp.xml'),
        (Join-Path $RepositoryRoot 'src/FunnySharp.AspNetCore/bin/Release/net10.0/FunnySharp.AspNetCore.xml')
    )
    $value = Get-XmlDocumentationInventory -Paths $paths
    Write-JsonFile -Value $value -Path (Join-Path $OutputDirectory 'xml-documentation.json')
    return [pscustomobject] [ordered]@{
        files = $value.Count
        members = ($value | Measure-Object members -Sum).Sum
        output = 'xml-documentation.json'
    }
}

$packages = $null
$packageSet = Invoke-EvidenceCheck -Name 'Package archive set' -Action {
    if (-not (Test-Path -LiteralPath $PackageDirectory -PathType Container)) {
        throw "Package directory was not found: '$PackageDirectory'."
    }

    $allFiles = @(Get-ChildItem -LiteralPath $PackageDirectory -File | Sort-Object Name)
    $nupkgs = @($allFiles | Where-Object { $_.Name.EndsWith('.nupkg', [System.StringComparison]::OrdinalIgnoreCase) -and -not $_.Name.EndsWith('.snupkg', [System.StringComparison]::OrdinalIgnoreCase) })
    $snupkgs = @($allFiles | Where-Object { $_.Name.EndsWith('.snupkg', [System.StringComparison]::OrdinalIgnoreCase) })
    if ($nupkgs.Count -ne 2 -or $snupkgs.Count -ne 2) {
        throw "Expected exactly two .nupkg and two .snupkg archives; found $($nupkgs.Count) and $($snupkgs.Count)."
    }

    $core = Find-PackageArchive -Files $nupkgs -PackageId 'FunnySharp' -Extension 'nupkg'
    $aspNetCore = Find-PackageArchive -Files $nupkgs -PackageId 'FunnySharp.AspNetCore' -Extension 'nupkg'
    $coreSymbols = Find-PackageArchive -Files $snupkgs -PackageId 'FunnySharp' -Extension 'snupkg'
    $aspNetCoreSymbols = Find-PackageArchive -Files $snupkgs -PackageId 'FunnySharp.AspNetCore' -Extension 'snupkg'
    $expectedCoreSymbolsName = $core.Name -replace '\.nupkg$', '.snupkg'
    $expectedAspNetCoreSymbolsName = $aspNetCore.Name -replace '\.nupkg$', '.snupkg'
    if ($coreSymbols.Name -ne $expectedCoreSymbolsName -or $aspNetCoreSymbols.Name -ne $expectedAspNetCoreSymbolsName) {
        throw 'Each symbol package must match the version of its corresponding NuGet package.'
    }
    $script:packages = [pscustomobject] [ordered]@{
        core = Get-PackageInspection -PackagePath $core.FullName
        aspNetCore = Get-PackageInspection -PackagePath $aspNetCore.FullName
        symbolPackages = @(
            [pscustomobject] [ordered]@{ fileName = $coreSymbols.Name; path = $coreSymbols.FullName; sha256 = Get-Sha256 -Path $coreSymbols.FullName },
            [pscustomobject] [ordered]@{ fileName = $aspNetCoreSymbols.Name; path = $aspNetCoreSymbols.FullName; sha256 = Get-Sha256 -Path $aspNetCoreSymbols.FullName }
        )
    }

    Write-JsonFile -Value $script:packages -Path (Join-Path $OutputDirectory 'package-inventory.json')
    return [pscustomobject] [ordered]@{ nupkgCount = $nupkgs.Count; snupkgCount = $snupkgs.Count; output = 'package-inventory.json' }
}

if ($null -ne $packages) {
    Invoke-EvidenceCheck -Name 'FunnySharp package layout and dependencies' -Action {
        Assert-PackageLayout `
            -Package $packages.core `
            -ExpectedId 'FunnySharp' `
            -AssemblyPath (Join-Path $RepositoryRoot 'src/FunnySharp/bin/Release/net10.0/FunnySharp.dll') `
            -XmlDocumentationPath (Join-Path $RepositoryRoot 'src/FunnySharp/bin/Release/net10.0/FunnySharp.xml') `
            -ReadmePath (Join-Path $RepositoryRoot 'README.md')
        Assert-CorePackageDependencies -Package $packages.core
        return [pscustomobject] [ordered]@{ package = $packages.core.fileName; version = $packages.core.version }
    } | Out-Null

    Invoke-EvidenceCheck -Name 'FunnySharp.AspNetCore package layout and dependencies' -Action {
        Assert-PackageLayout `
            -Package $packages.aspNetCore `
            -ExpectedId 'FunnySharp.AspNetCore' `
            -AssemblyPath (Join-Path $RepositoryRoot 'src/FunnySharp.AspNetCore/bin/Release/net10.0/FunnySharp.AspNetCore.dll') `
            -XmlDocumentationPath (Join-Path $RepositoryRoot 'src/FunnySharp.AspNetCore/bin/Release/net10.0/FunnySharp.AspNetCore.xml') `
            -ReadmePath (Join-Path $RepositoryRoot 'README.md')
        Assert-AspNetCorePackageDependencies -Package $packages.aspNetCore -CoreVersion $packages.core.version
        return [pscustomobject] [ordered]@{ package = $packages.aspNetCore.fileName; version = $packages.aspNetCore.version }
    } | Out-Null
}
else {
    Invoke-EvidenceCheck -Name 'FunnySharp package layout and dependencies' -Action { throw 'Package archive inspection did not complete.' } | Out-Null
    Invoke-EvidenceCheck -Name 'FunnySharp.AspNetCore package layout and dependencies' -Action { throw 'Package archive inspection did not complete.' } | Out-Null
}

$documentation = Invoke-EvidenceCheck -Name 'Documentation snippets' -Action {
    Invoke-DocumentationVerification -ScriptPath $DocumentationVerifier -Root $RepositoryRoot -EvidenceDirectory $OutputDirectory
}

$compatibility = Invoke-EvidenceCheck -Name 'Compatibility evidence' -Action {
    Invoke-CompatibilityVerification `
        -ScriptPath $CompatibilityScript `
        -EvidencePath $CompatibilityEvidencePath `
        -Root $RepositoryRoot `
        -EvidenceDirectory $OutputDirectory `
        -Packages $PackageDirectory `
        -RuntimeIdentifier $CompatibilityRuntimeIdentifier `
        -PackageFeed $CompatibilityPackageFeed `
        -PackageInventory $packages
}

Invoke-EvidenceCheck -Name 'Source fingerprint unchanged during verification' -Action {
    if ($null -eq $executionEvidence) {
        throw 'Release execution evidence did not validate.'
    }

    $currentFingerprint = Get-SourceFingerprint -Root $RepositoryRoot
    if (-not (Test-EquivalentSourceFingerprint -Left $executionEvidence.sourceFingerprint -Right $currentFingerprint)) {
        throw 'Source files changed while release verification ran.'
    }

    return $currentFingerprint
} | Out-Null

$releaseEvidence = [pscustomobject] [ordered]@{
    schemaVersion = 2
    succeeded = ($failures.Count -eq 0)
    environment = $environment
    executionEvidence = $executionEvidence
    apiInventory = $apiInventory
    xmlDocumentation = $xmlDocumentation
    packageInventory = if ($null -eq $packages) { $null } else { 'package-inventory.json' }
    documentation = $documentation
    compatibility = $compatibility
    checks = $checks
    failures = $failures
}
Write-JsonFile -Value $releaseEvidence -Path (Join-Path $OutputDirectory 'release-evidence.json')

$summary = [System.Collections.Generic.List[string]]::new()
$summary.Add('# FunnySharp Release Evidence')
$summary.Add('')
$summary.Add('Status: ' + $(if ($failures.Count -eq 0) { 'PASSED' } else { 'FAILED' }))
$summary.Add('')
$summary.Add('## Checks')
foreach ($check in $checks) {
    $summary.Add('- [' + $(if ($check.status -eq 'passed') { 'x' } else { ' ' }) + '] ' + $check.name)
}
if ($failures.Count -gt 0) {
    $summary.Add('')
    $summary.Add('## Failures')
    foreach ($failure in $failures) {
        $summary.Add('- ' + $failure)
    }
}
[System.IO.File]::WriteAllLines((Join-Path $OutputDirectory 'release-evidence.md'), $summary, [System.Text.UTF8Encoding]::new($false))

if ($failures.Count -gt 0) {
    Write-Error "Release evidence verification failed. See '$OutputDirectory\\release-evidence.md'."
    exit 1
}

Write-Output "Release evidence verification passed. Output: $OutputDirectory"
