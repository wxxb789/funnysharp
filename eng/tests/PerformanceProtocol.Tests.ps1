[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$verifier = Join-Path $repositoryRoot 'eng/Verify-Performance.ps1'
$documentationGenerator = Join-Path $repositoryRoot 'eng/Generate-PerformanceDocumentation.ps1'
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('funnysharp-performance-tests-' + [Guid]::NewGuid().ToString('N'))
$passed = 0

function Write-Json {
    param(
        [Parameter(Mandatory)] $Value,
        [Parameter(Mandatory)] [string] $Path
    )

    [System.IO.File]::WriteAllText(
        $Path,
        ($Value | ConvertTo-Json -Depth 12) + [Environment]::NewLine,
        [System.Text.UTF8Encoding]::new($false))
}

function Get-TextSha256 {
    param([Parameter(Mandatory)] [string] $Value)

    return [Convert]::ToHexString(
        [System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($Value))
    ).ToLowerInvariant()
}

function Get-PolicyFingerprint {
    param([Parameter(Mandatory)] [string] $Path)

    $document = [System.Text.Json.JsonDocument]::Parse([System.IO.File]::ReadAllText($Path))
    try {
        return Get-TextSha256 $document.RootElement.GetProperty('policy').GetRawText()
    }
    finally {
        $document.Dispose()
    }
}

function Get-FileSetFingerprint {
    param(
        [Parameter(Mandatory)] [string] $Root,
        [Parameter(Mandatory)] [string[]] $Files
    )

    $relativePaths = [string[]] @($Files)
    [Array]::Sort($relativePaths, [StringComparer]::Ordinal)
    $stream = [System.IO.MemoryStream]::new()
    try {
        foreach ($relativePath in $relativePaths) {
            $path = Join-Path $Root $relativePath
            $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
            $line = $relativePath.Replace('\', '/') + [char] 0 + $hash + [char] 10
            $bytes = [System.Text.Encoding]::UTF8.GetBytes($line)
            $stream.Write($bytes, 0, $bytes.Length)
        }

        return [Convert]::ToHexString(
            [System.Security.Cryptography.SHA256]::HashData($stream.ToArray())
        ).ToLowerInvariant()
    }
    finally {
        $stream.Dispose()
    }
}

function Get-EnvironmentKey {
    param([Parameter(Mandatory)] $Environment)

    return Get-TextSha256 -Value (@(
            [string] $Environment.os,
            [string] $Environment.architecture,
            [string] $Environment.sdkVersion,
            [string] $Environment.runtime,
            [string] $Environment.jit,
            ([string] $Environment.gcServer).ToLowerInvariant(),
            ([string] $Environment.gcConcurrent).ToLowerInvariant(),
            ([string] $Environment.gcAllocationQuantum)
        ) -join [char] 0)
}

function Invoke-Verifier {
    param([Parameter(Mandatory)] [string] $CaseRoot)

    $arguments = @{
        RepositoryRoot = $CaseRoot
        ManifestPath = Join-Path $CaseRoot 'baseline.json'
        ReceiptDirectory = Join-Path $CaseRoot 'receipts'
    }
    & $verifier @arguments
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

function New-Fixture {
    param([Parameter(Mandatory)] [string] $Name)

    $root = Join-Path $tempRoot $Name
    $receipts = Join-Path $root 'receipts'
    [System.IO.Directory]::CreateDirectory($receipts) | Out-Null
    [System.IO.File]::WriteAllText((Join-Path $root 'Z-input.txt'), 'input-z')
    [System.IO.File]::WriteAllText((Join-Path $root 'a-input.txt'), 'input-a')
    [System.IO.File]::WriteAllText((Join-Path $root 'Z-protocol.txt'), 'protocol-z')
    [System.IO.File]::WriteAllText((Join-Path $root 'a-protocol.txt'), 'protocol-a')
    $manifest = [pscustomobject] [ordered]@{
        schemaVersion = 1
        benchmarkInput = [pscustomobject] [ordered]@{ files = @('a-input.txt', 'Z-input.txt') }
        protocol = [pscustomobject] [ordered]@{ files = @('a-protocol.txt', 'Z-protocol.txt') }
        policy = [pscustomobject] [ordered]@{
            revision = 'fixture-v1'
            rows = @(
                [pscustomobject] [ordered]@{
                    id = 'Fixture|Category|Direct|'
                    benchmarkClass = 'Fixture'
                    category = 'Category'
                    method = 'Direct'
                    parameters = ''
                    baseline = $true
                    carrier = 'value'
                    completionPath = 'synchronous'
                    expectedResult = '42'
                    comparisonGroup = 'fixture'
                    included = $true
                    exclusionReason = $null
                    allocationBudgetBytes = 0
                },
                [pscustomobject] [ordered]@{
                    id = 'Fixture|Category|Funny|'
                    benchmarkClass = 'Fixture'
                    category = 'Category'
                    method = 'Funny'
                    parameters = ''
                    baseline = $false
                    carrier = 'value'
                    completionPath = 'synchronous'
                    expectedResult = '42'
                    comparisonGroup = 'fixture'
                    included = $true
                    exclusionReason = $null
                    allocationBudgetBytes = 16
                },
                [pscustomobject] [ordered]@{
                    id = 'excluded|fixture'
                    benchmarkClass = 'Fixture'
                    category = 'Unmeasured'
                    method = ''
                    parameters = ''
                    baseline = $false
                    carrier = 'not-measured'
                    completionPath = 'not-measured'
                    expectedResult = 'not-measured'
                    comparisonGroup = 'unmeasured fixture'
                    included = $false
                    exclusionReason = 'No release claim depends on this scenario.'
                    allocationBudgetBytes = $null
                }
            )
        }
        observation = $null
        documentation = @()
    }
    $manifestPath = Join-Path $root 'baseline.json'
    Write-Json $manifest $manifestPath
    $policyFingerprint = Get-PolicyFingerprint $manifestPath
    $inputFingerprint = Get-FileSetFingerprint $root @('a-input.txt', 'Z-input.txt')
    $protocolFingerprint = Get-FileSetFingerprint $root @('a-protocol.txt', 'Z-protocol.txt')
    $environment = [pscustomobject] [ordered]@{
        os = 'fixture-os'
        architecture = 'X64'
        sdkVersion = '10.0.400'
        runtime = '.NET 10.0.11'
        jit = 'RyuJIT'
        gcServer = $false
        gcConcurrent = $true
        gcAllocationQuantum = 8
    }
    $reportNames = @(
        'FunnySharp.Benchmarks.Fixture-report.csv',
        'FunnySharp.Benchmarks.Fixture-report-github.md',
        'FunnySharp.Benchmarks.Fixture-report.html'
    )
    foreach ($reportName in $reportNames) {
        [IO.File]::WriteAllText((Join-Path $receipts $reportName), $reportName)
    }
    $receipt = [pscustomobject] [ordered]@{
        schemaVersion = 1
        generatedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
        succeeded = $true
        candidateCommit = 'fixture'
        policyRevision = 'fixture-v1'
        policyFingerprint = $policyFingerprint
        benchmarkInputFingerprint = $inputFingerprint
        protocolFingerprint = $protocolFingerprint
        environmentKey = Get-EnvironmentKey $environment
        environment = $environment
        reports = @($reportNames | ForEach-Object {
                [pscustomobject] [ordered]@{
                    file = $_
                    sha256 = (Get-FileHash -LiteralPath (Join-Path $receipts $_) -Algorithm SHA256).Hash.ToLowerInvariant()
                }
            })
        rows = @(
            [pscustomobject] [ordered]@{
                id = 'Fixture|Category|Direct|'
                benchmarkClass = 'Fixture'
                category = 'Category'
                method = 'Direct'
                parameters = ''
                baseline = $true
                timingState = 'below-resolution'
                meanNanoseconds = $null
                allocatedBytesPerOperation = 0
            },
            [pscustomobject] [ordered]@{
                id = 'Fixture|Category|Funny|'
                benchmarkClass = 'Fixture'
                category = 'Category'
                method = 'Funny'
                parameters = ''
                baseline = $false
                timingState = 'observed'
                meanNanoseconds = 2.5
                allocatedBytesPerOperation = 16
            }
        )
    }
    $receiptPath = Join-Path $receipts 'Fixture-performance-receipt.json'
    Write-Json $receipt $receiptPath

    return [pscustomobject]@{
        Root = $root
        ManifestPath = $manifestPath
        ReceiptPath = $receiptPath
        Manifest = $manifest
        Receipt = $receipt
    }
}

try {
    $fixture = New-Fixture 'valid'
    Assert-Passes 'valid policy and receipt' { Invoke-Verifier $fixture.Root }

    $fixture = New-Fixture 'zero-regression'
    $fixture.Receipt.rows[0].allocatedBytesPerOperation = 1
    Write-Json $fixture.Receipt $fixture.ReceiptPath
    Assert-Fails 'zero allocation regression' 'above its 0 B budget|Zero-allocation' { Invoke-Verifier $fixture.Root }

    $fixture = New-Fixture 'ceiling-regression'
    $fixture.Receipt.rows[1].allocatedBytesPerOperation = 17
    Write-Json $fixture.Receipt $fixture.ReceiptPath
    Assert-Fails 'nonzero allocation ceiling' 'above its 16 B budget' { Invoke-Verifier $fixture.Root }

    $fixture = New-Fixture 'missing-row'
    $fixture.Receipt.rows = @($fixture.Receipt.rows[0])
    Write-Json $fixture.Receipt $fixture.ReceiptPath
    Assert-Fails 'missing required row' 'Required performance rows are missing' { Invoke-Verifier $fixture.Root }

    $fixture = New-Fixture 'fractional-allocation'
    $fixture.Receipt.rows[1].allocatedBytesPerOperation = 1.5
    Write-Json $fixture.Receipt $fixture.ReceiptPath
    Assert-Fails 'fractional allocation' 'non-integer allocation data' { Invoke-Verifier $fixture.Root }

    $fixture = New-Fixture 'unavailable-timing'
    $fixture.Receipt.rows[1].timingState = 'unavailable'
    $fixture.Receipt.rows[1].meanNanoseconds = $null
    Write-Json $fixture.Receipt $fixture.ReceiptPath
    Assert-Passes 'unavailable timing remains non-blocking' { Invoke-Verifier $fixture.Root }

    $fixture = New-Fixture 'environment-drift'
    $fixture.Receipt.environment.runtime = '.NET changed'
    Write-Json $fixture.Receipt $fixture.ReceiptPath
    Assert-Fails 'environment key drift' 'invalid environment key' { Invoke-Verifier $fixture.Root }

    $fixture = New-Fixture 'report-drift'
    [IO.File]::WriteAllText(
        (Join-Path $fixture.Root 'receipts/FunnySharp.Benchmarks.Fixture-report.csv'),
        'changed')
    Assert-Fails 'raw report hash drift' 'hash does not match' { Invoke-Verifier $fixture.Root }

    $fixture = New-Fixture 'input-drift'
    [System.IO.File]::WriteAllText((Join-Path $fixture.Root 'a-input.txt'), 'changed')
    Assert-Fails 'benchmark input drift' 'does not match the current benchmark input' { Invoke-Verifier $fixture.Root }

    $fixture = New-Fixture 'policy-mutation'
    $fixture.Manifest.policy.rows[1].allocationBudgetBytes = 32
    Write-Json $fixture.Manifest $fixture.ManifestPath
    Assert-Fails 'policy mutation after measurement' 'was not measured under the current policy' { Invoke-Verifier $fixture.Root }

    $fixture = New-Fixture 'documentation'
    $thirdPolicyRow = [pscustomobject] [ordered]@{
        id = 'Fixture|Category|Alternative|'
        benchmarkClass = 'Fixture'
        category = 'Category'
        method = 'Alternative'
        parameters = ''
        baseline = $false
        carrier = 'value'
        completionPath = 'synchronous'
        expectedResult = '42'
        comparisonGroup = 'fixture'
        included = $true
        exclusionReason = $null
        allocationBudgetBytes = 8
    }
    $fixture.Manifest.policy.rows = @($fixture.Manifest.policy.rows) + $thirdPolicyRow
    $guidePath = Join-Path $fixture.Root 'guide.md'
    [IO.File]::WriteAllText(
        $guidePath,
        (@(
                '# Guide',
                '',
                '<!-- performance-table:start fixture -->',
                'old',
                '<!-- performance-table:end fixture -->',
                ''
            ) -join [Environment]::NewLine))
    $fixture.Manifest.documentation = @(
        [pscustomobject] [ordered]@{
            id = 'fixture'
            path = 'guide.md'
            benchmarkClasses = @('Fixture')
        }
    )
    $fixture.Manifest.observation = $null
    Write-Json $fixture.Manifest $fixture.ManifestPath
    $policyFingerprint = Get-PolicyFingerprint $fixture.ManifestPath
    $fixture.Manifest.observation = [pscustomobject] [ordered]@{
        schemaVersion = 1
        policyRevision = 'fixture-v1'
        policyFingerprint = $policyFingerprint
        benchmarkInputFingerprint = $fixture.Receipt.benchmarkInputFingerprint
        protocolFingerprint = $fixture.Receipt.protocolFingerprint
        environmentKey = $fixture.Receipt.environmentKey
        rows = @(
            $fixture.Receipt.rows[0],
            $fixture.Receipt.rows[1],
            [pscustomobject] [ordered]@{
                id = 'Fixture|Category|Alternative|'
                benchmarkClass = 'Fixture'
                category = 'Category'
                method = 'Alternative'
                parameters = ''
                baseline = $false
                timingState = 'unavailable'
                meanNanoseconds = $null
                allocatedBytesPerOperation = 8
            }
        )
    }
    Write-Json $fixture.Manifest $fixture.ManifestPath
    $finalPolicyFingerprint = Get-PolicyFingerprint $fixture.ManifestPath
    if ($finalPolicyFingerprint -ne $policyFingerprint) {
        $fixture.Manifest.observation.policyFingerprint = $finalPolicyFingerprint
        Write-Json $fixture.Manifest $fixture.ManifestPath
    }

    Assert-Passes 'documentation generation and verify-only' {
        $originalCulture = [Threading.Thread]::CurrentThread.CurrentCulture
        try {
            [Threading.Thread]::CurrentThread.CurrentCulture = [Globalization.CultureInfo]::GetCultureInfo('fr-FR')
            & $documentationGenerator -RepositoryRoot $fixture.Root -ManifestPath $fixture.ManifestPath
            & $documentationGenerator -RepositoryRoot $fixture.Root -ManifestPath $fixture.ManifestPath -Verify
            $generated = [IO.File]::ReadAllText($guidePath)
            if (($generated -split '\r?\n' | Where-Object { $_ -match '^\| Category' }).Count -ne 2) {
                throw 'Expected two candidate rows for the shared baseline.'
            }
            if ($generated -notmatch '2\.500 ns') {
                throw 'Expected invariant numeric formatting in generated documentation.'
            }
        }
        finally {
            [Threading.Thread]::CurrentThread.CurrentCulture = $originalCulture
        }
    }
    [IO.File]::WriteAllText((Join-Path $fixture.Root 'a-input.txt'), 'changed')
    Assert-Fails 'documentation verify rejects benchmark input drift' 'policy, input, or protocol' {
        & $documentationGenerator -RepositoryRoot $fixture.Root -ManifestPath $fixture.ManifestPath -Verify
    }
    [IO.File]::WriteAllText((Join-Path $fixture.Root 'a-input.txt'), 'input-a')

    $allObservationRows = @($fixture.Manifest.observation.rows)
    $fixture.Manifest.observation.rows = @($allObservationRows | Select-Object -First 2)
    Write-Json $fixture.Manifest $fixture.ManifestPath
    Assert-Fails 'documentation verify rejects missing observation rows' 'row count' {
        & $documentationGenerator -RepositoryRoot $fixture.Root -ManifestPath $fixture.ManifestPath -Verify
    }
    $fixture.Manifest.observation.rows = $allObservationRows
    Write-Json $fixture.Manifest $fixture.ManifestPath

    [IO.File]::WriteAllText($guidePath, ([IO.File]::ReadAllText($guidePath)).Replace('2.500 ns', 'manual edit'))
    Assert-Fails 'documentation verify detects manual drift' 'stale' {
        & $documentationGenerator -RepositoryRoot $fixture.Root -ManifestPath $fixture.ManifestPath -Verify
    }
}
finally {
    $resolvedTemp = [System.IO.Path]::GetFullPath($tempRoot)
    $systemTemp = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    if ($resolvedTemp.StartsWith($systemTemp, [System.StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTemp)) {
        Remove-Item -LiteralPath $resolvedTemp -Recurse -Force
    }
}

Write-Output "Performance protocol tests passed: $passed."
