# Wordt door de installer aangeroepen: registreert of verwijdert de autostart-taak.
param(
    [Parameter(Mandatory)][ValidateSet('Install', 'Uninstall')][string]$Action,
    [string]$ExePath
)

$ErrorActionPreference = 'Stop'
trap {
    $message = $_ | Out-String
    try {
        $directory = Join-Path $env:ProgramData 'TempMonitor'
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
        [IO.File]::WriteAllText((Join-Path $directory 'task-error.log'), $message)
    } catch { }
    Write-Host $message
    exit 1
}
$taskName = 'TempMonitor'

# Lopende instantie altijd eerst stoppen
Stop-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
Get-Process -Name Qscreen, TempMonitor, PresentMon -ErrorAction SilentlyContinue | Stop-Process -Force

if ($Action -eq 'Uninstall') {
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
    return
}

# Start bij aanmelden van elke gebruiker (BUILTIN\Users), met de hoogste rechten
# zodat de CPU-sensoren leesbaar zijn.
$scheduledAction = New-ScheduledTaskAction -Execute $ExePath -Argument '--monitor' -WorkingDirectory (Split-Path $ExePath)
$trigger   = New-ScheduledTaskTrigger -AtLogOn
$principal = New-ScheduledTaskPrincipal -GroupId 'S-1-5-32-545' -RunLevel Highest
$settings  = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries `
                -ExecutionTimeLimit ([TimeSpan]::Zero) -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1) `
                -MultipleInstances IgnoreNew

Register-ScheduledTask -TaskName $taskName -Action $scheduledAction -Trigger $trigger -Principal $principal `
    -Settings $settings -Description 'Stuurt CPU/GPU-temperatuur naar het ronde USB-scherm' -Force | Out-Null
Start-ScheduledTask -TaskName $taskName
