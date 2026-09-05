[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)),

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$PackageDirectory,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$OutputDirectory,

    [ValidateNotNullOrEmpty()]
    [string]$RuntimeIdentifier = [System.Runtime.InteropServices.RuntimeInformation]::RuntimeIdentifier,

    [ValidateNotNullOrEmpty()]
    [string]$PackageFeed = "https://api.nuget.org/v3/index.json",

    [string[]]$Scenario = @(
        "CoreTrimmed",
        "CoreNativeAot",
        "AspNetCoreTrimmed",
        "AspNetCoreNativeAot"
    )
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $PSCommandPath
$definitions = @{
    CoreSmoke = @{
        Project = Join-Path $scriptRoot "FunnySharp.Compatibility.Core/FunnySharp.Compatibility.Core.csproj"
        AssemblyName = "FunnySharp.Compatibility.Core"
        PublishProperties = @()
    }
    CoreTrimmed = @{
        Project = Join-Path $scriptRoot "FunnySharp.Compatibility.Core/FunnySharp.Compatibility.Core.csproj"
        AssemblyName = "FunnySharp.Compatibility.Core"
        PublishProperties = @("-p:PublishTrimmed=true", "-p:TrimMode=full", "-p:RootShippingAssemblies=true")
    }
    AspNetCoreSmoke = @{
        Project = Join-Path $scriptRoot "FunnySharp.Compatibility.AspNetCore/FunnySharp.Compatibility.AspNetCore.csproj"
        AssemblyName = "FunnySharp.Compatibility.AspNetCore"
        PublishProperties = @()
    }
    CoreNativeAot = @{
        Project = Join-Path $scriptRoot "FunnySharp.Compatibility.Core/FunnySharp.Compatibility.Core.csproj"
        AssemblyName = "FunnySharp.Compatibility.Core"
        PublishProperties = @("-p:PublishTrimmed=true", "-p:TrimMode=full", "-p:PublishAot=true", "-p:IsAotCompatible=true", "-p:RootShippingAssemblies=false")
    }
    AspNetCoreTrimmed = @{
        Project = Join-Path $scriptRoot "FunnySharp.Compatibility.AspNetCore/FunnySharp.Compatibility.AspNetCore.csproj"
        AssemblyName = "FunnySharp.Compatibility.AspNetCore"
        PublishProperties = @("-p:PublishTrimmed=true", "-p:TrimMode=full", "-p:RootShippingAssemblies=true")
    }
    AspNetCoreNativeAot = @{
        Project = Join-Path $scriptRoot "FunnySharp.Compatibility.AspNetCore/FunnySharp.Compatibility.AspNetCore.csproj"
        AssemblyName = "FunnySharp.Compatibility.AspNetCore"
        PublishProperties = @("-p:PublishTrimmed=true", "-p:TrimMode=full", "-p:PublishAot=true", "-p:IsAotCompatible=true", "-p:RootShippingAssemblies=false")
    }
}
$Scenario = @($Scenario | ForEach-Object { $_ -split ',' } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
foreach ($name in $Scenario)
{
    if (-not $definitions.ContainsKey($name))
    {
        throw "Unknown compatibility scenario '$name'."
    }
}

function Get-PathComparison
{
    if ($IsWindows)
    {
        return [StringComparison]::OrdinalIgnoreCase
    }

    return [StringComparison]::Ordinal
}

function Test-PathAtOrWithin
{
    param(
        [Parameter(Mandatory)]
        [string]$ChildPath,

        [Parameter(Mandatory)]
        [string]$ParentPath
    )

    $comparison = Get-PathComparison
    $prefix = $ParentPath.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    return $ChildPath.Equals($ParentPath, $comparison) -or $ChildPath.StartsWith($prefix, $comparison)
}

function Assert-SafeArtifactsSubdirectory
{
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    $fullPath = [IO.Path]::GetFullPath($Path)
    if ($fullPath.Equals($artifactsDirectory, (Get-PathComparison)) -or
        -not (Test-PathAtOrWithin -ChildPath $fullPath -ParentPath $artifactsDirectory))
    {
        throw "Path must be a proper subdirectory of '$artifactsDirectory': '$fullPath'."
    }

    $relative = [IO.Path]::GetRelativePath($artifactsDirectory, $fullPath)
    $current = $artifactsDirectory
    foreach ($segment in $relative.Split(
            @([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar),
            [StringSplitOptions]::RemoveEmptyEntries))
    {
        $current = Join-Path $current $segment
        if (-not (Test-Path -LiteralPath $current))
        {
            continue
        }

        $item = Get-Item -LiteralPath $current -Force
        if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0)
        {
            throw "Path cannot contain a reparse point: '$($item.FullName)'."
        }

        $resolvedCurrent = (Resolve-Path -LiteralPath $current).Path
        if (-not (Test-PathAtOrWithin -ChildPath $resolvedCurrent -ParentPath $artifactsDirectory))
        {
            throw "Resolved path escapes '$artifactsDirectory': '$resolvedCurrent'."
        }
        $current = $resolvedCurrent
    }

    return $fullPath
}

$RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$PackageDirectory = [System.IO.Path]::GetFullPath($PackageDirectory)
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
$artifactsDirectory = [System.IO.Path]::GetFullPath((Join-Path $RepositoryRoot "artifacts"))
[IO.Directory]::CreateDirectory($artifactsDirectory) | Out-Null
$artifactsDirectory = (Resolve-Path -LiteralPath $artifactsDirectory).Path
$OutputDirectory = Assert-SafeArtifactsSubdirectory -Path $OutputDirectory

if (-not (Test-Path -LiteralPath $PackageDirectory -PathType Container))
{
    throw "Package directory does not exist: $PackageDirectory"
}

if (-not ($IsWindows -or $IsLinux -or $IsMacOS))
{
    throw "Unsupported host operating system."
}

$hostRuntimeIdentifier = [System.Runtime.InteropServices.RuntimeInformation]::RuntimeIdentifier
if (-not $RuntimeIdentifier.Equals($hostRuntimeIdentifier, [StringComparison]::OrdinalIgnoreCase))
{
    throw "RuntimeIdentifier '$RuntimeIdentifier' must match the runnable host RID '$hostRuntimeIdentifier'."
}

function Get-LocalPackageVersion
{
    param(
        [Parameter(Mandatory)]
        [string]$PackageId
    )

    $escapedPackageId = [regex]::Escape($PackageId)
    $packagePattern = "^$escapedPackageId\.(?<version>[0-9][0-9A-Za-z.+-]*)\.nupkg$"
    $packageFiles = @(
        Get-ChildItem -LiteralPath $PackageDirectory -File -Filter "*.nupkg" |
            Where-Object {
                $_.Name -match $packagePattern
            })

    if ($packageFiles.Count -ne 1)
    {
        throw "Expected exactly one $PackageId package in '$PackageDirectory', but found $($packageFiles.Count)."
    }

    return ([regex]::Match($packageFiles[0].Name, $packagePattern)).Groups["version"].Value
}

function Get-PackageAssemblySha256
{
    param(
        [Parameter(Mandatory)]
        [string]$PackagePath,

        [Parameter(Mandatory)]
        [string]$EntryPath
    )

    $archive = [IO.Compression.ZipFile]::OpenRead($PackagePath)
    try
    {
        $entry = $archive.GetEntry($EntryPath)
        if ($null -eq $entry)
        {
            throw "Package '$PackagePath' does not contain '$EntryPath'."
        }

        $stream = $entry.Open()
        try
        {
            return [Convert]::ToHexString(
                [Security.Cryptography.SHA256]::HashData($stream)).ToLowerInvariant()
        }
        finally
        {
            $stream.Dispose()
        }
    }
    finally
    {
        $archive.Dispose()
    }
}

function Invoke-DotNet
{
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    Write-Host "+ dotnet $($Arguments -join ' ')"
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0)
    {
        throw "dotnet exited with code $LASTEXITCODE."
    }
}

function Invoke-PublishedApplication
{
    param(
        [Parameter(Mandatory)]
        [string]$PublishDirectory,

        [Parameter(Mandatory)]
        [string]$AssemblyName
    )

    $suffix = if ($IsWindows) { ".exe" } else { "" }
    $application = Join-Path $PublishDirectory "$AssemblyName$suffix"
    if (-not (Test-Path -LiteralPath $application -PathType Leaf))
    {
        throw "Published application was not found: $application"
    }

    Write-Host "+ $application"
    & $application
    if ($LASTEXITCODE -ne 0)
    {
        throw "Published application exited with code $LASTEXITCODE."
    }
}

function Reset-Directory
{
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    if (Test-Path -LiteralPath $Path)
    {
        Assert-SafeArtifactsSubdirectory -Path $Path | Out-Null
        Remove-Item -LiteralPath $Path -Recurse -Force
    }

    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

function Get-IsolatedNuGetPackagesDirectory
{
    param(
        [Parameter(Mandatory)]
        [string]$ArtifactsDirectory,

        [Parameter(Mandatory)]
        [string]$OutputDirectory
    )

    $cacheKey = [Convert]::ToHexString(
        [Security.Cryptography.SHA256]::HashData(
            [Text.Encoding]::UTF8.GetBytes($OutputDirectory))).Substring(0, 16).ToLowerInvariant()
    return Join-Path $ArtifactsDirectory (Join-Path ".nuget-packages" $cacheKey)
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$outputRoot = (Resolve-Path -LiteralPath $OutputDirectory).Path
$coreVersion = Get-LocalPackageVersion -PackageId "FunnySharp"
$aspNetCoreVersion = Get-LocalPackageVersion -PackageId "FunnySharp.AspNetCore"
$corePackagePath = Join-Path $PackageDirectory "FunnySharp.$coreVersion.nupkg"
$aspNetCorePackagePath = Join-Path $PackageDirectory "FunnySharp.AspNetCore.$aspNetCoreVersion.nupkg"
$corePackageSha256 = (Get-FileHash -LiteralPath $corePackagePath -Algorithm SHA256).Hash
$aspNetCorePackageSha256 = (Get-FileHash -LiteralPath $aspNetCorePackagePath -Algorithm SHA256).Hash
$coreAssemblySha256 = Get-PackageAssemblySha256 -PackagePath $corePackagePath -EntryPath 'lib/net10.0/FunnySharp.dll'
$aspNetCoreAssemblySha256 = Get-PackageAssemblySha256 -PackagePath $aspNetCorePackagePath -EntryPath 'lib/net10.0/FunnySharp.AspNetCore.dll'
$nugetConfigPath = Join-Path $outputRoot "NuGet.Config"
$localSource = [System.Security.SecurityElement]::Escape($PackageDirectory)
$upstreamSource = [System.Security.SecurityElement]::Escape($PackageFeed)

@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local-release-packages" value="$localSource" />
    <add key="upstream" value="$upstreamSource" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="local-release-packages">
      <package pattern="FunnySharp" />
      <package pattern="FunnySharp.AspNetCore" />
    </packageSource>
    <packageSource key="upstream">
      <package pattern="Microsoft.*" />
      <package pattern="runtime.*" />
      <package pattern="System.*" />
      <package pattern="NETStandard.Library" />
      <package pattern="NuGet.*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
"@ | Set-Content -LiteralPath $nugetConfigPath -Encoding utf8NoBOM

$env:NUGET_PACKAGES = Get-IsolatedNuGetPackagesDirectory `
    -ArtifactsDirectory $artifactsDirectory `
    -OutputDirectory $outputRoot
Reset-Directory -Path $env:NUGET_PACKAGES

$results = [System.Collections.Generic.List[object]]::new()
foreach ($name in $Scenario)
{
    $definition = $definitions[$name]
    $scenarioRoot = Join-Path $outputRoot $name
    $publishDirectory = Join-Path $scenarioRoot "publish"
    $intermediateDirectory = Join-Path $scenarioRoot "obj"
    $binaryDirectory = Join-Path $scenarioRoot "bin"
    $startedAt = [DateTimeOffset]::UtcNow

    try
    {
        Reset-Directory -Path $scenarioRoot
        $commonProperties = @(
            "-p:FunnySharpPackageVersion=$coreVersion",
            "-p:FunnySharpAspNetCorePackageVersion=$aspNetCoreVersion",
            "-p:SelfContained=true",
            "-p:BaseIntermediateOutputPath=$intermediateDirectory$([IO.Path]::DirectorySeparatorChar)",
            "-p:BaseOutputPath=$binaryDirectory$([IO.Path]::DirectorySeparatorChar)"
        )

        Invoke-DotNet -Arguments (@(
            "restore",
            $definition.Project,
            "--configfile",
            $nugetConfigPath,
            "--no-cache",
            "--runtime",
            $RuntimeIdentifier
        ) + $commonProperties + $definition.PublishProperties)

        Invoke-DotNet -Arguments (@(
            "publish",
            $definition.Project,
            "--configuration",
            "Release",
            "--runtime",
            $RuntimeIdentifier,
            "--self-contained",
            "true",
            "--no-restore",
            "--output",
            $publishDirectory
        ) + $commonProperties + $definition.PublishProperties)

        Invoke-PublishedApplication -PublishDirectory $publishDirectory -AssemblyName $definition.AssemblyName
        $publishedCoreAssembly = Join-Path $publishDirectory 'FunnySharp.dll'
        $publishedAspNetCoreAssembly = Join-Path $publishDirectory 'FunnySharp.AspNetCore.dll'
        $publishedCoreAssemblySha256 = if (Test-Path -LiteralPath $publishedCoreAssembly -PathType Leaf)
        {
            (Get-FileHash -LiteralPath $publishedCoreAssembly -Algorithm SHA256).Hash.ToLowerInvariant()
        }
        else
        {
            $null
        }
        $publishedAspNetCoreAssemblySha256 = if (Test-Path -LiteralPath $publishedAspNetCoreAssembly -PathType Leaf)
        {
            (Get-FileHash -LiteralPath $publishedAspNetCoreAssembly -Algorithm SHA256).Hash.ToLowerInvariant()
        }
        else
        {
            $null
        }
        if ($name.EndsWith('Smoke', [StringComparison]::Ordinal) -and
            $null -ne $publishedCoreAssemblySha256 -and
            -not $publishedCoreAssemblySha256.Equals($coreAssemblySha256, [StringComparison]::OrdinalIgnoreCase))
        {
            throw "Published FunnySharp.dll does not match the canonical package assembly."
        }
        if ($name.EndsWith('Smoke', [StringComparison]::Ordinal) -and
            $null -ne $publishedAspNetCoreAssemblySha256 -and
            -not $publishedAspNetCoreAssemblySha256.Equals($aspNetCoreAssemblySha256, [StringComparison]::OrdinalIgnoreCase))
        {
            throw "Published FunnySharp.AspNetCore.dll does not match the canonical package assembly."
        }
        $results.Add([pscustomobject]@{
                Scenario = $name
                Outcome = "Passed"
                StartedAtUtc = $startedAt.ToString("O")
                FinishedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
                RuntimeIdentifier = $RuntimeIdentifier
                PackageFeed = $PackageFeed
                Project = $definition.Project
                PublishProperties = $definition.PublishProperties
                CorePackageVersion = $coreVersion
                AspNetCorePackageVersion = $aspNetCoreVersion
                CorePackageSha256 = $corePackageSha256
                AspNetCorePackageSha256 = $aspNetCorePackageSha256
                CoreAssemblySha256 = $coreAssemblySha256
                AspNetCoreAssemblySha256 = $aspNetCoreAssemblySha256
                PublishedCoreAssemblySha256 = $publishedCoreAssemblySha256
                PublishedAspNetCoreAssemblySha256 = $publishedAspNetCoreAssemblySha256
                Error = $null
            })
    }
    catch
    {
        $results.Add([pscustomobject]@{
                Scenario = $name
                Outcome = "Failed"
                StartedAtUtc = $startedAt.ToString("O")
                FinishedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
                RuntimeIdentifier = $RuntimeIdentifier
                PackageFeed = $PackageFeed
                Project = $definition.Project
                PublishProperties = $definition.PublishProperties
                CorePackageVersion = $coreVersion
                AspNetCorePackageVersion = $aspNetCoreVersion
                CorePackageSha256 = $corePackageSha256
                AspNetCorePackageSha256 = $aspNetCorePackageSha256
                CoreAssemblySha256 = $coreAssemblySha256
                AspNetCoreAssemblySha256 = $aspNetCoreAssemblySha256
                PublishedCoreAssemblySha256 = $null
                PublishedAspNetCoreAssemblySha256 = $null
                Error = $_.Exception.Message
            })
        [Console]::Error.WriteLine("$name failed: $($_.Exception.Message)")
    }
}

$resultsPath = Join-Path $outputRoot "compatibility-results.json"
$succeeded = -not ($results.Outcome -contains "Failed")
$evidence = [pscustomobject]@{
    SchemaVersion = 1
    Succeeded = $succeeded
    RuntimeIdentifier = $RuntimeIdentifier
    PackageFeed = $PackageFeed
    CorePackageVersion = $coreVersion
    AspNetCorePackageVersion = $aspNetCoreVersion
    CorePackageSha256 = $corePackageSha256
    AspNetCorePackageSha256 = $aspNetCorePackageSha256
    CoreAssemblySha256 = $coreAssemblySha256
    AspNetCoreAssemblySha256 = $aspNetCoreAssemblySha256
    Scenarios = $results
}
$evidence | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $resultsPath -Encoding utf8NoBOM
$results | Format-Table -AutoSize | Out-Host

if (-not $succeeded)
{
    exit 1
}
