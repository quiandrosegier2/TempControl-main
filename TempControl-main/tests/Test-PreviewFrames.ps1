$ErrorActionPreference='Stop'
$installer=Join-Path $PSScriptRoot '..\pc-app\installer'
$source=Get-Content (Join-Path $installer 'TempMonitor.iss') -Raw
$count=[int][regex]::Match($source,'#define PreviewFrameCount (\d+)').Groups[1].Value
$layouts=@([regex]::Matches($source,"AddLayout\('([^']+)'")|ForEach-Object {$_.Groups[1].Value}|Select-Object -Skip 3)
if($layouts.Count -ne 18 -or $count -lt 2){throw 'Onvolledige animatiecatalogus'}
foreach($layout in $layouts){
    $hashes=@()
    for($frame=0;$frame -lt $count;$frame++){
        $file=Join-Path $installer ('preview-animation\{0}-{1:D2}.bmp' -f $layout,$frame)
        $stream=[IO.File]::OpenRead($file)
        try{
            $header=New-Object byte[] 26
            if($stream.Read($header,0,26) -ne 26 -or $header[0] -ne 66 -or $header[1] -ne 77 -or [BitConverter]::ToInt32($header,18) -ne 220 -or [BitConverter]::ToInt32($header,22) -ne 220){throw "Ongeldig animatiebeeld: $file"}
        }finally{$stream.Dispose()}
        $hashes+=(Get-FileHash -LiteralPath $file).Hash
    }
    if(@($hashes|Select-Object -Unique).Count -lt 2){throw "Animatie beweegt niet: $layout"}
    "PASS: $layout ($count geldige beelden, variatie aangetoond)"
}
