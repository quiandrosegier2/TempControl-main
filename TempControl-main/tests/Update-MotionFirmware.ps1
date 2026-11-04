param([string]$Port='COM3',[switch]$Apply,[switch]$Restore,[string]$ExpectedBackupSha='')
$ErrorActionPreference='Stop'
if($Apply -and $Restore){throw 'Choose either upgrade or recovery.'}
$writeRequested=$Apply -or $Restore
$repo=Split-Path $PSScriptRoot -Parent
$python=Join-Path $repo '.platformio/penv/Scripts/python.exe'
$esptool=Join-Path $repo '.platformio/packages/tool-esptoolpy/esptool.py'
$build=Join-Path $repo 'firmware/.pio/build/waveshare_s3_touch_lcd_146'
$backupRoot=Join-Path $repo 'pc-app/installer/tool-cache/motion-firmware-backup'
New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
$backup=Join-Path $backupRoot 'flash-16MB.bin'
$status=Join-Path $backupRoot 'status.json'
$task=Get-ScheduledTask -TaskName TempMonitor -ErrorAction SilentlyContinue
$restart=$task -and $task.State -eq 'Running'
function Tool([string[]]$Arguments){& $python $esptool --chip esp32s3 --port $Port @Arguments;if($LASTEXITCODE){throw "esptool failed ($LASTEXITCODE)"}}
try{
    if($restart){Stop-ScheduledTask -TaskName TempMonitor;Start-Sleep -Seconds 2}
    if(!$writeRequested){
        if(Test-Path -LiteralPath $backup){throw 'A previous backup exists. Preserve/review it before creating another.'}
        Tool @('read-flash','0','0x1000000',$backup)
    }
    if(!(Test-Path -LiteralPath $backup)){throw 'A full flash backup is required before upgrade.'}
    [byte[]]$bytes=[IO.File]::ReadAllBytes($backup)
    if($bytes.Length -ne 0x1000000){throw 'Backup is not exactly 16 MB.'}
    $sha=(Get-FileHash -LiteralPath $backup -Algorithm SHA256).Hash
    [byte[]]$table=[IO.File]::ReadAllBytes((Join-Path $build 'partitions.bin'))
    for($i=0;$i -lt $table.Length;$i++){if($bytes[0x8000+$i] -ne $table[$i]){throw 'Installed partition table differs; automatic application-only upgrade is refused.'}}
    # Only the factory app0 boot selection is supported by this guarded helper.
    $bootData=Get-ChildItem (Join-Path $repo '.platformio/packages') -Recurse -Filter boot_app0.bin | Select-Object -First 1
    if(!$bootData){throw 'Reference OTA boot selection missing.'}
    [byte[]]$expected=[IO.File]::ReadAllBytes($bootData.FullName)
    $factory=$true;$erased=$true
    for($i=0;$i -lt 0x2000;$i++){if($bytes[0xe000+$i] -ne 255){$erased=$false};if($i -ge $expected.Length -or $bytes[0xe000+$i] -ne $expected[$i]){$factory=$false}}
    if(!$factory -and !$erased){throw 'OTA boot selection is not factory app0; manual review required.'}
    $firmware=Join-Path $build 'firmware.bin'
    if((Get-Item -LiteralPath $firmware).Length -ge 0x640000){throw 'Candidate does not fit application slot.'}
    [byte[]]$original=New-Object byte[] 0x640000
    [Buffer]::BlockCopy($bytes,0x10000,$original,0,0x640000)
    [IO.File]::WriteAllBytes((Join-Path $backupRoot 'original-app0.bin'),$original)
    if($Restore){$firmware=Join-Path $backupRoot 'original-app0.bin'}
    if($writeRequested){
        if(!$ExpectedBackupSha -or $ExpectedBackupSha -ne $sha){throw 'Explicit reviewed backup digest required.'}
        # Verify the current table again immediately before the single app-slot write.
        $current=Join-Path $backupRoot 'current-partitions.bin';Tool @('read-flash','0x8000',('0x{0:x}' -f $table.Length),$current)
        if((Get-FileHash -LiteralPath $current).Hash -ne (Get-FileHash -LiteralPath (Join-Path $build 'partitions.bin')).Hash){throw 'Partition table changed since backup.'}
        Tool @('write-flash','0x10000',$firmware)
        Tool @('verify-flash','0x10000',$firmware)
    }
    @{State=$(if($writeRequested){'written-and-verified'}else{'backup-reviewed'});BackupSha256=$sha;CandidateSha256=(Get-FileHash -LiteralPath $firmware).Hash;OnlyWriteOffset='0x10000';FirmwareFlashed=[bool]$writeRequested;UpdatedUtc=[DateTime]::UtcNow}|ConvertTo-Json|Set-Content -LiteralPath $status
}catch{@{State='failed';Error=$_.Exception.Message;FirmwareFlashed=$false;UpdatedUtc=[DateTime]::UtcNow}|ConvertTo-Json|Set-Content -LiteralPath $status;throw}
finally{if($restart){Start-ScheduledTask -TaskName TempMonitor}}
