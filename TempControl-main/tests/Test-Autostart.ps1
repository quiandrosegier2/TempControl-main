# Test taakregistratie zonder de Windows-taakplanner of processen te veranderen.
param([switch]$FailureChild)
$ErrorActionPreference = 'Stop'
$script:registered = $null
$script:started = $false
$script:removed = $false
function Stop-ScheduledTask { }
function Get-Process { }
function Stop-Process { }
function Unregister-ScheduledTask { $script:removed = $true }
function New-ScheduledTaskAction {
    param($Execute, $WorkingDirectory)
    [pscustomobject]@{ Execute=$Execute; WorkingDirectory=$WorkingDirectory }
}
function New-ScheduledTaskTrigger { param([switch]$AtLogOn) [pscustomobject]@{ AtLogOn=$AtLogOn } }
function New-ScheduledTaskPrincipal { param($GroupId,$RunLevel) [pscustomobject]@{ GroupId=$GroupId; RunLevel=$RunLevel } }
function New-ScheduledTaskSettingsSet {
    param([switch]$AllowStartIfOnBatteries,[switch]$DontStopIfGoingOnBatteries,$ExecutionTimeLimit,$RestartCount,$RestartInterval,$MultipleInstances)
    [pscustomobject]@{ MultipleInstances=$MultipleInstances }
}
function Register-ScheduledTask {
    param($TaskName,$Action,$Trigger,$Principal,$Settings,$Description,[switch]$Force)
    if ($FailureChild) { throw 'SIMULATED_REGISTER_FAILURE' }
    if ($TaskName -ne 'TempMonitor' -or $Action.Execute -ne 'C:\Program Files\TempMonitor\TempMonitor.exe') {
        throw 'De taak kreeg geen geldige executable-actie'
    }
    $script:registered = $Action
}
function Start-ScheduledTask { $script:started = $true }
$taskScript = Join-Path $PSScriptRoot '..\pc-app\installer\task.ps1'
$testData = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\pc-app\installer\tool-cache\autostart-tests'))
if ($FailureChild) { $env:ProgramData = $testData }
. $taskScript -Action Install -ExePath 'C:\Program Files\TempMonitor\TempMonitor.exe'
if (-not $script:registered -or -not $script:started -or $Action -ne 'Install') { throw 'Registreren/starten mislukt' }
'PASS: installatie registreert de executable en start de taak zonder de Action-parameter te overschrijven.'
$script:started = $false
. $taskScript -Action Uninstall
if (-not $script:removed -or $script:started) { throw 'Verwijderen mislukt' }
'PASS: verwijderen registreert of start geen nieuwe taak.'
New-Item -ItemType Directory -Path $testData -Force | Out-Null
$errorLog = Join-Path $testData 'TempMonitor\task-error.log'
if (Test-Path $errorLog) { Remove-Item -LiteralPath $errorLog }
$process = Start-Process "$PSHOME\powershell.exe" -ArgumentList ('-NoProfile -ExecutionPolicy Bypass -File "' + $PSCommandPath + '" -FailureChild') `
    -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput (Join-Path $testData 'stdout.log') -RedirectStandardError (Join-Path $testData 'stderr.log')
if ($process.ExitCode -ne 1 -or -not (Select-String -LiteralPath $errorLog -Pattern SIMULATED_REGISTER_FAILURE -Quiet)) {
    throw 'Registratiefout moet exitcode 1 en een foutlog opleveren'
}
$process.Dispose()
'PASS: mislukte registratie geeft exitcode 1 met foutlog voor de installer.'
