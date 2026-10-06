param([Parameter(Mandatory=$true)][string]$RepositoryRoot,[Parameter(Mandatory=$true)][string]$ArtifactDirectory)
$ErrorActionPreference = 'Stop'
$old = Join-Path $RepositoryRoot 'docs/audits/goal-24-resolution/portable-evidence/verify.fsx'
$new = Join-Path $PSScriptRoot 'verify-portable-history.fsx'
& dotnet fsi --warnaserror+ $old -- --repository-root $RepositoryRoot --artifact-directory $ArtifactDirectory
Write-Output ('OLD_PORTABLE_EXIT=' + $LASTEXITCODE)
& dotnet fsi --warnaserror+ $new -- --repository-root $RepositoryRoot --artifact-directory $ArtifactDirectory
Write-Output ('NEW_PORTABLE_EXIT=' + $LASTEXITCODE)
& dotnet fsi --warnaserror+ $new -- --help
Write-Output ('NEW_HELP_EXIT=' + $LASTEXITCODE)
& dotnet fsi --warnaserror+ $new --
Write-Output ('NEW_USAGE_EXIT=' + $LASTEXITCODE)

