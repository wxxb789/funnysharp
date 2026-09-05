<#
.SYNOPSIS
Verifies FunnySharp benchmark receipts against the tracked allocation policy.

.DESCRIPTION
The tracked manifest owns policy. BenchmarkDotNet exporters own observations.
This script never changes allocation budgets or exclusions. With
-ObservationProposalPath it writes a reviewable observation-only proposal.
#>
[CmdletBinding()]
param(
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot),

    [string] $ManifestPath,

    [Parameter(Mandatory)]
    [string] $ReceiptDirectory,

    [string] $ObservationProposalPath
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

function Get-Sha256 {
    param([Parameter(Mandatory)] [string] $Path)

    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
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

function Get-FileSetFingerprint {
    param(
        [Parameter(Mandatory)] [string] $Root,
        [Parameter(Mandatory)] [object[]] $Files
    )

    $rootPath = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    $comparison = if ($IsWindows) {
        [System.StringComparison]::OrdinalIgnoreCase
    }
    else {
        [System.StringComparison]::Ordinal
    }
    $relativePaths = [string[]] @($Files | ForEach-Object { [string] $_ })
    [Array]::Sort($relativePaths, [StringComparer]::Ordinal)
    $stream = [System.IO.MemoryStream]::new()
    try {
        foreach ($relativePath in $relativePaths) {
            if ([System.IO.Path]::IsPathFullyQualified($relativePath) -or
                ($relativePath -split '[\\/]') -contains '..') {
                throw "Fingerprint path must be repository-relative: '$relativePath'."
            }

            $path = [System.IO.Path]::GetFullPath((Join-Path $rootPath $relativePath))
            $prefix = $rootPath + [System.IO.Path]::DirectorySeparatorChar
            if (-not $path.StartsWith($prefix, $comparison) -or -not (Test-Path -LiteralPath $path -PathType Leaf)) {
                throw "Fingerprint input was not found inside the repository: '$relativePath'."
            }

            $line = $relativePath.Replace('\', '/') + [char] 0 + (Get-Sha256 -Path $path) + [char] 10
            $bytes = [System.Text.Encoding]::UTF8.GetBytes($line)
            $stream.Write($bytes, 0, $bytes.Length)
        }

        return [Convert]::ToHexString(
            [System.Security.Cryptography.SHA256]::HashData($stream.ToArray())).ToLowerInvariant()
    }
    finally {
        $stream.Dispose()
    }
}

function Test-Integer {
    param($Value)

    return $Value -is [byte] -or $Value -is [sbyte] -or
        $Value -is [int16] -or $Value -is [uint16] -or
        $Value -is [int32] -or $Value -is [uint32] -or
        $Value -is [int64] -or $Value -is [uint64]
}

function Get-EnvironmentKey {
    param([Parameter(Mandatory)] $Environment)

    foreach ($property in @('os', 'architecture', 'sdkVersion', 'runtime', 'jit')) {
        if ([string]::IsNullOrWhiteSpace([string] $Environment.$property)) {
            throw "Performance environment is missing '$property'."
        }
    }
    if ($Environment.gcServer -isnot [bool] -or
        $Environment.gcConcurrent -isnot [bool] -or
        -not (Test-Integer $Environment.gcAllocationQuantum) -or
        $Environment.gcAllocationQuantum -lt 0) {
        throw 'Performance environment contains invalid GC data.'
    }

    $values = @(
        [string] $Environment.os,
        [string] $Environment.architecture,
        [string] $Environment.sdkVersion,
        [string] $Environment.runtime,
        [string] $Environment.jit,
        ([string] $Environment.gcServer).ToLowerInvariant(),
        ([string] $Environment.gcConcurrent).ToLowerInvariant(),
        ([string] $Environment.gcAllocationQuantum)
    )
    return Get-TextSha256 -Value ($values -join [char] 0)
}

$RepositoryRoot = Resolve-FullPath -Path $RepositoryRoot -BasePath (Get-Location).Path
$ManifestPath = if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
    Join-Path $RepositoryRoot 'eng/performance/baseline.json'
}
else {
    Resolve-FullPath -Path $ManifestPath -BasePath $RepositoryRoot
}
$ReceiptDirectory = Resolve-FullPath -Path $ReceiptDirectory -BasePath $RepositoryRoot

if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
    throw "Performance manifest was not found: '$ManifestPath'."
}
if (-not (Test-Path -LiteralPath $ReceiptDirectory -PathType Container)) {
    throw "Performance receipt directory was not found: '$ReceiptDirectory'."
}

$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
if ($manifest.schemaVersion -ne 1) {
    throw 'Performance manifest must use schemaVersion 1.'
}
if ([string]::IsNullOrWhiteSpace([string] $manifest.policy.revision)) {
    throw 'Performance policy revision is required.'
}

$policyFingerprint = Get-PolicyFingerprint -Path $ManifestPath
$inputFingerprint = Get-FileSetFingerprint -Root $RepositoryRoot -Files @($manifest.benchmarkInput.files)
$protocolFingerprint = Get-FileSetFingerprint -Root $RepositoryRoot -Files @($manifest.protocol.files)
$policyRows = @($manifest.policy.rows)
$includedRows = @($policyRows | Where-Object included)
$excludedRows = @($policyRows | Where-Object { -not $_.included })
$policyById = @{}

foreach ($row in $policyRows) {
    $id = [string] $row.id
    if ([string]::IsNullOrWhiteSpace($id) -or $policyById.ContainsKey($id)) {
        throw "Performance policy contains a missing or duplicate row id: '$id'."
    }
    $policyById[$id] = $row

    if ([string]::IsNullOrWhiteSpace([string] $row.comparisonGroup) -or
        [string]::IsNullOrWhiteSpace([string] $row.carrier) -or
        [string]::IsNullOrWhiteSpace([string] $row.completionPath) -or
        [string]::IsNullOrWhiteSpace([string] $row.expectedResult)) {
        throw "Performance policy row '$id' is missing comparison semantics."
    }

    if ($row.included) {
        if (-not (Test-Integer $row.allocationBudgetBytes) -or $row.allocationBudgetBytes -lt 0) {
            throw "Included performance row '$id' must have a non-negative integer allocation budget."
        }
        if (-not [string]::IsNullOrWhiteSpace([string] $row.exclusionReason)) {
            throw "Included performance row '$id' cannot have an exclusion reason."
        }
    }
    elseif ([string]::IsNullOrWhiteSpace([string] $row.exclusionReason)) {
        throw "Excluded performance row '$id' must explain the exclusion."
    }
}

if ($includedRows.Count -eq 0) {
    throw 'Performance policy must include at least one measured row.'
}

$receiptFiles = @(Get-ChildItem -LiteralPath $ReceiptDirectory -File -Filter '*-performance-receipt.json' | Sort-Object Name)
if ($receiptFiles.Count -eq 0) {
    throw "No performance receipt files were found in '$ReceiptDirectory'."
}

$observedById = @{}
$receiptSummaries = [System.Collections.Generic.List[object]]::new()
$candidateCommits = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$environmentKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($receiptFile in $receiptFiles) {
    $receipt = Get-Content -LiteralPath $receiptFile.FullName -Raw | ConvertFrom-Json
    if ($receipt.schemaVersion -ne 1 -or $receipt.succeeded -ne $true) {
        throw "Performance receipt '$($receiptFile.Name)' is unsuccessful or uses an unsupported schema."
    }
    if ($receipt.policyRevision -ne $manifest.policy.revision -or
        $receipt.policyFingerprint -ne $policyFingerprint) {
        throw "Performance receipt '$($receiptFile.Name)' was not measured under the current policy."
    }
    if ($receipt.benchmarkInputFingerprint -ne $inputFingerprint -or
        $receipt.protocolFingerprint -ne $protocolFingerprint) {
        throw "Performance receipt '$($receiptFile.Name)' does not match the current benchmark input or verifier protocol."
    }

    $environmentKey = Get-EnvironmentKey -Environment $receipt.environment
    if ($receipt.environmentKey -ne $environmentKey) {
        throw "Performance receipt '$($receiptFile.Name)' has an invalid environment key."
    }
    $environmentKeys.Add($environmentKey) | Out-Null

    $benchmarkClasses = @($receipt.rows | ForEach-Object { [string] $_.benchmarkClass } | Sort-Object -Unique)
    if ($benchmarkClasses.Count -ne 1) {
        throw "Performance receipt '$($receiptFile.Name)' must contain exactly one benchmark class."
    }
    $reportPrefix = "FunnySharp.Benchmarks.$($benchmarkClasses[0])-report"
    $expectedReports = @("$reportPrefix.csv", "$reportPrefix-github.md", "$reportPrefix.html")
    $reports = @($receipt.reports)
    $actualReports = @($reports | ForEach-Object { [string] $_.file } | Sort-Object)
    if ($reports.Count -ne $expectedReports.Count -or
        (Compare-Object -ReferenceObject @($expectedReports | Sort-Object) -DifferenceObject $actualReports)) {
        throw "Performance receipt '$($receiptFile.Name)' does not declare the required raw reports."
    }
    foreach ($report in $reports) {
        $fileName = [string] $report.file
        if ([IO.Path]::GetFileName($fileName) -cne $fileName) {
            throw "Performance receipt '$($receiptFile.Name)' contains an invalid report path '$fileName'."
        }
        $reportPath = Join-Path $ReceiptDirectory $fileName
        if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf) -or
            (Get-Sha256 -Path $reportPath) -ne [string] $report.sha256) {
            throw "Performance report '$fileName' is missing or its hash does not match the receipt."
        }
    }

    if (-not [string]::IsNullOrWhiteSpace([string] $receipt.candidateCommit)) {
        $candidateCommits.Add([string] $receipt.candidateCommit) | Out-Null
    }

    foreach ($row in @($receipt.rows)) {
        $id = [string] $row.id
        if ([string]::IsNullOrWhiteSpace($id) -or $observedById.ContainsKey($id)) {
            throw "Performance receipts contain a missing or duplicate row id: '$id'."
        }
        if (-not $policyById.ContainsKey($id) -or -not $policyById[$id].included) {
            throw "Performance receipt contains unregistered or excluded row '$id'."
        }

        $policyRow = $policyById[$id]
        foreach ($property in @('benchmarkClass', 'category', 'method', 'parameters', 'baseline')) {
            if ($row.$property -cne $policyRow.$property) {
                throw "Performance row '$id' does not match policy field '$property'."
            }
        }

        if (-not (Test-Integer $row.allocatedBytesPerOperation) -or $row.allocatedBytesPerOperation -lt 0) {
            throw "Performance row '$id' has missing, nonnumeric, rounded, or non-integer allocation data."
        }
        if ($row.allocatedBytesPerOperation -gt $policyRow.allocationBudgetBytes) {
            throw "Performance row '$id' allocated $($row.allocatedBytesPerOperation) B, above its $($policyRow.allocationBudgetBytes) B budget."
        }
        if ($policyRow.allocationBudgetBytes -eq 0 -and $row.allocatedBytesPerOperation -ne 0) {
            throw "Zero-allocation performance row '$id' regressed to $($row.allocatedBytesPerOperation) B."
        }

        if (@('observed', 'below-resolution', 'unavailable') -notcontains [string] $row.timingState) {
            throw "Performance row '$id' has invalid timing state '$($row.timingState)'."
        }
        if ($row.timingState -eq 'observed') {
            if ($null -eq $row.meanNanoseconds -or [double] $row.meanNanoseconds -le 0) {
                throw "Observed performance row '$id' must contain a positive meanNanoseconds value."
            }
        }
        elseif ($null -ne $row.meanNanoseconds) {
            throw "Performance row '$id' cannot contain a mean for timing state '$($row.timingState)'."
        }

        $observedById[$id] = $row
    }

    $receiptSummaries.Add([pscustomobject] [ordered]@{
            file = $receiptFile.Name
            sha256 = Get-Sha256 -Path $receiptFile.FullName
            environment = $receipt.environment
        })
}

$missingRows = @($includedRows | Where-Object { -not $observedById.ContainsKey([string] $_.id) })
if ($missingRows.Count -gt 0) {
    throw "Required performance rows are missing: $(@($missingRows.id) -join ', ')."
}
if ($observedById.Count -ne $includedRows.Count) {
    throw "Performance receipt row count $($observedById.Count) does not match policy count $($includedRows.Count)."
}
if ($candidateCommits.Count -gt 1) {
    throw "Performance receipts refer to multiple candidate commits: $($candidateCommits -join ', ')."
}
if ($environmentKeys.Count -ne 1) {
    throw "Performance receipts refer to $($environmentKeys.Count) environments instead of one."
}

if (-not [string]::IsNullOrWhiteSpace($ObservationProposalPath)) {
    $ObservationProposalPath = Resolve-FullPath -Path $ObservationProposalPath -BasePath $RepositoryRoot
    $parent = Split-Path -Parent $ObservationProposalPath
    [System.IO.Directory]::CreateDirectory($parent) | Out-Null
    $proposal = [pscustomobject] [ordered]@{
        schemaVersion = 1
        generatedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
        candidateCommit = if ($candidateCommits.Count -eq 1) { @($candidateCommits)[0] } else { $null }
        policyRevision = [string] $manifest.policy.revision
        policyFingerprint = $policyFingerprint
        benchmarkInputFingerprint = $inputFingerprint
        protocolFingerprint = $protocolFingerprint
        environmentKey = @($environmentKeys)[0]
        receipts = @($receiptSummaries)
        rows = @($observedById.Values | Sort-Object benchmarkClass, category, method, parameters)
    }
    $json = $proposal | ConvertTo-Json -Depth 12
    [System.IO.File]::WriteAllText($ObservationProposalPath, $json + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
}

Write-Output "Verified $($includedRows.Count) included performance rows and $($excludedRows.Count) explicit exclusions across $($receiptFiles.Count) receipts."
