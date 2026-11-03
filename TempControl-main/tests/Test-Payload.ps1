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
if($version -notlike '2.4.0*'){throw ('Wrong app version: '+$version)}
foreach($name in @('accounts\Qscreen.Accounts.exe','accounts\Qscreen.Accounts.dll','accounts\account-client.json','accounts\coreclr.dll')){if(-not(Test-Path -LiteralPath (Join-Path $publish $name))){throw ('Missing account payload: '+$name)}}
$publicConfig=Get-Content (Join-Path $publish 'accounts\account-client.json') -Raw|ConvertFrom-Json
if($publicConfig.ProjectUrl -ne 'https://ybmesonqfclarqigwxfe.supabase.co' -or $publicConfig.PublishableKey -notlike 'sb_publishable_*'){throw 'Invalid public account configuration'}
if($publicConfig.RegistrationEnabled){throw 'Registration must remain gated until account pages and server consent are deployed'}
'PASS: Qscreen 2.4.0 payload, preserved optional self-contained account app, firmware offsets and matching factory image, no stale launcher.'
