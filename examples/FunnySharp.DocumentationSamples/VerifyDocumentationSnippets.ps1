[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$guides = @(
    'aspnet-core.md',
    'concurrency.md',
    'effects.md',
    'function-composition.md',
    'immutable-updates.md',
    'state-machines.md',
    'validation.md'
)
$failures = [System.Collections.Generic.List[string]]::new()
$regionPattern = [regex]'^\s*//\s*<snippet\s+(?<name>DocumentationSamples\.[A-Za-z0-9.]+)>\s*$'
$regionEndPattern = [regex]'^\s*//\s*</snippet>\s*$'
$markerPattern = [regex]'^<!-- documentation-sample: (?<name>DocumentationSamples\.[A-Za-z0-9.]+) -->$'
$csharpFencePattern = [regex]'^```csharp\s*$'
$fenceEndPattern = [regex]'^```\s*$'
$regions = @{}

$sourceFiles = Get-ChildItem -Path $PSScriptRoot -Filter '*.cs' -File -Recurse |
    Where-Object { $_.FullName -notmatch '[\\/](?:bin|obj)[\\/]' }

foreach ($sourceFile in $sourceFiles) {
    $sourceLines = [System.IO.File]::ReadAllLines($sourceFile.FullName)
    for ($lineIndex = 0; $lineIndex -lt $sourceLines.Length; $lineIndex++) {
        $start = $regionPattern.Match($sourceLines[$lineIndex])
        if (-not $start.Success) {
            continue
        }

        $name = $start.Groups['name'].Value
        if ($regions.ContainsKey($name)) {
            $failures.Add("Duplicate source region '$name' in $($sourceFile.FullName):$($lineIndex + 1).")
            continue
        }

        $endIndex = $lineIndex + 1
        while ($endIndex -lt $sourceLines.Length -and -not $regionEndPattern.IsMatch($sourceLines[$endIndex])) {
            $endIndex++
        }

        if ($endIndex -eq $sourceLines.Length) {
            $failures.Add("Source region '$name' in $($sourceFile.FullName):$($lineIndex + 1) has no closing snippet marker.")
            continue
        }

        $content = [string[]]@($sourceLines[($lineIndex + 1)..($endIndex - 1)])
        $nonBlankLines = @($content | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        $commonIndent = if ($nonBlankLines.Count -eq 0)
        {
            0
        }
        else
        {
            ($nonBlankLines | ForEach-Object { ([regex]::Match($_, '^\s*')).Length } | Measure-Object -Minimum).Minimum
        }
        $content = [string[]]@($content | ForEach-Object {
                if ([string]::IsNullOrWhiteSpace($_)) { '' } else { $_.Substring($commonIndent) }
            })
        $regions.Add($name, [pscustomobject]@{
                File = $sourceFile.FullName
                Line = $lineIndex + 1
                Content = $content
            })
        $lineIndex = $endIndex
    }
}

$usedRegions = @{}
$snippetCount = 0
foreach ($guide in $guides) {
    $guidePath = Join-Path $RepositoryRoot "docs/$guide"
    if (-not (Test-Path -LiteralPath $guidePath)) {
        $failures.Add("Missing primary guide '$guidePath'.")
        continue
    }

    $markdownLines = [System.IO.File]::ReadAllLines($guidePath)
    for ($lineIndex = 0; $lineIndex -lt $markdownLines.Length; $lineIndex++) {
        if (-not $csharpFencePattern.IsMatch($markdownLines[$lineIndex])) {
            continue
        }

        $snippetCount++
        $marker = if ($lineIndex -gt 0) { $markerPattern.Match($markdownLines[$lineIndex - 1]) } else { $null }
        $name = if ($null -ne $marker -and $marker.Success) { $marker.Groups['name'].Value } else { $null }
        if ($null -eq $name) {
            $failures.Add("${guide}:$($lineIndex + 1) must immediately follow a documentation-sample marker.")
        }

        $endIndex = $lineIndex + 1
        while ($endIndex -lt $markdownLines.Length -and -not $fenceEndPattern.IsMatch($markdownLines[$endIndex])) {
            $endIndex++
        }

        if ($endIndex -eq $markdownLines.Length) {
            $failures.Add("${guide}:$($lineIndex + 1) has no closing code fence.")
            continue
        }

        if ($null -ne $name) {
            if ($usedRegions.ContainsKey($name)) {
                $failures.Add("${guide}:$($lineIndex + 1) reuses source region '$name'.")
            }
            else {
                $usedRegions.Add($name, $true)
            }

            if (-not $regions.ContainsKey($name)) {
                $failures.Add("${guide}:$($lineIndex + 1) references missing source region '$name'.")
            }
            else {
                $snippet = [string[]]@($markdownLines[($lineIndex + 1)..($endIndex - 1)])
                $source = $regions[$name]
                if ($snippet.Length -ne $source.Content.Length) {
                    $failures.Add("${guide}:$($lineIndex + 1) differs from '$name' in $($source.File):$($source.Line).")
                }
                else {
                    for ($contentIndex = 0; $contentIndex -lt $snippet.Length; $contentIndex++) {
                        if ($snippet[$contentIndex] -ne $source.Content[$contentIndex]) {
                            $failures.Add("${guide}:$($lineIndex + 1) differs from '$name' in $($source.File):$($source.Line) at snippet line $($contentIndex + 1).")
                            break
                        }
                    }
                }
            }
        }

        $lineIndex = $endIndex
    }
}

foreach ($name in $regions.Keys) {
    if (-not $usedRegions.ContainsKey($name)) {
        $source = $regions[$name]
        $failures.Add("Source region '$name' in $($source.File):$($source.Line) has no documentation fence.")
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Output "Verified $snippetCount C# documentation snippets across $($guides.Count) primary guides."
