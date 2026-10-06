param([Parameter(Mandatory=$true)][string]$RepositoryRoot,[Parameter(Mandatory=$true)][string]$ArtifactDirectory)
$ErrorActionPreference = 'Stop'
$old = Join-Path $RepositoryRoot 'docs/audits/goal-24-resolution/portable-evidence/verify.fsx'
$new = Join-Path $PSScriptRoot 'verify-portable-history.fsx'
& dotnet fsi --warnaserror+ $old -- --repository-root $RepositoryRoot --artifact-directory $ArtifactDirectory
$oldExit = $LASTEXITCODE
Write-Output ('OLD_PORTABLE_EXIT=' + $oldExit)
& dotnet fsi --warnaserror+ $new -- --repository-root $RepositoryRoot --artifact-directory $ArtifactDirectory
$newExit = $LASTEXITCODE
Write-Output ('NEW_PORTABLE_EXIT=' + $newExit)
& dotnet fsi --warnaserror+ $new -- --help
$helpExit = $LASTEXITCODE
Write-Output ('NEW_HELP_EXIT=' + $helpExit)
& dotnet fsi --warnaserror+ $new --
$usageExit = $LASTEXITCODE
Write-Output ('NEW_USAGE_EXIT=' + $usageExit)

$expected = @(0, 0, 0, 2)
$actual = @($oldExit, $newExit, $helpExit, $usageExit)
if (($actual -join ',') -ne ($expected -join ',')) {
    Write-Error ('Portable verifier exit tuple mismatch: expected {0}; actual {1}' -f ($expected -join ','), ($actual -join ','))
    exit 1
}
