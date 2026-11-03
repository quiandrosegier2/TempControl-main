$ErrorActionPreference = 'Stop'
$source = Get-Content (Join-Path $PSScriptRoot '..\pc-app\installer\flash-firmware.ps1') -Raw
$tokens=$null; $errors=$null
$ast=[Management.Automation.Language.Parser]::ParseInput($source,[ref]$tokens,[ref]$errors)
if($errors.Count){throw 'Uploader syntax error'}
$function=$ast.Find({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Stop-MonitoringProcesses'},$true)
Invoke-Expression $function.Extent.Text
$script:stopped=@()
function Get-CimInstance {
    @(
        [pscustomobject]@{Name='TempMonitor.exe';ProcessId=10;CommandLine='"C:\Program Files\TempMonitor\TempMonitor.exe"'},
        [pscustomobject]@{Name='TempMonitor.exe';ProcessId=20;CommandLine='"C:\Program Files\TempMonitor\TempMonitor.exe" --settings'},
        [pscustomobject]@{Name='Qscreen.exe';ProcessId=30;CommandLine='Qscreen.exe --port COM3'},
        [pscustomobject]@{Name='Qscreen.exe';ProcessId=50;CommandLine='Qscreen.exe --monitor'},
        [pscustomobject]@{Name='Qscreen.exe';ProcessId=60;CommandLine='Qscreen.exe'},
        [pscustomobject]@{Name='Qscreen.exe';ProcessId=70;CommandLine='Qscreen.exe --settings'}
    )
}
function Get-Process { [pscustomobject]@{Id=40} }
function Stop-Process {
    param([int]$Id,[switch]$Force,[Parameter(ValueFromPipeline)]$InputObject)
    process { if($InputObject){$script:stopped+=$InputObject.Id}else{$script:stopped+=$Id} }
}
Stop-MonitoringProcesses
if(($script:stopped -join ',') -ne '10,30,50,40'){throw 'Upload stopped the settings window or left a monitor process running'}
'PASS: upload stops monitoring and PresentMon while preserving Layout Studio.'
