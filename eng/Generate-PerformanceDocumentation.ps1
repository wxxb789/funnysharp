<#
.SYNOPSIS
Generates or verifies documentation performance tables from the tracked manifest.
#>
[CmdletBinding()]
param(
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot),

    [string] $ManifestPath,

    [switch] $Verify
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$newline = [char] 10

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

function Get-TextSha256 {
    param([Parameter(Mandatory)] [string] $Value)

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Value)
    return [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
}

function Get-PolicyFingerprint {
    param([Parameter(Mandatory)] [string] $Path)

    $document = [System.Text.Json.JsonDocument]::Parse([System.IO.File]::ReadAllText($Path))
    try {
        return Get-TextSha256 -Value $document.RootElement.GetProperty('policy').GetRawText()
    }
    finally {
        $document.Dispose()
    }
}

function Get-Sha256 {
    param([Parameter(Mandatory)] [string] $Path)

    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-FileSetFingerprint {
    param(
        [Parameter(Mandatory)] [string] $Root,
        [Parameter(Mandatory)] [object[]] $Files
    )

    $rootPath = [IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    $comparison = if ($IsWindows) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
    $relativePaths = [string[]] @($Files | ForEach-Object { [string] $_ })
    [Array]::Sort($relativePaths, [StringComparer]::Ordinal)
    $stream = [IO.MemoryStream]::new()
    try {
        foreach ($relativePath in $relativePaths) {
            if ([IO.Path]::IsPathFullyQualified($relativePath) -or ($relativePath -split '[\\/]') -contains '..') {
                throw "Fingerprint path must be repository-relative: '$relativePath'."
            }

            $path = [IO.Path]::GetFullPath((Join-Path $rootPath $relativePath))
            $prefix = $rootPath + [IO.Path]::DirectorySeparatorChar
            if (-not $path.StartsWith($prefix, $comparison) -or -not (Test-Path -LiteralPath $path -PathType Leaf)) {
                throw "Fingerprint input was not found inside the repository: '$relativePath'."
            }

            $line = $relativePath.Replace('\', '/') + [char] 0 + (Get-Sha256 -Path $path) + [char] 10
            $bytes = [Text.Encoding]::UTF8.GetBytes($line)
            $stream.Write($bytes, 0, $bytes.Length)
        }

        return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream.ToArray())).ToLowerInvariant()
    }
    finally {
        $stream.Dispose()
    }
}

function Format-Mean {
    param(
        [string] $TimingState,
        $MeanNanoseconds
    )

    if ($TimingState -ne 'observed' -or $null -eq $MeanNanoseconds) {
        return 'N/A'
    }

    $mean = [double] $MeanNanoseconds
    if ($mean -ge 1000) {
        return [string]::Format(
            [Globalization.CultureInfo]::InvariantCulture,
            '{0:N3} us',
            ($mean / 1000))
    }

    return [string]::Format([Globalization.CultureInfo]::InvariantCulture, '{0:N3} ns', $mean)
}

function Format-Allocation {
    param($Bytes)

    return '{0} B' -f ([long] $Bytes)
}

$RepositoryRoot = Resolve-FullPath -Path $RepositoryRoot -BasePath (Get-Location).Path
$ManifestPath = if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
    Join-Path $RepositoryRoot 'eng/performance/baseline.json'
}
else {
    Resolve-FullPath -Path $ManifestPath -BasePath $RepositoryRoot
}

if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
    throw "Performance manifest was not found: '$ManifestPath'."
}

$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
if ($manifest.schemaVersion -ne 1 -or $manifest.observation.schemaVersion -ne 1) {
    throw 'Performance manifest and observation must use schemaVersion 1.'
}

$policyFingerprint = Get-PolicyFingerprint -Path $ManifestPath
$inputFingerprint = Get-FileSetFingerprint -Root $RepositoryRoot -Files @($manifest.benchmarkInput.files)
$protocolFingerprint = Get-FileSetFingerprint -Root $RepositoryRoot -Files @($manifest.protocol.files)
if ($manifest.observation.policyRevision -ne $manifest.policy.revision -or
    $manifest.observation.policyFingerprint -ne $policyFingerprint -or
    $manifest.observation.benchmarkInputFingerprint -ne $inputFingerprint -or
    $manifest.observation.protocolFingerprint -ne $protocolFingerprint) {
    throw 'The approved observation does not match the current performance policy, input, or protocol.'
}

$includedPolicyRows = @($manifest.policy.rows | Where-Object included)
$policyById = @{}
foreach ($row in $includedPolicyRows) {
    $policyById[[string] $row.id] = $row
}
$observationById = @{}
foreach ($row in @($manifest.observation.rows)) {
    $id = [string] $row.id
    if ([string]::IsNullOrWhiteSpace($id) -or $observationById.ContainsKey($id) -or -not $policyById.ContainsKey($id)) {
        throw "The approved observation contains a missing, duplicate, or unregistered row '$id'."
    }
    $policyRow = $policyById[$id]
    foreach ($property in @('benchmarkClass', 'category', 'method', 'parameters', 'baseline')) {
        if ($row.$property -cne $policyRow.$property) {
            throw "The approved observation row '$id' does not match policy field '$property'."
        }
    }
    $observationById[$id] = $row
}
if ($observationById.Count -ne $includedPolicyRows.Count) {
    throw "The approved observation row count $($observationById.Count) does not match policy count $($includedPolicyRows.Count)."
}

foreach ($document in @($manifest.documentation)) {
    $path = Resolve-FullPath -Path ([string] $document.path) -BasePath $RepositoryRoot
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Performance guide was not found: '$path'."
    }

    $classes = @($document.benchmarkClasses | ForEach-Object { [string] $_ })
    $rows = @($manifest.policy.rows | Where-Object {
            $classes -contains [string] $_.benchmarkClass
        })
    $included = @($rows | Where-Object included)
    $excluded = @($rows | Where-Object { -not $_.included })
    $groups = @($included | Group-Object comparisonGroup | Sort-Object Name)
    $generated = [System.Collections.Generic.List[string]]::new()
    $generated.Add("| Scenario | Baseline mean | FunnySharp mean | Ratio | Baseline allocation | FunnySharp allocation |")
    $generated.Add("| --- | ---: | ---: | ---: | ---: | ---: |")

    foreach ($group in $groups) {
        $baselineRows = @($group.Group | Where-Object baseline)
        $candidateRows = @($group.Group | Where-Object { -not $_.baseline })
        if ($baselineRows.Count -ne 1 -or $candidateRows.Count -lt 1) {
            throw "Comparison group '$($group.Name)' must contain exactly one baseline and at least one candidate row."
        }

        $baselinePolicy = $baselineRows[0]
        if (-not $observationById.ContainsKey([string] $baselinePolicy.id)) {
            throw "Comparison group '$($group.Name)' is missing its baseline observation."
        }

        $baseline = $observationById[[string] $baselinePolicy.id]
        foreach ($candidatePolicy in @($candidateRows | Sort-Object method)) {
            if (-not $observationById.ContainsKey([string] $candidatePolicy.id)) {
                throw "Comparison group '$($group.Name)' is missing an approved observation."
            }

            $candidate = $observationById[[string] $candidatePolicy.id]
            $ratio = if ($baseline.timingState -eq 'observed' -and
                $candidate.timingState -eq 'observed' -and
                [double] $baseline.meanNanoseconds -gt 0) {
                [string]::Format(
                    [Globalization.CultureInfo]::InvariantCulture,
                    '{0:N2}x',
                    ([double] $candidate.meanNanoseconds / [double] $baseline.meanNanoseconds))
            }
            else {
                'N/A'
            }
            $scenario = [string] $baselinePolicy.category
            if (-not [string]::IsNullOrWhiteSpace([string] $baselinePolicy.parameters)) {
                $scenario += " ($($baselinePolicy.parameters))"
            }
            if ($candidateRows.Count -gt 1) {
                $scenario += " - $($candidatePolicy.method)"
            }

            $generated.Add(
                "| $scenario | $(Format-Mean $baseline.timingState $baseline.meanNanoseconds) | " +
                "$(Format-Mean $candidate.timingState $candidate.meanNanoseconds) | $ratio | " +
                "$(Format-Allocation $baseline.allocatedBytesPerOperation) | " +
                "$(Format-Allocation $candidate.allocatedBytesPerOperation) |")
        }
    }

    if ($excluded.Count -gt 0) {
        $generated.Add('')
        $generated.Add('Excluded measurements:')
        foreach ($row in @($excluded | Sort-Object comparisonGroup)) {
            $generated.Add("- $($row.comparisonGroup): $($row.exclusionReason)")
        }
    }

    $startMarker = "<!-- performance-table:start $($document.id) -->"
    $endMarker = "<!-- performance-table:end $($document.id) -->"
    $content = [System.IO.File]::ReadAllText($path)
    $start = $content.IndexOf($startMarker, [System.StringComparison]::Ordinal)
    $end = $content.IndexOf($endMarker, [System.StringComparison]::Ordinal)
    if ($start -lt 0 -or $end -lt 0 -or $end -lt $start) {
        throw "Guide '$($document.path)' is missing the '$($document.id)' generated-region markers."
    }

    $replacement = $startMarker + $newline +
        ($generated -join $newline) + $newline +
        $endMarker
    $existing = $content.Substring($start, $end + $endMarker.Length - $start)
    if ($Verify) {
        if ($existing -cne $replacement) {
            throw "Generated performance table is stale: '$($document.path)'."
        }
        continue
    }

    $updated = $content.Substring(0, $start) + $replacement +
        $content.Substring($end + $endMarker.Length)
    [System.IO.File]::WriteAllText($path, $updated, [System.Text.UTF8Encoding]::new($false))
}

$verb = if ($Verify) { 'Verified' } else { 'Generated' }
Write-Output "$verb $(@($manifest.documentation).Count) performance documentation regions."
