function Get-SafeFirmwarePlan {
    param([byte[]]$Backup,[byte[]]$Factory,[bool]$AllowBlank=$false)
    if($Backup.Length -ne 0x1000000 -or $Factory.Length -le 0x10000 -or $Factory.Length -gt 0x650000){throw 'Ongeldige 16 MB-back-up of firmwaregrootte.'}
    $blank=$true
    for($i=0;$i -lt $Backup.Length;$i++){if($Backup[$i] -ne 255){$blank=$false;break}}
    if($blank){if(!$AllowBlank){throw 'Leeg scherm: kies expliciet initialiseren om de fabrieksindeling te plaatsen.'};return @{Offset='0x0';Image=$Factory;Factory=$true}}
    for($i=0x8000;$i -lt 0x9000;$i++){if($Backup[$i] -ne $Factory[$i]){throw 'Afwijkende partitie-indeling. Automatisch bijwerken is gestopt; je back-up is bewaard.'}}
    $factoryBoot=$true;$erasedBoot=$true
    for($i=0xe000;$i -lt 0x10000;$i++){if($Backup[$i] -ne $Factory[$i]){$factoryBoot=$false};if($Backup[$i] -ne 255){$erasedBoot=$false}}
    if(!$factoryBoot -and !$erasedBoot){throw 'Afwijkende actieve OTA-partitie. Bijwerken is gestopt; herstel vereist beoordeling.'}
    [byte[]]$app=New-Object byte[] ($Factory.Length-0x10000)
    [Buffer]::BlockCopy($Factory,0x10000,$app,0,$app.Length)
    return @{Offset='0x10000';Image=$app;Factory=$false}
}
