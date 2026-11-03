param([string]$Port='COM3')
$ErrorActionPreference='Stop'
if($Port -notmatch '^COM\d{1,3}$'){throw 'Invalid COM port'}
$project=Split-Path $PSScriptRoot
$output=Join-Path $project 'pc-app\installer\tool-cache\physical-expansion'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$status=Join-Path $output 'status.json'
$assembly=Join-Path $PSScriptRoot 'Expansion.Tests\bin\Release\net10.0-windows\Expansion.Tests.dll'
$dotnet=Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
$installed=Join-Path $env:ProgramFiles 'TempMonitor'
$wasRunning=$false
$result=@{State='starting';UpdatedUtc=[DateTime]::UtcNow.ToString('o')}
try{
    if(-not(Test-Path -LiteralPath $assembly)){throw 'Build Expansion.Tests in Release first'}
    $task=Get-ScheduledTask -TaskName TempMonitor
    $wasRunning=$task.State -eq 'Running'
    if($wasRunning){
        Stop-ScheduledTask -TaskName TempMonitor
        for($i=0;$i -lt 50 -and (Get-ScheduledTask -TaskName TempMonitor).State -eq 'Running';$i++){Start-Sleep -Milliseconds 200}
        if((Get-ScheduledTask -TaskName TempMonitor).State -eq 'Running'){throw 'Background task did not stop'}
    }
    $result.State='testing';$result|ConvertTo-Json|Set-Content -LiteralPath $status
    $arguments=@(('"'+$assembly+'"'),('"'+$output+'"'),'--physical',$Port,('"'+$installed+'"'))
    $child=Start-Process -FilePath $dotnet -ArgumentList $arguments -PassThru -WindowStyle Hidden -RedirectStandardOutput (Join-Path $output 'stdout.log') -RedirectStandardError (Join-Path $output 'stderr.log')
    if(-not $child.WaitForExit(240000)){Stop-Process -Id $child.Id -Force;throw 'Physical test timed out'}
    $child.Refresh()
    if($child.ExitCode -ne 0){throw ('Physical test failed: '+(Get-Content (Join-Path $output 'stderr.log') -Raw))}
    $result.State='passed'
}catch{$result.State='failed';$result.Error=$_.Exception.Message}
finally{
    if($wasRunning){
        try{Start-ScheduledTask -TaskName TempMonitor;$result.MeasurementRestarted=$true}
        catch{$result.MeasurementRestarted=$false;$result.RestartError=$_.Exception.Message}
    }
    $result.UpdatedUtc=[DateTime]::UtcNow.ToString('o');$result|ConvertTo-Json|Set-Content -LiteralPath $status
}
