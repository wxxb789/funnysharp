<#
.SYNOPSIS
Compares two externally prepared clean FunnySharp builds by evidence layer.

.DESCRIPTION
This command never creates, moves, or deletes repositories. Each root must
contain artifacts/reproducibility-input.json describing the pinned build
inputs and isolated NuGet cache used by the caller.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $LeftRoot,

    [Parameter(Mandatory)]
    [string] $RightRoot,

    [string] $OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-Sha256 {
    param([Parameter(Mandatory)] [string] $Path)

    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Invoke-GitText {
    param(
        [Parameter(Mandatory)] [string] $Root,
        [Parameter(Mandatory)] [string[]] $Arguments
    )

    $result = & git -C $Root @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed in '$Root': $result"
    }

    return ([string]::Join([Environment]::NewLine, @($result))).Trim()
}

function Get-AssemblyEvidence {
    param([Parameter(Mandatory)] [string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Assembly was not found: '$Path'."
    }

    $assembly = [Reflection.Assembly]::LoadFile([IO.Path]::GetFullPath($Path))
    return [pscustomobject] [ordered]@{
        path = $Path
        length = (Get-Item -LiteralPath $Path).Length
        sha256 = Get-Sha256 $Path
        mvid = $assembly.ManifestModule.ModuleVersionId.ToString()
        identity = $assembly.GetName().FullName
    }
}

function Get-FileEvidence {
    param([Parameter(Mandatory)] [string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Build output was not found: '$Path'."
    }

    return [pscustomobject] [ordered]@{
        path = $Path
        length = (Get-Item -LiteralPath $Path).Length
        sha256 = Get-Sha256 $Path
    }
}

function Get-ZipEvidence {
    param([Parameter(Mandatory)] [string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Package was not found: '$Path'."
    }

    $archive = [IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $entries = foreach ($entry in @($archive.Entries | Sort-Object FullName)) {
            $stream = $entry.Open()
            try {
                $contentHash = [Convert]::ToHexString(
                    [Security.Cryptography.SHA256]::HashData($stream)).ToLowerInvariant()
            }
            finally {
                $stream.Dispose()
            }

            [pscustomobject] [ordered]@{
                name = $entry.FullName
                length = $entry.Length
                compressedLength = $entry.CompressedLength
                lastWriteTimeUtc = $entry.LastWriteTime.UtcDateTime.ToString('O')
                sha256 = $contentHash
            }
        }

        return [pscustomobject] [ordered]@{
            path = $Path
            length = (Get-Item -LiteralPath $Path).Length
            sha256 = Get-Sha256 $Path
            entries = @($entries)
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Get-PreparedRoot {
    param([Parameter(Mandatory)] [string] $Root)

    $resolved = (Resolve-Path -LiteralPath $Root).Path
    if (-not (Test-Path -LiteralPath (Join-Path $resolved 'FunnySharp.slnx') -PathType Leaf)) {
        throw "Root is not a FunnySharp checkout: '$resolved'."
    }
    $status = Invoke-GitText $resolved @('status', '--porcelain=v1', '--untracked-files=all')
    if (-not [string]::IsNullOrWhiteSpace($status)) {
        throw "Reproducibility root must be clean: '$resolved'."
    }

    $inputPath = Join-Path $resolved 'artifacts/reproducibility-input.json'
    if (-not (Test-Path -LiteralPath $inputPath -PathType Leaf)) {
        throw "Reproducibility input evidence was not found: '$inputPath'."
    }
    $input = Get-Content -LiteralPath $inputPath -Raw | ConvertFrom-Json
    $commit = Invoke-GitText $resolved @('rev-parse', 'HEAD')
    if ($input.schemaVersion -ne 1 -or
        $input.candidateCommit -ne $commit -or
        $input.configuration -ne 'Release' -or
        $input.isolatedNuGetCache -ne $true) {
        throw "Reproducibility input evidence is incomplete or stale in '$resolved'."
    }
    $cachePath = [IO.Path]::GetFullPath((Join-Path $resolved ([string] $input.nugetPackagesDirectory)))
    $artifactsPath = [IO.Path]::GetFullPath((Join-Path $resolved 'artifacts')).TrimEnd('\', '/')
    if (-not $cachePath.StartsWith(
            $artifactsPath + [IO.Path]::DirectorySeparatorChar,
            [StringComparison]::OrdinalIgnoreCase)) {
        throw "NuGet cache is not isolated under the root's artifacts directory: '$cachePath'."
    }

    $packageDirectory = [IO.Path]::GetFullPath((Join-Path $resolved ([string] $input.packageDirectory)))
    return [pscustomobject] [ordered]@{
        root = $resolved
        commit = $commit
        tree = Invoke-GitText $resolved @('rev-parse', 'HEAD^{tree}')
        inputPath = $inputPath
        input = $input
        inputSha256 = Get-Sha256 $inputPath
        packageDirectory = $packageDirectory
        sdkPolicy = Get-FileEvidence (Join-Path $resolved 'global.json')
        buildProps = Get-FileEvidence (Join-Path $resolved 'Directory.Build.props')
        locks = @(Get-ChildItem -LiteralPath $resolved -Recurse -File -Filter 'packages.lock.json' |
            Where-Object FullName -NotMatch '[\\/](?:bin|obj|artifacts)[\\/]' |
            Sort-Object FullName |
            ForEach-Object {
                [pscustomobject] [ordered]@{
                    path = [IO.Path]::GetRelativePath($resolved, $_.FullName).Replace('\', '/')
                    sha256 = Get-Sha256 $_.FullName
                }
            })
    }
}

function Test-JsonEqual {
    param($Left, $Right)

    return ($Left | ConvertTo-Json -Depth 20 -Compress) -ceq
        ($Right | ConvertTo-Json -Depth 20 -Compress)
}

$left = Get-PreparedRoot $LeftRoot
$right = Get-PreparedRoot $RightRoot
if ($left.root.Equals($right.root, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'LeftRoot and RightRoot must be different directories.'
}
if ($left.commit -ne $right.commit -or $left.tree -ne $right.tree) {
    throw "Roots do not contain the same commit and source tree: '$($left.commit)' vs '$($right.commit)'."
}
if (-not (Test-JsonEqual $left.input $right.input) -or
    -not (Test-JsonEqual $left.locks $right.locks) -or
    $left.sdkPolicy.sha256 -ne $right.sdkPolicy.sha256 -or
    $left.buildProps.sha256 -ne $right.buildProps.sha256) {
    throw 'Controlled build inputs differ between roots.'
}

$assemblyRelativePaths = @(
    'src/FunnySharp/bin/Release/net10.0/FunnySharp.dll',
    'src/FunnySharp.AspNetCore/bin/Release/net10.0/FunnySharp.AspNetCore.dll'
)
$fileRelativePaths = @(
    'src/FunnySharp/bin/Release/net10.0/FunnySharp.pdb',
    'src/FunnySharp.AspNetCore/bin/Release/net10.0/FunnySharp.AspNetCore.pdb',
    'src/FunnySharp/bin/Release/net10.0/FunnySharp.xml',
    'src/FunnySharp.AspNetCore/bin/Release/net10.0/FunnySharp.AspNetCore.xml'
)
$packageNames = @(
    'FunnySharp.0.1.0.nupkg',
    'FunnySharp.0.1.0.snupkg',
    'FunnySharp.AspNetCore.0.1.0.nupkg',
    'FunnySharp.AspNetCore.0.1.0.snupkg'
)

$layers = [System.Collections.Generic.List[object]]::new()
$assemblyPairs = foreach ($relativePath in $assemblyRelativePaths) {
    $leftEvidence = Get-AssemblyEvidence (Join-Path $left.root $relativePath)
    $rightEvidence = Get-AssemblyEvidence (Join-Path $right.root $relativePath)
    [pscustomobject] [ordered]@{
        path = $relativePath
        sameBytes = $leftEvidence.sha256 -eq $rightEvidence.sha256
        sameMvid = $leftEvidence.mvid -eq $rightEvidence.mvid
        sameIdentity = $leftEvidence.identity -eq $rightEvidence.identity
        left = $leftEvidence
        right = $rightEvidence
    }
}
$layers.Add([pscustomobject] [ordered]@{
        name = 'assemblies'
        same = @($assemblyPairs | Where-Object { -not $_.sameBytes -or -not $_.sameMvid -or -not $_.sameIdentity }).Count -eq 0
        items = @($assemblyPairs)
    })

$filePairs = foreach ($relativePath in $fileRelativePaths) {
    $leftEvidence = Get-FileEvidence (Join-Path $left.root $relativePath)
    $rightEvidence = Get-FileEvidence (Join-Path $right.root $relativePath)
    [pscustomobject] [ordered]@{
        path = $relativePath
        sameBytes = $leftEvidence.sha256 -eq $rightEvidence.sha256
        left = $leftEvidence
        right = $rightEvidence
    }
}
$layers.Add([pscustomobject] [ordered]@{
        name = 'pdb-and-xml'
        same = @($filePairs | Where-Object { -not $_.sameBytes }).Count -eq 0
        items = @($filePairs)
    })

$packagePairs = foreach ($packageName in $packageNames) {
    $leftEvidence = Get-ZipEvidence (Join-Path $left.packageDirectory $packageName)
    $rightEvidence = Get-ZipEvidence (Join-Path $right.packageDirectory $packageName)
    [pscustomobject] [ordered]@{
        name = $packageName
        sameBytes = $leftEvidence.sha256 -eq $rightEvidence.sha256
        sameEntries = Test-JsonEqual $leftEvidence.entries $rightEvidence.entries
        left = $leftEvidence
        right = $rightEvidence
    }
}
$layers.Add([pscustomobject] [ordered]@{
        name = 'packages'
        same = @($packagePairs | Where-Object { -not $_.sameBytes -or -not $_.sameEntries }).Count -eq 0
        items = @($packagePairs)
    })

$firstDifference = @($layers | Where-Object { -not $_.same } | Select-Object -First 1)
$report = [pscustomobject] [ordered]@{
    schemaVersion = 1
    generatedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    candidateCommit = $left.commit
    leftRoot = $left.root
    rightRoot = $right.root
    controlledInputs = [pscustomobject] [ordered]@{
        same = $true
        sdkPolicySha256 = $left.sdkPolicy.sha256
        buildPropsSha256 = $left.buildProps.sha256
        lockFiles = $left.locks
        inputEvidenceSha256 = $left.inputSha256
    }
    firstDifference = if ($firstDifference.Count -eq 0) { $null } else { $firstDifference[0].name }
    byteIdentical = $firstDifference.Count -eq 0
    layers = @($layers)
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $left.root 'artifacts/reproducibility-comparison.json'
}
else {
    $OutputPath = [IO.Path]::GetFullPath($OutputPath)
}
[IO.Directory]::CreateDirectory((Split-Path -Parent $OutputPath)) | Out-Null
[IO.File]::WriteAllText(
    $OutputPath,
    ($report | ConvertTo-Json -Depth 30) + [Environment]::NewLine,
    [Text.UTF8Encoding]::new($false))

Write-Output "Reproducibility comparison written to '$OutputPath'. First difference: $($report.firstDifference ?? 'none')."
