$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot
$build=Join-Path $project 'firmware\.pio\build\waveshare_s3_touch_lcd_146'
$publish=Join-Path $project 'pc-app\installer\publish'
$factory=[IO.File]::ReadAllBytes((Join-Path $build 'firmware.factory.bin'))
function Hash([byte[]]$data){$sha=[Security.Cryptography.SHA256]::Create();try{[BitConverter]::ToString($sha.ComputeHash($data))}finally{$sha.Dispose()}}
foreach($part in @(@('partitions.bin',0x8000),@('firmware.bin',0x10000))){
    $expected=[IO.File]::ReadAllBytes((Join-Path $build $part[0]))
    $actual=New-Object byte[] $expected.Length
    [Array]::Copy($factory,[int]$part[1],$actual,0,$actual.Length)
    if((Hash $actual) -ne (Hash $expected)){throw ('Invalid factory offset: '+$part[0])}
}
if((Hash $factory) -ne (Hash ([IO.File]::ReadAllBytes((Join-Path $publish 'firmware\TempControl.factory.bin'))))){throw 'Bundled firmware differs from build'}
foreach($name in @('Qscreen.exe','Qscreen.dll','Qscreen.runtimeconfig.json','PresentMon.exe','coreclr.dll','System.Windows.Forms.dll','firmware\esptool\esptool.exe','drivers\PawnIO_setup.exe')){
    if(-not(Test-Path -LiteralPath (Join-Path $publish $name))){throw ('Missing payload: '+$name)}
}
if(Test-Path -LiteralPath (Join-Path $publish 'TempMonitor.exe')){throw 'Legacy executable remains in clean publish output'}
$version=[Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $publish 'Qscreen.exe')).ProductVersion
if($version -notlike '1.5.0*'){throw ('Wrong app version: '+$version)}
'PASS: Qscreen 1.5.0 payload, self-contained WinForms, firmware offsets and matching factory image, no stale launcher.'
