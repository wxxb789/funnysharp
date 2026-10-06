param([Parameter(Mandatory=$true)][string]$RepositoryRoot,[Parameter(Mandatory=$true)][string]$BaselineRoot)
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$original = Join-Path $BaselineRoot 'docs/reviews/goal24-pr-20261004/traversal-r9-performance-evidence/verify-current.fsx'
$cOriginal = Join-Path $BaselineRoot 'docs/reviews/goal24-pr-20261004/current-performance-evidence/verify-current.fsx'
$records = @()
foreach ($run in @(
    @{name='old-r9-first-observed';script=$original;root=$BaselineRoot},
    @{name='new-r9-first-observed';script=(Join-Path $here 'verify-traversal-r9-performance.fsx');root=$RepositoryRoot},
    @{name='old-r9-warm';script=$original;root=$BaselineRoot},
    @{name='new-r9-warm';script=(Join-Path $here 'verify-traversal-r9-performance.fsx');root=$RepositoryRoot},
    @{name='old-c-stale';script=$cOriginal;root=$BaselineRoot},
    @{name='new-c-stale';script=(Join-Path $here 'verify-current-performance.fsx');root=$RepositoryRoot},
    @{name='decoded-copy-controls';script=(Join-Path $here 'offline-packet-controls.fsx');root=$RepositoryRoot}
)) {
    $info = [System.Diagnostics.ProcessStartInfo]::new('dotnet')
    $info.UseShellExecute = $false
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    foreach ($a in @('fsi','--warnaserror+',$run.script,'--',$run.root)) { $info.ArgumentList.Add($a) }
    $child = [System.Diagnostics.Process]::new()
    $child.StartInfo = $info
    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    if (-not $child.Start()) { throw 'Unable to start dotnet' }
    $stdout = $child.StandardOutput.ReadToEndAsync()
    $stderr = $child.StandardError.ReadToEndAsync()
    $child.WaitForExit()
    $watch.Stop()
    $record = [ordered]@{name=$run.name;executable='dotnet';arguments=@($info.ArgumentList);exitCode=$child.ExitCode;elapsedMs=$watch.Elapsed.TotalMilliseconds;launcherCpuMs=$child.TotalProcessorTime.TotalMilliseconds;launcherPeakWorkingSetBytes=$child.PeakWorkingSet64;stdout=$stdout.Result;stderr=$stderr.Result}
    $records += $record
    Write-Output ($record | ConvertTo-Json -Depth 8 -Compress)
    $child.Dispose()
}
$valid = $true
foreach ($r in $records) {
    if ($r.name -like '*r9*') { $valid = $valid -and $r.exitCode -eq 0 -and $r.stdout.Contains('CURRENT_PERFORMANCE_PORTABLE_PASS objects=522 locators=547 rows=230 launches=230 census=499/468/31 runtime=24 writes=0 network=0') }
    elseif ($r.name -like '*c-stale') { $valid = $valid -and $r.exitCode -ne 0 -and $r.stderr.Contains('Current source/protocol bytes changed') -and -not $r.stdout.Contains('CURRENT_PERFORMANCE_PORTABLE_PASS') }
    else { $valid = $valid -and $r.exitCode -eq 0 -and $r.stdout.Contains('OFFLINE_PACKET_CONTROLS_PASS') }
}
if (-not $valid) { Write-Output 'OFFLINE_PAIRED_COST_FAIL'; exit 1 }
Write-Output 'OFFLINE_PAIRED_COST_PASS'
