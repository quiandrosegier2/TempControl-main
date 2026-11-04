param([string]$Port='COM3',[ValidateRange(30,600)][int]$Seconds=60)
$ErrorActionPreference='Stop'
if($Port -notmatch '^COM\d{1,3}$'){throw 'Invalid COM port'}
$project=Split-Path $PSScriptRoot
$candidate=[IO.Path]::GetFullPath((Join-Path $project 'pc-app\installer\tool-cache\gif-streaming-candidate'))
$output=Join-Path $project 'pc-app\installer\tool-cache\gif-hardware-test'
New-Item -ItemType Directory -Force -Path $output|Out-Null
$status=Join-Path $output 'status.json';$result=@{State='starting';UpdatedUtc=[DateTime]::UtcNow.ToString('o')};$wasRunning=$false;$child=$null
try{
    if(-not(Test-Path (Join-Path $candidate 'Qscreen.exe'))){throw 'GIF candidate build missing'}
    if(-not(Test-Path (Join-Path $candidate 'studio-settings.json'))){throw 'Prepared isolated GIF profile missing'}
    $wasRunning=(Get-ScheduledTask -TaskName TempMonitor).State -eq 'Running'
    if($wasRunning){Stop-ScheduledTask -TaskName TempMonitor;for($i=0;$i -lt 50 -and (Get-ScheduledTask -TaskName TempMonitor).State -eq 'Running';$i++){Start-Sleep -Milliseconds 200};if((Get-ScheduledTask -TaskName TempMonitor).State -eq 'Running'){throw 'Measurement task did not stop'}}
    $testStarted=[DateTime]::UtcNow;$result.State='testing';$result|ConvertTo-Json|Set-Content -LiteralPath $status
    $child=Start-Process -FilePath (Join-Path $candidate 'Qscreen.exe') -ArgumentList @('--monitor','--port',$Port) -WorkingDirectory $candidate -WindowStyle Hidden -PassThru
    $samples=New-Object Collections.Generic.List[object];$telemetryTimes=New-Object 'Collections.Generic.HashSet[string]';$indices=New-Object 'Collections.Generic.HashSet[int]';$hashes=New-Object 'Collections.Generic.HashSet[uint32]';$timer=[Diagnostics.Stopwatch]::StartNew()
    while($timer.Elapsed.TotalSeconds -lt $Seconds){
        Start-Sleep -Milliseconds 200
        if($child.HasExited){throw 'Candidate monitor exited early'}
        try{
            $devices=Get-Content (Join-Path $candidate 'devices-runtime.json') -Raw|ConvertFrom-Json
            foreach($device in $devices){if($device.Connected -and $device.Performance -and ([DateTime]$device.Performance.UpdatedUtc).ToUniversalTime() -ge $testStarted){$samples.Add($device);if($null -ne $device.Performance.GifFrameIndex){[void]$indices.Add([int]$device.Performance.GifFrameIndex)};foreach($layerIndex in $device.Performance.LayerFrameIndices){[void]$indices.Add([int]$layerIndex)};if($null -ne $device.Performance.FrameHash){[void]$hashes.Add([uint32]$device.Performance.FrameHash)}}}
            $telemetry=Get-Content (Join-Path $candidate 'live-telemetry.json') -Raw|ConvertFrom-Json
            if($telemetry -and ([DateTime]$telemetry.UpdatedUtc).ToUniversalTime() -ge $testStarted){[void]$telemetryTimes.Add([string]$telemetry.UpdatedUtc)}
        }catch{$result.LastReadError=$_.Exception.Message}
    }
    if($samples.Count -eq 0){throw 'No acknowledged frame metrics from device'}
    $last=$samples[$samples.Count-1];if($last.Performance.AcknowledgedFrames -lt 5 -or $indices.Count -lt 2 -or $hashes.Count -lt 2 -or $telemetryTimes.Count -lt 5){throw 'GIF movement/ACK/live-measurement benchmark insufficient'}
    $report=@{State='passed';Port=$Port;Firmware=$last.Firmware;Model='Configured WS146';AcknowledgedFrames=$last.Performance.AcknowledgedFrames;ObservedGifFrameIndices=@($indices);DistinctFrameHashes=$hashes.Count;SensorUpdates=$telemetryTimes.Count;Performance=$last.Performance;DeviceFreeMemory=$last.Performance.Device.HeapFree;DeviceRenderFps=$last.Performance.Device.CompletedDrawsPerSecond;VisualInspection=$false;FirmwareFlashed=$false;UpdatedUtc=[DateTime]::UtcNow.ToString('o')}
    $report|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $output 'benchmark.json');$result.State='passed';$result.Remove('LastReadError')
}catch{$result.State='failed';$result.Error=$_.Exception.Message}
finally{
    if($child){try{if(-not $child.HasExited){Stop-Process -Id $child.Id -Force}}catch{}}
    Get-Process -Name PresentMon -ErrorAction SilentlyContinue|Where-Object {$_.Path -eq (Join-Path $candidate 'PresentMon.exe')}|Stop-Process -Force -ErrorAction SilentlyContinue
    if($wasRunning){try{Start-ScheduledTask -TaskName TempMonitor;$result.OriginalMeasurementRestarted=$true}catch{$result.OriginalMeasurementRestarted=$false;$result.RestartError=$_.Exception.Message}}
    $result.UpdatedUtc=[DateTime]::UtcNow.ToString('o');$result|ConvertTo-Json|Set-Content -LiteralPath $status
}
