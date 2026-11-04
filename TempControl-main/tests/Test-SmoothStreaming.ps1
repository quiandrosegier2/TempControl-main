param([ValidateSet('smooth-baseline','smooth-candidate')][string]$Build='smooth-baseline',[string]$Port='COM3',[ValidateRange(20,300)][int]$Seconds=45)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$candidate=Join-Path $root ('pc-app/installer/tool-cache/'+$Build)
$output=Join-Path $root ('pc-app/installer/tool-cache/'+$Build+'-results')
New-Item -ItemType Directory -Path $output -Force | Out-Null
$status=Join-Path $output 'status.json';$result=@{State='starting'};$child=$null;$restart=$false
try{
    $cases=Get-Content (Join-Path $candidate 'benchmark-cases.json') -Raw | ConvertFrom-Json
    $restart=(Get-ScheduledTask -TaskName TempMonitor).State -eq 'Running'
    if($restart){Stop-ScheduledTask -TaskName TempMonitor;for($i=0;$i -lt 50 -and (Get-ScheduledTask -TaskName TempMonitor).State -eq 'Running';$i++){Start-Sleep -Milliseconds 200};if((Get-ScheduledTask -TaskName TempMonitor).State -eq 'Running'){throw 'Original monitor did not stop.'}}
    $reports=New-Object Collections.Generic.List[object]
    foreach($case in $cases){
        $started=[DateTime]::UtcNow
        @{ManualProfileId=$case.ProfileId}|ConvertTo-Json|Set-Content (Join-Path $candidate 'studio-settings.json')
        if(!$child){$child=Start-Process (Join-Path $candidate 'Qscreen.exe') -ArgumentList @('--monitor','--port',$Port) -WorkingDirectory $candidate -WindowStyle Hidden -PassThru}
        $result=@{State='testing';Case=$case.Name;StartedUtc=$started.ToString('o')};$result|ConvertTo-Json|Set-Content $status
        $last=$null;$snapshots=New-Object 'Collections.Generic.HashSet[string]';$hashes=New-Object 'Collections.Generic.HashSet[uint32]';$memory=New-Object Collections.Generic.List[object]
        $timer=[Diagnostics.Stopwatch]::StartNew()
        while($timer.Elapsed.TotalSeconds -lt $Seconds){
            Start-Sleep -Milliseconds 100
            if($child.HasExited){throw 'Candidate exited during benchmark.'}
            try{
                $devices=Get-Content (Join-Path $candidate 'devices-runtime.json') -Raw|ConvertFrom-Json
                foreach($device in $devices){if($device.Connected -and $device.ProfileId -eq $case.ProfileId -and $device.Performance -and ([DateTime]$device.Performance.UpdatedUtc).ToUniversalTime() -ge $started){$last=$device;[void]$hashes.Add([uint32]$device.Performance.FrameHash);if($device.Performance.Device){$memory.Add($device.Performance.Device)}}}
                $live=Get-Content (Join-Path $candidate 'live-telemetry.json') -Raw|ConvertFrom-Json
                if($live -and ([DateTime]$live.UpdatedUtc).ToUniversalTime() -ge $started){[void]$snapshots.Add([string]$live.UpdatedUtc)}
            }catch{$result.LastReadError=$_.Exception.Message}
        }
        if(!$last -or $snapshots.Count -lt 5 -or ($case.Name -ne 'static-layout' -and ($last.Performance.AcknowledgedFrames -lt 3 -or $hashes.Count -lt 2))){throw ('Insufficient fresh ACKs/animation/sensors: '+$case.Name)}
        $entry=@{Case=$case.Name;Firmware=$last.Firmware;Seconds=$Seconds;SensorSnapshots=$snapshots.Count;ObservedCompositeHashes=$hashes.Count;Performance=$last.Performance;FirstMemory=$(if($memory.Count){$memory[0]}else{$null});LastMemory=$(if($memory.Count){$memory[$memory.Count-1]}else{$null});VisualInspection=$false}
        $reports.Add($entry);$reports.ToArray()|ConvertTo-Json -Depth 10|Set-Content (Join-Path $output 'benchmarks.json')
    }
    $result=@{State='passed';Cases=$reports.Count}
}catch{$result.State='failed';$result.Error=$_.Exception.Message}
finally{
    if($child){try{if(!$child.HasExited){Stop-Process -Id $child.Id -Force}}catch{}}
    Get-Process PresentMon -ErrorAction SilentlyContinue|Where-Object {$_.Path -eq (Join-Path $candidate 'PresentMon.exe')}|Stop-Process -Force -ErrorAction SilentlyContinue
    if($restart){try{Start-ScheduledTask -TaskName TempMonitor;$result.OriginalMonitorRestarted=$true}catch{$result.OriginalMonitorRestarted=$false}}
    $result.UpdatedUtc=[DateTime]::UtcNow.ToString('o');$result|ConvertTo-Json|Set-Content $status
}
