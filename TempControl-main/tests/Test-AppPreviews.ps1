$ErrorActionPreference='Stop'
$installer=Join-Path $PSScriptRoot '..\pc-app\installer'
$source=Get-Content (Join-Path $installer 'TempMonitor.iss') -Raw
$layouts=@([regex]::Matches($source,"AddLayout\('([^']+)'")|ForEach-Object {$_.Groups[1].Value}|Select-Object -Skip 3 -First 23)
foreach($layout in $layouts){
    $hashes=@()
    for($frame=0;$frame -lt 40;$frame++){
        $file=Join-Path $installer ('app-previews\{0}-{1:D2}.png' -f $layout,$frame)
        $stream=[IO.File]::OpenRead($file)
        try{
            $header=New-Object byte[] 24
            if($stream.Read($header,0,24) -ne 24 -or $header[0] -ne 137 -or $header[1] -ne 80){throw "Invalid PNG: $file"}
            # 412 = 0x0000019c; PNG stores width and height as big-endian integers.
            if(($header[16..23] -join ',') -ne '0,0,1,156,0,0,1,156'){throw "Preview is not 412x412: $file"}
        }finally{$stream.Dispose()}
        $hashes+=(Get-FileHash -LiteralPath $file).Hash
    }
    if(@($hashes|Select-Object -Unique).Count -lt 2){throw "Preview is not animated: $layout"}
}
'PASS: 920 sharp PNG previews, 412x412 dimensions and animation variation.'
