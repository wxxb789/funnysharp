Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

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
    $normalizedParent = $ParentPath.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    return $ChildPath.Equals($ParentPath, $comparison) -or
        $ChildPath.StartsWith($normalizedParent, $comparison)
}

function Assert-RelativePath {
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [string] $Description
    )

    if ([System.IO.Path]::IsPathFullyQualified($Path) -or ($Path -split '[\\/]') -contains '..') {
        throw "$Description must be repository-relative: '$Path'."
    }
}

function Read-ReleaseProtocol {
    param([Parameter(Mandatory)] [string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Release protocol was not found: '$Path'."
    }

    $protocol = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    if ($protocol.schemaVersion -ne 1) {
        throw 'Release protocol must use schemaVersion 1.'
    }

    foreach ($modeName in @('full', 'benchmarkSkipped')) {
        $steps = @($protocol.modes.$modeName.steps)
        if ($steps.Count -eq 0 -or @($steps | Sort-Object -Unique).Count -ne $steps.Count) {
            throw "Release protocol mode '$modeName' has no steps or contains duplicates."
        }
        foreach ($stepName in $steps) {
            if ($null -eq $protocol.steps.PSObject.Properties[[string] $stepName]) {
                throw "Release protocol mode '$modeName' refers to undefined step '$stepName'."
            }
        }
    }

    return $protocol
}

function Assert-ReleaseAttemptId {
    param([Parameter(Mandatory)] [string] $AttemptId)

    if ($AttemptId -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$') {
        throw "AttemptId '$AttemptId' must match ^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$."
    }

    return $AttemptId
}

function Assert-NewReleaseAttemptPath {
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [string] $ArtifactsDirectory,
        [Parameter(Mandatory)] [string] $Commit,
        [Parameter(Mandatory)] [string] $AttemptId
    )

    Assert-ReleaseAttemptId $AttemptId | Out-Null
    if ($Commit -notmatch '^[0-9a-fA-F]{40}$') {
        throw "Commit '$Commit' must be a full 40-character SHA."
    }

    $artifacts = [System.IO.Path]::GetFullPath($ArtifactsDirectory).TrimEnd('\', '/')
    $expected = [System.IO.Path]::GetFullPath(
        (Join-Path $artifacts (Join-Path 'release-candidate' (Join-Path $Commit $AttemptId))))
    $actual = [System.IO.Path]::GetFullPath($Path)
    if (-not $actual.Equals($expected, (Get-PathComparison))) {
        throw "OutputDirectory must equal '$expected' for this candidate and attempt; got '$actual'."
    }
    if (Test-Path -LiteralPath $actual) {
        throw "Release attempt path already exists and is immutable: '$actual'."
    }

    $current = $artifacts
    $relative = [System.IO.Path]::GetRelativePath($artifacts, $actual)
    foreach ($segment in $relative -split '[\\/]') {
        $current = Join-Path $current $segment
        if (-not (Test-Path -LiteralPath $current)) {
            continue
        }

        $item = Get-Item -LiteralPath $current -Force
        if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Release attempt path cannot contain a reparse point: '$($item.FullName)'."
        }
        $resolved = (Resolve-Path -LiteralPath $current).Path
        if (-not (Test-PathAtOrWithin -ChildPath $resolved -ParentPath $artifacts)) {
            throw "Release attempt path escapes '$artifacts': '$resolved'."
        }
        $current = $resolved
    }

    return $actual
}

function Get-ValidatedProjectOutputDirectories {
    param(
        [Parameter(Mandatory)] [string] $RepositoryRoot,
        [Parameter(Mandatory)] [string[]] $ProjectFiles
    )

    $root = [System.IO.Path]::GetFullPath($RepositoryRoot).TrimEnd('\', '/')
    $outputs = [System.Collections.Generic.List[string]]::new()
    foreach ($projectFile in @($ProjectFiles | Sort-Object -Unique)) {
        Assert-RelativePath -Path $projectFile -Description 'Project path'
        $projectPath = [System.IO.Path]::GetFullPath((Join-Path $root $projectFile))
        if (-not (Test-PathAtOrWithin -ChildPath $projectPath -ParentPath $root) -or
            -not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
            throw "Tracked project was not found inside the repository: '$projectFile'."
        }

        $projectDirectory = Split-Path -Parent $projectPath
        foreach ($name in @('bin', 'obj')) {
            $output = [System.IO.Path]::GetFullPath((Join-Path $projectDirectory $name))
            if (-not (Test-PathAtOrWithin -ChildPath $output -ParentPath $projectDirectory) -or
                $output.Equals($projectDirectory, (Get-PathComparison))) {
                throw "Generated output path is not a direct child of '$projectDirectory': '$output'."
            }
            if (Test-Path -LiteralPath $output) {
                $item = Get-Item -LiteralPath $output -Force
                if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                    throw "Generated output path cannot be a reparse point: '$($item.FullName)'."
                }
            }

            $outputs.Add($output)
        }
    }

    return @($outputs)
}

function Assert-PackageVersionAbsent {
    param(
        [Parameter(Mandatory)] [string] $PackageId,
        [Parameter(Mandatory)] [string] $Version,
        [Parameter(Mandatory)] [AllowNull()] [AllowEmptyCollection()] [object[]] $Versions
    )

    if ($null -eq $Versions) {
        throw "Package version state for '$PackageId' is ambiguous."
    }
    if (@($Versions | ForEach-Object { [string] $_ }) -contains $Version) {
        throw "Package '$PackageId' already contains version '$Version'."
    }
}

function Assert-BenchmarkReportRows {
    param(
        [Parameter(Mandatory)] [object[]] $ExpectedRows,
        [Parameter(Mandatory)] [object[]] $ActualRows,
        [Parameter(Mandatory)] [string] $Description
    )

    $getCounts = {
        param([object[]] $Rows)

        $counts = @{}
        foreach ($row in $Rows) {
            $benchmarkClass = [string] $row.benchmarkClass
            $category = [string] $row.category
            $method = [string] $row.method
            $parameters = [string] $row.parameters
            if ([string]::IsNullOrWhiteSpace($benchmarkClass) -or
                [string]::IsNullOrWhiteSpace($category) -or
                [string]::IsNullOrWhiteSpace($method)) {
                throw "$Description contains a row with missing benchmark identity."
            }

            $key = $benchmarkClass + [char] 0 + $category + [char] 0 + $method + [char] 0 + $parameters
            $counts[$key] = if ($counts.ContainsKey($key)) { $counts[$key] + 1 } else { 1 }
        }

        return $counts
    }

    $expectedCounts = & $getCounts $ExpectedRows
    $actualCounts = & $getCounts $ActualRows
    if ($expectedCounts.Count -ne $actualCounts.Count) {
        throw "$Description does not match the registered benchmark rows."
    }

    foreach ($key in $expectedCounts.Keys) {
        if (-not $actualCounts.ContainsKey($key) -or $actualCounts[$key] -ne $expectedCounts[$key]) {
            throw "$Description does not match the registered benchmark rows."
        }
    }
}

Export-ModuleMember -Function @(
    'Read-ReleaseProtocol',
    'Assert-ReleaseAttemptId',
    'Assert-NewReleaseAttemptPath',
    'Get-ValidatedProjectOutputDirectories',
    'Assert-PackageVersionAbsent',
    'Assert-BenchmarkReportRows'
)
