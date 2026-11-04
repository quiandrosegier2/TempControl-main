param([string]$BackupPath)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
. (Join-Path $root 'pc-app/installer/firmware-safety.ps1')
[byte[]]$factory=[IO.File]::ReadAllBytes((Join-Path $root 'firmware/.pio/build/waveshare_s3_touch_lcd_146/firmware.factory.bin'))
if($BackupPath){[byte[]]$backup=[IO.File]::ReadAllBytes($BackupPath)}else{[byte[]]$backup=New-Object byte[] 0x1000000;[Buffer]::BlockCopy($factory,0,$backup,0,$factory.Length)}
$plan=Get-SafeFirmwarePlan -Backup $backup -Factory $factory
if($plan.Offset -ne '0x10000' -or $plan.Image.Length -ne $factory.Length-0x10000){throw 'Application-only plan incorrect.'}
$backup[0x8000]=$backup[0x8000] -bxor 1
$rejected=$false;try{Get-SafeFirmwarePlan -Backup $backup -Factory $factory | Out-Null}catch{$rejected=$true}
if(!$rejected){throw 'Different partition table accepted.'}
$backup[0x8000]=$backup[0x8000] -bxor 1
$backup[0xe000]=$backup[0xe000] -bxor 1
$rejected=$false;try{Get-SafeFirmwarePlan -Backup $backup -Factory $factory | Out-Null}catch{$rejected=$true}
if(!$rejected){throw 'Different OTA boot selection accepted.'}
foreach($relative in @('pc-app/installer/flash-firmware.ps1','pc-app/installer/firmware-safety.ps1','tests/Update-MotionFirmware.ps1')){
    $errors=$null;$tokens=$null;[void][System.Management.Automation.Language.Parser]::ParseFile((Join-Path $root $relative),[ref]$tokens,[ref]$errors)
    if($errors.Count){throw ($errors|Out-String)}
}
Write-Output 'PASS: application-only upgrade plan; different partitions/OTA rejected; PowerShell scripts parse.'
